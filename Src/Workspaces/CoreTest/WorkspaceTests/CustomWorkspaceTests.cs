// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UnitTests.SolutionGeneration;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using CS = Microsoft.CodeAnalysis.CSharp;
using VB = Microsoft.CodeAnalysis.VisualBasic;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public partial class CustomWorkspaceTests : WorkspaceTestBase
    {
        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestAddProject_FromCommandLineArgs()
        {
            CreateFiles(GetSimpleCSharpSolutionFiles());

            string commandLine = @"CSharpClass.cs /out:foo.dll /target:library";
            var baseDirectory = Path.Combine(this.SolutionDirectory.Path, "CSharpProject");

            using (var ws = new CustomWorkspace())
            {
                var info = CommandLineProject.CreateProjectInfo(ws, "TestProject", LanguageNames.CSharp, commandLine, baseDirectory);
                ws.AddProject(info);
                var project = ws.CurrentSolution.GetProject(info.Id);

                Assert.Equal("TestProject", project.Name);
                Assert.Equal("foo", project.AssemblyName);
                Assert.Equal(OutputKind.DynamicallyLinkedLibrary, project.CompilationOptions.OutputKind);

                Assert.Equal(1, project.Documents.Count());

                var fooDoc = project.Documents.First(d => d.Name == "CSharpClass.cs");
                Assert.Equal(0, fooDoc.Folders.Count);
                var expectedPath = Path.Combine(baseDirectory, "CSharpClass.cs");
                Assert.Equal(expectedPath, fooDoc.FilePath);

                var text = fooDoc.GetTextAsync().Result.ToString();
                Assert.NotEqual("", text);

                var tree = fooDoc.GetSyntaxRootAsync().Result;
                Assert.Equal(false, tree.ContainsDiagnostics);

                var compilation = project.GetCompilationAsync().Result;
            }
        }
    }
}