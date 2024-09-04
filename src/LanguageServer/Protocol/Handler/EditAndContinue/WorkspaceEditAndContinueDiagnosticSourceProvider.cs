﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;

[Export(typeof(IDiagnosticSourceProvider)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class WorkspaceEditAndContinueDiagnosticSourceProvider() : IDiagnosticSourceProvider
{
    public bool IsDocument => false;
    public string Name => PullDiagnosticCategories.EditAndContinue;

    public bool IsEnabled(ClientCapabilities capabilities) => true;

    public ValueTask<ImmutableArray<IDiagnosticSource>> CreateDiagnosticSourcesAsync(RequestContext context, CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(context.Solution);
        return EditAndContinueDiagnosticSource.CreateWorkspaceDiagnosticSourcesAsync(context.Solution!, document => context.IsTracking(document.GetURI()), cancellationToken);
    }
}
