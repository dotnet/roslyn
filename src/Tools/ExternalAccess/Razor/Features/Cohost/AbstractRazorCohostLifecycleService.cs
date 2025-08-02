// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost;

internal abstract class AbstractRazorCohostLifecycleService : IDisposable
{
    public abstract Task LspServerIntializedAsync(CancellationToken cancellationToken);
    public abstract Task RazorActivatedAsync(ClientCapabilities clientCapabilities, RazorCohostRequestContext requestContext, CancellationToken cancellationToken);
    public abstract void Dispose();
}
