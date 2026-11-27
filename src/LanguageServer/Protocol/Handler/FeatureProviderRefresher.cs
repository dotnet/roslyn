// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

[Export(typeof(FeatureProviderRefresher)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class FeatureProviderRefresher()
{
    public event Action<DocumentUri?>? ProviderRefreshRequested;

    public void RequestProviderRefresh(DocumentUri? documentUri)
    {
        ProviderRefreshRequested?.Invoke(documentUri);
    }
}
