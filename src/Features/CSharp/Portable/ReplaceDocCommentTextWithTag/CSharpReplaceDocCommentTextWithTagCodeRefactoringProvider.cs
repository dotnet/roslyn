// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
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
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
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
