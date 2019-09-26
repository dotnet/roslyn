// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Diagnostics.CodeAnalysis;

namespace Roslyn.Utilities
{
    internal static class RoslynString
    {
        /// <inheritdoc cref="string.IsNullOrEmpty(string)"/>
        public static bool IsNullOrEmpty([NotNullWhen(returnValue: false)] this string? value)
            => string.IsNullOrEmpty(value);
    }
}
