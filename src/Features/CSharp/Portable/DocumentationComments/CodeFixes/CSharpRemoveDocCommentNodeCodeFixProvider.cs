// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.DiagnosticComments.CodeFixes
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.RemoveDocCommentNode), Shared]
    [ExtensionOrder(After = PredefinedCodeFixProviderNames.ImplementInterface)]
    internal class CSharpRemoveDocCommentNodeCodeFixProvider :
        AbstractRemoveDocCommentNodeCodeFixProvider<XmlElementSyntax, XmlTextSyntax>
    {
        /// <summary>
        /// Duplicate param tag
        /// </summary>
        private const string CS1571 = nameof(CS1571);

        /// <summary>
        /// Param tag with no matching parameter
        /// </summary>
        private const string CS1572 = nameof(CS1572);

        /// <summary>
        /// Duplicate typeparam tag
        /// </summary>
        private const string CS1710 = nameof(CS1710);

        [ImportingConstructor]
        public CSharpRemoveDocCommentNodeCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(CS1571, CS1572, CS1710);

        protected override string DocCommentSignifierToken { get; } = "///";

        protected override SyntaxTriviaList GetRevisedDocCommentTrivia(string docCommentText)
            => SyntaxFactory.ParseLeadingTrivia(docCommentText);

        protected override SyntaxTokenList GetTextTokens(XmlTextSyntax xmlText)
            => xmlText.TextTokens;

        protected override bool IsXmlWhitespaceToken(SyntaxToken token)
            => token.Kind() == SyntaxKind.XmlTextLiteralToken && IsWhitespace(token.Text);

        protected override bool IsXmlNewLineToken(SyntaxToken token)
            => token.Kind() == SyntaxKind.XmlTextLiteralNewLineToken;

        private bool IsWhitespace(string text)
        {
            foreach (var c in text)
            {
                if (!SyntaxFacts.IsWhitespace(c))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
