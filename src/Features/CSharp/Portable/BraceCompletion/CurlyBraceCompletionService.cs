// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.BraceCompletion;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Indentation;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.BraceCompletion
{
    [Export(LanguageNames.CSharp, typeof(IBraceCompletionService)), Shared]
    internal class CurlyBraceCompletionService : AbstractBraceCompletionService
    {
        private static readonly SyntaxAnnotation s_closingBraceSyntaxAnnotation = new("original closing brace");

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CurlyBraceCompletionService()
        {
        }

        protected override char OpeningBrace => CurlyBrace.OpenCharacter;

        protected override char ClosingBrace => CurlyBrace.CloseCharacter;

        public override async Task<bool> AllowOverTypeAsync(BraceCompletionContext context, CancellationToken cancellationToken)
        {
            return await CheckCurrentPositionAsync(context.Document, context.CaretLocation, cancellationToken).ConfigureAwait(false)
                && await CheckClosingTokenKindAsync(context.Document, context.ClosingPoint, cancellationToken).ConfigureAwait(false);
        }

        public override async Task<BraceCompletionResult?> GetTextChangesAfterCompletionAsync(BraceCompletionContext braceCompletionContext, CancellationToken cancellationToken)
        {
            // Format the span from the open brace location up to and including the closing brace location.
            var (formattingChanges, newClosingPoint) = await FormatTrackingSpanAsync(braceCompletionContext.Document, braceCompletionContext.OpeningPoint, braceCompletionContext.ClosingPoint,
                shouldHonorAutoFormattingOnCloseBraceOption: true, rules: null, cancellationToken).ConfigureAwait(false);

            if (formattingChanges.IsEmpty)
            {
                return null;
            }

            // The caret location should be at the start of the closing brace character.
            var originalText = await braceCompletionContext.Document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var formattedText = originalText.WithChanges(formattingChanges);
            var caretLocation = formattedText.Lines.GetLinePosition(newClosingPoint - 1);

            return new BraceCompletionResult(formattingChanges, caretLocation);
        }

        public override async Task<BraceCompletionResult?> GetTextChangeAfterReturnAsync(BraceCompletionContext context, CancellationToken cancellationToken)
        {
            var document = context.Document;
            var closingPoint = context.ClosingPoint;
            var openingPoint = context.OpeningPoint;
            var documentSnapshotText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            // Create a new source text that is not based on an editor snapshot so we can take advantage
            // of the multi-version text change merging in ChangedText instead of ChangedSnapshotText.
            var originalDocumentText = SourceText.From(documentSnapshotText.ToString(), documentSnapshotText.Encoding, documentSnapshotText.ChecksumAlgorithm);

            // check whether shape of the braces are what we support
            // shape must be either "{|}" or "{ }". | is where caret is. otherwise, we don't do any special behavior
            if (!ContainsOnlyWhitespace(originalDocumentText, openingPoint, closingPoint))
            {
                return null;
            }

            // Insert a new line between the braces.
            var newLineEdit = new TextChange(new TextSpan(closingPoint - 1, 0), Environment.NewLine);
            var textWithNewLine = originalDocumentText.WithChanges(newLineEdit);

            // Modify the closing point location to adjust for the newly inserted line.
            closingPoint += Environment.NewLine.Length;

            // Format the text that contains the newly inserted line.
            var (formattingChanges, newClosingPoint) = await FormatTrackingSpanAsync(
                document.WithText(textWithNewLine),
                openingPoint,
                closingPoint,
                shouldHonorAutoFormattingOnCloseBraceOption: false,
                rules: GetBraceFormattingRules(document),
                cancellationToken).ConfigureAwait(false);
            closingPoint = newClosingPoint;
            var formattedText = textWithNewLine.WithChanges(formattingChanges);

            // Get the empty line between the curly braces.
            var desiredCaretLine = GetLineBetweenCurlys(closingPoint, formattedText);
            Debug.Assert(desiredCaretLine.GetFirstNonWhitespacePosition() == null, "the line between the formatted braces is not empty");

            // Set the caret position to the properly indented column in the desired line.
            var newDocument = document.WithText(formattedText);
            var newDocumentText = await newDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var caretPosition = GetIndentedLinePosition(newDocument, newDocumentText, desiredCaretLine.LineNumber, cancellationToken);

            var overallChanges = formattedText.GetTextChanges(originalDocumentText).ToImmutableArray();
            return new BraceCompletionResult(overallChanges, caretPosition);

            static TextLine GetLineBetweenCurlys(int closingPosition, SourceText text)
            {
                var closingBraceLineNumber = text.Lines.GetLineFromPosition(closingPosition - 1).LineNumber;
                return text.Lines[closingBraceLineNumber - 1];
            }

            static LinePosition GetIndentedLinePosition(Document document, SourceText sourceText, int lineNumber, CancellationToken cancellationToken)
            {
                var indentationService = document.GetRequiredLanguageService<IIndentationService>();
                var indentation = indentationService.GetIndentation(document, lineNumber, cancellationToken);

                var baseLinePosition = sourceText.Lines.GetLinePosition(indentation.BasePosition);
                var offsetOfBacePosition = baseLinePosition.Character;
                var totalOffset = offsetOfBacePosition + indentation.Offset;
                var indentedLinePosition = new LinePosition(lineNumber, totalOffset);
                return indentedLinePosition;
            }
        }

        public override async Task<bool> IsValidForBraceCompletionAsync(char brace, int openingPosition, Document document, CancellationToken cancellationToken)
        {
            // Only potentially valid for curly brace completion if not in an interpolation brace completion context.
            if (OpeningBrace == brace && await InterpolationBraceCompletionService.IsPositionInInterpolationContextAsync(document, openingPosition, cancellationToken).ConfigureAwait(false))
            {
                return false;
            }

            return await base.IsValidForBraceCompletionAsync(brace, openingPosition, document, cancellationToken).ConfigureAwait(false);
        }

        protected override bool IsValidOpeningBraceToken(SyntaxToken token) => token.IsKind(SyntaxKind.OpenBraceToken) && !token.Parent.IsKind(SyntaxKind.Interpolation);

        protected override bool IsValidClosingBraceToken(SyntaxToken token) => token.IsKind(SyntaxKind.CloseBraceToken);

        private bool ContainsOnlyWhitespace(SourceText text, int openingPosition, int closingPosition)
        {
            var start = openingPosition;
            start = text[start] == OpeningBrace ? start + 1 : start;

            var end = closingPosition - 1;
            end = text[end] == ClosingBrace ? end - 1 : end;

            if (!PositionInSnapshot(start, text) ||
                !PositionInSnapshot(end, text))
            {
                return false;
            }

            for (var i = start; i <= end; i++)
            {
                if (!char.IsWhiteSpace(text[i]))
                {
                    return false;
                }
            }

            return true;

            static int GetValueInValidRange(int value, int smallest, int largest)
            => Math.Max(smallest, Math.Min(value, largest));

            static bool PositionInSnapshot(int position, SourceText text)
                => GetValueInValidRange(position, 0, Math.Max(0, text.Length - 1)) == position;
        }

        /// <summary>
        /// Formats the span between the opening and closing points, options permitting.
        /// Returns the text changes that should be applied to the input document to 
        /// get the formatted text and the new closing point in the formatted text.
        /// </summary>
        private static async Task<(ImmutableArray<TextChange> TextChanges, int NewClosingPoint)> FormatTrackingSpanAsync(
            Document document,
            int openingPoint,
            int closingPoint,
            bool shouldHonorAutoFormattingOnCloseBraceOption,
            ImmutableArray<AbstractFormattingRule>? rules,
            CancellationToken cancellationToken)
        {
            var option = document.Project.Solution.Options.GetOption(BraceCompletionOptions.AutoFormattingOnCloseBrace, document.Project.Language);
            if (!option && shouldHonorAutoFormattingOnCloseBraceOption)
            {
                return (ImmutableArray<TextChange>.Empty, closingPoint);
            }

            // Annotate the original closing brace so we can find it after formatting.
            document = await GetDocumentWithAnnotatedClosingBraceAsync(document, closingPoint, cancellationToken).ConfigureAwait(false);

            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var startPoint = openingPoint;
            var endPoint = closingPoint;

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            // Do not format within the braces if they're on the same line for array/collection/object initializer expressions.
            // This is a heuristic to prevent brace completion from breaking user expectation/muscle memory in common scenarios.
            // see bug Devdiv:823958
            if (text.Lines.GetLineFromPosition(startPoint) == text.Lines.GetLineFromPosition(endPoint))
            {
                var startToken = root.FindToken(startPoint, findInsideTrivia: true);
                if (startToken.IsKind(SyntaxKind.OpenBraceToken) &&
                    (startToken.Parent?.IsInitializerForArrayOrCollectionCreationExpression() == true ||
                     startToken.Parent is AnonymousObjectCreationExpressionSyntax))
                {
                    // format everything but the brace pair.
                    var endToken = root.FindToken(endPoint, findInsideTrivia: true);
                    if (endToken.IsKind(SyntaxKind.CloseBraceToken))
                    {
                        endPoint -= endToken.Span.Length + startToken.Span.Length;
                    }
                }
            }

            var documentOptions = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            var style = documentOptions.GetOption(FormattingOptions.SmartIndent);

            if (style == FormattingOptions.IndentStyle.Smart)
            {
                // skip whitespace
                while (startPoint >= 0 && char.IsWhiteSpace(text[startPoint]))
                {
                    startPoint--;
                }

                // skip token
                startPoint--;
                while (startPoint >= 0 && !char.IsWhiteSpace(text[startPoint]))
                {
                    startPoint--;
                }
            }

            var spanToFormat = TextSpan.FromBounds(Math.Max(startPoint, 0), endPoint);
            rules = document.GetFormattingRules(rules, spanToFormat);

            var result = Formatter.GetFormattingResult(root, SpecializedCollections.SingletonEnumerable(spanToFormat), document.Project.Solution.Workspace, documentOptions, rules, cancellationToken);
            if (result == null)
            {
                return (ImmutableArray<TextChange>.Empty, closingPoint);
            }
            var newRoot = result.GetFormattedRoot(cancellationToken);
            var newClosingPoint = newRoot.GetAnnotatedTokens(s_closingBraceSyntaxAnnotation).Single().SpanStart + 1;

            var textChanges = result.GetTextChanges(cancellationToken).ToImmutableArray();
            return (textChanges, newClosingPoint);

            static async Task<Document> GetDocumentWithAnnotatedClosingBraceAsync(Document document, int closingPoint, CancellationToken cancellationToken)
            {
                var originalRoot = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var closeBraceToken = originalRoot.FindToken(closingPoint - 1);
                var newCloseBraceToken = closeBraceToken.WithAdditionalAnnotations(s_closingBraceSyntaxAnnotation);
                var root = originalRoot.ReplaceToken(closeBraceToken, newCloseBraceToken);
                return document.WithSyntaxRoot(root);
            }
        }

        private static ImmutableArray<AbstractFormattingRule> GetBraceFormattingRules(Document document)
        {
            var indentStyle = document.GetOptionsAsync(CancellationToken.None).WaitAndGetResult_CanCallOnBackground(CancellationToken.None).GetOption(FormattingOptions.SmartIndent);
            return ImmutableArray.Create(BraceCompletionFormattingRule.ForIndentStyle(indentStyle)).AddRange(Formatter.GetDefaultFormattingRules(document));
        }

        private sealed class BraceCompletionFormattingRule : BaseFormattingRule
        {
            private static readonly Predicate<SuppressOperation> s_predicate = o => o == null || o.Option.IsOn(SuppressOption.NoWrapping);

            private static readonly ImmutableArray<BraceCompletionFormattingRule> s_instances = ImmutableArray.Create(
                new BraceCompletionFormattingRule(FormattingOptions.IndentStyle.None),
                new BraceCompletionFormattingRule(FormattingOptions.IndentStyle.Block),
                new BraceCompletionFormattingRule(FormattingOptions.IndentStyle.Smart));

            private readonly FormattingOptions.IndentStyle _indentStyle;
            private readonly CachedOptions _options;

            public BraceCompletionFormattingRule(FormattingOptions.IndentStyle indentStyle)
                : this(indentStyle, new CachedOptions(null))
            {
            }

            private BraceCompletionFormattingRule(FormattingOptions.IndentStyle indentStyle, CachedOptions options)
            {
                _indentStyle = indentStyle;
                _options = options;
            }

            public static AbstractFormattingRule ForIndentStyle(FormattingOptions.IndentStyle indentStyle)
            {
                Debug.Assert(s_instances[(int)indentStyle]._indentStyle == indentStyle);
                return s_instances[(int)indentStyle];
            }

            public override AbstractFormattingRule WithOptions(AnalyzerConfigOptions options)
            {
                var cachedOptions = new CachedOptions(options);

                if (cachedOptions == _options)
                {
                    return this;
                }

                return new BraceCompletionFormattingRule(_indentStyle, cachedOptions);
            }

            public override AdjustNewLinesOperation? GetAdjustNewLinesOperation(in SyntaxToken previousToken, in SyntaxToken currentToken, in NextGetAdjustNewLinesOperation nextOperation)
            {
                // Eg Cases -
                // new MyObject {
                // new List<int> {
                // int[] arr = {
                //           = new[] {
                //           = new int[] {
                if (currentToken.IsKind(SyntaxKind.OpenBraceToken) && currentToken.Parent != null &&
                (currentToken.Parent.Kind() == SyntaxKind.ObjectInitializerExpression ||
                currentToken.Parent.Kind() == SyntaxKind.CollectionInitializerExpression ||
                currentToken.Parent.Kind() == SyntaxKind.ArrayInitializerExpression ||
                currentToken.Parent.Kind() == SyntaxKind.ImplicitArrayCreationExpression))
                {
                    if (_options.NewLinesForBracesInObjectCollectionArrayInitializers)
                    {
                        return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines);
                    }
                    else
                    {
                        return null;
                    }
                }

                return base.GetAdjustNewLinesOperation(in previousToken, in currentToken, in nextOperation);
            }

            public override void AddAlignTokensOperations(List<AlignTokensOperation> list, SyntaxNode node, in NextAlignTokensOperationAction nextOperation)
            {
                base.AddAlignTokensOperations(list, node, in nextOperation);
                if (_indentStyle == FormattingOptions.IndentStyle.Block)
                {
                    var bracePair = node.GetBracePair();
                    if (bracePair.IsValidBracePair())
                    {
                        AddAlignIndentationOfTokensToBaseTokenOperation(list, node, bracePair.Item1, SpecializedCollections.SingletonEnumerable(bracePair.Item2), AlignTokensOption.AlignIndentationOfTokensToFirstTokenOfBaseTokenLine);
                    }
                }
            }

            public override void AddSuppressOperations(List<SuppressOperation> list, SyntaxNode node, in NextSuppressOperationAction nextOperation)
            {
                base.AddSuppressOperations(list, node, in nextOperation);

                // remove suppression rules for array and collection initializer
                if (node.IsInitializerForArrayOrCollectionCreationExpression())
                {
                    // remove any suppression operation
                    list.RemoveAll(s_predicate);
                }
            }

            private readonly struct CachedOptions : IEquatable<CachedOptions>
            {
                public readonly bool NewLinesForBracesInObjectCollectionArrayInitializers;

                public CachedOptions(AnalyzerConfigOptions? options)
                {
                    NewLinesForBracesInObjectCollectionArrayInitializers = GetOptionOrDefault(options, CSharpFormattingOptions2.NewLinesForBracesInObjectCollectionArrayInitializers);
                }

                public static bool operator ==(CachedOptions left, CachedOptions right)
                    => left.Equals(right);

                public static bool operator !=(CachedOptions left, CachedOptions right)
                    => !(left == right);

                private static T GetOptionOrDefault<T>(AnalyzerConfigOptions? options, Option2<T> option)
                {
                    if (options is null)
                        return option.DefaultValue;

                    return options.GetOption(option);
                }

                public override bool Equals(object? obj)
                    => obj is CachedOptions options && Equals(options);

                public bool Equals(CachedOptions other)
                {
                    return NewLinesForBracesInObjectCollectionArrayInitializers == other.NewLinesForBracesInObjectCollectionArrayInitializers;
                }

                public override int GetHashCode()
                {
                    var hashCode = 0;
                    hashCode = (hashCode << 1) + (NewLinesForBracesInObjectCollectionArrayInitializers ? 1 : 0);
                    return hashCode;
                }
            }
        }
    }
}
