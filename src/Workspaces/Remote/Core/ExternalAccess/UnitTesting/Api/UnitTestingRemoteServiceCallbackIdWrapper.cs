// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api;

[DataContract]
internal readonly struct UnitTestingRemoteServiceCallbackIdWrapper
{
    [DataMember(Order = 0)]
    internal RemoteServiceCallbackId UnderlyingObject { get; }

    public UnitTestingRemoteServiceCallbackIdWrapper(RemoteServiceCallbackId underlyingObject)
        => UnderlyingObject = underlyingObject;

    public static implicit operator UnitTestingRemoteServiceCallbackIdWrapper(RemoteServiceCallbackId id)
        => new(id);
}
