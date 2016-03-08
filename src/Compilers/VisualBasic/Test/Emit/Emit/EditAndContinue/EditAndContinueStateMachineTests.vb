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

            Dim compilation0 = CompilationUtils.CreateCompilationWithReferences(source0, references:=LatestVbReferences, options:=TestOptions.DebugDll)
            Dim compilation1 = CompilationUtils.CreateCompilationWithReferences(source1, references:=LatestVbReferences, options:=TestOptions.DebugDll)

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

            Dim compilation0 = CompilationUtils.CreateCompilationWithReferences(source0, references:=LatestVbReferences, options:=TestOptions.DebugDll)
            Dim compilation1 = CompilationUtils.CreateCompilationWithReferences(source1, references:=LatestVbReferences, options:=TestOptions.DebugDll)

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
                            Row(23, TableIndex.MemberRef, EditAndContinueOperation.Default),
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
                            Row(18, TableIndex.TypeRef, EditAndContinueOperation.Default),
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
                            Row(8, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
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
                            Handle(18, TableIndex.TypeRef),
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
                            Handle(23, TableIndex.MemberRef),
                            Handle(4, TableIndex.CustomAttribute),
                            Handle(5, TableIndex.CustomAttribute),
                            Handle(6, TableIndex.CustomAttribute),
                            Handle(7, TableIndex.CustomAttribute),
                            Handle(8, TableIndex.CustomAttribute),
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
  IL_0015:  call       0x0A00000B
  IL_001a:  stfld      0x04000002
  IL_001f:  ldloc.0
  IL_0020:  ldflda     0x04000002
  IL_0025:  ldloca.s   V_0
  IL_0027:  call       0x2B000001
  IL_002c:  ldloc.0
  IL_002d:  ldflda     0x04000002
  IL_0032:  call       0x0A00000D
  IL_0037:  ret
}
{
  // Code size        7 (0x7)
  .maxstack  8
  IL_0000:  ldarg.0
  IL_0001:  call       0x0A00000E
  IL_0006:  ret
}
{
  // Code size      184 (0xb8)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldfld      0x04000001
  IL_0006:  stloc.1
  IL_0007:  ldloc.1
  IL_0008:  brfalse.s  IL_000c
  IL_000a:  br.s       IL_000e
  IL_000c:  br.s       IL_0049
  IL_000e:  nop
  IL_000f:  ldc.i4.1
  IL_0010:  call       0x2B000002
  IL_0015:  callvirt   0x0A000010
  IL_001a:  stloc.3
  IL_001b:  ldloca.s   V_3
  IL_001d:  call       0x0A000011
  IL_0022:  brtrue.s   IL_0067
  IL_0024:  ldarg.0
  IL_0025:  ldc.i4.0
  IL_0026:  dup
  IL_0027:  stloc.1
  IL_0028:  stfld      0x04000001
  IL_002d:  ldarg.0
  IL_002e:  ldloc.3
  IL_002f:  stfld      0x04000004
  IL_0034:  ldarg.0
  IL_0035:  ldflda     0x04000002
  IL_003a:  ldloca.s   V_3
  IL_003c:  ldarg.0
  IL_003d:  stloc.s    V_4
  IL_003f:  ldloca.s   V_4
  IL_0041:  call       0x2B000003
  IL_0046:  nop
  IL_0047:  leave.s    IL_00b7
  IL_0049:  ldarg.0
  IL_004a:  ldc.i4.m1
  IL_004b:  dup
  IL_004c:  stloc.1
  IL_004d:  stfld      0x04000001
  IL_0052:  ldarg.0
  IL_0053:  ldfld      0x04000004
  IL_0058:  stloc.3
  IL_0059:  ldarg.0
  IL_005a:  ldflda     0x04000004
  IL_005f:  initobj    0x1B000003
  IL_0065:  br.s       IL_0067
  IL_0067:  ldloca.s   V_3
  IL_0069:  call       0x0A000013
  IL_006e:  pop
  IL_006f:  ldloca.s   V_3
  IL_0071:  initobj    0x1B000003
  IL_0077:  ldc.i4.0
  IL_0078:  stloc.0
  IL_0079:  leave.s    IL_00a0
  IL_007b:  dup
  IL_007c:  call       0x0A000014
  IL_0081:  stloc.s    V_5
  IL_0083:  ldarg.0
  IL_0084:  ldc.i4.s   -2
  IL_0086:  stfld      0x04000001
  IL_008b:  ldarg.0
  IL_008c:  ldflda     0x04000002
  IL_0091:  ldloc.s    V_5
  IL_0093:  call       0x0A000015
  IL_0098:  nop
  IL_0099:  call       0x0A000016
  IL_009e:  leave.s    IL_00b7
  IL_00a0:  ldarg.0
  IL_00a1:  ldc.i4.s   -2
  IL_00a3:  dup
  IL_00a4:  stloc.1
  IL_00a5:  stfld      0x04000001
  IL_00aa:  ldarg.0
  IL_00ab:  ldflda     0x04000002
  IL_00b0:  ldloc.0
  IL_00b1:  call       0x0A000017
  IL_00b6:  nop
  IL_00b7:  ret
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
                <entry offset="0x1b" hidden="true" document="1"/>
                <entry offset="0x77" startLine="5" startColumn="9" endLine="5" endColumn="17" document="1"/>
                <entry offset="0x7b" hidden="true" document="1"/>
                <entry offset="0x83" hidden="true" document="1"/>
                <entry offset="0xa0" startLine="6" startColumn="5" endLine="6" endColumn="17" document="1"/>
                <entry offset="0xaa" hidden="true" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xb8">
                <namespace name="System.Threading.Tasks" importlevel="file"/>
                <currentnamespace name=""/>
            </scope>
            <asyncInfo>
                <kickoffMethod token="0x6000002"/>
                <await yield="0x2d" resume="0x49" token="0x6000004"/>
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

            Dim compilation0 = CompilationUtils.CreateCompilationWithReferences(source0, references:=LatestVbReferences, options:=TestOptions.DebugDll)
            Dim compilation1 = CompilationUtils.CreateCompilationWithReferences(source1, references:=LatestVbReferences, options:=TestOptions.DebugDll)

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

            Dim compilation0 = CompilationUtils.CreateCompilationWithReferences(source0, references:=LatestVbReferences, options:=TestOptions.DebugDll)
            Dim compilation1 = CompilationUtils.CreateCompilationWithReferences(source1, references:=LatestVbReferences, options:=TestOptions.DebugDll)

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
                                        Row(8, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
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

            Dim compilation0 = CompilationUtils.CreateCompilationWithReferences(source0, references:=LatestVbReferences, options:=TestOptions.DebugDll)
            Dim compilation1 = CompilationUtils.CreateCompilationWithReferences(source1, references:=LatestVbReferences, options:=TestOptions.DebugDll)

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

            Dim compilation0 = CompilationUtils.CreateCompilationWithReferences(source0, references:=LatestVbReferences, options:=TestOptions.DebugDll)
            Dim compilation1 = CompilationUtils.CreateCompilationWithReferences(source1, references:=LatestVbReferences, options:=TestOptions.DebugDll)

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
End Class
    </file>
</compilation>

            Dim compilation0 = CompilationUtils.CreateCompilationWithReferences(source0, references:=LatestVbReferences, options:=TestOptions.DebugDll)
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
                        Row(6, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(9, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(12, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(19, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(20, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(21, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(22, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(23, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(24, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(25, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(26, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(27, TableIndex.CustomAttribute, EditAndContinueOperation.Default))
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

            Dim compilation0 = CompilationUtils.CreateCompilationWithReferences(source0, references:=LatestVbReferences, options:=TestOptions.DebugDll)
            Dim compilation1 = CompilationUtils.CreateCompilationWithReferences(source1, references:=LatestVbReferences, options:=TestOptions.DebugDll)

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

            Dim compilation0 = CompilationUtils.CreateCompilationWithReferences(source0, references:=LatestVbReferences, options:=TestOptions.DebugDll)
            Dim compilation1 = CompilationUtils.CreateCompilationWithReferences(source1, references:=LatestVbReferences, options:=TestOptions.DebugDll)

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
                        Row(9, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(10, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(11, TableIndex.CustomAttribute, EditAndContinueOperation.Default))
                End Using

                diff1.VerifyIL("C.VB$StateMachine_1_F.MoveNext()", "
{
  // Code size      186 (0xba)
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
    IL_000f:  ldc.i4.s   10
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
    IL_0048:  leave.s    IL_00b9
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
    IL_0078:  ldc.i4.s   20
    IL_007a:  stloc.0
    IL_007b:  leave.s    IL_00a2
  }
  catch System.Exception
  {
    IL_007d:  dup
    IL_007e:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)""
    IL_0083:  stloc.s    V_5
    IL_0085:  ldarg.0
    IL_0086:  ldc.i4.s   -2
    IL_0088:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
    IL_008d:  ldarg.0
    IL_008e:  ldflda     ""C.VB$StateMachine_1_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
    IL_0093:  ldloc.s    V_5
    IL_0095:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetException(System.Exception)""
    IL_009a:  nop
    IL_009b:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()""
    IL_00a0:  leave.s    IL_00b9
  }
  IL_00a2:  ldarg.0
  IL_00a3:  ldc.i4.s   -2
  IL_00a5:  dup
  IL_00a6:  stloc.1
  IL_00a7:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_00ac:  ldarg.0
  IL_00ad:  ldflda     ""C.VB$StateMachine_1_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
  IL_00b2:  ldloc.0
  IL_00b3:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetResult(Integer)""
  IL_00b8:  nop
  IL_00b9:  ret
}
")
                v0.VerifyIL("C.VB$StateMachine_1_F.MoveNext()", "
{
  // Code size      184 (0xb8)
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
    IL_000c:  br.s       IL_0049
    IL_000e:  nop
    IL_000f:  ldc.i4.1
    IL_0010:  call       ""Function System.Threading.Tasks.Task.FromResult(Of Integer)(Integer) As System.Threading.Tasks.Task(Of Integer)""
    IL_0015:  callvirt   ""Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_001a:  stloc.3
    IL_001b:  ldloca.s   V_3
    IL_001d:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean""
    IL_0022:  brtrue.s   IL_0067
    IL_0024:  ldarg.0
    IL_0025:  ldc.i4.0
    IL_0026:  dup
    IL_0027:  stloc.1
    IL_0028:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
    IL_002d:  ldarg.0
    IL_002e:  ldloc.3
    IL_002f:  stfld      ""C.VB$StateMachine_1_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_0034:  ldarg.0
    IL_0035:  ldflda     ""C.VB$StateMachine_1_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
    IL_003a:  ldloca.s   V_3
    IL_003c:  ldarg.0
    IL_003d:  stloc.s    V_4
    IL_003f:  ldloca.s   V_4
    IL_0041:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), C.VB$StateMachine_1_F)(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef C.VB$StateMachine_1_F)""
    IL_0046:  nop
    IL_0047:  leave.s    IL_00b7
    IL_0049:  ldarg.0
    IL_004a:  ldc.i4.m1
    IL_004b:  dup
    IL_004c:  stloc.1
    IL_004d:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
    IL_0052:  ldarg.0
    IL_0053:  ldfld      ""C.VB$StateMachine_1_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_0058:  stloc.3
    IL_0059:  ldarg.0
    IL_005a:  ldflda     ""C.VB$StateMachine_1_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_005f:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_0065:  br.s       IL_0067
    IL_0067:  ldloca.s   V_3
    IL_0069:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer""
    IL_006e:  pop
    IL_006f:  ldloca.s   V_3
    IL_0071:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_0077:  ldc.i4.2
    IL_0078:  stloc.0
    IL_0079:  leave.s    IL_00a0
  }
  catch System.Exception
  {
    IL_007b:  dup
    IL_007c:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)""
    IL_0081:  stloc.s    V_5
    IL_0083:  ldarg.0
    IL_0084:  ldc.i4.s   -2
    IL_0086:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
    IL_008b:  ldarg.0
    IL_008c:  ldflda     ""C.VB$StateMachine_1_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
    IL_0091:  ldloc.s    V_5
    IL_0093:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetException(System.Exception)""
    IL_0098:  nop
    IL_0099:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()""
    IL_009e:  leave.s    IL_00b7
  }
  IL_00a0:  ldarg.0
  IL_00a1:  ldc.i4.s   -2
  IL_00a3:  dup
  IL_00a4:  stloc.1
  IL_00a5:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_00aa:  ldarg.0
  IL_00ab:  ldflda     ""C.VB$StateMachine_1_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
  IL_00b0:  ldloc.0
  IL_00b1:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetResult(Integer)""
  IL_00b6:  nop
  IL_00b7:  ret
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
End Class
    </file>
</compilation>

            Dim compilation0 = CompilationUtils.CreateCompilationWithReferences(source0, references:=LatestVbReferences, options:=ComSafeDebugDll)
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
                AssertEx.Equal(Of Integer)({&H06000002UI, &H06000005UI}, diff1.UpdatedMethods.Select(Function(m) MetadataTokens.GetToken(m)))

                ' verify delta metadata contains expected rows
                Using md1 = diff1.GetMetadata()
                    '  Verify that no new TypeDefs, FieldDefs or MethodDefs were added and 3 methods were updated:
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
End Class
    </file>
</compilation>

            Dim compilation0 = CompilationUtils.CreateCompilationWithReferences(source0, references:=LatestVbReferences, options:=ComSafeDebugDll)
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
                AssertEx.Equal(Of Integer)({&H06000002UI, &H06000005UI}, diff1.UpdatedMethods.Select(Function(m) MetadataTokens.GetToken(m)))

                ' verify delta metadata contains expected rows
                Using md1 = diff1.GetMetadata()
                    ' 1 field added & 3 methods updated
                    CheckEncLogDefinitions(md1.Reader,
                        Row(3, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(4, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                        Row(8, TableIndex.Field, EditAndContinueOperation.Default),
                        Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
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
End Class
    </file>
</compilation>

            Dim compilation0 = CompilationUtils.CreateCompilationWithReferences(source0, references:=LatestVbReferences, options:=ComSafeDebugDll)
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
                AssertEx.Equal(Of Integer)({&H06000002UI, &H06000005UI}, diff1.UpdatedMethods.Select(Function(m) MetadataTokens.GetToken(m)))

                ' verify delta metadata contains expected rows
                Using md1 = diff1.GetMetadata()
                    ' 1 field added & 3 methods updated
                    CheckEncLogDefinitions(md1.Reader,
                        Row(3, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(4, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                        Row(8, TableIndex.Field, EditAndContinueOperation.Default),
                        Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
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
End Class
    </file>
</compilation>

            Dim compilation0 = CompilationUtils.CreateCompilationWithReferences(source0, references:=LatestVbReferences, options:=ComSafeDebugDll)
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
                AssertEx.Equal(Of Integer)({&H06000002UI, &H06000005UI}, diff1.UpdatedMethods.Select(Function(m) MetadataTokens.GetToken(m)))

                ' verify delta metadata contains expected rows
                Using md1 = diff1.GetMetadata()
                    ' 1 field def added & 3 methods updated
                    CheckEncLogDefinitions(md1.Reader,
                        Row(3, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(4, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                        Row(6, TableIndex.Field, EditAndContinueOperation.Default),
                        Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
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
End Class
    </file>
</compilation>

            ' Rude edit but the compiler should handle it

            Dim compilation0 = CompilationUtils.CreateCompilationWithReferences(source0, references:=LatestVbReferences, options:=ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
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
                AssertEx.Equal(Of Integer)({&H06000002UI, &H06000005UI}, diff1.UpdatedMethods.Select(Function(m) MetadataTokens.GetToken(m)))

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
                        Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
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
End Class
    </file>
</compilation>

            Dim compilation0 = CompilationUtils.CreateCompilationWithReferences(source0, references:=LatestVbReferences, options:=ComSafeDebugDll)
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
                AssertEx.Equal(Of Integer)({&H06000004UI}, diff1.UpdatedMethods.Select(Function(m) MetadataTokens.GetToken(m)))

                ' verify delta metadata contains expected rows
                Using md1 = diff1.GetMetadata()
                    CheckEncLogDefinitions(md1.Reader,
                        Row(3, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(4, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(9, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(10, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(11, TableIndex.CustomAttribute, EditAndContinueOperation.Default))
                End Using

                diff1.VerifyIL("C.VB$StateMachine_1_F.MoveNext()", "
{
  // Code size      199 (0xc7)
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
    IL_000c:  br.s       IL_0056
    IL_000e:  nop
    IL_000f:  ldarg.0
    IL_0010:  ldarg.0
    IL_0011:  ldfld      ""C.VB$StateMachine_1_F.$VB$Local_p As Integer""
    IL_0016:  stfld      ""C.VB$StateMachine_1_F.$VB$ResumableLocal_x$0 As Integer""
    IL_001b:  ldc.i4.s   20
    IL_001d:  call       ""Function System.Threading.Tasks.Task.FromResult(Of Integer)(Integer) As System.Threading.Tasks.Task(Of Integer)""
    IL_0022:  callvirt   ""Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_0027:  stloc.3
    IL_0028:  ldloca.s   V_3
    IL_002a:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean""
    IL_002f:  brtrue.s   IL_0074
    IL_0031:  ldarg.0
    IL_0032:  ldc.i4.0
    IL_0033:  dup
    IL_0034:  stloc.1
    IL_0035:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
    IL_003a:  ldarg.0
    IL_003b:  ldloc.3
    IL_003c:  stfld      ""C.VB$StateMachine_1_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_0041:  ldarg.0
    IL_0042:  ldflda     ""C.VB$StateMachine_1_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
    IL_0047:  ldloca.s   V_3
    IL_0049:  ldarg.0
    IL_004a:  stloc.s    V_4
    IL_004c:  ldloca.s   V_4
    IL_004e:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), C.VB$StateMachine_1_F)(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef C.VB$StateMachine_1_F)""
    IL_0053:  nop
    IL_0054:  leave.s    IL_00c6
    IL_0056:  ldarg.0
    IL_0057:  ldc.i4.m1
    IL_0058:  dup
    IL_0059:  stloc.1
    IL_005a:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
    IL_005f:  ldarg.0
    IL_0060:  ldfld      ""C.VB$StateMachine_1_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_0065:  stloc.3
    IL_0066:  ldarg.0
    IL_0067:  ldflda     ""C.VB$StateMachine_1_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_006c:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_0072:  br.s       IL_0074
    IL_0074:  ldloca.s   V_3
    IL_0076:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer""
    IL_007b:  stloc.s    V_5
    IL_007d:  ldloca.s   V_3
    IL_007f:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_0085:  ldloc.s    V_5
    IL_0087:  stloc.0
    IL_0088:  leave.s    IL_00af
  }
  catch System.Exception
  {
    IL_008a:  dup
    IL_008b:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)""
    IL_0090:  stloc.s    V_6
    IL_0092:  ldarg.0
    IL_0093:  ldc.i4.s   -2
    IL_0095:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
    IL_009a:  ldarg.0
    IL_009b:  ldflda     ""C.VB$StateMachine_1_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
    IL_00a0:  ldloc.s    V_6
    IL_00a2:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetException(System.Exception)""
    IL_00a7:  nop
    IL_00a8:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()""
    IL_00ad:  leave.s    IL_00c6
  }
  IL_00af:  ldarg.0
  IL_00b0:  ldc.i4.s   -2
  IL_00b2:  dup
  IL_00b3:  stloc.1
  IL_00b4:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_00b9:  ldarg.0
  IL_00ba:  ldflda     ""C.VB$StateMachine_1_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
  IL_00bf:  ldloc.0
  IL_00c0:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetResult(Integer)""
  IL_00c5:  nop
  IL_00c6:  ret
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
End Class
    </file>
</compilation>

            Dim compilation0 = CompilationUtils.CreateCompilationWithReferences(source0, references:=LatestVbReferences, options:=ComSafeDebugDll)
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
                AssertEx.Equal(Of Integer)({&H06000004UI}, diff1.UpdatedMethods.Select(Function(m) MetadataTokens.GetToken(m)))

                ' verify delta metadata contains expected rows
                Using md1 = diff1.GetMetadata()
                    CheckEncLogDefinitions(md1.Reader,
                        Row(3, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(4, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                        Row(7, TableIndex.Field, EditAndContinueOperation.Default),
                        Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(9, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(10, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(11, TableIndex.CustomAttribute, EditAndContinueOperation.Default))
                End Using

                diff1.VerifyIL("C.VB$StateMachine_1_F.MoveNext()", "
{
  // Code size      211 (0xd3)
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
    IL_000c:  br.s       IL_0062
    IL_000e:  nop
    IL_000f:  ldarg.0
    IL_0010:  ldarg.0
    IL_0011:  ldfld      ""C.VB$StateMachine_1_F.$VB$Local_p As Integer""
    IL_0016:  stfld      ""C.VB$StateMachine_1_F.$VB$ResumableLocal_x$0 As Integer""
    IL_001b:  ldarg.0
    IL_001c:  ldc.i4.s   10
    IL_001e:  stfld      ""C.VB$StateMachine_1_F.$VB$ResumableLocal_y$1 As Integer""
    IL_0023:  ldarg.0
    IL_0024:  ldfld      ""C.VB$StateMachine_1_F.$VB$ResumableLocal_y$1 As Integer""
    IL_0029:  call       ""Function System.Threading.Tasks.Task.FromResult(Of Integer)(Integer) As System.Threading.Tasks.Task(Of Integer)""
    IL_002e:  callvirt   ""Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_0033:  stloc.3
    IL_0034:  ldloca.s   V_3
    IL_0036:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean""
    IL_003b:  brtrue.s   IL_0080
    IL_003d:  ldarg.0
    IL_003e:  ldc.i4.0
    IL_003f:  dup
    IL_0040:  stloc.1
    IL_0041:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
    IL_0046:  ldarg.0
    IL_0047:  ldloc.3
    IL_0048:  stfld      ""C.VB$StateMachine_1_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_004d:  ldarg.0
    IL_004e:  ldflda     ""C.VB$StateMachine_1_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
    IL_0053:  ldloca.s   V_3
    IL_0055:  ldarg.0
    IL_0056:  stloc.s    V_4
    IL_0058:  ldloca.s   V_4
    IL_005a:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), C.VB$StateMachine_1_F)(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef C.VB$StateMachine_1_F)""
    IL_005f:  nop
    IL_0060:  leave.s    IL_00d2
    IL_0062:  ldarg.0
    IL_0063:  ldc.i4.m1
    IL_0064:  dup
    IL_0065:  stloc.1
    IL_0066:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
    IL_006b:  ldarg.0
    IL_006c:  ldfld      ""C.VB$StateMachine_1_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_0071:  stloc.3
    IL_0072:  ldarg.0
    IL_0073:  ldflda     ""C.VB$StateMachine_1_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_0078:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_007e:  br.s       IL_0080
    IL_0080:  ldloca.s   V_3
    IL_0082:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer""
    IL_0087:  stloc.s    V_5
    IL_0089:  ldloca.s   V_3
    IL_008b:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_0091:  ldloc.s    V_5
    IL_0093:  stloc.0
    IL_0094:  leave.s    IL_00bb
  }
  catch System.Exception
  {
    IL_0096:  dup
    IL_0097:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)""
    IL_009c:  stloc.s    V_6
    IL_009e:  ldarg.0
    IL_009f:  ldc.i4.s   -2
    IL_00a1:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
    IL_00a6:  ldarg.0
    IL_00a7:  ldflda     ""C.VB$StateMachine_1_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
    IL_00ac:  ldloc.s    V_6
    IL_00ae:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetException(System.Exception)""
    IL_00b3:  nop
    IL_00b4:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()""
    IL_00b9:  leave.s    IL_00d2
  }
  IL_00bb:  ldarg.0
  IL_00bc:  ldc.i4.s   -2
  IL_00be:  dup
  IL_00bf:  stloc.1
  IL_00c0:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_00c5:  ldarg.0
  IL_00c6:  ldflda     ""C.VB$StateMachine_1_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
  IL_00cb:  ldloc.0
  IL_00cc:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetResult(Integer)""
  IL_00d1:  nop
  IL_00d2:  ret
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
End Class
    </file>
</compilation>

            Dim compilation0 = CompilationUtils.CreateCompilationWithReferences(source0, references:=LatestVbReferences, options:=ComSafeDebugDll)
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
                AssertEx.Equal(Of Integer)({&H06000004UI}, diff1.UpdatedMethods.Select(Function(m) MetadataTokens.GetToken(m)))

                ' verify delta metadata contains expected rows
                Using md1 = diff1.GetMetadata()
                    CheckEncLogDefinitions(md1.Reader,
                        Row(3, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(4, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                        Row(7, TableIndex.Field, EditAndContinueOperation.Default),
                        Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(9, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(10, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(11, TableIndex.CustomAttribute, EditAndContinueOperation.Default))
                End Using

                diff1.VerifyIL("C.VB$StateMachine_1_F.MoveNext()", "
{
  // Code size      202 (0xca)
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
    IL_000c:  br.s       IL_0059
    IL_000e:  nop
    IL_000f:  ldarg.0
    IL_0010:  ldc.i4     0x4d2
    IL_0015:  stfld      ""C.VB$StateMachine_1_F.$VB$ResumableLocal_y$1 As Integer""
    IL_001a:  ldarg.0
    IL_001b:  ldfld      ""C.VB$StateMachine_1_F.$VB$Local_p As Integer""
    IL_0020:  call       ""Function System.Threading.Tasks.Task.FromResult(Of Integer)(Integer) As System.Threading.Tasks.Task(Of Integer)""
    IL_0025:  callvirt   ""Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_002a:  stloc.3
    IL_002b:  ldloca.s   V_3
    IL_002d:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean""
    IL_0032:  brtrue.s   IL_0077
    IL_0034:  ldarg.0
    IL_0035:  ldc.i4.0
    IL_0036:  dup
    IL_0037:  stloc.1
    IL_0038:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
    IL_003d:  ldarg.0
    IL_003e:  ldloc.3
    IL_003f:  stfld      ""C.VB$StateMachine_1_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_0044:  ldarg.0
    IL_0045:  ldflda     ""C.VB$StateMachine_1_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
    IL_004a:  ldloca.s   V_3
    IL_004c:  ldarg.0
    IL_004d:  stloc.s    V_4
    IL_004f:  ldloca.s   V_4
    IL_0051:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), C.VB$StateMachine_1_F)(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef C.VB$StateMachine_1_F)""
    IL_0056:  nop
    IL_0057:  leave.s    IL_00c9
    IL_0059:  ldarg.0
    IL_005a:  ldc.i4.m1
    IL_005b:  dup
    IL_005c:  stloc.1
    IL_005d:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
    IL_0062:  ldarg.0
    IL_0063:  ldfld      ""C.VB$StateMachine_1_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_0068:  stloc.3
    IL_0069:  ldarg.0
    IL_006a:  ldflda     ""C.VB$StateMachine_1_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_006f:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_0075:  br.s       IL_0077
    IL_0077:  ldloca.s   V_3
    IL_0079:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer""
    IL_007e:  stloc.s    V_5
    IL_0080:  ldloca.s   V_3
    IL_0082:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_0088:  ldloc.s    V_5
    IL_008a:  stloc.0
    IL_008b:  leave.s    IL_00b2
  }
  catch System.Exception
  {
    IL_008d:  dup
    IL_008e:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)""
    IL_0093:  stloc.s    V_6
    IL_0095:  ldarg.0
    IL_0096:  ldc.i4.s   -2
    IL_0098:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
    IL_009d:  ldarg.0
    IL_009e:  ldflda     ""C.VB$StateMachine_1_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
    IL_00a3:  ldloc.s    V_6
    IL_00a5:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetException(System.Exception)""
    IL_00aa:  nop
    IL_00ab:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()""
    IL_00b0:  leave.s    IL_00c9
  }
  IL_00b2:  ldarg.0
  IL_00b3:  ldc.i4.s   -2
  IL_00b5:  dup
  IL_00b6:  stloc.1
  IL_00b7:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_00bc:  ldarg.0
  IL_00bd:  ldflda     ""C.VB$StateMachine_1_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
  IL_00c2:  ldloc.0
  IL_00c3:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetResult(Integer)""
  IL_00c8:  nop
  IL_00c9:  ret
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
End Class
    </file>
</compilation>

            Dim compilation0 = CompilationUtils.CreateCompilationWithReferences(source0, references:=LatestVbReferences, options:=ComSafeDebugDll)
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
                AssertEx.Equal(Of Integer)({&H06000004UI}, diff1.UpdatedMethods.Select(Function(m) MetadataTokens.GetToken(m)))

                ' verify delta metadata contains expected rows
                Using md1 = diff1.GetMetadata()
                    CheckEncLogDefinitions(md1.Reader,
                        Row(3, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(4, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                        Row(6, TableIndex.Field, EditAndContinueOperation.Default),
                        Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(9, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(10, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(11, TableIndex.CustomAttribute, EditAndContinueOperation.Default))
                End Using

                diff1.VerifyIL("C.VB$StateMachine_1_F.MoveNext()", "
{
  // Code size      202 (0xca)
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
    IL_000c:  br.s       IL_0059
    IL_000e:  nop
    IL_000f:  ldarg.0
    IL_0010:  ldc.r8     10
    IL_0019:  stfld      ""C.VB$StateMachine_1_F.$VB$ResumableLocal_x$1 As Double""
    IL_001e:  ldc.i4.s   20
    IL_0020:  call       ""Function System.Threading.Tasks.Task.FromResult(Of Integer)(Integer) As System.Threading.Tasks.Task(Of Integer)""
    IL_0025:  callvirt   ""Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_002a:  stloc.3
    IL_002b:  ldloca.s   V_3
    IL_002d:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean""
    IL_0032:  brtrue.s   IL_0077
    IL_0034:  ldarg.0
    IL_0035:  ldc.i4.0
    IL_0036:  dup
    IL_0037:  stloc.1
    IL_0038:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
    IL_003d:  ldarg.0
    IL_003e:  ldloc.3
    IL_003f:  stfld      ""C.VB$StateMachine_1_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_0044:  ldarg.0
    IL_0045:  ldflda     ""C.VB$StateMachine_1_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
    IL_004a:  ldloca.s   V_3
    IL_004c:  ldarg.0
    IL_004d:  stloc.s    V_4
    IL_004f:  ldloca.s   V_4
    IL_0051:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), C.VB$StateMachine_1_F)(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef C.VB$StateMachine_1_F)""
    IL_0056:  nop
    IL_0057:  leave.s    IL_00c9
    IL_0059:  ldarg.0
    IL_005a:  ldc.i4.m1
    IL_005b:  dup
    IL_005c:  stloc.1
    IL_005d:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
    IL_0062:  ldarg.0
    IL_0063:  ldfld      ""C.VB$StateMachine_1_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_0068:  stloc.3
    IL_0069:  ldarg.0
    IL_006a:  ldflda     ""C.VB$StateMachine_1_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_006f:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_0075:  br.s       IL_0077
    IL_0077:  ldloca.s   V_3
    IL_0079:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer""
    IL_007e:  stloc.s    V_5
    IL_0080:  ldloca.s   V_3
    IL_0082:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_0088:  ldloc.s    V_5
    IL_008a:  stloc.0
    IL_008b:  leave.s    IL_00b2
  }
  catch System.Exception
  {
    IL_008d:  dup
    IL_008e:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)""
    IL_0093:  stloc.s    V_6
    IL_0095:  ldarg.0
    IL_0096:  ldc.i4.s   -2
    IL_0098:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
    IL_009d:  ldarg.0
    IL_009e:  ldflda     ""C.VB$StateMachine_1_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
    IL_00a3:  ldloc.s    V_6
    IL_00a5:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetException(System.Exception)""
    IL_00aa:  nop
    IL_00ab:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()""
    IL_00b0:  leave.s    IL_00c9
  }
  IL_00b2:  ldarg.0
  IL_00b3:  ldc.i4.s   -2
  IL_00b5:  dup
  IL_00b6:  stloc.1
  IL_00b7:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_00bc:  ldarg.0
  IL_00bd:  ldflda     ""C.VB$StateMachine_1_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
  IL_00c2:  ldloc.0
  IL_00c3:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetResult(Integer)""
  IL_00c8:  nop
  IL_00c9:  ret
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
End Class
    </file>
</compilation>

            ' Rude edit but the compiler should handle it
            Dim compilation0 = CreateCompilationWithReferences(source0, references:=LatestVbReferences, options:=ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
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
                    Row(6, TableIndex.MethodDef, EditAndContinueOperation.Default),
                    Row(9, TableIndex.MethodDef, EditAndContinueOperation.Default),
                    Row(19, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(20, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(21, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(22, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(23, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(24, TableIndex.CustomAttribute, EditAndContinueOperation.Default))

            diff1.VerifyIL("C.VB$StateMachine_1_F.MoveNext()", "
{
  // Code size      202 (0xca)
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
    IL_000c:  br.s       IL_005b
    IL_000e:  nop
    IL_000f:  ldarg.0
    IL_0010:  newobj     ""Sub C..ctor()""
    IL_0015:  stfld      ""C.VB$StateMachine_1_F.$VB$ResumableLocal_a1$2 As C""
    IL_001a:  ldarg.0
    IL_001b:  ldc.i4.3
    IL_001c:  stfld      ""C.VB$StateMachine_1_F.$VB$ResumableLocal_a2$1 As Integer""
    IL_0021:  ldc.i4.0
    IL_0022:  call       ""Function System.Threading.Tasks.Task.Delay(Integer) As System.Threading.Tasks.Task""
    IL_0027:  callvirt   ""Function System.Threading.Tasks.Task.GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter""
    IL_002c:  stloc.3
    IL_002d:  ldloca.s   V_3
    IL_002f:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter.get_IsCompleted() As Boolean""
    IL_0034:  brtrue.s   IL_0079
    IL_0036:  ldarg.0
    IL_0037:  ldc.i4.0
    IL_0038:  dup
    IL_0039:  stloc.1
    IL_003a:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
    IL_003f:  ldarg.0
    IL_0040:  ldloc.3
    IL_0041:  stfld      ""C.VB$StateMachine_1_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
    IL_0046:  ldarg.0
    IL_0047:  ldflda     ""C.VB$StateMachine_1_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
    IL_004c:  ldloca.s   V_3
    IL_004e:  ldarg.0
    IL_004f:  stloc.s    V_4
    IL_0051:  ldloca.s   V_4
    IL_0053:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter, C.VB$StateMachine_1_F)(ByRef System.Runtime.CompilerServices.TaskAwaiter, ByRef C.VB$StateMachine_1_F)""
    IL_0058:  nop
    IL_0059:  leave.s    IL_00c9
    IL_005b:  ldarg.0
    IL_005c:  ldc.i4.m1
    IL_005d:  dup
    IL_005e:  stloc.1
    IL_005f:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
    IL_0064:  ldarg.0
    IL_0065:  ldfld      ""C.VB$StateMachine_1_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
    IL_006a:  stloc.3
    IL_006b:  ldarg.0
    IL_006c:  ldflda     ""C.VB$StateMachine_1_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
    IL_0071:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_0077:  br.s       IL_0079
    IL_0079:  ldloca.s   V_3
    IL_007b:  call       ""Sub System.Runtime.CompilerServices.TaskAwaiter.GetResult()""
    IL_0080:  nop
    IL_0081:  ldloca.s   V_3
    IL_0083:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_0089:  ldc.i4.1
    IL_008a:  stloc.0
    IL_008b:  leave.s    IL_00b2
  }
  catch System.Exception
  {
    IL_008d:  dup
    IL_008e:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)""
    IL_0093:  stloc.s    V_5
    IL_0095:  ldarg.0
    IL_0096:  ldc.i4.s   -2
    IL_0098:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
    IL_009d:  ldarg.0
    IL_009e:  ldflda     ""C.VB$StateMachine_1_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
    IL_00a3:  ldloc.s    V_5
    IL_00a5:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetException(System.Exception)""
    IL_00aa:  nop
    IL_00ab:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()""
    IL_00b0:  leave.s    IL_00c9
  }
  IL_00b2:  ldarg.0
  IL_00b3:  ldc.i4.s   -2
  IL_00b5:  dup
  IL_00b6:  stloc.1
  IL_00b7:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_00bc:  ldarg.0
  IL_00bd:  ldflda     ""C.VB$StateMachine_1_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
  IL_00c2:  ldloc.0
  IL_00c3:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetResult(Integer)""
  IL_00c8:  nop
  IL_00c9:  ret
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
                    Row(6, TableIndex.MethodDef, EditAndContinueOperation.Default),
                    Row(25, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(26, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(27, TableIndex.CustomAttribute, EditAndContinueOperation.Default))

            diff2.VerifyIL("C.VB$StateMachine_1_F.MoveNext()", "
{
  // Code size      202 (0xca)
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
    IL_000c:  br.s       IL_005b
    IL_000e:  nop
    IL_000f:  ldarg.0
    IL_0010:  ldc.i4.1
    IL_0011:  stfld      ""C.VB$StateMachine_1_F.$VB$ResumableLocal_a1$3 As Boolean""
    IL_0016:  ldarg.0
    IL_0017:  newobj     ""Sub C..ctor()""
    IL_001c:  stfld      ""C.VB$StateMachine_1_F.$VB$ResumableLocal_a2$4 As C""
    IL_0021:  ldc.i4.0
    IL_0022:  call       ""Function System.Threading.Tasks.Task.Delay(Integer) As System.Threading.Tasks.Task""
    IL_0027:  callvirt   ""Function System.Threading.Tasks.Task.GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter""
    IL_002c:  stloc.3
    IL_002d:  ldloca.s   V_3
    IL_002f:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter.get_IsCompleted() As Boolean""
    IL_0034:  brtrue.s   IL_0079
    IL_0036:  ldarg.0
    IL_0037:  ldc.i4.0
    IL_0038:  dup
    IL_0039:  stloc.1
    IL_003a:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
    IL_003f:  ldarg.0
    IL_0040:  ldloc.3
    IL_0041:  stfld      ""C.VB$StateMachine_1_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
    IL_0046:  ldarg.0
    IL_0047:  ldflda     ""C.VB$StateMachine_1_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
    IL_004c:  ldloca.s   V_3
    IL_004e:  ldarg.0
    IL_004f:  stloc.s    V_4
    IL_0051:  ldloca.s   V_4
    IL_0053:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter, C.VB$StateMachine_1_F)(ByRef System.Runtime.CompilerServices.TaskAwaiter, ByRef C.VB$StateMachine_1_F)""
    IL_0058:  nop
    IL_0059:  leave.s    IL_00c9
    IL_005b:  ldarg.0
    IL_005c:  ldc.i4.m1
    IL_005d:  dup
    IL_005e:  stloc.1
    IL_005f:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
    IL_0064:  ldarg.0
    IL_0065:  ldfld      ""C.VB$StateMachine_1_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
    IL_006a:  stloc.3
    IL_006b:  ldarg.0
    IL_006c:  ldflda     ""C.VB$StateMachine_1_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
    IL_0071:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_0077:  br.s       IL_0079
    IL_0079:  ldloca.s   V_3
    IL_007b:  call       ""Sub System.Runtime.CompilerServices.TaskAwaiter.GetResult()""
    IL_0080:  nop
    IL_0081:  ldloca.s   V_3
    IL_0083:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_0089:  ldc.i4.1
    IL_008a:  stloc.0
    IL_008b:  leave.s    IL_00b2
  }
  catch System.Exception
  {
    IL_008d:  dup
    IL_008e:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)""
    IL_0093:  stloc.s    V_5
    IL_0095:  ldarg.0
    IL_0096:  ldc.i4.s   -2
    IL_0098:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
    IL_009d:  ldarg.0
    IL_009e:  ldflda     ""C.VB$StateMachine_1_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
    IL_00a3:  ldloc.s    V_5
    IL_00a5:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetException(System.Exception)""
    IL_00aa:  nop
    IL_00ab:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()""
    IL_00b0:  leave.s    IL_00c9
  }
  IL_00b2:  ldarg.0
  IL_00b3:  ldc.i4.s   -2
  IL_00b5:  dup
  IL_00b6:  stloc.1
  IL_00b7:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_00bc:  ldarg.0
  IL_00bd:  ldflda     ""C.VB$StateMachine_1_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
  IL_00c2:  ldloc.0
  IL_00c3:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetResult(Integer)""
  IL_00c8:  nop
  IL_00c9:  ret
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
                    Row(9, TableIndex.MethodDef, EditAndContinueOperation.Default),
                    Row(12, TableIndex.MethodDef, EditAndContinueOperation.Default),
                    Row(28, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(29, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(30, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(31, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(32, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(33, TableIndex.CustomAttribute, EditAndContinueOperation.Default))

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

            Dim compilation0 = CreateCompilationWithReferences(source0, references:=LatestVbReferences, options:=ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All))

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
            Dim compilation0 = CreateCompilationWithReferences(source0, references:=LatestVbReferences, options:=ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
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
                    Row(19, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(20, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(21, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(22, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(23, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(24, TableIndex.CustomAttribute, EditAndContinueOperation.Default))

            diff1.VerifyIL("C.VB$StateMachine_4_F.MoveNext()", "
{
  // Code size      315 (0x13b)
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
    IL_0012:  br.s       IL_005c
    IL_0014:  br         IL_00cb
    IL_0019:  nop
    IL_001a:  ldarg.0
    IL_001b:  ldfld      ""C.VB$StateMachine_4_F.$VB$Me As C""
    IL_0020:  callvirt   ""Function C.A3() As System.Threading.Tasks.Task(Of C)""
    IL_0025:  callvirt   ""Function System.Threading.Tasks.Task(Of C).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of C)""
    IL_002a:  stloc.3
    IL_002b:  ldloca.s   V_3
    IL_002d:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter(Of C).get_IsCompleted() As Boolean""
    IL_0032:  brtrue.s   IL_007a
    IL_0034:  ldarg.0
    IL_0035:  ldc.i4.0
    IL_0036:  dup
    IL_0037:  stloc.1
    IL_0038:  stfld      ""C.VB$StateMachine_4_F.$State As Integer""
    IL_003d:  ldarg.0
    IL_003e:  ldloc.3
    IL_003f:  stfld      ""C.VB$StateMachine_4_F.$A2 As System.Runtime.CompilerServices.TaskAwaiter(Of C)""
    IL_0044:  ldarg.0
    IL_0045:  ldflda     ""C.VB$StateMachine_4_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
    IL_004a:  ldloca.s   V_3
    IL_004c:  ldarg.0
    IL_004d:  stloc.s    V_4
    IL_004f:  ldloca.s   V_4
    IL_0051:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of C), C.VB$StateMachine_4_F)(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of C), ByRef C.VB$StateMachine_4_F)""
    IL_0056:  nop
    IL_0057:  leave      IL_013a
    IL_005c:  ldarg.0
    IL_005d:  ldc.i4.m1
    IL_005e:  dup
    IL_005f:  stloc.1
    IL_0060:  stfld      ""C.VB$StateMachine_4_F.$State As Integer""
    IL_0065:  ldarg.0
    IL_0066:  ldfld      ""C.VB$StateMachine_4_F.$A2 As System.Runtime.CompilerServices.TaskAwaiter(Of C)""
    IL_006b:  stloc.3
    IL_006c:  ldarg.0
    IL_006d:  ldflda     ""C.VB$StateMachine_4_F.$A2 As System.Runtime.CompilerServices.TaskAwaiter(Of C)""
    IL_0072:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter(Of C)""
    IL_0078:  br.s       IL_007a
    IL_007a:  ldloca.s   V_3
    IL_007c:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter(Of C).GetResult() As C""
    IL_0081:  pop
    IL_0082:  ldloca.s   V_3
    IL_0084:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter(Of C)""
    IL_008a:  ldarg.0
    IL_008b:  ldfld      ""C.VB$StateMachine_4_F.$VB$Me As C""
    IL_0090:  callvirt   ""Function C.A2() As System.Threading.Tasks.Task(Of Integer)""
    IL_0095:  callvirt   ""Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_009a:  stloc.s    V_5
    IL_009c:  ldloca.s   V_5
    IL_009e:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean""
    IL_00a3:  brtrue.s   IL_00ea
    IL_00a5:  ldarg.0
    IL_00a6:  ldc.i4.1
    IL_00a7:  dup
    IL_00a8:  stloc.1
    IL_00a9:  stfld      ""C.VB$StateMachine_4_F.$State As Integer""
    IL_00ae:  ldarg.0
    IL_00af:  ldloc.s    V_5
    IL_00b1:  stfld      ""C.VB$StateMachine_4_F.$A1 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_00b6:  ldarg.0
    IL_00b7:  ldflda     ""C.VB$StateMachine_4_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
    IL_00bc:  ldloca.s   V_5
    IL_00be:  ldarg.0
    IL_00bf:  stloc.s    V_4
    IL_00c1:  ldloca.s   V_4
    IL_00c3:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), C.VB$StateMachine_4_F)(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef C.VB$StateMachine_4_F)""
    IL_00c8:  nop
    IL_00c9:  leave.s    IL_013a
    IL_00cb:  ldarg.0
    IL_00cc:  ldc.i4.m1
    IL_00cd:  dup
    IL_00ce:  stloc.1
    IL_00cf:  stfld      ""C.VB$StateMachine_4_F.$State As Integer""
    IL_00d4:  ldarg.0
    IL_00d5:  ldfld      ""C.VB$StateMachine_4_F.$A1 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_00da:  stloc.s    V_5
    IL_00dc:  ldarg.0
    IL_00dd:  ldflda     ""C.VB$StateMachine_4_F.$A1 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_00e2:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_00e8:  br.s       IL_00ea
    IL_00ea:  ldloca.s   V_5
    IL_00ec:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer""
    IL_00f1:  pop
    IL_00f2:  ldloca.s   V_5
    IL_00f4:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_00fa:  ldc.i4.1
    IL_00fb:  stloc.0
    IL_00fc:  leave.s    IL_0123
  }
  catch System.Exception
  {
    IL_00fe:  dup
    IL_00ff:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)""
    IL_0104:  stloc.s    V_6
    IL_0106:  ldarg.0
    IL_0107:  ldc.i4.s   -2
    IL_0109:  stfld      ""C.VB$StateMachine_4_F.$State As Integer""
    IL_010e:  ldarg.0
    IL_010f:  ldflda     ""C.VB$StateMachine_4_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
    IL_0114:  ldloc.s    V_6
    IL_0116:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetException(System.Exception)""
    IL_011b:  nop
    IL_011c:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()""
    IL_0121:  leave.s    IL_013a
  }
  IL_0123:  ldarg.0
  IL_0124:  ldc.i4.s   -2
  IL_0126:  dup
  IL_0127:  stloc.1
  IL_0128:  stfld      ""C.VB$StateMachine_4_F.$State As Integer""
  IL_012d:  ldarg.0
  IL_012e:  ldflda     ""C.VB$StateMachine_4_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
  IL_0133:  ldloc.0
  IL_0134:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetResult(Integer)""
  IL_0139:  nop
  IL_013a:  ret
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
                    Row(25, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(26, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(27, TableIndex.CustomAttribute, EditAndContinueOperation.Default))

            diff2.VerifyIL("C.VB$StateMachine_4_F.MoveNext()", "
{
  // Code size      315 (0x13b)
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
    IL_0012:  br.s       IL_005c
    IL_0014:  br         IL_00cb
    IL_0019:  nop
    IL_001a:  ldarg.0
    IL_001b:  ldfld      ""C.VB$StateMachine_4_F.$VB$Me As C""
    IL_0020:  callvirt   ""Function C.A1() As System.Threading.Tasks.Task(Of Boolean)""
    IL_0025:  callvirt   ""Function System.Threading.Tasks.Task(Of Boolean).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Boolean)""
    IL_002a:  stloc.3
    IL_002b:  ldloca.s   V_3
    IL_002d:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter(Of Boolean).get_IsCompleted() As Boolean""
    IL_0032:  brtrue.s   IL_007a
    IL_0034:  ldarg.0
    IL_0035:  ldc.i4.0
    IL_0036:  dup
    IL_0037:  stloc.1
    IL_0038:  stfld      ""C.VB$StateMachine_4_F.$State As Integer""
    IL_003d:  ldarg.0
    IL_003e:  ldloc.3
    IL_003f:  stfld      ""C.VB$StateMachine_4_F.$A3 As System.Runtime.CompilerServices.TaskAwaiter(Of Boolean)""
    IL_0044:  ldarg.0
    IL_0045:  ldflda     ""C.VB$StateMachine_4_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
    IL_004a:  ldloca.s   V_3
    IL_004c:  ldarg.0
    IL_004d:  stloc.s    V_4
    IL_004f:  ldloca.s   V_4
    IL_0051:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Boolean), C.VB$StateMachine_4_F)(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Boolean), ByRef C.VB$StateMachine_4_F)""
    IL_0056:  nop
    IL_0057:  leave      IL_013a
    IL_005c:  ldarg.0
    IL_005d:  ldc.i4.m1
    IL_005e:  dup
    IL_005f:  stloc.1
    IL_0060:  stfld      ""C.VB$StateMachine_4_F.$State As Integer""
    IL_0065:  ldarg.0
    IL_0066:  ldfld      ""C.VB$StateMachine_4_F.$A3 As System.Runtime.CompilerServices.TaskAwaiter(Of Boolean)""
    IL_006b:  stloc.3
    IL_006c:  ldarg.0
    IL_006d:  ldflda     ""C.VB$StateMachine_4_F.$A3 As System.Runtime.CompilerServices.TaskAwaiter(Of Boolean)""
    IL_0072:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter(Of Boolean)""
    IL_0078:  br.s       IL_007a
    IL_007a:  ldloca.s   V_3
    IL_007c:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter(Of Boolean).GetResult() As Boolean""
    IL_0081:  pop
    IL_0082:  ldloca.s   V_3
    IL_0084:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter(Of Boolean)""
    IL_008a:  ldarg.0
    IL_008b:  ldfld      ""C.VB$StateMachine_4_F.$VB$Me As C""
    IL_0090:  callvirt   ""Function C.A3() As System.Threading.Tasks.Task(Of C)""
    IL_0095:  callvirt   ""Function System.Threading.Tasks.Task(Of C).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of C)""
    IL_009a:  stloc.s    V_5
    IL_009c:  ldloca.s   V_5
    IL_009e:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter(Of C).get_IsCompleted() As Boolean""
    IL_00a3:  brtrue.s   IL_00ea
    IL_00a5:  ldarg.0
    IL_00a6:  ldc.i4.1
    IL_00a7:  dup
    IL_00a8:  stloc.1
    IL_00a9:  stfld      ""C.VB$StateMachine_4_F.$State As Integer""
    IL_00ae:  ldarg.0
    IL_00af:  ldloc.s    V_5
    IL_00b1:  stfld      ""C.VB$StateMachine_4_F.$A2 As System.Runtime.CompilerServices.TaskAwaiter(Of C)""
    IL_00b6:  ldarg.0
    IL_00b7:  ldflda     ""C.VB$StateMachine_4_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
    IL_00bc:  ldloca.s   V_5
    IL_00be:  ldarg.0
    IL_00bf:  stloc.s    V_4
    IL_00c1:  ldloca.s   V_4
    IL_00c3:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of C), C.VB$StateMachine_4_F)(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of C), ByRef C.VB$StateMachine_4_F)""
    IL_00c8:  nop
    IL_00c9:  leave.s    IL_013a
    IL_00cb:  ldarg.0
    IL_00cc:  ldc.i4.m1
    IL_00cd:  dup
    IL_00ce:  stloc.1
    IL_00cf:  stfld      ""C.VB$StateMachine_4_F.$State As Integer""
    IL_00d4:  ldarg.0
    IL_00d5:  ldfld      ""C.VB$StateMachine_4_F.$A2 As System.Runtime.CompilerServices.TaskAwaiter(Of C)""
    IL_00da:  stloc.s    V_5
    IL_00dc:  ldarg.0
    IL_00dd:  ldflda     ""C.VB$StateMachine_4_F.$A2 As System.Runtime.CompilerServices.TaskAwaiter(Of C)""
    IL_00e2:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter(Of C)""
    IL_00e8:  br.s       IL_00ea
    IL_00ea:  ldloca.s   V_5
    IL_00ec:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter(Of C).GetResult() As C""
    IL_00f1:  pop
    IL_00f2:  ldloca.s   V_5
    IL_00f4:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter(Of C)""
    IL_00fa:  ldc.i4.1
    IL_00fb:  stloc.0
    IL_00fc:  leave.s    IL_0123
  }
  catch System.Exception
  {
    IL_00fe:  dup
    IL_00ff:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)""
    IL_0104:  stloc.s    V_6
    IL_0106:  ldarg.0
    IL_0107:  ldc.i4.s   -2
    IL_0109:  stfld      ""C.VB$StateMachine_4_F.$State As Integer""
    IL_010e:  ldarg.0
    IL_010f:  ldflda     ""C.VB$StateMachine_4_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
    IL_0114:  ldloc.s    V_6
    IL_0116:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetException(System.Exception)""
    IL_011b:  nop
    IL_011c:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()""
    IL_0121:  leave.s    IL_013a
  }
  IL_0123:  ldarg.0
  IL_0124:  ldc.i4.s   -2
  IL_0126:  dup
  IL_0127:  stloc.1
  IL_0128:  stfld      ""C.VB$StateMachine_4_F.$State As Integer""
  IL_012d:  ldarg.0
  IL_012e:  ldflda     ""C.VB$StateMachine_4_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
  IL_0133:  ldloc.0
  IL_0134:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetResult(Integer)""
  IL_0139:  nop
  IL_013a:  ret
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
                    Row(28, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(29, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(30, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(31, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(32, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(33, TableIndex.CustomAttribute, EditAndContinueOperation.Default))
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
            Dim compilation0 = CreateCompilationWithReferences(source0, references:=LatestVbReferences, options:=ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
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

        <Fact, WorkItem(1170899, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1170899")>
        Public Sub HoistedAnonymousTypes1()
            Dim source0 = MarkedSource("
Imports System
Imports System.Collections.Generic

Class C
    Iterator Function F() As IEnumerable(Of Integer)
        Dim <N:0>x</N:0> = New With {.A = 1}
        Yield 1
        Console.WriteLine(x.A + 1)
    End Function
End Class
")
            Dim source1 = MarkedSource("
Imports System
Imports System.Collections.Generic

Class C
    Iterator Function F() As IEnumerable(Of Integer)
        Dim <N:0>x</N:0> = New With {.A = 1}
        Yield 1
        Console.WriteLine(x.A + 2)
    End Function
End Class
")
            Dim source2 = MarkedSource("
Imports System
Imports System.Collections.Generic

Class C
    Iterator Function F() As IEnumerable(Of Integer)
        Dim <N:0>x</N:0> = New With {.A = 1}
        Yield 1
        Console.WriteLine(x.A + 3)
    End Function
End Class
")
            Dim compilation0 = CreateCompilationWithMscorlib45({source0.Tree}, options:=ComSafeDebugDll)
            Dim compilation1 = compilation0.WithSource(source1.Tree)
            Dim compilation2 = compilation1.WithSource(source2.Tree)

            Dim v0 = CompileAndVerify(compilation0)
            v0.VerifyDiagnostics()
            Dim md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)

            Dim f0 = compilation0.GetMember(Of MethodSymbol)("C.F")
            Dim f1 = compilation1.GetMember(Of MethodSymbol)("C.F")
            Dim f2 = compilation2.GetMember(Of MethodSymbol)("C.F")

            Dim generation0 = EmitBaseline.CreateInitialBaseline(md0, AddressOf v0.CreateSymReader().GetEncMethodDebugInfo)

            Dim baselineIL = "
{
  // Code size       94 (0x5e)
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
  IL_0023:  ldc.i4.1
  IL_0024:  newobj     ""Sub VB$AnonymousType_0(Of Integer)..ctor(Integer)""
  IL_0029:  stfld      ""C.VB$StateMachine_1_F.$VB$ResumableLocal_x$0 As <anonymous type: A As Integer>""
  IL_002e:  ldarg.0
  IL_002f:  ldc.i4.1
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
  IL_0049:  ldarg.0
  IL_004a:  ldfld      ""C.VB$StateMachine_1_F.$VB$ResumableLocal_x$0 As <anonymous type: A As Integer>""
  IL_004f:  callvirt   ""Function VB$AnonymousType_0(Of Integer).get_A() As Integer""
  IL_0054:  ldc.i4.<<VALUE>>
  IL_0055:  add.ovf
  IL_0056:  call       ""Sub System.Console.WriteLine(Integer)""
  IL_005b:  nop
  IL_005c:  ldc.i4.0
  IL_005d:  ret
}"
            v0.VerifyIL("C.VB$StateMachine_1_F.MoveNext()", baselineIL.Replace("<<VALUE>>", "1"))

            Dim diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    New SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables:=True)))

            diff1.VerifySynthesizedMembers(
                "C: {VB$StateMachine_1_F}",
                "C.VB$StateMachine_1_F: {$State, $Current, $InitialThreadId, $VB$Me, $VB$ResumableLocal_x$0, Dispose, MoveNext, GetEnumerator, IEnumerable.GetEnumerator, get_Current, Reset, IEnumerator.get_Current, Current, IEnumerator.Current}")

            diff1.VerifyIL("C.VB$StateMachine_1_F.MoveNext()", baselineIL.Replace("<<VALUE>>", "2"))

            Dim diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    New SemanticEdit(SemanticEditKind.Update, f1, f2, GetSyntaxMapFromMarkers(source1, source2), preserveLocalVariables:=True)))

            diff2.VerifySynthesizedMembers(
                "C: {VB$StateMachine_1_F}",
                "C.VB$StateMachine_1_F: {$State, $Current, $InitialThreadId, $VB$Me, $VB$ResumableLocal_x$0, Dispose, MoveNext, GetEnumerator, IEnumerable.GetEnumerator, get_Current, Reset, IEnumerator.get_Current, Current, IEnumerator.Current}")

            diff2.VerifyIL("C.VB$StateMachine_1_F.MoveNext()", baselineIL.Replace("<<VALUE>>", "3"))
        End Sub

        <Fact, WorkItem(3192, "https://github.com/dotnet/roslyn/issues/3192")>
        Public Sub HoistedAnonymousTypes_Complex()
            Dim source0 = MarkedSource("
Imports System
Imports System.Collections.Generic

Class C
    Iterator Function F() As IEnumerable(Of Integer)
        Dim <N:0>x</N:0> = { New With {.A = New With { .B = 1 } } }
        Yield 1
        Console.WriteLine(x(0).A.B + 1)
    End Function
End Class
")
            Dim source1 = MarkedSource("
Imports System
Imports System.Collections.Generic

Class C
    Iterator Function F() As IEnumerable(Of Integer)
        Dim <N:0>x</N:0> = { New With {.A = New With { .B = 1 } } }
        Yield 1
        Console.WriteLine(x(0).A.B + 2)
    End Function
End Class
")
            Dim source2 = MarkedSource("
Imports System
Imports System.Collections.Generic

Class C
    Iterator Function F() As IEnumerable(Of Integer)
        Dim <N:0>x</N:0> = { New With {.A = New With { .B = 1 } } }
        Yield 1
        Console.WriteLine(x(0).A.B + 3)
    End Function
End Class
")
            Dim compilation0 = CreateCompilationWithMscorlib45({source0.Tree}, options:=ComSafeDebugDll)
            Dim compilation1 = compilation0.WithSource(source1.Tree)
            Dim compilation2 = compilation1.WithSource(source2.Tree)

            Dim v0 = CompileAndVerify(compilation0)
            v0.VerifyDiagnostics()
            Dim md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)

            Dim f0 = compilation0.GetMember(Of MethodSymbol)("C.F")
            Dim f1 = compilation1.GetMember(Of MethodSymbol)("C.F")
            Dim f2 = compilation2.GetMember(Of MethodSymbol)("C.F")

            Dim generation0 = EmitBaseline.CreateInitialBaseline(md0, AddressOf v0.CreateSymReader().GetEncMethodDebugInfo)

            Dim baselineIL = "
{
  // Code size      115 (0x73)
  .maxstack  5
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
  IL_0014:  br.s       IL_004e
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
  IL_0024:  newarr     ""VB$AnonymousType_0(Of <anonymous type: B As Integer>)""
  IL_0029:  dup
  IL_002a:  ldc.i4.0
  IL_002b:  ldc.i4.1
  IL_002c:  newobj     ""Sub VB$AnonymousType_1(Of Integer)..ctor(Integer)""
  IL_0031:  newobj     ""Sub VB$AnonymousType_0(Of <anonymous type: B As Integer>)..ctor(<anonymous type: B As Integer>)""
  IL_0036:  stelem.ref
  IL_0037:  stfld      ""C.VB$StateMachine_1_F.$VB$ResumableLocal_x$0 As <anonymous type: A As <anonymous type: B As Integer>>()""
  IL_003c:  ldarg.0
  IL_003d:  ldc.i4.1
  IL_003e:  stfld      ""C.VB$StateMachine_1_F.$Current As Integer""
  IL_0043:  ldarg.0
  IL_0044:  ldc.i4.1
  IL_0045:  dup
  IL_0046:  stloc.1
  IL_0047:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_004c:  ldc.i4.1
  IL_004d:  ret
  IL_004e:  ldarg.0
  IL_004f:  ldc.i4.m1
  IL_0050:  dup
  IL_0051:  stloc.1
  IL_0052:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_0057:  ldarg.0
  IL_0058:  ldfld      ""C.VB$StateMachine_1_F.$VB$ResumableLocal_x$0 As <anonymous type: A As <anonymous type: B As Integer>>()""
  IL_005d:  ldc.i4.0
  IL_005e:  ldelem.ref
  IL_005f:  callvirt   ""Function VB$AnonymousType_0(Of <anonymous type: B As Integer>).get_A() As <anonymous type: B As Integer>""
  IL_0064:  callvirt   ""Function VB$AnonymousType_1(Of Integer).get_B() As Integer""
  IL_0069:  ldc.i4.<<VALUE>>
  IL_006a:  add.ovf
  IL_006b:  call       ""Sub System.Console.WriteLine(Integer)""
  IL_0070:  nop
  IL_0071:  ldc.i4.0
  IL_0072:  ret
}"
            v0.VerifyIL("C.VB$StateMachine_1_F.MoveNext()", baselineIL.Replace("<<VALUE>>", "1"))

            Dim diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    New SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables:=True)))

            diff1.VerifySynthesizedMembers(
                "C: {VB$StateMachine_1_F}",
                "C.VB$StateMachine_1_F: {$State, $Current, $InitialThreadId, $VB$Me, $VB$ResumableLocal_x$0, Dispose, MoveNext, GetEnumerator, IEnumerable.GetEnumerator, get_Current, Reset, IEnumerator.get_Current, Current, IEnumerator.Current}")

            diff1.VerifyIL("C.VB$StateMachine_1_F.MoveNext()", baselineIL.Replace("<<VALUE>>", "2"))

            Dim diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    New SemanticEdit(SemanticEditKind.Update, f1, f2, GetSyntaxMapFromMarkers(source1, source2), preserveLocalVariables:=True)))

            diff2.VerifySynthesizedMembers(
                "C: {VB$StateMachine_1_F}",
                "C.VB$StateMachine_1_F: {$State, $Current, $InitialThreadId, $VB$Me, $VB$ResumableLocal_x$0, Dispose, MoveNext, GetEnumerator, IEnumerable.GetEnumerator, get_Current, Reset, IEnumerator.get_Current, Current, IEnumerator.Current}")

            diff2.VerifyIL("C.VB$StateMachine_1_F.MoveNext()", baselineIL.Replace("<<VALUE>>", "3"))
        End Sub

        <Fact, WorkItem(3192, "https://github.com/dotnet/roslyn/issues/3192")>
        Public Sub HoistedAnonymousTypes_Delete()
            Dim source0 = MarkedSource("
Imports System.Linq
Imports System.Threading.Tasks

Class C
    Shared Async Function F() As Task(Of Integer)
        Dim <N:1>x</N:1> = From b In { 1, 2, 3 } <N:0>Select <N:3>A = b</N:3></N:0>
        Return <N:2>Await Task.FromResult(1)</N:2>
    End Function
End Class
")
            Dim source1 = MarkedSource("
Imports System.Linq
Imports System.Threading.Tasks

Class C
    Shared Async Function F() As Task(Of Integer)
        Dim <N:1>x</N:1> = From b In { 1, 2, 3 } <N:0>Select <N:3>A = b</N:3></N:0>
        Dim y = x.First()
        Return <N:2>Await Task.FromResult(1)</N:2>
    End Function
End Class
")
            Dim source2 = source0
            Dim source3 = source1
            Dim source4 = source0
            Dim source5 = source1

            Dim compilation0 = CreateCompilationWithMscorlib45AndVBRuntime({source0.Tree}, {SystemCoreRef}, options:=ComSafeDebugDll)
            Dim compilation1 = compilation0.WithSource(source1.Tree)
            Dim compilation2 = compilation0.WithSource(source2.Tree)
            Dim compilation3 = compilation0.WithSource(source3.Tree)
            Dim compilation4 = compilation0.WithSource(source4.Tree)
            Dim compilation5 = compilation0.WithSource(source5.Tree)

            Dim v0 = CompileAndVerify(compilation0)
            Dim md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)

            Dim f0 = compilation0.GetMember(Of MethodSymbol)("C.F")
            Dim f1 = compilation1.GetMember(Of MethodSymbol)("C.F")
            Dim f2 = compilation2.GetMember(Of MethodSymbol)("C.F")
            Dim f3 = compilation3.GetMember(Of MethodSymbol)("C.F")
            Dim f4 = compilation4.GetMember(Of MethodSymbol)("C.F")
            Dim f5 = compilation5.GetMember(Of MethodSymbol)("C.F")

            Dim generation0 = EmitBaseline.CreateInitialBaseline(md0, AddressOf v0.CreateSymReader().GetEncMethodDebugInfo)

            ' y is added 
            Dim diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    New SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables:=True)))

            diff1.VerifySynthesizedMembers(
                "C: {_Closure$__, VB$StateMachine_1_F}",
                "C._Closure$__: {$I1-0, _Lambda$__1-0}",
                "C.VB$StateMachine_1_F: {$State, $Builder, $VB$ResumableLocal_x$0, $VB$ResumableLocal_y$1, $A0, MoveNext, System.Runtime.CompilerServices.IAsyncStateMachine.SetStateMachine}")

            ' y is removed
            Dim diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    New SemanticEdit(SemanticEditKind.Update, f1, f2, GetSyntaxMapFromMarkers(source1, source2), preserveLocalVariables:=True)))

            ' Synthesized members collection still includes y field since members are only added to it and never deleted.
            ' The corresponding CLR field is also present.
            diff2.VerifySynthesizedMembers(
                "C: {_Closure$__, VB$StateMachine_1_F}",
                "C._Closure$__: {$I1-0, _Lambda$__1-0}",
                "C.VB$StateMachine_1_F: {$State, $Builder, $VB$ResumableLocal_x$0, $A0, MoveNext, System.Runtime.CompilerServices.IAsyncStateMachine.SetStateMachine, $VB$ResumableLocal_y$1}")

            ' y is added and a new slot index is allocated for it
            Dim diff3 = compilation3.EmitDifference(
                diff2.NextGeneration,
                ImmutableArray.Create(
                    New SemanticEdit(SemanticEditKind.Update, f2, f3, GetSyntaxMapFromMarkers(source2, source3), preserveLocalVariables:=True)))

            diff3.VerifySynthesizedMembers(
                "C: {_Closure$__, VB$StateMachine_1_F}",
                "C._Closure$__: {$I1-0, _Lambda$__1-0}",
                "C.VB$StateMachine_1_F: {$State, $Builder, $VB$ResumableLocal_x$0, $VB$ResumableLocal_y$2, $A0, MoveNext, System.Runtime.CompilerServices.IAsyncStateMachine.SetStateMachine, $VB$ResumableLocal_y$1}")

            ' y is removed
            Dim diff4 = compilation4.EmitDifference(
                diff3.NextGeneration,
                ImmutableArray.Create(
                    New SemanticEdit(SemanticEditKind.Update, f3, f4, GetSyntaxMapFromMarkers(source3, source4), preserveLocalVariables:=True)))

            diff4.VerifySynthesizedMembers(
                "C: {_Closure$__, VB$StateMachine_1_F}",
                "C._Closure$__: {$I1-0, _Lambda$__1-0}",
                "C.VB$StateMachine_1_F: {$State, $Builder, $VB$ResumableLocal_x$0, $A0, MoveNext, System.Runtime.CompilerServices.IAsyncStateMachine.SetStateMachine, $VB$ResumableLocal_y$2, $VB$ResumableLocal_y$1}")

            ' y is added and a new slot index is allocated for it
            Dim diff5 = compilation5.EmitDifference(
                diff4.NextGeneration,
                ImmutableArray.Create(
                    New SemanticEdit(SemanticEditKind.Update, f4, f5, GetSyntaxMapFromMarkers(source4, source5), preserveLocalVariables:=True)))

            diff5.VerifySynthesizedMembers(
                "C: {_Closure$__, VB$StateMachine_1_F}",
                "C._Closure$__: {$I1-0, _Lambda$__1-0}",
                "C.VB$StateMachine_1_F: {$State, $Builder, $VB$ResumableLocal_x$0, $VB$ResumableLocal_y$3, $A0, MoveNext, System.Runtime.CompilerServices.IAsyncStateMachine.SetStateMachine, $VB$ResumableLocal_y$2, $VB$ResumableLocal_y$1}")
        End Sub

        <Fact, WorkItem(9119, "https://github.com/dotnet/roslyn/issues/9119")>
        Public Sub MissingIteratorStateMachineAttribute()
            Dim source0 = MarkedSource("
Imports System
Imports System.Collections.Generic

Class C
    Public Iterator Function F() As IEnumerable(Of Integer)
        Dim <N:0>a</N:0> As Integer = 0
        <N:1>Yield 0</N:1>
        Console.WriteLine(a)
    End Function
End Class
")
            Dim source1 = MarkedSource("
Imports System
Imports System.Collections.Generic

Class C
    Public Iterator Function F() As IEnumerable(Of Integer)
        Dim <N:0>a</N:0> As Integer = 1
        <N:1>Yield 1</N:1>
        Console.WriteLine(a)
    End Function
End Class
")
            Dim compilation0 = CreateCompilationWithMscorlib({source0.Tree}, options:=ComSafeDebugDll)
            Dim compilation1 = compilation0.WithSource(source1.Tree)

            ' older versions of mscorlib don't contain IteratorStateMachineAttribute
            Assert.Null(compilation0.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_IteratorStateMachineAttribute__ctor))

            Dim v0 = CompileAndVerify(compilation0, verify:=False)
            Dim md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)

            Dim f0 = compilation0.GetMember(Of MethodSymbol)("C.F")
            Dim f1 = compilation1.GetMember(Of MethodSymbol)("C.F")

            Dim generation0 = EmitBaseline.CreateInitialBaseline(md0, AddressOf v0.CreateSymReader().GetEncMethodDebugInfo)
            Dim diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables:=True)))

            diff1.EmitResult.Diagnostics.Verify(
                Diagnostic(ERRID.ERR_ModuleEmitFailure).WithArguments(compilation0.SourceModule.Name))
        End Sub

        <Fact, WorkItem(9119, "https://github.com/dotnet/roslyn/issues/9119")>
        Public Sub MissingAsyncStateMachineAttribute()
            Dim common = "
Imports System
Imports System.Threading.Tasks

Namespace Microsoft.VisualBasic.CompilerServices
    Class ProjectData
        Shared Sub  SetProjectError(e As Exception)
        End Sub

        Shared Sub  ClearProjectError()
        End Sub
    End Class
End Namespace"

            Dim source0 = MarkedSource(common & "
Class C
    Public Async Function F() As Task(Of Integer)
        Dim <N:0>a</N:0> As Integer = 0
        Await New Task()
        Return a
    End Function
End Class
")
            Dim source1 = MarkedSource(common & "
Class C
    Public Async Function F() As Task(Of Integer)
        Dim <N:0>a</N:0> As Integer = 1
        Await  New Task()
        Return a
    End Function
End Class
")
            Dim compilation0 = CompilationUtils.CreateCompilation({source0.Tree}, {TestReferences.NetFx.Minimal.mincorlib, TestReferences.NetFx.Minimal.minasync}, options:=ComSafeDebugDll)
            Dim compilation1 = compilation0.WithSource(source1.Tree)

            Assert.Null(compilation0.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_AsyncStateMachineAttribute__ctor))

            Dim v0 = CompileAndVerify(compilation0, verify:=False)
            Dim md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)

            Dim f0 = compilation0.GetMember(Of MethodSymbol)("C.F")
            Dim f1 = compilation1.GetMember(Of MethodSymbol)("C.F")

            Dim generation0 = EmitBaseline.CreateInitialBaseline(md0, AddressOf v0.CreateSymReader().GetEncMethodDebugInfo)
            Dim diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables:=True)))

            diff1.EmitResult.Diagnostics.Verify(
                Diagnostic(ERRID.ERR_ModuleEmitFailure).WithArguments(compilation0.SourceModule.Name),
                Diagnostic(ERRID.ERR_ModuleEmitFailure).WithArguments(compilation0.SourceModule.Name))
        End Sub
    End Class
End Namespace
