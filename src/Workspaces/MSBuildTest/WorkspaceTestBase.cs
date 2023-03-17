// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UnitTests.TestFiles;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    [UseExportProvider]
    public class WorkspaceTestBase : TestBase
    {
        protected readonly TempDirectory SolutionDirectory;

        protected static readonly TimeSpan AsyncEventTimeout = TimeSpan.FromMinutes(5);

        public WorkspaceTestBase()
        {
            this.SolutionDirectory = Temp.CreateDirectory();
        }

        /// <summary>
        /// Gets an absolute file name for a file relative to the tests solution directory.
        /// </summary>
        public string GetSolutionFileName(string relativeFileName)
        {
            return Path.Combine(this.SolutionDirectory.Path, relativeFileName);
        }

        protected void CreateFiles(IEnumerable<(string filePath, object fileContent)> fileNamesAndContent)
        {
            foreach (var (filePath, fileContent) in fileNamesAndContent)
            {
                Debug.Assert(fileContent is string or byte[]);

                var subdirectory = Path.GetDirectoryName(filePath);
                var fileName = Path.GetFileName(filePath);

                var dir = SolutionDirectory;

                if (!string.IsNullOrEmpty(subdirectory))
                {
                    dir = dir.CreateDirectory(subdirectory);
                }

                // workspace uses File APIs that don't work with "delete on close" files:
                var file = dir.CreateFile(fileName);

                if (fileContent is string s)
                {
                    file.WriteAllText(s);
                }
                else
                {
                    file.WriteAllBytes((byte[])fileContent);
                }
            }
        }

        protected void CreateCSharpFilesWith(string propertyName, string value)
        {
            CreateFiles(GetSimpleCSharpSolutionFiles()
                .WithFile(@"CSharpProject\CSharpProject.csproj", Resources.ProjectFiles.CSharp.AllOptions)
                .ReplaceFileElement(@"CSharpProject\CSharpProject.csproj", propertyName, value));
        }

        protected void CreateVBFilesWith(string propertyName, string value)
        {
            CreateFiles(GetMultiProjectSolutionFiles()
                .ReplaceFileElement(@"VisualBasicProject\VisualBasicProject.vbproj", propertyName, value));
        }

        protected void CreateCSharpFiles()
        {
            CreateFiles(GetSimpleCSharpSolutionFiles());
        }

        protected static FileSet GetBaseFiles()
        {
            return new FileSet(
                (@"NuGet.Config", Resources.NuGet_Config),
                (@"Directory.Build.props", Resources.Directory_Build_props),
                (@"Directory.Build.targets", Resources.Directory_Build_targets));
        }

        protected static FileSet GetSimpleCSharpSolutionFiles()
        {
            return new FileSet(
                (@"NuGet.Config", Resources.NuGet_Config),
                (@"Directory.Build.props", Resources.Directory_Build_props),
                (@"Directory.Build.targets", Resources.Directory_Build_targets),
                (@"TestSolution.sln", Resources.SolutionFiles.CSharp),
                (@"CSharpProject\CSharpProject.csproj", Resources.ProjectFiles.CSharp.CSharpProject),
                (@"CSharpProject\CSharpClass.cs", Resources.SourceFiles.CSharp.CSharpClass),
                (@"CSharpProject\Properties\AssemblyInfo.cs", Resources.SourceFiles.CSharp.AssemblyInfo));
        }

        protected static FileSet GetSimpleVisualBasicSolutionFiles()
        {
            return new FileSet(
                (@"NuGet.Config", Resources.NuGet_Config),
                (@"Directory.Build.props", Resources.Directory_Build_props),
                (@"Directory.Build.targets", Resources.Directory_Build_targets),
                (@"TestSolution.sln", Resources.SolutionFiles.VisualBasic),
                (@"VisualBasicProject\VisualBasicProject.vbproj", Resources.ProjectFiles.VisualBasic.VisualBasicProject),
                (@"VisualBasicProject\VisualBasicClass.vb", Resources.SourceFiles.VisualBasic.VisualBasicClass),
                (@"VisualBasicProject\My Project\Application.Designer.vb", Resources.SourceFiles.VisualBasic.Application_Designer),
                (@"VisualBasicProject\My Project\Application.myapp", Resources.SourceFiles.VisualBasic.Application),
                (@"VisualBasicProject\My Project\AssemblyInfo.vb", Resources.SourceFiles.VisualBasic.AssemblyInfo),
                (@"VisualBasicProject\My Project\Resources.Designer.vb", Resources.SourceFiles.VisualBasic.Resources_Designer),
                (@"VisualBasicProject\My Project\Resources.resx", Resources.SourceFiles.VisualBasic.Resources),
                (@"VisualBasicProject\My Project\Settings.Designer.vb", Resources.SourceFiles.VisualBasic.Settings_Designer),
                (@"VisualBasicProject\My Project\Settings.settings", Resources.SourceFiles.VisualBasic.Settings));
        }

        protected static FileSet GetSimpleCSharpSolutionWithAdditionaFile()
        {
            return new FileSet(
                (@"NuGet.Config", Resources.NuGet_Config),
                (@"Directory.Build.props", Resources.Directory_Build_props),
                (@"Directory.Build.targets", Resources.Directory_Build_targets),
                (@"TestSolution.sln", Resources.SolutionFiles.CSharp),
                (@"CSharpProject\CSharpProject.csproj", Resources.ProjectFiles.CSharp.AdditionalFile),
                (@"CSharpProject\CSharpClass.cs", Resources.SourceFiles.CSharp.CSharpClass),
                (@"CSharpProject\Properties\AssemblyInfo.cs", Resources.SourceFiles.CSharp.AssemblyInfo),
                (@"CSharpProject\ValidAdditionalFile.txt", Resources.SourceFiles.Text.ValidAdditionalFile));
        }

        protected static FileSet GetNetCoreAppFiles()
        {
            return new FileSet(
                (@"NuGet.Config", Resources.NuGet_Config),
                (@"Directory.Build.props", Resources.Directory_Build_props),
                (@"Directory.Build.targets", Resources.Directory_Build_targets),
                (@"Project.csproj", Resources.ProjectFiles.CSharp.NetCoreApp_Project),
                (@"Program.cs", Resources.SourceFiles.CSharp.NetCoreApp_Program));
        }

        protected static FileSet GetNetCoreAppAndLibraryFiles()
        {
            return new FileSet(
                (@"NuGet.Config", Resources.NuGet_Config),
                (@"Directory.Build.props", Resources.Directory_Build_props),
                (@"Directory.Build.targets", Resources.Directory_Build_targets),
                (@"Project\Project.csproj", Resources.ProjectFiles.CSharp.NetCoreAppAndLibrary_Project),
                (@"Project\Program.cs", Resources.SourceFiles.CSharp.NetCoreAppAndLibrary_Program),
                (@"Library\Library.csproj", Resources.ProjectFiles.CSharp.NetCoreAppAndLibrary_Library),
                (@"Library\Class1.cs", Resources.SourceFiles.CSharp.NetCoreAppAndLibrary_Class1));
        }

        protected static FileSet GetNetCoreAppAndTwoLibrariesFiles()
        {
            return new FileSet(
                (@"NuGet.Config", Resources.NuGet_Config),
                (@"Directory.Build.props", Resources.Directory_Build_props),
                (@"Directory.Build.targets", Resources.Directory_Build_targets),
                (@"Project\Project.csproj", Resources.ProjectFiles.CSharp.NetCoreAppAndTwoLibraries_Project),
                (@"Project\Program.cs", Resources.SourceFiles.CSharp.NetCoreAppAndTwoLibraries_Program),
                (@"Library1\Library1.csproj", Resources.ProjectFiles.CSharp.NetCoreAppAndTwoLibraries_Library1),
                (@"Library1\Class1.cs", Resources.SourceFiles.CSharp.NetCoreAppAndTwoLibraries_Class1),
                (@"Library2\Library2.csproj", Resources.ProjectFiles.CSharp.NetCoreAppAndTwoLibraries_Library2),
                (@"Library2\Class2.cs", Resources.SourceFiles.CSharp.NetCoreAppAndTwoLibraries_Class2));
        }

        protected static FileSet GetNetCoreMultiTFMFiles()
        {
            return new FileSet(
                (@"NuGet.Config", Resources.NuGet_Config),
                (@"Directory.Build.props", Resources.Directory_Build_props),
                (@"Directory.Build.targets", Resources.Directory_Build_targets),
                (@"Project.csproj", Resources.ProjectFiles.CSharp.NetCoreMultiTFM_Project),
                (@"Program.cs", Resources.SourceFiles.CSharp.NetCoreApp_Program));
        }

        protected static FileSet GetNetCoreMultiTFMFiles_ExtensionWithConditionOnTFM()
        {
            return new FileSet(
                (@"NuGet.Config", Resources.NuGet_Config),
                (@"Directory.Build.props", Resources.Directory_Build_props),
                (@"Directory.Build.targets", Resources.Directory_Build_targets),
                (@"Project.csproj", Resources.ProjectFiles.CSharp.NetCoreMultiTFM_ExtensionWithConditionOnTFM_Project),
                (@"obj\Project.csproj.test.props", Resources.ProjectFiles.CSharp.NetCoreMultiTFM_ExtensionWithConditionOnTFM_ProjectTestProps));
        }

        protected static FileSet GetNetCoreMultiTFMFiles_ProjectReference()
        {
            return new FileSet(
                (@"NuGet.Config", Resources.NuGet_Config),
                (@"Directory.Build.props", Resources.Directory_Build_props),
                (@"Directory.Build.targets", Resources.Directory_Build_targets),
                (@"Project\Project.csproj", Resources.ProjectFiles.CSharp.NetCoreMultiTFM_ProjectReference_Project),
                (@"Project\Program.cs", Resources.SourceFiles.CSharp.NetCoreMultiTFM_ProjectReference_Program),
                (@"Library\Library.csproj", Resources.ProjectFiles.CSharp.NetCoreMultiTFM_ProjectReference_Library),
                (@"Library\Class1.cs", Resources.SourceFiles.CSharp.NetCoreMultiTFM_ProjectReference_Class1));
        }

        protected static FileSet GetNetCoreMultiTFMFiles_ProjectReferenceToFSharp()
        {
            return new FileSet(
                (@"NuGet.Config", Resources.NuGet_Config),
                (@"Directory.Build.props", Resources.Directory_Build_props),
                (@"Directory.Build.targets", Resources.Directory_Build_targets),
                (@"Solution.sln", Resources.SolutionFiles.NetCoreMultiTFM_ProjectReferenceToFSharp),
                (@"csharplib\csharplib.csproj", Resources.ProjectFiles.CSharp.NetCoreMultiTFM_ProjectReferenceToFSharp_CSharpLib),
                (@"csharplib\Class1.cs", Resources.SourceFiles.CSharp.NetCoreMultiTFM_ProjectReferenceToFSharp_CSharpLib_Class1),
                (@"fsharplib\fsharplib.fsproj", Resources.ProjectFiles.FSharp.NetCoreMultiTFM_ProjectReferenceToFSharp_FSharpLib),
                (@"fsharplib\Library.fs", Resources.SourceFiles.FSharp.NetCoreMultiTFM_ProjectReferenceToFSharp_FSharpLib_Library));
        }

        protected static FileSet GetMultiProjectSolutionFiles()
        {
            return new FileSet(
                (@"NuGet.Config", Resources.NuGet_Config),
                (@"Directory.Build.props", Resources.Directory_Build_props),
                (@"Directory.Build.targets", Resources.Directory_Build_targets),
                (@"TestSolution.sln", Resources.SolutionFiles.VB_and_CSharp),
                (@"CSharpProject\CSharpProject.csproj", Resources.ProjectFiles.CSharp.CSharpProject),
                (@"CSharpProject\CSharpClass.cs", Resources.SourceFiles.CSharp.CSharpClass),
                (@"CSharpProject\Properties\AssemblyInfo.cs", Resources.SourceFiles.CSharp.AssemblyInfo),
                (@"VisualBasicProject\VisualBasicProject.vbproj", Resources.ProjectFiles.VisualBasic.VisualBasicProject),
                (@"VisualBasicProject\VisualBasicClass.vb", Resources.SourceFiles.VisualBasic.VisualBasicClass),
                (@"VisualBasicProject\My Project\Application.Designer.vb", Resources.SourceFiles.VisualBasic.Application_Designer),
                (@"VisualBasicProject\My Project\Application.myapp", Resources.SourceFiles.VisualBasic.Application),
                (@"VisualBasicProject\My Project\AssemblyInfo.vb", Resources.SourceFiles.VisualBasic.AssemblyInfo),
                (@"VisualBasicProject\My Project\Resources.Designer.vb", Resources.SourceFiles.VisualBasic.Resources_Designer),
                (@"VisualBasicProject\My Project\Resources.resx", Resources.SourceFiles.VisualBasic.Resources),
                (@"VisualBasicProject\My Project\Settings.Designer.vb", Resources.SourceFiles.VisualBasic.Settings_Designer),
                (@"VisualBasicProject\My Project\Settings.settings", Resources.SourceFiles.VisualBasic.Settings));
        }

        protected static FileSet GetProjectReferenceSolutionFiles()
        {
            return new FileSet(
                (@"NuGet.Config", Resources.NuGet_Config),
                (@"Directory.Build.props", Resources.Directory_Build_props),
                (@"Directory.Build.targets", Resources.Directory_Build_targets),
                (@"CSharpProjectReference.sln", Resources.SolutionFiles.CSharp_ProjectReference),
                (@"CSharpProject\CSharpProject.csproj", Resources.ProjectFiles.CSharp.CSharpProject),
                (@"CSharpProject\CSharpClass.cs", Resources.SourceFiles.CSharp.CSharpClass),
                (@"CSharpProject\Properties\AssemblyInfo.cs", Resources.SourceFiles.CSharp.AssemblyInfo),
                (@"CSharpProject\CSharpProject_ProjectReference.csproj", Resources.ProjectFiles.CSharp.ProjectReference),
                (@"CSharpProject\CSharpConsole.cs", Resources.SourceFiles.CSharp.CSharpConsole));
        }

        protected static FileSet GetDuplicateProjectReferenceSolutionFiles()
        {
            return new FileSet(
                (@"NuGet.Config", Resources.NuGet_Config),
                (@"Directory.Build.props", Resources.Directory_Build_props),
                (@"Directory.Build.targets", Resources.Directory_Build_targets),
                (@"CSharpProjectReference.sln", Resources.SolutionFiles.CSharp_ProjectReference),
                (@"CSharpProject\CSharpProject.csproj", Resources.ProjectFiles.CSharp.CSharpProject),
                (@"CSharpProject\CSharpClass.cs", Resources.SourceFiles.CSharp.CSharpClass),
                (@"CSharpProject\Properties\AssemblyInfo.cs", Resources.SourceFiles.CSharp.AssemblyInfo),
                (@"CSharpProject\CSharpProject_ProjectReference.csproj", Resources.ProjectFiles.CSharp.DuplicateReferences),
                (@"CSharpProject\CSharpConsole.cs", Resources.SourceFiles.CSharp.CSharpConsole),
                (@"CSharpProject\EmptyLibrary.dll", Resources.Dlls.EmptyLibrary));
        }

        protected static FileSet GetAnalyzerReferenceSolutionFiles()
        {
            return new FileSet(
                (@"NuGet.Config", Resources.NuGet_Config),
                (@"Directory.Build.props", Resources.Directory_Build_props),
                (@"Directory.Build.targets", Resources.Directory_Build_targets),
                (@"AnalyzerReference.sln", Resources.SolutionFiles.AnalyzerReference),
                (@"AnalyzerSolution\CSharpProject.dll", Resources.Dlls.CSharpProject),
                (@"AnalyzerSolution\CSharpProject_AnalyzerReference.csproj", Resources.ProjectFiles.CSharp.AnalyzerReference),
                (@"AnalyzerSolution\CSharpClass.cs", Resources.SourceFiles.CSharp.CSharpClass),
                (@"AnalyzerSolution\XamlFile.xaml", Resources.SourceFiles.Xaml.MainWindow),
                (@"AnalyzerSolution\VisualBasicProject_AnalyzerReference.vbproj", Resources.ProjectFiles.VisualBasic.AnalyzerReference),
                (@"AnalyzerSolution\VisualBasicClass.vb", Resources.SourceFiles.VisualBasic.VisualBasicClass),
                (@"AnalyzerSolution\My Project\Application.Designer.vb", Resources.SourceFiles.VisualBasic.Application_Designer),
                (@"AnalyzerSolution\My Project\Application.myapp", Resources.SourceFiles.VisualBasic.Application),
                (@"AnalyzerSolution\My Project\AssemblyInfo.vb", Resources.SourceFiles.VisualBasic.AssemblyInfo),
                (@"AnalyzerSolution\My Project\Resources.Designer.vb", Resources.SourceFiles.VisualBasic.Resources_Designer),
                (@"AnalyzerSolution\My Project\Resources.resx", Resources.SourceFiles.VisualBasic.Resources),
                (@"AnalyzerSolution\My Project\Settings.Designer.vb", Resources.SourceFiles.VisualBasic.Settings_Designer),
                (@"AnalyzerSolution\My Project\Settings.settings", Resources.SourceFiles.VisualBasic.Settings));
        }

        protected static FileSet GetSolutionWithDuplicatedGuidFiles()
        {
            return new FileSet(
                (@"NuGet.Config", Resources.NuGet_Config),
                (@"Directory.Build.props", Resources.Directory_Build_props),
                (@"Directory.Build.targets", Resources.Directory_Build_targets),
                (@"DuplicatedGuids.sln", Resources.SolutionFiles.DuplicatedGuids),
                (@"ReferenceTest\ReferenceTest.csproj", Resources.ProjectFiles.CSharp.DuplicatedGuidReferenceTest),
                (@"Library1\Library1.csproj", Resources.ProjectFiles.CSharp.DuplicatedGuidLibrary1),
                (@"Library2\Library2.csproj", Resources.ProjectFiles.CSharp.DuplicatedGuidLibrary2));
        }

        protected static FileSet GetSolutionWithCircularProjectReferences()
        {
            return new FileSet(
                (@"NuGet.Config", Resources.NuGet_Config),
                (@"Directory.Build.props", Resources.Directory_Build_props),
                (@"Directory.Build.targets", Resources.Directory_Build_targets),
                (@"CircularSolution.sln", Resources.SolutionFiles.CircularSolution),
                (@"CircularCSharpProject1.csproj", Resources.ProjectFiles.CSharp.CircularProjectReferences_CircularCSharpProject1),
                (@"CircularCSharpProject2.csproj", Resources.ProjectFiles.CSharp.CircularProjectReferences_CircularCSharpProject2));
        }

        protected static FileSet GetVBNetCoreAppWithGlobalImportAndLibraryFiles()
        {
            return new FileSet(
                (@"NuGet.Config", Resources.NuGet_Config),
                (@"Directory.Build.props", Resources.Directory_Build_props),
                (@"Directory.Build.targets", Resources.Directory_Build_targets),
                (@"VBProject\VBProject.vbproj", Resources.ProjectFiles.VisualBasic.VBNetCoreAppWithGlobalImportAndLibrary_VBProject),
                (@"VBProject\Program.vb", Resources.SourceFiles.VisualBasic.VBNetCoreAppWithGlobalImportAndLibrary_Program),
                (@"Library\Library.csproj", Resources.ProjectFiles.CSharp.VBNetCoreAppWithGlobalImportAndLibrary_Library),
                (@"Library\MyHelperClass.cs", Resources.SourceFiles.CSharp.VBNetCoreAppWithGlobalImportAndLibrary_MyHelperClass));
        }
    }
}
