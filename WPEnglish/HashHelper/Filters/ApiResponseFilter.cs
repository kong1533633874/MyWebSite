using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Commons.Filters
{
	public class ApiResponseFilter : IResultFilter
	{
		// 常用状态码的默认消息
		private static readonly Dictionary<int, string> DefaultMessages = new()
		{
			{ 400, "请求参数无效" },
			{ 401, "未授权，请先登录" },
			{ 403, "无权限访问该资源" },
			{ 404, "请求的资源不存在" },
			{ 405, "不支持的请求方法" },
			{ 415, "不支持的媒体类型" },
			{ 500, "服务器内部错误" }
		};

		public void OnResultExecuting(ResultExecutingContext context)
		{
			// 跳过文件、重定向、质询等特殊结果
			if (context.Result is not ObjectResult and not StatusCodeResult)
				return;

			// 只处理 ObjectResult 和 StatusCodeResult，其他如 EmptyResult 保持原样
			int statusCode;
			object? data = null;
			string? message = null;

			switch (context.Result)
			{
				case ObjectResult objResult:
					statusCode = objResult.StatusCode ?? 200;

					// 已经是包装过的，跳过
					if (objResult.Value is ApiResponse)
						return;

					if (statusCode >= 200 && statusCode < 300)
					{
						data = objResult.Value;
					}
					else
					{
						// 错误时，尝试从 ProblemDetails 提取友好信息
						message = objResult.Value switch
						{
							ValidationProblemDetails vp => string.Join("; ",
							vp.Errors.SelectMany(kv =>
							{
								var field = kv.Key.TrimStart('$', '.');
								return kv.Value.Select(e => $"{field}: {e}");
							})),
							ProblemDetails problem => problem.Title ?? problem.Detail,
							_ => objResult.Value?.ToString()
						};
					}
					break;

				case StatusCodeResult statusResult:
					statusCode = statusResult.StatusCode;
					// 无 body 的状态码，稍后统一设置默认消息
					break;

				default:
					return; // 其他结果类型直接放行
			}

			// 失败且消息为空时，使用预设默认消息
			if (!(statusCode >= 200 && statusCode < 300) && string.IsNullOrEmpty(message))
			{
				DefaultMessages.TryGetValue(statusCode, out message);
			}

			var wrapped = new ApiResponse<object?>
			{
				Success = statusCode >= 200 && statusCode < 300,
				StatusCode = statusCode,
				Message = message,
				Data = statusCode >= 200 && statusCode < 300 ? data : null
			};

			context.Result = new ObjectResult(wrapped) { StatusCode = statusCode };
		}

		public void OnResultExecuted(ResultExecutedContext context) { }
	}
}
