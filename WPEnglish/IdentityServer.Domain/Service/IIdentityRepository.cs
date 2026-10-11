using IdentityServer.Domain.Entity;
using Microsoft.AspNetCore.Identity;

namespace IdentityServer.Domain.Service
{
	public interface IIdentityRepository
	{
		Task<IdentityResult> AddToRoleAsync(User user, string role);
		Task<IdentityResult> AddUserAsync(string username, string password);
		Task<IdentityResult> AddRoleAsync(string role);
		Task<IdentityResult> RemoveUserAsync(Guid userId);
		Task<IdentityResult> RemoveRoleAsync(string role);
		Task<IdentityResult> ChangePassword(Guid userId, string oldPassword, string newPassword);
		Task<SignInResult> CheckForSignInAsync(User user, string password, bool lockoutOnFailure);
	}
}
