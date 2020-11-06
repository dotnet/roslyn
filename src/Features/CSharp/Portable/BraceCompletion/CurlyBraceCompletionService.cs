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
            var formattingChanges = await FormatTrackingSpanAsync(braceCompletionContext.Document, braceCompletionContext.OpeningPoint, braceCompletionContext.ClosingPoint,
                shouldHonorAutoFormattingOnCloseBraceOption: true, cancellationToken).ConfigureAwait(false);

            if (formattingChanges.IsEmpty)
            {
                return null;
            }

            var originalText = await braceCompletionContext.Document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var formattedText = originalText.WithChanges(formattingChanges);

            // The caret location should be at the start of the closing brace character.
            var newCaretLocation = GetNewCloseBraceLocation(braceCompletionContext.OpeningPoint, formattingChanges, formattedText);
            return new BraceCompletionResult(formattedText, ImmutableArray.Create(formattingChanges), newCaretLocation);
        }

        public override async Task<BraceCompletionResult?> GetTextChangeAfterReturnAsync(BraceCompletionContext context, CancellationToken cancellationToken, bool supportsVirtualSpace = true)
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

            using var _ = ArrayBuilder<ImmutableArray<TextChange>>.GetInstance(out var changes);

            // Insert a new line between the braces.
            var newLineEdit = new TextChange(new TextSpan(closingPoint - 1, 0), Environment.NewLine);
            var textWithNewLine = originalDocumentText.WithChanges(newLineEdit);
            changes.Add(ImmutableArray.Create(newLineEdit));

            // Modify the closing point location to adjust for the newly inserted line.
            closingPoint += Environment.NewLine.Length;
            // Retrieve the formatted text with the new line.
            var formattingChanges = await FormatTrackingSpanAsync(
                document.WithText(textWithNewLine),
                openingPoint,
                closingPoint,
                shouldHonorAutoFormattingOnCloseBraceOption: false,
                cancellationToken,
                rules: GetBraceFormattingRules(document)).ConfigureAwait(false);

            var formattedText = textWithNewLine.WithChanges(formattingChanges);
            changes.Add(formattingChanges);

            // Get the empty line that is between the curly braces.
            var desiredCaretLine = GetLineNumberBetweenCurlys(openingPoint, formattingChanges, formattedText);
            Debug.Assert(desiredCaretLine.GetFirstNonWhitespacePosition() == null, "the line between the formatted braces is not empty");

            if (supportsVirtualSpace)
            {
                return new BraceCompletionResult(formattedText, changes.ToImmutable(), desiredCaretLine.Start);
            }
            else
            {
                // The caller does not support virtual spaces, so we have to insert actual whitespace into the document.

                // Calculate the desired indentation.
                var indentationService = document.GetRequiredLanguageService<IIndentationService>();
                var indentation = indentationService.GetIndentation(document.WithText(formattedText), desiredCaretLine.LineNumber, cancellationToken);

                // Insert whitespace for the indentation.
                var documentOptions = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
                var indentedTextChange = GetIndentTextChange(indentation, documentOptions);
                var indentedText = formattedText.WithChanges(indentedTextChange);
                changes.Add(ImmutableArray.Create(indentedTextChange));

                // The caret should be placed at the end of the indented line.
                var caretLocation = indentedText.Lines[desiredCaretLine.LineNumber].End;
                return new BraceCompletionResult(indentedText, changes.ToImmutable(), caretLocation);
            }

            TextLine GetLineNumberBetweenCurlys(int openingPosition, ImmutableArray<TextChange> textChanges, SourceText text)
            {
                var closingParenLocation = GetNewCloseBraceLocation(openingPosition, textChanges, text);
                if (closingParenLocation != -1)
                {
                    return text.Lines[text.Lines.GetLineFromPosition(closingParenLocation).LineNumber - 1];
                }

                return text.Lines.GetLineFromPosition(openingPosition);
            }

            static TextChange GetIndentTextChange(IndentationResult indentation, DocumentOptionSet documentOptions)
            {
                var indentText = indentation.Offset.CreateIndentationString(documentOptions.GetOption(FormattingOptions.UseTabs), documentOptions.GetOption(FormattingOptions.TabSize));
                return new TextChange(new TextSpan(indentation.BasePosition, 0), indentText);
            }
        }

        public override async Task<bool> IsValidForBraceCompletionAsync(char brace, int openingPosition, Document document, CancellationToken cancellationToken)
        {
            // Only potentially valid for curly brace completion if not in an interpolation brace completion context.
            if (OpeningBrace == brace && await InterpolationBraceCompletionService.IsCurlyBraceInInterpolationContextAsync(document, openingPosition, cancellationToken).ConfigureAwait(false))
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

        private int GetNewCloseBraceLocation(int originalOpeningPoint, ImmutableArray<TextChange> textChanges, SourceText text)
        {
            // The closing point is the first matching } that occurs after our original start point
            // or the min text change span start (to capture cases where whitespace is inserted before the original point)
            var startPoint = Math.Min(originalOpeningPoint, textChanges.OrderBy(tc => tc.Span.Start).FirstOrDefault().Span.Start);
            var braceLocation = text.IndexOf(ClosingBrace.ToString(), startPoint, caseSensitive: false);
            // Braces were just inserted so we should never be unable to find them.
            Debug.Assert(braceLocation != -1, $"couldn't find location of {ClosingBrace} after {startPoint}");

            return braceLocation;
        }

        private static async Task<ImmutableArray<TextChange>> FormatTrackingSpanAsync(Document document, int openingPoint, int closingPoint, bool shouldHonorAutoFormattingOnCloseBraceOption, CancellationToken cancellationToken, ImmutableArray<AbstractFormattingRule>? rules = null)
        {
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var option = document.Project.Solution.Options.GetOption(BraceCompletionOptions.AutoFormattingOnCloseBrace, document.Project.Language);
            if (!option && shouldHonorAutoFormattingOnCloseBraceOption)
            {
                return ImmutableArray<TextChange>.Empty;
            }

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
            rules = GetDefaultFormattingRules(document, rules, spanToFormat);

            var changes = Formatter.GetFormattedTextChanges(root, SpecializedCollections.SingletonEnumerable(spanToFormat), document.Project.Solution.Workspace, documentOptions, rules, cancellationToken);
            return changes.ToImmutableArray();

            // todo get from ITextSnapshotExtensions
            static ImmutableArray<AbstractFormattingRule> GetDefaultFormattingRules(Document document, ImmutableArray<AbstractFormattingRule>? rules, TextSpan span)
            {
                var workspace = document.Project.Solution.Workspace;
                var formattingRuleFactory = workspace.Services.GetRequiredService<IHostDependentFormattingRuleFactoryService>();
                var position = (span.Start + span.End) / 2;

                return ImmutableArray.Create(formattingRuleFactory.CreateRule(document, position)).AddRange(rules ?? Formatter.GetDefaultFormattingRules(document));
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
