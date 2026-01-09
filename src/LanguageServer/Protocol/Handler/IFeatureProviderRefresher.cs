// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

/// <summary>
/// Used to request that all feature providers send a refresh notification.
/// </summary>
internal interface IFeatureProviderRefresher
{
    event Action<DocumentUri?>? ProviderRefreshRequested;

    /// <summary>
    /// Requests feature providers to send a refresh notification.
    /// </summary>
    void RequestProviderRefresh(DocumentUri? documentUri);

    /// <summary>
    /// Current version of global state. Incremented on every refresh.
    /// Used to determine whether any global state that might affect workspace project contexts has changed.
    /// </summary>
    int GlobalStateVersion { get; }
}

[Export(typeof(IFeatureProviderRefresher)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class DefaultProjectContextRefresher() : IFeatureProviderRefresher
{
    /// <summary>
    /// Incremented every time a refresh is requested.
    /// </summary>
    private int _globalStateVersion;

    public event Action<DocumentUri?>? ProviderRefreshRequested;

    public void RequestProviderRefresh(DocumentUri? documentUri)
    {
        // bump version before sending the request to the client:
        Interlocked.Increment(ref _globalStateVersion);

        ProviderRefreshRequested?.Invoke(documentUri);
    }

    public int GlobalStateVersion
        => _globalStateVersion;
}
