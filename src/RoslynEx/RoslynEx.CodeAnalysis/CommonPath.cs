using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RoslynEx
{
    static class CommonPath
    {
        /// <summary>
        /// Takes a list of paths and returns a delegate that changes any of the input paths into a relative path with common path prefix removed.
        /// </summary>
        internal static Func<string?, string> MakePrefixRemover(IEnumerable<string?> paths)
        {
            var prefixLength = GetPrefix(paths).Length;

            return path =>
            {
                var parts = Split(path).Skip(prefixLength);

                string result = string.Join(Path.DirectorySeparatorChar.ToString(), parts);
                // turn Unix-style absolute path into relative
                result = result.TrimStart(Path.DirectorySeparatorChar);
                // if files are on different drives on Windows, the returned relative path can't contain :
                result = result.Replace(":", "");
                return result;
            };
        }

        static readonly char[] separators =
            new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };

        static string[] Split(string? path) => path?.Split(separators) ?? Array.Empty<string>();

        static ReadOnlyMemory<string> GetPrefix(IEnumerable<string?> paths)
        {
            return GetPrefix(paths.Select(f =>
            {
                string? directoryName = null;
                if (!string.IsNullOrEmpty(f))
                    directoryName = Path.GetDirectoryName(f);

                return (ReadOnlyMemory<string>)Split(directoryName ?? string.Empty);
            }));
        }

        static ReadOnlyMemory<string> GetPrefix(IEnumerable<ReadOnlyMemory<string>> directories) => directories.Aggregate(GetPrefix);

        internal static ReadOnlyMemory<string> GetPrefix(ReadOnlyMemory<string> directory1, ReadOnlyMemory<string> directory2)
        {
            var d1 = directory1.Span;
            var d2 = directory2.Span;

            // special case for null and empty path
            if (d1.Length == 0 || (d1.Length == 1 && string.IsNullOrEmpty(d1[0])))
                return directory2;
            if (d2.Length == 0 || (d2.Length == 1 && string.IsNullOrEmpty(d2[0])))
                return directory1;

            int i = 0;
            for (; i < directory1.Length && i < directory2.Length; i++)
            {
                if (!string.Equals(d1[i], d2[i], StringComparison.OrdinalIgnoreCase))
                    break;
            }

            return directory1.Slice(0, i);
        }
    }
}
