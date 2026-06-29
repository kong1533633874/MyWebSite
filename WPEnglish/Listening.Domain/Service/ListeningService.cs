using Commons;
using Commons.Exceptions;
using Listening.Domain.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Listening.Domain.Service
{
	public class ListeningService
	{
		private readonly IListeningRepository repository;
		private readonly ILogger<ListeningService> logger;

		public ListeningService(IListeningRepository repository, ILogger<ListeningService> logger)
		{
			this.repository = repository;
			this.logger = logger;
		}

		public async Task<Category> AddCategoryAsync(string title,Uri url)
		{
			int seq = await repository.GetMaxSequenceNumberAsync();
			return Category.Create(seq + 1,title, url);
		}

		public async Task SortCategoriesAsync(Guid[] sortedCategoryIds)
		{
			var categories = await repository.GetAllCategoriesAsync();
			var idsInDB = categories.Where(x => !x.IsDeleted).Select(x => x.Id);
			if (!idsInDB.SequenceIgnoredEqual(sortedCategoryIds))
			{
				logger.LogWarning("分类排序失败：提交的ID与数据库中的分类ID不匹配");
				throw new BusinessException("提交的待排序Id中必须是所有的分类Id");
			}
			int seqNumber = 0;
			foreach (var id in sortedCategoryIds)
			{
				var category = await repository.GetCategoryByIdAsync(id);
				if (category == null)
				{
					logger.LogWarning("分类排序失败：未找到分类 {CategoryId}", id);
					throw new NotFoundException($"Unable to find category {id}");
				}
				category.ChangeSequenceNumber(seqNumber);
				seqNumber++;
			}
		}

		public async Task<Album> AddAlbumAsync(string title,Guid categoryId)
		{
			var seqNumber = await repository.GetMaxSequenceNumberOfAlbumAsync(categoryId);
			return Album.Create(title, categoryId, seqNumber + 1);
		}

		public async Task SortAlbumsAsync(Guid[] sortedAlbumIds,Guid categoryId)
		{
			var albums = await repository.GetAllAlbumsByCategoryIdAsync(categoryId);
			var albumsIds = albums.Select(x => x.Id);
			if (!albumsIds.SequenceIgnoredEqual(sortedAlbumIds))
			{
				logger.LogWarning("专辑排序失败：提交的ID与分类 {CategoryId} 下的专辑ID不匹配", categoryId);
				throw new Exception($"提交的待排序Id中必须是id为{categoryId}的分类中的所有的Id");
			}
			int seqNumber = 0;
			foreach(var id in sortedAlbumIds)
			{
				var album = await repository.GetAlbumByIdAsync(id);
				if(album == null)
				{
					logger.LogWarning("专辑排序失败：未找到专辑 {AlbumId}", id);
					throw new Exception($"Unable to find album {id}");
				}
				album.ChangeSequenceNumber(seqNumber);
				seqNumber++;
			}
		}

		public async Task<Episode> AddEpisodeAsync(string title, string subtitleType, string subtitle,Guid albumId,Uri audioUrl, double durationInSecond)
		{
			int seqMax = await repository.GetMaxSequenceNumberOfEpisodeAsync(albumId);

			var builder = new Episode.Builder();
			builder.Id(Guid.NewGuid()).Title(title).SequenceNumber(seqMax+1).Subtitle(subtitle).SubtitleType(subtitleType)
				.AlbumId(albumId).AudioUrl(audioUrl).DurationInSecond(durationInSecond);

			return builder.Build();
		}

		public async Task SortEpisodesAsync(Guid[] SortedEpisodesIds, Guid albumId)
		{
			var episodes = await repository.GetAllEpisodesByAlbumIdAsync(albumId);
			var episodesIds = episodes.Select(e => e.Id);
			if (!episodesIds.SequenceIgnoredEqual(SortedEpisodesIds))
			{
				logger.LogWarning("剧集排序失败：提交的ID与专辑 {AlbumId} 下的剧集ID不匹配", albumId);
				throw new Exception($"提交的待排序Id中必须是id为{albumId}专辑中的所有的Id");
			}
			int seqNumber = 0;
			foreach( var episodeId in SortedEpisodesIds)
			{
				var episode = await repository.GetEpisodeByIdAsync(episodeId);
				if (episode == null)
				{
					logger.LogWarning("剧集排序失败：未找到剧集 {EpisodeId}", episodeId);
					throw new Exception($"Unable to find Episode {episodeId}");
				}
				episode.ChangeSequenceNumber(seqNumber);
				seqNumber++;
			}
		}
 	}
}
