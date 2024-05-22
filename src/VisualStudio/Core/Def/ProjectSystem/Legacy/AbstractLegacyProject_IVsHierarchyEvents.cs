// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Diagnostics;
using System.IO;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Legacy;

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
        => VSConstants.E_NOTIMPL;

    int IVsHierarchyEvents.OnInvalidateItems(uint itemidParent)
        => VSConstants.E_NOTIMPL;

    int IVsHierarchyEvents.OnItemAdded(uint itemidParent, uint itemidSiblingPrev, uint itemidAdded)
        => VSConstants.E_NOTIMPL;

    int IVsHierarchyEvents.OnItemDeleted(uint itemid)
        => VSConstants.E_NOTIMPL;

    int IVsHierarchyEvents.OnItemsAppended(uint itemidParent)
        => VSConstants.E_NOTIMPL;

    int IVsHierarchyEvents.OnPropertyChanged(uint itemid, int propid, uint flags)
    {
        if ((propid == (int)__VSHPROPID.VSHPROPID_Caption ||
             propid == (int)__VSHPROPID.VSHPROPID_Name) &&
            itemid == (uint)VSConstants.VSITEMID.Root)
        {
            var filePath = Hierarchy.TryGetProjectFilePath();

            if (filePath != null && File.Exists(filePath))
            {
                ProjectSystemProject.FilePath = filePath;
            }

            if (Hierarchy.TryGetName(out var name))
            {
                ProjectSystemProject.DisplayName = name;
            }
        }

        return VSConstants.S_OK;
    }
}
