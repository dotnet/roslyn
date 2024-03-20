// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.WinForms.Interfaces;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation;

/// <summary>
/// The base class of both the Roslyn editor factories.
/// </summary>
internal abstract class AbstractEditorFactory : IVsEditorFactory, IVsEditorFactory4, IVsEditorFactoryNotify
{
    private readonly IComponentModel _componentModel;
    private Microsoft.VisualStudio.OLE.Interop.IServiceProvider? _oleServiceProvider;
    private bool _encoding;

    protected AbstractEditorFactory(IComponentModel componentModel)
        => _componentModel = componentModel;

    protected abstract string ContentTypeName { get; }
    protected abstract string LanguageName { get; }

    /// <summary>
    /// The project that is used to format newly added documents is in an unknown state - it might be
    /// fully realized, we might have only recieved part of the data about it, or it could be a temporary
    /// one that we create solely for the purpose of new document formatting. Since the language version
    /// informs what types of formatting changes might be possible, this method exists to ensure that we
    /// at least provide that piece of information regardless of anything else.
    /// </summary>
    protected abstract Project GetProjectWithCorrectParseOptionsForProject(Project project, IVsHierarchy hierarchy);

    public void SetEncoding(bool value)
        => _encoding = value;

    int IVsEditorFactory.Close()
        => VSConstants.S_OK;

    public int CreateEditorInstance(
        uint grfCreateDoc,
        string pszMkDocument,
        string? pszPhysicalView,
        IVsHierarchy vsHierarchy,
        uint itemid,
        IntPtr punkDocDataExisting,
        out IntPtr ppunkDocView,
        out IntPtr ppunkDocData,
        out string pbstrEditorCaption,
        out Guid pguidCmdUI,
        out int pgrfCDW)
    {
        Contract.ThrowIfNull(_oleServiceProvider);

        ppunkDocView = IntPtr.Zero;
        ppunkDocData = IntPtr.Zero;
        pbstrEditorCaption = string.Empty;
        pguidCmdUI = Guid.Empty;
        pgrfCDW = 0;

        var physicalView = pszPhysicalView ?? "Code";
        IVsTextBuffer? textBuffer = null;

        // Is this document already open? If so, let's see if it's a IVsTextBuffer we should re-use. This allows us
        // to properly handle multiple windows open for the same document.
        if (punkDocDataExisting != IntPtr.Zero)
        {
            var docDataExisting = Marshal.GetObjectForIUnknown(punkDocDataExisting);

            textBuffer = docDataExisting as IVsTextBuffer;

            if (textBuffer == null)
            {
                // We are incompatible with the existing doc data
                return VSConstants.VS_E_INCOMPATIBLEDOCDATA;
            }
        }

        var editorAdaptersFactoryService = _componentModel.GetService<IVsEditorAdaptersFactoryService>();

        // Do we need to create a text buffer?
        if (textBuffer == null)
        {
            textBuffer = (IVsTextBuffer)GetDocumentData(grfCreateDoc, pszMkDocument, vsHierarchy, itemid);
            Contract.ThrowIfNull(textBuffer, $"Failed to get document data for {pszMkDocument}");
        }

        // If the text buffer is marked as read-only, ensure that the padlock icon is displayed
        // next the new window's title and that [Read Only] is appended to title.
        var readOnlyStatus = READONLYSTATUS.ROSTATUS_NotReadOnly;
        if (ErrorHandler.Succeeded(textBuffer.GetStateFlags(out var textBufferFlags)) &&
            0 != (textBufferFlags & ((uint)BUFFERSTATEFLAGS.BSF_FILESYS_READONLY | (uint)BUFFERSTATEFLAGS.BSF_USER_READONLY)))
        {
            readOnlyStatus = READONLYSTATUS.ROSTATUS_ReadOnly;
        }

        switch (physicalView)
        {
            case "Form":

                if (CreateWinFormsEditorInstance(
                    vsHierarchy,
                    itemid,
                    textBuffer,
                    readOnlyStatus,
                    out ppunkDocView,
                    out pbstrEditorCaption,
                    out pguidCmdUI) == VSConstants.E_FAIL)
                {
                    goto case "Code";
                }

                break;

            case "Code":

                var codeWindow = editorAdaptersFactoryService.CreateVsCodeWindowAdapter(_oleServiceProvider);
                codeWindow.SetBuffer((IVsTextLines)textBuffer);

                codeWindow.GetEditorCaption(readOnlyStatus, out pbstrEditorCaption);

                ppunkDocView = Marshal.GetIUnknownForObject(codeWindow);
                pguidCmdUI = VSConstants.GUID_TextEditorFactory;

                break;

            default:

                return VSConstants.E_INVALIDARG;
        }

        ppunkDocData = Marshal.GetIUnknownForObject(textBuffer);

        return VSConstants.S_OK;
    }

