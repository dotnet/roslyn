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

namespace Microsoft.CodeAnalysis.Editor.Wrapping.BinaryExpression
{
    internal partial class AbstractBinaryExpressionWrapper<TBinaryExpressionSyntax> where TBinaryExpressionSyntax : SyntaxNode
    {
        private class BinaryExpressionCodeActionComputer :
            AbstractCodeActionComputer<AbstractBinaryExpressionWrapper<TBinaryExpressionSyntax>>
        {
            private readonly ImmutableArray<SyntaxNodeOrToken> _exprsAndOperators;
            private readonly OperatorPlacementWhenWrappingPreference _preference;

            private readonly SyntaxTriviaList _indentationTrivia;
            private readonly SyntaxTriviaList _newlineBeforeOperatorTrivia;

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
                var indentationString = OriginalSourceText.GetOffset(binaryExpression.Span.Start)
                                                          .CreateIndentationString(UseTabs, TabSize);

                _indentationTrivia = new SyntaxTriviaList(generator.Whitespace(indentationString));
                _newlineBeforeOperatorTrivia = service.GetNewLineBeforeOperatorTrivia(NewLineTrivia);
            }

            protected override async Task<ImmutableArray<WrappingGroup>> ComputeWrappingGroupsAsync()
                => ImmutableArray.Create(new WrappingGroup(
                    isInlinable: true, ImmutableArray.Create(
                        await GetWrapCodeActionAsync().ConfigureAwait(false),
                        await GetUnwrapCodeActionAsync().ConfigureAwait(false))));

            private Task<WrapItemsAction> GetWrapCodeActionAsync()
                => TryCreateCodeActionAsync(GetWrapEdits(), FeaturesResources.Wrapping, FeaturesResources.Wrap_expression);

            private Task<WrapItemsAction> GetUnwrapCodeActionAsync()
                => TryCreateCodeActionAsync(GetUnwrapEdits(), FeaturesResources.Wrapping, FeaturesResources.Unwrap_expression);

            private ImmutableArray<Edit> GetWrapEdits()
            {
                var result = ArrayBuilder<Edit>.GetInstance();

                for (int i = 1; i < _exprsAndOperators.Length; i += 2)
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
                        result.Add(Edit.UpdateBetween(left, _newlineBeforeOperatorTrivia, _indentationTrivia, opToken));
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
                        result.Add(Edit.UpdateBetween(opToken, NewLineTrivia, _indentationTrivia, right));
                    }
                }

                return result.ToImmutableAndFree();
            }

            private ImmutableArray<Edit> GetUnwrapEdits()
            {
                var result = ArrayBuilder<Edit>.GetInstance();

                for (int i = 0; i < _exprsAndOperators.Length - 1; i++)
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
