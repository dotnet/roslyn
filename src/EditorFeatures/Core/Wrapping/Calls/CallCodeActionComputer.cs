// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Utilities;

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
        /// <summary>
        /// Responsible for actually computing the set of potential wrapping options
        /// for complex call expressions.  The three options we offer are basically
        /// 1. wrap-each. Each call-chunk will be wrapped and aligned with the first).
        /// 2. wrap-long. The same as '1', except a call-chunk will only be wrapped
        ///    if it would go past the preferred wrapping column.
        /// 3. Unwrap.  All the call-chunks will be placed on a single line.
        /// 
        /// Note: These three options are always computed and returned.  The caller
        /// is the one that ends up eliminating any if they would be redundant.  i.e.
        /// if wrap-long produces the same results as wrap-each, then the caller will
        /// filter it out.
        /// </summary>
        private class CallCodeActionComputer :
            AbstractCodeActionComputer<AbstractCallWrapper>
        {
            /// <summary>
            /// The chunks to normalize and wrap.  The first chunk will be normalized,
            /// but not wrapped.  Successive chunks will be normalized and wrapped 
            /// appropriately depending on if this is wrap-each or wrap-long.
            /// </summary>
            private readonly ImmutableArray<CallChunk> _callChunks;

            /// <summary>
            /// Trivia to place before a call-chunk when it it wrapped.
            /// </summary>
            private readonly SyntaxTriviaList _indentationTrivia;

            /// <summary>
            /// trivia to place at the end of a node prior to a chunk that is wrapped.
            /// For C# this will just be a newline.  For VB this will include a line-
            /// continuation character.
            /// </summary>
            private readonly SyntaxTriviaList _newlineBeforeOperatorTrivia;

            public CallCodeActionComputer(
                AbstractCallWrapper service,
                Document document,
                SourceText originalSourceText,
                DocumentOptionSet options,
                ImmutableArray<CallChunk> callChunks,
                CancellationToken cancellationToken)
                : base(service, document, originalSourceText, options, cancellationToken)
            {
                _callChunks = callChunks;

                var generator = SyntaxGenerator.GetGenerator(document);

                // Both [0] indices are safe here.  We can only get here if we had more than
                // two call-chunks to wrap.  And each call-chunk is required to have at least
                // one member-name-chunk.
                var indentationString = OriginalSourceText.GetOffset(callChunks[0].MemberChunks[0].DotToken.SpanStart)
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

                // Deal with the first chunk (it has special behavior because we
                // don't want to delete any trivia directly before it).
                DeleteAllButLeadingSpacesInCallChunk(result, _callChunks[0]);

                // The position we're currently at.  If adding the next chunk would
                // make us go past our preferred wrapping column, then we will wrap 
                // that chunk.
                var position = _indentationTrivia.FullSpan.Length + _callChunks[0].NormalizedLength();

                // Now go through all subsequence call chunks and normalize them all.
                // Also, wrap any we encounter if we go past the specified wrapping
                // column
                for (var i = 1; i < _callChunks.Length; i++)
                {
                    var callChunk = _callChunks[i];
                    var wrapChunk = position + callChunk.NormalizedLength() > wrappingColumn;
                    if (wrapChunk)
                    {
                        // we're wrapping.  So our position is reset to the indentation
                        // on the next line.
                        position = _indentationTrivia.FullSpan.Length;

                        // First, add a newline at the end of the previous arglist, and then
                        // indent the very first member chunk appropriately.
                        result.Add(Edit.UpdateBetween(
                            _callChunks[i - 1].ArgumentList, _newlineBeforeOperatorTrivia,
                            _indentationTrivia, callChunk.MemberChunks[0].DotToken));

                        // Now, delete all the remaining spaces in this call chunk.
                        DeleteAllButLeadingSpacesInCallChunk(result, callChunk);
                    }
                    else
                    {
                        // not wrapping.  So just clean up this next call chunk by
                        // deleting all spaces.
                        DeleteAllSpacesInCallChunk(result, callChunk);
                    }

                    // Update position based on this chunk we just fixed up.
                    position += callChunk.NormalizedLength();
                }

                return result.ToImmutableAndFree();
            }

            private ImmutableArray<Edit> GetUnwrapEdits()
            {
                var result = ArrayBuilder<Edit>.GetInstance();

                // Deal with the first chunk (it has special behavior because we
                // don't want to delete any trivia directly before it).
                DeleteAllButLeadingSpacesInCallChunk(result, _callChunks[0]);

                // Now, handle all successive call chunks.
                for (var i = 1; i < _callChunks.Length; i++)
                {
                    // In successive call chunks we want to delete all the spacing
                    // in the member chunks unilaterally.
                    DeleteAllSpacesInCallChunk(result, _callChunks[i]);
                }

                return result.ToImmutableAndFree();
            }

            private static void DeleteAllSpacesInCallChunk(ArrayBuilder<Edit> result, CallChunk callChunk)
            {
                foreach (var memberChunk in callChunk.MemberChunks)
                {
                    DeleteSpacesInMemberChunk(result, memberChunk);
                }

                // and then any whitespace before the arg list.
                DeleteSpacesBeforeArgumentList(result, callChunk);
            }

            /// <summary>
            /// Removes all whitespace in the spaces between the elements of this chunk.
            /// However no edits will be made before the the first dot in the first member
            /// chunk of this call chunk.  This is useful for the very first call chunk or
            /// any callchunk we're explicitly wrapping.
            /// </summary>
            private void DeleteAllButLeadingSpacesInCallChunk(ArrayBuilder<Edit> result, CallChunk callChunk)
            {
                // For the very first member chunk we have, don't make any edits prior 
                // to it.  This is the chunk that contains the dot that we are aligning 
                // all wrapping to.  It should never be touched.
                //
                // After that first member chunk, remove all whitespace between the
                // other member chunks and between the arg list.

                var firstCallChunk = _callChunks[0];

                // For the very first name chunk in .A.B.C (i.e. `.A` just remove the spaces
                // between the dot and the name.
                var firstMemberChunk = firstCallChunk.MemberChunks[0];
                result.Add(Edit.DeleteBetween(firstMemberChunk.DotToken, firstMemberChunk.Name));

                // For all subsequence name chunks in .A.B.C (i.e. `.B.C`) remove any spaces between
                // the chunk and the last chunk, and between the dot and the name.
                for (var i = 1; i < firstCallChunk.MemberChunks.Length; i++)
                {
                    var memberChunk = firstCallChunk.MemberChunks[i];
                    DeleteSpacesInMemberChunk(result, memberChunk);
                }

                DeleteSpacesBeforeArgumentList(result, firstCallChunk);
            }

            private static void DeleteSpacesInMemberChunk(ArrayBuilder<Edit> result, MemberChunk memberChunk)
            {
                result.Add(Edit.DeleteBetween(memberChunk.DotToken.GetPreviousToken(), memberChunk.DotToken));
                result.Add(Edit.DeleteBetween(memberChunk.DotToken, memberChunk.Name));
            }

            private static void DeleteSpacesBeforeArgumentList(ArrayBuilder<Edit> result, CallChunk callChunk)
                => result.Add(Edit.DeleteBetween(callChunk.MemberChunks.Last().Name, callChunk.ArgumentList));
        }
    }
}
