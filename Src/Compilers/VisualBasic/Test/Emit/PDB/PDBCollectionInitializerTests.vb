' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
                    source,
                    TestOptions.DebugExe)

            Dim actual = PDBTests.GetPdbXml(compilation, "C1.Main")

            Dim expected = <symbols>
                               <entryPoint declaringType="C1" methodName="Main" parameterNames=""/>
                               <methods>
                                   <method containingType="C1" name="Main" parameterNames="">
                                       <sequencepoints total="3">
                                           <entry il_offset="0x0" start_row="9" start_column="5" end_row="9" end_column="29" file_ref="0"/>
                                           <entry il_offset="0x1" start_row="10" start_column="13" end_row="10" end_column="91" file_ref="0"/>
                                           <entry il_offset="0x2a" start_row="11" start_column="5" end_row="11" end_column="12" file_ref="0"/>
                                       </sequencepoints>
                                       <locals>
                                           <local name="aList1" il_index="0" il_start="0x0" il_end="0x2b" attributes="0"/>
                                       </locals>
                                       <scope startOffset="0x0" endOffset="0x2b">
                                           <namespace name="System" importlevel="file"/>
                                           <namespace name="System.Collections.Generic" importlevel="file"/>
                                           <currentnamespace name=""/>
                                           <local name="aList1" il_index="0" il_start="0x0" il_end="0x2b" attributes="0"/>
                                       </scope>
                                   </method>
                               </methods>
                           </symbols>

            PDBTests.AssertXmlEqual(expected, actual)
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

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
                    source,
                    TestOptions.DebugExe)

            Dim actual = PDBTests.GetPdbXml(compilation, "C1.Main")

            Dim expected = <symbols>
                               <entryPoint declaringType="C1" methodName="Main" parameterNames=""/>
                               <methods>
                                   <method containingType="C1" name="Main" parameterNames="">
                                       <sequencepoints total="3">
                                           <entry il_offset="0x0" start_row="9" start_column="5" end_row="9" end_column="29" file_ref="0"/>
                                           <entry il_offset="0x1" start_row="10" start_column="13" end_row="10" end_column="73" file_ref="0"/>
                                           <entry il_offset="0x2a" start_row="11" start_column="5" end_row="11" end_column="12" file_ref="0"/>
                                       </sequencepoints>
                                       <locals>
                                           <local name="aList2" il_index="0" il_start="0x0" il_end="0x2b" attributes="0"/>
                                       </locals>
                                       <scope startOffset="0x0" endOffset="0x2b">
                                           <namespace name="System" importlevel="file"/>
                                           <namespace name="System.Collections.Generic" importlevel="file"/>
                                           <currentnamespace name=""/>
                                           <local name="aList2" il_index="0" il_start="0x0" il_end="0x2b" attributes="0"/>
                                       </scope>
                                   </method>
                               </methods>
                           </symbols>
            PDBTests.AssertXmlEqual(expected, actual)
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
            Dim actual = GetPdbXml(compilation, "C1.Main")

            Dim expected = <symbols>
                               <entryPoint declaringType="C1" methodName="Main" parameterNames=""/>
                               <methods>
                                   <method containingType="C1" name="Main" parameterNames="">
                                       <sequencepoints total="3">
                                           <entry il_offset="0x0" start_row="9" start_column="5" end_row="9" end_column="29" file_ref="0"/>
                                           <entry il_offset="0x1" start_row="10" start_column="13" end_row="10" end_column="107" file_ref="0"/>
                                           <entry il_offset="0x2c" start_row="11" start_column="5" end_row="11" end_column="12" file_ref="0"/>
                                       </sequencepoints>
                                       <locals>
                                           <local name="aList1" il_index="0" il_start="0x0" il_end="0x2d" attributes="0"/>
                                       </locals>
                                       <scope startOffset="0x0" endOffset="0x2d">
                                           <namespace name="System" importlevel="file"/>
                                           <namespace name="System.Collections.Generic" importlevel="file"/>
                                           <currentnamespace name=""/>
                                           <local name="aList1" il_index="0" il_start="0x0" il_end="0x2d" attributes="0"/>
                                       </scope>
                                   </method>
                               </methods>
                           </symbols>

            PDBTests.AssertXmlEqual(expected, actual)
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

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
                    source,
                    TestOptions.DebugExe)

            Dim actual = PDBTests.GetPdbXml(compilation, "C1.Main")

            Dim expected = <symbols>
                               <entryPoint declaringType="C1" methodName="Main" parameterNames=""/>
                               <methods>
                                   <method containingType="C1" name="Main" parameterNames="">
                                       <sequencepoints total="4">
                                           <entry il_offset="0x0" start_row="9" start_column="5" end_row="9" end_column="29" file_ref="0"/>
                                           <entry il_offset="0x1" start_row="10" start_column="13" end_row="10" end_column="19" file_ref="0"/>
                                           <entry il_offset="0x2a" start_row="10" start_column="21" end_row="10" end_column="27" file_ref="0"/>
                                           <entry il_offset="0x53" start_row="11" start_column="5" end_row="11" end_column="12" file_ref="0"/>
                                       </sequencepoints>
                                       <locals>
                                           <local name="aList1" il_index="0" il_start="0x0" il_end="0x54" attributes="0"/>
                                           <local name="aList2" il_index="1" il_start="0x0" il_end="0x54" attributes="0"/>
                                       </locals>
                                       <scope startOffset="0x0" endOffset="0x54">
                                           <namespace name="System" importlevel="file"/>
                                           <namespace name="System.Collections.Generic" importlevel="file"/>
                                           <currentnamespace name=""/>
                                           <local name="aList1" il_index="0" il_start="0x0" il_end="0x54" attributes="0"/>
                                           <local name="aList2" il_index="1" il_start="0x0" il_end="0x54" attributes="0"/>
                                       </scope>
                                   </method>
                               </methods>
                           </symbols>

            PDBTests.AssertXmlEqual(expected, actual)
        End Sub

    End Class
End Namespace
