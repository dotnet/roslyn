' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.PDB
    Public Class PDBIteratorTests
        Inherits BasicTestBase

        <Fact, WorkItem(651996, "DevDiv")>
        Public Sub IteratorLambdaWithForEach()
            Dim source =
<compilation>
    <file>
Imports System
Imports System.Collections
Imports System.Collections.Generic

Module Program
    Sub Main(args As String())
        baz(Iterator Function(x)
                Yield 1
                Yield x
            End Function)
    End Sub

    Public Sub baz(Of T)(x As Func(Of Integer, IEnumerable(Of T)))
        For Each i In x(42)
            Console.Write(i)
        Next
    End Sub

End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
                    source,
                    TestOptions.DebugExe)

            Dim actual = PDBTests.GetPdbXml(compilation, "Program+VB$StateMachine_2__Lambda$__1.MoveNext")

            Dim expected =
<symbols>
    <entryPoint declaringType="Program" methodName="Main" parameterNames="args"/>
    <methods>
        <method containingType="Program+VB$StateMachine_2__Lambda$__1" name="MoveNext" parameterNames="">
            <sequencepoints total="6">
                <entry il_offset="0x0" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                <entry il_offset="0x2f" start_row="7" start_column="13" end_row="7" end_column="33" file_ref="0"/>
                <entry il_offset="0x30" start_row="7" start_column="13" end_row="7" end_column="33" file_ref="0"/>
                <entry il_offset="0x31" start_row="8" start_column="17" end_row="8" end_column="24" file_ref="0"/>
                <entry il_offset="0x4c" start_row="9" start_column="17" end_row="9" end_column="24" file_ref="0"/>
                <entry il_offset="0x6c" start_row="10" start_column="13" end_row="10" end_column="25" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="VB$returnTemp" il_index="0" il_start="0x0" il_end="0x6e" attributes="1"/>
                <local name="VB$cachedState" il_index="1" il_start="0x0" il_end="0x6e" attributes="1"/>
                <local name="MoveNext" il_index="2" il_start="0x30" il_end="0x6d" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x6e">
                <importsforward declaringType="Program" methodName="Main" parameterNames="args"/>
                <local name="VB$returnTemp" il_index="0" il_start="0x0" il_end="0x6e" attributes="1"/>
                <local name="VB$cachedState" il_index="1" il_start="0x0" il_end="0x6e" attributes="1"/>
                <scope startOffset="0x30" endOffset="0x6d">
                    <local name="MoveNext" il_index="2" il_start="0x30" il_end="0x6d" attributes="0"/>
                </scope>
            </scope>
        </method>
    </methods>
</symbols>

            PDBTests.AssertXmlEqual(expected, actual)
        End Sub

        <Fact(), WorkItem(651996, "DevDiv"), WorkItem(789705, "DevDiv")>
        Public Sub IteratorWithLiftedMultipleSameNameLocals()
            Dim source =
<compilation>
    <file>
Imports System
Imports System.Collections.Generic

Module Module1

    Sub Main()
        For Each i In Foo
            Console.Write(i)
        Next
    End Sub

    Iterator Function Foo() As IEnumerable(Of Integer)
        Dim arr(1) As Integer
        arr(0) = 42

        For Each x In arr
            Yield x
            Yield x
        Next

        For Each x In "abc"
            Yield System.Convert.ToInt32(x)
            Yield System.Convert.ToInt32(x)
        Next

    End Function

End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source, TestOptions.DebugExe)

            Dim actual = PDBTests.GetPdbXml(compilation, "Module1+VB$StateMachine_1_Foo.MoveNext")

            ' VERY IMPORTANT!!!! We must have locals named $VB$ResumableLocal_x$1 and $VB$ResumableLocal_x$2 here
            '                    Even though they do not really exist in IL, EE will rely on them for scoping     
            Dim expected =
