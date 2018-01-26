// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess
{
    internal class ImmediateWindow_InProc : InProcComponent
    {
        public static ImmediateWindow_InProc Create() => new ImmediateWindow_InProc();

        public void ShowImmediateWindow()=> ExecuteCommand("Debug.Immediate");

        public string GetText()
        {
            IVsUIShell vsUIShell = (IVsUIShell)ServiceProvider.GlobalProvider.GetService(typeof(SVsUIShell));
            Guid guid = new Guid("ECB7191A-597B-41F5-9843-03A4CF275DDE");
            IVsWindowFrame windowFrame;
            int result = vsUIShell.FindToolWindow((uint)__VSFINDTOOLWIN.FTW_fFindFirst, ref guid, out windowFrame);
            windowFrame.Show();
            windowFrame.GetProperty((int)__VSFPROPID.VSFPROPID_DocView, out object docView);
            var textView = docView as IVsTextView;
            textView.GetBuffer(out IVsTextLines vsTextLines);
            vsTextLines.GetLineCount(out int lineCount);
            vsTextLines.GetLengthOfLine(lineCount - 1, out int lastLineLength);
            vsTextLines.GetLineText(0, 0, lineCount - 1, lastLineLength, out string text);
            return text;
        }

        public void ClearAll()
        {
            ShowImmediateWindow();
            ExecuteCommand("Edit.ClearAll");
        }
    }
}
