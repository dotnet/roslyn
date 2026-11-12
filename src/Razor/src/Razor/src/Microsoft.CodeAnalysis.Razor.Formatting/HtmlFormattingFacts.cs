// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Razor.Language.Syntax;

namespace Microsoft.CodeAnalysis.Remote.Razor.Formatting;

/// <summary>
/// Facts about Html elements that only matter to the formatter. These live here, rather than in HtmlFacts, because this code
/// is also compiled into the dotnet format project, which can't see Microsoft.CodeAnalysis.Razor.Workspaces.
/// </summary>
internal static class HtmlFormattingFacts
{
    /// <summary>
    /// Elements that make up the skeleton of the page, whose content always goes on its own lines.
    /// </summary>
    public static bool IsStructuralElement(BaseMarkupStartTagSyntax tag)
        => tag.Name.Content.Equals("html", StringComparison.OrdinalIgnoreCase) ||
           tag.Name.Content.Equals("head", StringComparison.OrdinalIgnoreCase) ||
           tag.Name.Content.Equals("body", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Elements whose content is whitespace sensitive, so we never want to format their contents.
    /// </summary>
    public static bool HasSignificantWhitespace(BaseMarkupStartTagSyntax tag)
        => tag.Name.Content.Equals("textarea", StringComparison.OrdinalIgnoreCase) ||
           tag.Name.Content.Equals("pre", StringComparison.OrdinalIgnoreCase);
}
