// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
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

        protected abstract string? GetClassificationForIdentifier(SyntaxToken token);
        protected abstract TextSpan GetSpanForComment(SyntaxTrivia trivia);
        protected abstract TextSpan GetSpanForRawString(SyntaxToken token);
        protected abstract TextSpan GetSpanForString(SyntaxToken token);

        private static void AddSpan(ArrayBuilder<SpellCheckSpan> spans, SpellCheckSpan span)
        {
            if (span.TextSpan.Length > 0)
                spans.Add(span);
        }

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
                AddSpan(spans, new SpellCheckSpan(GetSpanForString(token), SpellCheckKind.String));
            }
            else if (token.RawKind == syntaxKinds.SingleLineRawStringLiteralToken ||
                     token.RawKind == syntaxKinds.MultiLineRawStringLiteralToken)
            {
                AddSpan(spans, new SpellCheckSpan(GetSpanForRawString(token), SpellCheckKind.String));
            }
            else if (token.RawKind == syntaxKinds.InterpolatedStringTextToken &&
                     token.Parent?.RawKind == syntaxKinds.InterpolatedStringText)
            {
                AddSpan(spans, new SpellCheckSpan(token.Span, SpellCheckKind.String));
            }
            else if (token.RawKind == syntaxKinds.IdentifierToken)
            {
                TryAddSpanForIdentifier(token, spans);
            }

            ProcessTriviaList(token.TrailingTrivia, syntaxFacts, spans, cancellationToken);
        }

        private void TryAddSpanForIdentifier(SyntaxToken token, ArrayBuilder<SpellCheckSpan> spans)
        {
            // Leverage syntactic classification which already has to determine if an identifier token is the name of
            // some construct.
            var classification = this.GetClassificationForIdentifier(token);
            switch (classification)
            {
                case ClassificationTypeNames.ClassName:
                case ClassificationTypeNames.RecordClassName:
                case ClassificationTypeNames.DelegateName:
                case ClassificationTypeNames.EnumName:
                case ClassificationTypeNames.InterfaceName:
                case ClassificationTypeNames.ModuleName:
                case ClassificationTypeNames.StructName:
                case ClassificationTypeNames.RecordStructName:
                case ClassificationTypeNames.TypeParameterName:
                case ClassificationTypeNames.FieldName:
                case ClassificationTypeNames.EnumMemberName:
                case ClassificationTypeNames.ConstantName:
                case ClassificationTypeNames.LocalName:
                case ClassificationTypeNames.ParameterName:
                case ClassificationTypeNames.MethodName:
                case ClassificationTypeNames.ExtensionMethodName:
                case ClassificationTypeNames.PropertyName:
                case ClassificationTypeNames.EventName:
                case ClassificationTypeNames.NamespaceName:
                case ClassificationTypeNames.LabelName:
                    break;
                default:
                    return;
            }

            AddSpan(spans, new SpellCheckSpan(token.Span, SpellCheckKind.Identifier));
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
                AddSpan(spans, new SpellCheckSpan(GetSpanForComment(trivia), SpellCheckKind.Comment));
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
                    if (token.RawKind == syntaxFacts.SyntaxKinds.XmlTextLiteralToken)
                        AddSpan(spans, new SpellCheckSpan(token.Span, SpellCheckKind.Comment));
                }
            }
        }
    }
}
