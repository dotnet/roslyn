// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;

namespace Roslyn.ComponentDebugger
{
    [Export(typeof(ILaunchSettingsUIProvider))]
    [AppliesTo(Constants.RoslynComponentCapability)]
    public class LaunchSettingsProvider : ILaunchSettingsUIProvider
    {
        private readonly UnconfiguredProject _unconfiguredProject;
        private readonly AsyncLazy<DebuggerOptionsViewModel> _viewModel;

        [ImportingConstructor]
        [Obsolete("This exported object must be obtained through the MEF export provider.", error: true)]
        public LaunchSettingsProvider(UnconfiguredProject unconfiguredProject)
        {
            _unconfiguredProject = unconfiguredProject;
            _viewModel = new AsyncLazy<DebuggerOptionsViewModel>(GetViewModelAsync, ThreadHelper.JoinableTaskFactory);
        }

        public string CommandName { get => Constants.CommandName; }

        // https://github.com/dotnet/roslyn-sdk/issues/730 : localization
        public string FriendlyName { get => "Roslyn Component"; }

        public UserControl? CustomUI { get => new DebuggerOptions() { DataContext = _viewModel.GetValue() }; }

        public void ProfileSelected(IWritableLaunchSettings curSettings)
        {
            // Update the viewmodel's current profile.
            this._viewModel.GetValue().LaunchProfile = curSettings?.ActiveProfile;
        }

        public bool ShouldEnableProperty(string propertyName)
        {
            // we disable all the default options for a debugger.
            // in the future we might want to enable env vars and (potentially) the exe to allow
            // customization of the compiler used?
            return false;
        }

        private async Task<DebuggerOptionsViewModel> GetViewModelAsync()
        {
            var targetProjects = ArrayBuilder<ConfiguredProject>.GetInstance();

            // NOTE: we assume the target projects are in the same configuration as this one (can they be different?)
            var configuredProject = await _unconfiguredProject.GetSuggestedConfiguredProjectAsync().ConfigureAwait(false);
            if (configuredProject is object)
            {
                // get the output assembly for this project
                var projectArgs = await configuredProject.GetCompilationArgumentsAsync().ConfigureAwait(false);
                var targetArg = projectArgs.LastOrDefault(a => a.StartsWith("/out:", StringComparison.OrdinalIgnoreCase));
                var target = Path.GetFileName(targetArg);

                var projectService = configuredProject.Services.ProjectService;
                foreach (var targetProjectUnconfigured in projectService.LoadedUnconfiguredProjects)
                {
                    var targetProject = await targetProjectUnconfigured.LoadConfiguredProjectAsync(configuredProject.ProjectConfiguration).ConfigureAwait(false);
                    if (targetProject is object)
                    {
                        // check if the args contain the project as an analyzer ref
                        foreach (var arg in await targetProject.GetCompilationArgumentsAsync().ConfigureAwait(false))
                        {
                            if (arg.StartsWith("/analyzer", StringComparison.OrdinalIgnoreCase)
                                && arg.EndsWith(target, StringComparison.OrdinalIgnoreCase))
                            {
                                targetProjects.Add(targetProject);
                            }
                        }
                    }
                }
            }

            return new DebuggerOptionsViewModel(targetProjects.ToImmutableAndFree());
        }
    }
}
