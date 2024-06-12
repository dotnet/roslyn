// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.AddImportOnPaste;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.Editor.InlineRename;
using Microsoft.CodeAnalysis.Editor.Options;
using Microsoft.CodeAnalysis.Editor.VisualBasic.LineCommit;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.InheritanceMargin;
using Microsoft.CodeAnalysis.InlineRename;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Extensibility.Testing;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.LanguageServices.Implementation;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Editor;
using WindowsInput.Native;

namespace Roslyn.VisualStudio.IntegrationTests.InProcess;

[TestService]
internal partial class StateResetInProcess
{
    /// <summary>
    /// Contains the persistence slots of tool windows to close between tests.
    /// </summary>
    /// <seealso cref="__VSFPROPID.VSFPROPID_GuidPersistenceSlot"/>
    private static readonly ImmutableHashSet<Guid> s_windowsToClose = ImmutableHashSet.Create(
        FindReferencesWindowInProcess.FindReferencesWindowGuid,
        new Guid(EnvDTE.Constants.vsWindowKindObjectBrowser),
        new Guid(ToolWindowGuids80.CodedefinitionWindow),
        VSConstants.StandardToolWindows.Immediate);

    public async Task ResetGlobalOptionsAsync(CancellationToken cancellationToken)
    {
        // clear configuration options, so that the workspace configuration global option update below is effective:
        var workspace = await TestServices.Shell.GetComponentModelServiceAsync<VisualStudioWorkspace>(cancellationToken);
        var configurationService = (WorkspaceConfigurationService)workspace.Services.GetRequiredService<IWorkspaceConfigurationService>();
        configurationService.Clear();

        var globalOptions = await GetComponentModelServiceAsync<IGlobalOptionService>(cancellationToken);
        ResetOption(globalOptions, CSharpCodeStyleOptions.NamespaceDeclarations);
        ResetOption(globalOptions, InheritanceMarginOptionsStorage.InheritanceMarginCombinedWithIndicatorMargin);
        ResetOption(globalOptions, InlineRenameSessionOptionsStorage.PreviewChanges);
        ResetOption(globalOptions, InlineRenameSessionOptionsStorage.RenameFile);
        ResetOption(globalOptions, InlineRenameSessionOptionsStorage.RenameInComments);
        ResetOption(globalOptions, InlineRenameSessionOptionsStorage.RenameInStrings);
        ResetOption(globalOptions, InlineRenameSessionOptionsStorage.RenameOverloads);
        ResetOption(globalOptions, InlineRenameUIOptionsStorage.UseInlineAdornment);
        ResetOption(globalOptions, MetadataAsSourceOptionsStorage.NavigateToDecompiledSources);
        ResetOption(globalOptions, WorkspaceConfigurationOptionsStorage.EnableOpeningSourceGeneratedFilesInWorkspace);
        ResetOption(globalOptions, WorkspaceConfigurationOptionsStorage.SourceGeneratorExecution);
        ResetOption(globalOptions, WorkspaceConfigurationOptionsStorage.SourceGeneratorExecutionManualFeatureFlag);
        ResetPerLanguageOption(globalOptions, BlockStructureOptionsStorage.CollapseSourceLinkEmbeddedDecompiledFilesWhenFirstOpened);
        ResetPerLanguageOption(globalOptions, CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces);
        ResetPerLanguageOption(globalOptions, CompletionOptionsStorage.TriggerInArgumentLists);
        ResetPerLanguageOption(globalOptions, InheritanceMarginOptionsStorage.InheritanceMarginIncludeGlobalImports);
        ResetPerLanguageOption(globalOptions, InheritanceMarginOptionsStorage.ShowInheritanceMargin);
        ResetPerLanguageOption(globalOptions, NavigationBarViewOptionsStorage.ShowNavigationBar);
        ResetPerLanguageOption(globalOptions, SolutionCrawlerOptionsStorage.BackgroundAnalysisScopeOption);
        ResetPerLanguageOption(globalOptions, SolutionCrawlerOptionsStorage.CompilerDiagnosticsScopeOption);
        ResetPerLanguageOption(globalOptions, VisualStudioNavigationOptionsStorage.NavigateToObjectBrowser);
        ResetPerLanguageOption(globalOptions, AddImportOnPasteOptionsStorage.AddImportsOnPaste);
        ResetPerLanguageOption(globalOptions, LineCommitOptionsStorage.PrettyListing);
        ResetPerLanguageOption(globalOptions, CompletionViewOptionsStorage.EnableArgumentCompletionSnippets);

        static void ResetOption<T>(IGlobalOptionService globalOptions, Option2<T> option)
        {
            globalOptions.SetGlobalOption(option, option.DefaultValue);
        }

        static void ResetPerLanguageOption<T>(IGlobalOptionService globalOptions, PerLanguageOption2<T> option)
        {
            globalOptions.SetGlobalOption(option, LanguageNames.CSharp, option.DefaultValue);
            globalOptions.SetGlobalOption(option, LanguageNames.VisualBasic, option.DefaultValue);
        }
    }

