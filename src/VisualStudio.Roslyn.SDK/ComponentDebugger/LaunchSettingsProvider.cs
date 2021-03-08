// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.Utilities;
using Task = System.Threading.Tasks.Task;

namespace Roslyn.ComponentDebugger
{
    [Export(typeof(ILaunchSettingsUIProvider))]
    [AppliesTo(Constants.RoslynComponentCapability)]
    public class LaunchSettingsProvider : ILaunchSettingsUIProvider
    {
        private readonly IProjectThreadingService _threadingService;
        private readonly UnconfiguredProject _unconfiguredProject;
        private readonly LaunchSettingsManager _launchSettingsManager;
        private readonly DebuggerOptionsViewModel _viewModel;

        private ImmutableArray<UnconfiguredProject> _projects;
        private IWritableLaunchProfile? _launchProfile;

        [ImportingConstructor]
        [Obsolete("This exported object must be obtained through the MEF export provider.", error: true)]
        public LaunchSettingsProvider(IProjectThreadingService threadingService, UnconfiguredProject unconfiguredProject, LaunchSettingsManager launchSettingsManager)
        {
            _threadingService = threadingService;
            _unconfiguredProject = unconfiguredProject;
            _launchSettingsManager = launchSettingsManager;
            _viewModel = new DebuggerOptionsViewModel(IndexChanged);
        }

        public string CommandName { get => Constants.CommandName; }

        // https://github.com/dotnet/roslyn-sdk/issues/730 : localization
        public string FriendlyName { get => "Roslyn Component"; }

        public UserControl? CustomUI { get => new DebuggerOptions() { DataContext = _viewModel }; }

        public void ProfileSelected(IWritableLaunchSettings curSettings)
        {
            _launchProfile = curSettings?.ActiveProfile;
            _threadingService.ExecuteSynchronously(UpdateViewModelAsync);
        }

        public bool ShouldEnableProperty(string propertyName)
        {
            // we disable all the default options for a debugger.
            // in the future we might want to enable env vars and (potentially) the exe to allow
            // customization of the compiler used?
            return false;
        }

        private async Task UpdateViewModelAsync()
        {
            var targetProjects = ArrayBuilder<UnconfiguredProject>.GetInstance();

            // get the output assembly for this project
            var projectArgs = await _unconfiguredProject.GetCompilationArgumentsAsync().ConfigureAwait(false);
            var targetArg = projectArgs.LastOrDefault(a => a.StartsWith("/out:", StringComparison.OrdinalIgnoreCase));
            var target = Path.GetFileName(targetArg);

            var projectService = _unconfiguredProject.Services.ProjectService;
            foreach (var targetProjectUnconfigured in projectService.LoadedUnconfiguredProjects)
            {
                // check if the args contain the project as an analyzer ref
                foreach (var arg in await targetProjectUnconfigured.GetCompilationArgumentsAsync().ConfigureAwait(false))
                {
                    if (arg.StartsWith("/analyzer:", StringComparison.OrdinalIgnoreCase)
                        && arg.EndsWith(target, StringComparison.OrdinalIgnoreCase))
                    {
                        targetProjects.Add(targetProjectUnconfigured);
                    }
                }
            }
            _projects = targetProjects.ToImmutableAndFree();

            var launchTargetProject = await _launchSettingsManager.TryGetProjectForLaunchAsync(_launchProfile?.ToLaunchProfile()).ConfigureAwait(true);
            var index = _projects.IndexOf(launchTargetProject!);

            _viewModel.ProjectNames = _projects.Select(p => Path.GetFileNameWithoutExtension(p.FullPath));
            _viewModel.SelectedProjectIndex = index;
        }

        private void IndexChanged(int newIndex)
        {
            if (_launchProfile is object && !_projects.IsDefaultOrEmpty && newIndex >= 0 && newIndex < _projects.Length)
            {
                var project = _projects[newIndex];
                _launchSettingsManager.WriteProjectForLaunch(_launchProfile, project);
            }
        }
    }
}
