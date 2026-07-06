using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Listening.Domain.Entities
{
	public class Category
	{
		public Guid Id { get; private set; }
		public int SequenceNumber { get; private set; }
		public string Title { get; private set; }
		public Uri CoverUrl { get; private set; }
		//commons
		public bool IsDeleted { get; private set; }
		public DateTime CreateTime { get; private set; }
		public DateTime? DeletionTime { get; private set; }
		public DateTime? LastModificationTime { get; private set; }

		public static Category Create(int sequenceNumber,string title,Uri coverUrl)
		{
			Category category = new Category()
			{
				Id = Guid.NewGuid(),
				SequenceNumber = sequenceNumber,
				Title = title,
				CoverUrl = coverUrl,
				CreateTime = DateTime.UtcNow,
			};
			return category;
		}
		public Category ChangeTitle(string title)
		{
			this.Title = title;
			return this;
		}

		public Category ChangeSequenceNumber(int value)
		{
			this.SequenceNumber = value;
			return this;
		}

		public Category ChangeCoverUrl(Uri url)
		{
			this.CoverUrl = url;
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
