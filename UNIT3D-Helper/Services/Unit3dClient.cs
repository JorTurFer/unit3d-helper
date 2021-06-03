using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using UNIT3D_Helper.Entities;

namespace UNIT3D_Helper.Services
{
    public class Unit3dClient
    {
        private readonly HttpClient _client;
        private readonly ILogger<Unit3dClient> _logger;
        private readonly TrackerOptions _trackerOptions;
        private readonly string _torrentPattern;
        private int _elementCount = 0;
        private LoginAssets _assets;
        private int _filesInRowReadyPreviouslyBeforeStop;
        private int _filesInRowReadyPreviously;

        public Unit3dClient(HttpClient client, IOptions<TrackerOptions> trackerOptions, ILogger<Unit3dClient> logger)
        {
            _client = client;
            _logger = logger;
            _trackerOptions = trackerOptions?.Value;
            _torrentPattern = string.Concat(_trackerOptions.Url, @"/torrents/(\d{1,10})");
        }

        public async Task ExecuteAsync(int FilesInRowReadyPreviouslyBeforeStop, CancellationToken cancellation)
        {
            _filesInRowReadyPreviouslyBeforeStop = FilesInRowReadyPreviouslyBeforeStop;
            var page = 1;
            MatchCollection matches;

            await LoginAsync();
            do
            {
                _logger.LogInformation("Processing page: {page}", page);
                using var request = new HttpRequestMessage(new HttpMethod("POST"), $"users/{_trackerOptions.Username}/userFilters");
                request.Content = new StringContent($"_token={_assets.Token}&page={page}&sorting=created_at&direction=desc&name=&view=history");

                request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/x-www-form-urlencoded; charset=UTF-8");

                var response = await ExecuteRequestAsync(request);
                var html = await response.Content.ReadAsStringAsync();
                matches = Regex.Matches(html, _torrentPattern);
                await ProcessMatchesAsync(matches);
                page++;
            } while (matches.Count > 0 && (_filesInRowReadyPreviously != _filesInRowReadyPreviouslyBeforeStop));
        }

        private async Task LoginAsync()
        {
            using var hello = new HttpRequestMessage(new HttpMethod("GET"), $"login");
            await ExecuteRequestAsync(hello);

            using var request = new HttpRequestMessage(new HttpMethod("POST"), $"login");
            request.Content = new StringContent($"_token={_assets.Token}&username={_trackerOptions.Username}&password={_trackerOptions.Password}&_captcha={_assets.Captcha}&_username=&{_assets.Key}={_assets.Value}");

            request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/x-www-form-urlencoded; charset=UTF-8");

            var response = await ExecuteRequestAsync(request);

            using var checkRequest = new HttpRequestMessage(new HttpMethod("GET"), "");
            var check = await ExecuteRequestAsync(checkRequest);

            check.EnsureSuccessStatusCode();
            var html = await check.Content.ReadAsStringAsync();
            if (!html.Contains(_trackerOptions.Username))
            {
                throw new HttpRequestException("Not correctly logged");
            }
        }

        private async Task ProcessMatchesAsync(MatchCollection matches)
        {
            foreach (Match match in matches)
            {
                using var request = new HttpRequestMessage(new HttpMethod("GET"), $"torrents/{match.Groups[1].Value}");
                var response = await ExecuteRequestAsync(request);
                var html = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"\tProcessing element: {_elementCount} --> {Regex.Match(html, "<title>(.*)</title>").Groups[1].Value}");
                var neededComment = await CommentAsync(match, html);
                var neededThanks = await GiveThanksAsync(match, html);
                if(neededComment || neededThanks)
                {
                    await DonateAsync(match);
                    _filesInRowReadyPreviously = 0;                    
                }
                else
                {
                    _filesInRowReadyPreviously++;
                }
                               
                _elementCount++;

                if (_filesInRowReadyPreviously == _filesInRowReadyPreviouslyBeforeStop)
                {
                    _logger.LogInformation($"Reached {_filesInRowReadyPreviously} those dont need nothing in a row, skipping the cycle");
                    break;
                }
            }
        }

