// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    internal partial class DocumentProvider
    {
        private class RunningDocTableEventsSink : IVsRunningDocTableEvents3
        {
            private readonly DocumentProvider _documentProvider;

            public RunningDocTableEventsSink(DocumentProvider documentProvider)
            {
                _documentProvider = documentProvider;
            }

            public int OnAfterAttributeChange(uint docCookie, uint grfAttribs)
            {
                return VSConstants.S_OK;
            }

            public int OnAfterAttributeChangeEx(uint docCookie, uint grfAttribs, IVsHierarchy pHierOld, uint itemidOld, string pszMkDocumentOld, IVsHierarchy pHierNew, uint itemidNew, string pszMkDocumentNew)
            {
                _documentProvider.OnAfterAttributeChangeEx(docCookie, grfAttribs, pHierOld, itemidOld, pszMkDocumentOld, pHierNew, itemidNew, pszMkDocumentNew);

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
                _documentProvider.OnBeforeDocumentWindowShow(pFrame, docCookie, fFirstShow != 0);

                return VSConstants.S_OK;
            }

            public int OnBeforeLastDocumentUnlock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
            {
                // If we have no remaining locks, then we're done
                if (dwReadLocksRemaining + dwEditLocksRemaining == 0)
                {
                    _documentProvider.CloseDocuments(docCookie, monikerToKeep: null);
                }

                return VSConstants.S_OK;
            }

            public int OnBeforeSave(uint docCookie)
            {
                return VSConstants.S_OK;
            }
        }
    }
}
