// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Telemetry;
using Microsoft.VisualStudio.Telemetry;

namespace Microsoft.CodeAnalysis.Remote.Telemetry
{
    /// <summary>
    /// Creates an <see cref="IIncrementalAnalyzer"/> that collects Api usage information from metadata references
    /// in current solution.
    /// </summary>
    [ExportIncrementalAnalyzerProvider(nameof(ApiUsageIncrementalAnalyzerProvider), new[] { WorkspaceKind.RemoteWorkspace }), Shared]
    internal sealed class ApiUsageIncrementalAnalyzerProvider : IIncrementalAnalyzerProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ApiUsageIncrementalAnalyzerProvider()
        {
        }

        public IIncrementalAnalyzer CreateIncrementalAnalyzer(Workspace workspace)
        {
#if DEBUG
            return new Analyzer(workspace.Services);
#else
            return null;
#endif
        }

        private sealed class Analyzer : IIncrementalAnalyzer
        {
            private readonly HostWorkspaceServices _services;

            // maximum number of symbols to report per project.
            private const int Max = 2000;

            private readonly HashSet<ProjectId> _reported = new HashSet<ProjectId>();

            public Analyzer(HostWorkspaceServices services)
            {
                _services = services;
            }

            public Task RemoveProjectAsync(ProjectId projectId, CancellationToken cancellationToken)
            {
                lock (_reported)
                {
                    _reported.Remove(projectId);
                }

                return Task.CompletedTask;
            }

            public async Task AnalyzeProjectAsync(Project project, bool semanticsChanged, InvocationReasons reasons, CancellationToken cancellationToken)
            {
                var telemetryService = _services.GetRequiredService<IWorkspaceTelemetryService>();
                if (!telemetryService.HasActiveSession)
                {
                    return;
                }

                lock (_reported)
                {
                    // to make sure that we don't report while solution load, we do this heuristic.
                    // if the reason we are called is due to "document being added" to project, we wait for next analyze call.
                    // also, we only report usage information per project once.
                    // this telemetry will only let us know which API ever used, this doesn't care how often/many times an API
                    // used. and this data is approximation not precise information. and we don't care much on how many times
                    // APIs used in the same solution. we are rather more interested in number of solutions or users APIs are used.
                    if (reasons.Contains(PredefinedInvocationReasons.DocumentAdded) ||
                        !_reported.Add(project.Id))
                    {
                        return;
                    }
                }

                // if this project has cross language p2p references, then pass in solution, otherwise, don't give in
                // solution since checking whether symbol is cross language symbol or not is expansive and
                // we know that population of solution with both C# and VB are very tiny. 
                // so no reason to pay the cost for common cases.
                var crossLanguageSolutionOpt =
                    project.ProjectReferences.Any(p => project.Solution.GetProject(p.ProjectId)?.Language != project.Language) ? project.Solution : null;

                var metadataSymbolUsed = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
                foreach (var document in project.Documents)
                {
                    var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                    var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

                    foreach (var operation in GetOperations(model, cancellationToken))
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if (metadataSymbolUsed.Count > Max)
                        {
                            // collect data up to max per project
                            break;
                        }

                        // this only gather reference and method call symbols but not type being used.
                        // if we want all types from metadata used, we need to add more cases 
                        // which will make things more expansive.
                        CollectApisUsed(operation, crossLanguageSolutionOpt, metadataSymbolUsed, cancellationToken);
                    }
                }

                var solutionSessionId = project.Solution.State.SolutionAttributes.TelemetryId;
                var projectGuid = project.State.ProjectInfo.Attributes.TelemetryId;

                telemetryService.ReportApiUsage(metadataSymbolUsed, solutionSessionId, projectGuid);

                return;

                // local functions
                static void CollectApisUsed(
                    IOperation operation, Solution solutionOpt, HashSet<ISymbol> metadataSymbolUsed, CancellationToken cancellationToken)
                {
                    switch (operation)
                    {
                        case IMemberReferenceOperation memberOperation:
                            AddIfMetadataSymbol(solutionOpt, memberOperation.Member, metadataSymbolUsed, cancellationToken);
                            break;
                        case IInvocationOperation invocationOperation:
                            AddIfMetadataSymbol(solutionOpt, invocationOperation.TargetMethod, metadataSymbolUsed, cancellationToken);
                            break;
                        case IObjectCreationOperation objectCreation:
                            AddIfMetadataSymbol(solutionOpt, objectCreation.Constructor, metadataSymbolUsed, cancellationToken);
                            break;
                    }
                }

                static void AddIfMetadataSymbol(
                    Solution solutionOpt, ISymbol symbol, HashSet<ISymbol> metadataSymbolUsed, CancellationToken cancellationToken)
                {
                    // get symbol as it is defined in metadata
                    symbol = symbol.OriginalDefinition;

                    if (metadataSymbolUsed.Contains(symbol))
                    {
                        return;
                    }

                    if (symbol.Locations.All(l => l.Kind == LocationKind.MetadataFile) &&
                        solutionOpt?.GetProject(symbol.ContainingAssembly, cancellationToken) == null)
                    {
                        metadataSymbolUsed.Add(symbol);
                    }
                }

                static IEnumerable<IOperation> GetOperations(SemanticModel model, CancellationToken cancellationToken)
                {
                    // root is already there
                    var root = model.SyntaxTree.GetRoot(cancellationToken);

                    // go through all nodes until we find first node that has IOperation
                    foreach (var rootOperation in root.DescendantNodes(n => model.GetOperation(n, cancellationToken) == null)
                                                     .Select(n => model.GetOperation(n, cancellationToken))
                                                     .Where(o => o != null))
                    {
                        foreach (var operation in rootOperation.DescendantsAndSelf())
                        {
                            yield return operation;
                        }
                    }
                }
            }

            public Task AnalyzeSyntaxAsync(Document document, InvocationReasons reasons, CancellationToken cancellationToken)
                => Task.CompletedTask;

            public Task DocumentOpenAsync(Document document, CancellationToken cancellationToken)
                => Task.CompletedTask;

            public Task DocumentCloseAsync(Document document, CancellationToken cancellationToken)
                => Task.CompletedTask;

            public Task DocumentResetAsync(Document document, CancellationToken cancellationToken)
                => Task.CompletedTask;

            public Task ActiveDocumentSwitchedAsync(TextDocument document, CancellationToken cancellationToken)
                => Task.CompletedTask;

            public Task AnalyzeDocumentAsync(Document document, SyntaxNode bodyOpt, InvocationReasons reasons, CancellationToken cancellationToken)
                => Task.CompletedTask;

            public bool NeedsReanalysisOnOptionChanged(object sender, OptionChangedEventArgs e)
                => false;

            public Task NewSolutionSnapshotAsync(Solution solution, CancellationToken cancellationToken)
                => Task.CompletedTask;

            public Task RemoveDocumentAsync(DocumentId documentId, CancellationToken cancellationToken)
                => Task.CompletedTask;

            public Task NonSourceDocumentOpenAsync(TextDocument textDocument, CancellationToken cancellationToken)
                => Task.CompletedTask;

            public Task NonSourceDocumentCloseAsync(TextDocument textDocument, CancellationToken cancellationToken)
                => Task.CompletedTask;

            public Task NonSourceDocumentResetAsync(TextDocument textDocument, CancellationToken cancellationToken)
                => Task.CompletedTask;

            public Task AnalyzeNonSourceDocumentAsync(TextDocument textDocument, InvocationReasons reasons, CancellationToken cancellationToken)
                => Task.CompletedTask;

            public int Priority => 1;

            public void LogAnalyzerCountSummary()
            {
            }
        }
    }
}
