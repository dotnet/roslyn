// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.MetadataUtilities;
using Roslyn.Test.PdbUtilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class EditAndContinueTests : EmitMetadataTestBase
    {
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
            var compilation0 = CreateCompilationWithMscorlib(source0, compOptions: TestOptions.UnoptimizedExe);
            var compilation1 = CreateCompilationWithMscorlib(source1, compOptions: TestOptions.UnoptimizedExe);

            // Verify full metadata contains expected rows.
            var bytes0 = compilation0.EmitToArray(debug: true);
            using (var md0 = ModuleMetadata.CreateFromImage(bytes0))
            {
                var reader0 = md0.MetadataReader;
                CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "C");
                CheckNames(reader0, reader0.GetMethodDefNames(), "Main", "F", ".ctor");
                CheckNames(reader0, reader0.GetMemberRefNames(), /*CompilationRelaxationsAttribute.*/".ctor", /*RuntimeCompatibilityAttribute.*/".ctor", /*Object.*/".ctor");

                var method0 = compilation0.GetMember<MethodSymbol>("C.F");
                var generation0 = EmitBaseline.CreateInitialBaseline(
                    md0,
                    EmptyLocalsProvider);
                var method1 = compilation1.GetMember<MethodSymbol>("C.F");

                var diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1)));

                // Verify delta metadata contains expected rows.
                using (var md1 = diff1.GetMetadata())
                {
                    var reader1 = md1.Reader;
                    var readers = new[] { reader0, reader1 };
                    EncValidation.VerifyModuleMvid(1, reader0, reader1);
                    CheckNames(readers, reader1.GetTypeDefNames());
                    CheckNames(readers, reader1.GetMethodDefNames(), "F");
                    CheckNames(readers, reader1.GetMemberRefNames(), /*String.*/"Empty");
                    CheckEncLog(reader1,
                        Row(2, TableIndex.AssemblyRef, EditAndContinueOperation.Default),
                        Row(4, TableIndex.MemberRef, EditAndContinueOperation.Default),
                        Row(4, TableIndex.TypeRef, EditAndContinueOperation.Default),
                        Row(5, TableIndex.TypeRef, EditAndContinueOperation.Default),
                        Row(2, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default)); // C.F
                    CheckEncMap(reader1,
                        Handle(4, TableIndex.TypeRef),
                        Handle(5, TableIndex.TypeRef),
                        Handle(2, TableIndex.MethodDef), // C.F
                        Handle(4, TableIndex.MemberRef),
                        Handle(2, TableIndex.StandAloneSig),
                        Handle(2, TableIndex.AssemblyRef));
                }
            }
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
            var compilation0 = CreateCompilationWithMscorlib(source0, compOptions: TestOptions.UnoptimizedExe);
            var compilation1 = CreateCompilationWithMscorlib(source1, compOptions: TestOptions.UnoptimizedExe);
            var compilation2 = CreateCompilationWithMscorlib(source2, compOptions: TestOptions.UnoptimizedExe);

            // Verify full metadata contains expected rows.
            var bytes0 = compilation0.EmitToArray(debug: true);
            using (var md0 = ModuleMetadata.CreateFromImage(bytes0))
            {
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
                using (var md1 = diff1.GetMetadata())
                {
                    var reader1 = md1.Reader;
                    var readers = new[] { reader0, reader1 };
                    EncValidation.VerifyModuleMvid(1, reader0, reader1);
                    CheckNames(readers, reader1.GetTypeDefNames());
                    CheckNames(readers, reader1.GetMethodDefNames(), "F2");
                    CheckNames(readers, reader1.GetParameterDefNames(), "", "s2");
                    CheckEncLog(reader1,
                        Row(2, TableIndex.AssemblyRef, EditAndContinueOperation.Default),
                        Row(5, TableIndex.TypeRef, EditAndContinueOperation.Default),
                        Row(2, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                        Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default), // C.F2
                        Row(5, TableIndex.MethodDef, EditAndContinueOperation.AddParameter),
                        Row(2, TableIndex.Param, EditAndContinueOperation.Default), // return type
                        Row(5, TableIndex.MethodDef, EditAndContinueOperation.AddParameter),
                        Row(3, TableIndex.Param, EditAndContinueOperation.Default), // s2
                        Row(1, TableIndex.Constant, EditAndContinueOperation.Default), // = "2"
                        Row(3, TableIndex.CustomAttribute, EditAndContinueOperation.Default)); // [A]
                    CheckEncMap(reader1,
                        Handle(5, TableIndex.TypeRef),
                        Handle(5, TableIndex.MethodDef), // C.F2
                        Handle(2, TableIndex.Param), // return type
                        Handle(3, TableIndex.Param), // s2
                        Handle(1, TableIndex.Constant),
                        Handle(3, TableIndex.CustomAttribute),
                        Handle(2, TableIndex.StandAloneSig),
                        Handle(2, TableIndex.AssemblyRef));

                    var method2 = compilation2.GetMember<MethodSymbol>("C.F2");
                    var diff2 = compilation2.EmitDifference(
                        diff1.NextGeneration,
                        ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method1, method2)));

                    // Verify delta metadata contains expected rows.
                    using (var md2 = diff2.GetMetadata())
                    {
                        var reader2 = md2.Reader;
                        readers = new[] { reader0, reader1, reader2 };
                        EncValidation.VerifyModuleMvid(2, reader1, reader2);
                        CheckNames(readers, reader2.GetTypeDefNames());
                        CheckNames(readers, reader2.GetMethodDefNames(), "F2");
                        CheckNames(readers, reader2.GetParameterDefNames());
                        CheckEncLog(reader2,
                            Row(3, TableIndex.AssemblyRef, EditAndContinueOperation.Default),
                            Row(6, TableIndex.TypeRef, EditAndContinueOperation.Default), // C.BaseType. Not strictly necessary.
                            Row(3, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                            Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default)); // C.F2
                        CheckEncMap(reader2,
                            Handle(6, TableIndex.TypeRef), // C.BaseType. Not strictly necessary.
                            Handle(5, TableIndex.MethodDef), // C.F2
                            Handle(3, TableIndex.StandAloneSig),
                            Handle(3, TableIndex.AssemblyRef));
                    }
                }
            }
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
            var compilation0 = CreateCompilationWithMscorlib(source0, compOptions: TestOptions.UnoptimizedDll);
            var compilation1 = CreateCompilationWithMscorlib(source1, compOptions: TestOptions.UnoptimizedDll);

            // Verify full metadata contains expected rows.
            var bytes0 = compilation0.EmitToArray(debug: true);
            using (var md0 = ModuleMetadata.CreateFromImage(bytes0))
            {
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
                using (var md1 = diff1.GetMetadata())
                {
                    var reader1 = md1.Reader;
                    var readers = new[] { reader0, reader1 };
                    CheckNames(readers, reader1.GetTypeDefNames());
                    CheckNames(readers, reader1.GetFieldDefNames(), "G");
                    CheckNames(readers, reader1.GetMethodDefNames(), ".ctor");
                    CheckEncLog(reader1,
                        Row(2, TableIndex.AssemblyRef, EditAndContinueOperation.Default),
                        Row(4, TableIndex.MemberRef, EditAndContinueOperation.Default),
                        Row(4, TableIndex.TypeRef, EditAndContinueOperation.Default),
                        Row(2, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                        Row(2, TableIndex.Field, EditAndContinueOperation.Default), // C.G
                        Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default));
                    CheckEncMap(reader1,
                        Handle(4, TableIndex.TypeRef),
                        Handle(2, TableIndex.Field), // C.G
                        Handle(1, TableIndex.MethodDef),
                        Handle(4, TableIndex.MemberRef),
                        Handle(2, TableIndex.AssemblyRef));
                }
            }
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
            var compilation0 = CreateCompilationWithMscorlib(source0, compOptions: TestOptions.UnoptimizedDll);
            var compilation1 = CreateCompilationWithMscorlib(source1, compOptions: TestOptions.UnoptimizedDll);
            var compilation2 = CreateCompilationWithMscorlib(source2, compOptions: TestOptions.UnoptimizedDll);

            // Verify full metadata contains expected rows.
            var bytes0 = compilation0.EmitToArray(debug: true);
            using (var md0 = ModuleMetadata.CreateFromImage(bytes0))
            {
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
                using (var md1 = diff1.GetMetadata())
                {
                    var reader1 = md1.Reader;
                    var readers = new[] { reader0, reader1 };
                    CheckNames(readers, reader1.GetTypeDefNames());
                    CheckNames(readers, reader1.GetFieldDefNames());
                    CheckNames(readers, reader1.GetPropertyDefNames(), "R");
                    CheckNames(readers, reader1.GetMethodDefNames(), "get_R");
                    CheckEncLog(reader1,
                        Row(2, TableIndex.AssemblyRef, EditAndContinueOperation.Default),
                        Row(5, TableIndex.TypeRef, EditAndContinueOperation.Default),
                        Row(2, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(2, TableIndex.PropertyMap, EditAndContinueOperation.Default),
                        Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                        Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(2, TableIndex.PropertyMap, EditAndContinueOperation.AddProperty),
                        Row(2, TableIndex.Property, EditAndContinueOperation.Default),
                        Row(3, TableIndex.MethodSemantics, EditAndContinueOperation.Default));
                    CheckEncMap(reader1,
                        Handle(5, TableIndex.TypeRef),
                        Handle(5, TableIndex.MethodDef),
                        Handle(2, TableIndex.StandAloneSig),
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
                    using (var md2 = diff2.GetMetadata())
                    {
                        var reader2 = md2.Reader;
                        readers = new[] { reader0, reader1, reader2 };
                        CheckNames(readers, reader2.GetTypeDefNames());
                        CheckNames(readers, reader2.GetFieldDefNames(), "<Q>k__BackingField");
                        CheckNames(readers, reader2.GetPropertyDefNames(), "Q", "S");
                        CheckNames(readers, reader2.GetMethodDefNames(), "get_Q", "set_Q", "set_S");
                        CheckEncLog(reader2,
                            Row(3, TableIndex.AssemblyRef, EditAndContinueOperation.Default),
                            Row(5, TableIndex.MemberRef, EditAndContinueOperation.Default), // CompilerGeneratedAttribute..ctor for <Q>k__BackingField
                            Row(6, TableIndex.TypeRef, EditAndContinueOperation.Default),
                            Row(7, TableIndex.TypeRef, EditAndContinueOperation.Default),
                            Row(3, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
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
                            Row(6, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(7, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(8, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(4, TableIndex.MethodSemantics, EditAndContinueOperation.Default),
                            Row(5, TableIndex.MethodSemantics, EditAndContinueOperation.Default),
                            Row(6, TableIndex.MethodSemantics, EditAndContinueOperation.Default));
                        CheckEncMap(reader2,
                            Handle(6, TableIndex.TypeRef),
                            Handle(7, TableIndex.TypeRef),
                            Handle(2, TableIndex.Field),
                            Handle(6, TableIndex.MethodDef),
                            Handle(7, TableIndex.MethodDef),
                            Handle(8, TableIndex.MethodDef),
                            Handle(2, TableIndex.Param),
                            Handle(3, TableIndex.Param),
                            Handle(5, TableIndex.MemberRef), // CompilerGeneratedAttribute..ctor for <Q>k__BackingField
                            Handle(6, TableIndex.CustomAttribute),
                            Handle(7, TableIndex.CustomAttribute),
                            Handle(8, TableIndex.CustomAttribute),
                            Handle(3, TableIndex.StandAloneSig),
                            Handle(3, TableIndex.Property),
                            Handle(4, TableIndex.Property),
                            Handle(4, TableIndex.MethodSemantics),
                            Handle(5, TableIndex.MethodSemantics),
                            Handle(6, TableIndex.MethodSemantics),
                            Handle(3, TableIndex.AssemblyRef));
                    }
                }
            }
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
            var compilation0 = CreateCompilationWithMscorlib(source0, compOptions: TestOptions.UnoptimizedDll);
            var compilation1 = CreateCompilationWithMscorlib(source1, compOptions: TestOptions.UnoptimizedDll);
            var compilation2 = CreateCompilationWithMscorlib(source2, compOptions: TestOptions.UnoptimizedDll);

            // Verify full metadata contains expected rows.
            var bytes0 = compilation0.EmitToArray(debug: true);
            using (var md0 = ModuleMetadata.CreateFromImage(bytes0))
            {
                var reader0 = md0.MetadataReader;
                CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "D", "A", "B");
                CheckNames(reader0, reader0.GetFieldDefNames(), "E");
                CheckNames(reader0, reader0.GetEventDefNames(), "E");
                CheckNames(reader0, reader0.GetMethodDefNames(), ".ctor", "BeginInvoke", "EndInvoke", "Invoke", "add_E", "remove_E", ".ctor", ".ctor");

                var generation0 = EmitBaseline.CreateInitialBaseline(md0, EmptyLocalsProvider);

                var diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Insert, null, compilation1.GetMember<EventSymbol>("B.F"))));

                // Verify delta metadata contains expected rows.
                using (var md1 = diff1.GetMetadata())
                {
                    var reader1 = md1.Reader;
                    var readers = new[] { reader0, reader1 };
                    CheckNames(readers, reader1.GetTypeDefNames());
                    CheckNames(readers, reader1.GetFieldDefNames(), "F");
                    CheckNames(readers, reader1.GetMethodDefNames(), "add_F", "remove_F");
                    CheckEncLog(reader1,
                        Row(2, TableIndex.AssemblyRef, EditAndContinueOperation.Default),
                        Row(8, TableIndex.MemberRef, EditAndContinueOperation.Default),
                        Row(9, TableIndex.MemberRef, EditAndContinueOperation.Default),
                        Row(10, TableIndex.MemberRef, EditAndContinueOperation.Default),
                        Row(11, TableIndex.MemberRef, EditAndContinueOperation.Default),
                        Row(2, TableIndex.MethodSpec, EditAndContinueOperation.Default),
                        Row(10, TableIndex.TypeRef, EditAndContinueOperation.Default),
                        Row(11, TableIndex.TypeRef, EditAndContinueOperation.Default),
                        Row(12, TableIndex.TypeRef, EditAndContinueOperation.Default),
                        Row(13, TableIndex.TypeRef, EditAndContinueOperation.Default),
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
                        Row(6, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(7, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(8, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(3, TableIndex.MethodSemantics, EditAndContinueOperation.Default),
                        Row(4, TableIndex.MethodSemantics, EditAndContinueOperation.Default));
                    CheckEncMap(reader1,
                        Handle(10, TableIndex.TypeRef),
                        Handle(11, TableIndex.TypeRef),
                        Handle(12, TableIndex.TypeRef),
                        Handle(13, TableIndex.TypeRef),
                        Handle(2, TableIndex.Field),
                        Handle(9, TableIndex.MethodDef),
                        Handle(10, TableIndex.MethodDef),
                        Handle(8, TableIndex.Param),
                        Handle(9, TableIndex.Param),
                        Handle(8, TableIndex.MemberRef),
                        Handle(9, TableIndex.MemberRef),
                        Handle(10, TableIndex.MemberRef),
                        Handle(11, TableIndex.MemberRef),
                        Handle(6, TableIndex.CustomAttribute),
                        Handle(7, TableIndex.CustomAttribute),
                        Handle(8, TableIndex.CustomAttribute),
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
                    using (var md2 = diff2.GetMetadata())
                    {
                        var reader2 = md2.Reader;
                        readers = new[] { reader0, reader1, reader2 };
                        CheckNames(readers, reader2.GetTypeDefNames());
                        CheckNames(readers, reader2.GetFieldDefNames(), "G", "H");
                        CheckNames(readers, reader2.GetMethodDefNames(), "add_G", "remove_G", "add_H", "remove_H");
                        CheckEncLog(reader2,
                            Row(3, TableIndex.AssemblyRef, EditAndContinueOperation.Default),
                            Row(12, TableIndex.MemberRef, EditAndContinueOperation.Default), // CompilerGeneratedAttribute..ctor for A.G and B.H fields
                            Row(13, TableIndex.MemberRef, EditAndContinueOperation.Default),
                            Row(14, TableIndex.MemberRef, EditAndContinueOperation.Default),
                            Row(15, TableIndex.MemberRef, EditAndContinueOperation.Default),
                            Row(3, TableIndex.MethodSpec, EditAndContinueOperation.Default),
                            Row(14, TableIndex.TypeRef, EditAndContinueOperation.Default),
                            Row(15, TableIndex.TypeRef, EditAndContinueOperation.Default),
                            Row(16, TableIndex.TypeRef, EditAndContinueOperation.Default),
                            Row(17, TableIndex.TypeRef, EditAndContinueOperation.Default),
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
                            Row(9, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(10, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(11, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(12, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(13, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(14, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(5, TableIndex.MethodSemantics, EditAndContinueOperation.Default),
                            Row(6, TableIndex.MethodSemantics, EditAndContinueOperation.Default),
                            Row(7, TableIndex.MethodSemantics, EditAndContinueOperation.Default),
                            Row(8, TableIndex.MethodSemantics, EditAndContinueOperation.Default));
                        CheckEncMap(reader2,
                            Handle(14, TableIndex.TypeRef),
                            Handle(15, TableIndex.TypeRef),
                            Handle(16, TableIndex.TypeRef),
                            Handle(17, TableIndex.TypeRef),
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
                            Handle(12, TableIndex.MemberRef), // CompilerGeneratedAttribute..ctor for A.G and B.H fields
                            Handle(13, TableIndex.MemberRef),
                            Handle(14, TableIndex.MemberRef),
                            Handle(15, TableIndex.MemberRef),
                            Handle(9, TableIndex.CustomAttribute),
                            Handle(10, TableIndex.CustomAttribute),
                            Handle(11, TableIndex.CustomAttribute),
                            Handle(12, TableIndex.CustomAttribute),
                            Handle(13, TableIndex.CustomAttribute),
                            Handle(14, TableIndex.CustomAttribute),
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
                }
            }
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
            var compilation0 = CreateCompilationWithMscorlib(source0, compOptions: TestOptions.UnoptimizedDll);
            var compilation1 = CreateCompilationWithMscorlib(source1, compOptions: TestOptions.UnoptimizedDll);

            // Verify full metadata contains expected rows.
            var bytes0 = compilation0.EmitToArray(debug: true);
            using (var md0 = ModuleMetadata.CreateFromImage(bytes0))
            {
                var reader0 = md0.MetadataReader;
                CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "A", "B");
                CheckNames(reader0, reader0.GetMethodDefNames(), "F", ".ctor", ".ctor");
                Assert.Equal(1, reader0.GetTableRowCount(TableIndex.NestedClass));

                var generation0 = EmitBaseline.CreateInitialBaseline(md0, EmptyLocalsProvider);

                var diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(
                        new SemanticEdit(SemanticEditKind.Insert, null, compilation1.GetMember<NamedTypeSymbol>("A.C")),
                        new SemanticEdit(SemanticEditKind.Update, compilation0.GetMember<MethodSymbol>("A.F"), compilation1.GetMember<MethodSymbol>("A.F"))));

                // Verify delta metadata contains expected rows.
                using (var md1 = diff1.GetMetadata())
                {
                    var reader1 = md1.Reader;
                    var readers = new[] { reader0, reader1 };
                    CheckNames(readers, reader1.GetTypeDefNames(), "C", "D");
                    Assert.Equal(2, reader1.GetTableRowCount(TableIndex.NestedClass));
                    CheckNames(readers, reader1.GetMethodDefNames(), "F", "G", ".ctor", ".ctor");
                    CheckEncLog(reader1,
                        Row(2, TableIndex.AssemblyRef, EditAndContinueOperation.Default),
                        Row(4, TableIndex.MemberRef, EditAndContinueOperation.Default),
                        Row(4, TableIndex.TypeRef, EditAndContinueOperation.Default),
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
                        Handle(4, TableIndex.TypeRef),
                        Handle(4, TableIndex.TypeDef),
                        Handle(5, TableIndex.TypeDef),
                        Handle(1, TableIndex.Field),
                        Handle(1, TableIndex.MethodDef),
                        Handle(4, TableIndex.MethodDef),
                        Handle(5, TableIndex.MethodDef),
                        Handle(6, TableIndex.MethodDef),
                        Handle(4, TableIndex.MemberRef),
                        Handle(2, TableIndex.StandAloneSig),
                        Handle(2, TableIndex.AssemblyRef),
                        Handle(2, TableIndex.NestedClass),
                        Handle(3, TableIndex.NestedClass));
                }
            }
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
            var compilation0 = CreateCompilationWithMscorlib(source0, compOptions: TestOptions.UnoptimizedDll);
            var compilation1 = CreateCompilationWithMscorlib(source1, compOptions: TestOptions.UnoptimizedDll);

            // Verify full metadata contains expected rows.
            var bytes0 = compilation0.EmitToArray(debug: true);
            using (var md0 = ModuleMetadata.CreateFromImage(bytes0))
            {
                var reader0 = md0.MetadataReader;
                CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "A", "B`1");
                Assert.Equal(1, reader0.GetTableRowCount(TableIndex.NestedClass));

                var generation0 = EmitBaseline.CreateInitialBaseline(md0, EmptyLocalsProvider);

                var diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(
                        new SemanticEdit(SemanticEditKind.Insert, null, compilation1.GetMember<NamedTypeSymbol>("A.B.C")),
                        new SemanticEdit(SemanticEditKind.Update, compilation0.GetMember<MethodSymbol>("A.F"), compilation1.GetMember<MethodSymbol>("A.F"))));

                // Verify delta metadata contains expected rows.
                using (var md1 = diff1.GetMetadata())
                {
                    var reader1 = md1.Reader;
                    var readers = new[] { reader0, reader1 };
                    CheckNames(readers, reader1.GetTypeDefNames(), "C`1");
                    Assert.Equal(1, reader1.GetTableRowCount(TableIndex.NestedClass));
                    CheckEncLog(reader1,
                        Row(2, TableIndex.AssemblyRef, EditAndContinueOperation.Default),
                        Row(4, TableIndex.MemberRef, EditAndContinueOperation.Default),
                        Row(5, TableIndex.MemberRef, EditAndContinueOperation.Default),
                        Row(6, TableIndex.MemberRef, EditAndContinueOperation.Default),
                        Row(7, TableIndex.MemberRef, EditAndContinueOperation.Default),
                        Row(1, TableIndex.MethodSpec, EditAndContinueOperation.Default),
                        Row(4, TableIndex.TypeRef, EditAndContinueOperation.Default),
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
                        Handle(4, TableIndex.TypeRef),
                        Handle(4, TableIndex.TypeDef),
                        Handle(1, TableIndex.MethodDef),
                        Handle(4, TableIndex.MethodDef),
                        Handle(5, TableIndex.MethodDef),
                        Handle(4, TableIndex.MemberRef),
                        Handle(5, TableIndex.MemberRef),
                        Handle(6, TableIndex.MemberRef),
                        Handle(7, TableIndex.MemberRef),
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
            }
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
            var compilation0 = CreateCompilationWithMscorlib(source, compOptions: TestOptions.UnoptimizedDll);
            var compilation1 = CreateCompilationWithMscorlib(source, compOptions: TestOptions.UnoptimizedDll);

            // Verify full metadata contains expected rows.
            var bytes0 = compilation0.EmitToArray(debug: true);
            using (var md0 = ModuleMetadata.CreateFromImage(bytes0))
            {
                var reader0 = md0.MetadataReader;
                CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "I", "C");
                CheckNames(reader0, reader0.GetMethodDefNames(), "M", "I.M", ".ctor");

                var method0 = compilation0.GetMember<NamedTypeSymbol>("C").GetMember<MethodSymbol>("I.M");
                var generation0 = EmitBaseline.CreateInitialBaseline(md0, EmptyLocalsProvider);
                var method1 = compilation1.GetMember<NamedTypeSymbol>("C").GetMember<MethodSymbol>("I.M");
                var diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1)));

                // Verify delta metadata contains expected rows.
                using (var block1 = diff1.GetMetadata())
                {
                    var reader1 = block1.Reader;
                    var readers = new[] { reader0, reader1 };
                    EncValidation.VerifyModuleMvid(1, reader0, reader1);
                    CheckNames(readers, reader1.GetTypeDefNames());
                    CheckNames(readers, reader1.GetMethodDefNames(), "I.M");
                    CheckEncLog(reader1,
                        Row(2, TableIndex.AssemblyRef, EditAndContinueOperation.Default),
                        Row(4, TableIndex.TypeRef, EditAndContinueOperation.Default),
                        Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(2, TableIndex.MethodImpl, EditAndContinueOperation.Default));
                    CheckEncMap(reader1,
                        Handle(4, TableIndex.TypeRef),
                        Handle(2, TableIndex.MethodDef),
                        Handle(2, TableIndex.MethodImpl),
                        Handle(2, TableIndex.AssemblyRef));
                }
            }
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
            var compilation0 = CreateCompilationWithMscorlib(source0, compOptions: TestOptions.UnoptimizedDll);
            var compilation1 = CreateCompilationWithMscorlib(source1, compOptions: TestOptions.UnoptimizedDll);

            // Verify full metadata contains expected rows.
            var bytes0 = compilation0.EmitToArray(debug: true);
            using (var md0 = ModuleMetadata.CreateFromImage(bytes0))
            {
                var reader0 = md0.MetadataReader;
                CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "A", "B", "C", "D");
                CheckNames(reader0, reader0.GetMethodDefNames(), ".ctor", ".ctor", "M1", "get_P1", "add_E1", "remove_E1", ".ctor", ".ctor", "BeginInvoke", "EndInvoke", "Invoke");
                CheckAttributes(reader0,
                    new CustomAttributeRow(Handle(1, TableIndex.Field), Handle(2, TableIndex.MethodDef)), // C.F1, B..ctor
                    new CustomAttributeRow(Handle(1, TableIndex.Property), Handle(1, TableIndex.MethodDef)), // C.P1, A..ctor
                    new CustomAttributeRow(Handle(1, TableIndex.Event), Handle(2, TableIndex.MethodDef)), // C.E1, B..ctor
                    new CustomAttributeRow(Handle(1, TableIndex.Assembly), Handle(1, TableIndex.MemberRef)), // assembly, CompilationRelaxations
                    new CustomAttributeRow(Handle(1, TableIndex.Assembly), Handle(2, TableIndex.MemberRef)), // assembly, RuntimeCompatibility
                    new CustomAttributeRow(Handle(1, TableIndex.GenericParam), Handle(2, TableIndex.MethodDef)), // C.M1.T, B..ctor
                    new CustomAttributeRow(Handle(2, TableIndex.Field), Handle(3, TableIndex.MemberRef)), // C.E1, CompilerGenerated
                    new CustomAttributeRow(Handle(3, TableIndex.MethodDef), Handle(1, TableIndex.MethodDef)), // C.M1, A..ctor
                    new CustomAttributeRow(Handle(5, TableIndex.MethodDef), Handle(3, TableIndex.MemberRef)), // C.add_E1, CompilerGenerated
                    new CustomAttributeRow(Handle(6, TableIndex.MethodDef), Handle(3, TableIndex.MemberRef))); // C.remove_E1, CompilerGenerated

                var generation0 = EmitBaseline.CreateInitialBaseline(md0, EmptyLocalsProvider);

                var diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(
                        new SemanticEdit(SemanticEditKind.Insert, null, compilation1.GetMember<MethodSymbol>("C.M2")),
                        new SemanticEdit(SemanticEditKind.Insert, null, compilation1.GetMember<FieldSymbol>("C.F2")),
                        new SemanticEdit(SemanticEditKind.Insert, null, compilation1.GetMember<PropertySymbol>("C.P2")),
                        new SemanticEdit(SemanticEditKind.Insert, null, compilation1.GetMember<EventSymbol>("C.E2"))));

                // Verify delta metadata contains expected rows.
                using (var md1 = diff1.GetMetadata())
                {
                    var reader1 = md1.Reader;
                    var readers = new[] { reader0, reader1 };
                    CheckNames(readers, reader1.GetTypeDefNames());
                    CheckNames(readers, reader1.GetMethodDefNames(), "M2", "get_P2", "add_E2", "remove_E2");
                    CheckEncLog(reader1,
                        Row(2, TableIndex.AssemblyRef, EditAndContinueOperation.Default),
                        Row(9, TableIndex.MemberRef, EditAndContinueOperation.Default),
                        Row(10, TableIndex.MemberRef, EditAndContinueOperation.Default),
                        Row(11, TableIndex.MemberRef, EditAndContinueOperation.Default),
                        Row(12, TableIndex.MemberRef, EditAndContinueOperation.Default),
                        Row(2, TableIndex.MethodSpec, EditAndContinueOperation.Default),
                        Row(11, TableIndex.TypeRef, EditAndContinueOperation.Default),
                        Row(12, TableIndex.TypeRef, EditAndContinueOperation.Default),
                        Row(13, TableIndex.TypeRef, EditAndContinueOperation.Default),
                        Row(14, TableIndex.TypeRef, EditAndContinueOperation.Default),
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
                        Row(11, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(12, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(13, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(14, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(15, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(16, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(17, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(18, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(4, TableIndex.MethodSemantics, EditAndContinueOperation.Default),
                        Row(5, TableIndex.MethodSemantics, EditAndContinueOperation.Default),
                        Row(6, TableIndex.MethodSemantics, EditAndContinueOperation.Default),
                        Row(2, TableIndex.GenericParam, EditAndContinueOperation.Default));
                    CheckEncMap(reader1,
                        Handle(11, TableIndex.TypeRef),
                        Handle(12, TableIndex.TypeRef),
                        Handle(13, TableIndex.TypeRef),
                        Handle(14, TableIndex.TypeRef),
                        Handle(3, TableIndex.Field),
                        Handle(4, TableIndex.Field),
                        Handle(12, TableIndex.MethodDef),
                        Handle(13, TableIndex.MethodDef),
                        Handle(14, TableIndex.MethodDef),
                        Handle(15, TableIndex.MethodDef),
                        Handle(8, TableIndex.Param),
                        Handle(9, TableIndex.Param),
                        Handle(9, TableIndex.MemberRef),
                        Handle(10, TableIndex.MemberRef),
                        Handle(11, TableIndex.MemberRef),
                        Handle(12, TableIndex.MemberRef),
                        Handle(11, TableIndex.CustomAttribute),
                        Handle(12, TableIndex.CustomAttribute),
                        Handle(13, TableIndex.CustomAttribute),
                        Handle(14, TableIndex.CustomAttribute),
                        Handle(15, TableIndex.CustomAttribute),
                        Handle(16, TableIndex.CustomAttribute),
                        Handle(17, TableIndex.CustomAttribute),
                        Handle(18, TableIndex.CustomAttribute),
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
                        new CustomAttributeRow(Handle(2, TableIndex.Property), Handle(2, TableIndex.MethodDef)),
                        new CustomAttributeRow(Handle(2, TableIndex.Event), Handle(1, TableIndex.MethodDef)),
                        new CustomAttributeRow(Handle(2, TableIndex.GenericParam), Handle(1, TableIndex.MethodDef)),
                        new CustomAttributeRow(Handle(3, TableIndex.Field), Handle(1, TableIndex.MethodDef)),
                        new CustomAttributeRow(Handle(4, TableIndex.Field), Handle(9, TableIndex.MemberRef)),
                        new CustomAttributeRow(Handle(12, TableIndex.MethodDef), Handle(2, TableIndex.MethodDef)),
                        new CustomAttributeRow(Handle(14, TableIndex.MethodDef), Handle(9, TableIndex.MemberRef)),
                        new CustomAttributeRow(Handle(15, TableIndex.MethodDef), Handle(9, TableIndex.MemberRef)));
                }
            }
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
            var compilation0 = CreateCompilationWithMscorlib(source0, compOptions: TestOptions.UnoptimizedDll);
            var compilation1 = CreateCompilationWithMscorlib(source1, compOptions: TestOptions.UnoptimizedDll);

            // Verify full metadata contains expected rows.
            var bytes0 = compilation0.EmitToArray(debug: true);
            using (var md0 = ModuleMetadata.CreateFromImage(bytes0))
            {
                var reader0 = md0.MetadataReader;
                CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "C");

                var generation0 = EmitBaseline.CreateInitialBaseline(md0, EmptyLocalsProvider);

                var diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Insert, null, compilation1.GetMember<MethodSymbol>("C.M"))));

                // Verify delta metadata contains expected rows.
                using (var md1 = diff1.GetMetadata())
                {
                    var readers = new[] { reader0, md1.Reader };
                    CheckNames(readers, md1.Reader.GetTypeDefNames());
                    CheckNames(readers, md1.Reader.GetMethodDefNames(), "M");
                    CheckEncLog(md1.Reader,
                        Row(2, TableIndex.AssemblyRef, EditAndContinueOperation.Default),
                        Row(5, TableIndex.TypeRef, EditAndContinueOperation.Default),
                        Row(2, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                        Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default)); // C.M
                    CheckEncMap(md1.Reader,
                        Handle(5, TableIndex.TypeRef),
                        Handle(2, TableIndex.MethodDef), // C.M
                        Handle(2, TableIndex.AssemblyRef));
                }
            }
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
            var compilation0 = CreateCompilationWithMscorlib(source0, compOptions: TestOptions.UnoptimizedDll);
            var compilation1 = CreateCompilationWithMscorlib(source1, compOptions: TestOptions.UnoptimizedDll);

            // Verify full metadata contains expected rows.
            var bytes0 = compilation0.EmitToArray(debug: true);
            using (var md0 = ModuleMetadata.CreateFromImage(bytes0))
            {
                var reader0 = md0.MetadataReader;
                CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "D", "C");
                CheckNames(reader0, reader0.GetEventDefNames(), "E");
                CheckNames(reader0, reader0.GetFieldDefNames(), "F", "E");
                CheckNames(reader0, reader0.GetMethodDefNames(), ".ctor", "BeginInvoke", "EndInvoke", "Invoke", "get_P", "add_E", "remove_E", "M", ".ctor");
                CheckNames(reader0, reader0.GetPropertyDefNames(), "P");

                var method0 = compilation0.GetMember<MethodSymbol>("C.M");

                // Emit delta metadata.
                var generation0 = EmitBaseline.CreateInitialBaseline(md0, EmptyLocalsProvider);
                var method1 = compilation1.GetMember<MethodSymbol>("C.M");

                var diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetLocalMap(method1, method0), preserveLocalVariables: true)));

                // Verify delta metadata contains expected rows.
                using (var md1 = diff1.GetMetadata())
                {
                    var reader1 = md1.Reader;
                    var readers = new[] { reader0, reader1 };
                    CheckNames(readers, reader1.GetTypeDefNames());
                    CheckNames(readers, reader1.GetEventDefNames());
                    CheckNames(readers, reader1.GetFieldDefNames());
                    CheckNames(readers, reader1.GetMethodDefNames(), "M");
                    CheckNames(readers, reader1.GetPropertyDefNames());
                }
            }
        }

        [Fact]
        public void Iterator()
        {
            var source0 =
@"using System.Collections.Generic;
class C
{
    static IEnumerable<object> F()
    {
        yield return 0;
    }
    static void M()
    {
    }
}";
            var source1 =
@"using System.Collections.Generic;
class C
{
    static IEnumerable<object> F()
    {
        yield return 0;
    }
    static IEnumerable<int> G()
    {
        yield return 1;
    }
    static void M()
    {
    }
}";
            var source2 =
@"using System.Collections.Generic;
class C
{
    static IEnumerable<object> F()
    {
        yield return 0;
    }
    static IEnumerable<int> G()
    {
        yield return 1;
    }
    static void M()
    {
        foreach (var i in G())
        {
        }
    }
}";
            var source3 =
@"using System.Collections.Generic;
class C
{
    static IEnumerable<object> F()
    {
        yield return 0;
    }
    static IEnumerable<int> G()
    {
        yield return 2;
    }
    static void M()
    {
        foreach (var i in G())
        {
        }
    }
}";
            var compilation0 = CreateCompilationWithMscorlib(Parse(source0, "a.cs"), compOptions: TestOptions.UnoptimizedDll);
            var compilation1 = CreateCompilationWithMscorlib(Parse(source1, "a.cs"), compOptions: TestOptions.UnoptimizedDll);
            var compilation2 = CreateCompilationWithMscorlib(Parse(source2, "a.cs"), compOptions: TestOptions.UnoptimizedDll);
            var compilation3 = CreateCompilationWithMscorlib(Parse(source3, "a.cs"), compOptions: TestOptions.UnoptimizedDll);

            var bytes0 = compilation0.EmitToArray(debug: true);
            var generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), EmptyLocalsProvider);
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Insert, null, compilation1.GetMember<MethodSymbol>("C.G"))));

            using (var md1 = diff1.GetMetadata())
            {
                var reader1 = md1.Reader;

                CheckEncLog(reader1,
                    Row(2, TableIndex.AssemblyRef, EditAndContinueOperation.Default),
                    Row(16, TableIndex.MemberRef, EditAndContinueOperation.Default),
                    Row(17, TableIndex.MemberRef, EditAndContinueOperation.Default),
                    Row(18, TableIndex.MemberRef, EditAndContinueOperation.Default),
                    Row(19, TableIndex.MemberRef, EditAndContinueOperation.Default),
                    Row(20, TableIndex.MemberRef, EditAndContinueOperation.Default),
                    Row(21, TableIndex.MemberRef, EditAndContinueOperation.Default),
                    Row(22, TableIndex.MemberRef, EditAndContinueOperation.Default),
                    Row(23, TableIndex.MemberRef, EditAndContinueOperation.Default),
                    Row(24, TableIndex.MemberRef, EditAndContinueOperation.Default),
                    Row(25, TableIndex.MemberRef, EditAndContinueOperation.Default),
                    Row(26, TableIndex.MemberRef, EditAndContinueOperation.Default),
                    Row(27, TableIndex.MemberRef, EditAndContinueOperation.Default),
                    Row(28, TableIndex.MemberRef, EditAndContinueOperation.Default),
                    Row(14, TableIndex.TypeRef, EditAndContinueOperation.Default),
                    Row(15, TableIndex.TypeRef, EditAndContinueOperation.Default),
                    Row(16, TableIndex.TypeRef, EditAndContinueOperation.Default),
                    Row(17, TableIndex.TypeRef, EditAndContinueOperation.Default),
                    Row(18, TableIndex.TypeRef, EditAndContinueOperation.Default),
                    Row(19, TableIndex.TypeRef, EditAndContinueOperation.Default),
                    Row(20, TableIndex.TypeRef, EditAndContinueOperation.Default),
                    Row(21, TableIndex.TypeRef, EditAndContinueOperation.Default),
                    Row(22, TableIndex.TypeRef, EditAndContinueOperation.Default),
                    Row(23, TableIndex.TypeRef, EditAndContinueOperation.Default),
                    Row(24, TableIndex.TypeRef, EditAndContinueOperation.Default),
                    Row(3, TableIndex.TypeSpec, EditAndContinueOperation.Default),
                    Row(4, TableIndex.TypeSpec, EditAndContinueOperation.Default),
                    Row(6, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                    Row(7, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                    Row(8, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                    Row(9, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                    Row(10, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                    Row(11, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                    Row(4, TableIndex.TypeDef, EditAndContinueOperation.Default),
                    Row(2, TableIndex.PropertyMap, EditAndContinueOperation.Default),
                    Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                    Row(4, TableIndex.Field, EditAndContinueOperation.Default),
                    Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                    Row(5, TableIndex.Field, EditAndContinueOperation.Default),
                    Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                    Row(6, TableIndex.Field, EditAndContinueOperation.Default),
                    Row(2, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                    Row(12, TableIndex.MethodDef, EditAndContinueOperation.Default),
                    Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                    Row(13, TableIndex.MethodDef, EditAndContinueOperation.Default),
                    Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                    Row(14, TableIndex.MethodDef, EditAndContinueOperation.Default),
                    Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                    Row(15, TableIndex.MethodDef, EditAndContinueOperation.Default),
                    Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                    Row(16, TableIndex.MethodDef, EditAndContinueOperation.Default),
                    Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                    Row(17, TableIndex.MethodDef, EditAndContinueOperation.Default),
                    Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                    Row(18, TableIndex.MethodDef, EditAndContinueOperation.Default),
                    Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                    Row(19, TableIndex.MethodDef, EditAndContinueOperation.Default),
                    Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                    Row(20, TableIndex.MethodDef, EditAndContinueOperation.Default),
                    Row(2, TableIndex.PropertyMap, EditAndContinueOperation.AddProperty),
                    Row(3, TableIndex.Property, EditAndContinueOperation.Default),
                    Row(2, TableIndex.PropertyMap, EditAndContinueOperation.AddProperty),
                    Row(4, TableIndex.Property, EditAndContinueOperation.Default),
                    Row(13, TableIndex.MethodDef, EditAndContinueOperation.AddParameter),
                    Row(2, TableIndex.Param, EditAndContinueOperation.Default),
                    Row(11, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(12, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(13, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(14, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(15, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(16, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(17, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(18, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(3, TableIndex.MethodSemantics, EditAndContinueOperation.Default),
                    Row(4, TableIndex.MethodSemantics, EditAndContinueOperation.Default),
                    Row(8, TableIndex.MethodImpl, EditAndContinueOperation.Default),
                    Row(9, TableIndex.MethodImpl, EditAndContinueOperation.Default),
                    Row(10, TableIndex.MethodImpl, EditAndContinueOperation.Default),
                    Row(11, TableIndex.MethodImpl, EditAndContinueOperation.Default),
                    Row(12, TableIndex.MethodImpl, EditAndContinueOperation.Default),
                    Row(13, TableIndex.MethodImpl, EditAndContinueOperation.Default),
                    Row(14, TableIndex.MethodImpl, EditAndContinueOperation.Default),
                    Row(2, TableIndex.NestedClass, EditAndContinueOperation.Default),
                    Row(6, TableIndex.InterfaceImpl, EditAndContinueOperation.Default),
                    Row(7, TableIndex.InterfaceImpl, EditAndContinueOperation.Default),
                    Row(8, TableIndex.InterfaceImpl, EditAndContinueOperation.Default),
                    Row(9, TableIndex.InterfaceImpl, EditAndContinueOperation.Default),
                    Row(10, TableIndex.InterfaceImpl, EditAndContinueOperation.Default));
                CheckEncMap(reader1,
                    Handle(14, TableIndex.TypeRef),
                    Handle(15, TableIndex.TypeRef),
                    Handle(16, TableIndex.TypeRef),
                    Handle(17, TableIndex.TypeRef),
                    Handle(18, TableIndex.TypeRef),
                    Handle(19, TableIndex.TypeRef),
                    Handle(20, TableIndex.TypeRef),
                    Handle(21, TableIndex.TypeRef),
                    Handle(22, TableIndex.TypeRef),
                    Handle(23, TableIndex.TypeRef),
                    Handle(24, TableIndex.TypeRef),
                    Handle(4, TableIndex.TypeDef),
                    Handle(4, TableIndex.Field),
                    Handle(5, TableIndex.Field),
                    Handle(6, TableIndex.Field),
                    Handle(12, TableIndex.MethodDef),
                    Handle(13, TableIndex.MethodDef),
                    Handle(14, TableIndex.MethodDef),
                    Handle(15, TableIndex.MethodDef),
                    Handle(16, TableIndex.MethodDef),
                    Handle(17, TableIndex.MethodDef),
                    Handle(18, TableIndex.MethodDef),
                    Handle(19, TableIndex.MethodDef),
                    Handle(20, TableIndex.MethodDef),
                    Handle(2, TableIndex.Param),
                    Handle(6, TableIndex.InterfaceImpl),
                    Handle(7, TableIndex.InterfaceImpl),
                    Handle(8, TableIndex.InterfaceImpl),
                    Handle(9, TableIndex.InterfaceImpl),
                    Handle(10, TableIndex.InterfaceImpl),
                    Handle(16, TableIndex.MemberRef),
                    Handle(17, TableIndex.MemberRef),
                    Handle(18, TableIndex.MemberRef),
                    Handle(19, TableIndex.MemberRef),
                    Handle(20, TableIndex.MemberRef),
                    Handle(21, TableIndex.MemberRef),
                    Handle(22, TableIndex.MemberRef),
                    Handle(23, TableIndex.MemberRef),
                    Handle(24, TableIndex.MemberRef),
                    Handle(25, TableIndex.MemberRef),
                    Handle(26, TableIndex.MemberRef),
                    Handle(27, TableIndex.MemberRef),
                    Handle(28, TableIndex.MemberRef),
                    Handle(11, TableIndex.CustomAttribute),
                    Handle(12, TableIndex.CustomAttribute),
                    Handle(13, TableIndex.CustomAttribute),
                    Handle(14, TableIndex.CustomAttribute),
                    Handle(15, TableIndex.CustomAttribute),
                    Handle(16, TableIndex.CustomAttribute),
                    Handle(17, TableIndex.CustomAttribute),
                    Handle(18, TableIndex.CustomAttribute),
                    Handle(6, TableIndex.StandAloneSig),
                    Handle(7, TableIndex.StandAloneSig),
                    Handle(8, TableIndex.StandAloneSig),
                    Handle(9, TableIndex.StandAloneSig),
                    Handle(10, TableIndex.StandAloneSig),
                    Handle(11, TableIndex.StandAloneSig),
                    Handle(2, TableIndex.PropertyMap),
                    Handle(3, TableIndex.Property),
                    Handle(4, TableIndex.Property),
                    Handle(3, TableIndex.MethodSemantics),
                    Handle(4, TableIndex.MethodSemantics),
                    Handle(8, TableIndex.MethodImpl),
                    Handle(9, TableIndex.MethodImpl),
                    Handle(10, TableIndex.MethodImpl),
                    Handle(11, TableIndex.MethodImpl),
                    Handle(12, TableIndex.MethodImpl),
                    Handle(13, TableIndex.MethodImpl),
                    Handle(14, TableIndex.MethodImpl),
                    Handle(3, TableIndex.TypeSpec),
                    Handle(4, TableIndex.TypeSpec),
                    Handle(2, TableIndex.AssemblyRef),
                    Handle(2, TableIndex.NestedClass));
            }

            string actualPdb1 = PdbToXmlConverter.DeltaPdbToXml(diff1.Pdb, Enumerable.Range(1, 100).Select(rid => 0x06000000U | (uint)rid));

            // TODO (tomat): bug in SymWriter.
            // The PDB is missing debug info for G method. The info is written to the PDB but the native SymWriter 
            // seems to ignore it. If another method is added to the class all information is written. 
            // This happens regardless of whether we emit just the delta or full PDB.

            string expectedPdb1 = @"
<symbols>
  <files>
    <file id=""1"" name=""a.cs"" language=""3f5162f8-07c6-11d3-9053-00c04fa302a1"" languageVendor=""994b45c4-e6e9-11d2-903f-00c04fa302a1"" documentType=""5a869d0b-6611-11d3-bd2a-0000f80849bd"" />
  </files>
  <methods>
    <method token=""0x600000f"">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""5"">
        <entry il_offset=""0x0"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""1"" />
        <entry il_offset=""0x21"" start_row=""9"" start_column=""5"" end_row=""9"" end_column=""6"" file_ref=""1"" />
        <entry il_offset=""0x22"" start_row=""10"" start_column=""9"" end_row=""10"" end_column=""24"" file_ref=""1"" />
        <entry il_offset=""0x34"" hidden=""true"" start_row=""16707566"" start_column=""0"" end_row=""16707566"" end_column=""0"" file_ref=""1"" />
        <entry il_offset=""0x3b"" start_row=""11"" start_column=""5"" end_row=""11"" end_column=""6"" file_ref=""1"" />
      </sequencepoints>
      <locals>
        <local name=""cachedState"" il_index=""0"" il_start=""0x0"" il_end=""0x7"" attributes=""0"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x3f"">
        <namespace name=""System.Collections.Generic"" />
        <scope startOffset=""0x0"" endOffset=""0x7"">
          <local name=""cachedState"" il_index=""0"" il_start=""0x0"" il_end=""0x7"" attributes=""0"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>";

            AssertXmlEqual(expectedPdb1, actualPdb1);

            var method1M = compilation1.GetMember<MethodSymbol>("C.M");
            var method2M = compilation2.GetMember<MethodSymbol>("C.M");
// TODO: Not preserving iterator local.
#if false
            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method1M, method2M, GetLocalMap(method2M, method1M), preserveLocalVariables: true)));

            var method2G = compilation2.GetMember<MethodSymbol>("C.G");
            var method3G = compilation3.GetMember<MethodSymbol>("C.G");
            var diff3 = compilation3.EmitDifference(
                diff2.NextGeneration,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method2G, method3G, GetLocalMap(method3G, method2G), preserveLocalVariables: true)));
            diff3.VerifyIL("C.G",
@"{
  // Code size       12 (0xc)
  .maxstack  1
  .locals init (C.<G>d__0 V_0,
  System.Collections.Generic.IEnumerable<int> V_1)
  IL_0000:  ldc.i4.s   -2
  IL_0002:  newobj     ""C.<G>d__0..ctor(int)""
  IL_0007:  stloc.0
  IL_0008:  ldloc.0
  IL_0009:  stloc.1
  IL_000a:  ldloc.1
  IL_000b:  ret
}");
#endif
        }

        [Fact]
        public void Lambda()
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
            var compilation0 = CreateCompilationWithMscorlib(source0, compOptions: TestOptions.UnoptimizedDll);
            var compilation1 = CreateCompilationWithMscorlib(source1, compOptions: TestOptions.UnoptimizedDll);
            var bytes0 = compilation0.EmitToArray(debug: true);
            var generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), EmptyLocalsProvider);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, compilation0.GetMember<MethodSymbol>("C.F"), compilation1.GetMember<MethodSymbol>("C.F"))));

            using (var md1 = diff1.GetMetadata())
            {
                var reader1 = md1.Reader;
                CheckEncLog(reader1,
                    Row(2, TableIndex.AssemblyRef, EditAndContinueOperation.Default),
                    Row(4, TableIndex.MemberRef, EditAndContinueOperation.Default),
                    Row(5, TableIndex.MemberRef, EditAndContinueOperation.Default),
                    Row(7, TableIndex.TypeRef, EditAndContinueOperation.Default),
                    Row(8, TableIndex.TypeRef, EditAndContinueOperation.Default),
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
                    Row(3, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(1, TableIndex.NestedClass, EditAndContinueOperation.Default));
                CheckEncMap(reader1,
                    Handle(7, TableIndex.TypeRef),
                    Handle(8, TableIndex.TypeRef),
                    Handle(4, TableIndex.TypeDef),
                    Handle(1, TableIndex.Field),
                    Handle(5, TableIndex.MethodDef),
                    Handle(7, TableIndex.MethodDef),
                    Handle(8, TableIndex.MethodDef),
                    Handle(4, TableIndex.MemberRef),
                    Handle(5, TableIndex.MemberRef),
                    Handle(3, TableIndex.CustomAttribute),
                    Handle(2, TableIndex.StandAloneSig),
                    Handle(3, TableIndex.StandAloneSig),
                    Handle(2, TableIndex.AssemblyRef),
                    Handle(1, TableIndex.NestedClass));
            }
        }

        [Fact]
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
            var compilation0 = CreateCompilationWithMscorlib(Parse(source0, "a.cs"), compOptions: TestOptions.UnoptimizedDll);
            var compilation1 = CreateCompilationWithMscorlib(Parse(source1, "a.cs"), compOptions: TestOptions.UnoptimizedDll);
            var bytes0 = compilation0.EmitToArray(debug: true);

            var generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), token => ImmutableArray.Create("a"));

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, compilation0.GetMember("C.M"), compilation1.GetMember("C.M"))));

            using (var md1 = diff1.GetMetadata())
            {
                var reader1 = md1.Reader;
                CheckEncLog(reader1,
                    Row(2, TableIndex.AssemblyRef, EditAndContinueOperation.Default),
                    Row(10, TableIndex.TypeRef, EditAndContinueOperation.Default),
                    Row(11, TableIndex.TypeRef, EditAndContinueOperation.Default),
                    Row(2, TableIndex.TypeSpec, EditAndContinueOperation.Default),
                    Row(2, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                    Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default));
                CheckEncMap(reader1,
                    Handle(10, TableIndex.TypeRef),
                    Handle(11, TableIndex.TypeRef),
                    Handle(1, TableIndex.MethodDef),
                    Handle(2, TableIndex.StandAloneSig),
                    Handle(2, TableIndex.TypeSpec),
                    Handle(2, TableIndex.AssemblyRef));
            }

            diff1.VerifyIL(
@"{
  // Code size       25 (0x19)
  .maxstack  4
  IL_0000:  nop
  IL_0001:  ldc.i4.4
  IL_0002:  newarr     0x0100000B
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

            diff1.VerifyPdb(new[] { 0x06000001U },
@"<symbols>
  <files>
    <file id=""1"" name=""a.cs"" language=""3f5162f8-07c6-11d3-9053-00c04fa302a1"" languageVendor=""994b45c4-e6e9-11d2-903f-00c04fa302a1"" documentType=""5a869d0b-6611-11d3-bd2a-0000f80849bd"" />
  </files>
  <methods>
    <method token=""0x6000001"">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""3"">
        <entry il_offset=""0x0"" start_row=""5"" start_column=""5"" end_row=""5"" end_column=""6"" file_ref=""1"" />
        <entry il_offset=""0x1"" start_row=""6"" start_column=""9"" end_row=""6"" end_column=""40"" file_ref=""1"" />
        <entry il_offset=""0x18"" start_row=""7"" start_column=""5"" end_row=""7"" end_column=""6"" file_ref=""1"" />
      </sequencepoints>
      <locals>
        <local name=""a"" il_index=""0"" il_start=""0x0"" il_end=""0x19"" attributes=""0"" />
      </locals>
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
            var compilation0 = CreateCompilationWithMscorlib(source0, compOptions: TestOptions.UnoptimizedDll);
            var compilation1 = CreateCompilationWithMscorlib(source1, compOptions: TestOptions.UnoptimizedDll);
            var bytes0 = compilation0.EmitToArray(debug: true);
            var generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), EmptyLocalsProvider);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Insert, null, compilation1.GetMember<MethodSymbol>("C.puts"))));

            using (var md1 = diff1.GetMetadata())
            {
                var reader1 = md1.Reader;
                CheckEncLog(reader1,
                    Row(2, TableIndex.AssemblyRef, EditAndContinueOperation.Default),
                    Row(2, TableIndex.ModuleRef, EditAndContinueOperation.Default),
                    Row(4, TableIndex.TypeRef, EditAndContinueOperation.Default),
                    Row(2, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                    Row(3, TableIndex.MethodDef, EditAndContinueOperation.Default),
                    Row(3, TableIndex.MethodDef, EditAndContinueOperation.AddParameter),
                    Row(1, TableIndex.Param, EditAndContinueOperation.Default),
                    Row(2, TableIndex.ImplMap, EditAndContinueOperation.Default));
                CheckEncMap(reader1,
                    Handle(4, TableIndex.TypeRef),
                    Handle(3, TableIndex.MethodDef),
                    Handle(1, TableIndex.Param),
                    Handle(2, TableIndex.ModuleRef),
                    Handle(2, TableIndex.ImplMap),
                    Handle(2, TableIndex.AssemblyRef));
            }
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
            var compilation0 = CreateCompilationWithMscorlib(source0, compOptions: TestOptions.UnoptimizedDll);
            var compilation1 = CreateCompilationWithMscorlib(source1, compOptions: TestOptions.UnoptimizedDll);
            var bytes0 = compilation0.EmitToArray(debug: true);
            var generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), EmptyLocalsProvider);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Insert, null, compilation1.GetMember<NamedTypeSymbol>("B"))));

            using (var md1 = diff1.GetMetadata())
            {
                var reader1 = md1.Reader;
                CheckEncLog(reader1,
                    Row(2, TableIndex.AssemblyRef, EditAndContinueOperation.Default),
                    Row(4, TableIndex.MemberRef, EditAndContinueOperation.Default),
                    Row(4, TableIndex.TypeRef, EditAndContinueOperation.Default),
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
                    Handle(4, TableIndex.TypeRef),
                    Handle(3, TableIndex.TypeDef),
                    Handle(3, TableIndex.Field),
                    Handle(4, TableIndex.Field),
                    Handle(2, TableIndex.MethodDef),
                    Handle(4, TableIndex.MemberRef),
                    Handle(2, TableIndex.ClassLayout),
                    Handle(3, TableIndex.FieldLayout),
                    Handle(4, TableIndex.FieldLayout),
                    Handle(2, TableIndex.AssemblyRef));
            }
        }

        [Fact]
        public void NamespacesAndOverloads()
        {
            var compilation0 = CreateCompilationWithMscorlib(compOptions: TestOptions.UnoptimizedDll, text:
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

            var bytes0 = compilation0.EmitToArray(debug: true);
            var generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), EmptyLocalsProvider);

            var compilation1 = CreateCompilationWithMscorlib(compOptions: TestOptions.UnoptimizedDll, text:
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

            var compilation2 = CreateCompilationWithMscorlib(compOptions: TestOptions.UnoptimizedDll, text:
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
            var options = TestOptions.UnoptimizedDll.WithAllowUnsafe(true);
            var compilation0 = CreateCompilationWithMscorlib(compOptions: options, references: new[] { SystemCoreRef, CSharpRef }, text: source);
            var bytes0 = compilation0.EmitToArray(debug: true);
            var generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), EmptyLocalsProvider);

            var n = compilation0.GetMembers("C.M").Length;
            Assert.Equal(n, 14);

            //static void M(A<B>.B<object> a)
            //{
            //    M(a);
            //    M((A<B>.B<B>)null);
            //}
            var compilation1 = CreateCompilationWithMscorlib(compOptions: options, references: new[] { SystemCoreRef, CSharpRef }, text: source);
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
            var compilation2 = CreateCompilationWithMscorlib(compOptions: options, references: new[] { SystemCoreRef, CSharpRef }, text: source);
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
            var compilation3 = CreateCompilationWithMscorlib(compOptions: options, references: new[] { SystemCoreRef, CSharpRef }, text: source);
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
  IL_0009:  newobj     0x0A000015
  IL_000e:  call       0x06000005
  IL_0013:  nop
  IL_0014:  ret
}");

            //static void M(Nullable<A<B>> a)
            //{
            //    M(a);
            //    M(a.Value);
            //}
            var compilation4 = CreateCompilationWithMscorlib(compOptions: options, references: new[] { SystemCoreRef, CSharpRef }, text: source);
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
  IL_000a:  call       0x0A000016
  IL_000f:  call       0x06000004
  IL_0014:  nop
  IL_0015:  ret
}");

            //unsafe static void M(int* p)
            //{
            //    M(p);
            //    M((byte*)p);
            //}
            var compilation5 = CreateCompilationWithMscorlib(compOptions: options, references: new[] { SystemCoreRef, CSharpRef }, text: source);
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
            var compilation6 = CreateCompilationWithMscorlib(compOptions: options, references: new[] { SystemCoreRef, CSharpRef }, text: source);
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
            var compilation7 = CreateCompilationWithMscorlib(compOptions: options, references: new[] { SystemCoreRef, CSharpRef }, text: source);
            var diff7 = compilation7.EmitDifference(
                diff6.NextGeneration,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, compilation6.GetMembers("C.M")[6], compilation7.GetMembers("C.M")[6])));

            diff7.VerifyIL(
@"{
  // Code size       16 (0x10)
  .maxstack  8
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  call       0x06000008
  IL_0007:  nop
  IL_0008:  ldarg.0
  IL_0009:  call       0x06000009
  IL_000e:  nop
  IL_000f:  ret
}");

            //static void M(object[][] b)
            //{
            //    M(b);
            //    M((B[][])b);
            //}
            var compilation8 = CreateCompilationWithMscorlib(compOptions: options, references: new[] { SystemCoreRef, CSharpRef }, text: source);
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
            var compilation9 = CreateCompilationWithMscorlib(compOptions: options, references: new[] { SystemCoreRef, CSharpRef }, text: source);
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
            var compilation10 = CreateCompilationWithMscorlib(compOptions: options, references: new[] { SystemCoreRef, CSharpRef }, text: source);
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
            compilation = CreateCompilationWithMscorlib(compOptions: options, references: new[] { SystemCoreRef, CSharpRef }, text: source);
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
            compilation = CreateCompilationWithMscorlib(compOptions: options, references: new[] { SystemCoreRef, CSharpRef }, text: source);
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
            var compilation11 = CreateCompilationWithMscorlib(compOptions: options, references: new[] { SystemCoreRef, CSharpRef }, text: source);
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
            var compilation12 = CreateCompilationWithMscorlib(compOptions: options, references: new[] { SystemCoreRef, CSharpRef }, text: source);
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
            var compilation0 = CreateCompilationWithMscorlib(source0, compOptions: TestOptions.UnoptimizedExe);
            var compilation1 = CreateCompilationWithMscorlib(source1, compOptions: TestOptions.UnoptimizedExe);

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(debug: true, testData: testData0);
            var methodData0 = testData0.GetMethodData("C.Main");
            var method0 = compilation0.GetMember<MethodSymbol>("C.Main");
            var generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), m => GetLocalNames(methodData0));
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
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetLocalMap(method1, method0), preserveLocalVariables: true)));

            diff1.TestData.GetMethodData("C.Main").VerifyIL(
            // Should be ".locals init (S1 V_0, //..." (with S1 type) to ensure alignment
            // of V_1. However this issue does not seem to affect runtime behavior.
 @"{
  // Code size       22 (0x16)
  .maxstack  2
  .locals init (S1 V_0,
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
            var compilation0 = CreateCompilationWithMscorlib(source, compOptions: TestOptions.UnoptimizedDll);
            var compilation1 = CreateCompilationWithMscorlib(source, compOptions: TestOptions.UnoptimizedDll);

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(debug: true, testData: testData0);
            using (var md0 = ModuleMetadata.CreateFromImage(bytes0))
            {
                var reader0 = md0.MetadataReader;
                var typeNames = new[] { reader0 }.GetStrings(reader0.GetTypeDefNames());
                Assert.NotNull(typeNames.FirstOrDefault(n => n.StartsWith("<PrivateImplementationDetails>")));
            }

            var methodData0 = testData0.GetMethodData("C.M");
            var method0 = compilation0.GetMember<MethodSymbol>("C.M");
            var generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), m => GetLocalNames(methodData0));

            var method1 = compilation1.GetMember<MethodSymbol>("C.M");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetLocalMap(method1, method0), preserveLocalVariables: true)));

            diff1.VerifyIL("C.M",
@"{
  // Code size       22 (0x16)
  .maxstack  3
  .locals init (int V_0,
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

        [WorkItem(780989, "DevDiv")]
        [WorkItem(829353, "DevDiv")]
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
            var compilation0 = CreateCompilationWithMscorlib(source0, compOptions: TestOptions.UnoptimizedDll.WithDebugInformationKind(DebugInformationKind.Full));
            var compilation1 = CreateCompilationWithMscorlib(source1, compOptions: TestOptions.UnoptimizedDll.WithDebugInformationKind(DebugInformationKind.Full));
            var compilation2 = CreateCompilationWithMscorlib(source2, compOptions: TestOptions.UnoptimizedDll.WithDebugInformationKind(DebugInformationKind.Full));

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(debug: true, testData: testData0, mvid: Guid.Parse("a2f225f6-b5b9-40f6-bb78-4479a0c55a9b"));
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
  IL_0008:  ldtoken    ""<PrivateImplementationDetails>{a2f225f6-b5b9-40f6-bb78-4479a0c55a9b}.__StaticArrayInitTypeSize=12 <PrivateImplementationDetails>{a2f225f6-b5b9-40f6-bb78-4479a0c55a9b}.$$method0x6000001-0""
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
            var generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), m => GetLocalNames(methodData0));

            var method1 = compilation1.GetMember<MethodSymbol>("C.M");

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetLocalMap(method1, method0), preserveLocalVariables: true)));

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
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method1, method2, GetLocalMap(method2, method1), preserveLocalVariables: true)));

            diff2.VerifyIL("C.M",
@"{
  // Code size       48 (0x30)
  .maxstack  4
  .locals init (int[] V_0,
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

        [WorkItem(780989, "DevDiv")]
        [WorkItem(829353, "DevDiv")]
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
            var compilation0 = CreateCompilationWithMscorlib(source0, compOptions: TestOptions.UnoptimizedDll.WithDebugInformationKind(DebugInformationKind.Full));
            var compilation1 = CreateCompilationWithMscorlib(source1, compOptions: TestOptions.UnoptimizedDll.WithDebugInformationKind(DebugInformationKind.Full));
            var compilation2 = CreateCompilationWithMscorlib(source2, compOptions: TestOptions.UnoptimizedDll.WithDebugInformationKind(DebugInformationKind.Full));

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(debug: true, testData: testData0, mvid: Guid.Parse("a2f225f6-b5b9-40f6-bb78-4479a0c55a9c"));
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
        [WorkItem(834086, "DevDiv")]
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
            const string ComputeStringHashName = "$$method0x6000001-ComputeStringHash";
            var compilation0 = CreateCompilationWithMscorlib(source, compOptions: TestOptions.UnoptimizedDll.WithDebugInformationKind(DebugInformationKind.Full));
            var compilation1 = CreateCompilationWithMscorlib(source, compOptions: TestOptions.UnoptimizedDll.WithDebugInformationKind(DebugInformationKind.Full));

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(debug: true, testData: testData0);
            var methodData0 = testData0.GetMethodData("C.F");
            var method0 = compilation0.GetMember<MethodSymbol>("C.F");
            var generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), m => GetLocalNames(methodData0));

            // Should have generated call to ComputeStringHash and
            // added the method to <PrivateImplementationDetails>.
            var actualIL0 = methodData0.GetMethodIL();
            Assert.True(actualIL0.Contains(ComputeStringHashName));

            using (var md0 = ModuleMetadata.CreateFromImage(bytes0))
            {
                var reader0 = md0.MetadataReader;
                CheckNames(reader0, reader0.GetMethodDefNames(), "F", ".ctor", ComputeStringHashName);

                var method1 = compilation1.GetMember<MethodSymbol>("C.F");
                var diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetLocalMap(method1, method0), preserveLocalVariables: true)));

                // Should not have generated call to ComputeStringHash nor
                // added the method to <PrivateImplementationDetails>.
                var actualIL1 = diff1.TestData.GetMethodData("C.F").GetMethodIL();
                Assert.False(actualIL1.Contains(ComputeStringHashName));

                using (var md1 = diff1.GetMetadata())
                {
                    var reader1 = md1.Reader;
                    var readers = new[] { reader0, reader1 };
                    CheckNames(readers, reader1.GetMethodDefNames(), "F");
                }
            }
        }

        /// <summary>
        /// Unique ids should not conflict with ids
        /// from previous generation.
        /// </summary>
        [Fact(Skip = "TODO")]
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
            var compilation0 = CreateCompilationWithMscorlib(source0, compOptions: TestOptions.UnoptimizedDll.WithDebugInformationKind(DebugInformationKind.Full));
            var compilation1 = CreateCompilationWithMscorlib(source1, compOptions: TestOptions.UnoptimizedDll.WithDebugInformationKind(DebugInformationKind.Full));
            var compilation2 = CreateCompilationWithMscorlib(source2, compOptions: TestOptions.UnoptimizedDll.WithDebugInformationKind(DebugInformationKind.Full));

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(debug: true, testData: testData0);
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
            var compilation0 = CreateCompilationWithMscorlib(source0, compOptions: TestOptions.UnoptimizedDll);
            var compilation1 = CreateCompilationWithMscorlib(source1, compOptions: TestOptions.UnoptimizedDll);

            // Verify full metadata contains expected rows.
            var bytes0 = compilation0.EmitToArray(debug: true);
            using (var md0 = ModuleMetadata.CreateFromImage(bytes0))
            {
                var reader0 = md0.MetadataReader;
                CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "C");
                CheckNames(reader0, reader0.GetMethodDefNames(), "F", "G", ".ctor");
                CheckNames(reader0, reader0.GetMemberRefNames(), ".ctor", ".ctor", "WriteLine", ".ctor");

                var generation0 = EmitBaseline.CreateInitialBaseline(md0, EmptyLocalsProvider);
                var method0 = compilation0.GetMember<MethodSymbol>("C.G");
                var method1 = compilation1.GetMember<MethodSymbol>("C.G");

                var diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(new SemanticEdit(
                        SemanticEditKind.Update,
                        method0,
                        method1,
                        GetLocalMap(method1, method0),
                        preserveLocalVariables: true)));

                // "Write" should be included in string table, but "WriteLine" should not.
                Assert.True(diff1.MetadataBlob.IsIncluded("Write"));
                Assert.False(diff1.MetadataBlob.IsIncluded("WriteLine"));
            }
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
            var compilation0 = CreateCompilationWithMscorlib(source0, compOptions: TestOptions.UnoptimizedDll);
            var compilation1 = CreateCompilationWithMscorlib(source1, compOptions: TestOptions.UnoptimizedDll);
            var compilation2 = CreateCompilationWithMscorlib(source2, compOptions: TestOptions.UnoptimizedDll);
            var compilation3 = CreateCompilationWithMscorlib(source3, compOptions: TestOptions.UnoptimizedDll);

            var method0 = compilation0.GetMember<MethodSymbol>("B.M");
            var methodN = compilation0.GetMember<MethodSymbol>("B.N");

            // Verify full metadata contains expected rows.
            LocalVariableNameProvider getLocalNames = m =>
                {
                    switch (m)
                    {
                        case 3u:
                            return GetLocalNames(method0);
                        case 4u:
                            return GetLocalNames(methodN);
                        default:
                            return default(ImmutableArray<string>);
                    }
                };

            var bytes0 = compilation0.EmitToArray(debug: true);
            var generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), getLocalNames);

            #region Gen1 

            var method1 = compilation1.GetMember<MethodSymbol>("B.M");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetLocalMap(method1, method0), preserveLocalVariables: true)));

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
            diff1.VerifyPdb(new[] { 0x06000001U, 0x06000002U, 0x06000003U, 0x06000004U }, @"
<symbols>
  <methods>
    <method token=""0x6000003"">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""7"">
        <entry il_offset=""0x0"" start_row=""9"" start_column=""5"" end_row=""9"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""10"" start_column=""9"" end_row=""10"" end_column=""19"" file_ref=""0"" />
        <entry il_offset=""0x7"" start_row=""11"" start_column=""9"" end_row=""11"" end_column=""22"" file_ref=""0"" />
        <entry il_offset=""0xd"" start_row=""12"" start_column=""9"" end_row=""12"" end_column=""24"" file_ref=""0"" />
        <entry il_offset=""0x14"" start_row=""13"" start_column=""9"" end_row=""13"" end_column=""14"" file_ref=""0"" />
        <entry il_offset=""0x1c"" start_row=""14"" start_column=""9"" end_row=""14"" end_column=""14"" file_ref=""0"" />
        <entry il_offset=""0x23"" start_row=""15"" start_column=""5"" end_row=""15"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""z"" il_index=""3"" il_start=""0x0"" il_end=""0x24"" attributes=""0"" />
        <local name=""y"" il_index=""1"" il_start=""0x0"" il_end=""0x24"" attributes=""0"" />
        <local name=""w"" il_index=""4"" il_start=""0x0"" il_end=""0x24"" attributes=""0"" />
      </locals>
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
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method1, method2, GetLocalMap(method2, method1), preserveLocalVariables: true)));

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

            diff2.VerifyPdb(new[] { 0x06000001U, 0x06000002U, 0x06000003U, 0x06000004U }, @"
<symbols>
  <methods>
    <method token=""0x6000003"">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""6"">
        <entry il_offset=""0x0"" start_row=""9"" start_column=""5"" end_row=""9"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""10"" start_column=""9"" end_row=""10"" end_column=""24"" file_ref=""0"" />
        <entry il_offset=""0x8"" start_row=""11"" start_column=""9"" end_row=""11"" end_column=""19"" file_ref=""0"" />
        <entry il_offset=""0xe"" start_row=""12"" start_column=""9"" end_row=""12"" end_column=""14"" file_ref=""0"" />
        <entry il_offset=""0x16"" start_row=""13"" start_column=""9"" end_row=""13"" end_column=""14"" file_ref=""0"" />
        <entry il_offset=""0x1d"" start_row=""14"" start_column=""5"" end_row=""14"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""x"" il_index=""5"" il_start=""0x0"" il_end=""0x1e"" attributes=""0"" />
        <local name=""z"" il_index=""3"" il_start=""0x0"" il_end=""0x1e"" attributes=""0"" />
      </locals>
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
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method2, method3, GetLocalMap(method3, method2), preserveLocalVariables: true)));

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
            diff3.VerifyPdb(new[] { 0x06000001U, 0x06000002U, 0x06000003U, 0x06000004U }, @"
<symbols>
  <methods>
    <method token=""0x6000004"">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""6"">
        <entry il_offset=""0x0"" start_row=""16"" start_column=""5"" end_row=""16"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""17"" start_column=""9"" end_row=""17"" end_column=""24"" file_ref=""0"" />
        <entry il_offset=""0x7"" start_row=""18"" start_column=""9"" end_row=""18"" end_column=""24"" file_ref=""0"" />
        <entry il_offset=""0xd"" start_row=""19"" start_column=""9"" end_row=""19"" end_column=""14"" file_ref=""0"" />
        <entry il_offset=""0x14"" start_row=""20"" start_column=""9"" end_row=""20"" end_column=""14"" file_ref=""0"" />
        <entry il_offset=""0x1b"" start_row=""21"" start_column=""5"" end_row=""21"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""c"" il_index=""2"" il_start=""0x0"" il_end=""0x1c"" attributes=""0"" />
        <local name=""b"" il_index=""1"" il_start=""0x0"" il_end=""0x1c"" attributes=""0"" />
      </locals>
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
            var compilation0 = CreateCompilationWithMscorlib(source0, compOptions: TestOptions.UnoptimizedDll);
            var compilation1 = CreateCompilationWithMscorlib(source1, compOptions: TestOptions.UnoptimizedDll);
            var compilation2 = CreateCompilationWithMscorlib(source2, compOptions: TestOptions.UnoptimizedDll);

            var bytes0 = compilation0.EmitToArray(debug: true);
            var generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), EmptyLocalsProvider);

            var method1 = compilation1.GetMember<MethodSymbol>("C.M");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Insert, null, method1, null, preserveLocalVariables: true)));

            var method2 = compilation2.GetMember<MethodSymbol>("C.M");
            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method1, method2, GetLocalMap(method2, method1), preserveLocalVariables: true)));
            diff2.VerifyIL("C.M",
@"{
  // Code size       10 (0xa)
  .maxstack  1
  .locals init (object V_0,
  string V_1, //b
  int V_2) //a
  IL_0000:  nop
  IL_0001:  ldc.i4.1
  IL_0002:  stloc.2
  IL_0003:  ldsfld     ""string string.Empty""
  IL_0008:  stloc.1
  IL_0009:  ret
}");
            diff2.VerifyPdb(new[] { 0x06000002U },
@"<?xml version=""1.0"" encoding=""utf-16""?>
<symbols>
  <methods>
    <method token=""0x6000002"">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""4"">
        <entry il_offset=""0x0"" start_row=""4"" start_column=""5"" end_row=""4"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""5"" start_column=""9"" end_row=""5"" end_column=""19"" file_ref=""0"" />
        <entry il_offset=""0x3"" start_row=""6"" start_column=""9"" end_row=""6"" end_column=""30"" file_ref=""0"" />
        <entry il_offset=""0x9"" start_row=""7"" start_column=""5"" end_row=""7"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals>
        <local name=""a"" il_index=""2"" il_start=""0x0"" il_end=""0xa"" attributes=""0"" />
        <local name=""b"" il_index=""1"" il_start=""0x0"" il_end=""0xa"" attributes=""0"" />
      </locals>
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
        [WorkItem(843320, "DevDiv")]
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
            var compilation0 = CreateCompilationWithMscorlib(source0, compOptions: TestOptions.UnoptimizedDll);
            var compilation1 = CreateCompilationWithMscorlib(source1, compOptions: TestOptions.UnoptimizedDll);
            var method0 = compilation0.GetMember<MethodSymbol>("C.Main");
            var method1 = compilation1.GetMember<MethodSymbol>("C.Main");
            var bytes0 = compilation0.EmitToArray(debug: true);
            var generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), m => GetLocalNames(method0));
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetLocalMap(method1, method0), preserveLocalVariables: true)));
            diff1.VerifyIL("C.Main",
@"{
  // Code size       17 (0x11)
  .maxstack  1
  .locals init (bool V_0,
  bool V_1,
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
            var compilation0 = CreateCompilationWithMscorlib(source, compOptions: TestOptions.UnoptimizedDll.WithDebugInformationKind(DebugInformationKind.Full));
            var compilation1 = CreateCompilationWithMscorlib(source, compOptions: TestOptions.UnoptimizedDll.WithDebugInformationKind(DebugInformationKind.Full));

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(debug: true, testData: testData0);
            var method0 = compilation0.GetMember<MethodSymbol>("C.M");
            var generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), m => GetLocalNames(testData0.GetMethodData("C.M")));

            var method1 = compilation1.GetMember<MethodSymbol>("C.M");
            var diff1a = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, preserveLocalVariables: false)));

            diff1a.VerifyIL("C.M",
@"{
  // Code size       54 (0x36)
  .maxstack  2
  .locals init (System.IDisposable V_0, //CS$3$0000
  bool V_1,
  System.IDisposable V_2) //x
  IL_0000:  nop
  IL_0001:  call       ""System.IDisposable C.F()""
  IL_0006:  stloc.0
  .try
{
  IL_0007:  nop
  IL_0008:  nop
  IL_0009:  leave.s    IL_001b
}
  finally
{
  IL_000b:  ldloc.0
  IL_000c:  ldnull
  IL_000d:  ceq
  IL_000f:  stloc.1
  IL_0010:  ldloc.1
  IL_0011:  brtrue.s   IL_001a
  IL_0013:  ldloc.0
  IL_0014:  callvirt   ""void System.IDisposable.Dispose()""
  IL_0019:  nop
  IL_001a:  endfinally
}
  IL_001b:  call       ""System.IDisposable C.F()""
  IL_0020:  stloc.2
  .try
{
  IL_0021:  nop
  IL_0022:  nop
  IL_0023:  leave.s    IL_0035
}
  finally
{
  IL_0025:  ldloc.2
  IL_0026:  ldnull
  IL_0027:  ceq
  IL_0029:  stloc.1
  IL_002a:  ldloc.1
  IL_002b:  brtrue.s   IL_0034
  IL_002d:  ldloc.2
  IL_002e:  callvirt   ""void System.IDisposable.Dispose()""
  IL_0033:  nop
  IL_0034:  endfinally
}
  IL_0035:  ret
}");

            var diff1b = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, preserveLocalVariables: true)));

            diff1b.VerifyIL("C.M",
@"{
  // Code size       54 (0x36)
  .maxstack  2
  .locals init (System.IDisposable V_0, //CS$3$0000
  bool V_1,
  System.IDisposable V_2, //x
  bool V_3)
  IL_0000:  nop
  IL_0001:  call       ""System.IDisposable C.F()""
  IL_0006:  stloc.0
  .try
{
  IL_0007:  nop
  IL_0008:  nop
  IL_0009:  leave.s    IL_001b
}
  finally
{
  IL_000b:  ldloc.0
  IL_000c:  ldnull
  IL_000d:  ceq
  IL_000f:  stloc.3
  IL_0010:  ldloc.3
  IL_0011:  brtrue.s   IL_001a
  IL_0013:  ldloc.0
  IL_0014:  callvirt   ""void System.IDisposable.Dispose()""
  IL_0019:  nop
  IL_001a:  endfinally
}
  IL_001b:  call       ""System.IDisposable C.F()""
  IL_0020:  stloc.2
  .try
{
  IL_0021:  nop
  IL_0022:  nop
  IL_0023:  leave.s    IL_0035
}
  finally
{
  IL_0025:  ldloc.2
  IL_0026:  ldnull
  IL_0027:  ceq
  IL_0029:  stloc.3
  IL_002a:  ldloc.3
  IL_002b:  brtrue.s   IL_0034
  IL_002d:  ldloc.2
  IL_002e:  callvirt   ""void System.IDisposable.Dispose()""
  IL_0033:  nop
  IL_0034:  endfinally
}
  IL_0035:  ret
}");
        }

        [WorkItem(779531, "DevDiv")]
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
            var compilation0 = CreateCompilationWithMscorlib(source0, compOptions: TestOptions.UnoptimizedDll.WithDebugInformationKind(DebugInformationKind.Full));
            var compilation1 = CreateCompilationWithMscorlib(source1, compOptions: TestOptions.UnoptimizedDll.WithDebugInformationKind(DebugInformationKind.Full));
            var compilation2 = CreateCompilationWithMscorlib(source2, compOptions: TestOptions.UnoptimizedDll.WithDebugInformationKind(DebugInformationKind.Full));
            var compilation3 = CreateCompilationWithMscorlib(source3, compOptions: TestOptions.UnoptimizedDll.WithDebugInformationKind(DebugInformationKind.Full));

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(debug: true, testData: testData0);
            var methodData0 = testData0.GetMethodData("C.M1");
            var method0 = compilation0.GetMember<MethodSymbol>("C.M1");
            var generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), m => GetLocalNames(methodData0));

            var method1 = compilation1.GetMember<MethodSymbol>("C.M1");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Insert, null, compilation1.GetMember<NamedTypeSymbol>("A")),
                    new SemanticEdit(SemanticEditKind.Update, method0, method1, GetLocalMap(method1, method0), preserveLocalVariables: true)));

            diff1.VerifyIL("C.M1",
@"{
  // Code size       17 (0x11)
  .maxstack  1
  .locals init (E V_0,
  E V_1,
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
                    new SemanticEdit(SemanticEditKind.Update, method1, method2, GetLocalMap(method2, method1), preserveLocalVariables: true)));

            diff2.VerifyIL("C.M2",
@"{
  // Code size       17 (0x11)
  .maxstack  1
  .locals init (E V_0,
  E V_1,
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
                    new SemanticEdit(SemanticEditKind.Update, method2, method3, GetLocalMap(method3, method2), preserveLocalVariables: true)));

            diff3.VerifyIL("C.M2",
@"{
  // Code size       18 (0x12)
  .maxstack  1
  .locals init (E V_0,
  E V_1,
  E V_2,
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

        /// <summary>
        /// Reuse existing anonymous types.
        /// </summary>
        [WorkItem(825903, "DevDiv")]
        [Fact(Skip = "900633")]
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
            var compilation0 = CreateCompilationWithMscorlib(source0, compOptions: TestOptions.UnoptimizedDll);
            var compilation1 = CreateCompilationWithMscorlib(source1, compOptions: TestOptions.UnoptimizedDll);

            var bytes0 = compilation0.EmitToArray(debug: true);
            using (var md0 = ModuleMetadata.CreateFromImage(bytes0))
            {
                var generation0 = EmitBaseline.CreateInitialBaseline(
                    md0,
                    m => ImmutableArray.Create("x", "y", "z"));
                var method0 = compilation0.GetMember<MethodSymbol>("M.B.M");
                var reader0 = md0.MetadataReader;
                CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "<>f__AnonymousType0`2", "<>f__AnonymousType1`2", "<>f__AnonymousType2", "B", "A");

                var method1 = compilation1.GetMember<MethodSymbol>("M.B.M");
                var diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetLocalMap(method1, method0), preserveLocalVariables: true)));
                using (var md1 = diff1.GetMetadata())
                {
                    var reader1 = md1.Reader;
                    CheckNames(new[] { reader0, reader1 }, reader1.GetTypeDefNames(), "<>f__AnonymousType3`1"); // one additional type

                    diff1.VerifyIL("M.B.M",
@"{
  // Code size       28 (0x1c)
  .maxstack  2
  .locals init (<>f__AnonymousType1<int, int> V_0, //x
  int V_1,
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
            }
        }

        /// <summary>
        /// Anonymous type names with module ids
        /// and gaps in indices.
        /// </summary>
        [Fact]
        public void AnonymousTypes_OtherTypeNames()
        {
            var ilSource =
@".assembly extern mscorlib { }
// Valid signature, although not sequential index
.class '<>f__AnonymousType2'<'<A>j__TPar', '<B>j__TPar'>
{
  .field public !'<A>j__TPar' A
  .field public !'<B>j__TPar' B
}
// Invalid signature, unexpected type parameter names
.class '<>f__AnonymousType1'<A, B>
{
  .field public !A A
  .field public !B B
}
// Module id, duplicate index
.class '<m>f__AnonymousType2`1'<'<A>j__TPar'>
{
  .field public !'<A>j__TPar' A
}
// Module id
.class '<m>f__AnonymousType3`1'<'<B>j__TPar'>
{
  .field public !'<B>j__TPar' B
}
.class public C
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
            var metadata0 = (MetadataImageReference)CompileIL(ilSource);
            // Still need a compilation with source for the initial
            // generation - to get a MethodSymbol and syntax map.
            var compilation0 = CreateCompilationWithMscorlib(source0, compOptions: TestOptions.UnoptimizedDll.WithDebugInformationKind(DebugInformationKind.Full));
            var compilation1 = CreateCompilationWithMscorlib(source1, compOptions: TestOptions.UnoptimizedDll.WithDebugInformationKind(DebugInformationKind.Full));

            var moduleMetadata0 = ((AssemblyMetadata)metadata0.GetMetadata()).Modules[0];
            var method0 = compilation0.GetMember<MethodSymbol>("C.F");
            var generation0 = EmitBaseline.CreateInitialBaseline(
            moduleMetadata0,
                m => ImmutableArray.Create<string>());

            var method1 = compilation1.GetMember<MethodSymbol>("C.F");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetLocalMap(method1, method0), preserveLocalVariables: true)));
            using (var md1 = diff1.GetMetadata())
            {
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
        }

        /// <summary>
        /// Update method with anonymous type that was
        /// not directly referenced in previous generation.
        /// </summary>
        [Fact]
        public void AnonymousTypes_SkipGeneration()
        {
            var source0 =
@"class A { }
class B
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
@"class A { }
class B
{
    static object F()
    {
        var x = new { A = 1 };
        return x.A;
    }
    static object G()
    {
        var x = 1;
        return x + 1;
    }
}";
            var source2 =
@"class A { }
class B
{
    static object F()
    {
        var x = new { A = 1 };
        return x.A;
    }
    static object G()
    {
        var x = new { A = new A() };
        var y = new { B = 2 };
        return x.A;
    }
}";
            var source3 =
@"class A { }
class B
{
    static object F()
    {
        var x = new { A = 1 };
        return x.A;
    }
    static object G()
    {
        var x = new { A = new A() };
        var y = new { B = 3 };
        return y.B;
    }
}";
            var compilation0 = CreateCompilationWithMscorlib(source0, compOptions: TestOptions.UnoptimizedDll);
            var compilation1 = CreateCompilationWithMscorlib(source1, compOptions: TestOptions.UnoptimizedDll);
            var compilation2 = CreateCompilationWithMscorlib(source2, compOptions: TestOptions.UnoptimizedDll);
            var compilation3 = CreateCompilationWithMscorlib(source3, compOptions: TestOptions.UnoptimizedDll);

            var bytes0 = compilation0.EmitToArray(debug: true);
            using (var md0 = ModuleMetadata.CreateFromImage(bytes0))
            {
                var generation0 = EmitBaseline.CreateInitialBaseline(
                    md0,
                    m => ImmutableArray.Create("x"));
                var method0 = compilation0.GetMember<MethodSymbol>("B.G");
                var reader0 = md0.MetadataReader;
                CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "<>f__AnonymousType0`1", "A", "B");

                var method1 = compilation1.GetMember<MethodSymbol>("B.G");
                var diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetLocalMap(method1, method0), preserveLocalVariables: true)));
                using (var md1 = diff1.GetMetadata())
                {
                    var reader1 = md1.Reader;
                    CheckNames(new[] { reader0, reader1 }, reader1.GetTypeDefNames()); // no additional types
                    diff1.VerifyIL("B.G",
@"{
  // Code size       16 (0x10)
  .maxstack  2
  .locals init (int V_0, //x
  object V_1,
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

                    var method2 = compilation2.GetMember<MethodSymbol>("B.G");
                    var diff2 = compilation2.EmitDifference(
                        diff1.NextGeneration,
                        ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method1, method2, GetLocalMap(method2, method1), preserveLocalVariables: true)));
                    using (var md2 = diff2.GetMetadata())
                    {
                        var reader2 = md2.Reader;
                        CheckNames(new[] { reader0, reader1, reader2 }, reader2.GetTypeDefNames(), "<>f__AnonymousType1`1"); // one additional type
                        diff2.VerifyIL("B.G",
@"{
  // Code size       33 (0x21)
  .maxstack  1
  .locals init (int V_0,
  object V_1,
  object V_2,
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

                        var method3 = compilation3.GetMember<MethodSymbol>("B.G");
                        var diff3 = compilation3.EmitDifference(
                            diff2.NextGeneration,
                            ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method2, method3, GetLocalMap(method3, method2), preserveLocalVariables: true)));
                        using (var md3 = diff3.GetMetadata())
                        {
                            var reader3 = md3.Reader;
                            CheckNames(new[] { reader0, reader1, reader2, reader3 }, reader3.GetTypeDefNames()); // no additional types
                            diff3.VerifyIL("B.G",
    @"{
  // Code size       39 (0x27)
  .maxstack  1
  .locals init (int V_0,
  object V_1,
  object V_2,
  <>f__AnonymousType0<A> V_3, //x
  <>f__AnonymousType1<int> V_4, //y
  object V_5)
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
  IL_0020:  stloc.s    V_5
  IL_0022:  br.s       IL_0024
  IL_0024:  ldloc.s    V_5
  IL_0026:  ret
}");
                        }
                    }
                }
            }
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
            var compilation0 = CreateCompilationWithMscorlib(source0, compOptions: TestOptions.UnoptimizedDll);
            var compilation1 = CreateCompilationWithMscorlib(source1, compOptions: TestOptions.UnoptimizedDll);
            var compilation2 = CreateCompilationWithMscorlib(source2, compOptions: TestOptions.UnoptimizedDll);
            var compilation3 = CreateCompilationWithMscorlib(source3, compOptions: TestOptions.UnoptimizedDll);

            var bytes0 = compilation0.EmitToArray(debug: true);
            using (var md0 = ModuleMetadata.CreateFromImage(bytes0))
            {
                var generation0 = EmitBaseline.CreateInitialBaseline(
                    md0,
                    m => ImmutableArray.Create("x"));
                var method0F = compilation0.GetMember<MethodSymbol>("C.F");
                var reader0 = md0.MetadataReader;
                CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "<>f__AnonymousType0`1", "C");

                var method1F = compilation1.GetMember<MethodSymbol>("C.F");
                var method1G = compilation1.GetMember<MethodSymbol>("C.G");
                var diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0F, method1F, GetLocalMap(method1F, method0F), preserveLocalVariables: true)));
                using (var md1 = diff1.GetMetadata())
                {
                    var reader1 = md1.Reader;
                    CheckNames(new[] { reader0, reader1 }, reader1.GetTypeDefNames(), "<>f__AnonymousType1`2"); // one additional type

                    var method2G = compilation2.GetMember<MethodSymbol>("C.G");
                    var diff2 = compilation2.EmitDifference(
                        diff1.NextGeneration,
                        ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method1G, method2G, GetLocalMap(method2G, method1G), preserveLocalVariables: true)));
                    using (var md2 = diff2.GetMetadata())
                    {
                        var reader2 = md2.Reader;
                        CheckNames(new[] { reader0, reader1, reader2 }, reader2.GetTypeDefNames()); // no additional types

                        var method3G = compilation3.GetMember<MethodSymbol>("C.G");
                        var diff3 = compilation3.EmitDifference(
                            diff2.NextGeneration,
                            ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method2G, method3G, GetLocalMap(method3G, method2G), preserveLocalVariables: true)));
                        using (var md3 = diff3.GetMetadata())
                        {
                            var reader3 = md3.Reader;
                            CheckNames(new[] { reader0, reader1, reader2, reader3 }, reader3.GetTypeDefNames()); // no additional types
                        }
                    }
                }
            }
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
            var compilation0 = CreateCompilationWithMscorlib(source0, compOptions: TestOptions.UnoptimizedDll);
            var compilation1 = CreateCompilationWithMscorlib(source1, compOptions: TestOptions.UnoptimizedDll);
            var compilation2 = CreateCompilationWithMscorlib(source2, compOptions: TestOptions.UnoptimizedDll);
            var compilation3 = CreateCompilationWithMscorlib(source3, compOptions: TestOptions.UnoptimizedDll);
            var compilation4 = CreateCompilationWithMscorlib(source4, compOptions: TestOptions.UnoptimizedDll);

            var bytes0 = compilation0.EmitToArray(debug: true);
            using (var md0 = ModuleMetadata.CreateFromImage(bytes0))
            {
                var generation0 = EmitBaseline.CreateInitialBaseline(
                    md0,
                    m => ImmutableArray.Create("x", "y"));
                var method0 = compilation0.GetMember<MethodSymbol>("C.F");
                var reader0 = md0.MetadataReader;
                CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "C");

                var method1 = compilation1.GetMember<MethodSymbol>("C.F");
                var diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetLocalMap(method1, method0), preserveLocalVariables: true)));
                using (var md1 = diff1.GetMetadata())
                {
                    var reader1 = md1.Reader;
                    CheckNames(new[] { reader0, reader1 }, reader1.GetTypeDefNames(), "<>f__AnonymousType0`1"); // one additional type

                    diff1.VerifyIL("C.F",
@"{
  // Code size       27 (0x1b)
  .maxstack  1
  .locals init (C V_0,
  object V_1, //y
  object V_2,
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
                    // TODO: Generate placeholder for missing types.
#if false
                    var diff2 = compilation2.EmitDifference(
                        diff1.NextGeneration,
                        ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method1, method2, GetLocalMap(method2, method1), preserveLocalVariables: true)));
                    using (var md2 = diff2.GetMetadata())
                    {
                        var reader2 = md2.Reader;
                        CheckNames(new[] { reader0, reader1, reader2 }, reader2.GetTypeDefNames(), "<>f__AnonymousType1`2", "<>f__AnonymousType2`1"); // two additional types

                        diff2.VerifyIL("C.F",
@"{
  // Code size       46 (0x2e)
  .maxstack  2
  .locals init ( V_0,
  object V_1, //y
  V_2,
  V_3,
  V_4,
  <>f__AnonymousType1<object, int> V_5, //x
  object V_6)
  IL_0000:  nop
  IL_0001:  newobj     ""object..ctor()""
  IL_0006:  ldc.i4.2
  IL_0007:  newobj     ""<>f__AnonymousType1<object, int>..ctor(object, int)""
  IL_000c:  stloc.s    V_5
  IL_000e:  ldloc.s    V_5
  IL_0010:  callvirt   ""object <>f__AnonymousType1<object, int>.A.get""
  IL_0015:  stloc.1
  IL_0016:  newobj     ""object..ctor()""
  IL_001b:  newobj     ""<>f__AnonymousType2<object>..ctor(object)""
  IL_0020:  call       ""object <>f__AnonymousType2<object>.B.get""
  IL_0025:  stloc.1
  IL_0026:  ldloc.1
  IL_0027:  stloc.s    V_6
  IL_0029:  br.s       IL_002b
  IL_002b:  ldloc.s    V_6
  IL_002d:  ret
}");

                        var method3 = compilation3.GetMember<MethodSymbol>("C.F");
                        var diff3 = compilation3.EmitDifference(
                            diff2.NextGeneration,
                            ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method2, method3, GetLocalMap(method3, method2), preserveLocalVariables: true)));
                        using (var md3 = diff3.GetMetadata())
                        {
                            var reader3 = md3.Reader;
                            CheckNames(new[] { reader0, reader1, reader2, reader3 }, reader3.GetTypeDefNames()); // no additional types

                            diff3.VerifyIL("C.F",
@"{
  // Code size       30 (0x1e)
  .maxstack  2
  .locals init ( V_0,
  object V_1, //y
  V_2,
  V_3,
  V_4,
  <>f__AnonymousType1<object, int> V_5, //x
  object V_6)
  IL_0000:  nop
  IL_0001:  newobj     ""object..ctor()""
  IL_0006:  ldc.i4.3
  IL_0007:  newobj     ""<>f__AnonymousType1<object, int>..ctor(object, int)""
  IL_000c:  stloc.s    V_5
  IL_000e:  ldloc.s    V_5
  IL_0010:  callvirt   ""object <>f__AnonymousType1<object, int>.A.get""
  IL_0015:  stloc.1
  IL_0016:  ldloc.1
  IL_0017:  stloc.s    V_6
  IL_0019:  br.s       IL_001b
  IL_001b:  ldloc.s    V_6
  IL_001d:  ret
}");

                            var method4 = compilation4.GetMember<MethodSymbol>("C.F");
                            var diff4 = compilation4.EmitDifference(
                                diff3.NextGeneration,
                                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method3, method4, GetLocalMap(method4, method3), preserveLocalVariables: true)));
                            using (var md4 = diff4.GetMetadata())
                            {
                                var reader4 = md4.Reader;
                                CheckNames(new[] { reader0, reader1, reader2, reader3, reader4 }, reader4.GetTypeDefNames(), "<>f__AnonymousType3`2"); // one additional type

                                diff4.VerifyIL("C.F",
@"{
  // Code size       30 (0x1e)
  .maxstack  2
  .locals init ( V_0,
  object V_1, //y
  V_2,
  V_3,
  V_4,
  <>f__AnonymousType3<int, object> V_5, //x
  object V_6)
  IL_0000:  nop
  IL_0001:  ldc.i4.4
  IL_0002:  newobj     ""object..ctor()""
  IL_0007:  newobj     ""<>f__AnonymousType3<int, object>..ctor(int, object)""
  IL_000c:  stloc.s    V_5
  IL_000e:  ldloc.s    V_5
  IL_0010:  callvirt   ""object <>f__AnonymousType3<int, object>.A.get""
  IL_0015:  stloc.1
  IL_0016:  ldloc.1
  IL_0017:  stloc.s    V_6
  IL_0019:  br.s       IL_001b
  IL_001b:  ldloc.s    V_6
  IL_001d:  ret
}");
                            }
                        }
                    }
