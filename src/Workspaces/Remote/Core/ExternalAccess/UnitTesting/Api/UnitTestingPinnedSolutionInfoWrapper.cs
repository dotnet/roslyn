// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api;

[DataContract]
internal readonly struct UnitTestingPinnedSolutionInfoWrapper
{
    [DataMember(Order = 0)]
    internal readonly Checksum UnderlyingObject;

    public UnitTestingPinnedSolutionInfoWrapper(Checksum underlyingObject)
        => UnderlyingObject = underlyingObject;

    public static implicit operator UnitTestingPinnedSolutionInfoWrapper(Checksum info)
        => new(info);
}
