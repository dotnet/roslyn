// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

namespace Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax;

internal static partial class SyntaxFactory
{
    internal static SyntaxToken Token(SyntaxKind kind, string content, params RazorDiagnostic[] diagnostics)
    {
        if (SyntaxTokenCache.Instance.CanBeCached(kind, diagnostics))
        {
            return SyntaxTokenCache.Instance.GetCachedToken(kind, content);
        }

        return new SyntaxToken(kind, content, diagnostics);
    }

    internal static SyntaxToken MissingToken(SyntaxKind kind, params RazorDiagnostic[] diagnostics)
    {
        return SyntaxToken.CreateMissing(kind, diagnostics);
    }

    public static CSharpExpressionLiteralSyntax CSharpExpressionLiteral(SyntaxList<SyntaxToken> literalTokens)
        => CSharpExpressionLiteral(literalTokens, chunkGenerator: null, editHandler: null);

    public static CSharpTransitionSyntax CSharpTransition(SyntaxToken transition)
        => CSharpTransition(transition, chunkGenerator: null, editHandler: null);

    public static MarkupTextLiteralSyntax MarkupTextLiteral(SyntaxList<SyntaxToken> literalTokens)
        => MarkupTextLiteral(literalTokens, chunkGenerator: null, editHandler: null);

    public static RazorDirectiveSyntax RazorDirective(CSharpTransitionSyntax transition, CSharpSyntaxNode body)
        => RazorDirective(transition, body, directiveDescriptor: null);

    public static RazorUsingDirectiveSyntax RazorUsingDirective(CSharpTransitionSyntax transition, CSharpSyntaxNode body)
        => RazorUsingDirective(transition, body, directiveDescriptor: null);
}
