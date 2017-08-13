// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.ReplaceDocCommentTextWithTag;

namespace Microsoft.CodeAnalysis.CSharp.ReplaceDocCommentTextWithTag
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp), Shared]
    internal class CSharpReplaceDocCommentTextWithTagCodeRefactoringProvider :
        AbstractReplaceDocCommentTextWithTagCodeRefactoringProvider
    {
        protected override bool IsXmlTextToken(SyntaxToken token)
            => token.Kind() == SyntaxKind.XmlTextLiteralToken ||
               token.Kind() == SyntaxKind.XmlTextLiteralNewLineToken;

        protected override bool IsAnyKeyword(string text)
            => SyntaxFacts.GetKeywordKind(text) != SyntaxKind.None ||
               SyntaxFacts.GetContextualKeywordKind(text) != SyntaxKind.None;

        protected override SyntaxNode ParseExpression(string text)
            => SyntaxFactory.ParseExpression(text);
    }
}
