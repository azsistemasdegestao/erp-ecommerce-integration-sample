using ErpEcommerceIntegration.Domain.Interfaces;
using ErpEcommerceIntegration.Workers.Jobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Quartz;

namespace ErpEcommerceIntegration.IntegrationTests.Jobs;

/// <summary>
/// Integration tests for Quartz job classes.
/// ISyncOrchestrator and IJobExecutionContext are mocked with NSubstitute.
/// Validates: delegation to orchestrator, bypass-flag behavior, and JobExecutionException wrapping.
/// </summary>
public sealed class SyncJobsTests
{
    private readonly ISyncOrchestrator       _orchestratorMock = Substitute.For<ISyncOrchestrator>();
    private readonly IJobExecutionContext     _contextMock      = Substitute.For<IJobExecutionContext>();

    public SyncJobsTests()
    {
        _contextMock.FireTimeUtc.Returns(DateTimeOffset.UtcNow);
        _contextMock.NextFireTimeUtc.Returns(DateTimeOffset.UtcNow.AddHours(2));
        _contextMock.CancellationToken.Returns(CancellationToken.None);
    }

    // ── SyncProductsJob ──────────────────────────────────────────────────────

    [Fact]
    public async Task SyncProductsJob_Execute_DelegatesToOrchestrator()
    {
        var job = CreateProductsJob(bypass: false);

        await job.Execute(_contextMock);

        await _orchestratorMock.Received(1).SynchronizeProductsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SyncProductsJob_WhenBypassed_DoesNotCallOrchestrator()
    {
        var job = CreateProductsJob(bypass: true);

        await job.Execute(_contextMock);

        await _orchestratorMock.DidNotReceive().SynchronizeProductsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SyncProductsJob_WhenOrchestratorThrows_ThrowsJobExecutionException()
    {
        _orchestratorMock.SynchronizeProductsAsync(Arg.Any<CancellationToken>())
                         .ThrowsAsync(new InvalidOperationException("ERP unreachable"));
        var job = CreateProductsJob(bypass: false);

        var ex = await Assert.ThrowsAsync<JobExecutionException>(() => job.Execute(_contextMock));

        Assert.IsType<InvalidOperationException>(ex.InnerException);
        Assert.False(ex.RefireImmediately);
    }

    // ── SyncInventoryJob ─────────────────────────────────────────────────────

    [Fact]
    public async Task SyncInventoryJob_Execute_DelegatesToOrchestrator()
    {
        var job = CreateInventoryJob(bypass: false);

        await job.Execute(_contextMock);

        await _orchestratorMock.Received(1).SynchronizeInventoryAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SyncInventoryJob_WhenBypassed_DoesNotCallOrchestrator()
    {
        var job = CreateInventoryJob(bypass: true);

        await job.Execute(_contextMock);

        await _orchestratorMock.DidNotReceive().SynchronizeInventoryAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SyncInventoryJob_WhenOrchestratorThrows_ThrowsJobExecutionException()
    {
        _orchestratorMock.SynchronizeInventoryAsync(Arg.Any<CancellationToken>())
                         .ThrowsAsync(new TimeoutException("DB timeout"));
        var job = CreateInventoryJob(bypass: false);

        var ex = await Assert.ThrowsAsync<JobExecutionException>(() => job.Execute(_contextMock));

        Assert.IsType<TimeoutException>(ex.InnerException);
        Assert.False(ex.RefireImmediately);
    }

    // ── SyncColorsJob ────────────────────────────────────────────────────────

    [Fact]
    public async Task SyncColorsJob_Execute_DelegatesToOrchestrator()
    {
        var job = CreateColorsJob(bypass: false);

        await job.Execute(_contextMock);

        await _orchestratorMock.Received(1).SynchronizeColorsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SyncColorsJob_WhenBypassed_DoesNotCallOrchestrator()
    {
        var job = CreateColorsJob(bypass: true);

        await job.Execute(_contextMock);

        await _orchestratorMock.DidNotReceive().SynchronizeColorsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SyncColorsJob_WhenOrchestratorThrows_ThrowsJobExecutionException()
    {
        _orchestratorMock.SynchronizeColorsAsync(Arg.Any<CancellationToken>())
                         .ThrowsAsync(new HttpRequestException("Network error"));
        var job = CreateColorsJob(bypass: false);

        await Assert.ThrowsAsync<JobExecutionException>(() => job.Execute(_contextMock));
    }

    // ── SyncSizesJob ─────────────────────────────────────────────────────────

    [Fact]
    public async Task SyncSizesJob_Execute_DelegatesToOrchestrator()
    {
        var job = CreateSizesJob(bypass: false);

        await job.Execute(_contextMock);

        await _orchestratorMock.Received(1).SynchronizeSizesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SyncSizesJob_WhenBypassed_DoesNotCallOrchestrator()
    {
        var job = CreateSizesJob(bypass: true);

        await job.Execute(_contextMock);

        await _orchestratorMock.DidNotReceive().SynchronizeSizesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SyncSizesJob_WhenOrchestratorThrows_ThrowsJobExecutionException()
    {
        _orchestratorMock.SynchronizeSizesAsync(Arg.Any<CancellationToken>())
                         .ThrowsAsync(new InvalidOperationException("Unexpected failure"));
        var job = CreateSizesJob(bypass: false);

        await Assert.ThrowsAsync<JobExecutionException>(() => job.Execute(_contextMock));
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private IConfiguration BuildConfig(string bypassKey, bool bypass) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [bypassKey] = bypass.ToString() })
            .Build();

    private SyncProductsJob CreateProductsJob(bool bypass) =>
        new(_orchestratorMock, NullLogger<SyncProductsJob>.Instance,
            BuildConfig("Jobs:BypassSyncProducts", bypass));

    private SyncInventoryJob CreateInventoryJob(bool bypass) =>
        new(_orchestratorMock, NullLogger<SyncInventoryJob>.Instance,
            BuildConfig("Jobs:BypassSyncInventory", bypass));

    private SyncColorsJob CreateColorsJob(bool bypass) =>
        new(_orchestratorMock, NullLogger<SyncColorsJob>.Instance,
            BuildConfig("Jobs:BypassSyncColors", bypass));

    private SyncSizesJob CreateSizesJob(bool bypass) =>
        new(_orchestratorMock, NullLogger<SyncSizesJob>.Instance,
            BuildConfig("Jobs:BypassSyncSizes", bypass));
}
