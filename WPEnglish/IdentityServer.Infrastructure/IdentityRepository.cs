using IdentityServer.Domain.Entity;
using IdentityServer.Domain.Service;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdentityServer.Infrastructure
{
	public class IdentityRepository : IIdentityRepository
	{
		private readonly IdUserManager _userManager;
		private readonly RoleManager<Role> _roleManager;
		private readonly ILogger<IdUserManager> _logger;

		public IdentityRepository(IdUserManager userManager, RoleManager<Role> roleManager,ILogger<IdUserManager> logger)
		{
			this._userManager = userManager;
			this._roleManager = roleManager;
			this._logger = logger;
		}

		public async Task<IdentityResult> AddUserAsync(string username, string password)
		{
			User? userData = await _userManager.FindByNameAsync(username);
			if (userData != null)
			{
				var error = new IdentityError()
				{
					Code = "UserName Invalid",
					Description = "用户名已存在"
				};
				_logger.LogWarning("用户名{UserName}已存在",username);
				return IdentityResult.Failed(error);
			}
			else
			{
				var user = new User(username);

				var createResult = await _userManager.CreateAsync(user, password);
				if (!createResult.Succeeded)
				{
					_logger.LogWarning("创建用户失败{User}", user);
					return createResult;
				}

				var addRoleResult = await AddToRoleAsync(user, "admin");
				if (!addRoleResult.Succeeded)
				{
					_logger.LogWarning("添加用户权限admin失败");
					return addRoleResult;
				}
				return IdentityResult.Success;
			}
		}

		public async Task<IdentityResult> AddToRoleAsync(User user, string role)
		{
			var flag = await _roleManager.RoleExistsAsync(role);
			if (!flag)
			{
				IdentityError identityError = new IdentityError() { Description = $"不存在{role}管理权限" };
				return IdentityResult.Failed(identityError);
			}

			return await _userManager.AddToRoleAsync(user, role);
		}

		public async Task<IdentityResult> AddRoleAsync(string roleName)
		{
			Role? role = await _roleManager.FindByNameAsync(roleName);
			if (role!=null)
			{
				IdentityError identityError = new IdentityError() { Code = "roleName have existed", Description= $"权限名:{roleName}已经存在" };
				return IdentityResult.Failed(identityError);
			}
			Role newRole = new Role { Name = roleName };
			return await _roleManager.CreateAsync(newRole);
		}

		public async Task<IdentityResult> ChangePassword(Guid userId, string oldPassword, string newPassword)
		{
			var user = await _userManager.FindByIdAsync(userId.ToString());
			if (user == null)
			{
				_logger.LogWarning("修改密码失败：未找到用户 {UserId}", userId);
				IdentityError identityError = new IdentityError()
				{
					Code = "UserId Invalid",
					Description = "未找到用户"
				};
				return IdentityResult.Failed(identityError);
			}
			if (newPassword.Length < 6)
			{
				_logger.LogWarning("修改密码失败：密码长度不足 {UserId}", userId);
				IdentityError identityError = new IdentityError() 
				{ 
					Code = "Password Invalid", 
					Description = "密码长度不能少于6" 
				};
				return IdentityResult.Failed(identityError);
			}

			var result = await _userManager.ChangePasswordAsync(user, oldPassword, newPassword);
			if (result.Succeeded)
			{
				_logger.LogInformation("用户 {UserName} 修改密码成功", user.UserName);
			}
			else
			{
				_logger.LogWarning("用户 {UserName} 修改密码失败", user.UserName);
			}
			return result;

		}

		public async Task<SignInResult> CheckForSignInAsync(User user, string password, bool lockoutOnFailure)
		{
			if(await _userManager.IsLockedOutAsync(user))
			{
				_logger.LogWarning("用户 {UserName} 账号已被锁定", user.UserName);
				return SignInResult.LockedOut;
			}

			var result = await _userManager.CheckPasswordAsync(user, password);
			if (result)
			{
				return SignInResult.Success;
			}
			else
			{
				if (lockoutOnFailure) 
				{
					var access = await _userManager.AccessFailedAsync(user);
					if (!access.Succeeded)
					{
						_logger.LogError("用户 {UserName} 记录登录失败次数失败", user.UserName);
						throw new ApplicationException("AccessFailed failed");
					}
				}
				return SignInResult.Failed;
			}
		}

		public async Task<IdentityResult> RemoveUserAsync(Guid userId)
		{
			var user = await _userManager.FindByIdAsync(userId.ToString());
			if (user == null)
			{
				_logger.LogWarning("删除用户失败：未找到用户 {UserId}", userId);
				IdentityError identityError = new IdentityError()
				{
					Code = "UserId Invalid",
					Description = "未找到用户"
				};
				return IdentityResult.Failed(identityError);
			}

			var userLoginStore = _userManager.UserLoginStore;
			var noneCT = default(CancellationToken);
			var logins  =  await _userManager.GetLoginsAsync(user);
			foreach (var login in logins)
			{
				await userLoginStore.RemoveLoginAsync(user,login.LoginProvider,login.ProviderKey,noneCT);
			}

			user.SoftDelete();
			var result = await _userManager.UpdateAsync(user);
			if (result.Succeeded)
			{
				_logger.LogWarning("用户 {UserName} 已被软删除", user.UserName);
			}
			return result;
		}

	}
}
