using FileService.Domain.Service;
using FileService.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileService.Infrastructure
{
	public class FSRepository : IFSRepository
	{
		private readonly FSDbContext fSDbContext;

		public FSRepository(FSDbContext fSDbContext)
		{
			this.fSDbContext = fSDbContext;
		}
		public Task<AudioFile?> FindFileAsync(long fileSize, string sha256Hash)
		{
			return fSDbContext.AudioFiles.FirstOrDefaultAsync(s=>s.FileSize == fileSize && s.FileSHA256Hash == sha256Hash);
		}
	}
}
