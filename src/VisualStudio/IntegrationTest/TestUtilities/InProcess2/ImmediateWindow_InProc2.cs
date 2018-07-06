// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess2
{
    public class ImmediateWindow_InProc2 : InProcComponent2
    {
        public ImmediateWindow_InProc2(TestServices testServices)
            : base(testServices)
        {
        }

        public async Task ShowImmediateWindowAsync(bool clearAll = false)
        {
            await ExecuteCommandAsync(WellKnownCommandNames.Debug_Immediate);
            if (clearAll)
            {
                await ClearAllAsync();
            }
        }

        public async Task<string> GetTextAsync()
        {
            var vsUIShell = await GetGlobalServiceAsync<SVsUIShell, IVsUIShell>();
            var immediateWindowGuid = VSConstants.StandardToolWindows.Immediate;
            ErrorHandler.ThrowOnFailure(vsUIShell.FindToolWindow((uint)__VSFINDTOOLWIN.FTW_fForceCreate, ref immediateWindowGuid, out var immediateWindowFrame));
            ErrorHandler.ThrowOnFailure(immediateWindowFrame.Show());
            ErrorHandler.ThrowOnFailure(immediateWindowFrame.GetProperty((int)__VSFPROPID.VSFPROPID_DocView, out var docView));
            var vsTextView = (IVsTextView)docView;
            ErrorHandler.ThrowOnFailure(vsTextView.GetBuffer(out var vsTextLines));
            ErrorHandler.ThrowOnFailure(vsTextLines.GetLineCount(out var lineCount));
            ErrorHandler.ThrowOnFailure(vsTextLines.GetLengthOfLine(lineCount - 1, out var lastLineLength));
            ErrorHandler.ThrowOnFailure(vsTextLines.GetLineText(0, 0, lineCount - 1, lastLineLength, out var text));
            return text;
        }

        public async Task ClearAllAsync()
        {
            await ShowImmediateWindowAsync(clearAll: false);
            await ExecuteCommandAsync(WellKnownCommandNames.Edit_ClearAll);
        }

        public async Task<bool> CloseWindowAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var uiShell = await GetGlobalServiceAsync<SVsUIShell, IVsUIShell>();
            if (ErrorHandler.Failed(uiShell.FindToolWindow((uint)__VSFINDTOOLWIN.FTW_fFrameOnly, new Guid(ToolWindowGuids80.ImmediateWindow), out var frame)))
            {
                return false;
            }

            ErrorHandler.ThrowOnFailure(frame.CloseFrame((uint)__FRAMECLOSE.FRAMECLOSE_NoSave));
            return true;
        }
    }
}
