// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if !MSBUILDWORKSPACE_BUILDHOST

using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Roslyn.Utilities
{
    /// <summary>
    /// Dependency-free implementation of the path-map parsing and prefix-rewriting used by
    /// <c>/pathmap</c>. It is shared as source (not via an assembly reference) so the minimal
    /// <c>Microsoft.Build.Tasks.CodeAnalysis</c> build task and the compiler run identical logic
    /// without the task taking a dependency on <c>Microsoft.CodeAnalysis</c>. <c>PathUtilities</c>
    /// and <c>CommandLineParser</c> delegate here where they can.
    /// </summary>
    internal static class PathMapParser
    {
        /// <summary>
        /// Parses a <c>/pathmap</c>-style string (<c>from=to,from2=to2</c>) into a list of prefix
        /// mappings ordered longest key first, so the most specific prefix wins. Unlike the
        /// compiler's <c>CommandLineParser.ParsePathMap</c> this does not report diagnostics;
        /// malformed entries are silently skipped. Callers that need diagnostics keep their own
        /// parsing shell and reuse the primitives here.
        /// </summary>
        public static List<KeyValuePair<string, string>> ParsePathMap(string pathMap)
        {
            var result = new List<KeyValuePair<string, string>>();
            if (string.IsNullOrEmpty(pathMap))
            {
                return result;
            }

            foreach (var kEqualsV in SplitWithDoubledSeparatorEscaping(pathMap, ','))
            {
                if (kEqualsV.Length == 0)
                {
                    continue;
                }

                var kv = SplitWithDoubledSeparatorEscaping(kEqualsV, '=');
                if (kv.Length != 2)
                {
                    continue;
                }

                var from = kv[0];
                var to = kv[1];
                if (from.Length == 0 || to.Length == 0)
                {
                    continue;
                }

                result.Add(new KeyValuePair<string, string>(EnsureTrailingSeparator(from), EnsureTrailingSeparator(to)));
            }

            result.Sort((x, y) => -x.Key.Length.CompareTo(y.Key.Length));
            return result;
        }

        /// <summary>
        /// Rewrites <paramref name="filePath"/> by replacing the first mapped prefix that matches it.
        /// The comparison is ordinal (case-sensitive); the caller is expected to have ordered
        /// <paramref name="pathMap"/> most-specific-first. Generic over the list type so both the
        /// compiler's <c>ImmutableArray</c> and the build task's <c>List</c> are consumed without
        /// allocation.
        /// </summary>
        public static string NormalizePathPrefix<TList>(string filePath, TList pathMap)
            where TList : IReadOnlyList<KeyValuePair<string, string>>
        {
            // find the first key in the path map that matches a prefix of the path.
            // Note that we expect the client to use consistent capitalization; we use ordinal (case-sensitive) comparisons.
            for (int i = 0; i < pathMap.Count; i++)
            {
                var oldPrefix = pathMap[i].Key;
                if (!(oldPrefix?.Length > 0)) continue;

                // oldPrefix always ends with a path separator, so there's no need to check if it was a partial match
                // e.g. for the map /goo=/bar and filename /goooo
                if (filePath.StartsWith(oldPrefix, System.StringComparison.Ordinal))
                {
                    var replacementPrefix = pathMap[i].Value;

                    // Replace that prefix.
                    var replacement = replacementPrefix + filePath.Substring(oldPrefix.Length);

                    // Normalize the path separators if used uniformly in the replacement
                    bool hasSlash = replacementPrefix.IndexOf('/') >= 0;
                    bool hasBackslash = replacementPrefix.IndexOf('\\') >= 0;
                    return
                        (hasSlash && !hasBackslash) ? replacement.Replace('\\', '/') :
                        (hasBackslash && !hasSlash) ? replacement.Replace('/', '\\') :
                        replacement;
                }
            }

            return filePath;
        }

        /// <summary>
        /// Splits <paramref name="str"/> on <paramref name="separator"/>, treating a doubled
        /// separator as an escaped literal. E.g. <c>"a,,b,c"</c> split on <c>','</c> yields
        /// <c>["a,b", "c"]</c>.
        /// </summary>
        private static string[] SplitWithDoubledSeparatorEscaping(string str, char separator)
        {
            if (str.Length == 0)
            {
                return System.Array.Empty<string>();
            }

            var result = new List<string>();
            var part = new StringBuilder();

            int i = 0;
            while (i < str.Length)
            {
                char c = str[i++];
                if (c == separator)
                {
                    if (i < str.Length && str[i] == separator)
                    {
                        i++;
                    }
                    else
                    {
                        result.Add(part.ToString());
                        part.Clear();
                        continue;
                    }
                }

                part.Append(c);
            }

            result.Add(part.ToString());
            return result.ToArray();
        }

        /// <summary>
        /// Appends a trailing directory separator to <paramref name="s"/> if it does not already end
        /// with one, preferring whichever separator the string already uses consistently.
        /// </summary>
        private static string EnsureTrailingSeparator(string s)
        {
            if (s.Length == 0 || s[s.Length - 1] == '/' || s[s.Length - 1] == '\\')
            {
                return s;
            }

            // Use the existing slashes in the path, if they're consistent
            bool hasSlash = s.IndexOf('/') >= 0;
            bool hasBackslash = s.IndexOf('\\') >= 0;
            if (hasSlash && !hasBackslash)
            {
                return s + '/';
            }
            else if (!hasSlash && hasBackslash)
            {
                return s + '\\';
            }
            else
            {
                // If there are no slashes or they are inconsistent, use the current platform's slash.
                return s + Path.DirectorySeparatorChar;
            }
        }
    }
}

#endif
