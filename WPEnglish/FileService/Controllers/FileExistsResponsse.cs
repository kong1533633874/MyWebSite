namespace FileService.WebApi.Controllers
{
	public record FileExistsResponse(bool IsExists,Uri? url);
}
