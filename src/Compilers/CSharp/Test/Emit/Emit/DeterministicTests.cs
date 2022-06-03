// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using System.Reflection.PortableExecutable;
using Roslyn.Test.Utilities;
using Xunit;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Retargeting;
using Microsoft.CodeAnalysis.PooledObjects;
using System.Security.Cryptography;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Emit
{
    [CompilerTrait(CompilerFeature.Determinism)]
    public class DeterministicTests : EmitMetadataTestBase
    {
        private Guid CompiledGuid(string source, string assemblyName, bool debug, Platform platform = Platform.AnyCpu)
        {
            return CompiledGuid(source, assemblyName, options: debug ? TestOptions.DebugExe : TestOptions.ReleaseExe, platform: platform);
        }

        private Guid CompiledGuid(string source, string assemblyName, CSharpCompilationOptions options, EmitOptions emitOptions = null, Platform platform = Platform.AnyCpu)
        {
            var compilation = CreateEmptyCompilation(source,
                assemblyName: assemblyName,
                references: new[] { MscorlibRef },
                options: options.WithDeterministic(true).WithPlatform(platform));

            Guid result = default(Guid);
            base.CompileAndVerify(compilation, emitOptions: emitOptions, validator: a =>
            {
                var module = a.Modules[0];
                result = module.GetModuleVersionIdOrThrow();
            });

            return result;
        }

        private (ImmutableArray<byte> pe, ImmutableArray<byte> pdb) EmitDeterministic(string source, Platform platform, DebugInformationFormat pdbFormat, bool optimize)
        {
            var options = (optimize ? TestOptions.ReleaseExe : TestOptions.DebugExe).WithPlatform(platform).WithDeterministic(true);

            var compilation = CreateEmptyCompilation(source, assemblyName: "DeterminismTest", references: new[] { MscorlibRef, SystemCoreRef, CSharpRef }, options: options);

            // The resolution of the PE header time date stamp is seconds, and we want to make sure that has an opportunity to change
            // between calls to Emit.
            if (pdbFormat == DebugInformationFormat.Pdb)
            {
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }

            var pdbStream = (pdbFormat == DebugInformationFormat.Embedded) ? null : new MemoryStream();

            return (pe: compilation.EmitToArray(EmitOptions.Default.WithDebugInformationFormat(pdbFormat), pdbStream: pdbStream),
                    pdb: (pdbStream ?? new MemoryStream()).ToImmutable());
        }

        [Fact, WorkItem(4578, "https://github.com/dotnet/roslyn/issues/4578")]
        public void BanVersionWildcards()
        {
            string source = @"[assembly: System.Reflection.AssemblyVersion(""10101.0.*"")] public class C {}";
            var compilationDeterministic = CreateEmptyCompilation(
                source,
                assemblyName: "DeterminismTest", references: new[] { MscorlibRef },
                options: TestOptions.DebugDll.WithDeterministic(true));
            var compilationNonDeterministic = CreateEmptyCompilation(
                source,
                assemblyName: "DeterminismTest",
                references: new[] { MscorlibRef },
                options: TestOptions.DebugDll.WithDeterministic(false));

            var resultDeterministic = compilationDeterministic.Emit(Stream.Null, pdbStream: Stream.Null);
            var resultNonDeterministic = compilationNonDeterministic.Emit(Stream.Null, pdbStream: Stream.Null);

            Assert.False(resultDeterministic.Success);
            Assert.True(resultNonDeterministic.Success);
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
        public void PlatformChangeGuid()
        {
            var source =
@"class Program
{
    public static void Main(string[] args) {}
}";
            // Two identical compilations should produce the same MVID
            var mvid1 = CompiledGuid(source, "X1", false, Platform.X86);
            var mvid2 = CompiledGuid(source, "X1", false, Platform.X86);
            Assert.Equal(mvid1, mvid2);

            var mvid3 = CompiledGuid(source, "X1", false, Platform.X64);
            var mvid4 = CompiledGuid(source, "X1", false, Platform.X64);
            Assert.Equal(mvid3, mvid4);

            var mvid5 = CompiledGuid(source, "X1", false, Platform.Arm64);
            var mvid6 = CompiledGuid(source, "X1", false, Platform.Arm64);
            Assert.Equal(mvid5, mvid6);

            // No two platforms should produce the same MVID
            Assert.NotEqual(mvid1, mvid3);
            Assert.NotEqual(mvid1, mvid5);
            Assert.NotEqual(mvid3, mvid5);
        }

        [Fact]
        public void PlatformChangeTimestamp()
        {
            var result1 = EmitDeterministic(CompareAllBytesEmitted_Source, Platform.X64, DebugInformationFormat.Embedded, optimize: false);
            var result2 = EmitDeterministic(CompareAllBytesEmitted_Source, Platform.Arm64, DebugInformationFormat.Embedded, optimize: false);

            AssertEx.NotEqual(result1.pe, result2.pe);

            PEReader peReader1 = new PEReader(result1.pe);
            PEReader peReader2 = new PEReader(result2.pe);
            Assert.Equal(Machine.Amd64, peReader1.PEHeaders.CoffHeader.Machine);
            Assert.Equal(Machine.Arm64, peReader2.PEHeaders.CoffHeader.Machine);
            Assert.NotEqual(peReader1.PEHeaders.CoffHeader.TimeDateStamp, peReader2.PEHeaders.CoffHeader.TimeDateStamp);
        }

        [Fact]
        public void RefAssembly()
        {
            var source =
@"class Program
{
    public static void Main(string[] args) {}
    CHANGE
}";
            var emitRefAssembly = EmitOptions.Default.WithEmitMetadataOnly(true).WithIncludePrivateMembers(false);

            var mvid1 = CompiledGuid(source.Replace("CHANGE", ""), "X1", TestOptions.DebugDll, emitRefAssembly);
            var mvid2 = CompiledGuid(source.Replace("CHANGE", "private void M() { }"), "X1", TestOptions.DebugDll, emitRefAssembly);
            Assert.Equal(mvid1, mvid2);
        }

        const string CompareAllBytesEmitted_Source = @"
using System;
using System.Linq;
using System.Collections.Generic;

namespace N
{
    using I4 = System.Int32;
    
    class Program
    {
        public static IEnumerable<int> F() 
        {
            I4 x = 1; 
            yield return 1;
            yield return x;
        }

        public static void Main(string[] args) 
        {
            dynamic x = 1;
            const int a = 1;
            F().ToArray();
            Console.WriteLine(x + a);
        }
    }
}";

        [Theory]
        [MemberData(nameof(PdbFormats))]
        public void CompareAllBytesEmitted_Release(DebugInformationFormat pdbFormat)
        {
            // Disable for PDB due to flakiness https://github.com/dotnet/roslyn/issues/41626
            if (pdbFormat == DebugInformationFormat.Pdb)
            {
                return;
            }

            var result1 = EmitDeterministic(CompareAllBytesEmitted_Source, Platform.AnyCpu32BitPreferred, pdbFormat, optimize: true);
            var result2 = EmitDeterministic(CompareAllBytesEmitted_Source, Platform.AnyCpu32BitPreferred, pdbFormat, optimize: true);
            AssertEx.Equal(result1.pe, result2.pe);
            AssertEx.Equal(result1.pdb, result2.pdb);

            var result3 = EmitDeterministic(CompareAllBytesEmitted_Source, Platform.X64, pdbFormat, optimize: true);
            var result4 = EmitDeterministic(CompareAllBytesEmitted_Source, Platform.X64, pdbFormat, optimize: true);
            AssertEx.Equal(result3.pe, result4.pe);
            AssertEx.Equal(result3.pdb, result4.pdb);

            var result5 = EmitDeterministic(CompareAllBytesEmitted_Source, Platform.Arm64, pdbFormat, optimize: true);
            var result6 = EmitDeterministic(CompareAllBytesEmitted_Source, Platform.Arm64, pdbFormat, optimize: true);
            AssertEx.Equal(result5.pe, result6.pe);
            AssertEx.Equal(result5.pdb, result6.pdb);
        }

        [WorkItem(926, "https://github.com/dotnet/roslyn/issues/926")]
        [Theory]
        [MemberData(nameof(PdbFormats))]
        public void CompareAllBytesEmitted_Debug(DebugInformationFormat pdbFormat)
        {
            // Disable for PDB due to flakiness https://github.com/dotnet/roslyn/issues/41626
            if (pdbFormat == DebugInformationFormat.Pdb)
            {
                return;
            }

            var result1 = EmitDeterministic(CompareAllBytesEmitted_Source, Platform.AnyCpu32BitPreferred, pdbFormat, optimize: false);
            var result2 = EmitDeterministic(CompareAllBytesEmitted_Source, Platform.AnyCpu32BitPreferred, pdbFormat, optimize: false);
            AssertEx.Equal(result1.pe, result2.pe);
            AssertEx.Equal(result1.pdb, result2.pdb);

            var result3 = EmitDeterministic(CompareAllBytesEmitted_Source, Platform.X64, pdbFormat, optimize: false);
            var result4 = EmitDeterministic(CompareAllBytesEmitted_Source, Platform.X64, pdbFormat, optimize: false);
            AssertEx.Equal(result3.pe, result4.pe);
            AssertEx.Equal(result3.pdb, result4.pdb);

            var result5 = EmitDeterministic(CompareAllBytesEmitted_Source, Platform.Arm64, pdbFormat, optimize: false);
            var result6 = EmitDeterministic(CompareAllBytesEmitted_Source, Platform.Arm64, pdbFormat, optimize: false);
            AssertEx.Equal(result5.pe, result6.pe);
            AssertEx.Equal(result5.pdb, result6.pdb);
        }

        [Fact]
        public void TestWriteOnlyStream()
        {
            var tree = CSharpSyntaxTree.ParseText("class Program { static void Main() { } }");
            var compilation = CSharpCompilation.Create("Program",
                                                       new[] { tree },
                                                       new[] { MetadataReference.CreateFromAssemblyInternal(typeof(object).Assembly) },
                                                       TestOptions.DebugExe.WithDeterministic(true));
            var output = new WriteOnlyStream();
            compilation.Emit(output);
        }

        [Fact, WorkItem(11990, "https://github.com/dotnet/roslyn/issues/11990")]
        public void ForwardedTypesAreEmittedInADeterministicOrder()
        {
            var forwardedToCode = @"
namespace Namespace2 {
    public class GenericType1<T> {}
    public class GenericType3<T> {}
    public class GenericType2<T> {}
}
namespace Namespace1 {
    public class Type3 {}
    public class Type2 {}
    public class Type1 {}
}
namespace Namespace4 {
    namespace Embedded {
        public class Type2 {}
        public class Type1 {}
    }
}
namespace Namespace3 {
    public class GenericType {}
    public class GenericType<T> {}
    public class GenericType<T, U> {}
}
";
            var forwardedToCompilation1 = CreateCompilation(forwardedToCode, assemblyName: "ForwardedTo");
            var forwardedToReference1 = new CSharpCompilationReference(forwardedToCompilation1);

            var forwardingCode = @"
using System.Runtime.CompilerServices;
[assembly: TypeForwardedTo(typeof(Namespace2.GenericType1<int>))]
[assembly: TypeForwardedTo(typeof(Namespace2.GenericType3<int>))]
[assembly: TypeForwardedTo(typeof(Namespace2.GenericType2<int>))]
[assembly: TypeForwardedTo(typeof(Namespace1.Type3))]
[assembly: TypeForwardedTo(typeof(Namespace1.Type2))]
[assembly: TypeForwardedTo(typeof(Namespace1.Type1))]
[assembly: TypeForwardedTo(typeof(Namespace4.Embedded.Type2))]
[assembly: TypeForwardedTo(typeof(Namespace4.Embedded.Type1))]
[assembly: TypeForwardedTo(typeof(Namespace3.GenericType))]
[assembly: TypeForwardedTo(typeof(Namespace3.GenericType<int>))]
[assembly: TypeForwardedTo(typeof(Namespace3.GenericType<int, int>))]
";

            var forwardingCompilation = CreateCompilation(forwardingCode, new MetadataReference[] { forwardedToReference1 });
            var forwardingReference = new CSharpCompilationReference(forwardingCompilation);

            var sortedFullNames = new string[]
            {
                "Namespace1.Type1",
                "Namespace1.Type2",
                "Namespace1.Type3",
                "Namespace2.GenericType1`1",
                "Namespace2.GenericType2`1",
                "Namespace2.GenericType3`1",
                "Namespace3.GenericType",
                "Namespace3.GenericType`1",
                "Namespace3.GenericType`2",
                "Namespace4.Embedded.Type1",
                "Namespace4.Embedded.Type2"
            };

            Action<ModuleSymbol> metadataValidator = module =>
            {
                var assembly = module.ContainingAssembly;
                Assert.Equal(sortedFullNames, getNamesOfForwardedTypes(assembly));
            };

            CompileAndVerify(forwardingCompilation, symbolValidator: metadataValidator, sourceSymbolValidator: metadataValidator, verify: Verification.Skipped);

            using (var stream = forwardingCompilation.EmitToStream())
            {
                using (var block = ModuleMetadata.CreateFromStream(stream))
                {
                    var metadataFullNames = MetadataValidation.GetExportedTypesFullNames(block.MetadataReader);
                    Assert.Equal(sortedFullNames, metadataFullNames);
                }
            }

            var forwardedToCompilation2 = CreateCompilation(forwardedToCode, assemblyName: "ForwardedTo");
            var forwardedToReference2 = new CSharpCompilationReference(forwardedToCompilation2);

            var withRetargeting = CreateCompilation("", new MetadataReference[] { forwardedToReference2, forwardingReference });

            var retargeting = (RetargetingAssemblySymbol)withRetargeting.GetReferencedAssemblySymbol(forwardingReference);
            Assert.Equal(sortedFullNames, getNamesOfForwardedTypes(retargeting));

            foreach (var type in getForwardedTypes(retargeting))
            {
                Assert.Same(forwardedToCompilation2.Assembly.GetPublicSymbol(), type.ContainingAssembly);
            }

            static IEnumerable<string> getNamesOfForwardedTypes(AssemblySymbol assembly)
            {
                return getForwardedTypes(assembly).Select(t => t.ToDisplayString(SymbolDisplayFormat.QualifiedNameArityFormat));
            }

            static ImmutableArray<INamedTypeSymbol> getForwardedTypes(AssemblySymbol assembly)
            {
                return assembly.GetPublicSymbol().GetForwardedTypes();
            }
        }

        [ConditionalFact(typeof(ClrOnly), Reason = "Static execution is runtime defined and this tests Clr behavior only")]
        public void TestPartialPartsDeterministic()
        {
            var x1 =
@"partial class Partial : I1
{
    public static int a = D.Init(1, ""Partial.a"");
}";
            var x2 =
@"partial class Partial : I2
{
    public static int c, b = D.Init(2, ""Partial.b"");
    static Partial()
    {
                c = D.Init(3, ""Partial.c"");
            }
        }";
            var x3 =
@"class D
{
    public static void Main(string[] args)
    {
        foreach (var i in typeof(Partial).GetInterfaces())
        {
            System.Console.WriteLine(i.Name);
        }
        System.Console.WriteLine($""Partial.a = {Partial.a}"");
        System.Console.WriteLine($""Partial.b = {Partial.b}"");
        System.Console.WriteLine($""Partial.c = {Partial.c}"");
    }
    public static int Init(int value, string message)
    {
        System.Console.WriteLine(message);
        return value;
    }
}

interface I1 { }
interface I2 { }
";
            var expectedOutput1 =
@"I1
I2
Partial.a
Partial.b
Partial.c
Partial.a = 1
Partial.b = 2
Partial.c = 3";
            var expectedOutput2 =
@"I2
I1
Partial.b
Partial.a
Partial.c
Partial.a = 1
Partial.b = 2
Partial.c = 3";
            // we run more than once to increase the chance of observing a problem due to nondeterminism
            for (int i = 0; i < 2; i++)
            {
                var cv = CompileAndVerify(source: new string[] { x1, x2, x3 }, expectedOutput: expectedOutput1);
                var trees = cv.Compilation.SyntaxTrees.ToArray();
                var comp2 = cv.Compilation.RemoveAllSyntaxTrees().AddSyntaxTrees(trees[1], trees[0], trees[2]);
                CompileAndVerify(comp2, expectedOutput: expectedOutput2);
                CompileAndVerify(source: new string[] { x2, x1, x3 }, expectedOutput: expectedOutput2);
            }
        }

        [Fact, WorkItem(53865, "https://github.com/dotnet/roslyn/issues/53865")]
        public void DeterminismWithFixedFields()
        {
            string source = @"
public unsafe struct UnsafeStructNUMBER1
{
    public fixed ushort FixedField1[10];
    public fixed ushort FixedField2[10];
    public UnsafeStructNUMBER2* pStruct;

    public void AccessMethod()
    {
        pStruct->FixedField2[0] = 0;
    }
}
";
            byte[] result = null;

            var sourceBuilder = ArrayBuilder<string>.GetInstance();
            const int max = 20;
            for (int i = 0; i < max; i++)
            {
                int j = (i + 7) % max;
                sourceBuilder.Add(source.Replace("NUMBER1", i.ToString()).Replace("NUMBER2", j.ToString()));
            }
            string[] source1 = sourceBuilder.ToArrayAndFree();

            var opt = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
            const string assemblyName = "TestAssembly";
            opt = opt.WithConcurrentBuild(true)
                .WithOptimizationLevel(OptimizationLevel.Debug)
                .WithDeterministic(true)
                .WithPlatform(Platform.AnyCpu)
                .WithAssemblyIdentityComparer(DesktopAssemblyIdentityComparer.Default)
                .WithAllowUnsafe(true)
                .WithOverflowChecks(false)
                .WithModuleName(assemblyName);

            for (int j = 0; j < 10; j++)
            {
                var comp = CreateCompilation(source1, options: opt, assemblyName: assemblyName);
                comp.VerifyDiagnostics();

                var optEmit = new EmitOptions()
                    .WithDebugInformationFormat(DebugInformationFormat.PortablePdb)
                    .WithPdbChecksumAlgorithm(HashAlgorithmName.SHA256);

                using var streamDll = new MemoryStream();
                using var streamXml = new MemoryStream();
                using var streamPdb = new MemoryStream();
                var emitResult = comp.Emit(streamDll, xmlDocumentationStream: streamXml,
                    pdbStream: optEmit.DebugInformationFormat != DebugInformationFormat.Embedded ? streamPdb : null,
                    options: optEmit);
                var newResult = streamDll.ToArray();
                if (result is null)
                {
                    result = newResult;
                }
                else
                {
                    Assert.Equal(result, newResult);
                }
            }
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
