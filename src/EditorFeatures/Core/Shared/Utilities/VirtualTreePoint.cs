// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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

        public int Position
        {
            get { return _position; }
        }

        public int VirtualSpaces
        {
            get { return _virtualSpaces; }
        }

        public SourceText Text
        {
            get { return _text; }
        }

        public SyntaxTree Tree
        {
            get { return _tree; }
        }

        public int CompareTo(VirtualTreePoint other)
        {
            if (Text != other.Text)
            {
                throw new InvalidOperationException(EditorFeaturesResources.Can_t_compare_positions_from_different_text_snapshots);
            }

            if (Position < other.Position)
            {
                return -1;
            }
            else if (Position > other.Position)
            {
                return 1;
            }

            if (VirtualSpaces < other.VirtualSpaces)
            {
                return -1;
            }
            else if (VirtualSpaces > other.VirtualSpaces)
            {
                return 1;
            }

            return 0;
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
