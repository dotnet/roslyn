// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public partial class CommandLineProjectTests : TestBase
    {
        [Fact]
        public void TestCreateWithoutRequiredServices()
        {
            string commandLine = @"foo.cs";

            Assert.Throws<InvalidOperationException>(delegate
            {
                var ws = new AdhocWorkspace(); // only includes portable services
                var info = CommandLineProject.CreateProjectInfo("TestProject", LanguageNames.CSharp, commandLine, @"C:\ProjectDirectory", ws);
            });
        }

        [Fact]
        public void TestCreateWithRequiredServices()
        {
            string commandLine = @"foo.cs";
            var ws = new AdhocWorkspace(DesktopMefHostServices.DefaultServices); // includes non-portable services too
            var info = CommandLineProject.CreateProjectInfo("TestProject", LanguageNames.CSharp, commandLine, @"C:\ProjectDirectory", ws);
        }

        [Fact]
        public void TestUnrootedPathInsideProjectCone()
        {
            string commandLine = @"foo.cs";
            var info = CommandLineProject.CreateProjectInfo("TestProject", LanguageNames.CSharp, commandLine, @"C:\ProjectDirectory");

            var docInfo = info.Documents.First();
            Assert.Equal(0, docInfo.Folders.Count);
            Assert.Equal("foo.cs", docInfo.Name);
        }

        [Fact]
        public void TestUnrootedSubPathInsideProjectCone()
        {
            string commandLine = @"subdir\foo.cs";
            var info = CommandLineProject.CreateProjectInfo("TestProject", LanguageNames.CSharp, commandLine, @"C:\ProjectDirectory");

            var docInfo = info.Documents.First();
            Assert.Equal(1, docInfo.Folders.Count);
            Assert.Equal("subdir", docInfo.Folders[0]);
            Assert.Equal("foo.cs", docInfo.Name);
        }

        [Fact]
        public void TestRootedPathInsideProjectCone()
        {
            string commandLine = @"c:\ProjectDirectory\foo.cs";
            var info = CommandLineProject.CreateProjectInfo("TestProject", LanguageNames.CSharp, commandLine, @"C:\ProjectDirectory");

            var docInfo = info.Documents.First();
            Assert.Equal(0, docInfo.Folders.Count);
            Assert.Equal("foo.cs", docInfo.Name);
        }

        [Fact]
        public void TestRootedSubPathInsideProjectCone()
        {
            string commandLine = @"c:\projectDirectory\subdir\foo.cs";
            var info = CommandLineProject.CreateProjectInfo("TestProject", LanguageNames.CSharp, commandLine, @"C:\ProjectDirectory");

            var docInfo = info.Documents.First();
            Assert.Equal(1, docInfo.Folders.Count);
            Assert.Equal("subdir", docInfo.Folders[0]);
            Assert.Equal("foo.cs", docInfo.Name);
        }

        [Fact]
        public void TestRootedPathOutsideProjectCone()
        {
            string commandLine = @"C:\SomeDirectory\foo.cs";
            var info = CommandLineProject.CreateProjectInfo("TestProject", LanguageNames.CSharp, commandLine, @"C:\ProjectDirectory");

            var docInfo = info.Documents.First();
            Assert.Equal(0, docInfo.Folders.Count);
            Assert.Equal("foo.cs", docInfo.Name);
        }

        [Fact]
        public void TestUnrootedPathOutsideProjectCone()
        {
            string commandLine = @"..\foo.cs";
            var info = CommandLineProject.CreateProjectInfo("TestProject", LanguageNames.CSharp, commandLine, @"C:\ProjectDirectory");

            var docInfo = info.Documents.First();
            Assert.Equal(0, docInfo.Folders.Count);
            Assert.Equal("foo.cs", docInfo.Name);
        }

        [Fact]
        public void TestAdditionalFiles()
        {
            string commandLine = @"foo.cs /additionalfile:bar.cs";
            var info = CommandLineProject.CreateProjectInfo("TestProject", LanguageNames.CSharp, commandLine, @"C:\ProjectDirectory");

            var firstDoc = info.Documents.Single();
            var secondDoc = info.AdditionalDocuments.Single();
            Assert.Equal("foo.cs", firstDoc.Name);
            Assert.Equal("bar.cs", secondDoc.Name);
        }
    }
}
