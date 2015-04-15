' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.PDB
    Public Class PDBAsyncTests
        Inherits BasicTestBase

        <Fact, WorkItem(651996, "DevDiv")>
        Public Sub TestAsync()
            Dim source =
<compilation>
    <file><![CDATA[
Option Strict Off
Imports System
Imports System.Threading
Imports System.Threading.Tasks

Module Module1
    Sub Main(args As String())
        Test().Wait()
    End Sub

    Async Function F(ParamArray a() As Integer) As Task(Of Integer)
        Await Task.Yield
        Return 0
    End Function

    Async Function Test() As Task
        Await F(Await F(
                    Await F(),
                    1,
                    Await F(12)),
                Await F(
                    Await F(Await F(Await F())),
                    Await F(12)))
    End Function

    Async Sub S()
        Await Task.Yield
    End Sub 
End Module
]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithReferences(source, references:=LatestReferences, options:=TestOptions.DebugDll)

            compilation.VerifyPdb("Module1.F",
<symbols>
    <methods>
        <method containingType="Module1" name="F" parameterNames="a">
            <customDebugInfo>
                <forwardIterator name="VB$StateMachine_1_F"/>
            </customDebugInfo>
        </method>
    </methods>
</symbols>)

            compilation.VerifyPdb("Module1+VB$StateMachine_1_F.MoveNext",
<symbols>
    <methods>
        <method containingType="Module1+VB$StateMachine_1_F" name="MoveNext">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="20" offset="-1"/>
                    <slot kind="27" offset="-1"/>
                    <slot kind="0" offset="-1"/>
                    <slot kind="33" offset="0"/>
                    <slot kind="temp"/>
                    <slot kind="temp"/>
                    <slot kind="temp"/>
                    <slot kind="temp"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" hidden="true" document="0"/>
                <entry offset="0x7" hidden="true" document="0"/>
                <entry offset="0xf" startLine="11" startColumn="5" endLine="11" endColumn="68" document="0"/>
                <entry offset="0x10" startLine="12" startColumn="9" endLine="12" endColumn="25" document="0"/>
                <entry offset="0x20" hidden="true" document="0"/>
                <entry offset="0x7f" startLine="13" startColumn="9" endLine="13" endColumn="17" document="0"/>
                <entry offset="0x83" hidden="true" document="0"/>
                <entry offset="0x8b" hidden="true" document="0"/>
                <entry offset="0xa8" startLine="14" startColumn="5" endLine="14" endColumn="17" document="0"/>
                <entry offset="0xb2" hidden="true" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xc0">
                <importsforward declaringType="Module1" methodName="Main" parameterNames="args"/>
            </scope>
            <asyncInfo>
                <kickoffMethod declaringType="Module1" methodName="F" parameterNames="a"/>
                <await yield="0x36" resume="0x52" declaringType="Module1+VB$StateMachine_1_F" methodName="MoveNext"/>
            </asyncInfo>
        </method>
    </methods>
</symbols>)

            compilation.VerifyPdb("Module1.Test",
<symbols>
    <methods>
        <method containingType="Module1" name="Test">
            <customDebugInfo>
                <forwardIterator name="VB$StateMachine_2_Test"/>
            </customDebugInfo>
        </method>
    </methods>
</symbols>)

            compilation.VerifyPdb("Module1+VB$StateMachine_2_Test.MoveNext",
<symbols>
    <methods>
        <method containingType="Module1+VB$StateMachine_2_Test" name="MoveNext">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="27" offset="-1"/>
                    <slot kind="33" offset="0"/>
                    <slot kind="33" offset="8"/>
                    <slot kind="33" offset="125"/>
                    <slot kind="33" offset="38"/>
                    <slot kind="33" offset="94"/>
                    <slot kind="temp"/>
                    <slot kind="temp"/>
                    <slot kind="temp"/>
                    <slot kind="33" offset="155"/>
                    <slot kind="33" offset="205"/>
                    <slot kind="33" offset="163"/>
                    <slot kind="33" offset="171"/>
                    <slot kind="temp"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" hidden="true" document="0"/>
                <entry offset="0x7" hidden="true" document="0"/>
                <entry offset="0x66" startLine="16" startColumn="5" endLine="16" endColumn="34" document="0"/>
                <entry offset="0x67" startLine="17" startColumn="9" endLine="23" endColumn="34" document="0"/>
                <entry offset="0x68" startLine="17" startColumn="9" endLine="23" endColumn="34" document="0"/>
                <entry offset="0x69" startLine="17" startColumn="9" endLine="23" endColumn="34" document="0"/>
                <entry offset="0x7c" hidden="true" document="0"/>
                <entry offset="0xea" startLine="17" startColumn="9" endLine="23" endColumn="34" document="0"/>
                <entry offset="0x102" hidden="true" document="0"/>
                <entry offset="0x18b" hidden="true" document="0"/>
                <entry offset="0x1f7" startLine="17" startColumn="9" endLine="23" endColumn="34" document="0"/>
                <entry offset="0x1f8" startLine="17" startColumn="9" endLine="23" endColumn="34" document="0"/>
                <entry offset="0x1f9" startLine="17" startColumn="9" endLine="23" endColumn="34" document="0"/>
                <entry offset="0x1fa" startLine="17" startColumn="9" endLine="23" endColumn="34" document="0"/>
                <entry offset="0x20d" hidden="true" document="0"/>
                <entry offset="0x28a" hidden="true" document="0"/>
                <entry offset="0x307" hidden="true" document="0"/>
                <entry offset="0x375" startLine="17" startColumn="9" endLine="23" endColumn="34" document="0"/>
                <entry offset="0x38d" hidden="true" document="0"/>
                <entry offset="0x412" hidden="true" document="0"/>
                <entry offset="0x495" hidden="true" document="0"/>
                <entry offset="0x4f5" startLine="24" startColumn="5" endLine="24" endColumn="17" document="0"/>
                <entry offset="0x4f7" hidden="true" document="0"/>
                <entry offset="0x4ff" hidden="true" document="0"/>
                <entry offset="0x51c" startLine="24" startColumn="5" endLine="24" endColumn="17" document="0"/>
                <entry offset="0x526" hidden="true" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x533">
                <importsforward declaringType="Module1" methodName="Main" parameterNames="args"/>
            </scope>
            <asyncInfo>
                <kickoffMethod declaringType="Module1" methodName="Test"/>
                <await yield="0x92" resume="0xb2" declaringType="Module1+VB$StateMachine_2_Test" methodName="MoveNext"/>
                <await yield="0x118" resume="0x138" declaringType="Module1+VB$StateMachine_2_Test" methodName="MoveNext"/>
                <await yield="0x1a1" resume="0x1c0" declaringType="Module1+VB$StateMachine_2_Test" methodName="MoveNext"/>
                <await yield="0x223" resume="0x243" declaringType="Module1+VB$StateMachine_2_Test" methodName="MoveNext"/>
                <await yield="0x2a0" resume="0x2c0" declaringType="Module1+VB$StateMachine_2_Test" methodName="MoveNext"/>
                <await yield="0x31d" resume="0x33d" declaringType="Module1+VB$StateMachine_2_Test" methodName="MoveNext"/>
                <await yield="0x3a3" resume="0x3c3" declaringType="Module1+VB$StateMachine_2_Test" methodName="MoveNext"/>
                <await yield="0x428" resume="0x447" declaringType="Module1+VB$StateMachine_2_Test" methodName="MoveNext"/>
                <await yield="0x4ab" resume="0x4c7" declaringType="Module1+VB$StateMachine_2_Test" methodName="MoveNext"/>
            </asyncInfo>
        </method>
    </methods>
</symbols>)

            compilation.VerifyPdb("Module1.S",
<symbols>
    <methods>
        <method containingType="Module1" name="S">
            <customDebugInfo>
                <forwardIterator name="VB$StateMachine_3_S"/>
            </customDebugInfo>
        </method>
    </methods>
</symbols>)

            compilation.VerifyPdb("Module1+VB$StateMachine_3_S.MoveNext",
<symbols>
    <methods>
        <method containingType="Module1+VB$StateMachine_3_S" name="MoveNext">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="27" offset="-1"/>
                    <slot kind="33" offset="0"/>
                    <slot kind="temp"/>
                    <slot kind="temp"/>
                    <slot kind="temp"/>
                    <slot kind="temp"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" hidden="true" document="0"/>
                <entry offset="0x7" hidden="true" document="0"/>
                <entry offset="0xf" startLine="26" startColumn="5" endLine="26" endColumn="18" document="0"/>
                <entry offset="0x10" startLine="27" startColumn="9" endLine="27" endColumn="25" document="0"/>
                <entry offset="0x1f" hidden="true" document="0"/>
                <entry offset="0x7c" startLine="28" startColumn="5" endLine="28" endColumn="12" document="0"/>
                <entry offset="0x7e" hidden="true" document="0"/>
                <entry offset="0x86" hidden="true" document="0"/>
                <entry offset="0xa3" startLine="28" startColumn="5" endLine="28" endColumn="12" document="0"/>
                <entry offset="0xad" hidden="true" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xba">
                <importsforward declaringType="Module1" methodName="Main" parameterNames="args"/>
            </scope>
            <asyncInfo>
                <catchHandler offset="0x7e"/>
                <kickoffMethod declaringType="Module1" methodName="S"/>
                <await yield="0x33" resume="0x4f" declaringType="Module1+VB$StateMachine_3_S" methodName="MoveNext"/>
            </asyncInfo>
        </method>
    </methods>
</symbols>)
        End Sub

        <Fact(), WorkItem(827337, "DevDiv"), WorkItem(836491, "DevDiv")>
        Public Sub LocalCapturedInBetweenSuspensionPoints_Debug()
            Dim source =
<compilation>
    <file>
Imports System
Imports System.Threading.Tasks

Public Class C
    Private Async Function Async_Lambda() As Task
        Dim x As Integer = 1
        Dim y As Integer = 2

        Dim a As Func(Of Integer) = Function() x + y

        Await Console.Out.WriteAsync((x + y).ToString)
        x.ToString()
        y.ToString()
    End Function
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithReferences(
                    source,
                    {MscorlibRef_v4_0_30316_17626, MsvbRef},
                    TestOptions.DebugDll)

            ' Goal: We're looking for "$VB$ResumableLocal_$VB$Closure_$0" and "$VB$ResumableLocal_a$1".
            compilation.VerifyPdb("C+VB$StateMachine_1_Async_Lambda.MoveNext",
<symbols>
    <methods>
        <method containingType="C+VB$StateMachine_1_Async_Lambda" name="MoveNext">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="27" offset="-1"/>
                    <slot kind="33" offset="118"/>
                    <slot kind="temp"/>
                    <slot kind="temp"/>
                    <slot kind="temp"/>
                    <slot kind="temp"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" hidden="true" document="0"/>
                <entry offset="0x7" hidden="true" document="0"/>
                <entry offset="0x12" startLine="5" startColumn="5" endLine="5" endColumn="50" document="0"/>
                <entry offset="0x13" hidden="true" document="0"/>
                <entry offset="0x1e" startLine="6" startColumn="13" endLine="6" endColumn="29" document="0"/>
                <entry offset="0x2a" startLine="7" startColumn="13" endLine="7" endColumn="29" document="0"/>
                <entry offset="0x36" startLine="9" startColumn="13" endLine="9" endColumn="53" document="0"/>
                <entry offset="0x4d" startLine="11" startColumn="9" endLine="11" endColumn="55" document="0"/>
                <entry offset="0x7d" hidden="true" document="0"/>
                <entry offset="0xdd" startLine="12" startColumn="9" endLine="12" endColumn="21" document="0"/>
                <entry offset="0xee" startLine="13" startColumn="9" endLine="13" endColumn="21" document="0"/>
                <entry offset="0xff" startLine="14" startColumn="5" endLine="14" endColumn="17" document="0"/>
                <entry offset="0x101" hidden="true" document="0"/>
                <entry offset="0x109" hidden="true" document="0"/>
                <entry offset="0x126" startLine="14" startColumn="5" endLine="14" endColumn="17" document="0"/>
                <entry offset="0x130" hidden="true" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x13d">
                <importsforward declaringType="C+_Closure$__1-0" methodName="_Lambda$__0"/>
                <scope startOffset="0x12" endOffset="0x100">
                    <local name="$VB$ResumableLocal_$VB$Closure_$0" il_index="0" il_start="0x12" il_end="0x100" attributes="0"/>
                    <local name="$VB$ResumableLocal_a$1" il_index="1" il_start="0x12" il_end="0x100" attributes="0"/>
                </scope>
            </scope>
            <asyncInfo>
                <kickoffMethod declaringType="C" methodName="Async_Lambda"/>
                <await yield="0x91" resume="0xb0" declaringType="C+VB$StateMachine_1_Async_Lambda" methodName="MoveNext"/>
            </asyncInfo>
        </method>
    </methods>
</symbols>)
        End Sub

        <Fact()>
        Public Sub LocalCapturedInBetweenSuspensionPoints_Release()
            Dim source =
<compilation>
    <file>
Imports System
Imports System.Threading.Tasks

Public Class C
    Private Async Function Async_Lambda() As Task
        Dim x As Integer = 1
        Dim y As Integer = 2

        Dim a As Func(Of Integer) = Function() x + y

        Await Console.Out.WriteAsync((x + y).ToString)
        x.ToString()
        y.ToString()
    End Function
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithReferences(
                    source,
                    {MscorlibRef_v4_0_30316_17626, MsvbRef},
                    TestOptions.ReleaseDll)

            ' Goal: We're looking for "$VB$ResumableLocal_$VB$Closure_$0" but not "$VB$ResumableLocal_a$1".
            compilation.VerifyPdb("C+VB$StateMachine_1_Async_Lambda.MoveNext",
<symbols>
    <methods>
        <method containingType="C+VB$StateMachine_1_Async_Lambda" name="MoveNext">
            <sequencePoints>
                <entry offset="0x0" hidden="true" document="0"/>
                <entry offset="0x7" hidden="true" document="0"/>
                <entry offset="0xa" hidden="true" document="0"/>
                <entry offset="0x15" startLine="6" startColumn="13" endLine="6" endColumn="29" document="0"/>
                <entry offset="0x21" startLine="7" startColumn="13" endLine="7" endColumn="29" document="0"/>
                <entry offset="0x2d" startLine="11" startColumn="9" endLine="11" endColumn="55" document="0"/>
                <entry offset="0x5c" hidden="true" document="0"/>
                <entry offset="0xb3" startLine="12" startColumn="9" endLine="12" endColumn="21" document="0"/>
                <entry offset="0xc4" startLine="13" startColumn="9" endLine="13" endColumn="21" document="0"/>
                <entry offset="0xd5" startLine="14" startColumn="5" endLine="14" endColumn="17" document="0"/>
                <entry offset="0xd7" hidden="true" document="0"/>
                <entry offset="0xde" hidden="true" document="0"/>
                <entry offset="0xf9" startLine="14" startColumn="5" endLine="14" endColumn="17" document="0"/>
                <entry offset="0x103" hidden="true" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x10f">
                <importsforward declaringType="C+_Closure$__1-0" methodName="_Lambda$__0"/>
                <scope startOffset="0xa" endOffset="0xd6">
                    <local name="$VB$ResumableLocal_$VB$Closure_$0" il_index="0" il_start="0xa" il_end="0xd6" attributes="0"/>
                </scope>
            </scope>
            <asyncInfo>
                <kickoffMethod declaringType="C" methodName="Async_Lambda"/>
                <await yield="0x6e" resume="0x88" declaringType="C+VB$StateMachine_1_Async_Lambda" methodName="MoveNext"/>
            </asyncInfo>
        </method>
    </methods>
</symbols>)
        End Sub

        <Fact(), WorkItem(827337, "DevDiv"), WorkItem(836491, "DevDiv")>
        Public Sub LocalNotCapturedInBetweenSuspensionPoints_Debug()
            Dim source =
<compilation>
    <file>
Imports System
Imports System.Threading.Tasks

Public Class C
    Private Async Function Async_NoLambda() As Task
        Dim x As Integer = 1
        Dim y As Integer = 2

        Await Console.Out.WriteAsync((x + y).ToString)
        x.ToString()
        y.ToString()
    End Function
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithReferences(
                    source,
                    {MscorlibRef_v4_0_30316_17626, MsvbRef},
                    TestOptions.DebugDll)

            ' Goal: We're looking for the single-mangled names "$VB$ResumableLocal_x$1" and "$VB$ResumableLocal_y$2".
            compilation.VerifyPdb("C+VB$StateMachine_1_Async_NoLambda.MoveNext",
<symbols>
    <methods>
        <method containingType="C+VB$StateMachine_1_Async_NoLambda" name="MoveNext">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="27" offset="-1"/>
                    <slot kind="33" offset="62"/>
                    <slot kind="temp"/>
                    <slot kind="temp"/>
                    <slot kind="temp"/>
                    <slot kind="temp"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" hidden="true" document="0"/>
                <entry offset="0x7" hidden="true" document="0"/>
                <entry offset="0xf" startLine="5" startColumn="5" endLine="5" endColumn="52" document="0"/>
                <entry offset="0x10" startLine="6" startColumn="13" endLine="6" endColumn="29" document="0"/>
                <entry offset="0x17" startLine="7" startColumn="13" endLine="7" endColumn="29" document="0"/>
                <entry offset="0x1e" startLine="9" startColumn="9" endLine="9" endColumn="55" document="0"/>
                <entry offset="0x44" hidden="true" document="0"/>
                <entry offset="0xa4" startLine="10" startColumn="9" endLine="10" endColumn="21" document="0"/>
                <entry offset="0xb0" startLine="11" startColumn="9" endLine="11" endColumn="21" document="0"/>
                <entry offset="0xbc" startLine="12" startColumn="5" endLine="12" endColumn="17" document="0"/>
                <entry offset="0xbe" hidden="true" document="0"/>
                <entry offset="0xc6" hidden="true" document="0"/>
                <entry offset="0xe3" startLine="12" startColumn="5" endLine="12" endColumn="17" document="0"/>
                <entry offset="0xed" hidden="true" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xfa">
                <namespace name="System" importlevel="file"/>
                <namespace name="System.Threading.Tasks" importlevel="file"/>
                <currentnamespace name=""/>
                <scope startOffset="0xf" endOffset="0xbd">
                    <local name="$VB$ResumableLocal_x$0" il_index="0" il_start="0xf" il_end="0xbd" attributes="0"/>
                    <local name="$VB$ResumableLocal_y$1" il_index="1" il_start="0xf" il_end="0xbd" attributes="0"/>
                </scope>
            </scope>
            <asyncInfo>
                <kickoffMethod declaringType="C" methodName="Async_NoLambda"/>
                <await yield="0x58" resume="0x77" declaringType="C+VB$StateMachine_1_Async_NoLambda" methodName="MoveNext"/>
            </asyncInfo>
        </method>
    </methods>
</symbols>)
        End Sub

        <Fact()>
        Public Sub LocalNotCapturedInBetweenSuspensionPoints_Release()
            Dim source =
<compilation>
    <file>
Imports System
Imports System.Threading.Tasks

Public Class C
    Private Async Function Async_NoLambda() As Task
        Dim x As Integer = 1
        Dim y As Integer = 2

        Await Console.Out.WriteAsync((x + y).ToString)
        x.ToString()
        y.ToString()
    End Function
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithReferences(
                    source,
                    {MscorlibRef_v4_0_30316_17626, MsvbRef},
                    TestOptions.ReleaseDll)

            ' Goal: We're looking for the single-mangled names "$VB$ResumableLocal_x$1" and "$VB$ResumableLocal_y$2".
            compilation.VerifyPdb("C+VB$StateMachine_1_Async_NoLambda.MoveNext",
<symbols>
    <methods>
        <method containingType="C+VB$StateMachine_1_Async_NoLambda" name="MoveNext">
            <sequencePoints>
                <entry offset="0x0" hidden="true" document="0"/>
                <entry offset="0x7" hidden="true" document="0"/>
                <entry offset="0xa" startLine="6" startColumn="13" endLine="6" endColumn="29" document="0"/>
                <entry offset="0x11" startLine="7" startColumn="13" endLine="7" endColumn="29" document="0"/>
                <entry offset="0x18" startLine="9" startColumn="9" endLine="9" endColumn="55" document="0"/>
                <entry offset="0x3d" hidden="true" document="0"/>
                <entry offset="0x91" startLine="10" startColumn="9" endLine="10" endColumn="21" document="0"/>
                <entry offset="0x9d" startLine="11" startColumn="9" endLine="11" endColumn="21" document="0"/>
                <entry offset="0xa9" startLine="12" startColumn="5" endLine="12" endColumn="17" document="0"/>
                <entry offset="0xab" hidden="true" document="0"/>
                <entry offset="0xb2" hidden="true" document="0"/>
                <entry offset="0xcd" startLine="12" startColumn="5" endLine="12" endColumn="17" document="0"/>
                <entry offset="0xd7" hidden="true" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xe3">
                <namespace name="System" importlevel="file"/>
                <namespace name="System.Threading.Tasks" importlevel="file"/>
                <currentnamespace name=""/>
                <scope startOffset="0xa" endOffset="0xaa">
                    <local name="$VB$ResumableLocal_x$0" il_index="0" il_start="0xa" il_end="0xaa" attributes="0"/>
                    <local name="$VB$ResumableLocal_y$1" il_index="1" il_start="0xa" il_end="0xaa" attributes="0"/>
                </scope>
            </scope>
            <asyncInfo>
                <kickoffMethod declaringType="C" methodName="Async_NoLambda"/>
                <await yield="0x4f" resume="0x66" declaringType="C+VB$StateMachine_1_Async_NoLambda" methodName="MoveNext"/>
            </asyncInfo>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(1085911)>
        <Fact>
        Public Sub AsyncReturnVariable()
            Dim source =
<compilation>
    <file>
Imports System
Imports System.Threading.Tasks

Class C
    Shared Async Function M() As Task(Of Integer)
        Return 1
    End Function
End Class
    </file>
</compilation>

            Dim c = CreateCompilationWithReferences(source, references:=LatestReferences, options:=TestOptions.DebugDll)
            c.AssertNoErrors()

            ' NOTE: No <local> for the return variable "M".
            c.VerifyPdb("C+VB$StateMachine_1_M.MoveNext",
<symbols>
    <methods>
        <method containingType="C+VB$StateMachine_1_M" name="MoveNext">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="20" offset="-1"/>
                    <slot kind="27" offset="-1"/>
                    <slot kind="0" offset="-1"/>
                    <slot kind="temp"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" hidden="true" document="0"/>
                <entry offset="0x7" startLine="5" startColumn="5" endLine="5" endColumn="50" document="0"/>
                <entry offset="0x8" startLine="6" startColumn="9" endLine="6" endColumn="17" document="0"/>
                <entry offset="0xc" hidden="true" document="0"/>
                <entry offset="0x13" hidden="true" document="0"/>
                <entry offset="0x2f" startLine="7" startColumn="5" endLine="7" endColumn="17" document="0"/>
                <entry offset="0x39" hidden="true" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x47">
                <namespace name="System" importlevel="file"/>
                <namespace name="System.Threading.Tasks" importlevel="file"/>
                <currentnamespace name=""/>
            </scope>
            <asyncInfo>
                <kickoffMethod declaringType="C" methodName="M"/>
            </asyncInfo>
        </method>
    </methods>
</symbols>)
        End Sub

    End Class
End Namespace