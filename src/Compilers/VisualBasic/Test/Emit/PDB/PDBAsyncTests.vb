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
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" hidden="true" document="0"/>
                <entry offset="0x7" hidden="true" document="0"/>
                <entry offset="0xf" startLine="11" startColumn="5" endLine="11" endColumn="68" document="0"/>
                <entry offset="0x10" startLine="12" startColumn="9" endLine="12" endColumn="25" document="0"/>
                <entry offset="0x20" hidden="true" document="0"/>
                <entry offset="0x7c" startLine="13" startColumn="9" endLine="13" endColumn="17" document="0"/>
                <entry offset="0x80" hidden="true" document="0"/>
                <entry offset="0x88" hidden="true" document="0"/>
                <entry offset="0xa5" startLine="14" startColumn="5" endLine="14" endColumn="17" document="0"/>
                <entry offset="0xaf" hidden="true" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xbd">
                <importsforward declaringType="Module1" methodName="Main" parameterNames="args"/>
            </scope>
            <asyncInfo>
                <kickoffMethod declaringType="Module1" methodName="F" parameterNames="a"/>
                <await yield="0x32" resume="0x4e" declaringType="Module1+VB$StateMachine_1_F" methodName="MoveNext"/>
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
                <entry offset="0xe6" startLine="17" startColumn="9" endLine="23" endColumn="34" document="0"/>
                <entry offset="0xfe" hidden="true" document="0"/>
                <entry offset="0x183" hidden="true" document="0"/>
                <entry offset="0x1eb" startLine="17" startColumn="9" endLine="23" endColumn="34" document="0"/>
                <entry offset="0x1ec" startLine="17" startColumn="9" endLine="23" endColumn="34" document="0"/>
                <entry offset="0x1ed" startLine="17" startColumn="9" endLine="23" endColumn="34" document="0"/>
                <entry offset="0x1ee" startLine="17" startColumn="9" endLine="23" endColumn="34" document="0"/>
                <entry offset="0x201" hidden="true" document="0"/>
                <entry offset="0x27a" hidden="true" document="0"/>
                <entry offset="0x2f3" hidden="true" document="0"/>
                <entry offset="0x35d" startLine="17" startColumn="9" endLine="23" endColumn="34" document="0"/>
                <entry offset="0x375" hidden="true" document="0"/>
                <entry offset="0x3f6" hidden="true" document="0"/>
                <entry offset="0x475" hidden="true" document="0"/>
                <entry offset="0x4d1" startLine="24" startColumn="5" endLine="24" endColumn="17" document="0"/>
                <entry offset="0x4d3" hidden="true" document="0"/>
                <entry offset="0x4db" hidden="true" document="0"/>
                <entry offset="0x4f8" startLine="24" startColumn="5" endLine="24" endColumn="17" document="0"/>
                <entry offset="0x502" hidden="true" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x50f">
                <importsforward declaringType="Module1" methodName="Main" parameterNames="args"/>
            </scope>
            <asyncInfo>
                <kickoffMethod declaringType="Module1" methodName="Test"/>
                <await yield="0x8e" resume="0xae" declaringType="Module1+VB$StateMachine_2_Test" methodName="MoveNext"/>
                <await yield="0x110" resume="0x130" declaringType="Module1+VB$StateMachine_2_Test" methodName="MoveNext"/>
                <await yield="0x195" resume="0x1b4" declaringType="Module1+VB$StateMachine_2_Test" methodName="MoveNext"/>
                <await yield="0x213" resume="0x233" declaringType="Module1+VB$StateMachine_2_Test" methodName="MoveNext"/>
                <await yield="0x28c" resume="0x2ac" declaringType="Module1+VB$StateMachine_2_Test" methodName="MoveNext"/>
                <await yield="0x305" resume="0x325" declaringType="Module1+VB$StateMachine_2_Test" methodName="MoveNext"/>
                <await yield="0x387" resume="0x3a7" declaringType="Module1+VB$StateMachine_2_Test" methodName="MoveNext"/>
                <await yield="0x408" resume="0x427" declaringType="Module1+VB$StateMachine_2_Test" methodName="MoveNext"/>
                <await yield="0x487" resume="0x4a3" declaringType="Module1+VB$StateMachine_2_Test" methodName="MoveNext"/>
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
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" hidden="true" document="0"/>
                <entry offset="0x7" hidden="true" document="0"/>
                <entry offset="0xf" startLine="26" startColumn="5" endLine="26" endColumn="18" document="0"/>
                <entry offset="0x10" startLine="27" startColumn="9" endLine="27" endColumn="25" document="0"/>
                <entry offset="0x1f" hidden="true" document="0"/>
                <entry offset="0x7a" startLine="28" startColumn="5" endLine="28" endColumn="12" document="0"/>
                <entry offset="0x7c" hidden="true" document="0"/>
                <entry offset="0x84" hidden="true" document="0"/>
                <entry offset="0xa1" startLine="28" startColumn="5" endLine="28" endColumn="12" document="0"/>
                <entry offset="0xab" hidden="true" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xb8">
                <importsforward declaringType="Module1" methodName="Main" parameterNames="args"/>
            </scope>
            <asyncInfo>
                <catchHandler offset="0x7c"/>
                <kickoffMethod declaringType="Module1" methodName="S"/>
                <await yield="0x31" resume="0x4c" declaringType="Module1+VB$StateMachine_3_S" methodName="MoveNext"/>
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
                <entry offset="0xdb" startLine="12" startColumn="9" endLine="12" endColumn="21" document="0"/>
                <entry offset="0xec" startLine="13" startColumn="9" endLine="13" endColumn="21" document="0"/>
                <entry offset="0xfd" startLine="14" startColumn="5" endLine="14" endColumn="17" document="0"/>
                <entry offset="0xff" hidden="true" document="0"/>
                <entry offset="0x107" hidden="true" document="0"/>
                <entry offset="0x124" startLine="14" startColumn="5" endLine="14" endColumn="17" document="0"/>
                <entry offset="0x12e" hidden="true" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x13b">
                <importsforward declaringType="C+_Closure$__1-0" methodName="_Lambda$__0"/>
                <scope startOffset="0x12" endOffset="0xfe">
                    <local name="$VB$ResumableLocal_$VB$Closure_$0" il_index="0" il_start="0x12" il_end="0xfe" attributes="0"/>
                    <local name="$VB$ResumableLocal_a$1" il_index="1" il_start="0x12" il_end="0xfe" attributes="0"/>
                </scope>
            </scope>
            <asyncInfo>
                <kickoffMethod declaringType="C" methodName="Async_Lambda"/>
                <await yield="0x8f" resume="0xad" declaringType="C+VB$StateMachine_1_Async_Lambda" methodName="MoveNext"/>
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
                <entry offset="0xa2" startLine="10" startColumn="9" endLine="10" endColumn="21" document="0"/>
                <entry offset="0xae" startLine="11" startColumn="9" endLine="11" endColumn="21" document="0"/>
                <entry offset="0xba" startLine="12" startColumn="5" endLine="12" endColumn="17" document="0"/>
                <entry offset="0xbc" hidden="true" document="0"/>
                <entry offset="0xc4" hidden="true" document="0"/>
                <entry offset="0xe1" startLine="12" startColumn="5" endLine="12" endColumn="17" document="0"/>
                <entry offset="0xeb" hidden="true" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xf8">
                <namespace name="System" importlevel="file"/>
                <namespace name="System.Threading.Tasks" importlevel="file"/>
                <currentnamespace name=""/>
                <scope startOffset="0xf" endOffset="0xbb">
                    <local name="$VB$ResumableLocal_x$0" il_index="0" il_start="0xf" il_end="0xbb" attributes="0"/>
                    <local name="$VB$ResumableLocal_y$1" il_index="1" il_start="0xf" il_end="0xbb" attributes="0"/>
                </scope>
            </scope>
            <asyncInfo>
                <kickoffMethod declaringType="C" methodName="Async_NoLambda"/>
                <await yield="0x56" resume="0x74" declaringType="C+VB$StateMachine_1_Async_NoLambda" methodName="MoveNext"/>
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