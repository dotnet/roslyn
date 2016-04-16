' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.PDB
    Public Class PDBIteratorTests
        Inherits BasicTestBase

        <Fact, WorkItem(2736, "https://github.com/dotnet/roslyn/issues/2736")>
        Public Sub SimpleIterator()
            Dim source =
<compilation>
    <file>
Imports System.Collections.Generic

Class C
    Public Iterator Function F() As IEnumerable(Of Integer)
        Yield 1
    End Function
End Class
    </file>
</compilation>

            Dim v = CompileAndVerify(source, options:=TestOptions.DebugDll)

            v.VerifyIL("C.VB$StateMachine_1_F.MoveNext", "
{
  // Code size       63 (0x3f)
  .maxstack  3
  .locals init (Boolean V_0,
                Integer V_1)
 ~IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_0006:  stloc.1
  IL_0007:  ldloc.1
  IL_0008:  brfalse.s  IL_0012
  IL_000a:  br.s       IL_000c
  IL_000c:  ldloc.1
  IL_000d:  ldc.i4.1
  IL_000e:  beq.s      IL_0014
  IL_0010:  br.s       IL_0016
  IL_0012:  br.s       IL_0018
  IL_0014:  br.s       IL_0034
  IL_0016:  ldc.i4.0
  IL_0017:  ret
  IL_0018:  ldarg.0
  IL_0019:  ldc.i4.m1
  IL_001a:  dup
  IL_001b:  stloc.1
  IL_001c:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
 -IL_0021:  nop
 -IL_0022:  ldarg.0
  IL_0023:  ldc.i4.1
  IL_0024:  stfld      ""C.VB$StateMachine_1_F.$Current As Integer""
  IL_0029:  ldarg.0
  IL_002a:  ldc.i4.1
  IL_002b:  dup
  IL_002c:  stloc.1
  IL_002d:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
  IL_0032:  ldc.i4.1
  IL_0033:  ret
  IL_0034:  ldarg.0
  IL_0035:  ldc.i4.m1
  IL_0036:  dup
  IL_0037:  stloc.1
  IL_0038:  stfld      ""C.VB$StateMachine_1_F.$State As Integer""
 -IL_003d:  ldc.i4.0
  IL_003e:  ret
}
", sequencePoints:="C+VB$StateMachine_1_F.MoveNext")
        End Sub

        <Fact, WorkItem(651996, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/651996")>
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
                    <slot kind="20" offset="4"/>
                    <slot kind="27" offset="4"/>
                    <slot kind="21" offset="4"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" hidden="true"/>
                <entry offset="0x2c" startLine="7" startColumn="13" endLine="7" endColumn="33"/>
                <entry offset="0x2d" startLine="8" startColumn="17" endLine="8" endColumn="24"/>
                <entry offset="0x48" startLine="9" startColumn="17" endLine="9" endColumn="24"/>
                <entry offset="0x68" startLine="10" startColumn="13" endLine="10" endColumn="25"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x6a">
                <importsforward declaringType="Program" methodName="Main" parameterNames="args"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <Fact, WorkItem(651996, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/651996"), WorkItem(789705, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/789705")>
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
                    <slot kind="1" offset="54"/>
                    <slot kind="1" offset="139"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" hidden="true"/>
                <entry offset="0x41" startLine="12" startColumn="5" endLine="12" endColumn="55"/>
                <entry offset="0x42" startLine="13" startColumn="13" endLine="13" endColumn="19"/>
                <entry offset="0x4e" startLine="14" startColumn="9" endLine="14" endColumn="20"/>
                <entry offset="0x58" startLine="16" startColumn="9" endLine="16" endColumn="26"/>
                <entry offset="0x6b" hidden="true"/>
                <entry offset="0x80" startLine="17" startColumn="13" endLine="17" endColumn="20"/>
                <entry offset="0xa0" startLine="18" startColumn="13" endLine="18" endColumn="20"/>
                <entry offset="0xc0" startLine="19" startColumn="9" endLine="19" endColumn="13"/>
                <entry offset="0xc1" hidden="true"/>
                <entry offset="0xcf" hidden="true"/>
                <entry offset="0xe0" hidden="true"/>
                <entry offset="0xe3" startLine="21" startColumn="9" endLine="21" endColumn="28"/>
                <entry offset="0xf5" hidden="true"/>
                <entry offset="0x10e" startLine="22" startColumn="13" endLine="22" endColumn="44"/>
                <entry offset="0x133" startLine="23" startColumn="13" endLine="23" endColumn="44"/>
                <entry offset="0x158" startLine="24" startColumn="9" endLine="24" endColumn="13"/>
                <entry offset="0x159" hidden="true"/>
                <entry offset="0x167" hidden="true"/>
                <entry offset="0x17b" hidden="true"/>
                <entry offset="0x181" startLine="26" startColumn="5" endLine="26" endColumn="17"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x183">
                <importsforward declaringType="Module1" methodName="Main"/>
                <scope startOffset="0x41" endOffset="0x182">
                    <local name="$VB$ResumableLocal_arr$0" il_index="0" il_start="0x41" il_end="0x182" attributes="0"/>
                    <scope startOffset="0x6d" endOffset="0xce">
                        <local name="$VB$ResumableLocal_x$3" il_index="3" il_start="0x6d" il_end="0xce" attributes="0"/>
                    </scope>
                    <scope startOffset="0xf7" endOffset="0x166">
                        <local name="$VB$ResumableLocal_x$6" il_index="6" il_start="0xf7" il_end="0x166" attributes="0"/>
                    </scope>
                </scope>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <Fact, WorkItem(827337, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/827337"), WorkItem(836491, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/836491")>
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
                <entry offset="0x0" hidden="true"/>
                <entry offset="0x19" hidden="true"/>
                <entry offset="0x24" startLine="6" startColumn="13" endLine="6" endColumn="29"/>
                <entry offset="0x30" startLine="7" startColumn="13" endLine="7" endColumn="29"/>
                <entry offset="0x3c" startLine="9" startColumn="13" endLine="9" endColumn="53"/>
                <entry offset="0x43" startLine="11" startColumn="9" endLine="11" endColumn="20"/>
                <entry offset="0x74" startLine="12" startColumn="9" endLine="12" endColumn="21"/>
                <entry offset="0x85" startLine="13" startColumn="9" endLine="13" endColumn="21"/>
                <entry offset="0x96" startLine="14" startColumn="5" endLine="14" endColumn="17"/>
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

        <Fact, WorkItem(827337, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/827337"), WorkItem(836491, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/836491")>
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
                <entry offset="0x0" hidden="true"/>
                <entry offset="0x19" hidden="true"/>
                <entry offset="0x1f" startLine="6" startColumn="13" endLine="6" endColumn="29"/>
                <entry offset="0x26" startLine="7" startColumn="13" endLine="7" endColumn="29"/>
                <entry offset="0x2d" startLine="11" startColumn="9" endLine="11" endColumn="20"/>
                <entry offset="0x54" startLine="12" startColumn="5" endLine="12" endColumn="17"/>
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

        <Fact, WorkItem(827337, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/827337"), WorkItem(836491, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/836491")>
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
                <entry offset="0x0" hidden="true"/>
                <entry offset="0x19" startLine="6" startColumn="13" endLine="6" endColumn="29"/>
                <entry offset="0x20" startLine="7" startColumn="13" endLine="7" endColumn="29"/>
                <entry offset="0x27" startLine="8" startColumn="9" endLine="8" endColumn="20"/>
                <entry offset="0x4e" startLine="9" startColumn="9" endLine="9" endColumn="21"/>
                <entry offset="0x5a" startLine="10" startColumn="9" endLine="10" endColumn="21"/>
                <entry offset="0x66" startLine="11" startColumn="5" endLine="11" endColumn="17"/>
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

        <Fact(), WorkItem(827337, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/827337"), WorkItem(836491, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/836491")>
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
                <entry offset="0x0" hidden="true"/>
                <entry offset="0x19" startLine="6" startColumn="13" endLine="6" endColumn="29"/>
                <entry offset="0x1b" startLine="7" startColumn="13" endLine="7" endColumn="29"/>
                <entry offset="0x1d" startLine="8" startColumn="9" endLine="8" endColumn="20"/>
                <entry offset="0x3a" startLine="9" startColumn="5" endLine="9" endColumn="17"/>
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
        <Fact, WorkItem(667579, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/667579")>
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
                <entry offset="0x0" hidden="true"/>
                <entry offset="0x2c" startLine="13" startColumn="5" endLine="13" endColumn="55"/>
                <entry offset="0x2d" startLine="14" startColumn="9" endLine="14" endColumn="16"/>
                <entry offset="0x48" startLine="15" startColumn="9" endLine="15" endColumn="16"/>
                <entry offset="0x63" startLine="16" startColumn="5" endLine="16" endColumn="17"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x65">
                <importsforward declaringType="Module1" methodName="Main"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

    End Class
End Namespace
