// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor;

[DataContract]
internal readonly struct RazorRemoteServiceCallbackIdWrapper
{
    [DataMember(Order = 0)]
    internal RemoteServiceCallbackId UnderlyingObject { get; }

    public RazorRemoteServiceCallbackIdWrapper(RemoteServiceCallbackId underlyingObject)
        => UnderlyingObject = underlyingObject;

    public static implicit operator RazorRemoteServiceCallbackIdWrapper(RemoteServiceCallbackId id)
        => new(id);
}
