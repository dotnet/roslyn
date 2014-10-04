' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.PDB
    Public Class PDBObjectInitializerTests
        Inherits BasicTestBase

        <Fact>
        Public Sub ObjectInitializerAsRefTypeEquals()
            Dim source =
<compilation>
    <file>
Option Strict On
Option Infer Off
Option Explicit Off

Imports System
Imports System.Collections.Generic

Public Class RefType
    Public Field1 as Integer
    Public Field2 as Integer
End Class

Class C1
    Public Shared Sub Main()
        Dim inst as RefType = new RefType() With {.Field1 = 23, .Field2 = 42}
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
            <customDebugInfo version="4" count="1">
                <encLocalSlotMap version="4" kind="EditAndContinueLocalSlotMap" size="12">
                    <slot kind="0" offset="4"/>
                    <slot kind="temp"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencepoints total="3">
                <entry il_offset="0x0" start_row="14" start_column="5" end_row="14" end_column="29" file_ref="0"/>
                <entry il_offset="0x1" start_row="15" start_column="13" end_row="15" end_column="78" file_ref="0"/>
                <entry il_offset="0x19" start_row="16" start_column="5" end_row="16" end_column="12" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="inst" il_index="0" il_start="0x0" il_end="0x1a" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x1a">
                <namespace name="System" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="inst" il_index="0" il_start="0x0" il_end="0x1a" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>

            PDBTests.AssertXmlEqual(expected, actual)
        End Sub

        <Fact>
        Public Sub ObjectInitializerAsNewRefType()
            Dim source =
<compilation>
    <file>
Option Strict On
Option Infer Off
Option Explicit Off

Imports System
Imports System.Collections.Generic

Public Class RefType
    Public Field1 as Integer
    Public Field2 as Integer
End Class

Class C1
    Public Shared Sub Main()
        Dim inst as new RefType() With {.Field1 = 23, .Field2 = 42}
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
            <customDebugInfo version="4" count="1">
                <encLocalSlotMap version="4" kind="EditAndContinueLocalSlotMap" size="12">
                    <slot kind="0" offset="4"/>
                    <slot kind="temp"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencepoints total="3">
                <entry il_offset="0x0" start_row="14" start_column="5" end_row="14" end_column="29" file_ref="0"/>
                <entry il_offset="0x1" start_row="15" start_column="13" end_row="15" end_column="68" file_ref="0"/>
                <entry il_offset="0x19" start_row="16" start_column="5" end_row="16" end_column="12" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="inst" il_index="0" il_start="0x0" il_end="0x1a" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x1a">
                <namespace name="System" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="inst" il_index="0" il_start="0x0" il_end="0x1a" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>

            PDBTests.AssertXmlEqual(expected, actual)
        End Sub

        <Fact>
        Public Sub ObjectInitializerNested()
            Dim source =
<compilation>
    <file>
Option Strict On
Option Infer Off
Option Explicit Off

Imports System
Imports System.Collections.Generic

Public Class RefType
    Public Field1 as RefType
End Class

Class C1
    Public Shared Sub Main()
        Dim inst as new RefType() With {.Field1 = new RefType() With {.Field1 = nothing}}
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
            <customDebugInfo version="4" count="1">
                <encLocalSlotMap version="4" kind="EditAndContinueLocalSlotMap" size="12">
                    <slot kind="0" offset="4"/>
                    <slot kind="temp"/>
                    <slot kind="temp"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencepoints total="3">
                <entry il_offset="0x0" start_row="13" start_column="5" end_row="13" end_column="29" file_ref="0"/>
                <entry il_offset="0x1" start_row="14" start_column="13" end_row="14" end_column="90" file_ref="0"/>
                <entry il_offset="0x1d" start_row="15" start_column="5" end_row="15" end_column="12" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="inst" il_index="0" il_start="0x0" il_end="0x1e" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x1e">
                <namespace name="System" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="inst" il_index="0" il_start="0x0" il_end="0x1e" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>

            PDBTests.AssertXmlEqual(expected, actual)
        End Sub

        <Fact>
        Public Sub ObjectInitializerAsNewRefTypeMultipleVariables()
            Dim source =
<compilation>
    <file>
Option Strict On
Option Infer Off
Option Explicit Off

Imports System
Imports System.Collections.Generic

Public Class RefType
    Public Field1 as Integer
    Public Field2 as Integer
End Class

Class C1
    Public Shared Sub Main()
        Dim inst1, inst2 as new RefType() With {.Field1 = 23, .Field2 = 42}
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
            <customDebugInfo version="4" count="1">
                <encLocalSlotMap version="4" kind="EditAndContinueLocalSlotMap" size="16">
                    <slot kind="0" offset="4"/>
                    <slot kind="0" offset="11"/>
                    <slot kind="temp"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencepoints total="4">
                <entry il_offset="0x0" start_row="14" start_column="5" end_row="14" end_column="29" file_ref="0"/>
                <entry il_offset="0x1" start_row="15" start_column="13" end_row="15" end_column="18" file_ref="0"/>
                <entry il_offset="0x19" start_row="15" start_column="20" end_row="15" end_column="25" file_ref="0"/>
                <entry il_offset="0x31" start_row="16" start_column="5" end_row="16" end_column="12" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="inst1" il_index="0" il_start="0x0" il_end="0x32" attributes="0"/>
                <local name="inst2" il_index="1" il_start="0x0" il_end="0x32" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x32">
                <namespace name="System" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="inst1" il_index="0" il_start="0x0" il_end="0x32" attributes="0"/>
                <local name="inst2" il_index="1" il_start="0x0" il_end="0x32" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>

            PDBTests.AssertXmlEqual(expected, actual)
        End Sub

    End Class
End Namespace
