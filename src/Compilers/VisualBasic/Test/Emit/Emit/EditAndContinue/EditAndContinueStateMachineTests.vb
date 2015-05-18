' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Reflection.Metadata.Ecma335
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class EditAndContinueStateMachineTests
        Inherits EditAndContinueTestBase

        <Fact>
        Public Sub AddIteratorMethod()
            Dim source0 =
<compilation>
    <file name="a.vb">
Imports System.Collections.Generic
Class C
    Iterator Function F() As IEnumerable(Of Integer)
        Yield 1
    End Function
End Class
    </file>
</compilation>
            Dim source1 =
<compilation>
    <file name="a.vb">
Imports System.Collections.Generic
Class C
    Iterator Function F() As IEnumerable(Of Integer)
        Yield 1
    End Function
    Iterator Function G() As IEnumerable(Of Integer)
        Yield 2
    End Function
End Class
    </file>
</compilation>

            Dim compilation0 = CompilationUtils.CreateCompilationWithReferences(source0, references:=LatestReferences, options:=TestOptions.DebugDll)
            Dim compilation1 = CompilationUtils.CreateCompilationWithReferences(source1, references:=LatestReferences, options:=TestOptions.DebugDll)

            Dim bytes0 = compilation0.EmitToArray()
            Dim generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), EmptyLocalsProvider)
            Dim diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Insert, Nothing, compilation1.GetMember(Of MethodSymbol)("C.G"))))

            Using md1 = diff1.GetMetadata()
                Dim reader1 = md1.Reader
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
                            Row(18, TableIndex.TypeRef, EditAndContinueOperation.Default),
                            Row(19, TableIndex.TypeRef, EditAndContinueOperation.Default),
                            Row(20, TableIndex.TypeRef, EditAndContinueOperation.Default),
                            Row(21, TableIndex.TypeRef, EditAndContinueOperation.Default),
                            Row(22, TableIndex.TypeRef, EditAndContinueOperation.Default),
                            Row(23, TableIndex.TypeRef, EditAndContinueOperation.Default),
                            Row(24, TableIndex.TypeRef, EditAndContinueOperation.Default),
                            Row(25, TableIndex.TypeRef, EditAndContinueOperation.Default),
                            Row(26, TableIndex.TypeRef, EditAndContinueOperation.Default),
                            Row(27, TableIndex.TypeRef, EditAndContinueOperation.Default),
                            Row(28, TableIndex.TypeRef, EditAndContinueOperation.Default),
                            Row(29, TableIndex.TypeRef, EditAndContinueOperation.Default),
                            Row(30, TableIndex.TypeRef, EditAndContinueOperation.Default),
                            Row(3, TableIndex.TypeSpec, EditAndContinueOperation.Default),
                            Row(4, TableIndex.TypeSpec, EditAndContinueOperation.Default),
                            Row(3, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                            Row(4, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                            Row(4, TableIndex.TypeDef, EditAndContinueOperation.Default),
                            Row(2, TableIndex.PropertyMap, EditAndContinueOperation.Default),
                            Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(5, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(6, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(7, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(8, TableIndex.Field, EditAndContinueOperation.Default),
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
                            Row(13, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(14, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(15, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(16, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(17, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(18, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(19, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(20, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(21, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
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
                            Row(10, TableIndex.InterfaceImpl, EditAndContinueOperation.Default))
                CheckEncMap(reader1,
                            Handle(18, TableIndex.TypeRef),
                            Handle(19, TableIndex.TypeRef),
                            Handle(20, TableIndex.TypeRef),
                            Handle(21, TableIndex.TypeRef),
                            Handle(22, TableIndex.TypeRef),
                            Handle(23, TableIndex.TypeRef),
                            Handle(24, TableIndex.TypeRef),
                            Handle(25, TableIndex.TypeRef),
                            Handle(26, TableIndex.TypeRef),
                            Handle(27, TableIndex.TypeRef),
                            Handle(28, TableIndex.TypeRef),
                            Handle(29, TableIndex.TypeRef),
                            Handle(30, TableIndex.TypeRef),
                            Handle(4, TableIndex.TypeDef),
                            Handle(5, TableIndex.Field),
                            Handle(6, TableIndex.Field),
                            Handle(7, TableIndex.Field),
                            Handle(8, TableIndex.Field),
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
                            Handle(2, TableIndex.NestedClass))

                diff1.VerifyIL("
{
  // Code size       17 (0x11)
  .maxstack  2
  IL_0000:  ldc.i4.s   -2
  IL_0002:  newobj     0x0600000C
  IL_0007:  stloc.0
  IL_0008:  ldloc.0
  IL_0009:  ldarg.0
  IL_000a:  stfld      0x04000008
  IL_000f:  ldloc.0
  IL_0010:  ret
}
{
  // Code size       25 (0x19)
  .maxstack  8
  IL_0000:  ldarg.0
  IL_0001:  call       0x0A00001B
  IL_0006:  ldarg.0
  IL_0007:  ldarg.1
  IL_0008:  stfld      0x04000005
  IL_000d:  ldarg.0
  IL_000e:  call       0x0A00001C
  IL_0013:  stfld      0x04000007
  IL_0018:  ret
}
{
  // Code size        1 (0x1)
  .maxstack  8
  IL_0000:  ret
}
{
  // Code size       63 (0x3f)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldfld      0x04000005
  IL_0006:  stloc.1
  IL_0007:  ldloc.1
  IL_0008:  brfalse.s  IL_0012
  IL_000a:  br.s       IL_000c
  IL_000c:  ldloc.1
  IL_000d:  ldc.i4.1
  IL_000e:  beq.s      IL_0014
  IL_0010:  br.s       IL_0016
  IL_0012:  br.s       IL_0018
  IL_0014:  br.s       IL_0034
  IL_0016:  ldc.i4.0
  IL_0017:  ret
  IL_0018:  ldarg.0
  IL_0019:  ldc.i4.m1
  IL_001a:  dup
  IL_001b:  stloc.1
  IL_001c:  stfld      0x04000005
  IL_0021:  nop
  IL_0022:  ldarg.0
  IL_0023:  ldc.i4.2
  IL_0024:  stfld      0x04000006
  IL_0029:  ldarg.0
  IL_002a:  ldc.i4.1
  IL_002b:  dup
  IL_002c:  stloc.1
  IL_002d:  stfld      0x04000005
  IL_0032:  ldc.i4.1
  IL_0033:  ret
  IL_0034:  ldarg.0
  IL_0035:  ldc.i4.m1
  IL_0036:  dup
  IL_0037:  stloc.1
  IL_0038:  stfld      0x04000005
  IL_003d:  ldc.i4.0
  IL_003e:  ret
}
{
  // Code size       55 (0x37)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      0x04000005
  IL_0006:  ldc.i4.s   -2
  IL_0008:  bne.un.s   IL_0022
  IL_000a:  ldarg.0
  IL_000b:  ldfld      0x04000007
  IL_0010:  call       0x0A00001C
  IL_0015:  bne.un.s   IL_0022
  IL_0017:  ldarg.0
  IL_0018:  ldc.i4.0
  IL_0019:  stfld      0x04000005
  IL_001e:  ldarg.0
  IL_001f:  stloc.0
  IL_0020:  br.s       IL_0035
  IL_0022:  ldc.i4.0
  IL_0023:  newobj     0x0600000C
  IL_0028:  stloc.0
  IL_0029:  ldloc.0
  IL_002a:  ldarg.0
  IL_002b:  ldfld      0x04000008
  IL_0030:  stfld      0x04000008
  IL_0035:  ldloc.0
  IL_0036:  ret
}
{
  // Code size        7 (0x7)
  .maxstack  8
  IL_0000:  ldarg.0
  IL_0001:  call       0x0600000F
  IL_0006:  ret
}
{
  // Code size        7 (0x7)
  .maxstack  8
  IL_0000:  ldarg.0
  IL_0001:  ldfld      0x04000006
  IL_0006:  ret
}
{
  // Code size        6 (0x6)
  .maxstack  8
  IL_0000:  newobj     0x0A00001D
  IL_0005:  throw
}
{
  // Code size       12 (0xc)
  .maxstack  8
  IL_0000:  ldarg.0
  IL_0001:  ldfld      0x04000006
  IL_0006:  box        0x0100001E
  IL_000b:  ret
}
")

                diff1.VerifyPdb({&H0600000EUI},
<symbols>
    <files>
        <file id="1" name="a.vb" language="3a12d0b8-c26c-11d0-b442-00a0244a1dd2" languageVendor="994b45c4-e6e9-11d2-903f-00c04fa302a1" documentType="5a869d0b-6611-11d3-bd2a-0000f80849bd" checkSumAlgorithmId="ff1816ec-aa5e-4d10-87f7-6f4963833460" checkSum="18, 50, 69, 51, C7, A5, E4, CF, 63, 8F, 2D, D6, 4D, C0, 2F, 1A, 2F, 4A, 8B, FA, "/>
    </files>
    <methods>
        <method token="0x600000e">
            <sequencePoints>
                <entry offset="0x0" hidden="true" document="1"/>
                <entry offset="0x21" startLine="6" startColumn="5" endLine="6" endColumn="53" document="1"/>
                <entry offset="0x22" startLine="7" startColumn="9" endLine="7" endColumn="16" document="1"/>
                <entry offset="0x3d" startLine="8" startColumn="5" endLine="8" endColumn="17" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x3f">
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <currentnamespace name=""/>
            </scope>
        </method>
    </methods>
</symbols>)
            End Using
        End Sub

        <Fact>
        Public Sub AddAsyncMethod()
            Dim source0 =
<compilation>
    <file name="a.vb">
Imports System.Threading.Tasks
Class C
End Class
    </file>
</compilation>
            Dim source1 =
<compilation>
    <file name="a.vb">
Imports System.Threading.Tasks
Class C
    Async Function F() As Task(Of Integer)
        Await Task.FromResult(1)
        Return 0
    End Function
End Class
    </file>
</compilation>

            Dim compilation0 = CompilationUtils.CreateCompilationWithReferences(source0, references:=LatestReferences, options:=TestOptions.DebugDll)
            Dim compilation1 = CompilationUtils.CreateCompilationWithReferences(source1, references:=LatestReferences, options:=TestOptions.DebugDll)

            Dim bytes0 = compilation0.EmitToArray()
            Dim generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), EmptyLocalsProvider)
            Dim diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Insert, Nothing, compilation1.GetMember(Of MethodSymbol)("C.F"))))

            Using md1 = diff1.GetMetadata()
                Dim reader1 = md1.Reader
                CheckEncLog(reader1,
                            Row(2, TableIndex.AssemblyRef, EditAndContinueOperation.Default),
                            Row(3, TableIndex.AssemblyRef, EditAndContinueOperation.Default),
                            Row(5, TableIndex.MemberRef, EditAndContinueOperation.Default),
                            Row(6, TableIndex.MemberRef, EditAndContinueOperation.Default),
                            Row(7, TableIndex.MemberRef, EditAndContinueOperation.Default),
                            Row(8, TableIndex.MemberRef, EditAndContinueOperation.Default),
                            Row(9, TableIndex.MemberRef, EditAndContinueOperation.Default),
                            Row(10, TableIndex.MemberRef, EditAndContinueOperation.Default),
                            Row(11, TableIndex.MemberRef, EditAndContinueOperation.Default),
                            Row(12, TableIndex.MemberRef, EditAndContinueOperation.Default),
                            Row(13, TableIndex.MemberRef, EditAndContinueOperation.Default),
                            Row(14, TableIndex.MemberRef, EditAndContinueOperation.Default),
                            Row(15, TableIndex.MemberRef, EditAndContinueOperation.Default),
                            Row(16, TableIndex.MemberRef, EditAndContinueOperation.Default),
                            Row(17, TableIndex.MemberRef, EditAndContinueOperation.Default),
                            Row(18, TableIndex.MemberRef, EditAndContinueOperation.Default),
                            Row(19, TableIndex.MemberRef, EditAndContinueOperation.Default),
                            Row(20, TableIndex.MemberRef, EditAndContinueOperation.Default),
                            Row(21, TableIndex.MemberRef, EditAndContinueOperation.Default),
                            Row(22, TableIndex.MemberRef, EditAndContinueOperation.Default),
                            Row(1, TableIndex.MethodSpec, EditAndContinueOperation.Default),
                            Row(2, TableIndex.MethodSpec, EditAndContinueOperation.Default),
                            Row(3, TableIndex.MethodSpec, EditAndContinueOperation.Default),
                            Row(6, TableIndex.TypeRef, EditAndContinueOperation.Default),
                            Row(7, TableIndex.TypeRef, EditAndContinueOperation.Default),
                            Row(8, TableIndex.TypeRef, EditAndContinueOperation.Default),
                            Row(9, TableIndex.TypeRef, EditAndContinueOperation.Default),
                            Row(10, TableIndex.TypeRef, EditAndContinueOperation.Default),
                            Row(11, TableIndex.TypeRef, EditAndContinueOperation.Default),
                            Row(12, TableIndex.TypeRef, EditAndContinueOperation.Default),
                            Row(13, TableIndex.TypeRef, EditAndContinueOperation.Default),
                            Row(14, TableIndex.TypeRef, EditAndContinueOperation.Default),
                            Row(15, TableIndex.TypeRef, EditAndContinueOperation.Default),
                            Row(16, TableIndex.TypeRef, EditAndContinueOperation.Default),
                            Row(17, TableIndex.TypeRef, EditAndContinueOperation.Default),
                            Row(1, TableIndex.TypeSpec, EditAndContinueOperation.Default),
                            Row(2, TableIndex.TypeSpec, EditAndContinueOperation.Default),
                            Row(3, TableIndex.TypeSpec, EditAndContinueOperation.Default),
                            Row(1, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                            Row(2, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                            Row(3, TableIndex.TypeDef, EditAndContinueOperation.Default),
                            Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(1, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(2, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(3, TableIndex.Field, EditAndContinueOperation.Default),
                            Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                            Row(4, TableIndex.Field, EditAndContinueOperation.Default),
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
                            Row(1, TableIndex.InterfaceImpl, EditAndContinueOperation.Default))
                CheckEncMap(reader1,
                            Handle(6, TableIndex.TypeRef),
                            Handle(7, TableIndex.TypeRef),
                            Handle(8, TableIndex.TypeRef),
                            Handle(9, TableIndex.TypeRef),
                            Handle(10, TableIndex.TypeRef),
                            Handle(11, TableIndex.TypeRef),
                            Handle(12, TableIndex.TypeRef),
                            Handle(13, TableIndex.TypeRef),
                            Handle(14, TableIndex.TypeRef),
                            Handle(15, TableIndex.TypeRef),
                            Handle(16, TableIndex.TypeRef),
                            Handle(17, TableIndex.TypeRef),
                            Handle(3, TableIndex.TypeDef),
                            Handle(1, TableIndex.Field),
                            Handle(2, TableIndex.Field),
                            Handle(3, TableIndex.Field),
                            Handle(4, TableIndex.Field),
                            Handle(2, TableIndex.MethodDef),
                            Handle(3, TableIndex.MethodDef),
                            Handle(4, TableIndex.MethodDef),
                            Handle(5, TableIndex.MethodDef),
                            Handle(1, TableIndex.Param),
                            Handle(1, TableIndex.InterfaceImpl),
                            Handle(5, TableIndex.MemberRef),
                            Handle(6, TableIndex.MemberRef),
                            Handle(7, TableIndex.MemberRef),
                            Handle(8, TableIndex.MemberRef),
                            Handle(9, TableIndex.MemberRef),
                            Handle(10, TableIndex.MemberRef),
                            Handle(11, TableIndex.MemberRef),
                            Handle(12, TableIndex.MemberRef),
                            Handle(13, TableIndex.MemberRef),
                            Handle(14, TableIndex.MemberRef),
                            Handle(15, TableIndex.MemberRef),
                            Handle(16, TableIndex.MemberRef),
                            Handle(17, TableIndex.MemberRef),
                            Handle(18, TableIndex.MemberRef),
                            Handle(19, TableIndex.MemberRef),
                            Handle(20, TableIndex.MemberRef),
                            Handle(21, TableIndex.MemberRef),
                            Handle(22, TableIndex.MemberRef),
                            Handle(4, TableIndex.CustomAttribute),
                            Handle(5, TableIndex.CustomAttribute),
                            Handle(6, TableIndex.CustomAttribute),
                            Handle(7, TableIndex.CustomAttribute),
                            Handle(1, TableIndex.StandAloneSig),
                            Handle(2, TableIndex.StandAloneSig),
                            Handle(1, TableIndex.MethodImpl),
                            Handle(2, TableIndex.MethodImpl),
                            Handle(1, TableIndex.TypeSpec),
                            Handle(2, TableIndex.TypeSpec),
                            Handle(3, TableIndex.TypeSpec),
                            Handle(2, TableIndex.AssemblyRef),
                            Handle(3, TableIndex.AssemblyRef),
                            Handle(1, TableIndex.NestedClass),
                            Handle(1, TableIndex.MethodSpec),
                            Handle(2, TableIndex.MethodSpec),
                            Handle(3, TableIndex.MethodSpec))

                diff1.VerifyIL("
{
  // Code size       56 (0x38)
  .maxstack  2
  IL_0000:  newobj     0x06000003
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldarg.0
  IL_0008:  stfld      0x04000003
  IL_000d:  ldloc.0
  IL_000e:  ldc.i4.m1
  IL_000f:  stfld      0x04000001
  IL_0014:  ldloc.0
  IL_0015:  call       0x0A00000A
  IL_001a:  stfld      0x04000002
  IL_001f:  ldloc.0
  IL_0020:  ldflda     0x04000002
  IL_0025:  ldloca.s   V_0
  IL_0027:  call       0x2B000001
  IL_002c:  ldloc.0
  IL_002d:  ldflda     0x04000002
  IL_0032:  call       0x0A00000C
  IL_0037:  ret
}
{
  // Code size        7 (0x7)
  .maxstack  8
  IL_0000:  ldarg.0
  IL_0001:  call       0x0A00000D
  IL_0006:  ret
}
{
  // Code size      185 (0xb9)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldfld      0x04000001
  IL_0006:  stloc.1
  IL_0007:  ldloc.1
  IL_0008:  brfalse.s  IL_000c
  IL_000a:  br.s       IL_000e
  IL_000c:  br.s       IL_004a
  IL_000e:  nop
  IL_000f:  nop
  IL_0010:  ldc.i4.1
  IL_0011:  call       0x2B000002
  IL_0016:  callvirt   0x0A00000F
  IL_001b:  stloc.3
  IL_001c:  ldloca.s   V_3
  IL_001e:  call       0x0A000010
  IL_0023:  brtrue.s   IL_0068
  IL_0025:  ldarg.0
  IL_0026:  ldc.i4.0
  IL_0027:  dup
  IL_0028:  stloc.1
  IL_0029:  stfld      0x04000001
  IL_002e:  ldarg.0
  IL_002f:  ldloc.3
  IL_0030:  stfld      0x04000004
  IL_0035:  ldarg.0
  IL_0036:  ldflda     0x04000002
  IL_003b:  ldloca.s   V_3
  IL_003d:  ldarg.0
  IL_003e:  stloc.s    V_4
  IL_0040:  ldloca.s   V_4
  IL_0042:  call       0x2B000003
  IL_0047:  nop
  IL_0048:  leave.s    IL_00b8
  IL_004a:  ldarg.0
  IL_004b:  ldc.i4.m1
  IL_004c:  dup
  IL_004d:  stloc.1
  IL_004e:  stfld      0x04000001
  IL_0053:  ldarg.0
  IL_0054:  ldfld      0x04000004
  IL_0059:  stloc.3
  IL_005a:  ldarg.0
  IL_005b:  ldflda     0x04000004
  IL_0060:  initobj    0x1B000003
  IL_0066:  br.s       IL_0068
  IL_0068:  ldloca.s   V_3
  IL_006a:  call       0x0A000012
  IL_006f:  pop
  IL_0070:  ldloca.s   V_3
  IL_0072:  initobj    0x1B000003
  IL_0078:  ldc.i4.0
  IL_0079:  stloc.0
  IL_007a:  leave.s    IL_00a1
  IL_007c:  dup
  IL_007d:  call       0x0A000013
  IL_0082:  stloc.s    V_5
  IL_0084:  ldarg.0
  IL_0085:  ldc.i4.s   -2
  IL_0087:  stfld      0x04000001
  IL_008c:  ldarg.0
  IL_008d:  ldflda     0x04000002
  IL_0092:  ldloc.s    V_5
  IL_0094:  call       0x0A000014
  IL_0099:  nop
  IL_009a:  call       0x0A000015
  IL_009f:  leave.s    IL_00b8
  IL_00a1:  ldarg.0
  IL_00a2:  ldc.i4.s   -2
  IL_00a4:  dup
  IL_00a5:  stloc.1
  IL_00a6:  stfld      0x04000001
  IL_00ab:  ldarg.0
  IL_00ac:  ldflda     0x04000002
  IL_00b1:  ldloc.0
  IL_00b2:  call       0x0A000016
  IL_00b7:  nop
  IL_00b8:  ret
}
{
  // Code size        1 (0x1)
  .maxstack  8
  IL_0000:  ret
}
                ")

                diff1.VerifyPdb({&H06000004UI},
<symbols>
    <files>
        <file id="1" name="a.vb" language="3a12d0b8-c26c-11d0-b442-00a0244a1dd2" languageVendor="994b45c4-e6e9-11d2-903f-00c04fa302a1" documentType="5a869d0b-6611-11d3-bd2a-0000f80849bd" checkSumAlgorithmId="ff1816ec-aa5e-4d10-87f7-6f4963833460" checkSum="E8, 25, E4, A7, D1, 61, DE, 6D, 8C, 99, C8, 28, 60, 8E, A4, 2C, 37, CC, 4A, 38, "/>
    </files>
    <methods>
        <method token="0x6000004">
            <sequencePoints>
                <entry offset="0x0" hidden="true" document="1"/>
                <entry offset="0x7" hidden="true" document="1"/>
                <entry offset="0xe" startLine="3" startColumn="5" endLine="3" endColumn="43" document="1"/>
                <entry offset="0xf" startLine="4" startColumn="9" endLine="4" endColumn="33" document="1"/>
                <entry offset="0x1c" hidden="true" document="1"/>
                <entry offset="0x78" startLine="5" startColumn="9" endLine="5" endColumn="17" document="1"/>
                <entry offset="0x7c" hidden="true" document="1"/>
                <entry offset="0x84" hidden="true" document="1"/>
                <entry offset="0xa1" startLine="6" startColumn="5" endLine="6" endColumn="17" document="1"/>
                <entry offset="0xab" hidden="true" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xb9">
                <namespace name="System.Threading.Tasks" importlevel="file"/>
                <currentnamespace name=""/>
            </scope>
            <asyncInfo>
                <kickoffMethod token="0x6000002"/>
                <await yield="0x2e" resume="0x4a" token="0x6000004"/>
            </asyncInfo>
        </method>
    </methods>
</symbols>)
            End Using
        End Sub

        <Fact>
        Public Sub MethodToIteratorMethod()
            Dim source0 =
<compilation>
    <file name="a.vb">
Imports System.Collections.Generic
Class C
    Function F() As IEnumerable(Of Integer)
        Return {1, 1}
    End Function
End Class
    </file>
</compilation>
            Dim source1 =
<compilation>
    <file name="a.vb">
Imports System.Collections.Generic
Class C
    Iterator Function F() As IEnumerable(Of Integer)
        Yield 1
        Yield 1
    End Function
End Class
    </file>
</compilation>

            Dim compilation0 = CompilationUtils.CreateCompilationWithReferences(source0, references:=LatestReferences, options:=TestOptions.DebugDll)
            Dim compilation1 = CompilationUtils.CreateCompilationWithReferences(source1, references:=LatestReferences, options:=TestOptions.DebugDll)

            Dim v0 = CompileAndVerify(compilation:=compilation0)

            Using md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)
                Dim method0 = compilation0.GetMember(Of MethodSymbol)("C.F")
                Dim method1 = compilation1.GetMember(Of MethodSymbol)("C.F")

                Dim generation0 = EmitBaseline.CreateInitialBaseline(md0, EmptyLocalsProvider)
                Dim diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, method0, method1)))

                Using md1 = diff1.GetMetadata()
                    CheckEncLogDefinitions(md1.Reader,
                                        Row(2, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                                        Row(3, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                                        Row(3, TableIndex.TypeDef, EditAndContinueOperation.Default),
                                        Row(1, TableIndex.PropertyMap, EditAndContinueOperation.Default),
                                        Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                                        Row(1, TableIndex.Field, EditAndContinueOperation.Default),
                                        Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                                        Row(2, TableIndex.Field, EditAndContinueOperation.Default),
                                        Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                                        Row(3, TableIndex.Field, EditAndContinueOperation.Default),
                                        Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                                        Row(4, TableIndex.Field, EditAndContinueOperation.Default),
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
                                        Row(5, TableIndex.InterfaceImpl, EditAndContinueOperation.Default))
                End Using
            End Using
        End Sub

        <Fact>
        Public Sub MethodToAsyncMethod()
            Dim source0 =
<compilation>
    <file name="a.vb">
Imports System.Threading.Tasks
Class C
    Function F() As Task(Of Integer)
        Return Task.FromResult(1)
    End Function
End Class
    </file>
</compilation>
            Dim source1 =
<compilation>
    <file name="a.vb">
Imports System.Threading.Tasks
Class C
    Async Function F() As Task(Of Integer)
        Return Await Task.FromResult(1)
    End Function
End Class
    </file>
</compilation>

            Dim compilation0 = CompilationUtils.CreateCompilationWithReferences(source0, references:=LatestReferences, options:=TestOptions.DebugDll)
            Dim compilation1 = CompilationUtils.CreateCompilationWithReferences(source1, references:=LatestReferences, options:=TestOptions.DebugDll)

            Dim v0 = CompileAndVerify(compilation:=compilation0)

            Using md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)
                Dim method0 = compilation0.GetMember(Of MethodSymbol)("C.F")
                Dim method1 = compilation1.GetMember(Of MethodSymbol)("C.F")

                Dim generation0 = EmitBaseline.CreateInitialBaseline(md0, EmptyLocalsProvider)
                Dim diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, method0, method1)))

                Using md1 = diff1.GetMetadata()
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
                                        Row(1, TableIndex.InterfaceImpl, EditAndContinueOperation.Default))
                End Using
            End Using
        End Sub

        <Fact>
        Public Sub IteratorMethodToMethod()
            Dim source0 =
<compilation>
    <file name="a.vb">
Imports System.Collections.Generic
Class C
    Iterator Function F() As IEnumerable(Of Integer)
        Yield 1
        Yield 1
    End Function
End Class
    </file>
</compilation>
            Dim source1 =
<compilation>
    <file name="a.vb">
Imports System.Collections.Generic
Class C
    Function F() As IEnumerable(Of Integer)
        Return {1, 1}
    End Function
End Class
    </file>
</compilation>

            Dim compilation0 = CompilationUtils.CreateCompilationWithReferences(source0, references:=LatestReferences, options:=TestOptions.DebugDll)
            Dim compilation1 = CompilationUtils.CreateCompilationWithReferences(source1, references:=LatestReferences, options:=TestOptions.DebugDll)

            Dim v0 = CompileAndVerify(compilation:=compilation0)

            Using md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)
                Dim method0 = compilation0.GetMember(Of MethodSymbol)("C.F")
                Dim method1 = compilation1.GetMember(Of MethodSymbol)("C.F")

                Dim generation0 = EmitBaseline.CreateInitialBaseline(md0, EmptyLocalsProvider)
                Dim diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, method0, method1)))

                Using md1 = diff1.GetMetadata()
                    CheckEncLogDefinitions(md1.Reader,
                        Row(3, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default))
                End Using
            End Using
        End Sub

        <Fact>
        Public Sub AsyncMethodToMethod()
            Dim source0 =
<compilation>
    <file name="a.vb">
Imports System.Threading.Tasks
Class C
    Async Function F() As Task(Of Integer)
        Return Await Task.FromResult(1)
    End Function
End Class
    </file>
</compilation>
            Dim source1 =
<compilation>
    <file name="a.vb">
Imports System.Threading.Tasks
Class C
    Function F() As Task(Of Integer)
        Return Task.FromResult(1)
    End Function
End Class
    </file>
</compilation>

            Dim compilation0 = CompilationUtils.CreateCompilationWithReferences(source0, references:=LatestReferences, options:=TestOptions.DebugDll)
            Dim compilation1 = CompilationUtils.CreateCompilationWithReferences(source1, references:=LatestReferences, options:=TestOptions.DebugDll)

            Dim v0 = CompileAndVerify(compilation:=compilation0)

            Using md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)
                Dim method0 = compilation0.GetMember(Of MethodSymbol)("C.F")
                Dim method1 = compilation1.GetMember(Of MethodSymbol)("C.F")

                Dim generation0 = EmitBaseline.CreateInitialBaseline(md0, EmptyLocalsProvider)
                Dim diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, method0, method1)))

                Using md1 = diff1.GetMetadata()
                    CheckEncLogDefinitions(md1.Reader,
                        Row(3, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default))
                End Using
            End Using
        End Sub

        <Fact>
        Public Sub AsyncMethodOverloads()
            Dim source0 =
