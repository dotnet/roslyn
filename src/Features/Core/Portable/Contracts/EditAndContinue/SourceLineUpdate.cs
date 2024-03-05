// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using System;

namespace Microsoft.CodeAnalysis.Contracts.EditAndContinue;

/// <summary>
/// Maps source lines affected by an update.
/// Zero-based line number.
/// </summary>
[DataContract]
internal readonly struct SourceLineUpdate
{
    /// <summary>
    /// Creates a SourceLineUpdate. 
    /// </summary>
    /// <param name="oldLine">Line number before the update was made.</param>
    /// <param name="newLine">Line number after the update was made.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// If <paramref name="oldLine"/> or <paramref name="newLine"/> is less than 0.
    /// </exception>
    /// <remarks>
    /// We expect that <paramref name="oldLine"/> and <paramref name="newLine"/> have the same value
    /// when the line delta is zero.
    /// </remarks>
    public SourceLineUpdate(
        int oldLine,
        int newLine)
    {
        if (oldLine < 0)
            throw new ArgumentOutOfRangeException(nameof(oldLine));
        if (newLine < 0)
            throw new ArgumentOutOfRangeException(nameof(newLine));

        OldLine = oldLine;
        NewLine = newLine;
    }

    /// <summary>
    /// Line number before the update was made, must be zero-based.
    /// </summary>
    [DataMember(Name = "oldLine")]
    public int OldLine { get; }

    /// <summary>
    /// Line number after the update was made, must be zero-based.
    /// </summary>
    [DataMember(Name = "newLine")]
    public int NewLine { get; }
}
