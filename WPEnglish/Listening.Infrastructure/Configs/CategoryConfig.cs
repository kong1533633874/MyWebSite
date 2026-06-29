using Listening.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Listening.Infrastructure.Configs
{
	public class CategoryConfig : IEntityTypeConfiguration<Category>
	{
		public void Configure(EntityTypeBuilder<Category> builder)
		{
			builder.ToTable("T_Categories");
			builder.HasKey(c => c.Id).IsClustered(false);
			builder.Property(s => s.CoverUrl).IsRequired(false).IsUnicode();
			builder.Property(s => s.Title).IsRequired().HasMaxLength(200).IsUnicode();
		}
	}
}
