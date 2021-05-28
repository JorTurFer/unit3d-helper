using System.Threading;
using System.Threading.Tasks;

namespace UNIT3D_Helper.Helpers
{
    public static class Tools
    {
        public static async Task SafeDelayAsync(int delay, CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(delay, cancellationToken);
            }
            catch (TaskCanceledException)
            {

            }
        }
    }
}
