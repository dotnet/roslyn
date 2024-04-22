// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;

namespace Microsoft.CodeAnalysis.ExternalAccess.VisualDiagnostics.Internal;

internal abstract class AbstractHotReloadDiagnosticSourceProvider : IDiagnosticSourceProvider
{
    string IDiagnosticSourceProvider.Name => "HotReloadDiagnostic";

    bool IDiagnosticSourceProvider.IsDocument => throw new NotImplementedException();
    ValueTask<ImmutableArray<IDiagnosticSource>> IDiagnosticSourceProvider.CreateDiagnosticSourcesAsync(RequestContext context, CancellationToken cancellationToken)
        => throw new NotImplementedException();

}
