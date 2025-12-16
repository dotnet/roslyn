// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor
{
    /// <summary>
    /// A wrapper for a solution that can be used by Razor for OOP services that communicate via MessagePack, or in proc services that don't communicate.
    /// </summary>
    [DataContract]
    internal readonly struct RazorPinnedSolutionInfoWrapper
    {
        [DataMember(Order = 0)]
        internal readonly Checksum UnderlyingObject;

        // Not serialized because it should only be used in in-proc scenarios.
        internal readonly Solution? Solution;

        // Needed for the message pack formatter to work
        internal RazorPinnedSolutionInfoWrapper(Checksum checksum)
            : this(checksum, null)
        {
        }

        internal RazorPinnedSolutionInfoWrapper(Checksum checksum, Solution? solution)
        {
            Contract.ThrowIfTrue(checksum == Checksum.Null && solution is null, "Either a Checksum or a Solution must be provided.");
            Contract.ThrowIfTrue(checksum != Checksum.Null && solution is not null, "Only one of Checksum or Solution can be provided.");

            UnderlyingObject = checksum;
            Solution = solution;
        }

        public static implicit operator RazorPinnedSolutionInfoWrapper(Checksum checksum)
            => new(checksum, null);

        public static implicit operator RazorPinnedSolutionInfoWrapper(Solution solution)
            => new(Checksum.Null, solution);
    }
}
