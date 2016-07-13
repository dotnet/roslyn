// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;
using Roslyn.VisualStudio.DiagnosticsWindow.OptionsPages;

namespace Roslyn.VisualStudio.DiagnosticsWindow
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
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", version: 1)]
    // These attributes specify the menu structure to be used in Tools | Options. These are not
    // localized because they are for internal use only.
    [ProvideOptionPage(typeof(InternalFeaturesOnOffPage), @"Roslyn\FeatureManager", @"Features", categoryResourceID: 0, pageNameResourceID: 0, supportsAutomation: true, SupportsProfiles = false)]
    [ProvideOptionPage(typeof(InternalComponentsOnOffPage), @"Roslyn\FeatureManager", @"Components", categoryResourceID: 0, pageNameResourceID: 0, supportsAutomation: true, SupportsProfiles = false)]
    [ProvideOptionPage(typeof(PerformanceFunctionIdPage), @"Roslyn\Performance", @"FunctionId", categoryResourceID: 0, pageNameResourceID: 0, supportsAutomation: true, SupportsProfiles = false)]
    [ProvideOptionPage(typeof(PerformanceLoggersPage), @"Roslyn\Performance", @"Loggers", categoryResourceID: 0, pageNameResourceID: 0, supportsAutomation: true, SupportsProfiles = false)]
    [ProvideOptionPage(typeof(InternalDiagnosticsPage), @"Roslyn\Diagnostics", @"Internal", categoryResourceID: 0, pageNameResourceID: 0, supportsAutomation: true, SupportsProfiles = false)]
    [ProvideOptionPage(typeof(InternalSolutionCrawlerPage), @"Roslyn\SolutionCrawler", @"Internal", categoryResourceID: 0, pageNameResourceID: 0, supportsAutomation: true, SupportsProfiles = false)]
    // This attribute registers a tool window exposed by this package.
    [ProvideToolWindow(typeof(DiagnosticsWindow))]
    [Guid(GuidList.guidVisualStudioDiagnosticsWindowPkgString)]
    [Description("Roslyn Diagnostics Window")]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
    public sealed class VisualStudioDiagnosticsWindowPackage : Package
    {
        private ForceLowMemoryMode _forceLowMemoryMode;

        /// <summary>
        /// This function is called when the user clicks the menu item that shows the 
        /// tool window. See the Initialize method to see how the menu item is associated to 
        /// this function using the OleMenuCommandService service and the MenuCommand class.
        /// </summary>
        private void ShowToolWindow(object sender, EventArgs e)
        {
            // Get the instance number 0 of this tool window. This window is single instance so this instance
            // is actually the only one.
            // The last flag is set to true so that if the tool window does not exists it will be created.
            ToolWindowPane window = this.FindToolWindow(typeof(DiagnosticsWindow), 0, true);
            if ((null == window) || (null == window.Frame))
            {
                throw new NotSupportedException(Resources.CanNotCreateWindow);
            }
            IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }

        /////////////////////////////////////////////////////////////////////////////
        // Overridden Package Implementation
        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            var componentModel = (IComponentModel)GetService(typeof(SComponentModel));

            var workspace = componentModel.GetService<VisualStudioWorkspace>();
            _forceLowMemoryMode = new ForceLowMemoryMode(workspace.Services.GetService<IOptionService>());

            // Add our command handlers for menu (commands must exist in the .vsct file)
            OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (null != mcs)
            {
                // Create the command for the tool window
                CommandID toolwndCommandID = new CommandID(GuidList.guidVisualStudioDiagnosticsWindowCmdSet, (int)PkgCmdIDList.CmdIDRoslynDiagnosticWindow);
                MenuCommand menuToolWin = new MenuCommand(ShowToolWindow, toolwndCommandID);
                mcs.AddCommand(menuToolWin);
            }
        }
        #endregion
    }
}
