// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.FileBasedPrograms;

[Export(typeof(IDiagnosticSourceProvider)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class VirtualProjectXmlDiagnosticSourceProvider(VirtualProjectXmlProvider virtualProjectXmlProvider) : IDiagnosticSourceProvider
{
    public bool IsDocument => true;

    public const string FileBasedPrograms = nameof(FileBasedPrograms);
    public string Name => FileBasedPrograms;

    public bool IsEnabled(ClientCapabilities clientCapabilities) => true;

    public ValueTask<ImmutableArray<IDiagnosticSource>> CreateDiagnosticSourcesAsync(RequestContext context, CancellationToken cancellationToken)
    {
        ImmutableArray<IDiagnosticSource> sources = context.Document is null
            ? []
            : [new DiagnosticSource(context.Document, virtualProjectXmlProvider)];

        return ValueTask.FromResult(sources);
    }

    private class DiagnosticSource(Document document, VirtualProjectXmlProvider virtualProjectXmlProvider) : IDiagnosticSource
    {
        public async Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(RequestContext context, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(document.FilePath))
                return [];

            var simpleDiagnostics = await virtualProjectXmlProvider.GetCachedDiagnosticsAsync(document.FilePath, cancellationToken);
            if (simpleDiagnostics.IsDefaultOrEmpty)
                return [];

            var diagnosticDatas = ImmutableArray.CreateBuilder<DiagnosticData>(simpleDiagnostics.Length);
            foreach (var simpleDiagnostic in simpleDiagnostics)
            {
                var location = new FileLinePositionSpan(simpleDiagnostic.Location.Path, simpleDiagnostic.Location.Span.ToLinePositionSpan());
                var diagnosticData = new DiagnosticData(
                    id: "FBP",
                    category: FileBasedPrograms,
                    message: simpleDiagnostic.Message,
                    severity: DiagnosticSeverity.Error,
                    defaultSeverity: DiagnosticSeverity.Error,
                    isEnabledByDefault: true,
                    warningLevel: 1,
                    customTags: ImmutableArray<string>.Empty,
                    properties: ImmutableDictionary<string, string?>.Empty,
                    projectId: document.Project.Id,
                    location: new DiagnosticDataLocation(location, document.Id)
                );
                diagnosticDatas.Add(diagnosticData);
            }
            return diagnosticDatas.MoveToImmutable();
        }

        public TextDocumentIdentifier? GetDocumentIdentifier()
        {
            return !string.IsNullOrEmpty(document.FilePath)
            ? new VSTextDocumentIdentifier { ProjectContext = ProtocolConversions.ProjectToProjectContext(document.Project), DocumentUri = document.GetURI() }
            : null;
        }

        public ProjectOrDocumentId GetId()
        {
            return new ProjectOrDocumentId(document.Id);
        }

        public Project GetProject()
        {
            return document.Project;
        }

        public bool IsLiveSource() => false;

        public string ToDisplayString() => $"{nameof(VirtualProjectXmlProvider)}.{nameof(DiagnosticSource)}";
    }
}
