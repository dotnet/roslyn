// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;

internal abstract class AbstractProjectDiagnosticSource(Project project)
    : IDiagnosticSource
{
    protected Project Project => project;
    protected Solution Solution => this.Project.Solution;

    public abstract Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(RequestContext context, CancellationToken cancellationToken);

    public ProjectOrDocumentId GetId() => new(Project.Id);
    public Project GetProject() => Project;
    public TextDocumentIdentifier? GetDocumentIdentifier()
        => !string.IsNullOrEmpty(Project.FilePath)
            ? new VSTextDocumentIdentifier { ProjectContext = ProtocolConversions.ProjectToProjectContext(Project), DocumentUri = ProtocolConversions.CreateAbsoluteDocumentUri(Project.FilePath) }
            : null;
    public string ToDisplayString() => Project.Name;
}
