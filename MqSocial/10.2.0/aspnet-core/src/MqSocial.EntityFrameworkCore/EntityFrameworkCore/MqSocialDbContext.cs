using Abp.Zero.EntityFrameworkCore;
using MqSocial.Authorization.Roles;
using MqSocial.Authorization.Users;
using MqSocial.Campaigns;
using MqSocial.Companies;
using MqSocial.Contracts;
using MqSocial.Kols;
using MqSocial.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace MqSocial.EntityFrameworkCore;

public class MqSocialDbContext : AbpZeroDbContext<Tenant, Role, User, MqSocialDbContext>
{
    /* Define a DbSet for each entity of the application */

    public DbSet<Campaign> Campaigns { get; set; }

    public DbSet<Kol> Kols { get; set; }

    public DbSet<Contract> Contracts { get; set; }

    public DbSet<Company> Companies { get; set; }

    public DbSet<ContractKol> ContractKols { get; set; }

    public MqSocialDbContext(DbContextOptions<MqSocialDbContext> options)
        : base(options)
    {
    }
}
