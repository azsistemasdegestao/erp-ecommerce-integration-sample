using ErpEcommerceIntegration.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Quartz;

namespace ErpEcommerceIntegration.Workers.Jobs;

[DisallowConcurrentExecution]
public sealed class SyncInventoryJob : IJob
{
    private readonly ISyncOrchestrator _sync;
    private readonly ILogger<SyncInventoryJob> _logger;
    private readonly IConfiguration _config;

    public SyncInventoryJob(ISyncOrchestrator sync, ILogger<SyncInventoryJob> logger, IConfiguration config)
    {
        _sync   = sync;
        _logger = logger;
        _config = config;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        if (_config.GetValue<bool>("Jobs:BypassSyncInventory"))
        {
            _logger.LogWarning("[JOB] SyncInventoryJob bypassed via configuration.");
            return;
        }

        _logger.LogInformation("[JOB] SyncInventoryJob triggered at {FireTime}", context.FireTimeUtc);
        try
        {
            await _sync.SynchronizeInventoryAsync(context.CancellationToken);
            _logger.LogInformation("[JOB] SyncInventoryJob finished — NextFireTime={Next}", context.NextFireTimeUtc);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[JOB] SyncInventoryJob failed");
            throw new JobExecutionException(ex, refireImmediately: false);
        }
    }
}
