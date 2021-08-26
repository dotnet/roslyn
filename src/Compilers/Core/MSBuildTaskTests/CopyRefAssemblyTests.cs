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

        [Fact]
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
            Assert.True(string.IsNullOrEmpty(engine.Log));
            Assert.Equal("test", File.ReadAllText(dest));
        }

        [Fact]
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
            Assert.False(string.IsNullOrEmpty(engine.Log));
            Assert.Equal("test", File.ReadAllText(dest.Path));
        }
    }
}