#endif
                }
            }
        }

        /// <summary>
        /// Should not re-use locals if the method metadata
        /// signature is unsupported.
        /// </summary>
        [Fact]
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
            var compilation0 = CreateCompilationWithMscorlib(source, compOptions: TestOptions.UnoptimizedDll.WithDebugInformationKind(DebugInformationKind.Full));
            var compilation1 = CreateCompilationWithMscorlib(source, compOptions: TestOptions.UnoptimizedDll.WithDebugInformationKind(DebugInformationKind.Full));

            var method0 = compilation0.GetMember<MethodSymbol>("C.M1");
            var generation0 = EmitBaseline.CreateInitialBaseline(md0, m => ImmutableArray.Create(null, "o"));

            var method1 = compilation1.GetMember<MethodSymbol>("C.M1");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetLocalMap(method1, method0), preserveLocalVariables: true)));

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
        [Fact]
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
             [1] class [mscorlib]System.IDisposable modopt(object) CS$3$0000,
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
            var metadata0 = (MetadataImageReference)CompileIL(ilSource, appendDefaultHeader: false);
            // Still need a compilation with source for the initial
            // generation - to get a MethodSymbol and syntax map.
            var compilation0 = CreateCompilationWithMscorlib(source, compOptions: TestOptions.UnoptimizedDll.WithDebugInformationKind(DebugInformationKind.Full));
            var compilation1 = CreateCompilationWithMscorlib(source, compOptions: TestOptions.UnoptimizedDll.WithDebugInformationKind(DebugInformationKind.Full));

            var moduleMetadata0 = ((AssemblyMetadata)metadata0.GetMetadata()).Modules[0];
            var method0 = compilation0.GetMember<MethodSymbol>("C.F");
            var generation0 = EmitBaseline.CreateInitialBaseline(
                moduleMetadata0,
                m => ImmutableArray.Create("c", "CS$3$0000", null, null));

            var method1 = compilation1.GetMember<MethodSymbol>("C.F");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetLocalMap(method1, method0), preserveLocalVariables: true)));
            diff1.VerifyIL("C.F",
@"{
  // Code size       45 (0x2d)
  .maxstack  2
  .locals init (C V_0,
  System.IDisposable V_1,
  bool V_2,
  object V_3,
  C V_4, //c
  System.IDisposable V_5, //CS$3$0000
  bool V_6,
  object V_7)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.s    V_5
  .try
{
  IL_0004:  nop
  IL_0005:  ldarg.0
  IL_0006:  castclass  ""C""
  IL_000b:  stloc.s    V_4
  IL_000d:  nop
  IL_000e:  leave.s    IL_0024
}
  finally
{
  IL_0010:  ldloc.s    V_5
  IL_0012:  ldnull
  IL_0013:  ceq
  IL_0015:  stloc.s    V_6
  IL_0017:  ldloc.s    V_6
  IL_0019:  brtrue.s   IL_0023
  IL_001b:  ldloc.s    V_5
  IL_001d:  callvirt   ""void System.IDisposable.Dispose()""
  IL_0022:  nop
  IL_0023:  endfinally
}
  IL_0024:  ldloc.s    V_4
  IL_0026:  stloc.s    V_7
  IL_0028:  br.s       IL_002a
  IL_002a:  ldloc.s    V_7
  IL_002c:  ret
}");
        }

        /// <summary>
        /// Temporaries should only be named in debug builds.
        /// </summary>
        [Fact]
        public void TemporaryLocals_DebugOnly()
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
        lock (F()) { }
        using (F()) { }
    }
}";
            var compilation0 = CreateCompilationWithMscorlib(
                source,
                compOptions: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, debugInformationKind: DebugInformationKind.Full, optimize: false, concurrentBuild: false));
            var compilation1 = CreateCompilationWithMscorlib(
                source,
                compOptions: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, debugInformationKind: DebugInformationKind.Full, optimize: true, concurrentBuild: false));
            var compilation2 = CreateCompilationWithMscorlib(
                source,
                compOptions: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, debugInformationKind: DebugInformationKind.None, optimize: false, concurrentBuild: false));
            var compilation3 = CreateCompilationWithMscorlib(
                source,
                compOptions: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, debugInformationKind: DebugInformationKind.None, optimize: true, concurrentBuild: false));

            CompilationTestData testData;
            ImmutableArray<string> names;

            testData = new CompilationTestData();
            compilation0.EmitToArray(debug: true, testData: testData);
            names = GetLocalNames(testData.GetMethodData("C.M"));
            Assert.True(names.SequenceEqual(new string[] { "CS$2$0000", "CS$520$0001", null, "CS$3$0002" }));

            testData = new CompilationTestData();
            compilation1.EmitToArray(debug: true, testData: testData);
            names = GetLocalNames(testData.GetMethodData("C.M"));
            Assert.True(names.SequenceEqual(new string[] { "CS$2$0000", "CS$520$0001", "CS$3$0002" }));

            testData = new CompilationTestData();
            compilation2.EmitToArray(debug: true, testData: testData);
            names = GetLocalNames(testData.GetMethodData("C.M"));
            Assert.True(names.SequenceEqual(new string[] { null, null, null }));

            testData = new CompilationTestData();
            compilation3.EmitToArray(debug: true, testData: testData);
            names = GetLocalNames(testData.GetMethodData("C.M"));
            Assert.True(names.SequenceEqual(new string[] { null, null }));
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
            var compilation0 = CreateCompilationWithMscorlib(source, compOptions: TestOptions.UnoptimizedDll.WithDebugInformationKind(DebugInformationKind.Full));
            var compilation1 = CreateCompilationWithMscorlib(source, compOptions: TestOptions.UnoptimizedDll.WithDebugInformationKind(DebugInformationKind.Full));

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(debug: true, testData: testData0);
            var methodData0 = testData0.GetMethodData("C.M");
            var method0 = compilation0.GetMember<MethodSymbol>("C.M");
            var generation0 = EmitBaseline.CreateInitialBaseline(
                ModuleMetadata.CreateFromImage(bytes0),
                m => GetLocalNames(methodData0));

            var method1 = compilation1.GetMember<MethodSymbol>("C.M");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetLocalMap(method1, method0), preserveLocalVariables: true)));

            diff1.VerifyIL("C.M",
@"{
  // Code size       37 (0x25)
  .maxstack  3
  .locals init (C V_0, //c
  C V_1,
  int V_2,
  C V_3,
  int V_4)
  IL_0000:  nop
  IL_0001:  newobj     ""C..ctor()""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  stloc.3
  IL_0009:  ldloc.3
  IL_000a:  callvirt   ""int C.P.get""
  IL_000f:  stloc.s    V_4
  IL_0011:  ldloc.3
  IL_0012:  ldloc.s    V_4
  IL_0014:  ldc.i4.1
  IL_0015:  add
  IL_0016:  callvirt   ""void C.P.set""
  IL_001b:  nop
  IL_001c:  ldloc.s    V_4
  IL_001e:  stloc.s    V_4
  IL_0020:  br.s       IL_0022
  IL_0022:  ldloc.s    V_4
  IL_0024:  ret
}");
        }

        [Fact]
        public void TemporaryLocals_Using()
        {
            var source =
@"class C : System.IDisposable
{
    public void Dispose()
    {
    }
    static System.IDisposable F()
    {
        return new C();
    }
    static void M()
    {
        using (F())
        {
            using (var u = F())
            {
            }
            using (F())
            {
            }
        }
    }
}";
            var compilation0 = CreateCompilationWithMscorlib(source, compOptions: TestOptions.UnoptimizedDll.WithDebugInformationKind(DebugInformationKind.Full));
            var compilation1 = CreateCompilationWithMscorlib(source, compOptions: TestOptions.UnoptimizedDll.WithDebugInformationKind(DebugInformationKind.Full));

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(debug: true, testData: testData0);
            var methodData0 = testData0.GetMethodData("C.M");
            var method0 = compilation0.GetMember<MethodSymbol>("C.M");
            var generation0 = EmitBaseline.CreateInitialBaseline(
                ModuleMetadata.CreateFromImage(bytes0),
                m => GetLocalNames(methodData0));

            var method1 = compilation1.GetMember<MethodSymbol>("C.M");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetLocalMap(method1, method0), preserveLocalVariables: true)));

            diff1.VerifyIL("C.M",
@"{
  // Code size       86 (0x56)
  .maxstack  2
  .locals init (System.IDisposable V_0, //CS$3$0000
  System.IDisposable V_1, //u
  bool V_2,
  System.IDisposable V_3, //CS$3$0001
  bool V_4)
  IL_0000:  nop
  IL_0001:  call       ""System.IDisposable C.F()""
  IL_0006:  stloc.0
  .try
{
  IL_0007:  nop
  IL_0008:  call       ""System.IDisposable C.F()""
  IL_000d:  stloc.1
  .try
{
  IL_000e:  nop
  IL_000f:  nop
  IL_0010:  leave.s    IL_0024
}
  finally
{
  IL_0012:  ldloc.1
  IL_0013:  ldnull
  IL_0014:  ceq
  IL_0016:  stloc.s    V_4
  IL_0018:  ldloc.s    V_4
  IL_001a:  brtrue.s   IL_0023
  IL_001c:  ldloc.1
  IL_001d:  callvirt   ""void System.IDisposable.Dispose()""
  IL_0022:  nop
  IL_0023:  endfinally
}
  IL_0024:  call       ""System.IDisposable C.F()""
  IL_0029:  stloc.3
  .try
{
  IL_002a:  nop
  IL_002b:  nop
  IL_002c:  leave.s    IL_0040
}
  finally
{
  IL_002e:  ldloc.3
  IL_002f:  ldnull
  IL_0030:  ceq
  IL_0032:  stloc.s    V_4
  IL_0034:  ldloc.s    V_4
  IL_0036:  brtrue.s   IL_003f
  IL_0038:  ldloc.3
  IL_0039:  callvirt   ""void System.IDisposable.Dispose()""
  IL_003e:  nop
  IL_003f:  endfinally
}
  IL_0040:  nop
  IL_0041:  leave.s    IL_0055
}
  finally
{
  IL_0043:  ldloc.0
  IL_0044:  ldnull
  IL_0045:  ceq
  IL_0047:  stloc.s    V_4
  IL_0049:  ldloc.s    V_4
  IL_004b:  brtrue.s   IL_0054
  IL_004d:  ldloc.0
  IL_004e:  callvirt   ""void System.IDisposable.Dispose()""
  IL_0053:  nop
  IL_0054:  endfinally
}
  IL_0055:  ret
}");
        }

        /// <summary>
        /// Local names array (from PDB) may have fewer slots than method
        /// signature (from metadata) if the trailing slots are unnamed.
        /// </summary>
        [WorkItem(782270, "DevDiv")]
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
            var compilation0 = CreateCompilationWithMscorlib(source, compOptions: TestOptions.UnoptimizedDll.WithAllowUnsafe(true).WithDebugInformationKind(DebugInformationKind.Full));
            var compilation1 = CreateCompilationWithMscorlib(source, compOptions: TestOptions.UnoptimizedDll.WithAllowUnsafe(true).WithDebugInformationKind(DebugInformationKind.Full));

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(debug: true, testData: testData0);
            var methodData0 = testData0.GetMethodData("C.M");
            var method0 = compilation0.GetMember<MethodSymbol>("C.M");
            var generation0 = EmitBaseline.CreateInitialBaseline(
                ModuleMetadata.CreateFromImage(bytes0),
                m => ImmutableArray.Create("o"));

            testData0.GetMethodData("C.M").VerifyIL(
@"
{
  // Code size       28 (0x1c)
  .maxstack  2
  .locals init (System.IDisposable V_0, //o
  bool V_1)
  IL_0000:  nop
  IL_0001:  call       ""System.IDisposable C.F()""
  IL_0006:  stloc.0
  .try
{
  IL_0007:  nop
  IL_0008:  nop
  IL_0009:  leave.s    IL_001b
}
  finally
{
  IL_000b:  ldloc.0
  IL_000c:  ldnull
  IL_000d:  ceq
  IL_000f:  stloc.1
  IL_0010:  ldloc.1
  IL_0011:  brtrue.s   IL_001a
  IL_0013:  ldloc.0
  IL_0014:  callvirt   ""void System.IDisposable.Dispose()""
  IL_0019:  nop
  IL_001a:  endfinally
}
  IL_001b:  ret
}");

            var method1 = compilation1.GetMember<MethodSymbol>("C.M");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetLocalMap(method1, method0), preserveLocalVariables: true)));

            diff1.VerifyIL("C.M",
@"{
  // Code size       28 (0x1c)
  .maxstack  2
  .locals init (System.IDisposable V_0, //o
  bool V_1,
  bool V_2)
  IL_0000:  nop
  IL_0001:  call       ""System.IDisposable C.F()""
  IL_0006:  stloc.0
  .try
{
  IL_0007:  nop
  IL_0008:  nop
  IL_0009:  leave.s    IL_001b
}
  finally
{
  IL_000b:  ldloc.0
  IL_000c:  ldnull
  IL_000d:  ceq
  IL_000f:  stloc.2
  IL_0010:  ldloc.2
  IL_0011:  brtrue.s   IL_001a
  IL_0013:  ldloc.0
  IL_0014:  callvirt   ""void System.IDisposable.Dispose()""
  IL_0019:  nop
  IL_001a:  endfinally
}
  IL_001b:  ret
}");
        }

        /// <summary>
        /// Similar to above test but with no named locals in original.
        /// </summary>
        [WorkItem(782270, "DevDiv")]
        [Fact]
        public void Bug782270_NoNamedLocals()
        {
            // Equivalent to C#, but with unnamed locals.
            // Used to generate initial metadata.
            var ilSource =
@".assembly extern mscorlib { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89) }
.assembly '<<GeneratedFileName>>' { }
.class C
{
  .method private static class [mscorlib]System.IDisposable F()
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
            ImmutableArray<byte> assemblyBytes;
            ImmutableArray<byte> pdbBytes;
            EmitILToArray(ilSource, appendDefaultHeader: false, includePdb: false, assemblyBytes: out assemblyBytes, pdbBytes: out pdbBytes);
            var md0 = ModuleMetadata.CreateFromImage(assemblyBytes);
            // Still need a compilation with source for the initial
            // generation - to get a MethodSymbol and syntax map.
            var compilation0 = CreateCompilationWithMscorlib(source0, compOptions: TestOptions.UnoptimizedDll.WithDebugInformationKind(DebugInformationKind.Full));
            var compilation1 = CreateCompilationWithMscorlib(source1, compOptions: TestOptions.UnoptimizedDll.WithDebugInformationKind(DebugInformationKind.Full));

            var method0 = compilation0.GetMember<MethodSymbol>("C.M");
            var generation0 = EmitBaseline.CreateInitialBaseline(md0, EmptyLocalsProvider);

            var method1 = compilation1.GetMember<MethodSymbol>("C.M");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetLocalMap(method1, method0), preserveLocalVariables: true)));

            diff1.VerifyIL("C.M",
@"{
  // Code size       28 (0x1c)
  .maxstack  2
  .locals init (object V_0,
  object V_1,
  System.IDisposable V_2, //o
  bool V_3)
  IL_0000:  nop
  IL_0001:  call       ""System.IDisposable C.F()""
  IL_0006:  stloc.2
  .try
{
  IL_0007:  nop
  IL_0008:  nop
  IL_0009:  leave.s    IL_001b
}
  finally
{
  IL_000b:  ldloc.2
  IL_000c:  ldnull
  IL_000d:  ceq
  IL_000f:  stloc.3
  IL_0010:  ldloc.3
  IL_0011:  brtrue.s   IL_001a
  IL_0013:  ldloc.2
  IL_0014:  callvirt   ""void System.IDisposable.Dispose()""
  IL_0019:  nop
  IL_001a:  endfinally
}
  IL_001b:  ret
}");
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
            var compilation0 = CreateCompilationWithMscorlib(source, new[] { CSharpRef, SystemCoreRef }, compOptions: TestOptions.UnoptimizedDll.WithDebugInformationKind(DebugInformationKind.Full));
            var compilation1 = CreateCompilationWithMscorlib(source, new[] { CSharpRef, SystemCoreRef }, compOptions: TestOptions.UnoptimizedDll.WithDebugInformationKind(DebugInformationKind.Full));

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(debug: true, testData: testData0);
            var methodData0 = testData0.GetMethodData("C.M");
            var method0 = compilation0.GetMember<MethodSymbol>("C.M");

            var modMeta = ModuleMetadata.CreateFromImage(bytes0);
            var generation0 = EmitBaseline.CreateInitialBaseline(
                modMeta,
                m => GetLocalNames(methodData0));

            var method1 = compilation1.GetMember<MethodSymbol>("C.M");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetLocalMap(method1, method0), preserveLocalVariables: true)));

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

        [Fact]
        public void TemporaryLocals_Lock()
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
        lock (F())
        {
            lock (F())
            {
            }
        }
    }
}";
            var compilation0 = CreateCompilationWithMscorlib(source, compOptions: TestOptions.UnoptimizedDll.WithDebugInformationKind(DebugInformationKind.Full));
            var compilation1 = CreateCompilationWithMscorlib(source, compOptions: TestOptions.UnoptimizedDll.WithDebugInformationKind(DebugInformationKind.Full));

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(debug: true, testData: testData0);
            var methodData0 = testData0.GetMethodData("C.M");
            var method0 = compilation0.GetMember<MethodSymbol>("C.M");
            var generation0 = EmitBaseline.CreateInitialBaseline(
                ModuleMetadata.CreateFromImage(bytes0),
                m => GetLocalNames(methodData0));

            var method1 = compilation1.GetMember<MethodSymbol>("C.M");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetLocalMap(method1, method0), preserveLocalVariables: true)));

            diff1.VerifyIL("C.M",
@"{
  // Code size       80 (0x50)
  .maxstack  2
  .locals init (object V_0, //CS$2$0000
  bool V_1, //CS$520$0001
  object V_2, //CS$2$0002
  bool V_3, //CS$520$0003
  bool V_4,
  bool V_5)
  IL_0000:  nop
  IL_0001:  ldc.i4.0
  IL_0002:  stloc.1
  .try
{
  IL_0003:  call       ""object C.F()""
  IL_0008:  stloc.0
  IL_0009:  ldloc.0
  IL_000a:  ldloca.s   V_1
  IL_000c:  call       ""void System.Threading.Monitor.Enter(object, ref bool)""
  IL_0011:  nop
  IL_0012:  nop
  IL_0013:  ldc.i4.0
  IL_0014:  stloc.3
  .try
{
  IL_0015:  call       ""object C.F()""
  IL_001a:  stloc.2
  IL_001b:  ldloc.2
  IL_001c:  ldloca.s   V_3
  IL_001e:  call       ""void System.Threading.Monitor.Enter(object, ref bool)""
  IL_0023:  nop
  IL_0024:  nop
  IL_0025:  nop
  IL_0026:  leave.s    IL_003a
}
  finally
{
  IL_0028:  ldloc.3
  IL_0029:  ldc.i4.0
  IL_002a:  ceq
  IL_002c:  stloc.s    V_5
  IL_002e:  ldloc.s    V_5
  IL_0030:  brtrue.s   IL_0039
  IL_0032:  ldloc.2
  IL_0033:  call       ""void System.Threading.Monitor.Exit(object)""
  IL_0038:  nop
  IL_0039:  endfinally
}
  IL_003a:  nop
  IL_003b:  leave.s    IL_004f
}
  finally
{
  IL_003d:  ldloc.1
  IL_003e:  ldc.i4.0
  IL_003f:  ceq
  IL_0041:  stloc.s    V_5
  IL_0043:  ldloc.s    V_5
  IL_0045:  brtrue.s   IL_004e
  IL_0047:  ldloc.0
  IL_0048:  call       ""void System.Threading.Monitor.Exit(object)""
  IL_004d:  nop
  IL_004e:  endfinally
}
  IL_004f:  ret
}");
        }

        /// <summary>
        /// Using Monitor.Enter(object).
        /// </summary>
        [Fact]
        public void TemporaryLocals_Lock_Pre40()
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
        lock (F())
        {
        }
    }
}";
            var compilation0 = CreateCompilation(
                source,
                compOptions: TestOptions.UnoptimizedDll.WithDebugInformationKind(DebugInformationKind.Full),
                references: new[] { MscorlibRef_v20 });
            var compilation1 = CreateCompilation(
                source,
                compOptions: TestOptions.UnoptimizedDll.WithDebugInformationKind(DebugInformationKind.Full),
                references: new[] { MscorlibRef_v20 });

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(debug: true, testData: testData0);
            var methodData0 = testData0.GetMethodData("C.M");
            var method0 = compilation0.GetMember<MethodSymbol>("C.M");
            var generation0 = EmitBaseline.CreateInitialBaseline(
                ModuleMetadata.CreateFromImage(bytes0),
                m => GetLocalNames(methodData0));

            var method1 = compilation1.GetMember<MethodSymbol>("C.M");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetLocalMap(method1, method0), preserveLocalVariables: true)));

            diff1.VerifyIL("C.M",
@"{
  // Code size       27 (0x1b)
  .maxstack  1
  .locals init (object V_0) //CS$2$0000
  IL_0000:  nop
  IL_0001:  call       ""object C.F()""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  call       ""void System.Threading.Monitor.Enter(object)""
  IL_000d:  nop
  .try
{
  IL_000e:  nop
  IL_000f:  nop
  IL_0010:  leave.s    IL_001a
}
  finally
{
  IL_0012:  ldloc.0
  IL_0013:  call       ""void System.Threading.Monitor.Exit(object)""
  IL_0018:  nop
  IL_0019:  endfinally
}
  IL_001a:  ret
}");
        }

        [Fact]
        public void TemporaryLocals_Fixed()
        {
            var source =
@"class C
{
    unsafe static void M(string s, int[] i)
    {
        fixed (char *p = s)
        {
            fixed (int *q = i)
            {
            }
            fixed (char *r = s)
            {
            }
        }
    }
}";
            var compilation0 = CreateCompilationWithMscorlib(source, compOptions: TestOptions.UnoptimizedDll.WithAllowUnsafe(true).WithDebugInformationKind(DebugInformationKind.Full));
            var compilation1 = CreateCompilationWithMscorlib(source, compOptions: TestOptions.UnoptimizedDll.WithAllowUnsafe(true).WithDebugInformationKind(DebugInformationKind.Full));

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(debug: true, testData: testData0);
            var methodData0 = testData0.GetMethodData("C.M");
            var method0 = compilation0.GetMember<MethodSymbol>("C.M");
            var generation0 = EmitBaseline.CreateInitialBaseline(
                ModuleMetadata.CreateFromImage(bytes0),
                m => GetLocalNames(methodData0));

            var method1 = compilation1.GetMember<MethodSymbol>("C.M");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetLocalMap(method1, method0), preserveLocalVariables: true)));

            diff1.VerifyIL("C.M",
@"
{
  // Code size      100 (0x64)
  .maxstack  2
  .locals init (char* V_0, //p
  pinned string V_1, //CS$519$0000
  bool V_2,
  pinned int& V_3, //q
  int[] V_4,
  char* V_5, //r
  pinned string V_6, //CS$519$0001
  bool V_7,
  int[] V_8)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.1
  IL_0003:  ldloc.1
  IL_0004:  conv.i
  IL_0005:  stloc.0
  IL_0006:  ldloc.1
  IL_0007:  conv.i
  IL_0008:  ldnull
  IL_0009:  ceq
  IL_000b:  stloc.s    V_7
  IL_000d:  ldloc.s    V_7
  IL_000f:  brtrue.s   IL_001b
  IL_0011:  ldloc.0
  IL_0012:  call       ""int System.Runtime.CompilerServices.RuntimeHelpers.OffsetToStringData.get""
  IL_0017:  add
  IL_0018:  stloc.0
  IL_0019:  br.s       IL_001b
  IL_001b:  nop
  IL_001c:  ldarg.1
  IL_001d:  dup
  IL_001e:  stloc.s    V_8
  IL_0020:  brfalse.s  IL_0028
  IL_0022:  ldloc.s    V_8
  IL_0024:  ldlen
  IL_0025:  conv.i4
  IL_0026:  brtrue.s   IL_002d
  IL_0028:  ldc.i4.0
  IL_0029:  conv.u
  IL_002a:  stloc.3
  IL_002b:  br.s       IL_0036
  IL_002d:  ldloc.s    V_8
  IL_002f:  ldc.i4.0
  IL_0030:  ldelema    ""int""
  IL_0035:  stloc.3
  IL_0036:  nop
  IL_0037:  nop
  IL_0038:  ldc.i4.0
  IL_0039:  conv.u
  IL_003a:  stloc.3
  IL_003b:  ldarg.0
  IL_003c:  stloc.s    V_6
  IL_003e:  ldloc.s    V_6
  IL_0040:  conv.i
  IL_0041:  stloc.s    V_5
  IL_0043:  ldloc.s    V_6
  IL_0045:  conv.i
  IL_0046:  ldnull
  IL_0047:  ceq
  IL_0049:  stloc.s    V_7
  IL_004b:  ldloc.s    V_7
  IL_004d:  brtrue.s   IL_005b
  IL_004f:  ldloc.s    V_5
  IL_0051:  call       ""int System.Runtime.CompilerServices.RuntimeHelpers.OffsetToStringData.get""
  IL_0056:  add
  IL_0057:  stloc.s    V_5
  IL_0059:  br.s       IL_005b
  IL_005b:  nop
  IL_005c:  nop
  IL_005d:  ldnull
  IL_005e:  stloc.s    V_6
  IL_0060:  nop
  IL_0061:  ldnull
  IL_0062:  stloc.1
  IL_0063:  ret
}");
        }

        /// <summary>
        /// Dev11 generates C$519$0000, CS$520$0001, CS$521$0002, ... for
        /// multiple declarations within a single fixed statement.
        /// Roslyn generates C$519$0000, CS$519$0001, CS$519$0002, ...
        /// rather than using a unique TempKind for each.
        /// </summary>
        [WorkItem(770053, "DevDiv")]
        [Fact]
        public void TemporaryLocals_FixedMultiple()
        {
            var source =
@"class C
{
    unsafe static void M(string s1, string s2, string s3, string s4)
    {
        fixed (char* p1 = s1, p2 = s2)
        {
            *p1 = *p2;
        }
        fixed (char* p1 = s1, p3 = s3, p2 = s4)
        {
            *p1 = *p2;
            *p2 = *p3;
            fixed (char *p4 = s2)
            {
                *p3 = *p4;
            }
        }
    }
}";
            var compilation0 = CreateCompilationWithMscorlib(source, compOptions: TestOptions.UnoptimizedDll.WithAllowUnsafe(true).WithDebugInformationKind(DebugInformationKind.Full));
            var compilation1 = CreateCompilationWithMscorlib(source, compOptions: TestOptions.UnoptimizedDll.WithAllowUnsafe(true).WithDebugInformationKind(DebugInformationKind.Full));

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(debug: true, testData: testData0);
            var methodData0 = testData0.GetMethodData("C.M");
            var method0 = compilation0.GetMember<MethodSymbol>("C.M");
            var generation0 = EmitBaseline.CreateInitialBaseline(
                ModuleMetadata.CreateFromImage(bytes0),
                m => GetLocalNames(methodData0));

            var method1 = compilation1.GetMember<MethodSymbol>("C.M");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetLocalMap(method1, method0), preserveLocalVariables: true)));

            diff1.VerifyIL("C.M",
@"{
  // Code size      226 (0xe2)
  .maxstack  2
  .locals init (char* V_0, //p1
  char* V_1, //p2
  pinned string V_2, //CS$519$0000
  pinned string V_3, //CS$519$0001
  bool V_4,
  char* V_5, //p1
  char* V_6, //p3
  char* V_7, //p2
  pinned string V_8, //CS$519$0002
  pinned string V_9, //CS$519$0003
  pinned string V_10, //CS$519$0004
  char* V_11, //p4
  pinned string V_12, //CS$519$0005
  bool V_13)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.2
  IL_0003:  ldloc.2
  IL_0004:  conv.i
  IL_0005:  stloc.0
  IL_0006:  ldloc.2
  IL_0007:  conv.i
  IL_0008:  ldnull
  IL_0009:  ceq
  IL_000b:  stloc.s    V_13
  IL_000d:  ldloc.s    V_13
  IL_000f:  brtrue.s   IL_001b
  IL_0011:  ldloc.0
  IL_0012:  call       ""int System.Runtime.CompilerServices.RuntimeHelpers.OffsetToStringData.get""
  IL_0017:  add
  IL_0018:  stloc.0
  IL_0019:  br.s       IL_001b
  IL_001b:  ldarg.1
  IL_001c:  stloc.3
  IL_001d:  ldloc.3
  IL_001e:  conv.i
  IL_001f:  stloc.1
  IL_0020:  ldloc.3
  IL_0021:  conv.i
  IL_0022:  ldnull
  IL_0023:  ceq
  IL_0025:  stloc.s    V_13
  IL_0027:  ldloc.s    V_13
  IL_0029:  brtrue.s   IL_0035
  IL_002b:  ldloc.1
  IL_002c:  call       ""int System.Runtime.CompilerServices.RuntimeHelpers.OffsetToStringData.get""
  IL_0031:  add
  IL_0032:  stloc.1
  IL_0033:  br.s       IL_0035
  IL_0035:  nop
  IL_0036:  ldloc.0
  IL_0037:  ldloc.1
  IL_0038:  ldind.u2
  IL_0039:  stind.i2
  IL_003a:  nop
  IL_003b:  ldnull
  IL_003c:  stloc.2
  IL_003d:  ldnull
  IL_003e:  stloc.3
  IL_003f:  ldarg.0
  IL_0040:  stloc.s    V_8
  IL_0042:  ldloc.s    V_8
  IL_0044:  conv.i
  IL_0045:  stloc.s    V_5
  IL_0047:  ldloc.s    V_8
  IL_0049:  conv.i
  IL_004a:  ldnull
  IL_004b:  ceq
  IL_004d:  stloc.s    V_13
  IL_004f:  ldloc.s    V_13
  IL_0051:  brtrue.s   IL_005f
  IL_0053:  ldloc.s    V_5
  IL_0055:  call       ""int System.Runtime.CompilerServices.RuntimeHelpers.OffsetToStringData.get""
  IL_005a:  add
  IL_005b:  stloc.s    V_5
  IL_005d:  br.s       IL_005f
  IL_005f:  ldarg.2
  IL_0060:  stloc.s    V_9
  IL_0062:  ldloc.s    V_9
  IL_0064:  conv.i
  IL_0065:  stloc.s    V_6
  IL_0067:  ldloc.s    V_9
  IL_0069:  conv.i
  IL_006a:  ldnull
  IL_006b:  ceq
  IL_006d:  stloc.s    V_13
  IL_006f:  ldloc.s    V_13
  IL_0071:  brtrue.s   IL_007f
  IL_0073:  ldloc.s    V_6
  IL_0075:  call       ""int System.Runtime.CompilerServices.RuntimeHelpers.OffsetToStringData.get""
  IL_007a:  add
  IL_007b:  stloc.s    V_6
  IL_007d:  br.s       IL_007f
  IL_007f:  ldarg.3
  IL_0080:  stloc.s    V_10
  IL_0082:  ldloc.s    V_10
  IL_0084:  conv.i
  IL_0085:  stloc.s    V_7
  IL_0087:  ldloc.s    V_10
  IL_0089:  conv.i
  IL_008a:  ldnull
  IL_008b:  ceq
  IL_008d:  stloc.s    V_13
  IL_008f:  ldloc.s    V_13
  IL_0091:  brtrue.s   IL_009f
  IL_0093:  ldloc.s    V_7
  IL_0095:  call       ""int System.Runtime.CompilerServices.RuntimeHelpers.OffsetToStringData.get""
  IL_009a:  add
  IL_009b:  stloc.s    V_7
  IL_009d:  br.s       IL_009f
  IL_009f:  nop
  IL_00a0:  ldloc.s    V_5
  IL_00a2:  ldloc.s    V_7
  IL_00a4:  ldind.u2
  IL_00a5:  stind.i2
  IL_00a6:  ldloc.s    V_7
  IL_00a8:  ldloc.s    V_6
  IL_00aa:  ldind.u2
  IL_00ab:  stind.i2
  IL_00ac:  ldarg.1
  IL_00ad:  stloc.s    V_12
  IL_00af:  ldloc.s    V_12
  IL_00b1:  conv.i
  IL_00b2:  stloc.s    V_11
  IL_00b4:  ldloc.s    V_12
  IL_00b6:  conv.i
  IL_00b7:  ldnull
  IL_00b8:  ceq
  IL_00ba:  stloc.s    V_13
  IL_00bc:  ldloc.s    V_13
  IL_00be:  brtrue.s   IL_00cc
  IL_00c0:  ldloc.s    V_11
  IL_00c2:  call       ""int System.Runtime.CompilerServices.RuntimeHelpers.OffsetToStringData.get""
  IL_00c7:  add
  IL_00c8:  stloc.s    V_11
  IL_00ca:  br.s       IL_00cc
  IL_00cc:  nop
  IL_00cd:  ldloc.s    V_6
  IL_00cf:  ldloc.s    V_11
  IL_00d1:  ldind.u2
  IL_00d2:  stind.i2
  IL_00d3:  nop
  IL_00d4:  ldnull
  IL_00d5:  stloc.s    V_12
  IL_00d7:  nop
  IL_00d8:  ldnull
  IL_00d9:  stloc.s    V_8
  IL_00db:  ldnull
  IL_00dc:  stloc.s    V_9
  IL_00de:  ldnull
  IL_00df:  stloc.s    V_10
  IL_00e1:  ret
}
");
        }

        [Fact]
        public void TemporaryLocals_ForEach()
        {
            var source =
@"using System.Collections;
using System.Collections.Generic;
class C
{
    static IEnumerable F1() { return null; }
    static List<object> F2() { return null; }
    static IEnumerable F3() { return null; }
    static List<object> F4() { return null; }
    static void M()
    {
        foreach (var @x/*CS$5$0000*/ in F1())
        {
            foreach (object y/*CS$5$0001*/ in F2()) { }
        }
        foreach (var x/*CS$5$0001*/ in F4())
        {
            foreach (var y/*CS$5$0000*/ in F3()) { }
            foreach (var z/*CS$5$0004*/ in F2()) { }
        }
    }
}";
            var compilation0 = CreateCompilationWithMscorlib(source, compOptions: TestOptions.UnoptimizedDll.WithDebugInformationKind(DebugInformationKind.Full));
            var compilation1 = CreateCompilationWithMscorlib(source, compOptions: TestOptions.UnoptimizedDll.WithDebugInformationKind(DebugInformationKind.Full));

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(debug: true, testData: testData0);
            var methodData0 = testData0.GetMethodData("C.M");
            var method0 = compilation0.GetMember<MethodSymbol>("C.M");
            var generation0 = EmitBaseline.CreateInitialBaseline(
                ModuleMetadata.CreateFromImage(bytes0),
                m => GetLocalNames(methodData0));

            var method1 = compilation1.GetMember<MethodSymbol>("C.M");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetLocalMap(method1, method0), preserveLocalVariables: true)));

            diff1.VerifyIL("C.M",
@"{
  // Code size      309 (0x135)
  .maxstack  2
  .locals init (System.Collections.IEnumerator V_0, //CS$5$0000
  object V_1, //x
  System.Collections.Generic.List<object>.Enumerator V_2, //CS$5$0001
  object V_3, //y
  bool V_4,
  System.IDisposable V_5,
  System.Collections.Generic.List<object>.Enumerator V_6, //CS$5$0002
  object V_7, //x
  System.Collections.IEnumerator V_8, //CS$5$0003
  object V_9, //y
  System.Collections.Generic.List<object>.Enumerator V_10, //CS$5$0004
  object V_11, //z
  bool V_12,
  System.IDisposable V_13)
  IL_0000:  nop
  IL_0001:  nop
  IL_0002:  call       ""System.Collections.IEnumerable C.F1()""
  IL_0007:  callvirt   ""System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()""
  IL_000c:  stloc.0
  .try
{
  IL_000d:  br.s       IL_004e
  IL_000f:  ldloc.0
  IL_0010:  callvirt   ""object System.Collections.IEnumerator.Current.get""
  IL_0015:  stloc.1
  IL_0016:  nop
  IL_0017:  nop
  IL_0018:  call       ""System.Collections.Generic.List<object> C.F2()""
  IL_001d:  callvirt   ""System.Collections.Generic.List<object>.Enumerator System.Collections.Generic.List<object>.GetEnumerator()""
  IL_0022:  stloc.2
  .try
{
  IL_0023:  br.s       IL_002f
  IL_0025:  ldloca.s   V_2
  IL_0027:  call       ""object System.Collections.Generic.List<object>.Enumerator.Current.get""
  IL_002c:  stloc.3
  IL_002d:  nop
  IL_002e:  nop
  IL_002f:  ldloca.s   V_2
  IL_0031:  call       ""bool System.Collections.Generic.List<object>.Enumerator.MoveNext()""
  IL_0036:  stloc.s    V_12
  IL_0038:  ldloc.s    V_12
  IL_003a:  brtrue.s   IL_0025
  IL_003c:  leave.s    IL_004d
}
  finally
{
  IL_003e:  ldloca.s   V_2
  IL_0040:  constrained. ""System.Collections.Generic.List<object>.Enumerator""
  IL_0046:  callvirt   ""void System.IDisposable.Dispose()""
  IL_004b:  nop
  IL_004c:  endfinally
}
  IL_004d:  nop
  IL_004e:  ldloc.0
  IL_004f:  callvirt   ""bool System.Collections.IEnumerator.MoveNext()""
  IL_0054:  stloc.s    V_12
  IL_0056:  ldloc.s    V_12
  IL_0058:  brtrue.s   IL_000f
  IL_005a:  leave.s    IL_0078
}
  finally
{
  IL_005c:  ldloc.0
  IL_005d:  isinst     ""System.IDisposable""
  IL_0062:  stloc.s    V_13
  IL_0064:  ldloc.s    V_13
  IL_0066:  ldnull
  IL_0067:  ceq
  IL_0069:  stloc.s    V_12
  IL_006b:  ldloc.s    V_12
  IL_006d:  brtrue.s   IL_0077
  IL_006f:  ldloc.s    V_13
  IL_0071:  callvirt   ""void System.IDisposable.Dispose()""
  IL_0076:  nop
  IL_0077:  endfinally
}
  IL_0078:  nop
  IL_0079:  call       ""System.Collections.Generic.List<object> C.F4()""
  IL_007e:  callvirt   ""System.Collections.Generic.List<object>.Enumerator System.Collections.Generic.List<object>.GetEnumerator()""
  IL_0083:  stloc.s    V_6
  .try
{
  IL_0085:  br         IL_0113
  IL_008a:  ldloca.s   V_6
  IL_008c:  call       ""object System.Collections.Generic.List<object>.Enumerator.Current.get""
  IL_0091:  stloc.s    V_7
  IL_0093:  nop
  IL_0094:  nop
  IL_0095:  call       ""System.Collections.IEnumerable C.F3()""
  IL_009a:  callvirt   ""System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()""
  IL_009f:  stloc.s    V_8
  .try
{
  IL_00a1:  br.s       IL_00ae
  IL_00a3:  ldloc.s    V_8
  IL_00a5:  callvirt   ""object System.Collections.IEnumerator.Current.get""
  IL_00aa:  stloc.s    V_9
  IL_00ac:  nop
  IL_00ad:  nop
  IL_00ae:  ldloc.s    V_8
  IL_00b0:  callvirt   ""bool System.Collections.IEnumerator.MoveNext()""
  IL_00b5:  stloc.s    V_12
  IL_00b7:  ldloc.s    V_12
  IL_00b9:  brtrue.s   IL_00a3
  IL_00bb:  leave.s    IL_00da
}
  finally
{
  IL_00bd:  ldloc.s    V_8
  IL_00bf:  isinst     ""System.IDisposable""
  IL_00c4:  stloc.s    V_13
  IL_00c6:  ldloc.s    V_13
  IL_00c8:  ldnull
  IL_00c9:  ceq
  IL_00cb:  stloc.s    V_12
  IL_00cd:  ldloc.s    V_12
  IL_00cf:  brtrue.s   IL_00d9
  IL_00d1:  ldloc.s    V_13
  IL_00d3:  callvirt   ""void System.IDisposable.Dispose()""
  IL_00d8:  nop
  IL_00d9:  endfinally
}
  IL_00da:  nop
  IL_00db:  call       ""System.Collections.Generic.List<object> C.F2()""
  IL_00e0:  callvirt   ""System.Collections.Generic.List<object>.Enumerator System.Collections.Generic.List<object>.GetEnumerator()""
  IL_00e5:  stloc.s    V_10
  .try
{
  IL_00e7:  br.s       IL_00f4
  IL_00e9:  ldloca.s   V_10
  IL_00eb:  call       ""object System.Collections.Generic.List<object>.Enumerator.Current.get""
  IL_00f0:  stloc.s    V_11
  IL_00f2:  nop
  IL_00f3:  nop
  IL_00f4:  ldloca.s   V_10
  IL_00f6:  call       ""bool System.Collections.Generic.List<object>.Enumerator.MoveNext()""
  IL_00fb:  stloc.s    V_12
  IL_00fd:  ldloc.s    V_12
  IL_00ff:  brtrue.s   IL_00e9
  IL_0101:  leave.s    IL_0112
}
  finally
{
  IL_0103:  ldloca.s   V_10
  IL_0105:  constrained. ""System.Collections.Generic.List<object>.Enumerator""
  IL_010b:  callvirt   ""void System.IDisposable.Dispose()""
  IL_0110:  nop
  IL_0111:  endfinally
}
  IL_0112:  nop
  IL_0113:  ldloca.s   V_6
  IL_0115:  call       ""bool System.Collections.Generic.List<object>.Enumerator.MoveNext()""
  IL_011a:  stloc.s    V_12
  IL_011c:  ldloc.s    V_12
  IL_011e:  brtrue     IL_008a
  IL_0123:  leave.s    IL_0134
}
  finally
{
  IL_0125:  ldloca.s   V_6
  IL_0127:  constrained. ""System.Collections.Generic.List<object>.Enumerator""
  IL_012d:  callvirt   ""void System.IDisposable.Dispose()""
  IL_0132:  nop
  IL_0133:  endfinally
}
  IL_0134:  ret
}");
        }

        [Fact]
        public void TemporaryLocals_ForEachArray()
        {
            var source =
@"class C
{
    static void M(string a, object[] b, double[,,] c)
    {
        foreach (var x in a)
        {
            foreach (var y in b)
            {
            }
        }
        foreach (var x in c)
        {
        }
    }
}";
            var compilation0 = CreateCompilationWithMscorlib(source, compOptions: TestOptions.UnoptimizedDll.WithDebugInformationKind(DebugInformationKind.Full));
            var compilation1 = CreateCompilationWithMscorlib(source, compOptions: TestOptions.UnoptimizedDll.WithDebugInformationKind(DebugInformationKind.Full));

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(debug: true, testData: testData0);
            var methodData0 = testData0.GetMethodData("C.M");
            var method0 = compilation0.GetMember<MethodSymbol>("C.M");
            var generation0 = EmitBaseline.CreateInitialBaseline(
                ModuleMetadata.CreateFromImage(bytes0),
                m => GetLocalNames(methodData0));

            var method1 = compilation1.GetMember<MethodSymbol>("C.M");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetLocalMap(method1, method0), preserveLocalVariables: true)));

            diff1.VerifyIL("C.M",
@"
{
  // Code size      223 (0xdf)
  .maxstack  4
  .locals init (string V_0, //CS$6$0000
  int V_1, //CS$7$0001
  char V_2, //x
  object[] V_3, //CS$6$0002
  int V_4, //CS$7$0003
  object V_5, //y
  bool V_6,
  double[,,] V_7, //CS$6$0004
  int V_8, //CS$263$0005
  int V_9, //CS$264$0006
  int V_10, //CS$265$0007
  int V_11, //CS$7$0008
  int V_12, //CS$8$0009
  int V_13, //CS$9$0010
  double V_14, //x
  bool V_15)
  IL_0000:  nop
  IL_0001:  nop
  IL_0002:  ldarg.0
  IL_0003:  stloc.0
  IL_0004:  ldc.i4.0
  IL_0005:  stloc.1
  IL_0006:  br.s       IL_0039
  IL_0008:  ldloc.0
  IL_0009:  ldloc.1
  IL_000a:  callvirt   ""char string.this[int].get""
  IL_000f:  stloc.2
  IL_0010:  nop
  IL_0011:  nop
  IL_0012:  ldarg.1
  IL_0013:  stloc.3
  IL_0014:  ldc.i4.0
  IL_0015:  stloc.s    V_4
  IL_0017:  br.s       IL_0027
  IL_0019:  ldloc.3
  IL_001a:  ldloc.s    V_4
  IL_001c:  ldelem.ref
  IL_001d:  stloc.s    V_5
  IL_001f:  nop
  IL_0020:  nop
  IL_0021:  ldloc.s    V_4
  IL_0023:  ldc.i4.1
  IL_0024:  add
  IL_0025:  stloc.s    V_4
  IL_0027:  ldloc.s    V_4
  IL_0029:  ldloc.3
  IL_002a:  ldlen
  IL_002b:  conv.i4
  IL_002c:  clt
  IL_002e:  stloc.s    V_15
  IL_0030:  ldloc.s    V_15
  IL_0032:  brtrue.s   IL_0019
  IL_0034:  nop
  IL_0035:  ldloc.1
  IL_0036:  ldc.i4.1
  IL_0037:  add
  IL_0038:  stloc.1
  IL_0039:  ldloc.1
  IL_003a:  ldloc.0
  IL_003b:  callvirt   ""int string.Length.get""
  IL_0040:  clt
  IL_0042:  stloc.s    V_15
  IL_0044:  ldloc.s    V_15
  IL_0046:  brtrue.s   IL_0008
  IL_0048:  nop
  IL_0049:  ldarg.2
  IL_004a:  stloc.s    V_7
  IL_004c:  ldloc.s    V_7
  IL_004e:  ldc.i4.0
  IL_004f:  callvirt   ""int System.Array.GetUpperBound(int)""
  IL_0054:  stloc.s    V_8
  IL_0056:  ldloc.s    V_7
  IL_0058:  ldc.i4.1
  IL_0059:  callvirt   ""int System.Array.GetUpperBound(int)""
  IL_005e:  stloc.s    V_9
  IL_0060:  ldloc.s    V_7
  IL_0062:  ldc.i4.2
  IL_0063:  callvirt   ""int System.Array.GetUpperBound(int)""
  IL_0068:  stloc.s    V_10
  IL_006a:  ldloc.s    V_7
  IL_006c:  ldc.i4.0
  IL_006d:  callvirt   ""int System.Array.GetLowerBound(int)""
  IL_0072:  stloc.s    V_11
  IL_0074:  br.s       IL_00cf
  IL_0076:  ldloc.s    V_7
  IL_0078:  ldc.i4.1
  IL_0079:  callvirt   ""int System.Array.GetLowerBound(int)""
  IL_007e:  stloc.s    V_12
  IL_0080:  br.s       IL_00ba
  IL_0082:  ldloc.s    V_7
  IL_0084:  ldc.i4.2
  IL_0085:  callvirt   ""int System.Array.GetLowerBound(int)""
  IL_008a:  stloc.s    V_13
  IL_008c:  br.s       IL_00a5
  IL_008e:  ldloc.s    V_7
  IL_0090:  ldloc.s    V_11
  IL_0092:  ldloc.s    V_12
  IL_0094:  ldloc.s    V_13
  IL_0096:  call       ""double[*,*,*].Get""
  IL_009b:  stloc.s    V_14
  IL_009d:  nop
  IL_009e:  nop
  IL_009f:  ldloc.s    V_13
  IL_00a1:  ldc.i4.1
  IL_00a2:  add
  IL_00a3:  stloc.s    V_13
  IL_00a5:  ldloc.s    V_13
  IL_00a7:  ldloc.s    V_10
  IL_00a9:  cgt
  IL_00ab:  ldc.i4.0
  IL_00ac:  ceq
  IL_00ae:  stloc.s    V_15
  IL_00b0:  ldloc.s    V_15
  IL_00b2:  brtrue.s   IL_008e
  IL_00b4:  ldloc.s    V_12
  IL_00b6:  ldc.i4.1
  IL_00b7:  add
  IL_00b8:  stloc.s    V_12
  IL_00ba:  ldloc.s    V_12
  IL_00bc:  ldloc.s    V_9
  IL_00be:  cgt
  IL_00c0:  ldc.i4.0
  IL_00c1:  ceq
  IL_00c3:  stloc.s    V_15
  IL_00c5:  ldloc.s    V_15
  IL_00c7:  brtrue.s   IL_0082
  IL_00c9:  ldloc.s    V_11
  IL_00cb:  ldc.i4.1
  IL_00cc:  add
  IL_00cd:  stloc.s    V_11
  IL_00cf:  ldloc.s    V_11
  IL_00d1:  ldloc.s    V_8
  IL_00d3:  cgt
  IL_00d5:  ldc.i4.0
  IL_00d6:  ceq
  IL_00d8:  stloc.s    V_15
  IL_00da:  ldloc.s    V_15
  IL_00dc:  brtrue.s   IL_0076
  IL_00de:  ret
}");
        }

        /// <summary>
        /// TempKind expects array with at most 256 dimensions.
        /// (Should any edits in such cases be considered rude edits?
        /// Or should we generate compile errors since the CLR throws
        /// TypeLoadException if the number of dimensions exceeds 256?)
        /// </summary>
        //[Fact(Skip = "ArgumentException")]
        public void TemporaryLocals_ForEachArray_Overflow()
        {
            var source =
@"class C
{
    static void M(object o)
    {
        foreach (var x in (object[,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,])o)
        {
        }
    }
}";
            // Make sure the source contains an array with too many dimensions.
            var tooManyCommas = new string(',', 256);
            Assert.True(source.IndexOf(tooManyCommas) > 0);

            var compilation0 = CreateCompilationWithMscorlib(source, compOptions: TestOptions.UnoptimizedDll.WithDebugInformationKind(DebugInformationKind.Full));
            var compilation1 = CreateCompilationWithMscorlib(source, compOptions: TestOptions.UnoptimizedDll.WithDebugInformationKind(DebugInformationKind.Full));

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(debug: true, testData: testData0);
            var methodData0 = testData0.GetMethodData("C.M");
            var method0 = compilation0.GetMember<MethodSymbol>("C.M");
            var generation0 = EmitBaseline.CreateInitialBaseline(
                ModuleMetadata.CreateFromImage(bytes0),
                m => GetLocalNames(methodData0));

            var method1 = compilation1.GetMember<MethodSymbol>("C.M");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetLocalMap(method1, method0), preserveLocalVariables: true)));
        }

        [Fact]
        public void TemporaryLocals_AddAndDelete()
        {
            var source0 =
@"class C
{
    static object F1() { return null; }
    static string F2() { return null; }
    static System.IDisposable F3() { return null; }
    static void M()
    {
        lock (F1()) { }
        foreach (var c in F2()) { }
        using (F3()) { }
    }
}";
            // Delete one statement.
            var source1 =
@"class C
{
    static object F1() { return null; }
    static string F2() { return null; }
    static System.IDisposable F3() { return null; }
    static void M()
    {
        lock (F1()) { }
        foreach (var c in F2()) { }
    }
}";
            // Add statement with same temp kind.
            var source2 =
@"class C
{
    static object F1() { return null; }
    static string F2() { return null; }
    static System.IDisposable F3() { return null; }
    static void M()
    {
        using (F3()) { }
        lock (F1()) { }
        foreach (var c in F2()) { }
    }
}";
            var compilation0 = CreateCompilationWithMscorlib(source0, compOptions: TestOptions.UnoptimizedDll.WithDebugInformationKind(DebugInformationKind.Full));
            var compilation1 = CreateCompilationWithMscorlib(source1, compOptions: TestOptions.UnoptimizedDll.WithDebugInformationKind(DebugInformationKind.Full));
            var compilation2 = CreateCompilationWithMscorlib(source2, compOptions: TestOptions.UnoptimizedDll.WithDebugInformationKind(DebugInformationKind.Full));

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(debug: true, testData: testData0);
            var methodData0 = testData0.GetMethodData("C.M");
            var method0 = compilation0.GetMember<MethodSymbol>("C.M");
            var generation0 = EmitBaseline.CreateInitialBaseline(
                ModuleMetadata.CreateFromImage(bytes0),
                m => GetLocalNames(methodData0));

            var method1 = compilation1.GetMember<MethodSymbol>("C.M");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetLocalMap(method1, method0), preserveLocalVariables: true)));

            var method2 = compilation2.GetMember<MethodSymbol>("C.M");
            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method1, method2, GetLocalMap(method2, method1), preserveLocalVariables: true)));

            diff2.VerifyIL("C.M",
@"{
  // Code size      118 (0x76)
  .maxstack  2
  .locals init (object V_0, //CS$2$0001
  bool V_1, //CS$520$0002
  bool V_2,
  string V_3, //CS$6$0003
  int V_4, //CS$7$0004
  char V_5, //c
  System.IDisposable V_6,
  bool V_7,
  System.IDisposable V_8, //CS$3$0000
  bool V_9)
  IL_0000:  nop
  IL_0001:  call       ""System.IDisposable C.F3()""
  IL_0006:  stloc.s    V_8
  .try
{
  IL_0008:  nop
  IL_0009:  nop
  IL_000a:  leave.s    IL_0020
}
  finally
{
  IL_000c:  ldloc.s    V_8
  IL_000e:  ldnull
  IL_000f:  ceq
  IL_0011:  stloc.s    V_9
  IL_0013:  ldloc.s    V_9
  IL_0015:  brtrue.s   IL_001f
  IL_0017:  ldloc.s    V_8
  IL_0019:  callvirt   ""void System.IDisposable.Dispose()""
  IL_001e:  nop
  IL_001f:  endfinally
}
  IL_0020:  ldc.i4.0
  IL_0021:  stloc.1
  .try
{
  IL_0022:  call       ""object C.F1()""
  IL_0027:  stloc.0
  IL_0028:  ldloc.0
  IL_0029:  ldloca.s   V_1
  IL_002b:  call       ""void System.Threading.Monitor.Enter(object, ref bool)""
  IL_0030:  nop
  IL_0031:  nop
  IL_0032:  nop
  IL_0033:  leave.s    IL_0047
}
  finally
{
  IL_0035:  ldloc.1
  IL_0036:  ldc.i4.0
  IL_0037:  ceq
  IL_0039:  stloc.s    V_9
  IL_003b:  ldloc.s    V_9
  IL_003d:  brtrue.s   IL_0046
  IL_003f:  ldloc.0
  IL_0040:  call       ""void System.Threading.Monitor.Exit(object)""
  IL_0045:  nop
  IL_0046:  endfinally
}
  IL_0047:  nop
  IL_0048:  call       ""string C.F2()""
  IL_004d:  stloc.3
  IL_004e:  ldc.i4.0
  IL_004f:  stloc.s    V_4
  IL_0051:  br.s       IL_0065
  IL_0053:  ldloc.3
  IL_0054:  ldloc.s    V_4
  IL_0056:  callvirt   ""char string.this[int].get""
  IL_005b:  stloc.s    V_5
  IL_005d:  nop
  IL_005e:  nop
  IL_005f:  ldloc.s    V_4
  IL_0061:  ldc.i4.1
  IL_0062:  add
  IL_0063:  stloc.s    V_4
  IL_0065:  ldloc.s    V_4
  IL_0067:  ldloc.3
  IL_0068:  callvirt   ""int string.Length.get""
  IL_006d:  clt
  IL_006f:  stloc.s    V_9
  IL_0071:  ldloc.s    V_9
  IL_0073:  brtrue.s   IL_0053
  IL_0075:  ret
}");
        }

        [Fact]
        public void TemporaryLocals_Insert()
        {
            var source0 =
@"class C
{
    static object F1() { return null; }
    static object F2() { return null; }
    static object F3() { return null; }
    static object F4() { return null; }
    static void M()
    {
        lock (F1()) { }
        lock (F2()) { }
    }
}";
            var source1 =
@"class C
{
    static object F1() { return null; }
    static object F2() { return null; }
    static object F3() { return null; }
    static object F4() { return null; }
    static void M()
    {
        lock (F3()) { } // added
        lock (F1()) { }
        lock (F4()) { } // replaced
    }
}";
            var compilation0 = CreateCompilationWithMscorlib(source0, compOptions: TestOptions.UnoptimizedDll.WithDebugInformationKind(DebugInformationKind.Full));
            var compilation1 = CreateCompilationWithMscorlib(source1, compOptions: TestOptions.UnoptimizedDll.WithDebugInformationKind(DebugInformationKind.Full));

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(debug: true, testData: testData0);
            var methodData0 = testData0.GetMethodData("C.M");
            var method0 = compilation0.GetMember<MethodSymbol>("C.M");
            var generation0 = EmitBaseline.CreateInitialBaseline(
                ModuleMetadata.CreateFromImage(bytes0),
                m => GetLocalNames(methodData0));

            var method1 = compilation1.GetMember<MethodSymbol>("C.M");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetLocalMap(method1, method0), preserveLocalVariables: true)));

            // Note that the order of unique ids in temporaries follows the
            // order of declaration in the updated method. Specifically, the
            // original temporary names (and unique ids) are not preserved.
            // (Should not be an issue since the names are used by EnC only.)
            diff1.VerifyIL("C.M",
@"{
  // Code size      129 (0x81)
  .maxstack  2
  .locals init (object V_0, //CS$2$0002
  bool V_1, //CS$520$0003
  bool V_2,
  object V_3,
  bool V_4,
  object V_5, //CS$2$0000
  bool V_6, //CS$520$0001
  bool V_7,
  object V_8, //CS$2$0004
  bool V_9) //CS$520$0005
  IL_0000:  nop
  IL_0001:  ldc.i4.0
  IL_0002:  stloc.s    V_6
  .try
{
  IL_0004:  call       ""object C.F3()""
  IL_0009:  stloc.s    V_5
  IL_000b:  ldloc.s    V_5
  IL_000d:  ldloca.s   V_6
  IL_000f:  call       ""void System.Threading.Monitor.Enter(object, ref bool)""
  IL_0014:  nop
  IL_0015:  nop
  IL_0016:  nop
  IL_0017:  leave.s    IL_002d
}
  finally
{
  IL_0019:  ldloc.s    V_6
  IL_001b:  ldc.i4.0
  IL_001c:  ceq
  IL_001e:  stloc.s    V_7
  IL_0020:  ldloc.s    V_7
  IL_0022:  brtrue.s   IL_002c
  IL_0024:  ldloc.s    V_5
  IL_0026:  call       ""void System.Threading.Monitor.Exit(object)""
  IL_002b:  nop
  IL_002c:  endfinally
}
  IL_002d:  ldc.i4.0
  IL_002e:  stloc.1
  .try
{
  IL_002f:  call       ""object C.F1()""
  IL_0034:  stloc.0
  IL_0035:  ldloc.0
  IL_0036:  ldloca.s   V_1
  IL_0038:  call       ""void System.Threading.Monitor.Enter(object, ref bool)""
  IL_003d:  nop
  IL_003e:  nop
  IL_003f:  nop
  IL_0040:  leave.s    IL_0054
}
  finally
{
  IL_0042:  ldloc.1
  IL_0043:  ldc.i4.0
  IL_0044:  ceq
  IL_0046:  stloc.s    V_7
  IL_0048:  ldloc.s    V_7
  IL_004a:  brtrue.s   IL_0053
  IL_004c:  ldloc.0
  IL_004d:  call       ""void System.Threading.Monitor.Exit(object)""
  IL_0052:  nop
  IL_0053:  endfinally
}
  IL_0054:  ldc.i4.0
  IL_0055:  stloc.s    V_9
  .try
{
  IL_0057:  call       ""object C.F4()""
  IL_005c:  stloc.s    V_8
  IL_005e:  ldloc.s    V_8
  IL_0060:  ldloca.s   V_9
  IL_0062:  call       ""void System.Threading.Monitor.Enter(object, ref bool)""
  IL_0067:  nop
  IL_0068:  nop
  IL_0069:  nop
  IL_006a:  leave.s    IL_0080
}
  finally
{
  IL_006c:  ldloc.s    V_9
  IL_006e:  ldc.i4.0
  IL_006f:  ceq
  IL_0071:  stloc.s    V_7
  IL_0073:  ldloc.s    V_7
  IL_0075:  brtrue.s   IL_007f
  IL_0077:  ldloc.s    V_8
  IL_0079:  call       ""void System.Threading.Monitor.Exit(object)""
  IL_007e:  nop
  IL_007f:  endfinally
}
  IL_0080:  ret
}");
        }

        /// <summary>
        /// Should not reuse temporary locals
        /// having different temporary kinds.
        /// </summary>
        [Fact]
        public void TemporaryLocals_NoReuseDifferentTempKind()
        {
            var source =
@"class A : System.IDisposable
{
    public object Current { get { return null; } }
    public bool MoveNext() { return false; }
    public void Dispose() { }
    internal int this[A a] { get { return 0; } set { } }
}
class B
{
    public A GetEnumerator() { return null; }
}
class C
{
    static A F() { return null; }
    static B G() { return null; }
    static void M(A a)
    {
        a[/*V_9*/F()]++;
        using (/*CS$3$0000*/F()) { }
        lock (/*CS$2$0001*/F()) { }
        foreach (var o in /*CS$5$0003*/G()) { }
    }
}";

            var compilation0 = CreateCompilationWithMscorlib(source, compOptions: TestOptions.UnoptimizedDll.WithDebugInformationKind(DebugInformationKind.Full));
            var compilation1 = CreateCompilationWithMscorlib(source, compOptions: TestOptions.UnoptimizedDll.WithDebugInformationKind(DebugInformationKind.Full));

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(debug: true, testData: testData0);
            var methodData0 = testData0.GetMethodData("C.M");
            var method0 = compilation0.GetMember<MethodSymbol>("C.M");
            var generation0 = EmitBaseline.CreateInitialBaseline(
                ModuleMetadata.CreateFromImage(bytes0),
                m => GetLocalNames(methodData0));

            var method1 = compilation1.GetMember<MethodSymbol>("C.M");
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetLocalMap(method1, method0), preserveLocalVariables: true)));

            diff1.VerifyIL("C.M",
@"{
  // Code size      170 (0xaa)
  .maxstack  4
  .locals init (A V_0,
  A V_1,
  int V_2,
  A V_3, //CS$3$0000
  bool V_4,
  A V_5, //CS$2$0001
  bool V_6, //CS$520$0002
  A V_7, //CS$5$0003
  object V_8, //o
  A V_9,
  A V_10,
  int V_11,
  bool V_12)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.s    V_9
  IL_0004:  call       ""A C.F()""
  IL_0009:  stloc.s    V_10
  IL_000b:  ldloc.s    V_9
  IL_000d:  ldloc.s    V_10
  IL_000f:  callvirt   ""int A.this[A].get""
  IL_0014:  stloc.s    V_11
  IL_0016:  ldloc.s    V_9
  IL_0018:  ldloc.s    V_10
  IL_001a:  ldloc.s    V_11
  IL_001c:  ldc.i4.1
  IL_001d:  add
  IL_001e:  callvirt   ""void A.this[A].set""
  IL_0023:  nop
  IL_0024:  call       ""A C.F()""
  IL_0029:  stloc.3
  .try
{
  IL_002a:  nop
  IL_002b:  nop
  IL_002c:  leave.s    IL_0040
}
  finally
{
  IL_002e:  ldloc.3
  IL_002f:  ldnull
  IL_0030:  ceq
  IL_0032:  stloc.s    V_12
  IL_0034:  ldloc.s    V_12
  IL_0036:  brtrue.s   IL_003f
  IL_0038:  ldloc.3
  IL_0039:  callvirt   ""void System.IDisposable.Dispose()""
  IL_003e:  nop
  IL_003f:  endfinally
}
  IL_0040:  ldc.i4.0
  IL_0041:  stloc.s    V_6
  .try
{
  IL_0043:  call       ""A C.F()""
  IL_0048:  stloc.s    V_5
  IL_004a:  ldloc.s    V_5
  IL_004c:  ldloca.s   V_6
  IL_004e:  call       ""void System.Threading.Monitor.Enter(object, ref bool)""
  IL_0053:  nop
  IL_0054:  nop
  IL_0055:  nop
  IL_0056:  leave.s    IL_006c
}
  finally
{
  IL_0058:  ldloc.s    V_6
  IL_005a:  ldc.i4.0
  IL_005b:  ceq
  IL_005d:  stloc.s    V_12
  IL_005f:  ldloc.s    V_12
  IL_0061:  brtrue.s   IL_006b
  IL_0063:  ldloc.s    V_5
  IL_0065:  call       ""void System.Threading.Monitor.Exit(object)""
  IL_006a:  nop
  IL_006b:  endfinally
}
  IL_006c:  nop
  IL_006d:  call       ""B C.G()""
  IL_0072:  callvirt   ""A B.GetEnumerator()""
  IL_0077:  stloc.s    V_7
  .try
{
  IL_0079:  br.s       IL_0086
  IL_007b:  ldloc.s    V_7
  IL_007d:  callvirt   ""object A.Current.get""
  IL_0082:  stloc.s    V_8
  IL_0084:  nop
  IL_0085:  nop
  IL_0086:  ldloc.s    V_7
  IL_0088:  callvirt   ""bool A.MoveNext()""
  IL_008d:  stloc.s    V_12
  IL_008f:  ldloc.s    V_12
  IL_0091:  brtrue.s   IL_007b
  IL_0093:  leave.s    IL_00a9
}
  finally
{
  IL_0095:  ldloc.s    V_7
  IL_0097:  ldnull
  IL_0098:  ceq
  IL_009a:  stloc.s    V_12
  IL_009c:  ldloc.s    V_12
  IL_009e:  brtrue.s   IL_00a8
  IL_00a0:  ldloc.s    V_7
  IL_00a2:  callvirt   ""void System.IDisposable.Dispose()""
  IL_00a7:  nop
  IL_00a8:  endfinally
}
  IL_00a9:  ret
}");
        }

        [Fact]
        public void SymbolMatcher_ConcurrentAccess()
        {
            var source =
@"class A
{
    B F;
    D P { get; set; }
    void M(A a, B b, S s, I i) { }
    delegate void D(S s);
    class B { }
    struct S { }
    interface I { }
}
class B
{
    A M<T, U>() where T : A where U : T, I { return null; }
    event D E;
    delegate void D(S s);
    struct S { }
    interface I { }
}";

            var compilation0 = CreateCompilationWithMscorlib(source, compOptions: TestOptions.UnoptimizedDll);
            var compilation1 = CreateCompilationWithMscorlib(source, compOptions: TestOptions.UnoptimizedDll);

            var builder = ArrayBuilder<Symbol>.GetInstance();
            var type = compilation1.GetMember<NamedTypeSymbol>("A");
            builder.Add(type);
            builder.AddRange(type.GetMembers());
            type = compilation1.GetMember<NamedTypeSymbol>("B");
            builder.Add(type);
            builder.AddRange(type.GetMembers());
            var members = builder.ToImmutableAndFree();
            Assert.True(members.Length > 10);

            for (int i = 0; i < 10; i++)
            {
                var matcher = new SymbolMatcher(
                    null,
                    compilation1.SourceAssembly,
                    default(Microsoft.CodeAnalysis.Emit.Context),
                    compilation0.SourceAssembly,
                    default(Microsoft.CodeAnalysis.Emit.Context));
                var tasks = new Task[10];
                for (int j = 0; j < tasks.Length; j++)
                {
                    int startAt = i + j + 1;
                    tasks[j] = Task.Run(() =>
                    {
                        MatchAll(matcher, members, startAt);
                        Thread.Sleep(10);
                    });
                }
                Task.WaitAll(tasks);
            }
        }

        private static void MatchAll(SymbolMatcher matcher, ImmutableArray<Symbol> members, int startAt)
        {
            int n = members.Length;
            for (int i = 0; i < n; i++)
            {
                var member = members[(i + startAt) % n];
                var other = matcher.MapDefinition((Cci.IDefinition)member);
                Assert.NotNull(other);
            }
        }

        [Fact]
        public void SymbolMatcher_TypeArguments()
        {
            const string source =
@"class A<T>
{
    class B<U>
    {
        static A<V> M<V>(A<U>.B<T> x, A<object>.S y)
        {
            return null;
        }
        static A<V> M<V>(A<U>.B<T> x, A<V>.S y)
        {
            return null;
        }
    }
    struct S
    {
    }
}";
            var compilation0 = CreateCompilationWithMscorlib(source, compOptions: TestOptions.UnoptimizedDll);
            var compilation1 = CreateCompilationWithMscorlib(source, compOptions: TestOptions.UnoptimizedDll);

            var matcher = new SymbolMatcher(
                null,
                compilation1.SourceAssembly,
                default(Microsoft.CodeAnalysis.Emit.Context),
                compilation0.SourceAssembly,
                default(Microsoft.CodeAnalysis.Emit.Context));
            var members = compilation1.GetMember<NamedTypeSymbol>("A.B").GetMembers("M");
            Assert.Equal(members.Length, 2);
            foreach (var member in members)
            {
                var other = matcher.MapDefinition((Cci.IMethodDefinition)member);
                Assert.NotNull(other);
            }
        }

        [Fact]
        public void SymbolMatcher_Constraints()
        {
            const string source =
@"interface I<T> where T : I<T>
{
}
class C
{
    static void M<T>(I<T> o) where T : I<T>
    {
    }
}";
            var compilation0 = CreateCompilationWithMscorlib(source, compOptions: TestOptions.UnoptimizedDll);
            var compilation1 = CreateCompilationWithMscorlib(source, compOptions: TestOptions.UnoptimizedDll);

            var matcher = new SymbolMatcher(
                null,
                compilation1.SourceAssembly,
                default(Microsoft.CodeAnalysis.Emit.Context),
                compilation0.SourceAssembly,
                default(Microsoft.CodeAnalysis.Emit.Context));
            var member = compilation1.GetMember<MethodSymbol>("C.M");
            var other = matcher.MapDefinition((Cci.IMethodDefinition)member);
            Assert.NotNull(other);
        }

        [Fact]
        public void SymbolMatcher_CustomModifiers()
        {
            var ilSource =
@".class public abstract A
{
  .method public hidebysig specialname rtspecialname instance void .ctor() { ret }
  .method public abstract virtual instance object modopt(A) [] F(int32 modopt(object) *p) { }
}";
            var metadataRef = CompileIL(ilSource);
            const string source =
@"unsafe class B : A
{
    public override object[] F(int* p) { return null; }
}";
            var compilation0 = CreateCompilationWithMscorlib(source, compOptions: TestOptions.UnoptimizedDll.WithAllowUnsafe(true), references: new[] { metadataRef });
            var compilation1 = CreateCompilationWithMscorlib(source, compOptions: TestOptions.UnoptimizedDll.WithAllowUnsafe(true), references: new[] { metadataRef });

            var member1 = compilation1.GetMember<MethodSymbol>("B.F");
            Assert.Equal(((PointerTypeSymbol)member1.Parameters[0].Type).CustomModifiers.Length, 1);
            Assert.Equal(((ArrayTypeSymbol)member1.ReturnType).CustomModifiers.Length, 1);

            var matcher = new SymbolMatcher(
                null,
                compilation1.SourceAssembly,
                default(Microsoft.CodeAnalysis.Emit.Context),
                compilation0.SourceAssembly,
                default(Microsoft.CodeAnalysis.Emit.Context));
            var other = (MethodSymbol)matcher.MapDefinition((Cci.IMethodDefinition)member1);
            Assert.NotNull(other);
            Assert.Equal(((PointerTypeSymbol)other.Parameters[0].Type).CustomModifiers.Length, 1);
            Assert.Equal(((ArrayTypeSymbol)other.ReturnType).CustomModifiers.Length, 1);
        }

        /// <summary>
        /// Disallow edits that include "dynamic" operations.
        /// </summary>
        [WorkItem(770502, "DevDiv")]
        [WorkItem(839565, "DevDiv")]
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
            var compilation0 = CreateCompilationWithMscorlib(source, compOptions: TestOptions.UnoptimizedDll, references: new[] { SystemCoreRef, CSharpRef });
            var compilation1 = CreateCompilationWithMscorlib(source, compOptions: TestOptions.UnoptimizedDll, references: new[] { SystemCoreRef, CSharpRef });

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(debug: true, testData: testData0);
            using (var md0 = ModuleMetadata.CreateFromImage(bytes0))
            {
                // Source method with dynamic operations.
                var methodData0 = testData0.GetMethodData("A.M");
                var generation0 = EmitBaseline.CreateInitialBaseline(md0, m => GetLocalNames(methodData0));
                var method0 = compilation0.GetMember<MethodSymbol>("A.M");
                var method1 = compilation1.GetMember<MethodSymbol>("A.M");
                var diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetLocalMap(method1, method0), preserveLocalVariables: true)));
                diff1.Result.Diagnostics.Verify(
                    // (10,17): error CS7097: Cannot continue since the edit includes an operation on a 'dynamic' type.
                    //     static void M(object o)
                    Diagnostic(ErrorCode.ERR_EnCNoDynamicOperation, "M").WithLocation(10, 17));

                // Source method with no dynamic operations.
                methodData0 = testData0.GetMethodData("A.N");
                generation0 = EmitBaseline.CreateInitialBaseline(md0, m => GetLocalNames(methodData0));
                method0 = compilation0.GetMember<MethodSymbol>("A.N");
                method1 = compilation1.GetMember<MethodSymbol>("A.N");
                diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetLocalMap(method1, method0), preserveLocalVariables: true)));
                diff1.Result.Diagnostics.Verify();

                // Explicit .ctor with dynamic operations.
                methodData0 = testData0.GetMethodData("A..ctor");
                generation0 = EmitBaseline.CreateInitialBaseline(md0, m => GetLocalNames(methodData0));
                method0 = compilation0.GetMember<MethodSymbol>("A..ctor");
                method1 = compilation1.GetMember<MethodSymbol>("A..ctor");
                diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetLocalMap(method1, method0), preserveLocalVariables: true)));
                diff1.Result.Diagnostics.Verify(
                    // (9,5): error CS7097: Cannot continue since the edit includes an operation on a 'dynamic' type.
                    //     A() { }
                    Diagnostic(ErrorCode.ERR_EnCNoDynamicOperation, "A").WithLocation(9, 5));

                // Explicit .cctor with dynamic operations.
                methodData0 = testData0.GetMethodData("A..cctor");
                generation0 = EmitBaseline.CreateInitialBaseline(md0, m => GetLocalNames(methodData0));
                method0 = compilation0.GetMember<MethodSymbol>("A..cctor");
                method1 = compilation1.GetMember<MethodSymbol>("A..cctor");
                diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetLocalMap(method1, method0), preserveLocalVariables: true)));
                diff1.Result.Diagnostics.Verify(
                    // (5,12): error CS7097: Cannot continue since the edit includes an operation on a 'dynamic' type.
                    //     static A()
                    Diagnostic(ErrorCode.ERR_EnCNoDynamicOperation, "A").WithLocation(5, 12));

                // Implicit .ctor with dynamic operations.
                methodData0 = testData0.GetMethodData("B..ctor");
                generation0 = EmitBaseline.CreateInitialBaseline(md0, m => GetLocalNames(methodData0));
                method0 = compilation0.GetMember<MethodSymbol>("B..ctor");
                method1 = compilation1.GetMember<MethodSymbol>("B..ctor");
                diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetLocalMap(method1, method0), preserveLocalVariables: true)));
                diff1.Result.Diagnostics.Verify(
                    // (19,7): error CS7097: Cannot continue since the edit includes an operation on a 'dynamic' type.
                    // class B
                    Diagnostic(ErrorCode.ERR_EnCNoDynamicOperation, "B").WithLocation(19, 7));

                // Implicit .cctor with dynamic operations.
                methodData0 = testData0.GetMethodData("B..cctor");
                generation0 = EmitBaseline.CreateInitialBaseline(md0, m => GetLocalNames(methodData0));
                method0 = compilation0.GetMember<MethodSymbol>("B..cctor");
                method1 = compilation1.GetMember<MethodSymbol>("B..cctor");
                diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetLocalMap(method1, method0), preserveLocalVariables: true)));
                diff1.Result.Diagnostics.Verify(
                    // (19,7): error CS7097: Cannot continue since the edit includes an operation on a 'dynamic' type.
                    // class B
                    Diagnostic(ErrorCode.ERR_EnCNoDynamicOperation, "B").WithLocation(19, 7));
            }
        }

        [WorkItem(844472, "DevDiv")]
        [Fact]
        public void MethodSignatureWithNoPIAType()
        {
        var sourcePIA =
@"using System;
using System.Runtime.InteropServices;
[assembly: ImportedFromTypeLib(""_.dll"")]
[assembly: Guid(""35DB1A6B-D635-4320-A062-28D42920E2A3"")]
[ComImport()]
[Guid(""35DB1A6B-D635-4320-A062-28D42920E2A4"")]
public interface I
{
}";
            var source0 =
@"class C
{
    static void M(I x)
    {
        I y = null;
        M(null);
    }
}";
            var source1 =
@"class C
{
    static void M(I x)
    {
        I y = null;
        M(x);
    }
}";
            var compilationPIA = CreateCompilationWithMscorlib(sourcePIA, compOptions: TestOptions.UnoptimizedDll);
            var referencePIA = new MetadataImageReference(compilationPIA.EmitToArray(), embedInteropTypes: true);
            var compilation0 = CreateCompilationWithMscorlib(source0, compOptions: TestOptions.UnoptimizedDll, references: new MetadataReference[] { referencePIA });
            var compilation1 = CreateCompilationWithMscorlib(source1, compOptions: TestOptions.UnoptimizedDll, references: new MetadataReference[] { referencePIA });

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(debug: true, testData: testData0);
            var methodData0 = testData0.GetMethodData("C.M");
            using (var md0 = ModuleMetadata.CreateFromImage(bytes0))
            {
                var generation0 = EmitBaseline.CreateInitialBaseline(md0, m => GetLocalNames(methodData0));
                var method0 = compilation0.GetMember<MethodSymbol>("C.M");
                var method1 = compilation1.GetMember<MethodSymbol>("C.M");
                var diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetLocalMap(method1, method0), preserveLocalVariables: true)));
                diff1.VerifyIL("C.M",
@"{
  // Code size       11 (0xb)
  .maxstack  1
  .locals init (I V_0,
  I V_1) //y
  IL_0000:  nop
  IL_0001:  ldnull
  IL_0002:  stloc.1
  IL_0003:  ldarg.0
  IL_0004:  call       ""void C.M(I)""
  IL_0009:  nop
  IL_000a:  ret
}");
            }
        }

        /// <summary>
        /// Disallow edits that require NoPIA references.
        /// </summary>
        [Fact]
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
            var compilationPIA = CreateCompilationWithMscorlib(sourcePIA, compOptions: TestOptions.UnoptimizedDll);
            var referencePIA = new MetadataImageReference(compilationPIA.EmitToArray(), embedInteropTypes: true);
            var compilation0 = CreateCompilationWithMscorlib(source0, compOptions: TestOptions.UnoptimizedDll, references: new MetadataReference[] { referencePIA, SystemCoreRef, CSharpRef });
            var compilation1A = CreateCompilationWithMscorlib(source1A, compOptions: TestOptions.UnoptimizedDll, references: new MetadataReference[] { referencePIA, SystemCoreRef, CSharpRef });
            var compilation1B = CreateCompilationWithMscorlib(source1B, compOptions: TestOptions.UnoptimizedDll, references: new MetadataReference[] { referencePIA, SystemCoreRef, CSharpRef });

            var testData0 = new CompilationTestData();
            var bytes0 = compilation0.EmitToArray(debug: true, testData: testData0);
            var methodData0 = testData0.GetMethodData("C<T>.M1");
            using (var md0 = ModuleMetadata.CreateFromImage(bytes0))
            {
                var reader0 = md0.MetadataReader;
                CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "C`1", "IA", "IC", "S");
                var generation0 = EmitBaseline.CreateInitialBaseline(md0, m => GetLocalNames(methodData0));
                var method0 = compilation0.GetMember<MethodSymbol>("C.M1");

                // Disallow edits that require NoPIA references.
                var method1A = compilation1A.GetMember<MethodSymbol>("C.M1");
                var diff1A = compilation1A.EmitDifference(
                    generation0,
                    ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1A, GetLocalMap(method1A, method0), preserveLocalVariables: true)));
                diff1A.Result.Diagnostics.Verify(
                    // error CS7094: Cannot continue since the edit includes a reference to an embedded type: 'S'.
                    Diagnostic(ErrorCode.ERR_EnCNoPIAReference).WithArguments("S"),
                    // error CS7094: Cannot continue since the edit includes a reference to an embedded type: 'IA'.
                    Diagnostic(ErrorCode.ERR_EnCNoPIAReference).WithArguments("IA"));

                // Allow edits that do not require NoPIA references,
                // even if the previous code included references.
                var method1B = compilation1B.GetMember<MethodSymbol>("C.M1");
                var diff1B = compilation1B.EmitDifference(
                    generation0,
                    ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1B, GetLocalMap(method1B, method0), preserveLocalVariables: true)));
                diff1B.VerifyIL("C<T>.M1",
@"{
  // Code size        9 (0x9)
  .maxstack  1
  .locals init (IA V_0,
  S V_1)
  IL_0000:  nop
  IL_0001:  ldnull
  IL_0002:  call       ""void C<T>.M2(object)""
  IL_0007:  nop
  IL_0008:  ret
}");
                using (var md1 = diff1B.GetMetadata())
                {
                    var reader1 = md1.Reader;
                    var readers = new[] { reader0, reader1 };
                    CheckNames(readers, reader1.GetTypeDefNames());
                }
            }
        }

        [WorkItem(844536, "DevDiv")]
        [Fact]
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
            var compilationPIA = CreateCompilationWithMscorlib(sourcePIA, compOptions: TestOptions.UnoptimizedDll);
            var referencePIA = new MetadataImageReference(compilationPIA.EmitToArray(), embedInteropTypes: true);
            var compilation0 = CreateCompilationWithMscorlib(source, compOptions: TestOptions.UnoptimizedDll, references: new MetadataReference[] { referencePIA, SystemCoreRef, CSharpRef });
            var compilation1 = CreateCompilationWithMscorlib(source, compOptions: TestOptions.UnoptimizedDll, references: new MetadataReference[] { referencePIA, SystemCoreRef, CSharpRef });

            var bytes0 = compilation0.EmitToArray(debug: true);
            using (var md0 = ModuleMetadata.CreateFromImage(bytes0))
            {
                var generation0 = EmitBaseline.CreateInitialBaseline(md0, m => ImmutableArray.Create<string>());
                var method0 = compilation0.GetMember<MethodSymbol>("C.M");
                var method1 = compilation1.GetMember<MethodSymbol>("C.M");
                var diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1)));
                diff1.Result.Diagnostics.Verify(
                    // error CS7094: Cannot continue since the edit includes a reference to an embedded type: 'N.IA'.
                    Diagnostic(ErrorCode.ERR_EnCNoPIAReference).WithArguments("N.IA"),
                    // error CS7094: Cannot continue since the edit includes a reference to an embedded type: 'IB'.
                    Diagnostic(ErrorCode.ERR_EnCNoPIAReference).WithArguments("IB"));
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
        }

        [Fact]
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
            var compilation0 = CreateCompilationWithMscorlib(source0, compOptions: TestOptions.UnoptimizedDll);
            var compilation1 = CreateCompilationWithMscorlib(source1, compOptions: TestOptions.UnoptimizedDll);

            // Verify full metadata contains expected rows.
            var bytes0 = compilation0.EmitToArray(debug: true);
            using (var md0 = ModuleMetadata.CreateFromImage(bytes0))
            {
                var diff1 = compilation1.EmitDifference(
                    EmitBaseline.CreateInitialBaseline(md0, EmptyLocalsProvider),
                    ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Insert, null, compilation1.GetMember<MethodSymbol>("C.Main"))),
                    new CompilationTestData { SymWriterFactory = () => new MockSymUnmanagedWriter() });

                diff1.Result.Diagnostics.Verify(
                    // error CS0041: Unexpected error writing debug information -- 'The method or operation is not implemented.'
                    Diagnostic(ErrorCode.FTL_DebugEmitFailure).WithArguments("The method or operation is not implemented."));

                Assert.False(diff1.Result.Success);
            }
        }

        #region Helpers 

        private static readonly LocalVariableNameProvider EmptyLocalsProvider = token => ImmutableArray<string>.Empty;

        private static ImmutableArray<SyntaxNode> GetAllLocals(MethodSymbol method)
        {
            return GetLocalVariableDeclaratorsVisitor.GetDeclarators(method);
        }

        private static ImmutableArray<string> GetLocalNames(MethodSymbol method)
        {
            var locals = GetAllLocals(method);
            return locals.SelectAsArray(GetLocalName);
        }

        private static Func<SyntaxNode, SyntaxNode> GetLocalMap(MethodSymbol method1, MethodSymbol method0)
        {
            var tree1 = method1.Locations[0].SourceTree;
            var tree0 = method0.Locations[0].SourceTree;
            Assert.NotEqual(tree1, tree0);

            var locals0 = GetAllLocals(method0);
            return s =>
                {
                    var s1 = (SyntaxNode)s;
                    Assert.Equal(s1.SyntaxTree, tree1);
                    foreach (var s0 in locals0)
                    {
                        if (!SyntaxFactory.AreEquivalent(s0, s1))
                        {
                            continue;
                        }
                        // Make sure the containing statements are the same.
                        var p0 = GetNearestStatement(s0);
                        var p1 = GetNearestStatement(s1);
                        if (SyntaxFactory.AreEquivalent(p0, p1))
                        {
                            return s0;
                        }
                    }
                    return null;
                };
        }

        private static string GetLocalName(SyntaxNode node)
        {
            switch (node.CSharpKind())
            {
                case SyntaxKind.VariableDeclarator:
                    return ((VariableDeclaratorSyntax)node).Identifier.ToString();
                default:
                    throw new NotImplementedException();
            }
        }

        private static ImmutableArray<string> GetLocalNames(CompilationTestData.MethodData methodData)
        {
            var locals = methodData.ILBuilder.LocalSlotManager.LocalsInOrder();
            return locals.SelectAsArray(l => l.Name);
        }

        private static StatementSyntax GetNearestStatement(SyntaxNode node)
        {
            while (node != null)
            {
                var statement = node as StatementSyntax;
                if (statement != null)
                {
                    return statement;
                }
                node = node.Parent;
            }
            return null;
        }

        private static EditAndContinueLogEntry Row(int rowNumber, TableIndex table, EditAndContinueOperation operation)
        {
            return new EditAndContinueLogEntry(MetadataTokens.Handle(table, rowNumber), operation);
        }

        private static Handle Handle(int rowNumber, TableIndex table)
        {
            return MetadataTokens.Handle(table, rowNumber);
        }

        private static void CheckEncLog(MetadataReader reader, params EditAndContinueLogEntry[] rows)
        {
            AssertEx.Equal(rows, reader.GetEditAndContinueLogEntries(), itemInspector: EncLogRowToString);
        }

        private static void CheckEncMap(MetadataReader reader, params Handle[] handles)
        {
            AssertEx.Equal(handles, reader.GetEditAndContinueMapEntries(), itemInspector: EncMapRowToString);
        }

        private static void CheckAttributes(MetadataReader reader, params CustomAttributeRow[] rows)
        {
            AssertEx.Equal(rows, reader.GetCustomAttributeRows(), itemInspector: AttributeRowToString);
        }

        internal static void CheckNames(MetadataReader reader, StringHandle[] handles, params string[] expectedNames)
        {
            CheckNames(new[] { reader }, handles, expectedNames);
        }

        internal static void CheckNames(MetadataReader[] readers, StringHandle[] handles, params string[] expectedNames)
        {
            var actualNames = readers.GetStrings(handles);
            AssertEx.Equal(actualNames, expectedNames);
        }

        private static string EncLogRowToString(EditAndContinueLogEntry row)
        {
            return string.Format(
                "Row({0}, TableIndices.{1}, EditAndContinueOperation.{2})",
                MetadataTokens.GetRowNumber(row.Handle),
                MetadataTokens.GetTableIndex(row.Handle),
                row.Operation);
        }

        private static string EncMapRowToString(Handle handle)
        {
            return string.Format(
                "Handle({0}, TableIndices.{1})",
                MetadataTokens.GetRowNumber(handle),
                MetadataTokens.GetTableIndex(handle));
        }

        private static string AttributeRowToString(CustomAttributeRow row)
        {
            return string.Format(
                "new CustomAttributeRow(Handle({0}, TableIndices.{1}), Handle({2}, TableIndices.{3}))",
                MetadataTokens.GetRowNumber(row.ParentToken),
                MetadataTokens.GetTableIndex(row.ParentToken),
                MetadataTokens.GetRowNumber(row.ConstructorToken),
                MetadataTokens.GetTableIndex(row.ConstructorToken));
        }

        #endregion
    }
}
