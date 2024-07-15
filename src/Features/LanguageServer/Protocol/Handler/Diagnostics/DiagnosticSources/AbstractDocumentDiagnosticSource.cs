// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;

internal abstract class AbstractDocumentDiagnosticSource<TDocument>(TDocument document) : IDiagnosticSource
    where TDocument : TextDocument
{
    public TDocument Document { get; } = document;

    public abstract bool IsLiveSource();

    public abstract Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(
        IDiagnosticAnalyzerService diagnosticAnalyzerService, RequestContext context, CancellationToken cancellationToken);

    public ProjectOrDocumentId GetId() => new(Document.Id);
    public Project GetProject() => Document.Project;

    public TextDocumentIdentifier? GetDocumentIdentifier()
        => !string.IsNullOrEmpty(Document.FilePath)
            ? new VSTextDocumentIdentifier { ProjectContext = ProtocolConversions.ProjectToProjectContext(Document.Project), Uri = Document.GetURI() }
            : null;

    public string ToDisplayString() => $"{this.GetType().Name}: {Document.FilePath ?? Document.Name} in {Document.Project.Name}";
}