        private async Task<bool> CommentAsync(Match match, string html)
        {
            var neededComment = !IsCommented(html);
            if (neededComment)
            {
                using var request = new HttpRequestMessage(new HttpMethod("GET"), $"comments/thanks/{match.Groups[1].Value}");
                _ = await ExecuteRequestAsync(request);
                _logger.LogInformation($"\tComentario hecho");
            }
            return neededComment;
        }

        private async Task DonateAsync(Match match)
        {
            if (_trackerOptions.TipQuantity <= 0) return;
            
            using var request = new HttpRequestMessage(new HttpMethod("POST"), $"torrents/{match.Groups[1].Value}/tip_uploader");
            request.Content = new StringContent($"_token={_assets.Token}&tip={_trackerOptions.TipQuantity}");
            request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/x-www-form-urlencoded; charset=UTF-8");
            request.Headers.TryAddWithoutValidation("Referer", $"{_trackerOptions.Url}/torrents/{match.Groups[1].Value}");
            _ = await ExecuteRequestAsync(request);
            _logger.LogInformation($"\tPropina dada");
        }

        private async Task<bool> GiveThanksAsync(Match match, string html)
        {
            var neededThanks = !IsGivenThanks(html);
            if (neededThanks)
            {
                var payload = JsonConvert.DeserializeObject<ThanksPayload>(HttpUtility.HtmlDecode(GetBody(html)));
                var update = new Update
                {
                    type = "callMethod",
                    payload = new Payload
                    {
                        method = "store",
                        @params = new List<int> { Convert.ToInt32(match.Groups[1].Value) }
                    }
                };
                payload.updates.Add(update);

                using var request = new HttpRequestMessage(new HttpMethod("POST"), "livewire/message/thank-button");
                request.Headers.TryAddWithoutValidation("X-Livewire", "true");
                request.Headers.TryAddWithoutValidation("X-CSRF-TOKEN", _assets.Token);

                var body = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                request.Content = body;
                request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

                var response = await ExecuteRequestAsync(request);
                response.EnsureSuccessStatusCode();

                _logger.LogInformation($"\tGracias dadas");
            }
            return neededThanks;
        }

        private string GetBody(string html) => Regex.Match(html, "wire:initial-data=\"(.*)\" wire:click").Groups[1].Value;
        private bool IsGivenThanks(string html) => Regex.IsMatch(html, "thank-button.+   *disabled");
        private bool IsCommented(string html) => html.Contains("kabestrus</span></a></strong>");

        private async Task<HttpResponseMessage> ExecuteRequestAsync(HttpRequestMessage request)
        {
            if (_assets is not null) request.Headers.TryAddWithoutValidation("Set-Cookie", _assets.Coockies);
            var response = await _client.SendAsync(request);
            _assets = await GetAssetsAsync(response);
            return response;
        }

        private async Task<LoginAssets> GetAssetsAsync(HttpResponseMessage response)
        {
            var html = await response.Content.ReadAsStringAsync();
            var token = Regex.Match(html, "csrf-token\" content=\"(.*)\">").Groups[1].Value;
            var captcha = Regex.Match(html, "name=\"_captcha\" value=\"(.*)\" />").Groups[1].Value;
            var match = Regex.Match(html, "name=\"(\\w{16})\" value=\"(\\d{10})\" />");
            var key = match.Groups[1].Value;
            var value = match.Groups[2].Value;
            var coockies = response.Headers.GetValues("Set-Cookie");
            return new LoginAssets { Token = token, Captcha = captcha, Key = key, Value = value, Coockies = coockies };
        }

        class LoginAssets
        {
            public string Token { get; set; }
            public string Captcha { get; set; }
            public string Key { get; set; }
            public string Value { get; set; }
            public IEnumerable<string> Coockies { get; set; }
        }
    }
}
