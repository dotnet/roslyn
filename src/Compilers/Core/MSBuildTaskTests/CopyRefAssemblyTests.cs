// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.CodeAnalysis.BuildTasks;
using Xunit;
using Moq;
using System.IO;
using Roslyn.Test.Utilities;
using Microsoft.CodeAnalysis.BuildTasks.UnitTests.TestUtilities;
using Microsoft.CodeAnalysis.Test.Utilities;

namespace Microsoft.CodeAnalysis.BuildTasks.UnitTests
{
    public sealed class CopyRefAssemblyTests : IDisposable
    {
        public TempRoot TempRoot { get; } = new TempRoot();

        public void Dispose()
        {
            TempRoot.Dispose();
        }

        [Fact]
        public void SourceDoesNotExist()
        {
            var dir = TempRoot.CreateDirectory();
            var engine = new MockEngine();
            var task = new CopyRefAssembly()
            {
                BuildEngine = engine,
                SourcePath = Path.Combine(dir.Path, "does_not_exist.dll")
            };

            Assert.False(task.Execute());
            Assert.False(string.IsNullOrEmpty(engine.Log));
        }

        [Fact]
        public void BadDestinationPath()
        {
            var dir = TempRoot.CreateDirectory();
            var file = dir.CreateFile("example.dll");
            File.WriteAllText(file.Path, "");
            var engine = new MockEngine();
            var task = new CopyRefAssembly()
            {
                BuildEngine = engine,
                SourcePath = file.Path,
                DestinationPath = null!,
            };

            Assert.False(task.Execute());
            Assert.False(string.IsNullOrEmpty(engine.Log));
        }

        [ConditionalFact(typeof(IsEnglishLocal))]
        public void SourceNotAssemblyNoDestination()
        {
            var dir = TempRoot.CreateDirectory();
            var file = dir.CreateFile("example.dll");
            File.WriteAllText(file.Path, "test");
            var dest = Path.Combine(dir.Path, "dest.dll");
            var engine = new MockEngine();
            var task = new CopyRefAssembly()
            {
                BuildEngine = engine,
                SourcePath = file.Path,
                DestinationPath = dest,
            };

            Assert.True(task.Execute());
            AssertEx.AssertEqualToleratingWhitespaceDifferences($$"""Copying reference assembly from "{{file.Path}}" to "{{dest}}".""", engine.Log);
            Assert.Equal("test", File.ReadAllText(dest));
        }

        [ConditionalFact(typeof(IsEnglishLocal))]
        public void SourceNotAssemblyWithDestination()
        {
            var dir = TempRoot.CreateDirectory();
            var source = dir.CreateFile("example.dll");
            File.WriteAllText(source.Path, "test");
            var dest = dir.CreateFile("dest.dll");
            File.WriteAllText(dest.Path, "dest");
            var engine = new MockEngine();
            var task = new CopyRefAssembly()
            {
                BuildEngine = engine,
                SourcePath = source.Path,
                DestinationPath = dest.Path,
            };

            Assert.True(task.Execute());

            AssertEx.AssertEqualToleratingWhitespaceDifferences($$"""
                Could not extract the MVID from "{{source.Path}}". Are you sure it is a reference assembly?
                Copying reference assembly from "{{source.Path}}" to "{{dest}}".
                """,
                engine.Log);

            Assert.Equal("test", File.ReadAllText(dest.Path));
        }

        [ConditionalFact(typeof(IsEnglishLocal))]
        public void SourceAssemblyWithDifferentDestinationAssembly()
        {
            var dir = TempRoot.CreateDirectory();
            var source = dir.CreateFile("mvid1.dll");
            File.WriteAllBytes(source.Path, TestResources.General.MVID1);
            var sourceTimestamp = File.GetLastWriteTimeUtc(source.Path).ToString("O");

            var dest = dir.CreateFile("mvid2.dll");
            File.WriteAllBytes(dest.Path, TestResources.General.MVID2);
            var destTimestamp = File.GetLastWriteTimeUtc(dest.Path).ToString("O");

            var engine = new MockEngine();
            var task = new CopyRefAssembly()
            {
                BuildEngine = engine,
                SourcePath = source.Path,
                DestinationPath = dest.Path,
            };

            Assert.True(task.Execute());

            AssertEx.AssertEqualToleratingWhitespaceDifferences($$"""
                Source reference assembly "{{source.Path}}" (timestamp "{{sourceTimestamp}}", MVID "f851dda2-6ea3-475e-8c0d-19bd3c4d9437") differs from destination "{{dest.Path}}" (timestamp "{{destTimestamp}}", MVID "8e1ed25b-2980-4f32-9dee-c1e3b0a57c4b").
                Copying reference assembly from "{{source.Path}}" to "{{dest.Path}}".
                """,
                engine.Log);
        }
    }
}
