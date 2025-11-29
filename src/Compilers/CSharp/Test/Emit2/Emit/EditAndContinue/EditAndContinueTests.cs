// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading;
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
        public void Constructor_Delete_WithParameterless()
        {
            using var _ = new EditAndContinueTest()
                .AddBaseline(
                    source: $$"""
                        class C
                        {
                            public C()
                            {
                            }

                            public C(int x)
                            {
                            }
                        }
                        """,
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("<Module>", "C");
                        g.VerifyMethodDefNames(".ctor", ".ctor");
                    })

                .AddGeneration(
                    source: """
                        class C
                        {
                            public C()
                            {
                            }
                        }
                        """,
                    edits: new[] {
                        Edit(SemanticEditKind.Delete, symbolProvider: c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.FirstOrDefault(c => c.Parameters.Length == 1), newSymbolProvider: c => c.GetMember("C")),
                    },
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("HotReloadException");

                        g.VerifyMethodDefs(
                            (".ctor", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName),
                            (".ctor", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName));

                        g.VerifyTypeRefNames("Object", "CompilerGeneratedAttribute", "Exception", "Action`1");
                        g.VerifyMemberRefNames(".ctor", ".ctor", "Invoke");

                        g.VerifyEncLogDefinitions(new[]
                        {
                            Row(1, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                            Row(3, TableIndex.TypeDef, EditAndContinueOperation.Default),
                            Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(1, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(2, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                            Row(3, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(4, TableIndex.CustomAttribute, EditAndContinueOperation.Default)
                        });

                        g.VerifyEncMapDefinitions(new[]
                        {
                            Handle(3, TableIndex.TypeDef),
                            Handle(1, TableIndex.Field),
                            Handle(2, TableIndex.Field),
                            Handle(2, TableIndex.MethodDef),
                            Handle(3, TableIndex.MethodDef),
                            Handle(4, TableIndex.CustomAttribute),
                            Handle(1, TableIndex.StandAloneSig)
                        });

                        g.VerifyIL("""
                            .ctor
                            {
                              // Code size       13 (0xd)
                              .maxstack  8
                              IL_0000:  ldstr      0x70000005
                              IL_0005:  ldc.i4.s   -2
                              IL_0007:  newobj     0x06000003
                              IL_000c:  throw
                            }
                            .ctor
                            {
                              // Code size       33 (0x21)
                              .maxstack  2
                              IL_0000:  ldarg.0
                              IL_0001:  ldarg.1
                              IL_0002:  call       0x0A000006
                              IL_0007:  nop
                              IL_0008:  ldarg.0
                              IL_0009:  ldarg.2
                              IL_000a:  stfld      0x04000001
                              IL_000f:  ldsfld     0x04000002
                              IL_0014:  dup
                              IL_0015:  stloc.0
                              IL_0016:  brfalse.s  IL_0020
                              IL_0018:  ldloc.0
                              IL_0019:  ldarg.0
                              IL_001a:  callvirt   0x0A000007
                              IL_001f:  nop
                              IL_0020:  ret
                            }
                            """);
                    })
                .Verify();
        }

        [Fact]
        public void Constructor_Delete()
        {
            using var _ = new EditAndContinueTest()
                .AddBaseline(
                    source: $$"""
                        class C
                        {
                            public C(int x)
                            {
                            }
                        }
                        """,
                    validator: g =>
                    {
                        g.VerifyMethodDefNames(".ctor");
                    })

                .AddGeneration(
                    source: """
                        class C
                        {
                        }
                        """,
                    edits: new[]
                    {
                        Edit(SemanticEditKind.Delete, symbolProvider: c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.FirstOrDefault(c => c.Parameters.Length == 1), newSymbolProvider: c => c.GetMember("C")),
                    },
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("HotReloadException");

                        // The default constructor is added and the deleted constructor is updated to throw:
                        g.VerifyMethodDefNames(".ctor", ".ctor", ".ctor");
                        g.VerifyTypeRefNames("Object", "CompilerGeneratedAttribute", "Exception", "Action`1");

                        g.VerifyEncLogDefinitions(new[]
                        {
                            Row(1, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                            Row(3, TableIndex.TypeDef, EditAndContinueOperation.Default),
                            Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(1, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(2, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(2, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                            Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                            Row(3, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(4, TableIndex.CustomAttribute, EditAndContinueOperation.Default)
                        });

                        g.VerifyEncMapDefinitions(new[]
                        {
                            Handle(3, TableIndex.TypeDef),
                            Handle(1, TableIndex.Field),
                            Handle(2, TableIndex.Field),
                            Handle(1, TableIndex.MethodDef),
                            Handle(2, TableIndex.MethodDef),
                            Handle(3, TableIndex.MethodDef),
                            Handle(4, TableIndex.CustomAttribute),
                            Handle(1, TableIndex.StandAloneSig)
                        });

                        g.VerifyIL("""
                            .ctor
                            {
                              // Code size       13 (0xd)
                              .maxstack  8
                              IL_0000:  ldstr      0x70000005
                              IL_0005:  ldc.i4.s   -2
                              IL_0007:  newobj     0x06000003
                              IL_000c:  throw
                            }
                            .ctor
                            {
                              // Code size        8 (0x8)
                              .maxstack  8
                              IL_0000:  ldarg.0
                              IL_0001:  call       0x0A000006
                              IL_0006:  nop
                              IL_0007:  ret
                            }
                            .ctor
                            {
                              // Code size       33 (0x21)
                              .maxstack  2
                              IL_0000:  ldarg.0
                              IL_0001:  ldarg.1
                              IL_0002:  call       0x0A000007
                              IL_0007:  nop
                              IL_0008:  ldarg.0
                              IL_0009:  ldarg.2
                              IL_000a:  stfld      0x04000001
                              IL_000f:  ldsfld     0x04000002
                              IL_0014:  dup
                              IL_0015:  stloc.0
                              IL_0016:  brfalse.s  IL_0020
                              IL_0018:  ldloc.0
                              IL_0019:  ldarg.0
                              IL_001a:  callvirt   0x0A000008
                              IL_001f:  nop
                              IL_0020:  ret
                            }
                            """);
                    })
                .Verify();
        }

        [Fact]
        public void Diagnostics_Nullable()
        {
            var source0 = @"
class C
{
    string _str;
    C(int x) { _str = ""a""; }
    static string F() => ""a"";
}
class D
{
    string _str = ""a"";
}
[A(""a"")]
class A : System.Attribute
{
    public A(string x = ""a"") {}
}
";
            var source1 = @"
class C
{
    string _str;
    C(int x) { }
    static string F() => null;
}
class D
{
    string _str;
}
[A(null)]
class A : System.Attribute
{
    public A(string x = null) {}
}
";
            var compilation0 = CreateCompilation(source0, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), options: TestOptions.DebugDll.WithNullableContextOptions(NullableContextOptions.Enable));
            var compilation1 = compilation0.WithSource(source1);

            var ctorC0 = compilation0.GetMember<NamedTypeSymbol>("C").InstanceConstructors[0];
            var ctorC1 = compilation1.GetMember<NamedTypeSymbol>("C").InstanceConstructors[0];
            var ctorD0 = compilation0.GetMember<NamedTypeSymbol>("D").InstanceConstructors[0];
            var ctorD1 = compilation1.GetMember<NamedTypeSymbol>("D").InstanceConstructors[0];
            var method0 = compilation0.GetMember<MethodSymbol>("C.F");
            var method1 = compilation1.GetMember<MethodSymbol>("C.F");
            var a0 = compilation0.GetMember<NamedTypeSymbol>("A");
            var a1 = compilation1.GetMember<NamedTypeSymbol>("A");
            var ctorA0 = compilation0.GetMember<NamedTypeSymbol>("A").InstanceConstructors[0];
            var ctorA1 = compilation1.GetMember<NamedTypeSymbol>("A").InstanceConstructors[0];

            using var md0 = ModuleMetadata.CreateFromImage(compilation0.EmitToArray());

            var diff1 = compilation1.EmitDifference(
                CreateInitialBaseline(compilation0, md0, EmptyLocalsProvider),
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, ctorC0, ctorC1),
                    SemanticEdit.Create(SemanticEditKind.Update, ctorD0, ctorD1),
                    SemanticEdit.Create(SemanticEditKind.Update, method0, method1),
                    SemanticEdit.Create(SemanticEditKind.Update, a0, a1),
                    SemanticEdit.Create(SemanticEditKind.Update, ctorA0, ctorA1)));

            // Nullable diagnostics not reported, except for attribute and default parameter values. 
            // The compiler doesn't have the necessary emit context when analyzing these.
            diff1.EmitResult.Diagnostics.Verify(
                // (12,4): warning CS8625: Cannot convert null literal to non-nullable reference type.
                // [A(null)]
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(12, 4),
                // (15,25): warning CS8625: Cannot convert null literal to non-nullable reference type.
                //     public A(string x = null) {}
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(15, 25));
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
            var compilation0 = CreateCompilation(source0, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);

            var method0 = compilation0.GetMember<MethodSymbol>("C.F");
            var method1 = compilation1.GetMember<MethodSymbol>("C.F");

            var bytes0 = compilation0.EmitToArray();
            using var md0 = ModuleMetadata.CreateFromImage(bytes0);
            var reader0 = md0.MetadataReader;

            var diff1 = compilation1.EmitDifference(
                CreateInitialBaseline(compilation0, md0, EmptyLocalsProvider),
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1)));

            using var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;

            var s = MetadataTokens.StringHandle(0);
            Assert.Equal("", reader1.GetString(s));

            var b = MetadataTokens.BlobHandle(0);
            Assert.Equal(0, reader1.GetBlobBytes(b).Length);

            var us = MetadataTokens.UserStringHandle(0);
            Assert.Equal("", reader1.GetUserString(us));
        }

        [Fact]
        public void Delta_AssemblyDefTable()
        {
            var source0 = @"public class C { public static void F() { System.Console.WriteLine(1); } }";
            var source1 = @"public class C { public static void F() { System.Console.WriteLine(2); } }";

            var compilation0 = CreateCompilationWithMscorlib461(source0, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");

            var v0 = CompileAndVerify(compilation0);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f0, f1)));

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
            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            // Semantic errors are reported only for the bodies of members being emitted.

            var diffError = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, e0, e1, GetSyntaxMapFromMarkers(source0, source1))));

            diffError.EmitResult.Diagnostics.Verify(
                // (6,17): error CS0103: The name 'Unknown' does not exist in the current context
                //         int x = Unknown(2);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "Unknown").WithArguments("Unknown").WithLocation(6, 17));

            var diffGood = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, g0, g1, GetSyntaxMapFromMarkers(source0, source1))));

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
            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, g0, g1, GetSyntaxMapFromMarkers(source0, source1))));

            // All declaration errors are reported regardless of what member do we emit.

            diff.EmitResult.Diagnostics.Verify(
                // (10,7): error CS0146: Circular base type dependency involving 'Bad' and 'Bad'
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
    static string F() { return ""abc""; }
}";
            var compilation0 = CreateCompilation(source0, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), options: TestOptions.DebugExe);
            var compilation1 = compilation0.WithSource(source1);

            var method0 = compilation0.GetMember<MethodSymbol>("C.F");

            // Verify full metadata contains expected rows.
            var bytes0 = compilation0.EmitToArray();
            using var md0 = ModuleMetadata.CreateFromImage(bytes0);
            var reader0 = md0.MetadataReader;

            CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "C");
            CheckNames(reader0, reader0.GetMethodDefNames(), "Main", "F", ".ctor");
            CheckNames(reader0, reader0.GetMemberRefNames(), /*CompilationRelaxationsAttribute.*/".ctor", /*RuntimeCompatibilityAttribute.*/".ctor", /*Object.*/".ctor", /*DebuggableAttribute*/".ctor");

            var generation0 = CreateInitialBaseline(compilation0,
                md0,
                EmptyLocalsProvider);
            var method1 = compilation1.GetMember<MethodSymbol>("C.F");

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1)));

            // Verify delta metadata contains expected rows.
            using var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;
            var readers = new[] { reader0, reader1 };

            EncValidation.VerifyModuleMvid(1, reader0, reader1);

            CheckNames(readers, reader1.GetTypeDefNames());
            CheckNames(readers, reader1.GetMethodDefNames(), "F");

            CheckEncLogDefinitions(reader1,
                Row(2, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default)); // C.F

            CheckEncMapDefinitions(reader1,
                Handle(2, TableIndex.MethodDef),
                Handle(2, TableIndex.StandAloneSig));
        }

        [Fact]
        public void ModifyMethod_RenameParameter()
        {
            var source0 =
@"class C
{
    static string F(int a) { return a.ToString(); }
}";
            var source1 =
@"class C
{
    static string F(int x) { return x.ToString(); }
}";
            var source2 =
@"class C
{
    static string F(int b) { return b.ToString(); }
}";

            var compilation0 = CreateCompilation(source0, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);
            var compilation2 = compilation1.WithSource(source2);

            var method0 = compilation0.GetMember<MethodSymbol>("C.F");

            // Verify full metadata contains expected rows.
            var bytes0 = compilation0.EmitToArray(EmitOptions.Default.WithDebugInformationFormat(DebugInformationFormat.PortablePdb));
            using var md0 = ModuleMetadata.CreateFromImage(bytes0);
            var reader0 = md0.MetadataReader;

            CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "C");
            CheckNames(reader0, reader0.GetMethodDefNames(), "F", ".ctor");
            CheckNames(reader0, reader0.GetParameterDefNames(), "a");

            var generation0 = CreateInitialBaseline(compilation0,
                md0,
                EmptyLocalsProvider);
            var method1 = compilation1.GetMember<MethodSymbol>("C.F");

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1)));

            // Verify delta metadata contains expected rows.
            using var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;
            var readers = new[] { reader0, reader1 };

            EncValidation.VerifyModuleMvid(1, reader0, reader1);

            CheckNames(readers, reader1.GetTypeDefNames());
            CheckNames(readers, reader1.GetMethodDefNames(), "F");
            CheckNames(readers, reader1.GetParameterDefNames(), "x");

            CheckEncLogDefinitions(reader1,
                Row(2, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(1, TableIndex.Param, EditAndContinueOperation.Default));

            CheckEncMapDefinitions(reader1,
                Handle(1, TableIndex.MethodDef),
                Handle(1, TableIndex.Param),
                Handle(2, TableIndex.StandAloneSig));

            var method2 = compilation2.GetMember<MethodSymbol>("C.F");

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method1, method2)));

            // Verify delta metadata contains expected rows.
            using var md2 = diff2.GetMetadata();
            var reader2 = md2.Reader;
            readers = new[] { reader0, reader1, reader2 };

            CheckNames(readers, reader2.GetTypeDefNames());
            CheckNames(readers, reader2.GetMethodDefNames(), "F");
            CheckNames(readers, reader2.GetMemberRefNames(), "ToString");
            CheckNames(readers, reader2.GetParameterDefNames(), "b");

            CheckEncLogDefinitions(reader2,
                Row(3, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(1, TableIndex.Param, EditAndContinueOperation.Default));

            CheckEncMapDefinitions(reader2,
                Handle(1, TableIndex.MethodDef),
                Handle(1, TableIndex.Param),
                Handle(3, TableIndex.StandAloneSig));
        }

        [Theory]
        [InlineData("in")]
        [InlineData("ref readonly")]
        public void ModifyMethod_ParameterModifiers_Ref_In_RefReadonly(string newModifier)
        {
            using var _ = new EditAndContinueTest()
                .AddBaseline(
                    source: """
                    class C
                    {
                        public int F(ref int x) => throw null;
                        public int G(in int y, ref readonly int z) => throw null;
                    }
                    """,
                    validator: g =>
                    {
                    })

                .AddGeneration(
                    source: $$"""
                    class C
                    {
                        public int F({{newModifier}} int x) => throw null;
                        public int G(in int y, ref readonly int z) => throw null;
                    }
                    """,
                    edits: new[]
                    {
                        Edit(SemanticEditKind.Update, c => c.GetMember("C.F")),
                    },
                    validator: g =>
                    {
                        g.VerifyTypeDefNames();
                        g.VerifyMethodDefNames("F");

                        g.VerifyEncLogDefinitions(new[]
                        {
                            Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(1, TableIndex.Param, EditAndContinueOperation.Default),
                            Row(12, TableIndex.CustomAttribute, EditAndContinueOperation.Default)
                        });
                        g.VerifyEncMapDefinitions(new[]
                        {
                            Handle(4, TableIndex.MethodDef),
                            Handle(1, TableIndex.Param),
                            Handle(12, TableIndex.CustomAttribute)
                        });
                    })
                .Verify();
        }

        [Fact]
        public void ModifyMethod_ParameterModifiers_Ref_Out()
        {
            using var _ = new EditAndContinueTest()
                .AddBaseline(
                    source: """
                    class C
                    {
                        public int F(ref int x) => throw null;
                    }
                    """,
                    validator: g =>
                    {
                    })

                .AddGeneration(
                    source: $$"""
                    class C
                    {
                        public int F(out int x) => throw null;
                    }
                    """,
                    edits: new[]
                    {
                        Edit(SemanticEditKind.Update, c => c.GetMember("C.F")),
                    },
                    validator: g =>
                    {
                        g.VerifyTypeDefNames();
                        g.VerifyMethodDefNames("F");

                        g.VerifyEncLogDefinitions(new[]
                        {
                            Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(1, TableIndex.Param, EditAndContinueOperation.Default)
                        });
                        g.VerifyEncMapDefinitions(new[]
                        {
                            Handle(1, TableIndex.MethodDef),
                            Handle(1, TableIndex.Param)
                        });
                    })
                .Verify();
        }

        [Fact]
        public void ModifyMethod_ParameterModifiers_RefReadOnly()
        {
            using var _ = new EditAndContinueTest()
                .AddBaseline(
                    source: """
                    class C
                    {
                        public int F(ref int x) => throw null;
                        public int G(ref readonly int x) => throw null;
                    }
                    """,
                    validator: g =>
                    {
                    })

                .AddGeneration(
                    source: $$"""
                    class C
                    {
                        public int F(ref readonly int x) => throw null;
                        public int G(ref readonly int x) => throw null;
                    }
                    """,
                    edits: new[]
                    {
                        Edit(SemanticEditKind.Update, c => c.GetMember("C.F")),
                    },
                    validator: g =>
                    {
                        g.VerifyTypeDefNames();
                        g.VerifyMethodDefNames("F");

                        g.VerifyEncLogDefinitions(new[]
                        {
                            Row(3, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(1, TableIndex.Param, EditAndContinueOperation.Default),
                            Row(9, TableIndex.CustomAttribute, EditAndContinueOperation.Default)
                        });
                        g.VerifyEncMapDefinitions(new[]
                        {
                            Handle(3, TableIndex.MethodDef),
                            Handle(1, TableIndex.Param),
                            Handle(9, TableIndex.CustomAttribute)
                        });
                    })
                .Verify();
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
            var compilation0 = CreateCompilation(source0, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), options: TestOptions.DebugExe, targetFramework: TargetFramework.NetStandard20);
            var compilation1 = compilation0.WithSource(source1);

            var bytes0 = compilation0.EmitToArray();
            using var md0 = ModuleMetadata.CreateFromImage(bytes0);
            var reader0 = md0.MetadataReader;

            var method0 = compilation0.GetMember<MethodSymbol>("C.F");
            var generation0 = CreateInitialBaseline(compilation0,
                md0,
                EmptyLocalsProvider);

            var method1 = compilation1.GetMember<MethodSymbol>("C.F");

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1)));

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

        [Fact]
        public void ModifyMethod_WithAttributes1()
        {
            var common = """
            using System;
            class A1 : Attribute { }
            class A2 : Attribute { }
            class A3 : Attribute { }
            class A4 : Attribute { }
            class A5 : Attribute { }
            class A6 : Attribute { }
            """;

            using var _ = new EditAndContinueTest(options: TestOptions.DebugDll, targetFramework: TargetFramework.NetStandard20)
                .AddBaseline(
                    source: common + """
                    class C
                    {
                        [A1]
                        static void F() { }
                    }
                    """,
                    validator: g =>
                    {
                        g.VerifyCustomAttributes(
                            "[System.Runtime.CompilerServices.CompilationRelaxationsAttribute..ctor] <assembly>",
                            "[System.Runtime.CompilerServices.RuntimeCompatibilityAttribute..ctor] <assembly>",
                            "[System.Diagnostics.DebuggableAttribute..ctor] <assembly>",
                            "[A1..ctor] C.F");
                    })
                .AddGeneration(
                    // 1
                    source: common + """
                    class C
                    {
                        [A2]
                        static void F() { }
                    }
                    """,
                    edits: new[] { Edit(SemanticEditKind.Update, c => c.GetMember("C.F")) },
                    validator: g =>
                    {
                        g.VerifyEncLogDefinitions(
                        [
                            Row(7, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(4, TableIndex.CustomAttribute, EditAndContinueOperation.Default), // Row 4, so updating existing CustomAttribute
                        ]);

                        g.VerifyEncMapDefinitions(
                        [
                            Handle(7, TableIndex.MethodDef),
                            Handle(4, TableIndex.CustomAttribute),
                        ]);

                        g.VerifyCustomAttributes("[A2..ctor] C.F");
                    })
                .AddGeneration(
                    // 2: Add attribute to method, and to class
                    source: common + """
                    [A5]
                    class C
                    {
                        [A3, A4]
                        static void F() { }
                    }
                    """,
                    edits: new[]
                    {
                        Edit(SemanticEditKind.Update, c => c.GetMember("C")),
                        Edit(SemanticEditKind.Update, c => c.GetMember("C.F"))
                    },
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("C");
                        g.VerifyMethodDefNames("F");

                        g.VerifyEncLogDefinitions(
                        [
                            Row(8, TableIndex.TypeDef, EditAndContinueOperation.Default),
                            Row(7, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(4, TableIndex.CustomAttribute, EditAndContinueOperation.Default), // updating the existing custom attribute
                            Row(5, TableIndex.CustomAttribute, EditAndContinueOperation.Default), // adding a new CustomAttribute for method F
                            Row(6, TableIndex.CustomAttribute, EditAndContinueOperation.Default), // adding a new CustomAttribute for type C
                        ]);

                        g.VerifyEncMapDefinitions(
                        [
                            Handle(8, TableIndex.TypeDef),
                            Handle(7, TableIndex.MethodDef),
                            Handle(4, TableIndex.CustomAttribute),
                            Handle(5, TableIndex.CustomAttribute),
                            Handle(6, TableIndex.CustomAttribute),
                        ]);

                        g.VerifyCustomAttributes(
                            "[A3..ctor] C.F",
                            "[A4..ctor] C.F",
                            "[A5..ctor] C");
                    })
                .AddGeneration(
                    // 3: Add attribute before existing attributes
                    source: common + """
                    [A5]
                    class C
                    {
                        [A6, A3, A4]
                        static void F() { }
                    }
                    """,
                    edits: new[] { Edit(SemanticEditKind.Update, c => c.GetMember("C.F")) },
                    validator: g =>
                    {
                        g.VerifyEncLogDefinitions(
                        [
                            Row(7, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(4, TableIndex.CustomAttribute, EditAndContinueOperation.Default), // updating the existing custom attribute
                            Row(5, TableIndex.CustomAttribute, EditAndContinueOperation.Default), // updating a row that was new in Generation 2
                            Row(7, TableIndex.CustomAttribute, EditAndContinueOperation.Default)  // adding a new CustomAttribute, and skipping row 6 which is not for the method being emitted
                        ]);

                        g.VerifyEncMapDefinitions(
                        [
                            Handle(7, TableIndex.MethodDef),
                            Handle(4, TableIndex.CustomAttribute),
                            Handle(5, TableIndex.CustomAttribute),
                            Handle(7, TableIndex.CustomAttribute),
                        ]);

                        g.VerifyCustomAttributes(
                            "[A6..ctor] C.F",
                            "[A3..ctor] C.F",
                            "[A4..ctor] C.F");
                    })
                .Verify();
        }

        [Fact]
        public void ModifyMethod_WithAttributes2()
        {
            var common = """
            using System;
            class A1 : Attribute { }
            class A2 : Attribute { }
            class A3 : Attribute { }
            class A4 : Attribute { }
            class A5 : Attribute { }
            class A6 : Attribute { }
            class A7 : Attribute { }
            """;

            using var _ = new EditAndContinueTest(options: TestOptions.DebugDll, targetFramework: TargetFramework.NetStandard20)
                .AddBaseline(
                    common + """
                    [A1]
                    class C
                    {
                        [A3]
                        static void F() {}
                    }
                    [A2]
                    class D
                    {
                        [A4]
                        static void G() {}
                    }
                    """,
                    validator: g =>
                    {
                        g.VerifyCustomAttributes(
                        [
                            new CustomAttributeRow(Handle(1, TableIndex.Assembly), Handle(1, TableIndex.MemberRef)),
                            new CustomAttributeRow(Handle(1, TableIndex.Assembly), Handle(2, TableIndex.MemberRef)),
                            new CustomAttributeRow(Handle(1, TableIndex.Assembly), Handle(3, TableIndex.MemberRef)),

                            // F:
                            new CustomAttributeRow(Handle(8, TableIndex.MethodDef), Handle(3, TableIndex.MethodDef)),

                            // C:
                            new CustomAttributeRow(Handle(9, TableIndex.TypeDef), Handle(1, TableIndex.MethodDef)),

                            // G:
                            new CustomAttributeRow(Handle(10, TableIndex.MethodDef), Handle(4, TableIndex.MethodDef)),

                            // D:
                            new CustomAttributeRow(Handle(10, TableIndex.TypeDef), Handle(2, TableIndex.MethodDef))
                        ]);
                    })
                .AddGeneration(
                    common + """
                    [A1]
                    class C
                    {
                        [A3, A5, A6]
                        static void F() {}
                    }
                    [A2]
                    class D
                    {
                        [A4, A7]
                        static void G() {}
                    }
                    """,
                    edits: new[]
                    {
                        Edit(SemanticEditKind.Update, c => c.GetMember("C.F")),
                        Edit(SemanticEditKind.Update, c => c.GetMember("D.G"))
                    },
                    validator: g =>
                    {
                        g.VerifyEncMapDefinitions(
                        [
                            Handle(8, TableIndex.MethodDef),
                            Handle(10, TableIndex.MethodDef),
                            Handle(4, TableIndex.CustomAttribute),
                            Handle(6, TableIndex.CustomAttribute),
                            Handle(8, TableIndex.CustomAttribute),
                            Handle(9, TableIndex.CustomAttribute),
                            Handle(10, TableIndex.CustomAttribute)
                        ]);

                        g.VerifyEncLogDefinitions(
                        [
                            Row(8, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(10, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(4, TableIndex.CustomAttribute, EditAndContinueOperation.Default), // update existing row
                            Row(6, TableIndex.CustomAttribute, EditAndContinueOperation.Default), // update existing row
                            Row(8, TableIndex.CustomAttribute, EditAndContinueOperation.Default), // add new row
                            Row(9, TableIndex.CustomAttribute, EditAndContinueOperation.Default), // add new row
                            Row(10, TableIndex.CustomAttribute, EditAndContinueOperation.Default),// add new row
                        ]);

                        g.VerifyCustomAttributes(
                        [
                            new CustomAttributeRow(Handle(8, TableIndex.MethodDef), Handle(3, TableIndex.MethodDef)),
                            new CustomAttributeRow(Handle(8, TableIndex.MethodDef), Handle(5, TableIndex.MethodDef)),
                            new CustomAttributeRow(Handle(8, TableIndex.MethodDef), Handle(6, TableIndex.MethodDef)),
                            new CustomAttributeRow(Handle(10, TableIndex.MethodDef), Handle(4, TableIndex.MethodDef)),
                            new CustomAttributeRow(Handle(10, TableIndex.MethodDef), Handle(7, TableIndex.MethodDef)),
                        ]);
                    })
                .Verify();
        }

        [Fact]
        public void ModifyMethod_DeleteAttributes1()
        {
            var source0 =
@"class C
{
    static void Main() { }
    [System.ComponentModel.Description(""The F method"")]
    static string F() { return null; }
}";
            var source1 =
@"class C
{
    static void Main() { }
    static string F() { return string.Empty; }
}";
            var source2 =
@"class C
{
    static void Main() { }
    [System.ComponentModel.Description(""The F method"")]
    static string F() { return string.Empty; }
}";

            var compilation0 = CreateCompilation(source0, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), options: TestOptions.DebugExe, targetFramework: TargetFramework.NetStandard20);
            var compilation1 = compilation0.WithSource(source1);
            var compilation2 = compilation1.WithSource(source2);

            var method0 = compilation0.GetMember<MethodSymbol>("C.F");

            // Verify full metadata contains expected rows.
            var bytes0 = compilation0.EmitToArray();
            using var md0 = ModuleMetadata.CreateFromImage(bytes0);
            var reader0 = md0.MetadataReader;

            CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "C");
            CheckNames(reader0, reader0.GetMethodDefNames(), "Main", "F", ".ctor");
            CheckNames(reader0, reader0.GetMemberRefNames(), /*CompilationRelaxationsAttribute.*/".ctor", /*RuntimeCompatibilityAttribute.*/".ctor", /*Object.*/".ctor", /*DebuggableAttribute*/".ctor", /*DescriptionAttribute*/".ctor");

            CheckAttributes(reader0,
                new CustomAttributeRow(Handle(1, TableIndex.Assembly), Handle(1, TableIndex.MemberRef)),
                new CustomAttributeRow(Handle(1, TableIndex.Assembly), Handle(2, TableIndex.MemberRef)),
                new CustomAttributeRow(Handle(1, TableIndex.Assembly), Handle(3, TableIndex.MemberRef)),
                new CustomAttributeRow(Handle(2, TableIndex.MethodDef), Handle(4, TableIndex.MemberRef)));

            var generation0 = CreateInitialBaseline(compilation0,
                md0,
                EmptyLocalsProvider);
            var method1 = compilation1.GetMember<MethodSymbol>("C.F");

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1)));

            // Verify delta metadata contains expected rows.
            using var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;
            var readers = new[] { reader0, reader1 };

            EncValidation.VerifyModuleMvid(1, reader0, reader1);

            CheckNames(readers, reader1.GetTypeDefNames());
            CheckNames(readers, reader1.GetMethodDefNames(), "F");
            CheckNames(readers, reader1.GetMemberRefNames(), /*String.*/"Empty");

            CheckAttributes(reader1,
                new CustomAttributeRow(Handle(0, TableIndex.MethodDef), Handle(0, TableIndex.MemberRef)));  // Parent row id is 0, signifying a delete

            CheckEncLog(reader1,
                Row(2, TableIndex.AssemblyRef, EditAndContinueOperation.Default),
                Row(6, TableIndex.MemberRef, EditAndContinueOperation.Default),
                Row(7, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(8, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(2, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(4, TableIndex.CustomAttribute, EditAndContinueOperation.Default)); // Row 4, so updating existing CustomAttribute

            CheckEncMap(reader1,
                Handle(7, TableIndex.TypeRef),
                Handle(8, TableIndex.TypeRef),
                Handle(2, TableIndex.MethodDef),
                Handle(6, TableIndex.MemberRef),
                Handle(4, TableIndex.CustomAttribute),
                Handle(2, TableIndex.StandAloneSig),
                Handle(2, TableIndex.AssemblyRef));

            var method2 = compilation2.GetMember<MethodSymbol>("C.F");
            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method1, method2)));

            // Verify delta metadata contains expected rows.
            using var md2 = diff2.GetMetadata();
            var reader2 = md2.Reader;
            readers = new[] { reader0, reader1, reader2 };

            EncValidation.VerifyModuleMvid(2, reader1, reader2);

            CheckNames(readers, reader2.GetTypeDefNames());
            CheckNames(readers, reader2.GetMethodDefNames(), "F");
            CheckNames(readers, reader2.GetMemberRefNames(), /*DescriptionAttribute*/".ctor", /*String.*/"Empty");

            CheckAttributes(reader2,
                new CustomAttributeRow(Handle(2, TableIndex.MethodDef), Handle(7, TableIndex.MemberRef)));

            CheckEncLog(reader2,
                Row(3, TableIndex.AssemblyRef, EditAndContinueOperation.Default),
                Row(7, TableIndex.MemberRef, EditAndContinueOperation.Default),
                Row(8, TableIndex.MemberRef, EditAndContinueOperation.Default),
                Row(9, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(10, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(11, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(3, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(4, TableIndex.CustomAttribute, EditAndContinueOperation.Default)); // Row 4, updating the original row back to a real one

            CheckEncMap(reader2,
                Handle(9, TableIndex.TypeRef),
                Handle(10, TableIndex.TypeRef),
                Handle(11, TableIndex.TypeRef),
                Handle(2, TableIndex.MethodDef),
                Handle(7, TableIndex.MemberRef),
                Handle(8, TableIndex.MemberRef),
                Handle(4, TableIndex.CustomAttribute),
                Handle(3, TableIndex.StandAloneSig),
                Handle(3, TableIndex.AssemblyRef));
        }

        [Fact]
        public void ModifyMethod_DeleteAttributes2()
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
    [System.ComponentModel.Description(""The F method"")]
    static string F() { return string.Empty; }
}";
            var source2 = source0; // Remove the attribute we just added
            var source3 = source1; // Add the attribute back again

            var compilation0 = CreateCompilation(source0, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), options: TestOptions.DebugExe, targetFramework: TargetFramework.NetStandard20);
            var compilation1 = compilation0.WithSource(source1);
            var compilation2 = compilation1.WithSource(source2);
            var compilation3 = compilation1.WithSource(source3);

            var method0 = compilation0.GetMember<MethodSymbol>("C.F");

            // Verify full metadata contains expected rows.
            var bytes0 = compilation0.EmitToArray();
            using var md0 = ModuleMetadata.CreateFromImage(bytes0);
            var reader0 = md0.MetadataReader;

            CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "C");
            CheckNames(reader0, reader0.GetMethodDefNames(), "Main", "F", ".ctor");
            CheckNames(reader0, reader0.GetMemberRefNames(), /*CompilationRelaxationsAttribute.*/".ctor", /*RuntimeCompatibilityAttribute.*/".ctor", /*Object.*/".ctor", /*DebuggableAttribute*/".ctor");

            CheckAttributes(reader0,
                new CustomAttributeRow(Handle(1, TableIndex.Assembly), Handle(1, TableIndex.MemberRef)),
                new CustomAttributeRow(Handle(1, TableIndex.Assembly), Handle(2, TableIndex.MemberRef)),
                new CustomAttributeRow(Handle(1, TableIndex.Assembly), Handle(3, TableIndex.MemberRef)));

            var generation0 = CreateInitialBaseline(compilation0,
                md0,
                EmptyLocalsProvider);
            var method1 = compilation1.GetMember<MethodSymbol>("C.F");

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1)));

            // Verify delta metadata contains expected rows.
            using var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;
            var readers = new[] { reader0, reader1 };

            EncValidation.VerifyModuleMvid(1, reader0, reader1);

            CheckNames(readers, reader1.GetTypeDefNames());
            CheckNames(readers, reader1.GetMethodDefNames(), "F");
            CheckNames(readers, reader1.GetMemberRefNames(), /*DescriptionAttribute*/".ctor", /*String.*/"Empty");

            CheckAttributes(reader1,
                new CustomAttributeRow(Handle(2, TableIndex.MethodDef), Handle(5, TableIndex.MemberRef)));

            CheckEncLog(reader1,
                Row(2, TableIndex.AssemblyRef, EditAndContinueOperation.Default),
                Row(5, TableIndex.MemberRef, EditAndContinueOperation.Default),
                Row(6, TableIndex.MemberRef, EditAndContinueOperation.Default),
                Row(6, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(7, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(8, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(2, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(4, TableIndex.CustomAttribute, EditAndContinueOperation.Default)); // Row 4, so adding a new CustomAttribute

            CheckEncMap(reader1,
                Handle(6, TableIndex.TypeRef),
                Handle(7, TableIndex.TypeRef),
                Handle(8, TableIndex.TypeRef),
                Handle(2, TableIndex.MethodDef),
                Handle(5, TableIndex.MemberRef),
                Handle(6, TableIndex.MemberRef),
                Handle(4, TableIndex.CustomAttribute),
                Handle(2, TableIndex.StandAloneSig),
                Handle(2, TableIndex.AssemblyRef));

            var method2 = compilation2.GetMember<MethodSymbol>("C.F");
            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, method1, method2)));

            // Verify delta metadata contains expected rows.
            using var md2 = diff2.GetMetadata();
            var reader2 = md2.Reader;
            readers = new[] { reader0, reader1, reader2 };

            CheckNames(readers, reader2.GetTypeDefNames());
            CheckNames(readers, reader2.GetMethodDefNames(), "F");
            CheckNames(readers, reader2.GetMemberRefNames());

            CheckAttributes(reader2,
                new CustomAttributeRow(Handle(0, TableIndex.MethodDef), Handle(0, TableIndex.MemberRef)));  // 0, delete

            CheckEncLog(reader2,
                Row(3, TableIndex.AssemblyRef, EditAndContinueOperation.Default),
                Row(9, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(3, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(4, TableIndex.CustomAttribute, EditAndContinueOperation.Default)); // Row 4, so updating existing CustomAttribute

            CheckEncMap(reader2,
                Handle(9, TableIndex.TypeRef),
                Handle(2, TableIndex.MethodDef),
                Handle(4, TableIndex.CustomAttribute),
                Handle(3, TableIndex.StandAloneSig),
                Handle(3, TableIndex.AssemblyRef));

            var method3 = compilation3.GetMember<MethodSymbol>("C.F");
            var diff3 = compilation3.EmitDifference(
                diff2.NextGeneration,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, method2, method3)));

            // Verify delta metadata contains expected rows.
            using var md3 = diff3.GetMetadata();
            var reader3 = md3.Reader;
            readers = new[] { reader0, reader1, reader2, reader3 };

            CheckNames(readers, reader3.GetTypeDefNames());
            CheckNames(readers, reader3.GetMethodDefNames(), "F");
            CheckNames(readers, reader3.GetMemberRefNames(), /*DescriptionAttribute*/".ctor", /*String.*/"Empty");

            CheckAttributes(reader3,
                new CustomAttributeRow(Handle(2, TableIndex.MethodDef), Handle(7, TableIndex.MemberRef)));

            CheckEncLog(reader3,
                Row(4, TableIndex.AssemblyRef, EditAndContinueOperation.Default),
                Row(7, TableIndex.MemberRef, EditAndContinueOperation.Default),
                Row(8, TableIndex.MemberRef, EditAndContinueOperation.Default),
                Row(10, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(11, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(12, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(4, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(4, TableIndex.CustomAttribute, EditAndContinueOperation.Default)); // Row 4, update the previously deleted row

            CheckEncMap(reader3,
                Handle(10, TableIndex.TypeRef),
                Handle(11, TableIndex.TypeRef),
                Handle(12, TableIndex.TypeRef),
                Handle(2, TableIndex.MethodDef),
                Handle(7, TableIndex.MemberRef),
                Handle(8, TableIndex.MemberRef),
                Handle(4, TableIndex.CustomAttribute),
                Handle(4, TableIndex.StandAloneSig),
                Handle(4, TableIndex.AssemblyRef));
        }

        [Fact]
        public void ModifyMethod_DeleteAttributes3()
        {
            var common = """
                using System;
                class A1 : Attribute { }
                class A2 : Attribute { }
                class A3 : Attribute { }
                class A4 : Attribute { }
                class A5 : Attribute { }
                class A6 : Attribute { }
                class A7 : Attribute { }
                class A8 : Attribute { }
                """;

            using var _ = new EditAndContinueTest(verification: Verification.Skipped)
                .AddBaseline(
                    source: common + """
                        class C
                        {
                            [A1, A2]void F() { }
                            [A3]void G() { }
                            [A5, A6]void H() { }
                        }
                        """,
                    validator: g =>
                    {
                        g.VerifyCustomAttributes(
                        [
                            new CustomAttributeRow(Handle(1, TableIndex.Assembly), Handle(1, TableIndex.MemberRef)),
                            new CustomAttributeRow(Handle(1, TableIndex.Assembly), Handle(2, TableIndex.MemberRef)),
                            new CustomAttributeRow(Handle(1, TableIndex.Assembly), Handle(3, TableIndex.MemberRef)),

                            // F:
                            new CustomAttributeRow(Handle(9, TableIndex.MethodDef), Handle(1, TableIndex.MethodDef)), // Row 4
                            new CustomAttributeRow(Handle(9, TableIndex.MethodDef), Handle(2, TableIndex.MethodDef)), // Row 5

                            // G:
                            new CustomAttributeRow(Handle(10, TableIndex.MethodDef), Handle(3, TableIndex.MethodDef)), // Row 6

                            // H:
                            new CustomAttributeRow(Handle(11, TableIndex.MethodDef), Handle(5, TableIndex.MethodDef)), // Row 7
                            new CustomAttributeRow(Handle(11, TableIndex.MethodDef), Handle(6, TableIndex.MethodDef)), // Row 8
                        ]);
                    })
                .AddGeneration(
                    source: common + """
                        class C
                        {
                            [A2]void F() { }
                            [A4, A3]void G() { }
                            [A7]void H() { }
                        }
                        """,
                    edits: new[]
                    {
                        Edit(SemanticEditKind.Update, c => c.GetMember("C.F")),
                        Edit(SemanticEditKind.Update, c => c.GetMember("C.G")),
                        Edit(SemanticEditKind.Update, c => c.GetMember("C.H")),
                    },
                    validator: g =>
                    {
                        g.VerifyEncLogDefinitions(
                        [
                            Row(9, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(10, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(11, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(4, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(5, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(6, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(7, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(8, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(9, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        ]);

                        g.VerifyEncMapDefinitions(
                        [
                            Handle(9, TableIndex.MethodDef),
                            Handle(10, TableIndex.MethodDef),
                            Handle(11, TableIndex.MethodDef),
                            Handle(4, TableIndex.CustomAttribute),
                            Handle(5, TableIndex.CustomAttribute),
                            Handle(6, TableIndex.CustomAttribute),
                            Handle(7, TableIndex.CustomAttribute),
                            Handle(8, TableIndex.CustomAttribute),
                            Handle(9, TableIndex.CustomAttribute),
                        ]);

                        g.VerifyCustomAttributes(
                        [
                            new CustomAttributeRow(Handle(0, TableIndex.MethodDef), Handle(0, TableIndex.MemberRef)), // F [A2] delete
                            new CustomAttributeRow(Handle(0, TableIndex.MethodDef), Handle(0, TableIndex.MemberRef)), // H [A5] delete
                            new CustomAttributeRow(Handle(9, TableIndex.MethodDef), Handle(2, TableIndex.MethodDef)), // F [A1] -> [A2]
                            new CustomAttributeRow(Handle(10, TableIndex.MethodDef), Handle(4, TableIndex.MethodDef)),// G [A3] -> [A4]
                            new CustomAttributeRow(Handle(10, TableIndex.MethodDef), Handle(3, TableIndex.MethodDef)),// G [A3] add with RowId 9
                            new CustomAttributeRow(Handle(11, TableIndex.MethodDef), Handle(7, TableIndex.MethodDef)),// H [A6] -> [A7]
                        ]);
                    })
                .AddGeneration(
                    source: common + """
                        class C
                        {
                            [A2]void F() { }
                            void G() { }
                            [A5, A6, A7, A8]void H() { }
                        }
                        """,
                    edits: new[]
                    {
                        Edit(SemanticEditKind.Update, c => c.GetMember("C.G")),
                        Edit(SemanticEditKind.Update, c => c.GetMember("C.H")),
                    },
                    validator: g =>
                    {
                        g.VerifyEncLogDefinitions(
                        [
                            Row(10, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(11, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(6, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(7, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(8, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(9, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(10, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(11, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        ]);

                        g.VerifyEncMapDefinitions(
                        [
                            Handle(10, TableIndex.MethodDef),
                            Handle(11, TableIndex.MethodDef),
                            Handle(6, TableIndex.CustomAttribute),
                            Handle(7, TableIndex.CustomAttribute),
                            Handle(8, TableIndex.CustomAttribute),
                            Handle(9, TableIndex.CustomAttribute),
                            Handle(10, TableIndex.CustomAttribute),
                            Handle(11, TableIndex.CustomAttribute),
                        ]);

                        g.VerifyCustomAttributes(
                        [
                            new CustomAttributeRow(Handle(0, TableIndex.MethodDef), Handle(0, TableIndex.MemberRef)),  // G [A4] delete
                            new CustomAttributeRow(Handle(0, TableIndex.MethodDef), Handle(0, TableIndex.MemberRef)),  // G [A3] delete
                            new CustomAttributeRow(Handle(11, TableIndex.MethodDef), Handle(5, TableIndex.MethodDef)), // H [A5]
                            new CustomAttributeRow(Handle(11, TableIndex.MethodDef), Handle(6, TableIndex.MethodDef)), // H [A6]
                            new CustomAttributeRow(Handle(11, TableIndex.MethodDef), Handle(7, TableIndex.MethodDef)), // H [A7] add with RowId 10
                            new CustomAttributeRow(Handle(11, TableIndex.MethodDef), Handle(8, TableIndex.MethodDef)), // H [A8] add with RowId 11
                        ]);
                    })
                .AddGeneration(
                    source: common + """
                        class C
                        {
                            [A2]void F() { }
                            void G() { }
                            void H() { }
                        }
                        """,
                    edits: new[]
                    {
                        Edit(SemanticEditKind.Update, c => c.GetMember("C.H")),
                    },
                    validator: g =>
                    {
                        g.VerifyEncLogDefinitions(
                        [
                            Row(11, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(7, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(8, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(10, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(11, TableIndex.CustomAttribute, EditAndContinueOperation.Default)
                        ]);

                        g.VerifyEncMapDefinitions(
                        [
                            Handle(11, TableIndex.MethodDef),
                            Handle(7, TableIndex.CustomAttribute),
                            Handle(8, TableIndex.CustomAttribute),
                            Handle(10, TableIndex.CustomAttribute),
                            Handle(11, TableIndex.CustomAttribute),
                        ]);

                        g.VerifyCustomAttributes(
                        [
                            new CustomAttributeRow(Handle(0, TableIndex.MethodDef), Handle(0, TableIndex.MemberRef)), // H [A5] delete
                            new CustomAttributeRow(Handle(0, TableIndex.MethodDef), Handle(0, TableIndex.MemberRef)), // H [A6] delete
                            new CustomAttributeRow(Handle(0, TableIndex.MethodDef), Handle(0, TableIndex.MemberRef)), // H [A7] delete
                            new CustomAttributeRow(Handle(0, TableIndex.MethodDef), Handle(0, TableIndex.MemberRef)), // H [A8] delete
                        ]);
                    })
                .Verify();
        }

        [Fact]
        public void ModifyMethod_DeleteAttributes_Ordering()
        {
            var common = """
                using System;
                class A1 : Attribute { }
                class A2 : Attribute { }
                class A3 : Attribute { }
                class A4 : Attribute { }
                """;

            using var _ = new EditAndContinueTest(verification: Verification.Skipped)
                .AddBaseline(
                    source: common + """
                        class C
                        {
                            void F() { }
                            void G() { }
                        }
                        """,
                    validator: g =>
                    {
                        g.VerifyCustomAttributes(
                        [
                            new CustomAttributeRow(Handle(1, TableIndex.Assembly), Handle(1, TableIndex.MemberRef)),
                            new CustomAttributeRow(Handle(1, TableIndex.Assembly), Handle(2, TableIndex.MemberRef)),
                            new CustomAttributeRow(Handle(1, TableIndex.Assembly), Handle(3, TableIndex.MemberRef)),
                        ]);
                    })
                .AddGeneration( // add attribute to G
                    source: common + """
                        class C
                        {
                            void F() { }
                            [A1] void G() { }
                        }
                        """,
                    edits: new[]
                    {
                        Edit(SemanticEditKind.Update, c => c.GetMember("C.G")),
                    },
                    validator: g =>
                    {
                        g.VerifyEncLogDefinitions(
                        [
                            Row(6, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(4, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        ]);

                        g.VerifyEncMapDefinitions(
                        [
                            Handle(6, TableIndex.MethodDef),
                            Handle(4, TableIndex.CustomAttribute),
                        ]);

                        g.VerifyCustomAttributes(
                        [
                            new CustomAttributeRow(Handle(6, TableIndex.MethodDef), Handle(1, TableIndex.MethodDef)), // G: [A1] add RowId 4
                        ]);
                    })
                .AddGeneration( // add attribute to F
                    source: common + """
                        class C
                        {
                            [A1] void F() { }
                            [A2] void G() { }
                        }
                        """,
                    edits: new[]
                    {
                        Edit(SemanticEditKind.Update, c => c.GetMember("C.F")),
                    },
                    validator: g =>
                    {
                        g.VerifyEncLogDefinitions(
                        [
                            Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(5, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        ]);

                        g.VerifyEncMapDefinitions(
                        [
                            Handle(5, TableIndex.MethodDef),
                            Handle(5, TableIndex.CustomAttribute),
                        ]);

                        g.VerifyCustomAttributes(
                        [
                            new CustomAttributeRow(Handle(5, TableIndex.MethodDef), Handle(1, TableIndex.MethodDef)), // F: [A2] add RowId 5
                        ]);
                    })
                .AddGeneration( // update attributes of both F and G
                    source: common + """
                        class C
                        {
                            [A3] void F() { }
                            [A4] void G() { }
                        }
                        """,
                    edits: new[]
                    {
                        Edit(SemanticEditKind.Update, c => c.GetMember("C.F")),
                        Edit(SemanticEditKind.Update, c => c.GetMember("C.G")),
                    },
                    validator: g =>
                    {
                        g.VerifyEncLogDefinitions(
                        [
                            Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(6, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(4, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(5, TableIndex.CustomAttribute, EditAndContinueOperation.Default)
                        ]);

                        g.VerifyEncMapDefinitions(
                        [
                            Handle(5, TableIndex.MethodDef),
                            Handle(6, TableIndex.MethodDef),
                            Handle(4, TableIndex.CustomAttribute),
                            Handle(5, TableIndex.CustomAttribute),
                        ]);

                        g.VerifyCustomAttributes(
                        [
                            new CustomAttributeRow(Handle(5, TableIndex.MethodDef), Handle(3, TableIndex.MethodDef)), // G: [A2] -> [A4]
                            new CustomAttributeRow(Handle(6, TableIndex.MethodDef), Handle(4, TableIndex.MethodDef)), // F: [A1] -> [A3]
                        ]);
                    })
                .Verify();
        }

        [Fact]
        public void ModifyMethod_DeleteAttributes_DeletePreviouslyAdded()
        {
            var common = """
                using System;
                class A1 : Attribute { }
                class A2 : Attribute { }
                class A3 : Attribute { }
                """;

            using var _ = new EditAndContinueTest(verification: Verification.Skipped)
                .AddBaseline(
                    source: common + """
                        class C
                        {
                            [A1] void F() { }
                        }
                        """,
                    validator: g =>
                    {
                        g.VerifyCustomAttributes(
                        [
                            new CustomAttributeRow(Handle(1, TableIndex.Assembly), Handle(1, TableIndex.MemberRef)),
                            new CustomAttributeRow(Handle(1, TableIndex.Assembly), Handle(2, TableIndex.MemberRef)),
                            new CustomAttributeRow(Handle(1, TableIndex.Assembly), Handle(3, TableIndex.MemberRef)),

                            // F:
                            new CustomAttributeRow(Handle(4, TableIndex.MethodDef), Handle(1, TableIndex.MethodDef)), // Row 4
                        ]);
                    })
                .AddGeneration(
                    source: common + """
                        class C
                        {
                            [A2, A3] void F() { }
                        }
                        """,
                    edits: new[]
                    {
                        Edit(SemanticEditKind.Update, c => c.GetMember("C.F")),
                    },
                    validator: g =>
                    {
                        g.VerifyEncLogDefinitions(
                        [
                            Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(4, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(5, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        ]);

                        g.VerifyEncMapDefinitions(
                        [
                            Handle(4, TableIndex.MethodDef),
                            Handle(4, TableIndex.CustomAttribute),
                            Handle(5, TableIndex.CustomAttribute),
                        ]);

                        g.VerifyCustomAttributes(
                        [
                            new CustomAttributeRow(Handle(4, TableIndex.MethodDef), Handle(2, TableIndex.MethodDef)), // F [A1] -> [A2]
                            new CustomAttributeRow(Handle(4, TableIndex.MethodDef), Handle(3, TableIndex.MethodDef)), // F [A3] add RowId 5
                        ]);
                    })
                .AddGeneration(
                    source: common + """
                        class C
                        {
                            void F() { }
                        }
                        """,
                    edits: new[]
                    {
                        Edit(SemanticEditKind.Update, c => c.GetMember("C.F")),
                    },
                    validator: g =>
                    {
                        g.VerifyEncLogDefinitions(
                        [
                            Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(4, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(5, TableIndex.CustomAttribute, EditAndContinueOperation.Default)
                        ]);

                        g.VerifyEncMapDefinitions(
                        [
                            Handle(4, TableIndex.MethodDef),
                            Handle(4, TableIndex.CustomAttribute),
                            Handle(5, TableIndex.CustomAttribute),
                        ]);

                        g.VerifyCustomAttributes(
                        [
                            new CustomAttributeRow(Handle(0, TableIndex.MethodDef), Handle(0, TableIndex.MemberRef)),  // F [A2] delete
                            new CustomAttributeRow(Handle(0, TableIndex.MethodDef), Handle(0, TableIndex.MemberRef)),  // F [A3] delete
                        ]);
                    })
                .Verify();
        }

        [Fact]
        public void Lambda_Attributes()
        {
            var source0 = MarkedSource(@"
class C
{
    void F()
    {
        var x = <N:0>(int a) => a</N:0>;
    }
}");
            var source1 = MarkedSource(@"
class C
{
    void F()
    {
        var x = <N:0>[System.Obsolete](int a) => a</N:0>;
    }
}");

            var compilation0 = CreateCompilation(source0.Tree, options: ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            var compilation1 = compilation0.WithSource(source1.Tree);

            var v0 = CompileAndVerify(compilation0);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);
            var reader0 = md0.MetadataReader;

            var method0 = compilation0.GetMember<MethodSymbol>("C.F");
            var method1 = compilation1.GetMember<MethodSymbol>("C.F");

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "C", "<>c");
            CheckNames(reader0, reader0.GetMethodDefNames(), "F", ".ctor", ".cctor", ".ctor", "<F>b__0_0");
            CheckAttributes(reader0,
                new CustomAttributeRow(Handle(1, TableIndex.Assembly), Handle(1, TableIndex.MemberRef)),
                new CustomAttributeRow(Handle(1, TableIndex.Assembly), Handle(2, TableIndex.MemberRef)),
                new CustomAttributeRow(Handle(1, TableIndex.Assembly), Handle(3, TableIndex.MemberRef)),
                new CustomAttributeRow(Handle(3, TableIndex.TypeDef), Handle(4, TableIndex.MemberRef)));

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1, GetSyntaxMapFromMarkers(source0, source1))));

            // Verify delta metadata contains expected rows.
            using var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;
            var readers = new[] { reader0, reader1 };

            EncValidation.VerifyModuleMvid(1, reader0, reader1);

            CheckNames(readers, reader1.GetTypeDefNames());
            CheckNames(readers, reader1.GetMethodDefNames(), "F", "<F>b__0_0");

            diff1.VerifySynthesizedMembers(
                "C: {<>c}",
                "C.<>c: {<>9__0_0, <F>b__0_0}");

            CheckAttributes(reader1,
                new CustomAttributeRow(Handle(5, TableIndex.MethodDef), Handle(8, TableIndex.MemberRef)));

            CheckEncLogDefinitions(reader1,
                Row(2, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(1, TableIndex.Param, EditAndContinueOperation.Default),
                Row(5, TableIndex.CustomAttribute, EditAndContinueOperation.Default)); // row 5 = new custom attribute

            CheckEncMapDefinitions(reader1,
                Handle(1, TableIndex.MethodDef),
                Handle(5, TableIndex.MethodDef),
                Handle(1, TableIndex.Param),
                Handle(5, TableIndex.CustomAttribute),
                Handle(2, TableIndex.StandAloneSig));
        }

        [Fact]
        public void Lambda_SynthesizedDelegate_01()
        {
            var source0 = MarkedSource(@"
class C
{
    void F()
    {
        var x = <N:0>(ref int a, int b) => a</N:0>;
    }
}");
            var source1 = MarkedSource(@"
class C
{
    void F()
    {
        var x = <N:0>(ref int a, int b) => b</N:0>;

        var y = <N:1>(int a, ref int b) => a</N:1>;

        var z = <N:2>(int _1, int _2, int _3, int _4, int _5, int _6, ref int _7, int _8, int _9, int _10, int _11, int _12, int _13, int _14, int _15, int _16, int _17, int _18, int _19, int _20, int _21, int _22, int _23, int _24, int _25, int _26, int _27, int _28, int _29, int _30, int _31, int _32, ref int _33) => { }</N:2>;
    }
}");
            var source2 = MarkedSource(@"
class C
{
    void F()
    {
        var x = <N:0>(ref int a, int b) => b</N:0>;

        var y = <N:1>(int a, ref int b) => b</N:1>;

        var z = <N:2>(int _1, int _2, int _3, int _4, int _5, int _6, ref int _7, int _8, int _9, int _10, int _11, int _12, int _13, int _14, int _15, int _16, int _17, int _18, int _19, int _20, int _21, int _22, int _23, int _24, int _25, int _26, int _27, int _28, int _29, int _30, int _31, int _32, ref int _33) => { _1.ToString(); }</N:2>;
    }
}");

            var compilation0 = CreateCompilation(source0.Tree, options: ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            var compilation1 = compilation0.WithSource(source1.Tree);
            var compilation2 = compilation1.WithSource(source2.Tree);

            var v0 = CompileAndVerify(compilation0);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);
            var reader0 = md0.MetadataReader;

            var method0 = compilation0.GetMember<MethodSymbol>("C.F");
            var method1 = compilation1.GetMember<MethodSymbol>("C.F");
            var method2 = compilation2.GetMember<MethodSymbol>("C.F");

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "<>F{00000001}`3", "C", "<>c");       // <>F{00000001}`3 is the synthesized delegate for the lambda
            CheckNames(reader0, reader0.GetMethodDefNames(), ".ctor", "Invoke", "F", ".ctor", ".cctor", ".ctor", "<F>b__0_0");

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1, GetSyntaxMapFromMarkers(source0, source1))));

            // Verify delta metadata contains expected rows.
            using var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;
            var readers = new[] { reader0, reader1 };

            EncValidation.VerifyModuleMvid(1, reader0, reader1);

            CheckNames(readers, reader1.GetTypeDefNames(), "<>A{00040000,100000000}`33", "<>F{00000008}`3");                              // new synthesized delegate for the new lambda
            CheckNames(readers, reader1.GetMethodDefNames(), "F", "<F>b__0_0", ".ctor", "Invoke", ".ctor", "Invoke", "<F>b__0_1#1", "<F>b__0_2#1");

            diff1.VerifySynthesizedMembers(
                "C: {<>c}",
                "C.<>c: {<>9__0_0, <>9__0_1#1, <>9__0_2#1, <F>b__0_0, <F>b__0_1#1, <F>b__0_2#1}");

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method1, method2, GetSyntaxMapFromMarkers(source1, source2))));

            using var md2 = diff2.GetMetadata();
            var reader2 = md2.Reader;
            readers = new[] { reader0, reader1, reader2 };

            EncValidation.VerifyModuleMvid(2, reader1, reader2);

            CheckNames(readers, reader2.GetTypeDefNames());                                     // No new delegate added, reusing from gen 0 and 1
            CheckNames(readers, reader2.GetMethodDefNames(), "F", "<F>b__0_0", "<F>b__0_1#1", "<F>b__0_2#1");

            diff2.VerifySynthesizedMembers(
                "C: {<>c}",
                "C.<>c: {<>9__0_0, <>9__0_1#1, <>9__0_2#1, <F>b__0_0, <F>b__0_1#1, <F>b__0_2#1}");
        }

        [Fact]
        public void Lambda_SynthesizedDelegate_02()
        {
            var source0 = MarkedSource(
@"class C
{
    static unsafe void F()
    {
        var x = <N:0>(int* a, int b) => *a</N:0>;
    }
}");
            var source1 = MarkedSource(
@"class C
{
    static unsafe void F()
    {
        var x = <N:0>(int* a, int b) => b</N:0>;
        var y = <N:1>(int a, int* b) => a</N:1>;
        var z = <N:2>(int* a) => a</N:2>;
    }
}");
            var source2 = MarkedSource(
@"class C
{
    static unsafe void F()
    {
        var x = <N:0>(int* a, int b) => b</N:0>;
        var y = <N:1>(int a, int* b) => *b</N:1>;
        var z = <N:2>(int* a) => a</N:2>;
    }
}");

            var compilation0 = CreateCompilation(source0.Tree, options: ComSafeDebugDll.WithAllowUnsafe(true).WithMetadataImportOptions(MetadataImportOptions.All));
            var compilation1 = compilation0.WithSource(source1.Tree);
            var compilation2 = compilation1.WithSource(source2.Tree);

            var v0 = CompileAndVerify(compilation0, verify: Verification.Skipped);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);
            var reader0 = md0.MetadataReader;

            var method0 = compilation0.GetMember<MethodSymbol>("C.F");
            var method1 = compilation1.GetMember<MethodSymbol>("C.F");
            var method2 = compilation2.GetMember<MethodSymbol>("C.F");

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "<>f__AnonymousDelegate0", "C", "<>c");
            CheckNames(reader0, reader0.GetMethodDefNames(), ".ctor", "Invoke", "F", ".ctor", ".cctor", ".ctor", "<F>b__0_0");

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1, GetSyntaxMapFromMarkers(source0, source1))));

            // Verify delta metadata contains expected rows.
            using var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;
            var readers = new[] { reader0, reader1 };

            EncValidation.VerifyModuleMvid(1, reader0, reader1);

            CheckNames(readers, reader1.GetTypeDefNames(), "<>f__AnonymousDelegate1", "<>f__AnonymousDelegate2");
            CheckNames(readers, reader1.GetMethodDefNames(), "F", "<F>b__0_0", ".ctor", "Invoke", ".ctor", "Invoke", "<F>b__0_1#1", "<F>b__0_2#1");

            diff1.VerifySynthesizedMembers(
                "C: {<>c}",
                "C.<>c: {<>9__0_0, <>9__0_1#1, <>9__0_2#1, <F>b__0_0, <F>b__0_1#1, <F>b__0_2#1}");

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method1, method2, GetSyntaxMapFromMarkers(source1, source2))));

            using var md2 = diff2.GetMetadata();
            var reader2 = md2.Reader;
            readers = new[] { reader0, reader1, reader2 };

            EncValidation.VerifyModuleMvid(2, reader1, reader2);

            CheckNames(readers, reader2.GetTypeDefNames());
            CheckNames(readers, reader2.GetMethodDefNames(), "F", "<F>b__0_0", "<F>b__0_1#1", "<F>b__0_2#1");

            diff2.VerifySynthesizedMembers(
                "C: {<>c}",
                "C.<>c: {<>9__0_0, <>9__0_1#1, <>9__0_2#1, <F>b__0_0, <F>b__0_1#1, <F>b__0_2#1}");
        }

        [Fact]
        public void Lambda_SynthesizedDelegate_04()
        {
            var source0 = MarkedSource(
@"class A { }
struct B<T> { }
class C
{
    static unsafe void F()
    {
    }
}");
            var source1 = MarkedSource(
@"class A { }
struct B<T> { }
class C
{
    static unsafe void F()
    {
        var x = <N:0>(B<A>* a, int b) => a</N:0>;
    }
}");

            var compilation0 = CreateCompilation(source0.Tree, options: ComSafeDebugDll.WithAllowUnsafe(true).WithMetadataImportOptions(MetadataImportOptions.All));
            var compilation1 = compilation0.WithSource(source1.Tree);

            var v0 = CompileAndVerify(compilation0, verify: Verification.Skipped);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);
            var reader0 = md0.MetadataReader;

            var method0 = compilation0.GetMember<MethodSymbol>("C.F");
            var method1 = compilation1.GetMember<MethodSymbol>("C.F");

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "A", "B`1", "C");
            CheckNames(reader0, reader0.GetMethodDefNames(), ".ctor", "F", ".ctor");

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1, GetSyntaxMapFromMarkers(source0, source1))));

            // Verify delta metadata contains expected rows.
            using var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;
            var readers = new[] { reader0, reader1 };

            EncValidation.VerifyModuleMvid(1, reader0, reader1);

            CheckNames(readers, reader1.GetTypeDefNames(), "<>f__AnonymousDelegate0", "<>c");
            CheckNames(readers, reader1.GetMethodDefNames(), "F", ".ctor", "Invoke", ".cctor", ".ctor", "<F>b__0#1_0#1");

            diff1.VerifySynthesizedMembers(
               "C.<>c: {<>9__0#1_0#1, <F>b__0#1_0#1}",
               "C: {<>c}");
        }

        [Fact]
        public void Lambda_SynthesizedDelegate_05()
        {
            var source0 = MarkedSource(
@"class C<T> where T : unmanaged
{
    static unsafe void F<U>() where U : unmanaged
    {
        var x = <N:0>(T t, U* u) => *u</N:0>;
    }
}");
            var source1 = MarkedSource(
@"class C<T> where T : unmanaged
{
    static unsafe void F<U>() where U : unmanaged
    {
        var x = <N:0>(T t, U* u) => default(U)</N:0>;
        var y = <N:1>(U u, T* t) => *t</N:1>;
    }
}");
            var source2 = MarkedSource(
@"class C<T> where T : unmanaged
{
    static unsafe void F<U>() where U : unmanaged
    {
        var x = <N:0>(T t, U* u) => *u</N:0>;
        var y = <N:1>(U u, T* t) => *t</N:1>;
    }
}");

            var compilation0 = CreateCompilation(source0.Tree, options: ComSafeDebugDll.WithAllowUnsafe(true).WithMetadataImportOptions(MetadataImportOptions.All));
            var compilation1 = compilation0.WithSource(source1.Tree);
            var compilation2 = compilation1.WithSource(source2.Tree);

            var v0 = CompileAndVerify(compilation0, verify: Verification.Skipped);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);
            var reader0 = md0.MetadataReader;

            var method0 = compilation0.GetMember<MethodSymbol>("C.F");
            var method1 = compilation1.GetMember<MethodSymbol>("C.F");
            var method2 = compilation2.GetMember<MethodSymbol>("C.F");

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "<>f__AnonymousDelegate0`2", "EmbeddedAttribute", "IsUnmanagedAttribute", "C`1", "<>c__0`1");
            CheckNames(reader0, reader0.GetMethodDefNames(), ".ctor", "Invoke", ".ctor", ".ctor", "F", ".ctor", ".cctor", ".ctor", "<F>b__0_0");

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1, GetSyntaxMapFromMarkers(source0, source1))));

            // Verify delta metadata contains expected rows.
            using var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;
            var readers = new[] { reader0, reader1 };

            EncValidation.VerifyModuleMvid(1, reader0, reader1);

            CheckNames(readers, reader1.GetTypeDefNames(), "<>f__AnonymousDelegate1`2");
            CheckNames(readers, reader1.GetMethodDefNames(), "F", "<F>b__0_0", ".ctor", "Invoke", "<F>b__0_1#1");

            diff1.VerifySynthesizedMembers(
                "Microsoft.CodeAnalysis.EmbeddedAttribute",
                "System.Runtime.CompilerServices.IsUnmanagedAttribute",
                "C<T>: {<>c__0}",
                "C<T>.<>c__0<U>: {<>9__0_0, <>9__0_1#1, <F>b__0_0, <F>b__0_1#1}");

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method1, method2, GetSyntaxMapFromMarkers(source1, source2))));

            using var md2 = diff2.GetMetadata();
            var reader2 = md2.Reader;
            readers = new[] { reader0, reader1, reader2 };

            EncValidation.VerifyModuleMvid(2, reader1, reader2);

            CheckNames(readers, reader2.GetTypeDefNames());
            CheckNames(readers, reader2.GetMethodDefNames(), "F", "<F>b__0_0", "<F>b__0_1#1");

            diff2.VerifySynthesizedMembers(
                "Microsoft.CodeAnalysis.EmbeddedAttribute",
                "System.Runtime.CompilerServices.IsUnmanagedAttribute",
                "C<T>: {<>c__0}",
                "C<T>.<>c__0<U>: {<>9__0_0, <>9__0_1#1, <F>b__0_0, <F>b__0_1#1}");
        }

        [Fact]
        public void Lambda_SynthesizedDelegate_06()
        {
            var source0 = MarkedSource(
@"class C
{
    void F()
    {
        var x = <N:0>(int p1, int p2, int p3, int p4, int p5, int p6, int p7, int p8, int p9, int p10, int p11, int p12, int p13, int p14, int p15, int p16, int p17) => 1</N:0>;
    }
}");
            var source1 = MarkedSource(
@"class C
{
    void F()
    {
        var x = <N:0>(int p1, int p2, int p3, int p4, int p5, int p6, int p7, int p8, int p9, int p10, int p11, int p12, int p13, int p14, int p15, int p16, int p17) => 1</N:0>;

        System.Console.WriteLine(1);
    }
}");

            var compilation0 = CreateCompilation(source0.Tree, options: ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            var compilation1 = compilation0.WithSource(source1.Tree);

            var v0 = CompileAndVerify(compilation0, verify: Verification.Skipped);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);
            var reader0 = md0.MetadataReader;

            var method0 = compilation0.GetMember<MethodSymbol>("C.F");
            var method1 = compilation1.GetMember<MethodSymbol>("C.F");
            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "<>F`18", "C", "<>c");
            CheckNames(reader0, reader0.GetMethodDefNames(), ".ctor", "Invoke", "F", ".ctor", ".cctor", ".ctor", "<F>b__0_0");

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1, GetSyntaxMapFromMarkers(source0, source1))));

            // Verify delta metadata contains expected rows.
            using var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;
            var readers = new[] { reader0, reader1 };

            EncValidation.VerifyModuleMvid(1, reader0, reader1);

            CheckNames(readers, reader1.GetTypeDefNames());
            CheckNames(readers, reader1.GetMethodDefNames(), "F", "<F>b__0_0");

            diff1.VerifySynthesizedMembers(
                "C.<>c: {<>9__0_0, <F>b__0_0}",
                "C: {<>c}");
        }

        [Fact]
        public void Lambda_SynthesizedDelegate()
        {
            using var _ = new EditAndContinueTest()
                .AddBaseline(
                    source: """
                        class C
                        {
                            void F()
                            <N:0>{
                                var f = <N:1>(ref int a) => a</N:1>;
                            }</N:0>
                        }
                        """,
                    validator: g =>
                    {
                        g.VerifySynthesizedMembers(displayTypeKind: true,
                        [
                            "class C: {<>c}",
                            "class C.<>c: {<>9__0_0, <F>b__0_0}"
                        ]);

                        g.VerifySynthesizedTypes(
                            "<>F{00000001}<T1, TResult>");
                    })
                .AddGeneration(
                    // 1
                    source: """
                        class C
                        {
                            void F()
                            <N:0>{
                                var g = <N:2>(out byte a) => a = 1</N:2>;
                                var f = <N:1>(ref int a) => a</N:1>;
                            }</N:0>
                        }
                        """,
                        edits:
                        [
                            Edit(SemanticEditKind.Update, c => c.GetMember("C.F"), preserveLocalVariables: true),
                        ],
                        validator: g =>
                        {
                            g.VerifySynthesizedMembers(displayTypeKind: true,
                            [
                               "class C: {<>c}",
                               "class C.<>c: {<>9__0_0#1, <>9__0_0, <F>b__0_0#1, <F>b__0_0}" // new and reused lambdas
                            ]);

                            g.VerifySynthesizedTypes(
                                "<>F{00000001}<T1, TResult>",
                                "<>F{00000002}<T1, TResult>"); // new synthesized delegate is created

                            g.VerifyTypeDefNames("<>F{00000002}`2");
                            g.VerifyMethodDefNames("F", "<F>b__0_0", ".ctor", "Invoke", "<F>b__0_0#1");
                        })
                .AddGeneration(
                    // 2
                    source: """
                        class C
                        {
                            void F()
                            <N:0>{
                                var f = <N:1>(ref bool a, ref bool b) => a</N:1>;
                            }</N:0>
                        }
                        """,
                        edits:
                        [
                            Edit(SemanticEditKind.Update, c => c.GetMember("C.F"), preserveLocalVariables: true, rudeEdits: _ => new RuntimeRudeEdit("Parameter changed", 0x123)),
                        ],
                        validator: g =>
                        {
                            g.VerifySynthesizedMembers(displayTypeKind: true,
                            [
                                "System.Runtime.CompilerServices.HotReloadException",
                                "class C: {<>c}",
                                "class C.<>c: {<>9__0_0#2, <F>b__0_0#2, <>9__0_0#1, <>9__0_0, <F>b__0_0#1, <F>b__0_0}"
                            ]);

                            g.VerifySynthesizedTypes(
                                "<>F{00000001}<T1, TResult>",
                                "<>F{00000002}<T1, TResult>",
                                "<>F{00000009}<T1, T2, TResult>"); // new synthesized delegate is created

                            g.VerifyTypeDefNames("<>F{00000009}`3", "HotReloadException");
                            g.VerifyMethodDefNames("F", "<F>b__0_0", "<F>b__0_0#1", ".ctor", "Invoke", ".ctor", "<F>b__0_0#2");
                        })
                .AddGeneration(
                    // 3
                    source: """
                        class C
                        {
                            void F()
                            <N:0>{
                                var f = <N:1>(ref bool a, ref bool b) => a</N:1>;
                                var g = <N:2>(ref bool a, ref bool b, bool c) => a</N:2>;
                            }</N:0>
                        }
                        """,
                        edits:
                        [
                            Edit(SemanticEditKind.Update, c => c.GetMember("C.F"), preserveLocalVariables: true),
                        ],
                        validator: g =>
                        {
                            g.VerifySynthesizedMembers(displayTypeKind: true,
                            [
                                "System.Runtime.CompilerServices.HotReloadException",
                                "class C: {<>c}",
                                "class C.<>c: {<>9__0_0#2, <>9__0_1#3, <F>b__0_0#2, <F>b__0_1#3, <>9__0_0#1, <>9__0_0, <F>b__0_0#1, <F>b__0_0}"
                            ]);

                            g.VerifySynthesizedTypes(
                                "<>F{00000001}<T1, TResult>",
                                "<>F{00000002}<T1, TResult>",
                                "<>F{00000009}<T1, T2, TResult>",
                                "<>F{00000009}<T1, T2, T3, TResult>"); // new synthesized delegate is created

                            g.VerifyTypeDefNames("<>F{00000009}`4");
                            g.VerifyMethodDefNames("F", "<F>b__0_0#2", ".ctor", "Invoke", "<F>b__0_1#3");
                        })
                .Verify();
        }

        [Fact]
        public void Lambda_SynthesizedDelegate_WithIndexedName()
        {
            using var _ = new EditAndContinueTest()
                .AddBaseline(
                    source: """
                        class C
                        {
                            void F()
                            <N:0>{
                                var f = <N:1>(int a = 1) => a</N:1>;
                            }</N:0>
                        }
                        """,
                    validator: g =>
                    {
                        g.VerifySynthesizedMembers(displayTypeKind: true,
                        [
                            "class C: {<>c}",
                            "class C.<>c: {<>9__0_0, <F>b__0_0}"
                        ]);

                        g.VerifySynthesizedTypes(
                            "<>f__AnonymousDelegate0<T1, TResult>");
                    })
                .AddGeneration(
                    // 1
                    source: """
                        class C
                        {
                            void F()
                            <N:0>{
                                var g = <N:2>(int a = 1) => a + 1</N:2>;
                                var f = <N:1>(int a = 2) => a</N:1>;
                            }</N:0>
                        }
                        """,
                        edits:
                        [
                            Edit(SemanticEditKind.Update, c => c.GetMember("C.F"), preserveLocalVariables: true),
                        ],
                        validator: g =>
                        {
                            g.VerifySynthesizedMembers(displayTypeKind: true,
                            [
                                "class C: {<>c}",
                                "class C.<>c: {<>9__0_0#1, <>9__0_0, <F>b__0_0#1, <F>b__0_0}" // new lambda is created
                            ]);

                            g.VerifySynthesizedTypes(
                                "<>f__AnonymousDelegate0<T1, TResult>",
                                "<>f__AnonymousDelegate1<T1, TResult>"); // new synthesized delegate is created

                            g.VerifyTypeDefNames("<>f__AnonymousDelegate1`2");
                            g.VerifyMethodDefNames("F", "<F>b__0_0", ".ctor", "Invoke", "<F>b__0_0#1");
                        })
                .AddGeneration(
                    // 2
                    source: """
                        class C
                        {
                            void F()
                            <N:0>{
                                var f = <N:1>(int a = 3) => a</N:1>;
                            }</N:0>
                        }
                        """,
                        edits:
                        [
                            Edit(SemanticEditKind.Update, c => c.GetMember("C.F"), preserveLocalVariables: true, rudeEdits: _ => new RuntimeRudeEdit("Parameter changed", 0x123)),
                        ],
                        validator: g =>
                        {
                            g.VerifySynthesizedMembers(displayTypeKind: true,
                            [
                                "System.Runtime.CompilerServices.HotReloadException",
                                "class C: {<>c}",
                                "class C.<>c: {<>9__0_0#2, <F>b__0_0#2, <>9__0_0#1, <>9__0_0, <F>b__0_0#1, <F>b__0_0}"
                            ]);

                            g.VerifySynthesizedTypes(
                                "<>f__AnonymousDelegate0<T1, TResult>",
                                "<>f__AnonymousDelegate1<T1, TResult>",
                                "<>f__AnonymousDelegate2<T1, TResult>"); // new synthesized delegate is created

                            g.VerifyTypeDefNames("<>f__AnonymousDelegate2`2", "HotReloadException");
                            g.VerifyMethodDefNames("F", "<F>b__0_0", "<F>b__0_0#1", ".ctor", "Invoke", ".ctor", "<F>b__0_0#2");
                        })
                .AddGeneration(
                    // 3
                    source: """
                        class C
                        {
                            void F()
                            <N:0>{
                                var f = <N:1>(int a = 3) => a</N:1>;
                                var g = <N:2>(int a = 1, int b = 2) => a</N:2>;
                            }</N:0>
                        }
                        """,
                        edits:
                        [
                            Edit(SemanticEditKind.Update, c => c.GetMember("C.F"), preserveLocalVariables: true),
                        ],
                        validator: g =>
                        {
                            g.VerifySynthesizedMembers(displayTypeKind: true,
                            [
                                "System.Runtime.CompilerServices.HotReloadException",
                                "class C: {<>c}",
                                "class C.<>c: {<>9__0_0#2, <>9__0_1#3, <F>b__0_0#2, <F>b__0_1#3, <>9__0_0#1, <>9__0_0, <F>b__0_0#1, <F>b__0_0}"
                            ]);

                            g.VerifySynthesizedTypes(
                                "<>f__AnonymousDelegate0<T1, TResult>",
                                "<>f__AnonymousDelegate1<T1, TResult>",
                                "<>f__AnonymousDelegate2<T1, TResult>",
                                "<>f__AnonymousDelegate3<T1, T2, TResult>"); // new synthesized delegate is created

                            g.VerifyTypeDefNames("<>f__AnonymousDelegate3`3");
                            g.VerifyMethodDefNames("F", "<F>b__0_0#2", ".ctor", "Invoke", "<F>b__0_1#3");
                        })
                .Verify();
        }

        [Fact]
        public void Lambda_Delete()
        {
            using var _ = new EditAndContinueTest()
                .AddBaseline(
                    source: """
                        using System;

                        class C
                        {
                            void F()
                            {
                                _ = new Action(() => Console.WriteLine(1));
                                _ = new Action(<N:0>() => Console.WriteLine(2)</N:0>);
                            } 
                        }
                        """,
                    validator: g =>
                    {
                        g.VerifyTableSize(TableIndex.MethodDef, 6);
                    })
                .AddGeneration(
                    source: """
                        using System;
                        
                        class C
                        {
                            void F()
                            {
                                _ = new Action(<N:0>() => Console.WriteLine(2)</N:0>);
                            } 
                        }
                        """,
                    edits: new[]
                    {
                        Edit(SemanticEditKind.Update, c => c.GetMember("C.F"), preserveLocalVariables: true),
                    },
                    validator: g =>
                    {
                        g.VerifySynthesizedMembers(
                            "System.Runtime.CompilerServices.HotReloadException",
                            "C: {<>c}",
                            "C.<>c: {<>9__0_1, <F>b__0_1}");

                        g.VerifyTypeDefNames("HotReloadException");
                        g.VerifyMethodDefNames("F", "<F>b__0_0", "<F>b__0_1", ".ctor");
                        g.VerifyTypeRefNames("Object", "CompilerGeneratedAttribute", "Exception", "Action`1", "Action", "Console");
                        g.VerifyMemberRefNames(".ctor", ".ctor", "WriteLine", ".ctor", "Invoke");

                        g.VerifyEncLogDefinitions(new[]
                        {
                            Row(1, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                            Row(4, TableIndex.TypeDef, EditAndContinueOperation.Default),
                            Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(4, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(5, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(6, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                            Row(7, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(5, TableIndex.CustomAttribute, EditAndContinueOperation.Default)
                        });
                        g.VerifyEncMapDefinitions(new[]
                        {
                            Handle(4, TableIndex.TypeDef),
                            Handle(4, TableIndex.Field),
                            Handle(5, TableIndex.Field),
                            Handle(1, TableIndex.MethodDef),
                            Handle(5, TableIndex.MethodDef),
                            Handle(6, TableIndex.MethodDef),
                            Handle(7, TableIndex.MethodDef),
                            Handle(5, TableIndex.CustomAttribute),
                            Handle(1, TableIndex.StandAloneSig)
                        });

                        g.VerifyIL("""
                            F
                            {
                              // Code size       30 (0x1e)
                              .maxstack  8
                              IL_0000:  nop
                              IL_0001:  ldsfld     0x04000003
                              IL_0006:  brtrue.s   IL_001d
                              IL_0008:  ldsfld     0x04000001
                              IL_000d:  ldftn      0x06000006
                              IL_0013:  newobj     0x0A000009
                              IL_0018:  stsfld     0x04000003
                              IL_001d:  ret
                            }
                            <F>b__0_0
                            {
                              // Code size       12 (0xc)
                              .maxstack  8
                              IL_0000:  ldstr      0x70000005
                              IL_0005:  ldc.i4.m1
                              IL_0006:  newobj     0x06000007
                              IL_000b:  throw
                            }
                            <F>b__0_1
                            {
                              // Code size        8 (0x8)
                              .maxstack  8
                              IL_0000:  ldc.i4.2
                              IL_0001:  call       0x0A00000A
                              IL_0006:  nop
                              IL_0007:  ret
                            }
                            .ctor
                            {
                              // Code size       33 (0x21)
                              .maxstack  2
                              IL_0000:  ldarg.0
                              IL_0001:  ldarg.1
                              IL_0002:  call       0x0A00000B
                              IL_0007:  nop
                              IL_0008:  ldarg.0
                              IL_0009:  ldarg.2
                              IL_000a:  stfld      0x04000004
                              IL_000f:  ldsfld     0x04000005
                              IL_0014:  dup
                              IL_0015:  stloc.0
                              IL_0016:  brfalse.s  IL_0020
                              IL_0018:  ldloc.0
                              IL_0019:  ldarg.0
                              IL_001a:  callvirt   0x0A00000C
                              IL_001f:  nop
                              IL_0020:  ret
                            }
                            """);
                    })
                .Verify();
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/67243")]
        public void SynthesizedDelegates_Ordering()
        {
            using var _ = new EditAndContinueTest(options: TestOptions.DebugExe)
                .AddBaseline(
                    source: """
                    var <N:0>g1 = C.G1</N:0>;
                    var <N:1>g2 = C.G2</N:1>;
                    
                    class C
                    {
                       public static void G1(bool a = true) { }
                       public static void G2(bool a = false) { }
                    }
                    """,
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("<Module>", "<>f__AnonymousDelegate0`1", "<>f__AnonymousDelegate1`1", "Program", "C", "<>O");

                        g.VerifyMethodBody("<top-level-statements-entry-point>", """
                        {
                          // Code size       57 (0x39)
                          .maxstack  2
                          .locals init (<>f__AnonymousDelegate0<bool> V_0, //g1
                                        <>f__AnonymousDelegate1<bool> V_1) //g2
                          // sequence point: var      g1 = C.G1      ;
                          IL_0000:  ldsfld     "<anonymous delegate> Program.<>O.<0>__G1"
                          IL_0005:  dup
                          IL_0006:  brtrue.s   IL_001b
                          IL_0008:  pop
                          IL_0009:  ldnull
                          IL_000a:  ldftn      "void C.G1(bool)"
                          IL_0010:  newobj     "<>f__AnonymousDelegate0<bool>..ctor(object, System.IntPtr)"
                          IL_0015:  dup
                          IL_0016:  stsfld     "<anonymous delegate> Program.<>O.<0>__G1"
                          IL_001b:  stloc.0
                          // sequence point: var      g2 = C.G2      ;
                          IL_001c:  ldsfld     "<anonymous delegate> Program.<>O.<1>__G2"
                          IL_0021:  dup
                          IL_0022:  brtrue.s   IL_0037
                          IL_0024:  pop
                          IL_0025:  ldnull
                          IL_0026:  ldftn      "void C.G2(bool)"
                          IL_002c:  newobj     "<>f__AnonymousDelegate1<bool>..ctor(object, System.IntPtr)"
                          IL_0031:  dup
                          IL_0032:  stsfld     "<anonymous delegate> Program.<>O.<1>__G2"
                          IL_0037:  stloc.1
                          IL_0038:  ret
                        }
                        """);
                    })
                .AddGeneration(
                    source: """
                    var <N:1>g2 = C.G2</N:1>;
                    var <N:0>g1 = C.G1</N:0>;

                    class C
                    {
                       public static void G1(bool a = true) { }
                       public static void G2(bool a = false) { }
                    }
                    """,
                    edits: new[]
                    {
                        Edit(SemanticEditKind.Update, c => c.GetMember("Program.<Main>$"), preserveLocalVariables: true),
                    },
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("<>O#1");

                        g.VerifyIL("<top-level-statements-entry-point>", """
                        {
                          // Code size       57 (0x39)
                          .maxstack  2
                          .locals init (<>f__AnonymousDelegate0<bool> V_0, //g1
                                        <>f__AnonymousDelegate1<bool> V_1) //g2
                          IL_0000:  ldsfld     "<anonymous delegate> Program.<>O#1.<0>__G2"
                          IL_0005:  dup
                          IL_0006:  brtrue.s   IL_001b
                          IL_0008:  pop
                          IL_0009:  ldnull
                          IL_000a:  ldftn      "void C.G2(bool)"
                          IL_0010:  newobj     "<>f__AnonymousDelegate1<bool>..ctor(object, System.IntPtr)"
                          IL_0015:  dup
                          IL_0016:  stsfld     "<anonymous delegate> Program.<>O#1.<0>__G2"
                          IL_001b:  stloc.1
                          IL_001c:  ldsfld     "<anonymous delegate> Program.<>O#1.<1>__G1"
                          IL_0021:  dup
                          IL_0022:  brtrue.s   IL_0037
                          IL_0024:  pop
                          IL_0025:  ldnull
                          IL_0026:  ldftn      "void C.G1(bool)"
                          IL_002c:  newobj     "<>f__AnonymousDelegate0<bool>..ctor(object, System.IntPtr)"
                          IL_0031:  dup
                          IL_0032:  stsfld     "<anonymous delegate> Program.<>O#1.<1>__G1"
                          IL_0037:  stloc.0
                          IL_0038:  ret
                        }
                        """);
                    })
                .Verify();
        }

        [Fact]
        public void SynthesizedDelegates_Delete()
        {
            using var _ = new EditAndContinueTest(options: TestOptions.DebugExe)
                .AddBaseline(
                    source: """
                    var <N:0>g1 = C.G1</N:0>;
                    var <N:1>g2 = C.G2</N:1>;
                    
                    class C
                    {
                       public static void G1(bool a = true) { }
                       public static void G2(bool a = false) { }
                    }
                    """,
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("<Module>", "<>f__AnonymousDelegate0`1", "<>f__AnonymousDelegate1`1", "Program", "C", "<>O");

                        g.VerifyMethodBody("<top-level-statements-entry-point>", """
                        {
                          // Code size       57 (0x39)
                          .maxstack  2
                          .locals init (<>f__AnonymousDelegate0<bool> V_0, //g1
                                        <>f__AnonymousDelegate1<bool> V_1) //g2
                          // sequence point: var      g1 = C.G1      ;
                          IL_0000:  ldsfld     "<anonymous delegate> Program.<>O.<0>__G1"
                          IL_0005:  dup
                          IL_0006:  brtrue.s   IL_001b
                          IL_0008:  pop
                          IL_0009:  ldnull
                          IL_000a:  ldftn      "void C.G1(bool)"
                          IL_0010:  newobj     "<>f__AnonymousDelegate0<bool>..ctor(object, System.IntPtr)"
                          IL_0015:  dup
                          IL_0016:  stsfld     "<anonymous delegate> Program.<>O.<0>__G1"
                          IL_001b:  stloc.0
                          // sequence point: var      g2 = C.G2      ;
                          IL_001c:  ldsfld     "<anonymous delegate> Program.<>O.<1>__G2"
                          IL_0021:  dup
                          IL_0022:  brtrue.s   IL_0037
                          IL_0024:  pop
                          IL_0025:  ldnull
                          IL_0026:  ldftn      "void C.G2(bool)"
                          IL_002c:  newobj     "<>f__AnonymousDelegate1<bool>..ctor(object, System.IntPtr)"
                          IL_0031:  dup
                          IL_0032:  stsfld     "<anonymous delegate> Program.<>O.<1>__G2"
                          IL_0037:  stloc.1
                          IL_0038:  ret
                        }
                        """);
                    })
                .AddGeneration(
                    source: """
                    var <N:1>g2 = C.G2</N:1>;

                    class C
                    {
                       public static void G1(bool a = true) { }
                       public static void G2(bool a = false) { }
                    }
                    """,
                    edits: new[]
                    {
                        Edit(SemanticEditKind.Update, c => c.GetMember("Program.<Main>$"), preserveLocalVariables: true),
                    },
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("<>O#1");

                        g.VerifyIL("<top-level-statements-entry-point>", """
                        {
                          // Code size       29 (0x1d)
                          .maxstack  2
                          .locals init ([unchanged] V_0,
                                        <>f__AnonymousDelegate1<bool> V_1) //g2
                          IL_0000:  ldsfld     "<anonymous delegate> Program.<>O#1.<0>__G2"
                          IL_0005:  dup
                          IL_0006:  brtrue.s   IL_001b
                          IL_0008:  pop
                          IL_0009:  ldnull
                          IL_000a:  ldftn      "void C.G2(bool)"
                          IL_0010:  newobj     "<>f__AnonymousDelegate1<bool>..ctor(object, System.IntPtr)"
                          IL_0015:  dup
                          IL_0016:  stsfld     "<anonymous delegate> Program.<>O#1.<0>__G2"
                          IL_001b:  stloc.1
                          IL_001c:  ret
                        }
                        """);
                    })
                .Verify();
        }

        [WorkItem(962219, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/962219")]
        [Fact]
        public void PartialMethod()
        {
            using var _ = new EditAndContinueTest(options: TestOptions.DebugDll)
               .AddBaseline(
                   source: """
                   partial class C
                   {
                       static partial void M1();
                       static partial void M2();
                       static partial void M3();
                       static partial void M1() { }
                       static partial void M2() { }
                   }
                   """,
                   validator: v =>
                   {
                       v.VerifyMethodDefNames("M1", "M2", ".ctor");
                   })
                .AddGeneration(
                    source: """
                    partial class C
                    {
                        static partial void M1();
                        static partial void M2();
                        static partial void M3();
                        static partial void M1() { }
                        static partial void M2() { }
                    }
                    """,
                    edits:
                    [
                        Edit(SemanticEditKind.Update, c => c.GetMember<IMethodSymbol>("C.M2").PartialImplementationPart),
                    ],
                    validator: v =>
                    {
                        v.VerifyMethodDefNames("M2");

                        v.VerifyEncLogDefinitions(
                        [
                            Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default)
                        ]);

                        v.VerifyEncMapDefinitions(
                        [
                            Handle(2, TableIndex.MethodDef)
                        ]);
                    })
                .Verify();
        }

        [WorkItem(60804, "https://github.com/dotnet/roslyn/issues/60804")]
        [Fact]
        public void PartialMethod_WithLambda()
        {
            using var _ = new EditAndContinueTest()
                .AddBaseline(
                    source: @"
partial class C
{
    partial void M();

    partial void M()
    {
        var y = 4;
        var x = () => y + 4;
    }
}
",
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("<Module>", "C", "<>c__DisplayClass0_0");
                        g.VerifyMethodDefNames("M", ".ctor", ".ctor", "<M>b__0");
                    })

                .AddGeneration(
                    source: @"
partial class C
{
    partial void M();

    partial void M()
    {
        var y = 5;
        var x = () => y + 4;
    }
}
",
                    edits: new[]
                    {
                        // note: lambda is not syntax-mapped to the previous generation
                        Edit(SemanticEditKind.Update, c => c.GetMember<IMethodSymbol>("C.M").PartialImplementationPart)
                    },
                    validator: g =>
                    {
                        g.VerifyMethodDefNames("M", "<M>b__0", ".ctor", ".ctor", "<M>b__0#1");

                        g.VerifyIL("""
                            M
                            {
                              // Code size       28 (0x1c)
                              .maxstack  2
                              IL_0000:  newobj     0x06000006
                              IL_0005:  stloc.0
                              IL_0006:  nop
                              IL_0007:  ldloc.0
                              IL_0008:  ldc.i4.5
                              IL_0009:  stfld      0x04000004
                              IL_000e:  ldloc.0
                              IL_000f:  ldftn      0x06000007
                              IL_0015:  newobj     0x0A000008
                              IL_001a:  stloc.1
                              IL_001b:  ret
                            }
                            <M>b__0
                            {
                              // Code size       12 (0xc)
                              .maxstack  8
                              IL_0000:  ldstr      0x70000005
                              IL_0005:  ldc.i4.m1
                              IL_0006:  newobj     0x06000005
                              IL_000b:  throw
                            }
                            .ctor
                            {
                              // Code size       33 (0x21)
                              .maxstack  2
                              IL_0000:  ldarg.0
                              IL_0001:  ldarg.1
                              IL_0002:  call       0x0A000009
                              IL_0007:  nop
                              IL_0008:  ldarg.0
                              IL_0009:  ldarg.2
                              IL_000a:  stfld      0x04000002
                              IL_000f:  ldsfld     0x04000003
                              IL_0014:  dup
                              IL_0015:  stloc.0
                              IL_0016:  brfalse.s  IL_0020
                              IL_0018:  ldloc.0
                              IL_0019:  ldarg.0
                              IL_001a:  callvirt   0x0A00000A
                              IL_001f:  nop
                              IL_0020:  ret
                            }
                            .ctor
                            {
                              // Code size        8 (0x8)
                              .maxstack  8
                              IL_0000:  ldarg.0
                              IL_0001:  call       0x0A00000B
                              IL_0006:  nop
                              IL_0007:  ret
                            }
                            <M>b__0#1
                            {
                              // Code size        9 (0x9)
                              .maxstack  8
                              IL_0000:  ldarg.0
                              IL_0001:  ldfld      0x04000004
                              IL_0006:  ldc.i4.4
                              IL_0007:  add
                              IL_0008:  ret
                            }
                            """);
                    })

                .Verify();
        }

        [Fact]
        public void PartialProperty()
        {
            using var _ = new EditAndContinueTest(options: TestOptions.DebugDll)
               .AddBaseline(
                   source: """
                   partial class C
                   {
                       partial int P { get; }
                       partial int P => 1;
                   }
                   """,
                   validator: v =>
                   {
                       v.VerifyMethodDefNames("get_P", ".ctor");
                   })
                .AddGeneration(
                    source: """
                    partial class C
                    {
                        [System.Obsolete]partial int P { get; }
                        partial int P => 1;
                    }
                    """,
                    edits:
                    [
                        Edit(SemanticEditKind.Update, c => c.GetMember<IPropertySymbol>("C.P").PartialImplementationPart),
                    ],
                    validator: v =>
                    {
                        v.VerifyMethodDefNames();

                        v.VerifyEncLogDefinitions(
                        [
                            Row(1, TableIndex.Property, EditAndContinueOperation.Default),
                            Row(4, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(2, TableIndex.MethodSemantics, EditAndContinueOperation.Default)
                        ]);

                        v.VerifyEncMapDefinitions(
                        [
                            Handle(4, TableIndex.CustomAttribute),
                            Handle(1, TableIndex.Property),
                            Handle(2, TableIndex.MethodSemantics)
                        ]);
                    })
                .Verify();
        }

        [Fact]
        public void PartialProperty_Accessor()
        {
            using var _ = new EditAndContinueTest(options: TestOptions.DebugDll)
               .AddBaseline(
                   source: """
                   partial class C
                   {
                       partial int P { get; }
                       partial int P => 1;
                   }
                   """,
                   validator: v =>
                   {
                       v.VerifyMethodDefNames("get_P", ".ctor");
                   })
                .AddGeneration(
                    source: """
                    partial class C
                    {
                        partial int P { get; }
                        partial int P => 2;
                    }
                    """,
                    edits:
                    [
                        Edit(SemanticEditKind.Update, c => c.GetMember<IMethodSymbol>("C.get_P").PartialImplementationPart),
                    ],
                    validator: v =>
                    {
                        v.VerifyMethodDefNames("get_P");

                        v.VerifyEncLogDefinitions(
                        [
                            Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default)
                        ]);

                        v.VerifyEncMapDefinitions(
                        [
                            Handle(1, TableIndex.MethodDef)
                        ]);
                    })
                .Verify();
        }

        [Fact]
        public void Method_WithAttributes_Add()
        {
            var source0 =
@"class C
{
    static void Main() { }
}";
            var source1 =
@"class C
{
    static void Main() { }
    [System.ComponentModel.Description(""The F method"")]
    static string F() { return string.Empty; }
}";
            var compilation0 = CreateCompilation(source0, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), options: TestOptions.DebugExe, targetFramework: TargetFramework.NetStandard20);
            var compilation1 = compilation0.WithSource(source1);

            // Verify full metadata contains expected rows.
            var bytes0 = compilation0.EmitToArray();
            using var md0 = ModuleMetadata.CreateFromImage(bytes0);
            var reader0 = md0.MetadataReader;

            CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "C");
            CheckNames(reader0, reader0.GetMethodDefNames(), "Main", ".ctor");
            CheckNames(reader0, reader0.GetMemberRefNames(), /*CompilationRelaxationsAttribute.*/".ctor", /*RuntimeCompatibilityAttribute.*/".ctor", /*Object.*/".ctor", /*DebuggableAttribute*/".ctor");

            Assert.Equal(3, reader0.CustomAttributes.Count);

            var generation0 = CreateInitialBaseline(compilation0,
                md0,
                EmptyLocalsProvider);
            var method1 = compilation1.GetMember<MethodSymbol>("C.F");

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Insert, null, method1)));

            // Verify delta metadata contains expected rows.
            using var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;
            var readers = new[] { reader0, reader1 };

            EncValidation.VerifyModuleMvid(1, reader0, reader1);

            CheckNames(readers, reader1.GetTypeDefNames());
            CheckNames(readers, reader1.GetMethodDefNames(), "F");
            CheckNames(readers, reader1.GetMemberRefNames(), /*DescriptionAttribute*/".ctor", /*String.*/"Empty");

            Assert.Equal(1, reader1.CustomAttributes.Count);

            CheckEncLog(reader1,
                Row(2, TableIndex.AssemblyRef, EditAndContinueOperation.Default),
                Row(5, TableIndex.MemberRef, EditAndContinueOperation.Default),
                Row(6, TableIndex.MemberRef, EditAndContinueOperation.Default),
                Row(6, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(7, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(8, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(1, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(2, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                Row(3, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(4, TableIndex.CustomAttribute, EditAndContinueOperation.Default)); // Row 4, a new attribute

            CheckEncMap(reader1,
                Handle(6, TableIndex.TypeRef),
                Handle(7, TableIndex.TypeRef),
                Handle(8, TableIndex.TypeRef),
                Handle(3, TableIndex.MethodDef),
                Handle(5, TableIndex.MemberRef),
                Handle(6, TableIndex.MemberRef),
                Handle(4, TableIndex.CustomAttribute),
                Handle(1, TableIndex.StandAloneSig),
                Handle(2, TableIndex.AssemblyRef));
        }

        [Fact]
        public void ModifyMethod_ParameterAttributes()
        {
            var source0 =
@"class C
{
    static void Main() { }
    static string F(string input, int a) { return input; }
}";
            var source1 =
@"class C
{
    static void Main() { }
    static string F([System.ComponentModel.Description(""input"")]string input, int a) { return input; }
    static void G(string input) { }
}";
            var source2 =
@"class C
{
    static void Main() { }
    static string F([System.ComponentModel.Description(""input"")]string input, int a) { return input; }
    static void G([System.ComponentModel.Description(""input"")]string input) { }
}";

            var compilation0 = CreateCompilation(source0, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), options: TestOptions.DebugExe, targetFramework: TargetFramework.NetStandard20);
            var compilation1 = compilation0.WithSource(source1);
            var compilation2 = compilation1.WithSource(source2);

            var methodF0 = compilation0.GetMember<MethodSymbol>("C.F");
            var methodF1 = compilation1.GetMember<MethodSymbol>("C.F");
            var methodG1 = compilation1.GetMember<MethodSymbol>("C.G");
            var methodG2 = compilation2.GetMember<MethodSymbol>("C.G");

            // Verify full metadata contains expected rows.
            var bytes0 = compilation0.EmitToArray();
            using var md0 = ModuleMetadata.CreateFromImage(bytes0);
            var reader0 = md0.MetadataReader;

            CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "C");
            CheckNames(reader0, reader0.GetMethodDefNames(), "Main", "F", ".ctor");
            CheckNames(reader0, reader0.GetMemberRefNames(), /*CompilationRelaxationsAttribute.*/".ctor", /*RuntimeCompatibilityAttribute.*/".ctor", /*Object.*/".ctor", /*DebuggableAttribute*/".ctor");
            CheckNames(reader0, reader0.GetParameterDefNames(), "input", "a");

            CheckAttributes(reader0,
                new CustomAttributeRow(Handle(1, TableIndex.Assembly), Handle(1, TableIndex.MemberRef)),
                new CustomAttributeRow(Handle(1, TableIndex.Assembly), Handle(2, TableIndex.MemberRef)),
                new CustomAttributeRow(Handle(1, TableIndex.Assembly), Handle(3, TableIndex.MemberRef)));

            var generation0 = CreateInitialBaseline(compilation0,
               md0,
               EmptyLocalsProvider);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, methodF0, methodF1),
                    SemanticEdit.Create(SemanticEditKind.Insert, null, methodG1)));

            // Verify delta metadata contains expected rows.
            using var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;
            var readers = new[] { reader0, reader1 };

            EncValidation.VerifyModuleMvid(1, reader0, reader1);

            CheckNames(readers, reader1.GetTypeDefNames());
            CheckNames(readers, reader1.GetMethodDefNames(), "F", "G");
            CheckNames(readers, reader1.GetMemberRefNames(), /*DescriptionAttribute*/".ctor");
            CheckNames(readers, reader1.GetParameterDefNames(), "input", "a", "input");

            CheckAttributes(reader1,
                 new CustomAttributeRow(Handle(1, TableIndex.Param), Handle(5, TableIndex.MemberRef)));

            CheckEncLog(reader1,
                Row(2, TableIndex.AssemblyRef, EditAndContinueOperation.Default),
                Row(5, TableIndex.MemberRef, EditAndContinueOperation.Default),
                Row(6, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(7, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(2, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(2, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),             // New method, G
                Row(1, TableIndex.Param, EditAndContinueOperation.Default),                 // Update existing param
                Row(2, TableIndex.Param, EditAndContinueOperation.Default),                 // Update existing param
                Row(4, TableIndex.MethodDef, EditAndContinueOperation.AddParameter),        // New param on method, G
                Row(3, TableIndex.Param, EditAndContinueOperation.Default),                 // Support for the above
                Row(4, TableIndex.CustomAttribute, EditAndContinueOperation.Default));

            CheckEncMap(reader1,
                Handle(6, TableIndex.TypeRef),
                Handle(7, TableIndex.TypeRef),
                Handle(2, TableIndex.MethodDef),
                Handle(4, TableIndex.MethodDef),
                Handle(1, TableIndex.Param),
                Handle(2, TableIndex.Param),
                Handle(3, TableIndex.Param),
                Handle(5, TableIndex.MemberRef),
                Handle(4, TableIndex.CustomAttribute),
                Handle(2, TableIndex.StandAloneSig),
                Handle(2, TableIndex.AssemblyRef));

            var diff2 = compilation2.EmitDifference(
              diff1.NextGeneration,
              ImmutableArray.Create(
                  SemanticEdit.Create(SemanticEditKind.Update, methodG1, methodG2)));

            using var md2 = diff2.GetMetadata();
            var reader2 = md2.Reader;
            readers = new[] { reader0, reader1, reader2 };

            EncValidation.VerifyModuleMvid(2, reader1, reader2);

            CheckNames(readers, reader2.GetTypeDefNames());
            CheckNames(readers, reader2.GetMethodDefNames(), "G");
            CheckNames(readers, reader2.GetMemberRefNames(), /*DescriptionAttribute*/".ctor");
            CheckNames(readers, reader2.GetParameterDefNames(), "input");

            CheckAttributes(reader2,
                 new CustomAttributeRow(Handle(3, TableIndex.Param), Handle(6, TableIndex.MemberRef)));

            CheckEncLog(reader2,
                Row(3, TableIndex.AssemblyRef, EditAndContinueOperation.Default),
                Row(6, TableIndex.MemberRef, EditAndContinueOperation.Default),
                Row(8, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(9, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(3, TableIndex.Param, EditAndContinueOperation.Default),             // Update existing param, from the first delta
                Row(5, TableIndex.CustomAttribute, EditAndContinueOperation.Default));

            CheckEncMap(reader2,
                Handle(8, TableIndex.TypeRef),
                Handle(9, TableIndex.TypeRef),
                Handle(4, TableIndex.MethodDef),
                Handle(3, TableIndex.Param),
                Handle(6, TableIndex.MemberRef),
                Handle(5, TableIndex.CustomAttribute),
                Handle(3, TableIndex.AssemblyRef));
        }

        [Fact]
        public void ModifyDelegateInvokeMethod_AddAttributes()
        {
            var source0 = @"
class A : System.Attribute { }
delegate void D(int x);
";
            var source1 = @"
class A : System.Attribute { }
delegate void D([A]int x);
";

            var compilation0 = CreateCompilation(source0, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), options: TestOptions.DebugDll, targetFramework: TargetFramework.NetStandard20);
            var compilation1 = compilation0.WithSource(source1);

            var invoke0 = compilation0.GetMember<MethodSymbol>("D.Invoke");
            var beginInvoke0 = compilation0.GetMember<MethodSymbol>("D.BeginInvoke");
            var invoke1 = compilation1.GetMember<MethodSymbol>("D.Invoke");
            var beginInvoke1 = compilation1.GetMember<MethodSymbol>("D.BeginInvoke");

            // Verify full metadata contains expected rows.
            var bytes0 = compilation0.EmitToArray();
            using var md0 = ModuleMetadata.CreateFromImage(bytes0);
            var reader0 = md0.MetadataReader;

            var generation0 = CreateInitialBaseline(compilation0, md0, EmptyLocalsProvider);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, invoke0, invoke1),
                    SemanticEdit.Create(SemanticEditKind.Update, beginInvoke0, beginInvoke1)));

            // Verify delta metadata contains expected rows.
            using var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;
            var readers = new[] { reader0, reader1 };

            CheckNames(readers, reader1.GetMethodDefNames(), "Invoke", "BeginInvoke");
            CheckNames(readers, reader1.GetParameterDefNames(), "x", "x", "callback", "object");

            CheckAttributes(reader1,
                new CustomAttributeRow(Handle(3, TableIndex.Param), Handle(1, TableIndex.MethodDef)),
                new CustomAttributeRow(Handle(4, TableIndex.Param), Handle(1, TableIndex.MethodDef)));

            CheckEncLog(reader1,
                Row(2, TableIndex.AssemblyRef, EditAndContinueOperation.Default),
                Row(10, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(11, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(12, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(13, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(3, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(3, TableIndex.Param, EditAndContinueOperation.Default),             // Updating existing parameter defs
                Row(4, TableIndex.Param, EditAndContinueOperation.Default),
                Row(5, TableIndex.Param, EditAndContinueOperation.Default),
                Row(6, TableIndex.Param, EditAndContinueOperation.Default),
                Row(4, TableIndex.CustomAttribute, EditAndContinueOperation.Default),   // Adding new custom attribute rows
                Row(5, TableIndex.CustomAttribute, EditAndContinueOperation.Default));

            CheckEncMap(reader1,
                Handle(10, TableIndex.TypeRef),
                Handle(11, TableIndex.TypeRef),
                Handle(12, TableIndex.TypeRef),
                Handle(13, TableIndex.TypeRef),
                Handle(3, TableIndex.MethodDef),
                Handle(4, TableIndex.MethodDef),
                Handle(3, TableIndex.Param),
                Handle(4, TableIndex.Param),
                Handle(5, TableIndex.Param),
                Handle(6, TableIndex.Param),
                Handle(4, TableIndex.CustomAttribute),
                Handle(5, TableIndex.CustomAttribute),
                Handle(2, TableIndex.AssemblyRef));
        }

        [Fact]
        public void TypePropertyField_Attributes()
        {
            var common = """
            using System;
            class A1 : Attribute { }
            class A2 : Attribute { }
            class A3 : Attribute { }
            class A4 : Attribute { }
            class A5 : Attribute { }
            class A6 : Attribute { }
            class A7 : Attribute { }
            class A8 : Attribute { }
            class A9 : Attribute { }
            class A10 : Attribute { }
            class A11 : Attribute { }
            class A12 : Attribute { }
            """;

            using var _ = new EditAndContinueTest()
                .AddBaseline(
                    source: common + """
                    enum E
                    {
                        A
                    }

                    class C
                    {
                        private int _x;
                        public int X { get; }
                    }

                    delegate int D(int x);
                    """,
                    validator: g =>
                    {
                        g.VerifyCustomAttributes(
                        [
                            new CustomAttributeRow(Handle(1, TableIndex.Assembly), Handle(1, TableIndex.MemberRef)),
                            new CustomAttributeRow(Handle(1, TableIndex.Assembly), Handle(2, TableIndex.MemberRef)),
                            new CustomAttributeRow(Handle(1, TableIndex.Assembly), Handle(3, TableIndex.MemberRef)),
                            new CustomAttributeRow(Handle(4, TableIndex.Field), Handle(4, TableIndex.MemberRef)),
                            new CustomAttributeRow(Handle(4, TableIndex.Field), Handle(5, TableIndex.MemberRef)),
                            new CustomAttributeRow(Handle(13, TableIndex.MethodDef), Handle(4, TableIndex.MemberRef)),
                        ]);
                    })

                .AddGeneration(
                    source: common + """

                    [A1]
                    enum E
                    {
                        [A2]
                        A
                    }

                    [A3]
                    class C
                    {
                        [A4]
                        private int _x;
                        [A5]
                        public int X { get; }
                    }

                    [A6]
                    delegate int D(int x);
                    """,
                    edits: new[]
                    {
                        Edit(SemanticEditKind.Update, c => c.GetMember("E")),
                        Edit(SemanticEditKind.Update, c => c.GetMember("E.A")),
                        Edit(SemanticEditKind.Update, c => c.GetMember("C")),
                        Edit(SemanticEditKind.Update, c => c.GetMember("C._x")),
                        Edit(SemanticEditKind.Update, c => c.GetMember("C.X")),
                        Edit(SemanticEditKind.Update, c => c.GetMember("D"))
                    },
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("E", "C", "D");
                        g.VerifyMethodDefNames();

                        g.VerifyCustomAttributes(
                        [
                            new CustomAttributeRow(Handle(1, TableIndex.Property), Handle(5, TableIndex.MethodDef)), // X
                            new CustomAttributeRow(Handle(2, TableIndex.Field), Handle(2, TableIndex.MethodDef)),    // E.A
                            new CustomAttributeRow(Handle(3, TableIndex.Field), Handle(4, TableIndex.MethodDef)),    // _x
                            new CustomAttributeRow(Handle(14, TableIndex.TypeDef), Handle(1, TableIndex.MethodDef)), // E
                            new CustomAttributeRow(Handle(15, TableIndex.TypeDef), Handle(3, TableIndex.MethodDef)), // C
                            new CustomAttributeRow(Handle(16, TableIndex.TypeDef), Handle(6, TableIndex.MethodDef)), // D
                        ]);

                        g.VerifyEncLogDefinitions(
                        [
                            Row(14, TableIndex.TypeDef, EditAndContinueOperation.Default),
                            Row(15, TableIndex.TypeDef, EditAndContinueOperation.Default),
                            Row(16, TableIndex.TypeDef, EditAndContinueOperation.Default),
                            Row(2, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(3, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(1, TableIndex.Property, EditAndContinueOperation.Default),
                            Row(2, TableIndex.Constant, EditAndContinueOperation.Default),
                            Row(7, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(8, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(9, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(10, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(11, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(12, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(2, TableIndex.MethodSemantics, EditAndContinueOperation.Default)
                        ]);

                        g.VerifyEncMapDefinitions(
                        [
                            Handle(14, TableIndex.TypeDef),
                            Handle(15, TableIndex.TypeDef),
                            Handle(16, TableIndex.TypeDef),
                            Handle(2, TableIndex.Field),
                            Handle(3, TableIndex.Field),
                            Handle(2, TableIndex.Constant),
                            Handle(7, TableIndex.CustomAttribute),
                            Handle(8, TableIndex.CustomAttribute),
                            Handle(9, TableIndex.CustomAttribute),
                            Handle(10, TableIndex.CustomAttribute),
                            Handle(11, TableIndex.CustomAttribute),
                            Handle(12, TableIndex.CustomAttribute),
                            Handle(1, TableIndex.Property),
                            Handle(2, TableIndex.MethodSemantics)
                        ]);
                    })
                .AddGeneration(
                    source: common + """
                    [A7]
                    enum E
                    {
                        [A8]
                        A
                    }

                    [A9]
                    class C
                    {
                        [A10]
                        private int _x;
                        [A11]
                        public int X { get; }
                    }

                    [A12]
                    delegate int D(int x);
                    """,
                    edits: new[]
                    {
                        Edit(SemanticEditKind.Update, c => c.GetMember("E")),
                        Edit(SemanticEditKind.Update, c => c.GetMember("E.A")),
                        Edit(SemanticEditKind.Update, c => c.GetMember("C")),
                        Edit(SemanticEditKind.Update, c => c.GetMember("C._x")),
                        Edit(SemanticEditKind.Update, c => c.GetMember("C.X")),
                        Edit(SemanticEditKind.Update, c => c.GetMember("D"))
                    },
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("E", "C", "D");
                        g.VerifyMethodDefNames();

                        g.VerifyCustomAttributes(
                        [
                            new CustomAttributeRow(Handle(1, TableIndex.Property), Handle(11, TableIndex.MethodDef)),// X
                            new CustomAttributeRow(Handle(2, TableIndex.Field), Handle(8, TableIndex.MethodDef)),    // E.A
                            new CustomAttributeRow(Handle(3, TableIndex.Field), Handle(10, TableIndex.MethodDef)),   // _x
                            new CustomAttributeRow(Handle(14, TableIndex.TypeDef), Handle(7, TableIndex.MethodDef)), // E
                            new CustomAttributeRow(Handle(15, TableIndex.TypeDef), Handle(9, TableIndex.MethodDef)), // C
                            new CustomAttributeRow(Handle(16, TableIndex.TypeDef), Handle(12, TableIndex.MethodDef)),// D
                        ]);

                        g.VerifyEncLogDefinitions(new[]
                        {
                            Row(14, TableIndex.TypeDef, EditAndContinueOperation.Default),
                            Row(15, TableIndex.TypeDef, EditAndContinueOperation.Default),
                            Row(16, TableIndex.TypeDef, EditAndContinueOperation.Default),
                            Row(2, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(3, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(1, TableIndex.Property, EditAndContinueOperation.Default),
                            Row(3, TableIndex.Constant, EditAndContinueOperation.Default),
                            Row(7, TableIndex.CustomAttribute, EditAndContinueOperation.Default),   // Same row numbers as previous gen
                            Row(8, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(9, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(10, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(11, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(12, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(3, TableIndex.MethodSemantics, EditAndContinueOperation.Default)
                        });

                        g.VerifyEncMapDefinitions(new[]
                        {
                            Handle(14, TableIndex.TypeDef),
                            Handle(15, TableIndex.TypeDef),
                            Handle(16, TableIndex.TypeDef),
                            Handle(2, TableIndex.Field),
                            Handle(3, TableIndex.Field),
                            Handle(3, TableIndex.Constant),
                            Handle(7, TableIndex.CustomAttribute),
                            Handle(8, TableIndex.CustomAttribute),
                            Handle(9, TableIndex.CustomAttribute),
                            Handle(10, TableIndex.CustomAttribute),
                            Handle(11, TableIndex.CustomAttribute),
                            Handle(12, TableIndex.CustomAttribute),
                            Handle(1, TableIndex.Property),
                            Handle(3, TableIndex.MethodSemantics)
                        });
                    })

                .Verify();
        }

        /// <summary>
        /// Add a method that requires entries in the ParameterDefs table.
        /// Specifically, normal parameters or return types with attributes.
        /// Add the method in the first edit, then modify the method in the second.
        /// </summary>
        [Fact]
        public void Method_WithParameterAttributes_AddThenUpdate()
        {
            var source0 =
@"class A : System.Attribute { }
class C
{
}";
            var source1 =
@"class A : System.Attribute { }
class C
{
    [return:A]static object F(int arg = 1) => arg;
}";
            var source2 =
@"class A : System.Attribute { }
class C
{
    [return:A]static object F(int arg = 1) => null;
}";
            var compilation0 = CreateCompilation(source0, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);
            var compilation2 = compilation0.WithSource(source2);

            var f1 = compilation1.GetMember<MethodSymbol>("C.F");
            var f2 = compilation2.GetMember<MethodSymbol>("C.F");

            var bytes0 = compilation0.EmitToArray();
            using var md0 = ModuleMetadata.CreateFromImage(bytes0);
            var reader0 = md0.MetadataReader;

            CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "A", "C");
            CheckNames(reader0, reader0.GetMethodDefNames(), ".ctor", ".ctor");

            var generation0 = CreateInitialBaseline(compilation0, md0, EmptyLocalsProvider);

            // gen 1

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Insert, null, f1)));

            using var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;
            var readers = new List<MetadataReader> { reader0, reader1 };
            EncValidation.VerifyModuleMvid(1, reader0, reader1);

            CheckNames(readers, reader1.GetTypeDefNames());
            CheckNames(readers, reader1.GetMethodDefNames(), "F");
            CheckNames(readers, reader1.GetParameterDefNames(), "", "arg");
            CheckNames(readers, diff1.EmitResult.ChangedTypes, "C");
            CheckNames(readers, diff1.EmitResult.UpdatedMethods);

            CheckEncLogDefinitions(reader1,
                Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                Row(3, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(3, TableIndex.MethodDef, EditAndContinueOperation.AddParameter),
                Row(1, TableIndex.Param, EditAndContinueOperation.Default),
                Row(3, TableIndex.MethodDef, EditAndContinueOperation.AddParameter),
                Row(2, TableIndex.Param, EditAndContinueOperation.Default),
                Row(1, TableIndex.Constant, EditAndContinueOperation.Default),
                Row(4, TableIndex.CustomAttribute, EditAndContinueOperation.Default));

            CheckEncMapDefinitions(reader1,
                Handle(3, TableIndex.MethodDef),
                Handle(1, TableIndex.Param),
                Handle(2, TableIndex.Param),
                Handle(1, TableIndex.Constant),
                Handle(4, TableIndex.CustomAttribute));

            // gen 2

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f1, f2)));

            using var md2 = diff2.GetMetadata();
            var reader2 = md2.Reader;
            readers.Add(reader2);

            EncValidation.VerifyModuleMvid(2, reader1, reader2);

            CheckNames(readers, reader2.GetTypeDefNames());
            CheckNames(readers, reader2.GetMethodDefNames(), "F");
            CheckNames(readers, reader2.GetParameterDefNames(), "", "arg");
            CheckNames(readers, diff2.EmitResult.ChangedTypes, "C");
            CheckNames(readers, diff2.EmitResult.UpdatedMethods, "F");

            CheckEncLogDefinitions(reader2,
                Row(3, TableIndex.MethodDef, EditAndContinueOperation.Default), // C.F2
                Row(1, TableIndex.Param, EditAndContinueOperation.Default),
                Row(2, TableIndex.Param, EditAndContinueOperation.Default),
                Row(2, TableIndex.Constant, EditAndContinueOperation.Default),
                Row(4, TableIndex.CustomAttribute, EditAndContinueOperation.Default));

            CheckEncMapDefinitions(reader2,
                Handle(3, TableIndex.MethodDef),
                Handle(1, TableIndex.Param),
                Handle(2, TableIndex.Param),
                Handle(2, TableIndex.Constant),
                Handle(4, TableIndex.CustomAttribute));
        }

        [Fact]
        public void Method_WithEmbeddedAttributes_AndThenUpdate()
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

            var compilation0 = CreateCompilation(source0, references: new[] { RefSafetyRulesAttributeLib }, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), options: ComSafeDebugDll);
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

            var generation0 = CreateInitialBaseline(compilation0, md0, EmptyLocalsProvider);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, main0, main1),
                    SemanticEdit.Create(SemanticEditKind.Insert, null, id1),
                    SemanticEdit.Create(SemanticEditKind.Insert, null, g1)));

            diff1.VerifySynthesizedMembers(
                 "Microsoft.CodeAnalysis.EmbeddedAttribute",
                "System.Runtime.CompilerServices.IsReadOnlyAttribute");

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
                    SemanticEdit.Create(SemanticEditKind.Update, g1, g2),
                    SemanticEdit.Create(SemanticEditKind.Insert, null, h2)));

            // synthesized member for nullable annotations added:
            diff2.VerifySynthesizedMembers(
                "System.Runtime.CompilerServices.IsReadOnlyAttribute",
                "System.Runtime.CompilerServices.NullableAttribute",
                "System.Runtime.CompilerServices.NullableContextAttribute",
                "Microsoft.CodeAnalysis.EmbeddedAttribute");

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
                Row(3, TableIndex.Param, EditAndContinueOperation.Default),
                Row(11, TableIndex.MethodDef, EditAndContinueOperation.AddParameter),
                Row(4, TableIndex.Param, EditAndContinueOperation.Default),
                Row(10, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
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
                Handle(3, TableIndex.Param),
                Handle(4, TableIndex.Param),
                Handle(7, TableIndex.MemberRef),
                Handle(8, TableIndex.MemberRef),
                Handle(9, TableIndex.MemberRef),
                Handle(10, TableIndex.CustomAttribute),
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
               ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Insert, null, f3)));

            // no change in synthesized members:
            diff3.VerifySynthesizedMembers(
                "System.Runtime.CompilerServices.IsReadOnlyAttribute",
                "System.Runtime.CompilerServices.NullableAttribute",
                "System.Runtime.CompilerServices.NullableContextAttribute",
                "Microsoft.CodeAnalysis.EmbeddedAttribute");

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
        public void Field_Add()
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
            var compilation0 = CreateCompilation(source0, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);

            // Verify full metadata contains expected rows.
            var bytes0 = compilation0.EmitToArray();
            using var md0 = ModuleMetadata.CreateFromImage(bytes0);
            var reader0 = md0.MetadataReader;

            CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "C");
            CheckNames(reader0, reader0.GetFieldDefNames(), "F");
            CheckNames(reader0, reader0.GetMethodDefNames(), ".ctor");

            var generation0 = CreateInitialBaseline(compilation0, md0, EmptyLocalsProvider);
            var method0 = compilation0.GetMember<MethodSymbol>("C..ctor");
            var method1 = compilation1.GetMember<MethodSymbol>("C..ctor");

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Insert, null, compilation1.GetMember<FieldSymbol>("C.G")),
                    SemanticEdit.Create(SemanticEditKind.Update, method0, method1)));

            // Verify delta metadata contains expected rows.
            using var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;
            var readers = new[] { reader0, reader1 };

            CheckNames(readers, diff1.EmitResult.ChangedTypes, "C");

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
        public void Property_Accessor_Update()
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
            var compilation0 = CreateCompilation(source0, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);

            var getP0 = compilation0.GetMember<MethodSymbol>("C.get_P");
            var getP1 = compilation1.GetMember<MethodSymbol>("C.get_P");

            var bytes0 = compilation0.EmitToArray();
            using var md0 = ModuleMetadata.CreateFromImage(bytes0);
            var reader0 = md0.MetadataReader;

            CheckNames(reader0, reader0.GetPropertyDefNames(), "P");
            CheckNames(reader0, reader0.GetMethodDefNames(), "get_P", ".ctor");
            var generation0 = CreateInitialBaseline(compilation0, md0, EmptyLocalsProvider);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, getP0, getP1)));

            using var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;
            var readers = new[] { reader0, reader1 };

            CheckNames(readers, reader1.GetPropertyDefNames());
            CheckNames(readers, reader1.GetMethodDefNames(), "get_P");

            CheckEncLog(reader1,
                Row(2, TableIndex.AssemblyRef, EditAndContinueOperation.Default),
                Row(7, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(8, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(2, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default));

            CheckEncMap(reader1,
                Handle(7, TableIndex.TypeRef),
                Handle(8, TableIndex.TypeRef),
                Handle(1, TableIndex.MethodDef),
                Handle(2, TableIndex.StandAloneSig),
                Handle(2, TableIndex.AssemblyRef));
        }

        /// <param name="explicitAccessorEdits">
        /// Validate that explicitly listing property accessors in the edit list produces the same results as listing just the property.
        /// </param>
        [Theory]
        [CombinatorialData]
        public void Property_Add(bool explicitAccessorEdits)
        {
            var source0 = @"
class C
{
}
";
            var source1 = @"
class C
{
    object R { get { return null; } }
}
";
            var source2 = @"
class C
{
    object R { get { return null; } }
    object Q { get; set; }
}
";
            var compilation0 = CreateCompilation(source0, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);
            var compilation2 = compilation0.WithSource(source2);

            var r1 = compilation1.GetMember<PropertySymbol>("C.R");
            var q2 = compilation2.GetMember<PropertySymbol>("C.Q");

            var bytes0 = compilation0.EmitToArray();
            using var md0 = ModuleMetadata.CreateFromImage(bytes0);
            var reader0 = md0.MetadataReader;

            CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "C");

            var generation0 = CreateInitialBaseline(compilation0, md0, EmptyLocalsProvider);

            // gen 1

            var diff1 = compilation1.EmitDifference(
                generation0,
                explicitAccessorEdits
                    ? ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Insert, null, r1), SemanticEdit.Create(SemanticEditKind.Insert, null, r1.GetMethod))
                    : ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Insert, null, r1)));

            using var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;
            var readers = new List<MetadataReader> { reader0, reader1 };

            CheckNames(readers, reader1.GetTypeDefNames());
            CheckNames(readers, reader1.GetFieldDefNames());
            CheckNames(readers, reader1.GetPropertyDefNames(), "R");
            CheckNames(readers, reader1.GetMethodDefNames(), "get_R");
            CheckNames(readers, diff1.EmitResult.UpdatedMethods);
            CheckNames(readers, diff1.EmitResult.ChangedTypes, "C");

            CheckEncLogDefinitions(reader1,
                Row(1, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(1, TableIndex.PropertyMap, EditAndContinueOperation.Default),
                Row(2, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(1, TableIndex.PropertyMap, EditAndContinueOperation.AddProperty),
                Row(1, TableIndex.Property, EditAndContinueOperation.Default),
                Row(1, TableIndex.MethodSemantics, EditAndContinueOperation.Default));

            CheckEncMapDefinitions(reader1,
                Handle(2, TableIndex.MethodDef),
                Handle(1, TableIndex.StandAloneSig),
                Handle(1, TableIndex.PropertyMap),
                Handle(1, TableIndex.Property),
                Handle(1, TableIndex.MethodSemantics));

            // gen 2

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                explicitAccessorEdits
                    ? ImmutableArray.Create(
                        SemanticEdit.Create(SemanticEditKind.Insert, null, q2),
                        SemanticEdit.Create(SemanticEditKind.Insert, null, q2.GetMethod),
                        SemanticEdit.Create(SemanticEditKind.Insert, null, q2.SetMethod))
                    : ImmutableArray.Create(
                        SemanticEdit.Create(SemanticEditKind.Insert, null, q2)));

            using var md2 = diff2.GetMetadata();
            var reader2 = md2.Reader;
            readers.Add(reader2);

            CheckNames(readers, reader2.GetTypeDefNames());
            CheckNames(readers, reader2.GetFieldDefNames(), "<Q>k__BackingField");
            CheckNames(readers, reader2.GetPropertyDefNames(), "Q");
            CheckNames(readers, reader2.GetMethodDefNames(), "get_Q", "set_Q");
            CheckNames(readers, diff1.EmitResult.UpdatedMethods);
            CheckNames(readers, diff1.EmitResult.ChangedTypes, "C");

            CheckEncLogDefinitions(reader2,
                Row(2, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                Row(1, TableIndex.Field, EditAndContinueOperation.Default),
                Row(2, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                Row(3, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(2, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(1, TableIndex.PropertyMap, EditAndContinueOperation.AddProperty),
                Row(2, TableIndex.Property, EditAndContinueOperation.Default),
                Row(4, TableIndex.MethodDef, EditAndContinueOperation.AddParameter),
                Row(1, TableIndex.Param, EditAndContinueOperation.Default),
                Row(4, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(5, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(6, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(7, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(2, TableIndex.MethodSemantics, EditAndContinueOperation.Default),
                Row(3, TableIndex.MethodSemantics, EditAndContinueOperation.Default));

            CheckEncMapDefinitions(reader2,
                Handle(1, TableIndex.Field),
                Handle(3, TableIndex.MethodDef),
                Handle(4, TableIndex.MethodDef),
                Handle(1, TableIndex.Param),
                Handle(4, TableIndex.CustomAttribute),
                Handle(5, TableIndex.CustomAttribute),
                Handle(6, TableIndex.CustomAttribute),
                Handle(7, TableIndex.CustomAttribute),
                Handle(2, TableIndex.Property),
                Handle(2, TableIndex.MethodSemantics),
                Handle(3, TableIndex.MethodSemantics));
        }

        [Fact]
        public void Property_DeleteAndAdd()
        {
            using var _ = new EditAndContinueTest()
                .AddBaseline(
                    source: MetadataUpdateDeletedAttributeSource + """
                        class C
                        {
                            public string P { get; set; }
                        }
                        """,
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("<Module>", "C", "MetadataUpdateDeletedAttribute");
                        g.VerifyMethodDefNames("get_P", "set_P", ".ctor", ".ctor");
                    })

                .AddGeneration(
                    // 1
                    source: MetadataUpdateDeletedAttributeSource + """
                        class C
                        {
                        }
                        """,
                    edits: new[] {
                        Edit(SemanticEditKind.Delete, symbolProvider: c => c.GetMember("C.get_P"), newSymbolProvider: c => c.GetMember("C")),
                        Edit(SemanticEditKind.Delete, symbolProvider: c => c.GetMember("C.set_P"), newSymbolProvider: c => c.GetMember("C")),
                        Edit(SemanticEditKind.Delete, symbolProvider: c => c.GetMember("C.P"), newSymbolProvider: c => c.GetMember("C")),
                    },
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("HotReloadException");

                        g.VerifyMethodDefNames("get_P", "set_P", ".ctor");
                        g.VerifyPropertyDefNames("P");

                        g.VerifyCustomAttributes(
                            "[System.Runtime.CompilerServices.MetadataUpdateDeletedAttribute..ctor] C.get_P",
                            "[System.Runtime.CompilerServices.MetadataUpdateDeletedAttribute..ctor] C.P",
                            "[System.Runtime.CompilerServices.MetadataUpdateDeletedAttribute..ctor] C.set_P",
                            "[System.Runtime.CompilerServices.CompilerGeneratedAttribute..ctor] System.Runtime.CompilerServices.HotReloadException");

                        g.VerifyEncLogDefinitions(new[]
                        {
                            Row(1, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                            Row(4, TableIndex.TypeDef, EditAndContinueOperation.Default),
                            Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(2, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(3, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                            Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(1, TableIndex.Property, EditAndContinueOperation.Default),
                            Row(1, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(7, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(9, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(10, TableIndex.CustomAttribute, EditAndContinueOperation.Default)
                        });

                        g.VerifyEncMapDefinitions(new[]
                        {
                            Handle(4, TableIndex.TypeDef),
                            Handle(2, TableIndex.Field),
                            Handle(3, TableIndex.Field),
                            Handle(1, TableIndex.MethodDef),
                            Handle(2, TableIndex.MethodDef),
                            Handle(5, TableIndex.MethodDef),
                            Handle(1, TableIndex.CustomAttribute),
                            Handle(7, TableIndex.CustomAttribute),
                            Handle(9, TableIndex.CustomAttribute),
                            Handle(10, TableIndex.CustomAttribute),
                            Handle(1, TableIndex.StandAloneSig),
                            Handle(1, TableIndex.Property)
                        });

                        g.VerifyIL("""
                            get_P, set_P
                            {
                              // Code size       13 (0xd)
                              .maxstack  8
                              IL_0000:  ldstr      0x70000005
                              IL_0005:  ldc.i4.s   -2
                              IL_0007:  newobj     0x06000005
                              IL_000c:  throw
                            }
                            .ctor
                            {
                              // Code size       33 (0x21)
                              .maxstack  2
                              IL_0000:  ldarg.0
                              IL_0001:  ldarg.1
                              IL_0002:  call       0x0A00000A
                              IL_0007:  nop
                              IL_0008:  ldarg.0
                              IL_0009:  ldarg.2
                              IL_000a:  stfld      0x04000002
                              IL_000f:  ldsfld     0x04000003
                              IL_0014:  dup
                              IL_0015:  stloc.0
                              IL_0016:  brfalse.s  IL_0020
                              IL_0018:  ldloc.0
                              IL_0019:  ldarg.0
                              IL_001a:  callvirt   0x0A00000B
                              IL_001f:  nop
                              IL_0020:  ret
                            }
                            """);
                    })
                .AddGeneration(
                    // 2
                    source: MetadataUpdateDeletedAttributeSource + """
                        class C
                        {
                            public string P { get; set; }
                        }
                        """,
                    edits: new[] {
                        Edit(SemanticEditKind.Insert, symbolProvider: c => c.GetMember("C.P")),
                    },
                    validator: g =>
                    {
                        g.VerifyTypeDefNames();
                        g.VerifyMethodDefNames("get_P", "set_P");
                        g.VerifyEncLogDefinitions(new[]
                        {
                            Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(1, TableIndex.Property, EditAndContinueOperation.Default),
                            Row(1, TableIndex.Param, EditAndContinueOperation.Default),
                            Row(1, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(7, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(10, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(3, TableIndex.MethodSemantics, EditAndContinueOperation.Default),
                            Row(4, TableIndex.MethodSemantics, EditAndContinueOperation.Default)
                        });

                        g.VerifyEncMapDefinitions(new[]
                        {
                            Handle(1, TableIndex.MethodDef),
                            Handle(2, TableIndex.MethodDef),
                            Handle(1, TableIndex.Param),
                            Handle(1, TableIndex.CustomAttribute),
                            Handle(7, TableIndex.CustomAttribute),
                            Handle(10, TableIndex.CustomAttribute),
                            Handle(1, TableIndex.Property),
                            Handle(3, TableIndex.MethodSemantics),
                            Handle(4, TableIndex.MethodSemantics)
                        });

                        g.VerifyCustomAttributes(
                            "[System.Runtime.CompilerServices.CompilerGeneratedAttribute..ctor] C.get_P",
                            "[System.Runtime.CompilerServices.CompilerGeneratedAttribute..ctor] C.set_P");

                        g.VerifyIL("""
                            get_P
                            {
                              // Code size        7 (0x7)
                              .maxstack  8
                              IL_0000:  ldarg.0
                              IL_0001:  ldfld      0x04000001
                              IL_0006:  ret
                            }
                            set_P
                            {
                              // Code size        8 (0x8)
                              .maxstack  8
                              IL_0000:  ldarg.0
                              IL_0001:  ldarg.1
                              IL_0002:  stfld      0x04000001
                              IL_0007:  ret
                            }
                            """);
                    })
                .Verify();
        }

        [Fact]
        public void Property_DeleteAndAdd_ChangeToAutoProp()
        {
            using var _ = new EditAndContinueTest()
                .AddBaseline(
                    source: $$"""
                        class C
                        {
                            public string P { get { return "1"; } set { } }
                        }
                        """,
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("<Module>", "C");
                        g.VerifyFieldDefNames();
                        g.VerifyMethodDefNames("get_P", "set_P", ".ctor");
                    })

                .AddGeneration(
                    // 1
                    source: """
                        class C
                        {
                        }
                        """,
                    edits:
                    [
                        Edit(SemanticEditKind.Delete, c => c.GetMember("C.get_P"), newSymbolProvider: c => c.GetMember("C")),
                        Edit(SemanticEditKind.Delete, c => c.GetMember("C.set_P"), newSymbolProvider: c => c.GetMember("C")),
                        Edit(SemanticEditKind.Delete, c => c.GetMember("C.P"), newSymbolProvider: c => c.GetMember("C")),
                    ],
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("HotReloadException");
                        g.VerifyFieldDefNames("Code", "Created");
                        g.VerifyMethodDefNames("get_P", "set_P", ".ctor");
                        g.VerifyPropertyDefNames("P");

                        g.VerifyEncLogDefinitions(
                        [
                            Row(2, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                            Row(3, TableIndex.TypeDef, EditAndContinueOperation.Default),
                            Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(1, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(2, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                            Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(1, TableIndex.Property, EditAndContinueOperation.Default),
                            Row(4, TableIndex.CustomAttribute, EditAndContinueOperation.Default)
                        ]);

                        g.VerifyEncMapDefinitions(
                        [
                            Handle(3, TableIndex.TypeDef),
                            Handle(1, TableIndex.Field),
                            Handle(2, TableIndex.Field),
                            Handle(1, TableIndex.MethodDef),
                            Handle(2, TableIndex.MethodDef),
                            Handle(4, TableIndex.MethodDef),
                            Handle(4, TableIndex.CustomAttribute),
                            Handle(2, TableIndex.StandAloneSig),
                            Handle(1, TableIndex.Property)
                        ]);

                        g.VerifyIL("""
                            get_P, set_P
                            {
                              // Code size       13 (0xd)
                              .maxstack  8
                              IL_0000:  ldstr      0x70000009
                              IL_0005:  ldc.i4.s   -2
                              IL_0007:  newobj     0x06000004
                              IL_000c:  throw
                            }
                            .ctor
                            {
                              // Code size       33 (0x21)
                              .maxstack  2
                              IL_0000:  ldarg.0
                              IL_0001:  ldarg.1
                              IL_0002:  call       0x0A000006
                              IL_0007:  nop
                              IL_0008:  ldarg.0
                              IL_0009:  ldarg.2
                              IL_000a:  stfld      0x04000001
                              IL_000f:  ldsfld     0x04000002
                              IL_0014:  dup
                              IL_0015:  stloc.0
                              IL_0016:  brfalse.s  IL_0020
                              IL_0018:  ldloc.0
                              IL_0019:  ldarg.0
                              IL_001a:  callvirt   0x0A000007
                              IL_001f:  nop
                              IL_0020:  ret
                            }
                            """);
                    })
                .AddGeneration(
                    // 2
                    source: """
                        class C
                        {
                            public string P { get; set; }
                        }
                        """,
                    edits: [
                        Edit(SemanticEditKind.Insert, symbolProvider: c => c.GetMember("C.P")),
                    ],
                    validator: g =>
                    {
                        g.VerifyTypeDefNames();
                        g.VerifyFieldDefNames("<P>k__BackingField");
                        g.VerifyMethodDefNames("get_P", "set_P");
                        g.VerifyMemberRefNames(".ctor", ".ctor");
                        g.VerifyEncLogDefinitions(
                        [
                            Row(2, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(3, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(1, TableIndex.Property, EditAndContinueOperation.Default),
                            Row(1, TableIndex.Param, EditAndContinueOperation.Default),
                            Row(5, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(6, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(7, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(8, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(3, TableIndex.MethodSemantics, EditAndContinueOperation.Default),
                            Row(4, TableIndex.MethodSemantics, EditAndContinueOperation.Default)
                        ]);
                        g.VerifyEncMapDefinitions(
                        [
                            Handle(3, TableIndex.Field),
                            Handle(1, TableIndex.MethodDef),
                            Handle(2, TableIndex.MethodDef),
                            Handle(1, TableIndex.Param),
                            Handle(5, TableIndex.CustomAttribute),
                            Handle(6, TableIndex.CustomAttribute),
                            Handle(7, TableIndex.CustomAttribute),
                            Handle(8, TableIndex.CustomAttribute),
                            Handle(1, TableIndex.Property),
                            Handle(3, TableIndex.MethodSemantics),
                            Handle(4, TableIndex.MethodSemantics)
                        ]);

                        g.VerifyIL("""
                            get_P
                            {
                              // Code size        7 (0x7)
                              .maxstack  8
                              IL_0000:  ldarg.0
                              IL_0001:  ldfld      0x04000003
                              IL_0006:  ret
                            }
                            set_P
                            {
                              // Code size        8 (0x8)
                              .maxstack  8
                              IL_0000:  ldarg.0
                              IL_0001:  ldarg.1
                              IL_0002:  stfld      0x04000003
                              IL_0007:  ret
                            }
                            """);
                    })
                .Verify();
        }

        [Fact]
        public void Property_DeleteAndAdd_WithAccessorBodies()
        {
            using var _ = new EditAndContinueTest()
                .AddBaseline(
                    source: $$"""
                        class C
                        {
                            public string P { get { return "1"; } set { } }
                        }
                        """,
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("<Module>", "C");
                        g.VerifyMethodDefNames("get_P", "set_P", ".ctor");
                    })

                .AddGeneration(
                    source: """
                        class C
                        {
                        }
                        """,
                    edits: new[] {
                        Edit(SemanticEditKind.Delete, c => c.GetMember("C.get_P"), newSymbolProvider: c => c.GetMember("C")),
                        Edit(SemanticEditKind.Delete, c => c.GetMember("C.set_P"), newSymbolProvider: c => c.GetMember("C")),
                        Edit(SemanticEditKind.Delete, c => c.GetMember("C.P"), newSymbolProvider: c => c.GetMember("C")),
                    },
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("HotReloadException");
                        g.VerifyMethodDefNames("get_P", "set_P", ".ctor");
                        g.VerifyPropertyDefNames("P");

                        g.VerifyEncLogDefinitions(new[]
                        {
                            Row(2, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                            Row(3, TableIndex.TypeDef, EditAndContinueOperation.Default),
                            Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(1, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(2, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                            Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(1, TableIndex.Property, EditAndContinueOperation.Default),
                            Row(4, TableIndex.CustomAttribute, EditAndContinueOperation.Default)
                        });

                        g.VerifyEncMapDefinitions(new[]
                        {
                            Handle(3, TableIndex.TypeDef),
                            Handle(1, TableIndex.Field),
                            Handle(2, TableIndex.Field),
                            Handle(1, TableIndex.MethodDef),
                            Handle(2, TableIndex.MethodDef),
                            Handle(4, TableIndex.MethodDef),
                            Handle(4, TableIndex.CustomAttribute),
                            Handle(2, TableIndex.StandAloneSig),
                            Handle(1, TableIndex.Property)
                        });

                        g.VerifyIL("""
                            get_P, set_P
                            {
                              // Code size       13 (0xd)
                              .maxstack  8
                              IL_0000:  ldstr      0x70000009
                              IL_0005:  ldc.i4.s   -2
                              IL_0007:  newobj     0x06000004
                              IL_000c:  throw
                            }
                            .ctor
                            {
                              // Code size       33 (0x21)
                              .maxstack  2
                              IL_0000:  ldarg.0
                              IL_0001:  ldarg.1
                              IL_0002:  call       0x0A000006
                              IL_0007:  nop
                              IL_0008:  ldarg.0
                              IL_0009:  ldarg.2
                              IL_000a:  stfld      0x04000001
                              IL_000f:  ldsfld     0x04000002
                              IL_0014:  dup
                              IL_0015:  stloc.0
                              IL_0016:  brfalse.s  IL_0020
                              IL_0018:  ldloc.0
                              IL_0019:  ldarg.0
                              IL_001a:  callvirt   0x0A000007
                              IL_001f:  nop
                              IL_0020:  ret
                            }
                            """);
                    })
                .AddGeneration(
                    source: """
                        class C
                        {
                            public string P { get { return "2"; } set { } }
                        }
                        """,
                    edits: new[]
                    {
                        Edit(SemanticEditKind.Insert, c => c.GetMember("C.P")),
                        Edit(SemanticEditKind.Insert, c => c.GetMember("C.get_P")), // the compiler does not need this edit, but the IDE adds it for simplicity
                        Edit(SemanticEditKind.Insert, c => c.GetMember("C.set_P")), // the compiler does not need this edit, but the IDE adds it for simplicity
                    },
                    validator: g =>
                    {
                        g.VerifyTypeDefNames();
                        g.VerifyMethodDefNames("get_P", "set_P");
                        g.VerifyMemberRefNames();
                        g.VerifyEncLogDefinitions(new[]
                        {
                            Row(3, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                            Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(1, TableIndex.Property, EditAndContinueOperation.Default),
                            Row(1, TableIndex.Param, EditAndContinueOperation.Default),
                            Row(3, TableIndex.MethodSemantics, EditAndContinueOperation.Default),
                            Row(4, TableIndex.MethodSemantics, EditAndContinueOperation.Default),
                        });
                        g.VerifyEncMapDefinitions(new[]
                        {
                            Handle(1, TableIndex.MethodDef),
                            Handle(2, TableIndex.MethodDef),
                            Handle(1, TableIndex.Param),
                            Handle(3, TableIndex.StandAloneSig),
                            Handle(1, TableIndex.Property),
                            Handle(3, TableIndex.MethodSemantics),
                            Handle(4, TableIndex.MethodSemantics),
                        });

                        g.VerifyIL("""
                            get_P
                            {
                              // Code size       11 (0xb)
                              .maxstack  1
                              IL_0000:  nop
                              IL_0001:  ldstr      0x70000155
                              IL_0006:  stloc.0
                              IL_0007:  br.s       IL_0009
                              IL_0009:  ldloc.0
                              IL_000a:  ret
                            }
                            set_P
                            {
                              // Code size        2 (0x2)
                              .maxstack  8
                              IL_0000:  nop
                              IL_0001:  ret
                            }
                            """);
                    })
                .Verify();
        }

        [Fact]
        public void Property_DeleteAndAdd_OneAccessor()
        {
            using var _ = new EditAndContinueTest()
                .AddBaseline(
                    source: $$"""
                        class C
                        {
                            public string P { get { return "1"; } set { } }
                        }
                        """,
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("<Module>", "C");
                        g.VerifyMethodDefNames("get_P", "set_P", ".ctor");
                    })

                .AddGeneration(
                    source: """
                        class C
                        {
                            public string P { get { return "1"; } }
                        }
                        """,
                    edits: new[]
                    {
                        Edit(SemanticEditKind.Delete, c => c.GetMember("C.set_P"), newSymbolProvider: c => c.GetMember("C")),
                    },
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("HotReloadException");
                        g.VerifyMethodDefNames("set_P", ".ctor");
                        g.VerifyMemberRefNames(/* Exception */ ".ctor", /* CompilerGeneratedAttribute */ ".ctor", "Invoke");

                        g.VerifyEncLogDefinitions(new[]
                        {
                            Row(2, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                            Row(3, TableIndex.TypeDef, EditAndContinueOperation.Default),
                            Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(1, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(2, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                            Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(4, TableIndex.CustomAttribute, EditAndContinueOperation.Default)
                        });

                        g.VerifyEncMapDefinitions(new[]
                        {
                            Handle(3, TableIndex.TypeDef),
                            Handle(1, TableIndex.Field),
                            Handle(2, TableIndex.Field),
                            Handle(2, TableIndex.MethodDef),
                            Handle(4, TableIndex.MethodDef),
                            Handle(4, TableIndex.CustomAttribute),
                            Handle(2, TableIndex.StandAloneSig)
                        });

                        g.VerifyIL("""
                            set_P
                            {
                              // Code size       13 (0xd)
                              .maxstack  8
                              IL_0000:  ldstr      0x70000009
                              IL_0005:  ldc.i4.s   -2
                              IL_0007:  newobj     0x06000004
                              IL_000c:  throw
                            }
                            .ctor
                            {
                              // Code size       33 (0x21)
                              .maxstack  2
                              IL_0000:  ldarg.0
                              IL_0001:  ldarg.1
                              IL_0002:  call       0x0A000006
                              IL_0007:  nop
                              IL_0008:  ldarg.0
                              IL_0009:  ldarg.2
                              IL_000a:  stfld      0x04000001
                              IL_000f:  ldsfld     0x04000002
                              IL_0014:  dup
                              IL_0015:  stloc.0
                              IL_0016:  brfalse.s  IL_0020
                              IL_0018:  ldloc.0
                              IL_0019:  ldarg.0
                              IL_001a:  callvirt   0x0A000007
                              IL_001f:  nop
                              IL_0020:  ret
                            }
                            """);
                    })
                .AddGeneration(
                    source: """
                        class C
                        {
                            public string P { get { return "2"; } set { } }
                        }
                        """,
                    edits: new[]
                    {
                        Edit(SemanticEditKind.Insert, c => c.GetMember("C.P")),
                        Edit(SemanticEditKind.Insert, c => c.GetMember("C.get_P")), // the compiler does not need this edit, but the IDE adds it for simplicity
                        Edit(SemanticEditKind.Insert, c => c.GetMember("C.set_P")), // the compiler does not need this edit, but the IDE adds it for simplicity
                    },
                    validator: g =>
                    {
                        g.VerifyTypeDefNames();
                        g.VerifyMethodDefNames("get_P", "set_P");
                        g.VerifyEncLogDefinitions(new[]
                        {
                            Row(3, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                            Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(1, TableIndex.Property, EditAndContinueOperation.Default),
                            Row(1, TableIndex.Param, EditAndContinueOperation.Default),
                            Row(3, TableIndex.MethodSemantics, EditAndContinueOperation.Default),
                            Row(4, TableIndex.MethodSemantics, EditAndContinueOperation.Default),
                        });
                        g.VerifyEncMapDefinitions(new[]
                        {
                            Handle(1, TableIndex.MethodDef),
                            Handle(2, TableIndex.MethodDef),
                            Handle(1, TableIndex.Param),
                            Handle(3, TableIndex.StandAloneSig),
                            Handle(1, TableIndex.Property),
                            Handle(3, TableIndex.MethodSemantics),
                            Handle(4, TableIndex.MethodSemantics),
                        });

                        g.VerifyIL("""
                            get_P
                            {
                              // Code size       11 (0xb)
                              .maxstack  1
                              IL_0000:  nop
                              IL_0001:  ldstr      0x70000155
                              IL_0006:  stloc.0
                              IL_0007:  br.s       IL_0009
                              IL_0009:  ldloc.0
                              IL_000a:  ret
                            }
                            set_P
                            {
                              // Code size        2 (0x2)
                              .maxstack  8
                              IL_0000:  nop
                              IL_0001:  ret
                            }
                            """);
                    })
                .Verify();
        }

        [Fact]
        public void Property_ChangeToAutoProp()
        {
            using var _ = new EditAndContinueTest()
                .AddBaseline(
                    source: $$"""
                        class C
                        {
                            public string P { get { return "1"; } set { } }
                        }
                        """,
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("<Module>", "C");
                        g.VerifyFieldDefNames();
                        g.VerifyMethodDefNames("get_P", "set_P", ".ctor");
                    })

                .AddGeneration(
                    source: """
                        class C
                        {
                            public string P { get; set; }
                        }
                        """,
                    edits: [
                        Edit(SemanticEditKind.Insert, symbolProvider: c => c.GetMember("C.P")),
                    ],
                    validator: g =>
                    {
                        g.VerifyTypeDefNames();
                        g.VerifyFieldDefNames("<P>k__BackingField");
                        g.VerifyMethodDefNames("get_P", "set_P");
                        g.VerifyMemberRefNames(".ctor", ".ctor");
                        g.VerifyEncLogDefinitions(
                        [
                            Row(2, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(1, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(1, TableIndex.Property, EditAndContinueOperation.Default),
                            Row(1, TableIndex.Param, EditAndContinueOperation.Default),
                            Row(4, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(5, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(6, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(7, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(3, TableIndex.MethodSemantics, EditAndContinueOperation.Default),
                            Row(4, TableIndex.MethodSemantics, EditAndContinueOperation.Default)
                        ]);
                        g.VerifyEncMapDefinitions(
                        [
                            Handle(1, TableIndex.Field),
                            Handle(1, TableIndex.MethodDef),
                            Handle(2, TableIndex.MethodDef),
                            Handle(1, TableIndex.Param),
                            Handle(4, TableIndex.CustomAttribute),
                            Handle(5, TableIndex.CustomAttribute),
                            Handle(6, TableIndex.CustomAttribute),
                            Handle(7, TableIndex.CustomAttribute),
                            Handle(1, TableIndex.Property),
                            Handle(3, TableIndex.MethodSemantics),
                            Handle(4, TableIndex.MethodSemantics)
                        ]);

                        var expectedIL = """
                            get_P
                            {
                              // Code size        7 (0x7)
                              .maxstack  8
                              IL_0000:  ldarg.0
                              IL_0001:  ldfld      0x04000001
                              IL_0006:  ret
                            }
                            set_P
                            {
                              // Code size        8 (0x8)
                              .maxstack  8
                              IL_0000:  ldarg.0
                              IL_0001:  ldarg.1
                              IL_0002:  stfld      0x04000001
                              IL_0007:  ret
                            }
                            """;

                        g.VerifyIL(expectedIL);
                    })
                .Verify();
        }

        [Fact]
        public void Property_ChangeToAutoProp_FieldAccess()
        {
            using var _ = new EditAndContinueTest(parseOptions: TestOptions.RegularPreview.WithNoRefSafetyRulesAttribute())
                .AddBaseline(
                    source: $$"""
                        class C
                        {
                            public string P { get { return "1"; } set { } }
                        }
                        """,
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("<Module>", "C");
                        g.VerifyFieldDefNames();
                        g.VerifyMethodDefNames("get_P", "set_P", ".ctor");
                    })

                .AddGeneration(
                    source: """
                        class C
                        {
                            public string P { get; set => field = value; }
                        }
                        """,
                    edits: [
                        Edit(SemanticEditKind.Insert, symbolProvider: c => c.GetMember("C.P")),
                    ],
                    validator: g =>
                    {
                        g.VerifyTypeDefNames();
                        g.VerifyFieldDefNames("<P>k__BackingField");
                        g.VerifyMethodDefNames("get_P", "set_P");
                        g.VerifyMemberRefNames(".ctor", ".ctor");
                        g.VerifyEncLogDefinitions(
                        [
                            Row(2, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(1, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(1, TableIndex.Property, EditAndContinueOperation.Default),
                            Row(1, TableIndex.Param, EditAndContinueOperation.Default),
                            Row(4, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(5, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(6, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(3, TableIndex.MethodSemantics, EditAndContinueOperation.Default),
                            Row(4, TableIndex.MethodSemantics, EditAndContinueOperation.Default)
                        ]);
                        g.VerifyEncMapDefinitions(
                        [
                            Handle(1, TableIndex.Field),
                            Handle(1, TableIndex.MethodDef),
                            Handle(2, TableIndex.MethodDef),
                            Handle(1, TableIndex.Param),
                            Handle(4, TableIndex.CustomAttribute),
                            Handle(5, TableIndex.CustomAttribute),
                            Handle(6, TableIndex.CustomAttribute),
                            Handle(1, TableIndex.Property),
                            Handle(3, TableIndex.MethodSemantics),
                            Handle(4, TableIndex.MethodSemantics)
                        ]);

                        var expectedIL = """
                            get_P
                            {
                              // Code size        7 (0x7)
                              .maxstack  8
                              IL_0000:  ldarg.0
                              IL_0001:  ldfld      0x04000001
                              IL_0006:  ret
                            }
                            set_P
                            {
                              // Code size        8 (0x8)
                              .maxstack  8
                              IL_0000:  ldarg.0
                              IL_0001:  ldarg.1
                              IL_0002:  stfld      0x04000001
                              IL_0007:  ret
                            }
                            """;

                        g.VerifyIL(expectedIL);
                    })
                .Verify();
        }

        [Fact]
        public void Property_AutoProp_AddFieldAccess()
        {
            using var _ = new EditAndContinueTest(parseOptions: TestOptions.RegularPreview.WithNoRefSafetyRulesAttribute())
                .AddBaseline(
                    source: $$"""
                        class C
                        {
                            public string P { get; set; }
                        }
                        """,
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("<Module>", "C");
                        g.VerifyFieldDefNames("<P>k__BackingField");
                        g.VerifyMethodDefNames("get_P", "set_P", ".ctor");
                    })

                .AddGeneration(
                    source: """
                        class C
                        {
                            public string P { get; set => field = value; }
                        }
                        """,
                    edits: [
                        Edit(SemanticEditKind.Insert, symbolProvider: c => c.GetMember("C.P")),
                    ],
                    validator: g =>
                    {
                        g.VerifyTypeDefNames();
                        g.VerifyFieldDefNames();
                        g.VerifyMethodDefNames("get_P", "set_P");
                        g.VerifyMemberRefNames(".ctor", ".ctor");
                        g.VerifyEncLogDefinitions(
                        [
                            Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(1, TableIndex.Property, EditAndContinueOperation.Default),
                            Row(1, TableIndex.Param, EditAndContinueOperation.Default),
                            Row(1, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(7, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(3, TableIndex.MethodSemantics, EditAndContinueOperation.Default),
                            Row(4, TableIndex.MethodSemantics, EditAndContinueOperation.Default)
                        ]);
                        g.VerifyEncMapDefinitions(
                        [
                            Handle(1, TableIndex.MethodDef),
                            Handle(2, TableIndex.MethodDef),
                            Handle(1, TableIndex.Param),
                            Handle(1, TableIndex.CustomAttribute),
                            Handle(7, TableIndex.CustomAttribute),
                            Handle(1, TableIndex.Property),
                            Handle(3, TableIndex.MethodSemantics),
                            Handle(4, TableIndex.MethodSemantics)
                        ]);

                        var expectedIL = """
                            get_P
                            {
                              // Code size        7 (0x7)
                              .maxstack  8
                              IL_0000:  ldarg.0
                              IL_0001:  ldfld      0x04000001
                              IL_0006:  ret
                            }
                            set_P
                            {
                              // Code size        8 (0x8)
                              .maxstack  8
                              IL_0000:  ldarg.0
                              IL_0001:  ldarg.1
                              IL_0002:  stfld      0x04000001
                              IL_0007:  ret
                            }
                            """;

                        g.VerifyIL(expectedIL);
                    })
                .Verify();
        }

        [Fact]
        public void Property_ChangeReturnType()
        {
            using var _ = new EditAndContinueTest()
                .AddBaseline(
                    source: MetadataUpdateDeletedAttributeSource + """
                        class C
                        {
                            string P { get; set; }
                        }
                        """,
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("<Module>", "C", "MetadataUpdateDeletedAttribute");
                        g.VerifyMethodDefNames("get_P", "set_P", ".ctor", ".ctor");
                    })
                .AddGeneration(
                    // 1
                    source: MetadataUpdateDeletedAttributeSource + """
                        class C
                        {
                            int P { get; set; }
                        }
                        """,
                    edits: new[]
                    {
                        Edit(SemanticEditKind.Delete, c => c.GetMember("C.P"), newSymbolProvider: c => c.GetMember("C")),
                        Edit(SemanticEditKind.Delete, c => c.GetMember("C.get_P"), newSymbolProvider: c => c.GetMember("C")),
                        Edit(SemanticEditKind.Delete, c => c.GetMember("C.set_P"), newSymbolProvider: c => c.GetMember("C")),
                        Edit(SemanticEditKind.Insert, c => c.GetMember("C.P")),
                        Edit(SemanticEditKind.Insert, c => c.GetMember("C.get_P")), // the compiler does not need this edit, but the IDE adds it for simplicity
                        Edit(SemanticEditKind.Insert, c => c.GetMember("C.set_P")), // the compiler does not need this edit, but the IDE adds it for simplicity
                    },
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("HotReloadException");
                        g.VerifyMethodDefNames("get_P", "set_P", "get_P", "set_P", ".ctor");
                        g.VerifyDeletedMembers("C: {P, get_P, set_P}");
                        g.VerifyPropertyDefNames("P", "P");

                        g.VerifyEncLogDefinitions(new[]
                        {
                            Row(1, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                            Row(4, TableIndex.TypeDef, EditAndContinueOperation.Default),
                            Row(2, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(2, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(3, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(4, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(2, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                            Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(2, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                            Row(6, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                            Row(7, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(1, TableIndex.Property, EditAndContinueOperation.Default),
                            Row(1, TableIndex.PropertyMap, EditAndContinueOperation.AddProperty),
                            Row(2, TableIndex.Property, EditAndContinueOperation.Default),
                            Row(6, TableIndex.MethodDef, EditAndContinueOperation.AddParameter),
                            Row(2, TableIndex.Param, EditAndContinueOperation.Default),
                            Row(1, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(7, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(9, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(10, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(11, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(12, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(13, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(14, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(3, TableIndex.MethodSemantics, EditAndContinueOperation.Default),
                            Row(4, TableIndex.MethodSemantics, EditAndContinueOperation.Default)
                        });

                        g.VerifyEncMapDefinitions(new[]
                        {
                            Handle(4, TableIndex.TypeDef),
                            Handle(2, TableIndex.Field),
                            Handle(3, TableIndex.Field),
                            Handle(4, TableIndex.Field),
                            Handle(1, TableIndex.MethodDef),
                            Handle(2, TableIndex.MethodDef),
                            Handle(5, TableIndex.MethodDef),
                            Handle(6, TableIndex.MethodDef),
                            Handle(7, TableIndex.MethodDef),
                            Handle(2, TableIndex.Param),
                            Handle(1, TableIndex.CustomAttribute),
                            Handle(7, TableIndex.CustomAttribute),
                            Handle(9, TableIndex.CustomAttribute),
                            Handle(10, TableIndex.CustomAttribute),
                            Handle(11, TableIndex.CustomAttribute),
                            Handle(12, TableIndex.CustomAttribute),
                            Handle(13, TableIndex.CustomAttribute),
                            Handle(14, TableIndex.CustomAttribute),
                            Handle(1, TableIndex.StandAloneSig),
                            Handle(1, TableIndex.Property),
                            Handle(2, TableIndex.Property),
                            Handle(3, TableIndex.MethodSemantics),
                            Handle(4, TableIndex.MethodSemantics)
                        });

                        g.VerifyIL("""
                            get_P, set_P
                            {
                              // Code size       13 (0xd)
                              .maxstack  8
                              IL_0000:  ldstr      0x70000005
                              IL_0005:  ldc.i4.s   -2
                              IL_0007:  newobj     0x06000007
                              IL_000c:  throw
                            }
                            get_P
                            {
                              // Code size        7 (0x7)
                              .maxstack  8
                              IL_0000:  ldarg.0
                              IL_0001:  ldfld      0x04000002
                              IL_0006:  ret
                            }
                            set_P
                            {
                              // Code size        8 (0x8)
                              .maxstack  8
                              IL_0000:  ldarg.0
                              IL_0001:  ldarg.1
                              IL_0002:  stfld      0x04000002
                              IL_0007:  ret
                            }
                            .ctor
                            {
                              // Code size       33 (0x21)
                              .maxstack  2
                              IL_0000:  ldarg.0
                              IL_0001:  ldarg.1
                              IL_0002:  call       0x0A00000B
                              IL_0007:  nop
                              IL_0008:  ldarg.0
                              IL_0009:  ldarg.2
                              IL_000a:  stfld      0x04000003
                              IL_000f:  ldsfld     0x04000004
                              IL_0014:  dup
                              IL_0015:  stloc.0
                              IL_0016:  brfalse.s  IL_0020
                              IL_0018:  ldloc.0
                              IL_0019:  ldarg.0
                              IL_001a:  callvirt   0x0A00000C
                              IL_001f:  nop
                              IL_0020:  ret
                            }
                            """);
                    })
                .AddGeneration(
                    // 2
                    source: MetadataUpdateDeletedAttributeSource + """
                        class C
                        {
                            string P { get; set; }
                        }
                        """,
                    edits: new[] {
                        Edit(SemanticEditKind.Delete, c => c.GetMember("C.P"), newSymbolProvider: c => c.GetMember("C")),
                        Edit(SemanticEditKind.Delete, c => c.GetMember("C.get_P"), newSymbolProvider: c => c.GetMember("C")),
                        Edit(SemanticEditKind.Delete, c => c.GetMember("C.set_P"), newSymbolProvider: c => c.GetMember("C")),
                        Edit(SemanticEditKind.Insert, c => c.GetMember("C.P")),
                        Edit(SemanticEditKind.Insert, c => c.GetMember("C.get_P")), // the compiler does not need this edit, but the IDE adds it for simplicity
                        Edit(SemanticEditKind.Insert, c => c.GetMember("C.set_P")), // the compiler does not need this edit, but the IDE adds it for simplicity
                    },
                    validator: g =>
                    {
                        g.VerifyTypeDefNames();
                        g.VerifyMethodDefNames("get_P", "set_P", "get_P", "set_P");
                        g.VerifyDeletedMembers("C: {P, get_P, set_P}");

                        g.VerifyEncLogDefinitions(new[]
                        {
                            Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(6, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(1, TableIndex.Property, EditAndContinueOperation.Default),
                            Row(2, TableIndex.Property, EditAndContinueOperation.Default),
                            Row(1, TableIndex.Param, EditAndContinueOperation.Default),
                            Row(1, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(7, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(12, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(13, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(14, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(15, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(5, TableIndex.MethodSemantics, EditAndContinueOperation.Default),
                            Row(6, TableIndex.MethodSemantics, EditAndContinueOperation.Default),
                        });
                        g.VerifyEncMapDefinitions(new[]
                        {
                            Handle(1, TableIndex.MethodDef),
                            Handle(2, TableIndex.MethodDef),
                            Handle(5, TableIndex.MethodDef),
                            Handle(6, TableIndex.MethodDef),
                            Handle(1, TableIndex.Param),
                            Handle(1, TableIndex.CustomAttribute),
                            Handle(7, TableIndex.CustomAttribute),
                            Handle(12, TableIndex.CustomAttribute),
                            Handle(13, TableIndex.CustomAttribute),
                            Handle(14, TableIndex.CustomAttribute),
                            Handle(15, TableIndex.CustomAttribute),
                            Handle(1, TableIndex.Property),
                            Handle(2, TableIndex.Property),
                            Handle(5, TableIndex.MethodSemantics),
                            Handle(6, TableIndex.MethodSemantics),
                        });

                        g.VerifyIL("""
                            get_P
                            {
                              // Code size        7 (0x7)
                              .maxstack  8
                              IL_0000:  ldarg.0
                              IL_0001:  ldfld      0x04000001
                              IL_0006:  ret
                            }
                            set_P
                            {
                              // Code size        8 (0x8)
                              .maxstack  8
                              IL_0000:  ldarg.0
                              IL_0001:  ldarg.1
                              IL_0002:  stfld      0x04000001
                              IL_0007:  ret
                            }
                            get_P, set_P
                            {
                              // Code size       13 (0xd)
                              .maxstack  8
                              IL_0000:  ldstr      0x70000151
                              IL_0005:  ldc.i4.s   -2
                              IL_0007:  newobj     0x06000007
                              IL_000c:  throw
                            }
                            """);
                    })
                .Verify();
        }

        [Fact]
        public void Property_Rename()
        {
            using var _ = new EditAndContinueTest()
                .AddBaseline(
                    source: MetadataUpdateDeletedAttributeSource + """
                        class C
                        {
                            string P { get; set; }
                        }
                        """,
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("<Module>", "C", "MetadataUpdateDeletedAttribute");
                        g.VerifyMethodDefNames("get_P", "set_P", ".ctor", ".ctor");
                    })
                .AddGeneration(
                    // 1
                    source: MetadataUpdateDeletedAttributeSource + """
                        class C
                        {
                            string Q { get; set; }
                        }
                        """,
                    edits: new[] {
                        Edit(SemanticEditKind.Delete, c => c.GetMember("C.get_P"), newSymbolProvider: c => c.GetMember("C")),
                        Edit(SemanticEditKind.Delete, c => c.GetMember("C.set_P"), newSymbolProvider: c => c.GetMember("C")),
                        Edit(SemanticEditKind.Delete, c => c.GetMember("C.P"), newSymbolProvider: c => c.GetMember("C")),
                        Edit(SemanticEditKind.Insert, c => c.GetMember("C.Q")),
                        Edit(SemanticEditKind.Insert, c => c.GetMember("C.get_Q")), // the compiler does not need this edit, but the IDE adds it for simplicity
                        Edit(SemanticEditKind.Insert, c => c.GetMember("C.set_Q")), // the compiler does not need this edit, but the IDE adds it for simplicity
                    },
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("HotReloadException");
                        g.VerifyMethodDefNames("get_P", "set_P", "get_Q", "set_Q", ".ctor");
                        g.VerifyPropertyDefNames("P", "Q");
                        g.VerifyDeletedMembers("C: {get_P, set_P, P}");

                        g.VerifyCustomAttributes(
                            "[System.Runtime.CompilerServices.MetadataUpdateDeletedAttribute..ctor] C.get_P",
                            "[System.Runtime.CompilerServices.MetadataUpdateDeletedAttribute..ctor] C.P",
                            "[System.Runtime.CompilerServices.MetadataUpdateDeletedAttribute..ctor] C.set_P",
                            "[System.Runtime.CompilerServices.CompilerGeneratedAttribute..ctor] C.<Q>k__BackingField",
                            "[System.Diagnostics.DebuggerBrowsableAttribute..ctor] C.<Q>k__BackingField",
                            "[System.Runtime.CompilerServices.CompilerGeneratedAttribute..ctor] System.Runtime.CompilerServices.HotReloadException",
                            "[System.Runtime.CompilerServices.CompilerGeneratedAttribute..ctor] C.get_Q",
                            "[System.Runtime.CompilerServices.CompilerGeneratedAttribute..ctor] C.set_Q");

                        g.VerifyEncLogDefinitions(new[]
                        {
                            Row(1, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                            Row(4, TableIndex.TypeDef, EditAndContinueOperation.Default),
                            Row(2, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(2, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(3, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(4, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(2, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                            Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(2, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                            Row(6, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                            Row(7, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(1, TableIndex.Property, EditAndContinueOperation.Default),
                            Row(1, TableIndex.PropertyMap, EditAndContinueOperation.AddProperty),
                            Row(2, TableIndex.Property, EditAndContinueOperation.Default),
                            Row(6, TableIndex.MethodDef, EditAndContinueOperation.AddParameter),
                            Row(2, TableIndex.Param, EditAndContinueOperation.Default),
                            Row(1, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(7, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(9, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(10, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(11, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(12, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(13, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(14, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(3, TableIndex.MethodSemantics, EditAndContinueOperation.Default),
                            Row(4, TableIndex.MethodSemantics, EditAndContinueOperation.Default)
                        });
                        g.VerifyEncMapDefinitions(new[]
                        {
                            Handle(4, TableIndex.TypeDef),
                            Handle(2, TableIndex.Field),
                            Handle(3, TableIndex.Field),
                            Handle(4, TableIndex.Field),
                            Handle(1, TableIndex.MethodDef),
                            Handle(2, TableIndex.MethodDef),
                            Handle(5, TableIndex.MethodDef),
                            Handle(6, TableIndex.MethodDef),
                            Handle(7, TableIndex.MethodDef),
                            Handle(2, TableIndex.Param),
                            Handle(1, TableIndex.CustomAttribute),
                            Handle(7, TableIndex.CustomAttribute),
                            Handle(9, TableIndex.CustomAttribute),
                            Handle(10, TableIndex.CustomAttribute),
                            Handle(11, TableIndex.CustomAttribute),
                            Handle(12, TableIndex.CustomAttribute),
                            Handle(13, TableIndex.CustomAttribute),
                            Handle(14, TableIndex.CustomAttribute),
                            Handle(1, TableIndex.StandAloneSig),
                            Handle(1, TableIndex.Property),
                            Handle(2, TableIndex.Property),
                            Handle(3, TableIndex.MethodSemantics),
                            Handle(4, TableIndex.MethodSemantics)
                        });

                        g.VerifyIL("""
                            get_P, set_P
                            {
                              // Code size       13 (0xd)
                              .maxstack  8
                              IL_0000:  ldstr      0x70000005
                              IL_0005:  ldc.i4.s   -2
                              IL_0007:  newobj     0x06000007
                              IL_000c:  throw
                            }
                            get_Q
                            {
                              // Code size        7 (0x7)
                              .maxstack  8
                              IL_0000:  ldarg.0
                              IL_0001:  ldfld      0x04000002
                              IL_0006:  ret
                            }
                            set_Q
                            {
                              // Code size        8 (0x8)
                              .maxstack  8
                              IL_0000:  ldarg.0
                              IL_0001:  ldarg.1
                              IL_0002:  stfld      0x04000002
                              IL_0007:  ret
                            }
                            .ctor
                            {
                              // Code size       33 (0x21)
                              .maxstack  2
                              IL_0000:  ldarg.0
                              IL_0001:  ldarg.1
                              IL_0002:  call       0x0A00000B
                              IL_0007:  nop
                              IL_0008:  ldarg.0
                              IL_0009:  ldarg.2
                              IL_000a:  stfld      0x04000003
                              IL_000f:  ldsfld     0x04000004
                              IL_0014:  dup
                              IL_0015:  stloc.0
                              IL_0016:  brfalse.s  IL_0020
                              IL_0018:  ldloc.0
                              IL_0019:  ldarg.0
                              IL_001a:  callvirt   0x0A00000C
                              IL_001f:  nop
                              IL_0020:  ret
                            }
                            """);
                    })
                .AddGeneration(
                    // 2
                    source: MetadataUpdateDeletedAttributeSource + """
                        class C
                        {
                            string P { get; set; }
                        }
                        """,
                    edits: new[] {
                        Edit(SemanticEditKind.Delete, c => c.GetMember("C.get_Q"), newSymbolProvider: c=>c.GetMember("C")),
                        Edit(SemanticEditKind.Delete, c => c.GetMember("C.set_Q"), newSymbolProvider: c=>c.GetMember("C")),
                        Edit(SemanticEditKind.Delete, c => c.GetMember("C.Q"), newSymbolProvider: c=>c.GetMember("C")),
                        Edit(SemanticEditKind.Insert, c => c.GetMember("C.P")),
                        Edit(SemanticEditKind.Insert, c => c.GetMember("C.get_P")), // the compiler does not need this edit, but the IDE adds it for simplicity
                        Edit(SemanticEditKind.Insert, c => c.GetMember("C.set_P")), // the compiler does not need this edit, but the IDE adds it for simplicity
                    },
                    validator: g =>
                    {
                        g.VerifyTypeDefNames();
                        g.VerifyMethodDefNames("get_P", "set_P", "get_Q", "set_Q");
                        g.VerifyDeletedMembers("C: {get_Q, set_Q, Q}");

                        g.VerifyCustomAttributes(
                            "[System.Runtime.CompilerServices.CompilerGeneratedAttribute..ctor] C.get_P",
                            "[System.Runtime.CompilerServices.CompilerGeneratedAttribute..ctor] C.set_P",
                            "[System.Runtime.CompilerServices.MetadataUpdateDeletedAttribute..ctor] C.Q",
                            "[System.Runtime.CompilerServices.MetadataUpdateDeletedAttribute..ctor] C.get_Q",
                            "[System.Runtime.CompilerServices.MetadataUpdateDeletedAttribute..ctor] C.set_Q");

                        g.VerifyEncLogDefinitions(new[]
                        {
                            Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(6, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(1, TableIndex.Property, EditAndContinueOperation.Default),
                            Row(2, TableIndex.Property, EditAndContinueOperation.Default),
                            Row(1, TableIndex.Param, EditAndContinueOperation.Default),
                            Row(1, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(7, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(12, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(13, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(14, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(15, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(5, TableIndex.MethodSemantics, EditAndContinueOperation.Default),
                            Row(6, TableIndex.MethodSemantics, EditAndContinueOperation.Default),
                        });
                        g.VerifyEncMapDefinitions(new[]
                        {
                            Handle(1, TableIndex.MethodDef),
                            Handle(2, TableIndex.MethodDef),
                            Handle(5, TableIndex.MethodDef),
                            Handle(6, TableIndex.MethodDef),
                            Handle(1, TableIndex.Param),
                            Handle(1, TableIndex.CustomAttribute),
                            Handle(7, TableIndex.CustomAttribute),
                            Handle(12, TableIndex.CustomAttribute),
                            Handle(13, TableIndex.CustomAttribute),
                            Handle(14, TableIndex.CustomAttribute),
                            Handle(15, TableIndex.CustomAttribute),
                            Handle(1, TableIndex.Property),
                            Handle(2, TableIndex.Property),
                            Handle(5, TableIndex.MethodSemantics),
                            Handle(6, TableIndex.MethodSemantics)
                        });

                        g.VerifyIL("""
                            get_P
                            {
                              // Code size        7 (0x7)
                              .maxstack  8
                              IL_0000:  ldarg.0
                              IL_0001:  ldfld      0x04000001
                              IL_0006:  ret
                            }
                            set_P
                            {
                              // Code size        8 (0x8)
                              .maxstack  8
                              IL_0000:  ldarg.0
                              IL_0001:  ldarg.1
                              IL_0002:  stfld      0x04000001
                              IL_0007:  ret
                            }
                            get_Q, set_Q
                            {
                              // Code size       13 (0xd)
                              .maxstack  8
                              IL_0000:  ldstr      0x70000151
                              IL_0005:  ldc.i4.s   -2
                              IL_0007:  newobj     0x06000007
                              IL_000c:  throw
                            }
                            """);
                    })
                .Verify();
        }

        [Fact]
        public void Indexer_Delete()
        {
            using var _ = new EditAndContinueTest()
                .AddBaseline(
                    source: MetadataUpdateDeletedAttributeSource + """
                        class C
                        {
                            public int this[int x] { get { return 1; } set { } }
                        }
                        """,
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("<Module>", "C", "MetadataUpdateDeletedAttribute");
                        g.VerifyMethodDefNames("get_Item", "set_Item", ".ctor", ".ctor");
                    })

                .AddGeneration(
                    // 1
                    source: MetadataUpdateDeletedAttributeSource + """
                        class C
                        {
                        }
                        """,
                    edits: new[] {
                        Edit(SemanticEditKind.Delete, c => c.GetMember("C.get_Item"), newSymbolProvider: c => c.GetMember("C")),
                        Edit(SemanticEditKind.Delete, c => c.GetMember("C.set_Item"), newSymbolProvider: c => c.GetMember("C")),
                        Edit(SemanticEditKind.Delete, c => c.GetMember("C.this[]"), newSymbolProvider: c => c.GetMember("C")),
                    },
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("HotReloadException");
                        g.VerifyMethodDefNames("get_Item", "set_Item", ".ctor");
                        g.VerifyDeletedMembers("C: {get_Item, set_Item, this[]}");

                        g.VerifyCustomAttributes(
                            "[System.Runtime.CompilerServices.MetadataUpdateDeletedAttribute..ctor] C.get_Item",
                            "[System.Runtime.CompilerServices.MetadataUpdateDeletedAttribute..ctor] C.Item",
                            "[System.Runtime.CompilerServices.MetadataUpdateDeletedAttribute..ctor] C.set_Item",
                            "[System.Runtime.CompilerServices.CompilerGeneratedAttribute..ctor] System.Runtime.CompilerServices.HotReloadException");

                        g.VerifyEncLogDefinitions(new[]
                        {
                            Row(2, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                            Row(4, TableIndex.TypeDef, EditAndContinueOperation.Default),
                            Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(1, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(2, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                            Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(1, TableIndex.Property, EditAndContinueOperation.Default),
                            Row(6, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(7, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(8, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(9, TableIndex.CustomAttribute, EditAndContinueOperation.Default)
                        });

                        g.VerifyEncMapDefinitions(new[]
                        {
                            Handle(4, TableIndex.TypeDef),
                            Handle(1, TableIndex.Field),
                            Handle(2, TableIndex.Field),
                            Handle(1, TableIndex.MethodDef),
                            Handle(2, TableIndex.MethodDef),
                            Handle(5, TableIndex.MethodDef),
                            Handle(6, TableIndex.CustomAttribute),
                            Handle(7, TableIndex.CustomAttribute),
                            Handle(8, TableIndex.CustomAttribute),
                            Handle(9, TableIndex.CustomAttribute),
                            Handle(2, TableIndex.StandAloneSig),
                            Handle(1, TableIndex.Property)
                        });

                        g.VerifyIL("""
                            get_Item, set_Item
                            {
                              // Code size       13 (0xd)
                              .maxstack  8
                              IL_0000:  ldstr      0x70000005
                              IL_0005:  ldc.i4.s   -2
                              IL_0007:  newobj     0x06000005
                              IL_000c:  throw
                            }
                            .ctor
                            {
                              // Code size       33 (0x21)
                              .maxstack  2
                              IL_0000:  ldarg.0
                              IL_0001:  ldarg.1
                              IL_0002:  call       0x0A000009
                              IL_0007:  nop
                              IL_0008:  ldarg.0
                              IL_0009:  ldarg.2
                              IL_000a:  stfld      0x04000001
                              IL_000f:  ldsfld     0x04000002
                              IL_0014:  dup
                              IL_0015:  stloc.0
                              IL_0016:  brfalse.s  IL_0020
                              IL_0018:  ldloc.0
                              IL_0019:  ldarg.0
                              IL_001a:  callvirt   0x0A00000A
                              IL_001f:  nop
                              IL_0020:  ret
                            }
                            """);
                    })
                .Verify();
        }

        [Fact]
        public void Indexer_ChangeParameterType()
        {
            using var _ = new EditAndContinueTest()
                .AddBaseline(
                    source: $$"""
                        class C
                        {
                            int this[int x] { get { return 2; } }
                        }
                        """,
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("<Module>", "C");
                        g.VerifyMethodDefNames("get_Item", ".ctor");
                    })
                .AddGeneration(
                    // 1
                    source: $$"""
                        class C
                        {
                            int this[string x] { get { return 2; } }
                        }
                        """,
                    edits: new[] {
                        Edit(SemanticEditKind.Delete, c => c.GetMember("C.this[]"), newSymbolProvider: c => c.GetMember("C")),
                        Edit(SemanticEditKind.Delete, c => c.GetMember("C.get_Item"), newSymbolProvider: c => c.GetMember("C")),
                        Edit(SemanticEditKind.Insert, c => c.GetMember("C.this[]")),
                        Edit(SemanticEditKind.Insert, c => c.GetMember("C.get_Item")), // the compiler does not need this edit, but the IDE adds it for simplicity
                    },
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("HotReloadException");
                        g.VerifyMethodDefNames("get_Item", "get_Item", ".ctor");
                        g.VerifyDeletedMembers("C: {this[], get_Item}");

                        g.VerifyEncLogDefinitions(new[]
                        {
                            Row(2, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                            Row(3, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                            Row(3, TableIndex.TypeDef, EditAndContinueOperation.Default),
                            Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(1, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(2, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(2, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                            Row(3, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                            Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(1, TableIndex.Property, EditAndContinueOperation.Default),
                            Row(1, TableIndex.PropertyMap, EditAndContinueOperation.AddProperty),
                            Row(2, TableIndex.Property, EditAndContinueOperation.Default),
                            Row(3, TableIndex.MethodDef, EditAndContinueOperation.AddParameter),
                            Row(2, TableIndex.Param, EditAndContinueOperation.Default),
                            Row(5, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(2, TableIndex.MethodSemantics, EditAndContinueOperation.Default)
                        });
                        g.VerifyEncMapDefinitions(new[]
                        {
                            Handle(3, TableIndex.TypeDef),
                            Handle(1, TableIndex.Field),
                            Handle(2, TableIndex.Field),
                            Handle(1, TableIndex.MethodDef),
                            Handle(3, TableIndex.MethodDef),
                            Handle(4, TableIndex.MethodDef),
                            Handle(2, TableIndex.Param),
                            Handle(5, TableIndex.CustomAttribute),
                            Handle(2, TableIndex.StandAloneSig),
                            Handle(3, TableIndex.StandAloneSig),
                            Handle(1, TableIndex.Property),
                            Handle(2, TableIndex.Property),
                            Handle(2, TableIndex.MethodSemantics)
                        });

                        g.VerifyIL("""
                            get_Item
                            {
                              // Code size       13 (0xd)
                              .maxstack  8
                              IL_0000:  ldstr      0x70000005
                              IL_0005:  ldc.i4.s   -2
                              IL_0007:  newobj     0x06000004
                              IL_000c:  throw
                            }
                            get_Item
                            {
                              // Code size        7 (0x7)
                              .maxstack  1
                              IL_0000:  nop
                              IL_0001:  ldc.i4.2
                              IL_0002:  stloc.0
                              IL_0003:  br.s       IL_0005
                              IL_0005:  ldloc.0
                              IL_0006:  ret
                            }
                            .ctor
                             {
                              // Code size       33 (0x21)
                              .maxstack  2
                              IL_0000:  ldarg.0
                              IL_0001:  ldarg.1
                              IL_0002:  call       0x0A000008
                              IL_0007:  nop
                              IL_0008:  ldarg.0
                              IL_0009:  ldarg.2
                              IL_000a:  stfld      0x04000001
                              IL_000f:  ldsfld     0x04000002
                              IL_0014:  dup
                              IL_0015:  stloc.0
                              IL_0016:  brfalse.s  IL_0020
                              IL_0018:  ldloc.0
                              IL_0019:  ldarg.0
                              IL_001a:  callvirt   0x0A000009
                              IL_001f:  nop
                              IL_0020:  ret
                            }
                            """);
                    })
                .AddGeneration(
                    // 2
                    source: $$"""
                        class C
                        {
                            int this[int x] { get { return 2; } }
                        }
                        """,
                    edits: new[] {
                        Edit(SemanticEditKind.Delete, c => c.GetMember("C.this[]"), newSymbolProvider: c=>c.GetMember("C")),
                        Edit(SemanticEditKind.Delete, c => c.GetMember("C.get_Item"), newSymbolProvider: c=>c.GetMember("C")),
                        Edit(SemanticEditKind.Insert, c => c.GetMember("C.this[]")),
                        Edit(SemanticEditKind.Insert, c => c.GetMember("C.get_Item")), // the compiler does not need this edit, but the IDE adds it for simplicity
                    },
                    validator: g =>
                    {
                        g.VerifyTypeDefNames();
                        g.VerifyMethodDefNames("get_Item", "get_Item");
                        g.VerifyDeletedMembers("C: {this[], get_Item}");

                        g.VerifyEncLogDefinitions(new[]
                        {
                            Row(4, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                            Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(3, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(1, TableIndex.Property, EditAndContinueOperation.Default),
                            Row(2, TableIndex.Property, EditAndContinueOperation.Default),
                            Row(1, TableIndex.Param, EditAndContinueOperation.Default),
                            Row(3, TableIndex.MethodSemantics, EditAndContinueOperation.Default)
                        });

                        g.VerifyEncMapDefinitions(new[]
                        {
                            Handle(1, TableIndex.MethodDef),
                            Handle(3, TableIndex.MethodDef),
                            Handle(1, TableIndex.Param),
                            Handle(4, TableIndex.StandAloneSig),
                            Handle(1, TableIndex.Property),
                            Handle(2, TableIndex.Property),
                            Handle(3, TableIndex.MethodSemantics),
                        });

                        g.VerifyIL("""
                            get_Item
                            {
                              // Code size        7 (0x7)
                              .maxstack  1
                              IL_0000:  nop
                              IL_0001:  ldc.i4.2
                              IL_0002:  stloc.0
                              IL_0003:  br.s       IL_0005
                              IL_0005:  ldloc.0
                              IL_0006:  ret
                            }
                            get_Item
                            {
                              // Code size       13 (0xd)
                              .maxstack  8
                              IL_0000:  ldstr      0x70000151
                              IL_0005:  ldc.i4.s   -2
                              IL_0007:  newobj     0x06000004
                              IL_000c:  throw
                            }
                            """);
                    })
                .AddGeneration(
                    // 3
                    source: """
                        class C
                        {
                            int this[int x] { get { return 2; } }
                            int this[string y] { get { return 3; } }
                        }
                        """,
                    edits: new[]
                    {
                        Edit(SemanticEditKind.Insert, c => c.GetMembers<IPropertySymbol>("C.this[]").Single(p => p.Parameters is [{ Name: "y" }])),
                        Edit(SemanticEditKind.Insert, c => c.GetMembers<IPropertySymbol>("C.this[]").Single(p => p.Parameters is [{ Name: "y" }]).GetMethod), // the compiler does not need this edit, but the IDE adds it for simplicity
                    },
                    validator: g =>
                    {
                        g.VerifyTypeDefNames();
                        g.VerifyMethodDefNames("get_Item");
                        g.VerifyDeletedMembers("C: {this[], get_Item}");

                        // existing property and getter are updated:
                        g.VerifyEncLogDefinitions(new[]
                        {
                            Row(5, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                            Row(3, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(2, TableIndex.Property, EditAndContinueOperation.Default),
                            Row(2, TableIndex.Param, EditAndContinueOperation.Default),
                            Row(4, TableIndex.MethodSemantics, EditAndContinueOperation.Default)
                        });

                        g.VerifyEncMapDefinitions(new[]
                        {
                            Handle(3, TableIndex.MethodDef),
                            Handle(2, TableIndex.Param),
                            Handle(5, TableIndex.StandAloneSig),
                            Handle(2, TableIndex.Property),
                            Handle(4, TableIndex.MethodSemantics)
                        });

                        g.VerifyIL("""
                            get_Item
                            {
                              // Code size        7 (0x7)
                              .maxstack  1
                              IL_0000:  nop
                              IL_0001:  ldc.i4.3
                              IL_0002:  stloc.0
                              IL_0003:  br.s       IL_0005
                              IL_0005:  ldloc.0
                              IL_0006:  ret
                            }
                            """);
                    })
                .Verify();
        }

        [Fact]
        public void Indexer_ChangeParameterName()
        {
            using var _ = new EditAndContinueTest()
                .AddBaseline(
                    source: $$"""
                        class C
                        {
                            int this[int x] { get => 1; set { } }
                        }
                        """,
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("<Module>", "C");
                        g.VerifyMethodDefNames("get_Item", "set_Item", ".ctor");
                    })
                .AddGeneration(
                    source: $$"""
                        class C
                        {
                            int this[int y] { get => 1; set { } }
                        }
                        """,
                    edits: new[]
                    {
                        // Both accessors must be updated, the indexer itself does not:
                        Edit(SemanticEditKind.Update, c => c.GetMember("C.get_Item")),
                        Edit(SemanticEditKind.Update, c => c.GetMember("C.set_Item")),
                    },
                    validator: g =>
                    {
                        g.VerifyTypeDefNames();
                        g.VerifyMethodDefNames("get_Item", "set_Item");

                        g.VerifyEncLogDefinitions(new[]
                        {
                            Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(1, TableIndex.Param, EditAndContinueOperation.Default),
                            Row(2, TableIndex.Param, EditAndContinueOperation.Default),
                            Row(3, TableIndex.Param, EditAndContinueOperation.Default),
                        });

                        g.VerifyEncMapDefinitions(new[]
                        {
                            Handle(1, TableIndex.MethodDef),
                            Handle(2, TableIndex.MethodDef),
                            Handle(1, TableIndex.Param),
                            Handle(2, TableIndex.Param),
                            Handle(3, TableIndex.Param),
                        });

                        g.VerifyIL("""
                            get_Item
                            {
                              // Code size        2 (0x2)
                              .maxstack  8
                              IL_0000:  ldc.i4.1
                              IL_0001:  ret
                            }
                            set_Item
                            {
                              // Code size        2 (0x2)
                              .maxstack  8
                              IL_0000:  nop
                              IL_0001:  ret
                            }
                            """);
                    })
                .Verify();
        }

        [Fact]
        public void Event_Add()
        {
            var source0 = @"
class C
{
}";
            var source1 = @"
class C
{
    event System.Action E;
}";
            var source2 = @"
class C
{
    event System.Action E;
    event System.Action G;
}";
            var compilation0 = CreateCompilation(source0, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);
            var compilation2 = compilation0.WithSource(source2);

            var e1 = compilation1.GetMember<EventSymbol>("C.E");
            var g2 = compilation2.GetMember<EventSymbol>("C.G");

            var bytes0 = compilation0.EmitToArray();
            using var md0 = ModuleMetadata.CreateFromImage(bytes0);
            var reader0 = md0.MetadataReader;
            var generation0 = CreateInitialBaseline(compilation0, md0, EmptyLocalsProvider);

            // gen 1

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Insert, null, e1)));

            using var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;
            var readers = new List<MetadataReader> { reader0, reader1 };

            CheckNames(readers, reader1.GetTypeDefNames());
            CheckNames(readers, reader1.GetFieldDefNames(), "E");
            CheckNames(readers, reader1.GetMethodDefNames(), "add_E", "remove_E");
            CheckNames(readers, diff1.EmitResult.UpdatedMethods);
            CheckNames(readers, diff1.EmitResult.ChangedTypes, "C");

            CheckEncLogDefinitions(reader1,
                Row(1, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(1, TableIndex.EventMap, EditAndContinueOperation.Default),
                Row(1, TableIndex.EventMap, EditAndContinueOperation.AddEvent),
                Row(1, TableIndex.Event, EditAndContinueOperation.Default),
                Row(2, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                Row(1, TableIndex.Field, EditAndContinueOperation.Default),
                Row(2, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(2, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                Row(3, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(2, TableIndex.MethodDef, EditAndContinueOperation.AddParameter),
                Row(1, TableIndex.Param, EditAndContinueOperation.Default),
                Row(3, TableIndex.MethodDef, EditAndContinueOperation.AddParameter),
                Row(2, TableIndex.Param, EditAndContinueOperation.Default),
                Row(4, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(5, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(6, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(7, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(1, TableIndex.MethodSemantics, EditAndContinueOperation.Default),
                Row(2, TableIndex.MethodSemantics, EditAndContinueOperation.Default));

            CheckEncMapDefinitions(reader1,
                Handle(1, TableIndex.Field),
                Handle(2, TableIndex.MethodDef),
                Handle(3, TableIndex.MethodDef),
                Handle(1, TableIndex.Param),
                Handle(2, TableIndex.Param),
                Handle(4, TableIndex.CustomAttribute),
                Handle(5, TableIndex.CustomAttribute),
                Handle(6, TableIndex.CustomAttribute),
                Handle(7, TableIndex.CustomAttribute),
                Handle(1, TableIndex.StandAloneSig),
                Handle(1, TableIndex.EventMap),
                Handle(1, TableIndex.Event),
                Handle(1, TableIndex.MethodSemantics),
                Handle(2, TableIndex.MethodSemantics));

            // gen 2

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Insert, null, g2)));

            using var md2 = diff2.GetMetadata();
            var reader2 = md2.Reader;

            readers.Add(reader2);
            CheckNames(readers, reader2.GetTypeDefNames());
            CheckNames(readers, reader2.GetFieldDefNames(), "G");
            CheckNames(readers, reader2.GetMethodDefNames(), "add_G", "remove_G");
            CheckNames(readers, diff1.EmitResult.UpdatedMethods);
            CheckNames(readers, diff1.EmitResult.ChangedTypes, "C");

            CheckEncLogDefinitions(reader2,
                Row(2, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(1, TableIndex.EventMap, EditAndContinueOperation.AddEvent),
                Row(2, TableIndex.Event, EditAndContinueOperation.Default),
                Row(2, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                Row(2, TableIndex.Field, EditAndContinueOperation.Default),
                Row(2, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(2, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(4, TableIndex.MethodDef, EditAndContinueOperation.AddParameter),
                Row(3, TableIndex.Param, EditAndContinueOperation.Default),
                Row(5, TableIndex.MethodDef, EditAndContinueOperation.AddParameter),
                Row(4, TableIndex.Param, EditAndContinueOperation.Default),
                Row(8, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(9, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(10, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(11, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(3, TableIndex.MethodSemantics, EditAndContinueOperation.Default),
                Row(4, TableIndex.MethodSemantics, EditAndContinueOperation.Default));

            CheckEncMapDefinitions(reader2,
                Handle(2, TableIndex.Field),
                Handle(4, TableIndex.MethodDef),
                Handle(5, TableIndex.MethodDef),
                Handle(3, TableIndex.Param),
                Handle(4, TableIndex.Param),
                Handle(8, TableIndex.CustomAttribute),
                Handle(9, TableIndex.CustomAttribute),
                Handle(10, TableIndex.CustomAttribute),
                Handle(11, TableIndex.CustomAttribute),
                Handle(2, TableIndex.StandAloneSig),
                Handle(2, TableIndex.Event),
                Handle(3, TableIndex.MethodSemantics),
                Handle(4, TableIndex.MethodSemantics));
        }

        [Fact]
        public void Event_Delete()
        {
            using var _ = new EditAndContinueTest()
                .AddBaseline(
                    source: MetadataUpdateDeletedAttributeSource + """
                        class C
                        {
                            public event System.EventHandler E;
                        }
                        """,
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("<Module>", "C", "MetadataUpdateDeletedAttribute");
                        g.VerifyMethodDefNames("add_E", "remove_E", ".ctor", ".ctor");
                    })

                .AddGeneration(
                    source: MetadataUpdateDeletedAttributeSource + """
                        class C
                        {
                        }
                        """,
                    edits: new[] {
                        Edit(SemanticEditKind.Delete, c => c.GetMember("C.E"), newSymbolProvider: c => c.GetMember("C")),
                        Edit(SemanticEditKind.Delete, c => c.GetMember("C.add_E"), newSymbolProvider: c => c.GetMember("C")),
                        Edit(SemanticEditKind.Delete, c => c.GetMember("C.remove_E"), newSymbolProvider: c => c.GetMember("C")),
                    },
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("HotReloadException");
                        g.VerifyMethodDefNames("add_E", "remove_E", ".ctor");
                        g.VerifyTypeRefNames("Object", "EventHandler", "CompilerGeneratedAttribute", "Exception", "Action`1");
                        g.VerifyEventDefNames("E");

                        g.VerifyCustomAttributes(
                            "[System.Runtime.CompilerServices.MetadataUpdateDeletedAttribute..ctor] C.add_E",
                            "[System.Runtime.CompilerServices.MetadataUpdateDeletedAttribute..ctor] C.E",
                            "[System.Runtime.CompilerServices.MetadataUpdateDeletedAttribute..ctor] C.remove_E",
                            "[System.Runtime.CompilerServices.CompilerGeneratedAttribute..ctor] System.Runtime.CompilerServices.HotReloadException");

                        g.VerifyEncLogDefinitions(new[]
                        {
                            Row(2, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                            Row(4, TableIndex.TypeDef, EditAndContinueOperation.Default),
                            Row(1, TableIndex.Event, EditAndContinueOperation.Default),
                            Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(2, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(3, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                            Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(1, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(7, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(9, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(10, TableIndex.CustomAttribute, EditAndContinueOperation.Default)
                        });

                        g.VerifyEncMapDefinitions(new[]
                        {
                            Handle(4, TableIndex.TypeDef),
                            Handle(2, TableIndex.Field),
                            Handle(3, TableIndex.Field),
                            Handle(1, TableIndex.MethodDef),
                            Handle(2, TableIndex.MethodDef),
                            Handle(5, TableIndex.MethodDef),
                            Handle(1, TableIndex.CustomAttribute),
                            Handle(7, TableIndex.CustomAttribute),
                            Handle(9, TableIndex.CustomAttribute),
                            Handle(10, TableIndex.CustomAttribute),
                            Handle(2, TableIndex.StandAloneSig),
                            Handle(1, TableIndex.Event)
                        });

                        g.VerifyIL("""
                            add_E, remove_E
                            {
                              // Code size       13 (0xd)
                              .maxstack  8
                              IL_0000:  ldstr      0x70000005
                              IL_0005:  ldc.i4.s   -2
                              IL_0007:  newobj     0x06000005
                              IL_000c:  throw
                            }
                            .ctor
                            {
                              // Code size       33 (0x21)
                              .maxstack  2
                              IL_0000:  ldarg.0
                              IL_0001:  ldarg.1
                              IL_0002:  call       0x0A00000D
                              IL_0007:  nop
                              IL_0008:  ldarg.0
                              IL_0009:  ldarg.2
                              IL_000a:  stfld      0x04000002
                              IL_000f:  ldsfld     0x04000003
                              IL_0014:  dup
                              IL_0015:  stloc.0
                              IL_0016:  brfalse.s  IL_0020
                              IL_0018:  ldloc.0
                              IL_0019:  ldarg.0
                              IL_001a:  callvirt   0x0A00000E
                              IL_001f:  nop
                              IL_0020:  ret
                            }
                            """);
                    })
                .Verify();
        }

        [Fact]
        public void Event_Delete_NoDeletedAttribute()
        {
            using var _ = new EditAndContinueTest()
                .AddBaseline(
                    source: $$"""
                        class C
                        {
                            public event System.EventHandler E;
                        }
                        """,
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("<Module>", "C");
                        g.VerifyMethodDefNames("add_E", "remove_E", ".ctor");

                        g.VerifyCustomAttributes(
                            "[System.Runtime.CompilerServices.CompilerGeneratedAttribute..ctor] C.add_E",
                            "[System.Runtime.CompilerServices.CompilerGeneratedAttribute..ctor] C.E",
                            "[System.Diagnostics.DebuggerBrowsableAttribute..ctor] C.E",
                            "[System.Runtime.CompilerServices.CompilationRelaxationsAttribute..ctor] <assembly>",
                            "[System.Runtime.CompilerServices.RuntimeCompatibilityAttribute..ctor] <assembly>",
                            "[System.Diagnostics.DebuggableAttribute..ctor] <assembly>",
                            "[System.Runtime.CompilerServices.CompilerGeneratedAttribute..ctor] C.remove_E");
                    })

                .AddGeneration(
                    source: """
                        class C
                        {
                        }
                        """,
                    edits: new[] {
                        Edit(SemanticEditKind.Delete, c => c.GetMember("C.E"), newSymbolProvider: c => c.GetMember("C")),
                        Edit(SemanticEditKind.Delete, c => c.GetMember("C.add_E"), newSymbolProvider: c => c.GetMember("C")),
                        Edit(SemanticEditKind.Delete, c => c.GetMember("C.remove_E"), newSymbolProvider: c => c.GetMember("C")),
                    },
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("HotReloadException");
                        g.VerifyMethodDefNames("add_E", "remove_E", ".ctor");
                        g.VerifyTypeRefNames("Object", "EventHandler", "CompilerGeneratedAttribute", "Exception", "Action`1");

                        g.VerifyCustomAttributes(
                            [
                                "<nil>",
                                "<nil>",
                                "[System.Runtime.CompilerServices.CompilerGeneratedAttribute..ctor] System.Runtime.CompilerServices.HotReloadException"
                            ],
                            includeNil: true);

                        g.VerifyEncLogDefinitions(new[]
                        {
                            Row(2, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                            Row(3, TableIndex.TypeDef, EditAndContinueOperation.Default),
                            Row(1, TableIndex.Event, EditAndContinueOperation.Default),
                            Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(2, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(3, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                            Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(1, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(7, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(8, TableIndex.CustomAttribute, EditAndContinueOperation.Default)
                        });

                        g.VerifyEncMapDefinitions(new[]
                        {
                            Handle(3, TableIndex.TypeDef),
                            Handle(2, TableIndex.Field),
                            Handle(3, TableIndex.Field),
                            Handle(1, TableIndex.MethodDef),
                            Handle(2, TableIndex.MethodDef),
                            Handle(4, TableIndex.MethodDef),
                            Handle(1, TableIndex.CustomAttribute),
                            Handle(7, TableIndex.CustomAttribute),
                            Handle(8, TableIndex.CustomAttribute),
                            Handle(2, TableIndex.StandAloneSig),
                            Handle(1, TableIndex.Event)
                        });

                        g.VerifyIL("""
                            add_E, remove_E
                            {
                              // Code size       13 (0xd)
                              .maxstack  8
                              IL_0000:  ldstr      0x70000005
                              IL_0005:  ldc.i4.s   -2
                              IL_0007:  newobj     0x06000004
                              IL_000c:  throw
                            }
                            .ctor
                            {
                              // Code size       33 (0x21)
                              .maxstack  2
                              IL_0000:  ldarg.0
                              IL_0001:  ldarg.1
                              IL_0002:  call       0x0A00000B
                              IL_0007:  nop
                              IL_0008:  ldarg.0
                              IL_0009:  ldarg.2
                              IL_000a:  stfld      0x04000002
                              IL_000f:  ldsfld     0x04000003
                              IL_0014:  dup
                              IL_0015:  stloc.0
                              IL_0016:  brfalse.s  IL_0020
                              IL_0018:  ldloc.0
                              IL_0019:  ldarg.0
                              IL_001a:  callvirt   0x0A00000C
                              IL_001f:  nop
                              IL_0020:  ret
                            }
                            """);
                    })
                .Verify();
        }

        [Fact]
        public void Event_Rename()
        {
            using var _ = new EditAndContinueTest()
                .AddBaseline(
                    source: MetadataUpdateDeletedAttributeSource + """
                        class C
                        {
                            public event System.EventHandler E;
                        }
                        """,
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("<Module>", "C", "MetadataUpdateDeletedAttribute");
                        g.VerifyMethodDefNames("add_E", "remove_E", ".ctor", ".ctor");
                    })

                .AddGeneration(
                    // 1
                    source: MetadataUpdateDeletedAttributeSource + """
                        class C
                        {
                            public event System.EventHandler F;
                        }
                        """,
                    edits: new[] {
                        Edit(SemanticEditKind.Delete, c => c.GetMember("C.E"), newSymbolProvider: c => c.GetMember("C")),
                        Edit(SemanticEditKind.Delete, c => c.GetMember("C.add_E"), newSymbolProvider: c => c.GetMember("C")),
                        Edit(SemanticEditKind.Delete, c => c.GetMember("C.remove_E"), newSymbolProvider: c => c.GetMember("C")),
                        Edit(SemanticEditKind.Insert, c => c.GetMember("C.F")),
                        Edit(SemanticEditKind.Insert, c => c.GetMember("C.add_F")),    // the compiler does not need this edit, but the IDE adds it for simplicity
                        Edit(SemanticEditKind.Insert, c => c.GetMember("C.remove_F")), // the compiler does not need this edit, but the IDE adds it for simplicity
                    },
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("HotReloadException");
                        g.VerifyMethodDefNames("add_E", "remove_E", "add_F", "remove_F", ".ctor");
                        g.VerifyEventDefNames("E", "F");

                        g.VerifyCustomAttributes(
                            "[System.Runtime.CompilerServices.MetadataUpdateDeletedAttribute..ctor] C.add_E",
                            "[System.Runtime.CompilerServices.MetadataUpdateDeletedAttribute..ctor] C.E",
                            "[System.Runtime.CompilerServices.MetadataUpdateDeletedAttribute..ctor] C.remove_E",
                            "[System.Runtime.CompilerServices.CompilerGeneratedAttribute..ctor] C.F",
                            "[System.Diagnostics.DebuggerBrowsableAttribute..ctor] C.F",
                            "[System.Runtime.CompilerServices.CompilerGeneratedAttribute..ctor] System.Runtime.CompilerServices.HotReloadException",
                            "[System.Runtime.CompilerServices.CompilerGeneratedAttribute..ctor] C.add_F",
                            "[System.Runtime.CompilerServices.CompilerGeneratedAttribute..ctor] C.remove_F");

                        g.VerifyEncLogDefinitions(new[]
                        {
                            Row(2, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                            Row(3, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                            Row(4, TableIndex.TypeDef, EditAndContinueOperation.Default),
                            Row(1, TableIndex.Event, EditAndContinueOperation.Default),
                            Row(1, TableIndex.EventMap, EditAndContinueOperation.AddEvent),
                            Row(2, TableIndex.Event, EditAndContinueOperation.Default),
                            Row(2, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(2, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(3, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(4, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(2, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                            Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(2, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                            Row(6, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                            Row(7, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(5, TableIndex.MethodDef, EditAndContinueOperation.AddParameter),
                            Row(3, TableIndex.Param, EditAndContinueOperation.Default),
                            Row(6, TableIndex.MethodDef, EditAndContinueOperation.AddParameter),
                            Row(4, TableIndex.Param, EditAndContinueOperation.Default),
                            Row(1, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(7, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(9, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(10, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(11, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(12, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(13, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(14, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(3, TableIndex.MethodSemantics, EditAndContinueOperation.Default),
                            Row(4, TableIndex.MethodSemantics, EditAndContinueOperation.Default)
                        });

                        g.VerifyEncMapDefinitions(new[]
                        {
                            Handle(4, TableIndex.TypeDef),
                            Handle(2, TableIndex.Field),
                            Handle(3, TableIndex.Field),
                            Handle(4, TableIndex.Field),
                            Handle(1, TableIndex.MethodDef),
                            Handle(2, TableIndex.MethodDef),
                            Handle(5, TableIndex.MethodDef),
                            Handle(6, TableIndex.MethodDef),
                            Handle(7, TableIndex.MethodDef),
                            Handle(3, TableIndex.Param),
                            Handle(4, TableIndex.Param),
                            Handle(1, TableIndex.CustomAttribute),
                            Handle(7, TableIndex.CustomAttribute),
                            Handle(9, TableIndex.CustomAttribute),
                            Handle(10, TableIndex.CustomAttribute),
                            Handle(11, TableIndex.CustomAttribute),
                            Handle(12, TableIndex.CustomAttribute),
                            Handle(13, TableIndex.CustomAttribute),
                            Handle(14, TableIndex.CustomAttribute),
                            Handle(2, TableIndex.StandAloneSig),
                            Handle(3, TableIndex.StandAloneSig),
                            Handle(1, TableIndex.Event),
                            Handle(2, TableIndex.Event),
                            Handle(3, TableIndex.MethodSemantics),
                            Handle(4, TableIndex.MethodSemantics)
                        });

                        g.VerifyIL("""
                            add_E, remove_E
                            {
                              // Code size       13 (0xd)
                              .maxstack  8
                              IL_0000:  ldstr      0x70000005
                              IL_0005:  ldc.i4.s   -2
                              IL_0007:  newobj     0x06000007
                              IL_000c:  throw
                            }
                            add_F
                            {
                              // Code size       41 (0x29)
                              .maxstack  3
                              IL_0000:  ldarg.0
                              IL_0001:  ldfld      0x04000002
                              IL_0006:  stloc.0
                              IL_0007:  ldloc.0
                              IL_0008:  stloc.1
                              IL_0009:  ldloc.1
                              IL_000a:  ldarg.1
                              IL_000b:  call       0x0A00000E
                              IL_0010:  castclass  0x01000010
                              IL_0015:  stloc.2
                              IL_0016:  ldarg.0
                              IL_0017:  ldflda     0x04000002
                              IL_001c:  ldloc.2
                              IL_001d:  ldloc.1
                              IL_001e:  call       0x2B000002
                              IL_0023:  stloc.0
                              IL_0024:  ldloc.0
                              IL_0025:  ldloc.1
                              IL_0026:  bne.un.s   IL_0007
                              IL_0028:  ret
                            }
                            remove_F
                            {
                              // Code size       41 (0x29)
                              .maxstack  3
                              IL_0000:  ldarg.0
                              IL_0001:  ldfld      0x04000002
                              IL_0006:  stloc.0
                              IL_0007:  ldloc.0
                              IL_0008:  stloc.1
                              IL_0009:  ldloc.1
                              IL_000a:  ldarg.1
                              IL_000b:  call       0x0A000010
                              IL_0010:  castclass  0x01000010
                              IL_0015:  stloc.2
                              IL_0016:  ldarg.0
                              IL_0017:  ldflda     0x04000002
                              IL_001c:  ldloc.2
                              IL_001d:  ldloc.1
                              IL_001e:  call       0x2B000002
                              IL_0023:  stloc.0
                              IL_0024:  ldloc.0
                              IL_0025:  ldloc.1
                              IL_0026:  bne.un.s   IL_0007
                              IL_0028:  ret
                            }
                            .ctor
                            {
                              // Code size       33 (0x21)
                              .maxstack  2
                              IL_0000:  ldarg.0
                              IL_0001:  ldarg.1
                              IL_0002:  call       0x0A000011
                              IL_0007:  nop
                              IL_0008:  ldarg.0
                              IL_0009:  ldarg.2
                              IL_000a:  stfld      0x04000003
                              IL_000f:  ldsfld     0x04000004
                              IL_0014:  dup
                              IL_0015:  stloc.0
                              IL_0016:  brfalse.s  IL_0020
                              IL_0018:  ldloc.0
                              IL_0019:  ldarg.0
                              IL_001a:  callvirt   0x0A000012
                              IL_001f:  nop
                              IL_0020:  ret
                            }
                            """);
                    })

                .AddGeneration(
                    // 2
                    source: MetadataUpdateDeletedAttributeSource + """
                        class C
                        {
                            public event System.EventHandler E;
                        }
                        """,
                    edits: new[] {
                        Edit(SemanticEditKind.Delete, c => c.GetMember("C.F"), newSymbolProvider: c => c.GetMember("C")),
                        Edit(SemanticEditKind.Delete, c => c.GetMember("C.add_F"), newSymbolProvider: c => c.GetMember("C")),
                        Edit(SemanticEditKind.Delete, c => c.GetMember("C.remove_F"), newSymbolProvider: c => c.GetMember("C")),
                        Edit(SemanticEditKind.Insert, c => c.GetMember("C.E")),
                        Edit(SemanticEditKind.Insert, c => c.GetMember("C.add_E")),    // the compiler does not need this edit, but the IDE adds it for simplicity
                        Edit(SemanticEditKind.Insert, c => c.GetMember("C.remove_E")), // the compiler does not need this edit, but the IDE adds it for simplicity
                    },
                    validator: g =>
                    {
                        g.VerifyTypeDefNames();
                        g.VerifyMethodDefNames("add_E", "remove_E", "add_F", "remove_F");
                        g.VerifyEventDefNames("E", "F");

                        g.VerifyCustomAttributes(
                            "[System.Runtime.CompilerServices.CompilerGeneratedAttribute..ctor] C.add_E",
                            "[System.Runtime.CompilerServices.CompilerGeneratedAttribute..ctor] C.remove_E",
                            "[System.Runtime.CompilerServices.MetadataUpdateDeletedAttribute..ctor] C.F",
                            "[System.Runtime.CompilerServices.MetadataUpdateDeletedAttribute..ctor] C.add_F",
                            "[System.Runtime.CompilerServices.MetadataUpdateDeletedAttribute..ctor] C.remove_F");

                        g.VerifyEncLogDefinitions(new[]
                        {
                            Row(4, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                            Row(1, TableIndex.Event, EditAndContinueOperation.Default),
                            Row(2, TableIndex.Event, EditAndContinueOperation.Default),
                            Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(6, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(1, TableIndex.Param, EditAndContinueOperation.Default),
                            Row(2, TableIndex.Param, EditAndContinueOperation.Default),
                            Row(1, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(7, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(12, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(13, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(14, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(15, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(5, TableIndex.MethodSemantics, EditAndContinueOperation.Default),
                            Row(6, TableIndex.MethodSemantics, EditAndContinueOperation.Default)
                        });

                        g.VerifyEncMapDefinitions(new[]
                        {
                            Handle(1, TableIndex.MethodDef),
                            Handle(2, TableIndex.MethodDef),
                            Handle(5, TableIndex.MethodDef),
                            Handle(6, TableIndex.MethodDef),
                            Handle(1, TableIndex.Param),
                            Handle(2, TableIndex.Param),
                            Handle(1, TableIndex.CustomAttribute),
                            Handle(7, TableIndex.CustomAttribute),
                            Handle(12, TableIndex.CustomAttribute),
                            Handle(13, TableIndex.CustomAttribute),
                            Handle(14, TableIndex.CustomAttribute),
                            Handle(15, TableIndex.CustomAttribute),
                            Handle(4, TableIndex.StandAloneSig),
                            Handle(1, TableIndex.Event),
                            Handle(2, TableIndex.Event),
                            Handle(5, TableIndex.MethodSemantics),
                            Handle(6, TableIndex.MethodSemantics)
                        });

                        g.VerifyIL("""
                            add_E
                            {
                              // Code size       41 (0x29)
                              .maxstack  3
                              IL_0000:  ldarg.0
                              IL_0001:  ldfld      0x04000001
                              IL_0006:  stloc.0
                              IL_0007:  ldloc.0
                              IL_0008:  stloc.1
                              IL_0009:  ldloc.1
                              IL_000a:  ldarg.1
                              IL_000b:  call       0x0A000015
                              IL_0010:  castclass  0x01000019
                              IL_0015:  stloc.2
                              IL_0016:  ldarg.0
                              IL_0017:  ldflda     0x04000001
                              IL_001c:  ldloc.2
                              IL_001d:  ldloc.1
                              IL_001e:  call       0x2B000003
                              IL_0023:  stloc.0
                              IL_0024:  ldloc.0
                              IL_0025:  ldloc.1
                              IL_0026:  bne.un.s   IL_0007
                              IL_0028:  ret
                            }
                            remove_E
                            {
                              // Code size       41 (0x29)
                              .maxstack  3
                              IL_0000:  ldarg.0
                              IL_0001:  ldfld      0x04000001
                              IL_0006:  stloc.0
                              IL_0007:  ldloc.0
                              IL_0008:  stloc.1
                              IL_0009:  ldloc.1
                              IL_000a:  ldarg.1
                              IL_000b:  call       0x0A000017
                              IL_0010:  castclass  0x01000019
                              IL_0015:  stloc.2
                              IL_0016:  ldarg.0
                              IL_0017:  ldflda     0x04000001
                              IL_001c:  ldloc.2
                              IL_001d:  ldloc.1
                              IL_001e:  call       0x2B000003
                              IL_0023:  stloc.0
                              IL_0024:  ldloc.0
                              IL_0025:  ldloc.1
                              IL_0026:  bne.un.s   IL_0007
                              IL_0028:  ret
                            }
                            add_F, remove_F
                            {
                              // Code size       13 (0xd)
                              .maxstack  8
                              IL_0000:  ldstr      0x70000151
                              IL_0005:  ldc.i4.s   -2
                              IL_0007:  newobj     0x06000007
                              IL_000c:  throw
                            }
                            """);
                    })
                .Verify();
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

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, f0, f1)));

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
        public void UpdateType_AddAttributes()
        {
            var source0 = @"
class C
{
}";
            var source1 = @"
[System.ComponentModel.Description(""C"")]
class C
{
}";
            var source2 = @"
[System.ComponentModel.Description(""C"")]
[System.ObsoleteAttribute]
class C
{
}";

            var compilation0 = CreateCompilation(source0, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), options: TestOptions.DebugDll, targetFramework: TargetFramework.NetStandard20);
            var compilation1 = compilation0.WithSource(source1);
            var compilation2 = compilation1.WithSource(source2);

            var c0 = compilation0.GetMember<NamedTypeSymbol>("C");
            var c1 = compilation1.GetMember<NamedTypeSymbol>("C");
            var c2 = compilation2.GetMember<NamedTypeSymbol>("C");

            // Verify full metadata contains expected rows.
            var bytes0 = compilation0.EmitToArray();
            using var md0 = ModuleMetadata.CreateFromImage(bytes0);
            var reader0 = md0.MetadataReader;

            CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "C");

            Assert.Equal(3, reader0.CustomAttributes.Count);

            var generation0 = CreateInitialBaseline(compilation0,
                md0,
                EmptyLocalsProvider);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, c0, c1)));

            // Verify delta metadata contains expected rows.
            using var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;
            var readers = new[] { reader0, reader1 };

            CheckNames(readers, reader1.GetTypeDefNames(), "C");

            Assert.Equal(1, reader1.CustomAttributes.Count);

            CheckEncLogDefinitions(reader1,
                Row(2, TableIndex.TypeDef, EditAndContinueOperation.Default),
                Row(4, TableIndex.CustomAttribute, EditAndContinueOperation.Default));

            CheckEncMapDefinitions(reader1,
                Handle(2, TableIndex.TypeDef),
                Handle(4, TableIndex.CustomAttribute));

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, c1, c2)));

            // Verify delta metadata contains expected rows.
            using var md2 = diff2.GetMetadata();
            var reader2 = md2.Reader;
            readers = new[] { reader0, reader1, reader2 };

            CheckNames(readers, reader2.GetTypeDefNames(), "C");

            Assert.Equal(2, reader2.CustomAttributes.Count);

            CheckEncLogDefinitions(reader2,
                Row(2, TableIndex.TypeDef, EditAndContinueOperation.Default),
                Row(4, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(5, TableIndex.CustomAttribute, EditAndContinueOperation.Default));

            CheckEncMapDefinitions(reader2,
                Handle(2, TableIndex.TypeDef),
                Handle(4, TableIndex.CustomAttribute),
                Handle(5, TableIndex.CustomAttribute));
        }

        public static string MetadataUpdateOriginalTypeAttributeSource = @"
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Struct, AllowMultiple=false, Inherited=false)]
    public class MetadataUpdateOriginalTypeAttribute : Attribute
    {
        public MetadataUpdateOriginalTypeAttribute(Type originalType) => OriginalType = originalType;
        public Type OriginalType { get; }
    }
}
";

        public static string BadMetadataUpdateOriginalTypeAttributeSource = @"
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Struct, AllowMultiple=false, Inherited=false)]
    public class MetadataUpdateOriginalTypeAttribute : Attribute
    {
        public MetadataUpdateOriginalTypeAttribute(object originalType) => OriginalType = (Type)originalType;
        public Type OriginalType { get; }
    }
}
";

        [Theory]
        [CombinatorialData]
        public void ReplaceType(bool hasAttribute)
        {
            // using incorrect definition of the attribute so that it's easier to compare the two emit results (having and attribute and not having one):
            var attributeSource = hasAttribute ? MetadataUpdateOriginalTypeAttributeSource : BadMetadataUpdateOriginalTypeAttributeSource;

            var source0 = @"
class C 
{
    private int _x;
    void F(int x) {}
}" + attributeSource;
            var source1 = @"
class C
{
    private int _x;
    void F(int x, int y) { }
}" + attributeSource;
            var source2 = @"
class C
{
    private int _x;
    void F(int x, int y) { System.Console.WriteLine(1); }
}" + attributeSource;
            var source3 = @"
[System.Obsolete]
class C
{
    private int _x;
    void F(int x, int y) { System.Console.WriteLine(2); }
}" + attributeSource;

            var compilation0 = CreateCompilation(source0, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), options: TestOptions.DebugDll, targetFramework: TargetFramework.NetStandard20);
            var compilation1 = compilation0.WithSource(source1);
            var compilation2 = compilation1.WithSource(source2);
            var compilation3 = compilation2.WithSource(source3);

            var c0 = compilation0.GetMember<NamedTypeSymbol>("C");
            var c1 = compilation1.GetMember<NamedTypeSymbol>("C");
            var c2 = compilation2.GetMember<NamedTypeSymbol>("C");
            var c3 = compilation3.GetMember<NamedTypeSymbol>("C");
            var f2 = c2.GetMember<MethodSymbol>("F");
            var f3 = c3.GetMember<MethodSymbol>("F");

            // Verify full metadata contains expected rows.
            var bytes0 = compilation0.EmitToArray();
            using var md0 = ModuleMetadata.CreateFromImage(bytes0);
            var reader0 = md0.MetadataReader;

            CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "C", "MetadataUpdateOriginalTypeAttribute");

            var generation0 = CreateInitialBaseline(compilation0,
                md0,
                EmptyLocalsProvider);

            var baseTypeCount = reader0.TypeDefinitions.Count;
            var baseFieldCount = reader0.FieldDefinitions.Count;
            var baseMethodCount = reader0.MethodDefinitions.Count;
            var baseAttributeCount = reader0.CustomAttributes.Count;
            var baseParameterCount = reader0.GetParameters().Count();

            Assert.Equal(3, baseTypeCount);
            Assert.Equal(2, baseFieldCount);
            Assert.Equal(4, baseMethodCount);
            Assert.Equal(7, baseAttributeCount);
            Assert.Equal(2, baseParameterCount);

            var attributeTypeDefHandle = reader0.TypeDefinitions.Single(d => reader0.StringComparer.Equals(reader0.GetTypeDefinition(d).Name, "MetadataUpdateOriginalTypeAttribute"));
            var attributeCtorDefHandle = reader0.MethodDefinitions.Single(d =>
            {
                var methodDef = reader0.GetMethodDefinition(d);
                return methodDef.GetDeclaringType() == attributeTypeDefHandle && reader0.StringComparer.Equals(methodDef.Name, ".ctor");
            });

            void ValidateReplacedType(CompilationDifference diff, MetadataReader[] readers)
            {
                var generation = diff.NextGeneration.Ordinal;
                var reader = readers[generation];

                CheckNames(readers, diff.EmitResult.ChangedTypes, "C#" + generation);
                CheckNames(readers, reader.GetTypeDefNames(), "C#" + generation);

                CheckEncLogDefinitions(reader,
                    Row(baseTypeCount + generation, TableIndex.TypeDef, EditAndContinueOperation.Default), // adding a type def
                    Row(baseTypeCount + generation, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                    Row(baseFieldCount + generation, TableIndex.Field, EditAndContinueOperation.Default),
                    Row(baseTypeCount + generation, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                    Row(baseMethodCount + generation * 2 - 1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                    Row(baseTypeCount + generation, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                    Row(baseMethodCount + generation * 2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                    Row(baseMethodCount + generation * 2 - 1, TableIndex.MethodDef, EditAndContinueOperation.AddParameter),
                    Row(baseParameterCount + generation * 2 - 1, TableIndex.Param, EditAndContinueOperation.Default),
                    Row(baseMethodCount + generation * 2 - 1, TableIndex.MethodDef, EditAndContinueOperation.AddParameter),
                    Row(baseParameterCount + generation * 2, TableIndex.Param, EditAndContinueOperation.Default),
                    hasAttribute ? Row(baseAttributeCount + generation, TableIndex.CustomAttribute, EditAndContinueOperation.Default) : default);  // adding a new attribute row for attribute on C#* definition

                CheckEncMapDefinitions(reader,
                    Handle(baseTypeCount + generation, TableIndex.TypeDef),
                    Handle(baseFieldCount + generation, TableIndex.Field),
                    Handle(baseMethodCount + generation * 2 - 1, TableIndex.MethodDef),
                    Handle(baseMethodCount + generation * 2, TableIndex.MethodDef),
                    Handle(baseParameterCount + generation * 2 - 1, TableIndex.Param),
                    Handle(baseParameterCount + generation * 2, TableIndex.Param),
                    hasAttribute ? Handle(baseAttributeCount + generation, TableIndex.CustomAttribute) : default);

                var newTypeDefHandle = reader.TypeDefinitions.Single();
                var newTypeDef = reader.GetTypeDefinition(newTypeDefHandle);
                CheckStringValue(readers, newTypeDef.Name, "C#" + generation);

                if (hasAttribute)
                {
                    var attribute = reader.GetCustomAttribute(reader.CustomAttributes.Single());

                    // parent should be C#1
                    var aggregator = GetAggregator(readers);
                    var parentHandle = aggregator.GetGenerationHandle(attribute.Parent, out var parentGeneration);
                    Assert.Equal(generation, parentGeneration);
                    Assert.Equal(newTypeDefHandle, parentHandle);

                    // attribute constructor should match
                    var ctorHandle = aggregator.GetGenerationHandle(attribute.Constructor, out var ctorGeneration);
                    Assert.Equal(0, ctorGeneration);
                    Assert.Equal(attributeCtorDefHandle, ctorHandle);

                    // The attribute value encodes serialized type name. It should be the base name "C", not "C#1".
                    CheckBlobValue(readers, attribute.Value, new byte[] { 0x01, 0x00, 0x01, (byte)'C', 0x00, 0x00 });
                }
            }

            // This update emulates "Reloadable" type behavior - a new type is generated instead of updating the existing one.
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Replace, null, c1)));

            using var md1 = diff1.GetMetadata();
            ValidateReplacedType(diff1, new[] { reader0, md1.Reader });

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Replace, null, c2)));

            using var md2 = diff2.GetMetadata();
            ValidateReplacedType(diff2, new[] { reader0, md1.Reader, md2.Reader });

            // This update is an EnC update - even reloadable types are updated in-place
            var diff3 = compilation3.EmitDifference(
                diff2.NextGeneration,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, c2, c3),
                    SemanticEdit.Create(SemanticEditKind.Update, f2, f3)));

            // Verify delta metadata contains expected rows.
            using var md3 = diff3.GetMetadata();
            var reader3 = md3.Reader;
            var readers = new[] { reader0, md1.Reader, md2.Reader, reader3 };

            CheckNames(readers, reader3.GetTypeDefNames(), "C#2");
            CheckNames(readers, diff3.EmitResult.ChangedTypes, "C#2");

            // Obsolete attribute is added. MetadataUpdateOriginalTypeAttribute is still present on the type.
            CheckEncLogDefinitions(reader3,
                Row(5, TableIndex.TypeDef, EditAndContinueOperation.Default),
                Row(7, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(5, TableIndex.Param, EditAndContinueOperation.Default),
                Row(6, TableIndex.Param, EditAndContinueOperation.Default),
                Row(hasAttribute ? 9 : 8, TableIndex.CustomAttribute, EditAndContinueOperation.Default));

            CheckEncMapDefinitions(reader3,
                Handle(5, TableIndex.TypeDef),
                Handle(7, TableIndex.MethodDef),
                Handle(5, TableIndex.Param),
                Handle(6, TableIndex.Param),
                Handle(hasAttribute ? 9 : 8, TableIndex.CustomAttribute));

            // Obsolete attribute:
            CheckBlobValue(readers, reader3.GetCustomAttribute(reader3.CustomAttributes.First()).Value, new byte[] { 0x01, 0x00, 0x00, 0x00 });
        }

        [Fact]
        public void ReplaceType_UpdateNestedType()
        {
            using var _ = new EditAndContinueTest()
                .AddBaseline(
                    source: $$"""
                        using System;
                        
                        class C
                        {
                            class D
                            {
                                void M()
                                {
                                    Console.WriteLine("1");
                                }
                            }

                            void N()
                            {
                                Console.WriteLine("1");
                            }
                        }
                        """ + MetadataUpdateOriginalTypeAttributeSource,
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("<Module>", "C", "MetadataUpdateOriginalTypeAttribute", "D");
                        g.VerifyMethodDefNames("N", ".ctor", ".ctor", "get_OriginalType", "M", ".ctor");
                    })

                .AddGeneration(
                    source: """
                        using System;

                        class C
                        {
                            class D
                            {
                                void M()
                                {
                                    Console.WriteLine("2");
                                }
                            }

                            void N()
                            {
                                Console.WriteLine("2");
                            }
                        }
                        """ + MetadataUpdateOriginalTypeAttributeSource,
                    edits: new[] {
                        // Note: Nested type edit needs to be seen first to repro the bug. Real world scenario requires the nested
                        // class to be in a separate file.
                        Edit(SemanticEditKind.Update, c => c.GetMember("C.D.M")),
                        Edit(SemanticEditKind.Replace, c => null, newSymbolProvider: c => c.GetMember("C")),
                    },
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("C#1");
                        g.VerifyMethodDefNames("M", "N", ".ctor", ".ctor");
                        g.VerifyEncLogDefinitions(new[]
                        {
                            Row(5, TableIndex.TypeDef, EditAndContinueOperation.Default),
                            Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(5, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                            Row(7, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(5, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                            Row(8, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                            Row(9, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(8, TableIndex.CustomAttribute, EditAndContinueOperation.Default)
                        });
                        g.VerifyEncMapDefinitions(new[]
                        {
                            Handle(5, TableIndex.TypeDef),
                            Handle(5, TableIndex.MethodDef),
                            Handle(7, TableIndex.MethodDef),
                            Handle(8, TableIndex.MethodDef),
                            Handle(9, TableIndex.MethodDef),
                            Handle(8, TableIndex.CustomAttribute)
                        });
                    })
                .Verify();
        }

        [Fact]
        public void EventFields_Attributes()
        {
            var source0 = MarkedSource(@"
using System;

class C
{
    static event EventHandler E;
}
");
            var source1 = MarkedSource(@"
using System;

class C
{
    [System.Obsolete]
    static event EventHandler E;
}");

            var compilation0 = CreateCompilation(source0.Tree, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);

            var event0 = compilation0.GetMember<EventSymbol>("C.E");
            var event1 = compilation1.GetMember<EventSymbol>("C.E");

            var v0 = CompileAndVerify(compilation0);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);
            var reader0 = md0.MetadataReader;
            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "C");
            CheckNames(reader0, reader0.GetMethodDefNames(), "add_E", "remove_E", ".ctor");

            CheckAttributes(reader0,
                new CustomAttributeRow(Handle(1, TableIndex.MethodDef), Handle(4, TableIndex.MemberRef)),
                new CustomAttributeRow(Handle(1, TableIndex.Field), Handle(4, TableIndex.MemberRef)),
                new CustomAttributeRow(Handle(1, TableIndex.Field), Handle(5, TableIndex.MemberRef)),
                new CustomAttributeRow(Handle(1, TableIndex.Assembly), Handle(1, TableIndex.MemberRef)),
                new CustomAttributeRow(Handle(1, TableIndex.Assembly), Handle(2, TableIndex.MemberRef)),
                new CustomAttributeRow(Handle(1, TableIndex.Assembly), Handle(3, TableIndex.MemberRef)),
                new CustomAttributeRow(Handle(2, TableIndex.MethodDef), Handle(4, TableIndex.MemberRef)));

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, event0, event1)));

            // Verify delta metadata contains expected rows.
            using var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;
            var readers = new[] { reader0, reader1 };

            EncValidation.VerifyModuleMvid(1, reader0, reader1);

            CheckNames(readers, reader1.GetTypeDefNames());
            CheckNames(readers, reader1.GetMethodDefNames());
            CheckNames(readers, reader1.GetEventDefNames(), "E");

            CheckAttributes(reader1,
                new CustomAttributeRow(Handle(1, TableIndex.Event), Handle(10, TableIndex.MemberRef)));

            CheckEncLogDefinitions(reader1,
                Row(1, TableIndex.Event, EditAndContinueOperation.Default),
                Row(8, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(3, TableIndex.MethodSemantics, EditAndContinueOperation.Default),
                Row(4, TableIndex.MethodSemantics, EditAndContinueOperation.Default));

            CheckEncMapDefinitions(reader1,
                Handle(8, TableIndex.CustomAttribute),
                Handle(1, TableIndex.Event),
                Handle(3, TableIndex.MethodSemantics),
                Handle(4, TableIndex.MethodSemantics));
        }

        [Fact]
        public void ReplaceType_AsyncLambda()
        {
            var source0 = @"
using System.Threading.Tasks;
class C
{
    void F(int x) { Task.Run(async() => {}); }
}
";
            var source1 = @"
using System.Threading.Tasks;
class C
{
    void F(bool y) { Task.Run(async() => {}); }
}
";
            var source2 = @"
using System.Threading.Tasks;
class C
{
    void F(uint z) { Task.Run(async() => {}); }
}
";

            var compilation0 = CreateCompilation(source0, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), options: TestOptions.DebugDll, targetFramework: TargetFramework.NetStandard20);
            var compilation1 = compilation0.WithSource(source1);
            var compilation2 = compilation1.WithSource(source2);

            var c0 = compilation0.GetMember<NamedTypeSymbol>("C");
            var c1 = compilation1.GetMember<NamedTypeSymbol>("C");
            var c2 = compilation2.GetMember<NamedTypeSymbol>("C");
            var f2 = c2.GetMember<MethodSymbol>("F");

            // Verify full metadata contains expected rows.
            var bytes0 = compilation0.EmitToArray();
            using var md0 = ModuleMetadata.CreateFromImage(bytes0);
            var reader0 = md0.MetadataReader;

            CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "C", "<>c", "<<F>b__0_0>d");

            var generation0 = CreateInitialBaseline(compilation0,
                md0,
                EmptyLocalsProvider);

            // This update emulates "Reloadable" type behavior - a new type is generated instead of updating the existing one.
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Replace, null, c1)));

            // Verify delta metadata contains expected rows.
            using var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;
            var readers = new[] { reader0, reader1 };

            // A new nested type <>c is generated in C#1
            CheckNames(readers, reader1.GetTypeDefNames(), "C#1", "<>c", "<<F>b__0#1_0#1>d");
            CheckNames(readers, diff1.EmitResult.ChangedTypes, "C#1", "<>c", "<<F>b__0#1_0#1>d");

            // This update emulates "Reloadable" type behavior - a new type is generated instead of updating the existing one.
            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Replace, null, c2)));

            // Verify delta metadata contains expected rows.
            using var md2 = diff2.GetMetadata();
            var reader2 = md2.Reader;
            readers = new[] { reader0, reader1, reader2 };

            // A new nested type <>c is generated in C#2
            CheckNames(readers, reader2.GetTypeDefNames(), "C#2", "<>c", "<<F>b__0#2_0#2>d");
            CheckNames(readers, diff2.EmitResult.ChangedTypes, "C#2", "<>c", "<<F>b__0#2_0#2>d");
        }

        [Fact]
        public void ReplaceType_AsyncLambda_InNestedType()
        {
            var source0 = @"
using System.Threading.Tasks;
class C
{
    class D
    {
        void F(int x) { Task.Run(async() => {}); }
    }
}
";
            var source1 = @"
using System.Threading.Tasks;
class C
{
    class D
    {
        void F(bool y) { Task.Run(async() => {}); }
    }
}
";
            var source2 = @"
using System.Threading.Tasks;
class C
{
    class D
    {
        void F(uint z) { Task.Run(async() => {}); }
    }
}
";

            var compilation0 = CreateCompilation(source0, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), options: TestOptions.DebugDll, targetFramework: TargetFramework.NetStandard20);
            var compilation1 = compilation0.WithSource(source1);
            var compilation2 = compilation1.WithSource(source2);

            var c0 = compilation0.GetMember<NamedTypeSymbol>("C");
            var c1 = compilation1.GetMember<NamedTypeSymbol>("C");
            var c2 = compilation2.GetMember<NamedTypeSymbol>("C");
            var f2 = c2.GetMember<MethodSymbol>("F");

            // Verify full metadata contains expected rows.
            var bytes0 = compilation0.EmitToArray();
            using var md0 = ModuleMetadata.CreateFromImage(bytes0);
            var reader0 = md0.MetadataReader;

            CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "C", "D", "<>c", "<<F>b__0_0>d");

            var generation0 = CreateInitialBaseline(compilation0,
                md0,
                EmptyLocalsProvider);

            // This update emulates "Reloadable" type behavior - a new type is generated instead of updating the existing one.
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Replace, null, c1)));

            // Verify delta metadata contains expected rows.
            using var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;
            var readers = new[] { reader0, reader1 };

            // A new nested type <>c is generated in C#1
            CheckNames(readers, reader1.GetTypeDefNames(), "C#1", "D", "<>c", "<<F>b__0#1_0#1>d");
            CheckNames(readers, diff1.EmitResult.ChangedTypes, "C#1", "D", "<>c", "<<F>b__0#1_0#1>d");

            // This update emulates "Reloadable" type behavior - a new type is generated instead of updating the existing one.
            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Replace, null, c2)));

            // Verify delta metadata contains expected rows.
            using var md2 = diff2.GetMetadata();
            var reader2 = md2.Reader;
            readers = new[] { reader0, reader1, reader2 };

            // A new nested type <>c is generated in C#2
            CheckNames(readers, reader2.GetTypeDefNames(), "C#2", "D", "<>c", "<<F>b__0#2_0#2>d");
            CheckNames(readers, diff2.EmitResult.ChangedTypes, "C#2", "D", "<>c", "<<F>b__0#2_0#2>d");
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
            var compilation0 = CreateCompilation(source0, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), options: TestOptions.DebugDll);
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

            var generation0 = CreateInitialBaseline(compilation0, md0, EmptyLocalsProvider);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Insert, null, c1),
                    SemanticEdit.Create(SemanticEditKind.Update, f0, f1)));

            // Verify delta metadata contains expected rows.
            using var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;
            var readers = new[] { reader0, reader1 };

            CheckNames(readers, reader1.GetTypeDefNames(), "C", "D");
            CheckNames(readers, reader1.GetMethodDefNames(), "F", "G", ".ctor", ".ctor");
            CheckNames(readers, diff1.EmitResult.UpdatedMethods, "F");
            CheckNames(readers, diff1.EmitResult.ChangedTypes, "A", "C", "D");

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
            var compilation0 = CreateCompilation(source0, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);

            var bytes0 = compilation0.EmitToArray();
            using var md0 = ModuleMetadata.CreateFromImage(bytes0);
            var reader0 = md0.MetadataReader;
            CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "A", "B1", "B2", "C1", "C2");
            Assert.Equal(4, reader0.GetTableRowCount(TableIndex.NestedClass));

            var generation0 = CreateInitialBaseline(compilation0, md0, EmptyLocalsProvider);
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Insert, null, compilation1.GetMember<NamedTypeSymbol>("A.B3")),
                    SemanticEdit.Create(SemanticEditKind.Insert, null, compilation1.GetMember<NamedTypeSymbol>("A.B4"))));

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
            var compilation0 = CreateCompilation(source0, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), options: TestOptions.DebugDll);
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

            var generation0 = CreateInitialBaseline(compilation0, md0, EmptyLocalsProvider);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Insert, null, c1),
                    SemanticEdit.Create(SemanticEditKind.Update, f0, f1)));

            // Verify delta metadata contains expected rows.
            using var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;
            var readers = new[] { reader0, reader1 };

            CheckNames(readers, diff1.EmitResult.ChangedTypes, "A", "B`1", "C`1");

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
        [WorkItem(54939, "https://github.com/dotnet/roslyn/issues/54939")]
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
namespace N1.N2
{
    class D { public static void F() { } } 
}

class C
{
    static void Main() => N1.N2.D.F();
}";
            var source2 =
@"
namespace N1.N2
{
    class D { public static void F() { } } 

    namespace M1.M2
    {
        class E { public static void G() { } } 
    }
}

class C
{
    static void Main() => N1.N2.M1.M2.E.G();
}";
            var compilation0 = CreateCompilation(source0, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1);
            var compilation2 = compilation1.WithSource(source2);

            var main0 = compilation0.GetMember<MethodSymbol>("C.Main");
            var main1 = compilation1.GetMember<MethodSymbol>("C.Main");
            var main2 = compilation2.GetMember<MethodSymbol>("C.Main");
            var d1 = compilation1.GetMember<NamedTypeSymbol>("N1.N2.D");
            var e2 = compilation2.GetMember<NamedTypeSymbol>("N1.N2.M1.M2.E");

            using var md0 = ModuleMetadata.CreateFromImage(compilation0.EmitToArray());

            var generation0 = CreateInitialBaseline(compilation0, md0, EmptyLocalsProvider);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, main0, main1),
                    SemanticEdit.Create(SemanticEditKind.Insert, null, d1)));

            diff1.VerifyIL("C.Main", @"
{
  // Code size        7 (0x7)
  .maxstack  0
  IL_0000:  call       ""void N1.N2.D.F()""
  IL_0005:  nop
  IL_0006:  ret
}");
            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, main1, main2),
                    SemanticEdit.Create(SemanticEditKind.Insert, null, e2)));

            diff2.VerifyIL("C.Main", @"
{
  // Code size        7 (0x7)
  .maxstack  0
  IL_0000:  call       ""void N1.N2.M1.M2.E.G()""
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
            var compilation0 = CreateCompilation(source, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source);

            var method0 = compilation0.GetMember<NamedTypeSymbol>("C").GetMethod("I.M");
            var method1 = compilation1.GetMember<NamedTypeSymbol>("C").GetMethod("I.M");

            // Verify full metadata contains expected rows.
            var bytes0 = compilation0.EmitToArray();
            using var md0 = ModuleMetadata.CreateFromImage(bytes0);
            var reader0 = md0.MetadataReader;

            CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "I", "C");
            CheckNames(reader0, reader0.GetMethodDefNames(), "M", "I.M", ".ctor");

            var generation0 = CreateInitialBaseline(compilation0, md0, EmptyLocalsProvider);
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1)));

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
            var compilation0 = CreateCompilation(source0, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);
            var compilation2 = compilation0.WithSource(source2);

            var method1 = compilation1.GetMember<NamedTypeSymbol>("B").GetMethod("I.M");
            var method2 = compilation2.GetMember<NamedTypeSymbol>("B").GetMethod("I.M");

            var bytes0 = compilation0.EmitToArray();
            using var md0 = ModuleMetadata.CreateFromImage(bytes0);
            var reader0 = md0.MetadataReader;
            var generation0 = CreateInitialBaseline(compilation0, md0, EmptyLocalsProvider);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Insert, null, method1)));

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
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method1, method2)));

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
            var compilation0 = CreateCompilation(source, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source);

            var method0 = compilation0.GetMember<NamedTypeSymbol>("C").InstanceConstructors.Single();
            var method1 = compilation1.GetMember<NamedTypeSymbol>("C").InstanceConstructors.Single();

            var bytes0 = compilation0.EmitToArray();
            using var md0 = ModuleMetadata.CreateFromImage(bytes0);
            var reader0 = md0.MetadataReader;

            var generation0 = CreateInitialBaseline(compilation0, md0, EmptyLocalsProvider);
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1)));

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
            var compilation0 = CreateCompilation(source0, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), options: TestOptions.DebugDll, targetFramework: TargetFramework.Net50);
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

            var generation0 = CreateInitialBaseline(compilation0, md0, EmptyLocalsProvider);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Insert, null, x1),
                    SemanticEdit.Create(SemanticEditKind.Insert, null, y1),
                    SemanticEdit.Create(SemanticEditKind.Insert, null, m1),
                    SemanticEdit.Create(SemanticEditKind.Insert, null, n1),
                    SemanticEdit.Create(SemanticEditKind.Insert, null, p1),
                    SemanticEdit.Create(SemanticEditKind.Insert, null, q1),
                    SemanticEdit.Create(SemanticEditKind.Insert, null, e1),
                    SemanticEdit.Create(SemanticEditKind.Insert, null, f1),
                    SemanticEdit.Create(SemanticEditKind.Insert, null, j1),
                    SemanticEdit.Create(SemanticEditKind.Insert, null, cctor1)));

            using var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;
            var readers = new[] { reader0, reader1 };

            CheckNames(readers, diff1.EmitResult.ChangedTypes, "I", "J");

            CheckNames(readers, reader1.GetTypeDefNames(), "J");
            CheckNames(readers, reader1.GetFieldDefNames(), "X", "Y");
            CheckNames(readers, reader1.GetMethodDefNames(), "add_Y", "remove_Y", "M", "N", "get_P", "set_P", "get_Q", "set_Q", "add_E", "remove_E", "add_F", "remove_F", ".cctor");
            Assert.Equal(1, reader1.GetTableRowCount(TableIndex.NestedClass));

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, x1, x2),
                    SemanticEdit.Create(SemanticEditKind.Update, m1, m2),
                    SemanticEdit.Create(SemanticEditKind.Update, n1, n2),
                    SemanticEdit.Create(SemanticEditKind.Update, getP1, getP2),
                    SemanticEdit.Create(SemanticEditKind.Update, setP1, setP2),
                    SemanticEdit.Create(SemanticEditKind.Update, getQ1, getQ2),
                    SemanticEdit.Create(SemanticEditKind.Update, setQ1, setQ2),
                    SemanticEdit.Create(SemanticEditKind.Update, addE1, addE2),
                    SemanticEdit.Create(SemanticEditKind.Update, removeE1, removeE2),
                    SemanticEdit.Create(SemanticEditKind.Update, addF1, addF2),
                    SemanticEdit.Create(SemanticEditKind.Update, removeF1, removeF2),
                    SemanticEdit.Create(SemanticEditKind.Update, cctor1, cctor2)));

            using var md2 = diff2.GetMetadata();
            var reader2 = md2.Reader;
            readers = new[] { reader0, reader1, reader2 };

            CheckNames(readers, diff1.EmitResult.ChangedTypes, "I", "J");

            CheckNames(readers, reader2.GetTypeDefNames());
            CheckNames(readers, reader2.GetFieldDefNames(), "X");
            CheckNames(readers, reader2.GetMethodDefNames(), "M", "N", "get_P", "set_P", "get_Q", "set_Q", "add_E", "remove_E", "add_F", "remove_F", ".cctor");
            Assert.Equal(0, reader2.GetTableRowCount(TableIndex.NestedClass));

            CheckEncLog(reader2,
                Row(4, TableIndex.AssemblyRef, EditAndContinueOperation.Default),
                Row(11, TableIndex.TypeRef, EditAndContinueOperation.Default),
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
                Row(3, TableIndex.Param, EditAndContinueOperation.Default),
                Row(4, TableIndex.Param, EditAndContinueOperation.Default),
                Row(5, TableIndex.Param, EditAndContinueOperation.Default),
                Row(6, TableIndex.Param, EditAndContinueOperation.Default),
                Row(7, TableIndex.Param, EditAndContinueOperation.Default),
                Row(8, TableIndex.Param, EditAndContinueOperation.Default));

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
            var compilation0 = CreateCompilation(source0, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), options: TestOptions.DebugDll);
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

            var generation0 = CreateInitialBaseline(compilation0, md0, EmptyLocalsProvider);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Insert, null, compilation1.GetMember<MethodSymbol>("C.M2")),
                    SemanticEdit.Create(SemanticEditKind.Insert, null, compilation1.GetMember<FieldSymbol>("C.F2")),
                    SemanticEdit.Create(SemanticEditKind.Insert, null, compilation1.GetMember<PropertySymbol>("C.P2")),
                    SemanticEdit.Create(SemanticEditKind.Insert, null, compilation1.GetMember<EventSymbol>("C.E2"))));

            // Verify delta metadata contains expected rows.
            using var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;
            var readers = new[] { reader0, reader1 };

            CheckNames(readers, diff1.EmitResult.ChangedTypes, "C");

            CheckNames(readers, reader1.GetTypeDefNames());
            CheckNames(readers, reader1.GetMethodDefNames(), "M2", "get_P2", "add_E2", "remove_E2");

            CheckEncLogDefinitions(reader1,
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
                Row(7, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(13, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(14, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(15, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(16, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(17, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(18, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(19, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(20, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(4, TableIndex.MethodSemantics, EditAndContinueOperation.Default),
                Row(5, TableIndex.MethodSemantics, EditAndContinueOperation.Default),
                Row(6, TableIndex.MethodSemantics, EditAndContinueOperation.Default),
                Row(2, TableIndex.GenericParam, EditAndContinueOperation.Default));

            CheckEncMapDefinitions(reader1,
                Handle(3, TableIndex.Field),
                Handle(4, TableIndex.Field),
                Handle(12, TableIndex.MethodDef),
                Handle(13, TableIndex.MethodDef),
                Handle(14, TableIndex.MethodDef),
                Handle(15, TableIndex.MethodDef),
                Handle(8, TableIndex.Param),
                Handle(9, TableIndex.Param),
                Handle(7, TableIndex.CustomAttribute),
                Handle(13, TableIndex.CustomAttribute),
                Handle(14, TableIndex.CustomAttribute),
                Handle(15, TableIndex.CustomAttribute),
                Handle(16, TableIndex.CustomAttribute),
                Handle(17, TableIndex.CustomAttribute),
                Handle(18, TableIndex.CustomAttribute),
                Handle(19, TableIndex.CustomAttribute),
                Handle(20, TableIndex.CustomAttribute),
                Handle(3, TableIndex.StandAloneSig),
                Handle(4, TableIndex.StandAloneSig),
                Handle(2, TableIndex.Event),
                Handle(2, TableIndex.Property),
                Handle(4, TableIndex.MethodSemantics),
                Handle(5, TableIndex.MethodSemantics),
                Handle(6, TableIndex.MethodSemantics),
                Handle(2, TableIndex.GenericParam));

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
            var compilation0 = CreateCompilation(source0, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);

            // Verify full metadata contains expected rows.
            var bytes0 = compilation0.EmitToArray();
            using var md0 = ModuleMetadata.CreateFromImage(bytes0);
            var reader0 = md0.MetadataReader;
            CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "C");

            var generation0 = CreateInitialBaseline(compilation0, md0, EmptyLocalsProvider);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Insert, null, compilation1.GetMember<MethodSymbol>("C.M"))));

            // Verify delta metadata contains expected rows.
            using var md1 = diff1.GetMetadata();
            var readers = new[] { reader0, md1.Reader };

            CheckNames(readers, diff1.EmitResult.ChangedTypes, "C");

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
            var compilation0 = CreateCompilation(source0, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), options: TestOptions.DebugDll);
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
            var generation0 = CreateInitialBaseline(compilation0, md0, EmptyLocalsProvider);
            var method1 = compilation1.GetMember<MethodSymbol>("C.M");

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0))));

            // Verify delta metadata contains expected rows.
            using var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;
            var readers = new[] { reader0, reader1 };

            CheckNames(readers, diff1.EmitResult.ChangedTypes, "C");

            CheckNames(readers, reader1.GetTypeDefNames());
            CheckNames(readers, reader1.GetEventDefNames());
            CheckNames(readers, reader1.GetFieldDefNames());
            CheckNames(readers, reader1.GetMethodDefNames(), "M");
            CheckNames(readers, reader1.GetPropertyDefNames());
        }

        [Fact]
        public void ArrayInitializer()
        {
            var source0 = WithWindowsLineBreaks(@"
class C
{
    static void M()
    {
        int[] a = new[] { 1, 2, 3 };
    }
}");
            var source1 = WithWindowsLineBreaks(@"
class C
{
    static void M()
    {
        int[] a = new[] { 1, 2, 3, 4 };
    }
}");
            var parseOptions = TestOptions.Regular.WithNoRefSafetyRulesAttribute();
            var compilation0 = CreateCompilation(Parse(source0, "a.cs", parseOptions), options: TestOptions.DebugDll);
            var compilation1 = compilation0.RemoveAllSyntaxTrees().AddSyntaxTrees(Parse(source1, "a.cs", parseOptions));

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(testData: testData0);
            using var md0 = ModuleMetadata.CreateFromImage(bytes0);
            var reader0 = md0.MetadataReader;

            var generation0 = CreateInitialBaseline(compilation0,
                ModuleMetadata.CreateFromImage(bytes0),
                testData0.GetMethodData("C.M").EncDebugInfoProvider());

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, compilation0.GetMember("C.M"), compilation1.GetMember("C.M"))));

            var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;
            var readers = new[] { reader0, reader1 };

            CheckNames(readers, diff1.EmitResult.ChangedTypes, "C");

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
            var compilation0 = CreateCompilation(source0, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);
            var bytes0 = compilation0.EmitToArray();
            var generation0 = CreateInitialBaseline(compilation0, ModuleMetadata.CreateFromImage(bytes0), EmptyLocalsProvider);
            using var md0 = ModuleMetadata.CreateFromImage(bytes0);
            var reader0 = md0.MetadataReader;

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Insert, null, compilation1.GetMember<MethodSymbol>("C.puts"))));

            using var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;
            var readers = new[] { reader0, reader1 };

            CheckNames(readers, diff1.EmitResult.ChangedTypes, "C");

            CheckEncLogDefinitions(reader1,
                Row(2, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                Row(3, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(3, TableIndex.MethodDef, EditAndContinueOperation.AddParameter),
                Row(1, TableIndex.Param, EditAndContinueOperation.Default),
                Row(2, TableIndex.ImplMap, EditAndContinueOperation.Default));

            CheckEncMapDefinitions(reader1,
                Handle(3, TableIndex.MethodDef),
                Handle(1, TableIndex.Param),
                Handle(2, TableIndex.ImplMap));
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
            var compilation0 = CreateCompilation(source0, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);
            var bytes0 = compilation0.EmitToArray();
            var generation0 = CreateInitialBaseline(compilation0, ModuleMetadata.CreateFromImage(bytes0), EmptyLocalsProvider);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Insert, null, compilation1.GetMember<NamedTypeSymbol>("B"))));

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
            var compilation0 = CreateCompilation(parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), options: TestOptions.DebugDll, source:
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
            var generation0 = CreateInitialBaseline(compilation0, ModuleMetadata.CreateFromImage(bytes0), EmptyLocalsProvider);

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
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Insert, null, compilation1.GetMembers("M.C.M1")[2])));

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
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, compilation1.GetMember<MethodSymbol>("M.C.M2"),
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
            var compilation0 = CreateCompilation(source, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), options: options, references: new[] { CSharpRef });
            var bytes0 = compilation0.EmitToArray();
            var generation0 = CreateInitialBaseline(compilation0, ModuleMetadata.CreateFromImage(bytes0), EmptyLocalsProvider);

            var n = compilation0.GetMembers("C.M").Length;
            Assert.Equal(14, n);

            //static void M(A<B>.B<object> a)
            //{
            //    M(a);
            //    M((A<B>.B<B>)null);
            //}
            var compilation1 = compilation0.WithSource(source);
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, compilation0.GetMembers("C.M")[0], compilation1.GetMembers("C.M")[0])));

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
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, compilation1.GetMembers("C.M")[1], compilation2.GetMembers("C.M")[1])));

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
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, compilation2.GetMembers("C.M")[2], compilation3.GetMembers("C.M")[2])));

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
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, compilation3.GetMembers("C.M")[3], compilation4.GetMembers("C.M")[3])));

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
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, compilation4.GetMembers("C.M")[4], compilation5.GetMembers("C.M")[4])));

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
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, compilation5.GetMembers("C.M")[5], compilation6.GetMembers("C.M")[5])));

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
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, compilation6.GetMembers("C.M")[6], compilation7.GetMembers("C.M")[6])));

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
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, compilation7.GetMembers("C.M")[7], compilation8.GetMembers("C.M")[7])));

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
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, compilation8.GetMembers("C.M")[8], compilation9.GetMembers("C.M")[8])));

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
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, compilation9.GetMembers("C.M")[9], compilation10.GetMembers("C.M")[9])));

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
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, previousMethod, compilation.GetMembers("C.M")[10])),
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
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, previousMethod, compilation.GetMembers("C.M")[11])),
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
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, compilation10.GetMembers("C.M")[12], compilation11.GetMembers("C.M")[12])));

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
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, compilation11.GetMembers("C.M")[13], compilation12.GetMembers("C.M")[13])));

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

        [Fact]
        public void Struct_ImplementSynthesizedConstructor()
        {
            var source0 =
@"
struct S
{
    int a = 1;
    int b = 2;
    public S(int a)
    {
        this.a = a;
    }
}
";
            var source1 =
@"
struct S
{
    int a = 1;
    int b = 2;
    public S(int a)
    {
        this.a = a;
    }
    public S()
    {
        b = 3;
    }
}
";

            var compilation0 = CreateCompilation(source0, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1);

            var ctor0 = compilation0.GetMember<NamedTypeSymbol>("S").InstanceConstructors.Single(m => m.ParameterCount == 0);
            var ctor1 = compilation1.GetMember<NamedTypeSymbol>("S").InstanceConstructors.Single(m => m.ParameterCount == 0);

            var v0 = CompileAndVerify(compilation0, verify: Verification.Skipped);

            using var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            // Verify full metadata contains expected rows.
            var reader0 = md0.MetadataReader;

            CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "S");
            CheckNames(reader0, reader0.GetMethodDefNames(), ".ctor");

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Insert, null, ctor1)));

            // Verify delta metadata contains expected rows.
            using var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;
            var readers = new[] { reader0, reader1 };

            EncValidation.VerifyModuleMvid(1, reader0, reader1);

            CheckNames(readers, reader1.GetTypeDefNames());
            CheckNames(readers, reader1.GetMethodDefNames(), ".ctor");

            CheckEncLog(reader1,
                Row(2, TableIndex.AssemblyRef, EditAndContinueOperation.Default),
                Row(6, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(2, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default));

            CheckEncMap(reader1,
                Handle(6, TableIndex.TypeRef),
                Handle(2, TableIndex.MethodDef),
                Handle(2, TableIndex.AssemblyRef));
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
            var compilation0 = CreateCompilation(source0, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), options: TestOptions.DebugExe);
            var compilation1 = compilation0.WithSource(source1);

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(testData: testData0);
            var methodData0 = testData0.GetMethodData("C.Main");
            var method0 = compilation0.GetMember<MethodSymbol>("C.Main");
            var generation0 = CreateInitialBaseline(compilation0, ModuleMetadata.CreateFromImage(bytes0), methodData0.EncDebugInfoProvider());
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
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0))));
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
            var compilation0 = CreateCompilation(source0, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);
            var compilation2 = compilation1.WithSource(source2);

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(testData: testData0);
            var generation0 = CreateInitialBaseline(compilation0, ModuleMetadata.CreateFromImage(bytes0), EmptyLocalsProvider);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, compilation0.GetMembers("C.F")[1], compilation1.GetMembers("C.F")[1])));

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
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, compilation1.GetMembers("C.F")[1], compilation2.GetMembers("C.F")[1])));

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
            var compilation0 = CreateCompilation(source0, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);

            // Verify full metadata contains expected rows.
            var bytes0 = compilation0.EmitToArray();
            using var md0 = ModuleMetadata.CreateFromImage(bytes0);
            var reader0 = md0.MetadataReader;
            CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "C");
            CheckNames(reader0, reader0.GetMethodDefNames(), "F", "G", ".ctor");
            CheckNames(reader0, reader0.GetMemberRefNames(), ".ctor", ".ctor", ".ctor", "WriteLine", ".ctor");

            var generation0 = CreateInitialBaseline(compilation0, md0, EmptyLocalsProvider);
            var method0 = compilation0.GetMember<MethodSymbol>("C.G");
            var method1 = compilation1.GetMember<MethodSymbol>("C.G");

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(
                    SemanticEditKind.Update,
                    method0,
                    method1,
                    GetEquivalentNodesMap(method1, method0))));

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
            var compilation0 = CreateCompilation(source0, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);
            var compilation2 = compilation1.WithSource(source2);
            var compilation3 = compilation2.WithSource(source3);

            var method0 = compilation0.GetMember<MethodSymbol>("B.M");
            var methodN = compilation0.GetMember<MethodSymbol>("B.N");

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(testData: testData0);
            var generation0 = CreateInitialBaseline(compilation0,
                ModuleMetadata.CreateFromImage(bytes0),
                m => testData0.GetMethodData(methodNames0[MetadataTokens.GetRowNumber(m) - 1]).GetEncDebugInfo());

            #region Gen1 

            var method1 = compilation1.GetMember<MethodSymbol>("B.M");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0))));

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
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method1, method2, GetEquivalentNodesMap(method2, method1))));

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
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method2, method3, GetEquivalentNodesMap(method3, method2))));

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
        [Fact]
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
            var compilation0 = CreateCompilation(source0, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);
            var compilation2 = compilation1.WithSource(source2);

            var bytes0 = compilation0.EmitToArray();
            var generation0 = CreateInitialBaseline(compilation0, ModuleMetadata.CreateFromImage(bytes0), EmptyLocalsProvider);

            var m1 = compilation1.GetMember<MethodSymbol>("C.M");
            var m2 = compilation2.GetMember<MethodSymbol>("C.M");

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Insert, null, m1, null)));

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, m1, m2, GetEquivalentNodesMap(m2, m1))));

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
            var compilation0 = CreateCompilation(source0, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);
            var method0 = compilation0.GetMember<MethodSymbol>("C.Main");
            var method1 = compilation1.GetMember<MethodSymbol>("C.Main");
            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(testData: testData0);

            var generation0 = CreateInitialBaseline(compilation0, ModuleMetadata.CreateFromImage(bytes0), testData0.GetMethodData("C.Main").EncDebugInfoProvider());

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0))));
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
            var generation0 = CreateInitialBaseline(compilation0,
                ModuleMetadata.CreateFromImage(bytes0),
                testData0.GetMethodData("C.M").EncDebugInfoProvider());

            var method1 = compilation1.GetMember<MethodSymbol>("C.M");
            var diff1a = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1)));

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
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1)));

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
            var compilation0 = CreateCompilation(source0, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);
            var compilation2 = compilation1.WithSource(source2);
            var compilation3 = compilation2.WithSource(source3);

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(testData: testData0);
            var methodData0 = testData0.GetMethodData("C.M1");
            var method0 = compilation0.GetMember<MethodSymbol>("C.M1");
            var generation0 = CreateInitialBaseline(compilation0, ModuleMetadata.CreateFromImage(bytes0), methodData0.EncDebugInfoProvider());

            var method1 = compilation1.GetMember<MethodSymbol>("C.M1");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Insert, null, compilation1.GetMember<NamedTypeSymbol>("A")),
                    SemanticEdit.Create(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0))));

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
                    SemanticEdit.Create(SemanticEditKind.Update, method1, method2, GetEquivalentNodesMap(method2, method1))));

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
                    SemanticEdit.Create(SemanticEditKind.Update, method2, method3, GetEquivalentNodesMap(method3, method2))));

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

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

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
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

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
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f1, f2, GetSyntaxMapFromMarkers(source1, source2))));

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

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

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
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f1, f2, GetSyntaxMapFromMarkers(source1, source2))));

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
        [Fact]
        [WorkItem(825903, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/825903")]
        public void AnonymousTypes()
        {
            using var _ = new EditAndContinueTest()
                .AddBaseline(
                    source: """
                        class C
                        {
                            void F()
                            {
                                var f = new { a = 1, b = 2 };
                            }
                        }
                        """,
                    validator: g =>
                    {
                        g.VerifySynthesizedMembers(displayTypeKind: true,
                        [
                            "class <>f__AnonymousType0<<a>j__TPar, <b>j__TPar>: {Equals, GetHashCode, ToString}"
                        ]);

                        g.VerifySynthesizedTypes(
                            "<>f__AnonymousType0<<a>j__TPar, <b>j__TPar>");

                        g.VerifyIL("C.F", """
                            {
                                // Code size       10 (0xa)
                                .maxstack  2
                                .locals init (<>f__AnonymousType0<int, int> V_0) //f
                                IL_0000:  nop
                                IL_0001:  ldc.i4.1
                                IL_0002:  ldc.i4.2
                                IL_0003:  newobj     "<>f__AnonymousType0<int, int>..ctor(int, int)"
                                IL_0008:  stloc.0
                                IL_0009:  ret
                            }
                            """);
                    })
                .AddGeneration(
                    // 1
                    source: """
                        class C
                        {
                            void F()
                            {
                                var g = new { x = 1 };
                                var f = new { a = 1, b = 2 };
                            }
                        }
                        """,
                        edits:
                        [
                            Edit(SemanticEditKind.Update, c => c.GetMember("C.F"), preserveLocalVariables: true),
                        ],
                        validator: g =>
                        {
                            g.VerifySynthesizedMembers(displayTypeKind: true,
                            [
                                "class <>f__AnonymousType0<<a>j__TPar, <b>j__TPar>: {Equals, GetHashCode, ToString}",
                                "class <>f__AnonymousType1<<x>j__TPar>: {Equals, GetHashCode, ToString}"
                            ]);

                            g.VerifySynthesizedTypes(
                                "<>f__AnonymousType0<<a>j__TPar, <b>j__TPar>",
                                "<>f__AnonymousType1<<x>j__TPar>");

                            g.VerifyTypeDefNames("<>f__AnonymousType1`1");
                            g.VerifyMethodDefNames("F", "get_x", ".ctor", "Equals", "GetHashCode", "ToString");

                            g.VerifyIL("C.F", """
                                {
                                  // Code size       17 (0x11)
                                  .maxstack  2
                                  .locals init (<>f__AnonymousType0<int, int> V_0, //f
                                                <>f__AnonymousType1<int> V_1) //g
                                  IL_0000:  nop
                                  IL_0001:  ldc.i4.1
                                  IL_0002:  newobj     "<>f__AnonymousType1<int>..ctor(int)"
                                  IL_0007:  stloc.1
                                  IL_0008:  ldc.i4.1
                                  IL_0009:  ldc.i4.2
                                  IL_000a:  newobj     "<>f__AnonymousType0<int, int>..ctor(int, int)"
                                  IL_000f:  stloc.0
                                  IL_0010:  ret
                                }
                                """);
                        })
                .AddGeneration(
                    // 2
                    source: """
                        class C
                        {
                            void F()
                            {
                                var f = new { a = 1, b = 2, c = 3 };
                            }
                        }
                        """,
                        edits:
                        [
                            Edit(SemanticEditKind.Update, c => c.GetMember("C.F"), preserveLocalVariables: true),
                        ],
                        validator: g =>
                        {
                            g.VerifySynthesizedMembers(displayTypeKind: true,
                            [
                                "class <>f__AnonymousType0<<a>j__TPar, <b>j__TPar>: {Equals, GetHashCode, ToString}",
                                "class <>f__AnonymousType1<<x>j__TPar>: {Equals, GetHashCode, ToString}",
                                "class <>f__AnonymousType2<<a>j__TPar, <b>j__TPar, <c>j__TPar>: {Equals, GetHashCode, ToString}",
                            ]);

                            g.VerifySynthesizedTypes(
                                "<>f__AnonymousType0<<a>j__TPar, <b>j__TPar>",
                                "<>f__AnonymousType1<<x>j__TPar>",
                                "<>f__AnonymousType2<<a>j__TPar, <b>j__TPar, <c>j__TPar>");

                            g.VerifyTypeDefNames("<>f__AnonymousType2`3");
                            g.VerifyMethodDefNames("F", "get_a", "get_b", "get_c", ".ctor", "Equals", "GetHashCode", "ToString");
                        })
                .AddGeneration(
                    // 3
                    source: """
                        class C
                        {
                            void F()
                            {
                                var f = new { a = 1, b = 2, c = 3 };
                                var g = new { x = 1, y = 2 };
                            }
                        }
                        """,
                        edits:
                        [
                            Edit(SemanticEditKind.Update, c => c.GetMember("C.F"), preserveLocalVariables: true),
                        ],
                        validator: g =>
                        {
                            g.VerifySynthesizedMembers(displayTypeKind: true,
                            [
                                "class <>f__AnonymousType0<<a>j__TPar, <b>j__TPar>: {Equals, GetHashCode, ToString}",
                                "class <>f__AnonymousType1<<x>j__TPar>: {Equals, GetHashCode, ToString}",
                                "class <>f__AnonymousType2<<a>j__TPar, <b>j__TPar, <c>j__TPar>: {Equals, GetHashCode, ToString}",
                                "class <>f__AnonymousType3<<x>j__TPar, <y>j__TPar>: {Equals, GetHashCode, ToString}",
                            ]);

                            g.VerifySynthesizedTypes(
                                "<>f__AnonymousType0<<a>j__TPar, <b>j__TPar>",
                                "<>f__AnonymousType1<<x>j__TPar>",
                                "<>f__AnonymousType2<<a>j__TPar, <b>j__TPar, <c>j__TPar>",
                                "<>f__AnonymousType3<<x>j__TPar, <y>j__TPar>");

                            g.VerifyTypeDefNames("<>f__AnonymousType3`2");
                            g.VerifyMethodDefNames("F", "get_x", "get_y", ".ctor", "Equals", "GetHashCode", "ToString");
                        })
                .Verify();
        }

        /// <summary>
        /// Anonymous type names with module ids
        /// and gaps in indices.
        /// </summary>
        [ConditionalFact(typeof(WindowsOnly), Reason = "ILASM doesn't support Portable PDBs")]
        [WorkItem(2982, "https://github.com/dotnet/coreclr/issues/2982")]
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

            var compilation0 = CreateCompilation(source0, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), options: TestOptions.DebugDll, targetFramework: TargetFramework.NetStandard20);
            var compilation1 = compilation0.WithSource(source1);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");

            var moduleMetadata0 = ((AssemblyMetadata)metadata0.GetMetadataNoCopy()).GetModules()[0];
            var generation0 = CreateInitialBaseline(compilation0, moduleMetadata0, m => default);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetEquivalentNodesMap(f1, f0))));

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

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var method0 = compilation0.GetMember<MethodSymbol>("B.G");
            var method1 = compilation1.GetMember<MethodSymbol>("B.G");
            var method2 = compilation2.GetMember<MethodSymbol>("B.G");
            var method3 = compilation3.GetMember<MethodSymbol>("B.G");

            var reader0 = md0.MetadataReader;
            CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "<>f__AnonymousType0`1", "A", "B");

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1, GetSyntaxMapFromMarkers(source0, source1))));

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
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method1, method2, GetSyntaxMapFromMarkers(source1, source2))));

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
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method2, method3, GetSyntaxMapFromMarkers(source2, source3))));

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
            var compilation0 = CreateCompilation(source0, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), options: TestOptions.DebugDll);
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

            var generation0 = CreateInitialBaseline(compilation0,
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
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetEquivalentNodesMap(f1, f0))));

            using var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;
            var readers = new List<MetadataReader> { reader0, reader1 };

            CheckNames(readers, reader1.GetTypeDefNames(), "<>f__AnonymousType1`2"); // one additional type

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, g1, g2, GetEquivalentNodesMap(g2, g1))));

            using var md2 = diff2.GetMetadata();
            var reader2 = md2.Reader;
            readers.Add(reader2);

            CheckNames(readers, reader2.GetTypeDefNames()); // no additional types

            var diff3 = compilation3.EmitDifference(
                diff2.NextGeneration,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, g2, g3, GetEquivalentNodesMap(g3, g2))));

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
            var compilation0 = CreateCompilation(source0, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);
            var compilation2 = compilation1.WithSource(source2);
            var compilation3 = compilation2.WithSource(source3);
            var compilation4 = compilation3.WithSource(source4);

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(testData: testData0);
            using var md0 = ModuleMetadata.CreateFromImage(bytes0);
            var generation0 = CreateInitialBaseline(compilation0, md0, testData0.GetMethodData("C.F").EncDebugInfoProvider());

            var method0 = compilation0.GetMember<MethodSymbol>("C.F");
            var reader0 = md0.MetadataReader;
            CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "C");

            var method1 = compilation1.GetMember<MethodSymbol>("C.F");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0))));
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

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "<>f__AnonymousType0`2", "<>f__AnonymousType1`2", "C");

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, m0, m1, GetSyntaxMapFromMarkers(source0, source1))));

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
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, m1, m2, GetSyntaxMapFromMarkers(source1, source2))));

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

            var compilation0 = CreateCompilationWithMscorlib461(new[] { source0.Tree }, new[] { SystemCoreRef }, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);
            var compilation2 = compilation0.WithSource(source2.Tree);

            var v0 = CompileAndVerify(compilation0);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");
            var f2 = compilation2.GetMember<MethodSymbol>("C.F");

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

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
                    SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

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
                    SemanticEdit.Create(SemanticEditKind.Update, f1, f2, GetSyntaxMapFromMarkers(source1, source2))));

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

            var compilation0 = CreateCompilationWithMscorlib461(new[] { source0.Tree }, new[] { SystemCoreRef }, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);
            var compilation2 = compilation0.WithSource(source2.Tree);

            var v0 = CompileAndVerify(compilation0);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");
            var f2 = compilation2.GetMember<MethodSymbol>("C.F");

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

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
                    SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

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
                    SemanticEdit.Create(SemanticEditKind.Update, f1, f2, GetSyntaxMapFromMarkers(source1, source2))));

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
            using var _ = new EditAndContinueTest(references: [CSharpRef])
                .AddBaseline(
                    source: """
                    using System.Linq;

                    class C
                    {
                        static void F(string[] args)
                        {
                            args = new[] { "a", "bB", "Cc", "DD" };
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
                    """,
                    validator: v =>
                    {
                        v.VerifyLocalSignature("C.F", """
                        .locals init (System.Collections.Generic.IEnumerable<<anonymous type: string Value, int Length>> V_0, //result
                                      System.Collections.Generic.IEnumerable<string> V_1) //newArgs
                        """);
                    })
                .AddGeneration(
                    // 1
                    source: """
                    using System.Linq;

                    class C
                    {
                        static void F(string[] args)
                        {
                            args = new[] { "a", "bB", "Cc", "DD" };
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
                    """,
                    edits:
                    [
                        Edit(SemanticEditKind.Update, c => c.GetMember("C.F"), preserveLocalVariables: true)
                    ],
                    validator: v =>
                    {
                        v.VerifySynthesizedMembers(
                            "System.Runtime.CompilerServices.HotReloadException",
                            "C.<>o__0#1: {<>p__0}",
                            "C: {<>c, <>o__0#1}",
                            "C.<>c: {<>9__0_0, <>9__0_1, <>9__0_2, <>9__0_3#1, <>9__0_4#1, <>9__0_3, <>9__0_6#1, <>9__0_4, <>9__0_5, <>9__0_6, <>9__0_10#1, <F>b__0_0, <F>b__0_1, <F>b__0_2, <F>b__0_3#1, <F>b__0_4#1, <F>b__0_3, <F>b__0_6#1, <F>b__0_4, <F>b__0_5, <F>b__0_6, <F>b__0_10#1}",
                            "<>f__AnonymousType4<<<>h__TransparentIdentifier0>j__TPar, <length>j__TPar>: {Equals, GetHashCode, ToString}",
                            "<>f__AnonymousType2<<Value>j__TPar, <Length>j__TPar>: {Equals, GetHashCode, ToString}",
                            "<>f__AnonymousType5<<Head>j__TPar, <Tail>j__TPar>: {Equals, GetHashCode, ToString}",
                            "<>f__AnonymousType3<<a>j__TPar, <value>j__TPar>: {Equals, GetHashCode, ToString}",
                            "<>f__AnonymousType0<<a>j__TPar, <x>j__TPar>: {Equals, GetHashCode, ToString}",
                            "<>f__AnonymousType1<<<>h__TransparentIdentifier0>j__TPar, <y>j__TPar>: {Equals, GetHashCode, ToString}");

                        v.VerifyLocalSignature("C.F", """
                        .locals init (System.Collections.Generic.IEnumerable<<anonymous type: string Value, int Length>> V_0, //result
                                      System.Collections.Generic.IEnumerable<string> V_1, //newArgs
                                      <>f__AnonymousType5<dynamic, dynamic> V_2, //list
                                      int V_3, //i
                                      <>f__AnonymousType5<string, dynamic> V_4, //linked
                                      object V_5, //str
                                      <>f__AnonymousType5<string, dynamic> V_6,
                                      object V_7,
                                      bool V_8)
                        """);
                    })
                .Verify();
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

            var compilation0 = CreateCompilationWithMscorlib461(new[] { source0.Tree }, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);
            var compilation2 = compilation1.WithSource(source2.Tree);

            var v0 = CompileAndVerify(compilation0);
            v0.VerifyDiagnostics();
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");
            var f2 = compilation2.GetMember<MethodSymbol>("C.F");

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

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
                    SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

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
                    SemanticEdit.Create(SemanticEditKind.Update, f1, f2, GetSyntaxMapFromMarkers(source1, source2))));

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
            var generation0 = CreateInitialBaseline(compilation0, md0, m => default);

            var method1 = compilation1.GetMember<MethodSymbol>("C.M1");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0))));

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
            var generation0 = CreateInitialBaseline(compilation0,
                moduleMetadata0,
                m => default);

            var method1 = compilation1.GetMember<MethodSymbol>("C.F");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0))));

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
}", methodToken: diff1.EmitResult.UpdatedMethods.Single());
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
            var generation0 = CreateInitialBaseline(compilation0,
                ModuleMetadata.CreateFromImage(bytes0),
                methodData0.EncDebugInfoProvider());

            var method1 = compilation1.GetMember<MethodSymbol>("C.M");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0))));

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
            var generation0 = CreateInitialBaseline(compilation0,
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
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0))));

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
            var compilation0 = CreateCompilation(source0, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), options: TestOptions.DebugDll, targetFramework: TargetFramework.NetStandard20);
            var compilation1 = compilation0.WithSource(source1);

            var method0 = compilation0.GetMember<MethodSymbol>("C.M");
            var generation0 = CreateInitialBaseline(compilation0, md0, EmptyLocalsProvider);

            var method1 = compilation1.GetMember<MethodSymbol>("C.M");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0))));

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
            var generation0 = CreateInitialBaseline(compilation0,
                modMeta,
                methodData0.EncDebugInfoProvider());

            var method1 = compilation1.GetMember<MethodSymbol>("C.M");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0))));

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
            var generation0 = CreateInitialBaseline(compilation0, md0, methodData0.EncDebugInfoProvider());
            var method0 = compilation0.GetMember<MethodSymbol>("A.M");
            var method1 = compilation1.GetMember<MethodSymbol>("A.M");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0))));
            diff1.EmitResult.Diagnostics.Verify();

            // Source method with no dynamic operations.
            methodData0 = testData0.GetMethodData("A.N");
            generation0 = CreateInitialBaseline(compilation0, md0, methodData0.EncDebugInfoProvider());
            method0 = compilation0.GetMember<MethodSymbol>("A.N");
            method1 = compilation1.GetMember<MethodSymbol>("A.N");
            diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0))));
            diff1.EmitResult.Diagnostics.Verify();

            // Explicit .ctor with dynamic operations.
            methodData0 = testData0.GetMethodData("A..ctor");
            generation0 = CreateInitialBaseline(compilation0, md0, methodData0.EncDebugInfoProvider());
            method0 = compilation0.GetMember<MethodSymbol>("A..ctor");
            method1 = compilation1.GetMember<MethodSymbol>("A..ctor");
            diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0))));
            diff1.EmitResult.Diagnostics.Verify();

            // Explicit .cctor with dynamic operations.
            methodData0 = testData0.GetMethodData("A..cctor");
            generation0 = CreateInitialBaseline(compilation0, md0, methodData0.EncDebugInfoProvider());
            method0 = compilation0.GetMember<MethodSymbol>("A..cctor");
            method1 = compilation1.GetMember<MethodSymbol>("A..cctor");
            diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0))));
            diff1.EmitResult.Diagnostics.Verify();

            // Implicit .ctor with dynamic operations.
            methodData0 = testData0.GetMethodData("B..ctor");
            generation0 = CreateInitialBaseline(compilation0, md0, methodData0.EncDebugInfoProvider());
            method0 = compilation0.GetMember<MethodSymbol>("B..ctor");
            method1 = compilation1.GetMember<MethodSymbol>("B..ctor");
            diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0))));
            diff1.EmitResult.Diagnostics.Verify();

            // Implicit .cctor with dynamic operations.
            methodData0 = testData0.GetMethodData("B..cctor");
            generation0 = CreateInitialBaseline(compilation0, md0, methodData0.EncDebugInfoProvider());
            method0 = compilation0.GetMember<MethodSymbol>("B..cctor");
            method1 = compilation1.GetMember<MethodSymbol>("B..cctor");
            diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0))));
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
            var compilation0 = CreateCompilationWithMscorlib461(new[] { source0.Tree }, new[] { SystemCoreRef, CSharpRef }, options: ComSafeDebugDll);

            var compilation1 = compilation0.WithSource(source1.Tree);
            var compilation2 = compilation1.WithSource(source2.Tree);

            var v0 = CompileAndVerify(compilation0);
            v0.VerifyDiagnostics();
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");
            var f2 = compilation2.GetMember<MethodSymbol>("C.F");

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

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
                    SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

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
                    SemanticEdit.Create(SemanticEditKind.Update, f1, f2, GetSyntaxMapFromMarkers(source1, source2))));

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

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

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
            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1, GetSyntaxMapFromMarkers(source0, source1))));

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
            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1, GetSyntaxMapFromMarkers(source0, source1))));

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
            var compilation0 = CreateCompilation(source0, parseOptions: TestOptions.Regular10, options: TestOptions.DebugDll, references: new MetadataReference[] { referencePIA, CSharpRef });
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

            var generation0 = CreateInitialBaseline(compilation0, md0, methodData0.EncDebugInfoProvider());

            // Disallow edits that require NoPIA references.
            var diff1A = compilation1A.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1A, GetEquivalentNodesMap(method1A, method0))));

            diff1A.EmitResult.Diagnostics.Verify(
                // error CS7094: Cannot continue since the edit includes a reference to an embedded type: 'S'.
                Diagnostic(ErrorCode.ERR_EncNoPIAReference).WithArguments("S"),
                // error CS7094: Cannot continue since the edit includes a reference to an embedded type: 'IA'.
                Diagnostic(ErrorCode.ERR_EncNoPIAReference).WithArguments("IA"));

            // Allow edits that do not require NoPIA references,
            // even if the previous code included references.
            var diff1B = compilation1B.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1B, GetEquivalentNodesMap(method1B, method0))));

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
            var generation0 = CreateInitialBaseline(compilation0, md0, m => default);
            var method0 = compilation0.GetMember<MethodSymbol>("C.M");
            var method1 = compilation1.GetMember<MethodSymbol>("C.M");

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1)));

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

        [Fact]
        public void Operator_Delete()
        {
            using var _ = new EditAndContinueTest()
                .AddBaseline(
                    source: $$"""
                        class C
                        {
                            public static bool operator !(C c) => true;
                        }
                        """,
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("<Module>", "C");
                        g.VerifyMethodDefNames("op_LogicalNot", ".ctor");
                        g.VerifyMemberRefNames(/*CompilationRelaxationsAttribute.*/".ctor", /*RuntimeCompatibilityAttribute.*/".ctor", /*Object.*/".ctor", /*DebuggableAttribute*/".ctor");
                    })

                .AddGeneration(
                    source: """
                        class C
                        {
                        }
                        """,
                    edits: new[] {
                        Edit(SemanticEditKind.Delete, symbolProvider: c => c.GetMember("C.op_LogicalNot"), newSymbolProvider: c => c.GetMember("C")),
                    },
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("HotReloadException");
                        g.VerifyMethodDefNames("op_LogicalNot", ".ctor");
                        g.VerifyMemberRefNames(/* Exception */ ".ctor", /* CompilerGeneratedAttribute */ ".ctor", "Invoke");

                        g.VerifyEncLogDefinitions(
                        [
                            Row(1, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                            Row(3, TableIndex.TypeDef, EditAndContinueOperation.Default),
                            Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(1, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(2, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                            Row(3, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(4, TableIndex.CustomAttribute, EditAndContinueOperation.Default)
                        ]);

                        g.VerifyEncMapDefinitions(
                        [
                            Handle(3, TableIndex.TypeDef),
                            Handle(1, TableIndex.Field),
                            Handle(2, TableIndex.Field),
                            Handle(1, TableIndex.MethodDef),
                            Handle(3, TableIndex.MethodDef),
                            Handle(4, TableIndex.CustomAttribute),
                            Handle(1, TableIndex.StandAloneSig)
                        ]);

                        g.VerifyIL("""
                            op_LogicalNot
                            {
                              // Code size       13 (0xd)
                              .maxstack  8
                              IL_0000:  ldstr      0x70000005
                              IL_0005:  ldc.i4.s   -2
                              IL_0007:  newobj     0x06000003
                              IL_000c:  throw
                            }
                            .ctor
                            {
                              // Code size       33 (0x21)
                              .maxstack  2
                              IL_0000:  ldarg.0
                              IL_0001:  ldarg.1
                              IL_0002:  call       0x0A000006
                              IL_0007:  nop
                              IL_0008:  ldarg.0
                              IL_0009:  ldarg.2
                              IL_000a:  stfld      0x04000001
                              IL_000f:  ldsfld     0x04000002
                              IL_0014:  dup
                              IL_0015:  stloc.0
                              IL_0016:  brfalse.s  IL_0020
                              IL_0018:  ldloc.0
                              IL_0019:  ldarg.0
                              IL_001a:  callvirt   0x0A000007
                              IL_001f:  nop
                              IL_0020:  ret
                            }
                            """);
                    })
                .Verify();
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
            var compilation0 = CreateCompilation(source, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), options: TestOptions.DebugDll, targetFramework: TargetFramework.NetStandard20);
            var compilation1 = compilation0.WithSource(source);
            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(testData: testData0);
            var methodData0 = testData0.GetMethodData("C.M");

            using var md0 = ModuleMetadata.CreateFromImage(bytes0);
            var reader0 = md0.MetadataReader;
            CheckNames(reader0, reader0.GetAssemblyRefNames(), "netstandard");
            var method0 = compilation0.GetMember<MethodSymbol>("C.M");
            var method1 = compilation1.GetMember<MethodSymbol>("C.M");

            var generation0 = CreateInitialBaseline(compilation0, md0, methodData0.EncDebugInfoProvider());

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0))));

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
            var compilation0 = CreateCompilation(source0, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), options: TestOptions.DebugDll, targetFramework: TargetFramework.NetStandard20);
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
            var generation0 = CreateInitialBaseline(compilation0,
                md0,
                EmptyLocalsProvider);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0F, method1F, syntaxMap: s => null)));

            using var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;
            var readers = new List<MetadataReader> { reader0, reader1 };

            CheckNames(readers, reader1.GetAssemblyRefNames(), "netstandard");
            CheckNames(readers, reader1.GetTypeDefNames(), "<>f__AnonymousType1`1");
            CheckNames(readers, reader1.GetTypeRefNames(), "CompilerGeneratedAttribute", "DebuggerDisplayAttribute", "Object", "DebuggerBrowsableState", "DebuggerBrowsableAttribute", "DebuggerHiddenAttribute", "EqualityComparer`1", "String", "IFormatProvider");

            // Change method updated in generation 1.
            var diff2F = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method1F, method2F, syntaxMap: s => null)));

            using var md2 = diff2F.GetMetadata();
            var reader2 = md2.Reader;
            readers.Add(reader2);
            CheckNames(readers, reader2.GetAssemblyRefNames(), "netstandard");
            CheckNames(readers, reader2.GetTypeDefNames());
            CheckNames(readers, reader2.GetTypeRefNames(), "Object");

            // Change method unchanged since generation 0.
            var diff2G = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method1G, method2G, syntaxMap: s => null)));
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
            var compilation0 = CreateCompilation(source0, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);
            var bytes0 = compilation0.EmitToArray();
            using (new EnsureEnglishUICulture())
            using (var md0 = ModuleMetadata.CreateFromImage(bytes0))
            {
                var method0F = compilation0.GetMember<MethodSymbol>("C.F");
                var generation0 = CreateInitialBaseline(compilation0,
                    md0,
                    EmptyLocalsProvider);
                var method1F = compilation1.GetMember<MethodSymbol>("C.F");

                using MemoryStream mdStream = new MemoryStream(), ilStream = new MemoryStream(), pdbStream = new MemoryStream();
                var isAddedSymbol = new Func<ISymbol, bool>(s => false);

                var badStream = new BrokenStream();
                badStream.BreakHow = BrokenStream.BreakHowType.ThrowOnWrite;

                var result = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0F, method1F, syntaxMap: s => null)),
                    isAddedSymbol,
                    badStream,
                    ilStream,
                    pdbStream,
                    EmitDifferenceOptions.Default,
                    new CompilationTestData(),
                    CancellationToken.None);
                Assert.False(result.Success);
                result.Diagnostics.Verify(
                    // error CS8104: An error occurred while writing the output file: System.IO.IOException: I/O error occurred.
                    Diagnostic(ErrorCode.ERR_PeWritingFailure).WithArguments(badStream.ThrownException.ToString()).WithLocation(1, 1)
                    );

                result = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0F, method1F, syntaxMap: s => null)),
                    isAddedSymbol,
                    mdStream,
                    badStream,
                    pdbStream,
                    EmitDifferenceOptions.Default,
                    new CompilationTestData(),
                    CancellationToken.None);
                Assert.False(result.Success);
                result.Diagnostics.Verify(
                    // error CS8104: An error occurred while writing the output file: System.IO.IOException: I/O error occurred.
                    Diagnostic(ErrorCode.ERR_PeWritingFailure).WithArguments(badStream.ThrownException.ToString()).WithLocation(1, 1)
                    );

                result = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0F, method1F, syntaxMap: s => null)),
                    isAddedSymbol,
                    mdStream,
                    ilStream,
                    badStream,
                    EmitDifferenceOptions.Default,
                    new CompilationTestData(),
                    CancellationToken.None);
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
            var compilation0 = CreateCompilation(source0, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);
            var bytes0 = compilation0.EmitToArray(EmitOptions.Default.WithDebugInformationFormat(DebugInformationFormat.PortablePdb));
            using (new EnsureEnglishUICulture())
            using (var md0 = ModuleMetadata.CreateFromImage(bytes0))
            {
                var method0F = compilation0.GetMember<MethodSymbol>("C.F");
                var generation0 = CreateInitialBaseline(compilation0,
                    md0,
                    EmptyLocalsProvider);
                var method1F = compilation1.GetMember<MethodSymbol>("C.F");

                using MemoryStream mdStream = new MemoryStream(), ilStream = new MemoryStream(), pdbStream = new MemoryStream();
                var isAddedSymbol = new Func<ISymbol, bool>(s => false);

                var badStream = new BrokenStream();
                badStream.BreakHow = BrokenStream.BreakHowType.ThrowOnWrite;

                var result = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0F, method1F, syntaxMap: s => null)),
                    isAddedSymbol,
                    mdStream,
                    ilStream,
                    badStream,
                    EmitDifferenceOptions.Default,
                    new CompilationTestData(),
                    CancellationToken.None);
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
            var compilation0 = CreateCompilation(source0, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);

            // Verify full metadata contains expected rows.
            var bytes0 = compilation0.EmitToArray();
            using var md0 = ModuleMetadata.CreateFromImage(bytes0);
            var diff1 = compilation1.EmitDifference(
CreateInitialBaseline(compilation0, md0, EmptyLocalsProvider),
ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Insert, null, compilation1.GetMember<MethodSymbol>("C.Main"))),
testData: new CompilationTestData { SymWriterFactory = _ => new MockSymUnmanagedWriter() });

            diff1.EmitResult.Diagnostics.Verify(
                // error CS0041: Unexpected error writing debug information -- 'MockSymUnmanagedWriter error message'
                Diagnostic(ErrorCode.FTL_DebugEmitFailure).WithArguments("MockSymUnmanagedWriter error message"));

            Assert.False(diff1.EmitResult.Success);
        }

        [Fact]
        public void TypeDefinitionDocumentInformation()
        {
            var sourceA0 = """
                interface I {}
                """;
            var sourceB0 = """
                class C
                {
                    static int Main() => 1;
                }
                """;

            var sourceA1 = """
                interface I {}
                """;
            var sourceB1 = """
                class C
                {
                    static int Main() => 2;
                }
                """;

            var compilation0 = CreateCompilation(new[] { sourceA0, sourceB0 }, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(new[] { sourceA1, sourceB1 });

            var bytes0 = compilation0.EmitToArray(EmitOptions.Default.WithDebugInformationFormat(DebugInformationFormat.PortablePdb));
            using var md0 = ModuleMetadata.CreateFromImage(bytes0);
            var diff1 = compilation1.EmitDifference(
                    CreateInitialBaseline(compilation0, md0, EmptyLocalsProvider),
                    ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, compilation0.GetMember("C.Main"), compilation1.GetMember("C.Main"))));

            Assert.True(diff1.EmitResult.Success);

            using var provider = MetadataReaderProvider.FromPortablePdbStream(new MemoryStream(diff1.PdbDelta.ToArray()));
            var pdbReader = provider.GetMetadataReader();

            // No CDIs should be emitted, specifically not PortableCustomDebugInfoKinds.TypeDefinitionDocuments
            Assert.Empty(pdbReader.CustomDebugInformation.Select(cdi => pdbReader.GetGuid(pdbReader.GetCustomDebugInformation(cdi).Kind)));
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
            var compilation0 = CreateCompilation(source0, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), options: TestOptions.DebugDll, targetFramework: TargetFramework.NetStandard20);
            var compilation1 = compilation0.WithSource(source1);
            var compilation2 = compilation1.WithSource(source2);
            var bytes0 = compilation0.EmitToArray();

            using var md0 = ModuleMetadata.CreateFromImage(bytes0);
            var reader0 = md0.MetadataReader;
            CheckNames(reader0, reader0.GetAssemblyRefNames(), "netstandard");
            var method0F = compilation0.GetMember<MethodSymbol>("C.F");
            var generation0 = CreateInitialBaseline(compilation0, md0, EmptyLocalsProvider);
            var method1F = compilation1.GetMember<MethodSymbol>("C.F");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0F, method1F, syntaxMap: s => null)));

            var handle = MetadataTokens.BlobHandle(1);
            byte[] value0 = reader0.GetBlobBytes(handle);
            Assert.Equal("20-01-01-08", BitConverter.ToString(value0));

            using var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;
            var method2F = compilation2.GetMember<MethodSymbol>("C.F");
            var diff2F = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method1F, method2F, syntaxMap: s => null)));

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
            var generationA0 = CreateInitialBaseline(compilationA0, mdA0, EmptyLocalsProvider);
            var generationB0 = CreateInitialBaseline(compilationB0, mdB0, EmptyLocalsProvider);
            var mA1 = compilationA1.GetMember<MethodSymbol>("A.M");
            var mX1 = compilationA1.GetMember<TypeSymbol>("X");

            var allAddedSymbols = new ISymbol[] { mA1.GetPublicSymbol(), mX1.GetPublicSymbol() };

            var diffA1 = compilationA1.EmitDifference(
                generationA0,
                edits: ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Insert, null, mA1),
                    SemanticEdit.Create(SemanticEditKind.Insert, null, mX1)),
                allAddedSymbols: allAddedSymbols);

            diffA1.EmitResult.Diagnostics.Verify();

            var diffB1 = compilationB1.EmitDifference(
                generationB0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, compilationB0.GetMember<MethodSymbol>("B.F"), compilationB1.GetMember<MethodSymbol>("B.F")),
                    SemanticEdit.Create(SemanticEditKind.Insert, null, compilationB1.GetMember<TypeSymbol>("Y"))),
                allAddedSymbols: allAddedSymbols);

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
            var generationB0 = CreateInitialBaseline(compilationB0, mdB0, testDataB0.GetMethodData("B.F").EncDebugInfoProvider());

            var f0 = compilationB0.GetMember<MethodSymbol>("B.F");
            var f1 = compilationB1.GetMember<MethodSymbol>("B.F");
            var f2 = compilationB2.GetMember<MethodSymbol>("B.F");

            var diffB1 = compilationB1.EmitDifference(
                generationB0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetEquivalentNodesMap(f1, f0))));

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
               ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f1, f2, GetEquivalentNodesMap(f2, f1))));

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

            var compilation0 = CreateCompilation(source0, targetFramework: TargetFramework.StandardAndCSharp,
                parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(),
                options: ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All), assemblyName: "A");

            var compilation1 = compilation0.WithSource(source1);
            var compilation2 = compilation1.WithSource(source2);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");
            var f_byte2 = compilation2.GetMembers("C.F").Single(m => m.ToString() == "C.F(dynamic, byte)");

            var v0 = CompileAndVerify(compilation0);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, f0, f1)));

            diff1.EmitResult.Diagnostics.Verify();

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Insert, null, f_byte2)));

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
            var generation0 = CreateInitialBaseline(compilation0, md0, EmptyLocalsProvider);
            var method0 = compilation0.GetMember<MethodSymbol>("C.F");

            for (int i = 2; i <= 50; i++)
            {
                var compilation1 = compilation0.WithSource(String.Format(source, i));
                var method1 = compilation1.GetMember<MethodSymbol>("C.F");
                var diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1)));

                compilation0 = compilation1;
                method0 = method1;
                generation0 = diff1.NextGeneration;
            }
        }

        [Theory]
        [InlineData(typeof(IOException))]
        [InlineData(typeof(BadImageFormatException))]
        [InlineData(typeof(InvalidDataException))]
        [WorkItem(187868, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/187868")]
        public void SymReaderErrors(Type exceptionType)
        {
            using var _ = new EditAndContinueTest(assemblyName: "test")
                .AddBaseline(
                    """
                    class C { void F() { int x = 1; } }
                    """,
                    debugInformationProvider: _ => throw (Exception)Activator.CreateInstance(exceptionType, ["bug!"]))
                .AddGeneration(
                    // 1
                    """
                    class C { void F() { int x = 2; } }
                    """,
                    edits:
                    [
                        Edit(SemanticEditKind.Update, c => c.GetMember("C.F"), preserveLocalVariables: true)
                    ],
                    expectedErrors:
                    [
                        // (1,16): error CS7103: Unable to read debug information of method 'C.F()' (token 0x06000001) from assembly 'test, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null': bug!
                        // class C { void F() { int x = 2; } }
                        Diagnostic(ErrorCode.ERR_InvalidDebugInfo, "F").WithArguments("C.F()", "100663297", "test, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "bug!").WithLocation(1, 16)
                    ])
                .Verify();
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

            var generation0 = CreateInitialBaseline(compilation0, md0, methodHandle =>
            {
                throw new ArgumentOutOfRangeException();
            });

            // the compiler should't swallow any exceptions but InvalidDataException, IOException and BadImageFormatException
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1)))));
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

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

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
                    SemanticEdit.Create(SemanticEditKind.Update, f1, f2, GetSyntaxMapFromMarkers(source1, source2))));

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

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

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
                    SemanticEdit.Create(SemanticEditKind.Update, f1, f2, GetSyntaxMapFromMarkers(source1, source2))));

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

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, ctor0, ctor1, GetSyntaxMapFromMarkers(source0, source1))));

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
                    SemanticEdit.Create(SemanticEditKind.Update, ctor1, ctor2, GetSyntaxMapFromMarkers(source1, source0))));

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

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, ctor0, ctor1, GetSyntaxMapFromMarkers(source0, source1))));

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
                    SemanticEdit.Create(SemanticEditKind.Update, ctor1, ctor2, GetSyntaxMapFromMarkers(source1, source0))));

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
            var compilation0 = CreateCompilation(source0.Tree, references: new[] { RefSafetyRulesAttributeLib }, options: ComSafeDebugDll);
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

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, n0, n1, GetSyntaxMapFromMarkers(source0, source1))));

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
                    SemanticEdit.Create(SemanticEditKind.Update, n1, n2, GetSyntaxMapFromMarkers(source1, source0))));

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
  .locals init (System.ValueTuple<int, System.ValueTuple<int, int>> V_0, //x
                int V_1)
  IL_0000:  nop
  IL_0001:  ldloca.s   V_0
  IL_0003:  ldc.i4.1
  IL_0004:  ldc.i4.2
  IL_0005:  ldc.i4.3
  IL_0006:  newobj     ""System.ValueTuple<int, int>..ctor(int, int)""
  IL_000b:  call       ""System.ValueTuple<int, System.ValueTuple<int, int>>..ctor(int, System.ValueTuple<int, int>)""
  IL_0010:  ldloc.0
  IL_0011:  ldfld      ""int System.ValueTuple<int, System.ValueTuple<int, int>>.Item1""
  IL_0016:  ldloc.0
  IL_0017:  ldfld      ""System.ValueTuple<int, int> System.ValueTuple<int, System.ValueTuple<int, int>>.Item2""
  IL_001c:  ldfld      ""int System.ValueTuple<int, int>.Item1""
  IL_0021:  add
  IL_0022:  ldloc.0
  IL_0023:  ldfld      ""System.ValueTuple<int, int> System.ValueTuple<int, System.ValueTuple<int, int>>.Item2""
  IL_0028:  ldfld      ""int System.ValueTuple<int, int>.Item2""
  IL_002d:  add
  IL_002e:  stloc.1
  IL_002f:  br.s       IL_0031
  IL_0031:  ldloc.1
  IL_0032:  ret
}
");

            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

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
                    SemanticEdit.Create(SemanticEditKind.Update, f1, f2, GetSyntaxMapFromMarkers(source1, source2))));

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

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

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
                    SemanticEdit.Create(SemanticEditKind.Update, f1, f2, GetSyntaxMapFromMarkers(source1, source2))));

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
  .locals init (System.ValueTuple<int, System.ValueTuple<bool, double>>[] V_0,
                int V_1,
                int V_2, //x
                bool V_3, //y
                double V_4, //z
                System.ValueTuple<bool, double> V_5)
  IL_0000:  nop
  IL_0001:  nop
  IL_0002:  call       ""System.ValueTuple<int, System.ValueTuple<bool, double>>[] C.F()""
  IL_0007:  stloc.0
  IL_0008:  ldc.i4.0
  IL_0009:  stloc.1
  IL_000a:  br.s       IL_003f
  IL_000c:  ldloc.0
  IL_000d:  ldloc.1
  IL_000e:  ldelem     ""System.ValueTuple<int, System.ValueTuple<bool, double>>""
  IL_0013:  dup
  IL_0014:  ldfld      ""System.ValueTuple<bool, double> System.ValueTuple<int, System.ValueTuple<bool, double>>.Item2""
  IL_0019:  stloc.s    V_5
  IL_001b:  ldfld      ""int System.ValueTuple<int, System.ValueTuple<bool, double>>.Item1""
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

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

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
                System.ValueTuple<int, System.ValueTuple<bool, double>>[] V_6,
                int V_7,
                System.ValueTuple<bool, double> V_8)
  IL_0000:  nop
  IL_0001:  nop
  IL_0002:  call       ""System.ValueTuple<int, System.ValueTuple<bool, double>>[] C.F()""
  IL_0007:  stloc.s    V_6
  IL_0009:  ldc.i4.0
  IL_000a:  stloc.s    V_7
  IL_000c:  br.s       IL_0045
  IL_000e:  ldloc.s    V_6
  IL_0010:  ldloc.s    V_7
  IL_0012:  ldelem     ""System.ValueTuple<int, System.ValueTuple<bool, double>>""
  IL_0017:  dup
  IL_0018:  ldfld      ""System.ValueTuple<bool, double> System.ValueTuple<int, System.ValueTuple<bool, double>>.Item2""
  IL_001d:  stloc.s    V_8
  IL_001f:  ldfld      ""int System.ValueTuple<int, System.ValueTuple<bool, double>>.Item1""
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
                    SemanticEdit.Create(SemanticEditKind.Update, f1, f2, GetSyntaxMapFromMarkers(source1, source2))));

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
                System.ValueTuple<int, System.ValueTuple<bool, double>>[] V_9,
                int V_10,
                System.ValueTuple<bool, double> V_11) //yz
  IL_0000:  nop
  IL_0001:  nop
  IL_0002:  call       ""System.ValueTuple<int, System.ValueTuple<bool, double>>[] C.F()""
  IL_0007:  stloc.s    V_9
  IL_0009:  ldc.i4.0
  IL_000a:  stloc.s    V_10
  IL_000c:  br.s       IL_0034
  IL_000e:  ldloc.s    V_9
  IL_0010:  ldloc.s    V_10
  IL_0012:  ldelem     ""System.ValueTuple<int, System.ValueTuple<bool, double>>""
  IL_0017:  dup
  IL_0018:  ldfld      ""int System.ValueTuple<int, System.ValueTuple<bool, double>>.Item1""
  IL_001d:  stloc.2
  IL_001e:  ldfld      ""System.ValueTuple<bool, double> System.ValueTuple<int, System.ValueTuple<bool, double>>.Item2""
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
            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

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
                    SemanticEdit.Create(SemanticEditKind.Update, f1, f2, GetSyntaxMapFromMarkers(source1, source2))));

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

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, ctor0, ctor1, GetSyntaxMapFromMarkers(source0, source1))));

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
                    SemanticEdit.Create(SemanticEditKind.Update, ctor1, ctor2, GetSyntaxMapFromMarkers(source1, source0))));

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

            var compilation0 = CreateCompilation(source0.Tree, references: new[] { RefSafetyRulesAttributeLib }, options: ComSafeDebugDll);
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
            var reader0 = md0.MetadataReader;
            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, ctor0, ctor1, GetSyntaxMapFromMarkers(source0, source1))));

            using var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;
            var readers = new[] { reader0, reader1 };

            CheckNames(readers, diff1.EmitResult.ChangedTypes, "C", "<>c__DisplayClass0_0");

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
                    SemanticEdit.Create(SemanticEditKind.Update, ctor1, ctor2, GetSyntaxMapFromMarkers(source1, source0))));

            using var md2 = diff2.GetMetadata();
            var reader2 = md2.Reader;
            readers = new[] { reader0, reader1, reader2 };

            CheckNames(readers, diff2.EmitResult.ChangedTypes, "C", "<>c__DisplayClass0_0");

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

            var compilation0 = CreateCompilation(source0.Tree, references: new[] { RefSafetyRulesAttributeLib }, options: ComSafeDebugDll);
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

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, ctor0, ctor1, GetSyntaxMapFromMarkers(source0, source1))));

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
                    SemanticEdit.Create(SemanticEditKind.Update, ctor1, ctor2, GetSyntaxMapFromMarkers(source1, source0))));

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

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, ctor0, ctor1, GetSyntaxMapFromMarkers(source0, source1))));

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
                    SemanticEdit.Create(SemanticEditKind.Update, ctor1, ctor2, GetSyntaxMapFromMarkers(source1, source0))));

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

            var compilation0 = CreateCompilation(source0.Tree, references: new[] { RefSafetyRulesAttributeLib }, options: ComSafeDebugDll);
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

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, ctor0, ctor1, GetSyntaxMapFromMarkers(source0, source1))));

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
                    SemanticEdit.Create(SemanticEditKind.Update, ctor1, ctor2, GetSyntaxMapFromMarkers(source1, source0))));

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
            var compilation0 = CreateCompilation(source0.Tree, references: new[] { RefSafetyRulesAttributeLib }, options: ComSafeDebugDll);
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

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, n0, n1, GetSyntaxMapFromMarkers(source0, source1))));

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
                    SemanticEdit.Create(SemanticEditKind.Update, n1, n2, GetSyntaxMapFromMarkers(source1, source0))));

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
            var compilation0 = CreateCompilation(source0.Tree, references: new[] { RefSafetyRulesAttributeLib }, options: ComSafeDebugDll);
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

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, n0, n1, GetSyntaxMapFromMarkers(source0, source1))));

            diff1.VerifySynthesizedMembers(
                "Program: {<>c, <>c__DisplayClass2_0}",
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
                    SemanticEdit.Create(SemanticEditKind.Update, n1, n2, GetSyntaxMapFromMarkers(source1, source0))));

            diff2.VerifySynthesizedMembers(
                "Program.<>c__DisplayClass2_0: {x, <N>b__1}",
                "Program: {<>c, <>c__DisplayClass2_0}",
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

        [Fact]
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
            var compilation0 = CreateCompilation(source0.Tree, references: new[] { RefSafetyRulesAttributeLib }, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);
            var compilation2 = compilation1.WithSource(source0.Tree);

            var n0 = compilation0.GetMember<MethodSymbol>("Program.G");
            var n1 = compilation1.GetMember<MethodSymbol>("Program.G");
            var n2 = compilation2.GetMember<MethodSymbol>("Program.G");

            var v0 = CompileAndVerify(compilation0);
            v0.VerifyIL("Program.G(int)", @"
    {
      // Code size       33 (0x21)
      .maxstack  1
      .locals init (int V_0,
                    object V_1)
      IL_0000:  nop
      IL_0001:  ldc.i4.1
      IL_0002:  brtrue.s   IL_0005
      IL_0004:  nop
      IL_0005:  ldarg.0
      IL_0006:  brfalse.s  IL_000a
      IL_0008:  br.s       IL_000e
      IL_000a:  ldc.i4.0
      IL_000b:  stloc.0
      IL_000c:  br.s       IL_0012
      IL_000e:  ldc.i4.1
      IL_000f:  stloc.0
      IL_0010:  br.s       IL_0012
      IL_0012:  ldc.i4.1
      IL_0013:  brtrue.s   IL_0016
      IL_0015:  nop
      IL_0016:  ldloc.0
      IL_0017:  box        ""int""
      IL_001c:  stloc.1
      IL_001d:  br.s       IL_001f
      IL_001f:  ldloc.1
      IL_0020:  ret
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

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, n0, n1, GetSyntaxMapFromMarkers(source0, source1))));

            diff1.VerifySynthesizedMembers();

            diff1.VerifyIL("Program.G(int)", @"
    {
      // Code size       52 (0x34)
      .maxstack  2
      .locals init ([int] V_0,
                    [object] V_1,
                    int V_2, //x
                    int V_3,
                    int V_4,
                    int V_5,
                    object V_6)
      IL_0000:  nop
      IL_0001:  ldarg.0
      IL_0002:  stloc.3
      IL_0003:  ldloca.s   V_2
      IL_0005:  call       ""int Program.N(out int)""
      IL_000a:  stloc.s    V_5
      IL_000c:  ldc.i4.1
      IL_000d:  brtrue.s   IL_0010
      IL_000f:  nop
      IL_0010:  ldloc.s    V_5
      IL_0012:  brfalse.s  IL_0016
      IL_0014:  br.s       IL_001b
      IL_0016:  ldc.i4.0
      IL_0017:  stloc.s    V_4
      IL_0019:  br.s       IL_0020
      IL_001b:  ldc.i4.1
      IL_001c:  stloc.s    V_4
      IL_001e:  br.s       IL_0020
      IL_0020:  ldc.i4.1
      IL_0021:  brtrue.s   IL_0024
      IL_0023:  nop
      IL_0024:  ldloc.3
      IL_0025:  ldloc.s    V_4
      IL_0027:  add
      IL_0028:  box        ""int""
      IL_002d:  stloc.s    V_6
      IL_002f:  br.s       IL_0031
      IL_0031:  ldloc.s    V_6
      IL_0033:  ret
    }
");

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, n1, n2, GetSyntaxMapFromMarkers(source1, source0))));

            diff2.VerifySynthesizedMembers();

            diff2.VerifyIL("Program.G(int)", @"
    {
      // Code size       38 (0x26)
      .maxstack  1
      .locals init ([int] V_0,
                    [object] V_1,
                    [int] V_2,
                    [int] V_3,
                    [int] V_4,
                    [int] V_5,
                    [object] V_6,
                    int V_7,
                    object V_8)
      IL_0000:  nop
      IL_0001:  ldc.i4.1
      IL_0002:  brtrue.s   IL_0005
      IL_0004:  nop
      IL_0005:  ldarg.0
      IL_0006:  brfalse.s  IL_000a
      IL_0008:  br.s       IL_000f
      IL_000a:  ldc.i4.0
      IL_000b:  stloc.s    V_7
      IL_000d:  br.s       IL_0014
      IL_000f:  ldc.i4.1
      IL_0010:  stloc.s    V_7
      IL_0012:  br.s       IL_0014
      IL_0014:  ldc.i4.1
      IL_0015:  brtrue.s   IL_0018
      IL_0017:  nop
      IL_0018:  ldloc.s    V_7
      IL_001a:  box        ""int""
      IL_001f:  stloc.s    V_8
      IL_0021:  br.s       IL_0023
      IL_0023:  ldloc.s    V_8
      IL_0025:  ret
    }
");
        }

        [Fact]
        public void AddUsing_AmbiguousCode()
        {
            var source0 = MarkedSource(@"
using System.Threading;

class C
{
    static void E() 
    {
        var t = new Timer(s => System.Console.WriteLine(s));
    }
}");
            var source1 = MarkedSource(@"
using System.Threading;
using System.Timers;

class C
{
    static void E() 
    {
        var t = new Timer(s => System.Console.WriteLine(s));
    }
    
    static void G() 
    {
        System.Console.WriteLine(new TimersDescriptionAttribute(""""));
    }
}");

            var compilation0 = CreateCompilation(source0.Tree, targetFramework: TargetFramework.NetStandard20, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);

            var e0 = compilation0.GetMember<MethodSymbol>("C.E");
            var e1 = compilation1.GetMember<MethodSymbol>("C.E");
            var g1 = compilation1.GetMember<MethodSymbol>("C.G");

            var v0 = CompileAndVerify(compilation0);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);
            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            // Pretend there was an update to C.E to ensure we haven't invalidated the test

            var diffError = compilation1.EmitDifference(
              generation0,
              ImmutableArray.Create(
                  SemanticEdit.Create(SemanticEditKind.Update, e0, e1, GetSyntaxMapFromMarkers(source0, source1))));

            diffError.EmitResult.Diagnostics.Verify(
                       // (9,21): error CS0104: 'Timer' is an ambiguous reference between 'System.Threading.Timer' and 'System.Timers.Timer'
                       //         var t = new Timer(s => System.Console.WriteLine(s));
                       Diagnostic(ErrorCode.ERR_AmbigContext, "Timer").WithArguments("Timer", "System.Threading.Timer", "System.Timers.Timer").WithLocation(9, 21));

            // Semantic errors are reported only for the bodies of members being emitted so we shouldn't see any

            var diff = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Insert, null, g1)));

            diff.EmitResult.Diagnostics.Verify();

            diff.VerifyIL(@"C.G", @"
{
  // Code size       18 (0x12)
  .maxstack  1
  IL_0000:  nop
  IL_0001:  ldstr      """"
  IL_0006:  newobj     ""System.Timers.TimersDescriptionAttribute..ctor(string)""
  IL_000b:  call       ""void System.Console.WriteLine(object)""
  IL_0010:  nop
  IL_0011:  ret
}
");
        }

        [Fact]
        public void Records_AddWellKnownMember()
        {
            var source0 =
@"
#nullable enable
namespace N
{
    record R(int X)
    {
    }
}
";
            var source1 =
@"
#nullable enable
namespace N
{
    record R(int X)
    {
        protected virtual bool PrintMembers(System.Text.StringBuilder sb) // note the different parameter name
        {
            return true;
        }
    }
}
";

            var compilation0 = CreateCompilation(new[] { source0, IsExternalInitTypeDefinition }, references: new[] { RefSafetyRulesAttributeLib }, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(new[] { source1, IsExternalInitTypeDefinition });

            var printMembers0 = compilation0.GetMember<MethodSymbol>("N.R.PrintMembers");
            var printMembers1 = compilation1.GetMember<MethodSymbol>("N.R.PrintMembers");

            var v0 = CompileAndVerify(compilation0, verify: Verification.Skipped);

            using var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            // Verify full metadata contains expected rows.
            var reader0 = md0.MetadataReader;

            CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "EmbeddedAttribute", "NullableAttribute", "NullableContextAttribute", "IsExternalInit", "R");
            CheckNames(reader0, reader0.GetMethodDefNames(),
                /* EmbeddedAttribute */".ctor",
                /* NullableAttribute */ ".ctor",
                /* NullableContextAttribute */".ctor",
                /* IsExternalInit */".ctor",
                /* R: */
                ".ctor",
                "get_EqualityContract",
                "get_X",
                "set_X",
                "ToString",
                "PrintMembers",
                "op_Inequality",
                "op_Equality",
                "GetHashCode",
                "Equals",
                "Equals",
                "<Clone>$",
                ".ctor",
                "Deconstruct");

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, printMembers0, printMembers1)));

            diff1.VerifySynthesizedMembers(
                "Microsoft.CodeAnalysis.EmbeddedAttribute",
                "System.Runtime.CompilerServices.NullableAttribute",
                "System.Runtime.CompilerServices.NullableContextAttribute");

            // Verify delta metadata contains expected rows.
            using var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;
            var readers = new[] { reader0, reader1 };

            EncValidation.VerifyModuleMvid(1, reader0, reader1);

            CheckNames(readers, reader1.GetTypeDefNames());
            CheckNames(readers, reader1.GetMethodDefNames(), "PrintMembers");

            CheckEncLog(reader1,
                Row(3, TableIndex.AssemblyRef, EditAndContinueOperation.Default),
                Row(21, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(22, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(23, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(4, TableIndex.TypeSpec, EditAndContinueOperation.Default),
                Row(3, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(10, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(3, TableIndex.Param, EditAndContinueOperation.Default),
                Row(23, TableIndex.CustomAttribute, EditAndContinueOperation.Default));

            CheckEncMap(reader1,
                Handle(21, TableIndex.TypeRef),
                Handle(22, TableIndex.TypeRef),
                Handle(23, TableIndex.TypeRef),
                Handle(10, TableIndex.MethodDef),
                Handle(3, TableIndex.Param),
                Handle(23, TableIndex.CustomAttribute),
                Handle(3, TableIndex.StandAloneSig),
                Handle(4, TableIndex.TypeSpec),
                Handle(3, TableIndex.AssemblyRef));
        }

        [Fact]
        public void Records_RemoveWellKnownMember()
        {
            var source0 =
@"
namespace N
{
    record R(int X)
    {
        protected virtual bool PrintMembers(System.Text.StringBuilder builder)
        {
            return true;
        }
    }
}
";
            var source1 =
@"
namespace N
{
    record R(int X)
    {
    }
}
";

            var compilation0 = CreateCompilation(new[] { source0, IsExternalInitTypeDefinition }, references: new[] { RefSafetyRulesAttributeLib }, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(new[] { source1, IsExternalInitTypeDefinition });

            var method0 = compilation0.GetMember<MethodSymbol>("N.R.PrintMembers");
            var method1 = compilation1.GetMember<MethodSymbol>("N.R.PrintMembers");

            var v0 = CompileAndVerify(compilation0, verify: Verification.Skipped);

            using var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, method0, method1)));

            diff1.VerifySynthesizedMembers(
                "Microsoft.CodeAnalysis.EmbeddedAttribute",
                "System.Runtime.CompilerServices.NullableAttribute",
                "System.Runtime.CompilerServices.NullableContextAttribute");
        }

        [Fact]
        public void Records_AddPrimaryConstructor()
        {
            using var _ = new EditAndContinueTest(options: TestOptions.DebugDll, targetFramework: TargetFramework.NetStandard20)
                .AddBaseline(
                    source: IsExternalInitTypeDefinition + "record R {}",
                    validator: g =>
                    {
                        g.VerifyMethodDefNames(
                            ".ctor",
                            ".ctor",
                            ".ctor",
                            ".ctor",
                            "get_EqualityContract",
                            "ToString",
                            "PrintMembers",
                            "op_Inequality",
                            "op_Equality",
                            "GetHashCode",
                            "Equals",
                            "Equals",
                            "<Clone>$",
                            ".ctor",
                            ".ctor");
                    })

                .AddGeneration(
                    source: IsExternalInitTypeDefinition + "record R(int P) {}",
                    edits: new[]
                    {
                        // The IDE actually also adds Update edits for synthesized methods (PrintMembers, Equals, GetHashCode).
                        // This test demonstrates that the compiler does not emit them automatically given just the constructor insert.
                        Edit(SemanticEditKind.Insert, c => c.GetPrimaryConstructor("R")),
                        Edit(SemanticEditKind.Delete, c => c.GetParameterlessConstructor("R"), c => c.GetMember("R"))
                    },
                    validator: g =>
                    {
                        g.VerifyMethodDefNames(
                            ".ctor", // updated parameterless ctor
                            ".ctor", // inserted primary ctor
                            "get_P",
                            "set_P",
                            "Deconstruct",
                            ".ctor"); // Exception

                        g.VerifyEncLogDefinitions(new[]
                        {
                            Row(2, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                            Row(7, TableIndex.TypeDef, EditAndContinueOperation.Default),
                            Row(5, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(3, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(7, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(4, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(7, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(5, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(15, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(5, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                            Row(16, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(5, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                            Row(17, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(5, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                            Row(18, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(5, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                            Row(19, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(7, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                            Row(20, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(1, TableIndex.PropertyMap, EditAndContinueOperation.AddProperty),
                            Row(2, TableIndex.Property, EditAndContinueOperation.Default),
                            Row(16, TableIndex.MethodDef, EditAndContinueOperation.AddParameter),
                            Row(9, TableIndex.Param, EditAndContinueOperation.Default),
                            Row(18, TableIndex.MethodDef, EditAndContinueOperation.AddParameter),
                            Row(10, TableIndex.Param, EditAndContinueOperation.Default),
                            Row(19, TableIndex.MethodDef, EditAndContinueOperation.AddParameter),
                            Row(11, TableIndex.Param, EditAndContinueOperation.Default),
                            Row(30, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(31, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(32, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(33, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(34, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(35, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(2, TableIndex.MethodSemantics, EditAndContinueOperation.Default),
                            Row(3, TableIndex.MethodSemantics, EditAndContinueOperation.Default)
                        });
                    })
                .Verify();
        }

        [Fact]
        public void Records_AddPrimaryConstructorParameter()
        {
            using var _ = new EditAndContinueTest(options: TestOptions.DebugDll, targetFramework: TargetFramework.NetLatest, verification: Verification.Skipped)
                .AddBaseline(
                    source: IsExternalInitTypeDefinition + "record R(int P, int U) {}",
                    validator: g =>
                    {
                        g.VerifyMethodDefNames(
                            ".ctor",
                            ".ctor",
                            ".ctor",
                            ".ctor",
                            ".ctor",
                            "get_EqualityContract",
                            "get_P",
                            "set_P",
                            "get_U",
                            "set_U",
                            "ToString",
                            "PrintMembers",
                            "op_Inequality",
                            "op_Equality",
                            "GetHashCode",
                            "Equals",
                            "Equals",
                            "<Clone>$",
                            ".ctor",
                            "Deconstruct");
                    })

                .AddGeneration(
                    source: IsExternalInitTypeDefinition + "record R(int P, int Q, int U) {}",
                    edits: new[]
                    {
                        // The IDE actually also adds Update edits for synthesized methods (PrintMembers, Equals, GetHashCode, copy-constructor) and 
                        // delete of the old primary constructor.
                        // This test demonstrates that the compiler does not emit them automatically given just the constructor insert.
                        // The synthesized auto-properties and Deconstruct method are emitted.
                        Edit(SemanticEditKind.Insert, c => c.GetPrimaryConstructor("R"))
                    },
                    validator: g =>
                    {
                        g.VerifyMethodDefNames(
                            ".ctor",
                            "get_Q",
                            "set_Q",
                            "Deconstruct");

                        g.VerifyEncLogDefinitions(new[]
                        {
                            Row(5, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(5, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(5, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                            Row(21, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(5, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                            Row(22, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(5, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                            Row(23, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(5, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                            Row(24, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(1, TableIndex.PropertyMap, EditAndContinueOperation.AddProperty),
                            Row(4, TableIndex.Property, EditAndContinueOperation.Default),
                            Row(21, TableIndex.MethodDef, EditAndContinueOperation.AddParameter),
                            Row(15, TableIndex.Param, EditAndContinueOperation.Default),
                            Row(21, TableIndex.MethodDef, EditAndContinueOperation.AddParameter),
                            Row(16, TableIndex.Param, EditAndContinueOperation.Default),
                            Row(21, TableIndex.MethodDef, EditAndContinueOperation.AddParameter),
                            Row(17, TableIndex.Param, EditAndContinueOperation.Default),
                            Row(23, TableIndex.MethodDef, EditAndContinueOperation.AddParameter),
                            Row(18, TableIndex.Param, EditAndContinueOperation.Default),
                            Row(24, TableIndex.MethodDef, EditAndContinueOperation.AddParameter),
                            Row(19, TableIndex.Param, EditAndContinueOperation.Default),
                            Row(24, TableIndex.MethodDef, EditAndContinueOperation.AddParameter),
                            Row(20, TableIndex.Param, EditAndContinueOperation.Default),
                            Row(24, TableIndex.MethodDef, EditAndContinueOperation.AddParameter),
                            Row(21, TableIndex.Param, EditAndContinueOperation.Default),
                            Row(39, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(40, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(41, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(42, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(43, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(6, TableIndex.MethodSemantics, EditAndContinueOperation.Default),
                            Row(7, TableIndex.MethodSemantics, EditAndContinueOperation.Default),
                        });

                        g.VerifyIL("""
                        .ctor
                        {
                          // Code size       29 (0x1d)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldarg.1
                          IL_0002:  stfld      0x04000003
                          IL_0007:  ldarg.0
                          IL_0008:  ldarg.2
                          IL_0009:  stfld      0x04000005
                          IL_000e:  ldarg.0
                          IL_000f:  ldarg.3
                          IL_0010:  stfld      0x04000004
                          IL_0015:  ldarg.0
                          IL_0016:  call       0x0A000017
                          IL_001b:  nop
                          IL_001c:  ret
                        }
                        get_Q
                        {
                          // Code size        7 (0x7)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldfld      0x04000005
                          IL_0006:  ret
                        }
                        set_Q
                        {
                          // Code size        8 (0x8)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldarg.1
                          IL_0002:  stfld      0x04000005
                          IL_0007:  ret
                        }
                        Deconstruct
                        {
                          // Code size       25 (0x19)
                          .maxstack  8
                          IL_0000:  ldarg.1
                          IL_0001:  ldarg.0
                          IL_0002:  call       0x06000007
                          IL_0007:  stind.i4
                          IL_0008:  ldarg.2
                          IL_0009:  ldarg.0
                          IL_000a:  call       0x06000016
                          IL_000f:  stind.i4
                          IL_0010:  ldarg.3
                          IL_0011:  ldarg.0
                          IL_0012:  call       0x06000009
                          IL_0017:  stind.i4
                          IL_0018:  ret
                        }
                        """);
                    })
                .Verify();
        }

        [Fact]
        public void Records_AddProperty_NonPrimary()
        {
            using var _ = new EditAndContinueTest(options: TestOptions.DebugDll, targetFramework: TargetFramework.NetStandard20, verification: Verification.Skipped)
                .AddBaseline(
                    source: IsExternalInitTypeDefinition + "record R(int P) {}",
                    validator: g =>
                    {
                        g.VerifyMethodDefNames(
                            ".ctor",
                            ".ctor",
                            ".ctor",
                            ".ctor",
                            ".ctor",
                            "get_EqualityContract",
                            "get_P",
                            "set_P",
                            "ToString",
                            "PrintMembers",
                            "op_Inequality",
                            "op_Equality",
                            "GetHashCode",
                            "Equals",
                            "Equals",
                            "<Clone>$",
                            ".ctor",
                            "Deconstruct");
                    })

                .AddGeneration(
                    source: IsExternalInitTypeDefinition + "record R(int P) { int Q { get; init; } }",
                    edits: new[]
                    {
                        // The IDE actually adds Update edits for synthesized methods (PrintMembers, Equals, GetHashCode, copy-constructor).
                        // This test demonstrates that the compiler does not emit them automatically given just the property insert.
                        Edit(SemanticEditKind.Insert, c => c.GetMember("R.Q")),
                    },
                    validator: g =>
                    {
                        g.VerifyMethodDefNames(
                            "get_Q",
                            "set_Q");

                        g.VerifyEncLogDefinitions(new[]
                        {
                            Row(5, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(4, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(5, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                            Row(19, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(5, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                            Row(20, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(1, TableIndex.PropertyMap, EditAndContinueOperation.AddProperty),
                            Row(3, TableIndex.Property, EditAndContinueOperation.Default),
                            Row(20, TableIndex.MethodDef, EditAndContinueOperation.AddParameter),
                            Row(12, TableIndex.Param, EditAndContinueOperation.Default),
                            Row(35, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(36, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(37, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(38, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(4, TableIndex.MethodSemantics, EditAndContinueOperation.Default),
                            Row(5, TableIndex.MethodSemantics, EditAndContinueOperation.Default)
                        });

                        g.VerifyIL("""
                        get_Q
                        {
                          // Code size        7 (0x7)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldfld      0x04000004
                          IL_0006:  ret
                        }
                        set_Q
                        {
                          // Code size        8 (0x8)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldarg.1
                          IL_0002:  stfld      0x04000004
                          IL_0007:  ret
                        }
                        """);
                    })
                .Verify();
        }

        [Fact]
        public void TopLevelStatement_Update()
        {
            var source0 = @"
using System;

Console.WriteLine(""Hello"");
";
            var source1 = @"
using System;

Console.WriteLine(""Hello World"");
";
            var compilation0 = CreateCompilation(source0, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), options: TestOptions.DebugExe);
            var compilation1 = compilation0.WithSource(source1);

            var method0 = compilation0.GetMember<MethodSymbol>("Program.<Main>$");
            var method1 = compilation1.GetMember<MethodSymbol>("Program.<Main>$");

            // Verify full metadata contains expected rows.
            var bytes0 = compilation0.EmitToArray();
            using var md0 = ModuleMetadata.CreateFromImage(bytes0);
            var reader0 = md0.MetadataReader;

            CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "Program");
            CheckNames(reader0, reader0.GetMethodDefNames(), "<Main>$", ".ctor");
            CheckNames(reader0, reader0.GetMemberRefNames(), /*CompilationRelaxationsAttribute.*/".ctor", /*RuntimeCompatibilityAttribute.*/".ctor", /*Object.*/".ctor", /*DebuggableAttribute*/".ctor", /*Console.*/"WriteLine", /*Program.*/".ctor");

            var generation0 = CreateInitialBaseline(compilation0,
                md0,
                EmptyLocalsProvider);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1)));

            // Verify delta metadata contains expected rows.
            using var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;
            var readers = new[] { reader0, reader1 };

            EncValidation.VerifyModuleMvid(1, reader0, reader1);

            CheckNames(readers, reader1.GetTypeDefNames());
            CheckNames(readers, reader1.GetMethodDefNames(), "<Main>$");
            CheckNames(readers, reader1.GetMemberRefNames(), /*CompilerGenerated*/".ctor", /*Console.*/"WriteLine");

            CheckEncLog(reader1,
                Row(2, TableIndex.AssemblyRef, EditAndContinueOperation.Default),
                Row(7, TableIndex.MemberRef, EditAndContinueOperation.Default),
                Row(8, TableIndex.MemberRef, EditAndContinueOperation.Default),
                Row(8, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(9, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(10, TableIndex.TypeRef, EditAndContinueOperation.Default),
                Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default), // Synthesized Main method
                Row(1, TableIndex.Param, EditAndContinueOperation.Default));

            CheckEncMap(reader1,
                Handle(8, TableIndex.TypeRef),
                Handle(9, TableIndex.TypeRef),
                Handle(10, TableIndex.TypeRef),
                Handle(1, TableIndex.MethodDef),
                Handle(1, TableIndex.Param),
                Handle(7, TableIndex.MemberRef),
                Handle(8, TableIndex.MemberRef),
                Handle(2, TableIndex.AssemblyRef));
        }

        [Fact]
        public void LambdaParameterToDiscard()
        {
            var source0 = MarkedSource(@"
using System;
class C
{
    void M()
    {
        var x = new Func<int, int, int>(<N:0>(a, b) => a + b + 1</N:0>);
        Console.WriteLine(x(1, 2));
    }
}");
            var source1 = MarkedSource(@"
using System;
class C
{
    void M()
    {
        var x = new Func<int, int, int>(<N:0>(_, _) => 10</N:0>);
        Console.WriteLine(x(1, 2));
    }
}");

            var compilation0 = CreateCompilation(source0.Tree, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);

            var method0 = compilation0.GetMember<MethodSymbol>("C.M");
            var method1 = compilation1.GetMember<MethodSymbol>("C.M");

            var v0 = CompileAndVerify(compilation0, verify: Verification.Skipped);

            using var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, method0, method1, GetSyntaxMapFromMarkers(source0, source1))));

            // There should be no diagnostics from rude edits
            diff.EmitResult.Diagnostics.Verify();

            diff.VerifySynthesizedMembers(
                "C: {<>c}",
                "C.<>c: {<>9__0_0, <M>b__0_0}");

            diff.VerifyIL("C.M",
                @"
 {
      // Code size       48 (0x30)
      .maxstack  3
      .locals init ([unchanged] V_0,
                    System.Func<int, int, int> V_1) //x
      IL_0000:  nop
      IL_0001:  ldsfld     ""System.Func<int, int, int> C.<>c.<>9__0_0""
      IL_0006:  dup
      IL_0007:  brtrue.s   IL_0020
      IL_0009:  pop
      IL_000a:  ldsfld     ""C.<>c C.<>c.<>9""
      IL_000f:  ldftn      ""int C.<>c.<M>b__0_0(int, int)""
      IL_0015:  newobj     ""System.Func<int, int, int>..ctor(object, System.IntPtr)""
      IL_001a:  dup
      IL_001b:  stsfld     ""System.Func<int, int, int> C.<>c.<>9__0_0""
      IL_0020:  stloc.1
      IL_0021:  ldloc.1
      IL_0022:  ldc.i4.1
      IL_0023:  ldc.i4.2
      IL_0024:  callvirt   ""int System.Func<int, int, int>.Invoke(int, int)""
      IL_0029:  call       ""void System.Console.WriteLine(int)""
      IL_002e:  nop
      IL_002f:  ret
}");

            diff.VerifyIL("C.<>c.<M>b__0_0(int, int)", @"
{
    // Code size        3 (0x3)
    .maxstack  1
    IL_0000:  ldc.i4.s   10
    IL_0002:  ret
}");
        }

        [Fact]
        public void Method_Delete_SynthesizedHotReloadException_MissingExceptionType()
        {
            using var _ = new EditAndContinueTest(targetFramework: TargetFramework.Minimal, verification: Verification.Skipped)
                .AddBaseline(
                    source: """
                        class C
                        {
                            void F() {}
                        }
                        """)
                .AddGeneration(
                    // 1
                    source: """
                        class C
                        {
                        }
                        """,
                    edits:
                    [
                        Edit(SemanticEditKind.Delete, symbolProvider: c => c.GetMember("C.F"), newSymbolProvider: c => c.GetMember("C")),
                    ],
                    expectedErrors:
                    [
                        // error CS7043: Cannot emit update; constructor 'System.Exception..ctor(string)' is missing.
                        Diagnostic(ErrorCode.ERR_EncUpdateFailedMissingSymbol).WithArguments("constructor", "System.Exception..ctor(string)").WithLocation(1, 1)
                    ])
                .Verify();
        }

        [Theory]
        [InlineData("")]
        [InlineData("public delegate void Action(System.Exception arg);")]
        [InlineData("public delegate void Action<S,T>(T arg);")]
        [InlineData("public delegate int Action<T>(T arg);")]
        [InlineData("public delegate void Action<T>(int arg);")]
        [InlineData("public delegate void Action<T>(in T arg);")]
        [InlineData("public delegate void Action<T>(ref T arg);")]
        [InlineData("public delegate void Action<T>(out T arg);")]
        [InlineData("public delegate void Action<T>(T arg, int x);")]
        public void Method_Delete_SynthesizedHotReloadException_MissingOrBadActionType(string actionDef)
        {
            var libs = $$"""
                namespace System
                {
                    public class Exception(string message);
                    {{actionDef}}
                }
                namespace System.Runtime.InteropServices
                {
                    public class InAttribute;
                }
                """;

            using var _ = new EditAndContinueTest(targetFramework: TargetFramework.Minimal, verification: Verification.Skipped)
                .AddBaseline(
                    source: """
                        class C
                        {
                            void F() {}
                        }
                        """ + libs)
                .AddGeneration(
                    // 1
                    source: """
                        class C
                        {
                        }
                        """ + libs,
                    edits:
                    [
                        Edit(SemanticEditKind.Delete, symbolProvider: c => c.GetMember("C.F"), newSymbolProvider: c => c.GetMember("C")),
                    ],
                    expectedErrors:
                    [
                        // error CS7043: Cannot emit update; method 'void System.Action<T>.Invoke(T arg)' is missing.
                        Diagnostic(ErrorCode.ERR_EncUpdateFailedMissingSymbol).WithArguments("method", "void System.Action<T>.Invoke(T arg)").WithLocation(1, 1)
                    ])
                .Verify();
        }

        [Fact]
        public void Method_Delete_SynthesizedHotReloadException_MissingCompilerGeneratedAttribute()
        {
            var libs = """
                namespace System
                {
                    public class Exception
                    {
                        public Exception(string message) {}
                    }

                    public delegate void Action<T>(T arg);
                }
                """;

            using var _ = new EditAndContinueTest(targetFramework: TargetFramework.Minimal, verification: Verification.Skipped)
                .AddBaseline(
                    source: libs + """
                        class C
                        {
                            void F() {}
                        }
                        """)
                .AddGeneration(
                    // 1
                    source: libs + """
                        class C
                        {
                        }
                        """,
                    edits:
                    [
                        Edit(SemanticEditKind.Delete, symbolProvider: c => c.GetMember("C.F"), newSymbolProvider: c => c.GetMember("C")),
                    ],
                    validator: g =>
                    {
                        g.VerifySynthesizedMembers("System.Runtime.CompilerServices.HotReloadException");
                        g.VerifyTypeDefNames("HotReloadException");

                        // Note TypeRef CompilerGeneratedAttribute not present:
                        g.VerifyTypeRefNames("Object");

                        g.VerifyIL("""
                            F
                            {
                              // Code size       13 (0xd)
                              .maxstack  8
                              IL_0000:  ldstr      0x70000005
                              IL_0005:  ldc.i4.s   -2
                              IL_0007:  newobj     0x06000008
                              IL_000c:  throw
                            }
                            .ctor
                            {
                              // Code size       33 (0x21)
                              .maxstack  2
                              IL_0000:  ldarg.0
                              IL_0001:  ldarg.1
                              IL_0002:  call       0x06000003
                              IL_0007:  nop
                              IL_0008:  ldarg.0
                              IL_0009:  ldarg.2
                              IL_000a:  stfld      0x04000001
                              IL_000f:  ldsfld     0x04000002
                              IL_0014:  dup
                              IL_0015:  stloc.0
                              IL_0016:  brfalse.s  IL_0020
                              IL_0018:  ldloc.0
                              IL_0019:  ldarg.0
                              IL_001a:  callvirt   0x0A000002
                              IL_001f:  nop
                              IL_0020:  ret
                            }
                            """);
                    })
                .Verify();
        }

        [Fact]
        public void Method_Delete_PredefinedHotReloadException()
        {
            var exceptionSource = """
                namespace System.Runtime.CompilerServices
                {
                    public class HotReloadException : Exception
                    {
                        public HotReloadException(string message, int code) : base(message) {}
                    }
                }
                """;

            using var _ = new EditAndContinueTest()
                .AddBaseline(
                    source: exceptionSource + """
                        class C
                        {
                            void F1() {}
                            void F2() {}
                        }
                        """)
                .AddGeneration(
                    // 1
                    source: exceptionSource + """
                        class C
                        {
                            void F2() {}
                        }
                        """,
                    edits:
                    [
                        Edit(SemanticEditKind.Delete, symbolProvider: c => c.GetMember("C.F1"), newSymbolProvider: c => c.GetMember("C")),
                    ],
                    validator: g =>
                    {
                        g.VerifySynthesizedMembers();
                        g.VerifyTypeDefNames();
                        g.VerifyTypeRefNames("Object");

                        g.VerifyIL("""
                            F1
                            {
                              // Code size       13 (0xd)
                              .maxstack  8
                              IL_0000:  ldstr      0x70000005
                              IL_0005:  ldc.i4.s   -2
                              IL_0007:  newobj     0x06000004
                              IL_000c:  throw
                            }
                            """);
                    })
                .AddGeneration(
                    // 2
                    source: exceptionSource + """
                        class C
                        {
                        }
                        """,
                    edits:
                    [
                        Edit(SemanticEditKind.Delete, symbolProvider: c => c.GetMember("C.F2"), newSymbolProvider: c => c.GetMember("C")),
                    ],
                    validator: g =>
                    {
                        g.VerifySynthesizedMembers();
                        g.VerifyTypeDefNames();
                        g.VerifyTypeRefNames("Object");

                        g.VerifyIL("""
                            F2
                            {
                              // Code size       13 (0xd)
                              .maxstack  8
                              IL_0000:  ldstr      0x70000151
                              IL_0005:  ldc.i4.s   -2
                              IL_0007:  newobj     0x06000004
                              IL_000c:  throw
                            }
                            """);
                    })
                .Verify();
        }

        [Fact]
        public void Method_Delete_PredefinedHotReloadException_Inserted()
        {
            var exceptionSource = """
                namespace System.Runtime.CompilerServices
                {
                    public class HotReloadException : Exception
                    {
                        public HotReloadException(string message, int code) : base(message) {}
                    }
                }
                """;

            using var _ = new EditAndContinueTest()
                .AddBaseline(
                    source: """
                        class C
                        {
                            void F1() {}
                            void F2() {}
                        }
                        """)
                .AddGeneration(
                    // 1
                    source: exceptionSource + """
                        class C
                        {
                            void F2() {}
                        }
                        """,
                    edits:
                    [
                        Edit(SemanticEditKind.Insert, symbolProvider: c => c.GetMember("System.Runtime.CompilerServices.HotReloadException")),
                        Edit(SemanticEditKind.Delete, symbolProvider: c => c.GetMember("C.F1"), newSymbolProvider: c => c.GetMember("C")),
                    ],
                    validator: g =>
                    {
                        g.VerifySynthesizedMembers();
                        g.VerifyTypeDefNames("HotReloadException");
                        g.VerifyTypeRefNames("Exception", "Object");

                        g.VerifyIL("""
                            F1
                            {
                              // Code size       13 (0xd)
                              .maxstack  8
                              IL_0000:  ldstr      0x70000005
                              IL_0005:  ldc.i4.s   -2
                              IL_0007:  newobj     0x06000004
                              IL_000c:  throw
                            }
                            .ctor
                            {
                              // Code size       10 (0xa)
                              .maxstack  8
                              IL_0000:  ldarg.0
                              IL_0001:  ldarg.1
                              IL_0002:  call       0x0A000005
                              IL_0007:  nop
                              IL_0008:  nop
                              IL_0009:  ret
                            }
                            """);
                    })
                .AddGeneration(
                    // 2
                    source: exceptionSource + """
                        class C
                        {
                        }
                        """,
                    edits:
                    [
                        Edit(SemanticEditKind.Delete, symbolProvider: c => c.GetMember("C.F2"), newSymbolProvider: c => c.GetMember("C")),
                    ],
                    validator: g =>
                    {
                        g.VerifySynthesizedMembers();
                        g.VerifyTypeDefNames();
                        g.VerifyTypeRefNames("Object");

                        g.VerifyIL("""
                            F2
                            {
                              // Code size       13 (0xd)
                              .maxstack  8
                              IL_0000:  ldstr      0x70000151
                              IL_0005:  ldc.i4.s   -2
                              IL_0007:  newobj     0x06000004
                              IL_000c:  throw
                            }
                            """);
                    })
                .Verify();
        }

        [Fact]
        public void Method_Delete_PredefinedHotReloadException_BadConstructor()
        {
            var exceptionSource = """
                namespace System.Runtime.CompilerServices
                {
                    public class HotReloadException : Exception
                    {
                        public HotReloadException(string message) : base(message) {}
                    }
                }
                """;

            using var _ = new EditAndContinueTest(assemblyName: "TestAssembly")
                .AddBaseline(
                    source: exceptionSource + """
                        class C
                        {
                            void F1() {}
                            void F2() {}
                        }
                        """)
                .AddGeneration(
                    // 1
                    source: exceptionSource + """
                        class C
                        {
                            void F2() {}
                        }
                        """,
                    edits:
                    [
                        Edit(SemanticEditKind.Delete, symbolProvider: c => c.GetMember("C.F1"), newSymbolProvider: c => c.GetMember("C")),
                    ],
                    expectedErrors:
                    [
                        // error CS7038: Failed to emit module 'TestAssembly': 'System.Runtime.CompilerServices.HotReloadException' type does not have the expected constructor
                        Diagnostic(ErrorCode.ERR_ModuleEmitFailure)
                            .WithArguments("TestAssembly", string.Format(CodeAnalysisResources.Type0DoesNotHaveExpectedConstructor, "System.Runtime.CompilerServices.HotReloadException"))
                    ])
                .Verify();
        }

        [Fact]
        public void Method_Delete_PredefinedHotReloadException_DataSectionLiterals()
        {
            var parseOptions = TestOptions.Regular.WithFeature("experimental-data-section-string-literals", "0");

            var exceptionSource = """
                namespace System.Runtime.CompilerServices
                {
                    public class HotReloadException : Exception
                    {
                        public HotReloadException(string message, int code) : base(message) {}
                    }
                }
                """;

            using var _ = new EditAndContinueTest(targetFramework: TargetFramework.Net90, verification: Verification.FailsPEVerify, parseOptions: parseOptions)
                .AddBaseline(
                    source: exceptionSource + """
                        class C
                        {
                            void F1() {}
                            void F2() {}
                        }
                        """)
                .AddGeneration(
                    // 1
                    source: exceptionSource + """
                        class C
                        {
                            void F2() {}
                        }
                        """,
                    edits:
                    [
                        Edit(SemanticEditKind.Delete, symbolProvider: c => c.GetMember("C.F1"), newSymbolProvider: c => c.GetMember("C")),
                    ],
                    validator: g =>
                    {
                        g.VerifySynthesizedMembers();
                        g.VerifyTypeDefNames("<PrivateImplementationDetails>#1", "__StaticArrayInitTypeSize=163", "<S>A70F5C822D3106BF474269B4991AB592");
                        g.VerifyTypeRefNames("Object", "CompilerGeneratedAttribute", "ValueType", "Encoding");

                        g.VerifyIL("""
                        F1
                        {
                            // Code size       13 (0xd)
                            .maxstack  8
                            IL_0000:  ldsfld     0x04000002
                            IL_0005:  ldc.i4.s   -2
                            IL_0007:  newobj     0x06000004
                            IL_000c:  throw
                        }
                        BytesToString
                        {
                            // Code size       13 (0xd)
                            .maxstack  8
                            IL_0000:  call       0x0A000008
                            IL_0005:  ldarg.0
                            IL_0006:  ldarg.1
                            IL_0007:  callvirt   0x0A000009
                            IL_000c:  ret
                        }
                        .cctor
                        {
                            // Code size       21 (0x15)
                            .maxstack  8
                            IL_0000:  ldsflda    0x04000001
                            IL_0005:  ldc.i4     0xa3
                            IL_000a:  call       0x06000005
                            IL_000f:  stsfld     0x04000002
                            IL_0014:  ret
                        }
                        """);
                    },
                    options: new EmitDifferenceOptions() { EmitFieldRva = true })
                .Verify();
        }

        [Theory]
        [InlineData("int M1(C c) { return 0; }")]
        [InlineData("C M1(C c) { return default; }")]
        [InlineData("C M1() { return default; }")]
        [InlineData("N M1() { return default; }")]
        [InlineData("T M1<T>() { return default; }")]
        [InlineData("T M1<T>() where T : C { return default; }")]
        public void Method_Delete_ReturnValue(string methodDef)
        {
            using var _ = new EditAndContinueTest()
                .AddBaseline(
                    source: $$"""
                        class C
                        {
                            {{methodDef}}
                        }

                        class N
                        {
                        }
                        """,
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("<Module>", "C", "N");
                        g.VerifyMethodDefNames("M1", ".ctor", ".ctor");
                    })

                .AddGeneration(// 1
                    source: """
                        class C
                        {
                        }

                        class N
                        {
                        }
                        """,
                    edits: new[] {
                        Edit(SemanticEditKind.Delete, symbolProvider: c => c.GetMember("C.M1"), newSymbolProvider: c => c.GetMember("C")),
                    },
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("HotReloadException");
                        g.VerifyMethodDefNames("M1", ".ctor");
                        g.VerifyMemberRefNames(".ctor", ".ctor", "Invoke");

                        g.VerifyEncLogDefinitions(
                        [
                            Row(2, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                            Row(4, TableIndex.TypeDef, EditAndContinueOperation.Default),
                            Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(1, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(2, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                            Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(4, TableIndex.CustomAttribute, EditAndContinueOperation.Default)
                        ]);

                        g.VerifyEncMapDefinitions(
                        [
                            Handle(4, TableIndex.TypeDef),
                            Handle(1, TableIndex.Field),
                            Handle(2, TableIndex.Field),
                            Handle(1, TableIndex.MethodDef),
                            Handle(4, TableIndex.MethodDef),
                            Handle(4, TableIndex.CustomAttribute),
                            Handle(2, TableIndex.StandAloneSig)
                        ]);

                        g.VerifyIL("""
                            M1
                            {
                              // Code size       13 (0xd)
                              .maxstack  8
                              IL_0000:  ldstr      0x70000005
                              IL_0005:  ldc.i4.s   -2
                              IL_0007:  newobj     0x06000004
                              IL_000c:  throw
                            }
                            .ctor
                            {
                              // Code size       33 (0x21)
                              .maxstack  2
                              IL_0000:  ldarg.0
                              IL_0001:  ldarg.1
                              IL_0002:  call       0x0A000006
                              IL_0007:  nop
                              IL_0008:  ldarg.0
                              IL_0009:  ldarg.2
                              IL_000a:  stfld      0x04000001
                              IL_000f:  ldsfld     0x04000002
                              IL_0014:  dup
                              IL_0015:  stloc.0
                              IL_0016:  brfalse.s  IL_0020
                              IL_0018:  ldloc.0
                              IL_0019:  ldarg.0
                              IL_001a:  callvirt   0x0A000007
                              IL_001f:  nop
                              IL_0020:  ret
                            }
                            """);
                    })
                .Verify();
        }

        [Theory]
        [InlineData("void M1() { }")]
        [InlineData("void M1(string s) { }")]
        [InlineData("void M1(C c) { }")]
        [InlineData("void M1(N n) { }")]
        [InlineData("void M1<T>(T t) { }")]
        [InlineData("void M1<T>(T t) where T : C { }")]
        public void Method_Delete_Void(string methodDef)
        {
            using var _ = new EditAndContinueTest()
                .AddBaseline(
                    source: $$"""
                        class C
                        {
                            {{methodDef}}
                        }

                        class N
                        {
                        }
                        """,
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("<Module>", "C", "N");
                        g.VerifyMethodDefNames("M1", ".ctor", ".ctor");
                    })

                .AddGeneration(// 1
                    source: """
                        class C
                        {
                        }

                        class N
                        {
                        }
                        """,
                    edits: new[] {
                        Edit(SemanticEditKind.Delete, symbolProvider: c => c.GetMember("C.M1"), newSymbolProvider: c => c.GetMember("C")),
                    },
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("HotReloadException");
                        g.VerifyMethodDefNames("M1", ".ctor");
                        g.VerifyMemberRefNames(".ctor", ".ctor", "Invoke");

                        g.VerifyEncLogDefinitions(
                        [
                            Row(1, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                            Row(4, TableIndex.TypeDef, EditAndContinueOperation.Default),
                            Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(1, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(2, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                            Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(4, TableIndex.CustomAttribute, EditAndContinueOperation.Default)
                        ]);

                        g.VerifyEncMapDefinitions(
                        [
                            Handle(4, TableIndex.TypeDef),
                            Handle(1, TableIndex.Field),
                            Handle(2, TableIndex.Field),
                            Handle(1, TableIndex.MethodDef),
                            Handle(4, TableIndex.MethodDef),
                            Handle(4, TableIndex.CustomAttribute),
                            Handle(1, TableIndex.StandAloneSig)
                        ]);

                        g.VerifyIL("""
                            M1
                            {
                              // Code size       13 (0xd)
                              .maxstack  8
                              IL_0000:  ldstr      0x70000005
                              IL_0005:  ldc.i4.s   -2
                              IL_0007:  newobj     0x06000004
                              IL_000c:  throw
                            }
                            .ctor
                            {
                              // Code size       33 (0x21)
                              .maxstack  2
                              IL_0000:  ldarg.0
                              IL_0001:  ldarg.1
                              IL_0002:  call       0x0A000006
                              IL_0007:  nop
                              IL_0008:  ldarg.0
                              IL_0009:  ldarg.2
                              IL_000a:  stfld      0x04000001
                              IL_000f:  ldsfld     0x04000002
                              IL_0014:  dup
                              IL_0015:  stloc.0
                              IL_0016:  brfalse.s  IL_0020
                              IL_0018:  ldloc.0
                              IL_0019:  ldarg.0
                              IL_001a:  callvirt   0x0A000007
                              IL_001f:  nop
                              IL_0020:  ret
                            }
                            """);
                    })
                .Verify();
        }

        [Fact]
        public void Method_AddThenDelete()
        {
            using var _ = new EditAndContinueTest()
                .AddBaseline(
                    source: $$"""
                        class C
                        {
                            void M1() { }
                        }
                        """,
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("<Module>", "C");
                        g.VerifyMethodDefNames("M1", ".ctor");
                        g.VerifyMemberRefNames(/*CompilationRelaxationsAttribute.*/".ctor", /*RuntimeCompatibilityAttribute.*/".ctor", /*Object.*/".ctor", /*DebuggableAttribute*/".ctor");
                    })

                .AddGeneration(
                    // 1
                    source: """
                        class C
                        {
                            void M1() { }
                            void M2() { }
                        }
                        """,
                    edits: new[] {
                        Edit(SemanticEditKind.Insert, symbolProvider: c => c.GetMember("C.M2")),
                    },
                    validator: g =>
                    {
                        g.VerifyTypeDefNames();
                        g.VerifyMethodDefNames("M2");
                        g.VerifyEncLogDefinitions(new[]
                        {
                            Row(2, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                            Row(3, TableIndex.MethodDef, EditAndContinueOperation.Default)
                        });
                        g.VerifyEncMapDefinitions(new[]
                        {
                            Handle(3, TableIndex.MethodDef),
                        });

                        g.VerifyIL("""
                            M2
                            {
                              // Code size        2 (0x2)
                              .maxstack  8
                              IL_0000:  nop
                              IL_0001:  ret
                            }
                            """);
                    })

                .AddGeneration(
                    // 2
                    source: """
                        class C
                        {
                            void M1() { }
                        }
                        """,
                    edits: new[] {
                        Edit(SemanticEditKind.Delete, symbolProvider: c => c.GetMember("C.M2"), newSymbolProvider: c => c.GetMember("C")),
                    },
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("HotReloadException");
                        g.VerifyMethodDefNames("M2", ".ctor");
                        g.VerifyEncLogDefinitions(new[]
                        {
                            Row(1, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                            Row(3, TableIndex.TypeDef, EditAndContinueOperation.Default),
                            Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(1, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(2, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(3, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                            Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(4, TableIndex.CustomAttribute, EditAndContinueOperation.Default)
                        });

                        g.VerifyEncMapDefinitions(new[]
                        {
                            Handle(3, TableIndex.TypeDef),
                            Handle(1, TableIndex.Field),
                            Handle(2, TableIndex.Field),
                            Handle(3, TableIndex.MethodDef),
                            Handle(4, TableIndex.MethodDef),
                            Handle(4, TableIndex.CustomAttribute),
                            Handle(1, TableIndex.StandAloneSig)
                        });

                        g.VerifyIL("""
                            M2
                            {
                              // Code size       13 (0xd)
                              .maxstack  8
                              IL_0000:  ldstr      0x70000009
                              IL_0005:  ldc.i4.s   -2
                              IL_0007:  newobj     0x06000004
                              IL_000c:  throw
                            }
                            .ctor
                            {
                              // Code size       33 (0x21)
                              .maxstack  2
                              IL_0000:  ldarg.0
                              IL_0001:  ldarg.1
                              IL_0002:  call       0x0A000006
                              IL_0007:  nop
                              IL_0008:  ldarg.0
                              IL_0009:  ldarg.2
                              IL_000a:  stfld      0x04000001
                              IL_000f:  ldsfld     0x04000002
                              IL_0014:  dup
                              IL_0015:  stloc.0
                              IL_0016:  brfalse.s  IL_0020
                              IL_0018:  ldloc.0
                              IL_0019:  ldarg.0
                              IL_001a:  callvirt   0x0A000007
                              IL_001f:  nop
                              IL_0020:  ret
                            }
                            """);
                    })
                .Verify();
        }

        [Fact]
        public void Method_DeleteThenAdd()
        {
            using var _ = new EditAndContinueTest()
                .AddBaseline(
                    source: $$"""
                        class C
                        {
                            void M1() { }
                        }
                        """,
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("<Module>", "C");
                        g.VerifyMethodDefNames("M1", ".ctor");
                        g.VerifyMemberRefNames(/*CompilationRelaxationsAttribute.*/".ctor", /*RuntimeCompatibilityAttribute.*/".ctor", /*Object.*/".ctor", /*DebuggableAttribute*/".ctor");
                    })

                .AddGeneration(
                    source: """
                        class C
                        {
                        }
                        """,
                    edits: new[] {
                        Edit(SemanticEditKind.Delete, symbolProvider: c => c.GetMember("C.M1"), newSymbolProvider: c => c.GetMember("C")),
                    },
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("HotReloadException");
                        g.VerifyMethodDefNames("M1", ".ctor");
                        g.VerifyEncLogDefinitions(new[]
                        {
                            Row(1, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                            Row(3, TableIndex.TypeDef, EditAndContinueOperation.Default),
                            Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(1, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(2, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                            Row(3, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(4, TableIndex.CustomAttribute, EditAndContinueOperation.Default)
                        });
                        g.VerifyEncMapDefinitions(new[]
                        {
                            Handle(3, TableIndex.TypeDef),
                            Handle(1, TableIndex.Field),
                            Handle(2, TableIndex.Field),
                            Handle(1, TableIndex.MethodDef),
                            Handle(3, TableIndex.MethodDef),
                            Handle(4, TableIndex.CustomAttribute),
                            Handle(1, TableIndex.StandAloneSig)
                        });

                        g.VerifyIL("""
                            M1
                            {
                              // Code size       13 (0xd)
                              .maxstack  8
                              IL_0000:  ldstr      0x70000005
                              IL_0005:  ldc.i4.s   -2
                              IL_0007:  newobj     0x06000003
                              IL_000c:  throw
                            }
                            .ctor
                            {
                              // Code size       33 (0x21)
                              .maxstack  2
                              IL_0000:  ldarg.0
                              IL_0001:  ldarg.1
                              IL_0002:  call       0x0A000006
                              IL_0007:  nop
                              IL_0008:  ldarg.0
                              IL_0009:  ldarg.2
                              IL_000a:  stfld      0x04000001
                              IL_000f:  ldsfld     0x04000002
                              IL_0014:  dup
                              IL_0015:  stloc.0
                              IL_0016:  brfalse.s  IL_0020
                              IL_0018:  ldloc.0
                              IL_0019:  ldarg.0
                              IL_001a:  callvirt   0x0A000007
                              IL_001f:  nop
                              IL_0020:  ret
                            }
                            """);
                    })

                .AddGeneration(
                    source: """
                        class C
                        {
                            void M1() { System.Console.Write(1); }
                        }
                        """,
                    edits: new[] {
                        Edit(SemanticEditKind.Insert, symbolProvider: c => c.GetMember("C.M1")),
                    },
                    validator: g =>
                    {
                        g.VerifyTypeDefNames();
                        g.VerifyMethodDefNames("M1");
                        g.VerifyEncLogDefinitions(new[]
                        {
                            Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default)
                        });
                        g.VerifyEncMapDefinitions(new[]
                        {
                            Handle(1, TableIndex.MethodDef),
                        });

                        g.VerifyIL("""
                            M1
                            {
                              // Code size        9 (0x9)
                              .maxstack  8
                              IL_0000:  nop
                              IL_0001:  ldc.i4.1
                              IL_0002:  call       0x0A000008
                              IL_0007:  nop
                              IL_0008:  ret
                            }
                            """);
                    })
                .Verify();
        }

        [Fact]
        public void Method_DeleteThenAdd_WithAttributes()
        {
            using var _ = new EditAndContinueTest()
                .AddBaseline(
                    source: MetadataUpdateDeletedAttributeSource + """
                        class A : System.Attribute { }
                        class B : System.Attribute { }

                        class C
                        {
                            [A]
                            [return: A]
                            void M1([A]int x) { }
                        }
                        """,
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("<Module>", "A", "B", "C", "MetadataUpdateDeletedAttribute");
                        g.VerifyMethodDefNames(".ctor", ".ctor", "M1", ".ctor", ".ctor");
                        g.VerifyMemberRefNames(/*CompilationRelaxationsAttribute.*/".ctor", /*RuntimeCompatibilityAttribute.*/".ctor", /*Object.*/".ctor", /*DebuggableAttribute*/".ctor", ".ctor", ".ctor");
                    })
                .AddGeneration(
                    // 1
                    source: MetadataUpdateDeletedAttributeSource + """
                        class A : System.Attribute { }
                        class B : System.Attribute { }
                        
                        class C
                        {
                        }
                        """,
                    edits: new[] {
                        Edit(SemanticEditKind.Delete, symbolProvider: c => c.GetMember("C.M1"), newSymbolProvider: c => c.GetMember("C")),
                    },
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("HotReloadException");
                        g.VerifyMethodDefNames("M1", ".ctor");
                        g.VerifyEncLogDefinitions(
                        [
                            Row(1, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                            Row(6, TableIndex.TypeDef, EditAndContinueOperation.Default),
                            Row(6, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(1, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(6, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(2, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(3, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(6, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                            Row(6, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(6, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(8, TableIndex.CustomAttribute, EditAndContinueOperation.Default)
                        ]);

                        g.VerifyEncMapDefinitions(
                        [
                            Handle(6, TableIndex.TypeDef),
                            Handle(1, TableIndex.Field),
                            Handle(2, TableIndex.Field),
                            Handle(3, TableIndex.MethodDef),
                            Handle(6, TableIndex.MethodDef),
                            Handle(6, TableIndex.CustomAttribute),
                            Handle(8, TableIndex.CustomAttribute),
                            Handle(1, TableIndex.StandAloneSig)
                        ]);

                        g.VerifyCustomAttributes(
                        [
                            new CustomAttributeRow(Handle(3, TableIndex.MethodDef), Handle(5, TableIndex.MethodDef)),
                            new CustomAttributeRow(Handle(6, TableIndex.TypeDef), Handle(7, TableIndex.MemberRef))
                        ]);

                        g.VerifyIL("""
                            M1
                            {
                              // Code size       13 (0xd)
                              .maxstack  8
                              IL_0000:  ldstr      0x70000005
                              IL_0005:  ldc.i4.s   -2
                              IL_0007:  newobj     0x06000006
                              IL_000c:  throw
                            }
                            .ctor
                            {
                              // Code size       33 (0x21)
                              .maxstack  2
                              IL_0000:  ldarg.0
                              IL_0001:  ldarg.1
                              IL_0002:  call       0x0A000008
                              IL_0007:  nop
                              IL_0008:  ldarg.0
                              IL_0009:  ldarg.2
                              IL_000a:  stfld      0x04000001
                              IL_000f:  ldsfld     0x04000002
                              IL_0014:  dup
                              IL_0015:  stloc.0
                              IL_0016:  brfalse.s  IL_0020
                              IL_0018:  ldloc.0
                              IL_0019:  ldarg.0
                              IL_001a:  callvirt   0x0A000009
                              IL_001f:  nop
                              IL_0020:  ret
                            }
                            """);
                    })
                .AddGeneration(
                    // 2
                    source: MetadataUpdateDeletedAttributeSource + """
                        class A : System.Attribute { }
                        class B : System.Attribute { }
                        
                        class C
                        {
                            [B]
                            [return: B]
                            void M1([B]int x) { }
                        
                        }
                        """,
                    edits: new[] {
                        Edit(SemanticEditKind.Insert, symbolProvider: c => c.GetMember("C.M1")),
                    },
                    validator: g =>
                    {
                        g.VerifyTypeDefNames();
                        g.VerifyMethodDefNames("M1");
                        g.VerifyEncLogDefinitions(new[]
                        {
                            Row(3, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(1, TableIndex.Param, EditAndContinueOperation.Default),
                            Row(2, TableIndex.Param, EditAndContinueOperation.Default),
                            Row(1, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(5, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(6, TableIndex.CustomAttribute, EditAndContinueOperation.Default)
                        });
                        g.VerifyEncMapDefinitions(new[]
                        {
                            Handle(3, TableIndex.MethodDef),
                            Handle(1, TableIndex.Param),
                            Handle(2, TableIndex.Param),
                            Handle(1, TableIndex.CustomAttribute),
                            Handle(5, TableIndex.CustomAttribute),
                            Handle(6, TableIndex.CustomAttribute)
                        });
                        g.VerifyCustomAttributes(new[]
                        {
                            new CustomAttributeRow(Handle(1, TableIndex.Param), Handle(2, TableIndex.MethodDef)),
                            new CustomAttributeRow(Handle(2, TableIndex.Param), Handle(2, TableIndex.MethodDef)),
                            new CustomAttributeRow(Handle(3, TableIndex.MethodDef), Handle(2, TableIndex.MethodDef))
                        });

                        g.VerifyIL("""
                            M1
                            {
                              // Code size        2 (0x2)
                              .maxstack  8
                              IL_0000:  nop
                              IL_0001:  ret
                            }
                            """);
                    })
                .Verify();
        }

        [Fact]
        public void Method_AddThenDeleteThenAdd()
        {
            using var _ = new EditAndContinueTest()
                .AddBaseline(
                    source: MetadataUpdateDeletedAttributeSource + """
                        class C
                        {
                            void Goo() { }
                        }
                        """,
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("<Module>", "C", "MetadataUpdateDeletedAttribute");
                        g.VerifyMethodDefNames("Goo", ".ctor", ".ctor");
                        g.VerifyMemberRefNames(/*CompilationRelaxationsAttribute.*/".ctor", /*RuntimeCompatibilityAttribute.*/".ctor", /*Object.*/".ctor", /*DebuggableAttribute*/".ctor", ".ctor", ".ctor");
                    })

                .AddGeneration(
                    // 1
                    source: MetadataUpdateDeletedAttributeSource + """
                        class C
                        {
                            void Goo() { }
                            C M1(C c) { return default; }
                        }
                        """,
                    edits: new[] {
                        Edit(SemanticEditKind.Insert, symbolProvider: c => c.GetMember("C.M1")),
                    },
                    validator: g =>
                    {
                        g.VerifyTypeDefNames();
                        g.VerifyMethodDefNames("M1");

                        g.VerifyEncLogDefinitions(new[]
                        {
                            Row(1, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                            Row(2, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                            Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(4, TableIndex.MethodDef, EditAndContinueOperation.AddParameter),
                            Row(1, TableIndex.Param, EditAndContinueOperation.Default)
                        });

                        g.VerifyEncMapDefinitions(new[]
                        {
                            Handle(4, TableIndex.MethodDef),
                            Handle(1, TableIndex.Param),
                            Handle(1, TableIndex.StandAloneSig)
                        });

                        g.VerifyIL("""
                            M1
                            {
                              // Code size        7 (0x7)
                              .maxstack  1
                              IL_0000:  nop
                              IL_0001:  ldnull
                              IL_0002:  stloc.0
                              IL_0003:  br.s       IL_0005
                              IL_0005:  ldloc.0
                              IL_0006:  ret
                            }
                            """);
                    })

                .AddGeneration(
                    // 2
                    source: MetadataUpdateDeletedAttributeSource + """
                        class C
                        {
                            void Goo() { }
                        }
                        """,
                    edits: new[] {
                        Edit(SemanticEditKind.Delete, symbolProvider: c => c.GetMember("C.M1"), newSymbolProvider: c => c.GetMember("C")),
                    },
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("HotReloadException");
                        g.VerifyMethodDefNames("M1", ".ctor");

                        g.VerifyEncLogDefinitions(new[]
                        {
                            Row(2, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                            Row(4, TableIndex.TypeDef, EditAndContinueOperation.Default),
                            Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(1, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(2, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                            Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(5, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(6, TableIndex.CustomAttribute, EditAndContinueOperation.Default)
                        });

                        g.VerifyEncMapDefinitions(new[]
                        {
                            Handle(4, TableIndex.TypeDef),
                            Handle(1, TableIndex.Field),
                            Handle(2, TableIndex.Field),
                            Handle(4, TableIndex.MethodDef),
                            Handle(5, TableIndex.MethodDef),
                            Handle(5, TableIndex.CustomAttribute),
                            Handle(6, TableIndex.CustomAttribute),
                            Handle(2, TableIndex.StandAloneSig)
                        });

                        g.VerifyIL("""
                            M1
                            {
                              // Code size       13 (0xd)
                              .maxstack  8
                              IL_0000:  ldstr      0x70000009
                              IL_0005:  ldc.i4.s   -2
                              IL_0007:  newobj     0x06000005
                              IL_000c:  throw
                            }
                            .ctor
                            {
                              // Code size       33 (0x21)
                              .maxstack  2
                              IL_0000:  ldarg.0
                              IL_0001:  ldarg.1
                              IL_0002:  call       0x0A000008
                              IL_0007:  nop
                              IL_0008:  ldarg.0
                              IL_0009:  ldarg.2
                              IL_000a:  stfld      0x04000001
                              IL_000f:  ldsfld     0x04000002
                              IL_0014:  dup
                              IL_0015:  stloc.0
                              IL_0016:  brfalse.s  IL_0020
                              IL_0018:  ldloc.0
                              IL_0019:  ldarg.0
                              IL_001a:  callvirt   0x0A000009
                              IL_001f:  nop
                              IL_0020:  ret
                            }
                            """);

                    })

                .AddGeneration(
                    // 3
                    source: MetadataUpdateDeletedAttributeSource + """
                        class C
                        {
                            void Goo() { }
                            C M1(C b) { System.Console.Write(1); return default; }
                        }
                        """,
                    edits: new[] {
                        Edit(SemanticEditKind.Insert, symbolProvider: c => c.GetMember("C.M1")),
                    },
                    validator: g =>
                    {
                        g.VerifyTypeDefNames();
                        g.VerifyMethodDefNames("M1");

                        g.VerifyEncLogDefinitions(new[]
                        {
                            Row(3, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                            Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(1, TableIndex.Param, EditAndContinueOperation.Default),
                            Row(6, TableIndex.CustomAttribute, EditAndContinueOperation.Default)
                        });

                        g.VerifyEncMapDefinitions(new[]
                        {
                            Handle(4, TableIndex.MethodDef),
                            Handle(1, TableIndex.Param),
                            Handle(6, TableIndex.CustomAttribute),
                            Handle(3, TableIndex.StandAloneSig)
                        });

                        g.VerifyIL("""
                            M1
                            {
                              // Code size       14 (0xe)
                              .maxstack  1
                              IL_0000:  nop
                              IL_0001:  ldc.i4.1
                              IL_0002:  call       0x0A00000A
                              IL_0007:  nop
                              IL_0008:  ldnull
                              IL_0009:  stloc.0
                              IL_000a:  br.s       IL_000c
                              IL_000c:  ldloc.0
                              IL_000d:  ret
                            }
                            """);
                    })
                .Verify();
        }

        [Fact]
        public void Method_Delete_WithLambda()
        {
            using var _ = new EditAndContinueTest()
                .AddBaseline(
                    source: """
                        using System;

                        class C
                        {
                            void F() { _ = new Action(() => Console.WriteLine(1)); } 
                        }
                        """,
                    validator: g =>
                    {
                        g.VerifySynthesizedMembers(
                            "C: {<>c}",
                            "C.<>c: {<>9__0_0, <F>b__0_0}");
                    })
                .AddGeneration(
                    // 1
                    source: """
                        using System;
                        
                        class C
                        {
                        }
                        """,
                    edits:
                    [
                        Edit(SemanticEditKind.Delete, symbolProvider: c => c.GetMember("C.F"), newSymbolProvider: c => c.GetMember("C")),
                    ],
                    validator: g =>
                    {
                        g.VerifySynthesizedMembers("System.Runtime.CompilerServices.HotReloadException");

                        g.VerifyTypeDefNames("HotReloadException");
                        g.VerifyMethodDefNames("F", "<F>b__0_0", ".ctor");
                        g.VerifyTypeRefNames("Object", "CompilerGeneratedAttribute", "Exception", "Action`1");
                        g.VerifyMemberRefNames(".ctor", ".ctor", "Invoke");

                        g.VerifyEncLogDefinitions(new[]
                        {
                            Row(1, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                            Row(4, TableIndex.TypeDef, EditAndContinueOperation.Default),
                            Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(3, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(4, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                            Row(6, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(5, TableIndex.CustomAttribute, EditAndContinueOperation.Default)
                        });

                        g.VerifyEncMapDefinitions(new[]
                        {
                            Handle(4, TableIndex.TypeDef),
                            Handle(3, TableIndex.Field),
                            Handle(4, TableIndex.Field),
                            Handle(1, TableIndex.MethodDef),
                            Handle(5, TableIndex.MethodDef),
                            Handle(6, TableIndex.MethodDef),
                            Handle(5, TableIndex.CustomAttribute),
                            Handle(1, TableIndex.StandAloneSig)
                        });

                        g.VerifyIL("""
                            F
                            {
                              // Code size       13 (0xd)
                              .maxstack  8
                              IL_0000:  ldstr      0x70000005
                              IL_0005:  ldc.i4.s   -2
                              IL_0007:  newobj     0x06000006
                              IL_000c:  throw
                            }
                            <F>b__0_0
                            {
                              // Code size       12 (0xc)
                              .maxstack  8
                              IL_0000:  ldstr      0x7000014E
                              IL_0005:  ldc.i4.m1
                              IL_0006:  newobj     0x06000006
                              IL_000b:  throw
                            }
                            .ctor
                            {
                              // Code size       33 (0x21)
                              .maxstack  2
                              IL_0000:  ldarg.0
                              IL_0001:  ldarg.1
                              IL_0002:  call       0x0A000009
                              IL_0007:  nop
                              IL_0008:  ldarg.0
                              IL_0009:  ldarg.2
                              IL_000a:  stfld      0x04000003
                              IL_000f:  ldsfld     0x04000004
                              IL_0014:  dup
                              IL_0015:  stloc.0
                              IL_0016:  brfalse.s  IL_0020
                              IL_0018:  ldloc.0
                              IL_0019:  ldarg.0
                              IL_001a:  callvirt   0x0A00000A
                              IL_001f:  nop
                              IL_0020:  ret
                            }
                            """);
                    })
                .AddGeneration(
                    // 2
                    source: """
                        using System;
                        
                        class C
                        {
                            void F() { _ = new Action(() => Console.WriteLine(2)); } 
                        }
                        """,
                    edits: new[]
                    {
                        Edit(SemanticEditKind.Insert, symbolProvider: c => c.GetMember("C.F")),
                    },
                    validator: g =>
                    {
                        g.VerifySynthesizedMembers(
                            "System.Runtime.CompilerServices.HotReloadException",
                            "C: {<>c}",
                            "C.<>c: {<>9__0#2_0#2, <F>b__0#2_0#2}");

                        g.VerifyTypeDefNames();
                        g.VerifyMethodDefNames("F", ".ctor", "<F>b__0#2_0#2");
                        g.VerifyTypeRefNames("Object", "CompilerGeneratedAttribute", "Action", "Console");

                        g.VerifyEncLogDefinitions(new[]
                        {
                            Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(5, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                            Row(7, TableIndex.MethodDef, EditAndContinueOperation.Default)
                        });

                        g.VerifyEncMapDefinitions(new[]
                        {
                            Handle(5, TableIndex.Field),
                            Handle(1, TableIndex.MethodDef),
                            Handle(4, TableIndex.MethodDef),
                            Handle(7, TableIndex.MethodDef)
                        });

                        g.VerifyIL("""
                            F
                            {
                              // Code size       30 (0x1e)
                              .maxstack  8
                              IL_0000:  nop
                              IL_0001:  ldsfld     0x04000005
                              IL_0006:  brtrue.s   IL_001d
                              IL_0008:  ldsfld     0x04000001
                              IL_000d:  ldftn      0x06000007
                              IL_0013:  newobj     0x0A00000C
                              IL_0018:  stsfld     0x04000005
                              IL_001d:  ret
                            }
                            .ctor
                            {
                              // Code size        8 (0x8)
                              .maxstack  8
                              IL_0000:  ldarg.0
                              IL_0001:  call       0x0A00000D
                              IL_0006:  nop
                              IL_0007:  ret
                            }
                            <F>b__0#2_0#2
                            {
                              // Code size        8 (0x8)
                              .maxstack  8
                              IL_0000:  ldc.i4.2
                              IL_0001:  call       0x0A00000E
                              IL_0006:  nop
                              IL_0007:  ret
                            }
                            """);
                    })
                .AddGeneration(
                    // 3
                    source: """
                        using System;
                        
                        class C
                        {
                        }
                        """,
                    edits: new[]
                    {
                        Edit(SemanticEditKind.Delete, symbolProvider: c => c.GetMember("C.F"), newSymbolProvider: c => c.GetMember("C")),
                    },
                    validator: g =>
                    {
                        // unchanged from previous generation:
                        g.VerifySynthesizedMembers(
                            "System.Runtime.CompilerServices.HotReloadException",
                            "C: {<>c}",
                            "C.<>c: {<>9__0#2_0#2, <F>b__0#2_0#2}");

                        g.VerifyTypeDefNames();
                        g.VerifyMethodDefNames("F", "<F>b__0#2_0#2");
                        g.VerifyTypeRefNames("Object");
                        g.VerifyMemberRefNames();

                        g.VerifyEncLogDefinitions(new[]
                        {
                            Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(7, TableIndex.MethodDef, EditAndContinueOperation.Default)
                        });

                        g.VerifyEncMapDefinitions(new[]
                        {
                            Handle(1, TableIndex.MethodDef),
                            Handle(7, TableIndex.MethodDef)
                        });

                        g.VerifyIL("""
                            F
                            {
                              // Code size       13 (0xd)
                              .maxstack  8
                              IL_0000:  ldstr      0x70000299
                              IL_0005:  ldc.i4.s   -2
                              IL_0007:  newobj     0x06000006
                              IL_000c:  throw
                            }
                            <F>b__0#2_0#2
                            {
                              // Code size       12 (0xc)
                              .maxstack  8
                              IL_0000:  ldstr      0x700003E2
                              IL_0005:  ldc.i4.m1
                              IL_0006:  newobj     0x06000006
                              IL_000b:  throw
                            }
                            """);
                    })
                .Verify();
        }

        [Fact]
        public void Method_Delete_WithLambda_AddedMethod()
        {
            using var _ = new EditAndContinueTest()
                .AddBaseline(
                    source: $$"""
                        using System;

                        class C
                        {
                        }
                        """,
                    validator: g =>
                    {
                        g.VerifySynthesizedMembers();
                    })
                .AddGeneration(
                    // 1: Add method with a lambda
                    source: """
                        using System;
                        
                        class C
                        {
                            void F() { _ = new Action(() => Console.WriteLine(1)); } 
                        }
                        """,
                    edits: new[]
                    {
                        Edit(SemanticEditKind.Insert, c => c.GetMember("C.F")),
                    },
                    validator: g =>
                    {
                        g.VerifySynthesizedMembers(
                            "C: {<>c}",
                            "C.<>c: {<>9__0#1_0#1, <F>b__0#1_0#1}");

                        g.VerifyTypeDefNames("<>c");
                        g.VerifyMethodDefNames("F", ".cctor", ".ctor", "<F>b__0#1_0#1");
                    }
                )
                .AddGeneration(
                    // 2: Delete the method
                    source: """
                        using System;
                        
                        class C
                        {
                        }
                        """,
                    edits: new[]
                    {
                        Edit(SemanticEditKind.Delete, symbolProvider: c => c.GetMember("C.F"), newSymbolProvider: c => c.GetMember("C")),
                    },
                    validator: g =>
                    {
                        g.VerifySynthesizedMembers(
                            "System.Runtime.CompilerServices.HotReloadException",
                            "C: {<>c}",
                            "C.<>c: {<>9__0#1_0#1, <F>b__0#1_0#1}");

                        g.VerifyTypeDefNames("HotReloadException");
                        g.VerifyMethodDefNames("F", "<F>b__0#1_0#1", ".ctor");
                        g.VerifyTypeRefNames("Object", "CompilerGeneratedAttribute", "Exception", "Action`1");
                        g.VerifyMemberRefNames(".ctor", ".ctor", "Invoke");

                        g.VerifyEncLogDefinitions(new[]
                        {
                            Row(1, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                            Row(4, TableIndex.TypeDef, EditAndContinueOperation.Default),
                            Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(3, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(4, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                            Row(6, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(5, TableIndex.CustomAttribute, EditAndContinueOperation.Default)
                        });

                        g.VerifyEncMapDefinitions(new[]
                        {
                            Handle(4, TableIndex.TypeDef),
                            Handle(3, TableIndex.Field),
                            Handle(4, TableIndex.Field),
                            Handle(2, TableIndex.MethodDef),
                            Handle(5, TableIndex.MethodDef),
                            Handle(6, TableIndex.MethodDef),
                            Handle(5, TableIndex.CustomAttribute),
                            Handle(1, TableIndex.StandAloneSig)
                        });

                        g.VerifyIL("""
                            F
                            {
                              // Code size       13 (0xd)
                              .maxstack  8
                              IL_0000:  ldstr      0x70000009
                              IL_0005:  ldc.i4.s   -2
                              IL_0007:  newobj     0x06000006
                              IL_000c:  throw
                            }
                            <F>b__0#1_0#1
                            {
                              // Code size       12 (0xc)
                              .maxstack  8
                              IL_0000:  ldstr      0x70000152
                              IL_0005:  ldc.i4.m1
                              IL_0006:  newobj     0x06000006
                              IL_000b:  throw
                            }
                            .ctor
                            {
                              // Code size       33 (0x21)
                              .maxstack  2
                              IL_0000:  ldarg.0
                              IL_0001:  ldarg.1
                              IL_0002:  call       0x0A00000A
                              IL_0007:  nop
                              IL_0008:  ldarg.0
                              IL_0009:  ldarg.2
                              IL_000a:  stfld      0x04000003
                              IL_000f:  ldsfld     0x04000004
                              IL_0014:  dup
                              IL_0015:  stloc.0
                              IL_0016:  brfalse.s  IL_0020
                              IL_0018:  ldloc.0
                              IL_0019:  ldarg.0
                              IL_001a:  callvirt   0x0A00000B
                              IL_001f:  nop
                              IL_0020:  ret
                            }
                            """);
                    })
                .Verify();
        }

        [Fact]
        public void Method_Delete_WithLambda_MultipleGenerations()
        {
            var common = """
                using System;
                class A : Attribute { }
                """;

            var synthesized = new[]
            {
                "Microsoft.CodeAnalysis.EmbeddedAttribute",
                "System.Runtime.CompilerServices.IsReadOnlyAttribute",
                "System.Runtime.CompilerServices.RequiresLocationAttribute",
            };

            using var _ = new EditAndContinueTest(verification: Verification.Skipped)
                .AddBaseline(
                    source: common + """
                        class C<T>
                        {
                            ref readonly S F<[A]S>([A]T a, ref readonly S b) where S : struct
                            <N:0>{</N:0>
                                _ = new Action<int>(<N:1>x => Console.WriteLine(1)</N:1>);
                                return ref b;
                            }
                        }
                        """,
                    validator: g =>
                    {
                        g.VerifySynthesizedMembers(
                        [
                            .. synthesized,
                            "C<T>: {<>c__0}",
                            "C<T>.<>c__0<S>: {<>9__0_0, <F>b__0_0}"
                        ]);
                    })
                .AddGeneration(
                    // 1
                    source: common + """
                        class C<T>
                        {
                            ref readonly S F<[A]S>([A]T a, ref readonly S b) where S : struct
                            <N:0>{</N:0>
                                _ = new Action<int>(<N:1>q => Console.WriteLine(1)</N:1>);
                                _ = new Action<S>(<N:2>s => Console.WriteLine(2)</N:2>);
                                return ref b;
                            }
                        }
                        """,
                    edits:
                    [
                        Edit(SemanticEditKind.Update, c => c.GetMember("C.F"), preserveLocalVariables: true),
                    ],
                    validator: g =>
                    {
                        g.VerifySynthesizedMembers(
                        [
                            .. synthesized,
                            "C<T>: {<>c__0}",
                            "C<T>.<>c__0<S>: {<>9__0_0, <>9__0_1#1, <F>b__0_0, <F>b__0_1#1}"
                        ]);
                    })
                .AddGeneration(
                    // 2
                    source: common + """
                        class C<T>
                        {
                            ref readonly S F<[A]S>([A]T a, ref readonly S b) where S : struct
                            <N:0>{</N:0>
                                _ = new Action<int>(<N:1>q => Console.WriteLine(1)</N:1>);
                                _ = new Action<S>(<N:2>s => Console.WriteLine(2)</N:2>);
                                _ = new Action<T>(<N:3>t => Console.WriteLine(3)</N:3>);
                                return ref b;
                            }
                        }
                        """,
                    edits:
                    [
                        Edit(SemanticEditKind.Update, c => c.GetMember("C.F"), preserveLocalVariables: true),
                    ],
                    validator: g =>
                    {
                        g.VerifySynthesizedMembers(
                        [
                            .. synthesized,
                            "C<T>: {<>c__0}",
                            "C<T>.<>c__0<S>: {<>9__0_0, <>9__0_1#1, <>9__0_2#2, <F>b__0_0, <F>b__0_1#1, <F>b__0_2#2}"
                        ]);
                    })
                .AddGeneration(
                    // 3
                    source: common + """
                        class C<T>
                        {
                        }
                        """,
                    edits:
                    [
                        Edit(SemanticEditKind.Delete, c => c.GetMember("C.F"), newSymbolProvider: c => c.GetMember("C")),
                    ],
                    validator: g =>
                    {
                        g.VerifySynthesizedMembers(
                        [
                            .. synthesized,
                            "System.Runtime.CompilerServices.HotReloadException",
                            "C<T>: {<>c__0}",
                            "C<T>.<>c__0<S>: {<>9__0_0, <>9__0_1#1, <>9__0_2#2, <F>b__0_0, <F>b__0_1#1, <F>b__0_2#2}"
                        ]);

                        g.VerifyTypeDefNames("HotReloadException");
                        g.VerifyMethodDefNames("F", "<F>b__0_0", "<F>b__0_1#1", "<F>b__0_2#2", ".ctor");

                        // Note: InAttribute is a custom modifier included in the signature
                        g.VerifyTypeRefNames("Object", "InAttribute", "CompilerGeneratedAttribute", "Exception", "Action`1");

                        g.VerifyMemberRefNames(".ctor", ".ctor", "Invoke");

                        g.VerifyEncLogDefinitions(new[]
                        {
                            Row(4, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                            Row(8, TableIndex.TypeDef, EditAndContinueOperation.Default),
                            Row(8, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(5, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(8, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(6, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(9, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(10, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(11, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(8, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                            Row(12, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(15, TableIndex.CustomAttribute, EditAndContinueOperation.Default)
                        });

                        g.VerifyEncMapDefinitions(new[]
                        {
                            Handle(8, TableIndex.TypeDef),
                            Handle(5, TableIndex.Field),
                            Handle(6, TableIndex.Field),
                            Handle(5, TableIndex.MethodDef),
                            Handle(9, TableIndex.MethodDef),
                            Handle(10, TableIndex.MethodDef),
                            Handle(11, TableIndex.MethodDef),
                            Handle(12, TableIndex.MethodDef),
                            Handle(15, TableIndex.CustomAttribute),
                            Handle(4, TableIndex.StandAloneSig)
                        });

                        g.VerifyIL("""
                            F
                            {
                              // Code size       13 (0xd)
                              .maxstack  8
                              IL_0000:  ldstr      0x7000000D
                              IL_0005:  ldc.i4.s   -2
                              IL_0007:  newobj     0x0600000C
                              IL_000c:  throw
                            }
                            <F>b__0_0, <F>b__0_1#1, <F>b__0_2#2
                            {
                              // Code size       12 (0xc)
                              .maxstack  8
                              IL_0000:  ldstr      0x70000156
                              IL_0005:  ldc.i4.m1
                              IL_0006:  newobj     0x0600000C
                              IL_000b:  throw
                            }
                            .ctor
                            {
                              // Code size       33 (0x21)
                              .maxstack  2
                              IL_0000:  ldarg.0
                              IL_0001:  ldarg.1
                              IL_0002:  call       0x0A000024
                              IL_0007:  nop
                              IL_0008:  ldarg.0
                              IL_0009:  ldarg.2
                              IL_000a:  stfld      0x04000005
                              IL_000f:  ldsfld     0x04000006
                              IL_0014:  dup
                              IL_0015:  stloc.0
                              IL_0016:  brfalse.s  IL_0020
                              IL_0018:  ldloc.0
                              IL_0019:  ldarg.0
                              IL_001a:  callvirt   0x0A000025
                              IL_001f:  nop
                              IL_0020:  ret
                            }
                            """);
                    })
                .AddGeneration(
                    // 4: Add deleted method back with another lambda
                    source: common + """
                        class C<T>
                        {
                            void F<[A]S>([A]T a, S b) where S : struct
                            {
                                _ = new Action<T>(r => Console.WriteLine(4));
                            }
                        }
                        """,
                    edits:
                    [
                        Edit(SemanticEditKind.Insert, c => c.GetMember("C.F")),
                    ],
                    validator: g =>
                    {
                        g.VerifySynthesizedMembers(
                        [
                            .. synthesized,
                            "System.Runtime.CompilerServices.HotReloadException",
                            "C<T>: {<>c__0#4, <>c__0}",
                            "C<T>.<>c__0#4<S>: {<>9__0#4_0#4, <F>b__0#4_0#4}",
                            "C<T>.<>c__0<S>: {<>9__0_0, <>9__0_1#1, <>9__0_2#2, <F>b__0_0, <F>b__0_1#1, <F>b__0_2#2}"
                        ]);

                        g.VerifyTypeDefNames("<>c__0#4`1");
                        g.VerifyMethodDefNames("F", ".cctor", ".ctor", "<F>b__0#4_0#4");
                        g.VerifyTypeRefNames("Object", "ValueType", "CompilerGeneratedAttribute", "Action`1", "Console");
                        g.VerifyMemberRefNames(".ctor", "<>9__0#4_0#4", "<>9", "<F>b__0#4_0#4", ".ctor", ".ctor", "<>9", ".ctor", "WriteLine");

                        g.VerifyIL("""
                            F
                            {
                              // Code size       30 (0x1e)
                              .maxstack  8
                              IL_0000:  nop
                              IL_0001:  ldsfld     0x0A000027
                              IL_0006:  brtrue.s   IL_001d
                              IL_0008:  ldsfld     0x0A000028
                              IL_000d:  ldftn      0x0A000029
                              IL_0013:  newobj     0x0A00002A
                              IL_0018:  stsfld     0x0A000027
                              IL_001d:  ret
                            }
                            .cctor
                            {
                              // Code size       11 (0xb)
                              .maxstack  8
                              IL_0000:  newobj     0x0A00002B
                              IL_0005:  stsfld     0x0A00002C
                              IL_000a:  ret
                            }
                            .ctor
                            {
                              // Code size        8 (0x8)
                              .maxstack  8
                              IL_0000:  ldarg.0
                              IL_0001:  call       0x0A00002D
                              IL_0006:  nop
                              IL_0007:  ret
                            }
                            <F>b__0#4_0#4
                            {
                              // Code size        8 (0x8)
                              .maxstack  8
                              IL_0000:  ldc.i4.4
                              IL_0001:  call       0x0A00002E
                              IL_0006:  nop
                              IL_0007:  ret
                            }
                            """);
                    })
                .AddGeneration(
                    // 5: Delete the method again.
                    source: common + """
                        class C<T>
                        {
                        }
                        """,
                    edits:
                    [
                        Edit(SemanticEditKind.Delete, c => c.GetMember("C.F"), newSymbolProvider: c => c.GetMember("C")),
                    ],
                    validator: g =>
                    {
                        g.VerifySynthesizedMembers(
                        [
                            .. synthesized,
                            "System.Runtime.CompilerServices.HotReloadException",
                            "C<T>: {<>c__0#4, <>c__0}",
                            "C<T>.<>c__0#4<S>: {<>9__0#4_0#4, <F>b__0#4_0#4}",
                            "C<T>.<>c__0<S>: {<>9__0_0, <>9__0_1#1, <>9__0_2#2, <F>b__0_0, <F>b__0_1#1, <F>b__0_2#2}"
                        ]);

                        g.VerifyTypeDefNames();

                        // Only lambdas that were not deleted before are updated:
                        g.VerifyMethodDefNames("F", "<F>b__0#4_0#4");

                        g.VerifyTypeRefNames("Object");
                        g.VerifyMemberRefNames();

                        g.VerifyEncLogDefinitions(new[]
                        {
                            Row(13, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(16, TableIndex.MethodDef, EditAndContinueOperation.Default)
                        });

                        g.VerifyEncMapDefinitions(new[]
                        {
                            Handle(13, TableIndex.MethodDef),
                            Handle(16, TableIndex.MethodDef)
                        });

                        g.VerifyIL("""
                            F
                            {
                              // Code size       13 (0xd)
                              .maxstack  8
                              IL_0000:  ldstr      0x700002A1
                              IL_0005:  ldc.i4.s   -2
                              IL_0007:  newobj     0x0600000C
                              IL_000c:  throw
                            }
                            <F>b__0#4_0#4
                            {
                              // Code size       12 (0xc)
                              .maxstack  8
                              IL_0000:  ldstr      0x700003EA
                              IL_0005:  ldc.i4.m1
                              IL_0006:  newobj     0x0600000C
                              IL_000b:  throw
                            }
                            """);
                    })
                .Verify();
        }

        [Fact]
        public void Method_Delete_WithLocalFunction_MultipleGenerations()
        {
            var common = """
                using System;
                class A : Attribute { }
                """ + MetadataUpdateDeletedAttributeSource;

            var synthesized = new[]
            {
                "Microsoft.CodeAnalysis.EmbeddedAttribute",
                "System.Runtime.CompilerServices.IsReadOnlyAttribute",
                "System.Runtime.CompilerServices.RequiresLocationAttribute",
            };

            using var _ = new EditAndContinueTest(verification: Verification.Skipped)
                .AddBaseline(
                    source: common + """
                        class C<T>
                        {
                            void F(T x, ref readonly int b)
                            <N:0>{</N:0>
                                <N:1>ref readonly S L<[A]S>([A]T a, ref readonly S b) where S : struct => ref b;</N:1>
                                _ = L(x, b);
                            }
                        }
                        """,
                    validator: g =>
                    {
                        g.VerifySynthesizedMembers(
                        [
                            .. synthesized,
                            "C<T>: {<F>g__L|0_0}",
                        ]);
                    })
                .AddGeneration(
                    // 1
                    source: common + """
                        class C<T>
                        {
                            void F(T x, ref readonly int b)
                            <N:0>{</N:0>
                                <N:1>ref readonly S L<[A]S>([A]T a, ref readonly S b) where S : struct => ref b;</N:1>
                                <N:2>void M<[A]S>(T a) { Console.WriteLine(2); }</N:2>
                                _ = L(x, b);
                                M<T>(x);
                            }
                        }
                        """,
                    edits:
                    [
                        Edit(SemanticEditKind.Update, c => c.GetMember("C.F"), preserveLocalVariables: true),
                    ],
                    validator: g =>
                    {
                        g.VerifySynthesizedMembers(
                        [
                            .. synthesized,
                            "C<T>: {<F>g__L|0_0, <F>g__M|0_1#1}"
                        ]);

                        g.VerifyCustomAttributes(
                            "[A..ctor] T",
                            "[System.Runtime.CompilerServices.RequiresLocationAttribute..ctor] b",
                            "[System.Runtime.CompilerServices.IsReadOnlyAttribute..ctor] <nil>",
                            "[A..ctor] a",
                            "[System.Runtime.CompilerServices.RequiresLocationAttribute..ctor] b",
                            "[System.Runtime.CompilerServices.CompilerGeneratedAttribute..ctor] C`1.<F>g__L|0_0",
                            "[System.Runtime.CompilerServices.CompilerGeneratedAttribute..ctor] C`1.<F>g__M|0_1#1");
                    })
                .AddGeneration(
                    // 2
                    source: common + """
                        class C<T>
                        {
                        }
                        """,
                    edits:
                    [
                        Edit(SemanticEditKind.Delete, c => c.GetMember("C.F"), newSymbolProvider: c => c.GetMember("C")),
                    ],
                    validator: g =>
                    {
                        g.VerifySynthesizedMembers(
                        [
                            .. synthesized,
                            "System.Runtime.CompilerServices.HotReloadException",
                            "C<T>: {<F>g__L|0_0, <F>g__M|0_1#1}"
                        ]);

                        g.VerifyTypeDefNames("HotReloadException");
                        g.VerifyMethodDefNames("F", "<F>g__L|0_0", "<F>g__M|0_1#1", ".ctor");
                        g.VerifyTypeRefNames("Object", "InAttribute", "CompilerGeneratedAttribute", "Exception", "Action`1");

                        g.VerifyMemberRefNames(".ctor", ".ctor", "Invoke");

                        g.VerifyCustomAttributes(
                            "[System.Runtime.CompilerServices.MetadataUpdateDeletedAttribute..ctor] C`1.F",
                            "[System.Runtime.CompilerServices.CompilerGeneratedAttribute..ctor] System.Runtime.CompilerServices.HotReloadException");

                        g.VerifyEncLogDefinitions(new[]
                        {
                            Row(1, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                            Row(8, TableIndex.TypeDef, EditAndContinueOperation.Default),
                            Row(8, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(1, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(8, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(2, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(7, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(9, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(8, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                            Row(10, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(15, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(17, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(19, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(20, TableIndex.CustomAttribute, EditAndContinueOperation.Default)
                        });

                        g.VerifyEncMapDefinitions(new[]
                        {
                            Handle(8, TableIndex.TypeDef),
                            Handle(1, TableIndex.Field),
                            Handle(2, TableIndex.Field),
                            Handle(5, TableIndex.MethodDef),
                            Handle(7, TableIndex.MethodDef),
                            Handle(9, TableIndex.MethodDef),
                            Handle(10, TableIndex.MethodDef),
                            Handle(15, TableIndex.CustomAttribute),
                            Handle(17, TableIndex.CustomAttribute),
                            Handle(19, TableIndex.CustomAttribute),
                            Handle(20, TableIndex.CustomAttribute),
                            Handle(1, TableIndex.StandAloneSig)
                        });

                        g.VerifyIL("""
                            F
                            {
                              // Code size       13 (0xd)
                              .maxstack  8
                              IL_0000:  ldstr      0x70000009
                              IL_0005:  ldc.i4.s   -2
                              IL_0007:  newobj     0x0600000A
                              IL_000c:  throw
                            }
                            <F>g__L|0_0, <F>g__M|0_1#1
                            {
                              // Code size       12 (0xc)
                              .maxstack  8
                              IL_0000:  ldstr      0x70000152
                              IL_0005:  ldc.i4.m1
                              IL_0006:  newobj     0x0600000A
                              IL_000b:  throw
                            }
                            .ctor
                            {
                              // Code size       33 (0x21)
                              .maxstack  2
                              IL_0000:  ldarg.0
                              IL_0001:  ldarg.1
                              IL_0002:  call       0x0A00000E
                              IL_0007:  nop
                              IL_0008:  ldarg.0
                              IL_0009:  ldarg.2
                              IL_000a:  stfld      0x04000001
                              IL_000f:  ldsfld     0x04000002
                              IL_0014:  dup
                              IL_0015:  stloc.0
                              IL_0016:  brfalse.s  IL_0020
                              IL_0018:  ldloc.0
                              IL_0019:  ldarg.0
                              IL_001a:  callvirt   0x0A00000F
                              IL_001f:  nop
                              IL_0020:  ret
                            }
                            """);
                    })
                .AddGeneration(
                    // 3: Add deleted method back with another local function and lambda
                    source: common + """
                        class C<T>
                        {
                            void F(T x, ref readonly int b)
                            {
                                ref readonly T O(ref readonly T b)
                                {
                                    T N(T z) => z;
                                    _ = new Func<T>(() => N(x));
                                    return ref b;
                                }
                                _ = O(x);
                            }
                        }
                        """,
                    edits:
                    [
                        Edit(SemanticEditKind.Insert, c => c.GetMember("C.F")),
                    ],
                    validator: g =>
                    {
                        g.VerifySynthesizedMembers(
                        [
                            .. synthesized,
                            "System.Runtime.CompilerServices.HotReloadException",
                            "C<T>: {<F>g__N|0#3_1#3, <>c__DisplayClass0#3_0#3, <F>g__L|0_0, <F>g__M|0_1#1}",
                            "C<T>.<>c__DisplayClass0#3_0#3: {x, <F>g__O|0#3, <F>b__2#3}"
                        ]);

                        g.VerifyTypeDefNames("<>c__DisplayClass0#3_0#3");
                        g.VerifyMethodDefNames("F", "<F>g__N|0#3_1#3", ".ctor", "<F>g__O|0#3", "<F>b__2#3");
                        g.VerifyTypeRefNames("Object", "CompilerGeneratedAttribute", "InAttribute");
                        g.VerifyMemberRefNames(".ctor", ".ctor", "x", "<F>g__O|0#3", ".ctor", "<F>g__N|0#3_1#3");

                        g.VerifyCustomAttributes(
                            "[System.Runtime.CompilerServices.RequiresLocationAttribute..ctor] b",
                            "[System.Runtime.CompilerServices.IsReadOnlyAttribute..ctor] <nil>",
                            "[System.Runtime.CompilerServices.CompilerGeneratedAttribute..ctor] C`1.<>c__DisplayClass0#3_0#3",
                            "[System.Runtime.CompilerServices.RequiresLocationAttribute..ctor] b",
                            "[System.Runtime.CompilerServices.CompilerGeneratedAttribute..ctor] C`1.<F>g__N|0#3_1#3");

                        g.VerifyIL("""
                            F
                            {
                              // Code size       29 (0x1d)
                              .maxstack  2
                              IL_0000:  newobj     0x0A000011
                              IL_0005:  stloc.0
                              IL_0006:  ldloc.0
                              IL_0007:  ldarg.1
                              IL_0008:  stfld      0x0A000012
                              IL_000d:  nop
                              IL_000e:  nop
                              IL_000f:  ldloc.0
                              IL_0010:  ldloc.0
                              IL_0011:  ldflda     0x0A000012
                              IL_0016:  callvirt   0x0A000013
                              IL_001b:  pop
                              IL_001c:  ret
                            }
                            <F>g__N|0#3_1#3
                            {
                              // Code size        2 (0x2)
                              .maxstack  8
                              IL_0000:  ldarg.0
                              IL_0001:  ret
                            }
                            .ctor
                            {
                              // Code size        8 (0x8)
                              .maxstack  8
                              IL_0000:  ldarg.0
                              IL_0001:  call       0x0A000014
                              IL_0006:  nop
                              IL_0007:  ret
                            }
                            <F>g__O|0#3
                            {
                              // Code size        9 (0x9)
                              .maxstack  1
                              IL_0000:  nop
                              IL_0001:  nop
                              IL_0002:  nop
                              IL_0003:  ldarg.1
                              IL_0004:  stloc.0
                              IL_0005:  br.s       IL_0007
                              IL_0007:  ldloc.0
                              IL_0008:  ret
                            }
                            <F>b__2#3
                            {
                              // Code size       12 (0xc)
                              .maxstack  8
                              IL_0000:  ldarg.0
                              IL_0001:  ldfld      0x0A000012
                              IL_0006:  call       0x0A000015
                              IL_000b:  ret
                            }
                            """);
                    })
                .AddGeneration(
                    // 4: Delete the method again.
                    source: common + """
                        class C<T>
                        {
                        }
                        """,
                    edits:
                    [
                        Edit(SemanticEditKind.Delete, c => c.GetMember("C.F"), newSymbolProvider: c => c.GetMember("C")),
                    ],
                    validator: g =>
                    {
                        g.VerifySynthesizedMembers(
                        [
                            .. synthesized,
                            "System.Runtime.CompilerServices.HotReloadException",
                            "C<T>: {<F>g__N|0#3_1#3, <>c__DisplayClass0#3_0#3, <F>g__L|0_0, <F>g__M|0_1#1}",
                            "C<T>.<>c__DisplayClass0#3_0#3: {x, <F>g__O|0#3, <F>b__2#3}"
                        ]);

                        g.VerifyTypeDefNames();

                        // Only lambdas that were not deleted before are updated:
                        g.VerifyMethodDefNames("F", "<F>g__N|0#3_1#3", "<F>g__O|0#3", "<F>b__2#3");

                        g.VerifyTypeRefNames("Object", "InAttribute");
                        g.VerifyMemberRefNames();

                        g.VerifyCustomAttributes("[System.Runtime.CompilerServices.MetadataUpdateDeletedAttribute..ctor] C`1.F");

                        g.VerifyEncLogDefinitions(
                        [
                            Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(11, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(13, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(14, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(20, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(22, TableIndex.CustomAttribute, EditAndContinueOperation.Default)
                        ]);

                        g.VerifyIL("""
                            F
                            {
                              // Code size       13 (0xd)
                              .maxstack  8
                              IL_0000:  ldstr      0x7000029D
                              IL_0005:  ldc.i4.s   -2
                              IL_0007:  newobj     0x0600000A
                              IL_000c:  throw
                            }
                            <F>g__N|0#3_1#3, <F>g__O|0#3, <F>b__2#3
                            {
                              // Code size       12 (0xc)
                              .maxstack  8
                              IL_0000:  ldstr      0x700003E6
                              IL_0005:  ldc.i4.m1
                              IL_0006:  newobj     0x0600000A
                              IL_000b:  throw
                            }
                            """);
                    })
                .Verify();
        }

        [Fact]
        public void Method_Rename_Multiple()
        {
            using var test = new EditAndContinueTest()
                .AddBaseline(
                    source: $$"""
                        class C
                        {
                            int M1() { return 0; }
                        }
                        """,
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("<Module>", "C");
                        g.VerifyMethodDefNames("M1", ".ctor");
                        g.VerifyMemberRefNames(/*CompilationRelaxationsAttribute.*/".ctor", /*RuntimeCompatibilityAttribute.*/".ctor", /*Object.*/".ctor", /*DebuggableAttribute*/".ctor");
                    });

            for (int i = 0; i < 10; i++)
            {
#pragma warning disable format // https://github.com/dotnet/roslyn/issues/38588
                test.AddGeneration(
                    source: @$"
class C
{{
    int M2() {{ return {i}; }}
}}",
                    edits: new[] {
                        Edit(SemanticEditKind.Delete, symbolProvider: c => c.GetMember("C.M1"), newSymbolProvider: c => c.GetMember("C")),
                        Edit(SemanticEditKind.Insert, symbolProvider: c => c.GetMember("C.M2")),
                    },
                    validator: (i == 0)
                        ? g =>
                        {
                            g.VerifyTypeDefNames("HotReloadException");
                            g.VerifyMethodDefNames("M1", "M2", ".ctor");
                            g.VerifyDeletedMembers("C: {M1}");
                        }
                        : g =>
                        {
                            g.VerifyTypeDefNames();
                            g.VerifyMethodDefNames("M1", "M2");
                            g.VerifyDeletedMembers("C: {M1}");
                        })
                .AddGeneration(
                    source: @$"
class C
{{
    int M1() {{ return {i}; }}
}}",
                    edits: new[] {
                        Edit(SemanticEditKind.Delete, symbolProvider: c => c.GetMember("C.M2"), newSymbolProvider: c => c.GetMember("C")),
                        Edit(SemanticEditKind.Insert, symbolProvider: c => c.GetMember("C.M1")),
                    },
                    validator: g =>
                    {
                        g.VerifyTypeDefNames();
                        g.VerifyMethodDefNames("M1", "M2");
                        g.VerifyDeletedMembers("C: {M2}");
                    });
#pragma warning restore format
            }

            test.Verify();
        }

        [Fact]
        public void Method_ChangeParameterType()
        {
            using var _ = new EditAndContinueTest()
                .AddBaseline(
                    source: """
                        class C
                        {
                            void M(int someInt) { someInt.ToString(); }
                        }
                        """,
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("<Module>", "C");
                        g.VerifyMethodDefNames("M", ".ctor");
                    })
                .AddGeneration(
                    // 1
                    source: """
                        class C
                        {
                            void M(bool someBool) { someBool.ToString(); }
                        }
                        """,
                    edits: new[] {
                        Edit(SemanticEditKind.Delete, symbolProvider: c => c.GetMembers("C.M").FirstOrDefault(m => m.GetParameterTypes()[0].SpecialType == SpecialType.System_Int32)?.ISymbol, newSymbolProvider: c=>c.GetMember("C")),
                        Edit(SemanticEditKind.Insert, symbolProvider: c => c.GetMembers("C.M").FirstOrDefault(m => m.GetParameterTypes()[0].SpecialType == SpecialType.System_Boolean)?.ISymbol),
                    },
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("HotReloadException");
                        g.VerifyMethodDefNames("M", "M", ".ctor");
                        g.VerifyDeletedMembers("C: {M}");

                        g.VerifyEncLogDefinitions(new[]
                        {
                            Row(1, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                            Row(3, TableIndex.TypeDef, EditAndContinueOperation.Default),
                            Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(1, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(2, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(2, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                            Row(3, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                            Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(3, TableIndex.MethodDef, EditAndContinueOperation.AddParameter),
                            Row(2, TableIndex.Param, EditAndContinueOperation.Default),
                            Row(4, TableIndex.CustomAttribute, EditAndContinueOperation.Default)
                        });
                        g.VerifyEncMapDefinitions(new[]
                        {
                            Handle(3, TableIndex.TypeDef),
                            Handle(1, TableIndex.Field),
                            Handle(2, TableIndex.Field),
                            Handle(1, TableIndex.MethodDef),
                            Handle(3, TableIndex.MethodDef),
                            Handle(4, TableIndex.MethodDef),
                            Handle(2, TableIndex.Param),
                            Handle(4, TableIndex.CustomAttribute),
                            Handle(1, TableIndex.StandAloneSig)
                        });

                        g.VerifyIL("""
                            M
                            {
                              // Code size       13 (0xd)
                              .maxstack  8
                              IL_0000:  ldstr      0x70000005
                              IL_0005:  ldc.i4.s   -2
                              IL_0007:  newobj     0x06000004
                              IL_000c:  throw
                            }
                            M
                            {
                              // Code size       10 (0xa)
                              .maxstack  8
                              IL_0000:  nop
                              IL_0001:  ldarga.s   V_1
                              IL_0003:  call       0x0A000007
                              IL_0008:  pop
                              IL_0009:  ret
                            }
                            .ctor
                            {
                              // Code size       33 (0x21)
                              .maxstack  2
                              IL_0000:  ldarg.0
                              IL_0001:  ldarg.1
                              IL_0002:  call       0x0A000008
                              IL_0007:  nop
                              IL_0008:  ldarg.0
                              IL_0009:  ldarg.2
                              IL_000a:  stfld      0x04000001
                              IL_000f:  ldsfld     0x04000002
                              IL_0014:  dup
                              IL_0015:  stloc.0
                              IL_0016:  brfalse.s  IL_0020
                              IL_0018:  ldloc.0
                              IL_0019:  ldarg.0
                              IL_001a:  callvirt   0x0A000009
                              IL_001f:  nop
                              IL_0020:  ret
                            }
                            """);
                    })
                .AddGeneration(
                    // 2
                    source: """
                        class C
                        {
                            void M(int someInt) { someInt.ToString(); }
                        }
                        """,
                    edits: new[] {
                        Edit(SemanticEditKind.Delete, symbolProvider: c => c.GetMembers("C.M").FirstOrDefault(m => m.GetParameterTypes()[0].SpecialType == SpecialType.System_Boolean)?.ISymbol, newSymbolProvider: c=>c.GetMember("C")),
                        Edit(SemanticEditKind.Insert, symbolProvider: c => c.GetMembers("C.M").FirstOrDefault(m => m.GetParameterTypes()[0].SpecialType == SpecialType.System_Int32)?.ISymbol),
                    },
                    validator: g =>
                    {
                        g.VerifyTypeDefNames();
                        g.VerifyMethodDefNames("M", "M");
                        g.VerifyDeletedMembers("C: {M}");

                        g.VerifyEncLogDefinitions(new[]
                        {
                            Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(3, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(1, TableIndex.Param, EditAndContinueOperation.Default)
                        });

                        g.VerifyEncMapDefinitions(new[]
                        {
                            Handle(1, TableIndex.MethodDef),
                            Handle(3, TableIndex.MethodDef),
                            Handle(1, TableIndex.Param)
                        });

                        g.VerifyIL("""
                            M
                            {
                              // Code size       10 (0xa)
                              .maxstack  8
                              IL_0000:  nop
                              IL_0001:  ldarga.s   V_1
                              IL_0003:  call       0x0A00000A
                              IL_0008:  pop
                              IL_0009:  ret
                            }
                            M
                            {
                              // Code size       13 (0xd)
                              .maxstack  8
                              IL_0000:  ldstr      0x70000151
                              IL_0005:  ldc.i4.s   -2
                              IL_0007:  newobj     0x06000004
                              IL_000c:  throw
                            }
                            """);
                    })
                .Verify();
        }

        [Fact]
        public void Method_ChangeReturnType()
        {
            using var _ = new EditAndContinueTest()
                .AddBaseline(
                    source: $$"""
                        class C
                        {
                            string M(int someInt) { return someInt.ToString(); }
                        }
                        """,
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("<Module>", "C");
                        g.VerifyMethodDefNames("M", ".ctor");
                    })
                .AddGeneration(
                    // 1
                    source: $$"""
                        class C
                        {
                            int M(int someInt) { return someInt; }
                        }
                        """,
                    edits: new[] {
                        Edit(SemanticEditKind.Delete, symbolProvider: c => c.GetMembers("C.M").FirstOrDefault(m => m.GetTypeOrReturnType().SpecialType == SpecialType.System_String)?.ISymbol, newSymbolProvider: c=>c.GetMember("C")),
                        Edit(SemanticEditKind.Insert, symbolProvider: c => c.GetMembers("C.M").FirstOrDefault(m => m.GetTypeOrReturnType().SpecialType == SpecialType.System_Int32)?.ISymbol),
                    },
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("HotReloadException");
                        g.VerifyMethodDefNames("M", "M", ".ctor");
                        g.VerifyDeletedMembers("C: {M}");

                        g.VerifyEncLogDefinitions(new[]
                        {
                            Row(2, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                            Row(3, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                            Row(3, TableIndex.TypeDef, EditAndContinueOperation.Default),
                            Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(1, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(2, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(2, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                            Row(3, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                            Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(3, TableIndex.MethodDef, EditAndContinueOperation.AddParameter),
                            Row(2, TableIndex.Param, EditAndContinueOperation.Default),
                            Row(4, TableIndex.CustomAttribute, EditAndContinueOperation.Default)
                        });

                        g.VerifyEncMapDefinitions(new[]
                        {
                            Handle(3, TableIndex.TypeDef),
                            Handle(1, TableIndex.Field),
                            Handle(2, TableIndex.Field),
                            Handle(1, TableIndex.MethodDef),
                            Handle(3, TableIndex.MethodDef),
                            Handle(4, TableIndex.MethodDef),
                            Handle(2, TableIndex.Param),
                            Handle(4, TableIndex.CustomAttribute),
                            Handle(2, TableIndex.StandAloneSig),
                            Handle(3, TableIndex.StandAloneSig)
                        });

                        g.VerifyIL("""
                            M
                            {
                              // Code size       13 (0xd)
                              .maxstack  8
                              IL_0000:  ldstr      0x70000005
                              IL_0005:  ldc.i4.s   -2
                              IL_0007:  newobj     0x06000004
                              IL_000c:  throw
                            }
                            M
                            {
                              // Code size        7 (0x7)
                              .maxstack  1
                              IL_0000:  nop
                              IL_0001:  ldarg.1
                              IL_0002:  stloc.0
                              IL_0003:  br.s       IL_0005
                              IL_0005:  ldloc.0
                              IL_0006:  ret
                            }
                            .ctor
                            {
                              // Code size       33 (0x21)
                              .maxstack  2
                              IL_0000:  ldarg.0
                              IL_0001:  ldarg.1
                              IL_0002:  call       0x0A000007
                              IL_0007:  nop
                              IL_0008:  ldarg.0
                              IL_0009:  ldarg.2
                              IL_000a:  stfld      0x04000001
                              IL_000f:  ldsfld     0x04000002
                              IL_0014:  dup
                              IL_0015:  stloc.0
                              IL_0016:  brfalse.s  IL_0020
                              IL_0018:  ldloc.0
                              IL_0019:  ldarg.0
                              IL_001a:  callvirt   0x0A000008
                              IL_001f:  nop
                              IL_0020:  ret
                            }
                            """);
                    })
                .AddGeneration(
                    // 2
                    source: $$"""
                        class C
                        {
                            string M(int someInt) { return someInt.ToString(); }
                        }
                        """,
                    edits: new[] {
                        Edit(SemanticEditKind.Delete, symbolProvider: c => c.GetMembers("C.M").FirstOrDefault(m => m.GetTypeOrReturnType().SpecialType == SpecialType.System_Int32)?.ISymbol, newSymbolProvider: c=>c.GetMember("C")),
                        Edit(SemanticEditKind.Insert, symbolProvider: c => c.GetMembers("C.M").FirstOrDefault(m => m.GetTypeOrReturnType().SpecialType == SpecialType.System_String)?.ISymbol),
                    },
                    validator: g =>
                    {
                        g.VerifyTypeDefNames();
                        g.VerifyMethodDefNames("M", "M");
                        g.VerifyDeletedMembers("C: {M}");

                        g.VerifyEncLogDefinitions(new[]
                        {
                            Row(4, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                            Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(3, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(1, TableIndex.Param, EditAndContinueOperation.Default),
                        });

                        g.VerifyEncMapDefinitions(new[]
                        {
                            Handle(1, TableIndex.MethodDef),
                            Handle(3, TableIndex.MethodDef),
                            Handle(1, TableIndex.Param),
                            Handle(4, TableIndex.StandAloneSig)
                        });

                        g.VerifyIL("""
                            M
                            {
                              // Code size       13 (0xd)
                              .maxstack  1
                              IL_0000:  nop
                              IL_0001:  ldarga.s   V_1
                              IL_0003:  call       0x0A000009
                              IL_0008:  stloc.0
                              IL_0009:  br.s       IL_000b
                              IL_000b:  ldloc.0
                              IL_000c:  ret
                            }
                            M
                            {
                              // Code size       13 (0xd)
                              .maxstack  8
                              IL_0000:  ldstr      0x70000151
                              IL_0005:  ldc.i4.s   -2
                              IL_0007:  newobj     0x06000004
                              IL_000c:  throw
                            }
                            """);
                    })
                .Verify();
        }

        [Fact]
        public void Method_InsertAndDeleteParameter()
        {
            using var _ = new EditAndContinueTest()
                .AddBaseline(
                    source: $$"""
                        class C
                        {
                            void M(int someInt) { someInt.ToString(); }
                        }
                        """,
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("<Module>", "C");
                        g.VerifyMethodDefNames("M", ".ctor");
                    })
                .AddGeneration(
                    // 1
                    source: $$"""
                        class C
                        {
                            void M(int someInt, bool someBool) { someInt.ToString(); }
                        }
                        """,
                    edits: new[] {
                        Edit(SemanticEditKind.Delete, symbolProvider: c => c.GetMembers("C.M").FirstOrDefault(m => m.GetParameterCount() == 1)?.ISymbol, newSymbolProvider: c=>c.GetMember("C")),
                        Edit(SemanticEditKind.Insert, symbolProvider: c => c.GetMembers("C.M").FirstOrDefault(m => m.GetParameterCount() == 2)?.ISymbol),
                    },
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("HotReloadException");
                        g.VerifyMethodDefNames("M", "M", ".ctor");
                        g.VerifyDeletedMembers("C: {M}");

                        g.VerifyEncLogDefinitions(new[]
                        {
                            Row(1, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                            Row(3, TableIndex.TypeDef, EditAndContinueOperation.Default),
                            Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(1, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(2, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(2, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                            Row(3, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                            Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(3, TableIndex.MethodDef, EditAndContinueOperation.AddParameter),
                            Row(2, TableIndex.Param, EditAndContinueOperation.Default),
                            Row(3, TableIndex.MethodDef, EditAndContinueOperation.AddParameter),
                            Row(3, TableIndex.Param, EditAndContinueOperation.Default),
                            Row(4, TableIndex.CustomAttribute, EditAndContinueOperation.Default)
                        });

                        g.VerifyEncMapDefinitions(new[]
                        {
                            Handle(3, TableIndex.TypeDef),
                            Handle(1, TableIndex.Field),
                            Handle(2, TableIndex.Field),
                            Handle(1, TableIndex.MethodDef),
                            Handle(3, TableIndex.MethodDef),
                            Handle(4, TableIndex.MethodDef),
                            Handle(2, TableIndex.Param),
                            Handle(3, TableIndex.Param),
                            Handle(4, TableIndex.CustomAttribute),
                            Handle(1, TableIndex.StandAloneSig)
                        });

                        g.VerifyIL("""
                            M
                            {
                              // Code size       13 (0xd)
                              .maxstack  8
                              IL_0000:  ldstr      0x70000005
                              IL_0005:  ldc.i4.s   -2
                              IL_0007:  newobj     0x06000004
                              IL_000c:  throw
                            }
                            M
                            {
                              // Code size       10 (0xa)
                              .maxstack  8
                              IL_0000:  nop
                              IL_0001:  ldarga.s   V_1
                              IL_0003:  call       0x0A000007
                              IL_0008:  pop
                              IL_0009:  ret
                            }
                            .ctor
                            {
                              // Code size       33 (0x21)
                              .maxstack  2
                              IL_0000:  ldarg.0
                              IL_0001:  ldarg.1
                              IL_0002:  call       0x0A000008
                              IL_0007:  nop
                              IL_0008:  ldarg.0
                              IL_0009:  ldarg.2
                              IL_000a:  stfld      0x04000001
                              IL_000f:  ldsfld     0x04000002
                              IL_0014:  dup
                              IL_0015:  stloc.0
                              IL_0016:  brfalse.s  IL_0020
                              IL_0018:  ldloc.0
                              IL_0019:  ldarg.0
                              IL_001a:  callvirt   0x0A000009
                              IL_001f:  nop
                              IL_0020:  ret
                            }
                            """);
                    })
                .AddGeneration(
                    // 2
                    source: $$"""
                        class C
                        {
                            void M(int someInt) { someInt.ToString(); }
                        }
                        """,
                    edits: new[] {
                        Edit(SemanticEditKind.Delete, symbolProvider: c => c.GetMembers("C.M").FirstOrDefault(m => m.GetParameterCount() == 2)?.ISymbol, newSymbolProvider: c=>c.GetMember("C")),
                        Edit(SemanticEditKind.Insert, symbolProvider: c => c.GetMembers("C.M").FirstOrDefault(m => m.GetParameterCount() == 1)?.ISymbol),
                    },
                    validator: g =>
                    {
                        g.VerifyTypeDefNames();
                        g.VerifyMethodDefNames("M", "M");
                        g.VerifyDeletedMembers("C: {M}");

                        g.VerifyEncLogDefinitions(new[]
                        {
                            Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(3, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(1, TableIndex.Param, EditAndContinueOperation.Default),
                        });
                        g.VerifyEncMapDefinitions(new[]
                        {
                            Handle(1, TableIndex.MethodDef),
                            Handle(3, TableIndex.MethodDef),
                            Handle(1, TableIndex.Param),
                        });

                        g.VerifyIL("""
                            M
                            {
                              // Code size       10 (0xa)
                              .maxstack  8
                              IL_0000:  nop
                              IL_0001:  ldarga.s   V_1
                              IL_0003:  call       0x0A00000A
                              IL_0008:  pop
                              IL_0009:  ret
                            }
                            M
                            {
                              // Code size       13 (0xd)
                              .maxstack  8
                              IL_0000:  ldstr      0x70000151
                              IL_0005:  ldc.i4.s   -2
                              IL_0007:  newobj     0x06000004
                              IL_000c:  throw
                            }
                            """);
                    })
                .Verify();
        }

        [Fact]
        public void FileTypes_01()
        {
            var source0 = MarkedSource(@"
using System;
file class C
{
    void M()
    {
        Console.Write(<N:0>1</N:0>);
    }
}", "file1.cs");
            var source1 = MarkedSource(@"
using System;
file class C
{
    void M()
    {
        Console.Write(<N:0>2</N:0>);
    }
}", "file1.cs");

            var compilation0 = CreateCompilation(source0.Tree, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);

            var method0 = compilation0.GetMember<MethodSymbol>("C.M");
            var method1 = compilation1.GetMember<MethodSymbol>("C.M");

            var v0 = CompileAndVerify(compilation0, verify: Verification.Skipped);

            v0.VerifyIL("C@file1.M", @"
{
  // Code size        9 (0x9)
  .maxstack  1
  IL_0000:  nop
  IL_0001:  ldc.i4.1
  IL_0002:  call       ""void System.Console.Write(int)""
  IL_0007:  nop
  IL_0008:  ret
}
");

            using var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, method0, method1, GetSyntaxMapFromMarkers(source0, source1))));

            // There should be no diagnostics from rude edits
            diff.EmitResult.Diagnostics.Verify();

            diff.VerifyIL("C@file1.M", @"
{
  // Code size        9 (0x9)
  .maxstack  1
  IL_0000:  nop
  IL_0001:  ldc.i4.2
  IL_0002:  call       ""void System.Console.Write(int)""
  IL_0007:  nop
  IL_0008:  ret
}");
        }

        [Fact]
        public void FileTypes_02()
        {
            var source0 = MarkedSource(@"
using System;
file class C
{
    void M()
    {
        Console.Write(<N:0>1</N:0>);
    }
}", "file1.cs");
            var source1 = MarkedSource(@"
using System;
file class C
{
    void M()
    {
        Console.Write(<N:0>2</N:0>);
    }
}", "file1.cs");
            var source2 = MarkedSource(@"
using System;
file class C
{
    void M()
    {
        Console.Write(<N:0>3</N:0>);
    }
}", "file2.cs");

            var compilation0 = CreateCompilation(source0.Tree, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(new[] { source1.Tree, source2.Tree });

            var cm1_gen0 = compilation0.GetMember<MethodSymbol>("C.M");
            var cm1_gen1 = ((NamedTypeSymbol)compilation1.GetMembers("C")[0]).GetMember("M");
            var c2_gen1 = ((NamedTypeSymbol)compilation1.GetMembers("C")[1]);

            var v0 = CompileAndVerify(compilation0, verify: Verification.Skipped);

            v0.VerifyIL("C@file1.M", @"
{
  // Code size        9 (0x9)
  .maxstack  1
  IL_0000:  nop
  IL_0001:  ldc.i4.1
  IL_0002:  call       ""void System.Console.Write(int)""
  IL_0007:  nop
  IL_0008:  ret
}
");

            using var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, cm1_gen0, cm1_gen1, GetSyntaxMapFromMarkers(source0, source1)),
                    SemanticEdit.Create(SemanticEditKind.Insert, null, c2_gen1, syntaxMap: null)));

            // There should be no diagnostics from rude edits
            diff.EmitResult.Diagnostics.Verify();

            diff.VerifyIL("C@file1.M", @"
{
  // Code size        9 (0x9)
  .maxstack  1
  IL_0000:  nop
  IL_0001:  ldc.i4.2
  IL_0002:  call       ""void System.Console.Write(int)""
  IL_0007:  nop
  IL_0008:  ret
}");

            diff.VerifyIL("C@file2.M", @"
{
  // Code size        9 (0x9)
  .maxstack  1
  IL_0000:  nop
  IL_0001:  ldc.i4.3
  IL_0002:  call       ""void System.Console.Write(int)""
  IL_0007:  nop
  IL_0008:  ret
}");
        }

        [Fact]
        public void FileTypes_03()
        {
            var source0_gen0 = MarkedSource(@"
using System;
file class C
{
    void M()
    {
        Console.Write(<N:0>1</N:0>);
    }
}", "file1.cs");
            var source1_gen1 = MarkedSource(@"
using System;
file class C
{
    void M()
    {
        Console.Write(<N:0>2</N:0>);
    }
}", "file2.cs");
            var source0_gen1 = MarkedSource(@"
using System;
file class C
{
    void M()
    {
        Console.Write(<N:0>3</N:0>);
    }
}", "file1.cs");

            var compilation0 = CreateCompilation(source0_gen0.Tree, options: ComSafeDebugDll);
            // Because the order of syntax trees has changed here, the original type is considered deleted and the two new types are completely new, unrelated types.

            // https://github.com/dotnet/roslyn/issues/61999
            // we should handle this as a modification of an existing type rather than deletion and insertion of distinct types.
            // most likely, we either need to identify file types based on something stable like the SyntaxTree.FilePath, or store a mapping of the ordinals from one generation to the next.
            // although "real-world" compilations disallow duplicated file paths, duplicated or empty file paths are very common via direct use of the APIs, so there's not necessarily a single slam-dunk answer here.
            var compilation1 = compilation0.WithSource(new[] { source1_gen1.Tree, source0_gen1.Tree });

            var c1_gen0 = compilation0.GetMember("C");
            var c1_gen1 = (NamedTypeSymbol)compilation1.GetMembers("C")[0];
            var c2_gen1 = (NamedTypeSymbol)compilation1.GetMembers("C")[1];

            var v0 = CompileAndVerify(compilation0, verify: Verification.Skipped);

            v0.VerifyIL("C@file1.M", @"
{
  // Code size        9 (0x9)
  .maxstack  1
  IL_0000:  nop
  IL_0001:  ldc.i4.1
  IL_0002:  call       ""void System.Console.Write(int)""
  IL_0007:  nop
  IL_0008:  ret
}
");

            using var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Insert, null, c1_gen1, syntaxMap: null),
                    SemanticEdit.Create(SemanticEditKind.Insert, null, c2_gen1, syntaxMap: null)));

            // There should be no diagnostics from rude edits
            diff.EmitResult.Diagnostics.Verify();

            diff.VerifyIL("C@file1.M", @"
{
  // Code size        9 (0x9)
  .maxstack  1
  IL_0000:  nop
  IL_0001:  ldc.i4.3
  IL_0002:  call       ""void System.Console.Write(int)""
  IL_0007:  nop
  IL_0008:  ret
}");

            diff.VerifyIL("C@file2.M", @"
{
  // Code size        9 (0x9)
  .maxstack  1
  IL_0000:  nop
  IL_0001:  ldc.i4.2
  IL_0002:  call       ""void System.Console.Write(int)""
  IL_0007:  nop
  IL_0008:  ret
}");
        }

        [Fact]
        public void FileTypes_04()
        {
            var source1_gen0 = MarkedSource(@"
using System;
file class C
{
    void M()
    {
        Console.Write(<N:0>1</N:0>);
    }
}", "file1.cs");
            var source2_gen0 = MarkedSource(@"
using System;
file class C
{
    void M()
    {
        Console.Write(<N:0>2</N:0>);
    }
}", "file2.cs");
            var source2_gen1 = MarkedSource(@"
using System;
file class C
{
    void M()
    {
        Console.Write(<N:0>3</N:0>);
    }
}", "file2.cs");

            var compilation0 = CreateCompilation(new[] { source1_gen0.Tree, source2_gen0.Tree }, options: ComSafeDebugDll);

            var compilation1 = compilation0.WithSource(new[] { source2_gen1.Tree });

            var c1_gen0 = ((NamedTypeSymbol)compilation0.GetMembers("C")[0]);
            var c2_gen0 = ((NamedTypeSymbol)compilation0.GetMembers("C")[1]);
            var c2_gen1 = compilation1.GetMember("C");

            var v0 = CompileAndVerify(compilation0, verify: Verification.Skipped);

            v0.VerifyIL("C@file2.M", @"
{
  // Code size        9 (0x9)
  .maxstack  1
  IL_0000:  nop
  IL_0001:  ldc.i4.2
  IL_0002:  call       ""void System.Console.Write(int)""
  IL_0007:  nop
  IL_0008:  ret
}
");

            using var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Insert, null, c2_gen1, syntaxMap: null)));

            // There should be no diagnostics from rude edits
            diff.EmitResult.Diagnostics.Verify();

            diff.VerifyIL("C@file2.M", @"
{
  // Code size        9 (0x9)
  .maxstack  1
  IL_0000:  nop
  IL_0001:  ldc.i4.3
  IL_0002:  call       ""void System.Console.Write(int)""
  IL_0007:  nop
  IL_0008:  ret
}");
        }

        [Fact]
        public void StackAlloc()
        {
            using var _ = new EditAndContinueTest(targetFramework: TargetFramework.NetCoreApp, verification: Verification.Fails)
                .AddBaseline(
                    source: """
                        using System;
                        class C
                        {
                            void F()
                            {
                                Span<bool> <N:0>x = stackalloc bool[64]</N:0>;
                            }
                        }
                        """,
                    validator: g =>
                    {
                        g.VerifyMethodBody("C.F", """
                        {
                          // Code size       17 (0x11)
                          .maxstack  2
                          .locals init (System.Span<bool> V_0, //x
                                        System.Span<bool> V_1)
                          // sequence point: {
                          IL_0000:  nop
                          // sequence point: Span<bool>      x = stackalloc bool[64]      ;
                          IL_0001:  ldc.i4.s   64
                          IL_0003:  conv.u
                          IL_0004:  localloc
                          IL_0006:  ldc.i4.s   64
                          IL_0008:  newobj     "System.Span<bool>..ctor(void*, int)"
                          IL_000d:  stloc.1
                          IL_000e:  ldloc.1
                          IL_000f:  stloc.0
                          // sequence point: }
                          IL_0010:  ret
                        }
                        """);
                    })

                .AddGeneration(
                    source: """
                        using System;
                        class C
                        {
                            void F()
                            {
                                /**/Span<bool> <N:0>x = stackalloc bool[64]</N:0>;
                            }
                        }
                        """,
                    edits: new[]
                    {
                        Edit(SemanticEditKind.Update, symbolProvider: c => c.GetMember("C.F"), preserveLocalVariables: true),
                    },
                    validator: g =>
                    {
                        g.VerifyIL("C.F", """
                        {
                          // Code size       17 (0x11)
                          .maxstack  2
                          .locals init (System.Span<bool> V_0, //x
                                        [unchanged] V_1,
                                        System.Span<bool> V_2)
                          IL_0000:  nop
                          IL_0001:  ldc.i4.s   64
                          IL_0003:  conv.u
                          IL_0004:  localloc
                          IL_0006:  ldc.i4.s   64
                          IL_0008:  newobj     "System.Span<bool>..ctor(void*, int)"
                          IL_000d:  stloc.2
                          IL_000e:  ldloc.2
                          IL_000f:  stloc.0
                          IL_0010:  ret
                        }
                        """);
                    })
                .Verify();
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/69480")]
        public void PrivateImplDetails_DataFields_Arrays_FieldRvaNotSupported()
        {
            using var _ = new EditAndContinueTest()
                .AddBaseline(
                    source: $$"""
                        class C
                        {
                            byte[] b = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
                        }
                        """,
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("<Module>", "C", "<PrivateImplementationDetails>", "__StaticArrayInitTypeSize=10");
                        g.VerifyFieldDefNames("b", "1F825AA2F0020EF7CF91DFA30DA4668D791C5D4824FC8E41354B89EC05795AB3");
                        g.VerifyMethodDefNames(".ctor");
                    })

                .AddGeneration(
                    source: """
                        class C
                        {
                            byte[] b = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
                        }
                        """,
                    edits: new[]
                    {
                        Edit(SemanticEditKind.Update, symbolProvider: c => c.GetParameterlessConstructor("C")),
                    },
                    validator: g =>
                    {
                        g.VerifyTypeDefNames();
                        g.VerifyFieldDefNames();
                        g.VerifyMethodDefNames(".ctor");

                        g.VerifyEncLogDefinitions(new[]
                        {
                            Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default)
                        });

                        g.VerifyEncMapDefinitions(new[]
                        {
                            Handle(1, TableIndex.MethodDef)
                        });

                        g.VerifyIL("C..ctor", """
                        {
                          // Code size       65 (0x41)
                          .maxstack  5
                          IL_0000:  ldarg.0
                          IL_0001:  ldc.i4.s   11
                          IL_0003:  newarr     "byte"
                          IL_0008:  dup
                          IL_0009:  ldc.i4.1
                          IL_000a:  ldc.i4.1
                          IL_000b:  stelem.i1
                          IL_000c:  dup
                          IL_000d:  ldc.i4.2
                          IL_000e:  ldc.i4.2
                          IL_000f:  stelem.i1
                          IL_0010:  dup
                          IL_0011:  ldc.i4.3
                          IL_0012:  ldc.i4.3
                          IL_0013:  stelem.i1
                          IL_0014:  dup
                          IL_0015:  ldc.i4.4
                          IL_0016:  ldc.i4.4
                          IL_0017:  stelem.i1
                          IL_0018:  dup
                          IL_0019:  ldc.i4.5
                          IL_001a:  ldc.i4.5
                          IL_001b:  stelem.i1
                          IL_001c:  dup
                          IL_001d:  ldc.i4.6
                          IL_001e:  ldc.i4.6
                          IL_001f:  stelem.i1
                          IL_0020:  dup
                          IL_0021:  ldc.i4.7
                          IL_0022:  ldc.i4.7
                          IL_0023:  stelem.i1
                          IL_0024:  dup
                          IL_0025:  ldc.i4.8
                          IL_0026:  ldc.i4.8
                          IL_0027:  stelem.i1
                          IL_0028:  dup
                          IL_0029:  ldc.i4.s   9
                          IL_002b:  ldc.i4.s   9
                          IL_002d:  stelem.i1
                          IL_002e:  dup
                          IL_002f:  ldc.i4.s   10
                          IL_0031:  ldc.i4.s   10
                          IL_0033:  stelem.i1
                          IL_0034:  stfld      "byte[] C.b"
                          IL_0039:  ldarg.0
                          IL_003a:  call       "object..ctor()"
                          IL_003f:  nop
                          IL_0040:  ret
                        }
                        """);
                    })
                .Verify();
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/69480")]
        public void PrivateImplDetails_DataFields_Arrays_FieldRvaSupported()
        {
            using var _ = new EditAndContinueTest()
                .AddBaseline(
                    source: $$"""
                        class C
                        {
                            byte[] b = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
                        }
                        """,
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("<Module>", "C", "<PrivateImplementationDetails>", "__StaticArrayInitTypeSize=10");
                        g.VerifyFieldDefNames("b", "1F825AA2F0020EF7CF91DFA30DA4668D791C5D4824FC8E41354B89EC05795AB3");
                        g.VerifyMethodDefNames(".ctor");
                    })

                .AddGeneration(
                    source: """
                        class C
                        {
                            byte[] b = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
                        }
                        """,
                    edits: new[]
                    {
                        Edit(SemanticEditKind.Update, symbolProvider: c => c.GetParameterlessConstructor("C")),
                    },
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("<PrivateImplementationDetails>#1", "__StaticArrayInitTypeSize=11");
                        g.VerifyFieldDefNames("78A6273103D17C39A0B6126E226CEC70E33337F4BC6A38067401B54A33E78EAD");
                        g.VerifyMethodDefNames(".ctor");

                        g.VerifyEncLogDefinitions(new[]
                        {
                            Row(5, TableIndex.TypeDef, EditAndContinueOperation.Default),
                            Row(6, TableIndex.TypeDef, EditAndContinueOperation.Default),
                            Row(5, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(3, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(5, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(2, TableIndex.ClassLayout, EditAndContinueOperation.Default),
                            Row(2, TableIndex.FieldRva, EditAndContinueOperation.Default),
                            Row(2, TableIndex.NestedClass, EditAndContinueOperation.Default)
                        });

                        g.VerifyEncMapDefinitions(new[]
                        {
                            Handle(5, TableIndex.TypeDef),
                            Handle(6, TableIndex.TypeDef),
                            Handle(3, TableIndex.Field),
                            Handle(1, TableIndex.MethodDef),
                            Handle(5, TableIndex.CustomAttribute),
                            Handle(2, TableIndex.ClassLayout),
                            Handle(2, TableIndex.FieldRva),
                            Handle(2, TableIndex.NestedClass)
                        });

                        g.VerifyIL("C..ctor", """
                        {
                          // Code size       32 (0x20)
                          .maxstack  4
                          IL_0000:  ldarg.0
                          IL_0001:  ldc.i4.s   11
                          IL_0003:  newarr     "byte"
                          IL_0008:  dup
                          IL_0009:  ldtoken    "<PrivateImplementationDetails>#1.__StaticArrayInitTypeSize=11 <PrivateImplementationDetails>#1.78A6273103D17C39A0B6126E226CEC70E33337F4BC6A38067401B54A33E78EAD"
                          IL_000e:  call       "void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)"
                          IL_0013:  stfld      "byte[] C.b"
                          IL_0018:  ldarg.0
                          IL_0019:  call       "object..ctor()"
                          IL_001e:  nop
                          IL_001f:  ret
                        }
                        """);

                        // trailing zeros for alignment:
                        g.VerifyEncFieldRvaData("""
                            78A6273103D17C39A0B6126E226CEC70E33337F4BC6A38067401B54A33E78EAD: 00-01-02-03-04-05-06-07-08-09-0A-00-00
                            """);
                    },
                    options: new EmitDifferenceOptions() { EmitFieldRva = true })
                .Verify();
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/69480")]
        public void PrivateImplDetails_DataFields_StackAlloc_FieldRvaNotSupported()
        {
            using var _ = new EditAndContinueTest(targetFramework: TargetFramework.Net80, verification: Verification.Skipped)
                .AddBaseline(
                    source: $$"""
                        class C
                        {
                            void F() { System.ReadOnlySpan<byte> b = stackalloc byte[] { 0, 1, 2, 3, 4, 5, 6 }; }
                        }
                        """,
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("<Module>", "C", "<PrivateImplementationDetails>", "__StaticArrayInitTypeSize=7");
                        g.VerifyFieldDefNames("57355AC3303C148F11AEF7CB179456B9232CDE33A818DFDA2C2FCB9325749A6B");
                        g.VerifyMethodDefNames("F", ".ctor");
                    })

                .AddGeneration(
                    source: """
                        class C
                        {
                            void F() { System.ReadOnlySpan<byte> b = stackalloc byte[] { 0, 1, 2, 3, 4, 5, 6, 7 }; }
                        }
                        """,
                    edits: new[]
                    {
                        Edit(SemanticEditKind.Update, symbolProvider: c => c.GetMember("C.F")),
                    },
                    validator: g =>
                    {
                        g.VerifyTypeDefNames();
                        g.VerifyFieldDefNames();
                        g.VerifyMethodDefNames("F");

                        g.VerifyEncLogDefinitions(new[]
                        {
                            Row(2, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                            Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default)
                        });

                        g.VerifyEncMapDefinitions(new[]
                        {
                            Handle(1, TableIndex.MethodDef),
                            Handle(2, TableIndex.StandAloneSig)
                        });

                        g.VerifyIL("C.F", """
                        {
                          // Code size       58 (0x3a)
                          .maxstack  3
                          .locals init (System.ReadOnlySpan<byte> V_0, //b
                                        System.Span<byte> V_1)
                          IL_0000:  nop
                          IL_0001:  ldc.i4.8
                          IL_0002:  conv.u
                          IL_0003:  localloc
                          IL_0005:  dup
                          IL_0006:  ldc.i4.0
                          IL_0007:  stind.i1
                          IL_0008:  dup
                          IL_0009:  ldc.i4.1
                          IL_000a:  add
                          IL_000b:  ldc.i4.1
                          IL_000c:  stind.i1
                          IL_000d:  dup
                          IL_000e:  ldc.i4.2
                          IL_000f:  add
                          IL_0010:  ldc.i4.2
                          IL_0011:  stind.i1
                          IL_0012:  dup
                          IL_0013:  ldc.i4.3
                          IL_0014:  add
                          IL_0015:  ldc.i4.3
                          IL_0016:  stind.i1
                          IL_0017:  dup
                          IL_0018:  ldc.i4.4
                          IL_0019:  add
                          IL_001a:  ldc.i4.4
                          IL_001b:  stind.i1
                          IL_001c:  dup
                          IL_001d:  ldc.i4.5
                          IL_001e:  add
                          IL_001f:  ldc.i4.5
                          IL_0020:  stind.i1
                          IL_0021:  dup
                          IL_0022:  ldc.i4.6
                          IL_0023:  add
                          IL_0024:  ldc.i4.6
                          IL_0025:  stind.i1
                          IL_0026:  dup
                          IL_0027:  ldc.i4.7
                          IL_0028:  add
                          IL_0029:  ldc.i4.7
                          IL_002a:  stind.i1
                          IL_002b:  ldc.i4.8
                          IL_002c:  newobj     "System.Span<byte>..ctor(void*, int)"
                          IL_0031:  stloc.1
                          IL_0032:  ldloc.1
                          IL_0033:  call       "System.ReadOnlySpan<byte> System.Span<byte>.op_Implicit(System.Span<byte>)"
                          IL_0038:  stloc.0
                          IL_0039:  ret
                        }
                        """);
                    })
                .Verify();
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/69480")]
        public void PrivateImplDetails_DataFields_StackAlloc_FieldRvaSupported()
        {
            using var _ = new EditAndContinueTest(targetFramework: TargetFramework.Net80, verification: Verification.Skipped)
                .AddBaseline(
                    source: $$"""
                        class C
                        {
                            void F() { System.ReadOnlySpan<byte> b = stackalloc byte[] { 0, 1, 2, 3, 4, 5, 6 }; }
                        }
                        """,
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("<Module>", "C", "<PrivateImplementationDetails>", "__StaticArrayInitTypeSize=7");
                        g.VerifyFieldDefNames("57355AC3303C148F11AEF7CB179456B9232CDE33A818DFDA2C2FCB9325749A6B");
                        g.VerifyMethodDefNames("F", ".ctor");
                    })

                .AddGeneration(
                    source: """
                        class C
                        {
                            void F() { System.ReadOnlySpan<byte> b = stackalloc byte[] { 0, 1, 2, 3, 4, 5, 6, 7 }; }
                        }
                        """,
                    edits: new[]
                    {
                        Edit(SemanticEditKind.Update, symbolProvider: c => c.GetMember("C.F")),
                    },
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("<PrivateImplementationDetails>#1");
                        g.VerifyFieldDefNames("8A851FF82EE7048AD09EC3847F1DDF44944104D2CBD17EF4E3DB22C6785A0D45");
                        g.VerifyMethodDefNames("F");

                        g.VerifyEncLogDefinitions(new[]
                        {
                            Row(2, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                            Row(5, TableIndex.TypeDef, EditAndContinueOperation.Default),
                            Row(5, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(2, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(5, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(2, TableIndex.FieldRva, EditAndContinueOperation.Default)
                        });

                        g.VerifyEncMapDefinitions(new[]
                        {
                            Handle(5, TableIndex.TypeDef),
                            Handle(2, TableIndex.Field),
                            Handle(1, TableIndex.MethodDef),
                            Handle(5, TableIndex.CustomAttribute),
                            Handle(2, TableIndex.StandAloneSig),
                            Handle(2, TableIndex.FieldRva)
                        });

                        g.VerifyIL("C.F", """
                        {
                          // Code size       32 (0x20)
                          .maxstack  4
                          .locals init (System.ReadOnlySpan<byte> V_0, //b
                                        System.Span<byte> V_1)
                          IL_0000:  nop
                          IL_0001:  ldc.i4.8
                          IL_0002:  conv.u
                          IL_0003:  localloc
                          IL_0005:  dup
                          IL_0006:  ldsflda    "long <PrivateImplementationDetails>#1.8A851FF82EE7048AD09EC3847F1DDF44944104D2CBD17EF4E3DB22C6785A0D45"
                          IL_000b:  ldc.i4.8
                          IL_000c:  unaligned. 1
                          IL_000f:  cpblk
                          IL_0011:  ldc.i4.8
                          IL_0012:  newobj     "System.Span<byte>..ctor(void*, int)"
                          IL_0017:  stloc.1
                          IL_0018:  ldloc.1
                          IL_0019:  call       "System.ReadOnlySpan<byte> System.Span<byte>.op_Implicit(System.Span<byte>)"
                          IL_001e:  stloc.0
                          IL_001f:  ret
                        }
                        """);

                        // trailing zeros for alignment:
                        g.VerifyEncFieldRvaData("""
                            8A851FF82EE7048AD09EC3847F1DDF44944104D2CBD17EF4E3DB22C6785A0D45: 00-01-02-03-04-05-06-07
                            """);
                    },
                    options: new EmitDifferenceOptions() { EmitFieldRva = true })
                .Verify();
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/69480")]
        public void PrivateImplDetails_DataFields_Utf8_FieldRvaNotSupported()
        {
            using var _ = new EditAndContinueTest(targetFramework: TargetFramework.Net80, verification: Verification.Skipped)
                .AddBaseline(
                    source: """
                        class C
                        {
                            System.ReadOnlySpan<byte> F() => "0123456789"u8;
                        }
                        """,
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("<Module>", "C", "<PrivateImplementationDetails>", "__StaticArrayInitTypeSize=11");
                        g.VerifyFieldDefNames("BEB0DBD1C6FAC1140DD817514F2FBDF501E246BF16C8E877E71187E9EB008189");
                        g.VerifyMethodDefNames("F", ".ctor");
                    })

                .AddGeneration(
                    source: """
                        class C
                        {
                            System.ReadOnlySpan<byte> F() => "0123456789X"u8;
                        }
                        """,
                    edits: new[]
                    {
                        Edit(SemanticEditKind.Update, symbolProvider: c => c.GetMember("C.F")),
                    },
                    validator: g =>
                    {
                        g.VerifyTypeDefNames();
                        g.VerifyFieldDefNames();
                        g.VerifyMethodDefNames("F");

                        g.VerifyEncLogDefinitions(new[]
                        {
                            Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default)
                        });

                        g.VerifyEncMapDefinitions(new[]
                        {
                            Handle(1, TableIndex.MethodDef)
                        });

                        g.VerifyIL("C.F", """
                        {
                          // Code size       73 (0x49)
                          .maxstack  4
                          IL_0000:  ldc.i4.s   12
                          IL_0002:  newarr     "byte"
                          IL_0007:  dup
                          IL_0008:  ldc.i4.0
                          IL_0009:  ldc.i4.s   48
                          IL_000b:  stelem.i1
                          IL_000c:  dup
                          IL_000d:  ldc.i4.1
                          IL_000e:  ldc.i4.s   49
                          IL_0010:  stelem.i1
                          IL_0011:  dup
                          IL_0012:  ldc.i4.2
                          IL_0013:  ldc.i4.s   50
                          IL_0015:  stelem.i1
                          IL_0016:  dup
                          IL_0017:  ldc.i4.3
                          IL_0018:  ldc.i4.s   51
                          IL_001a:  stelem.i1
                          IL_001b:  dup
                          IL_001c:  ldc.i4.4
                          IL_001d:  ldc.i4.s   52
                          IL_001f:  stelem.i1
                          IL_0020:  dup
                          IL_0021:  ldc.i4.5
                          IL_0022:  ldc.i4.s   53
                          IL_0024:  stelem.i1
                          IL_0025:  dup
                          IL_0026:  ldc.i4.6
                          IL_0027:  ldc.i4.s   54
                          IL_0029:  stelem.i1
                          IL_002a:  dup
                          IL_002b:  ldc.i4.7
                          IL_002c:  ldc.i4.s   55
                          IL_002e:  stelem.i1
                          IL_002f:  dup
                          IL_0030:  ldc.i4.8
                          IL_0031:  ldc.i4.s   56
                          IL_0033:  stelem.i1
                          IL_0034:  dup
                          IL_0035:  ldc.i4.s   9
                          IL_0037:  ldc.i4.s   57
                          IL_0039:  stelem.i1
                          IL_003a:  dup
                          IL_003b:  ldc.i4.s   10
                          IL_003d:  ldc.i4.s   88
                          IL_003f:  stelem.i1
                          IL_0040:  ldc.i4.0
                          IL_0041:  ldc.i4.s   11
                          IL_0043:  newobj     "System.ReadOnlySpan<byte>..ctor(byte[], int, int)"
                          IL_0048:  ret
                        }
                        """);
                    })
                .Verify();
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/69480")]
        public void PrivateImplDetails_DataFields_Utf8_FieldRvaSupported()
        {
            using var _ = new EditAndContinueTest(targetFramework: TargetFramework.Net80, verification: Verification.Skipped)
                .AddBaseline(
                    source: """
                        class C
                        {
                            System.ReadOnlySpan<byte> F() => "0123456789"u8;
                        }
                        """,
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("<Module>", "C", "<PrivateImplementationDetails>", "__StaticArrayInitTypeSize=11");
                        g.VerifyFieldDefNames("BEB0DBD1C6FAC1140DD817514F2FBDF501E246BF16C8E877E71187E9EB008189");
                        g.VerifyMethodDefNames("F", ".ctor");
                    })

                .AddGeneration(
                    source: """
                        class C
                        {
                            System.ReadOnlySpan<byte> F() => "0123456789X"u8;
                        }
                        """,
                    edits: new[]
                    {
                        Edit(SemanticEditKind.Update, symbolProvider: c => c.GetMember("C.F")),
                    },
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("<PrivateImplementationDetails>#1", "__StaticArrayInitTypeSize=12");
                        g.VerifyFieldDefNames("AFB1C33C5229BFF7EF739BA44DA795A2B68A49E06001C07C5B026CAA6C6322BB");
                        g.VerifyMethodDefNames("F");

                        g.VerifyEncLogDefinitions(new[]
                        {
                            Row(5, TableIndex.TypeDef, EditAndContinueOperation.Default),
                            Row(6, TableIndex.TypeDef, EditAndContinueOperation.Default),
                            Row(5, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(2, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(5, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(2, TableIndex.ClassLayout, EditAndContinueOperation.Default),
                            Row(2, TableIndex.FieldRva, EditAndContinueOperation.Default),
                            Row(2, TableIndex.NestedClass, EditAndContinueOperation.Default)
                        });

                        g.VerifyEncMapDefinitions(new[]
                        {
                            Handle(5, TableIndex.TypeDef),
                            Handle(6, TableIndex.TypeDef),
                            Handle(2, TableIndex.Field),
                            Handle(1, TableIndex.MethodDef),
                            Handle(5, TableIndex.CustomAttribute),
                            Handle(2, TableIndex.ClassLayout),
                            Handle(2, TableIndex.FieldRva),
                            Handle(2, TableIndex.NestedClass)
                        });

                        g.VerifyIL("C.F", """
                        {
                          // Code size       13 (0xd)
                          .maxstack  2
                          IL_0000:  ldsflda    "<PrivateImplementationDetails>#1.__StaticArrayInitTypeSize=12 <PrivateImplementationDetails>#1.AFB1C33C5229BFF7EF739BA44DA795A2B68A49E06001C07C5B026CAA6C6322BB"
                          IL_0005:  ldc.i4.s   11
                          IL_0007:  newobj     "System.ReadOnlySpan<byte>..ctor(void*, int)"
                          IL_000c:  ret
                        }
                        """);

                        // trailing zeros for alignment:
                        g.VerifyEncFieldRvaData($"""
                            AFB1C33C5229BFF7EF739BA44DA795A2B68A49E06001C07C5B026CAA6C6322BB: {BitConverter.ToString(Encoding.UTF8.GetBytes("0123456789X"))}-00-00-00-00-00-00-00
                            """);
                    },
                    options: new EmitDifferenceOptions() { EmitFieldRva = true })
                .Verify();
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/69480")]
        public void PrivateImplDetails_DataSectionStringLiterals_FieldRvaSupported()
        {
            var parseOptions = TestOptions.Regular.WithFeature("experimental-data-section-string-literals", "0");

            using var _ = new EditAndContinueTest(targetFramework: TargetFramework.Net90, verification: Verification.Skipped, parseOptions: parseOptions)
                .AddBaseline(
                    source: """
                        class C
                        {
                            string F() => "0123456789";
                        }
                        """,
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("<Module>", "C", "<PrivateImplementationDetails>", "__StaticArrayInitTypeSize=10", "<S>E353667619EC664B49655FC9692165FB");
                        g.VerifyFieldDefNames("84D89877F0D4041EFB6BF91A16F0248F2FD573E6AF05C19F96BEDB9F882F7882", "s");
                        g.VerifyMethodDefNames("F", ".ctor", "BytesToString", ".cctor");
                    })
                .AddGeneration(
                    source: """
                        class C
                        {
                            string F() => "0123456789X";
                        }
                        """,
                    edits:
                    [
                        Edit(SemanticEditKind.Update, symbolProvider: c => c.GetMember("C.F")),
                    ],
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("<PrivateImplementationDetails>#1", "__StaticArrayInitTypeSize=11", "<S>6D2201523542AEFFB91657B2AEBDC84B");
                        g.VerifyFieldDefNames("ACE59E7D984CCEB2D860A056A3386344236CE5C42C978E26ECE3F35956DAC3AD", "s");
                        g.VerifyMethodDefNames("F", "BytesToString", ".cctor");

                        g.VerifyEncLogDefinitions(
                        [
                            Row(6, TableIndex.TypeDef, EditAndContinueOperation.Default),
                            Row(7, TableIndex.TypeDef, EditAndContinueOperation.Default),
                            Row(8, TableIndex.TypeDef, EditAndContinueOperation.Default),
                            Row(6, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(3, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(8, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(4, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(6, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                            Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(8, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                            Row(6, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(5, TableIndex.MethodDef, EditAndContinueOperation.AddParameter),
                            Row(3, TableIndex.Param, EditAndContinueOperation.Default),
                            Row(5, TableIndex.MethodDef, EditAndContinueOperation.AddParameter),
                            Row(4, TableIndex.Param, EditAndContinueOperation.Default),
                            Row(6, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(2, TableIndex.ClassLayout, EditAndContinueOperation.Default),
                            Row(2, TableIndex.FieldRva, EditAndContinueOperation.Default),
                            Row(3, TableIndex.NestedClass, EditAndContinueOperation.Default),
                            Row(4, TableIndex.NestedClass, EditAndContinueOperation.Default)
                        ]);

                        g.VerifyEncMapDefinitions(
                        [
                            Handle(6, TableIndex.TypeDef),
                            Handle(7, TableIndex.TypeDef),
                            Handle(8, TableIndex.TypeDef),
                            Handle(3, TableIndex.Field),
                            Handle(4, TableIndex.Field),
                            Handle(1, TableIndex.MethodDef),
                            Handle(5, TableIndex.MethodDef),
                            Handle(6, TableIndex.MethodDef),
                            Handle(3, TableIndex.Param),
                            Handle(4, TableIndex.Param),
                            Handle(6, TableIndex.CustomAttribute),
                            Handle(2, TableIndex.ClassLayout),
                            Handle(2, TableIndex.FieldRva),
                            Handle(3, TableIndex.NestedClass),
                            Handle(4, TableIndex.NestedClass)
                        ]);

                        g.VerifyIL("C.F", """
                        {
                          // Code size        6 (0x6)
                          .maxstack  1
                          IL_0000:  ldsfld     "string <PrivateImplementationDetails>#1.<S>6D2201523542AEFFB91657B2AEBDC84B.s"
                          IL_0005:  ret
                        }
                        """);

                        // trailing zeros for alignment:
                        g.VerifyEncFieldRvaData($"""
                            ACE59E7D984CCEB2D860A056A3386344236CE5C42C978E26ECE3F35956DAC3AD: {BitConverter.ToString(Encoding.UTF8.GetBytes("0123456789X"))}-00
                            """);
                    },
                    options: new EmitDifferenceOptions() { EmitFieldRva = true })
                .Verify();
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/69480")]
        public void PrivateImplDetails_DataSectionStringLiterals_FieldRvaNotSupported()
        {
            var parseOptions = TestOptions.Regular.WithFeature("experimental-data-section-string-literals", "0");

            using var _ = new EditAndContinueTest(targetFramework: TargetFramework.Net90, verification: Verification.Skipped, parseOptions: parseOptions)
                .AddBaseline(
                    source: """
                        class C
                        {
                            string F() => "0123456789";
                        }
                        """,
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("<Module>", "C", "<PrivateImplementationDetails>", "__StaticArrayInitTypeSize=10", "<S>E353667619EC664B49655FC9692165FB");
                        g.VerifyFieldDefNames("84D89877F0D4041EFB6BF91A16F0248F2FD573E6AF05C19F96BEDB9F882F7882", "s");
                        g.VerifyMethodDefNames("F", ".ctor", "BytesToString", ".cctor");
                    })
                .AddGeneration(
                    source: """
                        class C
                        {
                            string F() => "0123456789X";
                        }
                        """,
                    edits:
                    [
                        Edit(SemanticEditKind.Update, symbolProvider: c => c.GetMember("C.F")),
                    ],
                    validator: g =>
                    {
                        g.VerifyTypeDefNames();
                        g.VerifyFieldDefNames();
                        g.VerifyMethodDefNames("F");

                        g.VerifyEncLogDefinitions(
                        [
                            Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default)
                        ]);

                        g.VerifyEncMapDefinitions(
                        [
                            Handle(1, TableIndex.MethodDef)
                        ]);

                        g.VerifyIL("C.F", """
                        {
                          // Code size        6 (0x6)
                          .maxstack  1
                          IL_0000:  ldstr      "0123456789X"
                          IL_0005:  ret
                        }
                        """);
                    })
                .Verify();
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/69480")]
        public void PrivateImplDetails_DataSectionStringLiterals_HeapOverflow_FieldRvaSupported()
        {
            // The longest string that can fit in the #US heap. The next string would overflow the heap.
            var baseString = new string('x', (1 << 23) - 3);

            using var _ = new EditAndContinueTest(targetFramework: TargetFramework.Net90, verification: Verification.Skipped)
                .AddBaseline(
                    source: $$"""
                        class C
                        {
                            string F() => "{{baseString}}";
                        }
                        """,
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("<Module>", "C");
                        g.VerifyFieldDefNames();
                        g.VerifyMethodDefNames("F", ".ctor");
                    })
                .AddGeneration(
                    source: """
                        class C
                        {
                            string F() => "0123456789";
                        }
                        """,
                    edits:
                    [
                        Edit(SemanticEditKind.Update, symbolProvider: c => c.GetMember("C.F")),
                    ],
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("<PrivateImplementationDetails>#1", "__StaticArrayInitTypeSize=10", "<S>E353667619EC664B49655FC9692165FB");
                        g.VerifyFieldDefNames("84D89877F0D4041EFB6BF91A16F0248F2FD573E6AF05C19F96BEDB9F882F7882", "s");
                        g.VerifyMethodDefNames("F", "BytesToString", ".cctor");

                        g.VerifyEncLogDefinitions(
                        [
                            Row(3, TableIndex.TypeDef, EditAndContinueOperation.Default),
                            Row(4, TableIndex.TypeDef, EditAndContinueOperation.Default),
                            Row(5, TableIndex.TypeDef, EditAndContinueOperation.Default),
                            Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(1, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(5, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(2, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                            Row(3, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(5, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                            Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(3, TableIndex.MethodDef, EditAndContinueOperation.AddParameter),
                            Row(1, TableIndex.Param, EditAndContinueOperation.Default),
                            Row(3, TableIndex.MethodDef, EditAndContinueOperation.AddParameter),
                            Row(2, TableIndex.Param, EditAndContinueOperation.Default),
                            Row(4, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(1, TableIndex.ClassLayout, EditAndContinueOperation.Default),
                            Row(1, TableIndex.FieldRva, EditAndContinueOperation.Default),
                            Row(1, TableIndex.NestedClass, EditAndContinueOperation.Default),
                            Row(2, TableIndex.NestedClass, EditAndContinueOperation.Default)
                        ]);

                        g.VerifyEncMapDefinitions(
                        [
                            Handle(3, TableIndex.TypeDef),
                            Handle(4, TableIndex.TypeDef),
                            Handle(5, TableIndex.TypeDef),
                            Handle(1, TableIndex.Field),
                            Handle(2, TableIndex.Field),
                            Handle(1, TableIndex.MethodDef),
                            Handle(3, TableIndex.MethodDef),
                            Handle(4, TableIndex.MethodDef),
                            Handle(1, TableIndex.Param),
                            Handle(2, TableIndex.Param),
                            Handle(4, TableIndex.CustomAttribute),
                            Handle(1, TableIndex.ClassLayout),
                            Handle(1, TableIndex.FieldRva),
                            Handle(1, TableIndex.NestedClass),
                            Handle(2, TableIndex.NestedClass)
                        ]);

                        g.VerifyIL("C.F", """
                        {
                          // Code size        6 (0x6)
                          .maxstack  1
                          IL_0000:  ldsfld     "string <PrivateImplementationDetails>#1.<S>E353667619EC664B49655FC9692165FB.s"
                          IL_0005:  ret
                        }
                        """);

                        // trailing zeros for alignment:
                        g.VerifyEncFieldRvaData($"""
                            84D89877F0D4041EFB6BF91A16F0248F2FD573E6AF05C19F96BEDB9F882F7882: {BitConverter.ToString(Encoding.UTF8.GetBytes("0123456789"))}-00-00
                            """);
                    },
                    options: new EmitDifferenceOptions() { EmitFieldRva = true })
                .Verify();
        }

        [Fact]
        public void PrivateImplDetails_DataSectionStringLiterals_StringReuse_FieldRvaSupported()
        {
            // Literals are currently only reused within generation.

            var baseString = new string('x', (1 << 23) - 100);
            var newString1 = new string('1', 40);
            var newString2 = new string('2', 80);

            using var _ = new EditAndContinueTest(targetFramework: TargetFramework.Net90, verification: Verification.Skipped)
                .AddBaseline(
                    source: $$"""
                        class C
                        {
                            void G(string a, string b, string c) {}
                        
                            void F() => G("{{baseString}}", "{{newString1}}", "");
                        }
                        """,
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("<Module>", "C");
                        g.VerifyFieldDefNames();
                        g.VerifyMethodDefNames("G", "F", ".ctor");
                    })
                .AddGeneration(
                    source: $$"""
                        class C
                        {
                            void G(string a, string b, string c) {}

                            void F() => G("{{newString2}}", "{{newString2}}", "{{newString1}}");
                        }
                        """,
                    edits:
                    [
                        Edit(SemanticEditKind.Update, symbolProvider: c => c.GetMember("C.F")),
                    ],
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("<PrivateImplementationDetails>#1", "__StaticArrayInitTypeSize=40", "<S>62CF64E173E5BF9EF5312BB6D57CC26C");
                        g.VerifyFieldDefNames("468D019EA81224AECA7EE270B11959D8A187F6F0B6A3FEBFF1C34DC1D66C8D85", "s");
                        g.VerifyMethodDefNames("F", "BytesToString", ".cctor");

                        g.VerifyEncLogDefinitions(
                        [
                            Row(3, TableIndex.TypeDef, EditAndContinueOperation.Default),
                            Row(4, TableIndex.TypeDef, EditAndContinueOperation.Default),
                            Row(5, TableIndex.TypeDef, EditAndContinueOperation.Default),
                            Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(1, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(5, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(2, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                            Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(5, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                            Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(4, TableIndex.MethodDef, EditAndContinueOperation.AddParameter),
                            Row(4, TableIndex.Param, EditAndContinueOperation.Default),
                            Row(4, TableIndex.MethodDef, EditAndContinueOperation.AddParameter),
                            Row(5, TableIndex.Param, EditAndContinueOperation.Default),
                            Row(4, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(1, TableIndex.ClassLayout, EditAndContinueOperation.Default),
                            Row(1, TableIndex.FieldRva, EditAndContinueOperation.Default),
                            Row(1, TableIndex.NestedClass, EditAndContinueOperation.Default),
                            Row(2, TableIndex.NestedClass, EditAndContinueOperation.Default)
                        ]);

                        g.VerifyEncMapDefinitions(
                        [
                            Handle(3, TableIndex.TypeDef),
                            Handle(4, TableIndex.TypeDef),
                            Handle(5, TableIndex.TypeDef),
                            Handle(1, TableIndex.Field),
                            Handle(2, TableIndex.Field),
                            Handle(2, TableIndex.MethodDef),
                            Handle(4, TableIndex.MethodDef),
                            Handle(5, TableIndex.MethodDef),
                            Handle(4, TableIndex.Param),
                            Handle(5, TableIndex.Param),
                            Handle(4, TableIndex.CustomAttribute),
                            Handle(1, TableIndex.ClassLayout),
                            Handle(1, TableIndex.FieldRva),
                            Handle(1, TableIndex.NestedClass),
                            Handle(2, TableIndex.NestedClass)
                        ]);

                        g.VerifyIL("C.F", """
                        {
                          // Code size       23 (0x17)
                          .maxstack  4
                          IL_0000:  ldarg.0
                          IL_0001:  ldstr      "22222222222222222222222222222222222222222222222222222222222222222222222222222222"
                          IL_0006:  ldstr      "22222222222222222222222222222222222222222222222222222222222222222222222222222222"
                          IL_000b:  ldsfld     "string <PrivateImplementationDetails>#1.<S>62CF64E173E5BF9EF5312BB6D57CC26C.s"
                          IL_0010:  call       "void C.G(string, string, string)"
                          IL_0015:  nop
                          IL_0016:  ret
                        }
                        """);
                    },
                    options: new EmitDifferenceOptions() { EmitFieldRva = true })
                .Verify();
        }

        [Fact]
        public void PrivateImplDetails_DataSectionStringLiterals_HeapOverflow_FieldRvaNotSupported()
        {
            // The max number of bytes that can fit into #US the heap is 2^29 - 1,
            // but each string also needs to have an offset < 0x1000000 (2^24) to be addressable by a token.
            // If the string is larger than that the next string can't be emitted.
            var baseString = new string('x', 1 << 23);

            using var _ = new EditAndContinueTest(targetFramework: TargetFramework.Net90, verification: Verification.Skipped)
                .AddBaseline(
                    source: $$"""
                        class C
                        {
                            string F() => "{{baseString}}";
                        }
                        """,
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("<Module>", "C");
                        g.VerifyFieldDefNames();
                        g.VerifyMethodDefNames("F", ".ctor");
                    })
                .AddGeneration(
                    source: """
                        class C
                        {
                            string F() => "new string that doesn't fit";
                        }
                        """,
                    edits:
                    [
                        Edit(SemanticEditKind.Update, symbolProvider: c => c.GetMember("C.F")),
                    ],
                    expectedErrors:
                    [
                        // (3,19): error CS9307: Combined length of user strings used by the program exceeds allowed limit. Adding a string literal requires restarting the application.
                        //     string F() => "new string that doesn't fit";
                        Diagnostic(ErrorCode.ERR_TooManyUserStrings_RestartRequired, @"""new string that doesn't fit""").WithLocation(3, 19)
                    ])
                .Verify();
        }

        [Theory]
        [InlineData("ComputeStringHash", "string")]
        [InlineData("ComputeSpanHash", "Span<char>")]
        [InlineData("ComputeReadOnlySpanHash", "ReadOnlySpan<char>")]
        public void PrivateImplDetails_ComputeStringHash(string hashMethodName, string typeName)
        {
            using var _ = new EditAndContinueTest(targetFramework: TargetFramework.Net80, verification: Verification.Skipped)
                .AddBaseline(
                    source: $$"""
                        using System;
                        class C
                        {
                           static int F({{typeName}} s)
                               => s switch 
                               {
                                   "A_______" => 1,
                                   "_B______" => 2,
                                   "__C_____" => 3,
                                   "___D____" => 4,
                                   "____E___" => 5,
                                   "_____F__" => 6,
                                   "______G_" => 7,
                                   "_______H" => 8,
                                   _ => 9
                               };
                        }
                        """,
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("<Module>", "C", "<PrivateImplementationDetails>");
                        g.VerifyMethodDefNames("F", ".ctor", hashMethodName);
                    })
                .AddGeneration(
                    source: $$"""
                        using System;
                        class C
                        {
                            static int F({{typeName}} s)
                               => s switch 
                               {
                                   "A_______" => 10,
                                   "_B______" => 20,
                                   "__C_____" => 30,
                                   "___D____" => 40,
                                   "____E___" => 50,
                                   "_____F__" => 60,
                                   "______G_" => 70,
                                   "_______H" => 80,
                                   _ => 90
                               };
                        }
                        """,
                    edits: new[]
                    {
                        Edit(SemanticEditKind.Update, symbolProvider: c => c.GetMember("C.F")),
                    },
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("<PrivateImplementationDetails>#1");
                        g.VerifyMethodDefNames("F", hashMethodName);

                        g.VerifyEncLogDefinitions(new[]
                        {
                            Row(3, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                            Row(4, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                            Row(4, TableIndex.TypeDef, EditAndContinueOperation.Default),
                            Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                            Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(1, TableIndex.Param, EditAndContinueOperation.Default),
                            Row(4, TableIndex.MethodDef, EditAndContinueOperation.AddParameter),
                            Row(3, TableIndex.Param, EditAndContinueOperation.Default),
                            Row(5, TableIndex.CustomAttribute, EditAndContinueOperation.Default)
                        });

                        g.VerifyEncMapDefinitions(new[]
                        {
                            Handle(4, TableIndex.TypeDef),
                            Handle(1, TableIndex.MethodDef),
                            Handle(4, TableIndex.MethodDef),
                            Handle(1, TableIndex.Param),
                            Handle(3, TableIndex.Param),
                            Handle(5, TableIndex.CustomAttribute),
                            Handle(3, TableIndex.StandAloneSig),
                            Handle(4, TableIndex.StandAloneSig)
                        });
                    })
                .Verify();
        }

        [Fact]
        public void PrivateImplDetails_ThrowSwitchExpressionException()
        {
            using var _ = new EditAndContinueTest(targetFramework: TargetFramework.Net80, verification: Verification.Skipped)
                .AddBaseline(
                    source: $$"""
                        class C
                        {
                            static int F(bool b) => b switch { true => 1 };
                        }
                        """,
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("<Module>", "C", "<PrivateImplementationDetails>");
                        g.VerifyMethodDefNames("F", ".ctor", "ThrowSwitchExpressionException");
                    })

                .AddGeneration(
                    source: """
                        class C
                        {
                            static int F(bool b) => b switch { true => 2 };
                        }
                        """,
                    edits: new[]
                    {
                        Edit(SemanticEditKind.Update, symbolProvider: c => c.GetMember("C.F")),
                    },
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("<PrivateImplementationDetails>#1");
                        g.VerifyMethodDefNames("F", "ThrowSwitchExpressionException");

                        g.VerifyEncLogDefinitions(new[]
                        {
                            Row(2, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                            Row(4, TableIndex.TypeDef, EditAndContinueOperation.Default),
                            Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                            Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(1, TableIndex.Param, EditAndContinueOperation.Default),
                            Row(4, TableIndex.MethodDef, EditAndContinueOperation.AddParameter),
                            Row(3, TableIndex.Param, EditAndContinueOperation.Default),
                            Row(5, TableIndex.CustomAttribute, EditAndContinueOperation.Default)
                        });

                        g.VerifyEncMapDefinitions(new[]
                        {
                            Handle(4, TableIndex.TypeDef),
                            Handle(1, TableIndex.MethodDef),
                            Handle(4, TableIndex.MethodDef),
                            Handle(1, TableIndex.Param),
                            Handle(3, TableIndex.Param),
                            Handle(5, TableIndex.CustomAttribute),
                            Handle(2, TableIndex.StandAloneSig)
                        });
                    })
                .Verify();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        [WorkItem("https://github.com/dotnet/roslyn/issues/69398")]
        public void PrivateImplDetails_InlineArray()
        {
            using var _ = new EditAndContinueTest(targetFramework: TargetFramework.Net80)
                .AddBaseline(
                    source: $$"""
                        using System.Runtime.CompilerServices;

                        [InlineArray(2)]
                        public struct Buffer
                        {
                            private int _element0;
                        }

                        class C
                        {
                            static void F()
                            {
                                var b = new Buffer();
                                b[0] = 1;
                            }
                        }
                        """,
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("<Module>", "Buffer", "C", "<PrivateImplementationDetails>");
                        g.VerifyMethodDefNames("F", ".ctor", "InlineArrayFirstElementRef");
                    })

                .AddGeneration(
                    source: """
                        using System.Runtime.CompilerServices;
                        
                        [InlineArray(2)]
                        public struct Buffer
                        {
                            private int _element0;
                        }
                        
                        class C
                        {
                            static void F()
                            {
                                var b = new Buffer();
                                b[0] = 1;
                                b[1] = 2;
                            }
                        }
                        """,
                    edits: new[]
                    {
                        Edit(SemanticEditKind.Update, symbolProvider: c => c.GetMember("C.F")),
                    },
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("<PrivateImplementationDetails>#1");
                        g.VerifyMethodDefNames("F", "InlineArrayElementRef", "InlineArrayFirstElementRef");

                        g.VerifyEncLogDefinitions(new[]
                        {
                            Row(2, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                            Row(5, TableIndex.TypeDef, EditAndContinueOperation.Default),
                            Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(5, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                            Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(5, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                            Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(4, TableIndex.MethodDef, EditAndContinueOperation.AddParameter),
                            Row(2, TableIndex.Param, EditAndContinueOperation.Default),
                            Row(4, TableIndex.MethodDef, EditAndContinueOperation.AddParameter),
                            Row(3, TableIndex.Param, EditAndContinueOperation.Default),
                            Row(5, TableIndex.MethodDef, EditAndContinueOperation.AddParameter),
                            Row(4, TableIndex.Param, EditAndContinueOperation.Default),
                            Row(6, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(3, TableIndex.GenericParam, EditAndContinueOperation.Default),
                            Row(4, TableIndex.GenericParam, EditAndContinueOperation.Default),
                            Row(5, TableIndex.GenericParam, EditAndContinueOperation.Default),
                            Row(6, TableIndex.GenericParam, EditAndContinueOperation.Default)
                        });

                        g.VerifyEncMapDefinitions(new[]
                        {
                            Handle(5, TableIndex.TypeDef),
                            Handle(1, TableIndex.MethodDef),
                            Handle(4, TableIndex.MethodDef),
                            Handle(5, TableIndex.MethodDef),
                            Handle(2, TableIndex.Param),
                            Handle(3, TableIndex.Param),
                            Handle(4, TableIndex.Param),
                            Handle(6, TableIndex.CustomAttribute),
                            Handle(2, TableIndex.StandAloneSig),
                            Handle(3, TableIndex.GenericParam),
                            Handle(4, TableIndex.GenericParam),
                            Handle(5, TableIndex.GenericParam),
                            Handle(6, TableIndex.GenericParam)
                        });
                    })
                .Verify();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        [WorkItem("https://github.com/dotnet/roslyn/issues/69398")]
        public void PrivateImplDetails_CollectionExpressions_InlineArrays()
        {
            var commonSource = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;

                namespace System.Runtime.CompilerServices
                {
                    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
                    public sealed class CollectionBuilderAttribute : Attribute
                    {
                        public CollectionBuilderAttribute(Type builderType, string methodName) { }
                    }
                }

                [CollectionBuilder(typeof(MyCollectionBuilder), nameof(MyCollectionBuilder.Create))]
                public struct MyCollection<T> : IEnumerable<T>
                {
                    private readonly List<T> _list;
                    public MyCollection(List<T> list) { _list = list; }
                    public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();
                    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                }
                public class MyCollectionBuilder
                {
                    public static MyCollection<T> Create<T>(ReadOnlySpan<T> items)
                    {
                        return new MyCollection<T>(new List<T>(items.ToArray()));
                    }
                }
                """;

            using var _ = new EditAndContinueTest(targetFramework: TargetFramework.Net80, verification: Verification.Skipped)
                .AddBaseline(
                    source: commonSource + """
                        class C
                        {
                            static MyCollection<object> F() => [0, 1, 2];
                        }
                        """,
                    validator: g =>
                    {
                        g.VerifyTypeDefNames(
                            "<Module>",
                            "<>y__InlineArray3`1",
                            "MyCollection`1",
                            "MyCollectionBuilder",
                            "C",
                            "CollectionBuilderAttribute",
                            "<PrivateImplementationDetails>");

                        g.VerifyMethodDefNames(
                            ".ctor",
                            "GetEnumerator",
                            "System.Collections.IEnumerable.GetEnumerator",
                            "Create",
                            ".ctor",
                            "F",
                            ".ctor",
                            ".ctor",
                            "InlineArrayAsReadOnlySpan",
                            "InlineArrayElementRef");
                    })

                .AddGeneration(
                    commonSource + """
                        class C
                        {
                            static MyCollection<object> F() => [0, 1, 2, 3];
                        }
                        """,
                    edits: new[]
                    {
                        Edit(SemanticEditKind.Update, symbolProvider: c => c.GetMember("C.F")),
                    },
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("<>y__InlineArray4#1`1", "<PrivateImplementationDetails>#1");
                        g.VerifyMethodDefNames("F", "InlineArrayAsReadOnlySpan", "InlineArrayElementRef");

                        g.VerifyEncLogDefinitions(new[]
                        {
                            Row(3, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                            Row(8, TableIndex.TypeDef, EditAndContinueOperation.Default),
                            Row(9, TableIndex.TypeDef, EditAndContinueOperation.Default),
                            Row(8, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(3, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(6, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(9, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                            Row(11, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(9, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                            Row(12, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(11, TableIndex.MethodDef, EditAndContinueOperation.AddParameter),
                            Row(9, TableIndex.Param, EditAndContinueOperation.Default),
                            Row(11, TableIndex.MethodDef, EditAndContinueOperation.AddParameter),
                            Row(10, TableIndex.Param, EditAndContinueOperation.Default),
                            Row(12, TableIndex.MethodDef, EditAndContinueOperation.AddParameter),
                            Row(11, TableIndex.Param, EditAndContinueOperation.Default),
                            Row(12, TableIndex.MethodDef, EditAndContinueOperation.AddParameter),
                            Row(12, TableIndex.Param, EditAndContinueOperation.Default),
                            Row(10, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(11, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(12, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(13, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(8, TableIndex.GenericParam, EditAndContinueOperation.Default),
                            Row(9, TableIndex.GenericParam, EditAndContinueOperation.Default),
                            Row(10, TableIndex.GenericParam, EditAndContinueOperation.Default),
                            Row(11, TableIndex.GenericParam, EditAndContinueOperation.Default),
                            Row(12, TableIndex.GenericParam, EditAndContinueOperation.Default)
                        });

                        g.VerifyEncMapDefinitions(new[]
                        {
                            Handle(8, TableIndex.TypeDef),
                            Handle(9, TableIndex.TypeDef),
                            Handle(3, TableIndex.Field),
                            Handle(6, TableIndex.MethodDef),
                            Handle(11, TableIndex.MethodDef),
                            Handle(12, TableIndex.MethodDef),
                            Handle(9, TableIndex.Param),
                            Handle(10, TableIndex.Param),
                            Handle(11, TableIndex.Param),
                            Handle(12, TableIndex.Param),
                            Handle(10, TableIndex.CustomAttribute),
                            Handle(11, TableIndex.CustomAttribute),
                            Handle(12, TableIndex.CustomAttribute),
                            Handle(13, TableIndex.CustomAttribute),
                            Handle(3, TableIndex.StandAloneSig),
                            Handle(8, TableIndex.GenericParam),
                            Handle(9, TableIndex.GenericParam),
                            Handle(10, TableIndex.GenericParam),
                            Handle(11, TableIndex.GenericParam),
                            Handle(12, TableIndex.GenericParam)
                        });
                    })
                .AddGeneration(
                    commonSource + """
                        class C
                        {
                            static MyCollection<object> F() => [0, 10, 20, 30];
                        }
                        """,
                    edits: new[]
                    {
                        Edit(SemanticEditKind.Update, symbolProvider: c => c.GetMember("C.F")),
                    },
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("<>y__InlineArray4#2`1", "<PrivateImplementationDetails>#2");
                        g.VerifyMethodDefNames("F", "InlineArrayAsReadOnlySpan", "InlineArrayElementRef");

                        g.VerifyEncLogDefinitions(new[]
                        {
                            Row(4, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                            Row(10, TableIndex.TypeDef, EditAndContinueOperation.Default),
                            Row(11, TableIndex.TypeDef, EditAndContinueOperation.Default),
                            Row(10, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(4, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(6, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(11, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                            Row(13, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(11, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                            Row(14, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(13, TableIndex.MethodDef, EditAndContinueOperation.AddParameter),
                            Row(13, TableIndex.Param, EditAndContinueOperation.Default),
                            Row(13, TableIndex.MethodDef, EditAndContinueOperation.AddParameter),
                            Row(14, TableIndex.Param, EditAndContinueOperation.Default),
                            Row(14, TableIndex.MethodDef, EditAndContinueOperation.AddParameter),
                            Row(15, TableIndex.Param, EditAndContinueOperation.Default),
                            Row(14, TableIndex.MethodDef, EditAndContinueOperation.AddParameter),
                            Row(16, TableIndex.Param, EditAndContinueOperation.Default),
                            Row(14, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(15, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(16, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(17, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(13, TableIndex.GenericParam, EditAndContinueOperation.Default),
                            Row(14, TableIndex.GenericParam, EditAndContinueOperation.Default),
                            Row(15, TableIndex.GenericParam, EditAndContinueOperation.Default),
                            Row(16, TableIndex.GenericParam, EditAndContinueOperation.Default),
                            Row(17, TableIndex.GenericParam, EditAndContinueOperation.Default)
                        });

                        g.VerifyEncMapDefinitions(new[]
                        {
                            Handle(10, TableIndex.TypeDef),
                            Handle(11, TableIndex.TypeDef),
                            Handle(4, TableIndex.Field),
                            Handle(6, TableIndex.MethodDef),
                            Handle(13, TableIndex.MethodDef),
                            Handle(14, TableIndex.MethodDef),
                            Handle(13, TableIndex.Param),
                            Handle(14, TableIndex.Param),
                            Handle(15, TableIndex.Param),
                            Handle(16, TableIndex.Param),
                            Handle(14, TableIndex.CustomAttribute),
                            Handle(15, TableIndex.CustomAttribute),
                            Handle(16, TableIndex.CustomAttribute),
                            Handle(17, TableIndex.CustomAttribute),
                            Handle(4, TableIndex.StandAloneSig),
                            Handle(13, TableIndex.GenericParam),
                            Handle(14, TableIndex.GenericParam),
                            Handle(15, TableIndex.GenericParam),
                            Handle(16, TableIndex.GenericParam),
                            Handle(17, TableIndex.GenericParam)
                        });
                    })
                .Verify();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void PrivateImplDetails_CollectionExpressions_ReadOnlyListTypes()
        {
            using var _ = new EditAndContinueTest(targetFramework: TargetFramework.Net80, verification: Verification.Skipped)
                .AddBaseline(
                    source: """
                        using System.Collections.Generic;
                        class C
                        {
                            static IEnumerable<int> F(int x, int y, IEnumerable<int> e) => [x, y];
                        }
                        """,
                    validator: g =>
                    {
                        g.VerifyTypeDefNames(
                            "<Module>",
                            "<>z__ReadOnlyArray`1",
                            "C");

                        g.VerifyMethodDefNames(
                            ".ctor",
                            "System.Collections.IEnumerable.GetEnumerator",
                            "System.Collections.ICollection.get_Count",
                            "System.Collections.ICollection.get_IsSynchronized",
                            "System.Collections.ICollection.get_SyncRoot",
                            "System.Collections.ICollection.CopyTo",
                            "System.Collections.IList.get_Item",
                            "System.Collections.IList.set_Item",
                            "System.Collections.IList.get_IsFixedSize",
                            "System.Collections.IList.get_IsReadOnly",
                            "System.Collections.IList.Add",
                            "System.Collections.IList.Clear",
                            "System.Collections.IList.Contains",
                            "System.Collections.IList.IndexOf",
                            "System.Collections.IList.Insert",
                            "System.Collections.IList.Remove",
                            "System.Collections.IList.RemoveAt",
                            "System.Collections.Generic.IEnumerable<T>.GetEnumerator",
                            "System.Collections.Generic.IReadOnlyCollection<T>.get_Count",
                            "System.Collections.Generic.IReadOnlyList<T>.get_Item",
                            "System.Collections.Generic.ICollection<T>.get_Count",
                            "System.Collections.Generic.ICollection<T>.get_IsReadOnly",
                            "System.Collections.Generic.ICollection<T>.Add",
                            "System.Collections.Generic.ICollection<T>.Clear",
                            "System.Collections.Generic.ICollection<T>.Contains",
                            "System.Collections.Generic.ICollection<T>.CopyTo",
                            "System.Collections.Generic.ICollection<T>.Remove",
                            "System.Collections.Generic.IList<T>.get_Item",
                            "System.Collections.Generic.IList<T>.set_Item",
                            "System.Collections.Generic.IList<T>.IndexOf",
                            "System.Collections.Generic.IList<T>.Insert",
                            "System.Collections.Generic.IList<T>.RemoveAt",
                            "F",
                            ".ctor"
                            );
                    })

                .AddGeneration(
                    """
                    using System.Collections.Generic;
                    class C
                    {
                        static IEnumerable<int> F(int x, int y, IEnumerable<int> e) => [x, y, default];
                    }
                    """,
                    edits: new[]
                    {
                        Edit(SemanticEditKind.Update, symbolProvider: c => c.GetMember("C.F")),
                    },
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("<>z__ReadOnlyArray#1`1");
                        g.VerifyMethodDefNames(
                            "F",
                            ".ctor",
                            "System.Collections.IEnumerable.GetEnumerator",
                            "System.Collections.ICollection.get_Count",
                            "System.Collections.ICollection.get_IsSynchronized",
                            "System.Collections.ICollection.get_SyncRoot",
                            "System.Collections.ICollection.CopyTo",
                            "System.Collections.IList.get_Item",
                            "System.Collections.IList.set_Item",
                            "System.Collections.IList.get_IsFixedSize",
                            "System.Collections.IList.get_IsReadOnly",
                            "System.Collections.IList.Add",
                            "System.Collections.IList.Clear",
                            "System.Collections.IList.Contains",
                            "System.Collections.IList.IndexOf",
                            "System.Collections.IList.Insert",
                            "System.Collections.IList.Remove",
                            "System.Collections.IList.RemoveAt",
                            "System.Collections.Generic.IEnumerable<T>.GetEnumerator",
                            "System.Collections.Generic.IReadOnlyCollection<T>.get_Count",
                            "System.Collections.Generic.IReadOnlyList<T>.get_Item",
                            "System.Collections.Generic.ICollection<T>.get_Count",
                            "System.Collections.Generic.ICollection<T>.get_IsReadOnly",
                            "System.Collections.Generic.ICollection<T>.Add",
                            "System.Collections.Generic.ICollection<T>.Clear",
                            "System.Collections.Generic.ICollection<T>.Contains",
                            "System.Collections.Generic.ICollection<T>.CopyTo",
                            "System.Collections.Generic.ICollection<T>.Remove",
                            "System.Collections.Generic.IList<T>.get_Item",
                            "System.Collections.Generic.IList<T>.set_Item",
                            "System.Collections.Generic.IList<T>.IndexOf",
                            "System.Collections.Generic.IList<T>.Insert",
                            "System.Collections.Generic.IList<T>.RemoveAt");

                        // Many EncLog and EncMap entries added.
                    })
                .AddGeneration(
                    """
                    using System.Collections.Generic;
                    class C
                    {
                        static IEnumerable<int> F(int x, int y, IEnumerable<int> e) => [x, y, ..e];
                    }
                    """,
                    edits: new[]
                    {
                        Edit(SemanticEditKind.Update, symbolProvider: c => c.GetMember("C.F")),
                    },
                    validator: g =>
                    {
                        g.VerifyTypeDefNames("<>z__ReadOnlyList#2`1");
                        g.VerifyMethodDefNames(
                            "F",
                            ".ctor",
                            "System.Collections.IEnumerable.GetEnumerator",
                            "System.Collections.ICollection.get_Count",
                            "System.Collections.ICollection.get_IsSynchronized",
                            "System.Collections.ICollection.get_SyncRoot",
                            "System.Collections.ICollection.CopyTo",
                            "System.Collections.IList.get_Item",
                            "System.Collections.IList.set_Item",
                            "System.Collections.IList.get_IsFixedSize",
                            "System.Collections.IList.get_IsReadOnly",
                            "System.Collections.IList.Add",
                            "System.Collections.IList.Clear",
                            "System.Collections.IList.Contains",
                            "System.Collections.IList.IndexOf",
                            "System.Collections.IList.Insert",
                            "System.Collections.IList.Remove",
                            "System.Collections.IList.RemoveAt",
                            "System.Collections.Generic.IEnumerable<T>.GetEnumerator",
                            "System.Collections.Generic.IReadOnlyCollection<T>.get_Count",
                            "System.Collections.Generic.IReadOnlyList<T>.get_Item",
                            "System.Collections.Generic.ICollection<T>.get_Count",
                            "System.Collections.Generic.ICollection<T>.get_IsReadOnly",
                            "System.Collections.Generic.ICollection<T>.Add",
                            "System.Collections.Generic.ICollection<T>.Clear",
                            "System.Collections.Generic.ICollection<T>.Contains",
                            "System.Collections.Generic.ICollection<T>.CopyTo",
                            "System.Collections.Generic.ICollection<T>.Remove",
                            "System.Collections.Generic.IList<T>.get_Item",
                            "System.Collections.Generic.IList<T>.set_Item",
                            "System.Collections.Generic.IList<T>.IndexOf",
                            "System.Collections.Generic.IList<T>.Insert",
                            "System.Collections.Generic.IList<T>.RemoveAt");

                        // Many EncLog and EncMap entries added.
                    })
                .Verify();
        }

        [Fact]
        public void ManifestResource_Add()
        {
            using var _ = new EditAndContinueTest(targetFramework: TargetFramework.Net80, verification: Verification.Skipped)
                .AddBaseline(
                    source: """
                        class C;
                        """)
                .AddGeneration(
                    source: """
                        class C;
                        """,
                    edits: [],
                    validator: g =>
                    {
                        
                    })
                .Verify();
        }
    }
}
