' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.PDB
    Public Class PDBForEachTests
        Inherits BasicTestBase

#Region "For Each Loop"

        <Fact()>
        Public Sub ForEachOverOneDimensionalArray()
            Dim source =
<compilation>
    <file>
Option Strict On

Imports System

        Class C1
            Public Shared Sub Main()
                Dim arr As Integer() = New Integer(1) {}
                arr(0) = 23
                arr(1) = 42

                For Each element As Integer In arr
                    Console.WriteLine(element)
                Next
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
                                       <sequencepoints total="11">
                                           <entry il_offset="0x0" start_row="6" start_column="13" end_row="6" end_column="37" file_ref="0"/>
                                           <entry il_offset="0x1" start_row="7" start_column="21" end_row="7" end_column="57" file_ref="0"/>
                                           <entry il_offset="0x8" start_row="8" start_column="17" end_row="8" end_column="28" file_ref="0"/>
                                           <entry il_offset="0xd" start_row="9" start_column="17" end_row="9" end_column="28" file_ref="0"/>
                                           <entry il_offset="0x12" start_row="11" start_column="17" end_row="11" end_column="51" file_ref="0"/>
                                           <entry il_offset="0x16" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                                           <entry il_offset="0x1c" start_row="12" start_column="21" end_row="12" end_column="47" file_ref="0"/>
                                           <entry il_offset="0x23" start_row="13" start_column="17" end_row="13" end_column="21" file_ref="0"/>
                                           <entry il_offset="0x24" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                                           <entry il_offset="0x28" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                                           <entry il_offset="0x34" start_row="14" start_column="13" end_row="14" end_column="20" file_ref="0"/>
                                       </sequencepoints>
                                       <locals>
                                           <local name="arr" il_index="0" il_start="0x0" il_end="0x35" attributes="0"/>
                                           <local name="VB$ForEachArray" il_index="1" il_start="0x12" il_end="0x33" attributes="1"/>
                                           <local name="VB$ForEachArrayIndex" il_index="2" il_start="0x12" il_end="0x33" attributes="1"/>
                                           <local name="element" il_index="3" il_start="0x18" il_end="0x27" attributes="0"/>
                                       </locals>
                                       <scope startOffset="0x0" endOffset="0x35">
                                           <namespace name="System" importlevel="file"/>
                                           <currentnamespace name=""/>
                                           <local name="arr" il_index="0" il_start="0x0" il_end="0x35" attributes="0"/>
                                           <scope startOffset="0x12" endOffset="0x33">
                                               <local name="VB$ForEachArray" il_index="1" il_start="0x12" il_end="0x33" attributes="1"/>
                                               <local name="VB$ForEachArrayIndex" il_index="2" il_start="0x12" il_end="0x33" attributes="1"/>
                                               <scope startOffset="0x18" endOffset="0x27">
                                                   <local name="element" il_index="3" il_start="0x18" il_end="0x27" attributes="0"/>
                                               </scope>
                                           </scope>
                                       </scope>
                                   </method>
                               </methods>
                           </symbols>

            PDBTests.AssertXmlEqual(expected, actual)
        End Sub

        <Fact()>
        Public Sub ForEachOverString()
            Dim source =
<compilation>
    <file>
Option Strict On

Imports System

Class C1
    Public Shared Sub Main()
        Dim str As String = "Hello"

        For Each element As Char In str
            Console.WriteLine(element)
        Next
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
                            <sequencepoints total="9">
                                <entry il_offset="0x0" start_row="6" start_column="5" end_row="6" end_column="29" file_ref="0"/>
                                <entry il_offset="0x1" start_row="7" start_column="13" end_row="7" end_column="36" file_ref="0"/>
                                <entry il_offset="0x7" start_row="9" start_column="9" end_row="9" end_column="40" file_ref="0"/>
                                <entry il_offset="0xb" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                                <entry il_offset="0x15" start_row="10" start_column="13" end_row="10" end_column="39" file_ref="0"/>
                                <entry il_offset="0x1c" start_row="11" start_column="9" end_row="11" end_column="13" file_ref="0"/>
                                <entry il_offset="0x1d" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                                <entry il_offset="0x21" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                                <entry il_offset="0x30" start_row="12" start_column="5" end_row="12" end_column="12" file_ref="0"/>
                            </sequencepoints>
                            <locals>
                                <local name="str" il_index="0" il_start="0x0" il_end="0x31" attributes="0"/>
                                <local name="VB$ForEachArray" il_index="1" il_start="0x7" il_end="0x2f" attributes="1"/>
                                <local name="VB$ForEachArrayIndex" il_index="2" il_start="0x7" il_end="0x2f" attributes="1"/>
                                <local name="element" il_index="3" il_start="0xd" il_end="0x20" attributes="0"/>
                            </locals>
                            <scope startOffset="0x0" endOffset="0x31">
                                <namespace name="System" importlevel="file"/>
                                <currentnamespace name=""/>
                                <local name="str" il_index="0" il_start="0x0" il_end="0x31" attributes="0"/>
                                <scope startOffset="0x7" endOffset="0x2f">
                                    <local name="VB$ForEachArray" il_index="1" il_start="0x7" il_end="0x2f" attributes="1"/>
                                    <local name="VB$ForEachArrayIndex" il_index="2" il_start="0x7" il_end="0x2f" attributes="1"/>
                                    <scope startOffset="0xd" endOffset="0x20">
                                        <local name="element" il_index="3" il_start="0xd" il_end="0x20" attributes="0"/>
                                    </scope>
                                </scope>
                            </scope>
                        </method>
                    </methods>
                </symbols>

            PDBTests.AssertXmlEqual(expected, actual)
        End Sub

        <Fact()>
        Public Sub ForEachIEnumerableWithNoTryCatch()
            Dim source =