    public object GetDocumentData(uint grfCreate, string pszMkDocument, IVsHierarchy pHier, uint itemid)
    {
        Contract.ThrowIfNull(_oleServiceProvider);
        var editorAdaptersFactoryService = _componentModel.GetService<IVsEditorAdaptersFactoryService>();
        var contentTypeRegistryService = _componentModel.GetService<IContentTypeRegistryService>();
        var contentType = contentTypeRegistryService.GetContentType(ContentTypeName);
        var textBuffer = editorAdaptersFactoryService.CreateVsTextBufferAdapter(_oleServiceProvider, contentType);

        if (_encoding)
        {
            if (textBuffer is IVsUserData userData)
            {
                // The editor shims require that the boxed value when setting the PromptOnLoad flag is a uint
                var hresult = userData.SetData(
                    VSConstants.VsTextBufferUserDataGuid.VsBufferEncodingPromptOnLoad_guid,
                    (uint)__PROMPTONLOADFLAGS.codepagePrompt);

                Marshal.ThrowExceptionForHR(hresult);
            }
        }

        return textBuffer;
    }

    public object GetDocumentView(uint grfCreate, string pszPhysicalView, IVsHierarchy pHier, IntPtr punkDocData, uint itemid)
    {
        // There is no scenario need currently to implement this method.
        throw new NotImplementedException();
    }

    public string GetEditorCaption(string pszMkDocument, string pszPhysicalView, IVsHierarchy pHier, IntPtr punkDocData, out Guid pguidCmdUI)
    {
        // It is not possible to get this information without initializing the designer.
        // There is no other scenario need currently to implement this method.
        throw new NotImplementedException();
    }

    public bool ShouldDeferUntilIntellisenseIsReady(uint grfCreate, string pszMkDocument, string pszPhysicalView)
    {
        return "Form".Equals(pszPhysicalView, StringComparison.OrdinalIgnoreCase);
    }

    public int MapLogicalView(ref Guid rguidLogicalView, out string? pbstrPhysicalView)
    {
        pbstrPhysicalView = null;

        if (rguidLogicalView == VSConstants.LOGVIEWID.Primary_guid ||
            rguidLogicalView == VSConstants.LOGVIEWID.Debugging_guid ||
            rguidLogicalView == VSConstants.LOGVIEWID.Code_guid ||
            rguidLogicalView == VSConstants.LOGVIEWID.TextView_guid)
        {
            return VSConstants.S_OK;
        }
        else if (rguidLogicalView == VSConstants.LOGVIEWID.Designer_guid)
        {
            pbstrPhysicalView = "Form";
            return VSConstants.S_OK;
        }
        else
        {
            return VSConstants.E_NOTIMPL;
        }
    }

    int IVsEditorFactory.SetSite(Microsoft.VisualStudio.OLE.Interop.IServiceProvider psp)
    {
        _oleServiceProvider = psp;
        return VSConstants.S_OK;
    }

    int IVsEditorFactoryNotify.NotifyDependentItemSaved(IVsHierarchy pHier, uint itemidParent, string pszMkDocumentParent, uint itemidDpendent, string pszMkDocumentDependent)
        => VSConstants.S_OK;

