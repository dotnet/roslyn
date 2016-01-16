' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.PDB
    Public Class PDBAsyncTests
        Inherits BasicTestBase

        <WorkItem(1085911, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1085911")>
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

            Dim v = CompileAndVerify(source, LatestVbReferences, options:=TestOptions.DebugDll)

            v.VerifyIL("C.VB$StateMachine_1_M.MoveNext", "
{
  // Code size      184 (0xb8)
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
    IL_000c:  br.s       IL_0049
   -IL_000e:  nop
   -IL_000f:  ldc.i4.1
    IL_0010:  call       ""Function System.Threading.Tasks.Task.Delay(Integer) As System.Threading.Tasks.Task""
    IL_0015:  callvirt   ""Function System.Threading.Tasks.Task.GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter""
    IL_001a:  stloc.3
   ~IL_001b:  ldloca.s   V_3
    IL_001d:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter.get_IsCompleted() As Boolean""
    IL_0022:  brtrue.s   IL_0067
    IL_0024:  ldarg.0
    IL_0025:  ldc.i4.0
    IL_0026:  dup
    IL_0027:  stloc.1
    IL_0028:  stfld      ""C.VB$StateMachine_1_M.$State As Integer""
   <IL_002d:  ldarg.0
    IL_002e:  ldloc.3
    IL_002f:  stfld      ""C.VB$StateMachine_1_M.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
    IL_0034:  ldarg.0
    IL_0035:  ldflda     ""C.VB$StateMachine_1_M.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
    IL_003a:  ldloca.s   V_3
    IL_003c:  ldarg.0
    IL_003d:  stloc.s    V_4
    IL_003f:  ldloca.s   V_4
    IL_0041:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter, C.VB$StateMachine_1_M)(ByRef System.Runtime.CompilerServices.TaskAwaiter, ByRef C.VB$StateMachine_1_M)""
    IL_0046:  nop
    IL_0047:  leave.s    IL_00b7
   >IL_0049:  ldarg.0
    IL_004a:  ldc.i4.m1
    IL_004b:  dup
    IL_004c:  stloc.1
    IL_004d:  stfld      ""C.VB$StateMachine_1_M.$State As Integer""
    IL_0052:  ldarg.0
    IL_0053:  ldfld      ""C.VB$StateMachine_1_M.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
    IL_0058:  stloc.3
    IL_0059:  ldarg.0
    IL_005a:  ldflda     ""C.VB$StateMachine_1_M.$A0 As System.Runtime.CompilerServices.TaskAwaiter""
    IL_005f:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_0065:  br.s       IL_0067
    IL_0067:  ldloca.s   V_3
    IL_0069:  call       ""Sub System.Runtime.CompilerServices.TaskAwaiter.GetResult()""
    IL_006e:  nop
    IL_006f:  ldloca.s   V_3
    IL_0071:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
   -IL_0077:  ldc.i4.1
    IL_0078:  stloc.0
    IL_0079:  leave.s    IL_00a0
  }
  catch System.Exception
  {
   ~IL_007b:  dup
    IL_007c:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)""
    IL_0081:  stloc.s    V_5
   ~IL_0083:  ldarg.0
    IL_0084:  ldc.i4.s   -2
    IL_0086:  stfld      ""C.VB$StateMachine_1_M.$State As Integer""
    IL_008b:  ldarg.0
    IL_008c:  ldflda     ""C.VB$StateMachine_1_M.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
    IL_0091:  ldloc.s    V_5
    IL_0093:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetException(System.Exception)""
    IL_0098:  nop
    IL_0099:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()""
    IL_009e:  leave.s    IL_00b7
  }
 -IL_00a0:  ldarg.0
  IL_00a1:  ldc.i4.s   -2
  IL_00a3:  dup
  IL_00a4:  stloc.1
  IL_00a5:  stfld      ""C.VB$StateMachine_1_M.$State As Integer""
 ~IL_00aa:  ldarg.0
  IL_00ab:  ldflda     ""C.VB$StateMachine_1_M.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)""
  IL_00b0:  ldloc.0
  IL_00b1:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetResult(Integer)""
  IL_00b6:  nop
  IL_00b7:  ret
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
                <entry offset="0x0" hidden="true"/>
                <entry offset="0x7" hidden="true"/>
                <entry offset="0xe" startLine="5" startColumn="5" endLine="5" endColumn="50"/>
                <entry offset="0xf" startLine="6" startColumn="9" endLine="6" endColumn="28"/>
                <entry offset="0x1b" hidden="true"/>
                <entry offset="0x77" startLine="7" startColumn="9" endLine="7" endColumn="17"/>
                <entry offset="0x7b" hidden="true"/>
                <entry offset="0x83" hidden="true"/>
                <entry offset="0xa0" startLine="8" startColumn="5" endLine="8" endColumn="17"/>
                <entry offset="0xaa" hidden="true"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xb8">
                <namespace name="System" importlevel="file"/>
                <namespace name="System.Threading.Tasks" importlevel="file"/>
                <currentnamespace name=""/>
            </scope>
            <asyncInfo>
                <kickoffMethod declaringType="C" methodName="M"/>
                <await yield="0x2d" resume="0x49" declaringType="C+VB$StateMachine_1_M" methodName="MoveNext"/>
            </asyncInfo>
        </method>
    </methods>
