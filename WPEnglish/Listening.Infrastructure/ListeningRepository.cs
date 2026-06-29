using Listening.Domain.Entities;
using Listening.Domain.Service;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Listening.Infrastructure
{
	public class ListeningRepository : IListeningRepository
	{
		private readonly ListengingDbContext listengingDbContext;

		public ListeningRepository(ListengingDbContext listengingDbContext)
		{
			this.listengingDbContext = listengingDbContext;
		}

		public Task<Category[]> GetAllCategoriesAsync()
		{
			return listengingDbContext.categories.OrderBy(s=>s.SequenceNumber).ToArrayAsync();
		}

		public async Task<Category?> GetCategoryByIdAsync(Guid id)
		{
			return  await listengingDbContext.categories.FindAsync(id);
		}

		public async Task<int> GetMaxSequenceNumberAsync()
		{
			int? seq = await listengingDbContext.categories.MaxAsync(c => (int?)c.SequenceNumber);
			return seq ?? 0;
		}

		public Task<Album[]> GetAllAlbumsByCategoryIdAsync(Guid categoryId)
		{
			return listengingDbContext.albums.OrderBy(s => s.SequenceNumber).Where(s=>s.CategoryId == categoryId).ToArrayAsync();
		}

		public async Task<Album?> GetAlbumByIdAsync(Guid id)
		{
			return await listengingDbContext.albums.FindAsync(id);
		}

		public async Task<int> GetMaxSequenceNumberOfAlbumAsync(Guid categoryId)
		{
			int? seq = await listengingDbContext.albums.Where(s=>s.CategoryId == categoryId).MaxAsync(s => (int?)s.SequenceNumber);
			return seq ?? 0;
		}

		public async Task<int> GetMaxSequenceNumberOfEpisodeAsync(Guid albumId)
		{
			int? seq = await listengingDbContext.episodes.Where(s => s.AlbumId == albumId).MaxAsync(s => (int?)s.SequenceNumber);
			return seq ?? 0;
		}

		public async Task<Episode?> GetEpisodeByIdAsync(Guid id)
		{
			return await listengingDbContext.episodes.FindAsync(id);
		}

		public async Task<Episode[]> GetAllEpisodesByAlbumIdAsync(Guid albumId)
		{
			return await listengingDbContext.episodes.Where(s => s.AlbumId == albumId).OrderBy(s => s.SequenceNumber).ToArrayAsync();
		}

	}
}
