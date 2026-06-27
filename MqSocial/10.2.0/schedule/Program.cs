using Hangfire;
using Hangfire.SqlServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        var connectionString = context.Configuration.GetConnectionString("Default")!;

        services.AddSingleton(new DbConnectionFactory(connectionString));

        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(connectionString,
                new SqlServerStorageOptions
                {
                    QueuePollInterval = TimeSpan.Zero,
                    UseRecommendedIsolationLevel = true,
                    DisableGlobalLocks = true
                }));

        services.AddHangfireServer();

        services.AddTransient<UpdateContractKolResultJob>();
    })
    .Build();

await host.StartAsync();

var jobManager = host.Services.GetRequiredService<IRecurringJobManager>();


jobManager.AddOrUpdate<UpdateContractKolResultJob>(
    "update-contract-kol-result",
    job => job.Execute(),
    Cron.Daily(6));

Console.WriteLine("MqScheduler đang chạy. Ctrl+C để dừng.");
await host.WaitForShutdownAsync();