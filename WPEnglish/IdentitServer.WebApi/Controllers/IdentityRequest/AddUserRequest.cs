using FluentValidation;
namespace IdentitServer.WebApi.Controllers.IdentityRequest
{
	public class AddUserRequest
	{
		public string UserName { get; set; }
		public string Password { get; set; }
	}
	public class AdduserRequestValidator : AbstractValidator<AddUserRequest>
	{
		public AdduserRequestValidator()
		{
			RuleFor(s => s.UserName).NotEmpty().MinimumLength(2).MaximumLength(20);
			RuleFor(s => s.Password).NotEmpty().MinimumLength(6).MaximumLength(20);
		}
	}
}
