// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.StringIndentation
{
    internal static class StringIndentationOptionsStorage
    {
        public static readonly PerLanguageOption2<bool> StringIdentation = new("dotnet_indent_strings", defaultValue: true);
    }
}
