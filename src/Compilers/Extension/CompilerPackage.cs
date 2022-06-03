// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace Roslyn.Compilers.Extension
{
    [Guid("31C0675E-87A4-4061-A0DD-A4E510FCCF97")]
    public sealed class CompilerPackage : AsyncPackage
    {
        public static string RoslynHive = null;

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress).ConfigureAwait(true);

            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var reg = (ILocalRegistry2)await GetServiceAsync(typeof(SLocalRegistry)).ConfigureAwait(true);
            cancellationToken.ThrowIfCancellationRequested();
            Assumes.Present(reg);

            var packagePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            string localRegistryRoot;
            reg.GetLocalRegistryRoot(out localRegistryRoot);
            var registryParts = localRegistryRoot.Split('\\');

            // Is it a valid Hive looks similar to:  
            //  'Software\Microsoft\VisualStudio\14.0'  'Software\Microsoft\VisualStudio\14.0Roslyn'  'Software\Microsoft\VSWinExpress\14.0'
            if (registryParts.Length >= 4)
            {
                var skuName = registryParts[2];
                var hiveName = registryParts[3];
                RoslynHive = string.Format(@"{0}.{1}", registryParts[2], registryParts[3]);

                await WriteMSBuildFilesAsync(packagePath, RoslynHive, cancellationToken).ConfigureAwait(true);

                try
                {
                    Microsoft.Build.Evaluation.ProjectCollection.GlobalProjectCollection.DisableMarkDirty = true;
                    Microsoft.Build.Evaluation.ProjectCollection.GlobalProjectCollection.SetGlobalProperty("RoslynHive", RoslynHive);
                }
                finally
                {
                    Microsoft.Build.Evaluation.ProjectCollection.GlobalProjectCollection.DisableMarkDirty = false;
                }
            }
        }

        private async Task WriteMSBuildFilesAsync(string packagePath, string hiveName, CancellationToken cancellationToken)
        {
            // A map of the file name to the content we need to ensure exists in the file
            var filesToWrite = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // The props we want to be included as early as possible since we want our tasks to be used and
            // to ensure our setting of targets path happens early enough
            filesToWrite.Add(await GetMSBuildRelativePathAsync($@"Imports\Microsoft.Common.props\ImportBefore\Roslyn.Compilers.Extension.{hiveName}.props", cancellationToken).ConfigureAwait(true),
                $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup Condition=""'$(RoslynHive)' == '{hiveName}'"">
    <CSharpCoreTargetsPath>{packagePath}\Microsoft.CSharp.Core.targets</CSharpCoreTargetsPath>
    <VisualBasicCoreTargetsPath>{packagePath}\Microsoft.VisualBasic.Core.targets</VisualBasicCoreTargetsPath>
  </PropertyGroup> 

  <UsingTask TaskName=""Microsoft.CodeAnalysis.BuildTasks.Csc"" AssemblyFile=""{packagePath}\Microsoft.Build.Tasks.CodeAnalysis.dll"" Condition=""'$(RoslynHive)' == '{hiveName}'"" />
  <UsingTask TaskName=""Microsoft.CodeAnalysis.BuildTasks.Vbc"" AssemblyFile=""{packagePath}\Microsoft.Build.Tasks.CodeAnalysis.dll"" Condition=""'$(RoslynHive)' == '{hiveName}'"" />
</Project>");

            // This targets content we want to be included later since the project file might touch UseSharedCompilation
            var targetsContent =
                    $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <!-- If we're not using the compiler server, set ToolPath/Exe to direct to the exes in this package -->
  <PropertyGroup Condition=""'$(RoslynHive)' == '{hiveName}' and '$(UseSharedCompilation)' == 'false'"">
    <CscToolPath>{packagePath}</CscToolPath>
    <CscToolExe>csc.exe</CscToolExe>
    <VbcToolPath>{packagePath}</VbcToolPath>
    <VbcToolExe>vbc.exe</VbcToolExe>
  </PropertyGroup>
</Project>";

            filesToWrite.Add(await GetMSBuildRelativePathAsync($@"Microsoft.CSharp.targets\ImportBefore\Roslyn.Compilers.Extension.{hiveName}.targets", cancellationToken).ConfigureAwait(true), targetsContent);
            filesToWrite.Add(await GetMSBuildRelativePathAsync($@"Microsoft.VisualBasic.targets\ImportBefore\Roslyn.Compilers.Extension.{hiveName}.targets", cancellationToken).ConfigureAwait(true), targetsContent);

            // First we want to ensure any Roslyn files with our hive name that we aren't writing -- this is probably
            // leftovers from older extensions
            var msbuildDirectory = new DirectoryInfo(await GetMSBuildPathAsync(cancellationToken).ConfigureAwait(true));
            if (msbuildDirectory.Exists)
            {
                foreach (var file in msbuildDirectory.EnumerateFiles($"*Roslyn*{hiveName}*", SearchOption.AllDirectories))
                {
                    if (!filesToWrite.ContainsKey(file.FullName))
                    {
                        file.Delete();
                    }
                }
            }

            try
            {
                foreach (var fileAndContents in filesToWrite)
                {
                    var parentDirectory = new DirectoryInfo(Path.GetDirectoryName(fileAndContents.Key));
                    parentDirectory.Create();

                    // If we already know the file has the same contents, then we can skip
                    if (File.Exists(fileAndContents.Key) && File.ReadAllText(fileAndContents.Key) == fileAndContents.Value)
                    {
                        continue;
                    }

                    File.WriteAllText(fileAndContents.Key, fileAndContents.Value);
                }
            }
            catch (Exception e)
            {
                var msg =
$@"{e.Message}

To reload the Roslyn compiler package, close Visual Studio and any MSBuild processes, then restart Visual Studio.";

                VsShellUtilities.ShowMessageBox(
                    this,
                    msg,
                    null,
                    OLEMSGICON.OLEMSGICON_WARNING,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
        }


        private async Task<string> GetMSBuildVersionStringAsync(CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var dte = (DTE)await GetServiceAsync(typeof(SDTE)).ConfigureAwait(true);
            var parts = dte.Version.Split('.');
            if (parts.Length != 2)
            {
                throw new Exception($"Unrecognized Visual Studio Version: {dte.Version}");
            }

            int majorVersion = int.Parse(parts[0]);

            if (majorVersion >= 16)
            {
                // Starting in Visual Studio 2019, the folder is just called "Current". See
                // https://github.com/Microsoft/msbuild/issues/4149 for further commentary.
                return "Current";
            }
            else
            {
                return majorVersion + ".0";
            }
        }

        private async Task<string> GetMSBuildPathAsync(CancellationToken cancellationToken)
        {
            var version = await GetMSBuildVersionStringAsync(cancellationToken).ConfigureAwait(true);
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localAppData, $@"Microsoft\MSBuild\{version}");
        }

        private async Task<string> GetMSBuildRelativePathAsync(string relativePath, CancellationToken cancellationToken)
        {
            return Path.Combine(await GetMSBuildPathAsync(cancellationToken).ConfigureAwait(true), relativePath);
        }
    }
}
