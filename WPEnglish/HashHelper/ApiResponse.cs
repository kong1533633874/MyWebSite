using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Commons
{
	public class ApiResponse<T>
	{
		public bool Success { get; set; }
		public string? Message { get; set; }
		public T? Data { get; set; }
		public int StatusCode { get; set; }
		public DateTime Timestamp { get; set; } = DateTime.UtcNow;

		public static ApiResponse<T> Ok(T data, string? message = null)
		{
			return new ApiResponse<T>
			{
				Success = true,
				Data = data,
				Message = message,
				StatusCode = 200
			};
		}

		public static ApiResponse<T> Fail(string message, int statusCode = 400)
		{
			return new ApiResponse<T>
			{
				Success = false,
				Message = message,
				StatusCode = statusCode
			};
		}
	}
	// 如果不需要泛型
	public class ApiResponse
	{
		public bool Success { get; set; }
		public string? Message { get; set; }
		public int StatusCode { get; set; }
		public DateTime Timestamp { get; set; } = DateTime.UtcNow;

		public static ApiResponse Fail(string message, int statusCode = 400)
		{
			return new ApiResponse
			{
				Success = false,
				Message = message,
				StatusCode = statusCode
			};
		}
	}
}