<symbols>
    <entryPoint declaringType="Module1" methodName="Main" parameterNames=""/>
    <methods>
        <method containingType="Module1+VB$StateMachine_1_Foo" name="MoveNext" parameterNames="">
            <sequencepoints total="20">
                <entry il_offset="0x0" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                <entry il_offset="0x43" start_row="12" start_column="5" end_row="12" end_column="55" file_ref="0"/>
                <entry il_offset="0x44" start_row="12" start_column="5" end_row="12" end_column="55" file_ref="0"/>
                <entry il_offset="0x45" start_row="13" start_column="13" end_row="13" end_column="19" file_ref="0"/>
                <entry il_offset="0x4c" start_row="14" start_column="9" end_row="14" end_column="20" file_ref="0"/>
                <entry il_offset="0x51" start_row="16" start_column="9" end_row="16" end_column="26" file_ref="0"/>
                <entry il_offset="0x5f" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                <entry il_offset="0x74" start_row="17" start_column="13" end_row="17" end_column="20" file_ref="0"/>
                <entry il_offset="0x94" start_row="18" start_column="13" end_row="18" end_column="20" file_ref="0"/>
                <entry il_offset="0xb4" start_row="19" start_column="9" end_row="19" end_column="13" file_ref="0"/>
                <entry il_offset="0xb5" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                <entry il_offset="0xc3" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                <entry il_offset="0xd9" start_row="21" start_column="9" end_row="21" end_column="28" file_ref="0"/>
                <entry il_offset="0xeb" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                <entry il_offset="0x104" start_row="22" start_column="13" end_row="22" end_column="44" file_ref="0"/>
                <entry il_offset="0x129" start_row="23" start_column="13" end_row="23" end_column="44" file_ref="0"/>
                <entry il_offset="0x14e" start_row="24" start_column="9" end_row="24" end_column="13" file_ref="0"/>
                <entry il_offset="0x14f" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                <entry il_offset="0x15d" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                <entry il_offset="0x179" start_row="26" start_column="5" end_row="26" end_column="17" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="VB$returnTemp" il_index="0" il_start="0x0" il_end="0x17b" attributes="1"/>
                <local name="VB$cachedState" il_index="1" il_start="0x0" il_end="0x17b" attributes="1"/>
                <local name="MoveNext" il_index="2" il_start="0x44" il_end="0x17a" attributes="0"/>
                <local name="arr" il_index="3" il_start="0x44" il_end="0x17a" attributes="0"/>
                <local name="$VB$ResumableLocal_x$1" il_index="0" il_start="0x61" il_end="0xc2" attributes="1" reusingslot="True"/>
                <local name="$VB$ResumableLocal_x$2" il_index="0" il_start="0xed" il_end="0x15c" attributes="1" reusingslot="True"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x17b">
                <importsforward declaringType="Module1" methodName="Main" parameterNames=""/>
                <local name="VB$returnTemp" il_index="0" il_start="0x0" il_end="0x17b" attributes="1"/>
                <local name="VB$cachedState" il_index="1" il_start="0x0" il_end="0x17b" attributes="1"/>
                <scope startOffset="0x44" endOffset="0x17a">
                    <local name="MoveNext" il_index="2" il_start="0x44" il_end="0x17a" attributes="0"/>
                    <local name="arr" il_index="3" il_start="0x44" il_end="0x17a" attributes="0"/>
                    <scope startOffset="0x61" endOffset="0xc2">
                        <local name="$VB$ResumableLocal_x$1" il_index="0" il_start="0x61" il_end="0xc2" attributes="1"/>
                    </scope>
                    <scope startOffset="0xed" endOffset="0x15c">
                        <local name="$VB$ResumableLocal_x$2" il_index="0" il_start="0xed" il_end="0x15c" attributes="1"/>
                    </scope>
                </scope>
            </scope>
        </method>
    </methods>
</symbols>

            PDBTests.AssertXmlEqual(expected, actual)
        End Sub

        <Fact(), WorkItem(827337, "DevDiv"), WorkItem(836491, "DevDiv")>
        Public Sub LocalCapturedAndHoisted()
            Dim source =
<compilation>
    <file>
Imports System
Imports System.Collections.Generic

Public Class C
    Private Iterator Function Iterator_Lambda_Hoisted() As IEnumerable(Of Integer)
        Dim x As Integer = 1
        Dim y As Integer = 2

        Dim a As Func(Of Integer) = Function() x + y

        Yield x + y
        x.ToString()
        y.ToString()
    End Function
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
                    source,
                    TestOptions.ReleaseDll)

            Dim actual = PDBTests.GetPdbXml(compilation, "C+VB$StateMachine_4_Iterator_Lambda_Hoisted.MoveNext")

            ' Goal: We're looking for the double-mangled name "$VB$ResumableLocal_$VB$Closure_4$1".
            Dim expected =
