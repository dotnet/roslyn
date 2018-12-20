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

namespace Microsoft.CodeAnalysis.Editor.Wrapping.Call
{
    internal abstract partial class AbstractCallWrapper<
            TExpressionSyntax,
            TNameSyntax,
            TMemberAccessExpressionSyntax,
            TInvocationExpressionSyntax,
            TElementAccessExpressionSyntax,
            TBaseArgumentListSyntax>
    {
        private class CallCodeActionComputer :
            AbstractCodeActionComputer<AbstractCallWrapper>
        {
            private readonly ImmutableArray<Chunk> _chunks;

            private readonly SyntaxTriviaList _newlineBeforeOperatorTrivia;

            public CallCodeActionComputer(
                AbstractCallWrapper service,
                Document document,
                SourceText originalSourceText,
                DocumentOptionSet options,
                ImmutableArray<Chunk> chunks,
                CancellationToken cancellationToken)
                : base(service, document, originalSourceText, options, cancellationToken)
            {
                _chunks = chunks;

                var generator = SyntaxGenerator.GetGenerator(document);
                var indentationString = OriginalSourceText.GetOffset(chunks[0].DotToken.SpanStart)
                                                          .CreateIndentationString(UseTabs, TabSize);

                _newlineBeforeOperatorTrivia = service.GetNewLineBeforeOperatorTrivia(NewLineTrivia);
            }

            protected override async Task<ImmutableArray<WrappingGroup>> ComputeWrappingGroupsAsync()
                => ImmutableArray.Create(new WrappingGroup(
                    isInlinable: true, ImmutableArray.Create(
                        await GetWrapCodeActionAsync().ConfigureAwait(false),
                        await GetUnwrapCodeActionAsync().ConfigureAwait(false))));

            private Task<WrapItemsAction> GetWrapCodeActionAsync()
                => TryCreateCodeActionAsync(GetWrapEdits(), FeaturesResources.Wrapping, FeaturesResources.Wrap_calls);

            private Task<WrapItemsAction> GetUnwrapCodeActionAsync()
                => TryCreateCodeActionAsync(GetUnwrapEdits(), FeaturesResources.Wrapping, FeaturesResources.Unwrap_calls);

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

                foreach (var chunk in _chunks)
                {
                    if (chunk.ExpressionOpt != null)
                    {
                        result.Add(Edit.DeleteBetween(chunk.ExpressionOpt, chunk.DotToken));
                    }

                    result.Add(Edit.DeleteBetween(chunk.DotToken, chunk.Name));

                    if (chunk.ArgumentListOpt != null)
                    {
                        result.Add(Edit.DeleteBetween(chunk.Name, chunk.ArgumentListOpt));
                    }
                }

                return result.ToImmutableAndFree();
            }
        }
    }
}
