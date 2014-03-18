// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CompilerPackage
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [PackageLoadKey("0.6.0.0", "CompilerPackage", "Microsoft", WDExpressId = 1, VWDExpressId = 2, VsWinExpressId = 3)]
    [Guid("fc8d0600-8f16-4a89-a49c-a4f6c38b216a")]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExists_string)]
    public sealed class CompilerPackage : Package
    {
        // The targets templates rely on two properties: DisableRoslyn and RoslynToolPath.
        // RoslynToolPath is the full path to the compiler binaries within the package
        // while DisableRoslyn is a simple boolean used to determine whether to build
        // with Roslyn or not. DisableRoslyn is not strictly necessary. Instead, the targets file
        // could check "'$(RoslynToolPath)'!=''" but DisableRoslyn allows disabling Roslyn
        // for builds outside of VS without first uninstalling.
        //
        // NOTE: even though the task defined above does not use the 'rcsc.exe'/'rvbc.exe' in any way
        // and compiles the target using Roslyn compiler server, Roslyn Csc/Vbc msbuild task
        // inherits host object initialization implementation from Microsoft.Build.Utilities.ToolTask,
        // it also inherits some tool executable pre-check logic; disabling these pre-checks 
        // is not straightforward, so we reference rcsc.exe/rvbc.exe here to simplify implementation
        private const string CSharpTargetsTemplate =
@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup>
    <RoslynToolPath>{0}</RoslynToolPath>
  </PropertyGroup>
  <UsingTask
    Condition=""'$(DisableRoslyn)'!='true' And Exists('$(RoslynToolPath)')""
    AssemblyFile=""$([MSBuild]::Unescape($(RoslynToolPath)))\Roslyn.Compilers.BuildTasks.dll""
    TaskName=""Csc""
    TaskFactory=""Microsoft.CodeAnalysis.BuildTasks.CscTaskFactory"">
    <ParameterGroup>
      <Parameter1 ParameterType = ""System.String"" Required=""true"" Output=""False"" />
    </ParameterGroup>
    <Task Evaluate=""true"" >$(VSSessionGuid)</Task>
  </UsingTask>
  <PropertyGroup Condition=""'$(DisableRoslyn)'!='true' And Exists('$(RoslynToolPath)')"">
    <CscToolPath>$(RoslynToolPath)</CscToolPath>
    <CscToolExe>rcsc.exe</CscToolExe>
  </PropertyGroup>
</Project>
";
        private const string VisualBasicTargetsTemplate =
@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup>
    <RoslynToolPath>{0}</RoslynToolPath>
  </PropertyGroup>
  <UsingTask
    Condition=""'$(DisableRoslyn)'!='true' And Exists('$(RoslynToolPath)')""
    AssemblyFile=""$([MSBuild]::Unescape($(RoslynToolPath)))\Roslyn.Compilers.BuildTasks.dll""
    TaskName=""Vbc""
    TaskFactory=""Microsoft.CodeAnalysis.BuildTasks.VbcTaskFactory"">
    <ParameterGroup>
      <Parameter1 ParameterType = ""System.String"" Required=""true"" Output=""False"" />
    </ParameterGroup>
    <Task Evaluate=""true"" >$(VSSessionGuid)</Task>
  </UsingTask>
  <PropertyGroup Condition=""'$(DisableRoslyn)'!='true' And Exists('$(RoslynToolPath)')"">
    <VbcToolPath>$(RoslynToolPath)</VbcToolPath>
    <VbcToolExe>rvbc.exe</VbcToolExe>
  </PropertyGroup>
</Project>
";
        private const string WriteFileExceptionTitle = "CompilerPackage";
        private const string WriteFileExceptionMessage =
@"Unable to write {0}

{1}

To reload the Roslyn compiler package, close Visual Studio and any MSBuild processes, then restart Visual Studio."/*.NeedsLocalization()*/;

        protected override void Initialize()
        {
            var packagePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var userExtensionsPath = Path.Combine(localAppData, @"Microsoft\MSBuild\12.0"); // $(MSBuildUserExtensionsPath)\$(MSBuildToolsVersion)

            WriteFile(
                Path.Combine(userExtensionsPath, @"Microsoft.CSharp.targets\ImportAfter\Microsoft.CSharp.Roslyn.targets"),
                string.Format(CSharpTargetsTemplate, packagePath));
            WriteFile(
                Path.Combine(userExtensionsPath, @"Microsoft.VisualBasic.targets\ImportAfter\Microsoft.VisualBasic.Roslyn.targets"),
                string.Format(VisualBasicTargetsTemplate, packagePath));
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
                WriteFileExceptionTitle,
                OLEMSGICON.OLEMSGICON_WARNING,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
    }
}
