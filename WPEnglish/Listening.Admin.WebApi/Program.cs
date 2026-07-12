
using Listening.Domain.Service;
using Listening.Infrastructure;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Listening.Admin.WebApi.CategoryController.Request;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using JWT;
using CommonInitializer;
using Commons.Middlewares;
using Commons.Filters;
using Serilog;
using Commons.JsonConverters;

namespace Listening.Admin.WebApi
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);

			// 允许上传大文件（AI 字幕生成时需要）
			builder.WebHost.ConfigureKestrel(options =>
			{
				options.Limits.MaxRequestBodySize = 100_000_000; // 100MB
			});

			// Add services to the container.

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
			builder.Services.AddDbContext<ListengingDbContext>(opt =>
			{
				string connStr = builder.Configuration.GetSection("connStr").Value;
				opt.UseSqlServer(connStr, option =>
				{
					option.EnableRetryOnFailure(maxRetryCount: 3);
				});
			});
			builder.Services.AddScoped<IListeningRepository, ListeningRepository>();
			builder.Services.AddScoped<ListeningService>();
			builder.Services.AddValidatorsFromAssemblyContaining<CategoryAddRequestValidator>();
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
						IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSetting.Key)),
						IssuerSigningKeyValidator = (key, token, parameters) => true
					};
				});
			//log
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
			app.MapControllers();
			app.Run();
		}
	}
}
