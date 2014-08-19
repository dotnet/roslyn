// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.MetadataUtilities;
using Roslyn.Test.PdbUtilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue.UnitTests
{
    public class EditAndContinueStateMachineTests : EditAndContinueTestBase
    {
        [Fact]
        public void AddIteratorMethod()
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
            var compilation0 = CreateCompilationWithMscorlib(Parse(source0, "a.cs"), options: TestOptions.DebugDll);
            var compilation1 = CreateCompilationWithMscorlib(Parse(source1, "a.cs"), options: TestOptions.DebugDll);

            var bytes0 = compilation0.EmitToArray();
            var generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), EmptyLocalsProvider);
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Insert, null, compilation1.GetMember<MethodSymbol>("C.G"))));

            using (var md1 = diff1.GetMetadata())
            {
                var reader1 = md1.Reader;

                CheckEncLog(reader1,
                    Row(2, TableIndex.AssemblyRef, EditAndContinueOperation.Default),
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
                    Row(29, TableIndex.MemberRef, EditAndContinueOperation.Default),
                    Row(16, TableIndex.TypeRef, EditAndContinueOperation.Default),
                    Row(17, TableIndex.TypeRef, EditAndContinueOperation.Default),
                    Row(18, TableIndex.TypeRef, EditAndContinueOperation.Default),
                    Row(19, TableIndex.TypeRef, EditAndContinueOperation.Default),
                    Row(20, TableIndex.TypeRef, EditAndContinueOperation.Default),
                    Row(21, TableIndex.TypeRef, EditAndContinueOperation.Default),
                    Row(22, TableIndex.TypeRef, EditAndContinueOperation.Default),
                    Row(23, TableIndex.TypeRef, EditAndContinueOperation.Default),
                    Row(24, TableIndex.TypeRef, EditAndContinueOperation.Default),
                    Row(25, TableIndex.TypeRef, EditAndContinueOperation.Default),
                    Row(26, TableIndex.TypeRef, EditAndContinueOperation.Default),
                    Row(3, TableIndex.TypeSpec, EditAndContinueOperation.Default),
                    Row(4, TableIndex.TypeSpec, EditAndContinueOperation.Default),
                    Row(4, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                    Row(5, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                    Row(6, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
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
                    Row(12, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(13, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(14, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(15, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(16, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(17, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(18, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(19, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
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
                    Handle(16, TableIndex.TypeRef),
                    Handle(17, TableIndex.TypeRef),
                    Handle(18, TableIndex.TypeRef),
                    Handle(19, TableIndex.TypeRef),
                    Handle(20, TableIndex.TypeRef),
                    Handle(21, TableIndex.TypeRef),
                    Handle(22, TableIndex.TypeRef),
                    Handle(23, TableIndex.TypeRef),
                    Handle(24, TableIndex.TypeRef),
                    Handle(25, TableIndex.TypeRef),
                    Handle(26, TableIndex.TypeRef),
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
                    Handle(29, TableIndex.MemberRef),
                    Handle(12, TableIndex.CustomAttribute),
                    Handle(13, TableIndex.CustomAttribute),
                    Handle(14, TableIndex.CustomAttribute),
                    Handle(15, TableIndex.CustomAttribute),
                    Handle(16, TableIndex.CustomAttribute),
                    Handle(17, TableIndex.CustomAttribute),
                    Handle(18, TableIndex.CustomAttribute),
                    Handle(19, TableIndex.CustomAttribute),
                    Handle(4, TableIndex.StandAloneSig),
                    Handle(5, TableIndex.StandAloneSig),
                    Handle(6, TableIndex.StandAloneSig),
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

            string actualPdb1 = PdbToXmlConverter.DeltaPdbToXml(diff1.PdbDelta, Enumerable.Range(1, 100).Select(rid => 0x06000000U | (uint)rid));

            // TODO (tomat): bug in SymWriter.
            // The PDB is missing debug info for G method. The info is written to the PDB but the native SymWriter 
            // seems to ignore it. If another method is added to the class all information is written. 
            // This happens regardless of whether we emit just the delta or full PDB.

            string expectedPdb1 = @"
<symbols>
  <files>
    <file id=""1"" name=""a.cs"" language=""3f5162f8-07c6-11d3-9053-00c04fa302a1"" languageVendor=""994b45c4-e6e9-11d2-903f-00c04fa302a1"" documentType=""5a869d0b-6611-11d3-bd2a-0000f80849bd"" checkSumAlgorithmId=""ff1816ec-aa5e-4d10-87f7-6f4963833460"" checkSum=""6E, 19, 36, 2B, 9A, 28, AB, E3, A2, DA, EB, 51, C1, 37,  1, 10, B0, 4F, CA, 84, "" />
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
        <local name=""CS$524$0000"" il_index=""0"" il_start=""0x0"" il_end=""0x3f"" attributes=""1"" />
      </locals>
      <scope startOffset=""0x0"" endOffset=""0x3f"">
        <namespace name=""System.Collections.Generic"" />
        <local name=""CS$524$0000"" il_index=""0"" il_start=""0x0"" il_end=""0x3f"" attributes=""1"" />
      </scope>
    </method>
  </methods>
</symbols>";

            AssertXmlEqual(expectedPdb1, actualPdb1);
        }

        [Fact]
        public void AddAsyncMethod()
        {
            var source0 = @"
using System.Threading.Tasks;

class C
{
}";
            var source1 = @"
using System.Threading.Tasks;

class C
{
    static async Task<int> F() 
    {
        await Task.FromResult(10);
        return 20;
    }
}";
            var compilation0 = CreateCompilationWithMscorlib45(source0, options: TestOptions.DebugDll);
            var compilation1 = CreateCompilationWithMscorlib45(source1, options: TestOptions.DebugDll);
            var v0 = CompileAndVerify(compilation0);

            var generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData), EmptyLocalsProvider);
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Insert, null, compilation1.GetMember<MethodSymbol>("C.F"))));

            using (var md1 = diff1.GetMetadata())
            {
                var reader1 = md1.Reader;

                // Add state machine type and its members:
                // - Method '.ctor'
                // - Method 'MoveNext'
                // - Method 'SetStateMachine'
                // - Field '<>1__state'
                // - Field '<>t__builder'
                // - Field '<>u__$awaiter0'
                // Add method F()
                CheckEncLogDefinitions(reader1,
                    Row(1, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                    Row(2, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                    Row(3, TableIndex.TypeDef, EditAndContinueOperation.Default),
                    Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                    Row(1, TableIndex.Field, EditAndContinueOperation.Default),
                    Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                    Row(2, TableIndex.Field, EditAndContinueOperation.Default),
                    Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                    Row(3, TableIndex.Field, EditAndContinueOperation.Default),
                    Row(2, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                    Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                    Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                    Row(3, TableIndex.MethodDef, EditAndContinueOperation.Default),
                    Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                    Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                    Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                    Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
                    Row(5, TableIndex.MethodDef, EditAndContinueOperation.AddParameter),
                    Row(1, TableIndex.Param, EditAndContinueOperation.Default),
                    Row(4, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(5, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(6, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(7, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(1, TableIndex.MethodImpl, EditAndContinueOperation.Default),
                    Row(2, TableIndex.MethodImpl, EditAndContinueOperation.Default),
                    Row(1, TableIndex.NestedClass, EditAndContinueOperation.Default),
                    Row(1, TableIndex.InterfaceImpl, EditAndContinueOperation.Default));
            }
        }

        [Fact]
        public void MethodToIteratorMethod()
        {
            var source0 = @"
using System.Collections.Generic;

class C
{
    static IEnumerable<int> F() 
    {
        return new int[] { 1, 2, 3 };
    }
}";
            var source1 = @"
using System.Collections.Generic;

class C
{
    static IEnumerable<int> F() 
    {
        yield return 2;
    }
}";
            var compilation0 = CreateCompilationWithMscorlib45(source0, options: TestOptions.DebugDll);
            var compilation1 = CreateCompilationWithMscorlib45(source1, options: TestOptions.DebugDll);

            var v0 = CompileAndVerify(compilation0);

            using (var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData))
            {
                var method0 = compilation0.GetMember<MethodSymbol>("C.F");
                var method1 = compilation1.GetMember<MethodSymbol>("C.F");

                var generation0 = EmitBaseline.CreateInitialBaseline(md0, EmptyLocalsProvider);
                var diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1)));

                using (var md1 = diff1.GetMetadata())
                {
                    CheckEncLogDefinitions(md1.Reader,
                        Row(2, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(3, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(4, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(5, TableIndex.TypeDef, EditAndContinueOperation.Default),
                        Row(1, TableIndex.PropertyMap, EditAndContinueOperation.Default),
                        Row(5, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                        Row(2, TableIndex.Field, EditAndContinueOperation.Default),
                        Row(5, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                        Row(3, TableIndex.Field, EditAndContinueOperation.Default),
                        Row(5, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                        Row(4, TableIndex.Field, EditAndContinueOperation.Default),
                        Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(5, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                        Row(3, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(5, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                        Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(5, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                        Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(5, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                        Row(6, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(5, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                        Row(7, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(5, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                        Row(8, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(5, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                        Row(9, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(5, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                        Row(10, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(1, TableIndex.PropertyMap, EditAndContinueOperation.AddProperty),
                        Row(1, TableIndex.Property, EditAndContinueOperation.Default),
                        Row(1, TableIndex.PropertyMap, EditAndContinueOperation.AddProperty),
                        Row(2, TableIndex.Property, EditAndContinueOperation.Default),
                        Row(3, TableIndex.MethodDef, EditAndContinueOperation.AddParameter),
                        Row(1, TableIndex.Param, EditAndContinueOperation.Default),
                        Row(5, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(6, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(7, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(8, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(9, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(10, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(11, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(12, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(1, TableIndex.MethodSemantics, EditAndContinueOperation.Default),
                        Row(2, TableIndex.MethodSemantics, EditAndContinueOperation.Default),
                        Row(1, TableIndex.MethodImpl, EditAndContinueOperation.Default),
                        Row(2, TableIndex.MethodImpl, EditAndContinueOperation.Default),
                        Row(3, TableIndex.MethodImpl, EditAndContinueOperation.Default),
                        Row(4, TableIndex.MethodImpl, EditAndContinueOperation.Default),
                        Row(5, TableIndex.MethodImpl, EditAndContinueOperation.Default),
                        Row(6, TableIndex.MethodImpl, EditAndContinueOperation.Default),
                        Row(7, TableIndex.MethodImpl, EditAndContinueOperation.Default),
                        Row(2, TableIndex.NestedClass, EditAndContinueOperation.Default),
                        Row(1, TableIndex.InterfaceImpl, EditAndContinueOperation.Default),
                        Row(2, TableIndex.InterfaceImpl, EditAndContinueOperation.Default),
                        Row(3, TableIndex.InterfaceImpl, EditAndContinueOperation.Default),
                        Row(4, TableIndex.InterfaceImpl, EditAndContinueOperation.Default),
                        Row(5, TableIndex.InterfaceImpl, EditAndContinueOperation.Default));
                }
            }
        }

        [Fact]
        public void MethodToAsyncMethod()
        {
            var source0 = @"
using System.Threading.Tasks;

class C
{
    static Task<int> F() 
    {
        return Task.FromResult(1);
    }
}";
            var source1 = @"
using System.Threading.Tasks;

class C
{
    static async Task<int> F() 
    {
        return await Task.FromResult(1);
    }
}";
            var compilation0 = CreateCompilationWithMscorlib45(source0, options: TestOptions.DebugDll);
            var compilation1 = CreateCompilationWithMscorlib45(source1, options: TestOptions.DebugDll);

            var v0 = CompileAndVerify(compilation0);

            using (var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData))
            {
                var method0 = compilation0.GetMember<MethodSymbol>("C.F");
                var method1 = compilation1.GetMember<MethodSymbol>("C.F");

                var generation0 = EmitBaseline.CreateInitialBaseline(md0, EmptyLocalsProvider);
                var diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1)));

                using (var md1 = diff1.GetMetadata())
                {
                    CheckEncLogDefinitions(md1.Reader,
                        Row(2, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(3, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(3, TableIndex.TypeDef, EditAndContinueOperation.Default),
                        Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                        Row(1, TableIndex.Field, EditAndContinueOperation.Default),
                        Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                        Row(2, TableIndex.Field, EditAndContinueOperation.Default),
                        Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                        Row(3, TableIndex.Field, EditAndContinueOperation.Default),
                        Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                        Row(3, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                        Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                        Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(5, TableIndex.MethodDef, EditAndContinueOperation.AddParameter),
                        Row(1, TableIndex.Param, EditAndContinueOperation.Default),
                        Row(4, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(5, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(6, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(7, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(1, TableIndex.MethodImpl, EditAndContinueOperation.Default),
                        Row(2, TableIndex.MethodImpl, EditAndContinueOperation.Default),
                        Row(1, TableIndex.NestedClass, EditAndContinueOperation.Default),
                        Row(1, TableIndex.InterfaceImpl, EditAndContinueOperation.Default));
                }
            }
        }

        [Fact]
        public void IteratorMethodToMethod()
        {
            var source0 = @"
using System.Collections.Generic;

class C
{
    static IEnumerable<int> F() 
    {
        yield return 2;
    }
}";
            var source1 = @"
using System.Collections.Generic;

class C
{
    static IEnumerable<int> F() 
    {
        return new int[] { 1, 2, 3 };
    }
}";
            var compilation0 = CreateCompilationWithMscorlib45(source0, options: TestOptions.DebugDll);
            var compilation1 = CreateCompilationWithMscorlib45(source1, options: TestOptions.DebugDll);

            var v0 = CompileAndVerify(compilation0);

            using (var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData))
            {
                var method0 = compilation0.GetMember<MethodSymbol>("C.F");
                var method1 = compilation1.GetMember<MethodSymbol>("C.F");

                var generation0 = EmitBaseline.CreateInitialBaseline(md0, EmptyLocalsProvider);
                var diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1)));

                using (var md1 = diff1.GetMetadata())
                {
                    CheckEncLogDefinitions(md1.Reader,
                        Row(4, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default));
                }
            }
        }

        [Fact]
        public void AsyncMethodToMethod()
        {
            var source0 = @"
using System.Threading.Tasks;

class C
{
    static async Task<int> F() 
    {
        return await Task.FromResult(1);
    }
}";
            var source1 = @"
using System.Threading.Tasks;

class C
{
    static Task<int> F() 
    {
        return Task.FromResult(1);
    }
}";
            var compilation0 = CreateCompilationWithMscorlib45(source0, options: TestOptions.DebugDll);
            var compilation1 = CreateCompilationWithMscorlib45(source1, options: TestOptions.DebugDll);

            var v0 = CompileAndVerify(compilation0);

            using (var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData))
            {
                var method0 = compilation0.GetMember<MethodSymbol>("C.F");
                var method1 = compilation1.GetMember<MethodSymbol>("C.F");

                var generation0 = EmitBaseline.CreateInitialBaseline(md0, EmptyLocalsProvider);
                var diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1)));

                using (var md1 = diff1.GetMetadata())
                {
                    CheckEncLogDefinitions(md1.Reader,
                        Row(3, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default));
                }
            }
        }

        [Fact]
        public void UpdateIterator_NoVariables()
        {
            var source0 = @"
using System.Collections.Generic;

class C
{
    static IEnumerable<int> F() 
    {
        yield return 1;
    }
}";
            var source1 = @"
using System.Collections.Generic;

class C
{
    static IEnumerable<int> F() 
    {
        yield return 2;
    }
}";
            var compilation0 = CreateCompilationWithMscorlib45(source0, options: TestOptions.DebugDll);
            var compilation1 = CreateCompilationWithMscorlib45(source1, options: TestOptions.DebugDll);

            var v0 = CompileAndVerify(compilation0);

            using (var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData))
            {
                var method0 = compilation0.GetMember<MethodSymbol>("C.F");
                var method1 = compilation1.GetMember<MethodSymbol>("C.F");

                var generation0 = EmitBaseline.CreateInitialBaseline(md0, EmptyLocalsProvider);
                var diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1)));

                // Verify delta metadata contains expected rows.
                using (var md1 = diff1.GetMetadata())
                {
                    // Verify that no new TypeDefs, FieldDefs or MethodDefs were added,
                    // 3 methods were updated: 
                    // - the kick-off method (might be changed if the method previously wasn't an iterator)
                    // - Finally method
                    // - MoveNext method
                    CheckEncLogDefinitions(md1.Reader,
                        Row(4, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(5, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(12, TableIndex.CustomAttribute, EditAndContinueOperation.Default));

                    diff1.VerifyIL("C.<F>d__0.System.Collections.IEnumerator.MoveNext", @"
{
  // Code size       63 (0x3f)
  .maxstack  2
  .locals init (int V_0, //CS$524$0000
                bool V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<F>d__0.<>1__state""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  brfalse.s  IL_0012
  IL_000a:  br.s       IL_000c
  IL_000c:  ldloc.0
  IL_000d:  ldc.i4.1
  IL_000e:  beq.s      IL_0014
  IL_0010:  br.s       IL_0016
  IL_0012:  br.s       IL_001a
  IL_0014:  br.s       IL_0034
  IL_0016:  ldc.i4.0
  IL_0017:  stloc.1
  IL_0018:  ldloc.1
  IL_0019:  ret
  IL_001a:  ldarg.0
  IL_001b:  ldc.i4.m1
  IL_001c:  stfld      ""int C.<F>d__0.<>1__state""
  IL_0021:  nop
  IL_0022:  ldarg.0
  IL_0023:  ldc.i4.2
  IL_0024:  stfld      ""int C.<F>d__0.<>2__current""
  IL_0029:  ldarg.0
  IL_002a:  ldc.i4.1
  IL_002b:  stfld      ""int C.<F>d__0.<>1__state""
  IL_0030:  ldc.i4.1
  IL_0031:  stloc.1
  IL_0032:  br.s       IL_0018
  IL_0034:  ldarg.0
  IL_0035:  ldc.i4.m1
  IL_0036:  stfld      ""int C.<F>d__0.<>1__state""
  IL_003b:  ldc.i4.0
  IL_003c:  stloc.1
  IL_003d:  br.s       IL_0018
}
");
                    v0.VerifyIL("C.<F>d__0.System.Collections.IEnumerator.MoveNext", @"
{
  // Code size       63 (0x3f)
  .maxstack  2
  .locals init (int V_0, //CS$524$0000
                bool V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<F>d__0.<>1__state""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  brfalse.s  IL_0012
  IL_000a:  br.s       IL_000c
  IL_000c:  ldloc.0
  IL_000d:  ldc.i4.1
  IL_000e:  beq.s      IL_0014
  IL_0010:  br.s       IL_0016
  IL_0012:  br.s       IL_001a
  IL_0014:  br.s       IL_0034
  IL_0016:  ldc.i4.0
  IL_0017:  stloc.1
  IL_0018:  ldloc.1
  IL_0019:  ret
  IL_001a:  ldarg.0
  IL_001b:  ldc.i4.m1
  IL_001c:  stfld      ""int C.<F>d__0.<>1__state""
  IL_0021:  nop
  IL_0022:  ldarg.0
  IL_0023:  ldc.i4.1
  IL_0024:  stfld      ""int C.<F>d__0.<>2__current""
  IL_0029:  ldarg.0
  IL_002a:  ldc.i4.1
  IL_002b:  stfld      ""int C.<F>d__0.<>1__state""
  IL_0030:  ldc.i4.1
  IL_0031:  stloc.1
  IL_0032:  br.s       IL_0018
  IL_0034:  ldarg.0
  IL_0035:  ldc.i4.m1
  IL_0036:  stfld      ""int C.<F>d__0.<>1__state""
  IL_003b:  ldc.i4.0
  IL_003c:  stloc.1
  IL_003d:  br.s       IL_0018
}");
                }
            }
        }

        [Fact]
        public void UpdateAsync_NoVariables()
        {
            var source0 = @"
using System.Threading.Tasks;

class C
{
    static async Task<int> F() 
    {
        await Task.FromResult(1);
        return 2;
    }
}";
            var source1 = @"
using System.Threading.Tasks;

class C
{
    static async Task<int> F() 
    {
        await Task.FromResult(10);
        return 20;
    }
}";
            var compilation0 = CreateCompilationWithMscorlib45(source0, options: TestOptions.DebugDll);
            var compilation1 = CreateCompilationWithMscorlib45(source1, options: TestOptions.DebugDll);

            var v0 = CompileAndVerify(compilation0);

            using (var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData))
            {
                var method0 = compilation0.GetMember<MethodSymbol>("C.F");
                var method1 = compilation1.GetMember<MethodSymbol>("C.F");

                var generation0 = EmitBaseline.CreateInitialBaseline(md0, EmptyLocalsProvider);
                var diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1)));

                using (var md1 = diff1.GetMetadata())
                {
                    // Verify that no new TypeDefs, FieldDefs or MethodDefs were added,
                    // 2 methods were updated: 
                    // - the kick-off method (might be changed if the method previously wasn't async)
                    // - MoveNext method
                    CheckEncLogDefinitions(md1.Reader,
                        Row(3, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(4, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(8, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(9, TableIndex.CustomAttribute, EditAndContinueOperation.Default));

                    diff1.VerifyIL("C.<F>d__1.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      179 (0xb3)
  .maxstack  3
  .locals init (int V_0, //CS$524$0000
                int V_1, //CS$523$0001
                System.Runtime.CompilerServices.TaskAwaiter<int> V_2,
                C.<F>d__1 V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<F>d__1.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0012
    IL_000a:  br.s       IL_000c
    IL_000c:  ldloc.0
    IL_000d:  ldc.i4.1
    IL_000e:  beq.s      IL_0014
    IL_0010:  br.s       IL_0016
    IL_0012:  br.s       IL_0016
    IL_0014:  br.s       IL_0051
    IL_0016:  nop
    IL_0017:  ldc.i4.s   10
    IL_0019:  call       ""System.Threading.Tasks.Task<int> System.Threading.Tasks.Task.FromResult<int>(int)""
    IL_001e:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0023:  stloc.2
    IL_0024:  ldloca.s   V_2
    IL_0026:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_002b:  brtrue.s   IL_006d
    IL_002d:  ldarg.0
    IL_002e:  ldc.i4.1
    IL_002f:  dup
    IL_0030:  stloc.0
    IL_0031:  stfld      ""int C.<F>d__1.<>1__state""
    IL_0036:  ldarg.0
    IL_0037:  ldloc.2
    IL_0038:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> C.<F>d__1.<>u__$awaiter0""
    IL_003d:  ldarg.0
    IL_003e:  stloc.3
    IL_003f:  ldarg.0
    IL_0040:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__1.<>t__builder""
    IL_0045:  ldloca.s   V_2
    IL_0047:  ldloca.s   V_3
    IL_0049:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, C.<F>d__1>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref C.<F>d__1)""
    IL_004e:  nop
    IL_004f:  leave.s    IL_00b2
    IL_0051:  ldarg.0
    IL_0052:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> C.<F>d__1.<>u__$awaiter0""
    IL_0057:  stloc.2
    IL_0058:  ldarg.0
    IL_0059:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> C.<F>d__1.<>u__$awaiter0""
    IL_005e:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0064:  ldarg.0
    IL_0065:  ldc.i4.m1
    IL_0066:  dup
    IL_0067:  stloc.0
    IL_0068:  stfld      ""int C.<F>d__1.<>1__state""
    IL_006d:  ldloca.s   V_2
    IL_006f:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0074:  pop
    IL_0075:  ldloca.s   V_2
    IL_0077:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_007d:  ldc.i4.s   20
    IL_007f:  stloc.1
    IL_0080:  leave.s    IL_009d
  }
  catch System.Exception
  {
    IL_0082:  stloc.s    V_4
    IL_0084:  nop
    IL_0085:  ldarg.0
    IL_0086:  ldc.i4.s   -2
    IL_0088:  stfld      ""int C.<F>d__1.<>1__state""
    IL_008d:  ldarg.0
    IL_008e:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__1.<>t__builder""
    IL_0093:  ldloc.s    V_4
    IL_0095:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)""
    IL_009a:  nop
    IL_009b:  leave.s    IL_00b2
  }
  IL_009d:  ldarg.0
  IL_009e:  ldc.i4.s   -2
  IL_00a0:  stfld      ""int C.<F>d__1.<>1__state""
  IL_00a5:  ldarg.0
  IL_00a6:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__1.<>t__builder""
  IL_00ab:  ldloc.1
  IL_00ac:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetResult(int)""
  IL_00b1:  nop
  IL_00b2:  ret
}
");
                    v0.VerifyIL("C.<F>d__1.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      177 (0xb1)
  .maxstack  3
  .locals init (int V_0, //CS$524$0000
                int V_1, //CS$523$0001
                System.Runtime.CompilerServices.TaskAwaiter<int> V_2,
                C.<F>d__1 V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<F>d__1.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0012
    IL_000a:  br.s       IL_000c
    IL_000c:  ldloc.0
    IL_000d:  ldc.i4.1
    IL_000e:  beq.s      IL_0014
    IL_0010:  br.s       IL_0016
    IL_0012:  br.s       IL_0016
    IL_0014:  br.s       IL_0050
    IL_0016:  nop
    IL_0017:  ldc.i4.1
    IL_0018:  call       ""System.Threading.Tasks.Task<int> System.Threading.Tasks.Task.FromResult<int>(int)""
    IL_001d:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0022:  stloc.2
    IL_0023:  ldloca.s   V_2
    IL_0025:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_002a:  brtrue.s   IL_006c
    IL_002c:  ldarg.0
    IL_002d:  ldc.i4.1
    IL_002e:  dup
    IL_002f:  stloc.0
    IL_0030:  stfld      ""int C.<F>d__1.<>1__state""
    IL_0035:  ldarg.0
    IL_0036:  ldloc.2
    IL_0037:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> C.<F>d__1.<>u__$awaiter0""
    IL_003c:  ldarg.0
    IL_003d:  stloc.3
    IL_003e:  ldarg.0
    IL_003f:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__1.<>t__builder""
    IL_0044:  ldloca.s   V_2
    IL_0046:  ldloca.s   V_3
    IL_0048:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, C.<F>d__1>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref C.<F>d__1)""
    IL_004d:  nop
    IL_004e:  leave.s    IL_00b0
    IL_0050:  ldarg.0
    IL_0051:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> C.<F>d__1.<>u__$awaiter0""
    IL_0056:  stloc.2
    IL_0057:  ldarg.0
    IL_0058:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> C.<F>d__1.<>u__$awaiter0""
    IL_005d:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0063:  ldarg.0
    IL_0064:  ldc.i4.m1
    IL_0065:  dup
    IL_0066:  stloc.0
    IL_0067:  stfld      ""int C.<F>d__1.<>1__state""
    IL_006c:  ldloca.s   V_2
    IL_006e:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0073:  pop
    IL_0074:  ldloca.s   V_2
    IL_0076:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_007c:  ldc.i4.2
    IL_007d:  stloc.1
    IL_007e:  leave.s    IL_009b
  }
  catch System.Exception
  {
    IL_0080:  stloc.s    V_4
    IL_0082:  nop
    IL_0083:  ldarg.0
    IL_0084:  ldc.i4.s   -2
    IL_0086:  stfld      ""int C.<F>d__1.<>1__state""
    IL_008b:  ldarg.0
    IL_008c:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__1.<>t__builder""
    IL_0091:  ldloc.s    V_4
    IL_0093:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)""
    IL_0098:  nop
    IL_0099:  leave.s    IL_00b0
  }
  IL_009b:  ldarg.0
  IL_009c:  ldc.i4.s   -2
  IL_009e:  stfld      ""int C.<F>d__1.<>1__state""
  IL_00a3:  ldarg.0
  IL_00a4:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__1.<>t__builder""
  IL_00a9:  ldloc.1
  IL_00aa:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetResult(int)""
  IL_00af:  nop
  IL_00b0:  ret
}
");
                }
            }
        }
    }
}
