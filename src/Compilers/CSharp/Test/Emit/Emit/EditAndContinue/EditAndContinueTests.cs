// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue.UnitTests
{
    /// <summary>
    /// Tip: debug EncVariableSlotAllocator.TryGetPreviousClosure or other TryGet methods to figure out missing markers in your test.
    /// </summary>
    public class EditAndContinueTests : EditAndContinueTestBase
    {
        private static IEnumerable<string> DumpTypeRefs(MetadataReader[] readers)
        {
            var currentGenerationReader = readers.Last();
            foreach (var typeRefHandle in currentGenerationReader.TypeReferences)
            {
                var typeRef = currentGenerationReader.GetTypeReference(typeRefHandle);
                yield return $"[0x{MetadataTokens.GetToken(typeRef.ResolutionScope):x8}] {readers.GetString(typeRef.Namespace)}.{readers.GetString(typeRef.Name)}";
            }
        }

        [Fact]
        public void DeltaHeapsStartWithEmptyItem()
        {
            var source0 =
@"class C
{
    static string F() { return null; }
}";
            var source1 =
@"class C
{
    static string F() { return ""a""; }
}";
            var compilation0 = CreateCompilation(source0, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);

            var method0 = compilation0.GetMember<MethodSymbol>("C.F");
            var method1 = compilation1.GetMember<MethodSymbol>("C.F");

            var bytes0 = compilation0.EmitToArray();
            using var md0 = ModuleMetadata.CreateFromImage(bytes0);
            var reader0 = md0.MetadataReader;

            var diff1 = compilation1.EmitDifference(
                EmitBaseline.CreateInitialBaseline(md0, EmptyLocalsProvider),
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1)));

            using var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;

            var s = MetadataTokens.StringHandle(0);
            Assert.Equal(reader1.GetString(s), "");

            var b = MetadataTokens.BlobHandle(0);
            Assert.Equal(0, reader1.GetBlobBytes(b).Length);

            var us = MetadataTokens.UserStringHandle(0);
            Assert.Equal(reader1.GetUserString(us), "");
        }

        [Fact]
        public void Delta_AssemblyDefTable()
        {
            var source0 = @"public class C { public static void F() { System.Console.WriteLine(1); } }";
            var source1 = @"public class C { public static void F() { System.Console.WriteLine(2); } }";

            var compilation0 = CreateCompilationWithMscorlib45(source0, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");

            var v0 = CompileAndVerify(compilation0);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, f0, f1, preserveLocalVariables: true)));

            // AssemblyDef record is not emitted to delta since changes in assembly identity are not allowed:
            Assert.True(md0.MetadataReader.IsAssembly);
            Assert.False(diff1.GetMetadata().Reader.IsAssembly);
        }

        [Fact]
        public void SemanticErrors_MethodBody()
        {
            var source0 = MarkedSource(@"
class C
{
    static void E() 
    {
        int x = 1;
        System.Console.WriteLine(x);
    }

    static void G() 
    {
        System.Console.WriteLine(1);
    }
}");
            var source1 = MarkedSource(@"
class C
{
    static void E() 
    {
        int x = Unknown(2);
        System.Console.WriteLine(x);
    }

    static void G() 
    {
        System.Console.WriteLine(2);
    }
}");

            var compilation0 = CreateCompilation(source0.Tree, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);

            var e0 = compilation0.GetMember<MethodSymbol>("C.E");
            var e1 = compilation1.GetMember<MethodSymbol>("C.E");
            var g0 = compilation0.GetMember<MethodSymbol>("C.G");
            var g1 = compilation1.GetMember<MethodSymbol>("C.G");

            var v0 = CompileAndVerify(compilation0);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);
            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            // Semantic errors are reported only for the bodies of members being emitted.

            var diffError = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, e0, e1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            diffError.EmitResult.Diagnostics.Verify(
                // (6,17): error CS0103: The name 'Unknown' does not exist in the current context
                //         int x = Unknown(2);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "Unknown").WithArguments("Unknown").WithLocation(6, 17));

            var diffGood = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, g0, g1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            diffGood.EmitResult.Diagnostics.Verify();

            diffGood.VerifyIL(@"C.G", @"
{
  // Code size        9 (0x9)
  .maxstack  1
  IL_0000:  nop
  IL_0001:  ldc.i4.2
  IL_0002:  call       ""void System.Console.WriteLine(int)""
  IL_0007:  nop
  IL_0008:  ret
}
");
        }

        [Fact]
        public void SemanticErrors_Declaration()
        {
            var source0 = MarkedSource(@"
class C
{
    static void G() 
    {
        System.Console.WriteLine(1);
    }
}
");
            var source1 = MarkedSource(@"
class C
{
    static void G() 
    {
        System.Console.WriteLine(2);
    }
}

class Bad : Bad
{
}
");

            var compilation0 = CreateCompilation(source0.Tree, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);

            var g0 = compilation0.GetMember<MethodSymbol>("C.G");
            var g1 = compilation1.GetMember<MethodSymbol>("C.G");

            var v0 = CompileAndVerify(compilation0);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);
            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, g0, g1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            // All declaration errors are reported regardless of what member do we emit.

            diff.EmitResult.Diagnostics.Verify(
                // (10,7): error CS0146: Circular base class dependency involving 'Bad' and 'Bad'
                // class Bad : Bad
                Diagnostic(ErrorCode.ERR_CircularBase, "Bad").WithArguments("Bad", "Bad").WithLocation(10, 7));
        }

        [Fact]
        public void ModifyMethod()
        {
            var source0 =
@"class C
{
    static void Main() { }
    static string F() { return null; }
}";
            var source1 =
@"class C
{
    static void Main() { }
    static string F() { return string.Empty; }
}";
            var compilation0 = CreateCompilation(source0, options: TestOptions.DebugExe);
            var compilation1 = compilation0.WithSource(source1);

            var method0 = compilation0.GetMember<MethodSymbol>("C.F");

            // Verify full metadata contains expected rows.
            var bytes0 = compilation0.EmitToArray();
            using var md0 = ModuleMetadata.CreateFromImage(bytes0);
            var reader0 = md0.MetadataReader;

            CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "C");
            CheckNames(reader0, reader0.GetMethodDefNames(), "Main", "F", ".ctor");
            CheckNames(reader0, reader0.GetMemberRefNames(), /*CompilationRelaxationsAttribute.*/".ctor", /*RuntimeCompatibilityAttribute.*/".ctor", /*Object.*/".ctor", /*DebuggableAttribute*/".ctor");

            var generation0 = EmitBaseline.CreateInitialBaseline(
                md0,
                EmptyLocalsProvider);
            var method1 = compilation1.GetMember<MethodSymbol>("C.F");

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1)));

            // Verify delta metadata contains expected rows.
            using var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;
            var readers = new[] { reader0, reader1 };

            EncValidation.VerifyModuleMvid(1, reader0, reader1);

            CheckNames(readers, reader1.GetTypeDefNames());
            CheckNames(readers, reader1.GetMethodDefNames(), "F");
            CheckNames(readers, reader1.GetMemberRefNames(), /*String.*/"Empty");

            CheckEncLog(reader1,
                Row(2, TableIndex.AssemblyRef, EditAndContinueOperation.Default),
                Row(5, TableIndex.MemberRef, EditAndContinueOperation.Default),
                Row(6, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(7, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(2, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default)); // C.F

            CheckEncMap(reader1,
                Handle(6, TableIndex.TypeRef),
                Handle(7, TableIndex.TypeRef),
                Handle(2, TableIndex.MethodDef),
                Handle(5, TableIndex.MemberRef),
                Handle(2, TableIndex.StandAloneSig),
                Handle(2, TableIndex.AssemblyRef));
        }

        [CompilerTrait(CompilerFeature.Tuples)]
        [Fact]
        public void ModifyMethod_WithTuples()
        {
            var source0 =
@"class C
{
    static void Main() { }
    static (int, int) F() { return (1, 2); }
}";
            var source1 =
@"class C
{
    static void Main() { }
    static (int, int) F() { return (2, 3); }
}";
            var compilation0 = CreateCompilation(source0, options: TestOptions.DebugExe, targetFramework: TargetFramework.NetStandard20);
            var compilation1 = compilation0.WithSource(source1);

            var bytes0 = compilation0.EmitToArray();
            using var md0 = ModuleMetadata.CreateFromImage(bytes0);
            var reader0 = md0.MetadataReader;

            var method0 = compilation0.GetMember<MethodSymbol>("C.F");
            var generation0 = EmitBaseline.CreateInitialBaseline(
                md0,
                EmptyLocalsProvider);

            var method1 = compilation1.GetMember<MethodSymbol>("C.F");


            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1)));

            // Verify delta metadata contains expected rows.
            using var md1 = diff1.GetMetadata();

            var reader1 = md1.Reader;
            var readers = new[] { reader0, reader1 };

            EncValidation.VerifyModuleMvid(1, reader0, reader1);

            CheckNames(readers, reader1.GetTypeDefNames());
            CheckNames(readers, reader1.GetMethodDefNames(), "F");
            CheckNames(readers, reader1.GetMemberRefNames(), /*System.ValueTuple.*/".ctor");

            CheckEncLog(reader1,
                Row(2, TableIndex.AssemblyRef, EditAndContinueOperation.Default),
                Row(6, TableIndex.MemberRef, EditAndContinueOperation.Default),
                Row(7, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(8, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(2, TableIndex.TypeSpec, EditAndContinueOperation.Default),
                Row(2, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default)); // C.F

            CheckEncMap(reader1,
                Handle(7, TableIndex.TypeRef),
                Handle(8, TableIndex.TypeRef),
                Handle(2, TableIndex.MethodDef),
                Handle(6, TableIndex.MemberRef),
                Handle(2, TableIndex.StandAloneSig),
                Handle(2, TableIndex.TypeSpec),
                Handle(2, TableIndex.AssemblyRef));
        }

        [WorkItem(962219, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/962219")]
        [Fact]
        public void PartialMethod()
        {
            var source =
@"partial class C
{
    static partial void M1();
    static partial void M2();
    static partial void M3();
    static partial void M1() { }
    static partial void M2() { }
}";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source);

            var bytes0 = compilation0.EmitToArray();
            using var md0 = ModuleMetadata.CreateFromImage(bytes0);
            var reader0 = md0.MetadataReader;

            CheckNames(reader0, reader0.GetMethodDefNames(), "M1", "M2", ".ctor");

            var method0 = compilation0.GetMember<MethodSymbol>("C.M2").PartialImplementationPart;
            var method1 = compilation1.GetMember<MethodSymbol>("C.M2").PartialImplementationPart;

            var generation0 = EmitBaseline.CreateInitialBaseline(
                md0,
                EmptyLocalsProvider);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1)));

            var methods = diff1.TestData.GetMethodsByName();
            Assert.Equal(methods.Count, 1);
            Assert.True(methods.ContainsKey("C.M2()"));

            using var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;
            var readers = new[] { reader0, reader1 };

            EncValidation.VerifyModuleMvid(1, reader0, reader1);

            CheckNames(readers, reader1.GetMethodDefNames(), "M2");

            CheckEncLog(reader1,
                Row(2, TableIndex.AssemblyRef, EditAndContinueOperation.Default),
                Row(6, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default));

            CheckEncMap(reader1,
                Handle(6, TableIndex.TypeRef),
                Handle(2, TableIndex.MethodDef),
                Handle(2, TableIndex.AssemblyRef));
        }

        /// <summary>
        /// Add a method that requires entries in the ParameterDefs table.
        /// Specifically, normal parameters or return types with attributes.
        /// Add the method in the first edit, then modify the method in the second.
        /// </summary>
        [Fact]
        public void AddThenModifyMethod()
        {
            var source0 =
@"class A : System.Attribute { }
class C
{
    static void Main() { F1(null); }
    static object F1(string s1) { return s1; }
}";
            var source1 =
@"class A : System.Attribute { }
class C
{
    static void Main() { F2(); }
    [return:A]static object F2(string s2 = ""2"") { return s2; }
}";
            var source2 =
@"class A : System.Attribute { }
class C
{
    static void Main() { F2(); }
    [return:A]static object F2(string s2 = ""2"") { return null; }
}";
            var compilation0 = CreateCompilation(source0, options: TestOptions.DebugExe);
            var compilation1 = compilation0.WithSource(source1);
            var compilation2 = compilation0.WithSource(source2);

            // Verify full metadata contains expected rows.
            var bytes0 = compilation0.EmitToArray();
            using var md0 = ModuleMetadata.CreateFromImage(bytes0);
            var reader0 = md0.MetadataReader;

            CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "A", "C");
            CheckNames(reader0, reader0.GetMethodDefNames(), ".ctor", "Main", "F1", ".ctor");
            CheckNames(reader0, reader0.GetParameterDefNames(), "s1");

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, EmptyLocalsProvider);

            var method1 = compilation1.GetMember<MethodSymbol>("C.F2");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Insert, null, method1)));

            // Verify delta metadata contains expected rows.
            using var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;
            var readers = new List<MetadataReader> { reader0, reader1 };
            EncValidation.VerifyModuleMvid(1, reader0, reader1);

            CheckNames(readers, reader1.GetTypeDefNames());
            CheckNames(readers, reader1.GetMethodDefNames(), "F2");
            CheckNames(readers, reader1.GetParameterDefNames(), "", "s2");

            CheckEncLog(reader1,
                Row(2, TableIndex.AssemblyRef, EditAndContinueOperation.Default),
                Row(7, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(2, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(5, TableIndex.MethodDef, EditAndContinueOperation.AddParameter),
                Row(2, TableIndex.Param, EditAndContinueOperation.Default),
                Row(5, TableIndex.MethodDef, EditAndContinueOperation.AddParameter),
                Row(3, TableIndex.Param, EditAndContinueOperation.Default),
                Row(1, TableIndex.Constant, EditAndContinueOperation.Default),
                Row(4, TableIndex.CustomAttribute, EditAndContinueOperation.Default));

            CheckEncMap(reader1,
                Handle(7, TableIndex.TypeRef),
                Handle(5, TableIndex.MethodDef),
                Handle(2, TableIndex.Param),
                Handle(3, TableIndex.Param),
                Handle(1, TableIndex.Constant),
                Handle(4, TableIndex.CustomAttribute),
                Handle(2, TableIndex.StandAloneSig),
                Handle(2, TableIndex.AssemblyRef));

            var method2 = compilation2.GetMember<MethodSymbol>("C.F2");
            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method1, method2)));

            // Verify delta metadata contains expected rows.
            using var md2 = diff2.GetMetadata();
            var reader2 = md2.Reader;
            readers.Add(reader2);

            EncValidation.VerifyModuleMvid(2, reader1, reader2);

            CheckNames(readers, reader2.GetTypeDefNames());
            CheckNames(readers, reader2.GetMethodDefNames(), "F2");
            CheckNames(readers, reader2.GetParameterDefNames());

            CheckEncLog(reader2,
                Row(3, TableIndex.AssemblyRef, EditAndContinueOperation.Default),
                Row(8, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(3, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default)); // C.F2

            CheckEncMap(reader2,
                Handle(8, TableIndex.TypeRef),
                Handle(5, TableIndex.MethodDef),
                Handle(3, TableIndex.StandAloneSig),
                Handle(3, TableIndex.AssemblyRef));
        }

        [Fact]
        public void AddThenModifyMethod_EmbeddedAttributes()
        {
            var source0 =
@"
namespace System.Runtime.CompilerServices { class X { } }
namespace N
{
    class C
    {
        static void Main() { }
    }
}
";
            var source1 =
@"
namespace System.Runtime.CompilerServices { class X { } }
namespace N
{
    struct C
    {
        static void Main() 
        { 
            Id(in G());
        }

        static ref readonly int Id(in int x) => ref x;
        static ref readonly int G() => ref new int[1] { 1 }[0];
    }
}";
            var source2 =
@"
namespace System.Runtime.CompilerServices { class X { } }
namespace N
{
    struct C
    {
        static void Main() { Id(in G()); }

        static ref readonly int Id(in int x) => ref x;
        static ref readonly int G() => ref new int[1] { 2 }[0];
        static void H(string? s) {}
    }
}";
            var source3 =
@"
namespace System.Runtime.CompilerServices { class X { } }
namespace N
{
    struct C
    {
        static void Main() { Id(in G()); }

        static ref readonly int Id(in int x) => ref x;
        static ref readonly int G() => ref new int[1] { 2 }[0];
        static void H(string? s) {}
        readonly ref readonly string?[]? F() => throw null;
    }
}";

            var compilation0 = CreateCompilation(source0, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1);
            var compilation2 = compilation1.WithSource(source2);
            var compilation3 = compilation2.WithSource(source3);

            var main0 = compilation0.GetMember<MethodSymbol>("N.C.Main");
            var main1 = compilation1.GetMember<MethodSymbol>("N.C.Main");
            var id1 = compilation1.GetMember<MethodSymbol>("N.C.Id");
            var g1 = compilation1.GetMember<MethodSymbol>("N.C.G");
            var g2 = compilation2.GetMember<MethodSymbol>("N.C.G");
            var h2 = compilation2.GetMember<MethodSymbol>("N.C.H");
            var f3 = compilation3.GetMember<MethodSymbol>("N.C.F");

            // Verify full metadata contains expected rows.
            using var md0 = ModuleMetadata.CreateFromImage(compilation0.EmitToArray());

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, EmptyLocalsProvider);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, main0, main1),
                    new SemanticEdit(SemanticEditKind.Insert, null, id1),
                    new SemanticEdit(SemanticEditKind.Insert, null, g1)));

            diff1.VerifySynthesizedMembers(
                "<global namespace>: {Microsoft}",
                "Microsoft: {CodeAnalysis}",
                "Microsoft.CodeAnalysis: {EmbeddedAttribute}",
                "System.Runtime.CompilerServices: {IsReadOnlyAttribute}");

            diff1.VerifyIL("N.C.Main", @"
{
  // Code size       13 (0xd)
  .maxstack  1
  IL_0000:  nop
  IL_0001:  call       ""ref readonly int N.C.G()""
  IL_0006:  call       ""ref readonly int N.C.Id(in int)""
  IL_000b:  pop
  IL_000c:  ret
}
");
            diff1.VerifyIL("N.C.Id", @"
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ret
}
");
            diff1.VerifyIL("N.C.G", @"
{
  // Code size       17 (0x11)
  .maxstack  4
  IL_0000:  ldc.i4.1
  IL_0001:  newarr     ""int""
  IL_0006:  dup
  IL_0007:  ldc.i4.0
  IL_0008:  ldc.i4.1
  IL_0009:  stelem.i4
  IL_000a:  ldc.i4.0
  IL_000b:  ldelema    ""int""
  IL_0010:  ret
}
");

            // Verify delta metadata contains expected rows.
            using var md1 = diff1.GetMetadata();

            var reader0 = md0.MetadataReader;
            var reader1 = md1.Reader;
            var readers = new List<MetadataReader>() { reader0, reader1 };

            CheckNames(readers, reader1.GetTypeDefFullNames(), "Microsoft.CodeAnalysis.EmbeddedAttribute", "System.Runtime.CompilerServices.IsReadOnlyAttribute");
            CheckNames(readers, reader1.GetMethodDefNames(), "Main", ".ctor", ".ctor", "Id", "G");

            CheckEncLog(reader1,
                Row(2, TableIndex.AssemblyRef, EditAndContinueOperation.Default),
                Row(5, TableIndex.MemberRef, EditAndContinueOperation.Default),
                Row(6, TableIndex.MemberRef, EditAndContinueOperation.Default),
                Row(6, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(7, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(8, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(9, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(10, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(4, TableIndex.TypeDef, EditAndContinueOperation.Default),
                Row(5, TableIndex.TypeDef, EditAndContinueOperation.Default),
                Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(5, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(2, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                Row(6, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(2, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                Row(7, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(6, TableIndex.MethodDef, EditAndContinueOperation.AddParameter),
                Row(1, TableIndex.Param, EditAndContinueOperation.Default),
                Row(6, TableIndex.MethodDef, EditAndContinueOperation.AddParameter),
                Row(2, TableIndex.Param, EditAndContinueOperation.Default),
                Row(7, TableIndex.MethodDef, EditAndContinueOperation.AddParameter),
                Row(3, TableIndex.Param, EditAndContinueOperation.Default),
                Row(4, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(5, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(6, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(7, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(8, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(9, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(10, TableIndex.CustomAttribute, EditAndContinueOperation.Default));

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, g1, g2),
                    new SemanticEdit(SemanticEditKind.Insert, null, h2)));

            // synthesized member for nullable annotations added:
            diff2.VerifySynthesizedMembers(
                "<global namespace>: {Microsoft}",
                "Microsoft: {CodeAnalysis}",
                "Microsoft.CodeAnalysis: {EmbeddedAttribute}",
                "System.Runtime.CompilerServices: {IsReadOnlyAttribute, NullableAttribute, NullableContextAttribute}");

            // Verify delta metadata contains expected rows.
            using var md2 = diff2.GetMetadata();

            var reader2 = md2.Reader;
            readers.Add(reader2);

            // note: NullableAttribute has 2 ctors, NullableContextAttribute has one
            CheckNames(readers, reader2.GetTypeDefFullNames(), "System.Runtime.CompilerServices.NullableAttribute", "System.Runtime.CompilerServices.NullableContextAttribute");
            CheckNames(readers, reader2.GetMethodDefNames(), "G", ".ctor", ".ctor", ".ctor", "H");

            // two new TypeDefs emitted for the attributes:
            CheckEncLog(reader2,
                Row(3, TableIndex.AssemblyRef, EditAndContinueOperation.Default),
                Row(7, TableIndex.MemberRef, EditAndContinueOperation.Default),
                Row(8, TableIndex.MemberRef, EditAndContinueOperation.Default),
                Row(9, TableIndex.MemberRef, EditAndContinueOperation.Default),
                Row(11, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(12, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(13, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(14, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(15, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(16, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(17, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(18, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(6, TableIndex.TypeDef, EditAndContinueOperation.Default), // NullableAttribute
                Row(7, TableIndex.TypeDef, EditAndContinueOperation.Default), // NullableContextAttribute
                Row(6, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                Row(1, TableIndex.Field, EditAndContinueOperation.Default),
                Row(7, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                Row(2, TableIndex.Field, EditAndContinueOperation.Default),
                Row(7, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(6, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                Row(8, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(6, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                Row(9, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(7, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                Row(10, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(2, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                Row(11, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(11, TableIndex.MethodDef, EditAndContinueOperation.AddParameter),
                Row(4, TableIndex.Param, EditAndContinueOperation.Default),
                Row(11, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(12, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(13, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(14, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(15, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(16, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(17, TableIndex.CustomAttribute, EditAndContinueOperation.Default));

            CheckEncMap(reader2,
                Handle(11, TableIndex.TypeRef),
                Handle(12, TableIndex.TypeRef),
                Handle(13, TableIndex.TypeRef),
                Handle(14, TableIndex.TypeRef),
                Handle(15, TableIndex.TypeRef),
                Handle(16, TableIndex.TypeRef),
                Handle(17, TableIndex.TypeRef),
                Handle(18, TableIndex.TypeRef),
                Handle(6, TableIndex.TypeDef),
                Handle(7, TableIndex.TypeDef),
                Handle(1, TableIndex.Field),
                Handle(2, TableIndex.Field),
                Handle(7, TableIndex.MethodDef),
                Handle(8, TableIndex.MethodDef),
                Handle(9, TableIndex.MethodDef),
                Handle(10, TableIndex.MethodDef),
                Handle(11, TableIndex.MethodDef),
                Handle(4, TableIndex.Param),
                Handle(7, TableIndex.MemberRef),
                Handle(8, TableIndex.MemberRef),
                Handle(9, TableIndex.MemberRef),
                Handle(11, TableIndex.CustomAttribute),
                Handle(12, TableIndex.CustomAttribute),
                Handle(13, TableIndex.CustomAttribute),
                Handle(14, TableIndex.CustomAttribute),
                Handle(15, TableIndex.CustomAttribute),
                Handle(16, TableIndex.CustomAttribute),
                Handle(17, TableIndex.CustomAttribute),
                Handle(3, TableIndex.AssemblyRef));

            var diff3 = compilation3.EmitDifference(
               diff2.NextGeneration,
               ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Insert, null, f3)));

            // no change in synthesized members:
            diff3.VerifySynthesizedMembers(
                "<global namespace>: {Microsoft}",
                "Microsoft: {CodeAnalysis}",
                "Microsoft.CodeAnalysis: {EmbeddedAttribute}",
                "System.Runtime.CompilerServices: {IsReadOnlyAttribute, NullableAttribute, NullableContextAttribute}");

            // Verify delta metadata contains expected rows.
            using var md3 = diff3.GetMetadata();

            var reader3 = md3.Reader;
            readers.Add(reader3);

            // no new type defs:
            CheckNames(readers, reader3.GetTypeDefFullNames());
            CheckNames(readers, reader3.GetMethodDefNames(), "F");

            CheckEncLog(reader3,
                Row(4, TableIndex.AssemblyRef, EditAndContinueOperation.Default),
                Row(19, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(20, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(2, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                Row(12, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(12, TableIndex.MethodDef, EditAndContinueOperation.AddParameter),
                Row(5, TableIndex.Param, EditAndContinueOperation.Default),
                Row(18, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(19, TableIndex.CustomAttribute, EditAndContinueOperation.Default));
        }

        [Fact]
        public void AddField()
        {
            var source0 =
@"class C
{
    string F = ""F"";
}";
            var source1 =
@"class C
{
    string F = ""F"";
    string G = ""G"";
}";
            var compilation0 = CreateCompilation(source0, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);

            // Verify full metadata contains expected rows.
            var bytes0 = compilation0.EmitToArray();
            using var md0 = ModuleMetadata.CreateFromImage(bytes0);
            var reader0 = md0.MetadataReader;

            CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "C");
            CheckNames(reader0, reader0.GetFieldDefNames(), "F");
            CheckNames(reader0, reader0.GetMethodDefNames(), ".ctor");

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, EmptyLocalsProvider);
            var method0 = compilation0.GetMember<MethodSymbol>("C..ctor");
            var method1 = compilation1.GetMember<MethodSymbol>("C..ctor");

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Insert, null, compilation1.GetMember<FieldSymbol>("C.G")),
                    new SemanticEdit(SemanticEditKind.Update, method0, method1)));

            // Verify delta metadata contains expected rows.
            using var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;
            var readers = new[] { reader0, reader1 };

            CheckNames(readers, reader1.GetTypeDefNames());
            CheckNames(readers, reader1.GetFieldDefNames(), "G");
            CheckNames(readers, reader1.GetMethodDefNames(), ".ctor");

            CheckEncLog(reader1,
                Row(2, TableIndex.AssemblyRef, EditAndContinueOperation.Default),
                Row(5, TableIndex.MemberRef, EditAndContinueOperation.Default),
                Row(6, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(2, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                Row(2, TableIndex.Field, EditAndContinueOperation.Default),
                Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default));

            CheckEncMap(reader1,
                Handle(6, TableIndex.TypeRef),
                Handle(2, TableIndex.Field),
                Handle(1, TableIndex.MethodDef),
                Handle(5, TableIndex.MemberRef),
                Handle(2, TableIndex.AssemblyRef));
        }

        [Fact]
        public void ModifyProperty()
        {
            var source0 =
@"class C
{
    object P { get { return 1; } }
}";
            var source1 =
@"class C
{
    object P { get { return 2; } }
}";
            var compilation0 = CreateCompilation(source0, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);

            var getP0 = compilation0.GetMember<MethodSymbol>("C.get_P");
            var getP1 = compilation1.GetMember<MethodSymbol>("C.get_P");

            var bytes0 = compilation0.EmitToArray();
            using var md0 = ModuleMetadata.CreateFromImage(bytes0);
            var reader0 = md0.MetadataReader;

            CheckNames(reader0, reader0.GetPropertyDefNames(), "P");
            CheckNames(reader0, reader0.GetMethodDefNames(), "get_P", ".ctor");
            var generation0 = EmitBaseline.CreateInitialBaseline(md0, EmptyLocalsProvider);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, getP0, getP1)));

            using var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;
            var readers = new[] { reader0, reader1 };

            CheckNames(readers, reader1.GetPropertyDefNames(), "P");
            CheckNames(readers, reader1.GetMethodDefNames(), "get_P");

            CheckEncLog(reader1,
                Row(2, TableIndex.AssemblyRef, EditAndContinueOperation.Default),
                Row(7, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(8, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(2, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(1, TableIndex.Property, EditAndContinueOperation.Default),
                Row(2, TableIndex.MethodSemantics, EditAndContinueOperation.Default));

            CheckEncMap(reader1,
                Handle(7, TableIndex.TypeRef),
                Handle(8, TableIndex.TypeRef),
                Handle(1, TableIndex.MethodDef),
                Handle(2, TableIndex.StandAloneSig),
                Handle(1, TableIndex.Property),
                Handle(2, TableIndex.MethodSemantics),
                Handle(2, TableIndex.AssemblyRef));
        }

        [Fact]
        public void AddProperty()
        {
            var source0 =
@"class A
{
    object P { get; set; }
}
class B
{
}";
            var source1 =
@"class A
{
    object P { get; set; }
}
class B
{
    object R { get { return null; } }
}";
            var source2 =
@"class A
{
    object P { get; set; }
    object Q { get; set; }
}
class B
{
    object R { get { return null; } }
    object S { set { } }
}";
            var compilation0 = CreateCompilation(source0, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);
            var compilation2 = compilation0.WithSource(source2);

            // Verify full metadata contains expected rows.
            var bytes0 = compilation0.EmitToArray();
            using var md0 = ModuleMetadata.CreateFromImage(bytes0);
            var reader0 = md0.MetadataReader;

            CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "A", "B");
            CheckNames(reader0, reader0.GetFieldDefNames(), "<P>k__BackingField");
            CheckNames(reader0, reader0.GetPropertyDefNames(), "P");
            CheckNames(reader0, reader0.GetMethodDefNames(), "get_P", "set_P", ".ctor", ".ctor");

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, EmptyLocalsProvider);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Insert, null, compilation1.GetMember<PropertySymbol>("B.R"))));

            // Verify delta metadata contains expected rows.
            using var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;
            var readers = new List<MetadataReader> { reader0, reader1 };

            CheckNames(readers, reader1.GetTypeDefNames());
            CheckNames(readers, reader1.GetFieldDefNames());
            CheckNames(readers, reader1.GetPropertyDefNames(), "R");
            CheckNames(readers, reader1.GetMethodDefNames(), "get_R");

            CheckEncLog(reader1,
                Row(2, TableIndex.AssemblyRef, EditAndContinueOperation.Default),
                Row(9, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(1, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(2, TableIndex.PropertyMap, EditAndContinueOperation.Default),
                Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(2, TableIndex.PropertyMap, EditAndContinueOperation.AddProperty),
                Row(2, TableIndex.Property, EditAndContinueOperation.Default),
                Row(3, TableIndex.MethodSemantics, EditAndContinueOperation.Default));

            CheckEncMap(reader1,
                Handle(9, TableIndex.TypeRef),
                Handle(5, TableIndex.MethodDef),
                Handle(1, TableIndex.StandAloneSig),
                Handle(2, TableIndex.PropertyMap),
                Handle(2, TableIndex.Property),
                Handle(3, TableIndex.MethodSemantics),
                Handle(2, TableIndex.AssemblyRef));

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Insert, null, compilation2.GetMember<PropertySymbol>("A.Q")),
                    new SemanticEdit(SemanticEditKind.Insert, null, compilation2.GetMember<PropertySymbol>("B.S"))));

            // Verify delta metadata contains expected rows.
            using var md2 = diff2.GetMetadata();
            var reader2 = md2.Reader;
            readers.Add(reader2);

            CheckNames(readers, reader2.GetTypeDefNames());
            CheckNames(readers, reader2.GetFieldDefNames(), "<Q>k__BackingField");
            CheckNames(readers, reader2.GetPropertyDefNames(), "Q", "S");
            CheckNames(readers, reader2.GetMethodDefNames(), "get_Q", "set_Q", "set_S");

            CheckEncLog(reader2,
                Row(3, TableIndex.AssemblyRef, EditAndContinueOperation.Default),
                Row(7, TableIndex.MemberRef, EditAndContinueOperation.Default),
                Row(8, TableIndex.MemberRef, EditAndContinueOperation.Default),
                Row(10, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(11, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(12, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(13, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(2, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                Row(2, TableIndex.Field, EditAndContinueOperation.Default),
                Row(2, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                Row(6, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(2, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                Row(7, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                Row(8, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(1, TableIndex.PropertyMap, EditAndContinueOperation.AddProperty),
                Row(3, TableIndex.Property, EditAndContinueOperation.Default),
                Row(2, TableIndex.PropertyMap, EditAndContinueOperation.AddProperty),
                Row(4, TableIndex.Property, EditAndContinueOperation.Default),
                Row(7, TableIndex.MethodDef, EditAndContinueOperation.AddParameter),
                Row(2, TableIndex.Param, EditAndContinueOperation.Default),
                Row(8, TableIndex.MethodDef, EditAndContinueOperation.AddParameter),
                Row(3, TableIndex.Param, EditAndContinueOperation.Default),
                Row(8, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(9, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(10, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(11, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(4, TableIndex.MethodSemantics, EditAndContinueOperation.Default),
                Row(5, TableIndex.MethodSemantics, EditAndContinueOperation.Default),
                Row(6, TableIndex.MethodSemantics, EditAndContinueOperation.Default));

            CheckEncMap(reader2,
                Handle(10, TableIndex.TypeRef),
                Handle(11, TableIndex.TypeRef),
                Handle(12, TableIndex.TypeRef),
                Handle(13, TableIndex.TypeRef),
                Handle(2, TableIndex.Field),
                Handle(6, TableIndex.MethodDef),
                Handle(7, TableIndex.MethodDef),
                Handle(8, TableIndex.MethodDef),
                Handle(2, TableIndex.Param),
                Handle(3, TableIndex.Param),
                Handle(7, TableIndex.MemberRef),
                Handle(8, TableIndex.MemberRef),
                Handle(8, TableIndex.CustomAttribute),
                Handle(9, TableIndex.CustomAttribute),
                Handle(10, TableIndex.CustomAttribute),
                Handle(11, TableIndex.CustomAttribute),
                Handle(3, TableIndex.Property),
                Handle(4, TableIndex.Property),
                Handle(4, TableIndex.MethodSemantics),
                Handle(5, TableIndex.MethodSemantics),
                Handle(6, TableIndex.MethodSemantics),
                Handle(3, TableIndex.AssemblyRef));
        }

        [Fact]
        public void AddEvent()
        {
            var source0 =
@"delegate void D();
class A
{
    event D E;
}
class B
{
}";
            var source1 =
@"delegate void D();
class A
{
    event D E;
}
class B
{
    event D F;
}";
            var source2 =
@"delegate void D();
class A
{
    event D E;
    event D G;
}
class B
{
    event D F;
    event D H;
}";
            var compilation0 = CreateCompilation(source0, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);
            var compilation2 = compilation0.WithSource(source2);

            // Verify full metadata contains expected rows.
            var bytes0 = compilation0.EmitToArray();
            using var md0 = ModuleMetadata.CreateFromImage(bytes0);
            var reader0 = md0.MetadataReader;

            CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "D", "A", "B");
            CheckNames(reader0, reader0.GetFieldDefNames(), "E");
            CheckNames(reader0, reader0.GetEventDefNames(), "E");
            CheckNames(reader0, reader0.GetMethodDefNames(), ".ctor", "Invoke", "BeginInvoke", "EndInvoke", "add_E", "remove_E", ".ctor", ".ctor");

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, EmptyLocalsProvider);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Insert, null, compilation1.GetMember<EventSymbol>("B.F"))));

            // Verify delta metadata contains expected rows.
            using var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;
            var readers = new List<MetadataReader> { reader0, reader1 };

            CheckNames(readers, reader1.GetTypeDefNames());
            CheckNames(readers, reader1.GetFieldDefNames(), "F");
            CheckNames(readers, reader1.GetMethodDefNames(), "add_F", "remove_F");

            CheckEncLog(reader1,
                Row(2, TableIndex.AssemblyRef, EditAndContinueOperation.Default),
                Row(10, TableIndex.MemberRef, EditAndContinueOperation.Default),
                Row(11, TableIndex.MemberRef, EditAndContinueOperation.Default),
                Row(12, TableIndex.MemberRef, EditAndContinueOperation.Default),
                Row(13, TableIndex.MemberRef, EditAndContinueOperation.Default),
                Row(14, TableIndex.MemberRef, EditAndContinueOperation.Default),
                Row(2, TableIndex.MethodSpec, EditAndContinueOperation.Default),
                Row(14, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(15, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(16, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(17, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(18, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(19, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(2, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(2, TableIndex.EventMap, EditAndContinueOperation.Default),
                Row(2, TableIndex.EventMap, EditAndContinueOperation.AddEvent),
                Row(2, TableIndex.Event, EditAndContinueOperation.Default),
                Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                Row(2, TableIndex.Field, EditAndContinueOperation.Default),
                Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                Row(9, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                Row(10, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(9, TableIndex.MethodDef, EditAndContinueOperation.AddParameter),
                Row(8, TableIndex.Param, EditAndContinueOperation.Default),
                Row(10, TableIndex.MethodDef, EditAndContinueOperation.AddParameter),
                Row(9, TableIndex.Param, EditAndContinueOperation.Default),
                Row(8, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(9, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(10, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(11, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(3, TableIndex.MethodSemantics, EditAndContinueOperation.Default),
                Row(4, TableIndex.MethodSemantics, EditAndContinueOperation.Default));

            CheckEncMap(reader1,
                Handle(14, TableIndex.TypeRef),
                Handle(15, TableIndex.TypeRef),
                Handle(16, TableIndex.TypeRef),
                Handle(17, TableIndex.TypeRef),
                Handle(18, TableIndex.TypeRef),
                Handle(19, TableIndex.TypeRef),
                Handle(2, TableIndex.Field),
                Handle(9, TableIndex.MethodDef),
                Handle(10, TableIndex.MethodDef),
                Handle(8, TableIndex.Param),
                Handle(9, TableIndex.Param),
                Handle(10, TableIndex.MemberRef),
                Handle(11, TableIndex.MemberRef),
                Handle(12, TableIndex.MemberRef),
                Handle(13, TableIndex.MemberRef),
                Handle(14, TableIndex.MemberRef),
                Handle(8, TableIndex.CustomAttribute),
                Handle(9, TableIndex.CustomAttribute),
                Handle(10, TableIndex.CustomAttribute),
                Handle(11, TableIndex.CustomAttribute),
                Handle(2, TableIndex.StandAloneSig),
                Handle(2, TableIndex.EventMap),
                Handle(2, TableIndex.Event),
                Handle(3, TableIndex.MethodSemantics),
                Handle(4, TableIndex.MethodSemantics),
                Handle(2, TableIndex.AssemblyRef),
                Handle(2, TableIndex.MethodSpec));

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Insert, null, compilation2.GetMember<EventSymbol>("A.G")),
                    new SemanticEdit(SemanticEditKind.Insert, null, compilation2.GetMember<EventSymbol>("B.H"))));

            // Verify delta metadata contains expected rows.
            using var md2 = diff2.GetMetadata();
            var reader2 = md2.Reader;

            readers.Add(reader2);
            CheckNames(readers, reader2.GetTypeDefNames());
            CheckNames(readers, reader2.GetFieldDefNames(), "G", "H");
            CheckNames(readers, reader2.GetMethodDefNames(), "add_G", "remove_G", "add_H", "remove_H");

            CheckEncLog(reader2,
                Row(3, TableIndex.AssemblyRef, EditAndContinueOperation.Default),
                Row(15, TableIndex.MemberRef, EditAndContinueOperation.Default),
                Row(16, TableIndex.MemberRef, EditAndContinueOperation.Default),
                Row(17, TableIndex.MemberRef, EditAndContinueOperation.Default),
                Row(18, TableIndex.MemberRef, EditAndContinueOperation.Default),
                Row(19, TableIndex.MemberRef, EditAndContinueOperation.Default),
                Row(3, TableIndex.MethodSpec, EditAndContinueOperation.Default),
                Row(20, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(21, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(22, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(23, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(24, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(25, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(3, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(1, TableIndex.EventMap, EditAndContinueOperation.AddEvent),
                Row(3, TableIndex.Event, EditAndContinueOperation.Default),
                Row(2, TableIndex.EventMap, EditAndContinueOperation.AddEvent),
                Row(4, TableIndex.Event, EditAndContinueOperation.Default),
                Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                Row(3, TableIndex.Field, EditAndContinueOperation.Default),
                Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                Row(4, TableIndex.Field, EditAndContinueOperation.Default),
                Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                Row(11, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                Row(12, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                Row(13, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                Row(14, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(11, TableIndex.MethodDef, EditAndContinueOperation.AddParameter),
                Row(10, TableIndex.Param, EditAndContinueOperation.Default),
                Row(12, TableIndex.MethodDef, EditAndContinueOperation.AddParameter),
                Row(11, TableIndex.Param, EditAndContinueOperation.Default),
                Row(13, TableIndex.MethodDef, EditAndContinueOperation.AddParameter),
                Row(12, TableIndex.Param, EditAndContinueOperation.Default),
                Row(14, TableIndex.MethodDef, EditAndContinueOperation.AddParameter),
                Row(13, TableIndex.Param, EditAndContinueOperation.Default),
                Row(12, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(13, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(14, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(15, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(16, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(17, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(18, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(19, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(5, TableIndex.MethodSemantics, EditAndContinueOperation.Default),
                Row(6, TableIndex.MethodSemantics, EditAndContinueOperation.Default),
                Row(7, TableIndex.MethodSemantics, EditAndContinueOperation.Default),
                Row(8, TableIndex.MethodSemantics, EditAndContinueOperation.Default));

            CheckEncMap(reader2,
                Handle(20, TableIndex.TypeRef),
                Handle(21, TableIndex.TypeRef),
                Handle(22, TableIndex.TypeRef),
                Handle(23, TableIndex.TypeRef),
                Handle(24, TableIndex.TypeRef),
                Handle(25, TableIndex.TypeRef),
                Handle(3, TableIndex.Field),
                Handle(4, TableIndex.Field),
                Handle(11, TableIndex.MethodDef),
                Handle(12, TableIndex.MethodDef),
                Handle(13, TableIndex.MethodDef),
                Handle(14, TableIndex.MethodDef),
                Handle(10, TableIndex.Param),
                Handle(11, TableIndex.Param),
                Handle(12, TableIndex.Param),
                Handle(13, TableIndex.Param),
                Handle(15, TableIndex.MemberRef),
                Handle(16, TableIndex.MemberRef),
                Handle(17, TableIndex.MemberRef),
                Handle(18, TableIndex.MemberRef),
                Handle(19, TableIndex.MemberRef),
                Handle(12, TableIndex.CustomAttribute),
                Handle(13, TableIndex.CustomAttribute),
                Handle(14, TableIndex.CustomAttribute),
                Handle(15, TableIndex.CustomAttribute),
                Handle(16, TableIndex.CustomAttribute),
                Handle(17, TableIndex.CustomAttribute),
                Handle(18, TableIndex.CustomAttribute),
                Handle(19, TableIndex.CustomAttribute),
                Handle(3, TableIndex.StandAloneSig),
                Handle(3, TableIndex.Event),
                Handle(4, TableIndex.Event),
                Handle(5, TableIndex.MethodSemantics),
                Handle(6, TableIndex.MethodSemantics),
                Handle(7, TableIndex.MethodSemantics),
                Handle(8, TableIndex.MethodSemantics),
                Handle(3, TableIndex.AssemblyRef),
                Handle(3, TableIndex.MethodSpec));
        }

        [WorkItem(1175704, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1175704")]
        [Fact]
        public void EventFields()
        {
            var source0 = MarkedSource(@"
using System;

class C
{
    static event EventHandler handler;

    static int F()
    {
        handler(null, null);
        return 1;
    }
}
");
            var source1 = MarkedSource(@"
using System;

class C
{
    static event EventHandler handler;

    static int F()
    {
        handler(null, null);
        return 10;
    }
}
");
            var compilation0 = CreateCompilation(source0.Tree, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");

            var v0 = CompileAndVerify(compilation0);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, f0, f1, preserveLocalVariables: true)));

            diff1.VerifyIL("C.F", @"
{
  // Code size       21 (0x15)
  .maxstack  3
  .locals init (int V_0)
  IL_0000:  nop
  IL_0001:  ldsfld     ""System.EventHandler C.handler""
  IL_0006:  ldnull
  IL_0007:  ldnull
  IL_0008:  callvirt   ""void System.EventHandler.Invoke(object, System.EventArgs)""
  IL_000d:  nop
  IL_000e:  ldc.i4.s   10
  IL_0010:  stloc.0
  IL_0011:  br.s       IL_0013
  IL_0013:  ldloc.0
  IL_0014:  ret
}
");
        }

        [Fact]
        public void AddNestedTypeAndMembers()
        {
            var source0 =
@"class A
{
    class B { }
    static object F()
    {
        return new B();
    }
}";
            var source1 =
@"class A
{
    class B { }
    class C
    {
        class D { }
        static object F;
        internal static object G()
        {
            return F;
        }
    }
    static object F()
    {
        return C.G();
    }
}";
            var compilation0 = CreateCompilation(source0, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);

            var c1 = compilation1.GetMember<NamedTypeSymbol>("A.C");
            var f0 = compilation0.GetMember<MethodSymbol>("A.F");
            var f1 = compilation1.GetMember<MethodSymbol>("A.F");

            // Verify full metadata contains expected rows.
            var bytes0 = compilation0.EmitToArray();
            using var md0 = ModuleMetadata.CreateFromImage(bytes0);
            var reader0 = md0.MetadataReader;

            CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "A", "B");
            CheckNames(reader0, reader0.GetMethodDefNames(), "F", ".ctor", ".ctor");
            Assert.Equal(1, reader0.GetTableRowCount(TableIndex.NestedClass));

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, EmptyLocalsProvider);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Insert, null, c1),
                    new SemanticEdit(SemanticEditKind.Update, f0, f1)));

            // Verify delta metadata contains expected rows.
            using var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;
            var readers = new[] { reader0, reader1 };

            CheckNames(readers, reader1.GetTypeDefNames(), "C", "D");
            CheckNames(readers, reader1.GetMethodDefNames(), "F", "G", ".ctor", ".ctor");
            Assert.Equal(2, reader1.GetTableRowCount(TableIndex.NestedClass));

            CheckEncLog(reader1,
                Row(2, TableIndex.AssemblyRef, EditAndContinueOperation.Default),
                Row(5, TableIndex.MemberRef, EditAndContinueOperation.Default),
                Row(6, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(2, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(4, TableIndex.TypeDef, EditAndContinueOperation.Default),
                Row(5, TableIndex.TypeDef, EditAndContinueOperation.Default),
                Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                Row(1, TableIndex.Field, EditAndContinueOperation.Default),
                Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(5, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                Row(6, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(2, TableIndex.NestedClass, EditAndContinueOperation.Default),
                Row(3, TableIndex.NestedClass, EditAndContinueOperation.Default));

            CheckEncMap(reader1,
                Handle(6, TableIndex.TypeRef),
                Handle(4, TableIndex.TypeDef),
                Handle(5, TableIndex.TypeDef),
                Handle(1, TableIndex.Field),
                Handle(1, TableIndex.MethodDef),
                Handle(4, TableIndex.MethodDef),
                Handle(5, TableIndex.MethodDef),
                Handle(6, TableIndex.MethodDef),
                Handle(5, TableIndex.MemberRef),
                Handle(2, TableIndex.StandAloneSig),
                Handle(2, TableIndex.AssemblyRef),
                Handle(2, TableIndex.NestedClass),
                Handle(3, TableIndex.NestedClass));
        }

        /// <summary>
        /// Nested types should be emitted in the
        /// same order as full emit.
        /// </summary>
        [Fact]
        public void AddNestedTypesOrder()
        {
            var source0 =
@"class A
{
    class B1
    {
        class C1 { }
    }
    class B2
    {
        class C2 { }
    }
}";
            var source1 =
@"class A
{
    class B1
    {
        class C1 { }
    }
    class B2
    {
        class C2 { }
    }
    class B3
    {
        class C3 { }
    }
    class B4
    {
        class C4 { }
    }
}";
            var compilation0 = CreateCompilation(source0, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);

            var bytes0 = compilation0.EmitToArray();
            using var md0 = ModuleMetadata.CreateFromImage(bytes0);
            var reader0 = md0.MetadataReader;
            CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "A", "B1", "B2", "C1", "C2");
            Assert.Equal(4, reader0.GetTableRowCount(TableIndex.NestedClass));

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, EmptyLocalsProvider);
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Insert, null, compilation1.GetMember<NamedTypeSymbol>("A.B3")),
                    new SemanticEdit(SemanticEditKind.Insert, null, compilation1.GetMember<NamedTypeSymbol>("A.B4"))));

            using var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;
            var readers = new[] { reader0, reader1 };
            CheckNames(readers, reader1.GetTypeDefNames(), "B3", "B4", "C3", "C4");
            Assert.Equal(4, reader1.GetTableRowCount(TableIndex.NestedClass));
        }

        [Fact]
        public void AddNestedGenericType()
        {
            var source0 =
@"class A
{
    class B<T>
    {
    }
    static object F()
    {
        return null;
    }
}";
            var source1 =
@"class A
{
    class B<T>
    {
        internal class C<U>
        {
            internal object F<V>() where V : T, new()
            {
                return new C<V>();
            }
        }
    }
    static object F()
    {
        return new B<A>.C<B<object>>().F<A>();
    }
}";
            var compilation0 = CreateCompilation(source0, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);

            var f0 = compilation0.GetMember<MethodSymbol>("A.F");
            var f1 = compilation1.GetMember<MethodSymbol>("A.F");
            var c1 = compilation1.GetMember<NamedTypeSymbol>("A.B.C");

            // Verify full metadata contains expected rows.
            var bytes0 = compilation0.EmitToArray();
            using var md0 = ModuleMetadata.CreateFromImage(bytes0);
            var reader0 = md0.MetadataReader;
            CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "A", "B`1");
            Assert.Equal(1, reader0.GetTableRowCount(TableIndex.NestedClass));

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, EmptyLocalsProvider);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Insert, null, c1),
                    new SemanticEdit(SemanticEditKind.Update, f0, f1)));

            // Verify delta metadata contains expected rows.
            using var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;
            var readers = new[] { reader0, reader1 };

            CheckNames(readers, reader1.GetTypeDefNames(), "C`1");
            Assert.Equal(1, reader1.GetTableRowCount(TableIndex.NestedClass));

            CheckEncLog(reader1,
                Row(2, TableIndex.AssemblyRef, EditAndContinueOperation.Default),
                Row(5, TableIndex.MemberRef, EditAndContinueOperation.Default),
                Row(6, TableIndex.MemberRef, EditAndContinueOperation.Default),
                Row(7, TableIndex.MemberRef, EditAndContinueOperation.Default),
                Row(8, TableIndex.MemberRef, EditAndContinueOperation.Default),
                Row(1, TableIndex.MethodSpec, EditAndContinueOperation.Default),
                Row(6, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(1, TableIndex.TypeSpec, EditAndContinueOperation.Default),
                Row(2, TableIndex.TypeSpec, EditAndContinueOperation.Default),
                Row(3, TableIndex.TypeSpec, EditAndContinueOperation.Default),
                Row(2, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(4, TableIndex.TypeDef, EditAndContinueOperation.Default),
                Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(2, TableIndex.NestedClass, EditAndContinueOperation.Default),
                Row(2, TableIndex.GenericParam, EditAndContinueOperation.Default),
                Row(3, TableIndex.GenericParam, EditAndContinueOperation.Default),
                Row(4, TableIndex.GenericParam, EditAndContinueOperation.Default),
                Row(1, TableIndex.GenericParamConstraint, EditAndContinueOperation.Default));

            CheckEncMap(reader1,
                Handle(6, TableIndex.TypeRef),
                Handle(4, TableIndex.TypeDef),
                Handle(1, TableIndex.MethodDef),
                Handle(4, TableIndex.MethodDef),
                Handle(5, TableIndex.MethodDef),
                Handle(5, TableIndex.MemberRef),
                Handle(6, TableIndex.MemberRef),
                Handle(7, TableIndex.MemberRef),
                Handle(8, TableIndex.MemberRef),
                Handle(2, TableIndex.StandAloneSig),
                Handle(1, TableIndex.TypeSpec),
                Handle(2, TableIndex.TypeSpec),
                Handle(3, TableIndex.TypeSpec),
                Handle(2, TableIndex.AssemblyRef),
                Handle(2, TableIndex.NestedClass),
                Handle(2, TableIndex.GenericParam),
                Handle(3, TableIndex.GenericParam),
                Handle(4, TableIndex.GenericParam),
                Handle(1, TableIndex.MethodSpec),
                Handle(1, TableIndex.GenericParamConstraint));
        }

        [Fact]
        public void AddNamespace()
        {
            var source0 =
@"
class C
{
    static void Main() { }
}";
            var source1 =
@"
namespace N
{
    class D { public static void F() { } } 
}

class C
{
    static void Main() => N.D.F();
}";
            var source2 =
@"
namespace N
{
    class D { public static void F() { } } 

    namespace M
    {
        class E { public static void G() { } } 
    }
}

class C
{
    static void Main() => N.M.E.G();
}";
            var compilation0 = CreateCompilation(source0, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1);
            var compilation2 = compilation1.WithSource(source2);

            var main0 = compilation0.GetMember<MethodSymbol>("C.Main");
            var main1 = compilation1.GetMember<MethodSymbol>("C.Main");
            var main2 = compilation2.GetMember<MethodSymbol>("C.Main");
            var d1 = compilation1.GetMember<NamedTypeSymbol>("N.D");
            var e2 = compilation2.GetMember<NamedTypeSymbol>("N.M.E");

            using var md0 = ModuleMetadata.CreateFromImage(compilation0.EmitToArray());

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, EmptyLocalsProvider);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, main0, main1),
                    new SemanticEdit(SemanticEditKind.Insert, null, d1)));

            diff1.VerifyIL("C.Main", @"
{
  // Code size        7 (0x7)
  .maxstack  0
  IL_0000:  call       ""void N.D.F()""
  IL_0005:  nop
  IL_0006:  ret
}");
            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, main1, main2),
                    new SemanticEdit(SemanticEditKind.Insert, null, e2)));

            diff2.VerifyIL("C.Main", @"
{
  // Code size        7 (0x7)
  .maxstack  0
  IL_0000:  call       ""void N.M.E.G()""
  IL_0005:  nop
  IL_0006:  ret
}");
        }

        [Fact]
        public void ModifyExplicitImplementation()
        {
            var source =
@"interface I
{
    void M();
}
class C : I
{
    void I.M() { }
}";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source);

            var method0 = compilation0.GetMember<NamedTypeSymbol>("C").GetMethod("I.M");
            var method1 = compilation1.GetMember<NamedTypeSymbol>("C").GetMethod("I.M");

            // Verify full metadata contains expected rows.
            var bytes0 = compilation0.EmitToArray();
            using var md0 = ModuleMetadata.CreateFromImage(bytes0);
            var reader0 = md0.MetadataReader;

            CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "I", "C");
            CheckNames(reader0, reader0.GetMethodDefNames(), "M", "I.M", ".ctor");

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, EmptyLocalsProvider);
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1)));

            // Verify delta metadata contains expected rows.
            using var block1 = diff1.GetMetadata();
            var reader1 = block1.Reader;
            var readers = new[] { reader0, reader1 };
            EncValidation.VerifyModuleMvid(1, reader0, reader1);

            CheckNames(readers, reader1.GetTypeDefNames());
            CheckNames(readers, reader1.GetMethodDefNames(), "I.M");

            CheckEncLog(reader1,
                Row(2, TableIndex.AssemblyRef, EditAndContinueOperation.Default),
                Row(6, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default));

            CheckEncMap(reader1,
                Handle(6, TableIndex.TypeRef),
                Handle(2, TableIndex.MethodDef),
                Handle(2, TableIndex.AssemblyRef));
        }

        [Fact]
        public void AddThenModifyExplicitImplementation()
        {
            var source0 =
@"interface I
{
    void M();
}
class A : I
{
    void I.M() { }
}
class B : I
{
    public void M() { }
}";
            var source1 =
@"interface I
{
    void M();
}
class A : I
{
    void I.M() { }
}
class B : I
{
    public void M() { }
    void I.M() { }
}";
            var source2 = source1;
            var compilation0 = CreateCompilation(source0, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);
            var compilation2 = compilation0.WithSource(source2);

            var method1 = compilation1.GetMember<NamedTypeSymbol>("B").GetMethod("I.M");
            var method2 = compilation2.GetMember<NamedTypeSymbol>("B").GetMethod("I.M");

            var bytes0 = compilation0.EmitToArray();
            using var md0 = ModuleMetadata.CreateFromImage(bytes0);
            var reader0 = md0.MetadataReader;
            var generation0 = EmitBaseline.CreateInitialBaseline(md0, EmptyLocalsProvider);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Insert, null, method1)));

            using var block1 = diff1.GetMetadata();
            var reader1 = block1.Reader;
            var readers = new List<MetadataReader> { reader0, reader1 };

            EncValidation.VerifyModuleMvid(1, reader0, reader1);

            CheckNames(readers, reader1.GetMethodDefNames(), "I.M");

            CheckEncLog(reader1,
                Row(2, TableIndex.AssemblyRef, EditAndContinueOperation.Default),
                Row(6, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                Row(6, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(2, TableIndex.MethodImpl, EditAndContinueOperation.Default));

            CheckEncMap(reader1,
                Handle(6, TableIndex.TypeRef),
                Handle(6, TableIndex.MethodDef),
                Handle(2, TableIndex.MethodImpl),
                Handle(2, TableIndex.AssemblyRef));

            var generation1 = diff1.NextGeneration;
            var diff2 = compilation2.EmitDifference(
                generation1,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method1, method2)));

            using var md2 = diff2.GetMetadata();
            var reader2 = md2.Reader;
            readers.Add(reader2);
            EncValidation.VerifyModuleMvid(2, reader1, reader2);

            CheckNames(readers, reader2.GetMethodDefNames(), "I.M");

            CheckEncLog(reader2,
                Row(3, TableIndex.AssemblyRef, EditAndContinueOperation.Default),
                Row(7, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(6, TableIndex.MethodDef, EditAndContinueOperation.Default));

            CheckEncMap(reader2,
                Handle(7, TableIndex.TypeRef),
                Handle(6, TableIndex.MethodDef),
                Handle(3, TableIndex.AssemblyRef));
        }

        [WorkItem(930065, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/930065")]
        [Fact]
        public void ModifyConstructorBodyInPresenceOfExplicitInterfaceImplementation()
        {
            var source = @"
interface I
{
    void M();
}
class C : I
{
    public C()
    {
    }
    void I.M() { }
}
";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source);

            var method0 = compilation0.GetMember<NamedTypeSymbol>("C").InstanceConstructors.Single();
            var method1 = compilation1.GetMember<NamedTypeSymbol>("C").InstanceConstructors.Single();

            var bytes0 = compilation0.EmitToArray();
            using var md0 = ModuleMetadata.CreateFromImage(bytes0);
            var reader0 = md0.MetadataReader;

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, EmptyLocalsProvider);
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1)));

            using var block1 = diff1.GetMetadata();
            var reader1 = block1.Reader;
            var readers = new[] { reader0, reader1 };
            EncValidation.VerifyModuleMvid(1, reader0, reader1);

            CheckNames(readers, reader1.GetTypeDefNames());
            CheckNames(readers, reader1.GetMethodDefNames(), ".ctor");

            CheckEncLog(reader1,
                Row(2, TableIndex.AssemblyRef, EditAndContinueOperation.Default),
                Row(5, TableIndex.MemberRef, EditAndContinueOperation.Default),
                Row(6, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default));

            CheckEncMap(reader1,
                Handle(6, TableIndex.TypeRef),
                Handle(2, TableIndex.MethodDef),
                Handle(5, TableIndex.MemberRef),
                Handle(2, TableIndex.AssemblyRef));
        }

        [Fact]
        public void AddAndModifyInterfaceMembers()
        {
            var source0 = @"
using System;
interface I
{
}";
            var source1 = @"
using System;
interface I
{
    static int X = 10;
    static event Action Y;

    static void M() { }
    void N() { }

    static int P { get => 1; set { } }
    int Q { get => 1; set { } }

    static event Action E { add { } remove { } }
    event Action F { add { } remove { } }

    interface J { }
}";
            var source2 = @"
using System;
interface I
{
    static int X = 2;
    static event Action Y;

    static I() { X--; }

    static void M() { X++; }
    void N() { X++; }

    static int P { get => 3; set { X++; } }
    int Q { get => 3; set { X++; } }

    static event Action E { add { X++; } remove { X++; } }
    event Action F { add { X++; } remove { X++; } }

    interface J { }
}";
            var compilation0 = CreateCompilation(source0, options: TestOptions.DebugDll, targetFramework: TargetFramework.NetCoreApp30);
            var compilation1 = compilation0.WithSource(source1);
            var compilation2 = compilation1.WithSource(source2);

            var x1 = compilation1.GetMember<FieldSymbol>("I.X");
            var y1 = compilation1.GetMember<EventSymbol>("I.Y");
            var m1 = compilation1.GetMember<MethodSymbol>("I.M");
            var n1 = compilation1.GetMember<MethodSymbol>("I.N");
            var p1 = compilation1.GetMember<PropertySymbol>("I.P");
            var q1 = compilation1.GetMember<PropertySymbol>("I.Q");
            var e1 = compilation1.GetMember<EventSymbol>("I.E");
            var f1 = compilation1.GetMember<EventSymbol>("I.F");
            var j1 = compilation1.GetMember<NamedTypeSymbol>("I.J");
            var getP1 = compilation1.GetMember<MethodSymbol>("I.get_P");
            var setP1 = compilation1.GetMember<MethodSymbol>("I.set_P");
            var getQ1 = compilation1.GetMember<MethodSymbol>("I.get_Q");
            var setQ1 = compilation1.GetMember<MethodSymbol>("I.set_Q");
            var addE1 = compilation1.GetMember<MethodSymbol>("I.add_E");
            var removeE1 = compilation1.GetMember<MethodSymbol>("I.remove_E");
            var addF1 = compilation1.GetMember<MethodSymbol>("I.add_F");
            var removeF1 = compilation1.GetMember<MethodSymbol>("I.remove_F");
            var cctor1 = compilation1.GetMember<NamedTypeSymbol>("I").StaticConstructors.Single();

            var x2 = compilation2.GetMember<FieldSymbol>("I.X");
            var m2 = compilation2.GetMember<MethodSymbol>("I.M");
            var n2 = compilation2.GetMember<MethodSymbol>("I.N");
            var getP2 = compilation2.GetMember<MethodSymbol>("I.get_P");
            var setP2 = compilation2.GetMember<MethodSymbol>("I.set_P");
            var getQ2 = compilation2.GetMember<MethodSymbol>("I.get_Q");
            var setQ2 = compilation2.GetMember<MethodSymbol>("I.set_Q");
            var addE2 = compilation2.GetMember<MethodSymbol>("I.add_E");
            var removeE2 = compilation2.GetMember<MethodSymbol>("I.remove_E");
            var addF2 = compilation2.GetMember<MethodSymbol>("I.add_F");
            var removeF2 = compilation2.GetMember<MethodSymbol>("I.remove_F");
            var cctor2 = compilation2.GetMember<NamedTypeSymbol>("I").StaticConstructors.Single();

            var bytes0 = compilation0.EmitToArray();
            using var md0 = ModuleMetadata.CreateFromImage(bytes0);
            var reader0 = md0.MetadataReader;

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, EmptyLocalsProvider);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Insert, null, x1),
                    new SemanticEdit(SemanticEditKind.Insert, null, y1),
                    new SemanticEdit(SemanticEditKind.Insert, null, m1),
                    new SemanticEdit(SemanticEditKind.Insert, null, n1),
                    new SemanticEdit(SemanticEditKind.Insert, null, p1),
                    new SemanticEdit(SemanticEditKind.Insert, null, q1),
                    new SemanticEdit(SemanticEditKind.Insert, null, e1),
                    new SemanticEdit(SemanticEditKind.Insert, null, f1),
                    new SemanticEdit(SemanticEditKind.Insert, null, j1),
                    new SemanticEdit(SemanticEditKind.Insert, null, cctor1)));

            using var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;
            var readers = new[] { reader0, reader1 };

            CheckNames(readers, reader1.GetTypeDefNames(), "J");
            CheckNames(readers, reader1.GetFieldDefNames(), "X", "Y");
            CheckNames(readers, reader1.GetMethodDefNames(), "add_Y", "remove_Y", "M", "N", "get_P", "set_P", "get_Q", "set_Q", "add_E", "remove_E", "add_F", "remove_F", ".cctor");
            Assert.Equal(1, reader1.GetTableRowCount(TableIndex.NestedClass));

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, x1, x2),
                    new SemanticEdit(SemanticEditKind.Update, m1, m2),
                    new SemanticEdit(SemanticEditKind.Update, n1, n2),
                    new SemanticEdit(SemanticEditKind.Update, getP1, getP2),
                    new SemanticEdit(SemanticEditKind.Update, setP1, setP2),
                    new SemanticEdit(SemanticEditKind.Update, getQ1, getQ2),
                    new SemanticEdit(SemanticEditKind.Update, setQ1, setQ2),
                    new SemanticEdit(SemanticEditKind.Update, addE1, addE2),
                    new SemanticEdit(SemanticEditKind.Update, removeE1, removeE2),
                    new SemanticEdit(SemanticEditKind.Update, addF1, addF2),
                    new SemanticEdit(SemanticEditKind.Update, removeF1, removeF2),
                    new SemanticEdit(SemanticEditKind.Update, cctor1, cctor2)));

            using var md2 = diff2.GetMetadata();
            var reader2 = md2.Reader;
            readers = new[] { reader0, reader1, reader2 };

            CheckNames(readers, reader2.GetTypeDefNames());
            CheckNames(readers, reader2.GetFieldDefNames(), "X");
            CheckNames(readers, reader2.GetMethodDefNames(), "M", "N", "get_P", "set_P", "get_Q", "set_Q", "add_E", "remove_E", "add_F", "remove_F", ".cctor");
            Assert.Equal(0, reader2.GetTableRowCount(TableIndex.NestedClass));

            CheckEncLog(reader2,
                Row(3, TableIndex.AssemblyRef, EditAndContinueOperation.Default),
                Row(8, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(2, TableIndex.Event, EditAndContinueOperation.Default),
                Row(3, TableIndex.Event, EditAndContinueOperation.Default),
                Row(1, TableIndex.Field, EditAndContinueOperation.Default),
                Row(3, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(6, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(7, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(8, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(9, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(10, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(11, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(12, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(13, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(1, TableIndex.Property, EditAndContinueOperation.Default),
                Row(2, TableIndex.Property, EditAndContinueOperation.Default),
                Row(11, TableIndex.MethodSemantics, EditAndContinueOperation.Default),
                Row(12, TableIndex.MethodSemantics, EditAndContinueOperation.Default),
                Row(13, TableIndex.MethodSemantics, EditAndContinueOperation.Default),
                Row(14, TableIndex.MethodSemantics, EditAndContinueOperation.Default),
                Row(15, TableIndex.MethodSemantics, EditAndContinueOperation.Default),
                Row(16, TableIndex.MethodSemantics, EditAndContinueOperation.Default),
                Row(17, TableIndex.MethodSemantics, EditAndContinueOperation.Default),
                Row(18, TableIndex.MethodSemantics, EditAndContinueOperation.Default));

            diff2.VerifyIL(@"
{
  // Code size       14 (0xe)
  .maxstack  8
  IL_0000:  nop
  IL_0001:  ldsfld     0x04000001
  IL_0006:  ldc.i4.1
  IL_0007:  add
  IL_0008:  stsfld     0x04000001
  IL_000d:  ret
}
{
  // Code size        2 (0x2)
  .maxstack  8
  IL_0000:  ldc.i4.3
  IL_0001:  ret
}
{
  // Code size       20 (0x14)
  .maxstack  8
  IL_0000:  ldc.i4.2
  IL_0001:  stsfld     0x04000001
  IL_0006:  nop
  IL_0007:  ldsfld     0x04000001
  IL_000c:  ldc.i4.1
  IL_000d:  sub
  IL_000e:  stsfld     0x04000001
  IL_0013:  ret
}
");
        }

        [Fact]
        public void AddAttributeReferences()
        {
            var source0 =
@"class A : System.Attribute { }
class B : System.Attribute { }
class C
{
    [A] static void M1<[B]T>() { }
    [B] static object F1;
    [A] static object P1 { get { return null; } }
    [B] static event D E1;
}
delegate void D();
";
            var source1 =
@"class A : System.Attribute { }
class B : System.Attribute { }
class C
{
    [A] static void M1<[B]T>() { }
    [B] static void M2<[A]T>() { }
    [B] static object F1;
    [A] static object F2;
    [A] static object P1 { get { return null; } }
    [B] static object P2 { get { return null; } }
    [B] static event D E1;
    [A] static event D E2;
}
delegate void D();
";
            var compilation0 = CreateCompilation(source0, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);

            // Verify full metadata contains expected rows.
            var bytes0 = compilation0.EmitToArray();
            using var md0 = ModuleMetadata.CreateFromImage(bytes0);
            var reader0 = md0.MetadataReader;

            CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "A", "B", "C", "D");
            CheckNames(reader0, reader0.GetMethodDefNames(), ".ctor", ".ctor", "M1", "get_P1", "add_E1", "remove_E1", ".ctor", ".ctor", "Invoke", "BeginInvoke", "EndInvoke");

            CheckAttributes(reader0,
                new CustomAttributeRow(Handle(1, TableIndex.Field), Handle(2, TableIndex.MethodDef)),
                new CustomAttributeRow(Handle(1, TableIndex.Property), Handle(1, TableIndex.MethodDef)),
                new CustomAttributeRow(Handle(1, TableIndex.Event), Handle(2, TableIndex.MethodDef)),
                new CustomAttributeRow(Handle(1, TableIndex.Assembly), Handle(1, TableIndex.MemberRef)),
                new CustomAttributeRow(Handle(1, TableIndex.Assembly), Handle(2, TableIndex.MemberRef)),
                new CustomAttributeRow(Handle(1, TableIndex.Assembly), Handle(3, TableIndex.MemberRef)),
                new CustomAttributeRow(Handle(1, TableIndex.GenericParam), Handle(2, TableIndex.MethodDef)),
                new CustomAttributeRow(Handle(2, TableIndex.Field), Handle(4, TableIndex.MemberRef)),
                new CustomAttributeRow(Handle(2, TableIndex.Field), Handle(5, TableIndex.MemberRef)),
                new CustomAttributeRow(Handle(3, TableIndex.MethodDef), Handle(1, TableIndex.MethodDef)),
                new CustomAttributeRow(Handle(5, TableIndex.MethodDef), Handle(4, TableIndex.MemberRef)),
                new CustomAttributeRow(Handle(6, TableIndex.MethodDef), Handle(4, TableIndex.MemberRef)));

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, EmptyLocalsProvider);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Insert, null, compilation1.GetMember<MethodSymbol>("C.M2")),
                    new SemanticEdit(SemanticEditKind.Insert, null, compilation1.GetMember<FieldSymbol>("C.F2")),
                    new SemanticEdit(SemanticEditKind.Insert, null, compilation1.GetMember<PropertySymbol>("C.P2")),
                    new SemanticEdit(SemanticEditKind.Insert, null, compilation1.GetMember<EventSymbol>("C.E2"))));

            // Verify delta metadata contains expected rows.
            using var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;
            var readers = new[] { reader0, reader1 };

            CheckNames(readers, reader1.GetTypeDefNames());
            CheckNames(readers, reader1.GetMethodDefNames(), "M2", "get_P2", "add_E2", "remove_E2");

            CheckEncLog(reader1,
                Row(2, TableIndex.AssemblyRef, EditAndContinueOperation.Default),
                Row(11, TableIndex.MemberRef, EditAndContinueOperation.Default),
                Row(12, TableIndex.MemberRef, EditAndContinueOperation.Default),
                Row(13, TableIndex.MemberRef, EditAndContinueOperation.Default),
                Row(14, TableIndex.MemberRef, EditAndContinueOperation.Default),
                Row(15, TableIndex.MemberRef, EditAndContinueOperation.Default),
                Row(2, TableIndex.MethodSpec, EditAndContinueOperation.Default),
                Row(15, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(16, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(17, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(18, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(19, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(20, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(3, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(4, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(1, TableIndex.EventMap, EditAndContinueOperation.AddEvent),
                Row(2, TableIndex.Event, EditAndContinueOperation.Default),
                Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                Row(3, TableIndex.Field, EditAndContinueOperation.Default),
                Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                Row(4, TableIndex.Field, EditAndContinueOperation.Default),
                Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                Row(12, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                Row(13, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                Row(14, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                Row(15, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(1, TableIndex.PropertyMap, EditAndContinueOperation.AddProperty),
                Row(2, TableIndex.Property, EditAndContinueOperation.Default),
                Row(14, TableIndex.MethodDef, EditAndContinueOperation.AddParameter),
                Row(8, TableIndex.Param, EditAndContinueOperation.Default),
                Row(15, TableIndex.MethodDef, EditAndContinueOperation.AddParameter),
                Row(9, TableIndex.Param, EditAndContinueOperation.Default),
                Row(13, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(14, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(15, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(16, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(17, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(18, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(19, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(20, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(21, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(4, TableIndex.MethodSemantics, EditAndContinueOperation.Default),
                Row(5, TableIndex.MethodSemantics, EditAndContinueOperation.Default),
                Row(6, TableIndex.MethodSemantics, EditAndContinueOperation.Default),
                Row(2, TableIndex.GenericParam, EditAndContinueOperation.Default));

            CheckEncMap(reader1,
                Handle(15, TableIndex.TypeRef),
                Handle(16, TableIndex.TypeRef),
                Handle(17, TableIndex.TypeRef),
                Handle(18, TableIndex.TypeRef),
                Handle(19, TableIndex.TypeRef),
                Handle(20, TableIndex.TypeRef),
                Handle(3, TableIndex.Field),
                Handle(4, TableIndex.Field),
                Handle(12, TableIndex.MethodDef),
                Handle(13, TableIndex.MethodDef),
                Handle(14, TableIndex.MethodDef),
                Handle(15, TableIndex.MethodDef),
                Handle(8, TableIndex.Param),
                Handle(9, TableIndex.Param),
                Handle(11, TableIndex.MemberRef),
                Handle(12, TableIndex.MemberRef),
                Handle(13, TableIndex.MemberRef),
                Handle(14, TableIndex.MemberRef),
                Handle(15, TableIndex.MemberRef),
                Handle(13, TableIndex.CustomAttribute),
                Handle(14, TableIndex.CustomAttribute),
                Handle(15, TableIndex.CustomAttribute),
                Handle(16, TableIndex.CustomAttribute),
                Handle(17, TableIndex.CustomAttribute),
                Handle(18, TableIndex.CustomAttribute),
                Handle(19, TableIndex.CustomAttribute),
                Handle(20, TableIndex.CustomAttribute),
                Handle(21, TableIndex.CustomAttribute),
                Handle(3, TableIndex.StandAloneSig),
                Handle(4, TableIndex.StandAloneSig),
                Handle(2, TableIndex.Event),
                Handle(2, TableIndex.Property),
                Handle(4, TableIndex.MethodSemantics),
                Handle(5, TableIndex.MethodSemantics),
                Handle(6, TableIndex.MethodSemantics),
                Handle(2, TableIndex.AssemblyRef),
                Handle(2, TableIndex.GenericParam),
                Handle(2, TableIndex.MethodSpec));

            CheckAttributes(reader1,
                new CustomAttributeRow(Handle(1, TableIndex.GenericParam), Handle(1, TableIndex.MethodDef)),
                new CustomAttributeRow(Handle(2, TableIndex.Property), Handle(2, TableIndex.MethodDef)),
                new CustomAttributeRow(Handle(2, TableIndex.Event), Handle(1, TableIndex.MethodDef)),
                new CustomAttributeRow(Handle(3, TableIndex.Field), Handle(1, TableIndex.MethodDef)),
                new CustomAttributeRow(Handle(4, TableIndex.Field), Handle(11, TableIndex.MemberRef)),
                new CustomAttributeRow(Handle(4, TableIndex.Field), Handle(12, TableIndex.MemberRef)),
                new CustomAttributeRow(Handle(12, TableIndex.MethodDef), Handle(2, TableIndex.MethodDef)),
                new CustomAttributeRow(Handle(14, TableIndex.MethodDef), Handle(11, TableIndex.MemberRef)),
                new CustomAttributeRow(Handle(15, TableIndex.MethodDef), Handle(11, TableIndex.MemberRef)));
        }

        /// <summary>
        /// [assembly: ...] and [module: ...] attributes should
        /// not be included in delta metadata.
        /// </summary>
        [Fact]
        public void AssemblyAndModuleAttributeReferences()
        {
            var source0 =
@"[assembly: System.CLSCompliantAttribute(true)]
[module: System.CLSCompliantAttribute(true)]
class C
{
}";
            var source1 =
@"[assembly: System.CLSCompliantAttribute(true)]
[module: System.CLSCompliantAttribute(true)]
class C
{
    static void M()
    {
    }
}";
            var compilation0 = CreateCompilation(source0, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);

            // Verify full metadata contains expected rows.
            var bytes0 = compilation0.EmitToArray();
            using var md0 = ModuleMetadata.CreateFromImage(bytes0);
            var reader0 = md0.MetadataReader;
            CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "C");

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, EmptyLocalsProvider);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Insert, null, compilation1.GetMember<MethodSymbol>("C.M"))));

            // Verify delta metadata contains expected rows.
            using var md1 = diff1.GetMetadata();
            var readers = new[] { reader0, md1.Reader };
            CheckNames(readers, md1.Reader.GetTypeDefNames());
            CheckNames(readers, md1.Reader.GetMethodDefNames(), "M");
            CheckEncLog(md1.Reader,
                Row(2, TableIndex.AssemblyRef, EditAndContinueOperation.Default),
                Row(7, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(2, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default)); // C.M
            CheckEncMap(md1.Reader,
                Handle(7, TableIndex.TypeRef),
                Handle(2, TableIndex.MethodDef),
                Handle(2, TableIndex.AssemblyRef));
        }

        [Fact]
        public void OtherReferences()
        {
            var source0 =
@"delegate void D();
class C
{
    object F;
    object P { get { return null; } }
    event D E;
    void M()
    {
    }
}";
            var source1 =
@"delegate void D();
class C
{
    object F;
    object P { get { return null; } }
    event D E;
    void M()
    {
        object o;
        o = typeof(D);
        o = F;
        o = P;
        E += null;
    }
}";
            var compilation0 = CreateCompilation(source0, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);

            // Verify full metadata contains expected rows.
            var bytes0 = compilation0.EmitToArray();
            using var md0 = ModuleMetadata.CreateFromImage(bytes0);
            var reader0 = md0.MetadataReader;
            CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "D", "C");
            CheckNames(reader0, reader0.GetEventDefNames(), "E");
            CheckNames(reader0, reader0.GetFieldDefNames(), "F", "E");
            CheckNames(reader0, reader0.GetMethodDefNames(), ".ctor", "Invoke", "BeginInvoke", "EndInvoke", "get_P", "add_E", "remove_E", "M", ".ctor");
            CheckNames(reader0, reader0.GetPropertyDefNames(), "P");

            var method0 = compilation0.GetMember<MethodSymbol>("C.M");

            // Emit delta metadata.
            var generation0 = EmitBaseline.CreateInitialBaseline(md0, EmptyLocalsProvider);
            var method1 = compilation1.GetMember<MethodSymbol>("C.M");

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables: true)));

            // Verify delta metadata contains expected rows.
            using var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;
            var readers = new[] { reader0, reader1 };
            CheckNames(readers, reader1.GetTypeDefNames());
            CheckNames(readers, reader1.GetEventDefNames());
            CheckNames(readers, reader1.GetFieldDefNames());
            CheckNames(readers, reader1.GetMethodDefNames(), "M");
            CheckNames(readers, reader1.GetPropertyDefNames());
        }

        [ConditionalFact(typeof(WindowsOnly), Reason = "https://github.com/dotnet/roslyn/issues/37137")]
        public void ArrayInitializer()
        {
            var source0 = @"
class C
{
    static void M()
    {
        int[] a = new[] { 1, 2, 3 };
    }
}";
            var source1 = @"
class C
{
    static void M()
    {
        int[] a = new[] { 1, 2, 3, 4 };
    }
}";
            var compilation0 = CreateCompilation(Parse(source0, "a.cs"), options: TestOptions.DebugDll);
            var compilation1 = compilation0.RemoveAllSyntaxTrees().AddSyntaxTrees(Parse(source1, "a.cs"));

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(testData: testData0);

            var generation0 = EmitBaseline.CreateInitialBaseline(
                ModuleMetadata.CreateFromImage(bytes0),
                testData0.GetMethodData("C.M").EncDebugInfoProvider());

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, compilation0.GetMember("C.M"), compilation1.GetMember("C.M"))));

            var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;

            CheckEncLog(reader1,
                Row(2, TableIndex.AssemblyRef, EditAndContinueOperation.Default),
                Row(12, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(13, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(2, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default));

            CheckEncMap(reader1,
                Handle(12, TableIndex.TypeRef),
                Handle(13, TableIndex.TypeRef),
                Handle(1, TableIndex.MethodDef),
                Handle(2, TableIndex.StandAloneSig),
                Handle(2, TableIndex.AssemblyRef));

            diff1.VerifyIL(
@"{
  // Code size       25 (0x19)
  .maxstack  4
  IL_0000:  nop
  IL_0001:  ldc.i4.4
  IL_0002:  newarr     0x0100000D
  IL_0007:  dup
  IL_0008:  ldc.i4.0
  IL_0009:  ldc.i4.1
  IL_000a:  stelem.i4
  IL_000b:  dup
  IL_000c:  ldc.i4.1
  IL_000d:  ldc.i4.2
  IL_000e:  stelem.i4
  IL_000f:  dup
  IL_0010:  ldc.i4.2
  IL_0011:  ldc.i4.3
  IL_0012:  stelem.i4
  IL_0013:  dup
  IL_0014:  ldc.i4.3
  IL_0015:  ldc.i4.4
  IL_0016:  stelem.i4
  IL_0017:  stloc.0
  IL_0018:  ret
}");

            diff1.VerifyPdb(new[] { 0x06000001 },
@"<symbols>
  <files>
    <file id=""1"" name=""a.cs"" language=""C#"" checksumAlgorithm=""SHA1"" checksum=""15-9B-5B-24-28-37-02-4F-D2-2E-40-DB-1A-89-9F-4D-54-D5-95-89"" />
  </files>
  <methods>
    <method token=""0x6000001"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""5"" startColumn=""5"" endLine=""5"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""6"" startColumn=""9"" endLine=""6"" endColumn=""40"" document=""1"" />
        <entry offset=""0x18"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x19"">
        <local name=""a"" il_index=""0"" il_start=""0x0"" il_end=""0x19"" attributes=""0"" />
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void PInvokeModuleRefAndImplMap()
        {
            var source0 =
@"using System.Runtime.InteropServices;
class C
{
    [DllImport(""msvcrt.dll"")]
    public static extern int getchar();
}";
            var source1 =
@"using System.Runtime.InteropServices;
class C
{
    [DllImport(""msvcrt.dll"")]
    public static extern int getchar();
    [DllImport(""msvcrt.dll"")]
    public static extern int puts(string s);
}";
            var compilation0 = CreateCompilation(source0, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);
            var bytes0 = compilation0.EmitToArray();
            var generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), EmptyLocalsProvider);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Insert, null, compilation1.GetMember<MethodSymbol>("C.puts"))));

            using var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;
            CheckEncLog(reader1,
                Row(2, TableIndex.AssemblyRef, EditAndContinueOperation.Default),
                Row(2, TableIndex.ModuleRef, EditAndContinueOperation.Default),
                Row(6, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(2, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                Row(3, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(3, TableIndex.MethodDef, EditAndContinueOperation.AddParameter),
                Row(1, TableIndex.Param, EditAndContinueOperation.Default),
                Row(2, TableIndex.ImplMap, EditAndContinueOperation.Default));
            CheckEncMap(reader1,
                Handle(6, TableIndex.TypeRef),
                Handle(3, TableIndex.MethodDef),
                Handle(1, TableIndex.Param),
                Handle(2, TableIndex.ModuleRef),
                Handle(2, TableIndex.ImplMap),
                Handle(2, TableIndex.AssemblyRef));
        }

        /// <summary>
        /// ClassLayout and FieldLayout tables.
        /// </summary>
        [Fact]
        public void ClassAndFieldLayout()
        {
            var source0 =
@"using System.Runtime.InteropServices;
[StructLayout(LayoutKind.Explicit, Pack=2)]
class A
{
    [FieldOffset(0)]internal byte F;
    [FieldOffset(2)]internal byte G;
}";
            var source1 =
@"using System.Runtime.InteropServices;
[StructLayout(LayoutKind.Explicit, Pack=2)]
class A
{
    [FieldOffset(0)]internal byte F;
    [FieldOffset(2)]internal byte G;
}
[StructLayout(LayoutKind.Explicit, Pack=4)]
class B
{
    [FieldOffset(0)]internal short F;
    [FieldOffset(4)]internal short G;
}";
            var compilation0 = CreateCompilation(source0, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);
            var bytes0 = compilation0.EmitToArray();
            var generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), EmptyLocalsProvider);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Insert, null, compilation1.GetMember<NamedTypeSymbol>("B"))));

            using var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;
            CheckEncLog(reader1,
                Row(2, TableIndex.AssemblyRef, EditAndContinueOperation.Default),
                Row(5, TableIndex.MemberRef, EditAndContinueOperation.Default),
                Row(6, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(3, TableIndex.TypeDef, EditAndContinueOperation.Default),
                Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                Row(3, TableIndex.Field, EditAndContinueOperation.Default),
                Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                Row(4, TableIndex.Field, EditAndContinueOperation.Default),
                Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(2, TableIndex.ClassLayout, EditAndContinueOperation.Default),
                Row(3, TableIndex.FieldLayout, EditAndContinueOperation.Default),
                Row(4, TableIndex.FieldLayout, EditAndContinueOperation.Default));
            CheckEncMap(reader1,
                Handle(6, TableIndex.TypeRef),
                Handle(3, TableIndex.TypeDef),
                Handle(3, TableIndex.Field),
                Handle(4, TableIndex.Field),
                Handle(2, TableIndex.MethodDef),
                Handle(5, TableIndex.MemberRef),
                Handle(2, TableIndex.ClassLayout),
                Handle(3, TableIndex.FieldLayout),
                Handle(4, TableIndex.FieldLayout),
                Handle(2, TableIndex.AssemblyRef));
        }

        [Fact]
        public void NamespacesAndOverloads()
        {
            var compilation0 = CreateCompilation(options: TestOptions.DebugDll, source:
@"class C { }
namespace N
{
    class C { }
}
namespace M
{
    class C
    {
        void M1(N.C o) { }
        void M1(M.C o) { }
        void M2(N.C a, M.C b, global::C c)
        {
            M1(a);
        }
    }
}");

            var method0 = compilation0.GetMember<MethodSymbol>("M.C.M2");

            var bytes0 = compilation0.EmitToArray();
            var generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), EmptyLocalsProvider);

            var compilation1 = compilation0.WithSource(@"
class C { }
namespace N
{
    class C { }
}
namespace M
{
    class C
    {
        void M1(N.C o) { }
        void M1(M.C o) { }
        void M1(global::C o) { }
        void M2(N.C a, M.C b, global::C c)
        {
            M1(a);
            M1(b);
        }
    }
}");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Insert, null, compilation1.GetMembers("M.C.M1")[2])));

            diff1.VerifyIL(
@"{
  // Code size        2 (0x2)
  .maxstack  8
  IL_0000:  nop
  IL_0001:  ret
}");

            var compilation2 = compilation1.WithSource(@"
class C { }
namespace N
{
    class C { }
}
namespace M
{
    class C
    {
        void M1(N.C o) { }
        void M1(M.C o) { }
        void M1(global::C o) { }
        void M2(N.C a, M.C b, global::C c)
        {
            M1(a);
            M1(b);
            M1(c);
        }
    }
}");
            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, compilation1.GetMember<MethodSymbol>("M.C.M2"),
                                                                        compilation2.GetMember<MethodSymbol>("M.C.M2"))));

            diff2.VerifyIL(
@"{
  // Code size       26 (0x1a)
  .maxstack  8
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldarg.1
  IL_0003:  call       0x06000002
  IL_0008:  nop
  IL_0009:  ldarg.0
  IL_000a:  ldarg.2
  IL_000b:  call       0x06000003
  IL_0010:  nop
  IL_0011:  ldarg.0
  IL_0012:  ldarg.3
  IL_0013:  call       0x06000007
  IL_0018:  nop
  IL_0019:  ret
}");
        }

        [Fact]
        public void TypesAndOverloads()
        {
            const string source =
@"using System;
struct A<T>
{
    internal class B<U> { }
}
class B { }
class C
{
    static void M(A<B>.B<object> a)
    {
        M(a);
        M((A<B>.B<B>)null);
    }
    static void M(A<B>.B<B> a)
    {
        M(a);
        M((A<B>.B<object>)null);
    }
    static void M(A<B> a)
    {
        M(a);
        M((A<B>?)a);
    }
    static void M(Nullable<A<B>> a)
    {
        M(a);
        M(a.Value);
    }
    unsafe static void M(int* p)
    {
        M(p);
        M((byte*)p);
    }
    unsafe static void M(byte* p)
    {
        M(p);
        M((int*)p);
    }
    static void M(B[][] b)
    {
        M(b);
        M((object[][])b);
    }
    static void M(object[][] b)
    {
        M(b);
        M((B[][])b);
    }
    static void M(A<B[]>.B<object> b)
    {
        M(b);
        M((A<B[, ,]>.B<object>)null);
    }
    static void M(A<B[, ,]>.B<object> b)
    {
        M(b);
        M((A<B[]>.B<object>)null);
    }
    static void M(dynamic d)
    {
        M(d);
        M((dynamic[])d);
    }
    static void M(dynamic[] d)
    {
        M(d);
        M((dynamic)d);
    }
    static void M<T>(A<int>.B<T> t) where T : B
    {
        M(t);
        M((A<double>.B<int>)null);
    }
    static void M<T>(A<double>.B<T> t) where T : struct
    {
        M(t);
        M((A<int>.B<B>)null);
    }
}";
            var options = TestOptions.UnsafeDebugDll;
            var compilation0 = CreateCompilation(source, options: options, references: new[] { CSharpRef });
            var bytes0 = compilation0.EmitToArray();
            var generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), EmptyLocalsProvider);

            var n = compilation0.GetMembers("C.M").Length;
            Assert.Equal(n, 14);

            //static void M(A<B>.B<object> a)
            //{
            //    M(a);
            //    M((A<B>.B<B>)null);
            //}
            var compilation1 = compilation0.WithSource(source);
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, compilation0.GetMembers("C.M")[0], compilation1.GetMembers("C.M")[0])));

            diff1.VerifyIL(
@"{
  // Code size       16 (0x10)
  .maxstack  8
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  call       0x06000002
  IL_0007:  nop
  IL_0008:  ldnull
  IL_0009:  call       0x06000003
  IL_000e:  nop
  IL_000f:  ret
}");

            //static void M(A<B>.B<B> a)
            //{
            //    M(a);
            //    M((A<B>.B<object>)null);
            //}
            var compilation2 = compilation1.WithSource(source);
            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, compilation1.GetMembers("C.M")[1], compilation2.GetMembers("C.M")[1])));

            diff2.VerifyIL(
@"{
  // Code size       16 (0x10)
  .maxstack  8
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  call       0x06000003
  IL_0007:  nop
  IL_0008:  ldnull
  IL_0009:  call       0x06000002
  IL_000e:  nop
  IL_000f:  ret
}");

            //static void M(A<B> a)
            //{
            //    M(a);
            //    M((A<B>?)a);
            //}
            var compilation3 = compilation2.WithSource(source);
            var diff3 = compilation3.EmitDifference(
                diff2.NextGeneration,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, compilation2.GetMembers("C.M")[2], compilation3.GetMembers("C.M")[2])));

            diff3.VerifyIL(
@"{
  // Code size       21 (0x15)
  .maxstack  8
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  call       0x06000004
  IL_0007:  nop
  IL_0008:  ldarg.0
  IL_0009:  newobj     0x0A000016
  IL_000e:  call       0x06000005
  IL_0013:  nop
  IL_0014:  ret
}");

            //static void M(Nullable<A<B>> a)
            //{
            //    M(a);
            //    M(a.Value);
            //}
            var compilation4 = compilation3.WithSource(source);
            var diff4 = compilation4.EmitDifference(
                diff3.NextGeneration,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, compilation3.GetMembers("C.M")[3], compilation4.GetMembers("C.M")[3])));

            diff4.VerifyIL(
@"{
  // Code size       22 (0x16)
  .maxstack  8
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  call       0x06000005
  IL_0007:  nop
  IL_0008:  ldarga.s   V_0
  IL_000a:  call       0x0A000017
  IL_000f:  call       0x06000004
  IL_0014:  nop
  IL_0015:  ret
}");

            //unsafe static void M(int* p)
            //{
            //    M(p);
            //    M((byte*)p);
            //}
            var compilation5 = compilation4.WithSource(source);
            var diff5 = compilation5.EmitDifference(
                diff4.NextGeneration,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, compilation4.GetMembers("C.M")[4], compilation5.GetMembers("C.M")[4])));

            diff5.VerifyIL(
@"{
  // Code size       16 (0x10)
  .maxstack  8
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  call       0x06000006
  IL_0007:  nop
  IL_0008:  ldarg.0
  IL_0009:  call       0x06000007
  IL_000e:  nop
  IL_000f:  ret
}");

            //unsafe static void M(byte* p)
            //{
            //    M(p);
            //    M((int*)p);
            //}
            var compilation6 = compilation5.WithSource(source);
            var diff6 = compilation6.EmitDifference(
                diff5.NextGeneration,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, compilation5.GetMembers("C.M")[5], compilation6.GetMembers("C.M")[5])));

            diff6.VerifyIL(
@"{
  // Code size       16 (0x10)
  .maxstack  8
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  call       0x06000007
  IL_0007:  nop
  IL_0008:  ldarg.0
  IL_0009:  call       0x06000006
  IL_000e:  nop
  IL_000f:  ret
}");

            //static void M(B[][] b)
            //{
            //    M(b);
            //    M((object[][])b);
            //}
            var compilation7 = compilation6.WithSource(source);
            var diff7 = compilation7.EmitDifference(
                diff6.NextGeneration,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, compilation6.GetMembers("C.M")[6], compilation7.GetMembers("C.M")[6])));

            diff7.VerifyIL(
@"{
  // Code size       18 (0x12)
  .maxstack  1
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  call       0x06000008
  IL_0007:  nop
  IL_0008:  ldarg.0
  IL_0009:  stloc.0
  IL_000a:  ldloc.0
  IL_000b:  call       0x06000009
  IL_0010:  nop
  IL_0011:  ret
}");

            //static void M(object[][] b)
            //{
            //    M(b);
            //    M((B[][])b);
            //}
            var compilation8 = compilation7.WithSource(source);
            var diff8 = compilation8.EmitDifference(
                diff7.NextGeneration,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, compilation7.GetMembers("C.M")[7], compilation8.GetMembers("C.M")[7])));

            diff8.VerifyIL(
@"{
  // Code size       21 (0x15)
  .maxstack  8
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  call       0x06000009
  IL_0007:  nop
  IL_0008:  ldarg.0
  IL_0009:  castclass  0x1B00000A
  IL_000e:  call       0x06000008
  IL_0013:  nop
  IL_0014:  ret
}");

            //static void M(A<B[]>.B<object> b)
            //{
            //    M(b);
            //    M((A<B[,,]>.B<object>)null);
            //}
            var compilation9 = compilation8.WithSource(source);
            var diff9 = compilation9.EmitDifference(
                diff8.NextGeneration,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, compilation8.GetMembers("C.M")[8], compilation9.GetMembers("C.M")[8])));

            diff9.VerifyIL(
@"{
  // Code size       16 (0x10)
  .maxstack  8
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  call       0x0600000A
  IL_0007:  nop
  IL_0008:  ldnull
  IL_0009:  call       0x0600000B
  IL_000e:  nop
  IL_000f:  ret
}");

            //static void M(A<B[,,]>.B<object> b)
            //{
            //    M(b);
            //    M((A<B[]>.B<object>)null);
            //}
            var compilation10 = compilation9.WithSource(source);
            var diff10 = compilation10.EmitDifference(
                diff9.NextGeneration,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, compilation9.GetMembers("C.M")[9], compilation10.GetMembers("C.M")[9])));

            diff10.VerifyIL(
@"{
  // Code size       16 (0x10)
  .maxstack  8
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  call       0x0600000B
  IL_0007:  nop
  IL_0008:  ldnull
  IL_0009:  call       0x0600000A
  IL_000e:  nop
  IL_000f:  ret
}");

            // TODO: dynamic
#if false
            //static void M(dynamic d)
            //{
            //    M(d);
            //    M((dynamic[])d);
            //}
            previousMethod = compilation.GetMembers("C.M")[10];
            compilation = compilation0.WithSource(source);
            generation = compilation.EmitDifference(
                generation,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, previousMethod, compilation.GetMembers("C.M")[10])),
@"{
  // Code size       16 (0x10)
  .maxstack  8
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  call       0x06000002
  IL_0007:  nop
  IL_0008:  ldnull
  IL_0009:  call       0x06000003
  IL_000e:  nop
  IL_000f:  ret
}");

            //static void M(dynamic[] d)
            //{
            //    M(d);
            //    M((dynamic)d);
            //}
            previousMethod = compilation.GetMembers("C.M")[11];
            compilation = compilation0.WithSource(source);
            generation = compilation.EmitDifference(
                generation,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, previousMethod, compilation.GetMembers("C.M")[11])),
@"{
  // Code size       16 (0x10)
  .maxstack  8
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  call       0x06000002
  IL_0007:  nop
  IL_0008:  ldnull
  IL_0009:  call       0x06000003
  IL_000e:  nop
  IL_000f:  ret
}");
#endif

            //static void M<T>(A<int>.B<T> t) where T : B
            //{
            //    M(t);
            //    M((A<double>.B<int>)null);
            //}
            var compilation11 = compilation10.WithSource(source);
            var diff11 = compilation11.EmitDifference(
                diff10.NextGeneration,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, compilation10.GetMembers("C.M")[12], compilation11.GetMembers("C.M")[12])));

            diff11.VerifyIL(
@"{
  // Code size       16 (0x10)
  .maxstack  8
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  call       0x2B000005
  IL_0007:  nop
  IL_0008:  ldnull
  IL_0009:  call       0x2B000006
  IL_000e:  nop
  IL_000f:  ret
}");

            //static void M<T>(A<double>.B<T> t) where T : struct
            //{
            //    M(t);
            //    M((A<int>.B<B>)null);
            //}
            var compilation12 = compilation11.WithSource(source);
            var diff12 = compilation12.EmitDifference(
                diff11.NextGeneration,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, compilation11.GetMembers("C.M")[13], compilation12.GetMembers("C.M")[13])));

            diff12.VerifyIL(
@"{
  // Code size       16 (0x10)
  .maxstack  8
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  call       0x2B000007
  IL_0007:  nop
  IL_0008:  ldnull
  IL_0009:  call       0x2B000008
  IL_000e:  nop
  IL_000f:  ret
}");
        }

        /// <summary>
        /// Types should be retained in deleted locals
        /// for correct alignment of remaining locals.
        /// </summary>
        [Fact]
        public void DeletedValueTypeLocal()
        {
            var source0 =
@"struct S1
{
    internal S1(int a, int b) { A = a; B = b; }
    internal int A;
    internal int B;
}
struct S2
{
    internal S2(int c) { C = c; }
    internal int C;
}
class C
{
    static void Main()
    {
        var x = new S1(1, 2);
        var y = new S2(3);
        System.Console.WriteLine(y.C);
    }
}";
            var source1 =
@"struct S1
{
    internal S1(int a, int b) { A = a; B = b; }
    internal int A;
    internal int B;
}
struct S2
{
    internal S2(int c) { C = c; }
    internal int C;
}
class C
{
    static void Main()
    {
        var y = new S2(3);
        System.Console.WriteLine(y.C);
    }
}";
            var compilation0 = CreateCompilation(source0, options: TestOptions.DebugExe);
            var compilation1 = compilation0.WithSource(source1);

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(testData: testData0);
            var methodData0 = testData0.GetMethodData("C.Main");
            var method0 = compilation0.GetMember<MethodSymbol>("C.Main");
            var generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), methodData0.EncDebugInfoProvider());
            testData0.GetMethodData("C.Main").VerifyIL(
@"
{
  // Code size       31 (0x1f)
  .maxstack  3
  .locals init (S1 V_0, //x
  S2 V_1) //y
  IL_0000:  nop
  IL_0001:  ldloca.s   V_0
  IL_0003:  ldc.i4.1
  IL_0004:  ldc.i4.2
  IL_0005:  call       ""S1..ctor(int, int)""
  IL_000a:  ldloca.s   V_1
  IL_000c:  ldc.i4.3
  IL_000d:  call       ""S2..ctor(int)""
  IL_0012:  ldloc.1
  IL_0013:  ldfld      ""int S2.C""
  IL_0018:  call       ""void System.Console.WriteLine(int)""
  IL_001d:  nop
  IL_001e:  ret
}");

            var method1 = compilation1.GetMember<MethodSymbol>("C.Main");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables: true)));
            diff1.VerifyIL("C.Main",
 @"{
  // Code size       22 (0x16)
  .maxstack  2
  .locals init ([unchanged] V_0,
  S2 V_1) //y
  IL_0000:  nop
  IL_0001:  ldloca.s   V_1
  IL_0003:  ldc.i4.3
  IL_0004:  call       ""S2..ctor(int)""
  IL_0009:  ldloc.1
  IL_000a:  ldfld      ""int S2.C""
  IL_000f:  call       ""void System.Console.WriteLine(int)""
  IL_0014:  nop
  IL_0015:  ret
}");
        }

        /// <summary>
        /// Instance and static constructors synthesized for
        /// PrivateImplementationDetails should not be
        /// generated for delta.
        /// </summary>
        [Fact]
        public void PrivateImplementationDetails()
        {
            var source =
@"class C
{
    static int[] F = new int[] { 1, 2, 3 };
    int[] G = new int[] { 4, 5, 6 };
    int M(int index)
    {
        return F[index] + G[index];
    }
}";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source);

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(testData: testData0);
            using (var md0 = ModuleMetadata.CreateFromImage(bytes0))
            {
                var reader0 = md0.MetadataReader;
                var typeNames = new[] { reader0 }.GetStrings(reader0.GetTypeDefNames());
                Assert.NotNull(typeNames.FirstOrDefault(n => n.StartsWith("<PrivateImplementationDetails>", StringComparison.Ordinal)));
            }

            var methodData0 = testData0.GetMethodData("C.M");
            var method0 = compilation0.GetMember<MethodSymbol>("C.M");
            var generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), methodData0.EncDebugInfoProvider());

            var method1 = compilation1.GetMember<MethodSymbol>("C.M");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables: true)));

            diff1.VerifyIL("C.M", @"
{
  // Code size       22 (0x16)
  .maxstack  3
  .locals init ([int] V_0,
                int V_1)
  IL_0000:  nop
  IL_0001:  ldsfld     ""int[] C.F""
  IL_0006:  ldarg.1
  IL_0007:  ldelem.i4
  IL_0008:  ldarg.0
  IL_0009:  ldfld      ""int[] C.G""
  IL_000e:  ldarg.1
  IL_000f:  ldelem.i4
  IL_0010:  add
  IL_0011:  stloc.1
  IL_0012:  br.s       IL_0014
  IL_0014:  ldloc.1
  IL_0015:  ret
}");
        }

        [WorkItem(780989, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/780989")]
        [WorkItem(829353, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/829353")]
        [Fact]
        public void PrivateImplementationDetails_ArrayInitializer_FromMetadata()
        {
            var source0 =
@"class C
{
    static void M()
    {
        int[] a = { 1, 2, 3 };
        System.Console.WriteLine(a[0]);
    }
}";
            var source1 =
@"class C
{
    static void M()
    {
        int[] a = { 1, 2, 3 };
        System.Console.WriteLine(a[1]);
    }
}";
            var source2 =
@"class C
{
    static void M()
    {
        int[] a = { 4, 5, 6, 7, 8, 9, 10 };
        System.Console.WriteLine(a[1]);
    }
}";
            var compilation0 = CreateCompilation(source0, options: TestOptions.DebugDll.WithModuleName("MODULE"));
            var compilation1 = compilation0.WithSource(source1);
            var compilation2 = compilation1.WithSource(source2);

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(testData: testData0);
            var methodData0 = testData0.GetMethodData("C.M");

            methodData0.VerifyIL(
@"{
  // Code size       29 (0x1d)
  .maxstack  3
  .locals init (int[] V_0) //a
  IL_0000:  nop
  IL_0001:  ldc.i4.3
  IL_0002:  newarr     ""int""
  IL_0007:  dup
  IL_0008:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=12 <PrivateImplementationDetails>.E429CCA3F703A39CC5954A6572FEC9086135B34E""
  IL_000d:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_0012:  stloc.0
  IL_0013:  ldloc.0
  IL_0014:  ldc.i4.0
  IL_0015:  ldelem.i4
  IL_0016:  call       ""void System.Console.WriteLine(int)""
  IL_001b:  nop
  IL_001c:  ret
}");

            var method0 = compilation0.GetMember<MethodSymbol>("C.M");
            var generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), methodData0.EncDebugInfoProvider());

            var method1 = compilation1.GetMember<MethodSymbol>("C.M");

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables: true)));

            diff1.VerifyIL("C.M",
@"{
  // Code size       30 (0x1e)
  .maxstack  4
  .locals init (int[] V_0) //a
  IL_0000:  nop
  IL_0001:  ldc.i4.3
  IL_0002:  newarr     ""int""
  IL_0007:  dup
  IL_0008:  ldc.i4.0
  IL_0009:  ldc.i4.1
  IL_000a:  stelem.i4
  IL_000b:  dup
  IL_000c:  ldc.i4.1
  IL_000d:  ldc.i4.2
  IL_000e:  stelem.i4
  IL_000f:  dup
  IL_0010:  ldc.i4.2
  IL_0011:  ldc.i4.3
  IL_0012:  stelem.i4
  IL_0013:  stloc.0
  IL_0014:  ldloc.0
  IL_0015:  ldc.i4.1
  IL_0016:  ldelem.i4
  IL_0017:  call       ""void System.Console.WriteLine(int)""
  IL_001c:  nop
  IL_001d:  ret
}");

            var method2 = compilation2.GetMember<MethodSymbol>("C.M");

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method1, method2, GetEquivalentNodesMap(method2, method1), preserveLocalVariables: true)));

            diff2.VerifyIL("C.M",
@"{
  // Code size       48 (0x30)
  .maxstack  4
  .locals init ([unchanged] V_0,
  int[] V_1) //a
  IL_0000:  nop
  IL_0001:  ldc.i4.7
  IL_0002:  newarr     ""int""
  IL_0007:  dup
  IL_0008:  ldc.i4.0
  IL_0009:  ldc.i4.4
  IL_000a:  stelem.i4
  IL_000b:  dup
  IL_000c:  ldc.i4.1
  IL_000d:  ldc.i4.5
  IL_000e:  stelem.i4
  IL_000f:  dup
  IL_0010:  ldc.i4.2
  IL_0011:  ldc.i4.6
  IL_0012:  stelem.i4
  IL_0013:  dup
  IL_0014:  ldc.i4.3
  IL_0015:  ldc.i4.7
  IL_0016:  stelem.i4
  IL_0017:  dup
  IL_0018:  ldc.i4.4
  IL_0019:  ldc.i4.8
  IL_001a:  stelem.i4
  IL_001b:  dup
  IL_001c:  ldc.i4.5
  IL_001d:  ldc.i4.s   9
  IL_001f:  stelem.i4
  IL_0020:  dup
  IL_0021:  ldc.i4.6
  IL_0022:  ldc.i4.s   10
  IL_0024:  stelem.i4
  IL_0025:  stloc.1
  IL_0026:  ldloc.1
  IL_0027:  ldc.i4.1
  IL_0028:  ldelem.i4
  IL_0029:  call       ""void System.Console.WriteLine(int)""
  IL_002e:  nop
  IL_002f:  ret
}");
        }

        [WorkItem(780989, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/780989")]
        [WorkItem(829353, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/829353")]
        [Fact]
        public void PrivateImplementationDetails_ArrayInitializer_FromSource()
        {
            // PrivateImplementationDetails not needed initially.
            var source0 =
@"class C
{
    static object F1() { return null; }
    static object F2() { return null; }
    static object F3() { return null; }
    static object F4() { return null; }
}";
            var source1 =
@"class C
{
    static object F1() { return new[] { 1, 2, 3 }; }
    static object F2() { return new[] { 4, 5, 6 }; }
    static object F3() { return null; }
    static object F4() { return new[] { 7, 8, 9 }; }
}";
            var source2 =
@"class C
{
    static object F1() { return new[] { 1, 2, 3 } ?? new[] { 10, 11, 12 }; }
    static object F2() { return new[] { 4, 5, 6 }; }
    static object F3() { return new[] { 13, 14, 15 }; }
    static object F4() { return new[] { 7, 8, 9 }; }
}";
            var compilation0 = CreateCompilation(source0, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);
            var compilation2 = compilation1.WithSource(source2);

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(testData: testData0);
            var generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), EmptyLocalsProvider);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, compilation0.GetMember<MethodSymbol>("C.F1"), compilation1.GetMember<MethodSymbol>("C.F1")),
                    new SemanticEdit(SemanticEditKind.Update, compilation0.GetMember<MethodSymbol>("C.F2"), compilation1.GetMember<MethodSymbol>("C.F2")),
                    new SemanticEdit(SemanticEditKind.Update, compilation0.GetMember<MethodSymbol>("C.F4"), compilation1.GetMember<MethodSymbol>("C.F4"))));

            diff1.VerifyIL("C.F1",
@"{
  // Code size       24 (0x18)
  .maxstack  4
  .locals init (object V_0)
  IL_0000:  nop
  IL_0001:  ldc.i4.3
  IL_0002:  newarr     ""int""
  IL_0007:  dup
  IL_0008:  ldc.i4.0
  IL_0009:  ldc.i4.1
  IL_000a:  stelem.i4
  IL_000b:  dup
  IL_000c:  ldc.i4.1
  IL_000d:  ldc.i4.2
  IL_000e:  stelem.i4
  IL_000f:  dup
  IL_0010:  ldc.i4.2
  IL_0011:  ldc.i4.3
  IL_0012:  stelem.i4
  IL_0013:  stloc.0
  IL_0014:  br.s       IL_0016
  IL_0016:  ldloc.0
  IL_0017:  ret
}");
            diff1.VerifyIL("C.F4",
@"{
  // Code size       25 (0x19)
  .maxstack  4
  .locals init (object V_0)
  IL_0000:  nop
  IL_0001:  ldc.i4.3
  IL_0002:  newarr     ""int""
  IL_0007:  dup
  IL_0008:  ldc.i4.0
  IL_0009:  ldc.i4.7
  IL_000a:  stelem.i4
  IL_000b:  dup
  IL_000c:  ldc.i4.1
  IL_000d:  ldc.i4.8
  IL_000e:  stelem.i4
  IL_000f:  dup
  IL_0010:  ldc.i4.2
  IL_0011:  ldc.i4.s   9
  IL_0013:  stelem.i4
  IL_0014:  stloc.0
  IL_0015:  br.s       IL_0017
  IL_0017:  ldloc.0
  IL_0018:  ret
}");
            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, compilation1.GetMember<MethodSymbol>("C.F1"), compilation2.GetMember<MethodSymbol>("C.F1")),
                    new SemanticEdit(SemanticEditKind.Update, compilation1.GetMember<MethodSymbol>("C.F3"), compilation2.GetMember<MethodSymbol>("C.F3"))));

            diff2.VerifyIL("C.F1",
@"{
  // Code size       49 (0x31)
  .maxstack  4
  .locals init (object V_0)
  IL_0000:  nop
  IL_0001:  ldc.i4.3
  IL_0002:  newarr     ""int""
  IL_0007:  dup
  IL_0008:  ldc.i4.0
  IL_0009:  ldc.i4.1
  IL_000a:  stelem.i4
  IL_000b:  dup
  IL_000c:  ldc.i4.1
  IL_000d:  ldc.i4.2
  IL_000e:  stelem.i4
  IL_000f:  dup
  IL_0010:  ldc.i4.2
  IL_0011:  ldc.i4.3
  IL_0012:  stelem.i4
  IL_0013:  dup
  IL_0014:  brtrue.s   IL_002c
  IL_0016:  pop
  IL_0017:  ldc.i4.3
  IL_0018:  newarr     ""int""
  IL_001d:  dup
  IL_001e:  ldc.i4.0
  IL_001f:  ldc.i4.s   10
  IL_0021:  stelem.i4
  IL_0022:  dup
  IL_0023:  ldc.i4.1
  IL_0024:  ldc.i4.s   11
  IL_0026:  stelem.i4
  IL_0027:  dup
  IL_0028:  ldc.i4.2
  IL_0029:  ldc.i4.s   12
  IL_002b:  stelem.i4
  IL_002c:  stloc.0
  IL_002d:  br.s       IL_002f
  IL_002f:  ldloc.0
  IL_0030:  ret
}");
            diff2.VerifyIL("C.F3",
@"{
  // Code size       27 (0x1b)
  .maxstack  4
  .locals init (object V_0)
  IL_0000:  nop
  IL_0001:  ldc.i4.3
  IL_0002:  newarr     ""int""
  IL_0007:  dup
  IL_0008:  ldc.i4.0
  IL_0009:  ldc.i4.s   13
  IL_000b:  stelem.i4
  IL_000c:  dup
  IL_000d:  ldc.i4.1
  IL_000e:  ldc.i4.s   14
  IL_0010:  stelem.i4
  IL_0011:  dup
  IL_0012:  ldc.i4.2
  IL_0013:  ldc.i4.s   15
  IL_0015:  stelem.i4
  IL_0016:  stloc.0
  IL_0017:  br.s       IL_0019
  IL_0019:  ldloc.0
  IL_001a:  ret
}");
        }

        /// <summary>
        /// Should not generate method for string switch since
        /// the CLR only allows adding private members.
        /// </summary>
        [WorkItem(834086, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/834086")]
        [Fact]
        public void PrivateImplementationDetails_ComputeStringHash()
        {
            var source =
@"class C
{
    static int F(string s)
    {
        switch (s)
        {
            case ""1"": return 1;
            case ""2"": return 2;
            case ""3"": return 3;
            case ""4"": return 4;
            case ""5"": return 5;
            case ""6"": return 6;
            case ""7"": return 7;
            default: return 0;
        }
    }
}";
            const string ComputeStringHashName = "ComputeStringHash";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source);

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(testData: testData0);
            var methodData0 = testData0.GetMethodData("C.F");
            var method0 = compilation0.GetMember<MethodSymbol>("C.F");
            var generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), methodData0.EncDebugInfoProvider());

            // Should have generated call to ComputeStringHash and
            // added the method to <PrivateImplementationDetails>.
            var actualIL0 = methodData0.GetMethodIL();
            Assert.True(actualIL0.Contains(ComputeStringHashName));

            using var md0 = ModuleMetadata.CreateFromImage(bytes0);
            var reader0 = md0.MetadataReader;
            CheckNames(reader0, reader0.GetMethodDefNames(), "F", ".ctor", ComputeStringHashName);

            var method1 = compilation1.GetMember<MethodSymbol>("C.F");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables: true)));

            // Should not have generated call to ComputeStringHash nor
            // added the method to <PrivateImplementationDetails>.
            var actualIL1 = diff1.GetMethodIL("C.F");
            Assert.False(actualIL1.Contains(ComputeStringHashName));

            using var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;
            var readers = new[] { reader0, reader1 };
            CheckNames(readers, reader1.GetMethodDefNames(), "F");
        }

        /// <summary>
        /// Unique ids should not conflict with ids
        /// from previous generation.
        /// </summary>
        [WorkItem(9847, "https://github.com/dotnet/roslyn/issues/9847")]
        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/9847")]
        public void UniqueIds()
        {
            var source0 =
@"class C
{
    int F()
    {
        System.Func<int> f = () => 3;
        return f();
    }
    static int F(bool b)
    {
        System.Func<int> f = () => 1;
        System.Func<int> g = () => 2;
        return (b ? f : g)();
    }
}";
            var source1 =
@"class C
{
    int F()
    {
        System.Func<int> f = () => 3;
        return f();
    }
    static int F(bool b)
    {
        System.Func<int> f = () => 1;
        return f();
    }
}";
            var source2 =
@"class C
{
    int F()
    {
        System.Func<int> f = () => 3;
        return f();
    }
    static int F(bool b)
    {
        System.Func<int> g = () => 2;
        return g();
    }
}";
            var compilation0 = CreateCompilation(source0, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);
            var compilation2 = compilation1.WithSource(source2);

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(testData: testData0);
            var generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), EmptyLocalsProvider);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, compilation0.GetMembers("C.F")[1], compilation1.GetMembers("C.F")[1])));

            diff1.VerifyIL("C.F",
@"{
  // Code size       40 (0x28)
  .maxstack  2
  .locals init (System.Func<int> V_0, //f
  int V_1)
  IL_0000:  nop
  IL_0001:  ldsfld     ""System.Func<int> C.CS$<>9__CachedAnonymousMethodDelegate6""
  IL_0006:  dup
  IL_0007:  brtrue.s   IL_001c
  IL_0009:  pop
  IL_000a:  ldnull
  IL_000b:  ldftn      ""int C.<F>b__5()""
  IL_0011:  newobj     ""System.Func<int>..ctor(object, System.IntPtr)""
  IL_0016:  dup
  IL_0017:  stsfld     ""System.Func<int> C.CS$<>9__CachedAnonymousMethodDelegate6""
  IL_001c:  stloc.0
  IL_001d:  ldloc.0
  IL_001e:  callvirt   ""int System.Func<int>.Invoke()""
  IL_0023:  stloc.1
  IL_0024:  br.s       IL_0026
  IL_0026:  ldloc.1
  IL_0027:  ret
}");

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, compilation1.GetMembers("C.F")[1], compilation2.GetMembers("C.F")[1])));

            diff2.VerifyIL("C.F",
@"{
  // Code size       40 (0x28)
  .maxstack  2
  .locals init (System.Func<int> V_0, //g
  int V_1)
  IL_0000:  nop
  IL_0001:  ldsfld     ""System.Func<int> C.CS$<>9__CachedAnonymousMethodDelegate8""
  IL_0006:  dup
  IL_0007:  brtrue.s   IL_001c
  IL_0009:  pop
  IL_000a:  ldnull
  IL_000b:  ldftn      ""int C.<F>b__7()""
  IL_0011:  newobj     ""System.Func<int>..ctor(object, System.IntPtr)""
  IL_0016:  dup
  IL_0017:  stsfld     ""System.Func<int> C.CS$<>9__CachedAnonymousMethodDelegate8""
  IL_001c:  stloc.0
  IL_001d:  ldloc.0
  IL_001e:  callvirt   ""int System.Func<int>.Invoke()""
  IL_0023:  stloc.1
  IL_0024:  br.s       IL_0026
  IL_0026:  ldloc.1
  IL_0027:  ret
}");
        }

        /// <summary>
        /// Avoid adding references from method bodies
        /// other than the changed methods.
        /// </summary>
        [Fact]
        public void ReferencesInIL()
        {
            var source0 =
@"class C
{
    static void F() { System.Console.WriteLine(1); }
    static void G() { System.Console.WriteLine(2); }
}";
            var source1 =
@"class C
{
    static void F() { System.Console.WriteLine(1); }
    static void G() { System.Console.Write(2); }
}";
            var compilation0 = CreateCompilation(source0, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);

            // Verify full metadata contains expected rows.
            var bytes0 = compilation0.EmitToArray();
            using var md0 = ModuleMetadata.CreateFromImage(bytes0);
            var reader0 = md0.MetadataReader;
            CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "C");
            CheckNames(reader0, reader0.GetMethodDefNames(), "F", "G", ".ctor");
            CheckNames(reader0, reader0.GetMemberRefNames(), ".ctor", ".ctor", ".ctor", "WriteLine", ".ctor");

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, EmptyLocalsProvider);
            var method0 = compilation0.GetMember<MethodSymbol>("C.G");
            var method1 = compilation1.GetMember<MethodSymbol>("C.G");

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(
                    SemanticEditKind.Update,
                    method0,
                    method1,
                    GetEquivalentNodesMap(method1, method0),
                    preserveLocalVariables: true)));

            // "Write" should be included in string table, but "WriteLine" should not.
            Assert.True(diff1.MetadataDelta.IsIncluded("Write"));
            Assert.False(diff1.MetadataDelta.IsIncluded("WriteLine"));
        }

        /// <summary>
        /// Local slots must be preserved based on signature.
        /// </summary>
        [Fact]
        public void PreserveLocalSlots()
        {
            var source0 =
@"class A<T> { }
class B : A<B>
{
    static B F()
    {
        return null;
    }
    static void M(object o)
    {
        object x = F();
        A<B> y = F();
        object z = F();
        M(x);
        M(y);
        M(z);
    }
    static void N()
    {
        object a = F();
        object b = F();
        M(a);
        M(b);
    }
}";
            var methodNames0 = new[] { "A<T>..ctor", "B.F", "B.M", "B.N" };

            var source1 =
@"class A<T> { }
class B : A<B>
{
    static B F()
    {
        return null;
    }
    static void M(object o)
    {
        B z = F();
        A<B> y = F();
        object w = F();
        M(w);
        M(y);
    }
    static void N()
    {
        object a = F();
        object b = F();
        M(a);
        M(b);
    }
}";
            var source2 =
@"class A<T> { }
class B : A<B>
{
    static B F()
    {
        return null;
    }
    static void M(object o)
    {
        object x = F();
        B z = F();
        M(x);
        M(z);
    }
    static void N()
    {
        object a = F();
        object b = F();
        M(a);
        M(b);
    }
}";
            var source3 =
@"class A<T> { }
class B : A<B>
{
    static B F()
    {
        return null;
    }
    static void M(object o)
    {
        object x = F();
        B z = F();
        M(x);
        M(z);
    }
    static void N()
    {
        object c = F();
        object b = F();
        M(c);
        M(b);
    }
}";
            var compilation0 = CreateCompilation(source0, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);
            var compilation2 = compilation1.WithSource(source2);
            var compilation3 = compilation2.WithSource(source3);

            var method0 = compilation0.GetMember<MethodSymbol>("B.M");
            var methodN = compilation0.GetMember<MethodSymbol>("B.N");

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(testData: testData0);
            var generation0 = EmitBaseline.CreateInitialBaseline(
                ModuleMetadata.CreateFromImage(bytes0),
                m => testData0.GetMethodData(methodNames0[MetadataTokens.GetRowNumber(m) - 1]).GetEncDebugInfo());

            #region Gen1 

            var method1 = compilation1.GetMember<MethodSymbol>("B.M");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables: true)));

            diff1.VerifyIL(
@"{
  // Code size       36 (0x24)
  .maxstack  1
  IL_0000:  nop       
  IL_0001:  call       0x06000002
  IL_0006:  stloc.3   
  IL_0007:  call       0x06000002
  IL_000c:  stloc.1   
  IL_000d:  call       0x06000002
  IL_0012:  stloc.s    V_4
  IL_0014:  ldloc.s    V_4
  IL_0016:  call       0x06000003
  IL_001b:  nop       
  IL_001c:  ldloc.1   
  IL_001d:  call       0x06000003
  IL_0022:  nop       
  IL_0023:  ret       
}");
            diff1.VerifyPdb(new[] { 0x06000001, 0x06000002, 0x06000003, 0x06000004 }, @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method token=""0x6000003"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""9"" startColumn=""5"" endLine=""9"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""19"" document=""1"" />
        <entry offset=""0x7"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""22"" document=""1"" />
        <entry offset=""0xd"" startLine=""12"" startColumn=""9"" endLine=""12"" endColumn=""24"" document=""1"" />
        <entry offset=""0x14"" startLine=""13"" startColumn=""9"" endLine=""13"" endColumn=""14"" document=""1"" />
        <entry offset=""0x1c"" startLine=""14"" startColumn=""9"" endLine=""14"" endColumn=""14"" document=""1"" />
        <entry offset=""0x23"" startLine=""15"" startColumn=""5"" endLine=""15"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x24"">
        <local name=""z"" il_index=""3"" il_start=""0x0"" il_end=""0x24"" attributes=""0"" />
        <local name=""y"" il_index=""1"" il_start=""0x0"" il_end=""0x24"" attributes=""0"" />
        <local name=""w"" il_index=""4"" il_start=""0x0"" il_end=""0x24"" attributes=""0"" />
      </scope>
    </method>
  </methods>
</symbols>");

            #endregion

            #region Gen2 

            var method2 = compilation2.GetMember<MethodSymbol>("B.M");
            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method1, method2, GetEquivalentNodesMap(method2, method1), preserveLocalVariables: true)));

            diff2.VerifyIL(
@"{
  // Code size       30 (0x1e)
  .maxstack  1
  IL_0000:  nop
  IL_0001:  call       0x06000002
  IL_0006:  stloc.s    V_5
  IL_0008:  call       0x06000002
  IL_000d:  stloc.3
  IL_000e:  ldloc.s    V_5
  IL_0010:  call       0x06000003
  IL_0015:  nop
  IL_0016:  ldloc.3
  IL_0017:  call       0x06000003
  IL_001c:  nop
  IL_001d:  ret
}");

            diff2.VerifyPdb(new[] { 0x06000001, 0x06000002, 0x06000003, 0x06000004 }, @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method token=""0x6000003"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""9"" startColumn=""5"" endLine=""9"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""24"" document=""1"" />
        <entry offset=""0x8"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""19"" document=""1"" />
        <entry offset=""0xe"" startLine=""12"" startColumn=""9"" endLine=""12"" endColumn=""14"" document=""1"" />
        <entry offset=""0x16"" startLine=""13"" startColumn=""9"" endLine=""13"" endColumn=""14"" document=""1"" />
        <entry offset=""0x1d"" startLine=""14"" startColumn=""5"" endLine=""14"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x1e"">
        <local name=""x"" il_index=""5"" il_start=""0x0"" il_end=""0x1e"" attributes=""0"" />
        <local name=""z"" il_index=""3"" il_start=""0x0"" il_end=""0x1e"" attributes=""0"" />
      </scope>
    </method>
  </methods>
</symbols>");

            #endregion

            #region Gen3

            // Modify different method. (Previous generations
            // have not referenced method.)
            method2 = compilation2.GetMember<MethodSymbol>("B.N");
            var method3 = compilation3.GetMember<MethodSymbol>("B.N");
            var diff3 = compilation3.EmitDifference(
                diff2.NextGeneration,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method2, method3, GetEquivalentNodesMap(method3, method2), preserveLocalVariables: true)));

            diff3.VerifyIL(
@"{
  // Code size       28 (0x1c)
  .maxstack  1
  IL_0000:  nop
  IL_0001:  call       0x06000002
  IL_0006:  stloc.2
  IL_0007:  call       0x06000002
  IL_000c:  stloc.1
  IL_000d:  ldloc.2
  IL_000e:  call       0x06000003
  IL_0013:  nop
  IL_0014:  ldloc.1
  IL_0015:  call       0x06000003
  IL_001a:  nop
  IL_001b:  ret
}");
            diff3.VerifyPdb(new[] { 0x06000001, 0x06000002, 0x06000003, 0x06000004 }, @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method token=""0x6000004"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""16"" startColumn=""5"" endLine=""16"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""17"" startColumn=""9"" endLine=""17"" endColumn=""24"" document=""1"" />
        <entry offset=""0x7"" startLine=""18"" startColumn=""9"" endLine=""18"" endColumn=""24"" document=""1"" />
        <entry offset=""0xd"" startLine=""19"" startColumn=""9"" endLine=""19"" endColumn=""14"" document=""1"" />
        <entry offset=""0x14"" startLine=""20"" startColumn=""9"" endLine=""20"" endColumn=""14"" document=""1"" />
        <entry offset=""0x1b"" startLine=""21"" startColumn=""5"" endLine=""21"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x1c"">
        <local name=""c"" il_index=""2"" il_start=""0x0"" il_end=""0x1c"" attributes=""0"" />
        <local name=""b"" il_index=""1"" il_start=""0x0"" il_end=""0x1c"" attributes=""0"" />
      </scope>
    </method>
  </methods>
</symbols>");

            #endregion
        }

        /// <summary>
        /// Preserve locals for method added after initial compilation.
        /// </summary>
        [ConditionalFact(typeof(WindowsOnly), Reason = "https://github.com/dotnet/roslyn/issues/37137")]
        public void PreserveLocalSlots_NewMethod()
        {
            var source0 =
@"class C
{
}";
            var source1 =
@"class C
{
    static void M()
    {
        var a = new object();
        var b = string.Empty;
    }
}";
            var source2 =
@"class C
{
    static void M()
    {
        var a = 1;
        var b = string.Empty;
    }
}";
            var compilation0 = CreateCompilation(source0, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);
            var compilation2 = compilation1.WithSource(source2);

            var bytes0 = compilation0.EmitToArray();
            var generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), EmptyLocalsProvider);

            var method1 = compilation1.GetMember<MethodSymbol>("C.M");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Insert, null, method1, null, preserveLocalVariables: true)));

            var method2 = compilation2.GetMember<MethodSymbol>("C.M");
            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method1, method2, GetEquivalentNodesMap(method2, method1), preserveLocalVariables: true)));
            diff2.VerifyIL("C.M",
@"{
  // Code size       10 (0xa)
  .maxstack  1
  .locals init ([object] V_0,
                string V_1, //b
                int V_2) //a
  IL_0000:  nop
  IL_0001:  ldc.i4.1
  IL_0002:  stloc.2
  IL_0003:  ldsfld     ""string string.Empty""
  IL_0008:  stloc.1
  IL_0009:  ret
}");
            diff2.VerifyPdb(new[] { 0x06000002 }, @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method token=""0x6000002"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""4"" startColumn=""5"" endLine=""4"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""5"" startColumn=""9"" endLine=""5"" endColumn=""19"" document=""1"" />
        <entry offset=""0x3"" startLine=""6"" startColumn=""9"" endLine=""6"" endColumn=""30"" document=""1"" />
        <entry offset=""0x9"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0xa"">
        <local name=""a"" il_index=""2"" il_start=""0x0"" il_end=""0xa"" attributes=""0"" />
        <local name=""b"" il_index=""1"" il_start=""0x0"" il_end=""0xa"" attributes=""0"" />
      </scope>
    </method>
  </methods>
</symbols>");
        }

        /// <summary>
        /// Local types should be retained, even if the local is no longer
        /// used by the method body, since there may be existing
        /// references to that slot, in a Watch window for instance.
        /// </summary>
        [WorkItem(843320, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/843320")]
        [Fact]
        public void PreserveLocalTypes()
        {
            var source0 =
@"class C
{
    static void Main()
    {
        var x = true;
        var y = x;
        System.Console.WriteLine(y);
    }
}";
            var source1 =
@"class C
{
    static void Main()
    {
        var x = ""A"";
        var y = x;
        System.Console.WriteLine(y);
    }
}";
            var compilation0 = CreateCompilation(source0, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);
            var method0 = compilation0.GetMember<MethodSymbol>("C.Main");
            var method1 = compilation1.GetMember<MethodSymbol>("C.Main");
            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(testData: testData0);

            var generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), testData0.GetMethodData("C.Main").EncDebugInfoProvider());

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables: true)));
            diff1.VerifyIL("C.Main", @"
{
  // Code size       17 (0x11)
  .maxstack  1
  .locals init ([bool] V_0,
                [bool] V_1,
                string V_2, //x
                string V_3) //y
  IL_0000:  nop
  IL_0001:  ldstr      ""A""
  IL_0006:  stloc.2
  IL_0007:  ldloc.2
  IL_0008:  stloc.3
  IL_0009:  ldloc.3
  IL_000a:  call       ""void System.Console.WriteLine(string)""
  IL_000f:  nop
  IL_0010:  ret
}");
        }

        /// <summary>
        /// Preserve locals if SemanticEdit.PreserveLocalVariables is set.
        /// </summary>
        [Fact]
        public void PreserveLocalVariablesFlag()
        {
            var source =
@"class C
{
    static System.IDisposable F() { return null; }
    static void M()
    {
        using (F()) { }
        using (var x = F()) { }
    }
}";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source);

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(testData: testData0);
            var method0 = compilation0.GetMember<MethodSymbol>("C.M");
            var generation0 = EmitBaseline.CreateInitialBaseline(
                ModuleMetadata.CreateFromImage(bytes0),
                testData0.GetMethodData("C.M").EncDebugInfoProvider());

            var method1 = compilation1.GetMember<MethodSymbol>("C.M");
            var diff1a = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, preserveLocalVariables: false)));

            diff1a.VerifyIL("C.M", @"
{
  // Code size       44 (0x2c)
  .maxstack  1
  .locals init (System.IDisposable V_0,
                System.IDisposable V_1) //x
  IL_0000:  nop
  IL_0001:  call       ""System.IDisposable C.F()""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  nop
    IL_0008:  nop
    IL_0009:  leave.s    IL_0016
  }
  finally
  {
    IL_000b:  ldloc.0
    IL_000c:  brfalse.s  IL_0015
    IL_000e:  ldloc.0
    IL_000f:  callvirt   ""void System.IDisposable.Dispose()""
    IL_0014:  nop
    IL_0015:  endfinally
  }
  IL_0016:  call       ""System.IDisposable C.F()""
  IL_001b:  stloc.1
  .try
  {
    IL_001c:  nop
    IL_001d:  nop
    IL_001e:  leave.s    IL_002b
  }
  finally
  {
    IL_0020:  ldloc.1
    IL_0021:  brfalse.s  IL_002a
    IL_0023:  ldloc.1
    IL_0024:  callvirt   ""void System.IDisposable.Dispose()""
    IL_0029:  nop
    IL_002a:  endfinally
  }
  IL_002b:  ret
}
");

            var diff1b = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, preserveLocalVariables: true)));

            diff1b.VerifyIL("C.M",
@"{
  // Code size       44 (0x2c)
  .maxstack  1
  .locals init (System.IDisposable V_0,
                System.IDisposable V_1) //x
  IL_0000:  nop
  IL_0001:  call       ""System.IDisposable C.F()""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  nop
    IL_0008:  nop
    IL_0009:  leave.s    IL_0016
  }
  finally
  {
    IL_000b:  ldloc.0
    IL_000c:  brfalse.s  IL_0015
    IL_000e:  ldloc.0
    IL_000f:  callvirt   ""void System.IDisposable.Dispose()""
    IL_0014:  nop
    IL_0015:  endfinally
  }
  IL_0016:  call       ""System.IDisposable C.F()""
  IL_001b:  stloc.1
  .try
  {
    IL_001c:  nop
    IL_001d:  nop
    IL_001e:  leave.s    IL_002b
  }
  finally
  {
    IL_0020:  ldloc.1
    IL_0021:  brfalse.s  IL_002a
    IL_0023:  ldloc.1
    IL_0024:  callvirt   ""void System.IDisposable.Dispose()""
    IL_0029:  nop
    IL_002a:  endfinally
  }
  IL_002b:  ret
}");
        }

        [WorkItem(779531, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/779531")]
        [Fact]
        public void ChangeLocalType()
        {
            var source0 =
@"enum E { }
class C
{
    static void M1()
    {
        var x = default(E);
        var y = x;
        var z = default(E);
        System.Console.WriteLine(y);
    }
    static void M2()
    {
        var x = default(E);
        var y = x;
        var z = default(E);
        System.Console.WriteLine(y);
    }
}";
            // Change locals in one method to type added.
            var source1 =
@"enum E { }
class A { }
class C
{
    static void M1()
    {
        var x = default(A);
        var y = x;
        var z = default(E);
        System.Console.WriteLine(y);
    }
    static void M2()
    {
        var x = default(E);
        var y = x;
        var z = default(E);
        System.Console.WriteLine(y);
    }
}";
            // Change locals in another method.
            var source2 =
@"enum E { }
class A { }
class C
{
    static void M1()
    {
        var x = default(A);
        var y = x;
        var z = default(E);
        System.Console.WriteLine(y);
    }
    static void M2()
    {
        var x = default(A);
        var y = x;
        var z = default(E);
        System.Console.WriteLine(y);
    }
}";
            // Change locals in same method.
            var source3 =
@"enum E { }
class A { }
class C
{
    static void M1()
    {
        var x = default(A);
        var y = x;
        var z = default(E);
        System.Console.WriteLine(y);
    }
    static void M2()
    {
        var x = default(A);
        var y = x;
        var z = default(A);
        System.Console.WriteLine(y);
    }
}";
            var compilation0 = CreateCompilation(source0, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);
            var compilation2 = compilation1.WithSource(source2);
            var compilation3 = compilation2.WithSource(source3);

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(testData: testData0);
            var methodData0 = testData0.GetMethodData("C.M1");
            var method0 = compilation0.GetMember<MethodSymbol>("C.M1");
            var generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), methodData0.EncDebugInfoProvider());

            var method1 = compilation1.GetMember<MethodSymbol>("C.M1");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Insert, null, compilation1.GetMember<NamedTypeSymbol>("A")),
                    new SemanticEdit(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables: true)));

            diff1.VerifyIL("C.M1",
@"{
  // Code size       17 (0x11)
  .maxstack  1
  .locals init ([unchanged] V_0,
                [unchanged] V_1,
                E V_2, //z
                A V_3, //x
                A V_4) //y
  IL_0000:  nop
  IL_0001:  ldnull
  IL_0002:  stloc.3
  IL_0003:  ldloc.3
  IL_0004:  stloc.s    V_4
  IL_0006:  ldc.i4.0
  IL_0007:  stloc.2
  IL_0008:  ldloc.s    V_4
  IL_000a:  call       ""void System.Console.WriteLine(object)""
  IL_000f:  nop
  IL_0010:  ret
}");

            var method2 = compilation2.GetMember<MethodSymbol>("C.M2");
            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, method1, method2, GetEquivalentNodesMap(method2, method1), preserveLocalVariables: true)));

            diff2.VerifyIL("C.M2",
@"{
  // Code size       17 (0x11)
  .maxstack  1
  .locals init ([unchanged] V_0,
                [unchanged] V_1,
                E V_2, //z
                A V_3, //x
                A V_4) //y
  IL_0000:  nop
  IL_0001:  ldnull
  IL_0002:  stloc.3
  IL_0003:  ldloc.3
  IL_0004:  stloc.s    V_4
  IL_0006:  ldc.i4.0
  IL_0007:  stloc.2
  IL_0008:  ldloc.s    V_4
  IL_000a:  call       ""void System.Console.WriteLine(object)""
  IL_000f:  nop
  IL_0010:  ret
}");

            var method3 = compilation3.GetMember<MethodSymbol>("C.M2");
            var diff3 = compilation3.EmitDifference(
                diff2.NextGeneration,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, method2, method3, GetEquivalentNodesMap(method3, method2), preserveLocalVariables: true)));

            diff3.VerifyIL("C.M2",
@"{
  // Code size       18 (0x12)
  .maxstack  1
  .locals init ([unchanged] V_0,
                [unchanged] V_1,
                [unchanged] V_2,
                A V_3, //x
                A V_4, //y
                A V_5) //z
  IL_0000:  nop
  IL_0001:  ldnull
  IL_0002:  stloc.3
  IL_0003:  ldloc.3
  IL_0004:  stloc.s    V_4
  IL_0006:  ldnull
  IL_0007:  stloc.s    V_5
  IL_0009:  ldloc.s    V_4
  IL_000b:  call       ""void System.Console.WriteLine(object)""
  IL_0010:  nop
  IL_0011:  ret
}");
        }

        [Fact]
        public void AnonymousTypes_Update()
        {
            var source0 = MarkedSource(@"
class C
{
    static void F()
    {
        var <N:0>x = new { A = 1 }</N:0>;
    }
}
");
            var source1 = MarkedSource(@"
class C
{
    static void F()
    {
        var <N:0>x = new { A = 2 }</N:0>;
    }
}
");
            var source2 = MarkedSource(@"
class C
{
    static void F()
    {
        var <N:0>x = new { A = 3 }</N:0>;
    }
}
");
            var compilation0 = CreateCompilation(source0.Tree, options: ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            var compilation1 = compilation0.WithSource(source1.Tree);
            var compilation2 = compilation1.WithSource(source2.Tree);

            var v0 = CompileAndVerify(compilation0);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");
            var f2 = compilation2.GetMember<MethodSymbol>("C.F");

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            v0.VerifyIL("C.F", @"
{
  // Code size        9 (0x9)
  .maxstack  1
  .locals init (<>f__AnonymousType0<int> V_0) //x
  IL_0000:  nop
  IL_0001:  ldc.i4.1
  IL_0002:  newobj     ""<>f__AnonymousType0<int>..ctor(int)""
  IL_0007:  stloc.0
  IL_0008:  ret
}
");

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            diff1.VerifySynthesizedMembers(
                "<>f__AnonymousType0<<A>j__TPar>: {Equals, GetHashCode, ToString}");

            diff1.VerifyIL("C.F", @"
{
  // Code size        9 (0x9)
  .maxstack  1
  .locals init (<>f__AnonymousType0<int> V_0) //x
  IL_0000:  nop
  IL_0001:  ldc.i4.2
  IL_0002:  newobj     ""<>f__AnonymousType0<int>..ctor(int)""
  IL_0007:  stloc.0
  IL_0008:  ret
}
");
            // expect a single TypeRef for System.Object
            var md1 = diff1.GetMetadata();
            AssertEx.Equal(new[] { "[0x23000002] System.Object" }, DumpTypeRefs(new[] { md0.MetadataReader, md1.Reader }));

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, f1, f2, GetSyntaxMapFromMarkers(source1, source2), preserveLocalVariables: true)));

            diff2.VerifySynthesizedMembers(
                "<>f__AnonymousType0<<A>j__TPar>: {Equals, GetHashCode, ToString}");

            diff2.VerifyIL("C.F", @"
{
  // Code size        9 (0x9)
  .maxstack  1
  .locals init (<>f__AnonymousType0<int> V_0) //x
  IL_0000:  nop
  IL_0001:  ldc.i4.3
  IL_0002:  newobj     ""<>f__AnonymousType0<int>..ctor(int)""
  IL_0007:  stloc.0
  IL_0008:  ret
}
");
            // expect a single TypeRef for System.Object
            var md2 = diff2.GetMetadata();
            AssertEx.Equal(new[] { "[0x23000003] System.Object" }, DumpTypeRefs(new[] { md0.MetadataReader, md1.Reader, md2.Reader }));
        }

        [Fact]
        public void AnonymousTypes_UpdateAfterAdd()
        {
            var source0 = MarkedSource(@"
class C
{
    static void F()
    {
    }
}
");
            var source1 = MarkedSource(@"
class C
{
    static void F()
    {
        var <N:0>x = new { A = 2 }</N:0>;
    }
}
");
            var source2 = MarkedSource(@"
class C
{
    static void F()
    {
        var <N:0>x = new { A = 3 }</N:0>;
    }
}
");
            var compilation0 = CreateCompilation(source0.Tree, options: ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All), targetFramework: TargetFramework.NetStandard20);
            var compilation1 = compilation0.WithSource(source1.Tree);
            var compilation2 = compilation1.WithSource(source2.Tree);

            var v0 = CompileAndVerify(compilation0);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");
            var f2 = compilation2.GetMember<MethodSymbol>("C.F");

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            var md1 = diff1.GetMetadata();

            diff1.VerifySynthesizedMembers(
                "<>f__AnonymousType0<<A>j__TPar>: {Equals, GetHashCode, ToString}");

            diff1.VerifyIL("C.F", @"
{
  // Code size        9 (0x9)
  .maxstack  1
  .locals init (<>f__AnonymousType0<int> V_0) //x
  IL_0000:  nop
  IL_0001:  ldc.i4.2
  IL_0002:  newobj     ""<>f__AnonymousType0<int>..ctor(int)""
  IL_0007:  stloc.0
  IL_0008:  ret
}
");
            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, f1, f2, GetSyntaxMapFromMarkers(source1, source2), preserveLocalVariables: true)));

            diff2.VerifySynthesizedMembers(
                "<>f__AnonymousType0<<A>j__TPar>: {Equals, GetHashCode, ToString}");

            diff2.VerifyIL("C.F", @"
{
  // Code size        9 (0x9)
  .maxstack  1
  .locals init (<>f__AnonymousType0<int> V_0) //x
  IL_0000:  nop
  IL_0001:  ldc.i4.3
  IL_0002:  newobj     ""<>f__AnonymousType0<int>..ctor(int)""
  IL_0007:  stloc.0
  IL_0008:  ret
}
");
            // expect a single TypeRef for System.Object
            var md2 = diff2.GetMetadata();
            AssertEx.Equal(new[] { "[0x23000003] System.Object" }, DumpTypeRefs(new[] { md0.MetadataReader, md1.Reader, md2.Reader }));
        }

        /// <summary>
        /// Reuse existing anonymous types.
        /// </summary>
        [WorkItem(825903, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/825903")]
        [Fact]
        public void AnonymousTypes()
        {
            var source0 =
@"namespace N
{
    class A
    {
        static object F = new { A = 1, B = 2 };
    }
}
namespace M
{
    class B
    {
        static void M()
        {
            var x = new { B = 3, A = 4 };
            var y = x.A;
            var z = new { };
        }
    }
}";
            var source1 =
@"namespace N
{
    class A
    {
        static object F = new { A = 1, B = 2 };
    }
}
namespace M
{
    class B
    {
        static void M()
        {
            var x = new { B = 3, A = 4 };
            var y = new { A = x.A };
            var z = new { };
        }
    }
}";
            var compilation0 = CreateCompilation(source0, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);

            var m0 = compilation0.GetMember<MethodSymbol>("M.B.M");
            var m1 = compilation1.GetMember<MethodSymbol>("M.B.M");

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(testData: testData0);

            using var md0 = ModuleMetadata.CreateFromImage(bytes0);
            var generation0 = EmitBaseline.CreateInitialBaseline(md0, testData0.GetMethodData("M.B.M").EncDebugInfoProvider());

            var reader0 = md0.MetadataReader;
            CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "<>f__AnonymousType0`2", "<>f__AnonymousType1`2", "<>f__AnonymousType2", "B", "A");

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, m0, m1, GetEquivalentNodesMap(m1, m0), preserveLocalVariables: true)));

            using var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;
            CheckNames(new[] { reader0, reader1 }, reader1.GetTypeDefNames(), "<>f__AnonymousType3`1"); // one additional type

            diff1.VerifyIL("M.B.M", @"
{
  // Code size       28 (0x1c)
  .maxstack  2
  .locals init (<>f__AnonymousType1<int, int> V_0, //x
                [int] V_1,
                <>f__AnonymousType2 V_2, //z
                <>f__AnonymousType3<int> V_3) //y
  IL_0000:  nop
  IL_0001:  ldc.i4.3
  IL_0002:  ldc.i4.4
  IL_0003:  newobj     ""<>f__AnonymousType1<int, int>..ctor(int, int)""
  IL_0008:  stloc.0
  IL_0009:  ldloc.0
  IL_000a:  callvirt   ""int <>f__AnonymousType1<int, int>.A.get""
  IL_000f:  newobj     ""<>f__AnonymousType3<int>..ctor(int)""
  IL_0014:  stloc.3
  IL_0015:  newobj     ""<>f__AnonymousType2..ctor()""
  IL_001a:  stloc.2
  IL_001b:  ret
}");
        }

        /// <summary>
        /// Anonymous type names with module ids
        /// and gaps in indices.
        /// </summary>
        [ConditionalFact(typeof(WindowsOnly), Reason = "https://github.com/dotnet/roslyn/issues/37137")]
        public void AnonymousTypes_OtherTypeNames()
        {
            var ilSource =
@".assembly extern netstandard { .ver 2:0:0:0 .publickeytoken = (cc 7b 13 ff cd 2d dd 51) }
// Valid signature, although not sequential index
.class '<>f__AnonymousType2'<'<A>j__TPar', '<B>j__TPar'> extends object
{
  .field public !'<A>j__TPar' A
  .field public !'<B>j__TPar' B
}
// Invalid signature, unexpected type parameter names
.class '<>f__AnonymousType1'<A, B> extends object
{
  .field public !A A
  .field public !B B
}
// Module id, duplicate index
.class '<m>f__AnonymousType2`1'<'<A>j__TPar'> extends object
{
  .field public !'<A>j__TPar' A
}
// Module id
.class '<m>f__AnonymousType3`1'<'<B>j__TPar'> extends object
{
  .field public !'<B>j__TPar' B
}
.class public C extends object
{
  .method public specialname rtspecialname instance void .ctor()
  {
    ret
  }
  .method public static object F()
  {
    ldnull
    ret
  }
}";
            var source0 =
@"class C
{
    static object F()
    {
        return 0;
    }
}";
            var source1 =
@"class C
{
    static object F()
    {
        var x = new { A = new object(), B = 1 };
        var y = new { A = x.A };
        return y;
    }
}";
            var metadata0 = (MetadataImageReference)CompileIL(ilSource, prependDefaultHeader: false);

            var compilation0 = CreateCompilation(source0, options: TestOptions.DebugDll, targetFramework: TargetFramework.NetStandard20);
            var compilation1 = compilation0.WithSource(source1);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");

            var moduleMetadata0 = ((AssemblyMetadata)metadata0.GetMetadataNoCopy()).GetModules()[0];
            var generation0 = EmitBaseline.CreateInitialBaseline(moduleMetadata0, m => default);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, f0, f1, GetEquivalentNodesMap(f1, f0), preserveLocalVariables: true)));

            using var md1 = diff1.GetMetadata();
            diff1.VerifyIL("C.F",
@"{
  // Code size       31 (0x1f)
  .maxstack  2
  .locals init (<>f__AnonymousType2<object, int> V_0, //x
  <>f__AnonymousType3<object> V_1, //y
  object V_2)
  IL_0000:  nop
  IL_0001:  newobj     ""object..ctor()""
  IL_0006:  ldc.i4.1
  IL_0007:  newobj     ""<>f__AnonymousType2<object, int>..ctor(object, int)""
  IL_000c:  stloc.0
  IL_000d:  ldloc.0
  IL_000e:  callvirt   ""object <>f__AnonymousType2<object, int>.A.get""
  IL_0013:  newobj     ""<>f__AnonymousType3<object>..ctor(object)""
  IL_0018:  stloc.1
  IL_0019:  ldloc.1
  IL_001a:  stloc.2
  IL_001b:  br.s       IL_001d
  IL_001d:  ldloc.2
  IL_001e:  ret
}");
        }

        /// <summary>
        /// Update method with anonymous type that was
        /// not directly referenced in previous generation.
        /// </summary>
        [Fact]
        public void AnonymousTypes_SkipGeneration()
        {
            var source0 = MarkedSource(
@"class A { }
class B
{
    static object F()
    {
        var <N:0>x = new { A = 1 }</N:0>;
        return x.A;
    }
    static object G()
    {
        var <N:1>x = 1</N:1>;
        return x;
    }
}");
            var source1 = MarkedSource(
@"class A { }
class B
{
    static object F()
    {
        var <N:0>x = new { A = 1 }</N:0>;
        return x.A;
    }
    static object G()
    {
        var <N:1>x = 1</N:1>;
        return x + 1;
    }
}");
            var source2 = MarkedSource(
@"class A { }
class B
{
    static object F()
    {
        var <N:0>x = new { A = 1 }</N:0>;
        return x.A;
    }
    static object G()
    {
        var <N:1>x = new { A = new A() }</N:1>;
        var <N:2>y = new { B = 2 }</N:2>;
        return x.A;
    }
}");
            var source3 = MarkedSource(
@"class A { }
class B
{
    static object F()
    {
        var <N:0>x = new { A = 1 }</N:0>;
        return x.A;
    }
    static object G()
    {
        var <N:1>x = new { A = new A() }</N:1>;
        var <N:2>y = new { B = 3 }</N:2>;
        return y.B;
    }
}");
            var compilation0 = CreateCompilation(source0.Tree, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);
            var compilation2 = compilation1.WithSource(source2.Tree);
            var compilation3 = compilation2.WithSource(source3.Tree);

            var v0 = CompileAndVerify(compilation0);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var method0 = compilation0.GetMember<MethodSymbol>("B.G");
            var method1 = compilation1.GetMember<MethodSymbol>("B.G");
            var method2 = compilation2.GetMember<MethodSymbol>("B.G");
            var method3 = compilation3.GetMember<MethodSymbol>("B.G");

            var reader0 = md0.MetadataReader;
            CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "<>f__AnonymousType0`1", "A", "B");

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;
            CheckNames(new[] { reader0, reader1 }, reader1.GetTypeDefNames()); // no additional types

            diff1.VerifyIL("B.G", @"
{
  // Code size       16 (0x10)
  .maxstack  2
  .locals init (int V_0, //x
                [object] V_1,
                object V_2)
  IL_0000:  nop       
  IL_0001:  ldc.i4.1  
  IL_0002:  stloc.0   
  IL_0003:  ldloc.0   
  IL_0004:  ldc.i4.1  
  IL_0005:  add       
  IL_0006:  box        ""int""
  IL_000b:  stloc.2   
  IL_000c:  br.s       IL_000e
  IL_000e:  ldloc.2   
  IL_000f:  ret       
}");

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method1, method2, GetSyntaxMapFromMarkers(source1, source2), preserveLocalVariables: true)));

            var md2 = diff2.GetMetadata();
            var reader2 = md2.Reader;
            CheckNames(new[] { reader0, reader1, reader2 }, reader2.GetTypeDefNames(), "<>f__AnonymousType1`1"); // one additional type
            diff2.VerifyIL("B.G", @"
{
  // Code size       33 (0x21)
  .maxstack  1
  .locals init ([int] V_0,
                [object] V_1,
                [object] V_2,
                <>f__AnonymousType0<A> V_3, //x
                <>f__AnonymousType1<int> V_4, //y
                object V_5)
  IL_0000:  nop       
  IL_0001:  newobj     ""A..ctor()""
  IL_0006:  newobj     ""<>f__AnonymousType0<A>..ctor(A)""
  IL_000b:  stloc.3   
  IL_000c:  ldc.i4.2  
  IL_000d:  newobj     ""<>f__AnonymousType1<int>..ctor(int)""
  IL_0012:  stloc.s    V_4
  IL_0014:  ldloc.3   
  IL_0015:  callvirt   ""A <>f__AnonymousType0<A>.A.get""
  IL_001a:  stloc.s    V_5
  IL_001c:  br.s       IL_001e
  IL_001e:  ldloc.s    V_5
  IL_0020:  ret       
}");

            var diff3 = compilation3.EmitDifference(
                diff2.NextGeneration,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method2, method3, GetSyntaxMapFromMarkers(source2, source3), preserveLocalVariables: true)));

            var md3 = diff3.GetMetadata();

            var reader3 = md3.Reader;
            CheckNames(new[] { reader0, reader1, reader2, reader3 }, reader3.GetTypeDefNames()); // no additional types
            diff3.VerifyIL("B.G", @"
{
  // Code size       39 (0x27)
  .maxstack  1
  .locals init ([int] V_0,
                [object] V_1,
                [object] V_2,
                <>f__AnonymousType0<A> V_3, //x
                <>f__AnonymousType1<int> V_4, //y
                [object] V_5,
                object V_6)
  IL_0000:  nop
  IL_0001:  newobj     ""A..ctor()""
  IL_0006:  newobj     ""<>f__AnonymousType0<A>..ctor(A)""
  IL_000b:  stloc.3
  IL_000c:  ldc.i4.3
  IL_000d:  newobj     ""<>f__AnonymousType1<int>..ctor(int)""
  IL_0012:  stloc.s    V_4
  IL_0014:  ldloc.s    V_4
  IL_0016:  callvirt   ""int <>f__AnonymousType1<int>.B.get""
  IL_001b:  box        ""int""
  IL_0020:  stloc.s    V_6
  IL_0022:  br.s       IL_0024
  IL_0024:  ldloc.s    V_6
  IL_0026:  ret
}");
        }

        /// <summary>
        /// Update another method (without directly referencing
        /// anonymous type) after updating method with anonymous type.
        /// </summary>
        [Fact]
        public void AnonymousTypes_SkipGeneration_2()
        {
            var source0 =
@"class C
{
    static object F()
    {
        var x = new { A = 1 };
        return x.A;
    }
    static object G()
    {
        var x = 1;
        return x;
    }
}";
            var source1 =
@"class C
{
    static object F()
    {
        var x = new { A = 2, B = 3 };
        return x.A;
    }
    static object G()
    {
        var x = 1;
        return x;
    }
}";
            var source2 =
@"class C
{
    static object F()
    {
        var x = new { A = 2, B = 3 };
        return x.A;
    }
    static object G()
    {
        var x = 1;
        return x + 1;
    }
}";
            var source3 =
@"class C
{
    static object F()
    {
        var x = new { A = 2, B = 3 };
        return x.A;
    }
    static object G()
    {
        var x = new { A = (object)null };
        var y = new { A = 'a', B = 'b' };
        return x;
    }
}";
            var compilation0 = CreateCompilation(source0, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);
            var compilation2 = compilation1.WithSource(source2);
            var compilation3 = compilation2.WithSource(source3);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");
            var g1 = compilation1.GetMember<MethodSymbol>("C.G");
            var g2 = compilation2.GetMember<MethodSymbol>("C.G");
            var g3 = compilation3.GetMember<MethodSymbol>("C.G");

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(testData: testData0);
            using var md0 = ModuleMetadata.CreateFromImage(bytes0);

            var generation0 = EmitBaseline.CreateInitialBaseline(
                md0,
                m => md0.MetadataReader.GetString(md0.MetadataReader.GetMethodDefinition(m).Name) switch
                {
                    "F" => testData0.GetMethodData("C.F").GetEncDebugInfo(),
                    "G" => testData0.GetMethodData("C.G").GetEncDebugInfo(),
                    _ => default,
                });

            var reader0 = md0.MetadataReader;
            CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "<>f__AnonymousType0`1", "C");

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, f0, f1, GetEquivalentNodesMap(f1, f0), preserveLocalVariables: true)));

            using var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;
            var readers = new List<MetadataReader> { reader0, reader1 };

            CheckNames(readers, reader1.GetTypeDefNames(), "<>f__AnonymousType1`2"); // one additional type

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, g1, g2, GetEquivalentNodesMap(g2, g1), preserveLocalVariables: true)));

            using var md2 = diff2.GetMetadata();
            var reader2 = md2.Reader;
            readers.Add(reader2);

            CheckNames(readers, reader2.GetTypeDefNames()); // no additional types

            var diff3 = compilation3.EmitDifference(
                diff2.NextGeneration,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, g2, g3, GetEquivalentNodesMap(g3, g2), preserveLocalVariables: true)));

            using var md3 = diff3.GetMetadata();
            var reader3 = md3.Reader;
            readers.Add(reader3);

            CheckNames(readers, reader3.GetTypeDefNames()); // no additional types
        }

        /// <summary>
        /// Local from previous generation is of an anonymous
        /// type not available in next generation.
        /// </summary>
        [Fact]
        public void AnonymousTypes_AddThenDelete()
        {
            var source0 =
@"class C
{
    object A;
    static object F()
    {
        var x = new C();
        var y = x.A;
        return y;
    }
}";
            var source1 =
@"class C
{
    static object F()
    {
        var x = new { A = new object() };
        var y = x.A;
        return y;
    }
}";
            var source2 =
@"class C
{
    static object F()
    {
        var x = new { A = new object(), B = 2 };
        var y = x.A;
        y = new { B = new object() }.B;
        return y;
    }
}";
            var source3 =
@"class C
{
    static object F()
    {
        var x = new { A = new object(), B = 3 };
        var y = x.A;
        return y;
    }
}";
            var source4 =
@"class C
{
    static object F()
    {
        var x = new { B = 4, A = new object() };
        var y = x.A;
        return y;
    }
}";
            var compilation0 = CreateCompilation(source0, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);
            var compilation2 = compilation1.WithSource(source2);
            var compilation3 = compilation2.WithSource(source3);
            var compilation4 = compilation3.WithSource(source4);

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(testData: testData0);
            using var md0 = ModuleMetadata.CreateFromImage(bytes0);
            var generation0 = EmitBaseline.CreateInitialBaseline(md0, testData0.GetMethodData("C.F").EncDebugInfoProvider());

            var method0 = compilation0.GetMember<MethodSymbol>("C.F");
            var reader0 = md0.MetadataReader;
            CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "C");

            var method1 = compilation1.GetMember<MethodSymbol>("C.F");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables: true)));
            using var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;
            CheckNames(new[] { reader0, reader1 }, reader1.GetTypeDefNames(), "<>f__AnonymousType0`1"); // one additional type

            diff1.VerifyIL("C.F", @"
{
  // Code size       27 (0x1b)
  .maxstack  1
  .locals init ([unchanged] V_0,
                object V_1, //y
                [object] V_2,
                <>f__AnonymousType0<object> V_3, //x
                object V_4)
  IL_0000:  nop
  IL_0001:  newobj     ""object..ctor()""
  IL_0006:  newobj     ""<>f__AnonymousType0<object>..ctor(object)""
  IL_000b:  stloc.3
  IL_000c:  ldloc.3
  IL_000d:  callvirt   ""object <>f__AnonymousType0<object>.A.get""
  IL_0012:  stloc.1
  IL_0013:  ldloc.1
  IL_0014:  stloc.s    V_4
  IL_0016:  br.s       IL_0018
  IL_0018:  ldloc.s    V_4
  IL_001a:  ret
}");

            var method2 = compilation2.GetMember<MethodSymbol>("C.F");
        }

        [Fact]
        public void AnonymousTypes_DifferentCase()
        {
            var source0 = MarkedSource(@"
class C
{
    static void M()
    {
        var <N:0>x = new { A = 1, B = 2 }</N:0>;
        var <N:1>y = new { a = 3, b = 4 }</N:1>;
    }
}");
            var source1 = MarkedSource(@"
class C
{
    static void M()
    {
        var <N:0>x = new { a = 1, B = 2 }</N:0>;
        var <N:1>y = new { AB = 3 }</N:1>;
    }
}");
            var source2 = MarkedSource(@"
class C
{
    static void M()
    {
        var <N:0>x = new { a = 1, B = 2 }</N:0>;
        var <N:1>y = new { Ab = 5 }</N:1>;
    }
}");
            var compilation0 = CreateCompilation(source0.Tree, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);
            var compilation2 = compilation1.WithSource(source2.Tree);

            var v0 = CompileAndVerify(compilation0);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);
            var reader0 = md0.MetadataReader;

            var m0 = compilation0.GetMember<MethodSymbol>("C.M");
            var m1 = compilation1.GetMember<MethodSymbol>("C.M");
            var m2 = compilation2.GetMember<MethodSymbol>("C.M");

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "<>f__AnonymousType0`2", "<>f__AnonymousType1`2", "C");

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, m0, m1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            var reader1 = diff1.GetMetadata().Reader;
            CheckNames(new[] { reader0, reader1 }, reader1.GetTypeDefNames(), "<>f__AnonymousType2`2", "<>f__AnonymousType3`1");

            // the first two slots can't be reused since the type changed
            diff1.VerifyIL("C.M",
@"{
  // Code size       17 (0x11)
  .maxstack  2
  .locals init ([unchanged] V_0,
                [unchanged] V_1,
                <>f__AnonymousType2<int, int> V_2, //x
                <>f__AnonymousType3<int> V_3) //y
  IL_0000:  nop
  IL_0001:  ldc.i4.1
  IL_0002:  ldc.i4.2
  IL_0003:  newobj     ""<>f__AnonymousType2<int, int>..ctor(int, int)""
  IL_0008:  stloc.2
  IL_0009:  ldc.i4.3
  IL_000a:  newobj     ""<>f__AnonymousType3<int>..ctor(int)""
  IL_000f:  stloc.3
  IL_0010:  ret
}");

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, m1, m2, GetSyntaxMapFromMarkers(source1, source2), preserveLocalVariables: true)));

            var reader2 = diff2.GetMetadata().Reader;
            CheckNames(new[] { reader0, reader1, reader2 }, reader2.GetTypeDefNames(), "<>f__AnonymousType4`1");

            // we can reuse slot for "x", it's type haven't changed
            diff2.VerifyIL("C.M",
@"{
  // Code size       18 (0x12)
  .maxstack  2
  .locals init ([unchanged] V_0,
                [unchanged] V_1,
                <>f__AnonymousType2<int, int> V_2, //x
                [unchanged] V_3,
                <>f__AnonymousType4<int> V_4) //y
  IL_0000:  nop
  IL_0001:  ldc.i4.1
  IL_0002:  ldc.i4.2
  IL_0003:  newobj     ""<>f__AnonymousType2<int, int>..ctor(int, int)""
  IL_0008:  stloc.2
  IL_0009:  ldc.i4.5
  IL_000a:  newobj     ""<>f__AnonymousType4<int>..ctor(int)""
  IL_000f:  stloc.s    V_4
  IL_0011:  ret
}");
        }

        [Fact]
        public void AnonymousTypes_Nested1()
        {
            var template = @"
using System;
using System.Linq;

class C
{
    static void F(string[] args)
    {
        var <N:0>result =
            from a in args
            <N:1>let x = a.Reverse()</N:1>
            <N:2>let y = x.Reverse()</N:2>
            <N:3>where x.SequenceEqual(y)</N:3>
            <N:4>select new { Value = a, Length = a.Length }</N:4></N:0>;

        Console.WriteLine(<<VALUE>>);
    }
}";
            var source0 = MarkedSource(template.Replace("<<VALUE>>", "0"));
            var source1 = MarkedSource(template.Replace("<<VALUE>>", "1"));
            var source2 = MarkedSource(template.Replace("<<VALUE>>", "2"));

            var compilation0 = CreateCompilationWithMscorlib45(new[] { source0.Tree }, new[] { SystemCoreRef }, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);
            var compilation2 = compilation0.WithSource(source2.Tree);

            var v0 = CompileAndVerify(compilation0);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");
            var f2 = compilation2.GetMember<MethodSymbol>("C.F");

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var expectedIL = @"
{
  // Code size      155 (0x9b)
  .maxstack  3
  .locals init (System.Collections.Generic.IEnumerable<<anonymous type: string Value, int Length>> V_0) //result
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldsfld     ""System.Func<string, <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x>> C.<>c.<>9__0_0""
  IL_0007:  dup
  IL_0008:  brtrue.s   IL_0021
  IL_000a:  pop
  IL_000b:  ldsfld     ""C.<>c C.<>c.<>9""
  IL_0010:  ldftn      ""<anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> C.<>c.<F>b__0_0(string)""
  IL_0016:  newobj     ""System.Func<string, <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x>>..ctor(object, System.IntPtr)""
  IL_001b:  dup
  IL_001c:  stsfld     ""System.Func<string, <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x>> C.<>c.<>9__0_0""
  IL_0021:  call       ""System.Collections.Generic.IEnumerable<<anonymous type: string a, System.Collections.Generic.IEnumerable<char> x>> System.Linq.Enumerable.Select<string, <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x>>(System.Collections.Generic.IEnumerable<string>, System.Func<string, <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x>>)""
  IL_0026:  ldsfld     ""System.Func<<anonymous type: string a, System.Collections.Generic.IEnumerable<char> x>, <anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>> C.<>c.<>9__0_1""
  IL_002b:  dup
  IL_002c:  brtrue.s   IL_0045
  IL_002e:  pop
  IL_002f:  ldsfld     ""C.<>c C.<>c.<>9""
  IL_0034:  ldftn      ""<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y> C.<>c.<F>b__0_1(<anonymous type: string a, System.Collections.Generic.IEnumerable<char> x>)""
  IL_003a:  newobj     ""System.Func<<anonymous type: string a, System.Collections.Generic.IEnumerable<char> x>, <anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>>..ctor(object, System.IntPtr)""
  IL_003f:  dup
  IL_0040:  stsfld     ""System.Func<<anonymous type: string a, System.Collections.Generic.IEnumerable<char> x>, <anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>> C.<>c.<>9__0_1""
  IL_0045:  call       ""System.Collections.Generic.IEnumerable<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>> System.Linq.Enumerable.Select<<anonymous type: string a, System.Collections.Generic.IEnumerable<char> x>, <anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>>(System.Collections.Generic.IEnumerable<<anonymous type: string a, System.Collections.Generic.IEnumerable<char> x>>, System.Func<<anonymous type: string a, System.Collections.Generic.IEnumerable<char> x>, <anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>>)""
  IL_004a:  ldsfld     ""System.Func<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, bool> C.<>c.<>9__0_2""
  IL_004f:  dup
  IL_0050:  brtrue.s   IL_0069
  IL_0052:  pop
  IL_0053:  ldsfld     ""C.<>c C.<>c.<>9""
  IL_0058:  ldftn      ""bool C.<>c.<F>b__0_2(<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>)""
  IL_005e:  newobj     ""System.Func<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, bool>..ctor(object, System.IntPtr)""
  IL_0063:  dup
  IL_0064:  stsfld     ""System.Func<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, bool> C.<>c.<>9__0_2""
  IL_0069:  call       ""System.Collections.Generic.IEnumerable<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>> System.Linq.Enumerable.Where<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>>(System.Collections.Generic.IEnumerable<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>>, System.Func<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, bool>)""
  IL_006e:  ldsfld     ""System.Func<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, <anonymous type: string Value, int Length>> C.<>c.<>9__0_3""
  IL_0073:  dup
  IL_0074:  brtrue.s   IL_008d
  IL_0076:  pop
  IL_0077:  ldsfld     ""C.<>c C.<>c.<>9""
  IL_007c:  ldftn      ""<anonymous type: string Value, int Length> C.<>c.<F>b__0_3(<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>)""
  IL_0082:  newobj     ""System.Func<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, <anonymous type: string Value, int Length>>..ctor(object, System.IntPtr)""
  IL_0087:  dup
  IL_0088:  stsfld     ""System.Func<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, <anonymous type: string Value, int Length>> C.<>c.<>9__0_3""
  IL_008d:  call       ""System.Collections.Generic.IEnumerable<<anonymous type: string Value, int Length>> System.Linq.Enumerable.Select<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, <anonymous type: string Value, int Length>>(System.Collections.Generic.IEnumerable<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>>, System.Func<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, <anonymous type: string Value, int Length>>)""
  IL_0092:  stloc.0
  IL_0093:  ldc.i4.<<VALUE>>
  IL_0094:  call       ""void System.Console.WriteLine(int)""
  IL_0099:  nop
  IL_009a:  ret
}
";
            v0.VerifyIL("C.F", expectedIL.Replace("<<VALUE>>", "0"));

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            diff1.VerifySynthesizedMembers(
                "C: {<>c}",
                "C.<>c: {<>9__0_0, <>9__0_1, <>9__0_2, <>9__0_3, <F>b__0_0, <F>b__0_1, <F>b__0_2, <F>b__0_3}",
                "<>f__AnonymousType2<<Value>j__TPar, <Length>j__TPar>: {Equals, GetHashCode, ToString}",
                "<>f__AnonymousType1<<<>h__TransparentIdentifier0>j__TPar, <y>j__TPar>: {Equals, GetHashCode, ToString}",
                "<>f__AnonymousType0<<a>j__TPar, <x>j__TPar>: {Equals, GetHashCode, ToString}");

            diff1.VerifyIL("C.F", expectedIL.Replace("<<VALUE>>", "1"));

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, f1, f2, GetSyntaxMapFromMarkers(source1, source2), preserveLocalVariables: true)));

            diff2.VerifySynthesizedMembers(
                "C: {<>c}",
                "C.<>c: {<>9__0_0, <>9__0_1, <>9__0_2, <>9__0_3, <F>b__0_0, <F>b__0_1, <F>b__0_2, <F>b__0_3}",
                "<>f__AnonymousType2<<Value>j__TPar, <Length>j__TPar>: {Equals, GetHashCode, ToString}",
                "<>f__AnonymousType1<<<>h__TransparentIdentifier0>j__TPar, <y>j__TPar>: {Equals, GetHashCode, ToString}",
                "<>f__AnonymousType0<<a>j__TPar, <x>j__TPar>: {Equals, GetHashCode, ToString}");

            diff2.VerifyIL("C.F", expectedIL.Replace("<<VALUE>>", "2"));
        }

        [Fact]
        public void AnonymousTypes_Nested2()
        {
            var template = @"
using System;
using System.Linq;

class C
{
    static void F(string[] args)
    {
        var <N:0>result =
            from a in args
            <N:1>let x = a.Reverse()</N:1>
            <N:2>let y = x.Reverse()</N:2>
            <N:3>where x.SequenceEqual(y)</N:3>
            <N:4>select new { Value = a, Length = a.Length }</N:4></N:0>;

        Console.WriteLine(<<VALUE>>);
    }
}";
            var source0 = MarkedSource(template.Replace("<<VALUE>>", "0"));
            var source1 = MarkedSource(template.Replace("<<VALUE>>", "1"));
            var source2 = MarkedSource(template.Replace("<<VALUE>>", "2"));

            var compilation0 = CreateCompilationWithMscorlib45(new[] { source0.Tree }, new[] { SystemCoreRef }, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);
            var compilation2 = compilation0.WithSource(source2.Tree);

            var v0 = CompileAndVerify(compilation0);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");
            var f2 = compilation2.GetMember<MethodSymbol>("C.F");

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var expectedIL = @"
{
  // Code size      155 (0x9b)
  .maxstack  3
  .locals init (System.Collections.Generic.IEnumerable<<anonymous type: string Value, int Length>> V_0) //result
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldsfld     ""System.Func<string, <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x>> C.<>c.<>9__0_0""
  IL_0007:  dup
  IL_0008:  brtrue.s   IL_0021
  IL_000a:  pop
  IL_000b:  ldsfld     ""C.<>c C.<>c.<>9""
  IL_0010:  ldftn      ""<anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> C.<>c.<F>b__0_0(string)""
  IL_0016:  newobj     ""System.Func<string, <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x>>..ctor(object, System.IntPtr)""
  IL_001b:  dup
  IL_001c:  stsfld     ""System.Func<string, <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x>> C.<>c.<>9__0_0""
  IL_0021:  call       ""System.Collections.Generic.IEnumerable<<anonymous type: string a, System.Collections.Generic.IEnumerable<char> x>> System.Linq.Enumerable.Select<string, <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x>>(System.Collections.Generic.IEnumerable<string>, System.Func<string, <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x>>)""
  IL_0026:  ldsfld     ""System.Func<<anonymous type: string a, System.Collections.Generic.IEnumerable<char> x>, <anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>> C.<>c.<>9__0_1""
  IL_002b:  dup
  IL_002c:  brtrue.s   IL_0045
  IL_002e:  pop
  IL_002f:  ldsfld     ""C.<>c C.<>c.<>9""
  IL_0034:  ldftn      ""<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y> C.<>c.<F>b__0_1(<anonymous type: string a, System.Collections.Generic.IEnumerable<char> x>)""
  IL_003a:  newobj     ""System.Func<<anonymous type: string a, System.Collections.Generic.IEnumerable<char> x>, <anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>>..ctor(object, System.IntPtr)""
  IL_003f:  dup
  IL_0040:  stsfld     ""System.Func<<anonymous type: string a, System.Collections.Generic.IEnumerable<char> x>, <anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>> C.<>c.<>9__0_1""
  IL_0045:  call       ""System.Collections.Generic.IEnumerable<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>> System.Linq.Enumerable.Select<<anonymous type: string a, System.Collections.Generic.IEnumerable<char> x>, <anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>>(System.Collections.Generic.IEnumerable<<anonymous type: string a, System.Collections.Generic.IEnumerable<char> x>>, System.Func<<anonymous type: string a, System.Collections.Generic.IEnumerable<char> x>, <anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>>)""
  IL_004a:  ldsfld     ""System.Func<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, bool> C.<>c.<>9__0_2""
  IL_004f:  dup
  IL_0050:  brtrue.s   IL_0069
  IL_0052:  pop
  IL_0053:  ldsfld     ""C.<>c C.<>c.<>9""
  IL_0058:  ldftn      ""bool C.<>c.<F>b__0_2(<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>)""
  IL_005e:  newobj     ""System.Func<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, bool>..ctor(object, System.IntPtr)""
  IL_0063:  dup
  IL_0064:  stsfld     ""System.Func<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, bool> C.<>c.<>9__0_2""
  IL_0069:  call       ""System.Collections.Generic.IEnumerable<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>> System.Linq.Enumerable.Where<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>>(System.Collections.Generic.IEnumerable<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>>, System.Func<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, bool>)""
  IL_006e:  ldsfld     ""System.Func<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, <anonymous type: string Value, int Length>> C.<>c.<>9__0_3""
  IL_0073:  dup
  IL_0074:  brtrue.s   IL_008d
  IL_0076:  pop
  IL_0077:  ldsfld     ""C.<>c C.<>c.<>9""
  IL_007c:  ldftn      ""<anonymous type: string Value, int Length> C.<>c.<F>b__0_3(<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>)""
  IL_0082:  newobj     ""System.Func<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, <anonymous type: string Value, int Length>>..ctor(object, System.IntPtr)""
  IL_0087:  dup
  IL_0088:  stsfld     ""System.Func<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, <anonymous type: string Value, int Length>> C.<>c.<>9__0_3""
  IL_008d:  call       ""System.Collections.Generic.IEnumerable<<anonymous type: string Value, int Length>> System.Linq.Enumerable.Select<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, <anonymous type: string Value, int Length>>(System.Collections.Generic.IEnumerable<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>>, System.Func<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, <anonymous type: string Value, int Length>>)""
  IL_0092:  stloc.0
  IL_0093:  ldc.i4.<<VALUE>>
  IL_0094:  call       ""void System.Console.WriteLine(int)""
  IL_0099:  nop
  IL_009a:  ret
}
";
            v0.VerifyIL("C.F", expectedIL.Replace("<<VALUE>>", "0"));

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            diff1.VerifySynthesizedMembers(
                "C: {<>c}",
                "C.<>c: {<>9__0_0, <>9__0_1, <>9__0_2, <>9__0_3, <F>b__0_0, <F>b__0_1, <F>b__0_2, <F>b__0_3}",
                "<>f__AnonymousType2<<Value>j__TPar, <Length>j__TPar>: {Equals, GetHashCode, ToString}",
                "<>f__AnonymousType1<<<>h__TransparentIdentifier0>j__TPar, <y>j__TPar>: {Equals, GetHashCode, ToString}",
                "<>f__AnonymousType0<<a>j__TPar, <x>j__TPar>: {Equals, GetHashCode, ToString}");

            diff1.VerifyIL("C.F", expectedIL.Replace("<<VALUE>>", "1"));

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, f1, f2, GetSyntaxMapFromMarkers(source1, source2), preserveLocalVariables: true)));

            diff2.VerifySynthesizedMembers(
                "C: {<>c}",
                "C.<>c: {<>9__0_0, <>9__0_1, <>9__0_2, <>9__0_3, <F>b__0_0, <F>b__0_1, <F>b__0_2, <F>b__0_3}",
                "<>f__AnonymousType2<<Value>j__TPar, <Length>j__TPar>: {Equals, GetHashCode, ToString}",
                "<>f__AnonymousType1<<<>h__TransparentIdentifier0>j__TPar, <y>j__TPar>: {Equals, GetHashCode, ToString}",
                "<>f__AnonymousType0<<a>j__TPar, <x>j__TPar>: {Equals, GetHashCode, ToString}");

            diff2.VerifyIL("C.F", expectedIL.Replace("<<VALUE>>", "2"));
        }

        [Fact]
        public void AnonymousTypes_Query1()
        {
            var source0 = MarkedSource(@"
using System.Linq;

class C
{
    static void F(string[] args)
    {
        args = new[] { ""a"", ""bB"", ""Cc"", ""DD"" };
        var <N:4>result =
            from a in args
            <N:0>let x = a.Reverse()</N:0>
            <N:1>let y = x.Reverse()</N:1>
            <N:2>where x.SequenceEqual(y)</N:2>
            <N:3>select new { Value = a, Length = a.Length }</N:3></N:4>;

        var <N:8>newArgs =
            from a in result
            <N:5>let value = a.Value</N:5>
            <N:6>let length = a.Length</N:6>
            <N:7>where value.Length == length</N:7>
            select value</N:8>;

        args = args.Concat(newArgs).ToArray();
        System.Diagnostics.Debugger.Break();
        result.ToString();
    }
}
");
            var source1 = MarkedSource(@"
using System.Linq;

class C
{
    static void F(string[] args)
    {
        args = new[] { ""a"", ""bB"", ""Cc"", ""DD"" };
        var list = false ? null : new { Head = (dynamic)null, Tail = (dynamic)null };
        for (int i = 0; i < 10; i++)
        {
            var <N:4>result =
                from a in args
                <N:0>let x = a.Reverse()</N:0>
                <N:1>let y = x.Reverse()</N:1>
                <N:2>where x.SequenceEqual(y)</N:2>
                orderby a.Length ascending, a descending
                <N:3>select new { Value = a, Length = x.Count() }</N:3></N:4>;

            var linked = result.Aggregate(
                false ? new { Head = (string)null, Tail = (dynamic)null } : null,
                (total, curr) => new { Head = curr.Value, Tail = (dynamic)total });

            var str = linked?.Tail?.Head;

            var <N:8>newArgs =
                from a in result
                <N:5>let value = a.Value</N:5>
                <N:6>let length = a.Length</N:6>
                <N:7>where value.Length == length</N:7>
                select value + value</N:8>;

            args = args.Concat(newArgs).ToArray();
            list = new { Head = (dynamic)i, Tail = (dynamic)list };
            System.Diagnostics.Debugger.Break();
        }
        System.Diagnostics.Debugger.Break();
    }
}
");
            var compilation0 = CreateCompilationWithMscorlib45(new[] { source0.Tree }, new[] { SystemCoreRef, CSharpRef }, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);

            var v0 = CompileAndVerify(compilation0);
            v0.VerifyDiagnostics();
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            v0.VerifyLocalSignature("C.F", @"
.locals init (System.Collections.Generic.IEnumerable<<anonymous type: string Value, int Length>> V_0, //result
              System.Collections.Generic.IEnumerable<string> V_1) //newArgs
");

            var diff1 = compilation1.EmitDifference(
                 generation0,
                 ImmutableArray.Create(
                     new SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            diff1.VerifySynthesizedMembers(
                "C.<>o__0#1: {<>p__0}",
                "C: {<>o__0#1, <>c}",
                "C.<>c: {<>9__0_0, <>9__0_1, <>9__0_2, <>9__0_3#1, <>9__0_4#1, <>9__0_3, <>9__0_6#1, <>9__0_4, <>9__0_5, <>9__0_6, <>9__0_10#1, <F>b__0_0, <F>b__0_1, <F>b__0_2, <F>b__0_3#1, <F>b__0_4#1, <F>b__0_3, <F>b__0_6#1, <F>b__0_4, <F>b__0_5, <F>b__0_6, <F>b__0_10#1}",
                "<>f__AnonymousType4<<<>h__TransparentIdentifier0>j__TPar, <length>j__TPar>: {Equals, GetHashCode, ToString}",
                "<>f__AnonymousType2<<Value>j__TPar, <Length>j__TPar>: {Equals, GetHashCode, ToString}",
                "<>f__AnonymousType5<<Head>j__TPar, <Tail>j__TPar>: {Equals, GetHashCode, ToString}",
                "<>f__AnonymousType3<<a>j__TPar, <value>j__TPar>: {Equals, GetHashCode, ToString}",
                "<>f__AnonymousType0<<a>j__TPar, <x>j__TPar>: {Equals, GetHashCode, ToString}",
                "<>f__AnonymousType1<<<>h__TransparentIdentifier0>j__TPar, <y>j__TPar>: {Equals, GetHashCode, ToString}");

            diff1.VerifyLocalSignature("C.F", @"
  .locals init (System.Collections.Generic.IEnumerable<<anonymous type: string Value, int Length>> V_0, //result
                System.Collections.Generic.IEnumerable<string> V_1, //newArgs
                <>f__AnonymousType5<dynamic, dynamic> V_2, //list
                int V_3, //i
                <>f__AnonymousType5<string, dynamic> V_4, //linked
                object V_5, //str
                <>f__AnonymousType5<string, dynamic> V_6,
                object V_7,
                bool V_8)
");
        }

        [Fact]
        public void AnonymousTypes_Dynamic1()
        {
            var template = @"
using System;

class C
{
    public void F()
    {
        var <N:0>x = new { A = (dynamic)null, B = 1 }</N:0>;
        Console.WriteLine(x.B + <<VALUE>>);
    }
}
";
            var source0 = MarkedSource(template.Replace("<<VALUE>>", "0"));
            var source1 = MarkedSource(template.Replace("<<VALUE>>", "1"));
            var source2 = MarkedSource(template.Replace("<<VALUE>>", "2"));

            var compilation0 = CreateCompilationWithMscorlib45(new[] { source0.Tree }, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);
            var compilation2 = compilation1.WithSource(source2.Tree);

            var v0 = CompileAndVerify(compilation0);
            v0.VerifyDiagnostics();
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");
            var f2 = compilation2.GetMember<MethodSymbol>("C.F");

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var baselineIL0 = @"
{
  // Code size       22 (0x16)
  .maxstack  2
  .locals init (<>f__AnonymousType0<dynamic, int> V_0) //x
  IL_0000:  nop
  IL_0001:  ldnull
  IL_0002:  ldc.i4.1
  IL_0003:  newobj     ""<>f__AnonymousType0<dynamic, int>..ctor(dynamic, int)""
  IL_0008:  stloc.0
  IL_0009:  ldloc.0
  IL_000a:  callvirt   ""int <>f__AnonymousType0<dynamic, int>.B.get""
  IL_000f:  call       ""void System.Console.WriteLine(int)""
  IL_0014:  nop
  IL_0015:  ret
}
";
            v0.VerifyIL("C.F", baselineIL0);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            diff1.VerifySynthesizedMembers(
                "<>f__AnonymousType0<<A>j__TPar, <B>j__TPar>: {Equals, GetHashCode, ToString}");


            var baselineIL = @"
{
  // Code size       24 (0x18)
  .maxstack  2
  .locals init (<>f__AnonymousType0<dynamic, int> V_0) //x
  IL_0000:  nop
  IL_0001:  ldnull
  IL_0002:  ldc.i4.1
  IL_0003:  newobj     ""<>f__AnonymousType0<dynamic, int>..ctor(dynamic, int)""
  IL_0008:  stloc.0
  IL_0009:  ldloc.0
  IL_000a:  callvirt   ""int <>f__AnonymousType0<dynamic, int>.B.get""
  IL_000f:  ldc.i4.<<VALUE>>
  IL_0010:  add
  IL_0011:  call       ""void System.Console.WriteLine(int)""
  IL_0016:  nop
  IL_0017:  ret
}
";

            diff1.VerifyIL("C.F", baselineIL.Replace("<<VALUE>>", "1"));

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, f1, f2, GetSyntaxMapFromMarkers(source1, source2), preserveLocalVariables: true)));

            diff2.VerifySynthesizedMembers(
                "<>f__AnonymousType0<<A>j__TPar, <B>j__TPar>: {Equals, GetHashCode, ToString}");

            diff2.VerifyIL("C.F", baselineIL.Replace("<<VALUE>>", "2"));
        }

        /// <summary>
        /// Should not re-use locals if the method metadata
        /// signature is unsupported.
        /// </summary>
        [WorkItem(9849, "https://github.com/dotnet/roslyn/issues/9849")]
        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/9849")]
        public void LocalType_UnsupportedSignatureContent()
        {
            // Equivalent to C#, but with extra local and required modifier on
            // expected local. Used to generate initial (unsupported) metadata.
            var ilSource =
@".assembly extern mscorlib { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89) }
.assembly '<<GeneratedFileName>>' { }
.class C
{
  .method public specialname rtspecialname instance void .ctor()
  {
    ret
  }
  .method private static object F()
  {
    ldnull
    ret
  }
  .method private static void M1()
  {
    .locals init ([0] object other, [1] object modreq(int32) o)
    call object C::F()
    stloc.1
    ldloc.1
    call void C::M2(object)
    ret
  }
  .method private static void M2(object o)
  {
    ret
  }
}";
            var source =
@"class C
{
    static object F()
    {
        return null;
    }
    static void M1()
    {
        object o = F();
        M2(o);
    }
    static void M2(object o)
    {
    }
}";
            ImmutableArray<byte> assemblyBytes;
            ImmutableArray<byte> pdbBytes;
            EmitILToArray(ilSource, appendDefaultHeader: false, includePdb: false, assemblyBytes: out assemblyBytes, pdbBytes: out pdbBytes);
            var md0 = ModuleMetadata.CreateFromImage(assemblyBytes);
            // Still need a compilation with source for the initial
            // generation - to get a MethodSymbol and syntax map.
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source);

            var method0 = compilation0.GetMember<MethodSymbol>("C.M1");
            var generation0 = EmitBaseline.CreateInitialBaseline(md0, m => default);

            var method1 = compilation1.GetMember<MethodSymbol>("C.M1");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables: true)));

            diff1.VerifyIL("C.M1",
@"{
  // Code size       15 (0xf)
  .maxstack  1
  .locals init (object V_0) //o
  IL_0000:  nop
  IL_0001:  call       ""object C.F()""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  call       ""void C.M2(object)""
  IL_000d:  nop
  IL_000e:  ret
}");
        }

        /// <summary>
        /// Should not re-use locals with custom modifiers.
        /// </summary>
        [WorkItem(9848, "https://github.com/dotnet/roslyn/issues/9848")]
        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/9848")]
        public void LocalType_CustomModifiers()
        {
            // Equivalent method signature to C#, but
            // with optional modifier on locals.
            var ilSource =
@".assembly extern mscorlib { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89) }
.assembly '<<GeneratedFileName>>' { }
.class public C
{
  .method public specialname rtspecialname instance void .ctor()
  {
    ret
  }
  .method public static object F(class [mscorlib]System.IDisposable d)
  {
    .locals init ([0] class C modopt(int32) c,
                  [1] class [mscorlib]System.IDisposable modopt(object),
                  [2] bool V_2,
                  [3] object V_3)
    ldnull
    ret
  }
}";
            var source =
@"class C
{
    static object F(System.IDisposable d)
    {
        C c;
        using (d)
        {
            c = (C)d;
        }
        return c;
    }
}";
            var metadata0 = (MetadataImageReference)CompileIL(ilSource, prependDefaultHeader: false);
            // Still need a compilation with source for the initial
            // generation - to get a MethodSymbol and syntax map.
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source);

            var moduleMetadata0 = ((AssemblyMetadata)metadata0.GetMetadataNoCopy()).GetModules()[0];
            var method0 = compilation0.GetMember<MethodSymbol>("C.F");
            var generation0 = EmitBaseline.CreateInitialBaseline(
                moduleMetadata0,
                m => default);

            var method1 = compilation1.GetMember<MethodSymbol>("C.F");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables: true)));

            diff1.VerifyIL("C.F", @"
{
  // Code size       38 (0x26)
  .maxstack  1
  .locals init ([unchanged] V_0,
                [unchanged] V_1,
                [bool] V_2,
                [object] V_3,
                C V_4, //c
                System.IDisposable V_5,
                object V_6)
 -IL_0000:  nop
 -IL_0001:  ldarg.0
  IL_0002:  stloc.s    V_5
  .try
  {
   -IL_0004:  nop
   -IL_0005:  ldarg.0
    IL_0006:  castclass  ""C""
    IL_000b:  stloc.s    V_4
   -IL_000d:  nop
    IL_000e:  leave.s    IL_001d
  }
  finally
  {
   ~IL_0010:  ldloc.s    V_5
    IL_0012:  brfalse.s  IL_001c
    IL_0014:  ldloc.s    V_5
    IL_0016:  callvirt   ""void System.IDisposable.Dispose()""
    IL_001b:  nop
    IL_001c:  endfinally
  }
 -IL_001d:  ldloc.s    V_4
  IL_001f:  stloc.s    V_6
  IL_0021:  br.s       IL_0023
 -IL_0023:  ldloc.s    V_6
  IL_0025:  ret
}", methodToken: diff1.UpdatedMethods.Single());
        }

        /// <summary>
        /// Temporaries for locals used within a single
        /// statement should not be preserved.
        /// </summary>
        [Fact]
        public void TemporaryLocals_Other()
        {
            // Use increment as an example of a compiler generated
            // temporary that does not span multiple statements.
            var source =
@"class C
{
    int P { get; set; }
    static int M()
    {
        var c = new C();
        return c.P++;
    }
}";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source);

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(testData: testData0);
            var methodData0 = testData0.GetMethodData("C.M");
            var method0 = compilation0.GetMember<MethodSymbol>("C.M");
            var generation0 = EmitBaseline.CreateInitialBaseline(
                ModuleMetadata.CreateFromImage(bytes0),
                methodData0.EncDebugInfoProvider());

            var method1 = compilation1.GetMember<MethodSymbol>("C.M");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables: true)));

            diff1.VerifyIL("C.M", @"
{
  // Code size       32 (0x20)
  .maxstack  3
  .locals init (C V_0, //c
                [int] V_1,
                [int] V_2,
                int V_3,
                int V_4)
  IL_0000:  nop
  IL_0001:  newobj     ""C..ctor()""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  dup
  IL_0009:  callvirt   ""int C.P.get""
  IL_000e:  stloc.3
  IL_000f:  ldloc.3
  IL_0010:  ldc.i4.1
  IL_0011:  add
  IL_0012:  callvirt   ""void C.P.set""
  IL_0017:  nop
  IL_0018:  ldloc.3
  IL_0019:  stloc.s    V_4
  IL_001b:  br.s       IL_001d
  IL_001d:  ldloc.s    V_4
  IL_001f:  ret
}");
        }

        /// <summary>
        /// Local names array (from PDB) may have fewer slots than method
        /// signature (from metadata) when the trailing slots are unnamed.
        /// </summary>
        [WorkItem(782270, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/782270")]
        [Fact]
        public void Bug782270()
        {
            var source =
@"class C
{
    static System.IDisposable F()
    {
        return null;
    }
    static void M()
    {
        using (var o = F())
        {
        }
    }
}";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source);

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(testData: testData0);
            var methodData0 = testData0.GetMethodData("C.M");
            var method0 = compilation0.GetMember<MethodSymbol>("C.M");
            var generation0 = EmitBaseline.CreateInitialBaseline(
                ModuleMetadata.CreateFromImage(bytes0),
                testData0.GetMethodData("C.M").EncDebugInfoProvider());

            testData0.GetMethodData("C.M").VerifyIL(@"
{
  // Code size       23 (0x17)
  .maxstack  1
  .locals init (System.IDisposable V_0) //o
  IL_0000:  nop       
  IL_0001:  call       ""System.IDisposable C.F()""
  IL_0006:  stloc.0   
  .try
  {
    IL_0007:  nop       
    IL_0008:  nop       
    IL_0009:  leave.s    IL_0016
  }
  finally
  {
    IL_000b:  ldloc.0   
    IL_000c:  brfalse.s  IL_0015
    IL_000e:  ldloc.0   
    IL_000f:  callvirt   ""void System.IDisposable.Dispose()""
    IL_0014:  nop       
    IL_0015:  endfinally
  }
  IL_0016:  ret       
}");

            var method1 = compilation1.GetMember<MethodSymbol>("C.M");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables: true)));

            diff1.VerifyIL("C.M", @"
{
  // Code size       23 (0x17)
  .maxstack  1
  .locals init (System.IDisposable V_0) //o
  IL_0000:  nop       
  IL_0001:  call       ""System.IDisposable C.F()""
  IL_0006:  stloc.0   
  .try
  {
    IL_0007:  nop       
    IL_0008:  nop       
    IL_0009:  leave.s    IL_0016
  }
  finally
  {
    IL_000b:  ldloc.0   
    IL_000c:  brfalse.s  IL_0015
    IL_000e:  ldloc.0   
    IL_000f:  callvirt   ""void System.IDisposable.Dispose()""
    IL_0014:  nop       
    IL_0015:  endfinally
  }
  IL_0016:  ret       
}");
        }

        /// <summary>
        /// Similar to above test but with no named locals in original.
        /// </summary>
        [WorkItem(782270, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/782270")]
        [Fact]
        public void Bug782270_NoNamedLocals()
        {
            // Equivalent to C#, but with unnamed locals.
            // Used to generate initial metadata.
            var ilSource =
@".assembly extern netstandard { .ver 2:0:0:0 .publickeytoken = (cc 7b 13 ff cd 2d dd 51) }
.assembly '<<GeneratedFileName>>' { }
.class C extends object
{
  .method private static class [netstandard]System.IDisposable F()
  {
    ldnull
    ret
  }
  .method private static void M()
  {
    .locals init ([0] object, [1] object)
    ret
  }
}";
            var source0 =
@"class C
{
    static System.IDisposable F()
    {
        return null;
    }
    static void M()
    {
    }
}";
            var source1 =
@"class C
{
    static System.IDisposable F()
    {
        return null;
    }
    static void M()
    {
        using (var o = F())
        {
        }
    }
}";

            EmitILToArray(ilSource, appendDefaultHeader: false, includePdb: false, assemblyBytes: out var assemblyBytes, pdbBytes: out var pdbBytes);
            var md0 = ModuleMetadata.CreateFromImage(assemblyBytes);
            // Still need a compilation with source for the initial
            // generation - to get a MethodSymbol and syntax map.
            var compilation0 = CreateCompilation(source0, options: TestOptions.DebugDll, targetFramework: TargetFramework.NetStandard20);
            var compilation1 = compilation0.WithSource(source1);

            var method0 = compilation0.GetMember<MethodSymbol>("C.M");
            var generation0 = EmitBaseline.CreateInitialBaseline(md0, EmptyLocalsProvider);

            var method1 = compilation1.GetMember<MethodSymbol>("C.M");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables: true)));

            diff1.VerifyIL("C.M", @"
{
  // Code size       23 (0x17)
  .maxstack  1
  .locals init ([object] V_0,
                [object] V_1,
                System.IDisposable V_2) //o
  IL_0000:  nop
  IL_0001:  call       ""System.IDisposable C.F()""
  IL_0006:  stloc.2
  .try
  {
    IL_0007:  nop
    IL_0008:  nop
    IL_0009:  leave.s    IL_0016
  }
  finally
  {
    IL_000b:  ldloc.2
    IL_000c:  brfalse.s  IL_0015
    IL_000e:  ldloc.2
    IL_000f:  callvirt   ""void System.IDisposable.Dispose()""
    IL_0014:  nop
    IL_0015:  endfinally
  }
  IL_0016:  ret
}
");
        }

        [Fact]
        public void TemporaryLocals_ReferencedType()
        {
            var source =
@"class C
{
    static object F()
    {
        return null;
    }
    static void M()
    {
        var x = new System.Collections.Generic.HashSet<int>();
        x.Add(1);
    }
}";
            var compilation0 = CreateCompilation(source, new[] { CSharpRef }, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source);

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(testData: testData0);
            var methodData0 = testData0.GetMethodData("C.M");
            var method0 = compilation0.GetMember<MethodSymbol>("C.M");

            var modMeta = ModuleMetadata.CreateFromImage(bytes0);
            var generation0 = EmitBaseline.CreateInitialBaseline(
                modMeta,
                methodData0.EncDebugInfoProvider());

            var method1 = compilation1.GetMember<MethodSymbol>("C.M");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables: true)));

            diff1.VerifyIL("C.M",
@"
{
  // Code size       16 (0x10)
  .maxstack  2
  .locals init (System.Collections.Generic.HashSet<int> V_0) //x
  IL_0000:  nop
  IL_0001:  newobj     ""System.Collections.Generic.HashSet<int>..ctor()""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  ldc.i4.1
  IL_0009:  callvirt   ""bool System.Collections.Generic.HashSet<int>.Add(int)""
  IL_000e:  pop
  IL_000f:  ret
}
");
        }

        [WorkItem(770502, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/770502")]
        [WorkItem(839565, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/839565")]
        [Fact]
        public void DynamicOperations()
        {
            var source =
@"class A
{
    static object F = null;
    object x = ((dynamic)F) + 1;
    static A()
    {
        ((dynamic)F).F();
    }
    A() { }
    static void M(object o)
    {
        ((dynamic)o).x = 1;
    }
    static void N(A o)
    {
        o.x = 1;
    }
}
class B
{
    static object F = null;
    static object G = ((dynamic)F).F();
    object x = ((dynamic)F) + 1;
}";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll, references: new[] { CSharpRef });
            var compilation1 = compilation0.WithSource(source);

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(testData: testData0);
            using var md0 = ModuleMetadata.CreateFromImage(bytes0);
            // Source method with dynamic operations.
            var methodData0 = testData0.GetMethodData("A.M");
            var generation0 = EmitBaseline.CreateInitialBaseline(md0, methodData0.EncDebugInfoProvider());
            var method0 = compilation0.GetMember<MethodSymbol>("A.M");
            var method1 = compilation1.GetMember<MethodSymbol>("A.M");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables: true)));
            diff1.EmitResult.Diagnostics.Verify();

            // Source method with no dynamic operations.
            methodData0 = testData0.GetMethodData("A.N");
            generation0 = EmitBaseline.CreateInitialBaseline(md0, methodData0.EncDebugInfoProvider());
            method0 = compilation0.GetMember<MethodSymbol>("A.N");
            method1 = compilation1.GetMember<MethodSymbol>("A.N");
            diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables: true)));
            diff1.EmitResult.Diagnostics.Verify();

            // Explicit .ctor with dynamic operations.
            methodData0 = testData0.GetMethodData("A..ctor");
            generation0 = EmitBaseline.CreateInitialBaseline(md0, methodData0.EncDebugInfoProvider());
            method0 = compilation0.GetMember<MethodSymbol>("A..ctor");
            method1 = compilation1.GetMember<MethodSymbol>("A..ctor");
            diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables: true)));
            diff1.EmitResult.Diagnostics.Verify();

            // Explicit .cctor with dynamic operations.
            methodData0 = testData0.GetMethodData("A..cctor");
            generation0 = EmitBaseline.CreateInitialBaseline(md0, methodData0.EncDebugInfoProvider());
            method0 = compilation0.GetMember<MethodSymbol>("A..cctor");
            method1 = compilation1.GetMember<MethodSymbol>("A..cctor");
            diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables: true)));
            diff1.EmitResult.Diagnostics.Verify();

            // Implicit .ctor with dynamic operations.
            methodData0 = testData0.GetMethodData("B..ctor");
            generation0 = EmitBaseline.CreateInitialBaseline(md0, methodData0.EncDebugInfoProvider());
            method0 = compilation0.GetMember<MethodSymbol>("B..ctor");
            method1 = compilation1.GetMember<MethodSymbol>("B..ctor");
            diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables: true)));
            diff1.EmitResult.Diagnostics.Verify();

            // Implicit .cctor with dynamic operations.
            methodData0 = testData0.GetMethodData("B..cctor");
            generation0 = EmitBaseline.CreateInitialBaseline(md0, methodData0.EncDebugInfoProvider());
            method0 = compilation0.GetMember<MethodSymbol>("B..cctor");
            method1 = compilation1.GetMember<MethodSymbol>("B..cctor");
            diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables: true)));
            diff1.EmitResult.Diagnostics.Verify();
        }

        [Fact]
        public void DynamicLocals()
        {
            var template = @"
using System;

class C
{
    public void F()
    {
        dynamic <N:0>x = 1</N:0>;
        Console.WriteLine((int)x + <<VALUE>>);
    }
}
";
            var source0 = MarkedSource(template.Replace("<<VALUE>>", "0"));
            var source1 = MarkedSource(template.Replace("<<VALUE>>", "1"));
            var source2 = MarkedSource(template.Replace("<<VALUE>>", "2"));
            var compilation0 = CreateCompilationWithMscorlib45(new[] { source0.Tree }, new[] { SystemCoreRef, CSharpRef }, options: ComSafeDebugDll);

            var compilation1 = compilation0.WithSource(source1.Tree);
            var compilation2 = compilation1.WithSource(source2.Tree);

            var v0 = CompileAndVerify(compilation0);
            v0.VerifyDiagnostics();
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");
            var f2 = compilation2.GetMember<MethodSymbol>("C.F");

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            v0.VerifyIL("C.F", @"
{
  // Code size       82 (0x52)
  .maxstack  3
  .locals init (object V_0) //x
  IL_0000:  nop
  IL_0001:  ldc.i4.1
  IL_0002:  box        ""int""
  IL_0007:  stloc.0
  IL_0008:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>> C.<>o__0.<>p__0""
  IL_000d:  brfalse.s  IL_0011
  IL_000f:  br.s       IL_0036
  IL_0011:  ldc.i4.s   16
  IL_0013:  ldtoken    ""int""
  IL_0018:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001d:  ldtoken    ""C""
  IL_0022:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0027:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.Convert(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Type)""
  IL_002c:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0031:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>> C.<>o__0.<>p__0""
  IL_0036:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>> C.<>o__0.<>p__0""
  IL_003b:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>>.Target""
  IL_0040:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>> C.<>o__0.<>p__0""
  IL_0045:  ldloc.0
  IL_0046:  callvirt   ""int System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_004b:  call       ""void System.Console.WriteLine(int)""
  IL_0050:  nop
  IL_0051:  ret
}
");

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            diff1.VerifySynthesizedMembers(
                "C: {<>o__0#1}",
                "C.<>o__0#1: {<>p__0}");

            diff1.VerifyIL("C.F", @"
{
  // Code size       84 (0x54)
  .maxstack  3
  .locals init (object V_0) //x
  IL_0000:  nop
  IL_0001:  ldc.i4.1
  IL_0002:  box        ""int""
  IL_0007:  stloc.0
  IL_0008:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>> C.<>o__0#1.<>p__0""
  IL_000d:  brfalse.s  IL_0011
  IL_000f:  br.s       IL_0036
  IL_0011:  ldc.i4.s   16
  IL_0013:  ldtoken    ""int""
  IL_0018:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001d:  ldtoken    ""C""
  IL_0022:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0027:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.Convert(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Type)""
  IL_002c:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0031:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>> C.<>o__0#1.<>p__0""
  IL_0036:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>> C.<>o__0#1.<>p__0""
  IL_003b:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>>.Target""
  IL_0040:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>> C.<>o__0#1.<>p__0""
  IL_0045:  ldloc.0
  IL_0046:  callvirt   ""int System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_004b:  ldc.i4.1
  IL_004c:  add
  IL_004d:  call       ""void System.Console.WriteLine(int)""
  IL_0052:  nop
  IL_0053:  ret
}
");
            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, f1, f2, GetSyntaxMapFromMarkers(source1, source2), preserveLocalVariables: true)));

            diff2.VerifySynthesizedMembers(
                "C: {<>o__0#2, <>o__0#1}",
                "C.<>o__0#1: {<>p__0}",
                "C.<>o__0#2: {<>p__0}");

            diff2.VerifyIL("C.F", @"
{
  // Code size       84 (0x54)
  .maxstack  3
  .locals init (object V_0) //x
  IL_0000:  nop
  IL_0001:  ldc.i4.1
  IL_0002:  box        ""int""
  IL_0007:  stloc.0
  IL_0008:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>> C.<>o__0#2.<>p__0""
  IL_000d:  brfalse.s  IL_0011
  IL_000f:  br.s       IL_0036
  IL_0011:  ldc.i4.s   16
  IL_0013:  ldtoken    ""int""
  IL_0018:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001d:  ldtoken    ""C""
  IL_0022:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0027:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.Convert(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Type)""
  IL_002c:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0031:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>> C.<>o__0#2.<>p__0""
  IL_0036:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>> C.<>o__0#2.<>p__0""
  IL_003b:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>>.Target""
  IL_0040:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>> C.<>o__0#2.<>p__0""
  IL_0045:  ldloc.0
  IL_0046:  callvirt   ""int System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_004b:  ldc.i4.2
  IL_004c:  add
  IL_004d:  call       ""void System.Console.WriteLine(int)""
  IL_0052:  nop
  IL_0053:  ret
}
");
        }

        [Fact]
        public void ExceptionFilters()
        {
            var source0 = MarkedSource(@"
using System;
using System.IO;

class C
{
    static bool G(Exception e) => true;

    static void F()
    {
        try
        {
            throw new InvalidOperationException();
        }
        catch <N:0>(IOException e)</N:0> <N:1>when (G(e))</N:1>
        {
            Console.WriteLine();
        }
        catch <N:2>(Exception e)</N:2> <N:3>when (G(e))</N:3>
        {
            Console.WriteLine();
        }
    }
}
");
            var source1 = MarkedSource(@"
using System;
using System.IO;

class C
{
    static bool G(Exception e) => true;

    static void F()
    {
        try
        {
            throw new InvalidOperationException();
        }
        catch <N:0>(IOException e)</N:0> <N:1>when (G(e))</N:1>
        {
            Console.WriteLine();
        }
        catch <N:2>(Exception e)</N:2> <N:3>when (G(e))</N:3>
        {
            Console.WriteLine();
        }

        Console.WriteLine(1);
    }
}");
            var compilation0 = CreateCompilation(source0.Tree, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);
            var v0 = CompileAndVerify(compilation0);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            diff1.VerifyIL("C.F", @"
{
  // Code size       90 (0x5a)
  .maxstack  2
  .locals init (System.IO.IOException V_0, //e
                bool V_1,
                System.Exception V_2, //e
                bool V_3)
  IL_0000:  nop
  .try
  {
    IL_0001:  nop
    IL_0002:  newobj     ""System.InvalidOperationException..ctor()""
    IL_0007:  throw
  }
  filter
  {
    IL_0008:  isinst     ""System.IO.IOException""
    IL_000d:  dup
    IL_000e:  brtrue.s   IL_0014
    IL_0010:  pop
    IL_0011:  ldc.i4.0
    IL_0012:  br.s       IL_0020
    IL_0014:  stloc.0
    IL_0015:  ldloc.0
    IL_0016:  call       ""bool C.G(System.Exception)""
    IL_001b:  stloc.1
    IL_001c:  ldloc.1
    IL_001d:  ldc.i4.0
    IL_001e:  cgt.un
    IL_0020:  endfilter
  }  // end filter
  {  // handler
    IL_0022:  pop
    IL_0023:  nop
    IL_0024:  call       ""void System.Console.WriteLine()""
    IL_0029:  nop
    IL_002a:  nop
    IL_002b:  leave.s    IL_0052
  }
  filter
  {
    IL_002d:  isinst     ""System.Exception""
    IL_0032:  dup
    IL_0033:  brtrue.s   IL_0039
    IL_0035:  pop
    IL_0036:  ldc.i4.0
    IL_0037:  br.s       IL_0045
    IL_0039:  stloc.2
    IL_003a:  ldloc.2
    IL_003b:  call       ""bool C.G(System.Exception)""
    IL_0040:  stloc.3
    IL_0041:  ldloc.3
    IL_0042:  ldc.i4.0
    IL_0043:  cgt.un
    IL_0045:  endfilter
  }  // end filter
  {  // handler
    IL_0047:  pop
    IL_0048:  nop
    IL_0049:  call       ""void System.Console.WriteLine()""
    IL_004e:  nop
    IL_004f:  nop
    IL_0050:  leave.s    IL_0052
  }
  IL_0052:  ldc.i4.1
  IL_0053:  call       ""void System.Console.WriteLine(int)""
  IL_0058:  nop
  IL_0059:  ret
}
");
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.NoPiaNeedsDesktop)]
        public void MethodSignatureWithNoPIAType()
        {
            var sourcePIA = @"
using System;
using System.Runtime.InteropServices;
[assembly: ImportedFromTypeLib(""_.dll"")]
[assembly: Guid(""35DB1A6B-D635-4320-A062-28D42920E2A3"")]
[ComImport()]
[Guid(""35DB1A6B-D635-4320-A062-28D42920E2A4"")]
public interface I
{
}";
            var source0 = MarkedSource(@"
class C
{
    static void M(I x)
    {
        System.Console.WriteLine(1);
    }
}");
            var source1 = MarkedSource(@"
class C
{
    static void M(I x)
    {
        System.Console.WriteLine(2);
    }
}");
            var compilationPIA = CreateCompilation(sourcePIA, options: TestOptions.DebugDll);
            var referencePIA = compilationPIA.EmitToImageReference(embedInteropTypes: true);
            var compilation0 = CreateCompilation(source0.Tree, options: ComSafeDebugDll, references: new MetadataReference[] { referencePIA });
            var compilation1 = compilation0.WithSource(source1.Tree);

            var method0 = compilation0.GetMember<MethodSymbol>("C.M");
            var method1 = compilation1.GetMember<MethodSymbol>("C.M");

            var v0 = CompileAndVerify(compilation0);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);
            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            diff1.EmitResult.Diagnostics.Verify(
                // error CS7096: Cannot continue since the edit includes a reference to an embedded type: 'I'.
                Diagnostic(ErrorCode.ERR_EncNoPIAReference).WithArguments("I"));
        }

        [WorkItem(844472, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/844472")]
        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.NoPiaNeedsDesktop)]
        public void LocalSignatureWithNoPIAType()
        {
            var sourcePIA = @"
using System;
using System.Runtime.InteropServices;
[assembly: ImportedFromTypeLib(""_.dll"")]
[assembly: Guid(""35DB1A6B-D635-4320-A062-28D42920E2A3"")]
[ComImport()]
[Guid(""35DB1A6B-D635-4320-A062-28D42920E2A4"")]
public interface I
{
}";
            var source0 = MarkedSource(@"
class C
{
    static void M(I x)
    {
        I <N:0>y = null</N:0>;
        M(null);
    }
}");
            var source1 = MarkedSource(@"
class C
{
    static void M(I x)
    {
        I <N:0>y = null</N:0>;
        M(x);
    }
}");
            var compilationPIA = CreateCompilation(sourcePIA, options: TestOptions.DebugDll);
            var referencePIA = compilationPIA.EmitToImageReference(embedInteropTypes: true);
            var compilation0 = CreateCompilation(source0.Tree, options: ComSafeDebugDll, references: new MetadataReference[] { referencePIA });
            var compilation1 = compilation0.WithSource(source1.Tree);

            var method0 = compilation0.GetMember<MethodSymbol>("C.M");
            var method1 = compilation1.GetMember<MethodSymbol>("C.M");

            var v0 = CompileAndVerify(compilation0);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);
            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            diff1.EmitResult.Diagnostics.Verify(
                // (6,16): warning CS0219: The variable 'y' is assigned but its value is never used
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"),
                // error CS7096: Cannot continue since the edit includes a reference to an embedded type: 'I'.
                Diagnostic(ErrorCode.ERR_EncNoPIAReference).WithArguments("I"));
        }

        /// <summary>
        /// Disallow edits that require NoPIA references.
        /// </summary>
        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.NoPiaNeedsDesktop)]
        public void NoPIAReferences()
        {
            var sourcePIA =
@"using System;
using System.Runtime.InteropServices;
[assembly: ImportedFromTypeLib(""_.dll"")]
[assembly: Guid(""35DB1A6B-D635-4320-A062-28D42921E2B3"")]
[ComImport()]
[Guid(""35DB1A6B-D635-4320-A062-28D42921E2B4"")]
public interface IA
{
    void M();
    int P { get; }
    event Action E;
}
[ComImport()]
[Guid(""35DB1A6B-D635-4320-A062-28D42921E2B5"")]
public interface IB
{
}
[ComImport()]
[Guid(""35DB1A6B-D635-4320-A062-28D42921E2B6"")]
public interface IC
{
}
public struct S
{
    public object F;
}";
            var source0 =
@"class C<T>
{
    static object F = typeof(IC);
    static void M1()
    {
        var o = default(IA);
        o.M();
        M2(o.P);
        o.E += M1;
        M2(C<IA>.F);
        M2(new S());
    }
    static void M2(object o)
    {
    }
}";
            var source1A = source0;
            var source1B =
@"class C<T>
{
    static object F = typeof(IC);
    static void M1()
    {
        M2(null);
    }
    static void M2(object o)
    {
    }
}";
            var compilationPIA = CreateCompilation(sourcePIA, options: TestOptions.DebugDll);
            var referencePIA = compilationPIA.EmitToImageReference(embedInteropTypes: true);
            var compilation0 = CreateCompilation(source0, options: TestOptions.DebugDll, references: new MetadataReference[] { referencePIA, CSharpRef });
            var compilation1A = compilation0.WithSource(source1A);
            var compilation1B = compilation0.WithSource(source1B);

            var method0 = compilation0.GetMember<MethodSymbol>("C.M1");
            var method1B = compilation1B.GetMember<MethodSymbol>("C.M1");
            var method1A = compilation1A.GetMember<MethodSymbol>("C.M1");

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(testData: testData0);
            var methodData0 = testData0.GetMethodData("C<T>.M1");
            using var md0 = ModuleMetadata.CreateFromImage(bytes0);
            var reader0 = md0.MetadataReader;

            CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "C`1", "IA", "IC", "S");

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, methodData0.EncDebugInfoProvider());

            // Disallow edits that require NoPIA references.
            var diff1A = compilation1A.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1A, GetEquivalentNodesMap(method1A, method0), preserveLocalVariables: true)));

            diff1A.EmitResult.Diagnostics.Verify(
                // error CS7094: Cannot continue since the edit includes a reference to an embedded type: 'S'.
                Diagnostic(ErrorCode.ERR_EncNoPIAReference).WithArguments("S"),
                // error CS7094: Cannot continue since the edit includes a reference to an embedded type: 'IA'.
                Diagnostic(ErrorCode.ERR_EncNoPIAReference).WithArguments("IA"));

            // Allow edits that do not require NoPIA references,
            // even if the previous code included references.
            var diff1B = compilation1B.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1B, GetEquivalentNodesMap(method1B, method0), preserveLocalVariables: true)));

            diff1B.VerifyIL("C<T>.M1",
@"{
  // Code size        9 (0x9)
  .maxstack  1
  .locals init ([unchanged] V_0,
  [unchanged] V_1)
  IL_0000:  nop
  IL_0001:  ldnull
  IL_0002:  call       ""void C<T>.M2(object)""
  IL_0007:  nop
  IL_0008:  ret
}");

            using var md1 = diff1B.GetMetadata();
            var reader1 = md1.Reader;
            var readers = new[] { reader0, reader1 };
            CheckNames(readers, reader1.GetTypeDefNames());
        }

        [WorkItem(844536, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/844536")]
        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.NoPiaNeedsDesktop)]
        public void NoPIATypeInNamespace()
        {
            var sourcePIA =
@"using System;
using System.Runtime.InteropServices;
[assembly: ImportedFromTypeLib(""_.dll"")]
[assembly: Guid(""35DB1A6B-D635-4320-A062-28D42920E2A5"")]
namespace N
{
    [ComImport()]
    [Guid(""35DB1A6B-D635-4320-A062-28D42920E2A6"")]
    public interface IA
    {
    }
}
[ComImport()]
[Guid(""35DB1A6B-D635-4320-A062-28D42920E2A7"")]
public interface IB
{
}";
            var source =
@"class C<T>
{
    static void M(object o)
    {
        M(C<N.IA>.E.X);
        M(C<IB>.E.X);
    }
    enum E { X }
}";
            var compilationPIA = CreateCompilation(sourcePIA, options: TestOptions.DebugDll);
            var referencePIA = compilationPIA.EmitToImageReference(embedInteropTypes: true);
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll, references: new MetadataReference[] { referencePIA, CSharpRef });
            var compilation1 = compilation0.WithSource(source);

            var bytes0 = compilation0.EmitToArray();
            using var md0 = ModuleMetadata.CreateFromImage(bytes0);
            var generation0 = EmitBaseline.CreateInitialBaseline(md0, m => default);
            var method0 = compilation0.GetMember<MethodSymbol>("C.M");
            var method1 = compilation1.GetMember<MethodSymbol>("C.M");

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1)));

            diff1.EmitResult.Diagnostics.Verify(
                // error CS7094: Cannot continue since the edit includes a reference to an embedded type: 'N.IA'.
                Diagnostic(ErrorCode.ERR_EncNoPIAReference).WithArguments("N.IA"),
                // error CS7094: Cannot continue since the edit includes a reference to an embedded type: 'IB'.
                Diagnostic(ErrorCode.ERR_EncNoPIAReference).WithArguments("IB"));

            diff1.VerifyIL("C<T>.M",
@"{
  // Code size       26 (0x1a)
  .maxstack  1
  IL_0000:  nop
  IL_0001:  ldc.i4.0
  IL_0002:  box        ""C<N.IA>.E""
  IL_0007:  call       ""void C<T>.M(object)""
  IL_000c:  nop
  IL_000d:  ldc.i4.0
  IL_000e:  box        ""C<IB>.E""
  IL_0013:  call       ""void C<T>.M(object)""
  IL_0018:  nop
  IL_0019:  ret
}");
        }

        /// <summary>
        /// Should use TypeDef rather than TypeRef for unrecognized
        /// local of a type defined in the original assembly.
        /// </summary>
        [WorkItem(910777, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/910777")]
        [Fact]
        public void UnrecognizedLocalOfTypeFromAssembly()
        {
            var source =
@"class E : System.Exception
{
}
class C
{
    static void M()
    {
        try
        {
        }
        catch (E e)
        {
        }
    }
}";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugDll, targetFramework: TargetFramework.NetStandard20);
            var compilation1 = compilation0.WithSource(source);
            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(testData: testData0);
            var methodData0 = testData0.GetMethodData("C.M");

            using var md0 = ModuleMetadata.CreateFromImage(bytes0);
            var reader0 = md0.MetadataReader;
            CheckNames(reader0, reader0.GetAssemblyRefNames(), "netstandard");
            var method0 = compilation0.GetMember<MethodSymbol>("C.M");
            var method1 = compilation1.GetMember<MethodSymbol>("C.M");

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, methodData0.EncDebugInfoProvider());

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables: true)));

            using var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;
            var readers = new[] { reader0, reader1 };

            CheckNames(readers, reader1.GetAssemblyRefNames(), "netstandard");
            CheckNames(readers, reader1.GetTypeRefNames(), "Object");

            CheckEncLog(reader1,
                Row(2, TableIndex.AssemblyRef, EditAndContinueOperation.Default),
                Row(7, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(2, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default));

            CheckEncMap(reader1,
                Handle(7, TableIndex.TypeRef),
                Handle(2, TableIndex.MethodDef),
                Handle(2, TableIndex.StandAloneSig),
                Handle(2, TableIndex.AssemblyRef));

            diff1.VerifyIL("C.M", @"
{
  // Code size       11 (0xb)
  .maxstack  1
  .locals init (E V_0) //e
  IL_0000:  nop
  .try
  {
    IL_0001:  nop
    IL_0002:  nop
    IL_0003:  leave.s    IL_000a
  }
  catch E
  {
    IL_0005:  stloc.0
    IL_0006:  nop
    IL_0007:  nop
    IL_0008:  leave.s    IL_000a
  }
  IL_000a:  ret
}");
        }

        /// <summary>
        /// Similar to above test but with anonymous type
        /// added in subsequent generation.
        /// </summary>
        [WorkItem(910777, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/910777")]
        [Fact]
        public void UnrecognizedLocalOfAnonymousTypeFromAssembly()
        {
            var source0 =
@"class C
{
    static string F()
    {
        return null;
    }
    static string G()
    {
        var o = new { Y = 1 };
        return o.ToString();
    }
}";
            var source1 =
@"class C
{
    static string F()
    {
        var o = new { X = 1 };
        return o.ToString();
    }
    static string G()
    {
        var o = new { Y = 1 };
        return o.ToString();
    }
}";
            var source2 =
@"class C
{
    static string F()
    {
        return null;
    }
    static string G()
    {
        return null;
    }
}";
            var compilation0 = CreateCompilation(source0, options: TestOptions.DebugDll, targetFramework: TargetFramework.NetStandard20);
            var compilation1 = compilation0.WithSource(source1);
            var compilation2 = compilation1.WithSource(source2);

            var method0F = compilation0.GetMember<MethodSymbol>("C.F");
            var method1F = compilation1.GetMember<MethodSymbol>("C.F");
            var method1G = compilation1.GetMember<MethodSymbol>("C.G");
            var method2F = compilation2.GetMember<MethodSymbol>("C.F");
            var method2G = compilation2.GetMember<MethodSymbol>("C.G");

            var bytes0 = compilation0.EmitToArray();
            using var md0 = ModuleMetadata.CreateFromImage(bytes0);
            var reader0 = md0.MetadataReader;
            CheckNames(reader0, reader0.GetAssemblyRefNames(), "netstandard");

            // Use empty LocalVariableNameProvider for original locals and
            // use preserveLocalVariables: true for the edit so that existing
            // locals are retained even though all are unrecognized.
            var generation0 = EmitBaseline.CreateInitialBaseline(
                md0,
                EmptyLocalsProvider);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0F, method1F, syntaxMap: s => null, preserveLocalVariables: true)));

            using var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;
            var readers = new List<MetadataReader> { reader0, reader1 };

            CheckNames(readers, reader1.GetAssemblyRefNames(), "netstandard");
            CheckNames(readers, reader1.GetTypeDefNames(), "<>f__AnonymousType1`1");
            CheckNames(readers, reader1.GetTypeRefNames(), "CompilerGeneratedAttribute", "DebuggerDisplayAttribute", "Object", "DebuggerBrowsableState", "DebuggerBrowsableAttribute", "DebuggerHiddenAttribute", "EqualityComparer`1", "String", "IFormatProvider");

            // Change method updated in generation 1.
            var diff2F = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method1F, method2F, syntaxMap: s => null, preserveLocalVariables: true)));

            using var md2 = diff2F.GetMetadata();
            var reader2 = md2.Reader;
            readers.Add(reader2);
            CheckNames(readers, reader2.GetAssemblyRefNames(), "netstandard");
            CheckNames(readers, reader2.GetTypeDefNames());
            CheckNames(readers, reader2.GetTypeRefNames(), "Object");

            // Change method unchanged since generation 0.
            var diff2G = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method1G, method2G, syntaxMap: s => null, preserveLocalVariables: true)));
        }

        [Fact]
        public void BrokenOutputStreams()
        {
            var source0 =
@"class C
{
    static string F()
    {
        return null;
    }
}";
            var source1 =
@"class C
{
    static string F()
    {
        var o = new { X = 1 };
        return o.ToString();
    }
}";
            var compilation0 = CreateCompilation(source0, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);
            var bytes0 = compilation0.EmitToArray();
            using (new EnsureEnglishUICulture())
            using (var md0 = ModuleMetadata.CreateFromImage(bytes0))
            {
                var method0F = compilation0.GetMember<MethodSymbol>("C.F");
                var generation0 = EmitBaseline.CreateInitialBaseline(
                    md0,
                    EmptyLocalsProvider);
                var method1F = compilation1.GetMember<MethodSymbol>("C.F");

                using MemoryStream mdStream = new MemoryStream(), ilStream = new MemoryStream(), pdbStream = new MemoryStream();
                var updatedMethods = new List<MethodDefinitionHandle>();
                var isAddedSymbol = new Func<ISymbol, bool>(s => false);

                var badStream = new BrokenStream();
                badStream.BreakHow = BrokenStream.BreakHowType.ThrowOnWrite;

                var result = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0F, method1F, syntaxMap: s => null, preserveLocalVariables: true)),
                    isAddedSymbol,
                    badStream,
                    ilStream,
                    pdbStream,
                    updatedMethods,
                    new CompilationTestData(),
                    default);
                Assert.False(result.Success);
                result.Diagnostics.Verify(
                    // error CS8104: An error occurred while writing the output file: System.IO.IOException: I/O error occurred.
                    Diagnostic(ErrorCode.ERR_PeWritingFailure).WithArguments(badStream.ThrownException.ToString()).WithLocation(1, 1)
                    );

                result = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0F, method1F, syntaxMap: s => null, preserveLocalVariables: true)),
                    isAddedSymbol,
                    mdStream,
                    badStream,
                    pdbStream,
                    updatedMethods,
                    new CompilationTestData(),
                    default);
                Assert.False(result.Success);
                result.Diagnostics.Verify(
                    // error CS8104: An error occurred while writing the output file: System.IO.IOException: I/O error occurred.
                    Diagnostic(ErrorCode.ERR_PeWritingFailure).WithArguments(badStream.ThrownException.ToString()).WithLocation(1, 1)
                    );

                result = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0F, method1F, syntaxMap: s => null, preserveLocalVariables: true)),
                    isAddedSymbol,
                    mdStream,
                    ilStream,
                    badStream,
                    updatedMethods,
                    new CompilationTestData(),
                    default);
                Assert.False(result.Success);
                result.Diagnostics.Verify(
                    // error CS0041: Unexpected error writing debug information -- 'I/O error occurred.'
                    Diagnostic(ErrorCode.FTL_DebugEmitFailure).WithArguments("I/O error occurred.").WithLocation(1, 1)
                    );
            }
        }

        [Fact]
        public void BrokenPortablePdbStream()
        {
            var source0 =
@"class C
{
    static string F()
    {
        return null;
    }
}";
            var source1 =
@"class C
{
    static string F()
    {
        var o = new { X = 1 };
        return o.ToString();
    }
}";
            var compilation0 = CreateCompilation(source0, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);
            var bytes0 = compilation0.EmitToArray(EmitOptions.Default.WithDebugInformationFormat(DebugInformationFormat.PortablePdb));
            using (new EnsureEnglishUICulture())
            using (var md0 = ModuleMetadata.CreateFromImage(bytes0))
            {
                var method0F = compilation0.GetMember<MethodSymbol>("C.F");
                var generation0 = EmitBaseline.CreateInitialBaseline(
                    md0,
                    EmptyLocalsProvider);
                var method1F = compilation1.GetMember<MethodSymbol>("C.F");

                using MemoryStream mdStream = new MemoryStream(), ilStream = new MemoryStream(), pdbStream = new MemoryStream();
                var updatedMethods = new List<MethodDefinitionHandle>();
                var isAddedSymbol = new Func<ISymbol, bool>(s => false);

                var badStream = new BrokenStream();
                badStream.BreakHow = BrokenStream.BreakHowType.ThrowOnWrite;

                var result = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0F, method1F, syntaxMap: s => null, preserveLocalVariables: true)),
                    isAddedSymbol,
                    mdStream,
                    ilStream,
                    badStream,
                    updatedMethods,
                    new CompilationTestData(),
                    default);
                Assert.False(result.Success);
                result.Diagnostics.Verify(
                    // error CS0041: Unexpected error writing debug information -- 'I/O error occurred.'
                    Diagnostic(ErrorCode.FTL_DebugEmitFailure).WithArguments("I/O error occurred.").WithLocation(1, 1)
                    );
            }
        }

        [WorkItem(923492, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/923492")]
        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
        public void SymWriterErrors()
        {
            var source0 =
@"class C
{
}";
            var source1 =
@"class C
{
    static void Main() { }
}";
            var compilation0 = CreateCompilation(source0, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);

            // Verify full metadata contains expected rows.
            var bytes0 = compilation0.EmitToArray();
            using var md0 = ModuleMetadata.CreateFromImage(bytes0);
            var diff1 = compilation1.EmitDifference(
EmitBaseline.CreateInitialBaseline(md0, EmptyLocalsProvider),
ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Insert, null, compilation1.GetMember<MethodSymbol>("C.Main"))),
testData: new CompilationTestData { SymWriterFactory = _ => new MockSymUnmanagedWriter() });

            diff1.EmitResult.Diagnostics.Verify(
                // error CS0041: Unexpected error writing debug information -- 'MockSymUnmanagedWriter error message'
                Diagnostic(ErrorCode.FTL_DebugEmitFailure).WithArguments("MockSymUnmanagedWriter error message"));

            Assert.False(diff1.EmitResult.Success);
        }

        [WorkItem(1058058, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1058058")]
        [Fact]
        public void BlobContainsInvalidValues()
        {
            var source0 =
@"class C
{
    static void F()
    {
        string goo = ""abc"";
    }
}";
            var source1 =
@"class C
{
    static void F()
    {
        float goo = 10;
    }
}";
            var source2 =
@"class C
{
    static void F()
    {
        bool goo = true;
    }
}";
            var compilation0 = CreateCompilation(source0, options: TestOptions.DebugDll, targetFramework: TargetFramework.NetStandard20);
            var compilation1 = compilation0.WithSource(source1);
            var compilation2 = compilation1.WithSource(source2);
            var bytes0 = compilation0.EmitToArray();

            using var md0 = ModuleMetadata.CreateFromImage(bytes0);
            var reader0 = md0.MetadataReader;
            CheckNames(reader0, reader0.GetAssemblyRefNames(), "netstandard");
            var method0F = compilation0.GetMember<MethodSymbol>("C.F");
            var generation0 = EmitBaseline.CreateInitialBaseline(md0, EmptyLocalsProvider);
            var method1F = compilation1.GetMember<MethodSymbol>("C.F");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0F, method1F, syntaxMap: s => null, preserveLocalVariables: true)));


            var handle = MetadataTokens.BlobHandle(1);
            byte[] value0 = reader0.GetBlobBytes(handle);
            Assert.Equal("20-01-01-08", BitConverter.ToString(value0));

            using var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;
            var method2F = compilation2.GetMember<MethodSymbol>("C.F");
            var diff2F = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method1F, method2F, syntaxMap: s => null, preserveLocalVariables: true)));

            byte[] value1 = reader1.GetBlobBytes(handle);
            Assert.Equal("07-02-0E-0C", BitConverter.ToString(value1));

            using var md2 = diff2F.GetMetadata();
            var reader2 = md2.Reader;
            byte[] value2 = reader2.GetBlobBytes(handle);
            Assert.Equal("07-03-0E-0C-02", BitConverter.ToString(value2));
        }

        [Fact]
        public void ReferenceToMemberAddedToAnotherAssembly1()
        {
            var sourceA0 = @"
public class A
{
}
";
            var sourceA1 = @"
public class A
{
    public void M() { System.Console.WriteLine(1);}
}

public class X {} 
";
            var sourceB0 = @"
public class B
{
    public static void F() { }
}";
            var sourceB1 = @"
public class B
{
    public static void F() { new A().M(); }
}

public class Y : X { }
";

            var compilationA0 = CreateCompilation(sourceA0, options: TestOptions.DebugDll, assemblyName: "LibA");
            var compilationA1 = compilationA0.WithSource(sourceA1);
            var compilationB0 = CreateCompilation(sourceB0, new[] { compilationA0.ToMetadataReference() }, options: TestOptions.DebugDll, assemblyName: "LibB");
            var compilationB1 = CreateCompilation(sourceB1, new[] { compilationA1.ToMetadataReference() }, options: TestOptions.DebugDll, assemblyName: "LibB");

            var bytesA0 = compilationA0.EmitToArray();
            var bytesB0 = compilationB0.EmitToArray();
            var mdA0 = ModuleMetadata.CreateFromImage(bytesA0);
            var mdB0 = ModuleMetadata.CreateFromImage(bytesB0);
            var generationA0 = EmitBaseline.CreateInitialBaseline(mdA0, EmptyLocalsProvider);
            var generationB0 = EmitBaseline.CreateInitialBaseline(mdB0, EmptyLocalsProvider);
            var mA1 = compilationA1.GetMember<MethodSymbol>("A.M");
            var mX1 = compilationA1.GetMember<TypeSymbol>("X");

            var allAddedSymbols = new ISymbol[] { mA1, mX1 };

            var diffA1 = compilationA1.EmitDifference(
                generationA0,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Insert, null, mA1),
                    new SemanticEdit(SemanticEditKind.Insert, null, mX1)),
                allAddedSymbols);

            diffA1.EmitResult.Diagnostics.Verify();

            var diffB1 = compilationB1.EmitDifference(
                generationB0,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, compilationB0.GetMember<MethodSymbol>("B.F"), compilationB1.GetMember<MethodSymbol>("B.F")),
                    new SemanticEdit(SemanticEditKind.Insert, null, compilationB1.GetMember<TypeSymbol>("Y"))),
                allAddedSymbols);

            diffB1.EmitResult.Diagnostics.Verify(
                // (7,14): error CS7101: Member 'X' added during the current debug session can only be accessed from within its declaring assembly 'LibA'.
                // public class X {} 
                Diagnostic(ErrorCode.ERR_EncReferenceToAddedMember, "X").WithArguments("X", "LibA").WithLocation(7, 14),
                // (4,17): error CS7101: Member 'M' added during the current debug session can only be accessed from within its declaring assembly 'LibA'.
                //     public void M() { System.Console.WriteLine(1);}
                Diagnostic(ErrorCode.ERR_EncReferenceToAddedMember, "M").WithArguments("M", "LibA").WithLocation(4, 17));
        }

        [Fact]
        public void ReferenceToMemberAddedToAnotherAssembly2()
        {
            var sourceA = @"
public class A
{
    public void M() { }
}";
            var sourceB0 = @"
public class B
{
    public static void F() { var a = new A(); }
}";
            var sourceB1 = @"
public class B
{
    public static void F() { var a = new A(); a.M(); }
}";
            var sourceB2 = @"
public class B
{
    public static void F() { var a = new A(); }
}";

            var compilationA = CreateCompilation(sourceA, options: TestOptions.DebugDll, assemblyName: "AssemblyA");
            var aRef = compilationA.ToMetadataReference();

            var compilationB0 = CreateCompilation(sourceB0, new[] { aRef }, options: TestOptions.DebugDll, assemblyName: "AssemblyB");
            var compilationB1 = compilationB0.WithSource(sourceB1);
            var compilationB2 = compilationB1.WithSource(sourceB2);

            var testDataB0 = new CompilationTestData();
            var bytesB0 = compilationB0.EmitToArray(testData: testDataB0);
            var mdB0 = ModuleMetadata.CreateFromImage(bytesB0);
            var generationB0 = EmitBaseline.CreateInitialBaseline(mdB0, testDataB0.GetMethodData("B.F").EncDebugInfoProvider());

            var f0 = compilationB0.GetMember<MethodSymbol>("B.F");
            var f1 = compilationB1.GetMember<MethodSymbol>("B.F");
            var f2 = compilationB2.GetMember<MethodSymbol>("B.F");

            var diffB1 = compilationB1.EmitDifference(
                generationB0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, f0, f1, GetEquivalentNodesMap(f1, f0), preserveLocalVariables: true)));

            diffB1.VerifyIL("B.F", @"
{
  // Code size       15 (0xf)
  .maxstack  1
  .locals init (A V_0) //a
  IL_0000:  nop
  IL_0001:  newobj     ""A..ctor()""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  callvirt   ""void A.M()""
  IL_000d:  nop
  IL_000e:  ret
}
");

            var diffB2 = compilationB2.EmitDifference(
               diffB1.NextGeneration,
               ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, f1, f2, GetEquivalentNodesMap(f2, f1), preserveLocalVariables: true)));

            diffB2.VerifyIL("B.F", @"
{
  // Code size        8 (0x8)
  .maxstack  1
  .locals init (A V_0) //a
  IL_0000:  nop
  IL_0001:  newobj     ""A..ctor()""
  IL_0006:  stloc.0
  IL_0007:  ret
}
");
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.TestExecutionNeedsDesktopTypes)]
        public void UniqueSynthesizedNames_DynamicSiteContainer()
        {
            var source0 = @"
public class C
{    
    public static void F(dynamic d) { d.Goo(); }
}";
            var source1 = @"
public class C
{
    public static void F(dynamic d) { d.Bar(); }
}";
            var source2 = @"
public class C
{
    public static void F(dynamic d, byte b) { d.Bar(); }
    public static void F(dynamic d) { d.Bar(); }
}";

            var compilation0 = CreateCompilationWithMscorlib45(source0, new[] { SystemCoreRef, CSharpRef }, options: ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All), assemblyName: "A");
            var compilation1 = compilation0.WithSource(source1);
            var compilation2 = compilation1.WithSource(source2);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");
            var f_byte2 = compilation2.GetMembers("C.F").Single(m => m.ToString() == "C.F(dynamic, byte)");

            var v0 = CompileAndVerify(compilation0);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, f0, f1, preserveLocalVariables: true)));

            diff1.EmitResult.Diagnostics.Verify();

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Insert, null, f_byte2)));

            diff2.EmitResult.Diagnostics.Verify();

            var reader0 = md0.MetadataReader;
            var reader1 = diff1.GetMetadata().Reader;
            var reader2 = diff2.GetMetadata().Reader;

            CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "C", "<>o__0");
            CheckNames(new[] { reader0, reader1 }, reader1.GetTypeDefNames(), "<>o__0#1");
            CheckNames(new[] { reader0, reader1, reader2 }, reader2.GetTypeDefNames(), "<>o__0#2");
        }

        [WorkItem(918650, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/918650")]
        [Fact]
        public void ManyGenerations()
        {
            var source =
@"class C
{{
    static int F() {{ return {0}; }}
}}";

            var compilation0 = CreateCompilation(String.Format(source, 1), options: TestOptions.DebugDll);

            var bytes0 = compilation0.EmitToArray();
            var md0 = ModuleMetadata.CreateFromImage(bytes0);
            var generation0 = EmitBaseline.CreateInitialBaseline(md0, EmptyLocalsProvider);
            var method0 = compilation0.GetMember<MethodSymbol>("C.F");

            for (int i = 2; i <= 50; i++)
            {
                var compilation1 = compilation0.WithSource(String.Format(source, i));
                var method1 = compilation1.GetMember<MethodSymbol>("C.F");
                var diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1)));

                compilation0 = compilation1;
                method0 = method1;
                generation0 = diff1.NextGeneration;
            }
        }

        [WorkItem(187868, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/187868")]
        [Fact]
        public void PdbReadingErrors()
        {
            var source0 = MarkedSource(@"
using System;

class C
{
	static void F() 
    {
        <N:0>Console.WriteLine(1);</N:0>
    }
}");

            var source1 = MarkedSource(@"
using System;

class C
{
	static void F() 
    {
        <N:0>Console.WriteLine(2);</N:0>
    }
}");
            var compilation0 = CreateCompilation(source0.Tree, options: TestOptions.DebugDll, assemblyName: "PdbReadingErrorsAssembly");
            var compilation1 = compilation0.WithSource(source1.Tree);

            var v0 = CompileAndVerify(compilation0);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, methodHandle =>
            {
                throw new InvalidDataException("Bad PDB!");
            });

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            diff1.EmitResult.Diagnostics.Verify(
                // (6,14): error CS7038: Failed to emit module 'Unable to read debug information of method 'C.F()' (token 0x06000001) from assembly 'PdbReadingErrorsAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null''.
                Diagnostic(ErrorCode.ERR_InvalidDebugInfo, "F").WithArguments("C.F()", "100663297", "PdbReadingErrorsAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(6, 14));
        }

        [Fact]
        public void PdbReadingErrors_PassThruExceptions()
        {
            var source0 = MarkedSource(@"
using System;

class C
{
	static void F() 
    {
        <N:0>Console.WriteLine(1);</N:0>
    }
}");

            var source1 = MarkedSource(@"
using System;

class C
{
	static void F() 
    {
        <N:0>Console.WriteLine(2);</N:0>
    }
}");
            var compilation0 = CreateCompilation(source0.Tree, options: TestOptions.DebugDll, assemblyName: "PdbReadingErrorsAssembly");
            var compilation1 = compilation0.WithSource(source1.Tree);

            var v0 = CompileAndVerify(compilation0);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, methodHandle =>
            {
                throw new ArgumentOutOfRangeException();
            });

            // the compiler should't swallow any exceptions but InvalidDataException
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true))));
        }

        [Fact]
        public void PatternVariable_TypeChange()
        {
            var source0 = MarkedSource(@"
class C
{
    static int F(object o) { if (o is int <N:0>i</N:0>) { return i; } return 0; }
}");
            var source1 = MarkedSource(@"
class C
{
    static int F(object o) { if (o is bool <N:0>i</N:0>) { return i ? 1 : 0; } return 0; }
}");

            var source2 = MarkedSource(@"
class C
{
    static int F(object o) { if (o is int <N:0>j</N:0>) { return j; } return 0; }
}");

            var compilation0 = CreateCompilation(source0.Tree, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);
            var compilation2 = compilation1.WithSource(source2.Tree);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");
            var f2 = compilation2.GetMember<MethodSymbol>("C.F");

            var v0 = CompileAndVerify(compilation0);
            v0.VerifyIL("C.F", @"
{
  // Code size       35 (0x23)
  .maxstack  1
  .locals init (int V_0, //i
                bool V_1,
                int V_2)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  isinst     ""int""
  IL_0007:  brfalse.s  IL_0013
  IL_0009:  ldarg.0
  IL_000a:  unbox.any  ""int""
  IL_000f:  stloc.0
  IL_0010:  ldc.i4.1
  IL_0011:  br.s       IL_0014
  IL_0013:  ldc.i4.0
  IL_0014:  stloc.1
  IL_0015:  ldloc.1
  IL_0016:  brfalse.s  IL_001d
  IL_0018:  nop
  IL_0019:  ldloc.0
  IL_001a:  stloc.2
  IL_001b:  br.s       IL_0021
  IL_001d:  ldc.i4.0
  IL_001e:  stloc.2
  IL_001f:  br.s       IL_0021
  IL_0021:  ldloc.2
  IL_0022:  ret
}");

            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            diff1.VerifyIL("C.F", @"
{
  // Code size       46 (0x2e)
  .maxstack  1
  .locals init ([int] V_0,
                [bool] V_1,
                [int] V_2,
                bool V_3, //i
                bool V_4,
                int V_5)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  isinst     ""bool""
  IL_0007:  brfalse.s  IL_0013
  IL_0009:  ldarg.0
  IL_000a:  unbox.any  ""bool""
  IL_000f:  stloc.3
  IL_0010:  ldc.i4.1
  IL_0011:  br.s       IL_0014
  IL_0013:  ldc.i4.0
  IL_0014:  stloc.s    V_4
  IL_0016:  ldloc.s    V_4
  IL_0018:  brfalse.s  IL_0026
  IL_001a:  nop
  IL_001b:  ldloc.3
  IL_001c:  brtrue.s   IL_0021
  IL_001e:  ldc.i4.0
  IL_001f:  br.s       IL_0022
  IL_0021:  ldc.i4.1
  IL_0022:  stloc.s    V_5
  IL_0024:  br.s       IL_002b
  IL_0026:  ldc.i4.0
  IL_0027:  stloc.s    V_5
  IL_0029:  br.s       IL_002b
  IL_002b:  ldloc.s    V_5
  IL_002d:  ret
}");

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, f1, f2, GetSyntaxMapFromMarkers(source1, source2), preserveLocalVariables: true)));

            diff2.VerifyIL("C.F", @"
{
  // Code size       42 (0x2a)
  .maxstack  1
  .locals init ([int] V_0,
                [bool] V_1,
                [int] V_2,
                [bool] V_3,
                [bool] V_4,
                [int] V_5,
                int V_6, //j
                bool V_7,
                int V_8)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  isinst     ""int""
  IL_0007:  brfalse.s  IL_0014
  IL_0009:  ldarg.0
  IL_000a:  unbox.any  ""int""
  IL_000f:  stloc.s    V_6
  IL_0011:  ldc.i4.1
  IL_0012:  br.s       IL_0015
  IL_0014:  ldc.i4.0
  IL_0015:  stloc.s    V_7
  IL_0017:  ldloc.s    V_7
  IL_0019:  brfalse.s  IL_0022
  IL_001b:  nop
  IL_001c:  ldloc.s    V_6
  IL_001e:  stloc.s    V_8
  IL_0020:  br.s       IL_0027
  IL_0022:  ldc.i4.0
  IL_0023:  stloc.s    V_8
  IL_0025:  br.s       IL_0027
  IL_0027:  ldloc.s    V_8
  IL_0029:  ret
}");
        }

        [Fact]
        public void PatternVariable_DeleteInsert()
        {
            var source0 = MarkedSource(@"
class C
{
    static int F(object o) { if (o is int <N:0>i</N:0>) { return i; } return 0; }
}");
            var source1 = MarkedSource(@"
class C
{
    static int F(object o) { if (o is int) { return 1; } return 0; }
}");

            var source2 = MarkedSource(@"
class C
{
    static int F(object o) { if (o is int <N:0>i</N:0>) { return i; } return 0; }
}");

            var compilation0 = CreateCompilation(source0.Tree, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);
            var compilation2 = compilation1.WithSource(source2.Tree);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");
            var f2 = compilation2.GetMember<MethodSymbol>("C.F");

            var v0 = CompileAndVerify(compilation0);
            v0.VerifyIL("C.F", @"
{
  // Code size       35 (0x23)
  .maxstack  1
  .locals init (int V_0, //i
                bool V_1,
                int V_2)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  isinst     ""int""
  IL_0007:  brfalse.s  IL_0013
  IL_0009:  ldarg.0
  IL_000a:  unbox.any  ""int""
  IL_000f:  stloc.0
  IL_0010:  ldc.i4.1
  IL_0011:  br.s       IL_0014
  IL_0013:  ldc.i4.0
  IL_0014:  stloc.1
  IL_0015:  ldloc.1
  IL_0016:  brfalse.s  IL_001d
  IL_0018:  nop
  IL_0019:  ldloc.0
  IL_001a:  stloc.2
  IL_001b:  br.s       IL_0021
  IL_001d:  ldc.i4.0
  IL_001e:  stloc.2
  IL_001f:  br.s       IL_0021
  IL_0021:  ldloc.2
  IL_0022:  ret
}");

            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            diff1.VerifyIL("C.F", @"
{
  // Code size       28 (0x1c)
  .maxstack  2
  .locals init ([int] V_0,
                [bool] V_1,
                [int] V_2,
                bool V_3,
                int V_4)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  isinst     ""int""
  IL_0007:  ldnull
  IL_0008:  cgt.un
  IL_000a:  stloc.3
  IL_000b:  ldloc.3
  IL_000c:  brfalse.s  IL_0014
  IL_000e:  nop
  IL_000f:  ldc.i4.1
  IL_0010:  stloc.s    V_4
  IL_0012:  br.s       IL_0019
  IL_0014:  ldc.i4.0
  IL_0015:  stloc.s    V_4
  IL_0017:  br.s       IL_0019
  IL_0019:  ldloc.s    V_4
  IL_001b:  ret
}");

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, f1, f2, GetSyntaxMapFromMarkers(source1, source2), preserveLocalVariables: true)));

            diff2.VerifyIL("C.F", @"
{
  // Code size       42 (0x2a)
  .maxstack  1
  .locals init ([int] V_0,
                [bool] V_1,
                [int] V_2,
                [bool] V_3,
                [int] V_4,
                int V_5, //i
                bool V_6,
                int V_7)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  isinst     ""int""
  IL_0007:  brfalse.s  IL_0014
  IL_0009:  ldarg.0
  IL_000a:  unbox.any  ""int""
  IL_000f:  stloc.s    V_5
  IL_0011:  ldc.i4.1
  IL_0012:  br.s       IL_0015
  IL_0014:  ldc.i4.0
  IL_0015:  stloc.s    V_6
  IL_0017:  ldloc.s    V_6
  IL_0019:  brfalse.s  IL_0022
  IL_001b:  nop
  IL_001c:  ldloc.s    V_5
  IL_001e:  stloc.s    V_7
  IL_0020:  br.s       IL_0027
  IL_0022:  ldc.i4.0
  IL_0023:  stloc.s    V_7
  IL_0025:  br.s       IL_0027
  IL_0027:  ldloc.s    V_7
  IL_0029:  ret
}");
        }

        [Fact]
        public void PatternVariable_InConstructorInitializer()
        {
            var baseClass = "public class Base { public Base(bool x) { } }";

            var source0 = MarkedSource(@"
public class C : Base
{
    public C(int a) : base(a is int <N:0>x</N:0> && x == 0 && a is int <N:1>y</N:1>) { y = 1; }
}" + baseClass);
            var source1 = MarkedSource(@"
public class C : Base
{
    public C(int a) : base(a is int <N:0>x</N:0> && x == 0) { }
}" + baseClass);

            var compilation0 = CreateCompilation(source0.Tree, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);
            var compilation2 = compilation1.WithSource(source0.Tree);

            var ctor0 = compilation0.GetMember<MethodSymbol>("C..ctor");
            var ctor1 = compilation1.GetMember<MethodSymbol>("C..ctor");
            var ctor2 = compilation2.GetMember<MethodSymbol>("C..ctor");

            var v0 = CompileAndVerify(compilation0);
            v0.VerifyIL("C..ctor", @"
{
  // Code size       22 (0x16)
  .maxstack  2
  .locals init (int V_0, //x
                int V_1) //y
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  stloc.0
  IL_0003:  ldloc.0
  IL_0004:  brtrue.s   IL_000b
  IL_0006:  ldarg.1
  IL_0007:  stloc.1
  IL_0008:  ldc.i4.1
  IL_0009:  br.s       IL_000c
  IL_000b:  ldc.i4.0
  IL_000c:  call       ""Base..ctor(bool)""
  IL_0011:  nop
  IL_0012:  nop
  IL_0013:  ldc.i4.1
  IL_0014:  stloc.1
  IL_0015:  ret
}
");

            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, ctor0, ctor1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            diff1.VerifyIL("C..ctor", @"
{
  // Code size       15 (0xf)
  .maxstack  3
  .locals init (int V_0, //x
                [int] V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  stloc.0
  IL_0003:  ldloc.0
  IL_0004:  ldc.i4.0
  IL_0005:  ceq
  IL_0007:  call       ""Base..ctor(bool)""
  IL_000c:  nop
  IL_000d:  nop
  IL_000e:  ret
}
");

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, ctor1, ctor2, GetSyntaxMapFromMarkers(source1, source0), preserveLocalVariables: true)));

            diff2.VerifyIL("C..ctor", @"
{
  // Code size       22 (0x16)
  .maxstack  2
  .locals init (int V_0, //x
                [int] V_1,
                int V_2) //y
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  stloc.0
  IL_0003:  ldloc.0
  IL_0004:  brtrue.s   IL_000b
  IL_0006:  ldarg.1
  IL_0007:  stloc.2
  IL_0008:  ldc.i4.1
  IL_0009:  br.s       IL_000c
  IL_000b:  ldc.i4.0
  IL_000c:  call       ""Base..ctor(bool)""
  IL_0011:  nop
  IL_0012:  nop
  IL_0013:  ldc.i4.1
  IL_0014:  stloc.2
  IL_0015:  ret
}
");
        }

        [Fact]
        public void PatternVariable_InFieldInitializer()
        {
            var source0 = MarkedSource(@"
public class C
{
    public static int a = 0;
    public bool field = a is int <N:0>x</N:0> && x == 0 && a is int <N:1>y</N:1>;
}");
            var source1 = MarkedSource(@"
public class C
{
    public static int a = 0;
    public bool field = a is int <N:0>x</N:0> && x == 0;
}");

            var compilation0 = CreateCompilation(source0.Tree, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);
            var compilation2 = compilation1.WithSource(source0.Tree);

            var ctor0 = compilation0.GetMember<MethodSymbol>("C..ctor");
            var ctor1 = compilation1.GetMember<MethodSymbol>("C..ctor");
            var ctor2 = compilation2.GetMember<MethodSymbol>("C..ctor");

            var v0 = CompileAndVerify(compilation0);
            v0.VerifyIL("C..ctor", @"
{
  // Code size       33 (0x21)
  .maxstack  2
  .locals init (int V_0, //x
                int V_1) //y
  IL_0000:  ldarg.0
  IL_0001:  ldsfld     ""int C.a""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  brtrue.s   IL_0013
  IL_000a:  ldsfld     ""int C.a""
  IL_000f:  stloc.1
  IL_0010:  ldc.i4.1
  IL_0011:  br.s       IL_0014
  IL_0013:  ldc.i4.0
  IL_0014:  stfld      ""bool C.field""
  IL_0019:  ldarg.0
  IL_001a:  call       ""object..ctor()""
  IL_001f:  nop
  IL_0020:  ret
}
");

            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, ctor0, ctor1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            diff1.VerifyIL("C..ctor", @"
{
  // Code size       24 (0x18)
  .maxstack  3
  .locals init (int V_0, //x
                [int] V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldsfld     ""int C.a""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  ldc.i4.0
  IL_0009:  ceq
  IL_000b:  stfld      ""bool C.field""
  IL_0010:  ldarg.0
  IL_0011:  call       ""object..ctor()""
  IL_0016:  nop
  IL_0017:  ret
}
");

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, ctor1, ctor2, GetSyntaxMapFromMarkers(source1, source0), preserveLocalVariables: true)));

            diff2.VerifyIL("C..ctor", @"
{
  // Code size       33 (0x21)
  .maxstack  2
  .locals init (int V_0, //x
                [int] V_1,
                int V_2) //y
  IL_0000:  ldarg.0
  IL_0001:  ldsfld     ""int C.a""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  brtrue.s   IL_0013
  IL_000a:  ldsfld     ""int C.a""
  IL_000f:  stloc.2
  IL_0010:  ldc.i4.1
  IL_0011:  br.s       IL_0014
  IL_0013:  ldc.i4.0
  IL_0014:  stfld      ""bool C.field""
  IL_0019:  ldarg.0
  IL_001a:  call       ""object..ctor()""
  IL_001f:  nop
  IL_0020:  ret
}
");
        }

        [Fact]
        public void PatternVariable_InQuery()
        {
            var source0 = MarkedSource(@"
using System.Linq;
public class Program
{
    static void N()
    {
        var <N:0>query =
            from a in new int[] { 1, 2 }
            <N:1>select a is int <N:2>x</N:2> && x == 0 && a is int <N:3>y</N:3></N:1></N:0>;
    }
}");
            var source1 = MarkedSource(@"
using System.Linq;
public class Program
{
    static int M(int x, out int y) { y = 42; return 43; }
    static void N()
    {
        var <N:0>query =
            from a in new int[] { 1, 2 }
            <N:1>select a is int <N:2>x</N:2> && x == 0</N:1></N:0>;
    }
}");
            var compilation0 = CreateCompilation(source0.Tree, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);
            var compilation2 = compilation1.WithSource(source0.Tree);

            var n0 = compilation0.GetMember<MethodSymbol>("Program.N");
            var n1 = compilation1.GetMember<MethodSymbol>("Program.N");
            var n2 = compilation2.GetMember<MethodSymbol>("Program.N");

            var v0 = CompileAndVerify(compilation0);
            v0.VerifyIL("Program.N()", @"
{
  // Code size       53 (0x35)
  .maxstack  4
  .locals init (System.Collections.Generic.IEnumerable<bool> V_0) //query
  IL_0000:  nop
  IL_0001:  ldc.i4.2
  IL_0002:  newarr     ""int""
  IL_0007:  dup
  IL_0008:  ldc.i4.0
  IL_0009:  ldc.i4.1
  IL_000a:  stelem.i4
  IL_000b:  dup
  IL_000c:  ldc.i4.1
  IL_000d:  ldc.i4.2
  IL_000e:  stelem.i4
  IL_000f:  ldsfld     ""System.Func<int, bool> Program.<>c.<>9__0_0""
  IL_0014:  dup
  IL_0015:  brtrue.s   IL_002e
  IL_0017:  pop
  IL_0018:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_001d:  ldftn      ""bool Program.<>c.<N>b__0_0(int)""
  IL_0023:  newobj     ""System.Func<int, bool>..ctor(object, System.IntPtr)""
  IL_0028:  dup
  IL_0029:  stsfld     ""System.Func<int, bool> Program.<>c.<>9__0_0""
  IL_002e:  call       ""System.Collections.Generic.IEnumerable<bool> System.Linq.Enumerable.Select<int, bool>(System.Collections.Generic.IEnumerable<int>, System.Func<int, bool>)""
  IL_0033:  stloc.0
  IL_0034:  ret
}
");
            v0.VerifyIL("Program.<>c.<N>b__0_0(int)", @"
{
  // Code size       12 (0xc)
  .maxstack  1
  .locals init (int V_0, //x
                int V_1) //y
  IL_0000:  ldarg.1
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  brtrue.s   IL_000a
  IL_0005:  ldarg.1
  IL_0006:  stloc.1
  IL_0007:  ldc.i4.1
  IL_0008:  br.s       IL_000b
  IL_000a:  ldc.i4.0
  IL_000b:  ret
}
");

            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, n0, n1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            diff1.VerifySynthesizedMembers("Program: {<>c}", "Program.<>c: {<>9__0_0, <N>b__0_0}");

            diff1.VerifyIL("Program.N()", @"
{
  // Code size       53 (0x35)
  .maxstack  4
  .locals init (System.Collections.Generic.IEnumerable<bool> V_0) //query
  IL_0000:  nop
  IL_0001:  ldc.i4.2
  IL_0002:  newarr     ""int""
  IL_0007:  dup
  IL_0008:  ldc.i4.0
  IL_0009:  ldc.i4.1
  IL_000a:  stelem.i4
  IL_000b:  dup
  IL_000c:  ldc.i4.1
  IL_000d:  ldc.i4.2
  IL_000e:  stelem.i4
  IL_000f:  ldsfld     ""System.Func<int, bool> Program.<>c.<>9__0_0""
  IL_0014:  dup
  IL_0015:  brtrue.s   IL_002e
  IL_0017:  pop
  IL_0018:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_001d:  ldftn      ""bool Program.<>c.<N>b__0_0(int)""
  IL_0023:  newobj     ""System.Func<int, bool>..ctor(object, System.IntPtr)""
  IL_0028:  dup
  IL_0029:  stsfld     ""System.Func<int, bool> Program.<>c.<>9__0_0""
  IL_002e:  call       ""System.Collections.Generic.IEnumerable<bool> System.Linq.Enumerable.Select<int, bool>(System.Collections.Generic.IEnumerable<int>, System.Func<int, bool>)""
  IL_0033:  stloc.0
  IL_0034:  ret
}
");
            diff1.VerifyIL("Program.<>c.<N>b__0_0(int)", @"
{
  // Code size        7 (0x7)
  .maxstack  2
  .locals init (int V_0, //x
                [int] V_1)
  IL_0000:  ldarg.1
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  ldc.i4.0
  IL_0004:  ceq
  IL_0006:  ret
}
");

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, n1, n2, GetSyntaxMapFromMarkers(source1, source0), preserveLocalVariables: true)));

            diff2.VerifySynthesizedMembers("Program: {<>c}", "Program.<>c: {<>9__0_0, <N>b__0_0}");

            diff2.VerifyIL("Program.N()", @"
{
  // Code size       53 (0x35)
  .maxstack  4
  .locals init (System.Collections.Generic.IEnumerable<bool> V_0) //query
  IL_0000:  nop
  IL_0001:  ldc.i4.2
  IL_0002:  newarr     ""int""
  IL_0007:  dup
  IL_0008:  ldc.i4.0
  IL_0009:  ldc.i4.1
  IL_000a:  stelem.i4
  IL_000b:  dup
  IL_000c:  ldc.i4.1
  IL_000d:  ldc.i4.2
  IL_000e:  stelem.i4
  IL_000f:  ldsfld     ""System.Func<int, bool> Program.<>c.<>9__0_0""
  IL_0014:  dup
  IL_0015:  brtrue.s   IL_002e
  IL_0017:  pop
  IL_0018:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_001d:  ldftn      ""bool Program.<>c.<N>b__0_0(int)""
  IL_0023:  newobj     ""System.Func<int, bool>..ctor(object, System.IntPtr)""
  IL_0028:  dup
  IL_0029:  stsfld     ""System.Func<int, bool> Program.<>c.<>9__0_0""
  IL_002e:  call       ""System.Collections.Generic.IEnumerable<bool> System.Linq.Enumerable.Select<int, bool>(System.Collections.Generic.IEnumerable<int>, System.Func<int, bool>)""
  IL_0033:  stloc.0
  IL_0034:  ret
}
");
            diff2.VerifyIL("Program.<>c.<N>b__0_0(int)", @"
{
  // Code size       12 (0xc)
  .maxstack  1
  .locals init (int V_0, //x
                [int] V_1,
                int V_2) //y
  IL_0000:  ldarg.1
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  brtrue.s   IL_000a
  IL_0005:  ldarg.1
  IL_0006:  stloc.2
  IL_0007:  ldc.i4.1
  IL_0008:  br.s       IL_000b
  IL_000a:  ldc.i4.0
  IL_000b:  ret
}
");
        }

        [Fact]
        public void Tuple_Parenthesized()
        {
            var source0 = MarkedSource(@"
class C
{
    static int F() { (int, (int, int)) <N:0>x</N:0> = (1, (2, 3)); return x.Item1 + x.Item2.Item1 + x.Item2.Item2; }
}");
            var source1 = MarkedSource(@"
class C
{
    static int F() { (int, int, int) <N:0>x</N:0> = (1, 2, 3); return x.Item1 + x.Item2 + x.Item3; }
}");

            var source2 = MarkedSource(@"
class C
{
    static int F() { (int, int) <N:0>x</N:0> = (1, 3); return x.Item1 + x.Item2; }
}");

            var compilation0 = CreateCompilation(source0.Tree, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);
            var compilation2 = compilation1.WithSource(source2.Tree);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");
            var f2 = compilation2.GetMember<MethodSymbol>("C.F");

            var v0 = CompileAndVerify(compilation0);
            v0.VerifyIL("C.F", @"
{
  // Code size       51 (0x33)
  .maxstack  4
  .locals init (System.ValueTuple<int, (int, int)> V_0, //x
                int V_1)
  IL_0000:  nop
  IL_0001:  ldloca.s   V_0
  IL_0003:  ldc.i4.1
  IL_0004:  ldc.i4.2
  IL_0005:  ldc.i4.3
  IL_0006:  newobj     ""System.ValueTuple<int, int>..ctor(int, int)""
  IL_000b:  call       ""System.ValueTuple<int, (int, int)>..ctor(int, (int, int))""
  IL_0010:  ldloc.0
  IL_0011:  ldfld      ""int System.ValueTuple<int, (int, int)>.Item1""
  IL_0016:  ldloc.0
  IL_0017:  ldfld      ""(int, int) System.ValueTuple<int, (int, int)>.Item2""
  IL_001c:  ldfld      ""int System.ValueTuple<int, int>.Item1""
  IL_0021:  add
  IL_0022:  ldloc.0
  IL_0023:  ldfld      ""(int, int) System.ValueTuple<int, (int, int)>.Item2""
  IL_0028:  ldfld      ""int System.ValueTuple<int, int>.Item2""
  IL_002d:  add
  IL_002e:  stloc.1
  IL_002f:  br.s       IL_0031
  IL_0031:  ldloc.1
  IL_0032:  ret
}
");

            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            diff1.VerifyIL("C.F", @"
{
  // Code size       36 (0x24)
  .maxstack  4
  .locals init ([unchanged] V_0,
                [int] V_1,
                System.ValueTuple<int, int, int> V_2, //x
                int V_3)
  IL_0000:  nop
  IL_0001:  ldloca.s   V_2
  IL_0003:  ldc.i4.1
  IL_0004:  ldc.i4.2
  IL_0005:  ldc.i4.3
  IL_0006:  call       ""System.ValueTuple<int, int, int>..ctor(int, int, int)""
  IL_000b:  ldloc.2
  IL_000c:  ldfld      ""int System.ValueTuple<int, int, int>.Item1""
  IL_0011:  ldloc.2
  IL_0012:  ldfld      ""int System.ValueTuple<int, int, int>.Item2""
  IL_0017:  add
  IL_0018:  ldloc.2
  IL_0019:  ldfld      ""int System.ValueTuple<int, int, int>.Item3""
  IL_001e:  add
  IL_001f:  stloc.3
  IL_0020:  br.s       IL_0022
  IL_0022:  ldloc.3
  IL_0023:  ret
}
");

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, f1, f2, GetSyntaxMapFromMarkers(source1, source2), preserveLocalVariables: true)));

            diff2.VerifyIL("C.F", @"
{
  // Code size       32 (0x20)
  .maxstack  3
  .locals init ([unchanged] V_0,
                [int] V_1,
                [unchanged] V_2,
                [int] V_3,
                System.ValueTuple<int, int> V_4, //x
                int V_5)
  IL_0000:  nop
  IL_0001:  ldloca.s   V_4
  IL_0003:  ldc.i4.1
  IL_0004:  ldc.i4.3
  IL_0005:  call       ""System.ValueTuple<int, int>..ctor(int, int)""
  IL_000a:  ldloc.s    V_4
  IL_000c:  ldfld      ""int System.ValueTuple<int, int>.Item1""
  IL_0011:  ldloc.s    V_4
  IL_0013:  ldfld      ""int System.ValueTuple<int, int>.Item2""
  IL_0018:  add
  IL_0019:  stloc.s    V_5
  IL_001b:  br.s       IL_001d
  IL_001d:  ldloc.s    V_5
  IL_001f:  ret
}
");
        }

        [Fact]
        public void Tuple_Decomposition()
        {
            var source0 = MarkedSource(@"
class C
{
    static int F() { (int <N:0>x</N:0>, int <N:1>y</N:1>, int <N:2>z</N:2>) = (1, 2, 3); return x + y + z; }
}");
            var source1 = MarkedSource(@"
class C
{
    static int F() { (int <N:0>x</N:0>, int <N:2>z</N:2>) = (1, 3); return x + z; }
}");

            var source2 = MarkedSource(@"
class C
{
    static int F() { (int <N:0>x</N:0>, int <N:1>y</N:1>, int <N:2>z</N:2>) = (1, 2, 3); return x + y + z; }
}");

            var compilation0 = CreateCompilation(source0.Tree, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);
            var compilation2 = compilation1.WithSource(source2.Tree);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");
            var f2 = compilation2.GetMember<MethodSymbol>("C.F");

            var v0 = CompileAndVerify(compilation0);
            v0.VerifyIL("C.F", @"
{
  // Code size       17 (0x11)
  .maxstack  2
  .locals init (int V_0, //x
                int V_1, //y
                int V_2, //z
                int V_3)
  IL_0000:  nop
  IL_0001:  ldc.i4.1
  IL_0002:  stloc.0
  IL_0003:  ldc.i4.2
  IL_0004:  stloc.1
  IL_0005:  ldc.i4.3
  IL_0006:  stloc.2
  IL_0007:  ldloc.0
  IL_0008:  ldloc.1
  IL_0009:  add
  IL_000a:  ldloc.2
  IL_000b:  add
  IL_000c:  stloc.3
  IL_000d:  br.s       IL_000f
  IL_000f:  ldloc.3
  IL_0010:  ret
}
");

            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            diff1.VerifyIL("C.F", @"
{
  // Code size       15 (0xf)
  .maxstack  2
  .locals init (int V_0, //x
                [int] V_1,
                int V_2, //z
                [int] V_3,
                int V_4)
  IL_0000:  nop
  IL_0001:  ldc.i4.1
  IL_0002:  stloc.0
  IL_0003:  ldc.i4.3
  IL_0004:  stloc.2
  IL_0005:  ldloc.0
  IL_0006:  ldloc.2
  IL_0007:  add
  IL_0008:  stloc.s    V_4
  IL_000a:  br.s       IL_000c
  IL_000c:  ldloc.s    V_4
  IL_000e:  ret
}
");

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, f1, f2, GetSyntaxMapFromMarkers(source1, source2), preserveLocalVariables: true)));

            diff2.VerifyIL("C.F", @"
{
  // Code size       21 (0x15)
  .maxstack  2
  .locals init (int V_0, //x
                [int] V_1,
                int V_2, //z
                [int] V_3,
                [int] V_4,
                int V_5, //y
                int V_6)
  IL_0000:  nop
  IL_0001:  ldc.i4.1
  IL_0002:  stloc.0
  IL_0003:  ldc.i4.2
  IL_0004:  stloc.s    V_5
  IL_0006:  ldc.i4.3
  IL_0007:  stloc.2
  IL_0008:  ldloc.0
  IL_0009:  ldloc.s    V_5
  IL_000b:  add
  IL_000c:  ldloc.2
  IL_000d:  add
  IL_000e:  stloc.s    V_6
  IL_0010:  br.s       IL_0012
  IL_0012:  ldloc.s    V_6
  IL_0014:  ret
}
");
        }

        [Fact]
        public void ForeachStatement()
        {
            var source0 = MarkedSource(@"
class C
{
    public static (int, (bool, double))[] F() => new[] { (1, (true, 2.0)) };

    public static void G()
    {        
        foreach (var (<N:0>x</N:0>, (<N:1>y</N:1>, <N:2>z</N:2>)) in F())
        {
            System.Console.WriteLine(x);
        }
    }
}");
            var source1 = MarkedSource(@"
class C
{
    public static (int, (bool, double))[] F() => new[] { (1, (true, 2.0)) };

    public static void G()
    {        
        foreach (var (<N:0>x1</N:0>, (<N:1>y</N:1>, <N:2>z</N:2>)) in F())
        {
            System.Console.WriteLine(x1);
        }
    }
}");

            var source2 = MarkedSource(@"
class C
{
    public static (int, (bool, double))[] F() => new[] { (1, (true, 2.0)) };

    public static void G()
    {        
        foreach (var (<N:0>x1</N:0>, <N:1>yz</N:1>) in F())
        {
            System.Console.WriteLine(x1);
        }
    }
}");

            var compilation0 = CreateCompilation(source0.Tree, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);
            var compilation2 = compilation1.WithSource(source2.Tree);

            var f0 = compilation0.GetMember<MethodSymbol>("C.G");
            var f1 = compilation1.GetMember<MethodSymbol>("C.G");
            var f2 = compilation2.GetMember<MethodSymbol>("C.G");

            var v0 = CompileAndVerify(compilation0);
            v0.VerifyIL("C.G", @"
{
  // Code size       70 (0x46)
  .maxstack  2
  .locals init ((int, (bool, double))[] V_0,
                int V_1,
                int V_2, //x
                bool V_3, //y
                double V_4, //z
                System.ValueTuple<bool, double> V_5)
  IL_0000:  nop
  IL_0001:  nop
  IL_0002:  call       ""(int, (bool, double))[] C.F()""
  IL_0007:  stloc.0
  IL_0008:  ldc.i4.0
  IL_0009:  stloc.1
  IL_000a:  br.s       IL_003f
  IL_000c:  ldloc.0
  IL_000d:  ldloc.1
  IL_000e:  ldelem     ""System.ValueTuple<int, (bool, double)>""
  IL_0013:  dup
  IL_0014:  ldfld      ""(bool, double) System.ValueTuple<int, (bool, double)>.Item2""
  IL_0019:  stloc.s    V_5
  IL_001b:  ldfld      ""int System.ValueTuple<int, (bool, double)>.Item1""
  IL_0020:  stloc.2
  IL_0021:  ldloc.s    V_5
  IL_0023:  ldfld      ""bool System.ValueTuple<bool, double>.Item1""
  IL_0028:  stloc.3
  IL_0029:  ldloc.s    V_5
  IL_002b:  ldfld      ""double System.ValueTuple<bool, double>.Item2""
  IL_0030:  stloc.s    V_4
  IL_0032:  nop
  IL_0033:  ldloc.2
  IL_0034:  call       ""void System.Console.WriteLine(int)""
  IL_0039:  nop
  IL_003a:  nop
  IL_003b:  ldloc.1
  IL_003c:  ldc.i4.1
  IL_003d:  add
  IL_003e:  stloc.1
  IL_003f:  ldloc.1
  IL_0040:  ldloc.0
  IL_0041:  ldlen
  IL_0042:  conv.i4
  IL_0043:  blt.s      IL_000c
  IL_0045:  ret
}
");

            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            diff1.VerifyIL("C.G", @"
{
  // Code size       78 (0x4e)
  .maxstack  2
  .locals init ([unchanged] V_0,
                [int] V_1,
                int V_2, //x1
                bool V_3, //y
                double V_4, //z
                [unchanged] V_5,
                (int, (bool, double))[] V_6,
                int V_7,
                System.ValueTuple<bool, double> V_8)
  IL_0000:  nop
  IL_0001:  nop
  IL_0002:  call       ""(int, (bool, double))[] C.F()""
  IL_0007:  stloc.s    V_6
  IL_0009:  ldc.i4.0
  IL_000a:  stloc.s    V_7
  IL_000c:  br.s       IL_0045
  IL_000e:  ldloc.s    V_6
  IL_0010:  ldloc.s    V_7
  IL_0012:  ldelem     ""System.ValueTuple<int, (bool, double)>""
  IL_0017:  dup
  IL_0018:  ldfld      ""(bool, double) System.ValueTuple<int, (bool, double)>.Item2""
  IL_001d:  stloc.s    V_8
  IL_001f:  ldfld      ""int System.ValueTuple<int, (bool, double)>.Item1""
  IL_0024:  stloc.2
  IL_0025:  ldloc.s    V_8
  IL_0027:  ldfld      ""bool System.ValueTuple<bool, double>.Item1""
  IL_002c:  stloc.3
  IL_002d:  ldloc.s    V_8
  IL_002f:  ldfld      ""double System.ValueTuple<bool, double>.Item2""
  IL_0034:  stloc.s    V_4
  IL_0036:  nop
  IL_0037:  ldloc.2
  IL_0038:  call       ""void System.Console.WriteLine(int)""
  IL_003d:  nop
  IL_003e:  nop
  IL_003f:  ldloc.s    V_7
  IL_0041:  ldc.i4.1
  IL_0042:  add
  IL_0043:  stloc.s    V_7
  IL_0045:  ldloc.s    V_7
  IL_0047:  ldloc.s    V_6
  IL_0049:  ldlen
  IL_004a:  conv.i4
  IL_004b:  blt.s      IL_000e
  IL_004d:  ret
}
");

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, f1, f2, GetSyntaxMapFromMarkers(source1, source2), preserveLocalVariables: true)));

            diff2.VerifyIL("C.G", @"
{
  // Code size       61 (0x3d)
  .maxstack  2
  .locals init ([unchanged] V_0,
                [int] V_1,
                int V_2, //x1
                [bool] V_3,
                [unchanged] V_4,
                [unchanged] V_5,
                [unchanged] V_6,
                [int] V_7,
                [unchanged] V_8,
                (int, (bool, double))[] V_9,
                int V_10,
                System.ValueTuple<bool, double> V_11) //yz
  IL_0000:  nop
  IL_0001:  nop
  IL_0002:  call       ""(int, (bool, double))[] C.F()""
  IL_0007:  stloc.s    V_9
  IL_0009:  ldc.i4.0
  IL_000a:  stloc.s    V_10
  IL_000c:  br.s       IL_0034
  IL_000e:  ldloc.s    V_9
  IL_0010:  ldloc.s    V_10
  IL_0012:  ldelem     ""System.ValueTuple<int, (bool, double)>""
  IL_0017:  dup
  IL_0018:  ldfld      ""int System.ValueTuple<int, (bool, double)>.Item1""
  IL_001d:  stloc.2
  IL_001e:  ldfld      ""(bool, double) System.ValueTuple<int, (bool, double)>.Item2""
  IL_0023:  stloc.s    V_11
  IL_0025:  nop
  IL_0026:  ldloc.2
  IL_0027:  call       ""void System.Console.WriteLine(int)""
  IL_002c:  nop
  IL_002d:  nop
  IL_002e:  ldloc.s    V_10
  IL_0030:  ldc.i4.1
  IL_0031:  add
  IL_0032:  stloc.s    V_10
  IL_0034:  ldloc.s    V_10
  IL_0036:  ldloc.s    V_9
  IL_0038:  ldlen
  IL_0039:  conv.i4
  IL_003a:  blt.s      IL_000e
  IL_003c:  ret
}
");
        }

        [Fact]
        public void OutVar()
        {
            var source0 = MarkedSource(@"
class C
{
    static void F(out int x, out int y) { x = 1; y = 2; }
    static int G() { F(out int <N:0>x</N:0>, out var <N:1>y</N:1>); return x + y; }
}");
            var source1 = MarkedSource(@"
class C
{
    static void F(out int x, out int y) { x = 1; y = 2; }
    static int G() { F(out int <N:0>x</N:0>, out var <N:1>z</N:1>); return x + z; }
}");

            var source2 = MarkedSource(@"
class C
{
    static void F(out int x, out int y) { x = 1; y = 2; }
    static int G() { F(out int <N:0>x</N:0>, out int <N:1>y</N:1>); return x + y; }
}");

            var compilation0 = CreateCompilation(source0.Tree, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);
            var compilation2 = compilation1.WithSource(source2.Tree);

            var f0 = compilation0.GetMember<MethodSymbol>("C.G");
            var f1 = compilation1.GetMember<MethodSymbol>("C.G");
            var f2 = compilation2.GetMember<MethodSymbol>("C.G");

            var v0 = CompileAndVerify(compilation0);
            v0.VerifyIL("C.G", @"
{
  // Code size       19 (0x13)
  .maxstack  2
  .locals init (int V_0, //x
                int V_1, //y
                int V_2)
  IL_0000:  nop
  IL_0001:  ldloca.s   V_0
  IL_0003:  ldloca.s   V_1
  IL_0005:  call       ""void C.F(out int, out int)""
  IL_000a:  nop
  IL_000b:  ldloc.0
  IL_000c:  ldloc.1
  IL_000d:  add
  IL_000e:  stloc.2
  IL_000f:  br.s       IL_0011
  IL_0011:  ldloc.2
  IL_0012:  ret
}
");

            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);
            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            diff1.VerifyIL("C.G", @"
{
  // Code size       19 (0x13)
  .maxstack  2
  .locals init (int V_0, //x
                int V_1, //z
                [int] V_2,
                int V_3)
  IL_0000:  nop
  IL_0001:  ldloca.s   V_0
  IL_0003:  ldloca.s   V_1
  IL_0005:  call       ""void C.F(out int, out int)""
  IL_000a:  nop
  IL_000b:  ldloc.0
  IL_000c:  ldloc.1
  IL_000d:  add
  IL_000e:  stloc.3
  IL_000f:  br.s       IL_0011
  IL_0011:  ldloc.3
  IL_0012:  ret
}
");

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, f1, f2, GetSyntaxMapFromMarkers(source1, source2), preserveLocalVariables: true)));

            diff2.VerifyIL("C.G", @"
{
  // Code size       21 (0x15)
  .maxstack  2
  .locals init (int V_0, //x
                int V_1, //y
                [int] V_2,
                [int] V_3,
                int V_4)
  IL_0000:  nop
  IL_0001:  ldloca.s   V_0
  IL_0003:  ldloca.s   V_1
  IL_0005:  call       ""void C.F(out int, out int)""
  IL_000a:  nop
  IL_000b:  ldloc.0
  IL_000c:  ldloc.1
  IL_000d:  add
  IL_000e:  stloc.s    V_4
  IL_0010:  br.s       IL_0012
  IL_0012:  ldloc.s    V_4
  IL_0014:  ret
}
");
        }

        [Fact]
        public void OutVar_InConstructorInitializer()
        {
            var baseClass = "public class Base { public Base(int x) { } }";

            var source0 = MarkedSource(@"
public class C : Base
{
    public C() : base(M(out int <N:0>x</N:0>) + x + M(out int <N:1>y</N:1>)) { System.Console.Write(y); }
    static int M(out int x) => throw null;
}" + baseClass);
            var source1 = MarkedSource(@"
public class C : Base
{
    public C() : base(M(out int <N:0>x</N:0>) + x) { }
    static int M(out int x) => throw null;
}" + baseClass);

            var compilation0 = CreateCompilation(source0.Tree, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);
            var compilation2 = compilation1.WithSource(source0.Tree);

            var ctor0 = compilation0.GetMember<MethodSymbol>("C..ctor");
            var ctor1 = compilation1.GetMember<MethodSymbol>("C..ctor");
            var ctor2 = compilation2.GetMember<MethodSymbol>("C..ctor");

            var v0 = CompileAndVerify(compilation0);
            v0.VerifyIL("C..ctor", @"
{
  // Code size       33 (0x21)
  .maxstack  3
  .locals init (int V_0, //x
                int V_1) //y
  IL_0000:  ldarg.0
  IL_0001:  ldloca.s   V_0
  IL_0003:  call       ""int C.M(out int)""
  IL_0008:  ldloc.0
  IL_0009:  add
  IL_000a:  ldloca.s   V_1
  IL_000c:  call       ""int C.M(out int)""
  IL_0011:  add
  IL_0012:  call       ""Base..ctor(int)""
  IL_0017:  nop
  IL_0018:  nop
  IL_0019:  ldloc.1
  IL_001a:  call       ""void System.Console.Write(int)""
  IL_001f:  nop
  IL_0020:  ret
}
");

            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, ctor0, ctor1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            diff1.VerifyIL("C..ctor", @"
{
  // Code size       18 (0x12)
  .maxstack  3
  .locals init (int V_0, //x
                [int] V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldloca.s   V_0
  IL_0003:  call       ""int C.M(out int)""
  IL_0008:  ldloc.0
  IL_0009:  add
  IL_000a:  call       ""Base..ctor(int)""
  IL_000f:  nop
  IL_0010:  nop
  IL_0011:  ret
}
");

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, ctor1, ctor2, GetSyntaxMapFromMarkers(source1, source0), preserveLocalVariables: true)));

            diff2.VerifyIL("C..ctor", @"
{
  // Code size       33 (0x21)
  .maxstack  3
  .locals init (int V_0, //x
                [int] V_1,
                int V_2) //y
  IL_0000:  ldarg.0
  IL_0001:  ldloca.s   V_0
  IL_0003:  call       ""int C.M(out int)""
  IL_0008:  ldloc.0
  IL_0009:  add
  IL_000a:  ldloca.s   V_2
  IL_000c:  call       ""int C.M(out int)""
  IL_0011:  add
  IL_0012:  call       ""Base..ctor(int)""
  IL_0017:  nop
  IL_0018:  nop
  IL_0019:  ldloc.2
  IL_001a:  call       ""void System.Console.Write(int)""
  IL_001f:  nop
  IL_0020:  ret
}
");
        }

        [Fact]
        public void OutVar_InConstructorInitializer_WithLambda()
        {
            var baseClass = "public class Base { public Base(int x) { } }";

            var source0 = MarkedSource(@"
public class C : Base
{
    <N:0>public C() : base(M(out int <N:1>x</N:1>) + M2(<N:2>() => x + 1</N:2>)) { }</N:0>
    static int M(out int x) => throw null;
    static int M2(System.Func<int> x) => throw null;
}" + baseClass);
            var source1 = MarkedSource(@"
public class C : Base
{
    <N:0>public C() : base(M(out int <N:1>x</N:1>) + M2(<N:2>() => x - 1</N:2>)) { }</N:0>
    static int M(out int x) => throw null;
    static int M2(System.Func<int> x) => throw null;
}" + baseClass);

            var compilation0 = CreateCompilation(source0.Tree, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);
            var compilation2 = compilation1.WithSource(source0.Tree);

            var ctor0 = compilation0.GetMember<MethodSymbol>("C..ctor");
            var ctor1 = compilation1.GetMember<MethodSymbol>("C..ctor");
            var ctor2 = compilation2.GetMember<MethodSymbol>("C..ctor");

            var v0 = CompileAndVerify(compilation0);
            v0.VerifyIL("C..ctor", @"
{
  // Code size       44 (0x2c)
  .maxstack  4
  .locals init (C.<>c__DisplayClass0_0 V_0) //CS$<>8__locals0
  IL_0000:  newobj     ""C.<>c__DisplayClass0_0..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldarg.0
  IL_0007:  ldloc.0
  IL_0008:  ldflda     ""int C.<>c__DisplayClass0_0.x""
  IL_000d:  call       ""int C.M(out int)""
  IL_0012:  ldloc.0
  IL_0013:  ldftn      ""int C.<>c__DisplayClass0_0.<.ctor>b__0()""
  IL_0019:  newobj     ""System.Func<int>..ctor(object, System.IntPtr)""
  IL_001e:  call       ""int C.M2(System.Func<int>)""
  IL_0023:  add
  IL_0024:  call       ""Base..ctor(int)""
  IL_0029:  nop
  IL_002a:  nop
  IL_002b:  ret
}
");
            v0.VerifyIL("C.<>c__DisplayClass0_0.<.ctor>b__0()", @"
{
  // Code size        9 (0x9)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<>c__DisplayClass0_0.x""
  IL_0006:  ldc.i4.1
  IL_0007:  add
  IL_0008:  ret
}
");

            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, ctor0, ctor1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            diff1.VerifySynthesizedMembers(
                "C: {<>c__DisplayClass0_0}",
                "C.<>c__DisplayClass0_0: {x, <.ctor>b__0}");

            diff1.VerifyIL("C..ctor", @"
{
  // Code size       44 (0x2c)
  .maxstack  4
  .locals init (C.<>c__DisplayClass0_0 V_0) //CS$<>8__locals0
  IL_0000:  newobj     ""C.<>c__DisplayClass0_0..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldarg.0
  IL_0007:  ldloc.0
  IL_0008:  ldflda     ""int C.<>c__DisplayClass0_0.x""
  IL_000d:  call       ""int C.M(out int)""
  IL_0012:  ldloc.0
  IL_0013:  ldftn      ""int C.<>c__DisplayClass0_0.<.ctor>b__0()""
  IL_0019:  newobj     ""System.Func<int>..ctor(object, System.IntPtr)""
  IL_001e:  call       ""int C.M2(System.Func<int>)""
  IL_0023:  add
  IL_0024:  call       ""Base..ctor(int)""
  IL_0029:  nop
  IL_002a:  nop
  IL_002b:  ret
}
");
            diff1.VerifyIL("C.<>c__DisplayClass0_0.<.ctor>b__0()", @"
{
  // Code size        9 (0x9)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<>c__DisplayClass0_0.x""
  IL_0006:  ldc.i4.1
  IL_0007:  sub
  IL_0008:  ret
}
");

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, ctor1, ctor2, GetSyntaxMapFromMarkers(source1, source0), preserveLocalVariables: true)));

            diff2.VerifySynthesizedMembers(
                "C: {<>c__DisplayClass0_0}",
                "C.<>c__DisplayClass0_0: {x, <.ctor>b__0}");

            diff2.VerifyIL("C..ctor", @"
{
  // Code size       44 (0x2c)
  .maxstack  4
  .locals init (C.<>c__DisplayClass0_0 V_0) //CS$<>8__locals0
  IL_0000:  newobj     ""C.<>c__DisplayClass0_0..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldarg.0
  IL_0007:  ldloc.0
  IL_0008:  ldflda     ""int C.<>c__DisplayClass0_0.x""
  IL_000d:  call       ""int C.M(out int)""
  IL_0012:  ldloc.0
  IL_0013:  ldftn      ""int C.<>c__DisplayClass0_0.<.ctor>b__0()""
  IL_0019:  newobj     ""System.Func<int>..ctor(object, System.IntPtr)""
  IL_001e:  call       ""int C.M2(System.Func<int>)""
  IL_0023:  add
  IL_0024:  call       ""Base..ctor(int)""
  IL_0029:  nop
  IL_002a:  nop
  IL_002b:  ret
}
");
            diff2.VerifyIL("C.<>c__DisplayClass0_0.<.ctor>b__0()", @"
{
  // Code size        9 (0x9)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<>c__DisplayClass0_0.x""
  IL_0006:  ldc.i4.1
  IL_0007:  add
  IL_0008:  ret
}
");
        }

        [Fact]
        public void OutVar_InMethodBody_WithLambda()
        {
            var source0 = MarkedSource(@"
public class C
{
    public void Method() <N:0>{ int _ = M(out int <N:1>x</N:1>) + M2(<N:2>() => x + 1</N:2>); }</N:0>
    static int M(out int x) => throw null;
    static int M2(System.Func<int> x) => throw null;
}");
            var source1 = MarkedSource(@"
public class C
{
    public void Method() <N:0>{ int _ = M(out int <N:1>x</N:1>) + M2(<N:2>() => x - 1</N:2>); }</N:0>
    static int M(out int x) => throw null;
    static int M2(System.Func<int> x) => throw null;
}");

            var compilation0 = CreateCompilation(source0.Tree, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);
            var compilation2 = compilation1.WithSource(source0.Tree);

            var ctor0 = compilation0.GetMember<MethodSymbol>("C.Method");
            var ctor1 = compilation1.GetMember<MethodSymbol>("C.Method");
            var ctor2 = compilation2.GetMember<MethodSymbol>("C.Method");

            var v0 = CompileAndVerify(compilation0);
            v0.VerifyIL("C.Method", @"
{
  // Code size       38 (0x26)
  .maxstack  3
  .locals init (C.<>c__DisplayClass0_0 V_0, //CS$<>8__locals0
                int V_1) //_
  IL_0000:  newobj     ""C.<>c__DisplayClass0_0..ctor()""
  IL_0005:  stloc.0
  IL_0006:  nop
  IL_0007:  ldloc.0
  IL_0008:  ldflda     ""int C.<>c__DisplayClass0_0.x""
  IL_000d:  call       ""int C.M(out int)""
  IL_0012:  ldloc.0
  IL_0013:  ldftn      ""int C.<>c__DisplayClass0_0.<Method>b__0()""
  IL_0019:  newobj     ""System.Func<int>..ctor(object, System.IntPtr)""
  IL_001e:  call       ""int C.M2(System.Func<int>)""
  IL_0023:  add
  IL_0024:  stloc.1
  IL_0025:  ret
}
");
            v0.VerifyIL("C.<>c__DisplayClass0_0.<Method>b__0()", @"
{
  // Code size        9 (0x9)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<>c__DisplayClass0_0.x""
  IL_0006:  ldc.i4.1
  IL_0007:  add
  IL_0008:  ret
}
");

            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, ctor0, ctor1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            diff1.VerifySynthesizedMembers("C: {<>c__DisplayClass0_0}", "C.<>c__DisplayClass0_0: {x, <Method>b__0}");

            diff1.VerifyIL("C.Method", @"
{
  // Code size       38 (0x26)
  .maxstack  3
  .locals init (C.<>c__DisplayClass0_0 V_0, //CS$<>8__locals0
                [int] V_1,
                int V_2) //_
  IL_0000:  newobj     ""C.<>c__DisplayClass0_0..ctor()""
  IL_0005:  stloc.0
  IL_0006:  nop
  IL_0007:  ldloc.0
  IL_0008:  ldflda     ""int C.<>c__DisplayClass0_0.x""
  IL_000d:  call       ""int C.M(out int)""
  IL_0012:  ldloc.0
  IL_0013:  ldftn      ""int C.<>c__DisplayClass0_0.<Method>b__0()""
  IL_0019:  newobj     ""System.Func<int>..ctor(object, System.IntPtr)""
  IL_001e:  call       ""int C.M2(System.Func<int>)""
  IL_0023:  add
  IL_0024:  stloc.2
  IL_0025:  ret
}
");
            diff1.VerifyIL("C.<>c__DisplayClass0_0.<Method>b__0()", @"
{
  // Code size        9 (0x9)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<>c__DisplayClass0_0.x""
  IL_0006:  ldc.i4.1
  IL_0007:  sub
  IL_0008:  ret
}
");

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, ctor1, ctor2, GetSyntaxMapFromMarkers(source1, source0), preserveLocalVariables: true)));

            diff2.VerifySynthesizedMembers("C: {<>c__DisplayClass0_0}", "C.<>c__DisplayClass0_0: {x, <Method>b__0}");

            diff2.VerifyIL("C.Method", @"
{
  // Code size       38 (0x26)
  .maxstack  3
  .locals init (C.<>c__DisplayClass0_0 V_0, //CS$<>8__locals0
                [int] V_1,
                [int] V_2,
                int V_3) //_
  IL_0000:  newobj     ""C.<>c__DisplayClass0_0..ctor()""
  IL_0005:  stloc.0
  IL_0006:  nop
  IL_0007:  ldloc.0
  IL_0008:  ldflda     ""int C.<>c__DisplayClass0_0.x""
  IL_000d:  call       ""int C.M(out int)""
  IL_0012:  ldloc.0
  IL_0013:  ldftn      ""int C.<>c__DisplayClass0_0.<Method>b__0()""
  IL_0019:  newobj     ""System.Func<int>..ctor(object, System.IntPtr)""
  IL_001e:  call       ""int C.M2(System.Func<int>)""
  IL_0023:  add
  IL_0024:  stloc.3
  IL_0025:  ret
}
");
            diff2.VerifyIL("C.<>c__DisplayClass0_0.<Method>b__0()", @"
{
  // Code size        9 (0x9)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<>c__DisplayClass0_0.x""
  IL_0006:  ldc.i4.1
  IL_0007:  add
  IL_0008:  ret
}
");
        }

        [Fact]
        public void OutVar_InFieldInitializer()
        {
            var source0 = MarkedSource(@"
public class C
{
    public int field = M(out int <N:0>x</N:0>) + x + M(out int <N:1>y</N:1>);
    static int M(out int x) => throw null;
}");
            var source1 = MarkedSource(@"
public class C
{
    public int field = M(out int <N:0>x</N:0>) + x;
    static int M(out int x) => throw null;
}");

            var compilation0 = CreateCompilation(source0.Tree, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);
            var compilation2 = compilation1.WithSource(source0.Tree);

            var ctor0 = compilation0.GetMember<MethodSymbol>("C..ctor");
            var ctor1 = compilation1.GetMember<MethodSymbol>("C..ctor");
            var ctor2 = compilation2.GetMember<MethodSymbol>("C..ctor");

            var v0 = CompileAndVerify(compilation0);
            v0.VerifyIL("C..ctor", @"
{
  // Code size       31 (0x1f)
  .maxstack  3
  .locals init (int V_0, //x
                int V_1) //y
  IL_0000:  ldarg.0
  IL_0001:  ldloca.s   V_0
  IL_0003:  call       ""int C.M(out int)""
  IL_0008:  ldloc.0
  IL_0009:  add
  IL_000a:  ldloca.s   V_1
  IL_000c:  call       ""int C.M(out int)""
  IL_0011:  add
  IL_0012:  stfld      ""int C.field""
  IL_0017:  ldarg.0
  IL_0018:  call       ""object..ctor()""
  IL_001d:  nop
  IL_001e:  ret
}
");

            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, ctor0, ctor1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            diff1.VerifyIL("C..ctor", @"
{
  // Code size       23 (0x17)
  .maxstack  3
  .locals init (int V_0, //x
                [int] V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldloca.s   V_0
  IL_0003:  call       ""int C.M(out int)""
  IL_0008:  ldloc.0
  IL_0009:  add
  IL_000a:  stfld      ""int C.field""
  IL_000f:  ldarg.0
  IL_0010:  call       ""object..ctor()""
  IL_0015:  nop
  IL_0016:  ret
}
");

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, ctor1, ctor2, GetSyntaxMapFromMarkers(source1, source0), preserveLocalVariables: true)));

            diff2.VerifyIL("C..ctor", @"
{
  // Code size       31 (0x1f)
  .maxstack  3
  .locals init (int V_0, //x
                [int] V_1,
                int V_2) //y
  IL_0000:  ldarg.0
  IL_0001:  ldloca.s   V_0
  IL_0003:  call       ""int C.M(out int)""
  IL_0008:  ldloc.0
  IL_0009:  add
  IL_000a:  ldloca.s   V_2
  IL_000c:  call       ""int C.M(out int)""
  IL_0011:  add
  IL_0012:  stfld      ""int C.field""
  IL_0017:  ldarg.0
  IL_0018:  call       ""object..ctor()""
  IL_001d:  nop
  IL_001e:  ret
}
");
        }

        [Fact]
        public void OutVar_InFieldInitializer_WithLambda()
        {
            var source0 = MarkedSource(@"
public class C
{
    int field = <N:0>M(out int <N:1>x</N:1>) + M2(<N:2>() => x + 1</N:2>)</N:0>;
    static int M(out int x) => throw null;
    static int M2(System.Func<int> x) => throw null;
}");
            var source1 = MarkedSource(@"
public class C
{
    int field = <N:0>M(out int <N:1>x</N:1>) + M2(<N:2>() => x - 1</N:2>)</N:0>;
    static int M(out int x) => throw null;
    static int M2(System.Func<int> x) => throw null;
}");

            var compilation0 = CreateCompilation(source0.Tree, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);
            var compilation2 = compilation1.WithSource(source0.Tree);

            var ctor0 = compilation0.GetMember<MethodSymbol>("C..ctor");
            var ctor1 = compilation1.GetMember<MethodSymbol>("C..ctor");
            var ctor2 = compilation2.GetMember<MethodSymbol>("C..ctor");

            var v0 = CompileAndVerify(compilation0);
            v0.VerifyIL("C..ctor", @"
{
  // Code size       49 (0x31)
  .maxstack  4
  .locals init (C.<>c__DisplayClass3_0 V_0) //CS$<>8__locals0
  IL_0000:  newobj     ""C.<>c__DisplayClass3_0..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldarg.0
  IL_0007:  ldloc.0
  IL_0008:  ldflda     ""int C.<>c__DisplayClass3_0.x""
  IL_000d:  call       ""int C.M(out int)""
  IL_0012:  ldloc.0
  IL_0013:  ldftn      ""int C.<>c__DisplayClass3_0.<.ctor>b__0()""
  IL_0019:  newobj     ""System.Func<int>..ctor(object, System.IntPtr)""
  IL_001e:  call       ""int C.M2(System.Func<int>)""
  IL_0023:  add
  IL_0024:  stfld      ""int C.field""
  IL_0029:  ldarg.0
  IL_002a:  call       ""object..ctor()""
  IL_002f:  nop
  IL_0030:  ret
}
");
            v0.VerifyIL("C.<>c__DisplayClass3_0.<.ctor>b__0()", @"
{
  // Code size        9 (0x9)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<>c__DisplayClass3_0.x""
  IL_0006:  ldc.i4.1
  IL_0007:  add
  IL_0008:  ret
}
");

            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, ctor0, ctor1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            diff1.VerifySynthesizedMembers(
                "C.<>c__DisplayClass3_0: {x, <.ctor>b__0}",
                "C: {<>c__DisplayClass3_0}");

            diff1.VerifyIL("C..ctor", @"
{
  // Code size       49 (0x31)
  .maxstack  4
  .locals init (C.<>c__DisplayClass3_0 V_0) //CS$<>8__locals0
  IL_0000:  newobj     ""C.<>c__DisplayClass3_0..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldarg.0
  IL_0007:  ldloc.0
  IL_0008:  ldflda     ""int C.<>c__DisplayClass3_0.x""
  IL_000d:  call       ""int C.M(out int)""
  IL_0012:  ldloc.0
  IL_0013:  ldftn      ""int C.<>c__DisplayClass3_0.<.ctor>b__0()""
  IL_0019:  newobj     ""System.Func<int>..ctor(object, System.IntPtr)""
  IL_001e:  call       ""int C.M2(System.Func<int>)""
  IL_0023:  add
  IL_0024:  stfld      ""int C.field""
  IL_0029:  ldarg.0
  IL_002a:  call       ""object..ctor()""
  IL_002f:  nop
  IL_0030:  ret
}
");
            diff1.VerifyIL("C.<>c__DisplayClass3_0.<.ctor>b__0()", @"
{
  // Code size        9 (0x9)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<>c__DisplayClass3_0.x""
  IL_0006:  ldc.i4.1
  IL_0007:  sub
  IL_0008:  ret
}
");

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, ctor1, ctor2, GetSyntaxMapFromMarkers(source1, source0), preserveLocalVariables: true)));

            diff2.VerifySynthesizedMembers(
                "C.<>c__DisplayClass3_0: {x, <.ctor>b__0}",
                "C: {<>c__DisplayClass3_0}");

            diff2.VerifyIL("C..ctor", @"
{
  // Code size       49 (0x31)
  .maxstack  4
  .locals init (C.<>c__DisplayClass3_0 V_0) //CS$<>8__locals0
  IL_0000:  newobj     ""C.<>c__DisplayClass3_0..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldarg.0
  IL_0007:  ldloc.0
  IL_0008:  ldflda     ""int C.<>c__DisplayClass3_0.x""
  IL_000d:  call       ""int C.M(out int)""
  IL_0012:  ldloc.0
  IL_0013:  ldftn      ""int C.<>c__DisplayClass3_0.<.ctor>b__0()""
  IL_0019:  newobj     ""System.Func<int>..ctor(object, System.IntPtr)""
  IL_001e:  call       ""int C.M2(System.Func<int>)""
  IL_0023:  add
  IL_0024:  stfld      ""int C.field""
  IL_0029:  ldarg.0
  IL_002a:  call       ""object..ctor()""
  IL_002f:  nop
  IL_0030:  ret
}
");
            diff2.VerifyIL("C.<>c__DisplayClass3_0.<.ctor>b__0()", @"
{
  // Code size        9 (0x9)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<>c__DisplayClass3_0.x""
  IL_0006:  ldc.i4.1
  IL_0007:  add
  IL_0008:  ret
}
");
        }

        [Fact]
        public void OutVar_InQuery()
        {
            var source0 = MarkedSource(@"
using System.Linq;
public class Program
{
    static int M(int x, out int y) { y = 42; return 43; }
    static void N()
    {
        var <N:0>query =
            from a in new int[] { 1, 2 }
            <N:1>select M(a, out int <N:2>x</N:2>) + x + M(a, out int <N:3>y</N:3></N:1>)</N:0>;
    }
}");
            var source1 = MarkedSource(@"
using System.Linq;
public class Program
{
    static int M(int x, out int y) { y = 42; return 43; }
    static void N()
    {
        var <N:0>query =
            from a in new int[] { 1, 2 }
            <N:1>select M(a, out int <N:2>x</N:2>) + x</N:1></N:0>;
    }
}");
            var compilation0 = CreateCompilation(source0.Tree, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);
            var compilation2 = compilation1.WithSource(source0.Tree);

            var n0 = compilation0.GetMember<MethodSymbol>("Program.N");
            var n1 = compilation1.GetMember<MethodSymbol>("Program.N");
            var n2 = compilation2.GetMember<MethodSymbol>("Program.N");

            var v0 = CompileAndVerify(compilation0);
            v0.VerifyIL("Program.N()", @"
{
  // Code size       53 (0x35)
  .maxstack  4
  .locals init (System.Collections.Generic.IEnumerable<int> V_0) //query
  IL_0000:  nop
  IL_0001:  ldc.i4.2
  IL_0002:  newarr     ""int""
  IL_0007:  dup
  IL_0008:  ldc.i4.0
  IL_0009:  ldc.i4.1
  IL_000a:  stelem.i4
  IL_000b:  dup
  IL_000c:  ldc.i4.1
  IL_000d:  ldc.i4.2
  IL_000e:  stelem.i4
  IL_000f:  ldsfld     ""System.Func<int, int> Program.<>c.<>9__1_0""
  IL_0014:  dup
  IL_0015:  brtrue.s   IL_002e
  IL_0017:  pop
  IL_0018:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_001d:  ldftn      ""int Program.<>c.<N>b__1_0(int)""
  IL_0023:  newobj     ""System.Func<int, int>..ctor(object, System.IntPtr)""
  IL_0028:  dup
  IL_0029:  stsfld     ""System.Func<int, int> Program.<>c.<>9__1_0""
  IL_002e:  call       ""System.Collections.Generic.IEnumerable<int> System.Linq.Enumerable.Select<int, int>(System.Collections.Generic.IEnumerable<int>, System.Func<int, int>)""
  IL_0033:  stloc.0
  IL_0034:  ret
}
");
            v0.VerifyIL("Program.<>c.<N>b__1_0(int)", @"
{
  // Code size       20 (0x14)
  .maxstack  3
  .locals init (int V_0, //x
                int V_1) //y
  IL_0000:  ldarg.1
  IL_0001:  ldloca.s   V_0
  IL_0003:  call       ""int Program.M(int, out int)""
  IL_0008:  ldloc.0
  IL_0009:  add
  IL_000a:  ldarg.1
  IL_000b:  ldloca.s   V_1
  IL_000d:  call       ""int Program.M(int, out int)""
  IL_0012:  add
  IL_0013:  ret
}
");

            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, n0, n1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            diff1.VerifySynthesizedMembers(
                "Program: {<>c}",
                "Program.<>c: {<>9__1_0, <N>b__1_0}");

            diff1.VerifyIL("Program.N()", @"
{
  // Code size       53 (0x35)
  .maxstack  4
  .locals init (System.Collections.Generic.IEnumerable<int> V_0) //query
  IL_0000:  nop
  IL_0001:  ldc.i4.2
  IL_0002:  newarr     ""int""
  IL_0007:  dup
  IL_0008:  ldc.i4.0
  IL_0009:  ldc.i4.1
  IL_000a:  stelem.i4
  IL_000b:  dup
  IL_000c:  ldc.i4.1
  IL_000d:  ldc.i4.2
  IL_000e:  stelem.i4
  IL_000f:  ldsfld     ""System.Func<int, int> Program.<>c.<>9__1_0""
  IL_0014:  dup
  IL_0015:  brtrue.s   IL_002e
  IL_0017:  pop
  IL_0018:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_001d:  ldftn      ""int Program.<>c.<N>b__1_0(int)""
  IL_0023:  newobj     ""System.Func<int, int>..ctor(object, System.IntPtr)""
  IL_0028:  dup
  IL_0029:  stsfld     ""System.Func<int, int> Program.<>c.<>9__1_0""
  IL_002e:  call       ""System.Collections.Generic.IEnumerable<int> System.Linq.Enumerable.Select<int, int>(System.Collections.Generic.IEnumerable<int>, System.Func<int, int>)""
  IL_0033:  stloc.0
  IL_0034:  ret
}
");
            diff1.VerifyIL("Program.<>c.<N>b__1_0(int)", @"
{
  // Code size       11 (0xb)
  .maxstack  2
  .locals init (int V_0, //x
                [int] V_1)
  IL_0000:  ldarg.1
  IL_0001:  ldloca.s   V_0
  IL_0003:  call       ""int Program.M(int, out int)""
  IL_0008:  ldloc.0
  IL_0009:  add
  IL_000a:  ret
}
");

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, n1, n2, GetSyntaxMapFromMarkers(source1, source0), preserveLocalVariables: true)));

            diff2.VerifySynthesizedMembers(
                "Program: {<>c}",
                "Program.<>c: {<>9__1_0, <N>b__1_0}");

            diff2.VerifyIL("Program.N()", @"
{
  // Code size       53 (0x35)
  .maxstack  4
  .locals init (System.Collections.Generic.IEnumerable<int> V_0) //query
  IL_0000:  nop
  IL_0001:  ldc.i4.2
  IL_0002:  newarr     ""int""
  IL_0007:  dup
  IL_0008:  ldc.i4.0
  IL_0009:  ldc.i4.1
  IL_000a:  stelem.i4
  IL_000b:  dup
  IL_000c:  ldc.i4.1
  IL_000d:  ldc.i4.2
  IL_000e:  stelem.i4
  IL_000f:  ldsfld     ""System.Func<int, int> Program.<>c.<>9__1_0""
  IL_0014:  dup
  IL_0015:  brtrue.s   IL_002e
  IL_0017:  pop
  IL_0018:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_001d:  ldftn      ""int Program.<>c.<N>b__1_0(int)""
  IL_0023:  newobj     ""System.Func<int, int>..ctor(object, System.IntPtr)""
  IL_0028:  dup
  IL_0029:  stsfld     ""System.Func<int, int> Program.<>c.<>9__1_0""
  IL_002e:  call       ""System.Collections.Generic.IEnumerable<int> System.Linq.Enumerable.Select<int, int>(System.Collections.Generic.IEnumerable<int>, System.Func<int, int>)""
  IL_0033:  stloc.0
  IL_0034:  ret
}
");
            diff2.VerifyIL("Program.<>c.<N>b__1_0(int)", @"
{
  // Code size       20 (0x14)
  .maxstack  3
  .locals init (int V_0, //x
                [int] V_1,
                int V_2) //y
  IL_0000:  ldarg.1
  IL_0001:  ldloca.s   V_0
  IL_0003:  call       ""int Program.M(int, out int)""
  IL_0008:  ldloc.0
  IL_0009:  add
  IL_000a:  ldarg.1
  IL_000b:  ldloca.s   V_2
  IL_000d:  call       ""int Program.M(int, out int)""
  IL_0012:  add
  IL_0013:  ret
}
");
        }

        [Fact]
        public void OutVar_InQuery_WithLambda()
        {
            var source0 = MarkedSource(@"
using System.Linq;
public class Program
{
    static int M(int x, out int y) { y = 42; return 43; }
    static int M2(System.Func<int> x) => throw null;
    static void N()
    {
        var <N:0>query =
            from a in new int[] { 1, 2 }
            <N:1>select <N:2>M(a, out int <N:3>x</N:3>) + M2(<N:4>() => x + 1</N:4>)</N:2></N:1></N:0>;
    }
}");
            var source1 = MarkedSource(@"
using System.Linq;
public class Program
{
    static int M(int x, out int y) { y = 42; return 43; }
    static int M2(System.Func<int> x) => throw null;
    static void N()
    {
        var <N:0>query =
            from a in new int[] { 1, 2 }
            <N:1>select <N:2>M(a, out int <N:3>x</N:3>) + M2(<N:4>() => x - 1</N:4>)</N:2></N:1></N:0>;
    }
}");
            var compilation0 = CreateCompilation(source0.Tree, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);
            var compilation2 = compilation1.WithSource(source0.Tree);

            var n0 = compilation0.GetMember<MethodSymbol>("Program.N");
            var n1 = compilation1.GetMember<MethodSymbol>("Program.N");
            var n2 = compilation2.GetMember<MethodSymbol>("Program.N");

            var v0 = CompileAndVerify(compilation0);
            v0.VerifyIL("Program.N()", @"
{
  // Code size       53 (0x35)
  .maxstack  4
  .locals init (System.Collections.Generic.IEnumerable<int> V_0) //query
  IL_0000:  nop
  IL_0001:  ldc.i4.2
  IL_0002:  newarr     ""int""
  IL_0007:  dup
  IL_0008:  ldc.i4.0
  IL_0009:  ldc.i4.1
  IL_000a:  stelem.i4
  IL_000b:  dup
  IL_000c:  ldc.i4.1
  IL_000d:  ldc.i4.2
  IL_000e:  stelem.i4
  IL_000f:  ldsfld     ""System.Func<int, int> Program.<>c.<>9__2_0""
  IL_0014:  dup
  IL_0015:  brtrue.s   IL_002e
  IL_0017:  pop
  IL_0018:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_001d:  ldftn      ""int Program.<>c.<N>b__2_0(int)""
  IL_0023:  newobj     ""System.Func<int, int>..ctor(object, System.IntPtr)""
  IL_0028:  dup
  IL_0029:  stsfld     ""System.Func<int, int> Program.<>c.<>9__2_0""
  IL_002e:  call       ""System.Collections.Generic.IEnumerable<int> System.Linq.Enumerable.Select<int, int>(System.Collections.Generic.IEnumerable<int>, System.Func<int, int>)""
  IL_0033:  stloc.0
  IL_0034:  ret
}
");
            v0.VerifyIL("Program.<>c.<N>b__2_0(int)", @"
{
  // Code size       37 (0x25)
  .maxstack  3
  .locals init (Program.<>c__DisplayClass2_0 V_0) //CS$<>8__locals0
  IL_0000:  newobj     ""Program.<>c__DisplayClass2_0..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldarg.1
  IL_0007:  ldloc.0
  IL_0008:  ldflda     ""int Program.<>c__DisplayClass2_0.x""
  IL_000d:  call       ""int Program.M(int, out int)""
  IL_0012:  ldloc.0
  IL_0013:  ldftn      ""int Program.<>c__DisplayClass2_0.<N>b__1()""
  IL_0019:  newobj     ""System.Func<int>..ctor(object, System.IntPtr)""
  IL_001e:  call       ""int Program.M2(System.Func<int>)""
  IL_0023:  add
  IL_0024:  ret
}
");
            v0.VerifyIL("Program.<>c__DisplayClass2_0.<N>b__1()", @"
{
  // Code size        9 (0x9)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<>c__DisplayClass2_0.x""
  IL_0006:  ldc.i4.1
  IL_0007:  add
  IL_0008:  ret
}
");

            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, n0, n1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            diff1.VerifySynthesizedMembers(
                "Program: {<>c__DisplayClass2_0, <>c}",
                "Program.<>c__DisplayClass2_0: {x, <N>b__1}",
                "Program.<>c: {<>9__2_0, <N>b__2_0}");

            diff1.VerifyIL("Program.N()", @"
{
  // Code size       53 (0x35)
  .maxstack  4
  .locals init (System.Collections.Generic.IEnumerable<int> V_0) //query
  IL_0000:  nop
  IL_0001:  ldc.i4.2
  IL_0002:  newarr     ""int""
  IL_0007:  dup
  IL_0008:  ldc.i4.0
  IL_0009:  ldc.i4.1
  IL_000a:  stelem.i4
  IL_000b:  dup
  IL_000c:  ldc.i4.1
  IL_000d:  ldc.i4.2
  IL_000e:  stelem.i4
  IL_000f:  ldsfld     ""System.Func<int, int> Program.<>c.<>9__2_0""
  IL_0014:  dup
  IL_0015:  brtrue.s   IL_002e
  IL_0017:  pop
  IL_0018:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_001d:  ldftn      ""int Program.<>c.<N>b__2_0(int)""
  IL_0023:  newobj     ""System.Func<int, int>..ctor(object, System.IntPtr)""
  IL_0028:  dup
  IL_0029:  stsfld     ""System.Func<int, int> Program.<>c.<>9__2_0""
  IL_002e:  call       ""System.Collections.Generic.IEnumerable<int> System.Linq.Enumerable.Select<int, int>(System.Collections.Generic.IEnumerable<int>, System.Func<int, int>)""
  IL_0033:  stloc.0
  IL_0034:  ret
}
");
            diff1.VerifyIL("Program.<>c.<N>b__2_0(int)", @"
{
  // Code size       37 (0x25)
  .maxstack  3
  .locals init (Program.<>c__DisplayClass2_0 V_0) //CS$<>8__locals0
  IL_0000:  newobj     ""Program.<>c__DisplayClass2_0..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldarg.1
  IL_0007:  ldloc.0
  IL_0008:  ldflda     ""int Program.<>c__DisplayClass2_0.x""
  IL_000d:  call       ""int Program.M(int, out int)""
  IL_0012:  ldloc.0
  IL_0013:  ldftn      ""int Program.<>c__DisplayClass2_0.<N>b__1()""
  IL_0019:  newobj     ""System.Func<int>..ctor(object, System.IntPtr)""
  IL_001e:  call       ""int Program.M2(System.Func<int>)""
  IL_0023:  add
  IL_0024:  ret
}
");
            diff1.VerifyIL("Program.<>c__DisplayClass2_0.<N>b__1()", @"
{
  // Code size        9 (0x9)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<>c__DisplayClass2_0.x""
  IL_0006:  ldc.i4.1
  IL_0007:  sub
  IL_0008:  ret
}
");

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, n1, n2, GetSyntaxMapFromMarkers(source1, source0), preserveLocalVariables: true)));

            diff2.VerifySynthesizedMembers(
                "Program.<>c__DisplayClass2_0: {x, <N>b__1}",
                "Program: {<>c__DisplayClass2_0, <>c}",
                "Program.<>c: {<>9__2_0, <N>b__2_0}");

            diff2.VerifyIL("Program.N()", @"
{
  // Code size       53 (0x35)
  .maxstack  4
  .locals init (System.Collections.Generic.IEnumerable<int> V_0) //query
  IL_0000:  nop
  IL_0001:  ldc.i4.2
  IL_0002:  newarr     ""int""
  IL_0007:  dup
  IL_0008:  ldc.i4.0
  IL_0009:  ldc.i4.1
  IL_000a:  stelem.i4
  IL_000b:  dup
  IL_000c:  ldc.i4.1
  IL_000d:  ldc.i4.2
  IL_000e:  stelem.i4
  IL_000f:  ldsfld     ""System.Func<int, int> Program.<>c.<>9__2_0""
  IL_0014:  dup
  IL_0015:  brtrue.s   IL_002e
  IL_0017:  pop
  IL_0018:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_001d:  ldftn      ""int Program.<>c.<N>b__2_0(int)""
  IL_0023:  newobj     ""System.Func<int, int>..ctor(object, System.IntPtr)""
  IL_0028:  dup
  IL_0029:  stsfld     ""System.Func<int, int> Program.<>c.<>9__2_0""
  IL_002e:  call       ""System.Collections.Generic.IEnumerable<int> System.Linq.Enumerable.Select<int, int>(System.Collections.Generic.IEnumerable<int>, System.Func<int, int>)""
  IL_0033:  stloc.0
  IL_0034:  ret
}
");
            diff2.VerifyIL("Program.<>c.<N>b__2_0(int)", @"
{
  // Code size       37 (0x25)
  .maxstack  3
  .locals init (Program.<>c__DisplayClass2_0 V_0) //CS$<>8__locals0
  IL_0000:  newobj     ""Program.<>c__DisplayClass2_0..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldarg.1
  IL_0007:  ldloc.0
  IL_0008:  ldflda     ""int Program.<>c__DisplayClass2_0.x""
  IL_000d:  call       ""int Program.M(int, out int)""
  IL_0012:  ldloc.0
  IL_0013:  ldftn      ""int Program.<>c__DisplayClass2_0.<N>b__1()""
  IL_0019:  newobj     ""System.Func<int>..ctor(object, System.IntPtr)""
  IL_001e:  call       ""int Program.M2(System.Func<int>)""
  IL_0023:  add
  IL_0024:  ret
}
");
            diff2.VerifyIL("Program.<>c__DisplayClass2_0.<N>b__1()", @"
{
  // Code size        9 (0x9)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<>c__DisplayClass2_0.x""
  IL_0006:  ldc.i4.1
  IL_0007:  add
  IL_0008:  ret
}
");
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), AlwaysSkip = "https://github.com/dotnet/roslyn/issues/37047")]
        public void OutVar_InSwitchExpression()
        {
            var source0 = MarkedSource(@"
public class Program
{
    static object G(int i)
    {
        return i switch 
        {
            0 => 0,
            _ => 1
        };
    }

    static object N(out int x) { x = 1; return null; }
}");
            var source1 = MarkedSource(@"
public class Program
{
    static object G(int i)
    {
        return i + N(out var x) switch 
        {
            0 => 0,
            _ => 1
        };
    }

    static int N(out int x) { x = 1; return 0; }
}");
            var compilation0 = CreateCompilation(source0.Tree, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);
            var compilation2 = compilation1.WithSource(source0.Tree);

            var n0 = compilation0.GetMember<MethodSymbol>("Program.G");
            var n1 = compilation1.GetMember<MethodSymbol>("Program.G");
            var n2 = compilation2.GetMember<MethodSymbol>("Program.G");

            var v0 = CompileAndVerify(compilation0);
            v0.VerifyIL("Program.G(int)", @"
{
  // Code size       27 (0x1b)
  .maxstack  1
  .locals init (int V_0,
                int V_1,
                object V_2)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  brfalse.s  IL_0006
  IL_0004:  br.s       IL_000a
  IL_0006:  ldc.i4.0
  IL_0007:  stloc.0
  IL_0008:  br.s       IL_000e
  IL_000a:  ldc.i4.1
  IL_000b:  stloc.0
  IL_000c:  br.s       IL_000e
  IL_000e:  ldloc.0
  IL_000f:  stloc.1
  IL_0010:  ldloc.1
  IL_0011:  box        ""int""
  IL_0016:  stloc.2
  IL_0017:  br.s       IL_0019
  IL_0019:  ldloc.2
  IL_001a:  ret
}
");
            v0.VerifyIL("Program.N(out int)", @"
{
  // Code size       10 (0xa)
  .maxstack  2
  .locals init (object V_0)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldc.i4.1
  IL_0003:  stind.i4
  IL_0004:  ldnull
  IL_0005:  stloc.0
  IL_0006:  br.s       IL_0008
  IL_0008:  ldloc.0
  IL_0009:  ret
}
");

            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, n0, n1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            diff1.VerifySynthesizedMembers();

            diff1.VerifyIL("Program.G(int)", @"
{
  // Code size       50 (0x32)
  .maxstack  2
  .locals init ([int] V_0,
                [int] V_1,
                [object] V_2,
                int V_3, //x
                int V_4,
                int V_5,
                int V_6,
                int V_7,
                object V_8)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.s    V_4
  IL_0004:  ldloca.s   V_3
  IL_0006:  call       ""int Program.N(out int)""
  IL_000b:  stloc.s    V_6
  IL_000d:  ldloc.s    V_6
  IL_000f:  brfalse.s  IL_0013
  IL_0011:  br.s       IL_0018
  IL_0013:  ldc.i4.0
  IL_0014:  stloc.s    V_5
  IL_0016:  br.s       IL_001d
  IL_0018:  ldc.i4.1
  IL_0019:  stloc.s    V_5
  IL_001b:  br.s       IL_001d
  IL_001d:  ldloc.s    V_5
  IL_001f:  stloc.s    V_7
  IL_0021:  ldloc.s    V_4
  IL_0023:  ldloc.s    V_7
  IL_0025:  add
  IL_0026:  box        ""int""
  IL_002b:  stloc.s    V_8
  IL_002d:  br.s       IL_002f
  IL_002f:  ldloc.s    V_8
  IL_0031:  ret
}
");
            diff1.VerifyIL("Program.N(out int)", @"
{
  // Code size       10 (0xa)
  .maxstack  2
  .locals init (int V_0)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldc.i4.1
  IL_0003:  stind.i4
  IL_0004:  ldc.i4.0
  IL_0005:  stloc.0
  IL_0006:  br.s       IL_0008
  IL_0008:  ldloc.0
  IL_0009:  ret
}
");

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, n1, n2, GetSyntaxMapFromMarkers(source1, source0), preserveLocalVariables: true)));

            diff2.VerifySynthesizedMembers();

            diff2.VerifyIL("Program.G(int)", @"
{
  // Code size       53 (0x35)
  .maxstack  4
  .locals init (System.Collections.Generic.IEnumerable<int> V_0) //query
  IL_0000:  nop
  IL_0001:  ldc.i4.2
  IL_0002:  newarr     ""int""
  IL_0007:  dup
  IL_0008:  ldc.i4.0
  IL_0009:  ldc.i4.1
  IL_000a:  stelem.i4
  IL_000b:  dup
  IL_000c:  ldc.i4.1
  IL_000d:  ldc.i4.2
  IL_000e:  stelem.i4
  IL_000f:  ldsfld     ""System.Func<int, int> Program.<>c.<>9__1_0""
  IL_0014:  dup
  IL_0015:  brtrue.s   IL_002e
  IL_0017:  pop
  IL_0018:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_001d:  ldftn      ""int Program.<>c.<N>b__1_0(int)""
  IL_0023:  newobj     ""System.Func<int, int>..ctor(object, System.IntPtr)""
  IL_0028:  dup
  IL_0029:  stsfld     ""System.Func<int, int> Program.<>c.<>9__1_0""
  IL_002e:  call       ""System.Collections.Generic.IEnumerable<int> System.Linq.Enumerable.Select<int, int>(System.Collections.Generic.IEnumerable<int>, System.Func<int, int>)""
  IL_0033:  stloc.0
  IL_0034:  ret
}
");
            diff2.VerifyIL("Program.N(out int)", @"
{
  // Code size       20 (0x14)
  .maxstack  3
  .locals init (int V_0, //x
                [int] V_1,
                int V_2) //y
  IL_0000:  ldarg.1
  IL_0001:  ldloca.s   V_0
  IL_0003:  call       ""int Program.M(int, out int)""
  IL_0008:  ldloc.0
  IL_0009:  add
  IL_000a:  ldarg.1
  IL_000b:  ldloca.s   V_2
  IL_000d:  call       ""int Program.M(int, out int)""
  IL_0012:  add
  IL_0013:  ret
}
");
        }
    }
}
