// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.Contracts.EditAndContinue;

/// <summary>
/// Active instruction identifier.
/// It has the information necessary to track an active instruction within the debug session.
/// </summary>
/// <remarks>
/// Creates an ActiveInstructionId.
/// </remarks>
/// <param name="method">Method which the instruction is scoped to.</param>
/// <param name="ilOffset">IL offset for the instruction.</param>
[DataContract]
[DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
internal readonly struct ManagedInstructionId(
    ManagedMethodId method,
    int ilOffset) : IEquatable<ManagedInstructionId>
{

    /// <summary>
    /// Method which the instruction is scoped to.
    /// </summary>
    [DataMember(Name = "method")]
    public ManagedMethodId Method { get; } = method;

    /// <summary>
    /// The IL offset for the instruction.
    /// </summary>
    [DataMember(Name = "ilOffset")]
    public int ILOffset { get; } = ilOffset;

    public bool Equals(ManagedInstructionId other)
    {
        return Method.Equals(other.Method) && ILOffset == other.ILOffset;
    }

    public override bool Equals(object? obj) => obj is ManagedInstructionId instr && Equals(instr);

    public override int GetHashCode()
    {
        return Method.GetHashCode() ^ ILOffset;
    }

    public static bool operator ==(ManagedInstructionId left, ManagedInstructionId right) => left.Equals(right);

    public static bool operator !=(ManagedInstructionId left, ManagedInstructionId right) => !(left == right);

    internal string GetDebuggerDisplay() => $"{Method.GetDebuggerDisplay()} IL_{ILOffset:X4}";
}
