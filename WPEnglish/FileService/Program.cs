using Serilog;
using CommonInitializer;
using FileService.Domain.Service;
using FileService.Infrastructure;
using FileService.Infrastructure.StorageServices;
using JWT;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Commons.Middlewares;
using Commons.Filters;
using Commons.JsonConverters;

namespace FileService
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);

			builder.Services.AddControllers(option =>
			{
				option.Filters.Add<ApiResponseFilter>();
			}).AddJsonOptions(o =>
			{
				o.JsonSerializerOptions.Converters.Add(new DateTimeJsonConverter());
				o.JsonSerializerOptions.Converters.Add(new NullableDateTimeJsonConverter());
			});
			builder.Services.AddEndpointsApiExplorer();
			builder.Services.AddSwaggerGen();
			builder.Services.AddScoped<IStorageClient, SMBStorageClient>();
			builder.Services.AddScoped<IFSRepository, FSRepository>();
			builder.Services.AddScoped<FSDomainService>();
			builder.Services.Configure<SMBStorageOptions>(builder.Configuration.GetSection("BaseUrl"));
			builder.Services.AddDbContext<FSDbContext>(opt =>
			{
				string? connstr = builder.Configuration.GetSection("connStr").Value;
				opt.UseSqlServer(connstr, option =>
				{
					option.EnableRetryOnFailure(maxRetryCount: 3);
				});
			});
			builder.Services.AddCors(options => {
				var corsSetting = builder.Configuration.GetSection("Cors").Get<CorsSettings>();
				string[] urls = corsSetting.Origins;
				options.AddDefaultPolicy(builder =>
					builder.WithOrigins(urls)
						   .AllowAnyMethod()
						   .AllowAnyHeader()
				);
			});
			//jwt
			builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
				.AddJwtBearer(opt =>
				{
					var jwtSetting = builder.Configuration.GetSection("JWT").Get<JWTOptions>();

					opt.TokenValidationParameters = new TokenValidationParameters()
					{
						ValidateIssuer = true,
						ValidateAudience = true,
						ValidateLifetime = true,
						ValidateIssuerSigningKey = true,
						ValidIssuer = jwtSetting.Issuer,
						ValidAudience = jwtSetting.Audience,
						IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSetting.Key))
					};
				});
			builder.Host.AddSerilogConfiguration(builder.Configuration);
			var app = builder.Build();

			// Configure the HTTP request pipeline.
			if (app.Environment.IsDevelopment())
			{
				app.UseSwagger();
				app.UseSwaggerUI();
			}

			app.UseCors();
			app.UseGlobalExceptionHandler();
			app.UseAuthentication();
			app.UseAuthorization();
			app.UseStaticFiles(new StaticFileOptions()
			{
				FileProvider = new PhysicalFileProvider(builder.Configuration["BaseUrl:BaseUrl"]),
				RequestPath = "/upload"
			});
			app.MapControllers();
			app.Run();
		}
	}
}
