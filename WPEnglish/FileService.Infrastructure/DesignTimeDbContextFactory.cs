using FileService.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace IdentityService.WebAPI;
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<FSDbContext>
{
	public FSDbContext CreateDbContext(string[] args)
	{
		DbContextOptionsBuilder<FSDbContext> builder = new DbContextOptionsBuilder<FSDbContext>();
		string connStr = "Data Source=.;Initial Catalog=WPEnglish;Uid=sa;Pwd=123456;Integrated Security=SSPI;TrustServerCertificate=True";
		builder.UseSqlServer(connStr);
		FSDbContext myDbContext = new FSDbContext(builder.Options);
		return myDbContext;
	}
}