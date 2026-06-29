using System.Collections.Generic;
using System.Linq;

namespace FluentValidation
{
	public static class EnumerableValidators
	{
		public static IRuleBuilderOptions<T, IEnumerable<TItem>> NotDuplicated<T, TItem>(this IRuleBuilder<T, IEnumerable<TItem>> ruleBuilder)
		{
			return ruleBuilder.Must(s => s == null || s.Distinct().Count() == s.Count());
		}

		public static IRuleBuilderOptions<T,IEnumerable<TItem>> NotContains<T,TItem>(this IRuleBuilder<T,IEnumerable<TItem>> ruleBuilder, TItem comparedValue)
		{
			return ruleBuilder.Must(s => s == null || !s.Contains(comparedValue));
		}
	}
}
