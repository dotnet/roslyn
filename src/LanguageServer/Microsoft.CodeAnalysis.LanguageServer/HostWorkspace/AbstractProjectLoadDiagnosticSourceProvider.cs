// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

/// <summary>
/// Reports the problems encountered while loading projects as workspace diagnostics on the files they're in.
/// </summary>
internal abstract class AbstractProjectLoadDiagnosticSourceProvider(string name) : IDiagnosticSourceProvider
{
    public bool IsDocument => false;
    public string Name => name;
    public bool HasInterFileDependencies => false;

    public bool IsEnabled(ClientCapabilities clientCapabilities) => true;

    // Providers are shared by all language servers while project loaders belong to a single server, so the loader is found
    // from the request rather than passed to the constructor
    protected abstract LanguageServerProjectLoader? GetProjectLoader(RequestContext context);

    public async ValueTask<ImmutableArray<IDiagnosticSource>> CreateDiagnosticSourcesAsync(RequestContext context, CancellationToken cancellationToken)
    {
        if (GetProjectLoader(context) is not { } projectLoader)
            return [];

        var solution = await context.GetRequiredSolutionAsync(cancellationToken).ConfigureAwait(false);

        // The client keeps one report per file, so a file with diagnostics from several projects (such as a Directory.Build.props
        // that they all import) gets a single source
        var filePathToSource = new Dictionary<string, (Project Project, SetWithInsertionOrder<DiagnosticLogItem> Diagnostics)>(PathUtilities.Comparer);

        foreach (var (projectFilePath, projectIds, diagnostics) in await projectLoader.GetLoadDiagnosticsAsync(cancellationToken).ConfigureAwait(false))
        {
            // The project is only used by the pull handler's DiagnosticsPullCache to decide whether results changed, which only needs it to be
            // the same project on every request, so use the one with the lowest id
            Project? project = null;
            foreach (var projectId in projectIds)
            {
                if (solution.GetProject(projectId) is { } targetProject && (project is null || IsLower(targetProject, project)))
                    project = targetProject;
            }

            if (project is null)
                continue;

            foreach (var diagnostic in diagnostics)
            {
                var filePath = diagnostic.FileName ?? projectFilePath;

                if (!filePathToSource.TryGetValue(filePath, out var source))
                {
                    source = (project, new SetWithInsertionOrder<DiagnosticLogItem>());
                    filePathToSource.Add(filePath, source);
                }
                else if (IsLower(project, source.Project))
                {
                    filePathToSource[filePath] = (project, source.Diagnostics);
                }

                // Every project that imports a file with a problem reports that problem, so leave out which project reported it to
                // only report it once
                source.Diagnostics.Add(diagnostic with { ProjectFilePath = "", FileName = filePath });
            }
        }

        return [.. filePathToSource.Select(pair => (IDiagnosticSource)new ProjectLoadDiagnosticSource(name, pair.Key, pair.Value.Project, pair.Value.Diagnostics.AsImmutable()))];

        static bool IsLower(Project project, Project otherProject)
            => project.Id.Id.CompareTo(otherProject.Id.Id) < 0;
    }

    private sealed class ProjectLoadDiagnosticSource(string name, string filePath, Project project, ImmutableArray<DiagnosticLogItem> diagnostics)
        : IDiagnosticSource
    {
        public Project GetProject() => project;
        public DiagnosticScope GetScope() => new(filePath);
        public TextDocumentIdentifier? GetDocumentIdentifier() => new() { DocumentUri = ProtocolConversions.CreateAbsoluteDocumentUri(filePath) };
        public string ToDisplayString() => filePath;

        public Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(RequestContext context, CancellationToken cancellationToken)
            => Task.FromResult(diagnostics.SelectAsArray(CreateDiagnosticData));

        private DiagnosticData CreateDiagnosticData(DiagnosticLogItem diagnostic)
        {
            var severity = diagnostic.Kind is DiagnosticLogItemKind.Error ? DiagnosticSeverity.Error : DiagnosticSeverity.Warning;

            // MSBuild positions are 1-based; LSP doesn't give us a way to return no position if we don't have one, so we just use the start of the
            // file in that case.
            var start = diagnostic.LineNumber is { } line
                ? new LinePosition(line - 1, Math.Max(diagnostic.ColumnNumber.GetValueOrDefault() - 1, 0))
                : default;

            var location = new DiagnosticDataLocation(new FileLinePositionSpan(filePath, new LinePositionSpan(start, start)));

            return new DiagnosticData(
                id: diagnostic.Code ?? "",
                category: name,
                message: diagnostic.Message,
                severity: severity,
                defaultSeverity: severity,
                isEnabledByDefault: true,
                warningLevel: 0,
                customTags: [],
                properties: ImmutableDictionary<string, string?>.Empty,
                projectId: project.Id,
                location: location,
                language: project.Language);
        }
    }
}
