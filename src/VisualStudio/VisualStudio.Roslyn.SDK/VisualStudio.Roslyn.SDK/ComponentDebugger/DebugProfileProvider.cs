// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.ProjectSystem.VS.Debug;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Task = System.Threading.Tasks.Task;

namespace Roslyn.ComponentDebugger
{
    [Export(typeof(IDebugProfileLaunchTargetsProvider))]
    [AppliesTo(Constants.RoslynComponentCapability)]
    public sealed class DebugProfileProvider : IDebugProfileLaunchTargetsProvider, IDisposable
    {
        private readonly ConfiguredProject _configuredProject;
        private readonly LaunchSettingsManager _launchSettingsManager;
        private readonly IProjectThreadingService _threadingService;
        private readonly AsyncLazy<string?> _compilerRoot;

        [ImportingConstructor]
        [Obsolete("This exported object must be obtained through the MEF export provider.", error: true)]
        public DebugProfileProvider(ConfiguredProject configuredProject, LaunchSettingsManager launchSettingsManager, SVsServiceProvider? serviceProvider, IProjectThreadingService threadingService)
        {
            _configuredProject = configuredProject;
            _launchSettingsManager = launchSettingsManager;
            _threadingService = threadingService;

            _compilerRoot = new AsyncLazy<string?>(() => GetCompilerRootAsync(serviceProvider), _threadingService.JoinableTaskFactory);
        }

        public Task OnAfterLaunchAsync(DebugLaunchOptions launchOptions, ILaunchProfile profile) => Task.CompletedTask;

        public Task OnBeforeLaunchAsync(DebugLaunchOptions launchOptions, ILaunchProfile profile) => Task.CompletedTask;

        public bool SupportsProfile(ILaunchProfile? profile) => Constants.CommandName.Equals(profile?.CommandName, StringComparison.Ordinal);

        public async Task<IReadOnlyList<IDebugLaunchSettings>> QueryDebugTargetsAsync(DebugLaunchOptions launchOptions, ILaunchProfile? profile)
        {
            // set up the managed (net fx) debugger to start a process
            // https://github.com/dotnet/roslyn-sdk/issues/729
            var settings = new DebugLaunchSettings(launchOptions)
            {
                LaunchDebugEngineGuid = Microsoft.VisualStudio.ProjectSystem.Debug.DebuggerEngines.ManagedOnlyEngine,
                LaunchOperation = DebugLaunchOperation.CreateProcess
            };

            var compilerRoot = await _compilerRoot.GetValueAsync().ConfigureAwait(true);
            if (compilerRoot is object)
            {
                // try and get the target project
                var targetProjectUnconfigured = await _launchSettingsManager.TryGetProjectForLaunchAsync(profile).ConfigureAwait(true);
                if (targetProjectUnconfigured is object)
                {
                    settings.CurrentDirectory = Path.GetDirectoryName(targetProjectUnconfigured.FullPath);
                    var compiler = _configuredProject.Capabilities.Contains(ProjectCapabilities.VB) ? "vbc.exe" : "csc.exe";
                    settings.Executable = Path.Combine(compilerRoot, compiler);

                    // get its compilation args
                    var args = await targetProjectUnconfigured.GetCompilationArgumentsAsync().ConfigureAwait(true);
                    args = args.Remove("/noconfig");

                    // write the command line args out to a response file
                    var file = GetResponseFileName();
                    File.WriteAllText(file, string.Join(" ", args));

                    // pass the response file as the argument to the launch command
                    settings.Arguments = $"@\"{file}\"";
                }
            }
            // https://github.com/dotnet/roslyn-sdk/issues/728 : better error handling
            return new IDebugLaunchSettings[] { settings };
        }

        private static string GetResponseFileName()
        {
            // prefix with the VS PID so we don't stomp on another instance
            var pid = Process.GetCurrentProcess().Id;
            return Path.Combine(Path.GetTempPath(), $"{Constants.CommandName}_{pid}.txt");
        }

        private async Task<string?> GetCompilerRootAsync(SVsServiceProvider? serviceProvider)
        {
            await _threadingService.SwitchToUIThread();

            // https://github.com/dotnet/roslyn-sdk/issues/729 : don't hardcode net fx compiler
            var shell = (IVsShell?)serviceProvider?.GetService(typeof(SVsShell));
            if (shell is object
                && shell.GetProperty((int)__VSSPROPID2.VSSPROPID_InstallRootDir, out var rootDirObj) == VSConstants.S_OK
                && rootDirObj is string rootDir)
            {
                return Path.Combine(rootDir, "MSBuild", "Current", "Bin", "Roslyn");
            }

            return null;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA1031:Do not catch general exception types", Justification = "Best effort only.")]
        public void Dispose()
        {
            try
            {
                var responseFile = GetResponseFileName();
                if (File.Exists(responseFile))
                {
                    File.Delete(responseFile);
                }
            }
            catch
            {
                // best effort, do nothing if we can't remove it for whatever reason
            }
        }
    }
}
