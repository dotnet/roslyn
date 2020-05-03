// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess
{
    internal class ImmediateWindow_InProc : InProcComponent
    {
        public static ImmediateWindow_InProc Create() => new ImmediateWindow_InProc();

        public void ShowImmediateWindow() => ExecuteCommand("Debug.Immediate");

        public string GetText()
        {
            IVsUIShell vsUIShell = (IVsUIShell)ServiceProvider.GlobalProvider.GetService(typeof(SVsUIShell));
            Guid immediateWindowGuid = VSConstants.StandardToolWindows.Immediate;
            IVsWindowFrame immediateWindowFrame;
            ErrorHandler.ThrowOnFailure(vsUIShell.FindToolWindow((uint)__VSFINDTOOLWIN.FTW_fForceCreate, ref immediateWindowGuid, out immediateWindowFrame));
            ErrorHandler.ThrowOnFailure(immediateWindowFrame.Show());
            ErrorHandler.ThrowOnFailure(immediateWindowFrame.GetProperty((int)__VSFPROPID.VSFPROPID_DocView, out object docView));
            var vsTextView = (IVsTextView)docView;
            ErrorHandler.ThrowOnFailure(vsTextView.GetBuffer(out IVsTextLines vsTextLines));
            ErrorHandler.ThrowOnFailure(vsTextLines.GetLineCount(out int lineCount));
            ErrorHandler.ThrowOnFailure(vsTextLines.GetLengthOfLine(lineCount - 1, out int lastLineLength));
            ErrorHandler.ThrowOnFailure(vsTextLines.GetLineText(0, 0, lineCount - 1, lastLineLength, out string text));
            return text;
        }

        public void ClearAll()
        {
            ShowImmediateWindow();
            ExecuteCommand("Edit.ClearAll");
        }
    }
}
