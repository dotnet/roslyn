// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Wrapping.BinaryExpression;

internal abstract partial class AbstractBinaryExpressionWrapper<TBinaryExpressionSyntax>
{
    private sealed class BinaryExpressionCodeActionComputer :
        AbstractCodeActionComputer<AbstractBinaryExpressionWrapper<TBinaryExpressionSyntax>>
    {
        private readonly ImmutableArray<SyntaxNodeOrToken> _exprsAndOperators;

        /// <summary>
        /// trivia to place at the end of a node prior to a chunk that is wrapped.
        /// For C# this will just be a newline.  For VB this will include a line-
        /// continuation character.
        /// </summary>
        private readonly SyntaxTriviaList _newlineBeforeOperatorTrivia;

        /// <summary>
        /// The indent trivia to insert if we are trying to align wrapped code with the 
        /// start of the original expression.
        /// </summary>
        private readonly SyntaxTriviaList _indentAndAlignTrivia;

        /// <summary>
        /// The indent trivia to insert if we are trying to simply smart-indent all wrapped
        /// parts of the expression.
        /// </summary>
        private readonly SyntaxTriviaList _smartIndentTrivia;

        public BinaryExpressionCodeActionComputer(
            AbstractBinaryExpressionWrapper<TBinaryExpressionSyntax> service,
            Document document,
            SourceText originalSourceText,
            SyntaxWrappingOptions options,
            TBinaryExpressionSyntax binaryExpression,
            ImmutableArray<SyntaxNodeOrToken> exprsAndOperators,
            CancellationToken cancellationToken)
            : base(service, document, originalSourceText, options, cancellationToken)
        {
            _exprsAndOperators = exprsAndOperators;

            var generator = SyntaxGenerator.GetGenerator(document);

            _newlineBeforeOperatorTrivia = service.GetNewLineBeforeOperatorTrivia(NewLineTrivia);

            _indentAndAlignTrivia = new SyntaxTriviaList(generator.Whitespace(
                OriginalSourceText.GetOffset(binaryExpression.Span.Start)
                                  .CreateIndentationString(options.FormattingOptions.UseTabs, options.FormattingOptions.TabSize)));

            _smartIndentTrivia = new SyntaxTriviaList(generator.Whitespace(
                GetSmartIndentationAfter(_exprsAndOperators[1])));
        }

        protected override async Task<ImmutableArray<WrappingGroup>> ComputeWrappingGroupsAsync()
            => [new WrappingGroup(
                isInlinable: true,
                [
                    await GetWrapCodeActionAsync(align: false).ConfigureAwait(false),
                    await GetWrapCodeActionAsync(align: true).ConfigureAwait(false),
                    await GetUnwrapCodeActionAsync().ConfigureAwait(false),
                ])];

        private Task<WrapItemsAction> GetWrapCodeActionAsync(bool align)
            => TryCreateCodeActionAsync(GetWrapEdits(align), FeaturesResources.Wrapping,
                    align ? FeaturesResources.Wrap_and_align_expression : FeaturesResources.Wrap_expression);

        private Task<WrapItemsAction> GetUnwrapCodeActionAsync()
            => TryCreateCodeActionAsync(GetUnwrapEdits(), FeaturesResources.Wrapping, FeaturesResources.Unwrap_expression);

        private ImmutableArray<Edit> GetWrapEdits(bool align)
        {
            using var _ = ArrayBuilder<Edit>.GetInstance(out var result);
            var indentationTrivia = align ? _indentAndAlignTrivia : _smartIndentTrivia;

            for (var i = 1; i < _exprsAndOperators.Length; i += 2)
            {
                var left = _exprsAndOperators[i - 1].AsNode();
                var opToken = _exprsAndOperators[i].AsToken();
                var right = _exprsAndOperators[i + 1].AsNode();

                if (Options.OperatorPlacement == OperatorPlacementWhenWrappingPreference.BeginningOfLine)
                {
                    // convert: 
                    //      (a == b) && (c == d) to
                    //
                    //      (a == b)
                    //      && (c == d)
                    result.Add(Edit.UpdateBetween(left, _newlineBeforeOperatorTrivia, indentationTrivia, opToken));
                    result.Add(Edit.UpdateBetween(opToken, SingleWhitespaceTrivia, NoTrivia, right));
                }
                else
                {
                    // convert: 
                    //      (a == b) && (c == d) to
                    //
                    //      (a == b) &&
                    //      (c == d)
                    result.Add(Edit.UpdateBetween(left, SingleWhitespaceTrivia, NoTrivia, opToken));
                    result.Add(Edit.UpdateBetween(opToken, NewLineTrivia, indentationTrivia, right));
                }
            }

            return result.ToImmutableAndClear();
        }

        private ImmutableArray<Edit> GetUnwrapEdits()
        {
            var count = _exprsAndOperators.Length - 1;
            var result = new FixedSizeArrayBuilder<Edit>(count);

            for (var i = 0; i < count; i++)
            {
                result.Add(Edit.UpdateBetween(
                    _exprsAndOperators[i], SingleWhitespaceTrivia,
                    NoTrivia, _exprsAndOperators[i + 1]));
            }

            return result.MoveToImmutable();
        }
    }
}
