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
                    <entryPoint declaringType="C1" methodName="Main" parameterNames=""/>
                    <methods>
                        <method containingType="C1" name="Main" parameterNames="">
                            <sequencepoints total="13">
                                <entry il_offset="0x0" start_row="10" start_column="5" end_row="10" end_column="29" file_ref="0"/>
                                <entry il_offset="0x1" start_row="11" start_column="9" end_row="11" end_column="12" file_ref="0"/>
                                <entry il_offset="0x2" start_row="12" start_column="17" end_row="12" end_column="37" file_ref="0"/>
                                <entry il_offset="0xd" start_row="13" start_column="13" end_row="13" end_column="35" file_ref="0"/>
                                <entry il_offset="0xe" start_row="13" start_column="22" end_row="13" end_column="35" file_ref="0"/>
                                <entry il_offset="0x21" start_row="14" start_column="21" end_row="14" end_column="38" file_ref="0"/>
                                <entry il_offset="0x24" start_row="15" start_column="17" end_row="15" end_column="38" file_ref="0"/>
                                <entry il_offset="0x2a" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                                <entry il_offset="0x3b" start_row="17" start_column="13" end_row="17" end_column="25" file_ref="0"/>
                                <entry il_offset="0x3d" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                                <entry il_offset="0x42" start_row="18" start_column="9" end_row="18" end_column="14" file_ref="0"/>
                                <entry il_offset="0x4a" start_row="19" start_column="9" end_row="19" end_column="16" file_ref="0"/>
                                <entry il_offset="0x4b" start_row="20" start_column="5" end_row="20" end_column="12" file_ref="0"/>
                            </sequencepoints>
                            <locals>
                                <local name="lock" il_index="0" il_start="0x2" il_end="0x3c" attributes="0"/>
                                <local name="VB$Lock" il_index="1" il_start="0xd" il_end="0x3c" attributes="1"/>
                                <local name="VB$LockTaken" il_index="2" il_start="0xd" il_end="0x3c" attributes="1"/>
                                <local name="x" il_index="3" il_start="0x21" il_end="0x29" attributes="0"/>
                            </locals>
                            <scope startOffset="0x0" endOffset="0x4c">
                                <importsforward declaringType="C1" methodName="Something" parameterNames="x"/>
                                <scope startOffset="0x2" endOffset="0x3c">
                                    <local name="lock" il_index="0" il_start="0x2" il_end="0x3c" attributes="0"/>
                                    <scope startOffset="0xd" endOffset="0x3c">
                                        <local name="VB$Lock" il_index="1" il_start="0xd" il_end="0x3c" attributes="1"/>
                                        <local name="VB$LockTaken" il_index="2" il_start="0xd" il_end="0x3c" attributes="1"/>
                                        <scope startOffset="0x21" endOffset="0x29">
                                            <local name="x" il_index="3" il_start="0x21" il_end="0x29" attributes="0"/>
                                        </scope>
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