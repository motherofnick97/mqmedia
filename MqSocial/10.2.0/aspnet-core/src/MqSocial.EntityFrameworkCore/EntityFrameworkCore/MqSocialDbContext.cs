using Abp.Zero.EntityFrameworkCore;
using MqSocial.Authorization.Roles;
using MqSocial.Authorization.Users;
using MqSocial.Campaigns;
using MqSocial.Kols;
using MqSocial.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace MqSocial.EntityFrameworkCore;

public class MqSocialDbContext : AbpZeroDbContext<Tenant, Role, User, MqSocialDbContext>
{
    /* Define a DbSet for each entity of the application */

    public DbSet<Campaign> Campaigns { get; set; }

    public DbSet<Kol> Kols { get; set; }

    public MqSocialDbContext(DbContextOptions<MqSocialDbContext> options)
        : base(options)
    {
    }
}
