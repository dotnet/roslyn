// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    [ExportEventListener(WellKnownEventListeners.Workspace, WorkspaceKind.MiscellaneousFiles), Shared]
    internal sealed class MiscSolutionCrawlerWorkspaceEventListener : IEventListener<object>, IEventListenerStoppable, IDisposable
    {
        private readonly IGlobalOptionService _globalOptions;

        /// <summary>
        /// A queue that we put our diagnostic-provider configuration work onto. We queue this work up so that we don't
        /// re-enter into the workspace as it is calling into us on the IEventXXX apis.
        /// </summary>
        private readonly AsyncBatchingWorkQueue<(bool enabled, Workspace workspace)> _workQueue;
        private readonly CancellationTokenSource _tokenSource = new();

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public MiscSolutionCrawlerWorkspaceEventListener(
            IGlobalOptionService globalOptions,
            IAsynchronousOperationListenerProvider listenerProvider)
        {
            _globalOptions = globalOptions;
            _workQueue = new AsyncBatchingWorkQueue<(bool enabled, Workspace workspace)>(
                DelayTimeSpan.Short,
                ProcessWorkQueueAsync,
                EqualityComparer<(bool, Workspace)>.Default,
                listenerProvider.GetListener(FeatureAttribute.Workspace),
                _tokenSource.Token);
        }

        public void Dispose()
            => _tokenSource.Cancel();

        public void StartListening(Workspace workspace, object serviceOpt)
            => _workQueue.AddWork((true, workspace));

        public void StopListening(Workspace workspace)
            => _workQueue.AddWork((false, workspace));

        private ValueTask ProcessWorkQueueAsync(ImmutableSegmentedList<(bool enabled, Workspace workspace)> list, CancellationToken cancellationToken)
        {
            foreach (var (enabled, workspace) in list)
            {
                if (enabled)
                {
                    if (_globalOptions.GetOption(SolutionCrawlerRegistrationService.EnableSolutionCrawler))
                    {
                        // misc workspace will enable syntax errors and semantic errors for script files for
                        // all participating projects in the workspace
                        DiagnosticProvider.Enable(workspace, DiagnosticProvider.Options.Syntax | DiagnosticProvider.Options.ScriptSemantic);
                    }
                }
                else
                {
                    DiagnosticProvider.Disable(workspace);
                }
            }

            return ValueTaskFactory.CompletedTask;
        }
    }
}
