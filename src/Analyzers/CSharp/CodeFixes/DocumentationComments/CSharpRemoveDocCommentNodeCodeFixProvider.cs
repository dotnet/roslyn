// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.DocumentationComments;

namespace Microsoft.CodeAnalysis.CSharp.DocumentationComments
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
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
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

        private static bool IsWhitespace(string text)
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