<symbols>
    <methods>
        <method containingType="C+VB$StateMachine_4_Iterator_Lambda_Hoisted" name="MoveNext" parameterNames="">
            <sequencepoints total="9">
                <entry il_offset="0x0" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                <entry il_offset="0x19" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                <entry il_offset="0x24" start_row="6" start_column="13" end_row="6" end_column="29" file_ref="0"/>
                <entry il_offset="0x30" start_row="7" start_column="13" end_row="7" end_column="29" file_ref="0"/>
                <entry il_offset="0x3c" start_row="9" start_column="13" end_row="9" end_column="53" file_ref="0"/>
                <entry il_offset="0x43" start_row="11" start_column="9" end_row="11" end_column="20" file_ref="0"/>
                <entry il_offset="0x74" start_row="12" start_column="9" end_row="12" end_column="21" file_ref="0"/>
                <entry il_offset="0x85" start_row="13" start_column="9" end_row="13" end_column="21" file_ref="0"/>
                <entry il_offset="0x96" start_row="14" start_column="5" end_row="14" end_column="17" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="VB$cachedState" il_index="0" il_start="0x0" il_end="0x98" attributes="1"/>
                <local name="$VB$ResumableLocal_$VB$Closure_2$1" il_index="0" il_start="0x19" il_end="0x97" attributes="1" reusingslot="True"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x98">
                <importsforward declaringType="C" methodName="Iterator_Lambda_Hoisted" parameterNames=""/>
                <local name="VB$cachedState" il_index="0" il_start="0x0" il_end="0x98" attributes="1"/>
                <scope startOffset="0x19" endOffset="0x97">
                    <local name="$VB$ResumableLocal_$VB$Closure_2$1" il_index="0" il_start="0x19" il_end="0x97" attributes="1"/>
                </scope>
            </scope>
        </method>
    </methods>
</symbols>

            PDBTests.AssertXmlEqual(expected, actual)
        End Sub

        <Fact(), WorkItem(827337, "DevDiv"), WorkItem(836491, "DevDiv")>
        Public Sub LocalCapturedAndNotHoisted()
            Dim source =
<compilation>
    <file>
Imports System
Imports System.Collections.Generic

Public Class C
    Private Iterator Function Iterator_Lambda_NotHoisted() As IEnumerable(Of Integer)
        Dim x As Integer = 1
        Dim y As Integer = 2

        Dim a As Func(Of Integer) = Function() x + y

        Yield x + y
    End Function
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
                    source,
                    TestOptions.ReleaseDll)

            Dim actual = PDBTests.GetPdbXml(compilation, "C+VB$StateMachine_4_Iterator_Lambda_NotHoisted.MoveNext")

            ' Goal: We're looking for the single-mangled name "$VB$Closure_2".
            Dim expected =
<symbols>
    <methods>
        <method containingType="C+VB$StateMachine_4_Iterator_Lambda_NotHoisted" name="MoveNext" parameterNames="">
            <sequencepoints total="6">
                <entry il_offset="0x0" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                <entry il_offset="0x19" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                <entry il_offset="0x1f" start_row="6" start_column="13" end_row="6" end_column="29" file_ref="0"/>
                <entry il_offset="0x26" start_row="7" start_column="13" end_row="7" end_column="29" file_ref="0"/>
                <entry il_offset="0x2d" start_row="11" start_column="9" end_row="11" end_column="20" file_ref="0"/>
                <entry il_offset="0x54" start_row="12" start_column="5" end_row="12" end_column="17" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="VB$cachedState" il_index="0" il_start="0x0" il_end="0x56" attributes="1"/>
                <local name="$VB$Closure_2" il_index="1" il_start="0x19" il_end="0x55" attributes="1"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x56">
                <importsforward declaringType="C" methodName="Iterator_Lambda_NotHoisted" parameterNames=""/>
                <local name="VB$cachedState" il_index="0" il_start="0x0" il_end="0x56" attributes="1"/>
                <scope startOffset="0x19" endOffset="0x55">
                    <local name="$VB$Closure_2" il_index="1" il_start="0x19" il_end="0x55" attributes="1"/>
                </scope>
            </scope>
        </method>
    </methods>
