using Microsoft.EntityFrameworkCore;
using System.Data.Common;

namespace MqSocial.EntityFrameworkCore;

public static class MqSocialDbContextConfigurer
{
    public static void Configure(DbContextOptionsBuilder<MqSocialDbContext> builder, string connectionString)
    {
        builder.UseNpgsql(connectionString);
    }

    public static void Configure(DbContextOptionsBuilder<MqSocialDbContext> builder, DbConnection connection)
    {
        builder.UseNpgsql(connection);
    }
}
