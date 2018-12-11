' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.PDB

    Public Class PDBTupleTests
        Inherits BasicTestBase

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub Local()
            Dim source = "
Class C
    Shared Sub F()
        Dim t As (A As Integer, B As Integer, (C As Integer, Integer), Integer, Integer, G As Integer, H As Integer, I As Integer) = (1, 2, (3, 4), 5, 6, 7, 8, 9)
    End Sub
End Class
"
            Dim comp = CreateCompilationWithMscorlib40(source, references:={ValueTupleRef, SystemRuntimeFacadeRef}, options:=TestOptions.DebugDll)
            comp.VerifyPdb("C.F",
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <methods>
        <method containingType="C" name="F">
            <customDebugInfo>
                <tupleElementNames>
                    <local elementNames="|A|B||||G|H|I|C||" slotIndex="0" localName="t" scopeStart="0x0" scopeEnd="0x0"/>
                </tupleElementNames>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="3" startColumn="5" endLine="3" endColumn="19" document="1"/>
                <entry offset="0x1" startLine="4" startColumn="13" endLine="4" endColumn="163" document="1"/>
                <entry offset="0x1c" startLine="5" startColumn="5" endLine="5" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x1d">
                <currentnamespace name=""/>
                <local name="t" il_index="0" il_start="0x0" il_end="0x1d" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(17947, "https://github.com/dotnet/roslyn/issues/17947")>
        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub VariablesAndConstantsInUnreachableCode()
            Dim source = "
Imports System
Imports System.Collections.Generic

Class C(Of T)
    Enum E
        A
    End Enum

    Sub F()
        Dim v1 As C(Of (a As Integer, b As Integer)).E = Nothing
        Const c1 As C(Of (a As Integer, b As Integer)).E = Nothing

        Throw New Exception()

        Dim v2 As C(Of (a As Integer, b As Integer)).E = Nothing
        Const c2 As C(Of (a As Integer, b As Integer)).E = Nothing

        Do
            Dim v3 As C(Of (a As Integer, b As Integer)).E = Nothing
            Const c3 As C(Of (a As Integer, b As Integer)).E = Nothing
        Loop
    End Sub
End Class
"
            Dim c = CreateCompilationWithMscorlib40(source, references:={ValueTupleRef, SystemRuntimeFacadeRef}, options:=TestOptions.DebugDll)

            Dim v = CompileAndVerify(c)
            v.VerifyIL("C(Of T).F()", "
{
  // Code size        9 (0x9)
  .maxstack  1
  .locals init (C(Of (a As Integer, b As Integer)).E V_0, //v1
                C(Of (a As Integer, b As Integer)).E V_1, //v2
                C(Of (a As Integer, b As Integer)).E V_2) //v3
  IL_0000:  nop
  IL_0001:  ldc.i4.0
  IL_0002:  stloc.0
  IL_0003:  newobj     ""Sub System.Exception..ctor()""
  IL_0008:  throw
}
")

            c.VerifyPdb(
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <methods>
        <method containingType="C`1" name="F">
            <customDebugInfo>
                <tupleElementNames>
                    <local elementNames="|a|b" slotIndex="0" localName="v1" scopeStart="0x0" scopeEnd="0x0"/>
                    <local elementNames="|a|b" slotIndex="1" localName="v2" scopeStart="0x0" scopeEnd="0x0"/>
                    <local elementNames="|a|b" slotIndex="-1" localName="c1" scopeStart="0x0" scopeEnd="0x9"/>
                    <local elementNames="|a|b" slotIndex="-1" localName="c2" scopeStart="0x0" scopeEnd="0x9"/>
                </tupleElementNames>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                    <slot kind="0" offset="173"/>
                    <slot kind="0" offset="325"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="10" startColumn="5" endLine="10" endColumn="12" document="1"/>
                <entry offset="0x1" startLine="11" startColumn="13" endLine="11" endColumn="65" document="1"/>
                <entry offset="0x3" startLine="14" startColumn="9" endLine="14" endColumn="30" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x9">
                <namespace name="System" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="v1" il_index="0" il_start="0x0" il_end="0x9" attributes="0"/>
                <local name="v2" il_index="1" il_start="0x0" il_end="0x9" attributes="0"/>
                <constant name="c1" value="0" signature="E{System.ValueTuple`2{Int32, Int32}}"/>
                <constant name="c2" value="0" signature="E{System.ValueTuple`2{Int32, Int32}}"/>
            </scope>
        </method>
    </methods>
</symbols>
            )
        End Sub

    End Class

End Namespace
