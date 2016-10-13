' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.PDB

    Public Class PDBTupleTests
        Inherits BasicTestBase

        <Fact>
        Public Sub Local()
            Dim source =
<compilation>
    <file><![CDATA[
Class C
    Shared Sub F()
        Dim t As (A As Integer, B As Integer, (C As Integer, Integer), Integer, Integer, G As Integer, H As Integer, I As Integer) = (1, 2, (3, 4), 5, 6, 7, 8, 9)
    End Sub
End Class
]]></file>
</compilation>
            Dim comp = CreateCompilationWithMscorlib(source, references:={ValueTupleRef}, options:=TestOptions.DebugDll)
            comp.VerifyPdb("C.F",
<symbols>
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
                <entry offset="0x0" startLine="2" startColumn="5" endLine="2" endColumn="19"/>
                <entry offset="0x1" startLine="3" startColumn="13" endLine="3" endColumn="163"/>
                <entry offset="0x1b" startLine="4" startColumn="5" endLine="4" endColumn="12"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x1c">
                <currentnamespace name=""/>
                <local name="t" il_index="0" il_start="0x0" il_end="0x1c" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

    End Class

End Namespace