    public async Task ResetHostSettingsAsync(CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        // Use default navigation behavior
        await TestServices.Editor.ConfigureAsyncNavigation(AsyncNavigationKind.Default, cancellationToken);

        // Suggestion mode defaults to on for debugger views, and off for other views.
        await TestServices.Editor.SetUseSuggestionModeAsync(forDebuggerTextView: true, true, cancellationToken);
        await TestServices.Editor.SetUseSuggestionModeAsync(forDebuggerTextView: false, false, cancellationToken);

        // Make sure responsive completion doesn't interfere if integration tests run slowly.
        await DisableResponsiveCompletion(cancellationToken);

        await CloseActiveWindowsAsync(cancellationToken);
    }

    public async Task DisableResponsiveCompletion(CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        // Make sure responsive completion doesn't interfere if integration tests run slowly.
        var editorOptionsFactory = await GetComponentModelServiceAsync<IEditorOptionsFactoryService>(cancellationToken);
        var options = editorOptionsFactory.GlobalOptions;
        options.SetOptionValue(DefaultOptions.ResponsiveCompletionOptionId, false);

        var latencyGuardOptionKey = new EditorOptionKey<bool>("EnableTypingLatencyGuard");
        options.SetOptionValue(latencyGuardOptionKey, false);
    }

    public async Task CloseActiveWindowsAsync(CancellationToken cancellationToken)
    {
        await TestServices.AddParameterDialog.CloseWindowAsync(cancellationToken);
        await TestServices.ChangeSignatureDialog.CloseWindowAsync(cancellationToken);
        await TestServices.ExtractInterfaceDialog.CloseWindowAsync(cancellationToken);
        await TestServices.MoveToNamespaceDialog.CloseWindowAsync(cancellationToken);
        await TestServices.PickMembersDialog.CloseWindowAsync(cancellationToken);

        // Close any modal windows
        var mainWindow = await TestServices.Shell.GetMainWindowAsync(cancellationToken);
        var modalWindow = IntegrationHelper.GetModalWindowFromParentWindow(mainWindow);
        while (modalWindow != IntPtr.Zero)
        {
            if ("Default IME" == IntegrationHelper.GetTitleForWindow(modalWindow))
            {
                // "Default IME" shows up as a modal window in some cases where there is no other window blocking
                // input to Visual Studio.
                break;
            }

            await TestServices.Input.SendWithoutActivateAsync(VirtualKeyCode.ESCAPE, cancellationToken);
            var nextModalWindow = IntegrationHelper.GetModalWindowFromParentWindow(mainWindow);
            if (nextModalWindow == modalWindow)
            {
                // Don't loop forever if windows aren't closing.
                break;
            }

            modalWindow = nextModalWindow;
        }

        // Close tool windows where desired (see s_windowsToClose)
        await foreach (var window in TestServices.Shell.EnumerateWindowsAsync(__WindowFrameTypeFlags.WINDOWFRAMETYPE_Tool, cancellationToken).WithCancellation(cancellationToken))
        {
            ErrorHandler.ThrowOnFailure(window.GetGuidProperty((int)__VSFPROPID.VSFPROPID_GuidPersistenceSlot, out var persistenceSlot));
            if (s_windowsToClose.Contains(persistenceSlot))
            {
                window.CloseFrame((uint)__FRAMECLOSE.FRAMECLOSE_NoSave);
            }
        }
    }
}
