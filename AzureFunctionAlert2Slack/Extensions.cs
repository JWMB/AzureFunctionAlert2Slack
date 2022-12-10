using System;
using System.Collections.Generic;
using System.Linq;

namespace AzureFunctionAlert2Slack
{
    public static class StringExtensions
    {
        public static string Truncate(this string str, int maxLength, string ellipsis = "…")
        {
            if (str.Length <= maxLength) return str;
            maxLength = maxLength - ellipsis.Length;
            return $"{str.Remove(maxLength)}{ellipsis}";
        }
    }

    public static class ExceptionExtensions
    {
        public static string ToStringRecursive(this Exception exception, int maxTotalLength = int.MaxValue)
        {
            var parts = RecurseSerializeException(exception).ToList();
            var lenPerPart = maxTotalLength / parts.Count;
            return string.Join("\n", parts.Select(o => o.Truncate(lenPerPart)));

            IEnumerable<string> RecurseSerializeException(Exception ex)
            {
                if (ex is AggregateException aex)
                    foreach (var child in aex.InnerExceptions.Select(RecurseSerializeException))
                        foreach (var item in child)
                            yield return item;
                yield return $"{ex.GetType().Name}:{ex.Message} Stack:{ex.StackTrace}";
            }
        }
    }
}
