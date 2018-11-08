// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Composition.Hosting;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    [UseExportProvider]
    public class CommandLineProjectTests : TestBase
    {
        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestCommandLineProjectWithRelativePathOutsideProjectCone()
        {
            string commandLine = @"..\goo.cs";
            var info = CommandLineProject.CreateProjectInfo("TestProject", LanguageNames.CSharp, commandLine, @"C:\ProjectDirectory");

            var docInfo = info.Documents.First();
            Assert.Equal(0, docInfo.Folders.Count);
            Assert.Equal("goo.cs", docInfo.Name);
        }

        [Fact]
        public void TestCreateWithoutRequiredServices()
        {
            string commandLine = @"goo.cs";

            Assert.Throws<InvalidOperationException>(delegate
            {
                var ws = new AdhocWorkspace(new MefHostServices(new ContainerConfiguration().CreateContainer())); // no services
                var info = CommandLineProject.CreateProjectInfo("TestProject", LanguageNames.CSharp, commandLine, @"C:\ProjectDirectory", ws);
            });
        }

        [Fact]
        public void TestCreateWithRequiredServices()
        {
            string commandLine = @"goo.cs";
            var ws = new AdhocWorkspace(DesktopMefHostServices.DefaultServices); // includes non-portable services too
            var info = CommandLineProject.CreateProjectInfo("TestProject", LanguageNames.CSharp, commandLine, @"C:\ProjectDirectory", ws);
        }

        [Fact]
        public void TestUnrootedPathInsideProjectCone()
        {
            string commandLine = @"goo.cs";
            var info = CommandLineProject.CreateProjectInfo("TestProject", LanguageNames.CSharp, commandLine, @"C:\ProjectDirectory");

            var docInfo = info.Documents.First();
            Assert.Equal(0, docInfo.Folders.Count);
            Assert.Equal("goo.cs", docInfo.Name);
        }

        [Fact]
        public void TestUnrootedSubPathInsideProjectCone()
        {
            string commandLine = @"subdir\goo.cs";
            var info = CommandLineProject.CreateProjectInfo("TestProject", LanguageNames.CSharp, commandLine, @"C:\ProjectDirectory");

            var docInfo = info.Documents.First();
            Assert.Equal(1, docInfo.Folders.Count);
            Assert.Equal("subdir", docInfo.Folders[0]);
            Assert.Equal("goo.cs", docInfo.Name);
        }

        [Fact]
        public void TestRootedPathInsideProjectCone()
        {
            string commandLine = @"c:\ProjectDirectory\goo.cs";
            var info = CommandLineProject.CreateProjectInfo("TestProject", LanguageNames.CSharp, commandLine, @"C:\ProjectDirectory");

            var docInfo = info.Documents.First();
            Assert.Equal(0, docInfo.Folders.Count);
            Assert.Equal("goo.cs", docInfo.Name);
        }

        [Fact]
        public void TestRootedSubPathInsideProjectCone()
        {
            string commandLine = @"c:\projectDirectory\subdir\goo.cs";
            var info = CommandLineProject.CreateProjectInfo("TestProject", LanguageNames.CSharp, commandLine, @"C:\ProjectDirectory");

            var docInfo = info.Documents.First();
            Assert.Equal(1, docInfo.Folders.Count);
            Assert.Equal("subdir", docInfo.Folders[0]);
            Assert.Equal("goo.cs", docInfo.Name);
        }

        [Fact]
        public void TestRootedPathOutsideProjectCone()
        {
            string commandLine = @"C:\SomeDirectory\goo.cs";
            var info = CommandLineProject.CreateProjectInfo("TestProject", LanguageNames.CSharp, commandLine, @"C:\ProjectDirectory");

            var docInfo = info.Documents.First();
            Assert.Equal(0, docInfo.Folders.Count);
            Assert.Equal("goo.cs", docInfo.Name);
        }

        [Fact]
        public void TestUnrootedPathOutsideProjectCone()
        {
            string commandLine = @"..\goo.cs";
            var info = CommandLineProject.CreateProjectInfo("TestProject", LanguageNames.CSharp, commandLine, @"C:\ProjectDirectory");

            var docInfo = info.Documents.First();
            Assert.Equal(0, docInfo.Folders.Count);
            Assert.Equal("goo.cs", docInfo.Name);
        }

        [Fact]
        public void TestAdditionalFiles()
        {
            string commandLine = @"goo.cs /additionalfile:bar.cs";
            var info = CommandLineProject.CreateProjectInfo("TestProject", LanguageNames.CSharp, commandLine, @"C:\ProjectDirectory");

            var firstDoc = info.Documents.Single();
            var secondDoc = info.AdditionalDocuments.Single();
            Assert.Equal("goo.cs", firstDoc.Name);
            Assert.Equal("bar.cs", secondDoc.Name);
        }

        [Fact]
        public void TestAnalyzerReferences()
        {
            var pathToAssembly = typeof(object).Assembly.Location;
            var assemblyBaseDir = Path.GetDirectoryName(pathToAssembly);
            var relativePath = Path.Combine(".", Path.GetFileName(pathToAssembly));
            string commandLine = @"goo.cs /a:" + relativePath;
            var info = CommandLineProject.CreateProjectInfo("TestProject", LanguageNames.CSharp, commandLine, assemblyBaseDir);

            var firstDoc = info.Documents.Single();
            var analyzerRef = info.AnalyzerReferences.First();
            Assert.Equal("goo.cs", firstDoc.Name);
            Assert.Equal(pathToAssembly, analyzerRef.FullPath);
        }
    }
}
