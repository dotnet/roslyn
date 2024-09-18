' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Reflection.Metadata.Ecma335
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.EditAndContinue.UnitTests
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class EditAndContinueStateMachineTests
        Inherits EditAndContinueTestBase

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
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

            Dim compilation0 = CompilationUtils.CreateEmptyCompilationWithReferences(source0, references:=LatestVbReferences, options:=TestOptions.DebugDll)
            Dim compilation1 = CompilationUtils.CreateEmptyCompilationWithReferences(source1, references:=LatestVbReferences, options:=TestOptions.DebugDll)

            Dim bytes0 = compilation0.EmitToArray()
            Dim generation0 = CreateInitialBaseline(compilation0, ModuleMetadata.CreateFromImage(bytes0), EmptyLocalsProvider)
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

                diff1.VerifyPdb({&H600000EUI},
<symbols>
    <files>
        <file id="1" name="a.vb" language="VB" checksumAlgorithm="SHA1" checksum="18-50-69-51-C7-A5-E4-CF-63-8F-2D-D6-4D-C0-2F-1A-2F-4A-8B-FA"/>
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

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
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

            Dim compilation0 = CompilationUtils.CreateEmptyCompilationWithReferences(source0, references:=LatestVbReferences, options:=TestOptions.DebugDll)
            Dim compilation1 = CompilationUtils.CreateEmptyCompilationWithReferences(source1, references:=LatestVbReferences, options:=TestOptions.DebugDll)

            Dim bytes0 = compilation0.EmitToArray()
            Dim generation0 = CreateInitialBaseline(compilation0, ModuleMetadata.CreateFromImage(bytes0), EmptyLocalsProvider)
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

                diff1.VerifyPdb({&H6000004UI},
<symbols>
    <files>
        <file id="1" name="a.vb" language="VB" checksumAlgorithm="SHA1" checksum="E8-25-E4-A7-D1-61-DE-6D-8C-99-C8-28-60-8E-A4-2C-37-CC-4A-38"/>
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

            Dim compilation0 = CompilationUtils.CreateEmptyCompilationWithReferences(source0, references:=LatestVbReferences, options:=TestOptions.DebugDll)
            Dim compilation1 = CompilationUtils.CreateEmptyCompilationWithReferences(source1, references:=LatestVbReferences, options:=TestOptions.DebugDll)

            Dim v0 = CompileAndVerify(compilation:=compilation0)

            Using md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)
                Dim method0 = compilation0.GetMember(Of MethodSymbol)("C.F")
                Dim method1 = compilation1.GetMember(Of MethodSymbol)("C.F")

                Dim generation0 = CreateInitialBaseline(compilation0, md0, EmptyLocalsProvider)
                Dim diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, method0, method1)))

                diff1.VerifySynthesizedMembers(
                    "C.VB$StateMachine_1#1_F: {$State, $Current, $InitialThreadId, $VB$Me, Dispose, MoveNext, GetEnumerator, IEnumerable.GetEnumerator, get_Current, Reset, IEnumerator.get_Current, Current, IEnumerator.Current}",
                    "C: {VB$StateMachine_1#1_F}")

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

            Dim compilation0 = CompilationUtils.CreateEmptyCompilationWithReferences(source0, references:=LatestVbReferences, options:=TestOptions.DebugDll)
            Dim compilation1 = CompilationUtils.CreateEmptyCompilationWithReferences(source1, references:=LatestVbReferences, options:=TestOptions.DebugDll)

            Dim v0 = CompileAndVerify(compilation:=compilation0)

            Using md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)
                Dim method0 = compilation0.GetMember(Of MethodSymbol)("C.F")
                Dim method1 = compilation1.GetMember(Of MethodSymbol)("C.F")

                Dim generation0 = CreateInitialBaseline(compilation0, md0, EmptyLocalsProvider)
                Dim diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, method0, method1)))

                diff1.VerifySynthesizedMembers(
                    "C.VB$StateMachine_1#1_F: {$State, $Builder, $VB$Me, $A0, MoveNext, System.Runtime.CompilerServices.IAsyncStateMachine.SetStateMachine}",
                    "C: {VB$StateMachine_1#1_F}")

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

            Dim compilation0 = CompilationUtils.CreateEmptyCompilationWithReferences(source0, references:=LatestVbReferences, options:=TestOptions.DebugDll)
            Dim compilation1 = CompilationUtils.CreateEmptyCompilationWithReferences(source1, references:=LatestVbReferences, options:=TestOptions.DebugDll)

            Dim v0 = CompileAndVerify(compilation:=compilation0)

            Using md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)
                Dim method0 = compilation0.GetMember(Of MethodSymbol)("C.F")
                Dim method1 = compilation1.GetMember(Of MethodSymbol)("C.F")

                Dim generation0 = CreateInitialBaseline(compilation0, md0, EmptyLocalsProvider)
                Dim diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, method0, method1)))

                Using md1 = diff1.GetMetadata()
                    CheckEncLogDefinitions(md1.Reader,
                        Row(3, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(4, TableIndex.CustomAttribute, EditAndContinueOperation.Default))
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

            Dim compilation0 = CompilationUtils.CreateEmptyCompilationWithReferences(source0, references:=LatestVbReferences, options:=TestOptions.DebugDll)
            Dim compilation1 = CompilationUtils.CreateEmptyCompilationWithReferences(source1, references:=LatestVbReferences, options:=TestOptions.DebugDll)

            Dim v0 = CompileAndVerify(compilation:=compilation0)

            Using md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)
                Dim method0 = compilation0.GetMember(Of MethodSymbol)("C.F")
                Dim method1 = compilation1.GetMember(Of MethodSymbol)("C.F")

                Dim generation0 = CreateInitialBaseline(compilation0, md0, EmptyLocalsProvider)
                Dim diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, method0, method1)))

                Using md1 = diff1.GetMetadata()
                    CheckEncLogDefinitions(md1.Reader,
                        Row(3, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(4, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(5, TableIndex.CustomAttribute, EditAndContinueOperation.Default))
                End Using
            End Using
        End Sub

        <ConditionalFact(GetType(NotMono))>
        Public Sub AsyncMethodOverloads()

            Using New EditAndContinueTest().
                AddBaseline(
                    source:="
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
",
                    validator:=
                    Sub(g)
                    End Sub).
                AddGeneration(
                    source:="
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
",
                    edits:=
                    {
                        Edit(SemanticEditKind.Update, Function(c) c.GetMembers("C.F").Single(Function(m) m.ToTestDisplayString() = "Function C.F(a As System.Int16) As System.Threading.Tasks.Task(Of System.Int32)"), preserveLocalVariables:=True),
                        Edit(SemanticEditKind.Update, Function(c) c.GetMembers("C.F").Single(Function(m) m.ToTestDisplayString() = "Function C.F(a As System.Int32) As System.Threading.Tasks.Task(Of System.Int32)"), preserveLocalVariables:=True),
                        Edit(SemanticEditKind.Update, Function(c) c.GetMembers("C.F").Single(Function(m) m.ToTestDisplayString() = "Function C.F(a As System.Int64) As System.Threading.Tasks.Task(Of System.Int32)"), preserveLocalVariables:=True)
                    },
                    validator:=
                    Sub(g)
                        ' notice no TypeDefs, FieldDefs
                        g.VerifyEncLogDefinitions(
                        {
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
                            Row(1, TableIndex.Param, EditAndContinueOperation.Default),
                            Row(2, TableIndex.Param, EditAndContinueOperation.Default),
                            Row(3, TableIndex.Param, EditAndContinueOperation.Default),
                            Row(4, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(5, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(6, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(7, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(9, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(10, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(13, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(15, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(17, TableIndex.CustomAttribute, EditAndContinueOperation.Default)
                        })
                    End Sub).
                Verify()
            End Using
        End Sub

        <ConditionalFact(GetType(NotMono))>
        Public Sub UpdateIterator_NoVariables()
            Using New EditAndContinueTest().
                AddBaseline(
                    source:="
Imports System.Collections.Generic
Class C
    Iterator Function F() As IEnumerable(Of Integer)
        <N:0>Yield 1</N:0>
    End Function
End Class",
                    validator:=
                    Sub(g)
                        g.VerifyIL("C.VB$StateMachine_1_F.MoveNext()", "
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
                    End Sub).
                AddGeneration(
                    source:="
Imports System.Collections.Generic
Class C
    Iterator Function F() As IEnumerable(Of Integer)
        <N:0>Yield 2</N:0>
    End Function
End Class",
                    edits:={Edit(SemanticEditKind.Update, Function(c) c.GetMember("C.F"), preserveLocalVariables:=True)},
                    validator:=
                    Sub(g)
                        g.VerifyUpdatedMethodNames("MoveNext")

                        g.VerifyEncLogDefinitions(
                        {
                            Row(3, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                            Row(4, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                            Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(4, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(6, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(7, TableIndex.CustomAttribute, EditAndContinueOperation.Default)
                        })

                        g.VerifyIL("C.VB$StateMachine_1_F.MoveNext()", "
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
                    End Sub).
                Verify()
            End Using
        End Sub

        <ConditionalFact(GetType(NotMono))>
        Public Sub UpdateAsync_NoVariables()
            Using New EditAndContinueTest().
                AddBaseline(
                    source:="
Imports System.Threading.Tasks
Class C
    Async Function F() As Task(Of Integer)
        <N:0>Await Task.FromResult(1)</N:0>
        Return 2
    End Function
End Class",
                    validator:=
                    Sub(g)
                        g.VerifyIL("C.VB$StateMachine_1_F.MoveNext()", "
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
                    End Sub).
                AddGeneration(
                    source:="
Imports System.Threading.Tasks
Class C
    Async Function F() As Task(Of Integer)
        <N:0>Await Task.FromResult(10)</N:0>
        Return 20
    End Function
End Class",
                    edits:={Edit(SemanticEditKind.Update, Function(c) c.GetMember("C.F"), preserveLocalVariables:=True)},
                    validator:=
                    Sub(g)
                        ' only methods with sequence points should be listed in UpdatedMethods:
                        g.VerifyUpdatedMethodNames("MoveNext")

                        g.VerifyEncLogDefinitions(
                        {
                            Row(3, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                            Row(4, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                            Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                            Row(4, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(5, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                            Row(7, TableIndex.CustomAttribute, EditAndContinueOperation.Default)
                        })

                        g.VerifyIL("C.VB$StateMachine_1_F.MoveNext()", "
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
                    End Sub).
                Verify()
            End Using
        End Sub

        <Fact>
        Public Sub UpdateAsync_Await_Add()
            Dim source0 = MarkedSource("
Imports System.Threading.Tasks
Class C
    Shared Function M1() As Task
        Return Nothing
    End Function

    Shared Function M2() As Task
        Return Nothing
    End Function

    Shared Function M3() As Task
        Return Nothing
    End Function

    Shared Sub [End]()
    End Sub

    Shared Async Function F() As Task
        <N:0>Await M1()</N:0>
        <N:1>Await M2()</N:1>
        [End]()
    End Function
End Class")
            Dim source1 = MarkedSource("
Imports System.Threading.Tasks
Class C
    Shared Function M1() As Task
        Return Nothing
    End Function

    Shared Function M2() As Task
        Return Nothing
    End Function

    Shared Function M3() As Task
        Return Nothing
    End Function

    Shared Sub [End]()
    End Sub

    Shared Async Function F() As Task
        <N:0>Await M1()</N:0>
        <N:2>Await M3()</N:2>
        <N:1>Await M2()</N:1>
        [End]()
    End Function
End Class")

            Dim compilation0 = CreateEmptyCompilationWithReferences(source0.Tree, references:=LatestVbReferences, options:=ComSafeDebugDll)
            Dim compilation1 = compilation0.WithSource(source1.Tree)

            Dim f0 = compilation0.GetMember(Of MethodSymbol)("C.F")
            Dim f1 = compilation1.GetMember(Of MethodSymbol)("C.F")

            Dim v0 = CompileAndVerify(compilation:=compilation0).VerifyDiagnostics()
            Using md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)

                Dim generation0 = CreateInitialBaseline(compilation0, md0, Function(handle) v0.CreateSymReader().GetEncMethodDebugInfo(handle))
                Dim diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))))

                v0.VerifyPdb("C.F", "
<symbols>
  <files>
    <file id=""1"" name="""" language=""VB"" />
  </files>
  <methods>
    <method containingType=""C"" name=""F"">
      <customDebugInfo>
        <forwardIterator name=""VB$StateMachine_5_F"" />
        <encStateMachineStateMap>
          <state number=""0"" offset=""0"" />
          <state number=""1"" offset=""31"" />
        </encStateMachineStateMap>
      </customDebugInfo>
    </method>
  </methods>
</symbols>
")
                v0.VerifyPdb("C+VB$StateMachine_5_F.MoveNext", "
<symbols>
  <files>
    <file id=""1"" name="""" language=""VB"" />
  </files>
  <methods>
    <method containingType=""C+VB$StateMachine_5_F"" name=""MoveNext"">
      <customDebugInfo>
        <encLocalSlotMap>
          <slot kind=""27"" offset=""-1"" />
          <slot kind=""33"" offset=""0"" />
          <slot kind=""temp"" />
          <slot kind=""33"" offset=""31"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <scope startOffset=""0x0"" endOffset=""0x12d"">
        <importsforward declaringType=""C"" methodName=""M1"" />
      </scope>
      <asyncInfo>
        <kickoffMethod declaringType=""C"" methodName=""F"" />
        <await yield=""0x37"" resume=""0x55"" declaringType=""C+VB$StateMachine_5_F"" methodName=""MoveNext"" />
        <await yield=""0xa0"" resume=""0xbb"" declaringType=""C+VB$StateMachine_5_F"" methodName=""MoveNext"" />
      </asyncInfo>
    </method>
  </methods>
</symbols>", options:=PdbValidationOptions.ExcludeSequencePoints)
                v0.VerifyIL("C.VB$StateMachine_5_F.MoveNext()", "
{
  // Code size      301 (0x12d)
  .maxstack  3
  .locals init (Integer V_0,
                System.Runtime.CompilerServices.TaskAwaiter V_1,
                C.VB$StateMachine_5_F V_2,
                System.Runtime.CompilerServices.TaskAwaiter V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.VB$StateMachine_5_F.$State As Integer""
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
    IL_001a:  call       ""Function C.M1() As System.Threading.Tasks.Task""
    IL_001f:  callvirt   ""Function System.Threading.Tasks.Task.GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter""
    IL_0024:  stloc.1
    IL_0025:  ldloca.s   V_1
    IL_0027:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter.get_IsCompleted() As Boolean""
    IL_002c:  brtrue.s   IL_0073
    IL_002e:  ldarg.0
    IL_002f:  ldc.i4.0
    IL_0030:  dup
    IL_0031:  stloc.0
    IL_0032:  stfld      ""C.VB$StateMachine_5_F.$State As Integer""
    IL_0037:  ldarg.0
    IL_0038:  ldloc.1
    IL_0039:  stfld      ""C.VB$StateMachine_5_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
    IL_003e:  ldarg.0
    IL_003f:  ldflda     ""C.VB$StateMachine_5_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder""
    IL_0044:  ldloca.s   V_1
    IL_0046:  ldarg.0
    IL_0047:  stloc.2
    IL_0048:  ldloca.s   V_2
    IL_004a:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter, C.VB$StateMachine_5_F)(ByRef System.Runtime.CompilerServices.TaskAwaiter, ByRef C.VB$StateMachine_5_F)""
    IL_004f:  nop
    IL_0050:  leave      IL_012c
    IL_0055:  ldarg.0
    IL_0056:  ldc.i4.m1
    IL_0057:  dup
    IL_0058:  stloc.0
    IL_0059:  stfld      ""C.VB$StateMachine_5_F.$State As Integer""
    IL_005e:  ldarg.0
    IL_005f:  ldfld      ""C.VB$StateMachine_5_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
    IL_0064:  stloc.1
    IL_0065:  ldarg.0
    IL_0066:  ldflda     ""C.VB$StateMachine_5_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
    IL_006b:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_0071:  br.s       IL_0073
    IL_0073:  ldloca.s   V_1
    IL_0075:  call       ""Sub System.Runtime.CompilerServices.TaskAwaiter.GetResult()""
    IL_007a:  nop
    IL_007b:  ldloca.s   V_1
    IL_007d:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_0083:  call       ""Function C.M2() As System.Threading.Tasks.Task""
    IL_0088:  callvirt   ""Function System.Threading.Tasks.Task.GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter""
    IL_008d:  stloc.3
    IL_008e:  ldloca.s   V_3
    IL_0090:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter.get_IsCompleted() As Boolean""
    IL_0095:  brtrue.s   IL_00d9
    IL_0097:  ldarg.0
    IL_0098:  ldc.i4.1
    IL_0099:  dup
    IL_009a:  stloc.0
    IL_009b:  stfld      ""C.VB$StateMachine_5_F.$State As Integer""
    IL_00a0:  ldarg.0
    IL_00a1:  ldloc.3
    IL_00a2:  stfld      ""C.VB$StateMachine_5_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
    IL_00a7:  ldarg.0
    IL_00a8:  ldflda     ""C.VB$StateMachine_5_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder""
    IL_00ad:  ldloca.s   V_3
    IL_00af:  ldarg.0
    IL_00b0:  stloc.2
    IL_00b1:  ldloca.s   V_2
    IL_00b3:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter, C.VB$StateMachine_5_F)(ByRef System.Runtime.CompilerServices.TaskAwaiter, ByRef C.VB$StateMachine_5_F)""
    IL_00b8:  nop
    IL_00b9:  leave.s    IL_012c
    IL_00bb:  ldarg.0
    IL_00bc:  ldc.i4.m1
    IL_00bd:  dup
    IL_00be:  stloc.0
    IL_00bf:  stfld      ""C.VB$StateMachine_5_F.$State As Integer""
    IL_00c4:  ldarg.0
    IL_00c5:  ldfld      ""C.VB$StateMachine_5_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
    IL_00ca:  stloc.3
    IL_00cb:  ldarg.0
    IL_00cc:  ldflda     ""C.VB$StateMachine_5_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
    IL_00d1:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_00d7:  br.s       IL_00d9
    IL_00d9:  ldloca.s   V_3
    IL_00db:  call       ""Sub System.Runtime.CompilerServices.TaskAwaiter.GetResult()""
    IL_00e0:  nop
    IL_00e1:  ldloca.s   V_3
    IL_00e3:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_00e9:  call       ""Sub C.End()""
    IL_00ee:  nop
    IL_00ef:  leave.s    IL_0116
  }
  catch System.Exception
  {
    IL_00f1:  dup
    IL_00f2:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)""
    IL_00f7:  stloc.s    V_4
    IL_00f9:  ldarg.0
    IL_00fa:  ldc.i4.s   -2
    IL_00fc:  stfld      ""C.VB$StateMachine_5_F.$State As Integer""
    IL_0101:  ldarg.0
    IL_0102:  ldflda     ""C.VB$StateMachine_5_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder""
    IL_0107:  ldloc.s    V_4
    IL_0109:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_010e:  nop
    IL_010f:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()""
    IL_0114:  leave.s    IL_012c
  }
  IL_0116:  ldarg.0
  IL_0117:  ldc.i4.s   -2
  IL_0119:  dup
  IL_011a:  stloc.0
  IL_011b:  stfld      ""C.VB$StateMachine_5_F.$State As Integer""
  IL_0120:  ldarg.0
  IL_0121:  ldflda     ""C.VB$StateMachine_5_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder""
  IL_0126:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_012b:  nop
  IL_012c:  ret
}")
                diff1.VerifyIL("C.VB$StateMachine_5_F.MoveNext()", "
{
  // Code size      423 (0x1a7)
  .maxstack  3
  .locals init (Integer V_0,
                System.Runtime.CompilerServices.TaskAwaiter V_1,
                C.VB$StateMachine_5_F V_2,
                System.Runtime.CompilerServices.TaskAwaiter V_3,
                System.Runtime.CompilerServices.TaskAwaiter V_4,
                System.Exception V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.VB$StateMachine_5_F.$State As Integer""
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
    IL_001d:  br         IL_0134
    IL_0022:  br         IL_00cc
    IL_0027:  nop
    IL_0028:  call       ""Function C.M1() As System.Threading.Tasks.Task""
    IL_002d:  callvirt   ""Function System.Threading.Tasks.Task.GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter""
    IL_0032:  stloc.1
    IL_0033:  ldloca.s   V_1
    IL_0035:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter.get_IsCompleted() As Boolean""
    IL_003a:  brtrue.s   IL_0081
    IL_003c:  ldarg.0
    IL_003d:  ldc.i4.0
    IL_003e:  dup
    IL_003f:  stloc.0
    IL_0040:  stfld      ""C.VB$StateMachine_5_F.$State As Integer""
    IL_0045:  ldarg.0
    IL_0046:  ldloc.1
    IL_0047:  stfld      ""C.VB$StateMachine_5_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
    IL_004c:  ldarg.0
    IL_004d:  ldflda     ""C.VB$StateMachine_5_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder""
    IL_0052:  ldloca.s   V_1
    IL_0054:  ldarg.0
    IL_0055:  stloc.2
    IL_0056:  ldloca.s   V_2
    IL_0058:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter, C.VB$StateMachine_5_F)(ByRef System.Runtime.CompilerServices.TaskAwaiter, ByRef C.VB$StateMachine_5_F)""
    IL_005d:  nop
    IL_005e:  leave      IL_01a6
    IL_0063:  ldarg.0
    IL_0064:  ldc.i4.m1
    IL_0065:  dup
    IL_0066:  stloc.0
    IL_0067:  stfld      ""C.VB$StateMachine_5_F.$State As Integer""
    IL_006c:  ldarg.0
    IL_006d:  ldfld      ""C.VB$StateMachine_5_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
    IL_0072:  stloc.1
    IL_0073:  ldarg.0
    IL_0074:  ldflda     ""C.VB$StateMachine_5_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
    IL_0079:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_007f:  br.s       IL_0081
    IL_0081:  ldloca.s   V_1
    IL_0083:  call       ""Sub System.Runtime.CompilerServices.TaskAwaiter.GetResult()""
    IL_0088:  nop
    IL_0089:  ldloca.s   V_1
    IL_008b:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_0091:  call       ""Function C.M3() As System.Threading.Tasks.Task""
    IL_0096:  callvirt   ""Function System.Threading.Tasks.Task.GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter""
    IL_009b:  stloc.3
    IL_009c:  ldloca.s   V_3
    IL_009e:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter.get_IsCompleted() As Boolean""
    IL_00a3:  brtrue.s   IL_00ea
    IL_00a5:  ldarg.0
    IL_00a6:  ldc.i4.2
    IL_00a7:  dup
    IL_00a8:  stloc.0
    IL_00a9:  stfld      ""C.VB$StateMachine_5_F.$State As Integer""
    IL_00ae:  ldarg.0
    IL_00af:  ldloc.3
    IL_00b0:  stfld      ""C.VB$StateMachine_5_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
    IL_00b5:  ldarg.0
    IL_00b6:  ldflda     ""C.VB$StateMachine_5_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder""
    IL_00bb:  ldloca.s   V_3
    IL_00bd:  ldarg.0
    IL_00be:  stloc.2
    IL_00bf:  ldloca.s   V_2
    IL_00c1:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter, C.VB$StateMachine_5_F)(ByRef System.Runtime.CompilerServices.TaskAwaiter, ByRef C.VB$StateMachine_5_F)""
    IL_00c6:  nop
    IL_00c7:  leave      IL_01a6
    IL_00cc:  ldarg.0
    IL_00cd:  ldc.i4.m1
    IL_00ce:  dup
    IL_00cf:  stloc.0
    IL_00d0:  stfld      ""C.VB$StateMachine_5_F.$State As Integer""
    IL_00d5:  ldarg.0
    IL_00d6:  ldfld      ""C.VB$StateMachine_5_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
    IL_00db:  stloc.3
    IL_00dc:  ldarg.0
    IL_00dd:  ldflda     ""C.VB$StateMachine_5_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
    IL_00e2:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_00e8:  br.s       IL_00ea
    IL_00ea:  ldloca.s   V_3
    IL_00ec:  call       ""Sub System.Runtime.CompilerServices.TaskAwaiter.GetResult()""
    IL_00f1:  nop
    IL_00f2:  ldloca.s   V_3
    IL_00f4:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_00fa:  call       ""Function C.M2() As System.Threading.Tasks.Task""
    IL_00ff:  callvirt   ""Function System.Threading.Tasks.Task.GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter""
    IL_0104:  stloc.s    V_4
    IL_0106:  ldloca.s   V_4
    IL_0108:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter.get_IsCompleted() As Boolean""
    IL_010d:  brtrue.s   IL_0153
    IL_010f:  ldarg.0
    IL_0110:  ldc.i4.1
    IL_0111:  dup
    IL_0112:  stloc.0
    IL_0113:  stfld      ""C.VB$StateMachine_5_F.$State As Integer""
    IL_0118:  ldarg.0
    IL_0119:  ldloc.s    V_4
    IL_011b:  stfld      ""C.VB$StateMachine_5_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
    IL_0120:  ldarg.0
    IL_0121:  ldflda     ""C.VB$StateMachine_5_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder""
    IL_0126:  ldloca.s   V_4
    IL_0128:  ldarg.0
    IL_0129:  stloc.2
    IL_012a:  ldloca.s   V_2
    IL_012c:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter, C.VB$StateMachine_5_F)(ByRef System.Runtime.CompilerServices.TaskAwaiter, ByRef C.VB$StateMachine_5_F)""
    IL_0131:  nop
    IL_0132:  leave.s    IL_01a6
    IL_0134:  ldarg.0
    IL_0135:  ldc.i4.m1
    IL_0136:  dup
    IL_0137:  stloc.0
    IL_0138:  stfld      ""C.VB$StateMachine_5_F.$State As Integer""
    IL_013d:  ldarg.0
    IL_013e:  ldfld      ""C.VB$StateMachine_5_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
    IL_0143:  stloc.s    V_4
    IL_0145:  ldarg.0
    IL_0146:  ldflda     ""C.VB$StateMachine_5_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
    IL_014b:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_0151:  br.s       IL_0153
    IL_0153:  ldloca.s   V_4
    IL_0155:  call       ""Sub System.Runtime.CompilerServices.TaskAwaiter.GetResult()""
    IL_015a:  nop
    IL_015b:  ldloca.s   V_4
    IL_015d:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_0163:  call       ""Sub C.End()""
    IL_0168:  nop
    IL_0169:  leave.s    IL_0190
  }
  catch System.Exception
  {
    IL_016b:  dup
    IL_016c:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)""
    IL_0171:  stloc.s    V_5
    IL_0173:  ldarg.0
    IL_0174:  ldc.i4.s   -2
    IL_0176:  stfld      ""C.VB$StateMachine_5_F.$State As Integer""
    IL_017b:  ldarg.0
    IL_017c:  ldflda     ""C.VB$StateMachine_5_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder""
    IL_0181:  ldloc.s    V_5
    IL_0183:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_0188:  nop
    IL_0189:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()""
    IL_018e:  leave.s    IL_01a6
  }
  IL_0190:  ldarg.0
  IL_0191:  ldc.i4.s   -2
  IL_0193:  dup
  IL_0194:  stloc.0
  IL_0195:  stfld      ""C.VB$StateMachine_5_F.$State As Integer""
  IL_019a:  ldarg.0
  IL_019b:  ldflda     ""C.VB$StateMachine_5_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder""
  IL_01a0:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_01a5:  nop
  IL_01a6:  ret
}")
            End Using
        End Sub

        <Fact>
        Public Sub UpdateAsync_Await_Remove_RemoveAdd()
            Dim source0 = MarkedSource("
Imports System.Threading.Tasks
Class C
    Shared Function M1() As Task
        Return Nothing
    End Function

    Shared Function M2() As Task
        Return Nothing
    End Function

    Shared Function M3() As Task
        Return Nothing
    End Function

    Shared Sub [End]()
    End Sub

    Shared Async Function F() As Task
        <N:0>Await M1()</N:0>
        <N:1>Await M3()</N:1>
        <N:2>Await M2()</N:2>
        [End]()
    End Function
End Class")
            Dim source1 = MarkedSource("
Imports System.Threading.Tasks
Class C
    Shared Function M1() As Task
        Return Nothing
    End Function

    Shared Function M2() As Task
        Return Nothing
    End Function

    Shared Function M3() As Task
        Return Nothing
    End Function

    Shared Sub [End]()
    End Sub

    Shared Async Function F() As Task
        <N:0>Await M1()</N:0>
        <N:2>Await M2()</N:2>
        [End]()
    End Function
End Class")
            Dim source2 = MarkedSource("
Imports System.Threading.Tasks
Class C
    Shared Function M1() As Task
        Return Nothing
    End Function

    Shared Function M2() As Task
        Return Nothing
    End Function

    Shared Function M3() As Task
        Return Nothing
    End Function

    Shared Sub [End]()
    End Sub

    Shared Async Function F() As Task
        <N:0>Await M1()</N:0>
        [End]()
    End Function
End Class")

            Dim compilation0 = CreateEmptyCompilationWithReferences(source0.Tree, references:=LatestVbReferences, options:=ComSafeDebugDll)
            Dim compilation1 = compilation0.WithSource(source1.Tree)
            Dim compilation2 = compilation1.WithSource(source2.Tree)

            Dim f0 = compilation0.GetMember(Of MethodSymbol)("C.F")
            Dim f1 = compilation1.GetMember(Of MethodSymbol)("C.F")
            Dim f2 = compilation2.GetMember(Of MethodSymbol)("C.F")

            Dim v0 = CompileAndVerify(compilation:=compilation0).VerifyDiagnostics()
            Using md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)
                Dim generation0 = CreateInitialBaseline(compilation0, md0, Function(handle) v0.CreateSymReader().GetEncMethodDebugInfo(handle))
                Dim diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))))

                Dim diff2 = compilation2.EmitDifference(
                    diff1.NextGeneration,
                    ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, f1, f2, GetSyntaxMapFromMarkers(source1, source2))))

                v0.VerifyPdb("C.F", "
<symbols>
  <files>
    <file id=""1"" name="""" language=""VB"" />
  </files>
  <methods>
    <method containingType=""C"" name=""F"">
      <customDebugInfo>
        <forwardIterator name=""VB$StateMachine_5_F"" />
        <encStateMachineStateMap>
          <state number=""0"" offset=""0"" />
          <state number=""1"" offset=""31"" />
          <state number=""2"" offset=""62"" />
        </encStateMachineStateMap>
      </customDebugInfo>
    </method>
  </methods>
</symbols>")
                v0.VerifyPdb("C+VB$StateMachine_5_F.MoveNext", "
<symbols>
  <files>
    <file id=""1"" name="""" language=""VB"" />
  </files>
  <methods>
    <method containingType=""C+VB$StateMachine_5_F"" name=""MoveNext"">
      <customDebugInfo>
        <encLocalSlotMap>
          <slot kind=""27"" offset=""-1"" />
          <slot kind=""33"" offset=""0"" />
          <slot kind=""temp"" />
          <slot kind=""33"" offset=""31"" />
          <slot kind=""33"" offset=""62"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <scope startOffset=""0x0"" endOffset=""0x1a7"">
        <importsforward declaringType=""C"" methodName=""M1"" />
      </scope>
      <asyncInfo>
        <kickoffMethod declaringType=""C"" methodName=""F"" />
        <await yield=""0x45"" resume=""0x63"" declaringType=""C+VB$StateMachine_5_F"" methodName=""MoveNext"" />
        <await yield=""0xae"" resume=""0xcc"" declaringType=""C+VB$StateMachine_5_F"" methodName=""MoveNext"" />
        <await yield=""0x118"" resume=""0x134"" declaringType=""C+VB$StateMachine_5_F"" methodName=""MoveNext"" />
      </asyncInfo>
    </method>
  </methods>
</symbols>", options:=PdbValidationOptions.ExcludeSequencePoints)
                v0.VerifyIL("C.VB$StateMachine_5_F.MoveNext", "
{
  // Code size      423 (0x1a7)
  .maxstack  3
  .locals init (Integer V_0,
                System.Runtime.CompilerServices.TaskAwaiter V_1,
                C.VB$StateMachine_5_F V_2,
                System.Runtime.CompilerServices.TaskAwaiter V_3,
                System.Runtime.CompilerServices.TaskAwaiter V_4,
                System.Exception V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.VB$StateMachine_5_F.$State As Integer""
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
    IL_001d:  br         IL_00cc
    IL_0022:  br         IL_0134
    IL_0027:  nop
    IL_0028:  call       ""Function C.M1() As System.Threading.Tasks.Task""
    IL_002d:  callvirt   ""Function System.Threading.Tasks.Task.GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter""
    IL_0032:  stloc.1
    IL_0033:  ldloca.s   V_1
    IL_0035:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter.get_IsCompleted() As Boolean""
    IL_003a:  brtrue.s   IL_0081
    IL_003c:  ldarg.0
    IL_003d:  ldc.i4.0
    IL_003e:  dup
    IL_003f:  stloc.0
    IL_0040:  stfld      ""C.VB$StateMachine_5_F.$State As Integer""
    IL_0045:  ldarg.0
    IL_0046:  ldloc.1
    IL_0047:  stfld      ""C.VB$StateMachine_5_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
    IL_004c:  ldarg.0
    IL_004d:  ldflda     ""C.VB$StateMachine_5_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder""
    IL_0052:  ldloca.s   V_1
    IL_0054:  ldarg.0
    IL_0055:  stloc.2
    IL_0056:  ldloca.s   V_2
    IL_0058:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter, C.VB$StateMachine_5_F)(ByRef System.Runtime.CompilerServices.TaskAwaiter, ByRef C.VB$StateMachine_5_F)""
    IL_005d:  nop
    IL_005e:  leave      IL_01a6
    IL_0063:  ldarg.0
    IL_0064:  ldc.i4.m1
    IL_0065:  dup
    IL_0066:  stloc.0
    IL_0067:  stfld      ""C.VB$StateMachine_5_F.$State As Integer""
    IL_006c:  ldarg.0
    IL_006d:  ldfld      ""C.VB$StateMachine_5_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
    IL_0072:  stloc.1
    IL_0073:  ldarg.0
    IL_0074:  ldflda     ""C.VB$StateMachine_5_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
    IL_0079:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_007f:  br.s       IL_0081
    IL_0081:  ldloca.s   V_1
    IL_0083:  call       ""Sub System.Runtime.CompilerServices.TaskAwaiter.GetResult()""
    IL_0088:  nop
    IL_0089:  ldloca.s   V_1
    IL_008b:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_0091:  call       ""Function C.M3() As System.Threading.Tasks.Task""
    IL_0096:  callvirt   ""Function System.Threading.Tasks.Task.GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter""
    IL_009b:  stloc.3
    IL_009c:  ldloca.s   V_3
    IL_009e:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter.get_IsCompleted() As Boolean""
    IL_00a3:  brtrue.s   IL_00ea
    IL_00a5:  ldarg.0
    IL_00a6:  ldc.i4.1
    IL_00a7:  dup
    IL_00a8:  stloc.0
    IL_00a9:  stfld      ""C.VB$StateMachine_5_F.$State As Integer""
    IL_00ae:  ldarg.0
    IL_00af:  ldloc.3
    IL_00b0:  stfld      ""C.VB$StateMachine_5_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
    IL_00b5:  ldarg.0
    IL_00b6:  ldflda     ""C.VB$StateMachine_5_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder""
    IL_00bb:  ldloca.s   V_3
    IL_00bd:  ldarg.0
    IL_00be:  stloc.2
    IL_00bf:  ldloca.s   V_2
    IL_00c1:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter, C.VB$StateMachine_5_F)(ByRef System.Runtime.CompilerServices.TaskAwaiter, ByRef C.VB$StateMachine_5_F)""
    IL_00c6:  nop
    IL_00c7:  leave      IL_01a6
    IL_00cc:  ldarg.0
    IL_00cd:  ldc.i4.m1
    IL_00ce:  dup
    IL_00cf:  stloc.0
    IL_00d0:  stfld      ""C.VB$StateMachine_5_F.$State As Integer""
    IL_00d5:  ldarg.0
    IL_00d6:  ldfld      ""C.VB$StateMachine_5_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
    IL_00db:  stloc.3
    IL_00dc:  ldarg.0
    IL_00dd:  ldflda     ""C.VB$StateMachine_5_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
    IL_00e2:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_00e8:  br.s       IL_00ea
    IL_00ea:  ldloca.s   V_3
    IL_00ec:  call       ""Sub System.Runtime.CompilerServices.TaskAwaiter.GetResult()""
    IL_00f1:  nop
    IL_00f2:  ldloca.s   V_3
    IL_00f4:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_00fa:  call       ""Function C.M2() As System.Threading.Tasks.Task""
    IL_00ff:  callvirt   ""Function System.Threading.Tasks.Task.GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter""
    IL_0104:  stloc.s    V_4
    IL_0106:  ldloca.s   V_4
    IL_0108:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter.get_IsCompleted() As Boolean""
    IL_010d:  brtrue.s   IL_0153
    IL_010f:  ldarg.0
    IL_0110:  ldc.i4.2
    IL_0111:  dup
    IL_0112:  stloc.0
    IL_0113:  stfld      ""C.VB$StateMachine_5_F.$State As Integer""
    IL_0118:  ldarg.0
    IL_0119:  ldloc.s    V_4
    IL_011b:  stfld      ""C.VB$StateMachine_5_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
    IL_0120:  ldarg.0
    IL_0121:  ldflda     ""C.VB$StateMachine_5_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder""
    IL_0126:  ldloca.s   V_4
    IL_0128:  ldarg.0
    IL_0129:  stloc.2
    IL_012a:  ldloca.s   V_2
    IL_012c:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter, C.VB$StateMachine_5_F)(ByRef System.Runtime.CompilerServices.TaskAwaiter, ByRef C.VB$StateMachine_5_F)""
    IL_0131:  nop
    IL_0132:  leave.s    IL_01a6
    IL_0134:  ldarg.0
    IL_0135:  ldc.i4.m1
    IL_0136:  dup
    IL_0137:  stloc.0
    IL_0138:  stfld      ""C.VB$StateMachine_5_F.$State As Integer""
    IL_013d:  ldarg.0
    IL_013e:  ldfld      ""C.VB$StateMachine_5_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
    IL_0143:  stloc.s    V_4
    IL_0145:  ldarg.0
    IL_0146:  ldflda     ""C.VB$StateMachine_5_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
    IL_014b:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_0151:  br.s       IL_0153
    IL_0153:  ldloca.s   V_4
    IL_0155:  call       ""Sub System.Runtime.CompilerServices.TaskAwaiter.GetResult()""
    IL_015a:  nop
    IL_015b:  ldloca.s   V_4
    IL_015d:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_0163:  call       ""Sub C.End()""
    IL_0168:  nop
    IL_0169:  leave.s    IL_0190
  }
  catch System.Exception
  {
    IL_016b:  dup
    IL_016c:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)""
    IL_0171:  stloc.s    V_5
    IL_0173:  ldarg.0
    IL_0174:  ldc.i4.s   -2
    IL_0176:  stfld      ""C.VB$StateMachine_5_F.$State As Integer""
    IL_017b:  ldarg.0
    IL_017c:  ldflda     ""C.VB$StateMachine_5_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder""
    IL_0181:  ldloc.s    V_5
    IL_0183:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_0188:  nop
    IL_0189:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()""
    IL_018e:  leave.s    IL_01a6
  }
  IL_0190:  ldarg.0
  IL_0191:  ldc.i4.s   -2
  IL_0193:  dup
  IL_0194:  stloc.0
  IL_0195:  stfld      ""C.VB$StateMachine_5_F.$State As Integer""
  IL_019a:  ldarg.0
  IL_019b:  ldflda     ""C.VB$StateMachine_5_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder""
  IL_01a0:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_01a5:  nop
  IL_01a6:  ret
}")
                diff1.VerifyIL("C.VB$StateMachine_5_F.MoveNext", "
{
  // Code size      316 (0x13c)
  .maxstack  3
  .locals init (Integer V_0,
                System.Runtime.CompilerServices.TaskAwaiter V_1,
                C.VB$StateMachine_5_F V_2,
                System.Runtime.CompilerServices.TaskAwaiter V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.VB$StateMachine_5_F.$State As Integer""
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
    IL_0012:  br.s       IL_0064
    IL_0014:  br         IL_00ca
    IL_0019:  ldloc.0
    IL_001a:  ldc.i4.0
    IL_001b:  blt.s      IL_0028
    IL_001d:  ldstr      """ & CodeAnalysisResources.EncCannotResumeSuspendedAsyncMethod & """
    IL_0022:  newobj     ""Sub System.InvalidOperationException..ctor(String)""
    IL_0027:  throw

    IL_0028:  nop
    IL_0029:  call       ""Function C.M1() As System.Threading.Tasks.Task""
    IL_002e:  callvirt   ""Function System.Threading.Tasks.Task.GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter""
    IL_0033:  stloc.1
    IL_0034:  ldloca.s   V_1
    IL_0036:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter.get_IsCompleted() As Boolean""
    IL_003b:  brtrue.s   IL_0082
    IL_003d:  ldarg.0
    IL_003e:  ldc.i4.0
    IL_003f:  dup
    IL_0040:  stloc.0
    IL_0041:  stfld      ""C.VB$StateMachine_5_F.$State As Integer""
    IL_0046:  ldarg.0
    IL_0047:  ldloc.1
    IL_0048:  stfld      ""C.VB$StateMachine_5_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
    IL_004d:  ldarg.0
    IL_004e:  ldflda     ""C.VB$StateMachine_5_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder""
    IL_0053:  ldloca.s   V_1
    IL_0055:  ldarg.0
    IL_0056:  stloc.2
    IL_0057:  ldloca.s   V_2
    IL_0059:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter, C.VB$StateMachine_5_F)(ByRef System.Runtime.CompilerServices.TaskAwaiter, ByRef C.VB$StateMachine_5_F)""
    IL_005e:  nop
    IL_005f:  leave      IL_013b

    IL_0064:  ldarg.0
    IL_0065:  ldc.i4.m1
    IL_0066:  dup
    IL_0067:  stloc.0
    IL_0068:  stfld      ""C.VB$StateMachine_5_F.$State As Integer""
    IL_006d:  ldarg.0
    IL_006e:  ldfld      ""C.VB$StateMachine_5_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
    IL_0073:  stloc.1
    IL_0074:  ldarg.0
    IL_0075:  ldflda     ""C.VB$StateMachine_5_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
    IL_007a:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_0080:  br.s       IL_0082
    IL_0082:  ldloca.s   V_1
    IL_0084:  call       ""Sub System.Runtime.CompilerServices.TaskAwaiter.GetResult()""
    IL_0089:  nop
    IL_008a:  ldloca.s   V_1
    IL_008c:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_0092:  call       ""Function C.M2() As System.Threading.Tasks.Task""
    IL_0097:  callvirt   ""Function System.Threading.Tasks.Task.GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter""
    IL_009c:  stloc.3
    IL_009d:  ldloca.s   V_3
    IL_009f:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter.get_IsCompleted() As Boolean""
    IL_00a4:  brtrue.s   IL_00e8

    IL_00a6:  ldarg.0
    IL_00a7:  ldc.i4.2
    IL_00a8:  dup
    IL_00a9:  stloc.0
    IL_00aa:  stfld      ""C.VB$StateMachine_5_F.$State As Integer""
    IL_00af:  ldarg.0
    IL_00b0:  ldloc.3
    IL_00b1:  stfld      ""C.VB$StateMachine_5_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
    IL_00b6:  ldarg.0
    IL_00b7:  ldflda     ""C.VB$StateMachine_5_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder""
    IL_00bc:  ldloca.s   V_3
    IL_00be:  ldarg.0
    IL_00bf:  stloc.2
    IL_00c0:  ldloca.s   V_2
    IL_00c2:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter, C.VB$StateMachine_5_F)(ByRef System.Runtime.CompilerServices.TaskAwaiter, ByRef C.VB$StateMachine_5_F)""
    IL_00c7:  nop
    IL_00c8:  leave.s    IL_013b

    IL_00ca:  ldarg.0
    IL_00cb:  ldc.i4.m1
    IL_00cc:  dup
    IL_00cd:  stloc.0
    IL_00ce:  stfld      ""C.VB$StateMachine_5_F.$State As Integer""
    IL_00d3:  ldarg.0
    IL_00d4:  ldfld      ""C.VB$StateMachine_5_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
    IL_00d9:  stloc.3
    IL_00da:  ldarg.0
    IL_00db:  ldflda     ""C.VB$StateMachine_5_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
    IL_00e0:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_00e6:  br.s       IL_00e8

    IL_00e8:  ldloca.s   V_3
    IL_00ea:  call       ""Sub System.Runtime.CompilerServices.TaskAwaiter.GetResult()""
    IL_00ef:  nop
    IL_00f0:  ldloca.s   V_3
    IL_00f2:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_00f8:  call       ""Sub C.End()""
    IL_00fd:  nop
    IL_00fe:  leave.s    IL_0125
  }
  catch System.Exception
  {
    IL_0100:  dup
    IL_0101:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)""
    IL_0106:  stloc.s    V_4
    IL_0108:  ldarg.0
    IL_0109:  ldc.i4.s   -2
    IL_010b:  stfld      ""C.VB$StateMachine_5_F.$State As Integer""
    IL_0110:  ldarg.0
    IL_0111:  ldflda     ""C.VB$StateMachine_5_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder""
    IL_0116:  ldloc.s    V_4
    IL_0118:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_011d:  nop
    IL_011e:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()""
    IL_0123:  leave.s    IL_013b
  }
  IL_0125:  ldarg.0
  IL_0126:  ldc.i4.s   -2
  IL_0128:  dup
  IL_0129:  stloc.0
  IL_012a:  stfld      ""C.VB$StateMachine_5_F.$State As Integer""
  IL_012f:  ldarg.0
  IL_0130:  ldflda     ""C.VB$StateMachine_5_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder""
  IL_0135:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_013a:  nop
  IL_013b:  ret
}")
                diff1.VerifyPdb(Enumerable.Range(1, 20).Select(AddressOf MetadataTokens.MethodDefinitionHandle), "
<symbols>
  <files>
    <file id=""1"" name="""" language=""VB"" />
  </files>
  <methods>
    <method token=""0x6000008"">
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""1"" />
        <entry offset=""0x7"" hidden=""true"" document=""1"" />
        <entry offset=""0x28"" startLine=""19"" startColumn=""5"" endLine=""19"" endColumn=""38"" document=""1"" />
        <entry offset=""0x29"" startLine=""20"" startColumn=""14"" endLine=""20"" endColumn=""24"" document=""1"" />
        <entry offset=""0x34"" hidden=""true"" document=""1"" />
        <entry offset=""0x92"" startLine=""21"" startColumn=""14"" endLine=""21"" endColumn=""24"" document=""1"" />
        <entry offset=""0x9d"" hidden=""true"" document=""1"" />
        <entry offset=""0xf8"" startLine=""22"" startColumn=""9"" endLine=""22"" endColumn=""16"" document=""1"" />
        <entry offset=""0xfe"" startLine=""23"" startColumn=""5"" endLine=""23"" endColumn=""17"" document=""1"" />
        <entry offset=""0x100"" hidden=""true"" document=""1"" />
        <entry offset=""0x108"" hidden=""true"" document=""1"" />
        <entry offset=""0x125"" startLine=""23"" startColumn=""5"" endLine=""23"" endColumn=""17"" document=""1"" />
        <entry offset=""0x12f"" hidden=""true"" document=""1"" />
      </sequencePoints>
      <asyncInfo>
        <kickoffMethod token=""0x6000006"" />
        <await yield=""0x46"" resume=""0x64"" token=""0x6000008"" />
        <await yield=""0xaf"" resume=""0xca"" token=""0x6000008"" />
      </asyncInfo>
    </method>
  </methods>
  <customDebugInfo>
    <defaultnamespace name="""" />
  </customDebugInfo>
</symbols>")
                diff2.VerifyIL("C.VB$StateMachine_5_F.MoveNext", "
{
  // Code size      198 (0xc6)
  .maxstack  3
  .locals init (Integer V_0,
                System.Runtime.CompilerServices.TaskAwaiter V_1,
                C.VB$StateMachine_5_F V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.VB$StateMachine_5_F.$State As Integer""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_000c
    IL_000a:  br.s       IL_000e
    IL_000c:  br.s       IL_0056
    IL_000e:  ldloc.0
    IL_000f:  ldc.i4.0
    IL_0010:  blt.s      IL_001d
    IL_0012:  ldstr      """ & CodeAnalysisResources.EncCannotResumeSuspendedAsyncMethod & """
    IL_0017:  newobj     ""Sub System.InvalidOperationException..ctor(String)""
    IL_001c:  throw
    IL_001d:  nop
    IL_001e:  call       ""Function C.M1() As System.Threading.Tasks.Task""
    IL_0023:  callvirt   ""Function System.Threading.Tasks.Task.GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter""
    IL_0028:  stloc.1
    IL_0029:  ldloca.s   V_1
    IL_002b:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter.get_IsCompleted() As Boolean""
    IL_0030:  brtrue.s   IL_0074

    IL_0032:  ldarg.0
    IL_0033:  ldc.i4.0
    IL_0034:  dup
    IL_0035:  stloc.0
    IL_0036:  stfld      ""C.VB$StateMachine_5_F.$State As Integer""
    IL_003b:  ldarg.0
    IL_003c:  ldloc.1
    IL_003d:  stfld      ""C.VB$StateMachine_5_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
    IL_0042:  ldarg.0
    IL_0043:  ldflda     ""C.VB$StateMachine_5_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder""
    IL_0048:  ldloca.s   V_1
    IL_004a:  ldarg.0
    IL_004b:  stloc.2
    IL_004c:  ldloca.s   V_2
    IL_004e:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter, C.VB$StateMachine_5_F)(ByRef System.Runtime.CompilerServices.TaskAwaiter, ByRef C.VB$StateMachine_5_F)""
    IL_0053:  nop
    IL_0054:  leave.s    IL_00c5

    IL_0056:  ldarg.0
    IL_0057:  ldc.i4.m1
    IL_0058:  dup
    IL_0059:  stloc.0
    IL_005a:  stfld      ""C.VB$StateMachine_5_F.$State As Integer""
    IL_005f:  ldarg.0
    IL_0060:  ldfld      ""C.VB$StateMachine_5_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
    IL_0065:  stloc.1
    IL_0066:  ldarg.0
    IL_0067:  ldflda     ""C.VB$StateMachine_5_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
    IL_006c:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_0072:  br.s       IL_0074

    IL_0074:  ldloca.s   V_1
    IL_0076:  call       ""Sub System.Runtime.CompilerServices.TaskAwaiter.GetResult()""
    IL_007b:  nop
    IL_007c:  ldloca.s   V_1
    IL_007e:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_0084:  call       ""Sub C.End()""
    IL_0089:  nop
    IL_008a:  leave.s    IL_00af
  }
  catch System.Exception
  {
    IL_008c:  dup
    IL_008d:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)""
    IL_0092:  stloc.3
    IL_0093:  ldarg.0
    IL_0094:  ldc.i4.s   -2
    IL_0096:  stfld      ""C.VB$StateMachine_5_F.$State As Integer""
    IL_009b:  ldarg.0
    IL_009c:  ldflda     ""C.VB$StateMachine_5_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder""
    IL_00a1:  ldloc.3
    IL_00a2:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00a7:  nop
    IL_00a8:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()""
    IL_00ad:  leave.s    IL_00c5
  }
  IL_00af:  ldarg.0
  IL_00b0:  ldc.i4.s   -2
  IL_00b2:  dup
  IL_00b3:  stloc.0
  IL_00b4:  stfld      ""C.VB$StateMachine_5_F.$State As Integer""
  IL_00b9:  ldarg.0
  IL_00ba:  ldflda     ""C.VB$StateMachine_5_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder""
  IL_00bf:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00c4:  nop
  IL_00c5:  ret
}")
            End Using
        End Sub

        <Fact>
        Public Sub UpdateAsync_Await_AddRemove_Lambda()
            Dim source0 = MarkedSource("
Imports System
Imports System.Threading.Tasks

Class C
    Shared Function M1() As Task
        Return Nothing
    End Function

    Shared Function M2() As Task
        Return Nothing
    End Function

    Shared Function M3() As Task
        Return Nothing
    End Function

    Shared Sub [End]()
    End Sub

    Shared Function F(t As Func(Of Task)) As Integer
        Return 1
    End Function

    Dim x As Integer = F(<N:4>Async Function()
                                <N:0>Await M1()</N:0>
                                <N:1>Await M2()</N:1>
                                [End]()
                              End Function</N:4>)

    Dim y As Integer = F(<N:5>Async Function()
                                <N:2>Await M1()</N:2>
                                <N:3>Await M2()</N:3>
                                [End]()
                              End Function</N:5>)
End Class")
            Dim source1 = MarkedSource("
Imports System
Imports System.Threading.Tasks

Class C
    Shared Function M1() As Task
        Return Nothing
    End Function

    Shared Function M2() As Task
        Return Nothing
    End Function

    Shared Function M3() As Task
        Return Nothing
    End Function

    Shared Sub [End]()
    End Sub

    Shared Function F(t As Func(Of Task)) As Integer
        Return 1
    End Function

    Dim x As Integer = F(<N:4>Async Function()
                                <N:0>Await M1()</N:0>
                                Await M3()
                                <N:1>Await M2()</N:1>
                                [End]()
                              End Function</N:4>)

    Dim y As Integer = F(<N:5>Async Function()
                                <N:3>Await M2()</N:3>
                                [End]()
                              End Function</N:5>)
End Class")
            Dim compilation0 = CreateEmptyCompilationWithReferences({source0.Tree}, references:=LatestVbReferences, options:=ComSafeDebugDll)
            Dim compilation1 = compilation0.WithSource(source1.Tree)

            Dim v0 = CompileAndVerify(compilation0)
            v0.VerifyDiagnostics()
            Dim md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)

            Dim ctor0 = compilation0.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single()
            Dim ctor1 = compilation1.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single()

            Dim generation0 = CreateInitialBaseline(compilation0, md0, AddressOf v0.CreateSymReader().GetEncMethodDebugInfo)

            Dim diff1 = compilation1.EmitDifference(
                 generation0,
                 ImmutableArray.Create(
                     SemanticEdit.Create(SemanticEditKind.Update, ctor0, ctor1, GetSyntaxMapFromMarkers(source0, source1))))

            v0.VerifyPdb("C..ctor",
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <methods>
        <method containingType="C" name=".ctor">
            <customDebugInfo>
                <encLambdaMap>
                    <methodOrdinal>0</methodOrdinal>
                    <lambda offset="-445"/>
                    <lambda offset="-218"/>
                </encLambdaMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" hidden="true" document="1"/>
                <entry offset="0x7" startLine="25" startColumn="9" endLine="29" endColumn="50" document="1"/>
                <entry offset="0x36" startLine="31" startColumn="9" endLine="35" endColumn="50" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x66">
                <namespace name="System" importlevel="file"/>
                <namespace name="System.Threading.Tasks" importlevel="file"/>
                <currentnamespace name=""/>
            </scope>
        </method>
    </methods>
</symbols>)
            v0.VerifyPdb("C+_Closure$__._Lambda$__0-0",
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <methods>
        <method containingType="C+_Closure$__" name="_Lambda$__0-0">
            <customDebugInfo>
                <forwardIterator name="VB$StateMachine___Lambda$__0-0"/>
                <encStateMachineStateMap>
                    <state number="0" offset="-390"/>
                    <state number="1" offset="-335"/>
                </encStateMachineStateMap>
            </customDebugInfo>
        </method>
    </methods>
</symbols>)
            v0.VerifyPdb("C+_Closure$__._Lambda$__0-1",
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <methods>
        <method containingType="C+_Closure$__" name="_Lambda$__0-1">
            <customDebugInfo>
                <forwardIterator name="VB$StateMachine___Lambda$__0-1"/>
                <encStateMachineStateMap>
                    <state number="0" offset="-163"/>
                    <state number="1" offset="-108"/>
                </encStateMachineStateMap>
            </customDebugInfo>
        </method>
    </methods>
</symbols>)
            v0.VerifyPdb("C+_Closure$__+VB$StateMachine___Lambda$__0-0.MoveNext",
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <methods>
        <method containingType="C+_Closure$__+VB$StateMachine___Lambda$__0-0" name="MoveNext">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="27" offset="-445"/>
                    <slot kind="21" offset="-445"/>
                    <slot kind="33" offset="-390"/>
                    <slot kind="temp"/>
                    <slot kind="33" offset="-335"/>
                    <slot kind="temp"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <scope startOffset="0x0" endOffset="0x130">
                <importsforward declaringType="C" methodName=".ctor"/>
            </scope>
            <asyncInfo>
                <kickoffMethod declaringType="C+_Closure$__" methodName="_Lambda$__0-0"/>
                <await yield="0x37" resume="0x55" declaringType="C+_Closure$__+VB$StateMachine___Lambda$__0-0" methodName="MoveNext"/>
                <await yield="0xa1" resume="0xbd" declaringType="C+_Closure$__+VB$StateMachine___Lambda$__0-0" methodName="MoveNext"/>
            </asyncInfo>
        </method>
    </methods>
</symbols>, options:=PdbValidationOptions.ExcludeSequencePoints)

            v0.VerifyPdb("C+_Closure$__+VB$StateMachine___Lambda$__0-1.MoveNext",
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <methods>
        <method containingType="C+_Closure$__+VB$StateMachine___Lambda$__0-1" name="MoveNext">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="27" offset="-218"/>
                    <slot kind="21" offset="-218"/>
                    <slot kind="33" offset="-163"/>
                    <slot kind="temp"/>
                    <slot kind="33" offset="-108"/>
                    <slot kind="temp"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <scope startOffset="0x0" endOffset="0x130">
                <importsforward declaringType="C" methodName=".ctor"/>
            </scope>
            <asyncInfo>
                <kickoffMethod declaringType="C+_Closure$__" methodName="_Lambda$__0-1"/>
                <await yield="0x37" resume="0x55" declaringType="C+_Closure$__+VB$StateMachine___Lambda$__0-1" methodName="MoveNext"/>
                <await yield="0xa1" resume="0xbd" declaringType="C+_Closure$__+VB$StateMachine___Lambda$__0-1" methodName="MoveNext"/>
            </asyncInfo>
        </method>
    </methods>
</symbols>, options:=PdbValidationOptions.ExcludeSequencePoints)

            diff1.VerifyIL("C._Closure$__.VB$StateMachine___Lambda$__0-0.MoveNext", "
{
  // Code size      426 (0x1aa)
  .maxstack  3
  .locals init (Integer V_0,
                System.Threading.Tasks.Task V_1,
                System.Runtime.CompilerServices.TaskAwaiter V_2,
                C._Closure$__.VB$StateMachine___Lambda$__0-0 V_3,
                System.Runtime.CompilerServices.TaskAwaiter V_4,
                System.Runtime.CompilerServices.TaskAwaiter V_5,
                System.Exception V_6)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C._Closure$__.VB$StateMachine___Lambda$__0-0.$State As Integer""
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
    IL_001d:  br         IL_0137
    IL_0022:  br         IL_00ce
    IL_0027:  nop
    IL_0028:  call       ""Function C.M1() As System.Threading.Tasks.Task""
    IL_002d:  callvirt   ""Function System.Threading.Tasks.Task.GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter""
    IL_0032:  stloc.2
    IL_0033:  ldloca.s   V_2
    IL_0035:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter.get_IsCompleted() As Boolean""
    IL_003a:  brtrue.s   IL_0081
    IL_003c:  ldarg.0
    IL_003d:  ldc.i4.0
    IL_003e:  dup
    IL_003f:  stloc.0
    IL_0040:  stfld      ""C._Closure$__.VB$StateMachine___Lambda$__0-0.$State As Integer""
    IL_0045:  ldarg.0
    IL_0046:  ldloc.2
    IL_0047:  stfld      ""C._Closure$__.VB$StateMachine___Lambda$__0-0.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
    IL_004c:  ldarg.0
    IL_004d:  ldflda     ""C._Closure$__.VB$StateMachine___Lambda$__0-0.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder""
    IL_0052:  ldloca.s   V_2
    IL_0054:  ldarg.0
    IL_0055:  stloc.3
    IL_0056:  ldloca.s   V_3
    IL_0058:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter, C._Closure$__.VB$StateMachine___Lambda$__0-0)(ByRef System.Runtime.CompilerServices.TaskAwaiter, ByRef C._Closure$__.VB$StateMachine___Lambda$__0-0)""
    IL_005d:  nop
    IL_005e:  leave      IL_01a9
    IL_0063:  ldarg.0
    IL_0064:  ldc.i4.m1
    IL_0065:  dup
    IL_0066:  stloc.0
    IL_0067:  stfld      ""C._Closure$__.VB$StateMachine___Lambda$__0-0.$State As Integer""
    IL_006c:  ldarg.0
    IL_006d:  ldfld      ""C._Closure$__.VB$StateMachine___Lambda$__0-0.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
    IL_0072:  stloc.2
    IL_0073:  ldarg.0
    IL_0074:  ldflda     ""C._Closure$__.VB$StateMachine___Lambda$__0-0.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
    IL_0079:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_007f:  br.s       IL_0081
    IL_0081:  ldloca.s   V_2
    IL_0083:  call       ""Sub System.Runtime.CompilerServices.TaskAwaiter.GetResult()""
    IL_0088:  nop
    IL_0089:  ldloca.s   V_2
    IL_008b:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_0091:  call       ""Function C.M3() As System.Threading.Tasks.Task""
    IL_0096:  callvirt   ""Function System.Threading.Tasks.Task.GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter""
    IL_009b:  stloc.s    V_4
    IL_009d:  ldloca.s   V_4
    IL_009f:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter.get_IsCompleted() As Boolean""
    IL_00a4:  brtrue.s   IL_00ed
    IL_00a6:  ldarg.0
    IL_00a7:  ldc.i4.2
    IL_00a8:  dup
    IL_00a9:  stloc.0
    IL_00aa:  stfld      ""C._Closure$__.VB$StateMachine___Lambda$__0-0.$State As Integer""
    IL_00af:  ldarg.0
    IL_00b0:  ldloc.s    V_4
    IL_00b2:  stfld      ""C._Closure$__.VB$StateMachine___Lambda$__0-0.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
    IL_00b7:  ldarg.0
    IL_00b8:  ldflda     ""C._Closure$__.VB$StateMachine___Lambda$__0-0.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder""
    IL_00bd:  ldloca.s   V_4
    IL_00bf:  ldarg.0
    IL_00c0:  stloc.3
    IL_00c1:  ldloca.s   V_3
    IL_00c3:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter, C._Closure$__.VB$StateMachine___Lambda$__0-0)(ByRef System.Runtime.CompilerServices.TaskAwaiter, ByRef C._Closure$__.VB$StateMachine___Lambda$__0-0)""
    IL_00c8:  nop
    IL_00c9:  leave      IL_01a9
    IL_00ce:  ldarg.0
    IL_00cf:  ldc.i4.m1
    IL_00d0:  dup
    IL_00d1:  stloc.0
    IL_00d2:  stfld      ""C._Closure$__.VB$StateMachine___Lambda$__0-0.$State As Integer""
    IL_00d7:  ldarg.0
    IL_00d8:  ldfld      ""C._Closure$__.VB$StateMachine___Lambda$__0-0.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
    IL_00dd:  stloc.s    V_4
    IL_00df:  ldarg.0
    IL_00e0:  ldflda     ""C._Closure$__.VB$StateMachine___Lambda$__0-0.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
    IL_00e5:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_00eb:  br.s       IL_00ed
    IL_00ed:  ldloca.s   V_4
    IL_00ef:  call       ""Sub System.Runtime.CompilerServices.TaskAwaiter.GetResult()""
    IL_00f4:  nop
    IL_00f5:  ldloca.s   V_4
    IL_00f7:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_00fd:  call       ""Function C.M2() As System.Threading.Tasks.Task""
    IL_0102:  callvirt   ""Function System.Threading.Tasks.Task.GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter""
    IL_0107:  stloc.s    V_5
    IL_0109:  ldloca.s   V_5
    IL_010b:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter.get_IsCompleted() As Boolean""
    IL_0110:  brtrue.s   IL_0156
    IL_0112:  ldarg.0
    IL_0113:  ldc.i4.1
    IL_0114:  dup
    IL_0115:  stloc.0
    IL_0116:  stfld      ""C._Closure$__.VB$StateMachine___Lambda$__0-0.$State As Integer""
    IL_011b:  ldarg.0
    IL_011c:  ldloc.s    V_5
    IL_011e:  stfld      ""C._Closure$__.VB$StateMachine___Lambda$__0-0.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
    IL_0123:  ldarg.0
    IL_0124:  ldflda     ""C._Closure$__.VB$StateMachine___Lambda$__0-0.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder""
    IL_0129:  ldloca.s   V_5
    IL_012b:  ldarg.0
    IL_012c:  stloc.3
    IL_012d:  ldloca.s   V_3
    IL_012f:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter, C._Closure$__.VB$StateMachine___Lambda$__0-0)(ByRef System.Runtime.CompilerServices.TaskAwaiter, ByRef C._Closure$__.VB$StateMachine___Lambda$__0-0)""
    IL_0134:  nop
    IL_0135:  leave.s    IL_01a9
    IL_0137:  ldarg.0
    IL_0138:  ldc.i4.m1
    IL_0139:  dup
    IL_013a:  stloc.0
    IL_013b:  stfld      ""C._Closure$__.VB$StateMachine___Lambda$__0-0.$State As Integer""
    IL_0140:  ldarg.0
    IL_0141:  ldfld      ""C._Closure$__.VB$StateMachine___Lambda$__0-0.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
    IL_0146:  stloc.s    V_5
    IL_0148:  ldarg.0
    IL_0149:  ldflda     ""C._Closure$__.VB$StateMachine___Lambda$__0-0.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
    IL_014e:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_0154:  br.s       IL_0156
    IL_0156:  ldloca.s   V_5
    IL_0158:  call       ""Sub System.Runtime.CompilerServices.TaskAwaiter.GetResult()""
    IL_015d:  nop
    IL_015e:  ldloca.s   V_5
    IL_0160:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_0166:  call       ""Sub C.End()""
    IL_016b:  nop
    IL_016c:  leave.s    IL_0193
  }
  catch System.Exception
  {
    IL_016e:  dup
    IL_016f:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)""
    IL_0174:  stloc.s    V_6
    IL_0176:  ldarg.0
    IL_0177:  ldc.i4.s   -2
    IL_0179:  stfld      ""C._Closure$__.VB$StateMachine___Lambda$__0-0.$State As Integer""
    IL_017e:  ldarg.0
    IL_017f:  ldflda     ""C._Closure$__.VB$StateMachine___Lambda$__0-0.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder""
    IL_0184:  ldloc.s    V_6
    IL_0186:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_018b:  nop
    IL_018c:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()""
    IL_0191:  leave.s    IL_01a9
  }
  IL_0193:  ldarg.0
  IL_0194:  ldc.i4.s   -2
  IL_0196:  dup
  IL_0197:  stloc.0
  IL_0198:  stfld      ""C._Closure$__.VB$StateMachine___Lambda$__0-0.$State As Integer""
  IL_019d:  ldarg.0
  IL_019e:  ldflda     ""C._Closure$__.VB$StateMachine___Lambda$__0-0.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder""
  IL_01a3:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_01a8:  nop
  IL_01a9:  ret
}")
            diff1.VerifyIL("C._Closure$__.VB$StateMachine___Lambda$__0-1.MoveNext", "
{
  // Code size      201 (0xc9)
  .maxstack  3
  .locals init (Integer V_0,
                System.Threading.Tasks.Task V_1,
                System.Runtime.CompilerServices.TaskAwaiter V_2,
                C._Closure$__.VB$StateMachine___Lambda$__0-1 V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C._Closure$__.VB$StateMachine___Lambda$__0-1.$State As Integer""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  ldc.i4.1
    IL_0009:  beq.s      IL_000d
    IL_000b:  br.s       IL_000f
    IL_000d:  br.s       IL_0057
    IL_000f:  ldloc.0
    IL_0010:  ldc.i4.0
    IL_0011:  blt.s      IL_001e
    IL_0013:  ldstr      """ & CodeAnalysisResources.EncCannotResumeSuspendedAsyncMethod & """
    IL_0018:  newobj     ""Sub System.InvalidOperationException..ctor(String)""
    IL_001d:  throw
    IL_001e:  nop
    IL_001f:  call       ""Function C.M2() As System.Threading.Tasks.Task""
    IL_0024:  callvirt   ""Function System.Threading.Tasks.Task.GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter""
    IL_0029:  stloc.2
    IL_002a:  ldloca.s   V_2
    IL_002c:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter.get_IsCompleted() As Boolean""
    IL_0031:  brtrue.s   IL_0075
    IL_0033:  ldarg.0
    IL_0034:  ldc.i4.1
    IL_0035:  dup
    IL_0036:  stloc.0
    IL_0037:  stfld      ""C._Closure$__.VB$StateMachine___Lambda$__0-1.$State As Integer""
    IL_003c:  ldarg.0
    IL_003d:  ldloc.2
    IL_003e:  stfld      ""C._Closure$__.VB$StateMachine___Lambda$__0-1.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
    IL_0043:  ldarg.0
    IL_0044:  ldflda     ""C._Closure$__.VB$StateMachine___Lambda$__0-1.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder""
    IL_0049:  ldloca.s   V_2
    IL_004b:  ldarg.0
    IL_004c:  stloc.3
    IL_004d:  ldloca.s   V_3
    IL_004f:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter, C._Closure$__.VB$StateMachine___Lambda$__0-1)(ByRef System.Runtime.CompilerServices.TaskAwaiter, ByRef C._Closure$__.VB$StateMachine___Lambda$__0-1)""
    IL_0054:  nop
    IL_0055:  leave.s    IL_00c8
    IL_0057:  ldarg.0
    IL_0058:  ldc.i4.m1
    IL_0059:  dup
    IL_005a:  stloc.0
    IL_005b:  stfld      ""C._Closure$__.VB$StateMachine___Lambda$__0-1.$State As Integer""
    IL_0060:  ldarg.0
    IL_0061:  ldfld      ""C._Closure$__.VB$StateMachine___Lambda$__0-1.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
    IL_0066:  stloc.2
    IL_0067:  ldarg.0
    IL_0068:  ldflda     ""C._Closure$__.VB$StateMachine___Lambda$__0-1.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
    IL_006d:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_0073:  br.s       IL_0075
    IL_0075:  ldloca.s   V_2
    IL_0077:  call       ""Sub System.Runtime.CompilerServices.TaskAwaiter.GetResult()""
    IL_007c:  nop
    IL_007d:  ldloca.s   V_2
    IL_007f:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_0085:  call       ""Sub C.End()""
    IL_008a:  nop
    IL_008b:  leave.s    IL_00b2
  }
  catch System.Exception
  {
    IL_008d:  dup
    IL_008e:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)""
    IL_0093:  stloc.s    V_4
    IL_0095:  ldarg.0
    IL_0096:  ldc.i4.s   -2
    IL_0098:  stfld      ""C._Closure$__.VB$StateMachine___Lambda$__0-1.$State As Integer""
    IL_009d:  ldarg.0
    IL_009e:  ldflda     ""C._Closure$__.VB$StateMachine___Lambda$__0-1.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder""
    IL_00a3:  ldloc.s    V_4
    IL_00a5:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00aa:  nop
    IL_00ab:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()""
    IL_00b0:  leave.s    IL_00c8
  }
  IL_00b2:  ldarg.0
  IL_00b3:  ldc.i4.s   -2
  IL_00b5:  dup
  IL_00b6:  stloc.0
  IL_00b7:  stfld      ""C._Closure$__.VB$StateMachine___Lambda$__0-1.$State As Integer""
  IL_00bc:  ldarg.0
  IL_00bd:  ldflda     ""C._Closure$__.VB$StateMachine___Lambda$__0-1.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder""
  IL_00c2:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00c7:  nop
  IL_00c8:  ret
}")
        End Sub

        <Fact>
        Public Sub UpdateAsync_Await_Remove_FirstAndLast()
            Dim source0 = MarkedSource("
Imports System.Threading.Tasks
Class C
    Shared Function M1() As Task
        Return Nothing
    End Function

    Shared Function M2() As Task
        Return Nothing
    End Function

    Shared Function M3() As Task
        Return Nothing
    End Function

    Shared Sub [End]()
    End Sub

    Shared Async Function F() As Task
        <N:0>Await M1()</N:0>
        <N:1>Await M2()</N:1>
        <N:2>Await M3()</N:2>
        [End]()
    End Function
End Class")
            Dim source1 = MarkedSource("
Imports System.Threading.Tasks
Class C
    Shared Function M1() As Task
        Return Nothing
    End Function

    Shared Function M2() As Task
        Return Nothing
    End Function

    Shared Function M3() As Task
        Return Nothing
    End Function

    Shared Sub [End]()
    End Sub

    Shared Async Function F() As Task
        <N:1>Await M2()</N:1>
        [End]()
    End Function
End Class")

            Dim compilation0 = CreateEmptyCompilationWithReferences(source0.Tree, references:=LatestVbReferences, options:=ComSafeDebugDll)
            Dim compilation1 = compilation0.WithSource(source1.Tree)

            Dim f0 = compilation0.GetMember(Of MethodSymbol)("C.F")
            Dim f1 = compilation1.GetMember(Of MethodSymbol)("C.F")

            Dim v0 = CompileAndVerify(compilation:=compilation0).VerifyDiagnostics()
            Using md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)

                Dim generation0 = CreateInitialBaseline(compilation0, md0, Function(handle) v0.CreateSymReader().GetEncMethodDebugInfo(handle))
                Dim diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))))

                diff1.VerifyIL("C.VB$StateMachine_5_F.MoveNext", "
{
  // Code size      199 (0xc7)
  .maxstack  3
  .locals init (Integer V_0,
                System.Runtime.CompilerServices.TaskAwaiter V_1,
                C.VB$StateMachine_5_F V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.VB$StateMachine_5_F.$State As Integer""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  ldc.i4.1
    IL_0009:  beq.s      IL_000d
    IL_000b:  br.s       IL_000f
    IL_000d:  br.s       IL_0057
    IL_000f:  ldloc.0
    IL_0010:  ldc.i4.0
    IL_0011:  blt.s      IL_001e
    IL_0013:  ldstr      """ & CodeAnalysisResources.EncCannotResumeSuspendedAsyncMethod & """
    IL_0018:  newobj     ""Sub System.InvalidOperationException..ctor(String)""
    IL_001d:  throw
    IL_001e:  nop
    IL_001f:  call       ""Function C.M2() As System.Threading.Tasks.Task""
    IL_0024:  callvirt   ""Function System.Threading.Tasks.Task.GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter""
    IL_0029:  stloc.1
    IL_002a:  ldloca.s   V_1
    IL_002c:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter.get_IsCompleted() As Boolean""
    IL_0031:  brtrue.s   IL_0075
    IL_0033:  ldarg.0
    IL_0034:  ldc.i4.1
    IL_0035:  dup
    IL_0036:  stloc.0
    IL_0037:  stfld      ""C.VB$StateMachine_5_F.$State As Integer""
    IL_003c:  ldarg.0
    IL_003d:  ldloc.1
    IL_003e:  stfld      ""C.VB$StateMachine_5_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
    IL_0043:  ldarg.0
    IL_0044:  ldflda     ""C.VB$StateMachine_5_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder""
    IL_0049:  ldloca.s   V_1
    IL_004b:  ldarg.0
    IL_004c:  stloc.2
    IL_004d:  ldloca.s   V_2
    IL_004f:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter, C.VB$StateMachine_5_F)(ByRef System.Runtime.CompilerServices.TaskAwaiter, ByRef C.VB$StateMachine_5_F)""
    IL_0054:  nop
    IL_0055:  leave.s    IL_00c6
    IL_0057:  ldarg.0
    IL_0058:  ldc.i4.m1
    IL_0059:  dup
    IL_005a:  stloc.0
    IL_005b:  stfld      ""C.VB$StateMachine_5_F.$State As Integer""
    IL_0060:  ldarg.0
    IL_0061:  ldfld      ""C.VB$StateMachine_5_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
    IL_0066:  stloc.1
    IL_0067:  ldarg.0
    IL_0068:  ldflda     ""C.VB$StateMachine_5_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
    IL_006d:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_0073:  br.s       IL_0075
    IL_0075:  ldloca.s   V_1
    IL_0077:  call       ""Sub System.Runtime.CompilerServices.TaskAwaiter.GetResult()""
    IL_007c:  nop
    IL_007d:  ldloca.s   V_1
    IL_007f:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_0085:  call       ""Sub C.End()""
    IL_008a:  nop
    IL_008b:  leave.s    IL_00b0
  }
  catch System.Exception
  {
    IL_008d:  dup
    IL_008e:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)""
    IL_0093:  stloc.3
    IL_0094:  ldarg.0
    IL_0095:  ldc.i4.s   -2
    IL_0097:  stfld      ""C.VB$StateMachine_5_F.$State As Integer""
    IL_009c:  ldarg.0
    IL_009d:  ldflda     ""C.VB$StateMachine_5_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder""
    IL_00a2:  ldloc.3
    IL_00a3:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00a8:  nop
    IL_00a9:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()""
    IL_00ae:  leave.s    IL_00c6
  }
  IL_00b0:  ldarg.0
  IL_00b1:  ldc.i4.s   -2
  IL_00b3:  dup
  IL_00b4:  stloc.0
  IL_00b5:  stfld      ""C.VB$StateMachine_5_F.$State As Integer""
  IL_00ba:  ldarg.0
  IL_00bb:  ldflda     ""C.VB$StateMachine_5_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder""
  IL_00c0:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00c5:  nop
  IL_00c6:  ret
}
")
            End Using
        End Sub

        <Fact>
        Public Sub UpdateAsync_Await_Remove_TryBlock()
            Dim template = "
