// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Tagging;

internal partial class AbstractAsynchronousTaggerProvider<TTag>
{
    private readonly struct BufferToTagTree(ImmutableDictionary<ITextBuffer, TagSpanIntervalTree<TTag>> map)
    {
        public static readonly BufferToTagTree Empty = new(ImmutableDictionary<ITextBuffer, TagSpanIntervalTree<TTag>>.Empty);

        public readonly ImmutableDictionary<ITextBuffer, TagSpanIntervalTree<TTag>> Map = map;

        public bool IsDefault => Map is null;

        public TagSpanIntervalTree<TTag> this[ITextBuffer buffer] => Map[buffer];

        internal static BufferToTagTree InterlockedExchange(ref BufferToTagTree location, BufferToTagTree value)
            => new(Interlocked.Exchange(ref Unsafe.AsRef(in location.Map), value.Map));

        internal static BufferToTagTree InterlockedCompareExchange(ref BufferToTagTree location, BufferToTagTree value, BufferToTagTree comparand)
            => new(Interlocked.CompareExchange(ref Unsafe.AsRef(in location.Map), value.Map, comparand.Map));

        public bool TryGetValue(ITextBuffer textBuffer, [NotNullWhen(true)] out TagSpanIntervalTree<TTag>? tagTree)
            => Map.TryGetValue(textBuffer, out tagTree);

        public BufferToTagTree Add(ITextBuffer buffer, TagSpanIntervalTree<TTag> newTagTree)
            => new(Map.Add(buffer, newTagTree));

        public BufferToTagTree SetItem(ITextBuffer buffer, TagSpanIntervalTree<TTag> newTagTree)
            => new(Map.SetItem(buffer, newTagTree));

        public static bool operator ==(BufferToTagTree left, BufferToTagTree right)
            => left.Map == right.Map;

        public static bool operator !=(BufferToTagTree left, BufferToTagTree right)
            => !(left == right);

        public override bool Equals([NotNullWhen(true)] object? obj)
            => throw new NotSupportedException();

        public override int GetHashCode()
            => throw new NotSupportedException();

        public bool ContainsKey(ITextBuffer oldBuffer)
            => Map.ContainsKey(oldBuffer);
    }
}
