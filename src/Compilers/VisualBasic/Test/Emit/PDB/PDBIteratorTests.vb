' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Test.Utilities
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

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source, TestOptions.DebugExe)
            compilation.VerifyPdb("Program+_Closure$__+VB$StateMachine___Lambda$__0-0.MoveNext",
<symbols>
    <entryPoint declaringType="Program" methodName="Main" parameterNames="args"/>
    <methods>
        <method containingType="Program+_Closure$__+VB$StateMachine___Lambda$__0-0" name="MoveNext">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="20" offset="-1"/>
                    <slot kind="27" offset="-1"/>
                    <slot kind="21" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" hidden="true" document="0"/>
                <entry offset="0x2f" startLine="7" startColumn="13" endLine="7" endColumn="33" document="0"/>
                <entry offset="0x30" startLine="7" startColumn="13" endLine="7" endColumn="33" document="0"/>
                <entry offset="0x31" startLine="8" startColumn="17" endLine="8" endColumn="24" document="0"/>
                <entry offset="0x4c" startLine="9" startColumn="17" endLine="9" endColumn="24" document="0"/>
                <entry offset="0x6c" startLine="10" startColumn="13" endLine="10" endColumn="25" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x6e">
                <importsforward declaringType="Program" methodName="Main" parameterNames="args"/>
            </scope>
        </method>
    </methods>
</symbols>)
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

            ' VERY IMPORTANT!!!! We must have locals named $VB$ResumableLocal_x$1 and $VB$ResumableLocal_x$2 here
            '                    Even though they do not really exist in IL, EE will rely on them for scoping     
            compilation.VerifyPdb("Module1+VB$StateMachine_1_Foo.MoveNext",
<symbols>
    <entryPoint declaringType="Module1" methodName="Main"/>
    <methods>
        <method containingType="Module1+VB$StateMachine_1_Foo" name="MoveNext">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="20" offset="-1"/>
                    <slot kind="27" offset="-1"/>
                    <slot kind="temp"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" hidden="true" document="0"/>
                <entry offset="0x46" startLine="12" startColumn="5" endLine="12" endColumn="55" document="0"/>
                <entry offset="0x47" startLine="12" startColumn="5" endLine="12" endColumn="55" document="0"/>
                <entry offset="0x48" startLine="13" startColumn="13" endLine="13" endColumn="19" document="0"/>
                <entry offset="0x54" startLine="14" startColumn="9" endLine="14" endColumn="20" document="0"/>
                <entry offset="0x5e" startLine="16" startColumn="9" endLine="16" endColumn="26" document="0"/>
                <entry offset="0x71" hidden="true" document="0"/>
                <entry offset="0x86" startLine="17" startColumn="13" endLine="17" endColumn="20" document="0"/>
                <entry offset="0xa6" startLine="18" startColumn="13" endLine="18" endColumn="20" document="0"/>
                <entry offset="0xc6" startLine="19" startColumn="9" endLine="19" endColumn="13" document="0"/>
                <entry offset="0xc7" hidden="true" document="0"/>
                <entry offset="0xd5" hidden="true" document="0"/>
                <entry offset="0xe9" startLine="21" startColumn="9" endLine="21" endColumn="28" document="0"/>
                <entry offset="0xfb" hidden="true" document="0"/>
                <entry offset="0x114" startLine="22" startColumn="13" endLine="22" endColumn="44" document="0"/>
                <entry offset="0x139" startLine="23" startColumn="13" endLine="23" endColumn="44" document="0"/>
                <entry offset="0x15e" startLine="24" startColumn="9" endLine="24" endColumn="13" document="0"/>
                <entry offset="0x15f" hidden="true" document="0"/>
                <entry offset="0x16d" hidden="true" document="0"/>
                <entry offset="0x187" startLine="26" startColumn="5" endLine="26" endColumn="17" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x189">
                <importsforward declaringType="Module1" methodName="Main"/>
                <scope startOffset="0x47" endOffset="0x188">
                    <local name="$VB$ResumableLocal_arr$0" il_index="0" il_start="0x47" il_end="0x188" attributes="0"/>
                    <scope startOffset="0x73" endOffset="0xd4">
                        <local name="$VB$ResumableLocal_x$3" il_index="3" il_start="0x73" il_end="0xd4" attributes="0"/>
                    </scope>
                    <scope startOffset="0xfd" endOffset="0x16c">
                        <local name="$VB$ResumableLocal_x$6" il_index="6" il_start="0xfd" il_end="0x16c" attributes="0"/>
                    </scope>
                </scope>
            </scope>
        </method>
    </methods>
</symbols>)
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

            ' Goal: We're looking for the double-mangled name "$VB$ResumableLocal_$VB$Closure_$0".
            compilation.VerifyPdb("C+VB$StateMachine_1_Iterator_Lambda_Hoisted.MoveNext",
<symbols>
    <methods>
        <method containingType="C+VB$StateMachine_1_Iterator_Lambda_Hoisted" name="MoveNext">
            <sequencePoints>
                <entry offset="0x0" hidden="true" document="0"/>
                <entry offset="0x19" hidden="true" document="0"/>
                <entry offset="0x24" startLine="6" startColumn="13" endLine="6" endColumn="29" document="0"/>
                <entry offset="0x30" startLine="7" startColumn="13" endLine="7" endColumn="29" document="0"/>
                <entry offset="0x3c" startLine="9" startColumn="13" endLine="9" endColumn="53" document="0"/>
                <entry offset="0x43" startLine="11" startColumn="9" endLine="11" endColumn="20" document="0"/>
                <entry offset="0x74" startLine="12" startColumn="9" endLine="12" endColumn="21" document="0"/>
                <entry offset="0x85" startLine="13" startColumn="9" endLine="13" endColumn="21" document="0"/>
                <entry offset="0x96" startLine="14" startColumn="5" endLine="14" endColumn="17" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x98">
                <importsforward declaringType="C+_Closure$__1-0" methodName="_Lambda$__0"/>
                <scope startOffset="0x19" endOffset="0x97">
                    <local name="$VB$ResumableLocal_$VB$Closure_$0" il_index="0" il_start="0x19" il_end="0x97" attributes="0"/>
                </scope>
            </scope>
        </method>
    </methods>
</symbols>)
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

            ' Goal: We're looking for the single-mangled name "$VB$Closure_0".
            compilation.VerifyPdb("C+VB$StateMachine_1_Iterator_Lambda_NotHoisted.MoveNext",
<symbols>
    <methods>
        <method containingType="C+VB$StateMachine_1_Iterator_Lambda_NotHoisted" name="MoveNext">
            <sequencePoints>
                <entry offset="0x0" hidden="true" document="0"/>
                <entry offset="0x19" hidden="true" document="0"/>
                <entry offset="0x1f" startLine="6" startColumn="13" endLine="6" endColumn="29" document="0"/>
                <entry offset="0x26" startLine="7" startColumn="13" endLine="7" endColumn="29" document="0"/>
                <entry offset="0x2d" startLine="11" startColumn="9" endLine="11" endColumn="20" document="0"/>
                <entry offset="0x54" startLine="12" startColumn="5" endLine="12" endColumn="17" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x56">
                <importsforward declaringType="C+_Closure$__1-0" methodName="_Lambda$__0"/>
                <scope startOffset="0x19" endOffset="0x55">
                    <local name="$VB$Closure_0" il_index="1" il_start="0x19" il_end="0x55" attributes="0"/>
                </scope>
            </scope>
        </method>
    </methods>
</symbols>)
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

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(source, TestOptions.ReleaseDll)

            ' Goal: We're looking for the single-mangled names "$VB$ResumableLocal_x$0" and "$VB$ResumableLocal_y$1".
            compilation.VerifyPdb("C+VB$StateMachine_1_Iterator_NoLambda_Hoisted.MoveNext",
<symbols>
    <methods>
        <method containingType="C+VB$StateMachine_1_Iterator_NoLambda_Hoisted" name="MoveNext">
            <sequencePoints>
                <entry offset="0x0" hidden="true" document="0"/>
                <entry offset="0x19" startLine="6" startColumn="13" endLine="6" endColumn="29" document="0"/>
                <entry offset="0x20" startLine="7" startColumn="13" endLine="7" endColumn="29" document="0"/>
                <entry offset="0x27" startLine="8" startColumn="9" endLine="8" endColumn="20" document="0"/>
                <entry offset="0x4e" startLine="9" startColumn="9" endLine="9" endColumn="21" document="0"/>
                <entry offset="0x5a" startLine="10" startColumn="9" endLine="10" endColumn="21" document="0"/>
                <entry offset="0x66" startLine="11" startColumn="5" endLine="11" endColumn="17" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x68">
                <namespace name="System" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <currentnamespace name=""/>
                <scope startOffset="0x19" endOffset="0x67">
                    <local name="$VB$ResumableLocal_x$0" il_index="0" il_start="0x19" il_end="0x67" attributes="0"/>
                    <local name="$VB$ResumableLocal_y$1" il_index="1" il_start="0x19" il_end="0x67" attributes="0"/>
                </scope>
            </scope>
        </method>
    </methods>
</symbols>)
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

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(source, TestOptions.ReleaseDll)

            ' Goal: We're looking for the unmangled names "x" and "y".
            compilation.VerifyPdb("C+VB$StateMachine_1_Iterator_NoLambda_NotHoisted.MoveNext",
<symbols>
    <methods>
        <method containingType="C+VB$StateMachine_1_Iterator_NoLambda_NotHoisted" name="MoveNext">
            <sequencePoints>
                <entry offset="0x0" hidden="true" document="0"/>
                <entry offset="0x19" startLine="6" startColumn="13" endLine="6" endColumn="29" document="0"/>
                <entry offset="0x1b" startLine="7" startColumn="13" endLine="7" endColumn="29" document="0"/>
                <entry offset="0x1d" startLine="8" startColumn="9" endLine="8" endColumn="20" document="0"/>
                <entry offset="0x3a" startLine="9" startColumn="5" endLine="9" endColumn="17" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x3c">
                <namespace name="System" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <currentnamespace name=""/>
                <scope startOffset="0x19" endOffset="0x3b">
                    <local name="x" il_index="1" il_start="0x19" il_end="0x3b" attributes="0"/>
                    <local name="y" il_index="2" il_start="0x19" il_end="0x3b" attributes="0"/>
                </scope>
            </scope>
        </method>
    </methods>
</symbols>)
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
            compilation.VerifyPdb("Module1+VB$StateMachine_1_Foo.MoveNext",
<symbols>
    <entryPoint declaringType="Module1" methodName="Main"/>
    <methods>
        <method containingType="Module1+VB$StateMachine_1_Foo" name="MoveNext">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="20" offset="-1"/>
                    <slot kind="27" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" hidden="true" document="0"/>
                <entry offset="0x2f" startLine="12" startColumn="5" endLine="13" endColumn="55" document="0"/>
                <entry offset="0x30" startLine="13" startColumn="5" endLine="13" endColumn="55" document="0"/>
                <entry offset="0x31" startLine="14" startColumn="9" endLine="14" endColumn="16" document="0"/>
                <entry offset="0x4c" startLine="15" startColumn="9" endLine="15" endColumn="16" document="0"/>
                <entry offset="0x67" startLine="16" startColumn="5" endLine="16" endColumn="17" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x69">
                <importsforward declaringType="Module1" methodName="Main"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

    End Class
End Namespace