</symbols>)
        End Sub

        <Fact, WorkItem(651996, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/651996")>
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

            Dim compilation = CompilationUtils.CreateCompilationWithReferences(source, references:=LatestVbReferences, options:=TestOptions.DebugDll)

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
                <entry offset="0x0" hidden="true"/>
                <entry offset="0x7" hidden="true"/>
                <entry offset="0xe" startLine="11" startColumn="5" endLine="11" endColumn="68"/>
                <entry offset="0xf" startLine="12" startColumn="9" endLine="12" endColumn="25"/>
                <entry offset="0x1e" hidden="true"/>
                <entry offset="0x7a" startLine="13" startColumn="9" endLine="13" endColumn="17"/>
                <entry offset="0x7e" hidden="true"/>
                <entry offset="0x86" hidden="true"/>
                <entry offset="0xa3" startLine="14" startColumn="5" endLine="14" endColumn="17"/>
                <entry offset="0xad" hidden="true"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xbb">
                <importsforward declaringType="Module1" methodName="Main" parameterNames="args"/>
            </scope>
            <asyncInfo>
                <kickoffMethod declaringType="Module1" methodName="F" parameterNames="a"/>
                <await yield="0x30" resume="0x4c" declaringType="Module1+VB$StateMachine_1_F" methodName="MoveNext"/>
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
                <entry offset="0x0" hidden="true"/>
                <entry offset="0x7" hidden="true"/>
                <entry offset="0x5d" startLine="16" startColumn="5" endLine="16" endColumn="34"/>
                <entry offset="0x5e" startLine="17" startColumn="9" endLine="23" endColumn="34"/>
                <entry offset="0x70" hidden="true"/>
                <entry offset="0xf1" hidden="true"/>
                <entry offset="0x176" hidden="true"/>
                <entry offset="0x1f0" hidden="true"/>
                <entry offset="0x269" hidden="true"/>
                <entry offset="0x2e2" hidden="true"/>
                <entry offset="0x363" hidden="true"/>
                <entry offset="0x3e4" hidden="true"/>
                <entry offset="0x463" hidden="true"/>
                <entry offset="0x4bf" startLine="24" startColumn="5" endLine="24" endColumn="17"/>
                <entry offset="0x4c1" hidden="true"/>
                <entry offset="0x4c9" hidden="true"/>
                <entry offset="0x4e6" startLine="24" startColumn="5" endLine="24" endColumn="17"/>
                <entry offset="0x4f0" hidden="true"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x4fd">
                <importsforward declaringType="Module1" methodName="Main" parameterNames="args"/>
            </scope>
            <asyncInfo>
                <kickoffMethod declaringType="Module1" methodName="Test"/>
                <await yield="0x82" resume="0xa2" declaringType="Module1+VB$StateMachine_2_Test" methodName="MoveNext"/>
                <await yield="0x103" resume="0x123" declaringType="Module1+VB$StateMachine_2_Test" methodName="MoveNext"/>
                <await yield="0x188" resume="0x1a7" declaringType="Module1+VB$StateMachine_2_Test" methodName="MoveNext"/>
                <await yield="0x202" resume="0x222" declaringType="Module1+VB$StateMachine_2_Test" methodName="MoveNext"/>
                <await yield="0x27b" resume="0x29b" declaringType="Module1+VB$StateMachine_2_Test" methodName="MoveNext"/>
                <await yield="0x2f4" resume="0x314" declaringType="Module1+VB$StateMachine_2_Test" methodName="MoveNext"/>
                <await yield="0x375" resume="0x395" declaringType="Module1+VB$StateMachine_2_Test" methodName="MoveNext"/>
                <await yield="0x3f6" resume="0x415" declaringType="Module1+VB$StateMachine_2_Test" methodName="MoveNext"/>
                <await yield="0x475" resume="0x491" declaringType="Module1+VB$StateMachine_2_Test" methodName="MoveNext"/>
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
                <entry offset="0x0" hidden="true"/>
                <entry offset="0x7" hidden="true"/>
                <entry offset="0xe" startLine="26" startColumn="5" endLine="26" endColumn="18"/>
                <entry offset="0xf" startLine="27" startColumn="9" endLine="27" endColumn="25"/>
                <entry offset="0x1d" hidden="true"/>
                <entry offset="0x78" startLine="28" startColumn="5" endLine="28" endColumn="12"/>
                <entry offset="0x7a" hidden="true"/>
                <entry offset="0x82" hidden="true"/>
                <entry offset="0x9f" startLine="28" startColumn="5" endLine="28" endColumn="12"/>
                <entry offset="0xa9" hidden="true"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xb6">
                <importsforward declaringType="Module1" methodName="Main" parameterNames="args"/>
            </scope>
            <asyncInfo>
                <catchHandler offset="0x7a"/>
                <kickoffMethod declaringType="Module1" methodName="S"/>
                <await yield="0x2f" resume="0x4a" declaringType="Module1+VB$StateMachine_3_S" methodName="MoveNext"/>
            </asyncInfo>
        </method>
    </methods>
</symbols>)
        End Sub

        <Fact(), WorkItem(827337, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/827337"), WorkItem(836491, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/836491")>
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
                <entry offset="0x0" hidden="true"/>
                <entry offset="0x7" hidden="true"/>
                <entry offset="0x11" startLine="5" startColumn="5" endLine="5" endColumn="50"/>
                <entry offset="0x12" hidden="true"/>
                <entry offset="0x1d" startLine="6" startColumn="13" endLine="6" endColumn="29"/>
                <entry offset="0x29" startLine="7" startColumn="13" endLine="7" endColumn="29"/>
                <entry offset="0x35" startLine="9" startColumn="13" endLine="9" endColumn="53"/>
                <entry offset="0x4c" startLine="11" startColumn="9" endLine="11" endColumn="55"/>
                <entry offset="0x7b" hidden="true"/>
                <entry offset="0xd9" startLine="12" startColumn="9" endLine="12" endColumn="21"/>
                <entry offset="0xea" startLine="13" startColumn="9" endLine="13" endColumn="21"/>
                <entry offset="0xfb" startLine="14" startColumn="5" endLine="14" endColumn="17"/>
                <entry offset="0xfd" hidden="true"/>
                <entry offset="0x105" hidden="true"/>
                <entry offset="0x122" startLine="14" startColumn="5" endLine="14" endColumn="17"/>
                <entry offset="0x12c" hidden="true"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x139">
                <importsforward declaringType="C+_Closure$__1-0" methodName="_Lambda$__0"/>
                <local name="$VB$ResumableLocal_$VB$Closure_$0" il_index="0" il_start="0x0" il_end="0x139" attributes="0"/>
                <local name="$VB$ResumableLocal_a$1" il_index="1" il_start="0x0" il_end="0x139" attributes="0"/>
            </scope>
            <asyncInfo>
                <kickoffMethod declaringType="C" methodName="Async_Lambda"/>
                <await yield="0x8d" resume="0xab" declaringType="C+VB$StateMachine_1_Async_Lambda" methodName="MoveNext"/>
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
                <entry offset="0x0" hidden="true"/>
                <entry offset="0x7" hidden="true"/>
                <entry offset="0xa" hidden="true"/>
                <entry offset="0x15" startLine="6" startColumn="13" endLine="6" endColumn="29"/>
                <entry offset="0x21" startLine="7" startColumn="13" endLine="7" endColumn="29"/>
                <entry offset="0x2d" startLine="11" startColumn="9" endLine="11" endColumn="55"/>
                <entry offset="0x5c" hidden="true"/>
                <entry offset="0xb3" startLine="12" startColumn="9" endLine="12" endColumn="21"/>
                <entry offset="0xc4" startLine="13" startColumn="9" endLine="13" endColumn="21"/>
                <entry offset="0xd5" startLine="14" startColumn="5" endLine="14" endColumn="17"/>
                <entry offset="0xd7" hidden="true"/>
                <entry offset="0xde" hidden="true"/>
                <entry offset="0xf9" startLine="14" startColumn="5" endLine="14" endColumn="17"/>
                <entry offset="0x103" hidden="true"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x10f">
                <importsforward declaringType="C+_Closure$__1-0" methodName="_Lambda$__0"/>
                <local name="$VB$ResumableLocal_$VB$Closure_$0" il_index="0" il_start="0x0" il_end="0x10f" attributes="0"/>
            </scope>
            <asyncInfo>
                <kickoffMethod declaringType="C" methodName="Async_Lambda"/>
                <await yield="0x6e" resume="0x88" declaringType="C+VB$StateMachine_1_Async_Lambda" methodName="MoveNext"/>
            </asyncInfo>
        </method>
    </methods>
