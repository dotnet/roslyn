// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Razor.Language.Syntax;

internal static class RazorSyntaxNodeOrTokenExtensions
{
    public static bool ContainsOnlyWhitespace(this SyntaxNodeOrToken nodeOrToken, bool includingNewLines = true)
        => nodeOrToken.IsToken
            ? nodeOrToken.AsToken().ContainsOnlyWhitespace(includingNewLines)
            : nodeOrToken.AsNode().AssumeNotNull().ContainsOnlyWhitespace(includingNewLines);

    public static LinePositionSpan GetLinePositionSpan(this SyntaxNodeOrToken nodeOrToken, RazorSourceDocument source)
        => nodeOrToken.IsToken
            ? nodeOrToken.AsToken().GetLinePositionSpan(source)
            : nodeOrToken.AsNode().AssumeNotNull().GetLinePositionSpan(source);
}
