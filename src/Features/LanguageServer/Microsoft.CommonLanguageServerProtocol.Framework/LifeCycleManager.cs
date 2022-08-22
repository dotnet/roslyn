// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;

namespace Microsoft.CommonLanguageServerProtocol.Framework;

/// <summary>
/// Enables the managment of the server lifecycle from outside of the AbstractlanguageServer object.
/// </summary>
/// <typeparam name="RequestContextType"></typeparam>
public class LifeCycleManager<RequestContextType>
{
    private readonly AbstractLanguageServer<RequestContextType> _target;

    public LifeCycleManager(AbstractLanguageServer<RequestContextType> languageServerTarget)
    {
        _target = languageServerTarget;
    }

    /// <summary>
    /// Begin exiting the Language Server, as when <see href="https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#exit">exit</see> is called.
    /// </summary>
    /// <returns></returns>
    public Task ExitAsync()
    {
        return _target.ExitAsync();
    }

    /// <summary>
    /// Begin shutting down the Language Server, as when <see href="https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#shutdown">shutdown</see> is called.
    /// </summary>
    public async Task ShutdownAsync(string message = "Shutting down")
    {
        await _target.ShutdownAsync();
        OnShutdown?.Invoke(this, new RequestShutdownEventArgs(message));
    }

    public event EventHandler<RequestShutdownEventArgs>? OnShutdown;
}
