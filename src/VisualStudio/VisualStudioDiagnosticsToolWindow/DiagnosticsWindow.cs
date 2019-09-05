// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using Roslyn.Hosting.Diagnostics.PerfMargin;
using Roslyn.Hosting.Diagnostics.RemoteHost;
using Roslyn.VisualStudio.DiagnosticsWindow.Telemetry;

namespace Roslyn.VisualStudio.DiagnosticsWindow
{
    [Guid("b2da68d7-fd1c-491a-a9a0-24f597b9f56c")]
    public class DiagnosticsWindow : ToolWindowPane
    {
        /// <summary>
        /// Standard constructor for the tool window.
        /// </summary>
        public DiagnosticsWindow(object context)
            : base(null)
        {
            // Set the window title reading it from the resources.
            this.Caption = Resources.ToolWindowTitle;
            // Set the image that will appear on the tab of the window frame
            // when docked with an other window
            // The resource ID correspond to the one defined in the resx file
            // while the Index is the offset in the bitmap strip. Each image in
            // the strip being 16x16.
            this.BitmapResourceID = 301;
            this.BitmapIndex = 1;

#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread
            var componentModel = (IComponentModel)ServiceProvider.GlobalProvider.GetService(typeof(SComponentModel));
#pragma warning restore VSTHRD010 // Invoke single-threaded types on Main thread

            var workspace = componentModel.GetService<VisualStudioWorkspace>();

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on 
            // the object returned by the Content property.
            var perfMarginPanel = new TabItem()
            {
                Header = "Perf",
                Content = new PerfMarginPanel()
            };

            var remoteHostPanel = new TabItem()
            {
                Header = "Remote",
                Content = new RemoteHostPanel(workspace)
            };

            var telemetryPanel = new TabItem()
            {
                Header = "Telemetry",
                Content = new TelemetryPanel()
            };

            var tabControl = new TabControl
            {
                TabStripPlacement = Dock.Bottom
            };

            tabControl.Items.Add(perfMarginPanel);
            tabControl.Items.Add(remoteHostPanel);
            tabControl.Items.Add(telemetryPanel);

            base.Content = tabControl;
        }
    }
}
