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
            var fileNamesAndContent = Array.ConvertAll(fileNames, fileName => (fileName, (object)Resources.LoadText(fileName)));
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
                .WithFile(@"CSharpProject\CSharpProject.csproj", Resources.LoadText(@"CSharpProject_CSharpProject_AllOptions.csproj"))
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

        protected FileSet GetSimpleCSharpSolutionFiles()
        {
            return new FileSet(
                (@"Directory.Build.props", Resources.LoadText("Directory.Build.props")),
                (@"Directory.Build.targets", Resources.LoadText("Directory.Build.targets")),
                (@"TestSolution.sln", Resources.LoadText("TestSolution_CSharp.sln")),
                (@"CSharpProject\CSharpProject.csproj", Resources.LoadText("CSharpProject_CSharpProject.csproj")),
                (@"CSharpProject\CSharpClass.cs", Resources.LoadText("CSharpProject_CSharpClass.cs")),
                (@"CSharpProject\Properties\AssemblyInfo.cs", Resources.LoadText("CSharpProject_AssemblyInfo.cs")));
        }

        protected FileSet GetMultiProjectSolutionFiles()
        {
            return new FileSet(
                (@"Directory.Build.props", Resources.LoadText("Directory.Build.props")),
                (@"Directory.Build.targets", Resources.LoadText("Directory.Build.targets")),
                (@"TestSolution.sln", Resources.LoadText("TestSolution_VB_and_CSharp.sln")),
                (@"CSharpProject\CSharpProject.csproj", Resources.LoadText("CSharpProject_CSharpProject.csproj")),
                (@"CSharpProject\CSharpClass.cs", Resources.LoadText("CSharpProject_CSharpClass.cs")),
                (@"CSharpProject\Properties\AssemblyInfo.cs", Resources.LoadText("CSharpProject_AssemblyInfo.cs")),
                (@"VisualBasicProject\VisualBasicProject.vbproj", Resources.LoadText("VisualBasicProject_VisualBasicProject.vbproj")),
                (@"VisualBasicProject\VisualBasicClass.vb", Resources.LoadText("VisualBasicProject_VisualBasicClass.vb")),
                (@"VisualBasicProject\My Project\Application.Designer.vb", Resources.LoadText("VisualBasicProject_Application.Designer.vb")),
                (@"VisualBasicProject\My Project\Application.myapp", Resources.LoadText("VisualBasicProject_Application.myapp")),
                (@"VisualBasicProject\My Project\AssemblyInfo.vb", Resources.LoadText("VisualBasicProject_AssemblyInfo.vb")),
                (@"VisualBasicProject\My Project\Resources.Designer.vb", Resources.LoadText("VisualBasicProject_Resources.Designer.vb")),
                (@"VisualBasicProject\My Project\Resources.resx", Resources.LoadText("VisualBasicProject_Resources.resx_")),
                (@"VisualBasicProject\My Project\Settings.Designer.vb", Resources.LoadText("VisualBasicProject_Settings.Designer.vb")),
                (@"VisualBasicProject\My Project\Settings.settings", Resources.LoadText("VisualBasicProject_Settings.settings")));
        }

        protected FileSet GetProjectReferenceSolutionFiles()
        {
            return new FileSet(
                (@"Directory.Build.props", Resources.LoadText("Directory.Build.props")),
                (@"Directory.Build.targets", Resources.LoadText("Directory.Build.targets")),
                (@"CSharpProjectReference.sln", Resources.LoadText("TestSolution_CSharpProjectReference.sln")),
                (@"CSharpProject\CSharpProject.csproj", Resources.LoadText("CSharpProject_CSharpProject.csproj")),
                (@"CSharpProject\CSharpClass.cs", Resources.LoadText("CSharpProject_CSharpClass.cs")),
                (@"CSharpProject\Properties\AssemblyInfo.cs", Resources.LoadText("CSharpProject_AssemblyInfo.cs")),
                (@"CSharpProject\CSharpProject_ProjectReference.csproj", Resources.LoadText("CSharpProject_CSharpProject_ProjectReference.csproj")),
                (@"CSharpProject\CSharpConsole.cs", Resources.LoadText("CSharpProject_CSharpConsole.cs")));
        }

        protected FileSet GetAnalyzerReferenceSolutionFiles()
        {
            return new FileSet(
                (@"Directory.Build.props", Resources.LoadText("Directory.Build.props")),
                (@"Directory.Build.targets", Resources.LoadText("Directory.Build.targets")),
                (@"AnalyzerReference.sln", Resources.LoadText("TestSolution_AnalyzerReference.sln")),
                (@"AnalyzerSolution\CSharpProject.dll", Resources.LoadText("CSharpProject.dll")),
                (@"AnalyzerSolution\CSharpProject_AnalyzerReference.csproj", Resources.LoadText("CSharpProject_CSharpProject_AnalyzerReference.csproj")),
                (@"AnalyzerSolution\CSharpClass.cs", Resources.LoadText("CSharpProject_CSharpClass.cs")),
                (@"AnalyzerSolution\XamlFile.xaml", Resources.LoadText("CSharpProject_MainWindow.xaml")),
                (@"AnalyzerSolution\VisualBasicProject_AnalyzerReference.vbproj", Resources.LoadText("VisualBasicProject_VisualBasicProject_AnalyzerReference.vbproj")),
                (@"AnalyzerSolution\VisualBasicClass.vb", Resources.LoadText("VisualBasicProject_VisualBasicClass.vb")),
                (@"AnalyzerSolution\My Project\Application.Designer.vb", Resources.LoadText("VisualBasicProject_Application.Designer.vb")),
                (@"AnalyzerSolution\My Project\Application.myapp", Resources.LoadText("VisualBasicProject_Application.myapp")),
                (@"AnalyzerSolution\My Project\AssemblyInfo.vb", Resources.LoadText("VisualBasicProject_AssemblyInfo.vb")),
                (@"AnalyzerSolution\My Project\Resources.Designer.vb", Resources.LoadText("VisualBasicProject_Resources.Designer.vb")),
                (@"AnalyzerSolution\My Project\Resources.resx", Resources.LoadText("VisualBasicProject_Resources.resx_")),
                (@"AnalyzerSolution\My Project\Settings.Designer.vb", Resources.LoadText("VisualBasicProject_Settings.Designer.vb")),
                (@"AnalyzerSolution\My Project\Settings.settings", Resources.LoadText("VisualBasicProject_Settings.settings")));
        }

        protected FileSet GetSolutionWithDuplicatedGuidFiles()
        {
            return new FileSet(
                (@"Directory.Build.props", Resources.LoadText("Directory.Build.props")),
                (@"Directory.Build.targets", Resources.LoadText("Directory.Build.targets")),
                (@"DuplicatedGuids.sln", Resources.LoadText("TestSolution_DuplicatedGuids.sln")),
                (@"ReferenceTest\ReferenceTest.csproj", Resources.LoadText("CSharpProject_DuplicatedGuidReferenceTest.csproj")),
                (@"Library1\Library1.csproj", Resources.LoadText("CSharpProject_DuplicatedGuidLibrary1.csproj")),
                (@"Library2\Library2.csproj", Resources.LoadText("CSharpProject_DuplicatedGuidLibrary2.csproj")));
        }

        protected FileSet GetSolutionWithCircularProjectReferences()
        {
            return new FileSet(
                (@"Directory.Build.props", Resources.LoadText("Directory.Build.props")),
                (@"Directory.Build.targets", Resources.LoadText("Directory.Build.targets")),
                (@"CircularSolution.sln", Resources.LoadText("CircularProjectReferences.CircularSolution.sln")),
                (@"CircularCSharpProject1.csproj", Resources.LoadText("CircularProjectReferences.CircularCSharpProject1.csproj")),
                (@"CircularCSharpProject2.csproj", Resources.LoadText("CircularProjectReferences.CircularCSharpProject2.csproj")));
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
