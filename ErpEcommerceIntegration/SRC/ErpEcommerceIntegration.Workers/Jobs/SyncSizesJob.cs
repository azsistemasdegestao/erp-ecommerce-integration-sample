using ErpEcommerceIntegration.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Quartz;

namespace ErpEcommerceIntegration.Workers.Jobs;

[DisallowConcurrentExecution]
public sealed class SyncSizesJob : IJob
{
    private readonly ISyncOrchestrator _sync;
    private readonly ILogger<SyncSizesJob> _logger;
    private readonly IConfiguration _config;

    public SyncSizesJob(ISyncOrchestrator sync, ILogger<SyncSizesJob> logger, IConfiguration config)
    {
        _sync   = sync;
        _logger = logger;
        _config = config;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        if (_config.GetValue<bool>("Jobs:BypassSyncSizes"))
        {
            _logger.LogWarning("[JOB] SyncSizesJob bypassed via configuration.");
            return;
        }

        _logger.LogInformation("[JOB] SyncSizesJob triggered at {FireTime}", context.FireTimeUtc);
        try
        {
            await _sync.SynchronizeSizesAsync(context.CancellationToken);
            _logger.LogInformation("[JOB] SyncSizesJob finished — NextFireTime={Next}", context.NextFireTimeUtc);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[JOB] SyncSizesJob failed");
            throw new JobExecutionException(ex, refireImmediately: false);
        }
    }
}
