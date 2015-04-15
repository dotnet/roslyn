' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.PDB
    Public Class PDBCollectionInitializerTests
        Inherits BasicTestBase

        <Fact>
        Public Sub CollectionInitializerAsCollTypeEquals()
            Dim source =
<compilation>
    <file>
Option Strict On
Option Infer Off
Option Explicit Off

Imports System
Imports System.Collections.Generic

Class C1
    Public Shared Sub Main()
        Dim aList1 As List(Of String) = New List(Of String)() From {"Hello", " ", "World"} 
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(source, TestOptions.DebugExe)

            compilation.VerifyPdb("C1.Main",
<symbols>
    <entryPoint declaringType="C1" methodName="Main"/>
    <methods>
        <method containingType="C1" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                    <slot kind="temp"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="9" startColumn="5" endLine="9" endColumn="29" document="0"/>
                <entry offset="0x1" startLine="10" startColumn="13" endLine="10" endColumn="91" document="0"/>
                <entry offset="0x2a" startLine="11" startColumn="5" endLine="11" endColumn="12" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x2b">
                <namespace name="System" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="aList1" il_index="0" il_start="0x0" il_end="0x2b" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <Fact>
        Public Sub CollectionInitializerAsNewCollType()
            Dim source =
<compilation>
    <file>
Option Strict On
Option Infer Off
Option Explicit Off

Imports System
Imports System.Collections.Generic

Class C1
    Public Shared Sub Main()
        Dim aList2 As New List(Of String)() From {"Hello", " ", "World"} 
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(source, TestOptions.DebugExe)

            compilation.VerifyPdb("C1.Main",
<symbols>
    <entryPoint declaringType="C1" methodName="Main"/>
    <methods>
        <method containingType="C1" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                    <slot kind="temp"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="9" startColumn="5" endLine="9" endColumn="29" document="0"/>
                <entry offset="0x1" startLine="10" startColumn="13" endLine="10" endColumn="73" document="0"/>
                <entry offset="0x2a" startLine="11" startColumn="5" endLine="11" endColumn="12" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x2b">
                <namespace name="System" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="aList2" il_index="0" il_start="0x0" il_end="0x2b" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <Fact>
        Public Sub CollectionInitializerNested()
            Dim source =
<compilation>
    <file>
Option Strict On
Option Infer Off
Option Explicit Off

Imports System
Imports System.Collections.Generic

Class C1
    Public Shared Sub Main()
        Dim aList1 As New List(Of List(Of String))() From {new List(Of String)() From {"Hello", "World"} } 
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(source, TestOptions.DebugExe)
            compilation.VerifyPdb("C1.Main",
<symbols>
    <entryPoint declaringType="C1" methodName="Main"/>
    <methods>
        <method containingType="C1" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                    <slot kind="temp"/>
                    <slot kind="temp"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="9" startColumn="5" endLine="9" endColumn="29" document="0"/>
                <entry offset="0x1" startLine="10" startColumn="13" endLine="10" endColumn="107" document="0"/>
                <entry offset="0x2c" startLine="11" startColumn="5" endLine="11" endColumn="12" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x2d">
                <namespace name="System" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="aList1" il_index="0" il_start="0x0" il_end="0x2d" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <Fact>
        Public Sub CollectionInitializerAsNewCollTypeMultipleVariables()
            Dim source =
<compilation>
    <file>
Option Strict On
Option Infer Off
Option Explicit Off

Imports System
Imports System.Collections.Generic

Class C1
    Public Shared Sub Main()
        Dim aList1, aList2 As New List(Of String)() From {"Hello", " ", "World"} 
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(source, TestOptions.DebugExe)
            compilation.VerifyPdb("C1.Main",
<symbols>
    <entryPoint declaringType="C1" methodName="Main"/>
    <methods>
        <method containingType="C1" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                    <slot kind="0" offset="12"/>
                    <slot kind="temp"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="9" startColumn="5" endLine="9" endColumn="29" document="0"/>
                <entry offset="0x1" startLine="10" startColumn="13" endLine="10" endColumn="19" document="0"/>
                <entry offset="0x2a" startLine="10" startColumn="21" endLine="10" endColumn="27" document="0"/>
                <entry offset="0x53" startLine="11" startColumn="5" endLine="11" endColumn="12" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x54">
                <namespace name="System" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="aList1" il_index="0" il_start="0x0" il_end="0x54" attributes="0"/>
                <local name="aList2" il_index="1" il_start="0x0" il_end="0x54" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

    End Class
End Namespace
