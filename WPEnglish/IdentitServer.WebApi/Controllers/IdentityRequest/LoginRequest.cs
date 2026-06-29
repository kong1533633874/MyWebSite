using FluentValidation;
namespace IdentitServer.WebApi.Controllers.IdentityRequest
{
	public class LoginRequest
	{
		public string Username { get; set; }
		public string Password { get; set; }
	}
	public class LoginRequestValidator : AbstractValidator<LoginRequest>
	{
		public LoginRequestValidator()
		{
			RuleFor(s => s.Username).NotEmpty();
			RuleFor(s => s.Password).NotEmpty();
		}
	}
}
