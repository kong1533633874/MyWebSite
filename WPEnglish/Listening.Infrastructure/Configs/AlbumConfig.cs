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
	public class AlbumConfig : IEntityTypeConfiguration<Album>
	{
		public void Configure(EntityTypeBuilder<Album> builder)
		{
			builder.ToTable("T_Albums");
			builder.HasKey(x => x.Id).IsClustered();
			builder.Property(s =>s.Title).IsRequired().IsUnicode();
			builder.HasIndex(x => new {x.CategoryId, x.IsDeleted});
		}
	}
}
