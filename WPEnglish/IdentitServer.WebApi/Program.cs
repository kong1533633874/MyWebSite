using IdentityServer.Domain.Entity;
using IdentityServer.Domain.Service;
using IdentityServer.Infrastructure;
using JWT;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Commons.JsonConverters;
using FluentValidation;
using IdentitServer.WebApi.Controllers.IdentityRequest;
using CommonInitializer;
using Commons.Middlewares;
using Commons.Filters;
using Serilog;
using Microsoft.Extensions.Logging;

namespace IdentitServer.WebApi
{
	public class Program
	{
		public static async Task Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);

			// Add services to the container.

			builder.Services.AddControllers(option =>
			{
				option.Filters.Add<ApiResponseFilter>();
			}).AddJsonOptions(o =>
			{
				o.JsonSerializerOptions.Converters.Add(new DateTimeJsonConverter());
				o.JsonSerializerOptions.Converters.Add(new NullableDateTimeJsonConverter());
			});
			// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
			builder.Services.AddEndpointsApiExplorer();
			builder.Services.AddSwaggerGen();

			builder.Services.AddScoped<IdService>();
			builder.Services.AddScoped<IIdentityRepository, IdentityRepository>();
			builder.Services.Configure<JWTOptions>(builder.Configuration.GetSection("JWT"));

			builder.Services.AddDbContext<IdDbContext>(opt =>
			{
				opt.UseSqlServer(builder.Configuration.GetSection("connStr").Value, option =>
				{
					option.EnableRetryOnFailure(maxRetryCount: 3);
				});
			});

			builder.Services.AddValidatorsFromAssemblyContaining<AdduserRequestValidator>();

			//datatime
			builder.Services.Configure<JsonOptions>(options =>
			{
				options.JsonSerializerOptions.Converters.Add(new DateTimeJsonConverter("yyyy-MM-ddTHH:mm:ssZ"));
			});

			builder.Services.AddAuthorization();
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

			//identity
			builder.Services.AddDataProtection();
			builder.Services.AddIdentityCore<User>(options =>
			{
				options.Password.RequireDigit = false;
				options.Password.RequireLowercase = false;
				options.Password.RequireNonAlphanumeric = false;
				options.Password.RequireUppercase = false;
				options.Password.RequiredLength = 6;
				options.Tokens.PasswordResetTokenProvider = TokenOptions.DefaultEmailProvider;
				options.Tokens.EmailConfirmationTokenProvider = TokenOptions.DefaultEmailProvider;
			});
			IdentityBuilder identityBuilder = new IdentityBuilder(typeof(User), typeof(Role), builder.Services);
			identityBuilder.AddEntityFrameworkStores<IdDbContext>();
			identityBuilder.AddDefaultTokenProviders();
			identityBuilder.AddUserManager<IdUserManager>();
			identityBuilder.AddRoleManager<RoleManager<Role>>();

			builder.Services.AddCors(options => {
				var corsSetting = builder.Configuration.GetSection("Cors").Get<CorsSettings>();
				string[] urls = corsSetting.Origins;
				options.AddDefaultPolicy(builder =>
					builder.WithOrigins(urls)
						   .AllowAnyMethod()
						   .AllowAnyHeader()
				);
			});
			builder.Host.AddSerilogConfiguration(builder.Configuration);

			var app = builder.Build();

			// Configure the HTTP request pipeline.
			using (var scope = app.Services.CreateScope())
			{
				var userManager = scope.ServiceProvider.GetRequiredService<IdUserManager>();
				var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
				var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
				await SeedAdminUser(userManager, roleManager, logger);
			}

			static async Task SeedAdminUser(IdUserManager userManager,RoleManager<Role> roleManager, ILogger<Program> logger)
			{
				var adminpassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD");

				if (!await roleManager.RoleExistsAsync("admin"))
				{
					await roleManager.CreateAsync(new Role() { Name = "admin"});
					logger.LogInformation("已创建admin角色");
				}

				if(await userManager.FindByNameAsync("admin") == null)
				{
					var adminUser = new User("admin");
					var result = await userManager.CreateAsync(adminUser, adminpassword);
					if (!result.Succeeded)
					{
						logger.LogError("创建admin用户失败：{Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
						throw new Exception("Failed to create admin user: " + string.Join(", ", result.Errors));
					}
					await userManager.AddToRoleAsync(adminUser, "admin");
					logger.LogInformation("已创建admin用户并分配角色");
				}
			}

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
