// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.Razor.Remote;

/// <summary>
/// A wrapper for a solution that can be used by Razor for OOP services that communicate via MessagePack, or in proc services that don't communicate.
/// </summary>
[DataContract]
internal readonly struct RazorSolutionWrapper
{
    [DataMember(Order = 0)]
    internal readonly Checksum Checksum;

    // Not serialized because it should only be used in in-proc scenarios.
    internal readonly Solution? Solution;

    // Needed for the message pack formatter to work
    internal RazorSolutionWrapper(Checksum checksum)
        : this(checksum, null)
    {
    }

    internal RazorSolutionWrapper(Checksum checksum, Solution? solution)
    {
        Contract.ThrowIfTrue(checksum == Checksum.Null && solution is null, "Either a Checksum or a Solution must be provided.");
        Contract.ThrowIfTrue(checksum != Checksum.Null && solution is not null, "Only one of Checksum or Solution can be provided.");

        Checksum = checksum;
        Solution = solution;
    }

    public static implicit operator RazorSolutionWrapper(Checksum checksum)
        => new(checksum, null);

    public static implicit operator RazorSolutionWrapper(Solution solution)
        => new(Checksum.Null, solution);
}
