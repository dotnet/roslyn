// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Indentation;

internal static class IndentationOptionsProviders
{
    public static IndentationOptions GetDefault(LanguageServices languageServices)
        => new(SyntaxFormattingOptionsProviders.GetDefault(languageServices));
}
