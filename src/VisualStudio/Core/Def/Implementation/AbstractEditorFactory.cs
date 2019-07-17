// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Designer.Interfaces;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    /// <summary>
    /// The base class of both the Roslyn editor factories.
    /// </summary>
    internal abstract partial class AbstractEditorFactory : IVsEditorFactory, IVsEditorFactoryNotify
    {
        private readonly IComponentModel _componentModel;
        private Microsoft.VisualStudio.OLE.Interop.IServiceProvider _oleServiceProvider;
        private bool _encoding;

        protected AbstractEditorFactory(IComponentModel componentModel)
        {
            _componentModel = componentModel;
        }

        protected abstract string ContentTypeName { get; }
        protected abstract string LanguageName { get; }

        public void SetEncoding(bool value)
        {
            _encoding = value;
        }

        int IVsEditorFactory.Close()
        {
            return VSConstants.S_OK;
        }

        public int CreateEditorInstance(
            uint grfCreateDoc,
            string pszMkDocument,
            string pszPhysicalView,
            IVsHierarchy vsHierarchy,
            uint itemid,
            IntPtr punkDocDataExisting,
            out IntPtr ppunkDocView,
            out IntPtr ppunkDocData,
            out string pbstrEditorCaption,
            out Guid pguidCmdUI,
            out int pgrfCDW)
        {
            ppunkDocView = IntPtr.Zero;
            ppunkDocData = IntPtr.Zero;
            pbstrEditorCaption = string.Empty;
            pguidCmdUI = Guid.Empty;
            pgrfCDW = 0;

            var physicalView = pszPhysicalView ?? "Code";
            IVsTextBuffer textBuffer = null;

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
                var contentTypeRegistryService = _componentModel.GetService<IContentTypeRegistryService>();
                var contentType = contentTypeRegistryService.GetContentType(ContentTypeName);
                textBuffer = editorAdaptersFactoryService.CreateVsTextBufferAdapter(_oleServiceProvider, contentType);

                if (_encoding)
                {
                    if (textBuffer is IVsUserData userData)
                    {
                        // The editor shims require that the boxed value when setting the PromptOnLoad flag is a uint
                        var hresult = userData.SetData(
                            VSConstants.VsTextBufferUserDataGuid.VsBufferEncodingPromptOnLoad_guid,
                            (uint)__PROMPTONLOADFLAGS.codepagePrompt);

                        if (ErrorHandler.Failed(hresult))
                        {
                            return hresult;
                        }
                    }
                }
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

                    // We must create the WinForms designer here
                    var loaderName = GetWinFormsLoaderName(vsHierarchy);

                    var designerService = (IVSMDDesignerService)_oleServiceProvider.QueryService<SVSMDDesignerService>();
                    var designerLoader = (IVSMDDesignerLoader)designerService.CreateDesignerLoader(loaderName);

                    try
                    {
                        designerLoader.Initialize(_oleServiceProvider, vsHierarchy, (int)itemid, (IVsTextLines)textBuffer);
                        pbstrEditorCaption = designerLoader.GetEditorCaption((int)readOnlyStatus);

                        var designer = designerService.CreateDesigner(_oleServiceProvider, designerLoader);
                        ppunkDocView = Marshal.GetIUnknownForObject(designer.View);
                        pguidCmdUI = designer.CommandGuid;
                    }
                    catch
                    {
                        designerLoader.Dispose();
                        throw;
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

        private static string GetWinFormsLoaderName(IVsHierarchy vsHierarchy)
        {
            const string LoaderName = "Microsoft.VisualStudio.Design.Serialization.CodeDom.VSCodeDomDesignerLoader";
            const string NewLoaderName = "Microsoft.VisualStudio.Design.Core.Serialization.CodeDom.VSCodeDomDesignerLoader";

            // If this is a netcoreapp3.0 (or newer), we must create the newer WinForms designer.
            // TODO: This check will eventually move into the WinForms designer itself.
            if (!vsHierarchy.TryGetTargetFrameworkMoniker((uint)VSConstants.VSITEMID.Root, out var targetFrameworkMoniker) ||
                string.IsNullOrWhiteSpace(targetFrameworkMoniker))
            {
                return LoaderName;
            }

            try
            {
                var frameworkName = new FrameworkName(targetFrameworkMoniker);

                if (frameworkName.Identifier == ".NETCoreApp" &&
                    frameworkName.Version?.Major >= 3)
                {
                    return NewLoaderName;
                }
            }
            catch
            {
                // Fall back to the old loader name if there are any failures 
                // while parsing the TFM.
            }

            return LoaderName;
        }

        public int MapLogicalView(ref Guid rguidLogicalView, out string pbstrPhysicalView)
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
        {
            return VSConstants.S_OK;
        }

        int IVsEditorFactoryNotify.NotifyItemAdded(uint grfEFN, IVsHierarchy pHier, uint itemid, string pszMkDocument)
        {
            // Is this being added from a template?
            if (((__EFNFLAGS)grfEFN & __EFNFLAGS.EFN_ClonedFromTemplate) != 0)
            {
                var waitIndicator = _componentModel.GetService<IWaitIndicator>();
                // TODO(cyrusn): Can this be cancellable?
                waitIndicator.Wait(
                    "Intellisense",
                    allowCancel: false,
                    action: c => FormatDocumentCreatedFromTemplate(pHier, itemid, pszMkDocument, c.CancellationToken));
            }

            return VSConstants.S_OK;
        }

        int IVsEditorFactoryNotify.NotifyItemRenamed(IVsHierarchy pHier, uint itemid, string pszMkDocumentOld, string pszMkDocumentNew)
        {
            return VSConstants.S_OK;
        }

        private void FormatDocumentCreatedFromTemplate(IVsHierarchy hierarchy, uint itemid, string filePath, CancellationToken cancellationToken)
        {
            // A file has been created on disk which the user added from the "Add Item" dialog. We need
            // to include this in a workspace to figure out the right options it should be formatted with.
            // This requires us to place it in the correct project.
            var workspace = _componentModel.GetService<VisualStudioWorkspace>();
            var solution = workspace.CurrentSolution;

            ProjectId projectIdToAddTo = null;

            foreach (var projectId in solution.ProjectIds)
            {
                if (workspace.GetHierarchy(projectId) == hierarchy)
                {
                    projectIdToAddTo = projectId;
                    break;
                }
            }

            if (projectIdToAddTo == null)
            {
                // We don't have a project for this, so we'll just make up a fake project altogether
                var temporaryProject = solution.AddProject(
                    name: nameof(FormatDocumentCreatedFromTemplate),
                    assemblyName: nameof(FormatDocumentCreatedFromTemplate),
                    language: LanguageName);

                solution = temporaryProject.Solution;
                projectIdToAddTo = temporaryProject.Id;
            }

            var documentId = DocumentId.CreateNewId(projectIdToAddTo);
            var forkedSolution = solution.AddDocument(DocumentInfo.Create(documentId, filePath, loader: new FileTextLoader(filePath, defaultEncoding: null), filePath: filePath));
            var addedDocument = forkedSolution.GetDocument(documentId);

            var rootToFormat = addedDocument.GetSyntaxRootSynchronously(cancellationToken);
            var documentOptions = ThreadHelper.JoinableTaskFactory.Run(() => addedDocument.GetOptionsAsync(cancellationToken));

            var formattedTextChanges = Formatter.GetFormattedTextChanges(rootToFormat, workspace, documentOptions, cancellationToken);
            var formattedText = addedDocument.GetTextSynchronously(cancellationToken).WithChanges(formattedTextChanges);

            // Ensure the line endings are normalized. The formatter doesn't touch everything if it doesn't need to.
            var targetLineEnding = documentOptions.GetOption(FormattingOptions.NewLine);

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
    }
}
