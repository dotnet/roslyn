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
using Microsoft.CodeAnalysis.CSharp.Extensions;
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
        /// <summary>
        /// Annotation used to find the closing brace location after formatting changes are applied.
        /// The closing brace location is then used as the caret location.
        /// </summary>
        private static readonly SyntaxAnnotation s_closingBraceSyntaxAnnotation = new(nameof(s_closingBraceSyntaxAnnotation));

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CurlyBraceCompletionService()
        {
        }

        protected override char OpeningBrace => CurlyBrace.OpenCharacter;

        protected override char ClosingBrace => CurlyBrace.CloseCharacter;

        public override Task<bool> AllowOverTypeAsync(BraceCompletionContext context, CancellationToken cancellationToken)
            => AllowOverTypeInUserCodeWithValidClosingTokenAsync(context, cancellationToken);

        public override async Task<BraceCompletionResult?> GetTextChangesAfterCompletionAsync(BraceCompletionContext context, IndentationOptions options, CancellationToken cancellationToken)
        {
            // After the closing brace is completed we need to format the span from the opening point to the closing point.
            // E.g. when the user triggers completion for an if statement ($$ is the caret location) we insert braces to get
            // if (true){$$}
            // We then need to format this to
            // if (true) { $$}

            if (!options.AutoFormattingOptions.FormatOnCloseBrace)
            {
                return null;
            }

            var (formattingChanges, finalCurlyBraceEnd) = await FormatTrackingSpanAsync(
                context.Document,
                context.OpeningPoint,
                context.ClosingPoint,
                // We're not trying to format the indented block here, so no need to pass in additional rules.
                braceFormattingIndentationRules: ImmutableArray<AbstractFormattingRule>.Empty,
                options,
                cancellationToken).ConfigureAwait(false);

            if (formattingChanges.IsEmpty)
            {
                return null;
            }

            // The caret location should be at the start of the closing brace character.
            var originalText = await context.Document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var formattedText = originalText.WithChanges(formattingChanges);
            var caretLocation = formattedText.Lines.GetLinePosition(finalCurlyBraceEnd - 1);

            return new BraceCompletionResult(formattingChanges, caretLocation);
        }

        public override async Task<BraceCompletionResult?> GetTextChangeAfterReturnAsync(
            BraceCompletionContext context,
            IndentationOptions options,
            CancellationToken cancellationToken)
        {
            var document = context.Document;
            var closingPoint = context.ClosingPoint;
            var openingPoint = context.OpeningPoint;
            var originalDocumentText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

            // check whether shape of the braces are what we support
            // shape must be either "{|}" or "{ }". | is where caret is. otherwise, we don't do any special behavior
            if (!ContainsOnlyWhitespace(originalDocumentText, openingPoint, closingPoint))
            {
                return null;
            }

            var openingPointLine = originalDocumentText.Lines.GetLineFromPosition(openingPoint).LineNumber;
            var closingPointLine = originalDocumentText.Lines.GetLineFromPosition(closingPoint).LineNumber;

            // If there are already multiple empty lines between the braces, don't do anything.
            // We need to allow a single empty line between the braces to account for razor scenarios where they insert a line.
            if (closingPointLine - openingPointLine > 2)
            {
                return null;
            }

            // If there is not already an empty line inserted between the braces, insert one.
            TextChange? newLineEdit = null;
            var textToFormat = originalDocumentText;
            if (closingPointLine - openingPointLine == 1)
            {
                var newLineString = options.FormattingOptions.GetOption(FormattingOptions2.NewLine);
                newLineEdit = new TextChange(new TextSpan(closingPoint - 1, 0), newLineString);
                textToFormat = originalDocumentText.WithChanges(newLineEdit.Value);

                // Modify the closing point location to adjust for the newly inserted line.
                closingPoint += newLineString.Length;
            }

            var braceFormattingIndentationRules = ImmutableArray.Create(
                BraceCompletionFormattingRule.ForIndentStyle(options.AutoFormattingOptions.IndentStyle));

            // Format the text that contains the newly inserted line.
            var (formattingChanges, newClosingPoint) = await FormatTrackingSpanAsync(
                document.WithText(textToFormat),
                openingPoint,
                closingPoint,
                braceFormattingIndentationRules,
                options,
                cancellationToken).ConfigureAwait(false);

            closingPoint = newClosingPoint;
            var formattedText = textToFormat.WithChanges(formattingChanges);

            // Get the empty line between the curly braces.
            var desiredCaretLine = GetLineBetweenCurlys(closingPoint, formattedText);
            Debug.Assert(desiredCaretLine.GetFirstNonWhitespacePosition() == null, "the line between the formatted braces is not empty");

            // Set the caret position to the properly indented column in the desired line.
            var newDocument = document.WithText(formattedText);
            var newDocumentText = await newDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var caretPosition = GetIndentedLinePosition(newDocument, newDocumentText, desiredCaretLine.LineNumber, cancellationToken);

            // The new line edit is calculated against the original text, d0, to get text d1.
            // The formatting edits are calculated against d1 to get text d2.
            // Merge the formatting and new line edits into a set of whitespace only text edits that all apply to d0.
            var overallChanges = newLineEdit != null ? GetMergedChanges(newLineEdit.Value, formattingChanges, formattedText) : formattingChanges;
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

            static ImmutableArray<TextChange> GetMergedChanges(TextChange newLineEdit, ImmutableArray<TextChange> formattingChanges, SourceText formattedText)
            {
                var newRanges = TextChangeRangeExtensions.Merge(
                    ImmutableArray.Create(newLineEdit.ToTextChangeRange()),
                    formattingChanges.SelectAsArray(f => f.ToTextChangeRange()));

                using var _ = ArrayBuilder<TextChange>.GetInstance(out var mergedChanges);
                var amountToShift = 0;
                foreach (var newRange in newRanges)
                {
                    var newTextChangeSpan = newRange.Span;
                    // Get the text to put in the text change by looking at the span in the formatted text.
                    // As the new range start is relative to the original text, we need to adjust it assuming the previous changes were applied
                    // to get the correct start location in the formatted text.
                    // E.g. with changes
                    //     1. Insert "hello" at 2
                    //     2. Insert "goodbye" at 3
                    // "goodbye" is after "hello" at location 3 + 5 (length of "hello") in the new text.
                    var newTextChangeText = formattedText.GetSubText(new TextSpan(newRange.Span.Start + amountToShift, newRange.NewLength)).ToString();
                    amountToShift += (newRange.NewLength - newRange.Span.Length);
                    mergedChanges.Add(new TextChange(newTextChangeSpan, newTextChangeText));
                }

                return mergedChanges.ToImmutable();
            }
        }

        public override async Task<bool> CanProvideBraceCompletionAsync(char brace, int openingPosition, Document document, CancellationToken cancellationToken)
        {
            // Only potentially valid for curly brace completion if not in an interpolation brace completion context.
            if (OpeningBrace == brace && await InterpolationBraceCompletionService.IsPositionInInterpolationContextAsync(document, openingPosition, cancellationToken).ConfigureAwait(false))
            {
                return false;
            }

            return await base.CanProvideBraceCompletionAsync(brace, openingPosition, document, cancellationToken).ConfigureAwait(false);
        }

        protected override bool IsValidOpeningBraceToken(SyntaxToken token)
            => token.IsKind(SyntaxKind.OpenBraceToken) && !token.Parent.IsKind(SyntaxKind.Interpolation);

        protected override bool IsValidClosingBraceToken(SyntaxToken token)
            => token.IsKind(SyntaxKind.CloseBraceToken);

        private static bool ContainsOnlyWhitespace(SourceText text, int openingPosition, int closingBraceEndPoint)
        {
            // Set the start point to the character after the opening brace.
            var start = openingPosition + 1;
            // Set the end point to the closing brace start character position.
            var end = closingBraceEndPoint - 1;

            for (var i = start; i < end; i++)
            {
                if (!char.IsWhiteSpace(text[i]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Formats the span between the opening and closing points, options permitting.
        /// Returns the text changes that should be applied to the input document to 
        /// get the formatted text and the end of the close curly brace in the formatted text.
        /// </summary>
        private static async Task<(ImmutableArray<TextChange> textChanges, int finalCurlyBraceEnd)> FormatTrackingSpanAsync(
            Document document,
            int openingPoint,
            int closingPoint,
            ImmutableArray<AbstractFormattingRule> braceFormattingIndentationRules,
            IndentationOptions options,
            CancellationToken cancellationToken)
        {
            // Annotate the original closing brace so we can find it after formatting.
            document = await GetDocumentWithAnnotatedClosingBraceAsync(document, closingPoint, cancellationToken).ConfigureAwait(false);

            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var startPoint = openingPoint;
            var endPoint = closingPoint;

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            // Only format outside of the completed braces if they're on the same line for array/collection/object initializer expressions.
            // Example:   `var x = new int[]{}`:
            // Correct:   `var x = new int[] {}`
            // Incorrect: `var x = new int[] { }`
            // This is a heuristic to prevent brace completion from breaking user expectation/muscle memory in common scenarios.
            // see bug Devdiv:823958
            if (text.Lines.GetLineFromPosition(startPoint) == text.Lines.GetLineFromPosition(endPoint))
            {
                var startToken = root.FindToken(startPoint, findInsideTrivia: true);
                if (startToken.IsKind(SyntaxKind.OpenBraceToken) &&
                    (startToken.Parent?.IsInitializerForArrayOrCollectionCreationExpression() == true ||
                     startToken.Parent is AnonymousObjectCreationExpressionSyntax))
                {
                    // Since the braces are next to each other the span to format is everything up to the opening brace start.
                    endPoint = startToken.SpanStart;
                }
            }

            if (options.AutoFormattingOptions.IndentStyle == FormattingOptions.IndentStyle.Smart)
            {
                // Set the formatting start point to be the beginning of the first word to the left 
                // of the opening brace location.
                // skip whitespace
                while (startPoint >= 0 && char.IsWhiteSpace(text[startPoint]))
                {
                    startPoint--;
                }

                // skip tokens in the first word to the left.
                startPoint--;
                while (startPoint >= 0 && !char.IsWhiteSpace(text[startPoint]))
                {
                    startPoint--;
                }
            }

            var spanToFormat = TextSpan.FromBounds(Math.Max(startPoint, 0), endPoint);
            var rules = document.GetFormattingRules(spanToFormat, braceFormattingIndentationRules);
            var services = document.Project.Solution.Workspace.Services;
            var result = Formatter.GetFormattingResult(
                root, SpecializedCollections.SingletonEnumerable(spanToFormat), services, options.FormattingOptions, rules, cancellationToken);
            if (result == null)
            {
                return (ImmutableArray<TextChange>.Empty, closingPoint);
            }

            var newRoot = result.GetFormattedRoot(cancellationToken);
            var newClosingPoint = newRoot.GetAnnotatedTokens(s_closingBraceSyntaxAnnotation).Single().SpanStart + 1;

            var textChanges = result.GetTextChanges(cancellationToken).ToImmutableArray();
            return (textChanges, newClosingPoint);

            static async Task<Document> GetDocumentWithAnnotatedClosingBraceAsync(Document document, int closingBraceEndPoint, CancellationToken cancellationToken)
            {
                var originalRoot = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var closeBraceToken = originalRoot.FindToken(closingBraceEndPoint - 1);
                Debug.Assert(closeBraceToken.IsKind(SyntaxKind.CloseBraceToken));

                var newCloseBraceToken = closeBraceToken.WithAdditionalAnnotations(s_closingBraceSyntaxAnnotation);
                var root = originalRoot.ReplaceToken(closeBraceToken, newCloseBraceToken);
                return document.WithSyntaxRoot(root);
            }
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

            public override AbstractFormattingRule WithOptions(SyntaxFormattingOptions options)
            {
                var cachedOptions = new CachedOptions(options.Options);

                if (cachedOptions == _options)
                {
                    return this;
                }

                return new BraceCompletionFormattingRule(_indentStyle, cachedOptions);
            }

            public override AdjustNewLinesOperation? GetAdjustNewLinesOperation(in SyntaxToken previousToken, in SyntaxToken currentToken, in NextGetAdjustNewLinesOperation nextOperation)
            {
                // If we're inside any of the following expressions check if the option for
                // braces on new lines in object / array initializers is set before we attempt
                // to move the open brace location to a new line.
                // new MyObject {
                // new List<int> {
                // int[] arr = {
                //           = new[] {
                //           = new int[] {
                if (currentToken.IsKind(SyntaxKind.OpenBraceToken) && currentToken.Parent.IsKind(
                    SyntaxKind.ObjectInitializerExpression,
                    SyntaxKind.CollectionInitializerExpression,
                    SyntaxKind.ArrayInitializerExpression,
                    SyntaxKind.ImplicitArrayCreationExpression))
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
                        // If the user has set block style indentation and we're in a valid brace pair
                        // then make sure we align the close brace to the open brace.
                        AddAlignIndentationOfTokensToBaseTokenOperation(list, node, bracePair.openBrace,
                            SpecializedCollections.SingletonEnumerable(bracePair.closeBrace), AlignTokensOption.AlignIndentationOfTokensToFirstTokenOfBaseTokenLine);
                    }
                }
            }

            public override void AddSuppressOperations(List<SuppressOperation> list, SyntaxNode node, in NextSuppressOperationAction nextOperation)
            {
                base.AddSuppressOperations(list, node, in nextOperation);

                // not sure exactly what is happening here, but removing the bellow causesthe indentation to be wrong.

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
