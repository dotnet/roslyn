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
        private const string MSBuildDirectory = @"Microsoft\MSBuild\14.0";

        private const string WriteFileExceptionMessage =
@"Unable to write {0}

{1}

To reload the Roslyn compiler package, close Visual Studio and any MSBuild processes, then restart Visual Studio.";

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

                WriteTargetsFile(packagePath, roslynHive);

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

        private void WriteTargetsFile(string packagePath, string hiveName)
        {
            var targetsFileContent =
                $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup Condition = ""'$(RoslynHive)' == '{hiveName}'"">
    <CscToolPath>{packagePath}</CscToolPath>
    <CscToolExe>csc.exe</CscToolExe>
    <VbcToolPath>{packagePath}</VbcToolPath>
    <VbcToolExe>vbc.exe</VbcToolExe>
  </PropertyGroup>
</Project>";

            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var msbuildExtensionsPath = Path.Combine(localAppData, MSBuildDirectory);

            foreach (var msbuildLanguage in new[] { "CSharp", "VisualBasic" })
            {
                var targetsFile = Path.Combine(msbuildExtensionsPath, $@"Microsoft.{msbuildLanguage}.targets\ImportAfter\Roslyn.Compilers.Extension.{msbuildLanguage}.{hiveName}.targets");

                try
                {
                    var directory = new DirectoryInfo(Path.GetDirectoryName(targetsFile));
                    if (!directory.Exists)
                    {
                        directory.Create();
                    }

                    // Delete any other targets we might have left around
                    foreach (var file in directory.GetFileSystemInfos("*.targets"))
                    {
                        if (file.Name.Contains("Roslyn"))
                        {
                            file.Delete();
                        }
                    }

                    File.WriteAllText(targetsFile, targetsFileContent);
                }
                catch (Exception e)
                {
                    ReportWriteFileException(targetsFile, e);
                }
            }
        }

        private void ReportWriteFileException(string path, Exception e)
        {
            VsShellUtilities.ShowMessageBox(
                this,
                string.Format(WriteFileExceptionMessage, path, e.Message),
                null,
                OLEMSGICON.OLEMSGICON_WARNING,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
    }
}
