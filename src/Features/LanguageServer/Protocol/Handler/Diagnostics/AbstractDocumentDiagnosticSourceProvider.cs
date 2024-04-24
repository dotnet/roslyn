// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;

internal abstract class AbstractDocumentDiagnosticSourceProvider(string name) : IDiagnosticSourceProvider
{
    public bool IsDocument => true;
    public string Name => name;

    public abstract ValueTask<ImmutableArray<IDiagnosticSource>> CreateDiagnosticSourcesAsync(RequestContext context, CancellationToken cancellationToken);
}
