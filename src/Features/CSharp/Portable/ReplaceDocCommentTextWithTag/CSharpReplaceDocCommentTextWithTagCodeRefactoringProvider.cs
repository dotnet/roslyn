// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.ReplaceDocCommentTextWithTag;

namespace Microsoft.CodeAnalysis.CSharp.ReplaceDocCommentTextWithTag
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp,
        Name = PredefinedCodeRefactoringProviderNames.ReplaceDocCommentTextWithTag), Shared]
    internal class CSharpReplaceDocCommentTextWithTagCodeRefactoringProvider :
        AbstractReplaceDocCommentTextWithTagCodeRefactoringProvider
    {
        private static readonly ImmutableHashSet<string> s_triggerKeywords = ImmutableHashSet.Create(
            SyntaxFacts.GetText(SyntaxKind.NullKeyword),
            SyntaxFacts.GetText(SyntaxKind.StaticKeyword),
            SyntaxFacts.GetText(SyntaxKind.VirtualKeyword),
            SyntaxFacts.GetText(SyntaxKind.TrueKeyword),
            SyntaxFacts.GetText(SyntaxKind.FalseKeyword),
            SyntaxFacts.GetText(SyntaxKind.AbstractKeyword),
            SyntaxFacts.GetText(SyntaxKind.SealedKeyword),
            SyntaxFacts.GetText(SyntaxKind.AsyncKeyword),
            SyntaxFacts.GetText(SyntaxKind.AwaitKeyword));

        [ImportingConstructor]
        public CSharpReplaceDocCommentTextWithTagCodeRefactoringProvider()
        {
        }

        protected override bool IsXmlTextToken(SyntaxToken token)
            => token.Kind() == SyntaxKind.XmlTextLiteralToken ||
               token.Kind() == SyntaxKind.XmlTextLiteralNewLineToken;

        protected override bool IsInXMLAttribute(SyntaxToken token)
        {
            return (token.Parent.Kind() == SyntaxKind.XmlCrefAttribute
                || token.Parent.Kind() == SyntaxKind.XmlNameAttribute
                || token.Parent.Kind() == SyntaxKind.XmlTextAttribute);
        }

        protected override bool IsKeyword(string text)
            => s_triggerKeywords.Contains(text);

        protected override SyntaxNode ParseExpression(string text)
            => SyntaxFactory.ParseExpression(text);

    }
}
