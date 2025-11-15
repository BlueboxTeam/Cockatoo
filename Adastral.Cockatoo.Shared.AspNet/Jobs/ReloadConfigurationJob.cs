using FluentScheduler;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Adastral.Cockatoo.Common.AspNet.Jobs;

public class ReloadConfigurationJob(ILogger<ReloadConfigurationJob> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        JobManager.AddJob(ExecuteJob, schedule => schedule
                .ToRunOnceAt(DateTime.Now.AddSeconds(5))
                .AndEvery(5)
                .Minutes());
        return Task.CompletedTask;
    }
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private void ExecuteJob()
    {
        const string sentrySlug = nameof(ReloadConfigurationJob);
        logger.LogDebug("Job Started");
        var checkInId = SentrySdk.CaptureCheckIn(sentrySlug, CheckInStatus.InProgress);
        try
        {
            AppConfig.Instance.ReadFromFile(FeatureFlags.ConfigLocation);
            SentrySdk.CaptureCheckIn(sentrySlug, CheckInStatus.Ok, checkInId);
            logger.LogDebug("Job Complete");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to run job ({SentryCheckInId})", checkInId);
            SentrySdk.CaptureException(ex);
            SentrySdk.CaptureCheckIn(sentrySlug, CheckInStatus.Error, checkInId);
        }
    }
}
