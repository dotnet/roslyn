// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.ProjectSystem.VS.Debug;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace Roslyn.ComponentDebugger
{
    [Export(typeof(IDebugProfileLaunchTargetsProvider))]
    [AppliesTo(Constants.RoslynComponentCapability)]
    public class DebugProfileProvider : IDebugProfileLaunchTargetsProvider
    {
        private readonly ConfiguredProject _configuredProject;

        private readonly string _compilerRoot;

        [ImportingConstructor]
        [Obsolete("This exported object must be obtained through the MEF export provider.", error: true)]
        public DebugProfileProvider(ConfiguredProject configuredProject, SVsServiceProvider? serviceProvider)
        {
            _configuredProject = configuredProject;
            _compilerRoot = GetCompilerRoot(serviceProvider);
        }

        public Task OnAfterLaunchAsync(DebugLaunchOptions launchOptions, ILaunchProfile profile) => Task.CompletedTask;

        public Task OnBeforeLaunchAsync(DebugLaunchOptions launchOptions, ILaunchProfile profile) => Task.CompletedTask;

        public bool SupportsProfile(ILaunchProfile? profile) => Constants.CommandName.Equals(profile?.CommandName, StringComparison.OrdinalIgnoreCase);

        public async Task<IReadOnlyList<IDebugLaunchSettings>> QueryDebugTargetsAsync(DebugLaunchOptions launchOptions, ILaunchProfile? profile)
        {
            // set up the managed (net fx) debugger to start a process
            var settings = new DebugLaunchSettings(launchOptions)
            {
                LaunchDebugEngineGuid = Microsoft.VisualStudio.ProjectSystem.Debug.DebuggerEngines.ManagedOnlyEngine,
                LaunchOperation = DebugLaunchOperation.CreateProcess
            };

            // try and get the target project
            if (TryGetTargetProject(_configuredProject, profile, out var targetProjectUnconfigured))
            {
                settings.CurrentDirectory = Path.GetDirectoryName(targetProjectUnconfigured!.FullPath);
                var compiler = _configuredProject.Capabilities.Contains(ProjectCapabilities.VB) ? "vbc.exe" : "csc.exe";
                settings.Executable = Path.Combine(_compilerRoot, compiler);

                // try and get the configured version of the target project
                var targetProject = await targetProjectUnconfigured.GetSuggestedConfiguredProjectAsync().ConfigureAwait(false);
                if (targetProject is object)
                {
                    // get its compilation args
                    var args = await targetProject.GetCompilationArgumentsAsync().ConfigureAwait(false);

                    // append the command line args to the debugger launch
                    settings.Arguments = string.Join(" ", args);
                }
            }

            //PROTOTYPE: we probably shouldn't return anything when we couldn't figure it out
            return new IDebugLaunchSettings[] { settings };
        }

        private static string GetCompilerRoot(SVsServiceProvider? serviceProvider)
        {
            // PROTOTYPE: we should try and work out the compiler location from the project itself
            object rootDir = string.Empty;
            var shell = (IVsShell?)serviceProvider?.GetService(typeof(SVsShell));
            shell?.GetProperty((int)__VSSPROPID2.VSSPROPID_InstallRootDir, out rootDir);
            return Path.Combine((string)rootDir, "MSBuild", "Current", "Bin", "Roslyn");
        }

        private static bool TryGetTargetProject(ConfiguredProject project, ILaunchProfile? profile, out UnconfiguredProject? targetProject)
        {
            var targetProjectPath = profile?.OtherSettings?.ContainsKey(Constants.TargetProjectPropertyName) == true
                                    ? profile.OtherSettings[Constants.TargetProjectPropertyName].ToString()
                                    : string.Empty;

            // PROTOTYPE: we should eval / expand the path to work with env/msbuild variables etc.
            targetProject = project.Services.ProjectService.LoadedUnconfiguredProjects.SingleOrDefault(p => p.FullPath == targetProjectPath);
            return targetProject is object;
        }
    }
}
