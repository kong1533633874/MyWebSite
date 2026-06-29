using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Listening.Domain.Entities
{
	public class Album
	{
		public Guid Id { get; private set; }
		public string Title { get; private set; }
		public bool IsVisible { get; private set; }
		public int SequenceNumber { get; private set; }
		public Guid CategoryId { get; private set; }

		public DateTime CreatedTime { get; private set; }
		public bool IsDeleted { get; private set; }
		public DateTime? DeletionTime { get; private set; }
		public DateTime? LastModificationTime { get; private set; }

		public static Album Create(string title,Guid categoryId, int seqNumber)
		{
			Album album = new Album()
			{
				Id = Guid.NewGuid(),
				Title = title,
				CategoryId = categoryId,
				CreatedTime = DateTime.UtcNow,
				IsVisible = true,
				SequenceNumber = seqNumber
			};
			return album;
		}

		public Album ChangeTitle(string title)
		{
			this.Title = title;
			return this;
		}
		public Album ChangeSequenceNumber(int seq)
		{
			this.SequenceNumber = seq;
			return this;
		}
		public Album Show()
		{
			this.IsVisible = true;
			return this;
		}
		public Album Hide()
		{
			this.IsVisible = false;
			return this;
		}

		public void SoftDelete()
		{
			this.IsDeleted = true;
			this.DeletionTime = DateTime.UtcNow;
		}

		public void NotifyModified()
		{
			this.LastModificationTime = DateTime.UtcNow;
		}

	}
}
