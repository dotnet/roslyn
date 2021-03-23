// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor
{
    [DataContract]
    internal readonly struct RazorPinnedSolutionInfoWrapper
    {
        [DataMember(Order = 0)]
        internal readonly PinnedSolutionInfo UnderlyingObject;

        public RazorPinnedSolutionInfoWrapper(PinnedSolutionInfo underlyingObject)
            => UnderlyingObject = underlyingObject;

        public static implicit operator RazorPinnedSolutionInfoWrapper(PinnedSolutionInfo info)
            => new(info);
    }
}
