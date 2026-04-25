// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Razor.Extensions;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.AspNetCore.Razor.Utilities;
using Microsoft.CodeAnalysis.Razor.NestedFiles;
using Microsoft.CodeAnalysis.Razor.Protocol.NestedFiles;
using Microsoft.VisualStudio.Razor.LanguageClient;
using Microsoft.VisualStudio.Razor.ProjectSystem;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.RazorExtension.NestedFiles;

/// <summary>
/// Handles Add/View nested file commands for Razor documents from both Solution Explorer
/// and editor context menus.
/// When the file doesn't exist, sends an LSP request to the Razor language server
/// to create it via workspace/applyEdit. When the file exists, just opens it.
/// </summary>
/// <param name="serviceProvider">VS service provider for accessing shell services.</param>
/// <param name="fileExtension">The nested file extension to add/view (e.g., ".cs", ".css", ".js").</param>
/// <param name="fileKind">The kind of nested file, used when creating new files via LSP.</param>
/// <param name="requestInvoker">Lazy wrapper for sending LSP requests to the Razor language server.</param>
/// <param name="allowExternalHandlers">
/// When true, sets Supported = false instead of Visible = false when the command doesn't apply. 
/// This tells VS that this handler does not own the command, allowing external handlers (e.g., the default 
/// ViewCode/F7 handler) to take over.
/// </param>
/// <param name="hideWhenFileExists">
/// When true, hides the command when the nested file already exists. Used for the editor-only
/// "Add .cs" command (cmdidAddNestedCsFileEditor), which is hidden when the .cs file exists
/// because cmdidViewCode with F7 handles the "View" case instead.
/// </param>
internal sealed class NestedFileCommandHandler(
    IServiceProvider serviceProvider,
    string fileExtension,
    NestedFileKind fileKind,
    Lazy<LSPRequestInvokerWrapper> requestInvoker,
    bool allowExternalHandlers,
    bool hideWhenFileExists)
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly string _fileExtension = fileExtension;
    private readonly NestedFileKind _fileKind = fileKind;
    private readonly Lazy<LSPRequestInvokerWrapper> _requestInvoker = requestInvoker;
    private readonly bool _allowExternalHandlers = allowExternalHandlers;
    private readonly bool _hideWhenFileExists = hideWhenFileExists;

    /// <summary>
    /// Configures the command status and text based on whether the nested file exists.
    /// </summary>
    public void OnBeforeQueryStatus(object sender, EventArgs e)
    {
        if (sender is not OleMenuCommand command)
        {
            return;
        }

        // Check if the Razor file context is active before doing expensive hierarchy queries
        if (!SelectionHelper.IsRazorFileUIContextActive(_serviceProvider)
            || GetSelectedRazorFilePath() is not string razorFilePath)
        {
            if (_allowExternalHandlers)
            {
                command.Supported = false;
            }
            else
            {
                command.Visible = false;
            }

            return;
        }

        var nestedFilePath = GetNestedFilePath(razorFilePath);
        var nestedFileExists = File.Exists(nestedFilePath);

        if (_allowExternalHandlers && !nestedFileExists)
        {
            // yield so another handler can show "Add" (without F7 keybinding)
            command.Supported = false;
            return;
        }

        if (_hideWhenFileExists && nestedFileExists)
        {
            // The nested file exists and we've been told this command should be hidden in that case
            command.Visible = false;
            return;
        }

        var nestedFileName = Path.GetFileName(nestedFilePath);

        command.Supported = true;
        command.Visible = true;
        command.Enabled = true;
        command.Text = nestedFileExists ? Resources.FormatView_Nested_File(nestedFileName) : Resources.FormatAdd_Nested_File(nestedFileName);
    }

    /// <summary>
    /// Executes the command - either opens an existing nested file or creates a new one
    /// via the LSP server and then opens it.
    /// </summary>
    public void Execute(object sender, EventArgs e)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        if (GetSelectedRazorFilePath() is not string razorFilePath)
        {
            return;
        }

        var nestedFilePath = GetNestedFilePath(razorFilePath);

        if (File.Exists(nestedFilePath))
        {
            // View: just open the existing file
            VsShellUtilities.OpenDocument(_serviceProvider, nestedFilePath);
        }
        else
        {
            // Add: send LSP request to create the file, then open it.
            // FileAndForget ensures exceptions are reported to telemetry rather than silently swallowed.
#pragma warning disable VSSDK007 // Fire-and-forget from synchronous EventHandler is intentional
            ThreadHelper.JoinableTaskFactory.RunAsync(
                () => CreateAndOpenNestedFileAsync(razorFilePath, nestedFilePath, CancellationToken.None)).FileAndForget("NestedFileCommandHandler.Execute");
#pragma warning restore VSSDK007
        }
    }

    private async Task CreateAndOpenNestedFileAsync(
        string razorFilePath,
        string nestedFilePath,
        CancellationToken cancellationToken)
    {
        // The cohost endpoint will create the file via workspace/applyEdit.
        // By the time this returns, the file should exist on disk.
        await _requestInvoker.Value.ReinvokeRequestOnServerAsync<AddNestedFileParams, object?>(
            RazorLSPConstants.AddNestedFileName,
            RazorLSPConstants.RoslynLanguageServerName,
            AddNestedFileParams.Create(new Uri(razorFilePath), _fileKind),
            cancellationToken);

        if (File.Exists(nestedFilePath))
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            // The workspace/applyEdit creates the file and inserts content via TextDocumentEdit,
            // which leaves the buffer dirty. Save it so the user sees a clean document.
            VsShellUtilities.SaveFileIfDirty(_serviceProvider, nestedFilePath);
            VsShellUtilities.OpenDocument(_serviceProvider, nestedFilePath);
        }
    }

    /// <summary>
    /// Gets the path to the nested file based on the Razor file path.
    /// </summary>
    private string GetNestedFilePath(string razorFilePath)
    {
        Debug.Assert(_fileExtension.StartsWith('.'));
        return razorFilePath + _fileExtension;
    }

    /// <summary>
    /// Gets the file path of the currently selected/active Razor file.
    /// This works for both Solution Explorer selection and the active editor document,
    /// because IVsMonitorSelection tracks the active window frame's hierarchy item.
    /// </summary>
    private string? GetSelectedRazorFilePath()
    {
        var filePath = SelectionHelper.GetCurrentSelectionPath(_serviceProvider);

        if (filePath is not null
            && FileUtilities.IsAnyRazorFilePath(filePath, StringComparison.OrdinalIgnoreCase)
            && Path.GetFileName(filePath) is string fileName
            && !string.Equals(fileName, ComponentHelpers.ImportsFileName, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(fileName, MvcImportProjectFeature.ImportsFileName, StringComparison.OrdinalIgnoreCase))
        {
            return filePath;
        }

        return null;
    }
}