</symbols>

            PDBTests.AssertXmlEqual(expected, actual)
        End Sub

        <Fact(), WorkItem(827337, "DevDiv"), WorkItem(836491, "DevDiv")>
        Public Sub LocalHoistedAndNotCapture()
            Dim source =
<compilation>
    <file>
Imports System
Imports System.Collections.Generic

Public Class C
    Private Iterator Function Iterator_NoLambda_Hoisted() As IEnumerable(Of Integer)
        Dim x As Integer = 1
        Dim y As Integer = 2
        Yield x + y
        x.ToString()
        y.ToString()
    End Function
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
                    source,
                    TestOptions.ReleaseDll)

            Dim actual = PDBTests.GetPdbXml(compilation, "C+VB$StateMachine_1_Iterator_NoLambda_Hoisted.MoveNext")

            ' Goal: We're looking for the single-mangled names "$VB$ResumableLocal_x$1" and "$VB$ResumableLocal_y$2".
            Dim expected =
<symbols>
    <methods>
        <method containingType="C+VB$StateMachine_1_Iterator_NoLambda_Hoisted" name="MoveNext" parameterNames="">
            <sequencepoints total="7">
                <entry il_offset="0x0" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                <entry il_offset="0x19" start_row="6" start_column="13" end_row="6" end_column="29" file_ref="0"/>
                <entry il_offset="0x20" start_row="7" start_column="13" end_row="7" end_column="29" file_ref="0"/>
                <entry il_offset="0x27" start_row="8" start_column="9" end_row="8" end_column="20" file_ref="0"/>
                <entry il_offset="0x4e" start_row="9" start_column="9" end_row="9" end_column="21" file_ref="0"/>
                <entry il_offset="0x5a" start_row="10" start_column="9" end_row="10" end_column="21" file_ref="0"/>
                <entry il_offset="0x66" start_row="11" start_column="5" end_row="11" end_column="17" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="VB$cachedState" il_index="0" il_start="0x0" il_end="0x68" attributes="1"/>
                <local name="$VB$ResumableLocal_x$1" il_index="0" il_start="0x19" il_end="0x67" attributes="1" reusingslot="True"/>
                <local name="$VB$ResumableLocal_y$2" il_index="0" il_start="0x19" il_end="0x67" attributes="1" reusingslot="True"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x68">
                <importsforward declaringType="C" methodName="Iterator_NoLambda_Hoisted" parameterNames=""/>
                <local name="VB$cachedState" il_index="0" il_start="0x0" il_end="0x68" attributes="1"/>
                <scope startOffset="0x19" endOffset="0x67">
                    <local name="$VB$ResumableLocal_x$1" il_index="0" il_start="0x19" il_end="0x67" attributes="1"/>
                    <local name="$VB$ResumableLocal_y$2" il_index="0" il_start="0x19" il_end="0x67" attributes="1"/>
                </scope>
            </scope>
        </method>
    </methods>
</symbols>

            PDBTests.AssertXmlEqual(expected, actual)
        End Sub

        <Fact(), WorkItem(827337, "DevDiv"), WorkItem(836491, "DevDiv")>
        Public Sub LocalNotHoistedAndNotCaptured()
            Dim source =
<compilation>
    <file>
Imports System
Imports System.Collections.Generic

Public Class C
    Private Iterator Function Iterator_NoLambda_NotHoisted() As IEnumerable(Of Integer)
        Dim x As Integer = 1
        Dim y As Integer = 2
        Yield x + y
    End Function
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
                    source,
                    TestOptions.ReleaseDll)

            Dim actual = PDBTests.GetPdbXml(compilation, "C+VB$StateMachine_1_Iterator_NoLambda_NotHoisted.MoveNext")

            ' Goal: We're looking for the unmangled names "x" and "y".
            Dim expected =
