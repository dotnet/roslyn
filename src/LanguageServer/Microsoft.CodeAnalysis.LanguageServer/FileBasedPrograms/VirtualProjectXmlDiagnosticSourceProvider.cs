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
internal sealed class VirtualProjectXmlDiagnosticSourceProvider(VirtualProjectXmlProvider virtualProjectXmlProvider) : IDiagnosticSourceProvider
{
    public const string FileBasedPrograms = nameof(FileBasedPrograms);

    public bool IsDocument => true;
    public string Name => FileBasedPrograms;

    public bool IsEnabled(ClientCapabilities clientCapabilities) => true;

    public ValueTask<ImmutableArray<IDiagnosticSource>> CreateDiagnosticSourcesAsync(RequestContext context, CancellationToken cancellationToken)
    {
        ImmutableArray<IDiagnosticSource> sources = context.Document is null
            ? []
            : [new VirtualProjectXmlDiagnosticSource(context.Document, virtualProjectXmlProvider)];

        return ValueTask.FromResult(sources);
    }

    private sealed class VirtualProjectXmlDiagnosticSource(Document document, VirtualProjectXmlProvider virtualProjectXmlProvider) : IDiagnosticSource
    {
        public async Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(RequestContext context, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(document.FilePath))
                return [];

            var simpleDiagnostics = await virtualProjectXmlProvider.GetCachedDiagnosticsAsync(document.FilePath, cancellationToken);
            if (simpleDiagnostics.IsDefaultOrEmpty)
                return [];

            var diagnosticDatas = new FixedSizeArrayBuilder<DiagnosticData>(simpleDiagnostics.Length);
            foreach (var simpleDiagnostic in simpleDiagnostics)
            {
                var location = new FileLinePositionSpan(simpleDiagnostic.Location.Path, simpleDiagnostic.Location.Span.ToLinePositionSpan());
                var diagnosticData = new DiagnosticData(
                    id: FileBasedPrograms,
                    category: FileBasedPrograms,
                    message: simpleDiagnostic.Message,
                    severity: DiagnosticSeverity.Error,
                    defaultSeverity: DiagnosticSeverity.Error,
                    isEnabledByDefault: true,
                    // Warning level 0 is used as a placeholder when the diagnostic has error severity
                    warningLevel: 0,
                    // Mark these diagnostics as build errors so they can be overridden by diagnostics from an explicit build.
                    customTags: [WellKnownDiagnosticTags.Build],
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

        public string ToDisplayString() => nameof(VirtualProjectXmlDiagnosticSource);
    }
}
