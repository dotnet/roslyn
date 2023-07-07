// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.SpellCheck
{
    internal abstract class AbstractSpellCheckSpanService : ISpellCheckSpanService
    {
        public async Task<ImmutableArray<SpellCheckSpan>> GetSpansAsync(Document document, CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            return GetSpans(document, root, cancellationToken);
        }

        private static ImmutableArray<SpellCheckSpan> GetSpans(Document document, SyntaxNode root, CancellationToken cancellationToken)
        {
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            var classifier = document.GetRequiredLanguageService<ISyntaxClassificationService>();
            using var _ = ArrayBuilder<SpellCheckSpan>.GetInstance(out var spans);

            var worker = new Worker(syntaxFacts, classifier, spans);
            worker.Recurse(root, cancellationToken);

            return spans.ToImmutable();
        }

        private readonly ref struct Worker(ISyntaxFactsService syntaxFacts, ISyntaxClassificationService classifier, ArrayBuilder<SpellCheckSpan> spans)
        {
            private readonly ISyntaxFactsService _syntaxFacts = syntaxFacts;
            private readonly ISyntaxKinds _syntaxKinds = syntaxFacts.SyntaxKinds;
            private readonly ISyntaxClassificationService _classifier = classifier;
            private readonly ArrayBuilder<SpellCheckSpan> _spans = spans;

            private void AddSpan(SpellCheckSpan span)
            {
                if (span.TextSpan.Length > 0)
                    _spans.Add(span);
            }

            public void Recurse(SyntaxNode root, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                foreach (var child in root.ChildNodesAndTokens())
                {
                    if (child.IsNode)
                    {
                        Recurse(child.AsNode()!, cancellationToken);
                    }
                    else
                    {
                        ProcessToken(child.AsToken(), cancellationToken);
                    }
                }
            }

            private void ProcessToken(
                SyntaxToken token,
                CancellationToken cancellationToken)
            {
                ProcessTriviaList(token.LeadingTrivia, cancellationToken);

                if (_syntaxFacts.IsStringLiteral(token) ||
                    token.RawKind == _syntaxKinds.SingleLineRawStringLiteralToken ||
                    token.RawKind == _syntaxKinds.MultiLineRawStringLiteralToken)
                {
                    AddSpan(new SpellCheckSpan(token.Span, SpellCheckKind.String));
                }
                else if (token.RawKind == _syntaxKinds.InterpolatedStringTextToken &&
                         token.Parent?.RawKind == _syntaxKinds.InterpolatedStringText)
                {
                    AddSpan(new SpellCheckSpan(token.Span, SpellCheckKind.String));
                }
                else if (token.RawKind == _syntaxKinds.IdentifierToken)
                {
                    TryAddSpanForIdentifier(token);
                }

                ProcessTriviaList(token.TrailingTrivia, cancellationToken);
            }

            private void TryAddSpanForIdentifier(SyntaxToken token)
            {
                // Leverage syntactic classification which already has to determine if an identifier token is the name of
                // some construct.
                var classification = _classifier.GetSyntacticClassificationForIdentifier(token);
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
                        AddSpan(new SpellCheckSpan(token.Span, SpellCheckKind.Identifier));
                        break;
                }
            }

            private void ProcessTriviaList(SyntaxTriviaList triviaList, CancellationToken cancellationToken)
            {
                foreach (var trivia in triviaList)
                    ProcessTrivia(trivia, cancellationToken);
            }

            private void ProcessTrivia(SyntaxTrivia trivia, CancellationToken cancellationToken)
            {
                if (_syntaxFacts.IsRegularComment(trivia))
                {
                    AddSpan(new SpellCheckSpan(trivia.Span, SpellCheckKind.Comment));
                }
                else if (_syntaxFacts.IsDocumentationComment(trivia))
                {
                    ProcessDocComment(trivia.GetStructure()!, cancellationToken);
                }
            }

            private void ProcessDocComment(SyntaxNode node, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();

                foreach (var child in node.ChildNodesAndTokens())
                {
                    if (child.IsNode)
                    {
                        ProcessDocComment(child.AsNode()!, cancellationToken);
                    }
                    else
                    {
                        var token = child.AsToken();
                        if (token.RawKind == _syntaxFacts.SyntaxKinds.XmlTextLiteralToken)
                            AddSpan(new SpellCheckSpan(token.Span, SpellCheckKind.Comment));
                    }
                }
            }
        }
    }
}
