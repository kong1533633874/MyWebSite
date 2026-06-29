namespace IdentitServer.WebApi.Controllers.IdentityRequest
{
	public class UserDto
	{
		public Guid Id { get; set; }
		public string UserName { get; set; }
		public DateTime CreatedTime { get; set; }
	}
}
