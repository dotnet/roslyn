// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Roslyn.LanguageServer.Protocol;

internal static partial class LspExtensions
{
    public static SyntaxNode? FindInnermostNode(
        this RazorSyntaxTree syntaxTree,
        SourceText sourceText,
        Position position,
        bool includeWhitespace = false)
    {
        if (!sourceText.TryGetAbsoluteIndex(position, out var absoluteIndex))
        {
            return null;
        }

        return syntaxTree.Root.FindInnermostNode(absoluteIndex, includeWhitespace);
    }
}
