// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace Roslyn.Utilities
{
    internal static class RoslynString
    {
        /// <inheritdoc cref="string.IsNullOrEmpty(string)"/>
        public static bool IsNullOrEmpty([NotNullWhen(returnValue: false)] string? value)
            => string.IsNullOrEmpty(value);

        /// <inheritdoc cref="string.IsNullOrWhiteSpace(string)"/>
        public static bool IsNullOrWhiteSpace([NotNullWhen(returnValue: false)] string? value)
            => string.IsNullOrWhiteSpace(value);
    }
}
