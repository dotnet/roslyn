// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Diagnostics
{
    public class TestDiagnosticAnalyzerDriver
    {
        private readonly TestDiagnosticAnalyzerService _diagnosticAnalyzerService;
        private readonly TestHostDiagnosticUpdateSource _exceptionDiagnosticsSource;
        private readonly bool _includeSuppressedDiagnostics;

        public TestDiagnosticAnalyzerDriver(
            Project project,
            DiagnosticAnalyzer workspaceAnalyzerOpt = null,
            bool includeSuppressedDiagnostics = false)
        {
            _exceptionDiagnosticsSource = new TestHostDiagnosticUpdateSource(project.Solution.Workspace);

            _diagnosticAnalyzerService = CreateDiagnosticAnalyzerService(project, workspaceAnalyzerOpt);
            _diagnosticAnalyzerService.CreateIncrementalAnalyzer(project.Solution.Workspace);

            _includeSuppressedDiagnostics = includeSuppressedDiagnostics;
        }

        private TestDiagnosticAnalyzerService CreateDiagnosticAnalyzerService(Project project, DiagnosticAnalyzer workspaceAnalyzerOpt)
        {
            if (workspaceAnalyzerOpt != null)
            {
                return new TestDiagnosticAnalyzerService(project.Language, workspaceAnalyzerOpt, _exceptionDiagnosticsSource);
            }

            var analyzer = DiagnosticExtensions.GetCompilerDiagnosticAnalyzer(project.Language);
            var analyzerService = project.Solution.Workspace.Services.GetService<IAnalyzerService>();
            var analyzerReferences = ImmutableArray.Create<AnalyzerReference>(new AnalyzerFileReference(analyzer.GetType().Assembly.Location, analyzerService.GetLoader()));

            return new TestDiagnosticAnalyzerService(analyzerReferences, _exceptionDiagnosticsSource);
        }

        private async Task<IEnumerable<Diagnostic>> GetDiagnosticsAsync(
            Project project,
            Document document,
            TextSpan? filterSpan,
            bool getDocumentDiagnostics,
            bool getProjectDiagnostics)
        {
            var documentDiagnostics = SpecializedCollections.EmptyEnumerable<Diagnostic>();
            var projectDiagnostics = SpecializedCollections.EmptyEnumerable<Diagnostic>();

            await SynchronizeGlobalAssetToRemoteHostIfNeededAsync(project.Solution.Workspace).ConfigureAwait(false);

            if (getDocumentDiagnostics)
            {
                var dxs = await _diagnosticAnalyzerService.GetDiagnosticsAsync(project.Solution, project.Id, document.Id, _includeSuppressedDiagnostics);
                documentDiagnostics = await CodeAnalysis.Diagnostics.Extensions.ToDiagnosticsAsync(
                    filterSpan is null
                        ? dxs.Where(d => d.HasTextSpan)
                        : dxs.Where(d => d.HasTextSpan && d.GetTextSpan().IntersectsWith(filterSpan.Value)),
                    project,
                    CancellationToken.None);
            }

            if (getProjectDiagnostics)
            {
                var dxs = await _diagnosticAnalyzerService.GetDiagnosticsAsync(project.Solution, project.Id, includeSuppressedDiagnostics: _includeSuppressedDiagnostics);
                projectDiagnostics = await CodeAnalysis.Diagnostics.Extensions.ToDiagnosticsAsync(dxs.Where(d => !d.HasTextSpan), project, CancellationToken.None);
            }

            var exceptionDiagnostics = await CodeAnalysis.Diagnostics.Extensions.ToDiagnosticsAsync(_exceptionDiagnosticsSource.GetTestAccessor().GetReportedDiagnostics(), project, CancellationToken.None);
            var allDiagnostics = documentDiagnostics.Concat(projectDiagnostics).Concat(exceptionDiagnostics);

            if (!_includeSuppressedDiagnostics)
            {
                Assert.True(!allDiagnostics.Any(d => d.IsSuppressed));
            }

            return allDiagnostics;
        }

        public Task<IEnumerable<Diagnostic>> GetAllDiagnosticsAsync(Document document, TextSpan? filterSpan)
            => GetDiagnosticsAsync(document.Project, document, filterSpan, getDocumentDiagnostics: true, getProjectDiagnostics: true);

        public async Task<IEnumerable<Diagnostic>> GetAllDiagnosticsAsync(Project project)
        {
            var diagnostics = new List<Diagnostic>();
            foreach (var document in project.Documents)
            {
                var span = (await document.GetSyntaxRootAsync()).FullSpan;
                var documentDiagnostics = await GetDocumentDiagnosticsAsync(document, span);
                diagnostics.AddRange(documentDiagnostics);
            }

            var projectDiagnostics = await GetProjectDiagnosticsAsync(project);
            diagnostics.AddRange(projectDiagnostics);
            return diagnostics;
        }

        public Task<IEnumerable<Diagnostic>> GetDocumentDiagnosticsAsync(Document document, TextSpan span)
            => GetDiagnosticsAsync(document.Project, document, span, getDocumentDiagnostics: true, getProjectDiagnostics: false);

        public Task<IEnumerable<Diagnostic>> GetProjectDiagnosticsAsync(Project project)
            => GetDiagnosticsAsync(project, document: null, filterSpan: null, getDocumentDiagnostics: false, getProjectDiagnostics: true);

        private async Task SynchronizeGlobalAssetToRemoteHostIfNeededAsync(Workspace workspace)
        {
            var client = await RemoteHostClient.TryGetClientAsync(workspace, CancellationToken.None).ConfigureAwait(false);
            if (client == null)
            {
                return;
            }

            // get global assets such as host analyzers for remote host
            var checksums = AddGlobalAssets(workspace);

            // send over global asset
            _ = await client.TryRunRemoteAsync(
                WellKnownRemoteHostServices.RemoteHostService,
                nameof(IRemoteHostService.SynchronizeGlobalAssetsAsync),
                workspace.CurrentSolution,
                new[] { (object)checksums },
                callbackTarget: null,
                CancellationToken.None).ConfigureAwait(false);
        }

        private Checksum[] AddGlobalAssets(Workspace workspace)
        {
            var builder = ArrayBuilder<Checksum>.GetInstance();

            var snapshotService = workspace.Services.GetService<CodeAnalysis.Execution.IRemotableDataService>();
            var assetBuilder = new CodeAnalysis.Execution.CustomAssetBuilder(workspace);

            foreach (var (_, reference) in _diagnosticAnalyzerService.HostAnalyzers.GetHostAnalyzerReferencesMap())
            {
                if (!(reference is AnalyzerFileReference))
                {
                    // OOP only supports analyzer file reference
                    continue;
                }

                var asset = assetBuilder.Build(reference, CancellationToken.None);

                builder.Add(asset.Checksum);
                snapshotService.AddGlobalAsset(reference, asset, CancellationToken.None);
            }

            return builder.ToArrayAndFree();
        }
    }
}
