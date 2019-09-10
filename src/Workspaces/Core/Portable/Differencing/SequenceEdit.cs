// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Differencing
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
        /// The kind of edit: <see cref="EditKind.Delete"/>, <see cref="EditKind.Insert"/>, or <see cref="EditKind.Update"/>.
        /// </summary>
        public EditKind Kind
        {
            get
            {
                if (_oldIndex == -1)
                {
                    return EditKind.Insert;
                }

                if (_newIndex == -1)
                {
                    return EditKind.Delete;
                }

                return EditKind.Update;
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

        private string GetDebuggerDisplay()
        {
            var result = Kind.ToString();
            switch (Kind)
            {
                case EditKind.Delete:
                    return result + " (" + _oldIndex + ")";

                case EditKind.Insert:
                    return result + " (" + _newIndex + ")";

                case EditKind.Update:
                    return result + " (" + _oldIndex + " -> " + _newIndex + ")";
            }

            return result;
        }

        internal TestAccessor GetTestAccessor()
            => new TestAccessor(this);

        internal readonly struct TestAccessor
        {
            private readonly SequenceEdit _sequenceEdit;

            public TestAccessor(SequenceEdit sequenceEdit)
            {
                _sequenceEdit = sequenceEdit;
            }

            internal string GetDebuggerDisplay()
                => _sequenceEdit.GetDebuggerDisplay();
        }
    }
}
