// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        [ConditionalFact(typeof(WindowsOnly), Reason = "https://github.com/dotnet/roslyn/issues/30152")]
        public void RespectCustomTempPath()
        {
            var tempDir = Temp.CreateDirectory();
            var provider = new DesktopStrongNameProvider(tempPath: tempDir.Path);
            using (var stream = (DesktopStrongNameProvider.TempFileStream)provider.CreateInputStream())
            {
                Assert.Equal(tempDir.Path, Path.GetDirectoryName(stream.Path));
            }
        }

        [ConditionalFact(typeof(WindowsOnly), Reason = "https://github.com/dotnet/roslyn/issues/30152")]
        public void RespectDefaultTempPath()
        {
            var provider = new DesktopStrongNameProvider(tempPath: null);
            using (var stream = (DesktopStrongNameProvider.TempFileStream)provider.CreateInputStream())
            {
                Assert.Equal(Path.GetTempPath(), Path.GetDirectoryName(stream.Path) + @"\");
            }
        }

        [ConditionalFact(typeof(WindowsOnly), Reason = "https://github.com/dotnet/roslyn/issues/30152")]
        public void EmitWithCustomTempPath()
        {
            string src = @"
class C
{
    public static void Main(string[] args) { }
}";
            var tempDir = Temp.CreateDirectory();
            var provider = new DesktopStrongNameProvider(ImmutableArray<string>.Empty, tempDir.Path, new VirtualizedStrongNameFileSystem());

            var options = TestOptions
                .DebugExe
                .WithStrongNameProvider(provider)
                .WithCryptoKeyFile(SigningTestHelpers.KeyPairFile);
            var comp = CreateCompilation(src, options: options);
            comp.VerifyEmitDiagnostics();
        }

        [ConditionalFact(typeof(WindowsOnly), Reason = "https://github.com/dotnet/roslyn/issues/30152")]
        public void EmitWithDefaultTempPath()
        {
            string src = @"
class C
{
    public static void Main(string[] args) { }
}";
            var provider = new DesktopStrongNameProvider(ImmutableArray<string>.Empty, null, new VirtualizedStrongNameFileSystem());
            var options = TestOptions
                .DebugExe
                .WithStrongNameProvider(provider)
                .WithCryptoKeyFile(SigningTestHelpers.KeyPairFile);
            var comp = CreateCompilation(src, options: options);
            comp.VerifyEmitDiagnostics();
        }
    }
}
