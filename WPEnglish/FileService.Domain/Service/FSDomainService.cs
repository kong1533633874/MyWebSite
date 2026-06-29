using Commons;
using FileService.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileService.Domain.Service
{
	public class FSDomainService
	{
		private readonly IFSRepository _fSRepository;
		private readonly IStorageClient _storageClient;
		private readonly ILogger<FSDomainService> _logger;

		public FSDomainService(IFSRepository fSRepository,IStorageClient storageClient,ILogger<FSDomainService> logger)
		{
			this._fSRepository = fSRepository;
			this._storageClient = storageClient;
			this._logger = logger;
		}

		public async Task<AudioFile> UploadAsync(string fileName,Stream content)
		{
			string hash = HashHelper.ComputeSha256Hash(content);
			long fileSize = content.Length;

			DateTime today = DateTime.Today;
			string path = $"{today.Year}/{today.Month}/{today.Day}/{hash}/{fileName}";

			var oldAudioFile = await _fSRepository.FindFileAsync(fileSize,hash);
			if (oldAudioFile != null)
			{
				_logger.LogInformation("文件已存在，跳过上传：{FileName}，大小：{FileSize}", fileName, fileSize);
				return oldAudioFile;
			}
			content.Position = 0;
			Uri url = await _storageClient.SaveAsync(path, content);
			Guid guid = Guid.NewGuid();
			_logger.LogInformation("文件上传成功：{FileName}，大小：{FileSize}，路径：{Path}", fileName, fileSize, path);
			return AudioFile.Create(guid, fileSize, fileName, hash,url);
		}
	}
}
