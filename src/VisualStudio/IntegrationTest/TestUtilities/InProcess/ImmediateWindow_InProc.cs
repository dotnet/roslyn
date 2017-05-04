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
using Microsoft.VisualStudio.Text;
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

        public static ImmediateWindow_InProc Create()
            => new ImmediateWindow_InProc();


        public void ExecuteCommand(string command)
        {
            //var immediateWindow = (((DTE2)GetDTE()).ToolWindows.GetToolWindow("Immediate Window"));//.CommandWindow;

            //((CommandWindow)immediateWindow).SendInput(immediateWindow.GetType().ToString(), Execute: false);
            var toolWindow = GetToolWindow(new Guid(ToolWindowGuids80.ImmediateWindow));
            toolWindow.Show();
            //toolWindow..SetProperty()
            //commandWindow.SendInput(command, Execute: true);
            
        }

        private void GetView(IVsWindowFrame frame)
        {
            object docView = null;
            frame.GetProperty((int)__VSFPROPID.VSFPROPID_DocView, out docView);
            
            if (docView is IVswin)
            {
                IVsTextView textView;
                ((IVsCodeWindow)docView).GetPrimaryView(out textView);
                

                var model = (IComponentModel)Package.GetGlobalService(typeof(SComponentModel));
                var adapterFactory = model.GetService<IVsEditorAdaptersFactoryService>();
                var wpfTextView = adapterFactory.GetWpfTextView(textView);
                return wpfTextView;
            }
        }

        private IVsWindowFrame GetToolWindow(Guid identifier)
        {
            var _shellService = GetGlobalService<SVsUIShell, IVsUIShell>();
            IVsWindowFrame frame;
            _shellService.FindToolWindow((uint)(__VSFINDTOOLWIN.FTW_fForceCreate | __VSFINDTOOLWIN.FTW_fFindFirst), ref identifier, out frame);

            return frame;
        }
    }
}
