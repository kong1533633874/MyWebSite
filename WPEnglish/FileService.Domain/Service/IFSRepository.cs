using FileService.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileService.Domain.Service
{
	public interface IFSRepository
	{
		public Task<AudioFile?> FindFileAsync(long fileSize,string sha256Hash);
	}
}
