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

// Hangfire mặc định chạy cron theo UTC nếu không set TimeZone — Cron.Daily(6) sẽ chạy lúc
// 6h UTC (= 13h giờ VN), không phải 6h sáng giờ VN như mong đợi. Phải truyền RecurringJobOptions
// với TimeZoneInfo Asia/Ho_Chi_Minh để job chạy đúng giờ địa phương.
var vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
var jobOptions = new RecurringJobOptions { TimeZone = vnTimeZone };

jobManager.AddOrUpdate<UpdateContractKolResultJob>(
    "update-contract-kol-result",
    job => job.Execute(),
    Cron.Daily(10),
    jobOptions);

jobManager.AddOrUpdate<UpdateKolSourceJob>(
    "update-kol-source",
    job => job.Execute(),
    Cron.Daily(4),
    jobOptions);

Console.WriteLine("MqScheduler đang chạy. Ctrl+C để dừng.");
await host.WaitForShutdownAsync();
