// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor
{
    /// <summary>
    /// A wrapper for a solution that can be used by Razor for OOP services that communicate via MessagePack
    /// </summary>
    [DataContract]
    internal readonly struct RazorPinnedSolutionInfoWrapper
    {
        [DataMember(Order = 0)]
        internal readonly Checksum UnderlyingObject;

        public RazorPinnedSolutionInfoWrapper(Checksum underlyingObject)
            => UnderlyingObject = underlyingObject;

        public static implicit operator RazorPinnedSolutionInfoWrapper(Checksum info)
            => new(info);
    }
}
