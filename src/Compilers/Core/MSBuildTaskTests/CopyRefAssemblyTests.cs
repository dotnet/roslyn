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

            // Ensure different timestamps to test MVID checking path
            var destTimestamp = File.GetLastWriteTimeUtc(dest.Path);
            File.SetLastWriteTimeUtc(source.Path, destTimestamp.AddSeconds(1));

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

            var dest = dir.CreateFile("mvid2.dll");
            File.WriteAllBytes(dest.Path, TestResources.General.MVID2);

            // Ensure different timestamps so size/timestamp check doesn't incorrectly short-circuit
            // (MVID1 and MVID2 have the same size)
            var destTime = File.GetLastWriteTimeUtc(dest.Path);
            File.SetLastWriteTimeUtc(source.Path, destTime.AddSeconds(1));

            var sourceTimestamp = File.GetLastWriteTimeUtc(source.Path).ToString("O");
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

        [ConditionalFact(typeof(IsEnglishLocal))]
        public void SourceAndDestinationWithSameSizeAndTimestamp()
        {
            var dir = TempRoot.CreateDirectory();
            var source = dir.CreateFile("mvid1.dll");
            File.WriteAllBytes(source.Path, TestResources.General.MVID1);

            var dest = dir.CreateFile("dest.dll");
            File.WriteAllBytes(dest.Path, TestResources.General.MVID1);

            // Set the destination to have the same timestamp as the source
            File.SetLastWriteTimeUtc(dest.Path, File.GetLastWriteTimeUtc(source.Path));

            var engine = new MockEngine();
            var task = new CopyRefAssembly()
            {
                BuildEngine = engine,
                SourcePath = source.Path,
                DestinationPath = dest.Path,
            };

            Assert.True(task.Execute());

            // Should skip copy due to matching size and timestamp (fast path optimization)
            AssertEx.AssertEqualToleratingWhitespaceDifferences($$"""
                Reference assembly "{{dest.Path}}" already has latest information. Leaving it untouched.
                """,
                engine.Log);
        }

        [ConditionalFact(typeof(IsEnglishLocal))]
        public void SourceAndDestinationWithSameMvidButDifferentTimestamp()
        {
            var dir = TempRoot.CreateDirectory();
            var source = dir.CreateFile("mvid1.dll");
            File.WriteAllBytes(source.Path, TestResources.General.MVID1);

            var dest = dir.CreateFile("dest.dll");
            File.WriteAllBytes(dest.Path, TestResources.General.MVID1);

            // Ensure different timestamps so size/timestamp check doesn't short-circuit
            var destTimestamp = File.GetLastWriteTimeUtc(dest.Path);
            File.SetLastWriteTimeUtc(source.Path, destTimestamp.AddSeconds(1));

            var engine = new MockEngine();
            var task = new CopyRefAssembly()
            {
                BuildEngine = engine,
                SourcePath = source.Path,
                DestinationPath = dest.Path,
            };

            Assert.True(task.Execute());

            // Should skip copy due to matching MVID (falls through to MVID check)
            AssertEx.AssertEqualToleratingWhitespaceDifferences($$"""
                Reference assembly "{{dest.Path}}" already has latest information. Leaving it untouched.
                """,
                engine.Log);
        }

    }
}
