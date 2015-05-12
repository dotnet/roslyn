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
  // Code size       66 (0x42)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldfld      0x04000005
  IL_0006:  stloc.1
  IL_0007:  ldloc.1
  IL_0008:  brfalse.s  IL_0012
  IL_000a:  br.s       IL_000c
  IL_000c:  ldloc.1
  IL_000d:  ldc.i4.1
  IL_000e:  beq.s      IL_0015
  IL_0010:  br.s       IL_0018
  IL_0012:  nop
  IL_0013:  br.s       IL_001a
  IL_0015:  nop
  IL_0016:  br.s       IL_0037
  IL_0018:  ldc.i4.0
  IL_0019:  ret
  IL_001a:  ldarg.0
  IL_001b:  ldc.i4.m1
  IL_001c:  dup
  IL_001d:  stloc.1
  IL_001e:  stfld      0x04000005
  IL_0023:  nop
  IL_0024:  nop
  IL_0025:  ldarg.0
  IL_0026:  ldc.i4.2
  IL_0027:  stfld      0x04000006
  IL_002c:  ldarg.0
  IL_002d:  ldc.i4.1
  IL_002e:  dup
  IL_002f:  stloc.1
  IL_0030:  stfld      0x04000005
  IL_0035:  ldc.i4.1
  IL_0036:  ret
  IL_0037:  ldarg.0
  IL_0038:  ldc.i4.m1
  IL_0039:  dup
  IL_003a:  stloc.1
  IL_003b:  stfld      0x04000005
  IL_0040:  ldc.i4.0
  IL_0041:  ret
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
                <entry offset="0x23" startLine="6" startColumn="5" endLine="6" endColumn="53" document="1"/>
                <entry offset="0x24" startLine="6" startColumn="5" endLine="6" endColumn="53" document="1"/>
                <entry offset="0x25" startLine="7" startColumn="9" endLine="7" endColumn="16" document="1"/>
                <entry offset="0x40" startLine="8" startColumn="5" endLine="8" endColumn="17" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x42">
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
  // Code size      186 (0xba)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldfld      0x04000001
  IL_0006:  stloc.1
  IL_0007:  ldloc.1
  IL_0008:  brfalse.s  IL_000c
  IL_000a:  br.s       IL_000f
  IL_000c:  nop
  IL_000d:  br.s       IL_004b
  IL_000f:  nop
  IL_0010:  nop
  IL_0011:  ldc.i4.1
  IL_0012:  call       0x2B000002
  IL_0017:  callvirt   0x0A00000F
  IL_001c:  stloc.3
  IL_001d:  ldloca.s   V_3
  IL_001f:  call       0x0A000010
  IL_0024:  brtrue.s   IL_0069
  IL_0026:  ldarg.0
  IL_0027:  ldc.i4.0
  IL_0028:  dup
  IL_0029:  stloc.1
  IL_002a:  stfld      0x04000001
  IL_002f:  ldarg.0
  IL_0030:  ldloc.3
  IL_0031:  stfld      0x04000004
  IL_0036:  ldarg.0
  IL_0037:  ldflda     0x04000002
  IL_003c:  ldloca.s   V_3
  IL_003e:  ldarg.0
  IL_003f:  stloc.s    V_4
  IL_0041:  ldloca.s   V_4
  IL_0043:  call       0x2B000003
  IL_0048:  nop
  IL_0049:  leave.s    IL_00b9
  IL_004b:  ldarg.0
  IL_004c:  ldc.i4.m1
  IL_004d:  dup
  IL_004e:  stloc.1
  IL_004f:  stfld      0x04000001
  IL_0054:  ldarg.0
  IL_0055:  ldfld      0x04000004
  IL_005a:  stloc.3
  IL_005b:  ldarg.0
  IL_005c:  ldflda     0x04000004
  IL_0061:  initobj    0x1B000003
  IL_0067:  br.s       IL_0069
  IL_0069:  ldloca.s   V_3
  IL_006b:  call       0x0A000012
  IL_0070:  pop
  IL_0071:  ldloca.s   V_3
  IL_0073:  initobj    0x1B000003
  IL_0079:  ldc.i4.0
  IL_007a:  stloc.0
  IL_007b:  leave.s    IL_00a2
  IL_007d:  dup
  IL_007e:  call       0x0A000013
  IL_0083:  stloc.s    V_5
  IL_0085:  ldarg.0
  IL_0086:  ldc.i4.s   -2
  IL_0088:  stfld      0x04000001
  IL_008d:  ldarg.0
  IL_008e:  ldflda     0x04000002
  IL_0093:  ldloc.s    V_5
  IL_0095:  call       0x0A000014
  IL_009a:  nop
  IL_009b:  call       0x0A000015
  IL_00a0:  leave.s    IL_00b9
  IL_00a2:  ldarg.0
  IL_00a3:  ldc.i4.s   -2
  IL_00a5:  dup
  IL_00a6:  stloc.1
  IL_00a7:  stfld      0x04000001
  IL_00ac:  ldarg.0
  IL_00ad:  ldflda     0x04000002
  IL_00b2:  ldloc.0
  IL_00b3:  call       0x0A000016
  IL_00b8:  nop
  IL_00b9:  ret
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
                <entry offset="0xf" startLine="3" startColumn="5" endLine="3" endColumn="43" document="1"/>
                <entry offset="0x10" startLine="4" startColumn="9" endLine="4" endColumn="33" document="1"/>
                <entry offset="0x1d" hidden="true" document="1"/>
                <entry offset="0x79" startLine="5" startColumn="9" endLine="5" endColumn="17" document="1"/>
                <entry offset="0x7d" hidden="true" document="1"/>
                <entry offset="0x85" hidden="true" document="1"/>
                <entry offset="0xa2" startLine="6" startColumn="5" endLine="6" endColumn="17" document="1"/>
                <entry offset="0xac" hidden="true" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xba">
                <namespace name="System.Threading.Tasks" importlevel="file"/>
                <currentnamespace name=""/>
            </scope>
            <asyncInfo>
                <kickoffMethod token="0x6000002"/>
                <await yield="0x2f" resume="0x4b" token="0x6000004"/>
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
  // Code size       66 (0x42)
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
  IL_000e:  beq.s      IL_0015
  IL_0010:  br.s       IL_0018
  IL_0012:  nop
  IL_0013:  br.s       IL_001a
  IL_0015:  nop
  IL_0016:  br.s       IL_0037
  IL_0018:  ldc.i4.0
  IL_0019:  ret
  IL_001a:  ldarg.0
  IL_001b:  ldc.i4.m1
  IL_001c:  dup
  IL_001d:  stloc.1
  IL_001e:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_0023:  nop
  IL_0024:  nop
  IL_0025:  ldarg.0
  IL_0026:  ldc.i4.2
  IL_0027:  stfld      ""C.VB$StateMachine_1_F.$Current As Integer""
  IL_002c:  ldarg.0
  IL_002d:  ldc.i4.1
  IL_002e:  dup
  IL_002f:  stloc.1
  IL_0030:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_0035:  ldc.i4.1
  IL_0036:  ret
  IL_0037:  ldarg.0
  IL_0038:  ldc.i4.m1
  IL_0039:  dup
  IL_003a:  stloc.1
  IL_003b:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_0040:  ldc.i4.0
  IL_0041:  ret
}
")
                v0.VerifyIL("C.VB$StateMachine_1_F.MoveNext()", "
{
  // Code size       66 (0x42)
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
  IL_000e:  beq.s      IL_0015
  IL_0010:  br.s       IL_0018
  IL_0012:  nop
  IL_0013:  br.s       IL_001a
  IL_0015:  nop
  IL_0016:  br.s       IL_0037
  IL_0018:  ldc.i4.0
  IL_0019:  ret
  IL_001a:  ldarg.0
  IL_001b:  ldc.i4.m1
  IL_001c:  dup
  IL_001d:  stloc.1
  IL_001e:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_0023:  nop
  IL_0024:  nop
  IL_0025:  ldarg.0
  IL_0026:  ldc.i4.1
  IL_0027:  stfld      ""C.VB$StateMachine_1_F.$Current As Integer""
  IL_002c:  ldarg.0
  IL_002d:  ldc.i4.1
  IL_002e:  dup
  IL_002f:  stloc.1
  IL_0030:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_0035:  ldc.i4.1
  IL_0036:  ret
  IL_0037:  ldarg.0
  IL_0038:  ldc.i4.m1
  IL_0039:  dup
  IL_003a:  stloc.1
  IL_003b:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_0040:  ldc.i4.0
  IL_0041:  ret
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
  // Code size      188 (0xbc)
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
    IL_000a:  br.s       IL_000f
    IL_000c:  nop
    IL_000d:  br.s       IL_004c
    IL_000f:  nop
    IL_0010:  nop
    IL_0011:  ldc.i4.s   10
    IL_0013:  call       ""Function System.Threading.Tasks.Task.FromResult(Of Integer)(Integer) As System.Threading.Tasks.Task(Of Integer)""
    IL_0018:  callvirt   ""Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_001d:  stloc.3
    IL_001e:  ldloca.s   V_3
    IL_0020:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean""
    IL_0025:  brtrue.s   IL_006a
    IL_0027:  ldarg.0
    IL_0028:  ldc.i4.0
    IL_0029:  dup
    IL_002a:  stloc.1
    IL_002b:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
    IL_0030:  ldarg.0
    IL_0031:  ldloc.3
    IL_0032:  stfld      ""C.VB$StateMachine_1_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_0037:  ldarg.0
    IL_0038:  ldflda     ""C.VB$StateMachine_1_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
    IL_003d:  ldloca.s   V_3
    IL_003f:  ldarg.0
    IL_0040:  stloc.s    V_4
    IL_0042:  ldloca.s   V_4
    IL_0044:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), C.VB$StateMachine_1_F)(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef C.VB$StateMachine_1_F)""
    IL_0049:  nop
    IL_004a:  leave.s    IL_00bb
    IL_004c:  ldarg.0
    IL_004d:  ldc.i4.m1
    IL_004e:  dup
    IL_004f:  stloc.1
    IL_0050:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
    IL_0055:  ldarg.0
    IL_0056:  ldfld      ""C.VB$StateMachine_1_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_005b:  stloc.3
    IL_005c:  ldarg.0
    IL_005d:  ldflda     ""C.VB$StateMachine_1_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_0062:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_0068:  br.s       IL_006a
    IL_006a:  ldloca.s   V_3
    IL_006c:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer""
    IL_0071:  pop
    IL_0072:  ldloca.s   V_3
    IL_0074:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_007a:  ldc.i4.s   20
    IL_007c:  stloc.0
    IL_007d:  leave.s    IL_00a4
  }
  catch System.Exception
  {
    IL_007f:  dup
    IL_0080:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)""
    IL_0085:  stloc.s    V_5
    IL_0087:  ldarg.0
    IL_0088:  ldc.i4.s   -2
    IL_008a:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
    IL_008f:  ldarg.0
    IL_0090:  ldflda     ""C.VB$StateMachine_1_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
    IL_0095:  ldloc.s    V_5
    IL_0097:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetException(System.Exception)""
    IL_009c:  nop
    IL_009d:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()""
    IL_00a2:  leave.s    IL_00bb
  }
  IL_00a4:  ldarg.0
  IL_00a5:  ldc.i4.s   -2
  IL_00a7:  dup
  IL_00a8:  stloc.1
  IL_00a9:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_00ae:  ldarg.0
  IL_00af:  ldflda     ""C.VB$StateMachine_1_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
  IL_00b4:  ldloc.0
  IL_00b5:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetResult(Integer)""
  IL_00ba:  nop
  IL_00bb:  ret
}
")
                v0.VerifyIL("C.VB$StateMachine_1_F.MoveNext()", "
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
    IL_000a:  br.s       IL_000f
    IL_000c:  nop
    IL_000d:  br.s       IL_004b
    IL_000f:  nop
    IL_0010:  nop
    IL_0011:  ldc.i4.1
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
    IL_0049:  leave.s    IL_00b9
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
    IL_0079:  ldc.i4.2
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
  // Code size       78 (0x4e)
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
  IL_000e:  beq.s      IL_0015
  IL_0010:  br.s       IL_0018
  IL_0012:  nop
  IL_0013:  br.s       IL_001a
  IL_0015:  nop
  IL_0016:  br.s       IL_0043
  IL_0018:  ldc.i4.0
  IL_0019:  ret
  IL_001a:  ldarg.0
  IL_001b:  ldc.i4.m1
  IL_001c:  dup
  IL_001d:  stloc.1
  IL_001e:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_0023:  nop
  IL_0024:  nop
  IL_0025:  ldarg.0
  IL_0026:  ldarg.0
  IL_0027:  ldfld      ""C.VB$StateMachine_1_F.$VB$Local_p As Integer""
  IL_002c:  stfld      ""C.VB$StateMachine_1_F.$VB$ResumableLocal_x$0 As Integer""
  IL_0031:  ldarg.0
  IL_0032:  ldc.i4.2
  IL_0033:  stfld      ""C.VB$StateMachine_1_F.$Current As Integer""
  IL_0038:  ldarg.0
  IL_0039:  ldc.i4.1
  IL_003a:  dup
  IL_003b:  stloc.1
  IL_003c:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_0041:  ldc.i4.1
  IL_0042:  ret
  IL_0043:  ldarg.0
  IL_0044:  ldc.i4.m1
  IL_0045:  dup
  IL_0046:  stloc.1
  IL_0047:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_004c:  ldc.i4.0
  IL_004d:  ret
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
  IL_000e:  beq.s      IL_0015
  IL_0010:  br.s       IL_0018
  IL_0012:  nop
  IL_0013:  br.s       IL_001a
  IL_0015:  nop
  IL_0016:  br.s       IL_0053
  IL_0018:  ldc.i4.0
  IL_0019:  ret
  IL_001a:  ldarg.0
  IL_001b:  ldc.i4.m1
  IL_001c:  dup
  IL_001d:  stloc.1
  IL_001e:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_0023:  nop
  IL_0024:  nop
  IL_0025:  ldarg.0
  IL_0026:  ldarg.0
  IL_0027:  ldfld      ""C.VB$StateMachine_1_F.$VB$Local_p As Integer""
  IL_002c:  stfld      ""C.VB$StateMachine_1_F.$VB$ResumableLocal_x$0 As Integer""
  IL_0031:  ldarg.0
  IL_0032:  ldc.i4     0x4d2
  IL_0037:  stfld      ""C.VB$StateMachine_1_F.$VB$ResumableLocal_y$1 As Integer""
  IL_003c:  ldarg.0
  IL_003d:  ldarg.0
  IL_003e:  ldfld      ""C.VB$StateMachine_1_F.$VB$ResumableLocal_y$1 As Integer""
  IL_0043:  stfld      ""C.VB$StateMachine_1_F.$Current As Integer""
  IL_0048:  ldarg.0
  IL_0049:  ldc.i4.1
  IL_004a:  dup
  IL_004b:  stloc.1
  IL_004c:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_0051:  ldc.i4.1
  IL_0052:  ret
  IL_0053:  ldarg.0
  IL_0054:  ldc.i4.m1
  IL_0055:  dup
  IL_0056:  stloc.1
  IL_0057:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_005c:  ldc.i4.0
  IL_005d:  ret
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
  // Code size       82 (0x52)
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
  IL_000e:  beq.s      IL_0015
  IL_0010:  br.s       IL_0018
  IL_0012:  nop
  IL_0013:  br.s       IL_001a
  IL_0015:  nop
  IL_0016:  br.s       IL_0047
  IL_0018:  ldc.i4.0
  IL_0019:  ret
  IL_001a:  ldarg.0
  IL_001b:  ldc.i4.m1
  IL_001c:  dup
  IL_001d:  stloc.1
  IL_001e:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_0023:  nop
  IL_0024:  nop
  IL_0025:  ldarg.0
  IL_0026:  ldc.i4     0x4d2
  IL_002b:  stfld      ""C.VB$StateMachine_1_F.$VB$ResumableLocal_y$1 As Integer""
  IL_0030:  ldarg.0
  IL_0031:  ldarg.0
  IL_0032:  ldfld      ""C.VB$StateMachine_1_F.$VB$Local_p As Integer""
  IL_0037:  stfld      ""C.VB$StateMachine_1_F.$Current As Integer""
  IL_003c:  ldarg.0
  IL_003d:  ldc.i4.1
  IL_003e:  dup
  IL_003f:  stloc.1
  IL_0040:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_0045:  ldc.i4.1
  IL_0046:  ret
  IL_0047:  ldarg.0
  IL_0048:  ldc.i4.m1
  IL_0049:  dup
  IL_004a:  stloc.1
  IL_004b:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_0050:  ldc.i4.0
  IL_0051:  ret
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
  // Code size       77 (0x4d)
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
  IL_000e:  beq.s      IL_0015
  IL_0010:  br.s       IL_0018
  IL_0012:  nop
  IL_0013:  br.s       IL_001a
  IL_0015:  nop
  IL_0016:  br.s       IL_0042
  IL_0018:  ldc.i4.0
  IL_0019:  ret
  IL_001a:  ldarg.0
  IL_001b:  ldc.i4.m1
  IL_001c:  dup
  IL_001d:  stloc.1
  IL_001e:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_0023:  nop
  IL_0024:  nop
  IL_0025:  ldarg.0
  IL_0026:  ldc.i4     0x4d2
  IL_002b:  stfld      ""C.VB$StateMachine_1_F.$VB$ResumableLocal_x$1 As Integer""
  IL_0030:  ldarg.0
  IL_0031:  ldc.i4.0
  IL_0032:  stfld      ""C.VB$StateMachine_1_F.$Current As Integer""
  IL_0037:  ldarg.0
  IL_0038:  ldc.i4.1
  IL_0039:  dup
  IL_003a:  stloc.1
  IL_003b:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_0040:  ldc.i4.1
  IL_0041:  ret
  IL_0042:  ldarg.0
  IL_0043:  ldc.i4.m1
  IL_0044:  dup
  IL_0045:  stloc.1
  IL_0046:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_004b:  ldc.i4.0
  IL_004c:  ret
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
  // Code size      165 (0xa5)
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
  IL_000e:  beq.s      IL_0015
  IL_0010:  br.s       IL_0018
  IL_0012:  nop
  IL_0013:  br.s       IL_001a
  IL_0015:  nop
  IL_0016:  br.s       IL_0077
  IL_0018:  ldc.i4.0
  IL_0019:  ret
  IL_001a:  ldarg.0
  IL_001b:  ldc.i4.m1
  IL_001c:  dup
  IL_001d:  stloc.1
  IL_001e:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_0023:  nop
  IL_0024:  nop
  IL_0025:  ldarg.0
  IL_0026:  ldc.i4.2
  IL_0027:  newarr     ""Double""
  IL_002c:  dup
  IL_002d:  ldc.i4.0
  IL_002e:  ldc.r8     1
  IL_0037:  stelem.r8
  IL_0038:  dup
  IL_0039:  ldc.i4.1
  IL_003a:  ldc.r8     2
  IL_0043:  stelem.r8
  IL_0044:  stfld      ""C.VB$StateMachine_1_F.$S3 As Double()""
  IL_0049:  ldarg.0
  IL_004a:  ldc.i4.0
  IL_004b:  stfld      ""C.VB$StateMachine_1_F.$S1 As Integer""
  IL_0050:  br.s       IL_008f
  IL_0052:  ldarg.0
  IL_0053:  ldarg.0
  IL_0054:  ldfld      ""C.VB$StateMachine_1_F.$S3 As Double()""
  IL_0059:  ldarg.0
  IL_005a:  ldfld      ""C.VB$StateMachine_1_F.$S1 As Integer""
  IL_005f:  ldelem.r8
  IL_0060:  stfld      ""C.VB$StateMachine_1_F.$VB$ResumableLocal_x$4 As Double""
  IL_0065:  ldarg.0
  IL_0066:  ldc.i4.1
  IL_0067:  stfld      ""C.VB$StateMachine_1_F.$Current As Integer""
  IL_006c:  ldarg.0
  IL_006d:  ldc.i4.1
  IL_006e:  dup
  IL_006f:  stloc.1
  IL_0070:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_0075:  ldc.i4.1
  IL_0076:  ret
  IL_0077:  ldarg.0
  IL_0078:  ldc.i4.m1
  IL_0079:  dup
  IL_007a:  stloc.1
  IL_007b:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_0080:  nop
  IL_0081:  ldarg.0
  IL_0082:  ldarg.0
  IL_0083:  ldfld      ""C.VB$StateMachine_1_F.$S1 As Integer""
  IL_0088:  ldc.i4.1
  IL_0089:  add.ovf
  IL_008a:  stfld      ""C.VB$StateMachine_1_F.$S1 As Integer""
  IL_008f:  ldarg.0
  IL_0090:  ldfld      ""C.VB$StateMachine_1_F.$S1 As Integer""
  IL_0095:  ldarg.0
  IL_0096:  ldfld      ""C.VB$StateMachine_1_F.$S3 As Double()""
  IL_009b:  ldlen
  IL_009c:  conv.i4
  IL_009d:  clt
  IL_009f:  stloc.2
  IL_00a0:  ldloc.2
  IL_00a1:  brtrue.s   IL_0052
  IL_00a3:  ldc.i4.0
  IL_00a4:  ret
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
  // Code size      201 (0xc9)
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
    IL_000a:  br.s       IL_000f
    IL_000c:  nop
    IL_000d:  br.s       IL_0058
    IL_000f:  nop
    IL_0010:  ldarg.0
    IL_0011:  ldarg.0
    IL_0012:  ldfld      ""C.VB$StateMachine_1_F.$VB$Local_p As Integer""
    IL_0017:  stfld      ""C.VB$StateMachine_1_F.$VB$ResumableLocal_x$0 As Integer""
    IL_001c:  nop
    IL_001d:  ldc.i4.s   20
    IL_001f:  call       ""Function System.Threading.Tasks.Task.FromResult(Of Integer)(Integer) As System.Threading.Tasks.Task(Of Integer)""
    IL_0024:  callvirt   ""Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_0029:  stloc.3
    IL_002a:  ldloca.s   V_3
    IL_002c:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean""
    IL_0031:  brtrue.s   IL_0076
    IL_0033:  ldarg.0
    IL_0034:  ldc.i4.0
    IL_0035:  dup
    IL_0036:  stloc.1
    IL_0037:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
    IL_003c:  ldarg.0
    IL_003d:  ldloc.3
    IL_003e:  stfld      ""C.VB$StateMachine_1_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_0043:  ldarg.0
    IL_0044:  ldflda     ""C.VB$StateMachine_1_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
    IL_0049:  ldloca.s   V_3
    IL_004b:  ldarg.0
    IL_004c:  stloc.s    V_4
    IL_004e:  ldloca.s   V_4
    IL_0050:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), C.VB$StateMachine_1_F)(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef C.VB$StateMachine_1_F)""
    IL_0055:  nop
    IL_0056:  leave.s    IL_00c8
    IL_0058:  ldarg.0
    IL_0059:  ldc.i4.m1
    IL_005a:  dup
    IL_005b:  stloc.1
    IL_005c:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
    IL_0061:  ldarg.0
    IL_0062:  ldfld      ""C.VB$StateMachine_1_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_0067:  stloc.3
    IL_0068:  ldarg.0
    IL_0069:  ldflda     ""C.VB$StateMachine_1_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_006e:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_0074:  br.s       IL_0076
    IL_0076:  ldloca.s   V_3
    IL_0078:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer""
    IL_007d:  stloc.s    V_5
    IL_007f:  ldloca.s   V_3
    IL_0081:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_0087:  ldloc.s    V_5
    IL_0089:  stloc.0
    IL_008a:  leave.s    IL_00b1
  }
  catch System.Exception
  {
    IL_008c:  dup
    IL_008d:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)""
    IL_0092:  stloc.s    V_6
    IL_0094:  ldarg.0
    IL_0095:  ldc.i4.s   -2
    IL_0097:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
    IL_009c:  ldarg.0
    IL_009d:  ldflda     ""C.VB$StateMachine_1_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
    IL_00a2:  ldloc.s    V_6
    IL_00a4:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetException(System.Exception)""
    IL_00a9:  nop
    IL_00aa:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()""
    IL_00af:  leave.s    IL_00c8
  }
  IL_00b1:  ldarg.0
  IL_00b2:  ldc.i4.s   -2
  IL_00b4:  dup
  IL_00b5:  stloc.1
  IL_00b6:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_00bb:  ldarg.0
  IL_00bc:  ldflda     ""C.VB$StateMachine_1_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
  IL_00c1:  ldloc.0
  IL_00c2:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetResult(Integer)""
  IL_00c7:  nop
  IL_00c8:  ret
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
  // Code size      213 (0xd5)
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
    IL_000a:  br.s       IL_000f
    IL_000c:  nop
    IL_000d:  br.s       IL_0064
    IL_000f:  nop
    IL_0010:  ldarg.0
    IL_0011:  ldarg.0
    IL_0012:  ldfld      ""C.VB$StateMachine_1_F.$VB$Local_p As Integer""
    IL_0017:  stfld      ""C.VB$StateMachine_1_F.$VB$ResumableLocal_x$0 As Integer""
    IL_001c:  ldarg.0
    IL_001d:  ldc.i4.s   10
    IL_001f:  stfld      ""C.VB$StateMachine_1_F.$VB$ResumableLocal_y$1 As Integer""
    IL_0024:  nop
    IL_0025:  ldarg.0
    IL_0026:  ldfld      ""C.VB$StateMachine_1_F.$VB$ResumableLocal_y$1 As Integer""
    IL_002b:  call       ""Function System.Threading.Tasks.Task.FromResult(Of Integer)(Integer) As System.Threading.Tasks.Task(Of Integer)""
    IL_0030:  callvirt   ""Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_0035:  stloc.3
    IL_0036:  ldloca.s   V_3
    IL_0038:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean""
    IL_003d:  brtrue.s   IL_0082
    IL_003f:  ldarg.0
    IL_0040:  ldc.i4.0
    IL_0041:  dup
    IL_0042:  stloc.1
    IL_0043:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
    IL_0048:  ldarg.0
    IL_0049:  ldloc.3
    IL_004a:  stfld      ""C.VB$StateMachine_1_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_004f:  ldarg.0
    IL_0050:  ldflda     ""C.VB$StateMachine_1_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
    IL_0055:  ldloca.s   V_3
    IL_0057:  ldarg.0
    IL_0058:  stloc.s    V_4
    IL_005a:  ldloca.s   V_4
    IL_005c:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), C.VB$StateMachine_1_F)(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef C.VB$StateMachine_1_F)""
    IL_0061:  nop
    IL_0062:  leave.s    IL_00d4
    IL_0064:  ldarg.0
    IL_0065:  ldc.i4.m1
    IL_0066:  dup
    IL_0067:  stloc.1
    IL_0068:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
    IL_006d:  ldarg.0
    IL_006e:  ldfld      ""C.VB$StateMachine_1_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_0073:  stloc.3
    IL_0074:  ldarg.0
    IL_0075:  ldflda     ""C.VB$StateMachine_1_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_007a:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_0080:  br.s       IL_0082
    IL_0082:  ldloca.s   V_3
    IL_0084:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer""
    IL_0089:  stloc.s    V_5
    IL_008b:  ldloca.s   V_3
    IL_008d:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_0093:  ldloc.s    V_5
    IL_0095:  stloc.0
    IL_0096:  leave.s    IL_00bd
  }
  catch System.Exception
  {
    IL_0098:  dup
    IL_0099:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)""
    IL_009e:  stloc.s    V_6
    IL_00a0:  ldarg.0
    IL_00a1:  ldc.i4.s   -2
    IL_00a3:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
    IL_00a8:  ldarg.0
    IL_00a9:  ldflda     ""C.VB$StateMachine_1_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
    IL_00ae:  ldloc.s    V_6
    IL_00b0:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetException(System.Exception)""
    IL_00b5:  nop
    IL_00b6:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()""
    IL_00bb:  leave.s    IL_00d4
  }
  IL_00bd:  ldarg.0
  IL_00be:  ldc.i4.s   -2
  IL_00c0:  dup
  IL_00c1:  stloc.1
  IL_00c2:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_00c7:  ldarg.0
  IL_00c8:  ldflda     ""C.VB$StateMachine_1_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
  IL_00cd:  ldloc.0
  IL_00ce:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetResult(Integer)""
  IL_00d3:  nop
  IL_00d4:  ret
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
  // Code size      204 (0xcc)
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
    IL_000a:  br.s       IL_000f
    IL_000c:  nop
    IL_000d:  br.s       IL_005b
    IL_000f:  nop
    IL_0010:  ldarg.0
    IL_0011:  ldc.i4     0x4d2
    IL_0016:  stfld      ""C.VB$StateMachine_1_F.$VB$ResumableLocal_y$1 As Integer""
    IL_001b:  nop
    IL_001c:  ldarg.0
    IL_001d:  ldfld      ""C.VB$StateMachine_1_F.$VB$Local_p As Integer""
    IL_0022:  call       ""Function System.Threading.Tasks.Task.FromResult(Of Integer)(Integer) As System.Threading.Tasks.Task(Of Integer)""
    IL_0027:  callvirt   ""Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_002c:  stloc.3
    IL_002d:  ldloca.s   V_3
    IL_002f:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean""
    IL_0034:  brtrue.s   IL_0079
    IL_0036:  ldarg.0
    IL_0037:  ldc.i4.0
    IL_0038:  dup
    IL_0039:  stloc.1
    IL_003a:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
    IL_003f:  ldarg.0
    IL_0040:  ldloc.3
    IL_0041:  stfld      ""C.VB$StateMachine_1_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_0046:  ldarg.0
    IL_0047:  ldflda     ""C.VB$StateMachine_1_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
    IL_004c:  ldloca.s   V_3
    IL_004e:  ldarg.0
    IL_004f:  stloc.s    V_4
    IL_0051:  ldloca.s   V_4
    IL_0053:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), C.VB$StateMachine_1_F)(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef C.VB$StateMachine_1_F)""
    IL_0058:  nop
    IL_0059:  leave.s    IL_00cb
    IL_005b:  ldarg.0
    IL_005c:  ldc.i4.m1
    IL_005d:  dup
    IL_005e:  stloc.1
    IL_005f:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
    IL_0064:  ldarg.0
    IL_0065:  ldfld      ""C.VB$StateMachine_1_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_006a:  stloc.3
    IL_006b:  ldarg.0
    IL_006c:  ldflda     ""C.VB$StateMachine_1_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_0071:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_0077:  br.s       IL_0079
    IL_0079:  ldloca.s   V_3
    IL_007b:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer""
    IL_0080:  stloc.s    V_5
    IL_0082:  ldloca.s   V_3
    IL_0084:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_008a:  ldloc.s    V_5
    IL_008c:  stloc.0
    IL_008d:  leave.s    IL_00b4
  }
  catch System.Exception
  {
    IL_008f:  dup
    IL_0090:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)""
    IL_0095:  stloc.s    V_6
    IL_0097:  ldarg.0
    IL_0098:  ldc.i4.s   -2
    IL_009a:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
    IL_009f:  ldarg.0
    IL_00a0:  ldflda     ""C.VB$StateMachine_1_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
    IL_00a5:  ldloc.s    V_6
    IL_00a7:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetException(System.Exception)""
    IL_00ac:  nop
    IL_00ad:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()""
    IL_00b2:  leave.s    IL_00cb
  }
  IL_00b4:  ldarg.0
  IL_00b5:  ldc.i4.s   -2
  IL_00b7:  dup
  IL_00b8:  stloc.1
  IL_00b9:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_00be:  ldarg.0
  IL_00bf:  ldflda     ""C.VB$StateMachine_1_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
  IL_00c4:  ldloc.0
  IL_00c5:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetResult(Integer)""
  IL_00ca:  nop
  IL_00cb:  ret
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
  // Code size      204 (0xcc)
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
    IL_000a:  br.s       IL_000f
    IL_000c:  nop
    IL_000d:  br.s       IL_005b
    IL_000f:  nop
    IL_0010:  ldarg.0
    IL_0011:  ldc.r8     10
    IL_001a:  stfld      ""C.VB$StateMachine_1_F.$VB$ResumableLocal_x$1 As Double""
    IL_001f:  nop
    IL_0020:  ldc.i4.s   20
    IL_0022:  call       ""Function System.Threading.Tasks.Task.FromResult(Of Integer)(Integer) As System.Threading.Tasks.Task(Of Integer)""
    IL_0027:  callvirt   ""Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_002c:  stloc.3
    IL_002d:  ldloca.s   V_3
    IL_002f:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean""
    IL_0034:  brtrue.s   IL_0079
    IL_0036:  ldarg.0
    IL_0037:  ldc.i4.0
    IL_0038:  dup
    IL_0039:  stloc.1
    IL_003a:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
    IL_003f:  ldarg.0
    IL_0040:  ldloc.3
    IL_0041:  stfld      ""C.VB$StateMachine_1_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_0046:  ldarg.0
    IL_0047:  ldflda     ""C.VB$StateMachine_1_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
    IL_004c:  ldloca.s   V_3
    IL_004e:  ldarg.0
    IL_004f:  stloc.s    V_4
    IL_0051:  ldloca.s   V_4
    IL_0053:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), C.VB$StateMachine_1_F)(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef C.VB$StateMachine_1_F)""
    IL_0058:  nop
    IL_0059:  leave.s    IL_00cb
    IL_005b:  ldarg.0
    IL_005c:  ldc.i4.m1
    IL_005d:  dup
    IL_005e:  stloc.1
    IL_005f:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
    IL_0064:  ldarg.0
    IL_0065:  ldfld      ""C.VB$StateMachine_1_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_006a:  stloc.3
    IL_006b:  ldarg.0
    IL_006c:  ldflda     ""C.VB$StateMachine_1_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_0071:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_0077:  br.s       IL_0079
    IL_0079:  ldloca.s   V_3
    IL_007b:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer""
    IL_0080:  stloc.s    V_5
    IL_0082:  ldloca.s   V_3
    IL_0084:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_008a:  ldloc.s    V_5
    IL_008c:  stloc.0
    IL_008d:  leave.s    IL_00b4
  }
  catch System.Exception
  {
    IL_008f:  dup
    IL_0090:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)""
    IL_0095:  stloc.s    V_6
    IL_0097:  ldarg.0
    IL_0098:  ldc.i4.s   -2
    IL_009a:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
    IL_009f:  ldarg.0
    IL_00a0:  ldflda     ""C.VB$StateMachine_1_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
    IL_00a5:  ldloc.s    V_6
    IL_00a7:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetException(System.Exception)""
    IL_00ac:  nop
    IL_00ad:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()""
    IL_00b2:  leave.s    IL_00cb
  }
  IL_00b4:  ldarg.0
  IL_00b5:  ldc.i4.s   -2
  IL_00b7:  dup
  IL_00b8:  stloc.1
  IL_00b9:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_00be:  ldarg.0
  IL_00bf:  ldflda     ""C.VB$StateMachine_1_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
  IL_00c4:  ldloc.0
  IL_00c5:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetResult(Integer)""
  IL_00ca:  nop
  IL_00cb:  ret
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
  // Code size      204 (0xcc)
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
    IL_000a:  br.s       IL_000f
    IL_000c:  nop
    IL_000d:  br.s       IL_005d
    IL_000f:  nop
    IL_0010:  ldarg.0
    IL_0011:  newobj     ""Sub C..ctor()""
    IL_0016:  stfld      ""C.VB$StateMachine_1_F.$VB$ResumableLocal_a1$2 As C""
    IL_001b:  ldarg.0
    IL_001c:  ldc.i4.3
    IL_001d:  stfld      ""C.VB$StateMachine_1_F.$VB$ResumableLocal_a2$1 As Integer""
    IL_0022:  nop
    IL_0023:  ldc.i4.0
    IL_0024:  call       ""Function System.Threading.Tasks.Task.Delay(Integer) As System.Threading.Tasks.Task""
    IL_0029:  callvirt   ""Function System.Threading.Tasks.Task.GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter""
    IL_002e:  stloc.3
    IL_002f:  ldloca.s   V_3
    IL_0031:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter.get_IsCompleted() As Boolean""
    IL_0036:  brtrue.s   IL_007b
    IL_0038:  ldarg.0
    IL_0039:  ldc.i4.0
    IL_003a:  dup
    IL_003b:  stloc.1
    IL_003c:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
    IL_0041:  ldarg.0
    IL_0042:  ldloc.3
    IL_0043:  stfld      ""C.VB$StateMachine_1_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
    IL_0048:  ldarg.0
    IL_0049:  ldflda     ""C.VB$StateMachine_1_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
    IL_004e:  ldloca.s   V_3
    IL_0050:  ldarg.0
    IL_0051:  stloc.s    V_4
    IL_0053:  ldloca.s   V_4
    IL_0055:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter, C.VB$StateMachine_1_F)(ByRef System.Runtime.CompilerServices.TaskAwaiter, ByRef C.VB$StateMachine_1_F)""
    IL_005a:  nop
    IL_005b:  leave.s    IL_00cb
    IL_005d:  ldarg.0
    IL_005e:  ldc.i4.m1
    IL_005f:  dup
    IL_0060:  stloc.1
    IL_0061:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
    IL_0066:  ldarg.0
    IL_0067:  ldfld      ""C.VB$StateMachine_1_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
    IL_006c:  stloc.3
    IL_006d:  ldarg.0
    IL_006e:  ldflda     ""C.VB$StateMachine_1_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
    IL_0073:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_0079:  br.s       IL_007b
    IL_007b:  ldloca.s   V_3
    IL_007d:  call       ""Sub System.Runtime.CompilerServices.TaskAwaiter.GetResult()""
    IL_0082:  nop
    IL_0083:  ldloca.s   V_3
    IL_0085:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_008b:  ldc.i4.1
    IL_008c:  stloc.0
    IL_008d:  leave.s    IL_00b4
  }
  catch System.Exception
  {
    IL_008f:  dup
    IL_0090:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)""
    IL_0095:  stloc.s    V_5
    IL_0097:  ldarg.0
    IL_0098:  ldc.i4.s   -2
    IL_009a:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
    IL_009f:  ldarg.0
    IL_00a0:  ldflda     ""C.VB$StateMachine_1_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
    IL_00a5:  ldloc.s    V_5
    IL_00a7:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetException(System.Exception)""
    IL_00ac:  nop
    IL_00ad:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()""
    IL_00b2:  leave.s    IL_00cb
  }
  IL_00b4:  ldarg.0
  IL_00b5:  ldc.i4.s   -2
  IL_00b7:  dup
  IL_00b8:  stloc.1
  IL_00b9:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_00be:  ldarg.0
  IL_00bf:  ldflda     ""C.VB$StateMachine_1_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
  IL_00c4:  ldloc.0
  IL_00c5:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetResult(Integer)""
  IL_00ca:  nop
  IL_00cb:  ret
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
  // Code size      204 (0xcc)
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
    IL_000a:  br.s       IL_000f
    IL_000c:  nop
    IL_000d:  br.s       IL_005d
    IL_000f:  nop
    IL_0010:  ldarg.0
    IL_0011:  ldc.i4.1
    IL_0012:  stfld      ""C.VB$StateMachine_1_F.$VB$ResumableLocal_a1$3 As Boolean""
    IL_0017:  ldarg.0
    IL_0018:  newobj     ""Sub C..ctor()""
    IL_001d:  stfld      ""C.VB$StateMachine_1_F.$VB$ResumableLocal_a2$4 As C""
    IL_0022:  nop
    IL_0023:  ldc.i4.0
    IL_0024:  call       ""Function System.Threading.Tasks.Task.Delay(Integer) As System.Threading.Tasks.Task""
    IL_0029:  callvirt   ""Function System.Threading.Tasks.Task.GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter""
    IL_002e:  stloc.3
    IL_002f:  ldloca.s   V_3
    IL_0031:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter.get_IsCompleted() As Boolean""
    IL_0036:  brtrue.s   IL_007b
    IL_0038:  ldarg.0
    IL_0039:  ldc.i4.0
    IL_003a:  dup
    IL_003b:  stloc.1
    IL_003c:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
    IL_0041:  ldarg.0
    IL_0042:  ldloc.3
    IL_0043:  stfld      ""C.VB$StateMachine_1_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
    IL_0048:  ldarg.0
    IL_0049:  ldflda     ""C.VB$StateMachine_1_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
    IL_004e:  ldloca.s   V_3
    IL_0050:  ldarg.0
    IL_0051:  stloc.s    V_4
    IL_0053:  ldloca.s   V_4
    IL_0055:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter, C.VB$StateMachine_1_F)(ByRef System.Runtime.CompilerServices.TaskAwaiter, ByRef C.VB$StateMachine_1_F)""
    IL_005a:  nop
    IL_005b:  leave.s    IL_00cb
    IL_005d:  ldarg.0
    IL_005e:  ldc.i4.m1
    IL_005f:  dup
    IL_0060:  stloc.1
    IL_0061:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
    IL_0066:  ldarg.0
    IL_0067:  ldfld      ""C.VB$StateMachine_1_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
    IL_006c:  stloc.3
    IL_006d:  ldarg.0
    IL_006e:  ldflda     ""C.VB$StateMachine_1_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
    IL_0073:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_0079:  br.s       IL_007b
    IL_007b:  ldloca.s   V_3
    IL_007d:  call       ""Sub System.Runtime.CompilerServices.TaskAwaiter.GetResult()""
    IL_0082:  nop
    IL_0083:  ldloca.s   V_3
    IL_0085:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_008b:  ldc.i4.1
    IL_008c:  stloc.0
    IL_008d:  leave.s    IL_00b4
  }
  catch System.Exception
  {
    IL_008f:  dup
    IL_0090:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)""
    IL_0095:  stloc.s    V_5
    IL_0097:  ldarg.0
    IL_0098:  ldc.i4.s   -2
    IL_009a:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
    IL_009f:  ldarg.0
    IL_00a0:  ldflda     ""C.VB$StateMachine_1_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
    IL_00a5:  ldloc.s    V_5
    IL_00a7:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetException(System.Exception)""
    IL_00ac:  nop
    IL_00ad:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()""
    IL_00b2:  leave.s    IL_00cb
  }
  IL_00b4:  ldarg.0
  IL_00b5:  ldc.i4.s   -2
  IL_00b7:  dup
  IL_00b8:  stloc.1
  IL_00b9:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_00be:  ldarg.0
  IL_00bf:  ldflda     ""C.VB$StateMachine_1_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
  IL_00c4:  ldloc.0
  IL_00c5:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetResult(Integer)""
  IL_00ca:  nop
  IL_00cb:  ret
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
  // Code size      319 (0x13f)
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
    IL_000e:  beq.s      IL_0015
    IL_0010:  br.s       IL_001b
    IL_0012:  nop
    IL_0013:  br.s       IL_005f
    IL_0015:  nop
    IL_0016:  br         IL_00cf
    IL_001b:  nop
    IL_001c:  nop
    IL_001d:  ldarg.0
    IL_001e:  ldfld      ""C.VB$StateMachine_4_F.$VB$Me As C""
    IL_0023:  callvirt   ""Function C.A3() As System.Threading.Tasks.Task(Of C)""
    IL_0028:  callvirt   ""Function System.Threading.Tasks.Task(Of C).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of C)""
    IL_002d:  stloc.3
    IL_002e:  ldloca.s   V_3
    IL_0030:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter(Of C).get_IsCompleted() As Boolean""
    IL_0035:  brtrue.s   IL_007d
    IL_0037:  ldarg.0
    IL_0038:  ldc.i4.0
    IL_0039:  dup
    IL_003a:  stloc.1
    IL_003b:  stfld      ""C.VB$StateMachine_4_F.$State As Integer""
    IL_0040:  ldarg.0
    IL_0041:  ldloc.3
    IL_0042:  stfld      ""C.VB$StateMachine_4_F.$A2 As System.Runtime.CompilerServices.TaskAwaiter(Of C)""
    IL_0047:  ldarg.0
    IL_0048:  ldflda     ""C.VB$StateMachine_4_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
    IL_004d:  ldloca.s   V_3
    IL_004f:  ldarg.0
    IL_0050:  stloc.s    V_4
    IL_0052:  ldloca.s   V_4
    IL_0054:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of C), C.VB$StateMachine_4_F)(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of C), ByRef C.VB$StateMachine_4_F)""
    IL_0059:  nop
    IL_005a:  leave      IL_013e
    IL_005f:  ldarg.0
    IL_0060:  ldc.i4.m1
    IL_0061:  dup
    IL_0062:  stloc.1
    IL_0063:  stfld      ""C.VB$StateMachine_4_F.$State As Integer""
    IL_0068:  ldarg.0
    IL_0069:  ldfld      ""C.VB$StateMachine_4_F.$A2 As System.Runtime.CompilerServices.TaskAwaiter(Of C)""
    IL_006e:  stloc.3
    IL_006f:  ldarg.0
    IL_0070:  ldflda     ""C.VB$StateMachine_4_F.$A2 As System.Runtime.CompilerServices.TaskAwaiter(Of C)""
    IL_0075:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter(Of C)""
    IL_007b:  br.s       IL_007d
    IL_007d:  ldloca.s   V_3
    IL_007f:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter(Of C).GetResult() As C""
    IL_0084:  pop
    IL_0085:  ldloca.s   V_3
    IL_0087:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter(Of C)""
    IL_008d:  nop
    IL_008e:  ldarg.0
    IL_008f:  ldfld      ""C.VB$StateMachine_4_F.$VB$Me As C""
    IL_0094:  callvirt   ""Function C.A2() As System.Threading.Tasks.Task(Of Integer)""
    IL_0099:  callvirt   ""Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_009e:  stloc.s    V_5
    IL_00a0:  ldloca.s   V_5
    IL_00a2:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean""
    IL_00a7:  brtrue.s   IL_00ee
    IL_00a9:  ldarg.0
    IL_00aa:  ldc.i4.1
    IL_00ab:  dup
    IL_00ac:  stloc.1
    IL_00ad:  stfld      ""C.VB$StateMachine_4_F.$State As Integer""
    IL_00b2:  ldarg.0
    IL_00b3:  ldloc.s    V_5
    IL_00b5:  stfld      ""C.VB$StateMachine_4_F.$A1 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_00ba:  ldarg.0
    IL_00bb:  ldflda     ""C.VB$StateMachine_4_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
    IL_00c0:  ldloca.s   V_5
    IL_00c2:  ldarg.0
    IL_00c3:  stloc.s    V_4
    IL_00c5:  ldloca.s   V_4
    IL_00c7:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), C.VB$StateMachine_4_F)(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef C.VB$StateMachine_4_F)""
    IL_00cc:  nop
    IL_00cd:  leave.s    IL_013e
    IL_00cf:  ldarg.0
    IL_00d0:  ldc.i4.m1
    IL_00d1:  dup
    IL_00d2:  stloc.1
    IL_00d3:  stfld      ""C.VB$StateMachine_4_F.$State As Integer""
    IL_00d8:  ldarg.0
    IL_00d9:  ldfld      ""C.VB$StateMachine_4_F.$A1 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_00de:  stloc.s    V_5
    IL_00e0:  ldarg.0
    IL_00e1:  ldflda     ""C.VB$StateMachine_4_F.$A1 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_00e6:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_00ec:  br.s       IL_00ee
    IL_00ee:  ldloca.s   V_5
    IL_00f0:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer""
    IL_00f5:  pop
    IL_00f6:  ldloca.s   V_5
    IL_00f8:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_00fe:  ldc.i4.1
    IL_00ff:  stloc.0
    IL_0100:  leave.s    IL_0127
  }
  catch System.Exception
  {
    IL_0102:  dup
    IL_0103:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)""
    IL_0108:  stloc.s    V_6
    IL_010a:  ldarg.0
    IL_010b:  ldc.i4.s   -2
    IL_010d:  stfld      ""C.VB$StateMachine_4_F.$State As Integer""
    IL_0112:  ldarg.0
    IL_0113:  ldflda     ""C.VB$StateMachine_4_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
    IL_0118:  ldloc.s    V_6
    IL_011a:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetException(System.Exception)""
    IL_011f:  nop
    IL_0120:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()""
    IL_0125:  leave.s    IL_013e
  }
  IL_0127:  ldarg.0
  IL_0128:  ldc.i4.s   -2
  IL_012a:  dup
  IL_012b:  stloc.1
  IL_012c:  stfld      ""C.VB$StateMachine_4_F.$State As Integer""
  IL_0131:  ldarg.0
  IL_0132:  ldflda     ""C.VB$StateMachine_4_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
  IL_0137:  ldloc.0
  IL_0138:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetResult(Integer)""
  IL_013d:  nop
  IL_013e:  ret
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
  // Code size      319 (0x13f)
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
    IL_000e:  beq.s      IL_0015
    IL_0010:  br.s       IL_001b
    IL_0012:  nop
    IL_0013:  br.s       IL_005f
    IL_0015:  nop
    IL_0016:  br         IL_00cf
    IL_001b:  nop
    IL_001c:  nop
    IL_001d:  ldarg.0
    IL_001e:  ldfld      ""C.VB$StateMachine_4_F.$VB$Me As C""
    IL_0023:  callvirt   ""Function C.A1() As System.Threading.Tasks.Task(Of Boolean)""
    IL_0028:  callvirt   ""Function System.Threading.Tasks.Task(Of Boolean).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Boolean)""
    IL_002d:  stloc.3
    IL_002e:  ldloca.s   V_3
    IL_0030:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter(Of Boolean).get_IsCompleted() As Boolean""
    IL_0035:  brtrue.s   IL_007d
    IL_0037:  ldarg.0
    IL_0038:  ldc.i4.0
    IL_0039:  dup
    IL_003a:  stloc.1
    IL_003b:  stfld      ""C.VB$StateMachine_4_F.$State As Integer""
    IL_0040:  ldarg.0
    IL_0041:  ldloc.3
    IL_0042:  stfld      ""C.VB$StateMachine_4_F.$A3 As System.Runtime.CompilerServices.TaskAwaiter(Of Boolean)""
    IL_0047:  ldarg.0
    IL_0048:  ldflda     ""C.VB$StateMachine_4_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
    IL_004d:  ldloca.s   V_3
    IL_004f:  ldarg.0
    IL_0050:  stloc.s    V_4
    IL_0052:  ldloca.s   V_4
    IL_0054:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Boolean), C.VB$StateMachine_4_F)(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Boolean), ByRef C.VB$StateMachine_4_F)""
    IL_0059:  nop
    IL_005a:  leave      IL_013e
    IL_005f:  ldarg.0
    IL_0060:  ldc.i4.m1
    IL_0061:  dup
    IL_0062:  stloc.1
    IL_0063:  stfld      ""C.VB$StateMachine_4_F.$State As Integer""
    IL_0068:  ldarg.0
    IL_0069:  ldfld      ""C.VB$StateMachine_4_F.$A3 As System.Runtime.CompilerServices.TaskAwaiter(Of Boolean)""
    IL_006e:  stloc.3
    IL_006f:  ldarg.0
    IL_0070:  ldflda     ""C.VB$StateMachine_4_F.$A3 As System.Runtime.CompilerServices.TaskAwaiter(Of Boolean)""
    IL_0075:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter(Of Boolean)""
    IL_007b:  br.s       IL_007d
    IL_007d:  ldloca.s   V_3
    IL_007f:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter(Of Boolean).GetResult() As Boolean""
    IL_0084:  pop
    IL_0085:  ldloca.s   V_3
    IL_0087:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter(Of Boolean)""
    IL_008d:  nop
    IL_008e:  ldarg.0
    IL_008f:  ldfld      ""C.VB$StateMachine_4_F.$VB$Me As C""
    IL_0094:  callvirt   ""Function C.A3() As System.Threading.Tasks.Task(Of C)""
    IL_0099:  callvirt   ""Function System.Threading.Tasks.Task(Of C).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of C)""
    IL_009e:  stloc.s    V_5
    IL_00a0:  ldloca.s   V_5
    IL_00a2:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter(Of C).get_IsCompleted() As Boolean""
    IL_00a7:  brtrue.s   IL_00ee
    IL_00a9:  ldarg.0
    IL_00aa:  ldc.i4.1
    IL_00ab:  dup
    IL_00ac:  stloc.1
    IL_00ad:  stfld      ""C.VB$StateMachine_4_F.$State As Integer""
    IL_00b2:  ldarg.0
    IL_00b3:  ldloc.s    V_5
    IL_00b5:  stfld      ""C.VB$StateMachine_4_F.$A2 As System.Runtime.CompilerServices.TaskAwaiter(Of C)""
    IL_00ba:  ldarg.0
    IL_00bb:  ldflda     ""C.VB$StateMachine_4_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
    IL_00c0:  ldloca.s   V_5
    IL_00c2:  ldarg.0
    IL_00c3:  stloc.s    V_4
    IL_00c5:  ldloca.s   V_4
    IL_00c7:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of C), C.VB$StateMachine_4_F)(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of C), ByRef C.VB$StateMachine_4_F)""
    IL_00cc:  nop
    IL_00cd:  leave.s    IL_013e
    IL_00cf:  ldarg.0
    IL_00d0:  ldc.i4.m1
    IL_00d1:  dup
    IL_00d2:  stloc.1
    IL_00d3:  stfld      ""C.VB$StateMachine_4_F.$State As Integer""
    IL_00d8:  ldarg.0
    IL_00d9:  ldfld      ""C.VB$StateMachine_4_F.$A2 As System.Runtime.CompilerServices.TaskAwaiter(Of C)""
    IL_00de:  stloc.s    V_5
    IL_00e0:  ldarg.0
    IL_00e1:  ldflda     ""C.VB$StateMachine_4_F.$A2 As System.Runtime.CompilerServices.TaskAwaiter(Of C)""
    IL_00e6:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter(Of C)""
    IL_00ec:  br.s       IL_00ee
    IL_00ee:  ldloca.s   V_5
    IL_00f0:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter(Of C).GetResult() As C""
    IL_00f5:  pop
    IL_00f6:  ldloca.s   V_5
    IL_00f8:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter(Of C)""
    IL_00fe:  ldc.i4.1
    IL_00ff:  stloc.0
    IL_0100:  leave.s    IL_0127
  }
  catch System.Exception
  {
    IL_0102:  dup
    IL_0103:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)""
    IL_0108:  stloc.s    V_6
    IL_010a:  ldarg.0
    IL_010b:  ldc.i4.s   -2
    IL_010d:  stfld      ""C.VB$StateMachine_4_F.$State As Integer""
    IL_0112:  ldarg.0
    IL_0113:  ldflda     ""C.VB$StateMachine_4_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
    IL_0118:  ldloc.s    V_6
    IL_011a:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetException(System.Exception)""
    IL_011f:  nop
    IL_0120:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()""
    IL_0125:  leave.s    IL_013e
  }
  IL_0127:  ldarg.0
  IL_0128:  ldc.i4.s   -2
  IL_012a:  dup
  IL_012b:  stloc.1
  IL_012c:  stfld      ""C.VB$StateMachine_4_F.$State As Integer""
  IL_0131:  ldarg.0
  IL_0132:  ldflda     ""C.VB$StateMachine_4_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
  IL_0137:  ldloc.0
  IL_0138:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetResult(Integer)""
  IL_013d:  nop
  IL_013e:  ret
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
    End Class
End Namespace
