// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using System;
using System.IO;
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
            using (var stream = (DesktopStrongNameProvider.TempFileStream)provider.CreateInputStream())
            {
                Assert.Equal(tempDir.Path, Path.GetDirectoryName(stream.Path));
            }
        }

        [Fact]
        public void RespectDefaultTempPath()
        {
            var provider = new DesktopStrongNameProvider(tempPath: null);
            using (var stream = (DesktopStrongNameProvider.TempFileStream)provider.CreateInputStream())
            {
                Assert.Equal(Path.GetTempPath(), Path.GetDirectoryName(stream.Path) + @"\");
            }
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
            var provider = new VirtualizedStrongNameProvider(tempPath: tempDir.Path);
            var options = TestOptions
                .DebugExe
                .WithStrongNameProvider(provider)
                .WithCryptoKeyFile(SigningTestHelpers.KeyPairFile);
            var comp = CreateStandardCompilation(src, options: options);
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
            var provider = new VirtualizedStrongNameProvider(tempPath: null);
            var options = TestOptions
                .DebugExe
                .WithStrongNameProvider(provider)
                .WithCryptoKeyFile(SigningTestHelpers.KeyPairFile);
            var comp = CreateStandardCompilation(src, options: options);
            comp.VerifyEmitDiagnostics();
        }
    }
}
