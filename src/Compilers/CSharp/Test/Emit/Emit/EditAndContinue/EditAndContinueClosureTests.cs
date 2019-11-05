// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
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
    public class EditAndContinueClosureTests : EditAndContinueTestBase
    {
        [Fact]
        public void MethodToMethodWithClosure()
        {
            var source0 =
@"delegate object D();
class C
{
    static object F(object o)
    {
        return o;
    }
}";
            var source1 =
@"delegate object D();
class C
{
    static object F(object o)
    {
        return ((D)(() => o))();
    }
}";
            var compilation0 = CreateCompilation(source0, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);
            var bytes0 = compilation0.EmitToArray();
            var generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), EmptyLocalsProvider);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, compilation0.GetMember<MethodSymbol>("C.F"), compilation1.GetMember<MethodSymbol>("C.F"))));

            using (var md1 = diff1.GetMetadata())
            {
                var reader1 = md1.Reader;

                // Field 'o'
                // Methods: 'F', '.ctor', '<F>b__1'
                // Type: display class
                CheckEncLogDefinitions(reader1,
                    Row(2, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                    Row(4, TableIndex.TypeDef, EditAndContinueOperation.Default),
                    Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                    Row(1, TableIndex.Field, EditAndContinueOperation.Default),
                    Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
                    Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                    Row(7, TableIndex.MethodDef, EditAndContinueOperation.Default),
                    Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                    Row(8, TableIndex.MethodDef, EditAndContinueOperation.Default),
                    Row(4, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(1, TableIndex.NestedClass, EditAndContinueOperation.Default));
            }
        }

        [Fact]
        public void MethodWithStaticLambda1()
        {
            var source0 = MarkedSource(@"
using System;

class C
{
    void F()
    {
        Func<int> x = <N:0>() => 1</N:0>;
    }
}");
            var source1 = MarkedSource(@"
using System;

class C
{
    void F()
    {
        Func<int> x = <N:0>() => 2</N:0>;
    }
}");
            var compilation0 = CreateCompilation(source0.Tree, options: ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            var compilation1 = compilation0.WithSource(source1.Tree);

            var v0 = CompileAndVerify(compilation0);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            // no new synthesized members generated (with #1 in names):
            diff1.VerifySynthesizedMembers(
                "C: {<>c}",
                "C.<>c: {<>9__0_0, <F>b__0_0}");

            var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;

            // Method updates
            CheckEncLogDefinitions(reader1,
                Row(2, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default));
        }

        [Fact]
        public void MethodWithStaticLocalFunction1()
        {
            var source0 = MarkedSource(@"
using System;

class C
{
    void F()
    {
        <N:0>int x() => 1;</N:0>
    }
}");
            var source1 = MarkedSource(@"
using System;

class C
{
    void F()
    {
        <N:0>int x() => 2;</N:0>
    }
}");

            var compilation0 = CreateCompilation(source0.Tree, options: ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            var compilation1 = compilation0.WithSource(source1.Tree);

            var v0 = CompileAndVerify(compilation0);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            diff1.VerifySynthesizedMembers(
                "C: {<F>g__x|0_0}");

            var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;

            // Method updates
            CheckEncLogDefinitions(reader1,
                Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(3, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(5, TableIndex.CustomAttribute, EditAndContinueOperation.Default));
        }

        [Fact]
        public void MethodWithStaticLocalFunction_ChangeStatic()
        {
            var source0 = MarkedSource(@"
using System;

class C
{
    void F()
    {
        <N:0>int x() => 1;</N:0>
    }
}");
            var source1 = MarkedSource(@"
using System;

class C
{
    void F()
    {
        <N:0>static int x() => 1;</N:0>
    }
}");

            var compilation0 = CreateCompilation(source0.Tree, options: ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            var compilation1 = compilation0.WithSource(source1.Tree);

            var v0 = CompileAndVerify(compilation0);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);
            var reader0 = md0.MetadataReader;

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            diff1.VerifySynthesizedMembers(
                "C: {<F>g__x|0_0}");

            var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;

            // Method updates
            CheckEncLogDefinitions(reader1,
                Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(3, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(5, TableIndex.CustomAttribute, EditAndContinueOperation.Default));

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(testData: testData0);
            var localFunction0 = testData0.GetMethodData("C.<F>g__x|0_0").Method;
            Assert.True(((Symbol)localFunction0).IsStatic);

            var localFunction1 = diff1.TestData.GetMethodData("C.<F>g__x|0_0").Method;
            Assert.True(((Symbol)localFunction1).IsStatic);
        }

        [Fact]
        public void MethodWithStaticLambdaGeneric1()
        {
            var source0 = MarkedSource(@"
using System;

class C
{
    void F<T>()
    {
        Func<T> x = <N:0>() => default(T)</N:0>;
    }
}");
            var source1 = MarkedSource(@"
using System;

class C
{
    void F<T>()
    {
        Func<T> x = <N:0>() => default(T)</N:0>;
    }
}");
            var compilation0 = CreateCompilation(source0.Tree, options: ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            var compilation1 = compilation0.WithSource(source1.Tree);

            var v0 = CompileAndVerify(compilation0);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            // no new synthesized members generated (with #1 in names):
            diff1.VerifySynthesizedMembers(
                "C: {<>c__0}",
                "C.<>c__0<T>: {<>9__0_0, <F>b__0_0}");

            var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;

            // Method updates
            CheckEncLogDefinitions(reader1,
                Row(3, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(4, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default));
        }

        [Fact]
        public void MethodWithStaticLocalFunctionGeneric1()
        {
            var source0 = MarkedSource(@"
using System;

class C
{
    void F<T>()
    {
        <N:0>T x() => default(T);</N:0>
    }
}");
            var source1 = MarkedSource(@"
using System;

class C
{
    void F<T>()
    {
        <N:0>T x() => default(T);</N:0>
    }
}");
            var compilation0 = CreateCompilation(source0.Tree, options: ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            var compilation1 = compilation0.WithSource(source1.Tree);

            var v0 = CompileAndVerify(compilation0);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            // no new synthesized members generated (with #1 in names):
            diff1.VerifySynthesizedMembers(
                "C: {<F>g__x|0_0}");

            var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;

            // Method updates
            CheckEncLogDefinitions(reader1,
                Row(2, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(3, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(5, TableIndex.CustomAttribute, EditAndContinueOperation.Default));
        }

        [Fact]
        public void MethodWithThisOnlyClosure1()
        {
            var source0 = MarkedSource(@"
using System;

class C
{
    int F(int a)
    {
        Func<int> x = <N:0>() => F(1)</N:0>;
        return 1;
    }
}");
            var source1 = MarkedSource(@"
using System;

class C
{
    int F(int a)
    {
        Func<int> x = <N:0>() => F(2)</N:0>;
        return 2;
    }
}");
            var compilation0 = CreateCompilation(source0.Tree, options: ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            var compilation1 = compilation0.WithSource(source1.Tree);

            var v0 = CompileAndVerify(compilation0);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            // no new synthesized members generated (with #1 in names):
            diff1.VerifySynthesizedMembers(
                "C: {<F>b__0_0}");

            var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;

            // Method updates
            CheckEncLogDefinitions(reader1,
                Row(2, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(3, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(5, TableIndex.CustomAttribute, EditAndContinueOperation.Default));
        }

        [Fact]
        public void MethodWithThisOnlyLocalFunctionClosure1()
        {
            var source0 = MarkedSource(@"
using System;

class C
{
    int F(int a)
    {
        <N:0>int x() => F(1);</N:0>
        return 1;
    }
}");
            var source1 = MarkedSource(@"
using System;

class C
{
    int F(int a)
    {
        <N:0>int x() => F(2);</N:0>
        return 2;
    }
}");
            var compilation0 = CreateCompilation(source0.Tree, options: ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            var compilation1 = compilation0.WithSource(source1.Tree);

            var v0 = CompileAndVerify(compilation0);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            diff1.VerifySynthesizedMembers(
                "C: {<F>g__x|0_0}");

            var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;

            // Method updates
            CheckEncLogDefinitions(reader1,
                Row(2, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(3, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(5, TableIndex.CustomAttribute, EditAndContinueOperation.Default));
        }

        [Fact]
        public void MethodWithClosure1()
        {
            var source0 = MarkedSource(@"
using System;

class C
{
    int F(int a)
    <N:0>{</N:0>
        Func<int> x = <N:1>() => F(a + 1)</N:1>;
        return 1;
    }
}");
            var source1 = MarkedSource(@"
using System;

class C
{
    int F(int a)
    <N:0>{</N:0>
        Func<int> x = <N:1>() => F(a + 2)</N:1>;
        return 2;
    }
}");
            var compilation0 = CreateCompilation(source0.Tree, options: ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            var compilation1 = compilation0.WithSource(source1.Tree);

            var v0 = CompileAndVerify(compilation0);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            // no new synthesized members generated (with #1 in names):
            diff1.VerifySynthesizedMembers(
                "C.<>c__DisplayClass0_0: {<>4__this, a, <F>b__0}",
                "C: {<>c__DisplayClass0_0}");

            var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;

            // Method updates
            CheckEncLogDefinitions(reader1,
                Row(2, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default));
        }

        [Fact]
        public void MethodWithNullable_AddingNullCheck()
        {
            var source0 = MarkedSource(@"
using System;
#nullable enable

class C
{
    static T id<T>(T t) => t;
    static T G<T>(Func<T> f) => f();

    public void F(string? x)
    <N:0>{</N:0>
        var <N:1>y1</N:1> = new { A = id(x) };
        var <N:2>y2</N:2> = G(<N:3>() => new { B = id(x) }</N:3>);
        var <N:4>z</N:4> = G(<N:5>() => y1.A + y2.B</N:5>);
    }
}", options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview));
            var source1 = MarkedSource(@"
using System;
#nullable enable

class C
{
    static T id<T>(T t) => t;
    static T G<T>(Func<T> f) => f();

    public void F(string? x)
    <N:0>{</N:0>
        if (x is null) throw new Exception();
        var <N:1>y1</N:1> = new { A = id(x) };
        var <N:2>y2</N:2> = G(<N:3>() => new { B = id(x) }</N:3>);
        var <N:4>z</N:4> = G(<N:5>() => y1.A + y2.B</N:5>);
    }
}", options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview));
            var compilation0 = CreateCompilation(source0.Tree, options: ComSafeDebugDll);

            var compilation1 = compilation0.WithSource(source1.Tree);

            var v0 = CompileAndVerify(compilation0);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            diff1.VerifySynthesizedMembers(
                "Microsoft.CodeAnalysis: {EmbeddedAttribute}",
                "Microsoft: {CodeAnalysis}",
                "System.Runtime: {CompilerServices, CompilerServices}",
                "<global namespace>: {Microsoft, System, System}",
                "C: {<>c__DisplayClass2_0}",
                "System: {Runtime, Runtime}",
                "C.<>c__DisplayClass2_0: {x, y1, y2, <F>b__0, <F>b__1}",
                "<>f__AnonymousType1<<B>j__TPar>: {Equals, GetHashCode, ToString}",
                "System.Runtime.CompilerServices: {NullableAttribute, NullableContextAttribute}",
                "<>f__AnonymousType0<<A>j__TPar>: {Equals, GetHashCode, ToString}");

            diff1.VerifyIL("C.<>c__DisplayClass2_0.<F>b__1()", @"
{
  // Code size       28 (0x1c)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""<anonymous type: string A> C.<>c__DisplayClass2_0.y1""
  IL_0006:  callvirt   ""string <>f__AnonymousType0<string>.A.get""
  IL_000b:  ldarg.0
  IL_000c:  ldfld      ""<anonymous type: string B> C.<>c__DisplayClass2_0.y2""
  IL_0011:  callvirt   ""string <>f__AnonymousType1<string>.B.get""
  IL_0016:  call       ""string string.Concat(string, string)""
  IL_001b:  ret
}");

            diff1.VerifyIL("C.<>c__DisplayClass2_0.<F>b__0()", @"
{
  // Code size       17 (0x11)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""string C.<>c__DisplayClass2_0.x""
  IL_0006:  call       ""string C.id<string>(string)""
  IL_000b:  newobj     ""<>f__AnonymousType1<string>..ctor(string)""
  IL_0010:  ret
}");
        }

        [Fact]
        public void MethodWithLocalFunctionClosure1()
        {
            var source0 = MarkedSource(@"
using System;

class C
{
    int F(int a)
    <N:0>{</N:0>
        <N:1>int x() => F(a + 1);</N:1>
        return 1;
    }
}");
            var source1 = MarkedSource(@"
using System;

class C
{
    int F(int a)
    <N:0>{</N:0>
        <N:1>int x() => F(a + 2);</N:1>
        return 2;
    }
}");
            var compilation0 = CreateCompilation(source0.Tree, options: ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            var compilation1 = compilation0.WithSource(source1.Tree);

            var v0 = CompileAndVerify(compilation0);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            diff1.VerifySynthesizedMembers(
                "C: {<F>g__x|0_0, <>c__DisplayClass0_0}",
                "C.<>c__DisplayClass0_0: {<>4__this, a}");

            var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;

            // Method updates
            CheckEncLogDefinitions(reader1,
                Row(2, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(3, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(6, TableIndex.CustomAttribute, EditAndContinueOperation.Default));
        }

        [Fact]
        public void ConstructorWithClosure1()
        {
            var source0 = MarkedSource(@"
using System;

class D { public D(Func<int> f) { } } 

class C : D
{
    <N:0>public C(int a, int b)</N:0>
      : base(<N:8>() => a</N:8>) 
    <N:1>{</N:1>
        int c = 0;

        Func<int> f1 = <N:2>() => a + 1</N:2>;        
        Func<int> f2 = <N:3>() => b + 2</N:3>;
        Func<int> f3 = <N:4>() => c + 3</N:4>;
        Func<int> f4 = <N:5>() => a + b + c</N:5>;
        Func<int> f5 = <N:6>() => a + c</N:6>;
        Func<int> f6 = <N:7>() => b + c</N:7>;
    }
}");
            var source1 = MarkedSource(@"
using System;

class D { public D(Func<int> f) { } } 

class C : D
{
    <N:0>public C(int a, int b)</N:0>
      : base(<N:8>() => a * 10</N:8>) 
    <N:1>{</N:1>
        int c = 0;

        Func<int> f1 = <N:2>() => a * 10 + 1</N:2>;        
        Func<int> f2 = <N:3>() => b * 10 + 2</N:3>;
        Func<int> f3 = <N:4>() => c * 10 + 3</N:4>;
        Func<int> f4 = <N:5>() => a * 10 + b + c</N:5>;
        Func<int> f5 = <N:6>() => a * 10 + c</N:6>;
        Func<int> f6 = <N:7>() => b * 10 + c</N:7>;
    }
}");
            var compilation0 = CreateCompilation(source0.Tree, options: ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            var compilation1 = compilation0.WithSource(source1.Tree);

            var v0 = CompileAndVerify(compilation0);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var ctor0 = compilation0.GetMember<NamedTypeSymbol>("C").InstanceConstructors.Single();
            var ctor1 = compilation1.GetMember<NamedTypeSymbol>("C").InstanceConstructors.Single();

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, ctor0, ctor1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            // no new synthesized members generated (with #1 in names):
            diff1.VerifySynthesizedMembers(
                "C: {<>c__DisplayClass0_0, <>c__DisplayClass0_1}",
                "C.<>c__DisplayClass0_0: {a, b, <.ctor>b__0, <.ctor>b__1, <.ctor>b__2}",
                "C.<>c__DisplayClass0_1: {c, CS$<>8__locals1, <.ctor>b__3, <.ctor>b__4, <.ctor>b__5, <.ctor>b__6}");

            var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;

            // Method updates
            CheckEncLogDefinitions(reader1,
                Row(2, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(6, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(8, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(9, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(10, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(11, TableIndex.MethodDef, EditAndContinueOperation.Default));
        }

        [Fact]
        public void PartialClass()
        {
            var source0 = MarkedSource(@"
using System;

partial class C
{
    Func<int> m1 = <N:0>() => 1</N:0>;
}

partial class C
{
    Func<int> m2 = <N:1>() => 1</N:1>;
}
");
            var source1 = MarkedSource(@"
using System;

partial class C
{
    Func<int> m1 = <N:0>() => 10</N:0>;
}

partial class C
{
    Func<int> m2 = <N:1>() => 10</N:1>;
}");
            var compilation0 = CreateCompilation(source0.Tree, options: ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            var compilation1 = compilation0.WithSource(source1.Tree);

            var v0 = CompileAndVerify(compilation0);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var ctor0 = compilation0.GetMember<NamedTypeSymbol>("C").InstanceConstructors.Single();
            var ctor1 = compilation1.GetMember<NamedTypeSymbol>("C").InstanceConstructors.Single();

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, ctor0, ctor1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            // no new synthesized members generated (with #1 in names):
            diff1.VerifySynthesizedMembers(
                "C: {<>c}",
                "C.<>c: {<>9__2_0, <>9__2_1, <.ctor>b__2_0, <.ctor>b__2_1}");

            var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;

            // Method updates
            CheckEncLogDefinitions(reader1,
                Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default));
        }

        [Fact]
        public void JoinAndGroupByClauses()
        {
            var source0 = MarkedSource(@"
using System.Linq;

class C
{
    void F()
    {
        var result = <N:0>from a in new[] { 1, 2, 3 }</N:0>
                     <N:1>join b in new[] { 5 } on a + 1 equals b - 1</N:1>
                     <N:2>group</N:2> new { a, b = a + 5 } by new { c = a + 4 } into d
                     <N:3>select d.Key</N:3>;
    }
}");
            var source1 = MarkedSource(@"
using System.Linq;

class C
{
    void F()
    {
        var result = <N:0>from a in new[] { 10, 20, 30 }</N:0>
                     <N:1>join b in new[] { 50 } on a + 10 equals b - 10</N:1>
                     <N:2>group</N:2> new { a, b = a + 50 } by new { c = a + 40 } into d
                     <N:3>select d.Key</N:3>;
    }
}");
            var compilation0 = CreateCompilation(source0.Tree, options: ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            var compilation1 = compilation0.WithSource(source1.Tree);

            var v0 = CompileAndVerify(compilation0);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            // no new synthesized members generated (with #1 in names):
            diff1.VerifySynthesizedMembers(
                "C: {<>c}",
                "C.<>c: {<>9__0_0, <>9__0_1, <>9__0_2, <>9__0_3, <>9__0_4, <>9__0_5, <F>b__0_0, <F>b__0_1, <F>b__0_2, <F>b__0_3, <F>b__0_4, <F>b__0_5}",
                "<>f__AnonymousType1<<c>j__TPar>: {Equals, GetHashCode, ToString}",
                "<>f__AnonymousType0<<a>j__TPar, <b>j__TPar>: {Equals, GetHashCode, ToString}");

            var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;

            // Method updates for lambdas:
            CheckEncLogDefinitions(reader1,
                Row(6, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(12, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(16, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(17, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(18, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(19, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(20, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(21, TableIndex.MethodDef, EditAndContinueOperation.Default));
        }

        [Fact]
        public void TransparentIdentifiers_FromClause()
        {
            var source0 = MarkedSource(@"
using System;
using System.Linq;

class C
{
	int Z(Func<int> f)
	{
		return 1;
	}

    void F()
    {
		var <N:10>result = <N:0>from a in new[] { 1 }</N:0>
		                   <N:1>from b in <N:9>new[] { 1 }</N:9></N:1>
		                   <N:2>where <N:7>Z(<N:5>() => a</N:5>) > 0</N:7></N:2>
		                   <N:3>where <N:8>Z(<N:6>() => b</N:6>) > 0</N:8></N:3>
		                   <N:4>select a</N:4></N:10>;
    }
}");

            var source1 = MarkedSource(@"
using System;
using System.Linq;

class C
{
	int Z(Func<int> f)
	{
		return 1;
	}

    void F()
    {
		var <N:10>result = <N:0>from a in new[] { 1 }</N:0>
		                   <N:1>from b in <N:9>new[] { 2 }</N:9></N:1>
		                   <N:2>where <N:7>Z(<N:5>() => a</N:5>) > 1</N:7></N:2>
		                   <N:3>where <N:8>Z(<N:6>() => b</N:6>) > 2</N:8></N:3>
		                   <N:4>select a</N:4></N:10>;
    }
}");
            var compilation0 = CreateCompilation(source0.Tree, options: ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            var compilation1 = compilation0.WithSource(source1.Tree);

            var v0 = CompileAndVerify(compilation0);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            // no new synthesized members generated (with #1 in names):
            diff1.VerifySynthesizedMembers(
                "C.<>c__DisplayClass1_1: {<>h__TransparentIdentifier0, <F>b__6}",
                "C.<>c__DisplayClass1_0: {<>h__TransparentIdentifier0, <F>b__5}",
                "C.<>c: {<>9__1_0, <>9__1_1, <>9__1_4, <F>b__1_0, <F>b__1_1, <F>b__1_4}",
                "C: {<F>b__1_2, <F>b__1_3, <>c__DisplayClass1_0, <>c__DisplayClass1_1, <>c}",
                "<>f__AnonymousType0<<a>j__TPar, <b>j__TPar>: {Equals, GetHashCode, ToString}");

            var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;

            // Method updates for lambdas:
            CheckEncLogDefinitions(reader1,
                Row(7, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(8, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(9, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(8, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(10, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(11, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(13, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(15, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(18, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(19, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(20, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(17, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(18, TableIndex.CustomAttribute, EditAndContinueOperation.Default));
        }

        [Fact]
        public void TransparentIdentifiers_LetClause()
        {
            var source0 = MarkedSource(@"
using System;
using System.Linq;

class C
{
	int Z(Func<int> f)
	{
		return 1;
	}

    void F()
    {
		var result = <N:0>from a in new[] { 1 }</N:0>
		             <N:1>let b = <N:2>Z(<N:3>() => a</N:3>)</N:2></N:1>
		             <N:4>select <N:5>a + b</N:5></N:4>;
    }
}");

            var source1 = MarkedSource(@"
using System;
using System.Linq;

class C
{
	int Z(Func<int> f)
	{
		return 1;
	}

    void F()
    {
		var result = <N:0>from a in new[] { 1 }</N:0>
		             <N:1>let b = <N:2>Z(<N:3>() => a</N:3>) + 1</N:2></N:1>
		             <N:4>select <N:5>a + b</N:5></N:4>;
    }
}");
            var compilation0 = CreateCompilation(source0.Tree, options: ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            var compilation1 = compilation0.WithSource(source1.Tree);

            var v0 = CompileAndVerify(compilation0);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            // no new synthesized members generated (with #1 in names):
            diff1.VerifySynthesizedMembers(
                "C.<>c: {<>9__1_1, <F>b__1_1}",
                "<>f__AnonymousType0<<a>j__TPar, <b>j__TPar>: {Equals, GetHashCode, ToString}",
                "C.<>c__DisplayClass1_0: {a, <F>b__2}",
                "C: {<F>b__1_0, <>c__DisplayClass1_0, <>c}");

            var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;

            // Method updates for lambdas:
            CheckEncLogDefinitions(reader1,
                Row(6, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(7, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(8, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(10, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(12, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(15, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(15, TableIndex.CustomAttribute, EditAndContinueOperation.Default));
        }

        [Fact]
        public void TransparentIdentifiers_JoinClause()
        {
            var source0 = MarkedSource(@"
using System;
using System.Linq;

class C
{
    int Z(Func<int> f)
    {
        return 1;
    }

    public void F()
    {
        var result = <N:0>from a in <N:1>new[] { 1 }</N:1></N:0>
                     <N:2>join b in new[] { 3 } on 
                          <N:3>Z(<N:4>() => <N:5>a + 1</N:5></N:4>)</N:3> 
                          equals 
                          <N:6>Z(<N:7>() => <N:8>b - 1</N:8></N:7>)</N:6></N:2>
                     <N:9>select <N:10>Z(<N:11>() => <N:12>a + b</N:12></N:11>)</N:10></N:9>;
    }
}");

            var source1 = MarkedSource(@"
using System;
using System.Linq;

class C
{
    int Z(Func<int> f)
    {
        return 1;
    }

    public void F()
    {
        var result = <N:0>from a in <N:1>new[] { 1 }</N:1></N:0>
                     <N:2>join b in new[] { 3 } on 
                          <N:3>Z(<N:4>() => <N:5>a + 1</N:5></N:4>)</N:3> 
                          equals 
                          <N:6>Z(<N:7>() => <N:8>b - 1</N:8></N:7>)</N:6></N:2>
                     <N:9>select <N:10>Z(<N:11>() => <N:12>a - b</N:12></N:11>)</N:10></N:9>;
    }
}");
            var compilation0 = CreateCompilation(source0.Tree, options: ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            var compilation1 = compilation0.WithSource(source1.Tree);

            var v0 = CompileAndVerify(compilation0);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            // no new synthesized members generated (with #1 in names):
            diff1.VerifySynthesizedMembers(
                "C.<>c__DisplayClass1_1: {b, <F>b__4}",
                "C.<>c__DisplayClass1_2: {a, b, <F>b__5}",
                "C.<>c__DisplayClass1_0: {a, <F>b__3}",
                "C: {<F>b__1_0, <F>b__1_1, <F>b__1_2, <>c__DisplayClass1_0, <>c__DisplayClass1_1, <>c__DisplayClass1_2}");

            var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;

            // Method updates for lambdas:
            CheckEncLogDefinitions(reader1,
                Row(6, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(7, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(8, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(9, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(6, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(8, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(10, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(12, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(10, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(11, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(12, TableIndex.CustomAttribute, EditAndContinueOperation.Default));
        }

        [Fact]
        public void TransparentIdentifiers_JoinIntoClause()
        {
            var source0 = MarkedSource(@"
using System;
using System.Linq;

class C
{
    int Z(Func<int> f)
    {
        return 1;
    }

    public void F()
    {
        var result = <N:0>from a in <N:1>new[] { 1 }</N:1></N:0>
                     <N:2>join b in new[] { 3 } on 
                          <N:3>a + 1</N:3> equals <N:4>b - 1</N:4>
                          into g</N:2>
                     <N:5>select <N:6>Z(<N:7>() => <N:8>g.First()</N:8></N:7>)</N:6></N:5>;
    }
}");

            var source1 = MarkedSource(@"
using System;
using System.Linq;

class C
{
    int Z(Func<int> f)
    {
        return 1;
    }

    public void F()
    {
        var result = <N:0>from a in <N:1>new[] { 1 }</N:1></N:0>
                     <N:2>join b in new[] { 3 } on 
                          <N:3>a + 1</N:3> equals <N:4>b - 1</N:4>
                          into g</N:2>
                     <N:5>select <N:6>Z(<N:7>() => <N:8>g.Last()</N:8></N:7>)</N:6></N:5>;
    }
}");
            var compilation0 = CreateCompilation(source0.Tree, options: ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            var compilation1 = compilation0.WithSource(source1.Tree);

            var v0 = CompileAndVerify(compilation0);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            // no new synthesized members generated (with #1 in names):
            diff1.VerifySynthesizedMembers(
                "C: {<F>b__1_2, <>c__DisplayClass1_0, <>c}",
                "C.<>c: {<>9__1_0, <>9__1_1, <F>b__1_0, <F>b__1_1}",
                "C.<>c__DisplayClass1_0: {g, <F>b__3}");

            var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;

            // Method updates for lambdas:
            CheckEncLogDefinitions(reader1,
                Row(4, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(5, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(6, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(9, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(10, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(7, TableIndex.CustomAttribute, EditAndContinueOperation.Default));
        }

        [Fact]
        public void TransparentIdentifiers_QueryContinuation()
        {
            var source0 = MarkedSource(@"
using System;
using System.Linq;

class C
{
    int Z(Func<int> f)
    {
        return 1;
    }

    public void F()
    {
        var result = <N:0>from a in <N:1>new[] { 1 }</N:1></N:0>
                     <N:2>group a by <N:3>a + 1</N:3></N:2>
                     <N:4>into g
                     <N:5>select <N:6>Z(<N:7>() => <N:8>g.First()</N:8></N:7>)</N:6></N:5></N:4>;
    }
}");

            var source1 = MarkedSource(@"
using System;
using System.Linq;

class C
{
    int Z(Func<int> f)
    {
        return 1;
    }

    public void F()
    {
        var result = <N:0>from a in <N:1>new[] { 1 }</N:1></N:0>
                     <N:2>group a by <N:3>a + 1</N:3></N:2>
                     <N:4>into g
                     <N:5>select <N:6>Z(<N:7>() => <N:8>g.Last()</N:8></N:7>)</N:6></N:5></N:4>;
    }
}");
            var compilation0 = CreateCompilation(source0.Tree, options: ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            var compilation1 = compilation0.WithSource(source1.Tree);

            var v0 = CompileAndVerify(compilation0);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            // no new synthesized members generated (with #1 in names):
            diff1.VerifySynthesizedMembers(
                "C: {<F>b__1_1, <>c__DisplayClass1_0, <>c}",
                "C.<>c: {<>9__1_0, <F>b__1_0}",
                "C.<>c__DisplayClass1_0: {g, <F>b__2}");

            var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;

            // Method updates for lambdas:
            CheckEncLogDefinitions(reader1,
                Row(4, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(5, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(6, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(9, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(7, TableIndex.CustomAttribute, EditAndContinueOperation.Default));
        }

        [Fact]
        public void Lambdas_UpdateAfterAdd()
        {
            var source0 = MarkedSource(@"
using System;

class C
{
    static int G(Func<int, int> f) => 1;

    static object F()
    {
        return G(null);
    }
}");
            var source1 = MarkedSource(@"
using System;

class C
{
    static int G(Func<int, int> f) => 1;

    static object F()
    {
        return G(<N:0>a => a + 1</N:0>);
    }
}");
            var source2 = MarkedSource(@"
using System;

class C
{
    static int G(Func<int, int> f) => 1;

    static object F()
    {
        return G(<N:0>a => a + 2</N:0>);
    }
}");

            var compilation0 = CreateCompilation(source0.Tree, options: ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
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
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;

            // new lambda "<F>b__0#1" has been added:
            diff1.VerifySynthesizedMembers(
                "C: {<>c}",
                "C.<>c: {<>9__0#1, <F>b__0#1}");

            // added:
            diff1.VerifyIL("C.<>c.<F>b__0#1", @"
{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldc.i4.1
  IL_0002:  add
  IL_0003:  ret
}
");

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f1, f2, GetSyntaxMapFromMarkers(source1, source2), preserveLocalVariables: true)));

            diff2.VerifySynthesizedMembers(
                "C: {<>c}",
                "C.<>c: {<>9__0#1, <F>b__0#1}");

            // updated:
            diff2.VerifyIL("C.<>c.<F>b__0#1", @"
{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldc.i4.2
  IL_0002:  add
  IL_0003:  ret
}
");
        }

        [Fact]
        public void LocalFunctions_UpdateAfterAdd()
        {
            var source0 = MarkedSource(@"
using System;

class C
{
    static int G(Func<int, int> f) => 1;

    static object F()
    {
        return G(null);
    }
}");
            var source1 = MarkedSource(@"
using System;

class C
{
    static int G(Func<int, int> f) => 1;

    static object F()
    {
        <N:0>int f(int a) => a + 1;</N:0>
        return G(f);
    }
}");
            var source2 = MarkedSource(@"
using System;

class C
{
    static int G(Func<int, int> f) => 1;

    static object F()
    {
        <N:0>int f(int a) => a + 2;</N:0>
        return G(f);
    }
}");

            var compilation0 = CreateCompilation(source0.Tree, options: ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
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
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;

            // new lambda "<F>b__0#1" has been added:
            diff1.VerifySynthesizedMembers(
                "C: {<F>g__f|0#1}");

            // added:
            diff1.VerifyIL("C.<F>g__f|0#1(int)", @"
{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  add
  IL_0003:  ret
}
");

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f1, f2, GetSyntaxMapFromMarkers(source1, source2), preserveLocalVariables: true)));

            diff2.VerifySynthesizedMembers(
                "C: {<F>g__f|0#1}");

            // updated:
            diff2.VerifyIL("C.<F>g__f|0#1(int)", @"
{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.2
  IL_0002:  add
  IL_0003:  ret
}
");
        }

        [Fact]
        public void LocalFunctions_UpdateAfterAdd_NoDelegatePassing()
        {
            var source0 = MarkedSource(@"
using System;

class C
{
    static object F()
    {
        return 0;
    }
}");
            var source1 = MarkedSource(@"
using System;

class C
{
    static object F()
    {
        <N:0>int f(int a) => a + 1;</N:0>
        return 1;
    }
}");
            var source2 = MarkedSource(@"
using System;

class C
{
    static object F()
    {
        <N:0>int f(int a) => a + 2;</N:0>
        return 2;
    }
}");

            var compilation0 = CreateCompilation(source0.Tree, options: ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
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
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;

            // new lambda "<F>b__0#1" has been added:
            diff1.VerifySynthesizedMembers(
                "C: {<F>g__f|0#1}");

            // added:
            diff1.VerifyIL("C.<F>g__f|0#1(int)", @"
{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  add
  IL_0003:  ret
}
");

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f1, f2, GetSyntaxMapFromMarkers(source1, source2), preserveLocalVariables: true)));

            diff2.VerifySynthesizedMembers(
                "C: {<F>g__f|0#1}");

            // updated:
            diff2.VerifyIL("C.<F>g__f|0#1(int)", @"
{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.2
  IL_0002:  add
  IL_0003:  ret
}
");
        }

        [Fact]
        public void LambdasMultipleGenerations1()
        {
            var source0 = MarkedSource(@"
using System;

class C
{
    static int G(Func<int, int> f) => 1;

    static object F()
    {
        return G(<N:0>a => a + 1</N:0>);
    }
}");
            var source1 = MarkedSource(@"
using System;

class C
{
    static int G(Func<int, int> f) => 1;

    static object F()
    {
        return G(<N:0>a => a + 2</N:0>) + G(<N:1>b => b + 20</N:1>);
    }
}");
            var source2 = MarkedSource(@"
using System;

class C
{
    static int G(Func<int, int> f) => 1;

    static object F()
    {
        return G(<N:0>a => a + 3</N:0>) + G(<N:1>b => b + 30</N:1>) + G(<N:2>c => c + 0x300</N:2>);
    }
}");
            var source3 = MarkedSource(@"
using System;

class C
{
    static int G(Func<int, int> f) => 1;

    static object F()
    {
        return G(<N:0>a => a + 4</N:0>) + G(<N:1>b => b + 40</N:1>) + G(<N:2>c => c + 0x400</N:2>);
    }
}");

            var compilation0 = CreateCompilation(source0.Tree, options: ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            var compilation1 = compilation0.WithSource(source1.Tree);
            var compilation2 = compilation1.WithSource(source2.Tree);
            var compilation3 = compilation2.WithSource(source3.Tree);
            var v0 = CompileAndVerify(compilation0);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");
            var f2 = compilation2.GetMember<MethodSymbol>("C.F");
            var f3 = compilation3.GetMember<MethodSymbol>("C.F");

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;

            // new lambda "<F>b__1_1#1" has been added:
            diff1.VerifySynthesizedMembers(
                "C: {<>c}",
                "C.<>c: {<>9__1_0, <>9__1_1#1, <F>b__1_0, <F>b__1_1#1}");

            // updated:
            diff1.VerifyIL("C.<>c.<F>b__1_0", @"
{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldc.i4.2
  IL_0002:  add
  IL_0003:  ret
}
");
            // added:
            diff1.VerifyIL("C.<>c.<F>b__1_1#1", @"
{
  // Code size        5 (0x5)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldc.i4.s   20
  IL_0003:  add
  IL_0004:  ret
}
");

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f1, f2, GetSyntaxMapFromMarkers(source1, source2), preserveLocalVariables: true)));

            // new lambda "<F>b__1_2#2" has been added:
            diff2.VerifySynthesizedMembers(
                "C: {<>c}",
                "C.<>c: {<>9__1_0, <>9__1_1#1, <>9__1_2#2, <F>b__1_0, <F>b__1_1#1, <F>b__1_2#2}");

            // updated:
            diff2.VerifyIL("C.<>c.<F>b__1_0", @"
{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldc.i4.3
  IL_0002:  add
  IL_0003:  ret
}
");
            // updated:
            diff2.VerifyIL("C.<>c.<F>b__1_1#1", @"
{
  // Code size        5 (0x5)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldc.i4.s   30
  IL_0003:  add
  IL_0004:  ret
}
");

            // added:
            diff2.VerifyIL("C.<>c.<F>b__1_2#2", @"
{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldc.i4     0x300
  IL_0006:  add
  IL_0007:  ret
}
");

            var diff3 = compilation3.EmitDifference(
                diff2.NextGeneration,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f2, f3, GetSyntaxMapFromMarkers(source2, source3), preserveLocalVariables: true)));

            diff3.VerifySynthesizedMembers(
                "C: {<>c}",
                "C.<>c: {<>9__1_0, <>9__1_1#1, <>9__1_2#2, <F>b__1_0, <F>b__1_1#1, <F>b__1_2#2}");

            // updated:
            diff3.VerifyIL("C.<>c.<F>b__1_0", @"
{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldc.i4.4
  IL_0002:  add
  IL_0003:  ret
}
");
            // updated:
            diff3.VerifyIL("C.<>c.<F>b__1_1#1", @"
{
  // Code size        5 (0x5)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldc.i4.s   40
  IL_0003:  add
  IL_0004:  ret
}
");

            // updated:
            diff3.VerifyIL("C.<>c.<F>b__1_2#2", @"
{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldc.i4     0x400
  IL_0006:  add
  IL_0007:  ret
}
");
        }

        [Fact]
        public void LocalFunctionsMultipleGenerations1()
        {
            var source0 = MarkedSource(@"
using System;

class C
{
    static int G(Func<int, int> f) => 1;

    static object F()
    {
        <N:0>int f1(int a) => a + 1;</N:0>
        return G(f1);
    }
}");
            var source1 = MarkedSource(@"
using System;

class C
{
    static int G(Func<int, int> f) => 1;

    static object F()
    {
        <N:0>int f1(int a) => a + 2;</N:0>
        <N:1>int f2(int b) => b + 20;</N:1>
        return G(f1) + G(f2);
    }
}");
            var source2 = MarkedSource(@"
using System;

class C
{
    static int G(Func<int, int> f) => 1;

    static object F()
    {
        <N:0>int f1(int a) => a + 3;</N:0>
        <N:1>int f2(int b) => b + 30;</N:1>
        <N:2>int f3(int c) => c + 0x300;</N:2>
        return G(f1) + G(f2) + G(f3);
    }
}");
            var source3 = MarkedSource(@"
using System;

class C
{
    static int G(Func<int, int> f) => 1;

    static object F()
    {
        <N:0>int f1(int a) => a + 4;</N:0>
        <N:1>int f2(int b) => b + 40;</N:1>
        <N:2>int f3(int c) => c + 0x400;</N:2>
        return G(f1) + G(f2) + G(f3);
    }
}");

            var compilation0 = CreateCompilation(source0.Tree, options: ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            var compilation1 = compilation0.WithSource(source1.Tree);
            var compilation2 = compilation1.WithSource(source2.Tree);
            var compilation3 = compilation2.WithSource(source3.Tree);
            var v0 = CompileAndVerify(compilation0);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");
            var f2 = compilation2.GetMember<MethodSymbol>("C.F");
            var f3 = compilation3.GetMember<MethodSymbol>("C.F");

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;

            diff1.VerifySynthesizedMembers(
                "C: {<F>g__f1|1_0, <F>g__f2|1_1#1}");

            // updated:
            diff1.VerifyIL("C.<F>g__f1|1_0(int)", @"
{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.2
  IL_0002:  add
  IL_0003:  ret
}
");

            // added:
            diff1.VerifyIL("C.<F>g__f2|1_1#1(int)", @"
{
  // Code size        5 (0x5)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.s   20
  IL_0003:  add
  IL_0004:  ret
}
");

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f1, f2, GetSyntaxMapFromMarkers(source1, source2), preserveLocalVariables: true)));

            diff2.VerifySynthesizedMembers(
                "C: {<F>g__f1|1_0, <F>g__f2|1_1#1, <F>g__f3|1_2#2}");

            // updated:
            diff2.VerifyIL("C.<F>g__f1|1_0(int)", @"
{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.3
  IL_0002:  add
  IL_0003:  ret
}
");
            // updated:
            diff2.VerifyIL("C.<F>g__f2|1_1#1(int)", @"
{
  // Code size        5 (0x5)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.s   30
  IL_0003:  add
  IL_0004:  ret
}
");

            // added:
            diff2.VerifyIL("C.<F>g__f3|1_2#2(int)", @"
{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4     0x300
  IL_0006:  add
  IL_0007:  ret
}
");

            var diff3 = compilation3.EmitDifference(
                diff2.NextGeneration,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f2, f3, GetSyntaxMapFromMarkers(source2, source3), preserveLocalVariables: true)));

            diff3.VerifySynthesizedMembers(
                "C: {<F>g__f1|1_0, <F>g__f2|1_1#1, <F>g__f3|1_2#2}");

            // updated:
            diff3.VerifyIL("C.<F>g__f1|1_0(int)", @"
{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.4
  IL_0002:  add
  IL_0003:  ret
}
");
            // updated:
            diff3.VerifyIL("C.<F>g__f2|1_1#1(int)", @"
{
  // Code size        5 (0x5)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.s   40
  IL_0003:  add
  IL_0004:  ret
}
");

            // updated:
            diff3.VerifyIL("C.<F>g__f3|1_2#2(int)", @"
{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4     0x400
  IL_0006:  add
  IL_0007:  ret
}
");
        }

        [Fact, WorkItem(2284, "https://github.com/dotnet/roslyn/issues/2284")]
        public void LambdasMultipleGenerations2()
        {
            var source0 = MarkedSource(@"
using System;
using System.Linq;

class C
{
    private int[] _titles = new int[] { 1, 2 };
    Action A;

    private void F()
    {
        // edit 1
        // var z = from title in _titles
        //         where title > 0 
        //         select title;

        A += <N:0>() =>
        <N:1>{
            Console.WriteLine(1);

            // edit 2
            // Console.WriteLine(2);
        }</N:1></N:0>;
    }
}");
            var source1 = MarkedSource(@"
using System;
using System.Linq;

class C
{
    private int[] _titles = new int[] { 1, 2 };
    Action A;

    private void F()
    {
        // edit 1
        var <N:3>z = from title in _titles 
                     <N:2>where title > 0</N:2>
                     select title</N:3>;

        A += <N:0>() =>
        <N:1>{
            Console.WriteLine(1);

            // edit 2
            // Console.WriteLine(2);
        }</N:1></N:0>;
    }
}");
            var source2 = MarkedSource(@"
using System;
using System.Linq;

class C
{
    private int[] _titles = new int[] { 1, 2 };
    Action A;

    private void F()
    {
        // edit 1
        var <N:3>z = from title in _titles
                     <N:2>where title > 0</N:2> 
                     select title</N:3>;

        A += <N:0>() =>
        <N:1>{
            Console.WriteLine(1);

            // edit 2
            Console.WriteLine(2);
        }</N:1></N:0>;
    }
}");

            var compilation0 = CreateCompilation(source0.Tree, options: ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
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
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;

            // new lambda "<F>b__2_0#1" has been added:
            diff1.VerifySynthesizedMembers(
                "C: {<>c}",
                "C.<>c: {<>9__2_0#1, <>9__2_0, <F>b__2_0#1, <F>b__2_0}");

            // lambda body unchanged:
            diff1.VerifyIL("C.<>c.<F>b__2_0", @"
{
  // Code size        9 (0x9)
  .maxstack  1
  IL_0000:  nop
  IL_0001:  ldc.i4.1
  IL_0002:  call       ""void System.Console.WriteLine(int)""
  IL_0007:  nop
  IL_0008:  ret
}");

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f1, f2, GetSyntaxMapFromMarkers(source1, source2), preserveLocalVariables: true)));

            // no new members:
            diff2.VerifySynthesizedMembers(
                "C: {<>c}",
                "C.<>c: {<>9__2_0#1, <>9__2_0, <F>b__2_0#1, <F>b__2_0}");

            // lambda body updated:
            diff2.VerifyIL("C.<>c.<F>b__2_0", @"
{
  // Code size       16 (0x10)
  .maxstack  1
  IL_0000:  nop
  IL_0001:  ldc.i4.1
  IL_0002:  call       ""void System.Console.WriteLine(int)""
  IL_0007:  nop
  IL_0008:  ldc.i4.2
  IL_0009:  call       ""void System.Console.WriteLine(int)""
  IL_000e:  nop
  IL_000f:  ret
}");
        }

        [Fact]
        public void UniqueSynthesizedNames1()
        {
            var source0 = @"
using System;

public class C
{    
    public int F() 
    { 
        var a = 1; 
        var f1 = new Func<int>(() => 1);
        var f2 = new Func<int>(() => F());
        var f3 = new Func<int>(() => a);
        return 2;
    }
}";
            var source1 = @"
using System;

public class C
{
    public int F(int x) 
    { 
        var a = 1; 
        var f1 = new Func<int>(() => 1);
        var f2 = new Func<int>(() => F());
        var f3 = new Func<int>(() => a);
        return 2;
    }

    public int F() 
    { 
        var a = 1; 
        var f1 = new Func<int>(() => 1);
        var f2 = new Func<int>(() => F());
        var f3 = new Func<int>(() => a);
        return 2;
    }
}";
            var source2 = @"
using System;

public class C
{
    public int F(int x) 
    { 
        var a = 1; 
        var f1 = new Func<int>(() => 1);
        var f2 = new Func<int>(() => F());
        var f3 = new Func<int>(() => a);
        return 2;
    }

    public int F(byte x) 
    { 
        var a = 1; 
        var f1 = new Func<int>(() => 1);
        var f2 = new Func<int>(() => F());
        var f3 = new Func<int>(() => a);
        return 2;
    }

    public int F() 
    { 
        var a = 1; 
        var f1 = new Func<int>(() => 1);
        var f2 = new Func<int>(() => F());
        var f3 = new Func<int>(() => a);
        return 2;
    }
}";

            var compilation0 = CreateCompilationWithMscorlib45(source0, options: ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All), assemblyName: "A");
            var compilation1 = compilation0.WithSource(source1);
            var compilation2 = compilation1.WithSource(source2);

            var f_int1 = compilation1.GetMembers("C.F").Single(m => m.ToString() == "C.F(int)");
            var f_byte2 = compilation2.GetMembers("C.F").Single(m => m.ToString() == "C.F(byte)");

            var v0 = CompileAndVerify(compilation0);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var reader0 = md0.MetadataReader;
            CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "C", "<>c__DisplayClass0_0", "<>c");
            CheckNames(reader0, reader0.GetMethodDefNames(), "F", ".ctor", ".ctor", "<F>b__1", "<F>b__2", ".cctor", ".ctor", "<F>b__0_0");
            CheckNames(reader0, reader0.GetFieldDefNames(), "<>4__this", "a", "<>9", "<>9__0_0");

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Insert, null, f_int1)));

            var reader1 = diff1.GetMetadata().Reader;

            CheckNames(new[] { reader0, reader1 }, reader1.GetTypeDefNames(), "<>c__DisplayClass0#1_0#1");
            CheckNames(new[] { reader0, reader1 }, reader1.GetMethodDefNames(), ".ctor", "F", ".ctor", "<F>b__1#1", "<F>b__2#1", "<F>b__0#1_0#1");
            CheckNames(new[] { reader0, reader1 }, reader1.GetFieldDefNames(), "<>4__this", "a", "<>9__0#1_0#1");

            diff1.VerifySynthesizedMembers(
                "C: {<>c__DisplayClass0#1_0#1, <>c}",
                "C.<>c__DisplayClass0#1_0#1: {<>4__this, a, <F>b__1#1, <F>b__2#1}",
                "C.<>c: {<>9__0#1_0#1, <F>b__0#1_0#1}");

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Insert, null, f_byte2)));

            var reader2 = diff2.GetMetadata().Reader;

            CheckNames(new[] { reader0, reader1, reader2 }, reader2.GetTypeDefNames(), "<>c__DisplayClass1#2_0#2");
            CheckNames(new[] { reader0, reader1, reader2 }, reader2.GetMethodDefNames(), ".ctor", "F", ".ctor", "<F>b__1#2", "<F>b__2#2", "<F>b__1#2_0#2");
            CheckNames(new[] { reader0, reader1, reader2 }, reader2.GetFieldDefNames(), "<>4__this", "a", "<>9__1#2_0#2");
        }

        [Fact]
        public void UniqueSynthesizedNames1_Generic()
        {
            var source0 = @"
using System;

public class C
{    
    public int F<T>() 
    { 
        var a = 1; 
        var f1 = new Func<int>(() => 1);
        var f2 = new Func<int>(() => F<T>());
        var f3 = new Func<int>(() => a);
        return 2;
    }
}";
            var source1 = @"
using System;

public class C
{
    public int F<T>(int x) 
    { 
        var a = 1; 
        var f1 = new Func<int>(() => 1);
        var f2 = new Func<int>(() => F<T>());
        var f3 = new Func<int>(() => a);
        return 2;
    }

    public int F<T>() 
    { 
        var a = 1; 
        var f1 = new Func<int>(() => 1);
        var f2 = new Func<int>(() => F<T>());
        var f3 = new Func<int>(() => a);
        return 2;
    }
}";
            var source2 = @"
using System;

public class C
{
    public int F<T>(int x) 
    { 
        var a = 1; 
        var f1 = new Func<int>(() => 1);
        var f2 = new Func<int>(() => F<T>());
        var f3 = new Func<int>(() => a);
        return 2;
    }

    public int F<T>(byte x) 
    { 
        var a = 1; 
        var f1 = new Func<int>(() => 1);
        var f2 = new Func<int>(() => F<T>());
        var f3 = new Func<int>(() => a);
        return 2;
    }

    public int F<T>() 
    { 
        var a = 1; 
        var f1 = new Func<int>(() => 1);
        var f2 = new Func<int>(() => F<T>());
        var f3 = new Func<int>(() => a);
        return 2;
    }
}";

            var compilation0 = CreateCompilationWithMscorlib45(source0, options: ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All), assemblyName: "A");
            var compilation1 = compilation0.WithSource(source1);
            var compilation2 = compilation1.WithSource(source2);

            var f_int1 = compilation1.GetMembers("C.F").Single(m => m.ToString() == "C.F<T>(int)");
            var f_byte2 = compilation2.GetMembers("C.F").Single(m => m.ToString() == "C.F<T>(byte)");

            var v0 = CompileAndVerify(compilation0);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var reader0 = md0.MetadataReader;
            CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "C", "<>c__DisplayClass0_0`1", "<>c__0`1");
            CheckNames(reader0, reader0.GetMethodDefNames(), "F", ".ctor", ".ctor", "<F>b__1", "<F>b__2", ".cctor", ".ctor", "<F>b__0_0");
            CheckNames(reader0, reader0.GetFieldDefNames(), "<>4__this", "a", "<>9", "<>9__0_0");

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Insert, null, f_int1)));

            var reader1 = diff1.GetMetadata().Reader;

            CheckNames(new[] { reader0, reader1 }, reader1.GetTypeDefNames(), "<>c__DisplayClass0#1_0#1`1", "<>c__0#1`1");
            CheckNames(new[] { reader0, reader1 }, reader1.GetMethodDefNames(), "F", ".ctor", "<F>b__1#1", "<F>b__2#1", ".cctor", ".ctor", "<F>b__0#1_0#1");
            CheckNames(new[] { reader0, reader1 }, reader1.GetFieldDefNames(), "<>4__this", "a", "<>9", "<>9__0#1_0#1");

            diff1.VerifySynthesizedMembers(
                "C.<>c__0#1<T>: {<>9__0#1_0#1, <F>b__0#1_0#1}",
                "C: {<>c__DisplayClass0#1_0#1, <>c__0#1}",
                "C.<>c__DisplayClass0#1_0#1<T>: {<>4__this, a, <F>b__1#1, <F>b__2#1}");

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Insert, null, f_byte2)));

            var reader2 = diff2.GetMetadata().Reader;

            CheckNames(new[] { reader0, reader1, reader2 }, reader2.GetTypeDefNames(), "<>c__DisplayClass1#2_0#2`1", "<>c__1#2`1");
            CheckNames(new[] { reader0, reader1, reader2 }, reader2.GetMethodDefNames(), "F", ".ctor", "<F>b__1#2", "<F>b__2#2", ".cctor", ".ctor", "<F>b__1#2_0#2");
            CheckNames(new[] { reader0, reader1, reader2 }, reader2.GetFieldDefNames(), "<>4__this", "a", "<>9", "<>9__1#2_0#2");
        }

        [Fact]
        public void UniqueSynthesizedNames2()
        {
            var source0 = @"
using System;

public class C
{    
    public static void Main() 
    {
    }
}";
            var source1 = @"
using System;

public class C
{
    public static void Main() 
    {
        new C().F();
    }

    public int F() 
    { 
        var a = 1; 
        var f1 = new Func<int>(() => 1);
        var f2 = new Func<int>(() => F());
        var f3 = new Func<int>(() => a);
        return 2;
    }
}";
            var source2 = @"
using System;

public class C
{
    public static void Main() 
    {
        new C().F(1);
        new C().F();
    }

    public int F(int x) 
    { 
        var a = 1; 
        var f1 = new Func<int>(() => 1);
        var f2 = new Func<int>(() => F());
        var f3 = new Func<int>(() => a);
        return 2;
    }

    public int F() 
    { 
        var a = 1; 
        var f1 = new Func<int>(() => 1);
        var f2 = new Func<int>(() => F());
        var f3 = new Func<int>(() => a);
        return 2;
    }
}";

            var source3 = @"
using System;

public class C
{
    public static void Main() 
    {
    }

    public int F(int x) 
    { 
        var a = 1; 
        var f1 = new Func<int>(() => 1);
        var f2 = new Func<int>(() => F());
        var f3 = new Func<int>(() => a);
        return 2;
    }

    public int F() 
    { 
        var a = 1; 
        var f1 = new Func<int>(() => 1);
        var f2 = new Func<int>(() => F());
        var f3 = new Func<int>(() => a);
        return 2;
    }
}";

            var compilation0 = CreateCompilationWithMscorlib45(source0, options: ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All), assemblyName: "A");
            var compilation1 = compilation0.WithSource(source1);
            var compilation2 = compilation1.WithSource(source2);
            var compilation3 = compilation2.WithSource(source3);

            var main0 = compilation0.GetMember<MethodSymbol>("C.Main");
            var main1 = compilation1.GetMember<MethodSymbol>("C.Main");
            var main2 = compilation2.GetMember<MethodSymbol>("C.Main");
            var main3 = compilation3.GetMember<MethodSymbol>("C.Main");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");
            var f_int2 = compilation2.GetMembers("C.F").Single(m => m.ToString() == "C.F(int)");

            var v0 = CompileAndVerify(compilation0);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Insert, null, f1),
                    SemanticEdit.Create(SemanticEditKind.Update, main0, main1, preserveLocalVariables: true)));

            diff1.VerifySynthesizedMembers(
                "C.<>c: {<>9__1#1_0#1, <F>b__1#1_0#1}",
                "C.<>c__DisplayClass1#1_0#1: {<>4__this, a, <F>b__1#1, <F>b__2#1}",
                "C: {<>c__DisplayClass1#1_0#1, <>c}");

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Insert, null, f_int2),
                    SemanticEdit.Create(SemanticEditKind.Update, main1, main2, preserveLocalVariables: true)));

            diff2.VerifySynthesizedMembers(
                "C.<>c__DisplayClass1#2_0#2: {<>4__this, a, <F>b__1#2, <F>b__2#2}",
                "C: {<>c__DisplayClass1#2_0#2, <>c, <>c__DisplayClass1#1_0#1}",
                "C.<>c: {<>9__1#2_0#2, <F>b__1#2_0#2, <>9__1#1_0#1, <F>b__1#1_0#1}",
                "C.<>c__DisplayClass1#1_0#1: {<>4__this, a, <F>b__1#1, <F>b__2#1}");

            var diff3 = compilation3.EmitDifference(
                diff2.NextGeneration,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, main2, main3, preserveLocalVariables: true)));

            diff3.VerifySynthesizedMembers(
                "C.<>c__DisplayClass1#1_0#1: {<>4__this, a, <F>b__1#1, <F>b__2#1}",
                "C.<>c: {<>9__1#2_0#2, <F>b__1#2_0#2, <>9__1#1_0#1, <F>b__1#1_0#1}",
                "C.<>c__DisplayClass1#2_0#2: {<>4__this, a, <F>b__1#2, <F>b__2#2}",
                "C: {<>c__DisplayClass1#2_0#2, <>c, <>c__DisplayClass1#1_0#1}");
        }

        [Fact]
        public void NestedLambdas()
        {
            var source0 = MarkedSource(@"
using System;

class C
{
    static int G(Func<int, int> f) => 1;

    static object F()
    {
        return G(<N:0>a => a + G(<N:1>b => 1</N:1>)</N:0>);
    }
}");
            var source1 = MarkedSource(@"
using System;

class C
{
    static int G(Func<int, int> f) => 1;

    static object F()
    {
        return G(<N:0>a => a + G(<N:1>b => 2</N:1>)</N:0>);
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
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;

            // 3 method updates:
            // Note that even if the change is in the inner lambda such a change will usually impact sequence point 
            // spans in outer lambda and the method body. So although the IL doesn't change we usually need to update the outer methods.
            CheckEncLogDefinitions(reader1,
                Row(2, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(6, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(7, TableIndex.MethodDef, EditAndContinueOperation.Default));
        }

        [Fact]
        public void LambdasInInitializers1()
        {
            var source0 = MarkedSource(@"
using System;

class C
{
    static int G(Func<int, int> f) => 1;

    public int A = G(<N:0>a => a + 1</N:0>);

    public C() : this(G(<N:1>a => a + 2</N:1>))
    {
        G(<N:2>a => a + 3</N:2>);
    }

    public C(int x)
    {
        G(<N:3>a => a + 4</N:3>);
    }
}");
            var source1 = MarkedSource(@"
using System;

class C
{
    static int G(Func<int, int> f) => 1;

    public int A = G(<N:0>a => a - 1</N:0>);

    public C() : this(G(<N:1>a => a - 2</N:1>))
    {
        G(<N:2>a => a - 3</N:2>);
    }

    public C(int x)
    {
        G(<N:3>a => a - 4</N:3>);
    }
}");
            var compilation0 = CreateCompilation(source0.Tree, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);
            var v0 = CompileAndVerify(compilation0);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var ctor00 = compilation0.GetMembers("C..ctor").Single(m => m.ToTestDisplayString() == "C..ctor()");
            var ctor10 = compilation0.GetMembers("C..ctor").Single(m => m.ToTestDisplayString() == "C..ctor(System.Int32 x)");
            var ctor01 = compilation1.GetMembers("C..ctor").Single(m => m.ToTestDisplayString() == "C..ctor()");
            var ctor11 = compilation1.GetMembers("C..ctor").Single(m => m.ToTestDisplayString() == "C..ctor(System.Int32 x)");

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, ctor00, ctor01, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true),
                    SemanticEdit.Create(SemanticEditKind.Update, ctor10, ctor11, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;

            diff1.VerifySynthesizedMembers(
                "C: {<>c}",
                "C.<>c: {<>9__2_0, <>9__2_1, <>9__3_0, <>9__3_1, <.ctor>b__2_0, <.ctor>b__2_1, <.ctor>b__3_0, <.ctor>b__3_1}");

            diff1.VerifyIL("C.<>c.<.ctor>b__2_0", @"
{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldc.i4.2
  IL_0002:  sub
  IL_0003:  ret
}");

            diff1.VerifyIL("C.<>c.<.ctor>b__2_1", @"
{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldc.i4.3
  IL_0002:  sub
  IL_0003:  ret
}");

            diff1.VerifyIL("C.<>c.<.ctor>b__3_0", @"
{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldc.i4.4
  IL_0002:  sub
  IL_0003:  ret
}");
            diff1.VerifyIL("C.<>c.<.ctor>b__3_1", @"
{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldc.i4.1
  IL_0002:  sub
  IL_0003:  ret
}");
        }

        [Fact]
        public void LambdasInInitializers2()
        {
            var source0 = MarkedSource(@"
using System;

class C
{
    static int G(Func<int, int> f) => 1;

    public int A = G(<N:0>a => { int <N:4>v1 = 1</N:4>; return 1; }</N:0>);

    public C() : this(G(<N:1>a => { int <N:5>v2 = 1</N:5>; return 2; }</N:1>))
    {
        G(<N:2>a => { int <N:6>v3 = 1</N:6>; return 3; }</N:2>);
    }

    public C(int x)
    {
        G(<N:3>a => { int <N:7>v4 = 1</N:7>; return 4; }</N:3>);
    }
}");
            var source1 = MarkedSource(@"
using System;

class C
{
    static int G(Func<int, int> f) => 1;

    public int A = G(<N:0>a => { int <N:4>v1 = 10</N:4>; return 1; }</N:0>);

    public C() : this(G(<N:1>a => { int <N:5>v2 = 10</N:5>; return 2; }</N:1>))
    {
        G(<N:2>a => { int <N:6>v3 = 10</N:6>; return 3; }</N:2>);
    }

    public C(int x)
    {
        G(<N:3>a => { int <N:7>v4 = 10</N:7>; return 4; }</N:3>);
    }
}");
            var compilation0 = CreateCompilation(source0.Tree, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);
            var v0 = CompileAndVerify(compilation0);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var ctor00 = compilation0.GetMembers("C..ctor").Single(m => m.ToTestDisplayString() == "C..ctor()");
            var ctor10 = compilation0.GetMembers("C..ctor").Single(m => m.ToTestDisplayString() == "C..ctor(System.Int32 x)");
            var ctor01 = compilation1.GetMembers("C..ctor").Single(m => m.ToTestDisplayString() == "C..ctor()");
            var ctor11 = compilation1.GetMembers("C..ctor").Single(m => m.ToTestDisplayString() == "C..ctor(System.Int32 x)");

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, ctor00, ctor01, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true),
                    SemanticEdit.Create(SemanticEditKind.Update, ctor10, ctor11, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;

            diff1.VerifySynthesizedMembers(
                "C: {<>c}",
                "C.<>c: {<>9__2_0, <>9__2_1, <>9__3_0, <>9__3_1, <.ctor>b__2_0, <.ctor>b__2_1, <.ctor>b__3_0, <.ctor>b__3_1}");

            diff1.VerifyIL("C.<>c.<.ctor>b__2_0", @"
{
  // Code size       10 (0xa)
  .maxstack  1
  .locals init (int V_0, //v2
                [int] V_1,
                int V_2)
  IL_0000:  nop
  IL_0001:  ldc.i4.s   10
  IL_0003:  stloc.0
  IL_0004:  ldc.i4.2
  IL_0005:  stloc.2
  IL_0006:  br.s       IL_0008
  IL_0008:  ldloc.2
  IL_0009:  ret
}");

            diff1.VerifyIL("C.<>c.<.ctor>b__2_1", @"
{
  // Code size       10 (0xa)
  .maxstack  1
  .locals init (int V_0, //v3
                [int] V_1,
                int V_2)
  IL_0000:  nop
  IL_0001:  ldc.i4.s   10
  IL_0003:  stloc.0
  IL_0004:  ldc.i4.3
  IL_0005:  stloc.2
  IL_0006:  br.s       IL_0008
  IL_0008:  ldloc.2
  IL_0009:  ret
}");

            diff1.VerifyIL("C.<>c.<.ctor>b__3_0", @"
{
  // Code size       10 (0xa)
  .maxstack  1
  .locals init (int V_0, //v4
                [int] V_1,
                int V_2)
  IL_0000:  nop
  IL_0001:  ldc.i4.s   10
  IL_0003:  stloc.0
  IL_0004:  ldc.i4.4
  IL_0005:  stloc.2
  IL_0006:  br.s       IL_0008
  IL_0008:  ldloc.2
  IL_0009:  ret
}");
            diff1.VerifyIL("C.<>c.<.ctor>b__3_1", @"
{
  // Code size       10 (0xa)
  .maxstack  1
  .locals init (int V_0, //v1
                [int] V_1,
                int V_2)
  IL_0000:  nop
  IL_0001:  ldc.i4.s   10
  IL_0003:  stloc.0
  IL_0004:  ldc.i4.1
  IL_0005:  stloc.2
  IL_0006:  br.s       IL_0008
  IL_0008:  ldloc.2
  IL_0009:  ret
}");
        }

        [Fact]
        public void UpdateParameterlessConstructorInPresenceOfFieldInitializersWithLambdas()
        {
            var source0 = MarkedSource(@"
using System;

class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0>a => a + 1</N:0>);
}");
            var source1 = MarkedSource(@"
using System;

class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0>a => a + 1</N:0>);
    int B = F(b => b + 1);                    // new field

    public C()                                // new ctor
    {
        F(c => c + 1);
    }
}");
            var compilation0 = CreateCompilation(source0.Tree, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);
            var v0 = CompileAndVerify(compilation0);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var b1 = compilation1.GetMember<FieldSymbol>("C.B");
            var ctor0 = compilation0.GetMember<MethodSymbol>("C..ctor");
            var ctor1 = compilation1.GetMember<MethodSymbol>("C..ctor");

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Insert, null, b1),
                    SemanticEdit.Create(SemanticEditKind.Update, ctor0, ctor1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            diff1.VerifySynthesizedMembers(
                "C: {<>c}",
                "C.<>c: {<>9__2_0#1, <>9__2_0, <>9__2_2#1, <.ctor>b__2_0#1, <.ctor>b__2_0, <.ctor>b__2_2#1}");

            diff1.VerifyIL("C..ctor", @"
{
  // Code size      130 (0x82)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldsfld     ""System.Func<int, int> C.<>c.<>9__2_0""
  IL_0006:  dup
  IL_0007:  brtrue.s   IL_0020
  IL_0009:  pop
  IL_000a:  ldsfld     ""C.<>c C.<>c.<>9""
  IL_000f:  ldftn      ""int C.<>c.<.ctor>b__2_0(int)""
  IL_0015:  newobj     ""System.Func<int, int>..ctor(object, System.IntPtr)""
  IL_001a:  dup
  IL_001b:  stsfld     ""System.Func<int, int> C.<>c.<>9__2_0""
  IL_0020:  call       ""int C.F(System.Func<int, int>)""
  IL_0025:  stfld      ""int C.A""
  IL_002a:  ldarg.0
  IL_002b:  ldsfld     ""System.Func<int, int> C.<>c.<>9__2_2#1""
  IL_0030:  dup
  IL_0031:  brtrue.s   IL_004a
  IL_0033:  pop
  IL_0034:  ldsfld     ""C.<>c C.<>c.<>9""
  IL_0039:  ldftn      ""int C.<>c.<.ctor>b__2_2#1(int)""
  IL_003f:  newobj     ""System.Func<int, int>..ctor(object, System.IntPtr)""
  IL_0044:  dup
  IL_0045:  stsfld     ""System.Func<int, int> C.<>c.<>9__2_2#1""
  IL_004a:  call       ""int C.F(System.Func<int, int>)""
  IL_004f:  stfld      ""int C.B""
  IL_0054:  ldarg.0
  IL_0055:  call       ""object..ctor()""
  IL_005a:  nop
  IL_005b:  nop
  IL_005c:  ldsfld     ""System.Func<int, int> C.<>c.<>9__2_0#1""
  IL_0061:  dup
  IL_0062:  brtrue.s   IL_007b
  IL_0064:  pop
  IL_0065:  ldsfld     ""C.<>c C.<>c.<>9""
  IL_006a:  ldftn      ""int C.<>c.<.ctor>b__2_0#1(int)""
  IL_0070:  newobj     ""System.Func<int, int>..ctor(object, System.IntPtr)""
  IL_0075:  dup
  IL_0076:  stsfld     ""System.Func<int, int> C.<>c.<>9__2_0#1""
  IL_007b:  call       ""int C.F(System.Func<int, int>)""
  IL_0080:  pop
  IL_0081:  ret
}
");
        }

        [Fact(Skip = "2504"), WorkItem(2504, "https://github.com/dotnet/roslyn/issues/2504")]
        public void InsertConstructorInPresenceOfFieldInitializersWithLambdas()
        {
            var source0 = MarkedSource(@"
using System;

class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0>a => a + 1</N:0>);
}");
            var source1 = MarkedSource(@"
using System;

class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0>a => a + 1</N:0>);
    int B = F(b => b + 1);                    // new field

    public C(int x)                           // new ctor
    {
        F(c => c + 1);
    }
}");
            var compilation0 = CreateCompilation(source0.Tree, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);
            var v0 = CompileAndVerify(compilation0);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var b1 = compilation1.GetMember<FieldSymbol>("C.B");
            var ctor1 = compilation1.GetMember<MethodSymbol>("C..ctor");

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Insert, null, b1),
                    SemanticEdit.Create(SemanticEditKind.Insert, null, ctor1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;

            diff1.VerifySynthesizedMembers(
                "C: {<> c}",
                "C.<>c: {<>9__2_0#1, <>9__2_0, <>9__2_2#1, <.ctor>b__2_0#1, <.ctor>b__2_0, <.ctor>b__2_2#1}");
        }

        [Fact]
        public void Queries_Select_Reduced1()
        {
            var source0 = MarkedSource(@"
using System;
using System.Linq;
using System.Collections.Generic;

class C
{
    void F()
    {
        var <N:0>result = from a in array
                          <N:1>where a > 0</N:1>
                          <N:2>select a</N:2></N:0>;
    }

    int[] array = null;
}
");
            var source1 = MarkedSource(@"
using System;
using System.Linq;
using System.Collections.Generic;

class C
{
    void F()
    {
        var <N:0>result = from a in array
                          <N:1>where a > 0</N:1>
                          <N:2>select a + 1</N:2></N:0>;
    }

    int[] array = null;
}
");
            var compilation0 = CreateCompilation(source0.Tree, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);
            var v0 = CompileAndVerify(compilation0);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;

            // new lambda for Select(a => a + 1)
            diff1.VerifySynthesizedMembers(
                "C: {<>c}",
                "C.<>c: {<>9__0_0, <>9__0_1#1, <F>b__0_0, <F>b__0_1#1}");

            diff1.VerifyIL("C.<>c.<F>b__0_1#1", @"
{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldc.i4.1
  IL_0002:  add
  IL_0003:  ret
}
");
            // old query:
            v0.VerifyIL("C.F", @"
{
  // Code size       45 (0x2d)
  .maxstack  3
  .locals init (System.Collections.Generic.IEnumerable<int> V_0) //result
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldfld      ""int[] C.array""
  IL_0007:  ldsfld     ""System.Func<int, bool> C.<>c.<>9__0_0""
  IL_000c:  dup
  IL_000d:  brtrue.s   IL_0026
  IL_000f:  pop
  IL_0010:  ldsfld     ""C.<>c C.<>c.<>9""
  IL_0015:  ldftn      ""bool C.<>c.<F>b__0_0(int)""
  IL_001b:  newobj     ""System.Func<int, bool>..ctor(object, System.IntPtr)""
  IL_0020:  dup
  IL_0021:  stsfld     ""System.Func<int, bool> C.<>c.<>9__0_0""
  IL_0026:  call       ""System.Collections.Generic.IEnumerable<int> System.Linq.Enumerable.Where<int>(System.Collections.Generic.IEnumerable<int>, System.Func<int, bool>)""
  IL_002b:  stloc.0
  IL_002c:  ret
}
");
            // new query:
            diff1.VerifyIL("C.F", @"
{
  // Code size       81 (0x51)
  .maxstack  3
  .locals init (System.Collections.Generic.IEnumerable<int> V_0) //result
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldfld      ""int[] C.array""
  IL_0007:  ldsfld     ""System.Func<int, bool> C.<>c.<>9__0_0""
  IL_000c:  dup
  IL_000d:  brtrue.s   IL_0026
  IL_000f:  pop
  IL_0010:  ldsfld     ""C.<>c C.<>c.<>9""
  IL_0015:  ldftn      ""bool C.<>c.<F>b__0_0(int)""
  IL_001b:  newobj     ""System.Func<int, bool>..ctor(object, System.IntPtr)""
  IL_0020:  dup
  IL_0021:  stsfld     ""System.Func<int, bool> C.<>c.<>9__0_0""
  IL_0026:  call       ""System.Collections.Generic.IEnumerable<int> System.Linq.Enumerable.Where<int>(System.Collections.Generic.IEnumerable<int>, System.Func<int, bool>)""
  IL_002b:  ldsfld     ""System.Func<int, int> C.<>c.<>9__0_1#1""
  IL_0030:  dup
  IL_0031:  brtrue.s   IL_004a
  IL_0033:  pop
  IL_0034:  ldsfld     ""C.<>c C.<>c.<>9""
  IL_0039:  ldftn      ""int C.<>c.<F>b__0_1#1(int)""
  IL_003f:  newobj     ""System.Func<int, int>..ctor(object, System.IntPtr)""
  IL_0044:  dup
  IL_0045:  stsfld     ""System.Func<int, int> C.<>c.<>9__0_1#1""
  IL_004a:  call       ""System.Collections.Generic.IEnumerable<int> System.Linq.Enumerable.Select<int, int>(System.Collections.Generic.IEnumerable<int>, System.Func<int, int>)""
  IL_004f:  stloc.0
  IL_0050:  ret
}
");
        }

        [Fact]
        public void Queries_Select_Reduced2()
        {
            var source0 = MarkedSource(@"
using System;
using System.Linq;
using System.Collections.Generic;

class C
{
    void F()
    {
        var <N:0>result = from a in array
                          <N:1>where a > 0</N:1>
                          <N:2>select a + 1</N:2></N:0>;
    }

    int[] array = null;
}
");
            var source1 = MarkedSource(@"
using System;
using System.Linq;
using System.Collections.Generic;

class C
{
    void F()
    {
        var <N:0>result = from a in array
                          <N:1>where a > 0</N:1>
                          <N:2>select a</N:2></N:0>;
    }

    int[] array = null;
}
");
            var compilation0 = CreateCompilation(source0.Tree, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);
            var v0 = CompileAndVerify(compilation0);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;

            // lambda for Select(a => a + 1) is gone
            diff1.VerifySynthesizedMembers(
                "C: {<>c}",
                "C.<>c: {<>9__0_0, <F>b__0_0}");

            // old query:
            v0.VerifyIL("C.F", @"
{
  // Code size       81 (0x51)
  .maxstack  3
  .locals init (System.Collections.Generic.IEnumerable<int> V_0) //result
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldfld      ""int[] C.array""
  IL_0007:  ldsfld     ""System.Func<int, bool> C.<>c.<>9__0_0""
  IL_000c:  dup
  IL_000d:  brtrue.s   IL_0026
  IL_000f:  pop
  IL_0010:  ldsfld     ""C.<>c C.<>c.<>9""
  IL_0015:  ldftn      ""bool C.<>c.<F>b__0_0(int)""
  IL_001b:  newobj     ""System.Func<int, bool>..ctor(object, System.IntPtr)""
  IL_0020:  dup
  IL_0021:  stsfld     ""System.Func<int, bool> C.<>c.<>9__0_0""
  IL_0026:  call       ""System.Collections.Generic.IEnumerable<int> System.Linq.Enumerable.Where<int>(System.Collections.Generic.IEnumerable<int>, System.Func<int, bool>)""
  IL_002b:  ldsfld     ""System.Func<int, int> C.<>c.<>9__0_1""
  IL_0030:  dup
  IL_0031:  brtrue.s   IL_004a
  IL_0033:  pop
  IL_0034:  ldsfld     ""C.<>c C.<>c.<>9""
  IL_0039:  ldftn      ""int C.<>c.<F>b__0_1(int)""
  IL_003f:  newobj     ""System.Func<int, int>..ctor(object, System.IntPtr)""
  IL_0044:  dup
  IL_0045:  stsfld     ""System.Func<int, int> C.<>c.<>9__0_1""
  IL_004a:  call       ""System.Collections.Generic.IEnumerable<int> System.Linq.Enumerable.Select<int, int>(System.Collections.Generic.IEnumerable<int>, System.Func<int, int>)""
  IL_004f:  stloc.0
  IL_0050:  ret
}
");
            // new query:
            diff1.VerifyIL("C.F", @"
{
  // Code size       45 (0x2d)
  .maxstack  3
  .locals init (System.Collections.Generic.IEnumerable<int> V_0) //result
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldfld      ""int[] C.array""
  IL_0007:  ldsfld     ""System.Func<int, bool> C.<>c.<>9__0_0""
  IL_000c:  dup
  IL_000d:  brtrue.s   IL_0026
  IL_000f:  pop
  IL_0010:  ldsfld     ""C.<>c C.<>c.<>9""
  IL_0015:  ldftn      ""bool C.<>c.<F>b__0_0(int)""
  IL_001b:  newobj     ""System.Func<int, bool>..ctor(object, System.IntPtr)""
  IL_0020:  dup
  IL_0021:  stsfld     ""System.Func<int, bool> C.<>c.<>9__0_0""
  IL_0026:  call       ""System.Collections.Generic.IEnumerable<int> System.Linq.Enumerable.Where<int>(System.Collections.Generic.IEnumerable<int>, System.Func<int, bool>)""
  IL_002b:  stloc.0
  IL_002c:  ret
}
");
        }

        [Fact]
        public void Queries_GroupBy_Reduced1()
        {
            var source0 = MarkedSource(@"
using System;
using System.Linq;
using System.Collections.Generic;

class C
{
    void F()
    {
        var <N:0>result = from a in array
                          <N:1>group a by a</N:1></N:0>;
    }

    int[] array = null;
}
");
            var source1 = MarkedSource(@"
using System;
using System.Linq;
using System.Collections.Generic;

class C
{
    void F()
    {
        var <N:0>result = from a in array
                          <N:1>group a + 1 by a</N:1></N:0>;
    }

    int[] array = null;
}
");
            var compilation0 = CreateCompilation(source0.Tree, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);
            var v0 = CompileAndVerify(compilation0);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;

            // new lambda for GroupBy(..., a => a + 1)
            diff1.VerifySynthesizedMembers(
                "C: {<>c}",
                "C.<>c: {<>9__0_0, <>9__0_1#1, <F>b__0_0, <F>b__0_1#1}");

            diff1.VerifyIL("C.<>c.<F>b__0_1#1", @"
{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldc.i4.1
  IL_0002:  add
  IL_0003:  ret
}
");
            // old query:
            v0.VerifyIL("C.F", @"
{
  // Code size       45 (0x2d)
  .maxstack  3
  .locals init (System.Collections.Generic.IEnumerable<System.Linq.IGrouping<int, int>> V_0) //result
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldfld      ""int[] C.array""
  IL_0007:  ldsfld     ""System.Func<int, int> C.<>c.<>9__0_0""
  IL_000c:  dup
  IL_000d:  brtrue.s   IL_0026
  IL_000f:  pop
  IL_0010:  ldsfld     ""C.<>c C.<>c.<>9""
  IL_0015:  ldftn      ""int C.<>c.<F>b__0_0(int)""
  IL_001b:  newobj     ""System.Func<int, int>..ctor(object, System.IntPtr)""
  IL_0020:  dup
  IL_0021:  stsfld     ""System.Func<int, int> C.<>c.<>9__0_0""
  IL_0026:  call       ""System.Collections.Generic.IEnumerable<System.Linq.IGrouping<int, int>> System.Linq.Enumerable.GroupBy<int, int>(System.Collections.Generic.IEnumerable<int>, System.Func<int, int>)""
  IL_002b:  stloc.0
  IL_002c:  ret
}
");
            // new query:
            diff1.VerifyIL("C.F", @"
{
  // Code size       76 (0x4c)
  .maxstack  4
  .locals init (System.Collections.Generic.IEnumerable<System.Linq.IGrouping<int, int>> V_0) //result
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldfld      ""int[] C.array""
  IL_0007:  ldsfld     ""System.Func<int, int> C.<>c.<>9__0_0""
  IL_000c:  dup
  IL_000d:  brtrue.s   IL_0026
  IL_000f:  pop
  IL_0010:  ldsfld     ""C.<>c C.<>c.<>9""
  IL_0015:  ldftn      ""int C.<>c.<F>b__0_0(int)""
  IL_001b:  newobj     ""System.Func<int, int>..ctor(object, System.IntPtr)""
  IL_0020:  dup
  IL_0021:  stsfld     ""System.Func<int, int> C.<>c.<>9__0_0""
  IL_0026:  ldsfld     ""System.Func<int, int> C.<>c.<>9__0_1#1""
  IL_002b:  dup
  IL_002c:  brtrue.s   IL_0045
  IL_002e:  pop
  IL_002f:  ldsfld     ""C.<>c C.<>c.<>9""
  IL_0034:  ldftn      ""int C.<>c.<F>b__0_1#1(int)""
  IL_003a:  newobj     ""System.Func<int, int>..ctor(object, System.IntPtr)""
  IL_003f:  dup
  IL_0040:  stsfld     ""System.Func<int, int> C.<>c.<>9__0_1#1""
  IL_0045:  call       ""System.Collections.Generic.IEnumerable<System.Linq.IGrouping<int, int>> System.Linq.Enumerable.GroupBy<int, int, int>(System.Collections.Generic.IEnumerable<int>, System.Func<int, int>, System.Func<int, int>)""
  IL_004a:  stloc.0
  IL_004b:  ret
}
");
        }

        [Fact]
        public void Queries_GroupBy_Reduced2()
        {
            var source0 = MarkedSource(@"
using System;
using System.Linq;
using System.Collections.Generic;

class C
{
    void F()
    {
        var <N:0>result = from a in array
                          <N:1>group a + 1 by a</N:1></N:0>;
    }

    int[] array = null;
}
");
            var source1 = MarkedSource(@"
using System;
using System.Linq;
using System.Collections.Generic;

class C
{
    void F()
    {
        var <N:0>result = from a in array
                          <N:1>group a by a</N:1></N:0>;
    }

    int[] array = null;
}
");
            var compilation0 = CreateCompilation(source0.Tree, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);
            var v0 = CompileAndVerify(compilation0);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;

            // lambda for GroupBy(..., a => a + 1) is gone
            diff1.VerifySynthesizedMembers(
                "C: {<>c}",
                "C.<>c: {<>9__0_0, <F>b__0_0}");

            // old query:
            v0.VerifyIL("C.F", @"
{
  // Code size       76 (0x4c)
  .maxstack  4
  .locals init (System.Collections.Generic.IEnumerable<System.Linq.IGrouping<int, int>> V_0) //result
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldfld      ""int[] C.array""
  IL_0007:  ldsfld     ""System.Func<int, int> C.<>c.<>9__0_0""
  IL_000c:  dup
  IL_000d:  brtrue.s   IL_0026
  IL_000f:  pop
  IL_0010:  ldsfld     ""C.<>c C.<>c.<>9""
  IL_0015:  ldftn      ""int C.<>c.<F>b__0_0(int)""
  IL_001b:  newobj     ""System.Func<int, int>..ctor(object, System.IntPtr)""
  IL_0020:  dup
  IL_0021:  stsfld     ""System.Func<int, int> C.<>c.<>9__0_0""
  IL_0026:  ldsfld     ""System.Func<int, int> C.<>c.<>9__0_1""
  IL_002b:  dup
  IL_002c:  brtrue.s   IL_0045
  IL_002e:  pop
  IL_002f:  ldsfld     ""C.<>c C.<>c.<>9""
  IL_0034:  ldftn      ""int C.<>c.<F>b__0_1(int)""
  IL_003a:  newobj     ""System.Func<int, int>..ctor(object, System.IntPtr)""
  IL_003f:  dup
  IL_0040:  stsfld     ""System.Func<int, int> C.<>c.<>9__0_1""
  IL_0045:  call       ""System.Collections.Generic.IEnumerable<System.Linq.IGrouping<int, int>> System.Linq.Enumerable.GroupBy<int, int, int>(System.Collections.Generic.IEnumerable<int>, System.Func<int, int>, System.Func<int, int>)""
  IL_004a:  stloc.0
  IL_004b:  ret
}
");
            // new query:
            diff1.VerifyIL("C.F", @"
{
  // Code size       45 (0x2d)
  .maxstack  3
  .locals init (System.Collections.Generic.IEnumerable<System.Linq.IGrouping<int, int>> V_0) //result
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldfld      ""int[] C.array""
  IL_0007:  ldsfld     ""System.Func<int, int> C.<>c.<>9__0_0""
  IL_000c:  dup
  IL_000d:  brtrue.s   IL_0026
  IL_000f:  pop
  IL_0010:  ldsfld     ""C.<>c C.<>c.<>9""
  IL_0015:  ldftn      ""int C.<>c.<F>b__0_0(int)""
  IL_001b:  newobj     ""System.Func<int, int>..ctor(object, System.IntPtr)""
  IL_0020:  dup
  IL_0021:  stsfld     ""System.Func<int, int> C.<>c.<>9__0_0""
  IL_0026:  call       ""System.Collections.Generic.IEnumerable<System.Linq.IGrouping<int, int>> System.Linq.Enumerable.GroupBy<int, int>(System.Collections.Generic.IEnumerable<int>, System.Func<int, int>)""
  IL_002b:  stloc.0
  IL_002c:  ret
}
");
        }

        [Fact, WorkItem(1170899, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1170899")]
        public void CapturedAnonymousTypes1()
        {
            var source0 = MarkedSource(@"
using System;

class C
{
    static void F()
    <N:0>{
        var <N:1>x = new { A = 1 }</N:1>;
        var <N:2>y = new Func<int>(<N:3>() => x.A</N:3>)</N:2>;
        Console.WriteLine(1);
    }</N:0>
}
");
            var source1 = MarkedSource(@"
using System;

class C
{
    static void F()
    <N:0>{
        var <N:1>x = new { A = 1 }</N:1>;
        var <N:2>y = new Func<int>(<N:3>() => x.A</N:3>)</N:2>;
        Console.WriteLine(2);
    }</N:0>
}
");
            var source2 = MarkedSource(@"
using System;

class C
{
    static void F()
    <N:0>{
        var <N:1>x = new { A = 1 }</N:1>;
        var <N:2>y = new Func<int>(<N:3>() => x.A</N:3>)</N:2>;
        Console.WriteLine(3);
    }</N:0>
}
");
            var compilation0 = CreateCompilation(source0.Tree, options: ComSafeDebugDll);
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
  // Code size       40 (0x28)
  .maxstack  2
  .locals init (C.<>c__DisplayClass0_0 V_0, //CS$<>8__locals0
                System.Func<int> V_1) //y
  IL_0000:  newobj     ""C.<>c__DisplayClass0_0..ctor()""
  IL_0005:  stloc.0
  IL_0006:  nop
  IL_0007:  ldloc.0
  IL_0008:  ldc.i4.1
  IL_0009:  newobj     ""<>f__AnonymousType0<int>..ctor(int)""
  IL_000e:  stfld      ""<anonymous type: int A> C.<>c__DisplayClass0_0.x""
  IL_0013:  ldloc.0
  IL_0014:  ldftn      ""int C.<>c__DisplayClass0_0.<F>b__0()""
  IL_001a:  newobj     ""System.Func<int>..ctor(object, System.IntPtr)""
  IL_001f:  stloc.1
  IL_0020:  ldc.i4.1
  IL_0021:  call       ""void System.Console.WriteLine(int)""
  IL_0026:  nop
  IL_0027:  ret
}");

            var diff1 = compilation1.EmitDifference(generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            diff1.VerifySynthesizedMembers(
                "C: {<>c__DisplayClass0_0}",
                "C.<>c__DisplayClass0_0: {x, <F>b__0}",
                "<>f__AnonymousType0<<A>j__TPar>: {Equals, GetHashCode, ToString}");

            diff1.VerifyIL("C.F", @"
{
  // Code size       40 (0x28)
  .maxstack  2
  .locals init (C.<>c__DisplayClass0_0 V_0, //CS$<>8__locals0
                System.Func<int> V_1) //y
  IL_0000:  newobj     ""C.<>c__DisplayClass0_0..ctor()""
  IL_0005:  stloc.0
  IL_0006:  nop
  IL_0007:  ldloc.0
  IL_0008:  ldc.i4.1
  IL_0009:  newobj     ""<>f__AnonymousType0<int>..ctor(int)""
  IL_000e:  stfld      ""<anonymous type: int A> C.<>c__DisplayClass0_0.x""
  IL_0013:  ldloc.0
  IL_0014:  ldftn      ""int C.<>c__DisplayClass0_0.<F>b__0()""
  IL_001a:  newobj     ""System.Func<int>..ctor(object, System.IntPtr)""
  IL_001f:  stloc.1
  IL_0020:  ldc.i4.2
  IL_0021:  call       ""void System.Console.WriteLine(int)""
  IL_0026:  nop
  IL_0027:  ret
}");

            var diff2 = compilation2.EmitDifference(diff1.NextGeneration,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, f1, f2, GetSyntaxMapFromMarkers(source1, source2), preserveLocalVariables: true)));

            diff2.VerifySynthesizedMembers(
                "C: {<>c__DisplayClass0_0}",
                "C.<>c__DisplayClass0_0: {x, <F>b__0}",
                "<>f__AnonymousType0<<A>j__TPar>: {Equals, GetHashCode, ToString}");

            diff2.VerifyIL("C.F", @"
{
  // Code size       40 (0x28)
  .maxstack  2
  .locals init (C.<>c__DisplayClass0_0 V_0, //CS$<>8__locals0
                System.Func<int> V_1) //y
  IL_0000:  newobj     ""C.<>c__DisplayClass0_0..ctor()""
  IL_0005:  stloc.0
  IL_0006:  nop
  IL_0007:  ldloc.0
  IL_0008:  ldc.i4.1
  IL_0009:  newobj     ""<>f__AnonymousType0<int>..ctor(int)""
  IL_000e:  stfld      ""<anonymous type: int A> C.<>c__DisplayClass0_0.x""
  IL_0013:  ldloc.0
  IL_0014:  ldftn      ""int C.<>c__DisplayClass0_0.<F>b__0()""
  IL_001a:  newobj     ""System.Func<int>..ctor(object, System.IntPtr)""
  IL_001f:  stloc.1
  IL_0020:  ldc.i4.3
  IL_0021:  call       ""void System.Console.WriteLine(int)""
  IL_0026:  nop
  IL_0027:  ret
}");
        }

        [Fact, WorkItem(1170899, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1170899")]
        public void CapturedAnonymousTypes2()
        {
            var template = @"
using System;

class C
{
    static void F()
    <N:0>{
        var x = new { X = <<VALUE>> };
        Func<int> <N:2>y = <N:1>() => x.X</N:1></N:2>;
        Console.WriteLine(y());
    }</N:0>
}
";
            var source0 = MarkedSource(template.Replace("<<VALUE>>", "0"));
            var source1 = MarkedSource(template.Replace("<<VALUE>>", "1"));
            var source2 = MarkedSource(template.Replace("<<VALUE>>", "2"));

            var compilation0 = CreateCompilation(source0.Tree, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);
            var compilation2 = compilation1.WithSource(source2.Tree);

            var v0 = CompileAndVerify(compilation0);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");
            var f2 = compilation2.GetMember<MethodSymbol>("C.F");

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            string expectedIL = @"
{
  // Code size       45 (0x2d)
  .maxstack  2
  .locals init (C.<>c__DisplayClass0_0 V_0, //CS$<>8__locals0
                System.Func<int> V_1) //y
  IL_0000:  newobj     ""C.<>c__DisplayClass0_0..ctor()""
  IL_0005:  stloc.0
  IL_0006:  nop
  IL_0007:  ldloc.0
  IL_0008:  ldc.i4.<<VALUE>>
  IL_0009:  newobj     ""<>f__AnonymousType0<int>..ctor(int)""
  IL_000e:  stfld      ""<anonymous type: int X> C.<>c__DisplayClass0_0.x""
  IL_0013:  ldloc.0
  IL_0014:  ldftn      ""int C.<>c__DisplayClass0_0.<F>b__0()""
  IL_001a:  newobj     ""System.Func<int>..ctor(object, System.IntPtr)""
  IL_001f:  stloc.1
  IL_0020:  ldloc.1
  IL_0021:  callvirt   ""int System.Func<int>.Invoke()""
  IL_0026:  call       ""void System.Console.WriteLine(int)""
  IL_002b:  nop
  IL_002c:  ret
}";

            v0.VerifyIL("C.F", expectedIL.Replace("<<VALUE>>", "0"));

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            diff1.VerifySynthesizedMembers(
                "C: {<>c__DisplayClass0_0}",
                "C.<>c__DisplayClass0_0: {x, <F>b__0}",
                "<>f__AnonymousType0<<X>j__TPar>: {Equals, GetHashCode, ToString}");

            diff1.VerifyIL("C.F", expectedIL.Replace("<<VALUE>>", "1"));

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, f1, f2, GetSyntaxMapFromMarkers(source1, source2), preserveLocalVariables: true)));

            diff2.VerifySynthesizedMembers(
                "C: {<>c__DisplayClass0_0}",
                "C.<>c__DisplayClass0_0: {x, <F>b__0}",
                "<>f__AnonymousType0<<X>j__TPar>: {Equals, GetHashCode, ToString}");

            diff2.VerifyIL("C.F", expectedIL.Replace("<<VALUE>>", "2"));
        }

        [WorkItem(179990, "https://devdiv.visualstudio.com:443/defaultcollection/DevDiv/_workitems/edit/179990")]
        [Fact]
        public void SynthesizedDelegates()
        {
            var template =
@"class C
{
    static void F(dynamic d, out object x, object y)
    <N:0>{
        <<CALL>>;
    }</N:0>
}";
            var source0 = MarkedSource(template.Replace("<<CALL>>", "d.F(out x, new { })"));
            var source1 = MarkedSource(template.Replace("<<CALL>>", "d.F(out x, new { y })"));
            var source2 = MarkedSource(template.Replace("<<CALL>>", "d.F(new { y }, out x)"));

            var compilation0 = CreateCompilationWithMscorlib40(new[] { source0.Tree }, references: new[] { SystemCoreRef, CSharpRef }, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);
            var compilation2 = compilation1.WithSource(source2.Tree);

            var v0 = CompileAndVerify(compilation0);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");
            var f2 = compilation2.GetMember<MethodSymbol>("C.F");

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);
            v0.VerifyIL("C.F",
@"{
  // Code size      112 (0x70)
  .maxstack  9
  IL_0000:  nop
  IL_0001:  ldsfld     ""System.Runtime.CompilerServices.CallSite<<>A{00000004}<System.Runtime.CompilerServices.CallSite, dynamic, object, <empty anonymous type>>> C.<>o__0.<>p__0""
  IL_0006:  brfalse.s  IL_000a
  IL_0008:  br.s       IL_0053
  IL_000a:  ldc.i4     0x100
  IL_000f:  ldstr      ""F""
  IL_0014:  ldnull
  IL_0015:  ldtoken    ""C""
  IL_001a:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001f:  ldc.i4.3
  IL_0020:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0025:  dup
  IL_0026:  ldc.i4.0
  IL_0027:  ldc.i4.0
  IL_0028:  ldnull
  IL_0029:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002e:  stelem.ref
  IL_002f:  dup
  IL_0030:  ldc.i4.1
  IL_0031:  ldc.i4.s   17
  IL_0033:  ldnull
  IL_0034:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0039:  stelem.ref
  IL_003a:  dup
  IL_003b:  ldc.i4.2
  IL_003c:  ldc.i4.1
  IL_003d:  ldnull
  IL_003e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0043:  stelem.ref
  IL_0044:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0049:  call       ""System.Runtime.CompilerServices.CallSite<<>A{00000004}<System.Runtime.CompilerServices.CallSite, dynamic, object, <empty anonymous type>>> System.Runtime.CompilerServices.CallSite<<>A{00000004}<System.Runtime.CompilerServices.CallSite, dynamic, object, <empty anonymous type>>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_004e:  stsfld     ""System.Runtime.CompilerServices.CallSite<<>A{00000004}<System.Runtime.CompilerServices.CallSite, dynamic, object, <empty anonymous type>>> C.<>o__0.<>p__0""
  IL_0053:  ldsfld     ""System.Runtime.CompilerServices.CallSite<<>A{00000004}<System.Runtime.CompilerServices.CallSite, dynamic, object, <empty anonymous type>>> C.<>o__0.<>p__0""
  IL_0058:  ldfld      ""<>A{00000004}<System.Runtime.CompilerServices.CallSite, dynamic, object, <empty anonymous type>> System.Runtime.CompilerServices.CallSite<<>A{00000004}<System.Runtime.CompilerServices.CallSite, dynamic, object, <empty anonymous type>>>.Target""
  IL_005d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<<>A{00000004}<System.Runtime.CompilerServices.CallSite, dynamic, object, <empty anonymous type>>> C.<>o__0.<>p__0""
  IL_0062:  ldarg.0
  IL_0063:  ldarg.1
  IL_0064:  newobj     ""<>f__AnonymousType0..ctor()""
  IL_0069:  callvirt   ""void <>A{00000004}<System.Runtime.CompilerServices.CallSite, dynamic, object, <empty anonymous type>>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, ref object, <empty anonymous type>)""
  IL_006e:  nop
  IL_006f:  ret
}");

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));
            diff1.VerifyIL("C.F",
@"{
  // Code size      113 (0x71)
  .maxstack  9
  IL_0000:  nop
  IL_0001:  ldsfld     ""System.Runtime.CompilerServices.CallSite<<>A{00000004}#1<System.Runtime.CompilerServices.CallSite, dynamic, object, <anonymous type: object y>>> C.<>o__0#1.<>p__0""
  IL_0006:  brfalse.s  IL_000a
  IL_0008:  br.s       IL_0053
  IL_000a:  ldc.i4     0x100
  IL_000f:  ldstr      ""F""
  IL_0014:  ldnull
  IL_0015:  ldtoken    ""C""
  IL_001a:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001f:  ldc.i4.3
  IL_0020:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0025:  dup
  IL_0026:  ldc.i4.0
  IL_0027:  ldc.i4.0
  IL_0028:  ldnull
  IL_0029:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002e:  stelem.ref
  IL_002f:  dup
  IL_0030:  ldc.i4.1
  IL_0031:  ldc.i4.s   17
  IL_0033:  ldnull
  IL_0034:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0039:  stelem.ref
  IL_003a:  dup
  IL_003b:  ldc.i4.2
  IL_003c:  ldc.i4.1
  IL_003d:  ldnull
  IL_003e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0043:  stelem.ref
  IL_0044:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0049:  call       ""System.Runtime.CompilerServices.CallSite<<>A{00000004}#1<System.Runtime.CompilerServices.CallSite, dynamic, object, <anonymous type: object y>>> System.Runtime.CompilerServices.CallSite<<>A{00000004}#1<System.Runtime.CompilerServices.CallSite, dynamic, object, <anonymous type: object y>>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_004e:  stsfld     ""System.Runtime.CompilerServices.CallSite<<>A{00000004}#1<System.Runtime.CompilerServices.CallSite, dynamic, object, <anonymous type: object y>>> C.<>o__0#1.<>p__0""
  IL_0053:  ldsfld     ""System.Runtime.CompilerServices.CallSite<<>A{00000004}#1<System.Runtime.CompilerServices.CallSite, dynamic, object, <anonymous type: object y>>> C.<>o__0#1.<>p__0""
  IL_0058:  ldfld      ""<>A{00000004}#1<System.Runtime.CompilerServices.CallSite, dynamic, object, <anonymous type: object y>> System.Runtime.CompilerServices.CallSite<<>A{00000004}#1<System.Runtime.CompilerServices.CallSite, dynamic, object, <anonymous type: object y>>>.Target""
  IL_005d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<<>A{00000004}#1<System.Runtime.CompilerServices.CallSite, dynamic, object, <anonymous type: object y>>> C.<>o__0#1.<>p__0""
  IL_0062:  ldarg.0
  IL_0063:  ldarg.1
  IL_0064:  ldarg.2
  IL_0065:  newobj     ""<>f__AnonymousType1<object>..ctor(object)""
  IL_006a:  callvirt   ""void <>A{00000004}#1<System.Runtime.CompilerServices.CallSite, dynamic, object, <anonymous type: object y>>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, ref object, <anonymous type: object y>)""
  IL_006f:  nop
  IL_0070:  ret
}");

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, f1, f2, GetSyntaxMapFromMarkers(source1, source2), preserveLocalVariables: true)));
            diff2.VerifyIL("C.F",
@"{
  // Code size      113 (0x71)
  .maxstack  9
  IL_0000:  nop
  IL_0001:  ldsfld     ""System.Runtime.CompilerServices.CallSite<<>A{00000008}#2<System.Runtime.CompilerServices.CallSite, dynamic, <anonymous type: object y>, object>> C.<>o__0#2.<>p__0""
  IL_0006:  brfalse.s  IL_000a
  IL_0008:  br.s       IL_0053
  IL_000a:  ldc.i4     0x100
  IL_000f:  ldstr      ""F""
  IL_0014:  ldnull
  IL_0015:  ldtoken    ""C""
  IL_001a:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001f:  ldc.i4.3
  IL_0020:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0025:  dup
  IL_0026:  ldc.i4.0
  IL_0027:  ldc.i4.0
  IL_0028:  ldnull
  IL_0029:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_002e:  stelem.ref
  IL_002f:  dup
  IL_0030:  ldc.i4.1
  IL_0031:  ldc.i4.1
  IL_0032:  ldnull
  IL_0033:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0038:  stelem.ref
  IL_0039:  dup
  IL_003a:  ldc.i4.2
  IL_003b:  ldc.i4.s   17
  IL_003d:  ldnull
  IL_003e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0043:  stelem.ref
  IL_0044:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0049:  call       ""System.Runtime.CompilerServices.CallSite<<>A{00000008}#2<System.Runtime.CompilerServices.CallSite, dynamic, <anonymous type: object y>, object>> System.Runtime.CompilerServices.CallSite<<>A{00000008}#2<System.Runtime.CompilerServices.CallSite, dynamic, <anonymous type: object y>, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_004e:  stsfld     ""System.Runtime.CompilerServices.CallSite<<>A{00000008}#2<System.Runtime.CompilerServices.CallSite, dynamic, <anonymous type: object y>, object>> C.<>o__0#2.<>p__0""
  IL_0053:  ldsfld     ""System.Runtime.CompilerServices.CallSite<<>A{00000008}#2<System.Runtime.CompilerServices.CallSite, dynamic, <anonymous type: object y>, object>> C.<>o__0#2.<>p__0""
  IL_0058:  ldfld      ""<>A{00000008}#2<System.Runtime.CompilerServices.CallSite, dynamic, <anonymous type: object y>, object> System.Runtime.CompilerServices.CallSite<<>A{00000008}#2<System.Runtime.CompilerServices.CallSite, dynamic, <anonymous type: object y>, object>>.Target""
  IL_005d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<<>A{00000008}#2<System.Runtime.CompilerServices.CallSite, dynamic, <anonymous type: object y>, object>> C.<>o__0#2.<>p__0""
  IL_0062:  ldarg.0
  IL_0063:  ldarg.2
  IL_0064:  newobj     ""<>f__AnonymousType1<object>..ctor(object)""
  IL_0069:  ldarg.1
  IL_006a:  callvirt   ""void <>A{00000008}#2<System.Runtime.CompilerServices.CallSite, dynamic, <anonymous type: object y>, object>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, <anonymous type: object y>, ref object)""
  IL_006f:  nop
  IL_0070:  ret
}");
        }

        [Fact]
        public void TwoStructClosures()
        {
            var source0 = MarkedSource(@"
public class C 
{
    public void F()            
    <N:0>{</N:0>
        int <N:1>x</N:1> = 0;
        <N:2>{</N:2>
            int <N:3>y</N:3> = 0;
            // Captures two struct closures
            <N:4>int L() => x + y;</N:4>
        }
    }
}");

            var source1 = MarkedSource(@"
public class C 
{
    public void F()            
<N:0>{</N:0>
        int <N:1>x</N:1> = 0;
        <N:2>{</N:2>
            int <N:3>y</N:3> = 0;
            // Captures two struct closures
            <N:4>int L() => x + y + 1;</N:4>
        }
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
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;

            diff1.VerifySynthesizedMembers(
                "C.<>c__DisplayClass0_0: {x}",
                "C.<>c__DisplayClass0_1: {y}",
                "C: {<F>g__L|0_0, <>c__DisplayClass0_0, <>c__DisplayClass0_1}");

            v0.VerifyIL("C.<F>g__L|0_0(ref C.<>c__DisplayClass0_0, ref C.<>c__DisplayClass0_1)", @"
{
  // Code size       14 (0xe)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<>c__DisplayClass0_0.x""
  IL_0006:  ldarg.1
  IL_0007:  ldfld      ""int C.<>c__DisplayClass0_1.y""
  IL_000c:  add
  IL_000d:  ret
}");

            diff1.VerifyIL("C.<F>g__L|0_0(ref C.<>c__DisplayClass0_0, ref C.<>c__DisplayClass0_1)", @"
{
  // Code size       16 (0x10)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<>c__DisplayClass0_0.x""
  IL_0006:  ldarg.1
  IL_0007:  ldfld      ""int C.<>c__DisplayClass0_1.y""
  IL_000c:  add
  IL_000d:  ldc.i4.1
  IL_000e:  add
  IL_000f:  ret
}
");

            CheckEncLogDefinitions(reader1,
                Row(2, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(3, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(7, TableIndex.CustomAttribute, EditAndContinueOperation.Default));
        }

        [Fact]
        public void ThisClosureAndStructClosure()
        {
            var source0 = MarkedSource(@"
public class C 
{
    int <N:0>x</N:0> = 0;
    public void F() 
    <N:1>{</N:1>
        int <N:2>y</N:2> = 0;
        // This + struct closures
        <N:3>int L() => x + y;</N:3>
        L();
    }
}");

            var source1 = MarkedSource(@"
public class C 
{
    int <N:0>x</N:0> = 0;
    public void F() 
    <N:1>{</N:1>
        int <N:2>y</N:2> = 0;
        // This + struct closures
        <N:3>int L() => x + y + 1;</N:3>
        L();
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
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;

            diff1.VerifySynthesizedMembers(
                "C.<>c__DisplayClass1_0: {<>4__this, y}",
                "C: {<F>g__L|1_0, <>c__DisplayClass1_0}");

            v0.VerifyIL("C.<F>g__L|1_0(ref C.<>c__DisplayClass1_0)", @"
{
  // Code size       14 (0xe)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.x""
  IL_0006:  ldarg.1
  IL_0007:  ldfld      ""int C.<>c__DisplayClass1_0.y""
  IL_000c:  add
  IL_000d:  ret
}
");

            diff1.VerifyIL("C.<F>g__L|1_0(ref C.<>c__DisplayClass1_0)", @"
{
  // Code size       16 (0x10)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.x""
  IL_0006:  ldarg.1
  IL_0007:  ldfld      ""int C.<>c__DisplayClass1_0.y""
  IL_000c:  add
  IL_000d:  ldc.i4.1
  IL_000e:  add
  IL_000f:  ret
}
");

            CheckEncLogDefinitions(reader1,
                Row(2, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(3, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(6, TableIndex.CustomAttribute, EditAndContinueOperation.Default));
        }

        [Fact]
        public void ThisOnlyClosure()
        {
            var source0 = MarkedSource(@"
public class C 
{
    int <N:0>x</N:0> = 0;
    public void F() 
    <N:1>{</N:1>
        // This-only closure
        <N:2>int L() => x;</N:2>
        L();
    }
}");

            var source1 = MarkedSource(@"
public class C 
{
    int <N:0>x</N:0> = 0;
    public void F() 
    <N:1>{</N:1>
        // This-only closure
        <N:2>int L() => x + 1;</N:2>
        L();
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
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;

            diff1.VerifySynthesizedMembers(
                "C: {<F>g__L|1_0}");

            v0.VerifyIL("C.<F>g__L|1_0()", @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.x""
  IL_0006:  ret
}");

            diff1.VerifyIL("C.<F>g__L|1_0()", @"
{
  // Code size        9 (0x9)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.x""
  IL_0006:  ldc.i4.1
  IL_0007:  add
  IL_0008:  ret
}
");

            CheckEncLogDefinitions(reader1,
                Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(3, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(5, TableIndex.CustomAttribute, EditAndContinueOperation.Default));
        }

        [Fact]
        public void LocatedInSameClosureEnvironment()
        {
            var source0 = MarkedSource(@"
using System;
public class C 
{
    public void F(int x) 
    <N:0>{</N:0>
        Func<int> f = <N:1>() => x</N:1>;
        // Located in same closure environment
        <N:2>int L() => x;</N:2>
    }
}");

            var source1 = MarkedSource(@"
using System;
public class C 
{
    public void F(int x) 
    <N:0>{</N:0>
        Func<int> f = <N:1>() => x</N:1>;
        // Located in same closure environment
        <N:2>int L() => x + 1;</N:2>
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
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;

            diff1.VerifySynthesizedMembers(
                "C.<>c__DisplayClass0_0: {x, <F>b__0, <F>g__L|1}",
                "C: {<>c__DisplayClass0_0}");

            v0.VerifyIL("C.<>c__DisplayClass0_0.<F>g__L|1()", @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<>c__DisplayClass0_0.x""
  IL_0006:  ret
}");

            diff1.VerifyIL("C.<>c__DisplayClass0_0.<F>g__L|1()", @"
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

            CheckEncLogDefinitions(reader1,
                Row(2, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default));
        }

        [Fact]
        public void SameClassEnvironmentWithStruct()
        {
            var source0 = MarkedSource(@"
using System;
public class C 
{
    public void F(int x) 
    <N:0>{</N:0>
        <N:1>{</N:1>
            int <N:2>y</N:2> = 0;
            Func<int> f = <N:3>() => x</N:3>;
            // Same class environment, with struct env
            <N:4>int L() => x + y;</N:4>
        }
    }
}");

            var source1 = MarkedSource(@"
using System;
public class C 
{
    public void F(int x) 
    <N:0>{</N:0>
        <N:1>{</N:1>
            int <N:2>y</N:2> = 0;
            Func<int> f = <N:3>() => x</N:3>;
            // Same class environment, with struct env
            <N:4>int L() => x + y + 1;</N:4>
        }
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
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;

            diff1.VerifySynthesizedMembers(
                "C: {<>c__DisplayClass0_0, <>c__DisplayClass0_1}",
                "C.<>c__DisplayClass0_0: {x, <F>b__0, <F>g__L|1}",
                "C.<>c__DisplayClass0_1: {y}");

            v0.VerifyIL("C.<>c__DisplayClass0_0.<F>g__L|1(ref C.<>c__DisplayClass0_1)", @"
{
  // Code size       14 (0xe)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<>c__DisplayClass0_0.x""
  IL_0006:  ldarg.1
  IL_0007:  ldfld      ""int C.<>c__DisplayClass0_1.y""
  IL_000c:  add
  IL_000d:  ret
}");

            diff1.VerifyIL("C.<>c__DisplayClass0_0.<F>g__L|1(ref C.<>c__DisplayClass0_1)", @"
{
  // Code size       16 (0x10)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<>c__DisplayClass0_0.x""
  IL_0006:  ldarg.1
  IL_0007:  ldfld      ""int C.<>c__DisplayClass0_1.y""
  IL_000c:  add
  IL_000d:  ldc.i4.1
  IL_000e:  add
  IL_000f:  ret
}
");

            CheckEncLogDefinitions(reader1,
                Row(2, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default));
        }

        [Fact]
        public void CaptureStructAndThroughClassEnvChain()
        {
            var source0 = MarkedSource(@"
using System;
public class C 
{
    public void F(int x) 
    <N:0>{</N:0>
        <N:1>{</N:1>
            int <N:2>y</N:2> = 0;
            Func<int> f = <N:3>() => x</N:3>;
            <N:4>{</N:4>
                Func<int> f2 = <N:5>() => x + y</N:5>;
                int <N:6>z</N:6> = 0;
                // Capture struct and through class env chain
                <N:7>int L() => x + y + z;</N:7>
            }
        }
    }
}");

            var source1 = MarkedSource(@"
using System;
public class C 
{
    public void F(int x) 
<N:0>{</N:0>
        <N:1>{</N:1>
            int <N:2>y</N:2> = 0;
            Func<int> f = <N:3>() => x</N:3>;
            <N:4>{</N:4>
                Func<int> f2 = <N:5>() => x + y</N:5>;
                int <N:6>z</N:6> = 0;
                // Capture struct and through class env chain
                <N:7>int L() => x + y + z + 1;</N:7>
            }
        }
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
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;

            diff1.VerifySynthesizedMembers(
                "C.<>c__DisplayClass0_2: {z}",
                "C.<>c__DisplayClass0_0: {x, <F>b__0}",
                "C: {<>c__DisplayClass0_0, <>c__DisplayClass0_1, <>c__DisplayClass0_2}",
                "C.<>c__DisplayClass0_1: {y, CS$<>8__locals1, <F>b__1, <F>g__L|2}");

            v0.VerifyIL("C.<>c__DisplayClass0_1.<F>g__L|2(ref C.<>c__DisplayClass0_2)", @"
{
  // Code size       26 (0x1a)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.<>c__DisplayClass0_0 C.<>c__DisplayClass0_1.CS$<>8__locals1""
  IL_0006:  ldfld      ""int C.<>c__DisplayClass0_0.x""
  IL_000b:  ldarg.0
  IL_000c:  ldfld      ""int C.<>c__DisplayClass0_1.y""
  IL_0011:  add
  IL_0012:  ldarg.1
  IL_0013:  ldfld      ""int C.<>c__DisplayClass0_2.z""
  IL_0018:  add
  IL_0019:  ret
}");

            diff1.VerifyIL("C.<>c__DisplayClass0_1.<F>g__L|2(ref C.<>c__DisplayClass0_2)", @"
{
  // Code size       28 (0x1c)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.<>c__DisplayClass0_0 C.<>c__DisplayClass0_1.CS$<>8__locals1""
  IL_0006:  ldfld      ""int C.<>c__DisplayClass0_0.x""
  IL_000b:  ldarg.0
  IL_000c:  ldfld      ""int C.<>c__DisplayClass0_1.y""
  IL_0011:  add
  IL_0012:  ldarg.1
  IL_0013:  ldfld      ""int C.<>c__DisplayClass0_2.z""
  IL_0018:  add
  IL_0019:  ldc.i4.1
  IL_001a:  add
  IL_001b:  ret
}
");

            CheckEncLogDefinitions(reader1,
                Row(2, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(6, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(7, TableIndex.MethodDef, EditAndContinueOperation.Default));
        }
    }
}