</symbols>)
        End Sub

        <Fact(), WorkItem(827337, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/827337"), WorkItem(836491, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/836491")>
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
                <entry offset="0x0" hidden="true"/>
                <entry offset="0x7" hidden="true"/>
                <entry offset="0xe" startLine="5" startColumn="5" endLine="5" endColumn="52"/>
                <entry offset="0xf" startLine="6" startColumn="13" endLine="6" endColumn="29"/>
                <entry offset="0x16" startLine="7" startColumn="13" endLine="7" endColumn="29"/>
                <entry offset="0x1d" startLine="9" startColumn="9" endLine="9" endColumn="55"/>
                <entry offset="0x42" hidden="true"/>
                <entry offset="0xa0" startLine="10" startColumn="9" endLine="10" endColumn="21"/>
                <entry offset="0xac" startLine="11" startColumn="9" endLine="11" endColumn="21"/>
                <entry offset="0xb8" startLine="12" startColumn="5" endLine="12" endColumn="17"/>
                <entry offset="0xba" hidden="true"/>
                <entry offset="0xc2" hidden="true"/>
                <entry offset="0xdf" startLine="12" startColumn="5" endLine="12" endColumn="17"/>
                <entry offset="0xe9" hidden="true"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xf6">
                <namespace name="System" importlevel="file"/>
                <namespace name="System.Threading.Tasks" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="$VB$ResumableLocal_x$0" il_index="0" il_start="0x0" il_end="0xf6" attributes="0"/>
                <local name="$VB$ResumableLocal_y$1" il_index="1" il_start="0x0" il_end="0xf6" attributes="0"/>
            </scope>
            <asyncInfo>
                <kickoffMethod declaringType="C" methodName="Async_NoLambda"/>
                <await yield="0x54" resume="0x72" declaringType="C+VB$StateMachine_1_Async_NoLambda" methodName="MoveNext"/>
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
                <entry offset="0x0" hidden="true"/>
                <entry offset="0x7" hidden="true"/>
                <entry offset="0xa" startLine="6" startColumn="13" endLine="6" endColumn="29"/>
                <entry offset="0x11" startLine="7" startColumn="13" endLine="7" endColumn="29"/>
                <entry offset="0x18" startLine="9" startColumn="9" endLine="9" endColumn="55"/>
                <entry offset="0x3d" hidden="true"/>
                <entry offset="0x91" startLine="10" startColumn="9" endLine="10" endColumn="21"/>
                <entry offset="0x9d" startLine="11" startColumn="9" endLine="11" endColumn="21"/>
                <entry offset="0xa9" startLine="12" startColumn="5" endLine="12" endColumn="17"/>
                <entry offset="0xab" hidden="true"/>
                <entry offset="0xb2" hidden="true"/>
                <entry offset="0xcd" startLine="12" startColumn="5" endLine="12" endColumn="17"/>
                <entry offset="0xd7" hidden="true"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xe3">
                <namespace name="System" importlevel="file"/>
                <namespace name="System.Threading.Tasks" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="$VB$ResumableLocal_x$0" il_index="0" il_start="0x0" il_end="0xe3" attributes="0"/>
                <local name="$VB$ResumableLocal_y$1" il_index="1" il_start="0x0" il_end="0xe3" attributes="0"/>
            </scope>
            <asyncInfo>
                <kickoffMethod declaringType="C" methodName="Async_NoLambda"/>
                <await yield="0x4f" resume="0x66" declaringType="C+VB$StateMachine_1_Async_NoLambda" methodName="MoveNext"/>
            </asyncInfo>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(1085911, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1085911")>
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

            Dim c = CreateCompilationWithReferences(source, references:=LatestVbReferences, options:=TestOptions.DebugDll)
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
                <entry offset="0x0" hidden="true"/>
                <entry offset="0x7" startLine="5" startColumn="5" endLine="5" endColumn="50"/>
                <entry offset="0x8" startLine="6" startColumn="9" endLine="6" endColumn="17"/>
                <entry offset="0xc" hidden="true"/>
                <entry offset="0x13" hidden="true"/>
                <entry offset="0x2f" startLine="7" startColumn="5" endLine="7" endColumn="17"/>
                <entry offset="0x39" hidden="true"/>
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

        <Fact>
        Public Sub AsyncAndClosure()
            Dim source =
<compilation>
    <file>
Imports System
Imports System.Threading.Tasks
Module M
    Async Function F() As Task(Of Boolean)
        Dim z = Await Task.FromResult(1)
 
        Dim x = Sub()
                    Console.WriteLine(z)
                End Sub
        Return False
    End Function
End Module
    </file>
</compilation>

            Dim v = CompileAndVerify(source, LatestVbReferences, options:=TestOptions.DebugDll)

            v.VerifyIL("M.VB$StateMachine_0_F.MoveNext", "
{
  // Code size      266 (0x10a)
  .maxstack  3
  .locals init (Boolean V_0,
                Integer V_1,
                System.Threading.Tasks.Task(Of Boolean) V_2,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_3,
                M.VB$StateMachine_0_F V_4,
                Integer V_5,
                System.Exception V_6)
 ~IL_0000:  ldarg.0
  IL_0001:  ldfld      ""M.VB$StateMachine_0_F.$State As Integer""
  IL_0006:  stloc.1
  .try
  {
   ~IL_0007:  ldloc.1
    IL_0008:  brfalse.s  IL_000c
    IL_000a:  br.s       IL_000e
    IL_000c:  br.s       IL_006f
   -IL_000e:  nop
   ~IL_000f:  ldarg.0
    IL_0010:  newobj     ""Sub M._Closure$__0-0..ctor()""
    IL_0015:  stfld      ""M.VB$StateMachine_0_F.$VB$ResumableLocal_$VB$Closure_$0 As M._Closure$__0-0""
   -IL_001a:  ldarg.0
    IL_001b:  ldarg.0
    IL_001c:  ldfld      ""M.VB$StateMachine_0_F.$VB$ResumableLocal_$VB$Closure_$0 As M._Closure$__0-0""
    IL_0021:  stfld      ""M.VB$StateMachine_0_F.$U1 As M._Closure$__0-0""
    IL_0026:  ldarg.0
    IL_0027:  ldfld      ""M.VB$StateMachine_0_F.$U1 As M._Closure$__0-0""
    IL_002c:  ldfld      ""M._Closure$__0-0.$VB$Local_z As Integer""
    IL_0031:  pop
    IL_0032:  ldc.i4.1
    IL_0033:  call       ""Function System.Threading.Tasks.Task.FromResult(Of Integer)(Integer) As System.Threading.Tasks.Task(Of Integer)""
    IL_0038:  callvirt   ""Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_003d:  stloc.3
   ~IL_003e:  ldloca.s   V_3
    IL_0040:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean""
    IL_0045:  brtrue.s   IL_008d
    IL_0047:  ldarg.0
    IL_0048:  ldc.i4.0
    IL_0049:  dup
    IL_004a:  stloc.1
    IL_004b:  stfld      ""M.VB$StateMachine_0_F.$State As Integer""
   <IL_0050:  ldarg.0
    IL_0051:  ldloc.3
    IL_0052:  stfld      ""M.VB$StateMachine_0_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_0057:  ldarg.0
    IL_0058:  ldflda     ""M.VB$StateMachine_0_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Boolean)""
    IL_005d:  ldloca.s   V_3
    IL_005f:  ldarg.0
    IL_0060:  stloc.s    V_4
    IL_0062:  ldloca.s   V_4
    IL_0064:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Boolean).AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), M.VB$StateMachine_0_F)(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef M.VB$StateMachine_0_F)""
    IL_0069:  nop
    IL_006a:  leave      IL_0109
   >IL_006f:  ldarg.0
    IL_0070:  ldc.i4.m1
    IL_0071:  dup
    IL_0072:  stloc.1
    IL_0073:  stfld      ""M.VB$StateMachine_0_F.$State As Integer""
    IL_0078:  ldarg.0
    IL_0079:  ldfld      ""M.VB$StateMachine_0_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_007e:  stloc.3
    IL_007f:  ldarg.0
    IL_0080:  ldflda     ""M.VB$StateMachine_0_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_0085:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_008b:  br.s       IL_008d
    IL_008d:  ldarg.0
    IL_008e:  ldfld      ""M.VB$StateMachine_0_F.$U1 As M._Closure$__0-0""
    IL_0093:  ldloca.s   V_3
    IL_0095:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer""
    IL_009a:  stloc.s    V_5
    IL_009c:  ldloca.s   V_3
    IL_009e:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter(Of Integer)""
    IL_00a4:  ldloc.s    V_5
    IL_00a6:  stfld      ""M._Closure$__0-0.$VB$Local_z As Integer""
    IL_00ab:  ldarg.0
    IL_00ac:  ldnull
    IL_00ad:  stfld      ""M.VB$StateMachine_0_F.$U1 As M._Closure$__0-0""
   -IL_00b2:  ldarg.0
    IL_00b3:  ldarg.0
    IL_00b4:  ldfld      ""M.VB$StateMachine_0_F.$VB$ResumableLocal_$VB$Closure_$0 As M._Closure$__0-0""
    IL_00b9:  ldftn      ""Sub M._Closure$__0-0._Lambda$__0()""
    IL_00bf:  newobj     ""Sub VB$AnonymousDelegate_0..ctor(Object, System.IntPtr)""
    IL_00c4:  stfld      ""M.VB$StateMachine_0_F.$VB$ResumableLocal_x$1 As <generated method>""
   -IL_00c9:  ldc.i4.0
    IL_00ca:  stloc.0
    IL_00cb:  leave.s    IL_00f2
  }
  catch System.Exception
  {
   ~IL_00cd:  dup
    IL_00ce:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)""
    IL_00d3:  stloc.s    V_6
   ~IL_00d5:  ldarg.0
    IL_00d6:  ldc.i4.s   -2
    IL_00d8:  stfld      ""M.VB$StateMachine_0_F.$State As Integer""
    IL_00dd:  ldarg.0
    IL_00de:  ldflda     ""M.VB$StateMachine_0_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Boolean)""
    IL_00e3:  ldloc.s    V_6
    IL_00e5:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Boolean).SetException(System.Exception)""
    IL_00ea:  nop
    IL_00eb:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()""
    IL_00f0:  leave.s    IL_0109
  }
 -IL_00f2:  ldarg.0
  IL_00f3:  ldc.i4.s   -2
  IL_00f5:  dup
  IL_00f6:  stloc.1
  IL_00f7:  stfld      ""M.VB$StateMachine_0_F.$State As Integer""
 ~IL_00fc:  ldarg.0
  IL_00fd:  ldflda     ""M.VB$StateMachine_0_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Boolean)""
  IL_0102:  ldloc.0
  IL_0103:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Boolean).SetResult(Boolean)""
  IL_0108:  nop
  IL_0109:  ret
}
", sequencePoints:="M+VB$StateMachine_0_F.MoveNext")
        End Sub
    End Class
End Namespace