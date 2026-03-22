using ErpEcommerceIntegration.Infra.IoC;
using ErpEcommerceIntegration.Workers.Jobs;
using Quartz;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((ctx, services) =>
    {
        services.AddApplicationServices(ctx.Configuration);

        // Register Quartz jobs (defined in this project) and configure scheduling
        services.AddTransient<SyncProductsJob>();
        services.AddTransient<SyncInventoryJob>();
        services.AddTransient<SyncColorsJob>();
        services.AddTransient<SyncSizesJob>();

        services.AddQuartz(q =>
        {
            void Schedule<TJob>(string configKey, string defaultCron) where TJob : IJob
            {
                var key = new JobKey(typeof(TJob).Name);
                var cron = ctx.Configuration[configKey] ?? defaultCron;
                q.AddJob<TJob>(o => o.WithIdentity(key).DisallowConcurrentExecution());
                q.AddTrigger(o => o.ForJob(key).WithCronSchedule(cron));
            }

            Schedule<SyncProductsJob> ("Jobs:ProductsCron",  "0 0 0/2 * * ?");  // every 2 hours
            Schedule<SyncInventoryJob>("Jobs:InventoryCron", "0 0/5 * * * ?");   // every 5 minutes
            Schedule<SyncColorsJob>   ("Jobs:ColorsCron",    "0 0 0/6 * * ?");   // every 6 hours
            Schedule<SyncSizesJob>    ("Jobs:SizesCron",     "0 0 0/6 * * ?");   // every 6 hours
        });
        services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
    })
    .Build();

await host.RunAsync();
