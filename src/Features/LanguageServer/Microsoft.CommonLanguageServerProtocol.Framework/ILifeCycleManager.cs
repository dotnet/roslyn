// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;

namespace Microsoft.CommonLanguageServerProtocol.Framework;

/// <summary>
/// An optional component to run additional logic when LSP shutdown and exit are called,
/// for example logging messages, cleaning up custom resources, etc.
/// </summary>
public interface ILifeCycleManager
{
    /// <summary>
    /// Called when the server recieves the LSP exit notification.
    /// </summary>
    /// <remarks>
    /// This is called before LSP services and the JsonRpc connection is disposed of.
    /// Implementations are not expected to be threadsafe.
    /// </remarks>
    Task ExitAsync();

    /// <summary>
    /// Called when the server recieves the LSP shutdown request.
    /// </summary>
    /// <remarks>
    /// This is called before the request execution is closed.
    /// Implementations are not expected to be threadsafe.
    /// </remarks>
    Task ShutdownAsync(string message = "Shutting down");
}
