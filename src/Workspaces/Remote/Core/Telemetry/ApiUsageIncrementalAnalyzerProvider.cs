// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.SolutionCrawler;
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
        public ApiUsageIncrementalAnalyzerProvider()
        {
        }

        public IIncrementalAnalyzer CreateIncrementalAnalyzer(Workspace workspace)
        {
            return new Analyzer();
        }

        private sealed class Analyzer : IIncrementalAnalyzer
        {
            // maximum number of symbols to report per project.
            private const int Max = 2000;

            private const string EventName = "vs/compilers/api";
            private const string PropertyName = "vs.compilers.api.pii";

            private readonly HashSet<ProjectId> _reported = new HashSet<ProjectId>();

            public void RemoveProject(ProjectId projectId)
            {
                lock (_reported)
                {
                    _reported.Remove(projectId);
                }
            }

            public async Task AnalyzeProjectAsync(Project project, bool semanticsChanged, InvocationReasons reasons, CancellationToken cancellationToken)
            {
                lock (_reported)
                {
                    // to make sure that we don't report while solution load, we do this heuristic.
                    // if the reason we are called is due to "document being added" to project, we wait for next analyze call.
                    // also, we only report usage information per project once.
                    // this telemetry will only let us know which API ever used, this doesn't care how often/many times an API
                    // used. and this data is approximation not precise information. and we don't care much on how many times
                    // APIs used in the same solution. we are rather more interested in number of solutions or users APIs are used.
                    if (reasons.Contains(PredefinedInvocationReasons.DocumentAdded) ||
                        _reported.Contains(project.Id))
                    {
                        return;
                    }
                }

                var metadataSymbolUsed = new HashSet<ISymbol>();
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
                        CollectApisUsed(operation, metadataSymbolUsed);
                    }
                }

                var groupByAssembly = metadataSymbolUsed.GroupBy(s => s.ContainingAssembly);
                var apiPerAssemlby = groupByAssembly.Select(g => new
                {
                    // mark all string as PII (customer data)
                    AssemblyName = new TelemetryPiiProperty(g.Key.Identity.Name),
                    AssemblyVersion = g.Key.Identity.Version.ToString(),
                    Symbols = g.Select(s => s.GetDocumentationCommentId()).Where(id => id != null).Select(id => new TelemetryPiiProperty(id))
                });

                lock (_reported)
                {
                    if (_reported.Add(project.Id))
                    {
                        // use telemetry API directly rather than Logger abstraction for PII data
                        var telemetryEvent = new TelemetryEvent(EventName);
                        telemetryEvent.Properties[PropertyName] = new TelemetryComplexProperty(apiPerAssemlby);

                        try
                        {
                            RoslynServices.SessionOpt?.PostEvent(telemetryEvent);
                        }
                        catch
                        {
                            // don't crash OOP because we failed to send telemetry
                        }
                    }
                }

                return;

                // local functions
                static void CollectApisUsed(IOperation operation, HashSet<ISymbol> metadataSymbolUsed)
                {
                    switch (operation)
                    {
                        case IMemberReferenceOperation memberOperation:
                            AddIfMetadataSymbol(metadataSymbolUsed, memberOperation.Member);
                            break;
                        case IInvocationOperation invocationOperation:
                            AddIfMetadataSymbol(metadataSymbolUsed, invocationOperation.TargetMethod);
                            break;
                    }
                }

                static void AddIfMetadataSymbol(HashSet<ISymbol> metadataSymbolUsed, ISymbol symbol)
                {
                    // get symbol as it is defined in metadata
                    symbol = symbol.OriginalDefinition;

                    if (metadataSymbolUsed.Contains(symbol))
                    {
                        return;
                    }

                    if (symbol.Locations.All(l => l.Kind == LocationKind.MetadataFile))
                    {
                        metadataSymbolUsed.Add(symbol);
                    }
                }

                static IEnumerable<IOperation> GetOperations(SemanticModel model, CancellationToken cancellationToken)
                {
                    // root is already there
                    var root = model.SyntaxTree.GetRoot(cancellationToken);

                    // go through all nodes until we find first node that has IOperation
                    foreach (var rootOperation in root.DescendantNodes(n => model.GetOperation(n) == null)
                                                     .Select(n => model.GetOperation(n))
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
            {
                return Task.CompletedTask;
            }

            public Task DocumentOpenAsync(Document document, CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }

            public Task DocumentCloseAsync(Document document, CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }

            public Task DocumentResetAsync(Document document, CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }

            public Task AnalyzeDocumentAsync(Document document, SyntaxNode bodyOpt, InvocationReasons reasons, CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }

            public bool NeedsReanalysisOnOptionChanged(object sender, OptionChangedEventArgs e)
            {
                return false;
            }

            public Task NewSolutionSnapshotAsync(Solution solution, CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }

            public void RemoveDocument(DocumentId documentId)
            {
            }
        }
    }
}
