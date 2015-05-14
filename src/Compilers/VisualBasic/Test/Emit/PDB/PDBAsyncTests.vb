' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.PDB
    Public Class PDBAsyncTests
        Inherits BasicTestBase

        <WorkItem(1085911)>
        <Fact>
        Public Sub SimpleAsyncMethod()
            Dim source =
<compilation>
    <file>
Imports System
Imports System.Threading.Tasks

Class C
    Shared Async Function M() As Task(Of Integer)
        Await Task.Delay(1)
        Return 1
    End Function
End Class
    </file>
</compilation>

            Dim v = CompileAndVerify(source, LatestReferences, options:=TestOptions.DebugDll)

            v.VerifyIL("C.VB$StateMachine_1_M.MoveNext", "
{
  // Code size      185 (0xb9)
  .maxstack  3
  .locals init (Integer V_0,
                Integer V_1,
                System.Threading.Tasks.Task(Of Integer) V_2,
                System.Runtime.CompilerServices.TaskAwaiter V_3,
                C.VB$StateMachine_1_M V_4,
                System.Exception V_5)
 ~IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.VB$StateMachine_1_M.$State As Integer""
  IL_0006:  stloc.1
  .try
  {
   ~IL_0007:  ldloc.1
    IL_0008:  brfalse.s  IL_000c
    IL_000a:  br.s       IL_000e
    IL_000c:  br.s       IL_004a
   -IL_000e:  nop
   -IL_000f:  nop
    IL_0010:  ldc.i4.1
    IL_0011:  call       ""Function System.Threading.Tasks.Task.Delay(Integer) As System.Threading.Tasks.Task""
    IL_0016:  callvirt   ""Function System.Threading.Tasks.Task.GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter""
    IL_001b:  stloc.3
   ~IL_001c:  ldloca.s   V_3
    IL_001e:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter.get_IsCompleted() As Boolean""
    IL_0023:  brtrue.s   IL_0068
    IL_0025:  ldarg.0
    IL_0026:  ldc.i4.0
    IL_0027:  dup
    IL_0028:  stloc.1
    IL_0029:  stfld      ""C.VB$StateMachine_1_M.$State As Integer""
   <IL_002e:  ldarg.0
    IL_002f:  ldloc.3
    IL_0030:  stfld      ""C.VB$StateMachine_1_M.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
    IL_0035:  ldarg.0
    IL_0036:  ldflda     ""C.VB$StateMachine_1_M.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
    IL_003b:  ldloca.s   V_3
    IL_003d:  ldarg.0
    IL_003e:  stloc.s    V_4
    IL_0040:  ldloca.s   V_4
    IL_0042:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter, C.VB$StateMachine_1_M)(ByRef System.Runtime.CompilerServices.TaskAwaiter, ByRef C.VB$StateMachine_1_M)""
    IL_0047:  nop
    IL_0048:  leave.s    IL_00b8
   >IL_004a:  ldarg.0
    IL_004b:  ldc.i4.m1
    IL_004c:  dup
    IL_004d:  stloc.1
    IL_004e:  stfld      ""C.VB$StateMachine_1_M.$State As Integer""
    IL_0053:  ldarg.0
    IL_0054:  ldfld      ""C.VB$StateMachine_1_M.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
    IL_0059:  stloc.3
    IL_005a:  ldarg.0
    IL_005b:  ldflda     ""C.VB$StateMachine_1_M.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
    IL_0060:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_0066:  br.s       IL_0068
    IL_0068:  ldloca.s   V_3
    IL_006a:  call       ""Sub System.Runtime.CompilerServices.TaskAwaiter.GetResult()""
    IL_006f:  nop
    IL_0070:  ldloca.s   V_3
    IL_0072:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
   -IL_0078:  ldc.i4.1
    IL_0079:  stloc.0
    IL_007a:  leave.s    IL_00a1
  }
  catch System.Exception
  {
   ~IL_007c:  dup
    IL_007d:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)""
    IL_0082:  stloc.s    V_5
   ~IL_0084:  ldarg.0
    IL_0085:  ldc.i4.s   -2
    IL_0087:  stfld      ""C.VB$StateMachine_1_M.$State As Integer""
    IL_008c:  ldarg.0
    IL_008d:  ldflda     ""C.VB$StateMachine_1_M.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
    IL_0092:  ldloc.s    V_5
    IL_0094:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetException(System.Exception)""
    IL_0099:  nop
    IL_009a:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()""
    IL_009f:  leave.s    IL_00b8
  }
 -IL_00a1:  ldarg.0
  IL_00a2:  ldc.i4.s   -2
  IL_00a4:  dup
  IL_00a5:  stloc.1
  IL_00a6:  stfld      ""C.VB$StateMachine_1_M.$State As Integer""
 ~IL_00ab:  ldarg.0
  IL_00ac:  ldflda     ""C.VB$StateMachine_1_M.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
  IL_00b1:  ldloc.0
  IL_00b2:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetResult(Integer)""
  IL_00b7:  nop
  IL_00b8:  ret
}", sequencePoints:="C+VB$StateMachine_1_M.MoveNext")

            ' NOTE: No <local> for the return variable "M".
            v.VerifyPdb("C+VB$StateMachine_1_M.MoveNext",
<symbols>
    <methods>
        <method containingType="C+VB$StateMachine_1_M" name="MoveNext">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="20" offset="-1"/>
                    <slot kind="27" offset="-1"/>
                    <slot kind="0" offset="-1"/>
                    <slot kind="33" offset="0"/>
                    <slot kind="temp"/>
                    <slot kind="temp"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" hidden="true" document="0"/>
                <entry offset="0x7" hidden="true" document="0"/>
                <entry offset="0xe" startLine="5" startColumn="5" endLine="5" endColumn="50" document="0"/>
                <entry offset="0xf" startLine="6" startColumn="9" endLine="6" endColumn="28" document="0"/>
                <entry offset="0x1c" hidden="true" document="0"/>
                <entry offset="0x78" startLine="7" startColumn="9" endLine="7" endColumn="17" document="0"/>
                <entry offset="0x7c" hidden="true" document="0"/>
                <entry offset="0x84" hidden="true" document="0"/>
                <entry offset="0xa1" startLine="8" startColumn="5" endLine="8" endColumn="17" document="0"/>
                <entry offset="0xab" hidden="true" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xb9">
                <namespace name="System" importlevel="file"/>
                <namespace name="System.Threading.Tasks" importlevel="file"/>
                <currentnamespace name=""/>
            </scope>
            <asyncInfo>
                <kickoffMethod declaringType="C" methodName="M"/>
                <await yield="0x2e" resume="0x4a" declaringType="C+VB$StateMachine_1_M" methodName="MoveNext"/>
            </asyncInfo>
        </method>
    </methods>
</symbols>)
        End Sub

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
                <entry offset="0xe" startLine="11" startColumn="5" endLine="11" endColumn="68" document="0"/>
                <entry offset="0xf" startLine="12" startColumn="9" endLine="12" endColumn="25" document="0"/>
                <entry offset="0x1f" hidden="true" document="0"/>
                <entry offset="0x7b" startLine="13" startColumn="9" endLine="13" endColumn="17" document="0"/>
                <entry offset="0x7f" hidden="true" document="0"/>
                <entry offset="0x87" hidden="true" document="0"/>
                <entry offset="0xa4" startLine="14" startColumn="5" endLine="14" endColumn="17" document="0"/>
                <entry offset="0xae" hidden="true" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xbc">
                <importsforward declaringType="Module1" methodName="Main" parameterNames="args"/>
            </scope>
            <asyncInfo>
                <kickoffMethod declaringType="Module1" methodName="F" parameterNames="a"/>
                <await yield="0x31" resume="0x4d" declaringType="Module1+VB$StateMachine_1_F" methodName="MoveNext"/>
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
                <entry offset="0x5d" startLine="16" startColumn="5" endLine="16" endColumn="34" document="0"/>
                <entry offset="0x5e" startLine="17" startColumn="9" endLine="23" endColumn="34" document="0"/>
                <entry offset="0x5f" startLine="17" startColumn="9" endLine="23" endColumn="34" document="0"/>
                <entry offset="0x60" startLine="17" startColumn="9" endLine="23" endColumn="34" document="0"/>
                <entry offset="0x73" hidden="true" document="0"/>
                <entry offset="0xdd" startLine="17" startColumn="9" endLine="23" endColumn="34" document="0"/>
                <entry offset="0xf5" hidden="true" document="0"/>
                <entry offset="0x17a" hidden="true" document="0"/>
                <entry offset="0x1e2" startLine="17" startColumn="9" endLine="23" endColumn="34" document="0"/>
                <entry offset="0x1e3" startLine="17" startColumn="9" endLine="23" endColumn="34" document="0"/>
                <entry offset="0x1e4" startLine="17" startColumn="9" endLine="23" endColumn="34" document="0"/>
                <entry offset="0x1e5" startLine="17" startColumn="9" endLine="23" endColumn="34" document="0"/>
                <entry offset="0x1f8" hidden="true" document="0"/>
                <entry offset="0x271" hidden="true" document="0"/>
                <entry offset="0x2ea" hidden="true" document="0"/>
                <entry offset="0x354" startLine="17" startColumn="9" endLine="23" endColumn="34" document="0"/>
                <entry offset="0x36c" hidden="true" document="0"/>
                <entry offset="0x3ed" hidden="true" document="0"/>
                <entry offset="0x46c" hidden="true" document="0"/>
                <entry offset="0x4c8" startLine="24" startColumn="5" endLine="24" endColumn="17" document="0"/>
                <entry offset="0x4ca" hidden="true" document="0"/>
                <entry offset="0x4d2" hidden="true" document="0"/>
                <entry offset="0x4ef" startLine="24" startColumn="5" endLine="24" endColumn="17" document="0"/>
                <entry offset="0x4f9" hidden="true" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x506">
                <importsforward declaringType="Module1" methodName="Main" parameterNames="args"/>
            </scope>
            <asyncInfo>
                <kickoffMethod declaringType="Module1" methodName="Test"/>
                <await yield="0x85" resume="0xa5" declaringType="Module1+VB$StateMachine_2_Test" methodName="MoveNext"/>
                <await yield="0x107" resume="0x127" declaringType="Module1+VB$StateMachine_2_Test" methodName="MoveNext"/>
                <await yield="0x18c" resume="0x1ab" declaringType="Module1+VB$StateMachine_2_Test" methodName="MoveNext"/>
                <await yield="0x20a" resume="0x22a" declaringType="Module1+VB$StateMachine_2_Test" methodName="MoveNext"/>
                <await yield="0x283" resume="0x2a3" declaringType="Module1+VB$StateMachine_2_Test" methodName="MoveNext"/>
                <await yield="0x2fc" resume="0x31c" declaringType="Module1+VB$StateMachine_2_Test" methodName="MoveNext"/>
                <await yield="0x37e" resume="0x39e" declaringType="Module1+VB$StateMachine_2_Test" methodName="MoveNext"/>
                <await yield="0x3ff" resume="0x41e" declaringType="Module1+VB$StateMachine_2_Test" methodName="MoveNext"/>
                <await yield="0x47e" resume="0x49a" declaringType="Module1+VB$StateMachine_2_Test" methodName="MoveNext"/>
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
                <entry offset="0xe" startLine="26" startColumn="5" endLine="26" endColumn="18" document="0"/>
                <entry offset="0xf" startLine="27" startColumn="9" endLine="27" endColumn="25" document="0"/>
                <entry offset="0x1e" hidden="true" document="0"/>
                <entry offset="0x79" startLine="28" startColumn="5" endLine="28" endColumn="12" document="0"/>
                <entry offset="0x7b" hidden="true" document="0"/>
                <entry offset="0x83" hidden="true" document="0"/>
                <entry offset="0xa0" startLine="28" startColumn="5" endLine="28" endColumn="12" document="0"/>
                <entry offset="0xaa" hidden="true" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xb7">
                <importsforward declaringType="Module1" methodName="Main" parameterNames="args"/>
            </scope>
            <asyncInfo>
                <catchHandler offset="0x7b"/>
                <kickoffMethod declaringType="Module1" methodName="S"/>
                <await yield="0x30" resume="0x4b" declaringType="Module1+VB$StateMachine_3_S" methodName="MoveNext"/>
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
                <entry offset="0x11" startLine="5" startColumn="5" endLine="5" endColumn="50" document="0"/>
                <entry offset="0x12" hidden="true" document="0"/>
                <entry offset="0x1d" startLine="6" startColumn="13" endLine="6" endColumn="29" document="0"/>
                <entry offset="0x29" startLine="7" startColumn="13" endLine="7" endColumn="29" document="0"/>
                <entry offset="0x35" startLine="9" startColumn="13" endLine="9" endColumn="53" document="0"/>
                <entry offset="0x4c" startLine="11" startColumn="9" endLine="11" endColumn="55" document="0"/>
                <entry offset="0x7c" hidden="true" document="0"/>
                <entry offset="0xda" startLine="12" startColumn="9" endLine="12" endColumn="21" document="0"/>
                <entry offset="0xeb" startLine="13" startColumn="9" endLine="13" endColumn="21" document="0"/>
                <entry offset="0xfc" startLine="14" startColumn="5" endLine="14" endColumn="17" document="0"/>
                <entry offset="0xfe" hidden="true" document="0"/>
                <entry offset="0x106" hidden="true" document="0"/>
                <entry offset="0x123" startLine="14" startColumn="5" endLine="14" endColumn="17" document="0"/>
                <entry offset="0x12d" hidden="true" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x13a">
                <importsforward declaringType="C+_Closure$__1-0" methodName="_Lambda$__0"/>
                <scope startOffset="0x11" endOffset="0xfd">
                    <local name="$VB$ResumableLocal_$VB$Closure_$0" il_index="0" il_start="0x11" il_end="0xfd" attributes="0"/>
                    <local name="$VB$ResumableLocal_a$1" il_index="1" il_start="0x11" il_end="0xfd" attributes="0"/>
                </scope>
            </scope>
            <asyncInfo>
                <kickoffMethod declaringType="C" methodName="Async_Lambda"/>
                <await yield="0x8e" resume="0xac" declaringType="C+VB$StateMachine_1_Async_Lambda" methodName="MoveNext"/>
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
                <entry offset="0xe" startLine="5" startColumn="5" endLine="5" endColumn="52" document="0"/>
                <entry offset="0xf" startLine="6" startColumn="13" endLine="6" endColumn="29" document="0"/>
                <entry offset="0x16" startLine="7" startColumn="13" endLine="7" endColumn="29" document="0"/>
                <entry offset="0x1d" startLine="9" startColumn="9" endLine="9" endColumn="55" document="0"/>
                <entry offset="0x43" hidden="true" document="0"/>
                <entry offset="0xa1" startLine="10" startColumn="9" endLine="10" endColumn="21" document="0"/>
                <entry offset="0xad" startLine="11" startColumn="9" endLine="11" endColumn="21" document="0"/>
                <entry offset="0xb9" startLine="12" startColumn="5" endLine="12" endColumn="17" document="0"/>
                <entry offset="0xbb" hidden="true" document="0"/>
                <entry offset="0xc3" hidden="true" document="0"/>
                <entry offset="0xe0" startLine="12" startColumn="5" endLine="12" endColumn="17" document="0"/>
                <entry offset="0xea" hidden="true" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xf7">
                <namespace name="System" importlevel="file"/>
                <namespace name="System.Threading.Tasks" importlevel="file"/>
                <currentnamespace name=""/>
                <scope startOffset="0xe" endOffset="0xba">
                    <local name="$VB$ResumableLocal_x$0" il_index="0" il_start="0xe" il_end="0xba" attributes="0"/>
                    <local name="$VB$ResumableLocal_y$1" il_index="1" il_start="0xe" il_end="0xba" attributes="0"/>
                </scope>
            </scope>
            <asyncInfo>
                <kickoffMethod declaringType="C" methodName="Async_NoLambda"/>
                <await yield="0x55" resume="0x73" declaringType="C+VB$StateMachine_1_Async_NoLambda" methodName="MoveNext"/>
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

    End Class
End Namespace