// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Test.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using Xunit;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Emit
{
    public class DeterministicTests : EmitMetadataTestBase
    {
        private Guid CompiledGuid(string source, string assemblyName, bool debug)
        {
            var compilation = CreateCompilation(source,
                assemblyName: assemblyName,
                references: new[] { MscorlibRef },
                options: (debug ? TestOptions.DebugExe : TestOptions.ReleaseExe).WithFeatures(ImmutableArray.Create("deterministic")));

            Guid result = default(Guid);
            base.CompileAndVerify(compilation, emitters: TestEmitters.CCI, validator: (a, eo) =>
            {
                var module = a.Modules[0];
                result = module.GetModuleVersionIdOrThrow();
            });

            return result;
        }

        private ImmutableArray<byte> EmitDeterministic(string source, Platform platform, bool debug)
        {
            var options = (debug ? TestOptions.DebugExe : TestOptions.ReleaseExe).WithPlatform(platform);
            options = options.WithFeatures((new[] { "dEtErmInIstIc" }).AsImmutable()); // expect case-insensitivity

            var compilation = CreateCompilation(source, assemblyName: "DeterminismTest", references: new[] { MscorlibRef }, options: options);

            // The resolution of the PE header time date stamp is seconds, and we want to make sure that has an opportunity to change
            // between calls to Emit.
            Thread.Sleep(TimeSpan.FromSeconds(1));

            return compilation.EmitToArray();
        }

        [Fact, WorkItem(372, "https://github.com/dotnet/roslyn/issues/372")]
        public void Simple()
        {
            var source =
@"class Program
{
    public static void Main(string[] args) {}
}";
            // Two identical compilations should produce the same MVID
            var mvid1 = CompiledGuid(source, "X1", false);
            var mvid2 = CompiledGuid(source, "X1", false);
            Assert.Equal(mvid1, mvid2);

            // Changing the module name should change the MVID
            var mvid3 = CompiledGuid(source, "X2", false);
            Assert.NotEqual(mvid1, mvid3);

            // Two identical debug compilations should produce the same MVID also
            var mvid5 = CompiledGuid(source, "X1", true);
            var mvid6 = CompiledGuid(source, "X1", true);
            Assert.Equal(mvid5, mvid6);

            // But even in debug, a changed module name changes the MVID
            var mvid7 = CompiledGuid(source, "X2", true);
            Assert.NotEqual(mvid5, mvid7);

            // adding the debug option should change the MVID
            Assert.NotEqual(mvid1, mvid5);
            Assert.NotEqual(mvid3, mvid7);
        }

        [Fact]
        public void CompareAllBytesEmitted_Release()
        {
            var source =
@"class Program
{
    public static void Main(string[] args) {}
}";
            var result1 = EmitDeterministic(source, platform: Platform.AnyCpu32BitPreferred, debug: false);
            var result2 = EmitDeterministic(source, platform: Platform.AnyCpu32BitPreferred, debug: false);
            AssertEx.Equal(result1, result2);

            var result3 = EmitDeterministic(source, platform: Platform.X64, debug: false);
            var result4 = EmitDeterministic(source, platform: Platform.X64, debug: false);
            AssertEx.Equal(result3, result4);
        }

        [Fact, WorkItem(926)]
        public void CompareAllBytesEmitted_Debug()
        {
            var source =
@"class Program
{
    public static void Main(string[] args) {}
}";
            var result1 = EmitDeterministic(source, platform: Platform.AnyCpu32BitPreferred, debug: true);
            var result2 = EmitDeterministic(source, platform: Platform.AnyCpu32BitPreferred, debug: true);
            AssertEx.Equal(result1, result2);

            var result3 = EmitDeterministic(source, platform: Platform.X64, debug: true);
            var result4 = EmitDeterministic(source, platform: Platform.X64, debug: true);
            AssertEx.Equal(result3, result4);
        }

        [Fact]
        public void TestWriteOnlyStream()
        {
            var tree = CSharpSyntaxTree.ParseText("class Program { static void Main() { } }");
            var compilation = CSharpCompilation.Create("Program",
                                                       new[] { tree },
                                                       new[] { MetadataReference.CreateFromAssembly(typeof(object).Assembly) },
                                                       new CSharpCompilationOptions(OutputKind.ConsoleApplication).WithFeatures((new[] { "deterministic" }).AsImmutable()));
            var output = new WriteOnlyStream();
            compilation.Emit(output);
        }

        private class WriteOnlyStream : Stream
        {
            private int _length;
            public override bool CanRead { get { return false; } }
            public override bool CanSeek { get { return false; } }
            public override bool CanWrite { get { return true; } }
            public override long Length { get { return _length; } }
            public override long Position
            {
                get { return _length; }
                set { throw new NotSupportedException(); }
            }
            public override void Flush() { }
            public override int Read(byte[] buffer, int offset, int count) { throw new NotSupportedException(); }
            public override long Seek(long offset, SeekOrigin origin) { throw new NotSupportedException(); }
            public override void SetLength(long value) { throw new NotSupportedException(); }
            public override void Write(byte[] buffer, int offset, int count) { _length += count; }
        }
    }
}