<compilation>
    <file>
Option Infer On

Class C
    Public Shared Sub Main()
        For Each x In New Enumerable()
            System.Console.WriteLine(x)
        Next
    End Sub
End Class

Class Enumerable
    Public Function GetEnumerator() As Enumerator
        Return New Enumerator()
    End Function
End Class
Structure Enumerator
    Private x As Integer
    Public ReadOnly Property Current() As Integer
        Get
            Return x
        End Get
    End Property
    Public Function MoveNext() As Boolean
        Return System.Threading.Interlocked.Increment(x) &lt; 4
    End Function
    Public Sub Dispose()
    End Sub
End Structure
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
                    source,
                    TestOptions.DebugExe)

            Dim actual = PDBTests.GetPdbXml(compilation, "C.Main")

            Dim expected = <symbols>
                               <entryPoint declaringType="C" methodName="Main" parameterNames=""/>
                               <methods>
                                   <method containingType="C" name="Main" parameterNames="">
                                       <sequencepoints total="7">
                                           <entry il_offset="0x0" start_row="4" start_column="5" end_row="4" end_column="29" file_ref="0"/>
                                           <entry il_offset="0x1" start_row="5" start_column="9" end_row="5" end_column="39" file_ref="0"/>
                                           <entry il_offset="0xc" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                                           <entry il_offset="0x16" start_row="6" start_column="13" end_row="6" end_column="40" file_ref="0"/>
                                           <entry il_offset="0x1d" start_row="7" start_column="9" end_row="7" end_column="13" file_ref="0"/>
                                           <entry il_offset="0x1e" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                                           <entry il_offset="0x29" start_row="8" start_column="5" end_row="8" end_column="12" file_ref="0"/>
                                       </sequencepoints>
                                       <locals>
                                           <local name="VB$ForEachEnumerator" il_index="0" il_start="0x1" il_end="0x28" attributes="1"/>
                                           <local name="x" il_index="1" il_start="0xe" il_end="0x1d" attributes="0"/>
                                       </locals>
                                       <scope startOffset="0x0" endOffset="0x2a">
                                           <currentnamespace name=""/>
                                           <scope startOffset="0x1" endOffset="0x28">
                                               <local name="VB$ForEachEnumerator" il_index="0" il_start="0x1" il_end="0x28" attributes="1"/>
                                               <scope startOffset="0xe" endOffset="0x1d">
                                                   <local name="x" il_index="1" il_start="0xe" il_end="0x1d" attributes="0"/>
                                               </scope>
                                           </scope>
                                       </scope>
                                   </method>
                               </methods>
                           </symbols>

            PDBTests.AssertXmlEqual(expected, actual)
        End Sub

        <Fact()>
        Public Sub ForEachIEnumerableWithTryCatchImplementIDisposable()
            Dim source =
<compilation>
    <file>
Option Infer On

Imports System.Collections.Generic
Imports System

Class C
    Public Shared Sub Main()
        For Each j In New Gen(Of Integer)(12, 42, 23)
            console.writeline(j)
        Next
    End Sub
End Class

