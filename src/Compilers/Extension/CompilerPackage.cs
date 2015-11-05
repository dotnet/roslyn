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
        private const string VisualStudioVersion = "14.0";
        private const string VisualStudioHive = "VisualStudio";
        private const string MSBuildDirectory = @"Microsoft\MSBuild\14.0";

        private readonly string _CSharpTargetsTemplate =
@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup Condition=""'$(RoslynHive)'=='{0}'"">
    <CscToolPath>{1}</CscToolPath>
    <CscToolExe>csc.exe</CscToolExe>
  </PropertyGroup>
</Project>
";
        private const string VisualBasicTargetsTemplate =
@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup Condition=""'$(RoslynHive)'=='{0}'"">
    <VbcToolPath>{1}</VbcToolPath>
    <VbcToolExe>vbc.exe</VbcToolExe>
  </PropertyGroup>
</Project>
";

        private const string WriteFileExceptionMessage =
@"Unable to write {0}

{1}

To reload the Roslyn compiler package, close Visual Studio and any MSBuild processes, then restart Visual Studio.";

        protected override void Initialize()
        {
            // Generate targets file
            var packagePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var msbuildExtensionsPath = Path.Combine(localAppData, MSBuildDirectory);

            string localRegistryRoot;
            var reg = (ILocalRegistry2)this.GetService(typeof(SLocalRegistry));
            reg.GetLocalRegistryRoot(out localRegistryRoot);
            var regDirs = localRegistryRoot.Split('\\');
            var roslynHive = string.Format(@"{0}\{1}", regDirs[2], regDirs[3]);

            // Is it a valid Hive looks similar to:  
            //  'Software\Microsoft\VisualStudio\14.0'  'Software\Microsoft\VisualStudio\14.0Roslyn'  'Software\Microsoft\VSWinExpress\14.0'
            if (regDirs.Length >= 4)
            {
                if (regDirs[3].ToUpperInvariant() != "VISUALSTUDIOVERSION")
                {
                    WriteFile(
                        Path.Combine(msbuildExtensionsPath, string.Format(@"Microsoft.CSharp.targets\ImportAfter\Microsoft.CSharp.Roslyn.{0}.{1}.targets", regDirs[2], regDirs[3])),
                        string.Format(_CSharpTargetsTemplate, roslynHive, packagePath));
                    WriteFile(
                        Path.Combine(msbuildExtensionsPath, string.Format(@"Microsoft.VisualBasic.targets\ImportAfter\Microsoft.VisualBasic.Roslyn.{0}.{1}.targets", regDirs[2], regDirs[3])),
                        string.Format(VisualBasicTargetsTemplate, roslynHive, packagePath));
                }

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
            base.Initialize();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        private void WriteFile(string path, string contents)
        {
            try
            {
                var directory = Path.GetDirectoryName(path);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                using (var writer = new StreamWriter(path))
                {
                    writer.Write(contents);
                }
            }
            catch (IOException e)
            {
                ReportWriteFileException(path, e);
            }
            catch (UnauthorizedAccessException e)
            {
                ReportWriteFileException(path, e);
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
