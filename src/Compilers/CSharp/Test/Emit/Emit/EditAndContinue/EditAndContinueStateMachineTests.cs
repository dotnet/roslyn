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
        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
        [WorkItem(1068894, "DevDiv"), WorkItem(1137300, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1137300")]
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
}";
            var compilation0 = CreateCompilationWithMscorlib40(new[] { Parse(source0, "a.cs") }, options: TestOptions.DebugDll);
            var compilation1 = CreateCompilationWithMscorlib40(new[] { Parse(source1, "a.cs") }, options: TestOptions.DebugDll);

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
                    Row(3, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                    Row(4, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                    Row(4, TableIndex.TypeDef, EditAndContinueOperation.Default),
                    Row(2, TableIndex.PropertyMap, EditAndContinueOperation.Default),
                    Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                    Row(4, TableIndex.Field, EditAndContinueOperation.Default),
                    Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                    Row(5, TableIndex.Field, EditAndContinueOperation.Default),
                    Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                    Row(6, TableIndex.Field, EditAndContinueOperation.Default),
                    Row(2, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                    Row(11, TableIndex.MethodDef, EditAndContinueOperation.Default),
                    Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
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
                    Row(2, TableIndex.PropertyMap, EditAndContinueOperation.AddProperty),
                    Row(3, TableIndex.Property, EditAndContinueOperation.Default),
                    Row(2, TableIndex.PropertyMap, EditAndContinueOperation.AddProperty),
                    Row(4, TableIndex.Property, EditAndContinueOperation.Default),
                    Row(12, TableIndex.MethodDef, EditAndContinueOperation.AddParameter),
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
                    Handle(11, TableIndex.MethodDef),
                    Handle(12, TableIndex.MethodDef),
                    Handle(13, TableIndex.MethodDef),
                    Handle(14, TableIndex.MethodDef),
                    Handle(15, TableIndex.MethodDef),
                    Handle(16, TableIndex.MethodDef),
                    Handle(17, TableIndex.MethodDef),
                    Handle(18, TableIndex.MethodDef),
                    Handle(19, TableIndex.MethodDef),
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
                    Handle(3, TableIndex.StandAloneSig),
                    Handle(4, TableIndex.StandAloneSig),
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
    <file id=""1"" name=""a.cs"" language=""C#"" checksumAlgorithm=""SHA1"" checksum=""61-E4-46-A3-DE-2B-DE-69-1A-31-07-F6-EA-02-CE-B0-5F-38-03-79"" />
  </files>
  <methods>
    <method token=""0x600000b"">
      <customDebugInfo>
        <forwardIterator name=""&lt;G&gt;d__1#1"" />
      </customDebugInfo>
    </method>
    <method token=""0x600000e"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""1"" />
        <entry offset=""0x1b"" startLine=""9"" startColumn=""5"" endLine=""9"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1c"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""24"" document=""1"" />
        <entry offset=""0x2c"" hidden=""true"" document=""1"" />
        <entry offset=""0x33"" startLine=""11"" startColumn=""5"" endLine=""11"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x35"">
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
                        Row(6, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(9, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(12, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(16, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(17, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(18, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(19, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(20, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(21, TableIndex.CustomAttribute, EditAndContinueOperation.Default));
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
                diff1.VerifyUpdatedMethods("0x06000005");

                // Verify delta metadata contains expected rows.
                using (var md1 = diff1.GetMetadata())
                {
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
                        Row(13, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(14, TableIndex.CustomAttribute, EditAndContinueOperation.Default));

                    diff1.VerifyIL("C.<F>d__0.System.Collections.IEnumerator.MoveNext", @"
{
  // Code size       53 (0x35)
  .maxstack  2
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<F>d__0.<>1__state""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  brfalse.s  IL_0014
  IL_000a:  br.s       IL_000c
  IL_000c:  ldloc.0
  IL_000d:  ldc.i4.1
  IL_000e:  beq.s      IL_002c
  IL_0010:  br.s       IL_0012
  IL_0012:  ldc.i4.0
  IL_0013:  ret
  IL_0014:  ldarg.0
  IL_0015:  ldc.i4.m1
  IL_0016:  stfld      ""int C.<F>d__0.<>1__state""
  IL_001b:  nop
  IL_001c:  ldarg.0
  IL_001d:  ldc.i4.2
  IL_001e:  stfld      ""int C.<F>d__0.<>2__current""
  IL_0023:  ldarg.0
  IL_0024:  ldc.i4.1
  IL_0025:  stfld      ""int C.<F>d__0.<>1__state""
  IL_002a:  ldc.i4.1
  IL_002b:  ret
  IL_002c:  ldarg.0
  IL_002d:  ldc.i4.m1
  IL_002e:  stfld      ""int C.<F>d__0.<>1__state""
  IL_0033:  ldc.i4.0
  IL_0034:  ret
}");
                    v0.VerifyIL("C.<F>d__0.System.Collections.IEnumerator.MoveNext", @"
{
  // Code size       53 (0x35)
  .maxstack  2
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<F>d__0.<>1__state""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  brfalse.s  IL_0014
  IL_000a:  br.s       IL_000c
  IL_000c:  ldloc.0
  IL_000d:  ldc.i4.1
  IL_000e:  beq.s      IL_002c
  IL_0010:  br.s       IL_0012
  IL_0012:  ldc.i4.0
  IL_0013:  ret
  IL_0014:  ldarg.0
  IL_0015:  ldc.i4.m1
  IL_0016:  stfld      ""int C.<F>d__0.<>1__state""
  IL_001b:  nop
  IL_001c:  ldarg.0
  IL_001d:  ldc.i4.1
  IL_001e:  stfld      ""int C.<F>d__0.<>2__current""
  IL_0023:  ldarg.0
  IL_0024:  ldc.i4.1
  IL_0025:  stfld      ""int C.<F>d__0.<>1__state""
  IL_002a:  ldc.i4.1
  IL_002b:  ret
  IL_002c:  ldarg.0
  IL_002d:  ldc.i4.m1
  IL_002e:  stfld      ""int C.<F>d__0.<>1__state""
  IL_0033:  ldc.i4.0
  IL_0034:  ret
}");
                }
            }
        }

        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
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
                diff1.VerifyUpdatedMethods("0x06000004");

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

                    diff1.VerifyIL("C.<F>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      160 (0xa0)
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
    IL_0008:  brfalse.s  IL_0047
    IL_000a:  br.s       IL_000c
    IL_000c:  nop
    IL_000d:  ldc.i4.s   10
    IL_000f:  call       ""System.Threading.Tasks.Task<int> System.Threading.Tasks.Task.FromResult<int>(int)""
    IL_0014:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0019:  stloc.2
    IL_001a:  ldloca.s   V_2
    IL_001c:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_0021:  brtrue.s   IL_0063
    IL_0023:  ldarg.0
    IL_0024:  ldc.i4.0
    IL_0025:  dup
    IL_0026:  stloc.0
    IL_0027:  stfld      ""int C.<F>d__0.<>1__state""
    IL_002c:  ldarg.0
    IL_002d:  ldloc.2
    IL_002e:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> C.<F>d__0.<>u__1""
    IL_0033:  ldarg.0
    IL_0034:  stloc.3
    IL_0035:  ldarg.0
    IL_0036:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__0.<>t__builder""
    IL_003b:  ldloca.s   V_2
    IL_003d:  ldloca.s   V_3
    IL_003f:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, C.<F>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref C.<F>d__0)""
    IL_0044:  nop
    IL_0045:  leave.s    IL_009f
    IL_0047:  ldarg.0
    IL_0048:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> C.<F>d__0.<>u__1""
    IL_004d:  stloc.2
    IL_004e:  ldarg.0
    IL_004f:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> C.<F>d__0.<>u__1""
    IL_0054:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_005a:  ldarg.0
    IL_005b:  ldc.i4.m1
    IL_005c:  dup
    IL_005d:  stloc.0
    IL_005e:  stfld      ""int C.<F>d__0.<>1__state""
    IL_0063:  ldloca.s   V_2
    IL_0065:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_006a:  pop
    IL_006b:  ldc.i4.s   20
    IL_006d:  stloc.1
    IL_006e:  leave.s    IL_008a
  }
  catch System.Exception
  {
    IL_0070:  stloc.s    V_4
    IL_0072:  ldarg.0
    IL_0073:  ldc.i4.s   -2
    IL_0075:  stfld      ""int C.<F>d__0.<>1__state""
    IL_007a:  ldarg.0
    IL_007b:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__0.<>t__builder""
    IL_0080:  ldloc.s    V_4
    IL_0082:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)""
    IL_0087:  nop
    IL_0088:  leave.s    IL_009f
  }
  IL_008a:  ldarg.0
  IL_008b:  ldc.i4.s   -2
  IL_008d:  stfld      ""int C.<F>d__0.<>1__state""
  IL_0092:  ldarg.0
  IL_0093:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__0.<>t__builder""
  IL_0098:  ldloc.1
  IL_0099:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetResult(int)""
  IL_009e:  nop
  IL_009f:  ret
}");
                    v0.VerifyIL("C.<F>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      158 (0x9e)
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
    IL_0008:  brfalse.s  IL_0046
    IL_000a:  br.s       IL_000c
   -IL_000c:  nop
   -IL_000d:  ldc.i4.1
    IL_000e:  call       ""System.Threading.Tasks.Task<int> System.Threading.Tasks.Task.FromResult<int>(int)""
    IL_0013:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0018:  stloc.2
   ~IL_0019:  ldloca.s   V_2
    IL_001b:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_0020:  brtrue.s   IL_0062
    IL_0022:  ldarg.0
    IL_0023:  ldc.i4.0
    IL_0024:  dup
    IL_0025:  stloc.0
    IL_0026:  stfld      ""int C.<F>d__0.<>1__state""
   <IL_002b:  ldarg.0
    IL_002c:  ldloc.2
    IL_002d:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> C.<F>d__0.<>u__1""
    IL_0032:  ldarg.0
    IL_0033:  stloc.3
    IL_0034:  ldarg.0
    IL_0035:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__0.<>t__builder""
    IL_003a:  ldloca.s   V_2
    IL_003c:  ldloca.s   V_3
    IL_003e:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, C.<F>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref C.<F>d__0)""
    IL_0043:  nop
    IL_0044:  leave.s    IL_009d
   >IL_0046:  ldarg.0
    IL_0047:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> C.<F>d__0.<>u__1""
    IL_004c:  stloc.2
    IL_004d:  ldarg.0
    IL_004e:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> C.<F>d__0.<>u__1""
    IL_0053:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0059:  ldarg.0
    IL_005a:  ldc.i4.m1
    IL_005b:  dup
    IL_005c:  stloc.0
    IL_005d:  stfld      ""int C.<F>d__0.<>1__state""
    IL_0062:  ldloca.s   V_2
    IL_0064:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0069:  pop
   -IL_006a:  ldc.i4.2
    IL_006b:  stloc.1
    IL_006c:  leave.s    IL_0088
  }
  catch System.Exception
  {
   ~IL_006e:  stloc.s    V_4
    IL_0070:  ldarg.0
    IL_0071:  ldc.i4.s   -2
    IL_0073:  stfld      ""int C.<F>d__0.<>1__state""
    IL_0078:  ldarg.0
    IL_0079:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__0.<>t__builder""
    IL_007e:  ldloc.s    V_4
    IL_0080:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)""
    IL_0085:  nop
    IL_0086:  leave.s    IL_009d
  }
 -IL_0088:  ldarg.0
  IL_0089:  ldc.i4.s   -2
  IL_008b:  stfld      ""int C.<F>d__0.<>1__state""
 ~IL_0090:  ldarg.0
  IL_0091:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__0.<>t__builder""
  IL_0096:  ldloc.1
  IL_0097:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetResult(int)""
  IL_009c:  nop
  IL_009d:  ret
}", sequencePoints: "C+<F>d__0.MoveNext");

                    v0.VerifyPdb("C+<F>d__0.MoveNext", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
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
        <entry offset=""0x0"" hidden=""true"" document=""1"" />
        <entry offset=""0x7"" hidden=""true"" document=""1"" />
        <entry offset=""0xc"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" document=""1"" />
        <entry offset=""0xd"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""34"" document=""1"" />
        <entry offset=""0x19"" hidden=""true"" document=""1"" />
        <entry offset=""0x6a"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""18"" document=""1"" />
        <entry offset=""0x6e"" hidden=""true"" document=""1"" />
        <entry offset=""0x88"" startLine=""10"" startColumn=""5"" endLine=""10"" endColumn=""6"" document=""1"" />
        <entry offset=""0x90"" hidden=""true"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x9e"">
        <namespace name=""System.Threading.Tasks"" />
      </scope>
      <asyncInfo>
        <kickoffMethod declaringType=""C"" methodName=""F"" />
        <await yield=""0x2b"" resume=""0x46"" declaringType=""C+&lt;F&gt;d__0"" methodName=""MoveNext"" />
      </asyncInfo>
    </method>
  </methods>
</symbols>");
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
                        Row(3, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(13, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(14, TableIndex.CustomAttribute, EditAndContinueOperation.Default));

                    diff1.VerifyIL("C.<F>d__0.System.Collections.IEnumerator.MoveNext", @"
{
  // Code size       65 (0x41)
  .maxstack  2
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<F>d__0.<>1__state""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  brfalse.s  IL_0014
  IL_000a:  br.s       IL_000c
  IL_000c:  ldloc.0
  IL_000d:  ldc.i4.1
  IL_000e:  beq.s      IL_0038
  IL_0010:  br.s       IL_0012
  IL_0012:  ldc.i4.0
  IL_0013:  ret
  IL_0014:  ldarg.0
  IL_0015:  ldc.i4.m1
  IL_0016:  stfld      ""int C.<F>d__0.<>1__state""
  IL_001b:  nop
  IL_001c:  ldarg.0
  IL_001d:  ldarg.0
  IL_001e:  ldfld      ""int C.<F>d__0.p""
  IL_0023:  stfld      ""int C.<F>d__0.<x>5__1""
  IL_0028:  ldarg.0
  IL_0029:  ldc.i4.2
  IL_002a:  stfld      ""int C.<F>d__0.<>2__current""
  IL_002f:  ldarg.0
  IL_0030:  ldc.i4.1
  IL_0031:  stfld      ""int C.<F>d__0.<>1__state""
  IL_0036:  ldc.i4.1
  IL_0037:  ret
  IL_0038:  ldarg.0
  IL_0039:  ldc.i4.m1
  IL_003a:  stfld      ""int C.<F>d__0.<>1__state""
  IL_003f:  ldc.i4.0
  IL_0040:  ret
}");
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
                        Row(3, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                        Row(7, TableIndex.Field, EditAndContinueOperation.Default),
                        Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(13, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(14, TableIndex.CustomAttribute, EditAndContinueOperation.Default));

                    diff1.VerifyIL("C.<F>d__0.System.Collections.IEnumerator.MoveNext", @"
{
  // Code size       93 (0x5d)
  .maxstack  2
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<F>d__0.<>1__state""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  brfalse.s  IL_0014
  IL_000a:  br.s       IL_000c
  IL_000c:  ldloc.0
  IL_000d:  ldc.i4.1
  IL_000e:  beq.s      IL_0048
  IL_0010:  br.s       IL_0012
  IL_0012:  ldc.i4.0
  IL_0013:  ret
  IL_0014:  ldarg.0
  IL_0015:  ldc.i4.m1
  IL_0016:  stfld      ""int C.<F>d__0.<>1__state""
  IL_001b:  nop
  IL_001c:  ldarg.0
  IL_001d:  ldc.i4     0x4d2
  IL_0022:  stfld      ""int C.<F>d__0.<y>5__2""
  IL_0027:  ldarg.0
  IL_0028:  ldarg.0
  IL_0029:  ldfld      ""int C.<F>d__0.p""
  IL_002e:  stfld      ""int C.<F>d__0.<x>5__1""
  IL_0033:  ldarg.0
  IL_0034:  ldarg.0
  IL_0035:  ldfld      ""int C.<F>d__0.<y>5__2""
  IL_003a:  stfld      ""int C.<F>d__0.<>2__current""
  IL_003f:  ldarg.0
  IL_0040:  ldc.i4.1
  IL_0041:  stfld      ""int C.<F>d__0.<>1__state""
  IL_0046:  ldc.i4.1
  IL_0047:  ret
  IL_0048:  ldarg.0
  IL_0049:  ldc.i4.m1
  IL_004a:  stfld      ""int C.<F>d__0.<>1__state""
  IL_004f:  ldarg.0
  IL_0050:  ldfld      ""int C.<F>d__0.<x>5__1""
  IL_0055:  call       ""void System.Console.WriteLine(int)""
  IL_005a:  nop
  IL_005b:  ldc.i4.0
  IL_005c:  ret
}");
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
                        Row(3, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                        Row(7, TableIndex.Field, EditAndContinueOperation.Default),
                        Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(13, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(14, TableIndex.CustomAttribute, EditAndContinueOperation.Default));

                    diff1.VerifyIL("C.<F>d__0.System.Collections.IEnumerator.MoveNext", @"
{
  // Code size       81 (0x51)
  .maxstack  2
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<F>d__0.<>1__state""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  brfalse.s  IL_0014
  IL_000a:  br.s       IL_000c
  IL_000c:  ldloc.0
  IL_000d:  ldc.i4.1
  IL_000e:  beq.s      IL_003c
  IL_0010:  br.s       IL_0012
  IL_0012:  ldc.i4.0
  IL_0013:  ret
  IL_0014:  ldarg.0
  IL_0015:  ldc.i4.m1
  IL_0016:  stfld      ""int C.<F>d__0.<>1__state""
  IL_001b:  nop
  IL_001c:  ldarg.0
  IL_001d:  ldc.i4     0x4d2
  IL_0022:  stfld      ""int C.<F>d__0.<y>5__2""
  IL_0027:  ldarg.0
  IL_0028:  ldarg.0
  IL_0029:  ldfld      ""int C.<F>d__0.<y>5__2""
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
  IL_0044:  ldfld      ""int C.<F>d__0.p""
  IL_0049:  call       ""void System.Console.WriteLine(int)""
  IL_004e:  nop
  IL_004f:  ldc.i4.0
  IL_0050:  ret
}");
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
                        Row(3, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                        Row(5, TableIndex.Field, EditAndContinueOperation.Default),
                        Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(13, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(14, TableIndex.CustomAttribute, EditAndContinueOperation.Default));

                    diff1.VerifyIL("C.<F>d__0.System.Collections.IEnumerator.MoveNext", @"
{
  // Code size       80 (0x50)
  .maxstack  2
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<F>d__0.<>1__state""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  brfalse.s  IL_0014
  IL_000a:  br.s       IL_000c
  IL_000c:  ldloc.0
  IL_000d:  ldc.i4.1
  IL_000e:  beq.s      IL_003b
  IL_0010:  br.s       IL_0012
  IL_0012:  ldc.i4.0
  IL_0013:  ret
  IL_0014:  ldarg.0
  IL_0015:  ldc.i4.m1
  IL_0016:  stfld      ""int C.<F>d__0.<>1__state""
  IL_001b:  nop
  IL_001c:  ldarg.0
  IL_001d:  ldc.r8     1
  IL_0026:  stfld      ""double C.<F>d__0.<x>5__2""
  IL_002b:  ldarg.0
  IL_002c:  ldc.i4.2
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
  IL_0043:  ldfld      ""double C.<F>d__0.<x>5__2""
  IL_0048:  call       ""void System.Console.WriteLine(double)""
  IL_004d:  nop
  IL_004e:  ldc.i4.0
  IL_004f:  ret
}");
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
                        Row(3, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                        Row(7, TableIndex.Field, EditAndContinueOperation.Default),
                        Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(13, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(14, TableIndex.CustomAttribute, EditAndContinueOperation.Default));

                    diff1.VerifyIL("C.<F>d__0.System.Collections.IEnumerator.MoveNext", @"
{
  // Code size      157 (0x9d)
  .maxstack  5
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<F>d__0.<>1__state""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  brfalse.s  IL_0014
  IL_000a:  br.s       IL_000c
  IL_000c:  ldloc.0
  IL_000d:  ldc.i4.1
  IL_000e:  beq.s      IL_0067
  IL_0010:  br.s       IL_0012
  IL_0012:  ldc.i4.0
  IL_0013:  ret
  IL_0014:  ldarg.0
  IL_0015:  ldc.i4.m1
  IL_0016:  stfld      ""int C.<F>d__0.<>1__state""
  IL_001b:  nop
  IL_001c:  nop
  IL_001d:  ldarg.0
  IL_001e:  ldc.i4.1
  IL_001f:  newarr     ""double""
  IL_0024:  dup
  IL_0025:  ldc.i4.0
  IL_0026:  ldc.r8     1
  IL_002f:  stelem.r8
  IL_0030:  stfld      ""double[] C.<F>d__0.<>s__4""
  IL_0035:  ldarg.0
  IL_0036:  ldc.i4.0
  IL_0037:  stfld      ""int C.<F>d__0.<>s__2""
  IL_003c:  br.s       IL_0084
  IL_003e:  ldarg.0
  IL_003f:  ldarg.0
  IL_0040:  ldfld      ""double[] C.<F>d__0.<>s__4""
  IL_0045:  ldarg.0
  IL_0046:  ldfld      ""int C.<F>d__0.<>s__2""
  IL_004b:  ldelem.r8
  IL_004c:  box        ""double""
  IL_0051:  stfld      ""object C.<F>d__0.<item>5__3""
  IL_0056:  nop
  IL_0057:  ldarg.0
  IL_0058:  ldc.i4.1
  IL_0059:  stfld      ""int C.<F>d__0.<>2__current""
  IL_005e:  ldarg.0
  IL_005f:  ldc.i4.1
  IL_0060:  stfld      ""int C.<F>d__0.<>1__state""
  IL_0065:  ldc.i4.1
  IL_0066:  ret
  IL_0067:  ldarg.0
  IL_0068:  ldc.i4.m1
  IL_0069:  stfld      ""int C.<F>d__0.<>1__state""
  IL_006e:  nop
  IL_006f:  ldarg.0
  IL_0070:  ldnull
  IL_0071:  stfld      ""object C.<F>d__0.<item>5__3""
  IL_0076:  ldarg.0
  IL_0077:  ldarg.0
  IL_0078:  ldfld      ""int C.<F>d__0.<>s__2""
  IL_007d:  ldc.i4.1
  IL_007e:  add
  IL_007f:  stfld      ""int C.<F>d__0.<>s__2""
  IL_0084:  ldarg.0
  IL_0085:  ldfld      ""int C.<F>d__0.<>s__2""
  IL_008a:  ldarg.0
  IL_008b:  ldfld      ""double[] C.<F>d__0.<>s__4""
  IL_0090:  ldlen
  IL_0091:  conv.i4
  IL_0092:  blt.s      IL_003e
  IL_0094:  ldarg.0
  IL_0095:  ldnull
  IL_0096:  stfld      ""double[] C.<F>d__0.<>s__4""
  IL_009b:  ldc.i4.0
  IL_009c:  ret
}");
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
                Row(6, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(9, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(16, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(17, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(18, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(19, TableIndex.CustomAttribute, EditAndContinueOperation.Default));

            diff1.VerifyIL("C.<F>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      176 (0xb0)
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
    IL_0008:  brfalse.s  IL_0058
    IL_000a:  br.s       IL_000c
    IL_000c:  nop
    IL_000d:  ldarg.0
    IL_000e:  newobj     ""C..ctor()""
    IL_0013:  stfld      ""C C.<F>d__0.<a1>5__3""
    IL_0018:  ldarg.0
    IL_0019:  ldc.i4.3
    IL_001a:  stfld      ""int C.<F>d__0.<a2>5__2""
    IL_001f:  ldc.i4.0
    IL_0020:  call       ""System.Threading.Tasks.Task System.Threading.Tasks.Task.Delay(int)""
    IL_0025:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter System.Threading.Tasks.Task.GetAwaiter()""
    IL_002a:  stloc.2
    IL_002b:  ldloca.s   V_2
    IL_002d:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter.IsCompleted.get""
    IL_0032:  brtrue.s   IL_0074
    IL_0034:  ldarg.0
    IL_0035:  ldc.i4.0
    IL_0036:  dup
    IL_0037:  stloc.0
    IL_0038:  stfld      ""int C.<F>d__0.<>1__state""
    IL_003d:  ldarg.0
    IL_003e:  ldloc.2
    IL_003f:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<F>d__0.<>u__1""
    IL_0044:  ldarg.0
    IL_0045:  stloc.3
    IL_0046:  ldarg.0
    IL_0047:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__0.<>t__builder""
    IL_004c:  ldloca.s   V_2
    IL_004e:  ldloca.s   V_3
    IL_0050:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter, C.<F>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter, ref C.<F>d__0)""
    IL_0055:  nop
    IL_0056:  leave.s    IL_00af
    IL_0058:  ldarg.0
    IL_0059:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<F>d__0.<>u__1""
    IL_005e:  stloc.2
    IL_005f:  ldarg.0
    IL_0060:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter C.<F>d__0.<>u__1""
    IL_0065:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_006b:  ldarg.0
    IL_006c:  ldc.i4.m1
    IL_006d:  dup
    IL_006e:  stloc.0
    IL_006f:  stfld      ""int C.<F>d__0.<>1__state""
    IL_0074:  ldloca.s   V_2
    IL_0076:  call       ""void System.Runtime.CompilerServices.TaskAwaiter.GetResult()""
    IL_007b:  nop
    IL_007c:  ldc.i4.1
    IL_007d:  stloc.1
    IL_007e:  leave.s    IL_009a
  }
  catch System.Exception
  {
    IL_0080:  stloc.s    V_4
    IL_0082:  ldarg.0
    IL_0083:  ldc.i4.s   -2
    IL_0085:  stfld      ""int C.<F>d__0.<>1__state""
    IL_008a:  ldarg.0
    IL_008b:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__0.<>t__builder""
    IL_0090:  ldloc.s    V_4
    IL_0092:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)""
    IL_0097:  nop
    IL_0098:  leave.s    IL_00af
  }
  IL_009a:  ldarg.0
  IL_009b:  ldc.i4.s   -2
  IL_009d:  stfld      ""int C.<F>d__0.<>1__state""
  IL_00a2:  ldarg.0
  IL_00a3:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__0.<>t__builder""
  IL_00a8:  ldloc.1
  IL_00a9:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetResult(int)""
  IL_00ae:  nop
  IL_00af:  ret
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
                Row(20, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(21, TableIndex.CustomAttribute, EditAndContinueOperation.Default));

            diff2.VerifyIL("C.<F>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      176 (0xb0)
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
    IL_0008:  brfalse.s  IL_0058
    IL_000a:  br.s       IL_000c
    IL_000c:  nop
    IL_000d:  ldarg.0
    IL_000e:  ldc.i4.1
    IL_000f:  stfld      ""bool C.<F>d__0.<a1>5__4""
    IL_0014:  ldarg.0
    IL_0015:  newobj     ""C..ctor()""
    IL_001a:  stfld      ""C C.<F>d__0.<a2>5__5""
    IL_001f:  ldc.i4.0
    IL_0020:  call       ""System.Threading.Tasks.Task System.Threading.Tasks.Task.Delay(int)""
    IL_0025:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter System.Threading.Tasks.Task.GetAwaiter()""
    IL_002a:  stloc.2
    IL_002b:  ldloca.s   V_2
    IL_002d:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter.IsCompleted.get""
    IL_0032:  brtrue.s   IL_0074
    IL_0034:  ldarg.0
    IL_0035:  ldc.i4.0
    IL_0036:  dup
    IL_0037:  stloc.0
    IL_0038:  stfld      ""int C.<F>d__0.<>1__state""
    IL_003d:  ldarg.0
    IL_003e:  ldloc.2
    IL_003f:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<F>d__0.<>u__1""
    IL_0044:  ldarg.0
    IL_0045:  stloc.3
    IL_0046:  ldarg.0
    IL_0047:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__0.<>t__builder""
    IL_004c:  ldloca.s   V_2
    IL_004e:  ldloca.s   V_3
    IL_0050:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter, C.<F>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter, ref C.<F>d__0)""
    IL_0055:  nop
    IL_0056:  leave.s    IL_00af
    IL_0058:  ldarg.0
    IL_0059:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter C.<F>d__0.<>u__1""
    IL_005e:  stloc.2
    IL_005f:  ldarg.0
    IL_0060:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter C.<F>d__0.<>u__1""
    IL_0065:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_006b:  ldarg.0
    IL_006c:  ldc.i4.m1
    IL_006d:  dup
    IL_006e:  stloc.0
    IL_006f:  stfld      ""int C.<F>d__0.<>1__state""
    IL_0074:  ldloca.s   V_2
    IL_0076:  call       ""void System.Runtime.CompilerServices.TaskAwaiter.GetResult()""
    IL_007b:  nop
    IL_007c:  ldc.i4.1
    IL_007d:  stloc.1
    IL_007e:  leave.s    IL_009a
  }
  catch System.Exception
  {
    IL_0080:  stloc.s    V_4
    IL_0082:  ldarg.0
    IL_0083:  ldc.i4.s   -2
    IL_0085:  stfld      ""int C.<F>d__0.<>1__state""
    IL_008a:  ldarg.0
    IL_008b:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__0.<>t__builder""
    IL_0090:  ldloc.s    V_4
    IL_0092:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)""
    IL_0097:  nop
    IL_0098:  leave.s    IL_00af
  }
  IL_009a:  ldarg.0
  IL_009b:  ldc.i4.s   -2
  IL_009d:  stfld      ""int C.<F>d__0.<>1__state""
  IL_00a2:  ldarg.0
  IL_00a3:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__0.<>t__builder""
  IL_00a8:  ldloc.1
  IL_00a9:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetResult(int)""
  IL_00ae:  nop
  IL_00af:  ret
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
                Row(22, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(23, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(24, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(25, TableIndex.CustomAttribute, EditAndContinueOperation.Default));
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
        yield return 1;
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

            var baselineIL0 = @"
{
  // Code size      143 (0x8f)
  .maxstack  3
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<F>d__0.<>1__state""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  brfalse.s  IL_0014
  IL_000a:  br.s       IL_000c
  IL_000c:  ldloc.0
  IL_000d:  ldc.i4.1
  IL_000e:  beq.s      IL_0038
  IL_0010:  br.s       IL_0012
  IL_0012:  ldc.i4.0
  IL_0013:  ret
  IL_0014:  ldarg.0
  IL_0015:  ldc.i4.m1
  IL_0016:  stfld      ""int C.<F>d__0.<>1__state""
  IL_001b:  nop
  IL_001c:  ldarg.0
  IL_001d:  ldc.i4.1
  IL_001e:  box        ""int""
  IL_0023:  stfld      ""dynamic C.<F>d__0.<x>5__1""
  IL_0028:  ldarg.0
  IL_0029:  ldc.i4.1
  IL_002a:  stfld      ""int C.<F>d__0.<>2__current""
  IL_002f:  ldarg.0
  IL_0030:  ldc.i4.1
  IL_0031:  stfld      ""int C.<F>d__0.<>1__state""
  IL_0036:  ldc.i4.1
  IL_0037:  ret
  IL_0038:  ldarg.0
  IL_0039:  ldc.i4.m1
  IL_003a:  stfld      ""int C.<F>d__0.<>1__state""
  IL_003f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>> C.<>o__0.<>p__0""
  IL_0044:  brfalse.s  IL_0048
  IL_0046:  br.s       IL_006d
  IL_0048:  ldc.i4.s   16
  IL_004a:  ldtoken    ""int""
  IL_004f:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0054:  ldtoken    ""C""
  IL_0059:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_005e:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.Convert(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Type)""
  IL_0063:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0068:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>> C.<>o__0.<>p__0""
  IL_006d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>> C.<>o__0.<>p__0""
  IL_0072:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>>.Target""
  IL_0077:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>> C.<>o__0.<>p__0""
  IL_007c:  ldarg.0
  IL_007d:  ldfld      ""dynamic C.<F>d__0.<x>5__1""
  IL_0082:  callvirt   ""int System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_0087:  call       ""void System.Console.WriteLine(int)""
  IL_008c:  nop
  IL_008d:  ldc.i4.0
  IL_008e:  ret
}";
            v0.VerifyIL("C.<F>d__0.System.Collections.IEnumerator.MoveNext()", baselineIL0);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            var baselineIL = @"
{
  // Code size      145 (0x91)
  .maxstack  3
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<F>d__0.<>1__state""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  brfalse.s  IL_0014
  IL_000a:  br.s       IL_000c
  IL_000c:  ldloc.0
  IL_000d:  ldc.i4.1
  IL_000e:  beq.s      IL_0038
  IL_0010:  br.s       IL_0012
  IL_0012:  ldc.i4.0
  IL_0013:  ret
  IL_0014:  ldarg.0
  IL_0015:  ldc.i4.m1
  IL_0016:  stfld      ""int C.<F>d__0.<>1__state""
  IL_001b:  nop
  IL_001c:  ldarg.0
  IL_001d:  ldc.i4.1
  IL_001e:  box        ""int""
  IL_0023:  stfld      ""dynamic C.<F>d__0.<x>5__1""
  IL_0028:  ldarg.0
  IL_0029:  ldc.i4.1
  IL_002a:  stfld      ""int C.<F>d__0.<>2__current""
  IL_002f:  ldarg.0
  IL_0030:  ldc.i4.1
  IL_0031:  stfld      ""int C.<F>d__0.<>1__state""
  IL_0036:  ldc.i4.1
  IL_0037:  ret
  IL_0038:  ldarg.0
  IL_0039:  ldc.i4.m1
  IL_003a:  stfld      ""int C.<F>d__0.<>1__state""
  IL_003f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>> C.<<DYNAMIC_CONTAINER_NAME>>.<>p__0""
  IL_0044:  brfalse.s  IL_0048
  IL_0046:  br.s       IL_006d
  IL_0048:  ldc.i4.s   16
  IL_004a:  ldtoken    ""int""
  IL_004f:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0054:  ldtoken    ""C""
  IL_0059:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_005e:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.Convert(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Type)""
  IL_0063:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0068:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>> C.<<DYNAMIC_CONTAINER_NAME>>.<>p__0""
  IL_006d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>> C.<<DYNAMIC_CONTAINER_NAME>>.<>p__0""
  IL_0072:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>>.Target""
  IL_0077:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>> C.<<DYNAMIC_CONTAINER_NAME>>.<>p__0""
  IL_007c:  ldarg.0
  IL_007d:  ldfld      ""dynamic C.<F>d__0.<x>5__1""
  IL_0082:  callvirt   ""int System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_0087:  ldc.i4.<<VALUE>>
  IL_0088:  add
  IL_0089:  call       ""void System.Console.WriteLine(int)""
  IL_008e:  nop
  IL_008f:  ldc.i4.0
  IL_0090:  ret
}";


            diff1.VerifySynthesizedMembers(
                "C: {<>o__0#1, <F>d__0}",
                "C.<>o__0#1: {<>p__0}",
                "C.<F>d__0: {<>1__state, <>2__current, <>l__initialThreadId, <>4__this, <x>5__1, System.IDisposable.Dispose, MoveNext, System.Collections.Generic.IEnumerator<System.Int32>.get_Current, System.Collections.IEnumerator.Reset, System.Collections.IEnumerator.get_Current, System.Collections.Generic.IEnumerable<System.Int32>.GetEnumerator, System.Collections.IEnumerable.GetEnumerator, System.Collections.Generic.IEnumerator<System.Int32>.Current, System.Collections.IEnumerator.Current}");

            diff1.VerifyIL("C.<F>d__0.System.Collections.IEnumerator.MoveNext()", baselineIL.Replace("<<VALUE>>", "1").Replace("<<DYNAMIC_CONTAINER_NAME>>", "<>o__0#1"));

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, f1, f2, GetSyntaxMapFromMarkers(source1, source2), preserveLocalVariables: true)));

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
            var source0 = MarkedSource(@"
using System;
using System.Collections.Generic;

class C
{
    private static IEnumerable<string> F()
    {
        dynamic <N:0>d = ""x""</N:0>;
        yield return d;
        Console.WriteLine(0);
    }
}
");
            var source1 = MarkedSource(@"
using System;
using System.Collections.Generic;

class C
{
    private static IEnumerable<string> F()
    {
        dynamic <N:0>d = ""x""</N:0>;
        yield return d.ToString();
        Console.WriteLine(1);
    }
}
");
            var source2 = MarkedSource(@"
using System;
using System.Collections.Generic;

class C
{
    private static IEnumerable<string> F()
    {
        dynamic <N:0>d = ""x""</N:0>;
        yield return d;
        Console.WriteLine(2);
    }
}
");
            var compilation0 = CreateCompilationWithMscorlib45(new[] { source0.Tree }, new[] { SystemCoreRef, CSharpRef }, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);
            var compilation2 = compilation0.WithSource(source2.Tree);

            var v0 = CompileAndVerify(compilation0);
            v0.VerifyDiagnostics();
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");
            var f2 = compilation2.GetMember<MethodSymbol>("C.F");

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                 generation0,
                 ImmutableArray.Create(
                     new SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            diff1.VerifySynthesizedMembers(
                "C: {<>o__0#1, <F>d__0}",
                "C.<>o__0#1: {<>p__0, <>p__1}",
                "C.<F>d__0: {<>1__state, <>2__current, <>l__initialThreadId, <d>5__1, System.IDisposable.Dispose, MoveNext, System.Collections.Generic.IEnumerator<System.String>.get_Current, System.Collections.IEnumerator.Reset, System.Collections.IEnumerator.get_Current, System.Collections.Generic.IEnumerable<System.String>.GetEnumerator, System.Collections.IEnumerable.GetEnumerator, System.Collections.Generic.IEnumerator<System.String>.Current, System.Collections.IEnumerator.Current}");

            var diff2 = compilation2.EmitDifference(
                 diff1.NextGeneration,
                 ImmutableArray.Create(
                     new SemanticEdit(SemanticEditKind.Update, f1, f2, GetSyntaxMapFromMarkers(source1, source2), preserveLocalVariables: true)));

            diff2.VerifySynthesizedMembers(
                "C: {<>o__0#2, <F>d__0, <>o__0#1}",
                "C.<>o__0#1: {<>p__0, <>p__1}",
                "C.<>o__0#2: {<>p__0}",
                "C.<F>d__0: {<>1__state, <>2__current, <>l__initialThreadId, <d>5__1, System.IDisposable.Dispose, MoveNext, System.Collections.Generic.IEnumerator<System.String>.get_Current, System.Collections.IEnumerator.Reset, System.Collections.IEnumerator.get_Current, System.Collections.Generic.IEnumerable<System.String>.GetEnumerator, System.Collections.IEnumerable.GetEnumerator, System.Collections.Generic.IEnumerator<System.String>.Current, System.Collections.IEnumerator.Current}");
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
                Row(16, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(17, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(18, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(19, TableIndex.CustomAttribute, EditAndContinueOperation.Default));

            // Note that the new awaiter is allocated slot <>u__3 since <>u__1 and <>u__2 are taken.
            diff1.VerifyIL("C.<F>d__3.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      264 (0x108)
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
    IL_0008:  brfalse.s  IL_0051
    IL_000a:  br.s       IL_000c
    IL_000c:  ldloc.0
    IL_000d:  ldc.i4.1
    IL_000e:  beq        IL_00af
    IL_0013:  br.s       IL_0015
    IL_0015:  nop
    IL_0016:  call       ""System.Threading.Tasks.Task<C> C.A3()""
    IL_001b:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<C> System.Threading.Tasks.Task<C>.GetAwaiter()""
    IL_0020:  stloc.2
    IL_0021:  ldloca.s   V_2
    IL_0023:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<C>.IsCompleted.get""
    IL_0028:  brtrue.s   IL_006d
    IL_002a:  ldarg.0
    IL_002b:  ldc.i4.0
    IL_002c:  dup
    IL_002d:  stloc.0
    IL_002e:  stfld      ""int C.<F>d__3.<>1__state""
    IL_0033:  ldarg.0
    IL_0034:  ldloc.2
    IL_0035:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<C> C.<F>d__3.<>u__3""
    IL_003a:  ldarg.0
    IL_003b:  stloc.3
    IL_003c:  ldarg.0
    IL_003d:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__3.<>t__builder""
    IL_0042:  ldloca.s   V_2
    IL_0044:  ldloca.s   V_3
    IL_0046:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<C>, C.<F>d__3>(ref System.Runtime.CompilerServices.TaskAwaiter<C>, ref C.<F>d__3)""
    IL_004b:  nop
    IL_004c:  leave      IL_0107
    IL_0051:  ldarg.0
    IL_0052:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<C> C.<F>d__3.<>u__3""
    IL_0057:  stloc.2
    IL_0058:  ldarg.0
    IL_0059:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<C> C.<F>d__3.<>u__3""
    IL_005e:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<C>""
    IL_0064:  ldarg.0
    IL_0065:  ldc.i4.m1
    IL_0066:  dup
    IL_0067:  stloc.0
    IL_0068:  stfld      ""int C.<F>d__3.<>1__state""
    IL_006d:  ldloca.s   V_2
    IL_006f:  call       ""C System.Runtime.CompilerServices.TaskAwaiter<C>.GetResult()""
    IL_0074:  pop
    IL_0075:  call       ""System.Threading.Tasks.Task<int> C.A2()""
    IL_007a:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_007f:  stloc.s    V_4
    IL_0081:  ldloca.s   V_4
    IL_0083:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_0088:  brtrue.s   IL_00cc
    IL_008a:  ldarg.0
    IL_008b:  ldc.i4.1
    IL_008c:  dup
    IL_008d:  stloc.0
    IL_008e:  stfld      ""int C.<F>d__3.<>1__state""
    IL_0093:  ldarg.0
    IL_0094:  ldloc.s    V_4
    IL_0096:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> C.<F>d__3.<>u__2""
    IL_009b:  ldarg.0
    IL_009c:  stloc.3
    IL_009d:  ldarg.0
    IL_009e:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__3.<>t__builder""
    IL_00a3:  ldloca.s   V_4
    IL_00a5:  ldloca.s   V_3
    IL_00a7:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, C.<F>d__3>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref C.<F>d__3)""
    IL_00ac:  nop
    IL_00ad:  leave.s    IL_0107
    IL_00af:  ldarg.0
    IL_00b0:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> C.<F>d__3.<>u__2""
    IL_00b5:  stloc.s    V_4
    IL_00b7:  ldarg.0
    IL_00b8:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> C.<F>d__3.<>u__2""
    IL_00bd:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_00c3:  ldarg.0
    IL_00c4:  ldc.i4.m1
    IL_00c5:  dup
    IL_00c6:  stloc.0
    IL_00c7:  stfld      ""int C.<F>d__3.<>1__state""
    IL_00cc:  ldloca.s   V_4
    IL_00ce:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_00d3:  pop
    IL_00d4:  ldc.i4.1
    IL_00d5:  stloc.1
    IL_00d6:  leave.s    IL_00f2
  }
  catch System.Exception
  {
    IL_00d8:  stloc.s    V_5
    IL_00da:  ldarg.0
    IL_00db:  ldc.i4.s   -2
    IL_00dd:  stfld      ""int C.<F>d__3.<>1__state""
    IL_00e2:  ldarg.0
    IL_00e3:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__3.<>t__builder""
    IL_00e8:  ldloc.s    V_5
    IL_00ea:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)""
    IL_00ef:  nop
    IL_00f0:  leave.s    IL_0107
  }
  IL_00f2:  ldarg.0
  IL_00f3:  ldc.i4.s   -2
  IL_00f5:  stfld      ""int C.<F>d__3.<>1__state""
  IL_00fa:  ldarg.0
  IL_00fb:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__3.<>t__builder""
  IL_0100:  ldloc.1
  IL_0101:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetResult(int)""
  IL_0106:  nop
  IL_0107:  ret
}");
            // 1 field def added & 2 methods updated
            CheckEncLogDefinitions(md2.Reader,
                Row(11, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(12, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                Row(12, TableIndex.Field, EditAndContinueOperation.Default),
                Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(9, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(20, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(21, TableIndex.CustomAttribute, EditAndContinueOperation.Default));

            // Note that the new awaiters are allocated slots <>u__4, <>u__5.
            diff2.VerifyIL("C.<F>d__3.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      264 (0x108)
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
    IL_0008:  brfalse.s  IL_0051
    IL_000a:  br.s       IL_000c
    IL_000c:  ldloc.0
    IL_000d:  ldc.i4.1
    IL_000e:  beq        IL_00af
    IL_0013:  br.s       IL_0015
    IL_0015:  nop
    IL_0016:  call       ""System.Threading.Tasks.Task<bool> C.A1()""
    IL_001b:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<bool> System.Threading.Tasks.Task<bool>.GetAwaiter()""
    IL_0020:  stloc.2
    IL_0021:  ldloca.s   V_2
    IL_0023:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<bool>.IsCompleted.get""
    IL_0028:  brtrue.s   IL_006d
    IL_002a:  ldarg.0
    IL_002b:  ldc.i4.0
    IL_002c:  dup
    IL_002d:  stloc.0
    IL_002e:  stfld      ""int C.<F>d__3.<>1__state""
    IL_0033:  ldarg.0
    IL_0034:  ldloc.2
    IL_0035:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<bool> C.<F>d__3.<>u__4""
    IL_003a:  ldarg.0
    IL_003b:  stloc.3
    IL_003c:  ldarg.0
    IL_003d:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__3.<>t__builder""
    IL_0042:  ldloca.s   V_2
    IL_0044:  ldloca.s   V_3
    IL_0046:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<bool>, C.<F>d__3>(ref System.Runtime.CompilerServices.TaskAwaiter<bool>, ref C.<F>d__3)""
    IL_004b:  nop
    IL_004c:  leave      IL_0107
    IL_0051:  ldarg.0
    IL_0052:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<bool> C.<F>d__3.<>u__4""
    IL_0057:  stloc.2
    IL_0058:  ldarg.0
    IL_0059:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<bool> C.<F>d__3.<>u__4""
    IL_005e:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<bool>""
    IL_0064:  ldarg.0
    IL_0065:  ldc.i4.m1
    IL_0066:  dup
    IL_0067:  stloc.0
    IL_0068:  stfld      ""int C.<F>d__3.<>1__state""
    IL_006d:  ldloca.s   V_2
    IL_006f:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<bool>.GetResult()""
    IL_0074:  pop
    IL_0075:  call       ""System.Threading.Tasks.Task<C> C.A3()""
    IL_007a:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<C> System.Threading.Tasks.Task<C>.GetAwaiter()""
    IL_007f:  stloc.s    V_4
    IL_0081:  ldloca.s   V_4
    IL_0083:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<C>.IsCompleted.get""
    IL_0088:  brtrue.s   IL_00cc
    IL_008a:  ldarg.0
    IL_008b:  ldc.i4.1
    IL_008c:  dup
    IL_008d:  stloc.0
    IL_008e:  stfld      ""int C.<F>d__3.<>1__state""
    IL_0093:  ldarg.0
    IL_0094:  ldloc.s    V_4
    IL_0096:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<C> C.<F>d__3.<>u__3""
    IL_009b:  ldarg.0
    IL_009c:  stloc.3
    IL_009d:  ldarg.0
    IL_009e:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__3.<>t__builder""
    IL_00a3:  ldloca.s   V_4
    IL_00a5:  ldloca.s   V_3
    IL_00a7:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<C>, C.<F>d__3>(ref System.Runtime.CompilerServices.TaskAwaiter<C>, ref C.<F>d__3)""
    IL_00ac:  nop
    IL_00ad:  leave.s    IL_0107
    IL_00af:  ldarg.0
    IL_00b0:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<C> C.<F>d__3.<>u__3""
    IL_00b5:  stloc.s    V_4
    IL_00b7:  ldarg.0
    IL_00b8:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<C> C.<F>d__3.<>u__3""
    IL_00bd:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<C>""
    IL_00c3:  ldarg.0
    IL_00c4:  ldc.i4.m1
    IL_00c5:  dup
    IL_00c6:  stloc.0
    IL_00c7:  stfld      ""int C.<F>d__3.<>1__state""
    IL_00cc:  ldloca.s   V_4
    IL_00ce:  call       ""C System.Runtime.CompilerServices.TaskAwaiter<C>.GetResult()""
    IL_00d3:  pop
    IL_00d4:  ldc.i4.1
    IL_00d5:  stloc.1
    IL_00d6:  leave.s    IL_00f2
  }
  catch System.Exception
  {
    IL_00d8:  stloc.s    V_5
    IL_00da:  ldarg.0
    IL_00db:  ldc.i4.s   -2
    IL_00dd:  stfld      ""int C.<F>d__3.<>1__state""
    IL_00e2:  ldarg.0
    IL_00e3:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__3.<>t__builder""
    IL_00e8:  ldloc.s    V_5
    IL_00ea:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)""
    IL_00ef:  nop
    IL_00f0:  leave.s    IL_0107
  }
  IL_00f2:  ldarg.0
  IL_00f3:  ldc.i4.s   -2
  IL_00f5:  stfld      ""int C.<F>d__3.<>1__state""
  IL_00fa:  ldarg.0
  IL_00fb:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<F>d__3.<>t__builder""
  IL_0100:  ldloc.1
  IL_0101:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetResult(int)""
  IL_0106:  nop
  IL_0107:  ret
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
                Row(22, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(23, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(24, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(25, TableIndex.CustomAttribute, EditAndContinueOperation.Default));
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

        [Fact]
        public void UpdateAsyncLambda()
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

            var compilation0 = CreateCompilationWithMscorlib45(new[] { source0.Tree }, options: ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
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

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");
            var f2 = compilation2.GetMember<MethodSymbol>("C.F");

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            // note that the types of the awaiter fields <>u__1, <>u__2 are the same as in the previous generation:
            diff1.VerifySynthesizedFields("C.<>c.<<F>b__0_0>d",
                "<>1__state: int",
                "<>t__builder: System.Runtime.CompilerServices.AsyncTaskMethodBuilder",
                "<>4__this: C.<>c",
                "<>u__1: System.Runtime.CompilerServices.TaskAwaiter<bool>",
                "<>u__2: System.Runtime.CompilerServices.TaskAwaiter<int>");

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, f1, f2, GetSyntaxMapFromMarkers(source1, source2), preserveLocalVariables: true)));

            // note that the types of the awaiter fields <>u__1, <>u__2 are the same as in the previous generation:
            diff2.VerifySynthesizedFields("C.<>c.<<F>b__0_0>d",
                "<>1__state: int",
                "<>t__builder: System.Runtime.CompilerServices.AsyncTaskMethodBuilder",
                "<>4__this: C.<>c",
                "<>u__1: System.Runtime.CompilerServices.TaskAwaiter<bool>",
                "<>u__2: System.Runtime.CompilerServices.TaskAwaiter<int>");
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
        yield return 1;
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
        yield return 1;
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
        yield return 1;
        Console.WriteLine(x.A + 3);
    }
}
");
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

            var baselineIL = @"
{
  // Code size       84 (0x54)
  .maxstack  2
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<F>d__0.<>1__state""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  brfalse.s  IL_0014
  IL_000a:  br.s       IL_000c
  IL_000c:  ldloc.0
  IL_000d:  ldc.i4.1
  IL_000e:  beq.s      IL_0038
  IL_0010:  br.s       IL_0012
  IL_0012:  ldc.i4.0
  IL_0013:  ret
  IL_0014:  ldarg.0
  IL_0015:  ldc.i4.m1
  IL_0016:  stfld      ""int C.<F>d__0.<>1__state""
  IL_001b:  nop
  IL_001c:  ldarg.0
  IL_001d:  ldc.i4.1
  IL_001e:  newobj     ""<>f__AnonymousType0<int>..ctor(int)""
  IL_0023:  stfld      ""<anonymous type: int A> C.<F>d__0.<x>5__1""
  IL_0028:  ldarg.0
  IL_0029:  ldc.i4.1
  IL_002a:  stfld      ""int C.<F>d__0.<>2__current""
  IL_002f:  ldarg.0
  IL_0030:  ldc.i4.1
  IL_0031:  stfld      ""int C.<F>d__0.<>1__state""
  IL_0036:  ldc.i4.1
  IL_0037:  ret
  IL_0038:  ldarg.0
  IL_0039:  ldc.i4.m1
  IL_003a:  stfld      ""int C.<F>d__0.<>1__state""
  IL_003f:  ldarg.0
  IL_0040:  ldfld      ""<anonymous type: int A> C.<F>d__0.<x>5__1""
  IL_0045:  callvirt   ""int <>f__AnonymousType0<int>.A.get""
  IL_004a:  ldc.i4.<<VALUE>>
  IL_004b:  add
  IL_004c:  call       ""void System.Console.WriteLine(int)""
  IL_0051:  nop
  IL_0052:  ldc.i4.0
  IL_0053:  ret
}";
            v0.VerifyIL("C.<F>d__0.System.Collections.IEnumerator.MoveNext()", baselineIL.Replace("<<VALUE>>", "1"));

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            diff1.VerifySynthesizedMembers(
                "C: {<F>d__0}",
                "C.<F>d__0: {<>1__state, <>2__current, <>l__initialThreadId, <>4__this, <x>5__1, System.IDisposable.Dispose, MoveNext, System.Collections.Generic.IEnumerator<System.Int32>.get_Current, System.Collections.IEnumerator.Reset, System.Collections.IEnumerator.get_Current, System.Collections.Generic.IEnumerable<System.Int32>.GetEnumerator, System.Collections.IEnumerable.GetEnumerator, System.Collections.Generic.IEnumerator<System.Int32>.Current, System.Collections.IEnumerator.Current}",
                "<>f__AnonymousType0<<A>j__TPar>: {Equals, GetHashCode, ToString}");

            diff1.VerifyIL("C.<F>d__0.System.Collections.IEnumerator.MoveNext()", baselineIL.Replace("<<VALUE>>", "2"));

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, f1, f2, GetSyntaxMapFromMarkers(source1, source2), preserveLocalVariables: true)));

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
        yield return 1;
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
        yield return 1;
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
        yield return 1;
        Console.WriteLine(x[0].A.B + 3);
    }
}
");
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

            var baselineIL = @"
{
  // Code size      105 (0x69)
  .maxstack  5
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<F>d__0.<>1__state""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  brfalse.s  IL_0014
  IL_000a:  br.s       IL_000c
  IL_000c:  ldloc.0
  IL_000d:  ldc.i4.1
  IL_000e:  beq.s      IL_0046
  IL_0010:  br.s       IL_0012
  IL_0012:  ldc.i4.0
  IL_0013:  ret
  IL_0014:  ldarg.0
  IL_0015:  ldc.i4.m1
  IL_0016:  stfld      ""int C.<F>d__0.<>1__state""
  IL_001b:  nop
  IL_001c:  ldarg.0
  IL_001d:  ldc.i4.1
  IL_001e:  newarr     ""<>f__AnonymousType0<<anonymous type: int B>>""
  IL_0023:  dup
  IL_0024:  ldc.i4.0
  IL_0025:  ldc.i4.1
  IL_0026:  newobj     ""<>f__AnonymousType1<int>..ctor(int)""
  IL_002b:  newobj     ""<>f__AnonymousType0<<anonymous type: int B>>..ctor(<anonymous type: int B>)""
  IL_0030:  stelem.ref
  IL_0031:  stfld      ""<anonymous type: <anonymous type: int B> A>[] C.<F>d__0.<x>5__1""
  IL_0036:  ldarg.0
  IL_0037:  ldc.i4.1
  IL_0038:  stfld      ""int C.<F>d__0.<>2__current""
  IL_003d:  ldarg.0
  IL_003e:  ldc.i4.1
  IL_003f:  stfld      ""int C.<F>d__0.<>1__state""
  IL_0044:  ldc.i4.1
  IL_0045:  ret
  IL_0046:  ldarg.0
  IL_0047:  ldc.i4.m1
  IL_0048:  stfld      ""int C.<F>d__0.<>1__state""
  IL_004d:  ldarg.0
  IL_004e:  ldfld      ""<anonymous type: <anonymous type: int B> A>[] C.<F>d__0.<x>5__1""
  IL_0053:  ldc.i4.0
  IL_0054:  ldelem.ref
  IL_0055:  callvirt   ""<anonymous type: int B> <>f__AnonymousType0<<anonymous type: int B>>.A.get""
  IL_005a:  callvirt   ""int <>f__AnonymousType1<int>.B.get""
  IL_005f:  ldc.i4.<<VALUE>>
  IL_0060:  add
  IL_0061:  call       ""void System.Console.WriteLine(int)""
  IL_0066:  nop
  IL_0067:  ldc.i4.0
  IL_0068:  ret
}";
            v0.VerifyIL("C.<F>d__0.System.Collections.IEnumerator.MoveNext()", baselineIL.Replace("<<VALUE>>", "1"));

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            diff1.VerifySynthesizedMembers(
                "C: {<F>d__0}",
                "C.<F>d__0: {<>1__state, <>2__current, <>l__initialThreadId, <>4__this, <x>5__1, System.IDisposable.Dispose, MoveNext, System.Collections.Generic.IEnumerator<System.Int32>.get_Current, System.Collections.IEnumerator.Reset, System.Collections.IEnumerator.get_Current, System.Collections.Generic.IEnumerable<System.Int32>.GetEnumerator, System.Collections.IEnumerable.GetEnumerator, System.Collections.Generic.IEnumerator<System.Int32>.Current, System.Collections.IEnumerator.Current}",
                "<>f__AnonymousType0<<A>j__TPar>: {Equals, GetHashCode, ToString}",
                "<>f__AnonymousType1<<B>j__TPar>: {Equals, GetHashCode, ToString}");

            diff1.VerifyIL("C.<F>d__0.System.Collections.IEnumerator.MoveNext()", baselineIL.Replace("<<VALUE>>", "2"));

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, f1, f2, GetSyntaxMapFromMarkers(source1, source2), preserveLocalVariables: true)));

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
        yield return 1;
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
        yield return 1;
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
        yield return 1;
        Console.WriteLine(x.a + x.b + 3);
    }
}
");
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

            var baselineIL = @"
{
  // Code size      104 (0x68)
  .maxstack  2
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<F>d__0.<>1__state""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  brfalse.s  IL_0014
  IL_000a:  br.s       IL_000c
  IL_000c:  ldloc.0
  IL_000d:  ldc.i4.1
  IL_000e:  beq.s      IL_0037
  IL_0010:  br.s       IL_0012
  IL_0012:  ldc.i4.0
  IL_0013:  ret
  IL_0014:  ldarg.0
  IL_0015:  ldc.i4.m1
  IL_0016:  stfld      ""int C.<F>d__0.<>1__state""
  IL_001b:  nop
  IL_001c:  ldarg.0
  IL_001d:  newobj     ""Z<double>.S<int>..ctor()""
  IL_0022:  stfld      ""Z<double>.S<int> C.<F>d__0.<x>5__1""
  IL_0027:  ldarg.0
  IL_0028:  ldc.i4.1
  IL_0029:  stfld      ""int C.<F>d__0.<>2__current""
  IL_002e:  ldarg.0
  IL_002f:  ldc.i4.1
  IL_0030:  stfld      ""int C.<F>d__0.<>1__state""
  IL_0035:  ldc.i4.1
  IL_0036:  ret
  IL_0037:  ldarg.0
  IL_0038:  ldc.i4.m1
  IL_0039:  stfld      ""int C.<F>d__0.<>1__state""
  IL_003e:  ldarg.0
  IL_003f:  ldfld      ""Z<double>.S<int> C.<F>d__0.<x>5__1""
  IL_0044:  ldfld      ""double Z<double>.S<int>.a""
  IL_0049:  ldarg.0
  IL_004a:  ldfld      ""Z<double>.S<int> C.<F>d__0.<x>5__1""
  IL_004f:  ldfld      ""int Z<double>.S<int>.b""
  IL_0054:  conv.r8
  IL_0055:  add
  IL_0056:  ldc.r8     <<VALUE>>
  IL_005f:  add
  IL_0060:  call       ""void System.Console.WriteLine(double)""
  IL_0065:  nop
  IL_0066:  ldc.i4.0
  IL_0067:  ret
}";
            v0.VerifyIL("C.<F>d__0.System.Collections.IEnumerator.MoveNext()", baselineIL.Replace("<<VALUE>>", "1"));

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            diff1.VerifySynthesizedMembers(
                "C: {<F>d__0}",
                "C.<F>d__0: {<>1__state, <>2__current, <>l__initialThreadId, <>4__this, <x>5__1, System.IDisposable.Dispose, MoveNext, System.Collections.Generic.IEnumerator<System.Int32>.get_Current, System.Collections.IEnumerator.Reset, System.Collections.IEnumerator.get_Current, System.Collections.Generic.IEnumerable<System.Int32>.GetEnumerator, System.Collections.IEnumerable.GetEnumerator, System.Collections.Generic.IEnumerator<System.Int32>.Current, System.Collections.IEnumerator.Current}");

            diff1.VerifyIL("C.<F>d__0.System.Collections.IEnumerator.MoveNext()", baselineIL.Replace("<<VALUE>>", "2"));

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, f1, f2, GetSyntaxMapFromMarkers(source1, source2), preserveLocalVariables: true)));

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
        yield return 1;
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
  // Code size       83 (0x53)
  .maxstack  3
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<F>d__0.<>1__state""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  brfalse.s  IL_0014
  IL_000a:  br.s       IL_000c
  IL_000c:  ldloc.0
  IL_000d:  ldc.i4.1
  IL_000e:  beq.s      IL_0039
  IL_0010:  br.s       IL_0012
  IL_0012:  ldc.i4.0
  IL_0013:  ret
  IL_0014:  ldarg.0
  IL_0015:  ldc.i4.m1
  IL_0016:  stfld      ""int C.<F>d__0.<>1__state""
  IL_001b:  nop
  IL_001c:  ldarg.0
  IL_001d:  ldnull
  IL_001e:  ldc.i4.1
  IL_001f:  newobj     ""<>f__AnonymousType0<dynamic, int>..ctor(dynamic, int)""
  IL_0024:  stfld      ""<anonymous type: dynamic A, int B> C.<F>d__0.<x>5__1""
  IL_0029:  ldarg.0
  IL_002a:  ldc.i4.1
  IL_002b:  stfld      ""int C.<F>d__0.<>2__current""
  IL_0030:  ldarg.0
  IL_0031:  ldc.i4.1
  IL_0032:  stfld      ""int C.<F>d__0.<>1__state""
  IL_0037:  ldc.i4.1
  IL_0038:  ret
  IL_0039:  ldarg.0
  IL_003a:  ldc.i4.m1
  IL_003b:  stfld      ""int C.<F>d__0.<>1__state""
  IL_0040:  ldarg.0
  IL_0041:  ldfld      ""<anonymous type: dynamic A, int B> C.<F>d__0.<x>5__1""
  IL_0046:  callvirt   ""int <>f__AnonymousType0<dynamic, int>.B.get""
  IL_004b:  call       ""void System.Console.WriteLine(int)""
  IL_0050:  nop
  IL_0051:  ldc.i4.0
  IL_0052:  ret
}";
            v0.VerifyIL("C.<F>d__0.System.Collections.IEnumerator.MoveNext()", baselineIL0);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            var baselineIL = @"
{
  // Code size       85 (0x55)
  .maxstack  3
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<F>d__0.<>1__state""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  brfalse.s  IL_0014
  IL_000a:  br.s       IL_000c
  IL_000c:  ldloc.0
  IL_000d:  ldc.i4.1
  IL_000e:  beq.s      IL_0039
  IL_0010:  br.s       IL_0012
  IL_0012:  ldc.i4.0
  IL_0013:  ret
  IL_0014:  ldarg.0
  IL_0015:  ldc.i4.m1
  IL_0016:  stfld      ""int C.<F>d__0.<>1__state""
  IL_001b:  nop
  IL_001c:  ldarg.0
  IL_001d:  ldnull
  IL_001e:  ldc.i4.1
  IL_001f:  newobj     ""<>f__AnonymousType0<dynamic, int>..ctor(dynamic, int)""
  IL_0024:  stfld      ""<anonymous type: dynamic A, int B> C.<F>d__0.<x>5__1""
  IL_0029:  ldarg.0
  IL_002a:  ldc.i4.1
  IL_002b:  stfld      ""int C.<F>d__0.<>2__current""
  IL_0030:  ldarg.0
  IL_0031:  ldc.i4.1
  IL_0032:  stfld      ""int C.<F>d__0.<>1__state""
  IL_0037:  ldc.i4.1
  IL_0038:  ret
  IL_0039:  ldarg.0
  IL_003a:  ldc.i4.m1
  IL_003b:  stfld      ""int C.<F>d__0.<>1__state""
  IL_0040:  ldarg.0
  IL_0041:  ldfld      ""<anonymous type: dynamic A, int B> C.<F>d__0.<x>5__1""
  IL_0046:  callvirt   ""int <>f__AnonymousType0<dynamic, int>.B.get""
  IL_004b:  ldc.i4.<<VALUE>>
  IL_004c:  add
  IL_004d:  call       ""void System.Console.WriteLine(int)""
  IL_0052:  nop
  IL_0053:  ldc.i4.0
  IL_0054:  ret
}";

            diff1.VerifySynthesizedMembers(
                "C: {<F>d__0}",
                "C.<F>d__0: {<>1__state, <>2__current, <>l__initialThreadId, <>4__this, <x>5__1, System.IDisposable.Dispose, MoveNext, System.Collections.Generic.IEnumerator<System.Int32>.get_Current, System.Collections.IEnumerator.Reset, System.Collections.IEnumerator.get_Current, System.Collections.Generic.IEnumerable<System.Int32>.GetEnumerator, System.Collections.IEnumerable.GetEnumerator, System.Collections.Generic.IEnumerator<System.Int32>.Current, System.Collections.IEnumerator.Current}",
                "<>f__AnonymousType0<<A>j__TPar, <B>j__TPar>: {Equals, GetHashCode, ToString}");

            diff1.VerifyIL("C.<F>d__0.System.Collections.IEnumerator.MoveNext()", baselineIL.Replace("<<VALUE>>", "1"));

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, f1, f2, GetSyntaxMapFromMarkers(source1, source2), preserveLocalVariables: true)));

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

            var compilation0 = CreateCompilationWithMscorlib45(new[] { source0.Tree }, new[] { SystemCoreRef }, options: ComSafeDebugDll);
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

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            // y is added 
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            diff1.VerifySynthesizedMembers(
                "C: {<>c, <F>d__0}",
                "C.<>c: {<>9__0_0, <F>b__0_0}",
                "C.<F>d__0: {<>1__state, <>t__builder, <x>5__1, <y>5__3, <>s__2, <>u__1, MoveNext, SetStateMachine}",
                "<>f__AnonymousType0<<A>j__TPar>: {Equals, GetHashCode, ToString}");

            // y is removed
            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, f1, f2, GetSyntaxMapFromMarkers(source1, source2), preserveLocalVariables: true)));

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
                   new SemanticEdit(SemanticEditKind.Update, f2, f3, GetSyntaxMapFromMarkers(source2, source3), preserveLocalVariables: true)));

            diff3.VerifySynthesizedMembers(
                "C: {<>c, <F>d__0}",
                "C.<>c: {<>9__0_0, <F>b__0_0}",
                "C.<F>d__0: {<>1__state, <>t__builder, <x>5__1, <y>5__4, <>s__2, <>u__1, MoveNext, SetStateMachine, <y>5__3}",
                "<>f__AnonymousType0<<A>j__TPar>: {Equals, GetHashCode, ToString}");

            // y is removed
            var diff4 = compilation4.EmitDifference(
                diff3.NextGeneration,
                ImmutableArray.Create(
                   new SemanticEdit(SemanticEditKind.Update, f3, f4, GetSyntaxMapFromMarkers(source3, source4), preserveLocalVariables: true)));

            diff4.VerifySynthesizedMembers(
                "C: {<>c, <F>d__0}",
                "C.<>c: {<>9__0_0, <F>b__0_0}",
                "C.<F>d__0: {<>1__state, <>t__builder, <x>5__1, <>s__2, <>u__1, MoveNext, SetStateMachine, <y>5__4, <y>5__3}",
                "<>f__AnonymousType0<<A>j__TPar>: {Equals, GetHashCode, ToString}");

            // y is added
            var diff5 = compilation5.EmitDifference(
                diff4.NextGeneration,
                ImmutableArray.Create(
                   new SemanticEdit(SemanticEditKind.Update, f4, f5, GetSyntaxMapFromMarkers(source4, source5), preserveLocalVariables: true)));

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
            var compilation0 = CreateCompilationWithMscorlib45(new[] { source0.Tree }, new[] { SystemCoreRef, CSharpRef }, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);
            var compilation2 = compilation1.WithSource(source2.Tree);

            var v0 = CompileAndVerify(compilation0);
            v0.VerifyDiagnostics();
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var f0 = compilation0.GetMember<MethodSymbol>("Program.Iterator");
            var f1 = compilation1.GetMember<MethodSymbol>("Program.Iterator");
            var f2 = compilation2.GetMember<MethodSymbol>("Program.Iterator");

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            v0.VerifyIL("Program.<Iterator>d__1.System.Collections.IEnumerator.MoveNext()", @"
{
  // Code size      794 (0x31a)
  .maxstack  5
  .locals init (int V_0,
                bool V_1,
                int V_2,
                bool V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Iterator>d__1.<>1__state""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  brfalse.s  IL_0017
  IL_000a:  br.s       IL_000c
  IL_000c:  ldloc.0
  IL_000d:  ldc.i4.1
  IL_000e:  beq        IL_0197
  IL_0013:  br.s       IL_0015
  IL_0015:  ldc.i4.0
  IL_0016:  ret
  IL_0017:  ldarg.0
  IL_0018:  ldc.i4.m1
  IL_0019:  stfld      ""int Program.<Iterator>d__1.<>1__state""
  IL_001e:  nop
  IL_001f:  ldarg.0
  IL_0020:  ldc.i4.4
  IL_0021:  newarr     ""string""
  IL_0026:  dup
  IL_0027:  ldc.i4.0
  IL_0028:  ldstr      ""a""
  IL_002d:  stelem.ref
  IL_002e:  dup
  IL_002f:  ldc.i4.1
  IL_0030:  ldstr      ""bB""
  IL_0035:  stelem.ref
  IL_0036:  dup
  IL_0037:  ldc.i4.2
  IL_0038:  ldstr      ""Cc""
  IL_003d:  stelem.ref
  IL_003e:  dup
  IL_003f:  ldc.i4.3
  IL_0040:  ldstr      ""DD""
  IL_0045:  stelem.ref
  IL_0046:  stfld      ""string[] Program.<Iterator>d__1.<args>5__1""
  IL_004b:  ldarg.0
  IL_004c:  ldnull
  IL_004d:  ldnull
  IL_004e:  newobj     ""<>f__AnonymousType0<dynamic, dynamic>..ctor(dynamic, dynamic)""
  IL_0053:  stfld      ""<anonymous type: dynamic Head, dynamic Tail> Program.<Iterator>d__1.<list>5__2""
  IL_0058:  ldarg.0
  IL_0059:  ldc.i4.0
  IL_005a:  stfld      ""int Program.<Iterator>d__1.<i>5__3""
  IL_005f:  br         IL_0301
  IL_0064:  nop
  IL_0065:  ldarg.0
  IL_0066:  ldarg.0
  IL_0067:  ldfld      ""string[] Program.<Iterator>d__1.<args>5__1""
  IL_006c:  ldsfld     ""System.Func<string, <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x>> Program.<>c.<>9__1_0""
  IL_0071:  dup
  IL_0072:  brtrue.s   IL_008b
  IL_0074:  pop
  IL_0075:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_007a:  ldftn      ""<anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> Program.<>c.<Iterator>b__1_0(string)""
  IL_0080:  newobj     ""System.Func<string, <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x>>..ctor(object, System.IntPtr)""
  IL_0085:  dup
  IL_0086:  stsfld     ""System.Func<string, <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x>> Program.<>c.<>9__1_0""
  IL_008b:  call       ""System.Collections.Generic.IEnumerable<<anonymous type: string a, System.Collections.Generic.IEnumerable<char> x>> System.Linq.Enumerable.Select<string, <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x>>(System.Collections.Generic.IEnumerable<string>, System.Func<string, <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x>>)""
  IL_0090:  ldsfld     ""System.Func<<anonymous type: string a, System.Collections.Generic.IEnumerable<char> x>, <anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>> Program.<>c.<>9__1_1""
  IL_0095:  dup
  IL_0096:  brtrue.s   IL_00af
  IL_0098:  pop
  IL_0099:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_009e:  ldftn      ""<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y> Program.<>c.<Iterator>b__1_1(<anonymous type: string a, System.Collections.Generic.IEnumerable<char> x>)""
  IL_00a4:  newobj     ""System.Func<<anonymous type: string a, System.Collections.Generic.IEnumerable<char> x>, <anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>>..ctor(object, System.IntPtr)""
  IL_00a9:  dup
  IL_00aa:  stsfld     ""System.Func<<anonymous type: string a, System.Collections.Generic.IEnumerable<char> x>, <anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>> Program.<>c.<>9__1_1""
  IL_00af:  call       ""System.Collections.Generic.IEnumerable<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>> System.Linq.Enumerable.Select<<anonymous type: string a, System.Collections.Generic.IEnumerable<char> x>, <anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>>(System.Collections.Generic.IEnumerable<<anonymous type: string a, System.Collections.Generic.IEnumerable<char> x>>, System.Func<<anonymous type: string a, System.Collections.Generic.IEnumerable<char> x>, <anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>>)""
  IL_00b4:  ldsfld     ""System.Func<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, bool> Program.<>c.<>9__1_2""
  IL_00b9:  dup
  IL_00ba:  brtrue.s   IL_00d3
  IL_00bc:  pop
  IL_00bd:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_00c2:  ldftn      ""bool Program.<>c.<Iterator>b__1_2(<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>)""
  IL_00c8:  newobj     ""System.Func<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, bool>..ctor(object, System.IntPtr)""
  IL_00cd:  dup
  IL_00ce:  stsfld     ""System.Func<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, bool> Program.<>c.<>9__1_2""
  IL_00d3:  call       ""System.Collections.Generic.IEnumerable<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>> System.Linq.Enumerable.Where<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>>(System.Collections.Generic.IEnumerable<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>>, System.Func<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, bool>)""
  IL_00d8:  ldsfld     ""System.Func<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, int> Program.<>c.<>9__1_3""
  IL_00dd:  dup
  IL_00de:  brtrue.s   IL_00f7
  IL_00e0:  pop
  IL_00e1:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_00e6:  ldftn      ""int Program.<>c.<Iterator>b__1_3(<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>)""
  IL_00ec:  newobj     ""System.Func<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, int>..ctor(object, System.IntPtr)""
  IL_00f1:  dup
  IL_00f2:  stsfld     ""System.Func<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, int> Program.<>c.<>9__1_3""
  IL_00f7:  call       ""System.Linq.IOrderedEnumerable<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>> System.Linq.Enumerable.OrderBy<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, int>(System.Collections.Generic.IEnumerable<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>>, System.Func<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, int>)""
  IL_00fc:  ldsfld     ""System.Func<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, string> Program.<>c.<>9__1_4""
  IL_0101:  dup
  IL_0102:  brtrue.s   IL_011b
  IL_0104:  pop
  IL_0105:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_010a:  ldftn      ""string Program.<>c.<Iterator>b__1_4(<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>)""
  IL_0110:  newobj     ""System.Func<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, string>..ctor(object, System.IntPtr)""
  IL_0115:  dup
  IL_0116:  stsfld     ""System.Func<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, string> Program.<>c.<>9__1_4""
  IL_011b:  call       ""System.Linq.IOrderedEnumerable<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>> System.Linq.Enumerable.ThenByDescending<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, string>(System.Linq.IOrderedEnumerable<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>>, System.Func<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, string>)""
  IL_0120:  ldsfld     ""System.Func<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, <anonymous type: string Value, int Length>> Program.<>c.<>9__1_5""
  IL_0125:  dup
  IL_0126:  brtrue.s   IL_013f
  IL_0128:  pop
  IL_0129:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_012e:  ldftn      ""<anonymous type: string Value, int Length> Program.<>c.<Iterator>b__1_5(<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>)""
  IL_0134:  newobj     ""System.Func<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, <anonymous type: string Value, int Length>>..ctor(object, System.IntPtr)""
  IL_0139:  dup
  IL_013a:  stsfld     ""System.Func<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, <anonymous type: string Value, int Length>> Program.<>c.<>9__1_5""
  IL_013f:  call       ""System.Collections.Generic.IEnumerable<<anonymous type: string Value, int Length>> System.Linq.Enumerable.Select<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, <anonymous type: string Value, int Length>>(System.Collections.Generic.IEnumerable<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>>, System.Func<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, <anonymous type: string Value, int Length>>)""
  IL_0144:  stfld      ""System.Collections.Generic.IEnumerable<<anonymous type: string Value, int Length>> Program.<Iterator>d__1.<result>5__4""
  IL_0149:  ldarg.0
  IL_014a:  ldarg.0
  IL_014b:  ldfld      ""System.Collections.Generic.IEnumerable<<anonymous type: string Value, int Length>> Program.<Iterator>d__1.<result>5__4""
  IL_0150:  ldnull
  IL_0151:  ldsfld     ""System.Func<<anonymous type: string Head, dynamic Tail>, <anonymous type: string Value, int Length>, <anonymous type: string Head, dynamic Tail>> Program.<>c.<>9__1_6""
  IL_0156:  dup
  IL_0157:  brtrue.s   IL_0170
  IL_0159:  pop
  IL_015a:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_015f:  ldftn      ""<anonymous type: string Head, dynamic Tail> Program.<>c.<Iterator>b__1_6(<anonymous type: string Head, dynamic Tail>, <anonymous type: string Value, int Length>)""
  IL_0165:  newobj     ""System.Func<<anonymous type: string Head, dynamic Tail>, <anonymous type: string Value, int Length>, <anonymous type: string Head, dynamic Tail>>..ctor(object, System.IntPtr)""
  IL_016a:  dup
  IL_016b:  stsfld     ""System.Func<<anonymous type: string Head, dynamic Tail>, <anonymous type: string Value, int Length>, <anonymous type: string Head, dynamic Tail>> Program.<>c.<>9__1_6""
  IL_0170:  call       ""<anonymous type: string Head, dynamic Tail> System.Linq.Enumerable.Aggregate<<anonymous type: string Value, int Length>, <anonymous type: string Head, dynamic Tail>>(System.Collections.Generic.IEnumerable<<anonymous type: string Value, int Length>>, <anonymous type: string Head, dynamic Tail>, System.Func<<anonymous type: string Head, dynamic Tail>, <anonymous type: string Value, int Length>, <anonymous type: string Head, dynamic Tail>>)""
  IL_0175:  stfld      ""<anonymous type: string Head, dynamic Tail> Program.<Iterator>d__1.<linked>5__5""
  IL_017a:  br.s       IL_01f1
  IL_017c:  nop
  IL_017d:  ldarg.0
  IL_017e:  ldarg.0
  IL_017f:  ldfld      ""<anonymous type: string Head, dynamic Tail> Program.<Iterator>d__1.<linked>5__5""
  IL_0184:  callvirt   ""string <>f__AnonymousType0<string, dynamic>.Head.get""
  IL_0189:  stfld      ""string Program.<Iterator>d__1.<>2__current""
  IL_018e:  ldarg.0
  IL_018f:  ldc.i4.1
  IL_0190:  stfld      ""int Program.<Iterator>d__1.<>1__state""
  IL_0195:  ldc.i4.1
  IL_0196:  ret
  IL_0197:  ldarg.0
  IL_0198:  ldc.i4.m1
  IL_0199:  stfld      ""int Program.<Iterator>d__1.<>1__state""
  IL_019e:  ldarg.0
  IL_019f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, <anonymous type: string Head, dynamic Tail>>> Program.<>o__1.<>p__0""
  IL_01a4:  brfalse.s  IL_01a8
  IL_01a6:  br.s       IL_01cc
  IL_01a8:  ldc.i4.0
  IL_01a9:  ldtoken    ""<>f__AnonymousType0<string, dynamic>""
  IL_01ae:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_01b3:  ldtoken    ""Program""
  IL_01b8:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_01bd:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.Convert(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Type)""
  IL_01c2:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, <anonymous type: string Head, dynamic Tail>>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, <anonymous type: string Head, dynamic Tail>>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_01c7:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, <anonymous type: string Head, dynamic Tail>>> Program.<>o__1.<>p__0""
  IL_01cc:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, <anonymous type: string Head, dynamic Tail>>> Program.<>o__1.<>p__0""
  IL_01d1:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, <anonymous type: string Head, dynamic Tail>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, <anonymous type: string Head, dynamic Tail>>>.Target""
  IL_01d6:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, <anonymous type: string Head, dynamic Tail>>> Program.<>o__1.<>p__0""
  IL_01db:  ldarg.0
  IL_01dc:  ldfld      ""<anonymous type: string Head, dynamic Tail> Program.<Iterator>d__1.<linked>5__5""
  IL_01e1:  callvirt   ""dynamic <>f__AnonymousType0<string, dynamic>.Tail.get""
  IL_01e6:  callvirt   ""<anonymous type: string Head, dynamic Tail> System.Func<System.Runtime.CompilerServices.CallSite, dynamic, <anonymous type: string Head, dynamic Tail>>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_01eb:  stfld      ""<anonymous type: string Head, dynamic Tail> Program.<Iterator>d__1.<linked>5__5""
  IL_01f0:  nop
  IL_01f1:  ldarg.0
  IL_01f2:  ldfld      ""<anonymous type: string Head, dynamic Tail> Program.<Iterator>d__1.<linked>5__5""
  IL_01f7:  ldnull
  IL_01f8:  cgt.un
  IL_01fa:  stloc.1
  IL_01fb:  ldloc.1
  IL_01fc:  brtrue     IL_017c
  IL_0201:  ldarg.0
  IL_0202:  ldarg.0
  IL_0203:  ldfld      ""System.Collections.Generic.IEnumerable<<anonymous type: string Value, int Length>> Program.<Iterator>d__1.<result>5__4""
  IL_0208:  ldsfld     ""System.Func<<anonymous type: string Value, int Length>, <anonymous type: <anonymous type: string Value, int Length> a, string value>> Program.<>c.<>9__1_7""
  IL_020d:  dup
  IL_020e:  brtrue.s   IL_0227
  IL_0210:  pop
  IL_0211:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_0216:  ldftn      ""<anonymous type: <anonymous type: string Value, int Length> a, string value> Program.<>c.<Iterator>b__1_7(<anonymous type: string Value, int Length>)""
  IL_021c:  newobj     ""System.Func<<anonymous type: string Value, int Length>, <anonymous type: <anonymous type: string Value, int Length> a, string value>>..ctor(object, System.IntPtr)""
  IL_0221:  dup
  IL_0222:  stsfld     ""System.Func<<anonymous type: string Value, int Length>, <anonymous type: <anonymous type: string Value, int Length> a, string value>> Program.<>c.<>9__1_7""
  IL_0227:  call       ""System.Collections.Generic.IEnumerable<<anonymous type: <anonymous type: string Value, int Length> a, string value>> System.Linq.Enumerable.Select<<anonymous type: string Value, int Length>, <anonymous type: <anonymous type: string Value, int Length> a, string value>>(System.Collections.Generic.IEnumerable<<anonymous type: string Value, int Length>>, System.Func<<anonymous type: string Value, int Length>, <anonymous type: <anonymous type: string Value, int Length> a, string value>>)""
  IL_022c:  ldsfld     ""System.Func<<anonymous type: <anonymous type: string Value, int Length> a, string value>, <anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>> Program.<>c.<>9__1_8""
  IL_0231:  dup
  IL_0232:  brtrue.s   IL_024b
  IL_0234:  pop
  IL_0235:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_023a:  ldftn      ""<anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length> Program.<>c.<Iterator>b__1_8(<anonymous type: <anonymous type: string Value, int Length> a, string value>)""
  IL_0240:  newobj     ""System.Func<<anonymous type: <anonymous type: string Value, int Length> a, string value>, <anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>>..ctor(object, System.IntPtr)""
  IL_0245:  dup
  IL_0246:  stsfld     ""System.Func<<anonymous type: <anonymous type: string Value, int Length> a, string value>, <anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>> Program.<>c.<>9__1_8""
  IL_024b:  call       ""System.Collections.Generic.IEnumerable<<anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>> System.Linq.Enumerable.Select<<anonymous type: <anonymous type: string Value, int Length> a, string value>, <anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>>(System.Collections.Generic.IEnumerable<<anonymous type: <anonymous type: string Value, int Length> a, string value>>, System.Func<<anonymous type: <anonymous type: string Value, int Length> a, string value>, <anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>>)""
  IL_0250:  ldsfld     ""System.Func<<anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>, bool> Program.<>c.<>9__1_9""
  IL_0255:  dup
  IL_0256:  brtrue.s   IL_026f
  IL_0258:  pop
  IL_0259:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_025e:  ldftn      ""bool Program.<>c.<Iterator>b__1_9(<anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>)""
  IL_0264:  newobj     ""System.Func<<anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>, bool>..ctor(object, System.IntPtr)""
  IL_0269:  dup
  IL_026a:  stsfld     ""System.Func<<anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>, bool> Program.<>c.<>9__1_9""
  IL_026f:  call       ""System.Collections.Generic.IEnumerable<<anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>> System.Linq.Enumerable.Where<<anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>>(System.Collections.Generic.IEnumerable<<anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>>, System.Func<<anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>, bool>)""
  IL_0274:  ldsfld     ""System.Func<<anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>, string> Program.<>c.<>9__1_10""
  IL_0279:  dup
  IL_027a:  brtrue.s   IL_0293
  IL_027c:  pop
  IL_027d:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_0282:  ldftn      ""string Program.<>c.<Iterator>b__1_10(<anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>)""
  IL_0288:  newobj     ""System.Func<<anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>, string>..ctor(object, System.IntPtr)""
  IL_028d:  dup
  IL_028e:  stsfld     ""System.Func<<anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>, string> Program.<>c.<>9__1_10""
  IL_0293:  call       ""System.Collections.Generic.IEnumerable<string> System.Linq.Enumerable.Select<<anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>, string>(System.Collections.Generic.IEnumerable<<anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>>, System.Func<<anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>, string>)""
  IL_0298:  stfld      ""System.Collections.Generic.IEnumerable<string> Program.<Iterator>d__1.<newArgs>5__6""
  IL_029d:  ldarg.0
  IL_029e:  ldarg.0
  IL_029f:  ldfld      ""string[] Program.<Iterator>d__1.<args>5__1""
  IL_02a4:  ldarg.0
  IL_02a5:  ldfld      ""System.Collections.Generic.IEnumerable<string> Program.<Iterator>d__1.<newArgs>5__6""
  IL_02aa:  call       ""System.Collections.Generic.IEnumerable<string> System.Linq.Enumerable.Concat<string>(System.Collections.Generic.IEnumerable<string>, System.Collections.Generic.IEnumerable<string>)""
  IL_02af:  call       ""string[] System.Linq.Enumerable.ToArray<string>(System.Collections.Generic.IEnumerable<string>)""
  IL_02b4:  stfld      ""string[] Program.<Iterator>d__1.<args>5__1""
  IL_02b9:  ldarg.0
  IL_02ba:  ldarg.0
  IL_02bb:  ldfld      ""int Program.<Iterator>d__1.<i>5__3""
  IL_02c0:  box        ""int""
  IL_02c5:  ldarg.0
  IL_02c6:  ldfld      ""<anonymous type: dynamic Head, dynamic Tail> Program.<Iterator>d__1.<list>5__2""
  IL_02cb:  newobj     ""<>f__AnonymousType0<dynamic, dynamic>..ctor(dynamic, dynamic)""
  IL_02d0:  stfld      ""<anonymous type: dynamic Head, dynamic Tail> Program.<Iterator>d__1.<list>5__2""
  IL_02d5:  call       ""void System.Diagnostics.Debugger.Break()""
  IL_02da:  nop
  IL_02db:  nop
  IL_02dc:  ldarg.0
  IL_02dd:  ldnull
  IL_02de:  stfld      ""System.Collections.Generic.IEnumerable<<anonymous type: string Value, int Length>> Program.<Iterator>d__1.<result>5__4""
  IL_02e3:  ldarg.0
  IL_02e4:  ldnull
  IL_02e5:  stfld      ""<anonymous type: string Head, dynamic Tail> Program.<Iterator>d__1.<linked>5__5""
  IL_02ea:  ldarg.0
  IL_02eb:  ldnull
  IL_02ec:  stfld      ""System.Collections.Generic.IEnumerable<string> Program.<Iterator>d__1.<newArgs>5__6""
  IL_02f1:  ldarg.0
  IL_02f2:  ldfld      ""int Program.<Iterator>d__1.<i>5__3""
  IL_02f7:  stloc.2
  IL_02f8:  ldarg.0
  IL_02f9:  ldloc.2
  IL_02fa:  ldc.i4.1
  IL_02fb:  add
  IL_02fc:  stfld      ""int Program.<Iterator>d__1.<i>5__3""
  IL_0301:  ldarg.0
  IL_0302:  ldfld      ""int Program.<Iterator>d__1.<i>5__3""
  IL_0307:  ldc.i4.s   10
  IL_0309:  clt
  IL_030b:  stloc.3
  IL_030c:  ldloc.3
  IL_030d:  brtrue     IL_0064
  IL_0312:  call       ""void System.Diagnostics.Debugger.Break()""
  IL_0317:  nop
  IL_0318:  ldc.i4.0
  IL_0319:  ret
}");

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            diff1.VerifySynthesizedMembers(
                "Program.<>o__1#1: {<>p__0, <>p__1}",
                "Program: {<>o__1#1, <>c, <Iterator>d__1}",
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
  // Code size      881 (0x371)
  .maxstack  5
  .locals init (int V_0,
                bool V_1,
                int V_2,
                bool V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Iterator>d__1.<>1__state""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  brfalse.s  IL_0017
  IL_000a:  br.s       IL_000c
  IL_000c:  ldloc.0
  IL_000d:  ldc.i4.1
  IL_000e:  beq        IL_01e7
  IL_0013:  br.s       IL_0015
  IL_0015:  ldc.i4.0
  IL_0016:  ret
  IL_0017:  ldarg.0
  IL_0018:  ldc.i4.m1
  IL_0019:  stfld      ""int Program.<Iterator>d__1.<>1__state""
  IL_001e:  nop
  IL_001f:  ldarg.0
  IL_0020:  ldc.i4.4
  IL_0021:  newarr     ""string""
  IL_0026:  dup
  IL_0027:  ldc.i4.0
  IL_0028:  ldstr      ""a""
  IL_002d:  stelem.ref
  IL_002e:  dup
  IL_002f:  ldc.i4.1
  IL_0030:  ldstr      ""bB""
  IL_0035:  stelem.ref
  IL_0036:  dup
  IL_0037:  ldc.i4.2
  IL_0038:  ldstr      ""Cc""
  IL_003d:  stelem.ref
  IL_003e:  dup
  IL_003f:  ldc.i4.3
  IL_0040:  ldstr      ""DD""
  IL_0045:  stelem.ref
  IL_0046:  stfld      ""string[] Program.<Iterator>d__1.<args>5__1""
  IL_004b:  ldarg.0
  IL_004c:  ldnull
  IL_004d:  ldnull
  IL_004e:  newobj     ""<>f__AnonymousType0<dynamic, dynamic>..ctor(dynamic, dynamic)""
  IL_0053:  stfld      ""<anonymous type: dynamic Head, dynamic Tail> Program.<Iterator>d__1.<list>5__2""
  IL_0058:  ldarg.0
  IL_0059:  ldc.i4.0
  IL_005a:  stfld      ""int Program.<Iterator>d__1.<i>5__3""
  IL_005f:  br         IL_0358
  IL_0064:  nop
  IL_0065:  ldarg.0
  IL_0066:  ldarg.0
  IL_0067:  ldfld      ""string[] Program.<Iterator>d__1.<args>5__1""
  IL_006c:  ldsfld     ""System.Func<string, <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x>> Program.<>c.<>9__1_0""
  IL_0071:  dup
  IL_0072:  brtrue.s   IL_008b
  IL_0074:  pop
  IL_0075:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_007a:  ldftn      ""<anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> Program.<>c.<Iterator>b__1_0(string)""
  IL_0080:  newobj     ""System.Func<string, <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x>>..ctor(object, System.IntPtr)""
  IL_0085:  dup
  IL_0086:  stsfld     ""System.Func<string, <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x>> Program.<>c.<>9__1_0""
  IL_008b:  call       ""System.Collections.Generic.IEnumerable<<anonymous type: string a, System.Collections.Generic.IEnumerable<char> x>> System.Linq.Enumerable.Select<string, <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x>>(System.Collections.Generic.IEnumerable<string>, System.Func<string, <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x>>)""
  IL_0090:  ldsfld     ""System.Func<<anonymous type: string a, System.Collections.Generic.IEnumerable<char> x>, <anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>> Program.<>c.<>9__1_1""
  IL_0095:  dup
  IL_0096:  brtrue.s   IL_00af
  IL_0098:  pop
  IL_0099:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_009e:  ldftn      ""<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y> Program.<>c.<Iterator>b__1_1(<anonymous type: string a, System.Collections.Generic.IEnumerable<char> x>)""
  IL_00a4:  newobj     ""System.Func<<anonymous type: string a, System.Collections.Generic.IEnumerable<char> x>, <anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>>..ctor(object, System.IntPtr)""
  IL_00a9:  dup
  IL_00aa:  stsfld     ""System.Func<<anonymous type: string a, System.Collections.Generic.IEnumerable<char> x>, <anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>> Program.<>c.<>9__1_1""
  IL_00af:  call       ""System.Collections.Generic.IEnumerable<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>> System.Linq.Enumerable.Select<<anonymous type: string a, System.Collections.Generic.IEnumerable<char> x>, <anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>>(System.Collections.Generic.IEnumerable<<anonymous type: string a, System.Collections.Generic.IEnumerable<char> x>>, System.Func<<anonymous type: string a, System.Collections.Generic.IEnumerable<char> x>, <anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>>)""
  IL_00b4:  ldsfld     ""System.Func<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, bool> Program.<>c.<>9__1_2""
  IL_00b9:  dup
  IL_00ba:  brtrue.s   IL_00d3
  IL_00bc:  pop
  IL_00bd:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_00c2:  ldftn      ""bool Program.<>c.<Iterator>b__1_2(<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>)""
  IL_00c8:  newobj     ""System.Func<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, bool>..ctor(object, System.IntPtr)""
  IL_00cd:  dup
  IL_00ce:  stsfld     ""System.Func<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, bool> Program.<>c.<>9__1_2""
  IL_00d3:  call       ""System.Collections.Generic.IEnumerable<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>> System.Linq.Enumerable.Where<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>>(System.Collections.Generic.IEnumerable<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>>, System.Func<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, bool>)""
  IL_00d8:  ldsfld     ""System.Func<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, int> Program.<>c.<>9__1_3""
  IL_00dd:  dup
  IL_00de:  brtrue.s   IL_00f7
  IL_00e0:  pop
  IL_00e1:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_00e6:  ldftn      ""int Program.<>c.<Iterator>b__1_3(<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>)""
  IL_00ec:  newobj     ""System.Func<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, int>..ctor(object, System.IntPtr)""
  IL_00f1:  dup
  IL_00f2:  stsfld     ""System.Func<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, int> Program.<>c.<>9__1_3""
  IL_00f7:  call       ""System.Linq.IOrderedEnumerable<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>> System.Linq.Enumerable.OrderBy<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, int>(System.Collections.Generic.IEnumerable<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>>, System.Func<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, int>)""
  IL_00fc:  ldsfld     ""System.Func<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, string> Program.<>c.<>9__1_4""
  IL_0101:  dup
  IL_0102:  brtrue.s   IL_011b
  IL_0104:  pop
  IL_0105:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_010a:  ldftn      ""string Program.<>c.<Iterator>b__1_4(<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>)""
  IL_0110:  newobj     ""System.Func<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, string>..ctor(object, System.IntPtr)""
  IL_0115:  dup
  IL_0116:  stsfld     ""System.Func<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, string> Program.<>c.<>9__1_4""
  IL_011b:  call       ""System.Linq.IOrderedEnumerable<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>> System.Linq.Enumerable.ThenByDescending<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, string>(System.Linq.IOrderedEnumerable<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>>, System.Func<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, string>)""
  IL_0120:  ldsfld     ""System.Func<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, <anonymous type: string Value, int Length>> Program.<>c.<>9__1_5""
  IL_0125:  dup
  IL_0126:  brtrue.s   IL_013f
  IL_0128:  pop
  IL_0129:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_012e:  ldftn      ""<anonymous type: string Value, int Length> Program.<>c.<Iterator>b__1_5(<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>)""
  IL_0134:  newobj     ""System.Func<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, <anonymous type: string Value, int Length>>..ctor(object, System.IntPtr)""
  IL_0139:  dup
  IL_013a:  stsfld     ""System.Func<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, <anonymous type: string Value, int Length>> Program.<>c.<>9__1_5""
  IL_013f:  call       ""System.Collections.Generic.IEnumerable<<anonymous type: string Value, int Length>> System.Linq.Enumerable.Select<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, <anonymous type: string Value, int Length>>(System.Collections.Generic.IEnumerable<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>>, System.Func<<anonymous type: <anonymous type: string a, System.Collections.Generic.IEnumerable<char> x> <>h__TransparentIdentifier0, System.Collections.Generic.IEnumerable<char> y>, <anonymous type: string Value, int Length>>)""
  IL_0144:  stfld      ""System.Collections.Generic.IEnumerable<<anonymous type: string Value, int Length>> Program.<Iterator>d__1.<result>5__4""
  IL_0149:  ldarg.0
  IL_014a:  ldarg.0
  IL_014b:  ldfld      ""System.Collections.Generic.IEnumerable<<anonymous type: string Value, int Length>> Program.<Iterator>d__1.<result>5__4""
  IL_0150:  ldnull
  IL_0151:  ldsfld     ""System.Func<<anonymous type: string Head, dynamic Tail>, <anonymous type: string Value, int Length>, <anonymous type: string Head, dynamic Tail>> Program.<>c.<>9__1_6""
  IL_0156:  dup
  IL_0157:  brtrue.s   IL_0170
  IL_0159:  pop
  IL_015a:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_015f:  ldftn      ""<anonymous type: string Head, dynamic Tail> Program.<>c.<Iterator>b__1_6(<anonymous type: string Head, dynamic Tail>, <anonymous type: string Value, int Length>)""
  IL_0165:  newobj     ""System.Func<<anonymous type: string Head, dynamic Tail>, <anonymous type: string Value, int Length>, <anonymous type: string Head, dynamic Tail>>..ctor(object, System.IntPtr)""
  IL_016a:  dup
  IL_016b:  stsfld     ""System.Func<<anonymous type: string Head, dynamic Tail>, <anonymous type: string Value, int Length>, <anonymous type: string Head, dynamic Tail>> Program.<>c.<>9__1_6""
  IL_0170:  call       ""<anonymous type: string Head, dynamic Tail> System.Linq.Enumerable.Aggregate<<anonymous type: string Value, int Length>, <anonymous type: string Head, dynamic Tail>>(System.Collections.Generic.IEnumerable<<anonymous type: string Value, int Length>>, <anonymous type: string Head, dynamic Tail>, System.Func<<anonymous type: string Head, dynamic Tail>, <anonymous type: string Value, int Length>, <anonymous type: string Head, dynamic Tail>>)""
  IL_0175:  stfld      ""<anonymous type: string Head, dynamic Tail> Program.<Iterator>d__1.<linked>5__5""
  IL_017a:  ldarg.0
  IL_017b:  ldarg.0
  IL_017c:  ldfld      ""<anonymous type: dynamic Head, dynamic Tail> Program.<Iterator>d__1.<list>5__2""
  IL_0181:  stfld      ""<anonymous type: dynamic Head, dynamic Tail> Program.<Iterator>d__1.<temp>5__7""
  IL_0186:  br         IL_0241
  IL_018b:  nop
  IL_018c:  ldarg.0
  IL_018d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, string>> Program.<>o__1#1.<>p__0""
  IL_0192:  brfalse.s  IL_0196
  IL_0194:  br.s       IL_01ba
  IL_0196:  ldc.i4.0
  IL_0197:  ldtoken    ""string""
  IL_019c:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_01a1:  ldtoken    ""Program""
  IL_01a6:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_01ab:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.Convert(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Type)""
  IL_01b0:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, string>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, string>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_01b5:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, string>> Program.<>o__1#1.<>p__0""
  IL_01ba:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, string>> Program.<>o__1#1.<>p__0""
  IL_01bf:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, string> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, string>>.Target""
  IL_01c4:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, string>> Program.<>o__1#1.<>p__0""
  IL_01c9:  ldarg.0
  IL_01ca:  ldfld      ""<anonymous type: dynamic Head, dynamic Tail> Program.<Iterator>d__1.<temp>5__7""
  IL_01cf:  callvirt   ""dynamic <>f__AnonymousType0<dynamic, dynamic>.Head.get""
  IL_01d4:  callvirt   ""string System.Func<System.Runtime.CompilerServices.CallSite, dynamic, string>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_01d9:  stfld      ""string Program.<Iterator>d__1.<>2__current""
  IL_01de:  ldarg.0
  IL_01df:  ldc.i4.1
  IL_01e0:  stfld      ""int Program.<Iterator>d__1.<>1__state""
  IL_01e5:  ldc.i4.1
  IL_01e6:  ret
  IL_01e7:  ldarg.0
  IL_01e8:  ldc.i4.m1
  IL_01e9:  stfld      ""int Program.<Iterator>d__1.<>1__state""
  IL_01ee:  ldarg.0
  IL_01ef:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, <anonymous type: dynamic Head, dynamic Tail>>> Program.<>o__1#1.<>p__1""
  IL_01f4:  brfalse.s  IL_01f8
  IL_01f6:  br.s       IL_021c
  IL_01f8:  ldc.i4.0
  IL_01f9:  ldtoken    ""<>f__AnonymousType0<dynamic, dynamic>""
  IL_01fe:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0203:  ldtoken    ""Program""
  IL_0208:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_020d:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.Convert(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Type)""
  IL_0212:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, <anonymous type: dynamic Head, dynamic Tail>>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, <anonymous type: dynamic Head, dynamic Tail>>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0217:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, <anonymous type: dynamic Head, dynamic Tail>>> Program.<>o__1#1.<>p__1""
  IL_021c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, <anonymous type: dynamic Head, dynamic Tail>>> Program.<>o__1#1.<>p__1""
  IL_0221:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, <anonymous type: dynamic Head, dynamic Tail>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, <anonymous type: dynamic Head, dynamic Tail>>>.Target""
  IL_0226:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, <anonymous type: dynamic Head, dynamic Tail>>> Program.<>o__1#1.<>p__1""
  IL_022b:  ldarg.0
  IL_022c:  ldfld      ""<anonymous type: dynamic Head, dynamic Tail> Program.<Iterator>d__1.<temp>5__7""
  IL_0231:  callvirt   ""dynamic <>f__AnonymousType0<dynamic, dynamic>.Tail.get""
  IL_0236:  callvirt   ""<anonymous type: dynamic Head, dynamic Tail> System.Func<System.Runtime.CompilerServices.CallSite, dynamic, <anonymous type: dynamic Head, dynamic Tail>>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_023b:  stfld      ""<anonymous type: dynamic Head, dynamic Tail> Program.<Iterator>d__1.<temp>5__7""
  IL_0240:  nop
  IL_0241:  ldarg.0
  IL_0242:  ldfld      ""<anonymous type: dynamic Head, dynamic Tail> Program.<Iterator>d__1.<temp>5__7""
  IL_0247:  ldnull
  IL_0248:  cgt.un
  IL_024a:  stloc.1
  IL_024b:  ldloc.1
  IL_024c:  brtrue     IL_018b
  IL_0251:  ldarg.0
  IL_0252:  ldarg.0
  IL_0253:  ldfld      ""System.Collections.Generic.IEnumerable<<anonymous type: string Value, int Length>> Program.<Iterator>d__1.<result>5__4""
  IL_0258:  ldsfld     ""System.Func<<anonymous type: string Value, int Length>, <anonymous type: <anonymous type: string Value, int Length> a, string value>> Program.<>c.<>9__1_7""
  IL_025d:  dup
  IL_025e:  brtrue.s   IL_0277
  IL_0260:  pop
  IL_0261:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_0266:  ldftn      ""<anonymous type: <anonymous type: string Value, int Length> a, string value> Program.<>c.<Iterator>b__1_7(<anonymous type: string Value, int Length>)""
  IL_026c:  newobj     ""System.Func<<anonymous type: string Value, int Length>, <anonymous type: <anonymous type: string Value, int Length> a, string value>>..ctor(object, System.IntPtr)""
  IL_0271:  dup
  IL_0272:  stsfld     ""System.Func<<anonymous type: string Value, int Length>, <anonymous type: <anonymous type: string Value, int Length> a, string value>> Program.<>c.<>9__1_7""
  IL_0277:  call       ""System.Collections.Generic.IEnumerable<<anonymous type: <anonymous type: string Value, int Length> a, string value>> System.Linq.Enumerable.Select<<anonymous type: string Value, int Length>, <anonymous type: <anonymous type: string Value, int Length> a, string value>>(System.Collections.Generic.IEnumerable<<anonymous type: string Value, int Length>>, System.Func<<anonymous type: string Value, int Length>, <anonymous type: <anonymous type: string Value, int Length> a, string value>>)""
  IL_027c:  ldsfld     ""System.Func<<anonymous type: <anonymous type: string Value, int Length> a, string value>, <anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>> Program.<>c.<>9__1_8""
  IL_0281:  dup
  IL_0282:  brtrue.s   IL_029b
  IL_0284:  pop
  IL_0285:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_028a:  ldftn      ""<anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length> Program.<>c.<Iterator>b__1_8(<anonymous type: <anonymous type: string Value, int Length> a, string value>)""
  IL_0290:  newobj     ""System.Func<<anonymous type: <anonymous type: string Value, int Length> a, string value>, <anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>>..ctor(object, System.IntPtr)""
  IL_0295:  dup
  IL_0296:  stsfld     ""System.Func<<anonymous type: <anonymous type: string Value, int Length> a, string value>, <anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>> Program.<>c.<>9__1_8""
  IL_029b:  call       ""System.Collections.Generic.IEnumerable<<anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>> System.Linq.Enumerable.Select<<anonymous type: <anonymous type: string Value, int Length> a, string value>, <anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>>(System.Collections.Generic.IEnumerable<<anonymous type: <anonymous type: string Value, int Length> a, string value>>, System.Func<<anonymous type: <anonymous type: string Value, int Length> a, string value>, <anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>>)""
  IL_02a0:  ldsfld     ""System.Func<<anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>, bool> Program.<>c.<>9__1_9""
  IL_02a5:  dup
  IL_02a6:  brtrue.s   IL_02bf
  IL_02a8:  pop
  IL_02a9:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_02ae:  ldftn      ""bool Program.<>c.<Iterator>b__1_9(<anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>)""
  IL_02b4:  newobj     ""System.Func<<anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>, bool>..ctor(object, System.IntPtr)""
  IL_02b9:  dup
  IL_02ba:  stsfld     ""System.Func<<anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>, bool> Program.<>c.<>9__1_9""
  IL_02bf:  call       ""System.Collections.Generic.IEnumerable<<anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>> System.Linq.Enumerable.Where<<anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>>(System.Collections.Generic.IEnumerable<<anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>>, System.Func<<anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>, bool>)""
  IL_02c4:  ldsfld     ""System.Func<<anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>, string> Program.<>c.<>9__1_10""
  IL_02c9:  dup
  IL_02ca:  brtrue.s   IL_02e3
  IL_02cc:  pop
  IL_02cd:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_02d2:  ldftn      ""string Program.<>c.<Iterator>b__1_10(<anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>)""
  IL_02d8:  newobj     ""System.Func<<anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>, string>..ctor(object, System.IntPtr)""
  IL_02dd:  dup
  IL_02de:  stsfld     ""System.Func<<anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>, string> Program.<>c.<>9__1_10""
  IL_02e3:  call       ""System.Collections.Generic.IEnumerable<string> System.Linq.Enumerable.Select<<anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>, string>(System.Collections.Generic.IEnumerable<<anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>>, System.Func<<anonymous type: <anonymous type: <anonymous type: string Value, int Length> a, string value> <>h__TransparentIdentifier0, int length>, string>)""
  IL_02e8:  stfld      ""System.Collections.Generic.IEnumerable<string> Program.<Iterator>d__1.<newArgs>5__6""
  IL_02ed:  ldarg.0
  IL_02ee:  ldarg.0
  IL_02ef:  ldfld      ""string[] Program.<Iterator>d__1.<args>5__1""
  IL_02f4:  ldarg.0
  IL_02f5:  ldfld      ""System.Collections.Generic.IEnumerable<string> Program.<Iterator>d__1.<newArgs>5__6""
  IL_02fa:  call       ""System.Collections.Generic.IEnumerable<string> System.Linq.Enumerable.Concat<string>(System.Collections.Generic.IEnumerable<string>, System.Collections.Generic.IEnumerable<string>)""
  IL_02ff:  call       ""string[] System.Linq.Enumerable.ToArray<string>(System.Collections.Generic.IEnumerable<string>)""
  IL_0304:  stfld      ""string[] Program.<Iterator>d__1.<args>5__1""
  IL_0309:  ldarg.0
  IL_030a:  ldarg.0
  IL_030b:  ldfld      ""int Program.<Iterator>d__1.<i>5__3""
  IL_0310:  box        ""int""
  IL_0315:  ldarg.0
  IL_0316:  ldfld      ""<anonymous type: dynamic Head, dynamic Tail> Program.<Iterator>d__1.<list>5__2""
  IL_031b:  newobj     ""<>f__AnonymousType0<dynamic, dynamic>..ctor(dynamic, dynamic)""
  IL_0320:  stfld      ""<anonymous type: dynamic Head, dynamic Tail> Program.<Iterator>d__1.<list>5__2""
  IL_0325:  call       ""void System.Diagnostics.Debugger.Break()""
  IL_032a:  nop
  IL_032b:  nop
  IL_032c:  ldarg.0
  IL_032d:  ldnull
  IL_032e:  stfld      ""System.Collections.Generic.IEnumerable<<anonymous type: string Value, int Length>> Program.<Iterator>d__1.<result>5__4""
  IL_0333:  ldarg.0
  IL_0334:  ldnull
  IL_0335:  stfld      ""<anonymous type: string Head, dynamic Tail> Program.<Iterator>d__1.<linked>5__5""
  IL_033a:  ldarg.0
  IL_033b:  ldnull
  IL_033c:  stfld      ""<anonymous type: dynamic Head, dynamic Tail> Program.<Iterator>d__1.<temp>5__7""
  IL_0341:  ldarg.0
  IL_0342:  ldnull
  IL_0343:  stfld      ""System.Collections.Generic.IEnumerable<string> Program.<Iterator>d__1.<newArgs>5__6""
  IL_0348:  ldarg.0
  IL_0349:  ldfld      ""int Program.<Iterator>d__1.<i>5__3""
  IL_034e:  stloc.2
  IL_034f:  ldarg.0
  IL_0350:  ldloc.2
  IL_0351:  ldc.i4.1
  IL_0352:  add
  IL_0353:  stfld      ""int Program.<Iterator>d__1.<i>5__3""
  IL_0358:  ldarg.0
  IL_0359:  ldfld      ""int Program.<Iterator>d__1.<i>5__3""
  IL_035e:  ldc.i4.s   10
  IL_0360:  clt
  IL_0362:  stloc.3
  IL_0363:  ldloc.3
  IL_0364:  brtrue     IL_0064
  IL_0369:  call       ""void System.Diagnostics.Debugger.Break()""
  IL_036e:  nop
  IL_036f:  ldc.i4.0
  IL_0370:  ret
}");

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, f1, f2, GetSyntaxMapFromMarkers(source1, source2), preserveLocalVariables: true)));

            diff2.VerifySynthesizedMembers(
                "Program.<>o__1#1: {<>p__0, <>p__1}",
                "Program.<>o__1#2: {<>p__0, <>p__1, <>p__2}",
                "Program: {<>o__1#2, <>c, <Iterator>d__1, <>o__1#1}",
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

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            diff1.EmitResult.Diagnostics.Verify(
                // (7,29): error CS7043: Cannot update 'C.F()'; attribute 'System.Runtime.CompilerServices.IteratorStateMachineAttribute' is missing.
                Diagnostic(ErrorCode.ERR_EncUpdateFailedMissingAttribute, "F").WithArguments("C.F()", "System.Runtime.CompilerServices.IteratorStateMachineAttribute").WithLocation(7, 29));
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

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            diff1.EmitResult.Diagnostics.Verify(
                // (12,29): error CS7043: Cannot update 'C.F()'; attribute 'System.Runtime.CompilerServices.IteratorStateMachineAttribute' is missing.
                //     public IEnumerable<int> F()
                Diagnostic(ErrorCode.ERR_EncUpdateFailedMissingAttribute, "F").WithArguments("C.F()", "System.Runtime.CompilerServices.IteratorStateMachineAttribute").WithLocation(12, 29));
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

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Insert, null, ism1),
                    new SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

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

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

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

            var v0 = CompileAndVerify(compilation0, verify: Verification.Fails);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            diff1.EmitResult.Diagnostics.Verify(
                // (6,28): error CS7043: Cannot update 'C.F()'; attribute 'System.Runtime.CompilerServices.AsyncStateMachineAttribute' is missing.
                Diagnostic(ErrorCode.ERR_EncUpdateFailedMissingAttribute, "F").WithArguments("C.F()", "System.Runtime.CompilerServices.AsyncStateMachineAttribute").WithLocation(6, 28));
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
        <N:1>await new Task<int>();</N:1>
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
        <N:1>await new Task<int>();</N:1>
        return a;
    }
}
");

            var compilation0 = CreateEmptyCompilation(new[] { source0.Tree }, new[] { TestReferences.NetFx.Minimal.mincorlib, TestReferences.NetFx.Minimal.minasync }, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);

            // older versions of mscorlib don't contain IteratorStateMachineAttribute
            Assert.Null(compilation0.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_AsyncStateMachineAttribute__ctor));

            var v0 = CompileAndVerify(compilation0, verify: Verification.Fails);
            v0.VerifyDiagnostics();
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");
            var asm1 = compilation1.GetMember<TypeSymbol>("System.Runtime.CompilerServices.AsyncStateMachineAttribute");

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Insert, null, asm1),
                    new SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            diff1.EmitResult.Diagnostics.Verify();
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
        <N:1>await new Task<int>();</N:1>
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
        <N:1>await new Task<int>();</N:1>
        return a;
    }
}
");

            var compilation0 = CreateEmptyCompilation(new[] { source0.Tree }, new[] { TestReferences.NetFx.Minimal.mincorlib, TestReferences.NetFx.Minimal.minasync }, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);

            Assert.NotNull(compilation0.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_AsyncStateMachineAttribute__ctor));

            var v0 = CompileAndVerify(compilation0, verify: Verification.Fails);
            v0.VerifyDiagnostics();
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            diff1.EmitResult.Diagnostics.Verify();
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
        <N:1>return Task.FromResult(a);</N:1>
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
        <N:1>return await Task.FromResult(a);</N:1>
    }
}
");

            var compilation0 = CreateEmptyCompilation(new[] { source0.Tree }, new[] { TestReferences.NetFx.v4_0_30319_17626.mscorlib }, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);

            Assert.NotNull(compilation0.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_AsyncStateMachineAttribute__ctor));

            var v0 = CompileAndVerify(compilation0, verify: Verification.Passes);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

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

            var v0 = CompileAndVerify(compilation0, verify: Verification.Fails);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            diff1.EmitResult.Diagnostics.Verify(
                // (6,28): error CS7043: Cannot update 'C.F()'; attribute 'System.Runtime.CompilerServices.AsyncStateMachineAttribute' is missing.
                Diagnostic(ErrorCode.ERR_EncUpdateFailedMissingAttribute, "F").WithArguments("C.F()", "System.Runtime.CompilerServices.AsyncStateMachineAttribute").WithLocation(6, 28));
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

            var compilation0 = CreateEmptyCompilation(new[] { source0.Tree }, new[] { TestReferences.NetFx.v2_0_50727.mscorlib }, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);

            Assert.Null(compilation0.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_IteratorStateMachineAttribute__ctor));

            var v0 = CompileAndVerify(compilation0, verify: Verification.Passes);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            diff1.EmitResult.Diagnostics.Verify(
                // (6,29): error CS7043: Cannot update 'C.F()'; attribute 'System.Runtime.CompilerServices.IteratorStateMachineAttribute' is missing.
                Diagnostic(ErrorCode.ERR_EncUpdateFailedMissingAttribute, "F").WithArguments("C.F()", "System.Runtime.CompilerServices.IteratorStateMachineAttribute").WithLocation(6, 29));
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

            var compilation0 = CreateEmptyCompilation(new[] { source0.Tree }, new[] { TestReferences.NetFx.v2_0_50727.mscorlib }, options: ComSafeDebugDll);
            var compilation1 = compilation0.WithSource(source1.Tree);

            Assert.NotNull(compilation0.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_IteratorStateMachineAttribute__ctor));

            var v0 = CompileAndVerify(compilation0, verify: Verification.Passes);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

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

            var compilation0 = CreateEmptyCompilation(new[] { source0.Tree }, new[] { TestReferences.NetFx.v4_0_30319_17626.mscorlib }, options: ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            var compilation1 = compilation0.WithSource(source1.Tree);

            Assert.NotNull(compilation0.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_AsyncStateMachineAttribute__ctor));

            var v0 = CompileAndVerify(compilation0, verify: Verification.Passes);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            diff1.EmitResult.Diagnostics.Verify();

            diff1.VerifySynthesizedMembers(
                "C: {<>c}",
                "C.<>c: {<>9__0_0, <F>b__0_0, <<F>b__0_0>d}",
                "C.<>c.<<F>b__0_0>d: {<>1__state, <>t__builder, <>4__this, <a>5__1, <>s__2, <>u__1, MoveNext, SetStateMachine}");
        }
    }
}
