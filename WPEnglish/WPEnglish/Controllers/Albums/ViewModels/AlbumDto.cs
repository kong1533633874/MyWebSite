using Listening.Domain.Entities;
using System.Runtime.InteropServices;

namespace Listening.Main.WebApi.Controllers.Albums.ViewModels
{
	public class AlbumDto
	{
		public Guid Id { get; set; }
		public string Title { get; set; }
		public Guid CategoryId { get; set; }

		public static AlbumDto? Create(Album? album)
		{
			if (album == null) return null;
			return new AlbumDto { Id = album.Id, Title = album.Title, CategoryId = album.CategoryId };
		}
		
		public static AlbumDto[]? Create(Album[] albums)
		{
			return albums.Select(s => Create(s)!).ToArray();
		}
	}
}