    int IVsEditorFactoryNotify.NotifyItemAdded(uint grfEFN, IVsHierarchy pHier, uint itemid, string pszMkDocument)
    {
        // Is this being added from a template?
        if (((__EFNFLAGS)grfEFN & __EFNFLAGS.EFN_ClonedFromTemplate) != 0)
        {
            var uiThreadOperationExecutor = _componentModel.GetService<IUIThreadOperationExecutor>();
            // TODO(cyrusn): Can this be cancellable?
            uiThreadOperationExecutor.Execute(
                "Intellisense",
                defaultDescription: "",
                allowCancellation: false,
                showProgress: false,
                action: c => FormatDocumentCreatedFromTemplate(pHier, itemid, pszMkDocument, c.UserCancellationToken));
        }

        return VSConstants.S_OK;
    }

    int IVsEditorFactoryNotify.NotifyItemRenamed(IVsHierarchy pHier, uint itemid, string pszMkDocumentOld, string pszMkDocumentNew)
        => VSConstants.S_OK;

    private void FormatDocumentCreatedFromTemplate(IVsHierarchy hierarchy, uint itemid, string filePath, CancellationToken cancellationToken)
    {
        var threadingContext = _componentModel.GetService<IThreadingContext>();
        threadingContext.JoinableTaskFactory.Run(() => FormatDocumentCreatedFromTemplateAsync(hierarchy, itemid, filePath, cancellationToken));
    }

