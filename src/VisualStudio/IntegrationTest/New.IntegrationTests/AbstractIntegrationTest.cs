// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Extensibility.Testing;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Harness;

namespace Roslyn.VisualStudio.IntegrationTests
{
    [IdeSettings(MinVersion = VisualStudioVersion.VS2022, RootSuffix = "RoslynDev", MaxAttempts = 2)]
    public abstract class AbstractIntegrationTest : AbstractIdeIntegrationTest
    {
        private static AsynchronousOperationListenerProvider? s_listenerProvider;

        protected const string ProjectName = "TestProj";
        protected const string SolutionName = "TestSolution";

        static AbstractIntegrationTest()
        {
            // Make sure to run the module initializer for Roslyn.Test.Utilities before installing TestTraceListener, or
            // it will replace it with ThrowingTraceListener later.
            RuntimeHelpers.RunModuleConstructor(typeof(TestBase).Module.ModuleHandle);
            TestTraceListener.Install();

            IdeStateCollector.RegisterCustomState(
                "Pending asynchronous operations",
                () =>
                {
                    if (s_listenerProvider is null)
                        return "Unknown";

                    var messageBuilder = new StringBuilder();
                    foreach (var group in s_listenerProvider.GetTokens().GroupBy(token => token.Listener.FeatureName))
                    {
                        messageBuilder.AppendLine($"Feature '{group.Key}'");
                        foreach (var token in group)
                        {
                            messageBuilder.AppendLine($"  {token}");
                        }
                    }

                    return messageBuilder.ToString();
                });
        }

        protected AbstractIntegrationTest()
        {
            WorkspaceInProcess.EnableAsynchronousOperationTracking();
        }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            s_listenerProvider ??= await TestServices.Shell.GetComponentModelServiceAsync<AsynchronousOperationListenerProvider>(HangMitigatingCancellationToken);

            if (await TestServices.SolutionExplorer.IsSolutionOpenAsync(HangMitigatingCancellationToken))
            {
                var dte = await TestServices.Shell.GetRequiredGlobalServiceAsync<SDTE, EnvDTE.DTE>(HangMitigatingCancellationToken);
                if (dte.Debugger.CurrentMode != EnvDTE.dbgDebugMode.dbgDesignMode)
                {
                    dte.Debugger.TerminateAll();
                }

                await TestServices.SolutionExplorer.CloseSolutionAsync(HangMitigatingCancellationToken);
            }

            await TestServices.Workarounds.RemoveConflictingKeyBindingsAsync(HangMitigatingCancellationToken);
            await TestServices.StateReset.ResetGlobalOptionsAsync(HangMitigatingCancellationToken);
            await TestServices.StateReset.ResetHostSettingsAsync(HangMitigatingCancellationToken);

            await TestServices.Workarounds.WaitForGitHubCoPilotAsync(HangMitigatingCancellationToken);
        }

        public override async Task DisposeAsync()
        {
            await TestServices.StateReset.CloseActiveWindowsAsync(HangMitigatingCancellationToken);

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

            TestTraceListener.Instance.VerifyNoErrorsAndReset();
        }
    }
}
