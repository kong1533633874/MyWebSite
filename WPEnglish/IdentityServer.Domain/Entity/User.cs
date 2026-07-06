using DomainCommons.Models;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdentityServer.Domain.Entity
{
	public class User:IdentityUser<Guid>
	{
		public DateTime CreationTime { get; init; }

		public DateTime? DeletionTime { get; private set; }

		public bool IsDeleted { get; private set; }

		public User(string userName) : base(userName)
		{
			Id = Guid.NewGuid();
			CreationTime = DateTime.UtcNow;
		}
		public void SoftDelete()
		{
			this.IsDeleted = true;
			this.DeletionTime = DateTime.UtcNow;
		}
	}
}
