// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using Roslyn.Utilities;

namespace Analyzer.Utilities
{
    internal static class PathHelper
    {
        private static readonly char[] DirectorySeparatorCharacters = [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar];

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