Public Class Gen(Of T As New)
    Dim list As New List(Of T)

    Public Sub New(ParamArray elem() As T)
        For Each el In elem
            list.add(el)
        Next
    End Sub

    Public Function GetEnumerator() As IEnumerator(Of T)
        Return list.GetEnumerator
    End Function
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
                    source,
                    TestOptions.DebugExe)

            Dim actual = PDBTests.GetPdbXml(compilation, "C.Main")

            Dim expected = <symbols>
                               <entryPoint declaringType="C" methodName="Main" parameterNames=""/>
                               <methods>
                                   <method containingType="C" name="Main" parameterNames="">
                                       <sequencepoints total="8">
                                           <entry il_offset="0x0" start_row="7" start_column="5" end_row="7" end_column="29" file_ref="0"/>
                                           <entry il_offset="0x1" start_row="8" start_column="9" end_row="8" end_column="54" file_ref="0"/>
                                           <entry il_offset="0x1d" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                                           <entry il_offset="0x26" start_row="9" start_column="13" end_row="9" end_column="33" file_ref="0"/>
                                           <entry il_offset="0x2d" start_row="10" start_column="9" end_row="10" end_column="13" file_ref="0"/>
                                           <entry il_offset="0x2e" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                                           <entry il_offset="0x3a" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                                           <entry il_offset="0x4a" start_row="11" start_column="5" end_row="11" end_column="12" file_ref="0"/>
                                       </sequencepoints>
                                       <locals>
                                           <local name="VB$ForEachEnumerator" il_index="0" il_start="0x1" il_end="0x49" attributes="1"/>
                                           <local name="j" il_index="1" il_start="0x1f" il_end="0x2d" attributes="0"/>
                                       </locals>
                                       <scope startOffset="0x0" endOffset="0x4b">
                                           <namespace name="System.Collections.Generic" importlevel="file"/>
                                           <namespace name="System" importlevel="file"/>
                                           <currentnamespace name=""/>
                                           <scope startOffset="0x1" endOffset="0x49">
                                               <local name="VB$ForEachEnumerator" il_index="0" il_start="0x1" il_end="0x49" attributes="1"/>
                                               <scope startOffset="0x1f" endOffset="0x2d">
                                                   <local name="j" il_index="1" il_start="0x1f" il_end="0x2d" attributes="0"/>
                                               </scope>
                                           </scope>
                                       </scope>
                                   </method>
                               </methods>
                           </symbols>

            PDBTests.AssertXmlEqual(expected, actual)
        End Sub

        <Fact()>
        Public Sub ForEachIEnumerableWithTryCatchPossiblyImplementIDisposable()
            Dim source =
<compilation>
    <file>
Option Infer On

Class C
    Public Shared Sub Main()
        For Each x In New Enumerable()
            System.Console.WriteLine(x)
        Next
    End Sub
End Class

Class Enumerable
    Implements System.Collections.IEnumerable
    ' Explicit implementation won't match pattern.
    Private Function System_Collections_IEnumerable_GetEnumerator() As System.Collections.IEnumerator Implements System.Collections.IEnumerable.GetEnumerator
        Dim list As New System.Collections.Generic.List(Of Integer)()
        list.Add(3)
        list.Add(2)
        list.Add(1)
        Return list.GetEnumerator()
    End Function
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
                    source,
                    TestOptions.DebugExe)

            Dim actual = PDBTests.GetPdbXml(compilation, "C.Main")

            Dim expected = <symbols>
                               <entryPoint declaringType="C" methodName="Main" parameterNames=""/>
                               <methods>
                                   <method containingType="C" name="Main" parameterNames="">
                                       <sequencepoints total="8">
                                           <entry il_offset="0x0" start_row="4" start_column="5" end_row="4" end_column="29" file_ref="0"/>
                                           <entry il_offset="0x1" start_row="5" start_column="9" end_row="5" end_column="39" file_ref="0"/>
                                           <entry il_offset="0xc" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                                           <entry il_offset="0x1a" start_row="6" start_column="13" end_row="6" end_column="40" file_ref="0"/>
                                           <entry il_offset="0x26" start_row="7" start_column="9" end_row="7" end_column="13" file_ref="0"/>
                                           <entry il_offset="0x27" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                                           <entry il_offset="0x33" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                                           <entry il_offset="0x4d" start_row="8" start_column="5" end_row="8" end_column="12" file_ref="0"/>
                                       </sequencepoints>
                                       <locals>
                                           <local name="VB$ForEachEnumerator" il_index="0" il_start="0x1" il_end="0x4c" attributes="1"/>
                                           <local name="x" il_index="1" il_start="0xe" il_end="0x26" attributes="0"/>
                                       </locals>
                                       <scope startOffset="0x0" endOffset="0x4e">
                                           <currentnamespace name=""/>
                                           <scope startOffset="0x1" endOffset="0x4c">
                                               <local name="VB$ForEachEnumerator" il_index="0" il_start="0x1" il_end="0x4c" attributes="1"/>
                                               <scope startOffset="0xe" endOffset="0x26">
                                                   <local name="x" il_index="1" il_start="0xe" il_end="0x26" attributes="0"/>
                                               </scope>
                                           </scope>
                                       </scope>
                                   </method>
                               </methods>
                           </symbols>

            PDBTests.AssertXmlEqual(expected, actual)
        End Sub

#End Region