<symbols>
    <methods>
        <method containingType="C+VB$StateMachine_1_Iterator_NoLambda_NotHoisted" name="MoveNext" parameterNames="">
            <sequencepoints total="5">
                <entry il_offset="0x0" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                <entry il_offset="0x19" start_row="6" start_column="13" end_row="6" end_column="29" file_ref="0"/>
                <entry il_offset="0x1b" start_row="7" start_column="13" end_row="7" end_column="29" file_ref="0"/>
                <entry il_offset="0x1d" start_row="8" start_column="9" end_row="8" end_column="20" file_ref="0"/>
                <entry il_offset="0x3a" start_row="9" start_column="5" end_row="9" end_column="17" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="VB$cachedState" il_index="0" il_start="0x0" il_end="0x3c" attributes="1"/>
                <local name="x" il_index="1" il_start="0x19" il_end="0x3b" attributes="0"/>
                <local name="y" il_index="2" il_start="0x19" il_end="0x3b" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x3c">
                <importsforward declaringType="C" methodName="Iterator_NoLambda_NotHoisted" parameterNames=""/>
                <local name="VB$cachedState" il_index="0" il_start="0x0" il_end="0x3c" attributes="1"/>
                <scope startOffset="0x19" endOffset="0x3b">
                    <local name="x" il_index="1" il_start="0x19" il_end="0x3b" attributes="0"/>
                    <local name="y" il_index="2" il_start="0x19" il_end="0x3b" attributes="0"/>
                </scope>
            </scope>
        </method>
    </methods>
</symbols>

            PDBTests.AssertXmlEqual(expected, actual)
        End Sub

        ''' <summary>
        ''' Sequence points of MoveNext method shall not be affected by DebuggerHidden attribute. 
        ''' The method contains user code that can be edited during debugging and might need remapping.
        ''' </summary>
        <Fact, WorkItem(667579, "DevDiv")>
        Public Sub DebuggerHiddenIterator()
            Dim source =
<compilation>
    <file>
Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Module Module1

    Sub Main()
        For Each i In Foo
            Console.Write(i)
        Next
    End Sub

    &lt;DebuggerHidden&gt;
    Iterator Function Foo() As IEnumerable(Of Integer)
        Yield 1
        Yield 2
    End Function

End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source, TestOptions.DebugExe)

            Dim actual = PDBTests.GetPdbXml(compilation, "Module1+VB$StateMachine_1_Foo.MoveNext")

            Dim expected =
<symbols>
    <entryPoint declaringType="Module1" methodName="Main" parameterNames=""/>
    <methods>
        <method containingType="Module1+VB$StateMachine_1_Foo" name="MoveNext" parameterNames="">
            <sequencepoints total="6">
                <entry il_offset="0x0" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                <entry il_offset="0x2f" start_row="12" start_column="5" end_row="13" end_column="55" file_ref="0"/>
                <entry il_offset="0x30" start_row="13" start_column="5" end_row="13" end_column="55" file_ref="0"/>
                <entry il_offset="0x31" start_row="14" start_column="9" end_row="14" end_column="16" file_ref="0"/>
                <entry il_offset="0x4c" start_row="15" start_column="9" end_row="15" end_column="16" file_ref="0"/>
                <entry il_offset="0x67" start_row="16" start_column="5" end_row="16" end_column="17" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="VB$returnTemp" il_index="0" il_start="0x0" il_end="0x69" attributes="1"/>
                <local name="VB$cachedState" il_index="1" il_start="0x0" il_end="0x69" attributes="1"/>
                <local name="MoveNext" il_index="2" il_start="0x30" il_end="0x68" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x69">
                <importsforward declaringType="Module1" methodName="Main" parameterNames=""/>
                <local name="VB$returnTemp" il_index="0" il_start="0x0" il_end="0x69" attributes="1"/>
                <local name="VB$cachedState" il_index="1" il_start="0x0" il_end="0x69" attributes="1"/>
                <scope startOffset="0x30" endOffset="0x68">
                    <local name="MoveNext" il_index="2" il_start="0x30" il_end="0x68" attributes="0"/>
                </scope>
            </scope>
        </method>
    </methods>
</symbols>
            PDBTests.AssertXmlEqual(expected, actual)
        End Sub

    End Class
End Namespace
