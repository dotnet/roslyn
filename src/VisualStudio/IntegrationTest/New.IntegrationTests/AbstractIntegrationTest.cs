// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Extensibility.Testing;
using Microsoft.VisualStudio.Shell.Interop;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests
{
    [IdeSettings(MinVersion = VisualStudioVersion.VS2022, RootSuffix = "RoslynDev", MaxAttempts = 2)]
    public abstract class AbstractIntegrationTest : AbstractIdeIntegrationTest
    {
        protected const string ProjectName = "TestProj";
        protected const string SolutionName = "TestSolution";

        static AbstractIntegrationTest()
        {
            Trace.Listeners.Clear();
            Trace.Listeners.Add(new ThrowingTraceListener());
        }

        protected AbstractIntegrationTest()
        {
            WorkspaceInProcess.EnableAsynchronousOperationTracking();
        }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            if (await TestServices.SolutionExplorer.IsSolutionOpenAsync(HangMitigatingCancellationToken))
            {
                var dte = await TestServices.Shell.GetRequiredGlobalServiceAsync<SDTE, EnvDTE.DTE>(HangMitigatingCancellationToken);
                if (dte.Debugger.CurrentMode != EnvDTE.dbgDebugMode.dbgDesignMode)
                {
                    dte.Debugger.TerminateAll();
                }

                await TestServices.SolutionExplorer.CloseSolutionAsync(HangMitigatingCancellationToken);
            }

            await TestServices.StateReset.ResetGlobalOptionsAsync(HangMitigatingCancellationToken);
            await TestServices.StateReset.ResetHostSettingsAsync(HangMitigatingCancellationToken);

            await TestServices.Workarounds.WaitForGitHubCoPilotAsync(HangMitigatingCancellationToken);
        }

        public override async Task DisposeAsync()
        {
            var dte = await TestServices.Shell.GetRequiredGlobalServiceAsync<SDTE, EnvDTE.DTE>(HangMitigatingCancellationToken);
            if (dte.Debugger.CurrentMode != EnvDTE.dbgDebugMode.dbgDesignMode)
            {
                dte.Debugger.TerminateAll();
                await TestServices.Workspace.WaitForAllAsyncOperationsAsync(
                    [
                        FeatureAttribute.Workspace,
                        FeatureAttribute.EditAndContinue,
                    ],
                    HangMitigatingCancellationToken);
            }

            await base.DisposeAsync();
        }
    }
}
