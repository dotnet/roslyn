// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.CodeAnalysis.Test.Utilities;
using Moq;
using Xunit;

namespace Microsoft.CodeAnalysis.BuildTasks.UnitTests
{
    public class DiscoverEditorConfigFilesTests : IDisposable
    {
        private readonly TempRoot _tempRoot;
        private readonly TempDirectory _tempDirectory;

        public DiscoverEditorConfigFilesTests()
        {
            _tempRoot = new TempRoot();
            _tempDirectory = _tempRoot.CreateDirectory();
        }

        public void Dispose()
        {
            _tempRoot.Dispose();
        }

        [Fact]
        public void NoInputsGivesNoOutputs()
        {
            var task = new DiscoverEditorConfigFiles();
            task.InputFiles = Array.Empty<ITaskItem>();
            Assert.True(task.Execute());
            Assert.Empty(task.EditorConfigFiles);
        }

        [Fact]
        public void EditorConfigInSameDirectoryIsFound()
        {
            var sourceFile = _tempDirectory.CreateFile("Cat.cs");
            var editorConfigFile = _tempDirectory.CreateFile(".editorconfig").WriteAllText("root = true");

            var task = new DiscoverEditorConfigFiles();
            task.InputFiles = MSBuildUtil.CreateTaskItems(sourceFile.Path);
            Assert.True(task.Execute());
            Assert.Equal(editorConfigFile.Path, Assert.Single(task.EditorConfigFiles).ItemSpec);
        }

        [Fact]
        public void EditorConfigInParentDirectoryIsFound()
        {
            var sourceFile = _tempDirectory.CreateDirectory("Subdirectory").CreateFile("Cat.cs");
            var editorConfigFile = _tempDirectory.CreateFile(".editorconfig").WriteAllText("root = true");

            var task = new DiscoverEditorConfigFiles();
            task.InputFiles = MSBuildUtil.CreateTaskItems(sourceFile.Path);
            Assert.True(task.Execute());
            Assert.Equal(editorConfigFile.Path, Assert.Single(task.EditorConfigFiles).ItemSpec);
        }

        [Fact]
        public void EditorConfigInParentDirectoryIsFoundWithMultipleInputs()
        {
            var sourceFile1 = _tempDirectory.CreateDirectory("Subdirectory").CreateFile("Cat.cs");
            var sourceFile2 = _tempDirectory.CreateFile("Dog.cs");
            var editorConfigFile = _tempDirectory.CreateFile(".editorconfig").WriteAllText("root = true");

            var task = new DiscoverEditorConfigFiles();
            task.InputFiles = MSBuildUtil.CreateTaskItems(sourceFile1.Path, sourceFile2.Path);
            Assert.True(task.Execute());
            Assert.Equal(editorConfigFile.Path, Assert.Single(task.EditorConfigFiles).ItemSpec);
        }

        [Fact]
        public void EditorConfigInParentDirectoryIsNotFoundIfChildHasRoot()
        {
            var subdirectory = _tempDirectory.CreateDirectory("Subdirectory");
            var sourceFile = subdirectory.CreateFile("Cat.cs");
            var editorConfigFileChild = subdirectory.CreateFile(".editorconfig").WriteAllText("root = true");
            var editorConfigFileParent = _tempDirectory.CreateFile(".editorconfig").WriteAllText("root = true");

            var task = new DiscoverEditorConfigFiles();
            task.InputFiles = MSBuildUtil.CreateTaskItems(sourceFile.Path);
            Assert.True(task.Execute());
            Assert.Equal(editorConfigFileChild.Path, Assert.Single(task.EditorConfigFiles).ItemSpec);
        }

        [Fact]
        public void EditorConfigPreservesCaseDuringSearch()
        {
            // We want to validate that the lookup preserves case sensitivity of file paths. To do this, we'll create
            // two directory paths with names that differ by case, and then pass both files to them. We'll call CreateDirectory
            // with both casings, so on case-sensitive file systems we'll have two actual directories and on case-insensitive
            // ones we'll actually only have one, but we'll preserve in both cases.
            var subdirectory1 = _tempDirectory.CreateDirectory("Subdirectory");
            var subdirectory2 = _tempDirectory.CreateDirectory("SubDirectory");
            var editorConfigFile1 = subdirectory1.CreateFile(".editorconfig").WriteAllText("root = true");
            var editorConfigFile2 = subdirectory2.CreateOrOpenFile(".editorconfig").WriteAllText("root = true");

            var sourceFile1 = Path.Combine(subdirectory1.Path, "Cat.cs");
            var sourceFile2 = Path.Combine(subdirectory2.Path, "Cat.cs");

            Assert.NotEqual(editorConfigFile1, editorConfigFile2);
            Assert.NotEqual(sourceFile1, sourceFile2);

            var task = new DiscoverEditorConfigFiles();
            task.InputFiles = MSBuildUtil.CreateTaskItems(sourceFile1, sourceFile2);
            Assert.True(task.Execute());

            var paths = task.EditorConfigFiles.Select(i => i.ItemSpec);
            Assert.Contains(editorConfigFile1.Path, paths);
            Assert.Contains(editorConfigFile2.Path, paths);
        }

