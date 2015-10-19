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
using Microsoft.CodeAnalysis.Emit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Emit
{
    public class DeterministicTests : EmitMetadataTestBase
    {
        private Guid CompiledGuid(string source, string assemblyName, bool debug)
        {
            var compilation = CreateCompilation(source,
                assemblyName: assemblyName,
                references: new[] { MscorlibRef },
                options: (debug ? TestOptions.DebugExe : TestOptions.ReleaseExe).WithDeterministic(true));

            Guid result = default(Guid);
            base.CompileAndVerify(compilation, validator: a =>
            {
                var module = a.Modules[0];
                result = module.GetModuleVersionIdOrThrow();
            });

            return result;
        }

        private ImmutableArray<byte> EmitDeterministic(string source, Platform platform, DebugInformationFormat pdbFormat, bool optimize)
        {
            var options = (optimize ? TestOptions.ReleaseExe : TestOptions.DebugExe).WithPlatform(platform).WithDeterministic(true);

            var compilation = CreateCompilation(source, assemblyName: "DeterminismTest", references: new[] { MscorlibRef }, options: options);

            // The resolution of the PE header time date stamp is seconds, and we want to make sure that has an opportunity to change
            // between calls to Emit.
            if (pdbFormat == DebugInformationFormat.Pdb)
            {
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }

            return compilation.EmitToArray(EmitOptions.Default.WithDebugInformationFormat(pdbFormat), pdbStream: new MemoryStream());
        }

        [Fact, WorkItem(4578, "https://github.com/dotnet/roslyn/issues/4578")]
        public void BanVersionWildcards()
        {
            string source = @"[assembly: System.Reflection.AssemblyVersion(""10101.0.*"")] public class C {}";
            var compilationDeterministic = CreateCompilation(
                source,
                assemblyName: "DeterminismTest", references: new[] { MscorlibRef },
                options: TestOptions.DebugDll.WithDeterministic(true));
            var compilationNonDeterministic = CreateCompilation(
                source, 
                assemblyName: "DeterminismTest", 
                references: new[] { MscorlibRef },
                options: TestOptions.DebugDll.WithDeterministic(false));

            var resultDeterministic = compilationDeterministic.Emit(Stream.Null, Stream.Null);
            var resultNonDeterministic = compilationNonDeterministic.Emit(Stream.Null, Stream.Null);

            Assert.False(resultDeterministic.Success);   
            Assert.True(resultNonDeterministic.Success);   
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/5813"), WorkItem(372, "https://github.com/dotnet/roslyn/issues/372")]
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

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/5813")]
        public void CompareAllBytesEmitted_Release()
        {
            foreach (var pdbFormat in new[]
            {
                DebugInformationFormat.Pdb,
                DebugInformationFormat.PortablePdb,
                DebugInformationFormat.Embedded
            })
            {
                var source =
    @"class Program
{
    public static void Main(string[] args) {}
}";
                var result1 = EmitDeterministic(source, Platform.AnyCpu32BitPreferred, pdbFormat, optimize: true);
                var result2 = EmitDeterministic(source, Platform.AnyCpu32BitPreferred, pdbFormat, optimize: true);
                AssertEx.Equal(result1, result2);

                var result3 = EmitDeterministic(source, Platform.X64, pdbFormat, optimize: true);
                var result4 = EmitDeterministic(source, Platform.X64, pdbFormat, optimize: true);
                AssertEx.Equal(result3, result4);
            }
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/5813"), WorkItem(926)]
        public void CompareAllBytesEmitted_Debug()
        {
            foreach (var pdbFormat in new[] 
            {
                DebugInformationFormat.Pdb,
                DebugInformationFormat.PortablePdb,
                DebugInformationFormat.Embedded
            })
            {
                var source =
@"class Program
{
    public static void Main(string[] args) {}
}";
                var result1 = EmitDeterministic(source, Platform.AnyCpu32BitPreferred, pdbFormat, optimize: false);
                var result2 = EmitDeterministic(source, Platform.AnyCpu32BitPreferred, pdbFormat, optimize: false);
                AssertEx.Equal(result1, result2);

                var result3 = EmitDeterministic(source, Platform.X64, pdbFormat, optimize: false);
                var result4 = EmitDeterministic(source, Platform.X64, pdbFormat, optimize: false);
                AssertEx.Equal(result3, result4);
            }
        }

        [Fact]
        public void TestWriteOnlyStream()
        {
            var tree = CSharpSyntaxTree.ParseText("class Program { static void Main() { } }");
            var compilation = CSharpCompilation.Create("Program",
                                                       new[] { tree },
                                                       new[] { MetadataReference.CreateFromAssemblyInternal(typeof(object).Assembly) },
                                                       new CSharpCompilationOptions(OutputKind.ConsoleApplication).WithDeterministic(true));
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