    // NOTE: This function has been created to hide IWinFormsEditorFactory type in non-WinForms scenarios (e.g. editing .cs or .vb file)
    // so that its corresponding dll doesn't get loaded. Due to this reason, function inlining has been disabled.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private int CreateWinFormsEditorInstance(
        IVsHierarchy vsHierarchy,
        uint itemid,
        IVsTextBuffer textBuffer,
        READONLYSTATUS readOnlyStatus,
        out IntPtr ppunkDocView,
        out string pbstrEditorCaption,
        out Guid pguidCmdUI)
    {
        ppunkDocView = IntPtr.Zero;
        pbstrEditorCaption = string.Empty;
        pguidCmdUI = Guid.Empty;

        var winFormsEditorFactory = (IWinFormsEditorFactory)PackageUtilities.QueryService<IWinFormsEditorFactory>(_oleServiceProvider);

        return winFormsEditorFactory is null
            ? VSConstants.E_FAIL
            : winFormsEditorFactory.CreateEditorInstance(
                vsHierarchy,
                itemid,
                _oleServiceProvider,
                textBuffer,
                readOnlyStatus,
                out ppunkDocView,
                out pbstrEditorCaption,
                out pguidCmdUI);
    }

    private async Task FormatDocumentCreatedFromTemplateAsync(IVsHierarchy hierarchy, uint itemid, string filePath, CancellationToken cancellationToken)
    {
        // A file has been created on disk which the user added from the "Add Item" dialog. We need
        // to include this in a workspace to figure out the right options it should be formatted with.
        // This requires us to place it in the correct project.
        var workspace = _componentModel.GetService<VisualStudioWorkspace>();
        var solution = workspace.CurrentSolution;

        Project? projectToAddTo = null;

        foreach (var projectId in solution.ProjectIds)
        {
            if (workspace.GetHierarchy(projectId) == hierarchy)
            {
                projectToAddTo = solution.GetRequiredProject(projectId);
                break;
            }
        }

        if (projectToAddTo == null)
        {
            // We don't have a project for this, so we'll just make up a fake project altogether
            projectToAddTo = solution.AddProject(
                name: nameof(FormatDocumentCreatedFromTemplate),
                assemblyName: nameof(FormatDocumentCreatedFromTemplate),
                language: LanguageName);

            // We have to discover .editorconfig files ourselves to ensure that code style rules are followed.
            // Normally the project system would tell us about these.
            projectToAddTo = AddEditorConfigFiles(projectToAddTo, Path.GetDirectoryName(filePath));
        }

        // We need to ensure that decisions made during new document formatting are based on the right language
        // version from the project system, but the NotifyItemAdded event happens before a design time build,
        // and sometimes before we have even been told about the projects existence, so we have to ask the hierarchy
        // for the language version to use.
        projectToAddTo = GetProjectWithCorrectParseOptionsForProject(projectToAddTo, hierarchy);

        var documentId = DocumentId.CreateNewId(projectToAddTo.Id);

        var fileLoader = new WorkspaceFileTextLoader(solution.Services, filePath, defaultEncoding: null);
        var forkedSolution = projectToAddTo.Solution.AddDocument(
            DocumentInfo.Create(
                documentId,
                name: filePath,
                loader: fileLoader,
                filePath: filePath));

        var addedDocument = forkedSolution.GetRequiredDocument(documentId);

        var globalOptions = _componentModel.GetService<IGlobalOptionService>();
        var cleanupOptions = await addedDocument.GetCodeCleanupOptionsAsync(globalOptions, cancellationToken).ConfigureAwait(true);

        // Call out to various new document formatters to tweak what they want
        var formattingService = addedDocument.GetLanguageService<INewDocumentFormattingService>();
        if (formattingService is not null)
        {
            addedDocument = await formattingService.FormatNewDocumentAsync(addedDocument, hintDocument: null, cleanupOptions, cancellationToken).ConfigureAwait(true);
        }

        var rootToFormat = await addedDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(true);

        // Format document
        var unformattedText = await addedDocument.GetValueTextAsync(cancellationToken).ConfigureAwait(true);
        var formattedRoot = Formatter.Format(rootToFormat, workspace.Services.SolutionServices, cleanupOptions.FormattingOptions, cancellationToken);
        var formattedText = formattedRoot.GetText(unformattedText.Encoding, unformattedText.ChecksumAlgorithm);

        // Ensure the line endings are normalized. The formatter doesn't touch everything if it doesn't need to.
        var targetLineEnding = cleanupOptions.FormattingOptions.NewLine;

        var originalText = formattedText;
        foreach (var originalLine in originalText.Lines)
        {
            var originalNewLine = originalText.ToString(CodeAnalysis.Text.TextSpan.FromBounds(originalLine.End, originalLine.EndIncludingLineBreak));

            // Check if we have a line ending, so we don't go adding one to the end if we don't need to.
            if (originalNewLine.Length > 0 && originalNewLine != targetLineEnding)
            {
                var currentLine = formattedText.Lines[originalLine.LineNumber];
                var currentSpan = CodeAnalysis.Text.TextSpan.FromBounds(currentLine.End, currentLine.EndIncludingLineBreak);
                formattedText = formattedText.WithChanges(new TextChange(currentSpan, targetLineEnding));
            }
        }

        IOUtilities.PerformIO(() =>
        {
            using var textWriter = new StreamWriter(filePath, append: false, encoding: formattedText.Encoding);
            // We pass null here for cancellation, since cancelling in the middle of the file write would leave the file corrupted
            formattedText.Write(textWriter, cancellationToken: CancellationToken.None);
        });
    }

    private static Project AddEditorConfigFiles(Project projectToAddTo, string projectFolder)
    {
        do
        {
            projectToAddTo = AddEditorConfigFile(projectToAddTo, projectFolder, out var foundRoot);

            if (foundRoot)
                break;

            projectFolder = Path.GetDirectoryName(projectFolder);
        }
        while (projectFolder is not null);

        return projectToAddTo;

        static Project AddEditorConfigFile(Project project, string folder, out bool foundRoot)
        {
            const string EditorConfigFileName = ".editorconfig";

            foundRoot = false;

            var editorConfigFile = Path.Combine(folder, EditorConfigFileName);

            var text = IOUtilities.PerformIO(() =>
            {
                using var stream = File.OpenRead(editorConfigFile);
                return SourceText.From(stream);
            });

            if (text is null)
                return project;

            return project.AddAnalyzerConfigDocument(EditorConfigFileName, text, filePath: editorConfigFile).Project;
        }
    }
}
