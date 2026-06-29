
using IdentityServer.Infrastructure;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace IdentityService.WebAPI;
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<IdDbContext>
{
	public IdDbContext CreateDbContext(string[] args)
	{
		DbContextOptionsBuilder<IdDbContext> builder = new DbContextOptionsBuilder<IdDbContext>();
		string connStr = "Data Source=.;Initial Catalog=WPEnglish;Uid=sa;Pwd=123456;Integrated Security=SSPI;TrustServerCertificate=True";
		builder.UseSqlServer(connStr);
		IdDbContext myDbContext = new IdDbContext(builder.Options);
		return myDbContext;
	}
}