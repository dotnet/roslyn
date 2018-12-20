// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Wrapping.ChainedExpression
{
    internal abstract partial class AbstractChainedExpressionWrapper<
        TNameSyntax,
        TBaseArgumentListSyntax>
    {
        /// <summary>
        /// Responsible for actually computing the set of potential wrapping options
        /// for chained expressions.  The three options we offer are basically
        /// 1. wrap-each. Each chunk will be wrapped and aligned with the first chunk.
        /// 2. wrap-long. The same as '1', except a chunk will only be wrapped
        ///    if it would go past the preferred wrapping column.
        /// 3. Unwrap.  All the chunks will be placed on a single line.
        /// 
        /// Note: These three options are always computed and returned.  The caller
        /// is the one that ends up eliminating any if they would be redundant.  i.e.
        /// if wrap-long produces the same results as wrap-each, then the caller will
        /// filter it out.
        /// </summary>
        private class CallExpressionCodeActionComputer :
            AbstractCodeActionComputer<AbstractChainedExpressionWrapper>
        {
            /// <summary>
            /// The chunks to normalize and wrap.  The first chunk will be normalized,
            /// but not wrapped.  Successive chunks will be normalized and wrapped 
            /// appropriately depending on if this is wrap-each or wrap-long.
            /// </summary>
            private readonly ImmutableArray<ImmutableArray<SyntaxNodeOrToken>> _chunks;

            /// <summary>
            /// Trivia to place before a chunk when it is wrapped.
            /// </summary>
            private readonly SyntaxTriviaList _indentationTrivia;

            /// <summary>
            /// trivia to place at the end of a node prior to a chunk that is wrapped.
            /// For C# this will just be a newline.  For VB this will include a line-
            /// continuation character.
            /// </summary>
            private readonly SyntaxTriviaList _newlineBeforeOperatorTrivia;

            public CallExpressionCodeActionComputer(
                AbstractChainedExpressionWrapper service,
                Document document,
                SourceText originalSourceText,
                DocumentOptionSet options,
                ImmutableArray<ImmutableArray<SyntaxNodeOrToken>> chunks,
                CancellationToken cancellationToken)
                : base(service, document, originalSourceText, options, cancellationToken)
            {
                _chunks = chunks;

                var generator = SyntaxGenerator.GetGenerator(document);

                // Both [0][0] indices are safe here.  We can only get here if we had more than
                // two chunks to wrap.  And each chunk is required to have at least three elements
                // (i.e. `. name (arglist)`).
                var indentationString = OriginalSourceText.GetOffset(chunks[0][0].SpanStart)
                                                          .CreateIndentationString(UseTabs, TabSize);

                _indentationTrivia = new SyntaxTriviaList(generator.Whitespace(indentationString));
                _newlineBeforeOperatorTrivia = service.GetNewLineBeforeOperatorTrivia(NewLineTrivia);
            }

            protected override async Task<ImmutableArray<WrappingGroup>> ComputeWrappingGroupsAsync()
                => ImmutableArray.Create(new WrappingGroup(
                    isInlinable: true, ImmutableArray.Create(
                        await GetWrapCodeActionAsync().ConfigureAwait(false),
                        await GetUnwrapCodeActionAsync().ConfigureAwait(false),
                        await GetWrapLongCodeActionAsync().ConfigureAwait(false))));

            // Pass 0 as the wrapping column as we effectively always want to wrap each chunk
            // Not just when the chunk would go past the wrapping column.
            private Task<WrapItemsAction> GetWrapCodeActionAsync()
                => TryCreateCodeActionAsync(GetWrapEdits(wrappingColumn: 0), FeaturesResources.Wrapping, FeaturesResources.Wrap_calls);

            private Task<WrapItemsAction> GetUnwrapCodeActionAsync()
                => TryCreateCodeActionAsync(GetUnwrapEdits(), FeaturesResources.Wrapping, FeaturesResources.Unwrap_calls);

            private Task<WrapItemsAction> GetWrapLongCodeActionAsync()
                => TryCreateCodeActionAsync(GetWrapEdits(WrappingColumn), FeaturesResources.Wrapping, FeaturesResources.Wrap_long_calls);

            private ImmutableArray<Edit> GetWrapEdits(int wrappingColumn)
            {
                var result = ArrayBuilder<Edit>.GetInstance();

                // First, normalize the first chunk.
                var firstChunk = _chunks[0];
                DeleteAllSpacesInChunk(result, firstChunk);
                var position = _indentationTrivia.FullSpan.Length + NormalizedWidth(firstChunk);

                // Now, go to each subsequent chunk.  If keeping it on the current line would
                // cause us to go past the requested wrapping column, then wrap it and proceed.
                for (var i = 1; i < _chunks.Length; i++)
                {
                    var chunk = _chunks[i];
                    var wrapChunk = position + NormalizedWidth(chunk) >= wrappingColumn;

                    if (wrapChunk)
                    {
                        // we're wrapping.  So our position is reset to the indentation
                        // on the next line.
                        position = _indentationTrivia.FullSpan.Length;

                        // First, add a newline at the end of the previous arglist, and then
                        // indent the very first member chunk appropriately.
                        result.Add(Edit.UpdateBetween(
                            _chunks[i - 1].Last(), _newlineBeforeOperatorTrivia,
                            _indentationTrivia, chunk[0]));
                    }

                    // Now, delete all the remaining spaces in this call chunk.
                    DeleteAllSpacesInChunk(result, chunk);

                    // Update position based on this chunk we just fixed up.
                    position += NormalizedWidth(chunk);
                }

                return result.ToImmutableAndFree();
            }

            private int NormalizedWidth(ImmutableArray<SyntaxNodeOrToken> chunk)
            {
                var width = 0;
                foreach (var syntax in chunk)
                {
                    width += syntax.IsNode ? syntax.AsNode().Width() : syntax.AsToken().Width();
                }
                return width;
            }

            private ImmutableArray<Edit> GetUnwrapEdits()
            {
                var result = ArrayBuilder<Edit>.GetInstance();

                // Flatten all the chunks into one long list.  Then delete all the spaces
                // between each piece in that full list.
                var flattened = _chunks.SelectMany(c => c).ToImmutableArray();
                DeleteAllSpacesInChunk(result, flattened);

                return result.ToImmutableAndFree();
            }

            private static void DeleteAllSpacesInChunk(
                ArrayBuilder<Edit> result, ImmutableArray<SyntaxNodeOrToken> chunk)
            {
                for (var i = 1; i < chunk.Length; i++)
                {
                    result.Add(Edit.DeleteBetween(chunk[i - 1], chunk[i]));
                }
            }
        }
    }
}
