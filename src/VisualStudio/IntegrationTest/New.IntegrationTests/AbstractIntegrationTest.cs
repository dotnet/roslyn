// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Extensibility.Testing;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Harness;

namespace Roslyn.VisualStudio.IntegrationTests;

[IdeSettings(
    MinVersion = VisualStudioVersion.VS18,
    RootSuffix =
#if ROSLYN_INTEGRATION_TESTS_EMPTY_ROOT_SUFFIX
        "",
#else
        "RoslynDev",
#endif
    MaxAttempts = 2)]
public abstract class AbstractIntegrationTest : AbstractIdeIntegrationTest
{
    private static string? s_catalogCacheFolder;
    private static AsynchronousOperationListenerProvider? s_listenerProvider;
    private static VisualStudioWorkspace? s_workspace;

    protected const string ProjectName = "TestProj";
    protected const string SolutionName = "TestSolution";

    static AbstractIntegrationTest()
    {
        // Make sure to run the module initializer for Roslyn.Test.Utilities before installing TestTraceListener, or
        // it will replace it with ThrowingTraceListener later.
        RuntimeHelpers.RunModuleConstructor(typeof(TestBase).Module.ModuleHandle);
        TestTraceListener.Install();

        DataCollectionService.RegisterCustomLogger(
            static fileName =>
            {
                if (s_catalogCacheFolder is null)
                    return;

                var file = Path.Combine(s_catalogCacheFolder, "Microsoft.VisualStudio.Default.err");
                if (File.Exists(file))
                {
                    string content;
                    try
                    {
                        content = File.ReadAllText(file);
                    }
                    catch (Exception ex)
                    {
                        content =
                            $"""
                            Exception thrown while reading '{file}':
                            {ex}
                            """;
                    }

                    File.WriteAllText(fileName, content);
                }
            },
            logId: "MEF",
            extension: "err");

        IdeStateCollector.RegisterCustomState(
            "Pending asynchronous operations",
            static () =>
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

        IdeStateCollector.RegisterCustomState(
            "Solution state",
            static () =>
            {
                if (s_workspace is null)
                    return "Unknown";

                var messageBuilder = new StringBuilder();
                foreach (var project in s_workspace.CurrentSolution.Projects)
                {
                    messageBuilder.AppendLine($"Project '{project.Name}'");
                    messageBuilder.AppendLine($"  Metadata References");
                    foreach (var reference in project.MetadataReferences)
                    {
                        messageBuilder.AppendLine($"    {reference.Display}");
                    }

                    messageBuilder.AppendLine($"  Project References");
                    foreach (var reference in project.ProjectReferences)
                    {
                        messageBuilder.AppendLine($"    {reference.ProjectId}");
                    }

                    messageBuilder.AppendLine($"  Analyzer References");
                    foreach (var reference in project.AnalyzerReferences)
                    {
                        messageBuilder.AppendLine($"    {reference.FullPath}");
                    }

                    messageBuilder.AppendLine($"  Documents");
                    foreach (var document in project.Documents)
                    {
                        var path = string.Join("/", document.Folders);
                        path = path == "" ? document.Name : $"{path}/{document.Name}";
                        messageBuilder.AppendLine($"    {path}");
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

        if (s_catalogCacheFolder is null)
        {
            var componentModel = await TestServices.Shell.GetRequiredGlobalServiceAsync<SVsComponentModelHost, IVsComponentModelHost>(HangMitigatingCancellationToken);
            ErrorHandler.ThrowOnFailure(componentModel.GetCatalogCacheFolder(out s_catalogCacheFolder));
        }

        s_listenerProvider ??= await TestServices.Shell.GetComponentModelServiceAsync<AsynchronousOperationListenerProvider>(HangMitigatingCancellationToken);
        s_workspace ??= await TestServices.Shell.GetComponentModelServiceAsync<VisualStudioWorkspace>(HangMitigatingCancellationToken);

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
    }

    public override async Task DisposeAsync()
    {
        using var cleanupCancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        var cleanupCancellationToken = cleanupCancellationTokenSource.Token;

        await TestServices.StateReset.CloseActiveWindowsAsync(cleanupCancellationToken);

        var dte = await TestServices.Shell.GetRequiredGlobalServiceAsync<SDTE, EnvDTE.DTE>(cleanupCancellationToken);
        if (dte.Debugger.CurrentMode != EnvDTE.dbgDebugMode.dbgDesignMode)
        {
            dte.Debugger.TerminateAll();
            await TestServices.Workspace.WaitForAllAsyncOperationsAsync(
                [
                    FeatureAttribute.Workspace,
                    FeatureAttribute.EditAndContinue,
                ],
                cleanupCancellationToken);
        }

        await base.DisposeAsync();

        TestTraceListener.Instance.VerifyNoErrorsAndReset();
    }
}
