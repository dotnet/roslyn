namespace Roslyn.Utilities
{
#if !COMPILERCORE
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
    internal static class StringUtilities
    {
        public static string ValueOf<T>(T value)
        {
            if (default(T) == null)
            {
                if (value == null)
                {
                    return string.Empty;
                }
            }

            return value.ToString();
        }
    }
}
