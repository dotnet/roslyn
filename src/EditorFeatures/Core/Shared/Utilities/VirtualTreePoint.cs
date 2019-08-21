// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Shared.Utilities
{
    internal struct VirtualTreePoint : IComparable<VirtualTreePoint>, IEquatable<VirtualTreePoint>
    {
        private readonly SyntaxTree _tree;
        private readonly SourceText _text;
        private readonly int _position;
        private readonly int _virtualSpaces;

        public VirtualTreePoint(SyntaxTree tree, SourceText text, int position, int virtualSpaces = 0)
        {
            Contract.ThrowIfNull(tree);
            Contract.ThrowIfFalse(position >= 0 && position <= tree.Length);
            Contract.ThrowIfFalse(virtualSpaces >= 0);

            _tree = tree;
            _text = text;
            _position = position;
            _virtualSpaces = virtualSpaces;
        }

        public static bool operator !=(VirtualTreePoint left, VirtualTreePoint right)
        {
            return !(left == right);
        }

        public static bool operator <(VirtualTreePoint left, VirtualTreePoint right)
        {
            return left.CompareTo(right) < 0;
        }

        public static bool operator <=(VirtualTreePoint left, VirtualTreePoint right)
        {
            return left.CompareTo(right) <= 0;
        }

        public static bool operator ==(VirtualTreePoint left, VirtualTreePoint right)
        {
            return object.Equals(left, right);
        }

        public static bool operator >(VirtualTreePoint left, VirtualTreePoint right)
        {
            return left.CompareTo(right) > 0;
        }

        public static bool operator >=(VirtualTreePoint left, VirtualTreePoint right)
        {
            return left.CompareTo(right) >= 0;
        }

        public bool IsInVirtualSpace
        {
            get { return _virtualSpaces != 0; }
        }

        public int Position => _position;

        public int VirtualSpaces => _virtualSpaces;

        public SourceText Text => _text;

        public SyntaxTree Tree => _tree;

        public int CompareTo(VirtualTreePoint other)
        {
            if (Text != other.Text)
            {
                throw new InvalidOperationException(EditorFeaturesResources.Can_t_compare_positions_from_different_text_snapshots);
            }

            return IComparableHelper.CompareTo(this, other, GetComparisonComponents);
        }

        private static IEnumerable<IComparable> GetComparisonComponents(VirtualTreePoint p)
        {
            yield return p.Position;
            yield return p.VirtualSpaces;
        }

        public bool Equals(VirtualTreePoint other)
        {
            return CompareTo(other) == 0;
        }

        public override bool Equals(object obj)
        {
            return (obj is VirtualTreePoint) && Equals((VirtualTreePoint)obj);
        }

        public override int GetHashCode()
        {
            return Text.GetHashCode() ^ Position.GetHashCode() ^ VirtualSpaces.GetHashCode();
        }

        public override string ToString()
        {
            return $"VirtualTreePoint {{ Tree: '{Tree}', Text: '{Text}', Position: '{Position}', VirtualSpaces '{VirtualSpaces}' }}";
        }

        public TextLine GetContainingLine()
        {
            return Text.Lines.GetLineFromPosition(Position);
        }
    }
}
