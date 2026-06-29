using IdentitServer.WebApi.Controllers.IdentityRequest;
using IdentityServer.Domain.Entity;
using IdentityServer.Domain.Service;
using JWT;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualBasic;
using System.Net;

namespace IdentitServer.WebApi.Controllers
{
	[Route("api/[controller]/[action]")]
	[ApiController]
	public class IdentityController : ControllerBase
	{
		private readonly IdService _idService;
		private readonly IIdentityRepository _repository;
		private readonly UserManager<User> _userManager;
		private readonly ILogger<IdentityController> _logger;

		public IdentityController(IdService idService,
			IIdentityRepository repository,
			UserManager<User> userManager,
			ILogger<IdentityController> logger)
		{
			_idService = idService;
			_repository = repository;
			_userManager = userManager;
			_logger = logger;
		}

		[HttpPost]
		public async Task<ActionResult<string?>> Login(LoginRequest loginRequest)
		{
			var (result, token) = await _idService.LoginByUserNameAndPwdAsync(loginRequest.Username, loginRequest.Password);
			if(result.Succeeded)
			{
				return token;
			}
			else if (result.IsLockedOut)
			{
				return StatusCode((int)HttpStatusCode.Locked, "账号已被锁定");
			}
			else
			{
				return BadRequest("登录失败");
			}
		}

		[HttpPost]
		[Authorize(Roles = "admin")]
		public async Task<ActionResult> Add(AddUserRequest addUserRequest)
		{
			var result = await _repository.AddUserAsync(addUserRequest.UserName, addUserRequest.Password);
			if (!result.Succeeded)
			{
				var errors = result.Errors.Select(s => $"code:{"发生错误"}, message:{s.Description}");
				string msg = string.Join("\n", errors);
				return BadRequest(msg);
			}
			_logger.LogInformation("用户创建成功：{UserName}", addUserRequest.UserName);
			return Ok("创建成功");
		}

		[HttpPost]
		[Authorize(Roles = "admin")]
		public async Task<ActionResult> AddRole(string roleName)
		{
			var result = await _repository.AddRoleAsync(roleName);
			if (!result.Succeeded)
			{
				var errors = result.Errors.Select(s => $"code:{s.Code}, message:{s.Description}");
				string msg = string.Join("\n", errors);
				return BadRequest(msg);
			}
			_logger.LogInformation("角色创建成功：{RoleName}", roleName);
			return Ok();
		}

		[HttpDelete]
		[Authorize(Roles = "admin")]
		public async Task<ActionResult> Delete(Guid Id)
		{
			var result = await _repository.RemoveUserAsync(Id);
			if (!result.Succeeded)
			{
				var errors = result.Errors.Select(s => $"code:{s.Code}, message:{s.Description}");
				string msg = string.Join("\n", errors);
				return BadRequest(msg);
			}
			_logger.LogWarning("用户已删除：{UserId}", Id);
			return Ok();
		}

		[HttpPut]
		[Authorize(Roles = "admin")]
		public async Task<ActionResult> UpdatePassword(UpdateUserRequest updateUserRequest)
		{
			var result = await _repository.ChangePassword(updateUserRequest.Id, updateUserRequest.OldPassword, updateUserRequest.NewPassword);
			if (!result.Succeeded)
			{
				var errors = result.Errors.Select(s => $"code:{s.Code}, message:{s.Description}");
				string msg = string.Join("\n", errors);
				return BadRequest(msg);
			}
			return Ok();
		}

		[HttpGet]
		[Authorize(Roles = "admin")]
		public async Task<ActionResult> FindById(Guid id)
		{
			var user = await _userManager.FindByIdAsync(id.ToString());
			if (user == null)
			{
				return BadRequest("未找到用户");
			}
			return Ok(new UserDto() { Id = user.Id,UserName = user.UserName,CreatedTime = user.CreationTime});
		}

		[HttpGet]
		[Authorize(Roles = "admin")]
		public async Task<ActionResult<UserDto[]?>> FindAllUsers()
		{
			return Ok(await _userManager.Users.Select(u => new UserDto() { Id = u.Id, UserName = u.UserName, CreatedTime = u.CreationTime }).ToArrayAsync());
		}
	}
}
