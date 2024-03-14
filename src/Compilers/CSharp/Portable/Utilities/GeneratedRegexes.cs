// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.RegularExpressions;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal static partial class GeneratedRegexes
    {
        // language=regex
        private const string s_interceptsLocationSpecifierPattern = """v1:(.*)\((\d+),(\d+)\)""";

#if NETCOREAPP
        [GeneratedRegex(s_interceptsLocationSpecifierPattern)]
        internal static partial Regex GetInterceptsLocationSpecifierRegex();
#else
        private static readonly Regex s_interceptsLocationSpecifier = new Regex(s_interceptsLocationSpecifierPattern, RegexOptions.Compiled);
        
        internal static Regex GetInterceptsLocationSpecifierRegex() => s_interceptsLocationSpecifier;
#endif
    }
}
