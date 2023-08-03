// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Text
{
    internal static class ChunkedTextUtilities
    {
        public static void GetIndexAndOffset(
            int[] chunkStartOffsets,
            int position,
            out int index,
            out int offset)
        {
            // Binary search to find the chunk that contains the given position.
            int idx = chunkStartOffsets.BinarySearch(position);
            index = idx >= 0 ? idx : (~idx - 1);
            offset = position - chunkStartOffsets[index];
        }

        public static SubTextChunkEnumerator<TChunk, TChunkHelper> EnumerateSubTextChunks<TChunk, TChunkHelper>(
            ImmutableArray<TChunk> chunks,
            int[] chunkStartOffsets,
            int start,
            int length)
            where TChunkHelper : struct, IChunkHelper<TChunk>
        {
            return new SubTextChunkEnumerator<TChunk, TChunkHelper>(chunks, chunkStartOffsets, start, length);
        }

        [NonCopyable]
        public struct SubTextChunkEnumerator<TChunk, TChunkHelper>
            where TChunkHelper : struct, IChunkHelper<TChunk>
        {
            private readonly ImmutableArray<TChunk> _chunks;
            private (TChunk chunk, int start, int length) _current;
            private int _count;
            private int _chunkIndex;
            private int _chunkOffset;

            public SubTextChunkEnumerator(
                ImmutableArray<TChunk> chunks,
                int[] chunkStartOffsets,
                int start,
                int length)
            {
                _chunks = chunks;
                _count = length;
                GetIndexAndOffset(chunkStartOffsets, start, out _chunkIndex, out _chunkOffset);
            }

            public readonly (TChunk chunk, int start, int length) Current
                => _current;

            public bool MoveNext()
            {
                if (_chunkIndex < _chunks.Length && _count > 0)
                {
                    var chunk = _chunks[_chunkIndex];
                    var length = Math.Min(_count, default(TChunkHelper).GetLength(chunk) - _chunkOffset);

                    _current = (chunk, _chunkOffset, length);
                    _count -= length;
                    _chunkIndex++;
                    _chunkOffset = 0;

                    return true;
                }

                return false;
            }

#pragma warning disable RS0042 // Do not copy value
            public readonly SubTextChunkEnumerator<TChunk, TChunkHelper> GetEnumerator() => this;
#pragma warning restore RS0042 // Do not copy value
        }

        public interface IChunkHelper<TChunk>
        {
            int GetLength(TChunk chunk);
        }
    }
}
