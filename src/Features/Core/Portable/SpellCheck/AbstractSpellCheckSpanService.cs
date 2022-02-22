// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.SpellCheck
{
    internal abstract class AbstractSpellCheckSpanService : ISpellCheckSpanService
    {
        protected AbstractSpellCheckSpanService()
        {
        }

        protected abstract bool IsDeclarationIdentifier(SyntaxToken token);
        protected abstract TextSpan GetSpanForComment(SyntaxTrivia trivia);
        protected abstract TextSpan GetSpanForRawString(SyntaxToken token);
        protected abstract TextSpan GetSpanForString(SyntaxToken token);

        public async Task<ImmutableArray<SpellCheckSpan>> GetSpansAsync(Document document, CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

            using var _ = ArrayBuilder<SpellCheckSpan>.GetInstance(out var spans);

            Recurse(root, syntaxFacts, spans, cancellationToken);

            return spans.ToImmutable();
        }

        private void Recurse(SyntaxNode root, ISyntaxFactsService syntaxFacts, ArrayBuilder<SpellCheckSpan> spans, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            foreach (var child in root.ChildNodesAndTokens())
            {
                if (child.IsNode)
                {
                    Recurse(child.AsNode()!, syntaxFacts, spans, cancellationToken);
                }
                else
                {
                    ProcessToken(child.AsToken(), syntaxFacts, spans, cancellationToken);
                }
            }
        }

        private void ProcessToken(
            SyntaxToken token,
            ISyntaxFactsService syntaxFacts,
            ArrayBuilder<SpellCheckSpan> spans,
            CancellationToken cancellationToken)
        {
            ProcessTriviaList(token.LeadingTrivia, syntaxFacts, spans, cancellationToken);

            var syntaxKinds = syntaxFacts.SyntaxKinds;
            if (syntaxFacts.IsStringLiteral(token))
            {
                spans.Add(new SpellCheckSpan(GetSpanForString(token), SpellCheckKind.String));
            }
            else if (token.RawKind == syntaxKinds.SingleLineRawStringLiteralToken ||
                     token.RawKind == syntaxKinds.MultiLineRawStringLiteralToken)
            {
                spans.Add(new SpellCheckSpan(GetSpanForRawString(token), SpellCheckKind.String));
            }
            else if (token.RawKind == syntaxKinds.InterpolatedStringTextToken &&
                     token.Parent?.RawKind == syntaxKinds.InterpolatedStringText)
            {
                spans.Add(new SpellCheckSpan(token.Span, SpellCheckKind.String));
            }
            else if (token.RawKind == syntaxKinds.IdentifierToken &&
                IsDeclarationIdentifier(token))
            {
                spans.Add(new SpellCheckSpan(token.Span, SpellCheckKind.Identifier));
            }

            ProcessTriviaList(token.TrailingTrivia, syntaxFacts, spans, cancellationToken);
        }

        private void ProcessTriviaList(SyntaxTriviaList triviaList, ISyntaxFactsService syntaxFacts, ArrayBuilder<SpellCheckSpan> spans, CancellationToken cancellationToken)
        {
            foreach (var trivia in triviaList)
                ProcessTrivia(trivia, syntaxFacts, spans, cancellationToken);
        }

        private void ProcessTrivia(SyntaxTrivia trivia, ISyntaxFactsService syntaxFacts, ArrayBuilder<SpellCheckSpan> spans, CancellationToken cancellationToken)
        {
            if (syntaxFacts.IsRegularComment(trivia))
            {
                spans.Add(new SpellCheckSpan(GetSpanForComment(trivia), SpellCheckKind.Comment));
            }
            else if (syntaxFacts.IsDocumentationComment(trivia))
            {
                var structure = trivia.GetStructure()!;
                ProcessDocComment(structure, syntaxFacts, spans, cancellationToken);
            }
        }

        private void ProcessDocComment(SyntaxNode node, ISyntaxFactsService syntaxFacts, ArrayBuilder<SpellCheckSpan> spans, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var child in node.ChildNodesAndTokens())
            {
                if (child.IsNode)
                {
                    ProcessDocComment(child.AsNode()!, syntaxFacts, spans, cancellationToken);
                }
                else
                {
                    var token = child.AsToken();
                    if (token.RawKind == syntaxFacts.SyntaxKinds.XmlTextToken)
                        spans.Add(new SpellCheckSpan(token.Span, SpellCheckKind.Comment));
                }
            }
        }
    }
}