<compilation>
    <file name="a.vb">
Imports System.Threading.Tasks
Class C
    Async Function F(a As Integer) As Task(Of Integer)
        Return Await Task.FromResult(1)
    End Function
    Async Function F(a As Short) As Task(Of Integer)
        Return Await Task.FromResult(1)
    End Function
    Async Function F(a As Long) As Task(Of Integer)
        Return Await Task.FromResult(1)
    End Function
    Public Sub X() ' needs to be present to work around SymWriter bug #1068894
    End Sub
End Class
    </file>
</compilation>
            Dim source1 =
<compilation>
    <file name="a.vb">
Imports System.Threading.Tasks
Class C
    Async Function F(a As Long) As Task(Of Integer)
        Return Await Task.FromResult(2)
    End Function
    Async Function F(a As Integer) As Task(Of Integer)
        Return Await Task.FromResult(3)
    End Function
    Async Function F(a As Short) As Task(Of Integer)
        Return Await Task.FromResult(4)
    End Function
    Public Sub X() ' needs to be present to work around SymWriter bug #1068894
    End Sub
End Class
    </file>
</compilation>

            Dim compilation0 = CompilationUtils.CreateCompilationWithReferences(source0, references:=LatestReferences, options:=TestOptions.DebugDll)
            Dim compilation1 = compilation0.WithSource(source1)

            Dim v0 = CompileAndVerify(compilation:=compilation0)

            Using md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)
                Dim methodShort0 = compilation0.GetMembers("C.F").Single(Function(m) m.ToTestDisplayString() = "Function C.F(a As System.Int16) As System.Threading.Tasks.Task(Of System.Int32)")
                Dim methodShort1 = compilation1.GetMembers("C.F").Single(Function(m) m.ToTestDisplayString() = "Function C.F(a As System.Int16) As System.Threading.Tasks.Task(Of System.Int32)")

                Dim methodInt0 = compilation0.GetMembers("C.F").Single(Function(m) m.ToTestDisplayString() = "Function C.F(a As System.Int32) As System.Threading.Tasks.Task(Of System.Int32)")
                Dim methodInt1 = compilation1.GetMembers("C.F").Single(Function(m) m.ToTestDisplayString() = "Function C.F(a As System.Int32) As System.Threading.Tasks.Task(Of System.Int32)")

                Dim methodLong0 = compilation0.GetMembers("C.F").Single(Function(m) m.ToTestDisplayString() = "Function C.F(a As System.Int64) As System.Threading.Tasks.Task(Of System.Int32)")
                Dim methodLong1 = compilation1.GetMembers("C.F").Single(Function(m) m.ToTestDisplayString() = "Function C.F(a As System.Int64) As System.Threading.Tasks.Task(Of System.Int32)")

                Dim generation0 = EmitBaseline.CreateInitialBaseline(md0, EmptyLocalsProvider)
                Dim diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(
                        New SemanticEdit(SemanticEditKind.Update, methodShort0, methodShort1, preserveLocalVariables:=True),
                        New SemanticEdit(SemanticEditKind.Update, methodInt0, methodInt1, preserveLocalVariables:=True),
                        New SemanticEdit(SemanticEditKind.Update, methodLong0, methodLong1, preserveLocalVariables:=True)))

                Using md1 = diff1.GetMetadata()
                    ' notice no TypeDefs, FieldDefs
                    CheckEncLogDefinitions(md1.Reader,
                        Row(7, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(8, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(9, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(10, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(11, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(12, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(3, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(7, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(10, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(13, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(16, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(17, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(18, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(19, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(20, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(21, TableIndex.CustomAttribute, EditAndContinueOperation.Default))
                End Using
            End Using
        End Sub

        <Fact>
        Public Sub UpdateIterator_NoVariables()
            Dim source0 =
<compilation>
    <file name="a.vb">
Imports System.Collections.Generic
Class C
    Iterator Function F() As IEnumerable(Of Integer)
        Yield 1
    End Function
End Class
    </file>
</compilation>
            Dim source1 =
<compilation>
    <file name="a.vb">
Imports System.Collections.Generic
Class C
    Iterator Function F() As IEnumerable(Of Integer)
        Yield 2
    End Function
End Class
    </file>
</compilation>

            Dim compilation0 = CompilationUtils.CreateCompilationWithReferences(source0, references:=LatestReferences, options:=TestOptions.DebugDll)
            Dim compilation1 = CompilationUtils.CreateCompilationWithReferences(source1, references:=LatestReferences, options:=TestOptions.DebugDll)

            Dim v0 = CompileAndVerify(compilation:=compilation0)

            Using md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)
                Dim method0 = compilation0.GetMember(Of MethodSymbol)("C.F")
                Dim method1 = compilation1.GetMember(Of MethodSymbol)("C.F")

                Dim generation0 = EmitBaseline.CreateInitialBaseline(md0, EmptyLocalsProvider)
                Dim diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, method0, method1, preserveLocalVariables:=True)))

                ' only methods with sequence points should be listed in UpdatedMethods:
                AssertEx.Equal(Of Integer)({&H06000002UI, &H06000005UI}, diff1.UpdatedMethods.Select(Function(m) MetadataTokens.GetToken(m)))

                ' verify delta metadata contains expected rows
                Using md1 = diff1.GetMetadata()
                    CheckEncLogDefinitions(md1.Reader,
                        Row(3, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(4, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(13, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(14, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(15, TableIndex.CustomAttribute, EditAndContinueOperation.Default))
                End Using

                diff1.VerifyIL("C.VB$StateMachine_1_F.MoveNext()", "
{
  // Code size       63 (0x3f)
  .maxstack  3
  .locals init (Boolean V_0,
                Integer V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_0006:  stloc.1
  IL_0007:  ldloc.1
  IL_0008:  brfalse.s  IL_0012
  IL_000a:  br.s       IL_000c
  IL_000c:  ldloc.1
  IL_000d:  ldc.i4.1
  IL_000e:  beq.s      IL_0014
  IL_0010:  br.s       IL_0016
  IL_0012:  br.s       IL_0018
  IL_0014:  br.s       IL_0034
  IL_0016:  ldc.i4.0
  IL_0017:  ret
  IL_0018:  ldarg.0
  IL_0019:  ldc.i4.m1
  IL_001a:  dup
  IL_001b:  stloc.1
  IL_001c:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_0021:  nop
  IL_0022:  ldarg.0
  IL_0023:  ldc.i4.2
  IL_0024:  stfld      ""C.VB$StateMachine_1_F.$Current As Integer""
  IL_0029:  ldarg.0
  IL_002a:  ldc.i4.1
  IL_002b:  dup
  IL_002c:  stloc.1
  IL_002d:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_0032:  ldc.i4.1
  IL_0033:  ret
  IL_0034:  ldarg.0
  IL_0035:  ldc.i4.m1
  IL_0036:  dup
  IL_0037:  stloc.1
  IL_0038:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_003d:  ldc.i4.0
  IL_003e:  ret
}
")
                v0.VerifyIL("C.VB$StateMachine_1_F.MoveNext()", "
{
  // Code size       63 (0x3f)
  .maxstack  3
  .locals init (Boolean V_0,
                Integer V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_0006:  stloc.1
  IL_0007:  ldloc.1
  IL_0008:  brfalse.s  IL_0012
  IL_000a:  br.s       IL_000c
  IL_000c:  ldloc.1
  IL_000d:  ldc.i4.1
  IL_000e:  beq.s      IL_0014
  IL_0010:  br.s       IL_0016
  IL_0012:  br.s       IL_0018
  IL_0014:  br.s       IL_0034
  IL_0016:  ldc.i4.0
  IL_0017:  ret
  IL_0018:  ldarg.0
  IL_0019:  ldc.i4.m1
  IL_001a:  dup
  IL_001b:  stloc.1
  IL_001c:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_0021:  nop
  IL_0022:  ldarg.0
  IL_0023:  ldc.i4.1
  IL_0024:  stfld      ""C.VB$StateMachine_1_F.$Current As Integer""
  IL_0029:  ldarg.0
  IL_002a:  ldc.i4.1
  IL_002b:  dup
  IL_002c:  stloc.1
  IL_002d:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_0032:  ldc.i4.1
  IL_0033:  ret
  IL_0034:  ldarg.0
  IL_0035:  ldc.i4.m1
  IL_0036:  dup
  IL_0037:  stloc.1
  IL_0038:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_003d:  ldc.i4.0
  IL_003e:  ret
}
")
            End Using
        End Sub

        <Fact>
        Public Sub UpdateAsync_NoVariables()
            Dim source0 =
<compilation>
    <file name="a.vb">
Imports System.Threading.Tasks
Class C
    Async Function F() As Task(Of Integer)
        Await Task.FromResult(1)
        Return 2
    End Function
End Class
    </file>
</compilation>
            Dim source1 =
<compilation>
    <file name="a.vb">
Imports System.Threading.Tasks
Class C
    Async Function F() As Task(Of Integer)
        Await Task.FromResult(10)
        Return 20
    End Function
End Class
    </file>
</compilation>

            Dim compilation0 = CompilationUtils.CreateCompilationWithReferences(source0, references:=LatestReferences, options:=TestOptions.DebugDll)
            Dim compilation1 = CompilationUtils.CreateCompilationWithReferences(source1, references:=LatestReferences, options:=TestOptions.DebugDll)

            Dim v0 = CompileAndVerify(compilation:=compilation0)

            Using md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)
                Dim method0 = compilation0.GetMember(Of MethodSymbol)("C.F")
                Dim method1 = compilation1.GetMember(Of MethodSymbol)("C.F")

                Dim generation0 = EmitBaseline.CreateInitialBaseline(md0, EmptyLocalsProvider)
                Dim diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, method0, method1, preserveLocalVariables:=True)))

                ' only methods with sequence points should be listed in UpdatedMethods:
                AssertEx.Equal(Of Integer)({&H06000004UI}, diff1.UpdatedMethods.Select(Function(m) MetadataTokens.GetToken(m)))

                ' verify delta metadata contains expected rows
                Using md1 = diff1.GetMetadata()
                    CheckEncLogDefinitions(md1.Reader,
                        Row(3, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(4, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(8, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(9, TableIndex.CustomAttribute, EditAndContinueOperation.Default))
                End Using

                diff1.VerifyIL("C.VB$StateMachine_1_F.MoveNext()", "
{
  // Code size      187 (0xbb)
  .maxstack  3
  .locals init (Integer V_0,
                Integer V_1,
                System.Threading.Tasks.Task(Of Integer) V_2,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_3,
                C.VB$StateMachine_1_F V_4,
                System.Exception V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_0006:  stloc.1
  .try
  {
    IL_0007:  ldloc.1
    IL_0008:  brfalse.s  IL_000c
    IL_000a:  br.s       IL_000e
    IL_000c:  br.s       IL_004b
    IL_000e:  nop
    IL_000f:  nop
    IL_0010:  ldc.i4.s   10
    IL_0012:  call       ""Function System.Threading.Tasks.Task.FromResult(Of Integer)(Integer) As System.Threading.Tasks.Task(Of Integer)""
    IL_0017:  callvirt   ""Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_001c:  stloc.3
    IL_001d:  ldloca.s   V_3
    IL_001f:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean""
    IL_0024:  brtrue.s   IL_0069
    IL_0026:  ldarg.0
    IL_0027:  ldc.i4.0
    IL_0028:  dup
    IL_0029:  stloc.1
    IL_002a:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
    IL_002f:  ldarg.0
    IL_0030:  ldloc.3
    IL_0031:  stfld      ""C.VB$StateMachine_1_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_0036:  ldarg.0
    IL_0037:  ldflda     ""C.VB$StateMachine_1_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
    IL_003c:  ldloca.s   V_3
    IL_003e:  ldarg.0
    IL_003f:  stloc.s    V_4
    IL_0041:  ldloca.s   V_4
    IL_0043:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), C.VB$StateMachine_1_F)(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef C.VB$StateMachine_1_F)""
    IL_0048:  nop
    IL_0049:  leave.s    IL_00ba
    IL_004b:  ldarg.0
    IL_004c:  ldc.i4.m1
    IL_004d:  dup
    IL_004e:  stloc.1
    IL_004f:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
    IL_0054:  ldarg.0
    IL_0055:  ldfld      ""C.VB$StateMachine_1_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_005a:  stloc.3
    IL_005b:  ldarg.0
    IL_005c:  ldflda     ""C.VB$StateMachine_1_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_0061:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_0067:  br.s       IL_0069
    IL_0069:  ldloca.s   V_3
    IL_006b:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer""
    IL_0070:  pop
    IL_0071:  ldloca.s   V_3
    IL_0073:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_0079:  ldc.i4.s   20
    IL_007b:  stloc.0
    IL_007c:  leave.s    IL_00a3
  }
  catch System.Exception
  {
    IL_007e:  dup
    IL_007f:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)""
    IL_0084:  stloc.s    V_5
    IL_0086:  ldarg.0
    IL_0087:  ldc.i4.s   -2
    IL_0089:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
    IL_008e:  ldarg.0
    IL_008f:  ldflda     ""C.VB$StateMachine_1_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
    IL_0094:  ldloc.s    V_5
    IL_0096:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetException(System.Exception)""
    IL_009b:  nop
    IL_009c:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()""
    IL_00a1:  leave.s    IL_00ba
  }
  IL_00a3:  ldarg.0
  IL_00a4:  ldc.i4.s   -2
  IL_00a6:  dup
  IL_00a7:  stloc.1
  IL_00a8:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_00ad:  ldarg.0
  IL_00ae:  ldflda     ""C.VB$StateMachine_1_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
  IL_00b3:  ldloc.0
  IL_00b4:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetResult(Integer)""
  IL_00b9:  nop
  IL_00ba:  ret
}
")
                v0.VerifyIL("C.VB$StateMachine_1_F.MoveNext()", "
{
  // Code size      185 (0xb9)
  .maxstack  3
  .locals init (Integer V_0,
                Integer V_1,
                System.Threading.Tasks.Task(Of Integer) V_2,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_3,
                C.VB$StateMachine_1_F V_4,
                System.Exception V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_0006:  stloc.1
  .try
  {
    IL_0007:  ldloc.1
    IL_0008:  brfalse.s  IL_000c
    IL_000a:  br.s       IL_000e
    IL_000c:  br.s       IL_004a
    IL_000e:  nop
    IL_000f:  nop
    IL_0010:  ldc.i4.1
    IL_0011:  call       ""Function System.Threading.Tasks.Task.FromResult(Of Integer)(Integer) As System.Threading.Tasks.Task(Of Integer)""
    IL_0016:  callvirt   ""Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_001b:  stloc.3
    IL_001c:  ldloca.s   V_3
    IL_001e:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean""
    IL_0023:  brtrue.s   IL_0068
    IL_0025:  ldarg.0
    IL_0026:  ldc.i4.0
    IL_0027:  dup
    IL_0028:  stloc.1
    IL_0029:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
    IL_002e:  ldarg.0
    IL_002f:  ldloc.3
    IL_0030:  stfld      ""C.VB$StateMachine_1_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_0035:  ldarg.0
    IL_0036:  ldflda     ""C.VB$StateMachine_1_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
    IL_003b:  ldloca.s   V_3
    IL_003d:  ldarg.0
    IL_003e:  stloc.s    V_4
    IL_0040:  ldloca.s   V_4
    IL_0042:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), C.VB$StateMachine_1_F)(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef C.VB$StateMachine_1_F)""
    IL_0047:  nop
    IL_0048:  leave.s    IL_00b8
    IL_004a:  ldarg.0
    IL_004b:  ldc.i4.m1
    IL_004c:  dup
    IL_004d:  stloc.1
    IL_004e:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
    IL_0053:  ldarg.0
    IL_0054:  ldfld      ""C.VB$StateMachine_1_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_0059:  stloc.3
    IL_005a:  ldarg.0
    IL_005b:  ldflda     ""C.VB$StateMachine_1_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_0060:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_0066:  br.s       IL_0068
    IL_0068:  ldloca.s   V_3
    IL_006a:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer""
    IL_006f:  pop
    IL_0070:  ldloca.s   V_3
    IL_0072:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_0078:  ldc.i4.2
    IL_0079:  stloc.0
    IL_007a:  leave.s    IL_00a1
  }
  catch System.Exception
  {
    IL_007c:  dup
    IL_007d:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)""
    IL_0082:  stloc.s    V_5
    IL_0084:  ldarg.0
    IL_0085:  ldc.i4.s   -2
    IL_0087:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
    IL_008c:  ldarg.0
    IL_008d:  ldflda     ""C.VB$StateMachine_1_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
    IL_0092:  ldloc.s    V_5
    IL_0094:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetException(System.Exception)""
    IL_0099:  nop
    IL_009a:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()""
    IL_009f:  leave.s    IL_00b8
  }
  IL_00a1:  ldarg.0
  IL_00a2:  ldc.i4.s   -2
  IL_00a4:  dup
  IL_00a5:  stloc.1
  IL_00a6:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_00ab:  ldarg.0
  IL_00ac:  ldflda     ""C.VB$StateMachine_1_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
  IL_00b1:  ldloc.0
  IL_00b2:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetResult(Integer)""
  IL_00b7:  nop
  IL_00b8:  ret
}
")
            End Using
        End Sub

        <Fact>
        Public Sub UpdateIterator_UserDefinedVariables_NoChange()
            Dim source0 =
<compilation>
    <file name="a.vb">
Imports System.Collections.Generic
Class C
    Iterator Function F(p as Integer) As IEnumerable(Of Integer)
        dim x = p
        Yield 1
    End Function
    Public Sub X() ' needs to be present to work around SymWriter bug #1068894
    End Sub
End Class
    </file>
</compilation>
            Dim source1 =
<compilation>
    <file name="a.vb">
Imports System.Collections.Generic
Class C
    Iterator Function F(p as Integer) As IEnumerable(Of Integer)
        dim x = p
        Yield 2
    End Function
    Public Sub X() ' needs to be present to work around SymWriter bug #1068894
    End Sub
End Class
    </file>
</compilation>

            Dim compilation0 = CompilationUtils.CreateCompilationWithReferences(source0, references:=LatestReferences, options:=ComSafeDebugDll)
            Dim compilation1 = compilation0.WithSource(source1)

            Dim v0 = CompileAndVerify(compilation:=compilation0)
            Dim symReader = v0.CreateSymReader()

            Using md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)
                Dim method0 = compilation0.GetMember(Of MethodSymbol)("C.F")
                Dim method1 = compilation1.GetMember(Of MethodSymbol)("C.F")

                Dim generation0 = EmitBaseline.CreateInitialBaseline(md0, Function(handle) symReader.GetEncMethodDebugInfo(handle))
                Dim diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables:=True)))

                ' only methods with sequence points should be listed in UpdatedMethods:
                AssertEx.Equal(Of Integer)({&H06000002UI, &H06000006UI}, diff1.UpdatedMethods.Select(Function(m) MetadataTokens.GetToken(m)))

                ' verify delta metadata contains expected rows
                Using md1 = diff1.GetMetadata()
                    '  Verify that no new TypeDefs, FieldDefs or MethodDefs were added and 3 methods were updated:
                    CheckEncLogDefinitions(md1.Reader,
                        Row(3, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(4, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(6, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(13, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(14, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(15, TableIndex.CustomAttribute, EditAndContinueOperation.Default))
                End Using

                diff1.VerifyIL("C.VB$StateMachine_1_F.MoveNext()", "
{
  // Code size       75 (0x4b)
  .maxstack  3
  .locals init (Boolean V_0,
                Integer V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_0006:  stloc.1
  IL_0007:  ldloc.1
  IL_0008:  brfalse.s  IL_0012
  IL_000a:  br.s       IL_000c
  IL_000c:  ldloc.1
  IL_000d:  ldc.i4.1
  IL_000e:  beq.s      IL_0014
  IL_0010:  br.s       IL_0016
  IL_0012:  br.s       IL_0018
  IL_0014:  br.s       IL_0040
  IL_0016:  ldc.i4.0
  IL_0017:  ret
  IL_0018:  ldarg.0
  IL_0019:  ldc.i4.m1
  IL_001a:  dup
  IL_001b:  stloc.1
  IL_001c:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_0021:  nop
  IL_0022:  ldarg.0
  IL_0023:  ldarg.0
  IL_0024:  ldfld      ""C.VB$StateMachine_1_F.$VB$Local_p As Integer""
  IL_0029:  stfld      ""C.VB$StateMachine_1_F.$VB$ResumableLocal_x$0 As Integer""
  IL_002e:  ldarg.0
  IL_002f:  ldc.i4.2
  IL_0030:  stfld      ""C.VB$StateMachine_1_F.$Current As Integer""
  IL_0035:  ldarg.0
  IL_0036:  ldc.i4.1
  IL_0037:  dup
  IL_0038:  stloc.1
  IL_0039:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_003e:  ldc.i4.1
  IL_003f:  ret
  IL_0040:  ldarg.0
  IL_0041:  ldc.i4.m1
  IL_0042:  dup
  IL_0043:  stloc.1
  IL_0044:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_0049:  ldc.i4.0
  IL_004a:  ret
}
")
            End Using
        End Sub

        <Fact>
        Public Sub UpdateIterator_UserDefinedVariables_AddVariable()
            Dim source0 =
<compilation>
    <file name="a.vb">
Imports System.Collections.Generic
Class C
    Iterator Function F(p as Integer) As IEnumerable(Of Integer)
        dim x = p
        Yield 1
    End Function
    Public Sub X() ' needs to be present to work around SymWriter bug #1068894
    End Sub
End Class
    </file>
</compilation>
            Dim source1 =
<compilation>
    <file name="a.vb">
Imports System.Collections.Generic
Class C
    Iterator Function F(p as Integer) As IEnumerable(Of Integer)
        dim x = p
        dim y = 1234
        Yield y
    End Function
    Public Sub X() ' needs to be present to work around SymWriter bug #1068894
    End Sub
End Class
    </file>
</compilation>

            Dim compilation0 = CompilationUtils.CreateCompilationWithReferences(source0, references:=LatestReferences, options:=ComSafeDebugDll)
            Dim compilation1 = compilation0.WithSource(source1)

            Dim v0 = CompileAndVerify(compilation:=compilation0)
            Dim symReader = v0.CreateSymReader()

            Using md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)
                Dim method0 = compilation0.GetMember(Of MethodSymbol)("C.F")
                Dim method1 = compilation1.GetMember(Of MethodSymbol)("C.F")

                Dim generation0 = EmitBaseline.CreateInitialBaseline(md0, Function(handle) symReader.GetEncMethodDebugInfo(handle))
                Dim diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables:=True)))

                ' only methods with sequence points should be listed in UpdatedMethods:
                AssertEx.Equal(Of Integer)({&H06000002UI, &H06000006UI}, diff1.UpdatedMethods.Select(Function(m) MetadataTokens.GetToken(m)))

                ' verify delta metadata contains expected rows
                Using md1 = diff1.GetMetadata()
                    ' 1 field added & 3 methods updated
                    CheckEncLogDefinitions(md1.Reader,
                        Row(3, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(4, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                        Row(8, TableIndex.Field, EditAndContinueOperation.Default),
                        Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(6, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(13, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(14, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(15, TableIndex.CustomAttribute, EditAndContinueOperation.Default))
                End Using

                diff1.VerifyIL("C.VB$StateMachine_1_F.MoveNext()", "
{
  // Code size       91 (0x5b)
  .maxstack  3
  .locals init (Boolean V_0,
                Integer V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_0006:  stloc.1
  IL_0007:  ldloc.1
  IL_0008:  brfalse.s  IL_0012
  IL_000a:  br.s       IL_000c
  IL_000c:  ldloc.1
  IL_000d:  ldc.i4.1
  IL_000e:  beq.s      IL_0014
  IL_0010:  br.s       IL_0016
  IL_0012:  br.s       IL_0018
  IL_0014:  br.s       IL_0050
  IL_0016:  ldc.i4.0
  IL_0017:  ret
  IL_0018:  ldarg.0
  IL_0019:  ldc.i4.m1
  IL_001a:  dup
  IL_001b:  stloc.1
  IL_001c:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_0021:  nop
  IL_0022:  ldarg.0
  IL_0023:  ldarg.0
  IL_0024:  ldfld      ""C.VB$StateMachine_1_F.$VB$Local_p As Integer""
  IL_0029:  stfld      ""C.VB$StateMachine_1_F.$VB$ResumableLocal_x$0 As Integer""
  IL_002e:  ldarg.0
  IL_002f:  ldc.i4     0x4d2
  IL_0034:  stfld      ""C.VB$StateMachine_1_F.$VB$ResumableLocal_y$1 As Integer""
  IL_0039:  ldarg.0
  IL_003a:  ldarg.0
  IL_003b:  ldfld      ""C.VB$StateMachine_1_F.$VB$ResumableLocal_y$1 As Integer""
  IL_0040:  stfld      ""C.VB$StateMachine_1_F.$Current As Integer""
  IL_0045:  ldarg.0
  IL_0046:  ldc.i4.1
  IL_0047:  dup
  IL_0048:  stloc.1
  IL_0049:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_004e:  ldc.i4.1
  IL_004f:  ret
  IL_0050:  ldarg.0
  IL_0051:  ldc.i4.m1
  IL_0052:  dup
  IL_0053:  stloc.1
  IL_0054:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_0059:  ldc.i4.0
  IL_005a:  ret
}
")
            End Using
        End Sub

        <Fact>
        Public Sub UpdateIterator_UserDefinedVariables_AddAndRemoveVariable()
            Dim source0 =
<compilation>
    <file name="a.vb">
Imports System.Collections.Generic
Class C
    Iterator Function F(p as Integer) As IEnumerable(Of Integer)
        dim x = p
        Yield 1
    End Function
    Public Sub X() ' needs to be present to work around SymWriter bug #1068894
    End Sub
End Class
    </file>
</compilation>
            Dim source1 =
<compilation>
    <file name="a.vb">
Imports System.Collections.Generic
Class C
    Iterator Function F(p as Integer) As IEnumerable(Of Integer)
        dim y = 1234
        Yield p
    End Function
    Public Sub X() ' needs to be present to work around SymWriter bug #1068894
    End Sub
End Class
    </file>
</compilation>

            Dim compilation0 = CompilationUtils.CreateCompilationWithReferences(source0, references:=LatestReferences, options:=ComSafeDebugDll)
            Dim compilation1 = compilation0.WithSource(source1)

            Dim v0 = CompileAndVerify(compilation:=compilation0)
            Dim symReader = v0.CreateSymReader()

            Using md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)
                Dim method0 = compilation0.GetMember(Of MethodSymbol)("C.F")
                Dim method1 = compilation1.GetMember(Of MethodSymbol)("C.F")

                Dim generation0 = EmitBaseline.CreateInitialBaseline(md0, Function(handle) symReader.GetEncMethodDebugInfo(handle))
                Dim diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables:=True)))

                ' only methods with sequence points should be listed in UpdatedMethods:
                AssertEx.Equal(Of Integer)({&H06000002UI, &H06000006UI}, diff1.UpdatedMethods.Select(Function(m) MetadataTokens.GetToken(m)))

                ' verify delta metadata contains expected rows
                Using md1 = diff1.GetMetadata()
                    ' 1 field added & 3 methods updated
                    CheckEncLogDefinitions(md1.Reader,
                        Row(3, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(4, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                        Row(8, TableIndex.Field, EditAndContinueOperation.Default),
                        Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(6, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(13, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(14, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(15, TableIndex.CustomAttribute, EditAndContinueOperation.Default))
                End Using

                diff1.VerifyIL("C.VB$StateMachine_1_F.MoveNext()", "
{
  // Code size       79 (0x4f)
  .maxstack  3
  .locals init (Boolean V_0,
                Integer V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_0006:  stloc.1
  IL_0007:  ldloc.1
  IL_0008:  brfalse.s  IL_0012
  IL_000a:  br.s       IL_000c
  IL_000c:  ldloc.1
  IL_000d:  ldc.i4.1
  IL_000e:  beq.s      IL_0014
  IL_0010:  br.s       IL_0016
  IL_0012:  br.s       IL_0018
  IL_0014:  br.s       IL_0044
  IL_0016:  ldc.i4.0
  IL_0017:  ret
  IL_0018:  ldarg.0
  IL_0019:  ldc.i4.m1
  IL_001a:  dup
  IL_001b:  stloc.1
  IL_001c:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_0021:  nop
  IL_0022:  ldarg.0
  IL_0023:  ldc.i4     0x4d2
  IL_0028:  stfld      ""C.VB$StateMachine_1_F.$VB$ResumableLocal_y$1 As Integer""
  IL_002d:  ldarg.0
  IL_002e:  ldarg.0
  IL_002f:  ldfld      ""C.VB$StateMachine_1_F.$VB$Local_p As Integer""
  IL_0034:  stfld      ""C.VB$StateMachine_1_F.$Current As Integer""
  IL_0039:  ldarg.0
  IL_003a:  ldc.i4.1
  IL_003b:  dup
  IL_003c:  stloc.1
  IL_003d:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_0042:  ldc.i4.1
  IL_0043:  ret
  IL_0044:  ldarg.0
  IL_0045:  ldc.i4.m1
  IL_0046:  dup
  IL_0047:  stloc.1
  IL_0048:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_004d:  ldc.i4.0
  IL_004e:  ret
}
")
            End Using
        End Sub

        <Fact>
        Public Sub UpdateIterator_UserDefinedVariables_ChangeVariableType()
            Dim source0 =
<compilation>
    <file name="a.vb">
Imports System.Collections.Generic
Class C
    Iterator Function F() As IEnumerable(Of Integer)
        dim x = 10.0
        Yield 1
    End Function
    Public Sub X() ' needs to be present to work around SymWriter bug #1068894
    End Sub
End Class
    </file>
</compilation>
            Dim source1 =
<compilation>
    <file name="a.vb">
Imports System.Collections.Generic
Class C
    Iterator Function F() As IEnumerable(Of Integer)
        dim x = 1234
        Yield 0
    End Function
    Public Sub X() ' needs to be present to work around SymWriter bug #1068894
    End Sub
End Class
    </file>
</compilation>

            Dim compilation0 = CompilationUtils.CreateCompilationWithReferences(source0, references:=LatestReferences, options:=ComSafeDebugDll)
            Dim compilation1 = compilation0.WithSource(source1)

            Dim v0 = CompileAndVerify(compilation:=compilation0)
            Dim symReader = v0.CreateSymReader()

            Using md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)
                Dim method0 = compilation0.GetMember(Of MethodSymbol)("C.F")
                Dim method1 = compilation1.GetMember(Of MethodSymbol)("C.F")

                Dim generation0 = EmitBaseline.CreateInitialBaseline(md0, Function(handle) symReader.GetEncMethodDebugInfo(handle))
                Dim diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables:=True)))

                ' only methods with sequence points should be listed in UpdatedMethods:
                AssertEx.Equal(Of Integer)({&H06000002UI, &H06000006UI}, diff1.UpdatedMethods.Select(Function(m) MetadataTokens.GetToken(m)))

                ' verify delta metadata contains expected rows
                Using md1 = diff1.GetMetadata()
                    ' 1 field def added & 3 methods updated
                    CheckEncLogDefinitions(md1.Reader,
                        Row(3, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(4, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                        Row(6, TableIndex.Field, EditAndContinueOperation.Default),
                        Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(6, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(13, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(14, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(15, TableIndex.CustomAttribute, EditAndContinueOperation.Default))
                End Using

                diff1.VerifyIL("C.VB$StateMachine_1_F.MoveNext()", "
{
  // Code size       74 (0x4a)
  .maxstack  3
  .locals init (Boolean V_0,
                Integer V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_0006:  stloc.1
  IL_0007:  ldloc.1
  IL_0008:  brfalse.s  IL_0012
  IL_000a:  br.s       IL_000c
  IL_000c:  ldloc.1
  IL_000d:  ldc.i4.1
  IL_000e:  beq.s      IL_0014
  IL_0010:  br.s       IL_0016
  IL_0012:  br.s       IL_0018
  IL_0014:  br.s       IL_003f
  IL_0016:  ldc.i4.0
  IL_0017:  ret
  IL_0018:  ldarg.0
  IL_0019:  ldc.i4.m1
  IL_001a:  dup
  IL_001b:  stloc.1
  IL_001c:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_0021:  nop
  IL_0022:  ldarg.0
  IL_0023:  ldc.i4     0x4d2
  IL_0028:  stfld      ""C.VB$StateMachine_1_F.$VB$ResumableLocal_x$1 As Integer""
  IL_002d:  ldarg.0
  IL_002e:  ldc.i4.0
  IL_002f:  stfld      ""C.VB$StateMachine_1_F.$Current As Integer""
  IL_0034:  ldarg.0
  IL_0035:  ldc.i4.1
  IL_0036:  dup
  IL_0037:  stloc.1
  IL_0038:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_003d:  ldc.i4.1
  IL_003e:  ret
  IL_003f:  ldarg.0
  IL_0040:  ldc.i4.m1
  IL_0041:  dup
  IL_0042:  stloc.1
  IL_0043:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_0048:  ldc.i4.0
  IL_0049:  ret
}
")
            End Using
        End Sub

        <Fact>
        Public Sub UpdateIterator_SynthesizedVariables_ChangeVariableType()
            Dim source0 =
<compilation>
    <file name="a.vb">
Imports System.Collections.Generic
Class C
    Iterator Function F() As IEnumerable(Of Integer)
        For Each x In {1, 2}
            Yield 1
        Next
    End Function
    Public Sub Y() ' needs to be present to work around SymWriter bug #1068894
    End Sub
End Class
    </file>
</compilation>
            Dim source1 =
<compilation>
    <file name="a.vb">
Imports System.Collections.Generic
Class C
    Iterator Function F() As IEnumerable(Of Integer)
        For Each x In {1.0, 2.0}
            Yield 1
        Next
    End Function
    Public Sub Y() ' needs to be present to work around SymWriter bug #1068894
    End Sub
End Class
    </file>
</compilation>

            ' Rude edit but the compiler should handle it

            Dim compilation0 = CompilationUtils.CreateCompilationWithReferences(source0, references:=LatestReferences, options:=ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            Dim compilation1 = compilation0.WithSource(source1)

            Dim v0 = CompileAndVerify(compilation:=compilation0, symbolValidator:=Sub([module] As ModuleSymbol)
                                                                                      Assert.Equal(
                                                                                      {
                                                                                        "$State: System.Int32",
                                                                                        "$Current: System.Int32",
                                                                                        "$InitialThreadId: System.Int32",
                                                                                        "$VB$Me: C",
                                                                                        "$S0: System.Int32()",
                                                                                        "$S1: System.Int32",
                                                                                        "$VB$ResumableLocal_x$2: System.Int32"
                                                                                      }, [module].GetFieldNamesAndTypes("C.VB$StateMachine_1_F"))
                                                                                  End Sub)

            Dim v1 = CompileAndVerify(compilation:=compilation1, symbolValidator:=Sub([module] As ModuleSymbol)
                                                                                      Assert.Equal(
                                                                                      {
                                                                                        "$State: System.Int32",
                                                                                        "$Current: System.Int32",
                                                                                        "$InitialThreadId: System.Int32",
                                                                                        "$VB$Me: C",
                                                                                        "$S0: System.Double()",
                                                                                        "$S1: System.Int32",
                                                                                        "$VB$ResumableLocal_x$2: System.Double"
                                                                                      }, [module].GetFieldNamesAndTypes("C.VB$StateMachine_1_F"))
                                                                                  End Sub)

            Dim symReader = v0.CreateSymReader()

            Using md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)
                Dim method0 = compilation0.GetMember(Of MethodSymbol)("C.F")
                Dim method1 = compilation1.GetMember(Of MethodSymbol)("C.F")

                Dim generation0 = EmitBaseline.CreateInitialBaseline(md0, Function(handle) symReader.GetEncMethodDebugInfo(handle))
                Dim diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, method0, method1, GetSyntaxMapByKind(method0, SyntaxKind.ForEachStatement), preserveLocalVariables:=True)))

                ' only methods with sequence points should be listed in UpdatedMethods:
                AssertEx.Equal(Of Integer)({&H06000002UI, &H06000006UI}, diff1.UpdatedMethods.Select(Function(m) MetadataTokens.GetToken(m)))

                ' verify delta metadata contains expected rows
                Using md1 = diff1.GetMetadata()
                    ' 2 field defs added and 3 methods updated
                    CheckEncLogDefinitions(md1.Reader,
                        Row(3, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(4, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                        Row(8, TableIndex.Field, EditAndContinueOperation.Default),
                        Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                        Row(9, TableIndex.Field, EditAndContinueOperation.Default),
                        Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(6, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(13, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(14, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(15, TableIndex.CustomAttribute, EditAndContinueOperation.Default))
                End Using

                diff1.VerifyIL("C.VB$StateMachine_1_F.MoveNext()", "
{
  // Code size      162 (0xa2)
  .maxstack  5
  .locals init (Boolean V_0,
                Integer V_1,
                Boolean V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_0006:  stloc.1
  IL_0007:  ldloc.1
  IL_0008:  brfalse.s  IL_0012
  IL_000a:  br.s       IL_000c
  IL_000c:  ldloc.1
  IL_000d:  ldc.i4.1
  IL_000e:  beq.s      IL_0014
  IL_0010:  br.s       IL_0016
  IL_0012:  br.s       IL_0018
  IL_0014:  br.s       IL_0074
  IL_0016:  ldc.i4.0
  IL_0017:  ret
  IL_0018:  ldarg.0
  IL_0019:  ldc.i4.m1
  IL_001a:  dup
  IL_001b:  stloc.1
  IL_001c:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_0021:  nop
  IL_0022:  ldarg.0
  IL_0023:  ldc.i4.2
  IL_0024:  newarr     ""Double""
  IL_0029:  dup
  IL_002a:  ldc.i4.0
  IL_002b:  ldc.r8     1
  IL_0034:  stelem.r8
  IL_0035:  dup
  IL_0036:  ldc.i4.1
  IL_0037:  ldc.r8     2
  IL_0040:  stelem.r8
  IL_0041:  stfld      ""C.VB$StateMachine_1_F.$S3 As Double()""
  IL_0046:  ldarg.0
  IL_0047:  ldc.i4.0
  IL_0048:  stfld      ""C.VB$StateMachine_1_F.$S1 As Integer""
  IL_004d:  br.s       IL_008c
  IL_004f:  ldarg.0
  IL_0050:  ldarg.0
  IL_0051:  ldfld      ""C.VB$StateMachine_1_F.$S3 As Double()""
  IL_0056:  ldarg.0
  IL_0057:  ldfld      ""C.VB$StateMachine_1_F.$S1 As Integer""
  IL_005c:  ldelem.r8
  IL_005d:  stfld      ""C.VB$StateMachine_1_F.$VB$ResumableLocal_x$4 As Double""
  IL_0062:  ldarg.0
  IL_0063:  ldc.i4.1
  IL_0064:  stfld      ""C.VB$StateMachine_1_F.$Current As Integer""
  IL_0069:  ldarg.0
  IL_006a:  ldc.i4.1
  IL_006b:  dup
  IL_006c:  stloc.1
  IL_006d:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_0072:  ldc.i4.1
  IL_0073:  ret
  IL_0074:  ldarg.0
  IL_0075:  ldc.i4.m1
  IL_0076:  dup
  IL_0077:  stloc.1
  IL_0078:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_007d:  nop
  IL_007e:  ldarg.0
  IL_007f:  ldarg.0
  IL_0080:  ldfld      ""C.VB$StateMachine_1_F.$S1 As Integer""
  IL_0085:  ldc.i4.1
  IL_0086:  add.ovf
  IL_0087:  stfld      ""C.VB$StateMachine_1_F.$S1 As Integer""
  IL_008c:  ldarg.0
  IL_008d:  ldfld      ""C.VB$StateMachine_1_F.$S1 As Integer""
  IL_0092:  ldarg.0
  IL_0093:  ldfld      ""C.VB$StateMachine_1_F.$S3 As Double()""
  IL_0098:  ldlen
  IL_0099:  conv.i4
  IL_009a:  clt
  IL_009c:  stloc.2
  IL_009d:  ldloc.2
  IL_009e:  brtrue.s   IL_004f
  IL_00a0:  ldc.i4.0
  IL_00a1:  ret
}
")
            End Using
        End Sub

        <Fact>
        Public Sub UpdateAsync_UserDefinedVariables_NoChange()
            Dim source0 =
<compilation>
    <file name="a.vb">
Imports System.Threading.Tasks
Class C
    Async Function F(p As Integer) As Task(Of Integer)
        Dim x = p
        Return Await Task.FromResult(10)
    End Function
    Public Sub X() ' needs to be present to work around SymWriter bug #1068894
    End Sub
End Class
    </file>
</compilation>
            Dim source1 =
<compilation>
    <file name="a.vb">
Imports System.Threading.Tasks
Class C
    Async Function F(p As Integer) As Task(Of Integer)
        Dim x = p
        Return Await Task.FromResult(20)
    End Function
    Public Sub X() ' needs to be present to work around SymWriter bug #1068894
    End Sub
End Class
    </file>
</compilation>

            Dim compilation0 = CompilationUtils.CreateCompilationWithReferences(source0, references:=LatestReferences, options:=ComSafeDebugDll)
            Dim compilation1 = compilation0.WithSource(source1)

            Dim v0 = CompileAndVerify(compilation:=compilation0)
            Dim symReader = v0.CreateSymReader()

            Using md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)
                Dim method0 = compilation0.GetMember(Of MethodSymbol)("C.F")
                Dim method1 = compilation1.GetMember(Of MethodSymbol)("C.F")

                Dim generation0 = EmitBaseline.CreateInitialBaseline(md0, Function(handle) symReader.GetEncMethodDebugInfo(handle))
                Dim diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables:=True)))

                ' only methods with sequence points should be listed in UpdatedMethods:
                AssertEx.Equal(Of Integer)({&H06000005UI}, diff1.UpdatedMethods.Select(Function(m) MetadataTokens.GetToken(m)))

                ' verify delta metadata contains expected rows
                Using md1 = diff1.GetMetadata()
                    CheckEncLogDefinitions(md1.Reader,
                        Row(3, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(4, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(8, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(9, TableIndex.CustomAttribute, EditAndContinueOperation.Default))
                End Using

                diff1.VerifyIL("C.VB$StateMachine_1_F.MoveNext()", "
{
  // Code size      200 (0xc8)
  .maxstack  3
  .locals init (Integer V_0,
                Integer V_1,
                System.Threading.Tasks.Task(Of Integer) V_2,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_3,
                C.VB$StateMachine_1_F V_4,
                Integer V_5,
                System.Exception V_6)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_0006:  stloc.1
  .try
  {
    IL_0007:  ldloc.1
    IL_0008:  brfalse.s  IL_000c
    IL_000a:  br.s       IL_000e
    IL_000c:  br.s       IL_0057
    IL_000e:  nop
    IL_000f:  ldarg.0
    IL_0010:  ldarg.0
    IL_0011:  ldfld      ""C.VB$StateMachine_1_F.$VB$Local_p As Integer""
    IL_0016:  stfld      ""C.VB$StateMachine_1_F.$VB$ResumableLocal_x$0 As Integer""
    IL_001b:  nop
    IL_001c:  ldc.i4.s   20
    IL_001e:  call       ""Function System.Threading.Tasks.Task.FromResult(Of Integer)(Integer) As System.Threading.Tasks.Task(Of Integer)""
    IL_0023:  callvirt   ""Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_0028:  stloc.3
    IL_0029:  ldloca.s   V_3
    IL_002b:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean""
    IL_0030:  brtrue.s   IL_0075
    IL_0032:  ldarg.0
    IL_0033:  ldc.i4.0
    IL_0034:  dup
    IL_0035:  stloc.1
    IL_0036:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
    IL_003b:  ldarg.0
    IL_003c:  ldloc.3
    IL_003d:  stfld      ""C.VB$StateMachine_1_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_0042:  ldarg.0
    IL_0043:  ldflda     ""C.VB$StateMachine_1_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
    IL_0048:  ldloca.s   V_3
    IL_004a:  ldarg.0
    IL_004b:  stloc.s    V_4
    IL_004d:  ldloca.s   V_4
    IL_004f:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), C.VB$StateMachine_1_F)(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef C.VB$StateMachine_1_F)""
    IL_0054:  nop
    IL_0055:  leave.s    IL_00c7
    IL_0057:  ldarg.0
    IL_0058:  ldc.i4.m1
    IL_0059:  dup
    IL_005a:  stloc.1
    IL_005b:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
    IL_0060:  ldarg.0
    IL_0061:  ldfld      ""C.VB$StateMachine_1_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_0066:  stloc.3
    IL_0067:  ldarg.0
    IL_0068:  ldflda     ""C.VB$StateMachine_1_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_006d:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_0073:  br.s       IL_0075
    IL_0075:  ldloca.s   V_3
    IL_0077:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer""
    IL_007c:  stloc.s    V_5
    IL_007e:  ldloca.s   V_3
    IL_0080:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_0086:  ldloc.s    V_5
    IL_0088:  stloc.0
    IL_0089:  leave.s    IL_00b0
  }
  catch System.Exception
  {
    IL_008b:  dup
    IL_008c:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)""
    IL_0091:  stloc.s    V_6
    IL_0093:  ldarg.0
    IL_0094:  ldc.i4.s   -2
    IL_0096:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
    IL_009b:  ldarg.0
    IL_009c:  ldflda     ""C.VB$StateMachine_1_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
    IL_00a1:  ldloc.s    V_6
    IL_00a3:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetException(System.Exception)""
    IL_00a8:  nop
    IL_00a9:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()""
    IL_00ae:  leave.s    IL_00c7
  }
  IL_00b0:  ldarg.0
  IL_00b1:  ldc.i4.s   -2
  IL_00b3:  dup
  IL_00b4:  stloc.1
  IL_00b5:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_00ba:  ldarg.0
  IL_00bb:  ldflda     ""C.VB$StateMachine_1_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
  IL_00c0:  ldloc.0
  IL_00c1:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetResult(Integer)""
  IL_00c6:  nop
  IL_00c7:  ret
}
")
            End Using
        End Sub

        <Fact>
        Public Sub UpdateAsync_UserDefinedVariables_AddVariable()
            Dim source0 =
<compilation>
    <file name="a.vb">
Imports System.Threading.Tasks
Class C
    Async Function F(p As Integer) As Task(Of Integer)
        Dim x = p
        Return Await Task.FromResult(10)
    End Function
    Public Sub X() ' needs to be present to work around SymWriter bug #1068894
    End Sub
End Class
    </file>
</compilation>
            Dim source1 =
<compilation>
    <file name="a.vb">
Imports System.Threading.Tasks
Class C
    Async Function F(p As Integer) As Task(Of Integer)
        Dim x = p
        Dim y = 10
        Return Await Task.FromResult(y)
    End Function
    Public Sub X() ' needs to be present to work around SymWriter bug #1068894
    End Sub
End Class
    </file>
</compilation>

            Dim compilation0 = CompilationUtils.CreateCompilationWithReferences(source0, references:=LatestReferences, options:=ComSafeDebugDll)
            Dim compilation1 = compilation0.WithSource(source1)

            Dim v0 = CompileAndVerify(compilation:=compilation0)
            Dim symReader = v0.CreateSymReader()

            Using md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)
                Dim method0 = compilation0.GetMember(Of MethodSymbol)("C.F")
                Dim method1 = compilation1.GetMember(Of MethodSymbol)("C.F")

                Dim generation0 = EmitBaseline.CreateInitialBaseline(md0, Function(handle) symReader.GetEncMethodDebugInfo(handle))
                Dim diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables:=True)))

                ' only methods with sequence points should be listed in UpdatedMethods:
                AssertEx.Equal(Of Integer)({&H06000005UI}, diff1.UpdatedMethods.Select(Function(m) MetadataTokens.GetToken(m)))

                ' verify delta metadata contains expected rows
                Using md1 = diff1.GetMetadata()
                    CheckEncLogDefinitions(md1.Reader,
                        Row(3, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(4, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                        Row(7, TableIndex.Field, EditAndContinueOperation.Default),
                        Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(8, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(9, TableIndex.CustomAttribute, EditAndContinueOperation.Default))
                End Using

                diff1.VerifyIL("C.VB$StateMachine_1_F.MoveNext()", "
{
  // Code size      212 (0xd4)
  .maxstack  3
  .locals init (Integer V_0,
                Integer V_1,
                System.Threading.Tasks.Task(Of Integer) V_2,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_3,
                C.VB$StateMachine_1_F V_4,
                Integer V_5,
                System.Exception V_6)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_0006:  stloc.1
  .try
  {
    IL_0007:  ldloc.1
    IL_0008:  brfalse.s  IL_000c
    IL_000a:  br.s       IL_000e
    IL_000c:  br.s       IL_0063
    IL_000e:  nop
    IL_000f:  ldarg.0
    IL_0010:  ldarg.0
    IL_0011:  ldfld      ""C.VB$StateMachine_1_F.$VB$Local_p As Integer""
    IL_0016:  stfld      ""C.VB$StateMachine_1_F.$VB$ResumableLocal_x$0 As Integer""
    IL_001b:  ldarg.0
    IL_001c:  ldc.i4.s   10
    IL_001e:  stfld      ""C.VB$StateMachine_1_F.$VB$ResumableLocal_y$1 As Integer""
    IL_0023:  nop
    IL_0024:  ldarg.0
    IL_0025:  ldfld      ""C.VB$StateMachine_1_F.$VB$ResumableLocal_y$1 As Integer""
    IL_002a:  call       ""Function System.Threading.Tasks.Task.FromResult(Of Integer)(Integer) As System.Threading.Tasks.Task(Of Integer)""
    IL_002f:  callvirt   ""Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_0034:  stloc.3
    IL_0035:  ldloca.s   V_3
    IL_0037:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean""
    IL_003c:  brtrue.s   IL_0081
    IL_003e:  ldarg.0
    IL_003f:  ldc.i4.0
    IL_0040:  dup
    IL_0041:  stloc.1
    IL_0042:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
    IL_0047:  ldarg.0
    IL_0048:  ldloc.3
    IL_0049:  stfld      ""C.VB$StateMachine_1_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_004e:  ldarg.0
    IL_004f:  ldflda     ""C.VB$StateMachine_1_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
    IL_0054:  ldloca.s   V_3
    IL_0056:  ldarg.0
    IL_0057:  stloc.s    V_4
    IL_0059:  ldloca.s   V_4
    IL_005b:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), C.VB$StateMachine_1_F)(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef C.VB$StateMachine_1_F)""
    IL_0060:  nop
    IL_0061:  leave.s    IL_00d3
    IL_0063:  ldarg.0
    IL_0064:  ldc.i4.m1
    IL_0065:  dup
    IL_0066:  stloc.1
    IL_0067:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
    IL_006c:  ldarg.0
    IL_006d:  ldfld      ""C.VB$StateMachine_1_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_0072:  stloc.3
    IL_0073:  ldarg.0
    IL_0074:  ldflda     ""C.VB$StateMachine_1_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_0079:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_007f:  br.s       IL_0081
    IL_0081:  ldloca.s   V_3
    IL_0083:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer""
    IL_0088:  stloc.s    V_5
    IL_008a:  ldloca.s   V_3
    IL_008c:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_0092:  ldloc.s    V_5
    IL_0094:  stloc.0
    IL_0095:  leave.s    IL_00bc
  }
  catch System.Exception
  {
    IL_0097:  dup
    IL_0098:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)""
    IL_009d:  stloc.s    V_6
    IL_009f:  ldarg.0
    IL_00a0:  ldc.i4.s   -2
    IL_00a2:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
    IL_00a7:  ldarg.0
    IL_00a8:  ldflda     ""C.VB$StateMachine_1_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
    IL_00ad:  ldloc.s    V_6
    IL_00af:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetException(System.Exception)""
    IL_00b4:  nop
    IL_00b5:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()""
    IL_00ba:  leave.s    IL_00d3
  }
  IL_00bc:  ldarg.0
  IL_00bd:  ldc.i4.s   -2
  IL_00bf:  dup
  IL_00c0:  stloc.1
  IL_00c1:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_00c6:  ldarg.0
  IL_00c7:  ldflda     ""C.VB$StateMachine_1_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
  IL_00cc:  ldloc.0
  IL_00cd:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetResult(Integer)""
  IL_00d2:  nop
  IL_00d3:  ret
}
")
            End Using
        End Sub

        <Fact>
        Public Sub UpdateAsync_UserDefinedVariables_AddAndRemoveVariable()
            Dim source0 =
<compilation>
    <file name="a.vb">
Imports System.Threading.Tasks
Class C
    Async Function F(p As Integer) As Task(Of Integer)
        Dim x = p
        Return Await Task.FromResult(10)
    End Function
    Public Sub X() ' needs to be present to work around SymWriter bug #1068894
    End Sub
End Class
    </file>
</compilation>
            Dim source1 =
<compilation>
    <file name="a.vb">
Imports System.Threading.Tasks
Class C
    Async Function F(p As Integer) As Task(Of Integer)
        Dim y = 1234
        Return Await Task.FromResult(p)
    End Function
    Public Sub X() ' needs to be present to work around SymWriter bug #1068894
    End Sub
End Class
    </file>
</compilation>

            Dim compilation0 = CompilationUtils.CreateCompilationWithReferences(source0, references:=LatestReferences, options:=ComSafeDebugDll)
            Dim compilation1 = compilation0.WithSource(source1)

            Dim v0 = CompileAndVerify(compilation:=compilation0)
            Dim symReader = v0.CreateSymReader()

            Using md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)
                Dim method0 = compilation0.GetMember(Of MethodSymbol)("C.F")
                Dim method1 = compilation1.GetMember(Of MethodSymbol)("C.F")

                Dim generation0 = EmitBaseline.CreateInitialBaseline(md0, Function(handle) symReader.GetEncMethodDebugInfo(handle))
                Dim diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables:=True)))

                ' only methods with sequence points should be listed in UpdatedMethods:
                AssertEx.Equal(Of Integer)({&H06000005UI}, diff1.UpdatedMethods.Select(Function(m) MetadataTokens.GetToken(m)))

                ' verify delta metadata contains expected rows
                Using md1 = diff1.GetMetadata()
                    CheckEncLogDefinitions(md1.Reader,
                        Row(3, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(4, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                        Row(7, TableIndex.Field, EditAndContinueOperation.Default),
                        Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(8, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(9, TableIndex.CustomAttribute, EditAndContinueOperation.Default))
                End Using

                diff1.VerifyIL("C.VB$StateMachine_1_F.MoveNext()", "
{
  // Code size      203 (0xcb)
  .maxstack  3
  .locals init (Integer V_0,
                Integer V_1,
                System.Threading.Tasks.Task(Of Integer) V_2,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_3,
                C.VB$StateMachine_1_F V_4,
                Integer V_5,
                System.Exception V_6)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_0006:  stloc.1
  .try
  {
    IL_0007:  ldloc.1
    IL_0008:  brfalse.s  IL_000c
    IL_000a:  br.s       IL_000e
    IL_000c:  br.s       IL_005a
    IL_000e:  nop
    IL_000f:  ldarg.0
    IL_0010:  ldc.i4     0x4d2
    IL_0015:  stfld      ""C.VB$StateMachine_1_F.$VB$ResumableLocal_y$1 As Integer""
    IL_001a:  nop
    IL_001b:  ldarg.0
    IL_001c:  ldfld      ""C.VB$StateMachine_1_F.$VB$Local_p As Integer""
    IL_0021:  call       ""Function System.Threading.Tasks.Task.FromResult(Of Integer)(Integer) As System.Threading.Tasks.Task(Of Integer)""
    IL_0026:  callvirt   ""Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_002b:  stloc.3
    IL_002c:  ldloca.s   V_3
    IL_002e:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean""
    IL_0033:  brtrue.s   IL_0078
    IL_0035:  ldarg.0
    IL_0036:  ldc.i4.0
    IL_0037:  dup
    IL_0038:  stloc.1
    IL_0039:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
    IL_003e:  ldarg.0
    IL_003f:  ldloc.3
    IL_0040:  stfld      ""C.VB$StateMachine_1_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_0045:  ldarg.0
    IL_0046:  ldflda     ""C.VB$StateMachine_1_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
    IL_004b:  ldloca.s   V_3
    IL_004d:  ldarg.0
    IL_004e:  stloc.s    V_4
    IL_0050:  ldloca.s   V_4
    IL_0052:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), C.VB$StateMachine_1_F)(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef C.VB$StateMachine_1_F)""
    IL_0057:  nop
    IL_0058:  leave.s    IL_00ca
    IL_005a:  ldarg.0
    IL_005b:  ldc.i4.m1
    IL_005c:  dup
    IL_005d:  stloc.1
    IL_005e:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
    IL_0063:  ldarg.0
    IL_0064:  ldfld      ""C.VB$StateMachine_1_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_0069:  stloc.3
    IL_006a:  ldarg.0
    IL_006b:  ldflda     ""C.VB$StateMachine_1_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_0070:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_0076:  br.s       IL_0078
    IL_0078:  ldloca.s   V_3
    IL_007a:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer""
    IL_007f:  stloc.s    V_5
    IL_0081:  ldloca.s   V_3
    IL_0083:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_0089:  ldloc.s    V_5
    IL_008b:  stloc.0
    IL_008c:  leave.s    IL_00b3
  }
  catch System.Exception
  {
    IL_008e:  dup
    IL_008f:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)""
    IL_0094:  stloc.s    V_6
    IL_0096:  ldarg.0
    IL_0097:  ldc.i4.s   -2
    IL_0099:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
    IL_009e:  ldarg.0
    IL_009f:  ldflda     ""C.VB$StateMachine_1_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
    IL_00a4:  ldloc.s    V_6
    IL_00a6:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetException(System.Exception)""
    IL_00ab:  nop
    IL_00ac:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()""
    IL_00b1:  leave.s    IL_00ca
  }
  IL_00b3:  ldarg.0
  IL_00b4:  ldc.i4.s   -2
  IL_00b6:  dup
  IL_00b7:  stloc.1
  IL_00b8:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_00bd:  ldarg.0
  IL_00be:  ldflda     ""C.VB$StateMachine_1_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
  IL_00c3:  ldloc.0
  IL_00c4:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetResult(Integer)""
  IL_00c9:  nop
  IL_00ca:  ret
}
")
            End Using
        End Sub

        <Fact>
        Public Sub UpdateAsync_UserDefinedVariables_ChangeVariableType()
            Dim source0 =
<compilation>
    <file name="a.vb">
Imports System.Threading.Tasks
Class C
    Async Function F() As Task(Of Integer)
        Dim x = 10
        Return Await Task.FromResult(10)
    End Function
    Public Sub X() ' needs to be present to work around SymWriter bug #1068894
    End Sub
End Class
    </file>
</compilation>
            Dim source1 =
<compilation>
    <file name="a.vb">
Imports System.Threading.Tasks
Class C
    Async Function F() As Task(Of Integer)
        Dim x = 10.0
        Return Await Task.FromResult(20)
    End Function
    Public Sub X() ' needs to be present to work around SymWriter bug #1068894
    End Sub
End Class
    </file>
</compilation>

            Dim compilation0 = CompilationUtils.CreateCompilationWithReferences(source0, references:=LatestReferences, options:=ComSafeDebugDll)
            Dim compilation1 = compilation0.WithSource(source1)

            Dim v0 = CompileAndVerify(compilation:=compilation0)
            Dim symReader = v0.CreateSymReader()

            Using md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)
                Dim method0 = compilation0.GetMember(Of MethodSymbol)("C.F")
                Dim method1 = compilation1.GetMember(Of MethodSymbol)("C.F")

                Dim generation0 = EmitBaseline.CreateInitialBaseline(md0, Function(handle) symReader.GetEncMethodDebugInfo(handle))
                Dim diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables:=True)))

                ' only methods with sequence points should be listed in UpdatedMethods:
                AssertEx.Equal(Of Integer)({&H06000005UI}, diff1.UpdatedMethods.Select(Function(m) MetadataTokens.GetToken(m)))

                ' verify delta metadata contains expected rows
                Using md1 = diff1.GetMetadata()
                    CheckEncLogDefinitions(md1.Reader,
                        Row(3, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(4, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                        Row(6, TableIndex.Field, EditAndContinueOperation.Default),
                        Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(8, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(9, TableIndex.CustomAttribute, EditAndContinueOperation.Default))
                End Using

                diff1.VerifyIL("C.VB$StateMachine_1_F.MoveNext()", "
{
  // Code size      203 (0xcb)
  .maxstack  3
  .locals init (Integer V_0,
                Integer V_1,
                System.Threading.Tasks.Task(Of Integer) V_2,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_3,
                C.VB$StateMachine_1_F V_4,
                Integer V_5,
                System.Exception V_6)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_0006:  stloc.1
  .try
  {
    IL_0007:  ldloc.1
    IL_0008:  brfalse.s  IL_000c
    IL_000a:  br.s       IL_000e
    IL_000c:  br.s       IL_005a
    IL_000e:  nop
    IL_000f:  ldarg.0
    IL_0010:  ldc.r8     10
    IL_0019:  stfld      ""C.VB$StateMachine_1_F.$VB$ResumableLocal_x$1 As Double""
    IL_001e:  nop
    IL_001f:  ldc.i4.s   20
    IL_0021:  call       ""Function System.Threading.Tasks.Task.FromResult(Of Integer)(Integer) As System.Threading.Tasks.Task(Of Integer)""
    IL_0026:  callvirt   ""Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_002b:  stloc.3
    IL_002c:  ldloca.s   V_3
    IL_002e:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean""
    IL_0033:  brtrue.s   IL_0078
    IL_0035:  ldarg.0
    IL_0036:  ldc.i4.0
    IL_0037:  dup
    IL_0038:  stloc.1
    IL_0039:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
    IL_003e:  ldarg.0
    IL_003f:  ldloc.3
    IL_0040:  stfld      ""C.VB$StateMachine_1_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_0045:  ldarg.0
    IL_0046:  ldflda     ""C.VB$StateMachine_1_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
    IL_004b:  ldloca.s   V_3
    IL_004d:  ldarg.0
    IL_004e:  stloc.s    V_4
    IL_0050:  ldloca.s   V_4
    IL_0052:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), C.VB$StateMachine_1_F)(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef C.VB$StateMachine_1_F)""
    IL_0057:  nop
    IL_0058:  leave.s    IL_00ca
    IL_005a:  ldarg.0
    IL_005b:  ldc.i4.m1
    IL_005c:  dup
    IL_005d:  stloc.1
    IL_005e:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
    IL_0063:  ldarg.0
    IL_0064:  ldfld      ""C.VB$StateMachine_1_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_0069:  stloc.3
    IL_006a:  ldarg.0
    IL_006b:  ldflda     ""C.VB$StateMachine_1_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_0070:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_0076:  br.s       IL_0078
    IL_0078:  ldloca.s   V_3
    IL_007a:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer""
    IL_007f:  stloc.s    V_5
    IL_0081:  ldloca.s   V_3
    IL_0083:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_0089:  ldloc.s    V_5
    IL_008b:  stloc.0
    IL_008c:  leave.s    IL_00b3
  }
  catch System.Exception
  {
    IL_008e:  dup
    IL_008f:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)""
    IL_0094:  stloc.s    V_6
    IL_0096:  ldarg.0
    IL_0097:  ldc.i4.s   -2
    IL_0099:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
    IL_009e:  ldarg.0
    IL_009f:  ldflda     ""C.VB$StateMachine_1_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
    IL_00a4:  ldloc.s    V_6
    IL_00a6:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetException(System.Exception)""
    IL_00ab:  nop
    IL_00ac:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()""
    IL_00b1:  leave.s    IL_00ca
  }
  IL_00b3:  ldarg.0
  IL_00b4:  ldc.i4.s   -2
  IL_00b6:  dup
  IL_00b7:  stloc.1
  IL_00b8:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_00bd:  ldarg.0
  IL_00be:  ldflda     ""C.VB$StateMachine_1_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
  IL_00c3:  ldloc.0
  IL_00c4:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetResult(Integer)""
  IL_00c9:  nop
  IL_00ca:  ret
}
")
            End Using
        End Sub

        <Fact>
        Public Sub HoistedVariables_MultipleGenerations()
            Dim source0 =
<compilation>
    <file name="a.vb">
Imports System.Threading.Tasks
Class C
    Async Function F() As Task(Of Integer) ' testing type changes G0 -> G1, G1 -> G2
        Dim a1 As Boolean = True
        Dim a2 As Integer = 3
        Await Task.Delay(0)
        Return 1
    End Function

    Async Function G() As Task(Of Integer) ' testing G1 -> G3
        Dim c = New C()
        Dim a1 As Boolean = True
        Await Task.Delay(0)
        Return 1
    End Function

    Async Function H() As Task(Of Integer) ' testing G0 -> G3
        Dim c = New C()
        Dim a1 As Boolean = True
        Await Task.Delay(0)
        Return 1
    End Function

    Public Sub X() ' needs to be present to work around SymWriter bug #1068894
    End Sub 
End Class
    </file>
</compilation>
            Dim source1 =
<compilation>
    <file name="a.vb">
Imports System.Threading.Tasks
Class C
    Async Function F() As Task(Of Integer) ' updated
        Dim a1 = new C()
        Dim a2 As Integer = 3
        Await Task.Delay(0)
        Return 1
    End Function

    Async Function G() As Task(Of Integer) ' updated
        Dim c = New C()
        Dim a1 As Boolean = True
        Await Task.Delay(0)
        Return 2
    End Function

    Async Function H() As Task(Of Integer)
        Dim c = New C()
        Dim a1 As Boolean = True
        Await Task.Delay(0)
        Return 1
    End Function

    Public Sub X() ' needs to be present to work around SymWriter bug #1068894
    End Sub
End Class
    </file>
</compilation>
            Dim source2 =
<compilation>
    <file name="a.vb">
Imports System.Threading.Tasks
Class C
    Async Function F() As Task(Of Integer) ' updated
        Dim a1 As Boolean = True
        Dim a2 = New C()
        Await Task.Delay(0)
        Return 1
    End Function

    Async Function G() As Task(Of Integer)
        Dim c = New C()
        Dim a1 As Boolean = True
        Await Task.Delay(0)
        Return 2
    End Function

    Async Function H() As Task(Of Integer)
        Dim c = New C()
        Dim a1 As Boolean = True
        Await Task.Delay(0)
        Return 1
    End Function

    Public Sub X() ' needs to be present to work around SymWriter bug #1068894
    End Sub
End Class
    </file>
</compilation>
            Dim source3 =
<compilation>
    <file name="a.vb">
Imports System.Threading.Tasks
Class C
    Async Function F() As Task(Of Integer)
        Dim a1 As Boolean = True
        Dim a2 = New C()
        Await Task.Delay(0)
        Return 1
    End Function

    Async Function G() As Task(Of Integer) ' updated
        Dim c = New C()
        Dim a1 = New C()
        Await Task.Delay(0)
        Return 1
    End Function

    Async Function H() As Task(Of Integer) ' updated
        Dim c = New C()
        Dim a1 = New C()
        Await Task.Delay(0)
        Return 1
    End Function

    Public Sub X() ' needs to be present to work around SymWriter bug #1068894
    End Sub
End Class
    </file>
</compilation>

            ' Rude edit but the compiler should handle it
            Dim compilation0 = CreateCompilationWithReferences(source0, references:=LatestReferences, options:=ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            Dim compilation1 = compilation0.WithSource(source1)
            Dim compilation2 = compilation1.WithSource(source2)
            Dim compilation3 = compilation2.WithSource(source3)

            Dim f0 = compilation0.GetMember(Of MethodSymbol)("C.F")
            Dim f1 = compilation1.GetMember(Of MethodSymbol)("C.F")
            Dim f2 = compilation2.GetMember(Of MethodSymbol)("C.F")
            Dim f3 = compilation3.GetMember(Of MethodSymbol)("C.F")

            Dim g0 = compilation0.GetMember(Of MethodSymbol)("C.G")
            Dim g1 = compilation1.GetMember(Of MethodSymbol)("C.G")
            Dim g2 = compilation2.GetMember(Of MethodSymbol)("C.G")
            Dim g3 = compilation3.GetMember(Of MethodSymbol)("C.G")

            Dim h0 = compilation0.GetMember(Of MethodSymbol)("C.H")
            Dim h1 = compilation1.GetMember(Of MethodSymbol)("C.H")
            Dim h2 = compilation2.GetMember(Of MethodSymbol)("C.H")
            Dim h3 = compilation3.GetMember(Of MethodSymbol)("C.H")

            Dim v0 = CompileAndVerify(compilation:=compilation0, symbolValidator:=Sub([module] As ModuleSymbol)
                                                                                      Assert.Equal(
                                                                                      {
                                                                                        "$State: System.Int32",
                                                                                        "$Builder: System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of System.Int32)",
                                                                                        "$VB$Me: C",
                                                                                        "$VB$ResumableLocal_a1$0: System.Boolean",
                                                                                        "$VB$ResumableLocal_a2$1: System.Int32",
                                                                                        "$A0: System.Runtime.CompilerServices.TaskAwaiter"
                                                                                      }, [module].GetFieldNamesAndTypes("C.VB$StateMachine_1_F"))
                                                                                  End Sub)

            Dim md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)

            Dim generation0 = EmitBaseline.CreateInitialBaseline(md0, Function(handle) v0.CreateSymReader().GetEncMethodDebugInfo(handle))

            Dim diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(
                        New SemanticEdit(SemanticEditKind.Update, f0, f1, GetEquivalentNodesMap(f1, f0), preserveLocalVariables:=True),
                        New SemanticEdit(SemanticEditKind.Update, g0, g1, GetEquivalentNodesMap(g1, g0), preserveLocalVariables:=True)))

            diff1.VerifySynthesizedMembers(
                "C: {VB$StateMachine_1_F, VB$StateMachine_2_G}",
                "C.VB$StateMachine_1_F: {$State, $Builder, $VB$Me, $VB$ResumableLocal_a1$2, $VB$ResumableLocal_a2$1, $A0, MoveNext, System.Runtime.CompilerServices.IAsyncStateMachine.SetStateMachine}",
                "C.VB$StateMachine_2_G: {$State, $Builder, $VB$Me, $VB$ResumableLocal_c$0, $VB$ResumableLocal_a1$1, $A0, MoveNext, System.Runtime.CompilerServices.IAsyncStateMachine.SetStateMachine}")

            Dim diff2 = compilation2.EmitDifference(
                    diff1.NextGeneration,
                    ImmutableArray.Create(
                        New SemanticEdit(SemanticEditKind.Update, f1, f2, GetEquivalentNodesMap(f2, f1), preserveLocalVariables:=True)))

            diff2.VerifySynthesizedMembers(
                "C: {VB$StateMachine_1_F, VB$StateMachine_2_G}",
                "C.VB$StateMachine_1_F: {$State, $Builder, $VB$Me, $VB$ResumableLocal_a1$3, $VB$ResumableLocal_a2$4, $A0, MoveNext, System.Runtime.CompilerServices.IAsyncStateMachine.SetStateMachine, $VB$ResumableLocal_a1$2, $VB$ResumableLocal_a2$1}",
                "C.VB$StateMachine_2_G: {$State, $Builder, $VB$Me, $VB$ResumableLocal_c$0, $VB$ResumableLocal_a1$1, $A0, MoveNext, System.Runtime.CompilerServices.IAsyncStateMachine.SetStateMachine}")

            Dim diff3 = compilation3.EmitDifference(
                    diff2.NextGeneration,
                    ImmutableArray.Create(
                        New SemanticEdit(SemanticEditKind.Update, g2, g3, GetEquivalentNodesMap(g3, g2), preserveLocalVariables:=True),
                        New SemanticEdit(SemanticEditKind.Update, h2, h3, GetEquivalentNodesMap(h3, h2), preserveLocalVariables:=True)))

            diff3.VerifySynthesizedMembers(
                "C: {VB$StateMachine_2_G, VB$StateMachine_3_H, VB$StateMachine_1_F}",
                "C.VB$StateMachine_1_F: {$State, $Builder, $VB$Me, $VB$ResumableLocal_a1$3, $VB$ResumableLocal_a2$4, $A0, MoveNext, System.Runtime.CompilerServices.IAsyncStateMachine.SetStateMachine, $VB$ResumableLocal_a1$2, $VB$ResumableLocal_a2$1}",
                "C.VB$StateMachine_2_G: {$State, $Builder, $VB$Me, $VB$ResumableLocal_c$0, $VB$ResumableLocal_a1$2, $A0, MoveNext, System.Runtime.CompilerServices.IAsyncStateMachine.SetStateMachine, $VB$ResumableLocal_a1$1}",
                "C.VB$StateMachine_3_H: {$State, $Builder, $VB$Me, $VB$ResumableLocal_c$0, $VB$ResumableLocal_a1$2, $A0, MoveNext, System.Runtime.CompilerServices.IAsyncStateMachine.SetStateMachine}")

            ' Verify delta metadata contains expected rows.
            Dim md1 = diff1.GetMetadata()
            Dim md2 = diff2.GetMetadata()
            Dim md3 = diff3.GetMetadata()

            ' 1 field def added & 4 methods updated (MoveNext And kickoff for F And G)
            CheckEncLogDefinitions(md1.Reader,
                    Row(7, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                    Row(8, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                    Row(9, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                    Row(10, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                    Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                    Row(19, TableIndex.Field, EditAndContinueOperation.Default),
                    Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                    Row(3, TableIndex.MethodDef, EditAndContinueOperation.Default),
                    Row(7, TableIndex.MethodDef, EditAndContinueOperation.Default),
                    Row(10, TableIndex.MethodDef, EditAndContinueOperation.Default),
                    Row(16, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(17, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(18, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(19, TableIndex.CustomAttribute, EditAndContinueOperation.Default))

            diff1.VerifyIL("C.VB$StateMachine_1_F.MoveNext()", "
{
  // Code size      203 (0xcb)
  .maxstack  3
  .locals init (Integer V_0,
                Integer V_1,
                System.Threading.Tasks.Task(Of Integer) V_2,
                System.Runtime.CompilerServices.TaskAwaiter V_3,
                C.VB$StateMachine_1_F V_4,
                System.Exception V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_0006:  stloc.1
  .try
  {
    IL_0007:  ldloc.1
    IL_0008:  brfalse.s  IL_000c
    IL_000a:  br.s       IL_000e
    IL_000c:  br.s       IL_005c
    IL_000e:  nop
    IL_000f:  ldarg.0
    IL_0010:  newobj     ""Sub C..ctor()""
    IL_0015:  stfld      ""C.VB$StateMachine_1_F.$VB$ResumableLocal_a1$2 As C""
    IL_001a:  ldarg.0
    IL_001b:  ldc.i4.3
    IL_001c:  stfld      ""C.VB$StateMachine_1_F.$VB$ResumableLocal_a2$1 As Integer""
    IL_0021:  nop
    IL_0022:  ldc.i4.0
    IL_0023:  call       ""Function System.Threading.Tasks.Task.Delay(Integer) As System.Threading.Tasks.Task""
    IL_0028:  callvirt   ""Function System.Threading.Tasks.Task.GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter""
    IL_002d:  stloc.3
    IL_002e:  ldloca.s   V_3
    IL_0030:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter.get_IsCompleted() As Boolean""
    IL_0035:  brtrue.s   IL_007a
    IL_0037:  ldarg.0
    IL_0038:  ldc.i4.0
    IL_0039:  dup
    IL_003a:  stloc.1
    IL_003b:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
    IL_0040:  ldarg.0
    IL_0041:  ldloc.3
    IL_0042:  stfld      ""C.VB$StateMachine_1_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
    IL_0047:  ldarg.0
    IL_0048:  ldflda     ""C.VB$StateMachine_1_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
    IL_004d:  ldloca.s   V_3
    IL_004f:  ldarg.0
    IL_0050:  stloc.s    V_4
    IL_0052:  ldloca.s   V_4
    IL_0054:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter, C.VB$StateMachine_1_F)(ByRef System.Runtime.CompilerServices.TaskAwaiter, ByRef C.VB$StateMachine_1_F)""
    IL_0059:  nop
    IL_005a:  leave.s    IL_00ca
    IL_005c:  ldarg.0
    IL_005d:  ldc.i4.m1
    IL_005e:  dup
    IL_005f:  stloc.1
    IL_0060:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
    IL_0065:  ldarg.0
    IL_0066:  ldfld      ""C.VB$StateMachine_1_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
    IL_006b:  stloc.3
    IL_006c:  ldarg.0
    IL_006d:  ldflda     ""C.VB$StateMachine_1_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
    IL_0072:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_0078:  br.s       IL_007a
    IL_007a:  ldloca.s   V_3
    IL_007c:  call       ""Sub System.Runtime.CompilerServices.TaskAwaiter.GetResult()""
    IL_0081:  nop
    IL_0082:  ldloca.s   V_3
    IL_0084:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_008a:  ldc.i4.1
    IL_008b:  stloc.0
    IL_008c:  leave.s    IL_00b3
  }
  catch System.Exception
  {
    IL_008e:  dup
    IL_008f:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)""
    IL_0094:  stloc.s    V_5
    IL_0096:  ldarg.0
    IL_0097:  ldc.i4.s   -2
    IL_0099:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
    IL_009e:  ldarg.0
    IL_009f:  ldflda     ""C.VB$StateMachine_1_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
    IL_00a4:  ldloc.s    V_5
    IL_00a6:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetException(System.Exception)""
    IL_00ab:  nop
    IL_00ac:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()""
    IL_00b1:  leave.s    IL_00ca
  }
  IL_00b3:  ldarg.0
  IL_00b4:  ldc.i4.s   -2
  IL_00b6:  dup
  IL_00b7:  stloc.1
  IL_00b8:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_00bd:  ldarg.0
  IL_00be:  ldflda     ""C.VB$StateMachine_1_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
  IL_00c3:  ldloc.0
  IL_00c4:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetResult(Integer)""
  IL_00c9:  nop
  IL_00ca:  ret
}
")

            ' 2 field defs added (both variables a1 and a2 of F changed their types) & 2 methods updated
            CheckEncLogDefinitions(md2.Reader,
                    Row(11, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                    Row(12, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                    Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                    Row(20, TableIndex.Field, EditAndContinueOperation.Default),
                    Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                    Row(21, TableIndex.Field, EditAndContinueOperation.Default),
                    Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                    Row(7, TableIndex.MethodDef, EditAndContinueOperation.Default),
                    Row(20, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(21, TableIndex.CustomAttribute, EditAndContinueOperation.Default))

            diff2.VerifyIL("C.VB$StateMachine_1_F.MoveNext()", "
{
  // Code size      203 (0xcb)
  .maxstack  3
  .locals init (Integer V_0,
                Integer V_1,
                System.Threading.Tasks.Task(Of Integer) V_2,
                System.Runtime.CompilerServices.TaskAwaiter V_3,
                C.VB$StateMachine_1_F V_4,
                System.Exception V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_0006:  stloc.1
  .try
  {
    IL_0007:  ldloc.1
    IL_0008:  brfalse.s  IL_000c
    IL_000a:  br.s       IL_000e
    IL_000c:  br.s       IL_005c
    IL_000e:  nop
    IL_000f:  ldarg.0
    IL_0010:  ldc.i4.1
    IL_0011:  stfld      ""C.VB$StateMachine_1_F.$VB$ResumableLocal_a1$3 As Boolean""
    IL_0016:  ldarg.0
    IL_0017:  newobj     ""Sub C..ctor()""
    IL_001c:  stfld      ""C.VB$StateMachine_1_F.$VB$ResumableLocal_a2$4 As C""
    IL_0021:  nop
    IL_0022:  ldc.i4.0
    IL_0023:  call       ""Function System.Threading.Tasks.Task.Delay(Integer) As System.Threading.Tasks.Task""
    IL_0028:  callvirt   ""Function System.Threading.Tasks.Task.GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter""
    IL_002d:  stloc.3
    IL_002e:  ldloca.s   V_3
    IL_0030:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter.get_IsCompleted() As Boolean""
    IL_0035:  brtrue.s   IL_007a
    IL_0037:  ldarg.0
    IL_0038:  ldc.i4.0
    IL_0039:  dup
    IL_003a:  stloc.1
    IL_003b:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
    IL_0040:  ldarg.0
    IL_0041:  ldloc.3
    IL_0042:  stfld      ""C.VB$StateMachine_1_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
    IL_0047:  ldarg.0
    IL_0048:  ldflda     ""C.VB$StateMachine_1_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
    IL_004d:  ldloca.s   V_3
    IL_004f:  ldarg.0
    IL_0050:  stloc.s    V_4
    IL_0052:  ldloca.s   V_4
    IL_0054:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter, C.VB$StateMachine_1_F)(ByRef System.Runtime.CompilerServices.TaskAwaiter, ByRef C.VB$StateMachine_1_F)""
    IL_0059:  nop
    IL_005a:  leave.s    IL_00ca
    IL_005c:  ldarg.0
    IL_005d:  ldc.i4.m1
    IL_005e:  dup
    IL_005f:  stloc.1
    IL_0060:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
    IL_0065:  ldarg.0
    IL_0066:  ldfld      ""C.VB$StateMachine_1_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
    IL_006b:  stloc.3
    IL_006c:  ldarg.0
    IL_006d:  ldflda     ""C.VB$StateMachine_1_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
    IL_0072:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_0078:  br.s       IL_007a
    IL_007a:  ldloca.s   V_3
    IL_007c:  call       ""Sub System.Runtime.CompilerServices.TaskAwaiter.GetResult()""
    IL_0081:  nop
    IL_0082:  ldloca.s   V_3
    IL_0084:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_008a:  ldc.i4.1
    IL_008b:  stloc.0
    IL_008c:  leave.s    IL_00b3
  }
  catch System.Exception
  {
    IL_008e:  dup
    IL_008f:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)""
    IL_0094:  stloc.s    V_5
    IL_0096:  ldarg.0
    IL_0097:  ldc.i4.s   -2
    IL_0099:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
    IL_009e:  ldarg.0
    IL_009f:  ldflda     ""C.VB$StateMachine_1_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
    IL_00a4:  ldloc.s    V_5
    IL_00a6:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetException(System.Exception)""
    IL_00ab:  nop
    IL_00ac:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()""
    IL_00b1:  leave.s    IL_00ca
  }
  IL_00b3:  ldarg.0
  IL_00b4:  ldc.i4.s   -2
  IL_00b6:  dup
  IL_00b7:  stloc.1
  IL_00b8:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_00bd:  ldarg.0
  IL_00be:  ldflda     ""C.VB$StateMachine_1_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
  IL_00c3:  ldloc.0
  IL_00c4:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetResult(Integer)""
  IL_00c9:  nop
  IL_00ca:  ret
}
")

            ' 2 field defs added - variables of G and H changed their types; 4 methods updated: G, H kickoff and MoveNext
            CheckEncLogDefinitions(md3.Reader,
                    Row(13, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                    Row(14, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                    Row(15, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                    Row(16, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                    Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                    Row(22, TableIndex.Field, EditAndContinueOperation.Default),
                    Row(5, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                    Row(23, TableIndex.Field, EditAndContinueOperation.Default),
                    Row(3, TableIndex.MethodDef, EditAndContinueOperation.Default),
                    Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                    Row(10, TableIndex.MethodDef, EditAndContinueOperation.Default),
                    Row(13, TableIndex.MethodDef, EditAndContinueOperation.Default),
                    Row(22, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(23, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(24, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(25, TableIndex.CustomAttribute, EditAndContinueOperation.Default))

        End Sub

        <Fact>
        Public Sub Awaiters1()
            Dim source0 =
<compilation>
    <file name="a.vb">
Imports System.Threading.Tasks
Class C
    Function A1() As Task(Of Boolean)
        Return Nothing
    End Function
    Function A2() As Task(Of Integer)
        Return Nothing
    End Function
    Function A3() As Task(Of Double)
        Return Nothing
    End Function

    Async Function F() As Task(Of Integer)
        Await A1()
        Await A2()
        Return 1
    End Function
    Async Function G() As Task(Of Integer)
        Await A2()
        Await A1()
        Return 1
    End Function
End Class
    </file>
</compilation>

            Dim compilation0 = CreateCompilationWithReferences(source0, references:=LatestReferences, options:=ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All))

            CompileAndVerify(compilation:=compilation0, symbolValidator:=Sub([module] As ModuleSymbol)
                                                                             Assert.Equal(
                                                                                      {
                                                                                        "$State: System.Int32",
                                                                                        "$Builder: System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of System.Int32)",
                                                                                        "$VB$Me: C",
                                                                                        "$A0: System.Runtime.CompilerServices.TaskAwaiter(Of System.Boolean)",
                                                                                        "$A1: System.Runtime.CompilerServices.TaskAwaiter(Of System.Int32)"
                                                                                      }, [module].GetFieldNamesAndTypes("C.VB$StateMachine_4_F"))

                                                                             Assert.Equal(
                                                                                      {
                                                                                        "$State: System.Int32",
                                                                                        "$Builder: System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of System.Int32)",
                                                                                        "$VB$Me: C",
                                                                                        "$A0: System.Runtime.CompilerServices.TaskAwaiter(Of System.Int32)",
                                                                                        "$A1: System.Runtime.CompilerServices.TaskAwaiter(Of System.Boolean)"
                                                                                      }, [module].GetFieldNamesAndTypes("C.VB$StateMachine_5_G"))
                                                                         End Sub)
        End Sub

        <Fact>
        Public Sub Awaiters_MultipleGenerations()
            Dim source0 =
<compilation>
    <file name="a.vb">
Imports System.Threading.Tasks
Class C
    Function A1() As Task(Of Boolean)
        Return Nothing
    End Function
    Function A2() As Task(Of Integer)
        Return Nothing
    End Function
    Function A3() As Task(Of C)
        Return Nothing
    End Function

    Async Function F() As Task(Of Integer) ' testing type changes G0 -> G1, G1 -> G2
        Await A1()
        Await A2()
        Return 1
    End Function
    Async Function G() As Task(Of Integer) ' testing G1 -> G3
        Await A1()
        Return 1
    End Function
    Async Function H() As Task(Of Integer) ' testing G0 -> G3
        Await A1()
        Return 1
    End Function
End Class
    </file>
</compilation>
            Dim source1 =
<compilation>
    <file name="a.vb">
Imports System.Threading.Tasks
Class C
    Function A1() As Task(Of Boolean)
        Return Nothing
    End Function
    Function A2() As Task(Of Integer)
        Return Nothing
    End Function
    Function A3() As Task(Of C)
        Return Nothing
    End Function

    Async Function F() As Task(Of Integer) ' updated
        Await A3()
        Await A2()
        Return 1
    End Function
    Async Function G() As Task(Of Integer) ' updated
        Await A1()
        Return 2
    End Function
    Async Function H() As Task(Of Integer)
        Await A1()
        Return 1
    End Function
End Class
    </file>
</compilation>
            Dim source2 =
<compilation>
    <file name="a.vb">
Imports System.Threading.Tasks
Class C
    Function A1() As Task(Of Boolean)
        Return Nothing
    End Function
    Function A2() As Task(Of Integer)
        Return Nothing
    End Function
    Function A3() As Task(Of C)
        Return Nothing
    End Function

    Async Function F() As Task(Of Integer) ' updated
        Await A1()
        Await A3()
        Return 1
    End Function
    Async Function G() As Task(Of Integer)
        Await A1()
        Return 2
    End Function
    Async Function H() As Task(Of Integer)
        Await A1()
        Return 1
    End Function
End Class
    </file>
</compilation>
            Dim source3 =
<compilation>
    <file name="a.vb">
Imports System.Threading.Tasks
Class C
    Function A1() As Task(Of Boolean)
        Return Nothing
    End Function
    Function A2() As Task(Of Integer)
        Return Nothing
    End Function
    Function A3() As Task(Of C)
        Return Nothing
    End Function

    Async Function F() As Task(Of Integer)
        Await A1()
        Await A3()
        Return 1
    End Function
    Async Function G() As Task(Of Integer) ' updated
        Await A3()
        Return 1
    End Function
    Async Function H() As Task(Of Integer) ' updated
        Await A3()
        Return 1
    End Function
End Class
    </file>
</compilation>

            ' Rude edit but the compiler should handle it
            Dim compilation0 = CreateCompilationWithReferences(source0, references:=LatestReferences, options:=ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            Dim compilation1 = compilation0.WithSource(source1)
            Dim compilation2 = compilation1.WithSource(source2)
            Dim compilation3 = compilation2.WithSource(source3)

            Dim f0 = compilation0.GetMember(Of MethodSymbol)("C.F")
            Dim f1 = compilation1.GetMember(Of MethodSymbol)("C.F")
            Dim f2 = compilation2.GetMember(Of MethodSymbol)("C.F")
            Dim f3 = compilation3.GetMember(Of MethodSymbol)("C.F")

            Dim g0 = compilation0.GetMember(Of MethodSymbol)("C.G")
            Dim g1 = compilation1.GetMember(Of MethodSymbol)("C.G")
            Dim g2 = compilation2.GetMember(Of MethodSymbol)("C.G")
            Dim g3 = compilation3.GetMember(Of MethodSymbol)("C.G")

            Dim h0 = compilation0.GetMember(Of MethodSymbol)("C.H")
            Dim h1 = compilation1.GetMember(Of MethodSymbol)("C.H")
            Dim h2 = compilation2.GetMember(Of MethodSymbol)("C.H")
            Dim h3 = compilation3.GetMember(Of MethodSymbol)("C.H")

            Dim v0 = CompileAndVerify(compilation:=compilation0, symbolValidator:=
                Sub([module] As ModuleSymbol)
                    Assert.Equal(
                    {
                        "$State: System.Int32",
                        "$Builder: System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of System.Int32)",
                        "$VB$Me: C",
                        "$A0: System.Runtime.CompilerServices.TaskAwaiter(Of System.Boolean)",
                        "$A1: System.Runtime.CompilerServices.TaskAwaiter(Of System.Int32)"
                    }, [module].GetFieldNamesAndTypes("C.VB$StateMachine_4_F"))
                End Sub)

            Dim md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)

            Dim generation0 = EmitBaseline.CreateInitialBaseline(md0, Function(handle) v0.CreateSymReader().GetEncMethodDebugInfo(handle))

            Dim diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(
                        New SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapByKind(f0, SyntaxKind.FunctionBlock), preserveLocalVariables:=True),
                        New SemanticEdit(SemanticEditKind.Update, g0, g1, GetSyntaxMapByKind(g0, SyntaxKind.FunctionBlock), preserveLocalVariables:=True)))

            diff1.VerifySynthesizedMembers(
                "C: {VB$StateMachine_4_F, VB$StateMachine_5_G}",
                "C.VB$StateMachine_4_F: {$State, $Builder, $VB$Me, $A2, $A1, MoveNext, System.Runtime.CompilerServices.IAsyncStateMachine.SetStateMachine}",
                "C.VB$StateMachine_5_G: {$State, $Builder, $VB$Me, $A0, MoveNext, System.Runtime.CompilerServices.IAsyncStateMachine.SetStateMachine}")

            Dim diff2 = compilation2.EmitDifference(
                    diff1.NextGeneration,
                    ImmutableArray.Create(
                        New SemanticEdit(SemanticEditKind.Update, f1, f2, GetSyntaxMapByKind(f1, SyntaxKind.FunctionBlock), preserveLocalVariables:=True)))

            diff2.VerifySynthesizedMembers(
                "C: {VB$StateMachine_4_F, VB$StateMachine_5_G}",
                "C.VB$StateMachine_4_F: {$State, $Builder, $VB$Me, $A3, $A2, MoveNext, System.Runtime.CompilerServices.IAsyncStateMachine.SetStateMachine, $A1}",
                "C.VB$StateMachine_5_G: {$State, $Builder, $VB$Me, $A0, MoveNext, System.Runtime.CompilerServices.IAsyncStateMachine.SetStateMachine}")

            Dim diff3 = compilation3.EmitDifference(
                    diff2.NextGeneration,
                    ImmutableArray.Create(
                        New SemanticEdit(SemanticEditKind.Update, g2, g3, GetSyntaxMapByKind(g2, SyntaxKind.FunctionBlock), preserveLocalVariables:=True),
                        New SemanticEdit(SemanticEditKind.Update, h2, h3, GetSyntaxMapByKind(h2, SyntaxKind.FunctionBlock), preserveLocalVariables:=True)))

            diff3.VerifySynthesizedMembers(
                "C: {VB$StateMachine_5_G, VB$StateMachine_6_H, VB$StateMachine_4_F}",
                "C.VB$StateMachine_4_F: {$State, $Builder, $VB$Me, $A3, $A2, MoveNext, System.Runtime.CompilerServices.IAsyncStateMachine.SetStateMachine, $A1}",
                "C.VB$StateMachine_5_G: {$State, $Builder, $VB$Me, $A1, MoveNext, System.Runtime.CompilerServices.IAsyncStateMachine.SetStateMachine, $A0}",
                "C.VB$StateMachine_6_H: {$State, $Builder, $VB$Me, $A1, MoveNext, System.Runtime.CompilerServices.IAsyncStateMachine.SetStateMachine}")

            ' Verify delta metadata contains expected rows.
            Dim md1 = diff1.GetMetadata()
            Dim md2 = diff2.GetMetadata()
            Dim md3 = diff3.GetMetadata()

            ' 1 field def added & 4 methods updated (MoveNext And kickoff for F And G)
            CheckEncLogDefinitions(md1.Reader,
                    Row(10, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                    Row(11, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                    Row(12, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                    Row(13, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                    Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                    Row(14, TableIndex.Field, EditAndContinueOperation.Default),
                    Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
                    Row(6, TableIndex.MethodDef, EditAndContinueOperation.Default),
                    Row(9, TableIndex.MethodDef, EditAndContinueOperation.Default),
                    Row(12, TableIndex.MethodDef, EditAndContinueOperation.Default),
                    Row(16, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(17, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(18, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(19, TableIndex.CustomAttribute, EditAndContinueOperation.Default))

            diff1.VerifyIL("C.VB$StateMachine_4_F.MoveNext()", "
{
  // Code size      317 (0x13d)
  .maxstack  3
  .locals init (Integer V_0,
                Integer V_1,
                System.Threading.Tasks.Task(Of Integer) V_2,
                System.Runtime.CompilerServices.TaskAwaiter(Of C) V_3,
                C.VB$StateMachine_4_F V_4,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_5,
                System.Exception V_6)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.VB$StateMachine_4_F.$State As Integer""
  IL_0006:  stloc.1
  .try
  {
    IL_0007:  ldloc.1
    IL_0008:  brfalse.s  IL_0012
    IL_000a:  br.s       IL_000c
    IL_000c:  ldloc.1
    IL_000d:  ldc.i4.1
    IL_000e:  beq.s      IL_0014
    IL_0010:  br.s       IL_0019
    IL_0012:  br.s       IL_005d
    IL_0014:  br         IL_00cd
    IL_0019:  nop
    IL_001a:  nop
    IL_001b:  ldarg.0
    IL_001c:  ldfld      ""C.VB$StateMachine_4_F.$VB$Me As C""
    IL_0021:  callvirt   ""Function C.A3() As System.Threading.Tasks.Task(Of C)""
    IL_0026:  callvirt   ""Function System.Threading.Tasks.Task(Of C).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of C)""
    IL_002b:  stloc.3
    IL_002c:  ldloca.s   V_3
    IL_002e:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter(Of C).get_IsCompleted() As Boolean""
    IL_0033:  brtrue.s   IL_007b
    IL_0035:  ldarg.0
    IL_0036:  ldc.i4.0
    IL_0037:  dup
    IL_0038:  stloc.1
    IL_0039:  stfld      ""C.VB$StateMachine_4_F.$State As Integer""
    IL_003e:  ldarg.0
    IL_003f:  ldloc.3
    IL_0040:  stfld      ""C.VB$StateMachine_4_F.$A2 As System.Runtime.CompilerServices.TaskAwaiter(Of C)""
    IL_0045:  ldarg.0
    IL_0046:  ldflda     ""C.VB$StateMachine_4_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
    IL_004b:  ldloca.s   V_3
    IL_004d:  ldarg.0
    IL_004e:  stloc.s    V_4
    IL_0050:  ldloca.s   V_4
    IL_0052:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of C), C.VB$StateMachine_4_F)(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of C), ByRef C.VB$StateMachine_4_F)""
    IL_0057:  nop
    IL_0058:  leave      IL_013c
    IL_005d:  ldarg.0
    IL_005e:  ldc.i4.m1
    IL_005f:  dup
    IL_0060:  stloc.1
    IL_0061:  stfld      ""C.VB$StateMachine_4_F.$State As Integer""
    IL_0066:  ldarg.0
    IL_0067:  ldfld      ""C.VB$StateMachine_4_F.$A2 As System.Runtime.CompilerServices.TaskAwaiter(Of C)""
    IL_006c:  stloc.3
    IL_006d:  ldarg.0
    IL_006e:  ldflda     ""C.VB$StateMachine_4_F.$A2 As System.Runtime.CompilerServices.TaskAwaiter(Of C)""
    IL_0073:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter(Of C)""
    IL_0079:  br.s       IL_007b
    IL_007b:  ldloca.s   V_3
    IL_007d:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter(Of C).GetResult() As C""
    IL_0082:  pop
    IL_0083:  ldloca.s   V_3
    IL_0085:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter(Of C)""
    IL_008b:  nop
    IL_008c:  ldarg.0
    IL_008d:  ldfld      ""C.VB$StateMachine_4_F.$VB$Me As C""
    IL_0092:  callvirt   ""Function C.A2() As System.Threading.Tasks.Task(Of Integer)""
    IL_0097:  callvirt   ""Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_009c:  stloc.s    V_5
    IL_009e:  ldloca.s   V_5
    IL_00a0:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean""
    IL_00a5:  brtrue.s   IL_00ec
    IL_00a7:  ldarg.0
    IL_00a8:  ldc.i4.1
    IL_00a9:  dup
    IL_00aa:  stloc.1
    IL_00ab:  stfld      ""C.VB$StateMachine_4_F.$State As Integer""
    IL_00b0:  ldarg.0
    IL_00b1:  ldloc.s    V_5
    IL_00b3:  stfld      ""C.VB$StateMachine_4_F.$A1 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_00b8:  ldarg.0
    IL_00b9:  ldflda     ""C.VB$StateMachine_4_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
    IL_00be:  ldloca.s   V_5
    IL_00c0:  ldarg.0
    IL_00c1:  stloc.s    V_4
    IL_00c3:  ldloca.s   V_4
    IL_00c5:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), C.VB$StateMachine_4_F)(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef C.VB$StateMachine_4_F)""
    IL_00ca:  nop
    IL_00cb:  leave.s    IL_013c
    IL_00cd:  ldarg.0
    IL_00ce:  ldc.i4.m1
    IL_00cf:  dup
    IL_00d0:  stloc.1
    IL_00d1:  stfld      ""C.VB$StateMachine_4_F.$State As Integer""
    IL_00d6:  ldarg.0
    IL_00d7:  ldfld      ""C.VB$StateMachine_4_F.$A1 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_00dc:  stloc.s    V_5
    IL_00de:  ldarg.0
    IL_00df:  ldflda     ""C.VB$StateMachine_4_F.$A1 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_00e4:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_00ea:  br.s       IL_00ec
    IL_00ec:  ldloca.s   V_5
    IL_00ee:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer""
    IL_00f3:  pop
    IL_00f4:  ldloca.s   V_5
    IL_00f6:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_00fc:  ldc.i4.1
    IL_00fd:  stloc.0
    IL_00fe:  leave.s    IL_0125
  }
  catch System.Exception
  {
    IL_0100:  dup
    IL_0101:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)""
    IL_0106:  stloc.s    V_6
    IL_0108:  ldarg.0
    IL_0109:  ldc.i4.s   -2
    IL_010b:  stfld      ""C.VB$StateMachine_4_F.$State As Integer""
    IL_0110:  ldarg.0
    IL_0111:  ldflda     ""C.VB$StateMachine_4_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
    IL_0116:  ldloc.s    V_6
    IL_0118:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetException(System.Exception)""
    IL_011d:  nop
    IL_011e:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()""
    IL_0123:  leave.s    IL_013c
  }
  IL_0125:  ldarg.0
  IL_0126:  ldc.i4.s   -2
  IL_0128:  dup
  IL_0129:  stloc.1
  IL_012a:  stfld      ""C.VB$StateMachine_4_F.$State As Integer""
  IL_012f:  ldarg.0
  IL_0130:  ldflda     ""C.VB$StateMachine_4_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
  IL_0135:  ldloc.0
  IL_0136:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetResult(Integer)""
  IL_013b:  nop
  IL_013c:  ret
}
")

            ' 1 field def added & 2 methods updated
            CheckEncLogDefinitions(md2.Reader,
                    Row(14, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                    Row(15, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                    Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                    Row(15, TableIndex.Field, EditAndContinueOperation.Default),
                    Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
                    Row(9, TableIndex.MethodDef, EditAndContinueOperation.Default),
                    Row(20, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(21, TableIndex.CustomAttribute, EditAndContinueOperation.Default))

            diff2.VerifyIL("C.VB$StateMachine_4_F.MoveNext()", "
{
  // Code size      317 (0x13d)
  .maxstack  3
  .locals init (Integer V_0,
                Integer V_1,
                System.Threading.Tasks.Task(Of Integer) V_2,
                System.Runtime.CompilerServices.TaskAwaiter(Of Boolean) V_3,
                C.VB$StateMachine_4_F V_4,
                System.Runtime.CompilerServices.TaskAwaiter(Of C) V_5,
                System.Exception V_6)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.VB$StateMachine_4_F.$State As Integer""
  IL_0006:  stloc.1
  .try
  {
    IL_0007:  ldloc.1
    IL_0008:  brfalse.s  IL_0012
    IL_000a:  br.s       IL_000c
    IL_000c:  ldloc.1
    IL_000d:  ldc.i4.1
    IL_000e:  beq.s      IL_0014
    IL_0010:  br.s       IL_0019
    IL_0012:  br.s       IL_005d
    IL_0014:  br         IL_00cd
    IL_0019:  nop
    IL_001a:  nop
    IL_001b:  ldarg.0
    IL_001c:  ldfld      ""C.VB$StateMachine_4_F.$VB$Me As C""
    IL_0021:  callvirt   ""Function C.A1() As System.Threading.Tasks.Task(Of Boolean)""
    IL_0026:  callvirt   ""Function System.Threading.Tasks.Task(Of Boolean).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Boolean)""
    IL_002b:  stloc.3
    IL_002c:  ldloca.s   V_3
    IL_002e:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter(Of Boolean).get_IsCompleted() As Boolean""
    IL_0033:  brtrue.s   IL_007b
    IL_0035:  ldarg.0
    IL_0036:  ldc.i4.0
    IL_0037:  dup
    IL_0038:  stloc.1
    IL_0039:  stfld      ""C.VB$StateMachine_4_F.$State As Integer""
    IL_003e:  ldarg.0
    IL_003f:  ldloc.3
    IL_0040:  stfld      ""C.VB$StateMachine_4_F.$A3 As System.Runtime.CompilerServices.TaskAwaiter(Of Boolean)""
    IL_0045:  ldarg.0
    IL_0046:  ldflda     ""C.VB$StateMachine_4_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
    IL_004b:  ldloca.s   V_3
    IL_004d:  ldarg.0
    IL_004e:  stloc.s    V_4
    IL_0050:  ldloca.s   V_4
    IL_0052:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Boolean), C.VB$StateMachine_4_F)(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Boolean), ByRef C.VB$StateMachine_4_F)""
    IL_0057:  nop
    IL_0058:  leave      IL_013c
    IL_005d:  ldarg.0
    IL_005e:  ldc.i4.m1
    IL_005f:  dup
    IL_0060:  stloc.1
    IL_0061:  stfld      ""C.VB$StateMachine_4_F.$State As Integer""
    IL_0066:  ldarg.0
    IL_0067:  ldfld      ""C.VB$StateMachine_4_F.$A3 As System.Runtime.CompilerServices.TaskAwaiter(Of Boolean)""
    IL_006c:  stloc.3
    IL_006d:  ldarg.0
    IL_006e:  ldflda     ""C.VB$StateMachine_4_F.$A3 As System.Runtime.CompilerServices.TaskAwaiter(Of Boolean)""
    IL_0073:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter(Of Boolean)""
    IL_0079:  br.s       IL_007b
    IL_007b:  ldloca.s   V_3
    IL_007d:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter(Of Boolean).GetResult() As Boolean""
    IL_0082:  pop
    IL_0083:  ldloca.s   V_3
    IL_0085:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter(Of Boolean)""
    IL_008b:  nop
    IL_008c:  ldarg.0
    IL_008d:  ldfld      ""C.VB$StateMachine_4_F.$VB$Me As C""
    IL_0092:  callvirt   ""Function C.A3() As System.Threading.Tasks.Task(Of C)""
    IL_0097:  callvirt   ""Function System.Threading.Tasks.Task(Of C).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of C)""
    IL_009c:  stloc.s    V_5
    IL_009e:  ldloca.s   V_5
    IL_00a0:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter(Of C).get_IsCompleted() As Boolean""
    IL_00a5:  brtrue.s   IL_00ec
    IL_00a7:  ldarg.0
    IL_00a8:  ldc.i4.1
    IL_00a9:  dup
    IL_00aa:  stloc.1
    IL_00ab:  stfld      ""C.VB$StateMachine_4_F.$State As Integer""
    IL_00b0:  ldarg.0
    IL_00b1:  ldloc.s    V_5
    IL_00b3:  stfld      ""C.VB$StateMachine_4_F.$A2 As System.Runtime.CompilerServices.TaskAwaiter(Of C)""
    IL_00b8:  ldarg.0
    IL_00b9:  ldflda     ""C.VB$StateMachine_4_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
    IL_00be:  ldloca.s   V_5
    IL_00c0:  ldarg.0
    IL_00c1:  stloc.s    V_4
    IL_00c3:  ldloca.s   V_4
    IL_00c5:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of C), C.VB$StateMachine_4_F)(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of C), ByRef C.VB$StateMachine_4_F)""
    IL_00ca:  nop
    IL_00cb:  leave.s    IL_013c
    IL_00cd:  ldarg.0
    IL_00ce:  ldc.i4.m1
    IL_00cf:  dup
    IL_00d0:  stloc.1
    IL_00d1:  stfld      ""C.VB$StateMachine_4_F.$State As Integer""
    IL_00d6:  ldarg.0
    IL_00d7:  ldfld      ""C.VB$StateMachine_4_F.$A2 As System.Runtime.CompilerServices.TaskAwaiter(Of C)""
    IL_00dc:  stloc.s    V_5
    IL_00de:  ldarg.0
    IL_00df:  ldflda     ""C.VB$StateMachine_4_F.$A2 As System.Runtime.CompilerServices.TaskAwaiter(Of C)""
    IL_00e4:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter(Of C)""
    IL_00ea:  br.s       IL_00ec
    IL_00ec:  ldloca.s   V_5
    IL_00ee:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter(Of C).GetResult() As C""
    IL_00f3:  pop
    IL_00f4:  ldloca.s   V_5
    IL_00f6:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter(Of C)""
    IL_00fc:  ldc.i4.1
    IL_00fd:  stloc.0
    IL_00fe:  leave.s    IL_0125
  }
  catch System.Exception
  {
    IL_0100:  dup
    IL_0101:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)""
    IL_0106:  stloc.s    V_6
    IL_0108:  ldarg.0
    IL_0109:  ldc.i4.s   -2
    IL_010b:  stfld      ""C.VB$StateMachine_4_F.$State As Integer""
    IL_0110:  ldarg.0
    IL_0111:  ldflda     ""C.VB$StateMachine_4_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
    IL_0116:  ldloc.s    V_6
    IL_0118:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetException(System.Exception)""
    IL_011d:  nop
    IL_011e:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()""
    IL_0123:  leave.s    IL_013c
  }
  IL_0125:  ldarg.0
  IL_0126:  ldc.i4.s   -2
  IL_0128:  dup
  IL_0129:  stloc.1
  IL_012a:  stfld      ""C.VB$StateMachine_4_F.$State As Integer""
  IL_012f:  ldarg.0
  IL_0130:  ldflda     ""C.VB$StateMachine_4_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
  IL_0135:  ldloc.0
  IL_0136:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetResult(Integer)""
  IL_013b:  nop
  IL_013c:  ret
}
")

            ' 2 field defs added - variables of G and H changed their types; 4 methods updated: G, H kickoff and MoveNext
            CheckEncLogDefinitions(md3.Reader,
                    Row(16, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                    Row(17, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                    Row(18, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                    Row(19, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                    Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                    Row(16, TableIndex.Field, EditAndContinueOperation.Default),
                    Row(5, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                    Row(17, TableIndex.Field, EditAndContinueOperation.Default),
                    Row(6, TableIndex.MethodDef, EditAndContinueOperation.Default),
                    Row(7, TableIndex.MethodDef, EditAndContinueOperation.Default),
                    Row(12, TableIndex.MethodDef, EditAndContinueOperation.Default),
                    Row(15, TableIndex.MethodDef, EditAndContinueOperation.Default),
                    Row(22, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(23, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(24, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(25, TableIndex.CustomAttribute, EditAndContinueOperation.Default))
        End Sub

        <Fact>
        Public Sub SynthesizedMembersMerging()
            Dim source0 =
<compilation>
    <file name="a.vb">
Imports System.Collections.Generic
Class C
End Class
    </file>
</compilation>
            Dim source1 =
<compilation>
    <file name="a.vb">
Imports System.Collections.Generic
Class C
    Iterator Function F() As IEnumerable(Of Integer)
        Yield 1
        Yield 2
    End Function
End Class
    </file>
</compilation>
            Dim source2 =
<compilation>
    <file name="a.vb">
Imports System.Collections.Generic
Class C
    Iterator Function F() As IEnumerable(Of Integer)
        Yield 1
        Yield 3
    End Function
End Class
    </file>
</compilation>
            Dim source3 =
<compilation>
    <file name="a.vb">
Imports System.Collections.Generic
Class C
    Iterator Function F() As IEnumerable(Of Integer)
        Yield 1
        Yield 3
    End Function
    Sub G()
        System.Console.WriteLine(1)
    End Sub
End Class
    </file>
</compilation>
            Dim source4 =
<compilation>
    <file name="a.vb">
Imports System.Collections.Generic
Class C
    Iterator Function F() As IEnumerable(Of Integer)
        Yield 1
        Yield 3
    End Function
    Sub G()
        System.Console.WriteLine(1)
    End Sub
    Iterator Function H() As IEnumerable(Of Integer)
        Yield 1
    End Function
End Class
    </file>
</compilation>

            ' Rude edit but the compiler should handle it.
            Dim compilation0 = CreateCompilationWithReferences(source0, references:=LatestReferences, options:=ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            Dim compilation1 = compilation0.WithSource(source1)
            Dim compilation2 = compilation1.WithSource(source2)
            Dim compilation3 = compilation2.WithSource(source3)
            Dim compilation4 = compilation3.WithSource(source4)

            Dim f1 = compilation1.GetMember(Of MethodSymbol)("C.F")
            Dim f2 = compilation2.GetMember(Of MethodSymbol)("C.F")
            Dim f3 = compilation3.GetMember(Of MethodSymbol)("C.F")

            Dim g3 = compilation3.GetMember(Of MethodSymbol)("C.G")
            Dim h4 = compilation4.GetMember(Of MethodSymbol)("C.H")

            Dim v0 = CompileAndVerify(compilation:=compilation0)
            Dim md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)

            Dim generation0 = EmitBaseline.CreateInitialBaseline(md0, Function(handle) v0.CreateSymReader().GetEncMethodDebugInfo(handle))

            Dim diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(
                        New SemanticEdit(SemanticEditKind.Insert, Nothing, f1)))

            diff1.VerifySynthesizedMembers(
                "C: {VB$StateMachine_1#1_F}",
                "C.VB$StateMachine_1#1_F: {$State, $Current, $InitialThreadId, $VB$Me, Dispose, MoveNext, GetEnumerator, IEnumerable.GetEnumerator, get_Current, Reset, IEnumerator.get_Current, Current, IEnumerator.Current}")

            Dim diff2 = compilation2.EmitDifference(
                    diff1.NextGeneration,
                    ImmutableArray.Create(
                        New SemanticEdit(SemanticEditKind.Update, f1, f2, GetSyntaxMapByKind(f1, SyntaxKind.FunctionBlock), preserveLocalVariables:=True)))

            diff2.VerifySynthesizedMembers(
                "C: {VB$StateMachine_1#1_F}",
                "C.VB$StateMachine_1#1_F: {$State, $Current, $InitialThreadId, $VB$Me, Dispose, MoveNext, GetEnumerator, IEnumerable.GetEnumerator, get_Current, Reset, IEnumerator.get_Current, Current, IEnumerator.Current}")

            Dim diff3 = compilation3.EmitDifference(
                    diff2.NextGeneration,
                    ImmutableArray.Create(
                        New SemanticEdit(SemanticEditKind.Insert, Nothing, g3)))

            diff3.VerifySynthesizedMembers(
                "C: {VB$StateMachine_1#1_F}",
                "C.VB$StateMachine_1#1_F: {$State, $Current, $InitialThreadId, $VB$Me, Dispose, MoveNext, GetEnumerator, IEnumerable.GetEnumerator, get_Current, Reset, IEnumerator.get_Current, Current, IEnumerator.Current}")

            Dim diff4 = compilation4.EmitDifference(
                    diff3.NextGeneration,
                    ImmutableArray.Create(
                        New SemanticEdit(SemanticEditKind.Insert, Nothing, h4)))

            diff4.VerifySynthesizedMembers(
                "C: {VB$StateMachine_3#4_H, VB$StateMachine_1#1_F}",
                "C.VB$StateMachine_1#1_F: {$State, $Current, $InitialThreadId, $VB$Me, Dispose, MoveNext, GetEnumerator, IEnumerable.GetEnumerator, get_Current, Reset, IEnumerator.get_Current, Current, IEnumerator.Current}",
                "C.VB$StateMachine_3#4_H: {$State, $Current, $InitialThreadId, $VB$Me, Dispose, MoveNext, GetEnumerator, IEnumerable.GetEnumerator, get_Current, Reset, IEnumerator.get_Current, Current, IEnumerator.Current}")
        End Sub

        <Fact>
        Public Sub UpdateAsyncLambda()
            Dim source0 = MarkedSource("
Imports System
Imports System.Threading.Tasks

Class C
    Shared Sub F()
        Dim <N:0>g1</N:0> = <N:1>Async Function()
                                    Await A1()
                                    Await A2()
                                 End Function</N:1>
    End Sub

    Shared Function A1() As Task(Of Boolean)
        Return Nothing
    End Function

    Shared Function A2() As Task(Of Integer)
        Return Nothing
    End Function

    Shared Function A3() As Task(Of Double)
        Return Nothing
    End Function
End Class
")
            Dim source1 = MarkedSource("
Imports System
Imports System.Threading.Tasks

Class C
    Shared Sub F()
        Dim <N:0>g1</N:0> = <N:1>Async Function()
                                    Await A2()
                                    Await A1()
                                 End Function</N:1>
    End Sub

    Shared Function A1() As Task(Of Boolean)
        Return Nothing
    End Function

    Shared Function A2() As Task(Of Integer)
        Return Nothing
    End Function

    Shared Function A3() As Task(Of Double)
        Return Nothing
    End Function
End Class
")
            Dim source2 = MarkedSource("
Imports System
Imports System.Threading.Tasks

Class C
    Shared Sub F()
        Dim <N:0>g1</N:0> = <N:1>Async Function()
                                    Await A1()
                                    Await A2()
                                 End Function</N:1>
    End Sub

    Shared Function A1() As Task(Of Boolean)
        Return Nothing
    End Function

    Shared Function A2() As Task(Of Integer)
        Return Nothing
    End Function

    Shared Function A3() As Task(Of Double)
        Return Nothing
    End Function
End Class")

            Dim compilation0 = CreateCompilationWithMscorlib45AndVBRuntime({source0.Tree}, options:=ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            Dim compilation1 = compilation0.WithSource(source1.Tree)
            Dim compilation2 = compilation1.WithSource(source2.Tree)

            Dim v0 = CompileAndVerify(compilation0, symbolValidator:=
                Sub([module])
                    Assert.Equal(
                    {
                         "$State: System.Int32",
                         "$Builder: System.Runtime.CompilerServices.AsyncTaskMethodBuilder",
                         "$VB$NonLocal__Closure$__: C._Closure$__",
                         "$A0: System.Runtime.CompilerServices.TaskAwaiter(Of System.Boolean)",
                         "$A1: System.Runtime.CompilerServices.TaskAwaiter(Of System.Int32)"
                    }, [module].GetFieldNamesAndTypes("C._Closure$__.VB$StateMachine___Lambda$__1-0"))
                End Sub)

            Dim md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)
            Dim generation0 = EmitBaseline.CreateInitialBaseline(md0, AddressOf v0.CreateSymReader().GetEncMethodDebugInfo)

            Dim f0 = compilation0.GetMember(Of MethodSymbol)("C.F")
            Dim f1 = compilation1.GetMember(Of MethodSymbol)("C.F")
            Dim f2 = compilation2.GetMember(Of MethodSymbol)("C.F")

            Dim diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    New SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables:=True)))

            ' note that the types of the awaiter fields $A0, $A1 are the same as in the previous generation
            diff1.VerifySynthesizedFields("C._Closure$__.VB$StateMachine___Lambda$__1-0",
                "$State: Integer",
                "$Builder: System.Runtime.CompilerServices.AsyncTaskMethodBuilder",
                "$VB$NonLocal__Closure$__: C._Closure$__",
                "$A0: System.Runtime.CompilerServices.TaskAwaiter(Of Boolean)",
                "$A1: System.Runtime.CompilerServices.TaskAwaiter(Of Integer)")

            Dim diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    New SemanticEdit(SemanticEditKind.Update, f1, f2, GetSyntaxMapFromMarkers(source1, source2), preserveLocalVariables:=True)))

            ' note that the types of the awaiter fields $A0, $A1 are the same as in the previous generation
            diff2.VerifySynthesizedFields("C._Closure$__.VB$StateMachine___Lambda$__1-0",
                "$State: Integer",
                "$Builder: System.Runtime.CompilerServices.AsyncTaskMethodBuilder",
                "$VB$NonLocal__Closure$__: C._Closure$__",
                "$A0: System.Runtime.CompilerServices.TaskAwaiter(Of Boolean)",
                "$A1: System.Runtime.CompilerServices.TaskAwaiter(Of Integer)")

        End Sub
    End Class
End Namespace