Imports System.Threading.Tasks
Class C
    Shared Sub Start()
    End Sub

    Shared Function M1() As Task
        Return Nothing
    End Function

    Shared Function M2() As Task
        Return Nothing
    End Function

    Shared Function M3() As Task
        Return Nothing
    End Function

    Shared Function M4() As Task
        Return Nothing
    End Function

    Shared Function M5() As Task
        Return Nothing
    End Function

    Shared Sub [End]()
    End Sub

    Shared Async Function F() As Task
{0}
    End Function
End Class"

            Dim source0 = MarkedSource(String.Format(template, "
Start()
<N:0>Await M1()</N:0>
<N:1>Try
    <N:2>Await M2()</N:2>
    <N:3>Await M3()</N:3>
Catch
End Try</N:1>
[End]()
"))
            Dim source1 = MarkedSource(String.Format(template, "
Start()
<N:1>Try
    <N:2>Await M2()</N:2>
Catch
End Try</N:1>
[End]()
"))

            Dim compilation0 = CreateEmptyCompilationWithReferences(source0.Tree, references:=LatestVbReferences, options:=ComSafeDebugDll)
            Dim compilation1 = compilation0.WithSource(source1.Tree)

            Dim f0 = compilation0.GetMember(Of MethodSymbol)("C.F")
            Dim f1 = compilation1.GetMember(Of MethodSymbol)("C.F")

            Dim v0 = CompileAndVerify(compilation:=compilation0).VerifyDiagnostics()
            Using md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)

                Dim generation0 = CreateInitialBaseline(compilation0, md0, Function(handle) v0.CreateSymReader().GetEncMethodDebugInfo(handle))
                Dim diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))))

                v0.VerifyIL("C.VB$StateMachine_8_F.MoveNext", "
 {
  // Code size      501 (0x1f5)
  .maxstack  3
  .locals init (Integer V_0,
                System.Runtime.CompilerServices.TaskAwaiter V_1,
                C.VB$StateMachine_8_F V_2,
                System.Runtime.CompilerServices.TaskAwaiter V_3,
                System.Runtime.CompilerServices.TaskAwaiter V_4,
                System.Exception V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.VB$StateMachine_8_F.$State As Integer""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  ldc.i4.s   -3
    IL_000a:  sub
    IL_000b:  switch    (
        IL_002a,
        IL_002e,
        IL_002e,
        IL_002c,
        IL_002a,
        IL_002a)
    IL_0028:  br.s       IL_002e
    IL_002a:  br.s       IL_009e
    IL_002c:  br.s       IL_0070
    IL_002e:  nop
    IL_002f:  call       ""Sub C.Start()""
    IL_0034:  nop
    IL_0035:  call       ""Function C.M1() As System.Threading.Tasks.Task""
    IL_003a:  callvirt   ""Function System.Threading.Tasks.Task.GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter""
    IL_003f:  stloc.1
    IL_0040:  ldloca.s   V_1
    IL_0042:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter.get_IsCompleted() As Boolean""
    IL_0047:  brtrue.s   IL_008e
    IL_0049:  ldarg.0
    IL_004a:  ldc.i4.0
    IL_004b:  dup
    IL_004c:  stloc.0
    IL_004d:  stfld      ""C.VB$StateMachine_8_F.$State As Integer""
    IL_0052:  ldarg.0
    IL_0053:  ldloc.1
    IL_0054:  stfld      ""C.VB$StateMachine_8_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
    IL_0059:  ldarg.0
    IL_005a:  ldflda     ""C.VB$StateMachine_8_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder""
    IL_005f:  ldloca.s   V_1
    IL_0061:  ldarg.0
    IL_0062:  stloc.2
    IL_0063:  ldloca.s   V_2
    IL_0065:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter, C.VB$StateMachine_8_F)(ByRef System.Runtime.CompilerServices.TaskAwaiter, ByRef C.VB$StateMachine_8_F)""
    IL_006a:  nop
    IL_006b:  leave      IL_01f4
    IL_0070:  ldarg.0
    IL_0071:  ldc.i4.m1
    IL_0072:  dup
    IL_0073:  stloc.0
    IL_0074:  stfld      ""C.VB$StateMachine_8_F.$State As Integer""
    IL_0079:  ldarg.0
    IL_007a:  ldfld      ""C.VB$StateMachine_8_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
    IL_007f:  stloc.1
    IL_0080:  ldarg.0
    IL_0081:  ldflda     ""C.VB$StateMachine_8_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
    IL_0086:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_008c:  br.s       IL_008e
    IL_008e:  ldloca.s   V_1
    IL_0090:  call       ""Sub System.Runtime.CompilerServices.TaskAwaiter.GetResult()""
    IL_0095:  nop
    IL_0096:  ldloca.s   V_1
    IL_0098:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_009e:  nop
    .try
    {
      IL_009f:  ldloc.0
      IL_00a0:  ldc.i4.s   -3
      IL_00a2:  beq.s      IL_00b2
      IL_00a4:  br.s       IL_00a6
      IL_00a6:  ldloc.0
      IL_00a7:  ldc.i4.1
      IL_00a8:  beq.s      IL_00b4
      IL_00aa:  br.s       IL_00ac
      IL_00ac:  ldloc.0
      IL_00ad:  ldc.i4.2
      IL_00ae:  beq.s      IL_00b6
      IL_00b0:  br.s       IL_00bb
      IL_00b2:  br.s       IL_00bd
      IL_00b4:  br.s       IL_0107
      IL_00b6:  br         IL_0172
      IL_00bb:  br.s       IL_00cb
      IL_00bd:  ldarg.0
      IL_00be:  ldc.i4.m1
      IL_00bf:  dup
      IL_00c0:  stloc.0
      IL_00c1:  stfld      ""C.VB$StateMachine_8_F.$State As Integer""
      IL_00c6:  leave      IL_01f4
      IL_00cb:  nop
      IL_00cc:  call       ""Function C.M2() As System.Threading.Tasks.Task""
      IL_00d1:  callvirt   ""Function System.Threading.Tasks.Task.GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter""
      IL_00d6:  stloc.3
      IL_00d7:  ldloca.s   V_3
      IL_00d9:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter.get_IsCompleted() As Boolean""
      IL_00de:  brtrue.s   IL_0125
      IL_00e0:  ldarg.0
      IL_00e1:  ldc.i4.1
      IL_00e2:  dup
      IL_00e3:  stloc.0
      IL_00e4:  stfld      ""C.VB$StateMachine_8_F.$State As Integer""
      IL_00e9:  ldarg.0
      IL_00ea:  ldloc.3
      IL_00eb:  stfld      ""C.VB$StateMachine_8_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
      IL_00f0:  ldarg.0
      IL_00f1:  ldflda     ""C.VB$StateMachine_8_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder""
      IL_00f6:  ldloca.s   V_3
      IL_00f8:  ldarg.0
      IL_00f9:  stloc.2
      IL_00fa:  ldloca.s   V_2
      IL_00fc:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter, C.VB$StateMachine_8_F)(ByRef System.Runtime.CompilerServices.TaskAwaiter, ByRef C.VB$StateMachine_8_F)""
      IL_0101:  nop
      IL_0102:  leave      IL_01f4
      IL_0107:  ldarg.0
      IL_0108:  ldc.i4.m1
      IL_0109:  dup
      IL_010a:  stloc.0
      IL_010b:  stfld      ""C.VB$StateMachine_8_F.$State As Integer""
      IL_0110:  ldarg.0
      IL_0111:  ldfld      ""C.VB$StateMachine_8_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
      IL_0116:  stloc.3
      IL_0117:  ldarg.0
      IL_0118:  ldflda     ""C.VB$StateMachine_8_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
      IL_011d:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
      IL_0123:  br.s       IL_0125
      IL_0125:  ldloca.s   V_3
      IL_0127:  call       ""Sub System.Runtime.CompilerServices.TaskAwaiter.GetResult()""
      IL_012c:  nop
      IL_012d:  ldloca.s   V_3
      IL_012f:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
      IL_0135:  call       ""Function C.M3() As System.Threading.Tasks.Task""
      IL_013a:  callvirt   ""Function System.Threading.Tasks.Task.GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter""
      IL_013f:  stloc.s    V_4
      IL_0141:  ldloca.s   V_4
      IL_0143:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter.get_IsCompleted() As Boolean""
      IL_0148:  brtrue.s   IL_0191
      IL_014a:  ldarg.0
      IL_014b:  ldc.i4.2
      IL_014c:  dup
      IL_014d:  stloc.0
      IL_014e:  stfld      ""C.VB$StateMachine_8_F.$State As Integer""
      IL_0153:  ldarg.0
      IL_0154:  ldloc.s    V_4
      IL_0156:  stfld      ""C.VB$StateMachine_8_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
      IL_015b:  ldarg.0
      IL_015c:  ldflda     ""C.VB$StateMachine_8_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder""
      IL_0161:  ldloca.s   V_4
      IL_0163:  ldarg.0
      IL_0164:  stloc.2
      IL_0165:  ldloca.s   V_2
      IL_0167:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter, C.VB$StateMachine_8_F)(ByRef System.Runtime.CompilerServices.TaskAwaiter, ByRef C.VB$StateMachine_8_F)""
      IL_016c:  nop
      IL_016d:  leave      IL_01f4
      IL_0172:  ldarg.0
      IL_0173:  ldc.i4.m1
      IL_0174:  dup
      IL_0175:  stloc.0
      IL_0176:  stfld      ""C.VB$StateMachine_8_F.$State As Integer""
      IL_017b:  ldarg.0
      IL_017c:  ldfld      ""C.VB$StateMachine_8_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
      IL_0181:  stloc.s    V_4
      IL_0183:  ldarg.0
      IL_0184:  ldflda     ""C.VB$StateMachine_8_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
      IL_0189:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
      IL_018f:  br.s       IL_0191
      IL_0191:  ldloca.s   V_4
      IL_0193:  call       ""Sub System.Runtime.CompilerServices.TaskAwaiter.GetResult()""
      IL_0198:  nop
      IL_0199:  ldloca.s   V_4
      IL_019b:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
      IL_01a1:  leave.s    IL_01b0
    }
    catch System.Exception
    {
      IL_01a3:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)""
      IL_01a8:  nop
      IL_01a9:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()""
      IL_01ae:  leave.s    IL_01b0
    }
    IL_01b0:  nop
    IL_01b1:  call       ""Sub C.End()""
    IL_01b6:  nop
    IL_01b7:  leave.s    IL_01de
  }
  catch System.Exception
  {
    IL_01b9:  dup
    IL_01ba:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)""
    IL_01bf:  stloc.s    V_5
    IL_01c1:  ldarg.0
    IL_01c2:  ldc.i4.s   -2
    IL_01c4:  stfld      ""C.VB$StateMachine_8_F.$State As Integer""
    IL_01c9:  ldarg.0
    IL_01ca:  ldflda     ""C.VB$StateMachine_8_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder""
    IL_01cf:  ldloc.s    V_5
    IL_01d1:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_01d6:  nop
    IL_01d7:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()""
    IL_01dc:  leave.s    IL_01f4
  }
  IL_01de:  ldarg.0
  IL_01df:  ldc.i4.s   -2
  IL_01e1:  dup
  IL_01e2:  stloc.0
  IL_01e3:  stfld      ""C.VB$StateMachine_8_F.$State As Integer""
  IL_01e8:  ldarg.0
  IL_01e9:  ldflda     ""C.VB$StateMachine_8_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder""
  IL_01ee:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_01f3:  nop
  IL_01f4:  ret
}")
                diff1.VerifyIL("C.VB$StateMachine_8_F.MoveNext", "
{
  // Code size      263 (0x107)
  .maxstack  3
  .locals init (Integer V_0,
                System.Runtime.CompilerServices.TaskAwaiter V_1,
                C.VB$StateMachine_8_F V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.VB$StateMachine_8_F.$State As Integer""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  ldc.i4.s   -3
    IL_000a:  beq.s      IL_0014
    IL_000c:  br.s       IL_000e
    IL_000e:  ldloc.0
    IL_000f:  ldc.i4.1
    IL_0010:  beq.s      IL_0014
    IL_0012:  br.s       IL_0016
    IL_0014:  br.s       IL_002c
    IL_0016:  ldloc.0
    IL_0017:  ldc.i4.0
    IL_0018:  blt.s      IL_0025
    IL_001a:  ldstr      """ & CodeAnalysisResources.EncCannotResumeSuspendedAsyncMethod & """
    IL_001f:  newobj     ""Sub System.InvalidOperationException..ctor(String)""
    IL_0024:  throw
    IL_0025:  nop
    IL_0026:  call       ""Sub C.Start()""
    IL_002b:  nop
    IL_002c:  nop
    .try
    {
      IL_002d:  ldloc.0
      IL_002e:  ldc.i4.s   -3
      IL_0030:  beq.s      IL_003a
      IL_0032:  br.s       IL_0034
      IL_0034:  ldloc.0
      IL_0035:  ldc.i4.1
      IL_0036:  beq.s      IL_003c
      IL_0038:  br.s       IL_003e
      IL_003a:  br.s       IL_0040
      IL_003c:  br.s       IL_0087
      IL_003e:  br.s       IL_004e
      IL_0040:  ldarg.0
      IL_0041:  ldc.i4.m1
      IL_0042:  dup
      IL_0043:  stloc.0
      IL_0044:  stfld      ""C.VB$StateMachine_8_F.$State As Integer""
      IL_0049:  leave      IL_0106
      IL_004e:  nop
      IL_004f:  call       ""Function C.M2() As System.Threading.Tasks.Task""
      IL_0054:  callvirt   ""Function System.Threading.Tasks.Task.GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter""
      IL_0059:  stloc.1
      IL_005a:  ldloca.s   V_1
      IL_005c:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter.get_IsCompleted() As Boolean""
      IL_0061:  brtrue.s   IL_00a5
      IL_0063:  ldarg.0
      IL_0064:  ldc.i4.1
      IL_0065:  dup
      IL_0066:  stloc.0
      IL_0067:  stfld      ""C.VB$StateMachine_8_F.$State As Integer""
      IL_006c:  ldarg.0
      IL_006d:  ldloc.1
      IL_006e:  stfld      ""C.VB$StateMachine_8_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
      IL_0073:  ldarg.0
      IL_0074:  ldflda     ""C.VB$StateMachine_8_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder""
      IL_0079:  ldloca.s   V_1
      IL_007b:  ldarg.0
      IL_007c:  stloc.2
      IL_007d:  ldloca.s   V_2
      IL_007f:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter, C.VB$StateMachine_8_F)(ByRef System.Runtime.CompilerServices.TaskAwaiter, ByRef C.VB$StateMachine_8_F)""
      IL_0084:  nop
      IL_0085:  leave.s    IL_0106
      IL_0087:  ldarg.0
      IL_0088:  ldc.i4.m1
      IL_0089:  dup
      IL_008a:  stloc.0
      IL_008b:  stfld      ""C.VB$StateMachine_8_F.$State As Integer""
      IL_0090:  ldarg.0
      IL_0091:  ldfld      ""C.VB$StateMachine_8_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
      IL_0096:  stloc.1
      IL_0097:  ldarg.0
      IL_0098:  ldflda     ""C.VB$StateMachine_8_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
      IL_009d:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
      IL_00a3:  br.s       IL_00a5
      IL_00a5:  ldloca.s   V_1
      IL_00a7:  call       ""Sub System.Runtime.CompilerServices.TaskAwaiter.GetResult()""
      IL_00ac:  nop
      IL_00ad:  ldloca.s   V_1
      IL_00af:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
      IL_00b5:  leave.s    IL_00c4
    }
    catch System.Exception
    {
      IL_00b7:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)""
      IL_00bc:  nop
      IL_00bd:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()""
      IL_00c2:  leave.s    IL_00c4
    }
    IL_00c4:  nop
    IL_00c5:  call       ""Sub C.End()""
    IL_00ca:  nop
    IL_00cb:  leave.s    IL_00f0
  }
  catch System.Exception
  {
    IL_00cd:  dup
    IL_00ce:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)""
    IL_00d3:  stloc.3
    IL_00d4:  ldarg.0
    IL_00d5:  ldc.i4.s   -2
    IL_00d7:  stfld      ""C.VB$StateMachine_8_F.$State As Integer""
    IL_00dc:  ldarg.0
    IL_00dd:  ldflda     ""C.VB$StateMachine_8_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder""
    IL_00e2:  ldloc.3
    IL_00e3:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00e8:  nop
    IL_00e9:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()""
    IL_00ee:  leave.s    IL_0106
  }
  IL_00f0:  ldarg.0
  IL_00f1:  ldc.i4.s   -2
  IL_00f3:  dup
  IL_00f4:  stloc.0
  IL_00f5:  stfld      ""C.VB$StateMachine_8_F.$State As Integer""
  IL_00fa:  ldarg.0
  IL_00fb:  ldflda     ""C.VB$StateMachine_8_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder""
  IL_0100:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_0105:  nop
  IL_0106:  ret
}")
            End Using
        End Sub

        <Fact>
        Public Sub UpdateIterator_UserDefinedVariables_NoChange()
            Dim source0 = MarkedSource("
Imports System.Collections.Generic
Class C
    Iterator Function F(p as Integer) As IEnumerable(Of Integer)
        Dim <N:0>x</N:0> = p
        <N:1>Yield 1</N:1>
    End Function
End Class")
            Dim source1 = MarkedSource("
Imports System.Collections.Generic
Class C
    Iterator Function F(p as Integer) As IEnumerable(Of Integer)
        Dim <N:0>x</N:0> = p
        <N:1>Yield 2</N:1>
    End Function
End Class")

            Dim compilation0 = CreateEmptyCompilationWithReferences(source0.Tree, references:=LatestVbReferences, options:=ComSafeDebugDll)
            Dim compilation1 = compilation0.WithSource(source1.Tree)

            Dim v0 = CompileAndVerify(compilation:=compilation0)
            Dim symReader = v0.CreateSymReader()

            Using md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)
                Dim method0 = compilation0.GetMember(Of MethodSymbol)("C.F")
                Dim method1 = compilation1.GetMember(Of MethodSymbol)("C.F")

                Dim generation0 = CreateInitialBaseline(compilation0, md0, Function(handle) symReader.GetEncMethodDebugInfo(handle))
                Dim diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, method0, method1, GetSyntaxMapFromMarkers(source0, source1))))

                ' only methods with sequence points should be listed in UpdatedMethods:
                diff1.VerifyUpdatedMethods("0x06000005")

                ' verify delta metadata contains expected rows
                Using md1 = diff1.GetMetadata()
                    '  Verify that no new TypeDefs, FieldDefs or MethodDefs were added and 3 methods were updated:
                    CheckEncLogDefinitions(md1.Reader,
                        Row(3, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(4, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(1, TableIndex.Param, EditAndContinueOperation.Default),
                        Row(4, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(6, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(7, TableIndex.CustomAttribute, EditAndContinueOperation.Default))
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
            Dim source0 = MarkedSource("
Imports System.Collections.Generic
Class C
    Iterator Function F(p as Integer) As IEnumerable(Of Integer)
        Dim <N:0>x</N:0> = p
        <N:1>Yield 1</N:1>
    End Function
End Class")
            Dim source1 = MarkedSource("
Imports System.Collections.Generic
Class C
    Iterator Function F(p as Integer) As IEnumerable(Of Integer)
        Dim <N:0>x</N:0> = p
        dim y = 1234
        <N:1>Yield y</N:1>
    End Function
End Class")

            Dim compilation0 = CreateEmptyCompilationWithReferences(source0.Tree, references:=LatestVbReferences, options:=ComSafeDebugDll)
            Dim compilation1 = compilation0.WithSource(source1.Tree)

            Dim v0 = CompileAndVerify(compilation:=compilation0)
            Dim symReader = v0.CreateSymReader()

            Using md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)
                Dim method0 = compilation0.GetMember(Of MethodSymbol)("C.F")
                Dim method1 = compilation1.GetMember(Of MethodSymbol)("C.F")

                Dim generation0 = CreateInitialBaseline(compilation0, md0, Function(handle) symReader.GetEncMethodDebugInfo(handle))
                Dim diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, method0, method1, GetSyntaxMapFromMarkers(source0, source1))))

                ' only methods with sequence points should be listed in UpdatedMethods:
                diff1.VerifyUpdatedMethods("0x06000005")

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
                        Row(1, TableIndex.Param, EditAndContinueOperation.Default),
                        Row(4, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(6, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(7, TableIndex.CustomAttribute, EditAndContinueOperation.Default))
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
            Dim source0 = MarkedSource("
Imports System.Collections.Generic
Class C
    Iterator Function F(p as Integer) As IEnumerable(Of Integer)
        dim x = p
        <N:0>Yield 1</N:0>
    End Function
End Class")
            Dim source1 = MarkedSource("
Imports System.Collections.Generic
Class C
    Iterator Function F(p as Integer) As IEnumerable(Of Integer)
        dim y = 1234
        <N:0>Yield p</N:0>
    End Function
End Class")

            Dim compilation0 = CreateEmptyCompilationWithReferences(source0.Tree, references:=LatestVbReferences, options:=ComSafeDebugDll)
            Dim compilation1 = compilation0.WithSource(source1.Tree)

            Dim v0 = CompileAndVerify(compilation:=compilation0)
            Dim symReader = v0.CreateSymReader()

            Using md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)
                Dim method0 = compilation0.GetMember(Of MethodSymbol)("C.F")
                Dim method1 = compilation1.GetMember(Of MethodSymbol)("C.F")

                Dim generation0 = CreateInitialBaseline(compilation0, md0, Function(handle) symReader.GetEncMethodDebugInfo(handle))
                Dim diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, method0, method1, GetSyntaxMapFromMarkers(source0, source1))))

                ' only methods with sequence points should be listed in UpdatedMethods:
                diff1.VerifyUpdatedMethods("0x06000005")

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
                        Row(1, TableIndex.Param, EditAndContinueOperation.Default),
                        Row(4, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(6, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(7, TableIndex.CustomAttribute, EditAndContinueOperation.Default))
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
            Dim source0 = MarkedSource("
Imports System.Collections.Generic
Class C
    Iterator Function F() As IEnumerable(Of Integer)
        dim <N:0>x</N:0> = 10.0
        <N:1>Yield 1</N:1>
    End Function
End Class")
            Dim source1 = MarkedSource("
Imports System.Collections.Generic
Class C
    Iterator Function F() As IEnumerable(Of Integer)
        dim <N:0>x</N:0> = 1234
        <N:1>Yield 0</N:1>
    End Function
End Class")

            Dim compilation0 = CreateEmptyCompilationWithReferences(source0.Tree, references:=LatestVbReferences, options:=ComSafeDebugDll)
            Dim compilation1 = compilation0.WithSource(source1.Tree)

            Dim v0 = CompileAndVerify(compilation:=compilation0)
            Dim symReader = v0.CreateSymReader()

            Using md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)
                Dim method0 = compilation0.GetMember(Of MethodSymbol)("C.F")
                Dim method1 = compilation1.GetMember(Of MethodSymbol)("C.F")

                Dim generation0 = CreateInitialBaseline(compilation0, md0, Function(handle) symReader.GetEncMethodDebugInfo(handle))
                Dim diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, method0, method1, GetSyntaxMapFromMarkers(source0, source1))))

                ' only methods with sequence points should be listed in UpdatedMethods:
                diff1.VerifyUpdatedMethods("0x06000005")

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
                        Row(4, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(6, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(7, TableIndex.CustomAttribute, EditAndContinueOperation.Default))
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
            Dim source0 = MarkedSource("
Imports System.Collections.Generic
Class C
    Iterator Function F() As IEnumerable(Of Integer)
        <N:1>For Each <N:0>x</N:0> In {1, 2}</N:1>
            <N:2>Yield 1</N:2>
        Next
    End Function
End Class")
            Dim source1 = MarkedSource("
Imports System.Collections.Generic
Class C
    Iterator Function F() As IEnumerable(Of Integer)
        <N:1>For Each <N:0>x</N:0> In {1.0, 2.0}</N:1>
            <N:2>Yield 1</N:2>
        Next
    End Function
End Class")

            ' Rude edit but the compiler should handle it

            Dim compilation0 = CreateEmptyCompilationWithReferences(source0.Tree, references:=LatestVbReferences, options:=ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            Dim compilation1 = compilation0.WithSource(source1.Tree)

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

                Dim generation0 = CreateInitialBaseline(compilation0, md0, Function(handle) symReader.GetEncMethodDebugInfo(handle))
                Dim diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, method0, method1, GetSyntaxMapFromMarkers(source0, source1))))

                ' only methods with sequence points should be listed in UpdatedMethods:
                diff1.VerifyUpdatedMethods("0x06000005")

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
                        Row(4, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(6, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(7, TableIndex.CustomAttribute, EditAndContinueOperation.Default))
                End Using

                diff1.VerifyIL("C.VB$StateMachine_1_F.MoveNext()", "
{
  // Code size      163 (0xa3)
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
  IL_0014:  br.s       IL_0075
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
  IL_004d:  br.s       IL_008d
  IL_004f:  ldarg.0
  IL_0050:  ldarg.0
  IL_0051:  ldfld      ""C.VB$StateMachine_1_F.$S3 As Double()""
  IL_0056:  ldarg.0
  IL_0057:  ldfld      ""C.VB$StateMachine_1_F.$S1 As Integer""
  IL_005c:  ldelem.r8
  IL_005d:  conv.r8
  IL_005e:  stfld      ""C.VB$StateMachine_1_F.$VB$ResumableLocal_x$4 As Double""
  IL_0063:  ldarg.0
  IL_0064:  ldc.i4.1
  IL_0065:  stfld      ""C.VB$StateMachine_1_F.$Current As Integer""
  IL_006a:  ldarg.0
  IL_006b:  ldc.i4.1
  IL_006c:  dup
  IL_006d:  stloc.1
  IL_006e:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_0073:  ldc.i4.1
  IL_0074:  ret
  IL_0075:  ldarg.0
  IL_0076:  ldc.i4.m1
  IL_0077:  dup
  IL_0078:  stloc.1
  IL_0079:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_007e:  nop
  IL_007f:  ldarg.0
  IL_0080:  ldarg.0
  IL_0081:  ldfld      ""C.VB$StateMachine_1_F.$S1 As Integer""
  IL_0086:  ldc.i4.1
  IL_0087:  add.ovf
  IL_0088:  stfld      ""C.VB$StateMachine_1_F.$S1 As Integer""
  IL_008d:  ldarg.0
  IL_008e:  ldfld      ""C.VB$StateMachine_1_F.$S1 As Integer""
  IL_0093:  ldarg.0
  IL_0094:  ldfld      ""C.VB$StateMachine_1_F.$S3 As Double()""
  IL_0099:  ldlen
  IL_009a:  conv.i4
  IL_009b:  clt
  IL_009d:  stloc.2
  IL_009e:  ldloc.2
  IL_009f:  brtrue.s   IL_004f
  IL_00a1:  ldc.i4.0
  IL_00a2:  ret
}
")
            End Using
        End Sub

        <Fact>
        Public Sub UpdateIterator_Yield_Add()
            Dim template = "
Imports System.Collections.Generic
Class C
    Shared Function M1() As Integer
        Return 0
    End Function

    Shared Function M2() As Integer
        Return 0
    End Function

    Shared Function M3() As Integer
        Return 0
    End Function

    Shared Function M4() As Integer
        Return 0
    End Function

    Shared Sub [End]()
    End Sub

    Shared Iterator Function F() As IEnumerable(Of Integer)
{0}
    End Function
End Class"

            Dim source0 = MarkedSource(String.Format(template, "
<N:0>Yield M1()</N:0>
<N:1>Yield M2()</N:1>
[End]()
"))
            Dim source1 = MarkedSource(String.Format(template, "
<N:0>Yield M1()</N:0>
<N:2>Yield M3()</N:2>
<N:3>Yield M4()</N:3>
<N:1>Yield M2()</N:1>   
[End]()
"))

            Dim compilation0 = CreateEmptyCompilationWithReferences(source0.Tree, references:=LatestVbReferences, options:=ComSafeDebugDll)
            Dim compilation1 = compilation0.WithSource(source1.Tree)

            Dim f0 = compilation0.GetMember(Of MethodSymbol)("C.F")
            Dim f1 = compilation1.GetMember(Of MethodSymbol)("C.F")

            Dim v0 = CompileAndVerify(compilation:=compilation0).VerifyDiagnostics()
            Using md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)

                Dim generation0 = CreateInitialBaseline(compilation0, md0, Function(handle) v0.CreateSymReader().GetEncMethodDebugInfo(handle))
                Dim diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))))

                v0.VerifyPdb("C.F", "
<symbols>
  <files>
    <file id=""1"" name="""" language=""VB"" />
  </files>
  <methods>
    <method containingType=""C"" name=""F"">
      <customDebugInfo>
        <forwardIterator name=""VB$StateMachine_6_F"" />
        <encStateMachineStateMap>
          <state number=""1"" offset=""0"" />
          <state number=""2"" offset=""23"" />
        </encStateMachineStateMap>
      </customDebugInfo>
    </method>
  </methods>
</symbols>")
                v0.VerifyPdb("C+VB$StateMachine_6_F.MoveNext", "
<symbols>
  <files>
    <file id=""1"" name="""" language=""VB"" />
  </files>
  <methods>
    <method containingType=""C+VB$StateMachine_6_F"" name=""MoveNext"">
      <customDebugInfo>
        <encLocalSlotMap>
          <slot kind=""20"" offset=""-1"" />
          <slot kind=""27"" offset=""-1"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <scope startOffset=""0x0"" endOffset=""0x73"">
        <importsforward declaringType=""C"" methodName=""M1"" />
      </scope>
    </method>
  </methods>
</symbols>", options:=PdbValidationOptions.ExcludeSequencePoints)
                v0.VerifyIL("C.VB$StateMachine_6_F.MoveNext()", "
{
  // Code size      115 (0x73)
  .maxstack  3
  .locals init (Boolean V_0,
                Integer V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.VB$StateMachine_6_F.$State As Integer""
  IL_0006:  stloc.1
  IL_0007:  ldloc.1
  IL_0008:  switch    (
        IL_001b,
        IL_001d,
        IL_001f)
  IL_0019:  br.s       IL_0021
  IL_001b:  br.s       IL_0023
  IL_001d:  br.s       IL_0043
  IL_001f:  br.s       IL_0062

  IL_0021:  ldc.i4.0
  IL_0022:  ret

  IL_0023:  ldarg.0
  IL_0024:  ldc.i4.m1
  IL_0025:  dup
  IL_0026:  stloc.1
  IL_0027:  stfld      ""C.VB$StateMachine_6_F.$State As Integer""
  IL_002c:  nop
  IL_002d:  ldarg.0
  IL_002e:  call       ""Function C.M1() As Integer""
  IL_0033:  stfld      ""C.VB$StateMachine_6_F.$Current As Integer""
  IL_0038:  ldarg.0
  IL_0039:  ldc.i4.1
  IL_003a:  dup
  IL_003b:  stloc.1
  IL_003c:  stfld      ""C.VB$StateMachine_6_F.$State As Integer""
  IL_0041:  ldc.i4.1
  IL_0042:  ret

  IL_0043:  ldarg.0
  IL_0044:  ldc.i4.m1
  IL_0045:  dup
  IL_0046:  stloc.1
  IL_0047:  stfld      ""C.VB$StateMachine_6_F.$State As Integer""
  IL_004c:  ldarg.0
  IL_004d:  call       ""Function C.M2() As Integer""
  IL_0052:  stfld      ""C.VB$StateMachine_6_F.$Current As Integer""
  IL_0057:  ldarg.0
  IL_0058:  ldc.i4.2
  IL_0059:  dup
  IL_005a:  stloc.1
  IL_005b:  stfld      ""C.VB$StateMachine_6_F.$State As Integer""
  IL_0060:  ldc.i4.1
  IL_0061:  ret

  IL_0062:  ldarg.0
  IL_0063:  ldc.i4.m1
  IL_0064:  dup
  IL_0065:  stloc.1
  IL_0066:  stfld      ""C.VB$StateMachine_6_F.$State As Integer""
  IL_006b:  call       ""Sub C.End()""
  IL_0070:  nop
  IL_0071:  ldc.i4.0
  IL_0072:  ret
}")
                diff1.VerifyIL("C.VB$StateMachine_6_F.MoveNext()", "
{
  // Code size      192 (0xc0)
  .maxstack  3
  .locals init (Boolean V_0,
                Integer V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.VB$StateMachine_6_F.$State As Integer""
  IL_0006:  stloc.1
  IL_0007:  ldloc.1
  IL_0008:  switch    (
        IL_0023,
        IL_0025,
        IL_0027,
        IL_002c,
        IL_002e)
  IL_0021:  br.s       IL_0030
  IL_0023:  br.s       IL_0032
  IL_0025:  br.s       IL_0052
  IL_0027:  br         IL_00af
  IL_002c:  br.s       IL_0071
  IL_002e:  br.s       IL_0090

  IL_0030:  ldc.i4.0
  IL_0031:  ret

  IL_0032:  ldarg.0
  IL_0033:  ldc.i4.m1
  IL_0034:  dup
  IL_0035:  stloc.1
  IL_0036:  stfld      ""C.VB$StateMachine_6_F.$State As Integer""
  IL_003b:  nop
  IL_003c:  ldarg.0
  IL_003d:  call       ""Function C.M1() As Integer""
  IL_0042:  stfld      ""C.VB$StateMachine_6_F.$Current As Integer""
  IL_0047:  ldarg.0
  IL_0048:  ldc.i4.1
  IL_0049:  dup
  IL_004a:  stloc.1
  IL_004b:  stfld      ""C.VB$StateMachine_6_F.$State As Integer""
  IL_0050:  ldc.i4.1
  IL_0051:  ret

  IL_0052:  ldarg.0
  IL_0053:  ldc.i4.m1
  IL_0054:  dup
  IL_0055:  stloc.1
  IL_0056:  stfld      ""C.VB$StateMachine_6_F.$State As Integer""
  IL_005b:  ldarg.0
  IL_005c:  call       ""Function C.M3() As Integer""
  IL_0061:  stfld      ""C.VB$StateMachine_6_F.$Current As Integer""
  IL_0066:  ldarg.0
  IL_0067:  ldc.i4.3
  IL_0068:  dup
  IL_0069:  stloc.1
  IL_006a:  stfld      ""C.VB$StateMachine_6_F.$State As Integer""
  IL_006f:  ldc.i4.1
  IL_0070:  ret

  IL_0071:  ldarg.0
  IL_0072:  ldc.i4.m1
  IL_0073:  dup
  IL_0074:  stloc.1
  IL_0075:  stfld      ""C.VB$StateMachine_6_F.$State As Integer""
  IL_007a:  ldarg.0
  IL_007b:  call       ""Function C.M4() As Integer""
  IL_0080:  stfld      ""C.VB$StateMachine_6_F.$Current As Integer""
  IL_0085:  ldarg.0
  IL_0086:  ldc.i4.4
  IL_0087:  dup
  IL_0088:  stloc.1
  IL_0089:  stfld      ""C.VB$StateMachine_6_F.$State As Integer""
  IL_008e:  ldc.i4.1
  IL_008f:  ret

  IL_0090:  ldarg.0
  IL_0091:  ldc.i4.m1
  IL_0092:  dup
  IL_0093:  stloc.1
  IL_0094:  stfld      ""C.VB$StateMachine_6_F.$State As Integer""
  IL_0099:  ldarg.0
  IL_009a:  call       ""Function C.M2() As Integer""
  IL_009f:  stfld      ""C.VB$StateMachine_6_F.$Current As Integer""
  IL_00a4:  ldarg.0
  IL_00a5:  ldc.i4.2
  IL_00a6:  dup
  IL_00a7:  stloc.1
  IL_00a8:  stfld      ""C.VB$StateMachine_6_F.$State As Integer""
  IL_00ad:  ldc.i4.1
  IL_00ae:  ret

  IL_00af:  ldarg.0
  IL_00b0:  ldc.i4.m1
  IL_00b1:  dup
  IL_00b2:  stloc.1
  IL_00b3:  stfld      ""C.VB$StateMachine_6_F.$State As Integer""
  IL_00b8:  call       ""Sub C.End()""
  IL_00bd:  nop
  IL_00be:  ldc.i4.0
  IL_00bf:  ret
}")
            End Using
        End Sub

        <Fact>
        Public Sub UpdateIterator_Yield_Remove()
            Dim template = "
Imports System.Collections.Generic
Class C
    Shared Function M1() As Integer
        Return 0
    End Function

    Shared Function M2() As Integer
        Return 0
    End Function

    Shared Function M3() As Integer
        Return 0
    End Function

    Shared Function M4() As Integer
        Return 0
    End Function

    Shared Sub [End]()
    End Sub

    Shared Iterator Function F() As IEnumerable(Of Integer)
{0}
    End Function
End Class"

            Dim source0 = MarkedSource(String.Format(template, "
<N:0>Yield M1()</N:0>
<N:1>Yield M2()</N:1>
<N:2>Yield M3()</N:2>
<N:3>Yield M4()</N:3>
[End]()
"))
            Dim source1 = MarkedSource(String.Format(template, "
<N:1>Yield M2()</N:1>
<N:2>Yield M3()</N:2>
[End]()
"))

            Dim compilation0 = CreateEmptyCompilationWithReferences(source0.Tree, references:=LatestVbReferences, options:=ComSafeDebugDll)
            Dim compilation1 = compilation0.WithSource(source1.Tree)

            Dim f0 = compilation0.GetMember(Of MethodSymbol)("C.F")
            Dim f1 = compilation1.GetMember(Of MethodSymbol)("C.F")

            Dim v0 = CompileAndVerify(compilation:=compilation0).VerifyDiagnostics()
            Using md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)

                Dim generation0 = CreateInitialBaseline(compilation0, md0, Function(handle) v0.CreateSymReader().GetEncMethodDebugInfo(handle))
                Dim diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))))

                diff1.VerifyIL("C.VB$StateMachine_6_F.MoveNext", "
{
  // Code size      134 (0x86)
  .maxstack  3
  .locals init (Boolean V_0,
                Integer V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.VB$StateMachine_6_F.$State As Integer""
  IL_0006:  stloc.1
  IL_0007:  ldloc.1
  IL_0008:  switch    (
        IL_001f,
        IL_0025,
        IL_0021,
        IL_0023)
  IL_001d:  br.s       IL_0025
  IL_001f:  br.s       IL_0036
  IL_0021:  br.s       IL_0056
  IL_0023:  br.s       IL_0075
  IL_0025:  ldloc.1
  IL_0026:  ldc.i4.1
  IL_0027:  blt.s      IL_0034
  IL_0029:  ldstr      """ & CodeAnalysisResources.EncCannotResumeSuspendedIteratorMethod & """
  IL_002e:  newobj     ""Sub System.InvalidOperationException..ctor(String)""
  IL_0033:  throw

  IL_0034:  ldc.i4.0
  IL_0035:  ret

  IL_0036:  ldarg.0
  IL_0037:  ldc.i4.m1
  IL_0038:  dup
  IL_0039:  stloc.1
  IL_003a:  stfld      ""C.VB$StateMachine_6_F.$State As Integer""
  IL_003f:  nop
  IL_0040:  ldarg.0
  IL_0041:  call       ""Function C.M2() As Integer""
  IL_0046:  stfld      ""C.VB$StateMachine_6_F.$Current As Integer""
  IL_004b:  ldarg.0
  IL_004c:  ldc.i4.2
  IL_004d:  dup
  IL_004e:  stloc.1
  IL_004f:  stfld      ""C.VB$StateMachine_6_F.$State As Integer""
  IL_0054:  ldc.i4.1
  IL_0055:  ret

  IL_0056:  ldarg.0
  IL_0057:  ldc.i4.m1
  IL_0058:  dup
  IL_0059:  stloc.1
  IL_005a:  stfld      ""C.VB$StateMachine_6_F.$State As Integer""
  IL_005f:  ldarg.0
  IL_0060:  call       ""Function C.M3() As Integer""
  IL_0065:  stfld      ""C.VB$StateMachine_6_F.$Current As Integer""
  IL_006a:  ldarg.0
  IL_006b:  ldc.i4.3
  IL_006c:  dup
  IL_006d:  stloc.1
  IL_006e:  stfld      ""C.VB$StateMachine_6_F.$State As Integer""
  IL_0073:  ldc.i4.1
  IL_0074:  ret

  IL_0075:  ldarg.0
  IL_0076:  ldc.i4.m1
  IL_0077:  dup
  IL_0078:  stloc.1
  IL_0079:  stfld      ""C.VB$StateMachine_6_F.$State As Integer""
  IL_007e:  call       ""Sub C.End()""
  IL_0083:  nop
  IL_0084:  ldc.i4.0
  IL_0085:  ret
}")
            End Using
        End Sub

        <Fact>
        Public Sub UpdateIterator_Yield_AddRemove_Lambda()
            Dim source0 = MarkedSource("
Imports System
Imports System.Collections.Generic

Class C
    Shared Function M1() As Integer
        Return 0
    End Function

    Shared Function M2() As Integer
        Return 0
    End Function

    Shared Function M3() As Integer
        Return 0
    End Function

    Shared Sub [End]()
    End Sub

    Shared Function F(t As Func(Of IEnumerable(Of Integer))) As Integer
        Return 1
    End Function

    Dim x As Integer = F(<N:4>Iterator Function()
                                <N:0>Yield M1()</N:0>
                                <N:1>Yield M2()</N:1>
                                [End]()
                              End Function</N:4>)

    Dim y As Integer = F(<N:5>Iterator Function()
                                <N:2>Yield M1()</N:2>
                                <N:3>Yield M2()</N:3>
                                [End]()
                              End Function</N:5>)
End Class")
            Dim source1 = MarkedSource("
Imports System
Imports System.Collections.Generic

Class C
    Shared Function M1() As Integer
        Return 0
    End Function

    Shared Function M2() As Integer
        Return 0
    End Function

    Shared Function M3() As Integer
        Return 0
    End Function

    Shared Sub [End]()
    End Sub

    Shared Function F(t As Func(Of IEnumerable(Of Integer))) As Integer
        Return 1
    End Function

    Dim x As Integer = F(<N:4>Iterator Function()
                                <N:0>Yield M1()</N:0>
                                Yield M3()
                                <N:1>Yield M2()</N:1>
                                [End]()
                              End Function</N:4>)

    Dim y As Integer = F(<N:5>Iterator Function()
                                <N:3>Yield M2()</N:3>
                                [End]()
                              End Function</N:5>)
End Class")
            Dim compilation0 = CreateEmptyCompilationWithReferences({source0.Tree}, references:=LatestVbReferences, options:=ComSafeDebugDll)
            Dim compilation1 = compilation0.WithSource(source1.Tree)

            Dim v0 = CompileAndVerify(compilation0)
            v0.VerifyDiagnostics()
            Dim md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)

            Dim ctor0 = compilation0.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single()
            Dim ctor1 = compilation1.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single()

            Dim generation0 = CreateInitialBaseline(compilation0, md0, AddressOf v0.CreateSymReader().GetEncMethodDebugInfo)

            Dim diff1 = compilation1.EmitDifference(
                 generation0,
                 ImmutableArray.Create(
                     SemanticEdit.Create(SemanticEditKind.Update, ctor0, ctor1, GetSyntaxMapFromMarkers(source0, source1))))

            v0.VerifyPdb("C..ctor",
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <methods>
        <method containingType="C" name=".ctor">
            <customDebugInfo>
                <encLambdaMap>
                    <methodOrdinal>0</methodOrdinal>
                    <lambda offset="-451"/>
                    <lambda offset="-221"/>
                </encLambdaMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" hidden="true" document="1"/>
                <entry offset="0x7" startLine="25" startColumn="9" endLine="29" endColumn="50" document="1"/>
                <entry offset="0x36" startLine="31" startColumn="9" endLine="35" endColumn="50" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x66">
                <namespace name="System" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <currentnamespace name=""/>
            </scope>
        </method>
    </methods>
</symbols>)
            v0.VerifyPdb("C+_Closure$__._Lambda$__0-0",
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <methods>
        <method containingType="C+_Closure$__" name="_Lambda$__0-0">
            <customDebugInfo>
                <forwardIterator name="VB$StateMachine___Lambda$__0-0"/>
                <encStateMachineStateMap>
                    <state number="1" offset="-393"/>
                    <state number="2" offset="-338"/>
                </encStateMachineStateMap>
            </customDebugInfo>
        </method>
    </methods>
</symbols>)

            v0.VerifyPdb("C+_Closure$__._Lambda$__0-1",
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <methods>
        <method containingType="C+_Closure$__" name="_Lambda$__0-1">
            <customDebugInfo>
                <forwardIterator name="VB$StateMachine___Lambda$__0-1"/>
                <encStateMachineStateMap>
                    <state number="1" offset="-163"/>
                    <state number="2" offset="-108"/>
                </encStateMachineStateMap>
            </customDebugInfo>
        </method>
    </methods>
</symbols>)

            v0.VerifyPdb("C+_Closure$__+VB$StateMachine___Lambda$__0-0.MoveNext",
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <methods>
        <method containingType="C+_Closure$__+VB$StateMachine___Lambda$__0-0" name="MoveNext">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="20" offset="-451"/>
                    <slot kind="27" offset="-451"/>
                    <slot kind="21" offset="-451"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <scope startOffset="0x0" endOffset="0x73">
                <importsforward declaringType="C" methodName=".ctor"/>
            </scope>
        </method>
    </methods>
</symbols>, options:=PdbValidationOptions.ExcludeSequencePoints)

            v0.VerifyPdb("C+_Closure$__+VB$StateMachine___Lambda$__0-1.MoveNext",
 <symbols>
     <files>
         <file id="1" name="" language="VB"/>
     </files>
     <methods>
         <method containingType="C+_Closure$__+VB$StateMachine___Lambda$__0-1" name="MoveNext">
             <customDebugInfo>
                 <encLocalSlotMap>
                     <slot kind="20" offset="-221"/>
                     <slot kind="27" offset="-221"/>
                     <slot kind="21" offset="-221"/>
                 </encLocalSlotMap>
             </customDebugInfo>
             <scope startOffset="0x0" endOffset="0x73">
                 <importsforward declaringType="C" methodName=".ctor"/>
             </scope>
         </method>
     </methods>
 </symbols>, options:=PdbValidationOptions.ExcludeSequencePoints)

            diff1.VerifyIL("C._Closure$__.VB$StateMachine___Lambda$__0-0.MoveNext", "
{
  // Code size      152 (0x98)
  .maxstack  3
  .locals init (Boolean V_0,
                Integer V_1,
                System.Collections.Generic.IEnumerable(Of Integer) V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C._Closure$__.VB$StateMachine___Lambda$__0-0.$State As Integer""
  IL_0006:  stloc.1
  IL_0007:  ldloc.1
  IL_0008:  switch    (
        IL_001f,
        IL_0021,
        IL_0023,
        IL_0025)
  IL_001d:  br.s       IL_0027
  IL_001f:  br.s       IL_0029
  IL_0021:  br.s       IL_0049
  IL_0023:  br.s       IL_0087
  IL_0025:  br.s       IL_0068
  IL_0027:  ldc.i4.0
  IL_0028:  ret
  IL_0029:  ldarg.0
  IL_002a:  ldc.i4.m1
  IL_002b:  dup
  IL_002c:  stloc.1
  IL_002d:  stfld      ""C._Closure$__.VB$StateMachine___Lambda$__0-0.$State As Integer""
  IL_0032:  nop
  IL_0033:  ldarg.0
  IL_0034:  call       ""Function C.M1() As Integer""
  IL_0039:  stfld      ""C._Closure$__.VB$StateMachine___Lambda$__0-0.$Current As Integer""
  IL_003e:  ldarg.0
  IL_003f:  ldc.i4.1
  IL_0040:  dup
  IL_0041:  stloc.1
  IL_0042:  stfld      ""C._Closure$__.VB$StateMachine___Lambda$__0-0.$State As Integer""
  IL_0047:  ldc.i4.1
  IL_0048:  ret
  IL_0049:  ldarg.0
  IL_004a:  ldc.i4.m1
  IL_004b:  dup
  IL_004c:  stloc.1
  IL_004d:  stfld      ""C._Closure$__.VB$StateMachine___Lambda$__0-0.$State As Integer""
  IL_0052:  ldarg.0
  IL_0053:  call       ""Function C.M3() As Integer""
  IL_0058:  stfld      ""C._Closure$__.VB$StateMachine___Lambda$__0-0.$Current As Integer""
  IL_005d:  ldarg.0
  IL_005e:  ldc.i4.3
  IL_005f:  dup
  IL_0060:  stloc.1
  IL_0061:  stfld      ""C._Closure$__.VB$StateMachine___Lambda$__0-0.$State As Integer""
  IL_0066:  ldc.i4.1
  IL_0067:  ret
  IL_0068:  ldarg.0
  IL_0069:  ldc.i4.m1
  IL_006a:  dup
  IL_006b:  stloc.1
  IL_006c:  stfld      ""C._Closure$__.VB$StateMachine___Lambda$__0-0.$State As Integer""
  IL_0071:  ldarg.0
  IL_0072:  call       ""Function C.M2() As Integer""
  IL_0077:  stfld      ""C._Closure$__.VB$StateMachine___Lambda$__0-0.$Current As Integer""
  IL_007c:  ldarg.0
  IL_007d:  ldc.i4.2
  IL_007e:  dup
  IL_007f:  stloc.1
  IL_0080:  stfld      ""C._Closure$__.VB$StateMachine___Lambda$__0-0.$State As Integer""
  IL_0085:  ldc.i4.1
  IL_0086:  ret
  IL_0087:  ldarg.0
  IL_0088:  ldc.i4.m1
  IL_0089:  dup
  IL_008a:  stloc.1
  IL_008b:  stfld      ""C._Closure$__.VB$StateMachine___Lambda$__0-0.$State As Integer""
  IL_0090:  call       ""Sub C.End()""
  IL_0095:  nop
  IL_0096:  ldc.i4.0
  IL_0097:  ret
}")
            diff1.VerifyIL("C._Closure$__.VB$StateMachine___Lambda$__0-1.MoveNext", "
{
  // Code size       88 (0x58)
  .maxstack  3
  .locals init (Boolean V_0,
                Integer V_1,
                System.Collections.Generic.IEnumerable(Of Integer) V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C._Closure$__.VB$StateMachine___Lambda$__0-1.$State As Integer""
  IL_0006:  stloc.1
  IL_0007:  ldloc.1
  IL_0008:  brfalse.s  IL_0012
  IL_000a:  br.s       IL_000c
  IL_000c:  ldloc.1
  IL_000d:  ldc.i4.2
  IL_000e:  beq.s      IL_0014
  IL_0010:  br.s       IL_0016
  IL_0012:  br.s       IL_0027
  IL_0014:  br.s       IL_0047
  IL_0016:  ldloc.1
  IL_0017:  ldc.i4.1
  IL_0018:  blt.s      IL_0025
  IL_001a:  ldstr      """ & CodeAnalysisResources.EncCannotResumeSuspendedIteratorMethod & """
  IL_001f:  newobj     ""Sub System.InvalidOperationException..ctor(String)""
  IL_0024:  throw
  IL_0025:  ldc.i4.0
  IL_0026:  ret
  IL_0027:  ldarg.0
  IL_0028:  ldc.i4.m1
  IL_0029:  dup
  IL_002a:  stloc.1
  IL_002b:  stfld      ""C._Closure$__.VB$StateMachine___Lambda$__0-1.$State As Integer""
  IL_0030:  nop
  IL_0031:  ldarg.0
  IL_0032:  call       ""Function C.M2() As Integer""
  IL_0037:  stfld      ""C._Closure$__.VB$StateMachine___Lambda$__0-1.$Current As Integer""
  IL_003c:  ldarg.0
  IL_003d:  ldc.i4.2
  IL_003e:  dup
  IL_003f:  stloc.1
  IL_0040:  stfld      ""C._Closure$__.VB$StateMachine___Lambda$__0-1.$State As Integer""
  IL_0045:  ldc.i4.1
  IL_0046:  ret
  IL_0047:  ldarg.0
  IL_0048:  ldc.i4.m1
  IL_0049:  dup
  IL_004a:  stloc.1
  IL_004b:  stfld      ""C._Closure$__.VB$StateMachine___Lambda$__0-1.$State As Integer""
  IL_0050:  call       ""Sub C.End()""
  IL_0055:  nop
  IL_0056:  ldc.i4.0
  IL_0057:  ret
}")
        End Sub

        <Fact>
        Public Sub UpdateIterator_Yield_Add_Finally_Try()
            Dim template = "
Imports System.Collections.Generic
Class C
    Shared Function M1() As Integer
        Return 0
    End Function

    Shared Function M2() As Integer
        Return 0
    End Function

    Shared Function M3() As Integer
        Return 0
    End Function

    Shared Function M4() As Integer
        Return 0
    End Function

    Shared Sub [End]()
    End Sub

    Shared Sub Finally1(gen As Integer)
    End Sub

    Shared Sub Finally2(gen As Integer)
    End Sub

    Shared Sub Finally3(gen As Integer)
    End Sub

    Shared Iterator Function F() As IEnumerable(Of Integer)
{0}
    End Function
End Class"

            Dim source0 = MarkedSource(String.Format(template, "
<N:0>Yield M1()</N:0>

<N:3>Try
    <N:1>Yield M2()</N:1>
Finally
    Finally1(0)
End Try</N:3>

<N:2>Yield M3()</N:2>

[End]()
"))
            Dim source1 = MarkedSource(String.Format(template, "
Try
    <N:0>Yield M1()</N:0>
Finally
    Finally2(1)
End Try

<N:3>Try
    <N:1>Yield M2()</N:1>
    Try
        Yield M4()
    Finally 
        Finally3(1)
    End Try
Finally
    Finally1(1)
End Try</N:3>

<N:2>Yield M3()</N:2>

[End]()
"))

            Dim compilation0 = CreateEmptyCompilationWithReferences(source0.Tree, references:=LatestVbReferences, options:=ComSafeDebugDll)
            Dim compilation1 = compilation0.WithSource(source1.Tree)

            Dim f0 = compilation0.GetMember(Of MethodSymbol)("C.F")
            Dim f1 = compilation1.GetMember(Of MethodSymbol)("C.F")

            Dim v0 = CompileAndVerify(compilation:=compilation0).VerifyDiagnostics()
            Using md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)

                Dim generation0 = CreateInitialBaseline(compilation0, md0, Function(handle) v0.CreateSymReader().GetEncMethodDebugInfo(handle))
                Dim diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))))

                v0.VerifyPdb("C.F", "
<symbols>
  <files>
    <file id=""1"" name="""" language=""VB"" />
  </files>
  <methods>
    <method containingType=""C"" name=""F"">
      <customDebugInfo>
        <forwardIterator name=""VB$StateMachine_9_F"" />
        <encStateMachineStateMap>
          <state number=""1"" offset=""0"" />
          <state number=""-3"" offset=""25"" />
          <state number=""2"" offset=""39"" />
          <state number=""3"" offset=""105"" />
        </encStateMachineStateMap>
      </customDebugInfo>
    </method>
  </methods>
</symbols>")
                diff1.VerifySynthesizedMembers(
                    "C.VB$StateMachine_9_F: {$State, $Current, $InitialThreadId, Dispose, MoveNext, GetEnumerator, IEnumerable.GetEnumerator, get_Current, Reset, IEnumerator.get_Current, Current, IEnumerator.Current}",
                    "C: {VB$StateMachine_9_F}")

                v0.VerifyIL("C.VB$StateMachine_9_F.Dispose", "
{
  // Code size       38 (0x26)
  .maxstack  2
  .locals init (Integer V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.VB$StateMachine_9_F.$State As Integer""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  ldc.i4.2
  IL_0009:  beq.s      IL_000d
  IL_000b:  br.s       IL_0017
  IL_000d:  ldarg.0
  IL_000e:  ldc.i4.s   -3
  IL_0010:  stfld      ""C.VB$StateMachine_9_F.$State As Integer""
  IL_0015:  br.s       IL_001e
  IL_0017:  ldarg.0
  IL_0018:  ldc.i4.m1
  IL_0019:  stfld      ""C.VB$StateMachine_9_F.$State As Integer""
  IL_001e:  ldarg.0
  IL_001f:  call       ""Function C.VB$StateMachine_9_F.MoveNext() As Boolean""
  IL_0024:  pop
  IL_0025:  ret
}")
                diff1.VerifyIL("C.VB$StateMachine_9_F.Dispose", "
{
  // Code size       78 (0x4e)
  .maxstack  2
  .locals init (Integer V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.VB$StateMachine_9_F.$State As Integer""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  ldc.i4.1
  IL_0009:  sub
  IL_000a:  switch    (
        IL_0021,
        IL_002b,
        IL_003f,
        IL_0035)
  IL_001f:  br.s       IL_003f
  IL_0021:  ldarg.0
  IL_0022:  ldc.i4.s   -4
  IL_0024:  stfld      ""C.VB$StateMachine_9_F.$State As Integer""
  IL_0029:  br.s       IL_0046
  IL_002b:  ldarg.0
  IL_002c:  ldc.i4.s   -3
  IL_002e:  stfld      ""C.VB$StateMachine_9_F.$State As Integer""
  IL_0033:  br.s       IL_0046
  IL_0035:  ldarg.0
  IL_0036:  ldc.i4.s   -5
  IL_0038:  stfld      ""C.VB$StateMachine_9_F.$State As Integer""
  IL_003d:  br.s       IL_0046
  IL_003f:  ldarg.0
  IL_0040:  ldc.i4.m1
  IL_0041:  stfld      ""C.VB$StateMachine_9_F.$State As Integer""
  IL_0046:  ldarg.0
  IL_0047:  call       ""Function C.VB$StateMachine_9_F.MoveNext() As Boolean""
  IL_004c:  pop
  IL_004d:  ret
}")
                v0.VerifyIL("C.VB$StateMachine_9_F.MoveNext", "
  {
  // Code size      226 (0xe2)
  .maxstack  3
  .locals init (Boolean V_0,
                Integer V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.VB$StateMachine_9_F.$State As Integer""
  IL_0006:  stloc.1
  IL_0007:  ldloc.1
  IL_0008:  ldc.i4.s   -3
  IL_000a:  sub
  IL_000b:  switch    (
        IL_002e,
        IL_0039,
        IL_0039,
        IL_0030,
        IL_0032,
        IL_002e,
        IL_0034)
  IL_002c:  br.s       IL_0039
  IL_002e:  br.s       IL_0064
  IL_0030:  br.s       IL_003b
  IL_0032:  br.s       IL_005b
  IL_0034:  br         IL_00cf
  IL_0039:  ldc.i4.0
  IL_003a:  ret
  IL_003b:  ldarg.0
  IL_003c:  ldc.i4.m1
  IL_003d:  dup
  IL_003e:  stloc.1
  IL_003f:  stfld      ""C.VB$StateMachine_9_F.$State As Integer""
  IL_0044:  nop
  IL_0045:  ldarg.0
  IL_0046:  call       ""Function C.M1() As Integer""
  IL_004b:  stfld      ""C.VB$StateMachine_9_F.$Current As Integer""
  IL_0050:  ldarg.0
  IL_0051:  ldc.i4.1
  IL_0052:  dup
  IL_0053:  stloc.1
  IL_0054:  stfld      ""C.VB$StateMachine_9_F.$State As Integer""
  IL_0059:  ldc.i4.1
  IL_005a:  ret
  IL_005b:  ldarg.0
  IL_005c:  ldc.i4.m1
  IL_005d:  dup
  IL_005e:  stloc.1
  IL_005f:  stfld      ""C.VB$StateMachine_9_F.$State As Integer""
  IL_0064:  nop
  .try
  {
    IL_0065:  ldloc.1
    IL_0066:  ldc.i4.s   -3
    IL_0068:  beq.s      IL_0072
    IL_006a:  br.s       IL_006c
    IL_006c:  ldloc.1
    IL_006d:  ldc.i4.2
    IL_006e:  beq.s      IL_0074
    IL_0070:  br.s       IL_0076
    IL_0072:  br.s       IL_0078
    IL_0074:  br.s       IL_009e
    IL_0076:  br.s       IL_0085
    IL_0078:  ldarg.0
    IL_0079:  ldc.i4.m1
    IL_007a:  dup
    IL_007b:  stloc.1
    IL_007c:  stfld      ""C.VB$StateMachine_9_F.$State As Integer""
    IL_0081:  ldc.i4.1
    IL_0082:  stloc.0
    IL_0083:  leave.s    IL_00e0
    IL_0085:  nop
    IL_0086:  ldarg.0
    IL_0087:  call       ""Function C.M2() As Integer""
    IL_008c:  stfld      ""C.VB$StateMachine_9_F.$Current As Integer""
    IL_0091:  ldarg.0
    IL_0092:  ldc.i4.2
    IL_0093:  dup
    IL_0094:  stloc.1
    IL_0095:  stfld      ""C.VB$StateMachine_9_F.$State As Integer""
    IL_009a:  ldc.i4.1
    IL_009b:  stloc.0
    IL_009c:  leave.s    IL_00e0
    IL_009e:  ldarg.0
    IL_009f:  ldc.i4.m1
    IL_00a0:  dup
    IL_00a1:  stloc.1
    IL_00a2:  stfld      ""C.VB$StateMachine_9_F.$State As Integer""
    IL_00a7:  leave.s    IL_00b8
  }
  finally
  {
    IL_00a9:  ldloc.1
    IL_00aa:  ldc.i4.0
    IL_00ab:  bge.s      IL_00b7
    IL_00ad:  nop
    IL_00ae:  ldc.i4.0
    IL_00af:  call       ""Sub C.Finally1(Integer)""
    IL_00b4:  nop
    IL_00b5:  br.s       IL_00b7
    IL_00b7:  endfinally
  }
  IL_00b8:  nop
  IL_00b9:  ldarg.0
  IL_00ba:  call       ""Function C.M3() As Integer""
  IL_00bf:  stfld      ""C.VB$StateMachine_9_F.$Current As Integer""
  IL_00c4:  ldarg.0
  IL_00c5:  ldc.i4.3
  IL_00c6:  dup
  IL_00c7:  stloc.1
  IL_00c8:  stfld      ""C.VB$StateMachine_9_F.$State As Integer""
  IL_00cd:  ldc.i4.1
  IL_00ce:  ret
  IL_00cf:  ldarg.0
  IL_00d0:  ldc.i4.m1
  IL_00d1:  dup
  IL_00d2:  stloc.1
  IL_00d3:  stfld      ""C.VB$StateMachine_9_F.$State As Integer""
  IL_00d8:  call       ""Sub C.End()""
  IL_00dd:  nop
  IL_00de:  ldc.i4.0
  IL_00df:  ret
  IL_00e0:  ldloc.0
  IL_00e1:  ret
}")
                diff1.VerifyIL("C.VB$StateMachine_9_F.MoveNext", "
{
  // Code size      409 (0x199)
  .maxstack  3
  .locals init (Boolean V_0,
                Integer V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.VB$StateMachine_9_F.$State As Integer""
  IL_0006:  stloc.1
  IL_0007:  ldloc.1
  IL_0008:  ldc.i4.s   -5
  IL_000a:  sub
  IL_000b:  switch    (
        IL_003a,
        IL_003c,
        IL_003a,
        IL_0045,
        IL_0045,
        IL_003e,
        IL_003c,
        IL_003a,
        IL_0040,
        IL_003a)
  IL_0038:  br.s       IL_0045
  IL_003a:  br.s       IL_00ac
  IL_003c:  br.s       IL_0051
  IL_003e:  br.s       IL_0047
  IL_0040:  br         IL_0186
  IL_0045:  ldc.i4.0
  IL_0046:  ret
  IL_0047:  ldarg.0
  IL_0048:  ldc.i4.m1
  IL_0049:  dup
  IL_004a:  stloc.1
  IL_004b:  stfld      ""C.VB$StateMachine_9_F.$State As Integer""
  IL_0050:  nop
  IL_0051:  nop
  .try
  {
    IL_0052:  ldloc.1
    IL_0053:  ldc.i4.s   -4
    IL_0055:  beq.s      IL_005f
    IL_0057:  br.s       IL_0059
    IL_0059:  ldloc.1
    IL_005a:  ldc.i4.1
    IL_005b:  beq.s      IL_0061
    IL_005d:  br.s       IL_0063
    IL_005f:  br.s       IL_0065
    IL_0061:  br.s       IL_0091
    IL_0063:  br.s       IL_0075
    IL_0065:  ldarg.0
    IL_0066:  ldc.i4.m1
    IL_0067:  dup
    IL_0068:  stloc.1
    IL_0069:  stfld      ""C.VB$StateMachine_9_F.$State As Integer""
    IL_006e:  ldc.i4.1
    IL_006f:  stloc.0
    IL_0070:  leave      IL_0197
    IL_0075:  nop
    IL_0076:  ldarg.0
    IL_0077:  call       ""Function C.M1() As Integer""
    IL_007c:  stfld      ""C.VB$StateMachine_9_F.$Current As Integer""
    IL_0081:  ldarg.0
    IL_0082:  ldc.i4.1
    IL_0083:  dup
    IL_0084:  stloc.1
    IL_0085:  stfld      ""C.VB$StateMachine_9_F.$State As Integer""
    IL_008a:  ldc.i4.1
    IL_008b:  stloc.0
    IL_008c:  leave      IL_0197
    IL_0091:  ldarg.0
    IL_0092:  ldc.i4.m1
    IL_0093:  dup
    IL_0094:  stloc.1
    IL_0095:  stfld      ""C.VB$StateMachine_9_F.$State As Integer""
    IL_009a:  leave.s    IL_00ab
  }
  finally
  {
    IL_009c:  ldloc.1
    IL_009d:  ldc.i4.0
    IL_009e:  bge.s      IL_00aa
    IL_00a0:  nop
    IL_00a1:  ldc.i4.1
    IL_00a2:  call       ""Sub C.Finally2(Integer)""
    IL_00a7:  nop
    IL_00a8:  br.s       IL_00aa
    IL_00aa:  endfinally
  }
  IL_00ab:  nop
  IL_00ac:  nop
  .try
  {
    IL_00ad:  ldloc.1
    IL_00ae:  ldc.i4.s   -3
    IL_00b0:  bgt.s      IL_00c0
    IL_00b2:  ldloc.1
    IL_00b3:  ldc.i4.s   -5
    IL_00b5:  beq.s      IL_00cc
    IL_00b7:  br.s       IL_00b9
    IL_00b9:  ldloc.1
    IL_00ba:  ldc.i4.s   -3
    IL_00bc:  beq.s      IL_00ce
    IL_00be:  br.s       IL_00d2
    IL_00c0:  ldloc.1
    IL_00c1:  ldc.i4.2
    IL_00c2:  beq.s      IL_00d0
    IL_00c4:  br.s       IL_00c6
    IL_00c6:  ldloc.1
    IL_00c7:  ldc.i4.4
    IL_00c8:  beq.s      IL_00cc
    IL_00ca:  br.s       IL_00d2
    IL_00cc:  br.s       IL_0109
    IL_00ce:  br.s       IL_00d4
    IL_00d0:  br.s       IL_0100
    IL_00d2:  br.s       IL_00e4
    IL_00d4:  ldarg.0
    IL_00d5:  ldc.i4.m1
    IL_00d6:  dup
    IL_00d7:  stloc.1
    IL_00d8:  stfld      ""C.VB$StateMachine_9_F.$State As Integer""
    IL_00dd:  ldc.i4.1
    IL_00de:  stloc.0
    IL_00df:  leave      IL_0197
    IL_00e4:  nop
    IL_00e5:  ldarg.0
    IL_00e6:  call       ""Function C.M2() As Integer""
    IL_00eb:  stfld      ""C.VB$StateMachine_9_F.$Current As Integer""
    IL_00f0:  ldarg.0
    IL_00f1:  ldc.i4.2
    IL_00f2:  dup
    IL_00f3:  stloc.1
    IL_00f4:  stfld      ""C.VB$StateMachine_9_F.$State As Integer""
    IL_00f9:  ldc.i4.1
    IL_00fa:  stloc.0
    IL_00fb:  leave      IL_0197
    IL_0100:  ldarg.0
    IL_0101:  ldc.i4.m1
    IL_0102:  dup
    IL_0103:  stloc.1
    IL_0104:  stfld      ""C.VB$StateMachine_9_F.$State As Integer""
    IL_0109:  nop
    .try
    {
      IL_010a:  ldloc.1
      IL_010b:  ldc.i4.s   -5
      IL_010d:  beq.s      IL_0117
      IL_010f:  br.s       IL_0111
      IL_0111:  ldloc.1
      IL_0112:  ldc.i4.4
      IL_0113:  beq.s      IL_0119
      IL_0115:  br.s       IL_011b
      IL_0117:  br.s       IL_011d
      IL_0119:  br.s       IL_0143
      IL_011b:  br.s       IL_012a
      IL_011d:  ldarg.0
      IL_011e:  ldc.i4.m1
      IL_011f:  dup
      IL_0120:  stloc.1
      IL_0121:  stfld      ""C.VB$StateMachine_9_F.$State As Integer""
      IL_0126:  ldc.i4.1
      IL_0127:  stloc.0
      IL_0128:  leave.s    IL_0197
      IL_012a:  nop
      IL_012b:  ldarg.0
      IL_012c:  call       ""Function C.M4() As Integer""
      IL_0131:  stfld      ""C.VB$StateMachine_9_F.$Current As Integer""
      IL_0136:  ldarg.0
      IL_0137:  ldc.i4.4
      IL_0138:  dup
      IL_0139:  stloc.1
      IL_013a:  stfld      ""C.VB$StateMachine_9_F.$State As Integer""
      IL_013f:  ldc.i4.1
      IL_0140:  stloc.0
      IL_0141:  leave.s    IL_0197
      IL_0143:  ldarg.0
      IL_0144:  ldc.i4.m1
      IL_0145:  dup
      IL_0146:  stloc.1
      IL_0147:  stfld      ""C.VB$StateMachine_9_F.$State As Integer""
      IL_014c:  leave.s    IL_015d
    }
    finally
    {
      IL_014e:  ldloc.1
      IL_014f:  ldc.i4.0
      IL_0150:  bge.s      IL_015c
      IL_0152:  nop
      IL_0153:  ldc.i4.1
      IL_0154:  call       ""Sub C.Finally3(Integer)""
      IL_0159:  nop
      IL_015a:  br.s       IL_015c
      IL_015c:  endfinally
    }
    IL_015d:  nop
    IL_015e:  leave.s    IL_016f
  }
  finally
  {
    IL_0160:  ldloc.1
    IL_0161:  ldc.i4.0
    IL_0162:  bge.s      IL_016e
    IL_0164:  nop
    IL_0165:  ldc.i4.1
    IL_0166:  call       ""Sub C.Finally1(Integer)""
    IL_016b:  nop
    IL_016c:  br.s       IL_016e
    IL_016e:  endfinally
  }
  IL_016f:  nop
  IL_0170:  ldarg.0
  IL_0171:  call       ""Function C.M3() As Integer""
  IL_0176:  stfld      ""C.VB$StateMachine_9_F.$Current As Integer""
  IL_017b:  ldarg.0
  IL_017c:  ldc.i4.3
  IL_017d:  dup
  IL_017e:  stloc.1
  IL_017f:  stfld      ""C.VB$StateMachine_9_F.$State As Integer""
  IL_0184:  ldc.i4.1
  IL_0185:  ret
  IL_0186:  ldarg.0
  IL_0187:  ldc.i4.m1
  IL_0188:  dup
  IL_0189:  stloc.1
  IL_018a:  stfld      ""C.VB$StateMachine_9_F.$State As Integer""
  IL_018f:  call       ""Sub C.End()""
  IL_0194:  nop
  IL_0195:  ldc.i4.0
  IL_0196:  ret
  IL_0197:  ldloc.0
  IL_0198:  ret
}")
            End Using
        End Sub

        <Fact>
        Public Sub UpdateIterator_Yield_Add_Finally_UsingDeclarationWithVariables()
            Dim template = "
Imports System
Imports System.Collections.Generic
Class C
    Implements IDisposable

    Public Sub Dispose() Implements IDisposable.Dispose

    End Sub
    Shared Function D() As IDisposable
        Return Nothing
    End Function

    Shared Function M2() As Integer
        Return 0
    End Function

    Shared Function M3() As Integer
        Return 0
    End Function

    Shared Sub [End]()
    End Sub

    Shared Iterator Function F() As IEnumerable(Of Integer)
{0}
    End Function
End Class"

            Dim source0 = MarkedSource(String.Format(template, "
Using <N:0>x</N:0> = D(), <N:1>y</N:1> As New C
    <N:2>Yield M2()</N:2>
End Using
[End]()
"))
            Dim source1 = MarkedSource(String.Format(template, "
Using <N:0>x</N:0> = D(), <N:1>y</N:1> As New C
    <N:2>Yield M2()</N:2>
    Yield M3()
End Using
[End]()
"))

            Dim compilation0 = CreateEmptyCompilationWithReferences(source0.Tree, references:=LatestVbReferences, options:=ComSafeDebugDll)
            Dim compilation1 = compilation0.WithSource(source1.Tree)

            Dim f0 = compilation0.GetMember(Of MethodSymbol)("C.F")
            Dim f1 = compilation1.GetMember(Of MethodSymbol)("C.F")

            Dim v0 = CompileAndVerify(compilation:=compilation0).VerifyDiagnostics()
            Using md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)

                Dim generation0 = CreateInitialBaseline(compilation0, md0, Function(handle) v0.CreateSymReader().GetEncMethodDebugInfo(handle))
                Dim diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))))

                v0.VerifyPdb("C.F", "
<symbols>
  <files>
    <file id=""1"" name="""" language=""VB"" />
  </files>
  <methods>
    <method containingType=""C"" name=""F"">
      <customDebugInfo>
        <forwardIterator name=""VB$StateMachine_6_F"" />
        <encLocalSlotMap>
          <slot kind=""0"" offset=""11"" />
          <slot kind=""0"" offset=""31"" />
        </encLocalSlotMap>
        <encStateMachineStateMap>
          <state number=""-3"" offset=""31"" />
          <state number=""1"" offset=""58"" />
        </encStateMachineStateMap>
      </customDebugInfo>
    </method>
  </methods>
</symbols>")
                diff1.VerifySynthesizedMembers(
                    "C.VB$StateMachine_6_F: {$State, $Current, $InitialThreadId, $VB$ResumableLocal_x$0, $VB$ResumableLocal_y$1, Dispose, MoveNext, GetEnumerator, IEnumerable.GetEnumerator, get_Current, Reset, IEnumerator.get_Current, Current, IEnumerator.Current}",
                    "C: {VB$StateMachine_6_F}")

                v0.VerifyIL("C.VB$StateMachine_6_F.Dispose", "
  {
  // Code size       38 (0x26)
  .maxstack  2
  .locals init (Integer V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.VB$StateMachine_6_F.$State As Integer""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  ldc.i4.1
  IL_0009:  beq.s      IL_000d
  IL_000b:  br.s       IL_0017

  IL_000d:  ldarg.0
  IL_000e:  ldc.i4.s   -3
  IL_0010:  stfld      ""C.VB$StateMachine_6_F.$State As Integer""
  IL_0015:  br.s       IL_001e

  IL_0017:  ldarg.0
  IL_0018:  ldc.i4.m1
  IL_0019:  stfld      ""C.VB$StateMachine_6_F.$State As Integer""

  IL_001e:  ldarg.0
  IL_001f:  call       ""Function C.VB$StateMachine_6_F.MoveNext() As Boolean""
  IL_0024:  pop
  IL_0025:  ret
}")
                diff1.VerifyIL("C.VB$StateMachine_6_F.Dispose", "
{
  // Code size       40 (0x28)
  .maxstack  2
  .locals init (Integer V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.VB$StateMachine_6_F.$State As Integer""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  ldc.i4.1
  IL_0009:  sub
  IL_000a:  ldc.i4.1
  IL_000b:  ble.un.s   IL_000f
  IL_000d:  br.s       IL_0019

  IL_000f:  ldarg.0
  IL_0010:  ldc.i4.s   -3
  IL_0012:  stfld      ""C.VB$StateMachine_6_F.$State As Integer""
  IL_0017:  br.s       IL_0020

  IL_0019:  ldarg.0
  IL_001a:  ldc.i4.m1
  IL_001b:  stfld      ""C.VB$StateMachine_6_F.$State As Integer""

  IL_0020:  ldarg.0
  IL_0021:  call       ""Function C.VB$StateMachine_6_F.MoveNext() As Boolean""
  IL_0026:  pop
  IL_0027:  ret
}")
                v0.VerifyIL("C.VB$StateMachine_6_F.MoveNext", "
{
  // Code size      229 (0xe5)
  .maxstack  3
  .locals init (Boolean V_0,
                Integer V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.VB$StateMachine_6_F.$State As Integer""
  IL_0006:  stloc.1
  IL_0007:  ldloc.1
  IL_0008:  ldc.i4.s   -3
  IL_000a:  sub
  IL_000b:  switch    (
        IL_0026,
        IL_002a,
        IL_002a,
        IL_0028,
        IL_0026)
  IL_0024:  br.s       IL_002a
  IL_0026:  br.s       IL_0042
  IL_0028:  br.s       IL_002c

  IL_002a:  ldc.i4.0
  IL_002b:  ret

  IL_002c:  ldarg.0
  IL_002d:  ldc.i4.m1
  IL_002e:  dup
  IL_002f:  stloc.1
  IL_0030:  stfld      ""C.VB$StateMachine_6_F.$State As Integer""
  IL_0035:  nop
  IL_0036:  nop
  IL_0037:  ldarg.0
  IL_0038:  call       ""Function C.D() As System.IDisposable""
  IL_003d:  stfld      ""C.VB$StateMachine_6_F.$VB$ResumableLocal_x$0 As System.IDisposable""

  IL_0042:  nop
  .try
  {
    IL_0043:  ldloc.1
    IL_0044:  ldc.i4.s   -3
    IL_0046:  beq.s      IL_0050
    IL_0048:  br.s       IL_004a

    IL_004a:  ldloc.1
    IL_004b:  ldc.i4.1
    IL_004c:  beq.s      IL_0050
    IL_004e:  br.s       IL_0052
    IL_0050:  br.s       IL_005d

    IL_0052:  ldarg.0
    IL_0053:  newobj     ""Sub C..ctor()""
    IL_0058:  stfld      ""C.VB$StateMachine_6_F.$VB$ResumableLocal_y$1 As C""

    IL_005d:  nop
    .try
    {
      IL_005e:  ldloc.1
      IL_005f:  ldc.i4.s   -3
      IL_0061:  beq.s      IL_006b
      IL_0063:  br.s       IL_0065
      IL_0065:  ldloc.1
      IL_0066:  ldc.i4.1
      IL_0067:  beq.s      IL_006d
      IL_0069:  br.s       IL_006f
      IL_006b:  br.s       IL_0071
      IL_006d:  br.s       IL_0096
      IL_006f:  br.s       IL_007e

      IL_0071:  ldarg.0
      IL_0072:  ldc.i4.m1
      IL_0073:  dup
      IL_0074:  stloc.1
      IL_0075:  stfld      ""C.VB$StateMachine_6_F.$State As Integer""
      IL_007a:  ldc.i4.1
      IL_007b:  stloc.0
      IL_007c:  leave.s    IL_00e3

      IL_007e:  ldarg.0
      IL_007f:  call       ""Function C.M2() As Integer""
      IL_0084:  stfld      ""C.VB$StateMachine_6_F.$Current As Integer""
      IL_0089:  ldarg.0
      IL_008a:  ldc.i4.1
      IL_008b:  dup
      IL_008c:  stloc.1
      IL_008d:  stfld      ""C.VB$StateMachine_6_F.$State As Integer""
      IL_0092:  ldc.i4.1
      IL_0093:  stloc.0
      IL_0094:  leave.s    IL_00e3

      IL_0096:  ldarg.0
      IL_0097:  ldc.i4.m1
      IL_0098:  dup
      IL_0099:  stloc.1
      IL_009a:  stfld      ""C.VB$StateMachine_6_F.$State As Integer""
      IL_009f:  leave.s    IL_00bd
    }
    finally
    {
      IL_00a1:  ldloc.1
      IL_00a2:  ldc.i4.0
      IL_00a3:  bge.s      IL_00bc
      IL_00a5:  nop
      IL_00a6:  ldarg.0
      IL_00a7:  ldfld      ""C.VB$StateMachine_6_F.$VB$ResumableLocal_y$1 As C""
      IL_00ac:  brfalse.s  IL_00ba
      IL_00ae:  ldarg.0
      IL_00af:  ldfld      ""C.VB$StateMachine_6_F.$VB$ResumableLocal_y$1 As C""
      IL_00b4:  callvirt   ""Sub System.IDisposable.Dispose()""
      IL_00b9:  nop
      IL_00ba:  br.s       IL_00bc
      IL_00bc:  endfinally
    }
    IL_00bd:  leave.s    IL_00db
  }
  finally
  {
    IL_00bf:  ldloc.1
    IL_00c0:  ldc.i4.0
    IL_00c1:  bge.s      IL_00da
    IL_00c3:  nop
    IL_00c4:  ldarg.0
    IL_00c5:  ldfld      ""C.VB$StateMachine_6_F.$VB$ResumableLocal_x$0 As System.IDisposable""
    IL_00ca:  brfalse.s  IL_00d8
    IL_00cc:  ldarg.0
    IL_00cd:  ldfld      ""C.VB$StateMachine_6_F.$VB$ResumableLocal_x$0 As System.IDisposable""
    IL_00d2:  callvirt   ""Sub System.IDisposable.Dispose()""
    IL_00d7:  nop
    IL_00d8:  br.s       IL_00da
    IL_00da:  endfinally
  }
  IL_00db:  call       ""Sub C.End()""
  IL_00e0:  nop
  IL_00e1:  ldc.i4.0
  IL_00e2:  ret

  IL_00e3:  ldloc.0
  IL_00e4:  ret
}")
                diff1.VerifyIL("C.VB$StateMachine_6_F.MoveNext", "
{
  // Code size      279 (0x117)
  .maxstack  3
  .locals init (Boolean V_0,
                Integer V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.VB$StateMachine_6_F.$State As Integer""
  IL_0006:  stloc.1
  IL_0007:  ldloc.1
  IL_0008:  ldc.i4.s   -3
  IL_000a:  sub
  IL_000b:  switch    (
        IL_002a,
        IL_002e,
        IL_002e,
        IL_002c,
        IL_002a,
        IL_002a)
  IL_0028:  br.s       IL_002e
  IL_002a:  br.s       IL_0046
  IL_002c:  br.s       IL_0030

  IL_002e:  ldc.i4.0
  IL_002f:  ret

  IL_0030:  ldarg.0
  IL_0031:  ldc.i4.m1
  IL_0032:  dup
  IL_0033:  stloc.1
  IL_0034:  stfld      ""C.VB$StateMachine_6_F.$State As Integer""
  IL_0039:  nop
  IL_003a:  nop
  IL_003b:  ldarg.0
  IL_003c:  call       ""Function C.D() As System.IDisposable""
  IL_0041:  stfld      ""C.VB$StateMachine_6_F.$VB$ResumableLocal_x$0 As System.IDisposable""

  IL_0046:  nop
  .try
  {
    IL_0047:  ldloc.1
    IL_0048:  ldc.i4.s   -3
    IL_004a:  beq.s      IL_0056
    IL_004c:  br.s       IL_004e

    IL_004e:  ldloc.1
    IL_004f:  ldc.i4.1
    IL_0050:  sub
    IL_0051:  ldc.i4.1
    IL_0052:  ble.un.s   IL_0056
    IL_0054:  br.s       IL_0058
    IL_0056:  br.s       IL_0063

    IL_0058:  ldarg.0
    IL_0059:  newobj     ""Sub C..ctor()""
    IL_005e:  stfld      ""C.VB$StateMachine_6_F.$VB$ResumableLocal_y$1 As C""

    IL_0063:  nop
    .try
    {
      IL_0064:  ldloc.1
      IL_0065:  ldc.i4.s   -3
      IL_0067:  beq.s      IL_0077
      IL_0069:  br.s       IL_006b
      IL_006b:  ldloc.1
      IL_006c:  ldc.i4.1
      IL_006d:  beq.s      IL_0079
      IL_006f:  br.s       IL_0071
      IL_0071:  ldloc.1
      IL_0072:  ldc.i4.2
      IL_0073:  beq.s      IL_007b
      IL_0075:  br.s       IL_007d
      IL_0077:  br.s       IL_007f
      IL_0079:  br.s       IL_00a7
      IL_007b:  br.s       IL_00c8
      IL_007d:  br.s       IL_008f

      IL_007f:  ldarg.0
      IL_0080:  ldc.i4.m1
      IL_0081:  dup
      IL_0082:  stloc.1
      IL_0083:  stfld      ""C.VB$StateMachine_6_F.$State As Integer""
      IL_0088:  ldc.i4.1
      IL_0089:  stloc.0
      IL_008a:  leave      IL_0115

      IL_008f:  ldarg.0
      IL_0090:  call       ""Function C.M2() As Integer""
      IL_0095:  stfld      ""C.VB$StateMachine_6_F.$Current As Integer""
      IL_009a:  ldarg.0
      IL_009b:  ldc.i4.1
      IL_009c:  dup
      IL_009d:  stloc.1
      IL_009e:  stfld      ""C.VB$StateMachine_6_F.$State As Integer""
      IL_00a3:  ldc.i4.1
      IL_00a4:  stloc.0
      IL_00a5:  leave.s    IL_0115

      IL_00a7:  ldarg.0
      IL_00a8:  ldc.i4.m1
      IL_00a9:  dup
      IL_00aa:  stloc.1
      IL_00ab:  stfld      ""C.VB$StateMachine_6_F.$State As Integer""
      IL_00b0:  ldarg.0
      IL_00b1:  call       ""Function C.M3() As Integer""
      IL_00b6:  stfld      ""C.VB$StateMachine_6_F.$Current As Integer""
      IL_00bb:  ldarg.0
      IL_00bc:  ldc.i4.2
      IL_00bd:  dup
      IL_00be:  stloc.1
      IL_00bf:  stfld      ""C.VB$StateMachine_6_F.$State As Integer""
      IL_00c4:  ldc.i4.1
      IL_00c5:  stloc.0
      IL_00c6:  leave.s    IL_0115

      IL_00c8:  ldarg.0
      IL_00c9:  ldc.i4.m1
      IL_00ca:  dup
      IL_00cb:  stloc.1
      IL_00cc:  stfld      ""C.VB$StateMachine_6_F.$State As Integer""
      IL_00d1:  leave.s    IL_00ef
    }
    finally
    {
      IL_00d3:  ldloc.1
      IL_00d4:  ldc.i4.0
      IL_00d5:  bge.s      IL_00ee
      IL_00d7:  nop
      IL_00d8:  ldarg.0
      IL_00d9:  ldfld      ""C.VB$StateMachine_6_F.$VB$ResumableLocal_y$1 As C""
      IL_00de:  brfalse.s  IL_00ec
      IL_00e0:  ldarg.0
      IL_00e1:  ldfld      ""C.VB$StateMachine_6_F.$VB$ResumableLocal_y$1 As C""
      IL_00e6:  callvirt   ""Sub System.IDisposable.Dispose()""
      IL_00eb:  nop
      IL_00ec:  br.s       IL_00ee
      IL_00ee:  endfinally
    }
    IL_00ef:  leave.s    IL_010d
  }
  finally
  {
    IL_00f1:  ldloc.1
    IL_00f2:  ldc.i4.0
    IL_00f3:  bge.s      IL_010c
    IL_00f5:  nop
    IL_00f6:  ldarg.0
    IL_00f7:  ldfld      ""C.VB$StateMachine_6_F.$VB$ResumableLocal_x$0 As System.IDisposable""
    IL_00fc:  brfalse.s  IL_010a
    IL_00fe:  ldarg.0
    IL_00ff:  ldfld      ""C.VB$StateMachine_6_F.$VB$ResumableLocal_x$0 As System.IDisposable""
    IL_0104:  callvirt   ""Sub System.IDisposable.Dispose()""
    IL_0109:  nop
    IL_010a:  br.s       IL_010c
    IL_010c:  endfinally
  }
  IL_010d:  call       ""Sub C.End()""
  IL_0112:  nop
  IL_0113:  ldc.i4.0
  IL_0114:  ret
  IL_0115:  ldloc.0
  IL_0116:  ret
}")
            End Using
        End Sub

        <Fact>
        Public Sub UpdateIterator_Yield_Add_Finally_Foreach_UsingExpr_Lock()
            Dim template = "
Imports System
Imports System.Collections.Generic
Class C
    Shared Function D() As IDisposable
        Return Nothing
    End Function

    Shared Function E() As IEnumerable(Of Integer)
        Return Nothing
    End Function

    Shared Iterator Function F() As IEnumerable(Of Integer)
{0}
    End Function
End Class"

            Dim source0 = MarkedSource(String.Format(template, "
<N:0><N:1>SyncLock D()</N:1>
    <N:2><N:3>Using D()</N:3>
        <N:4><N:5>For Each <N:6>y</N:6> in E()</N:5>
             <N:7>Yield 1</N:7>
        Next</N:4>
    End Using</N:2>
End SyncLock</N:0>
"))
            Dim source1 = MarkedSource(String.Format(template, "
<N:0><N:1>SyncLock D()</N:1>
    <N:2><N:3>Using D()</N:3>
        <N:4><N:5>For Each <N:6>y</N:6> in E()</N:5>
             <N:7>Yield 1</N:7>
             Yield 2
        Next</N:4>
    End Using</N:2>
End SyncLock</N:0>
"))

            Dim compilation0 = CreateEmptyCompilationWithReferences(source0.Tree, references:=LatestVbReferences, options:=ComSafeDebugDll)
            Dim compilation1 = compilation0.WithSource(source1.Tree)

            Dim f0 = compilation0.GetMember(Of MethodSymbol)("C.F")
            Dim f1 = compilation1.GetMember(Of MethodSymbol)("C.F")

            Dim v0 = CompileAndVerify(compilation:=compilation0).VerifyDiagnostics()
            Using md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)

                Dim generation0 = CreateInitialBaseline(compilation0, md0, Function(handle) v0.CreateSymReader().GetEncMethodDebugInfo(handle))
                Dim diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))))

                v0.VerifyPdb("C.F", "
 <symbols>
  <files>
    <file id=""1"" name="""" language=""VB"" />
  </files>
  <methods>
    <method containingType=""C"" name=""F"">
      <customDebugInfo>
        <forwardIterator name=""VB$StateMachine_3_F"" />
        <encLocalSlotMap>
          <slot kind=""3"" offset=""0"" />
          <slot kind=""2"" offset=""0"" />
          <slot kind=""4"" offset=""34"" />
          <slot kind=""5"" offset=""69"" />
          <slot kind=""0"" offset=""69"" />
        </encLocalSlotMap>
        <encStateMachineStateMap>
          <state number=""-3"" offset=""69"" />
          <state number=""1"" offset=""123"" />
        </encStateMachineStateMap>
      </customDebugInfo>
    </method>
  </methods>
</symbols>")
                v0.VerifyIL("C.VB$StateMachine_3_F.MoveNext", "
{
  // Code size      345 (0x159)
  .maxstack  3
  .locals init (Boolean V_0,
                Integer V_1,
                Boolean V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.VB$StateMachine_3_F.$State As Integer""
  IL_0006:  stloc.1
  IL_0007:  ldloc.1
  IL_0008:  ldc.i4.s   -3
  IL_000a:  sub
  IL_000b:  switch    (
        IL_0026,
        IL_002a,
        IL_002a,
        IL_0028,
        IL_0026)
  IL_0024:  br.s       IL_002a
  IL_0026:  br.s       IL_0049
  IL_0028:  br.s       IL_002c
  IL_002a:  ldc.i4.0
  IL_002b:  ret
  IL_002c:  ldarg.0
  IL_002d:  ldc.i4.m1
  IL_002e:  dup
  IL_002f:  stloc.1
  IL_0030:  stfld      ""C.VB$StateMachine_3_F.$State As Integer""
  IL_0035:  nop
  IL_0036:  nop
  IL_0037:  ldarg.0
  IL_0038:  call       ""Function C.D() As System.IDisposable""
  IL_003d:  stfld      ""C.VB$StateMachine_3_F.$S0 As Object""
  IL_0042:  ldarg.0
  IL_0043:  ldc.i4.0
  IL_0044:  stfld      ""C.VB$StateMachine_3_F.$S1 As Boolean""
  IL_0049:  nop
  .try
  {
    IL_004a:  ldloc.1
    IL_004b:  ldc.i4.s   -3
    IL_004d:  beq.s      IL_0057
    IL_004f:  br.s       IL_0051
    IL_0051:  ldloc.1
    IL_0052:  ldc.i4.1
    IL_0053:  beq.s      IL_0057
    IL_0055:  br.s       IL_0059
    IL_0057:  br.s       IL_0077
    IL_0059:  ldarg.0
    IL_005a:  ldfld      ""C.VB$StateMachine_3_F.$S0 As Object""
    IL_005f:  ldarg.0
    IL_0060:  ldflda     ""C.VB$StateMachine_3_F.$S1 As Boolean""
    IL_0065:  call       ""Sub System.Threading.Monitor.Enter(Object, ByRef Boolean)""
    IL_006a:  nop
    IL_006b:  nop
    IL_006c:  ldarg.0
    IL_006d:  call       ""Function C.D() As System.IDisposable""
    IL_0072:  stfld      ""C.VB$StateMachine_3_F.$S2 As System.IDisposable""
    IL_0077:  nop
    .try
    {
      IL_0078:  ldloc.1
      IL_0079:  ldc.i4.s   -3
      IL_007b:  beq.s      IL_0085
      IL_007d:  br.s       IL_007f
      IL_007f:  ldloc.1
      IL_0080:  ldc.i4.1
      IL_0081:  beq.s      IL_0085
      IL_0083:  br.s       IL_0087
      IL_0085:  br.s       IL_0087
      IL_0087:  nop
      .try
      {
        IL_0088:  ldloc.1
        IL_0089:  ldc.i4.s   -3
        IL_008b:  beq.s      IL_0095
        IL_008d:  br.s       IL_008f
        IL_008f:  ldloc.1
        IL_0090:  ldc.i4.1
        IL_0091:  beq.s      IL_0097
        IL_0093:  br.s       IL_0099
        IL_0095:  br.s       IL_009b
        IL_0097:  br.s       IL_00e2
        IL_0099:  br.s       IL_00ab
        IL_009b:  ldarg.0
        IL_009c:  ldc.i4.m1
        IL_009d:  dup
        IL_009e:  stloc.1
        IL_009f:  stfld      ""C.VB$StateMachine_3_F.$State As Integer""
        IL_00a4:  ldc.i4.1
        IL_00a5:  stloc.0
        IL_00a6:  leave      IL_0157
        IL_00ab:  ldarg.0
        IL_00ac:  call       ""Function C.E() As System.Collections.Generic.IEnumerable(Of Integer)""
        IL_00b1:  callvirt   ""Function System.Collections.Generic.IEnumerable(Of Integer).GetEnumerator() As System.Collections.Generic.IEnumerator(Of Integer)""
        IL_00b6:  stfld      ""C.VB$StateMachine_3_F.$S3 As System.Collections.Generic.IEnumerator(Of Integer)""
        IL_00bb:  br.s       IL_00ec
        IL_00bd:  ldarg.0
        IL_00be:  ldarg.0
        IL_00bf:  ldfld      ""C.VB$StateMachine_3_F.$S3 As System.Collections.Generic.IEnumerator(Of Integer)""
        IL_00c4:  callvirt   ""Function System.Collections.Generic.IEnumerator(Of Integer).get_Current() As Integer""
        IL_00c9:  stfld      ""C.VB$StateMachine_3_F.$VB$ResumableLocal_y$4 As Integer""
        IL_00ce:  ldarg.0
        IL_00cf:  ldc.i4.1
        IL_00d0:  stfld      ""C.VB$StateMachine_3_F.$Current As Integer""
        IL_00d5:  ldarg.0
        IL_00d6:  ldc.i4.1
        IL_00d7:  dup
        IL_00d8:  stloc.1
        IL_00d9:  stfld      ""C.VB$StateMachine_3_F.$State As Integer""
        IL_00de:  ldc.i4.1
        IL_00df:  stloc.0
        IL_00e0:  leave.s    IL_0157
        IL_00e2:  ldarg.0
        IL_00e3:  ldc.i4.m1
        IL_00e4:  dup
        IL_00e5:  stloc.1
        IL_00e6:  stfld      ""C.VB$StateMachine_3_F.$State As Integer""
        IL_00eb:  nop
        IL_00ec:  ldarg.0
        IL_00ed:  ldfld      ""C.VB$StateMachine_3_F.$S3 As System.Collections.Generic.IEnumerator(Of Integer)""
        IL_00f2:  callvirt   ""Function System.Collections.IEnumerator.MoveNext() As Boolean""
        IL_00f7:  stloc.2
        IL_00f8:  ldloc.2
        IL_00f9:  brtrue.s   IL_00bd
        IL_00fb:  leave.s    IL_0118
      }
      finally
      {
        IL_00fd:  ldloc.1
        IL_00fe:  ldc.i4.0
        IL_00ff:  bge.s      IL_0117
        IL_0101:  ldarg.0
        IL_0102:  ldfld      ""C.VB$StateMachine_3_F.$S3 As System.Collections.Generic.IEnumerator(Of Integer)""
        IL_0107:  brfalse.s  IL_0115
        IL_0109:  ldarg.0
        IL_010a:  ldfld      ""C.VB$StateMachine_3_F.$S3 As System.Collections.Generic.IEnumerator(Of Integer)""
        IL_010f:  callvirt   ""Sub System.IDisposable.Dispose()""
        IL_0114:  nop
        IL_0115:  br.s       IL_0117
        IL_0117:  endfinally
      }
      IL_0118:  leave.s    IL_0136
    }
    finally
    {
      IL_011a:  ldloc.1
      IL_011b:  ldc.i4.0
      IL_011c:  bge.s      IL_0135
      IL_011e:  nop
      IL_011f:  ldarg.0
      IL_0120:  ldfld      ""C.VB$StateMachine_3_F.$S2 As System.IDisposable""
      IL_0125:  brfalse.s  IL_0133
      IL_0127:  ldarg.0
      IL_0128:  ldfld      ""C.VB$StateMachine_3_F.$S2 As System.IDisposable""
      IL_012d:  callvirt   ""Sub System.IDisposable.Dispose()""
      IL_0132:  nop
      IL_0133:  br.s       IL_0135
      IL_0135:  endfinally
    }
    IL_0136:  leave.s    IL_0154
  }
  finally
  {
    IL_0138:  ldloc.1
    IL_0139:  ldc.i4.0
    IL_013a:  bge.s      IL_0153
    IL_013c:  ldarg.0
    IL_013d:  ldfld      ""C.VB$StateMachine_3_F.$S1 As Boolean""
    IL_0142:  brfalse.s  IL_0150
    IL_0144:  ldarg.0
    IL_0145:  ldfld      ""C.VB$StateMachine_3_F.$S0 As Object""
    IL_014a:  call       ""Sub System.Threading.Monitor.Exit(Object)""
    IL_014f:  nop
    IL_0150:  nop
    IL_0151:  br.s       IL_0153
    IL_0153:  endfinally
  }
  IL_0154:  nop
  IL_0155:  ldc.i4.0
  IL_0156:  ret
  IL_0157:  ldloc.0
  IL_0158:  ret
}")
                diff1.VerifyIL("C.VB$StateMachine_3_F.MoveNext", "
{
  // Code size      393 (0x189)
  .maxstack  3
  .locals init (Boolean V_0,
                Integer V_1,
                Boolean V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.VB$StateMachine_3_F.$State As Integer""
  IL_0006:  stloc.1
  IL_0007:  ldloc.1
  IL_0008:  ldc.i4.s   -3
  IL_000a:  sub
  IL_000b:  switch    (
        IL_002a,
        IL_002e,
        IL_002e,
        IL_002c,
        IL_002a,
        IL_002a)
  IL_0028:  br.s       IL_002e
  IL_002a:  br.s       IL_004d
  IL_002c:  br.s       IL_0030
  IL_002e:  ldc.i4.0
  IL_002f:  ret
  IL_0030:  ldarg.0
  IL_0031:  ldc.i4.m1
  IL_0032:  dup
  IL_0033:  stloc.1
  IL_0034:  stfld      ""C.VB$StateMachine_3_F.$State As Integer""
  IL_0039:  nop
  IL_003a:  nop
  IL_003b:  ldarg.0
  IL_003c:  call       ""Function C.D() As System.IDisposable""
  IL_0041:  stfld      ""C.VB$StateMachine_3_F.$S0 As Object""
  IL_0046:  ldarg.0
  IL_0047:  ldc.i4.0
  IL_0048:  stfld      ""C.VB$StateMachine_3_F.$S1 As Boolean""
  IL_004d:  nop
  .try
  {
    IL_004e:  ldloc.1
    IL_004f:  ldc.i4.s   -3
    IL_0051:  beq.s      IL_005d
    IL_0053:  br.s       IL_0055
    IL_0055:  ldloc.1
    IL_0056:  ldc.i4.1
    IL_0057:  sub
    IL_0058:  ldc.i4.1
    IL_0059:  ble.un.s   IL_005d
    IL_005b:  br.s       IL_005f
    IL_005d:  br.s       IL_007d
    IL_005f:  ldarg.0
    IL_0060:  ldfld      ""C.VB$StateMachine_3_F.$S0 As Object""
    IL_0065:  ldarg.0
    IL_0066:  ldflda     ""C.VB$StateMachine_3_F.$S1 As Boolean""
    IL_006b:  call       ""Sub System.Threading.Monitor.Enter(Object, ByRef Boolean)""
    IL_0070:  nop
    IL_0071:  nop
    IL_0072:  ldarg.0
    IL_0073:  call       ""Function C.D() As System.IDisposable""
    IL_0078:  stfld      ""C.VB$StateMachine_3_F.$S2 As System.IDisposable""
    IL_007d:  nop
    .try
    {
      IL_007e:  ldloc.1
      IL_007f:  ldc.i4.s   -3
      IL_0081:  beq.s      IL_008d
      IL_0083:  br.s       IL_0085
      IL_0085:  ldloc.1
      IL_0086:  ldc.i4.1
      IL_0087:  sub
      IL_0088:  ldc.i4.1
      IL_0089:  ble.un.s   IL_008d
      IL_008b:  br.s       IL_008f
      IL_008d:  br.s       IL_008f
      IL_008f:  nop
      .try
      {
        IL_0090:  ldloc.1
        IL_0091:  ldc.i4.s   -3
        IL_0093:  beq.s      IL_00a3
        IL_0095:  br.s       IL_0097
        IL_0097:  ldloc.1
        IL_0098:  ldc.i4.1
        IL_0099:  beq.s      IL_00a5
        IL_009b:  br.s       IL_009d
        IL_009d:  ldloc.1
        IL_009e:  ldc.i4.2
        IL_009f:  beq.s      IL_00a7
        IL_00a1:  br.s       IL_00a9
        IL_00a3:  br.s       IL_00ab
        IL_00a5:  br.s       IL_00f5
        IL_00a7:  br.s       IL_0112
        IL_00a9:  br.s       IL_00bb
        IL_00ab:  ldarg.0
        IL_00ac:  ldc.i4.m1
        IL_00ad:  dup
        IL_00ae:  stloc.1
        IL_00af:  stfld      ""C.VB$StateMachine_3_F.$State As Integer""
        IL_00b4:  ldc.i4.1
        IL_00b5:  stloc.0
        IL_00b6:  leave      IL_0187
        IL_00bb:  ldarg.0
        IL_00bc:  call       ""Function C.E() As System.Collections.Generic.IEnumerable(Of Integer)""
        IL_00c1:  callvirt   ""Function System.Collections.Generic.IEnumerable(Of Integer).GetEnumerator() As System.Collections.Generic.IEnumerator(Of Integer)""
        IL_00c6:  stfld      ""C.VB$StateMachine_3_F.$S3 As System.Collections.Generic.IEnumerator(Of Integer)""
        IL_00cb:  br.s       IL_011c
        IL_00cd:  ldarg.0
        IL_00ce:  ldarg.0
        IL_00cf:  ldfld      ""C.VB$StateMachine_3_F.$S3 As System.Collections.Generic.IEnumerator(Of Integer)""
        IL_00d4:  callvirt   ""Function System.Collections.Generic.IEnumerator(Of Integer).get_Current() As Integer""
        IL_00d9:  stfld      ""C.VB$StateMachine_3_F.$VB$ResumableLocal_y$4 As Integer""
        IL_00de:  ldarg.0
        IL_00df:  ldc.i4.1
        IL_00e0:  stfld      ""C.VB$StateMachine_3_F.$Current As Integer""
        IL_00e5:  ldarg.0
        IL_00e6:  ldc.i4.1
        IL_00e7:  dup
        IL_00e8:  stloc.1
        IL_00e9:  stfld      ""C.VB$StateMachine_3_F.$State As Integer""
        IL_00ee:  ldc.i4.1
        IL_00ef:  stloc.0
        IL_00f0:  leave      IL_0187
        IL_00f5:  ldarg.0
        IL_00f6:  ldc.i4.m1
        IL_00f7:  dup
        IL_00f8:  stloc.1
        IL_00f9:  stfld      ""C.VB$StateMachine_3_F.$State As Integer""
        IL_00fe:  ldarg.0
        IL_00ff:  ldc.i4.2
        IL_0100:  stfld      ""C.VB$StateMachine_3_F.$Current As Integer""
        IL_0105:  ldarg.0
        IL_0106:  ldc.i4.2
        IL_0107:  dup
        IL_0108:  stloc.1
        IL_0109:  stfld      ""C.VB$StateMachine_3_F.$State As Integer""
        IL_010e:  ldc.i4.1
        IL_010f:  stloc.0
        IL_0110:  leave.s    IL_0187
        IL_0112:  ldarg.0
        IL_0113:  ldc.i4.m1
        IL_0114:  dup
        IL_0115:  stloc.1
        IL_0116:  stfld      ""C.VB$StateMachine_3_F.$State As Integer""
        IL_011b:  nop
        IL_011c:  ldarg.0
        IL_011d:  ldfld      ""C.VB$StateMachine_3_F.$S3 As System.Collections.Generic.IEnumerator(Of Integer)""
        IL_0122:  callvirt   ""Function System.Collections.IEnumerator.MoveNext() As Boolean""
        IL_0127:  stloc.2
        IL_0128:  ldloc.2
        IL_0129:  brtrue.s   IL_00cd
        IL_012b:  leave.s    IL_0148
      }
      finally
      {
        IL_012d:  ldloc.1
        IL_012e:  ldc.i4.0
        IL_012f:  bge.s      IL_0147
        IL_0131:  ldarg.0
        IL_0132:  ldfld      ""C.VB$StateMachine_3_F.$S3 As System.Collections.Generic.IEnumerator(Of Integer)""
        IL_0137:  brfalse.s  IL_0145
        IL_0139:  ldarg.0
        IL_013a:  ldfld      ""C.VB$StateMachine_3_F.$S3 As System.Collections.Generic.IEnumerator(Of Integer)""
        IL_013f:  callvirt   ""Sub System.IDisposable.Dispose()""
        IL_0144:  nop
        IL_0145:  br.s       IL_0147
        IL_0147:  endfinally
      }
      IL_0148:  leave.s    IL_0166
    }
    finally
    {
      IL_014a:  ldloc.1
      IL_014b:  ldc.i4.0
      IL_014c:  bge.s      IL_0165
      IL_014e:  nop
      IL_014f:  ldarg.0
      IL_0150:  ldfld      ""C.VB$StateMachine_3_F.$S2 As System.IDisposable""
      IL_0155:  brfalse.s  IL_0163
      IL_0157:  ldarg.0
      IL_0158:  ldfld      ""C.VB$StateMachine_3_F.$S2 As System.IDisposable""
      IL_015d:  callvirt   ""Sub System.IDisposable.Dispose()""
      IL_0162:  nop
      IL_0163:  br.s       IL_0165
      IL_0165:  endfinally
    }
    IL_0166:  leave.s    IL_0184
  }
  finally
  {
    IL_0168:  ldloc.1
    IL_0169:  ldc.i4.0
    IL_016a:  bge.s      IL_0183
    IL_016c:  ldarg.0
    IL_016d:  ldfld      ""C.VB$StateMachine_3_F.$S1 As Boolean""
    IL_0172:  brfalse.s  IL_0180
    IL_0174:  ldarg.0
    IL_0175:  ldfld      ""C.VB$StateMachine_3_F.$S0 As Object""
    IL_017a:  call       ""Sub System.Threading.Monitor.Exit(Object)""
    IL_017f:  nop
    IL_0180:  nop
    IL_0181:  br.s       IL_0183
    IL_0183:  endfinally
  }
  IL_0184:  nop
  IL_0185:  ldc.i4.0
  IL_0186:  ret
  IL_0187:  ldloc.0
  IL_0188:  ret
}")
            End Using
        End Sub

        <Fact>
        Public Sub UpdateAsync_UserDefinedVariables_NoChange()
            Dim source0 = MarkedSource("
Imports System.Threading.Tasks
Class C
    Async Function F(p As Integer) As Task(Of Integer)
        Dim <N:0>x</N:0> = p
        Return <N:1>Await Task.FromResult(10)</N:1>
    End Function
End Class")
            Dim source1 = MarkedSource("
Imports System.Threading.Tasks
Class C
    Async Function F(p As Integer) As Task(Of Integer)
        Dim <N:0>x</N:0> = p
        Return <N:1>Await Task.FromResult(20)</N:1>
    End Function
End Class")

            Dim compilation0 = CreateEmptyCompilationWithReferences(source0.Tree, references:=LatestVbReferences, options:=ComSafeDebugDll)
            Dim compilation1 = compilation0.WithSource(source1.Tree)

            Dim v0 = CompileAndVerify(compilation:=compilation0)
            Dim symReader = v0.CreateSymReader()

            Using md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)
                Dim method0 = compilation0.GetMember(Of MethodSymbol)("C.F")
                Dim method1 = compilation1.GetMember(Of MethodSymbol)("C.F")

                Dim generation0 = CreateInitialBaseline(compilation0, md0, Function(handle) symReader.GetEncMethodDebugInfo(handle))
                Dim diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, method0, method1, GetSyntaxMapFromMarkers(source0, source1))))

                ' only methods with sequence points should be listed in UpdatedMethods:
                diff1.VerifyUpdatedMethods("0x06000004")

                ' verify delta metadata contains expected rows
                Using md1 = diff1.GetMetadata()
                    CheckEncLogDefinitions(md1.Reader,
                        Row(3, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(4, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(1, TableIndex.Param, EditAndContinueOperation.Default),
                        Row(4, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(5, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(7, TableIndex.CustomAttribute, EditAndContinueOperation.Default))
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
            Dim source0 = MarkedSource("
Imports System.Threading.Tasks
Class C
    Async Function F(p As Integer) As Task(Of Integer)
        Dim <N:0>x</N:0> = p
        Return <N:1>Await Task.FromResult(10)</N:1>
    End Function
End Class")
            Dim source1 = MarkedSource("
Imports System.Threading.Tasks
Class C
    Async Function F(p As Integer) As Task(Of Integer)
        Dim <N:0>x</N:0> = p
        Dim y = 10
        Return <N:1>Await Task.FromResult(y)</N:1>
    End Function
End Class")

            Dim compilation0 = CreateEmptyCompilationWithReferences(source0.Tree, references:=LatestVbReferences, options:=ComSafeDebugDll)
            Dim compilation1 = compilation0.WithSource(source1.Tree)

            Dim v0 = CompileAndVerify(compilation:=compilation0)
            Dim symReader = v0.CreateSymReader()

            Using md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)
                Dim method0 = compilation0.GetMember(Of MethodSymbol)("C.F")
                Dim method1 = compilation1.GetMember(Of MethodSymbol)("C.F")

                Dim generation0 = CreateInitialBaseline(compilation0, md0, Function(handle) symReader.GetEncMethodDebugInfo(handle))
                Dim diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, method0, method1, GetSyntaxMapFromMarkers(source0, source1))))

                ' only methods with sequence points should be listed in UpdatedMethods:
                diff1.VerifyUpdatedMethods("0x06000004")

                ' verify delta metadata contains expected rows
                Using md1 = diff1.GetMetadata()
                    CheckEncLogDefinitions(md1.Reader,
                        Row(3, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(4, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                        Row(7, TableIndex.Field, EditAndContinueOperation.Default),
                        Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(1, TableIndex.Param, EditAndContinueOperation.Default),
                        Row(4, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(5, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(7, TableIndex.CustomAttribute, EditAndContinueOperation.Default))
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
            Dim source0 = MarkedSource("
Imports System.Threading.Tasks
Class C
    Async Function F(p As Integer) As Task(Of Integer)
        Dim x = p
        Return <N:0>Await Task.FromResult(10)</N:0>
    End Function
End Class")
            Dim source1 = MarkedSource("
Imports System.Threading.Tasks
Class C
    Async Function F(p As Integer) As Task(Of Integer)
        Dim y = 1234
        Return <N:0>Await Task.FromResult(p)</N:0>
    End Function
End Class")

            Dim compilation0 = CreateEmptyCompilationWithReferences(source0.Tree, references:=LatestVbReferences, options:=ComSafeDebugDll)
            Dim compilation1 = compilation0.WithSource(source1.Tree)

            Dim v0 = CompileAndVerify(compilation:=compilation0)
            Dim symReader = v0.CreateSymReader()

            Using md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)
                Dim method0 = compilation0.GetMember(Of MethodSymbol)("C.F")
                Dim method1 = compilation1.GetMember(Of MethodSymbol)("C.F")

                Dim generation0 = CreateInitialBaseline(compilation0, md0, Function(handle) symReader.GetEncMethodDebugInfo(handle))
                Dim diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, method0, method1, GetSyntaxMapFromMarkers(source0, source1))))

                ' only methods with sequence points should be listed in UpdatedMethods:
                diff1.VerifyUpdatedMethods("0x06000004")

                ' verify delta metadata contains expected rows
                Using md1 = diff1.GetMetadata()
                    CheckEncLogDefinitions(md1.Reader,
                        Row(3, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(4, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                        Row(7, TableIndex.Field, EditAndContinueOperation.Default),
                        Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(1, TableIndex.Param, EditAndContinueOperation.Default),
                        Row(4, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(5, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(7, TableIndex.CustomAttribute, EditAndContinueOperation.Default))
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
            Dim source0 = MarkedSource("
Imports System.Threading.Tasks
Class C
    Async Function F() As Task(Of Integer)
        Dim <N:0>x</N:0> = 10
        Return <N:1>Await Task.FromResult(10)</N:1>
    End Function
End Class")
            Dim source1 = MarkedSource("
Imports System.Threading.Tasks
Class C
    Async Function F() As Task(Of Integer)
        Dim <N:0>x</N:0> = 10.0
        Return <N:1>Await Task.FromResult(20)</N:1>
    End Function
End Class")

            Dim compilation0 = CreateEmptyCompilationWithReferences(source0.Tree, references:=LatestVbReferences, options:=ComSafeDebugDll)
            Dim compilation1 = compilation0.WithSource(source1.Tree)

            Dim v0 = CompileAndVerify(compilation:=compilation0)
            Dim symReader = v0.CreateSymReader()

            Using md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)
                Dim method0 = compilation0.GetMember(Of MethodSymbol)("C.F")
                Dim method1 = compilation1.GetMember(Of MethodSymbol)("C.F")

                Dim generation0 = CreateInitialBaseline(compilation0, md0, Function(handle) symReader.GetEncMethodDebugInfo(handle))
                Dim diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, method0, method1, GetSyntaxMapFromMarkers(source0, source1))))

                ' only methods with sequence points should be listed in UpdatedMethods:
                diff1.VerifyUpdatedMethods("0x06000004")

                ' verify delta metadata contains expected rows
                Using md1 = diff1.GetMetadata()
                    CheckEncLogDefinitions(md1.Reader,
                        Row(3, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(4, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                        Row(6, TableIndex.Field, EditAndContinueOperation.Default),
                        Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(4, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(5, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                        Row(7, TableIndex.CustomAttribute, EditAndContinueOperation.Default))
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
            Dim source0 = MarkedSource("
Imports System.Threading.Tasks
Class C
    Async Function F() As Task(Of Integer) ' testing type changes G0 -> G1, G1 -> G2
        Dim <N:0>a1</N:0> As Boolean = True
        Dim <N:1>a2</N:1> As Integer = 3
        <N:2>Await Task.Delay(0)</N:2>
        Return 1
    End Function

    Async Function G() As Task(Of Integer) ' testing G1 -> G3
        Dim <N:3>c</N:3> = New C()
        Dim <N:4>a1</N:4> As Boolean = True
        <N:5>Await Task.Delay(0)</N:5>
        Return 1
    End Function

    Async Function H() As Task(Of Integer) ' testing G0 -> G3
        Dim <N:6>c</N:6> = New C()
        Dim <N:7>a1</N:7> As Boolean = True
        <N:8>Await Task.Delay(0)</N:8>
        Return 1
    End Function
End Class")
            Dim source1 = MarkedSource("
Imports System.Threading.Tasks
Class C
    Async Function F() As Task(Of Integer) ' updated
        Dim <N:0>a1</N:0> = new C()
        Dim <N:1>a2</N:1> As Integer = 3
        <N:2>Await Task.Delay(0)</N:2>
        Return 1
    End Function

    Async Function G() As Task(Of Integer) ' updated
        Dim <N:3>c</N:3> = New C()
        Dim <N:4>a1</N:4> As Boolean = True
        <N:5>Await Task.Delay(0)</N:5>
        Return 2
    End Function

    Async Function H() As Task(Of Integer)
        Dim <N:6>c</N:6> = New C()
        Dim <N:7>a1</N:7> As Boolean = True
        <N:8>Await Task.Delay(0)</N:8>
        Return 1
    End Function
End Class")
            Dim source2 = MarkedSource("
Imports System.Threading.Tasks
Class C
    Async Function F() As Task(Of Integer) ' updated
        Dim <N:0>a1</N:0> As Boolean = True
        Dim <N:1>a2</N:1> = New C()
        <N:2>Await Task.Delay(0)</N:2>
        Return 1
    End Function

    Async Function G() As Task(Of Integer)
        Dim <N:3>c</N:3> = New C()
        Dim <N:4>a1</N:4> As Boolean = True
        <N:5>Await Task.Delay(0)</N:5>
        Return 2
    End Function

    Async Function H() As Task(Of Integer)
        Dim <N:6>c</N:6> = New C()
        Dim <N:7>a1</N:7> As Boolean = True
        <N:8>Await Task.Delay(0)</N:8>
        Return 1
    End Function
End Class")
            Dim source3 = MarkedSource("
Imports System.Threading.Tasks
Class C
    Async Function F() As Task(Of Integer)
        Dim <N:0>a1</N:0> As Boolean = True
        Dim <N:1>a2</N:1> = New C()
        <N:2>Await Task.Delay(0)</N:2>
        Return 1
    End Function

    Async Function G() As Task(Of Integer) ' updated
        Dim <N:3>c</N:3> = New C()
        Dim <N:4>a1</N:4> = New C()
        <N:5>Await Task.Delay(0)</N:5>
        Return 1
    End Function

    Async Function H() As Task(Of Integer) ' updated
        Dim <N:6>c</N:6> = New C()
        Dim <N:7>a1</N:7> = New C()
        <N:8>Await Task.Delay(0)</N:8>
        Return 1
    End Function
End Class")

            ' Rude edit but the compiler should handle it
            Dim compilation0 = CreateEmptyCompilationWithReferences(source0.Tree, references:=LatestVbReferences, options:=ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            Dim compilation1 = compilation0.WithSource(source1.Tree)
            Dim compilation2 = compilation1.WithSource(source2.Tree)
            Dim compilation3 = compilation2.WithSource(source3.Tree)

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

            Dim generation0 = CreateInitialBaseline(compilation0, md0, Function(handle) v0.CreateSymReader().GetEncMethodDebugInfo(handle))
            Dim syntaxMap1 = GetSyntaxMapFromMarkers(source0, source1)
            Dim diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(
                        New SemanticEdit(SemanticEditKind.Update, f0, f1, syntaxMap1),
                        New SemanticEdit(SemanticEditKind.Update, g0, g1, syntaxMap1)))

            diff1.VerifySynthesizedMembers(
                "C: {VB$StateMachine_1_F, VB$StateMachine_2_G}",
                "C.VB$StateMachine_1_F: {$State, $Builder, $VB$Me, $VB$ResumableLocal_a1$2, $VB$ResumableLocal_a2$1, $A0, MoveNext, System.Runtime.CompilerServices.IAsyncStateMachine.SetStateMachine}",
                "C.VB$StateMachine_2_G: {$State, $Builder, $VB$Me, $VB$ResumableLocal_c$0, $VB$ResumableLocal_a1$1, $A0, MoveNext, System.Runtime.CompilerServices.IAsyncStateMachine.SetStateMachine}")

            Dim syntaxMap2 = GetSyntaxMapFromMarkers(source1, source2)
            Dim diff2 = compilation2.EmitDifference(
                    diff1.NextGeneration,
                    ImmutableArray.Create(
                        New SemanticEdit(SemanticEditKind.Update, f1, f2, syntaxMap2)))

            diff2.VerifySynthesizedMembers(
                "C: {VB$StateMachine_1_F, VB$StateMachine_2_G}",
                "C.VB$StateMachine_1_F: {$State, $Builder, $VB$Me, $VB$ResumableLocal_a1$3, $VB$ResumableLocal_a2$4, $A0, MoveNext, System.Runtime.CompilerServices.IAsyncStateMachine.SetStateMachine, $VB$ResumableLocal_a1$2, $VB$ResumableLocal_a2$1}",
                "C.VB$StateMachine_2_G: {$State, $Builder, $VB$Me, $VB$ResumableLocal_c$0, $VB$ResumableLocal_a1$1, $A0, MoveNext, System.Runtime.CompilerServices.IAsyncStateMachine.SetStateMachine}")

            Dim syntaxMap3 = GetSyntaxMapFromMarkers(source2, source3)
            Dim diff3 = compilation3.EmitDifference(
                    diff2.NextGeneration,
                    ImmutableArray.Create(
                        New SemanticEdit(SemanticEditKind.Update, g2, g3, syntaxMap3),
                        New SemanticEdit(SemanticEditKind.Update, h2, h3, syntaxMap3)))

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
                    Row(4, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(5, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(6, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(7, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(13, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(15, TableIndex.CustomAttribute, EditAndContinueOperation.Default))

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
                    Row(4, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(5, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(13, TableIndex.CustomAttribute, EditAndContinueOperation.Default))

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
                    Row(6, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(7, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(9, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(10, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(15, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(17, TableIndex.CustomAttribute, EditAndContinueOperation.Default))

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

            Dim compilation0 = CreateEmptyCompilationWithReferences(source0, references:=LatestVbReferences, options:=ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All))

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
            Dim source0 = MarkedSource("
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
        <N:0>Await A1()</N:0>
        <N:1>Await A2()</N:1>
        Return 1
    End Function
    Async Function G() As Task(Of Integer) ' testing G1 -> G3
        <N:2>Await A1()</N:2>
        Return 1
    End Function
    Async Function H() As Task(Of Integer) ' testing G0 -> G3
        <N:3>Await A1()</N:3>
        Return 1
    End Function
End Class")
            Dim source1 = MarkedSource("
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
        <N:0>Await A3()</N:0>
        <N:1>Await A2()</N:1>
        Return 1
    End Function
    Async Function G() As Task(Of Integer) ' updated
        <N:2>Await A1()</N:2>
        Return 2
    End Function
    Async Function H() As Task(Of Integer)
        <N:3>Await A1()</N:3>
        Return 1
    End Function
End Class")
            Dim source2 = MarkedSource("
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
        <N:0>Await A1()</N:0>
        <N:1>Await A3()</N:1>
        Return 1
    End Function
    Async Function G() As Task(Of Integer)
        <N:2>Await A1()</N:2>
        Return 2
    End Function
    Async Function H() As Task(Of Integer)
        <N:3>Await A1()</N:3>
        Return 1
    End Function
End Class")
            Dim source3 = MarkedSource("
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
        <N:0>Await A1()</N:0>
        <N:1>Await A3()</N:1>
        Return 1
    End Function
    Async Function G() As Task(Of Integer) ' updated
        <N:2>Await A3()</N:2>
        Return 1
    End Function
    Async Function H() As Task(Of Integer) ' updated
        <N:3>Await A3()</N:3>
        Return 1
    End Function
End Class")

            ' Rude edit but the compiler should handle it
            Dim compilation0 = CreateEmptyCompilationWithReferences(source0.Tree, references:=LatestVbReferences, options:=ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            Dim compilation1 = compilation0.WithSource(source1.Tree)
            Dim compilation2 = compilation1.WithSource(source2.Tree)
            Dim compilation3 = compilation2.WithSource(source3.Tree)

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

            Dim generation0 = CreateInitialBaseline(compilation0, md0, Function(handle) v0.CreateSymReader().GetEncMethodDebugInfo(handle))

            Dim syntaxMap1 = GetSyntaxMapFromMarkers(source0, source1)
            Dim diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(
                        New SemanticEdit(SemanticEditKind.Update, f0, f1, syntaxMap1),
                        New SemanticEdit(SemanticEditKind.Update, g0, g1, syntaxMap1)))

            diff1.VerifySynthesizedMembers(
                "C: {VB$StateMachine_4_F, VB$StateMachine_5_G}",
                "C.VB$StateMachine_4_F: {$State, $Builder, $VB$Me, $A2, $A1, MoveNext, System.Runtime.CompilerServices.IAsyncStateMachine.SetStateMachine}",
                "C.VB$StateMachine_5_G: {$State, $Builder, $VB$Me, $A0, MoveNext, System.Runtime.CompilerServices.IAsyncStateMachine.SetStateMachine}")

            Dim syntaxMap2 = GetSyntaxMapFromMarkers(source1, source2)
            Dim diff2 = compilation2.EmitDifference(
                    diff1.NextGeneration,
                    ImmutableArray.Create(
                        New SemanticEdit(SemanticEditKind.Update, f1, f2, syntaxMap2)))

            diff2.VerifySynthesizedMembers(
                "C: {VB$StateMachine_4_F, VB$StateMachine_5_G}",
                "C.VB$StateMachine_4_F: {$State, $Builder, $VB$Me, $A3, $A2, MoveNext, System.Runtime.CompilerServices.IAsyncStateMachine.SetStateMachine, $A1}",
                "C.VB$StateMachine_5_G: {$State, $Builder, $VB$Me, $A0, MoveNext, System.Runtime.CompilerServices.IAsyncStateMachine.SetStateMachine}")

            Dim syntaxMap3 = GetSyntaxMapFromMarkers(source2, source3)
            Dim diff3 = compilation3.EmitDifference(
                    diff2.NextGeneration,
                    ImmutableArray.Create(
                        New SemanticEdit(SemanticEditKind.Update, g2, g3, syntaxMap3),
                        New SemanticEdit(SemanticEditKind.Update, h2, h3, syntaxMap3)))

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
                    Row(6, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(7, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(9, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(10, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(13, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(15, TableIndex.CustomAttribute, EditAndContinueOperation.Default))

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
                    Row(6, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(7, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(13, TableIndex.CustomAttribute, EditAndContinueOperation.Default))

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
                    Row(9, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(10, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(11, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(12, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(15, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(17, TableIndex.CustomAttribute, EditAndContinueOperation.Default))
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
            Dim compilation0 = CreateEmptyCompilationWithReferences(source0, references:=LatestVbReferences, options:=ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
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

            Dim generation0 = CreateInitialBaseline(compilation0, md0, Function(handle) v0.CreateSymReader().GetEncMethodDebugInfo(handle))

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
                        New SemanticEdit(SemanticEditKind.Update, f1, f2, GetSyntaxMapByKind(f1, SyntaxKind.FunctionBlock))))

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
            Dim generation0 = CreateInitialBaseline(compilation0, md0, AddressOf v0.CreateSymReader().GetEncMethodDebugInfo)

            Dim f0 = compilation0.GetMember(Of MethodSymbol)("C.F")
            Dim f1 = compilation1.GetMember(Of MethodSymbol)("C.F")
            Dim f2 = compilation2.GetMember(Of MethodSymbol)("C.F")

            Dim diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    New SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))))

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
                    New SemanticEdit(SemanticEditKind.Update, f1, f2, GetSyntaxMapFromMarkers(source1, source2))))

            ' note that the types of the awaiter fields $A0, $A1 are the same as in the previous generation
            diff2.VerifySynthesizedFields("C._Closure$__.VB$StateMachine___Lambda$__1-0",
                "$State: Integer",
                "$Builder: System.Runtime.CompilerServices.AsyncTaskMethodBuilder",
                "$VB$NonLocal__Closure$__: C._Closure$__",
                "$A0: System.Runtime.CompilerServices.TaskAwaiter(Of Boolean)",
                "$A1: System.Runtime.CompilerServices.TaskAwaiter(Of Integer)")

        End Sub

        <Fact>
        Public Sub LiftedClosure()
            Dim source0 = MarkedSource("
Imports System
Imports System.Threading.Tasks
Class C
    <N:0>Shared Async Function M() As Task 
        Dim <N:1>num</N:1> As Integer = 1
                        
        <N:2>Await Task.Delay(1)</N:2>
                        
        G(<N:3>Function() num</N:3>)
    End Function</N:0>

    Shared Sub G(f As Func(Of Integer))
    End Sub
End Class")

            Dim source1 = MarkedSource("
Imports System
Imports System.Threading.Tasks
Class C
    <N:0>Shared Async Function M() As Task 
        Dim <N:1>num</N:1> As Integer = 1
                        
        <N:2>Await Task.Delay(2)</N:2>
                        
        G(<N:3>Function() num</N:3>)
    End Function</N:0>
    
    Shared Sub G(f As Func(Of Integer))
    End Sub
End Class")
            Dim compilation0 = CreateCompilationWithMscorlib45AndVBRuntime({source0.Tree}, options:=ComSafeDebugDll)
            Dim compilation1 = compilation0.WithSource(source1.Tree)

            Dim m0 = compilation0.GetMember(Of MethodSymbol)("C.M")
            Dim m1 = compilation1.GetMember(Of MethodSymbol)("C.M")

            Dim v0 = CompileAndVerify(compilation0)
            Using md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)

                Dim reader0 = md0.MetadataReader
                Dim generation0 = CreateInitialBaseline(compilation0, md0, AddressOf v0.CreateSymReader().GetEncMethodDebugInfo)

                ' Notice encLocalSlotMap CDI on both M and MoveNext methods.
                ' The former is used to calculate mapping for variables lifted to fields of the state machine,
                ' the latter is used to map local variable slots in the MoveNext method.
                ' Here, the variable lifted to the state machine field is the closure pointer storage.
                v0.VerifyPdb("
<symbols>
  <methods>
    <method containingType=""C"" name=""M"">
      <customDebugInfo>
        <forwardIterator name=""VB$StateMachine_1_M"" />
        <encLocalSlotMap>
          <slot kind=""30"" offset=""-1"" />
        </encLocalSlotMap>
        <encLambdaMap>
          <methodOrdinal>1</methodOrdinal>
          <closure offset=""-1"" />
          <lambda offset=""142"" closure=""0"" />
        </encLambdaMap>
        <encStateMachineStateMap>
          <state number=""0"" offset=""74"" />
        </encStateMachineStateMap>
      </customDebugInfo>
    </method>
    <method containingType=""C+VB$StateMachine_1_M"" name=""MoveNext"">
      <customDebugInfo>
        <hoistedLocalScopes>
          <slot startOffset=""0x0"" endOffset=""0xe3"" />
        </hoistedLocalScopes>
        <encLocalSlotMap>
          <slot kind=""27"" offset=""-1"" />
          <slot kind=""33"" offset=""74"" />
          <slot kind=""temp"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <asyncInfo>
        <kickoffMethod declaringType=""C"" methodName=""M"" />
        <await yield=""0x44"" resume=""0x62"" declaringType=""C+VB$StateMachine_1_M"" methodName=""MoveNext"" />
      </asyncInfo>
    </method>
    <method containingType=""C+_Closure$__1-0"" name=""_Lambda$__0"">
      <customDebugInfo>
        <encLocalSlotMap>
          <slot kind=""21"" offset=""142"" />
        </encLocalSlotMap>
      </customDebugInfo>
    </method>
  </methods>
</symbols>
", options:=PdbValidationOptions.ExcludeDocuments Or PdbValidationOptions.ExcludeSequencePoints Or PdbValidationOptions.ExcludeNamespaces Or PdbValidationOptions.ExcludeScopes,
   format:=DebugInformationFormat.PortablePdb)

                CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "C", "VB$StateMachine_1_M", "_Closure$__1-0")

                Dim diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, m0, m1, GetSyntaxMapFromMarkers(source0, source1))))

                ' Notice that we reused field "$VB$ResumableLocal_$VB$Closure_$0" (there is no "$VB$ResumableLocal_$VB$Closure_$1"), which stores the closure pointer.
                diff1.VerifySynthesizedMembers(
                    "C: {VB$StateMachine_1_M, _Closure$__1-0}",
                    "C.VB$StateMachine_1_M: {$State, $Builder, $VB$ResumableLocal_$VB$Closure_$0, $A0, MoveNext, System.Runtime.CompilerServices.IAsyncStateMachine.SetStateMachine}",
                    "C._Closure$__1-0: {_Lambda$__0}")
            End Using
        End Sub

        <Fact>
        Public Sub LiftedWithStatementVariable()
            Dim source0 = MarkedSource("
Imports System
Imports System.Threading.Tasks
Class C
    Private X As Integer = 1
    Private Y As Integer = 2

    Shared Async Function M() As Task
        <N:0>With New C()</N:0>
            <N:1>Await G()</N:1>
            Console.Write(.X)
        End With
    End Function

    Shared Function G() As Task(Of Integer)
        Return Task.FromResult(1)
    End Function
End Class")

            Dim source1 = MarkedSource("
Imports System
Imports System.Threading.Tasks
Class C
    Private X As Integer = 1
    Private Y As Integer = 2

    Shared Async Function M() As Task
        <N:0>With New C()</N:0>
            <N:1>Await G()</N:1>
            Console.Write(.Y)
        End With
    End Function

    Shared Function G() As Task(Of Integer)
        Return Task.FromResult(1)
    End Function
End Class")
            Dim compilation0 = CreateCompilationWithMscorlib45AndVBRuntime({source0.Tree}, options:=ComSafeDebugDll)
            Dim compilation1 = compilation0.WithSource(source1.Tree)

            Dim m0 = compilation0.GetMember(Of MethodSymbol)("C.M")
            Dim m1 = compilation1.GetMember(Of MethodSymbol)("C.M")

            Dim v0 = CompileAndVerify(compilation0)
            Using md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)

                Dim reader0 = md0.MetadataReader
                Dim generation0 = CreateInitialBaseline(compilation0, md0, AddressOf v0.CreateSymReader().GetEncMethodDebugInfo)

                ' Notice encLocalSlotMap CDI on both M and MoveNext methods.
                ' The former is used to calculate mapping for variables lifted to fields of the state machine,
                ' the latter is used to map local variable slots in the MoveNext method.
                ' Here, the variable lifted to the state machine field is the With statement storage.
                v0.VerifyPdb("
<symbols>
  <methods>
    <method containingType=""C"" name=""M"">
      <customDebugInfo>
        <encLocalSlotMap>
          <slot kind=""10"" offset=""0"" />
        </encLocalSlotMap>
        <encStateMachineStateMap>
          <state number=""0"" offset=""37"" />
        </encStateMachineStateMap>
      </customDebugInfo>
    </method>
    <method containingType=""C"" name=""G"">
      <customDebugInfo>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""-1"" />
        </encLocalSlotMap>
      </customDebugInfo>
    </method>
    <method containingType=""C+VB$StateMachine_3_M"" name=""MoveNext"">
      <customDebugInfo>
        <encLocalSlotMap>
          <slot kind=""27"" offset=""-1"" />
          <slot kind=""33"" offset=""37"" />
          <slot kind=""temp"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <asyncInfo>
        <kickoffMethod declaringType=""C"" methodName=""M"" />
        <await yield=""0x38"" resume=""0x56"" declaringType=""C+VB$StateMachine_3_M"" methodName=""MoveNext"" />
      </asyncInfo>
    </method>
  </methods>
</symbols>
", options:=PdbValidationOptions.ExcludeDocuments Or PdbValidationOptions.ExcludeSequencePoints Or PdbValidationOptions.ExcludeNamespaces Or PdbValidationOptions.ExcludeScopes,
   format:=DebugInformationFormat.PortablePdb)

                CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "C", "VB$StateMachine_3_M")
                CheckNames(reader0, reader0.GetFieldDefNames(), "X", "Y", "$State", "$Builder", "$W0", "$A0")

                Dim diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    SemanticEdit.Create(SemanticEditKind.Update, m0, m1, GetSyntaxMapFromMarkers(source0, source1))))

                ' Notice that we reused field "$W0" (there is no "$W1"), which stores the closure pointer.
                diff1.VerifySynthesizedMembers(
                    "C: {VB$StateMachine_3_M}",
                    "C.VB$StateMachine_3_M: {$State, $Builder, $W0, $A0, MoveNext, System.Runtime.CompilerServices.IAsyncStateMachine.SetStateMachine}")
            End Using
        End Sub

        <Fact, WorkItem(1170899, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1170899")>
        Public Sub HoistedAnonymousTypes1()
            Dim source0 = MarkedSource("
Imports System
Imports System.Collections.Generic

Class C
    Iterator Function F() As IEnumerable(Of Integer)
        Dim <N:0>x</N:0> = New With {.A = 1}
        <N:1>Yield 1</N:1>
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
        <N:1>Yield 1</N:1>
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
        <N:1>Yield 1</N:1>
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

            Dim generation0 = CreateInitialBaseline(compilation0, md0, AddressOf v0.CreateSymReader().GetEncMethodDebugInfo)

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
                    New SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))))

            diff1.VerifySynthesizedMembers(
                "C: {VB$StateMachine_1_F}",
                "C.VB$StateMachine_1_F: {$State, $Current, $InitialThreadId, $VB$Me, $VB$ResumableLocal_x$0, Dispose, MoveNext, GetEnumerator, IEnumerable.GetEnumerator, get_Current, Reset, IEnumerator.get_Current, Current, IEnumerator.Current}")

            diff1.VerifyIL("C.VB$StateMachine_1_F.MoveNext()", baselineIL.Replace("<<VALUE>>", "2"))

            Dim diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    New SemanticEdit(SemanticEditKind.Update, f1, f2, GetSyntaxMapFromMarkers(source1, source2))))

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
        <N:1>Yield 1</N:1>
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
        <N:1>Yield 1</N:1>
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
        <N:1>Yield 1</N:1>
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

            Dim generation0 = CreateInitialBaseline(compilation0, md0, AddressOf v0.CreateSymReader().GetEncMethodDebugInfo)

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
                    New SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))))

            diff1.VerifySynthesizedMembers(
                "C: {VB$StateMachine_1_F}",
                "C.VB$StateMachine_1_F: {$State, $Current, $InitialThreadId, $VB$Me, $VB$ResumableLocal_x$0, Dispose, MoveNext, GetEnumerator, IEnumerable.GetEnumerator, get_Current, Reset, IEnumerator.get_Current, Current, IEnumerator.Current}")

            diff1.VerifyIL("C.VB$StateMachine_1_F.MoveNext()", baselineIL.Replace("<<VALUE>>", "2"))

            Dim diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    New SemanticEdit(SemanticEditKind.Update, f1, f2, GetSyntaxMapFromMarkers(source1, source2))))

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

            Dim generation0 = CreateInitialBaseline(compilation0, md0, AddressOf v0.CreateSymReader().GetEncMethodDebugInfo)

            ' y is added 
            Dim diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    New SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))))

            diff1.VerifySynthesizedMembers(
                "C: {VB$StateMachine_1_F, _Closure$__}",
                "C._Closure$__: {$I1-0, _Lambda$__1-0}",
                "C.VB$StateMachine_1_F: {$State, $Builder, $VB$ResumableLocal_x$0, $VB$ResumableLocal_y$1, $A0, MoveNext, System.Runtime.CompilerServices.IAsyncStateMachine.SetStateMachine}")

            ' y is removed
            Dim diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    New SemanticEdit(SemanticEditKind.Update, f1, f2, GetSyntaxMapFromMarkers(source1, source2))))

            ' Synthesized members collection still includes y field since members are only added to it and never deleted.
            ' The corresponding CLR field is also present.
            diff2.VerifySynthesizedMembers(
                "C: {VB$StateMachine_1_F, _Closure$__}",
                "C._Closure$__: {$I1-0, _Lambda$__1-0}",
                "C.VB$StateMachine_1_F: {$State, $Builder, $VB$ResumableLocal_x$0, $A0, MoveNext, System.Runtime.CompilerServices.IAsyncStateMachine.SetStateMachine, $VB$ResumableLocal_y$1}")

            ' y is added and a new slot index is allocated for it
            Dim diff3 = compilation3.EmitDifference(
                diff2.NextGeneration,
                ImmutableArray.Create(
                    New SemanticEdit(SemanticEditKind.Update, f2, f3, GetSyntaxMapFromMarkers(source2, source3))))

            diff3.VerifySynthesizedMembers(
                "C: {VB$StateMachine_1_F, _Closure$__}",
                "C._Closure$__: {$I1-0, _Lambda$__1-0}",
                "C.VB$StateMachine_1_F: {$State, $Builder, $VB$ResumableLocal_x$0, $VB$ResumableLocal_y$2, $A0, MoveNext, System.Runtime.CompilerServices.IAsyncStateMachine.SetStateMachine, $VB$ResumableLocal_y$1}")

            ' y is removed
            Dim diff4 = compilation4.EmitDifference(
                diff3.NextGeneration,
                ImmutableArray.Create(
                    New SemanticEdit(SemanticEditKind.Update, f3, f4, GetSyntaxMapFromMarkers(source3, source4))))

            diff4.VerifySynthesizedMembers(
                "C: {VB$StateMachine_1_F, _Closure$__}",
                "C._Closure$__: {$I1-0, _Lambda$__1-0}",
                "C.VB$StateMachine_1_F: {$State, $Builder, $VB$ResumableLocal_x$0, $A0, MoveNext, System.Runtime.CompilerServices.IAsyncStateMachine.SetStateMachine, $VB$ResumableLocal_y$2, $VB$ResumableLocal_y$1}")

            ' y is added and a new slot index is allocated for it
            Dim diff5 = compilation5.EmitDifference(
                diff4.NextGeneration,
                ImmutableArray.Create(
                    New SemanticEdit(SemanticEditKind.Update, f4, f5, GetSyntaxMapFromMarkers(source4, source5))))

            diff5.VerifySynthesizedMembers(
                "C: {VB$StateMachine_1_F, _Closure$__}",
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
            Dim compilation0 = CreateCompilationWithMscorlib40({source0.Tree}, options:=ComSafeDebugDll)
            Dim compilation1 = compilation0.WithSource(source1.Tree)

            ' older versions of mscorlib don't contain IteratorStateMachineAttribute
            Assert.Null(compilation0.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_IteratorStateMachineAttribute__ctor))

            Dim v0 = CompileAndVerify(compilation0, verify:=Verification.Passes)
            Dim md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)

            Dim f0 = compilation0.GetMember(Of MethodSymbol)("C.F")
            Dim f1 = compilation1.GetMember(Of MethodSymbol)("C.F")

            Dim generation0 = CreateInitialBaseline(compilation0, md0, AddressOf v0.CreateSymReader().GetEncMethodDebugInfo)
            Dim diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))))

            diff1.EmitResult.Diagnostics.Verify(
                Diagnostic(ERRID.ERR_EncUpdateFailedMissingAttribute, "F").WithArguments("Public Function F() As IEnumerable(Of Integer)", "System.Runtime.CompilerServices.IteratorStateMachineAttribute").WithLocation(6, 30))
        End Sub

        <Fact>
        Public Sub BadIteratorStateMachineAttribute()
            Dim source0 = MarkedSource("
Imports System
Imports System.Collections.Generic

Namespace System.Runtime.CompilerServices
    Public Class IteratorStateMachineAttribute 
        Inherits Attribute
    End Class
End Namespace

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

Namespace System.Runtime.CompilerServices
    Public Class IteratorStateMachineAttribute 
        Inherits Attribute
    End Class
End Namespace

Class C
    Public Iterator Function F() As IEnumerable(Of Integer)
        Dim <N:0>a</N:0> As Integer = 1
        <N:1>Yield 1</N:1>
        Console.WriteLine(a)
    End Function
End Class
")
            Dim compilation0 = CreateCompilationWithMscorlib40({source0.Tree}, options:=ComSafeDebugDll)
            Dim compilation1 = compilation0.WithSource(source1.Tree)

            ' the ctor is missing a parameter
            Assert.Null(compilation0.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_IteratorStateMachineAttribute__ctor))

            Dim v0 = CompileAndVerify(compilation0, verify:=Verification.Passes)
            Dim md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)
            Dim f0 = compilation0.GetMember(Of MethodSymbol)("C.F")
            Dim f1 = compilation1.GetMember(Of MethodSymbol)("C.F")

            Dim generation0 = CreateInitialBaseline(compilation0, md0, AddressOf v0.CreateSymReader().GetEncMethodDebugInfo)
            Dim diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))))

            diff1.EmitResult.Diagnostics.Verify(
                Diagnostic(ERRID.ERR_EncUpdateFailedMissingAttribute, "F").WithArguments("Public Function F() As IEnumerable(Of Integer)", "System.Runtime.CompilerServices.IteratorStateMachineAttribute").WithLocation(12, 30))
        End Sub

        <Fact>
        Public Sub AddedIteratorStateMachineAttribute()
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

Namespace System.Runtime.CompilerServices
    Public Class IteratorStateMachineAttribute 
        Inherits Attribute

        Sub New(type As Type)
        End Sub
    End Class
End Namespace

Class C
    Public Iterator Function F() As IEnumerable(Of Integer)
        Dim <N:0>a</N:0> As Integer = 1
        <N:1>Yield 1</N:1>
        Console.WriteLine(a)
    End Function
End Class
")
            Dim compilation0 = CreateCompilationWithMscorlib40({source0.Tree}, options:=ComSafeDebugDll)
            Dim compilation1 = compilation0.WithSource(source1.Tree)

            ' older versions of mscorlib don't contain IteratorStateMachineAttribute
            Assert.Null(compilation0.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_IteratorStateMachineAttribute__ctor))

            Dim v0 = CompileAndVerify(compilation0, verify:=Verification.Passes)
            Dim md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)
            Dim f0 = compilation0.GetMember(Of MethodSymbol)("C.F")
            Dim f1 = compilation1.GetMember(Of MethodSymbol)("C.F")
            Dim ism1 = compilation1.GetMember(Of TypeSymbol)("System.Runtime.CompilerServices.IteratorStateMachineAttribute")

            Dim generation0 = CreateInitialBaseline(compilation0, md0, AddressOf v0.CreateSymReader().GetEncMethodDebugInfo)
            Dim diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    New SemanticEdit(SemanticEditKind.Insert, Nothing, ism1),
                    New SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))))

            diff1.EmitResult.Diagnostics.Verify()
        End Sub

        <Fact>
        Public Sub SourceIteratorStateMachineAttribute()
            Dim source0 = MarkedSource("
Imports System
Imports System.Collections.Generic

Namespace System.Runtime.CompilerServices
    Public Class IteratorStateMachineAttribute 
        Inherits Attribute

        Sub New(type As Type)
        End Sub
    End Class
End Namespace

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

Namespace System.Runtime.CompilerServices
    Public Class IteratorStateMachineAttribute 
        Inherits Attribute

        Sub New(type As Type)
        End Sub
    End Class
End Namespace

Class C
    Public Iterator Function F() As IEnumerable(Of Integer)
        Dim <N:0>a</N:0> As Integer = 1
        <N:1>Yield 1</N:1>
        Console.WriteLine(a)
    End Function
End Class
")
            Dim compilation0 = CreateCompilationWithMscorlib40({source0.Tree}, options:=ComSafeDebugDll)
            Dim compilation1 = compilation0.WithSource(source1.Tree)

            Dim v0 = CompileAndVerify(compilation0, verify:=Verification.Passes)
            Dim md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)
            Dim f0 = compilation0.GetMember(Of MethodSymbol)("C.F")
            Dim f1 = compilation1.GetMember(Of MethodSymbol)("C.F")

            Dim generation0 = CreateInitialBaseline(compilation0, md0, AddressOf v0.CreateSymReader().GetEncMethodDebugInfo)
            Dim diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))))

            diff1.EmitResult.Diagnostics.Verify()
        End Sub

        Const AsyncHelpers = "
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

        <Fact, WorkItem(9119, "https://github.com/dotnet/roslyn/issues/9119")>
        Public Sub MissingAsyncStateMachineAttribute()
            Dim source0 = MarkedSource(AsyncHelpers & "
Class C
    Public Async Function F() As Task(Of Integer)
        Dim <N:0>a</N:0> As Integer = 0
        Await New Task()
        Return a
    End Function
End Class
")
            Dim source1 = MarkedSource(AsyncHelpers & "
Class C
    Public Async Function F() As Task(Of Integer)
        Dim <N:0>a</N:0> As Integer = 1
        Await  New Task()
        Return a
    End Function
End Class
")
            Dim compilation0 = CompilationUtils.CreateEmptyCompilation({source0.Tree}, {TestReferences.NetFx.Minimal.mincorlib, TestReferences.NetFx.Minimal.minasync}, options:=ComSafeDebugDll)
            Dim compilation1 = compilation0.WithSource(source1.Tree)

            Assert.Null(compilation0.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_AsyncStateMachineAttribute__ctor))

            Dim v0 = CompileAndVerify(compilation0, verify:=Verification.FailsPEVerify)
            Dim md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)

            Dim f0 = compilation0.GetMember(Of MethodSymbol)("C.F")
            Dim f1 = compilation1.GetMember(Of MethodSymbol)("C.F")

            Dim generation0 = CreateInitialBaseline(compilation0, md0, AddressOf v0.CreateSymReader().GetEncMethodDebugInfo)
            Dim diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))))

            diff1.EmitResult.Diagnostics.Verify(
                Diagnostic(ERRID.ERR_EncUpdateFailedMissingAttribute, "F").WithArguments("Public Function F() As Task(Of Integer)", "System.Runtime.CompilerServices.AsyncStateMachineAttribute").WithLocation(15, 27))
        End Sub

        <Fact, WorkItem(10190, "https://github.com/dotnet/roslyn/issues/10190")>
        Public Sub NonAsyncToAsync()
            Dim source0 = MarkedSource(AsyncHelpers & "
Class C
    Public Function F() As Task(Of Integer)
        Dim <N:0>a</N:0> As Integer = 0
        <N:1>Return Task.FromResult(a)</N:1>
    End Function
End Class
")
            Dim source1 = MarkedSource(AsyncHelpers & "
Class C
    Public Async Function F() As Task(Of Integer)
        Dim <N:0>a</N:0> As Integer = 1
        <N:1>Return Await Task.FromResult(a)</N:1>
    End Function
End Class
")
            Dim compilation0 = CompilationUtils.CreateEmptyCompilation({source0.Tree}, {TestMetadata.Net451.mscorlib}, options:=ComSafeDebugDll)
            Dim compilation1 = compilation0.WithSource(source1.Tree)

            Assert.NotNull(compilation0.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_AsyncStateMachineAttribute__ctor))

            Dim v0 = CompileAndVerify(compilation0, verify:=Verification.Passes)
            Dim md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)

            Dim f0 = compilation0.GetMember(Of MethodSymbol)("C.F")
            Dim f1 = compilation1.GetMember(Of MethodSymbol)("C.F")

            Dim generation0 = CreateInitialBaseline(compilation0, md0, AddressOf v0.CreateSymReader().GetEncMethodDebugInfo)
            Dim diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))))

            diff1.EmitResult.Diagnostics.Verify()
        End Sub

        <Fact>
        Public Sub NonAsyncToAsync_MissingAttribute()
            Dim source0 = MarkedSource(AsyncHelpers & "
Class C
    Public Function F() As Task(Of Integer)
        Dim <N:0>a</N:0> As Integer = 0
        a = a + 1
        <N:1>Return New Task(Of Integer)()</N:1>
    End Function
End Class
")
            Dim source1 = MarkedSource(AsyncHelpers & "
Class C
    Public Async Function F() As Task(Of Integer)
        Dim <N:0>a</N:0> As Integer = 1
        a = a + 1
        <N:1>Return Await New Task(Of Integer)()</N:1>
    End Function
End Class
")
            Dim compilation0 = CompilationUtils.CreateEmptyCompilation({source0.Tree}, {TestReferences.NetFx.Minimal.mincorlib, TestReferences.NetFx.Minimal.minasync}, options:=ComSafeDebugDll)
            Dim compilation1 = compilation0.WithSource(source1.Tree)

            Assert.Null(compilation0.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_AsyncStateMachineAttribute__ctor))

            Dim v0 = CompileAndVerify(compilation0, verify:=Verification.FailsPEVerify)
            Dim md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)

            Dim f0 = compilation0.GetMember(Of MethodSymbol)("C.F")
            Dim f1 = compilation1.GetMember(Of MethodSymbol)("C.F")

            Dim generation0 = CreateInitialBaseline(compilation0, md0, AddressOf v0.CreateSymReader().GetEncMethodDebugInfo)
            Dim diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))))

            diff1.EmitResult.Diagnostics.Verify(
                Diagnostic(ERRID.ERR_EncUpdateFailedMissingAttribute, "F").WithArguments("Public Function F() As Task(Of Integer)", "System.Runtime.CompilerServices.AsyncStateMachineAttribute").WithLocation(15, 27))
        End Sub

        <Fact>
        Public Sub NonIteratorToIterator_MissingAttribute()
            Dim source0 = MarkedSource("
Imports System.Collections.Generic

Class C
    Public Function F() As IEnumerable(Of Integer)
        Dim <N:0>a</N:0> As Integer = 0
        <N:1>Return { 0 }</N:1>
    End Function
End Class
")
            Dim source1 = MarkedSource("
Imports System.Collections.Generic

Class C
    Public Iterator Function F() As IEnumerable(Of Integer)
        Dim <N:0>a</N:0> As Integer = 1
        <N:1>Yield a</N:1>
    End Function
End Class
")
            Dim compilation0 = CreateCompilationWithMscorlib40({source0.Tree}, options:=ComSafeDebugDll)
            Dim compilation1 = compilation0.WithSource(source1.Tree)

            Dim v0 = CompileAndVerify(compilation0, verify:=Verification.Passes)
            Dim md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)
            Dim f0 = compilation0.GetMember(Of MethodSymbol)("C.F")
            Dim f1 = compilation1.GetMember(Of MethodSymbol)("C.F")

            Dim generation0 = CreateInitialBaseline(compilation0, md0, AddressOf v0.CreateSymReader().GetEncMethodDebugInfo)
            Dim diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1))))

            diff1.EmitResult.Diagnostics.Verify(
                Diagnostic(ERRID.ERR_EncUpdateFailedMissingAttribute, "F").WithArguments("Public Function F() As IEnumerable(Of Integer)", "System.Runtime.CompilerServices.IteratorStateMachineAttribute").WithLocation(5, 30))
        End Sub
    End Class
End Namespace
