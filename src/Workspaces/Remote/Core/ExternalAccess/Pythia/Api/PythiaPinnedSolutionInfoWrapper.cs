// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.ExternalAccess.Pythia.Api
{
    [DataContract]
    internal readonly struct PythiaPinnedSolutionInfoWrapper
    {
        [DataMember(Order = 0)]
        internal readonly Checksum UnderlyingObject;

        public PythiaPinnedSolutionInfoWrapper(Checksum underlyingObject)
            => UnderlyingObject = underlyingObject;

        public static implicit operator PythiaPinnedSolutionInfoWrapper(Checksum info)
            => new(info);
    }
}
