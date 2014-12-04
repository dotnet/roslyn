// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private readonly int oldIndex;
        private readonly int newIndex;

        internal SequenceEdit(int oldIndex, int newIndex)
        {
            Debug.Assert(oldIndex >= -1);
            Debug.Assert(newIndex >= -1);
            Debug.Assert(newIndex != -1 || oldIndex != -1);

            this.oldIndex = oldIndex;
            this.newIndex = newIndex;
        }

        /// <summary>
        /// The kind of edit: <see cref="EditKind.Delete"/>, <see cref="EditKind.Insert"/>, or <see cref="EditKind.Update"/>.
        /// </summary>
        public EditKind Kind
        {
            get
            {
                if (oldIndex == -1)
                {
                    return EditKind.Insert;
                }

                if (newIndex == -1)
                {
                    return EditKind.Delete;
                }

                return EditKind.Update;
            }
        }

        /// <summary>
        /// Index in the old sequence, or -1 if the edit is insert.
        /// </summary>
        public int OldIndex
        {
            get
            {
                return oldIndex;
            }
        }

        /// <summary>
        /// Index in the new sequence, or -1 if the edit is delete.
        /// </summary>
        public int NewIndex
        {
            get
            {
                return newIndex;
            }
        }

        public bool Equals(SequenceEdit other)
        {
            return this.oldIndex == other.oldIndex
                && this.newIndex == other.newIndex;
        }

        public override bool Equals(object obj)
        {
            return obj is SequenceEdit && Equals((SequenceEdit)obj);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(oldIndex, newIndex);
        }

        // internal for testing
        internal string GetDebuggerDisplay()
        {
            string result = Kind.ToString();
            switch (Kind)
            {
                case EditKind.Delete:
                    return result + " (" + oldIndex + ")";

                case EditKind.Insert:
                    return result + " (" + newIndex + ")";

                case EditKind.Update:
                    return result + " (" + oldIndex + " -> " + newIndex + ")";
            }

            return result;
        }
    }
}