        [Fact]
        public void EditorConfigMultipleFilesAreOrderedCorrectly()
        {
            var subdirectory = _tempDirectory.CreateDirectory("Subdirectory");
            var editorConfigFileParent = _tempDirectory.CreateFile(".editorconfig").WriteAllText("root = true");
            var editorConfigFileChild = subdirectory.CreateFile(".editorconfig");

            var sourceFile = Path.Combine(subdirectory.Path, "Cat.cs");

            var task = new DiscoverEditorConfigFiles();
            task.InputFiles = MSBuildUtil.CreateTaskItems(sourceFile);
            Assert.True(task.Execute());

            var paths = task.EditorConfigFiles.Select(i => i.ItemSpec);
            Assert.Equal(new[] { editorConfigFileChild.Path, editorConfigFileParent.Path }, paths);
        }

        [Fact]
        public void LockedEditorConfigHandledGracefully()
        {
            var editorConfigFile = _tempDirectory.CreateFile(".editorconfig");
            var sourceFile = Path.Combine(_tempDirectory.Path, "Cat.cs");

            using (var stream = new FileStream(editorConfigFile.Path, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                var task = new DiscoverEditorConfigFiles();
                var buildEngine = new MockEngine();
                task.BuildEngine = buildEngine;

                task.InputFiles = MSBuildUtil.CreateTaskItems(sourceFile);
                Assert.False(task.Execute());

                Assert.Contains(editorConfigFile.Path, buildEngine.Log);
            }
        }

        [Fact]
        public void EmptyEditorConfigFileIsRoot()
        {
            var tempFile = _tempDirectory.CreateFile(".editorconfig").WriteAllText("");
            Assert.False(DiscoverEditorConfigFiles.FileIsRootEditorConfig(tempFile.Path));
        }

        [Fact]
        public void RootEditorConfigFileIsRoot()
        {
            var tempFile = _tempDirectory.CreateFile(".editorconfig").WriteAllText("root = true");
            Assert.True(DiscoverEditorConfigFiles.FileIsRootEditorConfig(tempFile.Path));
        }

        [Fact]
        public void RootEditorConfigFileIsRootColon()
        {
            var tempFile = _tempDirectory.CreateFile(".editorconfig").WriteAllText("root : true");
            Assert.True(DiscoverEditorConfigFiles.FileIsRootEditorConfig(tempFile.Path));
        }

        [Fact]
        public void RootEditorConfigFileIsRootNoSpace()
        {
            var tempFile = _tempDirectory.CreateFile(".editorconfig").WriteAllText("root=true");
            Assert.True(DiscoverEditorConfigFiles.FileIsRootEditorConfig(tempFile.Path));
        }

        [Fact]
        public void RootEditorConfigFileIsRootUnicodeWhitespace()
        {
            var tempFile = _tempDirectory.CreateFile(".editorconfig").WriteAllText("root\t= \u00a0 true");
            Assert.True(DiscoverEditorConfigFiles.FileIsRootEditorConfig(tempFile.Path));
        }

        [Fact]
        public void MixCaseRootEditorConfigFileIsRoot()
        {
            var tempFile = _tempDirectory.CreateFile(".editorconfig").WriteAllText("RoOt = TrUE");
            Assert.True(DiscoverEditorConfigFiles.FileIsRootEditorConfig(tempFile.Path));
        }

        [Fact]
        public void RootEditorConfigFileIsRootCommentAfter()
        {
            var tempFile = _tempDirectory.CreateFile(".editorconfig").WriteAllText(" root = true # comment");
            Assert.True(DiscoverEditorConfigFiles.FileIsRootEditorConfig(tempFile.Path));
        }

        [Fact]
        public void RootEditorConfigFileIsRootCommentAfter2()
        {
            var tempFile = _tempDirectory.CreateFile(".editorconfig").WriteAllText(" root = true ; comment");
            Assert.True(DiscoverEditorConfigFiles.FileIsRootEditorConfig(tempFile.Path));
        }

        [Fact]
        public void NonRootEditorConfigFileIsNotRoot()
        {
            var tempFile = _tempDirectory.CreateFile(".editorconfig").WriteAllText("root = false");
            Assert.False(DiscoverEditorConfigFiles.FileIsRootEditorConfig(tempFile.Path));
        }

        [Fact]
        public void NonRootEditorConfigFileIsNotRootMisspell()
        {
            var tempFile = _tempDirectory.CreateFile(".editorconfig").WriteAllText("roots = true");
            Assert.False(DiscoverEditorConfigFiles.FileIsRootEditorConfig(tempFile.Path));
        }

        [Fact]
        public void NonRootEditorConfigFileIsNotRootMisspell2()
        {
            var tempFile = _tempDirectory.CreateFile(".editorconfig").WriteAllText("root = tru");
            Assert.False(DiscoverEditorConfigFiles.FileIsRootEditorConfig(tempFile.Path));
        }

        [Fact]
        public void RootPropertyInSectionIsNotActualRoot()
        {
            var tempFile = _tempDirectory.CreateFile(".editorconfig").WriteAllText("[*]\r\nroot = true");
            Assert.False(DiscoverEditorConfigFiles.FileIsRootEditorConfig(tempFile.Path));
        }
    }
}
