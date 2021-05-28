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

        public Unit3dClient(HttpClient client, IOptions<TrackerOptions> trackerOptions, ILogger<Unit3dClient> logger)
        {
            _client = client;
            _logger = logger;
            _trackerOptions = trackerOptions?.Value;
            _torrentPattern = string.Concat(_trackerOptions.Url, @"torrents/(\d{1,10})");
        }

        public async Task ExecuteAsync(CancellationToken cancellation)
        {
            var page = 1;
            MatchCollection matches;

            var token = await GetAntiforgeryTokenAsync();
            do
            {
                _logger.LogInformation("Processing page: {page}", page);
                using var request = new HttpRequestMessage(new HttpMethod("POST"), $"users/{_trackerOptions.Username}/userFilters");

                request.Content = new StringContent($"_token={token}&page={page}&sorting=created_at&direction=desc&name=&view=history");

                request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/x-www-form-urlencoded; charset=UTF-8");

                var response = await _client.SendAsync(request);
                var html = await response.Content.ReadAsStringAsync();
                matches = Regex.Matches(html, _torrentPattern);
                await ProcessMatchesAsync(matches,token);
                page++;
            } while (matches.Count > 0);
        }

        private async Task<string> GetAntiforgeryTokenAsync()
        {
            var request = await _client.GetAsync("torrents");
            var html = await request.Content.ReadAsStringAsync();
            var token = GetToken(html);
            return token;
        }

        private async Task ProcessMatchesAsync(MatchCollection matches,string token)
        {
            foreach (Match match in matches)
            {
                var response = await _client.GetAsync($"torrents/{match.Groups[1].Value}");
                var html = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"\tProcessing element: {_elementCount} --> {Regex.Match(html, "<title>(.*)</title>").Groups[1].Value}");
                await GiveThanksAsync(match, html,token);
                await CommentAsync(match, html);
                _elementCount++;
            }
        }

        private async Task CommentAsync(Match match, string html)
        {
            if (!IsCommented(html))
            {
                _ = await _client.GetAsync($"comments/thanks/{match.Groups[1].Value}");
                _logger.LogInformation($"\tComentario hecho");
            }
        }

        private async Task GiveThanksAsync(Match match, string html,string token)
        {
            if (!IsGivenThanks(html))
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

                var body = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                using var request = new HttpRequestMessage(new HttpMethod("POST"), "livewire/message/thank-button");
                request.Headers.TryAddWithoutValidation("X-Livewire", "true");

                request.Content = body;
                request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
                request.Headers.TryAddWithoutValidation("Referer", $"torrents/{match.Groups[1].Value}");
                request.Headers.TryAddWithoutValidation("X-CSRF-TOKEN", token);

                var response = await _client.SendAsync(request);
                response.EnsureSuccessStatusCode();


                _logger.LogInformation($"\tGracias dadas");
            }
        }

        private string GetToken(string html) => Regex.Match(html, "csrf-token\" content=\"(.*)\">").Groups[1].Value;
        private string GetBody(string html) => Regex.Match(html, "wire:initial-data=\"(.*)\" wire:click").Groups[1].Value;
        private bool IsGivenThanks(string html) => Regex.IsMatch(html, "thank-button.+   *disabled");
        private bool IsCommented(string html) => html.Contains("kabestrus</span></a></strong>");
    }
}
