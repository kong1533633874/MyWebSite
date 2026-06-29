
using Listening.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace IdentityService.WebAPI;
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ListengingDbContext>
{
	public ListengingDbContext CreateDbContext(string[] args)
	{
		DbContextOptionsBuilder<ListengingDbContext> builder = new DbContextOptionsBuilder<ListengingDbContext>();
		string connStr = "Data Source=.;Initial Catalog=WPEnglish;Uid=sa;Pwd=123456;Integrated Security=SSPI;TrustServerCertificate=True";
		builder.UseSqlServer(connStr);
		ListengingDbContext myDbContext = new ListengingDbContext(builder.Options);
		return myDbContext;
	}
}