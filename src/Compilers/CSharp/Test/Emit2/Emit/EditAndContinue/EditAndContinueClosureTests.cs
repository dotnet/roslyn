// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
            using var _ = new EditAndContinueTest()
                .AddBaseline(
                    """
                    delegate object D();
                    class C
                    {
                        static void F()
                        {
                        }

                        static void F(object o)
                        {
                        }
                    
                        static void F(bool a, bool b)
                        {
                        }
                    }
                    """)
                .AddGeneration(
                    """
                    delegate object D();
                    class C
                    {
                        static void F()
                        {
                            int x = 1;
                            D d = () => x;
                        }

                        static void F(object o)
                        {
                            D d1 = () => o;
                            D d2 = () => o;
                        }
                    
                        static void F(bool a, bool b)
                        {
                            D d = () => null;
                        }
                    }
                    """,
                    edits:
                    [
                        Edit(SemanticEditKind.Update, c => c.GetMembers<IMethodSymbol>("C.F").Single(m => m.Parameters is []), preserveLocalVariables: true),
                        Edit(SemanticEditKind.Update, c => c.GetMembers<IMethodSymbol>("C.F").Single(m => m.Parameters is [_]), preserveLocalVariables: true),
                        Edit(SemanticEditKind.Update, c => c.GetMembers<IMethodSymbol>("C.F").Single(m => m.Parameters is [_, _]), preserveLocalVariables: true),
                    ],
                    validator: g =>
                    {
                        // Note that the synthesized names have generation #1 suffix. This is because baseline methods did not have any lambdas
                        // and thus no MethodId.
                        g.VerifySynthesizedMembers(
                            "C: {<>c, <>c__DisplayClass0#1_0#1, <>c__DisplayClass1#1_0#1}",
                            "C.<>c: {<>9__2#1_0#1, <F>b__2#1_0#1}",
                            "C.<>c__DisplayClass0#1_0#1: {x, <F>b__0#1}",
                            "C.<>c__DisplayClass1#1_0#1: {o, <F>b__0#1, <F>b__1#1}");

                        g.VerifyEncLogDefinitions(
                        [
                            Row(1, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                            Row(2, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                            Row(3, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                            Row(4, TableIndex.TypeDef, EditAndContinueOperation.Default),
                            Row(5, TableIndex.TypeDef, EditAndContinueOperation.Default),
                            Row(6, TableIndex.TypeDef, EditAndContinueOperation.Default),
                            Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(1, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(2, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(5, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(3, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(6, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(4, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(6, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(7, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                            Row(9, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                            Row(10, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                            Row(11, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(5, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                            Row(12, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(5, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                            Row(13, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(6, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                            Row(14, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(6, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                            Row(15, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(6, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                            Row(16, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(6, TableIndex.Param, EditAndContinueOperation.Default),
                            Row(7, TableIndex.Param, EditAndContinueOperation.Default),
                            Row(8, TableIndex.Param, EditAndContinueOperation.Default),
                            Row(4, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(5, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(6, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(1, TableIndex.NestedClass, EditAndContinueOperation.Default),
                            Row(2, TableIndex.NestedClass, EditAndContinueOperation.Default),
                            Row(3, TableIndex.NestedClass, EditAndContinueOperation.Default)
                        ]);
                    })
                .Verify();
        }

        [Fact]
        public void MethodToMethodWithClosure_DeletedAndReadded()
        {
            using var _ = new EditAndContinueTest()
                .AddBaseline(
                    """
                    using System;
                    class C
                    {
                        static void F()
                        {
                        }
                    }
                    """)
                .AddGeneration(
                    // 1
                    source: """
                    using System;
                    class C
                    {
                        static void F()
                        <N:0>{
                            int <N:1>x = 1</N:1>;
                            var d = new Func<int>(<N:2>() => x</N:2>);
                        }</N:0>
                    }
                    """,
                    edits:
                    [
                        Edit(SemanticEditKind.Update, c => c.GetMember("C.F"), preserveLocalVariables: true),
                    ],
                    validator: g =>
                    {
                        g.VerifySynthesizedMembers(
                            "C: {<>c__DisplayClass0#1_0#1}",
                            "C.<>c__DisplayClass0#1_0#1: {x, <F>b__0#1}");
                    })
                .AddGeneration(
                    // 2
                    source: """
                    using System;
                    class C
                    {
                        static void F()
                        <N:0>{
                            int <N:1>x = 1</N:1>;
                        }</N:0>
                    }
                    """,
                    edits:
                    [
                        Edit(SemanticEditKind.Update, c => c.GetMember("C.F"), preserveLocalVariables: true),
                    ],
                    validator: g =>
                    {
                        g.VerifySynthesizedMembers(
                            "System.Runtime.CompilerServices.HotReloadException",
                            "C: {<>c__DisplayClass0#1_0#1}",
                            "C.<>c__DisplayClass0#1_0#1: {x, <F>b__0#1}");
                    })
                .AddGeneration(
                    // 3
                    source: """
                    using System;
                    class C
                    {
                        static void F()
                        <N:0>{
                            int <N:1>x = 1</N:1>;
                            var d = new Func<bool>(() => x == 1);
                        }</N:0>
                    }
                    """,
                    edits:
                    [
                        Edit(SemanticEditKind.Update, c => c.GetMember("C.F"), preserveLocalVariables: true),
                    ],
                    validator: g =>
                    {
                        g.VerifySynthesizedMembers(
                            "System.Runtime.CompilerServices.HotReloadException",
                            "C: {<>c__DisplayClass0#1_0#3, <>c__DisplayClass0#1_0#1}",
                            "C.<>c__DisplayClass0#1_0#3: {x, <F>b__0#3}",
                            "C.<>c__DisplayClass0#1_0#1: {x, <F>b__0#1}");
                    })
                .Verify();
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

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

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
        public void MethodWithSwitchExpression()
        {
            var source0 = MarkedSource(@"
using System;

class C
{
    int F(object o)
    {
        <N:0>return o switch
        {
            int i => new Func<int>(<N:1>() => i + 1</N:1>)(),
            _ => 0
        }</N:0>;
    }
}");
            var source1 = MarkedSource(@"
using System;

class C
{
    int F(object o)
    {
        <N:0>return o switch
        {
            int i => new Func<int>(<N:1>() => i + 2</N:1>)(),
            _ => 0
        }</N:0>;
    }
}");
            var compilation0 = CreateCompilation(source0.Tree, options: ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            var compilation1 = compilation0.WithSource(source1.Tree);

            var v0 = CompileAndVerify(compilation0);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

            // no new synthesized members generated (with #1 in names):
            diff1.VerifySynthesizedMembers(
                "C.<>c__DisplayClass0_0: {<i>5__2, <F>b__0}",
                "C: {<>c__DisplayClass0_0}");

            var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;

            var x = Visualize(generation0.OriginalMetadata, md1);

            // Method updates
            CheckEncLogDefinitions(reader1,
                Row(2, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(1, TableIndex.Param, EditAndContinueOperation.Default));
        }

        [Fact]
        public void MethodWithNestedSwitchExpression()
        {
            var source0 = MarkedSource(@"
using System;

class C
{
    int F(object o)
    <N:0>{
        <N:1>return o switch
        {
            int i => new Func<int>(<N:2>() => i + (int)o + i switch
                {
                    1 => 1,
                    _ => new Func<int>(<N:3>() => (int)o + 1</N:3>)()
                }</N:2>)(),
            _ => 0
        }</N:1>;
    }</N:0>
}");
            var source1 = MarkedSource(@"
using System;

class C
{
    int F(object o)
    <N:0>{
        <N:1>return o switch
        {
            int i => new Func<int>(<N:2>() => i + (int)o + i switch
                {
                    1 => 1,
                    _ => new Func<int>(<N:3>() => (int)o + 2</N:3>)()
                }</N:2>)(),
            _ => 0
        }</N:1>;
    }</N:0>
}");
            var compilation0 = CreateCompilation(source0.Tree, options: ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            var compilation1 = compilation0.WithSource(source1.Tree);

            var v0 = CompileAndVerify(compilation0);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

            // no new synthesized members generated (with #1 in names):
            diff1.VerifySynthesizedMembers(
                "C: {<>c__DisplayClass0_0, <>c__DisplayClass0_1}",
                "C.<>c__DisplayClass0_1: {<i>5__2, CS$<>8__locals2, <F>b__0}",
                "C.<>c__DisplayClass0_0: {o, <>9__1, <F>b__1}");

            var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;

            // Method updates
            CheckEncLogDefinitions(reader1,
                Row(3, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(4, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(6, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(1, TableIndex.Param, EditAndContinueOperation.Default));
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

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

            diff1.VerifySynthesizedMembers(
                "C: {<F>g__x|0_0}");

            var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;

            // Method updates
            CheckEncLogDefinitions(reader1,
                Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(3, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(4, TableIndex.CustomAttribute, EditAndContinueOperation.Default));
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

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

            diff1.VerifySynthesizedMembers(
                "C: {<F>g__x|0_0}");

            var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;

            // Method updates
            CheckEncLogDefinitions(reader1,
                Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(3, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(4, TableIndex.CustomAttribute, EditAndContinueOperation.Default));

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

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

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

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

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
                Row(4, TableIndex.CustomAttribute, EditAndContinueOperation.Default));
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

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

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
                Row(1, TableIndex.Param, EditAndContinueOperation.Default),
                Row(4, TableIndex.CustomAttribute, EditAndContinueOperation.Default));
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

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

            diff1.VerifySynthesizedMembers(
                "C: {<F>g__x|0_0}");

            var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;

            // Method updates
            CheckEncLogDefinitions(reader1,
                Row(2, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(3, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(1, TableIndex.Param, EditAndContinueOperation.Default),
                Row(4, TableIndex.CustomAttribute, EditAndContinueOperation.Default));
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

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

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
                Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(1, TableIndex.Param, EditAndContinueOperation.Default));
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
}", options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp9));
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
}", options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp9));
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

            diff1.VerifySynthesizedMembers(
                "Microsoft.CodeAnalysis.EmbeddedAttribute",
                "System.Runtime.CompilerServices.NullableAttribute",
                "System.Runtime.CompilerServices.NullableContextAttribute",
                "C: {<>c__DisplayClass2_0}",
                "C.<>c__DisplayClass2_0: {x, y1, y2, <F>b__0, <F>b__1}",
                "<>f__AnonymousType1<<B>j__TPar>: {Equals, GetHashCode, ToString}",
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

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

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
                Row(1, TableIndex.Param, EditAndContinueOperation.Default),
                Row(4, TableIndex.CustomAttribute, EditAndContinueOperation.Default));
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

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, ctor0, ctor1, GetSyntaxMapFromMarkers(source0, source1))));

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
                Row(11, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(2, TableIndex.Param, EditAndContinueOperation.Default),
                Row(3, TableIndex.Param, EditAndContinueOperation.Default));
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

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, ctor0, ctor1, GetSyntaxMapFromMarkers(source0, source1))));

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

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

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
                Row(21, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(6, TableIndex.Param, EditAndContinueOperation.Default),
                Row(7, TableIndex.Param, EditAndContinueOperation.Default),
                Row(8, TableIndex.Param, EditAndContinueOperation.Default),
                Row(9, TableIndex.Param, EditAndContinueOperation.Default),
                Row(10, TableIndex.Param, EditAndContinueOperation.Default),
                Row(11, TableIndex.Param, EditAndContinueOperation.Default),
                Row(12, TableIndex.Param, EditAndContinueOperation.Default));
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

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

            // no new synthesized members generated (with #1 in names):
            diff1.VerifySynthesizedMembers(
                "C.<>c__DisplayClass1_1: {<>h__TransparentIdentifier0, <F>b__6}",
                "C.<>c__DisplayClass1_0: {<>h__TransparentIdentifier0, <F>b__5}",
                "C.<>c: {<>9__1_0, <>9__1_1, <>9__1_4, <F>b__1_0, <F>b__1_1, <F>b__1_4}",
                "C: {<F>b__1_2, <F>b__1_3, <>c, <>c__DisplayClass1_0, <>c__DisplayClass1_1}",
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
                Row(14, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(15, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(16, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(18, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(20, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(5, TableIndex.Param, EditAndContinueOperation.Default),
                Row(6, TableIndex.Param, EditAndContinueOperation.Default),
                Row(7, TableIndex.Param, EditAndContinueOperation.Default),
                Row(8, TableIndex.Param, EditAndContinueOperation.Default),
                Row(9, TableIndex.Param, EditAndContinueOperation.Default),
                Row(10, TableIndex.Param, EditAndContinueOperation.Default),
                Row(15, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(16, TableIndex.CustomAttribute, EditAndContinueOperation.Default));
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

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

            // no new synthesized members generated (with #1 in names):
            diff1.VerifySynthesizedMembers(
                "C.<>c: {<>9__1_1, <F>b__1_1}",
                "<>f__AnonymousType0<<a>j__TPar, <b>j__TPar>: {Equals, GetHashCode, ToString}",
                "C.<>c__DisplayClass1_0: {a, <F>b__2}",
                "C: {<F>b__1_0, <>c, <>c__DisplayClass1_0}");

            var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;

            // Method updates for lambdas:
            CheckEncLogDefinitions(reader1,
                Row(6, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(7, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(8, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(10, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(13, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(15, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(5, TableIndex.Param, EditAndContinueOperation.Default),
                Row(6, TableIndex.Param, EditAndContinueOperation.Default),
                Row(14, TableIndex.CustomAttribute, EditAndContinueOperation.Default));
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

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

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
                Row(2, TableIndex.Param, EditAndContinueOperation.Default),
                Row(3, TableIndex.Param, EditAndContinueOperation.Default),
                Row(4, TableIndex.Param, EditAndContinueOperation.Default),
                Row(5, TableIndex.Param, EditAndContinueOperation.Default),
                Row(5, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(7, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(9, TableIndex.CustomAttribute, EditAndContinueOperation.Default));
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

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

            // no new synthesized members generated (with #1 in names):
            diff1.VerifySynthesizedMembers(
                "C: {<F>b__1_2, <>c, <>c__DisplayClass1_0}",
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
                Row(7, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(8, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(10, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(2, TableIndex.Param, EditAndContinueOperation.Default),
                Row(3, TableIndex.Param, EditAndContinueOperation.Default),
                Row(4, TableIndex.Param, EditAndContinueOperation.Default),
                Row(5, TableIndex.Param, EditAndContinueOperation.Default),
                Row(5, TableIndex.CustomAttribute, EditAndContinueOperation.Default));
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

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

            // no new synthesized members generated (with #1 in names):
            diff1.VerifySynthesizedMembers(
                "C: {<F>b__1_1, <>c, <>c__DisplayClass1_0}",
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
                Row(7, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(9, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(2, TableIndex.Param, EditAndContinueOperation.Default),
                Row(3, TableIndex.Param, EditAndContinueOperation.Default),
                Row(5, TableIndex.CustomAttribute, EditAndContinueOperation.Default));
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

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

            var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;

            // new lambda "<F>b__1#1_0#1" has been added:
            diff1.VerifySynthesizedMembers(
                "C: {<>c}",
                "C.<>c: {<>9__1#1_0#1, <F>b__1#1_0#1}");

            // added:
            diff1.VerifyIL("C.<>c.<F>b__1#1_0#1", @"
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
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f1, f2, GetSyntaxMapFromMarkers(source1, source2))));

            diff2.VerifySynthesizedMembers(
                "C: {<>c}",
                "C.<>c: {<>9__1#1_0#1, <F>b__1#1_0#1}");

            // updated:
            diff2.VerifyIL("C.<>c.<F>b__1#1_0#1", @"
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

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

            var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;

            // new local function has been added:
            diff1.VerifySynthesizedMembers(
                "C: {<F>g__f|1#1_0#1}");

            // added:
            diff1.VerifyIL("C.<F>g__f|1#1_0#1", @"
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
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f1, f2, GetSyntaxMapFromMarkers(source1, source2))));

            diff2.VerifySynthesizedMembers(
                "C: {<F>g__f|1#1_0#1}");

            // updated:
            diff2.VerifyIL("C.<F>g__f|1#1_0#1", @"
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

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

            var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;

            // new local function has been added:
            diff1.VerifySynthesizedMembers(
                "C: {<F>g__f|0#1_0#1}");

            // added:
            diff1.VerifyIL("C.<F>g__f|0#1_0#1(int)", @"
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
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f1, f2, GetSyntaxMapFromMarkers(source1, source2))));

            diff2.VerifySynthesizedMembers(
                "C: {<F>g__f|0#1_0#1}");

            // updated:
            diff2.VerifyIL("C.<F>g__f|0#1_0#1(int)", @"
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

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

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
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f1, f2, GetSyntaxMapFromMarkers(source1, source2))));

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
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f2, f3, GetSyntaxMapFromMarkers(source2, source3))));

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

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

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
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f1, f2, GetSyntaxMapFromMarkers(source1, source2))));

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
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f2, f3, GetSyntaxMapFromMarkers(source2, source3))));

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

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

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
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f1, f2, GetSyntaxMapFromMarkers(source1, source2))));

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

            var compilation0 = CreateCompilationWithMscorlib461(source0, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), options: ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All), assemblyName: "A");
            var compilation1 = compilation0.WithSource(source1);
            var compilation2 = compilation1.WithSource(source2);

            var f_int1 = compilation1.GetMembers("C.F").Single(m => m.ToString() == "C.F(int)");
            var f_byte2 = compilation2.GetMembers("C.F").Single(m => m.ToString() == "C.F(byte)");

            var v0 = CompileAndVerify(compilation0);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var reader0 = md0.MetadataReader;
            CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "C", "<>c", "<>c__DisplayClass0_0");
            CheckNames(reader0, reader0.GetMethodDefNames(), "F", ".ctor", ".cctor", ".ctor", "<F>b__0_0", ".ctor", "<F>b__1", "<F>b__2");
            CheckNames(reader0, reader0.GetFieldDefNames(), "<>9", "<>9__0_0", "<>4__this", "a");

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Insert, null, f_int1)));

            var reader1 = diff1.GetMetadata().Reader;

            CheckNames(new[] { reader0, reader1 }, reader1.GetTypeDefNames(), "<>c__DisplayClass0#1_0#1");
            CheckNames(new[] { reader0, reader1 }, reader1.GetMethodDefNames(), ".ctor", "F", "<F>b__0#1_0#1", ".ctor", "<F>b__1#1", "<F>b__2#1");
            CheckNames(new[] { reader0, reader1 }, reader1.GetFieldDefNames(), "<>9__0#1_0#1", "<>4__this", "a");

            diff1.VerifySynthesizedMembers(
                "C: {<>c, <>c__DisplayClass0#1_0#1}",
                "C.<>c__DisplayClass0#1_0#1: {<>4__this, a, <F>b__1#1, <F>b__2#1}",
                "C.<>c: {<>9__0#1_0#1, <F>b__0#1_0#1}");

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Insert, null, f_byte2)));

            var reader2 = diff2.GetMetadata().Reader;

            CheckNames(new[] { reader0, reader1, reader2 }, reader2.GetTypeDefNames(), "<>c__DisplayClass1#2_0#2");
            CheckNames(new[] { reader0, reader1, reader2 }, reader2.GetMethodDefNames(), ".ctor", "F", "<F>b__1#2_0#2", ".ctor", "<F>b__1#2", "<F>b__2#2");
            CheckNames(new[] { reader0, reader1, reader2 }, reader2.GetFieldDefNames(), "<>9__1#2_0#2", "<>4__this", "a");
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

            var compilation0 = CreateCompilationWithMscorlib461(source0, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), options: ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All), assemblyName: "A");
            var compilation1 = compilation0.WithSource(source1);
            var compilation2 = compilation1.WithSource(source2);

            var f_int1 = compilation1.GetMembers("C.F").Single(m => m.ToString() == "C.F<T>(int)");
            var f_byte2 = compilation2.GetMembers("C.F").Single(m => m.ToString() == "C.F<T>(byte)");

            var v0 = CompileAndVerify(compilation0);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var reader0 = md0.MetadataReader;
            CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "C", "<>c__0`1", "<>c__DisplayClass0_0`1");
            CheckNames(reader0, reader0.GetMethodDefNames(), "F", ".ctor", ".cctor", ".ctor", "<F>b__0_0", ".ctor", "<F>b__1", "<F>b__2");
            CheckNames(reader0, reader0.GetFieldDefNames(), "<>9", "<>9__0_0", "<>4__this", "a");

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Insert, null, f_int1)));

            var reader1 = diff1.GetMetadata().Reader;

            CheckNames(new[] { reader0, reader1 }, reader1.GetTypeDefNames(), "<>c__0#1`1", "<>c__DisplayClass0#1_0#1`1");
            CheckNames(new[] { reader0, reader1 }, reader1.GetMethodDefNames(), "F", ".cctor", ".ctor", "<F>b__0#1_0#1", ".ctor", "<F>b__1#1", "<F>b__2#1");
            CheckNames(new[] { reader0, reader1 }, reader1.GetFieldDefNames(), "<>9", "<>9__0#1_0#1", "<>4__this", "a");

            diff1.VerifySynthesizedMembers(
                "C.<>c__0#1<T>: {<>9__0#1_0#1, <F>b__0#1_0#1}",
                "C: {<>c__0#1, <>c__DisplayClass0#1_0#1}",
                "C.<>c__DisplayClass0#1_0#1<T>: {<>4__this, a, <F>b__1#1, <F>b__2#1}");

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Insert, null, f_byte2)));

            var reader2 = diff2.GetMetadata().Reader;

            CheckNames(new[] { reader0, reader1, reader2 }, reader2.GetTypeDefNames(), "<>c__1#2`1", "<>c__DisplayClass1#2_0#2`1");
            CheckNames(new[] { reader0, reader1, reader2 }, reader2.GetMethodDefNames(), "F", ".cctor", ".ctor", "<F>b__1#2_0#2", ".ctor", "<F>b__1#2", "<F>b__2#2");
            CheckNames(new[] { reader0, reader1, reader2 }, reader2.GetFieldDefNames(), "<>9", "<>9__1#2_0#2", "<>4__this", "a");
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

            var compilation0 = CreateCompilationWithMscorlib461(source0, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), options: ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All), assemblyName: "A");
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

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Insert, null, f1),
                    SemanticEdit.Create(SemanticEditKind.Update, main0, main1)));

            diff1.VerifySynthesizedMembers(
                "C.<>c: {<>9__1#1_0#1, <F>b__1#1_0#1}",
                "C.<>c__DisplayClass1#1_0#1: {<>4__this, a, <F>b__1#1, <F>b__2#1}",
                "C: {<>c, <>c__DisplayClass1#1_0#1}");

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Insert, null, f_int2),
                    SemanticEdit.Create(SemanticEditKind.Update, main1, main2)));

            diff2.VerifySynthesizedMembers(
                "C.<>c__DisplayClass1#2_0#2: {<>4__this, a, <F>b__1#2, <F>b__2#2}",
                "C: {<>c, <>c__DisplayClass1#2_0#2, <>c__DisplayClass1#1_0#1}",
                "C.<>c: {<>9__1#2_0#2, <F>b__1#2_0#2, <>9__1#1_0#1, <F>b__1#1_0#1}",
                "C.<>c__DisplayClass1#1_0#1: {<>4__this, a, <F>b__1#1, <F>b__2#1}");

            var diff3 = compilation3.EmitDifference(
                diff2.NextGeneration,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, main2, main3)));

            diff3.VerifySynthesizedMembers(
                "C.<>c__DisplayClass1#1_0#1: {<>4__this, a, <F>b__1#1, <F>b__2#1}",
                "C.<>c: {<>9__1#2_0#2, <F>b__1#2_0#2, <>9__1#1_0#1, <F>b__1#1_0#1}",
                "C.<>c__DisplayClass1#2_0#2: {<>4__this, a, <F>b__1#2, <F>b__2#2}",
                "C: {<>c, <>c__DisplayClass1#2_0#2, <>c__DisplayClass1#1_0#1}");
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

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

            var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;

            // 3 method updates:
            // Note that even if the change is in the inner lambda such a change will usually impact sequence point 
            // spans in outer lambda and the method body. So although the IL doesn't change we usually need to update the outer methods.
            CheckEncLogDefinitions(reader1,
                Row(2, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(6, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(7, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(2, TableIndex.Param, EditAndContinueOperation.Default),
                Row(3, TableIndex.Param, EditAndContinueOperation.Default));
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

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, ctor00, ctor01, GetSyntaxMapFromMarkers(source0, source1)),
                    SemanticEdit.Create(SemanticEditKind.Update, ctor10, ctor11, GetSyntaxMapFromMarkers(source0, source1))));

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

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, ctor00, ctor01, GetSyntaxMapFromMarkers(source0, source1)),
                    SemanticEdit.Create(SemanticEditKind.Update, ctor10, ctor11, GetSyntaxMapFromMarkers(source0, source1))));

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

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Insert, null, b1),
                    SemanticEdit.Create(SemanticEditKind.Update, ctor0, ctor1, GetSyntaxMapFromMarkers(source0, source1))));

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

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Insert, null, b1),
                    SemanticEdit.Create(SemanticEditKind.Insert, null, ctor1, GetSyntaxMapFromMarkers(source0, source1))));

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

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

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
            using var _ = new EditAndContinueTest(references: [CSharpRef])
                .AddBaseline(
                    source: """
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
                    """,
                    validator: v =>
                    {
                        v.VerifyIL("C.F", """
                            {
                              // Code size       81 (0x51)
                              .maxstack  3
                              .locals init (System.Collections.Generic.IEnumerable<int> V_0) //result
                              IL_0000:  nop
                              IL_0001:  ldarg.0
                              IL_0002:  ldfld      "int[] C.array"
                              IL_0007:  ldsfld     "System.Func<int, bool> C.<>c.<>9__0_0"
                              IL_000c:  dup
                              IL_000d:  brtrue.s   IL_0026
                              IL_000f:  pop
                              IL_0010:  ldsfld     "C.<>c C.<>c.<>9"
                              IL_0015:  ldftn      "bool C.<>c.<F>b__0_0(int)"
                              IL_001b:  newobj     "System.Func<int, bool>..ctor(object, System.IntPtr)"
                              IL_0020:  dup
                              IL_0021:  stsfld     "System.Func<int, bool> C.<>c.<>9__0_0"
                              IL_0026:  call       "System.Collections.Generic.IEnumerable<int> System.Linq.Enumerable.Where<int>(System.Collections.Generic.IEnumerable<int>, System.Func<int, bool>)"
                              IL_002b:  ldsfld     "System.Func<int, int> C.<>c.<>9__0_1"
                              IL_0030:  dup
                              IL_0031:  brtrue.s   IL_004a
                              IL_0033:  pop
                              IL_0034:  ldsfld     "C.<>c C.<>c.<>9"
                              IL_0039:  ldftn      "int C.<>c.<F>b__0_1(int)"
                              IL_003f:  newobj     "System.Func<int, int>..ctor(object, System.IntPtr)"
                              IL_0044:  dup
                              IL_0045:  stsfld     "System.Func<int, int> C.<>c.<>9__0_1"
                              IL_004a:  call       "System.Collections.Generic.IEnumerable<int> System.Linq.Enumerable.Select<int, int>(System.Collections.Generic.IEnumerable<int>, System.Func<int, int>)"
                              IL_004f:  stloc.0
                              IL_0050:  ret
                            }
                            """);
                    })
                .AddGeneration(
                    source: """
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
                    """,
                    edits:
                    [
                        Edit(SemanticEditKind.Update, c => c.GetMember("C.F"), preserveLocalVariables: true)
                    ],
                    validator: v =>
                    {
                        // lambda for Select(a => a + 1) is gone
                        v.VerifySynthesizedMembers(
                            "System.Runtime.CompilerServices.HotReloadException",
                            "C: {<>c}",
                            "C.<>c: {<>9__0_0, <F>b__0_0}");

                        v.VerifyIL("C.F", """
                        {
                          // Code size       45 (0x2d)
                          .maxstack  3
                          .locals init (System.Collections.Generic.IEnumerable<int> V_0) //result
                          IL_0000:  nop
                          IL_0001:  ldarg.0
                          IL_0002:  ldfld      "int[] C.array"
                          IL_0007:  ldsfld     "System.Func<int, bool> C.<>c.<>9__0_0"
                          IL_000c:  dup
                          IL_000d:  brtrue.s   IL_0026
                          IL_000f:  pop
                          IL_0010:  ldsfld     "C.<>c C.<>c.<>9"
                          IL_0015:  ldftn      "bool C.<>c.<F>b__0_0(int)"
                          IL_001b:  newobj     "System.Func<int, bool>..ctor(object, System.IntPtr)"
                          IL_0020:  dup
                          IL_0021:  stsfld     "System.Func<int, bool> C.<>c.<>9__0_0"
                          IL_0026:  call       "System.Collections.Generic.IEnumerable<int> System.Linq.Enumerable.Where<int>(System.Collections.Generic.IEnumerable<int>, System.Func<int, bool>)"
                          IL_002b:  stloc.0
                          IL_002c:  ret
                        }
                        """);
                    })
                .Verify();
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

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

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
            using var _ = new EditAndContinueTest(references: [CSharpRef])
                .AddBaseline(
                    source: """
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
                    """,
                    validator: v =>
                    {
                        v.VerifyIL("C.F", """
                        {
                            // Code size       76 (0x4c)
                            .maxstack  4
                            .locals init (System.Collections.Generic.IEnumerable<System.Linq.IGrouping<int, int>> V_0) //result
                            IL_0000:  nop
                            IL_0001:  ldarg.0
                            IL_0002:  ldfld      "int[] C.array"
                            IL_0007:  ldsfld     "System.Func<int, int> C.<>c.<>9__0_0"
                            IL_000c:  dup
                            IL_000d:  brtrue.s   IL_0026
                            IL_000f:  pop
                            IL_0010:  ldsfld     "C.<>c C.<>c.<>9"
                            IL_0015:  ldftn      "int C.<>c.<F>b__0_0(int)"
                            IL_001b:  newobj     "System.Func<int, int>..ctor(object, System.IntPtr)"
                            IL_0020:  dup
                            IL_0021:  stsfld     "System.Func<int, int> C.<>c.<>9__0_0"
                            IL_0026:  ldsfld     "System.Func<int, int> C.<>c.<>9__0_1"
                            IL_002b:  dup
                            IL_002c:  brtrue.s   IL_0045
                            IL_002e:  pop
                            IL_002f:  ldsfld     "C.<>c C.<>c.<>9"
                            IL_0034:  ldftn      "int C.<>c.<F>b__0_1(int)"
                            IL_003a:  newobj     "System.Func<int, int>..ctor(object, System.IntPtr)"
                            IL_003f:  dup
                            IL_0040:  stsfld     "System.Func<int, int> C.<>c.<>9__0_1"
                            IL_0045:  call       "System.Collections.Generic.IEnumerable<System.Linq.IGrouping<int, int>> System.Linq.Enumerable.GroupBy<int, int, int>(System.Collections.Generic.IEnumerable<int>, System.Func<int, int>, System.Func<int, int>)"
                            IL_004a:  stloc.0
                            IL_004b:  ret
                        }
                        """);
                    })
                .AddGeneration(
                    // 1
                    source: """
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
                    """,
                    edits:
                    [
                        Edit(SemanticEditKind.Update, c => c.GetMember("C.F"), preserveLocalVariables: true)
                    ],
                    validator: v =>
                    {
                        // lambda for GroupBy(..., a => a + 1) is gone
                        v.VerifySynthesizedMembers(
                            "System.Runtime.CompilerServices.HotReloadException",
                            "C: {<>c}",
                            "C.<>c: {<>9__0_0, <F>b__0_0}");

                        v.VerifyIL("C.F", """
                        {
                          // Code size       45 (0x2d)
                          .maxstack  3
                          .locals init (System.Collections.Generic.IEnumerable<System.Linq.IGrouping<int, int>> V_0) //result
                          IL_0000:  nop
                          IL_0001:  ldarg.0
                          IL_0002:  ldfld      "int[] C.array"
                          IL_0007:  ldsfld     "System.Func<int, int> C.<>c.<>9__0_0"
                          IL_000c:  dup
                          IL_000d:  brtrue.s   IL_0026
                          IL_000f:  pop
                          IL_0010:  ldsfld     "C.<>c C.<>c.<>9"
                          IL_0015:  ldftn      "int C.<>c.<F>b__0_0(int)"
                          IL_001b:  newobj     "System.Func<int, int>..ctor(object, System.IntPtr)"
                          IL_0020:  dup
                          IL_0021:  stsfld     "System.Func<int, int> C.<>c.<>9__0_0"
                          IL_0026:  call       "System.Collections.Generic.IEnumerable<System.Linq.IGrouping<int, int>> System.Linq.Enumerable.GroupBy<int, int>(System.Collections.Generic.IEnumerable<int>, System.Func<int, int>)"
                          IL_002b:  stloc.0
                          IL_002c:  ret
                        }
                        """);
                    })
                .Verify();
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

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

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
                    SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

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
                    SemanticEdit.Create(SemanticEditKind.Update, f1, f2, GetSyntaxMapFromMarkers(source1, source2))));

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
            var source0 = MarkedSource(template.Replace("<<VALUE>>", "0"), options: TestOptions.Regular.WithNoRefSafetyRulesAttribute());
            var source1 = MarkedSource(template.Replace("<<VALUE>>", "1"), options: TestOptions.Regular.WithNoRefSafetyRulesAttribute());
            var source2 = MarkedSource(template.Replace("<<VALUE>>", "2"), options: TestOptions.Regular.WithNoRefSafetyRulesAttribute());

            var compilation0 = CreateCompilation(source0.Tree, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);
            var compilation2 = compilation1.WithSource(source2.Tree);

            var v0 = CompileAndVerify(compilation0);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");
            var f2 = compilation2.GetMember<MethodSymbol>("C.F");

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

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
                    SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

            diff1.VerifySynthesizedMembers(
                "C: {<>c__DisplayClass0_0}",
                "C.<>c__DisplayClass0_0: {x, <F>b__0}",
                "<>f__AnonymousType0<<X>j__TPar>: {Equals, GetHashCode, ToString}");

            diff1.VerifyIL("C.F", expectedIL.Replace("<<VALUE>>", "1"));

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, f1, f2, GetSyntaxMapFromMarkers(source1, source2))));

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

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);
            v0.VerifyIL("C.F",
@"{
  // Code size      112 (0x70)
  .maxstack  9
  IL_0000:  nop
  IL_0001:  ldsfld     ""System.Runtime.CompilerServices.CallSite<<>A{00000040}<System.Runtime.CompilerServices.CallSite, dynamic, object, <empty anonymous type>>> C.<>o__0.<>p__0""
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
  IL_0049:  call       ""System.Runtime.CompilerServices.CallSite<<>A{00000040}<System.Runtime.CompilerServices.CallSite, dynamic, object, <empty anonymous type>>> System.Runtime.CompilerServices.CallSite<<>A{00000040}<System.Runtime.CompilerServices.CallSite, dynamic, object, <empty anonymous type>>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_004e:  stsfld     ""System.Runtime.CompilerServices.CallSite<<>A{00000040}<System.Runtime.CompilerServices.CallSite, dynamic, object, <empty anonymous type>>> C.<>o__0.<>p__0""
  IL_0053:  ldsfld     ""System.Runtime.CompilerServices.CallSite<<>A{00000040}<System.Runtime.CompilerServices.CallSite, dynamic, object, <empty anonymous type>>> C.<>o__0.<>p__0""
  IL_0058:  ldfld      ""<>A{00000040}<System.Runtime.CompilerServices.CallSite, dynamic, object, <empty anonymous type>> System.Runtime.CompilerServices.CallSite<<>A{00000040}<System.Runtime.CompilerServices.CallSite, dynamic, object, <empty anonymous type>>>.Target""
  IL_005d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<<>A{00000040}<System.Runtime.CompilerServices.CallSite, dynamic, object, <empty anonymous type>>> C.<>o__0.<>p__0""
  IL_0062:  ldarg.0
  IL_0063:  ldarg.1
  IL_0064:  newobj     ""<>f__AnonymousType0..ctor()""
  IL_0069:  callvirt   ""void <>A{00000040}<System.Runtime.CompilerServices.CallSite, dynamic, object, <empty anonymous type>>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, ref object, <empty anonymous type>)""
  IL_006e:  nop
  IL_006f:  ret
}");

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));
            diff1.VerifyIL("C.F",
@"{
  // Code size      113 (0x71)
  .maxstack  9
  IL_0000:  nop
  IL_0001:  ldsfld     ""System.Runtime.CompilerServices.CallSite<<>A{00000040}#1<System.Runtime.CompilerServices.CallSite, dynamic, object, <anonymous type: object y>>> C.<>o__0#1.<>p__0""
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
  IL_0049:  call       ""System.Runtime.CompilerServices.CallSite<<>A{00000040}#1<System.Runtime.CompilerServices.CallSite, dynamic, object, <anonymous type: object y>>> System.Runtime.CompilerServices.CallSite<<>A{00000040}#1<System.Runtime.CompilerServices.CallSite, dynamic, object, <anonymous type: object y>>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_004e:  stsfld     ""System.Runtime.CompilerServices.CallSite<<>A{00000040}#1<System.Runtime.CompilerServices.CallSite, dynamic, object, <anonymous type: object y>>> C.<>o__0#1.<>p__0""
  IL_0053:  ldsfld     ""System.Runtime.CompilerServices.CallSite<<>A{00000040}#1<System.Runtime.CompilerServices.CallSite, dynamic, object, <anonymous type: object y>>> C.<>o__0#1.<>p__0""
  IL_0058:  ldfld      ""<>A{00000040}#1<System.Runtime.CompilerServices.CallSite, dynamic, object, <anonymous type: object y>> System.Runtime.CompilerServices.CallSite<<>A{00000040}#1<System.Runtime.CompilerServices.CallSite, dynamic, object, <anonymous type: object y>>>.Target""
  IL_005d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<<>A{00000040}#1<System.Runtime.CompilerServices.CallSite, dynamic, object, <anonymous type: object y>>> C.<>o__0#1.<>p__0""
  IL_0062:  ldarg.0
  IL_0063:  ldarg.1
  IL_0064:  ldarg.2
  IL_0065:  newobj     ""<>f__AnonymousType1<object>..ctor(object)""
  IL_006a:  callvirt   ""void <>A{00000040}#1<System.Runtime.CompilerServices.CallSite, dynamic, object, <anonymous type: object y>>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, ref object, <anonymous type: object y>)""
  IL_006f:  nop
  IL_0070:  ret
}");

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, f1, f2, GetSyntaxMapFromMarkers(source1, source2))));
            diff2.VerifyIL("C.F",
@"{
  // Code size      113 (0x71)
  .maxstack  9
  IL_0000:  nop
  IL_0001:  ldsfld     ""System.Runtime.CompilerServices.CallSite<<>A{00000200}#2<System.Runtime.CompilerServices.CallSite, dynamic, <anonymous type: object y>, object>> C.<>o__0#2.<>p__0""
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
  IL_0049:  call       ""System.Runtime.CompilerServices.CallSite<<>A{00000200}#2<System.Runtime.CompilerServices.CallSite, dynamic, <anonymous type: object y>, object>> System.Runtime.CompilerServices.CallSite<<>A{00000200}#2<System.Runtime.CompilerServices.CallSite, dynamic, <anonymous type: object y>, object>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_004e:  stsfld     ""System.Runtime.CompilerServices.CallSite<<>A{00000200}#2<System.Runtime.CompilerServices.CallSite, dynamic, <anonymous type: object y>, object>> C.<>o__0#2.<>p__0""
  IL_0053:  ldsfld     ""System.Runtime.CompilerServices.CallSite<<>A{00000200}#2<System.Runtime.CompilerServices.CallSite, dynamic, <anonymous type: object y>, object>> C.<>o__0#2.<>p__0""
  IL_0058:  ldfld      ""<>A{00000200}#2<System.Runtime.CompilerServices.CallSite, dynamic, <anonymous type: object y>, object> System.Runtime.CompilerServices.CallSite<<>A{00000200}#2<System.Runtime.CompilerServices.CallSite, dynamic, <anonymous type: object y>, object>>.Target""
  IL_005d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<<>A{00000200}#2<System.Runtime.CompilerServices.CallSite, dynamic, <anonymous type: object y>, object>> C.<>o__0#2.<>p__0""
  IL_0062:  ldarg.0
  IL_0063:  ldarg.2
  IL_0064:  newobj     ""<>f__AnonymousType1<object>..ctor(object)""
  IL_0069:  ldarg.1
  IL_006a:  callvirt   ""void <>A{00000200}#2<System.Runtime.CompilerServices.CallSite, dynamic, <anonymous type: object y>, object>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, <anonymous type: object y>, ref object)""
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

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

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
                Row(4, TableIndex.CustomAttribute, EditAndContinueOperation.Default));
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

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

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
                Row(4, TableIndex.CustomAttribute, EditAndContinueOperation.Default));
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

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

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
                Row(4, TableIndex.CustomAttribute, EditAndContinueOperation.Default));
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

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

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
                Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(1, TableIndex.Param, EditAndContinueOperation.Default));
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

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

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
                Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(1, TableIndex.Param, EditAndContinueOperation.Default));
        }

        [Fact]
        public void CaptureStructAndThroughClassEnvChain()
        {
            using var _ = new EditAndContinueTest()
                .AddBaseline("""
                    using System;
                    public class C 
                    {
                        public void F(int x) 
                        <N:0>{</N:0>
                            <N:1>{</N:1>
                                int <N:2>y = 0</N:2>;
                                Func<int> f = <N:3>() => x</N:3>;
                                <N:4>{</N:4>
                                    Func<int> f2 = <N:5>() => x + y</N:5>;
                                    int <N:6>z = 0</N:6>;
                                    // Capture struct and through class env chain
                                    <N:7>int L() => x + y + z;</N:7>
                                }
                            }
                        }
                    }
                    """,
                    validator: g =>
                    {
                        g.VerifyMethodBody("C.<>c__DisplayClass0_1.<F>g__L|2(ref C.<>c__DisplayClass0_2)", """
                        {
                          // Code size       26 (0x1a)
                          .maxstack  2
                          // sequence point: x + y + z
                          IL_0000:  ldarg.0
                          IL_0001:  ldfld      "C.<>c__DisplayClass0_0 C.<>c__DisplayClass0_1.CS$<>8__locals1"
                          IL_0006:  ldfld      "int C.<>c__DisplayClass0_0.x"
                          IL_000b:  ldarg.0
                          IL_000c:  ldfld      "int C.<>c__DisplayClass0_1.y"
                          IL_0011:  add
                          IL_0012:  ldarg.1
                          IL_0013:  ldfld      "int C.<>c__DisplayClass0_2.z"
                          IL_0018:  add
                          IL_0019:  ret
                        }
                        """);
                    })
                .AddGeneration("""
                    using System;
                    public class C 
                    {
                        public void F(int x) 
                        <N:0>{</N:0>
                            <N:1>{</N:1>
                                int <N:2>y = 0</N:2>;
                                Func<int> f = <N:3>() => x</N:3>;
                                <N:4>{</N:4>
                                    Func<int> f2 = <N:5>() => x + y</N:5>;
                                    int <N:6>z = 0</N:6>;
                                    // Capture struct and through class env chain
                                    <N:7>int L() => x + y + z + 1;</N:7>
                                }
                            }
                        }
                    }
                    """,
                    edits:
                    [
                        Edit(SemanticEditKind.Update, c => c.GetMember("C.F"), preserveLocalVariables: true)
                    ],
                    validator: g =>
                    {
                        g.VerifySynthesizedMembers(
                            "C.<>c__DisplayClass0_2: {z}",
                            "C.<>c__DisplayClass0_0: {x, <F>b__0}",
                            "C: {<>c__DisplayClass0_0, <>c__DisplayClass0_1, <>c__DisplayClass0_2}",
                            "C.<>c__DisplayClass0_1: {y, CS$<>8__locals1, <F>b__1, <F>g__L|2}");

                        g.VerifyIL("C.<>c__DisplayClass0_1.<F>g__L|2(ref C.<>c__DisplayClass0_2)", """
                        {
                            // Code size       28 (0x1c)
                            .maxstack  2
                            IL_0000:  ldarg.0
                            IL_0001:  ldfld      "C.<>c__DisplayClass0_0 C.<>c__DisplayClass0_1.CS$<>8__locals1"
                            IL_0006:  ldfld      "int C.<>c__DisplayClass0_0.x"
                            IL_000b:  ldarg.0
                            IL_000c:  ldfld      "int C.<>c__DisplayClass0_1.y"
                            IL_0011:  add
                            IL_0012:  ldarg.1
                            IL_0013:  ldfld      "int C.<>c__DisplayClass0_2.z"
                            IL_0018:  add
                            IL_0019:  ldc.i4.1
                            IL_001a:  add
                            IL_001b:  ret
                        }
                        """);

                        g.VerifyEncLogDefinitions(
                        [
                            Row(2, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                            Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(6, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(7, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(1, TableIndex.Param, EditAndContinueOperation.Default)
                        ]);
                    })
                .Verify();
        }

        [Fact]
        public void TopLevelStatement_Closure()
        {
            var source0 = MarkedSource(@"
<N:0>
using System;

Func<string> x = <N:1>() => args[0]</N:1>;
Console.WriteLine(x());
</N:0>
");
            var source1 = MarkedSource(@"
<N:0>
using System;

Func<string> x = <N:1>() => args[1]</N:1>;
Console.WriteLine(x());
</N:0>
");
            var compilation0 = CreateCompilation(source0.Tree, options: TestOptions.DebugExe);
            var compilation1 = compilation0.WithSource(source1.Tree);

            var v0 = CompileAndVerify(compilation0);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var f0 = compilation0.GetMember<MethodSymbol>("Program.<Main>$");
            var f1 = compilation1.GetMember<MethodSymbol>("Program.<Main>$");

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

            // no new synthesized members generated (with #1 in names):
            diff1.VerifySynthesizedMembers(
                "Program.<>c__DisplayClass0_0: {args, <<Main>$>b__0}",
                "Program: {<>c__DisplayClass0_0}");

            var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;

            // Method updates
            CheckEncLogDefinitions(reader1,
                Row(2, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(1, TableIndex.Param, EditAndContinueOperation.Default));
        }

        [Fact]
        [WorkItem(55381, "https://github.com/dotnet/roslyn/issues/55381")]
        public void HiddenMethodClosure()
        {
            var source0 = MarkedSource(@"
#line hidden
using System;

class C
{
    public static void F(int arg)
    <N:0>{</N:0>
        Func<int> x = <N:1>() => arg</N:1>;
    }
}
");
            var source1 = MarkedSource(@"
#line hidden    
using System;

class C
{
    public static void F(int arg)
    <N:0>{</N:0>
        Func<int> x = <N:1>() => arg + 1</N:1>;
    }
}
");
            var compilation0 = CreateCompilation(source0.Tree, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);

            var v0 = CompileAndVerify(compilation0);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

            // no new synthesized members generated (with #1 in names):
            diff1.VerifySynthesizedMembers(
                "C: {<>c__DisplayClass0_0}",
                "C.<>c__DisplayClass0_0: {arg, <F>b__0}");

            var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;

            // Method updates
            CheckEncLogDefinitions(reader1,
                Row(2, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(1, TableIndex.Param, EditAndContinueOperation.Default));
        }

        [Fact]
        public void ReplaceTypeWithClosure()
        {
            var source0 = @"
using System;
class C 
{
    void F() { var x = new Action(() => {}); Console.WriteLine(1); }
}
";
            var source1 = @"
using System;
class C
{
    void F() { var x = new Action(() => {}); Console.WriteLine(2); }
}";

            var compilation0 = CreateCompilation(source0, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), options: TestOptions.DebugDll, targetFramework: TargetFramework.NetStandard20);
            var compilation1 = compilation0.WithSource(source1);

            var c0 = compilation0.GetMember<NamedTypeSymbol>("C");
            var c1 = compilation1.GetMember<NamedTypeSymbol>("C");

            // Verify full metadata contains expected rows.
            var bytes0 = compilation0.EmitToArray();
            using var md0 = ModuleMetadata.CreateFromImage(bytes0);
            var reader0 = md0.MetadataReader;

            var generation0 = CreateInitialBaseline(compilation0, md0, EmptyLocalsProvider);

            // This update emulates "Reloadable" type behavior - a new type is generated instead of updating the existing one.
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Replace, null, c1)));

            // Verify delta metadata contains expected rows.
            using var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;
            var readers = new[] { reader0, reader1 };

            CheckNames(readers, reader1.GetTypeDefNames(), "C#1", "<>c");
            CheckNames(readers, diff1.EmitResult.ChangedTypes, "C#1", "<>c");

            // All definitions should be added, none should be updated:
            CheckEncLogDefinitions(reader1,
                Row(2, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(4, TableIndex.TypeDef, EditAndContinueOperation.Default),
                Row(5, TableIndex.TypeDef, EditAndContinueOperation.Default),
                Row(5, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                Row(3, TableIndex.Field, EditAndContinueOperation.Default),
                Row(5, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                Row(4, TableIndex.Field, EditAndContinueOperation.Default),
                Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                Row(6, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                Row(7, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(5, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                Row(8, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(5, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                Row(9, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(5, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                Row(10, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(5, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(2, TableIndex.NestedClass, EditAndContinueOperation.Default));

            CheckEncMapDefinitions(reader1,
                Handle(4, TableIndex.TypeDef),
                Handle(5, TableIndex.TypeDef),
                Handle(3, TableIndex.Field),
                Handle(4, TableIndex.Field),
                Handle(6, TableIndex.MethodDef),
                Handle(7, TableIndex.MethodDef),
                Handle(8, TableIndex.MethodDef),
                Handle(9, TableIndex.MethodDef),
                Handle(10, TableIndex.MethodDef),
                Handle(5, TableIndex.CustomAttribute),
                Handle(2, TableIndex.StandAloneSig),
                Handle(2, TableIndex.NestedClass));
        }

        [Fact]
        public void Capture_Local_Lambda()
        {
            using var _ = new EditAndContinueTest()
                .AddBaseline(
                    source: """
                    using System;
                    class C
                    {
                        public void F()
                        <N:0>{
                            int <N:1>x = 1</N:1>;
                            _ = new Action(<N:2>() => Console.WriteLine(0)</N:2>);
                            _ = new Action(<N:3>() => Console.WriteLine(1)</N:3>);
                        }</N:0>
                    }
                    """,
                    validator: g =>
                    {
                    })

                .AddGeneration(
                    source: """
                    using System;
                    class C
                    {
                        public void F()
                        <N:0>{
                            int <N:1>x = 1</N:1>;
                            _ = new Action(<N:2>() => Console.WriteLine(x)</N:2>);
                            _ = new Action(<N:3>() => Console.WriteLine(2)</N:3>);
                        }</N:0>
                    }
                    """,
                    edits:
                    [
                        Edit(SemanticEditKind.Update, c => c.GetMember("C.F"), preserveLocalVariables: true),
                    ],
                    validator: g =>
                    {
                        // Static lambda is reused.
                        // A new display class and method is generated for lambda that captures x.
                        g.VerifySynthesizedMembers(
                            "System.Runtime.CompilerServices.HotReloadException",
                            "C: {<>c, <>c__DisplayClass0_0#1}",
                            "C.<>c: {<>9__0_1, <F>b__0_1}",
                            "C.<>c__DisplayClass0_0#1: {x, <F>b__0#1}");

                        g.VerifyMethodDefNames("F", "<F>b__0_0", "<F>b__0_1", ".ctor", ".ctor", "<F>b__0#1");

                        g.VerifyIL(
                        """
                        {
                          // Code size       44 (0x2c)
                          .maxstack  2
                          IL_0000:  newobj     0x06000008
                          IL_0005:  stloc.1
                          IL_0006:  nop
                          IL_0007:  ldloc.1
                          IL_0008:  ldc.i4.1
                          IL_0009:  stfld      0x04000005
                          IL_000e:  nop
                          IL_000f:  ldsfld     0x04000003
                          IL_0014:  brtrue.s   IL_002b
                          IL_0016:  ldsfld     0x04000001
                          IL_001b:  ldftn      0x06000006
                          IL_0021:  newobj     0x0A000009
                          IL_0026:  stsfld     0x04000003
                          IL_002b:  ret
                        }
                        {
                          // Code size       13 (0xd)
                          .maxstack  8
                          IL_0000:  ldstr      0x70000005
                          IL_0005:  ldc.i4.s   -5
                          IL_0007:  newobj     0x06000007
                          IL_000c:  throw
                        }
                        {
                          // Code size        8 (0x8)
                          .maxstack  8
                          IL_0000:  ldc.i4.2
                          IL_0001:  call       0x0A00000A
                          IL_0006:  nop
                          IL_0007:  ret
                        }
                        {
                          // Code size       16 (0x10)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldarg.1
                          IL_0002:  call       0x0A00000B
                          IL_0007:  nop
                          IL_0008:  ldarg.0
                          IL_0009:  ldarg.2
                          IL_000a:  stfld      0x04000004
                          IL_000f:  ret
                        }
                        {
                          // Code size        8 (0x8)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  call       0x0A00000C
                          IL_0006:  nop
                          IL_0007:  ret
                        }
                        {
                          // Code size       13 (0xd)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldfld      0x04000005
                          IL_0006:  call       0x0A00000A
                          IL_000b:  nop
                          IL_000c:  ret
                        }
                        """);
                    })
                .Verify();
        }

        [Fact]
        public void Capture_Local_LocalFunction()
        {
            using var _ = new EditAndContinueTest()
                .AddBaseline(
                    source: """
                    using System;
                    class C
                    {
                        public void F()
                        <N:0>{
                            int <N:1>x = 1</N:1>;
                            <N:2>void L1() => Console.WriteLine(0);</N:2>
                            <N:3>void L2() => Console.WriteLine(1);</N:3>
                        }</N:0>
                    }
                    """,
                    validator: g =>
                    {
                        g.VerifyMethodBody("C.<F>g__L1|0_0", """
                            {
                              // Code size        8 (0x8)
                              .maxstack  1
                              // sequence point: Console.WriteLine(0)
                              IL_0000:  ldc.i4.0
                              IL_0001:  call       "void System.Console.WriteLine(int)"
                              IL_0006:  nop
                              IL_0007:  ret
                            }
                            """);

                        g.VerifyMethodBody("C.<F>g__L2|0_1", """
                            {
                              // Code size        8 (0x8)
                              .maxstack  1
                              // sequence point: Console.WriteLine(1)
                              IL_0000:  ldc.i4.1
                              IL_0001:  call       "void System.Console.WriteLine(int)"
                              IL_0006:  nop
                              IL_0007:  ret
                            }
                            """);
                    })

                .AddGeneration(
                    source: """
                    using System;
                    class C
                    {
                        public void F()
                        <N:0>{
                            int <N:1>x = 1</N:1>;
                            <N:2>void L1() => Console.WriteLine(x);</N:2>
                            <N:3>void L2() => Console.WriteLine(2);</N:3>
                        }</N:0>
                    }
                    """,
                    edits:
                    [
                        Edit(SemanticEditKind.Update, c => c.GetMember("C.F"), preserveLocalVariables: true),
                    ],
                    validator: g =>
                    {
                        g.VerifySynthesizedMembers(
                            "System.Runtime.CompilerServices.HotReloadException",
                            "C: {<F>g__L1|0_0#1, <F>g__L2|0_1, <>c__DisplayClass0_0#1}",
                            "C.<>c__DisplayClass0_0#1: {x}");

                        g.VerifyMethodDefNames("F", "<F>g__L1|0_0", "<F>g__L2|0_1", "<F>g__L1|0_0#1", ".ctor");

                        g.VerifyIL(
                        """
                        {
                          // Code size       12 (0xc)
                          .maxstack  2
                          IL_0000:  nop
                          IL_0001:  ldloca.s   V_1
                          IL_0003:  ldc.i4.1
                          IL_0004:  stfld      0x04000002
                          IL_0009:  nop
                          IL_000a:  nop
                          IL_000b:  ret
                        }
                        {
                          // Code size       13 (0xd)
                          .maxstack  8
                          IL_0000:  ldstr      0x70000005
                          IL_0005:  ldc.i4.s   -5
                          IL_0007:  newobj     0x06000006
                          IL_000c:  throw
                        }
                        {
                          // Code size        8 (0x8)
                          .maxstack  8
                          IL_0000:  ldc.i4.2
                          IL_0001:  call       0x0A000008
                          IL_0006:  nop
                          IL_0007:  ret
                        }
                        {
                          // Code size       13 (0xd)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldfld      0x04000002
                          IL_0006:  call       0x0A000008
                          IL_000b:  nop
                          IL_000c:  ret
                        }
                        {
                          // Code size       16 (0x10)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldarg.1
                          IL_0002:  call       0x0A000009
                          IL_0007:  nop
                          IL_0008:  ldarg.0
                          IL_0009:  ldarg.2
                          IL_000a:  stfld      0x04000001
                          IL_000f:  ret
                        }
                        """);
                    })
                .Verify();
        }

        [Fact]
        public void Capture_MethodParameter_Lambda()
        {
            using var _ = new EditAndContinueTest()
                .AddBaseline(
                    source: """
                    using System;
                    class C
                    {
                        public void F(int x)
                        <N:0>{
                            _ = new Action(<N:2>() => Console.WriteLine(0)</N:2>);
                            _ = new Action(<N:3>() => Console.WriteLine(1)</N:3>);
                        }</N:0>
                    }
                    """,
                    validator: g =>
                    {
                    })

                .AddGeneration(
                    source: """
                    using System;
                    class C
                    {
                        public void F(int x)
                        <N:0>{
                            _ = new Action(<N:2>() => Console.WriteLine(x)</N:2>);
                            _ = new Action(<N:3>() => Console.WriteLine(2)</N:3>);
                        }</N:0>
                    }
                    """,
                    edits:
                    [
                        Edit(SemanticEditKind.Update, c => c.GetMember("C.F"), preserveLocalVariables: true),
                    ],
                    validator: g =>
                    {
                        // Static lambda is reused.
                        // A new display class and method is generated for lambda that captures x.
                        g.VerifySynthesizedMembers(
                            "System.Runtime.CompilerServices.HotReloadException",
                            "C: {<>c, <>c__DisplayClass0_0#1}",
                            "C.<>c: {<>9__0_1, <F>b__0_1}",
                            "C.<>c__DisplayClass0_0#1: {x, <F>b__0#1}");

                        g.VerifyMethodDefNames("F", "<F>b__0_0", "<F>b__0_1", ".ctor", ".ctor", "<F>b__0#1");

                        g.VerifyIL(
                        """
                        {
                          // Code size       44 (0x2c)
                          .maxstack  2
                          IL_0000:  newobj     0x06000008
                          IL_0005:  stloc.0
                          IL_0006:  ldloc.0
                          IL_0007:  ldarg.1
                          IL_0008:  stfld      0x04000005
                          IL_000d:  nop
                          IL_000e:  nop
                          IL_000f:  ldsfld     0x04000003
                          IL_0014:  brtrue.s   IL_002b
                          IL_0016:  ldsfld     0x04000001
                          IL_001b:  ldftn      0x06000006
                          IL_0021:  newobj     0x0A000009
                          IL_0026:  stsfld     0x04000003
                          IL_002b:  ret
                        }
                        {
                          // Code size       13 (0xd)
                          .maxstack  8
                          IL_0000:  ldstr      0x70000005
                          IL_0005:  ldc.i4.s   -5
                          IL_0007:  newobj     0x06000007
                          IL_000c:  throw
                        }
                        {
                          // Code size        8 (0x8)
                          .maxstack  8
                          IL_0000:  ldc.i4.2
                          IL_0001:  call       0x0A00000A
                          IL_0006:  nop
                          IL_0007:  ret
                        }
                        {
                          // Code size       16 (0x10)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldarg.1
                          IL_0002:  call       0x0A00000B
                          IL_0007:  nop
                          IL_0008:  ldarg.0
                          IL_0009:  ldarg.2
                          IL_000a:  stfld      0x04000004
                          IL_000f:  ret
                        }
                        {
                          // Code size        8 (0x8)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  call       0x0A00000C
                          IL_0006:  nop
                          IL_0007:  ret
                        }
                        {
                          // Code size       13 (0xd)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldfld      0x04000005
                          IL_0006:  call       0x0A00000A
                          IL_000b:  nop
                          IL_000c:  ret
                        }
                        """);
                    })
                .Verify();
        }

        [Fact]
        public void Capture_MethodParameter_LocalFunction()
        {
            using var _ = new EditAndContinueTest()
                .AddBaseline(
                    source: """
                    using System;
                    class C
                    {
                        public void F(int x)
                        <N:0>{
                            <N:1>void L1() => Console.WriteLine(0);</N:1>
                            <N:2>void L2() => Console.WriteLine(1);</N:2>
                        }</N:0>
                    }
                    """,
                    validator: g =>
                    {
                        g.VerifyMethodBody("C.<F>g__L1|0_0", """
                            {
                              // Code size        8 (0x8)
                              .maxstack  1
                              // sequence point: Console.WriteLine(0)
                              IL_0000:  ldc.i4.0
                              IL_0001:  call       "void System.Console.WriteLine(int)"
                              IL_0006:  nop
                              IL_0007:  ret
                            }
                            """);

                        g.VerifyMethodBody("C.<F>g__L2|0_1", """
                            {
                              // Code size        8 (0x8)
                              .maxstack  1
                              // sequence point: Console.WriteLine(1)
                              IL_0000:  ldc.i4.1
                              IL_0001:  call       "void System.Console.WriteLine(int)"
                              IL_0006:  nop
                              IL_0007:  ret
                            }
                            """);
                    })

                .AddGeneration(
                    source: """
                    using System;
                    class C
                    {
                        public void F(int x)
                        <N:0>{
                            <N:1>void L1() => Console.WriteLine(x);</N:1>
                            <N:2>void L2() => Console.WriteLine(2);</N:2>
                        }</N:0>
                    }
                    """,
                    edits:
                    [
                        Edit(SemanticEditKind.Update, c => c.GetMember("C.F"), preserveLocalVariables: true),
                    ],
                    validator: g =>
                    {
                        g.VerifySynthesizedMembers(
                            "System.Runtime.CompilerServices.HotReloadException",
                            "C: {<F>g__L1|0_0#1, <F>g__L2|0_1, <>c__DisplayClass0_0#1}",
                            "C.<>c__DisplayClass0_0#1: {x}");

                        g.VerifyMethodDefNames("F", "<F>g__L1|0_0", "<F>g__L2|0_1", "<F>g__L1|0_0#1", ".ctor");

                        g.VerifyIL("""
                        {
                          // Code size       12 (0xc)
                          .maxstack  2
                          IL_0000:  ldloca.s   V_0
                          IL_0002:  ldarg.1
                          IL_0003:  stfld      0x04000002
                          IL_0008:  nop
                          IL_0009:  nop
                          IL_000a:  nop
                          IL_000b:  ret
                        }
                        {
                          // Code size       13 (0xd)
                          .maxstack  8
                          IL_0000:  ldstr      0x70000005
                          IL_0005:  ldc.i4.s   -5
                          IL_0007:  newobj     0x06000006
                          IL_000c:  throw
                        }
                        {
                          // Code size        8 (0x8)
                          .maxstack  8
                          IL_0000:  ldc.i4.2
                          IL_0001:  call       0x0A000008
                          IL_0006:  nop
                          IL_0007:  ret
                        }
                        {
                          // Code size       13 (0xd)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldfld      0x04000002
                          IL_0006:  call       0x0A000008
                          IL_000b:  nop
                          IL_000c:  ret
                        }
                        {
                          // Code size       16 (0x10)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldarg.1
                          IL_0002:  call       0x0A000009
                          IL_0007:  nop
                          IL_0008:  ldarg.0
                          IL_0009:  ldarg.2
                          IL_000a:  stfld      0x04000001
                          IL_000f:  ret
                        }
                        """);
                    })
                .Verify();
        }

        [Fact]
        public void Capture_LambdaParameter_ExpressionBody()
        {
            using var _ = new EditAndContinueTest()
                .AddBaseline(
                    source: """
                    using System;
                    
                    class C
                    {
                        static int G(Func<int> f) => 1;
                    
                        static void F()
                        {
                            Func<int, int> <N:0>f1 = <N:1>x => <N:2>G(<N:3>() => 1</N:3>)</N:2></N:1></N:0>;
                        }
                    }
                    """,
                    validator: g =>
                    {
                    })

                .AddGeneration(
                    source: """
                    using System;
                    
                    class C
                    {
                        static int G(Func<int> f) => 1;
                    
                        static void F()
                        {
                            Func<int, int> <N:0>f1 = <N:1>x => <N:2>G(<N:3>() => x</N:3>)</N:2></N:1></N:0>;
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
                            "System.Runtime.CompilerServices.HotReloadException",
                            "C: {<>c, <>c__DisplayClass1_0#1}",
                            "C.<>c: {<>9__1_0, <F>b__1_0}",
                            "C.<>c__DisplayClass1_0#1: {x, <F>b__1#1}");

                        g.VerifyMethodDefNames("F", "<F>b__1_0", "<F>b__1_1", ".ctor", ".ctor", "<F>b__1#1");

                        g.VerifyIL(
                        """
                        {
                          // Code size       34 (0x22)
                          .maxstack  2
                          IL_0000:  nop
                          IL_0001:  ldsfld     0x04000003
                          IL_0006:  dup
                          IL_0007:  brtrue.s   IL_0020
                          IL_0009:  pop
                          IL_000a:  ldsfld     0x04000001
                          IL_000f:  ldftn      0x06000006
                          IL_0015:  newobj     0x0A000009
                          IL_001a:  dup
                          IL_001b:  stsfld     0x04000003
                          IL_0020:  stloc.0
                          IL_0021:  ret
                        }
                        {
                          // Code size       31 (0x1f)
                          .maxstack  2
                          IL_0000:  newobj     0x06000009
                          IL_0005:  stloc.0
                          IL_0006:  ldloc.0
                          IL_0007:  ldarg.1
                          IL_0008:  stfld      0x04000005
                          IL_000d:  ldloc.0
                          IL_000e:  ldftn      0x0600000A
                          IL_0014:  newobj     0x0A00000A
                          IL_0019:  call       0x06000001
                          IL_001e:  ret
                        }
                        {
                          // Code size       12 (0xc)
                          .maxstack  8
                          IL_0000:  ldstr      0x70000005
                          IL_0005:  ldc.i4.m1
                          IL_0006:  newobj     0x06000008
                          IL_000b:  throw
                        }
                        {
                          // Code size       16 (0x10)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldarg.1
                          IL_0002:  call       0x0A00000B
                          IL_0007:  nop
                          IL_0008:  ldarg.0
                          IL_0009:  ldarg.2
                          IL_000a:  stfld      0x04000004
                          IL_000f:  ret
                        }
                        {
                          // Code size        8 (0x8)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  call       0x0A00000C
                          IL_0006:  nop
                          IL_0007:  ret
                        }
                        {
                          // Code size        7 (0x7)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldfld      0x04000005
                          IL_0006:  ret
                        }
                        """);
                    })
                .Verify();
        }

        [Fact]
        public void Capture_ConstructorParameter()
        {
            using var _ = new EditAndContinueTest()
                .AddBaseline(
                    source: """
                    using System;
                    class B(Func<int> f);

                    class C : B
                    {
                        <N:0>public C(int x, int y)
                            : base(<N:1>() => 1</N:1>)
                        {
                            _ = new Action(<N:2>() => Console.WriteLine(0)</N:2>);
                        }</N:0>
                    }
                    """,
                    validator: g =>
                    {
                    })

                .AddGeneration(
                    source: """
                    using System;
                    class B(Func<int> f);

                    class C : B
                    {
                        <N:0>public C(int x, int y)
                            : base(<N:1>() => x</N:1>)
                        {
                            _ = new Action(<N:2>() => Console.WriteLine(y)</N:2>);
                        }</N:0>
                    }
                    """,
                    edits:
                    [
                        Edit(SemanticEditKind.Update, c => c.GetMember("C..ctor"), preserveLocalVariables: true),
                    ],
                    validator: g =>
                    {
                        g.VerifySynthesizedMembers(
                            "System.Runtime.CompilerServices.HotReloadException",
                            "C: {<>c__DisplayClass0_0#1}",
                            "C.<>c__DisplayClass0_0#1: {x, y, <.ctor>b__0#1, <.ctor>b__1#1}");

                        g.VerifyMethodDefNames(
                            ".ctor", "<.ctor>b__0_0", "<.ctor>b__0_1", ".ctor", ".ctor", "<.ctor>b__0#1", "<.ctor>b__1#1");

                        g.VerifyIL(
                        """
                        {
                          // Code size       42 (0x2a)
                          .maxstack  3
                          IL_0000:  newobj     0x06000008
                          IL_0005:  stloc.0
                          IL_0006:  ldloc.0
                          IL_0007:  ldarg.1
                          IL_0008:  stfld      0x04000005
                          IL_000d:  ldloc.0
                          IL_000e:  ldarg.2
                          IL_000f:  stfld      0x04000006
                          IL_0014:  ldarg.0
                          IL_0015:  ldloc.0
                          IL_0016:  ldftn      0x06000009
                          IL_001c:  newobj     0x0A00000A
                          IL_0021:  call       0x06000001
                          IL_0026:  nop
                          IL_0027:  nop
                          IL_0028:  nop
                          IL_0029:  ret
                        }
                        {
                          // Code size       13 (0xd)
                          .maxstack  8
                          IL_0000:  ldstr      0x70000005
                          IL_0005:  ldc.i4.s   -5
                          IL_0007:  newobj     0x06000007
                          IL_000c:  throw
                        }
                        {
                          // Code size       12 (0xc)
                          .maxstack  8
                          IL_0000:  ldstr      0x700001F4
                          IL_0005:  ldc.i4.m1
                          IL_0006:  newobj     0x06000007
                          IL_000b:  throw
                        }
                        {
                          // Code size       16 (0x10)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldarg.1
                          IL_0002:  call       0x0A00000B
                          IL_0007:  nop
                          IL_0008:  ldarg.0
                          IL_0009:  ldarg.2
                          IL_000a:  stfld      0x04000004
                          IL_000f:  ret
                        }
                        {
                          // Code size        8 (0x8)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  call       0x0A00000C
                          IL_0006:  nop
                          IL_0007:  ret
                        }
                        {
                          // Code size        7 (0x7)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldfld      0x04000005
                          IL_0006:  ret
                        }
                        {
                          // Code size       13 (0xd)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldfld      0x04000006
                          IL_0006:  call       0x0A00000D
                          IL_000b:  nop
                          IL_000c:  ret
                        }
                        """);
                    })
                .Verify();
        }

        [Fact]
        public void Capture_PrimaryConstructorParameter()
        {
            using var _ = new EditAndContinueTest()
                .AddBaseline(
                    source: """
                    using System;
                    class B(Func<int> f);

                    <N:0>class C(int x) : B(<N:1>() => 1</N:1>);</N:0>
                    """,
                    validator: g =>
                    {
                    })

                .AddGeneration(
                    source: """
                    using System;
                    class B(Func<int> f);

                    <N:0>class C(int x) : B(<N:1>() => x</N:1>);</N:0>
                    """,
                    edits:
                    [
                        Edit(SemanticEditKind.Update, c => c.GetMember("C..ctor"), preserveLocalVariables: true),
                    ],
                    validator: g =>
                    {
                        g.VerifySynthesizedMembers(
                            "System.Runtime.CompilerServices.HotReloadException",
                            "C: {<>c__DisplayClass0_0#1}",
                            "C.<>c__DisplayClass0_0#1: {x, <.ctor>b__0#1}");

                        g.VerifyMethodDefNames(
                            ".ctor", "<.ctor>b__0_0", ".ctor", ".ctor", "<.ctor>b__0#1");

                        g.VerifyIL(
                        """
                        {
                          // Code size       33 (0x21)
                          .maxstack  3
                          IL_0000:  newobj     0x06000007
                          IL_0005:  stloc.0
                          IL_0006:  ldloc.0
                          IL_0007:  ldarg.1
                          IL_0008:  stfld      0x04000004
                          IL_000d:  ldarg.0
                          IL_000e:  ldloc.0
                          IL_000f:  ldftn      0x06000008
                          IL_0015:  newobj     0x0A000008
                          IL_001a:  call       0x06000001
                          IL_001f:  nop
                          IL_0020:  ret
                        }
                        {
                          // Code size       13 (0xd)
                          .maxstack  8
                          IL_0000:  ldstr      0x70000005
                          IL_0005:  ldc.i4.s   -5
                          IL_0007:  newobj     0x06000006
                          IL_000c:  throw
                        }
                        {
                          // Code size       16 (0x10)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldarg.1
                          IL_0002:  call       0x0A000009
                          IL_0007:  nop
                          IL_0008:  ldarg.0
                          IL_0009:  ldarg.2
                          IL_000a:  stfld      0x04000003
                          IL_000f:  ret
                        }
                        {
                          // Code size        8 (0x8)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  call       0x0A00000A
                          IL_0006:  nop
                          IL_0007:  ret
                        }
                        {
                          // Code size        7 (0x7)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldfld      0x04000004
                          IL_0006:  ret
                        }
                        """);
                    })
                .Verify();
        }

        [Fact]
        public void Capture_TopLevelArgs()
        {
            using var _ = new EditAndContinueTest(options: TestOptions.DebugExe)
                .AddBaseline(
                    source: """
                    <N:0>
                    using System;
                    var _ = new Func<string[]>(<N:1>() => null</N:1>);
                    </N:0>
                    """,
                    validator: g =>
                    {
                    })

                .AddGeneration(
                    source: """
                    <N:0>
                    using System;
                    var _ = new Func<string[]>(<N:1>() => args</N:1>);
                    </N:0>
                    """,
                    edits:
                    [
                        Edit(SemanticEditKind.Update, c => c.GetMember("Program.<Main>$"), preserveLocalVariables: true),
                    ],
                    validator: g =>
                    {
                        g.VerifySynthesizedMembers(
                            "System.Runtime.CompilerServices.HotReloadException",
                            "Program: {<>c__DisplayClass0_0#1}",
                            "Program.<>c__DisplayClass0_0#1: {args, <<Main>$>b__0#1}");

                        g.VerifyMethodDefNames(
                            "<Main>$", "<<Main>$>b__0_0", ".ctor", ".ctor", "<<Main>$>b__0#1");

                        g.VerifyIL(
                        """
                        {
                          // Code size       27 (0x1b)
                          .maxstack  2
                          IL_0000:  newobj     0x06000007
                          IL_0005:  stloc.1
                          IL_0006:  ldloc.1
                          IL_0007:  ldarg.0
                          IL_0008:  stfld      0x04000004
                          IL_000d:  ldloc.1
                          IL_000e:  ldftn      0x06000008
                          IL_0014:  newobj     0x0A000008
                          IL_0019:  stloc.2
                          IL_001a:  ret
                        }
                        {
                          // Code size       13 (0xd)
                          .maxstack  8
                          IL_0000:  ldstr      0x70000005
                          IL_0005:  ldc.i4.s   -5
                          IL_0007:  newobj     0x06000006
                          IL_000c:  throw
                        }
                        {
                          // Code size       16 (0x10)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldarg.1
                          IL_0002:  call       0x0A000009
                          IL_0007:  nop
                          IL_0008:  ldarg.0
                          IL_0009:  ldarg.2
                          IL_000a:  stfld      0x04000003
                          IL_000f:  ret
                        }
                        {
                          // Code size        8 (0x8)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  call       0x0A00000A
                          IL_0006:  nop
                          IL_0007:  ret
                        }
                        {
                          // Code size        7 (0x7)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldfld      0x04000004
                          IL_0006:  ret
                        }
                        """);
                    })
                .Verify();
        }

        [Fact]
        public void Capture_This_Lambda()
        {
            using var _ = new EditAndContinueTest()
                .AddBaseline(
                    source: """
                    using System;
                    class C
                    {
                        int x = 1;
                    
                        public void F()
                        <N:0>{
                            _ = new Action(<N:2>() => Console.WriteLine(0)</N:2>);
                            _ = new Action(<N:3>() => Console.WriteLine(1)</N:3>);
                        }</N:0>
                    }
                    """,
                    validator: g =>
                    {
                        g.VerifyMethodBody("C.<>c.<F>b__1_0", """
                            {
                              // Code size        8 (0x8)
                              .maxstack  1
                              // sequence point: Console.WriteLine(0)
                              IL_0000:  ldc.i4.0
                              IL_0001:  call       "void System.Console.WriteLine(int)"
                              IL_0006:  nop
                              IL_0007:  ret
                            }
                            """);

                        g.VerifyMethodBody("C.<>c.<F>b__1_1", """
                            {
                              // Code size        8 (0x8)
                              .maxstack  1
                              // sequence point: Console.WriteLine(1)
                              IL_0000:  ldc.i4.1
                              IL_0001:  call       "void System.Console.WriteLine(int)"
                              IL_0006:  nop
                              IL_0007:  ret
                            }
                            """);
                    })

                .AddGeneration(
                    source: """
                    using System;
                    class C
                    {
                        int x = 1;
                    
                        public void F()
                        <N:0>{
                            _ = new Action(<N:2>() => Console.WriteLine(x)</N:2>);
                            _ = new Action(<N:3>() => Console.WriteLine(1)</N:3>);
                        }</N:0>
                    }
                    """,
                    edits:
                    [
                        Edit(SemanticEditKind.Update, c => c.GetMember("C.F"), preserveLocalVariables: true),
                    ],
                    validator: g =>
                    {
                        // Static lambda is reused.
                        // A new display class and method is generated for lambda that captures x.
                        g.VerifySynthesizedMembers(
                            "System.Runtime.CompilerServices.HotReloadException",
                            "C: {<F>b__1_0#1, <>c}",
                            "C.<>c: {<>9__1_1, <F>b__1_1}");

                        g.VerifyMethodDefNames("F", "<F>b__1_0", "<F>b__1_1", "<F>b__1_0#1", ".ctor");

                        g.VerifyIL(
                        """
                        {
                          // Code size       31 (0x1f)
                          .maxstack  8
                          IL_0000:  nop
                          IL_0001:  nop
                          IL_0002:  ldsfld     0x04000004
                          IL_0007:  brtrue.s   IL_001e
                          IL_0009:  ldsfld     0x04000002
                          IL_000e:  ldftn      0x06000006
                          IL_0014:  newobj     0x0A000009
                          IL_0019:  stsfld     0x04000004
                          IL_001e:  ret
                        }
                        {
                          // Code size       13 (0xd)
                          .maxstack  8
                          IL_0000:  ldstr      0x70000005
                          IL_0005:  ldc.i4.s   -5
                          IL_0007:  newobj     0x06000008
                          IL_000c:  throw
                        }
                        {
                          // Code size        8 (0x8)
                          .maxstack  8
                          IL_0000:  ldc.i4.1
                          IL_0001:  call       0x0A00000A
                          IL_0006:  nop
                          IL_0007:  ret
                        }
                        {
                          // Code size       13 (0xd)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldfld      0x04000001
                          IL_0006:  call       0x0A00000A
                          IL_000b:  nop
                          IL_000c:  ret
                        }
                        {
                          // Code size       16 (0x10)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldarg.1
                          IL_0002:  call       0x0A00000B
                          IL_0007:  nop
                          IL_0008:  ldarg.0
                          IL_0009:  ldarg.2
                          IL_000a:  stfld      0x04000005
                          IL_000f:  ret
                        }
                        """);
                    })
                .Verify();
        }

        [Fact]
        public void Capture_This_LocalFunction()
        {
            // Rude edit since we can't convert static method to an instance method.
            using var _ = new EditAndContinueTest()
                .AddBaseline(
                    source: """
                    using System;
                    class C
                    {
                        int x = 1;
                    
                        public void F()
                        <N:0>{
                            <N:2>void L1() => Console.WriteLine(0);</N:2>
                            <N:3>void L2() => Console.WriteLine(1);</N:3>
                        }</N:0>
                    }
                    """,
                    validator: g =>
                    {
                        g.VerifyMethodBody("C.<F>g__L1|1_0", """
                            {
                              // Code size        8 (0x8)
                              .maxstack  1
                              // sequence point: Console.WriteLine(0)
                              IL_0000:  ldc.i4.0
                              IL_0001:  call       "void System.Console.WriteLine(int)"
                              IL_0006:  nop
                              IL_0007:  ret
                            }
                            """);

                        g.VerifyMethodBody("C.<F>g__L2|1_1", """
                            {
                              // Code size        8 (0x8)
                              .maxstack  1
                              // sequence point: Console.WriteLine(1)
                              IL_0000:  ldc.i4.1
                              IL_0001:  call       "void System.Console.WriteLine(int)"
                              IL_0006:  nop
                              IL_0007:  ret
                            }
                            """);
                    })

                .AddGeneration(
                    source: """
                    using System;
                    class C
                    {
                        int x = 1;
                    
                        public void F()
                        <N:0>{
                            <N:2>void L1() => Console.WriteLine(x);</N:2>
                            <N:3>void L2() => Console.WriteLine(1);</N:3>
                        }</N:0>
                    }
                    """,
                    edits:
                    [
                        Edit(SemanticEditKind.Update, c => c.GetMember("C.F"), preserveLocalVariables: true),
                    ],
                    validator: g =>
                    {
                        g.VerifySynthesizedMembers(
                            "System.Runtime.CompilerServices.HotReloadException",
                            "C: {<F>g__L1|1_0#1, <F>g__L2|1_1}");

                        g.VerifyMethodDefNames("F", "<F>g__L1|1_0", "<F>g__L2|1_1", "<F>g__L1|1_0#1", ".ctor");

                        g.VerifyIL(
                        """
                        {
                          // Code size        4 (0x4)
                          .maxstack  8
                          IL_0000:  nop
                          IL_0001:  nop
                          IL_0002:  nop
                          IL_0003:  ret
                        }
                        {
                          // Code size       13 (0xd)
                          .maxstack  8
                          IL_0000:  ldstr      0x70000005
                          IL_0005:  ldc.i4.s   -5
                          IL_0007:  newobj     0x06000006
                          IL_000c:  throw
                        }
                        {
                          // Code size        8 (0x8)
                          .maxstack  8
                          IL_0000:  ldc.i4.1
                          IL_0001:  call       0x0A000008
                          IL_0006:  nop
                          IL_0007:  ret
                        }
                        {
                          // Code size       13 (0xd)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldfld      0x04000001
                          IL_0006:  call       0x0A000008
                          IL_000b:  nop
                          IL_000c:  ret
                        }
                        {
                          // Code size       16 (0x10)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldarg.1
                          IL_0002:  call       0x0A000009
                          IL_0007:  nop
                          IL_0008:  ldarg.0
                          IL_0009:  ldarg.2
                          IL_000a:  stfld      0x04000002
                          IL_000f:  ret
                        }
                        """);
                    })
                .Verify();
        }

        [Fact]
        public void Capture_PrimaryParameter_Lambda()
        {
            using var _ = new EditAndContinueTest()
                .AddBaseline(
                    source: """
                    using System;
                    class C(int x)
                    {
                        public void F()
                        <N:0>{
                            _ = new Action(<N:2>() => Console.WriteLine(0)</N:2>);
                            _ = new Action(<N:3>() => Console.WriteLine(1)</N:3>);
                        }</N:0>
                    }
                    """,
                    validator: g =>
                    {
                    })

                .AddGeneration(
                    source: """
                    using System;
                    class C(int x)
                    {
                        public void F()
                        <N:0>{
                            _ = new Action(<N:2>() => Console.WriteLine(x)</N:2>);
                            _ = new Action(<N:3>() => Console.WriteLine(2)</N:3>);
                        }</N:0>
                    }
                    """,
                    edits:
                    [
                        Edit(SemanticEditKind.Update, c => c.GetMember("C.F"), preserveLocalVariables: true),
                    ],
                    validator: g =>
                    {
                        // Static lambda is reused.
                        // A new display class and method is generated for lambda that captures x.
                        g.VerifySynthesizedMembers(
                            "System.Runtime.CompilerServices.HotReloadException",
                            "C: {<F>b__1_0#1, <>c}",
                            "C.<>c: {<>9__1_1, <F>b__1_1}");

                        g.VerifyMethodDefNames("F", "<F>b__1_0", "<F>b__1_1", "<F>b__1_0#1", ".ctor");

                        g.VerifyIL(
                        """
                        {
                          // Code size       31 (0x1f)
                          .maxstack  8
                          IL_0000:  nop
                          IL_0001:  nop
                          IL_0002:  ldsfld     0x04000003
                          IL_0007:  brtrue.s   IL_001e
                          IL_0009:  ldsfld     0x04000001
                          IL_000e:  ldftn      0x06000006
                          IL_0014:  newobj     0x0A00000A
                          IL_0019:  stsfld     0x04000003
                          IL_001e:  ret
                        }
                        {
                          // Code size       13 (0xd)
                          .maxstack  8
                          IL_0000:  ldstr      0x70000005
                          IL_0005:  ldc.i4.s   -5
                          IL_0007:  newobj     0x06000008
                          IL_000c:  throw
                        }
                        {
                          // Code size        8 (0x8)
                          .maxstack  8
                          IL_0000:  ldc.i4.2
                          IL_0001:  call       0x0A00000B
                          IL_0006:  nop
                          IL_0007:  ret
                        }
                        {
                          // Code size       13 (0xd)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldfld      0x04000004
                          IL_0006:  call       0x0A00000B
                          IL_000b:  nop
                          IL_000c:  ret
                        }
                        {
                          // Code size       16 (0x10)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldarg.1
                          IL_0002:  call       0x0A00000C
                          IL_0007:  nop
                          IL_0008:  ldarg.0
                          IL_0009:  ldarg.2
                          IL_000a:  stfld      0x04000005
                          IL_000f:  ret
                        }
                        """);
                    })
                .Verify();
        }

        [Fact]
        public void Capture_PrimaryParameter_LocalFunction()
        {
            using var _ = new EditAndContinueTest()
                .AddBaseline(
                    source: """
                    using System;
                    class C(int x)
                    {
                        public void F()
                        <N:0>{
                            <N:2>void L1() => Console.WriteLine(0);</N:2>
                            <N:3>void L2() => Console.WriteLine(1);</N:3>
                        }</N:0>
                    }
                    """,
                    validator: g =>
                    {
                    })

                .AddGeneration(
                    source: """
                    using System;
                    class C(int x)
                    {
                        public void F()
                        <N:0>{
                            <N:2>void L1() => Console.WriteLine(x);</N:2>
                            <N:3>void L2() => Console.WriteLine(1);</N:3>
                        }</N:0>
                    }
                    """,
                    edits:
                    [
                        Edit(SemanticEditKind.Update, c => c.GetMember("C.F"), preserveLocalVariables: true),
                    ],
                    validator: g =>
                    {
                        g.VerifySynthesizedMembers(
                            "System.Runtime.CompilerServices.HotReloadException",
                            "C: {<F>g__L1|1_0#1, <F>g__L2|1_1}");

                        g.VerifyMethodDefNames("F", "<F>g__L1|1_0", "<F>g__L2|1_1", "<F>g__L1|1_0#1", ".ctor");

                        g.VerifyIL(
                        """
                        {
                          // Code size        4 (0x4)
                          .maxstack  8
                          IL_0000:  nop
                          IL_0001:  nop
                          IL_0002:  nop
                          IL_0003:  ret
                        }
                        {
                          // Code size       13 (0xd)
                          .maxstack  8
                          IL_0000:  ldstr      0x70000005
                          IL_0005:  ldc.i4.s   -5
                          IL_0007:  newobj     0x06000006
                          IL_000c:  throw
                        }
                        {
                          // Code size        8 (0x8)
                          .maxstack  8
                          IL_0000:  ldc.i4.1
                          IL_0001:  call       0x0A000009
                          IL_0006:  nop
                          IL_0007:  ret
                        }
                        {
                          // Code size       13 (0xd)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldfld      0x04000001
                          IL_0006:  call       0x0A000009
                          IL_000b:  nop
                          IL_000c:  ret
                        }
                        {
                          // Code size       16 (0x10)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldarg.1
                          IL_0002:  call       0x0A00000A
                          IL_0007:  nop
                          IL_0008:  ldarg.0
                          IL_0009:  ldarg.2
                          IL_000a:  stfld      0x04000002
                          IL_000f:  ret
                        }
                        """);
                    })
                .Verify();
        }

        [Fact]
        public void CeaseCapture_Local_Lambda()
        {
            using var _ = new EditAndContinueTest()
                .AddBaseline(
                    source: """
                    using System;
                    class C
                    {
                        static void G(Func<int> f) {}

                        public void F()
                        <N:0>{
                            int <N:1>x = 1</N:1>;
                            int <N:2>y = 2</N:2>;
                            G(<N:3>() => x + y</N:3>);
                        }</N:0>
                    }
                    """,
                    validator: g =>
                    {
                        g.VerifyCustomDebugInformation("C.F", """
                            <symbols>
                              <methods>
                                <method containingType="C" name="F">
                                  <customDebugInfo>
                                    <forward declaringType="C" methodName="G" parameterNames="f" />
                                    <encLocalSlotMap>
                                      <slot kind="30" offset="0" />
                                    </encLocalSlotMap>
                                    <encLambdaMap>
                                      <methodOrdinal>1</methodOrdinal>
                                      <closure offset="0" />
                                      <lambda offset="86" closure="0" />
                                    </encLambdaMap>
                                  </customDebugInfo>
                                </method>
                              </methods>
                            </symbols>
                            """);

                        g.VerifyMethodBody("C.<>c__DisplayClass1_0.<F>b__0", """
                            {
                              // Code size       14 (0xe)
                              .maxstack  2
                              // sequence point: x + y
                              IL_0000:  ldarg.0
                              IL_0001:  ldfld      "int C.<>c__DisplayClass1_0.x"
                              IL_0006:  ldarg.0
                              IL_0007:  ldfld      "int C.<>c__DisplayClass1_0.y"
                              IL_000c:  add
                              IL_000d:  ret
                            }
                            """);
                    })

                .AddGeneration(
                    source: """
                    using System;
                    class C
                    {
                        static void G(Func<int> f) {}

                        public void F()
                        <N:0>{
                            int <N:1>x = 1</N:1>;
                            int <N:2>y = 2</N:2>;
                            G(<N:3>() => x</N:3>);
                        }</N:0>
                    }
                    """,
                    edits:
                    [
                        Edit(SemanticEditKind.Update, c => c.GetMember("C.F"), preserveLocalVariables: true),
                    ],
                    validator: g =>
                    {
                        g.VerifySynthesizedMembers(
                            "C: {<>c__DisplayClass1_0}",
                            "C.<>c__DisplayClass1_0: {x, <F>b__0}");

                        g.VerifyMethodDefNames("F", "<F>b__0");

                        g.VerifyIL(
                        """
                        {
                          // Code size       35 (0x23)
                          .maxstack  2
                          IL_0000:  newobj     0x06000004
                          IL_0005:  stloc.0
                          IL_0006:  nop
                          IL_0007:  ldloc.0
                          IL_0008:  ldc.i4.1
                          IL_0009:  stfld      0x04000001
                          IL_000e:  ldc.i4.2
                          IL_000f:  stloc.1
                          IL_0010:  ldloc.0
                          IL_0011:  ldftn      0x06000005
                          IL_0017:  newobj     0x0A000008
                          IL_001c:  call       0x06000001
                          IL_0021:  nop
                          IL_0022:  ret
                        }
                        {
                          // Code size        7 (0x7)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldfld      0x04000001
                          IL_0006:  ret
                        }
                        """);
                    })
                .Verify();
        }

        [Fact]
        public void CeaseCapture_Local_LocalFunction()
        {
            using var _ = new EditAndContinueTest()
                .AddBaseline(
                    source: """
                    using System;
                    class C
                    {
                        public void F()
                        <N:0>{
                            int <N:1>x = 1</N:1>;
                            int <N:2>y = 2</N:2>;
                            <N:3>int L() => x + y;</N:3>
                        }</N:0>
                    }
                    """,
                    validator: g =>
                    {
                        g.VerifySynthesizedMembers(displayTypeKind: true,
                        [
                            "struct C.<>c__DisplayClass0_0: {x, y}",
                            "class C: {<F>g__L|0_0, <>c__DisplayClass0_0}"
                        ]);

                        g.VerifyMethodBody("C.<F>g__L|0_0", """
                            {
                              // Code size       14 (0xe)
                              .maxstack  2
                              // sequence point: x + y
                              IL_0000:  ldarg.0
                              IL_0001:  ldfld      "int C.<>c__DisplayClass0_0.x"
                              IL_0006:  ldarg.0
                              IL_0007:  ldfld      "int C.<>c__DisplayClass0_0.y"
                              IL_000c:  add
                              IL_000d:  ret
                            }
                            """);
                    })

                .AddGeneration(
                    source: """
                    using System;
                    class C
                    {
                        public void F()
                        <N:0>{
                            int <N:1>x = 1</N:1>;
                            int <N:2>y = 2</N:2>;
                            <N:3>int L() => x;</N:3>
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
                            "struct C.<>c__DisplayClass0_0: {x}",
                            "class C: {<F>g__L|0_0, <>c__DisplayClass0_0}"
                        ]);

                        g.VerifyMethodDefNames("F", "<F>g__L|0_0");

                        g.VerifyIL(
                        """
                        {
                          // Code size       13 (0xd)
                          .maxstack  2
                          IL_0000:  nop
                          IL_0001:  ldloca.s   V_0
                          IL_0003:  ldc.i4.1
                          IL_0004:  stfld      0x04000001
                          IL_0009:  ldc.i4.2
                          IL_000a:  stloc.1
                          IL_000b:  nop
                          IL_000c:  ret
                        }
                        {
                          // Code size        7 (0x7)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldfld      0x04000001
                          IL_0006:  ret
                        }
                        """);
                    })
                .Verify();
        }

        [Fact]
        public void CeaseCapture_LastLocal_Lambda()
        {
            using var _ = new EditAndContinueTest()
                .AddBaseline(
                    source: """
                    using System;
                    class C
                    {
                        public void F()
                        <N:0>{
                            int <N:1>x = 1</N:1>;
                            _ = new Action(<N:2>() => Console.WriteLine(x)</N:2>);
                        }</N:0>
                    }
                    """,
                    validator: g =>
                    {
                    })
                .AddGeneration(
                    // 1
                    source: """
                    using System;
                    class C
                    {
                        public void F()
                        <N:0>{
                            int <N:1>x = 1</N:1>;
                            _ = new Action(<N:2>() => Console.WriteLine(2)</N:2>);
                        }</N:0>
                    }
                    """,
                    edits:
                    [
                        Edit(SemanticEditKind.Update, c => c.GetMember("C.F"), preserveLocalVariables: true),
                    ],
                    validator: g =>
                    {
                        g.VerifySynthesizedMembers(
                            "System.Runtime.CompilerServices.HotReloadException",
                            "C: {<>c}",
                            "C.<>c: {<>9__0_0#1, <F>b__0_0#1}");

                        g.VerifyMethodDefNames("F", "<F>b__0", ".ctor", ".cctor", ".ctor", "<F>b__0_0#1");

                        g.VerifyIL(
                        """
                        {
                          // Code size       32 (0x20)
                          .maxstack  2
                          IL_0000:  nop
                          IL_0001:  ldc.i4.1
                          IL_0002:  stloc.1
                          IL_0003:  ldsfld     0x04000004
                          IL_0008:  brtrue.s   IL_001f
                          IL_000a:  ldsfld     0x04000003
                          IL_000f:  ldftn      0x06000008
                          IL_0015:  newobj     0x0A000008
                          IL_001a:  stsfld     0x04000004
                          IL_001f:  ret
                        }
                        {
                          // Code size       13 (0xd)
                          .maxstack  8
                          IL_0000:  ldstr      0x70000005
                          IL_0005:  ldc.i4.s   -5
                          IL_0007:  newobj     0x06000005
                          IL_000c:  throw
                        }
                        {
                          // Code size       16 (0x10)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldarg.1
                          IL_0002:  call       0x0A000009
                          IL_0007:  nop
                          IL_0008:  ldarg.0
                          IL_0009:  ldarg.2
                          IL_000a:  stfld      0x04000002
                          IL_000f:  ret
                        }
                        {
                          // Code size       11 (0xb)
                          .maxstack  8
                          IL_0000:  newobj     0x06000007
                          IL_0005:  stsfld     0x04000003
                          IL_000a:  ret
                        }
                        {
                          // Code size        8 (0x8)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  call       0x0A00000A
                          IL_0006:  nop
                          IL_0007:  ret
                        }
                        {
                          // Code size        8 (0x8)
                          .maxstack  8
                          IL_0000:  ldc.i4.2
                          IL_0001:  call       0x0A00000B
                          IL_0006:  nop
                          IL_0007:  ret
                        }
                        """);
                    })
                .AddGeneration(
                    // 2: resume capture
                    source: """
                    using System;
                    class C
                    {
                        public void F()
                        <N:0>{
                            int <N:1>x = 1</N:1>;
                            _ = new Action(<N:2>() => Console.WriteLine(x + 3)</N:2>);
                        }</N:0>
                    }
                    """,
                    edits:
                    [
                        Edit(SemanticEditKind.Update, c => c.GetMember("C.F"), preserveLocalVariables: true),
                    ],
                    validator: g =>
                    {
                        g.VerifySynthesizedMembers(
                            "System.Runtime.CompilerServices.HotReloadException",
                            "C: {<>c__DisplayClass0_0#2, <>c}",
                            "C.<>c: {<>9__0_0#1, <F>b__0_0#1}",
                            "C.<>c__DisplayClass0_0#2: {x, <F>b__0#2}");

                        // <HotReloadException> was emitted in previous generation and TypeRef is emitted to the current one:
                        g.VerifyTypeRefNames("Object", "CompilerGeneratedAttribute", "Console");

                        g.VerifyMethodDefNames("F", "<F>b__0_0#1", ".ctor", "<F>b__0#2");

                        g.VerifyIL(
                        """
                        {
                          // Code size       16 (0x10)
                          .maxstack  2
                          IL_0000:  newobj     0x06000009
                          IL_0005:  stloc.2
                          IL_0006:  nop
                          IL_0007:  ldloc.2
                          IL_0008:  ldc.i4.1
                          IL_0009:  stfld      0x04000005
                          IL_000e:  nop
                          IL_000f:  ret
                        }
                        {
                          // Code size       12 (0xc)
                          .maxstack  8
                          IL_0000:  ldstr      0x700001F5
                          IL_0005:  ldc.i4.m1
                          IL_0006:  newobj     0x06000005
                          IL_000b:  throw
                        }
                        {
                          // Code size        8 (0x8)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  call       0x0A00000D
                          IL_0006:  nop
                          IL_0007:  ret
                        }
                        {
                          // Code size       15 (0xf)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldfld      0x04000005
                          IL_0006:  ldc.i4.3
                          IL_0007:  add
                          IL_0008:  call       0x0A00000E
                          IL_000d:  nop
                          IL_000e:  ret
                        }
                        """);
                    })
                .Verify();
        }

        [Fact]
        public void CeaseCapture_LastLocal_LocalFunction()
        {
            using var _ = new EditAndContinueTest()
                .AddBaseline(
                    source: """
                    using System;
                    class C
                    {
                        public void F()
                        <N:0>{
                            <N:1>{
                                int <N:2>x = 1</N:2>;
                                <N:3>void L() => Console.WriteLine(x);</N:3>;
                            }</N:1>
                            <N:4>{
                                int <N:5>x = 1</N:5>;
                                <N:6>void L() => Console.WriteLine(x);</N:6>;
                            }</N:4>
                        }</N:0>
                    }
                    """,
                    validator: g =>
                    {
                        g.VerifyCustomDebugInformation("C.F", """
                            <symbols>
                              <methods>
                                <method containingType="C" name="F">
                                  <customDebugInfo>
                                    <using>
                                      <namespace usingCount="1" />
                                    </using>
                                    <encLocalSlotMap>
                                      <slot kind="30" offset="16" />
                                      <slot kind="30" offset="143" />
                                    </encLocalSlotMap>
                                    <encLambdaMap>
                                      <methodOrdinal>0</methodOrdinal>
                                      <closure offset="16" />
                                      <closure offset="143" />
                                      <lambda offset="83" />
                                      <lambda offset="210" />
                                    </encLambdaMap>
                                  </customDebugInfo>
                                </method>
                              </methods>
                            </symbols>
                            """);
                    })
                .AddGeneration(
                    source: """
                    using System;
                    class C
                    {
                        public void F()
                        <N:0>{
                            <N:1>{
                                int <N:2>x = 1</N:2>;
                                <N:3>void L() => Console.WriteLine(1);</N:3>;
                            }</N:1>
                            <N:4>{
                                int <N:5>x = 1</N:5>;
                                <N:6>void L() => Console.WriteLine(2);</N:6>;
                            }</N:4>
                        }</N:0>
                    }
                    """,
                    edits:
                    [
                        Edit(SemanticEditKind.Update, c => c.GetMember("C.F"), preserveLocalVariables: true),
                    ],
                    validator: g =>
                    {
                        g.VerifySynthesizedMembers(
                            "System.Runtime.CompilerServices.HotReloadException",
                            "C: {<F>g__L|0_0#1, <F>g__L|0_1#1}");

                        g.VerifyMethodDefNames("F", "<F>g__L|0_0", "<F>g__L|0_1", "<F>g__L|0_0#1", "<F>g__L|0_1#1", ".ctor");

                        g.VerifyIL(
                        """
                        {
                          // Code size       14 (0xe)
                          .maxstack  1
                          IL_0000:  nop
                          IL_0001:  nop
                          IL_0002:  ldc.i4.1
                          IL_0003:  stloc.2
                          IL_0004:  nop
                          IL_0005:  nop
                          IL_0006:  nop
                          IL_0007:  nop
                          IL_0008:  ldc.i4.1
                          IL_0009:  stloc.3
                          IL_000a:  nop
                          IL_000b:  nop
                          IL_000c:  nop
                          IL_000d:  ret
                        }
                        {
                          // Code size       13 (0xd)
                          .maxstack  8
                          IL_0000:  ldstr      0x70000005
                          IL_0005:  ldc.i4.s   -5
                          IL_0007:  newobj     0x06000007
                          IL_000c:  throw
                        }
                        {
                          // Code size       12 (0xc)
                          .maxstack  8
                          IL_0000:  ldstr      0x700001F4
                          IL_0005:  ldc.i4.m1
                          IL_0006:  newobj     0x06000007
                          IL_000b:  throw
                        }
                        {
                          // Code size        8 (0x8)
                          .maxstack  8
                          IL_0000:  ldc.i4.1
                          IL_0001:  call       0x0A000008
                          IL_0006:  nop
                          IL_0007:  ret
                        }
                        {
                          // Code size        8 (0x8)
                          .maxstack  8
                          IL_0000:  ldc.i4.2
                          IL_0001:  call       0x0A000008
                          IL_0006:  nop
                          IL_0007:  ret
                        }
                        {
                          // Code size       16 (0x10)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldarg.1
                          IL_0002:  call       0x0A000009
                          IL_0007:  nop
                          IL_0008:  ldarg.0
                          IL_0009:  ldarg.2
                          IL_000a:  stfld      0x04000003
                          IL_000f:  ret
                        }
                        """);
                    })
                .Verify();
        }

        [Fact]
        public void CeaseCapture_This_Lambda()
        {
            using var _ = new EditAndContinueTest()
                .AddBaseline(
                    source: """
                    using System;
                    class C
                    {
                        int x = 1;
                    
                        public void F()
                        <N:0>{
                            _ = new Action(<N:2>() => Console.WriteLine(x)</N:2>);
                            _ = new Action(<N:3>() => Console.WriteLine(1)</N:3>);
                        }</N:0>
                    }
                    """,
                    validator: g =>
                    {
                        g.VerifyCustomDebugInformation("C.F", """
                            <symbols>
                              <methods>
                                <method containingType="C" name="F">
                                  <customDebugInfo>
                                    <using>
                                      <namespace usingCount="1" />
                                    </using>
                                    <encLambdaMap>
                                      <methodOrdinal>1</methodOrdinal>
                                      <lambda offset="37" closure="this" />
                                      <lambda offset="101" />
                                    </encLambdaMap>
                                  </customDebugInfo>
                                </method>
                              </methods>
                            </symbols>
                            """);

                        g.VerifyMethodBody("C.<F>b__1_0", """
                            {
                              // Code size       13 (0xd)
                              .maxstack  1
                              // sequence point: Console.WriteLine(x)
                              IL_0000:  ldarg.0
                              IL_0001:  ldfld      "int C.x"
                              IL_0006:  call       "void System.Console.WriteLine(int)"
                              IL_000b:  nop
                              IL_000c:  ret
                            }
                            """);

                        g.VerifyMethodBody("C.<>c.<F>b__1_1", """
                            {
                              // Code size        8 (0x8)
                              .maxstack  1
                              // sequence point: Console.WriteLine(1)
                              IL_0000:  ldc.i4.1
                              IL_0001:  call       "void System.Console.WriteLine(int)"
                              IL_0006:  nop
                              IL_0007:  ret
                            }
                            """);
                    })

                .AddGeneration(
                    // 1
                    source: """
                    using System;
                    class C
                    {
                        int x = 1;
                    
                        public void F()
                        <N:0>{
                            _ = new Action(<N:2>() => Console.WriteLine(0)</N:2>);
                            _ = new Action(<N:3>() => Console.WriteLine(1)</N:3>);
                        }</N:0>
                    }
                    """,
                    edits:
                    [
                        Edit(SemanticEditKind.Update, c => c.GetMember("C.F"), preserveLocalVariables: true),
                    ],
                    validator: g =>
                    {
                        // Static lambda is reused.
                        // A new display class and method is generated for lambda that captured x.
                        g.VerifySynthesizedMembers(
                            "System.Runtime.CompilerServices.HotReloadException",
                            "C: {<>c}",
                            "C.<>c: {<>9__1_0#1, <>9__1_1, <F>b__1_0#1, <F>b__1_1}");

                        g.VerifyMethodDefNames("F", "<F>b__1_0", "<F>b__1_1", ".ctor", "<F>b__1_0#1");

                        g.VerifyIL(
                        """
                        {
                          // Code size       58 (0x3a)
                          .maxstack  8
                          IL_0000:  nop
                          IL_0001:  ldsfld     0x04000005
                          IL_0006:  brtrue.s   IL_001d
                          IL_0008:  ldsfld     0x04000002
                          IL_000d:  ldftn      0x06000008
                          IL_0013:  newobj     0x0A000009
                          IL_0018:  stsfld     0x04000005
                          IL_001d:  ldsfld     0x04000003
                          IL_0022:  brtrue.s   IL_0039
                          IL_0024:  ldsfld     0x04000002
                          IL_0029:  ldftn      0x06000006
                          IL_002f:  newobj     0x0A000009
                          IL_0034:  stsfld     0x04000003
                          IL_0039:  ret
                        }
                        {
                          // Code size       13 (0xd)
                          .maxstack  8
                          IL_0000:  ldstr      0x70000005
                          IL_0005:  ldc.i4.s   -5
                          IL_0007:  newobj     0x06000007
                          IL_000c:  throw
                        }
                        {
                          // Code size        8 (0x8)
                          .maxstack  8
                          IL_0000:  ldc.i4.1
                          IL_0001:  call       0x0A00000A
                          IL_0006:  nop
                          IL_0007:  ret
                        }
                        {
                          // Code size       16 (0x10)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldarg.1
                          IL_0002:  call       0x0A00000B
                          IL_0007:  nop
                          IL_0008:  ldarg.0
                          IL_0009:  ldarg.2
                          IL_000a:  stfld      0x04000004
                          IL_000f:  ret
                        }
                        {
                          // Code size        8 (0x8)
                          .maxstack  8
                          IL_0000:  ldc.i4.0
                          IL_0001:  call       0x0A00000A
                          IL_0006:  nop
                          IL_0007:  ret
                        }
                        """);
                    })
                .Verify();
        }

        [Fact]
        public void CeaseCapture_This_LocalFunction()
        {
            using var _ = new EditAndContinueTest()
                .AddBaseline(
                    source: """
                    using System;
                    class C
                    {
                        int x = 1;
                    
                        public void F()
                        <N:0>{
                            <N:2>void L1() => Console.WriteLine(x);</N:2>
                            <N:3>void L2() => Console.WriteLine(1);</N:3>
                        }</N:0>
                    }
                    """,
                    validator: g =>
                    {
                        g.VerifyCustomDebugInformation("C.F", """
                            <symbols>
                              <methods>
                                <method containingType="C" name="F">
                                  <customDebugInfo>
                                    <using>
                                      <namespace usingCount="1" />
                                    </using>
                                    <encLambdaMap>
                                      <methodOrdinal>1</methodOrdinal>
                                      <lambda offset="29" closure="this" />
                                      <lambda offset="84" />
                                    </encLambdaMap>
                                  </customDebugInfo>
                                </method>
                              </methods>
                            </symbols>
                            """);

                        g.VerifyMethodBody("C.<F>g__L1|1_0", """
                            {
                              // Code size       13 (0xd)
                              .maxstack  1
                              // sequence point: Console.WriteLine(x)
                              IL_0000:  ldarg.0
                              IL_0001:  ldfld      "int C.x"
                              IL_0006:  call       "void System.Console.WriteLine(int)"
                              IL_000b:  nop
                              IL_000c:  ret
                            }
                            """);

                        g.VerifyMethodBody("C.<F>g__L2|1_1", """
                            {
                              // Code size        8 (0x8)
                              .maxstack  1
                              // sequence point: Console.WriteLine(1)
                              IL_0000:  ldc.i4.1
                              IL_0001:  call       "void System.Console.WriteLine(int)"
                              IL_0006:  nop
                              IL_0007:  ret
                            }
                            """);
                    })

                .AddGeneration(
                    source: """
                    using System;
                    class C
                    {
                        int x = 1;
                    
                        public void F()
                        <N:0>{
                            <N:2>void L1() => Console.WriteLine(0);</N:2>
                            <N:3>void L2() => Console.WriteLine(1);</N:3>
                        }</N:0>
                    }
                    """,
                    edits:
                    [
                        Edit(SemanticEditKind.Update, c => c.GetMember("C.F"), preserveLocalVariables: true),
                    ],
                    validator: g =>
                    {
                        g.VerifySynthesizedMembers(
                            "System.Runtime.CompilerServices.HotReloadException",
                            "C: {<F>g__L1|1_0#1, <F>g__L2|1_1}");

                        g.VerifyMethodDefNames("F", "<F>g__L1|1_0", "<F>g__L2|1_1", "<F>g__L1|1_0#1", ".ctor");

                        g.VerifyIL(
                        """
                        {
                          // Code size        4 (0x4)
                          .maxstack  8
                          IL_0000:  nop
                          IL_0001:  nop
                          IL_0002:  nop
                          IL_0003:  ret
                        }
                        {
                          // Code size       13 (0xd)
                          .maxstack  8
                          IL_0000:  ldstr      0x70000005
                          IL_0005:  ldc.i4.s   -5
                          IL_0007:  newobj     0x06000006
                          IL_000c:  throw
                        }
                        {
                          // Code size        8 (0x8)
                          .maxstack  8
                          IL_0000:  ldc.i4.1
                          IL_0001:  call       0x0A000008
                          IL_0006:  nop
                          IL_0007:  ret
                        }
                        {
                          // Code size        8 (0x8)
                          .maxstack  8
                          IL_0000:  ldc.i4.0
                          IL_0001:  call       0x0A000008
                          IL_0006:  nop
                          IL_0007:  ret
                        }
                        {
                          // Code size       16 (0x10)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldarg.1
                          IL_0002:  call       0x0A000009
                          IL_0007:  nop
                          IL_0008:  ldarg.0
                          IL_0009:  ldarg.2
                          IL_000a:  stfld      0x04000002
                          IL_000f:  ret
                        }
                        """);
                    })
                .Verify();
        }

        [Fact]
        public void AddingAndRemovingClosure_Lambda()
        {
            using var _ = new EditAndContinueTest()
                .AddBaseline(
                    source: """
                    using System;
                    class C
                    {
                        public void F()
                        <N:0>{
                            int <N:1>x = 1</N:1>;
                        }</N:0>
                    }
                    """,
                    validator: g =>
                    {
                    })
                .AddGeneration(
                    // 1
                    source: """
                    using System;
                    class C
                    {
                        public void F()
                        <N:0>{
                            int <N:1>x = 1</N:1>;
                            _ = new Action(() => Console.WriteLine(x));
                        }</N:0>
                    }
                    """,
                    edits:
                    [
                        Edit(SemanticEditKind.Update, c => c.GetMember("C.F"), preserveLocalVariables: true),
                    ],
                    validator: g =>
                    {
                        // A new display class and method is generated for lambda that captures x.
                        g.VerifySynthesizedMembers(
                            "C: {<>c__DisplayClass0#1_0#1}",
                            "C.<>c__DisplayClass0#1_0#1: {x, <F>b__0#1}");

                        g.VerifyMethodDefNames("F", ".ctor", "<F>b__0#1");

                        g.VerifyTypeRefNames("Object", "CompilerGeneratedAttribute", "Console");

                        g.VerifyIL(
                        """
                        {
                          // Code size       16 (0x10)
                          .maxstack  2
                          IL_0000:  newobj     0x06000003
                          IL_0005:  stloc.1
                          IL_0006:  nop
                          IL_0007:  ldloc.1
                          IL_0008:  ldc.i4.1
                          IL_0009:  stfld      0x04000001
                          IL_000e:  nop
                          IL_000f:  ret
                        }
                        {
                          // Code size        8 (0x8)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  call       0x0A000006
                          IL_0006:  nop
                          IL_0007:  ret
                        }
                        {
                          // Code size       13 (0xd)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldfld      0x04000001
                          IL_0006:  call       0x0A000007
                          IL_000b:  nop
                          IL_000c:  ret
                        }
                        """);
                    })
                .AddGeneration(
                    // 2: remove closure
                    source: """
                    using System;
                    class C
                    {
                        public void F()
                        <N:0>{
                            int <N:1>x = 1</N:1>;
                        }</N:0>
                    }
                    """,
                    edits:
                    [
                        Edit(SemanticEditKind.Update, c => c.GetMember("C.F"), preserveLocalVariables: true),
                    ],
                    validator: g =>
                    {
                        g.VerifySynthesizedMembers(
                            "System.Runtime.CompilerServices.HotReloadException",
                            "C: {<>c__DisplayClass0#1_0#1}",
                            "C.<>c__DisplayClass0#1_0#1: {x, <F>b__0#1}");

                        // <HotReloadException> is emitted as a definition in this generation
                        g.VerifyTypeRefNames("Object", "CompilerGeneratedAttribute", "Exception");

                        g.VerifyMethodDefNames("F", "<F>b__0#1", ".ctor");

                        g.VerifyIL("""
                        {
                          // Code size        4 (0x4)
                          .maxstack  1
                          IL_0000:  nop
                          IL_0001:  ldc.i4.1
                          IL_0002:  stloc.2
                          IL_0003:  ret
                        }
                        {
                          // Code size       12 (0xc)
                          .maxstack  8
                          IL_0000:  ldstr      0x70000009
                          IL_0005:  ldc.i4.m1
                          IL_0006:  newobj     0x06000005
                          IL_000b:  throw
                        }
                        {
                          // Code size       16 (0x10)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldarg.1
                          IL_0002:  call       0x0A000009
                          IL_0007:  nop
                          IL_0008:  ldarg.0
                          IL_0009:  ldarg.2
                          IL_000a:  stfld      0x04000002
                          IL_000f:  ret
                        }
                        """);
                    })
                .Verify();
        }

        [Fact]
        public void AddingAndRemovingClosure_LocalFunction()
        {
            using var _ = new EditAndContinueTest()
                .AddBaseline(
                    source: """
                    using System;
                    class C
                    {
                        public void F()
                        <N:0>{
                            int <N:1>x = 1</N:1>;
                        }</N:0>
                    }
                    """,
                    validator: g =>
                    {
                    })
                .AddGeneration(
                    // 1
                    source: """
                    using System;
                    class C
                    {
                        public void F()
                        <N:0>{
                            int <N:1>x = 1</N:1>;
                            void L() => Console.WriteLine(x);
                        }</N:0>
                    }
                    """,
                    edits:
                    [
                        Edit(SemanticEditKind.Update, c => c.GetMember("C.F"), preserveLocalVariables: true),
                    ],
                    validator: g =>
                    {
                        // A new display class and method is generated for lambda that captures x.
                        g.VerifySynthesizedMembers(
                            "C: {<F>g__L|0#1_0#1, <>c__DisplayClass0#1_0#1}",
                            "C.<>c__DisplayClass0#1_0#1: {x}");

                        g.VerifyMethodDefNames("F", "<F>g__L|0#1_0#1");

                        g.VerifyIL(
                        """
                        {
                          // Code size       11 (0xb)
                          .maxstack  2
                          IL_0000:  nop
                          IL_0001:  ldloca.s   V_1
                          IL_0003:  ldc.i4.1
                          IL_0004:  stfld      0x04000001
                          IL_0009:  nop
                          IL_000a:  ret
                        }
                        {
                          // Code size       13 (0xd)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldfld      0x04000001
                          IL_0006:  call       0x0A000006
                          IL_000b:  nop
                          IL_000c:  ret
                        }
                        """);
                    })
                .AddGeneration(
                    // 2: remove closure
                    source: """
                    using System;
                    class C
                    {
                        public void F()
                        <N:0>{
                            int <N:1>x = 1</N:1>;
                        }</N:0>
                    }
                    """,
                    edits:
                    [
                        Edit(SemanticEditKind.Update, c => c.GetMember("C.F"), preserveLocalVariables: true),
                    ],
                    validator: g =>
                    {
                        g.VerifySynthesizedMembers(
                            "System.Runtime.CompilerServices.HotReloadException",
                            "C: {<F>g__L|0#1_0#1, <>c__DisplayClass0#1_0#1}",
                            "C.<>c__DisplayClass0#1_0#1: {x}");

                        g.VerifyMethodDefNames("F", "<F>g__L|0#1_0#1", ".ctor");

                        g.VerifyIL("""
                        {
                          // Code size        4 (0x4)
                          .maxstack  1
                          IL_0000:  nop
                          IL_0001:  ldc.i4.1
                          IL_0002:  stloc.2
                          IL_0003:  ret
                        }
                        {
                          // Code size       12 (0xc)
                          .maxstack  8
                          IL_0000:  ldstr      0x70000009
                          IL_0005:  ldc.i4.m1
                          IL_0006:  newobj     0x06000004
                          IL_000b:  throw
                        }
                        {
                          // Code size       16 (0x10)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldarg.1
                          IL_0002:  call       0x0A000008
                          IL_0007:  nop
                          IL_0008:  ldarg.0
                          IL_0009:  ldarg.2
                          IL_000a:  stfld      0x04000002
                          IL_000f:  ret
                        }
                        """);
                    })
                .Verify();
        }

        [Fact]
        public void ChainClosure_Lambda()
        {
            using var _ = new EditAndContinueTest()
                .AddBaseline(
                    source: """
                    using System;

                    class C
                    {
                        void G(Func<int, int> f) {}

                        void F()
                        {
                            <N:0>{ int <N:1>x0 = 0</N:1>;      // Closure 0
                                <N:2>{ int <N:3>x1 = 0</N:3>;  // Closure 1

                                    G(<N:4>a => x0</N:4>);
                                    G(<N:5>a => x1</N:5>);
                                }</N:2>
                            }</N:0>
                        }
                    }
                    """,
                    validator: g =>
                    {
                        g.VerifyCustomDebugInformation("C.F", """
                             <symbols>
                              <methods>
                                <method containingType="C" name="F">
                                  <customDebugInfo>
                                    <forward declaringType="C" methodName="G" parameterNames="f" />
                                    <encLocalSlotMap>
                                      <slot kind="30" offset="16" />
                                      <slot kind="30" offset="77" />
                                    </encLocalSlotMap>
                                    <encLambdaMap>
                                      <methodOrdinal>1</methodOrdinal>
                                      <closure offset="16" />
                                      <closure offset="77" />
                                      <lambda offset="147" closure="0" />
                                      <lambda offset="187" closure="1" />
                                    </encLambdaMap>
                                  </customDebugInfo>
                                </method>
                              </methods>
                            </symbols>
                            """);
                    })
                .AddGeneration(
                    source: """
                    using System;
                    
                    class C
                    {
                        void G(Func<int, int> f) {}
                    
                        void F()
                        {
                            <N:0>{ int <N:1>x0 = 0</N:1>;      // Closure 0
                                <N:2>{ int <N:3>x1 = 0</N:3>;  // Closure 1 -> Closure 0
                    
                                    G(<N:4>a => x0</N:4>);
                                    G(<N:5>a => x0 + x1</N:5>);
                                }</N:2>
                            }</N:0>
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
                            "System.Runtime.CompilerServices.HotReloadException",
                            "C: {<>c__DisplayClass1_0, <>c__DisplayClass1_1#1}",
                            "C.<>c__DisplayClass1_1#1: {x1, CS$<>8__locals1, <F>b__1#1}",
                            "C.<>c__DisplayClass1_0: {x0, <F>b__0}");

                        g.VerifyMethodDefNames("F", "<F>b__0", "<F>b__1", ".ctor", ".ctor", "<F>b__1#1");

                        g.VerifyIL(
                        """
                        {
                          // Code size       82 (0x52)
                          .maxstack  3
                          IL_0000:  nop
                          IL_0001:  newobj     0x06000004
                          IL_0006:  stloc.0
                          IL_0007:  nop
                          IL_0008:  ldloc.0
                          IL_0009:  ldc.i4.0
                          IL_000a:  stfld      0x04000001
                          IL_000f:  newobj     0x06000009
                          IL_0014:  stloc.2
                          IL_0015:  ldloc.2
                          IL_0016:  ldloc.0
                          IL_0017:  stfld      0x04000005
                          IL_001c:  nop
                          IL_001d:  ldloc.2
                          IL_001e:  ldc.i4.0
                          IL_001f:  stfld      0x04000004
                          IL_0024:  ldarg.0
                          IL_0025:  ldloc.2
                          IL_0026:  ldfld      0x04000005
                          IL_002b:  ldftn      0x06000005
                          IL_0031:  newobj     0x0A000008
                          IL_0036:  call       0x06000001
                          IL_003b:  nop
                          IL_003c:  ldarg.0
                          IL_003d:  ldloc.2
                          IL_003e:  ldftn      0x0600000A
                          IL_0044:  newobj     0x0A000008
                          IL_0049:  call       0x06000001
                          IL_004e:  nop
                          IL_004f:  nop
                          IL_0050:  nop
                          IL_0051:  ret
                        }
                        {
                          // Code size        7 (0x7)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldfld      0x04000001
                          IL_0006:  ret
                        }
                        {
                          // Code size       12 (0xc)
                          .maxstack  8
                          IL_0000:  ldstr      0x70000005
                          IL_0005:  ldc.i4.m1
                          IL_0006:  newobj     0x06000008
                          IL_000b:  throw
                        }
                        {
                          // Code size       16 (0x10)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldarg.1
                          IL_0002:  call       0x0A000009
                          IL_0007:  nop
                          IL_0008:  ldarg.0
                          IL_0009:  ldarg.2
                          IL_000a:  stfld      0x04000003
                          IL_000f:  ret
                        }
                        {
                          // Code size        8 (0x8)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  call       0x0A00000A
                          IL_0006:  nop
                          IL_0007:  ret
                        }
                        {
                          // Code size       19 (0x13)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldfld      0x04000005
                          IL_0006:  ldfld      0x04000001
                          IL_000b:  ldarg.0
                          IL_000c:  ldfld      0x04000004
                          IL_0011:  add
                          IL_0012:  ret
                        }
                        """);
                    })
                .Verify();
        }

        [Fact]
        public void ChainClosure_LocalFunction()
        {
            using var _ = new EditAndContinueTest()
                .AddBaseline(
                    source: """
                    using System;

                    class C
                    {
                        void F()
                        {
                            <N:0>{ int <N:1>x0 = 0</N:1>;      // Closure 0
                                <N:2>{ int <N:3>x1 = 0</N:3>;  // Closure 1

                                    <N:4>int L1() => x0;</N:4>
                                    <N:5>int L2() => x1;</N:5>
                                }</N:2>
                            }</N:0>
                        }
                    }
                    """,
                    validator: g =>
                    {
                        g.VerifyCustomDebugInformation("C.F", """
                            <symbols>
                              <methods>
                                <method containingType="C" name="F">
                                  <customDebugInfo>
                                    <using>
                                      <namespace usingCount="1" />
                                    </using>
                                    <encLocalSlotMap>
                                      <slot kind="30" offset="16" />
                                      <slot kind="30" offset="77" />
                                    </encLocalSlotMap>
                                    <encLambdaMap>
                                      <methodOrdinal>0</methodOrdinal>
                                      <closure offset="16" />
                                      <closure offset="77" />
                                      <lambda offset="152" />
                                      <lambda offset="196" />
                                    </encLambdaMap>
                                  </customDebugInfo>
                                </method>
                              </methods>
                            </symbols>
                            """);

                        g.VerifyMethodBody("C.<F>g__L1|0_0(ref C.<>c__DisplayClass0_0)", """
                            {
                              // Code size        7 (0x7)
                              .maxstack  1
                              // sequence point: x0
                              IL_0000:  ldarg.0
                              IL_0001:  ldfld      "int C.<>c__DisplayClass0_0.x0"
                              IL_0006:  ret
                            }
                            """);

                        g.VerifyMethodBody("C.<F>g__L2|0_1(ref C.<>c__DisplayClass0_1)", """
                            {
                              // Code size        7 (0x7)
                              .maxstack  1
                              // sequence point: x1
                              IL_0000:  ldarg.0
                              IL_0001:  ldfld      "int C.<>c__DisplayClass0_1.x1"
                              IL_0006:  ret
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
                            <N:0>{ int <N:1>x0 = 0</N:1>;      // Closure 0
                                <N:2>{ int <N:3>x1 = 0</N:3>;  // Closure 1 -> Closure 0
                    
                                    <N:4>int L1() => x0;</N:4>
                                    <N:5>int L2() => x0 + x1;</N:5>
                                }</N:2>
                            }</N:0>
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
                            "System.Runtime.CompilerServices.HotReloadException",
                            "C: {<F>g__L1|0_0, <F>g__L2|0_1#1, <>c__DisplayClass0_0, <>c__DisplayClass0_1}",
                            "C.<>c__DisplayClass0_0: {x0}",
                            "C.<>c__DisplayClass0_1: {x1}");

                        g.VerifyMethodDefNames("F", "<F>g__L1|0_0", "<F>g__L2|0_1", "<F>g__L2|0_1#1", ".ctor");

                        g.VerifyIL(
                        """
                        {
                          // Code size       24 (0x18)
                          .maxstack  2
                          IL_0000:  nop
                          IL_0001:  nop
                          IL_0002:  ldloca.s   V_0
                          IL_0004:  ldc.i4.0
                          IL_0005:  stfld      0x04000001
                          IL_000a:  nop
                          IL_000b:  ldloca.s   V_1
                          IL_000d:  ldc.i4.0
                          IL_000e:  stfld      0x04000002
                          IL_0013:  nop
                          IL_0014:  nop
                          IL_0015:  nop
                          IL_0016:  nop
                          IL_0017:  ret
                        }
                        {
                          // Code size        7 (0x7)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldfld      0x04000001
                          IL_0006:  ret
                        }
                        {
                          // Code size       12 (0xc)
                          .maxstack  8
                          IL_0000:  ldstr      0x70000005
                          IL_0005:  ldc.i4.m1
                          IL_0006:  newobj     0x06000006
                          IL_000b:  throw
                        }
                        {
                          // Code size       14 (0xe)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldfld      0x04000001
                          IL_0006:  ldarg.1
                          IL_0007:  ldfld      0x04000002
                          IL_000c:  add
                          IL_000d:  ret
                        }
                        {
                          // Code size       16 (0x10)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldarg.1
                          IL_0002:  call       0x0A000007
                          IL_0007:  nop
                          IL_0008:  ldarg.0
                          IL_0009:  ldarg.2
                          IL_000a:  stfld      0x04000003
                          IL_000f:  ret
                        }
                        """);
                    })
                .Verify();
        }

        [Fact]
        public void UnchainClosure_Lambda()
        {
            using var _ = new EditAndContinueTest()
                .AddBaseline(
                    source: """
                    using System;

                    class C
                    {
                        static void G(Func<int, int> f) {}

                        void F()
                        {
                            <N:0>{ int <N:1>x0 = 0</N:1>;      // Closure 0
                                <N:2>{ int <N:3>x1 = 0</N:3>;  // Closure 1 -> Closure 0

                                    G(<N:4>a => x0</N:4>);
                                    G(<N:5>a => x0 + x1</N:5>);
                                }</N:2>
                            }</N:0>
                        }
                    }
                    """,
                    validator: g =>
                    {
                        g.VerifyCustomDebugInformation("C.F", """
                            <symbols>
                              <methods>
                                <method containingType="C" name="F">
                                  <customDebugInfo>
                                    <forward declaringType="C" methodName="G" parameterNames="f" />
                                    <encLocalSlotMap>
                                      <slot kind="30" offset="16" />
                                      <slot kind="30" offset="77" />
                                    </encLocalSlotMap>
                                    <encLambdaMap>
                                      <methodOrdinal>1</methodOrdinal>
                                      <closure offset="16" />
                                      <closure offset="77" />
                                      <lambda offset="160" closure="0" />
                                      <lambda offset="200" closure="1" />
                                    </encLambdaMap>
                                  </customDebugInfo>
                                </method>
                              </methods>
                            </symbols>
                            """);
                    })
                .AddGeneration(
                    source: """
                    using System;

                    class C
                    {
                        static void G(Func<int, int> f) {}

                        void F()
                        {
                            <N:0>{ int <N:1>x0 = 0</N:1>;      // Closure 0
                                <N:2>{ int <N:3>x1 = 0</N:3>;  // Closure 1

                                    G(<N:4>a => x0</N:4>);
                                    G(<N:5>a => x1</N:5>);
                                }</N:2>
                            }</N:0>
                        }
                    }
                    """,
                    edits:
                    [
                        Edit(SemanticEditKind.Update, c => c.GetMember("C.F"), preserveLocalVariables: true),
                    ],
                    validator: g =>
                    {
                        // closure #0 is preserved, a new closure #1 is created as it has a different parent now:
                        g.VerifySynthesizedMembers(
                            "System.Runtime.CompilerServices.HotReloadException",
                            "C: {<>c__DisplayClass1_0, <>c__DisplayClass1_1#1}",
                            "C.<>c__DisplayClass1_0: {x0, <F>b__0}",
                            "C.<>c__DisplayClass1_1#1: {x1, <F>b__1#1}");

                        g.VerifyMethodDefNames("F", "<F>b__0", "<F>b__1", ".ctor", ".ctor", "<F>b__1#1");

                        g.VerifyIL(
                        """
                        {
                          // Code size       68 (0x44)
                          .maxstack  2
                          IL_0000:  nop
                          IL_0001:  newobj     0x06000004
                          IL_0006:  stloc.0
                          IL_0007:  nop
                          IL_0008:  ldloc.0
                          IL_0009:  ldc.i4.0
                          IL_000a:  stfld      0x04000001
                          IL_000f:  newobj     0x06000009
                          IL_0014:  stloc.2
                          IL_0015:  nop
                          IL_0016:  ldloc.2
                          IL_0017:  ldc.i4.0
                          IL_0018:  stfld      0x04000005
                          IL_001d:  ldloc.0
                          IL_001e:  ldftn      0x06000005
                          IL_0024:  newobj     0x0A000008
                          IL_0029:  call       0x06000001
                          IL_002e:  nop
                          IL_002f:  ldloc.2
                          IL_0030:  ldftn      0x0600000A
                          IL_0036:  newobj     0x0A000008
                          IL_003b:  call       0x06000001
                          IL_0040:  nop
                          IL_0041:  nop
                          IL_0042:  nop
                          IL_0043:  ret
                        }
                        {
                          // Code size        7 (0x7)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldfld      0x04000001
                          IL_0006:  ret
                        }
                        {
                          // Code size       12 (0xc)
                          .maxstack  8
                          IL_0000:  ldstr      0x70000005
                          IL_0005:  ldc.i4.m1
                          IL_0006:  newobj     0x06000008
                          IL_000b:  throw
                        }
                        {
                          // Code size       16 (0x10)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldarg.1
                          IL_0002:  call       0x0A000009
                          IL_0007:  nop
                          IL_0008:  ldarg.0
                          IL_0009:  ldarg.2
                          IL_000a:  stfld      0x04000004
                          IL_000f:  ret
                        }
                        {
                          // Code size        8 (0x8)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  call       0x0A00000A
                          IL_0006:  nop
                          IL_0007:  ret
                        }
                        {
                          // Code size        7 (0x7)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldfld      0x04000005
                          IL_0006:  ret
                        }
                        """);
                    })
                .Verify();
        }

        [Fact]
        public void UnchainClosure_LocalFunction()
        {
            using var _ = new EditAndContinueTest()
                .AddBaseline(
                    source: """
                    using System;
                    
                    class C
                    {
                        void F()                       
                        {                              
                            <N:0>{ int <N:1>x0 = 0</N:1>;      // Closure 0             
                                <N:2>{ int <N:3>x1 = 0</N:3>;  // Closure 1 -> Closure 0             
                    
                                    <N:4>int L1() => x0;</N:4>
                                    <N:5>int L2() => x0 + x1;</N:5>
                                }</N:2>
                            }</N:0>
                        }
                    }
                    """,
                    validator: g =>
                    {
                        g.VerifyCustomDebugInformation("C.F", """
                            <symbols>
                              <methods>
                                <method containingType="C" name="F">
                                  <customDebugInfo>
                                    <using>
                                      <namespace usingCount="1" />
                                    </using>
                                    <encLocalSlotMap>
                                      <slot kind="30" offset="46" />
                                      <slot kind="30" offset="120" />
                                    </encLocalSlotMap>
                                    <encLambdaMap>
                                      <methodOrdinal>0</methodOrdinal>
                                      <closure offset="46" />
                                      <closure offset="120" />
                                      <lambda offset="221" />
                                      <lambda offset="265" />
                                    </encLambdaMap>
                                  </customDebugInfo>
                                </method>
                              </methods>
                            </symbols>
                            """);

                        g.VerifyMethodBody("C.<F>g__L1|0_0(ref C.<>c__DisplayClass0_0)", """
                            {
                              // Code size        7 (0x7)
                              .maxstack  1
                              // sequence point: x0
                              IL_0000:  ldarg.0
                              IL_0001:  ldfld      "int C.<>c__DisplayClass0_0.x0"
                              IL_0006:  ret
                            }
                            """);

                        g.VerifyMethodBody("C.<F>g__L2|0_1(ref C.<>c__DisplayClass0_0, ref C.<>c__DisplayClass0_1)", """
                            {
                              // Code size       14 (0xe)
                              .maxstack  2
                              // sequence point: x0 + x1
                              IL_0000:  ldarg.0
                              IL_0001:  ldfld      "int C.<>c__DisplayClass0_0.x0"
                              IL_0006:  ldarg.1
                              IL_0007:  ldfld      "int C.<>c__DisplayClass0_1.x1"
                              IL_000c:  add
                              IL_000d:  ret
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
                            <N:0>{ int <N:1>x0 = 0</N:1>;      // Closure 0             
                                <N:2>{ int <N:3>x1 = 0</N:3>;  // Closure 1               
                    
                                    <N:4>int L1() => x0;</N:4>
                                    <N:5>int L2() => x1;</N:5>
                                }</N:2>
                            }</N:0>
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
                            "System.Runtime.CompilerServices.HotReloadException",
                            "C: {<F>g__L1|0_0, <F>g__L2|0_1#1, <>c__DisplayClass0_0, <>c__DisplayClass0_1}",
                            "C.<>c__DisplayClass0_1: {x1}",
                            "C.<>c__DisplayClass0_0: {x0}");

                        g.VerifyMethodDefNames("F", "<F>g__L1|0_0", "<F>g__L2|0_1", "<F>g__L2|0_1#1", ".ctor");

                        g.VerifyIL(
                        """
                        {
                          // Code size       24 (0x18)
                          .maxstack  2
                          IL_0000:  nop
                          IL_0001:  nop
                          IL_0002:  ldloca.s   V_0
                          IL_0004:  ldc.i4.0
                          IL_0005:  stfld      0x04000001
                          IL_000a:  nop
                          IL_000b:  ldloca.s   V_1
                          IL_000d:  ldc.i4.0
                          IL_000e:  stfld      0x04000002
                          IL_0013:  nop
                          IL_0014:  nop
                          IL_0015:  nop
                          IL_0016:  nop
                          IL_0017:  ret
                        }
                        {
                          // Code size        7 (0x7)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldfld      0x04000001
                          IL_0006:  ret
                        }
                        {
                          // Code size       12 (0xc)
                          .maxstack  8
                          IL_0000:  ldstr      0x70000005
                          IL_0005:  ldc.i4.m1
                          IL_0006:  newobj     0x06000006
                          IL_000b:  throw
                        }
                        {
                          // Code size        7 (0x7)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldfld      0x04000002
                          IL_0006:  ret
                        }
                        {
                          // Code size       16 (0x10)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldarg.1
                          IL_0002:  call       0x0A000007
                          IL_0007:  nop
                          IL_0008:  ldarg.0
                          IL_0009:  ldarg.2
                          IL_000a:  stfld      0x04000003
                          IL_000f:  ret
                        }
                        """);
                    })
                .Verify();
        }

        [Fact]
        public void ChangeClosureParent_Lambda()
        {
            using var _ = new EditAndContinueTest()
                .AddBaseline(
                    source: """
                    using System;

                    class C
                    {
                        static void G(Func<int> f) {}

                        void F()
                        {
                            <N:0>{ int <N:1>x = 1</N:1>;
                                <N:2>{ int <N:3>y = 2</N:3>;
                                    <N:4>{ int <N:5>z = 3</N:5>;
                                        G(<N:6>() => x</N:6>);
                                        G(<N:7>() => z + x</N:7>);
                                    }</N:4>
                                }</N:2>
                            }</N:0>
                        }
                    }
                    """,
                    validator: g =>
                    {
                        g.VerifySynthesizedMembers(
                            "C: {<>c__DisplayClass1_0, <>c__DisplayClass1_1}",
                            "C.<>c__DisplayClass1_0: {x, <F>b__0}",
                            "C.<>c__DisplayClass1_1: {z, CS$<>8__locals1, <F>b__1}");
                    })
                .AddGeneration(
                    source: """
                    using System;
                    
                    class C
                    {
                        static void G(Func<int> f) {}
                    
                        void F()
                        {
                            <N:0>{ int <N:1>x = 1</N:1>;
                                <N:2>{ int <N:3>y = 2</N:3>;
                                    <N:4>{ int <N:5>z = 3</N:5>;
                                        G(<N:6>() => x</N:6>);
                                        G(<N:7>() => z + x</N:7>);
                                        G(() => z + x + y);
                                    }</N:4>
                                }</N:2>
                            }</N:0>
                        }
                    }
                    """,
                    edits:
                    [
                        Edit(SemanticEditKind.Update, c => c.GetMember("C.F"), preserveLocalVariables: true),
                    ],
                    validator: g =>
                    {
                        // closure #0 is preserved, new closures #1 and #2 are created:
                        g.VerifySynthesizedMembers(
                            "System.Runtime.CompilerServices.HotReloadException",
                            "C: {<>c__DisplayClass1_0, <>c__DisplayClass1_1#1, <>c__DisplayClass1_2#1}",
                            "C.<>c__DisplayClass1_0: {x, <F>b__0}",
                            "C.<>c__DisplayClass1_1#1: {y, CS$<>8__locals1}",
                            "C.<>c__DisplayClass1_2#1: {z, CS$<>8__locals2, <F>b__1#1, <F>b__2#1}");

                        g.VerifyMethodDefNames(
                            "F",
                            "<F>b__0",
                            "<F>b__1",
                            ".ctor",
                            ".ctor",
                            ".ctor",
                            "<F>b__1#1",
                            "<F>b__2#1");

                        g.VerifyIL("""
                        {
                          // Code size      131 (0x83)
                          .maxstack  2
                          IL_0000:  nop
                          IL_0001:  newobj     0x06000004
                          IL_0006:  stloc.0
                          IL_0007:  nop
                          IL_0008:  ldloc.0
                          IL_0009:  ldc.i4.1
                          IL_000a:  stfld      0x04000001
                          IL_000f:  newobj     0x06000009
                          IL_0014:  stloc.3
                          IL_0015:  ldloc.3
                          IL_0016:  ldloc.0
                          IL_0017:  stfld      0x04000006
                          IL_001c:  nop
                          IL_001d:  ldloc.3
                          IL_001e:  ldc.i4.2
                          IL_001f:  stfld      0x04000005
                          IL_0024:  newobj     0x0600000A
                          IL_0029:  stloc.s    V_4
                          IL_002b:  ldloc.s    V_4
                          IL_002d:  ldloc.3
                          IL_002e:  stfld      0x04000008
                          IL_0033:  nop
                          IL_0034:  ldloc.s    V_4
                          IL_0036:  ldc.i4.3
                          IL_0037:  stfld      0x04000007
                          IL_003c:  ldloc.s    V_4
                          IL_003e:  ldfld      0x04000008
                          IL_0043:  ldfld      0x04000006
                          IL_0048:  ldftn      0x06000005
                          IL_004e:  newobj     0x0A000008
                          IL_0053:  call       0x06000001
                          IL_0058:  nop
                          IL_0059:  ldloc.s    V_4
                          IL_005b:  ldftn      0x0600000B
                          IL_0061:  newobj     0x0A000008
                          IL_0066:  call       0x06000001
                          IL_006b:  nop
                          IL_006c:  ldloc.s    V_4
                          IL_006e:  ldftn      0x0600000C
                          IL_0074:  newobj     0x0A000008
                          IL_0079:  call       0x06000001
                          IL_007e:  nop
                          IL_007f:  nop
                          IL_0080:  nop
                          IL_0081:  nop
                          IL_0082:  ret
                        }
                        {
                          // Code size        7 (0x7)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldfld      0x04000001
                          IL_0006:  ret
                        }
                        {
                          // Code size       12 (0xc)
                          .maxstack  8
                          IL_0000:  ldstr      0x70000005
                          IL_0005:  ldc.i4.m1
                          IL_0006:  newobj     0x06000008
                          IL_000b:  throw
                        }
                        {
                          // Code size       16 (0x10)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldarg.1
                          IL_0002:  call       0x0A000009
                          IL_0007:  nop
                          IL_0008:  ldarg.0
                          IL_0009:  ldarg.2
                          IL_000a:  stfld      0x04000004
                          IL_000f:  ret
                        }
                        {
                          // Code size        8 (0x8)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  call       0x0A00000A
                          IL_0006:  nop
                          IL_0007:  ret
                        }
                        {
                          // Code size       24 (0x18)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldfld      0x04000007
                          IL_0006:  ldarg.0
                          IL_0007:  ldfld      0x04000008
                          IL_000c:  ldfld      0x04000006
                          IL_0011:  ldfld      0x04000001
                          IL_0016:  add
                          IL_0017:  ret
                        }
                        {
                          // Code size       36 (0x24)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldfld      0x04000007
                          IL_0006:  ldarg.0
                          IL_0007:  ldfld      0x04000008
                          IL_000c:  ldfld      0x04000006
                          IL_0011:  ldfld      0x04000001
                          IL_0016:  add
                          IL_0017:  ldarg.0
                          IL_0018:  ldfld      0x04000008
                          IL_001d:  ldfld      0x04000005
                          IL_0022:  add
                          IL_0023:  ret
                        }
                        """);
                    })
                .Verify();
        }

        [Fact]
        public void ChangeClosureParent_LocalFunction()
        {
            using var _ = new EditAndContinueTest()
                .AddBaseline(
                    source: """
                    using System;

                    class C
                    {
                        void F()
                        {
                            <N:0>{ int <N:1>x = 1</N:1>;
                                <N:2>{ int <N:3>y = 2</N:3>;
                                    <N:4>{ int <N:5>z = 3</N:5>;
                                        <N:6>int L1() => x;</N:6>
                                        <N:7>int L2() => z + x;</N:7>
                                    }</N:4>
                                }</N:2>
                            }</N:0>
                        }
                    }
                    """,
                    validator: g =>
                    {
                        g.VerifyCustomDebugInformation("C.F", """
                            <symbols>
                              <methods>
                                <method containingType="C" name="F">
                                  <customDebugInfo>
                                    <using>
                                      <namespace usingCount="1" />
                                    </using>
                                    <encLocalSlotMap>
                                      <slot kind="30" offset="16" />
                                      <slot kind="0" offset="69" />
                                      <slot kind="30" offset="104" />
                                    </encLocalSlotMap>
                                    <encLambdaMap>
                                      <methodOrdinal>0</methodOrdinal>
                                      <closure offset="16" />
                                      <closure offset="104" />
                                      <lambda offset="166" />
                                      <lambda offset="213" />
                                    </encLambdaMap>
                                  </customDebugInfo>
                                </method>
                              </methods>
                            </symbols>
                            """);

                        g.VerifyMethodBody("C.<F>g__L1|0_0(ref C.<>c__DisplayClass0_0)", """
                            {
                              // Code size        7 (0x7)
                              .maxstack  1
                              // sequence point: x
                              IL_0000:  ldarg.0
                              IL_0001:  ldfld      "int C.<>c__DisplayClass0_0.x"
                              IL_0006:  ret
                            }
                            """);

                        g.VerifyMethodBody("C.<F>g__L2|0_1(ref C.<>c__DisplayClass0_0, ref C.<>c__DisplayClass0_1)", """
                            {
                              // Code size       14 (0xe)
                              .maxstack  2
                              // sequence point: z + x
                              IL_0000:  ldarg.1
                              IL_0001:  ldfld      "int C.<>c__DisplayClass0_1.z"
                              IL_0006:  ldarg.0
                              IL_0007:  ldfld      "int C.<>c__DisplayClass0_0.x"
                              IL_000c:  add
                              IL_000d:  ret
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
                            <N:0>{ int <N:1>x = 1</N:1>;
                                <N:2>{ int <N:3>y = 2</N:3>;
                                    <N:4>{ int <N:5>z = 3</N:5>;
                                        <N:6>int L1() => x;</N:6>
                                        <N:7>int L2() => z + x;</N:7>
                                        int L3() => z + x + y;
                                    }</N:4>
                                }</N:2>
                            }</N:0>
                        }
                    }
                    """,
                    edits:
                    [
                        Edit(SemanticEditKind.Update, c => c.GetMember("C.F"), preserveLocalVariables: true),
                    ],
                    validator: g =>
                    {
                        // closures 0 and 1 are preserved, a new closure is created:
                        g.VerifySynthesizedMembers(
                            "C: {<F>g__L1|0_0, <F>g__L2|0_1, <F>g__L3|0_2#1, <>c__DisplayClass0_0, <>c__DisplayClass0_1, <>c__DisplayClass0_1#1}",
                            "C.<>c__DisplayClass0_0: {x}",
                            "C.<>c__DisplayClass0_1: {z}",
                            "C.<>c__DisplayClass0_1#1: {y}");

                        g.VerifyMethodDefNames("F", "<F>g__L1|0_0", "<F>g__L2|0_1", "<F>g__L3|0_2#1");

                        g.VerifyIL("""
                        {
                          // Code size       35 (0x23)
                          .maxstack  2
                          IL_0000:  nop
                          IL_0001:  nop
                          IL_0002:  ldloca.s   V_0
                          IL_0004:  ldc.i4.1
                          IL_0005:  stfld      0x04000001
                          IL_000a:  nop
                          IL_000b:  ldloca.s   V_3
                          IL_000d:  ldc.i4.2
                          IL_000e:  stfld      0x04000003
                          IL_0013:  nop
                          IL_0014:  ldloca.s   V_2
                          IL_0016:  ldc.i4.3
                          IL_0017:  stfld      0x04000002
                          IL_001c:  nop
                          IL_001d:  nop
                          IL_001e:  nop
                          IL_001f:  nop
                          IL_0020:  nop
                          IL_0021:  nop
                          IL_0022:  ret
                        }
                        {
                          // Code size        7 (0x7)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldfld      0x04000001
                          IL_0006:  ret
                        }
                        {
                          // Code size       14 (0xe)
                          .maxstack  8
                          IL_0000:  ldarg.1
                          IL_0001:  ldfld      0x04000002
                          IL_0006:  ldarg.0
                          IL_0007:  ldfld      0x04000001
                          IL_000c:  add
                          IL_000d:  ret
                        }
                        {
                          // Code size       21 (0x15)
                          .maxstack  8
                          IL_0000:  ldarg.2
                          IL_0001:  ldfld      0x04000002
                          IL_0006:  ldarg.0
                          IL_0007:  ldfld      0x04000001
                          IL_000c:  add
                          IL_000d:  ldarg.1
                          IL_000e:  ldfld      0x04000003
                          IL_0013:  add
                          IL_0014:  ret
                        }
                        """);
                    })
                .Verify();
        }

        [Fact]
        public void ChangeLambdaParent_Lambda()
        {
            using var _ = new EditAndContinueTest()
                .AddBaseline(
                    source: """
                    using System;
                    class C
                    {
                        static void G(Func<int> f) {}

                        public void F()
                        <N:0>{ int <N:1>x = 1</N:1>;
                            <N:2>{ int <N:3>y = 2</N:3>;
                                   G(<N:4>() => x</N:4>);
                                   G(<N:5>() => y</N:5>);

                                   G(<N:6>() => x + 1</N:6>);
                            }</N:2>
                        }</N:0>
                    }
                    """,
                    validator: g =>
                    {
                        g.VerifyCustomDebugInformation("C.F", """
                            <symbols>
                              <methods>
                                <method containingType="C" name="F">
                                  <customDebugInfo>
                                    <forward declaringType="C" methodName="G" parameterNames="f" />
                                    <encLocalSlotMap>
                                      <slot kind="30" offset="0" />
                                      <slot kind="30" offset="38" />
                                    </encLocalSlotMap>
                                    <encLambdaMap>
                                      <methodOrdinal>1</methodOrdinal>
                                      <closure offset="0" />
                                      <closure offset="38" />
                                      <lambda offset="91" closure="0" />
                                      <lambda offset="130" closure="1" />
                                      <lambda offset="171" closure="0" />
                                    </encLambdaMap>
                                  </customDebugInfo>
                                </method>
                              </methods>
                            </symbols>
                            """);

                        g.VerifyMethodBody("C.<>c__DisplayClass1_0.<F>b__0", """
                            {
                              // Code size        7 (0x7)
                              .maxstack  1
                              // sequence point: x
                              IL_0000:  ldarg.0
                              IL_0001:  ldfld      "int C.<>c__DisplayClass1_0.x"
                              IL_0006:  ret
                            }
                            """);

                        g.VerifyMethodBody("C.<>c__DisplayClass1_1.<F>b__1", """
                            {
                              // Code size        7 (0x7)
                              .maxstack  1
                              // sequence point: y
                              IL_0000:  ldarg.0
                              IL_0001:  ldfld      "int C.<>c__DisplayClass1_1.y"
                              IL_0006:  ret
                            }
                            """);

                        g.VerifyMethodBody("C.<>c__DisplayClass1_0.<F>b__2", """
                            {
                              // Code size        9 (0x9)
                              .maxstack  2
                              // sequence point: x + 1
                              IL_0000:  ldarg.0
                              IL_0001:  ldfld      "int C.<>c__DisplayClass1_0.x"
                              IL_0006:  ldc.i4.1
                              IL_0007:  add
                              IL_0008:  ret
                            }
                            """);
                    })

                .AddGeneration(
                    source: """
                    using System;
                    class C
                    {
                        static void G(Func<int> f) {}

                        public void F()
                        <N:0>{ int <N:1>x = 1</N:1>;
                            <N:2>{ int <N:3>y = 2</N:3>;
                                   G(<N:4>() => x</N:4>);
                                   G(<N:5>() => y</N:5>);
                    
                                   G(<N:6>() => y + 1</N:6>);
                            }</N:2>
                        }</N:0>
                    }
                    """,
                    edits:
                    [
                        Edit(SemanticEditKind.Update, c => c.GetMember("C.F"), preserveLocalVariables: true),
                    ],
                    validator: g =>
                    {
                        // Lambda moved from closure 0 to 1:
                        g.VerifySynthesizedMembers(
                            "System.Runtime.CompilerServices.HotReloadException",
                            "C: {<>c__DisplayClass1_0, <>c__DisplayClass1_1}",
                            "C.<>c__DisplayClass1_0: {x, <F>b__0}",
                            "C.<>c__DisplayClass1_1: {y, <F>b__1, <F>b__2#1}");

                        g.VerifyMethodDefNames("F", "<F>b__0", "<F>b__2", "<F>b__1", ".ctor", "<F>b__2#1");

                        g.VerifyIL(
                        """
                        {
                          // Code size       84 (0x54)
                          .maxstack  2
                          IL_0000:  newobj     0x06000004
                          IL_0005:  stloc.0
                          IL_0006:  nop
                          IL_0007:  ldloc.0
                          IL_0008:  ldc.i4.1
                          IL_0009:  stfld      0x04000001
                          IL_000e:  newobj     0x06000007
                          IL_0013:  stloc.1
                          IL_0014:  nop
                          IL_0015:  ldloc.1
                          IL_0016:  ldc.i4.2
                          IL_0017:  stfld      0x04000002
                          IL_001c:  ldloc.0
                          IL_001d:  ldftn      0x06000005
                          IL_0023:  newobj     0x0A000008
                          IL_0028:  call       0x06000001
                          IL_002d:  nop
                          IL_002e:  ldloc.1
                          IL_002f:  ldftn      0x06000008
                          IL_0035:  newobj     0x0A000008
                          IL_003a:  call       0x06000001
                          IL_003f:  nop
                          IL_0040:  ldloc.1
                          IL_0041:  ldftn      0x0600000A
                          IL_0047:  newobj     0x0A000008
                          IL_004c:  call       0x06000001
                          IL_0051:  nop
                          IL_0052:  nop
                          IL_0053:  ret
                        }
                        {
                          // Code size        7 (0x7)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldfld      0x04000001
                          IL_0006:  ret
                        }
                        {
                          // Code size       12 (0xc)
                          .maxstack  8
                          IL_0000:  ldstr      0x70000005
                          IL_0005:  ldc.i4.m1
                          IL_0006:  newobj     0x06000009
                          IL_000b:  throw
                        }
                        {
                          // Code size        7 (0x7)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldfld      0x04000002
                          IL_0006:  ret
                        }
                        {
                          // Code size       16 (0x10)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldarg.1
                          IL_0002:  call       0x0A000009
                          IL_0007:  nop
                          IL_0008:  ldarg.0
                          IL_0009:  ldarg.2
                          IL_000a:  stfld      0x04000003
                          IL_000f:  ret
                        }
                        {
                          // Code size        9 (0x9)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldfld      0x04000002
                          IL_0006:  ldc.i4.1
                          IL_0007:  add
                          IL_0008:  ret
                        }
                        """);
                    })
                .Verify();
        }

        [Fact]
        public void ChangeLambdaParent_LocalFunction()
        {
            using var _ = new EditAndContinueTest()
                .AddBaseline(
                    source: """
                    using System;
                    class C
                    {
                        public void F()
                        <N:0>{ int <N:1>x = 1</N:1>;
                            <N:2>{ int <N:3>y = 2</N:3>;
                                <N:4>int L1() => x;</N:4>
                                <N:5>int L2() => y;</N:5>

                                <N:6>int L3() => x + 1;</N:6>
                            }</N:2>
                        }</N:0>
                    }
                    """,
                    validator: g =>
                    {
                        g.VerifyCustomDebugInformation("C.F", """
                            <symbols>
                              <methods>
                                <method containingType="C" name="F">
                                  <customDebugInfo>
                                    <using>
                                      <namespace usingCount="1" />
                                    </using>
                                    <encLocalSlotMap>
                                      <slot kind="30" offset="0" />
                                      <slot kind="30" offset="38" />
                                    </encLocalSlotMap>
                                    <encLambdaMap>
                                      <methodOrdinal>0</methodOrdinal>
                                      <closure offset="0" />
                                      <closure offset="38" />
                                      <lambda offset="92" />
                                      <lambda offset="131" />
                                      <lambda offset="172" />
                                    </encLambdaMap>
                                  </customDebugInfo>
                                </method>
                              </methods>
                            </symbols>
                            """);

                        g.VerifyMethodBody("C.<F>g__L1|0_0(ref C.<>c__DisplayClass0_0)", """
                            {
                              // Code size        7 (0x7)
                              .maxstack  1
                              // sequence point: x
                              IL_0000:  ldarg.0
                              IL_0001:  ldfld      "int C.<>c__DisplayClass0_0.x"
                              IL_0006:  ret
                            }
                            """);

                        g.VerifyMethodBody("C.<F>g__L2|0_1(ref C.<>c__DisplayClass0_1)", """
                            {
                              // Code size        7 (0x7)
                              .maxstack  1
                              // sequence point: y
                              IL_0000:  ldarg.0
                              IL_0001:  ldfld      "int C.<>c__DisplayClass0_1.y"
                              IL_0006:  ret
                            }
                            """);

                        g.VerifyMethodBody("C.<F>g__L3|0_2(ref C.<>c__DisplayClass0_0)", """
                            {
                              // Code size        9 (0x9)
                              .maxstack  2
                              // sequence point: x + 1
                              IL_0000:  ldarg.0
                              IL_0001:  ldfld      "int C.<>c__DisplayClass0_0.x"
                              IL_0006:  ldc.i4.1
                              IL_0007:  add
                              IL_0008:  ret
                            }
                            """);
                    })

                .AddGeneration(
                    source: """
                    using System;
                    class C
                    {
                        public void F()
                        <N:0>{ int <N:1>x = 1</N:1>;
                            <N:2>{ int <N:3>y = 2</N:3>;
                                <N:4>int L1() => x;</N:4>
                                <N:5>int L2() => y;</N:5>
                    
                                <N:6>int L3() => y + 1;</N:6>
                            }</N:2>
                        }</N:0>
                    }
                    """,
                    edits:
                    [
                        Edit(SemanticEditKind.Update, c => c.GetMember("C.F"), preserveLocalVariables: true),
                    ],
                    validator: g =>
                    {
                        // Lambda moved from closure 0 to 1:
                        g.VerifySynthesizedMembers(
                            "System.Runtime.CompilerServices.HotReloadException",
                            "C: {<F>g__L1|0_0, <F>g__L2|0_1, <F>g__L3|0_2#1, <>c__DisplayClass0_0, <>c__DisplayClass0_1}",
                            "C.<>c__DisplayClass0_0: {x}",
                            "C.<>c__DisplayClass0_1: {y}");

                        g.VerifyMethodDefNames("F", "<F>g__L1|0_0", "<F>g__L2|0_1", "<F>g__L3|0_2", "<F>g__L3|0_2#1", ".ctor");

                        g.VerifyIL(
                        """
                        {
                          // Code size       23 (0x17)
                          .maxstack  2
                          IL_0000:  nop
                          IL_0001:  ldloca.s   V_0
                          IL_0003:  ldc.i4.1
                          IL_0004:  stfld      0x04000001
                          IL_0009:  nop
                          IL_000a:  ldloca.s   V_1
                          IL_000c:  ldc.i4.2
                          IL_000d:  stfld      0x04000002
                          IL_0012:  nop
                          IL_0013:  nop
                          IL_0014:  nop
                          IL_0015:  nop
                          IL_0016:  ret
                        }
                        {
                          // Code size        7 (0x7)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldfld      0x04000001
                          IL_0006:  ret
                        }
                        {
                          // Code size        7 (0x7)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldfld      0x04000002
                          IL_0006:  ret
                        }
                        {
                          // Code size       12 (0xc)
                          .maxstack  8
                          IL_0000:  ldstr      0x70000005
                          IL_0005:  ldc.i4.m1
                          IL_0006:  newobj     0x06000007
                          IL_000b:  throw
                        }
                        {
                          // Code size        9 (0x9)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldfld      0x04000002
                          IL_0006:  ldc.i4.1
                          IL_0007:  add
                          IL_0008:  ret
                        }
                        {
                          // Code size       16 (0x10)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldarg.1
                          IL_0002:  call       0x0A000007
                          IL_0007:  nop
                          IL_0008:  ldarg.0
                          IL_0009:  ldarg.2
                          IL_000a:  stfld      0x04000003
                          IL_000f:  ret
                        }
                        """);
                    })
                .Verify();
        }

        [Fact]
        public void ChangeLambdaParent_LambdaAndLocalFunction_Lambda()
        {
            using var _ = new EditAndContinueTest()
                .AddBaseline(
                    source: """
                    using System;
                    class C
                    {
                        static void G(Func<int> f) {}
                    
                        public void F()
                        <N:0>{ int <N:1>x = 1</N:1>;
                            <N:2>{ int <N:3>y = 2</N:3>;
                                <N:4>int L1() => x;</N:4>
                                G(<N:5>() => y</N:5>);

                                G(<N:6>() => x + 1</N:6>);
                            }</N:2>
                        }</N:0>
                    }
                    """,
                    validator: g =>
                    {
                        g.VerifySynthesizedMembers(
                            "C: {<>c__DisplayClass1_0, <>c__DisplayClass1_1}",
                            "C.<>c__DisplayClass1_0: {x, <F>g__L1|0, <F>b__2}",
                            "C.<>c__DisplayClass1_1: {y, <F>b__1}");

                        g.VerifyMethodBody("C.<>c__DisplayClass1_0.<F>g__L1|0()", """
                            {
                              // Code size        7 (0x7)
                              .maxstack  1
                              // sequence point: x
                              IL_0000:  ldarg.0
                              IL_0001:  ldfld      "int C.<>c__DisplayClass1_0.x"
                              IL_0006:  ret
                            }
                            """);

                        g.VerifyMethodBody("C.<>c__DisplayClass1_1.<F>b__1()", """
                            {
                              // Code size        7 (0x7)
                              .maxstack  1
                              // sequence point: y
                              IL_0000:  ldarg.0
                              IL_0001:  ldfld      "int C.<>c__DisplayClass1_1.y"
                              IL_0006:  ret
                            }
                            """);

                        g.VerifyMethodBody("C.<>c__DisplayClass1_0.<F>b__2()", """
                            {
                              // Code size        9 (0x9)
                              .maxstack  2
                              // sequence point: x + 1
                              IL_0000:  ldarg.0
                              IL_0001:  ldfld      "int C.<>c__DisplayClass1_0.x"
                              IL_0006:  ldc.i4.1
                              IL_0007:  add
                              IL_0008:  ret
                            }
                            """);
                    })

                .AddGeneration(
                    source: """
                    using System;
                    class C
                    {
                        static void G(Func<int> f) {}

                        public void F()
                        <N:0>{ int <N:1>x = 1</N:1>;
                            <N:2>{ int <N:3>y = 2</N:3>;
                                <N:4>int L1() => x;</N:4>
                                G(<N:5>() => y</N:5>);
                    
                                G(<N:6>() => y + 1</N:6>);
                            }</N:2>
                        }</N:0>
                    }
                    """,
                    edits:
                    [
                        Edit(SemanticEditKind.Update, c => c.GetMember("C.F"), preserveLocalVariables: true),
                    ],
                    validator: g =>
                    {
                        g.VerifySynthesizedMembers(
                            "System.Runtime.CompilerServices.HotReloadException",
                            "C: {<F>g__L1|1_0#1, <>c__DisplayClass1_0#1, <>c__DisplayClass1_1}",
                            "C.<>c__DisplayClass1_0#1: {x}",
                            "C.<>c__DisplayClass1_1: {y, <F>b__1, <F>b__2#1}");

                        g.VerifyMethodDefNames("F", "<F>g__L1|0", "<F>b__2", "<F>b__1", "<F>g__L1|1_0#1", ".ctor", "<F>b__2#1");

                        g.VerifyIL(
                        """
                        {
                          // Code size       62 (0x3e)
                          .maxstack  2
                          IL_0000:  nop
                          IL_0001:  ldloca.s   V_2
                          IL_0003:  ldc.i4.1
                          IL_0004:  stfld      0x04000004
                          IL_0009:  newobj     0x06000007
                          IL_000e:  stloc.1
                          IL_000f:  nop
                          IL_0010:  ldloc.1
                          IL_0011:  ldc.i4.2
                          IL_0012:  stfld      0x04000002
                          IL_0017:  nop
                          IL_0018:  ldloc.1
                          IL_0019:  ldftn      0x06000008
                          IL_001f:  newobj     0x0A000008
                          IL_0024:  call       0x06000001
                          IL_0029:  nop
                          IL_002a:  ldloc.1
                          IL_002b:  ldftn      0x0600000B
                          IL_0031:  newobj     0x0A000008
                          IL_0036:  call       0x06000001
                          IL_003b:  nop
                          IL_003c:  nop
                          IL_003d:  ret
                        }
                        {
                          // Code size       13 (0xd)
                          .maxstack  8
                          IL_0000:  ldstr      0x70000005
                          IL_0005:  ldc.i4.s   -5
                          IL_0007:  newobj     0x0600000A
                          IL_000c:  throw
                        }
                        {
                          // Code size       12 (0xc)
                          .maxstack  8
                          IL_0000:  ldstr      0x700001F4
                          IL_0005:  ldc.i4.m1
                          IL_0006:  newobj     0x0600000A
                          IL_000b:  throw
                        }
                        {
                          // Code size        7 (0x7)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldfld      0x04000002
                          IL_0006:  ret
                        }
                        {
                          // Code size        7 (0x7)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldfld      0x04000004
                          IL_0006:  ret
                        }
                        {
                          // Code size       16 (0x10)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldarg.1
                          IL_0002:  call       0x0A000009
                          IL_0007:  nop
                          IL_0008:  ldarg.0
                          IL_0009:  ldarg.2
                          IL_000a:  stfld      0x04000003
                          IL_000f:  ret
                        }
                        {
                          // Code size        9 (0x9)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldfld      0x04000002
                          IL_0006:  ldc.i4.1
                          IL_0007:  add
                          IL_0008:  ret
                        }
                        """);
                    })
                .Verify();
        }

        [Fact]
        public void ChangeLambdaParent_LambdaAndLocalFunction_LocalFunction()
        {
            using var _ = new EditAndContinueTest()
                .AddBaseline(
                    source: """
                    using System;
                    class C
                    {
                        static void G(Func<int> f) {}
                    
                        public void F()
                        <N:0>{ int <N:1>x = 1</N:1>;
                            <N:2>{ int <N:3>y = 2</N:3>;
                                <N:4>int L1() => x;</N:4>
                                G(<N:5>() => y</N:5>);

                                <N:6>int L3() => x + 1;</N:6>
                            }</N:2>
                        }</N:0>
                    }
                    """,
                    validator: g =>
                    {
                        g.VerifyCustomDebugInformation("C.F", """
                            <symbols>
                              <methods>
                                <method containingType="C" name="F">
                                  <customDebugInfo>
                                    <forward declaringType="C" methodName="G" parameterNames="f" />
                                    <encLocalSlotMap>
                                      <slot kind="30" offset="0" />
                                      <slot kind="30" offset="38" />
                                    </encLocalSlotMap>
                                    <encLambdaMap>
                                      <methodOrdinal>1</methodOrdinal>
                                      <closure offset="0" />
                                      <closure offset="38" />
                                      <lambda offset="92" />
                                      <lambda offset="127" closure="1" />
                                      <lambda offset="169" />
                                    </encLambdaMap>
                                  </customDebugInfo>
                                </method>
                              </methods>
                            </symbols>
                            """);

                        g.VerifyMethodBody("C.<F>g__L1|1_0(ref C.<>c__DisplayClass1_0)", """
                            {
                              // Code size        7 (0x7)
                              .maxstack  1
                              // sequence point: x
                              IL_0000:  ldarg.0
                              IL_0001:  ldfld      "int C.<>c__DisplayClass1_0.x"
                              IL_0006:  ret
                            }
                            """);

                        g.VerifyMethodBody("C.<>c__DisplayClass1_1.<F>b__1()", """
                            {
                              // Code size        7 (0x7)
                              .maxstack  1
                              // sequence point: y
                              IL_0000:  ldarg.0
                              IL_0001:  ldfld      "int C.<>c__DisplayClass1_1.y"
                              IL_0006:  ret
                            }
                            """);

                        g.VerifyMethodBody("C.<F>g__L3|1_2(ref C.<>c__DisplayClass1_0)", """
                            {
                              // Code size        9 (0x9)
                              .maxstack  2
                              // sequence point: x + 1
                              IL_0000:  ldarg.0
                              IL_0001:  ldfld      "int C.<>c__DisplayClass1_0.x"
                              IL_0006:  ldc.i4.1
                              IL_0007:  add
                              IL_0008:  ret
                            }
                            """);
                    })

                .AddGeneration(
                    source: """
                    using System;
                    class C
                    {
                        static void G(Func<int> f) {}

                        public void F()
                        <N:0>{ int <N:1>x = 1</N:1>;
                            <N:2>{ int <N:3>y = 2</N:3>;
                                <N:4>int L1() => x;</N:4>
                                G(<N:5>() => y</N:5>);
                    
                                <N:6>int L3() => y + 1;</N:6>
                            }</N:2>
                        }</N:0>
                    }
                    """,
                    edits:
                    [
                        Edit(SemanticEditKind.Update, c => c.GetMember("C.F"), preserveLocalVariables: true),
                    ],
                    validator: g =>
                    {
                        // Lambda moved from closure 0 to 1:
                        g.VerifySynthesizedMembers(
                            "System.Runtime.CompilerServices.HotReloadException",
                            "C: {<F>g__L1|1_0, <>c__DisplayClass1_0, <>c__DisplayClass1_1}",
                            "C.<>c__DisplayClass1_0: {x}",
                            "C.<>c__DisplayClass1_1: {y, <F>b__1, <F>g__L3|2#1}");

                        g.VerifyMethodDefNames("F", "<F>g__L1|1_0", "<F>g__L3|1_2", "<F>b__1", ".ctor", "<F>g__L3|2#1");

                        g.VerifyIL("""
                        {
                          // Code size       45 (0x2d)
                          .maxstack  2
                          IL_0000:  nop
                          IL_0001:  ldloca.s   V_0
                          IL_0003:  ldc.i4.1
                          IL_0004:  stfld      0x04000001
                          IL_0009:  newobj     0x06000006
                          IL_000e:  stloc.1
                          IL_000f:  nop
                          IL_0010:  ldloc.1
                          IL_0011:  ldc.i4.2
                          IL_0012:  stfld      0x04000002
                          IL_0017:  nop
                          IL_0018:  ldloc.1
                          IL_0019:  ldftn      0x06000007
                          IL_001f:  newobj     0x0A000008
                          IL_0024:  call       0x06000001
                          IL_0029:  nop
                          IL_002a:  nop
                          IL_002b:  nop
                          IL_002c:  ret
                        }
                        {
                          // Code size        7 (0x7)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldfld      0x04000001
                          IL_0006:  ret
                        }
                        {
                          // Code size       12 (0xc)
                          .maxstack  8
                          IL_0000:  ldstr      0x70000005
                          IL_0005:  ldc.i4.m1
                          IL_0006:  newobj     0x06000008
                          IL_000b:  throw
                        }
                        {
                          // Code size        7 (0x7)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldfld      0x04000002
                          IL_0006:  ret
                        }
                        {
                          // Code size       16 (0x10)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldarg.1
                          IL_0002:  call       0x0A000009
                          IL_0007:  nop
                          IL_0008:  ldarg.0
                          IL_0009:  ldarg.2
                          IL_000a:  stfld      0x04000003
                          IL_000f:  ret
                        }
                        {
                          // Code size        9 (0x9)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldfld      0x04000002
                          IL_0006:  ldc.i4.1
                          IL_0007:  add
                          IL_0008:  ret
                        }
                        """);
                    })
                .Verify();
        }

        /// <summary>
        /// We allow to add a capture as long as the closure tree shape remains the same.
        /// The value of the captured variable might be uninitialized in the lambda.
        /// We leave it up to the user to set its value as needed.
        /// </summary>
        [Fact]
        public void UninitializedCapture_Lambda()
        {
            using var _ = new EditAndContinueTest()
                .AddBaseline(
                    source: """
                    using System;
                    class C
                    {
                        static void G(Func<int> f) {}

                        public void F()
                        <N:0>{
                            int <N:1>x = 1</N:1>;
                            int <N:2>y = 2</N:2>;
                            G(<N:3>() => x</N:3>);
                        }</N:0>
                    }
                    """,
                    validator: g =>
                    {
                        g.VerifyMethodBody("C.<>c__DisplayClass1_0.<F>b__0", """
                            {
                              // Code size        7 (0x7)
                              .maxstack  1
                              // sequence point: x
                              IL_0000:  ldarg.0
                              IL_0001:  ldfld      "int C.<>c__DisplayClass1_0.x"
                              IL_0006:  ret
                            }
                            """);
                    })

                .AddGeneration(
                    source: """
                    using System;
                    class C
                    {
                        static void G(Func<int> f) {}

                        public void F()
                        <N:0>{
                            int <N:1>x = 1</N:1>;
                            int <N:2>y = 2</N:2>;
                            G(<N:3>() => x + y</N:3>);
                        }</N:0>
                    }
                    """,
                    edits:
                    [
                        Edit(SemanticEditKind.Update, c => c.GetMember("C.F"), preserveLocalVariables: true),
                    ],
                    validator: g =>
                    {
                        g.VerifySynthesizedMembers(
                            "C: {<>c__DisplayClass1_0}",
                            "C.<>c__DisplayClass1_0: {x, y, <F>b__0}");

                        g.VerifyMethodDefNames("F", "<F>b__0");

                        g.VerifyIL(
                        """
                        {
                          // Code size       40 (0x28)
                          .maxstack  2
                          IL_0000:  newobj     0x06000004
                          IL_0005:  stloc.0
                          IL_0006:  nop
                          IL_0007:  ldloc.0
                          IL_0008:  ldc.i4.1
                          IL_0009:  stfld      0x04000001
                          IL_000e:  ldloc.0
                          IL_000f:  ldc.i4.2
                          IL_0010:  stfld      0x04000002
                          IL_0015:  ldloc.0
                          IL_0016:  ldftn      0x06000005
                          IL_001c:  newobj     0x0A000008
                          IL_0021:  call       0x06000001
                          IL_0026:  nop
                          IL_0027:  ret
                        }
                        {
                          // Code size       14 (0xe)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldfld      0x04000001
                          IL_0006:  ldarg.0
                          IL_0007:  ldfld      0x04000002
                          IL_000c:  add
                          IL_000d:  ret
                        }
                        """);
                    })
                .Verify();
        }

        [Fact]
        public void UninitializedCapture_LocalFunction()
        {
            using var _ = new EditAndContinueTest()
                .AddBaseline(
                    source: """
                    using System;
                    class C
                    {
                        public void F()
                        <N:0>{
                            int <N:1>x = 1</N:1>;
                            int <N:2>y = 2</N:2>;
                            <N:3>int L() => x;</N:3>
                        }</N:0>
                    }
                    """,
                    validator: g =>
                    {
                        g.VerifySynthesizedMembers(
                            "C: {<F>g__L|0_0, <>c__DisplayClass0_0}",
                            "C.<>c__DisplayClass0_0: {x}");

                        g.VerifyMethodBody("C.<F>g__L|0_0(ref C.<>c__DisplayClass0_0)", """
                             {
                              // Code size        7 (0x7)
                              .maxstack  1
                              // sequence point: x
                              IL_0000:  ldarg.0
                              IL_0001:  ldfld      "int C.<>c__DisplayClass0_0.x"
                              IL_0006:  ret
                            }
                            """);
                    })

                .AddGeneration(
                    source: """
                    using System;
                    class C
                    {
                        public void F()
                        <N:0>{
                            int <N:1>x = 1</N:1>;
                            int <N:2>y = 2</N:2>;
                            <N:3>int L() => x + y;</N:3>
                        }</N:0>
                    }
                    """,
                    edits:
                    [
                        Edit(SemanticEditKind.Update, c => c.GetMember("C.F"), preserveLocalVariables: true),
                    ],
                    validator: g =>
                    {
                        g.VerifySynthesizedMembers(
                            "System.Runtime.CompilerServices.HotReloadException",
                            "C: {<F>g__L|0_0#1, <>c__DisplayClass0_0#1}",
                            "C.<>c__DisplayClass0_0#1: {x, y}");

                        g.VerifyMethodDefNames("F", "<F>g__L|0_0", "<F>g__L|0_0#1", ".ctor");

                        g.VerifyIL("""
                        {
                          // Code size       19 (0x13)
                          .maxstack  2
                          IL_0000:  nop
                          IL_0001:  ldloca.s   V_2
                          IL_0003:  ldc.i4.1
                          IL_0004:  stfld      0x04000003
                          IL_0009:  ldloca.s   V_2
                          IL_000b:  ldc.i4.2
                          IL_000c:  stfld      0x04000004
                          IL_0011:  nop
                          IL_0012:  ret
                        }
                        {
                          // Code size       13 (0xd)
                          .maxstack  8
                          IL_0000:  ldstr      0x70000005
                          IL_0005:  ldc.i4.s   -5
                          IL_0007:  newobj     0x06000005
                          IL_000c:  throw
                        }
                        {
                          // Code size       14 (0xe)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldfld      0x04000003
                          IL_0006:  ldarg.0
                          IL_0007:  ldfld      0x04000004
                          IL_000c:  add
                          IL_000d:  ret
                        }
                        {
                          // Code size       16 (0x10)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldarg.1
                          IL_0002:  call       0x0A000007
                          IL_0007:  nop
                          IL_0008:  ldarg.0
                          IL_0009:  ldarg.2
                          IL_000a:  stfld      0x04000002
                          IL_000f:  ret
                        }
                        """);
                    })
                .Verify();
        }

        [Fact]
        public void CaptureOrdering()
        {
            using var _ = new EditAndContinueTest()
                .AddBaseline(
                    source: """
                    using System;
                    class C
                    {
                        static void G(Func<int> f) {}

                        public void F()
                        <N:0>{
                            int <N:1>x = 1</N:1>;
                            int <N:2>y = 2</N:2>;
                            G(<N:3>() => x + y</N:3>);
                        }</N:0>
                    }
                    """,
                    validator: g =>
                    {
                        g.VerifySynthesizedMembers(
                            "C: {<>c__DisplayClass1_0}",
                            "C.<>c__DisplayClass1_0: {x, y, <F>b__0}");

                        g.VerifyMethodBody("C.<>c__DisplayClass1_0.<F>b__0", """
                            {
                              // Code size       14 (0xe)
                              .maxstack  2
                              // sequence point: x + y
                              IL_0000:  ldarg.0
                              IL_0001:  ldfld      "int C.<>c__DisplayClass1_0.x"
                              IL_0006:  ldarg.0
                              IL_0007:  ldfld      "int C.<>c__DisplayClass1_0.y"
                              IL_000c:  add
                              IL_000d:  ret
                            }
                            """);
                    })

                .AddGeneration(
                    source: """
                    using System;
                    class C
                    {
                        static void G(Func<int> f) {}
                    
                        public void F()
                        <N:0>{
                            int <N:1>x = 1</N:1>;
                            int <N:2>y = 2</N:2>;
                            G(<N:3>() => y + x</N:3>);
                        }</N:0>
                    }
                    """,
                    edits:
                    [
                        Edit(SemanticEditKind.Update, c => c.GetMember("C.F"), preserveLocalVariables: true),
                    ],
                    validator: g =>
                    {
                        // Unlike local slots, the order is insignificant since the fields are referred to by name (MemberRef)
                        g.VerifySynthesizedMembers(
                            "C: {<>c__DisplayClass1_0}",
                            "C.<>c__DisplayClass1_0: {y, x, <F>b__0}");

                        g.VerifyMethodDefNames("F", "<F>b__0");

                        g.VerifyIL(
                        """
                        {
                          // Code size       40 (0x28)
                          .maxstack  2
                          IL_0000:  newobj     0x06000004
                          IL_0005:  stloc.0
                          IL_0006:  nop
                          IL_0007:  ldloc.0
                          IL_0008:  ldc.i4.1
                          IL_0009:  stfld      0x04000001
                          IL_000e:  ldloc.0
                          IL_000f:  ldc.i4.2
                          IL_0010:  stfld      0x04000002
                          IL_0015:  ldloc.0
                          IL_0016:  ldftn      0x06000005
                          IL_001c:  newobj     0x0A000008
                          IL_0021:  call       0x06000001
                          IL_0026:  nop
                          IL_0027:  ret
                        }
                        {
                          // Code size       14 (0xe)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldfld      0x04000002
                          IL_0006:  ldarg.0
                          IL_0007:  ldfld      0x04000001
                          IL_000c:  add
                          IL_000d:  ret
                        }
                        """);

                        g.VerifyEncLogDefinitions(
                        [
                            Row(2, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                            Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default)
                        ]);
                    })
                .Verify();
        }

        [Fact]
        public void Closure_ClassToStruct()
        {
            using var _ = new EditAndContinueTest()
                .AddBaseline(
                    source: """
                    using System;
                    class C
                    {
                        static void G(Func<int> f) {}

                        public void F()
                        <N:0>{</N:0>
                            int <N:1>x = 1</N:1>;
                            <N:2>int L() => x;</N:2>
                            G(<N:3>() => x</N:3>);
                        }
                    }
                    """,
                    validator: g =>
                    {
                        g.VerifySynthesizedMembers(displayTypeKind: true,
                        [
                            "class C: {<>c__DisplayClass1_0}",
                            "class C.<>c__DisplayClass1_0: {x, <F>g__L|0, <F>b__1}"
                        ]);
                    })

                .AddGeneration(
                    source: """
                    using System;
                    class C
                    {
                        static void G(Func<int> f) {}
                    
                        public void F()
                        <N:0>{</N:0>
                            int <N:1>x = 1</N:1>;
                            <N:2>int L() => x;</N:2>
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
                            "System.Runtime.CompilerServices.HotReloadException",
                            "struct C.<>c__DisplayClass1_0#1: {x}",
                            "class C: {<F>g__L|1_0#1, <>c__DisplayClass1_0#1}"
                        ]);

                        g.VerifyMethodDefNames("F", "<F>g__L|0", "<F>b__1", "<F>g__L|1_0#1", ".ctor");

                        g.VerifyIL(
                        """
                        {
                          // Code size       11 (0xb)
                          .maxstack  2
                          IL_0000:  nop
                          IL_0001:  ldloca.s   V_1
                          IL_0003:  ldc.i4.1
                          IL_0004:  stfld      0x04000003
                          IL_0009:  nop
                          IL_000a:  ret
                        }
                        {
                          // Code size       13 (0xd)
                          .maxstack  8
                          IL_0000:  ldstr      0x70000005
                          IL_0005:  ldc.i4.s   -5
                          IL_0007:  newobj     0x06000008
                          IL_000c:  throw
                        }
                        {
                          // Code size       12 (0xc)
                          .maxstack  8
                          IL_0000:  ldstr      0x700001F4
                          IL_0005:  ldc.i4.m1
                          IL_0006:  newobj     0x06000008
                          IL_000b:  throw
                        }
                        {
                          // Code size        7 (0x7)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldfld      0x04000003
                          IL_0006:  ret
                        }
                        {
                          // Code size       16 (0x10)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldarg.1
                          IL_0002:  call       0x0A000008
                          IL_0007:  nop
                          IL_0008:  ldarg.0
                          IL_0009:  ldarg.2
                          IL_000a:  stfld      0x04000002
                          IL_000f:  ret
                        }
                        """);

                        g.VerifyEncLogDefinitions(
                        [
                            Row(2, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                            Row(4, TableIndex.TypeDef, EditAndContinueOperation.Default),
                            Row(5, TableIndex.TypeDef, EditAndContinueOperation.Default),
                            Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(2, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(5, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(3, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(6, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(2, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                            Row(7, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                            Row(8, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(5, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(6, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(7, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(2, TableIndex.NestedClass, EditAndContinueOperation.Default)
                        ]);
                    })
                .Verify();
        }

        [Fact]
        public void Closure_ClassToStruct_DelegateConversion()
        {
            using var _ = new EditAndContinueTest()
                .AddBaseline(
                    source: """
                    using System;
                    class C
                    {
                        static void G(Func<int> f) {}

                        public void F()
                        <N:0>{</N:0>
                            int <N:1>x = 1</N:1>;
                            <N:2>int L() => x;</N:2>
                            G(L);
                        }
                    }
                    """,
                    validator: g =>
                    {
                        g.VerifySynthesizedMembers(displayTypeKind: true,
                        [
                            "class C: {<>c__DisplayClass1_0}",
                            "class C.<>c__DisplayClass1_0: {x, <F>g__L|0}"
                        ]);
                    })

                .AddGeneration(
                    source: """
                    using System;
                    class C
                    {
                        static void G(Func<int> f) {}
                    
                        public void F()
                        <N:0>{</N:0>
                            int <N:1>x = 1</N:1>;
                            <N:2>int L() => x;</N:2>
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
                            "System.Runtime.CompilerServices.HotReloadException",
                            "struct C.<>c__DisplayClass1_0#1: {x}",
                            "class C: {<F>g__L|1_0#1, <>c__DisplayClass1_0#1}"
                        ]);

                        g.VerifyMethodDefNames("F", "<F>g__L|0", "<F>g__L|1_0#1", ".ctor");

                        g.VerifyIL(
                        """
                        {
                          // Code size       11 (0xb)
                          .maxstack  2
                          IL_0000:  nop
                          IL_0001:  ldloca.s   V_1
                          IL_0003:  ldc.i4.1
                          IL_0004:  stfld      0x04000003
                          IL_0009:  nop
                          IL_000a:  ret
                        }
                        {
                          // Code size       13 (0xd)
                          .maxstack  8
                          IL_0000:  ldstr      0x70000005
                          IL_0005:  ldc.i4.s   -5
                          IL_0007:  newobj     0x06000007
                          IL_000c:  throw
                        }
                        {
                          // Code size        7 (0x7)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldfld      0x04000003
                          IL_0006:  ret
                        }
                        {
                          // Code size       16 (0x10)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldarg.1
                          IL_0002:  call       0x0A000008
                          IL_0007:  nop
                          IL_0008:  ldarg.0
                          IL_0009:  ldarg.2
                          IL_000a:  stfld      0x04000002
                          IL_000f:  ret
                        }
                        """);
                    })
                .Verify();
        }

        [Fact]
        public void Closure_StructToClass()
        {
            using var _ = new EditAndContinueTest()
                .AddBaseline(
                    source: """
                    using System;
                    class C
                    {
                        static void G(Func<int> f) {}

                        public void F()
                        <N:0>{</N:0>
                            int <N:1>x = 1</N:1>;
                            <N:2>int L() => x;</N:2>
                        }
                    }
                    """,
                    validator: g =>
                    {
                        g.VerifySynthesizedMembers(displayTypeKind: true,
                        [
                            "struct C.<>c__DisplayClass1_0: {x}",
                            "class C: {<F>g__L|1_0, <>c__DisplayClass1_0}",
                        ]);
                    })

                .AddGeneration(
                    // 1
                    source: """
                    using System;
                    class C
                    {
                        static void G(Func<int> f) {}
                    
                        public void F()
                        <N:0>{</N:0>
                            int <N:1>x = 1</N:1>;
                            <N:2>int L() => x;</N:2>
                            G(<N:3>() => x</N:3>);
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
                            "System.Runtime.CompilerServices.HotReloadException",
                            "class C: {<>c__DisplayClass1_0#1}",
                            "class C.<>c__DisplayClass1_0#1: {x, <F>g__L|0#1, <F>b__1#1}"
                        ]);

                        g.VerifyTypeRefNames("Object", "CompilerGeneratedAttribute", "Exception", "Func`1");

                        g.VerifyMethodDefNames("F", "<F>g__L|1_0", ".ctor", ".ctor", "<F>g__L|0#1", "<F>b__1#1");

                        g.VerifyIL("""
                        {
                          // Code size       34 (0x22)
                          .maxstack  2
                          IL_0000:  newobj     0x06000006
                          IL_0005:  stloc.1
                          IL_0006:  nop
                          IL_0007:  ldloc.1
                          IL_0008:  ldc.i4.1
                          IL_0009:  stfld      0x04000003
                          IL_000e:  nop
                          IL_000f:  ldloc.1
                          IL_0010:  ldftn      0x06000008
                          IL_0016:  newobj     0x0A000007
                          IL_001b:  call       0x06000001
                          IL_0020:  nop
                          IL_0021:  ret
                        }
                        {
                          // Code size       13 (0xd)
                          .maxstack  8
                          IL_0000:  ldstr      0x70000005
                          IL_0005:  ldc.i4.s   -5
                          IL_0007:  newobj     0x06000005
                          IL_000c:  throw
                        }
                        {
                          // Code size       16 (0x10)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldarg.1
                          IL_0002:  call       0x0A000008
                          IL_0007:  nop
                          IL_0008:  ldarg.0
                          IL_0009:  ldarg.2
                          IL_000a:  stfld      0x04000002
                          IL_000f:  ret
                        }
                        {
                          // Code size        8 (0x8)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  call       0x0A000009
                          IL_0006:  nop
                          IL_0007:  ret
                        }
                        {
                          // Code size        7 (0x7)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldfld      0x04000003
                          IL_0006:  ret
                        }
                        """);
                    })
                .Verify();
        }

        [Fact]
        public void Closure_StructToClass_DelegateConversion()
        {
            using var _ = new EditAndContinueTest()
                .AddBaseline(
                    source: """
                    using System;
                    class C
                    {
                        static void G(Func<int> f) {}

                        public void F()
                        <N:0>{</N:0>
                            int <N:1>x = 1</N:1>;
                            <N:2>int L() => x;</N:2>
                        }
                    }
                    """,
                    validator: g =>
                    {
                        g.VerifySynthesizedMembers(displayTypeKind: true,
                        [
                            "struct C.<>c__DisplayClass1_0: {x}",
                            "class C: {<F>g__L|1_0, <>c__DisplayClass1_0}",
                        ]);
                    })

                .AddGeneration(
                    source: """
                    using System;
                    class C
                    {
                        static void G(Func<int> f) {}
                    
                        public void F()
                        <N:0>{</N:0>
                            int <N:1>x = 1</N:1>;
                            <N:2>int L() => x;</N:2>
                            G(L);
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
                            "System.Runtime.CompilerServices.HotReloadException",
                            "class C: {<>c__DisplayClass1_0#1}",
                            "class C.<>c__DisplayClass1_0#1: {x, <F>g__L|0#1}"
                        ]);

                        g.VerifyMethodDefNames("F", "<F>g__L|1_0", ".ctor", ".ctor", "<F>g__L|0#1");

                        g.VerifyTypeRefNames("Object", "CompilerGeneratedAttribute", "Exception", "Func`1");

                        g.VerifyIL(
                        """
                         {
                          // Code size       34 (0x22)
                          .maxstack  2
                          IL_0000:  newobj     0x06000006
                          IL_0005:  stloc.1
                          IL_0006:  nop
                          IL_0007:  ldloc.1
                          IL_0008:  ldc.i4.1
                          IL_0009:  stfld      0x04000003
                          IL_000e:  nop
                          IL_000f:  ldloc.1
                          IL_0010:  ldftn      0x06000007
                          IL_0016:  newobj     0x0A000007
                          IL_001b:  call       0x06000001
                          IL_0020:  nop
                          IL_0021:  ret
                        }
                        {
                          // Code size       13 (0xd)
                          .maxstack  8
                          IL_0000:  ldstr      0x70000005
                          IL_0005:  ldc.i4.s   -5
                          IL_0007:  newobj     0x06000005
                          IL_000c:  throw
                        }
                        {
                          // Code size       16 (0x10)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldarg.1
                          IL_0002:  call       0x0A000008
                          IL_0007:  nop
                          IL_0008:  ldarg.0
                          IL_0009:  ldarg.2
                          IL_000a:  stfld      0x04000002
                          IL_000f:  ret
                        }
                        {
                          // Code size        8 (0x8)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  call       0x0A000009
                          IL_0006:  nop
                          IL_0007:  ret
                        }
                        {
                          // Code size        7 (0x7)
                          .maxstack  8
                          IL_0000:  ldarg.0
                          IL_0001:  ldfld      0x04000003
                          IL_0006:  ret
                        }
                        """);
                    })
                .Verify();
        }
    }
}
