// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Shell.Interop;

namespace Roslyn.VisualStudio.ProjectSystem
{
    internal sealed class HierarchyEventsSink : IVsHierarchyEvents
    {
        private readonly DocumentId _documentId;
        private readonly IVsHierarchy _sharedHierarchy;
        private readonly VisualStudioWorkspaceImpl _workspace;

        public HierarchyEventsSink(VisualStudioWorkspaceImpl visualStudioWorkspace, IVsHierarchy sharedHierarchy, DocumentId documentId)
        {
            _workspace = visualStudioWorkspace;
            _sharedHierarchy = sharedHierarchy;
            _documentId = documentId;
        }

        public int OnPropertyChanged(uint itemid, int propid, uint flags)
        {
            if (propid == (int)__VSHPROPID7.VSHPROPID_SharedItemContextHierarchy ||
                propid == (int)__VSHPROPID8.VSHPROPID_ActiveIntellisenseProjectContext)
            {
                _workspace.UpdateDocumentContextIfContainsDocument(_sharedHierarchy, _documentId);
                return VSConstants.S_OK;
            }

            return VSConstants.DISP_E_MEMBERNOTFOUND;
        }

        public int OnInvalidateIcon(IntPtr hicon)
        {
            return VSConstants.E_NOTIMPL;
        }

        public int OnInvalidateItems([ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSITEMID")]uint itemidParent)
        {
            return VSConstants.E_NOTIMPL;
        }

        public int OnItemAdded([ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSITEMID")]uint itemidParent, [ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSITEMID")]uint itemidSiblingPrev, [ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSITEMID")]uint itemidAdded)
        {
            return VSConstants.E_NOTIMPL;
        }

        public int OnItemDeleted([ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSITEMID")]uint itemid)
        {
            return VSConstants.E_NOTIMPL;
        }

        public int OnItemsAppended([ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSITEMID")]uint itemidParent)
        {
            return VSConstants.E_NOTIMPL;
        }
    }
}
