using System;

namespace FASUtils
{
    public static class StringExtensions
    {
        public static bool Contains(this string source, string toCheck, StringComparison comp)
        {
            return source?.IndexOf(toCheck, comp) >= 0;
        }

        public static string TrimStartString(this string source, string value, char delimiter = '.')
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return source.TrimStart(delimiter);
            }
            else
            {
                return source.Substring(value.Length).TrimStart(delimiter);
            }
        }

        public static string TrimEndString(this string source, string value, char delimiter = '.')
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return source.TrimEnd(delimiter);
            }
            else
            {
                return source.Substring(0, source.Length - value.Length).TrimEnd(delimiter);
            }
        }

        public static T ToEnum<T>(this string value, T defaultValue) where T : struct
        {
            if (string.IsNullOrEmpty(value))
            {
                return defaultValue;
            }

            return Enum.TryParse(value, true, out T result) ? result : defaultValue;
        }

        public static string PluralizeWith(this string source, params object[] values)
        {
            return string.Format(new PluralFormatProvider(), source, values);
        }
    }
}
