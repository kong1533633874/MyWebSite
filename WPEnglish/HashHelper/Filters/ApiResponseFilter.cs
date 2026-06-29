using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Commons.Filters
{
	public class ApiResponseFilter : IResultFilter
	{
		public void OnResultExecuting(ResultExecutingContext context)
		{
			if (context.Result is ObjectResult objectResult)
			{
				var statusCode = objectResult.StatusCode ?? 200;

				if (objectResult.Value is ApiResponse ||
					objectResult.Value?.GetType().Name.StartsWith("ApiResponse") == true)
				{
					return;
				}

				var wrappedResponse = new
				{
					success = statusCode >= 200 & statusCode < 300,
					statusCode = statusCode,
					message = null as string,
					data = objectResult.Value,
					timestamp = DateTime.UtcNow
				};
				context.Result = new ObjectResult(wrappedResponse)
				{
					StatusCode = statusCode,
				};
			}
		}

		public void OnResultExecuted(ResultExecutedContext context)
		{
		}
	}
}
