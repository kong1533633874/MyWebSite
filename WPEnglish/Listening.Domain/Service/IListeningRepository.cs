using Listening.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Listening.Domain.Service
{
	public interface IListeningRepository
	{
		//Category
		public Task<Category?> GetCategoryByIdAsync(Guid id);
		public Task<Category[]> GetAllCategoriesAsync();
		public Task<int> GetMaxSequenceNumberAsync();
		//Album
		public Task<Album?> GetAlbumByIdAsync(Guid id);
		public Task<Album[]> GetAllAlbumsByCategoryIdAsync(Guid categoryId);
		public Task<int> GetMaxSequenceNumberOfAlbumAsync(Guid categoryId);
		//Episode
		public Task<Episode?> GetEpisodeByIdAsync(Guid id);
		public Task<Episode[]> GetAllEpisodesByAlbumIdAsync(Guid albumId);
		public Task<int> GetMaxSequenceNumberOfEpisodeAsync(Guid albumId);
	}
}
