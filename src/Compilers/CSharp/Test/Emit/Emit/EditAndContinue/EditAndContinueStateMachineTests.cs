// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue.UnitTests
{
    public class EditAndContinueStateMachineTests : EditAndContinueTestBase
    {
        [Fact(Skip = "1068894"), WorkItem(1137300, "DevDiv")]
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

            diff1.VerifyPdb(Enumerable.Range(0x06000001, 0x20), @"
<symbols>
  <files>
    <file id=""1"" name=""a.cs"" language=""3f5162f8-07c6-11d3-9053-00c04fa302a1"" languageVendor=""994b45c4-e6e9-11d2-903f-00c04fa302a1"" documentType=""5a869d0b-6611-11d3-bd2a-0000f80849bd"" checkSumAlgorithmId=""ff1816ec-aa5e-4d10-87f7-6f4963833460"" checkSum=""6E, 19, 36, 2B, 9A, 28, AB, E3, A2, DA, EB, 51, C1, 37,  1, 10, B0, 4F, CA, 84, "" />
  </files>
  <methods>
    <method token=""0x600000c"">
      <customDebugInfo>
        <forwardIterator name=""&lt;G&gt;d__1#1"" />
      </customDebugInfo>
      <sequencePoints />
    </method>
    <method token=""0x600000f"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""1"" />
        <entry offset=""0x21"" startLine=""9"" startColumn=""5"" endLine=""9"" endColumn=""6"" document=""1"" />
        <entry offset=""0x22"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""24"" document=""1"" />
        <entry offset=""0x34"" hidden=""true"" document=""1"" />
        <entry offset=""0x3b"" startLine=""11"" startColumn=""5"" endLine=""11"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x3f"">
        <namespace name=""System.Collections.Generic"" />
      </scope>
    </method>
  </methods>
</symbols>");
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
            var compilation1 = compilation0.WithSource(source1);
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
                // - Field '<>u__1'
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
            var compilation1 = compilation0.WithSource(source1);

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
                        Row(13, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
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
            var compilation1 = compilation0.WithSource(source1);

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
                        Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                        Row(4, TableIndex.Field, EditAndContinueOperation.Default),
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
            var compilation1 = compilation0.WithSource(source1);

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
            var compilation1 = compilation0.WithSource(source1);

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
        public void AsyncMethodOverloads()
        {
            var source0 = @"
using System.Threading.Tasks;

class C
{
    static async Task<int> F(long a) 
    {
        return await Task.FromResult(1);
    }

    static async Task<int> F(int a) 
    {
        return await Task.FromResult(1);
    }

    static async Task<int> F(short a) 
    {
        return await Task.FromResult(1);
    }

    public void X() { } // needs to be present to work around SymWriter bug #1068894
}";
            var source1 = @"
using System.Threading.Tasks;

class C
{
    static async Task<int> F(short a) 
    {
        return await Task.FromResult(2);
    }

    static async Task<int> F(long a) 
    {
        return await Task.FromResult(3);
    }

    static async Task<int> F(int a) 
    {
        return await Task.FromResult(4);
    }

    public void X() { } // needs to be present to work around SymWriter bug #1068894
}";
            var compilation0 = CreateCompilationWithMscorlib45(source0, options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);

            var v0 = CompileAndVerify(compilation0);

            using (var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData))
            {
                var methodShort0 = compilation0.GetMembers("C.F").Single(m => m.ToTestDisplayString() == "System.Threading.Tasks.Task<System.Int32> C.F(System.Int16 a)");
                var methodShort1 = compilation1.GetMembers("C.F").Single(m => m.ToTestDisplayString() == "System.Threading.Tasks.Task<System.Int32> C.F(System.Int16 a)");

                var methodInt0 = compilation0.GetMembers("C.F").Single(m => m.ToTestDisplayString() == "System.Threading.Tasks.Task<System.Int32> C.F(System.Int32 a)");
                var methodInt1 = compilation1.GetMembers("C.F").Single(m => m.ToTestDisplayString() == "System.Threading.Tasks.Task<System.Int32> C.F(System.Int32 a)");

                var methodLong0 = compilation0.GetMembers("C.F").Single(m => m.ToTestDisplayString() == "System.Threading.Tasks.Task<System.Int32> C.F(System.Int64 a)");
                var methodLong1 = compilation1.GetMembers("C.F").Single(m => m.ToTestDisplayString() == "System.Threading.Tasks.Task<System.Int32> C.F(System.Int64 a)");

                var generation0 = EmitBaseline.CreateInitialBaseline(md0, EmptyLocalsProvider);
                var diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(
                        new SemanticEdit(SemanticEditKind.Update, methodShort0, methodShort1, preserveLocalVariables: true),
                        new SemanticEdit(SemanticEditKind.Update, methodInt0, methodInt1, preserveLocalVariables: true),
                        new SemanticEdit(SemanticEditKind.Update, methodLong0, methodLong1, preserveLocalVariables: true)
                    ));

                using (var md1 = diff1.GetMetadata())
                {
                    // notice no TypeDefs, FieldDefs
                    CheckEncLogDefinitions(md1.Reader,
                        Row(7, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(8, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(9, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(10, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(11, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(12, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(3, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(7, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(10, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(13, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(13, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(14, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(15, TableIndex.CustomAttribute, EditAndContinueOperation.Default));
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
            var compilation1 = compilation0.WithSource(source1);

            var v0 = CompileAndVerify(compilation0);

            using (var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData))
            {
                var method0 = compilation0.GetMember<MethodSymbol>("C.F");
                var method1 = compilation1.GetMember<MethodSymbol>("C.F");

                var generation0 = EmitBaseline.CreateInitialBaseline(md0, EmptyLocalsProvider);
                var diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, preserveLocalVariables: true)));

                // only methods with sequence points should be listed in UpdatedMethods:
                AssertEx.Equal(new[] { 0x06000005 }, diff1.UpdatedMethods.Select(m => MetadataTokens.GetToken(m)));

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
                        Row(13, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(14, TableIndex.CustomAttribute, EditAndContinueOperation.Default));

                    diff1.VerifyIL("C.<F>d__0.System.Collections.IEnumerator.MoveNext", @"
{
  // Code size       63 (0x3f)
  .maxstack  2
  .locals init (int V_0,
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
  .locals init (int V_0,
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
            var compilation1 = compilation0.WithSource(source1);

            var v0 = CompileAndVerify(compilation0);

            using (var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData))
            {
                var method0 = compilation0.GetMember<MethodSymbol>("C.F");
                var method1 = compilation1.GetMember<MethodSymbol>("C.F");

                var generation0 = EmitBaseline.CreateInitialBaseline(md0, EmptyLocalsProvider);
                var diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, preserveLocalVariables: true)));

                // only methods with sequence points should be listed in UpdatedMethods:
                AssertEx.Equal(new[] { 0x06000004 }, diff1.UpdatedMethods.Select(m => MetadataTokens.GetToken(m)));

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
                        Row(7, TableIndex.CustomAttribute, EditAndContinueOperation.Default));

                    diff1.VerifyIL("C.<F>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      170 (0xaa)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_2,
                C.<F>d__0 V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<F>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_000c
    IL_000a:  br.s       IL_000e
    IL_000c:  br.s       IL_0049
    IL_000e:  nop
    IL_000f:  ldc.i4.s   10
    IL_0011:  call       ""System.Threading.Tasks.Task<int> System.Threading.Tasks.Task.FromResult<int>(int)""
    IL_0016:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_001b:  stloc.2
    IL_001c:  ldloca.s   V_2
    IL_001e:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_0023:  brtrue.s   IL_0065
    IL_0025:  ldarg.0
    IL_0026:  ldc.i4.0
    IL_0027:  dup
    IL_0028:  stloc.0
    IL_0029:  stfld      ""int C.<F>d__0.<>1__state""
    IL_002e:  ldarg.0
    IL_002f:  ldloc.2
    IL_0030:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> C.<F>d__0.<>u__1""
    IL_0035:  ldarg.0
    IL_0036:  stloc.3
    IL_0037:  ldarg.0
    IL_0038:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__0.<>t__builder""
    IL_003d:  ldloca.s   V_2
    IL_003f:  ldloca.s   V_3
    IL_0041:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, C.<F>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref C.<F>d__0)""
    IL_0046:  nop
    IL_0047:  leave.s    IL_00a9
    IL_0049:  ldarg.0
    IL_004a:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> C.<F>d__0.<>u__1""
    IL_004f:  stloc.2
    IL_0050:  ldarg.0
    IL_0051:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> C.<F>d__0.<>u__1""
    IL_0056:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_005c:  ldarg.0
    IL_005d:  ldc.i4.m1
    IL_005e:  dup
    IL_005f:  stloc.0
    IL_0060:  stfld      ""int C.<F>d__0.<>1__state""
    IL_0065:  ldloca.s   V_2
    IL_0067:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_006c:  pop
    IL_006d:  ldloca.s   V_2
    IL_006f:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0075:  ldc.i4.s   20
    IL_0077:  stloc.1
    IL_0078:  leave.s    IL_0094
  }
  catch System.Exception
  {
    IL_007a:  stloc.s    V_4
    IL_007c:  ldarg.0
    IL_007d:  ldc.i4.s   -2
    IL_007f:  stfld      ""int C.<F>d__0.<>1__state""
    IL_0084:  ldarg.0
    IL_0085:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__0.<>t__builder""
    IL_008a:  ldloc.s    V_4
    IL_008c:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)""
    IL_0091:  nop
    IL_0092:  leave.s    IL_00a9
  }
  IL_0094:  ldarg.0
  IL_0095:  ldc.i4.s   -2
  IL_0097:  stfld      ""int C.<F>d__0.<>1__state""
  IL_009c:  ldarg.0
  IL_009d:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__0.<>t__builder""
  IL_00a2:  ldloc.1
  IL_00a3:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetResult(int)""
  IL_00a8:  nop
  IL_00a9:  ret
}
");
                    v0.VerifyIL("C.<F>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      168 (0xa8)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_2,
                C.<F>d__0 V_3,
                System.Exception V_4)
 ~IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<F>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
   ~IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_000c
    IL_000a:  br.s       IL_000e
    IL_000c:  br.s       IL_0048
   -IL_000e:  nop
   -IL_000f:  ldc.i4.1
    IL_0010:  call       ""System.Threading.Tasks.Task<int> System.Threading.Tasks.Task.FromResult<int>(int)""
    IL_0015:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_001a:  stloc.2
   ~IL_001b:  ldloca.s   V_2
    IL_001d:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_0022:  brtrue.s   IL_0064
    IL_0024:  ldarg.0
    IL_0025:  ldc.i4.0
    IL_0026:  dup
    IL_0027:  stloc.0
    IL_0028:  stfld      ""int C.<F>d__0.<>1__state""
   <IL_002d:  ldarg.0
    IL_002e:  ldloc.2
    IL_002f:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> C.<F>d__0.<>u__1""
    IL_0034:  ldarg.0
    IL_0035:  stloc.3
    IL_0036:  ldarg.0
    IL_0037:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__0.<>t__builder""
    IL_003c:  ldloca.s   V_2
    IL_003e:  ldloca.s   V_3
    IL_0040:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, C.<F>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref C.<F>d__0)""
    IL_0045:  nop
    IL_0046:  leave.s    IL_00a7
   >IL_0048:  ldarg.0
    IL_0049:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> C.<F>d__0.<>u__1""
    IL_004e:  stloc.2
    IL_004f:  ldarg.0
    IL_0050:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> C.<F>d__0.<>u__1""
    IL_0055:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_005b:  ldarg.0
    IL_005c:  ldc.i4.m1
    IL_005d:  dup
    IL_005e:  stloc.0
    IL_005f:  stfld      ""int C.<F>d__0.<>1__state""
    IL_0064:  ldloca.s   V_2
    IL_0066:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_006b:  pop
    IL_006c:  ldloca.s   V_2
    IL_006e:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
   -IL_0074:  ldc.i4.2
    IL_0075:  stloc.1
    IL_0076:  leave.s    IL_0092
  }
  catch System.Exception
  {
   ~IL_0078:  stloc.s    V_4
    IL_007a:  ldarg.0
    IL_007b:  ldc.i4.s   -2
    IL_007d:  stfld      ""int C.<F>d__0.<>1__state""
    IL_0082:  ldarg.0
    IL_0083:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__0.<>t__builder""
    IL_0088:  ldloc.s    V_4
    IL_008a:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)""
    IL_008f:  nop
    IL_0090:  leave.s    IL_00a7
  }
 -IL_0092:  ldarg.0
  IL_0093:  ldc.i4.s   -2
  IL_0095:  stfld      ""int C.<F>d__0.<>1__state""
 ~IL_009a:  ldarg.0
  IL_009b:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__0.<>t__builder""
  IL_00a0:  ldloc.1
  IL_00a1:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetResult(int)""
  IL_00a6:  nop
  IL_00a7:  ret
}
", sequencePoints: "C+<F>d__0.MoveNext");

                    v0.VerifyPdb("C+<F>d__0.MoveNext", @"
<symbols>
  <methods>
    <method containingType=""C+&lt;F&gt;d__0"" name=""MoveNext"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""27"" offset=""0"" />
          <slot kind=""20"" offset=""0"" />
          <slot kind=""33"" offset=""11"" />
          <slot kind=""temp"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""0"" />
        <entry offset=""0x7"" hidden=""true"" document=""0"" />
        <entry offset=""0xe"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" document=""0"" />
        <entry offset=""0xf"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""34"" document=""0"" />
        <entry offset=""0x1b"" hidden=""true"" document=""0"" />
        <entry offset=""0x74"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""18"" document=""0"" />
        <entry offset=""0x78"" hidden=""true"" document=""0"" />
        <entry offset=""0x92"" startLine=""10"" startColumn=""5"" endLine=""10"" endColumn=""6"" document=""0"" />
        <entry offset=""0x9a"" hidden=""true"" document=""0"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0xa8"">
        <namespace name=""System.Threading.Tasks"" />
      </scope>
      <asyncInfo>
        <kickoffMethod declaringType=""C"" methodName=""F"" />
        <await yield=""0x2d"" resume=""0x48"" declaringType=""C+&lt;F&gt;d__0"" methodName=""MoveNext"" />
      </asyncInfo>
    </method>
  </methods>
</symbols>
");
                }
            }
        }

        [Fact]
        public void UpdateIterator_UserDefinedVariables_NoChange()
        {
            var source0 = @"
using System.Collections.Generic;

class C
{
    static IEnumerable<int> F(int p) 
    {
        int x = p;
        yield return 1;
    }

    public void X() { } // needs to be present to work around SymWriter bug #1068894
}";
            var source1 = @"
using System.Collections.Generic;

class C
{
    static IEnumerable<int> F(int p) 
    {
        int x = p;
        yield return 2;
    }

    public void X() { } // needs to be present to work around SymWriter bug #1068894
}";
            var compilation0 = CreateCompilationWithMscorlib45(source0, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1);

            var v0 = CompileAndVerify(compilation0);
            var symReader = v0.CreateSymReader();

            using (var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData))
            {
                var method0 = compilation0.GetMember<MethodSymbol>("C.F");
                var method1 = compilation1.GetMember<MethodSymbol>("C.F");

                var generation0 = EmitBaseline.CreateInitialBaseline(md0, symReader.GetEncMethodDebugInfo);

                var diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables: true)));

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
                        Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(6, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(13, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(14, TableIndex.CustomAttribute, EditAndContinueOperation.Default));

                    diff1.VerifyIL("C.<F>d__0.System.Collections.IEnumerator.MoveNext", @"
{
  // Code size       75 (0x4b)
  .maxstack  2
  .locals init (int V_0,
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
  IL_0014:  br.s       IL_0040
  IL_0016:  ldc.i4.0
  IL_0017:  stloc.1
  IL_0018:  ldloc.1
  IL_0019:  ret
  IL_001a:  ldarg.0
  IL_001b:  ldc.i4.m1
  IL_001c:  stfld      ""int C.<F>d__0.<>1__state""
  IL_0021:  nop
  IL_0022:  ldarg.0
  IL_0023:  ldarg.0
  IL_0024:  ldfld      ""int C.<F>d__0.p""
  IL_0029:  stfld      ""int C.<F>d__0.<x>5__1""
  IL_002e:  ldarg.0
  IL_002f:  ldc.i4.2
  IL_0030:  stfld      ""int C.<F>d__0.<>2__current""
  IL_0035:  ldarg.0
  IL_0036:  ldc.i4.1
  IL_0037:  stfld      ""int C.<F>d__0.<>1__state""
  IL_003c:  ldc.i4.1
  IL_003d:  stloc.1
  IL_003e:  br.s       IL_0018
  IL_0040:  ldarg.0
  IL_0041:  ldc.i4.m1
  IL_0042:  stfld      ""int C.<F>d__0.<>1__state""
  IL_0047:  ldc.i4.0
  IL_0048:  stloc.1
  IL_0049:  br.s       IL_0018
}
");
                }
            }
        }

        [Fact]
        public void UpdateIterator_UserDefinedVariables_AddVariable()
        {
            var source0 = @"
using System;
using System.Collections.Generic;

class C
{
    static IEnumerable<int> F(int p) 
    {
        int x = p;
        yield return x;
    }

    public void X() { } // needs to be present to work around SymWriter bug #1068894
}";
            var source1 = @"
using System;
using System.Collections.Generic;

class C
{
    static IEnumerable<int> F(int p) 
    {
        int y = 1234;
        int x = p;
        yield return y;
        Console.WriteLine(x);
    }

    public void X() { } // needs to be present to work around SymWriter bug #1068894
}";
            var compilation0 = CreateCompilationWithMscorlib45(source0, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1);

            var v0 = CompileAndVerify(compilation0);
            var symReader = v0.CreateSymReader();

            using (var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData))
            {
                var method0 = compilation0.GetMember<MethodSymbol>("C.F");
                var method1 = compilation1.GetMember<MethodSymbol>("C.F");

                var generation0 = EmitBaseline.CreateInitialBaseline(md0, symReader.GetEncMethodDebugInfo);
                var diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables: true)));

                // Verify delta metadata contains expected rows.
                using (var md1 = diff1.GetMetadata())
                {
                    // 1 field def added & 3 methods updated
                    CheckEncLogDefinitions(md1.Reader,
                        Row(4, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(5, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                        Row(7, TableIndex.Field, EditAndContinueOperation.Default),
                        Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(6, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(13, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(14, TableIndex.CustomAttribute, EditAndContinueOperation.Default));

                    diff1.VerifyIL("C.<F>d__0.System.Collections.IEnumerator.MoveNext", @"
{
  // Code size      103 (0x67)
  .maxstack  2
  .locals init (int V_0,
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
  IL_0014:  br.s       IL_0050
  IL_0016:  ldc.i4.0
  IL_0017:  stloc.1
  IL_0018:  ldloc.1
  IL_0019:  ret
  IL_001a:  ldarg.0
  IL_001b:  ldc.i4.m1
  IL_001c:  stfld      ""int C.<F>d__0.<>1__state""
  IL_0021:  nop
  IL_0022:  ldarg.0
  IL_0023:  ldc.i4     0x4d2
  IL_0028:  stfld      ""int C.<F>d__0.<y>5__2""
  IL_002d:  ldarg.0
  IL_002e:  ldarg.0
  IL_002f:  ldfld      ""int C.<F>d__0.p""
  IL_0034:  stfld      ""int C.<F>d__0.<x>5__1""
  IL_0039:  ldarg.0
  IL_003a:  ldarg.0
  IL_003b:  ldfld      ""int C.<F>d__0.<y>5__2""
  IL_0040:  stfld      ""int C.<F>d__0.<>2__current""
  IL_0045:  ldarg.0
  IL_0046:  ldc.i4.1
  IL_0047:  stfld      ""int C.<F>d__0.<>1__state""
  IL_004c:  ldc.i4.1
  IL_004d:  stloc.1
  IL_004e:  br.s       IL_0018
  IL_0050:  ldarg.0
  IL_0051:  ldc.i4.m1
  IL_0052:  stfld      ""int C.<F>d__0.<>1__state""
  IL_0057:  ldarg.0
  IL_0058:  ldfld      ""int C.<F>d__0.<x>5__1""
  IL_005d:  call       ""void System.Console.WriteLine(int)""
  IL_0062:  nop
  IL_0063:  ldc.i4.0
  IL_0064:  stloc.1
  IL_0065:  br.s       IL_0018
}
");
                }
            }
        }

        [Fact]
        public void UpdateIterator_UserDefinedVariables_AddAndRemoveVariable()
        {
            var source0 = @"
using System;
using System.Collections.Generic;

class C
{
    static IEnumerable<int> F(int p) 
    {
        int x = p;
        yield return x;
    }

    public void X() { } // needs to be present to work around SymWriter bug #1068894
}";
            var source1 = @"
using System;
using System.Collections.Generic;

class C
{
    static IEnumerable<int> F(int p) 
    {
        int y = 1234;
        yield return y;
        Console.WriteLine(p);
    }

    public void X() { } // needs to be present to work around SymWriter bug #1068894
}";
            var compilation0 = CreateCompilationWithMscorlib45(source0, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1);

            var v0 = CompileAndVerify(compilation0);
            var symReader = v0.CreateSymReader();

            using (var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData))
            {
                var method0 = compilation0.GetMember<MethodSymbol>("C.F");
                var method1 = compilation1.GetMember<MethodSymbol>("C.F");

                var generation0 = EmitBaseline.CreateInitialBaseline(md0, symReader.GetEncMethodDebugInfo);
                var diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables: true)));

                // Verify delta metadata contains expected rows.
                using (var md1 = diff1.GetMetadata())
                {
                    // 1 field def added & 3 methods updated
                    CheckEncLogDefinitions(md1.Reader,
                        Row(4, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(5, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                        Row(7, TableIndex.Field, EditAndContinueOperation.Default),
                        Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(6, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(13, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(14, TableIndex.CustomAttribute, EditAndContinueOperation.Default));

                    diff1.VerifyIL("C.<F>d__0.System.Collections.IEnumerator.MoveNext", @"
{
  // Code size       91 (0x5b)
  .maxstack  2
  .locals init (int V_0,
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
  IL_0014:  br.s       IL_0044
  IL_0016:  ldc.i4.0
  IL_0017:  stloc.1
  IL_0018:  ldloc.1
  IL_0019:  ret
  IL_001a:  ldarg.0
  IL_001b:  ldc.i4.m1
  IL_001c:  stfld      ""int C.<F>d__0.<>1__state""
  IL_0021:  nop
  IL_0022:  ldarg.0
  IL_0023:  ldc.i4     0x4d2
  IL_0028:  stfld      ""int C.<F>d__0.<y>5__2""
  IL_002d:  ldarg.0
  IL_002e:  ldarg.0
  IL_002f:  ldfld      ""int C.<F>d__0.<y>5__2""
  IL_0034:  stfld      ""int C.<F>d__0.<>2__current""
  IL_0039:  ldarg.0
  IL_003a:  ldc.i4.1
  IL_003b:  stfld      ""int C.<F>d__0.<>1__state""
  IL_0040:  ldc.i4.1
  IL_0041:  stloc.1
  IL_0042:  br.s       IL_0018
  IL_0044:  ldarg.0
  IL_0045:  ldc.i4.m1
  IL_0046:  stfld      ""int C.<F>d__0.<>1__state""
  IL_004b:  ldarg.0
  IL_004c:  ldfld      ""int C.<F>d__0.p""
  IL_0051:  call       ""void System.Console.WriteLine(int)""
  IL_0056:  nop
  IL_0057:  ldc.i4.0
  IL_0058:  stloc.1
  IL_0059:  br.s       IL_0018
}
");
                }
            }
        }

        [Fact]
        public void UpdateIterator_UserDefinedVariables_ChangeVariableType()
        {
            var source0 = @"
using System;
using System.Collections.Generic;

class C
{
    static IEnumerable<int> F() 
    {
        var x = 1;
        yield return 1;
        Console.WriteLine(x);
    }

    public void X() { } // needs to be present to work around SymWriter bug #1068894
}";
            var source1 = @"
using System;
using System.Collections.Generic;

class C
{
    static IEnumerable<int> F() 
    {
        var x = 1.0;
        yield return 2;
        Console.WriteLine(x);
    }

    public void X() { } // needs to be present to work around SymWriter bug #1068894
}";
            var compilation0 = CreateCompilationWithMscorlib45(source0, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1);

            var v0 = CompileAndVerify(compilation0);
            var symReader = v0.CreateSymReader();

            using (var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData))
            {
                var method0 = compilation0.GetMember<MethodSymbol>("C.F");
                var method1 = compilation1.GetMember<MethodSymbol>("C.F");

                var generation0 = EmitBaseline.CreateInitialBaseline(md0, symReader.GetEncMethodDebugInfo);
                var diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables: true)));

                // Verify delta metadata contains expected rows.
                using (var md1 = diff1.GetMetadata())
                {
                    // 1 field def added & 3 methods updated
                    CheckEncLogDefinitions(md1.Reader,
                        Row(4, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(5, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                        Row(5, TableIndex.Field, EditAndContinueOperation.Default),
                        Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(6, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(13, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(14, TableIndex.CustomAttribute, EditAndContinueOperation.Default));

                    diff1.VerifyIL("C.<F>d__0.System.Collections.IEnumerator.MoveNext", @"
{
  // Code size       90 (0x5a)
  .maxstack  2
  .locals init (int V_0,
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
  IL_0014:  br.s       IL_0043
  IL_0016:  ldc.i4.0
  IL_0017:  stloc.1
  IL_0018:  ldloc.1
  IL_0019:  ret
  IL_001a:  ldarg.0
  IL_001b:  ldc.i4.m1
  IL_001c:  stfld      ""int C.<F>d__0.<>1__state""
  IL_0021:  nop
  IL_0022:  ldarg.0
  IL_0023:  ldc.r8     1
  IL_002c:  stfld      ""double C.<F>d__0.<x>5__2""
  IL_0031:  ldarg.0
  IL_0032:  ldc.i4.2
  IL_0033:  stfld      ""int C.<F>d__0.<>2__current""
  IL_0038:  ldarg.0
  IL_0039:  ldc.i4.1
  IL_003a:  stfld      ""int C.<F>d__0.<>1__state""
  IL_003f:  ldc.i4.1
  IL_0040:  stloc.1
  IL_0041:  br.s       IL_0018
  IL_0043:  ldarg.0
  IL_0044:  ldc.i4.m1
  IL_0045:  stfld      ""int C.<F>d__0.<>1__state""
  IL_004a:  ldarg.0
  IL_004b:  ldfld      ""double C.<F>d__0.<x>5__2""
  IL_0050:  call       ""void System.Console.WriteLine(double)""
  IL_0055:  nop
  IL_0056:  ldc.i4.0
  IL_0057:  stloc.1
  IL_0058:  br.s       IL_0018
}
");
                }
            }
        }

        [Fact]
        public void UpdateIterator_SynthesizedVariables_ChangeVariableType()
        {
            var source0 = @"
using System;
using System.Collections.Generic;

class C
{
    static IEnumerable<int> F() 
    {
        foreach (object item in new[] { 1 }) { yield return 1; }
    }

    public void X() { } // needs to be present to work around SymWriter bug #1068894
}";
            var source1 = @"
using System;
using System.Collections.Generic;

class C
{
    static IEnumerable<int> F() 
    {
        foreach (object item in new[] { 1.0 }) { yield return 1; }
    }

    public void X() { } // needs to be present to work around SymWriter bug #1068894
}";
            // Rude edit but the compiler should handle it.

            var compilation0 = CreateCompilationWithMscorlib45(source0, options: ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            var compilation1 = compilation0.WithSource(source1);

            var v0 = CompileAndVerify(compilation0, symbolValidator: module =>
            {
                Assert.Equal(new[]
                {
                    "<>1__state: int",
                    "<>2__current: int",
                    "<>l__initialThreadId: int",
                    "<>s__1: int[]",
                    "<>s__2: int",
                    "<item>5__3: object"
                }, module.GetFieldNamesAndTypes("C.<F>d__0"));
            });

            var symReader = v0.CreateSymReader();

            using (var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData))
            {
                var method0 = compilation0.GetMember<MethodSymbol>("C.F");
                var method1 = compilation1.GetMember<MethodSymbol>("C.F");

                var generation0 = EmitBaseline.CreateInitialBaseline(md0, symReader.GetEncMethodDebugInfo);
                var diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, method0, method1, GetSyntaxMapByKind(method0, SyntaxKind.ForEachStatement), preserveLocalVariables: true)));

                // Verify delta metadata contains expected rows.
                using (var md1 = diff1.GetMetadata())
                {
                    // 1 field def added & 3 methods updated
                    CheckEncLogDefinitions(md1.Reader,
                        Row(4, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(5, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                        Row(7, TableIndex.Field, EditAndContinueOperation.Default),
                        Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(6, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(13, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(14, TableIndex.CustomAttribute, EditAndContinueOperation.Default));

                    diff1.VerifyIL("C.<F>d__0.System.Collections.IEnumerator.MoveNext", @"
{
  // Code size      170 (0xaa)
  .maxstack  5
  .locals init (int V_0,
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
  IL_0014:  br.s       IL_006f
  IL_0016:  ldc.i4.0
  IL_0017:  stloc.1
  IL_0018:  ldloc.1
  IL_0019:  ret
  IL_001a:  ldarg.0
  IL_001b:  ldc.i4.m1
  IL_001c:  stfld      ""int C.<F>d__0.<>1__state""
  IL_0021:  nop
  IL_0022:  nop
  IL_0023:  ldarg.0
  IL_0024:  ldc.i4.1
  IL_0025:  newarr     ""double""
  IL_002a:  dup
  IL_002b:  ldc.i4.0
  IL_002c:  ldc.r8     1
  IL_0035:  stelem.r8
  IL_0036:  stfld      ""double[] C.<F>d__0.<>s__4""
  IL_003b:  ldarg.0
  IL_003c:  ldc.i4.0
  IL_003d:  stfld      ""int C.<F>d__0.<>s__2""
  IL_0042:  br.s       IL_008c
  IL_0044:  ldarg.0
  IL_0045:  ldarg.0
  IL_0046:  ldfld      ""double[] C.<F>d__0.<>s__4""
  IL_004b:  ldarg.0
  IL_004c:  ldfld      ""int C.<F>d__0.<>s__2""
  IL_0051:  ldelem.r8
  IL_0052:  box        ""double""
  IL_0057:  stfld      ""object C.<F>d__0.<item>5__3""
  IL_005c:  nop
  IL_005d:  ldarg.0
  IL_005e:  ldc.i4.1
  IL_005f:  stfld      ""int C.<F>d__0.<>2__current""
  IL_0064:  ldarg.0
  IL_0065:  ldc.i4.1
  IL_0066:  stfld      ""int C.<F>d__0.<>1__state""
  IL_006b:  ldc.i4.1
  IL_006c:  stloc.1
  IL_006d:  br.s       IL_0018
  IL_006f:  ldarg.0
  IL_0070:  ldc.i4.m1
  IL_0071:  stfld      ""int C.<F>d__0.<>1__state""
  IL_0076:  nop
  IL_0077:  ldarg.0
  IL_0078:  ldnull
  IL_0079:  stfld      ""object C.<F>d__0.<item>5__3""
  IL_007e:  ldarg.0
  IL_007f:  ldarg.0
  IL_0080:  ldfld      ""int C.<F>d__0.<>s__2""
  IL_0085:  ldc.i4.1
  IL_0086:  add
  IL_0087:  stfld      ""int C.<F>d__0.<>s__2""
  IL_008c:  ldarg.0
  IL_008d:  ldfld      ""int C.<F>d__0.<>s__2""
  IL_0092:  ldarg.0
  IL_0093:  ldfld      ""double[] C.<F>d__0.<>s__4""
  IL_0098:  ldlen
  IL_0099:  conv.i4
  IL_009a:  blt.s      IL_0044
  IL_009c:  ldarg.0
  IL_009d:  ldnull
  IL_009e:  stfld      ""double[] C.<F>d__0.<>s__4""
  IL_00a3:  ldc.i4.0
  IL_00a4:  stloc.1
  IL_00a5:  br         IL_0018
}
");
                }
            }
        }

        [Fact]
        public void HoistedVariables_MultipleGenerations()
        {
            var source0 = @"
using System.Threading.Tasks;

class C
{
    static async Task<int> F() // testing type changes G0 -> G1, G1 -> G2
    {
        bool a1 = true; 
        int a2 = 3;
        await Task.Delay(0);
        return 1;
    }

    static async Task<int> G() // testing G1 -> G3
    {
        C c = new C();
        bool a1 = true;
        await Task.Delay(0);
        return 1;
    }

    static async Task<int> H() // testing G0 -> G3
    {
        C c = new C();
        bool a1 = true;
        await Task.Delay(0);
        return 1;
    }

    public void X() { } // needs to be present to work around SymWriter bug #1068894
}";
            var source1 = @"
using System.Threading.Tasks;

class C
{
    static async Task<int> F() // updated 
    {
        C a1 = new C(); 
        int a2 = 3;
        await Task.Delay(0);
        return 1;
    }

    static async Task<int> G() // updated 
    {
        C c = new C();
        bool a1 = true;
        await Task.Delay(0);
        return 2;
    }

    static async Task<int> H() 
    {
        C c = new C();
        bool a1 = true;
        await Task.Delay(0);
        return 1;
    }

    public void X() { } // needs to be present to work around SymWriter bug #1068894
}";
            var source2 = @"
using System.Threading.Tasks;

class C
{
    static async Task<int> F()  // updated
    {
        bool a1 = true; 
        C a2 = new C();
        await Task.Delay(0);
        return 1;
    }

    static async Task<int> G()
    {
        C c = new C();
        bool a1 = true;
        await Task.Delay(0);
        return 2;
    }

    static async Task<int> H() 
    {
        C c = new C();
        bool a1 = true;
        await Task.Delay(0);
        return 1;
    }

    public void X() { } // needs to be present to work around SymWriter bug #1068894
}";
            var source3 = @"
using System.Threading.Tasks;

class C
{
    static async Task<int> F() 
    {
        bool a1 = true; 
        C a2 = new C();
        await Task.Delay(0);
        return 1;
    }

    static async Task<int> G() // updated
    {
        C c = new C();
        C a1 = new C();
        await Task.Delay(0);
        return 1;
    }

    static async Task<int> H() // updated
    {
        C c = new C();
        C a1 = new C();
        await Task.Delay(0);
        return 1;
    }

    public void X() { } // needs to be present to work around SymWriter bug #1068894
}";

            // Rude edit but the compiler should handle it.

            var compilation0 = CreateCompilationWithMscorlib45(source0, options: ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            var compilation1 = compilation0.WithSource(source1);
            var compilation2 = compilation1.WithSource(source2);
            var compilation3 = compilation2.WithSource(source3);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");
            var f2 = compilation2.GetMember<MethodSymbol>("C.F");
            var f3 = compilation3.GetMember<MethodSymbol>("C.F");

            var g0 = compilation0.GetMember<MethodSymbol>("C.G");
            var g1 = compilation1.GetMember<MethodSymbol>("C.G");
            var g2 = compilation2.GetMember<MethodSymbol>("C.G");
            var g3 = compilation3.GetMember<MethodSymbol>("C.G");

            var h0 = compilation0.GetMember<MethodSymbol>("C.H");
            var h1 = compilation1.GetMember<MethodSymbol>("C.H");
            var h2 = compilation2.GetMember<MethodSymbol>("C.H");
            var h3 = compilation3.GetMember<MethodSymbol>("C.H");

            var v0 = CompileAndVerify(compilation0, symbolValidator: module =>
            {
                Assert.Equal(new[]
                {
                    "<>1__state: int",
                    "<>t__builder: System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>",
                    "<a1>5__1: bool",
                    "<a2>5__2: int",
                    "<>u__1: System.Runtime.CompilerServices.TaskAwaiter"
                }, module.GetFieldNamesAndTypes("C.<F>d__0"));
            });

            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, f0, f1, GetEquivalentNodesMap(f1, f0), preserveLocalVariables: true),
                    new SemanticEdit(SemanticEditKind.Update, g0, g1, GetEquivalentNodesMap(g1, g0), preserveLocalVariables: true)));

            diff1.VerifySynthesizedMembers(
                "C: {<F>d__0, <G>d__1}",
                "C.<F>d__0: {<>1__state, <>t__builder, <a1>5__3, <a2>5__2, <>u__1, MoveNext, SetStateMachine}",
                "C.<G>d__1: {<>1__state, <>t__builder, <c>5__1, <a1>5__2, <>u__1, MoveNext, SetStateMachine}");

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, f1, f2, GetEquivalentNodesMap(f2, f1), preserveLocalVariables: true)));

            diff2.VerifySynthesizedMembers(
                "C: {<F>d__0, <G>d__1}",
                "C.<F>d__0: {<>1__state, <>t__builder, <a1>5__4, <a2>5__5, <>u__1, MoveNext, SetStateMachine, <a1>5__3, <a2>5__2}",
                "C.<G>d__1: {<>1__state, <>t__builder, <c>5__1, <a1>5__2, <>u__1, MoveNext, SetStateMachine}");

            var diff3 = compilation3.EmitDifference(
                diff2.NextGeneration,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, g2, g3, GetEquivalentNodesMap(g3, g2), preserveLocalVariables: true),
                    new SemanticEdit(SemanticEditKind.Update, h2, h3, GetEquivalentNodesMap(h3, h2), preserveLocalVariables: true)));

            diff3.VerifySynthesizedMembers(
                "C: {<G>d__1, <H>d__2, <F>d__0}",
                "C.<F>d__0: {<>1__state, <>t__builder, <a1>5__4, <a2>5__5, <>u__1, MoveNext, SetStateMachine, <a1>5__3, <a2>5__2}",
                "C.<G>d__1: {<>1__state, <>t__builder, <c>5__1, <a1>5__3, <>u__1, MoveNext, SetStateMachine, <a1>5__2}",
                "C.<H>d__2: {<>1__state, <>t__builder, <c>5__1, <a1>5__3, <>u__1, MoveNext, SetStateMachine}");

            // Verify delta metadata contains expected rows.
            var md1 = diff1.GetMetadata();
            var md2 = diff2.GetMetadata();
            var md3 = diff3.GetMetadata();

            // 1 field def added & 4 methods updated (MoveNext and kickoff for F and G)
            CheckEncLogDefinitions(md1.Reader,
                Row(7, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(8, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(9, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(10, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                Row(16, TableIndex.Field, EditAndContinueOperation.Default),
                Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(7, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(10, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(13, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(14, TableIndex.CustomAttribute, EditAndContinueOperation.Default));

            diff1.VerifyIL("C.<F>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      186 (0xba)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                System.Runtime.CompilerServices.TaskAwaiter V_2,
                C.<F>d__0 V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<F>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_000c
    IL_000a:  br.s       IL_000e
    IL_000c:  br.s       IL_005a
    IL_000e:  nop
    IL_000f:  ldarg.0
    IL_0010:  newobj     ""C..ctor()""
    IL_0015:  stfld      ""C C.<F>d__0.<a1>5__3""
    IL_001a:  ldarg.0
    IL_001b:  ldc.i4.3
    IL_001c:  stfld      ""int C.<F>d__0.<a2>5__2""
    IL_0021:  ldc.i4.0
    IL_0022:  call       ""System.Threading.Tasks.Task System.Threading.Tasks.Task.Delay(int)""
    IL_0027:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter System.Threading.Tasks.Task.GetAwaiter()""
    IL_002c:  stloc.2
    IL_002d:  ldloca.s   V_2
    IL_002f:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter.IsCompleted.get""
    IL_0034:  brtrue.s   IL_0076
    IL_0036:  ldarg.0
    IL_0037:  ldc.i4.0
    IL_0038:  dup
    IL_0039:  stloc.0
    IL_003a:  stfld      ""int C.<F>d__0.<>1__state""
    IL_003f:  ldarg.0
    IL_0040:  ldloc.2
    IL_0041:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<F>d__0.<>u__1""
    IL_0046:  ldarg.0
    IL_0047:  stloc.3
    IL_0048:  ldarg.0
    IL_0049:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__0.<>t__builder""
    IL_004e:  ldloca.s   V_2
    IL_0050:  ldloca.s   V_3
    IL_0052:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter, C.<F>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter, ref C.<F>d__0)""
    IL_0057:  nop
    IL_0058:  leave.s    IL_00b9
    IL_005a:  ldarg.0
    IL_005b:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<F>d__0.<>u__1""
    IL_0060:  stloc.2
    IL_0061:  ldarg.0
    IL_0062:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter C.<F>d__0.<>u__1""
    IL_0067:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_006d:  ldarg.0
    IL_006e:  ldc.i4.m1
    IL_006f:  dup
    IL_0070:  stloc.0
    IL_0071:  stfld      ""int C.<F>d__0.<>1__state""
    IL_0076:  ldloca.s   V_2
    IL_0078:  call       ""void System.Runtime.CompilerServices.TaskAwaiter.GetResult()""
    IL_007d:  nop
    IL_007e:  ldloca.s   V_2
    IL_0080:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_0086:  ldc.i4.1
    IL_0087:  stloc.1
    IL_0088:  leave.s    IL_00a4
  }
  catch System.Exception
  {
    IL_008a:  stloc.s    V_4
    IL_008c:  ldarg.0
    IL_008d:  ldc.i4.s   -2
    IL_008f:  stfld      ""int C.<F>d__0.<>1__state""
    IL_0094:  ldarg.0
    IL_0095:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__0.<>t__builder""
    IL_009a:  ldloc.s    V_4
    IL_009c:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)""
    IL_00a1:  nop
    IL_00a2:  leave.s    IL_00b9
  }
  IL_00a4:  ldarg.0
  IL_00a5:  ldc.i4.s   -2
  IL_00a7:  stfld      ""int C.<F>d__0.<>1__state""
  IL_00ac:  ldarg.0
  IL_00ad:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__0.<>t__builder""
  IL_00b2:  ldloc.1
  IL_00b3:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetResult(int)""
  IL_00b8:  nop
  IL_00b9:  ret
}
");
            // 2 field defs added (both variables a1 and a2 of F changed their types) & 2 methods updated
            CheckEncLogDefinitions(md2.Reader,
                Row(11, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(12, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                Row(17, TableIndex.Field, EditAndContinueOperation.Default),
                Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                Row(18, TableIndex.Field, EditAndContinueOperation.Default),
                Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(7, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(15, TableIndex.CustomAttribute, EditAndContinueOperation.Default));

            diff2.VerifyIL("C.<F>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      186 (0xba)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                System.Runtime.CompilerServices.TaskAwaiter V_2,
                C.<F>d__0 V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<F>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_000c
    IL_000a:  br.s       IL_000e
    IL_000c:  br.s       IL_005a
    IL_000e:  nop
    IL_000f:  ldarg.0
    IL_0010:  ldc.i4.1
    IL_0011:  stfld      ""bool C.<F>d__0.<a1>5__4""
    IL_0016:  ldarg.0
    IL_0017:  newobj     ""C..ctor()""
    IL_001c:  stfld      ""C C.<F>d__0.<a2>5__5""
    IL_0021:  ldc.i4.0
    IL_0022:  call       ""System.Threading.Tasks.Task System.Threading.Tasks.Task.Delay(int)""
    IL_0027:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter System.Threading.Tasks.Task.GetAwaiter()""
    IL_002c:  stloc.2
    IL_002d:  ldloca.s   V_2
    IL_002f:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter.IsCompleted.get""
    IL_0034:  brtrue.s   IL_0076
    IL_0036:  ldarg.0
    IL_0037:  ldc.i4.0
    IL_0038:  dup
    IL_0039:  stloc.0
    IL_003a:  stfld      ""int C.<F>d__0.<>1__state""
    IL_003f:  ldarg.0
    IL_0040:  ldloc.2
    IL_0041:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<F>d__0.<>u__1""
    IL_0046:  ldarg.0
    IL_0047:  stloc.3
    IL_0048:  ldarg.0
    IL_0049:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__0.<>t__builder""
    IL_004e:  ldloca.s   V_2
    IL_0050:  ldloca.s   V_3
    IL_0052:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter, C.<F>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter, ref C.<F>d__0)""
    IL_0057:  nop
    IL_0058:  leave.s    IL_00b9
    IL_005a:  ldarg.0
    IL_005b:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<F>d__0.<>u__1""
    IL_0060:  stloc.2
    IL_0061:  ldarg.0
    IL_0062:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter C.<F>d__0.<>u__1""
    IL_0067:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_006d:  ldarg.0
    IL_006e:  ldc.i4.m1
    IL_006f:  dup
    IL_0070:  stloc.0
    IL_0071:  stfld      ""int C.<F>d__0.<>1__state""
    IL_0076:  ldloca.s   V_2
    IL_0078:  call       ""void System.Runtime.CompilerServices.TaskAwaiter.GetResult()""
    IL_007d:  nop
    IL_007e:  ldloca.s   V_2
    IL_0080:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_0086:  ldc.i4.1
    IL_0087:  stloc.1
    IL_0088:  leave.s    IL_00a4
  }
  catch System.Exception
  {
    IL_008a:  stloc.s    V_4
    IL_008c:  ldarg.0
    IL_008d:  ldc.i4.s   -2
    IL_008f:  stfld      ""int C.<F>d__0.<>1__state""
    IL_0094:  ldarg.0
    IL_0095:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__0.<>t__builder""
    IL_009a:  ldloc.s    V_4
    IL_009c:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)""
    IL_00a1:  nop
    IL_00a2:  leave.s    IL_00b9
  }
  IL_00a4:  ldarg.0
  IL_00a5:  ldc.i4.s   -2
  IL_00a7:  stfld      ""int C.<F>d__0.<>1__state""
  IL_00ac:  ldarg.0
  IL_00ad:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__0.<>t__builder""
  IL_00b2:  ldloc.1
  IL_00b3:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetResult(int)""
  IL_00b8:  nop
  IL_00b9:  ret
}
");
            // 2 field defs added - variables of G and H changed their types; 4 methods updated: G, H kickoff and MoveNext
            CheckEncLogDefinitions(md3.Reader,
                Row(13, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(14, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(15, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(16, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                Row(19, TableIndex.Field, EditAndContinueOperation.Default),
                Row(5, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                Row(20, TableIndex.Field, EditAndContinueOperation.Default),
                Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(3, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(10, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(13, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(16, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(17, TableIndex.CustomAttribute, EditAndContinueOperation.Default));
        }

        [Fact]
        public void Awaiters1()
        {
            var source0 = @"
using System.Threading.Tasks;

class C
{
    static Task<bool> A1() => null;
    static Task<int> A2() => null;
    static Task<double> A3() => null;

    static async Task<int> F() 
    {
        await A1(); 
        await A2();
        return 1;
    }

    static async Task<int> G() 
    {
        await A2(); 
        await A1();
        return 1;
    }
}";
            var compilation0 = CreateCompilationWithMscorlib45(source0, options: ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All));

            CompileAndVerify(compilation0, symbolValidator: module =>
            {
                Assert.Equal(new[]
                {
                    "<>1__state: int",
                    "<>t__builder: System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>",
                    "<>u__1: System.Runtime.CompilerServices.TaskAwaiter<bool>",
                    "<>u__2: System.Runtime.CompilerServices.TaskAwaiter<int>"
                }, module.GetFieldNamesAndTypes("C.<F>d__3"));

                Assert.Equal(new[]
                {
                    "<>1__state: int",
                    "<>t__builder: System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>",
                    "<>u__1: System.Runtime.CompilerServices.TaskAwaiter<int>",
                    "<>u__2: System.Runtime.CompilerServices.TaskAwaiter<bool>"
                }, module.GetFieldNamesAndTypes("C.<G>d__4"));
            });
        }

        [Fact]
        public void Awaiters_MultipleGenerations()
        {
            var source0 = @"
using System.Threading.Tasks;

class C
{
    static Task<bool> A1() => null;
    static Task<int> A2() => null;
    static Task<C> A3() => null;

    static async Task<int> F() // testing type changes G0 -> G1, G1 -> G2
    {
        await A1(); 
        await A2();
        return 1;
    }

    static async Task<int> G() // testing G1 -> G3
    {
        await A1();
        return 1;
    }

    static async Task<int> H() // testing G0 -> G3
    {
        await A1();
        return 1;
    }
}";
            var source1 = @"
using System.Threading.Tasks;

class C
{
    static Task<bool> A1() => null;
    static Task<int> A2() => null;
    static Task<C> A3() => null;

    static async Task<int> F() // updated 
    {
        await A3(); 
        await A2();
        return 1;
    }

    static async Task<int> G() // updated 
    {
        await A1();
        return 2;
    }

    static async Task<int> H() 
    {
        await A1();
        return 1;
    }
}";
            var source2 = @"
using System.Threading.Tasks;

class C
{
    static Task<bool> A1() => null;
    static Task<int> A2() => null;
    static Task<C> A3() => null;

    static async Task<int> F()  // updated
    {
        await A1(); 
        await A3();
        return 1;
    }

    static async Task<int> G()
    {
        await A1();
        return 2;
    }

    static async Task<int> H() 
    {
        await A1();
        return 1;
    }
}";
            var source3 = @"
using System.Threading.Tasks;

class C
{
    static Task<bool> A1() => null;
    static Task<int> A2() => null;
    static Task<C> A3() => null;

    static async Task<int> F() 
    {
        await A1(); 
        await A3();
        return 1;
    }

    static async Task<int> G() // updated
    {
        await A3();
        return 1;
    }

    static async Task<int> H() // updated
    {
        await A3();
        return 1;
    }
}";

            // Rude edit but the compiler should handle it.

            var compilation0 = CreateCompilationWithMscorlib45(source0, options: ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All), assemblyName: "A");
            var compilation1 = compilation0.WithSource(source1);
            var compilation2 = compilation1.WithSource(source2);
            var compilation3 = compilation2.WithSource(source3);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");
            var f2 = compilation2.GetMember<MethodSymbol>("C.F");
            var f3 = compilation3.GetMember<MethodSymbol>("C.F");

            var g0 = compilation0.GetMember<MethodSymbol>("C.G");
            var g1 = compilation1.GetMember<MethodSymbol>("C.G");
            var g2 = compilation2.GetMember<MethodSymbol>("C.G");
            var g3 = compilation3.GetMember<MethodSymbol>("C.G");

            var h0 = compilation0.GetMember<MethodSymbol>("C.H");
            var h1 = compilation1.GetMember<MethodSymbol>("C.H");
            var h2 = compilation2.GetMember<MethodSymbol>("C.H");
            var h3 = compilation3.GetMember<MethodSymbol>("C.H");

            var v0 = CompileAndVerify(compilation0, symbolValidator: module =>
            {
                Assert.Equal(new[]
                {
                    "<>1__state: int",
                    "<>t__builder: System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>",
                    "<>u__1: System.Runtime.CompilerServices.TaskAwaiter<bool>",
                    "<>u__2: System.Runtime.CompilerServices.TaskAwaiter<int>"
                }, module.GetFieldNamesAndTypes("C.<F>d__3"));
            });

            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapByKind(f0, SyntaxKind.Block), preserveLocalVariables: true),
                    new SemanticEdit(SemanticEditKind.Update, g0, g1, GetSyntaxMapByKind(g0, SyntaxKind.Block), preserveLocalVariables: true)));

            diff1.VerifySynthesizedMembers(
                "C: {<F>d__3, <G>d__4}",
                "C.<F>d__3: {<>1__state, <>t__builder, <>u__3, <>u__2, MoveNext, SetStateMachine}",
                "C.<G>d__4: {<>1__state, <>t__builder, <>u__1, MoveNext, SetStateMachine}");

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, f1, f2, GetSyntaxMapByKind(f1, SyntaxKind.Block), preserveLocalVariables: true)));

            diff2.VerifySynthesizedMembers(
                "C: {<F>d__3, <G>d__4}",
                "C.<F>d__3: {<>1__state, <>t__builder, <>u__4, <>u__3, MoveNext, SetStateMachine, <>u__2}",
                "C.<G>d__4: {<>1__state, <>t__builder, <>u__1, MoveNext, SetStateMachine}");

            var diff3 = compilation3.EmitDifference(
                diff2.NextGeneration,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, g2, g3, GetSyntaxMapByKind(g2, SyntaxKind.Block), preserveLocalVariables: true),
                    new SemanticEdit(SemanticEditKind.Update, h2, h3, GetSyntaxMapByKind(h2, SyntaxKind.Block), preserveLocalVariables: true)));

            diff3.VerifySynthesizedMembers(
                "C: {<G>d__4, <H>d__5, <F>d__3}",
                "C.<G>d__4: {<>1__state, <>t__builder, <>u__2, MoveNext, SetStateMachine, <>u__1}",
                "C.<H>d__5: {<>1__state, <>t__builder, <>u__2, MoveNext, SetStateMachine}",
                "C.<F>d__3: {<>1__state, <>t__builder, <>u__4, <>u__3, MoveNext, SetStateMachine, <>u__2}");

            // Verify delta metadata contains expected rows.
            var md1 = diff1.GetMetadata();
            var md2 = diff2.GetMetadata();
            var md3 = diff3.GetMetadata();

            // 1 field def added & 4 methods updated (MoveNext and kickoff for F and G)
            CheckEncLogDefinitions(md1.Reader,
                Row(10, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(11, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(12, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(13, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                Row(11, TableIndex.Field, EditAndContinueOperation.Default),
                Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(9, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(12, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(13, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(14, TableIndex.CustomAttribute, EditAndContinueOperation.Default));

            // Note that the new awaiter is allocated slot <>u__3 since <>u__1 and <>u__2 are taken.
            diff1.VerifyIL("C.<F>d__3.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      284 (0x11c)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                System.Runtime.CompilerServices.TaskAwaiter<C> V_2,
                C.<F>d__3 V_3,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_4,
                System.Exception V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<F>d__3.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0012
    IL_000a:  br.s       IL_000c
    IL_000c:  ldloc.0
    IL_000d:  ldc.i4.1
    IL_000e:  beq.s      IL_0014
    IL_0010:  br.s       IL_0019
    IL_0012:  br.s       IL_0055
    IL_0014:  br         IL_00bb
    IL_0019:  nop
    IL_001a:  call       ""System.Threading.Tasks.Task<C> C.A3()""
    IL_001f:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<C> System.Threading.Tasks.Task<C>.GetAwaiter()""
    IL_0024:  stloc.2
    IL_0025:  ldloca.s   V_2
    IL_0027:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<C>.IsCompleted.get""
    IL_002c:  brtrue.s   IL_0071
    IL_002e:  ldarg.0
    IL_002f:  ldc.i4.0
    IL_0030:  dup
    IL_0031:  stloc.0
    IL_0032:  stfld      ""int C.<F>d__3.<>1__state""
    IL_0037:  ldarg.0
    IL_0038:  ldloc.2
    IL_0039:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<C> C.<F>d__3.<>u__3""
    IL_003e:  ldarg.0
    IL_003f:  stloc.3
    IL_0040:  ldarg.0
    IL_0041:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__3.<>t__builder""
    IL_0046:  ldloca.s   V_2
    IL_0048:  ldloca.s   V_3
    IL_004a:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<C>, C.<F>d__3>(ref System.Runtime.CompilerServices.TaskAwaiter<C>, ref C.<F>d__3)""
    IL_004f:  nop
    IL_0050:  leave      IL_011b
    IL_0055:  ldarg.0
    IL_0056:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<C> C.<F>d__3.<>u__3""
    IL_005b:  stloc.2
    IL_005c:  ldarg.0
    IL_005d:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<C> C.<F>d__3.<>u__3""
    IL_0062:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<C>""
    IL_0068:  ldarg.0
    IL_0069:  ldc.i4.m1
    IL_006a:  dup
    IL_006b:  stloc.0
    IL_006c:  stfld      ""int C.<F>d__3.<>1__state""
    IL_0071:  ldloca.s   V_2
    IL_0073:  call       ""C System.Runtime.CompilerServices.TaskAwaiter<C>.GetResult()""
    IL_0078:  pop
    IL_0079:  ldloca.s   V_2
    IL_007b:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<C>""
    IL_0081:  call       ""System.Threading.Tasks.Task<int> C.A2()""
    IL_0086:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_008b:  stloc.s    V_4
    IL_008d:  ldloca.s   V_4
    IL_008f:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_0094:  brtrue.s   IL_00d8
    IL_0096:  ldarg.0
    IL_0097:  ldc.i4.1
    IL_0098:  dup
    IL_0099:  stloc.0
    IL_009a:  stfld      ""int C.<F>d__3.<>1__state""
    IL_009f:  ldarg.0
    IL_00a0:  ldloc.s    V_4
    IL_00a2:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> C.<F>d__3.<>u__2""
    IL_00a7:  ldarg.0
    IL_00a8:  stloc.3
    IL_00a9:  ldarg.0
    IL_00aa:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__3.<>t__builder""
    IL_00af:  ldloca.s   V_4
    IL_00b1:  ldloca.s   V_3
    IL_00b3:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, C.<F>d__3>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref C.<F>d__3)""
    IL_00b8:  nop
    IL_00b9:  leave.s    IL_011b
    IL_00bb:  ldarg.0
    IL_00bc:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> C.<F>d__3.<>u__2""
    IL_00c1:  stloc.s    V_4
    IL_00c3:  ldarg.0
    IL_00c4:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> C.<F>d__3.<>u__2""
    IL_00c9:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_00cf:  ldarg.0
    IL_00d0:  ldc.i4.m1
    IL_00d1:  dup
    IL_00d2:  stloc.0
    IL_00d3:  stfld      ""int C.<F>d__3.<>1__state""
    IL_00d8:  ldloca.s   V_4
    IL_00da:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_00df:  pop
    IL_00e0:  ldloca.s   V_4
    IL_00e2:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_00e8:  ldc.i4.1
    IL_00e9:  stloc.1
    IL_00ea:  leave.s    IL_0106
  }
  catch System.Exception
  {
    IL_00ec:  stloc.s    V_5
    IL_00ee:  ldarg.0
    IL_00ef:  ldc.i4.s   -2
    IL_00f1:  stfld      ""int C.<F>d__3.<>1__state""
    IL_00f6:  ldarg.0
    IL_00f7:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__3.<>t__builder""
    IL_00fc:  ldloc.s    V_5
    IL_00fe:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)""
    IL_0103:  nop
    IL_0104:  leave.s    IL_011b
  }
  IL_0106:  ldarg.0
  IL_0107:  ldc.i4.s   -2
  IL_0109:  stfld      ""int C.<F>d__3.<>1__state""
  IL_010e:  ldarg.0
  IL_010f:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__3.<>t__builder""
  IL_0114:  ldloc.1
  IL_0115:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetResult(int)""
  IL_011a:  nop
  IL_011b:  ret
}
");
            // 1 field def added & 2 methods updated
            CheckEncLogDefinitions(md2.Reader,
                Row(14, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(15, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                Row(12, TableIndex.Field, EditAndContinueOperation.Default),
                Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(9, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(15, TableIndex.CustomAttribute, EditAndContinueOperation.Default));

            // Note that the new awaiters are allocated slots <>u__4, <>u__5.
            diff2.VerifyIL("C.<F>d__3.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      284 (0x11c)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                System.Runtime.CompilerServices.TaskAwaiter<bool> V_2,
                C.<F>d__3 V_3,
                System.Runtime.CompilerServices.TaskAwaiter<C> V_4,
                System.Exception V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<F>d__3.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0012
    IL_000a:  br.s       IL_000c
    IL_000c:  ldloc.0
    IL_000d:  ldc.i4.1
    IL_000e:  beq.s      IL_0014
    IL_0010:  br.s       IL_0019
    IL_0012:  br.s       IL_0055
    IL_0014:  br         IL_00bb
    IL_0019:  nop
    IL_001a:  call       ""System.Threading.Tasks.Task<bool> C.A1()""
    IL_001f:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<bool> System.Threading.Tasks.Task<bool>.GetAwaiter()""
    IL_0024:  stloc.2
    IL_0025:  ldloca.s   V_2
    IL_0027:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<bool>.IsCompleted.get""
    IL_002c:  brtrue.s   IL_0071
    IL_002e:  ldarg.0
    IL_002f:  ldc.i4.0
    IL_0030:  dup
    IL_0031:  stloc.0
    IL_0032:  stfld      ""int C.<F>d__3.<>1__state""
    IL_0037:  ldarg.0
    IL_0038:  ldloc.2
    IL_0039:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<bool> C.<F>d__3.<>u__4""
    IL_003e:  ldarg.0
    IL_003f:  stloc.3
    IL_0040:  ldarg.0
    IL_0041:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__3.<>t__builder""
    IL_0046:  ldloca.s   V_2
    IL_0048:  ldloca.s   V_3
    IL_004a:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<bool>, C.<F>d__3>(ref System.Runtime.CompilerServices.TaskAwaiter<bool>, ref C.<F>d__3)""
    IL_004f:  nop
    IL_0050:  leave      IL_011b
    IL_0055:  ldarg.0
    IL_0056:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<bool> C.<F>d__3.<>u__4""
    IL_005b:  stloc.2
    IL_005c:  ldarg.0
    IL_005d:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<bool> C.<F>d__3.<>u__4""
    IL_0062:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<bool>""
    IL_0068:  ldarg.0
    IL_0069:  ldc.i4.m1
    IL_006a:  dup
    IL_006b:  stloc.0
    IL_006c:  stfld      ""int C.<F>d__3.<>1__state""
    IL_0071:  ldloca.s   V_2
    IL_0073:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<bool>.GetResult()""
    IL_0078:  pop
    IL_0079:  ldloca.s   V_2
    IL_007b:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<bool>""
    IL_0081:  call       ""System.Threading.Tasks.Task<C> C.A3()""
    IL_0086:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<C> System.Threading.Tasks.Task<C>.GetAwaiter()""
    IL_008b:  stloc.s    V_4
    IL_008d:  ldloca.s   V_4
    IL_008f:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<C>.IsCompleted.get""
    IL_0094:  brtrue.s   IL_00d8
    IL_0096:  ldarg.0
    IL_0097:  ldc.i4.1
    IL_0098:  dup
    IL_0099:  stloc.0
    IL_009a:  stfld      ""int C.<F>d__3.<>1__state""
    IL_009f:  ldarg.0
    IL_00a0:  ldloc.s    V_4
    IL_00a2:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<C> C.<F>d__3.<>u__3""
    IL_00a7:  ldarg.0
    IL_00a8:  stloc.3
    IL_00a9:  ldarg.0
    IL_00aa:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__3.<>t__builder""
    IL_00af:  ldloca.s   V_4
    IL_00b1:  ldloca.s   V_3
    IL_00b3:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<C>, C.<F>d__3>(ref System.Runtime.CompilerServices.TaskAwaiter<C>, ref C.<F>d__3)""
    IL_00b8:  nop
    IL_00b9:  leave.s    IL_011b
    IL_00bb:  ldarg.0
    IL_00bc:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<C> C.<F>d__3.<>u__3""
    IL_00c1:  stloc.s    V_4
    IL_00c3:  ldarg.0
    IL_00c4:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<C> C.<F>d__3.<>u__3""
    IL_00c9:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<C>""
    IL_00cf:  ldarg.0
    IL_00d0:  ldc.i4.m1
    IL_00d1:  dup
    IL_00d2:  stloc.0
    IL_00d3:  stfld      ""int C.<F>d__3.<>1__state""
    IL_00d8:  ldloca.s   V_4
    IL_00da:  call       ""C System.Runtime.CompilerServices.TaskAwaiter<C>.GetResult()""
    IL_00df:  pop
    IL_00e0:  ldloca.s   V_4
    IL_00e2:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<C>""
    IL_00e8:  ldc.i4.1
    IL_00e9:  stloc.1
    IL_00ea:  leave.s    IL_0106
  }
  catch System.Exception
  {
    IL_00ec:  stloc.s    V_5
    IL_00ee:  ldarg.0
    IL_00ef:  ldc.i4.s   -2
    IL_00f1:  stfld      ""int C.<F>d__3.<>1__state""
    IL_00f6:  ldarg.0
    IL_00f7:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__3.<>t__builder""
    IL_00fc:  ldloc.s    V_5
    IL_00fe:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)""
    IL_0103:  nop
    IL_0104:  leave.s    IL_011b
  }
  IL_0106:  ldarg.0
  IL_0107:  ldc.i4.s   -2
  IL_0109:  stfld      ""int C.<F>d__3.<>1__state""
  IL_010e:  ldarg.0
  IL_010f:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__3.<>t__builder""
  IL_0114:  ldloc.1
  IL_0115:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetResult(int)""
  IL_011a:  nop
  IL_011b:  ret
}
");
            // 2 field defs added - G and H awaiters & 4 methods updated: G, H kickoff and MoveNext
            CheckEncLogDefinitions(md3.Reader,
                Row(16, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(17, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(18, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(19, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                Row(13, TableIndex.Field, EditAndContinueOperation.Default),
                Row(5, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                Row(14, TableIndex.Field, EditAndContinueOperation.Default),
                Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(6, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(12, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(15, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(16, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(17, TableIndex.CustomAttribute, EditAndContinueOperation.Default));
        }

        [Fact]
        public void SynthesizedMembersMerging()
        {
            var source0 = @"
using System.Collections.Generic;

public class C
{    
}";
            var source1 = @"
using System.Collections.Generic;

public class C
{
    public static IEnumerable<int> F() 
    {
        yield return 1;
        yield return 2;
    }
}";
            var source2 = @"
using System.Collections.Generic;

public class C
{
    public static IEnumerable<int> F() 
    {
        yield return 1;
        yield return 3;
    }
}";
            var source3 = @"
using System.Collections.Generic;

public class C
{
    public static IEnumerable<int> F() 
    {
        yield return 1;
        yield return 3;
    }

    public static void G() 
    {
        System.Console.WriteLine(1);    
    }
}";
            var source4 = @"
using System.Collections.Generic;

public class C
{
    public static IEnumerable<int> F() 
    {
        yield return 1;
        yield return 3;
    }

    public static void G() 
    {
        System.Console.WriteLine(1);    
    }

    public static IEnumerable<int> H() 
    {
        yield return 1;
    }
}";

            // Rude edit but the compiler should handle it.

            var compilation0 = CreateCompilationWithMscorlib45(source0, options: ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All), assemblyName: "A");
            var compilation1 = compilation0.WithSource(source1);
            var compilation2 = compilation1.WithSource(source2);
            var compilation3 = compilation2.WithSource(source3);
            var compilation4 = compilation3.WithSource(source4);

            var f1 = compilation1.GetMember<MethodSymbol>("C.F");
            var f2 = compilation2.GetMember<MethodSymbol>("C.F");
            var f3 = compilation3.GetMember<MethodSymbol>("C.F");

            var g3 = compilation3.GetMember<MethodSymbol>("C.G");
            var h4 = compilation4.GetMember<MethodSymbol>("C.H");

            var v0 = CompileAndVerify(compilation0);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Insert, null, f1)));

            diff1.VerifySynthesizedMembers(
                "C: {<F>d__0#1}",
                "C.<F>d__0#1: {<>1__state, <>2__current, <>l__initialThreadId, System.IDisposable.Dispose, MoveNext, System.Collections.Generic.IEnumerator<System.Int32>.get_Current, System.Collections.IEnumerator.Reset, System.Collections.IEnumerator.get_Current, System.Collections.Generic.IEnumerable<System.Int32>.GetEnumerator, System.Collections.IEnumerable.GetEnumerator, System.Collections.Generic.IEnumerator<System.Int32>.Current, System.Collections.IEnumerator.Current}");

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, f1, f2, GetSyntaxMapByKind(f1, SyntaxKind.Block), preserveLocalVariables: true)));

            diff2.VerifySynthesizedMembers(
                "C: {<F>d__0#1}",
                "C.<F>d__0#1: {<>1__state, <>2__current, <>l__initialThreadId, System.IDisposable.Dispose, MoveNext, System.Collections.Generic.IEnumerator<System.Int32>.get_Current, System.Collections.IEnumerator.Reset, System.Collections.IEnumerator.get_Current, System.Collections.Generic.IEnumerable<System.Int32>.GetEnumerator, System.Collections.IEnumerable.GetEnumerator, System.Collections.Generic.IEnumerator<System.Int32>.Current, System.Collections.IEnumerator.Current}");

            var diff3 = compilation3.EmitDifference(
                diff2.NextGeneration,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Insert, null, g3)));

            diff3.VerifySynthesizedMembers(
                "C: {<F>d__0#1}",
                "C.<F>d__0#1: {<>1__state, <>2__current, <>l__initialThreadId, System.IDisposable.Dispose, MoveNext, System.Collections.Generic.IEnumerator<System.Int32>.get_Current, System.Collections.IEnumerator.Reset, System.Collections.IEnumerator.get_Current, System.Collections.Generic.IEnumerable<System.Int32>.GetEnumerator, System.Collections.IEnumerable.GetEnumerator, System.Collections.Generic.IEnumerator<System.Int32>.Current, System.Collections.IEnumerator.Current}");

            var diff4 = compilation4.EmitDifference(
                diff3.NextGeneration,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Insert, null, h4)));

            diff4.VerifySynthesizedMembers(
                "C: {<H>d__2#4, <F>d__0#1}",
                "C.<F>d__0#1: {<>1__state, <>2__current, <>l__initialThreadId, System.IDisposable.Dispose, MoveNext, System.Collections.Generic.IEnumerator<System.Int32>.get_Current, System.Collections.IEnumerator.Reset, System.Collections.IEnumerator.get_Current, System.Collections.Generic.IEnumerable<System.Int32>.GetEnumerator, System.Collections.IEnumerable.GetEnumerator, System.Collections.Generic.IEnumerator<System.Int32>.Current, System.Collections.IEnumerator.Current}",
                "C.<H>d__2#4: {<>1__state, <>2__current, <>l__initialThreadId, System.IDisposable.Dispose, MoveNext, System.Collections.Generic.IEnumerator<System.Int32>.get_Current, System.Collections.IEnumerator.Reset, System.Collections.IEnumerator.get_Current, System.Collections.Generic.IEnumerable<System.Int32>.GetEnumerator, System.Collections.IEnumerable.GetEnumerator, System.Collections.Generic.IEnumerator<System.Int32>.Current, System.Collections.IEnumerator.Current}");
        }

        [Fact]
        public void UniqueSynthesizedNames()
        {
            var source0 = @"
using System.Collections.Generic;

public class C
{    
    public static IEnumerable<int> F()  { yield return 1; }
}";
            var source1 = @"
using System.Collections.Generic;

public class C
{
    public static IEnumerable<int> F(int a)  { yield return 2; }
    public static IEnumerable<int> F()  { yield return 1; }
}";
            var source2 = @"
using System.Collections.Generic;

public class C
{
    public static IEnumerable<int> F(int a)  { yield return 2; }
    public static IEnumerable<int> F(byte a)  { yield return 3; }
    public static IEnumerable<int> F()  { yield return 1; }
}";

            var compilation0 = CreateCompilationWithMscorlib45(source0, options: ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All), assemblyName: "A");
            var compilation1 = compilation0.WithSource(source1);
            var compilation2 = compilation1.WithSource(source2);

            var f_int1 = compilation1.GetMembers("C.F").Single(m => m.ToString() == "C.F(int)");
            var f_byte2 = compilation2.GetMembers("C.F").Single(m => m.ToString() == "C.F(byte)");

            var v0 = CompileAndVerify(compilation0);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Insert, null, f_int1)));

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Insert, null, f_byte2)));

            var reader0 = md0.MetadataReader;
            var reader1 = diff1.GetMetadata().Reader;
            var reader2 = diff2.GetMetadata().Reader;

            CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "C", "<F>d__0");
            CheckNames(new[] { reader0, reader1 }, reader1.GetTypeDefNames(), "<F>d__0#1");
            CheckNames(new[] { reader0, reader1, reader2 }, reader2.GetTypeDefNames(), "<F>d__1#2");
        }
    }
}
