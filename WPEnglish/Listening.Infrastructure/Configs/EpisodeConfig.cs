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
	public class EpisodeConfig : IEntityTypeConfiguration<Episode>
	{
		public void Configure(EntityTypeBuilder<Episode> builder)
		{
			builder.ToTable("T_Episodes");
			builder.HasKey(x => x.Id).IsClustered();
			builder.Property(s=>s.Title).IsRequired();
			builder.HasIndex(e => new { e.AlbumId, e.IsDeleted });
			builder.Property(e => e.AudioUrl).HasMaxLength(1000).IsUnicode().IsRequired();
			builder.Property(e => e.Subtitle).HasMaxLength(int.MaxValue).IsUnicode().IsRequired();
			builder.Property(e => e.SubtitleType).HasMaxLength(10).IsUnicode(false).IsRequired();
		}
	}
}
