﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Legacy
{
    internal partial class AbstractLegacyProject : IVsHierarchyEvents
    {
        private uint _hierarchyEventsCookie;

        private void ConnectHierarchyEvents()
        {
            Debug.Assert(!this.AreHierarchyEventsConnected, "IVsHierarchyEvents are already connected!");

            if (ErrorHandler.Failed(Hierarchy.AdviseHierarchyEvents(this, out _hierarchyEventsCookie)))
            {
                Debug.Fail("Failed to connect IVsHierarchyEvents");
                _hierarchyEventsCookie = 0;
            }
        }

        private void DisconnectHierarchyEvents()
        {
            if (this.AreHierarchyEventsConnected)
            {
                Hierarchy.UnadviseHierarchyEvents(_hierarchyEventsCookie);
                _hierarchyEventsCookie = 0;
            }
        }

        private bool AreHierarchyEventsConnected
        {
            get { return _hierarchyEventsCookie != 0; }
        }

        int IVsHierarchyEvents.OnInvalidateIcon(IntPtr hicon)
        {
            return VSConstants.E_NOTIMPL;
        }

        int IVsHierarchyEvents.OnInvalidateItems(uint itemidParent)
        {
            return VSConstants.E_NOTIMPL;
        }

        int IVsHierarchyEvents.OnItemAdded(uint itemidParent, uint itemidSiblingPrev, uint itemidAdded)
        {
            return VSConstants.E_NOTIMPL;
        }

        int IVsHierarchyEvents.OnItemDeleted(uint itemid)
        {
            return VSConstants.E_NOTIMPL;
        }

        int IVsHierarchyEvents.OnItemsAppended(uint itemidParent)
        {
            return VSConstants.E_NOTIMPL;
        }

        int IVsHierarchyEvents.OnPropertyChanged(uint itemid, int propid, uint flags)
        {
            if ((propid == (int)__VSHPROPID.VSHPROPID_Caption ||
                 propid == (int)__VSHPROPID.VSHPROPID_Name) &&
                itemid == (uint)VSConstants.VSITEMID.Root)
            {
                string newDisplayName = GetProjectDisplayName(Hierarchy);
                string newPath = GetProjectFilePath(Hierarchy);

                UpdateProjectDisplayNameAndFilePath(newDisplayName, newPath);
            }

            if ((propid == (int)__VSHPROPID.VSHPROPID_ProjectIDGuid) &&
                 itemid == (uint)VSConstants.VSITEMID.Root)
            {
                // this should happen while project loading if it ever happens
                Guid = GetProjectIDGuid(Hierarchy);
            }

            return VSConstants.S_OK;
        }
    }
}
