using FluentValidation;

namespace IdentitServer.WebApi.Controllers.IdentityRequest
{
	public class UpdateUserRequest
	{
		public Guid Id { get; set; }
		public string OldPassword { get; set; }
		public string NewPassword { get; set; }
	}
	public class UpdateUserValidator : AbstractValidator<UpdateUserRequest>
	{
		public UpdateUserValidator()
		{
			RuleFor(x => x.Id).NotEmpty();
			RuleFor(x => x.OldPassword).NotEmpty().MaximumLength(20).MinimumLength(6);
			RuleFor(x => x.NewPassword).NotEmpty().MaximumLength(20).MinimumLength(6);
		}
	}
}
