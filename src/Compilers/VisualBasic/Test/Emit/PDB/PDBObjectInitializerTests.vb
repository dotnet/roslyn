' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Test.Utilities

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

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(source, TestOptions.DebugExe)

            compilation.VerifyPdb("C1.Main",
<symbols>
    <entryPoint declaringType="C1" methodName="Main"/>
    <methods>
        <method containingType="C1" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="14" startColumn="5" endLine="14" endColumn="29"/>
                <entry offset="0x1" startLine="15" startColumn="13" endLine="15" endColumn="78"/>
                <entry offset="0x17" startLine="16" startColumn="5" endLine="16" endColumn="12"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x18">
                <namespace name="System" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="inst" il_index="0" il_start="0x0" il_end="0x18" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>)
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

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(source, TestOptions.DebugExe)
            compilation.VerifyPdb("C1.Main",
<symbols>
    <entryPoint declaringType="C1" methodName="Main"/>
    <methods>
        <method containingType="C1" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="14" startColumn="5" endLine="14" endColumn="29"/>
                <entry offset="0x1" startLine="15" startColumn="13" endLine="15" endColumn="68"/>
                <entry offset="0x17" startLine="16" startColumn="5" endLine="16" endColumn="12"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x18">
                <namespace name="System" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="inst" il_index="0" il_start="0x0" il_end="0x18" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>)
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

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(source, TestOptions.DebugExe)
            compilation.VerifyPdb("C1.Main",
<symbols>
    <entryPoint declaringType="C1" methodName="Main"/>
    <methods>
        <method containingType="C1" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="13" startColumn="5" endLine="13" endColumn="29"/>
                <entry offset="0x1" startLine="14" startColumn="13" endLine="14" endColumn="90"/>
                <entry offset="0x19" startLine="15" startColumn="5" endLine="15" endColumn="12"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x1a">
                <namespace name="System" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="inst" il_index="0" il_start="0x0" il_end="0x1a" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>)
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

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(source, TestOptions.DebugExe)
            compilation.VerifyPdb("C1.Main",
<symbols>
    <entryPoint declaringType="C1" methodName="Main"/>
    <methods>
        <method containingType="C1" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                    <slot kind="0" offset="11"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="14" startColumn="5" endLine="14" endColumn="29"/>
                <entry offset="0x1" startLine="15" startColumn="13" endLine="15" endColumn="18"/>
                <entry offset="0x17" startLine="15" startColumn="20" endLine="15" endColumn="25"/>
                <entry offset="0x2d" startLine="16" startColumn="5" endLine="16" endColumn="12"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x2e">
                <namespace name="System" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="inst1" il_index="0" il_start="0x0" il_end="0x2e" attributes="0"/>
                <local name="inst2" il_index="1" il_start="0x0" il_end="0x2e" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

    End Class
End Namespace
