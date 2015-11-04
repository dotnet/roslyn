// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Utilities
{
    internal static class IVsTextViewExtensions
    {
        internal static bool IsImmediateWindow(this IVsTextView vsTextView, IVsUIShell shellService, IVsEditorAdaptersFactoryService _editorAdaptersFactoryService)
        {
            return ContainsImmediateWindow(SpecializedCollections.SingletonEnumerable(vsTextView), shellService, _editorAdaptersFactoryService);
        }

        internal static bool ContainsImmediateWindow(this IEnumerable<IVsTextView> vsTextViews, IVsUIShell shellService, IVsEditorAdaptersFactoryService _editorAdaptersFactoryService)
        {
            IEnumWindowFrames windowEnum = null;
            Marshal.ThrowExceptionForHR(shellService.GetToolWindowEnum(out windowEnum));

            IVsWindowFrame[] frame = new IVsWindowFrame[1];
            uint value;

            var immediateWindowGuid = Guid.Parse(ToolWindowGuids80.ImmediateWindow);

            while (windowEnum.Next(1, frame, out value) == VSConstants.S_OK)
            {
                Guid toolWindowGuid;
                Marshal.ThrowExceptionForHR(frame[0].GetGuidProperty((int)__VSFPROPID.VSFPROPID_GuidPersistenceSlot, out toolWindowGuid));
                if (toolWindowGuid == immediateWindowGuid)
                {
                    IntPtr frameTextView;
                    Marshal.ThrowExceptionForHR(frame[0].QueryViewInterface(typeof(IVsTextView).GUID, out frameTextView));
                    try
                    {
                        var immediateWindowTextView = Marshal.GetObjectForIUnknown(frameTextView) as IVsTextView;
                        var immediateWindowWpfTextView = _editorAdaptersFactoryService.GetWpfTextView(immediateWindowTextView);
                        return vsTextViews.Any(vsTextView => _editorAdaptersFactoryService.GetWpfTextView(vsTextView) == immediateWindowWpfTextView);
                    }
                    finally
                    {
                        Marshal.Release(frameTextView);
                    }
                }
            }

            return false;
        }
    }
}
