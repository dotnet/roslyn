// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Common;
using Microsoft.VisualStudio.LanguageServices.CSharp.Interactive;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess
{
    internal class ImmediateWindow_InProc : InProcComponent
    {
        private static readonly Guid immediateWindowGuid = Guid.Parse(ToolWindowGuids80.ImmediateWindow);
        // private readonly OutputWindowPane _outputWindowPane;

        private ImmediateWindow_InProc()
        {
            //var win = ((DTE2)GetDTE()).Windows.Item(EnvDTE.Constants.vsext_wk_ImmedWindow);
            //OutputWindow OW = (OutputWindow)win.Object;
            //_outputWindowPane = OW.OutputWindowPanes.Add("A New Pane");

        }

        private static IWpfTextViewHost GetActiveTextViewHost()
        {
            // The active text view might not have finished composing yet, waiting for the application to 'idle'
            // means that it is done pumping messages (including WM_PAINT) and the window should return the correct text view
            WaitForApplicationIdle();

            var activeVsTextView = (IVsUserData)GetActiveVsTextView();

            var hresult = activeVsTextView.GetData(immediateWindowGuid, out var wpfTextViewHost);
            Marshal.ThrowExceptionForHR(hresult);

            return (IWpfTextViewHost)wpfTextViewHost;
        }

        private static IVsTextView GetActiveVsTextView()
        {
            var vsTextManager = GetGlobalService<SVsTextManager, IVsTextManager>();

            var hresult = vsTextManager.GetActiveView(fMustHaveFocus: 1, pBuffer: null, ppView: out var vsTextView);
            Marshal.ThrowExceptionForHR(hresult);

            return vsTextView;
        }

        public static ImmediateWindow_InProc Create()
            => new ImmediateWindow_InProc();

        public void ExecuteCommand(string command)
        { 
            Window win = ((DTE2)GetDTE()).Windows.Item(ToolWindowGuids80.ImmediateWindow);
            win.Activate();
        //    win...Document.

       //     win.Object
     //       OutputWindow ow;

        // .ToolWindows.GetToolWindow("Immediate Window");.Windows..CreateLinkedWindowFrame.CreateToolWindow.CreateToolWindow(.Item(EnvDTE.Constants.vsext_wk_ImmedWindow);

   //     var outputWindowPane = OW.OutputWindowPanes.Add("A New Pane");
   //     outputWindowPane.OutputString(command);
        }
    }
}
