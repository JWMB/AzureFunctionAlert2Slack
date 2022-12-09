using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
}
