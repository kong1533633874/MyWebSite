using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainCommons
{
	public class DomainResult<T>
	{
		public bool Success { get; set; }
		public T? Data { get; set; }
		public int StatusCode { get; set; }
		public string? Message { get; set; }

		public static DomainResult<T> Ok(T data) =>
			new() { Success = true, Data = data, StatusCode = 200 };

		public static DomainResult<T> Fail(string message, int statusCode = 400) =>
			new() { Success = false, Message = message, StatusCode = statusCode };
	}
}
