using IdentityServer.Domain.Entity;
using JWT;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace IdentityServer.Domain.Service
{
	public class IdService
	{
		private readonly IIdentityRepository _repository;
		private readonly IOptions<JWTOptions> _jwtOptions;
		private readonly UserManager<User> _userManager;
		private readonly ILogger<IdService> _logger;

		public IdService(IIdentityRepository repository,IOptions<JWTOptions> jwtOptions,
			UserManager<User> userManager,
			ILogger<IdService> logger)
		{
			this._repository = repository;
			this._jwtOptions = jwtOptions;
			this._userManager = userManager;
			this._logger = logger;
		}

		private async Task<string> BuildToenkAsync(User user)
		{
			var roles = await _userManager.GetRolesAsync(user);
			List<Claim> claims = new List<Claim>();
			claims.Add(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
			foreach (var role in roles)
			{
				claims.Add(new Claim(ClaimTypes.Role, role));
			}
			return TokenService.BuildToken(claims,_jwtOptions.Value);
		}

		public async Task<(SignInResult Result,string? token)> LoginByUserNameAndPwdAsync(string userName, string password)
		{
			var user = await _userManager.FindByNameAsync(userName);
			if (user == null)
			{
				_logger.LogWarning("登录失败：用户名不存在 {UserName}",userName);
				return (SignInResult.Failed,null);
			}
			var checkResult = await _repository.CheckForSignInAsync(user, password, true);

			if (checkResult.Succeeded)
			{
				var token = await BuildToenkAsync(user);
				_logger.LogInformation("用户 {UserName} 登录成功", userName);
				return (SignInResult.Success, token);
			}

			if(checkResult.IsLockedOut)
			{
				_logger.LogWarning("用户 {UserName} 登录失败：账号已被锁定", userName);
				return (SignInResult.LockedOut,null);
			}
			else
			{
				_logger.LogWarning("用户 {UserName} 登录失败：密码错误", userName);
				return (checkResult, null);
			}
			
		}
	}
}