#Region "For Loop"

        <WorkItem(529183, "DevDiv")>
        <Fact()>
        Public Sub ForLoop01()
            Dim source =
<compilation>
    <file>
Option Strict On
Imports System

Module M1
    Sub Main()
        Dim myFArr(3) As Short
        Dim i As Short
        For i = 1 To 3
            myFArr(i) = i
        Next i
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
                    source,
                    TestOptions.DebugExe)

            ' Note: the scope of the loop variable is intentionally different from Dev11. It's now the scope of the complete loop and not just the body

            Dim actual = PDBTests.GetPdbXml(compilation, "M1.Main")
            Dim expected =
<symbols>
    <entryPoint declaringType="M1" methodName="Main" parameterNames=""/>
    <methods>
        <method containingType="M1" name="Main" parameterNames="">
            <sequencepoints total="7">
                <entry il_offset="0x0" start_row="5" start_column="5" end_row="5" end_column="15" file_ref="0"/>
                <entry il_offset="0x1" start_row="6" start_column="13" end_row="6" end_column="22" file_ref="0"/>
                <entry il_offset="0x8" start_row="8" start_column="9" end_row="8" end_column="23" file_ref="0"/>
                <entry il_offset="0xa" start_row="9" start_column="13" end_row="9" end_column="26" file_ref="0"/>
                <entry il_offset="0xe" start_row="10" start_column="9" end_row="10" end_column="15" file_ref="0"/>
                <entry il_offset="0x13" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                <entry il_offset="0x1e" start_row="11" start_column="5" end_row="11" end_column="12" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="myFArr" il_index="0" il_start="0x0" il_end="0x1f" attributes="0"/>
                <local name="i" il_index="1" il_start="0x0" il_end="0x1f" attributes="0"/>
                <local name="VB$LoopObject" il_index="2" il_start="0x8" il_end="0x1d" attributes="1"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x1f">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="myFArr" il_index="0" il_start="0x0" il_end="0x1f" attributes="0"/>
                <local name="i" il_index="1" il_start="0x0" il_end="0x1f" attributes="0"/>
                <scope startOffset="0x8" endOffset="0x1d">
                    <local name="VB$LoopObject" il_index="2" il_start="0x8" il_end="0x1d" attributes="1"/>
                </scope>
            </scope>
        </method>
    </methods>
</symbols>

            PDBTests.AssertXmlEqual(expected, actual)
        End Sub

        <WorkItem(529183, "DevDiv")>
        <Fact()>
        Public Sub ForLoop02()
            Dim source =
<compilation>
    <file>
Option Strict On
Imports System

Module M1
  Sub Main()
    For i as Object = 3 To 6 step 2
    Console.Writeline("Hello")        
    Next
   End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
                    source,
                    TestOptions.DebugExe)

            ' Note: the scope of the loop variable is intentionally different from Dev11. It's now the scope of the complete loop and not just the body            

            Dim actual = PDBTests.GetPdbXml(compilation, "M1.Main")
            Dim expected = <symbols>
                               <entryPoint declaringType="M1" methodName="Main" parameterNames=""/>
                               <methods>
                                   <method containingType="M1" name="Main" parameterNames="">
                                       <sequencepoints total="5">
                                           <entry il_offset="0x0" start_row="5" start_column="3" end_row="5" end_column="13" file_ref="0"/>
                                           <entry il_offset="0x1" start_row="6" start_column="5" end_row="6" end_column="36" file_ref="0"/>
                                           <entry il_offset="0x24" start_row="7" start_column="5" end_row="7" end_column="31" file_ref="0"/>
                                           <entry il_offset="0x2f" start_row="8" start_column="5" end_row="8" end_column="9" file_ref="0"/>
                                           <entry il_offset="0x3c" start_row="9" start_column="4" end_row="9" end_column="11" file_ref="0"/>
                                       </sequencepoints>
                                       <locals>
                                           <local name="VB$LoopObject" il_index="0" il_start="0x1" il_end="0x3b" attributes="1"/>
                                           <local name="i" il_index="1" il_start="0x1" il_end="0x3b" attributes="0"/>
                                       </locals>
                                       <scope startOffset="0x0" endOffset="0x3d">
                                           <namespace name="System" importlevel="file"/>
                                           <currentnamespace name=""/>
                                           <scope startOffset="0x1" endOffset="0x3b">
                                               <local name="VB$LoopObject" il_index="0" il_start="0x1" il_end="0x3b" attributes="1"/>
                                               <local name="i" il_index="1" il_start="0x1" il_end="0x3b" attributes="0"/>
                                           </scope>
                                       </scope>
                                   </method>
                               </methods>
                           </symbols>

            PDBTests.AssertXmlEqual(expected, actual)
        End Sub

#End Region

    End Class
End Namespace
