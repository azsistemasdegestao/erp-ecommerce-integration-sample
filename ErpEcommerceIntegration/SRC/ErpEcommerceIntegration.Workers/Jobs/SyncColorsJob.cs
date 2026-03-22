using ErpEcommerceIntegration.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Quartz;

namespace ErpEcommerceIntegration.Workers.Jobs;

[DisallowConcurrentExecution]
public sealed class SyncColorsJob : IJob
{
    private readonly ISyncOrchestrator _sync;
    private readonly ILogger<SyncColorsJob> _logger;
    private readonly IConfiguration _config;

    public SyncColorsJob(ISyncOrchestrator sync, ILogger<SyncColorsJob> logger, IConfiguration config)
    {
        _sync   = sync;
        _logger = logger;
        _config = config;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        if (_config.GetValue<bool>("Jobs:BypassSyncColors"))
        {
            _logger.LogWarning("[JOB] SyncColorsJob bypassed via configuration.");
            return;
        }

        _logger.LogInformation("[JOB] SyncColorsJob triggered at {FireTime}", context.FireTimeUtc);
        try
        {
            await _sync.SynchronizeColorsAsync(context.CancellationToken);
            _logger.LogInformation("[JOB] SyncColorsJob finished — NextFireTime={Next}", context.NextFireTimeUtc);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[JOB] SyncColorsJob failed");
            throw new JobExecutionException(ex, refireImmediately: false);
        }
    }
}
