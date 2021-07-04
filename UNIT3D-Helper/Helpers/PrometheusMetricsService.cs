using Prometheus;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using UNIT3D_Helper.Entities;

namespace UNIT3D_Helper.Helpers
{
    public static class PrometheusMetricsHelper
    {
        private const string DataPath = "/stats";
        private static PrometheusData _data;
        static PrometheusMetricsHelper()
        {
            if (!File.Exists(DataPath))
            {
                _data = new PrometheusData();
                return;
            }
            var data = File.ReadAllText(DataPath);
            _data = JsonSerializer.Deserialize<PrometheusData>(data);
            Thanks.IncTo(_data.Thanks);
            Comments.IncTo(_data.Comments);
            Tips.IncTo(_data.Tips);
        }

        private static readonly Gauge ExecutionInProgress = Metrics.CreateGauge("execution_in_progress", "Cycle in progress");
        private static readonly Gauge ExecutionInIdle = Metrics.CreateGauge("execution_in_idle", "Cycle in waiting");
        private static readonly Counter Thanks = Metrics.CreateCounter("thanks_given_total", "Number of thanks given");
        private static readonly Counter Comments = Metrics.CreateCounter("comments_done_total", "Number of comments done");
        private static readonly Counter Tips = Metrics.CreateCounter("tips_given_total", "Amount fo tips given");

        public static async Task SaveStatsAsync()
        {
            var data = JsonSerializer.Serialize(_data);
            await File.WriteAllTextAsync(DataPath, data);
        }

        public static IDisposable TrackExecution() => ExecutionInProgress.TrackInProgress();
        public static IDisposable TrackIdle() => ExecutionInIdle.TrackInProgress();


        public static void IncreaseThanks()
        {
            _data.Thanks++;
            Thanks.Inc();
        }
        public static void IncreaseComments()
        {
            _data.Comments++;
            Comments.Inc();
        }
        public static void IncreaseTips(double value)
        {
            _data.Tips++;
            Tips.Inc(value);
        }
    }
}
