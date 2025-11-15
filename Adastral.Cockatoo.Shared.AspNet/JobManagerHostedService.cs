using FluentScheduler;
using Microsoft.Extensions.Hosting;

namespace Adastral.Cockatoo.Common.AspNet;

public class JobManagerHostedService : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        JobManager.Start();
        return Task.CompletedTask;
    }
    public Task StopAsync(CancellationToken cancellationToken)
    {
        JobManager.StopAndBlock();
        return Task.CompletedTask;
    }
}
