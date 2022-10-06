﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;

internal sealed record class ProjectDiagnosticSource(Project Project) : IDiagnosticSource
{
    public ProjectOrDocumentId GetId() => new(Project.Id);
    public Project GetProject() => Project;
    public TextDocumentIdentifier GetDocumentIdentifier()
    {
        Contract.ThrowIfNull(Project.FilePath);
        return new VSTextDocumentIdentifier { ProjectContext = ProtocolConversions.ProjectToProjectContext(Project), Uri = ProtocolConversions.GetUriFromFilePath(Project.FilePath) };
    }

    public async Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(
        IDiagnosticAnalyzerService diagnosticAnalyzerService,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        // Directly use the IDiagnosticAnalyzerService.  This will use the actual snapshots
        // we're passing in.  If information is already cached for that snapshot, it will be returned.  Otherwise,
        // it will be computed on demand.  Because it is always accurate as per this snapshot, all spans are correct
        // and do not need to be adjusted.
        var projectDiagnostics = await diagnosticAnalyzerService.GetProjectDiagnosticsForIdsAsync(Project.Solution, Project.Id, cancellationToken: cancellationToken).ConfigureAwait(false);
        return projectDiagnostics;
    }
}
