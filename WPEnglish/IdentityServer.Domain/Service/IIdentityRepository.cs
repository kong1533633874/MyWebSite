using IdentityServer.Domain.Entity;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdentityServer.Domain.Service
{
	public interface IIdentityRepository
	{
		Task<IdentityResult> RemoveUserAsync(Guid userId);
		Task<SignInResult> CheckForSignInAsync(User user, string password, bool lockoutOnFailure);
		Task<IdentityResult> ChangePassword(Guid userId, string oldPassword, string newPassword);
		Task<IdentityResult> AddToRoleAsync(User user, string role);
		Task<IdentityResult> AddUserAsync(string username, string password);
		Task<IdentityResult> AddRoleAsync(string role);
	}
}
