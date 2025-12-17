// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor;

internal abstract class RazorRemoteServiceCallbackDispatcher : IRemoteServiceCallbackDispatcher
{
    private readonly RemoteServiceCallbackDispatcher _dispatcher = new();

    public object GetCallback(RazorRemoteServiceCallbackIdWrapper callbackId)
        => _dispatcher.GetCallback(callbackId.UnderlyingObject);

    RemoteServiceCallbackDispatcher.Handle IRemoteServiceCallbackDispatcher.CreateHandle(object? instance)
        => _dispatcher.CreateHandle(instance);
}
