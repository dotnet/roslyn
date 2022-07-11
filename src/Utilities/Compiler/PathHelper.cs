// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

#if CODEANALYSIS_V3_OR_BETTER

using System;
using System.IO;

namespace Analyzer.Utilities
{
    internal static class PathHelper
    {
        private static readonly char[] DirectorySeparatorCharacters = { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };

        public static ReadOnlySpan<char> GetFileName(string? path)
        {
            if (RoslynString.IsNullOrEmpty(path))
                return ReadOnlySpan<char>.Empty;

            var lastSeparator = path.LastIndexOfAny(DirectorySeparatorCharacters);
            if (lastSeparator < 0)
                return path.AsSpan();

            return path.AsSpan()[(lastSeparator + 1)..];
        }
    }
}

#endif
