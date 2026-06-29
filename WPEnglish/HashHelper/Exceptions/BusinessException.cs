using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Commons.Exceptions
{
	public class BusinessException : Exception
	{
		public int Code { get; set; }

		public BusinessException(string message, int code = 400):base(message) 
		{
			Code = code;
		}
	}
}
