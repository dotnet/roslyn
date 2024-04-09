// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.ExternalAccess.VisualDiagnostics.Internal;

internal sealed class HotReloadDiagnosticSource(Project project, IDiagnosticsRefresher diagnosticsRefresher) : IDiagnosticSource
{
    Task<ImmutableArray<DiagnosticData>> IDiagnosticSource.GetDiagnosticsAsync(RequestContext context, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    TextDocumentIdentifier? IDiagnosticSource.GetDocumentIdentifier() => null;
    ProjectOrDocumentId IDiagnosticSource.GetId() => new(project.Id);
    Project IDiagnosticSource.GetProject() => project;
    bool IDiagnosticSource.IsLiveSource() => true;
    string IDiagnosticSource.ToDisplayString() => project.Name;
}
