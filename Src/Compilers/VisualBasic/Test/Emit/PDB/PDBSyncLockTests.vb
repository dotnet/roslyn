' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.PDB

    Public Class PDBSyncLockTests
        Inherits BasicTestBase

        <Fact()>
        Public Sub SyncLockWithThrow()
            Dim source =
<compilation>
    <file>
Option Strict On

Imports System

Class C1
    Public Shared Function Something(x As Integer) As C1
        Return New C1()
    End Function

    Public Shared Sub Main()
        Try
            Dim lock As New Object()
            SyncLock Something(12)
                Dim x As Integer = 23
                Throw New exception()
                Console.WriteLine("Inside SyncLock.")
            End SyncLock
        Catch
        End Try
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
                    source,
                    TestOptions.DebugExe)

            Dim actual = PDBTests.GetPdbXml(compilation, "C1.Main")

            Dim expected =
<symbols>
    <entryPoint declaringType="C1" methodName="Main"/>
    <methods>
        <method containingType="C1" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="21"/>
                    <slot kind="3" offset="55"/>
                    <slot kind="2" offset="55"/>
                    <slot kind="0" offset="99"/>
                    <slot kind="temp"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="10" startColumn="5" endLine="10" endColumn="29" document="0"/>
                <entry offset="0x1" startLine="11" startColumn="9" endLine="11" endColumn="12" document="0"/>
                <entry offset="0x2" startLine="12" startColumn="17" endLine="12" endColumn="37" document="0"/>
                <entry offset="0xd" startLine="13" startColumn="13" endLine="13" endColumn="35" document="0"/>
                <entry offset="0xe" startLine="13" startColumn="22" endLine="13" endColumn="35" document="0"/>
                <entry offset="0x21" startLine="14" startColumn="21" endLine="14" endColumn="38" document="0"/>
                <entry offset="0x24" startLine="15" startColumn="17" endLine="15" endColumn="38" document="0"/>
                <entry offset="0x2a" hidden="true" document="0"/>
                <entry offset="0x3b" startLine="17" startColumn="13" endLine="17" endColumn="25" document="0"/>
                <entry offset="0x3d" hidden="true" document="0"/>
                <entry offset="0x42" startLine="18" startColumn="9" endLine="18" endColumn="14" document="0"/>
                <entry offset="0x4a" startLine="19" startColumn="9" endLine="19" endColumn="16" document="0"/>
                <entry offset="0x4b" startLine="20" startColumn="5" endLine="20" endColumn="12" document="0"/>
            </sequencePoints>
            <locals>
                <local name="lock" il_index="0" il_start="0x2" il_end="0x3c" attributes="0"/>
                <local name="x" il_index="3" il_start="0x21" il_end="0x29" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x4c">
                <importsforward declaringType="C1" methodName="Something" parameterNames="x"/>
                <scope startOffset="0x2" endOffset="0x3c">
                    <local name="lock" il_index="0" il_start="0x2" il_end="0x3c" attributes="0"/>
                    <scope startOffset="0x21" endOffset="0x29">
                        <local name="x" il_index="3" il_start="0x21" il_end="0x29" attributes="0"/>
                    </scope>
                </scope>
            </scope>
        </method>
    </methods>
</symbols>

            PDBTests.AssertXmlEqual(expected, actual)
        End Sub
    End Class

End Namespace