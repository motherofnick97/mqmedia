using Hangfire;
using Hangfire.PostgreSql;
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
            .UsePostgreSqlStorage(o => o.UseNpgsqlConnection(connectionString)));

        services.AddHangfireServer();

        services.AddTransient<UpdateContractKolResultJob>();
        services.AddTransient<UpdateKolSourceJob>();
    })
    .Build();

await host.StartAsync();

var jobManager = host.Services.GetRequiredService<IRecurringJobManager>();

jobManager.AddOrUpdate<UpdateContractKolResultJob>(
    "update-contract-kol-result",
    job => job.Execute(),
    Cron.Daily(6));

jobManager.AddOrUpdate<UpdateKolSourceJob>(
    "update-kol-source",
    job => job.Execute(),
    Cron.Daily(4));

Console.WriteLine("MqScheduler đang chạy. Ctrl+C để dừng.");
await host.WaitForShutdownAsync();
