// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.LineSeparators
{
    internal static class LineSeparatorsOptions
    {
        public static readonly PerLanguageOption2<bool> LineSeparator = new("dotnet_line_separators_options_line_separator", defaultValue: false);
    }
}
