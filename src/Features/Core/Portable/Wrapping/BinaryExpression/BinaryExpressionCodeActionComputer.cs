// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
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

            public BinaryExpressionCodeActionComputer(
                AbstractBinaryExpressionWrapper<TBinaryExpressionSyntax> service,
                Document document,
                SourceText originalSourceText,
                DocumentOptionSet options,
                TBinaryExpressionSyntax binaryExpression,
                ImmutableArray<SyntaxNodeOrToken> exprsAndOperators,
                CancellationToken cancellationToken)
                : base(service, document, originalSourceText, options, cancellationToken)
            {
                _exprsAndOperators = exprsAndOperators;
                _preference = options.GetOption(CodeStyleOptions.OperatorPlacementWhenWrapping);

                var generator = SyntaxGenerator.GetGenerator(document);

                _newlineBeforeOperatorTrivia = service.GetNewLineBeforeOperatorTrivia(NewLineTrivia);

                _indentAndAlignTrivia = new SyntaxTriviaList(generator.Whitespace(
                    OriginalSourceText.GetOffset(binaryExpression.Span.Start)
                                      .CreateIndentationString(UseTabs, TabSize)));

                _smartIndentTrivia = new SyntaxTriviaList(generator.Whitespace(
                    this.GetSmartIndentationAfter(_exprsAndOperators[1])));
            }

            protected override async Task<ImmutableArray<WrappingGroup>> ComputeWrappingGroupsAsync()
                => ImmutableArray.Create(new WrappingGroup(
                    isInlinable: true, ImmutableArray.Create(
                        await GetWrapCodeActionAsync(align: false).ConfigureAwait(false),
                        await GetWrapCodeActionAsync(align: true).ConfigureAwait(false),
                        await GetUnwrapCodeActionAsync().ConfigureAwait(false))));

            private Task<WrapItemsAction> GetWrapCodeActionAsync(bool align)
                => TryCreateCodeActionAsync(GetWrapEdits(align), FeaturesResources.Wrapping,
                        align ? FeaturesResources.Wrap_and_align_expression : FeaturesResources.Wrap_expression);

            private Task<WrapItemsAction> GetUnwrapCodeActionAsync()
                => TryCreateCodeActionAsync(GetUnwrapEdits(), FeaturesResources.Wrapping, FeaturesResources.Unwrap_expression);

            private ImmutableArray<Edit> GetWrapEdits(bool align)
            {
                var result = ArrayBuilder<Edit>.GetInstance();
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

                return result.ToImmutableAndFree();
            }

            private ImmutableArray<Edit> GetUnwrapEdits()
            {
                var result = ArrayBuilder<Edit>.GetInstance();

                for (var i = 0; i < _exprsAndOperators.Length - 1; i++)
                {
                    result.Add(Edit.UpdateBetween(
                        _exprsAndOperators[i], SingleWhitespaceTrivia,
                        NoTrivia, _exprsAndOperators[i + 1]));
                }

                return result.ToImmutableAndFree();
            }
        }
    }
}
