// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Roslyn.LanguageServer.Protocol;

internal static partial class LspExtensions
{
    public static LspRange GetRange(this SyntaxNode node, RazorSourceDocument source)
    {
        var linePositionSpan = node.GetLinePositionSpan(source);

        return LspFactory.CreateRange(linePositionSpan);
    }
}
