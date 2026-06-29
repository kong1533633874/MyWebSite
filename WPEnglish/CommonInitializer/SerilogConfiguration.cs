using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonInitializer
{
	public static class SerilogConfiguration
	{
		public static void AddSerilogConfiguration(this IHostBuilder host, IConfiguration configuration)
		{
			host.UseSerilog((context, config) =>
			{
				config.MinimumLevel.Information()
					  .Enrich.FromLogContext()
					  .WriteTo.Async(a => a.File(
						  configuration.GetSection("LoggerToFile").Value,
						  rollingInterval: RollingInterval.Day,
						  retainedFileCountLimit: 7,
						  fileSizeLimitBytes: 500 * 1024 * 1024,
						  rollOnFileSizeLimit: true,
						  shared: true,
						  outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
					  ))
					  .WriteTo.Console();
			});
		}
	}
}
