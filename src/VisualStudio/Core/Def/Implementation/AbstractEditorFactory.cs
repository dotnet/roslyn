// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Designer.Interfaces;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    /// <summary>
    /// The base class of both the Roslyn editor factories.
    /// </summary>
    internal abstract partial class AbstractEditorFactory : IVsEditorFactory, IVsEditorFactoryNotify
    {
        private readonly Package _package;
        private readonly IComponentModel _componentModel;
        private Microsoft.VisualStudio.OLE.Interop.IServiceProvider _oleServiceProvider;
        private readonly IVsEditorAdaptersFactoryService _editorAdaptersFactoryService;
        private readonly IContentTypeRegistryService _contentTypeRegistryService;
        private readonly IWaitIndicator _waitIndicator;
        private bool _encoding;

        protected AbstractEditorFactory(Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException(nameof(package));
            }

            _package = package;
            _componentModel = (IComponentModel)ServiceProvider.GetService(typeof(SComponentModel));

            _editorAdaptersFactoryService = _componentModel.GetService<IVsEditorAdaptersFactoryService>();
            _contentTypeRegistryService = _componentModel.GetService<IContentTypeRegistryService>();
            _waitIndicator = _componentModel.GetService<IWaitIndicator>();
        }

        protected IServiceProvider ServiceProvider
        {
            get
            {
                return _package;
            }
        }

        protected IComponentModel ComponentModel
        {
            get
            {
                return _componentModel;
            }
        }

        protected abstract string ContentTypeName { get; }

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

            var physicalView = pszPhysicalView == null
                ? "Code"
                : pszPhysicalView;

            IVsTextBuffer textBuffer = null;

            // Is this document already open? If so, let's see if it's a IVsTextBuffer we should re-use. This allows us
            // to properly handle multiple windows open for the same document.
            if (punkDocDataExisting != IntPtr.Zero)
            {
                object docDataExisting = Marshal.GetObjectForIUnknown(punkDocDataExisting);

                textBuffer = docDataExisting as IVsTextBuffer;

                if (textBuffer == null)
                {
                    // We are incompatible with the existing doc data
                    return VSConstants.VS_E_INCOMPATIBLEDOCDATA;
                }
            }

            // Do we need to create a text buffer?
            if (textBuffer == null)
            {
                var contentType = _contentTypeRegistryService.GetContentType(ContentTypeName);
                textBuffer = _editorAdaptersFactoryService.CreateVsTextBufferAdapter(_oleServiceProvider, contentType);

                if (_encoding)
                {
                    var userData = textBuffer as IVsUserData;
                    if (userData != null)
                    {
                        // The editor shims require that the boxed value when setting the PromptOnLoad flag is a uint
                        int hresult = userData.SetData(
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
            READONLYSTATUS readOnlyStatus = READONLYSTATUS.ROSTATUS_NotReadOnly;
            uint textBufferFlags;
            if (ErrorHandler.Succeeded(textBuffer.GetStateFlags(out textBufferFlags)) &&
                0 != (textBufferFlags & ((uint)BUFFERSTATEFLAGS.BSF_FILESYS_READONLY | (uint)BUFFERSTATEFLAGS.BSF_USER_READONLY)))
            {
                readOnlyStatus = READONLYSTATUS.ROSTATUS_ReadOnly;
            }

            switch (physicalView)
            {
                case "Form":

                    // We must create the WinForms designer here
                    const string LoaderName = "Microsoft.VisualStudio.Design.Serialization.CodeDom.VSCodeDomDesignerLoader";
                    var designerService = (IVSMDDesignerService)ServiceProvider.GetService(typeof(SVSMDDesignerService));
                    var designerLoader = (IVSMDDesignerLoader)designerService.CreateDesignerLoader(LoaderName);

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

                    var codeWindow = _editorAdaptersFactoryService.CreateVsCodeWindowAdapter(_oleServiceProvider);
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
                // TODO(cyrusn): Can this be cancellable?
                _waitIndicator.Wait(
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
            // A file has been created on disk which the user added from the "Add Item" dialog. The
            // project system hasn't told us about this file yet, so we're free to edit it directly
            // on disk.

            var contentTypeRegistryService = ComponentModel.GetService<IContentTypeRegistryService>();
            var contentType = contentTypeRegistryService.GetContentType(ContentTypeName);

            var textDocumentFactoryService = ComponentModel.GetService<ITextDocumentFactoryService>();
            using (var textDocument = textDocumentFactoryService.CreateAndLoadTextDocument(filePath, contentType))
            {
                // Some templates will hand us files that aren't CRLF. Let's convert them. This is
                // more or less copied from IEditorOperations.NormalizeLineEndings, but we can't use
                // it because it assumes we have an actual view
                using (var edit = textDocument.TextBuffer.CreateEdit())
                {
                    foreach (var line in textDocument.TextBuffer.CurrentSnapshot.Lines)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        // If there is a linebreak at all...
                        if (line.LineBreakLength != 0)
                        {
                            if (line.GetLineBreakText() != "\r\n")
                            {
                                edit.Replace(line.End, line.LineBreakLength, "\r\n");
                            }
                        }
                    }

                    edit.Apply();
                }

                var formattedTextChanges = GetFormattedTextChanges(ComponentModel.GetService<VisualStudioWorkspace>(), filePath, textDocument.TextBuffer.CurrentSnapshot.AsText(), cancellationToken);

                using (var edit = textDocument.TextBuffer.CreateEdit())
                {
                    foreach (var change in formattedTextChanges)
                    {
                        edit.Replace(change.Span.ToSpan(), change.NewText);
                    }

                    edit.Apply();
                }

                textDocument.Save();
            }
        }

        protected abstract IList<TextChange> GetFormattedTextChanges(VisualStudioWorkspace workspace, string filePath, SourceText text, CancellationToken cancellationToken);
    }
}
