// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SyntaxDifferencing
{
    /// <summary>
    /// Represents an edit operation on a sequence of values.
    /// </summary>
    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    internal struct SequenceEdit : IEquatable<SequenceEdit>
    {
        private readonly int _oldIndex;
        private readonly int _newIndex;

        internal SequenceEdit(int oldIndex, int newIndex)
        {
            Debug.Assert(oldIndex >= -1);
            Debug.Assert(newIndex >= -1);
            Debug.Assert(newIndex != -1 || oldIndex != -1);

            _oldIndex = oldIndex;
            _newIndex = newIndex;
        }

        /// <summary>
        /// The kind of edit: <see cref="SyntaxEditKind.Delete"/>, <see cref="SyntaxEditKind.Insert"/>, or <see cref="SyntaxEditKind.Update"/>.
        /// </summary>
        public SyntaxEditKind Kind
        {
            get
            {
                if (_oldIndex == -1)
                {
                    return SyntaxEditKind.Insert;
                }

                if (_newIndex == -1)
                {
                    return SyntaxEditKind.Delete;
                }

                return SyntaxEditKind.Update;
            }
        }

        /// <summary>
        /// Index in the old sequence, or -1 if the edit is insert.
        /// </summary>
        public int OldIndex => _oldIndex;

        /// <summary>
        /// Index in the new sequence, or -1 if the edit is delete.
        /// </summary>
        public int NewIndex => _newIndex;

        public bool Equals(SequenceEdit other)
        {
            return _oldIndex == other._oldIndex
                && _newIndex == other._newIndex;
        }

        public override bool Equals(object obj)
        {
            return obj is SequenceEdit && Equals((SequenceEdit)obj);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(_oldIndex, _newIndex);
        }

        // internal for testing
        internal string GetDebuggerDisplay()
        {
            string result = Kind.ToString();
            switch (Kind)
            {
                case SyntaxEditKind.Delete:
                    return result + " (" + _oldIndex + ")";

                case SyntaxEditKind.Insert:
                    return result + " (" + _newIndex + ")";

                case SyntaxEditKind.Update:
                    return result + " (" + _oldIndex + " -> " + _newIndex + ")";
            }

            return result;
        }
    }
}
