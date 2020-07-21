// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.VisualStudio.DiagnosticsWindow.OptionsPages;
using Task = System.Threading.Tasks.Task;

namespace Roslyn.VisualStudio.DiagnosticsWindow
{
    // The option page configuration is duplicated in PackageRegistration.pkgdef.
    // These attributes specify the menu structure to be used in Tools | Options. These are not
    // localized because they are for internal use only.
    [ProvideOptionPage(typeof(InternalFeaturesOnOffPage), @"Roslyn\FeatureManager", @"Features", categoryResourceID: 0, pageNameResourceID: 0, supportsAutomation: true, SupportsProfiles = false)]
    [ProvideOptionPage(typeof(InternalComponentsOnOffPage), @"Roslyn\FeatureManager", @"Components", categoryResourceID: 0, pageNameResourceID: 0, supportsAutomation: true, SupportsProfiles = false)]
    [ProvideOptionPage(typeof(PerformanceFunctionIdPage), @"Roslyn\Performance", @"FunctionId", categoryResourceID: 0, pageNameResourceID: 0, supportsAutomation: true, SupportsProfiles = false)]
    [ProvideOptionPage(typeof(PerformanceLoggersPage), @"Roslyn\Performance", @"Loggers", categoryResourceID: 0, pageNameResourceID: 0, supportsAutomation: true, SupportsProfiles = false)]
    [ProvideOptionPage(typeof(InternalDiagnosticsPage), @"Roslyn\Diagnostics", @"Internal", categoryResourceID: 0, pageNameResourceID: 0, supportsAutomation: true, SupportsProfiles = false)]
    [ProvideOptionPage(typeof(InternalSolutionCrawlerPage), @"Roslyn\SolutionCrawler", @"Internal", categoryResourceID: 0, pageNameResourceID: 0, supportsAutomation: true, SupportsProfiles = false)]
    [ProvideOptionPage(typeof(ExperimentationPage), @"Roslyn\Experimentation", @"Internal", categoryResourceID: 0, pageNameResourceID: 0, supportsAutomation: true, SupportsProfiles = false)]
    [Guid(GuidList.guidVisualStudioDiagnosticsWindowPkgString)]
    [Description("Roslyn Diagnostics Window")]
    public sealed class VisualStudioDiagnosticsWindowPackage : AsyncPackage
    {
        private IThreadingContext _threadingContext;

        /// <summary>
        /// This function is called when the user clicks the menu item that shows the 
        /// tool window. See the Initialize method to see how the menu item is associated to 
        /// this function using the OleMenuCommandService service and the MenuCommand class.
        /// </summary>
        private void ShowToolWindow(object sender, EventArgs e)
        {
            _threadingContext.ThrowIfNotOnUIThread();

            JoinableTaskFactory.RunAsync(async () =>
            {
                await ShowToolWindowAsync(typeof(DiagnosticsWindow), id: 0, create: true, this.DisposalToken).ConfigureAwait(true);
            });
        }

        /////////////////////////////////////////////////////////////////////////////
        // Overridden Package Implementation
        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress).ConfigureAwait(true);

            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var componentModel = (IComponentModel)await GetServiceAsync(typeof(SComponentModel)).ConfigureAwait(true);
            var menuCommandService = (IMenuCommandService)await GetServiceAsync(typeof(IMenuCommandService)).ConfigureAwait(true);

            cancellationToken.ThrowIfCancellationRequested();

            Assumes.Present(componentModel);
            Assumes.Present(menuCommandService);

            _threadingContext = componentModel.GetService<IThreadingContext>();

            var workspace = componentModel.GetService<VisualStudioWorkspace>();
            _ = new ForceLowMemoryMode(workspace.Services.GetService<IOptionService>());

            // Add our command handlers for menu (commands must exist in the .vsct file)
            if (menuCommandService is OleMenuCommandService mcs)
            {
                // Create the command for the tool window
                var toolwndCommandID = new CommandID(GuidList.guidVisualStudioDiagnosticsWindowCmdSet, (int)PkgCmdIDList.CmdIDRoslynDiagnosticWindow);
                var menuToolWin = new MenuCommand(ShowToolWindow, toolwndCommandID);
                mcs.AddCommand(menuToolWin);
            }

            // set logger at start up
            var optionService = componentModel.GetService<IGlobalOptionService>();
            var remoteClientProvider = workspace.Services.GetService<IRemoteHostClientProvider>();

            PerformanceLoggersPage.SetLoggers(optionService, _threadingContext, remoteClientProvider);
        }
        #endregion

        public override IVsAsyncToolWindowFactory GetAsyncToolWindowFactory(Guid toolWindowType)
        {
            // Return this for everything, as all our windows are now async
            return this;
        }

        protected override string GetToolWindowTitle(Type toolWindowType, int id)
        {
            if (toolWindowType == typeof(DiagnosticsWindow))
            {
                return Resources.ToolWindowTitle;
            }

            return null;
        }

        protected override Task<object> InitializeToolWindowAsync(Type toolWindowType, int id, CancellationToken cancellationToken)
        {
            return Task.FromResult(new object());
        }
    }
}
