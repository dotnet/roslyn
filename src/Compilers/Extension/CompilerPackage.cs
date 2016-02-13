// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using System.Reflection;
using System.IO;

namespace Roslyn.Compilers.Extension
{
    [ProvideAutoLoad(UIContextGuids.SolutionExists)]
    public sealed class CompilerPackage : Package
    {
        protected override void Initialize()
        {
            base.Initialize();

            var packagePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            string localRegistryRoot;
            var reg = (ILocalRegistry2)this.GetService(typeof(SLocalRegistry));
            reg.GetLocalRegistryRoot(out localRegistryRoot);
            var registryParts = localRegistryRoot.Split('\\');

            // Is it a valid Hive looks similar to:  
            //  'Software\Microsoft\VisualStudio\14.0'  'Software\Microsoft\VisualStudio\14.0Roslyn'  'Software\Microsoft\VSWinExpress\14.0'
            if (registryParts.Length >= 4)
            {
                var skuName = registryParts[2];
                var hiveName = registryParts[3];
                var roslynHive = string.Format(@"{0}.{1}", registryParts[2], registryParts[3]);

                WriteMSBuildFiles(packagePath, roslynHive);

                try
                {
                    Microsoft.Build.Evaluation.ProjectCollection.GlobalProjectCollection.DisableMarkDirty = true;
                    Microsoft.Build.Evaluation.ProjectCollection.GlobalProjectCollection.SetGlobalProperty("RoslynHive", roslynHive);
                }
                finally
                {
                    Microsoft.Build.Evaluation.ProjectCollection.GlobalProjectCollection.DisableMarkDirty = false;
                }
            }
        }

        private void WriteMSBuildFiles(string packagePath, string hiveName)
        {
            // First we want to ensure any existing Roslyn files are deleted so we don't have old stuff floating
            // aroud and causing troubles
            var msbuildDirectory = new DirectoryInfo(GetMSBuildPath());
            if (msbuildDirectory.Exists)
            {
                foreach (var file in msbuildDirectory.EnumerateFiles($"*Roslyn*{hiveName}*", SearchOption.AllDirectories))
                {
                    file.Delete();
                }
            }

            try
            {
                // The props we want to be included as early as possible since we want our tasks to be used and
                // to ensure our setting of targets path happens early enough
                var propsContent =
                    $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup Condition=""'$(RoslynHive)' == '{hiveName}'"">
    <RoslynCompilerExtensionActive>true</RoslynCompilerExtensionActive>
    <CSharpCoreTargetsPath>{packagePath}\Microsoft.CSharp.Core.targets</CSharpCoreTargetsPath>
    <VisualBasicCoreTargetsPath>{packagePath}\Microsoft.VisualBasic.Core.targets</VisualBasicCoreTargetsPath>
  </PropertyGroup> 

  <UsingTask TaskName=""Microsoft.CodeAnalysis.BuildTasks.Csc"" AssemblyFile=""{packagePath}\Microsoft.Build.Tasks.CodeAnalysis.dll"" Condition=""'$(RoslynCompilerExtensionActive)' == 'true'"" />
  <UsingTask TaskName=""Microsoft.CodeAnalysis.BuildTasks.Vbc"" AssemblyFile=""{packagePath}\Microsoft.Build.Tasks.CodeAnalysis.dll"" Condition=""'$(RoslynCompilerExtensionActive)' == 'true'"" />
</Project>";

                WriteMSBuildFile(propsContent, $@"Imports\Microsoft.Common.props\ImportBefore\Roslyn.Compilers.Extension.{hiveName}.props");

                // This targets content we want to be included later since the project flie might touch UseSharedCompilation
                var targetsContent =
                    $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <!-- If we're not using the compiler server, set ToolPath/Exe to direct to the exes in this package -->
  <PropertyGroup Condition=""'$(RoslynCompilerExtensionActive)' == 'true' and '$(UseSharedCompilation)' != 'true'"">
    <CscToolPath>{packagePath}</CscToolPath>
    <CscToolExe>csc.exe</CscToolExe>
    <VbcToolPath>{packagePath}</VbcToolPath>
    <VbcToolExe>vbc.exe</VbcToolExe>
    <UseSharedCompilation>false</UseSharedCompilation>
  </PropertyGroup>
</Project>";

                WriteMSBuildFile(targetsContent, $@"Microsoft.CSharp.targets\ImportBefore\Roslyn.Compilers.Extension.{hiveName}.targets");
                WriteMSBuildFile(targetsContent, $@"Microsoft.VisualBasic.targets\ImportBefore\Roslyn.Compilers.Extension.{hiveName}.targets");
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

        private static void WriteMSBuildFile(string content, string relativeFilePath)
        {
            var fileFullPath = Path.Combine(GetMSBuildPath(), relativeFilePath);
            var directory = new DirectoryInfo(Path.GetDirectoryName(fileFullPath));
            if (!directory.Exists)
            {
                directory.Create();
            }

            File.WriteAllText(fileFullPath, content);
        }

        private static string GetMSBuildPath()
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localAppData, @"Microsoft\MSBuild\14.0");
        }
    }
}
