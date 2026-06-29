using Listening.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Listening.Infrastructure
{
	public class ListengingDbContext:DbContext
	{
		public DbSet<Category> categories {  get; set; }
		public DbSet<Album> albums { get; set; }
		public DbSet<Episode> episodes { get; set; }

		public ListengingDbContext(DbContextOptions<ListengingDbContext> option):base(option)
		{
			
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);
			modelBuilder.ApplyConfigurationsFromAssembly(this.GetType().Assembly);
			modelBuilder.Entity<Category>().HasQueryFilter(s => !s.IsDeleted);
			modelBuilder.Entity<Album>().HasQueryFilter(s => !s.IsDeleted);
			modelBuilder.Entity<Episode>().HasQueryFilter(s => !s.IsDeleted);

			foreach (var entityType in modelBuilder.Model.GetEntityTypes())
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
