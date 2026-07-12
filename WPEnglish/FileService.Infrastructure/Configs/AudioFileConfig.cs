using FileService.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FileService.Infrastructure.Configs
{
	public class AudioFileConfig : IEntityTypeConfiguration<AudioFile>
	{
		public void Configure(EntityTypeBuilder<AudioFile> builder)
		{
			builder.HasKey(e => e.Id);
			builder.Property(x => x.FileName).IsUnicode().HasMaxLength(1024);
			builder.Property(x => x.FileSHA256Hash).IsUnicode(false).HasMaxLength(64);
			builder.HasIndex(x => new { x.FileSHA256Hash,x.FileSize});
		}
	}
}
