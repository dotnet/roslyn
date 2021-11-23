// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;

namespace Roslyn.VisualStudio.IntegrationTests.InProcess
{
    internal class TestServices
    {
        protected TestServices(JoinableTaskFactory joinableTaskFactory)
        {
            JoinableTaskFactory = joinableTaskFactory;

            Editor = new EditorInProcess(this);
            EditorVerifier = new EditorVerifierInProcess(this);
            ErrorList = new ErrorListInProcess(this);
            FindReferencesWindow = new FindReferencesWindowInProcess(this);
            Input = new InputInProcess(this);
            Shell = new ShellInProcess(this);
            SolutionExplorer = new SolutionExplorerInProcess(this);
            SolutionVerifier = new SolutionVerifierInProcess(this);
            StateReset = new StateResetInProcess(this);
            Telemetry = new TelemetryInProcess(this);
            Workspace = new WorkspaceInProcess(this);
        }

        public JoinableTaskFactory JoinableTaskFactory { get; }

        public EditorInProcess Editor { get; }

        public EditorVerifierInProcess EditorVerifier { get; }

        public ErrorListInProcess ErrorList { get; }

        public FindReferencesWindowInProcess FindReferencesWindow { get; }

        public InputInProcess Input { get; }

        public ShellInProcess Shell { get; }

        public SolutionExplorerInProcess SolutionExplorer { get; }

        public SolutionVerifierInProcess SolutionVerifier { get; }

        public StateResetInProcess StateReset { get; }

        public TelemetryInProcess Telemetry { get; }

        public WorkspaceInProcess Workspace { get; }

        internal static async Task<TestServices> CreateAsync(JoinableTaskFactory joinableTaskFactory)
        {
            var services = new TestServices(joinableTaskFactory);
            await services.InitializeAsync();
            return services;
        }

        protected virtual Task InitializeAsync()
        {
            WorkspaceInProcess.EnableAsynchronousOperationTracking();
            return Task.CompletedTask;
        }
    }
}
