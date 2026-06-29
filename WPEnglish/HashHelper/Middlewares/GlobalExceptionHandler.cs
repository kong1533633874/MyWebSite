using Commons.Exceptions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Commons.Middlewares
{
	public class GlobalExceptionHandler
	{
		private readonly RequestDelegate _next;
		private readonly ILogger<GlobalExceptionHandler> _logger;
		private static readonly JsonSerializerOptions _jsonOptions = new()
		{
			Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
		};

		public GlobalExceptionHandler(RequestDelegate next,ILogger<GlobalExceptionHandler> logger)
		{
			this._next = next;
			this._logger = logger;
		}

		public async Task InvokeAsync(HttpContext httpContext)
		{
			try
			{
				await _next.Invoke(httpContext);
			}catch (Exception ex)
			{
				await HandleExceptionAsync(httpContext, ex);
			}
		}

		private async Task HandleExceptionAsync(HttpContext httpContext, Exception exception)
		{
			_logger.LogError(exception, "发生未处理异常:{Message}", exception.Message);

			var (statusCode, message) = exception switch
			{
				NotFoundException ex => (HttpStatusCode.NotFound, ex.Message),
				BusinessException ex => (HttpStatusCode.BadRequest, ex.Message),
				UnauthorizedAccessException ex => (HttpStatusCode.Unauthorized, ex.Message),
				ArgumentException ex => (HttpStatusCode.BadRequest, ex.Message),
				_ => (HttpStatusCode.InternalServerError, "服务器内部错误，请稍后重试")
			};

			var response = new
			{
				success = false,
				message = message,
				statusCode = (int)statusCode,
				timestamp = DateTime.UtcNow
			};


			httpContext.Response.StatusCode = (int)statusCode;
			httpContext.Response.ContentType = "application/json";

			await httpContext.Response.WriteAsync(JsonSerializer.Serialize(response, _jsonOptions));
		}
	}

	public static class GlobalExceptionHandlerExtensions
	{
		public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
		{
			return app.UseMiddleware<GlobalExceptionHandler>();
		}
	}
}
