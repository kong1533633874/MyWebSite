using IdentityServer.Domain.Entity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace IdentityServer.Infrastructure
{
	public class IdDbContext : IdentityDbContext<User,Role,Guid>
	{
		public DbSet<User> _users {  get; set; }
		public DbSet<Role> _roles { get; set; }
		public IdDbContext(DbContextOptions<IdDbContext> opt): base(opt)
		{
			
		}
		protected override void OnModelCreating(ModelBuilder builder)
		{
			base.OnModelCreating(builder);
			builder.ApplyConfigurationsFromAssembly(this.GetType().Assembly);
			builder.Entity<User>().HasQueryFilter(s => !s.IsDeleted);
			builder.Entity<User>().HasIndex(u => u.NormalizedUserName)
			.HasFilter("[IsDeleted] = 0")
			.IsUnique();

			foreach (var entityType in builder.Model.GetEntityTypes())
			{
				foreach (var prop in entityType.GetProperties()
					.Where(p => p.ClrType == typeof(DateTime) || p.ClrType == typeof(DateTime?)))
				{
					prop.SetColumnType("datetime2(0)");  // 0位小数 = 精确到秒
				}
			}
		}

	}
}
