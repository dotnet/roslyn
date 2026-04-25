// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.CodeAnalysis.Razor.NestedFiles;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Razor;
using Microsoft.VisualStudio.Razor.Debugging;
using Microsoft.VisualStudio.Razor.LanguageClient.Options;
using Microsoft.VisualStudio.Razor.Logging;
using Microsoft.VisualStudio.Razor.ProjectSystem;
using Microsoft.VisualStudio.Razor.Snippets;
using Microsoft.VisualStudio.RazorExtension.NestedFiles;
using Microsoft.VisualStudio.RazorExtension.Snippets;
using Microsoft.VisualStudio.RazorExtension.SyntaxVisualizer;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.RazorExtension;

[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
[AboutDialogInfo(PackageGuidString, "Razor (ASP.NET Core)", "#110", "#112", IconResourceID = "#400")]
[ProvideService(typeof(RazorLanguageService))]
[ProvideLanguageService(typeof(RazorLanguageService), RazorConstants.RazorLSPContentTypeName, 110)]
[ProvideMenuResource("Menus.ctmenu", 1)]
[ProvideToolWindow(typeof(SyntaxVisualizerToolWindow))]
[ProvideSettingsManifest(PackageRelativeManifestFile = @"UnifiedSettings\razor.registration.json")]
[Guid(PackageGuidString)]
// We activate cohosting when the first Razor file is opened. This matches the previous behavior where the
// LSP client MEF export had the Razor content type metadata.
[ProvideUIContextRule(
        contextGuid: RazorConstants.RazorCohostingUIContext,
        name: "Razor Cohosting Activation",
        expression: "RazorContentType",
        termNames: ["RazorContentType"],
        termValues: [$"ActiveEditorContentType:{RazorConstants.RazorLSPContentTypeName}"])]
// Activate context menu commands when a .razor or .cshtml file (or their nested files) is selected or opened
[ProvideAutoLoad(GuidRazorFileContextString, PackageAutoLoadFlags.BackgroundLoad)]
[ProvideUIContextRule(
        contextGuid: GuidRazorFileContextString,
        name: "Razor File Selected",
        expression: "DotNetCoreRazorProject & (RazorFile | CshtmlFile | RazorNestedFile | CshtmlNestedFile)",
        termNames: ["DotNetCoreRazorProject", "RazorFile", "CshtmlFile", "RazorNestedFile", "CshtmlNestedFile"],
        termValues: ["ActiveProjectCapability:DotNetCoreRazor", @"HierSingleSelectionName:\.razor$", @"HierSingleSelectionName:\.cshtml$", @"HierSingleSelectionName:\.razor\.", @"HierSingleSelectionName:\.cshtml\."])]
internal sealed class RazorPackage : AsyncPackage
{
    public const string PackageGuidString = "13b72f58-279e-49e0-a56d-296be02f0805";

    internal const string GuidSyntaxVisualizerMenuCmdSetString = "a3a603a2-2b17-4ce2-bd21-cbb8ccc084ec";
    internal static readonly Guid GuidSyntaxVisualizerMenuCmdSet = new Guid(GuidSyntaxVisualizerMenuCmdSetString);
    internal const uint CmdIDRazorSyntaxVisualizer = 0x101;

    // Razor nested files command set
    internal const string GuidRazorNestedFilesCmdSetString = "8B2B3C5D-6E4A-4F9B-9C8D-1A2B3C4D5E6F";
    internal static readonly Guid GuidRazorNestedFilesCmdSet = new Guid(GuidRazorNestedFilesCmdSetString);

    internal const uint CmdIdAddOrViewNestedCsFile = 0x0100;
    internal const uint CmdIdAddOrViewNestedCssFile = 0x0101;
    internal const uint CmdIdAddOrViewNestedJsFile = 0x0102;
    internal const uint CmdIdViewPageEditor = 0x0203;
    internal const uint CmdIdAddNestedCsFileEditor = 0x0204;

    // UI Context for when a .razor or .cshtml file is selected
    internal const string GuidRazorFileContextString = "7C3F2F9E-8D4A-4B6C-9E1F-5A8D7C6B3E2D";
    internal static readonly Guid GuidRazorFileContext = new Guid(GuidRazorFileContextString);

    private OptionsStorage? _optionsStorage = null;

    protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        var container = this as IServiceContainer;
        container.AddService(typeof(RazorLanguageService), (container, type) =>
        {
            var componentModel = (IComponentModel)GetGlobalService(typeof(SComponentModel));
            var breakpointResolver = componentModel.GetService<IRazorBreakpointResolver>();
            var proximityExpressionResolver = componentModel.GetService<IRazorProximityExpressionResolver>();
            var uiThreadOperationExecutor = componentModel.GetService<IUIThreadOperationExecutor>();
            var editorAdaptersFactory = componentModel.GetService<IVsEditorAdaptersFactoryService>();
            var lspServerActivationTracker = componentModel.GetService<ILspServerActivationTracker>();
            var joinableTaskContext = componentModel.GetService<JoinableTaskContext>();

            return new RazorLanguageService(breakpointResolver, proximityExpressionResolver, lspServerActivationTracker, uiThreadOperationExecutor, editorAdaptersFactory, joinableTaskContext.Factory);
        }, promote: true);

        // Add our command handlers for menu (commands must exist in the .vsct file).
        if (await GetServiceAsync(typeof(IMenuCommandService)) is OleMenuCommandService mcs)
        {
            // Create the command for the tool window.
            var toolwndCommandID = new CommandID(GuidSyntaxVisualizerMenuCmdSet, (int)CmdIDRazorSyntaxVisualizer);
            var menuToolWin = new MenuCommand(ShowToolWindow, toolwndCommandID);
            mcs.AddCommand(menuToolWin);

            // Register nested file commands
            RegisterNestedFileCommands(mcs);
        }

        var componentModel = (IComponentModel)GetGlobalService(typeof(SComponentModel));
        _optionsStorage = componentModel.GetService<OptionsStorage>();
        CreateSnippetService(componentModel);

        // LogHub can be initialized off the UI thread
        await TaskScheduler.Default;

        var traceProvider = componentModel.GetService<RazorLogHubTraceProvider>();
        await traceProvider.InitializeTraceAsync("Razor", 1, cancellationToken);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _optionsStorage?.Dispose();
        _optionsStorage = null;
    }

    private SnippetService CreateSnippetService(IComponentModel componentModel)
    {
        var joinableTaskContext = componentModel.GetService<JoinableTaskContext>();
        var cache = componentModel.GetService<SnippetCache>();
        return new SnippetService(joinableTaskContext.Factory, this, cache, _optionsStorage.AssumeNotNull());
    }

    /// <summary>
    /// This function is called when the user clicks the menu item that shows the
    /// tool window. See the Initialize method to see how the menu item is associated to
    /// this function using the OleMenuCommandService service and the MenuCommand class.
    /// </summary>
    private void ShowToolWindow(object sender, EventArgs e)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        // Get the instance number 0 of this tool window. This window is single instance so this instance
        // is actually the only one. The last flag is set to true so that if the tool window does not exist
        // it will be created.
        var window = (SyntaxVisualizerToolWindow)FindToolWindow(typeof(SyntaxVisualizerToolWindow), id: 0, create: true);
        if (window?.Frame is not IVsWindowFrame windowFrame)
        {
            throw new NotSupportedException("Can not create window");
        }

        // Initialize command handlers in the window
        if (!window.CommandHandlersInitialized)
        {
            var mcs = (IMenuCommandService?)GetService(typeof(IMenuCommandService));
            if (mcs is not null)
            {
                window.InitializeCommands(mcs, GuidSyntaxVisualizerMenuCmdSet);
            }
        }

        ErrorHandler.ThrowOnFailure(windowFrame.Show());
    }

    /// <summary>
    /// Registers the nested file commands (CSS, C#, Javascript) in the menu command service
    /// for both Solution Explorer and editor context menus.
    /// </summary>
    private void RegisterNestedFileCommands(OleMenuCommandService mcs)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        var componentModel = (IComponentModel)GetGlobalService(typeof(SComponentModel));
        var requestInvoker = new Lazy<LSPRequestInvokerWrapper>(() => componentModel.GetService<LSPRequestInvokerWrapper>());

        // Add nested file commands
        AddMenuNestedFileCommand(".cs", NestedFileKind.CSharp, (int)CmdIdAddOrViewNestedCsFile, GuidRazorNestedFilesCmdSet, allowExternalHandlers: false, hideWhenFileExists: false);
        AddMenuNestedFileCommand(".css", NestedFileKind.Css, (int)CmdIdAddOrViewNestedCssFile, GuidRazorNestedFilesCmdSet, allowExternalHandlers: false, hideWhenFileExists: false);
        AddMenuNestedFileCommand(".js", NestedFileKind.JavaScript, (int)CmdIdAddOrViewNestedJsFile, GuidRazorNestedFilesCmdSet, allowExternalHandlers: false, hideWhenFileExists: false);

        // .cs View Code (Editor) — override standard ViewCode (F7) command so VS displays the F7
        // keybinding annotation. Only shows when .cs file exists; yields when missing so the
        // cmdidAddNestedCsFileEditor command (without F7) handles the "Add" case instead.
        // When not in a Razor file, sets Supported = false to fall through to default ViewCode.
        AddMenuNestedFileCommand(".cs", NestedFileKind.CSharp, (int)VSConstants.VSStd97CmdID.ViewCode, VSConstants.GUID_VSStandardCommandSet97, allowExternalHandlers: true, hideWhenFileExists: false);

        // .cs Add Code (Editor) — shows "Add .cs file" only when the .cs doesn't exist yet.
        // Uses a separate command ID from ViewCode so it doesn't display the F7 keybinding.
        AddMenuNestedFileCommand(".cs", NestedFileKind.CSharp, (int)CmdIdAddNestedCsFileEditor, GuidRazorNestedFilesCmdSet, allowExternalHandlers: false, hideWhenFileExists: true);

        // View Page Command (Editor) — appears in nested file editors (.cshtml.cs, etc.)
        var viewPageHandler = new ViewPageCommandHandler(this);
        AddMenuCommand((int)CmdIdViewPageEditor, GuidRazorNestedFilesCmdSet, viewPageHandler.Execute, viewPageHandler.OnBeforeQueryStatus);

        return;

        void AddMenuCommand(int cmdId, Guid cmdSet, EventHandler executeHandler, EventHandler queryStatusHandler)
        {
            var cmdID = new CommandID(cmdSet, cmdId);
            var command = new OleMenuCommand(executeHandler, cmdID);
            command.BeforeQueryStatus += queryStatusHandler;
            mcs.AddCommand(command);
        }

        void AddMenuNestedFileCommand(string fileExtension, NestedFileKind fileKind, int cmdId, Guid cmdSet, bool allowExternalHandlers, bool hideWhenFileExists)
        {
            var handler = new NestedFileCommandHandler(this, fileExtension, fileKind, requestInvoker, allowExternalHandlers, hideWhenFileExists);
            AddMenuCommand(cmdId, cmdSet, handler.Execute, handler.OnBeforeQueryStatus);
        }
    }
}
