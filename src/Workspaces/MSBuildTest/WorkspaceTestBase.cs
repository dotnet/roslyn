// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        protected void CreateFiles(params string[] fileNames)
        {
            var fileNamesAndContent = Array.ConvertAll(fileNames, fileName => (fileName, (object)Resources.GetText(fileName)));
            var fileSet = new FileSet(fileNamesAndContent);
            CreateFiles(fileSet);
        }

        protected void CreateFiles(IEnumerable<(string filePath, object fileContent)> fileNamesAndContent)
        {
            foreach (var (filePath, fileContent) in fileNamesAndContent)
            {
                Debug.Assert(fileContent is string || fileContent is byte[]);

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

        protected FileSet GetBaseFiles()
        {
            return new FileSet(
                (@"NuGet.Config", Resources.NuGet_Config),
                (@"Directory.Build.props", Resources.Directory_Build_props),
                (@"Directory.Build.targets", Resources.Directory_Build_targets));
        }

        protected FileSet GetSimpleCSharpSolutionFiles()
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

        protected FileSet GetNetCoreApp2Files()
        {
            return new FileSet(
                (@"NuGet.Config", Resources.NuGet_Config),
                (@"Directory.Build.props", Resources.Directory_Build_props),
                (@"Directory.Build.targets", Resources.Directory_Build_targets),
                (@"Project.csproj", Resources.ProjectFiles.CSharp.NetCoreApp2_Project),
                (@"Program.cs", Resources.SourceFiles.CSharp.NetCoreApp2_Program));
        }

        protected FileSet GetNetCoreApp2AndLibraryFiles()
        {
            return new FileSet(
                (@"NuGet.Config", Resources.NuGet_Config),
                (@"Directory.Build.props", Resources.Directory_Build_props),
                (@"Directory.Build.targets", Resources.Directory_Build_targets),
                (@"Project\Project.csproj", Resources.ProjectFiles.CSharp.NetCoreApp2AndLibrary_Project),
                (@"Project\Program.cs", Resources.SourceFiles.CSharp.NetCoreApp2AndLibrary_Program),
                (@"Library\Library.csproj", Resources.ProjectFiles.CSharp.NetCoreApp2AndLibrary_Library),
                (@"Library\Class1.cs", Resources.SourceFiles.CSharp.NetCoreApp2AndLibrary_Class1));
        }

        protected FileSet GetNetCoreApp2AndTwoLibrariesFiles()
        {
            return new FileSet(
                (@"NuGet.Config", Resources.NuGet_Config),
                (@"Directory.Build.props", Resources.Directory_Build_props),
                (@"Directory.Build.targets", Resources.Directory_Build_targets),
                (@"Project\Project.csproj", Resources.ProjectFiles.CSharp.NetCoreApp2AndTwoLibraries_Project),
                (@"Project\Program.cs", Resources.SourceFiles.CSharp.NetCoreApp2AndTwoLibraries_Program),
                (@"Library1\Library1.csproj", Resources.ProjectFiles.CSharp.NetCoreApp2AndTwoLibraries_Library1),
                (@"Library1\Class1.cs", Resources.SourceFiles.CSharp.NetCoreApp2AndTwoLibraries_Class1),
                (@"Library2\Library2.csproj", Resources.ProjectFiles.CSharp.NetCoreApp2AndTwoLibraries_Library2),
                (@"Library2\Class2.cs", Resources.SourceFiles.CSharp.NetCoreApp2AndTwoLibraries_Class2));
        }

        protected FileSet GetNetCoreMultiTFMFiles()
        {
            return new FileSet(
                (@"NuGet.Config", Resources.NuGet_Config),
                (@"Directory.Build.props", Resources.Directory_Build_props),
                (@"Directory.Build.targets", Resources.Directory_Build_targets),
                (@"Project.csproj", Resources.ProjectFiles.CSharp.NetCoreMultiTFM_Project),
                (@"Program.cs", Resources.SourceFiles.CSharp.NetCoreApp2_Program));
        }

        protected FileSet GetNetCoreMultiTFMFiles_ExtensionWithConditionOnTFM()
        {
            return new FileSet(
                (@"NuGet.Config", Resources.NuGet_Config),
                (@"Directory.Build.props", Resources.Directory_Build_props),
                (@"Directory.Build.targets", Resources.Directory_Build_targets),
                (@"Project.csproj", Resources.ProjectFiles.CSharp.NetCoreMultiTFM_ExtensionWithConditionOnTFM_Project),
                (@"obj\Project.csproj.test.props", Resources.ProjectFiles.CSharp.NetCoreMultiTFM_ExtensionWithConditionOnTFM_ProjectTestProps));
        }

        protected FileSet GetNetCoreMultiTFMFiles_ProjectReference()
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

        protected FileSet GetNetCoreMultiTFMFiles_ProjectReferenceWithReversedTFMs()
        {
            return new FileSet(
                (@"NuGet.Config", Resources.NuGet_Config),
                (@"Directory.Build.props", Resources.Directory_Build_props),
                (@"Directory.Build.targets", Resources.Directory_Build_targets),
                (@"Project\Project.csproj", Resources.ProjectFiles.CSharp.NetCoreMultiTFM_ProjectReferenceWithReversedTFMs_Project),
                (@"Project\Program.cs", Resources.SourceFiles.CSharp.NetCoreMultiTFM_ProjectReferenceWithReversedTFMs_Program),
                (@"Library\Library.csproj", Resources.ProjectFiles.CSharp.NetCoreMultiTFM_ProjectReferenceWithReversedTFMs_Library),
                (@"Library\Class1.cs", Resources.SourceFiles.CSharp.NetCoreMultiTFM_ProjectReferenceWithReversedTFMs_Class1));
        }

        protected FileSet GetNetCoreMultiTFMFiles_ProjectReferenceToFSharp()
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

        protected FileSet GetMultiProjectSolutionFiles()
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

        protected FileSet GetProjectReferenceSolutionFiles()
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

        protected FileSet GetDuplicateProjectReferenceSolutionFiles()
        {
            return new FileSet(
                (@"NuGet.Config", Resources.NuGet_Config),
                (@"Directory.Build.props", Resources.Directory_Build_props),
                (@"Directory.Build.targets", Resources.Directory_Build_targets),
                (@"CSharpProjectReference.sln", Resources.SolutionFiles.CSharp_ProjectReference),
                (@"CSharpProject\CSharpProject.csproj", Resources.ProjectFiles.CSharp.CSharpProject),
                (@"CSharpProject\CSharpClass.cs", Resources.SourceFiles.CSharp.CSharpClass),
                (@"CSharpProject\Properties\AssemblyInfo.cs", Resources.SourceFiles.CSharp.AssemblyInfo),
                (@"CSharpProject\CSharpProject_ProjectReference.csproj", Resources.ProjectFiles.CSharp.DuplicateProjectReference),
                (@"CSharpProject\CSharpConsole.cs", Resources.SourceFiles.CSharp.CSharpConsole));
        }

        protected FileSet GetAnalyzerReferenceSolutionFiles()
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

        protected FileSet GetSolutionWithDuplicatedGuidFiles()
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

        protected FileSet GetSolutionWithCircularProjectReferences()
        {
            return new FileSet(
                (@"NuGet.Config", Resources.NuGet_Config),
                (@"Directory.Build.props", Resources.Directory_Build_props),
                (@"Directory.Build.targets", Resources.Directory_Build_targets),
                (@"CircularSolution.sln", Resources.SolutionFiles.CircularSolution),
                (@"CircularCSharpProject1.csproj", Resources.ProjectFiles.CSharp.CircularProjectReferences_CircularCSharpProject1),
                (@"CircularCSharpProject2.csproj", Resources.ProjectFiles.CSharp.CircularProjectReferences_CircularCSharpProject2));
        }

        protected static string GetParentDirOfParentDirOfContainingDir(string fileName)
        {
            var containingDir = Directory.GetParent(fileName).FullName;
            var parentOfContainingDir = Directory.GetParent(containingDir).FullName;

            return Directory.GetParent(parentOfContainingDir).FullName;
        }

        protected Document AssertSemanticVersionChanged(Document document, SourceText newText)
        {
            var docVersion = document.GetTopLevelChangeTextVersionAsync().Result;
            var projVersion = document.Project.GetSemanticVersionAsync().Result;

            var text = document.GetTextAsync().Result;
            var newDoc = document.WithText(newText);

            var newDocVersion = newDoc.GetTopLevelChangeTextVersionAsync().Result;
            var newProjVersion = newDoc.Project.GetSemanticVersionAsync().Result;

            Assert.NotEqual(docVersion, newDocVersion);
            Assert.NotEqual(projVersion, newProjVersion);

            return newDoc;
        }

        protected Document AssertSemanticVersionUnchanged(Document document, SourceText newText)
        {
            var docVersion = document.GetTopLevelChangeTextVersionAsync().Result;
            var projVersion = document.Project.GetSemanticVersionAsync().Result;

            var text = document.GetTextAsync().Result;
            var newDoc = document.WithText(newText);

            var newDocVersion = newDoc.GetTopLevelChangeTextVersionAsync().Result;
            var newProjVersion = newDoc.Project.GetSemanticVersionAsync().Result;

            Assert.Equal(docVersion, newDocVersion);
            Assert.Equal(projVersion, newProjVersion);

            return newDoc;
        }
    }
}
