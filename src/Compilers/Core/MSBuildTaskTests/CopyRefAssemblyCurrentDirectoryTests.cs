// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.CodeAnalysis.BuildTasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.BuildTasks.UnitTests
{
    /// <summary>
    /// Verifies that after the multithreaded-task migration, <see cref="CopyRefAssembly"/> resolves relative
    /// <see cref="CopyRefAssembly.SourcePath"/> / <see cref="CopyRefAssembly.DestinationPath"/> values against the
    /// task's <see cref="TaskEnvironment.ProjectDirectory"/> instead of the process current working directory, and
    /// keeps the original (relative) paths in its log output.
    /// </summary>
    /// <remarks>
    /// This test mutates the process-global current working directory, so it is pinned to a non-parallel
    /// collection to avoid flaking sibling tests that also depend on the current directory.
    /// </remarks>
    [CollectionDefinition(nameof(CopyRefAssemblyCurrentDirectoryTests), DisableParallelization = true)]
    [Collection(nameof(CopyRefAssemblyCurrentDirectoryTests))]
    public sealed class CopyRefAssemblyCurrentDirectoryTests : IDisposable
    {
        private readonly TempRoot _tempRoot = new TempRoot();
        private readonly string _originalWorkingDirectory = Directory.GetCurrentDirectory();

        public void Dispose()
        {
            Directory.SetCurrentDirectory(_originalWorkingDirectory);
            _tempRoot.Dispose();
        }

        [ConditionalFact(typeof(IsEnglishLocal))]
        public void RelativePaths_LogUsesRelativePath_NotAbsolutePath()
        {
            var projectDir = _tempRoot.CreateDirectory();
            var decoyCurrentDirectory = _tempRoot.CreateDirectory();
            File.WriteAllText(Path.Combine(projectDir.Path, "example.dll"), "test");

            Directory.SetCurrentDirectory(decoyCurrentDirectory.Path);

            var engine = new MockEngine();
            var task = new CopyRefAssembly()
            {
                BuildEngine = engine,
                TaskEnvironment = TaskEnvironment.CreateWithProjectDirectoryAndEnvironment(projectDir.Path),
                SourcePath = "example.dll",
                DestinationPath = "dest.dll",
            };

            Assert.True(task.Execute());

            AssertEx.AssertEqualToleratingWhitespaceDifferences(
                """Copying reference assembly from "example.dll" to "dest.dll".""",
                engine.Log);
            Assert.DoesNotContain(projectDir.Path, engine.Log);
        }
    }
}
