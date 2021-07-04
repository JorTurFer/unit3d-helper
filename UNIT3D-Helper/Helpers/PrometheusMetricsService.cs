using Prometheus;
using System;

namespace UNIT3D_Helper.Helpers
{
    public static class PrometheusMetricsHelper
    {
        private static readonly Gauge ExecutionInProgress = Metrics.CreateGauge("execution_in_progress", "Cycle in progress");
        private static readonly Gauge ExecutionInIdle = Metrics.CreateGauge("execution_in_idle", "Cycle in waiting");
        private static readonly Counter Thanks = Metrics.CreateCounter("thanks_given_total", "Number of thanks given");
        private static readonly Counter Comments = Metrics.CreateCounter("comments_done_total", "Number of comments done");
        private static readonly Counter Tips = Metrics.CreateCounter("tips_given_total", "Amount fo tips given");

        public static IDisposable TrackExecution() => ExecutionInProgress.TrackInProgress();
        public static IDisposable TrackIdle() => ExecutionInIdle.TrackInProgress();


        public static void IncreaseThanks() => Thanks.Inc();
        public static void IncreaseComments() => Comments.Inc();
        public static void IncreaseTips(double value) => Tips.Inc(value);
    }
}
