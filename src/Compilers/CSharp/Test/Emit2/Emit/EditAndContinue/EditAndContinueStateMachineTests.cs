// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Basic.Reference.Assemblies;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue.UnitTests
{
    public class EditAndContinueStateMachineTests : EditAndContinueTestBase
    {
        [Fact]
        [WorkItem(1068894, "DevDiv"), WorkItem(1137300, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1137300")]
        public void AddIteratorMethod()
        {
            var source0 = WithWindowsLineBreaks(@"
using System.Collections.Generic;
class C
{
}
");
            var source1 = WithWindowsLineBreaks(@"
using System.Collections.Generic;
class C
{
    static IEnumerable<int> G()
    {
        yield return 1;
    }
}
");
            var parseOptions = TestOptions.Regular.WithNoRefSafetyRulesAttribute();
            var compilation0 = CreateCompilation(Parse(source0, "a.cs", parseOptions), options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(Parse(source1, "a.cs", parseOptions));

            var g1 = compilation1.GetMember<MethodSymbol>("C.G");

            var bytes0 = compilation0.EmitToArray();
            using var md0 = ModuleMetadata.CreateFromImage(bytes0);
            var generation0 = CreateInitialBaseline(compilation0, md0, EmptyLocalsProvider);
            var reader0 = md0.MetadataReader;

            // gen 1

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Insert, null, g1)));

            using var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;
            var readers = new[] { reader0, reader1 };

            CheckNames(readers, diff1.EmitResult.UpdatedMethods);
            CheckNames(readers, diff1.EmitResult.ChangedTypes, "C", "<G>d__0#1");

            CheckEncLogDefinitions(reader1,
                Row(1, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(2, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(3, TableIndex.TypeDef, EditAndContinueOperation.Default),
                Row(1, TableIndex.PropertyMap, EditAndContinueOperation.Default),
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
                Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                Row(6, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                Row(7, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                Row(8, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                Row(9, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                Row(10, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(1, TableIndex.PropertyMap, EditAndContinueOperation.AddProperty),
                Row(1, TableIndex.Property, EditAndContinueOperation.Default),
                Row(1, TableIndex.PropertyMap, EditAndContinueOperation.AddProperty),
                Row(2, TableIndex.Property, EditAndContinueOperation.Default),
                Row(3, TableIndex.MethodDef, EditAndContinueOperation.AddParameter),
                Row(1, TableIndex.Param, EditAndContinueOperation.Default),
                Row(4, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
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
                Row(1, TableIndex.NestedClass, EditAndContinueOperation.Default),
                Row(1, TableIndex.InterfaceImpl, EditAndContinueOperation.Default),
                Row(2, TableIndex.InterfaceImpl, EditAndContinueOperation.Default),
                Row(3, TableIndex.InterfaceImpl, EditAndContinueOperation.Default),
                Row(4, TableIndex.InterfaceImpl, EditAndContinueOperation.Default),
                Row(5, TableIndex.InterfaceImpl, EditAndContinueOperation.Default));

            CheckEncMapDefinitions(reader1,
                Handle(3, TableIndex.TypeDef),
                Handle(1, TableIndex.Field),
                Handle(2, TableIndex.Field),
                Handle(3, TableIndex.Field),
                Handle(2, TableIndex.MethodDef),
                Handle(3, TableIndex.MethodDef),
                Handle(4, TableIndex.MethodDef),
                Handle(5, TableIndex.MethodDef),
                Handle(6, TableIndex.MethodDef),
                Handle(7, TableIndex.MethodDef),
                Handle(8, TableIndex.MethodDef),
                Handle(9, TableIndex.MethodDef),
                Handle(10, TableIndex.MethodDef),
                Handle(1, TableIndex.Param),
                Handle(1, TableIndex.InterfaceImpl),
                Handle(2, TableIndex.InterfaceImpl),
                Handle(3, TableIndex.InterfaceImpl),
                Handle(4, TableIndex.InterfaceImpl),
                Handle(5, TableIndex.InterfaceImpl),
                Handle(4, TableIndex.CustomAttribute),
                Handle(5, TableIndex.CustomAttribute),
                Handle(6, TableIndex.CustomAttribute),
                Handle(7, TableIndex.CustomAttribute),
                Handle(8, TableIndex.CustomAttribute),
                Handle(9, TableIndex.CustomAttribute),
                Handle(10, TableIndex.CustomAttribute),
                Handle(11, TableIndex.CustomAttribute),
                Handle(12, TableIndex.CustomAttribute),
                Handle(1, TableIndex.StandAloneSig),
                Handle(2, TableIndex.StandAloneSig),
                Handle(1, TableIndex.PropertyMap),
                Handle(1, TableIndex.Property),
                Handle(2, TableIndex.Property),
                Handle(1, TableIndex.MethodSemantics),
                Handle(2, TableIndex.MethodSemantics),
                Handle(1, TableIndex.MethodImpl),
                Handle(2, TableIndex.MethodImpl),
                Handle(3, TableIndex.MethodImpl),
                Handle(4, TableIndex.MethodImpl),
                Handle(5, TableIndex.MethodImpl),
                Handle(6, TableIndex.MethodImpl),
                Handle(7, TableIndex.MethodImpl),
                Handle(1, TableIndex.NestedClass));

            diff1.VerifyPdb(Enumerable.Range(0x06000001, 0x20), @"
<symbols>
  <files>
    <file id=""1"" name=""a.cs"" language=""C#"" checksumAlgorithm=""SHA1"" checksum=""66-9A-93-25-E7-42-DC-A9-DD-D1-61-3F-D9-45-A8-E1-39-8C-37-79"" />
  </files>
  <methods>
    <method token=""0x6000002"">
      <customDebugInfo>
        <forwardIterator name=""&lt;G&gt;d__0#1"" />
      </customDebugInfo>
    </method>
    <method token=""0x6000005"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""1"" />
        <entry offset=""0x1f"" startLine=""6"" startColumn=""5"" endLine=""6"" endColumn=""6"" document=""1"" />
        <entry offset=""0x20"" startLine=""7"" startColumn=""9"" endLine=""7"" endColumn=""24"" document=""1"" />
        <entry offset=""0x30"" hidden=""true"" document=""1"" />
        <entry offset=""0x37"" startLine=""8"" startColumn=""5"" endLine=""8"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x39"">
        <namespace name=""System.Collections.Generic"" />
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [Theory]
        [MemberData(nameof(ExternalPdbFormats))]
        public void AddAsyncMethod(DebugInformationFormat format)
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
            var compilation0 = CreateCompilationWithMscorlib461(source0, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);

            var v0 = CompileAndVerify(compilation0, emitOptions: EmitOptions.Default.WithDebugInformationFormat(format));
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");

            using var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);
            var reader0 = md0.MetadataReader;
            var generation0 = CreateInitialBaseline(compilation0, md0, EmptyLocalsProvider);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Insert, null, f1)));

            using var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;
            var readers = new[] { reader0, reader1 };

            CheckNames(readers, reader1.GetTypeDefNames(), "<F>d__0#1");
            CheckNames(readers, reader1.GetMethodDefNames(), "F", ".ctor", "MoveNext", "SetStateMachine");
            CheckNames(readers, reader1.GetFieldDefNames(), "<>1__state", "<>t__builder", "<>u__1");

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
                Row(7, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(1, TableIndex.MethodImpl, EditAndContinueOperation.Default),
                Row(2, TableIndex.MethodImpl, EditAndContinueOperation.Default),
                Row(1, TableIndex.NestedClass, EditAndContinueOperation.Default),
                Row(1, TableIndex.InterfaceImpl, EditAndContinueOperation.Default));

            diff1.VerifyPdb(new[] { MetadataTokens.MethodDefinitionHandle(4) }, @"
    <symbols>
      <files>
        <file id=""1"" name="""" language=""C#"" />
      </files>
      <methods>
        <method token=""0x6000004"">
          <customDebugInfo>
            <using>
              <namespace usingCount=""1"" />
            </using>
          </customDebugInfo>
          <sequencePoints>
            <entry offset=""0x0"" hidden=""true"" document=""1"" />
            <entry offset=""0x7"" hidden=""true"" document=""1"" />
            <entry offset=""0xe"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" document=""1"" />
            <entry offset=""0xf"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""35"" document=""1"" />
            <entry offset=""0x1c"" hidden=""true"" document=""1"" />
            <entry offset=""0x6d"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""19"" document=""1"" />
            <entry offset=""0x72"" hidden=""true"" document=""1"" />
            <entry offset=""0x8c"" startLine=""10"" startColumn=""5"" endLine=""10"" endColumn=""6"" document=""1"" />
            <entry offset=""0x94"" hidden=""true"" document=""1"" />
          </sequencePoints>
          <scope startOffset=""0x0"" endOffset=""0xa2"">
            <namespace name=""System.Threading.Tasks"" />
          </scope>
          <asyncInfo>
            <kickoffMethod token=""0x6000002"" />
            <await yield=""0x2e"" resume=""0x49"" token=""0x6000004"" />
          </asyncInfo>
        </method>
      </methods>
    </symbols>");
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
            var compilation0 = CreateCompilationWithMscorlib461(source0, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);

            var v0 = CompileAndVerify(compilation0);

            using var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);
            var method0 = compilation0.GetMember<MethodSymbol>("C.F");
            var method1 = compilation1.GetMember<MethodSymbol>("C.F");

            var generation0 = CreateInitialBaseline(compilation0, md0, EmptyLocalsProvider);
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1)));

            diff1.VerifySynthesizedMembers(
                "C: {<F>d__0#1}",
                "C.<F>d__0#1: {<>1__state, <>2__current, <>l__initialThreadId, System.IDisposable.Dispose, MoveNext, System.Collections.Generic.IEnumerator<System.Int32>.get_Current, System.Collections.IEnumerator.Reset, System.Collections.IEnumerator.get_Current, System.Collections.Generic.IEnumerable<System.Int32>.GetEnumerator, System.Collections.IEnumerable.GetEnumerator, System.Collections.Generic.IEnumerator<System.Int32>.Current, System.Collections.IEnumerator.Current}");

            using var md1 = diff1.GetMetadata();
            CheckEncLogDefinitions(md1.Reader,
                Row(2, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(3, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
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
            var compilation0 = CreateCompilationWithMscorlib461(source0, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);

            var v0 = CompileAndVerify(compilation0);

            using var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);
            var method0 = compilation0.GetMember<MethodSymbol>("C.F");
            var method1 = compilation1.GetMember<MethodSymbol>("C.F");

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1)));

            diff1.VerifySynthesizedMembers(
                "C.<F>d__0#1: {<>1__state, <>t__builder, <>s__1, <>u__1, MoveNext, SetStateMachine}",
                "C: {<F>d__0#1}");

            using var md1 = diff1.GetMetadata();
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
                Row(7, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(1, TableIndex.MethodImpl, EditAndContinueOperation.Default),
                Row(2, TableIndex.MethodImpl, EditAndContinueOperation.Default),
                Row(1, TableIndex.NestedClass, EditAndContinueOperation.Default),
                Row(1, TableIndex.InterfaceImpl, EditAndContinueOperation.Default));
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
            var compilation0 = CreateCompilationWithMscorlib461(source0, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);

            var v0 = CompileAndVerify(compilation0);

            using (var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData))
            {
                var method0 = compilation0.GetMember<MethodSymbol>("C.F");
                var method1 = compilation1.GetMember<MethodSymbol>("C.F");

                var generation0 = CreateInitialBaseline(compilation0, md0, EmptyLocalsProvider);
                var diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1)));

                using (var md1 = diff1.GetMetadata())
                {
                    CheckAttributes(md1.Reader,
                        new CustomAttributeRow(Handle(0, TableIndex.MethodDef), Handle(0, TableIndex.MemberRef)));  // row id 0 == delete

                    CheckEncLogDefinitions(md1.Reader,
                        Row(3, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(1, TableIndex.CustomAttribute, EditAndContinueOperation.Default)); // Delete IteratorStateMachineAttribute
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
            var compilation0 = CreateCompilationWithMscorlib461(source0, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(source1);

            var v0 = CompileAndVerify(compilation0);

            using var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);
            var method0 = compilation0.GetMember<MethodSymbol>("C.F");
            var method1 = compilation1.GetMember<MethodSymbol>("C.F");

            var generation0 = CreateInitialBaseline(compilation0, md0, EmptyLocalsProvider);
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1)));

            using (var md1 = diff1.GetMetadata())
            {
                CheckAttributes(md1.Reader,
                    new CustomAttributeRow(Handle(0, TableIndex.MethodDef), Handle(0, TableIndex.MemberRef)),  // row id 0 == delete
                    new CustomAttributeRow(Handle(0, TableIndex.MethodDef), Handle(0, TableIndex.MemberRef))); // row id 0 == delete

                CheckEncLogDefinitions(md1.Reader,
                    Row(3, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                    Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                    Row(1, TableIndex.CustomAttribute, EditAndContinueOperation.Default),  // Delete AsyncStateMachineAttribute
                    Row(2, TableIndex.CustomAttribute, EditAndContinueOperation.Default));  // Delete DebuggerStepThroughAttribute
            }
        }

        [Fact]
        public void AsyncMethodOverloads()
        {
            using var _ = new EditAndContinueTest()
                .AddBaseline(
                    """
                    using System.Threading.Tasks;
                    
                    class C
                    {
                        static async Task<int> F(short a) 
                        {
                            return <N:0>await Task.FromResult(1)</N:0>;
                        }

                        static async Task<int> F(long a) 
                        {
                            return <N:1>await Task.FromResult(1)</N:1>;
                        }
                    
                        static async Task<int> F(int a) 
                        {
                            return <N:2>await Task.FromResult(1)</N:2>;
                        }
                    }
                    """)
                .AddGeneration(
                    """
                    using System.Threading.Tasks;
                
                    class C
                    {
                        static async Task<int> F(short a) 
                        {
                            return <N:0>await Task.FromResult(2)</N:0>;
                        }
                
                        static async Task<int> F(long a) 
                        {
                            return <N:1>await Task.FromResult(3)</N:1>;
                        }
                
                        static async Task<int> F(int a) 
                        {
                            return <N:2>await Task.FromResult(4)</N:2>;
                        }
                    }
                    """,
                    edits:
                    [
                        Edit(SemanticEditKind.Update, c => c.GetMembers<IMethodSymbol>("C.F").Single(m => m.ToTestDisplayString() == "System.Threading.Tasks.Task<System.Int32> C.F(System.Int16 a)"), preserveLocalVariables: true),
                        Edit(SemanticEditKind.Update, c => c.GetMembers<IMethodSymbol>("C.F").Single(m => m.ToTestDisplayString() == "System.Threading.Tasks.Task<System.Int32> C.F(System.Int32 a)"), preserveLocalVariables: true),
                        Edit(SemanticEditKind.Update, c => c.GetMembers<IMethodSymbol>("C.F").Single(m => m.ToTestDisplayString() == "System.Threading.Tasks.Task<System.Int32> C.F(System.Int64 a)"), preserveLocalVariables: true),
                    ],
                    validator: g =>
                    {
                        // notice no TypeDefs, FieldDefs
                        g.VerifyEncLogDefinitions(
                        [
                            Row(7, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                            Row(8, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                            Row(9, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                            Row(10, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                            Row(11, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                            Row(12, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                            Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(3, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(6, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(9, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(12, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(1, TableIndex.Param, EditAndContinueOperation.Default),
                            Row(2, TableIndex.Param, EditAndContinueOperation.Default),
                            Row(3, TableIndex.Param, EditAndContinueOperation.Default),
                            Row(1, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(2, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(6, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(7, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(8, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(9, TableIndex.CustomAttribute, EditAndContinueOperation.Default)
                        ]);
                    })
                .Verify();
        }

        [Fact]
        public void UpdateIterator_NoVariables()
        {
            using var _ = new EditAndContinueTest()
                .AddBaseline("""
                using System.Collections.Generic;
                
                class C
                {
                    static IEnumerable<int> F() 
                    {
                        <N:0>yield return 1;</N:0>
                    }
                }
                """,
                validator: g =>
                {
                    g.VerifyIL("C.<F>d__0.System.Collections.IEnumerator.MoveNext", """
                    {
                      // Code size       57 (0x39)
                      .maxstack  2
                      .locals init (int V_0)
                      IL_0000:  ldarg.0
                      IL_0001:  ldfld      "int C.<F>d__0.<>1__state"
                      IL_0006:  stloc.0
                      IL_0007:  ldloc.0
                      IL_0008:  brfalse.s  IL_0012
                      IL_000a:  br.s       IL_000c
                      IL_000c:  ldloc.0
                      IL_000d:  ldc.i4.1
                      IL_000e:  beq.s      IL_0014
                      IL_0010:  br.s       IL_0016
                      IL_0012:  br.s       IL_0018
                      IL_0014:  br.s       IL_0030
                      IL_0016:  ldc.i4.0
                      IL_0017:  ret
                      IL_0018:  ldarg.0
                      IL_0019:  ldc.i4.m1
                      IL_001a:  stfld      "int C.<F>d__0.<>1__state"
                      IL_001f:  nop
                      IL_0020:  ldarg.0
                      IL_0021:  ldc.i4.1
                      IL_0022:  stfld      "int C.<F>d__0.<>2__current"
                      IL_0027:  ldarg.0
                      IL_0028:  ldc.i4.1
                      IL_0029:  stfld      "int C.<F>d__0.<>1__state"
                      IL_002e:  ldc.i4.1
                      IL_002f:  ret
                      IL_0030:  ldarg.0
                      IL_0031:  ldc.i4.m1
                      IL_0032:  stfld      "int C.<F>d__0.<>1__state"
                      IL_0037:  ldc.i4.0
                      IL_0038:  ret
                    }
                    """);
                })
                .AddGeneration("""
                using System.Collections.Generic;
                
                class C
                {
                    static IEnumerable<int> F() 
                    {
                        <N:0>yield return 2;</N:0>
                    }
                }
                """,
                edits: new[] { Edit(SemanticEditKind.Update, c => c.GetMember("C.F"), preserveLocalVariables: true) },
                validator: g =>
                {
                    // only methods with sequence points should be listed in UpdatedMethods:
                    g.VerifyUpdatedMethodNames("MoveNext");
                    g.VerifyChangedTypeNames("C", "<F>d__0");

                    // Verify that no new TypeDefs, FieldDefs or MethodDefs were added,
                    // 3 methods were updated: 
                    // - the kick-off method (might be changed if the method previously wasn't an iterator)
                    // - Finally method
                    // - MoveNext method
                    g.VerifyEncLogDefinitions(
                    [
                        Row(3, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(1, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(7, TableIndex.CustomAttribute, EditAndContinueOperation.Default)
                    ]);

                    g.VerifyIL("C.<F>d__0.System.Collections.IEnumerator.MoveNext", """
                    {
                      // Code size       57 (0x39)
                      .maxstack  2
                      .locals init (int V_0)
                      IL_0000:  ldarg.0
                      IL_0001:  ldfld      "int C.<F>d__0.<>1__state"
                      IL_0006:  stloc.0
                      IL_0007:  ldloc.0
                      IL_0008:  brfalse.s  IL_0012
                      IL_000a:  br.s       IL_000c
                      IL_000c:  ldloc.0
                      IL_000d:  ldc.i4.1
                      IL_000e:  beq.s      IL_0014
                      IL_0010:  br.s       IL_0016
                      IL_0012:  br.s       IL_0018
                      IL_0014:  br.s       IL_0030
                      IL_0016:  ldc.i4.0
                      IL_0017:  ret
                      IL_0018:  ldarg.0
                      IL_0019:  ldc.i4.m1
                      IL_001a:  stfld      "int C.<F>d__0.<>1__state"
                      IL_001f:  nop
                      IL_0020:  ldarg.0
                      IL_0021:  ldc.i4.2
                      IL_0022:  stfld      "int C.<F>d__0.<>2__current"
                      IL_0027:  ldarg.0
                      IL_0028:  ldc.i4.1
                      IL_0029:  stfld      "int C.<F>d__0.<>1__state"
                      IL_002e:  ldc.i4.1
                      IL_002f:  ret
                      IL_0030:  ldarg.0
                      IL_0031:  ldc.i4.m1
                      IL_0032:  stfld      "int C.<F>d__0.<>1__state"
                      IL_0037:  ldc.i4.0
                      IL_0038:  ret
                    }
                    """);
                })
                .Verify();
        }

        [Fact]
        public void UpdateAsync_NoVariables()
        {
            using var _ = new EditAndContinueTest()
                .AddBaseline("""
                using System.Threading.Tasks;
                
                class C
                {
                    static async Task<int> F() 
                    {
                        <N:0>await Task.FromResult(1)</N:0>;
                        return 2;
                    }
                }
                """,
                g =>
                {
                    g.VerifyMethodBody("C.<F>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", """
                    {
                      // Code size      160 (0xa0)
                      .maxstack  3
                      .locals init (int V_0,
                                    int V_1,
                                    System.Runtime.CompilerServices.TaskAwaiter<int> V_2,
                                    C.<F>d__0 V_3,
                                    System.Exception V_4)
                      // sequence point: <hidden>
                      IL_0000:  ldarg.0
                      IL_0001:  ldfld      "int C.<F>d__0.<>1__state"
                      IL_0006:  stloc.0
                      .try
                      {
                        // sequence point: <hidden>
                        IL_0007:  ldloc.0
                        IL_0008:  brfalse.s  IL_000c
                        IL_000a:  br.s       IL_000e
                        IL_000c:  br.s       IL_0048
                        // sequence point: {
                        IL_000e:  nop
                        // sequence point: await Task.FromResult(1)      ;
                        IL_000f:  ldc.i4.1
                        IL_0010:  call       "System.Threading.Tasks.Task<int> System.Threading.Tasks.Task.FromResult<int>(int)"
                        IL_0015:  callvirt   "System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()"
                        IL_001a:  stloc.2
                        // sequence point: <hidden>
                        IL_001b:  ldloca.s   V_2
                        IL_001d:  call       "bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get"
                        IL_0022:  brtrue.s   IL_0064
                        IL_0024:  ldarg.0
                        IL_0025:  ldc.i4.0
                        IL_0026:  dup
                        IL_0027:  stloc.0
                        IL_0028:  stfld      "int C.<F>d__0.<>1__state"
                        // async: yield
                        IL_002d:  ldarg.0
                        IL_002e:  ldloc.2
                        IL_002f:  stfld      "System.Runtime.CompilerServices.TaskAwaiter<int> C.<F>d__0.<>u__1"
                        IL_0034:  ldarg.0
                        IL_0035:  stloc.3
                        IL_0036:  ldarg.0
                        IL_0037:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__0.<>t__builder"
                        IL_003c:  ldloca.s   V_2
                        IL_003e:  ldloca.s   V_3
                        IL_0040:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, C.<F>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref C.<F>d__0)"
                        IL_0045:  nop
                        IL_0046:  leave.s    IL_009f
                        // async: resume
                        IL_0048:  ldarg.0
                        IL_0049:  ldfld      "System.Runtime.CompilerServices.TaskAwaiter<int> C.<F>d__0.<>u__1"
                        IL_004e:  stloc.2
                        IL_004f:  ldarg.0
                        IL_0050:  ldflda     "System.Runtime.CompilerServices.TaskAwaiter<int> C.<F>d__0.<>u__1"
                        IL_0055:  initobj    "System.Runtime.CompilerServices.TaskAwaiter<int>"
                        IL_005b:  ldarg.0
                        IL_005c:  ldc.i4.m1
                        IL_005d:  dup
                        IL_005e:  stloc.0
                        IL_005f:  stfld      "int C.<F>d__0.<>1__state"
                        IL_0064:  ldloca.s   V_2
                        IL_0066:  call       "int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()"
                        IL_006b:  pop
                        // sequence point: return 2;
                        IL_006c:  ldc.i4.2
                        IL_006d:  stloc.1
                        IL_006e:  leave.s    IL_008a
                      }
                      catch System.Exception
                      {
                        // sequence point: <hidden>
                        IL_0070:  stloc.s    V_4
                        IL_0072:  ldarg.0
                        IL_0073:  ldc.i4.s   -2
                        IL_0075:  stfld      "int C.<F>d__0.<>1__state"
                        IL_007a:  ldarg.0
                        IL_007b:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__0.<>t__builder"
                        IL_0080:  ldloc.s    V_4
                        IL_0082:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)"
                        IL_0087:  nop
                        IL_0088:  leave.s    IL_009f
                      }
                      // sequence point: }
                      IL_008a:  ldarg.0
                      IL_008b:  ldc.i4.s   -2
                      IL_008d:  stfld      "int C.<F>d__0.<>1__state"
                      // sequence point: <hidden>
                      IL_0092:  ldarg.0
                      IL_0093:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__0.<>t__builder"
                      IL_0098:  ldloc.1
                      IL_0099:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetResult(int)"
                      IL_009e:  nop
                      IL_009f:  ret
                    }
                    """);

                    g.VerifyPdb("C+<F>d__0.MoveNext", """
                    <symbols>
                      <methods>
                        <method containingType="C+&lt;F&gt;d__0" name="MoveNext">
                          <customDebugInfo>
                            <using>
                              <namespace usingCount="1" />
                            </using>
                            <encLocalSlotMap>
                              <slot kind="27" offset="0" />
                              <slot kind="20" offset="0" />
                              <slot kind="33" offset="16" />
                              <slot kind="temp" />
                              <slot kind="temp" />
                            </encLocalSlotMap>
                          </customDebugInfo>
                          <scope startOffset="0x0" endOffset="0xa0">
                            <namespace name="System.Threading.Tasks" />
                          </scope>
                          <asyncInfo>
                            <kickoffMethod declaringType="C" methodName="F" />
                            <await yield="0x2d" resume="0x48" declaringType="C+&lt;F&gt;d__0" methodName="MoveNext" />
                          </asyncInfo>
                        </method>
                      </methods>
                    </symbols>
                    """, PdbValidationOptions.ExcludeSequencePoints | PdbValidationOptions.ExcludeDocuments);
                })
                .AddGeneration(
                """
                using System.Threading.Tasks;
                
                class C
                {
                    static async Task<int> F() 
                    {
                        <N:0>await Task.FromResult(10)</N:0>;
                        return 20;
                    }
                }
                """,
                edits: new[] { Edit(SemanticEditKind.Update, c => c.GetMember("C.F"), preserveLocalVariables: true) },
                validator: g =>
                {
                    // only methods with sequence points should be listed in UpdatedMethods:
                    g.VerifyUpdatedMethodNames("MoveNext");
                    g.VerifyChangedTypeNames("C", "<F>d__0");

                    // Verify that no new TypeDefs, FieldDefs or MethodDefs were added,
                    // 2 methods were updated: 
                    // - the kick-off method (might be changed if the method previously wasn't async)
                    // - MoveNext method
                    g.VerifyEncLogDefinitions(
                    [
                        Row(3, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(4, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(1, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(2, TableIndex.CustomAttribute, EditAndContinueOperation.Default)
                    ]);

                    g.VerifyIL("C.<F>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", """
                    {
                      // Code size      162 (0xa2)
                      .maxstack  3
                      .locals init (int V_0,
                                    int V_1,
                                    System.Runtime.CompilerServices.TaskAwaiter<int> V_2,
                                    C.<F>d__0 V_3,
                                    System.Exception V_4)
                      IL_0000:  ldarg.0
                      IL_0001:  ldfld      "int C.<F>d__0.<>1__state"
                      IL_0006:  stloc.0
                      .try
                      {
                        IL_0007:  ldloc.0
                        IL_0008:  brfalse.s  IL_000c
                        IL_000a:  br.s       IL_000e
                        IL_000c:  br.s       IL_0049
                        IL_000e:  nop
                        IL_000f:  ldc.i4.s   10
                        IL_0011:  call       "System.Threading.Tasks.Task<int> System.Threading.Tasks.Task.FromResult<int>(int)"
                        IL_0016:  callvirt   "System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()"
                        IL_001b:  stloc.2
                        IL_001c:  ldloca.s   V_2
                        IL_001e:  call       "bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get"
                        IL_0023:  brtrue.s   IL_0065
                        IL_0025:  ldarg.0
                        IL_0026:  ldc.i4.0
                        IL_0027:  dup
                        IL_0028:  stloc.0
                        IL_0029:  stfld      "int C.<F>d__0.<>1__state"
                        IL_002e:  ldarg.0
                        IL_002f:  ldloc.2
                        IL_0030:  stfld      "System.Runtime.CompilerServices.TaskAwaiter<int> C.<F>d__0.<>u__1"
                        IL_0035:  ldarg.0
                        IL_0036:  stloc.3
                        IL_0037:  ldarg.0
                        IL_0038:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__0.<>t__builder"
                        IL_003d:  ldloca.s   V_2
                        IL_003f:  ldloca.s   V_3
                        IL_0041:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, C.<F>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref C.<F>d__0)"
                        IL_0046:  nop
                        IL_0047:  leave.s    IL_00a1
                        IL_0049:  ldarg.0
                        IL_004a:  ldfld      "System.Runtime.CompilerServices.TaskAwaiter<int> C.<F>d__0.<>u__1"
                        IL_004f:  stloc.2
                        IL_0050:  ldarg.0
                        IL_0051:  ldflda     "System.Runtime.CompilerServices.TaskAwaiter<int> C.<F>d__0.<>u__1"
                        IL_0056:  initobj    "System.Runtime.CompilerServices.TaskAwaiter<int>"
                        IL_005c:  ldarg.0
                        IL_005d:  ldc.i4.m1
                        IL_005e:  dup
                        IL_005f:  stloc.0
                        IL_0060:  stfld      "int C.<F>d__0.<>1__state"
                        IL_0065:  ldloca.s   V_2
                        IL_0067:  call       "int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()"
                        IL_006c:  pop
                        IL_006d:  ldc.i4.s   20
                        IL_006f:  stloc.1
                        IL_0070:  leave.s    IL_008c
                      }
                      catch System.Exception
                      {
                        IL_0072:  stloc.s    V_4
                        IL_0074:  ldarg.0
                        IL_0075:  ldc.i4.s   -2
                        IL_0077:  stfld      "int C.<F>d__0.<>1__state"
                        IL_007c:  ldarg.0
                        IL_007d:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__0.<>t__builder"
                        IL_0082:  ldloc.s    V_4
                        IL_0084:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)"
                        IL_0089:  nop
                        IL_008a:  leave.s    IL_00a1
                      }
                      IL_008c:  ldarg.0
                      IL_008d:  ldc.i4.s   -2
                      IL_008f:  stfld      "int C.<F>d__0.<>1__state"
                      IL_0094:  ldarg.0
                      IL_0095:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__0.<>t__builder"
                      IL_009a:  ldloc.1
                      IL_009b:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetResult(int)"
                      IL_00a0:  nop
                      IL_00a1:  ret
                    }
                    """);
                })
                .Verify();
        }

        [Fact]
        public void UpdateAsync_Await_Add()
        {
            var source0 = MarkedSource(@"
using System.Threading.Tasks;

class C
{
    static Task M1() => null;
    static Task M2() => null;
    static Task M3() => null;
    static void End() {}

    static async Task F() 
    {
        <N:0>await M1()</N:0>;
        <N:1>await M2()</N:1>;
        End();
    }
}");
            var source1 = MarkedSource(@"
using System.Threading.Tasks;

class C
{
    static Task M1() => null;
    static Task M2() => null;
    static Task M3() => null;
    static void End() {}

    static async Task F() 
    {
        <N:0>await M1()</N:0>;
        <N:2>await M3()</N:2>;
        <N:1>await M2()</N:1>;
        End();
    }
}");
            var compilation0 = CreateCompilationWithMscorlib461(new[] { source0.Tree }, new[] { SystemCoreRef, CSharpRef }, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);

            var v0 = CompileAndVerify(compilation0);
            v0.VerifyDiagnostics();
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                 generation0,
                 ImmutableArray.Create(
                     SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

            v0.VerifyPdb("C.F", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""F"">
      <customDebugInfo>
        <forwardIterator name=""&lt;F&gt;d__4"" />
        <encStateMachineStateMap>
          <state number=""0"" offset=""16"" />
          <state number=""1"" offset=""48"" />
        </encStateMachineStateMap>
      </customDebugInfo>
    </method>
  </methods>
</symbols>
");
            v0.VerifyPdb("C+<F>d__4.MoveNext", @"
  <symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C+&lt;F&gt;d__4"" name=""MoveNext"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""M1"" />
        <encLocalSlotMap>
          <slot kind=""27"" offset=""0"" />
          <slot kind=""33"" offset=""16"" />
          <slot kind=""temp"" />
          <slot kind=""33"" offset=""48"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <asyncInfo>
        <kickoffMethod declaringType=""C"" methodName=""F"" />
        <await yield=""0x37"" resume=""0x55"" declaringType=""C+&lt;F&gt;d__4"" methodName=""MoveNext"" />
        <await yield=""0x96"" resume=""0xb1"" declaringType=""C+&lt;F&gt;d__4"" methodName=""MoveNext"" />
      </asyncInfo>
    </method>
  </methods>
</symbols>
", options: PdbValidationOptions.ExcludeSequencePoints);

            v0.VerifyIL("C.<F>d__4.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      268 (0x10c)
  .maxstack  3
  .locals init (int V_0,
                System.Runtime.CompilerServices.TaskAwaiter V_1,
                C.<F>d__4 V_2,
                System.Runtime.CompilerServices.TaskAwaiter V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<F>d__4.<>1__state""
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
    IL_0014:  br         IL_00b1

    IL_0019:  nop
    IL_001a:  call       ""System.Threading.Tasks.Task C.M1()""
    IL_001f:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter System.Threading.Tasks.Task.GetAwaiter()""
    IL_0024:  stloc.1
    IL_0025:  ldloca.s   V_1
    IL_0027:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter.IsCompleted.get""
    IL_002c:  brtrue.s   IL_0071

    IL_002e:  ldarg.0
    IL_002f:  ldc.i4.0
    IL_0030:  dup
    IL_0031:  stloc.0
    IL_0032:  stfld      ""int C.<F>d__4.<>1__state""
    IL_0037:  ldarg.0
    IL_0038:  ldloc.1
    IL_0039:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<F>d__4.<>u__1""
    IL_003e:  ldarg.0
    IL_003f:  stloc.2
    IL_0040:  ldarg.0
    IL_0041:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<F>d__4.<>t__builder""
    IL_0046:  ldloca.s   V_1
    IL_0048:  ldloca.s   V_2
    IL_004a:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter, C.<F>d__4>(ref System.Runtime.CompilerServices.TaskAwaiter, ref C.<F>d__4)""
    IL_004f:  nop
    IL_0050:  leave      IL_010b

    IL_0055:  ldarg.0
    IL_0056:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<F>d__4.<>u__1""
    IL_005b:  stloc.1
    IL_005c:  ldarg.0
    IL_005d:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter C.<F>d__4.<>u__1""
    IL_0062:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_0068:  ldarg.0
    IL_0069:  ldc.i4.m1
    IL_006a:  dup
    IL_006b:  stloc.0
    IL_006c:  stfld      ""int C.<F>d__4.<>1__state""

    IL_0071:  ldloca.s   V_1
    IL_0073:  call       ""void System.Runtime.CompilerServices.TaskAwaiter.GetResult()""
    IL_0078:  nop
    IL_0079:  call       ""System.Threading.Tasks.Task C.M2()""
    IL_007e:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter System.Threading.Tasks.Task.GetAwaiter()""
    IL_0083:  stloc.3
    IL_0084:  ldloca.s   V_3
    IL_0086:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter.IsCompleted.get""
    IL_008b:  brtrue.s   IL_00cd

    IL_008d:  ldarg.0
    IL_008e:  ldc.i4.1
    IL_008f:  dup
    IL_0090:  stloc.0
    IL_0091:  stfld      ""int C.<F>d__4.<>1__state""
    IL_0096:  ldarg.0
    IL_0097:  ldloc.3
    IL_0098:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<F>d__4.<>u__1""
    IL_009d:  ldarg.0
    IL_009e:  stloc.2
    IL_009f:  ldarg.0
    IL_00a0:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<F>d__4.<>t__builder""
    IL_00a5:  ldloca.s   V_3
    IL_00a7:  ldloca.s   V_2
    IL_00a9:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter, C.<F>d__4>(ref System.Runtime.CompilerServices.TaskAwaiter, ref C.<F>d__4)""
    IL_00ae:  nop
    IL_00af:  leave.s    IL_010b

    IL_00b1:  ldarg.0
    IL_00b2:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<F>d__4.<>u__1""
    IL_00b7:  stloc.3
    IL_00b8:  ldarg.0
    IL_00b9:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter C.<F>d__4.<>u__1""
    IL_00be:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_00c4:  ldarg.0
    IL_00c5:  ldc.i4.m1
    IL_00c6:  dup
    IL_00c7:  stloc.0
    IL_00c8:  stfld      ""int C.<F>d__4.<>1__state""

    IL_00cd:  ldloca.s   V_3
    IL_00cf:  call       ""void System.Runtime.CompilerServices.TaskAwaiter.GetResult()""
    IL_00d4:  nop
    IL_00d5:  call       ""void C.End()""
    IL_00da:  nop
    IL_00db:  leave.s    IL_00f7
  }
  catch System.Exception
  {
    IL_00dd:  stloc.s    V_4
    IL_00df:  ldarg.0
    IL_00e0:  ldc.i4.s   -2
    IL_00e2:  stfld      ""int C.<F>d__4.<>1__state""
    IL_00e7:  ldarg.0
    IL_00e8:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<F>d__4.<>t__builder""
    IL_00ed:  ldloc.s    V_4
    IL_00ef:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00f4:  nop
    IL_00f5:  leave.s    IL_010b
  }
  IL_00f7:  ldarg.0
  IL_00f8:  ldc.i4.s   -2
  IL_00fa:  stfld      ""int C.<F>d__4.<>1__state""
  IL_00ff:  ldarg.0
  IL_0100:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<F>d__4.<>t__builder""
  IL_0105:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_010a:  nop
  IL_010b:  ret
}
");

            diff1.VerifyIL("C.<F>d__4.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      380 (0x17c)
  .maxstack  3
  .locals init (int V_0,
                System.Runtime.CompilerServices.TaskAwaiter V_1,
                C.<F>d__4 V_2,
                System.Runtime.CompilerServices.TaskAwaiter V_3,
                System.Runtime.CompilerServices.TaskAwaiter V_4,
                System.Exception V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<F>d__4.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  switch    (
        IL_001b,
        IL_001d,
        IL_0022)
    IL_0019:  br.s       IL_0027
    IL_001b:  br.s       IL_0063
    IL_001d:  br         IL_0120
    IL_0022:  br         IL_00c2

    IL_0027:  nop
    IL_0028:  call       ""System.Threading.Tasks.Task C.M1()""
    IL_002d:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter System.Threading.Tasks.Task.GetAwaiter()""
    IL_0032:  stloc.1
    IL_0033:  ldloca.s   V_1
    IL_0035:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter.IsCompleted.get""
    IL_003a:  brtrue.s   IL_007f

    IL_003c:  ldarg.0
    IL_003d:  ldc.i4.0
    IL_003e:  dup
    IL_003f:  stloc.0
    IL_0040:  stfld      ""int C.<F>d__4.<>1__state""
    IL_0045:  ldarg.0
    IL_0046:  ldloc.1
    IL_0047:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<F>d__4.<>u__1""
    IL_004c:  ldarg.0
    IL_004d:  stloc.2
    IL_004e:  ldarg.0
    IL_004f:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<F>d__4.<>t__builder""
    IL_0054:  ldloca.s   V_1
    IL_0056:  ldloca.s   V_2
    IL_0058:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter, C.<F>d__4>(ref System.Runtime.CompilerServices.TaskAwaiter, ref C.<F>d__4)""
    IL_005d:  nop
    IL_005e:  leave      IL_017b

    IL_0063:  ldarg.0
    IL_0064:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<F>d__4.<>u__1""
    IL_0069:  stloc.1
    IL_006a:  ldarg.0
    IL_006b:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter C.<F>d__4.<>u__1""
    IL_0070:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_0076:  ldarg.0
    IL_0077:  ldc.i4.m1
    IL_0078:  dup
    IL_0079:  stloc.0
    IL_007a:  stfld      ""int C.<F>d__4.<>1__state""

    IL_007f:  ldloca.s   V_1
    IL_0081:  call       ""void System.Runtime.CompilerServices.TaskAwaiter.GetResult()""
    IL_0086:  nop
    IL_0087:  call       ""System.Threading.Tasks.Task C.M3()""
    IL_008c:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter System.Threading.Tasks.Task.GetAwaiter()""
    IL_0091:  stloc.3
    IL_0092:  ldloca.s   V_3
    IL_0094:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter.IsCompleted.get""
    IL_0099:  brtrue.s   IL_00de

    IL_009b:  ldarg.0
    IL_009c:  ldc.i4.2
    IL_009d:  dup
    IL_009e:  stloc.0
    IL_009f:  stfld      ""int C.<F>d__4.<>1__state""
    IL_00a4:  ldarg.0
    IL_00a5:  ldloc.3
    IL_00a6:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<F>d__4.<>u__1""
    IL_00ab:  ldarg.0
    IL_00ac:  stloc.2
    IL_00ad:  ldarg.0
    IL_00ae:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<F>d__4.<>t__builder""
    IL_00b3:  ldloca.s   V_3
    IL_00b5:  ldloca.s   V_2
    IL_00b7:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter, C.<F>d__4>(ref System.Runtime.CompilerServices.TaskAwaiter, ref C.<F>d__4)""
    IL_00bc:  nop
    IL_00bd:  leave      IL_017b

    IL_00c2:  ldarg.0
    IL_00c3:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<F>d__4.<>u__1""
    IL_00c8:  stloc.3
    IL_00c9:  ldarg.0
    IL_00ca:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter C.<F>d__4.<>u__1""
    IL_00cf:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_00d5:  ldarg.0
    IL_00d6:  ldc.i4.m1
    IL_00d7:  dup
    IL_00d8:  stloc.0
    IL_00d9:  stfld      ""int C.<F>d__4.<>1__state""

    IL_00de:  ldloca.s   V_3
    IL_00e0:  call       ""void System.Runtime.CompilerServices.TaskAwaiter.GetResult()""
    IL_00e5:  nop
    IL_00e6:  call       ""System.Threading.Tasks.Task C.M2()""
    IL_00eb:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter System.Threading.Tasks.Task.GetAwaiter()""
    IL_00f0:  stloc.s    V_4
    IL_00f2:  ldloca.s   V_4
    IL_00f4:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter.IsCompleted.get""
    IL_00f9:  brtrue.s   IL_013d

    IL_00fb:  ldarg.0
    IL_00fc:  ldc.i4.1
    IL_00fd:  dup
    IL_00fe:  stloc.0
    IL_00ff:  stfld      ""int C.<F>d__4.<>1__state""
    IL_0104:  ldarg.0
    IL_0105:  ldloc.s    V_4
    IL_0107:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<F>d__4.<>u__1""
    IL_010c:  ldarg.0
    IL_010d:  stloc.2
    IL_010e:  ldarg.0
    IL_010f:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<F>d__4.<>t__builder""
    IL_0114:  ldloca.s   V_4
    IL_0116:  ldloca.s   V_2
    IL_0118:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter, C.<F>d__4>(ref System.Runtime.CompilerServices.TaskAwaiter, ref C.<F>d__4)""
    IL_011d:  nop
    IL_011e:  leave.s    IL_017b

    IL_0120:  ldarg.0
    IL_0121:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<F>d__4.<>u__1""
    IL_0126:  stloc.s    V_4
    IL_0128:  ldarg.0
    IL_0129:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter C.<F>d__4.<>u__1""
    IL_012e:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_0134:  ldarg.0
    IL_0135:  ldc.i4.m1
    IL_0136:  dup
    IL_0137:  stloc.0
    IL_0138:  stfld      ""int C.<F>d__4.<>1__state""

    IL_013d:  ldloca.s   V_4
    IL_013f:  call       ""void System.Runtime.CompilerServices.TaskAwaiter.GetResult()""
    IL_0144:  nop
    IL_0145:  call       ""void C.End()""
    IL_014a:  nop
    IL_014b:  leave.s    IL_0167
  }
  catch System.Exception
  {
    IL_014d:  stloc.s    V_5
    IL_014f:  ldarg.0
    IL_0150:  ldc.i4.s   -2
    IL_0152:  stfld      ""int C.<F>d__4.<>1__state""
    IL_0157:  ldarg.0
    IL_0158:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<F>d__4.<>t__builder""
    IL_015d:  ldloc.s    V_5
    IL_015f:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_0164:  nop
    IL_0165:  leave.s    IL_017b
  }
  IL_0167:  ldarg.0
  IL_0168:  ldc.i4.s   -2
  IL_016a:  stfld      ""int C.<F>d__4.<>1__state""
  IL_016f:  ldarg.0
  IL_0170:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<F>d__4.<>t__builder""
  IL_0175:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_017a:  nop
  IL_017b:  ret
}
");
        }

        [Fact]
        public void UpdateAsync_Await_AddRemove_Lambda()
        {
            var source0 = MarkedSource(@"
using System;
using System.Threading.Tasks;

class C
{
    static Task M1() => null;
    static Task M2() => null;
    static Task M3() => null;
    static void End() {}
    static int F(Func<Task> t) => 1;

    int x = F(<N:4>async () =>
    {
        <N:0>await M1()</N:0>;
        <N:1>await M2()</N:1>;
        End();
    }</N:4>);

    int y = F(<N:5>async () =>
    {
        <N:2>await M1()</N:2>;
        <N:3>await M2()</N:3>;
        End();
    }</N:5>);
}");
            var source1 = MarkedSource(@"
using System;
using System.Threading.Tasks;

class C
{
    static Task M1() => null;
    static Task M2() => null;
    static Task M3() => null;
    static void End() {}
    static int F(Func<Task> t) => 1;

    int x = F(<N:4>async () =>
    {
        <N:0>await M1()</N:0>;
        await M3();
        <N:1>await M2()</N:1>;
        End();
    }</N:4>);

    int y = F(<N:5>async () =>
    {
        <N:3>await M2()</N:3>;
        End();
    }</N:5>);
}");
            var compilation0 = CreateCompilationWithMscorlib461(new[] { source0.Tree }, new[] { SystemCoreRef, CSharpRef }, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);

            var v0 = CompileAndVerify(compilation0);
            v0.VerifyDiagnostics();
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var ctor0 = compilation0.GetMember<NamedTypeSymbol>("C").InstanceConstructors.Single();
            var ctor1 = compilation1.GetMember<NamedTypeSymbol>("C").InstanceConstructors.Single();

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                 generation0,
                 ImmutableArray.Create(
                     SemanticEdit.Create(SemanticEditKind.Update, ctor0, ctor1, GetSyntaxMapFromMarkers(source0, source1))));

            v0.VerifyPdb("C..ctor", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name="".ctor"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""M1"" />
        <encLambdaMap>
          <methodOrdinal>7</methodOrdinal>
          <lambda offset=""-216"" />
          <lambda offset=""-95"" />
        </encLambdaMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""13"" startColumn=""5"" endLine=""18"" endColumn=""14"" document=""1"" />
        <entry offset=""0x2a"" startLine=""20"" startColumn=""5"" endLine=""25"" endColumn=""14"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>
");
            v0.VerifyPdb("C+<>c.<.ctor>b__7_0", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C+&lt;&gt;c"" name=""&lt;.ctor&gt;b__7_0"">
      <customDebugInfo>
        <forwardIterator name=""&lt;&lt;-ctor&gt;b__7_0&gt;d"" />
        <encStateMachineStateMap>
            <state number=""0"" offset=""-200"" />
            <state number=""1"" offset=""-168"" />
        </encStateMachineStateMap>
      </customDebugInfo>
    </method>
  </methods>
</symbols>");

            v0.VerifyPdb("C+<>c.<.ctor>b__7_1", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C+&lt;&gt;c"" name=""&lt;.ctor&gt;b__7_1"">
      <customDebugInfo>
        <forwardIterator name=""&lt;&lt;-ctor&gt;b__7_1&gt;d"" />
        <encStateMachineStateMap>
          <state number=""0"" offset=""-79"" />
          <state number=""1"" offset=""-47"" />
        </encStateMachineStateMap>
      </customDebugInfo>
    </method>
  </methods>
</symbols>");

            v0.VerifyPdb("C+<>c+<<-ctor>b__7_0>d.MoveNext", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C+&lt;&gt;c+&lt;&lt;-ctor&gt;b__7_0&gt;d"" name=""MoveNext"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""M1"" />
        <encLocalSlotMap>
          <slot kind=""27"" offset=""-216"" />
          <slot kind=""33"" offset=""-200"" />
          <slot kind=""temp"" />
          <slot kind=""33"" offset=""-168"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <asyncInfo>
        <kickoffMethod declaringType=""C+&lt;&gt;c"" methodName=""&lt;.ctor&gt;b__7_0"" />
        <await yield=""0x37"" resume=""0x55"" declaringType=""C+&lt;&gt;c+&lt;&lt;-ctor&gt;b__7_0&gt;d"" methodName=""MoveNext"" />
        <await yield=""0x96"" resume=""0xb1"" declaringType=""C+&lt;&gt;c+&lt;&lt;-ctor&gt;b__7_0&gt;d"" methodName=""MoveNext"" />
      </asyncInfo>
    </method>
  </methods>
</symbols>
", options: PdbValidationOptions.ExcludeSequencePoints);

            v0.VerifyPdb("C+<>c+<<-ctor>b__7_1>d.MoveNext", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C+&lt;&gt;c+&lt;&lt;-ctor&gt;b__7_1&gt;d"" name=""MoveNext"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""M1"" />
        <encLocalSlotMap>
          <slot kind=""27"" offset=""-95"" />
          <slot kind=""33"" offset=""-79"" />
          <slot kind=""temp"" />
          <slot kind=""33"" offset=""-47"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <asyncInfo>
        <kickoffMethod declaringType=""C+&lt;&gt;c"" methodName=""&lt;.ctor&gt;b__7_1"" />
        <await yield=""0x37"" resume=""0x55"" declaringType=""C+&lt;&gt;c+&lt;&lt;-ctor&gt;b__7_1&gt;d"" methodName=""MoveNext"" />
        <await yield=""0x96"" resume=""0xb1"" declaringType=""C+&lt;&gt;c+&lt;&lt;-ctor&gt;b__7_1&gt;d"" methodName=""MoveNext"" />
      </asyncInfo>
    </method>
  </methods>
</symbols>
", options: PdbValidationOptions.ExcludeSequencePoints);

            diff1.VerifyIL("C.<>c.<<-ctor>b__7_0>d.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      380 (0x17c)
  .maxstack  3
  .locals init (int V_0,
                System.Runtime.CompilerServices.TaskAwaiter V_1,
                C.<>c.<<-ctor>b__7_0>d V_2,
                System.Runtime.CompilerServices.TaskAwaiter V_3,
                System.Runtime.CompilerServices.TaskAwaiter V_4,
                System.Exception V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<>c.<<-ctor>b__7_0>d.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  switch    (
        IL_001b,
        IL_001d,
        IL_0022)
    IL_0019:  br.s       IL_0027
    IL_001b:  br.s       IL_0063
    IL_001d:  br         IL_0120
    IL_0022:  br         IL_00c2
    IL_0027:  nop
    IL_0028:  call       ""System.Threading.Tasks.Task C.M1()""
    IL_002d:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter System.Threading.Tasks.Task.GetAwaiter()""
    IL_0032:  stloc.1
    IL_0033:  ldloca.s   V_1
    IL_0035:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter.IsCompleted.get""
    IL_003a:  brtrue.s   IL_007f
    IL_003c:  ldarg.0
    IL_003d:  ldc.i4.0
    IL_003e:  dup
    IL_003f:  stloc.0
    IL_0040:  stfld      ""int C.<>c.<<-ctor>b__7_0>d.<>1__state""
    IL_0045:  ldarg.0
    IL_0046:  ldloc.1
    IL_0047:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<>c.<<-ctor>b__7_0>d.<>u__1""
    IL_004c:  ldarg.0
    IL_004d:  stloc.2
    IL_004e:  ldarg.0
    IL_004f:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<>c.<<-ctor>b__7_0>d.<>t__builder""
    IL_0054:  ldloca.s   V_1
    IL_0056:  ldloca.s   V_2
    IL_0058:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter, C.<>c.<<-ctor>b__7_0>d>(ref System.Runtime.CompilerServices.TaskAwaiter, ref C.<>c.<<-ctor>b__7_0>d)""
    IL_005d:  nop
    IL_005e:  leave      IL_017b
    IL_0063:  ldarg.0
    IL_0064:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<>c.<<-ctor>b__7_0>d.<>u__1""
    IL_0069:  stloc.1
    IL_006a:  ldarg.0
    IL_006b:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter C.<>c.<<-ctor>b__7_0>d.<>u__1""
    IL_0070:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_0076:  ldarg.0
    IL_0077:  ldc.i4.m1
    IL_0078:  dup
    IL_0079:  stloc.0
    IL_007a:  stfld      ""int C.<>c.<<-ctor>b__7_0>d.<>1__state""
    IL_007f:  ldloca.s   V_1
    IL_0081:  call       ""void System.Runtime.CompilerServices.TaskAwaiter.GetResult()""
    IL_0086:  nop
    IL_0087:  call       ""System.Threading.Tasks.Task C.M3()""
    IL_008c:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter System.Threading.Tasks.Task.GetAwaiter()""
    IL_0091:  stloc.3
    IL_0092:  ldloca.s   V_3
    IL_0094:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter.IsCompleted.get""
    IL_0099:  brtrue.s   IL_00de
    IL_009b:  ldarg.0
    IL_009c:  ldc.i4.2
    IL_009d:  dup
    IL_009e:  stloc.0
    IL_009f:  stfld      ""int C.<>c.<<-ctor>b__7_0>d.<>1__state""
    IL_00a4:  ldarg.0
    IL_00a5:  ldloc.3
    IL_00a6:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<>c.<<-ctor>b__7_0>d.<>u__1""
    IL_00ab:  ldarg.0
    IL_00ac:  stloc.2
    IL_00ad:  ldarg.0
    IL_00ae:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<>c.<<-ctor>b__7_0>d.<>t__builder""
    IL_00b3:  ldloca.s   V_3
    IL_00b5:  ldloca.s   V_2
    IL_00b7:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter, C.<>c.<<-ctor>b__7_0>d>(ref System.Runtime.CompilerServices.TaskAwaiter, ref C.<>c.<<-ctor>b__7_0>d)""
    IL_00bc:  nop
    IL_00bd:  leave      IL_017b
    IL_00c2:  ldarg.0
    IL_00c3:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<>c.<<-ctor>b__7_0>d.<>u__1""
    IL_00c8:  stloc.3
    IL_00c9:  ldarg.0
    IL_00ca:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter C.<>c.<<-ctor>b__7_0>d.<>u__1""
    IL_00cf:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_00d5:  ldarg.0
    IL_00d6:  ldc.i4.m1
    IL_00d7:  dup
    IL_00d8:  stloc.0
    IL_00d9:  stfld      ""int C.<>c.<<-ctor>b__7_0>d.<>1__state""
    IL_00de:  ldloca.s   V_3
    IL_00e0:  call       ""void System.Runtime.CompilerServices.TaskAwaiter.GetResult()""
    IL_00e5:  nop
    IL_00e6:  call       ""System.Threading.Tasks.Task C.M2()""
    IL_00eb:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter System.Threading.Tasks.Task.GetAwaiter()""
    IL_00f0:  stloc.s    V_4
    IL_00f2:  ldloca.s   V_4
    IL_00f4:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter.IsCompleted.get""
    IL_00f9:  brtrue.s   IL_013d
    IL_00fb:  ldarg.0
    IL_00fc:  ldc.i4.1
    IL_00fd:  dup
    IL_00fe:  stloc.0
    IL_00ff:  stfld      ""int C.<>c.<<-ctor>b__7_0>d.<>1__state""
    IL_0104:  ldarg.0
    IL_0105:  ldloc.s    V_4
    IL_0107:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<>c.<<-ctor>b__7_0>d.<>u__1""
    IL_010c:  ldarg.0
    IL_010d:  stloc.2
    IL_010e:  ldarg.0
    IL_010f:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<>c.<<-ctor>b__7_0>d.<>t__builder""
    IL_0114:  ldloca.s   V_4
    IL_0116:  ldloca.s   V_2
    IL_0118:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter, C.<>c.<<-ctor>b__7_0>d>(ref System.Runtime.CompilerServices.TaskAwaiter, ref C.<>c.<<-ctor>b__7_0>d)""
    IL_011d:  nop
    IL_011e:  leave.s    IL_017b
    IL_0120:  ldarg.0
    IL_0121:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<>c.<<-ctor>b__7_0>d.<>u__1""
    IL_0126:  stloc.s    V_4
    IL_0128:  ldarg.0
    IL_0129:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter C.<>c.<<-ctor>b__7_0>d.<>u__1""
    IL_012e:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_0134:  ldarg.0
    IL_0135:  ldc.i4.m1
    IL_0136:  dup
    IL_0137:  stloc.0
    IL_0138:  stfld      ""int C.<>c.<<-ctor>b__7_0>d.<>1__state""
    IL_013d:  ldloca.s   V_4
    IL_013f:  call       ""void System.Runtime.CompilerServices.TaskAwaiter.GetResult()""
    IL_0144:  nop
    IL_0145:  call       ""void C.End()""
    IL_014a:  nop
    IL_014b:  leave.s    IL_0167
  }
  catch System.Exception
  {
    IL_014d:  stloc.s    V_5
    IL_014f:  ldarg.0
    IL_0150:  ldc.i4.s   -2
    IL_0152:  stfld      ""int C.<>c.<<-ctor>b__7_0>d.<>1__state""
    IL_0157:  ldarg.0
    IL_0158:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<>c.<<-ctor>b__7_0>d.<>t__builder""
    IL_015d:  ldloc.s    V_5
    IL_015f:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_0164:  nop
    IL_0165:  leave.s    IL_017b
  }
  IL_0167:  ldarg.0
  IL_0168:  ldc.i4.s   -2
  IL_016a:  stfld      ""int C.<>c.<<-ctor>b__7_0>d.<>1__state""
  IL_016f:  ldarg.0
  IL_0170:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<>c.<<-ctor>b__7_0>d.<>t__builder""
  IL_0175:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_017a:  nop
  IL_017b:  ret
}");

            diff1.VerifyIL("C.<>c.<<-ctor>b__7_1>d.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
 {
      // Code size      178 (0xb2)
      .maxstack  3
      .locals init (int V_0,
                    System.Runtime.CompilerServices.TaskAwaiter V_1,
                    C.<>c.<<-ctor>b__7_1>d V_2,
                    System.Exception V_3)
      IL_0000:  ldarg.0
      IL_0001:  ldfld      ""int C.<>c.<<-ctor>b__7_1>d.<>1__state""
      IL_0006:  stloc.0
      .try
      {
        IL_0007:  ldloc.0
        IL_0008:  ldc.i4.1
        IL_0009:  beq.s      IL_000d
        IL_000b:  br.s       IL_000f
        IL_000d:  br.s       IL_0059
        IL_000f:  ldloc.0
        IL_0010:  ldc.i4.0
        IL_0011:  blt.s      IL_0020
        IL_0013:  ldstr      """ + CodeAnalysisResources.EncCannotResumeSuspendedAsyncMethod + @"""
        IL_0018:  ldc.i4.s   -4
        IL_001a:  newobj     ""System.Runtime.CompilerServices.HotReloadException..ctor(string, int)""
        IL_001f:  throw
        IL_0020:  nop
        IL_0021:  call       ""System.Threading.Tasks.Task C.M2()""
        IL_0026:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter System.Threading.Tasks.Task.GetAwaiter()""
        IL_002b:  stloc.1
        IL_002c:  ldloca.s   V_1
        IL_002e:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter.IsCompleted.get""
        IL_0033:  brtrue.s   IL_0075
        IL_0035:  ldarg.0
        IL_0036:  ldc.i4.1
        IL_0037:  dup
        IL_0038:  stloc.0
        IL_0039:  stfld      ""int C.<>c.<<-ctor>b__7_1>d.<>1__state""
        IL_003e:  ldarg.0
        IL_003f:  ldloc.1
        IL_0040:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<>c.<<-ctor>b__7_1>d.<>u__1""
        IL_0045:  ldarg.0
        IL_0046:  stloc.2
        IL_0047:  ldarg.0
        IL_0048:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<>c.<<-ctor>b__7_1>d.<>t__builder""
        IL_004d:  ldloca.s   V_1
        IL_004f:  ldloca.s   V_2
        IL_0051:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter, C.<>c.<<-ctor>b__7_1>d>(ref System.Runtime.CompilerServices.TaskAwaiter, ref C.<>c.<<-ctor>b__7_1>d)""
        IL_0056:  nop
        IL_0057:  leave.s    IL_00b1
        IL_0059:  ldarg.0
        IL_005a:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<>c.<<-ctor>b__7_1>d.<>u__1""
        IL_005f:  stloc.1
        IL_0060:  ldarg.0
        IL_0061:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter C.<>c.<<-ctor>b__7_1>d.<>u__1""
        IL_0066:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
        IL_006c:  ldarg.0
        IL_006d:  ldc.i4.m1
        IL_006e:  dup
        IL_006f:  stloc.0
        IL_0070:  stfld      ""int C.<>c.<<-ctor>b__7_1>d.<>1__state""
        IL_0075:  ldloca.s   V_1
        IL_0077:  call       ""void System.Runtime.CompilerServices.TaskAwaiter.GetResult()""
        IL_007c:  nop
        IL_007d:  call       ""void C.End()""
        IL_0082:  nop
        IL_0083:  leave.s    IL_009d
      }
      catch System.Exception
      {
        IL_0085:  stloc.3
        IL_0086:  ldarg.0
        IL_0087:  ldc.i4.s   -2
        IL_0089:  stfld      ""int C.<>c.<<-ctor>b__7_1>d.<>1__state""
        IL_008e:  ldarg.0
        IL_008f:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<>c.<<-ctor>b__7_1>d.<>t__builder""
        IL_0094:  ldloc.3
        IL_0095:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
        IL_009a:  nop
        IL_009b:  leave.s    IL_00b1
      }
      IL_009d:  ldarg.0
      IL_009e:  ldc.i4.s   -2
      IL_00a0:  stfld      ""int C.<>c.<<-ctor>b__7_1>d.<>1__state""
      IL_00a5:  ldarg.0
      IL_00a6:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<>c.<<-ctor>b__7_1>d.<>t__builder""
      IL_00ab:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
      IL_00b0:  nop
      IL_00b1:  ret
    }
");
        }

        [Fact]
        public void UpdateAsync_Await_Remove_RemoveAdd()
        {
            var source0 = MarkedSource(@"
using System.Threading.Tasks;

class C
{
    static Task M1() => null;
    static Task M2() => null;
    static Task M3() => null;
    static void End() {}

    static async Task F() 
    {
        <N:0>await M1()</N:0>;
        <N:1>await M3()</N:1>;
        <N:2>await M2()</N:2>;
        End();
    }
}");
            var source1 = MarkedSource(@"
using System.Threading.Tasks;

class C
{
    static Task M1() => null;
    static Task M2() => null;
    static Task M3() => null;
    static void End() {}

    static async Task F() 
    {
        <N:0>await M1()</N:0>;
        <N:2>await M2()</N:2>;
        End();
    }
}");
            var source2 = MarkedSource(@"
using System.Threading.Tasks;

class C
{
    static Task M1() => null;
    static Task M2() => null;
    static Task M3() => null;
    static void End() {}

    static async Task F() 
    {
        await M3();
        <N:0>await M1()</N:0>;
        End();
    }
}");
            var compilation0 = CreateCompilationWithMscorlib461(new[] { source0.Tree }, new[] { SystemCoreRef, CSharpRef }, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);
            var compilation2 = compilation0.WithSource(source2.Tree);

            var v0 = CompileAndVerify(compilation0);
            v0.VerifyDiagnostics();
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");
            var f2 = compilation2.GetMember<MethodSymbol>("C.F");

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                 generation0,
                 ImmutableArray.Create(
                     SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

            var diff2 = compilation2.EmitDifference(
                 diff1.NextGeneration,
                 ImmutableArray.Create(
                     SemanticEdit.Create(SemanticEditKind.Update, f1, f2, GetSyntaxMapFromMarkers(source1, source2))));

            v0.VerifyPdb("C.F", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""F"">
      <customDebugInfo>
        <forwardIterator name=""&lt;F&gt;d__4"" />
        <encStateMachineStateMap>
          <state number=""0"" offset=""16"" />
          <state number=""1"" offset=""48"" />
          <state number=""2"" offset=""80"" />
        </encStateMachineStateMap>
      </customDebugInfo>
    </method>
  </methods>
</symbols>
");
            v0.VerifyPdb("C+<F>d__4.MoveNext", @"
  <symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C+&lt;F&gt;d__4"" name=""MoveNext"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""M1"" />
        <encLocalSlotMap>
          <slot kind=""27"" offset=""0"" />
          <slot kind=""33"" offset=""16"" />
          <slot kind=""temp"" />
          <slot kind=""33"" offset=""48"" />
          <slot kind=""33"" offset=""80"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <asyncInfo>
        <kickoffMethod declaringType=""C"" methodName=""F"" />
        <await yield=""0x45"" resume=""0x63"" declaringType=""C+&lt;F&gt;d__4"" methodName=""MoveNext"" />
        <await yield=""0xa4"" resume=""0xc2"" declaringType=""C+&lt;F&gt;d__4"" methodName=""MoveNext"" />
        <await yield=""0x104"" resume=""0x120"" declaringType=""C+&lt;F&gt;d__4"" methodName=""MoveNext"" />
      </asyncInfo>
    </method>
  </methods>
</symbols>
", options: PdbValidationOptions.ExcludeSequencePoints);

            v0.VerifyIL("C.<F>d__4.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      380 (0x17c)
  .maxstack  3
  .locals init (int V_0,
                System.Runtime.CompilerServices.TaskAwaiter V_1,
                C.<F>d__4 V_2,
                System.Runtime.CompilerServices.TaskAwaiter V_3,
                System.Runtime.CompilerServices.TaskAwaiter V_4,
                System.Exception V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<F>d__4.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  switch    (
        IL_001b,
        IL_001d,
        IL_0022)
    IL_0019:  br.s       IL_0027
    IL_001b:  br.s       IL_0063
    IL_001d:  br         IL_00c2
    IL_0022:  br         IL_0120

    IL_0027:  nop
    IL_0028:  call       ""System.Threading.Tasks.Task C.M1()""
    IL_002d:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter System.Threading.Tasks.Task.GetAwaiter()""
    IL_0032:  stloc.1
    IL_0033:  ldloca.s   V_1
    IL_0035:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter.IsCompleted.get""
    IL_003a:  brtrue.s   IL_007f

    IL_003c:  ldarg.0
    IL_003d:  ldc.i4.0
    IL_003e:  dup
    IL_003f:  stloc.0
    IL_0040:  stfld      ""int C.<F>d__4.<>1__state""
    IL_0045:  ldarg.0
    IL_0046:  ldloc.1
    IL_0047:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<F>d__4.<>u__1""
    IL_004c:  ldarg.0
    IL_004d:  stloc.2
    IL_004e:  ldarg.0
    IL_004f:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<F>d__4.<>t__builder""
    IL_0054:  ldloca.s   V_1
    IL_0056:  ldloca.s   V_2
    IL_0058:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter, C.<F>d__4>(ref System.Runtime.CompilerServices.TaskAwaiter, ref C.<F>d__4)""
    IL_005d:  nop
    IL_005e:  leave      IL_017b

    IL_0063:  ldarg.0
    IL_0064:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<F>d__4.<>u__1""
    IL_0069:  stloc.1
    IL_006a:  ldarg.0
    IL_006b:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter C.<F>d__4.<>u__1""
    IL_0070:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_0076:  ldarg.0
    IL_0077:  ldc.i4.m1
    IL_0078:  dup
    IL_0079:  stloc.0
    IL_007a:  stfld      ""int C.<F>d__4.<>1__state""

    IL_007f:  ldloca.s   V_1
    IL_0081:  call       ""void System.Runtime.CompilerServices.TaskAwaiter.GetResult()""
    IL_0086:  nop
    IL_0087:  call       ""System.Threading.Tasks.Task C.M3()""
    IL_008c:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter System.Threading.Tasks.Task.GetAwaiter()""
    IL_0091:  stloc.3
    IL_0092:  ldloca.s   V_3
    IL_0094:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter.IsCompleted.get""
    IL_0099:  brtrue.s   IL_00de

    IL_009b:  ldarg.0
    IL_009c:  ldc.i4.1
    IL_009d:  dup
    IL_009e:  stloc.0
    IL_009f:  stfld      ""int C.<F>d__4.<>1__state""
    IL_00a4:  ldarg.0
    IL_00a5:  ldloc.3
    IL_00a6:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<F>d__4.<>u__1""
    IL_00ab:  ldarg.0
    IL_00ac:  stloc.2
    IL_00ad:  ldarg.0
    IL_00ae:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<F>d__4.<>t__builder""
    IL_00b3:  ldloca.s   V_3
    IL_00b5:  ldloca.s   V_2
    IL_00b7:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter, C.<F>d__4>(ref System.Runtime.CompilerServices.TaskAwaiter, ref C.<F>d__4)""
    IL_00bc:  nop
    IL_00bd:  leave      IL_017b

    IL_00c2:  ldarg.0
    IL_00c3:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<F>d__4.<>u__1""
    IL_00c8:  stloc.3
    IL_00c9:  ldarg.0
    IL_00ca:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter C.<F>d__4.<>u__1""
    IL_00cf:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_00d5:  ldarg.0
    IL_00d6:  ldc.i4.m1
    IL_00d7:  dup
    IL_00d8:  stloc.0
    IL_00d9:  stfld      ""int C.<F>d__4.<>1__state""

    IL_00de:  ldloca.s   V_3
    IL_00e0:  call       ""void System.Runtime.CompilerServices.TaskAwaiter.GetResult()""
    IL_00e5:  nop
    IL_00e6:  call       ""System.Threading.Tasks.Task C.M2()""
    IL_00eb:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter System.Threading.Tasks.Task.GetAwaiter()""
    IL_00f0:  stloc.s    V_4
    IL_00f2:  ldloca.s   V_4
    IL_00f4:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter.IsCompleted.get""
    IL_00f9:  brtrue.s   IL_013d

    IL_00fb:  ldarg.0
    IL_00fc:  ldc.i4.2
    IL_00fd:  dup
    IL_00fe:  stloc.0
    IL_00ff:  stfld      ""int C.<F>d__4.<>1__state""
    IL_0104:  ldarg.0
    IL_0105:  ldloc.s    V_4
    IL_0107:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<F>d__4.<>u__1""
    IL_010c:  ldarg.0
    IL_010d:  stloc.2
    IL_010e:  ldarg.0
    IL_010f:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<F>d__4.<>t__builder""
    IL_0114:  ldloca.s   V_4
    IL_0116:  ldloca.s   V_2
    IL_0118:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter, C.<F>d__4>(ref System.Runtime.CompilerServices.TaskAwaiter, ref C.<F>d__4)""
    IL_011d:  nop
    IL_011e:  leave.s    IL_017b

    IL_0120:  ldarg.0
    IL_0121:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<F>d__4.<>u__1""
    IL_0126:  stloc.s    V_4
    IL_0128:  ldarg.0
    IL_0129:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter C.<F>d__4.<>u__1""
    IL_012e:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_0134:  ldarg.0
    IL_0135:  ldc.i4.m1
    IL_0136:  dup
    IL_0137:  stloc.0
    IL_0138:  stfld      ""int C.<F>d__4.<>1__state""

    IL_013d:  ldloca.s   V_4
    IL_013f:  call       ""void System.Runtime.CompilerServices.TaskAwaiter.GetResult()""
    IL_0144:  nop
    IL_0145:  call       ""void C.End()""
    IL_014a:  nop
    IL_014b:  leave.s    IL_0167
  }
  catch System.Exception
  {
    IL_014d:  stloc.s    V_5
    IL_014f:  ldarg.0
    IL_0150:  ldc.i4.s   -2
    IL_0152:  stfld      ""int C.<F>d__4.<>1__state""
    IL_0157:  ldarg.0
    IL_0158:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<F>d__4.<>t__builder""
    IL_015d:  ldloc.s    V_5
    IL_015f:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_0164:  nop
    IL_0165:  leave.s    IL_017b
  }
  IL_0167:  ldarg.0
  IL_0168:  ldc.i4.s   -2
  IL_016a:  stfld      ""int C.<F>d__4.<>1__state""
  IL_016f:  ldarg.0
  IL_0170:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<F>d__4.<>t__builder""
  IL_0175:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_017a:  nop
  IL_017b:  ret
}
");

            diff1.VerifyIL("C.<F>d__4.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      285 (0x11d)
  .maxstack  3
  .locals init (int V_0,
                System.Runtime.CompilerServices.TaskAwaiter V_1,
                C.<F>d__4 V_2,
                System.Runtime.CompilerServices.TaskAwaiter V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<F>d__4.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0012
    IL_000a:  br.s       IL_000c
    IL_000c:  ldloc.0
    IL_000d:  ldc.i4.2
    IL_000e:  beq.s      IL_0014
    IL_0010:  br.s       IL_0019
    IL_0012:  br.s       IL_0066
    IL_0014:  br         IL_00c2
    IL_0019:  ldloc.0
    IL_001a:  ldc.i4.0
    IL_001b:  blt.s      IL_002a
    IL_001d:  ldstr      """ + CodeAnalysisResources.EncCannotResumeSuspendedAsyncMethod + @"""
    IL_0022:  ldc.i4.s   -4
    IL_0024:  newobj     ""System.Runtime.CompilerServices.HotReloadException..ctor(string, int)""
    IL_0029:  throw
    IL_002a:  nop
    IL_002b:  call       ""System.Threading.Tasks.Task C.M1()""
    IL_0030:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter System.Threading.Tasks.Task.GetAwaiter()""
    IL_0035:  stloc.1
    IL_0036:  ldloca.s   V_1
    IL_0038:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter.IsCompleted.get""
    IL_003d:  brtrue.s   IL_0082
    IL_003f:  ldarg.0
    IL_0040:  ldc.i4.0
    IL_0041:  dup
    IL_0042:  stloc.0
    IL_0043:  stfld      ""int C.<F>d__4.<>1__state""
    IL_0048:  ldarg.0
    IL_0049:  ldloc.1
    IL_004a:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<F>d__4.<>u__1""
    IL_004f:  ldarg.0
    IL_0050:  stloc.2
    IL_0051:  ldarg.0
    IL_0052:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<F>d__4.<>t__builder""
    IL_0057:  ldloca.s   V_1
    IL_0059:  ldloca.s   V_2
    IL_005b:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter, C.<F>d__4>(ref System.Runtime.CompilerServices.TaskAwaiter, ref C.<F>d__4)""
    IL_0060:  nop
    IL_0061:  leave      IL_011c
    IL_0066:  ldarg.0
    IL_0067:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<F>d__4.<>u__1""
    IL_006c:  stloc.1
    IL_006d:  ldarg.0
    IL_006e:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter C.<F>d__4.<>u__1""
    IL_0073:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_0079:  ldarg.0
    IL_007a:  ldc.i4.m1
    IL_007b:  dup
    IL_007c:  stloc.0
    IL_007d:  stfld      ""int C.<F>d__4.<>1__state""
    IL_0082:  ldloca.s   V_1
    IL_0084:  call       ""void System.Runtime.CompilerServices.TaskAwaiter.GetResult()""
    IL_0089:  nop
    IL_008a:  call       ""System.Threading.Tasks.Task C.M2()""
    IL_008f:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter System.Threading.Tasks.Task.GetAwaiter()""
    IL_0094:  stloc.3
    IL_0095:  ldloca.s   V_3
    IL_0097:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter.IsCompleted.get""
    IL_009c:  brtrue.s   IL_00de
    IL_009e:  ldarg.0
    IL_009f:  ldc.i4.2
    IL_00a0:  dup
    IL_00a1:  stloc.0
    IL_00a2:  stfld      ""int C.<F>d__4.<>1__state""
    IL_00a7:  ldarg.0
    IL_00a8:  ldloc.3
    IL_00a9:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<F>d__4.<>u__1""
    IL_00ae:  ldarg.0
    IL_00af:  stloc.2
    IL_00b0:  ldarg.0
    IL_00b1:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<F>d__4.<>t__builder""
    IL_00b6:  ldloca.s   V_3
    IL_00b8:  ldloca.s   V_2
    IL_00ba:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter, C.<F>d__4>(ref System.Runtime.CompilerServices.TaskAwaiter, ref C.<F>d__4)""
    IL_00bf:  nop
    IL_00c0:  leave.s    IL_011c
    IL_00c2:  ldarg.0
    IL_00c3:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<F>d__4.<>u__1""
    IL_00c8:  stloc.3
    IL_00c9:  ldarg.0
    IL_00ca:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter C.<F>d__4.<>u__1""
    IL_00cf:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_00d5:  ldarg.0
    IL_00d6:  ldc.i4.m1
    IL_00d7:  dup
    IL_00d8:  stloc.0
    IL_00d9:  stfld      ""int C.<F>d__4.<>1__state""
    IL_00de:  ldloca.s   V_3
    IL_00e0:  call       ""void System.Runtime.CompilerServices.TaskAwaiter.GetResult()""
    IL_00e5:  nop
    IL_00e6:  call       ""void C.End()""
    IL_00eb:  nop
    IL_00ec:  leave.s    IL_0108
  }
  catch System.Exception
  {
    IL_00ee:  stloc.s    V_4
    IL_00f0:  ldarg.0
    IL_00f1:  ldc.i4.s   -2
    IL_00f3:  stfld      ""int C.<F>d__4.<>1__state""
    IL_00f8:  ldarg.0
    IL_00f9:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<F>d__4.<>t__builder""
    IL_00fe:  ldloc.s    V_4
    IL_0100:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_0105:  nop
    IL_0106:  leave.s    IL_011c
  }
  IL_0108:  ldarg.0
  IL_0109:  ldc.i4.s   -2
  IL_010b:  stfld      ""int C.<F>d__4.<>1__state""
  IL_0110:  ldarg.0
  IL_0111:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<F>d__4.<>t__builder""
  IL_0116:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_011b:  nop
  IL_011c:  ret
}
");
            // note that CDI is not emitted to the delta since we already have the information captured in changed symbols:
            diff1.VerifyPdb(Enumerable.Range(1, 20).Select(MetadataTokens.MethodDefinitionHandle), @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method token=""0x6000008"">
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""1"" />
        <entry offset=""0x7"" hidden=""true"" document=""1"" />
        <entry offset=""0x2a"" startLine=""12"" startColumn=""5"" endLine=""12"" endColumn=""6"" document=""1"" />
        <entry offset=""0x2b"" startLine=""13"" startColumn=""14"" endLine=""13"" endColumn=""31"" document=""1"" />
        <entry offset=""0x36"" hidden=""true"" document=""1"" />
        <entry offset=""0x8a"" startLine=""14"" startColumn=""14"" endLine=""14"" endColumn=""31"" document=""1"" />
        <entry offset=""0x95"" hidden=""true"" document=""1"" />
        <entry offset=""0xe6"" startLine=""15"" startColumn=""9"" endLine=""15"" endColumn=""15"" document=""1"" />
        <entry offset=""0xee"" hidden=""true"" document=""1"" />
        <entry offset=""0x108"" startLine=""16"" startColumn=""5"" endLine=""16"" endColumn=""6"" document=""1"" />
        <entry offset=""0x110"" hidden=""true"" document=""1"" />
      </sequencePoints>
      <asyncInfo>
        <kickoffMethod token=""0x6000005"" />
        <await yield=""0x48"" resume=""0x66"" token=""0x6000008"" />
        <await yield=""0xa7"" resume=""0xc2"" token=""0x6000008"" />
      </asyncInfo>
    </method>
  </methods>
</symbols>
");
            diff2.VerifyIL("C.<F>d__4.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", """
            {
              // Code size      285 (0x11d)
              .maxstack  3
              .locals init (int V_0,
                            System.Runtime.CompilerServices.TaskAwaiter V_1,
                            C.<F>d__4 V_2,
                            System.Runtime.CompilerServices.TaskAwaiter V_3,
                            System.Exception V_4)
              IL_0000:  ldarg.0
              IL_0001:  ldfld      "int C.<F>d__4.<>1__state"
              IL_0006:  stloc.0
              .try
              {
                IL_0007:  ldloc.0
                IL_0008:  brfalse.s  IL_0012
                IL_000a:  br.s       IL_000c
                IL_000c:  ldloc.0
                IL_000d:  ldc.i4.3
                IL_000e:  beq.s      IL_0017
                IL_0010:  br.s       IL_0019
                IL_0012:  br         IL_00c2
                IL_0017:  br.s       IL_0066
                IL_0019:  ldloc.0
                IL_001a:  ldc.i4.0
                IL_001b:  blt.s      IL_002a
                IL_001d:  ldstr      "Edit and Continue can't resume suspended asynchronous method since the corresponding await expression has been deleted"
                IL_0022:  ldc.i4.s   -4
                IL_0024:  newobj     "System.Runtime.CompilerServices.HotReloadException..ctor(string, int)"
                IL_0029:  throw
                IL_002a:  nop
                IL_002b:  call       "System.Threading.Tasks.Task C.M3()"
                IL_0030:  callvirt   "System.Runtime.CompilerServices.TaskAwaiter System.Threading.Tasks.Task.GetAwaiter()"
                IL_0035:  stloc.1
                IL_0036:  ldloca.s   V_1
                IL_0038:  call       "bool System.Runtime.CompilerServices.TaskAwaiter.IsCompleted.get"
                IL_003d:  brtrue.s   IL_0082
                IL_003f:  ldarg.0
                IL_0040:  ldc.i4.3
                IL_0041:  dup
                IL_0042:  stloc.0
                IL_0043:  stfld      "int C.<F>d__4.<>1__state"
                IL_0048:  ldarg.0
                IL_0049:  ldloc.1
                IL_004a:  stfld      "System.Runtime.CompilerServices.TaskAwaiter C.<F>d__4.<>u__1"
                IL_004f:  ldarg.0
                IL_0050:  stloc.2
                IL_0051:  ldarg.0
                IL_0052:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<F>d__4.<>t__builder"
                IL_0057:  ldloca.s   V_1
                IL_0059:  ldloca.s   V_2
                IL_005b:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter, C.<F>d__4>(ref System.Runtime.CompilerServices.TaskAwaiter, ref C.<F>d__4)"
                IL_0060:  nop
                IL_0061:  leave      IL_011c
                IL_0066:  ldarg.0
                IL_0067:  ldfld      "System.Runtime.CompilerServices.TaskAwaiter C.<F>d__4.<>u__1"
                IL_006c:  stloc.1
                IL_006d:  ldarg.0
                IL_006e:  ldflda     "System.Runtime.CompilerServices.TaskAwaiter C.<F>d__4.<>u__1"
                IL_0073:  initobj    "System.Runtime.CompilerServices.TaskAwaiter"
                IL_0079:  ldarg.0
                IL_007a:  ldc.i4.m1
                IL_007b:  dup
                IL_007c:  stloc.0
                IL_007d:  stfld      "int C.<F>d__4.<>1__state"
                IL_0082:  ldloca.s   V_1
                IL_0084:  call       "void System.Runtime.CompilerServices.TaskAwaiter.GetResult()"
                IL_0089:  nop
                IL_008a:  call       "System.Threading.Tasks.Task C.M1()"
                IL_008f:  callvirt   "System.Runtime.CompilerServices.TaskAwaiter System.Threading.Tasks.Task.GetAwaiter()"
                IL_0094:  stloc.3
                IL_0095:  ldloca.s   V_3
                IL_0097:  call       "bool System.Runtime.CompilerServices.TaskAwaiter.IsCompleted.get"
                IL_009c:  brtrue.s   IL_00de
                IL_009e:  ldarg.0
                IL_009f:  ldc.i4.0
                IL_00a0:  dup
                IL_00a1:  stloc.0
                IL_00a2:  stfld      "int C.<F>d__4.<>1__state"
                IL_00a7:  ldarg.0
                IL_00a8:  ldloc.3
                IL_00a9:  stfld      "System.Runtime.CompilerServices.TaskAwaiter C.<F>d__4.<>u__1"
                IL_00ae:  ldarg.0
                IL_00af:  stloc.2
                IL_00b0:  ldarg.0
                IL_00b1:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<F>d__4.<>t__builder"
                IL_00b6:  ldloca.s   V_3
                IL_00b8:  ldloca.s   V_2
                IL_00ba:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter, C.<F>d__4>(ref System.Runtime.CompilerServices.TaskAwaiter, ref C.<F>d__4)"
                IL_00bf:  nop
                IL_00c0:  leave.s    IL_011c
                IL_00c2:  ldarg.0
                IL_00c3:  ldfld      "System.Runtime.CompilerServices.TaskAwaiter C.<F>d__4.<>u__1"
                IL_00c8:  stloc.3
                IL_00c9:  ldarg.0
                IL_00ca:  ldflda     "System.Runtime.CompilerServices.TaskAwaiter C.<F>d__4.<>u__1"
                IL_00cf:  initobj    "System.Runtime.CompilerServices.TaskAwaiter"
                IL_00d5:  ldarg.0
                IL_00d6:  ldc.i4.m1
                IL_00d7:  dup
                IL_00d8:  stloc.0
                IL_00d9:  stfld      "int C.<F>d__4.<>1__state"
                IL_00de:  ldloca.s   V_3
                IL_00e0:  call       "void System.Runtime.CompilerServices.TaskAwaiter.GetResult()"
                IL_00e5:  nop
                IL_00e6:  call       "void C.End()"
                IL_00eb:  nop
                IL_00ec:  leave.s    IL_0108
              }
              catch System.Exception
              {
                IL_00ee:  stloc.s    V_4
                IL_00f0:  ldarg.0
                IL_00f1:  ldc.i4.s   -2
                IL_00f3:  stfld      "int C.<F>d__4.<>1__state"
                IL_00f8:  ldarg.0
                IL_00f9:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<F>d__4.<>t__builder"
                IL_00fe:  ldloc.s    V_4
                IL_0100:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
                IL_0105:  nop
                IL_0106:  leave.s    IL_011c
              }
              IL_0108:  ldarg.0
              IL_0109:  ldc.i4.s   -2
              IL_010b:  stfld      "int C.<F>d__4.<>1__state"
              IL_0110:  ldarg.0
              IL_0111:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<F>d__4.<>t__builder"
              IL_0116:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
              IL_011b:  nop
              IL_011c:  ret
            }
            """);
        }

        [Fact]
        public void UpdateAsync_Await_Remove_FirstAndLast()
        {
            var source0 = MarkedSource(@"
using System.Threading.Tasks;

class C
{
    static Task M1() => null;
    static Task M2() => null;
    static Task M3() => null;
    static void End() {}

    static async Task F() 
    {
        <N:0>await M1()</N:0>;
        <N:1>await M2()</N:1>;
        <N:2>await M3()</N:2>;
        End();
    }
}");
            var source1 = MarkedSource(@"
using System.Threading.Tasks;

class C
{
    static Task M1() => null;
    static Task M2() => null;
    static Task M3() => null;
    static void End() {}

    static async Task F() 
    {
        <N:1>await M2()</N:1>;
        End();
    }
}");
            var compilation0 = CreateCompilationWithMscorlib461(new[] { source0.Tree }, new[] { SystemCoreRef, CSharpRef }, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);

            var v0 = CompileAndVerify(compilation0);
            v0.VerifyDiagnostics();
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                 generation0,
                 ImmutableArray.Create(
                     SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

            diff1.VerifyIL("C.<F>d__4.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      178 (0xb2)
  .maxstack  3
  .locals init (int V_0,
                System.Runtime.CompilerServices.TaskAwaiter V_1,
                C.<F>d__4 V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<F>d__4.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  ldc.i4.1
    IL_0009:  beq.s      IL_000d
    IL_000b:  br.s       IL_000f
    IL_000d:  br.s       IL_0059
    IL_000f:  ldloc.0
    IL_0010:  ldc.i4.0
    IL_0011:  blt.s      IL_0020
    IL_0013:  ldstr      """ + CodeAnalysisResources.EncCannotResumeSuspendedAsyncMethod + @"""
    IL_0018:  ldc.i4.s   -4
    IL_001a:  newobj     ""System.Runtime.CompilerServices.HotReloadException..ctor(string, int)""
    IL_001f:  throw
    IL_0020:  nop
    IL_0021:  call       ""System.Threading.Tasks.Task C.M2()""
    IL_0026:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter System.Threading.Tasks.Task.GetAwaiter()""
    IL_002b:  stloc.1
    IL_002c:  ldloca.s   V_1
    IL_002e:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter.IsCompleted.get""
    IL_0033:  brtrue.s   IL_0075
    IL_0035:  ldarg.0
    IL_0036:  ldc.i4.1
    IL_0037:  dup
    IL_0038:  stloc.0
    IL_0039:  stfld      ""int C.<F>d__4.<>1__state""
    IL_003e:  ldarg.0
    IL_003f:  ldloc.1
    IL_0040:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<F>d__4.<>u__1""
    IL_0045:  ldarg.0
    IL_0046:  stloc.2
    IL_0047:  ldarg.0
    IL_0048:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<F>d__4.<>t__builder""
    IL_004d:  ldloca.s   V_1
    IL_004f:  ldloca.s   V_2
    IL_0051:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter, C.<F>d__4>(ref System.Runtime.CompilerServices.TaskAwaiter, ref C.<F>d__4)""
    IL_0056:  nop
    IL_0057:  leave.s    IL_00b1
    IL_0059:  ldarg.0
    IL_005a:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<F>d__4.<>u__1""
    IL_005f:  stloc.1
    IL_0060:  ldarg.0
    IL_0061:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter C.<F>d__4.<>u__1""
    IL_0066:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_006c:  ldarg.0
    IL_006d:  ldc.i4.m1
    IL_006e:  dup
    IL_006f:  stloc.0
    IL_0070:  stfld      ""int C.<F>d__4.<>1__state""
    IL_0075:  ldloca.s   V_1
    IL_0077:  call       ""void System.Runtime.CompilerServices.TaskAwaiter.GetResult()""
    IL_007c:  nop
    IL_007d:  call       ""void C.End()""
    IL_0082:  nop
    IL_0083:  leave.s    IL_009d
  }
  catch System.Exception
  {
    IL_0085:  stloc.3
    IL_0086:  ldarg.0
    IL_0087:  ldc.i4.s   -2
    IL_0089:  stfld      ""int C.<F>d__4.<>1__state""
    IL_008e:  ldarg.0
    IL_008f:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<F>d__4.<>t__builder""
    IL_0094:  ldloc.3
    IL_0095:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_009a:  nop
    IL_009b:  leave.s    IL_00b1
  }
  IL_009d:  ldarg.0
  IL_009e:  ldc.i4.s   -2
  IL_00a0:  stfld      ""int C.<F>d__4.<>1__state""
  IL_00a5:  ldarg.0
  IL_00a6:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<F>d__4.<>t__builder""
  IL_00ab:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00b0:  nop
  IL_00b1:  ret
}
");
        }

        [Fact]
        public void UpdateAsync_Await_Remove_TryBlock()
        {
            var source0 = MarkedSource(@"
using System.Threading.Tasks;

class C
{
    static void Start() {}
    static Task M1() => null;
    static Task M2() => null;
    static Task M3() => null;
    static Task M4() => null;
    static Task M5() => null;
    static void End() {}

    static async Task F() 
    {
        Start();
        <N:0>await M1()</N:0>;
        try
        {
            <N:1>await M2()</N:1>;
            <N:2>await M3()</N:2>;
        }
        catch
        {
            <N:3>await M4()</N:3>;
            <N:4>await M5()</N:4>;
        }
        End();
    }
}");
            var source1 = MarkedSource(@"
using System.Threading.Tasks;

class C
{
    static void Start() {}
    static Task M1() => null;
    static Task M2() => null;
    static Task M3() => null;
    static Task M4() => null;
    static Task M5() => null;
    static void End() {}

    static async Task F() 
    {
        Start();
        try
        {
            <N:1>await M2()</N:1>;
        }
        catch
        {
            <N:2>await M4()</N:2>;
        }
        End();
    }
}");
            var compilation0 = CreateCompilationWithMscorlib461(new[] { source0.Tree }, new[] { SystemCoreRef, CSharpRef }, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);

            var v0 = CompileAndVerify(compilation0);
            v0.VerifyDiagnostics();
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                 generation0,
                 ImmutableArray.Create(
                     SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

            v0.VerifyIL("C.<F>d__7.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      671 (0x29f)
  .maxstack  3
  .locals init (int V_0,
                System.Runtime.CompilerServices.TaskAwaiter V_1,
                C.<F>d__7 V_2,
                System.Runtime.CompilerServices.TaskAwaiter V_3,
                System.Runtime.CompilerServices.TaskAwaiter V_4,
                object V_5,
                int V_6,
                System.Runtime.CompilerServices.TaskAwaiter V_7,
                System.Runtime.CompilerServices.TaskAwaiter V_8,
                System.Exception V_9)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<F>d__7.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  switch    (
        IL_0023,
        IL_0025,
        IL_0025,
        IL_0027,
        IL_002c)
    IL_0021:  br.s       IL_0031
    IL_0023:  br.s       IL_0073
    IL_0025:  br.s       IL_009e
    IL_0027:  br         IL_01da
    IL_002c:  br         IL_0239

    IL_0031:  nop
    IL_0032:  call       ""void C.Start()""
    IL_0037:  nop
    IL_0038:  call       ""System.Threading.Tasks.Task C.M1()""
    IL_003d:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter System.Threading.Tasks.Task.GetAwaiter()""
    IL_0042:  stloc.1
    IL_0043:  ldloca.s   V_1
    IL_0045:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter.IsCompleted.get""
    IL_004a:  brtrue.s   IL_008f

    IL_004c:  ldarg.0
    IL_004d:  ldc.i4.0
    IL_004e:  dup
    IL_004f:  stloc.0
    IL_0050:  stfld      ""int C.<F>d__7.<>1__state""
    IL_0055:  ldarg.0
    IL_0056:  ldloc.1
    IL_0057:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<F>d__7.<>u__1""
    IL_005c:  ldarg.0
    IL_005d:  stloc.2
    IL_005e:  ldarg.0
    IL_005f:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<F>d__7.<>t__builder""
    IL_0064:  ldloca.s   V_1
    IL_0066:  ldloca.s   V_2
    IL_0068:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter, C.<F>d__7>(ref System.Runtime.CompilerServices.TaskAwaiter, ref C.<F>d__7)""
    IL_006d:  nop
    IL_006e:  leave      IL_029e

    IL_0073:  ldarg.0
    IL_0074:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<F>d__7.<>u__1""
    IL_0079:  stloc.1
    IL_007a:  ldarg.0
    IL_007b:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter C.<F>d__7.<>u__1""
    IL_0080:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_0086:  ldarg.0
    IL_0087:  ldc.i4.m1
    IL_0088:  dup
    IL_0089:  stloc.0
    IL_008a:  stfld      ""int C.<F>d__7.<>1__state""

    IL_008f:  ldloca.s   V_1
    IL_0091:  call       ""void System.Runtime.CompilerServices.TaskAwaiter.GetResult()""
    IL_0096:  nop
    IL_0097:  ldarg.0
    IL_0098:  ldc.i4.0
    IL_0099:  stfld      ""int C.<F>d__7.<>s__2""

    IL_009e:  nop
    .try
    {
      IL_009f:  ldloc.0
      IL_00a0:  ldc.i4.1
      IL_00a1:  beq.s      IL_00ab
      IL_00a3:  br.s       IL_00a5
      IL_00a5:  ldloc.0
      IL_00a6:  ldc.i4.2
      IL_00a7:  beq.s      IL_00ad
      IL_00a9:  br.s       IL_00b2
      IL_00ab:  br.s       IL_00ee
      IL_00ad:  br         IL_014f

      IL_00b2:  nop
      IL_00b3:  call       ""System.Threading.Tasks.Task C.M2()""
      IL_00b8:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter System.Threading.Tasks.Task.GetAwaiter()""
      IL_00bd:  stloc.3
      IL_00be:  ldloca.s   V_3
      IL_00c0:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter.IsCompleted.get""
      IL_00c5:  brtrue.s   IL_010a

      IL_00c7:  ldarg.0
      IL_00c8:  ldc.i4.1
      IL_00c9:  dup
      IL_00ca:  stloc.0
      IL_00cb:  stfld      ""int C.<F>d__7.<>1__state""
      IL_00d0:  ldarg.0
      IL_00d1:  ldloc.3
      IL_00d2:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<F>d__7.<>u__1""
      IL_00d7:  ldarg.0
      IL_00d8:  stloc.2
      IL_00d9:  ldarg.0
      IL_00da:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<F>d__7.<>t__builder""
      IL_00df:  ldloca.s   V_3
      IL_00e1:  ldloca.s   V_2
      IL_00e3:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter, C.<F>d__7>(ref System.Runtime.CompilerServices.TaskAwaiter, ref C.<F>d__7)""
      IL_00e8:  nop
      IL_00e9:  leave      IL_029e

      IL_00ee:  ldarg.0
      IL_00ef:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<F>d__7.<>u__1""
      IL_00f4:  stloc.3
      IL_00f5:  ldarg.0
      IL_00f6:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter C.<F>d__7.<>u__1""
      IL_00fb:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
      IL_0101:  ldarg.0
      IL_0102:  ldc.i4.m1
      IL_0103:  dup
      IL_0104:  stloc.0
      IL_0105:  stfld      ""int C.<F>d__7.<>1__state""

      IL_010a:  ldloca.s   V_3
      IL_010c:  call       ""void System.Runtime.CompilerServices.TaskAwaiter.GetResult()""
      IL_0111:  nop
      IL_0112:  call       ""System.Threading.Tasks.Task C.M3()""
      IL_0117:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter System.Threading.Tasks.Task.GetAwaiter()""
      IL_011c:  stloc.s    V_4
      IL_011e:  ldloca.s   V_4
      IL_0120:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter.IsCompleted.get""
      IL_0125:  brtrue.s   IL_016c

      IL_0127:  ldarg.0
      IL_0128:  ldc.i4.2
      IL_0129:  dup
      IL_012a:  stloc.0
      IL_012b:  stfld      ""int C.<F>d__7.<>1__state""
      IL_0130:  ldarg.0
      IL_0131:  ldloc.s    V_4
      IL_0133:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<F>d__7.<>u__1""
      IL_0138:  ldarg.0
      IL_0139:  stloc.2
      IL_013a:  ldarg.0
      IL_013b:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<F>d__7.<>t__builder""
      IL_0140:  ldloca.s   V_4
      IL_0142:  ldloca.s   V_2
      IL_0144:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter, C.<F>d__7>(ref System.Runtime.CompilerServices.TaskAwaiter, ref C.<F>d__7)""
      IL_0149:  nop
      IL_014a:  leave      IL_029e

      IL_014f:  ldarg.0
      IL_0150:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<F>d__7.<>u__1""
      IL_0155:  stloc.s    V_4
      IL_0157:  ldarg.0
      IL_0158:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter C.<F>d__7.<>u__1""
      IL_015d:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
      IL_0163:  ldarg.0
      IL_0164:  ldc.i4.m1
      IL_0165:  dup
      IL_0166:  stloc.0
      IL_0167:  stfld      ""int C.<F>d__7.<>1__state""

      IL_016c:  ldloca.s   V_4
      IL_016e:  call       ""void System.Runtime.CompilerServices.TaskAwaiter.GetResult()""
      IL_0173:  nop
      IL_0174:  nop
      IL_0175:  leave.s    IL_018a
    }
    catch object
    {
      IL_0177:  stloc.s    V_5
      IL_0179:  ldarg.0
      IL_017a:  ldloc.s    V_5
      IL_017c:  stfld      ""object C.<F>d__7.<>s__1""
      IL_0181:  ldarg.0
      IL_0182:  ldc.i4.1
      IL_0183:  stfld      ""int C.<F>d__7.<>s__2""
      IL_0188:  leave.s    IL_018a
    }
    IL_018a:  ldarg.0
    IL_018b:  ldfld      ""int C.<F>d__7.<>s__2""
    IL_0190:  stloc.s    V_6
    IL_0192:  ldloc.s    V_6
    IL_0194:  ldc.i4.1
    IL_0195:  beq.s      IL_019c
    IL_0197:  br         IL_0261

    IL_019c:  nop
    IL_019d:  call       ""System.Threading.Tasks.Task C.M4()""
    IL_01a2:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter System.Threading.Tasks.Task.GetAwaiter()""
    IL_01a7:  stloc.s    V_7
    IL_01a9:  ldloca.s   V_7
    IL_01ab:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter.IsCompleted.get""
    IL_01b0:  brtrue.s   IL_01f7

    IL_01b2:  ldarg.0
    IL_01b3:  ldc.i4.3
    IL_01b4:  dup
    IL_01b5:  stloc.0
    IL_01b6:  stfld      ""int C.<F>d__7.<>1__state""
    IL_01bb:  ldarg.0
    IL_01bc:  ldloc.s    V_7
    IL_01be:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<F>d__7.<>u__1""
    IL_01c3:  ldarg.0
    IL_01c4:  stloc.2
    IL_01c5:  ldarg.0
    IL_01c6:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<F>d__7.<>t__builder""
    IL_01cb:  ldloca.s   V_7
    IL_01cd:  ldloca.s   V_2
    IL_01cf:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter, C.<F>d__7>(ref System.Runtime.CompilerServices.TaskAwaiter, ref C.<F>d__7)""
    IL_01d4:  nop
    IL_01d5:  leave      IL_029e

    IL_01da:  ldarg.0
    IL_01db:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<F>d__7.<>u__1""
    IL_01e0:  stloc.s    V_7
    IL_01e2:  ldarg.0
    IL_01e3:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter C.<F>d__7.<>u__1""
    IL_01e8:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_01ee:  ldarg.0
    IL_01ef:  ldc.i4.m1
    IL_01f0:  dup
    IL_01f1:  stloc.0
    IL_01f2:  stfld      ""int C.<F>d__7.<>1__state""

    IL_01f7:  ldloca.s   V_7
    IL_01f9:  call       ""void System.Runtime.CompilerServices.TaskAwaiter.GetResult()""
    IL_01fe:  nop
    IL_01ff:  call       ""System.Threading.Tasks.Task C.M5()""
    IL_0204:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter System.Threading.Tasks.Task.GetAwaiter()""
    IL_0209:  stloc.s    V_8
    IL_020b:  ldloca.s   V_8
    IL_020d:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter.IsCompleted.get""
    IL_0212:  brtrue.s   IL_0256

    IL_0214:  ldarg.0
    IL_0215:  ldc.i4.4
    IL_0216:  dup
    IL_0217:  stloc.0
    IL_0218:  stfld      ""int C.<F>d__7.<>1__state""
    IL_021d:  ldarg.0
    IL_021e:  ldloc.s    V_8
    IL_0220:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<F>d__7.<>u__1""
    IL_0225:  ldarg.0
    IL_0226:  stloc.2
    IL_0227:  ldarg.0
    IL_0228:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<F>d__7.<>t__builder""
    IL_022d:  ldloca.s   V_8
    IL_022f:  ldloca.s   V_2
    IL_0231:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter, C.<F>d__7>(ref System.Runtime.CompilerServices.TaskAwaiter, ref C.<F>d__7)""
    IL_0236:  nop
    IL_0237:  leave.s    IL_029e

    IL_0239:  ldarg.0
    IL_023a:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<F>d__7.<>u__1""
    IL_023f:  stloc.s    V_8
    IL_0241:  ldarg.0
    IL_0242:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter C.<F>d__7.<>u__1""
    IL_0247:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_024d:  ldarg.0
    IL_024e:  ldc.i4.m1
    IL_024f:  dup
    IL_0250:  stloc.0
    IL_0251:  stfld      ""int C.<F>d__7.<>1__state""

    IL_0256:  ldloca.s   V_8
    IL_0258:  call       ""void System.Runtime.CompilerServices.TaskAwaiter.GetResult()""
    IL_025d:  nop
    IL_025e:  nop
    IL_025f:  br.s       IL_0261

    IL_0261:  ldarg.0
    IL_0262:  ldnull
    IL_0263:  stfld      ""object C.<F>d__7.<>s__1""
    IL_0268:  call       ""void C.End()""
    IL_026d:  nop
    IL_026e:  leave.s    IL_028a
  }
  catch System.Exception
  {
    IL_0270:  stloc.s    V_9
    IL_0272:  ldarg.0
    IL_0273:  ldc.i4.s   -2
    IL_0275:  stfld      ""int C.<F>d__7.<>1__state""
    IL_027a:  ldarg.0
    IL_027b:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<F>d__7.<>t__builder""
    IL_0280:  ldloc.s    V_9
    IL_0282:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_0287:  nop
    IL_0288:  leave.s    IL_029e
  }
  IL_028a:  ldarg.0
  IL_028b:  ldc.i4.s   -2
  IL_028d:  stfld      ""int C.<F>d__7.<>1__state""
  IL_0292:  ldarg.0
  IL_0293:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<F>d__7.<>t__builder""
  IL_0298:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_029d:  nop
  IL_029e:  ret
}
");

            diff1.VerifyIL("C.<F>d__7.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", """
            {
              // Code size      358 (0x166)
              .maxstack  3
              .locals init (int V_0,
                            System.Runtime.CompilerServices.TaskAwaiter V_1,
                            C.<F>d__7 V_2,
                            object V_3,
                            int V_4,
                            System.Runtime.CompilerServices.TaskAwaiter V_5,
                            System.Exception V_6)
              IL_0000:  ldarg.0
              IL_0001:  ldfld      "int C.<F>d__7.<>1__state"
              IL_0006:  stloc.0
              .try
              {
                IL_0007:  ldloc.0
                IL_0008:  ldc.i4.1
                IL_0009:  beq.s      IL_0013
                IL_000b:  br.s       IL_000d
                IL_000d:  ldloc.0
                IL_000e:  ldc.i4.2
                IL_000f:  beq.s      IL_0015
                IL_0011:  br.s       IL_001a
                IL_0013:  br.s       IL_0039
                IL_0015:  br         IL_0100
                IL_001a:  ldloc.0
                IL_001b:  ldc.i4.0
                IL_001c:  blt.s      IL_002b
                IL_001e:  ldstr      "Edit and Continue can't resume suspended asynchronous method since the corresponding await expression has been deleted"
                IL_0023:  ldc.i4.s   -4
                IL_0025:  newobj     "System.Runtime.CompilerServices.HotReloadException..ctor(string, int)"
                IL_002a:  throw
                IL_002b:  nop
                IL_002c:  call       "void C.Start()"
                IL_0031:  nop
                IL_0032:  ldarg.0
                IL_0033:  ldc.i4.0
                IL_0034:  stfld      "int C.<F>d__7.<>s__4"
                IL_0039:  nop
                .try
                {
                  IL_003a:  ldloc.0
                  IL_003b:  ldc.i4.1
                  IL_003c:  beq.s      IL_0040
                  IL_003e:  br.s       IL_0042
                  IL_0040:  br.s       IL_007e
                  IL_0042:  nop
                  IL_0043:  call       "System.Threading.Tasks.Task C.M2()"
                  IL_0048:  callvirt   "System.Runtime.CompilerServices.TaskAwaiter System.Threading.Tasks.Task.GetAwaiter()"
                  IL_004d:  stloc.1
                  IL_004e:  ldloca.s   V_1
                  IL_0050:  call       "bool System.Runtime.CompilerServices.TaskAwaiter.IsCompleted.get"
                  IL_0055:  brtrue.s   IL_009a
                  IL_0057:  ldarg.0
                  IL_0058:  ldc.i4.1
                  IL_0059:  dup
                  IL_005a:  stloc.0
                  IL_005b:  stfld      "int C.<F>d__7.<>1__state"
                  IL_0060:  ldarg.0
                  IL_0061:  ldloc.1
                  IL_0062:  stfld      "System.Runtime.CompilerServices.TaskAwaiter C.<F>d__7.<>u__1"
                  IL_0067:  ldarg.0
                  IL_0068:  stloc.2
                  IL_0069:  ldarg.0
                  IL_006a:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<F>d__7.<>t__builder"
                  IL_006f:  ldloca.s   V_1
                  IL_0071:  ldloca.s   V_2
                  IL_0073:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter, C.<F>d__7>(ref System.Runtime.CompilerServices.TaskAwaiter, ref C.<F>d__7)"
                  IL_0078:  nop
                  IL_0079:  leave      IL_0165
                  IL_007e:  ldarg.0
                  IL_007f:  ldfld      "System.Runtime.CompilerServices.TaskAwaiter C.<F>d__7.<>u__1"
                  IL_0084:  stloc.1
                  IL_0085:  ldarg.0
                  IL_0086:  ldflda     "System.Runtime.CompilerServices.TaskAwaiter C.<F>d__7.<>u__1"
                  IL_008b:  initobj    "System.Runtime.CompilerServices.TaskAwaiter"
                  IL_0091:  ldarg.0
                  IL_0092:  ldc.i4.m1
                  IL_0093:  dup
                  IL_0094:  stloc.0
                  IL_0095:  stfld      "int C.<F>d__7.<>1__state"
                  IL_009a:  ldloca.s   V_1
                  IL_009c:  call       "void System.Runtime.CompilerServices.TaskAwaiter.GetResult()"
                  IL_00a1:  nop
                  IL_00a2:  nop
                  IL_00a3:  leave.s    IL_00b6
                }
                catch object
                {
                  IL_00a5:  stloc.3
                  IL_00a6:  ldarg.0
                  IL_00a7:  ldloc.3
                  IL_00a8:  stfld      "object C.<F>d__7.<>s__3"
                  IL_00ad:  ldarg.0
                  IL_00ae:  ldc.i4.1
                  IL_00af:  stfld      "int C.<F>d__7.<>s__4"
                  IL_00b4:  leave.s    IL_00b6
                }
                IL_00b6:  ldarg.0
                IL_00b7:  ldfld      "int C.<F>d__7.<>s__4"
                IL_00bc:  stloc.s    V_4
                IL_00be:  ldloc.s    V_4
                IL_00c0:  ldc.i4.1
                IL_00c1:  beq.s      IL_00c5
                IL_00c3:  br.s       IL_0128
                IL_00c5:  nop
                IL_00c6:  call       "System.Threading.Tasks.Task C.M4()"
                IL_00cb:  callvirt   "System.Runtime.CompilerServices.TaskAwaiter System.Threading.Tasks.Task.GetAwaiter()"
                IL_00d0:  stloc.s    V_5
                IL_00d2:  ldloca.s   V_5
                IL_00d4:  call       "bool System.Runtime.CompilerServices.TaskAwaiter.IsCompleted.get"
                IL_00d9:  brtrue.s   IL_011d
                IL_00db:  ldarg.0
                IL_00dc:  ldc.i4.2
                IL_00dd:  dup
                IL_00de:  stloc.0
                IL_00df:  stfld      "int C.<F>d__7.<>1__state"
                IL_00e4:  ldarg.0
                IL_00e5:  ldloc.s    V_5
                IL_00e7:  stfld      "System.Runtime.CompilerServices.TaskAwaiter C.<F>d__7.<>u__1"
                IL_00ec:  ldarg.0
                IL_00ed:  stloc.2
                IL_00ee:  ldarg.0
                IL_00ef:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<F>d__7.<>t__builder"
                IL_00f4:  ldloca.s   V_5
                IL_00f6:  ldloca.s   V_2
                IL_00f8:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter, C.<F>d__7>(ref System.Runtime.CompilerServices.TaskAwaiter, ref C.<F>d__7)"
                IL_00fd:  nop
                IL_00fe:  leave.s    IL_0165
                IL_0100:  ldarg.0
                IL_0101:  ldfld      "System.Runtime.CompilerServices.TaskAwaiter C.<F>d__7.<>u__1"
                IL_0106:  stloc.s    V_5
                IL_0108:  ldarg.0
                IL_0109:  ldflda     "System.Runtime.CompilerServices.TaskAwaiter C.<F>d__7.<>u__1"
                IL_010e:  initobj    "System.Runtime.CompilerServices.TaskAwaiter"
                IL_0114:  ldarg.0
                IL_0115:  ldc.i4.m1
                IL_0116:  dup
                IL_0117:  stloc.0
                IL_0118:  stfld      "int C.<F>d__7.<>1__state"
                IL_011d:  ldloca.s   V_5
                IL_011f:  call       "void System.Runtime.CompilerServices.TaskAwaiter.GetResult()"
                IL_0124:  nop
                IL_0125:  nop
                IL_0126:  br.s       IL_0128
                IL_0128:  ldarg.0
                IL_0129:  ldnull
                IL_012a:  stfld      "object C.<F>d__7.<>s__3"
                IL_012f:  call       "void C.End()"
                IL_0134:  nop
                IL_0135:  leave.s    IL_0151
              }
              catch System.Exception
              {
                IL_0137:  stloc.s    V_6
                IL_0139:  ldarg.0
                IL_013a:  ldc.i4.s   -2
                IL_013c:  stfld      "int C.<F>d__7.<>1__state"
                IL_0141:  ldarg.0
                IL_0142:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<F>d__7.<>t__builder"
                IL_0147:  ldloc.s    V_6
                IL_0149:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
                IL_014e:  nop
                IL_014f:  leave.s    IL_0165
              }
              IL_0151:  ldarg.0
              IL_0152:  ldc.i4.s   -2
              IL_0154:  stfld      "int C.<F>d__7.<>1__state"
              IL_0159:  ldarg.0
              IL_015a:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<F>d__7.<>t__builder"
              IL_015f:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
              IL_0164:  nop
              IL_0165:  ret
            }
            """);
        }

        [Fact]
        public void UpdateAsync_AwaitDeclarationMappedToNonAwait()
        {
            var source0 = MarkedSource(@"
using System;
using System.Threading.Tasks;

class C
{
    static async Task F() 
    {
        <N:0>await Task.CompletedTask</N:0>;
        IAsyncDisposable <N:1>x = G()</N:1>, <N:2>y = G()</N:2>;
    }

    static IAsyncDisposable G() => null; 
}");
            var source1 = MarkedSource(@"
using System;
using System.Threading.Tasks;

class C
{
    static async Task F() 
    {
        <N:0>await Task.CompletedTask</N:0>;
        await using IAsyncDisposable <N:1>x = G()</N:1>, <N:2>y = G()</N:2>;
    }

    static IAsyncDisposable G() => null; 
}");
            var asyncStreamsTree = Parse(AsyncStreamsTypes, options: (CSharpParseOptions)source0.Tree.Options);

            var compilation0 = CreateCompilationWithTasksExtensions(new[] { source0.Tree, asyncStreamsTree }, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(new[] { source1.Tree, asyncStreamsTree });

            var v0 = CompileAndVerify(compilation0);
            v0.VerifyDiagnostics();
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                 generation0,
                 ImmutableArray.Create(
                     SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

            // Note that the CDI contains local variable declaration mapping to facilitate local variable slot allocation,
            // but these are not included in the state machine map since the V0 version of the local declaration statement does not have "await" keyword.
            // Therefore, the V1 version will not be able ot match the local declaration statements to their previous versions when emitting
            // state machine states and create new states for them as expected.
            v0.VerifyPdb("C.F", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""F"">
      <customDebugInfo>
        <forwardIterator name=""&lt;F&gt;d__0"" />
        <encLocalSlotMap>
          <slot kind=""0"" offset=""79"" />
          <slot kind=""0"" offset=""99"" />
        </encLocalSlotMap>
        <encStateMachineStateMap>
          <state number=""0"" offset=""16"" />
        </encStateMachineStateMap>
      </customDebugInfo>
    </method>
  </methods>
</symbols>");

            // note preserved hoisted variables x, y:

            v0.VerifySynthesizedFields("C.<F>d__0",
                "int <>1__state",
                "System.Runtime.CompilerServices.AsyncTaskMethodBuilder <>t__builder",
                "System.IAsyncDisposable <x>5__1",
                "System.IAsyncDisposable <y>5__2",
                "System.Runtime.CompilerServices.TaskAwaiter <>u__1");

            diff1.VerifySynthesizedFields("C.<F>d__0",
                "<>1__state: int",
                "<>t__builder: System.Runtime.CompilerServices.AsyncTaskMethodBuilder",
                "<x>5__1: System.IAsyncDisposable",
                "<y>5__2: System.IAsyncDisposable",
                "<>s__3: object",
                "<>s__4: int",
                "<>s__5: object",
                "<>s__6: int",
                "<>u__1: System.Runtime.CompilerServices.TaskAwaiter",
                "<>u__2: System.Runtime.CompilerServices.ValueTaskAwaiter");

            diff1.VerifyIL("C.<F>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      678 (0x2a6)
  .maxstack  3
  .locals init (int V_0,
                System.Runtime.CompilerServices.TaskAwaiter V_1,
                C.<F>d__0 V_2,
                object V_3,
                System.Runtime.CompilerServices.ValueTaskAwaiter V_4,
                System.Threading.Tasks.ValueTask V_5,
                System.Exception V_6,
                int V_7,
                System.Runtime.CompilerServices.ValueTaskAwaiter V_8)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<F>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  switch    (
        IL_001b,
        IL_001d,
        IL_001f)
    IL_0019:  br.s       IL_0024
    IL_001b:  br.s       IL_0060
    IL_001d:  br.s       IL_009d
    IL_001f:  br         IL_01e9
    IL_0024:  nop
    IL_0025:  call       ""System.Threading.Tasks.Task System.Threading.Tasks.Task.CompletedTask.get""
    IL_002a:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter System.Threading.Tasks.Task.GetAwaiter()""
    IL_002f:  stloc.1
    IL_0030:  ldloca.s   V_1
    IL_0032:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter.IsCompleted.get""
    IL_0037:  brtrue.s   IL_007c
    IL_0039:  ldarg.0
    IL_003a:  ldc.i4.0
    IL_003b:  dup
    IL_003c:  stloc.0
    IL_003d:  stfld      ""int C.<F>d__0.<>1__state""
    IL_0042:  ldarg.0
    IL_0043:  ldloc.1
    IL_0044:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<F>d__0.<>u__1""
    IL_0049:  ldarg.0
    IL_004a:  stloc.2
    IL_004b:  ldarg.0
    IL_004c:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<F>d__0.<>t__builder""
    IL_0051:  ldloca.s   V_1
    IL_0053:  ldloca.s   V_2
    IL_0055:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter, C.<F>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter, ref C.<F>d__0)""
    IL_005a:  nop
    IL_005b:  leave      IL_02a5
    IL_0060:  ldarg.0
    IL_0061:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<F>d__0.<>u__1""
    IL_0066:  stloc.1
    IL_0067:  ldarg.0
    IL_0068:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter C.<F>d__0.<>u__1""
    IL_006d:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_0073:  ldarg.0
    IL_0074:  ldc.i4.m1
    IL_0075:  dup
    IL_0076:  stloc.0
    IL_0077:  stfld      ""int C.<F>d__0.<>1__state""
    IL_007c:  ldloca.s   V_1
    IL_007e:  call       ""void System.Runtime.CompilerServices.TaskAwaiter.GetResult()""
    IL_0083:  nop
    IL_0084:  ldarg.0
    IL_0085:  call       ""System.IAsyncDisposable C.G()""
    IL_008a:  stfld      ""System.IAsyncDisposable C.<F>d__0.<x>5__1""
    IL_008f:  ldarg.0
    IL_0090:  ldnull
    IL_0091:  stfld      ""object C.<F>d__0.<>s__3""
    IL_0096:  ldarg.0
    IL_0097:  ldc.i4.0
    IL_0098:  stfld      ""int C.<F>d__0.<>s__4""
    IL_009d:  nop
    .try
    {
      IL_009e:  ldloc.0
      IL_009f:  ldc.i4.1
      IL_00a0:  beq.s      IL_00a4
      IL_00a2:  br.s       IL_00a6
      IL_00a4:  br.s       IL_0123
      IL_00a6:  ldarg.0
      IL_00a7:  call       ""System.IAsyncDisposable C.G()""
      IL_00ac:  stfld      ""System.IAsyncDisposable C.<F>d__0.<y>5__2""
      IL_00b1:  ldarg.0
      IL_00b2:  ldnull
      IL_00b3:  stfld      ""object C.<F>d__0.<>s__5""
      IL_00b8:  ldarg.0
      IL_00b9:  ldc.i4.0
      IL_00ba:  stfld      ""int C.<F>d__0.<>s__6""
      .try
      {
        IL_00bf:  br.s       IL_00c1
        IL_00c1:  ldarg.0
        IL_00c2:  ldc.i4.1
        IL_00c3:  stfld      ""int C.<F>d__0.<>s__6""
        IL_00c8:  leave.s    IL_00d4
      }
      catch object
      {
        IL_00ca:  stloc.3
        IL_00cb:  ldarg.0
        IL_00cc:  ldloc.3
        IL_00cd:  stfld      ""object C.<F>d__0.<>s__5""
        IL_00d2:  leave.s    IL_00d4
      }
      IL_00d4:  ldarg.0
      IL_00d5:  ldfld      ""System.IAsyncDisposable C.<F>d__0.<y>5__2""
      IL_00da:  brfalse.s  IL_0148
      IL_00dc:  ldarg.0
      IL_00dd:  ldfld      ""System.IAsyncDisposable C.<F>d__0.<y>5__2""
      IL_00e2:  callvirt   ""System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()""
      IL_00e7:  stloc.s    V_5
      IL_00e9:  ldloca.s   V_5
      IL_00eb:  call       ""System.Runtime.CompilerServices.ValueTaskAwaiter System.Threading.Tasks.ValueTask.GetAwaiter()""
      IL_00f0:  stloc.s    V_4
      IL_00f2:  ldloca.s   V_4
      IL_00f4:  call       ""bool System.Runtime.CompilerServices.ValueTaskAwaiter.IsCompleted.get""
      IL_00f9:  brtrue.s   IL_0140
      IL_00fb:  ldarg.0
      IL_00fc:  ldc.i4.1
      IL_00fd:  dup
      IL_00fe:  stloc.0
      IL_00ff:  stfld      ""int C.<F>d__0.<>1__state""
      IL_0104:  ldarg.0
      IL_0105:  ldloc.s    V_4
      IL_0107:  stfld      ""System.Runtime.CompilerServices.ValueTaskAwaiter C.<F>d__0.<>u__2""
      IL_010c:  ldarg.0
      IL_010d:  stloc.2
      IL_010e:  ldarg.0
      IL_010f:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<F>d__0.<>t__builder""
      IL_0114:  ldloca.s   V_4
      IL_0116:  ldloca.s   V_2
      IL_0118:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.ValueTaskAwaiter, C.<F>d__0>(ref System.Runtime.CompilerServices.ValueTaskAwaiter, ref C.<F>d__0)""
      IL_011d:  nop
      IL_011e:  leave      IL_02a5
      IL_0123:  ldarg.0
      IL_0124:  ldfld      ""System.Runtime.CompilerServices.ValueTaskAwaiter C.<F>d__0.<>u__2""
      IL_0129:  stloc.s    V_4
      IL_012b:  ldarg.0
      IL_012c:  ldflda     ""System.Runtime.CompilerServices.ValueTaskAwaiter C.<F>d__0.<>u__2""
      IL_0131:  initobj    ""System.Runtime.CompilerServices.ValueTaskAwaiter""
      IL_0137:  ldarg.0
      IL_0138:  ldc.i4.m1
      IL_0139:  dup
      IL_013a:  stloc.0
      IL_013b:  stfld      ""int C.<F>d__0.<>1__state""
      IL_0140:  ldloca.s   V_4
      IL_0142:  call       ""void System.Runtime.CompilerServices.ValueTaskAwaiter.GetResult()""
      IL_0147:  nop
      IL_0148:  ldarg.0
      IL_0149:  ldfld      ""object C.<F>d__0.<>s__5""
      IL_014e:  stloc.3
      IL_014f:  ldloc.3
      IL_0150:  brfalse.s  IL_016d
      IL_0152:  ldloc.3
      IL_0153:  isinst     ""System.Exception""
      IL_0158:  stloc.s    V_6
      IL_015a:  ldloc.s    V_6
      IL_015c:  brtrue.s   IL_0160
      IL_015e:  ldloc.3
      IL_015f:  throw
      IL_0160:  ldloc.s    V_6
      IL_0162:  call       ""System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)""
      IL_0167:  callvirt   ""void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()""
      IL_016c:  nop
      IL_016d:  ldarg.0
      IL_016e:  ldfld      ""int C.<F>d__0.<>s__6""
      IL_0173:  stloc.s    V_7
      IL_0175:  ldloc.s    V_7
      IL_0177:  ldc.i4.1
      IL_0178:  beq.s      IL_017c
      IL_017a:  br.s       IL_017e
      IL_017c:  br.s       IL_0187
      IL_017e:  ldarg.0
      IL_017f:  ldnull
      IL_0180:  stfld      ""object C.<F>d__0.<>s__5""
      IL_0185:  leave.s    IL_019a
      IL_0187:  ldarg.0
      IL_0188:  ldc.i4.1
      IL_0189:  stfld      ""int C.<F>d__0.<>s__4""
      IL_018e:  leave.s    IL_019a
    }
    catch object
    {
      IL_0190:  stloc.3
      IL_0191:  ldarg.0
      IL_0192:  ldloc.3
      IL_0193:  stfld      ""object C.<F>d__0.<>s__3""
      IL_0198:  leave.s    IL_019a
    }
    IL_019a:  ldarg.0
    IL_019b:  ldfld      ""System.IAsyncDisposable C.<F>d__0.<x>5__1""
    IL_01a0:  brfalse.s  IL_020e
    IL_01a2:  ldarg.0
    IL_01a3:  ldfld      ""System.IAsyncDisposable C.<F>d__0.<x>5__1""
    IL_01a8:  callvirt   ""System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()""
    IL_01ad:  stloc.s    V_5
    IL_01af:  ldloca.s   V_5
    IL_01b1:  call       ""System.Runtime.CompilerServices.ValueTaskAwaiter System.Threading.Tasks.ValueTask.GetAwaiter()""
    IL_01b6:  stloc.s    V_8
    IL_01b8:  ldloca.s   V_8
    IL_01ba:  call       ""bool System.Runtime.CompilerServices.ValueTaskAwaiter.IsCompleted.get""
    IL_01bf:  brtrue.s   IL_0206
    IL_01c1:  ldarg.0
    IL_01c2:  ldc.i4.2
    IL_01c3:  dup
    IL_01c4:  stloc.0
    IL_01c5:  stfld      ""int C.<F>d__0.<>1__state""
    IL_01ca:  ldarg.0
    IL_01cb:  ldloc.s    V_8
    IL_01cd:  stfld      ""System.Runtime.CompilerServices.ValueTaskAwaiter C.<F>d__0.<>u__2""
    IL_01d2:  ldarg.0
    IL_01d3:  stloc.2
    IL_01d4:  ldarg.0
    IL_01d5:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<F>d__0.<>t__builder""
    IL_01da:  ldloca.s   V_8
    IL_01dc:  ldloca.s   V_2
    IL_01de:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.ValueTaskAwaiter, C.<F>d__0>(ref System.Runtime.CompilerServices.ValueTaskAwaiter, ref C.<F>d__0)""
    IL_01e3:  nop
    IL_01e4:  leave      IL_02a5
    IL_01e9:  ldarg.0
    IL_01ea:  ldfld      ""System.Runtime.CompilerServices.ValueTaskAwaiter C.<F>d__0.<>u__2""
    IL_01ef:  stloc.s    V_8
    IL_01f1:  ldarg.0
    IL_01f2:  ldflda     ""System.Runtime.CompilerServices.ValueTaskAwaiter C.<F>d__0.<>u__2""
    IL_01f7:  initobj    ""System.Runtime.CompilerServices.ValueTaskAwaiter""
    IL_01fd:  ldarg.0
    IL_01fe:  ldc.i4.m1
    IL_01ff:  dup
    IL_0200:  stloc.0
    IL_0201:  stfld      ""int C.<F>d__0.<>1__state""
    IL_0206:  ldloca.s   V_8
    IL_0208:  call       ""void System.Runtime.CompilerServices.ValueTaskAwaiter.GetResult()""
    IL_020d:  nop
    IL_020e:  ldarg.0
    IL_020f:  ldfld      ""object C.<F>d__0.<>s__3""
    IL_0214:  stloc.3
    IL_0215:  ldloc.3
    IL_0216:  brfalse.s  IL_0233
    IL_0218:  ldloc.3
    IL_0219:  isinst     ""System.Exception""
    IL_021e:  stloc.s    V_6
    IL_0220:  ldloc.s    V_6
    IL_0222:  brtrue.s   IL_0226
    IL_0224:  ldloc.3
    IL_0225:  throw
    IL_0226:  ldloc.s    V_6
    IL_0228:  call       ""System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)""
    IL_022d:  callvirt   ""void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()""
    IL_0232:  nop
    IL_0233:  ldarg.0
    IL_0234:  ldfld      ""int C.<F>d__0.<>s__4""
    IL_0239:  stloc.s    V_7
    IL_023b:  ldloc.s    V_7
    IL_023d:  ldc.i4.1
    IL_023e:  beq.s      IL_0242
    IL_0240:  br.s       IL_0244
    IL_0242:  leave.s    IL_0283
    IL_0244:  ldarg.0
    IL_0245:  ldnull
    IL_0246:  stfld      ""object C.<F>d__0.<>s__3""
    IL_024b:  ldarg.0
    IL_024c:  ldnull
    IL_024d:  stfld      ""System.IAsyncDisposable C.<F>d__0.<x>5__1""
    IL_0252:  ldarg.0
    IL_0253:  ldnull
    IL_0254:  stfld      ""System.IAsyncDisposable C.<F>d__0.<y>5__2""
    IL_0259:  leave.s    IL_0283
  }
  catch System.Exception
  {
    IL_025b:  stloc.s    V_6
    IL_025d:  ldarg.0
    IL_025e:  ldc.i4.s   -2
    IL_0260:  stfld      ""int C.<F>d__0.<>1__state""
    IL_0265:  ldarg.0
    IL_0266:  ldnull
    IL_0267:  stfld      ""System.IAsyncDisposable C.<F>d__0.<x>5__1""
    IL_026c:  ldarg.0
    IL_026d:  ldnull
    IL_026e:  stfld      ""System.IAsyncDisposable C.<F>d__0.<y>5__2""
    IL_0273:  ldarg.0
    IL_0274:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<F>d__0.<>t__builder""
    IL_0279:  ldloc.s    V_6
    IL_027b:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_0280:  nop
    IL_0281:  leave.s    IL_02a5
  }
  IL_0283:  ldarg.0
  IL_0284:  ldc.i4.s   -2
  IL_0286:  stfld      ""int C.<F>d__0.<>1__state""
  IL_028b:  ldarg.0
  IL_028c:  ldnull
  IL_028d:  stfld      ""System.IAsyncDisposable C.<F>d__0.<x>5__1""
  IL_0292:  ldarg.0
  IL_0293:  ldnull
  IL_0294:  stfld      ""System.IAsyncDisposable C.<F>d__0.<y>5__2""
  IL_0299:  ldarg.0
  IL_029a:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<F>d__0.<>t__builder""
  IL_029f:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_02a4:  nop
  IL_02a5:  ret
}");
        }

        [Fact]
        public void UpdateIterator_UserDefinedVariables_NoChange()
        {
            var source0 = MarkedSource(@"
using System.Collections.Generic;

class C
{
    static IEnumerable<int> F(int p) 
    {
        int <N:0>x = p</N:0>;
        <N:1>yield return 1</N:1>;
    }
}");
            var source1 = MarkedSource(@"
using System.Collections.Generic;

class C
{
    static IEnumerable<int> F(int p) 
    {
        int <N:0>x = p</N:0>;
        <N:1>yield return 2</N:1>;
    }
}");
            var compilation0 = CreateCompilationWithMscorlib461(source0.Tree, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);

            var v0 = CompileAndVerify(compilation0);
            var symReader = v0.CreateSymReader();

            using var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);
            var method0 = compilation0.GetMember<MethodSymbol>("C.F");
            var method1 = compilation1.GetMember<MethodSymbol>("C.F");

            var generation0 = CreateInitialBaseline(compilation0, md0, symReader.GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1, GetSyntaxMapFromMarkers(source0, source1))));

            // Verify delta metadata contains expected rows.
            using var md1 = diff1.GetMetadata();

            // Verify that no new TypeDefs, FieldDefs or MethodDefs were added,
            // 3 methods were updated: 
            // - the kick-off method (might be changed if the method previously wasn't an iterator)
            // - Finally method
            // - MoveNext method
            CheckEncLogDefinitions(md1.Reader,
                Row(3, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(1, TableIndex.Param, EditAndContinueOperation.Default),
                Row(1, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(7, TableIndex.CustomAttribute, EditAndContinueOperation.Default));

            diff1.VerifyIL("C.<F>d__0.System.Collections.IEnumerator.MoveNext", @"
{
  // Code size       69 (0x45)
  .maxstack  2
  .locals init (int V_0)
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
  IL_0012:  br.s       IL_0018
  IL_0014:  br.s       IL_003c
  IL_0016:  ldc.i4.0
  IL_0017:  ret
  IL_0018:  ldarg.0
  IL_0019:  ldc.i4.m1
  IL_001a:  stfld      ""int C.<F>d__0.<>1__state""
  IL_001f:  nop
  IL_0020:  ldarg.0
  IL_0021:  ldarg.0
  IL_0022:  ldfld      ""int C.<F>d__0.p""
  IL_0027:  stfld      ""int C.<F>d__0.<x>5__1""
  IL_002c:  ldarg.0
  IL_002d:  ldc.i4.2
  IL_002e:  stfld      ""int C.<F>d__0.<>2__current""
  IL_0033:  ldarg.0
  IL_0034:  ldc.i4.1
  IL_0035:  stfld      ""int C.<F>d__0.<>1__state""
  IL_003a:  ldc.i4.1
  IL_003b:  ret
  IL_003c:  ldarg.0
  IL_003d:  ldc.i4.m1
  IL_003e:  stfld      ""int C.<F>d__0.<>1__state""
  IL_0043:  ldc.i4.0
  IL_0044:  ret
}");
        }

        [Fact]
        public void UpdateIterator_UserDefinedVariables_AddVariable()
        {
            var source0 = MarkedSource(@"
using System;
using System.Collections.Generic;

class C
{
    static IEnumerable<int> F(int p) 
    {
        int <N:0>x = p</N:0>;
        <N:1>yield return x;</N:1>
    }
}");
            var source1 = MarkedSource(@"
using System;
using System.Collections.Generic;

class C
{
    static IEnumerable<int> F(int p) 
    {
        int y = 1234;
        int <N:0>x = p</N:0>;
        <N:1>yield return y</N:1>;
        Console.WriteLine(x);
    }
}");
            var compilation0 = CreateCompilationWithMscorlib461(source0.Tree, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);

            var v0 = CompileAndVerify(compilation0);
            var symReader = v0.CreateSymReader();

            using var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);
            var method0 = compilation0.GetMember<MethodSymbol>("C.F");
            var method1 = compilation1.GetMember<MethodSymbol>("C.F");

            var generation0 = CreateInitialBaseline(compilation0, md0, symReader.GetEncMethodDebugInfo);
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1, GetSyntaxMapFromMarkers(source0, source1))));

            // Verify delta metadata contains expected rows.
            using var md1 = diff1.GetMetadata();

            // 1 field def added & 3 methods updated
            CheckEncLogDefinitions(md1.Reader,
                Row(3, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                Row(7, TableIndex.Field, EditAndContinueOperation.Default),
                Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(1, TableIndex.Param, EditAndContinueOperation.Default),
                Row(1, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(7, TableIndex.CustomAttribute, EditAndContinueOperation.Default));

            diff1.VerifyIL("C.<F>d__0.System.Collections.IEnumerator.MoveNext", @"
{
  // Code size       97 (0x61)
  .maxstack  2
  .locals init (int V_0)
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
  IL_0012:  br.s       IL_0018
  IL_0014:  br.s       IL_004c
  IL_0016:  ldc.i4.0
  IL_0017:  ret
  IL_0018:  ldarg.0
  IL_0019:  ldc.i4.m1
  IL_001a:  stfld      ""int C.<F>d__0.<>1__state""
  IL_001f:  nop
  IL_0020:  ldarg.0
  IL_0021:  ldc.i4     0x4d2
  IL_0026:  stfld      ""int C.<F>d__0.<y>5__2""
  IL_002b:  ldarg.0
  IL_002c:  ldarg.0
  IL_002d:  ldfld      ""int C.<F>d__0.p""
  IL_0032:  stfld      ""int C.<F>d__0.<x>5__1""
  IL_0037:  ldarg.0
  IL_0038:  ldarg.0
  IL_0039:  ldfld      ""int C.<F>d__0.<y>5__2""
  IL_003e:  stfld      ""int C.<F>d__0.<>2__current""
  IL_0043:  ldarg.0
  IL_0044:  ldc.i4.1
  IL_0045:  stfld      ""int C.<F>d__0.<>1__state""
  IL_004a:  ldc.i4.1
  IL_004b:  ret
  IL_004c:  ldarg.0
  IL_004d:  ldc.i4.m1
  IL_004e:  stfld      ""int C.<F>d__0.<>1__state""
  IL_0053:  ldarg.0
  IL_0054:  ldfld      ""int C.<F>d__0.<x>5__1""
  IL_0059:  call       ""void System.Console.WriteLine(int)""
  IL_005e:  nop
  IL_005f:  ldc.i4.0
  IL_0060:  ret
}");
        }

        [Fact]
        public void UpdateIterator_UserDefinedVariables_AddAndRemoveVariable()
        {
            var source0 = MarkedSource(@"
using System;
using System.Collections.Generic;

class C
{
    static IEnumerable<int> F(int p) 
    {
        int x = p;
        <N:0>yield return x;</N:0>
    }
}");
            var source1 = MarkedSource(@"
using System;
using System.Collections.Generic;

class C
{
    static IEnumerable<int> F(int p) 
    {
        int y = 1234;
        <N:0>yield return y;</N:0>
        Console.WriteLine(p);
    }
}");
            var compilation0 = CreateCompilationWithMscorlib461(source0.Tree, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);

            var v0 = CompileAndVerify(compilation0);
            var symReader = v0.CreateSymReader();

            using var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);
            var method0 = compilation0.GetMember<MethodSymbol>("C.F");
            var method1 = compilation1.GetMember<MethodSymbol>("C.F");

            var generation0 = CreateInitialBaseline(compilation0, md0, symReader.GetEncMethodDebugInfo);
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1, GetSyntaxMapFromMarkers(source0, source1))));

            // Verify delta metadata contains expected rows.
            using var md1 = diff1.GetMetadata();

            // 1 field def added & 3 methods updated
            CheckEncLogDefinitions(md1.Reader,
                Row(3, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                Row(7, TableIndex.Field, EditAndContinueOperation.Default),
                Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(1, TableIndex.Param, EditAndContinueOperation.Default),
                Row(1, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(7, TableIndex.CustomAttribute, EditAndContinueOperation.Default));

            diff1.VerifyIL("C.<F>d__0.System.Collections.IEnumerator.MoveNext", @"
{
  // Code size       85 (0x55)
  .maxstack  2
  .locals init (int V_0)
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
  IL_0012:  br.s       IL_0018
  IL_0014:  br.s       IL_0040
  IL_0016:  ldc.i4.0
  IL_0017:  ret
  IL_0018:  ldarg.0
  IL_0019:  ldc.i4.m1
  IL_001a:  stfld      ""int C.<F>d__0.<>1__state""
  IL_001f:  nop
  IL_0020:  ldarg.0
  IL_0021:  ldc.i4     0x4d2
  IL_0026:  stfld      ""int C.<F>d__0.<y>5__2""
  IL_002b:  ldarg.0
  IL_002c:  ldarg.0
  IL_002d:  ldfld      ""int C.<F>d__0.<y>5__2""
  IL_0032:  stfld      ""int C.<F>d__0.<>2__current""
  IL_0037:  ldarg.0
  IL_0038:  ldc.i4.1
  IL_0039:  stfld      ""int C.<F>d__0.<>1__state""
  IL_003e:  ldc.i4.1
  IL_003f:  ret
  IL_0040:  ldarg.0
  IL_0041:  ldc.i4.m1
  IL_0042:  stfld      ""int C.<F>d__0.<>1__state""
  IL_0047:  ldarg.0
  IL_0048:  ldfld      ""int C.<F>d__0.p""
  IL_004d:  call       ""void System.Console.WriteLine(int)""
  IL_0052:  nop
  IL_0053:  ldc.i4.0
  IL_0054:  ret
}");
        }

        [Fact]
        public void UpdateIterator_UserDefinedVariables_ChangeVariableType()
        {
            var source0 = MarkedSource(@"
using System;
using System.Collections.Generic;

class C
{
    static IEnumerable<int> F() 
    {
        var <N:0>x = 1</N:0>;
        <N:1>yield return 1</N:1>;
        Console.WriteLine(x);
    }
}");
            var source1 = MarkedSource(@"
using System;
using System.Collections.Generic;

class C
{
    static IEnumerable<int> F() 
    {
        var <N:0>x = 1.0</N:0>;
        <N:1>yield return 2</N:1>;
        Console.WriteLine(x);
    }
}");
            var compilation0 = CreateCompilationWithMscorlib461(source0.Tree, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);

            var v0 = CompileAndVerify(compilation0);
            var symReader = v0.CreateSymReader();

            using var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);
            var method0 = compilation0.GetMember<MethodSymbol>("C.F");
            var method1 = compilation1.GetMember<MethodSymbol>("C.F");

            var generation0 = CreateInitialBaseline(compilation0, md0, symReader.GetEncMethodDebugInfo);
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1, GetSyntaxMapFromMarkers(source0, source1))));

            // Verify delta metadata contains expected rows.
            using var md1 = diff1.GetMetadata();

            // 1 field def added & 3 methods updated
            CheckEncLogDefinitions(md1.Reader,
                Row(3, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                Row(5, TableIndex.Field, EditAndContinueOperation.Default),
                Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(1, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(7, TableIndex.CustomAttribute, EditAndContinueOperation.Default));

            diff1.VerifyIL("C.<F>d__0.System.Collections.IEnumerator.MoveNext", @"
{
  // Code size       84 (0x54)
  .maxstack  2
  .locals init (int V_0)
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
  IL_0012:  br.s       IL_0018
  IL_0014:  br.s       IL_003f
  IL_0016:  ldc.i4.0
  IL_0017:  ret
  IL_0018:  ldarg.0
  IL_0019:  ldc.i4.m1
  IL_001a:  stfld      ""int C.<F>d__0.<>1__state""
  IL_001f:  nop
  IL_0020:  ldarg.0
  IL_0021:  ldc.r8     1
  IL_002a:  stfld      ""double C.<F>d__0.<x>5__2""
  IL_002f:  ldarg.0
  IL_0030:  ldc.i4.2
  IL_0031:  stfld      ""int C.<F>d__0.<>2__current""
  IL_0036:  ldarg.0
  IL_0037:  ldc.i4.1
  IL_0038:  stfld      ""int C.<F>d__0.<>1__state""
  IL_003d:  ldc.i4.1
  IL_003e:  ret
  IL_003f:  ldarg.0
  IL_0040:  ldc.i4.m1
  IL_0041:  stfld      ""int C.<F>d__0.<>1__state""
  IL_0046:  ldarg.0
  IL_0047:  ldfld      ""double C.<F>d__0.<x>5__2""
  IL_004c:  call       ""void System.Console.WriteLine(double)""
  IL_0051:  nop
  IL_0052:  ldc.i4.0
  IL_0053:  ret
}");
        }

        [Fact]
        public void UpdateIterator_SynthesizedVariables_ChangeVariableType()
        {
            var source0 = MarkedSource(@"
using System;
using System.Collections.Generic;

class C
{
    static IEnumerable<int> F() 
    {
        <N:0>foreach</N:0> (object item in new[] { 1 }) { <N:1>yield return 1;</N:1> }
    }
}");
            var source1 = MarkedSource(@"
using System;
using System.Collections.Generic;

class C
{
    static IEnumerable<int> F() 
    {
        <N:0>foreach</N:0> (object item in new[] { 1.0 }) { <N:1>yield return 1;</N:1> }
    }
}");
            // Rude edit but the compiler should handle it.

            var compilation0 = CreateCompilationWithMscorlib461(source0.Tree, options: ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            var compilation1 = compilation0.WithSource(source1.Tree);

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

            using var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);
            var method0 = compilation0.GetMember<MethodSymbol>("C.F");
            var method1 = compilation1.GetMember<MethodSymbol>("C.F");

            var generation0 = CreateInitialBaseline(compilation0, md0, symReader.GetEncMethodDebugInfo);
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, method0, method1, GetSyntaxMapFromMarkers(source0, source1))));

            // Verify delta metadata contains expected rows.
            using var md1 = diff1.GetMetadata();

            // 1 field def added & 3 methods updated
            CheckEncLogDefinitions(md1.Reader,
                Row(3, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                Row(7, TableIndex.Field, EditAndContinueOperation.Default),
                Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(1, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(7, TableIndex.CustomAttribute, EditAndContinueOperation.Default));

            diff1.VerifyIL("C.<F>d__0.System.Collections.IEnumerator.MoveNext", @"
{
  // Code size      161 (0xa1)
  .maxstack  5
  .locals init (int V_0)
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
  IL_0012:  br.s       IL_0018
  IL_0014:  br.s       IL_006b
  IL_0016:  ldc.i4.0
  IL_0017:  ret
  IL_0018:  ldarg.0
  IL_0019:  ldc.i4.m1
  IL_001a:  stfld      ""int C.<F>d__0.<>1__state""
  IL_001f:  nop
  IL_0020:  nop
  IL_0021:  ldarg.0
  IL_0022:  ldc.i4.1
  IL_0023:  newarr     ""double""
  IL_0028:  dup
  IL_0029:  ldc.i4.0
  IL_002a:  ldc.r8     1
  IL_0033:  stelem.r8
  IL_0034:  stfld      ""double[] C.<F>d__0.<>s__4""
  IL_0039:  ldarg.0
  IL_003a:  ldc.i4.0
  IL_003b:  stfld      ""int C.<F>d__0.<>s__2""
  IL_0040:  br.s       IL_0088
  IL_0042:  ldarg.0
  IL_0043:  ldarg.0
  IL_0044:  ldfld      ""double[] C.<F>d__0.<>s__4""
  IL_0049:  ldarg.0
  IL_004a:  ldfld      ""int C.<F>d__0.<>s__2""
  IL_004f:  ldelem.r8
  IL_0050:  box        ""double""
  IL_0055:  stfld      ""object C.<F>d__0.<item>5__3""
  IL_005a:  nop
  IL_005b:  ldarg.0
  IL_005c:  ldc.i4.1
  IL_005d:  stfld      ""int C.<F>d__0.<>2__current""
  IL_0062:  ldarg.0
  IL_0063:  ldc.i4.1
  IL_0064:  stfld      ""int C.<F>d__0.<>1__state""
  IL_0069:  ldc.i4.1
  IL_006a:  ret
  IL_006b:  ldarg.0
  IL_006c:  ldc.i4.m1
  IL_006d:  stfld      ""int C.<F>d__0.<>1__state""
  IL_0072:  nop
  IL_0073:  ldarg.0
  IL_0074:  ldnull
  IL_0075:  stfld      ""object C.<F>d__0.<item>5__3""
  IL_007a:  ldarg.0
  IL_007b:  ldarg.0
  IL_007c:  ldfld      ""int C.<F>d__0.<>s__2""
  IL_0081:  ldc.i4.1
  IL_0082:  add
  IL_0083:  stfld      ""int C.<F>d__0.<>s__2""
  IL_0088:  ldarg.0
  IL_0089:  ldfld      ""int C.<F>d__0.<>s__2""
  IL_008e:  ldarg.0
  IL_008f:  ldfld      ""double[] C.<F>d__0.<>s__4""
  IL_0094:  ldlen
  IL_0095:  conv.i4
  IL_0096:  blt.s      IL_0042
  IL_0098:  ldarg.0
  IL_0099:  ldnull
  IL_009a:  stfld      ""double[] C.<F>d__0.<>s__4""
  IL_009f:  ldc.i4.0
  IL_00a0:  ret
}");
        }

        [Fact]
        public void UpdateIterator_YieldReturn_Add()
        {
            var source0 = MarkedSource(@"
using System.Collections.Generic;

class C
{
    static int M1() => 1;
    static int M2() => 2;
    static int M3() => 3;
    static int M4() => 4;
    static void End() {}

    static IEnumerable<int> F() 
    {
        <N:0>yield return M1();</N:0>
        <N:1>yield return M2();</N:1>
        End();
    }
}");
            var source1 = MarkedSource(@"
using System.Collections.Generic;

class C
{
    static int M1() => 1;
    static int M2() => 2;
    static int M3() => 3;
    static int M4() => 4;
    static void End() {}

    static IEnumerable<int> F() 
    {
        <N:0>yield return M1();</N:0>
        <N:2>yield return M3();</N:2>
        <N:3>yield return M4();</N:3>
        <N:1>yield return M2();</N:1>   
        End();
    }
}");
            var compilation0 = CreateCompilationWithMscorlib461(new[] { source0.Tree }, new[] { SystemCoreRef, CSharpRef }, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);

            var v0 = CompileAndVerify(compilation0);
            v0.VerifyDiagnostics();
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                 generation0,
                 ImmutableArray.Create(
                     SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

            v0.VerifyPdb("C.F", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""F"">
      <customDebugInfo>
        <forwardIterator name=""&lt;F&gt;d__5"" />
        <encStateMachineStateMap>
          <state number=""1"" offset=""16"" />
          <state number=""2"" offset=""55"" />
        </encStateMachineStateMap>
      </customDebugInfo>
    </method>
  </methods>
</symbols>
");
            v0.VerifyPdb("C+<F>d__5.MoveNext", @"
  <symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C+&lt;F&gt;d__5"" name=""MoveNext"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""M1"" />
        <encLocalSlotMap>
          <slot kind=""27"" offset=""0"" />
        </encLocalSlotMap>
      </customDebugInfo>
    </method>
  </methods>
</symbols>
", options: PdbValidationOptions.ExcludeSequencePoints);

            v0.VerifyIL("C.<F>d__5.System.Collections.IEnumerator.MoveNext", @"
 {
  // Code size      105 (0x69)
  .maxstack  2
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<F>d__5.<>1__state""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  switch    (
        IL_001b,
        IL_001d,
        IL_001f)
  IL_0019:  br.s       IL_0021
  IL_001b:  br.s       IL_0023
  IL_001d:  br.s       IL_003f
  IL_001f:  br.s       IL_005a

  IL_0021:  ldc.i4.0
  IL_0022:  ret

  IL_0023:  ldarg.0
  IL_0024:  ldc.i4.m1
  IL_0025:  stfld      ""int C.<F>d__5.<>1__state""
  IL_002a:  nop
  IL_002b:  ldarg.0
  IL_002c:  call       ""int C.M1()""
  IL_0031:  stfld      ""int C.<F>d__5.<>2__current""
  IL_0036:  ldarg.0
  IL_0037:  ldc.i4.1
  IL_0038:  stfld      ""int C.<F>d__5.<>1__state""
  IL_003d:  ldc.i4.1
  IL_003e:  ret

  IL_003f:  ldarg.0
  IL_0040:  ldc.i4.m1
  IL_0041:  stfld      ""int C.<F>d__5.<>1__state""
  IL_0046:  ldarg.0
  IL_0047:  call       ""int C.M2()""
  IL_004c:  stfld      ""int C.<F>d__5.<>2__current""
  IL_0051:  ldarg.0
  IL_0052:  ldc.i4.2
  IL_0053:  stfld      ""int C.<F>d__5.<>1__state""
  IL_0058:  ldc.i4.1
  IL_0059:  ret

  IL_005a:  ldarg.0
  IL_005b:  ldc.i4.m1
  IL_005c:  stfld      ""int C.<F>d__5.<>1__state""
  IL_0061:  call       ""void C.End()""
  IL_0066:  nop
  IL_0067:  ldc.i4.0
  IL_0068:  ret
}");

            diff1.VerifyIL("C.<F>d__5.System.Collections.IEnumerator.MoveNext", @"
{
  // Code size      171 (0xab)
  .maxstack  2
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<F>d__5.<>1__state""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  switch    (
        IL_0023,
        IL_0025,
        IL_0027,
        IL_0029,
        IL_002b)
  IL_0021:  br.s       IL_002d
  IL_0023:  br.s       IL_002f
  IL_0025:  br.s       IL_004b
  IL_0027:  br.s       IL_009c
  IL_0029:  br.s       IL_0066
  IL_002b:  br.s       IL_0081

  IL_002d:  ldc.i4.0
  IL_002e:  ret

  IL_002f:  ldarg.0
  IL_0030:  ldc.i4.m1
  IL_0031:  stfld      ""int C.<F>d__5.<>1__state""
  IL_0036:  nop
  IL_0037:  ldarg.0
  IL_0038:  call       ""int C.M1()""
  IL_003d:  stfld      ""int C.<F>d__5.<>2__current""
  IL_0042:  ldarg.0
  IL_0043:  ldc.i4.1
  IL_0044:  stfld      ""int C.<F>d__5.<>1__state""
  IL_0049:  ldc.i4.1
  IL_004a:  ret

  IL_004b:  ldarg.0
  IL_004c:  ldc.i4.m1
  IL_004d:  stfld      ""int C.<F>d__5.<>1__state""
  IL_0052:  ldarg.0
  IL_0053:  call       ""int C.M3()""
  IL_0058:  stfld      ""int C.<F>d__5.<>2__current""
  IL_005d:  ldarg.0
  IL_005e:  ldc.i4.3
  IL_005f:  stfld      ""int C.<F>d__5.<>1__state""
  IL_0064:  ldc.i4.1
  IL_0065:  ret

  IL_0066:  ldarg.0
  IL_0067:  ldc.i4.m1
  IL_0068:  stfld      ""int C.<F>d__5.<>1__state""
  IL_006d:  ldarg.0
  IL_006e:  call       ""int C.M4()""
  IL_0073:  stfld      ""int C.<F>d__5.<>2__current""
  IL_0078:  ldarg.0
  IL_0079:  ldc.i4.4
  IL_007a:  stfld      ""int C.<F>d__5.<>1__state""
  IL_007f:  ldc.i4.1
  IL_0080:  ret

  IL_0081:  ldarg.0
  IL_0082:  ldc.i4.m1
  IL_0083:  stfld      ""int C.<F>d__5.<>1__state""
  IL_0088:  ldarg.0
  IL_0089:  call       ""int C.M2()""
  IL_008e:  stfld      ""int C.<F>d__5.<>2__current""
  IL_0093:  ldarg.0
  IL_0094:  ldc.i4.2
  IL_0095:  stfld      ""int C.<F>d__5.<>1__state""
  IL_009a:  ldc.i4.1
  IL_009b:  ret

  IL_009c:  ldarg.0
  IL_009d:  ldc.i4.m1
  IL_009e:  stfld      ""int C.<F>d__5.<>1__state""
  IL_00a3:  call       ""void C.End()""
  IL_00a8:  nop
  IL_00a9:  ldc.i4.0
  IL_00aa:  ret
}
");
        }

        [Fact]
        public void UpdateIterator_YieldReturn_Remove()
        {
            var source0 = MarkedSource(@"
using System.Collections.Generic;

class C
{
    static int M1() => 1;
    static int M2() => 2;
    static int M3() => 3;
    static int M4() => 4;
    static void End() {}

    static IEnumerable<int> F() 
    {
        <N:0>yield return M1();</N:0>
        <N:1>yield return M2();</N:1>
        <N:2>yield return M3();</N:2>
        <N:3>yield return M4();</N:3>
        End();
    }
}");
            var source1 = MarkedSource(@"
using System.Collections.Generic;

class C
{
    static int M1() => 1;
    static int M2() => 2;
    static int M3() => 3;
    static int M4() => 4;
    static void End() {}

    static IEnumerable<int> F() 
    {
        <N:1>yield return M2();</N:1>
        <N:2>yield return M3();</N:2>
        End();
    }
}");
            var compilation0 = CreateCompilationWithMscorlib461(new[] { source0.Tree }, new[] { SystemCoreRef, CSharpRef }, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);

            var v0 = CompileAndVerify(compilation0);
            v0.VerifyDiagnostics();
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                 generation0,
                 ImmutableArray.Create(
                     SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

            diff1.VerifyIL("C.<F>d__5.System.Collections.IEnumerator.MoveNext", @"
{
  // Code size      126 (0x7e)
  .maxstack  2
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<F>d__5.<>1__state""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  switch    (
        IL_001f,
        IL_0025,
        IL_0021,
        IL_0023)
  IL_001d:  br.s       IL_0025
  IL_001f:  br.s       IL_0038
  IL_0021:  br.s       IL_0054
  IL_0023:  br.s       IL_006f
  IL_0025:  ldloc.0
  IL_0026:  ldc.i4.1
  IL_0027:  blt.s      IL_0036
  IL_0029:  ldstr      """ + CodeAnalysisResources.EncCannotResumeSuspendedIteratorMethod + @"""
  IL_002e:  ldc.i4.s   -3
  IL_0030:  newobj     ""System.Runtime.CompilerServices.HotReloadException..ctor(string, int)""
  IL_0035:  throw
  IL_0036:  ldc.i4.0
  IL_0037:  ret
  IL_0038:  ldarg.0
  IL_0039:  ldc.i4.m1
  IL_003a:  stfld      ""int C.<F>d__5.<>1__state""
  IL_003f:  nop
  IL_0040:  ldarg.0
  IL_0041:  call       ""int C.M2()""
  IL_0046:  stfld      ""int C.<F>d__5.<>2__current""
  IL_004b:  ldarg.0
  IL_004c:  ldc.i4.2
  IL_004d:  stfld      ""int C.<F>d__5.<>1__state""
  IL_0052:  ldc.i4.1
  IL_0053:  ret
  IL_0054:  ldarg.0
  IL_0055:  ldc.i4.m1
  IL_0056:  stfld      ""int C.<F>d__5.<>1__state""
  IL_005b:  ldarg.0
  IL_005c:  call       ""int C.M3()""
  IL_0061:  stfld      ""int C.<F>d__5.<>2__current""
  IL_0066:  ldarg.0
  IL_0067:  ldc.i4.3
  IL_0068:  stfld      ""int C.<F>d__5.<>1__state""
  IL_006d:  ldc.i4.1
  IL_006e:  ret
  IL_006f:  ldarg.0
  IL_0070:  ldc.i4.m1
  IL_0071:  stfld      ""int C.<F>d__5.<>1__state""
  IL_0076:  call       ""void C.End()""
  IL_007b:  nop
  IL_007c:  ldc.i4.0
  IL_007d:  ret
}
");
        }

        [Fact]
        public void UpdateIterator_YieldReturn_Add_Finally_Try()
        {
            var source0 = MarkedSource(@"
using System.Collections.Generic;

class C
{
    static IEnumerable<int> F() 
    {
        <N:0>yield return M1();</N:0>

        <N:3>try</N:3>
        {
            <N:1>yield return M2();</N:1>
        }
        finally
        {
            Finally1(0);
        }

        <N:2>yield return M3();</N:2>

        End();
    }

    static int M1() => 1;
    static int M2() => 2;
    static int M3() => 3;
    static int M4() => 4;
    static void End() {}
    static void Finally1(int gen) {}
    static void Finally2(int gen) {}
    static void Finally3(int gen) {}
}");
            var source1 = MarkedSource(@"
using System.Collections.Generic;

class C
{
    static IEnumerable<int> F() 
    {
        try
        {
            <N:0>yield return M1();</N:0>
        }
        finally
        {
            Finally2(1);
        }

        <N:3>try</N:3>
        {
            <N:1>yield return M2();</N:1>
            try
            {
                <N:4>yield return M4();</N:4>
            }
            finally 
            {
                Finally3(1);
            }
        }
        finally
        {
            Finally1(1);
        }

        <N:2>yield return M3();</N:2>

        End();
    }

    static int M1() => 1;
    static int M2() => 2;
    static int M3() => 3;
    static int M4() => 4;
    static void End() {}
    static void Finally1(int gen) {}
    static void Finally2(int gen) {}
    static void Finally3(int gen) {}
}");
            var compilation0 = CreateCompilationWithMscorlib461(new[] { source0.Tree }, new[] { SystemCoreRef, CSharpRef }, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);

            var v0 = CompileAndVerify(compilation0);
            v0.VerifyDiagnostics();
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                 generation0,
                 ImmutableArray.Create(
                     SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

            v0.VerifyPdb("C.F", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""F"">
        <customDebugInfo>
        <forwardIterator name=""&lt;F&gt;d__0"" />
        <encStateMachineStateMap>
            <state number=""1"" offset=""16"" />
            <state number=""-3"" offset=""57"" />
            <state number=""2"" offset=""96"" />
            <state number=""3"" offset=""213"" />
        </encStateMachineStateMap>
        </customDebugInfo>
    </method>
  </methods>
</symbols>");

            diff1.VerifySynthesizedMembers(
               "C: {<F>d__0}",
               "C.<F>d__0: {" + string.Join(", ", new[]
               {
                    "<>1__state",
                    "<>2__current",
                    "<>l__initialThreadId",
                    "System.IDisposable.Dispose",
                    "MoveNext",
                    "<>m__Finally2",
                    "<>m__Finally1",
                    "<>m__Finally3",
                    "System.Collections.Generic.IEnumerator<System.Int32>.get_Current",
                    "System.Collections.IEnumerator.Reset, System.Collections.IEnumerator.get_Current",
                    "System.Collections.Generic.IEnumerable<System.Int32>.GetEnumerator",
                    "System.Collections.IEnumerable.GetEnumerator",
                    "System.Collections.Generic.IEnumerator<System.Int32>.Current",
                    "System.Collections.IEnumerator.Current"
               }) + "}");

            diff1.VerifyIL("C.<F>d__0.<>m__Finally1", @"
{
  // Code size       17 (0x11)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.m1
  IL_0002:  stfld      ""int C.<F>d__0.<>1__state""
  IL_0007:  nop
  IL_0008:  ldc.i4.1
  IL_0009:  call       ""void C.Finally1(int)""
  IL_000e:  nop
  IL_000f:  nop
  IL_0010:  ret
}
");
            diff1.VerifyIL("C.<F>d__0.<>m__Finally2", @"
{
  // Code size       17 (0x11)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.m1
  IL_0002:  stfld      ""int C.<F>d__0.<>1__state""
  IL_0007:  nop
  IL_0008:  ldc.i4.1
  IL_0009:  call       ""void C.Finally2(int)""
  IL_000e:  nop
  IL_000f:  nop
  IL_0010:  ret
}
");
            diff1.VerifyIL("C.<F>d__0.<>m__Finally3", @"
{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.s   -3
  IL_0003:  stfld      ""int C.<F>d__0.<>1__state""
  IL_0008:  nop
  IL_0009:  ldc.i4.1
  IL_000a:  call       ""void C.Finally3(int)""
  IL_000f:  nop
  IL_0010:  nop
  IL_0011:  ret
}
");

            v0.VerifyIL("C.<F>d__0.System.IDisposable.Dispose", @"
{
  // Code size       33 (0x21)
  .maxstack  2
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<F>d__0.<>1__state""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  ldc.i4.s   -3
  IL_000a:  beq.s      IL_0014
  IL_000c:  br.s       IL_000e
  IL_000e:  ldloc.0
  IL_000f:  ldc.i4.2
  IL_0010:  beq.s      IL_0014
  IL_0012:  br.s       IL_0020
  IL_0014:  nop
  .try
  {
    IL_0015:  leave.s    IL_001e
  }
  finally
  {
    IL_0017:  ldarg.0
    IL_0018:  call       ""void C.<F>d__0.<>m__Finally1()""
    IL_001d:  endfinally
  }
  IL_001e:  br.s       IL_0020
  IL_0020:  ret
}
");
            diff1.VerifyIL("C.<F>d__0.System.IDisposable.Dispose", @"
{
  // Code size      108 (0x6c)
  .maxstack  2
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<F>d__0.<>1__state""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  ldc.i4.s   -5
  IL_000a:  sub
  IL_000b:  switch    (
        IL_0046,
        IL_003a,
        IL_0046,
        IL_006b,
        IL_006b,
        IL_006b,
        IL_003a,
        IL_0046,
        IL_006b,
        IL_0046)
  IL_0038:  br.s       IL_006b
  IL_003a:  nop
  .try
  {
    IL_003b:  leave.s    IL_0044
  }
  finally
  {
    IL_003d:  ldarg.0
    IL_003e:  call       ""void C.<F>d__0.<>m__Finally2()""
    IL_0043:  endfinally
  }
  IL_0044:  br.s       IL_006b
  IL_0046:  nop
  .try
  {
    IL_0047:  ldloc.0
    IL_0048:  ldc.i4.s   -5
    IL_004a:  beq.s      IL_0054
    IL_004c:  br.s       IL_004e
    IL_004e:  ldloc.0
    IL_004f:  ldc.i4.4
    IL_0050:  beq.s      IL_0054
    IL_0052:  br.s       IL_0060
    IL_0054:  nop
    .try
    {
      IL_0055:  leave.s    IL_005e
    }
    finally
    {
      IL_0057:  ldarg.0
      IL_0058:  call       ""void C.<F>d__0.<>m__Finally3()""
      IL_005d:  endfinally
    }
    IL_005e:  br.s       IL_0060
    IL_0060:  leave.s    IL_0069
  }
  finally
  {
    IL_0062:  ldarg.0
    IL_0063:  call       ""void C.<F>d__0.<>m__Finally1()""
    IL_0068:  endfinally
  }
  IL_0069:  br.s       IL_006b
  IL_006b:  ret
}
");

            v0.VerifyIL("C.<F>d__0.System.Collections.IEnumerator.MoveNext", @"
{
  // Code size      179 (0xb3)
  .maxstack  2
  .locals init (bool V_0,
                int V_1)
  .try
  {
    IL_0000:  ldarg.0
    IL_0001:  ldfld      ""int C.<F>d__0.<>1__state""
    IL_0006:  stloc.1
    IL_0007:  ldloc.1
    IL_0008:  switch    (
        IL_001f,
        IL_0021,
        IL_0023,
        IL_0025)
    IL_001d:  br.s       IL_0027
    IL_001f:  br.s       IL_002e
    IL_0021:  br.s       IL_004c
    IL_0023:  br.s       IL_0072
    IL_0025:  br.s       IL_0098
    IL_0027:  ldc.i4.0
    IL_0028:  stloc.0
    IL_0029:  leave      IL_00b1
    IL_002e:  ldarg.0
    IL_002f:  ldc.i4.m1
    IL_0030:  stfld      ""int C.<F>d__0.<>1__state""
    IL_0035:  nop
    IL_0036:  ldarg.0
    IL_0037:  call       ""int C.M1()""
    IL_003c:  stfld      ""int C.<F>d__0.<>2__current""
    IL_0041:  ldarg.0
    IL_0042:  ldc.i4.1
    IL_0043:  stfld      ""int C.<F>d__0.<>1__state""
    IL_0048:  ldc.i4.1
    IL_0049:  stloc.0
    IL_004a:  leave.s    IL_00b1
    IL_004c:  ldarg.0
    IL_004d:  ldc.i4.m1
    IL_004e:  stfld      ""int C.<F>d__0.<>1__state""
    IL_0053:  ldarg.0
    IL_0054:  ldc.i4.s   -3
    IL_0056:  stfld      ""int C.<F>d__0.<>1__state""
    IL_005b:  nop
    IL_005c:  ldarg.0
    IL_005d:  call       ""int C.M2()""
    IL_0062:  stfld      ""int C.<F>d__0.<>2__current""
    IL_0067:  ldarg.0
    IL_0068:  ldc.i4.2
    IL_0069:  stfld      ""int C.<F>d__0.<>1__state""
    IL_006e:  ldc.i4.1
    IL_006f:  stloc.0
    IL_0070:  leave.s    IL_00b1
    IL_0072:  ldarg.0
    IL_0073:  ldc.i4.s   -3
    IL_0075:  stfld      ""int C.<F>d__0.<>1__state""
    IL_007a:  nop
    IL_007b:  ldarg.0
    IL_007c:  call       ""void C.<F>d__0.<>m__Finally1()""
    IL_0081:  nop
    IL_0082:  ldarg.0
    IL_0083:  call       ""int C.M3()""
    IL_0088:  stfld      ""int C.<F>d__0.<>2__current""
    IL_008d:  ldarg.0
    IL_008e:  ldc.i4.3
    IL_008f:  stfld      ""int C.<F>d__0.<>1__state""
    IL_0094:  ldc.i4.1
    IL_0095:  stloc.0
    IL_0096:  leave.s    IL_00b1
    IL_0098:  ldarg.0
    IL_0099:  ldc.i4.m1
    IL_009a:  stfld      ""int C.<F>d__0.<>1__state""
    IL_009f:  call       ""void C.End()""
    IL_00a4:  nop
    IL_00a5:  ldc.i4.0
    IL_00a6:  stloc.0
    IL_00a7:  leave.s    IL_00b1
  }
  fault
  {
    IL_00a9:  ldarg.0
    IL_00aa:  call       ""void C.<F>d__0.Dispose()""
    IL_00af:  nop
    IL_00b0:  endfinally
  }
  IL_00b1:  ldloc.0
  IL_00b2:  ret
} 
");

            diff1.VerifyIL("C.<F>d__0.System.Collections.IEnumerator.MoveNext", @"
{
  // Code size      259 (0x103)
  .maxstack  2
  .locals init (bool V_0,
                int V_1)
  .try
  {
    IL_0000:  ldarg.0
    IL_0001:  ldfld      ""int C.<F>d__0.<>1__state""
    IL_0006:  stloc.1
    IL_0007:  ldloc.1
    IL_0008:  switch    (
        IL_0023,
        IL_0025,
        IL_0027,
        IL_0029,
        IL_002e)
    IL_0021:  br.s       IL_0033
    IL_0023:  br.s       IL_003a
    IL_0025:  br.s       IL_0064
    IL_0027:  br.s       IL_0093
    IL_0029:  br         IL_00e8
    IL_002e:  br         IL_00ba
    IL_0033:  ldc.i4.0
    IL_0034:  stloc.0
    IL_0035:  leave      IL_0101
    IL_003a:  ldarg.0
    IL_003b:  ldc.i4.m1
    IL_003c:  stfld      ""int C.<F>d__0.<>1__state""
    IL_0041:  nop
    IL_0042:  ldarg.0
    IL_0043:  ldc.i4.s   -4
    IL_0045:  stfld      ""int C.<F>d__0.<>1__state""
    IL_004a:  nop
    IL_004b:  ldarg.0
    IL_004c:  call       ""int C.M1()""
    IL_0051:  stfld      ""int C.<F>d__0.<>2__current""
    IL_0056:  ldarg.0
    IL_0057:  ldc.i4.1
    IL_0058:  stfld      ""int C.<F>d__0.<>1__state""
    IL_005d:  ldc.i4.1
    IL_005e:  stloc.0
    IL_005f:  leave      IL_0101
    IL_0064:  ldarg.0
    IL_0065:  ldc.i4.s   -4
    IL_0067:  stfld      ""int C.<F>d__0.<>1__state""
    IL_006c:  nop
    IL_006d:  ldarg.0
    IL_006e:  call       ""void C.<F>d__0.<>m__Finally2()""
    IL_0073:  nop
    IL_0074:  ldarg.0
    IL_0075:  ldc.i4.s   -3
    IL_0077:  stfld      ""int C.<F>d__0.<>1__state""
    IL_007c:  nop
    IL_007d:  ldarg.0
    IL_007e:  call       ""int C.M2()""
    IL_0083:  stfld      ""int C.<F>d__0.<>2__current""
    IL_0088:  ldarg.0
    IL_0089:  ldc.i4.2
    IL_008a:  stfld      ""int C.<F>d__0.<>1__state""
    IL_008f:  ldc.i4.1
    IL_0090:  stloc.0
    IL_0091:  leave.s    IL_0101
    IL_0093:  ldarg.0
    IL_0094:  ldc.i4.s   -3
    IL_0096:  stfld      ""int C.<F>d__0.<>1__state""
    IL_009b:  ldarg.0
    IL_009c:  ldc.i4.s   -5
    IL_009e:  stfld      ""int C.<F>d__0.<>1__state""
    IL_00a3:  nop
    IL_00a4:  ldarg.0
    IL_00a5:  call       ""int C.M4()""
    IL_00aa:  stfld      ""int C.<F>d__0.<>2__current""
    IL_00af:  ldarg.0
    IL_00b0:  ldc.i4.4
    IL_00b1:  stfld      ""int C.<F>d__0.<>1__state""
    IL_00b6:  ldc.i4.1
    IL_00b7:  stloc.0
    IL_00b8:  leave.s    IL_0101
    IL_00ba:  ldarg.0
    IL_00bb:  ldc.i4.s   -5
    IL_00bd:  stfld      ""int C.<F>d__0.<>1__state""
    IL_00c2:  nop
    IL_00c3:  ldarg.0
    IL_00c4:  call       ""void C.<F>d__0.<>m__Finally3()""
    IL_00c9:  nop
    IL_00ca:  nop
    IL_00cb:  ldarg.0
    IL_00cc:  call       ""void C.<F>d__0.<>m__Finally1()""
    IL_00d1:  nop
    IL_00d2:  ldarg.0
    IL_00d3:  call       ""int C.M3()""
    IL_00d8:  stfld      ""int C.<F>d__0.<>2__current""
    IL_00dd:  ldarg.0
    IL_00de:  ldc.i4.3
    IL_00df:  stfld      ""int C.<F>d__0.<>1__state""
    IL_00e4:  ldc.i4.1
    IL_00e5:  stloc.0
    IL_00e6:  leave.s    IL_0101
    IL_00e8:  ldarg.0
    IL_00e9:  ldc.i4.m1
    IL_00ea:  stfld      ""int C.<F>d__0.<>1__state""
    IL_00ef:  call       ""void C.End()""
    IL_00f4:  nop
    IL_00f5:  ldc.i4.0
    IL_00f6:  stloc.0
    IL_00f7:  leave.s    IL_0101
  }
  fault
  {
    IL_00f9:  ldarg.0
    IL_00fa:  call       ""void C.<F>d__0.Dispose()""
    IL_00ff:  nop
    IL_0100:  endfinally
  }
  IL_0101:  ldloc.0
  IL_0102:  ret
}");
        }

        [Fact]
        public void UpdateIterator_YieldReturn_Add_Finally_UsingDeclaration()
        {
            var source0 = MarkedSource(@"
using System;
using System.Collections.Generic;

class C
{
    static IEnumerable<int> F() 
    {
        using var <N:0>x = M1()</N:0>;
        <N:1>yield return M2();</N:1>
        End();
    }

    static IDisposable M1() => null;
    static int M2() => 0;
    static int M3() => 0;
    static void End() {}
}");
            var source1 = MarkedSource(@"
using System;
using System.Collections.Generic;

class C
{
    static IEnumerable<int> F() 
    {
        using var <N:0>x = M1()</N:0>;
        <N:1>yield return M2();</N:1>
        yield return M3();
        End();
    }

    static IDisposable M1() => null;
    static int M2() => 0;
    static int M3() => 0;
    static void End() {}
}");
            var compilation0 = CreateCompilationWithMscorlib461(new[] { source0.Tree }, new[] { SystemCoreRef, CSharpRef }, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);

            var v0 = CompileAndVerify(compilation0);
            v0.VerifyDiagnostics();
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                 generation0,
                 ImmutableArray.Create(
                     SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

            v0.VerifyPdb("C.F", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""F"">
      <customDebugInfo>
        <forwardIterator name=""&lt;F&gt;d__0"" />
        <encLocalSlotMap>
          <slot kind=""0"" offset=""26"" />
        </encLocalSlotMap>
        <encStateMachineStateMap>
          <state number=""-3"" offset=""26"" />
          <state number=""1"" offset=""56"" />
        </encStateMachineStateMap>
      </customDebugInfo>
    </method>
  </methods>
</symbols>");

            diff1.VerifySynthesizedMembers(
               "C: {<F>d__0}",
               "C.<F>d__0: {" + string.Join(", ", new[]
               {
                    "<>1__state",
                    "<>2__current",
                    "<>l__initialThreadId",
                    "<x>5__1",
                    "System.IDisposable.Dispose",
                    "MoveNext",
                    "<>m__Finally1",
                    "System.Collections.Generic.IEnumerator<System.Int32>.get_Current",
                    "System.Collections.IEnumerator.Reset, System.Collections.IEnumerator.get_Current",
                    "System.Collections.Generic.IEnumerable<System.Int32>.GetEnumerator",
                    "System.Collections.IEnumerable.GetEnumerator",
                    "System.Collections.Generic.IEnumerator<System.Int32>.Current",
                    "System.Collections.IEnumerator.Current"
               }) + "}");

            diff1.VerifyIL("C.<F>d__0.<>m__Finally1", @"
{
  // Code size       28 (0x1c)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.m1
  IL_0002:  stfld      ""int C.<F>d__0.<>1__state""
  IL_0007:  ldarg.0
  IL_0008:  ldfld      ""System.IDisposable C.<F>d__0.<x>5__1""
  IL_000d:  brfalse.s  IL_001b
  IL_000f:  ldarg.0
  IL_0010:  ldfld      ""System.IDisposable C.<F>d__0.<x>5__1""
  IL_0015:  callvirt   ""void System.IDisposable.Dispose()""
  IL_001a:  nop
  IL_001b:  ret
}
");

            v0.VerifyIL("C.<F>d__0.System.IDisposable.Dispose", @"
{
  // Code size       33 (0x21)
  .maxstack  2
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<F>d__0.<>1__state""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  ldc.i4.s   -3
  IL_000a:  beq.s      IL_0014
  IL_000c:  br.s       IL_000e
  IL_000e:  ldloc.0
  IL_000f:  ldc.i4.1
  IL_0010:  beq.s      IL_0014
  IL_0012:  br.s       IL_0020
  IL_0014:  nop
  .try
  {
    IL_0015:  leave.s    IL_001e
  }
  finally
  {
    IL_0017:  ldarg.0
    IL_0018:  call       ""void C.<F>d__0.<>m__Finally1()""
    IL_001d:  endfinally
  }
  IL_001e:  br.s       IL_0020
  IL_0020:  ret
}
");
            diff1.VerifyIL("C.<F>d__0.System.IDisposable.Dispose", @"
{
  // Code size       35 (0x23)
  .maxstack  2
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<F>d__0.<>1__state""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  ldc.i4.s   -3
  IL_000a:  beq.s      IL_0016
  IL_000c:  br.s       IL_000e
  IL_000e:  ldloc.0
  IL_000f:  ldc.i4.1
  IL_0010:  sub
  IL_0011:  ldc.i4.1
  IL_0012:  ble.un.s   IL_0016
  IL_0014:  br.s       IL_0022
  IL_0016:  nop
  .try
  {
    IL_0017:  leave.s    IL_0020
  }
  finally
  {
    IL_0019:  ldarg.0
    IL_001a:  call       ""void C.<F>d__0.<>m__Finally1()""
    IL_001f:  endfinally
  }
  IL_0020:  br.s       IL_0022
  IL_0022:  ret
}
");

            v0.VerifyIL("C.<F>d__0.System.Collections.IEnumerator.MoveNext", @"
 {
  // Code size      112 (0x70)
  .maxstack  2
  .locals init (bool V_0,
                int V_1)
  .try
  {
    IL_0000:  ldarg.0
    IL_0001:  ldfld      ""int C.<F>d__0.<>1__state""
    IL_0006:  stloc.1
    IL_0007:  ldloc.1
    IL_0008:  brfalse.s  IL_0012
    IL_000a:  br.s       IL_000c
    IL_000c:  ldloc.1
    IL_000d:  ldc.i4.1
    IL_000e:  beq.s      IL_0014
    IL_0010:  br.s       IL_0016
    IL_0012:  br.s       IL_001a
    IL_0014:  br.s       IL_004b
    IL_0016:  ldc.i4.0
    IL_0017:  stloc.0
    IL_0018:  leave.s    IL_006e
    IL_001a:  ldarg.0
    IL_001b:  ldc.i4.m1
    IL_001c:  stfld      ""int C.<F>d__0.<>1__state""
    IL_0021:  nop
    IL_0022:  ldarg.0
    IL_0023:  call       ""System.IDisposable C.M1()""
    IL_0028:  stfld      ""System.IDisposable C.<F>d__0.<x>5__1""
    IL_002d:  ldarg.0
    IL_002e:  ldc.i4.s   -3
    IL_0030:  stfld      ""int C.<F>d__0.<>1__state""
    IL_0035:  ldarg.0
    IL_0036:  call       ""int C.M2()""
    IL_003b:  stfld      ""int C.<F>d__0.<>2__current""
    IL_0040:  ldarg.0
    IL_0041:  ldc.i4.1
    IL_0042:  stfld      ""int C.<F>d__0.<>1__state""
    IL_0047:  ldc.i4.1
    IL_0048:  stloc.0
    IL_0049:  leave.s    IL_006e
    IL_004b:  ldarg.0
    IL_004c:  ldc.i4.s   -3
    IL_004e:  stfld      ""int C.<F>d__0.<>1__state""
    IL_0053:  call       ""void C.End()""
    IL_0058:  nop
    IL_0059:  ldc.i4.0
    IL_005a:  stloc.0
    IL_005b:  br.s       IL_005d
    IL_005d:  ldarg.0
    IL_005e:  call       ""void C.<F>d__0.<>m__Finally1()""
    IL_0063:  nop
    IL_0064:  leave.s    IL_006e
  }
  fault
  {
    IL_0066:  ldarg.0
    IL_0067:  call       ""void C.<F>d__0.Dispose()""
    IL_006c:  nop
    IL_006d:  endfinally
  }
  IL_006e:  ldloc.0
  IL_006f:  ret
}
");

            diff1.VerifyIL("C.<F>d__0.System.Collections.IEnumerator.MoveNext", @"
{
  // Code size      153 (0x99)
  .maxstack  2
  .locals init (bool V_0,
                int V_1)
  .try
  {
    IL_0000:  ldarg.0
    IL_0001:  ldfld      ""int C.<F>d__0.<>1__state""
    IL_0006:  stloc.1
    IL_0007:  ldloc.1
    IL_0008:  switch    (
        IL_001b,
        IL_001d,
        IL_001f)
    IL_0019:  br.s       IL_0021
    IL_001b:  br.s       IL_0025
    IL_001d:  br.s       IL_0056
    IL_001f:  br.s       IL_0074
    IL_0021:  ldc.i4.0
    IL_0022:  stloc.0
    IL_0023:  leave.s    IL_0097
    IL_0025:  ldarg.0
    IL_0026:  ldc.i4.m1
    IL_0027:  stfld      ""int C.<F>d__0.<>1__state""
    IL_002c:  nop
    IL_002d:  ldarg.0
    IL_002e:  call       ""System.IDisposable C.M1()""
    IL_0033:  stfld      ""System.IDisposable C.<F>d__0.<x>5__1""
    IL_0038:  ldarg.0
    IL_0039:  ldc.i4.s   -3
    IL_003b:  stfld      ""int C.<F>d__0.<>1__state""
    IL_0040:  ldarg.0
    IL_0041:  call       ""int C.M2()""
    IL_0046:  stfld      ""int C.<F>d__0.<>2__current""
    IL_004b:  ldarg.0
    IL_004c:  ldc.i4.1
    IL_004d:  stfld      ""int C.<F>d__0.<>1__state""
    IL_0052:  ldc.i4.1
    IL_0053:  stloc.0
    IL_0054:  leave.s    IL_0097
    IL_0056:  ldarg.0
    IL_0057:  ldc.i4.s   -3
    IL_0059:  stfld      ""int C.<F>d__0.<>1__state""
    IL_005e:  ldarg.0
    IL_005f:  call       ""int C.M3()""
    IL_0064:  stfld      ""int C.<F>d__0.<>2__current""
    IL_0069:  ldarg.0
    IL_006a:  ldc.i4.2
    IL_006b:  stfld      ""int C.<F>d__0.<>1__state""
    IL_0070:  ldc.i4.1
    IL_0071:  stloc.0
    IL_0072:  leave.s    IL_0097
    IL_0074:  ldarg.0
    IL_0075:  ldc.i4.s   -3
    IL_0077:  stfld      ""int C.<F>d__0.<>1__state""
    IL_007c:  call       ""void C.End()""
    IL_0081:  nop
    IL_0082:  ldc.i4.0
    IL_0083:  stloc.0
    IL_0084:  br.s       IL_0086
    IL_0086:  ldarg.0
    IL_0087:  call       ""void C.<F>d__0.<>m__Finally1()""
    IL_008c:  nop
    IL_008d:  leave.s    IL_0097
  }
  fault
  {
    IL_008f:  ldarg.0
    IL_0090:  call       ""void C.<F>d__0.Dispose()""
    IL_0095:  nop
    IL_0096:  endfinally
  }
  IL_0097:  ldloc.0
  IL_0098:  ret
}");
        }

        [Fact]
        public void UpdateIterator_YieldReturn_Add_Finally_Foreach_ForEachVar_Using_Lock()
        {
            var source0 = MarkedSource(@"
using System;
using System.Collections.Generic;

class C
{
    static IEnumerable<int> F() 
    {
        using IDisposable <N:0>x1 = D()</N:0>, <N:1>x2 = D()</N:1>;

        <N:2>using (D())</N:2>
            using (IDisposable <N:3>y1 = D()</N:3>, <N:4>y2 = D()</N:4>)
                <N:5>foreach</N:5> (var z in E())
                    <N:6>foreach</N:6> (var (u, w) in E())
                        <N:7>lock</N:7> (D())
                        {
                            <N:8>yield return 1;</N:8>
                        }
    }

    static IDisposable D() => null;
    static IEnumerable<(int, int)> E() => null;
}");
            var source1 = MarkedSource(@"
using System;
using System.Collections.Generic;

class C
{
    static IEnumerable<int> F() 
    {
        using IDisposable <N:0>x1 = D()</N:0>, <N:1>x2 = D()</N:1>;

        <N:2>using (D())</N:2>
            using (IDisposable <N:3>y1 = D()</N:3>, <N:4>y2 = D()</N:4>)
                <N:5>foreach</N:5> (var z in E())
                    <N:6>foreach</N:6> (var (u, w) in E())
                        <N:7>lock</N:7> (D())
                        {
                            <N:8>yield return 1;</N:8>
                            yield return 2;
                        }
    }

    static IDisposable D() => null;
    static IEnumerable<(int, int)> E() => null;
}");
            var compilation0 = CreateCompilationWithMscorlib461(new[] { source0.Tree }, new[] { SystemRuntimeFacadeRef, ValueTupleRef }, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);

            var v0 = CompileAndVerify(compilation0);
            v0.VerifyDiagnostics();
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                 generation0,
                 ImmutableArray.Create(
                     SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

            v0.VerifyPdb("C.F", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""F"">
      <customDebugInfo>
        <forwardIterator name=""&lt;F&gt;d__0"" />
        <encLocalSlotMap>
          <slot kind=""0"" offset=""34"" />
          <slot kind=""0"" offset=""55"" />
          <slot kind=""4"" offset=""87"" />
          <slot kind=""0"" offset=""142"" />
          <slot kind=""0"" offset=""163"" />
          <slot kind=""5"" offset=""201"" />
          <slot kind=""0"" offset=""201"" />
          <slot kind=""5"" offset=""256"" />
          <slot kind=""0"" offset=""276"" />
          <slot kind=""0"" offset=""279"" />
          <slot kind=""3"" offset=""320"" />
          <slot kind=""2"" offset=""320"" />
        </encLocalSlotMap>
        <encStateMachineStateMap>
          <state number=""-3"" offset=""34"" />
          <state number=""-4"" offset=""55"" />
          <state number=""-5"" offset=""87"" />
          <state number=""-6"" offset=""142"" />
          <state number=""-7"" offset=""163"" />
          <state number=""-8"" offset=""201"" />
          <state number=""-9"" offset=""256"" />
          <state number=""-10"" offset=""320"" />
          <state number=""1"" offset=""398"" />
        </encStateMachineStateMap>
      </customDebugInfo>
    </method>
  </methods>
</symbols>");

            diff1.VerifyIL("C.<F>d__0.System.Collections.IEnumerator.MoveNext", @"
{
  // Code size      526 (0x20e)
  .maxstack  2
  .locals init (bool V_0,
                int V_1,
                System.ValueTuple<int, int> V_2)
  .try
  {
    IL_0000:  ldarg.0
    IL_0001:  ldfld      ""int C.<F>d__0.<>1__state""
    IL_0006:  stloc.1
    IL_0007:  ldloc.1
    IL_0008:  switch    (
        IL_001b,
        IL_001d,
        IL_0022)
    IL_0019:  br.s       IL_0027
    IL_001b:  br.s       IL_002e
    IL_001d:  br         IL_0148
    IL_0022:  br         IL_0165
    IL_0027:  ldc.i4.0
    IL_0028:  stloc.0
    IL_0029:  leave      IL_020c
    IL_002e:  ldarg.0
    IL_002f:  ldc.i4.m1
    IL_0030:  stfld      ""int C.<F>d__0.<>1__state""
    IL_0035:  nop
    IL_0036:  ldarg.0
    IL_0037:  call       ""System.IDisposable C.D()""
    IL_003c:  stfld      ""System.IDisposable C.<F>d__0.<x1>5__1""
    IL_0041:  ldarg.0
    IL_0042:  ldc.i4.s   -3
    IL_0044:  stfld      ""int C.<F>d__0.<>1__state""
    IL_0049:  ldarg.0
    IL_004a:  call       ""System.IDisposable C.D()""
    IL_004f:  stfld      ""System.IDisposable C.<F>d__0.<x2>5__2""
    IL_0054:  ldarg.0
    IL_0055:  ldc.i4.s   -4
    IL_0057:  stfld      ""int C.<F>d__0.<>1__state""
    IL_005c:  ldarg.0
    IL_005d:  call       ""System.IDisposable C.D()""
    IL_0062:  stfld      ""System.IDisposable C.<F>d__0.<>s__3""
    IL_0067:  ldarg.0
    IL_0068:  ldc.i4.s   -5
    IL_006a:  stfld      ""int C.<F>d__0.<>1__state""
    IL_006f:  ldarg.0
    IL_0070:  call       ""System.IDisposable C.D()""
    IL_0075:  stfld      ""System.IDisposable C.<F>d__0.<y1>5__4""
    IL_007a:  ldarg.0
    IL_007b:  ldc.i4.s   -6
    IL_007d:  stfld      ""int C.<F>d__0.<>1__state""
    IL_0082:  ldarg.0
    IL_0083:  call       ""System.IDisposable C.D()""
    IL_0088:  stfld      ""System.IDisposable C.<F>d__0.<y2>5__5""
    IL_008d:  ldarg.0
    IL_008e:  ldc.i4.s   -7
    IL_0090:  stfld      ""int C.<F>d__0.<>1__state""
    IL_0095:  nop
    IL_0096:  ldarg.0
    IL_0097:  call       ""System.Collections.Generic.IEnumerable<System.ValueTuple<int, int>> C.E()""
    IL_009c:  callvirt   ""System.Collections.Generic.IEnumerator<System.ValueTuple<int, int>> System.Collections.Generic.IEnumerable<System.ValueTuple<int, int>>.GetEnumerator()""
    IL_00a1:  stfld      ""System.Collections.Generic.IEnumerator<System.ValueTuple<int, int>> C.<F>d__0.<>s__6""
    IL_00a6:  ldarg.0
    IL_00a7:  ldc.i4.s   -8
    IL_00a9:  stfld      ""int C.<F>d__0.<>1__state""
    IL_00ae:  br         IL_01a6
    IL_00b3:  ldarg.0
    IL_00b4:  ldarg.0
    IL_00b5:  ldfld      ""System.Collections.Generic.IEnumerator<System.ValueTuple<int, int>> C.<F>d__0.<>s__6""
    IL_00ba:  callvirt   ""System.ValueTuple<int, int> System.Collections.Generic.IEnumerator<System.ValueTuple<int, int>>.Current.get""
    IL_00bf:  stfld      ""System.ValueTuple<int, int> C.<F>d__0.<z>5__7""
    IL_00c4:  nop
    IL_00c5:  ldarg.0
    IL_00c6:  call       ""System.Collections.Generic.IEnumerable<System.ValueTuple<int, int>> C.E()""
    IL_00cb:  callvirt   ""System.Collections.Generic.IEnumerator<System.ValueTuple<int, int>> System.Collections.Generic.IEnumerable<System.ValueTuple<int, int>>.GetEnumerator()""
    IL_00d0:  stfld      ""System.Collections.Generic.IEnumerator<System.ValueTuple<int, int>> C.<F>d__0.<>s__8""
    IL_00d5:  ldarg.0
    IL_00d6:  ldc.i4.s   -9
    IL_00d8:  stfld      ""int C.<F>d__0.<>1__state""
    IL_00dd:  br         IL_017c
    IL_00e2:  ldarg.0
    IL_00e3:  ldfld      ""System.Collections.Generic.IEnumerator<System.ValueTuple<int, int>> C.<F>d__0.<>s__8""
    IL_00e8:  callvirt   ""System.ValueTuple<int, int> System.Collections.Generic.IEnumerator<System.ValueTuple<int, int>>.Current.get""
    IL_00ed:  stloc.2
    IL_00ee:  ldarg.0
    IL_00ef:  ldloc.2
    IL_00f0:  ldfld      ""int System.ValueTuple<int, int>.Item1""
    IL_00f5:  stfld      ""int C.<F>d__0.<u>5__13""
    IL_00fa:  ldarg.0
    IL_00fb:  ldloc.2
    IL_00fc:  ldfld      ""int System.ValueTuple<int, int>.Item2""
    IL_0101:  stfld      ""int C.<F>d__0.<w>5__14""
    IL_0106:  ldarg.0
    IL_0107:  call       ""System.IDisposable C.D()""
    IL_010c:  stfld      ""System.IDisposable C.<F>d__0.<>s__11""
    IL_0111:  ldarg.0
    IL_0112:  ldc.i4.0
    IL_0113:  stfld      ""bool C.<F>d__0.<>s__12""
    IL_0118:  ldarg.0
    IL_0119:  ldc.i4.s   -10
    IL_011b:  stfld      ""int C.<F>d__0.<>1__state""
    IL_0120:  ldarg.0
    IL_0121:  ldfld      ""System.IDisposable C.<F>d__0.<>s__11""
    IL_0126:  ldarg.0
    IL_0127:  ldflda     ""bool C.<F>d__0.<>s__12""
    IL_012c:  call       ""void System.Threading.Monitor.Enter(object, ref bool)""
    IL_0131:  nop
    IL_0132:  nop
    IL_0133:  ldarg.0
    IL_0134:  ldc.i4.1
    IL_0135:  stfld      ""int C.<F>d__0.<>2__current""
    IL_013a:  ldarg.0
    IL_013b:  ldc.i4.1
    IL_013c:  stfld      ""int C.<F>d__0.<>1__state""
    IL_0141:  ldc.i4.1
    IL_0142:  stloc.0
    IL_0143:  leave      IL_020c
    IL_0148:  ldarg.0
    IL_0149:  ldc.i4.s   -10
    IL_014b:  stfld      ""int C.<F>d__0.<>1__state""
    IL_0150:  ldarg.0
    IL_0151:  ldc.i4.2
    IL_0152:  stfld      ""int C.<F>d__0.<>2__current""
    IL_0157:  ldarg.0
    IL_0158:  ldc.i4.2
    IL_0159:  stfld      ""int C.<F>d__0.<>1__state""
    IL_015e:  ldc.i4.1
    IL_015f:  stloc.0
    IL_0160:  leave      IL_020c
    IL_0165:  ldarg.0
    IL_0166:  ldc.i4.s   -10
    IL_0168:  stfld      ""int C.<F>d__0.<>1__state""
    IL_016d:  nop
    IL_016e:  ldarg.0
    IL_016f:  call       ""void C.<F>d__0.<>m__Finally8()""
    IL_0174:  nop
    IL_0175:  ldarg.0
    IL_0176:  ldnull
    IL_0177:  stfld      ""System.IDisposable C.<F>d__0.<>s__11""
    IL_017c:  ldarg.0
    IL_017d:  ldfld      ""System.Collections.Generic.IEnumerator<System.ValueTuple<int, int>> C.<F>d__0.<>s__8""
    IL_0182:  callvirt   ""bool System.Collections.IEnumerator.MoveNext()""
    IL_0187:  brtrue     IL_00e2
    IL_018c:  ldarg.0
    IL_018d:  call       ""void C.<F>d__0.<>m__Finally7()""
    IL_0192:  nop
    IL_0193:  ldarg.0
    IL_0194:  ldnull
    IL_0195:  stfld      ""System.Collections.Generic.IEnumerator<System.ValueTuple<int, int>> C.<F>d__0.<>s__8""
    IL_019a:  ldarg.0
    IL_019b:  ldflda     ""System.ValueTuple<int, int> C.<F>d__0.<z>5__7""
    IL_01a0:  initobj    ""System.ValueTuple<int, int>""
    IL_01a6:  ldarg.0
    IL_01a7:  ldfld      ""System.Collections.Generic.IEnumerator<System.ValueTuple<int, int>> C.<F>d__0.<>s__6""
    IL_01ac:  callvirt   ""bool System.Collections.IEnumerator.MoveNext()""
    IL_01b1:  brtrue     IL_00b3
    IL_01b6:  ldarg.0
    IL_01b7:  call       ""void C.<F>d__0.<>m__Finally6()""
    IL_01bc:  nop
    IL_01bd:  ldarg.0
    IL_01be:  ldnull
    IL_01bf:  stfld      ""System.Collections.Generic.IEnumerator<System.ValueTuple<int, int>> C.<F>d__0.<>s__6""
    IL_01c4:  ldarg.0
    IL_01c5:  call       ""void C.<F>d__0.<>m__Finally5()""
    IL_01ca:  nop
    IL_01cb:  ldarg.0
    IL_01cc:  call       ""void C.<F>d__0.<>m__Finally4()""
    IL_01d1:  nop
    IL_01d2:  ldarg.0
    IL_01d3:  ldnull
    IL_01d4:  stfld      ""System.IDisposable C.<F>d__0.<y1>5__4""
    IL_01d9:  ldarg.0
    IL_01da:  ldnull
    IL_01db:  stfld      ""System.IDisposable C.<F>d__0.<y2>5__5""
    IL_01e0:  ldarg.0
    IL_01e1:  call       ""void C.<F>d__0.<>m__Finally3()""
    IL_01e6:  nop
    IL_01e7:  ldarg.0
    IL_01e8:  ldnull
    IL_01e9:  stfld      ""System.IDisposable C.<F>d__0.<>s__3""
    IL_01ee:  ldc.i4.0
    IL_01ef:  stloc.0
    IL_01f0:  br.s       IL_01f2
    IL_01f2:  ldarg.0
    IL_01f3:  call       ""void C.<F>d__0.<>m__Finally2()""
    IL_01f8:  nop
    IL_01f9:  br.s       IL_01fb
    IL_01fb:  ldarg.0
    IL_01fc:  call       ""void C.<F>d__0.<>m__Finally1()""
    IL_0201:  nop
    IL_0202:  leave.s    IL_020c
  }
  fault
  {
    IL_0204:  ldarg.0
    IL_0205:  call       ""void C.<F>d__0.Dispose()""
    IL_020a:  nop
    IL_020b:  endfinally
  }
  IL_020c:  ldloc.0
  IL_020d:  ret
}");
        }

        [Fact]
        public void UpdateAsyncEnumerable_AwaitAndYield_AddAndRemove()
        {
            var source0 = MarkedSource(@"
using System.Collections.Generic;
using System.Threading.Tasks;

class C
{
    async IAsyncEnumerable<int> F()
    {
        <N:0>yield return F1();</N:0>
        <N:1>await Task.FromResult(1)</N:1>;
        End();
    }

    static int F1() => 1;
    static int F2() => 1;
    static void End() { }
}
");
            var source1 = MarkedSource(@"
using System.Collections.Generic;
using System.Threading.Tasks;

class C
{
    async IAsyncEnumerable<int> F()
    {
        <N:2>yield return F2();</N:2>
        <N:3>await Task.FromResult(2)</N:3>;
        <N:0>yield return F1();</N:0>
        <N:1>await Task.FromResult(1)</N:1>;
        End();
    }

    static int F1() => 1;
    static int F2() => 1;
    static void End() { }
}");
            var source2 = MarkedSource(@"
using System.Collections.Generic;
using System.Threading.Tasks;

class C
{
    async IAsyncEnumerable<int> F()
    {
        <N:2>yield return F2();</N:2>
        <N:3>await Task.FromResult(2)</N:3>;
        <N:1>await Task.FromResult(1)</N:1>;
        End();
    }

    static int F1() => 1;
    static int F2() => 1;
    static void End() { }
}");
            var source3 = MarkedSource(@"
using System.Collections.Generic;
using System.Threading.Tasks;

class C
{
    async IAsyncEnumerable<int> F()
    {
        <N:2>yield return F2();</N:2>
        <N:1>await Task.FromResult(1)</N:1>;
        End();
    }

    static int F1() => 1;
    static int F2() => 1;
    static void End() { }
}");
            var asyncStreamsTree = Parse(AsyncStreamsTypes, options: (CSharpParseOptions)source0.Tree.Options);

            var compilation0 = CreateCompilationWithTasksExtensions(new[] { source0.Tree, asyncStreamsTree }, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(new[] { source1.Tree, asyncStreamsTree });
            var compilation2 = compilation1.WithSource(new[] { source2.Tree, asyncStreamsTree });
            var compilation3 = compilation2.WithSource(new[] { source3.Tree, asyncStreamsTree });

            var v0 = CompileAndVerify(compilation0);
            v0.VerifyDiagnostics();
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");
            var f2 = compilation2.GetMember<MethodSymbol>("C.F");
            var f3 = compilation3.GetMember<MethodSymbol>("C.F");

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                 generation0,
                 ImmutableArray.Create(
                     SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

            var diff2 = compilation2.EmitDifference(
                 diff1.NextGeneration,
                 ImmutableArray.Create(
                     SemanticEdit.Create(SemanticEditKind.Update, f1, f2, GetSyntaxMapFromMarkers(source1, source2))));

            var diff3 = compilation3.EmitDifference(
                 diff2.NextGeneration,
                 ImmutableArray.Create(
                     SemanticEdit.Create(SemanticEditKind.Update, f2, f3, GetSyntaxMapFromMarkers(source2, source3))));

            v0.VerifyPdb("C.F", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""F"">
      <customDebugInfo>
        <forwardIterator name=""&lt;F&gt;d__0"" />
        <encStateMachineStateMap>
          <state number=""-4"" offset=""16"" />
          <state number=""0"" offset=""55"" />
        </encStateMachineStateMap>
      </customDebugInfo>
    </method>
  </methods>
</symbols>
");

            diff1.VerifyIL("C.<F>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      484 (0x1e4)
  .maxstack  3
  .locals init (int V_0,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_1,
                C.<F>d__0 V_2,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<F>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  ldc.i4.s   -5
    IL_000a:  sub
    IL_000b:  switch    (
        IL_002e,
        IL_0030,
        IL_0035,
        IL_0041,
        IL_0041,
        IL_0037,
        IL_003c)
    IL_002c:  br.s       IL_0041
    IL_002e:  br.s       IL_0072
    IL_0030:  br         IL_0102
    IL_0035:  br.s       IL_0041
    IL_0037:  br         IL_0154
    IL_003c:  br         IL_00c4
    IL_0041:  ldarg.0
    IL_0042:  ldfld      ""bool C.<F>d__0.<>w__disposeMode""
    IL_0047:  brfalse.s  IL_004e
    IL_0049:  leave      IL_01ad
    IL_004e:  ldarg.0
    IL_004f:  ldc.i4.m1
    IL_0050:  dup
    IL_0051:  stloc.0
    IL_0052:  stfld      ""int C.<F>d__0.<>1__state""
    IL_0057:  nop
    IL_0058:  ldarg.0
    IL_0059:  call       ""int C.F2()""
    IL_005e:  stfld      ""int C.<F>d__0.<>2__current""
    IL_0063:  ldarg.0
    IL_0064:  ldc.i4.s   -5
    IL_0066:  dup
    IL_0067:  stloc.0
    IL_0068:  stfld      ""int C.<F>d__0.<>1__state""
    IL_006d:  leave      IL_01d6
    IL_0072:  ldarg.0
    IL_0073:  ldc.i4.m1
    IL_0074:  dup
    IL_0075:  stloc.0
    IL_0076:  stfld      ""int C.<F>d__0.<>1__state""
    IL_007b:  ldarg.0
    IL_007c:  ldfld      ""bool C.<F>d__0.<>w__disposeMode""
    IL_0081:  brfalse.s  IL_0088
    IL_0083:  leave      IL_01ad
    IL_0088:  ldc.i4.2
    IL_0089:  call       ""System.Threading.Tasks.Task<int> System.Threading.Tasks.Task.FromResult<int>(int)""
    IL_008e:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0093:  stloc.1
    IL_0094:  ldloca.s   V_1
    IL_0096:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_009b:  brtrue.s   IL_00e0
    IL_009d:  ldarg.0
    IL_009e:  ldc.i4.1
    IL_009f:  dup
    IL_00a0:  stloc.0
    IL_00a1:  stfld      ""int C.<F>d__0.<>1__state""
    IL_00a6:  ldarg.0
    IL_00a7:  ldloc.1
    IL_00a8:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> C.<F>d__0.<>u__1""
    IL_00ad:  ldarg.0
    IL_00ae:  stloc.2
    IL_00af:  ldarg.0
    IL_00b0:  ldflda     ""System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<F>d__0.<>t__builder""
    IL_00b5:  ldloca.s   V_1
    IL_00b7:  ldloca.s   V_2
    IL_00b9:  call       ""void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, C.<F>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref C.<F>d__0)""
    IL_00be:  nop
    IL_00bf:  leave      IL_01e3
    IL_00c4:  ldarg.0
    IL_00c5:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> C.<F>d__0.<>u__1""
    IL_00ca:  stloc.1
    IL_00cb:  ldarg.0
    IL_00cc:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> C.<F>d__0.<>u__1""
    IL_00d1:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_00d7:  ldarg.0
    IL_00d8:  ldc.i4.m1
    IL_00d9:  dup
    IL_00da:  stloc.0
    IL_00db:  stfld      ""int C.<F>d__0.<>1__state""
    IL_00e0:  ldloca.s   V_1
    IL_00e2:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_00e7:  pop
    IL_00e8:  ldarg.0
    IL_00e9:  call       ""int C.F1()""
    IL_00ee:  stfld      ""int C.<F>d__0.<>2__current""
    IL_00f3:  ldarg.0
    IL_00f4:  ldc.i4.s   -4
    IL_00f6:  dup
    IL_00f7:  stloc.0
    IL_00f8:  stfld      ""int C.<F>d__0.<>1__state""
    IL_00fd:  leave      IL_01d6
    IL_0102:  ldarg.0
    IL_0103:  ldc.i4.m1
    IL_0104:  dup
    IL_0105:  stloc.0
    IL_0106:  stfld      ""int C.<F>d__0.<>1__state""
    IL_010b:  ldarg.0
    IL_010c:  ldfld      ""bool C.<F>d__0.<>w__disposeMode""
    IL_0111:  brfalse.s  IL_0118
    IL_0113:  leave      IL_01ad
    IL_0118:  ldc.i4.1
    IL_0119:  call       ""System.Threading.Tasks.Task<int> System.Threading.Tasks.Task.FromResult<int>(int)""
    IL_011e:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0123:  stloc.3
    IL_0124:  ldloca.s   V_3
    IL_0126:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_012b:  brtrue.s   IL_0170
    IL_012d:  ldarg.0
    IL_012e:  ldc.i4.0
    IL_012f:  dup
    IL_0130:  stloc.0
    IL_0131:  stfld      ""int C.<F>d__0.<>1__state""
    IL_0136:  ldarg.0
    IL_0137:  ldloc.3
    IL_0138:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> C.<F>d__0.<>u__1""
    IL_013d:  ldarg.0
    IL_013e:  stloc.2
    IL_013f:  ldarg.0
    IL_0140:  ldflda     ""System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<F>d__0.<>t__builder""
    IL_0145:  ldloca.s   V_3
    IL_0147:  ldloca.s   V_2
    IL_0149:  call       ""void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, C.<F>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref C.<F>d__0)""
    IL_014e:  nop
    IL_014f:  leave      IL_01e3
    IL_0154:  ldarg.0
    IL_0155:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> C.<F>d__0.<>u__1""
    IL_015a:  stloc.3
    IL_015b:  ldarg.0
    IL_015c:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> C.<F>d__0.<>u__1""
    IL_0161:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0167:  ldarg.0
    IL_0168:  ldc.i4.m1
    IL_0169:  dup
    IL_016a:  stloc.0
    IL_016b:  stfld      ""int C.<F>d__0.<>1__state""
    IL_0170:  ldloca.s   V_3
    IL_0172:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0177:  pop
    IL_0178:  call       ""void C.End()""
    IL_017d:  nop
    IL_017e:  leave.s    IL_01ad
  }
  catch System.Exception
  {
    IL_0180:  stloc.s    V_4
    IL_0182:  ldarg.0
    IL_0183:  ldc.i4.s   -2
    IL_0185:  stfld      ""int C.<F>d__0.<>1__state""
    IL_018a:  ldarg.0
    IL_018b:  ldc.i4.0
    IL_018c:  stfld      ""int C.<F>d__0.<>2__current""
    IL_0191:  ldarg.0
    IL_0192:  ldflda     ""System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<F>d__0.<>t__builder""
    IL_0197:  call       ""void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.Complete()""
    IL_019c:  nop
    IL_019d:  ldarg.0
    IL_019e:  ldflda     ""System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<F>d__0.<>v__promiseOfValueOrEnd""
    IL_01a3:  ldloc.s    V_4
    IL_01a5:  call       ""void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetException(System.Exception)""
    IL_01aa:  nop
    IL_01ab:  leave.s    IL_01e3
  }
  IL_01ad:  ldarg.0
  IL_01ae:  ldc.i4.s   -2
  IL_01b0:  stfld      ""int C.<F>d__0.<>1__state""
  IL_01b5:  ldarg.0
  IL_01b6:  ldc.i4.0
  IL_01b7:  stfld      ""int C.<F>d__0.<>2__current""
  IL_01bc:  ldarg.0
  IL_01bd:  ldflda     ""System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<F>d__0.<>t__builder""
  IL_01c2:  call       ""void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.Complete()""
  IL_01c7:  nop
  IL_01c8:  ldarg.0
  IL_01c9:  ldflda     ""System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<F>d__0.<>v__promiseOfValueOrEnd""
  IL_01ce:  ldc.i4.0
  IL_01cf:  call       ""void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetResult(bool)""
  IL_01d4:  nop
  IL_01d5:  ret
  IL_01d6:  ldarg.0
  IL_01d7:  ldflda     ""System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<F>d__0.<>v__promiseOfValueOrEnd""
  IL_01dc:  ldc.i4.1
  IL_01dd:  call       ""void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetResult(bool)""
  IL_01e2:  nop
  IL_01e3:  ret
}
");

            diff2.VerifyIL("C.<F>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      449 (0x1c1)
  .maxstack  3
  .locals init (int V_0,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_1,
                C.<F>d__0 V_2,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<F>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  ldc.i4.s   -5
    IL_000a:  sub
    IL_000b:  switch    (
        IL_002e,
        IL_003c,
        IL_0030,
        IL_003c,
        IL_003c,
        IL_0032,
        IL_0037)
    IL_002c:  br.s       IL_003c
    IL_002e:  br.s       IL_007f
    IL_0030:  br.s       IL_004e
    IL_0032:  br         IL_0131
    IL_0037:  br         IL_00d1
    IL_003c:  ldloc.0
    IL_003d:  ldc.i4.s   -4
    IL_003f:  bgt.s      IL_004e
    IL_0041:  ldstr      """ + CodeAnalysisResources.EncCannotResumeSuspendedIteratorMethod + @"""
    IL_0046:  ldc.i4.s   -3
    IL_0048:  newobj     ""System.Runtime.CompilerServices.HotReloadException..ctor(string, int)""
    IL_004d:  throw
    IL_004e:  ldarg.0
    IL_004f:  ldfld      ""bool C.<F>d__0.<>w__disposeMode""
    IL_0054:  brfalse.s  IL_005b
    IL_0056:  leave      IL_018a
    IL_005b:  ldarg.0
    IL_005c:  ldc.i4.m1
    IL_005d:  dup
    IL_005e:  stloc.0
    IL_005f:  stfld      ""int C.<F>d__0.<>1__state""
    IL_0064:  nop
    IL_0065:  ldarg.0
    IL_0066:  call       ""int C.F2()""
    IL_006b:  stfld      ""int C.<F>d__0.<>2__current""
    IL_0070:  ldarg.0
    IL_0071:  ldc.i4.s   -5
    IL_0073:  dup
    IL_0074:  stloc.0
    IL_0075:  stfld      ""int C.<F>d__0.<>1__state""
    IL_007a:  leave      IL_01b3
    IL_007f:  ldarg.0
    IL_0080:  ldc.i4.m1
    IL_0081:  dup
    IL_0082:  stloc.0
    IL_0083:  stfld      ""int C.<F>d__0.<>1__state""
    IL_0088:  ldarg.0
    IL_0089:  ldfld      ""bool C.<F>d__0.<>w__disposeMode""
    IL_008e:  brfalse.s  IL_0095
    IL_0090:  leave      IL_018a
    IL_0095:  ldc.i4.2
    IL_0096:  call       ""System.Threading.Tasks.Task<int> System.Threading.Tasks.Task.FromResult<int>(int)""
    IL_009b:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_00a0:  stloc.1
    IL_00a1:  ldloca.s   V_1
    IL_00a3:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_00a8:  brtrue.s   IL_00ed
    IL_00aa:  ldarg.0
    IL_00ab:  ldc.i4.1
    IL_00ac:  dup
    IL_00ad:  stloc.0
    IL_00ae:  stfld      ""int C.<F>d__0.<>1__state""
    IL_00b3:  ldarg.0
    IL_00b4:  ldloc.1
    IL_00b5:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> C.<F>d__0.<>u__1""
    IL_00ba:  ldarg.0
    IL_00bb:  stloc.2
    IL_00bc:  ldarg.0
    IL_00bd:  ldflda     ""System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<F>d__0.<>t__builder""
    IL_00c2:  ldloca.s   V_1
    IL_00c4:  ldloca.s   V_2
    IL_00c6:  call       ""void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, C.<F>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref C.<F>d__0)""
    IL_00cb:  nop
    IL_00cc:  leave      IL_01c0
    IL_00d1:  ldarg.0
    IL_00d2:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> C.<F>d__0.<>u__1""
    IL_00d7:  stloc.1
    IL_00d8:  ldarg.0
    IL_00d9:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> C.<F>d__0.<>u__1""
    IL_00de:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_00e4:  ldarg.0
    IL_00e5:  ldc.i4.m1
    IL_00e6:  dup
    IL_00e7:  stloc.0
    IL_00e8:  stfld      ""int C.<F>d__0.<>1__state""
    IL_00ed:  ldloca.s   V_1
    IL_00ef:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_00f4:  pop
    IL_00f5:  ldc.i4.1
    IL_00f6:  call       ""System.Threading.Tasks.Task<int> System.Threading.Tasks.Task.FromResult<int>(int)""
    IL_00fb:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0100:  stloc.3
    IL_0101:  ldloca.s   V_3
    IL_0103:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_0108:  brtrue.s   IL_014d
    IL_010a:  ldarg.0
    IL_010b:  ldc.i4.0
    IL_010c:  dup
    IL_010d:  stloc.0
    IL_010e:  stfld      ""int C.<F>d__0.<>1__state""
    IL_0113:  ldarg.0
    IL_0114:  ldloc.3
    IL_0115:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> C.<F>d__0.<>u__1""
    IL_011a:  ldarg.0
    IL_011b:  stloc.2
    IL_011c:  ldarg.0
    IL_011d:  ldflda     ""System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<F>d__0.<>t__builder""
    IL_0122:  ldloca.s   V_3
    IL_0124:  ldloca.s   V_2
    IL_0126:  call       ""void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, C.<F>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref C.<F>d__0)""
    IL_012b:  nop
    IL_012c:  leave      IL_01c0
    IL_0131:  ldarg.0
    IL_0132:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> C.<F>d__0.<>u__1""
    IL_0137:  stloc.3
    IL_0138:  ldarg.0
    IL_0139:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> C.<F>d__0.<>u__1""
    IL_013e:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0144:  ldarg.0
    IL_0145:  ldc.i4.m1
    IL_0146:  dup
    IL_0147:  stloc.0
    IL_0148:  stfld      ""int C.<F>d__0.<>1__state""
    IL_014d:  ldloca.s   V_3
    IL_014f:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0154:  pop
    IL_0155:  call       ""void C.End()""
    IL_015a:  nop
    IL_015b:  leave.s    IL_018a
  }
  catch System.Exception
  {
    IL_015d:  stloc.s    V_4
    IL_015f:  ldarg.0
    IL_0160:  ldc.i4.s   -2
    IL_0162:  stfld      ""int C.<F>d__0.<>1__state""
    IL_0167:  ldarg.0
    IL_0168:  ldc.i4.0
    IL_0169:  stfld      ""int C.<F>d__0.<>2__current""
    IL_016e:  ldarg.0
    IL_016f:  ldflda     ""System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<F>d__0.<>t__builder""
    IL_0174:  call       ""void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.Complete()""
    IL_0179:  nop
    IL_017a:  ldarg.0
    IL_017b:  ldflda     ""System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<F>d__0.<>v__promiseOfValueOrEnd""
    IL_0180:  ldloc.s    V_4
    IL_0182:  call       ""void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetException(System.Exception)""
    IL_0187:  nop
    IL_0188:  leave.s    IL_01c0
  }
  IL_018a:  ldarg.0
  IL_018b:  ldc.i4.s   -2
  IL_018d:  stfld      ""int C.<F>d__0.<>1__state""
  IL_0192:  ldarg.0
  IL_0193:  ldc.i4.0
  IL_0194:  stfld      ""int C.<F>d__0.<>2__current""
  IL_0199:  ldarg.0
  IL_019a:  ldflda     ""System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<F>d__0.<>t__builder""
  IL_019f:  call       ""void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.Complete()""
  IL_01a4:  nop
  IL_01a5:  ldarg.0
  IL_01a6:  ldflda     ""System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<F>d__0.<>v__promiseOfValueOrEnd""
  IL_01ab:  ldc.i4.0
  IL_01ac:  call       ""void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetResult(bool)""
  IL_01b1:  nop
  IL_01b2:  ret
  IL_01b3:  ldarg.0
  IL_01b4:  ldflda     ""System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<F>d__0.<>v__promiseOfValueOrEnd""
  IL_01b9:  ldc.i4.1
  IL_01ba:  call       ""void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetResult(bool)""
  IL_01bf:  nop
  IL_01c0:  ret
}");
            diff3.VerifyIL("C.<F>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
 {
  // Code size      343 (0x157)
  .maxstack  3
  .locals init (int V_0,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_1,
                C.<F>d__0 V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<F>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  ldc.i4.s   -5
    IL_000a:  beq.s      IL_001a
    IL_000c:  br.s       IL_000e
    IL_000e:  ldloc.0
    IL_000f:  ldc.i4.s   -3
    IL_0011:  beq.s      IL_001c
    IL_0013:  br.s       IL_0015
    IL_0015:  ldloc.0
    IL_0016:  brfalse.s  IL_001e
    IL_0018:  br.s       IL_0023
    IL_001a:  br.s       IL_0077
    IL_001c:  br.s       IL_0046
    IL_001e:  br         IL_00c9
    IL_0023:  ldloc.0
    IL_0024:  ldc.i4.0
    IL_0025:  blt.s      IL_0034
    IL_0027:  ldstr      """ + CodeAnalysisResources.EncCannotResumeSuspendedAsyncMethod + @"""
    IL_002c:  ldc.i4.s   -4
    IL_002e:  newobj     ""System.Runtime.CompilerServices.HotReloadException..ctor(string, int)""
    IL_0033:  throw
    IL_0034:  ldloc.0
    IL_0035:  ldc.i4.s   -4
    IL_0037:  bgt.s      IL_0046
    IL_0039:  ldstr      """ + CodeAnalysisResources.EncCannotResumeSuspendedIteratorMethod + @"""
    IL_003e:  ldc.i4.s   -3
    IL_0040:  newobj     ""System.Runtime.CompilerServices.HotReloadException..ctor(string, int)""
    IL_0045:  throw
    IL_0046:  ldarg.0
    IL_0047:  ldfld      ""bool C.<F>d__0.<>w__disposeMode""
    IL_004c:  brfalse.s  IL_0053
    IL_004e:  leave      IL_0120
    IL_0053:  ldarg.0
    IL_0054:  ldc.i4.m1
    IL_0055:  dup
    IL_0056:  stloc.0
    IL_0057:  stfld      ""int C.<F>d__0.<>1__state""
    IL_005c:  nop
    IL_005d:  ldarg.0
    IL_005e:  call       ""int C.F2()""
    IL_0063:  stfld      ""int C.<F>d__0.<>2__current""
    IL_0068:  ldarg.0
    IL_0069:  ldc.i4.s   -5
    IL_006b:  dup
    IL_006c:  stloc.0
    IL_006d:  stfld      ""int C.<F>d__0.<>1__state""
    IL_0072:  leave      IL_0149
    IL_0077:  ldarg.0
    IL_0078:  ldc.i4.m1
    IL_0079:  dup
    IL_007a:  stloc.0
    IL_007b:  stfld      ""int C.<F>d__0.<>1__state""
    IL_0080:  ldarg.0
    IL_0081:  ldfld      ""bool C.<F>d__0.<>w__disposeMode""
    IL_0086:  brfalse.s  IL_008d
    IL_0088:  leave      IL_0120
    IL_008d:  ldc.i4.1
    IL_008e:  call       ""System.Threading.Tasks.Task<int> System.Threading.Tasks.Task.FromResult<int>(int)""
    IL_0093:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0098:  stloc.1
    IL_0099:  ldloca.s   V_1
    IL_009b:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_00a0:  brtrue.s   IL_00e5
    IL_00a2:  ldarg.0
    IL_00a3:  ldc.i4.0
    IL_00a4:  dup
    IL_00a5:  stloc.0
    IL_00a6:  stfld      ""int C.<F>d__0.<>1__state""
    IL_00ab:  ldarg.0
    IL_00ac:  ldloc.1
    IL_00ad:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> C.<F>d__0.<>u__1""
    IL_00b2:  ldarg.0
    IL_00b3:  stloc.2
    IL_00b4:  ldarg.0
    IL_00b5:  ldflda     ""System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<F>d__0.<>t__builder""
    IL_00ba:  ldloca.s   V_1
    IL_00bc:  ldloca.s   V_2
    IL_00be:  call       ""void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, C.<F>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref C.<F>d__0)""
    IL_00c3:  nop
    IL_00c4:  leave      IL_0156
    IL_00c9:  ldarg.0
    IL_00ca:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> C.<F>d__0.<>u__1""
    IL_00cf:  stloc.1
    IL_00d0:  ldarg.0
    IL_00d1:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> C.<F>d__0.<>u__1""
    IL_00d6:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_00dc:  ldarg.0
    IL_00dd:  ldc.i4.m1
    IL_00de:  dup
    IL_00df:  stloc.0
    IL_00e0:  stfld      ""int C.<F>d__0.<>1__state""
    IL_00e5:  ldloca.s   V_1
    IL_00e7:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_00ec:  pop
    IL_00ed:  call       ""void C.End()""
    IL_00f2:  nop
    IL_00f3:  leave.s    IL_0120
  }
  catch System.Exception
  {
    IL_00f5:  stloc.3
    IL_00f6:  ldarg.0
    IL_00f7:  ldc.i4.s   -2
    IL_00f9:  stfld      ""int C.<F>d__0.<>1__state""
    IL_00fe:  ldarg.0
    IL_00ff:  ldc.i4.0
    IL_0100:  stfld      ""int C.<F>d__0.<>2__current""
    IL_0105:  ldarg.0
    IL_0106:  ldflda     ""System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<F>d__0.<>t__builder""
    IL_010b:  call       ""void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.Complete()""
    IL_0110:  nop
    IL_0111:  ldarg.0
    IL_0112:  ldflda     ""System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<F>d__0.<>v__promiseOfValueOrEnd""
    IL_0117:  ldloc.3
    IL_0118:  call       ""void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetException(System.Exception)""
    IL_011d:  nop
    IL_011e:  leave.s    IL_0156
  }
  IL_0120:  ldarg.0
  IL_0121:  ldc.i4.s   -2
  IL_0123:  stfld      ""int C.<F>d__0.<>1__state""
  IL_0128:  ldarg.0
  IL_0129:  ldc.i4.0
  IL_012a:  stfld      ""int C.<F>d__0.<>2__current""
  IL_012f:  ldarg.0
  IL_0130:  ldflda     ""System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<F>d__0.<>t__builder""
  IL_0135:  call       ""void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.Complete()""
  IL_013a:  nop
  IL_013b:  ldarg.0
  IL_013c:  ldflda     ""System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<F>d__0.<>v__promiseOfValueOrEnd""
  IL_0141:  ldc.i4.0
  IL_0142:  call       ""void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetResult(bool)""
  IL_0147:  nop
  IL_0148:  ret
  IL_0149:  ldarg.0
  IL_014a:  ldflda     ""System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<F>d__0.<>v__promiseOfValueOrEnd""
  IL_014f:  ldc.i4.1
  IL_0150:  call       ""void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetResult(bool)""
  IL_0155:  nop
  IL_0156:  ret
}");
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/69805")]
        public void UpdateAwaitForEach_AsyncDisposableEnumerator()
        {
            var source0 = MarkedSource(@"
using System.Collections.Generic;
using System.Threading.Tasks;

class C
{
    async Task F()
    {
        <N:0>await foreach (var x in Iterator())</N:0>
        {
            Body(1);
        }

        End();
    }

    IAsyncEnumerable<int> Iterator() => null;
    static void Body(int x) {}
    static void End() { }
}
");
            var source1 = MarkedSource(@"
using System.Collections.Generic;
using System.Threading.Tasks;

class C
{
    async Task F()
    {
        <N:0>await foreach (var x in Iterator())</N:0>
        {
            Body(2);
        }

        End();
    }

    IAsyncEnumerable<int> Iterator() => null;
    static void Body(int x) { }
    static void End() { }
}");
            var asyncStreamsTree = Parse(
                AsyncStreamsTypes, options: (CSharpParseOptions)source0.Tree.Options, filename: "AsyncStreams.cs");

            var compilation0 = CreateCompilationWithTasksExtensions(new[] { source0.Tree, asyncStreamsTree }, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource([source1.Tree, asyncStreamsTree]);

            var v0 = CompileAndVerify(compilation0);
            v0.VerifyDiagnostics();
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            v0.VerifyPdb("C.F", @"
<symbols>
  <methods>
    <method containingType=""C"" name=""F"">
      <customDebugInfo>
        <forwardIterator name=""&lt;F&gt;d__0"" />
        <encLocalSlotMap>
          <slot kind=""5"" offset=""16"" />
          <slot kind=""22"" offset=""16"" />
          <slot kind=""23"" offset=""16"" />
          <slot kind=""0"" offset=""16"" />
          <slot kind=""28"" offset=""16"" />
        </encLocalSlotMap>
        <encStateMachineStateMap>
          <state number=""0"" offset=""16"" />
          <state number=""1"" offset=""16"" />
        </encStateMachineStateMap>
      </customDebugInfo>
    </method>
  </methods>
</symbols>
", options: PdbValidationOptions.ExcludeDocuments);

            v0.VerifyMethodBody("C.<F>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      477 (0x1dd)
  .maxstack  3
  .locals init (int V_0,
                System.Threading.CancellationToken V_1,
                System.Runtime.CompilerServices.ValueTaskAwaiter<bool> V_2,
                System.Threading.Tasks.ValueTask<bool> V_3,
                C.<F>d__0 V_4,
                object V_5,
                System.Runtime.CompilerServices.ValueTaskAwaiter V_6,
                System.Threading.Tasks.ValueTask V_7,
                System.Exception V_8)
  // sequence point: <hidden>
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<F>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    // sequence point: <hidden>
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0012
    IL_000a:  br.s       IL_000c
    IL_000c:  ldloc.0
    IL_000d:  ldc.i4.1
    IL_000e:  beq.s      IL_0014
    IL_0010:  br.s       IL_0019
    IL_0012:  br.s       IL_0048
    IL_0014:  br         IL_0143
    // sequence point: {
    IL_0019:  nop
    // sequence point: await foreach
    IL_001a:  nop
    // sequence point: Iterator()
    IL_001b:  ldarg.0
    IL_001c:  ldarg.0
    IL_001d:  ldfld      ""C C.<F>d__0.<>4__this""
    IL_0022:  call       ""System.Collections.Generic.IAsyncEnumerable<int> C.Iterator()""
    IL_0027:  ldloca.s   V_1
    IL_0029:  initobj    ""System.Threading.CancellationToken""
    IL_002f:  ldloc.1
    IL_0030:  callvirt   ""System.Collections.Generic.IAsyncEnumerator<int> System.Collections.Generic.IAsyncEnumerable<int>.GetAsyncEnumerator(System.Threading.CancellationToken)""
    IL_0035:  stfld      ""System.Collections.Generic.IAsyncEnumerator<int> C.<F>d__0.<>s__1""
    // sequence point: <hidden>
    IL_003a:  ldarg.0
    IL_003b:  ldnull
    IL_003c:  stfld      ""object C.<F>d__0.<>s__2""
    IL_0041:  ldarg.0
    IL_0042:  ldc.i4.0
    IL_0043:  stfld      ""int C.<F>d__0.<>s__3""
    // sequence point: <hidden>
    IL_0048:  nop
    .try
    {
      // sequence point: <hidden>
      IL_0049:  ldloc.0
      IL_004a:  brfalse.s  IL_004e
      IL_004c:  br.s       IL_0050
      IL_004e:  br.s       IL_00b1
      // sequence point: <hidden>
      IL_0050:  br.s       IL_006c
      // sequence point: var x
      IL_0052:  ldarg.0
      IL_0053:  ldarg.0
      IL_0054:  ldfld      ""System.Collections.Generic.IAsyncEnumerator<int> C.<F>d__0.<>s__1""
      IL_0059:  callvirt   ""int System.Collections.Generic.IAsyncEnumerator<int>.Current.get""
      IL_005e:  stfld      ""int C.<F>d__0.<x>5__4""
      // sequence point: {
      IL_0063:  nop
      // sequence point: Body(1);
      IL_0064:  ldc.i4.1
      IL_0065:  call       ""void C.Body(int)""
      IL_006a:  nop
      // sequence point: }
      IL_006b:  nop
      // sequence point: in
      IL_006c:  ldarg.0
      IL_006d:  ldfld      ""System.Collections.Generic.IAsyncEnumerator<int> C.<F>d__0.<>s__1""
      IL_0072:  callvirt   ""System.Threading.Tasks.ValueTask<bool> System.Collections.Generic.IAsyncEnumerator<int>.MoveNextAsync()""
      IL_0077:  stloc.3
      IL_0078:  ldloca.s   V_3
      IL_007a:  call       ""System.Runtime.CompilerServices.ValueTaskAwaiter<bool> System.Threading.Tasks.ValueTask<bool>.GetAwaiter()""
      IL_007f:  stloc.2
      // sequence point: <hidden>
      IL_0080:  ldloca.s   V_2
      IL_0082:  call       ""bool System.Runtime.CompilerServices.ValueTaskAwaiter<bool>.IsCompleted.get""
      IL_0087:  brtrue.s   IL_00cd
      IL_0089:  ldarg.0
      IL_008a:  ldc.i4.0
      IL_008b:  dup
      IL_008c:  stloc.0
      IL_008d:  stfld      ""int C.<F>d__0.<>1__state""
      // async: yield
      IL_0092:  ldarg.0
      IL_0093:  ldloc.2
      IL_0094:  stfld      ""System.Runtime.CompilerServices.ValueTaskAwaiter<bool> C.<F>d__0.<>u__1""
      IL_0099:  ldarg.0
      IL_009a:  stloc.s    V_4
      IL_009c:  ldarg.0
      IL_009d:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<F>d__0.<>t__builder""
      IL_00a2:  ldloca.s   V_2
      IL_00a4:  ldloca.s   V_4
      IL_00a6:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.ValueTaskAwaiter<bool>, C.<F>d__0>(ref System.Runtime.CompilerServices.ValueTaskAwaiter<bool>, ref C.<F>d__0)""
      IL_00ab:  nop
      IL_00ac:  leave      IL_01dc
      // async: resume
      IL_00b1:  ldarg.0
      IL_00b2:  ldfld      ""System.Runtime.CompilerServices.ValueTaskAwaiter<bool> C.<F>d__0.<>u__1""
      IL_00b7:  stloc.2
      IL_00b8:  ldarg.0
      IL_00b9:  ldflda     ""System.Runtime.CompilerServices.ValueTaskAwaiter<bool> C.<F>d__0.<>u__1""
      IL_00be:  initobj    ""System.Runtime.CompilerServices.ValueTaskAwaiter<bool>""
      IL_00c4:  ldarg.0
      IL_00c5:  ldc.i4.m1
      IL_00c6:  dup
      IL_00c7:  stloc.0
      IL_00c8:  stfld      ""int C.<F>d__0.<>1__state""
      IL_00cd:  ldarg.0
      IL_00ce:  ldloca.s   V_2
      IL_00d0:  call       ""bool System.Runtime.CompilerServices.ValueTaskAwaiter<bool>.GetResult()""
      IL_00d5:  stfld      ""bool C.<F>d__0.<>s__5""
      IL_00da:  ldarg.0
      IL_00db:  ldfld      ""bool C.<F>d__0.<>s__5""
      IL_00e0:  brtrue     IL_0052
      // sequence point: <hidden>
      IL_00e5:  leave.s    IL_00f3
    }
    catch object
    {
      // sequence point: <hidden>
      IL_00e7:  stloc.s    V_5
      IL_00e9:  ldarg.0
      IL_00ea:  ldloc.s    V_5
      IL_00ec:  stfld      ""object C.<F>d__0.<>s__2""
      IL_00f1:  leave.s    IL_00f3
    }
    // sequence point: <hidden>
    IL_00f3:  ldarg.0
    IL_00f4:  ldfld      ""System.Collections.Generic.IAsyncEnumerator<int> C.<F>d__0.<>s__1""
    IL_00f9:  brfalse.s  IL_0168
    IL_00fb:  ldarg.0
    IL_00fc:  ldfld      ""System.Collections.Generic.IAsyncEnumerator<int> C.<F>d__0.<>s__1""
    IL_0101:  callvirt   ""System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()""
    IL_0106:  stloc.s    V_7
    IL_0108:  ldloca.s   V_7
    IL_010a:  call       ""System.Runtime.CompilerServices.ValueTaskAwaiter System.Threading.Tasks.ValueTask.GetAwaiter()""
    IL_010f:  stloc.s    V_6
    // sequence point: <hidden>
    IL_0111:  ldloca.s   V_6
    IL_0113:  call       ""bool System.Runtime.CompilerServices.ValueTaskAwaiter.IsCompleted.get""
    IL_0118:  brtrue.s   IL_0160
    IL_011a:  ldarg.0
    IL_011b:  ldc.i4.1
    IL_011c:  dup
    IL_011d:  stloc.0
    IL_011e:  stfld      ""int C.<F>d__0.<>1__state""
    // async: yield
    IL_0123:  ldarg.0
    IL_0124:  ldloc.s    V_6
    IL_0126:  stfld      ""System.Runtime.CompilerServices.ValueTaskAwaiter C.<F>d__0.<>u__2""
    IL_012b:  ldarg.0
    IL_012c:  stloc.s    V_4
    IL_012e:  ldarg.0
    IL_012f:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<F>d__0.<>t__builder""
    IL_0134:  ldloca.s   V_6
    IL_0136:  ldloca.s   V_4
    IL_0138:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.ValueTaskAwaiter, C.<F>d__0>(ref System.Runtime.CompilerServices.ValueTaskAwaiter, ref C.<F>d__0)""
    IL_013d:  nop
    IL_013e:  leave      IL_01dc
    // async: resume
    IL_0143:  ldarg.0
    IL_0144:  ldfld      ""System.Runtime.CompilerServices.ValueTaskAwaiter C.<F>d__0.<>u__2""
    IL_0149:  stloc.s    V_6
    IL_014b:  ldarg.0
    IL_014c:  ldflda     ""System.Runtime.CompilerServices.ValueTaskAwaiter C.<F>d__0.<>u__2""
    IL_0151:  initobj    ""System.Runtime.CompilerServices.ValueTaskAwaiter""
    IL_0157:  ldarg.0
    IL_0158:  ldc.i4.m1
    IL_0159:  dup
    IL_015a:  stloc.0
    IL_015b:  stfld      ""int C.<F>d__0.<>1__state""
    IL_0160:  ldloca.s   V_6
    IL_0162:  call       ""void System.Runtime.CompilerServices.ValueTaskAwaiter.GetResult()""
    IL_0167:  nop
    // sequence point: <hidden>
    IL_0168:  ldarg.0
    IL_0169:  ldfld      ""object C.<F>d__0.<>s__2""
    IL_016e:  stloc.s    V_5
    IL_0170:  ldloc.s    V_5
    IL_0172:  brfalse.s  IL_0191
    IL_0174:  ldloc.s    V_5
    IL_0176:  isinst     ""System.Exception""
    IL_017b:  stloc.s    V_8
    IL_017d:  ldloc.s    V_8
    IL_017f:  brtrue.s   IL_0184
    IL_0181:  ldloc.s    V_5
    IL_0183:  throw
    IL_0184:  ldloc.s    V_8
    IL_0186:  call       ""System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)""
    IL_018b:  callvirt   ""void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()""
    IL_0190:  nop
    IL_0191:  ldarg.0
    IL_0192:  ldfld      ""int C.<F>d__0.<>s__3""
    IL_0197:  pop
    IL_0198:  ldarg.0
    IL_0199:  ldnull
    IL_019a:  stfld      ""object C.<F>d__0.<>s__2""
    IL_019f:  ldarg.0
    IL_01a0:  ldnull
    IL_01a1:  stfld      ""System.Collections.Generic.IAsyncEnumerator<int> C.<F>d__0.<>s__1""
    // sequence point: End();
    IL_01a6:  call       ""void C.End()""
    IL_01ab:  nop
    IL_01ac:  leave.s    IL_01c8
  }
  catch System.Exception
  {
    // sequence point: <hidden>
    IL_01ae:  stloc.s    V_8
    IL_01b0:  ldarg.0
    IL_01b1:  ldc.i4.s   -2
    IL_01b3:  stfld      ""int C.<F>d__0.<>1__state""
    IL_01b8:  ldarg.0
    IL_01b9:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<F>d__0.<>t__builder""
    IL_01be:  ldloc.s    V_8
    IL_01c0:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_01c5:  nop
    IL_01c6:  leave.s    IL_01dc
  }
  // sequence point: }
  IL_01c8:  ldarg.0
  IL_01c9:  ldc.i4.s   -2
  IL_01cb:  stfld      ""int C.<F>d__0.<>1__state""
  // sequence point: <hidden>
  IL_01d0:  ldarg.0
  IL_01d1:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<F>d__0.<>t__builder""
  IL_01d6:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_01db:  nop
  IL_01dc:  ret
}
");

            var diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

            diff1.VerifyIL("C.<F>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      477 (0x1dd)
  .maxstack  3
  .locals init (int V_0,
                System.Threading.CancellationToken V_1,
                System.Runtime.CompilerServices.ValueTaskAwaiter<bool> V_2,
                System.Threading.Tasks.ValueTask<bool> V_3,
                C.<F>d__0 V_4,
                object V_5,
                System.Runtime.CompilerServices.ValueTaskAwaiter V_6,
                System.Threading.Tasks.ValueTask V_7,
                System.Exception V_8)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<F>d__0.<>1__state""
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
    IL_0012:  br.s       IL_0048
    IL_0014:  br         IL_0143
    IL_0019:  nop
    IL_001a:  nop
    IL_001b:  ldarg.0
    IL_001c:  ldarg.0
    IL_001d:  ldfld      ""C C.<F>d__0.<>4__this""
    IL_0022:  call       ""System.Collections.Generic.IAsyncEnumerable<int> C.Iterator()""
    IL_0027:  ldloca.s   V_1
    IL_0029:  initobj    ""System.Threading.CancellationToken""
    IL_002f:  ldloc.1
    IL_0030:  callvirt   ""System.Collections.Generic.IAsyncEnumerator<int> System.Collections.Generic.IAsyncEnumerable<int>.GetAsyncEnumerator(System.Threading.CancellationToken)""
    IL_0035:  stfld      ""System.Collections.Generic.IAsyncEnumerator<int> C.<F>d__0.<>s__1""
    IL_003a:  ldarg.0
    IL_003b:  ldnull
    IL_003c:  stfld      ""object C.<F>d__0.<>s__2""
    IL_0041:  ldarg.0
    IL_0042:  ldc.i4.0
    IL_0043:  stfld      ""int C.<F>d__0.<>s__3""
    IL_0048:  nop
    .try
    {
      IL_0049:  ldloc.0
      IL_004a:  brfalse.s  IL_004e
      IL_004c:  br.s       IL_0050
      IL_004e:  br.s       IL_00b1
      IL_0050:  br.s       IL_006c
      IL_0052:  ldarg.0
      IL_0053:  ldarg.0
      IL_0054:  ldfld      ""System.Collections.Generic.IAsyncEnumerator<int> C.<F>d__0.<>s__1""
      IL_0059:  callvirt   ""int System.Collections.Generic.IAsyncEnumerator<int>.Current.get""
      IL_005e:  stfld      ""int C.<F>d__0.<x>5__4""
      IL_0063:  nop
      IL_0064:  ldc.i4.2
      IL_0065:  call       ""void C.Body(int)""
      IL_006a:  nop
      IL_006b:  nop
      IL_006c:  ldarg.0
      IL_006d:  ldfld      ""System.Collections.Generic.IAsyncEnumerator<int> C.<F>d__0.<>s__1""
      IL_0072:  callvirt   ""System.Threading.Tasks.ValueTask<bool> System.Collections.Generic.IAsyncEnumerator<int>.MoveNextAsync()""
      IL_0077:  stloc.3
      IL_0078:  ldloca.s   V_3
      IL_007a:  call       ""System.Runtime.CompilerServices.ValueTaskAwaiter<bool> System.Threading.Tasks.ValueTask<bool>.GetAwaiter()""
      IL_007f:  stloc.2
      IL_0080:  ldloca.s   V_2
      IL_0082:  call       ""bool System.Runtime.CompilerServices.ValueTaskAwaiter<bool>.IsCompleted.get""
      IL_0087:  brtrue.s   IL_00cd
      IL_0089:  ldarg.0
      IL_008a:  ldc.i4.0
      IL_008b:  dup
      IL_008c:  stloc.0
      IL_008d:  stfld      ""int C.<F>d__0.<>1__state""
      IL_0092:  ldarg.0
      IL_0093:  ldloc.2
      IL_0094:  stfld      ""System.Runtime.CompilerServices.ValueTaskAwaiter<bool> C.<F>d__0.<>u__1""
      IL_0099:  ldarg.0
      IL_009a:  stloc.s    V_4
      IL_009c:  ldarg.0
      IL_009d:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<F>d__0.<>t__builder""
      IL_00a2:  ldloca.s   V_2
      IL_00a4:  ldloca.s   V_4
      IL_00a6:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.ValueTaskAwaiter<bool>, C.<F>d__0>(ref System.Runtime.CompilerServices.ValueTaskAwaiter<bool>, ref C.<F>d__0)""
      IL_00ab:  nop
      IL_00ac:  leave      IL_01dc
      IL_00b1:  ldarg.0
      IL_00b2:  ldfld      ""System.Runtime.CompilerServices.ValueTaskAwaiter<bool> C.<F>d__0.<>u__1""
      IL_00b7:  stloc.2
      IL_00b8:  ldarg.0
      IL_00b9:  ldflda     ""System.Runtime.CompilerServices.ValueTaskAwaiter<bool> C.<F>d__0.<>u__1""
      IL_00be:  initobj    ""System.Runtime.CompilerServices.ValueTaskAwaiter<bool>""
      IL_00c4:  ldarg.0
      IL_00c5:  ldc.i4.m1
      IL_00c6:  dup
      IL_00c7:  stloc.0
      IL_00c8:  stfld      ""int C.<F>d__0.<>1__state""
      IL_00cd:  ldarg.0
      IL_00ce:  ldloca.s   V_2
      IL_00d0:  call       ""bool System.Runtime.CompilerServices.ValueTaskAwaiter<bool>.GetResult()""
      IL_00d5:  stfld      ""bool C.<F>d__0.<>s__5""
      IL_00da:  ldarg.0
      IL_00db:  ldfld      ""bool C.<F>d__0.<>s__5""
      IL_00e0:  brtrue     IL_0052
      IL_00e5:  leave.s    IL_00f3
    }
    catch object
    {
      IL_00e7:  stloc.s    V_5
      IL_00e9:  ldarg.0
      IL_00ea:  ldloc.s    V_5
      IL_00ec:  stfld      ""object C.<F>d__0.<>s__2""
      IL_00f1:  leave.s    IL_00f3
    }
    IL_00f3:  ldarg.0
    IL_00f4:  ldfld      ""System.Collections.Generic.IAsyncEnumerator<int> C.<F>d__0.<>s__1""
    IL_00f9:  brfalse.s  IL_0168
    IL_00fb:  ldarg.0
    IL_00fc:  ldfld      ""System.Collections.Generic.IAsyncEnumerator<int> C.<F>d__0.<>s__1""
    IL_0101:  callvirt   ""System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()""
    IL_0106:  stloc.s    V_7
    IL_0108:  ldloca.s   V_7
    IL_010a:  call       ""System.Runtime.CompilerServices.ValueTaskAwaiter System.Threading.Tasks.ValueTask.GetAwaiter()""
    IL_010f:  stloc.s    V_6
    IL_0111:  ldloca.s   V_6
    IL_0113:  call       ""bool System.Runtime.CompilerServices.ValueTaskAwaiter.IsCompleted.get""
    IL_0118:  brtrue.s   IL_0160
    IL_011a:  ldarg.0
    IL_011b:  ldc.i4.1
    IL_011c:  dup
    IL_011d:  stloc.0
    IL_011e:  stfld      ""int C.<F>d__0.<>1__state""
    IL_0123:  ldarg.0
    IL_0124:  ldloc.s    V_6
    IL_0126:  stfld      ""System.Runtime.CompilerServices.ValueTaskAwaiter C.<F>d__0.<>u__2""
    IL_012b:  ldarg.0
    IL_012c:  stloc.s    V_4
    IL_012e:  ldarg.0
    IL_012f:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<F>d__0.<>t__builder""
    IL_0134:  ldloca.s   V_6
    IL_0136:  ldloca.s   V_4
    IL_0138:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.ValueTaskAwaiter, C.<F>d__0>(ref System.Runtime.CompilerServices.ValueTaskAwaiter, ref C.<F>d__0)""
    IL_013d:  nop
    IL_013e:  leave      IL_01dc
    IL_0143:  ldarg.0
    IL_0144:  ldfld      ""System.Runtime.CompilerServices.ValueTaskAwaiter C.<F>d__0.<>u__2""
    IL_0149:  stloc.s    V_6
    IL_014b:  ldarg.0
    IL_014c:  ldflda     ""System.Runtime.CompilerServices.ValueTaskAwaiter C.<F>d__0.<>u__2""
    IL_0151:  initobj    ""System.Runtime.CompilerServices.ValueTaskAwaiter""
    IL_0157:  ldarg.0
    IL_0158:  ldc.i4.m1
    IL_0159:  dup
    IL_015a:  stloc.0
    IL_015b:  stfld      ""int C.<F>d__0.<>1__state""
    IL_0160:  ldloca.s   V_6
    IL_0162:  call       ""void System.Runtime.CompilerServices.ValueTaskAwaiter.GetResult()""
    IL_0167:  nop
    IL_0168:  ldarg.0
    IL_0169:  ldfld      ""object C.<F>d__0.<>s__2""
    IL_016e:  stloc.s    V_5
    IL_0170:  ldloc.s    V_5
    IL_0172:  brfalse.s  IL_0191
    IL_0174:  ldloc.s    V_5
    IL_0176:  isinst     ""System.Exception""
    IL_017b:  stloc.s    V_8
    IL_017d:  ldloc.s    V_8
    IL_017f:  brtrue.s   IL_0184
    IL_0181:  ldloc.s    V_5
    IL_0183:  throw
    IL_0184:  ldloc.s    V_8
    IL_0186:  call       ""System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)""
    IL_018b:  callvirt   ""void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()""
    IL_0190:  nop
    IL_0191:  ldarg.0
    IL_0192:  ldfld      ""int C.<F>d__0.<>s__3""
    IL_0197:  pop
    IL_0198:  ldarg.0
    IL_0199:  ldnull
    IL_019a:  stfld      ""object C.<F>d__0.<>s__2""
    IL_019f:  ldarg.0
    IL_01a0:  ldnull
    IL_01a1:  stfld      ""System.Collections.Generic.IAsyncEnumerator<int> C.<F>d__0.<>s__1""
    IL_01a6:  call       ""void C.End()""
    IL_01ab:  nop
    IL_01ac:  leave.s    IL_01c8
  }
  catch System.Exception
  {
    IL_01ae:  stloc.s    V_8
    IL_01b0:  ldarg.0
    IL_01b1:  ldc.i4.s   -2
    IL_01b3:  stfld      ""int C.<F>d__0.<>1__state""
    IL_01b8:  ldarg.0
    IL_01b9:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<F>d__0.<>t__builder""
    IL_01be:  ldloc.s    V_8
    IL_01c0:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_01c5:  nop
    IL_01c6:  leave.s    IL_01dc
  }
  IL_01c8:  ldarg.0
  IL_01c9:  ldc.i4.s   -2
  IL_01cb:  stfld      ""int C.<F>d__0.<>1__state""
  IL_01d0:  ldarg.0
  IL_01d1:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<F>d__0.<>t__builder""
  IL_01d6:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_01db:  nop
  IL_01dc:  ret
}");
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/69805")]
        public void UpdateAwaitForEach_NonDisposableEnumerator()
        {
            var source0 = MarkedSource(@"
using System.Collections.Generic;
using System.Threading.Tasks;

class C
{
    async Task F()
    {
        <N:0>await foreach (var x in Iterator())</N:0>
        {
            Body(1);
        }

        End();
    }

    IAsyncEnumerable<int> Iterator() => null;
    static void Body(int x) {}
    static void End() { }
}
");
            var source1 = MarkedSource(@"
using System.Collections.Generic;
using System.Threading.Tasks;

class C
{
    async Task F()
    {
        <N:0>await foreach (var x in Iterator())</N:0>
        {
            Body(2);
        }

        End();
    }

    IAsyncEnumerable<int> Iterator() => null;
    static void Body(int x) { }
    static void End() { }
}");
            var asyncStreamsTree = Parse(
                NonDisposableAsyncEnumeratorDefinition + CommonAsyncStreamsTypes, options: (CSharpParseOptions)source0.Tree.Options, filename: "AsyncStreams.cs");

            var compilation0 = CreateCompilationWithTasksExtensions(new[] { source0.Tree, asyncStreamsTree }, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(new[] { source1.Tree, asyncStreamsTree });

            var v0 = CompileAndVerify(compilation0);
            v0.VerifyDiagnostics();
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            // Both states are allocated eventhough only a single await is emitted:
            v0.VerifyPdb("C.F", @"
<symbols>
  <methods>
    <method containingType=""C"" name=""F"">
      <customDebugInfo>
        <forwardIterator name=""&lt;F&gt;d__0"" />
        <encLocalSlotMap>
          <slot kind=""5"" offset=""16"" />
          <slot kind=""0"" offset=""16"" />
          <slot kind=""28"" offset=""16"" />
        </encLocalSlotMap>
        <encStateMachineStateMap>
          <state number=""0"" offset=""16"" />
          <state number=""1"" offset=""16"" />
        </encStateMachineStateMap>
      </customDebugInfo>
    </method>
  </methods>
</symbols>
", options: PdbValidationOptions.ExcludeDocuments);

            v0.VerifyMethodBody("C.<F>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      266 (0x10a)
  .maxstack  3
  .locals init (int V_0,
                System.Threading.CancellationToken V_1,
                System.Runtime.CompilerServices.ValueTaskAwaiter<bool> V_2,
                System.Threading.Tasks.ValueTask<bool> V_3,
                C.<F>d__0 V_4,
                System.Exception V_5)
  // sequence point: <hidden>
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<F>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    // sequence point: <hidden>
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0012
    IL_000a:  br.s       IL_000c
    IL_000c:  ldloc.0
    IL_000d:  ldc.i4.1
    IL_000e:  beq.s      IL_0017
    IL_0010:  br.s       IL_0019
    IL_0012:  br         IL_0098
    IL_0017:  br.s       IL_0098
    // sequence point: {
    IL_0019:  nop
    // sequence point: await foreach
    IL_001a:  nop
    // sequence point: Iterator()
    IL_001b:  ldarg.0
    IL_001c:  ldarg.0
    IL_001d:  ldfld      ""C C.<F>d__0.<>4__this""
    IL_0022:  call       ""System.Collections.Generic.IAsyncEnumerable<int> C.Iterator()""
    IL_0027:  ldloca.s   V_1
    IL_0029:  initobj    ""System.Threading.CancellationToken""
    IL_002f:  ldloc.1
    IL_0030:  callvirt   ""System.Collections.Generic.IAsyncEnumerator<int> System.Collections.Generic.IAsyncEnumerable<int>.GetAsyncEnumerator(System.Threading.CancellationToken)""
    IL_0035:  stfld      ""System.Collections.Generic.IAsyncEnumerator<int> C.<F>d__0.<>s__1""
    // sequence point: <hidden>
    IL_003a:  br.s       IL_0056
    // sequence point: var x
    IL_003c:  ldarg.0
    IL_003d:  ldarg.0
    IL_003e:  ldfld      ""System.Collections.Generic.IAsyncEnumerator<int> C.<F>d__0.<>s__1""
    IL_0043:  callvirt   ""int System.Collections.Generic.IAsyncEnumerator<int>.Current.get""
    IL_0048:  stfld      ""int C.<F>d__0.<x>5__2""
    // sequence point: {
    IL_004d:  nop
    // sequence point: Body(1);
    IL_004e:  ldc.i4.1
    IL_004f:  call       ""void C.Body(int)""
    IL_0054:  nop
    // sequence point: }
    IL_0055:  nop
    // sequence point: in
    IL_0056:  ldarg.0
    IL_0057:  ldfld      ""System.Collections.Generic.IAsyncEnumerator<int> C.<F>d__0.<>s__1""
    IL_005c:  callvirt   ""System.Threading.Tasks.ValueTask<bool> System.Collections.Generic.IAsyncEnumerator<int>.MoveNextAsync()""
    IL_0061:  stloc.3
    IL_0062:  ldloca.s   V_3
    IL_0064:  call       ""System.Runtime.CompilerServices.ValueTaskAwaiter<bool> System.Threading.Tasks.ValueTask<bool>.GetAwaiter()""
    IL_0069:  stloc.2
    // sequence point: <hidden>
    IL_006a:  ldloca.s   V_2
    IL_006c:  call       ""bool System.Runtime.CompilerServices.ValueTaskAwaiter<bool>.IsCompleted.get""
    IL_0071:  brtrue.s   IL_00b4
    IL_0073:  ldarg.0
    IL_0074:  ldc.i4.0
    IL_0075:  dup
    IL_0076:  stloc.0
    IL_0077:  stfld      ""int C.<F>d__0.<>1__state""
    // async: yield
    IL_007c:  ldarg.0
    IL_007d:  ldloc.2
    IL_007e:  stfld      ""System.Runtime.CompilerServices.ValueTaskAwaiter<bool> C.<F>d__0.<>u__1""
    IL_0083:  ldarg.0
    IL_0084:  stloc.s    V_4
    IL_0086:  ldarg.0
    IL_0087:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<F>d__0.<>t__builder""
    IL_008c:  ldloca.s   V_2
    IL_008e:  ldloca.s   V_4
    IL_0090:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.ValueTaskAwaiter<bool>, C.<F>d__0>(ref System.Runtime.CompilerServices.ValueTaskAwaiter<bool>, ref C.<F>d__0)""
    IL_0095:  nop
    IL_0096:  leave.s    IL_0109
    // async: resume
    IL_0098:  ldarg.0
    IL_0099:  ldfld      ""System.Runtime.CompilerServices.ValueTaskAwaiter<bool> C.<F>d__0.<>u__1""
    IL_009e:  stloc.2
    IL_009f:  ldarg.0
    IL_00a0:  ldflda     ""System.Runtime.CompilerServices.ValueTaskAwaiter<bool> C.<F>d__0.<>u__1""
    IL_00a5:  initobj    ""System.Runtime.CompilerServices.ValueTaskAwaiter<bool>""
    IL_00ab:  ldarg.0
    IL_00ac:  ldc.i4.m1
    IL_00ad:  dup
    IL_00ae:  stloc.0
    IL_00af:  stfld      ""int C.<F>d__0.<>1__state""
    IL_00b4:  ldarg.0
    IL_00b5:  ldloca.s   V_2
    IL_00b7:  call       ""bool System.Runtime.CompilerServices.ValueTaskAwaiter<bool>.GetResult()""
    IL_00bc:  stfld      ""bool C.<F>d__0.<>s__3""
    IL_00c1:  ldarg.0
    IL_00c2:  ldfld      ""bool C.<F>d__0.<>s__3""
    IL_00c7:  brtrue     IL_003c
    IL_00cc:  ldarg.0
    IL_00cd:  ldnull
    IL_00ce:  stfld      ""System.Collections.Generic.IAsyncEnumerator<int> C.<F>d__0.<>s__1""
    // sequence point: End();
    IL_00d3:  call       ""void C.End()""
    IL_00d8:  nop
    IL_00d9:  leave.s    IL_00f5
  }
  catch System.Exception
  {
    // sequence point: <hidden>
    IL_00db:  stloc.s    V_5
    IL_00dd:  ldarg.0
    IL_00de:  ldc.i4.s   -2
    IL_00e0:  stfld      ""int C.<F>d__0.<>1__state""
    IL_00e5:  ldarg.0
    IL_00e6:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<F>d__0.<>t__builder""
    IL_00eb:  ldloc.s    V_5
    IL_00ed:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00f2:  nop
    IL_00f3:  leave.s    IL_0109
  }
  // sequence point: }
  IL_00f5:  ldarg.0
  IL_00f6:  ldc.i4.s   -2
  IL_00f8:  stfld      ""int C.<F>d__0.<>1__state""
  // sequence point: <hidden>
  IL_00fd:  ldarg.0
  IL_00fe:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<F>d__0.<>t__builder""
  IL_0103:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_0108:  nop
  IL_0109:  ret
}");

            var diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

            diff1.VerifyIL("C.<F>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      266 (0x10a)
  .maxstack  3
  .locals init (int V_0,
                System.Threading.CancellationToken V_1,
                System.Runtime.CompilerServices.ValueTaskAwaiter<bool> V_2,
                System.Threading.Tasks.ValueTask<bool> V_3,
                C.<F>d__0 V_4,
                System.Exception V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<F>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0012
    IL_000a:  br.s       IL_000c
    IL_000c:  ldloc.0
    IL_000d:  ldc.i4.1
    IL_000e:  beq.s      IL_0017
    IL_0010:  br.s       IL_0019
    IL_0012:  br         IL_0098
    IL_0017:  br.s       IL_0098
    IL_0019:  nop
    IL_001a:  nop
    IL_001b:  ldarg.0
    IL_001c:  ldarg.0
    IL_001d:  ldfld      ""C C.<F>d__0.<>4__this""
    IL_0022:  call       ""System.Collections.Generic.IAsyncEnumerable<int> C.Iterator()""
    IL_0027:  ldloca.s   V_1
    IL_0029:  initobj    ""System.Threading.CancellationToken""
    IL_002f:  ldloc.1
    IL_0030:  callvirt   ""System.Collections.Generic.IAsyncEnumerator<int> System.Collections.Generic.IAsyncEnumerable<int>.GetAsyncEnumerator(System.Threading.CancellationToken)""
    IL_0035:  stfld      ""System.Collections.Generic.IAsyncEnumerator<int> C.<F>d__0.<>s__1""
    IL_003a:  br.s       IL_0056
    IL_003c:  ldarg.0
    IL_003d:  ldarg.0
    IL_003e:  ldfld      ""System.Collections.Generic.IAsyncEnumerator<int> C.<F>d__0.<>s__1""
    IL_0043:  callvirt   ""int System.Collections.Generic.IAsyncEnumerator<int>.Current.get""
    IL_0048:  stfld      ""int C.<F>d__0.<x>5__2""
    IL_004d:  nop
    IL_004e:  ldc.i4.2
    IL_004f:  call       ""void C.Body(int)""
    IL_0054:  nop
    IL_0055:  nop
    IL_0056:  ldarg.0
    IL_0057:  ldfld      ""System.Collections.Generic.IAsyncEnumerator<int> C.<F>d__0.<>s__1""
    IL_005c:  callvirt   ""System.Threading.Tasks.ValueTask<bool> System.Collections.Generic.IAsyncEnumerator<int>.MoveNextAsync()""
    IL_0061:  stloc.3
    IL_0062:  ldloca.s   V_3
    IL_0064:  call       ""System.Runtime.CompilerServices.ValueTaskAwaiter<bool> System.Threading.Tasks.ValueTask<bool>.GetAwaiter()""
    IL_0069:  stloc.2
    IL_006a:  ldloca.s   V_2
    IL_006c:  call       ""bool System.Runtime.CompilerServices.ValueTaskAwaiter<bool>.IsCompleted.get""
    IL_0071:  brtrue.s   IL_00b4
    IL_0073:  ldarg.0
    IL_0074:  ldc.i4.0
    IL_0075:  dup
    IL_0076:  stloc.0
    IL_0077:  stfld      ""int C.<F>d__0.<>1__state""
    IL_007c:  ldarg.0
    IL_007d:  ldloc.2
    IL_007e:  stfld      ""System.Runtime.CompilerServices.ValueTaskAwaiter<bool> C.<F>d__0.<>u__1""
    IL_0083:  ldarg.0
    IL_0084:  stloc.s    V_4
    IL_0086:  ldarg.0
    IL_0087:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<F>d__0.<>t__builder""
    IL_008c:  ldloca.s   V_2
    IL_008e:  ldloca.s   V_4
    IL_0090:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.ValueTaskAwaiter<bool>, C.<F>d__0>(ref System.Runtime.CompilerServices.ValueTaskAwaiter<bool>, ref C.<F>d__0)""
    IL_0095:  nop
    IL_0096:  leave.s    IL_0109
    IL_0098:  ldarg.0
    IL_0099:  ldfld      ""System.Runtime.CompilerServices.ValueTaskAwaiter<bool> C.<F>d__0.<>u__1""
    IL_009e:  stloc.2
    IL_009f:  ldarg.0
    IL_00a0:  ldflda     ""System.Runtime.CompilerServices.ValueTaskAwaiter<bool> C.<F>d__0.<>u__1""
    IL_00a5:  initobj    ""System.Runtime.CompilerServices.ValueTaskAwaiter<bool>""
    IL_00ab:  ldarg.0
    IL_00ac:  ldc.i4.m1
    IL_00ad:  dup
    IL_00ae:  stloc.0
    IL_00af:  stfld      ""int C.<F>d__0.<>1__state""
    IL_00b4:  ldarg.0
    IL_00b5:  ldloca.s   V_2
    IL_00b7:  call       ""bool System.Runtime.CompilerServices.ValueTaskAwaiter<bool>.GetResult()""
    IL_00bc:  stfld      ""bool C.<F>d__0.<>s__3""
    IL_00c1:  ldarg.0
    IL_00c2:  ldfld      ""bool C.<F>d__0.<>s__3""
    IL_00c7:  brtrue     IL_003c
    IL_00cc:  ldarg.0
    IL_00cd:  ldnull
    IL_00ce:  stfld      ""System.Collections.Generic.IAsyncEnumerator<int> C.<F>d__0.<>s__1""
    IL_00d3:  call       ""void C.End()""
    IL_00d8:  nop
    IL_00d9:  leave.s    IL_00f5
  }
  catch System.Exception
  {
    IL_00db:  stloc.s    V_5
    IL_00dd:  ldarg.0
    IL_00de:  ldc.i4.s   -2
    IL_00e0:  stfld      ""int C.<F>d__0.<>1__state""
    IL_00e5:  ldarg.0
    IL_00e6:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<F>d__0.<>t__builder""
    IL_00eb:  ldloc.s    V_5
    IL_00ed:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00f2:  nop
    IL_00f3:  leave.s    IL_0109
  }
  IL_00f5:  ldarg.0
  IL_00f6:  ldc.i4.s   -2
  IL_00f8:  stfld      ""int C.<F>d__0.<>1__state""
  IL_00fd:  ldarg.0
  IL_00fe:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<F>d__0.<>t__builder""
  IL_0103:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_0108:  nop
  IL_0109:  ret
}
");
        }

        [Fact]
        public void HoistedVariables_MultipleGenerations()
        {
            var source0 = MarkedSource(@"
using System.Threading.Tasks;

class C
{
    static async Task<int> F() // testing type changes G0 -> G1, G1 -> G2
    {
        bool <N:0>a1 = true</N:0>; 
        int <N:1>a2 = 3</N:1>;
        <N:2>await Task.Delay(0)</N:2>;
        return 1;
    }

    static async Task<int> G() // testing G1 -> G3
    {
        C <N:3>c = new C()</N:3>;
        bool <N:4>a1 = true</N:4>;
        <N:5>await Task.Delay(0)</N:5>;
        return 1;
    }

    static async Task<int> H() // testing G0 -> G3
    {
        C <N:6>c = new C()</N:6>;
        bool <N:7>a1 = true</N:7>;
        <N:8>await Task.Delay(0)</N:8>;
        return 1;
    }
}");
            var source1 = MarkedSource(@"
using System.Threading.Tasks;

class C
{
    static async Task<int> F() // updated 
    {
        C <N:0>a1 = new C()</N:0>; 
        int <N:1>a2 = 3</N:1>;
        <N:2>await Task.Delay(0)</N:2>;
        return 1;
    }

    static async Task<int> G() // updated 
    {
        C <N:3>c = new C()</N:3>;
        bool <N:4>a1 = true</N:4>;
        <N:5>await Task.Delay(0)</N:5>;
        return 2;
    }

    static async Task<int> H() 
    {
        C <N:6>c = new C()</N:6>;
        bool <N:7>a1 = true</N:7>;
        <N:8>await Task.Delay(0)</N:8>;
        return 1;
    }
}");
            var source2 = MarkedSource(@"
using System.Threading.Tasks;

class C
{
    static async Task<int> F()  // updated
    {
        bool <N:0>a1 = true</N:0>;
        C <N:1>a2 = new C()</N:1>;
        <N:2>await Task.Delay(0)</N:2>;
        return 1;
    }

    static async Task<int> G()
    {
        C <N:3>c = new C()</N:3>;
        bool <N:4>a1 = true</N:4>;
        <N:5>await Task.Delay(0)</N:5>;
        return 2;
    }

    static async Task<int> H() 
    {
        C <N:6>c = new C()</N:6>;
        bool <N:7>a1 = true</N:7>;
        <N:8>await Task.Delay(0)</N:8>;
        return 1;
    }
}");
            var source3 = MarkedSource(@"
using System.Threading.Tasks;

class C
{
    static async Task<int> F() 
    {
        bool <N:0>a1 = true</N:0>;
        C <N:1>a2 = new C()</N:1>;
        <N:2>await Task.Delay(0)</N:2>;
        return 1;
    }

    static async Task<int> G() // updated
    {
        C <N:3>c = new C()</N:3>;
        C <N:4>a1 = new C()</N:4>;
        <N:5>await Task.Delay(0)</N:5>;
        return 1;
    }

    static async Task<int> H() // updated
    {
        C <N:6>c = new C()</N:6>;
        C <N:7>a1 = new C()</N:7>;
        <N:8>await Task.Delay(0)</N:8>;
        return 1;
    }
}");

            // Rude edit but the compiler should handle it.

            var compilation0 = CreateCompilationWithMscorlib461(source0.Tree, options: ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            var compilation1 = compilation0.WithSource(source1.Tree);
            var compilation2 = compilation1.WithSource(source2.Tree);
            var compilation3 = compilation2.WithSource(source3.Tree);

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
            var syntaxMap1 = GetSyntaxMapFromMarkers(source0, source1);
            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, f0, f1, syntaxMap1),
                    SemanticEdit.Create(SemanticEditKind.Update, g0, g1, syntaxMap1)));

            diff1.VerifySynthesizedMembers(
                "C: {<F>d__0, <G>d__1}",
                "C.<F>d__0: {<>1__state, <>t__builder, <a1>5__3, <a2>5__2, <>u__1, MoveNext, SetStateMachine}",
                "C.<G>d__1: {<>1__state, <>t__builder, <c>5__1, <a1>5__2, <>u__1, MoveNext, SetStateMachine}");

            var syntaxMap2 = GetSyntaxMapFromMarkers(source1, source2);
            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, f1, f2, syntaxMap2)));

            diff2.VerifySynthesizedMembers(
                "C: {<F>d__0, <G>d__1}",
                "C.<F>d__0: {<>1__state, <>t__builder, <a1>5__4, <a2>5__5, <>u__1, MoveNext, SetStateMachine, <a1>5__3, <a2>5__2}",
                "C.<G>d__1: {<>1__state, <>t__builder, <c>5__1, <a1>5__2, <>u__1, MoveNext, SetStateMachine}");

            var syntaxMap3 = GetSyntaxMapFromMarkers(source2, source3);
            var diff3 = compilation3.EmitDifference(
                diff2.NextGeneration,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, g2, g3, syntaxMap3),
                    SemanticEdit.Create(SemanticEditKind.Update, h2, h3, syntaxMap3)));

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
                Row(6, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(9, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(1, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(2, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(6, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(7, TableIndex.CustomAttribute, EditAndContinueOperation.Default));

            diff1.VerifyIL("C.<F>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      192 (0xc0)
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
    IL_0058:  leave.s    IL_00bf
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
    IL_007e:  ldc.i4.1
    IL_007f:  stloc.1
    IL_0080:  leave.s    IL_00a3
  }
  catch System.Exception
  {
    IL_0082:  stloc.s    V_4
    IL_0084:  ldarg.0
    IL_0085:  ldc.i4.s   -2
    IL_0087:  stfld      ""int C.<F>d__0.<>1__state""
    IL_008c:  ldarg.0
    IL_008d:  ldnull
    IL_008e:  stfld      ""C C.<F>d__0.<a1>5__3""
    IL_0093:  ldarg.0
    IL_0094:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__0.<>t__builder""
    IL_0099:  ldloc.s    V_4
    IL_009b:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)""
    IL_00a0:  nop
    IL_00a1:  leave.s    IL_00bf
  }
  IL_00a3:  ldarg.0
  IL_00a4:  ldc.i4.s   -2
  IL_00a6:  stfld      ""int C.<F>d__0.<>1__state""
  IL_00ab:  ldarg.0
  IL_00ac:  ldnull
  IL_00ad:  stfld      ""C C.<F>d__0.<a1>5__3""
  IL_00b2:  ldarg.0
  IL_00b3:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__0.<>t__builder""
  IL_00b8:  ldloc.1
  IL_00b9:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetResult(int)""
  IL_00be:  nop
  IL_00bf:  ret
}");
            // 2 field defs added (both variables a1 and a2 of F changed their types) & 2 methods updated
            CheckEncLogDefinitions(md2.Reader,
                Row(11, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(12, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                Row(17, TableIndex.Field, EditAndContinueOperation.Default),
                Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                Row(18, TableIndex.Field, EditAndContinueOperation.Default),
                Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(6, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(1, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(2, TableIndex.CustomAttribute, EditAndContinueOperation.Default));

            diff2.VerifyIL("C.<F>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      192 (0xc0)
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
    IL_0058:  leave.s    IL_00bf
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
    IL_007e:  ldc.i4.1
    IL_007f:  stloc.1
    IL_0080:  leave.s    IL_00a3
  }
  catch System.Exception
  {
    IL_0082:  stloc.s    V_4
    IL_0084:  ldarg.0
    IL_0085:  ldc.i4.s   -2
    IL_0087:  stfld      ""int C.<F>d__0.<>1__state""
    IL_008c:  ldarg.0
    IL_008d:  ldnull
    IL_008e:  stfld      ""C C.<F>d__0.<a2>5__5""
    IL_0093:  ldarg.0
    IL_0094:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__0.<>t__builder""
    IL_0099:  ldloc.s    V_4
    IL_009b:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)""
    IL_00a0:  nop
    IL_00a1:  leave.s    IL_00bf
  }
  IL_00a3:  ldarg.0
  IL_00a4:  ldc.i4.s   -2
  IL_00a6:  stfld      ""int C.<F>d__0.<>1__state""
  IL_00ab:  ldarg.0
  IL_00ac:  ldnull
  IL_00ad:  stfld      ""C C.<F>d__0.<a2>5__5""
  IL_00b2:  ldarg.0
  IL_00b3:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__0.<>t__builder""
  IL_00b8:  ldloc.1
  IL_00b9:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetResult(int)""
  IL_00be:  nop
  IL_00bf:  ret
}");
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
                Row(9, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(12, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(6, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(7, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(8, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(9, TableIndex.CustomAttribute, EditAndContinueOperation.Default));
        }

        [Fact]
        public void HoistedVariables_Dynamic1()
        {
            var template = @"
using System;
using System.Collections.Generic;

class C
{
    public IEnumerable<int> F()
    {
        dynamic <N:0>x = 1</N:0>;
        <N:1>yield return 1;</N:1>
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

            var baselineIL0 = @"
{
  // Code size      147 (0x93)
  .maxstack  3
  .locals init (int V_0)
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
  IL_0012:  br.s       IL_0018
  IL_0014:  br.s       IL_003c
  IL_0016:  ldc.i4.0
  IL_0017:  ret
  IL_0018:  ldarg.0
  IL_0019:  ldc.i4.m1
  IL_001a:  stfld      ""int C.<F>d__0.<>1__state""
  IL_001f:  nop
  IL_0020:  ldarg.0
  IL_0021:  ldc.i4.1
  IL_0022:  box        ""int""
  IL_0027:  stfld      ""dynamic C.<F>d__0.<x>5__1""
  IL_002c:  ldarg.0
  IL_002d:  ldc.i4.1
  IL_002e:  stfld      ""int C.<F>d__0.<>2__current""
  IL_0033:  ldarg.0
  IL_0034:  ldc.i4.1
  IL_0035:  stfld      ""int C.<F>d__0.<>1__state""
  IL_003a:  ldc.i4.1
  IL_003b:  ret
  IL_003c:  ldarg.0
  IL_003d:  ldc.i4.m1
  IL_003e:  stfld      ""int C.<F>d__0.<>1__state""
  IL_0043:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>> C.<>o__0.<>p__0""
  IL_0048:  brfalse.s  IL_004c
  IL_004a:  br.s       IL_0071
  IL_004c:  ldc.i4.s   16
  IL_004e:  ldtoken    ""int""
  IL_0053:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0058:  ldtoken    ""C""
  IL_005d:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0062:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.Convert(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Type)""
  IL_0067:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_006c:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>> C.<>o__0.<>p__0""
  IL_0071:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>> C.<>o__0.<>p__0""
  IL_0076:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>>.Target""
  IL_007b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>> C.<>o__0.<>p__0""
  IL_0080:  ldarg.0
  IL_0081:  ldfld      ""dynamic C.<F>d__0.<x>5__1""
  IL_0086:  callvirt   ""int System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_008b:  call       ""void System.Console.WriteLine(int)""
  IL_0090:  nop
  IL_0091:  ldc.i4.0
  IL_0092:  ret
}";
            v0.VerifyIL("C.<F>d__0.System.Collections.IEnumerator.MoveNext()", baselineIL0);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

            var baselineIL = @"
{
  // Code size      149 (0x95)
  .maxstack  3
  .locals init (int V_0)
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
  IL_0012:  br.s       IL_0018
  IL_0014:  br.s       IL_003c
  IL_0016:  ldc.i4.0
  IL_0017:  ret
  IL_0018:  ldarg.0
  IL_0019:  ldc.i4.m1
  IL_001a:  stfld      ""int C.<F>d__0.<>1__state""
  IL_001f:  nop
  IL_0020:  ldarg.0
  IL_0021:  ldc.i4.1
  IL_0022:  box        ""int""
  IL_0027:  stfld      ""dynamic C.<F>d__0.<x>5__1""
  IL_002c:  ldarg.0
  IL_002d:  ldc.i4.1
  IL_002e:  stfld      ""int C.<F>d__0.<>2__current""
  IL_0033:  ldarg.0
  IL_0034:  ldc.i4.1
  IL_0035:  stfld      ""int C.<F>d__0.<>1__state""
  IL_003a:  ldc.i4.1
  IL_003b:  ret
  IL_003c:  ldarg.0
  IL_003d:  ldc.i4.m1
  IL_003e:  stfld      ""int C.<F>d__0.<>1__state""
  IL_0043:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>> C.<<DYNAMIC_CONTAINER_NAME>>.<>p__0""
  IL_0048:  brfalse.s  IL_004c
  IL_004a:  br.s       IL_0071
  IL_004c:  ldc.i4.s   16
  IL_004e:  ldtoken    ""int""
  IL_0053:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0058:  ldtoken    ""C""
  IL_005d:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0062:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.Convert(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Type)""
  IL_0067:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_006c:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>> C.<<DYNAMIC_CONTAINER_NAME>>.<>p__0""
  IL_0071:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>> C.<<DYNAMIC_CONTAINER_NAME>>.<>p__0""
  IL_0076:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>>.Target""
  IL_007b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>> C.<<DYNAMIC_CONTAINER_NAME>>.<>p__0""
  IL_0080:  ldarg.0
  IL_0081:  ldfld      ""dynamic C.<F>d__0.<x>5__1""
  IL_0086:  callvirt   ""int System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_008b:  ldc.i4.<<VALUE>>
  IL_008c:  add
  IL_008d:  call       ""void System.Console.WriteLine(int)""
  IL_0092:  nop
  IL_0093:  ldc.i4.0
  IL_0094:  ret
}";

            diff1.VerifySynthesizedMembers(
                "C: {<>o__0#1, <F>d__0}",
                "C.<>o__0#1: {<>p__0}",
                "C.<F>d__0: {<>1__state, <>2__current, <>l__initialThreadId, <>4__this, <x>5__1, System.IDisposable.Dispose, MoveNext, System.Collections.Generic.IEnumerator<System.Int32>.get_Current, System.Collections.IEnumerator.Reset, System.Collections.IEnumerator.get_Current, System.Collections.Generic.IEnumerable<System.Int32>.GetEnumerator, System.Collections.IEnumerable.GetEnumerator, System.Collections.Generic.IEnumerator<System.Int32>.Current, System.Collections.IEnumerator.Current}");

            diff1.VerifyIL("C.<F>d__0.System.Collections.IEnumerator.MoveNext()", baselineIL.Replace("<<VALUE>>", "1").Replace("<<DYNAMIC_CONTAINER_NAME>>", "<>o__0#1"));

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, f1, f2, GetSyntaxMapFromMarkers(source1, source2))));

            diff2.VerifySynthesizedMembers(
                "C: {<>o__0#2, <F>d__0, <>o__0#1}",
                "C.<>o__0#1: {<>p__0}",
                "C.<>o__0#2: {<>p__0}",
                "C.<F>d__0: {<>1__state, <>2__current, <>l__initialThreadId, <>4__this, <x>5__1, System.IDisposable.Dispose, MoveNext, System.Collections.Generic.IEnumerator<System.Int32>.get_Current, System.Collections.IEnumerator.Reset, System.Collections.IEnumerator.get_Current, System.Collections.Generic.IEnumerable<System.Int32>.GetEnumerator, System.Collections.IEnumerable.GetEnumerator, System.Collections.Generic.IEnumerator<System.Int32>.Current, System.Collections.IEnumerator.Current}");

            diff2.VerifyIL("C.<F>d__0.System.Collections.IEnumerator.MoveNext()", baselineIL.Replace("<<VALUE>>", "2").Replace("<<DYNAMIC_CONTAINER_NAME>>", "<>o__0#2"));
        }

        [Fact]
        public void HoistedVariables_Dynamic2()
        {
            using var _ = new EditAndContinueTest(references: [CSharpRef])
                .AddBaseline(
                    source: """
                    using System;
                    using System.Collections.Generic;

                    class C
                    {
                        private static IEnumerable<string> F()
                        {
                            dynamic <N:0>d = "x"</N:0>;
                            yield return d;
                            Console.WriteLine(0);
                        }
                    }
                    """)
                .AddGeneration(
                    // 1
                    source: """
                    using System;
                    using System.Collections.Generic;

                    class C
                    {
                        private static IEnumerable<string> F()
                        {
                            dynamic <N:0>d = "x"</N:0>;
                            yield return d.ToString();
                            Console.WriteLine(1);
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
                            "C: {<>o__0#1, <F>d__0}",
                            "C.<>o__0#1: {<>p__0, <>p__1}",
                            "C.<F>d__0: {<>1__state, <>2__current, <>l__initialThreadId, <d>5__1, System.IDisposable.Dispose, MoveNext, System.Collections.Generic.IEnumerator<System.String>.get_Current, System.Collections.IEnumerator.Reset, System.Collections.IEnumerator.get_Current, System.Collections.Generic.IEnumerable<System.String>.GetEnumerator, System.Collections.IEnumerable.GetEnumerator, System.Collections.Generic.IEnumerator<System.String>.Current, System.Collections.IEnumerator.Current}");
                    })
                .AddGeneration(
                    // 2
                    source: """
                    using System;
                    using System.Collections.Generic;
                    
                    class C
                    {
                        private static IEnumerable<string> F()
                        {
                            dynamic <N:0>d = "x"</N:0>;
                            yield return d;
                            Console.WriteLine(2);
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
                            "C: {<>o__0#2, <F>d__0, <>o__0#1}",
                            "C.<>o__0#1: {<>p__0, <>p__1}",
                            "C.<>o__0#2: {<>p__0}",
                            "C.<F>d__0: {<>1__state, <>2__current, <>l__initialThreadId, <d>5__1, System.IDisposable.Dispose, MoveNext, System.Collections.Generic.IEnumerator<System.String>.get_Current, System.Collections.IEnumerator.Reset, System.Collections.IEnumerator.get_Current, System.Collections.Generic.IEnumerable<System.String>.GetEnumerator, System.Collections.IEnumerable.GetEnumerator, System.Collections.Generic.IEnumerator<System.String>.Current, System.Collections.IEnumerator.Current}");
                    })
                .Verify();
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
            var compilation0 = CreateCompilationWithMscorlib461(source0, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), options: ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All));

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

        [Theory]
        [MemberData(nameof(ExternalPdbFormats))]
        public void Awaiters_MultipleGenerations(DebugInformationFormat format)
        {
            var source0 = MarkedSource(@"
using System.Threading.Tasks;

class C
{
    static Task<bool> A1() => null;
    static Task<int> A2() => null;
    static Task<C> A3() => null;

    static async Task<int> F() // testing type changes G0 -> G1, G1 -> G2
    {
        <N:0>await A1()</N:0>;
        <N:1>await A2()</N:1>;
        return 1;
    }

    static async Task<int> G() // testing G1 -> G3
    {
        <N:2>await A1()</N:2>;
        return 1;
    }

    static async Task<int> H() // testing G0 -> G3
    {
        <N:3>await A1()</N:3>;
        return 1;
    }
}");
            var source1 = MarkedSource(@"
using System.Threading.Tasks;

class C
{
    static Task<bool> A1() => null;
    static Task<int> A2() => null;
    static Task<C> A3() => null;

    static async Task<int> F() // updated 
    {
        <N:0>await A3()</N:0>; 
        <N:1>await A2()</N:1>;
        return 1;
    }

    static async Task<int> G() // updated 
    {
        <N:2>await A1()</N:2>;
        return 2;
    }

    static async Task<int> H() 
    {
        <N:3>await A1()</N:3>;
        return 1;
    }
}");
            var source2 = MarkedSource(@"
using System.Threading.Tasks;

class C
{
    static Task<bool> A1() => null;
    static Task<int> A2() => null;
    static Task<C> A3() => null;

    static async Task<int> F()  // updated
    {
        <N:0>await A1()</N:0>; 
        <N:1>await A3()</N:1>;
        return 1;
    }

    static async Task<int> G()
    {
        <N:2>await A1()</N:2>;
        return 2;
    }

    static async Task<int> H() 
    {
        <N:3>await A1()</N:3>;
        return 1;
    }
}");
            var source3 = MarkedSource(@"
using System.Threading.Tasks;

class C
{
    static Task<bool> A1() => null;
    static Task<int> A2() => null;
    static Task<C> A3() => null;

    static async Task<int> F() 
    {
        <N:0>await A1()</N:0>; 
        <N:1>await A3()</N:1>;
        return 1;
    }

    static async Task<int> G() // updated
    {
        <N:2>await A3()</N:2>;
        return 1;
    }

    static async Task<int> H() // updated
    {
        <N:3>await A3()</N:3>;
        return 1;
    }
}");

            // Rude edit but the compiler should handle it.

            var compilation0 = CreateCompilationWithMscorlib461(source0.Tree, options: ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All), assemblyName: "A");
            var compilation1 = compilation0.WithSource(source1.Tree);
            var compilation2 = compilation1.WithSource(source2.Tree);
            var compilation3 = compilation2.WithSource(source3.Tree);

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

            var v0 = CompileAndVerify(compilation0, emitOptions: EmitOptions.Default.WithDebugInformationFormat(format), symbolValidator: module =>
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

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);
            var syntaxMap1 = GetSyntaxMapFromMarkers(source0, source1);
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, f0, f1, syntaxMap1),
                    SemanticEdit.Create(SemanticEditKind.Update, g0, g1, syntaxMap1)));

            diff1.VerifySynthesizedMembers(
                "C: {<F>d__3, <G>d__4}",
                "C.<F>d__3: {<>1__state, <>t__builder, <>u__3, <>u__2, MoveNext, SetStateMachine}",
                "C.<G>d__4: {<>1__state, <>t__builder, <>u__1, MoveNext, SetStateMachine}");

            var syntaxMap2 = GetSyntaxMapFromMarkers(source1, source2);
            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, f1, f2, syntaxMap2)));

            diff2.VerifySynthesizedMembers(
                "C: {<F>d__3, <G>d__4}",
                "C.<F>d__3: {<>1__state, <>t__builder, <>u__4, <>u__3, MoveNext, SetStateMachine, <>u__2}",
                "C.<G>d__4: {<>1__state, <>t__builder, <>u__1, MoveNext, SetStateMachine}");

            var syntaxMap3 = GetSyntaxMapFromMarkers(source2, source3);
            var diff3 = compilation3.EmitDifference(
                diff2.NextGeneration,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, g2, g3, syntaxMap3),
                    SemanticEdit.Create(SemanticEditKind.Update, h2, h3, syntaxMap3)));

            diff3.VerifySynthesizedMembers(
                "C: {<G>d__4, <H>d__5, <F>d__3}",
                "C.<G>d__4: {<>1__state, <>t__builder, <>u__2, MoveNext, SetStateMachine, <>u__1}",
                "C.<H>d__5: {<>1__state, <>t__builder, <>u__2, MoveNext, SetStateMachine}",
                "C.<F>d__3: {<>1__state, <>t__builder, <>u__4, <>u__3, MoveNext, SetStateMachine, <>u__2}");

            // Verify delta metadata contains expected rows.
            var md1 = diff1.GetMetadata();
            var md2 = diff2.GetMetadata();
            var md3 = diff3.GetMetadata();

            diff1.VerifyPdb(new[] { MetadataTokens.MethodDefinitionHandle(9) }, @"
    <symbols>
      <files>
        <file id=""1"" name="""" language =""C#"" />
      </files>
      <methods>
        <method token=""0x6000009"">
          <customDebugInfo>
            <using>
              <namespace usingCount=""1"" />
            </using>
          </customDebugInfo>
          <sequencePoints>
            <entry offset=""0x0"" hidden=""true"" document=""1"" />
            <entry offset=""0x7"" hidden=""true"" document=""1"" />
            <entry offset=""0x19"" startLine=""11"" startColumn=""5"" endLine=""11"" endColumn=""6"" document=""1"" />
            <entry offset=""0x1a"" startLine=""12"" startColumn=""14"" endLine=""12"" endColumn=""31"" document=""1"" />
            <entry offset=""0x25"" hidden=""true"" document=""1"" />
            <entry offset=""0x79"" startLine=""13"" startColumn=""14"" endLine=""13"" endColumn=""31"" document=""1"" />
            <entry offset=""0x85"" hidden=""true"" document=""1"" />
            <entry offset=""0xd8"" startLine=""14"" startColumn=""9"" endLine=""14"" endColumn=""18"" document=""1"" />
            <entry offset=""0xdc"" hidden=""true"" document=""1"" />
            <entry offset=""0xf6"" startLine=""15"" startColumn=""5"" endLine=""15"" endColumn=""6"" document=""1"" />
            <entry offset=""0xfe"" hidden=""true"" document=""1"" />
          </sequencePoints>
          <scope startOffset=""0x0"" endOffset=""0x10c"">
            <namespace name=""System.Threading.Tasks"" />
          </scope>
          <asyncInfo>
            <kickoffMethod token=""0x6000004"" />
            <await yield=""0x37"" resume=""0x55"" token=""0x6000009"" />
            <await yield=""0x97"" resume=""0xb3"" token=""0x6000009"" />
          </asyncInfo>
        </method>
      </methods>
    </symbols>");

            // 1 field def added & 4 methods updated (MoveNext and kickoff for F and G)
            CheckEncLogDefinitions(md1.Reader,
                Row(7, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(8, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(9, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(10, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                Row(11, TableIndex.Field, EditAndContinueOperation.Default),
                Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(9, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(12, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(5, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(6, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(8, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(9, TableIndex.CustomAttribute, EditAndContinueOperation.Default));

            // Note that the new awaiter is allocated slot <>u__3 since <>u__1 and <>u__2 are taken.
            diff1.VerifyIL("C.<F>d__3.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      268 (0x10c)
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
    IL_0014:  br         IL_00b3
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
    IL_0050:  leave      IL_010b
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
    IL_0079:  call       ""System.Threading.Tasks.Task<int> C.A2()""
    IL_007e:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0083:  stloc.s    V_4
    IL_0085:  ldloca.s   V_4
    IL_0087:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_008c:  brtrue.s   IL_00d0
    IL_008e:  ldarg.0
    IL_008f:  ldc.i4.1
    IL_0090:  dup
    IL_0091:  stloc.0
    IL_0092:  stfld      ""int C.<F>d__3.<>1__state""
    IL_0097:  ldarg.0
    IL_0098:  ldloc.s    V_4
    IL_009a:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> C.<F>d__3.<>u__2""
    IL_009f:  ldarg.0
    IL_00a0:  stloc.3
    IL_00a1:  ldarg.0
    IL_00a2:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__3.<>t__builder""
    IL_00a7:  ldloca.s   V_4
    IL_00a9:  ldloca.s   V_3
    IL_00ab:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, C.<F>d__3>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref C.<F>d__3)""
    IL_00b0:  nop
    IL_00b1:  leave.s    IL_010b
    IL_00b3:  ldarg.0
    IL_00b4:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> C.<F>d__3.<>u__2""
    IL_00b9:  stloc.s    V_4
    IL_00bb:  ldarg.0
    IL_00bc:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> C.<F>d__3.<>u__2""
    IL_00c1:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_00c7:  ldarg.0
    IL_00c8:  ldc.i4.m1
    IL_00c9:  dup
    IL_00ca:  stloc.0
    IL_00cb:  stfld      ""int C.<F>d__3.<>1__state""
    IL_00d0:  ldloca.s   V_4
    IL_00d2:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_00d7:  pop
    IL_00d8:  ldc.i4.1
    IL_00d9:  stloc.1
    IL_00da:  leave.s    IL_00f6
  }
  catch System.Exception
  {
    IL_00dc:  stloc.s    V_5
    IL_00de:  ldarg.0
    IL_00df:  ldc.i4.s   -2
    IL_00e1:  stfld      ""int C.<F>d__3.<>1__state""
    IL_00e6:  ldarg.0
    IL_00e7:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__3.<>t__builder""
    IL_00ec:  ldloc.s    V_5
    IL_00ee:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)""
    IL_00f3:  nop
    IL_00f4:  leave.s    IL_010b
  }
  IL_00f6:  ldarg.0
  IL_00f7:  ldc.i4.s   -2
  IL_00f9:  stfld      ""int C.<F>d__3.<>1__state""
  IL_00fe:  ldarg.0
  IL_00ff:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__3.<>t__builder""
  IL_0104:  ldloc.1
  IL_0105:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetResult(int)""
  IL_010a:  nop
  IL_010b:  ret
}");
            // 1 field def added & 2 methods updated
            CheckEncLogDefinitions(md2.Reader,
                Row(11, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(12, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                Row(12, TableIndex.Field, EditAndContinueOperation.Default),
                Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(9, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(5, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(6, TableIndex.CustomAttribute, EditAndContinueOperation.Default));

            diff2.VerifyPdb(new[] { MetadataTokens.MethodDefinitionHandle(9) }, @"
    <symbols>
      <files>
        <file id=""1"" name="""" language=""C#"" />
      </files>
      <methods>
        <method token=""0x6000009"">
          <customDebugInfo>
            <using>
              <namespace usingCount=""1"" />
            </using>
          </customDebugInfo>
          <sequencePoints>
            <entry offset=""0x0"" hidden=""true"" document=""1"" />
            <entry offset=""0x7"" hidden=""true"" document=""1"" />
            <entry offset=""0x19"" startLine=""11"" startColumn=""5"" endLine=""11"" endColumn=""6"" document=""1"" />
            <entry offset=""0x1a"" startLine=""12"" startColumn=""14"" endLine=""12"" endColumn=""31"" document=""1"" />
            <entry offset=""0x25"" hidden=""true"" document=""1"" />
            <entry offset=""0x79"" startLine=""13"" startColumn=""14"" endLine=""13"" endColumn=""31"" document=""1"" />
            <entry offset=""0x85"" hidden=""true"" document=""1"" />
            <entry offset=""0xd8"" startLine=""14"" startColumn=""9"" endLine=""14"" endColumn=""18"" document=""1"" />
            <entry offset=""0xdc"" hidden=""true"" document=""1"" />
            <entry offset=""0xf6"" startLine=""15"" startColumn=""5"" endLine=""15"" endColumn=""6"" document=""1"" />
            <entry offset=""0xfe"" hidden=""true"" document=""1"" />
          </sequencePoints>
          <scope startOffset=""0x0"" endOffset=""0x10c"">
            <namespace name=""System.Threading.Tasks"" />
          </scope>
          <asyncInfo>
            <kickoffMethod token=""0x6000004"" />
            <await yield=""0x37"" resume=""0x55"" token=""0x6000009"" />
            <await yield=""0x97"" resume=""0xb3"" token=""0x6000009"" />
          </asyncInfo>
        </method>
      </methods>
    </symbols>");

            // Note that the new awaiters are allocated slots <>u__4, <>u__5.
            diff2.VerifyIL("C.<F>d__3.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      268 (0x10c)
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
    IL_0014:  br         IL_00b3
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
    IL_0050:  leave      IL_010b
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
    IL_0079:  call       ""System.Threading.Tasks.Task<C> C.A3()""
    IL_007e:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<C> System.Threading.Tasks.Task<C>.GetAwaiter()""
    IL_0083:  stloc.s    V_4
    IL_0085:  ldloca.s   V_4
    IL_0087:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<C>.IsCompleted.get""
    IL_008c:  brtrue.s   IL_00d0
    IL_008e:  ldarg.0
    IL_008f:  ldc.i4.1
    IL_0090:  dup
    IL_0091:  stloc.0
    IL_0092:  stfld      ""int C.<F>d__3.<>1__state""
    IL_0097:  ldarg.0
    IL_0098:  ldloc.s    V_4
    IL_009a:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<C> C.<F>d__3.<>u__3""
    IL_009f:  ldarg.0
    IL_00a0:  stloc.3
    IL_00a1:  ldarg.0
    IL_00a2:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__3.<>t__builder""
    IL_00a7:  ldloca.s   V_4
    IL_00a9:  ldloca.s   V_3
    IL_00ab:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<C>, C.<F>d__3>(ref System.Runtime.CompilerServices.TaskAwaiter<C>, ref C.<F>d__3)""
    IL_00b0:  nop
    IL_00b1:  leave.s    IL_010b
    IL_00b3:  ldarg.0
    IL_00b4:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<C> C.<F>d__3.<>u__3""
    IL_00b9:  stloc.s    V_4
    IL_00bb:  ldarg.0
    IL_00bc:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<C> C.<F>d__3.<>u__3""
    IL_00c1:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<C>""
    IL_00c7:  ldarg.0
    IL_00c8:  ldc.i4.m1
    IL_00c9:  dup
    IL_00ca:  stloc.0
    IL_00cb:  stfld      ""int C.<F>d__3.<>1__state""
    IL_00d0:  ldloca.s   V_4
    IL_00d2:  call       ""C System.Runtime.CompilerServices.TaskAwaiter<C>.GetResult()""
    IL_00d7:  pop
    IL_00d8:  ldc.i4.1
    IL_00d9:  stloc.1
    IL_00da:  leave.s    IL_00f6
  }
  catch System.Exception
  {
    IL_00dc:  stloc.s    V_5
    IL_00de:  ldarg.0
    IL_00df:  ldc.i4.s   -2
    IL_00e1:  stfld      ""int C.<F>d__3.<>1__state""
    IL_00e6:  ldarg.0
    IL_00e7:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__3.<>t__builder""
    IL_00ec:  ldloc.s    V_5
    IL_00ee:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)""
    IL_00f3:  nop
    IL_00f4:  leave.s    IL_010b
  }
  IL_00f6:  ldarg.0
  IL_00f7:  ldc.i4.s   -2
  IL_00f9:  stfld      ""int C.<F>d__3.<>1__state""
  IL_00fe:  ldarg.0
  IL_00ff:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__3.<>t__builder""
  IL_0104:  ldloc.1
  IL_0105:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetResult(int)""
  IL_010a:  nop
  IL_010b:  ret
}");
            // 2 field defs added - G and H awaiters & 4 methods updated: G, H kickoff and MoveNext
            CheckEncLogDefinitions(md3.Reader,
                Row(13, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(14, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(15, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(16, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                Row(13, TableIndex.Field, EditAndContinueOperation.Default),
                Row(5, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                Row(14, TableIndex.Field, EditAndContinueOperation.Default),
                Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(6, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(12, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(15, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(8, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(9, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(11, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(12, TableIndex.CustomAttribute, EditAndContinueOperation.Default));

            diff3.VerifyPdb(new[] { MetadataTokens.MethodDefinitionHandle(15) }, @"
    <symbols>
      <files>
        <file id=""1"" name="""" language=""C#"" />
      </files>
      <methods>
        <method token=""0x600000f"">
          <customDebugInfo>
            <forward token=""0x600000c"" />
          </customDebugInfo>
          <sequencePoints>
            <entry offset=""0x0"" hidden=""true"" document=""1"" />
            <entry offset=""0x7"" hidden=""true"" document=""1"" />
            <entry offset=""0xe"" startLine=""24"" startColumn=""5"" endLine=""24"" endColumn=""6"" document=""1"" />
            <entry offset=""0xf"" startLine=""25"" startColumn=""14"" endLine=""25"" endColumn=""31"" document=""1"" />
            <entry offset=""0x1a"" hidden=""true"" document=""1"" />
            <entry offset=""0x6b"" startLine=""26"" startColumn=""9"" endLine=""26"" endColumn=""18"" document=""1"" />
            <entry offset=""0x6f"" hidden=""true"" document=""1"" />
            <entry offset=""0x89"" startLine=""27"" startColumn=""5"" endLine=""27"" endColumn=""6"" document=""1"" />
            <entry offset=""0x91"" hidden=""true"" document=""1"" />
          </sequencePoints>
          <asyncInfo>
            <kickoffMethod token=""0x6000006"" />
            <await yield=""0x2c"" resume=""0x47"" token=""0x600000f"" />
          </asyncInfo>
        </method>
      </methods>
    </symbols>");
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

            var compilation0 = CreateCompilationWithMscorlib461(source0, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), options: ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All), assemblyName: "A");
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

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Insert, null, f1)));

            diff1.VerifySynthesizedMembers(
                "C: {<F>d__0#1}",
                "C.<F>d__0#1: {<>1__state, <>2__current, <>l__initialThreadId, System.IDisposable.Dispose, MoveNext, System.Collections.Generic.IEnumerator<System.Int32>.get_Current, System.Collections.IEnumerator.Reset, System.Collections.IEnumerator.get_Current, System.Collections.Generic.IEnumerable<System.Int32>.GetEnumerator, System.Collections.IEnumerable.GetEnumerator, System.Collections.Generic.IEnumerator<System.Int32>.Current, System.Collections.IEnumerator.Current}");

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, f1, f2, GetSyntaxMapByKind(f1, SyntaxKind.Block))));

            diff2.VerifySynthesizedMembers(
                "C: {<F>d__0#1}",
                "C.<F>d__0#1: {<>1__state, <>2__current, <>l__initialThreadId, System.IDisposable.Dispose, MoveNext, System.Collections.Generic.IEnumerator<System.Int32>.get_Current, System.Collections.IEnumerator.Reset, System.Collections.IEnumerator.get_Current, System.Collections.Generic.IEnumerable<System.Int32>.GetEnumerator, System.Collections.IEnumerable.GetEnumerator, System.Collections.Generic.IEnumerator<System.Int32>.Current, System.Collections.IEnumerator.Current}");

            var diff3 = compilation3.EmitDifference(
                diff2.NextGeneration,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Insert, null, g3)));

            diff3.VerifySynthesizedMembers(
                "C: {<F>d__0#1}",
                "C.<F>d__0#1: {<>1__state, <>2__current, <>l__initialThreadId, System.IDisposable.Dispose, MoveNext, System.Collections.Generic.IEnumerator<System.Int32>.get_Current, System.Collections.IEnumerator.Reset, System.Collections.IEnumerator.get_Current, System.Collections.Generic.IEnumerable<System.Int32>.GetEnumerator, System.Collections.IEnumerable.GetEnumerator, System.Collections.Generic.IEnumerator<System.Int32>.Current, System.Collections.IEnumerator.Current}");

            var diff4 = compilation4.EmitDifference(
                diff3.NextGeneration,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Insert, null, h4)));

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

            var compilation0 = CreateCompilationWithMscorlib461(source0, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), options: ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All), assemblyName: "A");
            var compilation1 = compilation0.WithSource(source1);
            var compilation2 = compilation1.WithSource(source2);

            var f_int1 = compilation1.GetMembers("C.F").Single(m => m.ToString() == "C.F(int)");
            var f_byte2 = compilation2.GetMembers("C.F").Single(m => m.ToString() == "C.F(byte)");

            var v0 = CompileAndVerify(compilation0);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Insert, null, f_int1)));

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Insert, null, f_byte2)));

            var reader0 = md0.MetadataReader;
            var reader1 = diff1.GetMetadata().Reader;
            var reader2 = diff2.GetMetadata().Reader;

            CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "C", "<F>d__0");
            CheckNames(new[] { reader0, reader1 }, reader1.GetTypeDefNames(), "<F>d__0#1");
            CheckNames(new[] { reader0, reader1, reader2 }, reader2.GetTypeDefNames(), "<F>d__1#2");
        }

        [Fact]
        public void AsyncLambda_Update()
        {
            var source0 = MarkedSource(
@"using System;
using System.Threading.Tasks;
class C
{
    static void F()
    {
        Func<Task> <N:0>g1 = <N:1>async () =>
        {
            await A1(); 
            await A2();
        }</N:1></N:0>;
    }

    static Task<bool> A1() => null;
    static Task<int> A2() => null;
    static Task<double> A3() => null;
}");
            var source1 = MarkedSource(
@"using System;
using System.Threading.Tasks;
class C
{
    static int G() => 1;

    static void F()
    {
        Func<Task> <N:0>g1 = <N:1>async () =>
        {
            await A2(); 
            await A1();
        }</N:1></N:0>;
    }

    static Task<bool> A1() => null;
    static Task<int> A2() => null;
    static Task<double> A3() => null;
}");
            var source2 = MarkedSource(
 @"using System;
using System.Threading.Tasks;
class C
{
    static int G() => 1;

    static void F()
    {
        Func<Task> <N:0>g1 = <N:1>async () =>
        {
            await A1(); 
            await A2();
        }</N:1></N:0>;
    }

    static Task<bool> A1() => null;
    static Task<int> A2() => null;
    static Task<double> A3() => null;
}");

            var compilation0 = CreateCompilationWithMscorlib461(new[] { source0.Tree }, options: ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            var compilation1 = compilation0.WithSource(source1.Tree);
            var compilation2 = compilation1.WithSource(source2.Tree);

            var v0 = CompileAndVerify(compilation0, symbolValidator: module =>
            {
                Assert.Equal(new[]
                {
                    "<>1__state: int",
                    "<>t__builder: System.Runtime.CompilerServices.AsyncTaskMethodBuilder",
                    "<>4__this: C.<>c",
                    "<>u__1: System.Runtime.CompilerServices.TaskAwaiter<bool>",
                    "<>u__2: System.Runtime.CompilerServices.TaskAwaiter<int>"
                }, module.GetFieldNamesAndTypes("C.<>c.<<F>b__0_0>d"));
            });

            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");
            var f2 = compilation2.GetMember<MethodSymbol>("C.F");

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

            // note that the types of the awaiter fields <>u__1, <>u__2 are the same as in the previous generation:
            diff1.VerifySynthesizedFields("C.<>c.<<F>b__0_0>d",
                "<>1__state: int",
                "<>t__builder: System.Runtime.CompilerServices.AsyncTaskMethodBuilder",
                "<>4__this: C.<>c",
                "<>u__1: System.Runtime.CompilerServices.TaskAwaiter<bool>",
                "<>u__2: System.Runtime.CompilerServices.TaskAwaiter<int>");

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(SemanticEdit.Create(SemanticEditKind.Update, f1, f2, GetSyntaxMapFromMarkers(source1, source2))));

            // note that the types of the awaiter fields <>u__1, <>u__2 are the same as in the previous generation:
            diff2.VerifySynthesizedFields("C.<>c.<<F>b__0_0>d",
                "<>1__state: int",
                "<>t__builder: System.Runtime.CompilerServices.AsyncTaskMethodBuilder",
                "<>4__this: C.<>c",
                "<>u__1: System.Runtime.CompilerServices.TaskAwaiter<bool>",
                "<>u__2: System.Runtime.CompilerServices.TaskAwaiter<int>");
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/72887")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72887")]
        public void AsyncLambda_Delete()
        {
            using var _ = new EditAndContinueTest()
            .AddBaseline("""
                using System.Threading.Tasks;

                class C
                {
                    static void F()
                    {
                        Task.Run(async () =>
                        {
                            await Task.FromResult(1);
                        });
                    }
                }
                """)
            .AddGeneration("""
                using System.Threading.Tasks;
                
                class C
                {
                    static void F()
                    {
                        
                    }
                }
                """,
                edits:
                [
                    Edit(SemanticEditKind.Update, c => c.GetMember("C.F"), preserveLocalVariables: true),
                ],
                validator: v =>
                {
                    v.VerifySynthesizedMembers();

                    v.VerifyMethodDefNames("F", "<F>b__1_0", "MoveNext");

                    v.VerifyEncLogDefinitions(
                    [
                        Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default)
                    ]);

                    v.VerifyIL("""
                        {
                          // Code size        2 (0x2)
                          .maxstack  8
                          IL_0000:  nop
                          IL_0001:  ret
                        }
                        {
                          // Code size       11 (0xb)
                          .maxstack  8
                          IL_0000:  ldstr      0x70000005
                          IL_0005:  newobj     0x0A000017
                          IL_000a:  throw
                        }
                        {
                          // Code size       11 (0xb)
                          .maxstack  8
                          IL_0000:  ldstr      0x70000005
                          IL_0005:  newobj     0x0A000017
                          IL_000a:  throw
                        }
                        """);
                })
            .Verify();
        }

        [Fact, WorkItem(63294, "https://github.com/dotnet/roslyn/issues/63294")]
        public void LiftedClosure()
        {
            var source0 = MarkedSource(@"
using System.Threading.Tasks;
static class C
{
    static async Task M()
    <N:0>{
        int <N:1>num</N:1> = 1;
        F();
                        
        <N:2>await Task.Delay(1)</N:2>;
                        
        <N:3>int F() => num;</N:3>
    }</N:0>
}");

            var source1 = MarkedSource(@"
using System.Threading.Tasks;
static class C
{
    static async Task M()
    <N:0>{
        int <N:1>num</N:1> = 1;
        F();
                        
        <N:2>await Task.Delay(2)</N:2>;
                        
        <N:3>int F() => num;</N:3>
    }</N:0>
}");
            var compilation0 = CreateCompilationWithMscorlib461(new[] { source0.Tree }, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);

            var m0 = compilation0.GetMember<MethodSymbol>("C.M");
            var m1 = compilation1.GetMember<MethodSymbol>("C.M");

            var v0 = CompileAndVerify(compilation0);
            using var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);
            var reader0 = md0.MetadataReader;
            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            // Notice encLocalSlotMap CDI on both M and MoveNext methods.
            // The former is used to calculate mapping for variables lifted to fields of the state machine,
            // the latter is used to map local variable slots in the MoveNext method.
            // Here, the variable lifted to the state machine field is the closure pointer storage.
            v0.VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""C"" name=""M"">
      <customDebugInfo>
        <forwardIterator name=""&lt;M&gt;d__0"" />
        <encLocalSlotMap>
          <slot kind=""30"" offset=""0"" />
        </encLocalSlotMap>
        <encLambdaMap>
          <methodOrdinal>0</methodOrdinal>
          <closure offset=""0"" />
          <lambda offset=""167"" />
        </encLambdaMap>
        <encStateMachineStateMap>
          <state number=""0"" offset=""89"" />
        </encStateMachineStateMap>
      </customDebugInfo>
    </method>
    <method containingType=""C"" name=""&lt;M&gt;g__F|0_0"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
    </method>
    <method containingType=""C+&lt;M&gt;d__0"" name=""MoveNext"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""&lt;M&gt;g__F|0_0"" />
        <hoistedLocalScopes>
          <slot startOffset=""0x0"" endOffset=""0xb4"" />
        </hoistedLocalScopes>
        <encLocalSlotMap>
          <slot kind=""27"" offset=""0"" />
          <slot kind=""33"" offset=""89"" />
          <slot kind=""temp"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <asyncInfo>
        <kickoffMethod declaringType=""C"" methodName=""M"" />
        <await yield=""0x45"" resume=""0x60"" declaringType=""C+&lt;M&gt;d__0"" methodName=""MoveNext"" />
      </asyncInfo>
    </method>
  </methods>
</symbols>
", options: PdbValidationOptions.ExcludeDocuments | PdbValidationOptions.ExcludeSequencePoints | PdbValidationOptions.ExcludeNamespaces | PdbValidationOptions.ExcludeScopes);

            CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "C", "<>c__DisplayClass0_0", "<M>d__0");
            CheckNames(reader0, reader0.GetFieldDefNames(), "num", "<>1__state", "<>t__builder", "<>8__1", "<>u__1");

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, m0, m1, GetSyntaxMapFromMarkers(source0, source1))));

            // Notice that we reused field "<>8__1" (there is no "<>8__2"), which stores the local function closure pointer.
            diff1.VerifySynthesizedMembers(
                "C: {<M>g__F|0_0, <>c__DisplayClass0_0, <M>d__0}",
                "C.<M>d__0: {<>1__state, <>t__builder, <>8__1, <>u__1, MoveNext, SetStateMachine}",
                "C.<>c__DisplayClass0_0: {num}");
        }

        [Fact, WorkItem(1170899, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1170899")]
        public void HoistedAnonymousTypes1()
        {
            var source0 = MarkedSource(@"
using System;
using System.Collections.Generic;

class C
{
    public IEnumerable<int> F()
    {
        var <N:0>x = new { A = 1 }</N:0>;
        <N:1>yield return 1;</N:1>
        Console.WriteLine(x.A + 1);
    }
}
");
            var source1 = MarkedSource(@"
using System;
using System.Collections.Generic;

class C
{
    public IEnumerable<int> F()
    {
        var <N:0>x = new { A = 1 }</N:0>;
        <N:1>yield return 1;</N:1>
        Console.WriteLine(x.A + 2);
    }
}
");
            var source2 = MarkedSource(@"
using System;
using System.Collections.Generic;

class C
{
    public IEnumerable<int> F()
    {
        var <N:0>x = new { A = 1 }</N:0>;
        <N:1>yield return 1;</N:1>
        Console.WriteLine(x.A + 3);
    }
}
");
            var compilation0 = CreateCompilationWithMscorlib461(new[] { source0.Tree }, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);
            var compilation2 = compilation1.WithSource(source2.Tree);

            var v0 = CompileAndVerify(compilation0);
            v0.VerifyDiagnostics();
            using var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);
            var reader0 = md0.MetadataReader;

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");
            var f2 = compilation2.GetMember<MethodSymbol>("C.F");

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var baselineIL = @"
{
  // Code size       88 (0x58)
  .maxstack  2
  .locals init (int V_0)
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
  IL_0012:  br.s       IL_0018
  IL_0014:  br.s       IL_003c
  IL_0016:  ldc.i4.0
  IL_0017:  ret
  IL_0018:  ldarg.0
  IL_0019:  ldc.i4.m1
  IL_001a:  stfld      ""int C.<F>d__0.<>1__state""
  IL_001f:  nop
  IL_0020:  ldarg.0
  IL_0021:  ldc.i4.1
  IL_0022:  newobj     ""<>f__AnonymousType0<int>..ctor(int)""
  IL_0027:  stfld      ""<anonymous type: int A> C.<F>d__0.<x>5__1""
  IL_002c:  ldarg.0
  IL_002d:  ldc.i4.1
  IL_002e:  stfld      ""int C.<F>d__0.<>2__current""
  IL_0033:  ldarg.0
  IL_0034:  ldc.i4.1
  IL_0035:  stfld      ""int C.<F>d__0.<>1__state""
  IL_003a:  ldc.i4.1
  IL_003b:  ret
  IL_003c:  ldarg.0
  IL_003d:  ldc.i4.m1
  IL_003e:  stfld      ""int C.<F>d__0.<>1__state""
  IL_0043:  ldarg.0
  IL_0044:  ldfld      ""<anonymous type: int A> C.<F>d__0.<x>5__1""
  IL_0049:  callvirt   ""int <>f__AnonymousType0<int>.A.get""
  IL_004e:  ldc.i4.<<VALUE>>
  IL_004f:  add
  IL_0050:  call       ""void System.Console.WriteLine(int)""
  IL_0055:  nop
  IL_0056:  ldc.i4.0
  IL_0057:  ret
}";
            v0.VerifyIL("C.<F>d__0.System.Collections.IEnumerator.MoveNext()", baselineIL.Replace("<<VALUE>>", "1"));

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

            using var md1 = diff1.GetMetadata();
            var reader1 = md1.Reader;
            var readers = new[] { reader0, reader1 };

            CheckNames(readers, diff1.EmitResult.UpdatedMethods, "MoveNext");
            CheckNames(readers, diff1.EmitResult.ChangedTypes, "C", "<F>d__0");

            diff1.VerifySynthesizedMembers(
                "C: {<F>d__0}",
                "C.<F>d__0: {<>1__state, <>2__current, <>l__initialThreadId, <>4__this, <x>5__1, System.IDisposable.Dispose, MoveNext, System.Collections.Generic.IEnumerator<System.Int32>.get_Current, System.Collections.IEnumerator.Reset, System.Collections.IEnumerator.get_Current, System.Collections.Generic.IEnumerable<System.Int32>.GetEnumerator, System.Collections.IEnumerable.GetEnumerator, System.Collections.Generic.IEnumerator<System.Int32>.Current, System.Collections.IEnumerator.Current}",
                "<>f__AnonymousType0<<A>j__TPar>: {Equals, GetHashCode, ToString}");

            diff1.VerifyIL("C.<F>d__0.System.Collections.IEnumerator.MoveNext()", baselineIL.Replace("<<VALUE>>", "2"));

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, f1, f2, GetSyntaxMapFromMarkers(source1, source2))));

            using var md2 = diff2.GetMetadata();
            var reader2 = md2.Reader;
            readers = new[] { reader0, reader1, reader2 };

            CheckNames(readers, diff2.EmitResult.UpdatedMethods, "MoveNext");
            CheckNames(readers, diff2.EmitResult.ChangedTypes, "C", "<F>d__0");

            diff2.VerifySynthesizedMembers(
                 "C: {<F>d__0}",
                 "C.<F>d__0: {<>1__state, <>2__current, <>l__initialThreadId, <>4__this, <x>5__1, System.IDisposable.Dispose, MoveNext, System.Collections.Generic.IEnumerator<System.Int32>.get_Current, System.Collections.IEnumerator.Reset, System.Collections.IEnumerator.get_Current, System.Collections.Generic.IEnumerable<System.Int32>.GetEnumerator, System.Collections.IEnumerable.GetEnumerator, System.Collections.Generic.IEnumerator<System.Int32>.Current, System.Collections.IEnumerator.Current}",
                 "<>f__AnonymousType0<<A>j__TPar>: {Equals, GetHashCode, ToString}");

            diff2.VerifyIL("C.<F>d__0.System.Collections.IEnumerator.MoveNext()", baselineIL.Replace("<<VALUE>>", "3"));
        }

        [Fact, WorkItem(3192, "https://github.com/dotnet/roslyn/issues/3192")]
        public void HoistedAnonymousTypes_Nested()
        {
            var source0 = MarkedSource(@"
using System;
using System.Collections.Generic;

class C
{
    public IEnumerable<int> F()
    {
        var <N:0>x = new[] { new { A = new { B = 1 } } }</N:0>;
        <N:1>yield return 1;</N:1>
        Console.WriteLine(x[0].A.B + 1);
    }
}
");
            var source1 = MarkedSource(@"
using System;
using System.Collections.Generic;

class C
{
    public IEnumerable<int> F()
    {
        var <N:0>x = new[] { new { A = new { B = 1 } } }</N:0>;
        <N:1>yield return 1;</N:1>
        Console.WriteLine(x[0].A.B + 2);
    }
}
");
            var source2 = MarkedSource(@"
using System;
using System.Collections.Generic;

class C
{
    public IEnumerable<int> F()
    {
        var <N:0>x = new[] { new { A = new { B = 1 } } }</N:0>;
        <N:1>yield return 1;</N:1>
        Console.WriteLine(x[0].A.B + 3);
    }
}
");
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

            var baselineIL = @"
{
  // Code size      109 (0x6d)
  .maxstack  5
  .locals init (int V_0)
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
  IL_0012:  br.s       IL_0018
  IL_0014:  br.s       IL_004a
  IL_0016:  ldc.i4.0
  IL_0017:  ret
  IL_0018:  ldarg.0
  IL_0019:  ldc.i4.m1
  IL_001a:  stfld      ""int C.<F>d__0.<>1__state""
  IL_001f:  nop
  IL_0020:  ldarg.0
  IL_0021:  ldc.i4.1
  IL_0022:  newarr     ""<>f__AnonymousType0<<anonymous type: int B>>""
  IL_0027:  dup
  IL_0028:  ldc.i4.0
  IL_0029:  ldc.i4.1
  IL_002a:  newobj     ""<>f__AnonymousType1<int>..ctor(int)""
  IL_002f:  newobj     ""<>f__AnonymousType0<<anonymous type: int B>>..ctor(<anonymous type: int B>)""
  IL_0034:  stelem.ref
  IL_0035:  stfld      ""<anonymous type: <anonymous type: int B> A>[] C.<F>d__0.<x>5__1""
  IL_003a:  ldarg.0
  IL_003b:  ldc.i4.1
  IL_003c:  stfld      ""int C.<F>d__0.<>2__current""
  IL_0041:  ldarg.0
  IL_0042:  ldc.i4.1
  IL_0043:  stfld      ""int C.<F>d__0.<>1__state""
  IL_0048:  ldc.i4.1
  IL_0049:  ret
  IL_004a:  ldarg.0
  IL_004b:  ldc.i4.m1
  IL_004c:  stfld      ""int C.<F>d__0.<>1__state""
  IL_0051:  ldarg.0
  IL_0052:  ldfld      ""<anonymous type: <anonymous type: int B> A>[] C.<F>d__0.<x>5__1""
  IL_0057:  ldc.i4.0
  IL_0058:  ldelem.ref
  IL_0059:  callvirt   ""<anonymous type: int B> <>f__AnonymousType0<<anonymous type: int B>>.A.get""
  IL_005e:  callvirt   ""int <>f__AnonymousType1<int>.B.get""
  IL_0063:  ldc.i4.<<VALUE>>
  IL_0064:  add
  IL_0065:  call       ""void System.Console.WriteLine(int)""
  IL_006a:  nop
  IL_006b:  ldc.i4.0
  IL_006c:  ret
}";
            v0.VerifyIL("C.<F>d__0.System.Collections.IEnumerator.MoveNext()", baselineIL.Replace("<<VALUE>>", "1"));

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

            diff1.VerifySynthesizedMembers(
                "C: {<F>d__0}",
                "C.<F>d__0: {<>1__state, <>2__current, <>l__initialThreadId, <>4__this, <x>5__1, System.IDisposable.Dispose, MoveNext, System.Collections.Generic.IEnumerator<System.Int32>.get_Current, System.Collections.IEnumerator.Reset, System.Collections.IEnumerator.get_Current, System.Collections.Generic.IEnumerable<System.Int32>.GetEnumerator, System.Collections.IEnumerable.GetEnumerator, System.Collections.Generic.IEnumerator<System.Int32>.Current, System.Collections.IEnumerator.Current}",
                "<>f__AnonymousType0<<A>j__TPar>: {Equals, GetHashCode, ToString}",
                "<>f__AnonymousType1<<B>j__TPar>: {Equals, GetHashCode, ToString}");

            diff1.VerifyIL("C.<F>d__0.System.Collections.IEnumerator.MoveNext()", baselineIL.Replace("<<VALUE>>", "2"));

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, f1, f2, GetSyntaxMapFromMarkers(source1, source2))));

            diff2.VerifySynthesizedMembers(
                "C: {<F>d__0}",
                "C.<F>d__0: {<>1__state, <>2__current, <>l__initialThreadId, <>4__this, <x>5__1, System.IDisposable.Dispose, MoveNext, System.Collections.Generic.IEnumerator<System.Int32>.get_Current, System.Collections.IEnumerator.Reset, System.Collections.IEnumerator.get_Current, System.Collections.Generic.IEnumerable<System.Int32>.GetEnumerator, System.Collections.IEnumerable.GetEnumerator, System.Collections.Generic.IEnumerator<System.Int32>.Current, System.Collections.IEnumerator.Current}",
                "<>f__AnonymousType0<<A>j__TPar>: {Equals, GetHashCode, ToString}",
                "<>f__AnonymousType1<<B>j__TPar>: {Equals, GetHashCode, ToString}");

            diff2.VerifyIL("C.<F>d__0.System.Collections.IEnumerator.MoveNext()", baselineIL.Replace("<<VALUE>>", "3"));
        }

        [Fact, WorkItem(3192, "https://github.com/dotnet/roslyn/issues/3192")]
        public void HoistedGenericTypes()
        {
            var source0 = MarkedSource(@"
using System;
using System.Collections.Generic;

class Z<T1>
{
    public class S<T2> { public T1 a = default(T1); public T2 b = default(T2); }
}

class C
{
    public IEnumerable<int> F()
    {
        var <N:0>x = new Z<double>.S<int>()</N:0>;
        <N:1>yield return 1;</N:1>
        Console.WriteLine(x.a + x.b + 1);
    }
}
");
            var source1 = MarkedSource(@"
using System;
using System.Collections.Generic;

class Z<T1>
{
    public class S<T2> { public T1 a = default(T1); public T2 b = default(T2); }
}

class C
{
    public IEnumerable<int> F()
    {
        var <N:0>x = new Z<double>.S<int>()</N:0>;
        <N:1>yield return 1;</N:1>
        Console.WriteLine(x.a + x.b + 2);
    }
}
");
            var source2 = MarkedSource(@"
using System;
using System.Collections.Generic;

class Z<T1>
{
    public class S<T2> { public T1 a = default(T1); public T2 b = default(T2); }
}

class C
{
    public IEnumerable<int> F()
    {
        var <N:0>x = new Z<double>.S<int>()</N:0>;
        <N:1>yield return 1;</N:1>
        Console.WriteLine(x.a + x.b + 3);
    }
}
");
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

            var baselineIL = @"
{
  // Code size      108 (0x6c)
  .maxstack  2
  .locals init (int V_0)
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
  IL_0012:  br.s       IL_0018
  IL_0014:  br.s       IL_003b
  IL_0016:  ldc.i4.0
  IL_0017:  ret
  IL_0018:  ldarg.0
  IL_0019:  ldc.i4.m1
  IL_001a:  stfld      ""int C.<F>d__0.<>1__state""
  IL_001f:  nop
  IL_0020:  ldarg.0
  IL_0021:  newobj     ""Z<double>.S<int>..ctor()""
  IL_0026:  stfld      ""Z<double>.S<int> C.<F>d__0.<x>5__1""
  IL_002b:  ldarg.0
  IL_002c:  ldc.i4.1
  IL_002d:  stfld      ""int C.<F>d__0.<>2__current""
  IL_0032:  ldarg.0
  IL_0033:  ldc.i4.1
  IL_0034:  stfld      ""int C.<F>d__0.<>1__state""
  IL_0039:  ldc.i4.1
  IL_003a:  ret
  IL_003b:  ldarg.0
  IL_003c:  ldc.i4.m1
  IL_003d:  stfld      ""int C.<F>d__0.<>1__state""
  IL_0042:  ldarg.0
  IL_0043:  ldfld      ""Z<double>.S<int> C.<F>d__0.<x>5__1""
  IL_0048:  ldfld      ""double Z<double>.S<int>.a""
  IL_004d:  ldarg.0
  IL_004e:  ldfld      ""Z<double>.S<int> C.<F>d__0.<x>5__1""
  IL_0053:  ldfld      ""int Z<double>.S<int>.b""
  IL_0058:  conv.r8
  IL_0059:  add
  IL_005a:  ldc.r8     <<VALUE>>
  IL_0063:  add
  IL_0064:  call       ""void System.Console.WriteLine(double)""
  IL_0069:  nop
  IL_006a:  ldc.i4.0
  IL_006b:  ret
}";
            v0.VerifyIL("C.<F>d__0.System.Collections.IEnumerator.MoveNext()", baselineIL.Replace("<<VALUE>>", "1"));

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

            diff1.VerifySynthesizedMembers(
                "C: {<F>d__0}",
                "C.<F>d__0: {<>1__state, <>2__current, <>l__initialThreadId, <>4__this, <x>5__1, System.IDisposable.Dispose, MoveNext, System.Collections.Generic.IEnumerator<System.Int32>.get_Current, System.Collections.IEnumerator.Reset, System.Collections.IEnumerator.get_Current, System.Collections.Generic.IEnumerable<System.Int32>.GetEnumerator, System.Collections.IEnumerable.GetEnumerator, System.Collections.Generic.IEnumerator<System.Int32>.Current, System.Collections.IEnumerator.Current}");

            diff1.VerifyIL("C.<F>d__0.System.Collections.IEnumerator.MoveNext()", baselineIL.Replace("<<VALUE>>", "2"));

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, f1, f2, GetSyntaxMapFromMarkers(source1, source2))));

            diff2.VerifySynthesizedMembers(
                "C: {<F>d__0}",
                "C.<F>d__0: {<>1__state, <>2__current, <>l__initialThreadId, <>4__this, <x>5__1, System.IDisposable.Dispose, MoveNext, System.Collections.Generic.IEnumerator<System.Int32>.get_Current, System.Collections.IEnumerator.Reset, System.Collections.IEnumerator.get_Current, System.Collections.Generic.IEnumerable<System.Int32>.GetEnumerator, System.Collections.IEnumerable.GetEnumerator, System.Collections.Generic.IEnumerator<System.Int32>.Current, System.Collections.IEnumerator.Current}");

            diff2.VerifyIL("C.<F>d__0.System.Collections.IEnumerator.MoveNext()", baselineIL.Replace("<<VALUE>>", "3"));
        }

        [Fact]
        public void HoistedAnonymousTypes_Dynamic()
        {
            var template = @"
using System;
using System.Collections.Generic;

class C
{
    public IEnumerable<int> F()
    {
        var <N:0>x = new { A = (dynamic)null, B = 1 }</N:0>;
        <N:1>yield return 1;</N:1>
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
  // Code size       87 (0x57)
  .maxstack  3
  .locals init (int V_0)
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
  IL_0012:  br.s       IL_0018
  IL_0014:  br.s       IL_003d
  IL_0016:  ldc.i4.0
  IL_0017:  ret
  IL_0018:  ldarg.0
  IL_0019:  ldc.i4.m1
  IL_001a:  stfld      ""int C.<F>d__0.<>1__state""
  IL_001f:  nop
  IL_0020:  ldarg.0
  IL_0021:  ldnull
  IL_0022:  ldc.i4.1
  IL_0023:  newobj     ""<>f__AnonymousType0<dynamic, int>..ctor(dynamic, int)""
  IL_0028:  stfld      ""<anonymous type: dynamic A, int B> C.<F>d__0.<x>5__1""
  IL_002d:  ldarg.0
  IL_002e:  ldc.i4.1
  IL_002f:  stfld      ""int C.<F>d__0.<>2__current""
  IL_0034:  ldarg.0
  IL_0035:  ldc.i4.1
  IL_0036:  stfld      ""int C.<F>d__0.<>1__state""
  IL_003b:  ldc.i4.1
  IL_003c:  ret
  IL_003d:  ldarg.0
  IL_003e:  ldc.i4.m1
  IL_003f:  stfld      ""int C.<F>d__0.<>1__state""
  IL_0044:  ldarg.0
  IL_0045:  ldfld      ""<anonymous type: dynamic A, int B> C.<F>d__0.<x>5__1""
  IL_004a:  callvirt   ""int <>f__AnonymousType0<dynamic, int>.B.get""
  IL_004f:  call       ""void System.Console.WriteLine(int)""
  IL_0054:  nop
  IL_0055:  ldc.i4.0
  IL_0056:  ret
}";
            v0.VerifyIL("C.<F>d__0.System.Collections.IEnumerator.MoveNext()", baselineIL0);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

            var baselineIL = @"
{
  // Code size       89 (0x59)
  .maxstack  3
  .locals init (int V_0)
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
  IL_0012:  br.s       IL_0018
  IL_0014:  br.s       IL_003d
  IL_0016:  ldc.i4.0
  IL_0017:  ret
  IL_0018:  ldarg.0
  IL_0019:  ldc.i4.m1
  IL_001a:  stfld      ""int C.<F>d__0.<>1__state""
  IL_001f:  nop
  IL_0020:  ldarg.0
  IL_0021:  ldnull
  IL_0022:  ldc.i4.1
  IL_0023:  newobj     ""<>f__AnonymousType0<dynamic, int>..ctor(dynamic, int)""
  IL_0028:  stfld      ""<anonymous type: dynamic A, int B> C.<F>d__0.<x>5__1""
  IL_002d:  ldarg.0
  IL_002e:  ldc.i4.1
  IL_002f:  stfld      ""int C.<F>d__0.<>2__current""
  IL_0034:  ldarg.0
  IL_0035:  ldc.i4.1
  IL_0036:  stfld      ""int C.<F>d__0.<>1__state""
  IL_003b:  ldc.i4.1
  IL_003c:  ret
  IL_003d:  ldarg.0
  IL_003e:  ldc.i4.m1
  IL_003f:  stfld      ""int C.<F>d__0.<>1__state""
  IL_0044:  ldarg.0
  IL_0045:  ldfld      ""<anonymous type: dynamic A, int B> C.<F>d__0.<x>5__1""
  IL_004a:  callvirt   ""int <>f__AnonymousType0<dynamic, int>.B.get""
  IL_004f:  ldc.i4.<<VALUE>>
  IL_0050:  add
  IL_0051:  call       ""void System.Console.WriteLine(int)""
  IL_0056:  nop
  IL_0057:  ldc.i4.0
  IL_0058:  ret
}";

            diff1.VerifySynthesizedMembers(
                "C: {<F>d__0}",
                "C.<F>d__0: {<>1__state, <>2__current, <>l__initialThreadId, <>4__this, <x>5__1, System.IDisposable.Dispose, MoveNext, System.Collections.Generic.IEnumerator<System.Int32>.get_Current, System.Collections.IEnumerator.Reset, System.Collections.IEnumerator.get_Current, System.Collections.Generic.IEnumerable<System.Int32>.GetEnumerator, System.Collections.IEnumerable.GetEnumerator, System.Collections.Generic.IEnumerator<System.Int32>.Current, System.Collections.IEnumerator.Current}",
                "<>f__AnonymousType0<<A>j__TPar, <B>j__TPar>: {Equals, GetHashCode, ToString}");

            diff1.VerifyIL("C.<F>d__0.System.Collections.IEnumerator.MoveNext()", baselineIL.Replace("<<VALUE>>", "1"));

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, f1, f2, GetSyntaxMapFromMarkers(source1, source2))));

            diff2.VerifySynthesizedMembers(
                "C: {<F>d__0}",
                "C.<F>d__0: {<>1__state, <>2__current, <>l__initialThreadId, <>4__this, <x>5__1, System.IDisposable.Dispose, MoveNext, System.Collections.Generic.IEnumerator<System.Int32>.get_Current, System.Collections.IEnumerator.Reset, System.Collections.IEnumerator.get_Current, System.Collections.Generic.IEnumerable<System.Int32>.GetEnumerator, System.Collections.IEnumerable.GetEnumerator, System.Collections.Generic.IEnumerator<System.Int32>.Current, System.Collections.IEnumerator.Current}",
                "<>f__AnonymousType0<<A>j__TPar, <B>j__TPar>: {Equals, GetHashCode, ToString}");

            diff2.VerifyIL("C.<F>d__0.System.Collections.IEnumerator.MoveNext()", baselineIL.Replace("<<VALUE>>", "2"));
        }

        [Fact, WorkItem(3192, "https://github.com/dotnet/roslyn/issues/3192")]
        public void HoistedAnonymousTypes_Delete()
        {
            var source0 = MarkedSource(@"
using System.Linq;
using System.Threading.Tasks;

class C
{
    static async Task<int> F()
    {
        var <N:1>x = from b in new[] { 1, 2, 3 } <N:0>select new { A = b }</N:0></N:1>;
        return <N:2>await Task.FromResult(1)</N:2>;
    }
}
");
            var source1 = MarkedSource(@"
using System.Linq;
using System.Threading.Tasks;

class C
{
    static async Task<int> F()
    {
        var <N:1>x = from b in new[] { 1, 2, 3 } <N:0>select new { A = b }</N:0></N:1>;
        var y = x.First();
        return <N:2>await Task.FromResult(1)</N:2>;
    }
}
");
            var source2 = source0;
            var source3 = source1;
            var source4 = source0;
            var source5 = source1;

            var compilation0 = CreateCompilationWithMscorlib461(new[] { source0.Tree }, new[] { SystemCoreRef }, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);
            var compilation2 = compilation0.WithSource(source2.Tree);
            var compilation3 = compilation0.WithSource(source3.Tree);
            var compilation4 = compilation0.WithSource(source4.Tree);
            var compilation5 = compilation0.WithSource(source5.Tree);

            var v0 = CompileAndVerify(compilation0);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");
            var f2 = compilation2.GetMember<MethodSymbol>("C.F");
            var f3 = compilation3.GetMember<MethodSymbol>("C.F");
            var f4 = compilation4.GetMember<MethodSymbol>("C.F");
            var f5 = compilation5.GetMember<MethodSymbol>("C.F");

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            // y is added 
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

            diff1.VerifySynthesizedMembers(
                "C: {<>c, <F>d__0}",
                "C.<>c: {<>9__0_0, <F>b__0_0}",
                "C.<F>d__0: {<>1__state, <>t__builder, <x>5__1, <y>5__3, <>s__2, <>u__1, MoveNext, SetStateMachine}",
                "<>f__AnonymousType0<<A>j__TPar>: {Equals, GetHashCode, ToString}");

            // y is removed
            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, f1, f2, GetSyntaxMapFromMarkers(source1, source2))));

            // Synthesized members collection still includes y field since members are only added to it and never deleted.
            // The corresponding CLR field is also present.
            diff2.VerifySynthesizedMembers(
                "C: {<>c, <F>d__0}",
                "C.<>c: {<>9__0_0, <F>b__0_0}",
                "C.<F>d__0: {<>1__state, <>t__builder, <x>5__1, <>s__2, <>u__1, MoveNext, SetStateMachine, <y>5__3}",
                "<>f__AnonymousType0<<A>j__TPar>: {Equals, GetHashCode, ToString}");

            // y is added and a new slot index is allocated for it
            var diff3 = compilation3.EmitDifference(
                diff2.NextGeneration,
                ImmutableArray.Create(
                   SemanticEdit.Create(SemanticEditKind.Update, f2, f3, GetSyntaxMapFromMarkers(source2, source3))));

            diff3.VerifySynthesizedMembers(
                "C: {<>c, <F>d__0}",
                "C.<>c: {<>9__0_0, <F>b__0_0}",
                "C.<F>d__0: {<>1__state, <>t__builder, <x>5__1, <y>5__4, <>s__2, <>u__1, MoveNext, SetStateMachine, <y>5__3}",
                "<>f__AnonymousType0<<A>j__TPar>: {Equals, GetHashCode, ToString}");

            // y is removed
            var diff4 = compilation4.EmitDifference(
                diff3.NextGeneration,
                ImmutableArray.Create(
                   SemanticEdit.Create(SemanticEditKind.Update, f3, f4, GetSyntaxMapFromMarkers(source3, source4))));

            diff4.VerifySynthesizedMembers(
                "C: {<>c, <F>d__0}",
                "C.<>c: {<>9__0_0, <F>b__0_0}",
                "C.<F>d__0: {<>1__state, <>t__builder, <x>5__1, <>s__2, <>u__1, MoveNext, SetStateMachine, <y>5__4, <y>5__3}",
                "<>f__AnonymousType0<<A>j__TPar>: {Equals, GetHashCode, ToString}");

            // y is added
            var diff5 = compilation5.EmitDifference(
                diff4.NextGeneration,
                ImmutableArray.Create(
                   SemanticEdit.Create(SemanticEditKind.Update, f4, f5, GetSyntaxMapFromMarkers(source4, source5))));

            diff5.VerifySynthesizedMembers(
                "C: {<>c, <F>d__0}",
                "C.<>c: {<>9__0_0, <F>b__0_0}",
                "C.<F>d__0: {<>1__state, <>t__builder, <x>5__1, <y>5__5, <>s__2, <>u__1, MoveNext, SetStateMachine, <y>5__4, <y>5__3}",
                "<>f__AnonymousType0<<A>j__TPar>: {Equals, GetHashCode, ToString}");
        }

        [Fact]
        public void HoistedAnonymousTypes_Dynamic2()
        {
            var source0 = MarkedSource(@"
using System.Collections.Generic;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        args = Iterator().ToArray();
    }

    private static IEnumerable<string> Iterator()
    {
        string[] <N:15>args = new[] { ""a"", ""bB"", ""Cc"", ""DD"" }</N:15>;
        var <N:16>list = false ? null : new { Head = (dynamic)null, Tail = (dynamic)null }</N:16>;
        for (int <N:18>i = 0</N:18>; i < 10; i++)
        {
            var <N:6>result =
                from a in args
                <N:0>let x = a.Reverse()</N:0>
                <N:1>let y = x.Reverse()</N:1>
                <N:2>where x.SequenceEqual(y)</N:2>
                orderby <N:3>a.Length ascending</N:3>, <N:4>a descending</N:4>
                <N:5>select new { Value = a, Length = x.Count() }</N:5></N:6>;

            var <N:8>linked = result.Aggregate(
                false ? new { Head = (string)null, Tail = (dynamic)null } : null,
                <N:7>(total, curr) => new { Head = curr.Value, Tail = (dynamic)total }</N:7>)</N:8>;

            while (linked != null)
            {
                <N:9>yield return linked.Head</N:9>;
                linked = linked.Tail;
            }

            var <N:14>newArgs =
                from a in result
                <N:10>let value = a.Value</N:10>
                <N:11>let length = a.Length</N:11>
                <N:12>where value.Length == length</N:12>
                <N:13>select value + value</N:13></N:14>;

            args = args.Concat(newArgs).ToArray();
            list = new { Head = (dynamic)i, Tail = (dynamic)list };
            System.Diagnostics.Debugger.Break();
        }
        System.Diagnostics.Debugger.Break();
    }
}
");
            var source1 = MarkedSource(@"
using System.Collections.Generic;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        args = Iterator().ToArray();
    }

    private static IEnumerable<string> Iterator()
    {
        string[] <N:15>args = new[] { ""a"", ""bB"", ""Cc"", ""DD"" }</N:15>;
        var <N:16>list = false ? null : new { Head = (dynamic)null, Tail = (dynamic)null }</N:16>;
        for (int <N:18>i = 0</N:18>; i < 10; i++)
        {
            var <N:6>result =
                from a in args
                <N:0>let x = a.Reverse()</N:0>
                <N:1>let y = x.Reverse()</N:1>
                <N:2>where x.SequenceEqual(y)</N:2>
                orderby <N:3>a.Length ascending</N:3>, <N:4>a descending</N:4>
                <N:5>select new { Value = a, Length = x.Count() }</N:5></N:6>;

            var <N:8>linked = result.Aggregate(
                false ? new { Head = (string)null, Tail = (dynamic)null } : null,
                <N:7>(total, curr) => new { Head = curr.Value, Tail = (dynamic)total }</N:7>)</N:8>;

            var <N:17>temp = list</N:17>;
            while (temp != null)
            {
                <N:9>yield return temp.Head</N:9>;
                temp = temp.Tail;
            }

            var <N:14>newArgs =
                from a in result
                <N:10>let value = a.Value</N:10>
                <N:11>let length = a.Length</N:11>
                <N:12>where value.Length == length</N:12>
                <N:13>select value + value</N:13></N:14>;

            args = args.Concat(newArgs).ToArray();
            list = new { Head = (dynamic)i, Tail = (dynamic)list };
            System.Diagnostics.Debugger.Break();
        }
        System.Diagnostics.Debugger.Break();
    }
}
");
            var source2 = MarkedSource(@"
using System.Collections.Generic;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        args = Iterator().ToArray();
    }

    private static IEnumerable<string> Iterator()
    {
        string[] <N:15>args = new[] { ""a"", ""bB"", ""Cc"", ""DD"" }</N:15>;
        var <N:16>list = false ? null : new { Head = (dynamic)null, Tail = (dynamic)null }</N:16>;
        for (int <N:18>i = 0</N:18>; i < 10; i++)
        {
            var <N:6>result =
                from a in args
                <N:0>let x = a.Reverse()</N:0>
                <N:1>let y = x.Reverse()</N:1>
                <N:2>where x.SequenceEqual(y)</N:2>
                orderby <N:3>a.Length ascending</N:3>, <N:4>a descending</N:4>
                <N:5>select new { Value = a, Length = x.Count() }</N:5></N:6>;

            var <N:8>linked = result.Aggregate(
                false ? new { Head = (string)null, Tail = (dynamic)null } : null,
                <N:7>(total, curr) => new { Head = curr.Value, Tail = (dynamic)total }</N:7>)</N:8>;

            var <N:17>temp = list</N:17>;
            while (temp != null)
            {
                <N:9>yield return temp.Head.ToString()</N:9>;
                temp = temp.Tail;
            }

            var <N:14>newArgs =
                from a in result
                <N:10>let value = a.Value</N:10>
                <N:11>let length = a.Length</N:11>
                <N:12>where value.Length == length</N:12>
                <N:13>select value + value</N:13></N:14>;

            args = args.Concat(newArgs).ToArray();
            list = new { Head = (dynamic)i, Tail = (dynamic)list };
            System.Diagnostics.Debugger.Break();
        }
        System.Diagnostics.Debugger.Break();
    }
}
");
            var compilation0 = CreateCompilationWithMscorlib461(new[] { source0.Tree }, new[] { SystemCoreRef, CSharpRef }, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);
            var compilation2 = compilation1.WithSource(source2.Tree);

            var v0 = CompileAndVerify(compilation0);
            v0.VerifyDiagnostics();
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var f0 = compilation0.GetMember<MethodSymbol>("Program.Iterator");
            var f1 = compilation1.GetMember<MethodSymbol>("Program.Iterator");
            var f2 = compilation2.GetMember<MethodSymbol>("Program.Iterator");

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            v0.VerifyIL("Program.<Iterator>d__1.System.Collections.IEnumerator.MoveNext()", @"
{
  // Code size      798 (0x31e)
  .maxstack  5
  .locals init (int V_0,
                bool V_1,
                int V_2,
                bool V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Iterator>d__1.<>1__state""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  brfalse.s  IL_0012
  IL_000a:  br.s       IL_000c
  IL_000c:  ldloc.0
  IL_000d:  ldc.i4.1
  IL_000e:  beq.s      IL_0014
  IL_0010:  br.s       IL_0019
  IL_0012:  br.s       IL_001b
  IL_0014:  br         IL_019b
  IL_0019:  ldc.i4.0
  IL_001a:  ret
  IL_001b:  ldarg.0
  IL_001c:  ldc.i4.m1
  IL_001d:  stfld      ""int Program.<Iterator>d__1.<>1__state""
  IL_0022:  nop
  IL_0023:  ldarg.0
  IL_0024:  ldc.i4.4
  IL_0025:  newarr     ""string""
  IL_002a:  dup
  IL_002b:  ldc.i4.0
  IL_002c:  ldstr      ""a""
  IL_0031:  stelem.ref
  IL_0032:  dup
  IL_0033:  ldc.i4.1
  IL_0034:  ldstr      ""bB""
  IL_0039:  stelem.ref
  IL_003a:  dup
  IL_003b:  ldc.i4.2
  IL_003c:  ldstr      ""Cc""
  IL_0041:  stelem.ref
  IL_0042:  dup
  IL_0043:  ldc.i4.3
  IL_0044:  ldstr      ""DD""
  IL_0049:  stelem.ref
  IL_004a:  stfld      ""string[] Program.<Iterator>d__1.<args>5__1""
  IL_004f:  ldarg.0
  IL_0050:  ldnull
  IL_0051:  ldnull
  IL_0052:  newobj     ""<>f__AnonymousType0<dynamic, dynamic>..ctor(dynamic, dynamic)""
  IL_0057:  stfld      ""<anonymous type: dynamic Head, dynamic Tail> Program.<Iterator>d__1.<list>5__2""
  IL_005c:  ldarg.0
  IL_005d:  ldc.i4.0
  IL_005e:  stfld      ""int Program.<Iterator>d__1.<i>5__3""
  IL_0063:  br         IL_0305
  IL_0068:  nop
  IL_0069:  ldarg.0
  IL_006a:  ldarg.0
  IL_006b:  ldfld      ""string[] Program.<Iterator>d__1.<args>5__1""
  IL_0070:  ldsfld     ""System.Func<string, <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x>> Program.<>c.<>9__1_0""
  IL_0075:  dup
  IL_0076:  brtrue.s   IL_008f
  IL_0078:  pop
  IL_0079:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_007e:  ldftn      ""<anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> Program.<>c.<Iterator>b__1_0(string)""
  IL_0084:  newobj     ""System.Func<string, <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x>>..ctor(object, System.IntPtr)""
  IL_0089:  dup
  IL_008a:  stsfld     ""System.Func<string, <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x>> Program.<>c.<>9__1_0""
  IL_008f:  call       ""System.Collections.Generic.IEnumerable<<anonymous type: string a, System.Collections.Generic.IEnumerable<char> x>> System.Linq.Enumerable.Select<string, <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x>>(System.Collections.Generic.IEnumerable<string>, System.Func<string, <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x>>)""
  IL_0094:  ldsfld     ""System.Func<<anonymous type: string a, System.Collections.Generic.IEnumerable<char> x>, <anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>> Program.<>c.<>9__1_1""
  IL_0099:  dup
  IL_009a:  brtrue.s   IL_00b3
  IL_009c:  pop
  IL_009d:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_00a2:  ldftn      ""<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y> Program.<>c.<Iterator>b__1_1(<anonymous type: string a, System.Collections.Generic.IEnumerable<char> x>)""
  IL_00a8:  newobj     ""System.Func<<anonymous type: string a, System.Collections.Generic.IEnumerable<char> x>, <anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>>..ctor(object, System.IntPtr)""
  IL_00ad:  dup
  IL_00ae:  stsfld     ""System.Func<<anonymous type: string a, System.Collections.Generic.IEnumerable<char> x>, <anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>> Program.<>c.<>9__1_1""
  IL_00b3:  call       ""System.Collections.Generic.IEnumerable<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>> System.Linq.Enumerable.Select<<anonymous type: string a, System.Collections.Generic.IEnumerable<char> x>, <anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>>(System.Collections.Generic.IEnumerable<<anonymous type: string a, System.Collections.Generic.IEnumerable<char> x>>, System.Func<<anonymous type: string a, System.Collections.Generic.IEnumerable<char> x>, <anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>>)""
  IL_00b8:  ldsfld     ""System.Func<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, bool> Program.<>c.<>9__1_2""
  IL_00bd:  dup
  IL_00be:  brtrue.s   IL_00d7
  IL_00c0:  pop
  IL_00c1:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_00c6:  ldftn      ""bool Program.<>c.<Iterator>b__1_2(<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>)""
  IL_00cc:  newobj     ""System.Func<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, bool>..ctor(object, System.IntPtr)""
  IL_00d1:  dup
  IL_00d2:  stsfld     ""System.Func<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, bool> Program.<>c.<>9__1_2""
  IL_00d7:  call       ""System.Collections.Generic.IEnumerable<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>> System.Linq.Enumerable.Where<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>>(System.Collections.Generic.IEnumerable<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>>, System.Func<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, bool>)""
  IL_00dc:  ldsfld     ""System.Func<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, int> Program.<>c.<>9__1_3""
  IL_00e1:  dup
  IL_00e2:  brtrue.s   IL_00fb
  IL_00e4:  pop
  IL_00e5:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_00ea:  ldftn      ""int Program.<>c.<Iterator>b__1_3(<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>)""
  IL_00f0:  newobj     ""System.Func<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, int>..ctor(object, System.IntPtr)""
  IL_00f5:  dup
  IL_00f6:  stsfld     ""System.Func<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, int> Program.<>c.<>9__1_3""
  IL_00fb:  call       ""System.Linq.IOrderedEnumerable<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>> System.Linq.Enumerable.OrderBy<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, int>(System.Collections.Generic.IEnumerable<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>>, System.Func<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, int>)""
  IL_0100:  ldsfld     ""System.Func<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, string> Program.<>c.<>9__1_4""
  IL_0105:  dup
  IL_0106:  brtrue.s   IL_011f
  IL_0108:  pop
  IL_0109:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_010e:  ldftn      ""string Program.<>c.<Iterator>b__1_4(<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>)""
  IL_0114:  newobj     ""System.Func<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, string>..ctor(object, System.IntPtr)""
  IL_0119:  dup
  IL_011a:  stsfld     ""System.Func<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, string> Program.<>c.<>9__1_4""
  IL_011f:  call       ""System.Linq.IOrderedEnumerable<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>> System.Linq.Enumerable.ThenByDescending<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, string>(System.Linq.IOrderedEnumerable<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>>, System.Func<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, string>)""
  IL_0124:  ldsfld     ""System.Func<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, <anonymous type: string Value, int Length>> Program.<>c.<>9__1_5""
  IL_0129:  dup
  IL_012a:  brtrue.s   IL_0143
  IL_012c:  pop
  IL_012d:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_0132:  ldftn      ""<anonymous type: string Value, int Length> Program.<>c.<Iterator>b__1_5(<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>)""
  IL_0138:  newobj     ""System.Func<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, <anonymous type: string Value, int Length>>..ctor(object, System.IntPtr)""
  IL_013d:  dup
  IL_013e:  stsfld     ""System.Func<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, <anonymous type: string Value, int Length>> Program.<>c.<>9__1_5""
  IL_0143:  call       ""System.Collections.Generic.IEnumerable<<anonymous type: string Value, int Length>> System.Linq.Enumerable.Select<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, <anonymous type: string Value, int Length>>(System.Collections.Generic.IEnumerable<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>>, System.Func<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, <anonymous type: string Value, int Length>>)""
  IL_0148:  stfld      ""System.Collections.Generic.IEnumerable<<anonymous type: string Value, int Length>> Program.<Iterator>d__1.<result>5__4""
  IL_014d:  ldarg.0
  IL_014e:  ldarg.0
  IL_014f:  ldfld      ""System.Collections.Generic.IEnumerable<<anonymous type: string Value, int Length>> Program.<Iterator>d__1.<result>5__4""
  IL_0154:  ldnull
  IL_0155:  ldsfld     ""System.Func<<anonymous type: string Head, dynamic Tail>, <anonymous type: string Value, int Length>, <anonymous type: string Head, dynamic Tail>> Program.<>c.<>9__1_6""
  IL_015a:  dup
  IL_015b:  brtrue.s   IL_0174
  IL_015d:  pop
  IL_015e:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_0163:  ldftn      ""<anonymous type: string Head, dynamic Tail> Program.<>c.<Iterator>b__1_6(<anonymous type: string Head, dynamic Tail>, <anonymous type: string Value, int Length>)""
  IL_0169:  newobj     ""System.Func<<anonymous type: string Head, dynamic Tail>, <anonymous type: string Value, int Length>, <anonymous type: string Head, dynamic Tail>>..ctor(object, System.IntPtr)""
  IL_016e:  dup
  IL_016f:  stsfld     ""System.Func<<anonymous type: string Head, dynamic Tail>, <anonymous type: string Value, int Length>, <anonymous type: string Head, dynamic Tail>> Program.<>c.<>9__1_6""
  IL_0174:  call       ""<anonymous type: string Head, dynamic Tail> System.Linq.Enumerable.Aggregate<<anonymous type: string Value, int Length>, <anonymous type: string Head, dynamic Tail>>(System.Collections.Generic.IEnumerable<<anonymous type: string Value, int Length>>, <anonymous type: string Head, dynamic Tail>, System.Func<<anonymous type: string Head, dynamic Tail>, <anonymous type: string Value, int Length>, <anonymous type: string Head, dynamic Tail>>)""
  IL_0179:  stfld      ""<anonymous type: string Head, dynamic Tail> Program.<Iterator>d__1.<linked>5__5""
  IL_017e:  br.s       IL_01f5
  IL_0180:  nop
  IL_0181:  ldarg.0
  IL_0182:  ldarg.0
  IL_0183:  ldfld      ""<anonymous type: string Head, dynamic Tail> Program.<Iterator>d__1.<linked>5__5""
  IL_0188:  callvirt   ""string <>f__AnonymousType0<string, dynamic>.Head.get""
  IL_018d:  stfld      ""string Program.<Iterator>d__1.<>2__current""
  IL_0192:  ldarg.0
  IL_0193:  ldc.i4.1
  IL_0194:  stfld      ""int Program.<Iterator>d__1.<>1__state""
  IL_0199:  ldc.i4.1
  IL_019a:  ret
  IL_019b:  ldarg.0
  IL_019c:  ldc.i4.m1
  IL_019d:  stfld      ""int Program.<Iterator>d__1.<>1__state""
  IL_01a2:  ldarg.0
  IL_01a3:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, <anonymous type: string Head, dynamic Tail>>> Program.<>o__1.<>p__0""
  IL_01a8:  brfalse.s  IL_01ac
  IL_01aa:  br.s       IL_01d0
  IL_01ac:  ldc.i4.0
  IL_01ad:  ldtoken    ""<>f__AnonymousType0<string, dynamic>""
  IL_01b2:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_01b7:  ldtoken    ""Program""
  IL_01bc:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_01c1:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.Convert(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Type)""
  IL_01c6:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, <anonymous type: string Head, dynamic Tail>>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, <anonymous type: string Head, dynamic Tail>>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_01cb:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, <anonymous type: string Head, dynamic Tail>>> Program.<>o__1.<>p__0""
  IL_01d0:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, <anonymous type: string Head, dynamic Tail>>> Program.<>o__1.<>p__0""
  IL_01d5:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, <anonymous type: string Head, dynamic Tail>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, <anonymous type: string Head, dynamic Tail>>>.Target""
  IL_01da:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, <anonymous type: string Head, dynamic Tail>>> Program.<>o__1.<>p__0""
  IL_01df:  ldarg.0
  IL_01e0:  ldfld      ""<anonymous type: string Head, dynamic Tail> Program.<Iterator>d__1.<linked>5__5""
  IL_01e5:  callvirt   ""dynamic <>f__AnonymousType0<string, dynamic>.Tail.get""
  IL_01ea:  callvirt   ""<anonymous type: string Head, dynamic Tail> System.Func<System.Runtime.CompilerServices.CallSite, dynamic, <anonymous type: string Head, dynamic Tail>>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_01ef:  stfld      ""<anonymous type: string Head, dynamic Tail> Program.<Iterator>d__1.<linked>5__5""
  IL_01f4:  nop
  IL_01f5:  ldarg.0
  IL_01f6:  ldfld      ""<anonymous type: string Head, dynamic Tail> Program.<Iterator>d__1.<linked>5__5""
  IL_01fb:  ldnull
  IL_01fc:  cgt.un
  IL_01fe:  stloc.1
  IL_01ff:  ldloc.1
  IL_0200:  brtrue     IL_0180
  IL_0205:  ldarg.0
  IL_0206:  ldarg.0
  IL_0207:  ldfld      ""System.Collections.Generic.IEnumerable<<anonymous type: string Value, int Length>> Program.<Iterator>d__1.<result>5__4""
  IL_020c:  ldsfld     ""System.Func<<anonymous type: string Value, int Length>, <anonymous type: <anonymous type: string Value, int Length> a, string value>> Program.<>c.<>9__1_7""
  IL_0211:  dup
  IL_0212:  brtrue.s   IL_022b
  IL_0214:  pop
  IL_0215:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_021a:  ldftn      ""<anonymous type: <anonymous type: string Value, int Length> a, string value> Program.<>c.<Iterator>b__1_7(<anonymous type: string Value, int Length>)""
  IL_0220:  newobj     ""System.Func<<anonymous type: string Value, int Length>, <anonymous type: <anonymous type: string Value, int Length> a, string value>>..ctor(object, System.IntPtr)""
  IL_0225:  dup
  IL_0226:  stsfld     ""System.Func<<anonymous type: string Value, int Length>, <anonymous type: <anonymous type: string Value, int Length> a, string value>> Program.<>c.<>9__1_7""
  IL_022b:  call       ""System.Collections.Generic.IEnumerable<<anonymous type: <anonymous type: string Value, int Length> a, string value>> System.Linq.Enumerable.Select<<anonymous type: string Value, int Length>, <anonymous type: <anonymous type: string Value, int Length> a, string value>>(System.Collections.Generic.IEnumerable<<anonymous type: string Value, int Length>>, System.Func<<anonymous type: string Value, int Length>, <anonymous type: <anonymous type: string Value, int Length> a, string value>>)""
  IL_0230:  ldsfld     ""System.Func<<anonymous type: <anonymous type: string Value, int Length> a, string value>, <anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>> Program.<>c.<>9__1_8""
  IL_0235:  dup
  IL_0236:  brtrue.s   IL_024f
  IL_0238:  pop
  IL_0239:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_023e:  ldftn      ""<anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length> Program.<>c.<Iterator>b__1_8(<anonymous type: <anonymous type: string Value, int Length> a, string value>)""
  IL_0244:  newobj     ""System.Func<<anonymous type: <anonymous type: string Value, int Length> a, string value>, <anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>>..ctor(object, System.IntPtr)""
  IL_0249:  dup
  IL_024a:  stsfld     ""System.Func<<anonymous type: <anonymous type: string Value, int Length> a, string value>, <anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>> Program.<>c.<>9__1_8""
  IL_024f:  call       ""System.Collections.Generic.IEnumerable<<anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>> System.Linq.Enumerable.Select<<anonymous type: <anonymous type: string Value, int Length> a, string value>, <anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>>(System.Collections.Generic.IEnumerable<<anonymous type: <anonymous type: string Value, int Length> a, string value>>, System.Func<<anonymous type: <anonymous type: string Value, int Length> a, string value>, <anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>>)""
  IL_0254:  ldsfld     ""System.Func<<anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>, bool> Program.<>c.<>9__1_9""
  IL_0259:  dup
  IL_025a:  brtrue.s   IL_0273
  IL_025c:  pop
  IL_025d:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_0262:  ldftn      ""bool Program.<>c.<Iterator>b__1_9(<anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>)""
  IL_0268:  newobj     ""System.Func<<anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>, bool>..ctor(object, System.IntPtr)""
  IL_026d:  dup
  IL_026e:  stsfld     ""System.Func<<anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>, bool> Program.<>c.<>9__1_9""
  IL_0273:  call       ""System.Collections.Generic.IEnumerable<<anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>> System.Linq.Enumerable.Where<<anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>>(System.Collections.Generic.IEnumerable<<anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>>, System.Func<<anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>, bool>)""
  IL_0278:  ldsfld     ""System.Func<<anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>, string> Program.<>c.<>9__1_10""
  IL_027d:  dup
  IL_027e:  brtrue.s   IL_0297
  IL_0280:  pop
  IL_0281:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_0286:  ldftn      ""string Program.<>c.<Iterator>b__1_10(<anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>)""
  IL_028c:  newobj     ""System.Func<<anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>, string>..ctor(object, System.IntPtr)""
  IL_0291:  dup
  IL_0292:  stsfld     ""System.Func<<anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>, string> Program.<>c.<>9__1_10""
  IL_0297:  call       ""System.Collections.Generic.IEnumerable<string> System.Linq.Enumerable.Select<<anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>, string>(System.Collections.Generic.IEnumerable<<anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>>, System.Func<<anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>, string>)""
  IL_029c:  stfld      ""System.Collections.Generic.IEnumerable<string> Program.<Iterator>d__1.<newArgs>5__6""
  IL_02a1:  ldarg.0
  IL_02a2:  ldarg.0
  IL_02a3:  ldfld      ""string[] Program.<Iterator>d__1.<args>5__1""
  IL_02a8:  ldarg.0
  IL_02a9:  ldfld      ""System.Collections.Generic.IEnumerable<string> Program.<Iterator>d__1.<newArgs>5__6""
  IL_02ae:  call       ""System.Collections.Generic.IEnumerable<string> System.Linq.Enumerable.Concat<string>(System.Collections.Generic.IEnumerable<string>, System.Collections.Generic.IEnumerable<string>)""
  IL_02b3:  call       ""string[] System.Linq.Enumerable.ToArray<string>(System.Collections.Generic.IEnumerable<string>)""
  IL_02b8:  stfld      ""string[] Program.<Iterator>d__1.<args>5__1""
  IL_02bd:  ldarg.0
  IL_02be:  ldarg.0
  IL_02bf:  ldfld      ""int Program.<Iterator>d__1.<i>5__3""
  IL_02c4:  box        ""int""
  IL_02c9:  ldarg.0
  IL_02ca:  ldfld      ""<anonymous type: dynamic Head, dynamic Tail> Program.<Iterator>d__1.<list>5__2""
  IL_02cf:  newobj     ""<>f__AnonymousType0<dynamic, dynamic>..ctor(dynamic, dynamic)""
  IL_02d4:  stfld      ""<anonymous type: dynamic Head, dynamic Tail> Program.<Iterator>d__1.<list>5__2""
  IL_02d9:  call       ""void System.Diagnostics.Debugger.Break()""
  IL_02de:  nop
  IL_02df:  nop
  IL_02e0:  ldarg.0
  IL_02e1:  ldnull
  IL_02e2:  stfld      ""System.Collections.Generic.IEnumerable<<anonymous type: string Value, int Length>> Program.<Iterator>d__1.<result>5__4""
  IL_02e7:  ldarg.0
  IL_02e8:  ldnull
  IL_02e9:  stfld      ""<anonymous type: string Head, dynamic Tail> Program.<Iterator>d__1.<linked>5__5""
  IL_02ee:  ldarg.0
  IL_02ef:  ldnull
  IL_02f0:  stfld      ""System.Collections.Generic.IEnumerable<string> Program.<Iterator>d__1.<newArgs>5__6""
  IL_02f5:  ldarg.0
  IL_02f6:  ldfld      ""int Program.<Iterator>d__1.<i>5__3""
  IL_02fb:  stloc.2
  IL_02fc:  ldarg.0
  IL_02fd:  ldloc.2
  IL_02fe:  ldc.i4.1
  IL_02ff:  add
  IL_0300:  stfld      ""int Program.<Iterator>d__1.<i>5__3""
  IL_0305:  ldarg.0
  IL_0306:  ldfld      ""int Program.<Iterator>d__1.<i>5__3""
  IL_030b:  ldc.i4.s   10
  IL_030d:  clt
  IL_030f:  stloc.3
  IL_0310:  ldloc.3
  IL_0311:  brtrue     IL_0068
  IL_0316:  call       ""void System.Diagnostics.Debugger.Break()""
  IL_031b:  nop
  IL_031c:  ldc.i4.0
  IL_031d:  ret
}");

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

            diff1.VerifySynthesizedMembers(
                "Program.<>o__1#1: {<>p__0, <>p__1}",
                "Program: {<>c, <>o__1#1, <Iterator>d__1}",
                "Program.<>c: {<>9__1_0, <>9__1_1, <>9__1_2, <>9__1_3, <>9__1_4, <>9__1_5, <>9__1_6, <>9__1_7, <>9__1_8, <>9__1_9, <>9__1_10, <Iterator>b__1_0, <Iterator>b__1_1, <Iterator>b__1_2, <Iterator>b__1_3, <Iterator>b__1_4, <Iterator>b__1_5, <Iterator>b__1_6, <Iterator>b__1_7, <Iterator>b__1_8, <Iterator>b__1_9, <Iterator>b__1_10}",
                "Program.<Iterator>d__1: {<>1__state, <>2__current, <>l__initialThreadId, <args>5__1, <list>5__2, <i>5__3, <result>5__4, <linked>5__5, <temp>5__7, <newArgs>5__6, System.IDisposable.Dispose, MoveNext, System.Collections.Generic.IEnumerator<System.String>.get_Current, System.Collections.IEnumerator.Reset, System.Collections.IEnumerator.get_Current, System.Collections.Generic.IEnumerable<System.String>.GetEnumerator, System.Collections.IEnumerable.GetEnumerator, System.Collections.Generic.IEnumerator<System.String>.Current, System.Collections.IEnumerator.Current}",
                "<>f__AnonymousType4<<a>j__TPar, <value>j__TPar>: {Equals, GetHashCode, ToString}",
                "<>f__AnonymousType3<<Value>j__TPar, <Length>j__TPar>: {Equals, GetHashCode, ToString}",
                "<>f__AnonymousType5<<<>h__TransparentIdentifier0>j__TPar, <length>j__TPar>: {Equals, GetHashCode, ToString}",
                "<>f__AnonymousType2<<<>h__TransparentIdentifier0>j__TPar, <y>j__TPar>: {Equals, GetHashCode, ToString}",
                "<>f__AnonymousType0<<Head>j__TPar, <Tail>j__TPar>: {Equals, GetHashCode, ToString}",
                "<>f__AnonymousType1<<a>j__TPar, <x>j__TPar>: {Equals, GetHashCode, ToString}");

            diff1.VerifyIL("Program.<Iterator>d__1.System.Collections.IEnumerator.MoveNext()", @"
{
  // Code size      885 (0x375)
  .maxstack  5
  .locals init (int V_0,
                bool V_1,
                int V_2,
                bool V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Iterator>d__1.<>1__state""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  brfalse.s  IL_0012
  IL_000a:  br.s       IL_000c
  IL_000c:  ldloc.0
  IL_000d:  ldc.i4.1
  IL_000e:  beq.s      IL_0014
  IL_0010:  br.s       IL_0019
  IL_0012:  br.s       IL_001b
  IL_0014:  br         IL_01eb
  IL_0019:  ldc.i4.0
  IL_001a:  ret
  IL_001b:  ldarg.0
  IL_001c:  ldc.i4.m1
  IL_001d:  stfld      ""int Program.<Iterator>d__1.<>1__state""
  IL_0022:  nop
  IL_0023:  ldarg.0
  IL_0024:  ldc.i4.4
  IL_0025:  newarr     ""string""
  IL_002a:  dup
  IL_002b:  ldc.i4.0
  IL_002c:  ldstr      ""a""
  IL_0031:  stelem.ref
  IL_0032:  dup
  IL_0033:  ldc.i4.1
  IL_0034:  ldstr      ""bB""
  IL_0039:  stelem.ref
  IL_003a:  dup
  IL_003b:  ldc.i4.2
  IL_003c:  ldstr      ""Cc""
  IL_0041:  stelem.ref
  IL_0042:  dup
  IL_0043:  ldc.i4.3
  IL_0044:  ldstr      ""DD""
  IL_0049:  stelem.ref
  IL_004a:  stfld      ""string[] Program.<Iterator>d__1.<args>5__1""
  IL_004f:  ldarg.0
  IL_0050:  ldnull
  IL_0051:  ldnull
  IL_0052:  newobj     ""<>f__AnonymousType0<dynamic, dynamic>..ctor(dynamic, dynamic)""
  IL_0057:  stfld      ""<anonymous type: dynamic Head, dynamic Tail> Program.<Iterator>d__1.<list>5__2""
  IL_005c:  ldarg.0
  IL_005d:  ldc.i4.0
  IL_005e:  stfld      ""int Program.<Iterator>d__1.<i>5__3""
  IL_0063:  br         IL_035c
  IL_0068:  nop
  IL_0069:  ldarg.0
  IL_006a:  ldarg.0
  IL_006b:  ldfld      ""string[] Program.<Iterator>d__1.<args>5__1""
  IL_0070:  ldsfld     ""System.Func<string, <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x>> Program.<>c.<>9__1_0""
  IL_0075:  dup
  IL_0076:  brtrue.s   IL_008f
  IL_0078:  pop
  IL_0079:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_007e:  ldftn      ""<anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> Program.<>c.<Iterator>b__1_0(string)""
  IL_0084:  newobj     ""System.Func<string, <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x>>..ctor(object, System.IntPtr)""
  IL_0089:  dup
  IL_008a:  stsfld     ""System.Func<string, <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x>> Program.<>c.<>9__1_0""
  IL_008f:  call       ""System.Collections.Generic.IEnumerable<<anonymous type: string a, System.Collections.Generic.IEnumerable<char> x>> System.Linq.Enumerable.Select<string, <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x>>(System.Collections.Generic.IEnumerable<string>, System.Func<string, <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x>>)""
  IL_0094:  ldsfld     ""System.Func<<anonymous type: string a, System.Collections.Generic.IEnumerable<char> x>, <anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>> Program.<>c.<>9__1_1""
  IL_0099:  dup
  IL_009a:  brtrue.s   IL_00b3
  IL_009c:  pop
  IL_009d:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_00a2:  ldftn      ""<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y> Program.<>c.<Iterator>b__1_1(<anonymous type: string a, System.Collections.Generic.IEnumerable<char> x>)""
  IL_00a8:  newobj     ""System.Func<<anonymous type: string a, System.Collections.Generic.IEnumerable<char> x>, <anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>>..ctor(object, System.IntPtr)""
  IL_00ad:  dup
  IL_00ae:  stsfld     ""System.Func<<anonymous type: string a, System.Collections.Generic.IEnumerable<char> x>, <anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>> Program.<>c.<>9__1_1""
  IL_00b3:  call       ""System.Collections.Generic.IEnumerable<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>> System.Linq.Enumerable.Select<<anonymous type: string a, System.Collections.Generic.IEnumerable<char> x>, <anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>>(System.Collections.Generic.IEnumerable<<anonymous type: string a, System.Collections.Generic.IEnumerable<char> x>>, System.Func<<anonymous type: string a, System.Collections.Generic.IEnumerable<char> x>, <anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>>)""
  IL_00b8:  ldsfld     ""System.Func<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, bool> Program.<>c.<>9__1_2""
  IL_00bd:  dup
  IL_00be:  brtrue.s   IL_00d7
  IL_00c0:  pop
  IL_00c1:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_00c6:  ldftn      ""bool Program.<>c.<Iterator>b__1_2(<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>)""
  IL_00cc:  newobj     ""System.Func<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, bool>..ctor(object, System.IntPtr)""
  IL_00d1:  dup
  IL_00d2:  stsfld     ""System.Func<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, bool> Program.<>c.<>9__1_2""
  IL_00d7:  call       ""System.Collections.Generic.IEnumerable<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>> System.Linq.Enumerable.Where<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>>(System.Collections.Generic.IEnumerable<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>>, System.Func<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, bool>)""
  IL_00dc:  ldsfld     ""System.Func<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, int> Program.<>c.<>9__1_3""
  IL_00e1:  dup
  IL_00e2:  brtrue.s   IL_00fb
  IL_00e4:  pop
  IL_00e5:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_00ea:  ldftn      ""int Program.<>c.<Iterator>b__1_3(<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>)""
  IL_00f0:  newobj     ""System.Func<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, int>..ctor(object, System.IntPtr)""
  IL_00f5:  dup
  IL_00f6:  stsfld     ""System.Func<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, int> Program.<>c.<>9__1_3""
  IL_00fb:  call       ""System.Linq.IOrderedEnumerable<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>> System.Linq.Enumerable.OrderBy<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, int>(System.Collections.Generic.IEnumerable<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>>, System.Func<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, int>)""
  IL_0100:  ldsfld     ""System.Func<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, string> Program.<>c.<>9__1_4""
  IL_0105:  dup
  IL_0106:  brtrue.s   IL_011f
  IL_0108:  pop
  IL_0109:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_010e:  ldftn      ""string Program.<>c.<Iterator>b__1_4(<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>)""
  IL_0114:  newobj     ""System.Func<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, string>..ctor(object, System.IntPtr)""
  IL_0119:  dup
  IL_011a:  stsfld     ""System.Func<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, string> Program.<>c.<>9__1_4""
  IL_011f:  call       ""System.Linq.IOrderedEnumerable<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>> System.Linq.Enumerable.ThenByDescending<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, string>(System.Linq.IOrderedEnumerable<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>>, System.Func<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, string>)""
  IL_0124:  ldsfld     ""System.Func<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, <anonymous type: string Value, int Length>> Program.<>c.<>9__1_5""
  IL_0129:  dup
  IL_012a:  brtrue.s   IL_0143
  IL_012c:  pop
  IL_012d:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_0132:  ldftn      ""<anonymous type: string Value, int Length> Program.<>c.<Iterator>b__1_5(<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>)""
  IL_0138:  newobj     ""System.Func<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, <anonymous type: string Value, int Length>>..ctor(object, System.IntPtr)""
  IL_013d:  dup
  IL_013e:  stsfld     ""System.Func<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, <anonymous type: string Value, int Length>> Program.<>c.<>9__1_5""
  IL_0143:  call       ""System.Collections.Generic.IEnumerable<<anonymous type: string Value, int Length>> System.Linq.Enumerable.Select<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, <anonymous type: string Value, int Length>>(System.Collections.Generic.IEnumerable<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>>, System.Func<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, <anonymous type: string Value, int Length>>)""
  IL_0148:  stfld      ""System.Collections.Generic.IEnumerable<<anonymous type: string Value, int Length>> Program.<Iterator>d__1.<result>5__4""
  IL_014d:  ldarg.0
  IL_014e:  ldarg.0
  IL_014f:  ldfld      ""System.Collections.Generic.IEnumerable<<anonymous type: string Value, int Length>> Program.<Iterator>d__1.<result>5__4""
  IL_0154:  ldnull
  IL_0155:  ldsfld     ""System.Func<<anonymous type: string Head, dynamic Tail>, <anonymous type: string Value, int Length>, <anonymous type: string Head, dynamic Tail>> Program.<>c.<>9__1_6""
  IL_015a:  dup
  IL_015b:  brtrue.s   IL_0174
  IL_015d:  pop
  IL_015e:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_0163:  ldftn      ""<anonymous type: string Head, dynamic Tail> Program.<>c.<Iterator>b__1_6(<anonymous type: string Head, dynamic Tail>, <anonymous type: string Value, int Length>)""
  IL_0169:  newobj     ""System.Func<<anonymous type: string Head, dynamic Tail>, <anonymous type: string Value, int Length>, <anonymous type: string Head, dynamic Tail>>..ctor(object, System.IntPtr)""
  IL_016e:  dup
  IL_016f:  stsfld     ""System.Func<<anonymous type: string Head, dynamic Tail>, <anonymous type: string Value, int Length>, <anonymous type: string Head, dynamic Tail>> Program.<>c.<>9__1_6""
  IL_0174:  call       ""<anonymous type: string Head, dynamic Tail> System.Linq.Enumerable.Aggregate<<anonymous type: string Value, int Length>, <anonymous type: string Head, dynamic Tail>>(System.Collections.Generic.IEnumerable<<anonymous type: string Value, int Length>>, <anonymous type: string Head, dynamic Tail>, System.Func<<anonymous type: string Head, dynamic Tail>, <anonymous type: string Value, int Length>, <anonymous type: string Head, dynamic Tail>>)""
  IL_0179:  stfld      ""<anonymous type: string Head, dynamic Tail> Program.<Iterator>d__1.<linked>5__5""
  IL_017e:  ldarg.0
  IL_017f:  ldarg.0
  IL_0180:  ldfld      ""<anonymous type: dynamic Head, dynamic Tail> Program.<Iterator>d__1.<list>5__2""
  IL_0185:  stfld      ""<anonymous type: dynamic Head, dynamic Tail> Program.<Iterator>d__1.<temp>5__7""
  IL_018a:  br         IL_0245
  IL_018f:  nop
  IL_0190:  ldarg.0
  IL_0191:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, string>> Program.<>o__1#1.<>p__0""
  IL_0196:  brfalse.s  IL_019a
  IL_0198:  br.s       IL_01be
  IL_019a:  ldc.i4.0
  IL_019b:  ldtoken    ""string""
  IL_01a0:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_01a5:  ldtoken    ""Program""
  IL_01aa:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_01af:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.Convert(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Type)""
  IL_01b4:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, string>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, string>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_01b9:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, string>> Program.<>o__1#1.<>p__0""
  IL_01be:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, string>> Program.<>o__1#1.<>p__0""
  IL_01c3:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, string> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, string>>.Target""
  IL_01c8:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, string>> Program.<>o__1#1.<>p__0""
  IL_01cd:  ldarg.0
  IL_01ce:  ldfld      ""<anonymous type: dynamic Head, dynamic Tail> Program.<Iterator>d__1.<temp>5__7""
  IL_01d3:  callvirt   ""dynamic <>f__AnonymousType0<dynamic, dynamic>.Head.get""
  IL_01d8:  callvirt   ""string System.Func<System.Runtime.CompilerServices.CallSite, dynamic, string>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_01dd:  stfld      ""string Program.<Iterator>d__1.<>2__current""
  IL_01e2:  ldarg.0
  IL_01e3:  ldc.i4.1
  IL_01e4:  stfld      ""int Program.<Iterator>d__1.<>1__state""
  IL_01e9:  ldc.i4.1
  IL_01ea:  ret
  IL_01eb:  ldarg.0
  IL_01ec:  ldc.i4.m1
  IL_01ed:  stfld      ""int Program.<Iterator>d__1.<>1__state""
  IL_01f2:  ldarg.0
  IL_01f3:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, <anonymous type: dynamic Head, dynamic Tail>>> Program.<>o__1#1.<>p__1""
  IL_01f8:  brfalse.s  IL_01fc
  IL_01fa:  br.s       IL_0220
  IL_01fc:  ldc.i4.0
  IL_01fd:  ldtoken    ""<>f__AnonymousType0<dynamic, dynamic>""
  IL_0202:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0207:  ldtoken    ""Program""
  IL_020c:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0211:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.Convert(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Type)""
  IL_0216:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, <anonymous type: dynamic Head, dynamic Tail>>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, <anonymous type: dynamic Head, dynamic Tail>>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_021b:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, <anonymous type: dynamic Head, dynamic Tail>>> Program.<>o__1#1.<>p__1""
  IL_0220:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, <anonymous type: dynamic Head, dynamic Tail>>> Program.<>o__1#1.<>p__1""
  IL_0225:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, <anonymous type: dynamic Head, dynamic Tail>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, <anonymous type: dynamic Head, dynamic Tail>>>.Target""
  IL_022a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, <anonymous type: dynamic Head, dynamic Tail>>> Program.<>o__1#1.<>p__1""
  IL_022f:  ldarg.0
  IL_0230:  ldfld      ""<anonymous type: dynamic Head, dynamic Tail> Program.<Iterator>d__1.<temp>5__7""
  IL_0235:  callvirt   ""dynamic <>f__AnonymousType0<dynamic, dynamic>.Tail.get""
  IL_023a:  callvirt   ""<anonymous type: dynamic Head, dynamic Tail> System.Func<System.Runtime.CompilerServices.CallSite, dynamic, <anonymous type: dynamic Head, dynamic Tail>>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_023f:  stfld      ""<anonymous type: dynamic Head, dynamic Tail> Program.<Iterator>d__1.<temp>5__7""
  IL_0244:  nop
  IL_0245:  ldarg.0
  IL_0246:  ldfld      ""<anonymous type: dynamic Head, dynamic Tail> Program.<Iterator>d__1.<temp>5__7""
  IL_024b:  ldnull
  IL_024c:  cgt.un
  IL_024e:  stloc.1
  IL_024f:  ldloc.1
  IL_0250:  brtrue     IL_018f
  IL_0255:  ldarg.0
  IL_0256:  ldarg.0
  IL_0257:  ldfld      ""System.Collections.Generic.IEnumerable<<anonymous type: string Value, int Length>> Program.<Iterator>d__1.<result>5__4""
  IL_025c:  ldsfld     ""System.Func<<anonymous type: string Value, int Length>, <anonymous type: <anonymous type: string Value, int Length> a, string value>> Program.<>c.<>9__1_7""
  IL_0261:  dup
  IL_0262:  brtrue.s   IL_027b
  IL_0264:  pop
  IL_0265:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_026a:  ldftn      ""<anonymous type: <anonymous type: string Value, int Length> a, string value> Program.<>c.<Iterator>b__1_7(<anonymous type: string Value, int Length>)""
  IL_0270:  newobj     ""System.Func<<anonymous type: string Value, int Length>, <anonymous type: <anonymous type: string Value, int Length> a, string value>>..ctor(object, System.IntPtr)""
  IL_0275:  dup
  IL_0276:  stsfld     ""System.Func<<anonymous type: string Value, int Length>, <anonymous type: <anonymous type: string Value, int Length> a, string value>> Program.<>c.<>9__1_7""
  IL_027b:  call       ""System.Collections.Generic.IEnumerable<<anonymous type: <anonymous type: string Value, int Length> a, string value>> System.Linq.Enumerable.Select<<anonymous type: string Value, int Length>, <anonymous type: <anonymous type: string Value, int Length> a, string value>>(System.Collections.Generic.IEnumerable<<anonymous type: string Value, int Length>>, System.Func<<anonymous type: string Value, int Length>, <anonymous type: <anonymous type: string Value, int Length> a, string value>>)""
  IL_0280:  ldsfld     ""System.Func<<anonymous type: <anonymous type: string Value, int Length> a, string value>, <anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>> Program.<>c.<>9__1_8""
  IL_0285:  dup
  IL_0286:  brtrue.s   IL_029f
  IL_0288:  pop
  IL_0289:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_028e:  ldftn      ""<anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length> Program.<>c.<Iterator>b__1_8(<anonymous type: <anonymous type: string Value, int Length> a, string value>)""
  IL_0294:  newobj     ""System.Func<<anonymous type: <anonymous type: string Value, int Length> a, string value>, <anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>>..ctor(object, System.IntPtr)""
  IL_0299:  dup
  IL_029a:  stsfld     ""System.Func<<anonymous type: <anonymous type: string Value, int Length> a, string value>, <anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>> Program.<>c.<>9__1_8""
  IL_029f:  call       ""System.Collections.Generic.IEnumerable<<anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>> System.Linq.Enumerable.Select<<anonymous type: <anonymous type: string Value, int Length> a, string value>, <anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>>(System.Collections.Generic.IEnumerable<<anonymous type: <anonymous type: string Value, int Length> a, string value>>, System.Func<<anonymous type: <anonymous type: string Value, int Length> a, string value>, <anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>>)""
  IL_02a4:  ldsfld     ""System.Func<<anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>, bool> Program.<>c.<>9__1_9""
  IL_02a9:  dup
  IL_02aa:  brtrue.s   IL_02c3
  IL_02ac:  pop
  IL_02ad:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_02b2:  ldftn      ""bool Program.<>c.<Iterator>b__1_9(<anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>)""
  IL_02b8:  newobj     ""System.Func<<anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>, bool>..ctor(object, System.IntPtr)""
  IL_02bd:  dup
  IL_02be:  stsfld     ""System.Func<<anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>, bool> Program.<>c.<>9__1_9""
  IL_02c3:  call       ""System.Collections.Generic.IEnumerable<<anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>> System.Linq.Enumerable.Where<<anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>>(System.Collections.Generic.IEnumerable<<anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>>, System.Func<<anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>, bool>)""
  IL_02c8:  ldsfld     ""System.Func<<anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>, string> Program.<>c.<>9__1_10""
  IL_02cd:  dup
  IL_02ce:  brtrue.s   IL_02e7
  IL_02d0:  pop
  IL_02d1:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_02d6:  ldftn      ""string Program.<>c.<Iterator>b__1_10(<anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>)""
  IL_02dc:  newobj     ""System.Func<<anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>, string>..ctor(object, System.IntPtr)""
  IL_02e1:  dup
  IL_02e2:  stsfld     ""System.Func<<anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>, string> Program.<>c.<>9__1_10""
  IL_02e7:  call       ""System.Collections.Generic.IEnumerable<string> System.Linq.Enumerable.Select<<anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>, string>(System.Collections.Generic.IEnumerable<<anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>>, System.Func<<anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>, string>)""
  IL_02ec:  stfld      ""System.Collections.Generic.IEnumerable<string> Program.<Iterator>d__1.<newArgs>5__6""
  IL_02f1:  ldarg.0
  IL_02f2:  ldarg.0
  IL_02f3:  ldfld      ""string[] Program.<Iterator>d__1.<args>5__1""
  IL_02f8:  ldarg.0
  IL_02f9:  ldfld      ""System.Collections.Generic.IEnumerable<string> Program.<Iterator>d__1.<newArgs>5__6""
  IL_02fe:  call       ""System.Collections.Generic.IEnumerable<string> System.Linq.Enumerable.Concat<string>(System.Collections.Generic.IEnumerable<string>, System.Collections.Generic.IEnumerable<string>)""
  IL_0303:  call       ""string[] System.Linq.Enumerable.ToArray<string>(System.Collections.Generic.IEnumerable<string>)""
  IL_0308:  stfld      ""string[] Program.<Iterator>d__1.<args>5__1""
  IL_030d:  ldarg.0
  IL_030e:  ldarg.0
  IL_030f:  ldfld      ""int Program.<Iterator>d__1.<i>5__3""
  IL_0314:  box        ""int""
  IL_0319:  ldarg.0
  IL_031a:  ldfld      ""<anonymous type: dynamic Head, dynamic Tail> Program.<Iterator>d__1.<list>5__2""
  IL_031f:  newobj     ""<>f__AnonymousType0<dynamic, dynamic>..ctor(dynamic, dynamic)""
  IL_0324:  stfld      ""<anonymous type: dynamic Head, dynamic Tail> Program.<Iterator>d__1.<list>5__2""
  IL_0329:  call       ""void System.Diagnostics.Debugger.Break()""
  IL_032e:  nop
  IL_032f:  nop
  IL_0330:  ldarg.0
  IL_0331:  ldnull
  IL_0332:  stfld      ""System.Collections.Generic.IEnumerable<<anonymous type: string Value, int Length>> Program.<Iterator>d__1.<result>5__4""
  IL_0337:  ldarg.0
  IL_0338:  ldnull
  IL_0339:  stfld      ""<anonymous type: string Head, dynamic Tail> Program.<Iterator>d__1.<linked>5__5""
  IL_033e:  ldarg.0
  IL_033f:  ldnull
  IL_0340:  stfld      ""<anonymous type: dynamic Head, dynamic Tail> Program.<Iterator>d__1.<temp>5__7""
  IL_0345:  ldarg.0
  IL_0346:  ldnull
  IL_0347:  stfld      ""System.Collections.Generic.IEnumerable<string> Program.<Iterator>d__1.<newArgs>5__6""
  IL_034c:  ldarg.0
  IL_034d:  ldfld      ""int Program.<Iterator>d__1.<i>5__3""
  IL_0352:  stloc.2
  IL_0353:  ldarg.0
  IL_0354:  ldloc.2
  IL_0355:  ldc.i4.1
  IL_0356:  add
  IL_0357:  stfld      ""int Program.<Iterator>d__1.<i>5__3""
  IL_035c:  ldarg.0
  IL_035d:  ldfld      ""int Program.<Iterator>d__1.<i>5__3""
  IL_0362:  ldc.i4.s   10
  IL_0364:  clt
  IL_0366:  stloc.3
  IL_0367:  ldloc.3
  IL_0368:  brtrue     IL_0068
  IL_036d:  call       ""void System.Diagnostics.Debugger.Break()""
  IL_0372:  nop
  IL_0373:  ldc.i4.0
  IL_0374:  ret
}");

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, f1, f2, GetSyntaxMapFromMarkers(source1, source2))));

            diff2.VerifySynthesizedMembers(
                "Program.<>o__1#1: {<>p__0, <>p__1}",
                "Program.<>o__1#2: {<>p__0, <>p__1, <>p__2}",
                "Program: {<>c, <>o__1#2, <Iterator>d__1, <>o__1#1}",
                "Program.<>c: {<>9__1_0, <>9__1_1, <>9__1_2, <>9__1_3, <>9__1_4, <>9__1_5, <>9__1_6, <>9__1_7, <>9__1_8, <>9__1_9, <>9__1_10, <Iterator>b__1_0, <Iterator>b__1_1, <Iterator>b__1_2, <Iterator>b__1_3, <Iterator>b__1_4, <Iterator>b__1_5, <Iterator>b__1_6, <Iterator>b__1_7, <Iterator>b__1_8, <Iterator>b__1_9, <Iterator>b__1_10}",
                "Program.<Iterator>d__1: {<>1__state, <>2__current, <>l__initialThreadId, <args>5__1, <list>5__2, <i>5__3, <result>5__4, <linked>5__5, <temp>5__7, <newArgs>5__6, System.IDisposable.Dispose, MoveNext, System.Collections.Generic.IEnumerator<System.String>.get_Current, System.Collections.IEnumerator.Reset, System.Collections.IEnumerator.get_Current, System.Collections.Generic.IEnumerable<System.String>.GetEnumerator, System.Collections.IEnumerable.GetEnumerator, System.Collections.Generic.IEnumerator<System.String>.Current, System.Collections.IEnumerator.Current}",
                "<>f__AnonymousType4<<a>j__TPar, <value>j__TPar>: {Equals, GetHashCode, ToString}",
                "<>f__AnonymousType1<<a>j__TPar, <x>j__TPar>: {Equals, GetHashCode, ToString}",
                "<>f__AnonymousType3<<Value>j__TPar, <Length>j__TPar>: {Equals, GetHashCode, ToString}",
                "<>f__AnonymousType0<<Head>j__TPar, <Tail>j__TPar>: {Equals, GetHashCode, ToString}",
                "<>f__AnonymousType5<<<>h__TransparentIdentifier0>j__TPar, <length>j__TPar>: {Equals, GetHashCode, ToString}",
                "<>f__AnonymousType2<<<>h__TransparentIdentifier0>j__TPar, <y>j__TPar>: {Equals, GetHashCode, ToString}");
        }

        [Fact, WorkItem(9119, "https://github.com/dotnet/roslyn/issues/9119")]
        public void MissingIteratorStateMachineAttribute()
        {
            var source0 = MarkedSource(@"
using System;
using System.Collections.Generic;

class C
{
    public IEnumerable<int> F()
    {
        int <N:0>a = 0</N:0>;
        <N:1>yield return 0;</N:1>
        Console.WriteLine(a);
    }
}
");
            var source1 = MarkedSource(@"
using System;
using System.Collections.Generic;

class C
{
    public IEnumerable<int> F()
    {
        int <N:0>a = 1</N:0>;
        <N:1>yield return 1;</N:1>
        Console.WriteLine(a);
    }
}
");

            var compilation0 = CreateCompilationWithMscorlib40(new[] { source0.Tree }, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);

            // older versions of mscorlib don't contain IteratorStateMachineAttribute
            Assert.Null(compilation0.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_IteratorStateMachineAttribute__ctor));

            var v0 = CompileAndVerify(compilation0);
            v0.VerifyDiagnostics();
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

            diff1.EmitResult.Diagnostics.Verify(
                // (7,29): error CS7043: Cannot emit update; attribute 'System.Runtime.CompilerServices.IteratorStateMachineAttribute' is missing.
                Diagnostic(ErrorCode.ERR_EncUpdateFailedMissingSymbol, "F").WithArguments(CodeAnalysisResources.Attribute, "System.Runtime.CompilerServices.IteratorStateMachineAttribute").WithLocation(7, 29));
        }

        [Fact, WorkItem(9119, "https://github.com/dotnet/roslyn/issues/9119")]
        public void BadIteratorStateMachineAttribute()
        {
            var source0 = MarkedSource(@"
using System;
using System.Collections.Generic;

namespace System.Runtime.CompilerServices
{
    public class IteratorStateMachineAttribute : Attribute { }
}

class C
{
    public IEnumerable<int> F()
    {
        int <N:0>a = 0</N:0>;
        <N:1>yield return 0;</N:1>
        Console.WriteLine(a);
    }
}
");
            var source1 = MarkedSource(@"
using System;
using System.Collections.Generic;

namespace System.Runtime.CompilerServices
{
    public class IteratorStateMachineAttribute : Attribute { }
}

class C
{
    public IEnumerable<int> F()
    {
        int <N:0>a = 1</N:0>;
        <N:1>yield return 1;</N:1>
        Console.WriteLine(a);
    }
}
");

            var compilation0 = CreateCompilation(new[] { source0.Tree }, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);

            // the ctor is missing a parameter
            Assert.Null(compilation0.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_IteratorStateMachineAttribute__ctor));

            var v0 = CompileAndVerify(compilation0);
            v0.VerifyDiagnostics();
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

            diff1.EmitResult.Diagnostics.Verify(
                // (12,29): error CS7043: Cannot emit update; attribute 'System.Runtime.CompilerServices.IteratorStateMachineAttribute' is missing.
                //     public IEnumerable<int> F()
                Diagnostic(ErrorCode.ERR_EncUpdateFailedMissingSymbol, "F").WithArguments(CodeAnalysisResources.Attribute, "System.Runtime.CompilerServices.IteratorStateMachineAttribute").WithLocation(12, 29));
        }

        [Fact]
        public void AddedIteratorStateMachineAttribute()
        {
            var source0 = MarkedSource(@"
using System;
using System.Collections.Generic;


class C
{
    public IEnumerable<int> F()
    {
        int <N:0>a = 0</N:0>;
        <N:1>yield return 0;</N:1>
        Console.WriteLine(a);
    }
}
");
            var source1 = MarkedSource(@"
using System;
using System.Collections.Generic;

namespace System.Runtime.CompilerServices
{
    public class IteratorStateMachineAttribute : Attribute { public IteratorStateMachineAttribute(Type type) { } }
}

class C
{
    public IEnumerable<int> F()
    {
        int <N:0>a = 1</N:0>;
        <N:1>yield return 1;</N:1>
        Console.WriteLine(a);
    }
}
");

            var compilation0 = CreateCompilationWithMscorlib40(new[] { source0.Tree }, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);

            // older versions of mscorlib don't contain IteratorStateMachineAttribute
            Assert.Null(compilation0.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_IteratorStateMachineAttribute__ctor));

            var v0 = CompileAndVerify(compilation0);
            v0.VerifyDiagnostics();
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");
            var ism1 = compilation1.GetMember<TypeSymbol>("System.Runtime.CompilerServices.IteratorStateMachineAttribute");

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Insert, null, ism1),
                    SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

            // We conclude the original method wasn't a state machine.
            // The IDE however reports a Rude Edit in that case.
            diff1.EmitResult.Diagnostics.Verify();
        }

        [Fact]
        public void SourceIteratorStateMachineAttribute()
        {
            var source0 = MarkedSource(@"
using System;
using System.Collections.Generic;

namespace System.Runtime.CompilerServices
{
    public class IteratorStateMachineAttribute : Attribute { public IteratorStateMachineAttribute(Type type) { } }
}

class C
{
    public IEnumerable<int> F()
    {
        int <N:0>a = 0</N:0>;
        <N:1>yield return 0;</N:1>
        Console.WriteLine(a);
    }
}
");
            var source1 = MarkedSource(@"
using System;
using System.Collections.Generic;

namespace System.Runtime.CompilerServices
{
    public class IteratorStateMachineAttribute : Attribute { public IteratorStateMachineAttribute(Type type) { } }
}

class C
{
    public IEnumerable<int> F()
    {
        int <N:0>a = 1</N:0>;
        <N:1>yield return 1;</N:1>
        Console.WriteLine(a);
    }
}
");

            var compilation0 = CreateCompilation(new[] { source0.Tree }, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);

            // older versions of mscorlib don't contain IteratorStateMachineAttribute
            Assert.NotNull(compilation0.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_IteratorStateMachineAttribute__ctor));

            var v0 = CompileAndVerify(compilation0);
            v0.VerifyDiagnostics();
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

            diff1.EmitResult.Diagnostics.Verify();
        }

        [Fact, WorkItem(9119, "https://github.com/dotnet/roslyn/issues/9119")]
        public void MissingAsyncStateMachineAttribute()
        {
            var source0 = MarkedSource(@"
using System.Threading.Tasks;

class C
{
    public async Task<int> F()
    {
        int <N:0>a = 0</N:0>;
        <N:1>await new Task();</N:1>
        return a;
    }
}
");
            var source1 = MarkedSource(@"
using System.Threading.Tasks;

class C
{
    public async Task<int> F()
    {
        int <N:0>a = 1</N:0>;
        <N:1>await new Task();</N:1>
        return a;
    }
}
");

            var compilation0 = CreateEmptyCompilation(new[] { source0.Tree }, new[] { TestReferences.NetFx.Minimal.mincorlib, TestReferences.NetFx.Minimal.minasync }, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);

            // older versions of mscorlib don't contain AsyncStateMachineAttribute, IteratorStateMachineAttribute
            Assert.Null(compilation0.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_AsyncStateMachineAttribute__ctor));
            Assert.Null(compilation0.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_IteratorStateMachineAttribute__ctor));

            var v0 = CompileAndVerify(compilation0, verify: Verification.FailsPEVerify);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

            diff1.EmitResult.Diagnostics.Verify(
                // error CS7043: Cannot emit update; constructor 'System.Exception..ctor(string)' is missing.
                Diagnostic(ErrorCode.ERR_EncUpdateFailedMissingSymbol).WithArguments("constructor", "System.Exception..ctor(string)").WithLocation(1, 1),
                // (6,28): error CS7043: Cannot emit update; attribute 'System.Runtime.CompilerServices.AsyncStateMachineAttribute' is missing.
                //     public async Task<int> F()
                Diagnostic(ErrorCode.ERR_EncUpdateFailedMissingSymbol, "F").WithArguments("attribute", "System.Runtime.CompilerServices.AsyncStateMachineAttribute").WithLocation(6, 28));
        }

        [Fact]
        public void AddedAsyncStateMachineAttribute()
        {
            var source0 = MarkedSource(@"
using System.Threading.Tasks;

class C
{
    public async Task<int> F()
    {
        int <N:0>a = 0</N:0>;
        <N:1>await new Task<int>()</N:1>;
        return a;
    }
}
");
            var source1 = MarkedSource(@"
using System.Threading.Tasks;

namespace System.Runtime.CompilerServices
{
    public class AsyncStateMachineAttribute : Attribute { public AsyncStateMachineAttribute(Type type) { } }
}

class C
{
    public async Task<int> F()
    {
        int <N:0>a = 1</N:0>;
        <N:1>await new Task<int>()</N:1>;
        return a;
    }
}
");

            var compilation0 = CreateEmptyCompilation(new[] { source0.Tree }, new[] { TestReferences.NetFx.Minimal.mincorlib, TestReferences.NetFx.Minimal.minasync }, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);

            // older versions of mscorlib don't contain IteratorStateMachineAttribute
            Assert.Null(compilation0.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_AsyncStateMachineAttribute__ctor));

            var v0 = CompileAndVerify(compilation0, verify: Verification.FailsPEVerify);
            v0.VerifyDiagnostics();
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");
            var asm1 = compilation1.GetMember<TypeSymbol>("System.Runtime.CompilerServices.AsyncStateMachineAttribute");

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Insert, null, asm1),
                    SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

            diff1.EmitResult.Diagnostics.Verify(
                // error CS7043: Cannot emit update; constructor 'System.Exception..ctor(string)' is missing.
                Diagnostic(ErrorCode.ERR_EncUpdateFailedMissingSymbol).WithArguments("constructor", "System.Exception..ctor(string)"));
        }

        [Fact]
        public void SourceAsyncStateMachineAttribute()
        {
            var source0 = MarkedSource(@"
using System.Threading.Tasks;

namespace System.Runtime.CompilerServices
{
    public class AsyncStateMachineAttribute : Attribute { public AsyncStateMachineAttribute(Type type) { } }
}

class C
{
    public async Task<int> F()
    {
        int <N:0>a = 0</N:0>;
        <N:1>await new Task<int>()</N:1>;
        return a;
    }
}
");
            var source1 = MarkedSource(@"
using System.Threading.Tasks;

namespace System.Runtime.CompilerServices
{
    public class AsyncStateMachineAttribute : Attribute { public AsyncStateMachineAttribute(Type type) { } }
}

class C
{
    public async Task<int> F()
    {
        int <N:0>a = 1</N:0>;
        <N:1>await new Task<int>()</N:1>;
        return a;
    }
}
");

            var compilation0 = CreateEmptyCompilation(new[] { source0.Tree }, new[] { TestReferences.NetFx.Minimal.mincorlib, TestReferences.NetFx.Minimal.minasync }, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);

            Assert.NotNull(compilation0.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_AsyncStateMachineAttribute__ctor));

            var v0 = CompileAndVerify(compilation0, verify: Verification.FailsPEVerify);
            v0.VerifyDiagnostics();

            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

            diff1.EmitResult.Diagnostics.Verify(
                // error CS7043: Cannot emit update; constructor 'System.Exception..ctor(string)' is missing.
                Diagnostic(ErrorCode.ERR_EncUpdateFailedMissingSymbol).WithArguments(CodeAnalysisResources.Constructor, "System.Exception..ctor(string)"));
        }

        [Fact, WorkItem(10190, "https://github.com/dotnet/roslyn/issues/10190")]
        public void NonAsyncToAsync()
        {
            var source0 = MarkedSource(@"
using System.Threading.Tasks;

class C
{
    public Task<int> F()
    {
        int <N:0>a = 0</N:0>;
        return Task.FromResult(a);
    }
}
");
            var source1 = MarkedSource(@"
using System.Threading.Tasks;

class C
{
    public async Task<int> F()
    {
        int <N:0>a = 1</N:0>;
        return <N:1>await Task.FromResult(a)</N:1>;
    }
}
");

            var compilation0 = CreateEmptyCompilation(new[] { source0.Tree }, new[] { NetFramework.mscorlib }, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);

            Assert.NotNull(compilation0.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_AsyncStateMachineAttribute__ctor));

            var v0 = CompileAndVerify(compilation0, verify: Verification.Passes);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

            diff1.EmitResult.Diagnostics.Verify();
        }

        [Fact]
        public void NonAsyncToAsync_MissingAttribute()
        {
            var source0 = MarkedSource(@"
using System.Threading.Tasks;

class C
{
    public Task<int> F()
    {
        int <N:0>a = 0</N:0>;
        a++;
        <N:1>return new Task<int>();</N:1>
    }
}
");
            var source1 = MarkedSource(@"
using System.Threading.Tasks;

class C
{
    public async Task<int> F()
    {
        int <N:0>a = 1</N:0>;
        a++;
        <N:1>return await new Task<int>();</N:1>
    }
}
");

            var compilation0 = CreateEmptyCompilation(new[] { source0.Tree }, new[] { TestReferences.NetFx.Minimal.mincorlib, TestReferences.NetFx.Minimal.minasync }, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);

            Assert.Null(compilation0.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_AsyncStateMachineAttribute__ctor));

            var v0 = CompileAndVerify(compilation0, verify: Verification.FailsPEVerify);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

            diff1.EmitResult.Diagnostics.Verify(
                    // error CS7043: Cannot emit update; constructor 'System.Exception..ctor(string)' is missing.
                    Diagnostic(ErrorCode.ERR_EncUpdateFailedMissingSymbol).WithArguments("constructor", "System.Exception..ctor(string)").WithLocation(1, 1),
                    // (6,28): error CS7043: Cannot emit update; attribute 'System.Runtime.CompilerServices.AsyncStateMachineAttribute' is missing.
                    //     public async Task<int> F()
                    Diagnostic(ErrorCode.ERR_EncUpdateFailedMissingSymbol, "F").WithArguments("attribute", "System.Runtime.CompilerServices.AsyncStateMachineAttribute").WithLocation(6, 28));
        }

        [Fact]
        public void NonIteratorToIterator_MissingAttribute()
        {
            var source0 = MarkedSource(@"
using System.Collections.Generic;

class C
{
    public IEnumerable<int> F()
    {
        int <N:0>a = 0</N:0>;
        <N:1>return new int[] { a };</N:1>
    }
}
");
            var source1 = MarkedSource(@"
using System.Collections.Generic;

class C
{
    public IEnumerable<int> F()
    {
        int <N:0>a = 1</N:0>;
        <N:1>yield return a;</N:1>
    }
}
");

            var compilation0 = CreateEmptyCompilation(new[] { source0.Tree }, new[] { Net20.References.mscorlib }, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);

            Assert.Null(compilation0.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_IteratorStateMachineAttribute__ctor));

            var v0 = CompileAndVerify(compilation0, verify: Verification.Passes);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

            diff1.EmitResult.Diagnostics.Verify(
                // (6,29): error CS7043: Cannot emit update; attribute 'System.Runtime.CompilerServices.IteratorStateMachineAttribute' is missing.
                Diagnostic(ErrorCode.ERR_EncUpdateFailedMissingSymbol, "F").WithArguments(CodeAnalysisResources.Attribute, "System.Runtime.CompilerServices.IteratorStateMachineAttribute").WithLocation(6, 29));
        }

        [Fact]
        public void NonIteratorToIterator_SourceAttribute()
        {
            var source0 = MarkedSource(@"
using System.Collections.Generic;

namespace System.Runtime.CompilerServices
{
    public class IteratorStateMachineAttribute : Attribute { public IteratorStateMachineAttribute(Type type) { } }
}

class C
{
    public IEnumerable<int> F()
    {
        int <N:0>a = 0</N:0>;
        <N:1>return new int[] { a };</N:1>
    }
}
");
            var source1 = MarkedSource(@"
using System.Collections.Generic;

namespace System.Runtime.CompilerServices
{
    public class IteratorStateMachineAttribute : Attribute { public IteratorStateMachineAttribute(Type type) { } }
}

class C
{
    public IEnumerable<int> F()
    {
        int <N:0>a = 1</N:0>;
        <N:1>yield return a;</N:1>
    }
}
");

            var compilation0 = CreateEmptyCompilation(new[] { source0.Tree }, new[] { Net20.References.mscorlib }, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);

            Assert.NotNull(compilation0.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_IteratorStateMachineAttribute__ctor));

            var v0 = CompileAndVerify(compilation0, verify: Verification.Passes);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

            diff1.EmitResult.Diagnostics.Verify();
        }

        [Fact]
        public void NonAsyncToAsyncLambda()
        {
            var source0 = MarkedSource(@"
using System.Threading.Tasks;

class C
{
    public object F()
    {
        return new System.Func<Task<int>>(<N:2>() =>
        <N:3>{
           int <N:0>a = 0</N:0>;
           <N:1>return Task.FromResult(a);</N:1>
        }</N:3></N:2>);
    }
}
");
            var source1 = MarkedSource(@"
using System.Threading.Tasks;

class C
{
    public object F()
    {
        return new System.Func<Task<int>>(<N:2>async () =>
        <N:3>{
           int <N:0>a = 0</N:0>;
           <N:1>return await Task.FromResult(a);</N:1>
        }</N:3></N:2>);
    }
}
");

            var compilation0 = CreateEmptyCompilation(new[] { source0.Tree }, new[] { NetFramework.mscorlib }, options: ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            var compilation1 = compilation0.WithSource(source1.Tree);

            Assert.NotNull(compilation0.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_AsyncStateMachineAttribute__ctor));

            var v0 = CompileAndVerify(compilation0, verify: Verification.Passes);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

            diff1.EmitResult.Diagnostics.Verify();

            diff1.VerifySynthesizedMembers(
                "C: {<>c}",
                "C.<>c: {<>9__0_0, <F>b__0_0, <<F>b__0_0>d}",
                "C.<>c.<<F>b__0_0>d: {<>1__state, <>t__builder, <>4__this, <a>5__1, <>s__2, <>u__1, MoveNext, SetStateMachine}");
        }

        [Fact]
        public void AsyncMethodWithNullableParameterAddingNullCheck()
        {
            var source0 = MarkedSource(@"
using System;
using System.Threading.Tasks;
#nullable enable

class C
{
    static T id<T>(T t) => t;
    static Task<T> G<T>(Func<T> f) => Task.FromResult(f());
    static T H<T>(Func<T> f) => f();

    public async void F(string? x)
    <N:4>{</N:4>
        var <N:2>y = <N:5>await G(<N:0>() => new { A = id(x) }</N:0>)</N:5></N:2>;
        var <N:3>z = H(<N:1>() => y.A</N:1>)</N:3>;
    }
}
", options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp9));
            var source1 = MarkedSource(@"
using System;
using System.Threading.Tasks;
#nullable enable

class C
{
    static T id<T>(T t) => t;
    static Task<T> G<T>(Func<T> f) => Task.FromResult(f());
    static T H<T>(Func<T> f) => f();

    public async void F(string? x)
    <N:4>{</N:4>
        if (x is null) throw new Exception();
        var <N:2>y = <N:5>await G(<N:0>() => new { A = id(x) }</N:0>)</N:5></N:2>;
        var <N:3>z = H(<N:1>() => y.A</N:1>)</N:3>;
    }
}
", options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp9));

            var compilation0 = CreateCompilationWithMscorlib461(new[] { source0.Tree }, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);

            Assert.NotNull(compilation0.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_AsyncStateMachineAttribute__ctor));

            var v0 = CompileAndVerify(compilation0, verify: Verification.Passes);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");

            var generation0 = CreateInitialBaseline(compilation0, md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))));

            diff1.EmitResult.Diagnostics.Verify();

            diff1.VerifySynthesizedMembers(
                "System.Runtime.CompilerServices.NullableContextAttribute",
                "Microsoft.CodeAnalysis.EmbeddedAttribute",
                "System.Runtime.CompilerServices.NullableAttribute",
                "C.<>c__DisplayClass3_0: {x, y, <F>b__1, <F>b__0}",
                "<>f__AnonymousType0<<A>j__TPar>: {Equals, GetHashCode, ToString}",
                "C: {<>c__DisplayClass3_0, <F>d__3}",
                "C.<F>d__3: {<>1__state, <>t__builder, x, <>4__this, <>8__1, <z>5__2, <>s__3, <>u__1, MoveNext, SetStateMachine}");

            diff1.VerifyIL("C.<>c__DisplayClass3_0.<F>b__1()", @"
{
  // Code size       17 (0x11)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""string C.<>c__DisplayClass3_0.x""
  IL_0006:  call       ""string C.id<string>(string)""
  IL_000b:  newobj     ""<>f__AnonymousType0<string>..ctor(string)""
  IL_0010:  ret
}
");

            diff1.VerifyIL("C.<>c__DisplayClass3_0.<F>b__0()", @"
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""<anonymous type: string A> C.<>c__DisplayClass3_0.y""
  IL_0006:  callvirt   ""string <>f__AnonymousType0<string>.A.get""
  IL_000b:  ret
}
");
        }
    }
}
