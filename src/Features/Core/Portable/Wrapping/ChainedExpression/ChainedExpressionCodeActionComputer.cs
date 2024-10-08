// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Wrapping.ChainedExpression;

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
    private sealed class CallExpressionCodeActionComputer :
        AbstractCodeActionComputer<AbstractChainedExpressionWrapper<TNameSyntax, TBaseArgumentListSyntax>>
    {
        /// <summary>
        /// The chunks to normalize and wrap.  The first chunk will be normalized,
        /// but not wrapped.  Successive chunks will be normalized and wrapped 
        /// appropriately depending on if this is wrap-each or wrap-long.
        /// </summary>
        private readonly ImmutableArray<ImmutableArray<SyntaxNodeOrToken>> _chunks;

        /// <summary>
        /// trivia to place at the end of a node prior to a chunk that is wrapped.
        /// For C# this will just be a newline.  For VB this will include a line-
        /// continuation character.
        /// </summary>
        private readonly SyntaxTriviaList _newlineBeforeOperatorTrivia;

        /// <summary>
        /// The indent trivia to insert if we are trying to align wrapped chunks with the 
        /// first period of the original chunk.
        /// </summary>
        private readonly SyntaxTriviaList _firstPeriodIndentationTrivia;

        /// <summary>
        /// The indent trivia to insert if we are trying to simply smart-indent all wrapped
        /// chunks.
        /// </summary>
        private readonly SyntaxTriviaList _smartIndentTrivia;

        public CallExpressionCodeActionComputer(
            AbstractChainedExpressionWrapper<TNameSyntax, TBaseArgumentListSyntax> service,
            Document document,
            SourceText originalSourceText,
            SyntaxWrappingOptions options,
            ImmutableArray<ImmutableArray<SyntaxNodeOrToken>> chunks,
            CancellationToken cancellationToken)
            : base(service, document, originalSourceText, options, cancellationToken)
        {
            _chunks = chunks;

            var generator = SyntaxGenerator.GetGenerator(document);

            // Both [0][0] indices are safe here.  We can only get here if we had more than
            // two chunks to wrap.  And each chunk is required to have at least three elements
            // (i.e. <c>. name (arglist)</c>).
            var firstPeriod = chunks[0][0];

            _firstPeriodIndentationTrivia = new SyntaxTriviaList(generator.Whitespace(
                OriginalSourceText.GetOffset(firstPeriod.SpanStart).CreateIndentationString(options.FormattingOptions.UseTabs, options.FormattingOptions.TabSize)));

            _smartIndentTrivia = new SyntaxTriviaList(generator.Whitespace(
                GetSmartIndentationAfter(firstPeriod)));

            _newlineBeforeOperatorTrivia = service.GetNewLineBeforeOperatorTrivia(NewLineTrivia);
        }

        protected override async Task<ImmutableArray<WrappingGroup>> ComputeWrappingGroupsAsync()
        {
            using var _ = ArrayBuilder<WrapItemsAction>.GetInstance(out var actions);

            await AddWrapCodeActionAsync(actions).ConfigureAwait(false);
            await AddUnwrapCodeActionAsync(actions).ConfigureAwait(false);
            await AddWrapLongCodeActionAsync(actions).ConfigureAwait(false);

            return [new WrappingGroup(isInlinable: true, actions.ToImmutable())];
        }

        // Pass 0 as the wrapping column as we effectively always want to wrap each chunk
        // Not just when the chunk would go past the wrapping column.
        private async Task AddWrapCodeActionAsync(ArrayBuilder<WrapItemsAction> actions)
        {
            actions.Add(await TryCreateCodeActionAsync(GetWrapEdits(wrappingColumn: 0, align: false), FeaturesResources.Wrapping, FeaturesResources.Wrap_call_chain).ConfigureAwait(false));
            actions.Add(await TryCreateCodeActionAsync(GetWrapEdits(wrappingColumn: 0, align: true), FeaturesResources.Wrapping, FeaturesResources.Wrap_and_align_call_chain).ConfigureAwait(false));
        }

        private async Task AddUnwrapCodeActionAsync(ArrayBuilder<WrapItemsAction> actions)
            => actions.Add(await TryCreateCodeActionAsync(GetUnwrapEdits(), FeaturesResources.Wrapping, FeaturesResources.Unwrap_call_chain).ConfigureAwait(false));

        private async Task AddWrapLongCodeActionAsync(ArrayBuilder<WrapItemsAction> actions)
        {
            actions.Add(await TryCreateCodeActionAsync(GetWrapEdits(Options.WrappingColumn, align: false), FeaturesResources.Wrapping, FeaturesResources.Wrap_long_call_chain).ConfigureAwait(false));
            actions.Add(await TryCreateCodeActionAsync(GetWrapEdits(Options.WrappingColumn, align: true), FeaturesResources.Wrapping, FeaturesResources.Wrap_and_align_long_call_chain).ConfigureAwait(false));
        }

        private ImmutableArray<Edit> GetWrapEdits(int wrappingColumn, bool align)
        {
            using var _ = ArrayBuilder<Edit>.GetInstance(out var result);

            // First, normalize the first chunk.
            var firstChunk = _chunks[0];
            DeleteAllSpacesInChunk(result, firstChunk);

            var indentationTrivia = align ? _firstPeriodIndentationTrivia : _smartIndentTrivia;

            // Our starting position is at the end of the first chunk.  That position
            // is effectively the start of the first period, plus the length of the 
            // normalized first chuck.
            var position = _firstPeriodIndentationTrivia.FullSpan.Length + NormalizedWidth(firstChunk);

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
                    position = indentationTrivia.FullSpan.Length;

                    // First, add a newline at the end of the previous arglist, and then
                    // indent the very first member chunk appropriately.
                    result.Add(Edit.UpdateBetween(
                        _chunks[i - 1].Last(), _newlineBeforeOperatorTrivia,
                        indentationTrivia, chunk[0]));
                }

                // Now, delete all the remaining spaces in this call chunk.
                DeleteAllSpacesInChunk(result, chunk);

                // Update position based on this chunk we just fixed up.
                position += NormalizedWidth(chunk);
            }

            return result.ToImmutableAndClear();
        }

        private static int NormalizedWidth(ImmutableArray<SyntaxNodeOrToken> chunk)
            => chunk.Sum(s => s.IsNode ? s.AsNode().Width() : s.AsToken().Width());

        private ImmutableArray<Edit> GetUnwrapEdits()
        {
            using var _ = ArrayBuilder<Edit>.GetInstance(out var result);

            // Flatten all the chunks into one long list.  Then delete all the spaces
            // between each piece in that full list.
            var flattened = _chunks.SelectManyAsArray(c => c);
            DeleteAllSpacesInChunk(result, flattened);

            return result.ToImmutableAndClear();
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
