using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace Roslyn.SyntaxVisualizer.Extension
{
    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    ///
    /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane, 
    /// usually implemented by the package implementer.
    ///
    /// This class derives from the ToolWindowPane class provided from the MPF in order to use its 
    /// implementation of the IVsUIElementPane interface.
    /// </summary>
    [Guid("da7e21aa-da94-452d-8aa1-d1b23f73f576")]
    public class SyntaxVisualizerToolWindow : ToolWindowPane
    {
        /// <summary>
        /// Standard constructor for the tool window.
        /// </summary>
        public SyntaxVisualizerToolWindow() :
            base(null)
        {
            // Set the window title reading it from the resources.
            this.Caption = Resources.ToolWindowTitle;

            // Set the image that will appear on the tab of the window frame
            // when docked with an other window
            // The resource ID correspond to the one defined in the resx file
            // while the Index is the offset in the bitmap strip. Each image in
            // the strip being 16x16.
            this.BitmapResourceID = 500;
            this.BitmapIndex = 0;

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on 
            // the object returned by the Content property.
            this.Content = new SyntaxVisualizerContainer(this);
        }

        internal TServiceInterface GetVsService<TServiceInterface, TService>() 
            where TServiceInterface : class
            where TService : class
        {
            return (TServiceInterface)GetService(typeof(TService));
        }
    }
}