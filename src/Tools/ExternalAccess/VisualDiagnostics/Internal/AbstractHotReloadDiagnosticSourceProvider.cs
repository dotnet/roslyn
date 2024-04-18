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

internal class AbstractHotReloadDiagnosticSourceProvider : IDiagnosticSourceProvider
{
    internal const string SourceName = "HotReloadDiagnostic";
    internal static readonly ImmutableArray<string> SourceNames = [SourceName];

    ImmutableArray<string> IDiagnosticSourceProvider.SourceNames => SourceNames;
    bool IDiagnosticSourceProvider.IsDocument => throw new NotImplementedException();
    ValueTask<ImmutableArray<IDiagnosticSource>> IDiagnosticSourceProvider.CreateDiagnosticSourcesAsync(RequestContext context, string sourceName, CancellationToken cancellationToken)
        => throw new NotImplementedException();

}
