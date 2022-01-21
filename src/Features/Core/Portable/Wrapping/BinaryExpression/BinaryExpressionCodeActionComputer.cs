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
using Microsoft.CodeAnalysis.Indentation;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Wrapping.BinaryExpression
{
    internal partial class AbstractBinaryExpressionWrapper<TBinaryExpressionSyntax>
    {
        private class BinaryExpressionCodeActionComputer :
            AbstractCodeActionComputer<AbstractBinaryExpressionWrapper<TBinaryExpressionSyntax>>
        {
            private readonly ImmutableArray<SyntaxNodeOrToken> _exprsAndOperators;
            private readonly OperatorPlacementWhenWrappingPreference _preference;

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

            private BinaryExpressionCodeActionComputer(
                AbstractBinaryExpressionWrapper<TBinaryExpressionSyntax> service,
                Document document,
                SourceText originalSourceText,
                IndentationOptions options,
                ImmutableArray<SyntaxNodeOrToken> exprsAndOperators,
                SyntaxTriviaList indentAndAlignTrivia,
                SyntaxTriviaList smartIndentTrivia)
                : base(service, document, originalSourceText, options)
            {
                _exprsAndOperators = exprsAndOperators;
                _preference = options.FormattingOptions.GetOption(CodeStyleOptions2.OperatorPlacementWhenWrapping);
                _newlineBeforeOperatorTrivia = service.GetNewLineBeforeOperatorTrivia(NewLineTrivia);
                _indentAndAlignTrivia = indentAndAlignTrivia;
                _smartIndentTrivia = smartIndentTrivia;
            }

            public static async ValueTask<BinaryExpressionCodeActionComputer> CreateAsync(
                AbstractBinaryExpressionWrapper<TBinaryExpressionSyntax> service,
                Document document,
                TBinaryExpressionSyntax binaryExpression,
                ImmutableArray<SyntaxNodeOrToken> exprsAndOperators,
                CancellationToken cancellationToken)
            {
                var originalSourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
                var options = await IndentationOptions.FromDocumentAsync(document, cancellationToken).ConfigureAwait(false);

                var generator = SyntaxGenerator.GetGenerator(document);

                var useTabs = options.FormattingOptions.GetOption(FormattingOptions2.UseTabs);
                var tabSize = options.FormattingOptions.GetOption(FormattingOptions2.TabSize);

                var indentAndAlignTrivia = new SyntaxTriviaList(generator.Whitespace(
                    originalSourceText.GetOffset(binaryExpression.Span.Start).CreateIndentationString(useTabs, tabSize)));

                var smartIndentTrivia = new SyntaxTriviaList(generator.Whitespace(
                    await GetSmartIndentationAfterAsync(document, originalSourceText, options, service, exprsAndOperators[1], cancellationToken).ConfigureAwait(false)));

                return new BinaryExpressionCodeActionComputer(service, document, originalSourceText, options, exprsAndOperators, indentAndAlignTrivia, smartIndentTrivia);
            }

            protected override async Task<ImmutableArray<WrappingGroup>> ComputeWrappingGroupsAsync(CancellationToken cancellationToken)
                => ImmutableArray.Create(new WrappingGroup(
                    isInlinable: true, ImmutableArray.Create(
                        await GetWrapCodeActionAsync(align: false, cancellationToken).ConfigureAwait(false),
                        await GetWrapCodeActionAsync(align: true, cancellationToken).ConfigureAwait(false),
                        await GetUnwrapCodeActionAsync(cancellationToken).ConfigureAwait(false))));

            private Task<WrapItemsAction> GetWrapCodeActionAsync(bool align, CancellationToken cancellationToken)
                => TryCreateCodeActionAsync(
                    GetWrapEdits(align),
                    FeaturesResources.Wrapping,
                    align ? FeaturesResources.Wrap_and_align_expression : FeaturesResources.Wrap_expression,
                    cancellationToken);

            private Task<WrapItemsAction> GetUnwrapCodeActionAsync(CancellationToken cancellationToken)
                => TryCreateCodeActionAsync(GetUnwrapEdits(), FeaturesResources.Wrapping, FeaturesResources.Unwrap_expression, cancellationToken);

            private ImmutableArray<Edit> GetWrapEdits(bool align)
            {
                using var _ = ArrayBuilder<Edit>.GetInstance(out var result);
                var indentationTrivia = align ? _indentAndAlignTrivia : _smartIndentTrivia;

                for (var i = 1; i < _exprsAndOperators.Length; i += 2)
                {
                    var left = _exprsAndOperators[i - 1].AsNode();
                    var opToken = _exprsAndOperators[i].AsToken();
                    var right = _exprsAndOperators[i + 1].AsNode();

                    if (_preference == OperatorPlacementWhenWrappingPreference.BeginningOfLine)
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

                return result.ToImmutable();
            }

            private ImmutableArray<Edit> GetUnwrapEdits()
            {
                using var _ = ArrayBuilder<Edit>.GetInstance(out var result);

                for (var i = 0; i < _exprsAndOperators.Length - 1; i++)
                {
                    result.Add(Edit.UpdateBetween(
                        _exprsAndOperators[i], SingleWhitespaceTrivia,
                        NoTrivia, _exprsAndOperators[i + 1]));
                }

                return result.ToImmutable();
            }
        }
    }
}
