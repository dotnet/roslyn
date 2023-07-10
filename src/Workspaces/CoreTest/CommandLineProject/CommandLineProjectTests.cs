// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
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
            var commandLine = @"..\goo.cs";
            var info = CommandLineProject.CreateProjectInfo("TestProject", LanguageNames.CSharp, commandLine, @"C:\ProjectDirectory");

            var docInfo = info.Documents.First();
            Assert.Equal(0, docInfo.Folders.Count);
            Assert.Equal("goo.cs", docInfo.Name);
        }

        [Fact]
        public void TestCreateWithoutRequiredServices()
        {
            var commandLine = @"goo.cs";

            Assert.Throws<InvalidOperationException>(delegate
            {
                var ws = new AdhocWorkspace(new MefHostServices(new ContainerConfiguration().CreateContainer())); // no services
                var info = CommandLineProject.CreateProjectInfo("TestProject", LanguageNames.CSharp, commandLine, @"C:\ProjectDirectory", ws);
            });
        }

        [Fact]
        public void TestCreateWithRequiredServices()
        {
            var commandLine = @"goo.cs";
            var ws = new AdhocWorkspace();
            _ = CommandLineProject.CreateProjectInfo("TestProject", LanguageNames.CSharp, commandLine, @"C:\ProjectDirectory", ws);
        }

        [Fact]
        public void TestUnrootedPathInsideProjectCone()
        {
            var commandLine = @"goo.cs";
            var info = CommandLineProject.CreateProjectInfo("TestProject", LanguageNames.CSharp, commandLine, @"C:\ProjectDirectory");

            var docInfo = info.Documents.First();
            Assert.Equal(0, docInfo.Folders.Count);
            Assert.Equal("goo.cs", docInfo.Name);
        }

        [Fact]
        public void TestUnrootedSubPathInsideProjectCone()
        {
            var commandLine = @"subdir\goo.cs";
            var info = CommandLineProject.CreateProjectInfo("TestProject", LanguageNames.CSharp, commandLine, @"C:\ProjectDirectory");

            var docInfo = info.Documents.First();
            Assert.Equal(1, docInfo.Folders.Count);
            Assert.Equal("subdir", docInfo.Folders[0]);
            Assert.Equal("goo.cs", docInfo.Name);
        }

        [Fact]
        public void TestRootedPathInsideProjectCone()
        {
            var commandLine = @"c:\ProjectDirectory\goo.cs";
            var info = CommandLineProject.CreateProjectInfo("TestProject", LanguageNames.CSharp, commandLine, @"C:\ProjectDirectory");

            var docInfo = info.Documents.First();
            Assert.Equal(0, docInfo.Folders.Count);
            Assert.Equal("goo.cs", docInfo.Name);
        }

        [Fact]
        public void TestRootedSubPathInsideProjectCone()
        {
            var commandLine = @"c:\projectDirectory\subdir\goo.cs";
            var info = CommandLineProject.CreateProjectInfo("TestProject", LanguageNames.CSharp, commandLine, @"C:\ProjectDirectory");

            var docInfo = info.Documents.First();
            Assert.Equal(1, docInfo.Folders.Count);
            Assert.Equal("subdir", docInfo.Folders[0]);
            Assert.Equal("goo.cs", docInfo.Name);
        }

        [Fact]
        public void TestRootedPathOutsideProjectCone()
        {
            var commandLine = @"C:\SomeDirectory\goo.cs";
            var info = CommandLineProject.CreateProjectInfo("TestProject", LanguageNames.CSharp, commandLine, @"C:\ProjectDirectory");

            var docInfo = info.Documents.First();
            Assert.Equal(0, docInfo.Folders.Count);
            Assert.Equal("goo.cs", docInfo.Name);
        }

        [Fact]
        public void TestUnrootedPathOutsideProjectCone()
        {
            var commandLine = @"..\goo.cs";
            var info = CommandLineProject.CreateProjectInfo("TestProject", LanguageNames.CSharp, commandLine, @"C:\ProjectDirectory");

            var docInfo = info.Documents.First();
            Assert.Equal(0, docInfo.Folders.Count);
            Assert.Equal("goo.cs", docInfo.Name);
        }

        [Fact]
        public void TestAdditionalFiles()
        {
            var commandLine = @"goo.cs /additionalfile:bar.cs";
            var info = CommandLineProject.CreateProjectInfo("TestProject", LanguageNames.CSharp, commandLine, @"C:\ProjectDirectory");

            var firstDoc = info.Documents.Single();
            var secondDoc = info.AdditionalDocuments.Single();
            Assert.Equal("goo.cs", firstDoc.Name);
            Assert.Equal("bar.cs", secondDoc.Name);
        }

        [Fact]
        public void TestAnalyzerConfigFiles()
        {
            var commandLine = @"/analyzerconfig:.editorconfig";
            var info = CommandLineProject.CreateProjectInfo("TestProject", LanguageNames.CSharp, commandLine, @"C:\ProjectDirectory");

            var document = Assert.Single(info.AnalyzerConfigDocuments);
            Assert.Equal(".editorconfig", document.Name);
            Assert.Equal(Path.Combine(@"C:\ProjectDirectory", ".editorconfig"), document.FilePath);
        }

        [Fact]
        public void TestAnalyzerReferences()
        {
            var pathToAssembly = typeof(object).Assembly.Location;
            var assemblyBaseDir = Path.GetDirectoryName(pathToAssembly);
            var relativePath = Path.Combine(".", Path.GetFileName(pathToAssembly));
            var commandLine = @"goo.cs /a:" + relativePath;
            var info = CommandLineProject.CreateProjectInfo("TestProject", LanguageNames.CSharp, commandLine, assemblyBaseDir);

            var firstDoc = info.Documents.Single();
            var analyzerRef = info.AnalyzerReferences.Single();
            Assert.Equal("goo.cs", firstDoc.Name);
            Assert.Equal(pathToAssembly, analyzerRef.FullPath);
        }

        [Fact]
        public void TestDuplicateAnalyzerReferences()
        {
            var pathToAssembly = typeof(object).Assembly.Location;
            var assemblyBaseDir = Path.GetDirectoryName(pathToAssembly);
            var relativePath = Path.Combine(".", Path.GetFileName(pathToAssembly));
            var commandLine = $@"goo.cs /a:{relativePath} /a:{relativePath}";
            var info = CommandLineProject.CreateProjectInfo("TestProject", LanguageNames.CSharp, commandLine, assemblyBaseDir);

            var analyzerRef = info.AnalyzerReferences.Single();
            Assert.Equal(pathToAssembly, analyzerRef.FullPath);
        }

        [Fact]
        public void TestDuplicateReferenceInVisualBasic()
        {
            var pathToAssembly = typeof(object).Assembly.Location;
            var quotedPathToAssembly = '"' + pathToAssembly + '"';
            var commandLine = $"goo.vb /r:{quotedPathToAssembly},{quotedPathToAssembly}";
            var info = CommandLineProject.CreateProjectInfo("TestProject", LanguageNames.VisualBasic, commandLine, baseDirectory: @"C:\ProjectDirectory");

            // The compiler may add other references automatically, so we'll only assert a single reference for the one we're interested in
            Assert.Single(info.MetadataReferences.OfType<PortableExecutableReference>(), r => r.FilePath == pathToAssembly);
        }

        [Fact]
        public void TestDuplicateReferenceInVisualBasicWithVbRuntimeFlag()
        {
            var pathToAssembly = typeof(object).Assembly.Location;
            var quotedPathToAssembly = '"' + pathToAssembly + '"';
            var commandLine = $"goo.vb /r:{quotedPathToAssembly} /vbruntime:{quotedPathToAssembly}";
            var info = CommandLineProject.CreateProjectInfo("TestProject", LanguageNames.VisualBasic, commandLine, baseDirectory: @"C:\ProjectDirectory");

            // The compiler may add other references automatically, so we'll only assert a single reference for the one we're interested in
            Assert.Single(info.MetadataReferences.OfType<PortableExecutableReference>(), r => r.FilePath == pathToAssembly);
        }
    }
}
