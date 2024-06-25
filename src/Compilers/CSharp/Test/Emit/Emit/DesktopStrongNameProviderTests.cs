// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.IO;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using static Roslyn.Test.Utilities.SigningTestHelpers;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class DesktopStrongNameProviderTests : CSharpTestBase
    {
        [WorkItem(13995, "https://github.com/dotnet/roslyn/issues/13995")]
        [Fact]
        public void RespectCustomTempPath()
        {
            var tempDir = Temp.CreateDirectory();
            var provider = new DesktopStrongNameProvider(tempPath: tempDir.Path);
            Assert.Equal(tempDir.Path, provider.FileSystem.GetSigningTempPath());
        }

        [Fact]
        public void RespectNullTempPath()
        {
            var provider = new DesktopStrongNameProvider(tempPath: null);
            Assert.Null(provider.FileSystem.GetSigningTempPath());
        }

        [Fact]
        public void EqualityUsingPath()
        {
            var tempDir = Temp.CreateDirectory();
            var provider1 = new DesktopStrongNameProvider(tempPath: tempDir.Path);
            var provider2 = new DesktopStrongNameProvider(tempPath: tempDir.Path);

            Assert.Equal(provider1, provider2);
        }

        [Fact]
        public void EqualityUsingKeyFileSearchPaths()
        {
            var tempDir = Temp.CreateDirectory();
            var provider1 = new DesktopStrongNameProvider(keyFileSearchPaths: [@"c:\test"]);
            var provider2 = new DesktopStrongNameProvider(keyFileSearchPaths: [@"c:\test"]);

            Assert.Equal(provider1, provider2);
        }

        [Fact]
        public void InequalityUsingPath()
        {
            var tempDir = Temp.CreateDirectory();
            var provider1 = new DesktopStrongNameProvider(tempPath: tempDir.Path);
            var provider2 = new DesktopStrongNameProvider(tempPath: tempDir.Path + "2");

            Assert.NotEqual(provider1, provider2);
        }

        [Fact]
        public void InequalityUsingKeyFileSearchPaths()
        {
            var tempDir = Temp.CreateDirectory();
            var provider1 = new DesktopStrongNameProvider(keyFileSearchPaths: [@"c:\test"]);
            var provider2 = new DesktopStrongNameProvider(keyFileSearchPaths: [@"c:\test2"]);

            Assert.NotEqual(provider1, provider2);
        }

        [Fact]
        public void EmitWithCustomTempPath()
        {
            string src = @"
class C
{
    public static void Main(string[] args) { }
}";
            var tempDir = Temp.CreateDirectory();
            var provider = new DesktopStrongNameProvider(ImmutableArray<string>.Empty, new VirtualizedStrongNameFileSystem(tempDir.Path));

            var options = TestOptions
                .DebugExe
                .WithStrongNameProvider(provider)
                .WithCryptoKeyFile(SigningTestHelpers.KeyPairFile);
            var comp = CreateCompilation(src, options: options);
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void EmitWithDefaultTempPath()
        {
            string src = @"
class C
{
    public static void Main(string[] args) { }
}";
            var provider = new DesktopStrongNameProvider(ImmutableArray<string>.Empty, new VirtualizedStrongNameFileSystem());
            var options = TestOptions
                .DebugExe
                .WithStrongNameProvider(provider)
                .WithCryptoKeyFile(SigningTestHelpers.KeyPairFile);
            var comp = CreateCompilation(src, options: options);
            comp.VerifyEmitDiagnostics();
        }
    }
}
