using ErpEcommerceIntegration.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Quartz;

namespace ErpEcommerceIntegration.Workers.Jobs;

[DisallowConcurrentExecution]
public sealed class SyncProductsJob : IJob
{
    private readonly ISyncOrchestrator _sync;
    private readonly ILogger<SyncProductsJob> _logger;
    private readonly IConfiguration _config;

    public SyncProductsJob(ISyncOrchestrator sync, ILogger<SyncProductsJob> logger, IConfiguration config)
    {
        _sync   = sync;
        _logger = logger;
        _config = config;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        if (_config.GetValue<bool>("Jobs:BypassSyncProducts"))
        {
            _logger.LogWarning("[JOB] SyncProductsJob bypassed via configuration.");
            return;
        }

        _logger.LogInformation("[JOB] SyncProductsJob triggered at {FireTime}", context.FireTimeUtc);
        try
        {
            await _sync.SynchronizeProductsAsync(context.CancellationToken);
            _logger.LogInformation("[JOB] SyncProductsJob finished — NextFireTime={Next}", context.NextFireTimeUtc);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[JOB] SyncProductsJob failed");
            throw new JobExecutionException(ex, refireImmediately: false);
        }
    }
}
