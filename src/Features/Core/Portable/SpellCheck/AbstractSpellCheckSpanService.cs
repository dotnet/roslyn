// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.SpellCheck
{
    internal abstract class AbstractSpellCheckSpanService(char? escapeCharacter) : ISpellCheckSpanService
    {
        private readonly char? _escapeCharacter = escapeCharacter;

        public async Task<ImmutableArray<SpellCheckSpan>> GetSpansAsync(Document document, CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            return GetSpans();

            // Broken into its own method as it uses a ref-struct, which isn't allowed with the async call above.
            ImmutableArray<SpellCheckSpan> GetSpans()
            {
                var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
                var classifier = document.GetRequiredLanguageService<ISyntaxClassificationService>();
                var virtualCharService = document.GetRequiredLanguageService<IVirtualCharLanguageService>();

                using var _ = ArrayBuilder<SpellCheckSpan>.GetInstance(out var spans);

                var worker = new Worker(this, syntaxFacts, classifier, virtualCharService, spans);
                worker.Recurse(root, cancellationToken);

                return spans.ToImmutable();
            }
        }

        private readonly ref struct Worker(
            AbstractSpellCheckSpanService spellCheckSpanService,
            ISyntaxFactsService syntaxFacts,
            ISyntaxClassificationService classifier,
            IVirtualCharLanguageService virtualCharService,
            ArrayBuilder<SpellCheckSpan> spans)
        {
            private readonly AbstractSpellCheckSpanService _spellCheckSpanService = spellCheckSpanService;
            private readonly ISyntaxFactsService _syntaxFacts = syntaxFacts;
            private readonly ISyntaxKinds _syntaxKinds = syntaxFacts.SyntaxKinds;
            private readonly ISyntaxClassificationService _classifier = classifier;
            private readonly IVirtualCharLanguageService _virtualCharService = virtualCharService;
            private readonly ArrayBuilder<SpellCheckSpan> _spans = spans;

            private void AddSpan(SpellCheckSpan span)
            {
                if (span.TextSpan.Length > 0)
                    _spans.Add(span);
            }

            public void Recurse(SyntaxNode root, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();

                using var _ = ArrayBuilder<SyntaxNodeOrToken>.GetInstance(out var stack);
                stack.Push(root);

                while (stack.Count > 0)
                {
                    var current = stack.Pop();

                    if (current.IsToken)
                    {
                        ProcessToken(current.AsToken(), cancellationToken);
                    }
                    else if (current.IsNode)
                    {
                        foreach (var child in current.ChildNodesAndTokens().Reverse())
                            stack.Push(child);
                    }
                }
            }

            private void ProcessToken(
                SyntaxToken token,
                CancellationToken cancellationToken)
            {
                ProcessTriviaList(token.LeadingTrivia, cancellationToken);

                if (_syntaxFacts.IsStringLiteral(token))
                {
                    AddStringSpans(token, canContainEscapes: !_syntaxFacts.IsVerbatimStringLiteral(token));
                }
                else if (
                    token.RawKind == _syntaxKinds.SingleLineRawStringLiteralToken ||
                    token.RawKind == _syntaxKinds.MultiLineRawStringLiteralToken)
                {
                    AddStringSpans(token, canContainEscapes: false);
                }
                else if (token.RawKind == _syntaxKinds.InterpolatedStringTextToken &&
                         token.Parent?.RawKind == _syntaxKinds.InterpolatedStringText)
                {
                    AddStringSpans(token, canContainEscapes: !_syntaxFacts.IsVerbatimInterpolatedStringExpression(token.Parent.Parent));
                }
                else if (token.RawKind == _syntaxKinds.IdentifierToken)
                {
                    TryAddSpanForIdentifier(token);
                }

                ProcessTriviaList(token.TrailingTrivia, cancellationToken);
            }

            private void AddStringSpans(SyntaxToken token, bool canContainEscapes)
            {
                // Don't bother with strings that are in error.  This is both because we can't properly break them into
                // pieces, and also because a string in error often may be grabbing more of the file than intended, and
                // we don't want to start spell checking normal code that is caught up in the middle of being edited.
                if (token.ContainsDiagnostics)
                    return;

                // First, see if there's actually the presence of an escape character in the string token.  If not, we
                // can just provide the entire string as-is to the caller to spell check since there's no escapes for
                // them to be confused by.
                //
                // Note: .Text on a string token is non-allocating.  It is captured at the time of token creation and
                // held by the token.
                var escapeChar = _spellCheckSpanService._escapeCharacter;
                if (canContainEscapes &&
                    escapeChar != null &&
                    token.Text.AsSpan().IndexOf(escapeChar.Value) >= 0)
                {
                    AddStringSubSpans(token);
                }
                else
                {
                    // Just add the full string span as is and let the client handle it.
                    AddSpan(new SpellCheckSpan(token.Span, SpellCheckKind.String));
                }
            }

            private void AddStringSubSpans(SyntaxToken token)
            {
                var virtualChars = _virtualCharService.TryConvertToVirtualChars(token);
                if (virtualChars.IsDefaultOrEmpty)
                    return;

                // find the sequences of letters in a row that should be spell checked. if any part of that sequence is
                // an escaped character (like `\u0065`) then filter that out.  The platform won't be able to understand
                // this word and will report bogus spell checking mistakes.
                var currentCharIndex = 0;
                while (currentCharIndex < virtualChars.Length)
                {
                    var currentChar = virtualChars[currentCharIndex];
                    if (!currentChar.IsLetter)
                    {
                        currentCharIndex++;
                        continue;
                    }

                    var spanStart = currentChar.Span.Start;
                    var spanEnd = currentChar.Span.End;

                    var seenEscape = false;
                    while (
                        currentCharIndex < virtualChars.Length &&
                        virtualChars[currentCharIndex] is { IsLetter: true } endChar)
                    {
                        // we know if we've seen a letter that is an escape character if it takes more than two actual
                        // characters in the source.
                        seenEscape = seenEscape || endChar.Span.Length > 1;
                        spanEnd = endChar.Span.End;
                        currentCharIndex++;
                    }

                    if (!seenEscape)
                        AddSpan(new SpellCheckSpan(TextSpan.FromBounds(spanStart, spanEnd), SpellCheckKind.String));
                }
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
                using var _ = ArrayBuilder<SyntaxNodeOrToken>.GetInstance(out var stack);
                stack.Push(node);

                while (stack.Count > 0)
                {
                    var current = stack.Pop();
                    if (current.IsToken)
                    {
                        var token = current.AsToken();
                        if (token.RawKind == _syntaxFacts.SyntaxKinds.XmlTextLiteralToken)
                            AddSpan(new SpellCheckSpan(token.Span, SpellCheckKind.Comment));
                    }
                    else if (current.IsNode)
                    {
                        foreach (var child in current.ChildNodesAndTokens().Reverse())
                            stack.Push(child);
                    }
                }
            }
        }
    }
}
