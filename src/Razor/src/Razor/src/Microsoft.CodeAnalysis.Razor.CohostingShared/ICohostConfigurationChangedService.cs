// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer.Handler;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

internal interface ICohostConfigurationChangedService
{
    Task OnConfigurationChangedAsync(RequestContext requestContext, CancellationToken cancellationToken);
}
