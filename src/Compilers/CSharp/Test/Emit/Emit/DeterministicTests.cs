﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private Guid CompiledGuid(string source, string assemblyName)
        {
            var compilation = CreateCompilation(source,
                assemblyName: assemblyName,
                references: new[] { MscorlibRef },
                options: TestOptions.ReleaseExe.WithFeatures(ImmutableArray.Create("deterministic")));

            Guid result = default(Guid);
            base.CompileAndVerify(compilation, emitOptions: TestEmitters.CCI, validator: (a, eo) =>
            {
                var module = a.Modules[0];
                result = module.GetModuleVersionIdOrThrow();
            });

            return result;
        }

        private ImmutableArray<byte> GetBytesEmitted(string source, Platform platform, bool debug, bool deterministic)
        {
            var options = (debug ? TestOptions.DebugExe : TestOptions.ReleaseExe).WithPlatform(platform);
            if (deterministic)
            {
                options = options.WithFeatures((new[] { "dEtErmInIstIc" }).AsImmutable()); // expect case-insensitivity
            }

            var compilation = CreateCompilation(source, assemblyName: "DeterminismTest", references: new[] { MscorlibRef }, options: options);

            // The resolution of the PE header time date stamp is seconds, and we want to make sure that has an opportunity to change
            // between calls to Emit.
            Thread.Sleep(TimeSpan.FromSeconds(1));

            return compilation.EmitToArray();
        }

        private class ImmutableByteArrayEqualityComparer : IEqualityComparer<ImmutableArray<byte>>
        {
            public bool Equals(ImmutableArray<byte> x, ImmutableArray<byte> y)
            {
                return x.SequenceEqual(y);
            }

            public int GetHashCode(ImmutableArray<byte> obj)
            {
                return obj.GetHashCode();
            }
        }

        [Fact(Skip = "900646"), WorkItem(900646)]
        public void Simple()
        {
            var source =
@"class Program
{
    public static void Main(string[] args) {}
}";
            var mvid1 = CompiledGuid(source, "X1");
            var mvid2 = CompiledGuid(source, "X1");
            var mvid3 = CompiledGuid(source, "X2");
            var mvid4 = CompiledGuid(source, "X2");
            Assert.Equal(mvid1, mvid2);
            Assert.Equal(mvid3, mvid4);
            Assert.NotEqual(mvid1, mvid3);
        }

        [Fact]
        public void CompareAllBytesEmitted()
        {
            var source =
@"class Program
{
    public static void Main(string[] args) {}
}";
            var comparer = new ImmutableByteArrayEqualityComparer();

            var result1 = GetBytesEmitted(source, platform: Platform.AnyCpu32BitPreferred, debug: true, deterministic: true);
            var result2 = GetBytesEmitted(source, platform: Platform.AnyCpu32BitPreferred, debug: true, deterministic: true);
            Assert.Equal(result1, result2, comparer);

            var result3 = GetBytesEmitted(source, platform: Platform.X64, debug: false, deterministic: true);
            var result4 = GetBytesEmitted(source, platform: Platform.X64, debug: false, deterministic: true);
            Assert.Equal(result3, result4, comparer);
            Assert.NotEqual(result1, result3, comparer);

            var result5 = GetBytesEmitted(source, platform: Platform.X64, debug: false, deterministic: false);
            var result6 = GetBytesEmitted(source, platform: Platform.X64, debug: false, deterministic: false);
            Assert.NotEqual(result5, result6, comparer);
            Assert.NotEqual(result3, result5, comparer);
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
            private int _length = 0;
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
