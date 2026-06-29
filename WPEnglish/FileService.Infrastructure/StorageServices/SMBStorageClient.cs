using FileService.Domain.Service;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileService.Infrastructure.StorageServices
{
	public class SMBStorageClient : IStorageClient
	{
		private readonly IOptionsSnapshot<SMBStorageOptions> _baseUrl;
		private readonly ILogger<SMBStorageClient> _logger;

		public SMBStorageClient(IOptionsSnapshot<SMBStorageOptions> BaseUrl, ILogger<SMBStorageClient> logger)
		{
			_baseUrl = BaseUrl;
			_logger = logger;
		}
		public async Task<Uri> SaveAsync(string path, Stream content)
		{
			if (path.StartsWith('/'))
			{
				throw new ArgumentException("key should not start with /", nameof(path));
			}
			string baseUrl = _baseUrl.Value.BaseUrl;
			string fullPath = Path.Combine(baseUrl, path);
			string? fullDir = Path.GetDirectoryName(fullPath);
			if (!Directory.Exists(fullDir))
			{
				Directory.CreateDirectory(fullDir);
			}
			if(File.Exists(fullPath))
			{
				File.Delete(fullPath);
			}
			using Stream writeStream = File.OpenWrite(fullPath);
			await content.CopyToAsync(writeStream);
			_logger.LogInformation("文件保存成功：{Path}", path);

			return new Uri($"/upload/{path}",UriKind.Relative);
		}
	}
}
