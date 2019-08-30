// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    [Export(typeof(SaveEventsService))]
    internal sealed class SaveEventsService : IVsRunningDocTableEvents3
    {
        private readonly IVsRunningDocumentTable _runningDocumentTable;
        private bool _subscribed;

        private readonly IVsEditorAdaptersFactoryService _editorAdaptersFactoryService;
        private readonly ICommandHandlerServiceFactory _commandHandlerServiceFactory;
        private readonly IVsTextManager _textManager;

        [ImportingConstructor]
        public SaveEventsService(
            IVsEditorAdaptersFactoryService editorAdaptersFactoryService,
            ICommandHandlerServiceFactory commandHandlerServiceFactory,
            SVsServiceProvider serviceProvider)
        {
            _editorAdaptersFactoryService = editorAdaptersFactoryService;
            _commandHandlerServiceFactory = commandHandlerServiceFactory;
            _runningDocumentTable = (IVsRunningDocumentTable)serviceProvider.GetService(typeof(SVsRunningDocumentTable));
            _textManager = (IVsTextManager)serviceProvider.GetService(typeof(SVsTextManager));
        }

        public void StartSendingSaveEvents()
        {
            if (!_subscribed)
            {
                Marshal.ThrowExceptionForHR(_runningDocumentTable.AdviseRunningDocTableEvents(this, out var runningDocumentTableEventCookie));
                _subscribed = true;
            }
        }

        public int OnAfterAttributeChange(uint docCookie, uint grfAttribs)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterAttributeChangeEx(uint docCookie, uint grfAttribs, IVsHierarchy pHierOld, uint itemidOld, string pszMkDocumentOld, IVsHierarchy pHierNew, uint itemidNew, string pszMkDocumentNew)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterDocumentWindowHide(uint docCookie, IVsWindowFrame pFrame)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterFirstDocumentLock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterSave(uint docCookie)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeDocumentWindowShow(uint docCookie, int fFirstShow, IVsWindowFrame pFrame)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeLastDocumentUnlock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeSave(uint docCookie)
        {
            using (Logger.LogBlock(FunctionId.Misc_SaveEventsSink_OnBeforeSave, CancellationToken.None))
            {
                OnBeforeSaveWorker(docCookie);
            }

            return VSConstants.S_OK;
        }

        private void OnBeforeSaveWorker(uint docCookie)
        {
            var docData = IntPtr.Zero;

            try
            {
                // We want to raise a save event for this document. First let's try to get the docData
                Marshal.ThrowExceptionForHR(_runningDocumentTable.GetDocumentInfo(docCookie, out var flags, out var readLocks, out var writeLocks, out var moniker, out var hierarchy, out var itemid, out docData));

                if (Marshal.GetObjectForIUnknown(docData) is IVsTextBuffer textBufferAdapter)
                {
                    var textBuffer = _editorAdaptersFactoryService.GetDocumentBuffer(textBufferAdapter);

                    // Do a quick check that this is a Roslyn file at all before we go do more expensive things
                    if (textBuffer != null && textBuffer.ContentType.IsOfType(ContentTypeNames.RoslynContentType))
                    {
                        if (textBufferAdapter != null)
                        {
                            // OK, we want to go and raise a save event. Currently, CommandArgs demands that we have a view, so let's try to go and find one.
                            _textManager.EnumViews(textBufferAdapter, out var enumTextViews);
                            var views = new IVsTextView[1];
                            uint fetched = 0;

                            if (ErrorHandler.Succeeded(enumTextViews.Next(1, views, ref fetched)) && fetched == 1)
                            {
                                var view = _editorAdaptersFactoryService.GetWpfTextView(views[0]);
                                var commandHandlerService = _commandHandlerServiceFactory.GetService(textBuffer);
                                commandHandlerService.Execute(textBuffer.ContentType, new SaveCommandArgs(view, textBuffer));
                            }
                        }
                    }
                }
            }
            finally
            {
                if (docData != IntPtr.Zero)
                {
                    Marshal.Release(docData);
                }
            }
        }
    }
}
