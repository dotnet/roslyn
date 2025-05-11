// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Differencing;

/// <summary>
/// Represents an edit operation on a sequence of values.
/// </summary>
[DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
internal readonly struct SequenceEdit : IEquatable<SequenceEdit>
{
    internal SequenceEdit(int oldIndex, int newIndex)
    {
        Debug.Assert(oldIndex >= -1);
        Debug.Assert(newIndex >= -1);
        Debug.Assert(newIndex != -1 || oldIndex != -1);

        OldIndex = oldIndex;
        NewIndex = newIndex;
    }

    /// <summary>
    /// The kind of edit: <see cref="EditKind.Delete"/>, <see cref="EditKind.Insert"/>, or <see cref="EditKind.Update"/>.
    /// </summary>
    public EditKind Kind
    {
        get
        {
            if (OldIndex == -1)
            {
                return EditKind.Insert;
            }

            if (NewIndex == -1)
            {
                return EditKind.Delete;
            }

            return EditKind.Update;
        }
    }

    /// <summary>
    /// Index in the old sequence, or -1 if the edit is insert.
    /// </summary>
    public int OldIndex { get; }

    /// <summary>
    /// Index in the new sequence, or -1 if the edit is delete.
    /// </summary>
    public int NewIndex { get; }

    public bool Equals(SequenceEdit other)
    {
        return OldIndex == other.OldIndex
            && NewIndex == other.NewIndex;
    }

    public override bool Equals(object obj)
        => obj is SequenceEdit && Equals((SequenceEdit)obj);

    public override int GetHashCode()
        => Hash.Combine(OldIndex, NewIndex);

    private string GetDebuggerDisplay()
    {
        var result = Kind.ToString();
        switch (Kind)
        {
            case EditKind.Delete:
                return result + " (" + OldIndex + ")";

            case EditKind.Insert:
                return result + " (" + NewIndex + ")";

            case EditKind.Update:
                return result + " (" + OldIndex + " -> " + NewIndex + ")";
        }

        return result;
    }

    internal TestAccessor GetTestAccessor()
        => new(this);

    internal readonly struct TestAccessor(SequenceEdit sequenceEdit)
    {
        internal string GetDebuggerDisplay()
            => sequenceEdit.GetDebuggerDisplay();
    }
}
