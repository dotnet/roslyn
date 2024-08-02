// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Shared.Utilities;

internal readonly record struct VirtualTreePoint : IComparable<VirtualTreePoint>
{
    private readonly int _virtualSpaces;

    public VirtualTreePoint(SyntaxTree tree, SourceText text, int position, int virtualSpaces = 0)
    {
        Contract.ThrowIfNull(tree);
        Contract.ThrowIfFalse(position >= 0 && position <= tree.Length);
        Contract.ThrowIfFalse(virtualSpaces >= 0);

        Tree = tree;
        Text = text;
        Position = position;
        _virtualSpaces = virtualSpaces;
    }

    public static bool operator <(VirtualTreePoint left, VirtualTreePoint right)
        => left.CompareTo(right) < 0;

    public static bool operator <=(VirtualTreePoint left, VirtualTreePoint right)
        => left.CompareTo(right) <= 0;

    public static bool operator >(VirtualTreePoint left, VirtualTreePoint right)
        => left.CompareTo(right) > 0;

    public static bool operator >=(VirtualTreePoint left, VirtualTreePoint right)
        => left.CompareTo(right) >= 0;

    public bool IsInVirtualSpace
    {
        get { return _virtualSpaces != 0; }
    }

    public int Position { get; }

    public int VirtualSpaces => _virtualSpaces;

    public SourceText Text { get; }

    public SyntaxTree Tree { get; }

    public int CompareTo(VirtualTreePoint other)
    {
        if (Text != other.Text)
        {
            throw new InvalidOperationException(EditorFeaturesResources.Can_t_compare_positions_from_different_text_snapshots);
        }

        return ComparerWithState.CompareTo(this, other, s_comparers);
    }

    private static readonly ImmutableArray<Func<VirtualTreePoint, IComparable>> s_comparers =
        [p => p.Position, prop => prop.VirtualSpaces];

    public bool Equals(VirtualTreePoint other)
        => CompareTo(other) == 0;

    public override int GetHashCode()
        => Text.GetHashCode() ^ Position.GetHashCode() ^ VirtualSpaces.GetHashCode();

    public override string ToString()
        => $"VirtualTreePoint {{ Tree: '{Tree}', Text: '{Text}', Position: '{Position}', VirtualSpaces '{VirtualSpaces}' }}";

    public TextLine GetContainingLine()
        => Text.Lines.GetLineFromPosition(Position);
}
