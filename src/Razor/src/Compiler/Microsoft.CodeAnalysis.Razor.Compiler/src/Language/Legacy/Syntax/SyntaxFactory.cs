// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language.Syntax;

internal static partial class SyntaxFactory
{
    public static CSharpExpressionLiteralSyntax CSharpExpressionLiteral(SyntaxToken token)
        => CSharpExpressionLiteral(new SyntaxTokenList(token), chunkGenerator: null, editHandler: null);

    public static CSharpExpressionLiteralSyntax CSharpExpressionLiteral(SyntaxTokenList literalTokens)
        => CSharpExpressionLiteral(literalTokens, chunkGenerator: null, editHandler: null);

    public static CSharpTransitionSyntax CSharpTransition(SyntaxToken transition)
        => CSharpTransition(transition, chunkGenerator: null, editHandler: null);

    public static MarkupTextLiteralSyntax MarkupTextLiteral(SyntaxToken token)
        => MarkupTextLiteral(new SyntaxTokenList(token), chunkGenerator: null, editHandler: null);

    public static MarkupTextLiteralSyntax MarkupTextLiteral(SyntaxTokenList literalTokens)
        => MarkupTextLiteral(literalTokens, chunkGenerator: null, editHandler: null);

    public static RazorMetaCodeSyntax RazorMetaCode(SyntaxToken token)
        => RazorMetaCode(new SyntaxTokenList(token), chunkGenerator:null, editHandler: null);

    public static RazorMetaCodeSyntax RazorMetaCode(SyntaxTokenList metaCode)
        => RazorMetaCode(metaCode, chunkGenerator: null, editHandler: null);
}
