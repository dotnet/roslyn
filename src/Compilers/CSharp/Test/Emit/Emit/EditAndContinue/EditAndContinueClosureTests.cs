// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.MetadataUtilities;
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
            var compilation0 = CreateCompilationWithMscorlib(source0, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);
            var bytes0 = compilation0.EmitToArray();
            var generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), EmptyLocalsProvider);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, compilation0.GetMember<MethodSymbol>("C.F"), compilation1.GetMember<MethodSymbol>("C.F"))));

            using (var md1 = diff1.GetMetadata())
            {
                var reader1 = md1.Reader;

                var w = new StringWriter();
                var v = new MetadataVisualizer(new[] { generation0.MetadataReader, reader1 }, w);
                v.VisualizeAllGenerations();
                var s = w.ToString();

                // Field 'o'
                // Methods: 'F', '.ctor', '<F>b__1'
                // Type: display class
                CheckEncLogDefinitions(reader1,
                    Row(2, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                    Row(3, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
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
            CheckNames(reader0, reader0.GetMethodDefNames(), "F", ".ctor", "<F>b__0_1", ".ctor", "<F>b__2", ".cctor", ".ctor", "<F>b__0_0");
            CheckNames(reader0, reader0.GetFieldDefNames(), "a", "<>9", "<>9__0_0");

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreatePdbInfoProvider().GetEncMethodDebugInfo);
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Insert, null, f_int1)));

            var reader1 = diff1.GetMetadata().Reader;

            CheckNames(new[] { reader0, reader1 }, reader1.GetTypeDefNames(), "<>c__DisplayClass0#1_0");
            CheckNames(new[] { reader0, reader1 }, reader1.GetMethodDefNames(), ".ctor", "F", "<F>b__0#1_1", ".ctor", "<F>b__2", "<F>b__0#1_0");
            CheckNames(new[] { reader0, reader1 }, reader1.GetFieldDefNames(), "a", "<>9__0#1_0");

            diff1.VerifySynthesizedMembers(
                "C: {<F>b__0#1_1, <>c__DisplayClass0#1_0, <>c}",
                "C.<>c: {<>9__0#1_0, <F>b__0#1_0}",
                "C.<>c__DisplayClass0#1_0: {a, <F>b__2}");

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Insert, null, f_byte2)));

            var reader2 = diff2.GetMetadata().Reader;

            CheckNames(new[] { reader0, reader1, reader2 }, reader2.GetTypeDefNames(), "<>c__DisplayClass1#2_0");
            CheckNames(new[] { reader0, reader1, reader2 }, reader2.GetMethodDefNames(), ".ctor", "F", "<F>b__1#2_1", ".ctor", "<F>b__2", "<F>b__1#2_0");
            CheckNames(new[] { reader0, reader1, reader2 }, reader2.GetFieldDefNames(), "a", "<>9__1#2_0");
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
            CheckNames(reader0, reader0.GetMethodDefNames(), "F", ".ctor", "<F>b__0_1", ".ctor", "<F>b__2", ".cctor", ".ctor", "<F>b__0_0");
            CheckNames(reader0, reader0.GetFieldDefNames(), "a", "<>9", "<>9__0_0");

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreatePdbInfoProvider().GetEncMethodDebugInfo);
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Insert, null, f_int1)));

            var reader1 = diff1.GetMetadata().Reader;

            CheckNames(new[] { reader0, reader1 }, reader1.GetTypeDefNames(), "<>c__DisplayClass0#1_0`1", "<>c__0#1`1");
            CheckNames(new[] { reader0, reader1 }, reader1.GetMethodDefNames(), "F", "<F>b__0#1_1", ".ctor", "<F>b__2", ".cctor", ".ctor", "<F>b__0#1_0");
            CheckNames(new[] { reader0, reader1 }, reader1.GetFieldDefNames(), "a", "<>9", "<>9__0#1_0");

            diff1.VerifySynthesizedMembers(
                "C: {<F>b__0#1_1, <>c__DisplayClass0#1_0, <>c__0#1}",
                "C.<>c__0#1<T>: {<>9__0#1_0, <F>b__0#1_0}",
                "C.<>c__DisplayClass0#1_0<T>: {a, <F>b__2}");

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Insert, null, f_byte2)));

            var reader2 = diff2.GetMetadata().Reader;

            CheckNames(new[] { reader0, reader1, reader2 }, reader2.GetTypeDefNames(), "<>c__DisplayClass1#2_0`1", "<>c__1#2`1");
            CheckNames(new[] { reader0, reader1, reader2 }, reader2.GetMethodDefNames(), "F", "<F>b__1#2_1", ".ctor", "<F>b__2", ".cctor", ".ctor", "<F>b__1#2_0");
            CheckNames(new[] { reader0, reader1, reader2 }, reader2.GetFieldDefNames(), "a", "<>9", "<>9__1#2_0");
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

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreatePdbInfoProvider().GetEncMethodDebugInfo);
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Insert, null, f1),
                    new SemanticEdit(SemanticEditKind.Update, main0, main1, preserveLocalVariables: true)));

            diff1.VerifySynthesizedMembers(
                "C: {<F>b__1#1_1, <>c__DisplayClass1#1_0, <>c}",
                "C.<>c: {<>9__1#1_0, <F>b__1#1_0}",
                "C.<>c__DisplayClass1#1_0: {a, <F>b__2}");

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Insert, null, f_int2),
                    new SemanticEdit(SemanticEditKind.Update, main1, main2, preserveLocalVariables: true)));

            diff2.VerifySynthesizedMembers(
                "C: {<F>b__1#2_1, <>c__DisplayClass1#2_0, <>c, <F>b__1#1_1, <>c__DisplayClass1#1_0}",
                "C.<>c: {<>9__1#2_0, <F>b__1#2_0, <>9__1#1_0, <F>b__1#1_0}",
                "C.<>c__DisplayClass1#1_0: {a, <F>b__2}",
                "C.<>c__DisplayClass1#2_0: {a, <F>b__2}");

            var diff3 = compilation3.EmitDifference(
                diff2.NextGeneration,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, main2, main3, preserveLocalVariables: true)));

            diff3.VerifySynthesizedMembers(
                "C: {<F>b__1#2_1, <>c__DisplayClass1#2_0, <>c, <F>b__1#1_1, <>c__DisplayClass1#1_0}",
                "C.<>c: {<>9__1#2_0, <F>b__1#2_0, <>9__1#1_0, <F>b__1#1_0}",
                "C.<>c__DisplayClass1#1_0: {a, <F>b__2}",
                "C.<>c__DisplayClass1#2_0: {a, <F>b__2}");
        }
    }
}
