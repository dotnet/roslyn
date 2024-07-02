// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Formatting;

internal static class SyntaxFormattingOptionsStorage
{
    public static SyntaxFormattingOptions GetSyntaxFormattingOptions(this IGlobalOptionService globalOptions, LanguageServices languageServices)
        => globalOptions.GetSyntaxFormattingOptions(languageServices);
}

