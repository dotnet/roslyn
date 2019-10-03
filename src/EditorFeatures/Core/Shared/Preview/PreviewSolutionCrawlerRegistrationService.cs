// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Shared.Preview
{
    [ExportWorkspaceServiceFactory(typeof(ISolutionCrawlerRegistrationService), WorkspaceKind.Preview), Shared]
    internal class PreviewSolutionCrawlerRegistrationServiceFactory : IWorkspaceServiceFactory
    {
        private readonly DiagnosticAnalyzerService _analyzerService;
        private readonly IAsynchronousOperationListener _listener;

        [ImportingConstructor]
        public PreviewSolutionCrawlerRegistrationServiceFactory(IDiagnosticAnalyzerService analyzerService, IAsynchronousOperationListenerProvider listenerProvider)
        {
            // this service is directly tied to DiagnosticAnalyzerService and
            // depends on its implementation.
            _analyzerService = analyzerService as DiagnosticAnalyzerService;
            Contract.ThrowIfNull(_analyzerService);

            _listener = listenerProvider.GetListener(FeatureAttribute.DiagnosticService);
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            // to make life time management easier, just create new service per new workspace
            return new Service(this, workspaceServices.Workspace);
        }

        // internal for testing
        internal class Service : ISolutionCrawlerRegistrationService
        {
            private readonly PreviewSolutionCrawlerRegistrationServiceFactory _owner;
            private readonly Workspace _workspace;
            private readonly CancellationTokenSource _source;

            // since we now have one service for each one specific instance of workspace,
            // we can have states for this specific workspace.
            private Task _analyzeTask;

            public Service(PreviewSolutionCrawlerRegistrationServiceFactory owner, Workspace workspace)
            {
                _owner = owner;
                _workspace = workspace;
                _source = new CancellationTokenSource();
            }

            public void Register(Workspace workspace)
            {
                // given workspace must be owner of this workspace service
                Contract.ThrowIfFalse(workspace == _workspace);

                // this can't be called twice
                Contract.ThrowIfFalse(_analyzeTask == null);

                var asyncToken = _owner._listener.BeginAsyncOperation(nameof(PreviewSolutionCrawlerRegistrationServiceFactory) + "." + nameof(Service) + "." + nameof(Register));
                _analyzeTask = AnalyzeAsync().CompletesAsyncOperation(asyncToken);
            }

            private async Task AnalyzeAsync()
            {
                var workerBackOffTimeSpanInMS = _workspace.Options.GetOption(InternalSolutionCrawlerOptions.PreviewBackOffTimeSpanInMS);
                var diagnosticAnalyzer = _owner._analyzerService.CreateIncrementalAnalyzer(_workspace);

                var solution = _workspace.CurrentSolution;
                var documentIds = _workspace.GetOpenDocumentIds().ToImmutableArray();

                try
                {
                    foreach (var documentId in documentIds)
                    {
                        var document = solution.GetDocument(documentId);
                        if (document == null)
                        {
                            continue;
                        }

                        // delay analyzing
                        await Task.Delay(workerBackOffTimeSpanInMS, _source.Token).ConfigureAwait(false);

                        // do actual analysis
                        await diagnosticAnalyzer.AnalyzeSyntaxAsync(document, InvocationReasons.Empty, _source.Token).ConfigureAwait(false);
                        await diagnosticAnalyzer.AnalyzeDocumentAsync(document, bodyOpt: null, reasons: InvocationReasons.Empty, cancellationToken: _source.Token).ConfigureAwait(false);

                        // don't call project one.
                    }
                }
                catch (OperationCanceledException)
                {
                    // do nothing
                }
            }

            public void Unregister(Workspace workspace, bool blockingShutdown = false)
            {
                _ = UnregisterAsync(workspace, blockingShutdown);
            }

            private async Task UnregisterAsync(Workspace workspace, bool blockingShutdown)
            {
                Contract.ThrowIfFalse(workspace == _workspace);
                _source.Cancel();

                // wait for analyzer work to be finished
                await _analyzeTask.ConfigureAwait(false);

                // ask it to reset its stages for the given workspace
                _owner._analyzerService.ShutdownAnalyzerFrom(_workspace);
            }

            public void AddAnalyzerProvider(IIncrementalAnalyzerProvider provider, IncrementalAnalyzerProviderMetadata metadata)
            {
                // preview solution crawler doesn't support adding and removing analyzer dynamically
                throw new NotSupportedException();
            }
        }
    }
}
