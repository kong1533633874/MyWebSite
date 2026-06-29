using FileService.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileService.Infrastructure
{
	public class FSDbContext:DbContext
	{
		public DbSet<AudioFile> AudioFiles { get; set; }
		public FSDbContext(DbContextOptions<FSDbContext> dbContextOptions) : base(dbContextOptions) 
		{
			
		}
		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);
			modelBuilder.ApplyConfigurationsFromAssembly(this.GetType().Assembly);

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
