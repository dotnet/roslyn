// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Roslyn.SyntaxVisualizer.Extension
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    ///
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the 
    /// IVsPackage interface and uses the registration attributes defined in the framework to 
    /// register itself and its components with the shell.
    /// </summary>
    [PackageRegistration(UseManagedResourcesOnly = true)] // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is a package.
    [ProvideMenuResource("Menus.ctmenu", 1)] // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideToolWindow(typeof(SyntaxVisualizerToolWindow))] // This attribute registers a tool window exposed by this package.
    [Guid(GuidList.GuidSyntaxVisualizerExtensionPkgString)]
    public sealed class SyntaxVisualizerExtensionPackage : Package
    {
        /// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require 
        /// any Visual Studio service because at this point the package object is created but 
        /// not sited yet inside Visual Studio environment. The place to do all the other 
        /// initialization is the Initialize method.
        /// </summary>
        public SyntaxVisualizerExtensionPackage()
        {
            Trace.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", ToString()));
        }

        /// <summary>
        /// This function is called when the user clicks the menu item that shows the 
        /// tool window. See the Initialize method to see how the menu item is associated to 
        /// this function using the OleMenuCommandService service and the MenuCommand class.
        /// </summary>
        private void ShowToolWindow(object sender, EventArgs e)
        {
            // Get the instance number 0 of this tool window. This window is single instance so this instance
            // is actually the only one. The last flag is set to true so that if the tool window does not exist
            // it will be created.
            var window = FindToolWindow(typeof(SyntaxVisualizerToolWindow), 0, true);
            if ((null == window) || (null == window.Frame))
            {
                throw new NotSupportedException(Resources.CanNotCreateWindow);
            }

#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread
            var windowFrame = (IVsWindowFrame)window.Frame;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
#pragma warning restore VSTHRD010 // Invoke single-threaded types on Main thread
        }

        // Overridden Package Implementation.
        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited,
        /// so this is the place where you can put all the initialization code that relies on
        /// services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            Trace.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", ToString()));
            base.Initialize();

            // Add our command handlers for menu (commands must exist in the .vsct file).
#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread
            if (GetService(typeof(IMenuCommandService)) is OleMenuCommandService mcs)
#pragma warning restore VSTHRD010 // Invoke single-threaded types on Main thread
            {
                // Create the command for the tool window.
                var toolwndCommandID = new CommandID(GuidList.GuidSyntaxVisualizerExtensionCmdSet, (int)PkgCmdIDList.CmdIDRoslynSyntaxVisualizer);
                var menuToolWin = new MenuCommand(ShowToolWindow, toolwndCommandID);
                mcs.AddCommand(menuToolWin);
            }
        }
        #endregion
    }
}
