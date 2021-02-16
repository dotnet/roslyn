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
        private readonly IDebugTokenReplacer _tokenReplacer;
        private readonly string _compilerRoot;

        [ImportingConstructor]
        [Obsolete("This exported object must be obtained through the MEF export provider.", error: true)]
        public DebugProfileProvider(ConfiguredProject configuredProject, IDebugTokenReplacer tokenReplacer, SVsServiceProvider? serviceProvider)
        {
            _configuredProject = configuredProject;
            _tokenReplacer = tokenReplacer;
            _compilerRoot = GetCompilerRoot(serviceProvider);
        }

        public Task OnAfterLaunchAsync(DebugLaunchOptions launchOptions, ILaunchProfile profile) => Task.CompletedTask;

        public Task OnBeforeLaunchAsync(DebugLaunchOptions launchOptions, ILaunchProfile profile) => Task.CompletedTask;

        public bool SupportsProfile(ILaunchProfile? profile) => Constants.CommandName.Equals(profile?.CommandName, StringComparison.OrdinalIgnoreCase);

        public async Task<IReadOnlyList<IDebugLaunchSettings>> QueryDebugTargetsAsync(DebugLaunchOptions launchOptions, ILaunchProfile? profile)
        {
            // set up the managed (net fx) debugger to start a process
            // https://github.com/dotnet/roslyn-sdk/issues/729
            var settings = new DebugLaunchSettings(launchOptions)
            {
                LaunchDebugEngineGuid = Microsoft.VisualStudio.ProjectSystem.Debug.DebuggerEngines.ManagedOnlyEngine,
                LaunchOperation = DebugLaunchOperation.CreateProcess
            };

            // try and get the target project
            var targetProjectUnconfigured = await TryGetTargetProjectAsync(profile).ConfigureAwait(false);
            if (targetProjectUnconfigured is object)
            {
                settings.CurrentDirectory = Path.GetDirectoryName(targetProjectUnconfigured.FullPath);
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

            // https://github.com/dotnet/roslyn-sdk/issues/728 : better error handling
            return new IDebugLaunchSettings[] { settings };
        }

        private static string GetCompilerRoot(SVsServiceProvider? serviceProvider)
        {
            // https://github.com/dotnet/roslyn-sdk/issues/729
            object rootDir = string.Empty;
            var shell = (IVsShell?)serviceProvider?.GetService(typeof(SVsShell));
            shell?.GetProperty((int)__VSSPROPID2.VSSPROPID_InstallRootDir, out rootDir);
            return Path.Combine((string)rootDir, "MSBuild", "Current", "Bin", "Roslyn");
        }

        private async Task<UnconfiguredProject?> TryGetTargetProjectAsync(ILaunchProfile? profile)
        {
            UnconfiguredProject? targetProject = null;
            object? value = null;
            profile?.OtherSettings?.TryGetValue(Constants.TargetProjectPropertyName, out value);

            if (value is string targetProjectPath)
            {
                // expand any variables in the path, and root it based on this project
                var replacedProjectPath = await _tokenReplacer.ReplaceTokensInStringAsync(targetProjectPath, true).ConfigureAwait(false);
                replacedProjectPath = _configuredProject.UnconfiguredProject.MakeRooted(replacedProjectPath);

                targetProject = _configuredProject.Services.ProjectService.LoadedUnconfiguredProjects.SingleOrDefault(p => p.FullPath == replacedProjectPath);
            }

            return targetProject;
        }
    }
}
