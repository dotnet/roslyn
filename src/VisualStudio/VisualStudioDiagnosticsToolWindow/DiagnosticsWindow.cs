// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using Roslyn.Hosting.Diagnostics.PerfMargin;
using Roslyn.Utilities;

namespace Roslyn.VisualStudio.DiagnosticsWindow
{
    [Guid("b2da68d7-fd1c-491a-a9a0-24f597b9f56c")]
    public sealed class DiagnosticsWindow : ToolWindowPane
    {
        public VisualStudioWorkspace? Workspace { get; private set; }

        /// <summary>
        /// Standard constructor for the tool window.
        /// </summary>
        public DiagnosticsWindow(object _)
            : base(null)
        {
            // Set the window title reading it from the resources.
            Caption = Resources.ToolWindowTitle;
            // Set the image that will appear on the tab of the window frame
            // when docked with an other window
            // The resource ID correspond to the one defined in the resx file
            // while the Index is the offset in the bitmap strip. Each image in
            // the strip being 16x16.
            BitmapResourceID = 301;
            BitmapIndex = 1;

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on 
            // the object returned by the Content property.
            var perfMarginPanel = new TabItem()
            {
                Header = "Perf",
                Content = new PerfMarginPanel()
            };

            var telemetryPanel = new TabItem()
            {
                Header = "Telemetry",
                Content = new TelemetryPanel()
            };

            var workspacePanel = new TabItem()
            {
                Header = "Workspace",
                Content = new WorkspacePanel(this)
            };

            var tabControl = new TabControl
            {
                TabStripPlacement = Dock.Bottom
            };

            tabControl.Items.Add(perfMarginPanel);
            tabControl.Items.Add(telemetryPanel);
            tabControl.Items.Add(workspacePanel);

            Content = tabControl;
        }

        public void Initialize(VisualStudioWorkspace workspace)
        {
            Contract.ThrowIfFalse(Workspace == null);
            Workspace = workspace;
        }
    }
}
