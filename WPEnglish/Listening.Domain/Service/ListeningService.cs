using Commons;
using DomainCommons;
using Listening.Domain.Entities;
using Microsoft.Extensions.Logging;

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

		public async Task<DomainResult<Category>> AddCategoryAsync(string title,Uri url)
		{
			DomainResult<Category> result = new DomainResult<Category>();

			int seq = await repository.GetMaxSequenceNumberAsync();
			var category = Category.Create(seq + 1, title, url);
			if (category == null)
			{
				return DomainResult<Category>.Fail("创建分类失败", 500);
			}
			return DomainResult<Category>.Ok(category);

		}

		public async Task<DomainResult<Category>> DeleteCategoryAsync(Guid id)
		{
			var category = await repository.GetCategoryByIdAsync(id);
			if (category == null)
			{
				return DomainResult<Category>.Fail($"未找到Id:{id}的Category");
			}
			category.SoftDelete();
			return DomainResult<Category>.Ok(category);
		}

		public async Task<DomainResult<Category>> UpdateCategoryAsync(Guid id, string title, Uri coverUrl)
		{
			var category = await repository.GetCategoryByIdAsync(id);
			if (category == null)
			{
				return new DomainResult<Category> 
				{ 
					Success = false, Message = $"未找到Id:{id}的Category" ,StatusCode = 400 
				};
			}
			category.ChangeTitle(title).ChangeCoverUrl(coverUrl).NotifyModified();
			return new DomainResult<Category>
			{
				Success = true,
				Data = category,
				StatusCode = 200
			};
		}

		public async Task<DomainResult<Category[]>> SortCategoriesAsync(Guid[] sortedCategoryIds)
		{
			var categories = await repository.GetAllCategoriesAsync();
			var idsInDB = categories.Where(x => !x.IsDeleted).Select(x => x.Id);
			if (!idsInDB.SequenceIgnoredEqual(sortedCategoryIds))
			{
				logger.LogWarning("分类排序失败：提交的ID与数据库中的分类ID不匹配");
				return new DomainResult<Category[]>
				{
					Success = false,
					StatusCode = 400,
					Message = "提交的待排序Id中必须是所有的分类Id"
				};
			}
			int seqNumber = 0;
			foreach (var id in sortedCategoryIds)
			{
				var category = await repository.GetCategoryByIdAsync(id);
				if (category == null)
				{
					logger.LogWarning("分类排序失败：未找到分类 {CategoryId}", id);
					return new DomainResult<Category[]>
					{
						Success = false,
						StatusCode = 400,
						Message = $"Unable to find category {id}"
					};
				}
				category.ChangeSequenceNumber(seqNumber);
				seqNumber++;
			}
			return new DomainResult<Category[]>
			{
				Success = true,
				StatusCode = 200,
				Data = categories
			};
		}

		public async Task<Album> AddAlbumAsync(string title,Guid categoryId)
		{
			var seqNumber = await repository.GetMaxSequenceNumberOfAlbumAsync(categoryId);
			return Album.Create(title, categoryId, seqNumber + 1);
		}

		public async Task<DomainResult<Album>> UpdateAlbumAsync(Guid id, string title)
		{
			var album = await repository.GetAlbumByIdAsync(id);
			if (album == null)
			{
				return new DomainResult<Album>
				{
					Success = false,
					StatusCode = 400,
					Message = $"id:{id}的Ablum不存在"
				};
			}
			album.ChangeTitle(title).NotifyModified();
			return new DomainResult<Album>
			{
				Success = true,
				Data = album,
				StatusCode = 200,
			};
		}

		public async Task<DomainResult<Album>> DeleteAlbumAsync(Guid id)
		{
			var album = await repository.GetAlbumByIdAsync(id);
			if (album == null)
			{
				return new DomainResult<Album>
				{
					Success = false,
					StatusCode = 400,
					Message = $"不存在id为:{id}的Album"
				};
			}
			album.SoftDelete();
			return new DomainResult<Album>
			{
				Success = true,
				Data = album,
				StatusCode = 200
			};
		}
		public async Task<DomainResult<Album[]>> SortAlbumsAsync(Guid[] sortedAlbumIds,Guid categoryId)
		{
			var albums = await repository.GetAllAlbumsByCategoryIdAsync(categoryId);
			var albumsIds = albums.Select(x => x.Id);
			if (!albumsIds.SequenceIgnoredEqual(sortedAlbumIds))
			{
				logger.LogWarning("专辑排序失败：提交的ID与分类 {CategoryId} 下的专辑ID不匹配", categoryId);
				return new DomainResult<Album[]>
				{
					Success = false,
					StatusCode = 400,
					Message = $"提交的待排序Id中必须是id为{categoryId}的分类中的所有的Id"
				};
			}
			int seqNumber = 0;
			foreach(var id in sortedAlbumIds)
			{
				var album = await repository.GetAlbumByIdAsync(id);
				if(album == null)
				{
					logger.LogWarning("专辑排序失败：未找到专辑 {AlbumId}", id);
					return new DomainResult<Album[]>
					{
						Success = false,
						StatusCode = 400,
						Message = $"Unable to find album {id}"
					};
				}
				album.ChangeSequenceNumber(seqNumber);
				seqNumber++;
			}
			return new DomainResult<Album[]>
			{
				Success = true,
				StatusCode = 200,
				Data = albums
			};
		}

		public async Task<Episode> AddEpisodeAsync(string title, string subtitleType, string subtitle,Guid albumId,Uri audioUrl, double durationInSecond)
		{
			int seqMax = await repository.GetMaxSequenceNumberOfEpisodeAsync(albumId);

			var builder = new Episode.Builder();
			builder.Id(Guid.NewGuid()).Title(title).SequenceNumber(seqMax+1).Subtitle(subtitle).SubtitleType(subtitleType)
				.AlbumId(albumId).AudioUrl(audioUrl).DurationInSecond(durationInSecond);

			return builder.Build();
		}

		public async Task<DomainResult<Episode>> UpdateEpisodeAsync(Guid id, string title, string subtitleType, string subtitle)
		{
			var episode = await repository.GetEpisodeByIdAsync(id);
			if (episode == null)
			{
				return DomainResult<Episode>.Fail($"没有id:{id}的episode");
			}
			episode.ChangeSubtitle(subtitleType,subtitle).ChangeTitle(title).NotifyModified();
			return DomainResult<Episode>.Ok(episode);
		}
		public async Task<DomainResult<Episode>> DeleteEpisodeAsync(Guid id)
		{
			var episode = await repository.GetEpisodeByIdAsync(id);
			if(episode == null)
			{
				return DomainResult<Episode>.Fail($"没有id:{id}的episode");
			}
			episode.SoftDelete();
			return DomainResult<Episode>.Ok(episode);
		}

		public async Task<DomainResult<Episode[]>> SortEpisodesAsync(Guid[] SortedEpisodesIds, Guid albumId)
		{
			var episodes = await repository.GetAllEpisodesByAlbumIdAsync(albumId);
			var episodesIds = episodes.Select(e => e.Id);
			if (!episodesIds.SequenceIgnoredEqual(SortedEpisodesIds))
			{
				logger.LogWarning("剧集排序失败：提交的ID与专辑 {AlbumId} 下的剧集ID不匹配", albumId);
				return new DomainResult<Episode[]>
				{
					Success = false,
					StatusCode = 400,
					Message = $"提交的待排序Id中必须是id为{albumId}专辑中的所有的Id"
				};
			}
			int seqNumber = 0;
			foreach( var episodeId in SortedEpisodesIds)
			{
				var episode = await repository.GetEpisodeByIdAsync(episodeId);
				if (episode == null)
				{
					logger.LogWarning("剧集排序失败：未找到剧集 {EpisodeId}", episodeId);
					return new DomainResult<Episode[]>
					{
						Success = false,
						StatusCode = 400,
						Message = $"Unable to find Episode {episodeId}"
					};
				}
				episode.ChangeSequenceNumber(seqNumber);
				seqNumber++;
			}
			return new DomainResult<Episode[]>
			{
				Success = true,
				StatusCode = 200,
				Data = episodes
			};
		}
 	}
}
