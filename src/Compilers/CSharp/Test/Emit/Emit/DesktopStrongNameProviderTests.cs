// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
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
            Assert.Equal(tempDir.Path, provider.FileSystem.GetSigningPath());
        }

        [Fact]
        public void RespectNullTempPath()
        {
            var provider = new DesktopStrongNameProvider(tempPath: null);
            Assert.Null(provider.FileSystem.GetSigningPath());
        }

        [ConditionalFact(typeof(WindowsOnly))]
        public void LegacySigningUsesCustomTempPathForStreams()
        {
            var directory = Temp.CreateDirectory();
            var key = Temp.CreateFile().WriteAllBytes(TestResources.General.snKey);
            var stagingPaths = new List<string>();
            var fileSystem = new TestStrongNameFileSystem(directory.Path);
            fileSystem.CreateFileStreamFunc = (path, mode, access, share) =>
            {
                Assert.Equal(FileMode.CreateNew, mode);
                stagingPaths.Add(path);
                return new FileStream(path, mode, access, share);
            };
            var provider = new DesktopStrongNameProvider(default, fileSystem);
            var compilation = CreateCompilation(
                "public class C { }",
                options: TestOptions.ReleaseDll.WithStrongNameProvider(provider).WithCryptoKeyFile(key.Path),
                parseOptions: TestOptions.RegularWithLegacyStrongName);
            // Compilation setup may emit while validating used assembly references.
            stagingPaths.Clear();
            using var output = new MemoryStream();
            using var reference = new MemoryStream();
            var result = compilation.Emit(output, metadataPEStream: reference);

            Assert.True(result.Success, string.Join(", ", result.Diagnostics));
            Assert.True(output.CanWrite);
            Assert.True(reference.CanWrite);
            Assert.Equal(2, stagingPaths.Count);
            Assert.All(stagingPaths, path =>
            {
                Assert.Equal(directory.Path, Path.GetDirectoryName(path));
                Assert.Matches(@"^legacy-sign\.[0-9a-f]{32}\.dll$", Path.GetFileName(path));
                Assert.False(File.Exists(path));
            });
            output.Position = 0;
            reference.Position = 0;
            Assert.True(ILValidation.IsStreamFullSigned(output));
            Assert.True(ILValidation.IsStreamFullSigned(reference));
        }

        [ConditionalTheory(typeof(WindowsOnly))]
        [InlineData(false)]
        [InlineData(true)]
        public void LegacySigningStreamFailures(bool cancel)
        {
            var directory = Temp.CreateDirectory();
            var key = Temp.CreateFile().WriteAllBytes(TestResources.General.snKey);
            string stagingPath = null;
            var fileSystem = new TestStrongNameFileSystem(directory.Path)
            {
                CreateFileStreamFunc = (path, mode, access, share) =>
                {
                    stagingPath = path;
                    return new FileStream(path, mode, access, share);
                }
            };
            var compilation = CreateCompilation(
                "public class C { }",
                options: TestOptions.ReleaseDll.WithStrongNameProvider(new DesktopStrongNameProvider(default, fileSystem)).WithCryptoKeyFile(key.Path),
                parseOptions: TestOptions.RegularWithLegacyStrongName);
            using var output = new BrokenStream
            {
                BreakHow = cancel ? BrokenStream.BreakHowType.CancelOnWrite : BrokenStream.BreakHowType.ThrowOnWrite
            };
            if (cancel)
            {
                Assert.Throws<OperationCanceledException>(() => compilation.Emit(output));
            }
            else
            {
                var result = compilation.Emit(output);
                Assert.False(result.Success);
                Assert.Equal((int)ErrorCode.ERR_PublicKeyFileFailure, Assert.Single(result.Diagnostics).Code);
            }
            Assert.NotNull(stagingPath);
            Assert.False(File.Exists(stagingPath));
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
            var provider1 = new DesktopStrongNameProvider(keyFileSearchPaths: [tempDir.Path]);
            var provider2 = new DesktopStrongNameProvider(keyFileSearchPaths: [tempDir.Path]);

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
            var provider1 = new DesktopStrongNameProvider(keyFileSearchPaths: [tempDir.Path]);
            var provider2 = new DesktopStrongNameProvider(keyFileSearchPaths: [tempDir.Path + "2"]);

            Assert.NotEqual(provider1, provider2);
        }

        [Fact]
        public void InequalityUsingKeyFileSearchPathsDueToCaseSensitivity()
        {
            var tempDir = Temp.CreateDirectory();
            var provider1 = new DesktopStrongNameProvider(keyFileSearchPaths: [tempDir.Path]);
            var provider2 = new DesktopStrongNameProvider(keyFileSearchPaths: [tempDir.Path.ToUpper()]);

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
