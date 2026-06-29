using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileService.Domain.Service
{
	public interface IStorageClient
	{
		public Task<Uri> SaveAsync(string path,Stream content);
	}
}
