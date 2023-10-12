' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class CodeCallGenTests
        Inherits BasicTestBase

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Call_Class()
            Dim comp =
<compilation>
    <file>
Imports System

Interface IMoveable
    Sub GetName(x As Integer)
End Interface

Class Item
    Implements IMoveable

    Public Property Name As String

    Public Sub GetName(x As Integer) Implements IMoveable.GetName
        Console.WriteLine("Position GetName for item '{0}'", Me.Name)
    End Sub
End Class

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1)

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2)
    End Sub

    Private Shared Sub Call1(Of T As {Class, IMoveable})(item As T)
        item.GetName(GetOffset(item))
    End Sub

    Private Shared Sub Call2(Of T As {IMoveable})(item As T)
        item.GetName(GetOffset(item))
    End Sub

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function
End Class
    </file>
</compilation>

            ' Wrong output on some frameworks
            '            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
            '"
            'Position GetName for item '1'
            'Position GetName for item '2'
            '").VerifyDiagnostics()
            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe).VerifyDiagnostics()

            'Wrong IL
            verifier.VerifyIL("Program.Call1(Of T)",
            <![CDATA[
{
  // Code size       21 (0x15)
  .maxstack  2
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldarga.s   V_0
  IL_0004:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0009:  constrained. "T"
  IL_000f:  callvirt   "Sub IMoveable.GetName(Integer)"
  IL_0014:  ret
}
]]>)

            'Wrong IL
            verifier.VerifyIL("Program.Call2(Of T)",
            <![CDATA[
{
  // Code size       21 (0x15)
  .maxstack  2
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldarga.s   V_0
  IL_0004:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0009:  constrained. "T"
  IL_000f:  callvirt   "Sub IMoveable.GetName(Integer)"
  IL_0014:  ret
}
]]>)
        End Sub

        <Theory>
        <CombinatorialData>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Call_Class_InWith(asRValue As Boolean)

            Dim leftParen As String = ""
            Dim rightParen As String = ""

            If asRValue Then
                leftParen = "("
                rightParen = ")"
            End If

            Dim comp =
<compilation>
    <file>
Imports System

Interface IMoveable
    Sub GetName(x As Integer)
End Interface

Class Item
    Implements IMoveable

    Public Property Name As String

    Public Sub GetName(x As Integer) Implements IMoveable.GetName
        Console.WriteLine("Position GetName for item '{0}'", Me.Name)
    End Sub
End Class

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1)

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2)

        Dim item3 = New Item With {.Name = "3"}
        Call3(item3)
    End Sub

    Private Shared Sub Call1(Of T As {Class, IMoveable})(item As T)
        With <%= leftParen %>item<%= rightParen %>
            call .GetName(GetOffset(item))
        End With
    End Sub

    Private Shared Sub Call2(Of T As {IMoveable})(item As T)
        With <%= leftParen %>item<%= rightParen %>
            call .GetName(GetOffset(item))
        End With
    End Sub

    Private Shared Sub Call3(item As Item)
        With <%= leftParen %>item<%= rightParen %>
            call .GetName(GetOffset(item))
        End With
    End Sub

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function
End Class
    </file>
</compilation>

            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position GetName for item '1'
Position GetName for item '2'
Position GetName for item '3'
").VerifyDiagnostics()

            'Wrong IL
            verifier.VerifyIL("Program.Call1(Of T)",
                              If(asRValue,
            <![CDATA[
{
  // Code size       19 (0x13)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  box        "T"
  IL_0006:  ldarga.s   V_0
  IL_0008:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_000d:  callvirt   "Sub IMoveable.GetName(Integer)"
  IL_0012:  ret
}
]]>,
            <![CDATA[
{
  // Code size       21 (0x15)
  .maxstack  2
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldarga.s   V_0
  IL_0004:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0009:  constrained. "T"
  IL_000f:  callvirt   "Sub IMoveable.GetName(Integer)"
  IL_0014:  ret
}
]]>))

            'Wrong IL
            verifier.VerifyIL("Program.Call2(Of T)",
                              If(asRValue,
            <![CDATA[
{
  // Code size       31 (0x1f)
  .maxstack  2
  .locals init (T V_0) //$W0
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  ldarga.s   V_0
  IL_0006:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_000b:  constrained. "T"
  IL_0011:  callvirt   "Sub IMoveable.GetName(Integer)"
  IL_0016:  ldloca.s   V_0
  IL_0018:  initobj    "T"
  IL_001e:  ret
}
]]>,
            <![CDATA[
{
  // Code size       21 (0x15)
  .maxstack  2
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldarga.s   V_0
  IL_0004:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0009:  constrained. "T"
  IL_000f:  callvirt   "Sub IMoveable.GetName(Integer)"
  IL_0014:  ret
}
]]>))
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Call_Struct()
            Dim comp =
<compilation>
    <file>
Imports System

Interface IMoveable
    Sub GetName(x As Integer)
End Interface

Structure Item
    Implements IMoveable

    Public Property Name As String

    Public Sub GetName(x As Integer) Implements IMoveable.GetName
        Console.WriteLine("Position GetName for item '{0}'", Me.Name)
    End Sub
End Structure

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1)

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2)
    End Sub

    Private Shared Sub Call1(Of T As {Structure, IMoveable})(item As T)
        item.GetName(GetOffset(item))
    End Sub

    Private Shared Sub Call2(Of T As {IMoveable})(item As T)
        item.GetName(GetOffset(item))
    End Sub

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function
End Class
    </file>
</compilation>

            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position GetName for item '-1'
Position GetName for item '-2'
").VerifyDiagnostics()

            verifier.VerifyIL("Program.Call1(Of T)",
            <![CDATA[
{
  // Code size       21 (0x15)
  .maxstack  2
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldarga.s   V_0
  IL_0004:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0009:  constrained. "T"
  IL_000f:  callvirt   "Sub IMoveable.GetName(Integer)"
  IL_0014:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Call_Class_Ref()
            Dim comp =
<compilation>
    <file>
Imports System

Interface IMoveable
    Sub GetName(x As Integer)
End Interface

Class Item
    Implements IMoveable

    Public Property Name As String

    Public Sub GetName(x As Integer) Implements IMoveable.GetName
        Console.WriteLine("Position GetName for item '{0}'", Me.Name)
    End Sub
End Class

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1)

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2)
    End Sub

    Private Shared Sub Call1(Of T As {Class, IMoveable})(ByRef item As T)
        item.GetName(GetOffset(item))
    End Sub

    Private Shared Sub Call2(Of T As {IMoveable})(ByRef item As T)
        item.GetName(GetOffset(item))
    End Sub

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function
End Class
    </file>
</compilation>

            ' Wrong output on some frameworks
            '            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
            '"
            'Position GetName for item '1'
            'Position GetName for item '2'
            '").VerifyDiagnostics()
            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe).VerifyDiagnostics()

            'Wrong IL
            verifier.VerifyIL("Program.Call1(Of T)",
            <![CDATA[
{
  // Code size       19 (0x13)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0007:  constrained. "T"
  IL_000d:  callvirt   "Sub IMoveable.GetName(Integer)"
  IL_0012:  ret
}
]]>)

            'Wrong IL
            verifier.VerifyIL("Program.Call2(Of T)",
            <![CDATA[
{
  // Code size       19 (0x13)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0007:  constrained. "T"
  IL_000d:  callvirt   "Sub IMoveable.GetName(Integer)"
  IL_0012:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Call_Struct_Ref()
            Dim comp =
<compilation>
    <file>
Imports System

Interface IMoveable
    Sub GetName(x As Integer)
End Interface

Structure Item
    Implements IMoveable

    Public Property Name As String

    Public Sub GetName(x As Integer) Implements IMoveable.GetName
        Console.WriteLine("Position GetName for item '{0}'", Me.Name)
    End Sub
End Structure

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1)

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2)
    End Sub

    Private Shared Sub Call1(Of T As {Structure, IMoveable})(ByRef item As T)
        item.GetName(GetOffset(item))
    End Sub

    Private Shared Sub Call2(Of T As {IMoveable})(ByRef item As T)
        item.GetName(GetOffset(item))
    End Sub

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function
End Class
    </file>
</compilation>

            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position GetName for item '-1'
Position GetName for item '-2'
").VerifyDiagnostics()

            verifier.VerifyIL("Program.Call1(Of T)",
            <![CDATA[
{
  // Code size       19 (0x13)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0007:  constrained. "T"
  IL_000d:  callvirt   "Sub IMoveable.GetName(Integer)"
  IL_0012:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Call_Class_Async_01()
            Dim comp =
<compilation>
    <file>
Imports System
Imports System.Threading.Tasks

Interface IMoveable
    Sub GetName(x As Integer)
End Interface

Class Item
    Implements IMoveable

    Public Property Name As String

    Public Sub GetName(x As Integer) Implements IMoveable.GetName
        Console.WriteLine("Position GetName for item '{0}'", Me.Name)
    End Sub
End Class

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1).Wait()

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2).Wait()
    End Sub

    Private Shared Async Function Call1(Of T As {Class, IMoveable})(item As T) As Task
        item.GetName(await GetOffsetAsync(GetOffset(item)))
    End Function

    Private Shared Async Function Call2(Of T As {IMoveable})(item As T) As Task
        item.GetName(await GetOffsetAsync(GetOffset(item)))
    End Function

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function

    Shared Function GetOffsetAsync(i As Integer) As Task(Of Integer)
        Return Task.FromResult(i)
    End Function
End Class
    </file>
</compilation>

            ' Wrong output
            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position GetName for item '-1'
Position GetName for item '-2'
").VerifyDiagnostics()

            'Wrong IL
            verifier.VerifyIL("Program.VB$StateMachine_2_Call1(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      191 (0xbf)
  .maxstack  3
  .locals init (Integer V_0,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_1,
                System.Exception V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0049
    IL_000a:  ldarg.0
    IL_000b:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0010:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_0015:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_001a:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_001f:  stloc.1
    IL_0020:  ldloca.s   V_1
    IL_0022:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_0027:  brtrue.s   IL_0065
    IL_0029:  ldarg.0
    IL_002a:  ldc.i4.0
    IL_002b:  dup
    IL_002c:  stloc.0
    IL_002d:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0032:  ldarg.0
    IL_0033:  ldloc.1
    IL_0034:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0039:  ldarg.0
    IL_003a:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_003f:  ldloca.s   V_1
    IL_0041:  ldarg.0
    IL_0042:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_2_Call1(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_2_Call1(Of SM$T))"
    IL_0047:  leave.s    IL_00be
    IL_0049:  ldarg.0
    IL_004a:  ldc.i4.m1
    IL_004b:  dup
    IL_004c:  stloc.0
    IL_004d:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0052:  ldarg.0
    IL_0053:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0058:  stloc.1
    IL_0059:  ldarg.0
    IL_005a:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_005f:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0065:  ldarg.0
    IL_0066:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_006b:  ldloca.s   V_1
    IL_006d:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_0072:  ldloca.s   V_1
    IL_0074:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_007a:  constrained. "SM$T"
    IL_0080:  callvirt   "Sub IMoveable.GetName(Integer)"
    IL_0085:  leave.s    IL_00a9
  }
  catch System.Exception
  {
    IL_0087:  dup
    IL_0088:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_008d:  stloc.2
    IL_008e:  ldarg.0
    IL_008f:  ldc.i4.s   -2
    IL_0091:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0096:  ldarg.0
    IL_0097:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_009c:  ldloc.2
    IL_009d:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_00a2:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_00a7:  leave.s    IL_00be
  }
  IL_00a9:  ldarg.0
  IL_00aa:  ldc.i4.s   -2
  IL_00ac:  dup
  IL_00ad:  stloc.0
  IL_00ae:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_00b3:  ldarg.0
  IL_00b4:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_00b9:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_00be:  ret
}
]]>)

            'Wrong IL
            verifier.VerifyIL("Program.VB$StateMachine_3_Call2(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      191 (0xbf)
  .maxstack  3
  .locals init (Integer V_0,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_1,
                System.Exception V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0049
    IL_000a:  ldarg.0
    IL_000b:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_0010:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_0015:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_001a:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_001f:  stloc.1
    IL_0020:  ldloca.s   V_1
    IL_0022:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_0027:  brtrue.s   IL_0065
    IL_0029:  ldarg.0
    IL_002a:  ldc.i4.0
    IL_002b:  dup
    IL_002c:  stloc.0
    IL_002d:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_0032:  ldarg.0
    IL_0033:  ldloc.1
    IL_0034:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0039:  ldarg.0
    IL_003a:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_003f:  ldloca.s   V_1
    IL_0041:  ldarg.0
    IL_0042:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_3_Call2(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_3_Call2(Of SM$T))"
    IL_0047:  leave.s    IL_00be
    IL_0049:  ldarg.0
    IL_004a:  ldc.i4.m1
    IL_004b:  dup
    IL_004c:  stloc.0
    IL_004d:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_0052:  ldarg.0
    IL_0053:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0058:  stloc.1
    IL_0059:  ldarg.0
    IL_005a:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_005f:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0065:  ldarg.0
    IL_0066:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_006b:  ldloca.s   V_1
    IL_006d:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_0072:  ldloca.s   V_1
    IL_0074:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_007a:  constrained. "SM$T"
    IL_0080:  callvirt   "Sub IMoveable.GetName(Integer)"
    IL_0085:  leave.s    IL_00a9
  }
  catch System.Exception
  {
    IL_0087:  dup
    IL_0088:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_008d:  stloc.2
    IL_008e:  ldarg.0
    IL_008f:  ldc.i4.s   -2
    IL_0091:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_0096:  ldarg.0
    IL_0097:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_009c:  ldloc.2
    IL_009d:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_00a2:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_00a7:  leave.s    IL_00be
  }
  IL_00a9:  ldarg.0
  IL_00aa:  ldc.i4.s   -2
  IL_00ac:  dup
  IL_00ad:  stloc.0
  IL_00ae:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
  IL_00b3:  ldarg.0
  IL_00b4:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_00b9:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_00be:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Call_Struct_Async_01()
            Dim comp =
<compilation>
    <file>
Imports System
Imports System.Threading.Tasks

Interface IMoveable
    Sub GetName(x As Integer)
End Interface

Structure Item
    Implements IMoveable

    Public Property Name As String

    Public Sub GetName(x As Integer) Implements IMoveable.GetName
        Console.WriteLine("Position GetName for item '{0}'", Me.Name)
    End Sub
End Structure

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1).Wait()

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2).Wait()
    End Sub

    Private Shared Async Function Call1(Of T As {Structure, IMoveable})(item As T) As Task
        item.GetName(await GetOffsetAsync(GetOffset(item)))
    End Function

    Private Shared Async Function Call2(Of T As {IMoveable})(item As T) As Task
        item.GetName(await GetOffsetAsync(GetOffset(item)))
    End Function

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function

    Shared Function GetOffsetAsync(i As Integer) As Task(Of Integer)
        Return Task.FromResult(i)
    End Function
End Class
    </file>
</compilation>

            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position GetName for item '-1'
Position GetName for item '-2'
").VerifyDiagnostics()

            verifier.VerifyIL("Program.VB$StateMachine_2_Call1(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      191 (0xbf)
  .maxstack  3
  .locals init (Integer V_0,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_1,
                System.Exception V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0049
    IL_000a:  ldarg.0
    IL_000b:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0010:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_0015:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_001a:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_001f:  stloc.1
    IL_0020:  ldloca.s   V_1
    IL_0022:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_0027:  brtrue.s   IL_0065
    IL_0029:  ldarg.0
    IL_002a:  ldc.i4.0
    IL_002b:  dup
    IL_002c:  stloc.0
    IL_002d:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0032:  ldarg.0
    IL_0033:  ldloc.1
    IL_0034:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0039:  ldarg.0
    IL_003a:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_003f:  ldloca.s   V_1
    IL_0041:  ldarg.0
    IL_0042:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_2_Call1(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_2_Call1(Of SM$T))"
    IL_0047:  leave.s    IL_00be
    IL_0049:  ldarg.0
    IL_004a:  ldc.i4.m1
    IL_004b:  dup
    IL_004c:  stloc.0
    IL_004d:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0052:  ldarg.0
    IL_0053:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0058:  stloc.1
    IL_0059:  ldarg.0
    IL_005a:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_005f:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0065:  ldarg.0
    IL_0066:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_006b:  ldloca.s   V_1
    IL_006d:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_0072:  ldloca.s   V_1
    IL_0074:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_007a:  constrained. "SM$T"
    IL_0080:  callvirt   "Sub IMoveable.GetName(Integer)"
    IL_0085:  leave.s    IL_00a9
  }
  catch System.Exception
  {
    IL_0087:  dup
    IL_0088:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_008d:  stloc.2
    IL_008e:  ldarg.0
    IL_008f:  ldc.i4.s   -2
    IL_0091:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0096:  ldarg.0
    IL_0097:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_009c:  ldloc.2
    IL_009d:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_00a2:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_00a7:  leave.s    IL_00be
  }
  IL_00a9:  ldarg.0
  IL_00aa:  ldc.i4.s   -2
  IL_00ac:  dup
  IL_00ad:  stloc.0
  IL_00ae:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_00b3:  ldarg.0
  IL_00b4:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_00b9:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_00be:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Call_Class_Async_02()
            Dim comp =
<compilation>
    <file>
Imports System
Imports System.Threading.Tasks

Interface IMoveable
    Sub GetName(x As Integer)
End Interface

Class Item
    Implements IMoveable

    Public Property Name As String

    Public Sub GetName(x As Integer) Implements IMoveable.GetName
        Console.WriteLine("Position GetName for item '{0}'", Me.Name)
    End Sub
End Class

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1).Wait()

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2).Wait()
    End Sub

    Private Shared Async Function Call1(Of T As {Class, IMoveable})(item As T) As Task
        await Task.Yield()
        item.GetName(await GetOffsetAsync(GetOffset(item)))
    End Function

    Private Shared Async Function Call2(Of T As {IMoveable})(item As T) As Task
        await Task.Yield()
        item.GetName(await GetOffsetAsync(GetOffset(item)))
    End Function

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function

    Shared Function GetOffsetAsync(i As Integer) As Task(Of Integer)
        Return Task.FromResult(i)
    End Function
End Class
    </file>
</compilation>

            ' Wrong output
            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position GetName for item '-1'
Position GetName for item '-2'
").VerifyDiagnostics()

            'Wrong IL
            verifier.VerifyIL("Program.VB$StateMachine_2_Call1(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      301 (0x12d)
  .maxstack  3
  .locals init (Integer V_0,
                System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_1,
                System.Runtime.CompilerServices.YieldAwaitable V_2,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004b
    IL_000a:  ldloc.0
    IL_000b:  ldc.i4.1
    IL_000c:  beq        IL_00b5
    IL_0011:  call       "Function System.Threading.Tasks.Task.Yield() As System.Runtime.CompilerServices.YieldAwaitable"
    IL_0016:  stloc.2
    IL_0017:  ldloca.s   V_2
    IL_0019:  call       "Function System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter() As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_001e:  stloc.1
    IL_001f:  ldloca.s   V_1
    IL_0021:  call       "Function System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.get_IsCompleted() As Boolean"
    IL_0026:  brtrue.s   IL_0067
    IL_0028:  ldarg.0
    IL_0029:  ldc.i4.0
    IL_002a:  dup
    IL_002b:  stloc.0
    IL_002c:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0031:  ldarg.0
    IL_0032:  ldloc.1
    IL_0033:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0038:  ldarg.0
    IL_0039:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_003e:  ldloca.s   V_1
    IL_0040:  ldarg.0
    IL_0041:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, Program.VB$StateMachine_2_Call1(Of SM$T))(ByRef System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ByRef Program.VB$StateMachine_2_Call1(Of SM$T))"
    IL_0046:  leave      IL_012c
    IL_004b:  ldarg.0
    IL_004c:  ldc.i4.m1
    IL_004d:  dup
    IL_004e:  stloc.0
    IL_004f:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0054:  ldarg.0
    IL_0055:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_005a:  stloc.1
    IL_005b:  ldarg.0
    IL_005c:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0061:  initobj    "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0067:  ldloca.s   V_1
    IL_0069:  call       "Sub System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()"
    IL_006e:  ldloca.s   V_1
    IL_0070:  initobj    "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0076:  ldarg.0
    IL_0077:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_007c:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_0081:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_0086:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_008b:  stloc.3
    IL_008c:  ldloca.s   V_3
    IL_008e:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_0093:  brtrue.s   IL_00d1
    IL_0095:  ldarg.0
    IL_0096:  ldc.i4.1
    IL_0097:  dup
    IL_0098:  stloc.0
    IL_0099:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_009e:  ldarg.0
    IL_009f:  ldloc.3
    IL_00a0:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A1 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00a5:  ldarg.0
    IL_00a6:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00ab:  ldloca.s   V_3
    IL_00ad:  ldarg.0
    IL_00ae:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_2_Call1(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_2_Call1(Of SM$T))"
    IL_00b3:  leave.s    IL_012c
    IL_00b5:  ldarg.0
    IL_00b6:  ldc.i4.m1
    IL_00b7:  dup
    IL_00b8:  stloc.0
    IL_00b9:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_00be:  ldarg.0
    IL_00bf:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A1 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00c4:  stloc.3
    IL_00c5:  ldarg.0
    IL_00c6:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$A1 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00cb:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00d1:  ldarg.0
    IL_00d2:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_00d7:  ldloca.s   V_3
    IL_00d9:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_00de:  ldloca.s   V_3
    IL_00e0:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00e6:  constrained. "SM$T"
    IL_00ec:  callvirt   "Sub IMoveable.GetName(Integer)"
    IL_00f1:  leave.s    IL_0117
  }
  catch System.Exception
  {
    IL_00f3:  dup
    IL_00f4:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_00f9:  stloc.s    V_4
    IL_00fb:  ldarg.0
    IL_00fc:  ldc.i4.s   -2
    IL_00fe:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0103:  ldarg.0
    IL_0104:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_0109:  ldloc.s    V_4
    IL_010b:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_0110:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_0115:  leave.s    IL_012c
  }
  IL_0117:  ldarg.0
  IL_0118:  ldc.i4.s   -2
  IL_011a:  dup
  IL_011b:  stloc.0
  IL_011c:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_0121:  ldarg.0
  IL_0122:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_0127:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_012c:  ret
}
]]>)

            'Wrong IL
            verifier.VerifyIL("Program.VB$StateMachine_3_Call2(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      301 (0x12d)
  .maxstack  3
  .locals init (Integer V_0,
                System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_1,
                System.Runtime.CompilerServices.YieldAwaitable V_2,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004b
    IL_000a:  ldloc.0
    IL_000b:  ldc.i4.1
    IL_000c:  beq        IL_00b5
    IL_0011:  call       "Function System.Threading.Tasks.Task.Yield() As System.Runtime.CompilerServices.YieldAwaitable"
    IL_0016:  stloc.2
    IL_0017:  ldloca.s   V_2
    IL_0019:  call       "Function System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter() As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_001e:  stloc.1
    IL_001f:  ldloca.s   V_1
    IL_0021:  call       "Function System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.get_IsCompleted() As Boolean"
    IL_0026:  brtrue.s   IL_0067
    IL_0028:  ldarg.0
    IL_0029:  ldc.i4.0
    IL_002a:  dup
    IL_002b:  stloc.0
    IL_002c:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_0031:  ldarg.0
    IL_0032:  ldloc.1
    IL_0033:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0038:  ldarg.0
    IL_0039:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_003e:  ldloca.s   V_1
    IL_0040:  ldarg.0
    IL_0041:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, Program.VB$StateMachine_3_Call2(Of SM$T))(ByRef System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ByRef Program.VB$StateMachine_3_Call2(Of SM$T))"
    IL_0046:  leave      IL_012c
    IL_004b:  ldarg.0
    IL_004c:  ldc.i4.m1
    IL_004d:  dup
    IL_004e:  stloc.0
    IL_004f:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_0054:  ldarg.0
    IL_0055:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_005a:  stloc.1
    IL_005b:  ldarg.0
    IL_005c:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0061:  initobj    "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0067:  ldloca.s   V_1
    IL_0069:  call       "Sub System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()"
    IL_006e:  ldloca.s   V_1
    IL_0070:  initobj    "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0076:  ldarg.0
    IL_0077:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_007c:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_0081:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_0086:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_008b:  stloc.3
    IL_008c:  ldloca.s   V_3
    IL_008e:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_0093:  brtrue.s   IL_00d1
    IL_0095:  ldarg.0
    IL_0096:  ldc.i4.1
    IL_0097:  dup
    IL_0098:  stloc.0
    IL_0099:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_009e:  ldarg.0
    IL_009f:  ldloc.3
    IL_00a0:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A1 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00a5:  ldarg.0
    IL_00a6:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00ab:  ldloca.s   V_3
    IL_00ad:  ldarg.0
    IL_00ae:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_3_Call2(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_3_Call2(Of SM$T))"
    IL_00b3:  leave.s    IL_012c
    IL_00b5:  ldarg.0
    IL_00b6:  ldc.i4.m1
    IL_00b7:  dup
    IL_00b8:  stloc.0
    IL_00b9:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_00be:  ldarg.0
    IL_00bf:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A1 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00c4:  stloc.3
    IL_00c5:  ldarg.0
    IL_00c6:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$A1 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00cb:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00d1:  ldarg.0
    IL_00d2:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_00d7:  ldloca.s   V_3
    IL_00d9:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_00de:  ldloca.s   V_3
    IL_00e0:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00e6:  constrained. "SM$T"
    IL_00ec:  callvirt   "Sub IMoveable.GetName(Integer)"
    IL_00f1:  leave.s    IL_0117
  }
  catch System.Exception
  {
    IL_00f3:  dup
    IL_00f4:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_00f9:  stloc.s    V_4
    IL_00fb:  ldarg.0
    IL_00fc:  ldc.i4.s   -2
    IL_00fe:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_0103:  ldarg.0
    IL_0104:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_0109:  ldloc.s    V_4
    IL_010b:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_0110:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_0115:  leave.s    IL_012c
  }
  IL_0117:  ldarg.0
  IL_0118:  ldc.i4.s   -2
  IL_011a:  dup
  IL_011b:  stloc.0
  IL_011c:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
  IL_0121:  ldarg.0
  IL_0122:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_0127:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_012c:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Call_Struct_Async_02()
            Dim comp =
<compilation>
    <file>
Imports System
Imports System.Threading.Tasks

Interface IMoveable
    Sub GetName(x As Integer)
End Interface

Structure Item
    Implements IMoveable

    Public Property Name As String

    Public Sub GetName(x As Integer) Implements IMoveable.GetName
        Console.WriteLine("Position GetName for item '{0}'", Me.Name)
    End Sub
End Structure

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1).Wait()

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2).Wait()
    End Sub

    Private Shared Async Function Call1(Of T As {Structure, IMoveable})(item As T) As Task
        await Task.Yield()
        item.GetName(await GetOffsetAsync(GetOffset(item)))
    End Function

    Private Shared Async Function Call2(Of T As {IMoveable})(item As T) As Task
        await Task.Yield()
        item.GetName(await GetOffsetAsync(GetOffset(item)))
    End Function

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function

    Shared Function GetOffsetAsync(i As Integer) As Task(Of Integer)
        Return Task.FromResult(i)
    End Function
End Class
    </file>
</compilation>

            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position GetName for item '-1'
Position GetName for item '-2'
").VerifyDiagnostics()

            verifier.VerifyIL("Program.VB$StateMachine_2_Call1(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      301 (0x12d)
  .maxstack  3
  .locals init (Integer V_0,
                System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_1,
                System.Runtime.CompilerServices.YieldAwaitable V_2,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004b
    IL_000a:  ldloc.0
    IL_000b:  ldc.i4.1
    IL_000c:  beq        IL_00b5
    IL_0011:  call       "Function System.Threading.Tasks.Task.Yield() As System.Runtime.CompilerServices.YieldAwaitable"
    IL_0016:  stloc.2
    IL_0017:  ldloca.s   V_2
    IL_0019:  call       "Function System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter() As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_001e:  stloc.1
    IL_001f:  ldloca.s   V_1
    IL_0021:  call       "Function System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.get_IsCompleted() As Boolean"
    IL_0026:  brtrue.s   IL_0067
    IL_0028:  ldarg.0
    IL_0029:  ldc.i4.0
    IL_002a:  dup
    IL_002b:  stloc.0
    IL_002c:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0031:  ldarg.0
    IL_0032:  ldloc.1
    IL_0033:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0038:  ldarg.0
    IL_0039:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_003e:  ldloca.s   V_1
    IL_0040:  ldarg.0
    IL_0041:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, Program.VB$StateMachine_2_Call1(Of SM$T))(ByRef System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ByRef Program.VB$StateMachine_2_Call1(Of SM$T))"
    IL_0046:  leave      IL_012c
    IL_004b:  ldarg.0
    IL_004c:  ldc.i4.m1
    IL_004d:  dup
    IL_004e:  stloc.0
    IL_004f:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0054:  ldarg.0
    IL_0055:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_005a:  stloc.1
    IL_005b:  ldarg.0
    IL_005c:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0061:  initobj    "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0067:  ldloca.s   V_1
    IL_0069:  call       "Sub System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()"
    IL_006e:  ldloca.s   V_1
    IL_0070:  initobj    "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0076:  ldarg.0
    IL_0077:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_007c:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_0081:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_0086:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_008b:  stloc.3
    IL_008c:  ldloca.s   V_3
    IL_008e:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_0093:  brtrue.s   IL_00d1
    IL_0095:  ldarg.0
    IL_0096:  ldc.i4.1
    IL_0097:  dup
    IL_0098:  stloc.0
    IL_0099:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_009e:  ldarg.0
    IL_009f:  ldloc.3
    IL_00a0:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A1 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00a5:  ldarg.0
    IL_00a6:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00ab:  ldloca.s   V_3
    IL_00ad:  ldarg.0
    IL_00ae:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_2_Call1(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_2_Call1(Of SM$T))"
    IL_00b3:  leave.s    IL_012c
    IL_00b5:  ldarg.0
    IL_00b6:  ldc.i4.m1
    IL_00b7:  dup
    IL_00b8:  stloc.0
    IL_00b9:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_00be:  ldarg.0
    IL_00bf:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A1 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00c4:  stloc.3
    IL_00c5:  ldarg.0
    IL_00c6:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$A1 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00cb:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00d1:  ldarg.0
    IL_00d2:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_00d7:  ldloca.s   V_3
    IL_00d9:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_00de:  ldloca.s   V_3
    IL_00e0:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00e6:  constrained. "SM$T"
    IL_00ec:  callvirt   "Sub IMoveable.GetName(Integer)"
    IL_00f1:  leave.s    IL_0117
  }
  catch System.Exception
  {
    IL_00f3:  dup
    IL_00f4:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_00f9:  stloc.s    V_4
    IL_00fb:  ldarg.0
    IL_00fc:  ldc.i4.s   -2
    IL_00fe:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0103:  ldarg.0
    IL_0104:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_0109:  ldloc.s    V_4
    IL_010b:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_0110:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_0115:  leave.s    IL_012c
  }
  IL_0117:  ldarg.0
  IL_0118:  ldc.i4.s   -2
  IL_011a:  dup
  IL_011b:  stloc.0
  IL_011c:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_0121:  ldarg.0
  IL_0122:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_0127:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_012c:  ret
}
]]>)
        End Sub

        <Theory>
        <CombinatorialData>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Call_Class_Async_01_ThroughArray(asRValue As Boolean)

            Dim leftParen As String = ""
            Dim rightParen As String = ""

            If asRValue Then
                leftParen = "("
                rightParen = ")"
            End If

            Dim comp =
<compilation>
    <file>
Imports System
Imports System.Threading.Tasks

Interface IMoveable
    Sub GetName(x As Integer)
End Interface

Class Item
    Implements IMoveable

    Public Property Name As String

    Public Sub GetName(x As Integer) Implements IMoveable.GetName
        Console.WriteLine("Position GetName for item '{0}'", Me.Name)
    End Sub
End Class

Class Item2
    Inherits Item
End Class

Class Program
    Shared Sub Main()
        Dim item1 = {New Item2 With {.Name = "1"}}
        Call1(DirectCast(item1, Item())).Wait()

        Dim item2 = {New Item2 With {.Name = "2"}}
        Call2(DirectCast(item2, Item())).Wait()

        Dim item3 = {New Item2 With {.Name = "3"}}
        Call3(DirectCast(item3, Item())).Wait()
    End Sub

    Private Shared Async Function Call1(Of T As {Class, IMoveable})(item As T()) As Task
        call <%= leftParen %>item(GetArrayIndex())<%= rightParen %>.GetName(await GetOffsetAsync(GetOffset(item)))
    End Function

    Private Shared Async Function Call2(Of T As {IMoveable})(item As T()) As Task
        call <%= leftParen %>item(GetArrayIndex())<%= rightParen %>.GetName(await GetOffsetAsync(GetOffset(item)))
    End Function

    Private Shared Async Function Call3(item As Item()) As Task
        call <%= leftParen %>item(GetArrayIndex())<%= rightParen %>.GetName(await GetOffsetAsync(GetOffset(item)))
    End Function

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T()) As Integer
        value -= 1
        item(0) = DirectCast(DirectCast(New Item2 With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function

    Shared Function GetOffsetAsync(i As Integer) As Task(Of Integer)
        Return Task.FromResult(i)
    End Function

    Shared Function GetArrayIndex() As Integer
        Return 0
    End Function
End Class
    </file>
</compilation>

            ' Wrong output
            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position GetName for item '-1'
Position GetName for item '-2'
Position GetName for item '3'
").VerifyDiagnostics()

            verifier.VerifyIL("Program.VB$StateMachine_2_Call1(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      257 (0x101)
  .maxstack  3
  .locals init (Integer V_0,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_1,
                System.Exception V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0077
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T()"
    IL_0011:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As SM$T()"
    IL_0016:  ldarg.0
    IL_0017:  call       "Function Program.GetArrayIndex() As Integer"
    IL_001c:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U2 As Integer"
    IL_0021:  ldarg.0
    IL_0022:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As SM$T()"
    IL_0027:  ldarg.0
    IL_0028:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U2 As Integer"
    IL_002d:  readonly.
    IL_002f:  ldelema    "SM$T"
    IL_0034:  pop
    IL_0035:  ldarg.0
    IL_0036:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T()"
    IL_003b:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T()) As Integer"
    IL_0040:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_0045:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_004a:  stloc.1
    IL_004b:  ldloca.s   V_1
    IL_004d:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_0052:  brtrue.s   IL_0093
    IL_0054:  ldarg.0
    IL_0055:  ldc.i4.0
    IL_0056:  dup
    IL_0057:  stloc.0
    IL_0058:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_005d:  ldarg.0
    IL_005e:  ldloc.1
    IL_005f:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0064:  ldarg.0
    IL_0065:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_006a:  ldloca.s   V_1
    IL_006c:  ldarg.0
    IL_006d:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_2_Call1(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_2_Call1(Of SM$T))"
    IL_0072:  leave      IL_0100
    IL_0077:  ldarg.0
    IL_0078:  ldc.i4.m1
    IL_0079:  dup
    IL_007a:  stloc.0
    IL_007b:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0080:  ldarg.0
    IL_0081:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0086:  stloc.1
    IL_0087:  ldarg.0
    IL_0088:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_008d:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0093:  ldarg.0
    IL_0094:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As SM$T()"
    IL_0099:  ldarg.0
    IL_009a:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U2 As Integer"
    IL_009f:  readonly.
    IL_00a1:  ldelema    "SM$T"
    IL_00a6:  ldloca.s   V_1
    IL_00a8:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_00ad:  ldloca.s   V_1
    IL_00af:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00b5:  constrained. "SM$T"
    IL_00bb:  callvirt   "Sub IMoveable.GetName(Integer)"
    IL_00c0:  ldarg.0
    IL_00c1:  ldnull
    IL_00c2:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As SM$T()"
    IL_00c7:  leave.s    IL_00eb
  }
  catch System.Exception
  {
    IL_00c9:  dup
    IL_00ca:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_00cf:  stloc.2
    IL_00d0:  ldarg.0
    IL_00d1:  ldc.i4.s   -2
    IL_00d3:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_00d8:  ldarg.0
    IL_00d9:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00de:  ldloc.2
    IL_00df:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_00e4:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_00e9:  leave.s    IL_0100
  }
  IL_00eb:  ldarg.0
  IL_00ec:  ldc.i4.s   -2
  IL_00ee:  dup
  IL_00ef:  stloc.0
  IL_00f0:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_00f5:  ldarg.0
  IL_00f6:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_00fb:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_0100:  ret
}
]]>)

            verifier.VerifyIL("Program.VB$StateMachine_3_Call2(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      257 (0x101)
  .maxstack  3
  .locals init (Integer V_0,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_1,
                System.Exception V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0077
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T()"
    IL_0011:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$U1 As SM$T()"
    IL_0016:  ldarg.0
    IL_0017:  call       "Function Program.GetArrayIndex() As Integer"
    IL_001c:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$U2 As Integer"
    IL_0021:  ldarg.0
    IL_0022:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$U1 As SM$T()"
    IL_0027:  ldarg.0
    IL_0028:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$U2 As Integer"
    IL_002d:  readonly.
    IL_002f:  ldelema    "SM$T"
    IL_0034:  pop
    IL_0035:  ldarg.0
    IL_0036:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T()"
    IL_003b:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T()) As Integer"
    IL_0040:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_0045:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_004a:  stloc.1
    IL_004b:  ldloca.s   V_1
    IL_004d:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_0052:  brtrue.s   IL_0093
    IL_0054:  ldarg.0
    IL_0055:  ldc.i4.0
    IL_0056:  dup
    IL_0057:  stloc.0
    IL_0058:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_005d:  ldarg.0
    IL_005e:  ldloc.1
    IL_005f:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0064:  ldarg.0
    IL_0065:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_006a:  ldloca.s   V_1
    IL_006c:  ldarg.0
    IL_006d:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_3_Call2(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_3_Call2(Of SM$T))"
    IL_0072:  leave      IL_0100
    IL_0077:  ldarg.0
    IL_0078:  ldc.i4.m1
    IL_0079:  dup
    IL_007a:  stloc.0
    IL_007b:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_0080:  ldarg.0
    IL_0081:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0086:  stloc.1
    IL_0087:  ldarg.0
    IL_0088:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_008d:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0093:  ldarg.0
    IL_0094:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$U1 As SM$T()"
    IL_0099:  ldarg.0
    IL_009a:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$U2 As Integer"
    IL_009f:  readonly.
    IL_00a1:  ldelema    "SM$T"
    IL_00a6:  ldloca.s   V_1
    IL_00a8:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_00ad:  ldloca.s   V_1
    IL_00af:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00b5:  constrained. "SM$T"
    IL_00bb:  callvirt   "Sub IMoveable.GetName(Integer)"
    IL_00c0:  ldarg.0
    IL_00c1:  ldnull
    IL_00c2:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$U1 As SM$T()"
    IL_00c7:  leave.s    IL_00eb
  }
  catch System.Exception
  {
    IL_00c9:  dup
    IL_00ca:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_00cf:  stloc.2
    IL_00d0:  ldarg.0
    IL_00d1:  ldc.i4.s   -2
    IL_00d3:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_00d8:  ldarg.0
    IL_00d9:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00de:  ldloc.2
    IL_00df:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_00e4:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_00e9:  leave.s    IL_0100
  }
  IL_00eb:  ldarg.0
  IL_00ec:  ldc.i4.s   -2
  IL_00ee:  dup
  IL_00ef:  stloc.0
  IL_00f0:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
  IL_00f5:  ldarg.0
  IL_00f6:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_00fb:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_0100:  ret
}
]]>)
        End Sub

        <Theory>
        <CombinatorialData>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Call_Struct_Async_01_ThroughArray(asRValue As Boolean)

            Dim leftParen As String = ""
            Dim rightParen As String = ""

            If asRValue Then
                leftParen = "("
                rightParen = ")"
            End If

            Dim comp =
<compilation>
    <file>
Imports System
Imports System.Threading.Tasks

Interface IMoveable
    Sub GetName(x As Integer)
End Interface

Structure Item
    Implements IMoveable

    Public Property Name As String

    Public Sub GetName(x As Integer) Implements IMoveable.GetName
        Console.WriteLine("Position GetName for item '{0}'", Me.Name)
    End Sub
End Structure

Class Program
    Shared Sub Main()
        Dim item1 = {New Item With {.Name = "1"}}
        Call1(item1).Wait()

        Dim item2 = {New Item With {.Name = "2"}}
        Call2(item2).Wait()

        Dim item3 = {New Item With {.Name = "3"}}
        Call3(item3).Wait()
    End Sub

    Private Shared Async Function Call1(Of T As {Structure, IMoveable})(item As T()) As Task
        call <%= leftParen %>item(GetArrayIndex())<%= rightParen %>.GetName(await GetOffsetAsync(GetOffset(item)))
    End Function

    Private Shared Async Function Call2(Of T As {IMoveable})(item As T()) As Task
        call <%= leftParen %>item(GetArrayIndex())<%= rightParen %>.GetName(await GetOffsetAsync(GetOffset(item)))
    End Function

    Private Shared Async Function Call3(item As Item()) As Task
        call <%= leftParen %>item(GetArrayIndex())<%= rightParen %>.GetName(await GetOffsetAsync(GetOffset(item)))
    End Function

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T()) As Integer
        value -= 1
        item(0) = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function

    Shared Function GetOffsetAsync(i As Integer) As Task(Of Integer)
        Return Task.FromResult(i)
    End Function

    Shared Function GetArrayIndex() As Integer
        Return 0
    End Function
End Class
    </file>
</compilation>

            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position GetName for item '-1'
Position GetName for item '-2'
Position GetName for item '-3'
").VerifyDiagnostics()

            verifier.VerifyIL("Program.VB$StateMachine_2_Call1(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      257 (0x101)
  .maxstack  3
  .locals init (Integer V_0,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_1,
                System.Exception V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0077
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T()"
    IL_0011:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As SM$T()"
    IL_0016:  ldarg.0
    IL_0017:  call       "Function Program.GetArrayIndex() As Integer"
    IL_001c:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U2 As Integer"
    IL_0021:  ldarg.0
    IL_0022:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As SM$T()"
    IL_0027:  ldarg.0
    IL_0028:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U2 As Integer"
    IL_002d:  readonly.
    IL_002f:  ldelema    "SM$T"
    IL_0034:  pop
    IL_0035:  ldarg.0
    IL_0036:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T()"
    IL_003b:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T()) As Integer"
    IL_0040:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_0045:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_004a:  stloc.1
    IL_004b:  ldloca.s   V_1
    IL_004d:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_0052:  brtrue.s   IL_0093
    IL_0054:  ldarg.0
    IL_0055:  ldc.i4.0
    IL_0056:  dup
    IL_0057:  stloc.0
    IL_0058:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_005d:  ldarg.0
    IL_005e:  ldloc.1
    IL_005f:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0064:  ldarg.0
    IL_0065:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_006a:  ldloca.s   V_1
    IL_006c:  ldarg.0
    IL_006d:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_2_Call1(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_2_Call1(Of SM$T))"
    IL_0072:  leave      IL_0100
    IL_0077:  ldarg.0
    IL_0078:  ldc.i4.m1
    IL_0079:  dup
    IL_007a:  stloc.0
    IL_007b:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0080:  ldarg.0
    IL_0081:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0086:  stloc.1
    IL_0087:  ldarg.0
    IL_0088:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_008d:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0093:  ldarg.0
    IL_0094:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As SM$T()"
    IL_0099:  ldarg.0
    IL_009a:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U2 As Integer"
    IL_009f:  readonly.
    IL_00a1:  ldelema    "SM$T"
    IL_00a6:  ldloca.s   V_1
    IL_00a8:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_00ad:  ldloca.s   V_1
    IL_00af:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00b5:  constrained. "SM$T"
    IL_00bb:  callvirt   "Sub IMoveable.GetName(Integer)"
    IL_00c0:  ldarg.0
    IL_00c1:  ldnull
    IL_00c2:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As SM$T()"
    IL_00c7:  leave.s    IL_00eb
  }
  catch System.Exception
  {
    IL_00c9:  dup
    IL_00ca:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_00cf:  stloc.2
    IL_00d0:  ldarg.0
    IL_00d1:  ldc.i4.s   -2
    IL_00d3:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_00d8:  ldarg.0
    IL_00d9:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00de:  ldloc.2
    IL_00df:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_00e4:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_00e9:  leave.s    IL_0100
  }
  IL_00eb:  ldarg.0
  IL_00ec:  ldc.i4.s   -2
  IL_00ee:  dup
  IL_00ef:  stloc.0
  IL_00f0:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_00f5:  ldarg.0
  IL_00f6:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_00fb:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_0100:  ret
}
]]>)
        End Sub

        <Theory>
        <CombinatorialData>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Call_Class_Async_01_ThroughArray_InWith(asRValue As Boolean)

            Dim leftParen As String = ""
            Dim rightParen As String = ""

            If asRValue Then
                leftParen = "("
                rightParen = ")"
            End If

            Dim comp =
<compilation>
    <file>
Imports System
Imports System.Threading.Tasks

Interface IMoveable
    Sub GetName(x As Integer)
End Interface

Class Item
    Implements IMoveable

    Public Property Name As String

    Public Sub GetName(x As Integer) Implements IMoveable.GetName
        Console.WriteLine("Position GetName for item '{0}'", Me.Name)
    End Sub
End Class

Class Item2
    Inherits Item
End Class

Class Program
    Shared Sub Main()
        Dim item1 = {New Item2 With {.Name = "1"}}
        Call1(DirectCast(item1, Item())).Wait()

        Dim item2 = {New Item2 With {.Name = "2"}}
        Call2(DirectCast(item2, Item())).Wait()

        Dim item3 = {New Item2 With {.Name = "3"}}
        Call3(DirectCast(item3, Item())).Wait()
    End Sub

    Private Shared Async Function Call1(Of T As {Class, IMoveable})(item As T()) As Task
        With <%= leftParen %>item(GetArrayIndex())<%= rightParen %>
            call .GetName(await GetOffsetAsync(GetOffset(item)))
        End With
    End Function

    Private Shared Async Function Call2(Of T As {IMoveable})(item As T()) As Task
        With <%= leftParen %>item(GetArrayIndex())<%= rightParen %>
            call .GetName(await GetOffsetAsync(GetOffset(item)))
        End With
    End Function

    Private Shared Async Function Call3(item As Item()) As Task
        With <%= leftParen %>item(GetArrayIndex())<%= rightParen %>
            call .GetName(await GetOffsetAsync(GetOffset(item)))
        End With
    End Function

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T()) As Integer
        value -= 1
        item(0) = DirectCast(DirectCast(New Item2 With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function

    Shared Function GetOffsetAsync(i As Integer) As Task(Of Integer)
        Return Task.FromResult(i)
    End Function

    Shared Function GetArrayIndex() As Integer
        Return 0
    End Function
End Class
    </file>
</compilation>

            ' Wrong output for asRValue = False case
            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
If(asRValue,
"
Position GetName for item '1'
Position GetName for item '2'
Position GetName for item '3'
",
"
Position GetName for item '-1'
Position GetName for item '-2'
Position GetName for item '3'
")).VerifyDiagnostics()

            verifier.VerifyIL("Program.VB$StateMachine_2_Call1(Of SM$T).MoveNext()",
If(asRValue,
            <![CDATA[
{
  // Code size      212 (0xd4)
  .maxstack  3
  .locals init (Integer V_0,
                SM$T V_1, //$W0
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_005a
    IL_000a:  ldarg.0
    IL_000b:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T()"
    IL_0010:  call       "Function Program.GetArrayIndex() As Integer"
    IL_0015:  ldelem     "SM$T"
    IL_001a:  stloc.1
    IL_001b:  ldarg.0
    IL_001c:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T()"
    IL_0021:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T()) As Integer"
    IL_0026:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_002b:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0030:  stloc.2
    IL_0031:  ldloca.s   V_2
    IL_0033:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_0038:  brtrue.s   IL_0076
    IL_003a:  ldarg.0
    IL_003b:  ldc.i4.0
    IL_003c:  dup
    IL_003d:  stloc.0
    IL_003e:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0043:  ldarg.0
    IL_0044:  ldloc.2
    IL_0045:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_004a:  ldarg.0
    IL_004b:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_0050:  ldloca.s   V_2
    IL_0052:  ldarg.0
    IL_0053:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_2_Call1(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_2_Call1(Of SM$T))"
    IL_0058:  leave.s    IL_00d3
    IL_005a:  ldarg.0
    IL_005b:  ldc.i4.m1
    IL_005c:  dup
    IL_005d:  stloc.0
    IL_005e:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0063:  ldarg.0
    IL_0064:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0069:  stloc.2
    IL_006a:  ldarg.0
    IL_006b:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0070:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0076:  ldloca.s   V_1
    IL_0078:  ldloca.s   V_2
    IL_007a:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_007f:  ldloca.s   V_2
    IL_0081:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0087:  constrained. "SM$T"
    IL_008d:  callvirt   "Sub IMoveable.GetName(Integer)"
    IL_0092:  ldloca.s   V_1
    IL_0094:  initobj    "SM$T"
    IL_009a:  leave.s    IL_00be
  }
  catch System.Exception
  {
    IL_009c:  dup
    IL_009d:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_00a2:  stloc.3
    IL_00a3:  ldarg.0
    IL_00a4:  ldc.i4.s   -2
    IL_00a6:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_00ab:  ldarg.0
    IL_00ac:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00b1:  ldloc.3
    IL_00b2:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_00b7:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_00bc:  leave.s    IL_00d3
  }
  IL_00be:  ldarg.0
  IL_00bf:  ldc.i4.s   -2
  IL_00c1:  dup
  IL_00c2:  stloc.0
  IL_00c3:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_00c8:  ldarg.0
  IL_00c9:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_00ce:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_00d3:  ret
}
]]>,
            <![CDATA[
{
  // Code size      265 (0x109)
  .maxstack  3
  .locals init (Integer V_0,
                SM$T() V_1, //$W0
                Integer V_2, //$W1
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_007b
    IL_000a:  ldarg.0
    IL_000b:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T()"
    IL_0010:  stloc.1
    IL_0011:  call       "Function Program.GetArrayIndex() As Integer"
    IL_0016:  stloc.2
    IL_0017:  ldarg.0
    IL_0018:  ldloc.1
    IL_0019:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As SM$T()"
    IL_001e:  ldarg.0
    IL_001f:  ldloc.2
    IL_0020:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U2 As Integer"
    IL_0025:  ldarg.0
    IL_0026:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As SM$T()"
    IL_002b:  ldarg.0
    IL_002c:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U2 As Integer"
    IL_0031:  readonly.
    IL_0033:  ldelema    "SM$T"
    IL_0038:  pop
    IL_0039:  ldarg.0
    IL_003a:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T()"
    IL_003f:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T()) As Integer"
    IL_0044:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_0049:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_004e:  stloc.3
    IL_004f:  ldloca.s   V_3
    IL_0051:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_0056:  brtrue.s   IL_0097
    IL_0058:  ldarg.0
    IL_0059:  ldc.i4.0
    IL_005a:  dup
    IL_005b:  stloc.0
    IL_005c:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0061:  ldarg.0
    IL_0062:  ldloc.3
    IL_0063:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0068:  ldarg.0
    IL_0069:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_006e:  ldloca.s   V_3
    IL_0070:  ldarg.0
    IL_0071:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_2_Call1(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_2_Call1(Of SM$T))"
    IL_0076:  leave      IL_0108
    IL_007b:  ldarg.0
    IL_007c:  ldc.i4.m1
    IL_007d:  dup
    IL_007e:  stloc.0
    IL_007f:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0084:  ldarg.0
    IL_0085:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_008a:  stloc.3
    IL_008b:  ldarg.0
    IL_008c:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0091:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0097:  ldarg.0
    IL_0098:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As SM$T()"
    IL_009d:  ldarg.0
    IL_009e:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U2 As Integer"
    IL_00a3:  readonly.
    IL_00a5:  ldelema    "SM$T"
    IL_00aa:  ldloca.s   V_3
    IL_00ac:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_00b1:  ldloca.s   V_3
    IL_00b3:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00b9:  constrained. "SM$T"
    IL_00bf:  callvirt   "Sub IMoveable.GetName(Integer)"
    IL_00c4:  ldarg.0
    IL_00c5:  ldnull
    IL_00c6:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As SM$T()"
    IL_00cb:  ldnull
    IL_00cc:  stloc.1
    IL_00cd:  leave.s    IL_00f3
  }
  catch System.Exception
  {
    IL_00cf:  dup
    IL_00d0:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_00d5:  stloc.s    V_4
    IL_00d7:  ldarg.0
    IL_00d8:  ldc.i4.s   -2
    IL_00da:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_00df:  ldarg.0
    IL_00e0:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00e5:  ldloc.s    V_4
    IL_00e7:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_00ec:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_00f1:  leave.s    IL_0108
  }
  IL_00f3:  ldarg.0
  IL_00f4:  ldc.i4.s   -2
  IL_00f6:  dup
  IL_00f7:  stloc.0
  IL_00f8:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_00fd:  ldarg.0
  IL_00fe:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_0103:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_0108:  ret
}
]]>))

            verifier.VerifyIL("Program.VB$StateMachine_3_Call2(Of SM$T).MoveNext()",
If(asRValue,
            <![CDATA[
{
  // Code size      212 (0xd4)
  .maxstack  3
  .locals init (Integer V_0,
                SM$T V_1, //$W0
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_005a
    IL_000a:  ldarg.0
    IL_000b:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T()"
    IL_0010:  call       "Function Program.GetArrayIndex() As Integer"
    IL_0015:  ldelem     "SM$T"
    IL_001a:  stloc.1
    IL_001b:  ldarg.0
    IL_001c:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T()"
    IL_0021:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T()) As Integer"
    IL_0026:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_002b:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0030:  stloc.2
    IL_0031:  ldloca.s   V_2
    IL_0033:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_0038:  brtrue.s   IL_0076
    IL_003a:  ldarg.0
    IL_003b:  ldc.i4.0
    IL_003c:  dup
    IL_003d:  stloc.0
    IL_003e:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_0043:  ldarg.0
    IL_0044:  ldloc.2
    IL_0045:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_004a:  ldarg.0
    IL_004b:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_0050:  ldloca.s   V_2
    IL_0052:  ldarg.0
    IL_0053:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_3_Call2(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_3_Call2(Of SM$T))"
    IL_0058:  leave.s    IL_00d3
    IL_005a:  ldarg.0
    IL_005b:  ldc.i4.m1
    IL_005c:  dup
    IL_005d:  stloc.0
    IL_005e:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_0063:  ldarg.0
    IL_0064:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0069:  stloc.2
    IL_006a:  ldarg.0
    IL_006b:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0070:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0076:  ldloca.s   V_1
    IL_0078:  ldloca.s   V_2
    IL_007a:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_007f:  ldloca.s   V_2
    IL_0081:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0087:  constrained. "SM$T"
    IL_008d:  callvirt   "Sub IMoveable.GetName(Integer)"
    IL_0092:  ldloca.s   V_1
    IL_0094:  initobj    "SM$T"
    IL_009a:  leave.s    IL_00be
  }
  catch System.Exception
  {
    IL_009c:  dup
    IL_009d:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_00a2:  stloc.3
    IL_00a3:  ldarg.0
    IL_00a4:  ldc.i4.s   -2
    IL_00a6:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_00ab:  ldarg.0
    IL_00ac:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00b1:  ldloc.3
    IL_00b2:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_00b7:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_00bc:  leave.s    IL_00d3
  }
  IL_00be:  ldarg.0
  IL_00bf:  ldc.i4.s   -2
  IL_00c1:  dup
  IL_00c2:  stloc.0
  IL_00c3:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
  IL_00c8:  ldarg.0
  IL_00c9:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_00ce:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_00d3:  ret
}
]]>,
            <![CDATA[
{
  // Code size      265 (0x109)
  .maxstack  3
  .locals init (Integer V_0,
                SM$T() V_1, //$W0
                Integer V_2, //$W1
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_007b
    IL_000a:  ldarg.0
    IL_000b:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T()"
    IL_0010:  stloc.1
    IL_0011:  call       "Function Program.GetArrayIndex() As Integer"
    IL_0016:  stloc.2
    IL_0017:  ldarg.0
    IL_0018:  ldloc.1
    IL_0019:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$U1 As SM$T()"
    IL_001e:  ldarg.0
    IL_001f:  ldloc.2
    IL_0020:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$U2 As Integer"
    IL_0025:  ldarg.0
    IL_0026:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$U1 As SM$T()"
    IL_002b:  ldarg.0
    IL_002c:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$U2 As Integer"
    IL_0031:  readonly.
    IL_0033:  ldelema    "SM$T"
    IL_0038:  pop
    IL_0039:  ldarg.0
    IL_003a:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T()"
    IL_003f:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T()) As Integer"
    IL_0044:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_0049:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_004e:  stloc.3
    IL_004f:  ldloca.s   V_3
    IL_0051:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_0056:  brtrue.s   IL_0097
    IL_0058:  ldarg.0
    IL_0059:  ldc.i4.0
    IL_005a:  dup
    IL_005b:  stloc.0
    IL_005c:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_0061:  ldarg.0
    IL_0062:  ldloc.3
    IL_0063:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0068:  ldarg.0
    IL_0069:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_006e:  ldloca.s   V_3
    IL_0070:  ldarg.0
    IL_0071:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_3_Call2(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_3_Call2(Of SM$T))"
    IL_0076:  leave      IL_0108
    IL_007b:  ldarg.0
    IL_007c:  ldc.i4.m1
    IL_007d:  dup
    IL_007e:  stloc.0
    IL_007f:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_0084:  ldarg.0
    IL_0085:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_008a:  stloc.3
    IL_008b:  ldarg.0
    IL_008c:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0091:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0097:  ldarg.0
    IL_0098:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$U1 As SM$T()"
    IL_009d:  ldarg.0
    IL_009e:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$U2 As Integer"
    IL_00a3:  readonly.
    IL_00a5:  ldelema    "SM$T"
    IL_00aa:  ldloca.s   V_3
    IL_00ac:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_00b1:  ldloca.s   V_3
    IL_00b3:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00b9:  constrained. "SM$T"
    IL_00bf:  callvirt   "Sub IMoveable.GetName(Integer)"
    IL_00c4:  ldarg.0
    IL_00c5:  ldnull
    IL_00c6:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$U1 As SM$T()"
    IL_00cb:  ldnull
    IL_00cc:  stloc.1
    IL_00cd:  leave.s    IL_00f3
  }
  catch System.Exception
  {
    IL_00cf:  dup
    IL_00d0:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_00d5:  stloc.s    V_4
    IL_00d7:  ldarg.0
    IL_00d8:  ldc.i4.s   -2
    IL_00da:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_00df:  ldarg.0
    IL_00e0:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00e5:  ldloc.s    V_4
    IL_00e7:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_00ec:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_00f1:  leave.s    IL_0108
  }
  IL_00f3:  ldarg.0
  IL_00f4:  ldc.i4.s   -2
  IL_00f6:  dup
  IL_00f7:  stloc.0
  IL_00f8:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
  IL_00fd:  ldarg.0
  IL_00fe:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_0103:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_0108:  ret
}
]]>))
        End Sub

        <Theory>
        <CombinatorialData>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Call_Struct_Async_01_ThroughArray_InWith(asRValue As Boolean)

            Dim leftParen As String = ""
            Dim rightParen As String = ""

            If asRValue Then
                leftParen = "("
                rightParen = ")"
            End If

            Dim comp =
<compilation>
    <file>
Imports System
Imports System.Threading.Tasks

Interface IMoveable
    Sub GetName(x As Integer)
End Interface

Structure Item
    Implements IMoveable

    Public Property Name As String

    Public Sub GetName(x As Integer) Implements IMoveable.GetName
        Console.WriteLine("Position GetName for item '{0}'", Me.Name)
    End Sub
End Structure

Class Program
    Shared Sub Main()
        Dim item1 = {New Item With {.Name = "1"}}
        Call1(item1).Wait()

        Dim item2 = {New Item With {.Name = "2"}}
        Call2(item2).Wait()

        Dim item3 = {New Item With {.Name = "3"}}
        Call3(item3).Wait()
    End Sub

    Private Shared Async Function Call1(Of T As {Structure, IMoveable})(item As T()) As Task
        With <%= leftParen %>item(GetArrayIndex())<%= rightParen %>
            call .GetName(await GetOffsetAsync(GetOffset(item)))
        End With
    End Function

    Private Shared Async Function Call2(Of T As {IMoveable})(item As T()) As Task
        With <%= leftParen %>item(GetArrayIndex())<%= rightParen %>
            call .GetName(await GetOffsetAsync(GetOffset(item)))
        End With
    End Function

    Private Shared Async Function Call3(item As Item()) As Task
        With <%= leftParen %>item(GetArrayIndex())<%= rightParen %>
            call .GetName(await GetOffsetAsync(GetOffset(item)))
        End With
    End Function

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T()) As Integer
        value -= 1
        item(0) = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function

    Shared Function GetOffsetAsync(i As Integer) As Task(Of Integer)
        Return Task.FromResult(i)
    End Function

    Shared Function GetArrayIndex() As Integer
        Return 0
    End Function
End Class
    </file>
</compilation>

            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
If(asRValue,
"
Position GetName for item '1'
Position GetName for item '2'
Position GetName for item '3'
",
"
Position GetName for item '-1'
Position GetName for item '-2'
Position GetName for item '-3'
")).VerifyDiagnostics()

            verifier.VerifyIL("Program.VB$StateMachine_2_Call1(Of SM$T).MoveNext()",
If(asRValue,
            <![CDATA[
{
  // Code size      212 (0xd4)
  .maxstack  3
  .locals init (Integer V_0,
                SM$T V_1, //$W0
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_005a
    IL_000a:  ldarg.0
    IL_000b:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T()"
    IL_0010:  call       "Function Program.GetArrayIndex() As Integer"
    IL_0015:  ldelem     "SM$T"
    IL_001a:  stloc.1
    IL_001b:  ldarg.0
    IL_001c:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T()"
    IL_0021:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T()) As Integer"
    IL_0026:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_002b:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0030:  stloc.2
    IL_0031:  ldloca.s   V_2
    IL_0033:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_0038:  brtrue.s   IL_0076
    IL_003a:  ldarg.0
    IL_003b:  ldc.i4.0
    IL_003c:  dup
    IL_003d:  stloc.0
    IL_003e:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0043:  ldarg.0
    IL_0044:  ldloc.2
    IL_0045:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_004a:  ldarg.0
    IL_004b:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_0050:  ldloca.s   V_2
    IL_0052:  ldarg.0
    IL_0053:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_2_Call1(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_2_Call1(Of SM$T))"
    IL_0058:  leave.s    IL_00d3
    IL_005a:  ldarg.0
    IL_005b:  ldc.i4.m1
    IL_005c:  dup
    IL_005d:  stloc.0
    IL_005e:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0063:  ldarg.0
    IL_0064:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0069:  stloc.2
    IL_006a:  ldarg.0
    IL_006b:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0070:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0076:  ldloca.s   V_1
    IL_0078:  ldloca.s   V_2
    IL_007a:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_007f:  ldloca.s   V_2
    IL_0081:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0087:  constrained. "SM$T"
    IL_008d:  callvirt   "Sub IMoveable.GetName(Integer)"
    IL_0092:  ldloca.s   V_1
    IL_0094:  initobj    "SM$T"
    IL_009a:  leave.s    IL_00be
  }
  catch System.Exception
  {
    IL_009c:  dup
    IL_009d:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_00a2:  stloc.3
    IL_00a3:  ldarg.0
    IL_00a4:  ldc.i4.s   -2
    IL_00a6:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_00ab:  ldarg.0
    IL_00ac:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00b1:  ldloc.3
    IL_00b2:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_00b7:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_00bc:  leave.s    IL_00d3
  }
  IL_00be:  ldarg.0
  IL_00bf:  ldc.i4.s   -2
  IL_00c1:  dup
  IL_00c2:  stloc.0
  IL_00c3:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_00c8:  ldarg.0
  IL_00c9:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_00ce:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_00d3:  ret
}
]]>,
            <![CDATA[
{
  // Code size      265 (0x109)
  .maxstack  3
  .locals init (Integer V_0,
                SM$T() V_1, //$W0
                Integer V_2, //$W1
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_007b
    IL_000a:  ldarg.0
    IL_000b:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T()"
    IL_0010:  stloc.1
    IL_0011:  call       "Function Program.GetArrayIndex() As Integer"
    IL_0016:  stloc.2
    IL_0017:  ldarg.0
    IL_0018:  ldloc.1
    IL_0019:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As SM$T()"
    IL_001e:  ldarg.0
    IL_001f:  ldloc.2
    IL_0020:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U2 As Integer"
    IL_0025:  ldarg.0
    IL_0026:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As SM$T()"
    IL_002b:  ldarg.0
    IL_002c:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U2 As Integer"
    IL_0031:  readonly.
    IL_0033:  ldelema    "SM$T"
    IL_0038:  pop
    IL_0039:  ldarg.0
    IL_003a:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T()"
    IL_003f:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T()) As Integer"
    IL_0044:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_0049:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_004e:  stloc.3
    IL_004f:  ldloca.s   V_3
    IL_0051:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_0056:  brtrue.s   IL_0097
    IL_0058:  ldarg.0
    IL_0059:  ldc.i4.0
    IL_005a:  dup
    IL_005b:  stloc.0
    IL_005c:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0061:  ldarg.0
    IL_0062:  ldloc.3
    IL_0063:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0068:  ldarg.0
    IL_0069:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_006e:  ldloca.s   V_3
    IL_0070:  ldarg.0
    IL_0071:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_2_Call1(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_2_Call1(Of SM$T))"
    IL_0076:  leave      IL_0108
    IL_007b:  ldarg.0
    IL_007c:  ldc.i4.m1
    IL_007d:  dup
    IL_007e:  stloc.0
    IL_007f:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0084:  ldarg.0
    IL_0085:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_008a:  stloc.3
    IL_008b:  ldarg.0
    IL_008c:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0091:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0097:  ldarg.0
    IL_0098:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As SM$T()"
    IL_009d:  ldarg.0
    IL_009e:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U2 As Integer"
    IL_00a3:  readonly.
    IL_00a5:  ldelema    "SM$T"
    IL_00aa:  ldloca.s   V_3
    IL_00ac:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_00b1:  ldloca.s   V_3
    IL_00b3:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00b9:  constrained. "SM$T"
    IL_00bf:  callvirt   "Sub IMoveable.GetName(Integer)"
    IL_00c4:  ldarg.0
    IL_00c5:  ldnull
    IL_00c6:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As SM$T()"
    IL_00cb:  ldnull
    IL_00cc:  stloc.1
    IL_00cd:  leave.s    IL_00f3
  }
  catch System.Exception
  {
    IL_00cf:  dup
    IL_00d0:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_00d5:  stloc.s    V_4
    IL_00d7:  ldarg.0
    IL_00d8:  ldc.i4.s   -2
    IL_00da:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_00df:  ldarg.0
    IL_00e0:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00e5:  ldloc.s    V_4
    IL_00e7:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_00ec:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_00f1:  leave.s    IL_0108
  }
  IL_00f3:  ldarg.0
  IL_00f4:  ldc.i4.s   -2
  IL_00f6:  dup
  IL_00f7:  stloc.0
  IL_00f8:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_00fd:  ldarg.0
  IL_00fe:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_0103:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_0108:  ret
}
]]>))
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Call_Conditional_Class()
            Dim comp =
<compilation>
    <file>
Imports System

Interface IMoveable
    Sub GetName(x As Integer)
End Interface

Class Item
    Implements IMoveable

    Public Property Name As String

    Public Sub GetName(x As Integer) Implements IMoveable.GetName
        Console.WriteLine("Position GetName for item '{0}'", Me.Name)
    End Sub
End Class

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1)

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2)
    End Sub

    Private Shared Sub Call1(Of T As {Class, IMoveable})(item As T)
        item?.GetName(GetOffset(item))
    End Sub

    Private Shared Sub Call2(Of T As {IMoveable})(item As T)
        item?.GetName(GetOffset(item))
    End Sub

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function
End Class
    </file>
</compilation>

            ' Wrong output on some frameworks
            '            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
            '"
            'Position GetName for item '1'
            'Position GetName for item '2'
            '").VerifyDiagnostics()
            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe).VerifyDiagnostics()

            verifier.VerifyIL("Program.Call1(Of T)",
            <![CDATA[
{
  // Code size       24 (0x18)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  box        "T"
  IL_0006:  dup
  IL_0007:  brtrue.s   IL_000b
  IL_0009:  pop
  IL_000a:  ret
  IL_000b:  ldarga.s   V_0
  IL_000d:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0012:  callvirt   "Sub IMoveable.GetName(Integer)"
  IL_0017:  ret
}
]]>)

            'Wrong IL
            verifier.VerifyIL("Program.Call2(Of T)",
            <![CDATA[
{
  // Code size       29 (0x1d)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  box        "T"
  IL_0006:  brfalse.s  IL_001c
  IL_0008:  ldarga.s   V_0
  IL_000a:  ldarga.s   V_0
  IL_000c:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0011:  constrained. "T"
  IL_0017:  callvirt   "Sub IMoveable.GetName(Integer)"
  IL_001c:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Call_Conditional_Struct()
            Dim comp =
<compilation>
    <file>
Imports System

Interface IMoveable
    Sub GetName(x As Integer)
End Interface

Structure Item
    Implements IMoveable

    Public Property Name As String

    Public Sub GetName(x As Integer) Implements IMoveable.GetName
        Console.WriteLine("Position GetName for item '{0}'", Me.Name)
    End Sub
End Structure

Class Program
    Shared Sub Main()
        Dim item2 = New Item With {.Name = "2"}
        Call2(item2)
    End Sub

    Private Shared Sub Call2(Of T As {IMoveable})(item As T)
        item?.GetName(GetOffset(item))
    End Sub

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function
End Class
    </file>
</compilation>

            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position GetName for item '-1'
").VerifyDiagnostics()
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Call_Conditional_Class_Ref()
            Dim comp =
<compilation>
    <file>
Imports System

Interface IMoveable
    Sub GetName(x As Integer)
End Interface

Class Item
    Implements IMoveable

    Public Property Name As String

    Public Sub GetName(x As Integer) Implements IMoveable.GetName
        Console.WriteLine("Position GetName for item '{0}'", Me.Name)
    End Sub
End Class

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1)

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2)
    End Sub

    Private Shared Sub Call1(Of T As {Class, IMoveable})(ByRef item As T)
        item?.GetName(GetOffset(item))
    End Sub

    Private Shared Sub Call2(Of T As {IMoveable})(ByRef item As T)
        item?.GetName(GetOffset(item))
    End Sub

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function
End Class
    </file>
</compilation>

            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position GetName for item '1'
Position GetName for item '2'
").VerifyDiagnostics()

            verifier.VerifyIL("Program.Call1(Of T)",
            <![CDATA[
{
  // Code size       28 (0x1c)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldobj      "T"
  IL_0006:  box        "T"
  IL_000b:  dup
  IL_000c:  brtrue.s   IL_0010
  IL_000e:  pop
  IL_000f:  ret
  IL_0010:  ldarg.0
  IL_0011:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0016:  callvirt   "Sub IMoveable.GetName(Integer)"
  IL_001b:  ret
}
]]>)

            verifier.VerifyIL("Program.Call2(Of T)",
            <![CDATA[
{
  // Code size       53 (0x35)
  .maxstack  2
  .locals init (T V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldloca.s   V_0
  IL_0003:  initobj    "T"
  IL_0009:  ldloc.0
  IL_000a:  box        "T"
  IL_000f:  brtrue.s   IL_0023
  IL_0011:  ldobj      "T"
  IL_0016:  stloc.0
  IL_0017:  ldloca.s   V_0
  IL_0019:  ldloc.0
  IL_001a:  box        "T"
  IL_001f:  brtrue.s   IL_0023
  IL_0021:  pop
  IL_0022:  ret
  IL_0023:  ldarg.0
  IL_0024:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0029:  constrained. "T"
  IL_002f:  callvirt   "Sub IMoveable.GetName(Integer)"
  IL_0034:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Call_Conditional_Struct_Ref()
            Dim comp =
<compilation>
    <file>
Imports System

Interface IMoveable
    Sub GetName(x As Integer)
End Interface

Structure Item
    Implements IMoveable

    Public Property Name As String

    Public Sub GetName(x As Integer) Implements IMoveable.GetName
        Console.WriteLine("Position GetName for item '{0}'", Me.Name)
    End Sub
End Structure

Class Program
    Shared Sub Main()
        Dim item2 = New Item With {.Name = "2"}
        Call2(item2)
    End Sub

    Private Shared Sub Call2(Of T As {IMoveable})(ByRef item As T)
        item?.GetName(GetOffset(item))
    End Sub

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function
End Class
    </file>
</compilation>

            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position GetName for item '-1'
").VerifyDiagnostics()
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Call_Conditional_Class_Async_01()
            Dim comp =
<compilation>
    <file>
Imports System
Imports System.Threading.Tasks

Interface IMoveable
    Sub GetName(x As Integer)
End Interface

Class Item
    Implements IMoveable

    Public Property Name As String

    Public Sub GetName(x As Integer) Implements IMoveable.GetName
        Console.WriteLine("Position GetName for item '{0}'", Me.Name)
    End Sub
End Class

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1).Wait()

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2).Wait()
    End Sub

    Private Shared Async Function Call1(Of T As {Class, IMoveable})(item As T) As Task
        item?.GetName(await GetOffsetAsync(GetOffset(item)))
    End Function

    Private Shared Async Function Call2(Of T As {IMoveable})(item As T) As Task
        item?.GetName(await GetOffsetAsync(GetOffset(item)))
    End Function

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function

    Shared Function GetOffsetAsync(i As Integer) As Task(Of Integer)
        Return Task.FromResult(i)
    End Function
End Class
    </file>
</compilation>

            ' Wrong output
            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position GetName for item '-1'
Position GetName for item '-2'
", verify:=Verification.Skipped).VerifyDiagnostics()

            'Wrong IL
            'PEVerify failed
            '[ : Program+VB$StateMachine_2_Call1`1[SM$T]::MoveNext][mdToken=0x600000c][offset 0x00000019][found (unboxed) 'SM$T'] Non-compatible types on the stack.
            verifier.VerifyIL("Program.VB$StateMachine_2_Call1(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      208 (0xd0)
  .maxstack  3
  .locals init (Integer V_0,
                SM$T V_1,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_005a
    IL_000a:  ldarg.0
    IL_000b:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0010:  ldloca.s   V_1
    IL_0012:  initobj    "SM$T"
    IL_0018:  ldloc.1
    IL_0019:  beq.s      IL_0096
    IL_001b:  ldarg.0
    IL_001c:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0021:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_0026:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_002b:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0030:  stloc.2
    IL_0031:  ldloca.s   V_2
    IL_0033:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_0038:  brtrue.s   IL_0076
    IL_003a:  ldarg.0
    IL_003b:  ldc.i4.0
    IL_003c:  dup
    IL_003d:  stloc.0
    IL_003e:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0043:  ldarg.0
    IL_0044:  ldloc.2
    IL_0045:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_004a:  ldarg.0
    IL_004b:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_0050:  ldloca.s   V_2
    IL_0052:  ldarg.0
    IL_0053:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_2_Call1(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_2_Call1(Of SM$T))"
    IL_0058:  leave.s    IL_00cf
    IL_005a:  ldarg.0
    IL_005b:  ldc.i4.m1
    IL_005c:  dup
    IL_005d:  stloc.0
    IL_005e:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0063:  ldarg.0
    IL_0064:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0069:  stloc.2
    IL_006a:  ldarg.0
    IL_006b:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0070:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0076:  ldarg.0
    IL_0077:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_007c:  ldloca.s   V_2
    IL_007e:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_0083:  ldloca.s   V_2
    IL_0085:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_008b:  constrained. "SM$T"
    IL_0091:  callvirt   "Sub IMoveable.GetName(Integer)"
    IL_0096:  leave.s    IL_00ba
  }
  catch System.Exception
  {
    IL_0098:  dup
    IL_0099:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_009e:  stloc.3
    IL_009f:  ldarg.0
    IL_00a0:  ldc.i4.s   -2
    IL_00a2:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_00a7:  ldarg.0
    IL_00a8:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00ad:  ldloc.3
    IL_00ae:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_00b3:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_00b8:  leave.s    IL_00cf
  }
  IL_00ba:  ldarg.0
  IL_00bb:  ldc.i4.s   -2
  IL_00bd:  dup
  IL_00be:  stloc.0
  IL_00bf:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_00c4:  ldarg.0
  IL_00c5:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_00ca:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_00cf:  ret
}
]]>)

            'Wrong IL
            verifier.VerifyIL("Program.VB$StateMachine_3_Call2(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      220 (0xdc)
  .maxstack  3
  .locals init (Integer V_0,
                SM$T V_1,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0066
    IL_000a:  ldloca.s   V_1
    IL_000c:  initobj    "SM$T"
    IL_0012:  ldloc.1
    IL_0013:  box        "SM$T"
    IL_0018:  brtrue.s   IL_0027
    IL_001a:  ldarg.0
    IL_001b:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_0020:  box        "SM$T"
    IL_0025:  brfalse.s  IL_00a2
    IL_0027:  ldarg.0
    IL_0028:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_002d:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_0032:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_0037:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_003c:  stloc.2
    IL_003d:  ldloca.s   V_2
    IL_003f:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_0044:  brtrue.s   IL_0082
    IL_0046:  ldarg.0
    IL_0047:  ldc.i4.0
    IL_0048:  dup
    IL_0049:  stloc.0
    IL_004a:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_004f:  ldarg.0
    IL_0050:  ldloc.2
    IL_0051:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0056:  ldarg.0
    IL_0057:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_005c:  ldloca.s   V_2
    IL_005e:  ldarg.0
    IL_005f:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_3_Call2(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_3_Call2(Of SM$T))"
    IL_0064:  leave.s    IL_00db
    IL_0066:  ldarg.0
    IL_0067:  ldc.i4.m1
    IL_0068:  dup
    IL_0069:  stloc.0
    IL_006a:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_006f:  ldarg.0
    IL_0070:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0075:  stloc.2
    IL_0076:  ldarg.0
    IL_0077:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_007c:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0082:  ldarg.0
    IL_0083:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_0088:  ldloca.s   V_2
    IL_008a:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_008f:  ldloca.s   V_2
    IL_0091:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0097:  constrained. "SM$T"
    IL_009d:  callvirt   "Sub IMoveable.GetName(Integer)"
    IL_00a2:  leave.s    IL_00c6
  }
  catch System.Exception
  {
    IL_00a4:  dup
    IL_00a5:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_00aa:  stloc.3
    IL_00ab:  ldarg.0
    IL_00ac:  ldc.i4.s   -2
    IL_00ae:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_00b3:  ldarg.0
    IL_00b4:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00b9:  ldloc.3
    IL_00ba:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_00bf:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_00c4:  leave.s    IL_00db
  }
  IL_00c6:  ldarg.0
  IL_00c7:  ldc.i4.s   -2
  IL_00c9:  dup
  IL_00ca:  stloc.0
  IL_00cb:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
  IL_00d0:  ldarg.0
  IL_00d1:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_00d6:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_00db:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Call_Conditional_Struct_Async_01()
            Dim comp =
<compilation>
    <file>
Imports System
Imports System.Threading.Tasks

Interface IMoveable
    Sub GetName(x As Integer)
End Interface

Structure Item
    Implements IMoveable

    Public Property Name As String

    Public Sub GetName(x As Integer) Implements IMoveable.GetName
        Console.WriteLine("Position GetName for item '{0}'", Me.Name)
    End Sub
End Structure

Class Program
    Shared Sub Main()
        Dim item2 = New Item With {.Name = "2"}
        Call2(item2).Wait()
    End Sub

    Private Shared Async Function Call2(Of T As {IMoveable})(item As T) As Task
        item?.GetName(await GetOffsetAsync(GetOffset(item)))
    End Function

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function

    Shared Function GetOffsetAsync(i As Integer) As Task(Of Integer)
        Return Task.FromResult(i)
    End Function
End Class
    </file>
</compilation>

            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position GetName for item '-1'
").VerifyDiagnostics()
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Call_Conditional_Class_Async_02()
            Dim comp =
<compilation>
    <file>
Imports System
Imports System.Threading.Tasks

Interface IMoveable
    Sub GetName(x As Integer)
End Interface

Class Item
    Implements IMoveable

    Public Property Name As String

    Public Sub GetName(x As Integer) Implements IMoveable.GetName
        Console.WriteLine("Position GetName for item '{0}'", Me.Name)
    End Sub
End Class

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1).Wait()

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2).Wait()
    End Sub

    Private Shared Async Function Call1(Of T As {Class, IMoveable})(item As T) As Task
        await Task.Yield()
        item?.GetName(await GetOffsetAsync(GetOffset(item)))
    End Function

    Private Shared Async Function Call2(Of T As {IMoveable})(item As T) As Task
        await Task.Yield()
        item?.GetName(await GetOffsetAsync(GetOffset(item)))
    End Function

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function

    Shared Function GetOffsetAsync(i As Integer) As Task(Of Integer)
        Return Task.FromResult(i)
    End Function
End Class
    </file>
</compilation>

            ' Wrong output
            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position GetName for item '-1'
Position GetName for item '-2'
", verify:=Verification.Skipped).VerifyDiagnostics()

            'Wrong IL
            'PEVerify failed
            '[ : Program+VB$StateMachine_2_Call1`1[SM$T]::MoveNext][mdToken=0x600000c][offset 0x00000085][found (unboxed) 'SM$T'] Non-compatible types on the stack.
            verifier.VerifyIL("Program.VB$StateMachine_2_Call1(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      321 (0x141)
  .maxstack  3
  .locals init (Integer V_0,
                System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_1,
                System.Runtime.CompilerServices.YieldAwaitable V_2,
                SM$T V_3,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_4,
                System.Exception V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004b
    IL_000a:  ldloc.0
    IL_000b:  ldc.i4.1
    IL_000c:  beq        IL_00c8
    IL_0011:  call       "Function System.Threading.Tasks.Task.Yield() As System.Runtime.CompilerServices.YieldAwaitable"
    IL_0016:  stloc.2
    IL_0017:  ldloca.s   V_2
    IL_0019:  call       "Function System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter() As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_001e:  stloc.1
    IL_001f:  ldloca.s   V_1
    IL_0021:  call       "Function System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.get_IsCompleted() As Boolean"
    IL_0026:  brtrue.s   IL_0067
    IL_0028:  ldarg.0
    IL_0029:  ldc.i4.0
    IL_002a:  dup
    IL_002b:  stloc.0
    IL_002c:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0031:  ldarg.0
    IL_0032:  ldloc.1
    IL_0033:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0038:  ldarg.0
    IL_0039:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_003e:  ldloca.s   V_1
    IL_0040:  ldarg.0
    IL_0041:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, Program.VB$StateMachine_2_Call1(Of SM$T))(ByRef System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ByRef Program.VB$StateMachine_2_Call1(Of SM$T))"
    IL_0046:  leave      IL_0140
    IL_004b:  ldarg.0
    IL_004c:  ldc.i4.m1
    IL_004d:  dup
    IL_004e:  stloc.0
    IL_004f:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0054:  ldarg.0
    IL_0055:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_005a:  stloc.1
    IL_005b:  ldarg.0
    IL_005c:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0061:  initobj    "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0067:  ldloca.s   V_1
    IL_0069:  call       "Sub System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()"
    IL_006e:  ldloca.s   V_1
    IL_0070:  initobj    "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0076:  ldarg.0
    IL_0077:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_007c:  ldloca.s   V_3
    IL_007e:  initobj    "SM$T"
    IL_0084:  ldloc.3
    IL_0085:  beq.s      IL_0105
    IL_0087:  ldarg.0
    IL_0088:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_008d:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_0092:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_0097:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_009c:  stloc.s    V_4
    IL_009e:  ldloca.s   V_4
    IL_00a0:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_00a5:  brtrue.s   IL_00e5
    IL_00a7:  ldarg.0
    IL_00a8:  ldc.i4.1
    IL_00a9:  dup
    IL_00aa:  stloc.0
    IL_00ab:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_00b0:  ldarg.0
    IL_00b1:  ldloc.s    V_4
    IL_00b3:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A1 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00b8:  ldarg.0
    IL_00b9:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00be:  ldloca.s   V_4
    IL_00c0:  ldarg.0
    IL_00c1:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_2_Call1(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_2_Call1(Of SM$T))"
    IL_00c6:  leave.s    IL_0140
    IL_00c8:  ldarg.0
    IL_00c9:  ldc.i4.m1
    IL_00ca:  dup
    IL_00cb:  stloc.0
    IL_00cc:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_00d1:  ldarg.0
    IL_00d2:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A1 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00d7:  stloc.s    V_4
    IL_00d9:  ldarg.0
    IL_00da:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$A1 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00df:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00e5:  ldarg.0
    IL_00e6:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_00eb:  ldloca.s   V_4
    IL_00ed:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_00f2:  ldloca.s   V_4
    IL_00f4:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00fa:  constrained. "SM$T"
    IL_0100:  callvirt   "Sub IMoveable.GetName(Integer)"
    IL_0105:  leave.s    IL_012b
  }
  catch System.Exception
  {
    IL_0107:  dup
    IL_0108:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_010d:  stloc.s    V_5
    IL_010f:  ldarg.0
    IL_0110:  ldc.i4.s   -2
    IL_0112:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0117:  ldarg.0
    IL_0118:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_011d:  ldloc.s    V_5
    IL_011f:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_0124:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_0129:  leave.s    IL_0140
  }
  IL_012b:  ldarg.0
  IL_012c:  ldc.i4.s   -2
  IL_012e:  dup
  IL_012f:  stloc.0
  IL_0130:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_0135:  ldarg.0
  IL_0136:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_013b:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_0140:  ret
}
]]>)

            'Wrong IL
            verifier.VerifyIL("Program.VB$StateMachine_3_Call2(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      333 (0x14d)
  .maxstack  3
  .locals init (Integer V_0,
                System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_1,
                System.Runtime.CompilerServices.YieldAwaitable V_2,
                SM$T V_3,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_4,
                System.Exception V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004b
    IL_000a:  ldloc.0
    IL_000b:  ldc.i4.1
    IL_000c:  beq        IL_00d4
    IL_0011:  call       "Function System.Threading.Tasks.Task.Yield() As System.Runtime.CompilerServices.YieldAwaitable"
    IL_0016:  stloc.2
    IL_0017:  ldloca.s   V_2
    IL_0019:  call       "Function System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter() As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_001e:  stloc.1
    IL_001f:  ldloca.s   V_1
    IL_0021:  call       "Function System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.get_IsCompleted() As Boolean"
    IL_0026:  brtrue.s   IL_0067
    IL_0028:  ldarg.0
    IL_0029:  ldc.i4.0
    IL_002a:  dup
    IL_002b:  stloc.0
    IL_002c:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_0031:  ldarg.0
    IL_0032:  ldloc.1
    IL_0033:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0038:  ldarg.0
    IL_0039:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_003e:  ldloca.s   V_1
    IL_0040:  ldarg.0
    IL_0041:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, Program.VB$StateMachine_3_Call2(Of SM$T))(ByRef System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ByRef Program.VB$StateMachine_3_Call2(Of SM$T))"
    IL_0046:  leave      IL_014c
    IL_004b:  ldarg.0
    IL_004c:  ldc.i4.m1
    IL_004d:  dup
    IL_004e:  stloc.0
    IL_004f:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_0054:  ldarg.0
    IL_0055:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_005a:  stloc.1
    IL_005b:  ldarg.0
    IL_005c:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0061:  initobj    "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0067:  ldloca.s   V_1
    IL_0069:  call       "Sub System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()"
    IL_006e:  ldloca.s   V_1
    IL_0070:  initobj    "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0076:  ldloca.s   V_3
    IL_0078:  initobj    "SM$T"
    IL_007e:  ldloc.3
    IL_007f:  box        "SM$T"
    IL_0084:  brtrue.s   IL_0093
    IL_0086:  ldarg.0
    IL_0087:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_008c:  box        "SM$T"
    IL_0091:  brfalse.s  IL_0111
    IL_0093:  ldarg.0
    IL_0094:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_0099:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_009e:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_00a3:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00a8:  stloc.s    V_4
    IL_00aa:  ldloca.s   V_4
    IL_00ac:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_00b1:  brtrue.s   IL_00f1
    IL_00b3:  ldarg.0
    IL_00b4:  ldc.i4.1
    IL_00b5:  dup
    IL_00b6:  stloc.0
    IL_00b7:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_00bc:  ldarg.0
    IL_00bd:  ldloc.s    V_4
    IL_00bf:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A1 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00c4:  ldarg.0
    IL_00c5:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00ca:  ldloca.s   V_4
    IL_00cc:  ldarg.0
    IL_00cd:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_3_Call2(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_3_Call2(Of SM$T))"
    IL_00d2:  leave.s    IL_014c
    IL_00d4:  ldarg.0
    IL_00d5:  ldc.i4.m1
    IL_00d6:  dup
    IL_00d7:  stloc.0
    IL_00d8:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_00dd:  ldarg.0
    IL_00de:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A1 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00e3:  stloc.s    V_4
    IL_00e5:  ldarg.0
    IL_00e6:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$A1 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00eb:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00f1:  ldarg.0
    IL_00f2:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_00f7:  ldloca.s   V_4
    IL_00f9:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_00fe:  ldloca.s   V_4
    IL_0100:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0106:  constrained. "SM$T"
    IL_010c:  callvirt   "Sub IMoveable.GetName(Integer)"
    IL_0111:  leave.s    IL_0137
  }
  catch System.Exception
  {
    IL_0113:  dup
    IL_0114:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_0119:  stloc.s    V_5
    IL_011b:  ldarg.0
    IL_011c:  ldc.i4.s   -2
    IL_011e:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_0123:  ldarg.0
    IL_0124:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_0129:  ldloc.s    V_5
    IL_012b:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_0130:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_0135:  leave.s    IL_014c
  }
  IL_0137:  ldarg.0
  IL_0138:  ldc.i4.s   -2
  IL_013a:  dup
  IL_013b:  stloc.0
  IL_013c:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
  IL_0141:  ldarg.0
  IL_0142:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_0147:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_014c:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Call_Conditional_Struct_Async_02()
            Dim comp =
<compilation>
    <file>
Imports System
Imports System.Threading.Tasks

Interface IMoveable
    Sub GetName(x As Integer)
End Interface

Structure Item
    Implements IMoveable

    Public Property Name As String

    Public Sub GetName(x As Integer) Implements IMoveable.GetName
        Console.WriteLine("Position GetName for item '{0}'", Me.Name)
    End Sub
End Structure

Class Program
    Shared Sub Main()
        Dim item2 = New Item With {.Name = "2"}
        Call2(item2).Wait()
    End Sub

    Private Shared Async Function Call2(Of T As {IMoveable})(item As T) As Task
        await Task.Yield()
        item?.GetName(await GetOffsetAsync(GetOffset(item)))
    End Function

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function

    Shared Function GetOffsetAsync(i As Integer) As Task(Of Integer)
        Return Task.FromResult(i)
    End Function
End Class
    </file>
</compilation>

            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position GetName for item '-1'
").VerifyDiagnostics()
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Assignment_Compound_Property_Class()
            Dim comp =
<compilation>
    <file>
Imports System

Interface IMoveable
    Property Position As Integer
End Interface

Class Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Class

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1)

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2)
    End Sub

    Private Shared Sub Call1(Of T As {Class, IMoveable})(item As T)
        item.Position += GetOffset(item)
    End Sub

    Private Shared Sub Call2(Of T As {IMoveable})(item As T)
        item.Position += GetOffset(item)
    End Sub

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function
End Class
    </file>
</compilation>

            ' Wrong output on some frameworks
            '            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
            '"
            'Position get for item '1'
            'Position set for item '1'
            'Position get for item '2'
            'Position set for item '2'
            '").VerifyDiagnostics()
            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe).VerifyDiagnostics()

            'Wrong IL
            verifier.VerifyIL("Program.Call1(Of T)",
            <![CDATA[
{
  // Code size       35 (0x23)
  .maxstack  3
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldarga.s   V_0
  IL_0004:  constrained. "T"
  IL_000a:  callvirt   "Function IMoveable.get_Position() As Integer"
  IL_000f:  ldarga.s   V_0
  IL_0011:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0016:  add.ovf
  IL_0017:  constrained. "T"
  IL_001d:  callvirt   "Sub IMoveable.set_Position(Integer)"
  IL_0022:  ret
}
]]>)

            'Wrong IL
            verifier.VerifyIL("Program.Call2(Of T)",
            <![CDATA[
{
  // Code size       35 (0x23)
  .maxstack  3
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldarga.s   V_0
  IL_0004:  constrained. "T"
  IL_000a:  callvirt   "Function IMoveable.get_Position() As Integer"
  IL_000f:  ldarga.s   V_0
  IL_0011:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0016:  add.ovf
  IL_0017:  constrained. "T"
  IL_001d:  callvirt   "Sub IMoveable.set_Position(Integer)"
  IL_0022:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Assignment_Compound_Property_Struct()
            Dim comp =
<compilation>
    <file>
Imports System

Interface IMoveable
    Property Position As Integer
End Interface

Structure Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Structure

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1)

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2)
    End Sub

    Private Shared Sub Call1(Of T As {Structure, IMoveable})(item As T)
        item.Position += GetOffset(item)
    End Sub

    Private Shared Sub Call2(Of T As {IMoveable})(item As T)
        item.Position += GetOffset(item)
    End Sub

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function
End Class
    </file>
</compilation>

            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '1'
Position set for item '-1'
Position get for item '2'
Position set for item '-2'
").VerifyDiagnostics()

            verifier.VerifyIL("Program.Call1(Of T)",
            <![CDATA[
{
  // Code size       35 (0x23)
  .maxstack  3
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldarga.s   V_0
  IL_0004:  constrained. "T"
  IL_000a:  callvirt   "Function IMoveable.get_Position() As Integer"
  IL_000f:  ldarga.s   V_0
  IL_0011:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0016:  add.ovf
  IL_0017:  constrained. "T"
  IL_001d:  callvirt   "Sub IMoveable.set_Position(Integer)"
  IL_0022:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Assignment_Compound_Property_Class_Ref()
            Dim comp =
<compilation>
    <file>
Imports System

Interface IMoveable
    Property Position As Integer
End Interface

Class Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Class

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1)

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2)
    End Sub

    Private Shared Sub Call1(Of T As {Class, IMoveable})(ByRef item As T)
        item.Position += GetOffset(item)
    End Sub

    Private Shared Sub Call2(Of T As {IMoveable})(ByRef item As T)
        item.Position += GetOffset(item)
    End Sub

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function
End Class
    </file>
</compilation>

            ' Wrong output on some frameworks
            '            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
            '"
            'Position get for item '1'
            'Position set for item '1'
            'Position get for item '2'
            'Position set for item '2'
            '").VerifyDiagnostics()
            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe).VerifyDiagnostics()

            'Wrong IL
            verifier.VerifyIL("Program.Call1(Of T)",
            <![CDATA[
{
  // Code size       32 (0x20)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  constrained. "T"
  IL_0008:  callvirt   "Function IMoveable.get_Position() As Integer"
  IL_000d:  ldarg.0
  IL_000e:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0013:  add.ovf
  IL_0014:  constrained. "T"
  IL_001a:  callvirt   "Sub IMoveable.set_Position(Integer)"
  IL_001f:  ret
}
]]>)

            'Wrong IL
            verifier.VerifyIL("Program.Call2(Of T)",
            <![CDATA[
{
  // Code size       32 (0x20)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  constrained. "T"
  IL_0008:  callvirt   "Function IMoveable.get_Position() As Integer"
  IL_000d:  ldarg.0
  IL_000e:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0013:  add.ovf
  IL_0014:  constrained. "T"
  IL_001a:  callvirt   "Sub IMoveable.set_Position(Integer)"
  IL_001f:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Assignment_Compound_Property_Struct_Ref()
            Dim comp =
<compilation>
    <file>
Imports System

Interface IMoveable
    Property Position As Integer
End Interface

Structure Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Structure

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1)

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2)
    End Sub

    Private Shared Sub Call1(Of T As {Structure, IMoveable})(ByRef item As T)
        item.Position += GetOffset(item)
    End Sub

    Private Shared Sub Call2(Of T As {IMoveable})(ByRef item As T)
        item.Position += GetOffset(item)
    End Sub

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function
End Class
    </file>
</compilation>

            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '1'
Position set for item '-1'
Position get for item '2'
Position set for item '-2'
").VerifyDiagnostics()

            verifier.VerifyIL("Program.Call1(Of T)",
            <![CDATA[
{
  // Code size       32 (0x20)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  constrained. "T"
  IL_0008:  callvirt   "Function IMoveable.get_Position() As Integer"
  IL_000d:  ldarg.0
  IL_000e:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0013:  add.ovf
  IL_0014:  constrained. "T"
  IL_001a:  callvirt   "Sub IMoveable.set_Position(Integer)"
  IL_001f:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Assignment_Compound_Property_Class_Async_01()
            Dim comp =
<compilation>
    <file>
Imports System
Imports System.Threading.Tasks

Interface IMoveable
    Property Position As Integer
End Interface

Class Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Class

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1).Wait()

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2).Wait()
    End Sub

    Private Shared Async Function Call1(Of T As {Class, IMoveable})(item As T) As Task
        item.Position += await GetOffsetAsync(GetOffset(item))
    End Function

    Private Shared Async Function Call2(Of T As {IMoveable})(item As T) As Task
        item.Position += await GetOffsetAsync(GetOffset(item))
    End Function

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function

    Shared Function GetOffsetAsync(i As Integer) As Task(Of Integer)
        Return Task.FromResult(i)
    End Function
End Class
    </file>
</compilation>

            ' Wrong output
            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '1'
Position set for item '-1'
Position get for item '2'
Position set for item '-2'
").VerifyDiagnostics()

            'Wrong IL
            verifier.VerifyIL("Program.VB$StateMachine_2_Call1(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      221 (0xdd)
  .maxstack  4
  .locals init (Integer V_0,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_1,
                System.Exception V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0060
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0011:  constrained. "SM$T"
    IL_0017:  callvirt   "Function IMoveable.get_Position() As Integer"
    IL_001c:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As Integer"
    IL_0021:  ldarg.0
    IL_0022:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0027:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_002c:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_0031:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0036:  stloc.1
    IL_0037:  ldloca.s   V_1
    IL_0039:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_003e:  brtrue.s   IL_007c
    IL_0040:  ldarg.0
    IL_0041:  ldc.i4.0
    IL_0042:  dup
    IL_0043:  stloc.0
    IL_0044:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0049:  ldarg.0
    IL_004a:  ldloc.1
    IL_004b:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0050:  ldarg.0
    IL_0051:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_0056:  ldloca.s   V_1
    IL_0058:  ldarg.0
    IL_0059:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_2_Call1(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_2_Call1(Of SM$T))"
    IL_005e:  leave.s    IL_00dc
    IL_0060:  ldarg.0
    IL_0061:  ldc.i4.m1
    IL_0062:  dup
    IL_0063:  stloc.0
    IL_0064:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0069:  ldarg.0
    IL_006a:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_006f:  stloc.1
    IL_0070:  ldarg.0
    IL_0071:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0076:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_007c:  ldarg.0
    IL_007d:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0082:  ldarg.0
    IL_0083:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As Integer"
    IL_0088:  ldloca.s   V_1
    IL_008a:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_008f:  ldloca.s   V_1
    IL_0091:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0097:  add.ovf
    IL_0098:  constrained. "SM$T"
    IL_009e:  callvirt   "Sub IMoveable.set_Position(Integer)"
    IL_00a3:  leave.s    IL_00c7
  }
  catch System.Exception
  {
    IL_00a5:  dup
    IL_00a6:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_00ab:  stloc.2
    IL_00ac:  ldarg.0
    IL_00ad:  ldc.i4.s   -2
    IL_00af:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_00b4:  ldarg.0
    IL_00b5:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00ba:  ldloc.2
    IL_00bb:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_00c0:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_00c5:  leave.s    IL_00dc
  }
  IL_00c7:  ldarg.0
  IL_00c8:  ldc.i4.s   -2
  IL_00ca:  dup
  IL_00cb:  stloc.0
  IL_00cc:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_00d1:  ldarg.0
  IL_00d2:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_00d7:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_00dc:  ret
}
]]>)

            'Wrong IL
            verifier.VerifyIL("Program.VB$StateMachine_3_Call2(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      221 (0xdd)
  .maxstack  4
  .locals init (Integer V_0,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_1,
                System.Exception V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0060
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_0011:  constrained. "SM$T"
    IL_0017:  callvirt   "Function IMoveable.get_Position() As Integer"
    IL_001c:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$U1 As Integer"
    IL_0021:  ldarg.0
    IL_0022:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_0027:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_002c:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_0031:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0036:  stloc.1
    IL_0037:  ldloca.s   V_1
    IL_0039:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_003e:  brtrue.s   IL_007c
    IL_0040:  ldarg.0
    IL_0041:  ldc.i4.0
    IL_0042:  dup
    IL_0043:  stloc.0
    IL_0044:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_0049:  ldarg.0
    IL_004a:  ldloc.1
    IL_004b:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0050:  ldarg.0
    IL_0051:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_0056:  ldloca.s   V_1
    IL_0058:  ldarg.0
    IL_0059:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_3_Call2(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_3_Call2(Of SM$T))"
    IL_005e:  leave.s    IL_00dc
    IL_0060:  ldarg.0
    IL_0061:  ldc.i4.m1
    IL_0062:  dup
    IL_0063:  stloc.0
    IL_0064:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_0069:  ldarg.0
    IL_006a:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_006f:  stloc.1
    IL_0070:  ldarg.0
    IL_0071:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0076:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_007c:  ldarg.0
    IL_007d:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_0082:  ldarg.0
    IL_0083:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$U1 As Integer"
    IL_0088:  ldloca.s   V_1
    IL_008a:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_008f:  ldloca.s   V_1
    IL_0091:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0097:  add.ovf
    IL_0098:  constrained. "SM$T"
    IL_009e:  callvirt   "Sub IMoveable.set_Position(Integer)"
    IL_00a3:  leave.s    IL_00c7
  }
  catch System.Exception
  {
    IL_00a5:  dup
    IL_00a6:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_00ab:  stloc.2
    IL_00ac:  ldarg.0
    IL_00ad:  ldc.i4.s   -2
    IL_00af:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_00b4:  ldarg.0
    IL_00b5:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00ba:  ldloc.2
    IL_00bb:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_00c0:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_00c5:  leave.s    IL_00dc
  }
  IL_00c7:  ldarg.0
  IL_00c8:  ldc.i4.s   -2
  IL_00ca:  dup
  IL_00cb:  stloc.0
  IL_00cc:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
  IL_00d1:  ldarg.0
  IL_00d2:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_00d7:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_00dc:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Assignment_Compound_Property_Struct_Async_01()
            Dim comp =
<compilation>
    <file>
Imports System
Imports System.Threading.Tasks

Interface IMoveable
    Property Position As Integer
End Interface

Structure Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Structure

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1).Wait()

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2).Wait()
    End Sub

    Private Shared Async Function Call1(Of T As {Structure, IMoveable})(item As T) As Task
        item.Position += await GetOffsetAsync(GetOffset(item))
    End Function

    Private Shared Async Function Call2(Of T As {IMoveable})(item As T) As Task
        item.Position += await GetOffsetAsync(GetOffset(item))
    End Function

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function

    Shared Function GetOffsetAsync(i As Integer) As Task(Of Integer)
        Return Task.FromResult(i)
    End Function
End Class
    </file>
</compilation>

            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '1'
Position set for item '-1'
Position get for item '2'
Position set for item '-2'
").VerifyDiagnostics()

            verifier.VerifyIL("Program.VB$StateMachine_2_Call1(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      221 (0xdd)
  .maxstack  4
  .locals init (Integer V_0,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_1,
                System.Exception V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0060
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0011:  constrained. "SM$T"
    IL_0017:  callvirt   "Function IMoveable.get_Position() As Integer"
    IL_001c:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As Integer"
    IL_0021:  ldarg.0
    IL_0022:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0027:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_002c:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_0031:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0036:  stloc.1
    IL_0037:  ldloca.s   V_1
    IL_0039:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_003e:  brtrue.s   IL_007c
    IL_0040:  ldarg.0
    IL_0041:  ldc.i4.0
    IL_0042:  dup
    IL_0043:  stloc.0
    IL_0044:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0049:  ldarg.0
    IL_004a:  ldloc.1
    IL_004b:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0050:  ldarg.0
    IL_0051:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_0056:  ldloca.s   V_1
    IL_0058:  ldarg.0
    IL_0059:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_2_Call1(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_2_Call1(Of SM$T))"
    IL_005e:  leave.s    IL_00dc
    IL_0060:  ldarg.0
    IL_0061:  ldc.i4.m1
    IL_0062:  dup
    IL_0063:  stloc.0
    IL_0064:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0069:  ldarg.0
    IL_006a:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_006f:  stloc.1
    IL_0070:  ldarg.0
    IL_0071:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0076:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_007c:  ldarg.0
    IL_007d:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0082:  ldarg.0
    IL_0083:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As Integer"
    IL_0088:  ldloca.s   V_1
    IL_008a:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_008f:  ldloca.s   V_1
    IL_0091:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0097:  add.ovf
    IL_0098:  constrained. "SM$T"
    IL_009e:  callvirt   "Sub IMoveable.set_Position(Integer)"
    IL_00a3:  leave.s    IL_00c7
  }
  catch System.Exception
  {
    IL_00a5:  dup
    IL_00a6:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_00ab:  stloc.2
    IL_00ac:  ldarg.0
    IL_00ad:  ldc.i4.s   -2
    IL_00af:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_00b4:  ldarg.0
    IL_00b5:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00ba:  ldloc.2
    IL_00bb:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_00c0:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_00c5:  leave.s    IL_00dc
  }
  IL_00c7:  ldarg.0
  IL_00c8:  ldc.i4.s   -2
  IL_00ca:  dup
  IL_00cb:  stloc.0
  IL_00cc:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_00d1:  ldarg.0
  IL_00d2:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_00d7:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_00dc:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Assignment_Compound_Property_Class_Async_02()
            Dim comp =
<compilation>
    <file>
Imports System
Imports System.Threading.Tasks

Interface IMoveable
    Property Position As Integer
End Interface

Class Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Class

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1).Wait()

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2).Wait()
    End Sub

    Private Shared Async Function Call1(Of T As {Class, IMoveable})(item As T) As Task
        await Task.Yield()
        item.Position += await GetOffsetAsync(GetOffset(item))
    End Function

    Private Shared Async Function Call2(Of T As {IMoveable})(item As T) As Task
        await Task.Yield()
        item.Position += await GetOffsetAsync(GetOffset(item))
    End Function

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function

    Shared Function GetOffsetAsync(i As Integer) As Task(Of Integer)
        Return Task.FromResult(i)
    End Function
End Class
    </file>
</compilation>

            ' Wrong output
            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '1'
Position set for item '-1'
Position get for item '2'
Position set for item '-2'
").VerifyDiagnostics()

            'Wrong IL
            verifier.VerifyIL("Program.VB$StateMachine_2_Call1(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      331 (0x14b)
  .maxstack  4
  .locals init (Integer V_0,
                System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_1,
                System.Runtime.CompilerServices.YieldAwaitable V_2,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004b
    IL_000a:  ldloc.0
    IL_000b:  ldc.i4.1
    IL_000c:  beq        IL_00cc
    IL_0011:  call       "Function System.Threading.Tasks.Task.Yield() As System.Runtime.CompilerServices.YieldAwaitable"
    IL_0016:  stloc.2
    IL_0017:  ldloca.s   V_2
    IL_0019:  call       "Function System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter() As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_001e:  stloc.1
    IL_001f:  ldloca.s   V_1
    IL_0021:  call       "Function System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.get_IsCompleted() As Boolean"
    IL_0026:  brtrue.s   IL_0067
    IL_0028:  ldarg.0
    IL_0029:  ldc.i4.0
    IL_002a:  dup
    IL_002b:  stloc.0
    IL_002c:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0031:  ldarg.0
    IL_0032:  ldloc.1
    IL_0033:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0038:  ldarg.0
    IL_0039:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_003e:  ldloca.s   V_1
    IL_0040:  ldarg.0
    IL_0041:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, Program.VB$StateMachine_2_Call1(Of SM$T))(ByRef System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ByRef Program.VB$StateMachine_2_Call1(Of SM$T))"
    IL_0046:  leave      IL_014a
    IL_004b:  ldarg.0
    IL_004c:  ldc.i4.m1
    IL_004d:  dup
    IL_004e:  stloc.0
    IL_004f:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0054:  ldarg.0
    IL_0055:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_005a:  stloc.1
    IL_005b:  ldarg.0
    IL_005c:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0061:  initobj    "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0067:  ldloca.s   V_1
    IL_0069:  call       "Sub System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()"
    IL_006e:  ldloca.s   V_1
    IL_0070:  initobj    "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0076:  ldarg.0
    IL_0077:  ldarg.0
    IL_0078:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_007d:  constrained. "SM$T"
    IL_0083:  callvirt   "Function IMoveable.get_Position() As Integer"
    IL_0088:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As Integer"
    IL_008d:  ldarg.0
    IL_008e:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0093:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_0098:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_009d:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00a2:  stloc.3
    IL_00a3:  ldloca.s   V_3
    IL_00a5:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_00aa:  brtrue.s   IL_00e8
    IL_00ac:  ldarg.0
    IL_00ad:  ldc.i4.1
    IL_00ae:  dup
    IL_00af:  stloc.0
    IL_00b0:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_00b5:  ldarg.0
    IL_00b6:  ldloc.3
    IL_00b7:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A1 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00bc:  ldarg.0
    IL_00bd:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00c2:  ldloca.s   V_3
    IL_00c4:  ldarg.0
    IL_00c5:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_2_Call1(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_2_Call1(Of SM$T))"
    IL_00ca:  leave.s    IL_014a
    IL_00cc:  ldarg.0
    IL_00cd:  ldc.i4.m1
    IL_00ce:  dup
    IL_00cf:  stloc.0
    IL_00d0:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_00d5:  ldarg.0
    IL_00d6:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A1 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00db:  stloc.3
    IL_00dc:  ldarg.0
    IL_00dd:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$A1 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00e2:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00e8:  ldarg.0
    IL_00e9:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_00ee:  ldarg.0
    IL_00ef:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As Integer"
    IL_00f4:  ldloca.s   V_3
    IL_00f6:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_00fb:  ldloca.s   V_3
    IL_00fd:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0103:  add.ovf
    IL_0104:  constrained. "SM$T"
    IL_010a:  callvirt   "Sub IMoveable.set_Position(Integer)"
    IL_010f:  leave.s    IL_0135
  }
  catch System.Exception
  {
    IL_0111:  dup
    IL_0112:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_0117:  stloc.s    V_4
    IL_0119:  ldarg.0
    IL_011a:  ldc.i4.s   -2
    IL_011c:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0121:  ldarg.0
    IL_0122:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_0127:  ldloc.s    V_4
    IL_0129:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_012e:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_0133:  leave.s    IL_014a
  }
  IL_0135:  ldarg.0
  IL_0136:  ldc.i4.s   -2
  IL_0138:  dup
  IL_0139:  stloc.0
  IL_013a:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_013f:  ldarg.0
  IL_0140:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_0145:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_014a:  ret
}
]]>)

            'Wrong IL
            verifier.VerifyIL("Program.VB$StateMachine_3_Call2(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      331 (0x14b)
  .maxstack  4
  .locals init (Integer V_0,
                System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_1,
                System.Runtime.CompilerServices.YieldAwaitable V_2,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004b
    IL_000a:  ldloc.0
    IL_000b:  ldc.i4.1
    IL_000c:  beq        IL_00cc
    IL_0011:  call       "Function System.Threading.Tasks.Task.Yield() As System.Runtime.CompilerServices.YieldAwaitable"
    IL_0016:  stloc.2
    IL_0017:  ldloca.s   V_2
    IL_0019:  call       "Function System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter() As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_001e:  stloc.1
    IL_001f:  ldloca.s   V_1
    IL_0021:  call       "Function System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.get_IsCompleted() As Boolean"
    IL_0026:  brtrue.s   IL_0067
    IL_0028:  ldarg.0
    IL_0029:  ldc.i4.0
    IL_002a:  dup
    IL_002b:  stloc.0
    IL_002c:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_0031:  ldarg.0
    IL_0032:  ldloc.1
    IL_0033:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0038:  ldarg.0
    IL_0039:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_003e:  ldloca.s   V_1
    IL_0040:  ldarg.0
    IL_0041:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, Program.VB$StateMachine_3_Call2(Of SM$T))(ByRef System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ByRef Program.VB$StateMachine_3_Call2(Of SM$T))"
    IL_0046:  leave      IL_014a
    IL_004b:  ldarg.0
    IL_004c:  ldc.i4.m1
    IL_004d:  dup
    IL_004e:  stloc.0
    IL_004f:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_0054:  ldarg.0
    IL_0055:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_005a:  stloc.1
    IL_005b:  ldarg.0
    IL_005c:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0061:  initobj    "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0067:  ldloca.s   V_1
    IL_0069:  call       "Sub System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()"
    IL_006e:  ldloca.s   V_1
    IL_0070:  initobj    "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0076:  ldarg.0
    IL_0077:  ldarg.0
    IL_0078:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_007d:  constrained. "SM$T"
    IL_0083:  callvirt   "Function IMoveable.get_Position() As Integer"
    IL_0088:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$U1 As Integer"
    IL_008d:  ldarg.0
    IL_008e:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_0093:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_0098:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_009d:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00a2:  stloc.3
    IL_00a3:  ldloca.s   V_3
    IL_00a5:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_00aa:  brtrue.s   IL_00e8
    IL_00ac:  ldarg.0
    IL_00ad:  ldc.i4.1
    IL_00ae:  dup
    IL_00af:  stloc.0
    IL_00b0:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_00b5:  ldarg.0
    IL_00b6:  ldloc.3
    IL_00b7:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A1 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00bc:  ldarg.0
    IL_00bd:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00c2:  ldloca.s   V_3
    IL_00c4:  ldarg.0
    IL_00c5:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_3_Call2(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_3_Call2(Of SM$T))"
    IL_00ca:  leave.s    IL_014a
    IL_00cc:  ldarg.0
    IL_00cd:  ldc.i4.m1
    IL_00ce:  dup
    IL_00cf:  stloc.0
    IL_00d0:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_00d5:  ldarg.0
    IL_00d6:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A1 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00db:  stloc.3
    IL_00dc:  ldarg.0
    IL_00dd:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$A1 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00e2:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00e8:  ldarg.0
    IL_00e9:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_00ee:  ldarg.0
    IL_00ef:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$U1 As Integer"
    IL_00f4:  ldloca.s   V_3
    IL_00f6:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_00fb:  ldloca.s   V_3
    IL_00fd:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0103:  add.ovf
    IL_0104:  constrained. "SM$T"
    IL_010a:  callvirt   "Sub IMoveable.set_Position(Integer)"
    IL_010f:  leave.s    IL_0135
  }
  catch System.Exception
  {
    IL_0111:  dup
    IL_0112:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_0117:  stloc.s    V_4
    IL_0119:  ldarg.0
    IL_011a:  ldc.i4.s   -2
    IL_011c:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_0121:  ldarg.0
    IL_0122:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_0127:  ldloc.s    V_4
    IL_0129:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_012e:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_0133:  leave.s    IL_014a
  }
  IL_0135:  ldarg.0
  IL_0136:  ldc.i4.s   -2
  IL_0138:  dup
  IL_0139:  stloc.0
  IL_013a:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
  IL_013f:  ldarg.0
  IL_0140:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_0145:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_014a:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Assignment_Compound_Property_Struct_Async_02()
            Dim comp =
<compilation>
    <file>
Imports System
Imports System.Threading.Tasks

Interface IMoveable
    Property Position As Integer
End Interface

Structure Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Structure

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1).Wait()

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2).Wait()
    End Sub

    Private Shared Async Function Call1(Of T As {Structure, IMoveable})(item As T) As Task
        await Task.Yield()
        item.Position += await GetOffsetAsync(GetOffset(item))
    End Function

    Private Shared Async Function Call2(Of T As {IMoveable})(item As T) As Task
        await Task.Yield()
        item.Position += await GetOffsetAsync(GetOffset(item))
    End Function

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function

    Shared Function GetOffsetAsync(i As Integer) As Task(Of Integer)
        Return Task.FromResult(i)
    End Function
End Class
    </file>
</compilation>

            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '1'
Position set for item '-1'
Position get for item '2'
Position set for item '-2'
").VerifyDiagnostics()

            verifier.VerifyIL("Program.VB$StateMachine_2_Call1(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      331 (0x14b)
  .maxstack  4
  .locals init (Integer V_0,
                System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_1,
                System.Runtime.CompilerServices.YieldAwaitable V_2,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004b
    IL_000a:  ldloc.0
    IL_000b:  ldc.i4.1
    IL_000c:  beq        IL_00cc
    IL_0011:  call       "Function System.Threading.Tasks.Task.Yield() As System.Runtime.CompilerServices.YieldAwaitable"
    IL_0016:  stloc.2
    IL_0017:  ldloca.s   V_2
    IL_0019:  call       "Function System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter() As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_001e:  stloc.1
    IL_001f:  ldloca.s   V_1
    IL_0021:  call       "Function System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.get_IsCompleted() As Boolean"
    IL_0026:  brtrue.s   IL_0067
    IL_0028:  ldarg.0
    IL_0029:  ldc.i4.0
    IL_002a:  dup
    IL_002b:  stloc.0
    IL_002c:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0031:  ldarg.0
    IL_0032:  ldloc.1
    IL_0033:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0038:  ldarg.0
    IL_0039:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_003e:  ldloca.s   V_1
    IL_0040:  ldarg.0
    IL_0041:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, Program.VB$StateMachine_2_Call1(Of SM$T))(ByRef System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ByRef Program.VB$StateMachine_2_Call1(Of SM$T))"
    IL_0046:  leave      IL_014a
    IL_004b:  ldarg.0
    IL_004c:  ldc.i4.m1
    IL_004d:  dup
    IL_004e:  stloc.0
    IL_004f:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0054:  ldarg.0
    IL_0055:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_005a:  stloc.1
    IL_005b:  ldarg.0
    IL_005c:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0061:  initobj    "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0067:  ldloca.s   V_1
    IL_0069:  call       "Sub System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()"
    IL_006e:  ldloca.s   V_1
    IL_0070:  initobj    "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0076:  ldarg.0
    IL_0077:  ldarg.0
    IL_0078:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_007d:  constrained. "SM$T"
    IL_0083:  callvirt   "Function IMoveable.get_Position() As Integer"
    IL_0088:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As Integer"
    IL_008d:  ldarg.0
    IL_008e:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0093:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_0098:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_009d:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00a2:  stloc.3
    IL_00a3:  ldloca.s   V_3
    IL_00a5:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_00aa:  brtrue.s   IL_00e8
    IL_00ac:  ldarg.0
    IL_00ad:  ldc.i4.1
    IL_00ae:  dup
    IL_00af:  stloc.0
    IL_00b0:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_00b5:  ldarg.0
    IL_00b6:  ldloc.3
    IL_00b7:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A1 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00bc:  ldarg.0
    IL_00bd:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00c2:  ldloca.s   V_3
    IL_00c4:  ldarg.0
    IL_00c5:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_2_Call1(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_2_Call1(Of SM$T))"
    IL_00ca:  leave.s    IL_014a
    IL_00cc:  ldarg.0
    IL_00cd:  ldc.i4.m1
    IL_00ce:  dup
    IL_00cf:  stloc.0
    IL_00d0:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_00d5:  ldarg.0
    IL_00d6:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A1 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00db:  stloc.3
    IL_00dc:  ldarg.0
    IL_00dd:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$A1 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00e2:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00e8:  ldarg.0
    IL_00e9:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_00ee:  ldarg.0
    IL_00ef:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As Integer"
    IL_00f4:  ldloca.s   V_3
    IL_00f6:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_00fb:  ldloca.s   V_3
    IL_00fd:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0103:  add.ovf
    IL_0104:  constrained. "SM$T"
    IL_010a:  callvirt   "Sub IMoveable.set_Position(Integer)"
    IL_010f:  leave.s    IL_0135
  }
  catch System.Exception
  {
    IL_0111:  dup
    IL_0112:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_0117:  stloc.s    V_4
    IL_0119:  ldarg.0
    IL_011a:  ldc.i4.s   -2
    IL_011c:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0121:  ldarg.0
    IL_0122:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_0127:  ldloc.s    V_4
    IL_0129:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_012e:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_0133:  leave.s    IL_014a
  }
  IL_0135:  ldarg.0
  IL_0136:  ldc.i4.s   -2
  IL_0138:  dup
  IL_0139:  stloc.0
  IL_013a:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_013f:  ldarg.0
  IL_0140:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_0145:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_014a:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Assignment_Compound_Indexer_Class_Index()
            Dim comp =
<compilation>
    <file>
Imports System

Interface IMoveable
    Property Position(x As Integer) As Integer
End Interface

Class Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Class

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1)

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2)
    End Sub

    Private Shared Sub Call1(Of T As {Class, IMoveable})(item As T)
        item.Position(GetOffset(item)) += 1
    End Sub

    Private Shared Sub Call2(Of T As {IMoveable})(item As T)
        item.Position(GetOffset(item)) += 1
    End Sub

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function
End Class
    </file>
</compilation>

            ' Wrong output
            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '-1'
Position set for item '-1'
Position get for item '-2'
Position set for item '-2'
").VerifyDiagnostics()

            'Wrong IL
            verifier.VerifyIL("Program.Call1(Of T)",
            <![CDATA[
{
  // Code size       39 (0x27)
  .maxstack  4
  .locals init (Integer V_0)
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldarga.s   V_0
  IL_0004:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0009:  dup
  IL_000a:  stloc.0
  IL_000b:  ldarga.s   V_0
  IL_000d:  ldloc.0
  IL_000e:  constrained. "T"
  IL_0014:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
  IL_0019:  ldc.i4.1
  IL_001a:  add.ovf
  IL_001b:  constrained. "T"
  IL_0021:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
  IL_0026:  ret
}
]]>)

            'Wrong IL
            verifier.VerifyIL("Program.Call2(Of T)",
            <![CDATA[
{
  // Code size       39 (0x27)
  .maxstack  4
  .locals init (Integer V_0)
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldarga.s   V_0
  IL_0004:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0009:  dup
  IL_000a:  stloc.0
  IL_000b:  ldarga.s   V_0
  IL_000d:  ldloc.0
  IL_000e:  constrained. "T"
  IL_0014:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
  IL_0019:  ldc.i4.1
  IL_001a:  add.ovf
  IL_001b:  constrained. "T"
  IL_0021:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
  IL_0026:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Assignment_Compound_Indexer_Struct_Index()
            Dim comp =
<compilation>
    <file>
Imports System

Interface IMoveable
    Property Position(x As Integer) As Integer
End Interface

Structure Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Structure

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1)

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2)
    End Sub

    Private Shared Sub Call1(Of T As {Structure, IMoveable})(item As T)
        item.Position(GetOffset(item)) += 1
    End Sub

    Private Shared Sub Call2(Of T As {IMoveable})(item As T)
        item.Position(GetOffset(item)) += 1
    End Sub

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function
End Class
    </file>
</compilation>

            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '-1'
Position set for item '-1'
Position get for item '-2'
Position set for item '-2'
").VerifyDiagnostics()

            verifier.VerifyIL("Program.Call1(Of T)",
            <![CDATA[
{
  // Code size       39 (0x27)
  .maxstack  4
  .locals init (Integer V_0)
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldarga.s   V_0
  IL_0004:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0009:  dup
  IL_000a:  stloc.0
  IL_000b:  ldarga.s   V_0
  IL_000d:  ldloc.0
  IL_000e:  constrained. "T"
  IL_0014:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
  IL_0019:  ldc.i4.1
  IL_001a:  add.ovf
  IL_001b:  constrained. "T"
  IL_0021:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
  IL_0026:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Assignment_Compound_Indexer_Class_Index_Ref()
            Dim comp =
<compilation>
    <file>
Imports System

Interface IMoveable
    Property Position(x As Integer) As Integer
End Interface

Class Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Class

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1)

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2)
    End Sub

    Private Shared Sub Call1(Of T As {Class, IMoveable})(ByRef item As T)
        item.Position(GetOffset(item)) += 1
    End Sub

    Private Shared Sub Call2(Of T As {IMoveable})(ByRef item As T)
        item.Position(GetOffset(item)) += 1
    End Sub

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function
End Class
    </file>
</compilation>

            ' Wrong output
            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '-1'
Position set for item '-1'
Position get for item '-2'
Position set for item '-2'
").VerifyDiagnostics()

            'Wrong IL
            verifier.VerifyIL("Program.Call1(Of T)",
            <![CDATA[
{
  // Code size       36 (0x24)
  .maxstack  4
  .locals init (Integer V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0007:  dup
  IL_0008:  stloc.0
  IL_0009:  ldarg.0
  IL_000a:  ldloc.0
  IL_000b:  constrained. "T"
  IL_0011:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
  IL_0016:  ldc.i4.1
  IL_0017:  add.ovf
  IL_0018:  constrained. "T"
  IL_001e:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
  IL_0023:  ret
}
]]>)

            'Wrong IL
            verifier.VerifyIL("Program.Call2(Of T)",
            <![CDATA[
{
  // Code size       36 (0x24)
  .maxstack  4
  .locals init (Integer V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0007:  dup
  IL_0008:  stloc.0
  IL_0009:  ldarg.0
  IL_000a:  ldloc.0
  IL_000b:  constrained. "T"
  IL_0011:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
  IL_0016:  ldc.i4.1
  IL_0017:  add.ovf
  IL_0018:  constrained. "T"
  IL_001e:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
  IL_0023:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Assignment_Compound_Indexer_Struct_Index_Ref()
            Dim comp =
<compilation>
    <file>
Imports System

Interface IMoveable
    Property Position(x As Integer) As Integer
End Interface

Structure Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Structure

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1)

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2)
    End Sub

    Private Shared Sub Call1(Of T As {Structure, IMoveable})(ByRef item As T)
        item.Position(GetOffset(item)) += 1
    End Sub

    Private Shared Sub Call2(Of T As {IMoveable})(ByRef item As T)
        item.Position(GetOffset(item)) += 1
    End Sub

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function
End Class
    </file>
</compilation>

            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '-1'
Position set for item '-1'
Position get for item '-2'
Position set for item '-2'
").VerifyDiagnostics()

            verifier.VerifyIL("Program.Call1(Of T)",
            <![CDATA[
{
  // Code size       36 (0x24)
  .maxstack  4
  .locals init (Integer V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0007:  dup
  IL_0008:  stloc.0
  IL_0009:  ldarg.0
  IL_000a:  ldloc.0
  IL_000b:  constrained. "T"
  IL_0011:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
  IL_0016:  ldc.i4.1
  IL_0017:  add.ovf
  IL_0018:  constrained. "T"
  IL_001e:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
  IL_0023:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Assignment_Compound_Indexer_Class_Index_Async_01()
            Dim comp =
<compilation>
    <file>
Imports System
Imports System.Threading.Tasks

Interface IMoveable
    Property Position(x As Integer) As Integer
End Interface

Class Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Class

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1).Wait()

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2).Wait()
    End Sub

    Private Shared Async Function Call1(Of T As {Class, IMoveable})(item As T) As Task
        item.Position(await GetOffsetAsync(GetOffset(item))) += 1
    End Function

    Private Shared Async Function Call2(Of T As {IMoveable})(item As T) As Task
        item.Position(await GetOffsetAsync(GetOffset(item))) += 1
    End Function

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function

    Shared Function GetOffsetAsync(i As Integer) As Task(Of Integer)
        Return Task.FromResult(i)
    End Function
End Class
    </file>
</compilation>

            ' Wrong output
            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '-1'
Position set for item '-1'
Position get for item '-2'
Position set for item '-2'
").VerifyDiagnostics()

            'Wrong IL
            verifier.VerifyIL("Program.VB$StateMachine_2_Call1(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      216 (0xd8)
  .maxstack  4
  .locals init (Integer V_0,
                Integer V_1,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004c
    IL_000a:  ldarg.0
    IL_000b:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0010:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_0015:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_001a:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_001f:  stloc.2
    IL_0020:  ldloca.s   V_2
    IL_0022:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_0027:  brtrue.s   IL_0068
    IL_0029:  ldarg.0
    IL_002a:  ldc.i4.0
    IL_002b:  dup
    IL_002c:  stloc.0
    IL_002d:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0032:  ldarg.0
    IL_0033:  ldloc.2
    IL_0034:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0039:  ldarg.0
    IL_003a:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_003f:  ldloca.s   V_2
    IL_0041:  ldarg.0
    IL_0042:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_2_Call1(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_2_Call1(Of SM$T))"
    IL_0047:  leave      IL_00d7
    IL_004c:  ldarg.0
    IL_004d:  ldc.i4.m1
    IL_004e:  dup
    IL_004f:  stloc.0
    IL_0050:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0055:  ldarg.0
    IL_0056:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_005b:  stloc.2
    IL_005c:  ldarg.0
    IL_005d:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0062:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0068:  ldarg.0
    IL_0069:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_006e:  ldloca.s   V_2
    IL_0070:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_0075:  ldloca.s   V_2
    IL_0077:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_007d:  dup
    IL_007e:  stloc.1
    IL_007f:  ldarg.0
    IL_0080:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0085:  ldloc.1
    IL_0086:  constrained. "SM$T"
    IL_008c:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
    IL_0091:  ldc.i4.1
    IL_0092:  add.ovf
    IL_0093:  constrained. "SM$T"
    IL_0099:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
    IL_009e:  leave.s    IL_00c2
  }
  catch System.Exception
  {
    IL_00a0:  dup
    IL_00a1:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_00a6:  stloc.3
    IL_00a7:  ldarg.0
    IL_00a8:  ldc.i4.s   -2
    IL_00aa:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_00af:  ldarg.0
    IL_00b0:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00b5:  ldloc.3
    IL_00b6:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_00bb:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_00c0:  leave.s    IL_00d7
  }
  IL_00c2:  ldarg.0
  IL_00c3:  ldc.i4.s   -2
  IL_00c5:  dup
  IL_00c6:  stloc.0
  IL_00c7:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_00cc:  ldarg.0
  IL_00cd:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_00d2:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_00d7:  ret
}
]]>)

            'Wrong IL
            verifier.VerifyIL("Program.VB$StateMachine_3_Call2(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      216 (0xd8)
  .maxstack  4
  .locals init (Integer V_0,
                Integer V_1,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004c
    IL_000a:  ldarg.0
    IL_000b:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_0010:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_0015:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_001a:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_001f:  stloc.2
    IL_0020:  ldloca.s   V_2
    IL_0022:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_0027:  brtrue.s   IL_0068
    IL_0029:  ldarg.0
    IL_002a:  ldc.i4.0
    IL_002b:  dup
    IL_002c:  stloc.0
    IL_002d:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_0032:  ldarg.0
    IL_0033:  ldloc.2
    IL_0034:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0039:  ldarg.0
    IL_003a:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_003f:  ldloca.s   V_2
    IL_0041:  ldarg.0
    IL_0042:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_3_Call2(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_3_Call2(Of SM$T))"
    IL_0047:  leave      IL_00d7
    IL_004c:  ldarg.0
    IL_004d:  ldc.i4.m1
    IL_004e:  dup
    IL_004f:  stloc.0
    IL_0050:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_0055:  ldarg.0
    IL_0056:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_005b:  stloc.2
    IL_005c:  ldarg.0
    IL_005d:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0062:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0068:  ldarg.0
    IL_0069:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_006e:  ldloca.s   V_2
    IL_0070:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_0075:  ldloca.s   V_2
    IL_0077:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_007d:  dup
    IL_007e:  stloc.1
    IL_007f:  ldarg.0
    IL_0080:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_0085:  ldloc.1
    IL_0086:  constrained. "SM$T"
    IL_008c:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
    IL_0091:  ldc.i4.1
    IL_0092:  add.ovf
    IL_0093:  constrained. "SM$T"
    IL_0099:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
    IL_009e:  leave.s    IL_00c2
  }
  catch System.Exception
  {
    IL_00a0:  dup
    IL_00a1:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_00a6:  stloc.3
    IL_00a7:  ldarg.0
    IL_00a8:  ldc.i4.s   -2
    IL_00aa:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_00af:  ldarg.0
    IL_00b0:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00b5:  ldloc.3
    IL_00b6:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_00bb:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_00c0:  leave.s    IL_00d7
  }
  IL_00c2:  ldarg.0
  IL_00c3:  ldc.i4.s   -2
  IL_00c5:  dup
  IL_00c6:  stloc.0
  IL_00c7:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
  IL_00cc:  ldarg.0
  IL_00cd:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_00d2:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_00d7:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Assignment_Compound_Indexer_Struct_Index_Async_01()
            Dim comp =
<compilation>
    <file>
Imports System
Imports System.Threading.Tasks

Interface IMoveable
    Property Position(x As Integer) As Integer
End Interface

Structure Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Structure

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1).Wait()

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2).Wait()
    End Sub

    Private Shared Async Function Call1(Of T As {Structure, IMoveable})(item As T) As Task
        item.Position(await GetOffsetAsync(GetOffset(item))) += 1
    End Function

    Private Shared Async Function Call2(Of T As {IMoveable})(item As T) As Task
        item.Position(await GetOffsetAsync(GetOffset(item))) += 1
    End Function

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function

    Shared Function GetOffsetAsync(i As Integer) As Task(Of Integer)
        Return Task.FromResult(i)
    End Function
End Class
    </file>
</compilation>

            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '-1'
Position set for item '-1'
Position get for item '-2'
Position set for item '-2'
").VerifyDiagnostics()

            verifier.VerifyIL("Program.VB$StateMachine_2_Call1(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      216 (0xd8)
  .maxstack  4
  .locals init (Integer V_0,
                Integer V_1,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004c
    IL_000a:  ldarg.0
    IL_000b:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0010:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_0015:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_001a:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_001f:  stloc.2
    IL_0020:  ldloca.s   V_2
    IL_0022:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_0027:  brtrue.s   IL_0068
    IL_0029:  ldarg.0
    IL_002a:  ldc.i4.0
    IL_002b:  dup
    IL_002c:  stloc.0
    IL_002d:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0032:  ldarg.0
    IL_0033:  ldloc.2
    IL_0034:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0039:  ldarg.0
    IL_003a:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_003f:  ldloca.s   V_2
    IL_0041:  ldarg.0
    IL_0042:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_2_Call1(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_2_Call1(Of SM$T))"
    IL_0047:  leave      IL_00d7
    IL_004c:  ldarg.0
    IL_004d:  ldc.i4.m1
    IL_004e:  dup
    IL_004f:  stloc.0
    IL_0050:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0055:  ldarg.0
    IL_0056:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_005b:  stloc.2
    IL_005c:  ldarg.0
    IL_005d:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0062:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0068:  ldarg.0
    IL_0069:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_006e:  ldloca.s   V_2
    IL_0070:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_0075:  ldloca.s   V_2
    IL_0077:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_007d:  dup
    IL_007e:  stloc.1
    IL_007f:  ldarg.0
    IL_0080:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0085:  ldloc.1
    IL_0086:  constrained. "SM$T"
    IL_008c:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
    IL_0091:  ldc.i4.1
    IL_0092:  add.ovf
    IL_0093:  constrained. "SM$T"
    IL_0099:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
    IL_009e:  leave.s    IL_00c2
  }
  catch System.Exception
  {
    IL_00a0:  dup
    IL_00a1:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_00a6:  stloc.3
    IL_00a7:  ldarg.0
    IL_00a8:  ldc.i4.s   -2
    IL_00aa:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_00af:  ldarg.0
    IL_00b0:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00b5:  ldloc.3
    IL_00b6:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_00bb:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_00c0:  leave.s    IL_00d7
  }
  IL_00c2:  ldarg.0
  IL_00c3:  ldc.i4.s   -2
  IL_00c5:  dup
  IL_00c6:  stloc.0
  IL_00c7:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_00cc:  ldarg.0
  IL_00cd:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_00d2:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_00d7:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Assignment_Compound_Indexer_Class_Index_Async_02()
            Dim comp =
<compilation>
    <file>
Imports System
Imports System.Threading.Tasks

Interface IMoveable
    Property Position(x as Integer) As Integer
End Interface

Class Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x as Integer) As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Class

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1).Wait()

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2).Wait()
    End Sub

    Private Shared Async Function Call1(Of T As {Class, IMoveable})(item As T) As Task
        await Task.Yield()
        item.Position(await GetOffsetAsync(GetOffset(item))) += 1
    End Function

    Private Shared Async Function Call2(Of T As {IMoveable})(item As T) As Task
        await Task.Yield()
        item.Position(await GetOffsetAsync(GetOffset(item))) += 1
    End Function

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function

    Shared Function GetOffsetAsync(i As Integer) As Task(Of Integer)
        Return Task.FromResult(i)
    End Function
End Class
    </file>
</compilation>

            ' Wrong output
            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '-1'
Position set for item '-1'
Position get for item '-2'
Position set for item '-2'
").VerifyDiagnostics()

            'Wrong IL
            verifier.VerifyIL("Program.VB$StateMachine_2_Call1(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      329 (0x149)
  .maxstack  4
  .locals init (Integer V_0,
                System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_1,
                System.Runtime.CompilerServices.YieldAwaitable V_2,
                Integer V_3,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_4,
                System.Exception V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004b
    IL_000a:  ldloc.0
    IL_000b:  ldc.i4.1
    IL_000c:  beq        IL_00ba
    IL_0011:  call       "Function System.Threading.Tasks.Task.Yield() As System.Runtime.CompilerServices.YieldAwaitable"
    IL_0016:  stloc.2
    IL_0017:  ldloca.s   V_2
    IL_0019:  call       "Function System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter() As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_001e:  stloc.1
    IL_001f:  ldloca.s   V_1
    IL_0021:  call       "Function System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.get_IsCompleted() As Boolean"
    IL_0026:  brtrue.s   IL_0067
    IL_0028:  ldarg.0
    IL_0029:  ldc.i4.0
    IL_002a:  dup
    IL_002b:  stloc.0
    IL_002c:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0031:  ldarg.0
    IL_0032:  ldloc.1
    IL_0033:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0038:  ldarg.0
    IL_0039:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_003e:  ldloca.s   V_1
    IL_0040:  ldarg.0
    IL_0041:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, Program.VB$StateMachine_2_Call1(Of SM$T))(ByRef System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ByRef Program.VB$StateMachine_2_Call1(Of SM$T))"
    IL_0046:  leave      IL_0148
    IL_004b:  ldarg.0
    IL_004c:  ldc.i4.m1
    IL_004d:  dup
    IL_004e:  stloc.0
    IL_004f:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0054:  ldarg.0
    IL_0055:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_005a:  stloc.1
    IL_005b:  ldarg.0
    IL_005c:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0061:  initobj    "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0067:  ldloca.s   V_1
    IL_0069:  call       "Sub System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()"
    IL_006e:  ldloca.s   V_1
    IL_0070:  initobj    "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0076:  ldarg.0
    IL_0077:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_007c:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_0081:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_0086:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_008b:  stloc.s    V_4
    IL_008d:  ldloca.s   V_4
    IL_008f:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_0094:  brtrue.s   IL_00d7
    IL_0096:  ldarg.0
    IL_0097:  ldc.i4.1
    IL_0098:  dup
    IL_0099:  stloc.0
    IL_009a:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_009f:  ldarg.0
    IL_00a0:  ldloc.s    V_4
    IL_00a2:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A1 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00a7:  ldarg.0
    IL_00a8:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00ad:  ldloca.s   V_4
    IL_00af:  ldarg.0
    IL_00b0:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_2_Call1(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_2_Call1(Of SM$T))"
    IL_00b5:  leave      IL_0148
    IL_00ba:  ldarg.0
    IL_00bb:  ldc.i4.m1
    IL_00bc:  dup
    IL_00bd:  stloc.0
    IL_00be:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_00c3:  ldarg.0
    IL_00c4:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A1 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00c9:  stloc.s    V_4
    IL_00cb:  ldarg.0
    IL_00cc:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$A1 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00d1:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00d7:  ldarg.0
    IL_00d8:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_00dd:  ldloca.s   V_4
    IL_00df:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_00e4:  ldloca.s   V_4
    IL_00e6:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00ec:  dup
    IL_00ed:  stloc.3
    IL_00ee:  ldarg.0
    IL_00ef:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_00f4:  ldloc.3
    IL_00f5:  constrained. "SM$T"
    IL_00fb:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
    IL_0100:  ldc.i4.1
    IL_0101:  add.ovf
    IL_0102:  constrained. "SM$T"
    IL_0108:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
    IL_010d:  leave.s    IL_0133
  }
  catch System.Exception
  {
    IL_010f:  dup
    IL_0110:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_0115:  stloc.s    V_5
    IL_0117:  ldarg.0
    IL_0118:  ldc.i4.s   -2
    IL_011a:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_011f:  ldarg.0
    IL_0120:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_0125:  ldloc.s    V_5
    IL_0127:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_012c:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_0131:  leave.s    IL_0148
  }
  IL_0133:  ldarg.0
  IL_0134:  ldc.i4.s   -2
  IL_0136:  dup
  IL_0137:  stloc.0
  IL_0138:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_013d:  ldarg.0
  IL_013e:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_0143:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_0148:  ret
}
]]>)

            'Wrong IL
            verifier.VerifyIL("Program.VB$StateMachine_3_Call2(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      329 (0x149)
  .maxstack  4
  .locals init (Integer V_0,
                System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_1,
                System.Runtime.CompilerServices.YieldAwaitable V_2,
                Integer V_3,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_4,
                System.Exception V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004b
    IL_000a:  ldloc.0
    IL_000b:  ldc.i4.1
    IL_000c:  beq        IL_00ba
    IL_0011:  call       "Function System.Threading.Tasks.Task.Yield() As System.Runtime.CompilerServices.YieldAwaitable"
    IL_0016:  stloc.2
    IL_0017:  ldloca.s   V_2
    IL_0019:  call       "Function System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter() As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_001e:  stloc.1
    IL_001f:  ldloca.s   V_1
    IL_0021:  call       "Function System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.get_IsCompleted() As Boolean"
    IL_0026:  brtrue.s   IL_0067
    IL_0028:  ldarg.0
    IL_0029:  ldc.i4.0
    IL_002a:  dup
    IL_002b:  stloc.0
    IL_002c:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_0031:  ldarg.0
    IL_0032:  ldloc.1
    IL_0033:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0038:  ldarg.0
    IL_0039:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_003e:  ldloca.s   V_1
    IL_0040:  ldarg.0
    IL_0041:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, Program.VB$StateMachine_3_Call2(Of SM$T))(ByRef System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ByRef Program.VB$StateMachine_3_Call2(Of SM$T))"
    IL_0046:  leave      IL_0148
    IL_004b:  ldarg.0
    IL_004c:  ldc.i4.m1
    IL_004d:  dup
    IL_004e:  stloc.0
    IL_004f:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_0054:  ldarg.0
    IL_0055:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_005a:  stloc.1
    IL_005b:  ldarg.0
    IL_005c:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0061:  initobj    "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0067:  ldloca.s   V_1
    IL_0069:  call       "Sub System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()"
    IL_006e:  ldloca.s   V_1
    IL_0070:  initobj    "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0076:  ldarg.0
    IL_0077:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_007c:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_0081:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_0086:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_008b:  stloc.s    V_4
    IL_008d:  ldloca.s   V_4
    IL_008f:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_0094:  brtrue.s   IL_00d7
    IL_0096:  ldarg.0
    IL_0097:  ldc.i4.1
    IL_0098:  dup
    IL_0099:  stloc.0
    IL_009a:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_009f:  ldarg.0
    IL_00a0:  ldloc.s    V_4
    IL_00a2:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A1 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00a7:  ldarg.0
    IL_00a8:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00ad:  ldloca.s   V_4
    IL_00af:  ldarg.0
    IL_00b0:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_3_Call2(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_3_Call2(Of SM$T))"
    IL_00b5:  leave      IL_0148
    IL_00ba:  ldarg.0
    IL_00bb:  ldc.i4.m1
    IL_00bc:  dup
    IL_00bd:  stloc.0
    IL_00be:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_00c3:  ldarg.0
    IL_00c4:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A1 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00c9:  stloc.s    V_4
    IL_00cb:  ldarg.0
    IL_00cc:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$A1 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00d1:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00d7:  ldarg.0
    IL_00d8:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_00dd:  ldloca.s   V_4
    IL_00df:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_00e4:  ldloca.s   V_4
    IL_00e6:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00ec:  dup
    IL_00ed:  stloc.3
    IL_00ee:  ldarg.0
    IL_00ef:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_00f4:  ldloc.3
    IL_00f5:  constrained. "SM$T"
    IL_00fb:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
    IL_0100:  ldc.i4.1
    IL_0101:  add.ovf
    IL_0102:  constrained. "SM$T"
    IL_0108:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
    IL_010d:  leave.s    IL_0133
  }
  catch System.Exception
  {
    IL_010f:  dup
    IL_0110:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_0115:  stloc.s    V_5
    IL_0117:  ldarg.0
    IL_0118:  ldc.i4.s   -2
    IL_011a:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_011f:  ldarg.0
    IL_0120:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_0125:  ldloc.s    V_5
    IL_0127:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_012c:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_0131:  leave.s    IL_0148
  }
  IL_0133:  ldarg.0
  IL_0134:  ldc.i4.s   -2
  IL_0136:  dup
  IL_0137:  stloc.0
  IL_0138:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
  IL_013d:  ldarg.0
  IL_013e:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_0143:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_0148:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Assignment_Compound_Indexer_Struct_Index_Async_02()
            Dim comp =
<compilation>
    <file>
Imports System
Imports System.Threading.Tasks

Interface IMoveable
    Property Position(x as Integer) As Integer
End Interface

Structure Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x as Integer) As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Structure

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1).Wait()

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2).Wait()
    End Sub

    Private Shared Async Function Call1(Of T As {Structure, IMoveable})(item As T) As Task
        await Task.Yield()
        item.Position(await GetOffsetAsync(GetOffset(item))) += 1
    End Function

    Private Shared Async Function Call2(Of T As {IMoveable})(item As T) As Task
        await Task.Yield()
        item.Position(await GetOffsetAsync(GetOffset(item))) += 1
    End Function

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function

    Shared Function GetOffsetAsync(i As Integer) As Task(Of Integer)
        Return Task.FromResult(i)
    End Function
End Class
    </file>
</compilation>

            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '-1'
Position set for item '-1'
Position get for item '-2'
Position set for item '-2'
").VerifyDiagnostics()

            verifier.VerifyIL("Program.VB$StateMachine_2_Call1(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      329 (0x149)
  .maxstack  4
  .locals init (Integer V_0,
                System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_1,
                System.Runtime.CompilerServices.YieldAwaitable V_2,
                Integer V_3,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_4,
                System.Exception V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004b
    IL_000a:  ldloc.0
    IL_000b:  ldc.i4.1
    IL_000c:  beq        IL_00ba
    IL_0011:  call       "Function System.Threading.Tasks.Task.Yield() As System.Runtime.CompilerServices.YieldAwaitable"
    IL_0016:  stloc.2
    IL_0017:  ldloca.s   V_2
    IL_0019:  call       "Function System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter() As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_001e:  stloc.1
    IL_001f:  ldloca.s   V_1
    IL_0021:  call       "Function System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.get_IsCompleted() As Boolean"
    IL_0026:  brtrue.s   IL_0067
    IL_0028:  ldarg.0
    IL_0029:  ldc.i4.0
    IL_002a:  dup
    IL_002b:  stloc.0
    IL_002c:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0031:  ldarg.0
    IL_0032:  ldloc.1
    IL_0033:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0038:  ldarg.0
    IL_0039:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_003e:  ldloca.s   V_1
    IL_0040:  ldarg.0
    IL_0041:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, Program.VB$StateMachine_2_Call1(Of SM$T))(ByRef System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ByRef Program.VB$StateMachine_2_Call1(Of SM$T))"
    IL_0046:  leave      IL_0148
    IL_004b:  ldarg.0
    IL_004c:  ldc.i4.m1
    IL_004d:  dup
    IL_004e:  stloc.0
    IL_004f:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0054:  ldarg.0
    IL_0055:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_005a:  stloc.1
    IL_005b:  ldarg.0
    IL_005c:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0061:  initobj    "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0067:  ldloca.s   V_1
    IL_0069:  call       "Sub System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()"
    IL_006e:  ldloca.s   V_1
    IL_0070:  initobj    "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0076:  ldarg.0
    IL_0077:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_007c:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_0081:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_0086:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_008b:  stloc.s    V_4
    IL_008d:  ldloca.s   V_4
    IL_008f:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_0094:  brtrue.s   IL_00d7
    IL_0096:  ldarg.0
    IL_0097:  ldc.i4.1
    IL_0098:  dup
    IL_0099:  stloc.0
    IL_009a:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_009f:  ldarg.0
    IL_00a0:  ldloc.s    V_4
    IL_00a2:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A1 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00a7:  ldarg.0
    IL_00a8:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00ad:  ldloca.s   V_4
    IL_00af:  ldarg.0
    IL_00b0:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_2_Call1(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_2_Call1(Of SM$T))"
    IL_00b5:  leave      IL_0148
    IL_00ba:  ldarg.0
    IL_00bb:  ldc.i4.m1
    IL_00bc:  dup
    IL_00bd:  stloc.0
    IL_00be:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_00c3:  ldarg.0
    IL_00c4:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A1 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00c9:  stloc.s    V_4
    IL_00cb:  ldarg.0
    IL_00cc:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$A1 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00d1:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00d7:  ldarg.0
    IL_00d8:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_00dd:  ldloca.s   V_4
    IL_00df:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_00e4:  ldloca.s   V_4
    IL_00e6:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00ec:  dup
    IL_00ed:  stloc.3
    IL_00ee:  ldarg.0
    IL_00ef:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_00f4:  ldloc.3
    IL_00f5:  constrained. "SM$T"
    IL_00fb:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
    IL_0100:  ldc.i4.1
    IL_0101:  add.ovf
    IL_0102:  constrained. "SM$T"
    IL_0108:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
    IL_010d:  leave.s    IL_0133
  }
  catch System.Exception
  {
    IL_010f:  dup
    IL_0110:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_0115:  stloc.s    V_5
    IL_0117:  ldarg.0
    IL_0118:  ldc.i4.s   -2
    IL_011a:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_011f:  ldarg.0
    IL_0120:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_0125:  ldloc.s    V_5
    IL_0127:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_012c:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_0131:  leave.s    IL_0148
  }
  IL_0133:  ldarg.0
  IL_0134:  ldc.i4.s   -2
  IL_0136:  dup
  IL_0137:  stloc.0
  IL_0138:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_013d:  ldarg.0
  IL_013e:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_0143:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_0148:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Assignment_Compound_Indexer_Class_Value()
            Dim comp =
<compilation>
    <file>
Imports System

Interface IMoveable
    Property Position(x As Integer) As Integer
End Interface

Class Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Class

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1)

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2)
    End Sub

    Private Shared Sub Call1(Of T As {Class, IMoveable})(item As T)
        item.Position(1) += GetOffset(item)
    End Sub

    Private Shared Sub Call2(Of T As {IMoveable})(item As T)
        item.Position(1) += GetOffset(item)
    End Sub

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function
End Class
    </file>
</compilation>

            ' Wrong output on some frameworks
            '            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
            '"
            'Position get for item '1'
            'Position set for item '1'
            'Position get for item '2'
            'Position set for item '2'
            '").VerifyDiagnostics()
            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe).VerifyDiagnostics()

            'Wrong IL
            verifier.VerifyIL("Program.Call1(Of T)",
            <![CDATA[
{
  // Code size       37 (0x25)
  .maxstack  4
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldc.i4.1
  IL_0003:  ldarga.s   V_0
  IL_0005:  ldc.i4.1
  IL_0006:  constrained. "T"
  IL_000c:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
  IL_0011:  ldarga.s   V_0
  IL_0013:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0018:  add.ovf
  IL_0019:  constrained. "T"
  IL_001f:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
  IL_0024:  ret
}
]]>)

            'Wrong IL
            verifier.VerifyIL("Program.Call2(Of T)",
            <![CDATA[
{
  // Code size       37 (0x25)
  .maxstack  4
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldc.i4.1
  IL_0003:  ldarga.s   V_0
  IL_0005:  ldc.i4.1
  IL_0006:  constrained. "T"
  IL_000c:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
  IL_0011:  ldarga.s   V_0
  IL_0013:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0018:  add.ovf
  IL_0019:  constrained. "T"
  IL_001f:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
  IL_0024:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Assignment_Compound_Indexer_Struct_Value()
            Dim comp =
<compilation>
    <file>
Imports System

Interface IMoveable
    Property Position(x As Integer) As Integer
End Interface

Structure Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Structure

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1)

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2)
    End Sub

    Private Shared Sub Call1(Of T As {Structure, IMoveable})(item As T)
        item.Position(1) += GetOffset(item)
    End Sub

    Private Shared Sub Call2(Of T As {IMoveable})(item As T)
        item.Position(1) += GetOffset(item)
    End Sub

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function
End Class
    </file>
</compilation>

            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '1'
Position set for item '-1'
Position get for item '2'
Position set for item '-2'
").VerifyDiagnostics()

            verifier.VerifyIL("Program.Call1(Of T)",
            <![CDATA[
{
  // Code size       37 (0x25)
  .maxstack  4
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldc.i4.1
  IL_0003:  ldarga.s   V_0
  IL_0005:  ldc.i4.1
  IL_0006:  constrained. "T"
  IL_000c:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
  IL_0011:  ldarga.s   V_0
  IL_0013:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0018:  add.ovf
  IL_0019:  constrained. "T"
  IL_001f:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
  IL_0024:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Assignment_Compound_Indexer_Class_Value_Ref()
            Dim comp =
<compilation>
    <file>
Imports System

Interface IMoveable
    Property Position(x As Integer) As Integer
End Interface

Class Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Class

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1)

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2)
    End Sub

    Private Shared Sub Call1(Of T As {Class, IMoveable})(ByRef item As T)
        item.Position(1) += GetOffset(item)
    End Sub

    Private Shared Sub Call2(Of T As {IMoveable})(ByRef item As T)
        item.Position(1) += GetOffset(item)
    End Sub

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function
End Class
    </file>
</compilation>

            'Wrong output on some frameworks
            '            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
            '"
            'Position get for item '1'
            'Position set for item '1'
            'Position get for item '2'
            'Position set for item '2'
            '").VerifyDiagnostics()
            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe).VerifyDiagnostics()

            'Wrong IL
            verifier.VerifyIL("Program.Call1(Of T)",
            <![CDATA[
{
  // Code size       34 (0x22)
  .maxstack  4
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  ldarg.0
  IL_0003:  ldc.i4.1
  IL_0004:  constrained. "T"
  IL_000a:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
  IL_000f:  ldarg.0
  IL_0010:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0015:  add.ovf
  IL_0016:  constrained. "T"
  IL_001c:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
  IL_0021:  ret
}
]]>)

            'Wrong IL
            verifier.VerifyIL("Program.Call2(Of T)",
            <![CDATA[
{
  // Code size       34 (0x22)
  .maxstack  4
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  ldarg.0
  IL_0003:  ldc.i4.1
  IL_0004:  constrained. "T"
  IL_000a:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
  IL_000f:  ldarg.0
  IL_0010:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0015:  add.ovf
  IL_0016:  constrained. "T"
  IL_001c:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
  IL_0021:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Assignment_Compound_Indexer_Struct_Value_Ref()
            Dim comp =
<compilation>
    <file>
Imports System

Interface IMoveable
    Property Position(x As Integer) As Integer
End Interface

Structure Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Structure

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1)

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2)
    End Sub

    Private Shared Sub Call1(Of T As {Structure, IMoveable})(ByRef item As T)
        item.Position(1) += GetOffset(item)
    End Sub

    Private Shared Sub Call2(Of T As {IMoveable})(ByRef item As T)
        item.Position(1) += GetOffset(item)
    End Sub

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function
End Class
    </file>
</compilation>

            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '1'
Position set for item '-1'
Position get for item '2'
Position set for item '-2'
").VerifyDiagnostics()

            verifier.VerifyIL("Program.Call1(Of T)",
            <![CDATA[
{
  // Code size       34 (0x22)
  .maxstack  4
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  ldarg.0
  IL_0003:  ldc.i4.1
  IL_0004:  constrained. "T"
  IL_000a:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
  IL_000f:  ldarg.0
  IL_0010:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0015:  add.ovf
  IL_0016:  constrained. "T"
  IL_001c:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
  IL_0021:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Assignment_Compound_Indexer_Class_Value_Async_01()
            Dim comp =
<compilation>
    <file>
Imports System
Imports System.Threading.Tasks

Interface IMoveable
    Property Position(x As Integer) As Integer
End Interface

Class Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Class

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1).Wait()

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2).Wait()
    End Sub

    Private Shared Async Function Call1(Of T As {Class, IMoveable})(item As T) As Task
        item.Position(1) += await GetOffsetAsync(GetOffset(item))
    End Function

    Private Shared Async Function Call2(Of T As {IMoveable})(item As T) As Task
        item.Position(1) += await GetOffsetAsync(GetOffset(item))
    End Function

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function

    Shared Function GetOffsetAsync(i As Integer) As Task(Of Integer)
        Return Task.FromResult(i)
    End Function
End Class
    </file>
</compilation>

            ' Wrong output
            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '1'
Position set for item '-1'
Position get for item '2'
Position set for item '-2'
").VerifyDiagnostics()

            'Wrong IL
            verifier.VerifyIL("Program.VB$StateMachine_2_Call1(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      223 (0xdf)
  .maxstack  5
  .locals init (Integer V_0,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_1,
                System.Exception V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0061
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0011:  ldc.i4.1
    IL_0012:  constrained. "SM$T"
    IL_0018:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
    IL_001d:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As Integer"
    IL_0022:  ldarg.0
    IL_0023:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0028:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_002d:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_0032:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0037:  stloc.1
    IL_0038:  ldloca.s   V_1
    IL_003a:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_003f:  brtrue.s   IL_007d
    IL_0041:  ldarg.0
    IL_0042:  ldc.i4.0
    IL_0043:  dup
    IL_0044:  stloc.0
    IL_0045:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_004a:  ldarg.0
    IL_004b:  ldloc.1
    IL_004c:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0051:  ldarg.0
    IL_0052:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_0057:  ldloca.s   V_1
    IL_0059:  ldarg.0
    IL_005a:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_2_Call1(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_2_Call1(Of SM$T))"
    IL_005f:  leave.s    IL_00de
    IL_0061:  ldarg.0
    IL_0062:  ldc.i4.m1
    IL_0063:  dup
    IL_0064:  stloc.0
    IL_0065:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_006a:  ldarg.0
    IL_006b:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0070:  stloc.1
    IL_0071:  ldarg.0
    IL_0072:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0077:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_007d:  ldarg.0
    IL_007e:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0083:  ldc.i4.1
    IL_0084:  ldarg.0
    IL_0085:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As Integer"
    IL_008a:  ldloca.s   V_1
    IL_008c:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_0091:  ldloca.s   V_1
    IL_0093:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0099:  add.ovf
    IL_009a:  constrained. "SM$T"
    IL_00a0:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
    IL_00a5:  leave.s    IL_00c9
  }
  catch System.Exception
  {
    IL_00a7:  dup
    IL_00a8:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_00ad:  stloc.2
    IL_00ae:  ldarg.0
    IL_00af:  ldc.i4.s   -2
    IL_00b1:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_00b6:  ldarg.0
    IL_00b7:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00bc:  ldloc.2
    IL_00bd:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_00c2:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_00c7:  leave.s    IL_00de
  }
  IL_00c9:  ldarg.0
  IL_00ca:  ldc.i4.s   -2
  IL_00cc:  dup
  IL_00cd:  stloc.0
  IL_00ce:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_00d3:  ldarg.0
  IL_00d4:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_00d9:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_00de:  ret
}
]]>)

            'Wrong IL
            verifier.VerifyIL("Program.VB$StateMachine_3_Call2(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      223 (0xdf)
  .maxstack  5
  .locals init (Integer V_0,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_1,
                System.Exception V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0061
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_0011:  ldc.i4.1
    IL_0012:  constrained. "SM$T"
    IL_0018:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
    IL_001d:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$U1 As Integer"
    IL_0022:  ldarg.0
    IL_0023:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_0028:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_002d:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_0032:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0037:  stloc.1
    IL_0038:  ldloca.s   V_1
    IL_003a:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_003f:  brtrue.s   IL_007d
    IL_0041:  ldarg.0
    IL_0042:  ldc.i4.0
    IL_0043:  dup
    IL_0044:  stloc.0
    IL_0045:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_004a:  ldarg.0
    IL_004b:  ldloc.1
    IL_004c:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0051:  ldarg.0
    IL_0052:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_0057:  ldloca.s   V_1
    IL_0059:  ldarg.0
    IL_005a:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_3_Call2(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_3_Call2(Of SM$T))"
    IL_005f:  leave.s    IL_00de
    IL_0061:  ldarg.0
    IL_0062:  ldc.i4.m1
    IL_0063:  dup
    IL_0064:  stloc.0
    IL_0065:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_006a:  ldarg.0
    IL_006b:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0070:  stloc.1
    IL_0071:  ldarg.0
    IL_0072:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0077:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_007d:  ldarg.0
    IL_007e:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_0083:  ldc.i4.1
    IL_0084:  ldarg.0
    IL_0085:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$U1 As Integer"
    IL_008a:  ldloca.s   V_1
    IL_008c:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_0091:  ldloca.s   V_1
    IL_0093:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0099:  add.ovf
    IL_009a:  constrained. "SM$T"
    IL_00a0:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
    IL_00a5:  leave.s    IL_00c9
  }
  catch System.Exception
  {
    IL_00a7:  dup
    IL_00a8:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_00ad:  stloc.2
    IL_00ae:  ldarg.0
    IL_00af:  ldc.i4.s   -2
    IL_00b1:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_00b6:  ldarg.0
    IL_00b7:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00bc:  ldloc.2
    IL_00bd:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_00c2:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_00c7:  leave.s    IL_00de
  }
  IL_00c9:  ldarg.0
  IL_00ca:  ldc.i4.s   -2
  IL_00cc:  dup
  IL_00cd:  stloc.0
  IL_00ce:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
  IL_00d3:  ldarg.0
  IL_00d4:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_00d9:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_00de:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Assignment_Compound_Indexer_Struct_Value_Async_01()
            Dim comp =
<compilation>
    <file>
Imports System
Imports System.Threading.Tasks

Interface IMoveable
    Property Position(x As Integer) As Integer
End Interface

Structure Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Structure

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1).Wait()

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2).Wait()
    End Sub

    Private Shared Async Function Call1(Of T As {Structure, IMoveable})(item As T) As Task
        item.Position(1) += await GetOffsetAsync(GetOffset(item))
    End Function

    Private Shared Async Function Call2(Of T As {IMoveable})(item As T) As Task
        item.Position(1) += await GetOffsetAsync(GetOffset(item))
    End Function

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function

    Shared Function GetOffsetAsync(i As Integer) As Task(Of Integer)
        Return Task.FromResult(i)
    End Function
End Class
    </file>
</compilation>

            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '1'
Position set for item '-1'
Position get for item '2'
Position set for item '-2'
").VerifyDiagnostics()

            verifier.VerifyIL("Program.VB$StateMachine_2_Call1(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      223 (0xdf)
  .maxstack  5
  .locals init (Integer V_0,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_1,
                System.Exception V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0061
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0011:  ldc.i4.1
    IL_0012:  constrained. "SM$T"
    IL_0018:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
    IL_001d:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As Integer"
    IL_0022:  ldarg.0
    IL_0023:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0028:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_002d:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_0032:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0037:  stloc.1
    IL_0038:  ldloca.s   V_1
    IL_003a:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_003f:  brtrue.s   IL_007d
    IL_0041:  ldarg.0
    IL_0042:  ldc.i4.0
    IL_0043:  dup
    IL_0044:  stloc.0
    IL_0045:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_004a:  ldarg.0
    IL_004b:  ldloc.1
    IL_004c:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0051:  ldarg.0
    IL_0052:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_0057:  ldloca.s   V_1
    IL_0059:  ldarg.0
    IL_005a:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_2_Call1(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_2_Call1(Of SM$T))"
    IL_005f:  leave.s    IL_00de
    IL_0061:  ldarg.0
    IL_0062:  ldc.i4.m1
    IL_0063:  dup
    IL_0064:  stloc.0
    IL_0065:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_006a:  ldarg.0
    IL_006b:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0070:  stloc.1
    IL_0071:  ldarg.0
    IL_0072:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0077:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_007d:  ldarg.0
    IL_007e:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0083:  ldc.i4.1
    IL_0084:  ldarg.0
    IL_0085:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As Integer"
    IL_008a:  ldloca.s   V_1
    IL_008c:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_0091:  ldloca.s   V_1
    IL_0093:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0099:  add.ovf
    IL_009a:  constrained. "SM$T"
    IL_00a0:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
    IL_00a5:  leave.s    IL_00c9
  }
  catch System.Exception
  {
    IL_00a7:  dup
    IL_00a8:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_00ad:  stloc.2
    IL_00ae:  ldarg.0
    IL_00af:  ldc.i4.s   -2
    IL_00b1:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_00b6:  ldarg.0
    IL_00b7:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00bc:  ldloc.2
    IL_00bd:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_00c2:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_00c7:  leave.s    IL_00de
  }
  IL_00c9:  ldarg.0
  IL_00ca:  ldc.i4.s   -2
  IL_00cc:  dup
  IL_00cd:  stloc.0
  IL_00ce:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_00d3:  ldarg.0
  IL_00d4:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_00d9:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_00de:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Assignment_Compound_Indexer_Class_IndexAndValue()
            Dim comp =
<compilation>
    <file>
Imports System

Interface IMoveable
    Property Position(x As Integer) As Integer
End Interface

Class Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Class

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1)

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2)
    End Sub

    Private Shared Sub Call1(Of T As {Class, IMoveable})(item As T)
        item.Position(GetOffset(item)) += GetOffset(item)
    End Sub

    Private Shared Sub Call2(Of T As {IMoveable})(item As T)
        item.Position(GetOffset(item)) += GetOffset(item)
    End Sub

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function
End Class
    </file>
</compilation>

            ' Wrong output and differs, but still wrong, on some frameworks 
            '            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
            '"
            'Position get for item '-1'
            'Position set for item '-1'
            'Position get for item '-3'
            'Position set for item '-3'
            '").VerifyDiagnostics()
            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe).VerifyDiagnostics()

            'Wrong IL
            verifier.VerifyIL("Program.Call1(Of T)",
            <![CDATA[
{
  // Code size       45 (0x2d)
  .maxstack  4
  .locals init (Integer V_0)
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldarga.s   V_0
  IL_0004:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0009:  dup
  IL_000a:  stloc.0
  IL_000b:  ldarga.s   V_0
  IL_000d:  ldloc.0
  IL_000e:  constrained. "T"
  IL_0014:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
  IL_0019:  ldarga.s   V_0
  IL_001b:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0020:  add.ovf
  IL_0021:  constrained. "T"
  IL_0027:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
  IL_002c:  ret
}
]]>)

            'Wrong IL
            verifier.VerifyIL("Program.Call2(Of T)",
            <![CDATA[
{
  // Code size       45 (0x2d)
  .maxstack  4
  .locals init (Integer V_0)
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldarga.s   V_0
  IL_0004:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0009:  dup
  IL_000a:  stloc.0
  IL_000b:  ldarga.s   V_0
  IL_000d:  ldloc.0
  IL_000e:  constrained. "T"
  IL_0014:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
  IL_0019:  ldarga.s   V_0
  IL_001b:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0020:  add.ovf
  IL_0021:  constrained. "T"
  IL_0027:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
  IL_002c:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Assignment_Compound_Indexer_Struct_IndexAndValue()
            Dim comp =
<compilation>
    <file>
Imports System

Interface IMoveable
    Property Position(x As Integer) As Integer
End Interface

Structure Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Structure

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1)

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2)
    End Sub

    Private Shared Sub Call1(Of T As {Structure, IMoveable})(item As T)
        item.Position(GetOffset(item)) += GetOffset(item)
    End Sub

    Private Shared Sub Call2(Of T As {IMoveable})(item As T)
        item.Position(GetOffset(item)) += GetOffset(item)
    End Sub

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function
End Class
    </file>
</compilation>

            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '-1'
Position set for item '-2'
Position get for item '-3'
Position set for item '-4'
").VerifyDiagnostics()

            verifier.VerifyIL("Program.Call1(Of T)",
            <![CDATA[
{
  // Code size       45 (0x2d)
  .maxstack  4
  .locals init (Integer V_0)
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldarga.s   V_0
  IL_0004:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0009:  dup
  IL_000a:  stloc.0
  IL_000b:  ldarga.s   V_0
  IL_000d:  ldloc.0
  IL_000e:  constrained. "T"
  IL_0014:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
  IL_0019:  ldarga.s   V_0
  IL_001b:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0020:  add.ovf
  IL_0021:  constrained. "T"
  IL_0027:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
  IL_002c:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Assignment_Compound_Indexer_Class_IndexAndValue_Ref()
            Dim comp =
<compilation>
    <file>
Imports System

Interface IMoveable
    Property Position(x As Integer) As Integer
End Interface

Class Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Class

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1)

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2)
    End Sub

    Private Shared Sub Call1(Of T As {Class, IMoveable})(ByRef item As T)
        item.Position(GetOffset(item)) += GetOffset(item)
    End Sub

    Private Shared Sub Call2(Of T As {IMoveable})(ByRef item As T)
        item.Position(GetOffset(item)) += GetOffset(item)
    End Sub

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function
End Class
    </file>
</compilation>

            ' Wrong output and framework dependent
            '            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
            '"
            'Position get for item '-1'
            'Position set for item '-1'
            'Position get for item '-3'
            'Position set for item '-3'
            '").VerifyDiagnostics()
            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe).VerifyDiagnostics()

            'Wrong IL
            verifier.VerifyIL("Program.Call1(Of T)",
            <![CDATA[
{
  // Code size       41 (0x29)
  .maxstack  4
  .locals init (Integer V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0007:  dup
  IL_0008:  stloc.0
  IL_0009:  ldarg.0
  IL_000a:  ldloc.0
  IL_000b:  constrained. "T"
  IL_0011:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
  IL_0016:  ldarg.0
  IL_0017:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_001c:  add.ovf
  IL_001d:  constrained. "T"
  IL_0023:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
  IL_0028:  ret
}
]]>)

            'Wrong IL
            verifier.VerifyIL("Program.Call2(Of T)",
            <![CDATA[
{
  // Code size       41 (0x29)
  .maxstack  4
  .locals init (Integer V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0007:  dup
  IL_0008:  stloc.0
  IL_0009:  ldarg.0
  IL_000a:  ldloc.0
  IL_000b:  constrained. "T"
  IL_0011:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
  IL_0016:  ldarg.0
  IL_0017:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_001c:  add.ovf
  IL_001d:  constrained. "T"
  IL_0023:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
  IL_0028:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Assignment_Compound_Indexer_Struct_IndexAndValue_Ref()
            Dim comp =
<compilation>
    <file>
Imports System

Interface IMoveable
    Property Position(x As Integer) As Integer
End Interface

Structure Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Structure

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1)

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2)
    End Sub

    Private Shared Sub Call1(Of T As {Structure, IMoveable})(ByRef item As T)
        item.Position(GetOffset(item)) += GetOffset(item)
    End Sub

    Private Shared Sub Call2(Of T As {IMoveable})(ByRef item As T)
        item.Position(GetOffset(item)) += GetOffset(item)
    End Sub

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function
End Class
    </file>
</compilation>

            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '-1'
Position set for item '-2'
Position get for item '-3'
Position set for item '-4'
").VerifyDiagnostics()

            verifier.VerifyIL("Program.Call1(Of T)",
            <![CDATA[
{
  // Code size       41 (0x29)
  .maxstack  4
  .locals init (Integer V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0007:  dup
  IL_0008:  stloc.0
  IL_0009:  ldarg.0
  IL_000a:  ldloc.0
  IL_000b:  constrained. "T"
  IL_0011:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
  IL_0016:  ldarg.0
  IL_0017:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_001c:  add.ovf
  IL_001d:  constrained. "T"
  IL_0023:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
  IL_0028:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Assignment_Compound_Indexer_Class_IndexAndValue_Async_01()
            Dim comp =
<compilation>
    <file>
Imports System
Imports System.Threading.Tasks

Interface IMoveable
    Property Position(x As Integer) As Integer
End Interface

Class Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Class

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1).Wait()

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2).Wait()
    End Sub

    Private Shared Async Function Call1(Of T As {Class, IMoveable})(item As T) As Task
        item.Position(GetOffset(item)) += await GetOffsetAsync(GetOffset(item))
    End Function

    Private Shared Async Function Call2(Of T As {IMoveable})(item As T) As Task
        item.Position(GetOffset(item)) += await GetOffsetAsync(GetOffset(item))
    End Function

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function

    Shared Function GetOffsetAsync(i As Integer) As Task(Of Integer)
        Return Task.FromResult(i)
    End Function
End Class
    </file>
</compilation>

            ' Wrong output
            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '-1'
Position set for item '-2'
Position get for item '-3'
Position set for item '-4'
").VerifyDiagnostics()

            'Wrong IL
            verifier.VerifyIL("Program.VB$StateMachine_2_Call1(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      262 (0x106)
  .maxstack  5
  .locals init (Integer V_0,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_1,
                Integer V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0083
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldarg.0
    IL_000d:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0012:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_0017:  dup
    IL_0018:  stloc.2
    IL_0019:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$S0 As Integer"
    IL_001e:  ldloc.2
    IL_001f:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U2 As Integer"
    IL_0024:  ldarg.0
    IL_0025:  ldarg.0
    IL_0026:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_002b:  ldarg.0
    IL_002c:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$S0 As Integer"
    IL_0031:  constrained. "SM$T"
    IL_0037:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
    IL_003c:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As Integer"
    IL_0041:  ldarg.0
    IL_0042:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0047:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_004c:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_0051:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0056:  stloc.1
    IL_0057:  ldloca.s   V_1
    IL_0059:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_005e:  brtrue.s   IL_009f
    IL_0060:  ldarg.0
    IL_0061:  ldc.i4.0
    IL_0062:  dup
    IL_0063:  stloc.0
    IL_0064:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0069:  ldarg.0
    IL_006a:  ldloc.1
    IL_006b:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0070:  ldarg.0
    IL_0071:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_0076:  ldloca.s   V_1
    IL_0078:  ldarg.0
    IL_0079:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_2_Call1(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_2_Call1(Of SM$T))"
    IL_007e:  leave      IL_0105
    IL_0083:  ldarg.0
    IL_0084:  ldc.i4.m1
    IL_0085:  dup
    IL_0086:  stloc.0
    IL_0087:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_008c:  ldarg.0
    IL_008d:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0092:  stloc.1
    IL_0093:  ldarg.0
    IL_0094:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0099:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_009f:  ldarg.0
    IL_00a0:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_00a5:  ldarg.0
    IL_00a6:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U2 As Integer"
    IL_00ab:  ldarg.0
    IL_00ac:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As Integer"
    IL_00b1:  ldloca.s   V_1
    IL_00b3:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_00b8:  ldloca.s   V_1
    IL_00ba:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00c0:  add.ovf
    IL_00c1:  constrained. "SM$T"
    IL_00c7:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
    IL_00cc:  leave.s    IL_00f0
  }
  catch System.Exception
  {
    IL_00ce:  dup
    IL_00cf:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_00d4:  stloc.3
    IL_00d5:  ldarg.0
    IL_00d6:  ldc.i4.s   -2
    IL_00d8:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_00dd:  ldarg.0
    IL_00de:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00e3:  ldloc.3
    IL_00e4:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_00e9:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_00ee:  leave.s    IL_0105
  }
  IL_00f0:  ldarg.0
  IL_00f1:  ldc.i4.s   -2
  IL_00f3:  dup
  IL_00f4:  stloc.0
  IL_00f5:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_00fa:  ldarg.0
  IL_00fb:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_0100:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_0105:  ret
}
]]>)

            'Wrong IL
            verifier.VerifyIL("Program.VB$StateMachine_3_Call2(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      262 (0x106)
  .maxstack  5
  .locals init (Integer V_0,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_1,
                Integer V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0083
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldarg.0
    IL_000d:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_0012:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_0017:  dup
    IL_0018:  stloc.2
    IL_0019:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$S0 As Integer"
    IL_001e:  ldloc.2
    IL_001f:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$U2 As Integer"
    IL_0024:  ldarg.0
    IL_0025:  ldarg.0
    IL_0026:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_002b:  ldarg.0
    IL_002c:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$S0 As Integer"
    IL_0031:  constrained. "SM$T"
    IL_0037:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
    IL_003c:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$U1 As Integer"
    IL_0041:  ldarg.0
    IL_0042:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_0047:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_004c:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_0051:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0056:  stloc.1
    IL_0057:  ldloca.s   V_1
    IL_0059:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_005e:  brtrue.s   IL_009f
    IL_0060:  ldarg.0
    IL_0061:  ldc.i4.0
    IL_0062:  dup
    IL_0063:  stloc.0
    IL_0064:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_0069:  ldarg.0
    IL_006a:  ldloc.1
    IL_006b:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0070:  ldarg.0
    IL_0071:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_0076:  ldloca.s   V_1
    IL_0078:  ldarg.0
    IL_0079:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_3_Call2(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_3_Call2(Of SM$T))"
    IL_007e:  leave      IL_0105
    IL_0083:  ldarg.0
    IL_0084:  ldc.i4.m1
    IL_0085:  dup
    IL_0086:  stloc.0
    IL_0087:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_008c:  ldarg.0
    IL_008d:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0092:  stloc.1
    IL_0093:  ldarg.0
    IL_0094:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0099:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_009f:  ldarg.0
    IL_00a0:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_00a5:  ldarg.0
    IL_00a6:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$U2 As Integer"
    IL_00ab:  ldarg.0
    IL_00ac:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$U1 As Integer"
    IL_00b1:  ldloca.s   V_1
    IL_00b3:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_00b8:  ldloca.s   V_1
    IL_00ba:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00c0:  add.ovf
    IL_00c1:  constrained. "SM$T"
    IL_00c7:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
    IL_00cc:  leave.s    IL_00f0
  }
  catch System.Exception
  {
    IL_00ce:  dup
    IL_00cf:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_00d4:  stloc.3
    IL_00d5:  ldarg.0
    IL_00d6:  ldc.i4.s   -2
    IL_00d8:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_00dd:  ldarg.0
    IL_00de:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00e3:  ldloc.3
    IL_00e4:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_00e9:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_00ee:  leave.s    IL_0105
  }
  IL_00f0:  ldarg.0
  IL_00f1:  ldc.i4.s   -2
  IL_00f3:  dup
  IL_00f4:  stloc.0
  IL_00f5:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
  IL_00fa:  ldarg.0
  IL_00fb:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_0100:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_0105:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Assignment_Compound_Indexer_Struct_IndexAndValue_Async_01()
            Dim comp =
<compilation>
    <file>
Imports System
Imports System.Threading.Tasks

Interface IMoveable
    Property Position(x As Integer) As Integer
End Interface

Structure Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Structure

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1).Wait()

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2).Wait()
    End Sub

    Private Shared Async Function Call1(Of T As {Structure, IMoveable})(item As T) As Task
        item.Position(GetOffset(item)) += await GetOffsetAsync(GetOffset(item))
    End Function

    Private Shared Async Function Call2(Of T As {IMoveable})(item As T) As Task
        item.Position(GetOffset(item)) += await GetOffsetAsync(GetOffset(item))
    End Function

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function

    Shared Function GetOffsetAsync(i As Integer) As Task(Of Integer)
        Return Task.FromResult(i)
    End Function
End Class
    </file>
</compilation>

            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '-1'
Position set for item '-2'
Position get for item '-3'
Position set for item '-4'
").VerifyDiagnostics()

            verifier.VerifyIL("Program.VB$StateMachine_2_Call1(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      262 (0x106)
  .maxstack  5
  .locals init (Integer V_0,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_1,
                Integer V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0083
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldarg.0
    IL_000d:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0012:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_0017:  dup
    IL_0018:  stloc.2
    IL_0019:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$S0 As Integer"
    IL_001e:  ldloc.2
    IL_001f:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U2 As Integer"
    IL_0024:  ldarg.0
    IL_0025:  ldarg.0
    IL_0026:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_002b:  ldarg.0
    IL_002c:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$S0 As Integer"
    IL_0031:  constrained. "SM$T"
    IL_0037:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
    IL_003c:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As Integer"
    IL_0041:  ldarg.0
    IL_0042:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0047:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_004c:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_0051:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0056:  stloc.1
    IL_0057:  ldloca.s   V_1
    IL_0059:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_005e:  brtrue.s   IL_009f
    IL_0060:  ldarg.0
    IL_0061:  ldc.i4.0
    IL_0062:  dup
    IL_0063:  stloc.0
    IL_0064:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0069:  ldarg.0
    IL_006a:  ldloc.1
    IL_006b:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0070:  ldarg.0
    IL_0071:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_0076:  ldloca.s   V_1
    IL_0078:  ldarg.0
    IL_0079:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_2_Call1(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_2_Call1(Of SM$T))"
    IL_007e:  leave      IL_0105
    IL_0083:  ldarg.0
    IL_0084:  ldc.i4.m1
    IL_0085:  dup
    IL_0086:  stloc.0
    IL_0087:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_008c:  ldarg.0
    IL_008d:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0092:  stloc.1
    IL_0093:  ldarg.0
    IL_0094:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0099:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_009f:  ldarg.0
    IL_00a0:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_00a5:  ldarg.0
    IL_00a6:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U2 As Integer"
    IL_00ab:  ldarg.0
    IL_00ac:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As Integer"
    IL_00b1:  ldloca.s   V_1
    IL_00b3:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_00b8:  ldloca.s   V_1
    IL_00ba:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00c0:  add.ovf
    IL_00c1:  constrained. "SM$T"
    IL_00c7:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
    IL_00cc:  leave.s    IL_00f0
  }
  catch System.Exception
  {
    IL_00ce:  dup
    IL_00cf:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_00d4:  stloc.3
    IL_00d5:  ldarg.0
    IL_00d6:  ldc.i4.s   -2
    IL_00d8:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_00dd:  ldarg.0
    IL_00de:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00e3:  ldloc.3
    IL_00e4:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_00e9:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_00ee:  leave.s    IL_0105
  }
  IL_00f0:  ldarg.0
  IL_00f1:  ldc.i4.s   -2
  IL_00f3:  dup
  IL_00f4:  stloc.0
  IL_00f5:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_00fa:  ldarg.0
  IL_00fb:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_0100:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_0105:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Assignment_Compound_Indexer_Class_IndexAndValue_Async_03()
            Dim comp =
<compilation>
    <file>
Imports System
Imports System.Threading.Tasks

Interface IMoveable
    Property Position(x As Integer) As Integer
End Interface

Class Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Class

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1).Wait()

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2).Wait()
    End Sub

    Private Shared Async Function Call1(Of T As {Class, IMoveable})(item As T) As Task
        item.Position(await GetOffsetAsync(GetOffset(item))) += GetOffset(item)
    End Function

    Private Shared Async Function Call2(Of T As {IMoveable})(item As T) As Task
        item.Position(await GetOffsetAsync(GetOffset(item))) += GetOffset(item)
    End Function

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function

    Shared Function GetOffsetAsync(i As Integer) As Task(Of Integer)
        Return Task.FromResult(i)
    End Function
End Class
    </file>
</compilation>

            ' Wrong output and framework dependent 
            '            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
            '"
            'Position get for item '-1'
            'Position set for item '-1'
            'Position get for item '-3'
            'Position set for item '-3'
            '").VerifyDiagnostics()
            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe).VerifyDiagnostics()

            'Wrong IL
            verifier.VerifyIL("Program.VB$StateMachine_2_Call1(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      226 (0xe2)
  .maxstack  4
  .locals init (Integer V_0,
                Integer V_1,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004c
    IL_000a:  ldarg.0
    IL_000b:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0010:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_0015:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_001a:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_001f:  stloc.2
    IL_0020:  ldloca.s   V_2
    IL_0022:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_0027:  brtrue.s   IL_0068
    IL_0029:  ldarg.0
    IL_002a:  ldc.i4.0
    IL_002b:  dup
    IL_002c:  stloc.0
    IL_002d:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0032:  ldarg.0
    IL_0033:  ldloc.2
    IL_0034:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0039:  ldarg.0
    IL_003a:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_003f:  ldloca.s   V_2
    IL_0041:  ldarg.0
    IL_0042:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_2_Call1(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_2_Call1(Of SM$T))"
    IL_0047:  leave      IL_00e1
    IL_004c:  ldarg.0
    IL_004d:  ldc.i4.m1
    IL_004e:  dup
    IL_004f:  stloc.0
    IL_0050:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0055:  ldarg.0
    IL_0056:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_005b:  stloc.2
    IL_005c:  ldarg.0
    IL_005d:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0062:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0068:  ldarg.0
    IL_0069:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_006e:  ldloca.s   V_2
    IL_0070:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_0075:  ldloca.s   V_2
    IL_0077:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_007d:  dup
    IL_007e:  stloc.1
    IL_007f:  ldarg.0
    IL_0080:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0085:  ldloc.1
    IL_0086:  constrained. "SM$T"
    IL_008c:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
    IL_0091:  ldarg.0
    IL_0092:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0097:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_009c:  add.ovf
    IL_009d:  constrained. "SM$T"
    IL_00a3:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
    IL_00a8:  leave.s    IL_00cc
  }
  catch System.Exception
  {
    IL_00aa:  dup
    IL_00ab:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_00b0:  stloc.3
    IL_00b1:  ldarg.0
    IL_00b2:  ldc.i4.s   -2
    IL_00b4:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_00b9:  ldarg.0
    IL_00ba:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00bf:  ldloc.3
    IL_00c0:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_00c5:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_00ca:  leave.s    IL_00e1
  }
  IL_00cc:  ldarg.0
  IL_00cd:  ldc.i4.s   -2
  IL_00cf:  dup
  IL_00d0:  stloc.0
  IL_00d1:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_00d6:  ldarg.0
  IL_00d7:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_00dc:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_00e1:  ret
}
]]>)

            'Wrong IL
            verifier.VerifyIL("Program.VB$StateMachine_3_Call2(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      226 (0xe2)
  .maxstack  4
  .locals init (Integer V_0,
                Integer V_1,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004c
    IL_000a:  ldarg.0
    IL_000b:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_0010:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_0015:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_001a:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_001f:  stloc.2
    IL_0020:  ldloca.s   V_2
    IL_0022:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_0027:  brtrue.s   IL_0068
    IL_0029:  ldarg.0
    IL_002a:  ldc.i4.0
    IL_002b:  dup
    IL_002c:  stloc.0
    IL_002d:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_0032:  ldarg.0
    IL_0033:  ldloc.2
    IL_0034:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0039:  ldarg.0
    IL_003a:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_003f:  ldloca.s   V_2
    IL_0041:  ldarg.0
    IL_0042:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_3_Call2(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_3_Call2(Of SM$T))"
    IL_0047:  leave      IL_00e1
    IL_004c:  ldarg.0
    IL_004d:  ldc.i4.m1
    IL_004e:  dup
    IL_004f:  stloc.0
    IL_0050:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_0055:  ldarg.0
    IL_0056:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_005b:  stloc.2
    IL_005c:  ldarg.0
    IL_005d:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0062:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0068:  ldarg.0
    IL_0069:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_006e:  ldloca.s   V_2
    IL_0070:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_0075:  ldloca.s   V_2
    IL_0077:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_007d:  dup
    IL_007e:  stloc.1
    IL_007f:  ldarg.0
    IL_0080:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_0085:  ldloc.1
    IL_0086:  constrained. "SM$T"
    IL_008c:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
    IL_0091:  ldarg.0
    IL_0092:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_0097:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_009c:  add.ovf
    IL_009d:  constrained. "SM$T"
    IL_00a3:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
    IL_00a8:  leave.s    IL_00cc
  }
  catch System.Exception
  {
    IL_00aa:  dup
    IL_00ab:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_00b0:  stloc.3
    IL_00b1:  ldarg.0
    IL_00b2:  ldc.i4.s   -2
    IL_00b4:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_00b9:  ldarg.0
    IL_00ba:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00bf:  ldloc.3
    IL_00c0:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_00c5:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_00ca:  leave.s    IL_00e1
  }
  IL_00cc:  ldarg.0
  IL_00cd:  ldc.i4.s   -2
  IL_00cf:  dup
  IL_00d0:  stloc.0
  IL_00d1:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
  IL_00d6:  ldarg.0
  IL_00d7:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_00dc:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_00e1:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Assignment_Compound_Indexer_Struct_IndexAndValue_Async_03()
            Dim comp =
<compilation>
    <file>
Imports System
Imports System.Threading.Tasks

Interface IMoveable
    Property Position(x As Integer) As Integer
End Interface

Structure Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Structure

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1).Wait()

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2).Wait()
    End Sub

    Private Shared Async Function Call1(Of T As {Structure, IMoveable})(item As T) As Task
        item.Position(await GetOffsetAsync(GetOffset(item))) += GetOffset(item)
    End Function

    Private Shared Async Function Call2(Of T As {IMoveable})(item As T) As Task
        item.Position(await GetOffsetAsync(GetOffset(item))) += GetOffset(item)
    End Function

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function

    Shared Function GetOffsetAsync(i As Integer) As Task(Of Integer)
        Return Task.FromResult(i)
    End Function
End Class
    </file>
</compilation>

            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '-1'
Position set for item '-2'
Position get for item '-3'
Position set for item '-4'
").VerifyDiagnostics()

            verifier.VerifyIL("Program.VB$StateMachine_2_Call1(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      226 (0xe2)
  .maxstack  4
  .locals init (Integer V_0,
                Integer V_1,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004c
    IL_000a:  ldarg.0
    IL_000b:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0010:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_0015:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_001a:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_001f:  stloc.2
    IL_0020:  ldloca.s   V_2
    IL_0022:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_0027:  brtrue.s   IL_0068
    IL_0029:  ldarg.0
    IL_002a:  ldc.i4.0
    IL_002b:  dup
    IL_002c:  stloc.0
    IL_002d:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0032:  ldarg.0
    IL_0033:  ldloc.2
    IL_0034:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0039:  ldarg.0
    IL_003a:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_003f:  ldloca.s   V_2
    IL_0041:  ldarg.0
    IL_0042:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_2_Call1(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_2_Call1(Of SM$T))"
    IL_0047:  leave      IL_00e1
    IL_004c:  ldarg.0
    IL_004d:  ldc.i4.m1
    IL_004e:  dup
    IL_004f:  stloc.0
    IL_0050:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0055:  ldarg.0
    IL_0056:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_005b:  stloc.2
    IL_005c:  ldarg.0
    IL_005d:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0062:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0068:  ldarg.0
    IL_0069:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_006e:  ldloca.s   V_2
    IL_0070:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_0075:  ldloca.s   V_2
    IL_0077:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_007d:  dup
    IL_007e:  stloc.1
    IL_007f:  ldarg.0
    IL_0080:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0085:  ldloc.1
    IL_0086:  constrained. "SM$T"
    IL_008c:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
    IL_0091:  ldarg.0
    IL_0092:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0097:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_009c:  add.ovf
    IL_009d:  constrained. "SM$T"
    IL_00a3:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
    IL_00a8:  leave.s    IL_00cc
  }
  catch System.Exception
  {
    IL_00aa:  dup
    IL_00ab:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_00b0:  stloc.3
    IL_00b1:  ldarg.0
    IL_00b2:  ldc.i4.s   -2
    IL_00b4:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_00b9:  ldarg.0
    IL_00ba:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00bf:  ldloc.3
    IL_00c0:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_00c5:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_00ca:  leave.s    IL_00e1
  }
  IL_00cc:  ldarg.0
  IL_00cd:  ldc.i4.s   -2
  IL_00cf:  dup
  IL_00d0:  stloc.0
  IL_00d1:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_00d6:  ldarg.0
  IL_00d7:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_00dc:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_00e1:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Assignment_Compound_Indexer_Class_IndexAndValue_Async_04()
            Dim comp =
<compilation>
    <file>
Imports System
Imports System.Threading.Tasks

Interface IMoveable
    Property Position(x As Integer) As Integer
End Interface

Class Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Class

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1).Wait()

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2).Wait()
    End Sub

    Private Shared Async Function Call1(Of T As {Class, IMoveable})(item As T) As Task
        item.Position(await GetOffsetAsync(GetOffset(item))) += await GetOffsetAsync(GetOffset(item))
    End Function

    Private Shared Async Function Call2(Of T As {IMoveable})(item As T) As Task
        item.Position(await GetOffsetAsync(GetOffset(item))) += await GetOffsetAsync(GetOffset(item))
    End Function

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function

    Shared Function GetOffsetAsync(i As Integer) As Task(Of Integer)
        Return Task.FromResult(i)
    End Function
End Class
    </file>
</compilation>

            ' Wrong output
            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '-1'
Position set for item '-2'
Position get for item '-3'
Position set for item '-4'
").VerifyDiagnostics()

            'Wrong IL
            verifier.VerifyIL("Program.VB$StateMachine_2_Call1(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      369 (0x171)
  .maxstack  5
  .locals init (Integer V_0,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_1,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_2,
                Integer V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0053
    IL_000a:  ldloc.0
    IL_000b:  ldc.i4.1
    IL_000c:  beq        IL_00ec
    IL_0011:  ldarg.0
    IL_0012:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0017:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_001c:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_0021:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0026:  stloc.1
    IL_0027:  ldloca.s   V_1
    IL_0029:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_002e:  brtrue.s   IL_006f
    IL_0030:  ldarg.0
    IL_0031:  ldc.i4.0
    IL_0032:  dup
    IL_0033:  stloc.0
    IL_0034:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0039:  ldarg.0
    IL_003a:  ldloc.1
    IL_003b:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0040:  ldarg.0
    IL_0041:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_0046:  ldloca.s   V_1
    IL_0048:  ldarg.0
    IL_0049:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_2_Call1(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_2_Call1(Of SM$T))"
    IL_004e:  leave      IL_0170
    IL_0053:  ldarg.0
    IL_0054:  ldc.i4.m1
    IL_0055:  dup
    IL_0056:  stloc.0
    IL_0057:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_005c:  ldarg.0
    IL_005d:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0062:  stloc.1
    IL_0063:  ldarg.0
    IL_0064:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0069:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_006f:  ldarg.0
    IL_0070:  ldarg.0
    IL_0071:  ldloca.s   V_1
    IL_0073:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_0078:  ldloca.s   V_1
    IL_007a:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0080:  dup
    IL_0081:  stloc.3
    IL_0082:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$S0 As Integer"
    IL_0087:  ldloc.3
    IL_0088:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U2 As Integer"
    IL_008d:  ldarg.0
    IL_008e:  ldarg.0
    IL_008f:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0094:  ldarg.0
    IL_0095:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$S0 As Integer"
    IL_009a:  constrained. "SM$T"
    IL_00a0:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
    IL_00a5:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As Integer"
    IL_00aa:  ldarg.0
    IL_00ab:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_00b0:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_00b5:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_00ba:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00bf:  stloc.2
    IL_00c0:  ldloca.s   V_2
    IL_00c2:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_00c7:  brtrue.s   IL_0108
    IL_00c9:  ldarg.0
    IL_00ca:  ldc.i4.1
    IL_00cb:  dup
    IL_00cc:  stloc.0
    IL_00cd:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_00d2:  ldarg.0
    IL_00d3:  ldloc.2
    IL_00d4:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00d9:  ldarg.0
    IL_00da:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00df:  ldloca.s   V_2
    IL_00e1:  ldarg.0
    IL_00e2:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_2_Call1(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_2_Call1(Of SM$T))"
    IL_00e7:  leave      IL_0170
    IL_00ec:  ldarg.0
    IL_00ed:  ldc.i4.m1
    IL_00ee:  dup
    IL_00ef:  stloc.0
    IL_00f0:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_00f5:  ldarg.0
    IL_00f6:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00fb:  stloc.2
    IL_00fc:  ldarg.0
    IL_00fd:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0102:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0108:  ldarg.0
    IL_0109:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_010e:  ldarg.0
    IL_010f:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U2 As Integer"
    IL_0114:  ldarg.0
    IL_0115:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As Integer"
    IL_011a:  ldloca.s   V_2
    IL_011c:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_0121:  ldloca.s   V_2
    IL_0123:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0129:  add.ovf
    IL_012a:  constrained. "SM$T"
    IL_0130:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
    IL_0135:  leave.s    IL_015b
  }
  catch System.Exception
  {
    IL_0137:  dup
    IL_0138:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_013d:  stloc.s    V_4
    IL_013f:  ldarg.0
    IL_0140:  ldc.i4.s   -2
    IL_0142:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0147:  ldarg.0
    IL_0148:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_014d:  ldloc.s    V_4
    IL_014f:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_0154:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_0159:  leave.s    IL_0170
  }
  IL_015b:  ldarg.0
  IL_015c:  ldc.i4.s   -2
  IL_015e:  dup
  IL_015f:  stloc.0
  IL_0160:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_0165:  ldarg.0
  IL_0166:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_016b:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_0170:  ret
}
]]>)

            'Wrong IL
            verifier.VerifyIL("Program.VB$StateMachine_3_Call2(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      369 (0x171)
  .maxstack  5
  .locals init (Integer V_0,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_1,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_2,
                Integer V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0053
    IL_000a:  ldloc.0
    IL_000b:  ldc.i4.1
    IL_000c:  beq        IL_00ec
    IL_0011:  ldarg.0
    IL_0012:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_0017:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_001c:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_0021:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0026:  stloc.1
    IL_0027:  ldloca.s   V_1
    IL_0029:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_002e:  brtrue.s   IL_006f
    IL_0030:  ldarg.0
    IL_0031:  ldc.i4.0
    IL_0032:  dup
    IL_0033:  stloc.0
    IL_0034:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_0039:  ldarg.0
    IL_003a:  ldloc.1
    IL_003b:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0040:  ldarg.0
    IL_0041:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_0046:  ldloca.s   V_1
    IL_0048:  ldarg.0
    IL_0049:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_3_Call2(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_3_Call2(Of SM$T))"
    IL_004e:  leave      IL_0170
    IL_0053:  ldarg.0
    IL_0054:  ldc.i4.m1
    IL_0055:  dup
    IL_0056:  stloc.0
    IL_0057:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_005c:  ldarg.0
    IL_005d:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0062:  stloc.1
    IL_0063:  ldarg.0
    IL_0064:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0069:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_006f:  ldarg.0
    IL_0070:  ldarg.0
    IL_0071:  ldloca.s   V_1
    IL_0073:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_0078:  ldloca.s   V_1
    IL_007a:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0080:  dup
    IL_0081:  stloc.3
    IL_0082:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$S0 As Integer"
    IL_0087:  ldloc.3
    IL_0088:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$U2 As Integer"
    IL_008d:  ldarg.0
    IL_008e:  ldarg.0
    IL_008f:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_0094:  ldarg.0
    IL_0095:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$S0 As Integer"
    IL_009a:  constrained. "SM$T"
    IL_00a0:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
    IL_00a5:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$U1 As Integer"
    IL_00aa:  ldarg.0
    IL_00ab:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_00b0:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_00b5:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_00ba:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00bf:  stloc.2
    IL_00c0:  ldloca.s   V_2
    IL_00c2:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_00c7:  brtrue.s   IL_0108
    IL_00c9:  ldarg.0
    IL_00ca:  ldc.i4.1
    IL_00cb:  dup
    IL_00cc:  stloc.0
    IL_00cd:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_00d2:  ldarg.0
    IL_00d3:  ldloc.2
    IL_00d4:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00d9:  ldarg.0
    IL_00da:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00df:  ldloca.s   V_2
    IL_00e1:  ldarg.0
    IL_00e2:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_3_Call2(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_3_Call2(Of SM$T))"
    IL_00e7:  leave      IL_0170
    IL_00ec:  ldarg.0
    IL_00ed:  ldc.i4.m1
    IL_00ee:  dup
    IL_00ef:  stloc.0
    IL_00f0:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_00f5:  ldarg.0
    IL_00f6:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00fb:  stloc.2
    IL_00fc:  ldarg.0
    IL_00fd:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0102:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0108:  ldarg.0
    IL_0109:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_010e:  ldarg.0
    IL_010f:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$U2 As Integer"
    IL_0114:  ldarg.0
    IL_0115:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$U1 As Integer"
    IL_011a:  ldloca.s   V_2
    IL_011c:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_0121:  ldloca.s   V_2
    IL_0123:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0129:  add.ovf
    IL_012a:  constrained. "SM$T"
    IL_0130:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
    IL_0135:  leave.s    IL_015b
  }
  catch System.Exception
  {
    IL_0137:  dup
    IL_0138:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_013d:  stloc.s    V_4
    IL_013f:  ldarg.0
    IL_0140:  ldc.i4.s   -2
    IL_0142:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_0147:  ldarg.0
    IL_0148:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_014d:  ldloc.s    V_4
    IL_014f:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_0154:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_0159:  leave.s    IL_0170
  }
  IL_015b:  ldarg.0
  IL_015c:  ldc.i4.s   -2
  IL_015e:  dup
  IL_015f:  stloc.0
  IL_0160:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
  IL_0165:  ldarg.0
  IL_0166:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_016b:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_0170:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Assignment_Compound_Indexer_Struct_IndexAndValue_Async_04()
            Dim comp =
<compilation>
    <file>
Imports System
Imports System.Threading.Tasks

Interface IMoveable
    Property Position(x As Integer) As Integer
End Interface

Structure Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Structure

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1).Wait()

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2).Wait()
    End Sub

    Private Shared Async Function Call1(Of T As {Structure, IMoveable})(item As T) As Task
        item.Position(await GetOffsetAsync(GetOffset(item))) += await GetOffsetAsync(GetOffset(item))
    End Function

    Private Shared Async Function Call2(Of T As {IMoveable})(item As T) As Task
        item.Position(await GetOffsetAsync(GetOffset(item))) += await GetOffsetAsync(GetOffset(item))
    End Function

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function

    Shared Function GetOffsetAsync(i As Integer) As Task(Of Integer)
        Return Task.FromResult(i)
    End Function
End Class
    </file>
</compilation>

            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '-1'
Position set for item '-2'
Position get for item '-3'
Position set for item '-4'
").VerifyDiagnostics()

            verifier.VerifyIL("Program.VB$StateMachine_2_Call1(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      369 (0x171)
  .maxstack  5
  .locals init (Integer V_0,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_1,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_2,
                Integer V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0053
    IL_000a:  ldloc.0
    IL_000b:  ldc.i4.1
    IL_000c:  beq        IL_00ec
    IL_0011:  ldarg.0
    IL_0012:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0017:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_001c:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_0021:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0026:  stloc.1
    IL_0027:  ldloca.s   V_1
    IL_0029:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_002e:  brtrue.s   IL_006f
    IL_0030:  ldarg.0
    IL_0031:  ldc.i4.0
    IL_0032:  dup
    IL_0033:  stloc.0
    IL_0034:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0039:  ldarg.0
    IL_003a:  ldloc.1
    IL_003b:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0040:  ldarg.0
    IL_0041:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_0046:  ldloca.s   V_1
    IL_0048:  ldarg.0
    IL_0049:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_2_Call1(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_2_Call1(Of SM$T))"
    IL_004e:  leave      IL_0170
    IL_0053:  ldarg.0
    IL_0054:  ldc.i4.m1
    IL_0055:  dup
    IL_0056:  stloc.0
    IL_0057:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_005c:  ldarg.0
    IL_005d:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0062:  stloc.1
    IL_0063:  ldarg.0
    IL_0064:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0069:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_006f:  ldarg.0
    IL_0070:  ldarg.0
    IL_0071:  ldloca.s   V_1
    IL_0073:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_0078:  ldloca.s   V_1
    IL_007a:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0080:  dup
    IL_0081:  stloc.3
    IL_0082:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$S0 As Integer"
    IL_0087:  ldloc.3
    IL_0088:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U2 As Integer"
    IL_008d:  ldarg.0
    IL_008e:  ldarg.0
    IL_008f:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0094:  ldarg.0
    IL_0095:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$S0 As Integer"
    IL_009a:  constrained. "SM$T"
    IL_00a0:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
    IL_00a5:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As Integer"
    IL_00aa:  ldarg.0
    IL_00ab:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_00b0:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_00b5:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_00ba:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00bf:  stloc.2
    IL_00c0:  ldloca.s   V_2
    IL_00c2:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_00c7:  brtrue.s   IL_0108
    IL_00c9:  ldarg.0
    IL_00ca:  ldc.i4.1
    IL_00cb:  dup
    IL_00cc:  stloc.0
    IL_00cd:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_00d2:  ldarg.0
    IL_00d3:  ldloc.2
    IL_00d4:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00d9:  ldarg.0
    IL_00da:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00df:  ldloca.s   V_2
    IL_00e1:  ldarg.0
    IL_00e2:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_2_Call1(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_2_Call1(Of SM$T))"
    IL_00e7:  leave      IL_0170
    IL_00ec:  ldarg.0
    IL_00ed:  ldc.i4.m1
    IL_00ee:  dup
    IL_00ef:  stloc.0
    IL_00f0:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_00f5:  ldarg.0
    IL_00f6:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00fb:  stloc.2
    IL_00fc:  ldarg.0
    IL_00fd:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0102:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0108:  ldarg.0
    IL_0109:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_010e:  ldarg.0
    IL_010f:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U2 As Integer"
    IL_0114:  ldarg.0
    IL_0115:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As Integer"
    IL_011a:  ldloca.s   V_2
    IL_011c:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_0121:  ldloca.s   V_2
    IL_0123:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0129:  add.ovf
    IL_012a:  constrained. "SM$T"
    IL_0130:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
    IL_0135:  leave.s    IL_015b
  }
  catch System.Exception
  {
    IL_0137:  dup
    IL_0138:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_013d:  stloc.s    V_4
    IL_013f:  ldarg.0
    IL_0140:  ldc.i4.s   -2
    IL_0142:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0147:  ldarg.0
    IL_0148:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_014d:  ldloc.s    V_4
    IL_014f:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_0154:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_0159:  leave.s    IL_0170
  }
  IL_015b:  ldarg.0
  IL_015c:  ldc.i4.s   -2
  IL_015e:  dup
  IL_015f:  stloc.0
  IL_0160:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_0165:  ldarg.0
  IL_0166:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_016b:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_0170:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Assignment_Compound_Indexer_Class_Index_InWith()
            Dim comp =
<compilation>
    <file>
Imports System

Interface IMoveable
    Property Position(x As Integer) As Integer
End Interface

Class Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Class

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1)

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2)
    End Sub

    Private Shared Sub Call1(Of T As {Class, IMoveable})(item As T)
        With item
            .Position(GetOffset(item)) += 1
        End With
    End Sub

    Private Shared Sub Call2(Of T As {IMoveable})(item As T)
        With item
            .Position(GetOffset(item)) += 1
        End With
    End Sub

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function
End Class
    </file>
</compilation>

            ' Wrong output
            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '-1'
Position set for item '-1'
Position get for item '-2'
Position set for item '-2'
").VerifyDiagnostics()

            'Wrong IL
            verifier.VerifyIL("Program.Call1(Of T)",
            <![CDATA[
{
  // Code size       39 (0x27)
  .maxstack  4
  .locals init (Integer V_0)
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldarga.s   V_0
  IL_0004:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0009:  dup
  IL_000a:  stloc.0
  IL_000b:  ldarga.s   V_0
  IL_000d:  ldloc.0
  IL_000e:  constrained. "T"
  IL_0014:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
  IL_0019:  ldc.i4.1
  IL_001a:  add.ovf
  IL_001b:  constrained. "T"
  IL_0021:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
  IL_0026:  ret
}
]]>)

            'Wrong IL
            verifier.VerifyIL("Program.Call2(Of T)",
            <![CDATA[
{
  // Code size       39 (0x27)
  .maxstack  4
  .locals init (Integer V_0)
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldarga.s   V_0
  IL_0004:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0009:  dup
  IL_000a:  stloc.0
  IL_000b:  ldarga.s   V_0
  IL_000d:  ldloc.0
  IL_000e:  constrained. "T"
  IL_0014:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
  IL_0019:  ldc.i4.1
  IL_001a:  add.ovf
  IL_001b:  constrained. "T"
  IL_0021:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
  IL_0026:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Assignment_Compound_Indexer_Struct_Index_InWith()
            Dim comp =
<compilation>
    <file>
Imports System

Interface IMoveable
    Property Position(x As Integer) As Integer
End Interface

Structure Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Structure

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1)

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2)
    End Sub

    Private Shared Sub Call1(Of T As {Structure, IMoveable})(item As T)
        With item
            .Position(GetOffset(item)) += 1
        End With
    End Sub

    Private Shared Sub Call2(Of T As {IMoveable})(item As T)
        With item
            .Position(GetOffset(item)) += 1
        End With
    End Sub

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function
End Class
    </file>
</compilation>

            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '-1'
Position set for item '-1'
Position get for item '-2'
Position set for item '-2'
").VerifyDiagnostics()

            verifier.VerifyIL("Program.Call1(Of T)",
            <![CDATA[
{
  // Code size       39 (0x27)
  .maxstack  4
  .locals init (Integer V_0)
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldarga.s   V_0
  IL_0004:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0009:  dup
  IL_000a:  stloc.0
  IL_000b:  ldarga.s   V_0
  IL_000d:  ldloc.0
  IL_000e:  constrained. "T"
  IL_0014:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
  IL_0019:  ldc.i4.1
  IL_001a:  add.ovf
  IL_001b:  constrained. "T"
  IL_0021:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
  IL_0026:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Assignment_Compound_Indexer_Class_Index_Ref_InWith()
            Dim comp =
<compilation>
    <file>
Imports System

Interface IMoveable
    Property Position(x As Integer) As Integer
End Interface

Class Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Class

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1)

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2)
    End Sub

    Private Shared Sub Call1(Of T As {Class, IMoveable})(ByRef item As T)
        With item
            .Position(GetOffset(item)) += 1
        End With
    End Sub

    Private Shared Sub Call2(Of T As {IMoveable})(ByRef item As T)
        With item
            .Position(GetOffset(item)) += 1
        End With
    End Sub

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function
End Class
    </file>
</compilation>

            ' Wrong output
            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '-1'
Position set for item '-1'
Position get for item '-2'
Position set for item '-2'
").VerifyDiagnostics()

            'Wrong IL
            verifier.VerifyIL("Program.Call1(Of T)",
            <![CDATA[
{
  // Code size       36 (0x24)
  .maxstack  4
  .locals init (Integer V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0007:  dup
  IL_0008:  stloc.0
  IL_0009:  ldarg.0
  IL_000a:  ldloc.0
  IL_000b:  constrained. "T"
  IL_0011:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
  IL_0016:  ldc.i4.1
  IL_0017:  add.ovf
  IL_0018:  constrained. "T"
  IL_001e:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
  IL_0023:  ret
}
]]>)

            'Wrong IL
            verifier.VerifyIL("Program.Call2(Of T)",
            <![CDATA[
{
  // Code size       36 (0x24)
  .maxstack  4
  .locals init (Integer V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0007:  dup
  IL_0008:  stloc.0
  IL_0009:  ldarg.0
  IL_000a:  ldloc.0
  IL_000b:  constrained. "T"
  IL_0011:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
  IL_0016:  ldc.i4.1
  IL_0017:  add.ovf
  IL_0018:  constrained. "T"
  IL_001e:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
  IL_0023:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Assignment_Compound_Indexer_Struct_Index_Ref_InWith()
            Dim comp =
<compilation>
    <file>
Imports System

Interface IMoveable
    Property Position(x As Integer) As Integer
End Interface

Structure Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Structure

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1)

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2)
    End Sub

    Private Shared Sub Call1(Of T As {Structure, IMoveable})(ByRef item As T)
        With item
            .Position(GetOffset(item)) += 1
        End With
    End Sub

    Private Shared Sub Call2(Of T As {IMoveable})(ByRef item As T)
        With item
            .Position(GetOffset(item)) += 1
        End With
    End Sub

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function
End Class
    </file>
</compilation>

            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '-1'
Position set for item '-1'
Position get for item '-2'
Position set for item '-2'
").VerifyDiagnostics()

            verifier.VerifyIL("Program.Call1(Of T)",
            <![CDATA[
{
  // Code size       36 (0x24)
  .maxstack  4
  .locals init (Integer V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0007:  dup
  IL_0008:  stloc.0
  IL_0009:  ldarg.0
  IL_000a:  ldloc.0
  IL_000b:  constrained. "T"
  IL_0011:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
  IL_0016:  ldc.i4.1
  IL_0017:  add.ovf
  IL_0018:  constrained. "T"
  IL_001e:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
  IL_0023:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Assignment_Compound_Indexer_Class_Index_Async_01_InWith()
            Dim comp =
<compilation>
    <file>
Imports System
Imports System.Threading.Tasks

Interface IMoveable
    Property Position(x As Integer) As Integer
End Interface

Class Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Class

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1).Wait()

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2).Wait()
    End Sub

    Private Shared Async Function Call1(Of T As {Class, IMoveable})(item As T) As Task
        With item
            .Position(await GetOffsetAsync(GetOffset(item))) += 1
        End With
    End Function

    Private Shared Async Function Call2(Of T As {IMoveable})(item As T) As Task
        With item
            .Position(await GetOffsetAsync(GetOffset(item))) += 1
        End With
    End Function

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function

    Shared Function GetOffsetAsync(i As Integer) As Task(Of Integer)
        Return Task.FromResult(i)
    End Function
End Class
    </file>
</compilation>

            ' Wrong output
            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '-1'
Position set for item '-1'
Position get for item '-2'
Position set for item '-2'
").VerifyDiagnostics()

            'Wrong IL
            verifier.VerifyIL("Program.VB$StateMachine_2_Call1(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      216 (0xd8)
  .maxstack  4
  .locals init (Integer V_0,
                Integer V_1,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004c
    IL_000a:  ldarg.0
    IL_000b:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0010:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_0015:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_001a:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_001f:  stloc.2
    IL_0020:  ldloca.s   V_2
    IL_0022:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_0027:  brtrue.s   IL_0068
    IL_0029:  ldarg.0
    IL_002a:  ldc.i4.0
    IL_002b:  dup
    IL_002c:  stloc.0
    IL_002d:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0032:  ldarg.0
    IL_0033:  ldloc.2
    IL_0034:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0039:  ldarg.0
    IL_003a:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_003f:  ldloca.s   V_2
    IL_0041:  ldarg.0
    IL_0042:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_2_Call1(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_2_Call1(Of SM$T))"
    IL_0047:  leave      IL_00d7
    IL_004c:  ldarg.0
    IL_004d:  ldc.i4.m1
    IL_004e:  dup
    IL_004f:  stloc.0
    IL_0050:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0055:  ldarg.0
    IL_0056:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_005b:  stloc.2
    IL_005c:  ldarg.0
    IL_005d:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0062:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0068:  ldarg.0
    IL_0069:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_006e:  ldloca.s   V_2
    IL_0070:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_0075:  ldloca.s   V_2
    IL_0077:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_007d:  dup
    IL_007e:  stloc.1
    IL_007f:  ldarg.0
    IL_0080:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0085:  ldloc.1
    IL_0086:  constrained. "SM$T"
    IL_008c:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
    IL_0091:  ldc.i4.1
    IL_0092:  add.ovf
    IL_0093:  constrained. "SM$T"
    IL_0099:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
    IL_009e:  leave.s    IL_00c2
  }
  catch System.Exception
  {
    IL_00a0:  dup
    IL_00a1:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_00a6:  stloc.3
    IL_00a7:  ldarg.0
    IL_00a8:  ldc.i4.s   -2
    IL_00aa:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_00af:  ldarg.0
    IL_00b0:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00b5:  ldloc.3
    IL_00b6:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_00bb:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_00c0:  leave.s    IL_00d7
  }
  IL_00c2:  ldarg.0
  IL_00c3:  ldc.i4.s   -2
  IL_00c5:  dup
  IL_00c6:  stloc.0
  IL_00c7:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_00cc:  ldarg.0
  IL_00cd:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_00d2:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_00d7:  ret
}
]]>)

            'Wrong IL
            verifier.VerifyIL("Program.VB$StateMachine_3_Call2(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      216 (0xd8)
  .maxstack  4
  .locals init (Integer V_0,
                Integer V_1,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004c
    IL_000a:  ldarg.0
    IL_000b:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_0010:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_0015:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_001a:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_001f:  stloc.2
    IL_0020:  ldloca.s   V_2
    IL_0022:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_0027:  brtrue.s   IL_0068
    IL_0029:  ldarg.0
    IL_002a:  ldc.i4.0
    IL_002b:  dup
    IL_002c:  stloc.0
    IL_002d:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_0032:  ldarg.0
    IL_0033:  ldloc.2
    IL_0034:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0039:  ldarg.0
    IL_003a:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_003f:  ldloca.s   V_2
    IL_0041:  ldarg.0
    IL_0042:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_3_Call2(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_3_Call2(Of SM$T))"
    IL_0047:  leave      IL_00d7
    IL_004c:  ldarg.0
    IL_004d:  ldc.i4.m1
    IL_004e:  dup
    IL_004f:  stloc.0
    IL_0050:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_0055:  ldarg.0
    IL_0056:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_005b:  stloc.2
    IL_005c:  ldarg.0
    IL_005d:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0062:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0068:  ldarg.0
    IL_0069:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_006e:  ldloca.s   V_2
    IL_0070:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_0075:  ldloca.s   V_2
    IL_0077:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_007d:  dup
    IL_007e:  stloc.1
    IL_007f:  ldarg.0
    IL_0080:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_0085:  ldloc.1
    IL_0086:  constrained. "SM$T"
    IL_008c:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
    IL_0091:  ldc.i4.1
    IL_0092:  add.ovf
    IL_0093:  constrained. "SM$T"
    IL_0099:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
    IL_009e:  leave.s    IL_00c2
  }
  catch System.Exception
  {
    IL_00a0:  dup
    IL_00a1:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_00a6:  stloc.3
    IL_00a7:  ldarg.0
    IL_00a8:  ldc.i4.s   -2
    IL_00aa:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_00af:  ldarg.0
    IL_00b0:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00b5:  ldloc.3
    IL_00b6:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_00bb:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_00c0:  leave.s    IL_00d7
  }
  IL_00c2:  ldarg.0
  IL_00c3:  ldc.i4.s   -2
  IL_00c5:  dup
  IL_00c6:  stloc.0
  IL_00c7:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
  IL_00cc:  ldarg.0
  IL_00cd:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_00d2:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_00d7:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Assignment_Compound_Indexer_Struct_Index_Async_01_InWith()
            Dim comp =
<compilation>
    <file>
Imports System
Imports System.Threading.Tasks

Interface IMoveable
    Property Position(x As Integer) As Integer
End Interface

Structure Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Structure

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1).Wait()

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2).Wait()
    End Sub

    Private Shared Async Function Call1(Of T As {Structure, IMoveable})(item As T) As Task
        With item
            .Position(await GetOffsetAsync(GetOffset(item))) += 1
        End With
    End Function

    Private Shared Async Function Call2(Of T As {IMoveable})(item As T) As Task
        With item
            .Position(await GetOffsetAsync(GetOffset(item))) += 1
        End With
    End Function

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function

    Shared Function GetOffsetAsync(i As Integer) As Task(Of Integer)
        Return Task.FromResult(i)
    End Function
End Class
    </file>
</compilation>

            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '-1'
Position set for item '-1'
Position get for item '-2'
Position set for item '-2'
").VerifyDiagnostics()

            verifier.VerifyIL("Program.VB$StateMachine_2_Call1(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      216 (0xd8)
  .maxstack  4
  .locals init (Integer V_0,
                Integer V_1,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004c
    IL_000a:  ldarg.0
    IL_000b:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0010:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_0015:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_001a:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_001f:  stloc.2
    IL_0020:  ldloca.s   V_2
    IL_0022:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_0027:  brtrue.s   IL_0068
    IL_0029:  ldarg.0
    IL_002a:  ldc.i4.0
    IL_002b:  dup
    IL_002c:  stloc.0
    IL_002d:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0032:  ldarg.0
    IL_0033:  ldloc.2
    IL_0034:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0039:  ldarg.0
    IL_003a:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_003f:  ldloca.s   V_2
    IL_0041:  ldarg.0
    IL_0042:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_2_Call1(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_2_Call1(Of SM$T))"
    IL_0047:  leave      IL_00d7
    IL_004c:  ldarg.0
    IL_004d:  ldc.i4.m1
    IL_004e:  dup
    IL_004f:  stloc.0
    IL_0050:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0055:  ldarg.0
    IL_0056:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_005b:  stloc.2
    IL_005c:  ldarg.0
    IL_005d:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0062:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0068:  ldarg.0
    IL_0069:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_006e:  ldloca.s   V_2
    IL_0070:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_0075:  ldloca.s   V_2
    IL_0077:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_007d:  dup
    IL_007e:  stloc.1
    IL_007f:  ldarg.0
    IL_0080:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0085:  ldloc.1
    IL_0086:  constrained. "SM$T"
    IL_008c:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
    IL_0091:  ldc.i4.1
    IL_0092:  add.ovf
    IL_0093:  constrained. "SM$T"
    IL_0099:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
    IL_009e:  leave.s    IL_00c2
  }
  catch System.Exception
  {
    IL_00a0:  dup
    IL_00a1:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_00a6:  stloc.3
    IL_00a7:  ldarg.0
    IL_00a8:  ldc.i4.s   -2
    IL_00aa:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_00af:  ldarg.0
    IL_00b0:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00b5:  ldloc.3
    IL_00b6:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_00bb:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_00c0:  leave.s    IL_00d7
  }
  IL_00c2:  ldarg.0
  IL_00c3:  ldc.i4.s   -2
  IL_00c5:  dup
  IL_00c6:  stloc.0
  IL_00c7:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_00cc:  ldarg.0
  IL_00cd:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_00d2:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_00d7:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Assignment_Compound_Indexer_Class_Value_InWith()
            Dim comp =
<compilation>
    <file>
Imports System

Interface IMoveable
    Property Position(x As Integer) As Integer
End Interface

Class Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Class

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1)

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2)
    End Sub

    Private Shared Sub Call1(Of T As {Class, IMoveable})(item As T)
        With item
            .Position(1) += GetOffset(item)
        End With
    End Sub

    Private Shared Sub Call2(Of T As {IMoveable})(item As T)
        With item
            .Position(1) += GetOffset(item)
        End With
    End Sub

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function
End Class
    </file>
</compilation>

            ' Wrong output on some frameworks
            '            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
            '"
            'Position get for item '1'
            'Position set for item '1'
            'Position get for item '2'
            'Position set for item '2'
            '").VerifyDiagnostics()
            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe).VerifyDiagnostics()

            'Wrong IL
            verifier.VerifyIL("Program.Call1(Of T)",
            <![CDATA[
{
  // Code size       37 (0x25)
  .maxstack  4
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldc.i4.1
  IL_0003:  ldarga.s   V_0
  IL_0005:  ldc.i4.1
  IL_0006:  constrained. "T"
  IL_000c:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
  IL_0011:  ldarga.s   V_0
  IL_0013:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0018:  add.ovf
  IL_0019:  constrained. "T"
  IL_001f:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
  IL_0024:  ret
}
]]>)

            'Wrong IL
            verifier.VerifyIL("Program.Call2(Of T)",
            <![CDATA[
{
  // Code size       37 (0x25)
  .maxstack  4
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldc.i4.1
  IL_0003:  ldarga.s   V_0
  IL_0005:  ldc.i4.1
  IL_0006:  constrained. "T"
  IL_000c:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
  IL_0011:  ldarga.s   V_0
  IL_0013:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0018:  add.ovf
  IL_0019:  constrained. "T"
  IL_001f:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
  IL_0024:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Assignment_Compound_Indexer_Struct_Value_InWith()
            Dim comp =
<compilation>
    <file>
Imports System

Interface IMoveable
    Property Position(x As Integer) As Integer
End Interface

Structure Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Structure

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1)

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2)
    End Sub

    Private Shared Sub Call1(Of T As {Structure, IMoveable})(item As T)
        With item
            .Position(1) += GetOffset(item)
        End With
    End Sub

    Private Shared Sub Call2(Of T As {IMoveable})(item As T)
        With item
            .Position(1) += GetOffset(item)
        End With
    End Sub

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function
End Class
    </file>
</compilation>

            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '1'
Position set for item '-1'
Position get for item '2'
Position set for item '-2'
").VerifyDiagnostics()

            verifier.VerifyIL("Program.Call1(Of T)",
            <![CDATA[
{
  // Code size       37 (0x25)
  .maxstack  4
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldc.i4.1
  IL_0003:  ldarga.s   V_0
  IL_0005:  ldc.i4.1
  IL_0006:  constrained. "T"
  IL_000c:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
  IL_0011:  ldarga.s   V_0
  IL_0013:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0018:  add.ovf
  IL_0019:  constrained. "T"
  IL_001f:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
  IL_0024:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Assignment_Compound_Indexer_Class_Value_Ref_InWith()
            Dim comp =
<compilation>
    <file>
Imports System

Interface IMoveable
    Property Position(x As Integer) As Integer
End Interface

Class Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Class

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1)

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2)
    End Sub

    Private Shared Sub Call1(Of T As {Class, IMoveable})(ByRef item As T)
        With item
            .Position(1) += GetOffset(item)
        End With
    End Sub

    Private Shared Sub Call2(Of T As {IMoveable})(ByRef item As T)
        With item
            .Position(1) += GetOffset(item)
        End With
    End Sub

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function
End Class
    </file>
</compilation>

            'Wrong output on some frameworks
            '            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
            '"
            'Position get for item '1'
            'Position set for item '1'
            'Position get for item '2'
            'Position set for item '2'
            '").VerifyDiagnostics()
            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe).VerifyDiagnostics()

            'Wrong IL
            verifier.VerifyIL("Program.Call1(Of T)",
            <![CDATA[
{
  // Code size       34 (0x22)
  .maxstack  4
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  ldarg.0
  IL_0003:  ldc.i4.1
  IL_0004:  constrained. "T"
  IL_000a:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
  IL_000f:  ldarg.0
  IL_0010:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0015:  add.ovf
  IL_0016:  constrained. "T"
  IL_001c:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
  IL_0021:  ret
}
]]>)

            'Wrong IL
            verifier.VerifyIL("Program.Call2(Of T)",
            <![CDATA[
{
  // Code size       34 (0x22)
  .maxstack  4
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  ldarg.0
  IL_0003:  ldc.i4.1
  IL_0004:  constrained. "T"
  IL_000a:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
  IL_000f:  ldarg.0
  IL_0010:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0015:  add.ovf
  IL_0016:  constrained. "T"
  IL_001c:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
  IL_0021:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Assignment_Compound_Indexer_Struct_Value_Ref_InWith()
            Dim comp =
<compilation>
    <file>
Imports System

Interface IMoveable
    Property Position(x As Integer) As Integer
End Interface

Structure Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Structure

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1)

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2)
    End Sub

    Private Shared Sub Call1(Of T As {Structure, IMoveable})(ByRef item As T)
        With item
            .Position(1) += GetOffset(item)
        End With
    End Sub

    Private Shared Sub Call2(Of T As {IMoveable})(ByRef item As T)
        With item
            .Position(1) += GetOffset(item)
        End With
    End Sub

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function
End Class
    </file>
</compilation>

            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '1'
Position set for item '-1'
Position get for item '2'
Position set for item '-2'
").VerifyDiagnostics()

            verifier.VerifyIL("Program.Call1(Of T)",
            <![CDATA[
{
  // Code size       34 (0x22)
  .maxstack  4
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  ldarg.0
  IL_0003:  ldc.i4.1
  IL_0004:  constrained. "T"
  IL_000a:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
  IL_000f:  ldarg.0
  IL_0010:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0015:  add.ovf
  IL_0016:  constrained. "T"
  IL_001c:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
  IL_0021:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Assignment_Compound_Indexer_Class_Value_Async_01_InWith()
            Dim comp =
<compilation>
    <file>
Imports System
Imports System.Threading.Tasks

Interface IMoveable
    Property Position(x As Integer) As Integer
End Interface

Class Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Class

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1).Wait()

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2).Wait()
    End Sub

    Private Shared Async Function Call1(Of T As {Class, IMoveable})(item As T) As Task
        With item
            .Position(1) += await GetOffsetAsync(GetOffset(item))
        End With
    End Function

    Private Shared Async Function Call2(Of T As {IMoveable})(item As T) As Task
        With item
            .Position(1) += await GetOffsetAsync(GetOffset(item))
        End With
    End Function

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function

    Shared Function GetOffsetAsync(i As Integer) As Task(Of Integer)
        Return Task.FromResult(i)
    End Function
End Class
    </file>
</compilation>

            ' Wrong output
            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '1'
Position set for item '-1'
Position get for item '2'
Position set for item '-2'
").VerifyDiagnostics()

            'Wrong IL
            verifier.VerifyIL("Program.VB$StateMachine_2_Call1(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      223 (0xdf)
  .maxstack  5
  .locals init (Integer V_0,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_1,
                System.Exception V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0061
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0011:  ldc.i4.1
    IL_0012:  constrained. "SM$T"
    IL_0018:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
    IL_001d:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As Integer"
    IL_0022:  ldarg.0
    IL_0023:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0028:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_002d:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_0032:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0037:  stloc.1
    IL_0038:  ldloca.s   V_1
    IL_003a:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_003f:  brtrue.s   IL_007d
    IL_0041:  ldarg.0
    IL_0042:  ldc.i4.0
    IL_0043:  dup
    IL_0044:  stloc.0
    IL_0045:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_004a:  ldarg.0
    IL_004b:  ldloc.1
    IL_004c:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0051:  ldarg.0
    IL_0052:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_0057:  ldloca.s   V_1
    IL_0059:  ldarg.0
    IL_005a:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_2_Call1(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_2_Call1(Of SM$T))"
    IL_005f:  leave.s    IL_00de
    IL_0061:  ldarg.0
    IL_0062:  ldc.i4.m1
    IL_0063:  dup
    IL_0064:  stloc.0
    IL_0065:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_006a:  ldarg.0
    IL_006b:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0070:  stloc.1
    IL_0071:  ldarg.0
    IL_0072:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0077:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_007d:  ldarg.0
    IL_007e:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0083:  ldc.i4.1
    IL_0084:  ldarg.0
    IL_0085:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As Integer"
    IL_008a:  ldloca.s   V_1
    IL_008c:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_0091:  ldloca.s   V_1
    IL_0093:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0099:  add.ovf
    IL_009a:  constrained. "SM$T"
    IL_00a0:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
    IL_00a5:  leave.s    IL_00c9
  }
  catch System.Exception
  {
    IL_00a7:  dup
    IL_00a8:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_00ad:  stloc.2
    IL_00ae:  ldarg.0
    IL_00af:  ldc.i4.s   -2
    IL_00b1:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_00b6:  ldarg.0
    IL_00b7:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00bc:  ldloc.2
    IL_00bd:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_00c2:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_00c7:  leave.s    IL_00de
  }
  IL_00c9:  ldarg.0
  IL_00ca:  ldc.i4.s   -2
  IL_00cc:  dup
  IL_00cd:  stloc.0
  IL_00ce:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_00d3:  ldarg.0
  IL_00d4:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_00d9:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_00de:  ret
}
]]>)

            'Wrong IL
            verifier.VerifyIL("Program.VB$StateMachine_3_Call2(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      223 (0xdf)
  .maxstack  5
  .locals init (Integer V_0,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_1,
                System.Exception V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0061
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_0011:  ldc.i4.1
    IL_0012:  constrained. "SM$T"
    IL_0018:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
    IL_001d:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$U1 As Integer"
    IL_0022:  ldarg.0
    IL_0023:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_0028:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_002d:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_0032:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0037:  stloc.1
    IL_0038:  ldloca.s   V_1
    IL_003a:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_003f:  brtrue.s   IL_007d
    IL_0041:  ldarg.0
    IL_0042:  ldc.i4.0
    IL_0043:  dup
    IL_0044:  stloc.0
    IL_0045:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_004a:  ldarg.0
    IL_004b:  ldloc.1
    IL_004c:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0051:  ldarg.0
    IL_0052:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_0057:  ldloca.s   V_1
    IL_0059:  ldarg.0
    IL_005a:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_3_Call2(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_3_Call2(Of SM$T))"
    IL_005f:  leave.s    IL_00de
    IL_0061:  ldarg.0
    IL_0062:  ldc.i4.m1
    IL_0063:  dup
    IL_0064:  stloc.0
    IL_0065:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_006a:  ldarg.0
    IL_006b:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0070:  stloc.1
    IL_0071:  ldarg.0
    IL_0072:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0077:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_007d:  ldarg.0
    IL_007e:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_0083:  ldc.i4.1
    IL_0084:  ldarg.0
    IL_0085:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$U1 As Integer"
    IL_008a:  ldloca.s   V_1
    IL_008c:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_0091:  ldloca.s   V_1
    IL_0093:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0099:  add.ovf
    IL_009a:  constrained. "SM$T"
    IL_00a0:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
    IL_00a5:  leave.s    IL_00c9
  }
  catch System.Exception
  {
    IL_00a7:  dup
    IL_00a8:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_00ad:  stloc.2
    IL_00ae:  ldarg.0
    IL_00af:  ldc.i4.s   -2
    IL_00b1:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_00b6:  ldarg.0
    IL_00b7:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00bc:  ldloc.2
    IL_00bd:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_00c2:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_00c7:  leave.s    IL_00de
  }
  IL_00c9:  ldarg.0
  IL_00ca:  ldc.i4.s   -2
  IL_00cc:  dup
  IL_00cd:  stloc.0
  IL_00ce:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
  IL_00d3:  ldarg.0
  IL_00d4:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_00d9:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_00de:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Assignment_Compound_Indexer_Struct_Value_Async_01_InWith()
            Dim comp =
<compilation>
    <file>
Imports System
Imports System.Threading.Tasks

Interface IMoveable
    Property Position(x As Integer) As Integer
End Interface

Structure Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Structure

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1).Wait()

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2).Wait()
    End Sub

    Private Shared Async Function Call1(Of T As {Structure, IMoveable})(item As T) As Task
        With item
            .Position(1) += await GetOffsetAsync(GetOffset(item))
        End With
    End Function

    Private Shared Async Function Call2(Of T As {IMoveable})(item As T) As Task
        With item
            .Position(1) += await GetOffsetAsync(GetOffset(item))
        End With
    End Function

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function

    Shared Function GetOffsetAsync(i As Integer) As Task(Of Integer)
        Return Task.FromResult(i)
    End Function
End Class
    </file>
</compilation>

            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '1'
Position set for item '-1'
Position get for item '2'
Position set for item '-2'
").VerifyDiagnostics()

            verifier.VerifyIL("Program.VB$StateMachine_2_Call1(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      223 (0xdf)
  .maxstack  5
  .locals init (Integer V_0,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_1,
                System.Exception V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0061
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0011:  ldc.i4.1
    IL_0012:  constrained. "SM$T"
    IL_0018:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
    IL_001d:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As Integer"
    IL_0022:  ldarg.0
    IL_0023:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0028:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_002d:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_0032:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0037:  stloc.1
    IL_0038:  ldloca.s   V_1
    IL_003a:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_003f:  brtrue.s   IL_007d
    IL_0041:  ldarg.0
    IL_0042:  ldc.i4.0
    IL_0043:  dup
    IL_0044:  stloc.0
    IL_0045:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_004a:  ldarg.0
    IL_004b:  ldloc.1
    IL_004c:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0051:  ldarg.0
    IL_0052:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_0057:  ldloca.s   V_1
    IL_0059:  ldarg.0
    IL_005a:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_2_Call1(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_2_Call1(Of SM$T))"
    IL_005f:  leave.s    IL_00de
    IL_0061:  ldarg.0
    IL_0062:  ldc.i4.m1
    IL_0063:  dup
    IL_0064:  stloc.0
    IL_0065:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_006a:  ldarg.0
    IL_006b:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0070:  stloc.1
    IL_0071:  ldarg.0
    IL_0072:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0077:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_007d:  ldarg.0
    IL_007e:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0083:  ldc.i4.1
    IL_0084:  ldarg.0
    IL_0085:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As Integer"
    IL_008a:  ldloca.s   V_1
    IL_008c:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_0091:  ldloca.s   V_1
    IL_0093:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0099:  add.ovf
    IL_009a:  constrained. "SM$T"
    IL_00a0:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
    IL_00a5:  leave.s    IL_00c9
  }
  catch System.Exception
  {
    IL_00a7:  dup
    IL_00a8:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_00ad:  stloc.2
    IL_00ae:  ldarg.0
    IL_00af:  ldc.i4.s   -2
    IL_00b1:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_00b6:  ldarg.0
    IL_00b7:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00bc:  ldloc.2
    IL_00bd:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_00c2:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_00c7:  leave.s    IL_00de
  }
  IL_00c9:  ldarg.0
  IL_00ca:  ldc.i4.s   -2
  IL_00cc:  dup
  IL_00cd:  stloc.0
  IL_00ce:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_00d3:  ldarg.0
  IL_00d4:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_00d9:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_00de:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Assignment_Compound_Indexer_Class_Index_ThroughArray()
            Dim comp =
<compilation>
    <file>
Imports System

Interface IMoveable
    Property Position(x As Integer) As Integer
End Interface

Class Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Class

Class Item2
    Inherits Item
End Class

Class Program
    Shared Sub Main()
        Dim item1 = { New Item2 With {.Name = "1"} }
        Call1(DirectCast(item1, Item()))

        Dim item2 = { New Item2 With {.Name = "2"} }
        Call2(DirectCast(item2, Item()))
    End Sub

    Private Shared Sub Call1(Of T As {Class, IMoveable})(item As T())
        item(GetArrayIndex()).Position(GetOffset(item)) += 1
    End Sub

    Private Shared Sub Call2(Of T As {IMoveable})(item As T())
        item(GetArrayIndex()).Position(GetOffset(item)) += 1
    End Sub

    Shared value as Integer

    Shared Function GetOffset(Of T)(item As T()) As Integer
        value -= 1
        item(0) = DirectCast(DirectCast(New Item2 With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function

    Shared Function GetArrayIndex() As Integer
        Return 0
    End Function
End Class
    </file>
</compilation>

            ' Wrong output
            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '-1'
Position set for item '-1'
Position get for item '-2'
Position set for item '-2'
").VerifyDiagnostics()

            'Wrong IL
            verifier.VerifyIL("Program.Call1(Of T)",
            <![CDATA[
{
  // Code size       60 (0x3c)
  .maxstack  4
  .locals init (T() V_0,
                Integer V_1,
                Integer V_2)
  IL_0000:  ldarg.0
  IL_0001:  dup
  IL_0002:  stloc.0
  IL_0003:  call       "Function Program.GetArrayIndex() As Integer"
  IL_0008:  dup
  IL_0009:  stloc.1
  IL_000a:  readonly.
  IL_000c:  ldelema    "T"
  IL_0011:  ldarg.0
  IL_0012:  call       "Function Program.GetOffset(Of T)(T()) As Integer"
  IL_0017:  dup
  IL_0018:  stloc.2
  IL_0019:  ldloc.0
  IL_001a:  ldloc.1
  IL_001b:  readonly.
  IL_001d:  ldelema    "T"
  IL_0022:  ldloc.2
  IL_0023:  constrained. "T"
  IL_0029:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
  IL_002e:  ldc.i4.1
  IL_002f:  add.ovf
  IL_0030:  constrained. "T"
  IL_0036:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
  IL_003b:  ret
}
]]>)

            'Wrong IL
            verifier.VerifyIL("Program.Call2(Of T)",
            <![CDATA[
{
  // Code size       60 (0x3c)
  .maxstack  4
  .locals init (T() V_0,
                Integer V_1,
                Integer V_2)
  IL_0000:  ldarg.0
  IL_0001:  dup
  IL_0002:  stloc.0
  IL_0003:  call       "Function Program.GetArrayIndex() As Integer"
  IL_0008:  dup
  IL_0009:  stloc.1
  IL_000a:  readonly.
  IL_000c:  ldelema    "T"
  IL_0011:  ldarg.0
  IL_0012:  call       "Function Program.GetOffset(Of T)(T()) As Integer"
  IL_0017:  dup
  IL_0018:  stloc.2
  IL_0019:  ldloc.0
  IL_001a:  ldloc.1
  IL_001b:  readonly.
  IL_001d:  ldelema    "T"
  IL_0022:  ldloc.2
  IL_0023:  constrained. "T"
  IL_0029:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
  IL_002e:  ldc.i4.1
  IL_002f:  add.ovf
  IL_0030:  constrained. "T"
  IL_0036:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
  IL_003b:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Assignment_Compound_Indexer_Struct_Index_ThroughArray()
            Dim comp =
<compilation>
    <file>
Imports System

Interface IMoveable
    Property Position(x As Integer) As Integer
End Interface

Structure Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Structure

Class Program
    Shared Sub Main()
        Dim item1 = { New Item With {.Name = "1"} }
        Call1(item1)

        Dim item2 = { New Item With {.Name = "2"} }
        Call2(item2)
    End Sub

    Private Shared Sub Call1(Of T As {Structure, IMoveable})(item As T())
        item(GetArrayIndex()).Position(GetOffset(item)) += 1
    End Sub

    Private Shared Sub Call2(Of T As {IMoveable})(item As T())
        item(GetArrayIndex()).Position(GetOffset(item)) += 1
    End Sub

    Shared value as Integer

    Shared Function GetOffset(Of T)(item As T()) As Integer
        value -= 1
        item(0) = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function

    Shared Function GetArrayIndex() As Integer
        Return 0
    End Function
End Class
    </file>
</compilation>

            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '-1'
Position set for item '-1'
Position get for item '-2'
Position set for item '-2'
").VerifyDiagnostics()

            verifier.VerifyIL("Program.Call1(Of T)",
            <![CDATA[
{
  // Code size       60 (0x3c)
  .maxstack  4
  .locals init (T() V_0,
                Integer V_1,
                Integer V_2)
  IL_0000:  ldarg.0
  IL_0001:  dup
  IL_0002:  stloc.0
  IL_0003:  call       "Function Program.GetArrayIndex() As Integer"
  IL_0008:  dup
  IL_0009:  stloc.1
  IL_000a:  readonly.
  IL_000c:  ldelema    "T"
  IL_0011:  ldarg.0
  IL_0012:  call       "Function Program.GetOffset(Of T)(T()) As Integer"
  IL_0017:  dup
  IL_0018:  stloc.2
  IL_0019:  ldloc.0
  IL_001a:  ldloc.1
  IL_001b:  readonly.
  IL_001d:  ldelema    "T"
  IL_0022:  ldloc.2
  IL_0023:  constrained. "T"
  IL_0029:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
  IL_002e:  ldc.i4.1
  IL_002f:  add.ovf
  IL_0030:  constrained. "T"
  IL_0036:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
  IL_003b:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Assignment_Compound_Indexer_Class_Index_Async_01_ThroughArray()
            Dim comp =
<compilation>
    <file>
Imports System
Imports System.Threading.Tasks

Interface IMoveable
    Property Position(x As Integer) As Integer
End Interface

Class Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Class

Class Item2
    Inherits Item
End Class

Class Program
    Shared Sub Main()
        Dim item1 = { New Item2 With {.Name = "1"} }
        Call1(DirectCast(item1, Item())).Wait()

        Dim item2 = { New Item2 With {.Name = "2"} }
        Call2(DirectCast(item2, Item())).Wait()
    End Sub

    Private Shared Async Function Call1(Of T As {Class, IMoveable})(item As T()) As Task
        item(GetArrayIndex()).Position(await GetOffsetAsync(GetOffset(item))) += 1
    End Function

    Private Shared Async Function Call2(Of T As {IMoveable})(item As T()) As Task
        item(GetArrayIndex()).Position(await GetOffsetAsync(GetOffset(item))) += 1
    End Function

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T()) As Integer
        value -= 1
        item(0) = DirectCast(DirectCast(New Item2 With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function

    Shared Function GetOffsetAsync(i As Integer) As Task(Of Integer)
        Return Task.FromResult(i)
    End Function

    Shared Function GetArrayIndex() As Integer
        Return 0
    End Function
End Class
    </file>
</compilation>

            ' Wrong output
            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '-1'
Position set for item '-1'
Position get for item '-2'
Position set for item '-2'
").VerifyDiagnostics()

            'Wrong IL
            verifier.VerifyIL("Program.VB$StateMachine_2_Call1(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      317 (0x13d)
  .maxstack  4
  .locals init (Integer V_0,
                Integer V_1,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_2,
                SM$T() V_3,
                Integer V_4,
                System.Exception V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse    IL_008e
    IL_000d:  ldarg.0
    IL_000e:  ldarg.0
    IL_000f:  ldarg.0
    IL_0010:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T()"
    IL_0015:  dup
    IL_0016:  stloc.3
    IL_0017:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$S0 As SM$T()"
    IL_001c:  ldloc.3
    IL_001d:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As SM$T()"
    IL_0022:  ldarg.0
    IL_0023:  ldarg.0
    IL_0024:  call       "Function Program.GetArrayIndex() As Integer"
    IL_0029:  dup
    IL_002a:  stloc.s    V_4
    IL_002c:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$S1 As Integer"
    IL_0031:  ldloc.s    V_4
    IL_0033:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U2 As Integer"
    IL_0038:  ldarg.0
    IL_0039:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As SM$T()"
    IL_003e:  ldarg.0
    IL_003f:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U2 As Integer"
    IL_0044:  readonly.
    IL_0046:  ldelema    "SM$T"
    IL_004b:  pop
    IL_004c:  ldarg.0
    IL_004d:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T()"
    IL_0052:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T()) As Integer"
    IL_0057:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_005c:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0061:  stloc.2
    IL_0062:  ldloca.s   V_2
    IL_0064:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_0069:  brtrue.s   IL_00aa
    IL_006b:  ldarg.0
    IL_006c:  ldc.i4.0
    IL_006d:  dup
    IL_006e:  stloc.0
    IL_006f:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0074:  ldarg.0
    IL_0075:  ldloc.2
    IL_0076:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_007b:  ldarg.0
    IL_007c:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_0081:  ldloca.s   V_2
    IL_0083:  ldarg.0
    IL_0084:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_2_Call1(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_2_Call1(Of SM$T))"
    IL_0089:  leave      IL_013c
    IL_008e:  ldarg.0
    IL_008f:  ldc.i4.m1
    IL_0090:  dup
    IL_0091:  stloc.0
    IL_0092:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0097:  ldarg.0
    IL_0098:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_009d:  stloc.2
    IL_009e:  ldarg.0
    IL_009f:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00a4:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00aa:  ldarg.0
    IL_00ab:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As SM$T()"
    IL_00b0:  ldarg.0
    IL_00b1:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U2 As Integer"
    IL_00b6:  readonly.
    IL_00b8:  ldelema    "SM$T"
    IL_00bd:  ldloca.s   V_2
    IL_00bf:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_00c4:  ldloca.s   V_2
    IL_00c6:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00cc:  dup
    IL_00cd:  stloc.1
    IL_00ce:  ldarg.0
    IL_00cf:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$S0 As SM$T()"
    IL_00d4:  ldarg.0
    IL_00d5:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$S1 As Integer"
    IL_00da:  readonly.
    IL_00dc:  ldelema    "SM$T"
    IL_00e1:  ldloc.1
    IL_00e2:  constrained. "SM$T"
    IL_00e8:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
    IL_00ed:  ldc.i4.1
    IL_00ee:  add.ovf
    IL_00ef:  constrained. "SM$T"
    IL_00f5:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
    IL_00fa:  ldarg.0
    IL_00fb:  ldnull
    IL_00fc:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As SM$T()"
    IL_0101:  leave.s    IL_0127
  }
  catch System.Exception
  {
    IL_0103:  dup
    IL_0104:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_0109:  stloc.s    V_5
    IL_010b:  ldarg.0
    IL_010c:  ldc.i4.s   -2
    IL_010e:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0113:  ldarg.0
    IL_0114:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_0119:  ldloc.s    V_5
    IL_011b:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_0120:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_0125:  leave.s    IL_013c
  }
  IL_0127:  ldarg.0
  IL_0128:  ldc.i4.s   -2
  IL_012a:  dup
  IL_012b:  stloc.0
  IL_012c:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_0131:  ldarg.0
  IL_0132:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_0137:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_013c:  ret
}
]]>)

            'Wrong IL
            verifier.VerifyIL("Program.VB$StateMachine_3_Call2(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      317 (0x13d)
  .maxstack  4
  .locals init (Integer V_0,
                Integer V_1,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_2,
                SM$T() V_3,
                Integer V_4,
                System.Exception V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse    IL_008e
    IL_000d:  ldarg.0
    IL_000e:  ldarg.0
    IL_000f:  ldarg.0
    IL_0010:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T()"
    IL_0015:  dup
    IL_0016:  stloc.3
    IL_0017:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$S0 As SM$T()"
    IL_001c:  ldloc.3
    IL_001d:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$U1 As SM$T()"
    IL_0022:  ldarg.0
    IL_0023:  ldarg.0
    IL_0024:  call       "Function Program.GetArrayIndex() As Integer"
    IL_0029:  dup
    IL_002a:  stloc.s    V_4
    IL_002c:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$S1 As Integer"
    IL_0031:  ldloc.s    V_4
    IL_0033:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$U2 As Integer"
    IL_0038:  ldarg.0
    IL_0039:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$U1 As SM$T()"
    IL_003e:  ldarg.0
    IL_003f:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$U2 As Integer"
    IL_0044:  readonly.
    IL_0046:  ldelema    "SM$T"
    IL_004b:  pop
    IL_004c:  ldarg.0
    IL_004d:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T()"
    IL_0052:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T()) As Integer"
    IL_0057:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_005c:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0061:  stloc.2
    IL_0062:  ldloca.s   V_2
    IL_0064:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_0069:  brtrue.s   IL_00aa
    IL_006b:  ldarg.0
    IL_006c:  ldc.i4.0
    IL_006d:  dup
    IL_006e:  stloc.0
    IL_006f:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_0074:  ldarg.0
    IL_0075:  ldloc.2
    IL_0076:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_007b:  ldarg.0
    IL_007c:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_0081:  ldloca.s   V_2
    IL_0083:  ldarg.0
    IL_0084:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_3_Call2(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_3_Call2(Of SM$T))"
    IL_0089:  leave      IL_013c
    IL_008e:  ldarg.0
    IL_008f:  ldc.i4.m1
    IL_0090:  dup
    IL_0091:  stloc.0
    IL_0092:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_0097:  ldarg.0
    IL_0098:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_009d:  stloc.2
    IL_009e:  ldarg.0
    IL_009f:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00a4:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00aa:  ldarg.0
    IL_00ab:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$U1 As SM$T()"
    IL_00b0:  ldarg.0
    IL_00b1:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$U2 As Integer"
    IL_00b6:  readonly.
    IL_00b8:  ldelema    "SM$T"
    IL_00bd:  ldloca.s   V_2
    IL_00bf:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_00c4:  ldloca.s   V_2
    IL_00c6:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00cc:  dup
    IL_00cd:  stloc.1
    IL_00ce:  ldarg.0
    IL_00cf:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$S0 As SM$T()"
    IL_00d4:  ldarg.0
    IL_00d5:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$S1 As Integer"
    IL_00da:  readonly.
    IL_00dc:  ldelema    "SM$T"
    IL_00e1:  ldloc.1
    IL_00e2:  constrained. "SM$T"
    IL_00e8:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
    IL_00ed:  ldc.i4.1
    IL_00ee:  add.ovf
    IL_00ef:  constrained. "SM$T"
    IL_00f5:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
    IL_00fa:  ldarg.0
    IL_00fb:  ldnull
    IL_00fc:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$U1 As SM$T()"
    IL_0101:  leave.s    IL_0127
  }
  catch System.Exception
  {
    IL_0103:  dup
    IL_0104:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_0109:  stloc.s    V_5
    IL_010b:  ldarg.0
    IL_010c:  ldc.i4.s   -2
    IL_010e:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_0113:  ldarg.0
    IL_0114:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_0119:  ldloc.s    V_5
    IL_011b:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_0120:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_0125:  leave.s    IL_013c
  }
  IL_0127:  ldarg.0
  IL_0128:  ldc.i4.s   -2
  IL_012a:  dup
  IL_012b:  stloc.0
  IL_012c:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
  IL_0131:  ldarg.0
  IL_0132:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_0137:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_013c:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Assignment_Compound_Indexer_Struct_Index_Async_01_ThroughArray()
            Dim comp =
<compilation>
    <file>
Imports System
Imports System.Threading.Tasks

Interface IMoveable
    Property Position(x As Integer) As Integer
End Interface

Structure Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Structure

Class Program
    Shared Sub Main()
        Dim item1 = { New Item With {.Name = "1"} }
        Call1(item1).Wait()

        Dim item2 = { New Item With {.Name = "2"} }
        Call2(item2).Wait()
    End Sub

    Private Shared Async Function Call1(Of T As {Structure, IMoveable})(item As T()) As Task
        item(GetArrayIndex()).Position(await GetOffsetAsync(GetOffset(item))) += 1
    End Function

    Private Shared Async Function Call2(Of T As {IMoveable})(item As T()) As Task
        item(GetArrayIndex()).Position(await GetOffsetAsync(GetOffset(item))) += 1
    End Function

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T()) As Integer
        value -= 1
        item(0) = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function

    Shared Function GetOffsetAsync(i As Integer) As Task(Of Integer)
        Return Task.FromResult(i)
    End Function

    Shared Function GetArrayIndex() As Integer
        Return 0
    End Function
End Class
    </file>
</compilation>

            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '-1'
Position set for item '-1'
Position get for item '-2'
Position set for item '-2'
").VerifyDiagnostics()

            verifier.VerifyIL("Program.VB$StateMachine_2_Call1(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      317 (0x13d)
  .maxstack  4
  .locals init (Integer V_0,
                Integer V_1,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_2,
                SM$T() V_3,
                Integer V_4,
                System.Exception V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse    IL_008e
    IL_000d:  ldarg.0
    IL_000e:  ldarg.0
    IL_000f:  ldarg.0
    IL_0010:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T()"
    IL_0015:  dup
    IL_0016:  stloc.3
    IL_0017:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$S0 As SM$T()"
    IL_001c:  ldloc.3
    IL_001d:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As SM$T()"
    IL_0022:  ldarg.0
    IL_0023:  ldarg.0
    IL_0024:  call       "Function Program.GetArrayIndex() As Integer"
    IL_0029:  dup
    IL_002a:  stloc.s    V_4
    IL_002c:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$S1 As Integer"
    IL_0031:  ldloc.s    V_4
    IL_0033:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U2 As Integer"
    IL_0038:  ldarg.0
    IL_0039:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As SM$T()"
    IL_003e:  ldarg.0
    IL_003f:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U2 As Integer"
    IL_0044:  readonly.
    IL_0046:  ldelema    "SM$T"
    IL_004b:  pop
    IL_004c:  ldarg.0
    IL_004d:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T()"
    IL_0052:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T()) As Integer"
    IL_0057:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_005c:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0061:  stloc.2
    IL_0062:  ldloca.s   V_2
    IL_0064:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_0069:  brtrue.s   IL_00aa
    IL_006b:  ldarg.0
    IL_006c:  ldc.i4.0
    IL_006d:  dup
    IL_006e:  stloc.0
    IL_006f:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0074:  ldarg.0
    IL_0075:  ldloc.2
    IL_0076:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_007b:  ldarg.0
    IL_007c:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_0081:  ldloca.s   V_2
    IL_0083:  ldarg.0
    IL_0084:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_2_Call1(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_2_Call1(Of SM$T))"
    IL_0089:  leave      IL_013c
    IL_008e:  ldarg.0
    IL_008f:  ldc.i4.m1
    IL_0090:  dup
    IL_0091:  stloc.0
    IL_0092:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0097:  ldarg.0
    IL_0098:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_009d:  stloc.2
    IL_009e:  ldarg.0
    IL_009f:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00a4:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00aa:  ldarg.0
    IL_00ab:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As SM$T()"
    IL_00b0:  ldarg.0
    IL_00b1:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U2 As Integer"
    IL_00b6:  readonly.
    IL_00b8:  ldelema    "SM$T"
    IL_00bd:  ldloca.s   V_2
    IL_00bf:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_00c4:  ldloca.s   V_2
    IL_00c6:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00cc:  dup
    IL_00cd:  stloc.1
    IL_00ce:  ldarg.0
    IL_00cf:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$S0 As SM$T()"
    IL_00d4:  ldarg.0
    IL_00d5:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$S1 As Integer"
    IL_00da:  readonly.
    IL_00dc:  ldelema    "SM$T"
    IL_00e1:  ldloc.1
    IL_00e2:  constrained. "SM$T"
    IL_00e8:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
    IL_00ed:  ldc.i4.1
    IL_00ee:  add.ovf
    IL_00ef:  constrained. "SM$T"
    IL_00f5:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
    IL_00fa:  ldarg.0
    IL_00fb:  ldnull
    IL_00fc:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As SM$T()"
    IL_0101:  leave.s    IL_0127
  }
  catch System.Exception
  {
    IL_0103:  dup
    IL_0104:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_0109:  stloc.s    V_5
    IL_010b:  ldarg.0
    IL_010c:  ldc.i4.s   -2
    IL_010e:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0113:  ldarg.0
    IL_0114:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_0119:  ldloc.s    V_5
    IL_011b:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_0120:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_0125:  leave.s    IL_013c
  }
  IL_0127:  ldarg.0
  IL_0128:  ldc.i4.s   -2
  IL_012a:  dup
  IL_012b:  stloc.0
  IL_012c:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_0131:  ldarg.0
  IL_0132:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_0137:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_013c:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Assignment_Compound_Indexer_Class_Value_ThroughArray()
            Dim comp =
<compilation>
    <file>
Imports System

Interface IMoveable
    Property Position(x As Integer) As Integer
End Interface

Class Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Class

Class Item2
    Inherits Item
End Class

Class Program
    Shared Sub Main()
        Dim item1 = { New Item2 With {.Name = "1"} }
        Call1(DirectCast(item1, Item()))

        Dim item2 = { New Item2 With {.Name = "2"} }
        Call2(DirectCast(item2, Item()))
    End Sub

    Private Shared Sub Call1(Of T As {Class, IMoveable})(item As T())
        item(GetArrayIndex()).Position(1) += GetOffset(item)
    End Sub

    Private Shared Sub Call2(Of T As {IMoveable})(item As T())
        item(GetArrayIndex()).Position(1) += GetOffset(item)
    End Sub

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T()) As Integer
        value -= 1
        item(0) = DirectCast(DirectCast(New Item2 With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function

    Shared Function GetArrayIndex() As Integer
        Return 0
    End Function
End Class
    </file>
</compilation>

            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '1'
Position set for item '1'
Position get for item '2'
Position set for item '2'
").VerifyDiagnostics()

            'Wrong IL
            verifier.VerifyIL("Program.Call1(Of T)",
            <![CDATA[
{
  // Code size       59 (0x3b)
  .maxstack  4
  .locals init (T() V_0,
                Integer V_1)
  IL_0000:  ldarg.0
  IL_0001:  dup
  IL_0002:  stloc.0
  IL_0003:  call       "Function Program.GetArrayIndex() As Integer"
  IL_0008:  dup
  IL_0009:  stloc.1
  IL_000a:  readonly.
  IL_000c:  ldelema    "T"
  IL_0011:  ldc.i4.1
  IL_0012:  ldloc.0
  IL_0013:  ldloc.1
  IL_0014:  readonly.
  IL_0016:  ldelema    "T"
  IL_001b:  ldc.i4.1
  IL_001c:  constrained. "T"
  IL_0022:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
  IL_0027:  ldarga.s   V_0
  IL_0029:  call       "Function Program.GetOffset(Of T)(ByRef T()) As Integer"
  IL_002e:  add.ovf
  IL_002f:  constrained. "T"
  IL_0035:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
  IL_003a:  ret
}
]]>)

            'Wrong IL
            verifier.VerifyIL("Program.Call2(Of T)",
            <![CDATA[
{
  // Code size       59 (0x3b)
  .maxstack  4
  .locals init (T() V_0,
                Integer V_1)
  IL_0000:  ldarg.0
  IL_0001:  dup
  IL_0002:  stloc.0
  IL_0003:  call       "Function Program.GetArrayIndex() As Integer"
  IL_0008:  dup
  IL_0009:  stloc.1
  IL_000a:  readonly.
  IL_000c:  ldelema    "T"
  IL_0011:  ldc.i4.1
  IL_0012:  ldloc.0
  IL_0013:  ldloc.1
  IL_0014:  readonly.
  IL_0016:  ldelema    "T"
  IL_001b:  ldc.i4.1
  IL_001c:  constrained. "T"
  IL_0022:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
  IL_0027:  ldarga.s   V_0
  IL_0029:  call       "Function Program.GetOffset(Of T)(ByRef T()) As Integer"
  IL_002e:  add.ovf
  IL_002f:  constrained. "T"
  IL_0035:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
  IL_003a:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Assignment_Compound_Indexer_Struct_Value_ThroughArray()
            Dim comp =
<compilation>
    <file>
Imports System

Interface IMoveable
    Property Position(x As Integer) As Integer
End Interface

Structure Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Structure

Class Program
    Shared Sub Main()
        Dim item1 = { New Item With {.Name = "1"} }
        Call1(item1)

        Dim item2 = { New Item With {.Name = "2"} }
        Call2(item2)
    End Sub

    Private Shared Sub Call1(Of T As {Structure, IMoveable})(item As T())
        item(GetArrayIndex()).Position(1) += GetOffset(item)
    End Sub

    Private Shared Sub Call2(Of T As {IMoveable})(item As T())
        item(GetArrayIndex()).Position(1) += GetOffset(item)
    End Sub

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T()) As Integer
        value -= 1
        item(0) = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function

    Shared Function GetArrayIndex() As Integer
        Return 0
    End Function
End Class
    </file>
</compilation>

            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '1'
Position set for item '-1'
Position get for item '2'
Position set for item '-2'
").VerifyDiagnostics()

            verifier.VerifyIL("Program.Call1(Of T)",
            <![CDATA[
{
  // Code size       59 (0x3b)
  .maxstack  4
  .locals init (T() V_0,
                Integer V_1)
  IL_0000:  ldarg.0
  IL_0001:  dup
  IL_0002:  stloc.0
  IL_0003:  call       "Function Program.GetArrayIndex() As Integer"
  IL_0008:  dup
  IL_0009:  stloc.1
  IL_000a:  readonly.
  IL_000c:  ldelema    "T"
  IL_0011:  ldc.i4.1
  IL_0012:  ldloc.0
  IL_0013:  ldloc.1
  IL_0014:  readonly.
  IL_0016:  ldelema    "T"
  IL_001b:  ldc.i4.1
  IL_001c:  constrained. "T"
  IL_0022:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
  IL_0027:  ldarga.s   V_0
  IL_0029:  call       "Function Program.GetOffset(Of T)(ByRef T()) As Integer"
  IL_002e:  add.ovf
  IL_002f:  constrained. "T"
  IL_0035:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
  IL_003a:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Assignment_Compound_Indexer_Class_Value_Async_01_ThroughArray()
            Dim comp =
<compilation>
    <file>
Imports System
Imports System.Threading.Tasks

Interface IMoveable
    Property Position(x As Integer) As Integer
End Interface

Class Item2
    Inherits Item
End Class

Class Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Class

Class Program
    Shared Sub Main()
        Dim item1 = { New Item2 With {.Name = "1"} }
        Call1(DirectCast(item1, Item())).Wait()

        Dim item2 = { New Item2 With {.Name = "2"} }
        Call2(DirectCast(item2, Item())).Wait()
    End Sub

    Private Shared Async Function Call1(Of T As {Class, IMoveable})(item As T()) As Task
        item(GetArrayIndex()).Position(1) += await GetOffsetAsync(GetOffset(item))
    End Function

    Private Shared Async Function Call2(Of T As {IMoveable})(item As T()) As Task
        item(GetArrayIndex()).Position(1) += await GetOffsetAsync(GetOffset(item))
    End Function

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T()) As Integer
        value -= 1
        item(0) = DirectCast(DirectCast(New Item2 With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function

    Shared Function GetOffsetAsync(i As Integer) As Task(Of Integer)
        Return Task.FromResult(i)
    End Function

    Shared Function GetArrayIndex() As Integer
        Return 0
    End Function
End Class
    </file>
</compilation>

            ' Wrong output
            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '1'
Position set for item '-1'
Position get for item '2'
Position set for item '-2'
").VerifyDiagnostics()

            'Wrong IL
            verifier.VerifyIL("Program.VB$StateMachine_2_Call1(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      325 (0x145)
  .maxstack  5
  .locals init (Integer V_0,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_1,
                SM$T() V_2,
                Integer V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse    IL_00b1
    IL_000d:  ldarg.0
    IL_000e:  ldarg.0
    IL_000f:  ldarg.0
    IL_0010:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T()"
    IL_0015:  dup
    IL_0016:  stloc.2
    IL_0017:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$S0 As SM$T()"
    IL_001c:  ldloc.2
    IL_001d:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U2 As SM$T()"
    IL_0022:  ldarg.0
    IL_0023:  ldarg.0
    IL_0024:  call       "Function Program.GetArrayIndex() As Integer"
    IL_0029:  dup
    IL_002a:  stloc.3
    IL_002b:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$S1 As Integer"
    IL_0030:  ldloc.3
    IL_0031:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U3 As Integer"
    IL_0036:  ldarg.0
    IL_0037:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U2 As SM$T()"
    IL_003c:  ldarg.0
    IL_003d:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U3 As Integer"
    IL_0042:  readonly.
    IL_0044:  ldelema    "SM$T"
    IL_0049:  pop
    IL_004a:  ldarg.0
    IL_004b:  ldarg.0
    IL_004c:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$S0 As SM$T()"
    IL_0051:  ldarg.0
    IL_0052:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$S1 As Integer"
    IL_0057:  readonly.
    IL_0059:  ldelema    "SM$T"
    IL_005e:  ldc.i4.1
    IL_005f:  constrained. "SM$T"
    IL_0065:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
    IL_006a:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As Integer"
    IL_006f:  ldarg.0
    IL_0070:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T()"
    IL_0075:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T()) As Integer"
    IL_007a:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_007f:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0084:  stloc.1
    IL_0085:  ldloca.s   V_1
    IL_0087:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_008c:  brtrue.s   IL_00cd
    IL_008e:  ldarg.0
    IL_008f:  ldc.i4.0
    IL_0090:  dup
    IL_0091:  stloc.0
    IL_0092:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0097:  ldarg.0
    IL_0098:  ldloc.1
    IL_0099:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_009e:  ldarg.0
    IL_009f:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00a4:  ldloca.s   V_1
    IL_00a6:  ldarg.0
    IL_00a7:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_2_Call1(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_2_Call1(Of SM$T))"
    IL_00ac:  leave      IL_0144
    IL_00b1:  ldarg.0
    IL_00b2:  ldc.i4.m1
    IL_00b3:  dup
    IL_00b4:  stloc.0
    IL_00b5:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_00ba:  ldarg.0
    IL_00bb:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00c0:  stloc.1
    IL_00c1:  ldarg.0
    IL_00c2:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00c7:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00cd:  ldarg.0
    IL_00ce:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U2 As SM$T()"
    IL_00d3:  ldarg.0
    IL_00d4:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U3 As Integer"
    IL_00d9:  readonly.
    IL_00db:  ldelema    "SM$T"
    IL_00e0:  ldc.i4.1
    IL_00e1:  ldarg.0
    IL_00e2:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As Integer"
    IL_00e7:  ldloca.s   V_1
    IL_00e9:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_00ee:  ldloca.s   V_1
    IL_00f0:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00f6:  add.ovf
    IL_00f7:  constrained. "SM$T"
    IL_00fd:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
    IL_0102:  ldarg.0
    IL_0103:  ldnull
    IL_0104:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U2 As SM$T()"
    IL_0109:  leave.s    IL_012f
  }
  catch System.Exception
  {
    IL_010b:  dup
    IL_010c:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_0111:  stloc.s    V_4
    IL_0113:  ldarg.0
    IL_0114:  ldc.i4.s   -2
    IL_0116:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_011b:  ldarg.0
    IL_011c:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_0121:  ldloc.s    V_4
    IL_0123:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_0128:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_012d:  leave.s    IL_0144
  }
  IL_012f:  ldarg.0
  IL_0130:  ldc.i4.s   -2
  IL_0132:  dup
  IL_0133:  stloc.0
  IL_0134:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_0139:  ldarg.0
  IL_013a:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_013f:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_0144:  ret
}
]]>)

            'Wrong IL
            verifier.VerifyIL("Program.VB$StateMachine_3_Call2(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      325 (0x145)
  .maxstack  5
  .locals init (Integer V_0,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_1,
                SM$T() V_2,
                Integer V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse    IL_00b1
    IL_000d:  ldarg.0
    IL_000e:  ldarg.0
    IL_000f:  ldarg.0
    IL_0010:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T()"
    IL_0015:  dup
    IL_0016:  stloc.2
    IL_0017:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$S0 As SM$T()"
    IL_001c:  ldloc.2
    IL_001d:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$U2 As SM$T()"
    IL_0022:  ldarg.0
    IL_0023:  ldarg.0
    IL_0024:  call       "Function Program.GetArrayIndex() As Integer"
    IL_0029:  dup
    IL_002a:  stloc.3
    IL_002b:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$S1 As Integer"
    IL_0030:  ldloc.3
    IL_0031:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$U3 As Integer"
    IL_0036:  ldarg.0
    IL_0037:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$U2 As SM$T()"
    IL_003c:  ldarg.0
    IL_003d:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$U3 As Integer"
    IL_0042:  readonly.
    IL_0044:  ldelema    "SM$T"
    IL_0049:  pop
    IL_004a:  ldarg.0
    IL_004b:  ldarg.0
    IL_004c:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$S0 As SM$T()"
    IL_0051:  ldarg.0
    IL_0052:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$S1 As Integer"
    IL_0057:  readonly.
    IL_0059:  ldelema    "SM$T"
    IL_005e:  ldc.i4.1
    IL_005f:  constrained. "SM$T"
    IL_0065:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
    IL_006a:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$U1 As Integer"
    IL_006f:  ldarg.0
    IL_0070:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T()"
    IL_0075:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T()) As Integer"
    IL_007a:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_007f:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0084:  stloc.1
    IL_0085:  ldloca.s   V_1
    IL_0087:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_008c:  brtrue.s   IL_00cd
    IL_008e:  ldarg.0
    IL_008f:  ldc.i4.0
    IL_0090:  dup
    IL_0091:  stloc.0
    IL_0092:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_0097:  ldarg.0
    IL_0098:  ldloc.1
    IL_0099:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_009e:  ldarg.0
    IL_009f:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00a4:  ldloca.s   V_1
    IL_00a6:  ldarg.0
    IL_00a7:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_3_Call2(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_3_Call2(Of SM$T))"
    IL_00ac:  leave      IL_0144
    IL_00b1:  ldarg.0
    IL_00b2:  ldc.i4.m1
    IL_00b3:  dup
    IL_00b4:  stloc.0
    IL_00b5:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_00ba:  ldarg.0
    IL_00bb:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00c0:  stloc.1
    IL_00c1:  ldarg.0
    IL_00c2:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00c7:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00cd:  ldarg.0
    IL_00ce:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$U2 As SM$T()"
    IL_00d3:  ldarg.0
    IL_00d4:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$U3 As Integer"
    IL_00d9:  readonly.
    IL_00db:  ldelema    "SM$T"
    IL_00e0:  ldc.i4.1
    IL_00e1:  ldarg.0
    IL_00e2:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$U1 As Integer"
    IL_00e7:  ldloca.s   V_1
    IL_00e9:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_00ee:  ldloca.s   V_1
    IL_00f0:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00f6:  add.ovf
    IL_00f7:  constrained. "SM$T"
    IL_00fd:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
    IL_0102:  ldarg.0
    IL_0103:  ldnull
    IL_0104:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$U2 As SM$T()"
    IL_0109:  leave.s    IL_012f
  }
  catch System.Exception
  {
    IL_010b:  dup
    IL_010c:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_0111:  stloc.s    V_4
    IL_0113:  ldarg.0
    IL_0114:  ldc.i4.s   -2
    IL_0116:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_011b:  ldarg.0
    IL_011c:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_0121:  ldloc.s    V_4
    IL_0123:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_0128:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_012d:  leave.s    IL_0144
  }
  IL_012f:  ldarg.0
  IL_0130:  ldc.i4.s   -2
  IL_0132:  dup
  IL_0133:  stloc.0
  IL_0134:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
  IL_0139:  ldarg.0
  IL_013a:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_013f:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_0144:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Assignment_Compound_Indexer_Struct_Value_Async_01_ThroughArray()
            Dim comp =
<compilation>
    <file>
Imports System
Imports System.Threading.Tasks

Interface IMoveable
    Property Position(x As Integer) As Integer
End Interface

Structure Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Structure

Class Program
    Shared Sub Main()
        Dim item1 = { New Item With {.Name = "1"} }
        Call1(item1).Wait()

        Dim item2 = { New Item With {.Name = "2"} }
        Call2(item2).Wait()
    End Sub

    Private Shared Async Function Call1(Of T As {Structure, IMoveable})(item As T()) As Task
        item(GetArrayIndex()).Position(1) += await GetOffsetAsync(GetOffset(item))
    End Function

    Private Shared Async Function Call2(Of T As {IMoveable})(item As T()) As Task
        item(GetArrayIndex()).Position(1) += await GetOffsetAsync(GetOffset(item))
    End Function

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T()) As Integer
        value -= 1
        item(0) = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function

    Shared Function GetOffsetAsync(i As Integer) As Task(Of Integer)
        Return Task.FromResult(i)
    End Function

    Shared Function GetArrayIndex() As Integer
        Return 0
    End Function
End Class
    </file>
</compilation>

            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '1'
Position set for item '-1'
Position get for item '2'
Position set for item '-2'
").VerifyDiagnostics()

            verifier.VerifyIL("Program.VB$StateMachine_2_Call1(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      325 (0x145)
  .maxstack  5
  .locals init (Integer V_0,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_1,
                SM$T() V_2,
                Integer V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse    IL_00b1
    IL_000d:  ldarg.0
    IL_000e:  ldarg.0
    IL_000f:  ldarg.0
    IL_0010:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T()"
    IL_0015:  dup
    IL_0016:  stloc.2
    IL_0017:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$S0 As SM$T()"
    IL_001c:  ldloc.2
    IL_001d:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U2 As SM$T()"
    IL_0022:  ldarg.0
    IL_0023:  ldarg.0
    IL_0024:  call       "Function Program.GetArrayIndex() As Integer"
    IL_0029:  dup
    IL_002a:  stloc.3
    IL_002b:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$S1 As Integer"
    IL_0030:  ldloc.3
    IL_0031:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U3 As Integer"
    IL_0036:  ldarg.0
    IL_0037:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U2 As SM$T()"
    IL_003c:  ldarg.0
    IL_003d:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U3 As Integer"
    IL_0042:  readonly.
    IL_0044:  ldelema    "SM$T"
    IL_0049:  pop
    IL_004a:  ldarg.0
    IL_004b:  ldarg.0
    IL_004c:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$S0 As SM$T()"
    IL_0051:  ldarg.0
    IL_0052:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$S1 As Integer"
    IL_0057:  readonly.
    IL_0059:  ldelema    "SM$T"
    IL_005e:  ldc.i4.1
    IL_005f:  constrained. "SM$T"
    IL_0065:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
    IL_006a:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As Integer"
    IL_006f:  ldarg.0
    IL_0070:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T()"
    IL_0075:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T()) As Integer"
    IL_007a:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_007f:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0084:  stloc.1
    IL_0085:  ldloca.s   V_1
    IL_0087:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_008c:  brtrue.s   IL_00cd
    IL_008e:  ldarg.0
    IL_008f:  ldc.i4.0
    IL_0090:  dup
    IL_0091:  stloc.0
    IL_0092:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0097:  ldarg.0
    IL_0098:  ldloc.1
    IL_0099:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_009e:  ldarg.0
    IL_009f:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00a4:  ldloca.s   V_1
    IL_00a6:  ldarg.0
    IL_00a7:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_2_Call1(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_2_Call1(Of SM$T))"
    IL_00ac:  leave      IL_0144
    IL_00b1:  ldarg.0
    IL_00b2:  ldc.i4.m1
    IL_00b3:  dup
    IL_00b4:  stloc.0
    IL_00b5:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_00ba:  ldarg.0
    IL_00bb:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00c0:  stloc.1
    IL_00c1:  ldarg.0
    IL_00c2:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00c7:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00cd:  ldarg.0
    IL_00ce:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U2 As SM$T()"
    IL_00d3:  ldarg.0
    IL_00d4:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U3 As Integer"
    IL_00d9:  readonly.
    IL_00db:  ldelema    "SM$T"
    IL_00e0:  ldc.i4.1
    IL_00e1:  ldarg.0
    IL_00e2:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As Integer"
    IL_00e7:  ldloca.s   V_1
    IL_00e9:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_00ee:  ldloca.s   V_1
    IL_00f0:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00f6:  add.ovf
    IL_00f7:  constrained. "SM$T"
    IL_00fd:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
    IL_0102:  ldarg.0
    IL_0103:  ldnull
    IL_0104:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U2 As SM$T()"
    IL_0109:  leave.s    IL_012f
  }
  catch System.Exception
  {
    IL_010b:  dup
    IL_010c:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_0111:  stloc.s    V_4
    IL_0113:  ldarg.0
    IL_0114:  ldc.i4.s   -2
    IL_0116:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_011b:  ldarg.0
    IL_011c:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_0121:  ldloc.s    V_4
    IL_0123:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_0128:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_012d:  leave.s    IL_0144
  }
  IL_012f:  ldarg.0
  IL_0130:  ldc.i4.s   -2
  IL_0132:  dup
  IL_0133:  stloc.0
  IL_0134:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_0139:  ldarg.0
  IL_013a:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_013f:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_0144:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Assignment_Compound_Indexer_Class_Index_ThroughArray_InWith()
            Dim comp =
<compilation>
    <file>
Imports System

Interface IMoveable
    Property Position(x As Integer) As Integer
End Interface

Class Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Class

Class Item2
    Inherits Item
End Class

Class Program
    Shared Sub Main()
        Dim item1 = { New Item2 With {.Name = "1"} }
        Call1(DirectCast(item1, Item()))

        Dim item2 = { New Item2 With {.Name = "2"} }
        Call2(DirectCast(item2, Item()))
    End Sub

    Private Shared Sub Call1(Of T As {Class, IMoveable})(item As T())
        With item(GetArrayIndex())
            .Position(GetOffset(item)) += 1
        End With
    End Sub

    Private Shared Sub Call2(Of T As {IMoveable})(item As T())
        With item(GetArrayIndex())
            .Position(GetOffset(item)) += 1
        End With
    End Sub

    Shared value as Integer

    Shared Function GetOffset(Of T)(item As T()) As Integer
        value -= 1
        item(0) = DirectCast(DirectCast(New Item2 With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function

    Shared Function GetArrayIndex() As Integer
        Return 0
    End Function
End Class
    </file>
</compilation>

            ' Wrong output
            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '-1'
Position set for item '-1'
Position get for item '-2'
Position set for item '-2'
").VerifyDiagnostics()

            'Wrong IL
            verifier.VerifyIL("Program.Call1(Of T)",
            <![CDATA[
{
  // Code size       62 (0x3e)
  .maxstack  4
  .locals init (T() V_0, //$W0
                Integer V_1, //$W1
                Integer V_2)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  call       "Function Program.GetArrayIndex() As Integer"
  IL_0007:  stloc.1
  IL_0008:  ldloc.0
  IL_0009:  ldloc.1
  IL_000a:  readonly.
  IL_000c:  ldelema    "T"
  IL_0011:  ldarg.0
  IL_0012:  call       "Function Program.GetOffset(Of T)(T()) As Integer"
  IL_0017:  dup
  IL_0018:  stloc.2
  IL_0019:  ldloc.0
  IL_001a:  ldloc.1
  IL_001b:  readonly.
  IL_001d:  ldelema    "T"
  IL_0022:  ldloc.2
  IL_0023:  constrained. "T"
  IL_0029:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
  IL_002e:  ldc.i4.1
  IL_002f:  add.ovf
  IL_0030:  constrained. "T"
  IL_0036:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
  IL_003b:  ldnull
  IL_003c:  stloc.0
  IL_003d:  ret
}
]]>)

            'Wrong IL
            verifier.VerifyIL("Program.Call2(Of T)",
            <![CDATA[
{
  // Code size       62 (0x3e)
  .maxstack  4
  .locals init (T() V_0, //$W0
                Integer V_1, //$W1
                Integer V_2)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  call       "Function Program.GetArrayIndex() As Integer"
  IL_0007:  stloc.1
  IL_0008:  ldloc.0
  IL_0009:  ldloc.1
  IL_000a:  readonly.
  IL_000c:  ldelema    "T"
  IL_0011:  ldarg.0
  IL_0012:  call       "Function Program.GetOffset(Of T)(T()) As Integer"
  IL_0017:  dup
  IL_0018:  stloc.2
  IL_0019:  ldloc.0
  IL_001a:  ldloc.1
  IL_001b:  readonly.
  IL_001d:  ldelema    "T"
  IL_0022:  ldloc.2
  IL_0023:  constrained. "T"
  IL_0029:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
  IL_002e:  ldc.i4.1
  IL_002f:  add.ovf
  IL_0030:  constrained. "T"
  IL_0036:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
  IL_003b:  ldnull
  IL_003c:  stloc.0
  IL_003d:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Assignment_Compound_Indexer_Struct_Index_ThroughArray_InWith()
            Dim comp =
<compilation>
    <file>
Imports System

Interface IMoveable
    Property Position(x As Integer) As Integer
End Interface

Structure Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Structure

Class Program
    Shared Sub Main()
        Dim item1 = { New Item With {.Name = "1"} }
        Call1(item1)

        Dim item2 = { New Item With {.Name = "2"} }
        Call2(item2)
    End Sub

    Private Shared Sub Call1(Of T As {Structure, IMoveable})(item As T())
        With item(GetArrayIndex())
            .Position(GetOffset(item)) += 1
        End With
    End Sub

    Private Shared Sub Call2(Of T As {IMoveable})(item As T())
        With item(GetArrayIndex())
            .Position(GetOffset(item)) += 1
        End With
    End Sub

    Shared value as Integer

    Shared Function GetOffset(Of T)(item As T()) As Integer
        value -= 1
        item(0) = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function

    Shared Function GetArrayIndex() As Integer
        Return 0
    End Function
End Class
    </file>
</compilation>

            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '-1'
Position set for item '-1'
Position get for item '-2'
Position set for item '-2'
").VerifyDiagnostics()

            verifier.VerifyIL("Program.Call1(Of T)",
            <![CDATA[
{
  // Code size       62 (0x3e)
  .maxstack  4
  .locals init (T() V_0, //$W0
                Integer V_1, //$W1
                Integer V_2)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  call       "Function Program.GetArrayIndex() As Integer"
  IL_0007:  stloc.1
  IL_0008:  ldloc.0
  IL_0009:  ldloc.1
  IL_000a:  readonly.
  IL_000c:  ldelema    "T"
  IL_0011:  ldarg.0
  IL_0012:  call       "Function Program.GetOffset(Of T)(T()) As Integer"
  IL_0017:  dup
  IL_0018:  stloc.2
  IL_0019:  ldloc.0
  IL_001a:  ldloc.1
  IL_001b:  readonly.
  IL_001d:  ldelema    "T"
  IL_0022:  ldloc.2
  IL_0023:  constrained. "T"
  IL_0029:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
  IL_002e:  ldc.i4.1
  IL_002f:  add.ovf
  IL_0030:  constrained. "T"
  IL_0036:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
  IL_003b:  ldnull
  IL_003c:  stloc.0
  IL_003d:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Assignment_Compound_Indexer_Class_Index_Async_01_ThroughArray_InWith()
            Dim comp =
<compilation>
    <file>
Imports System
Imports System.Threading.Tasks

Interface IMoveable
    Property Position(x As Integer) As Integer
End Interface

Class Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Class

Class Item2
    Inherits Item
End Class

Class Program
    Shared Sub Main()
        Dim item1 = { New Item2 With {.Name = "1"} }
        Call1(DirectCast(item1, Item())).Wait()

        Dim item2 = { New Item2 With {.Name = "2"} }
        Call2(DirectCast(item2, Item())).Wait()
    End Sub

    Private Shared Async Function Call1(Of T As {Class, IMoveable})(item As T()) As Task
        With item(GetArrayIndex())
            .Position(await GetOffsetAsync(GetOffset(item))) += 1
        End With
    End Function

    Private Shared Async Function Call2(Of T As {IMoveable})(item As T()) As Task
        With item(GetArrayIndex())
            .Position(await GetOffsetAsync(GetOffset(item))) += 1
        End With
    End Function

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T()) As Integer
        value -= 1
        item(0) = DirectCast(DirectCast(New Item2 With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function

    Shared Function GetOffsetAsync(i As Integer) As Task(Of Integer)
        Return Task.FromResult(i)
    End Function

    Shared Function GetArrayIndex() As Integer
        Return 0
    End Function
End Class
    </file>
</compilation>

            ' Wrong output
            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '-1'
Position set for item '-1'
Position get for item '-2'
Position set for item '-2'
").VerifyDiagnostics()

            'Wrong IL
            verifier.VerifyIL("Program.VB$StateMachine_2_Call1(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      326 (0x146)
  .maxstack  4
  .locals init (Integer V_0,
                Integer V_1,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse    IL_0092
    IL_000d:  ldarg.0
    IL_000e:  ldarg.0
    IL_000f:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T()"
    IL_0014:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$W0 As SM$T()"
    IL_0019:  ldarg.0
    IL_001a:  call       "Function Program.GetArrayIndex() As Integer"
    IL_001f:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$W1 As Integer"
    IL_0024:  ldarg.0
    IL_0025:  ldarg.0
    IL_0026:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$W0 As SM$T()"
    IL_002b:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As SM$T()"
    IL_0030:  ldarg.0
    IL_0031:  ldarg.0
    IL_0032:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$W1 As Integer"
    IL_0037:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U2 As Integer"
    IL_003c:  ldarg.0
    IL_003d:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As SM$T()"
    IL_0042:  ldarg.0
    IL_0043:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U2 As Integer"
    IL_0048:  readonly.
    IL_004a:  ldelema    "SM$T"
    IL_004f:  pop
    IL_0050:  ldarg.0
    IL_0051:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T()"
    IL_0056:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T()) As Integer"
    IL_005b:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_0060:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0065:  stloc.2
    IL_0066:  ldloca.s   V_2
    IL_0068:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_006d:  brtrue.s   IL_00ae
    IL_006f:  ldarg.0
    IL_0070:  ldc.i4.0
    IL_0071:  dup
    IL_0072:  stloc.0
    IL_0073:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0078:  ldarg.0
    IL_0079:  ldloc.2
    IL_007a:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_007f:  ldarg.0
    IL_0080:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_0085:  ldloca.s   V_2
    IL_0087:  ldarg.0
    IL_0088:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_2_Call1(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_2_Call1(Of SM$T))"
    IL_008d:  leave      IL_0145
    IL_0092:  ldarg.0
    IL_0093:  ldc.i4.m1
    IL_0094:  dup
    IL_0095:  stloc.0
    IL_0096:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_009b:  ldarg.0
    IL_009c:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00a1:  stloc.2
    IL_00a2:  ldarg.0
    IL_00a3:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00a8:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00ae:  ldarg.0
    IL_00af:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As SM$T()"
    IL_00b4:  ldarg.0
    IL_00b5:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U2 As Integer"
    IL_00ba:  readonly.
    IL_00bc:  ldelema    "SM$T"
    IL_00c1:  ldloca.s   V_2
    IL_00c3:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_00c8:  ldloca.s   V_2
    IL_00ca:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00d0:  dup
    IL_00d1:  stloc.1
    IL_00d2:  ldarg.0
    IL_00d3:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$W0 As SM$T()"
    IL_00d8:  ldarg.0
    IL_00d9:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$W1 As Integer"
    IL_00de:  readonly.
    IL_00e0:  ldelema    "SM$T"
    IL_00e5:  ldloc.1
    IL_00e6:  constrained. "SM$T"
    IL_00ec:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
    IL_00f1:  ldc.i4.1
    IL_00f2:  add.ovf
    IL_00f3:  constrained. "SM$T"
    IL_00f9:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
    IL_00fe:  ldarg.0
    IL_00ff:  ldnull
    IL_0100:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As SM$T()"
    IL_0105:  ldarg.0
    IL_0106:  ldnull
    IL_0107:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$W0 As SM$T()"
    IL_010c:  leave.s    IL_0130
  }
  catch System.Exception
  {
    IL_010e:  dup
    IL_010f:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_0114:  stloc.3
    IL_0115:  ldarg.0
    IL_0116:  ldc.i4.s   -2
    IL_0118:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_011d:  ldarg.0
    IL_011e:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_0123:  ldloc.3
    IL_0124:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_0129:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_012e:  leave.s    IL_0145
  }
  IL_0130:  ldarg.0
  IL_0131:  ldc.i4.s   -2
  IL_0133:  dup
  IL_0134:  stloc.0
  IL_0135:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_013a:  ldarg.0
  IL_013b:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_0140:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_0145:  ret
}
]]>)

            'Wrong IL
            verifier.VerifyIL("Program.VB$StateMachine_3_Call2(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      326 (0x146)
  .maxstack  4
  .locals init (Integer V_0,
                Integer V_1,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse    IL_0092
    IL_000d:  ldarg.0
    IL_000e:  ldarg.0
    IL_000f:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T()"
    IL_0014:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$W0 As SM$T()"
    IL_0019:  ldarg.0
    IL_001a:  call       "Function Program.GetArrayIndex() As Integer"
    IL_001f:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$W1 As Integer"
    IL_0024:  ldarg.0
    IL_0025:  ldarg.0
    IL_0026:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$W0 As SM$T()"
    IL_002b:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$U1 As SM$T()"
    IL_0030:  ldarg.0
    IL_0031:  ldarg.0
    IL_0032:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$W1 As Integer"
    IL_0037:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$U2 As Integer"
    IL_003c:  ldarg.0
    IL_003d:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$U1 As SM$T()"
    IL_0042:  ldarg.0
    IL_0043:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$U2 As Integer"
    IL_0048:  readonly.
    IL_004a:  ldelema    "SM$T"
    IL_004f:  pop
    IL_0050:  ldarg.0
    IL_0051:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T()"
    IL_0056:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T()) As Integer"
    IL_005b:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_0060:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0065:  stloc.2
    IL_0066:  ldloca.s   V_2
    IL_0068:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_006d:  brtrue.s   IL_00ae
    IL_006f:  ldarg.0
    IL_0070:  ldc.i4.0
    IL_0071:  dup
    IL_0072:  stloc.0
    IL_0073:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_0078:  ldarg.0
    IL_0079:  ldloc.2
    IL_007a:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_007f:  ldarg.0
    IL_0080:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_0085:  ldloca.s   V_2
    IL_0087:  ldarg.0
    IL_0088:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_3_Call2(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_3_Call2(Of SM$T))"
    IL_008d:  leave      IL_0145
    IL_0092:  ldarg.0
    IL_0093:  ldc.i4.m1
    IL_0094:  dup
    IL_0095:  stloc.0
    IL_0096:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_009b:  ldarg.0
    IL_009c:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00a1:  stloc.2
    IL_00a2:  ldarg.0
    IL_00a3:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00a8:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00ae:  ldarg.0
    IL_00af:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$U1 As SM$T()"
    IL_00b4:  ldarg.0
    IL_00b5:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$U2 As Integer"
    IL_00ba:  readonly.
    IL_00bc:  ldelema    "SM$T"
    IL_00c1:  ldloca.s   V_2
    IL_00c3:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_00c8:  ldloca.s   V_2
    IL_00ca:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00d0:  dup
    IL_00d1:  stloc.1
    IL_00d2:  ldarg.0
    IL_00d3:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$W0 As SM$T()"
    IL_00d8:  ldarg.0
    IL_00d9:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$W1 As Integer"
    IL_00de:  readonly.
    IL_00e0:  ldelema    "SM$T"
    IL_00e5:  ldloc.1
    IL_00e6:  constrained. "SM$T"
    IL_00ec:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
    IL_00f1:  ldc.i4.1
    IL_00f2:  add.ovf
    IL_00f3:  constrained. "SM$T"
    IL_00f9:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
    IL_00fe:  ldarg.0
    IL_00ff:  ldnull
    IL_0100:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$U1 As SM$T()"
    IL_0105:  ldarg.0
    IL_0106:  ldnull
    IL_0107:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$W0 As SM$T()"
    IL_010c:  leave.s    IL_0130
  }
  catch System.Exception
  {
    IL_010e:  dup
    IL_010f:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_0114:  stloc.3
    IL_0115:  ldarg.0
    IL_0116:  ldc.i4.s   -2
    IL_0118:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_011d:  ldarg.0
    IL_011e:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_0123:  ldloc.3
    IL_0124:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_0129:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_012e:  leave.s    IL_0145
  }
  IL_0130:  ldarg.0
  IL_0131:  ldc.i4.s   -2
  IL_0133:  dup
  IL_0134:  stloc.0
  IL_0135:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
  IL_013a:  ldarg.0
  IL_013b:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_0140:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_0145:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Assignment_Compound_Indexer_Struct_Index_Async_01_ThroughArray_InWith()
            Dim comp =
<compilation>
    <file>
Imports System
Imports System.Threading.Tasks

Interface IMoveable
    Property Position(x As Integer) As Integer
End Interface

Structure Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Structure

Class Program
    Shared Sub Main()
        Dim item1 = { New Item With {.Name = "1"} }
        Call1(item1).Wait()

        Dim item2 = { New Item With {.Name = "2"} }
        Call2(item2).Wait()
    End Sub

    Private Shared Async Function Call1(Of T As {Structure, IMoveable})(item As T()) As Task
        With item(GetArrayIndex())
            .Position(await GetOffsetAsync(GetOffset(item))) += 1
        End With
    End Function

    Private Shared Async Function Call2(Of T As {IMoveable})(item As T()) As Task
        With item(GetArrayIndex())
            .Position(await GetOffsetAsync(GetOffset(item))) += 1
        End With
    End Function

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T()) As Integer
        value -= 1
        item(0) = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function

    Shared Function GetOffsetAsync(i As Integer) As Task(Of Integer)
        Return Task.FromResult(i)
    End Function

    Shared Function GetArrayIndex() As Integer
        Return 0
    End Function
End Class
    </file>
</compilation>

            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '-1'
Position set for item '-1'
Position get for item '-2'
Position set for item '-2'
").VerifyDiagnostics()

            verifier.VerifyIL("Program.VB$StateMachine_2_Call1(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      326 (0x146)
  .maxstack  4
  .locals init (Integer V_0,
                Integer V_1,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse    IL_0092
    IL_000d:  ldarg.0
    IL_000e:  ldarg.0
    IL_000f:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T()"
    IL_0014:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$W0 As SM$T()"
    IL_0019:  ldarg.0
    IL_001a:  call       "Function Program.GetArrayIndex() As Integer"
    IL_001f:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$W1 As Integer"
    IL_0024:  ldarg.0
    IL_0025:  ldarg.0
    IL_0026:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$W0 As SM$T()"
    IL_002b:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As SM$T()"
    IL_0030:  ldarg.0
    IL_0031:  ldarg.0
    IL_0032:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$W1 As Integer"
    IL_0037:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U2 As Integer"
    IL_003c:  ldarg.0
    IL_003d:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As SM$T()"
    IL_0042:  ldarg.0
    IL_0043:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U2 As Integer"
    IL_0048:  readonly.
    IL_004a:  ldelema    "SM$T"
    IL_004f:  pop
    IL_0050:  ldarg.0
    IL_0051:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T()"
    IL_0056:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T()) As Integer"
    IL_005b:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_0060:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0065:  stloc.2
    IL_0066:  ldloca.s   V_2
    IL_0068:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_006d:  brtrue.s   IL_00ae
    IL_006f:  ldarg.0
    IL_0070:  ldc.i4.0
    IL_0071:  dup
    IL_0072:  stloc.0
    IL_0073:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0078:  ldarg.0
    IL_0079:  ldloc.2
    IL_007a:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_007f:  ldarg.0
    IL_0080:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_0085:  ldloca.s   V_2
    IL_0087:  ldarg.0
    IL_0088:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_2_Call1(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_2_Call1(Of SM$T))"
    IL_008d:  leave      IL_0145
    IL_0092:  ldarg.0
    IL_0093:  ldc.i4.m1
    IL_0094:  dup
    IL_0095:  stloc.0
    IL_0096:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_009b:  ldarg.0
    IL_009c:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00a1:  stloc.2
    IL_00a2:  ldarg.0
    IL_00a3:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00a8:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00ae:  ldarg.0
    IL_00af:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As SM$T()"
    IL_00b4:  ldarg.0
    IL_00b5:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U2 As Integer"
    IL_00ba:  readonly.
    IL_00bc:  ldelema    "SM$T"
    IL_00c1:  ldloca.s   V_2
    IL_00c3:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_00c8:  ldloca.s   V_2
    IL_00ca:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00d0:  dup
    IL_00d1:  stloc.1
    IL_00d2:  ldarg.0
    IL_00d3:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$W0 As SM$T()"
    IL_00d8:  ldarg.0
    IL_00d9:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$W1 As Integer"
    IL_00de:  readonly.
    IL_00e0:  ldelema    "SM$T"
    IL_00e5:  ldloc.1
    IL_00e6:  constrained. "SM$T"
    IL_00ec:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
    IL_00f1:  ldc.i4.1
    IL_00f2:  add.ovf
    IL_00f3:  constrained. "SM$T"
    IL_00f9:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
    IL_00fe:  ldarg.0
    IL_00ff:  ldnull
    IL_0100:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As SM$T()"
    IL_0105:  ldarg.0
    IL_0106:  ldnull
    IL_0107:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$W0 As SM$T()"
    IL_010c:  leave.s    IL_0130
  }
  catch System.Exception
  {
    IL_010e:  dup
    IL_010f:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_0114:  stloc.3
    IL_0115:  ldarg.0
    IL_0116:  ldc.i4.s   -2
    IL_0118:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_011d:  ldarg.0
    IL_011e:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_0123:  ldloc.3
    IL_0124:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_0129:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_012e:  leave.s    IL_0145
  }
  IL_0130:  ldarg.0
  IL_0131:  ldc.i4.s   -2
  IL_0133:  dup
  IL_0134:  stloc.0
  IL_0135:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_013a:  ldarg.0
  IL_013b:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_0140:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_0145:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Assignment_Compound_Indexer_Class_Value_ThroughArray_InWith()
            Dim comp =
<compilation>
    <file>
Imports System

Interface IMoveable
    Property Position(x As Integer) As Integer
End Interface

Class Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Class

Class Item2
    Inherits Item
End Class

Class Program
    Shared Sub Main()
        Dim item1 = { New Item2 With {.Name = "1"} }
        Call1(DirectCast(item1, Item()))

        Dim item2 = { New Item2 With {.Name = "2"} }
        Call2(DirectCast(item2, Item()))
    End Sub

    Private Shared Sub Call1(Of T As {Class, IMoveable})(item As T())
        With item(GetArrayIndex())
            .Position(1) += GetOffset(item)
        End With
    End Sub

    Private Shared Sub Call2(Of T As {IMoveable})(item As T())
        With item(GetArrayIndex())
            .Position(1) += GetOffset(item)
        End With
    End Sub

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T()) As Integer
        value -= 1
        item(0) = DirectCast(DirectCast(New Item2 With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function

    Shared Function GetArrayIndex() As Integer
        Return 0
    End Function
End Class
    </file>
</compilation>

            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '1'
Position set for item '1'
Position get for item '2'
Position set for item '2'
").VerifyDiagnostics()

            'Wrong IL
            verifier.VerifyIL("Program.Call1(Of T)",
            <![CDATA[
{
  // Code size       61 (0x3d)
  .maxstack  4
  .locals init (T() V_0, //$W0
                Integer V_1) //$W1
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  call       "Function Program.GetArrayIndex() As Integer"
  IL_0007:  stloc.1
  IL_0008:  ldloc.0
  IL_0009:  ldloc.1
  IL_000a:  readonly.
  IL_000c:  ldelema    "T"
  IL_0011:  ldc.i4.1
  IL_0012:  ldloc.0
  IL_0013:  ldloc.1
  IL_0014:  readonly.
  IL_0016:  ldelema    "T"
  IL_001b:  ldc.i4.1
  IL_001c:  constrained. "T"
  IL_0022:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
  IL_0027:  ldarga.s   V_0
  IL_0029:  call       "Function Program.GetOffset(Of T)(ByRef T()) As Integer"
  IL_002e:  add.ovf
  IL_002f:  constrained. "T"
  IL_0035:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
  IL_003a:  ldnull
  IL_003b:  stloc.0
  IL_003c:  ret
}
]]>)

            'Wrong IL
            verifier.VerifyIL("Program.Call2(Of T)",
            <![CDATA[
{
  // Code size       61 (0x3d)
  .maxstack  4
  .locals init (T() V_0, //$W0
                Integer V_1) //$W1
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  call       "Function Program.GetArrayIndex() As Integer"
  IL_0007:  stloc.1
  IL_0008:  ldloc.0
  IL_0009:  ldloc.1
  IL_000a:  readonly.
  IL_000c:  ldelema    "T"
  IL_0011:  ldc.i4.1
  IL_0012:  ldloc.0
  IL_0013:  ldloc.1
  IL_0014:  readonly.
  IL_0016:  ldelema    "T"
  IL_001b:  ldc.i4.1
  IL_001c:  constrained. "T"
  IL_0022:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
  IL_0027:  ldarga.s   V_0
  IL_0029:  call       "Function Program.GetOffset(Of T)(ByRef T()) As Integer"
  IL_002e:  add.ovf
  IL_002f:  constrained. "T"
  IL_0035:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
  IL_003a:  ldnull
  IL_003b:  stloc.0
  IL_003c:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Assignment_Compound_Indexer_Struct_Value_ThroughArray_InWith()
            Dim comp =
<compilation>
    <file>
Imports System

Interface IMoveable
    Property Position(x As Integer) As Integer
End Interface

Structure Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Structure

Class Program
    Shared Sub Main()
        Dim item1 = { New Item With {.Name = "1"} }
        Call1(item1)

        Dim item2 = { New Item With {.Name = "2"} }
        Call2(item2)
    End Sub

    Private Shared Sub Call1(Of T As {Structure, IMoveable})(item As T())
        With item(GetArrayIndex())
            .Position(1) += GetOffset(item)
        End With
    End Sub

    Private Shared Sub Call2(Of T As {IMoveable})(item As T())
        With item(GetArrayIndex())
            .Position(1) += GetOffset(item)
        End With
    End Sub

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T()) As Integer
        value -= 1
        item(0) = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function

    Shared Function GetArrayIndex() As Integer
        Return 0
    End Function
End Class
    </file>
</compilation>

            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '1'
Position set for item '-1'
Position get for item '2'
Position set for item '-2'
").VerifyDiagnostics()

            verifier.VerifyIL("Program.Call1(Of T)",
            <![CDATA[
{
  // Code size       61 (0x3d)
  .maxstack  4
  .locals init (T() V_0, //$W0
                Integer V_1) //$W1
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  call       "Function Program.GetArrayIndex() As Integer"
  IL_0007:  stloc.1
  IL_0008:  ldloc.0
  IL_0009:  ldloc.1
  IL_000a:  readonly.
  IL_000c:  ldelema    "T"
  IL_0011:  ldc.i4.1
  IL_0012:  ldloc.0
  IL_0013:  ldloc.1
  IL_0014:  readonly.
  IL_0016:  ldelema    "T"
  IL_001b:  ldc.i4.1
  IL_001c:  constrained. "T"
  IL_0022:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
  IL_0027:  ldarga.s   V_0
  IL_0029:  call       "Function Program.GetOffset(Of T)(ByRef T()) As Integer"
  IL_002e:  add.ovf
  IL_002f:  constrained. "T"
  IL_0035:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
  IL_003a:  ldnull
  IL_003b:  stloc.0
  IL_003c:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Assignment_Compound_Indexer_Class_Value_Async_01_ThroughArray_InWith()
            Dim comp =
<compilation>
    <file>
Imports System
Imports System.Threading.Tasks

Interface IMoveable
    Property Position(x As Integer) As Integer
End Interface

Class Item2
    Inherits Item
End Class

Class Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Class

Class Program
    Shared Sub Main()
        Dim item1 = { New Item2 With {.Name = "1"} }
        Call1(DirectCast(item1, Item())).Wait()

        Dim item2 = { New Item2 With {.Name = "2"} }
        Call2(DirectCast(item2, Item())).Wait()
    End Sub

    Private Shared Async Function Call1(Of T As {Class, IMoveable})(item As T()) As Task
        With item(GetArrayIndex())
            .Position(1) += await GetOffsetAsync(GetOffset(item))
        End With
    End Function

    Private Shared Async Function Call2(Of T As {IMoveable})(item As T()) As Task
        With item(GetArrayIndex())
            .Position(1) += await GetOffsetAsync(GetOffset(item))
        End With
    End Function

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T()) As Integer
        value -= 1
        item(0) = DirectCast(DirectCast(New Item2 With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function

    Shared Function GetOffsetAsync(i As Integer) As Task(Of Integer)
        Return Task.FromResult(i)
    End Function

    Shared Function GetArrayIndex() As Integer
        Return 0
    End Function
End Class
    </file>
</compilation>

            ' Wrong output
            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '1'
Position set for item '-1'
Position get for item '2'
Position set for item '-2'
").VerifyDiagnostics()

            'Wrong IL
            verifier.VerifyIL("Program.VB$StateMachine_2_Call1(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      303 (0x12f)
  .maxstack  5
  .locals init (Integer V_0,
                SM$T() V_1, //$W0
                Integer V_2, //$W1
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse    IL_0099
    IL_000d:  ldarg.0
    IL_000e:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T()"
    IL_0013:  stloc.1
    IL_0014:  call       "Function Program.GetArrayIndex() As Integer"
    IL_0019:  stloc.2
    IL_001a:  ldarg.0
    IL_001b:  ldloc.1
    IL_001c:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U2 As SM$T()"
    IL_0021:  ldarg.0
    IL_0022:  ldloc.2
    IL_0023:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U3 As Integer"
    IL_0028:  ldarg.0
    IL_0029:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U2 As SM$T()"
    IL_002e:  ldarg.0
    IL_002f:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U3 As Integer"
    IL_0034:  readonly.
    IL_0036:  ldelema    "SM$T"
    IL_003b:  pop
    IL_003c:  ldarg.0
    IL_003d:  ldloc.1
    IL_003e:  ldloc.2
    IL_003f:  readonly.
    IL_0041:  ldelema    "SM$T"
    IL_0046:  ldc.i4.1
    IL_0047:  constrained. "SM$T"
    IL_004d:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
    IL_0052:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As Integer"
    IL_0057:  ldarg.0
    IL_0058:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T()"
    IL_005d:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T()) As Integer"
    IL_0062:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_0067:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_006c:  stloc.3
    IL_006d:  ldloca.s   V_3
    IL_006f:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_0074:  brtrue.s   IL_00b5
    IL_0076:  ldarg.0
    IL_0077:  ldc.i4.0
    IL_0078:  dup
    IL_0079:  stloc.0
    IL_007a:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_007f:  ldarg.0
    IL_0080:  ldloc.3
    IL_0081:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0086:  ldarg.0
    IL_0087:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_008c:  ldloca.s   V_3
    IL_008e:  ldarg.0
    IL_008f:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_2_Call1(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_2_Call1(Of SM$T))"
    IL_0094:  leave      IL_012e
    IL_0099:  ldarg.0
    IL_009a:  ldc.i4.m1
    IL_009b:  dup
    IL_009c:  stloc.0
    IL_009d:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_00a2:  ldarg.0
    IL_00a3:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00a8:  stloc.3
    IL_00a9:  ldarg.0
    IL_00aa:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00af:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00b5:  ldarg.0
    IL_00b6:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U2 As SM$T()"
    IL_00bb:  ldarg.0
    IL_00bc:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U3 As Integer"
    IL_00c1:  readonly.
    IL_00c3:  ldelema    "SM$T"
    IL_00c8:  ldc.i4.1
    IL_00c9:  ldarg.0
    IL_00ca:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As Integer"
    IL_00cf:  ldloca.s   V_3
    IL_00d1:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_00d6:  ldloca.s   V_3
    IL_00d8:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00de:  add.ovf
    IL_00df:  constrained. "SM$T"
    IL_00e5:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
    IL_00ea:  ldarg.0
    IL_00eb:  ldnull
    IL_00ec:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U2 As SM$T()"
    IL_00f1:  ldnull
    IL_00f2:  stloc.1
    IL_00f3:  leave.s    IL_0119
  }
  catch System.Exception
  {
    IL_00f5:  dup
    IL_00f6:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_00fb:  stloc.s    V_4
    IL_00fd:  ldarg.0
    IL_00fe:  ldc.i4.s   -2
    IL_0100:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0105:  ldarg.0
    IL_0106:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_010b:  ldloc.s    V_4
    IL_010d:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_0112:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_0117:  leave.s    IL_012e
  }
  IL_0119:  ldarg.0
  IL_011a:  ldc.i4.s   -2
  IL_011c:  dup
  IL_011d:  stloc.0
  IL_011e:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_0123:  ldarg.0
  IL_0124:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_0129:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_012e:  ret
}
]]>)

            'Wrong IL
            verifier.VerifyIL("Program.VB$StateMachine_3_Call2(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      303 (0x12f)
  .maxstack  5
  .locals init (Integer V_0,
                SM$T() V_1, //$W0
                Integer V_2, //$W1
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse    IL_0099
    IL_000d:  ldarg.0
    IL_000e:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T()"
    IL_0013:  stloc.1
    IL_0014:  call       "Function Program.GetArrayIndex() As Integer"
    IL_0019:  stloc.2
    IL_001a:  ldarg.0
    IL_001b:  ldloc.1
    IL_001c:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$U2 As SM$T()"
    IL_0021:  ldarg.0
    IL_0022:  ldloc.2
    IL_0023:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$U3 As Integer"
    IL_0028:  ldarg.0
    IL_0029:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$U2 As SM$T()"
    IL_002e:  ldarg.0
    IL_002f:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$U3 As Integer"
    IL_0034:  readonly.
    IL_0036:  ldelema    "SM$T"
    IL_003b:  pop
    IL_003c:  ldarg.0
    IL_003d:  ldloc.1
    IL_003e:  ldloc.2
    IL_003f:  readonly.
    IL_0041:  ldelema    "SM$T"
    IL_0046:  ldc.i4.1
    IL_0047:  constrained. "SM$T"
    IL_004d:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
    IL_0052:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$U1 As Integer"
    IL_0057:  ldarg.0
    IL_0058:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T()"
    IL_005d:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T()) As Integer"
    IL_0062:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_0067:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_006c:  stloc.3
    IL_006d:  ldloca.s   V_3
    IL_006f:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_0074:  brtrue.s   IL_00b5
    IL_0076:  ldarg.0
    IL_0077:  ldc.i4.0
    IL_0078:  dup
    IL_0079:  stloc.0
    IL_007a:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_007f:  ldarg.0
    IL_0080:  ldloc.3
    IL_0081:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0086:  ldarg.0
    IL_0087:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_008c:  ldloca.s   V_3
    IL_008e:  ldarg.0
    IL_008f:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_3_Call2(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_3_Call2(Of SM$T))"
    IL_0094:  leave      IL_012e
    IL_0099:  ldarg.0
    IL_009a:  ldc.i4.m1
    IL_009b:  dup
    IL_009c:  stloc.0
    IL_009d:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_00a2:  ldarg.0
    IL_00a3:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00a8:  stloc.3
    IL_00a9:  ldarg.0
    IL_00aa:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00af:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00b5:  ldarg.0
    IL_00b6:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$U2 As SM$T()"
    IL_00bb:  ldarg.0
    IL_00bc:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$U3 As Integer"
    IL_00c1:  readonly.
    IL_00c3:  ldelema    "SM$T"
    IL_00c8:  ldc.i4.1
    IL_00c9:  ldarg.0
    IL_00ca:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$U1 As Integer"
    IL_00cf:  ldloca.s   V_3
    IL_00d1:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_00d6:  ldloca.s   V_3
    IL_00d8:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00de:  add.ovf
    IL_00df:  constrained. "SM$T"
    IL_00e5:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
    IL_00ea:  ldarg.0
    IL_00eb:  ldnull
    IL_00ec:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$U2 As SM$T()"
    IL_00f1:  ldnull
    IL_00f2:  stloc.1
    IL_00f3:  leave.s    IL_0119
  }
  catch System.Exception
  {
    IL_00f5:  dup
    IL_00f6:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_00fb:  stloc.s    V_4
    IL_00fd:  ldarg.0
    IL_00fe:  ldc.i4.s   -2
    IL_0100:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_0105:  ldarg.0
    IL_0106:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_010b:  ldloc.s    V_4
    IL_010d:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_0112:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_0117:  leave.s    IL_012e
  }
  IL_0119:  ldarg.0
  IL_011a:  ldc.i4.s   -2
  IL_011c:  dup
  IL_011d:  stloc.0
  IL_011e:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
  IL_0123:  ldarg.0
  IL_0124:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_0129:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_012e:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Assignment_Compound_Indexer_Struct_Value_Async_01_ThroughArray_InWith()
            Dim comp =
<compilation>
    <file>
Imports System
Imports System.Threading.Tasks

Interface IMoveable
    Property Position(x As Integer) As Integer
End Interface

Structure Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Structure

Class Program
    Shared Sub Main()
        Dim item1 = { New Item With {.Name = "1"} }
        Call1(item1).Wait()

        Dim item2 = { New Item With {.Name = "2"} }
        Call2(item2).Wait()
    End Sub

    Private Shared Async Function Call1(Of T As {Structure, IMoveable})(item As T()) As Task
        With item(GetArrayIndex())
            .Position(1) += await GetOffsetAsync(GetOffset(item))
        End With
    End Function

    Private Shared Async Function Call2(Of T As {IMoveable})(item As T()) As Task
        With item(GetArrayIndex())
            .Position(1) += await GetOffsetAsync(GetOffset(item))
        End With
    End Function

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T()) As Integer
        value -= 1
        item(0) = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function

    Shared Function GetOffsetAsync(i As Integer) As Task(Of Integer)
        Return Task.FromResult(i)
    End Function

    Shared Function GetArrayIndex() As Integer
        Return 0
    End Function
End Class
    </file>
</compilation>

            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '1'
Position set for item '-1'
Position get for item '2'
Position set for item '-2'
").VerifyDiagnostics()

            verifier.VerifyIL("Program.VB$StateMachine_2_Call1(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      303 (0x12f)
  .maxstack  5
  .locals init (Integer V_0,
                SM$T() V_1, //$W0
                Integer V_2, //$W1
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse    IL_0099
    IL_000d:  ldarg.0
    IL_000e:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T()"
    IL_0013:  stloc.1
    IL_0014:  call       "Function Program.GetArrayIndex() As Integer"
    IL_0019:  stloc.2
    IL_001a:  ldarg.0
    IL_001b:  ldloc.1
    IL_001c:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U2 As SM$T()"
    IL_0021:  ldarg.0
    IL_0022:  ldloc.2
    IL_0023:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U3 As Integer"
    IL_0028:  ldarg.0
    IL_0029:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U2 As SM$T()"
    IL_002e:  ldarg.0
    IL_002f:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U3 As Integer"
    IL_0034:  readonly.
    IL_0036:  ldelema    "SM$T"
    IL_003b:  pop
    IL_003c:  ldarg.0
    IL_003d:  ldloc.1
    IL_003e:  ldloc.2
    IL_003f:  readonly.
    IL_0041:  ldelema    "SM$T"
    IL_0046:  ldc.i4.1
    IL_0047:  constrained. "SM$T"
    IL_004d:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
    IL_0052:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As Integer"
    IL_0057:  ldarg.0
    IL_0058:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T()"
    IL_005d:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T()) As Integer"
    IL_0062:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_0067:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_006c:  stloc.3
    IL_006d:  ldloca.s   V_3
    IL_006f:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_0074:  brtrue.s   IL_00b5
    IL_0076:  ldarg.0
    IL_0077:  ldc.i4.0
    IL_0078:  dup
    IL_0079:  stloc.0
    IL_007a:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_007f:  ldarg.0
    IL_0080:  ldloc.3
    IL_0081:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0086:  ldarg.0
    IL_0087:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_008c:  ldloca.s   V_3
    IL_008e:  ldarg.0
    IL_008f:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_2_Call1(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_2_Call1(Of SM$T))"
    IL_0094:  leave      IL_012e
    IL_0099:  ldarg.0
    IL_009a:  ldc.i4.m1
    IL_009b:  dup
    IL_009c:  stloc.0
    IL_009d:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_00a2:  ldarg.0
    IL_00a3:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00a8:  stloc.3
    IL_00a9:  ldarg.0
    IL_00aa:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00af:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00b5:  ldarg.0
    IL_00b6:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U2 As SM$T()"
    IL_00bb:  ldarg.0
    IL_00bc:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U3 As Integer"
    IL_00c1:  readonly.
    IL_00c3:  ldelema    "SM$T"
    IL_00c8:  ldc.i4.1
    IL_00c9:  ldarg.0
    IL_00ca:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As Integer"
    IL_00cf:  ldloca.s   V_3
    IL_00d1:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_00d6:  ldloca.s   V_3
    IL_00d8:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00de:  add.ovf
    IL_00df:  constrained. "SM$T"
    IL_00e5:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
    IL_00ea:  ldarg.0
    IL_00eb:  ldnull
    IL_00ec:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U2 As SM$T()"
    IL_00f1:  ldnull
    IL_00f2:  stloc.1
    IL_00f3:  leave.s    IL_0119
  }
  catch System.Exception
  {
    IL_00f5:  dup
    IL_00f6:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_00fb:  stloc.s    V_4
    IL_00fd:  ldarg.0
    IL_00fe:  ldc.i4.s   -2
    IL_0100:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0105:  ldarg.0
    IL_0106:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_010b:  ldloc.s    V_4
    IL_010d:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_0112:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_0117:  leave.s    IL_012e
  }
  IL_0119:  ldarg.0
  IL_011a:  ldc.i4.s   -2
  IL_011c:  dup
  IL_011d:  stloc.0
  IL_011e:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_0123:  ldarg.0
  IL_0124:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_0129:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_012e:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Redim_Indexer_Class_Index()
            Dim comp =
<compilation>
    <file>
Imports System

Interface IMoveable
    Property Position(x As Integer) As Integer()
End Interface

Class Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer() Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return Nothing
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Class

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1)

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2)
    End Sub

    Private Shared Sub Call1(Of T As {Class, IMoveable})(item As T)
        Redim Preserve item.Position(GetOffset(item))(1)
    End Sub

    Private Shared Sub Call2(Of T As {IMoveable})(item As T)
        Redim Preserve item.Position(GetOffset(item))(1)
    End Sub

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function
End Class
    </file>
</compilation>

            ' Wrong output
            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '-1'
Position set for item '-1'
Position get for item '-2'
Position set for item '-2'
").VerifyDiagnostics()

            'Wrong IL
            verifier.VerifyIL("Program.Call1(Of T)",
            <![CDATA[
{
  // Code size       53 (0x35)
  .maxstack  4
  .locals init (Integer V_0)
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldarga.s   V_0
  IL_0004:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0009:  dup
  IL_000a:  stloc.0
  IL_000b:  ldarga.s   V_0
  IL_000d:  ldloc.0
  IL_000e:  constrained. "T"
  IL_0014:  callvirt   "Function IMoveable.get_Position(Integer) As Integer()"
  IL_0019:  ldc.i4.2
  IL_001a:  newarr     "Integer"
  IL_001f:  call       "Function Microsoft.VisualBasic.CompilerServices.Utils.CopyArray(System.Array, System.Array) As System.Array"
  IL_0024:  castclass  "Integer()"
  IL_0029:  constrained. "T"
  IL_002f:  callvirt   "Sub IMoveable.set_Position(Integer, Integer())"
  IL_0034:  ret
}
]]>)

            'Wrong IL
            verifier.VerifyIL("Program.Call2(Of T)",
            <![CDATA[
{
  // Code size       53 (0x35)
  .maxstack  4
  .locals init (Integer V_0)
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldarga.s   V_0
  IL_0004:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0009:  dup
  IL_000a:  stloc.0
  IL_000b:  ldarga.s   V_0
  IL_000d:  ldloc.0
  IL_000e:  constrained. "T"
  IL_0014:  callvirt   "Function IMoveable.get_Position(Integer) As Integer()"
  IL_0019:  ldc.i4.2
  IL_001a:  newarr     "Integer"
  IL_001f:  call       "Function Microsoft.VisualBasic.CompilerServices.Utils.CopyArray(System.Array, System.Array) As System.Array"
  IL_0024:  castclass  "Integer()"
  IL_0029:  constrained. "T"
  IL_002f:  callvirt   "Sub IMoveable.set_Position(Integer, Integer())"
  IL_0034:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Redim_Indexer_Struct_Index()
            Dim comp =
<compilation>
    <file>
Imports System

Interface IMoveable
    Property Position(x As Integer) As Integer()
End Interface

Structure Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer() Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return Nothing
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Structure

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1)

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2)
    End Sub

    Private Shared Sub Call1(Of T As {Structure, IMoveable})(item As T)
        Redim Preserve item.Position(GetOffset(item))(1)
    End Sub

    Private Shared Sub Call2(Of T As {IMoveable})(item As T)
        Redim Preserve item.Position(GetOffset(item))(1)
    End Sub

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return Nothing
    End Function
End Class
    </file>
</compilation>

            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '-1'
Position set for item '-1'
Position get for item '-2'
Position set for item '-2'
").VerifyDiagnostics()

            verifier.VerifyIL("Program.Call1(Of T)",
            <![CDATA[
{
  // Code size       53 (0x35)
  .maxstack  4
  .locals init (Integer V_0)
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldarga.s   V_0
  IL_0004:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0009:  dup
  IL_000a:  stloc.0
  IL_000b:  ldarga.s   V_0
  IL_000d:  ldloc.0
  IL_000e:  constrained. "T"
  IL_0014:  callvirt   "Function IMoveable.get_Position(Integer) As Integer()"
  IL_0019:  ldc.i4.2
  IL_001a:  newarr     "Integer"
  IL_001f:  call       "Function Microsoft.VisualBasic.CompilerServices.Utils.CopyArray(System.Array, System.Array) As System.Array"
  IL_0024:  castclass  "Integer()"
  IL_0029:  constrained. "T"
  IL_002f:  callvirt   "Sub IMoveable.set_Position(Integer, Integer())"
  IL_0034:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Redim_Indexer_Class_Index_Ref()
            Dim comp =
<compilation>
    <file>
Imports System

Interface IMoveable
    Property Position(x As Integer) As Integer()
End Interface

Class Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer() Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return Nothing
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Class

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1)

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2)
    End Sub

    Private Shared Sub Call1(Of T As {Class, IMoveable})(ByRef item As T)
        Redim Preserve item.Position(GetOffset(item))(1)
    End Sub

    Private Shared Sub Call2(Of T As {IMoveable})(ByRef item As T)
        Redim Preserve item.Position(GetOffset(item))(1)
    End Sub

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return Nothing
    End Function
End Class
    </file>
</compilation>

            ' Wrong output
            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '-1'
Position set for item '-1'
Position get for item '-2'
Position set for item '-2'
").VerifyDiagnostics()

            'Wrong IL
            verifier.VerifyIL("Program.Call1(Of T)",
            <![CDATA[
{
  // Code size       50 (0x32)
  .maxstack  4
  .locals init (Integer V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0007:  dup
  IL_0008:  stloc.0
  IL_0009:  ldarg.0
  IL_000a:  ldloc.0
  IL_000b:  constrained. "T"
  IL_0011:  callvirt   "Function IMoveable.get_Position(Integer) As Integer()"
  IL_0016:  ldc.i4.2
  IL_0017:  newarr     "Integer"
  IL_001c:  call       "Function Microsoft.VisualBasic.CompilerServices.Utils.CopyArray(System.Array, System.Array) As System.Array"
  IL_0021:  castclass  "Integer()"
  IL_0026:  constrained. "T"
  IL_002c:  callvirt   "Sub IMoveable.set_Position(Integer, Integer())"
  IL_0031:  ret
}
]]>)

            'Wrong IL
            verifier.VerifyIL("Program.Call2(Of T)",
            <![CDATA[
{
  // Code size       50 (0x32)
  .maxstack  4
  .locals init (Integer V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0007:  dup
  IL_0008:  stloc.0
  IL_0009:  ldarg.0
  IL_000a:  ldloc.0
  IL_000b:  constrained. "T"
  IL_0011:  callvirt   "Function IMoveable.get_Position(Integer) As Integer()"
  IL_0016:  ldc.i4.2
  IL_0017:  newarr     "Integer"
  IL_001c:  call       "Function Microsoft.VisualBasic.CompilerServices.Utils.CopyArray(System.Array, System.Array) As System.Array"
  IL_0021:  castclass  "Integer()"
  IL_0026:  constrained. "T"
  IL_002c:  callvirt   "Sub IMoveable.set_Position(Integer, Integer())"
  IL_0031:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Redim_Indexer_Struct_Index_Ref()
            Dim comp =
<compilation>
    <file>
Imports System

Interface IMoveable
    Property Position(x As Integer) As Integer()
End Interface

Structure Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer() Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return Nothing
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Structure

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1)

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2)
    End Sub

    Private Shared Sub Call1(Of T As {Structure, IMoveable})(ByRef item As T)
        Redim Preserve item.Position(GetOffset(item))(1)
    End Sub

    Private Shared Sub Call2(Of T As {IMoveable})(ByRef item As T)
        Redim Preserve item.Position(GetOffset(item))(1)
    End Sub

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return Nothing
    End Function
End Class
    </file>
</compilation>

            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '-1'
Position set for item '-1'
Position get for item '-2'
Position set for item '-2'
").VerifyDiagnostics()

            verifier.VerifyIL("Program.Call1(Of T)",
            <![CDATA[
{
  // Code size       50 (0x32)
  .maxstack  4
  .locals init (Integer V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0007:  dup
  IL_0008:  stloc.0
  IL_0009:  ldarg.0
  IL_000a:  ldloc.0
  IL_000b:  constrained. "T"
  IL_0011:  callvirt   "Function IMoveable.get_Position(Integer) As Integer()"
  IL_0016:  ldc.i4.2
  IL_0017:  newarr     "Integer"
  IL_001c:  call       "Function Microsoft.VisualBasic.CompilerServices.Utils.CopyArray(System.Array, System.Array) As System.Array"
  IL_0021:  castclass  "Integer()"
  IL_0026:  constrained. "T"
  IL_002c:  callvirt   "Sub IMoveable.set_Position(Integer, Integer())"
  IL_0031:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Redim_Indexer_Class_Index_Async_01()
            Dim comp =
<compilation>
    <file>
Imports System
Imports System.Threading.Tasks

Interface IMoveable
    Property Position(x As Integer) As Integer()
End Interface

Class Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer() Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return Nothing
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Class

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1).Wait()

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2).Wait()
    End Sub

    Private Shared Async Function Call1(Of T As {Class, IMoveable})(item As T) As Task
        Redim Preserve item.Position(await GetOffsetAsync(GetOffset(item)))(1)
    End Function

    Private Shared Async Function Call2(Of T As {IMoveable})(item As T) As Task
        Redim Preserve item.Position(await GetOffsetAsync(GetOffset(item)))(1)
    End Function

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return Nothing
    End Function

    Shared Function GetOffsetAsync(i As Integer) As Task(Of Integer)
        Return Task.FromResult(i)
    End Function
End Class
    </file>
</compilation>

            ' Wrong output
            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '-1'
Position set for item '-1'
Position get for item '-2'
Position set for item '-2'
").VerifyDiagnostics()

            'Wrong IL
            verifier.VerifyIL("Program.VB$StateMachine_2_Call1(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      230 (0xe6)
  .maxstack  4
  .locals init (Integer V_0,
                Integer V_1,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004c
    IL_000a:  ldarg.0
    IL_000b:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0010:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_0015:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_001a:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_001f:  stloc.2
    IL_0020:  ldloca.s   V_2
    IL_0022:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_0027:  brtrue.s   IL_0068
    IL_0029:  ldarg.0
    IL_002a:  ldc.i4.0
    IL_002b:  dup
    IL_002c:  stloc.0
    IL_002d:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0032:  ldarg.0
    IL_0033:  ldloc.2
    IL_0034:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0039:  ldarg.0
    IL_003a:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_003f:  ldloca.s   V_2
    IL_0041:  ldarg.0
    IL_0042:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_2_Call1(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_2_Call1(Of SM$T))"
    IL_0047:  leave      IL_00e5
    IL_004c:  ldarg.0
    IL_004d:  ldc.i4.m1
    IL_004e:  dup
    IL_004f:  stloc.0
    IL_0050:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0055:  ldarg.0
    IL_0056:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_005b:  stloc.2
    IL_005c:  ldarg.0
    IL_005d:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0062:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0068:  ldarg.0
    IL_0069:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_006e:  ldloca.s   V_2
    IL_0070:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_0075:  ldloca.s   V_2
    IL_0077:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_007d:  dup
    IL_007e:  stloc.1
    IL_007f:  ldarg.0
    IL_0080:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0085:  ldloc.1
    IL_0086:  constrained. "SM$T"
    IL_008c:  callvirt   "Function IMoveable.get_Position(Integer) As Integer()"
    IL_0091:  ldc.i4.2
    IL_0092:  newarr     "Integer"
    IL_0097:  call       "Function Microsoft.VisualBasic.CompilerServices.Utils.CopyArray(System.Array, System.Array) As System.Array"
    IL_009c:  castclass  "Integer()"
    IL_00a1:  constrained. "SM$T"
    IL_00a7:  callvirt   "Sub IMoveable.set_Position(Integer, Integer())"
    IL_00ac:  leave.s    IL_00d0
  }
  catch System.Exception
  {
    IL_00ae:  dup
    IL_00af:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_00b4:  stloc.3
    IL_00b5:  ldarg.0
    IL_00b6:  ldc.i4.s   -2
    IL_00b8:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_00bd:  ldarg.0
    IL_00be:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00c3:  ldloc.3
    IL_00c4:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_00c9:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_00ce:  leave.s    IL_00e5
  }
  IL_00d0:  ldarg.0
  IL_00d1:  ldc.i4.s   -2
  IL_00d3:  dup
  IL_00d4:  stloc.0
  IL_00d5:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_00da:  ldarg.0
  IL_00db:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_00e0:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_00e5:  ret
}
]]>)

            'Wrong IL
            verifier.VerifyIL("Program.VB$StateMachine_3_Call2(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      230 (0xe6)
  .maxstack  4
  .locals init (Integer V_0,
                Integer V_1,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004c
    IL_000a:  ldarg.0
    IL_000b:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_0010:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_0015:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_001a:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_001f:  stloc.2
    IL_0020:  ldloca.s   V_2
    IL_0022:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_0027:  brtrue.s   IL_0068
    IL_0029:  ldarg.0
    IL_002a:  ldc.i4.0
    IL_002b:  dup
    IL_002c:  stloc.0
    IL_002d:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_0032:  ldarg.0
    IL_0033:  ldloc.2
    IL_0034:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0039:  ldarg.0
    IL_003a:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_003f:  ldloca.s   V_2
    IL_0041:  ldarg.0
    IL_0042:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_3_Call2(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_3_Call2(Of SM$T))"
    IL_0047:  leave      IL_00e5
    IL_004c:  ldarg.0
    IL_004d:  ldc.i4.m1
    IL_004e:  dup
    IL_004f:  stloc.0
    IL_0050:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_0055:  ldarg.0
    IL_0056:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_005b:  stloc.2
    IL_005c:  ldarg.0
    IL_005d:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0062:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0068:  ldarg.0
    IL_0069:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_006e:  ldloca.s   V_2
    IL_0070:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_0075:  ldloca.s   V_2
    IL_0077:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_007d:  dup
    IL_007e:  stloc.1
    IL_007f:  ldarg.0
    IL_0080:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_0085:  ldloc.1
    IL_0086:  constrained. "SM$T"
    IL_008c:  callvirt   "Function IMoveable.get_Position(Integer) As Integer()"
    IL_0091:  ldc.i4.2
    IL_0092:  newarr     "Integer"
    IL_0097:  call       "Function Microsoft.VisualBasic.CompilerServices.Utils.CopyArray(System.Array, System.Array) As System.Array"
    IL_009c:  castclass  "Integer()"
    IL_00a1:  constrained. "SM$T"
    IL_00a7:  callvirt   "Sub IMoveable.set_Position(Integer, Integer())"
    IL_00ac:  leave.s    IL_00d0
  }
  catch System.Exception
  {
    IL_00ae:  dup
    IL_00af:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_00b4:  stloc.3
    IL_00b5:  ldarg.0
    IL_00b6:  ldc.i4.s   -2
    IL_00b8:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_00bd:  ldarg.0
    IL_00be:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00c3:  ldloc.3
    IL_00c4:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_00c9:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_00ce:  leave.s    IL_00e5
  }
  IL_00d0:  ldarg.0
  IL_00d1:  ldc.i4.s   -2
  IL_00d3:  dup
  IL_00d4:  stloc.0
  IL_00d5:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
  IL_00da:  ldarg.0
  IL_00db:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_00e0:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_00e5:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Redim_Indexer_Struct_Index_Async_01()
            Dim comp =
<compilation>
    <file>
Imports System
Imports System.Threading.Tasks

Interface IMoveable
    Property Position(x As Integer) As Integer()
End Interface

Structure Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer() Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return Nothing
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Structure

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1).Wait()

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2).Wait()
    End Sub

    Private Shared Async Function Call1(Of T As {Structure, IMoveable})(item As T) As Task
        Redim Preserve item.Position(await GetOffsetAsync(GetOffset(item)))(1)
    End Function

    Private Shared Async Function Call2(Of T As {IMoveable})(item As T) As Task
        Redim Preserve item.Position(await GetOffsetAsync(GetOffset(item)))(1)
    End Function

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return Nothing
    End Function

    Shared Function GetOffsetAsync(i As Integer) As Task(Of Integer)
        Return Task.FromResult(i)
    End Function
End Class
    </file>
</compilation>

            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '-1'
Position set for item '-1'
Position get for item '-2'
Position set for item '-2'
").VerifyDiagnostics()

            verifier.VerifyIL("Program.VB$StateMachine_2_Call1(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      230 (0xe6)
  .maxstack  4
  .locals init (Integer V_0,
                Integer V_1,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004c
    IL_000a:  ldarg.0
    IL_000b:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0010:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_0015:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_001a:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_001f:  stloc.2
    IL_0020:  ldloca.s   V_2
    IL_0022:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_0027:  brtrue.s   IL_0068
    IL_0029:  ldarg.0
    IL_002a:  ldc.i4.0
    IL_002b:  dup
    IL_002c:  stloc.0
    IL_002d:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0032:  ldarg.0
    IL_0033:  ldloc.2
    IL_0034:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0039:  ldarg.0
    IL_003a:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_003f:  ldloca.s   V_2
    IL_0041:  ldarg.0
    IL_0042:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_2_Call1(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_2_Call1(Of SM$T))"
    IL_0047:  leave      IL_00e5
    IL_004c:  ldarg.0
    IL_004d:  ldc.i4.m1
    IL_004e:  dup
    IL_004f:  stloc.0
    IL_0050:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0055:  ldarg.0
    IL_0056:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_005b:  stloc.2
    IL_005c:  ldarg.0
    IL_005d:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0062:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0068:  ldarg.0
    IL_0069:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_006e:  ldloca.s   V_2
    IL_0070:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_0075:  ldloca.s   V_2
    IL_0077:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_007d:  dup
    IL_007e:  stloc.1
    IL_007f:  ldarg.0
    IL_0080:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0085:  ldloc.1
    IL_0086:  constrained. "SM$T"
    IL_008c:  callvirt   "Function IMoveable.get_Position(Integer) As Integer()"
    IL_0091:  ldc.i4.2
    IL_0092:  newarr     "Integer"
    IL_0097:  call       "Function Microsoft.VisualBasic.CompilerServices.Utils.CopyArray(System.Array, System.Array) As System.Array"
    IL_009c:  castclass  "Integer()"
    IL_00a1:  constrained. "SM$T"
    IL_00a7:  callvirt   "Sub IMoveable.set_Position(Integer, Integer())"
    IL_00ac:  leave.s    IL_00d0
  }
  catch System.Exception
  {
    IL_00ae:  dup
    IL_00af:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_00b4:  stloc.3
    IL_00b5:  ldarg.0
    IL_00b6:  ldc.i4.s   -2
    IL_00b8:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_00bd:  ldarg.0
    IL_00be:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00c3:  ldloc.3
    IL_00c4:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_00c9:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_00ce:  leave.s    IL_00e5
  }
  IL_00d0:  ldarg.0
  IL_00d1:  ldc.i4.s   -2
  IL_00d3:  dup
  IL_00d4:  stloc.0
  IL_00d5:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_00da:  ldarg.0
  IL_00db:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_00e0:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_00e5:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Redim_Indexer_Class_Index_Async_02()
            Dim comp =
<compilation>
    <file>
Imports System
Imports System.Threading.Tasks

Interface IMoveable
    Property Position(x As Integer) As Integer()
End Interface

Class Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer() Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return Nothing
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Class

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1).Wait()

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2).Wait()
    End Sub

    Private Shared Async Function Call1(Of T As {Class, IMoveable})(item As T) As Task
        await Task.Yield()
        Redim Preserve item.Position(await GetOffsetAsync(GetOffset(item)))(1)
    End Function

    Private Shared Async Function Call2(Of T As {IMoveable})(item As T) As Task
        await Task.Yield()
        Redim Preserve item.Position(await GetOffsetAsync(GetOffset(item)))(1)
    End Function

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return Nothing
    End Function

    Shared Function GetOffsetAsync(i As Integer) As Task(Of Integer)
        Return Task.FromResult(i)
    End Function
End Class
    </file>
</compilation>

            ' Wrong output
            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '-1'
Position set for item '-1'
Position get for item '-2'
Position set for item '-2'
").VerifyDiagnostics()

            'Wrong IL
            verifier.VerifyIL("Program.VB$StateMachine_2_Call1(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      343 (0x157)
  .maxstack  4
  .locals init (Integer V_0,
                System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_1,
                System.Runtime.CompilerServices.YieldAwaitable V_2,
                Integer V_3,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_4,
                System.Exception V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004b
    IL_000a:  ldloc.0
    IL_000b:  ldc.i4.1
    IL_000c:  beq        IL_00ba
    IL_0011:  call       "Function System.Threading.Tasks.Task.Yield() As System.Runtime.CompilerServices.YieldAwaitable"
    IL_0016:  stloc.2
    IL_0017:  ldloca.s   V_2
    IL_0019:  call       "Function System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter() As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_001e:  stloc.1
    IL_001f:  ldloca.s   V_1
    IL_0021:  call       "Function System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.get_IsCompleted() As Boolean"
    IL_0026:  brtrue.s   IL_0067
    IL_0028:  ldarg.0
    IL_0029:  ldc.i4.0
    IL_002a:  dup
    IL_002b:  stloc.0
    IL_002c:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0031:  ldarg.0
    IL_0032:  ldloc.1
    IL_0033:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0038:  ldarg.0
    IL_0039:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_003e:  ldloca.s   V_1
    IL_0040:  ldarg.0
    IL_0041:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, Program.VB$StateMachine_2_Call1(Of SM$T))(ByRef System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ByRef Program.VB$StateMachine_2_Call1(Of SM$T))"
    IL_0046:  leave      IL_0156
    IL_004b:  ldarg.0
    IL_004c:  ldc.i4.m1
    IL_004d:  dup
    IL_004e:  stloc.0
    IL_004f:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0054:  ldarg.0
    IL_0055:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_005a:  stloc.1
    IL_005b:  ldarg.0
    IL_005c:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0061:  initobj    "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0067:  ldloca.s   V_1
    IL_0069:  call       "Sub System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()"
    IL_006e:  ldloca.s   V_1
    IL_0070:  initobj    "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0076:  ldarg.0
    IL_0077:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_007c:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_0081:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_0086:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_008b:  stloc.s    V_4
    IL_008d:  ldloca.s   V_4
    IL_008f:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_0094:  brtrue.s   IL_00d7
    IL_0096:  ldarg.0
    IL_0097:  ldc.i4.1
    IL_0098:  dup
    IL_0099:  stloc.0
    IL_009a:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_009f:  ldarg.0
    IL_00a0:  ldloc.s    V_4
    IL_00a2:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A1 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00a7:  ldarg.0
    IL_00a8:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00ad:  ldloca.s   V_4
    IL_00af:  ldarg.0
    IL_00b0:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_2_Call1(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_2_Call1(Of SM$T))"
    IL_00b5:  leave      IL_0156
    IL_00ba:  ldarg.0
    IL_00bb:  ldc.i4.m1
    IL_00bc:  dup
    IL_00bd:  stloc.0
    IL_00be:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_00c3:  ldarg.0
    IL_00c4:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A1 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00c9:  stloc.s    V_4
    IL_00cb:  ldarg.0
    IL_00cc:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$A1 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00d1:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00d7:  ldarg.0
    IL_00d8:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_00dd:  ldloca.s   V_4
    IL_00df:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_00e4:  ldloca.s   V_4
    IL_00e6:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00ec:  dup
    IL_00ed:  stloc.3
    IL_00ee:  ldarg.0
    IL_00ef:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_00f4:  ldloc.3
    IL_00f5:  constrained. "SM$T"
    IL_00fb:  callvirt   "Function IMoveable.get_Position(Integer) As Integer()"
    IL_0100:  ldc.i4.2
    IL_0101:  newarr     "Integer"
    IL_0106:  call       "Function Microsoft.VisualBasic.CompilerServices.Utils.CopyArray(System.Array, System.Array) As System.Array"
    IL_010b:  castclass  "Integer()"
    IL_0110:  constrained. "SM$T"
    IL_0116:  callvirt   "Sub IMoveable.set_Position(Integer, Integer())"
    IL_011b:  leave.s    IL_0141
  }
  catch System.Exception
  {
    IL_011d:  dup
    IL_011e:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_0123:  stloc.s    V_5
    IL_0125:  ldarg.0
    IL_0126:  ldc.i4.s   -2
    IL_0128:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_012d:  ldarg.0
    IL_012e:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_0133:  ldloc.s    V_5
    IL_0135:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_013a:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_013f:  leave.s    IL_0156
  }
  IL_0141:  ldarg.0
  IL_0142:  ldc.i4.s   -2
  IL_0144:  dup
  IL_0145:  stloc.0
  IL_0146:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_014b:  ldarg.0
  IL_014c:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_0151:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_0156:  ret
}
]]>)

            'Wrong IL
            verifier.VerifyIL("Program.VB$StateMachine_3_Call2(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      343 (0x157)
  .maxstack  4
  .locals init (Integer V_0,
                System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_1,
                System.Runtime.CompilerServices.YieldAwaitable V_2,
                Integer V_3,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_4,
                System.Exception V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004b
    IL_000a:  ldloc.0
    IL_000b:  ldc.i4.1
    IL_000c:  beq        IL_00ba
    IL_0011:  call       "Function System.Threading.Tasks.Task.Yield() As System.Runtime.CompilerServices.YieldAwaitable"
    IL_0016:  stloc.2
    IL_0017:  ldloca.s   V_2
    IL_0019:  call       "Function System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter() As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_001e:  stloc.1
    IL_001f:  ldloca.s   V_1
    IL_0021:  call       "Function System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.get_IsCompleted() As Boolean"
    IL_0026:  brtrue.s   IL_0067
    IL_0028:  ldarg.0
    IL_0029:  ldc.i4.0
    IL_002a:  dup
    IL_002b:  stloc.0
    IL_002c:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_0031:  ldarg.0
    IL_0032:  ldloc.1
    IL_0033:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0038:  ldarg.0
    IL_0039:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_003e:  ldloca.s   V_1
    IL_0040:  ldarg.0
    IL_0041:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, Program.VB$StateMachine_3_Call2(Of SM$T))(ByRef System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ByRef Program.VB$StateMachine_3_Call2(Of SM$T))"
    IL_0046:  leave      IL_0156
    IL_004b:  ldarg.0
    IL_004c:  ldc.i4.m1
    IL_004d:  dup
    IL_004e:  stloc.0
    IL_004f:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_0054:  ldarg.0
    IL_0055:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_005a:  stloc.1
    IL_005b:  ldarg.0
    IL_005c:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0061:  initobj    "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0067:  ldloca.s   V_1
    IL_0069:  call       "Sub System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()"
    IL_006e:  ldloca.s   V_1
    IL_0070:  initobj    "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0076:  ldarg.0
    IL_0077:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_007c:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_0081:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_0086:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_008b:  stloc.s    V_4
    IL_008d:  ldloca.s   V_4
    IL_008f:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_0094:  brtrue.s   IL_00d7
    IL_0096:  ldarg.0
    IL_0097:  ldc.i4.1
    IL_0098:  dup
    IL_0099:  stloc.0
    IL_009a:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_009f:  ldarg.0
    IL_00a0:  ldloc.s    V_4
    IL_00a2:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A1 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00a7:  ldarg.0
    IL_00a8:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00ad:  ldloca.s   V_4
    IL_00af:  ldarg.0
    IL_00b0:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_3_Call2(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_3_Call2(Of SM$T))"
    IL_00b5:  leave      IL_0156
    IL_00ba:  ldarg.0
    IL_00bb:  ldc.i4.m1
    IL_00bc:  dup
    IL_00bd:  stloc.0
    IL_00be:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_00c3:  ldarg.0
    IL_00c4:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A1 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00c9:  stloc.s    V_4
    IL_00cb:  ldarg.0
    IL_00cc:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$A1 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00d1:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00d7:  ldarg.0
    IL_00d8:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_00dd:  ldloca.s   V_4
    IL_00df:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_00e4:  ldloca.s   V_4
    IL_00e6:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00ec:  dup
    IL_00ed:  stloc.3
    IL_00ee:  ldarg.0
    IL_00ef:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_00f4:  ldloc.3
    IL_00f5:  constrained. "SM$T"
    IL_00fb:  callvirt   "Function IMoveable.get_Position(Integer) As Integer()"
    IL_0100:  ldc.i4.2
    IL_0101:  newarr     "Integer"
    IL_0106:  call       "Function Microsoft.VisualBasic.CompilerServices.Utils.CopyArray(System.Array, System.Array) As System.Array"
    IL_010b:  castclass  "Integer()"
    IL_0110:  constrained. "SM$T"
    IL_0116:  callvirt   "Sub IMoveable.set_Position(Integer, Integer())"
    IL_011b:  leave.s    IL_0141
  }
  catch System.Exception
  {
    IL_011d:  dup
    IL_011e:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_0123:  stloc.s    V_5
    IL_0125:  ldarg.0
    IL_0126:  ldc.i4.s   -2
    IL_0128:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_012d:  ldarg.0
    IL_012e:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_0133:  ldloc.s    V_5
    IL_0135:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_013a:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_013f:  leave.s    IL_0156
  }
  IL_0141:  ldarg.0
  IL_0142:  ldc.i4.s   -2
  IL_0144:  dup
  IL_0145:  stloc.0
  IL_0146:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
  IL_014b:  ldarg.0
  IL_014c:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_0151:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_0156:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Redim_Indexer_Struct_Index_Async_02()
            Dim comp =
<compilation>
    <file>
Imports System
Imports System.Threading.Tasks

Interface IMoveable
    Property Position(x As Integer) As Integer()
End Interface

Structure Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer() Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return Nothing
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Structure

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1).Wait()

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2).Wait()
    End Sub

    Private Shared Async Function Call1(Of T As {Structure, IMoveable})(item As T) As Task
        await Task.Yield()
        Redim Preserve item.Position(await GetOffsetAsync(GetOffset(item)))(1)
    End Function

    Private Shared Async Function Call2(Of T As {IMoveable})(item As T) As Task
        await Task.Yield()
        Redim Preserve item.Position(await GetOffsetAsync(GetOffset(item)))(1)
    End Function

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return Nothing
    End Function

    Shared Function GetOffsetAsync(i As Integer) As Task(Of Integer)
        Return Task.FromResult(i)
    End Function
End Class
    </file>
</compilation>

            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '-1'
Position set for item '-1'
Position get for item '-2'
Position set for item '-2'
").VerifyDiagnostics()

            verifier.VerifyIL("Program.VB$StateMachine_2_Call1(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      343 (0x157)
  .maxstack  4
  .locals init (Integer V_0,
                System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_1,
                System.Runtime.CompilerServices.YieldAwaitable V_2,
                Integer V_3,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_4,
                System.Exception V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004b
    IL_000a:  ldloc.0
    IL_000b:  ldc.i4.1
    IL_000c:  beq        IL_00ba
    IL_0011:  call       "Function System.Threading.Tasks.Task.Yield() As System.Runtime.CompilerServices.YieldAwaitable"
    IL_0016:  stloc.2
    IL_0017:  ldloca.s   V_2
    IL_0019:  call       "Function System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter() As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_001e:  stloc.1
    IL_001f:  ldloca.s   V_1
    IL_0021:  call       "Function System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.get_IsCompleted() As Boolean"
    IL_0026:  brtrue.s   IL_0067
    IL_0028:  ldarg.0
    IL_0029:  ldc.i4.0
    IL_002a:  dup
    IL_002b:  stloc.0
    IL_002c:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0031:  ldarg.0
    IL_0032:  ldloc.1
    IL_0033:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0038:  ldarg.0
    IL_0039:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_003e:  ldloca.s   V_1
    IL_0040:  ldarg.0
    IL_0041:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, Program.VB$StateMachine_2_Call1(Of SM$T))(ByRef System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ByRef Program.VB$StateMachine_2_Call1(Of SM$T))"
    IL_0046:  leave      IL_0156
    IL_004b:  ldarg.0
    IL_004c:  ldc.i4.m1
    IL_004d:  dup
    IL_004e:  stloc.0
    IL_004f:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0054:  ldarg.0
    IL_0055:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_005a:  stloc.1
    IL_005b:  ldarg.0
    IL_005c:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0061:  initobj    "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0067:  ldloca.s   V_1
    IL_0069:  call       "Sub System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()"
    IL_006e:  ldloca.s   V_1
    IL_0070:  initobj    "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0076:  ldarg.0
    IL_0077:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_007c:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_0081:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_0086:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_008b:  stloc.s    V_4
    IL_008d:  ldloca.s   V_4
    IL_008f:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_0094:  brtrue.s   IL_00d7
    IL_0096:  ldarg.0
    IL_0097:  ldc.i4.1
    IL_0098:  dup
    IL_0099:  stloc.0
    IL_009a:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_009f:  ldarg.0
    IL_00a0:  ldloc.s    V_4
    IL_00a2:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A1 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00a7:  ldarg.0
    IL_00a8:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00ad:  ldloca.s   V_4
    IL_00af:  ldarg.0
    IL_00b0:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_2_Call1(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_2_Call1(Of SM$T))"
    IL_00b5:  leave      IL_0156
    IL_00ba:  ldarg.0
    IL_00bb:  ldc.i4.m1
    IL_00bc:  dup
    IL_00bd:  stloc.0
    IL_00be:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_00c3:  ldarg.0
    IL_00c4:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A1 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00c9:  stloc.s    V_4
    IL_00cb:  ldarg.0
    IL_00cc:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$A1 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00d1:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00d7:  ldarg.0
    IL_00d8:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_00dd:  ldloca.s   V_4
    IL_00df:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_00e4:  ldloca.s   V_4
    IL_00e6:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00ec:  dup
    IL_00ed:  stloc.3
    IL_00ee:  ldarg.0
    IL_00ef:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_00f4:  ldloc.3
    IL_00f5:  constrained. "SM$T"
    IL_00fb:  callvirt   "Function IMoveable.get_Position(Integer) As Integer()"
    IL_0100:  ldc.i4.2
    IL_0101:  newarr     "Integer"
    IL_0106:  call       "Function Microsoft.VisualBasic.CompilerServices.Utils.CopyArray(System.Array, System.Array) As System.Array"
    IL_010b:  castclass  "Integer()"
    IL_0110:  constrained. "SM$T"
    IL_0116:  callvirt   "Sub IMoveable.set_Position(Integer, Integer())"
    IL_011b:  leave.s    IL_0141
  }
  catch System.Exception
  {
    IL_011d:  dup
    IL_011e:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_0123:  stloc.s    V_5
    IL_0125:  ldarg.0
    IL_0126:  ldc.i4.s   -2
    IL_0128:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_012d:  ldarg.0
    IL_012e:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_0133:  ldloc.s    V_5
    IL_0135:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_013a:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_013f:  leave.s    IL_0156
  }
  IL_0141:  ldarg.0
  IL_0142:  ldc.i4.s   -2
  IL_0144:  dup
  IL_0145:  stloc.0
  IL_0146:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_014b:  ldarg.0
  IL_014c:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_0151:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_0156:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Redim_Indexer_Class_Value()
            Dim comp =
<compilation>
    <file>
Imports System

Interface IMoveable
    Property Position(x As Integer) As Integer()
End Interface

Class Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer() Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return Nothing
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Class

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1)

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2)
    End Sub

    Private Shared Sub Call1(Of T As {Class, IMoveable})(item As T)
        Redim Preserve item.Position(1)(GetOffset(item))
    End Sub

    Private Shared Sub Call2(Of T As {IMoveable})(item As T)
        Redim Preserve item.Position(1)(GetOffset(item))
    End Sub

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return Nothing
    End Function
End Class
    </file>
</compilation>

            ' Wrong output on some frameworks
            '            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
            '"
            'Position get for item '1'
            'Position set for item '1'
            'Position get for item '2'
            'Position set for item '2'
            '").VerifyDiagnostics()
            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe).VerifyDiagnostics()

            'Wrong IL
            verifier.VerifyIL("Program.Call1(Of T)",
            <![CDATA[
{
  // Code size       53 (0x35)
  .maxstack  5
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldc.i4.1
  IL_0003:  ldarga.s   V_0
  IL_0005:  ldc.i4.1
  IL_0006:  constrained. "T"
  IL_000c:  callvirt   "Function IMoveable.get_Position(Integer) As Integer()"
  IL_0011:  ldarga.s   V_0
  IL_0013:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0018:  ldc.i4.1
  IL_0019:  add.ovf
  IL_001a:  newarr     "Integer"
  IL_001f:  call       "Function Microsoft.VisualBasic.CompilerServices.Utils.CopyArray(System.Array, System.Array) As System.Array"
  IL_0024:  castclass  "Integer()"
  IL_0029:  constrained. "T"
  IL_002f:  callvirt   "Sub IMoveable.set_Position(Integer, Integer())"
  IL_0034:  ret
}
]]>)

            'Wrong IL
            verifier.VerifyIL("Program.Call2(Of T)",
            <![CDATA[
{
  // Code size       53 (0x35)
  .maxstack  5
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldc.i4.1
  IL_0003:  ldarga.s   V_0
  IL_0005:  ldc.i4.1
  IL_0006:  constrained. "T"
  IL_000c:  callvirt   "Function IMoveable.get_Position(Integer) As Integer()"
  IL_0011:  ldarga.s   V_0
  IL_0013:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0018:  ldc.i4.1
  IL_0019:  add.ovf
  IL_001a:  newarr     "Integer"
  IL_001f:  call       "Function Microsoft.VisualBasic.CompilerServices.Utils.CopyArray(System.Array, System.Array) As System.Array"
  IL_0024:  castclass  "Integer()"
  IL_0029:  constrained. "T"
  IL_002f:  callvirt   "Sub IMoveable.set_Position(Integer, Integer())"
  IL_0034:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Redim_Indexer_Struct_Value()
            Dim comp =
<compilation>
    <file>
Imports System

Interface IMoveable
    Property Position(x As Integer) As Integer()
End Interface

Structure Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer() Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return Nothing
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Structure

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1)

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2)
    End Sub

    Private Shared Sub Call1(Of T As {Structure, IMoveable})(item As T)
        Redim Preserve item.Position(1)(GetOffset(item))
    End Sub

    Private Shared Sub Call2(Of T As {IMoveable})(item As T)
        Redim Preserve item.Position(1)(GetOffset(item))
    End Sub

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return Nothing
    End Function
End Class
    </file>
</compilation>

            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '1'
Position set for item '-1'
Position get for item '2'
Position set for item '-2'
").VerifyDiagnostics()

            verifier.VerifyIL("Program.Call1(Of T)",
            <![CDATA[
{
  // Code size       53 (0x35)
  .maxstack  5
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldc.i4.1
  IL_0003:  ldarga.s   V_0
  IL_0005:  ldc.i4.1
  IL_0006:  constrained. "T"
  IL_000c:  callvirt   "Function IMoveable.get_Position(Integer) As Integer()"
  IL_0011:  ldarga.s   V_0
  IL_0013:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0018:  ldc.i4.1
  IL_0019:  add.ovf
  IL_001a:  newarr     "Integer"
  IL_001f:  call       "Function Microsoft.VisualBasic.CompilerServices.Utils.CopyArray(System.Array, System.Array) As System.Array"
  IL_0024:  castclass  "Integer()"
  IL_0029:  constrained. "T"
  IL_002f:  callvirt   "Sub IMoveable.set_Position(Integer, Integer())"
  IL_0034:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Redim_Indexer_Class_Value_Ref()
            Dim comp =
<compilation>
    <file>
Imports System

Interface IMoveable
    Property Position(x As Integer) As Integer()
End Interface

Class Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer() Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return Nothing
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Class

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1)

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2)
    End Sub

    Private Shared Sub Call1(Of T As {Class, IMoveable})(ByRef item As T)
        Redim Preserve item.Position(1)(GetOffset(item))
    End Sub

    Private Shared Sub Call2(Of T As {IMoveable})(ByRef item As T)
        Redim Preserve item.Position(1)(GetOffset(item))
    End Sub

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return Nothing
    End Function
End Class
    </file>
</compilation>

            'Wrong output on some frameworks
            '            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
            '"
            'Position get for item '1'
            'Position set for item '1'
            'Position get for item '2'
            'Position set for item '2'
            '").VerifyDiagnostics()
            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe).VerifyDiagnostics()

            'Wrong IL
            verifier.VerifyIL("Program.Call1(Of T)",
            <![CDATA[
{
  // Code size       50 (0x32)
  .maxstack  5
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  ldarg.0
  IL_0003:  ldc.i4.1
  IL_0004:  constrained. "T"
  IL_000a:  callvirt   "Function IMoveable.get_Position(Integer) As Integer()"
  IL_000f:  ldarg.0
  IL_0010:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0015:  ldc.i4.1
  IL_0016:  add.ovf
  IL_0017:  newarr     "Integer"
  IL_001c:  call       "Function Microsoft.VisualBasic.CompilerServices.Utils.CopyArray(System.Array, System.Array) As System.Array"
  IL_0021:  castclass  "Integer()"
  IL_0026:  constrained. "T"
  IL_002c:  callvirt   "Sub IMoveable.set_Position(Integer, Integer())"
  IL_0031:  ret
}
]]>)

            'Wrong IL
            verifier.VerifyIL("Program.Call2(Of T)",
            <![CDATA[
{
  // Code size       50 (0x32)
  .maxstack  5
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  ldarg.0
  IL_0003:  ldc.i4.1
  IL_0004:  constrained. "T"
  IL_000a:  callvirt   "Function IMoveable.get_Position(Integer) As Integer()"
  IL_000f:  ldarg.0
  IL_0010:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0015:  ldc.i4.1
  IL_0016:  add.ovf
  IL_0017:  newarr     "Integer"
  IL_001c:  call       "Function Microsoft.VisualBasic.CompilerServices.Utils.CopyArray(System.Array, System.Array) As System.Array"
  IL_0021:  castclass  "Integer()"
  IL_0026:  constrained. "T"
  IL_002c:  callvirt   "Sub IMoveable.set_Position(Integer, Integer())"
  IL_0031:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Redim_Indexer_Struct_Value_Ref()
            Dim comp =
<compilation>
    <file>
Imports System

Interface IMoveable
    Property Position(x As Integer) As Integer()
End Interface

Structure Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer() Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return Nothing
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Structure

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1)

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2)
    End Sub

    Private Shared Sub Call1(Of T As {Structure, IMoveable})(ByRef item As T)
        Redim Preserve item.Position(1)(GetOffset(item))
    End Sub

    Private Shared Sub Call2(Of T As {IMoveable})(ByRef item As T)
        Redim Preserve item.Position(1)(GetOffset(item))
    End Sub

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return Nothing
    End Function
End Class
    </file>
</compilation>

            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '1'
Position set for item '-1'
Position get for item '2'
Position set for item '-2'
").VerifyDiagnostics()

            verifier.VerifyIL("Program.Call1(Of T)",
            <![CDATA[
{
  // Code size       50 (0x32)
  .maxstack  5
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  ldarg.0
  IL_0003:  ldc.i4.1
  IL_0004:  constrained. "T"
  IL_000a:  callvirt   "Function IMoveable.get_Position(Integer) As Integer()"
  IL_000f:  ldarg.0
  IL_0010:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0015:  ldc.i4.1
  IL_0016:  add.ovf
  IL_0017:  newarr     "Integer"
  IL_001c:  call       "Function Microsoft.VisualBasic.CompilerServices.Utils.CopyArray(System.Array, System.Array) As System.Array"
  IL_0021:  castclass  "Integer()"
  IL_0026:  constrained. "T"
  IL_002c:  callvirt   "Sub IMoveable.set_Position(Integer, Integer())"
  IL_0031:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Redim_Indexer_Class_Value_Async_01()
            Dim comp =
<compilation>
    <file>
Imports System
Imports System.Threading.Tasks

Interface IMoveable
    Property Position(x As Integer) As Integer()
End Interface

Class Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer() Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return Nothing
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Class

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1).Wait()

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2).Wait()
    End Sub

    Private Shared Async Function Call1(Of T As {Class, IMoveable})(item As T) As Task
        Redim Preserve item.Position(1)(await GetOffsetAsync(GetOffset(item)))
    End Function

    Private Shared Async Function Call2(Of T As {IMoveable})(item As T) As Task
        Redim Preserve item.Position(1)(await GetOffsetAsync(GetOffset(item)))
    End Function

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return Nothing
    End Function

    Shared Function GetOffsetAsync(i As Integer) As Task(Of Integer)
        Return Task.FromResult(i)
    End Function
End Class
    </file>
</compilation>

            ' Wrong output
            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '1'
Position set for item '-1'
Position get for item '2'
Position set for item '-2'
").VerifyDiagnostics()

            'Wrong IL
            verifier.VerifyIL("Program.VB$StateMachine_2_Call1(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      249 (0xf9)
  .maxstack  5
  .locals init (Integer V_0,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_1,
                System.Exception V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0064
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0011:  ldc.i4.1
    IL_0012:  constrained. "SM$T"
    IL_0018:  callvirt   "Function IMoveable.get_Position(Integer) As Integer()"
    IL_001d:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As System.Array"
    IL_0022:  ldarg.0
    IL_0023:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0028:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_002d:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_0032:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0037:  stloc.1
    IL_0038:  ldloca.s   V_1
    IL_003a:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_003f:  brtrue.s   IL_0080
    IL_0041:  ldarg.0
    IL_0042:  ldc.i4.0
    IL_0043:  dup
    IL_0044:  stloc.0
    IL_0045:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_004a:  ldarg.0
    IL_004b:  ldloc.1
    IL_004c:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0051:  ldarg.0
    IL_0052:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_0057:  ldloca.s   V_1
    IL_0059:  ldarg.0
    IL_005a:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_2_Call1(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_2_Call1(Of SM$T))"
    IL_005f:  leave      IL_00f8
    IL_0064:  ldarg.0
    IL_0065:  ldc.i4.m1
    IL_0066:  dup
    IL_0067:  stloc.0
    IL_0068:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_006d:  ldarg.0
    IL_006e:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0073:  stloc.1
    IL_0074:  ldarg.0
    IL_0075:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_007a:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0080:  ldarg.0
    IL_0081:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0086:  ldc.i4.1
    IL_0087:  ldarg.0
    IL_0088:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As System.Array"
    IL_008d:  ldloca.s   V_1
    IL_008f:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_0094:  ldloca.s   V_1
    IL_0096:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_009c:  ldc.i4.1
    IL_009d:  add.ovf
    IL_009e:  newarr     "Integer"
    IL_00a3:  call       "Function Microsoft.VisualBasic.CompilerServices.Utils.CopyArray(System.Array, System.Array) As System.Array"
    IL_00a8:  castclass  "Integer()"
    IL_00ad:  constrained. "SM$T"
    IL_00b3:  callvirt   "Sub IMoveable.set_Position(Integer, Integer())"
    IL_00b8:  ldarg.0
    IL_00b9:  ldnull
    IL_00ba:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As System.Array"
    IL_00bf:  leave.s    IL_00e3
  }
  catch System.Exception
  {
    IL_00c1:  dup
    IL_00c2:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_00c7:  stloc.2
    IL_00c8:  ldarg.0
    IL_00c9:  ldc.i4.s   -2
    IL_00cb:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_00d0:  ldarg.0
    IL_00d1:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00d6:  ldloc.2
    IL_00d7:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_00dc:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_00e1:  leave.s    IL_00f8
  }
  IL_00e3:  ldarg.0
  IL_00e4:  ldc.i4.s   -2
  IL_00e6:  dup
  IL_00e7:  stloc.0
  IL_00e8:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_00ed:  ldarg.0
  IL_00ee:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_00f3:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_00f8:  ret
}
]]>)

            'Wrong IL
            verifier.VerifyIL("Program.VB$StateMachine_3_Call2(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      249 (0xf9)
  .maxstack  5
  .locals init (Integer V_0,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_1,
                System.Exception V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0064
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_0011:  ldc.i4.1
    IL_0012:  constrained. "SM$T"
    IL_0018:  callvirt   "Function IMoveable.get_Position(Integer) As Integer()"
    IL_001d:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$U1 As System.Array"
    IL_0022:  ldarg.0
    IL_0023:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_0028:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_002d:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_0032:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0037:  stloc.1
    IL_0038:  ldloca.s   V_1
    IL_003a:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_003f:  brtrue.s   IL_0080
    IL_0041:  ldarg.0
    IL_0042:  ldc.i4.0
    IL_0043:  dup
    IL_0044:  stloc.0
    IL_0045:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_004a:  ldarg.0
    IL_004b:  ldloc.1
    IL_004c:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0051:  ldarg.0
    IL_0052:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_0057:  ldloca.s   V_1
    IL_0059:  ldarg.0
    IL_005a:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_3_Call2(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_3_Call2(Of SM$T))"
    IL_005f:  leave      IL_00f8
    IL_0064:  ldarg.0
    IL_0065:  ldc.i4.m1
    IL_0066:  dup
    IL_0067:  stloc.0
    IL_0068:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_006d:  ldarg.0
    IL_006e:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0073:  stloc.1
    IL_0074:  ldarg.0
    IL_0075:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_007a:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0080:  ldarg.0
    IL_0081:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_0086:  ldc.i4.1
    IL_0087:  ldarg.0
    IL_0088:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$U1 As System.Array"
    IL_008d:  ldloca.s   V_1
    IL_008f:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_0094:  ldloca.s   V_1
    IL_0096:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_009c:  ldc.i4.1
    IL_009d:  add.ovf
    IL_009e:  newarr     "Integer"
    IL_00a3:  call       "Function Microsoft.VisualBasic.CompilerServices.Utils.CopyArray(System.Array, System.Array) As System.Array"
    IL_00a8:  castclass  "Integer()"
    IL_00ad:  constrained. "SM$T"
    IL_00b3:  callvirt   "Sub IMoveable.set_Position(Integer, Integer())"
    IL_00b8:  ldarg.0
    IL_00b9:  ldnull
    IL_00ba:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$U1 As System.Array"
    IL_00bf:  leave.s    IL_00e3
  }
  catch System.Exception
  {
    IL_00c1:  dup
    IL_00c2:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_00c7:  stloc.2
    IL_00c8:  ldarg.0
    IL_00c9:  ldc.i4.s   -2
    IL_00cb:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_00d0:  ldarg.0
    IL_00d1:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00d6:  ldloc.2
    IL_00d7:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_00dc:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_00e1:  leave.s    IL_00f8
  }
  IL_00e3:  ldarg.0
  IL_00e4:  ldc.i4.s   -2
  IL_00e6:  dup
  IL_00e7:  stloc.0
  IL_00e8:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
  IL_00ed:  ldarg.0
  IL_00ee:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_00f3:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_00f8:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Redim_Indexer_Struct_Value_Async_01()
            Dim comp =
<compilation>
    <file>
Imports System
Imports System.Threading.Tasks

Interface IMoveable
    Property Position(x As Integer) As Integer()
End Interface

Structure Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer() Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return Nothing
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Structure

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1).Wait()

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2).Wait()
    End Sub

    Private Shared Async Function Call1(Of T As {Structure, IMoveable})(item As T) As Task
        Redim Preserve item.Position(1)(await GetOffsetAsync(GetOffset(item)))
    End Function

    Private Shared Async Function Call2(Of T As {IMoveable})(item As T) As Task
        Redim Preserve item.Position(1)(await GetOffsetAsync(GetOffset(item)))
    End Function

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return Nothing
    End Function

    Shared Function GetOffsetAsync(i As Integer) As Task(Of Integer)
        Return Task.FromResult(i)
    End Function
End Class
    </file>
</compilation>

            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '1'
Position set for item '-1'
Position get for item '2'
Position set for item '-2'
").VerifyDiagnostics()

            verifier.VerifyIL("Program.VB$StateMachine_2_Call1(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      249 (0xf9)
  .maxstack  5
  .locals init (Integer V_0,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_1,
                System.Exception V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0064
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0011:  ldc.i4.1
    IL_0012:  constrained. "SM$T"
    IL_0018:  callvirt   "Function IMoveable.get_Position(Integer) As Integer()"
    IL_001d:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As System.Array"
    IL_0022:  ldarg.0
    IL_0023:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0028:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_002d:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_0032:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0037:  stloc.1
    IL_0038:  ldloca.s   V_1
    IL_003a:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_003f:  brtrue.s   IL_0080
    IL_0041:  ldarg.0
    IL_0042:  ldc.i4.0
    IL_0043:  dup
    IL_0044:  stloc.0
    IL_0045:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_004a:  ldarg.0
    IL_004b:  ldloc.1
    IL_004c:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0051:  ldarg.0
    IL_0052:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_0057:  ldloca.s   V_1
    IL_0059:  ldarg.0
    IL_005a:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_2_Call1(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_2_Call1(Of SM$T))"
    IL_005f:  leave      IL_00f8
    IL_0064:  ldarg.0
    IL_0065:  ldc.i4.m1
    IL_0066:  dup
    IL_0067:  stloc.0
    IL_0068:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_006d:  ldarg.0
    IL_006e:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0073:  stloc.1
    IL_0074:  ldarg.0
    IL_0075:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_007a:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0080:  ldarg.0
    IL_0081:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0086:  ldc.i4.1
    IL_0087:  ldarg.0
    IL_0088:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As System.Array"
    IL_008d:  ldloca.s   V_1
    IL_008f:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_0094:  ldloca.s   V_1
    IL_0096:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_009c:  ldc.i4.1
    IL_009d:  add.ovf
    IL_009e:  newarr     "Integer"
    IL_00a3:  call       "Function Microsoft.VisualBasic.CompilerServices.Utils.CopyArray(System.Array, System.Array) As System.Array"
    IL_00a8:  castclass  "Integer()"
    IL_00ad:  constrained. "SM$T"
    IL_00b3:  callvirt   "Sub IMoveable.set_Position(Integer, Integer())"
    IL_00b8:  ldarg.0
    IL_00b9:  ldnull
    IL_00ba:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As System.Array"
    IL_00bf:  leave.s    IL_00e3
  }
  catch System.Exception
  {
    IL_00c1:  dup
    IL_00c2:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_00c7:  stloc.2
    IL_00c8:  ldarg.0
    IL_00c9:  ldc.i4.s   -2
    IL_00cb:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_00d0:  ldarg.0
    IL_00d1:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00d6:  ldloc.2
    IL_00d7:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_00dc:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_00e1:  leave.s    IL_00f8
  }
  IL_00e3:  ldarg.0
  IL_00e4:  ldc.i4.s   -2
  IL_00e6:  dup
  IL_00e7:  stloc.0
  IL_00e8:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_00ed:  ldarg.0
  IL_00ee:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_00f3:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_00f8:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Redim_Indexer_Class_IndexAndValue()
            Dim comp =
<compilation>
    <file>
Imports System

Interface IMoveable
    Property Position(x As Integer) As Integer()
End Interface

Class Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer() Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return Nothing
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Class

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1)

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2)
    End Sub

    Private Shared Sub Call1(Of T As {Class, IMoveable})(item As T)
        Redim Preserve item.Position(GetOffset(item))(GetOffset(item))
    End Sub

    Private Shared Sub Call2(Of T As {IMoveable})(item As T)
        Redim Preserve item.Position(GetOffset(item))(GetOffset(item))
    End Sub

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return Nothing
    End Function
End Class
    </file>
</compilation>

            ' Wrong output and differs, but still wrong, on some frameworks 
            '            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
            '"
            'Position get for item '-1'
            'Position set for item '-1'
            'Position get for item '-3'
            'Position set for item '-3'
            '").VerifyDiagnostics()
            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe).VerifyDiagnostics()

            'Wrong IL
            verifier.VerifyIL("Program.Call1(Of T)",
            <![CDATA[
{
  // Code size       61 (0x3d)
  .maxstack  5
  .locals init (Integer V_0)
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldarga.s   V_0
  IL_0004:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0009:  dup
  IL_000a:  stloc.0
  IL_000b:  ldarga.s   V_0
  IL_000d:  ldloc.0
  IL_000e:  constrained. "T"
  IL_0014:  callvirt   "Function IMoveable.get_Position(Integer) As Integer()"
  IL_0019:  ldarga.s   V_0
  IL_001b:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0020:  ldc.i4.1
  IL_0021:  add.ovf
  IL_0022:  newarr     "Integer"
  IL_0027:  call       "Function Microsoft.VisualBasic.CompilerServices.Utils.CopyArray(System.Array, System.Array) As System.Array"
  IL_002c:  castclass  "Integer()"
  IL_0031:  constrained. "T"
  IL_0037:  callvirt   "Sub IMoveable.set_Position(Integer, Integer())"
  IL_003c:  ret
}
]]>)

            'Wrong IL
            verifier.VerifyIL("Program.Call2(Of T)",
            <![CDATA[
{
  // Code size       61 (0x3d)
  .maxstack  5
  .locals init (Integer V_0)
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldarga.s   V_0
  IL_0004:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0009:  dup
  IL_000a:  stloc.0
  IL_000b:  ldarga.s   V_0
  IL_000d:  ldloc.0
  IL_000e:  constrained. "T"
  IL_0014:  callvirt   "Function IMoveable.get_Position(Integer) As Integer()"
  IL_0019:  ldarga.s   V_0
  IL_001b:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0020:  ldc.i4.1
  IL_0021:  add.ovf
  IL_0022:  newarr     "Integer"
  IL_0027:  call       "Function Microsoft.VisualBasic.CompilerServices.Utils.CopyArray(System.Array, System.Array) As System.Array"
  IL_002c:  castclass  "Integer()"
  IL_0031:  constrained. "T"
  IL_0037:  callvirt   "Sub IMoveable.set_Position(Integer, Integer())"
  IL_003c:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Redim_Indexer_Struct_IndexAndValue()
            Dim comp =
<compilation>
    <file>
Imports System

Interface IMoveable
    Property Position(x As Integer) As Integer()
End Interface

Structure Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer() Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return Nothing
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Structure

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1)

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2)
    End Sub

    Private Shared Sub Call1(Of T As {Structure, IMoveable})(item As T)
        Redim Preserve item.Position(GetOffset(item))(GetOffset(item))
    End Sub

    Private Shared Sub Call2(Of T As {IMoveable})(item As T)
        Redim Preserve item.Position(GetOffset(item))(GetOffset(item))
    End Sub

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return Nothing
    End Function
End Class
    </file>
</compilation>

            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '-1'
Position set for item '-2'
Position get for item '-3'
Position set for item '-4'
").VerifyDiagnostics()

            verifier.VerifyIL("Program.Call1(Of T)",
            <![CDATA[
{
  // Code size       61 (0x3d)
  .maxstack  5
  .locals init (Integer V_0)
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldarga.s   V_0
  IL_0004:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0009:  dup
  IL_000a:  stloc.0
  IL_000b:  ldarga.s   V_0
  IL_000d:  ldloc.0
  IL_000e:  constrained. "T"
  IL_0014:  callvirt   "Function IMoveable.get_Position(Integer) As Integer()"
  IL_0019:  ldarga.s   V_0
  IL_001b:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0020:  ldc.i4.1
  IL_0021:  add.ovf
  IL_0022:  newarr     "Integer"
  IL_0027:  call       "Function Microsoft.VisualBasic.CompilerServices.Utils.CopyArray(System.Array, System.Array) As System.Array"
  IL_002c:  castclass  "Integer()"
  IL_0031:  constrained. "T"
  IL_0037:  callvirt   "Sub IMoveable.set_Position(Integer, Integer())"
  IL_003c:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Redim_Indexer_Class_IndexAndValue_Ref()
            Dim comp =
<compilation>
    <file>
Imports System

Interface IMoveable
    Property Position(x As Integer) As Integer()
End Interface

Class Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer() Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return Nothing
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Class

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1)

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2)
    End Sub

    Private Shared Sub Call1(Of T As {Class, IMoveable})(ByRef item As T)
        Redim Preserve item.Position(GetOffset(item))(GetOffset(item))
    End Sub

    Private Shared Sub Call2(Of T As {IMoveable})(ByRef item As T)
        Redim Preserve item.Position(GetOffset(item))(GetOffset(item))
    End Sub

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return Nothing
    End Function
End Class
    </file>
</compilation>

            ' Wrong output and framework dependent
            '            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
            '"
            'Position get for item '-1'
            'Position set for item '-1'
            'Position get for item '-3'
            'Position set for item '-3'
            '").VerifyDiagnostics()
            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe).VerifyDiagnostics()

            'Wrong IL
            verifier.VerifyIL("Program.Call1(Of T)",
            <![CDATA[
{
  // Code size       57 (0x39)
  .maxstack  5
  .locals init (Integer V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0007:  dup
  IL_0008:  stloc.0
  IL_0009:  ldarg.0
  IL_000a:  ldloc.0
  IL_000b:  constrained. "T"
  IL_0011:  callvirt   "Function IMoveable.get_Position(Integer) As Integer()"
  IL_0016:  ldarg.0
  IL_0017:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_001c:  ldc.i4.1
  IL_001d:  add.ovf
  IL_001e:  newarr     "Integer"
  IL_0023:  call       "Function Microsoft.VisualBasic.CompilerServices.Utils.CopyArray(System.Array, System.Array) As System.Array"
  IL_0028:  castclass  "Integer()"
  IL_002d:  constrained. "T"
  IL_0033:  callvirt   "Sub IMoveable.set_Position(Integer, Integer())"
  IL_0038:  ret
}
]]>)

            'Wrong IL
            verifier.VerifyIL("Program.Call2(Of T)",
            <![CDATA[
{
  // Code size       57 (0x39)
  .maxstack  5
  .locals init (Integer V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0007:  dup
  IL_0008:  stloc.0
  IL_0009:  ldarg.0
  IL_000a:  ldloc.0
  IL_000b:  constrained. "T"
  IL_0011:  callvirt   "Function IMoveable.get_Position(Integer) As Integer()"
  IL_0016:  ldarg.0
  IL_0017:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_001c:  ldc.i4.1
  IL_001d:  add.ovf
  IL_001e:  newarr     "Integer"
  IL_0023:  call       "Function Microsoft.VisualBasic.CompilerServices.Utils.CopyArray(System.Array, System.Array) As System.Array"
  IL_0028:  castclass  "Integer()"
  IL_002d:  constrained. "T"
  IL_0033:  callvirt   "Sub IMoveable.set_Position(Integer, Integer())"
  IL_0038:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Redim_Indexer_Struct_IndexAndValue_Ref()
            Dim comp =
<compilation>
    <file>
Imports System

Interface IMoveable
    Property Position(x As Integer) As Integer()
End Interface

Structure Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer() Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return Nothing
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Structure

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1)

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2)
    End Sub

    Private Shared Sub Call1(Of T As {Structure, IMoveable})(ByRef item As T)
        Redim Preserve item.Position(GetOffset(item))(GetOffset(item))
    End Sub

    Private Shared Sub Call2(Of T As {IMoveable})(ByRef item As T)
        Redim Preserve item.Position(GetOffset(item))(GetOffset(item))
    End Sub

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return Nothing
    End Function
End Class
    </file>
</compilation>

            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '-1'
Position set for item '-2'
Position get for item '-3'
Position set for item '-4'
").VerifyDiagnostics()

            verifier.VerifyIL("Program.Call1(Of T)",
            <![CDATA[
{
  // Code size       57 (0x39)
  .maxstack  5
  .locals init (Integer V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0007:  dup
  IL_0008:  stloc.0
  IL_0009:  ldarg.0
  IL_000a:  ldloc.0
  IL_000b:  constrained. "T"
  IL_0011:  callvirt   "Function IMoveable.get_Position(Integer) As Integer()"
  IL_0016:  ldarg.0
  IL_0017:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_001c:  ldc.i4.1
  IL_001d:  add.ovf
  IL_001e:  newarr     "Integer"
  IL_0023:  call       "Function Microsoft.VisualBasic.CompilerServices.Utils.CopyArray(System.Array, System.Array) As System.Array"
  IL_0028:  castclass  "Integer()"
  IL_002d:  constrained. "T"
  IL_0033:  callvirt   "Sub IMoveable.set_Position(Integer, Integer())"
  IL_0038:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Redim_Indexer_Class_IndexAndValue_Async_01()
            Dim comp =
<compilation>
    <file>
Imports System
Imports System.Threading.Tasks

Interface IMoveable
    Property Position(x As Integer) As Integer()
End Interface

Class Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer() Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return Nothing
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Class

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1).Wait()

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2).Wait()
    End Sub

    Private Shared Async Function Call1(Of T As {Class, IMoveable})(item As T) As Task
        Redim Preserve item.Position(GetOffset(item))(await GetOffsetAsync(GetOffset(item)))
    End Function

    Private Shared Async Function Call2(Of T As {IMoveable})(item As T) As Task
        Redim Preserve item.Position(GetOffset(item))(await GetOffsetAsync(GetOffset(item)))
    End Function

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return Nothing
    End Function

    Shared Function GetOffsetAsync(i As Integer) As Task(Of Integer)
        Return Task.FromResult(i)
    End Function
End Class
    </file>
</compilation>

            ' Wrong output
            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '-1'
Position set for item '-2'
Position get for item '-3'
Position set for item '-4'
").VerifyDiagnostics()

            'Wrong IL
            verifier.VerifyIL("Program.VB$StateMachine_2_Call1(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      285 (0x11d)
  .maxstack  5
  .locals init (Integer V_0,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_1,
                Integer V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0083
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldarg.0
    IL_000d:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0012:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_0017:  dup
    IL_0018:  stloc.2
    IL_0019:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$S0 As Integer"
    IL_001e:  ldloc.2
    IL_001f:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U2 As Integer"
    IL_0024:  ldarg.0
    IL_0025:  ldarg.0
    IL_0026:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_002b:  ldarg.0
    IL_002c:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$S0 As Integer"
    IL_0031:  constrained. "SM$T"
    IL_0037:  callvirt   "Function IMoveable.get_Position(Integer) As Integer()"
    IL_003c:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As System.Array"
    IL_0041:  ldarg.0
    IL_0042:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0047:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_004c:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_0051:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0056:  stloc.1
    IL_0057:  ldloca.s   V_1
    IL_0059:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_005e:  brtrue.s   IL_009f
    IL_0060:  ldarg.0
    IL_0061:  ldc.i4.0
    IL_0062:  dup
    IL_0063:  stloc.0
    IL_0064:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0069:  ldarg.0
    IL_006a:  ldloc.1
    IL_006b:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0070:  ldarg.0
    IL_0071:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_0076:  ldloca.s   V_1
    IL_0078:  ldarg.0
    IL_0079:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_2_Call1(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_2_Call1(Of SM$T))"
    IL_007e:  leave      IL_011c
    IL_0083:  ldarg.0
    IL_0084:  ldc.i4.m1
    IL_0085:  dup
    IL_0086:  stloc.0
    IL_0087:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_008c:  ldarg.0
    IL_008d:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0092:  stloc.1
    IL_0093:  ldarg.0
    IL_0094:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0099:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_009f:  ldarg.0
    IL_00a0:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_00a5:  ldarg.0
    IL_00a6:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U2 As Integer"
    IL_00ab:  ldarg.0
    IL_00ac:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As System.Array"
    IL_00b1:  ldloca.s   V_1
    IL_00b3:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_00b8:  ldloca.s   V_1
    IL_00ba:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00c0:  ldc.i4.1
    IL_00c1:  add.ovf
    IL_00c2:  newarr     "Integer"
    IL_00c7:  call       "Function Microsoft.VisualBasic.CompilerServices.Utils.CopyArray(System.Array, System.Array) As System.Array"
    IL_00cc:  castclass  "Integer()"
    IL_00d1:  constrained. "SM$T"
    IL_00d7:  callvirt   "Sub IMoveable.set_Position(Integer, Integer())"
    IL_00dc:  ldarg.0
    IL_00dd:  ldnull
    IL_00de:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As System.Array"
    IL_00e3:  leave.s    IL_0107
  }
  catch System.Exception
  {
    IL_00e5:  dup
    IL_00e6:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_00eb:  stloc.3
    IL_00ec:  ldarg.0
    IL_00ed:  ldc.i4.s   -2
    IL_00ef:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_00f4:  ldarg.0
    IL_00f5:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00fa:  ldloc.3
    IL_00fb:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_0100:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_0105:  leave.s    IL_011c
  }
  IL_0107:  ldarg.0
  IL_0108:  ldc.i4.s   -2
  IL_010a:  dup
  IL_010b:  stloc.0
  IL_010c:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_0111:  ldarg.0
  IL_0112:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_0117:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_011c:  ret
}
]]>)

            'Wrong IL
            verifier.VerifyIL("Program.VB$StateMachine_3_Call2(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      285 (0x11d)
  .maxstack  5
  .locals init (Integer V_0,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_1,
                Integer V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0083
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldarg.0
    IL_000d:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_0012:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_0017:  dup
    IL_0018:  stloc.2
    IL_0019:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$S0 As Integer"
    IL_001e:  ldloc.2
    IL_001f:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$U2 As Integer"
    IL_0024:  ldarg.0
    IL_0025:  ldarg.0
    IL_0026:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_002b:  ldarg.0
    IL_002c:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$S0 As Integer"
    IL_0031:  constrained. "SM$T"
    IL_0037:  callvirt   "Function IMoveable.get_Position(Integer) As Integer()"
    IL_003c:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$U1 As System.Array"
    IL_0041:  ldarg.0
    IL_0042:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_0047:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_004c:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_0051:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0056:  stloc.1
    IL_0057:  ldloca.s   V_1
    IL_0059:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_005e:  brtrue.s   IL_009f
    IL_0060:  ldarg.0
    IL_0061:  ldc.i4.0
    IL_0062:  dup
    IL_0063:  stloc.0
    IL_0064:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_0069:  ldarg.0
    IL_006a:  ldloc.1
    IL_006b:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0070:  ldarg.0
    IL_0071:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_0076:  ldloca.s   V_1
    IL_0078:  ldarg.0
    IL_0079:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_3_Call2(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_3_Call2(Of SM$T))"
    IL_007e:  leave      IL_011c
    IL_0083:  ldarg.0
    IL_0084:  ldc.i4.m1
    IL_0085:  dup
    IL_0086:  stloc.0
    IL_0087:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_008c:  ldarg.0
    IL_008d:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0092:  stloc.1
    IL_0093:  ldarg.0
    IL_0094:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0099:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_009f:  ldarg.0
    IL_00a0:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_00a5:  ldarg.0
    IL_00a6:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$U2 As Integer"
    IL_00ab:  ldarg.0
    IL_00ac:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$U1 As System.Array"
    IL_00b1:  ldloca.s   V_1
    IL_00b3:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_00b8:  ldloca.s   V_1
    IL_00ba:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00c0:  ldc.i4.1
    IL_00c1:  add.ovf
    IL_00c2:  newarr     "Integer"
    IL_00c7:  call       "Function Microsoft.VisualBasic.CompilerServices.Utils.CopyArray(System.Array, System.Array) As System.Array"
    IL_00cc:  castclass  "Integer()"
    IL_00d1:  constrained. "SM$T"
    IL_00d7:  callvirt   "Sub IMoveable.set_Position(Integer, Integer())"
    IL_00dc:  ldarg.0
    IL_00dd:  ldnull
    IL_00de:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$U1 As System.Array"
    IL_00e3:  leave.s    IL_0107
  }
  catch System.Exception
  {
    IL_00e5:  dup
    IL_00e6:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_00eb:  stloc.3
    IL_00ec:  ldarg.0
    IL_00ed:  ldc.i4.s   -2
    IL_00ef:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_00f4:  ldarg.0
    IL_00f5:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00fa:  ldloc.3
    IL_00fb:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_0100:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_0105:  leave.s    IL_011c
  }
  IL_0107:  ldarg.0
  IL_0108:  ldc.i4.s   -2
  IL_010a:  dup
  IL_010b:  stloc.0
  IL_010c:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
  IL_0111:  ldarg.0
  IL_0112:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_0117:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_011c:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Redim_Indexer_Struct_IndexAndValue_Async_01()
            Dim comp =
<compilation>
    <file>
Imports System
Imports System.Threading.Tasks

Interface IMoveable
    Property Position(x As Integer) As Integer()
End Interface

Structure Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer() Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return Nothing
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Structure

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1).Wait()

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2).Wait()
    End Sub

    Private Shared Async Function Call1(Of T As {Structure, IMoveable})(item As T) As Task
        Redim Preserve item.Position(GetOffset(item))(await GetOffsetAsync(GetOffset(item)))
    End Function

    Private Shared Async Function Call2(Of T As {IMoveable})(item As T) As Task
        Redim Preserve item.Position(GetOffset(item))(await GetOffsetAsync(GetOffset(item)))
    End Function

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return Nothing
    End Function

    Shared Function GetOffsetAsync(i As Integer) As Task(Of Integer)
        Return Task.FromResult(i)
    End Function
End Class
    </file>
</compilation>

            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '-1'
Position set for item '-2'
Position get for item '-3'
Position set for item '-4'
").VerifyDiagnostics()

            verifier.VerifyIL("Program.VB$StateMachine_2_Call1(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      285 (0x11d)
  .maxstack  5
  .locals init (Integer V_0,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_1,
                Integer V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0083
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldarg.0
    IL_000d:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0012:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_0017:  dup
    IL_0018:  stloc.2
    IL_0019:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$S0 As Integer"
    IL_001e:  ldloc.2
    IL_001f:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U2 As Integer"
    IL_0024:  ldarg.0
    IL_0025:  ldarg.0
    IL_0026:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_002b:  ldarg.0
    IL_002c:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$S0 As Integer"
    IL_0031:  constrained. "SM$T"
    IL_0037:  callvirt   "Function IMoveable.get_Position(Integer) As Integer()"
    IL_003c:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As System.Array"
    IL_0041:  ldarg.0
    IL_0042:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0047:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_004c:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_0051:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0056:  stloc.1
    IL_0057:  ldloca.s   V_1
    IL_0059:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_005e:  brtrue.s   IL_009f
    IL_0060:  ldarg.0
    IL_0061:  ldc.i4.0
    IL_0062:  dup
    IL_0063:  stloc.0
    IL_0064:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0069:  ldarg.0
    IL_006a:  ldloc.1
    IL_006b:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0070:  ldarg.0
    IL_0071:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_0076:  ldloca.s   V_1
    IL_0078:  ldarg.0
    IL_0079:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_2_Call1(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_2_Call1(Of SM$T))"
    IL_007e:  leave      IL_011c
    IL_0083:  ldarg.0
    IL_0084:  ldc.i4.m1
    IL_0085:  dup
    IL_0086:  stloc.0
    IL_0087:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_008c:  ldarg.0
    IL_008d:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0092:  stloc.1
    IL_0093:  ldarg.0
    IL_0094:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0099:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_009f:  ldarg.0
    IL_00a0:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_00a5:  ldarg.0
    IL_00a6:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U2 As Integer"
    IL_00ab:  ldarg.0
    IL_00ac:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As System.Array"
    IL_00b1:  ldloca.s   V_1
    IL_00b3:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_00b8:  ldloca.s   V_1
    IL_00ba:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00c0:  ldc.i4.1
    IL_00c1:  add.ovf
    IL_00c2:  newarr     "Integer"
    IL_00c7:  call       "Function Microsoft.VisualBasic.CompilerServices.Utils.CopyArray(System.Array, System.Array) As System.Array"
    IL_00cc:  castclass  "Integer()"
    IL_00d1:  constrained. "SM$T"
    IL_00d7:  callvirt   "Sub IMoveable.set_Position(Integer, Integer())"
    IL_00dc:  ldarg.0
    IL_00dd:  ldnull
    IL_00de:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As System.Array"
    IL_00e3:  leave.s    IL_0107
  }
  catch System.Exception
  {
    IL_00e5:  dup
    IL_00e6:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_00eb:  stloc.3
    IL_00ec:  ldarg.0
    IL_00ed:  ldc.i4.s   -2
    IL_00ef:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_00f4:  ldarg.0
    IL_00f5:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00fa:  ldloc.3
    IL_00fb:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_0100:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_0105:  leave.s    IL_011c
  }
  IL_0107:  ldarg.0
  IL_0108:  ldc.i4.s   -2
  IL_010a:  dup
  IL_010b:  stloc.0
  IL_010c:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_0111:  ldarg.0
  IL_0112:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_0117:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_011c:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Redim_Indexer_Class_IndexAndValue_Async_03()
            Dim comp =
<compilation>
    <file>
Imports System
Imports System.Threading.Tasks

Interface IMoveable
    Property Position(x As Integer) As Integer()
End Interface

Class Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer() Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return Nothing
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Class

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1).Wait()

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2).Wait()
    End Sub

    Private Shared Async Function Call1(Of T As {Class, IMoveable})(item As T) As Task
        Redim Preserve item.Position(await GetOffsetAsync(GetOffset(item)))(GetOffset(item))
    End Function

    Private Shared Async Function Call2(Of T As {IMoveable})(item As T) As Task
        Redim Preserve item.Position(await GetOffsetAsync(GetOffset(item)))(GetOffset(item))
    End Function

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return Nothing
    End Function

    Shared Function GetOffsetAsync(i As Integer) As Task(Of Integer)
        Return Task.FromResult(i)
    End Function
End Class
    </file>
</compilation>

            ' Wrong output and framework dependent 
            '            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
            '"
            'Position get for item '-1'
            'Position set for item '-1'
            'Position get for item '-3'
            'Position set for item '-3'
            '").VerifyDiagnostics()
            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe).VerifyDiagnostics()

            'Wrong IL
            verifier.VerifyIL("Program.VB$StateMachine_2_Call1(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      242 (0xf2)
  .maxstack  5
  .locals init (Integer V_0,
                Integer V_1,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004c
    IL_000a:  ldarg.0
    IL_000b:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0010:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_0015:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_001a:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_001f:  stloc.2
    IL_0020:  ldloca.s   V_2
    IL_0022:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_0027:  brtrue.s   IL_0068
    IL_0029:  ldarg.0
    IL_002a:  ldc.i4.0
    IL_002b:  dup
    IL_002c:  stloc.0
    IL_002d:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0032:  ldarg.0
    IL_0033:  ldloc.2
    IL_0034:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0039:  ldarg.0
    IL_003a:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_003f:  ldloca.s   V_2
    IL_0041:  ldarg.0
    IL_0042:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_2_Call1(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_2_Call1(Of SM$T))"
    IL_0047:  leave      IL_00f1
    IL_004c:  ldarg.0
    IL_004d:  ldc.i4.m1
    IL_004e:  dup
    IL_004f:  stloc.0
    IL_0050:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0055:  ldarg.0
    IL_0056:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_005b:  stloc.2
    IL_005c:  ldarg.0
    IL_005d:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0062:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0068:  ldarg.0
    IL_0069:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_006e:  ldloca.s   V_2
    IL_0070:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_0075:  ldloca.s   V_2
    IL_0077:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_007d:  dup
    IL_007e:  stloc.1
    IL_007f:  ldarg.0
    IL_0080:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0085:  ldloc.1
    IL_0086:  constrained. "SM$T"
    IL_008c:  callvirt   "Function IMoveable.get_Position(Integer) As Integer()"
    IL_0091:  ldarg.0
    IL_0092:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0097:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_009c:  ldc.i4.1
    IL_009d:  add.ovf
    IL_009e:  newarr     "Integer"
    IL_00a3:  call       "Function Microsoft.VisualBasic.CompilerServices.Utils.CopyArray(System.Array, System.Array) As System.Array"
    IL_00a8:  castclass  "Integer()"
    IL_00ad:  constrained. "SM$T"
    IL_00b3:  callvirt   "Sub IMoveable.set_Position(Integer, Integer())"
    IL_00b8:  leave.s    IL_00dc
  }
  catch System.Exception
  {
    IL_00ba:  dup
    IL_00bb:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_00c0:  stloc.3
    IL_00c1:  ldarg.0
    IL_00c2:  ldc.i4.s   -2
    IL_00c4:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_00c9:  ldarg.0
    IL_00ca:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00cf:  ldloc.3
    IL_00d0:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_00d5:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_00da:  leave.s    IL_00f1
  }
  IL_00dc:  ldarg.0
  IL_00dd:  ldc.i4.s   -2
  IL_00df:  dup
  IL_00e0:  stloc.0
  IL_00e1:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_00e6:  ldarg.0
  IL_00e7:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_00ec:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_00f1:  ret
}
]]>)

            'Wrong IL
            verifier.VerifyIL("Program.VB$StateMachine_3_Call2(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      242 (0xf2)
  .maxstack  5
  .locals init (Integer V_0,
                Integer V_1,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004c
    IL_000a:  ldarg.0
    IL_000b:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_0010:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_0015:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_001a:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_001f:  stloc.2
    IL_0020:  ldloca.s   V_2
    IL_0022:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_0027:  brtrue.s   IL_0068
    IL_0029:  ldarg.0
    IL_002a:  ldc.i4.0
    IL_002b:  dup
    IL_002c:  stloc.0
    IL_002d:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_0032:  ldarg.0
    IL_0033:  ldloc.2
    IL_0034:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0039:  ldarg.0
    IL_003a:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_003f:  ldloca.s   V_2
    IL_0041:  ldarg.0
    IL_0042:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_3_Call2(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_3_Call2(Of SM$T))"
    IL_0047:  leave      IL_00f1
    IL_004c:  ldarg.0
    IL_004d:  ldc.i4.m1
    IL_004e:  dup
    IL_004f:  stloc.0
    IL_0050:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_0055:  ldarg.0
    IL_0056:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_005b:  stloc.2
    IL_005c:  ldarg.0
    IL_005d:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0062:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0068:  ldarg.0
    IL_0069:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_006e:  ldloca.s   V_2
    IL_0070:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_0075:  ldloca.s   V_2
    IL_0077:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_007d:  dup
    IL_007e:  stloc.1
    IL_007f:  ldarg.0
    IL_0080:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_0085:  ldloc.1
    IL_0086:  constrained. "SM$T"
    IL_008c:  callvirt   "Function IMoveable.get_Position(Integer) As Integer()"
    IL_0091:  ldarg.0
    IL_0092:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_0097:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_009c:  ldc.i4.1
    IL_009d:  add.ovf
    IL_009e:  newarr     "Integer"
    IL_00a3:  call       "Function Microsoft.VisualBasic.CompilerServices.Utils.CopyArray(System.Array, System.Array) As System.Array"
    IL_00a8:  castclass  "Integer()"
    IL_00ad:  constrained. "SM$T"
    IL_00b3:  callvirt   "Sub IMoveable.set_Position(Integer, Integer())"
    IL_00b8:  leave.s    IL_00dc
  }
  catch System.Exception
  {
    IL_00ba:  dup
    IL_00bb:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_00c0:  stloc.3
    IL_00c1:  ldarg.0
    IL_00c2:  ldc.i4.s   -2
    IL_00c4:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_00c9:  ldarg.0
    IL_00ca:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00cf:  ldloc.3
    IL_00d0:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_00d5:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_00da:  leave.s    IL_00f1
  }
  IL_00dc:  ldarg.0
  IL_00dd:  ldc.i4.s   -2
  IL_00df:  dup
  IL_00e0:  stloc.0
  IL_00e1:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
  IL_00e6:  ldarg.0
  IL_00e7:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_00ec:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_00f1:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Redim_Indexer_Struct_IndexAndValue_Async_03()
            Dim comp =
<compilation>
    <file>
Imports System
Imports System.Threading.Tasks

Interface IMoveable
    Property Position(x As Integer) As Integer()
End Interface

Structure Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer() Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return Nothing
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Structure

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1).Wait()

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2).Wait()
    End Sub

    Private Shared Async Function Call1(Of T As {Structure, IMoveable})(item As T) As Task
        Redim Preserve item.Position(await GetOffsetAsync(GetOffset(item)))(GetOffset(item))
    End Function

    Private Shared Async Function Call2(Of T As {IMoveable})(item As T) As Task
        Redim Preserve item.Position(await GetOffsetAsync(GetOffset(item)))(GetOffset(item))
    End Function

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return Nothing
    End Function

    Shared Function GetOffsetAsync(i As Integer) As Task(Of Integer)
        Return Task.FromResult(i)
    End Function
End Class
    </file>
</compilation>

            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '-1'
Position set for item '-2'
Position get for item '-3'
Position set for item '-4'
").VerifyDiagnostics()

            verifier.VerifyIL("Program.VB$StateMachine_2_Call1(Of SM$T).MoveNext()",
            <![CDATA[
{
    // Code size      242 (0xf2)
    .maxstack  5
    .locals init (Integer V_0,
                Integer V_1,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_2,
                System.Exception V_3)
    IL_0000:  ldarg.0
    IL_0001:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0006:  stloc.0
    .try
    {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004c
    IL_000a:  ldarg.0
    IL_000b:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0010:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_0015:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_001a:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_001f:  stloc.2
    IL_0020:  ldloca.s   V_2
    IL_0022:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_0027:  brtrue.s   IL_0068
    IL_0029:  ldarg.0
    IL_002a:  ldc.i4.0
    IL_002b:  dup
    IL_002c:  stloc.0
    IL_002d:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0032:  ldarg.0
    IL_0033:  ldloc.2
    IL_0034:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0039:  ldarg.0
    IL_003a:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_003f:  ldloca.s   V_2
    IL_0041:  ldarg.0
    IL_0042:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_2_Call1(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_2_Call1(Of SM$T))"
    IL_0047:  leave      IL_00f1
    IL_004c:  ldarg.0
    IL_004d:  ldc.i4.m1
    IL_004e:  dup
    IL_004f:  stloc.0
    IL_0050:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0055:  ldarg.0
    IL_0056:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_005b:  stloc.2
    IL_005c:  ldarg.0
    IL_005d:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0062:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0068:  ldarg.0
    IL_0069:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_006e:  ldloca.s   V_2
    IL_0070:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_0075:  ldloca.s   V_2
    IL_0077:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_007d:  dup
    IL_007e:  stloc.1
    IL_007f:  ldarg.0
    IL_0080:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0085:  ldloc.1
    IL_0086:  constrained. "SM$T"
    IL_008c:  callvirt   "Function IMoveable.get_Position(Integer) As Integer()"
    IL_0091:  ldarg.0
    IL_0092:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0097:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_009c:  ldc.i4.1
    IL_009d:  add.ovf
    IL_009e:  newarr     "Integer"
    IL_00a3:  call       "Function Microsoft.VisualBasic.CompilerServices.Utils.CopyArray(System.Array, System.Array) As System.Array"
    IL_00a8:  castclass  "Integer()"
    IL_00ad:  constrained. "SM$T"
    IL_00b3:  callvirt   "Sub IMoveable.set_Position(Integer, Integer())"
    IL_00b8:  leave.s    IL_00dc
    }
    catch System.Exception
    {
    IL_00ba:  dup
    IL_00bb:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_00c0:  stloc.3
    IL_00c1:  ldarg.0
    IL_00c2:  ldc.i4.s   -2
    IL_00c4:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_00c9:  ldarg.0
    IL_00ca:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00cf:  ldloc.3
    IL_00d0:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_00d5:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_00da:  leave.s    IL_00f1
    }
    IL_00dc:  ldarg.0
    IL_00dd:  ldc.i4.s   -2
    IL_00df:  dup
    IL_00e0:  stloc.0
    IL_00e1:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_00e6:  ldarg.0
    IL_00e7:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00ec:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
    IL_00f1:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Redim_Indexer_Class_IndexAndValue_Async_04()
            Dim comp =
<compilation>
    <file>
Imports System
Imports System.Threading.Tasks

Interface IMoveable
    Property Position(x As Integer) As Integer()
End Interface

Class Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer() Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return Nothing
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Class

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1).Wait()

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2).Wait()
    End Sub

    Private Shared Async Function Call1(Of T As {Class, IMoveable})(item As T) As Task
        Redim Preserve item.Position(await GetOffsetAsync(GetOffset(item)))(await GetOffsetAsync(GetOffset(item)))
    End Function

    Private Shared Async Function Call2(Of T As {IMoveable})(item As T) As Task
        Redim Preserve item.Position(await GetOffsetAsync(GetOffset(item)))(await GetOffsetAsync(GetOffset(item)))
    End Function

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return Nothing
    End Function

    Shared Function GetOffsetAsync(i As Integer) As Task(Of Integer)
        Return Task.FromResult(i)
    End Function
End Class
    </file>
</compilation>

            ' Wrong output
            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '-1'
Position set for item '-2'
Position get for item '-3'
Position set for item '-4'
").VerifyDiagnostics()

            'Wrong IL
            verifier.VerifyIL("Program.VB$StateMachine_2_Call1(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      392 (0x188)
  .maxstack  5
  .locals init (Integer V_0,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_1,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_2,
                Integer V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0053
    IL_000a:  ldloc.0
    IL_000b:  ldc.i4.1
    IL_000c:  beq        IL_00ec
    IL_0011:  ldarg.0
    IL_0012:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0017:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_001c:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_0021:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0026:  stloc.1
    IL_0027:  ldloca.s   V_1
    IL_0029:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_002e:  brtrue.s   IL_006f
    IL_0030:  ldarg.0
    IL_0031:  ldc.i4.0
    IL_0032:  dup
    IL_0033:  stloc.0
    IL_0034:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0039:  ldarg.0
    IL_003a:  ldloc.1
    IL_003b:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0040:  ldarg.0
    IL_0041:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_0046:  ldloca.s   V_1
    IL_0048:  ldarg.0
    IL_0049:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_2_Call1(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_2_Call1(Of SM$T))"
    IL_004e:  leave      IL_0187
    IL_0053:  ldarg.0
    IL_0054:  ldc.i4.m1
    IL_0055:  dup
    IL_0056:  stloc.0
    IL_0057:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_005c:  ldarg.0
    IL_005d:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0062:  stloc.1
    IL_0063:  ldarg.0
    IL_0064:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0069:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_006f:  ldarg.0
    IL_0070:  ldarg.0
    IL_0071:  ldloca.s   V_1
    IL_0073:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_0078:  ldloca.s   V_1
    IL_007a:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0080:  dup
    IL_0081:  stloc.3
    IL_0082:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$S0 As Integer"
    IL_0087:  ldloc.3
    IL_0088:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U2 As Integer"
    IL_008d:  ldarg.0
    IL_008e:  ldarg.0
    IL_008f:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0094:  ldarg.0
    IL_0095:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$S0 As Integer"
    IL_009a:  constrained. "SM$T"
    IL_00a0:  callvirt   "Function IMoveable.get_Position(Integer) As Integer()"
    IL_00a5:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As System.Array"
    IL_00aa:  ldarg.0
    IL_00ab:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_00b0:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_00b5:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_00ba:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00bf:  stloc.2
    IL_00c0:  ldloca.s   V_2
    IL_00c2:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_00c7:  brtrue.s   IL_0108
    IL_00c9:  ldarg.0
    IL_00ca:  ldc.i4.1
    IL_00cb:  dup
    IL_00cc:  stloc.0
    IL_00cd:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_00d2:  ldarg.0
    IL_00d3:  ldloc.2
    IL_00d4:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00d9:  ldarg.0
    IL_00da:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00df:  ldloca.s   V_2
    IL_00e1:  ldarg.0
    IL_00e2:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_2_Call1(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_2_Call1(Of SM$T))"
    IL_00e7:  leave      IL_0187
    IL_00ec:  ldarg.0
    IL_00ed:  ldc.i4.m1
    IL_00ee:  dup
    IL_00ef:  stloc.0
    IL_00f0:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_00f5:  ldarg.0
    IL_00f6:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00fb:  stloc.2
    IL_00fc:  ldarg.0
    IL_00fd:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0102:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0108:  ldarg.0
    IL_0109:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_010e:  ldarg.0
    IL_010f:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U2 As Integer"
    IL_0114:  ldarg.0
    IL_0115:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As System.Array"
    IL_011a:  ldloca.s   V_2
    IL_011c:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_0121:  ldloca.s   V_2
    IL_0123:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0129:  ldc.i4.1
    IL_012a:  add.ovf
    IL_012b:  newarr     "Integer"
    IL_0130:  call       "Function Microsoft.VisualBasic.CompilerServices.Utils.CopyArray(System.Array, System.Array) As System.Array"
    IL_0135:  castclass  "Integer()"
    IL_013a:  constrained. "SM$T"
    IL_0140:  callvirt   "Sub IMoveable.set_Position(Integer, Integer())"
    IL_0145:  ldarg.0
    IL_0146:  ldnull
    IL_0147:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As System.Array"
    IL_014c:  leave.s    IL_0172
  }
  catch System.Exception
  {
    IL_014e:  dup
    IL_014f:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_0154:  stloc.s    V_4
    IL_0156:  ldarg.0
    IL_0157:  ldc.i4.s   -2
    IL_0159:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_015e:  ldarg.0
    IL_015f:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_0164:  ldloc.s    V_4
    IL_0166:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_016b:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_0170:  leave.s    IL_0187
  }
  IL_0172:  ldarg.0
  IL_0173:  ldc.i4.s   -2
  IL_0175:  dup
  IL_0176:  stloc.0
  IL_0177:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_017c:  ldarg.0
  IL_017d:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_0182:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_0187:  ret
}
]]>)

            'Wrong IL
            verifier.VerifyIL("Program.VB$StateMachine_3_Call2(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      392 (0x188)
  .maxstack  5
  .locals init (Integer V_0,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_1,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_2,
                Integer V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0053
    IL_000a:  ldloc.0
    IL_000b:  ldc.i4.1
    IL_000c:  beq        IL_00ec
    IL_0011:  ldarg.0
    IL_0012:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_0017:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_001c:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_0021:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0026:  stloc.1
    IL_0027:  ldloca.s   V_1
    IL_0029:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_002e:  brtrue.s   IL_006f
    IL_0030:  ldarg.0
    IL_0031:  ldc.i4.0
    IL_0032:  dup
    IL_0033:  stloc.0
    IL_0034:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_0039:  ldarg.0
    IL_003a:  ldloc.1
    IL_003b:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0040:  ldarg.0
    IL_0041:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_0046:  ldloca.s   V_1
    IL_0048:  ldarg.0
    IL_0049:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_3_Call2(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_3_Call2(Of SM$T))"
    IL_004e:  leave      IL_0187
    IL_0053:  ldarg.0
    IL_0054:  ldc.i4.m1
    IL_0055:  dup
    IL_0056:  stloc.0
    IL_0057:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_005c:  ldarg.0
    IL_005d:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0062:  stloc.1
    IL_0063:  ldarg.0
    IL_0064:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0069:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_006f:  ldarg.0
    IL_0070:  ldarg.0
    IL_0071:  ldloca.s   V_1
    IL_0073:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_0078:  ldloca.s   V_1
    IL_007a:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0080:  dup
    IL_0081:  stloc.3
    IL_0082:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$S0 As Integer"
    IL_0087:  ldloc.3
    IL_0088:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$U2 As Integer"
    IL_008d:  ldarg.0
    IL_008e:  ldarg.0
    IL_008f:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_0094:  ldarg.0
    IL_0095:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$S0 As Integer"
    IL_009a:  constrained. "SM$T"
    IL_00a0:  callvirt   "Function IMoveable.get_Position(Integer) As Integer()"
    IL_00a5:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$U1 As System.Array"
    IL_00aa:  ldarg.0
    IL_00ab:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_00b0:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_00b5:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_00ba:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00bf:  stloc.2
    IL_00c0:  ldloca.s   V_2
    IL_00c2:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_00c7:  brtrue.s   IL_0108
    IL_00c9:  ldarg.0
    IL_00ca:  ldc.i4.1
    IL_00cb:  dup
    IL_00cc:  stloc.0
    IL_00cd:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_00d2:  ldarg.0
    IL_00d3:  ldloc.2
    IL_00d4:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00d9:  ldarg.0
    IL_00da:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00df:  ldloca.s   V_2
    IL_00e1:  ldarg.0
    IL_00e2:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_3_Call2(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_3_Call2(Of SM$T))"
    IL_00e7:  leave      IL_0187
    IL_00ec:  ldarg.0
    IL_00ed:  ldc.i4.m1
    IL_00ee:  dup
    IL_00ef:  stloc.0
    IL_00f0:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_00f5:  ldarg.0
    IL_00f6:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00fb:  stloc.2
    IL_00fc:  ldarg.0
    IL_00fd:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0102:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0108:  ldarg.0
    IL_0109:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_010e:  ldarg.0
    IL_010f:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$U2 As Integer"
    IL_0114:  ldarg.0
    IL_0115:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$U1 As System.Array"
    IL_011a:  ldloca.s   V_2
    IL_011c:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_0121:  ldloca.s   V_2
    IL_0123:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0129:  ldc.i4.1
    IL_012a:  add.ovf
    IL_012b:  newarr     "Integer"
    IL_0130:  call       "Function Microsoft.VisualBasic.CompilerServices.Utils.CopyArray(System.Array, System.Array) As System.Array"
    IL_0135:  castclass  "Integer()"
    IL_013a:  constrained. "SM$T"
    IL_0140:  callvirt   "Sub IMoveable.set_Position(Integer, Integer())"
    IL_0145:  ldarg.0
    IL_0146:  ldnull
    IL_0147:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$U1 As System.Array"
    IL_014c:  leave.s    IL_0172
  }
  catch System.Exception
  {
    IL_014e:  dup
    IL_014f:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_0154:  stloc.s    V_4
    IL_0156:  ldarg.0
    IL_0157:  ldc.i4.s   -2
    IL_0159:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_015e:  ldarg.0
    IL_015f:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_0164:  ldloc.s    V_4
    IL_0166:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_016b:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_0170:  leave.s    IL_0187
  }
  IL_0172:  ldarg.0
  IL_0173:  ldc.i4.s   -2
  IL_0175:  dup
  IL_0176:  stloc.0
  IL_0177:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
  IL_017c:  ldarg.0
  IL_017d:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_0182:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_0187:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_Redim_Indexer_Struct_IndexAndValue_Async_04()
            Dim comp =
<compilation>
    <file>
Imports System
Imports System.Threading.Tasks

Interface IMoveable
    Property Position(x As Integer) As Integer()
End Interface

Structure Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer() Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return Nothing
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Structure

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1).Wait()

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2).Wait()
    End Sub

    Private Shared Async Function Call1(Of T As {Structure, IMoveable})(item As T) As Task
        Redim Preserve item.Position(await GetOffsetAsync(GetOffset(item)))(await GetOffsetAsync(GetOffset(item)))
    End Function

    Private Shared Async Function Call2(Of T As {IMoveable})(item As T) As Task
        Redim Preserve item.Position(await GetOffsetAsync(GetOffset(item)))(await GetOffsetAsync(GetOffset(item)))
    End Function

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return Nothing
    End Function

    Shared Function GetOffsetAsync(i As Integer) As Task(Of Integer)
        Return Task.FromResult(i)
    End Function
End Class
    </file>
</compilation>

            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '-1'
Position set for item '-2'
Position get for item '-3'
Position set for item '-4'
").VerifyDiagnostics()

            verifier.VerifyIL("Program.VB$StateMachine_2_Call1(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      392 (0x188)
  .maxstack  5
  .locals init (Integer V_0,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_1,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_2,
                Integer V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0053
    IL_000a:  ldloc.0
    IL_000b:  ldc.i4.1
    IL_000c:  beq        IL_00ec
    IL_0011:  ldarg.0
    IL_0012:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0017:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_001c:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_0021:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0026:  stloc.1
    IL_0027:  ldloca.s   V_1
    IL_0029:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_002e:  brtrue.s   IL_006f
    IL_0030:  ldarg.0
    IL_0031:  ldc.i4.0
    IL_0032:  dup
    IL_0033:  stloc.0
    IL_0034:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0039:  ldarg.0
    IL_003a:  ldloc.1
    IL_003b:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0040:  ldarg.0
    IL_0041:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_0046:  ldloca.s   V_1
    IL_0048:  ldarg.0
    IL_0049:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_2_Call1(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_2_Call1(Of SM$T))"
    IL_004e:  leave      IL_0187
    IL_0053:  ldarg.0
    IL_0054:  ldc.i4.m1
    IL_0055:  dup
    IL_0056:  stloc.0
    IL_0057:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_005c:  ldarg.0
    IL_005d:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0062:  stloc.1
    IL_0063:  ldarg.0
    IL_0064:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0069:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_006f:  ldarg.0
    IL_0070:  ldarg.0
    IL_0071:  ldloca.s   V_1
    IL_0073:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_0078:  ldloca.s   V_1
    IL_007a:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0080:  dup
    IL_0081:  stloc.3
    IL_0082:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$S0 As Integer"
    IL_0087:  ldloc.3
    IL_0088:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U2 As Integer"
    IL_008d:  ldarg.0
    IL_008e:  ldarg.0
    IL_008f:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0094:  ldarg.0
    IL_0095:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$S0 As Integer"
    IL_009a:  constrained. "SM$T"
    IL_00a0:  callvirt   "Function IMoveable.get_Position(Integer) As Integer()"
    IL_00a5:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As System.Array"
    IL_00aa:  ldarg.0
    IL_00ab:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_00b0:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_00b5:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_00ba:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00bf:  stloc.2
    IL_00c0:  ldloca.s   V_2
    IL_00c2:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_00c7:  brtrue.s   IL_0108
    IL_00c9:  ldarg.0
    IL_00ca:  ldc.i4.1
    IL_00cb:  dup
    IL_00cc:  stloc.0
    IL_00cd:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_00d2:  ldarg.0
    IL_00d3:  ldloc.2
    IL_00d4:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00d9:  ldarg.0
    IL_00da:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00df:  ldloca.s   V_2
    IL_00e1:  ldarg.0
    IL_00e2:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_2_Call1(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_2_Call1(Of SM$T))"
    IL_00e7:  leave      IL_0187
    IL_00ec:  ldarg.0
    IL_00ed:  ldc.i4.m1
    IL_00ee:  dup
    IL_00ef:  stloc.0
    IL_00f0:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_00f5:  ldarg.0
    IL_00f6:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00fb:  stloc.2
    IL_00fc:  ldarg.0
    IL_00fd:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0102:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0108:  ldarg.0
    IL_0109:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_010e:  ldarg.0
    IL_010f:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U2 As Integer"
    IL_0114:  ldarg.0
    IL_0115:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As System.Array"
    IL_011a:  ldloca.s   V_2
    IL_011c:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_0121:  ldloca.s   V_2
    IL_0123:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0129:  ldc.i4.1
    IL_012a:  add.ovf
    IL_012b:  newarr     "Integer"
    IL_0130:  call       "Function Microsoft.VisualBasic.CompilerServices.Utils.CopyArray(System.Array, System.Array) As System.Array"
    IL_0135:  castclass  "Integer()"
    IL_013a:  constrained. "SM$T"
    IL_0140:  callvirt   "Sub IMoveable.set_Position(Integer, Integer())"
    IL_0145:  ldarg.0
    IL_0146:  ldnull
    IL_0147:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$U1 As System.Array"
    IL_014c:  leave.s    IL_0172
  }
  catch System.Exception
  {
    IL_014e:  dup
    IL_014f:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_0154:  stloc.s    V_4
    IL_0156:  ldarg.0
    IL_0157:  ldc.i4.s   -2
    IL_0159:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_015e:  ldarg.0
    IL_015f:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_0164:  ldloc.s    V_4
    IL_0166:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_016b:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_0170:  leave.s    IL_0187
  }
  IL_0172:  ldarg.0
  IL_0173:  ldc.i4.s   -2
  IL_0175:  dup
  IL_0176:  stloc.0
  IL_0177:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_017c:  ldarg.0
  IL_017d:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_0182:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_0187:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_CopyBack_Property_Class_Ref()
            Dim comp =
<compilation>
    <file>
Imports System

Interface IMoveable
    Property Position As Integer
End Interface

Class Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Class

Class Program
    Shared Item As Item

    Shared Sub Main()
        Item = New Item With {.Name = "1"}
        Call1(Item)

        Item = New Item With {.Name = "2"}
        Call2(Item)
    End Sub

    Private Shared Sub Call1(Of T As {Class, IMoveable})(ByRef item As T)
        M(item.Position)
    End Sub

    Private Shared Sub Call2(Of T As {IMoveable})(ByRef item As T)
        M(item.Position)
    End Sub

    Shared value as Integer

    Shared Sub M(ByRef x As Integer)
        value -= 1
        Item = New Item With {.Name = value.ToString()}
    End Sub
End Class
    </file>
</compilation>

            ' Wrong output
            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '1'
Position set for item '-1'
Position get for item '2'
Position set for item '-2'
").VerifyDiagnostics()

            'Wrong IL
            verifier.VerifyIL("Program.Call1(Of T)",
            <![CDATA[
{
  // Code size       34 (0x22)
  .maxstack  2
  .locals init (Integer V_0)
  IL_0000:  ldarg.0
  IL_0001:  constrained. "T"
  IL_0007:  callvirt   "Function IMoveable.get_Position() As Integer"
  IL_000c:  stloc.0
  IL_000d:  ldloca.s   V_0
  IL_000f:  call       "Sub Program.M(ByRef Integer)"
  IL_0014:  ldarg.0
  IL_0015:  ldloc.0
  IL_0016:  constrained. "T"
  IL_001c:  callvirt   "Sub IMoveable.set_Position(Integer)"
  IL_0021:  ret
}
]]>)

            'Wrong IL
            verifier.VerifyIL("Program.Call2(Of T)",
            <![CDATA[
{
  // Code size       34 (0x22)
  .maxstack  2
  .locals init (Integer V_0)
  IL_0000:  ldarg.0
  IL_0001:  constrained. "T"
  IL_0007:  callvirt   "Function IMoveable.get_Position() As Integer"
  IL_000c:  stloc.0
  IL_000d:  ldloca.s   V_0
  IL_000f:  call       "Sub Program.M(ByRef Integer)"
  IL_0014:  ldarg.0
  IL_0015:  ldloc.0
  IL_0016:  constrained. "T"
  IL_001c:  callvirt   "Sub IMoveable.set_Position(Integer)"
  IL_0021:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_CopyBack_Property_Struct_Ref()
            Dim comp =
<compilation>
    <file>
Imports System

Interface IMoveable
    Property Position As Integer
End Interface

Structure Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Structure

Class Program
    Shared Item As Item

    Shared Sub Main()
        Item = New Item With {.Name = "1"}
        Call1(Item)

        Item = New Item With {.Name = "2"}
        Call2(Item)
    End Sub

    Private Shared Sub Call1(Of T As {Structure, IMoveable})(ByRef item As T)
        M(item.Position)
    End Sub

    Private Shared Sub Call2(Of T As {IMoveable})(ByRef item As T)
        M(item.Position)
    End Sub

    Shared value as Integer

    Shared Sub M(ByRef x As Integer)
        value -= 1
        Item = New Item With {.Name = value.ToString()}
    End Sub
End Class
    </file>
</compilation>

            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '1'
Position set for item '-1'
Position get for item '2'
Position set for item '-2'
").VerifyDiagnostics()

            verifier.VerifyIL("Program.Call1(Of T)",
            <![CDATA[
{
  // Code size       34 (0x22)
  .maxstack  2
  .locals init (Integer V_0)
  IL_0000:  ldarg.0
  IL_0001:  constrained. "T"
  IL_0007:  callvirt   "Function IMoveable.get_Position() As Integer"
  IL_000c:  stloc.0
  IL_000d:  ldloca.s   V_0
  IL_000f:  call       "Sub Program.M(ByRef Integer)"
  IL_0014:  ldarg.0
  IL_0015:  ldloc.0
  IL_0016:  constrained. "T"
  IL_001c:  callvirt   "Sub IMoveable.set_Position(Integer)"
  IL_0021:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_CopyBack_Indexer_Class_Index()
            Dim comp =
<compilation>
    <file>
Imports System

Interface IMoveable
    Property Position(x As Integer) As Integer
End Interface

Class Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Class

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1)

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2)
    End Sub

    Private Shared Sub Call1(Of T As {Class, IMoveable})(item As T)
        M(item.Position(GetOffset(item)), 1)
    End Sub

    Private Shared Sub Call2(Of T As {IMoveable})(item As T)
        M(item.Position(GetOffset(item)), 1)
    End Sub

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function

    Shared Sub M(ByRef x As Integer, y As Integer)
        x += y
    End Sub
End Class
    </file>
</compilation>

            ' Wrong output
            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '-1'
Position set for item '-1'
Position get for item '-2'
Position set for item '-2'
").VerifyDiagnostics()

            'Wrong IL
            verifier.VerifyIL("Program.Call1(Of T)",
            <![CDATA[
{
  // Code size       47 (0x2f)
  .maxstack  3
  .locals init (Integer V_0,
                Integer V_1)
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldarga.s   V_0
  IL_0004:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0009:  dup
  IL_000a:  stloc.0
  IL_000b:  constrained. "T"
  IL_0011:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
  IL_0016:  stloc.1
  IL_0017:  ldloca.s   V_1
  IL_0019:  ldc.i4.1
  IL_001a:  call       "Sub Program.M(ByRef Integer, Integer)"
  IL_001f:  ldarga.s   V_0
  IL_0021:  ldloc.0
  IL_0022:  ldloc.1
  IL_0023:  constrained. "T"
  IL_0029:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
  IL_002e:  ret
}
]]>)

            'Wrong IL
            verifier.VerifyIL("Program.Call2(Of T)",
            <![CDATA[
{
  // Code size       47 (0x2f)
  .maxstack  3
  .locals init (Integer V_0,
                Integer V_1)
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldarga.s   V_0
  IL_0004:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0009:  dup
  IL_000a:  stloc.0
  IL_000b:  constrained. "T"
  IL_0011:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
  IL_0016:  stloc.1
  IL_0017:  ldloca.s   V_1
  IL_0019:  ldc.i4.1
  IL_001a:  call       "Sub Program.M(ByRef Integer, Integer)"
  IL_001f:  ldarga.s   V_0
  IL_0021:  ldloc.0
  IL_0022:  ldloc.1
  IL_0023:  constrained. "T"
  IL_0029:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
  IL_002e:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_CopyBack_Indexer_Struct_Index()
            Dim comp =
<compilation>
    <file>
Imports System

Interface IMoveable
    Property Position(x As Integer) As Integer
End Interface

Structure Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Structure

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1)

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2)
    End Sub

    Private Shared Sub Call1(Of T As {Structure, IMoveable})(item As T)
        M(item.Position(GetOffset(item)), 1)
    End Sub

    Private Shared Sub Call2(Of T As {IMoveable})(item As T)
        M(item.Position(GetOffset(item)), 1)
    End Sub

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function

    Shared Sub M(ByRef x As Integer, y As Integer)
        x += y
    End Sub
End Class
    </file>
</compilation>

            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '-1'
Position set for item '-1'
Position get for item '-2'
Position set for item '-2'
").VerifyDiagnostics()

            verifier.VerifyIL("Program.Call1(Of T)",
            <![CDATA[
{
  // Code size       47 (0x2f)
  .maxstack  3
  .locals init (Integer V_0,
                Integer V_1)
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldarga.s   V_0
  IL_0004:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0009:  dup
  IL_000a:  stloc.0
  IL_000b:  constrained. "T"
  IL_0011:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
  IL_0016:  stloc.1
  IL_0017:  ldloca.s   V_1
  IL_0019:  ldc.i4.1
  IL_001a:  call       "Sub Program.M(ByRef Integer, Integer)"
  IL_001f:  ldarga.s   V_0
  IL_0021:  ldloc.0
  IL_0022:  ldloc.1
  IL_0023:  constrained. "T"
  IL_0029:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
  IL_002e:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_CopyBack_Indexer_Class_Index_Ref()
            Dim comp =
<compilation>
    <file>
Imports System

Interface IMoveable
    Property Position(x As Integer) As Integer
End Interface

Class Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Class

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1)

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2)
    End Sub

    Private Shared Sub Call1(Of T As {Class, IMoveable})(ByRef item As T)
        M(item.Position(GetOffset(item)), 1)
    End Sub

    Private Shared Sub Call2(Of T As {IMoveable})(ByRef item As T)
        M(item.Position(GetOffset(item)), 1)
    End Sub

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function

    Shared Sub M(ByRef x As Integer, y As Integer)
        x += y
    End Sub
End Class
    </file>
</compilation>

            ' Wrong output
            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '-1'
Position set for item '-1'
Position get for item '-2'
Position set for item '-2'
").VerifyDiagnostics()

            'Wrong IL
            verifier.VerifyIL("Program.Call1(Of T)",
            <![CDATA[
{
  // Code size       44 (0x2c)
  .maxstack  3
  .locals init (Integer V_0,
                Integer V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0007:  dup
  IL_0008:  stloc.0
  IL_0009:  constrained. "T"
  IL_000f:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
  IL_0014:  stloc.1
  IL_0015:  ldloca.s   V_1
  IL_0017:  ldc.i4.1
  IL_0018:  call       "Sub Program.M(ByRef Integer, Integer)"
  IL_001d:  ldarg.0
  IL_001e:  ldloc.0
  IL_001f:  ldloc.1
  IL_0020:  constrained. "T"
  IL_0026:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
  IL_002b:  ret
}
]]>)

            'Wrong IL
            verifier.VerifyIL("Program.Call2(Of T)",
            <![CDATA[
{
  // Code size       44 (0x2c)
  .maxstack  3
  .locals init (Integer V_0,
                Integer V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0007:  dup
  IL_0008:  stloc.0
  IL_0009:  constrained. "T"
  IL_000f:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
  IL_0014:  stloc.1
  IL_0015:  ldloca.s   V_1
  IL_0017:  ldc.i4.1
  IL_0018:  call       "Sub Program.M(ByRef Integer, Integer)"
  IL_001d:  ldarg.0
  IL_001e:  ldloc.0
  IL_001f:  ldloc.1
  IL_0020:  constrained. "T"
  IL_0026:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
  IL_002b:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_CopyBack_Indexer_Struct_Index_Ref()
            Dim comp =
<compilation>
    <file>
Imports System

Interface IMoveable
    Property Position(x As Integer) As Integer
End Interface

Structure Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Structure

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1)

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2)
    End Sub

    Private Shared Sub Call1(Of T As {Structure, IMoveable})(ByRef item As T)
        M(item.Position(GetOffset(item)), 1)
    End Sub

    Private Shared Sub Call2(Of T As {IMoveable})(ByRef item As T)
        M(item.Position(GetOffset(item)), 1)
    End Sub

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function

    Shared Sub M(ByRef x As Integer, y As Integer)
        x += y
    End Sub
End Class
    </file>
</compilation>

            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '-1'
Position set for item '-1'
Position get for item '-2'
Position set for item '-2'
").VerifyDiagnostics()

            verifier.VerifyIL("Program.Call1(Of T)",
            <![CDATA[
{
  // Code size       44 (0x2c)
  .maxstack  3
  .locals init (Integer V_0,
                Integer V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0007:  dup
  IL_0008:  stloc.0
  IL_0009:  constrained. "T"
  IL_000f:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
  IL_0014:  stloc.1
  IL_0015:  ldloca.s   V_1
  IL_0017:  ldc.i4.1
  IL_0018:  call       "Sub Program.M(ByRef Integer, Integer)"
  IL_001d:  ldarg.0
  IL_001e:  ldloc.0
  IL_001f:  ldloc.1
  IL_0020:  constrained. "T"
  IL_0026:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
  IL_002b:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_CopyBack_Indexer_Class_Index_Async_01()
            Dim comp =
<compilation>
    <file>
Imports System
Imports System.Threading.Tasks

Interface IMoveable
    Property Position(x As Integer) As Integer
End Interface

Class Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Class

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1).Wait()

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2).Wait()
    End Sub

    Private Shared Async Function Call1(Of T As {Class, IMoveable})(item As T) As Task
        M(item.Position(await GetOffsetAsync(GetOffset(item))), 1)
    End Function

    Private Shared Async Function Call2(Of T As {IMoveable})(item As T) As Task
        M(item.Position(await GetOffsetAsync(GetOffset(item))), 1)
    End Function

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function

    Shared Function GetOffsetAsync(i As Integer) As Task(Of Integer)
        Return Task.FromResult(i)
    End Function

    Shared Sub M(ByRef x As Integer, y As Integer)
        x += y
    End Sub
End Class
    </file>
</compilation>

            ' Wrong output
            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '-1'
Position set for item '-1'
Position get for item '-2'
Position set for item '-2'
").VerifyDiagnostics()

            'Wrong IL
            verifier.VerifyIL("Program.VB$StateMachine_2_Call1(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      226 (0xe2)
  .maxstack  3
  .locals init (Integer V_0,
                Integer V_1,
                Integer V_2,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004c
    IL_000a:  ldarg.0
    IL_000b:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0010:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_0015:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_001a:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_001f:  stloc.3
    IL_0020:  ldloca.s   V_3
    IL_0022:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_0027:  brtrue.s   IL_0068
    IL_0029:  ldarg.0
    IL_002a:  ldc.i4.0
    IL_002b:  dup
    IL_002c:  stloc.0
    IL_002d:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0032:  ldarg.0
    IL_0033:  ldloc.3
    IL_0034:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0039:  ldarg.0
    IL_003a:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_003f:  ldloca.s   V_3
    IL_0041:  ldarg.0
    IL_0042:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_2_Call1(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_2_Call1(Of SM$T))"
    IL_0047:  leave      IL_00e1
    IL_004c:  ldarg.0
    IL_004d:  ldc.i4.m1
    IL_004e:  dup
    IL_004f:  stloc.0
    IL_0050:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0055:  ldarg.0
    IL_0056:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_005b:  stloc.3
    IL_005c:  ldarg.0
    IL_005d:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0062:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0068:  ldarg.0
    IL_0069:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_006e:  ldloca.s   V_3
    IL_0070:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_0075:  ldloca.s   V_3
    IL_0077:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_007d:  dup
    IL_007e:  stloc.1
    IL_007f:  constrained. "SM$T"
    IL_0085:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
    IL_008a:  stloc.2
    IL_008b:  ldloca.s   V_2
    IL_008d:  ldc.i4.1
    IL_008e:  call       "Sub Program.M(ByRef Integer, Integer)"
    IL_0093:  ldarg.0
    IL_0094:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0099:  ldloc.1
    IL_009a:  ldloc.2
    IL_009b:  constrained. "SM$T"
    IL_00a1:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
    IL_00a6:  leave.s    IL_00cc
  }
  catch System.Exception
  {
    IL_00a8:  dup
    IL_00a9:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_00ae:  stloc.s    V_4
    IL_00b0:  ldarg.0
    IL_00b1:  ldc.i4.s   -2
    IL_00b3:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_00b8:  ldarg.0
    IL_00b9:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00be:  ldloc.s    V_4
    IL_00c0:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_00c5:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_00ca:  leave.s    IL_00e1
  }
  IL_00cc:  ldarg.0
  IL_00cd:  ldc.i4.s   -2
  IL_00cf:  dup
  IL_00d0:  stloc.0
  IL_00d1:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_00d6:  ldarg.0
  IL_00d7:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_00dc:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_00e1:  ret
}
]]>)

            'Wrong IL
            verifier.VerifyIL("Program.VB$StateMachine_3_Call2(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      226 (0xe2)
  .maxstack  3
  .locals init (Integer V_0,
                Integer V_1,
                Integer V_2,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004c
    IL_000a:  ldarg.0
    IL_000b:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_0010:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_0015:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_001a:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_001f:  stloc.3
    IL_0020:  ldloca.s   V_3
    IL_0022:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_0027:  brtrue.s   IL_0068
    IL_0029:  ldarg.0
    IL_002a:  ldc.i4.0
    IL_002b:  dup
    IL_002c:  stloc.0
    IL_002d:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_0032:  ldarg.0
    IL_0033:  ldloc.3
    IL_0034:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0039:  ldarg.0
    IL_003a:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_003f:  ldloca.s   V_3
    IL_0041:  ldarg.0
    IL_0042:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_3_Call2(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_3_Call2(Of SM$T))"
    IL_0047:  leave      IL_00e1
    IL_004c:  ldarg.0
    IL_004d:  ldc.i4.m1
    IL_004e:  dup
    IL_004f:  stloc.0
    IL_0050:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_0055:  ldarg.0
    IL_0056:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_005b:  stloc.3
    IL_005c:  ldarg.0
    IL_005d:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0062:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0068:  ldarg.0
    IL_0069:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_006e:  ldloca.s   V_3
    IL_0070:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_0075:  ldloca.s   V_3
    IL_0077:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_007d:  dup
    IL_007e:  stloc.1
    IL_007f:  constrained. "SM$T"
    IL_0085:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
    IL_008a:  stloc.2
    IL_008b:  ldloca.s   V_2
    IL_008d:  ldc.i4.1
    IL_008e:  call       "Sub Program.M(ByRef Integer, Integer)"
    IL_0093:  ldarg.0
    IL_0094:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_0099:  ldloc.1
    IL_009a:  ldloc.2
    IL_009b:  constrained. "SM$T"
    IL_00a1:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
    IL_00a6:  leave.s    IL_00cc
  }
  catch System.Exception
  {
    IL_00a8:  dup
    IL_00a9:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_00ae:  stloc.s    V_4
    IL_00b0:  ldarg.0
    IL_00b1:  ldc.i4.s   -2
    IL_00b3:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_00b8:  ldarg.0
    IL_00b9:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00be:  ldloc.s    V_4
    IL_00c0:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_00c5:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_00ca:  leave.s    IL_00e1
  }
  IL_00cc:  ldarg.0
  IL_00cd:  ldc.i4.s   -2
  IL_00cf:  dup
  IL_00d0:  stloc.0
  IL_00d1:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
  IL_00d6:  ldarg.0
  IL_00d7:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_00dc:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_00e1:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_CopyBack_Indexer_Struct_Index_Async_01()
            Dim comp =
<compilation>
    <file>
Imports System
Imports System.Threading.Tasks

Interface IMoveable
    Property Position(x As Integer) As Integer
End Interface

Structure Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Structure

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1).Wait()

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2).Wait()
    End Sub

    Private Shared Async Function Call1(Of T As {Structure, IMoveable})(item As T) As Task
        M(item.Position(await GetOffsetAsync(GetOffset(item))), 1)
    End Function

    Private Shared Async Function Call2(Of T As {IMoveable})(item As T) As Task
        M(item.Position(await GetOffsetAsync(GetOffset(item))), 1)
    End Function

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function

    Shared Function GetOffsetAsync(i As Integer) As Task(Of Integer)
        Return Task.FromResult(i)
    End Function

    Shared Sub M(ByRef x As Integer, y As Integer)
        x += y
    End Sub
End Class
    </file>
</compilation>

            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '-1'
Position set for item '-1'
Position get for item '-2'
Position set for item '-2'
").VerifyDiagnostics()

            verifier.VerifyIL("Program.VB$StateMachine_2_Call1(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      226 (0xe2)
  .maxstack  3
  .locals init (Integer V_0,
                Integer V_1,
                Integer V_2,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004c
    IL_000a:  ldarg.0
    IL_000b:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0010:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_0015:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_001a:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_001f:  stloc.3
    IL_0020:  ldloca.s   V_3
    IL_0022:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_0027:  brtrue.s   IL_0068
    IL_0029:  ldarg.0
    IL_002a:  ldc.i4.0
    IL_002b:  dup
    IL_002c:  stloc.0
    IL_002d:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0032:  ldarg.0
    IL_0033:  ldloc.3
    IL_0034:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0039:  ldarg.0
    IL_003a:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_003f:  ldloca.s   V_3
    IL_0041:  ldarg.0
    IL_0042:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_2_Call1(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_2_Call1(Of SM$T))"
    IL_0047:  leave      IL_00e1
    IL_004c:  ldarg.0
    IL_004d:  ldc.i4.m1
    IL_004e:  dup
    IL_004f:  stloc.0
    IL_0050:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0055:  ldarg.0
    IL_0056:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_005b:  stloc.3
    IL_005c:  ldarg.0
    IL_005d:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0062:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0068:  ldarg.0
    IL_0069:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_006e:  ldloca.s   V_3
    IL_0070:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_0075:  ldloca.s   V_3
    IL_0077:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_007d:  dup
    IL_007e:  stloc.1
    IL_007f:  constrained. "SM$T"
    IL_0085:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
    IL_008a:  stloc.2
    IL_008b:  ldloca.s   V_2
    IL_008d:  ldc.i4.1
    IL_008e:  call       "Sub Program.M(ByRef Integer, Integer)"
    IL_0093:  ldarg.0
    IL_0094:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0099:  ldloc.1
    IL_009a:  ldloc.2
    IL_009b:  constrained. "SM$T"
    IL_00a1:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
    IL_00a6:  leave.s    IL_00cc
  }
  catch System.Exception
  {
    IL_00a8:  dup
    IL_00a9:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_00ae:  stloc.s    V_4
    IL_00b0:  ldarg.0
    IL_00b1:  ldc.i4.s   -2
    IL_00b3:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_00b8:  ldarg.0
    IL_00b9:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00be:  ldloc.s    V_4
    IL_00c0:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_00c5:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_00ca:  leave.s    IL_00e1
  }
  IL_00cc:  ldarg.0
  IL_00cd:  ldc.i4.s   -2
  IL_00cf:  dup
  IL_00d0:  stloc.0
  IL_00d1:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_00d6:  ldarg.0
  IL_00d7:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_00dc:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_00e1:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_CopyBack_Indexer_Class_Index_Async_02()
            Dim comp =
<compilation>
    <file>
Imports System
Imports System.Threading.Tasks

Interface IMoveable
    Property Position(x as Integer) As Integer
End Interface

Class Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x as Integer) As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Class

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1).Wait()

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2).Wait()
    End Sub

    Private Shared Async Function Call1(Of T As {Class, IMoveable})(item As T) As Task
        await Task.Yield()
        M(item.Position(await GetOffsetAsync(GetOffset(item))), 1)
    End Function

    Private Shared Async Function Call2(Of T As {IMoveable})(item As T) As Task
        await Task.Yield()
        M(item.Position(await GetOffsetAsync(GetOffset(item))), 1)
    End Function

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function

    Shared Function GetOffsetAsync(i As Integer) As Task(Of Integer)
        Return Task.FromResult(i)
    End Function

    Shared Sub M(ByRef x As Integer, y As Integer)
        x += y
    End Sub
End Class
    </file>
</compilation>

            ' Wrong output
            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '-1'
Position set for item '-1'
Position get for item '-2'
Position set for item '-2'
").VerifyDiagnostics()

            'Wrong IL
            verifier.VerifyIL("Program.VB$StateMachine_2_Call1(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      339 (0x153)
  .maxstack  3
  .locals init (Integer V_0,
                System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_1,
                System.Runtime.CompilerServices.YieldAwaitable V_2,
                Integer V_3,
                Integer V_4,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_5,
                System.Exception V_6)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004b
    IL_000a:  ldloc.0
    IL_000b:  ldc.i4.1
    IL_000c:  beq        IL_00ba
    IL_0011:  call       "Function System.Threading.Tasks.Task.Yield() As System.Runtime.CompilerServices.YieldAwaitable"
    IL_0016:  stloc.2
    IL_0017:  ldloca.s   V_2
    IL_0019:  call       "Function System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter() As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_001e:  stloc.1
    IL_001f:  ldloca.s   V_1
    IL_0021:  call       "Function System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.get_IsCompleted() As Boolean"
    IL_0026:  brtrue.s   IL_0067
    IL_0028:  ldarg.0
    IL_0029:  ldc.i4.0
    IL_002a:  dup
    IL_002b:  stloc.0
    IL_002c:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0031:  ldarg.0
    IL_0032:  ldloc.1
    IL_0033:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0038:  ldarg.0
    IL_0039:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_003e:  ldloca.s   V_1
    IL_0040:  ldarg.0
    IL_0041:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, Program.VB$StateMachine_2_Call1(Of SM$T))(ByRef System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ByRef Program.VB$StateMachine_2_Call1(Of SM$T))"
    IL_0046:  leave      IL_0152
    IL_004b:  ldarg.0
    IL_004c:  ldc.i4.m1
    IL_004d:  dup
    IL_004e:  stloc.0
    IL_004f:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0054:  ldarg.0
    IL_0055:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_005a:  stloc.1
    IL_005b:  ldarg.0
    IL_005c:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0061:  initobj    "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0067:  ldloca.s   V_1
    IL_0069:  call       "Sub System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()"
    IL_006e:  ldloca.s   V_1
    IL_0070:  initobj    "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0076:  ldarg.0
    IL_0077:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_007c:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_0081:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_0086:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_008b:  stloc.s    V_5
    IL_008d:  ldloca.s   V_5
    IL_008f:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_0094:  brtrue.s   IL_00d7
    IL_0096:  ldarg.0
    IL_0097:  ldc.i4.1
    IL_0098:  dup
    IL_0099:  stloc.0
    IL_009a:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_009f:  ldarg.0
    IL_00a0:  ldloc.s    V_5
    IL_00a2:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A1 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00a7:  ldarg.0
    IL_00a8:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00ad:  ldloca.s   V_5
    IL_00af:  ldarg.0
    IL_00b0:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_2_Call1(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_2_Call1(Of SM$T))"
    IL_00b5:  leave      IL_0152
    IL_00ba:  ldarg.0
    IL_00bb:  ldc.i4.m1
    IL_00bc:  dup
    IL_00bd:  stloc.0
    IL_00be:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_00c3:  ldarg.0
    IL_00c4:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A1 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00c9:  stloc.s    V_5
    IL_00cb:  ldarg.0
    IL_00cc:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$A1 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00d1:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00d7:  ldarg.0
    IL_00d8:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_00dd:  ldloca.s   V_5
    IL_00df:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_00e4:  ldloca.s   V_5
    IL_00e6:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00ec:  dup
    IL_00ed:  stloc.3
    IL_00ee:  constrained. "SM$T"
    IL_00f4:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
    IL_00f9:  stloc.s    V_4
    IL_00fb:  ldloca.s   V_4
    IL_00fd:  ldc.i4.1
    IL_00fe:  call       "Sub Program.M(ByRef Integer, Integer)"
    IL_0103:  ldarg.0
    IL_0104:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0109:  ldloc.3
    IL_010a:  ldloc.s    V_4
    IL_010c:  constrained. "SM$T"
    IL_0112:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
    IL_0117:  leave.s    IL_013d
  }
  catch System.Exception
  {
    IL_0119:  dup
    IL_011a:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_011f:  stloc.s    V_6
    IL_0121:  ldarg.0
    IL_0122:  ldc.i4.s   -2
    IL_0124:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0129:  ldarg.0
    IL_012a:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_012f:  ldloc.s    V_6
    IL_0131:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_0136:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_013b:  leave.s    IL_0152
  }
  IL_013d:  ldarg.0
  IL_013e:  ldc.i4.s   -2
  IL_0140:  dup
  IL_0141:  stloc.0
  IL_0142:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_0147:  ldarg.0
  IL_0148:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_014d:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_0152:  ret
}
]]>)

            'Wrong IL
            verifier.VerifyIL("Program.VB$StateMachine_3_Call2(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      339 (0x153)
  .maxstack  3
  .locals init (Integer V_0,
                System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_1,
                System.Runtime.CompilerServices.YieldAwaitable V_2,
                Integer V_3,
                Integer V_4,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_5,
                System.Exception V_6)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004b
    IL_000a:  ldloc.0
    IL_000b:  ldc.i4.1
    IL_000c:  beq        IL_00ba
    IL_0011:  call       "Function System.Threading.Tasks.Task.Yield() As System.Runtime.CompilerServices.YieldAwaitable"
    IL_0016:  stloc.2
    IL_0017:  ldloca.s   V_2
    IL_0019:  call       "Function System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter() As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_001e:  stloc.1
    IL_001f:  ldloca.s   V_1
    IL_0021:  call       "Function System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.get_IsCompleted() As Boolean"
    IL_0026:  brtrue.s   IL_0067
    IL_0028:  ldarg.0
    IL_0029:  ldc.i4.0
    IL_002a:  dup
    IL_002b:  stloc.0
    IL_002c:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_0031:  ldarg.0
    IL_0032:  ldloc.1
    IL_0033:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0038:  ldarg.0
    IL_0039:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_003e:  ldloca.s   V_1
    IL_0040:  ldarg.0
    IL_0041:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, Program.VB$StateMachine_3_Call2(Of SM$T))(ByRef System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ByRef Program.VB$StateMachine_3_Call2(Of SM$T))"
    IL_0046:  leave      IL_0152
    IL_004b:  ldarg.0
    IL_004c:  ldc.i4.m1
    IL_004d:  dup
    IL_004e:  stloc.0
    IL_004f:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_0054:  ldarg.0
    IL_0055:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_005a:  stloc.1
    IL_005b:  ldarg.0
    IL_005c:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0061:  initobj    "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0067:  ldloca.s   V_1
    IL_0069:  call       "Sub System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()"
    IL_006e:  ldloca.s   V_1
    IL_0070:  initobj    "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0076:  ldarg.0
    IL_0077:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_007c:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_0081:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_0086:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_008b:  stloc.s    V_5
    IL_008d:  ldloca.s   V_5
    IL_008f:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_0094:  brtrue.s   IL_00d7
    IL_0096:  ldarg.0
    IL_0097:  ldc.i4.1
    IL_0098:  dup
    IL_0099:  stloc.0
    IL_009a:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_009f:  ldarg.0
    IL_00a0:  ldloc.s    V_5
    IL_00a2:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A1 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00a7:  ldarg.0
    IL_00a8:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00ad:  ldloca.s   V_5
    IL_00af:  ldarg.0
    IL_00b0:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_3_Call2(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_3_Call2(Of SM$T))"
    IL_00b5:  leave      IL_0152
    IL_00ba:  ldarg.0
    IL_00bb:  ldc.i4.m1
    IL_00bc:  dup
    IL_00bd:  stloc.0
    IL_00be:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_00c3:  ldarg.0
    IL_00c4:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A1 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00c9:  stloc.s    V_5
    IL_00cb:  ldarg.0
    IL_00cc:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$A1 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00d1:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00d7:  ldarg.0
    IL_00d8:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_00dd:  ldloca.s   V_5
    IL_00df:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_00e4:  ldloca.s   V_5
    IL_00e6:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00ec:  dup
    IL_00ed:  stloc.3
    IL_00ee:  constrained. "SM$T"
    IL_00f4:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
    IL_00f9:  stloc.s    V_4
    IL_00fb:  ldloca.s   V_4
    IL_00fd:  ldc.i4.1
    IL_00fe:  call       "Sub Program.M(ByRef Integer, Integer)"
    IL_0103:  ldarg.0
    IL_0104:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_0109:  ldloc.3
    IL_010a:  ldloc.s    V_4
    IL_010c:  constrained. "SM$T"
    IL_0112:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
    IL_0117:  leave.s    IL_013d
  }
  catch System.Exception
  {
    IL_0119:  dup
    IL_011a:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_011f:  stloc.s    V_6
    IL_0121:  ldarg.0
    IL_0122:  ldc.i4.s   -2
    IL_0124:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_0129:  ldarg.0
    IL_012a:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_012f:  ldloc.s    V_6
    IL_0131:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_0136:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_013b:  leave.s    IL_0152
  }
  IL_013d:  ldarg.0
  IL_013e:  ldc.i4.s   -2
  IL_0140:  dup
  IL_0141:  stloc.0
  IL_0142:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
  IL_0147:  ldarg.0
  IL_0148:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_014d:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_0152:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_CopyBack_Indexer_Struct_Index_Async_02()
            Dim comp =
<compilation>
    <file>
Imports System
Imports System.Threading.Tasks

Interface IMoveable
    Property Position(x as Integer) As Integer
End Interface

Structure Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x as Integer) As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Structure

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1).Wait()

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2).Wait()
    End Sub

    Private Shared Async Function Call1(Of T As {Structure, IMoveable})(item As T) As Task
        await Task.Yield()
        M(item.Position(await GetOffsetAsync(GetOffset(item))), 1)
    End Function

    Private Shared Async Function Call2(Of T As {IMoveable})(item As T) As Task
        await Task.Yield()
        M(item.Position(await GetOffsetAsync(GetOffset(item))), 1)
    End Function

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function

    Shared Function GetOffsetAsync(i As Integer) As Task(Of Integer)
        Return Task.FromResult(i)
    End Function

    Shared Sub M(ByRef x As Integer, y As Integer)
        x += y
    End Sub
End Class
    </file>
</compilation>

            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '-1'
Position set for item '-1'
Position get for item '-2'
Position set for item '-2'
").VerifyDiagnostics()

            verifier.VerifyIL("Program.VB$StateMachine_2_Call1(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      339 (0x153)
  .maxstack  3
  .locals init (Integer V_0,
                System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_1,
                System.Runtime.CompilerServices.YieldAwaitable V_2,
                Integer V_3,
                Integer V_4,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_5,
                System.Exception V_6)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004b
    IL_000a:  ldloc.0
    IL_000b:  ldc.i4.1
    IL_000c:  beq        IL_00ba
    IL_0011:  call       "Function System.Threading.Tasks.Task.Yield() As System.Runtime.CompilerServices.YieldAwaitable"
    IL_0016:  stloc.2
    IL_0017:  ldloca.s   V_2
    IL_0019:  call       "Function System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter() As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_001e:  stloc.1
    IL_001f:  ldloca.s   V_1
    IL_0021:  call       "Function System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.get_IsCompleted() As Boolean"
    IL_0026:  brtrue.s   IL_0067
    IL_0028:  ldarg.0
    IL_0029:  ldc.i4.0
    IL_002a:  dup
    IL_002b:  stloc.0
    IL_002c:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0031:  ldarg.0
    IL_0032:  ldloc.1
    IL_0033:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0038:  ldarg.0
    IL_0039:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_003e:  ldloca.s   V_1
    IL_0040:  ldarg.0
    IL_0041:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, Program.VB$StateMachine_2_Call1(Of SM$T))(ByRef System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ByRef Program.VB$StateMachine_2_Call1(Of SM$T))"
    IL_0046:  leave      IL_0152
    IL_004b:  ldarg.0
    IL_004c:  ldc.i4.m1
    IL_004d:  dup
    IL_004e:  stloc.0
    IL_004f:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0054:  ldarg.0
    IL_0055:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_005a:  stloc.1
    IL_005b:  ldarg.0
    IL_005c:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0061:  initobj    "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0067:  ldloca.s   V_1
    IL_0069:  call       "Sub System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()"
    IL_006e:  ldloca.s   V_1
    IL_0070:  initobj    "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0076:  ldarg.0
    IL_0077:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_007c:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_0081:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_0086:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_008b:  stloc.s    V_5
    IL_008d:  ldloca.s   V_5
    IL_008f:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_0094:  brtrue.s   IL_00d7
    IL_0096:  ldarg.0
    IL_0097:  ldc.i4.1
    IL_0098:  dup
    IL_0099:  stloc.0
    IL_009a:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_009f:  ldarg.0
    IL_00a0:  ldloc.s    V_5
    IL_00a2:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A1 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00a7:  ldarg.0
    IL_00a8:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00ad:  ldloca.s   V_5
    IL_00af:  ldarg.0
    IL_00b0:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_2_Call1(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_2_Call1(Of SM$T))"
    IL_00b5:  leave      IL_0152
    IL_00ba:  ldarg.0
    IL_00bb:  ldc.i4.m1
    IL_00bc:  dup
    IL_00bd:  stloc.0
    IL_00be:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_00c3:  ldarg.0
    IL_00c4:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A1 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00c9:  stloc.s    V_5
    IL_00cb:  ldarg.0
    IL_00cc:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$A1 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00d1:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00d7:  ldarg.0
    IL_00d8:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_00dd:  ldloca.s   V_5
    IL_00df:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_00e4:  ldloca.s   V_5
    IL_00e6:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00ec:  dup
    IL_00ed:  stloc.3
    IL_00ee:  constrained. "SM$T"
    IL_00f4:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
    IL_00f9:  stloc.s    V_4
    IL_00fb:  ldloca.s   V_4
    IL_00fd:  ldc.i4.1
    IL_00fe:  call       "Sub Program.M(ByRef Integer, Integer)"
    IL_0103:  ldarg.0
    IL_0104:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0109:  ldloc.3
    IL_010a:  ldloc.s    V_4
    IL_010c:  constrained. "SM$T"
    IL_0112:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
    IL_0117:  leave.s    IL_013d
  }
  catch System.Exception
  {
    IL_0119:  dup
    IL_011a:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_011f:  stloc.s    V_6
    IL_0121:  ldarg.0
    IL_0122:  ldc.i4.s   -2
    IL_0124:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0129:  ldarg.0
    IL_012a:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_012f:  ldloc.s    V_6
    IL_0131:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_0136:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_013b:  leave.s    IL_0152
  }
  IL_013d:  ldarg.0
  IL_013e:  ldc.i4.s   -2
  IL_0140:  dup
  IL_0141:  stloc.0
  IL_0142:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_0147:  ldarg.0
  IL_0148:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_014d:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_0152:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_CopyBack_Indexer_Class_Value()
            Dim comp =
<compilation>
    <file>
Imports System

Interface IMoveable
    Property Position(x As Integer) As Integer
End Interface

Class Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Class

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1)

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2)
    End Sub

    Private Shared Sub Call1(Of T As {Class, IMoveable})(item As T)
        M(item.Position(1), GetOffset(item))
    End Sub

    Private Shared Sub Call2(Of T As {IMoveable})(item As T)
        M(item.Position(1), GetOffset(item))
    End Sub

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function

    Shared Sub M(ByRef x As Integer, y As Integer)
        x += y
    End Sub
End Class
    </file>
</compilation>

            ' Wrong output on some frameworks
            '            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
            '"
            'Position get for item '1'
            'Position set for item '1'
            'Position get for item '2'
            'Position set for item '2'
            '").VerifyDiagnostics()
            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe).VerifyDiagnostics()

            'Wrong IL
            verifier.VerifyIL("Program.Call1(Of T)",
            <![CDATA[
{
  // Code size       45 (0x2d)
  .maxstack  3
  .locals init (Integer V_0)
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldc.i4.1
  IL_0003:  constrained. "T"
  IL_0009:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
  IL_000e:  stloc.0
  IL_000f:  ldloca.s   V_0
  IL_0011:  ldarga.s   V_0
  IL_0013:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0018:  call       "Sub Program.M(ByRef Integer, Integer)"
  IL_001d:  ldarga.s   V_0
  IL_001f:  ldc.i4.1
  IL_0020:  ldloc.0
  IL_0021:  constrained. "T"
  IL_0027:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
  IL_002c:  ret
}
]]>)

            'Wrong IL
            verifier.VerifyIL("Program.Call2(Of T)",
            <![CDATA[
{
  // Code size       45 (0x2d)
  .maxstack  3
  .locals init (Integer V_0)
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldc.i4.1
  IL_0003:  constrained. "T"
  IL_0009:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
  IL_000e:  stloc.0
  IL_000f:  ldloca.s   V_0
  IL_0011:  ldarga.s   V_0
  IL_0013:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0018:  call       "Sub Program.M(ByRef Integer, Integer)"
  IL_001d:  ldarga.s   V_0
  IL_001f:  ldc.i4.1
  IL_0020:  ldloc.0
  IL_0021:  constrained. "T"
  IL_0027:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
  IL_002c:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_CopyBack_Indexer_Struct_Value()
            Dim comp =
<compilation>
    <file>
Imports System

Interface IMoveable
    Property Position(x As Integer) As Integer
End Interface

Structure Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Structure

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1)

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2)
    End Sub

    Private Shared Sub Call1(Of T As {Structure, IMoveable})(item As T)
        M(item.Position(1), GetOffset(item))
    End Sub

    Private Shared Sub Call2(Of T As {IMoveable})(item As T)
        M(item.Position(1), GetOffset(item))
    End Sub

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function

    Shared Sub M(ByRef x As Integer, y As Integer)
        x += y
    End Sub
End Class
    </file>
</compilation>

            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '1'
Position set for item '-1'
Position get for item '2'
Position set for item '-2'
").VerifyDiagnostics()

            verifier.VerifyIL("Program.Call1(Of T)",
            <![CDATA[
{
  // Code size       45 (0x2d)
  .maxstack  3
  .locals init (Integer V_0)
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldc.i4.1
  IL_0003:  constrained. "T"
  IL_0009:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
  IL_000e:  stloc.0
  IL_000f:  ldloca.s   V_0
  IL_0011:  ldarga.s   V_0
  IL_0013:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0018:  call       "Sub Program.M(ByRef Integer, Integer)"
  IL_001d:  ldarga.s   V_0
  IL_001f:  ldc.i4.1
  IL_0020:  ldloc.0
  IL_0021:  constrained. "T"
  IL_0027:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
  IL_002c:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_CopyBack_Indexer_Class_Value_Ref()
            Dim comp =
<compilation>
    <file>
Imports System

Interface IMoveable
    Property Position(x As Integer) As Integer
End Interface

Class Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Class

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1)

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2)
    End Sub

    Private Shared Sub Call1(Of T As {Class, IMoveable})(ByRef item As T)
        M(item.Position(1), GetOffset(item))
    End Sub

    Private Shared Sub Call2(Of T As {IMoveable})(ByRef item As T)
        M(item.Position(1), GetOffset(item))
    End Sub

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function

    Shared Sub M(ByRef x As Integer, y As Integer)
        x += y
    End Sub
End Class
    </file>
</compilation>

            'Wrong output on some frameworks
            '            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
            '"
            'Position get for item '1'
            'Position set for item '1'
            'Position get for item '2'
            'Position set for item '2'
            '").VerifyDiagnostics()
            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe).VerifyDiagnostics()

            'Wrong IL
            verifier.VerifyIL("Program.Call1(Of T)",
            <![CDATA[
{
  // Code size       42 (0x2a)
  .maxstack  3
  .locals init (Integer V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  constrained. "T"
  IL_0008:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
  IL_000d:  stloc.0
  IL_000e:  ldloca.s   V_0
  IL_0010:  ldarg.0
  IL_0011:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0016:  call       "Sub Program.M(ByRef Integer, Integer)"
  IL_001b:  ldarg.0
  IL_001c:  ldc.i4.1
  IL_001d:  ldloc.0
  IL_001e:  constrained. "T"
  IL_0024:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
  IL_0029:  ret
}
]]>)

            'Wrong IL
            verifier.VerifyIL("Program.Call2(Of T)",
            <![CDATA[
{
  // Code size       42 (0x2a)
  .maxstack  3
  .locals init (Integer V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  constrained. "T"
  IL_0008:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
  IL_000d:  stloc.0
  IL_000e:  ldloca.s   V_0
  IL_0010:  ldarg.0
  IL_0011:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0016:  call       "Sub Program.M(ByRef Integer, Integer)"
  IL_001b:  ldarg.0
  IL_001c:  ldc.i4.1
  IL_001d:  ldloc.0
  IL_001e:  constrained. "T"
  IL_0024:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
  IL_0029:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_CopyBack_Indexer_Struct_Value_Ref()
            Dim comp =
<compilation>
    <file>
Imports System

Interface IMoveable
    Property Position(x As Integer) As Integer
End Interface

Structure Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Structure

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1)

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2)
    End Sub

    Private Shared Sub Call1(Of T As {Structure, IMoveable})(ByRef item As T)
        M(item.Position(1), GetOffset(item))
    End Sub

    Private Shared Sub Call2(Of T As {IMoveable})(ByRef item As T)
        M(item.Position(1), GetOffset(item))
    End Sub

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function

    Shared Sub M(ByRef x As Integer, y As Integer)
        x += y
    End Sub
End Class
    </file>
</compilation>

            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '1'
Position set for item '-1'
Position get for item '2'
Position set for item '-2'
").VerifyDiagnostics()

            verifier.VerifyIL("Program.Call1(Of T)",
            <![CDATA[
{
  // Code size       42 (0x2a)
  .maxstack  3
  .locals init (Integer V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  constrained. "T"
  IL_0008:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
  IL_000d:  stloc.0
  IL_000e:  ldloca.s   V_0
  IL_0010:  ldarg.0
  IL_0011:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0016:  call       "Sub Program.M(ByRef Integer, Integer)"
  IL_001b:  ldarg.0
  IL_001c:  ldc.i4.1
  IL_001d:  ldloc.0
  IL_001e:  constrained. "T"
  IL_0024:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
  IL_0029:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_CopyBack_Indexer_Class_Value_Async_01()
            Dim comp =
<compilation>
    <file>
Imports System
Imports System.Threading.Tasks

Interface IMoveable
    Property Position(x As Integer) As Integer
End Interface

Class Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Class

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1).Wait()

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2).Wait()
    End Sub

    Private Shared Async Function Call1(Of T As {Class, IMoveable})(item As T) As Task
        M(item.Position(1), await GetOffsetAsync(GetOffset(item)))
    End Function

    Private Shared Async Function Call2(Of T As {IMoveable})(item As T) As Task
        M(item.Position(1), await GetOffsetAsync(GetOffset(item)))
    End Function

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function

    Shared Function GetOffsetAsync(i As Integer) As Task(Of Integer)
        Return Task.FromResult(i)
    End Function

    Shared Sub M(ByRef x As Integer, y As Integer)
        x += y
    End Sub
End Class
    </file>
</compilation>

            ' Wrong output
            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '1'
Position set for item '-1'
Position get for item '2'
Position set for item '-2'
").VerifyDiagnostics()

            'Wrong IL
            verifier.VerifyIL("Program.VB$StateMachine_2_Call1(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      236 (0xec)
  .maxstack  3
  .locals init (Integer V_0,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_1,
                System.Exception V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0064
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0011:  ldc.i4.1
    IL_0012:  constrained. "SM$T"
    IL_0018:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
    IL_001d:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$S0 As Integer"
    IL_0022:  ldarg.0
    IL_0023:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0028:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_002d:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_0032:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0037:  stloc.1
    IL_0038:  ldloca.s   V_1
    IL_003a:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_003f:  brtrue.s   IL_0080
    IL_0041:  ldarg.0
    IL_0042:  ldc.i4.0
    IL_0043:  dup
    IL_0044:  stloc.0
    IL_0045:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_004a:  ldarg.0
    IL_004b:  ldloc.1
    IL_004c:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0051:  ldarg.0
    IL_0052:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_0057:  ldloca.s   V_1
    IL_0059:  ldarg.0
    IL_005a:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_2_Call1(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_2_Call1(Of SM$T))"
    IL_005f:  leave      IL_00eb
    IL_0064:  ldarg.0
    IL_0065:  ldc.i4.m1
    IL_0066:  dup
    IL_0067:  stloc.0
    IL_0068:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_006d:  ldarg.0
    IL_006e:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0073:  stloc.1
    IL_0074:  ldarg.0
    IL_0075:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_007a:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0080:  ldarg.0
    IL_0081:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$S0 As Integer"
    IL_0086:  ldloca.s   V_1
    IL_0088:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_008d:  ldloca.s   V_1
    IL_008f:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0095:  call       "Sub Program.M(ByRef Integer, Integer)"
    IL_009a:  ldarg.0
    IL_009b:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_00a0:  ldc.i4.1
    IL_00a1:  ldarg.0
    IL_00a2:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$S0 As Integer"
    IL_00a7:  constrained. "SM$T"
    IL_00ad:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
    IL_00b2:  leave.s    IL_00d6
  }
  catch System.Exception
  {
    IL_00b4:  dup
    IL_00b5:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_00ba:  stloc.2
    IL_00bb:  ldarg.0
    IL_00bc:  ldc.i4.s   -2
    IL_00be:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_00c3:  ldarg.0
    IL_00c4:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00c9:  ldloc.2
    IL_00ca:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_00cf:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_00d4:  leave.s    IL_00eb
  }
  IL_00d6:  ldarg.0
  IL_00d7:  ldc.i4.s   -2
  IL_00d9:  dup
  IL_00da:  stloc.0
  IL_00db:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_00e0:  ldarg.0
  IL_00e1:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_00e6:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_00eb:  ret
}
]]>)

            'Wrong IL
            verifier.VerifyIL("Program.VB$StateMachine_3_Call2(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      236 (0xec)
  .maxstack  3
  .locals init (Integer V_0,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_1,
                System.Exception V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0064
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_0011:  ldc.i4.1
    IL_0012:  constrained. "SM$T"
    IL_0018:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
    IL_001d:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$S0 As Integer"
    IL_0022:  ldarg.0
    IL_0023:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_0028:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_002d:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_0032:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0037:  stloc.1
    IL_0038:  ldloca.s   V_1
    IL_003a:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_003f:  brtrue.s   IL_0080
    IL_0041:  ldarg.0
    IL_0042:  ldc.i4.0
    IL_0043:  dup
    IL_0044:  stloc.0
    IL_0045:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_004a:  ldarg.0
    IL_004b:  ldloc.1
    IL_004c:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0051:  ldarg.0
    IL_0052:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_0057:  ldloca.s   V_1
    IL_0059:  ldarg.0
    IL_005a:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_3_Call2(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_3_Call2(Of SM$T))"
    IL_005f:  leave      IL_00eb
    IL_0064:  ldarg.0
    IL_0065:  ldc.i4.m1
    IL_0066:  dup
    IL_0067:  stloc.0
    IL_0068:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_006d:  ldarg.0
    IL_006e:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0073:  stloc.1
    IL_0074:  ldarg.0
    IL_0075:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_007a:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0080:  ldarg.0
    IL_0081:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$S0 As Integer"
    IL_0086:  ldloca.s   V_1
    IL_0088:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_008d:  ldloca.s   V_1
    IL_008f:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0095:  call       "Sub Program.M(ByRef Integer, Integer)"
    IL_009a:  ldarg.0
    IL_009b:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_00a0:  ldc.i4.1
    IL_00a1:  ldarg.0
    IL_00a2:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$S0 As Integer"
    IL_00a7:  constrained. "SM$T"
    IL_00ad:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
    IL_00b2:  leave.s    IL_00d6
  }
  catch System.Exception
  {
    IL_00b4:  dup
    IL_00b5:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_00ba:  stloc.2
    IL_00bb:  ldarg.0
    IL_00bc:  ldc.i4.s   -2
    IL_00be:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_00c3:  ldarg.0
    IL_00c4:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00c9:  ldloc.2
    IL_00ca:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_00cf:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_00d4:  leave.s    IL_00eb
  }
  IL_00d6:  ldarg.0
  IL_00d7:  ldc.i4.s   -2
  IL_00d9:  dup
  IL_00da:  stloc.0
  IL_00db:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
  IL_00e0:  ldarg.0
  IL_00e1:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_00e6:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_00eb:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_CopyBack_Indexer_Struct_Value_Async_01()
            Dim comp =
<compilation>
    <file>
Imports System
Imports System.Threading.Tasks

Interface IMoveable
    Property Position(x As Integer) As Integer
End Interface

Structure Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Structure

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1).Wait()

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2).Wait()
    End Sub

    Private Shared Async Function Call1(Of T As {Structure, IMoveable})(item As T) As Task
        M(item.Position(1), await GetOffsetAsync(GetOffset(item)))
    End Function

    Private Shared Async Function Call2(Of T As {IMoveable})(item As T) As Task
        M(item.Position(1), await GetOffsetAsync(GetOffset(item)))
    End Function

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function

    Shared Function GetOffsetAsync(i As Integer) As Task(Of Integer)
        Return Task.FromResult(i)
    End Function

    Shared Sub M(ByRef x As Integer, y As Integer)
        x += y
    End Sub
End Class
    </file>
</compilation>

            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '1'
Position set for item '-1'
Position get for item '2'
Position set for item '-2'
").VerifyDiagnostics()

            verifier.VerifyIL("Program.VB$StateMachine_2_Call1(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      236 (0xec)
  .maxstack  3
  .locals init (Integer V_0,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_1,
                System.Exception V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0064
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0011:  ldc.i4.1
    IL_0012:  constrained. "SM$T"
    IL_0018:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
    IL_001d:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$S0 As Integer"
    IL_0022:  ldarg.0
    IL_0023:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0028:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_002d:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_0032:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0037:  stloc.1
    IL_0038:  ldloca.s   V_1
    IL_003a:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_003f:  brtrue.s   IL_0080
    IL_0041:  ldarg.0
    IL_0042:  ldc.i4.0
    IL_0043:  dup
    IL_0044:  stloc.0
    IL_0045:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_004a:  ldarg.0
    IL_004b:  ldloc.1
    IL_004c:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0051:  ldarg.0
    IL_0052:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_0057:  ldloca.s   V_1
    IL_0059:  ldarg.0
    IL_005a:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_2_Call1(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_2_Call1(Of SM$T))"
    IL_005f:  leave      IL_00eb
    IL_0064:  ldarg.0
    IL_0065:  ldc.i4.m1
    IL_0066:  dup
    IL_0067:  stloc.0
    IL_0068:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_006d:  ldarg.0
    IL_006e:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0073:  stloc.1
    IL_0074:  ldarg.0
    IL_0075:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_007a:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0080:  ldarg.0
    IL_0081:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$S0 As Integer"
    IL_0086:  ldloca.s   V_1
    IL_0088:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_008d:  ldloca.s   V_1
    IL_008f:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0095:  call       "Sub Program.M(ByRef Integer, Integer)"
    IL_009a:  ldarg.0
    IL_009b:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_00a0:  ldc.i4.1
    IL_00a1:  ldarg.0
    IL_00a2:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$S0 As Integer"
    IL_00a7:  constrained. "SM$T"
    IL_00ad:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
    IL_00b2:  leave.s    IL_00d6
  }
  catch System.Exception
  {
    IL_00b4:  dup
    IL_00b5:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_00ba:  stloc.2
    IL_00bb:  ldarg.0
    IL_00bc:  ldc.i4.s   -2
    IL_00be:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_00c3:  ldarg.0
    IL_00c4:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00c9:  ldloc.2
    IL_00ca:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_00cf:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_00d4:  leave.s    IL_00eb
  }
  IL_00d6:  ldarg.0
  IL_00d7:  ldc.i4.s   -2
  IL_00d9:  dup
  IL_00da:  stloc.0
  IL_00db:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_00e0:  ldarg.0
  IL_00e1:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_00e6:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_00eb:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_CopyBack_Indexer_Class_IndexAndValue()
            Dim comp =
<compilation>
    <file>
Imports System

Interface IMoveable
    Property Position(x As Integer) As Integer
End Interface

Class Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Class

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1)

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2)
    End Sub

    Private Shared Sub Call1(Of T As {Class, IMoveable})(item As T)
        M(item.Position(GetOffset(item)), GetOffset(item))
    End Sub

    Private Shared Sub Call2(Of T As {IMoveable})(item As T)
        M(item.Position(GetOffset(item)), GetOffset(item))
    End Sub

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function

    Shared Sub M(ByRef x As Integer, y As Integer)
        x += y
    End Sub
End Class
    </file>
</compilation>

            ' Wrong output and differs, but still wrong, on some frameworks 
            '            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
            '"
            'Position get for item '-1'
            'Position set for item '-1'
            'Position get for item '-3'
            'Position set for item '-3'
            '").VerifyDiagnostics()
            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe).VerifyDiagnostics()

            'Wrong IL
            verifier.VerifyIL("Program.Call1(Of T)",
            <![CDATA[
{
  // Code size       53 (0x35)
  .maxstack  3
  .locals init (Integer V_0,
                Integer V_1)
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldarga.s   V_0
  IL_0004:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0009:  dup
  IL_000a:  stloc.0
  IL_000b:  constrained. "T"
  IL_0011:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
  IL_0016:  stloc.1
  IL_0017:  ldloca.s   V_1
  IL_0019:  ldarga.s   V_0
  IL_001b:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0020:  call       "Sub Program.M(ByRef Integer, Integer)"
  IL_0025:  ldarga.s   V_0
  IL_0027:  ldloc.0
  IL_0028:  ldloc.1
  IL_0029:  constrained. "T"
  IL_002f:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
  IL_0034:  ret
}
]]>)

            'Wrong IL
            verifier.VerifyIL("Program.Call2(Of T)",
            <![CDATA[
{
  // Code size       53 (0x35)
  .maxstack  3
  .locals init (Integer V_0,
                Integer V_1)
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldarga.s   V_0
  IL_0004:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0009:  dup
  IL_000a:  stloc.0
  IL_000b:  constrained. "T"
  IL_0011:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
  IL_0016:  stloc.1
  IL_0017:  ldloca.s   V_1
  IL_0019:  ldarga.s   V_0
  IL_001b:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0020:  call       "Sub Program.M(ByRef Integer, Integer)"
  IL_0025:  ldarga.s   V_0
  IL_0027:  ldloc.0
  IL_0028:  ldloc.1
  IL_0029:  constrained. "T"
  IL_002f:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
  IL_0034:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_CopyBack_Indexer_Struct_IndexAndValue()
            Dim comp =
<compilation>
    <file>
Imports System

Interface IMoveable
    Property Position(x As Integer) As Integer
End Interface

Structure Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Structure

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1)

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2)
    End Sub

    Private Shared Sub Call1(Of T As {Structure, IMoveable})(item As T)
        M(item.Position(GetOffset(item)), GetOffset(item))
    End Sub

    Private Shared Sub Call2(Of T As {IMoveable})(item As T)
        M(item.Position(GetOffset(item)), GetOffset(item))
    End Sub

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function

    Shared Sub M(ByRef x As Integer, y As Integer)
        x += y
    End Sub
End Class
    </file>
</compilation>

            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '-1'
Position set for item '-2'
Position get for item '-3'
Position set for item '-4'
").VerifyDiagnostics()

            verifier.VerifyIL("Program.Call1(Of T)",
            <![CDATA[
{
  // Code size       53 (0x35)
  .maxstack  3
  .locals init (Integer V_0,
                Integer V_1)
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldarga.s   V_0
  IL_0004:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0009:  dup
  IL_000a:  stloc.0
  IL_000b:  constrained. "T"
  IL_0011:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
  IL_0016:  stloc.1
  IL_0017:  ldloca.s   V_1
  IL_0019:  ldarga.s   V_0
  IL_001b:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0020:  call       "Sub Program.M(ByRef Integer, Integer)"
  IL_0025:  ldarga.s   V_0
  IL_0027:  ldloc.0
  IL_0028:  ldloc.1
  IL_0029:  constrained. "T"
  IL_002f:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
  IL_0034:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_CopyBack_Indexer_Class_IndexAndValue_Ref()
            Dim comp =
<compilation>
    <file>
Imports System

Interface IMoveable
    Property Position(x As Integer) As Integer
End Interface

Class Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Class

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1)

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2)
    End Sub

    Private Shared Sub Call1(Of T As {Class, IMoveable})(ByRef item As T)
        M(item.Position(GetOffset(item)), GetOffset(item))
    End Sub

    Private Shared Sub Call2(Of T As {IMoveable})(ByRef item As T)
        M(item.Position(GetOffset(item)), GetOffset(item))
    End Sub

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function

    Shared Sub M(ByRef x As Integer, y As Integer)
        x += y
    End Sub
End Class
    </file>
</compilation>

            ' Wrong output and framework dependent
            '            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
            '"
            'Position get for item '-1'
            'Position set for item '-1'
            'Position get for item '-3'
            'Position set for item '-3'
            '").VerifyDiagnostics()
            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe).VerifyDiagnostics()

            'Wrong IL
            verifier.VerifyIL("Program.Call1(Of T)",
            <![CDATA[
{
  // Code size       49 (0x31)
  .maxstack  3
  .locals init (Integer V_0,
                Integer V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0007:  dup
  IL_0008:  stloc.0
  IL_0009:  constrained. "T"
  IL_000f:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
  IL_0014:  stloc.1
  IL_0015:  ldloca.s   V_1
  IL_0017:  ldarg.0
  IL_0018:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_001d:  call       "Sub Program.M(ByRef Integer, Integer)"
  IL_0022:  ldarg.0
  IL_0023:  ldloc.0
  IL_0024:  ldloc.1
  IL_0025:  constrained. "T"
  IL_002b:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
  IL_0030:  ret
}
]]>)

            'Wrong IL
            verifier.VerifyIL("Program.Call2(Of T)",
            <![CDATA[
{
  // Code size       49 (0x31)
  .maxstack  3
  .locals init (Integer V_0,
            Integer V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0007:  dup
  IL_0008:  stloc.0
  IL_0009:  constrained. "T"
  IL_000f:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
  IL_0014:  stloc.1
  IL_0015:  ldloca.s   V_1
  IL_0017:  ldarg.0
  IL_0018:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_001d:  call       "Sub Program.M(ByRef Integer, Integer)"
  IL_0022:  ldarg.0
  IL_0023:  ldloc.0
  IL_0024:  ldloc.1
  IL_0025:  constrained. "T"
  IL_002b:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
  IL_0030:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_CopyBack_Indexer_Struct_IndexAndValue_Ref()
            Dim comp =
<compilation>
    <file>
Imports System

Interface IMoveable
    Property Position(x As Integer) As Integer
End Interface

Structure Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Structure

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1)

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2)
    End Sub

    Private Shared Sub Call1(Of T As {Structure, IMoveable})(ByRef item As T)
        M(item.Position(GetOffset(item)), GetOffset(item))
    End Sub

    Private Shared Sub Call2(Of T As {IMoveable})(ByRef item As T)
        M(item.Position(GetOffset(item)), GetOffset(item))
    End Sub

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function

    Shared Sub M(ByRef x As Integer, y As Integer)
        x += y
    End Sub
End Class
    </file>
</compilation>

            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '-1'
Position set for item '-2'
Position get for item '-3'
Position set for item '-4'
").VerifyDiagnostics()

            verifier.VerifyIL("Program.Call1(Of T)",
            <![CDATA[
{
  // Code size       49 (0x31)
  .maxstack  3
  .locals init (Integer V_0,
            Integer V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_0007:  dup
  IL_0008:  stloc.0
  IL_0009:  constrained. "T"
  IL_000f:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
  IL_0014:  stloc.1
  IL_0015:  ldloca.s   V_1
  IL_0017:  ldarg.0
  IL_0018:  call       "Function Program.GetOffset(Of T)(ByRef T) As Integer"
  IL_001d:  call       "Sub Program.M(ByRef Integer, Integer)"
  IL_0022:  ldarg.0
  IL_0023:  ldloc.0
  IL_0024:  ldloc.1
  IL_0025:  constrained. "T"
  IL_002b:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
  IL_0030:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_CopyBack_Indexer_Class_IndexAndValue_Async_01()
            Dim comp =
<compilation>
    <file>
Imports System
Imports System.Threading.Tasks

Interface IMoveable
    Property Position(x As Integer) As Integer
End Interface

Class Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Class

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1).Wait()

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2).Wait()
    End Sub

    Private Shared Async Function Call1(Of T As {Class, IMoveable})(item As T) As Task
        M(item.Position(GetOffset(item)), await GetOffsetAsync(GetOffset(item)))
    End Function

    Private Shared Async Function Call2(Of T As {IMoveable})(item As T) As Task
        M(item.Position(GetOffset(item)), await GetOffsetAsync(GetOffset(item)))
    End Function

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function

    Shared Function GetOffsetAsync(i As Integer) As Task(Of Integer)
        Return Task.FromResult(i)
    End Function

    Shared Sub M(ByRef x As Integer, y As Integer)
        x += y
    End Sub
End Class
    </file>
</compilation>

            ' Wrong output
            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '-1'
Position set for item '-2'
Position get for item '-3'
Position set for item '-4'
").VerifyDiagnostics()

            'Wrong IL
            verifier.VerifyIL("Program.VB$StateMachine_2_Call1(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      260 (0x104)
  .maxstack  5
  .locals init (Integer V_0,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_1,
                Integer V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0077
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0011:  ldarg.0
    IL_0012:  ldarg.0
    IL_0013:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0018:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_001d:  dup
    IL_001e:  stloc.2
    IL_001f:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$S0 As Integer"
    IL_0024:  ldloc.2
    IL_0025:  constrained. "SM$T"
    IL_002b:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
    IL_0030:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$S1 As Integer"
    IL_0035:  ldarg.0
    IL_0036:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_003b:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_0040:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_0045:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_004a:  stloc.1
    IL_004b:  ldloca.s   V_1
    IL_004d:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_0052:  brtrue.s   IL_0093
    IL_0054:  ldarg.0
    IL_0055:  ldc.i4.0
    IL_0056:  dup
    IL_0057:  stloc.0
    IL_0058:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_005d:  ldarg.0
    IL_005e:  ldloc.1
    IL_005f:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0064:  ldarg.0
    IL_0065:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_006a:  ldloca.s   V_1
    IL_006c:  ldarg.0
    IL_006d:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_2_Call1(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_2_Call1(Of SM$T))"
    IL_0072:  leave      IL_0103
    IL_0077:  ldarg.0
    IL_0078:  ldc.i4.m1
    IL_0079:  dup
    IL_007a:  stloc.0
    IL_007b:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0080:  ldarg.0
    IL_0081:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0086:  stloc.1
    IL_0087:  ldarg.0
    IL_0088:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_008d:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0093:  ldarg.0
    IL_0094:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$S1 As Integer"
    IL_0099:  ldloca.s   V_1
    IL_009b:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_00a0:  ldloca.s   V_1
    IL_00a2:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00a8:  call       "Sub Program.M(ByRef Integer, Integer)"
    IL_00ad:  ldarg.0
    IL_00ae:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_00b3:  ldarg.0
    IL_00b4:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$S0 As Integer"
    IL_00b9:  ldarg.0
    IL_00ba:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$S1 As Integer"
    IL_00bf:  constrained. "SM$T"
    IL_00c5:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
    IL_00ca:  leave.s    IL_00ee
  }
  catch System.Exception
  {
    IL_00cc:  dup
    IL_00cd:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_00d2:  stloc.3
    IL_00d3:  ldarg.0
    IL_00d4:  ldc.i4.s   -2
    IL_00d6:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_00db:  ldarg.0
    IL_00dc:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00e1:  ldloc.3
    IL_00e2:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_00e7:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_00ec:  leave.s    IL_0103
  }
  IL_00ee:  ldarg.0
  IL_00ef:  ldc.i4.s   -2
  IL_00f1:  dup
  IL_00f2:  stloc.0
  IL_00f3:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_00f8:  ldarg.0
  IL_00f9:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_00fe:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_0103:  ret
}
]]>)

            'Wrong IL
            verifier.VerifyIL("Program.VB$StateMachine_3_Call2(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      260 (0x104)
  .maxstack  5
  .locals init (Integer V_0,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_1,
                Integer V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0077
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_0011:  ldarg.0
    IL_0012:  ldarg.0
    IL_0013:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_0018:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_001d:  dup
    IL_001e:  stloc.2
    IL_001f:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$S0 As Integer"
    IL_0024:  ldloc.2
    IL_0025:  constrained. "SM$T"
    IL_002b:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
    IL_0030:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$S1 As Integer"
    IL_0035:  ldarg.0
    IL_0036:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_003b:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_0040:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_0045:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_004a:  stloc.1
    IL_004b:  ldloca.s   V_1
    IL_004d:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_0052:  brtrue.s   IL_0093
    IL_0054:  ldarg.0
    IL_0055:  ldc.i4.0
    IL_0056:  dup
    IL_0057:  stloc.0
    IL_0058:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_005d:  ldarg.0
    IL_005e:  ldloc.1
    IL_005f:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0064:  ldarg.0
    IL_0065:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_006a:  ldloca.s   V_1
    IL_006c:  ldarg.0
    IL_006d:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_3_Call2(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_3_Call2(Of SM$T))"
    IL_0072:  leave      IL_0103
    IL_0077:  ldarg.0
    IL_0078:  ldc.i4.m1
    IL_0079:  dup
    IL_007a:  stloc.0
    IL_007b:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_0080:  ldarg.0
    IL_0081:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0086:  stloc.1
    IL_0087:  ldarg.0
    IL_0088:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_008d:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0093:  ldarg.0
    IL_0094:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$S1 As Integer"
    IL_0099:  ldloca.s   V_1
    IL_009b:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_00a0:  ldloca.s   V_1
    IL_00a2:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00a8:  call       "Sub Program.M(ByRef Integer, Integer)"
    IL_00ad:  ldarg.0
    IL_00ae:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_00b3:  ldarg.0
    IL_00b4:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$S0 As Integer"
    IL_00b9:  ldarg.0
    IL_00ba:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$S1 As Integer"
    IL_00bf:  constrained. "SM$T"
    IL_00c5:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
    IL_00ca:  leave.s    IL_00ee
  }
  catch System.Exception
  {
    IL_00cc:  dup
    IL_00cd:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_00d2:  stloc.3
    IL_00d3:  ldarg.0
    IL_00d4:  ldc.i4.s   -2
    IL_00d6:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_00db:  ldarg.0
    IL_00dc:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00e1:  ldloc.3
    IL_00e2:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_00e7:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_00ec:  leave.s    IL_0103
  }
  IL_00ee:  ldarg.0
  IL_00ef:  ldc.i4.s   -2
  IL_00f1:  dup
  IL_00f2:  stloc.0
  IL_00f3:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
  IL_00f8:  ldarg.0
  IL_00f9:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_00fe:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_0103:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_CopyBack_Indexer_Struct_IndexAndValue_Async_01()
            Dim comp =
<compilation>
    <file>
Imports System
Imports System.Threading.Tasks

Interface IMoveable
    Property Position(x As Integer) As Integer
End Interface

Structure Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Structure

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1).Wait()

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2).Wait()
    End Sub

    Private Shared Async Function Call1(Of T As {Structure, IMoveable})(item As T) As Task
        M(item.Position(GetOffset(item)), await GetOffsetAsync(GetOffset(item)))
    End Function

    Private Shared Async Function Call2(Of T As {IMoveable})(item As T) As Task
        M(item.Position(GetOffset(item)), await GetOffsetAsync(GetOffset(item)))
    End Function

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function

    Shared Function GetOffsetAsync(i As Integer) As Task(Of Integer)
        Return Task.FromResult(i)
    End Function

    Shared Sub M(ByRef x As Integer, y As Integer)
        x += y
    End Sub
End Class
    </file>
</compilation>

            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '-1'
Position set for item '-2'
Position get for item '-3'
Position set for item '-4'
").VerifyDiagnostics()

            verifier.VerifyIL("Program.VB$StateMachine_2_Call1(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      260 (0x104)
  .maxstack  5
  .locals init (Integer V_0,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_1,
                Integer V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0077
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0011:  ldarg.0
    IL_0012:  ldarg.0
    IL_0013:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0018:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_001d:  dup
    IL_001e:  stloc.2
    IL_001f:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$S0 As Integer"
    IL_0024:  ldloc.2
    IL_0025:  constrained. "SM$T"
    IL_002b:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
    IL_0030:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$S1 As Integer"
    IL_0035:  ldarg.0
    IL_0036:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_003b:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_0040:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_0045:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_004a:  stloc.1
    IL_004b:  ldloca.s   V_1
    IL_004d:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_0052:  brtrue.s   IL_0093
    IL_0054:  ldarg.0
    IL_0055:  ldc.i4.0
    IL_0056:  dup
    IL_0057:  stloc.0
    IL_0058:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_005d:  ldarg.0
    IL_005e:  ldloc.1
    IL_005f:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0064:  ldarg.0
    IL_0065:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_006a:  ldloca.s   V_1
    IL_006c:  ldarg.0
    IL_006d:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_2_Call1(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_2_Call1(Of SM$T))"
    IL_0072:  leave      IL_0103
    IL_0077:  ldarg.0
    IL_0078:  ldc.i4.m1
    IL_0079:  dup
    IL_007a:  stloc.0
    IL_007b:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0080:  ldarg.0
    IL_0081:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0086:  stloc.1
    IL_0087:  ldarg.0
    IL_0088:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_008d:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0093:  ldarg.0
    IL_0094:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$S1 As Integer"
    IL_0099:  ldloca.s   V_1
    IL_009b:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_00a0:  ldloca.s   V_1
    IL_00a2:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00a8:  call       "Sub Program.M(ByRef Integer, Integer)"
    IL_00ad:  ldarg.0
    IL_00ae:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_00b3:  ldarg.0
    IL_00b4:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$S0 As Integer"
    IL_00b9:  ldarg.0
    IL_00ba:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$S1 As Integer"
    IL_00bf:  constrained. "SM$T"
    IL_00c5:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
    IL_00ca:  leave.s    IL_00ee
  }
  catch System.Exception
  {
    IL_00cc:  dup
    IL_00cd:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_00d2:  stloc.3
    IL_00d3:  ldarg.0
    IL_00d4:  ldc.i4.s   -2
    IL_00d6:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_00db:  ldarg.0
    IL_00dc:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00e1:  ldloc.3
    IL_00e2:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_00e7:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_00ec:  leave.s    IL_0103
  }
  IL_00ee:  ldarg.0
  IL_00ef:  ldc.i4.s   -2
  IL_00f1:  dup
  IL_00f2:  stloc.0
  IL_00f3:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_00f8:  ldarg.0
  IL_00f9:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_00fe:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_0103:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_CopyBack_Indexer_Class_IndexAndValue_Async_03()
            Dim comp =
<compilation>
    <file>
Imports System
Imports System.Threading.Tasks

Interface IMoveable
    Property Position(x As Integer) As Integer
End Interface

Class Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Class

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1).Wait()

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2).Wait()
    End Sub

    Private Shared Async Function Call1(Of T As {Class, IMoveable})(item As T) As Task
        M(item.Position(await GetOffsetAsync(GetOffset(item))), GetOffset(item))
    End Function

    Private Shared Async Function Call2(Of T As {IMoveable})(item As T) As Task
        M(item.Position(await GetOffsetAsync(GetOffset(item))), GetOffset(item))
    End Function

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function

    Shared Function GetOffsetAsync(i As Integer) As Task(Of Integer)
        Return Task.FromResult(i)
    End Function

    Shared Sub M(ByRef x As Integer, y As Integer)
        x += y
    End Sub
End Class
    </file>
</compilation>

            ' Wrong output and framework dependent 
            '            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
            '"
            'Position get for item '-1'
            'Position set for item '-1'
            'Position get for item '-3'
            'Position set for item '-3'
            '").VerifyDiagnostics()
            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe).VerifyDiagnostics()

            'Wrong IL
            verifier.VerifyIL("Program.VB$StateMachine_2_Call1(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      236 (0xec)
  .maxstack  3
  .locals init (Integer V_0,
                Integer V_1,
                Integer V_2,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004c
    IL_000a:  ldarg.0
    IL_000b:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0010:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_0015:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_001a:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_001f:  stloc.3
    IL_0020:  ldloca.s   V_3
    IL_0022:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_0027:  brtrue.s   IL_0068
    IL_0029:  ldarg.0
    IL_002a:  ldc.i4.0
    IL_002b:  dup
    IL_002c:  stloc.0
    IL_002d:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0032:  ldarg.0
    IL_0033:  ldloc.3
    IL_0034:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0039:  ldarg.0
    IL_003a:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_003f:  ldloca.s   V_3
    IL_0041:  ldarg.0
    IL_0042:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_2_Call1(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_2_Call1(Of SM$T))"
    IL_0047:  leave      IL_00eb
    IL_004c:  ldarg.0
    IL_004d:  ldc.i4.m1
    IL_004e:  dup
    IL_004f:  stloc.0
    IL_0050:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0055:  ldarg.0
    IL_0056:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_005b:  stloc.3
    IL_005c:  ldarg.0
    IL_005d:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0062:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0068:  ldarg.0
    IL_0069:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_006e:  ldloca.s   V_3
    IL_0070:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_0075:  ldloca.s   V_3
    IL_0077:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_007d:  dup
    IL_007e:  stloc.1
    IL_007f:  constrained. "SM$T"
    IL_0085:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
    IL_008a:  stloc.2
    IL_008b:  ldloca.s   V_2
    IL_008d:  ldarg.0
    IL_008e:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0093:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_0098:  call       "Sub Program.M(ByRef Integer, Integer)"
    IL_009d:  ldarg.0
    IL_009e:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_00a3:  ldloc.1
    IL_00a4:  ldloc.2
    IL_00a5:  constrained. "SM$T"
    IL_00ab:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
    IL_00b0:  leave.s    IL_00d6
  }
  catch System.Exception
  {
    IL_00b2:  dup
    IL_00b3:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_00b8:  stloc.s    V_4
    IL_00ba:  ldarg.0
    IL_00bb:  ldc.i4.s   -2
    IL_00bd:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_00c2:  ldarg.0
    IL_00c3:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00c8:  ldloc.s    V_4
    IL_00ca:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_00cf:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_00d4:  leave.s    IL_00eb
  }
  IL_00d6:  ldarg.0
  IL_00d7:  ldc.i4.s   -2
  IL_00d9:  dup
  IL_00da:  stloc.0
  IL_00db:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_00e0:  ldarg.0
  IL_00e1:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_00e6:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_00eb:  ret
}
]]>)

            'Wrong IL
            verifier.VerifyIL("Program.VB$StateMachine_3_Call2(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      236 (0xec)
  .maxstack  3
  .locals init (Integer V_0,
                Integer V_1,
                Integer V_2,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004c
    IL_000a:  ldarg.0
    IL_000b:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_0010:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_0015:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_001a:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_001f:  stloc.3
    IL_0020:  ldloca.s   V_3
    IL_0022:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_0027:  brtrue.s   IL_0068
    IL_0029:  ldarg.0
    IL_002a:  ldc.i4.0
    IL_002b:  dup
    IL_002c:  stloc.0
    IL_002d:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_0032:  ldarg.0
    IL_0033:  ldloc.3
    IL_0034:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0039:  ldarg.0
    IL_003a:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_003f:  ldloca.s   V_3
    IL_0041:  ldarg.0
    IL_0042:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_3_Call2(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_3_Call2(Of SM$T))"
    IL_0047:  leave      IL_00eb
    IL_004c:  ldarg.0
    IL_004d:  ldc.i4.m1
    IL_004e:  dup
    IL_004f:  stloc.0
    IL_0050:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_0055:  ldarg.0
    IL_0056:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_005b:  stloc.3
    IL_005c:  ldarg.0
    IL_005d:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0062:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0068:  ldarg.0
    IL_0069:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_006e:  ldloca.s   V_3
    IL_0070:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_0075:  ldloca.s   V_3
    IL_0077:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_007d:  dup
    IL_007e:  stloc.1
    IL_007f:  constrained. "SM$T"
    IL_0085:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
    IL_008a:  stloc.2
    IL_008b:  ldloca.s   V_2
    IL_008d:  ldarg.0
    IL_008e:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_0093:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_0098:  call       "Sub Program.M(ByRef Integer, Integer)"
    IL_009d:  ldarg.0
    IL_009e:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_00a3:  ldloc.1
    IL_00a4:  ldloc.2
    IL_00a5:  constrained. "SM$T"
    IL_00ab:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
    IL_00b0:  leave.s    IL_00d6
  }
  catch System.Exception
  {
    IL_00b2:  dup
    IL_00b3:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_00b8:  stloc.s    V_4
    IL_00ba:  ldarg.0
    IL_00bb:  ldc.i4.s   -2
    IL_00bd:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_00c2:  ldarg.0
    IL_00c3:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00c8:  ldloc.s    V_4
    IL_00ca:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_00cf:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_00d4:  leave.s    IL_00eb
  }
  IL_00d6:  ldarg.0
  IL_00d7:  ldc.i4.s   -2
  IL_00d9:  dup
  IL_00da:  stloc.0
  IL_00db:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
  IL_00e0:  ldarg.0
  IL_00e1:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_00e6:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_00eb:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_CopyBack_Indexer_Struct_IndexAndValue_Async_03()
            Dim comp =
<compilation>
    <file>
Imports System
Imports System.Threading.Tasks

Interface IMoveable
    Property Position(x As Integer) As Integer
End Interface

Structure Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Structure

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1).Wait()

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2).Wait()
    End Sub

    Private Shared Async Function Call1(Of T As {Structure, IMoveable})(item As T) As Task
        M(item.Position(await GetOffsetAsync(GetOffset(item))), GetOffset(item))
    End Function

    Private Shared Async Function Call2(Of T As {IMoveable})(item As T) As Task
        M(item.Position(await GetOffsetAsync(GetOffset(item))), GetOffset(item))
    End Function

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function

    Shared Function GetOffsetAsync(i As Integer) As Task(Of Integer)
        Return Task.FromResult(i)
    End Function

    Shared Sub M(ByRef x As Integer, y As Integer)
        x += y
    End Sub
End Class
    </file>
</compilation>

            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '-1'
Position set for item '-2'
Position get for item '-3'
Position set for item '-4'
").VerifyDiagnostics()

            verifier.VerifyIL("Program.VB$StateMachine_2_Call1(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      236 (0xec)
  .maxstack  3
  .locals init (Integer V_0,
                Integer V_1,
                Integer V_2,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004c
    IL_000a:  ldarg.0
    IL_000b:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0010:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_0015:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_001a:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_001f:  stloc.3
    IL_0020:  ldloca.s   V_3
    IL_0022:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_0027:  brtrue.s   IL_0068
    IL_0029:  ldarg.0
    IL_002a:  ldc.i4.0
    IL_002b:  dup
    IL_002c:  stloc.0
    IL_002d:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0032:  ldarg.0
    IL_0033:  ldloc.3
    IL_0034:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0039:  ldarg.0
    IL_003a:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_003f:  ldloca.s   V_3
    IL_0041:  ldarg.0
    IL_0042:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_2_Call1(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_2_Call1(Of SM$T))"
    IL_0047:  leave      IL_00eb
    IL_004c:  ldarg.0
    IL_004d:  ldc.i4.m1
    IL_004e:  dup
    IL_004f:  stloc.0
    IL_0050:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0055:  ldarg.0
    IL_0056:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_005b:  stloc.3
    IL_005c:  ldarg.0
    IL_005d:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0062:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0068:  ldarg.0
    IL_0069:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_006e:  ldloca.s   V_3
    IL_0070:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_0075:  ldloca.s   V_3
    IL_0077:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_007d:  dup
    IL_007e:  stloc.1
    IL_007f:  constrained. "SM$T"
    IL_0085:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
    IL_008a:  stloc.2
    IL_008b:  ldloca.s   V_2
    IL_008d:  ldarg.0
    IL_008e:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0093:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_0098:  call       "Sub Program.M(ByRef Integer, Integer)"
    IL_009d:  ldarg.0
    IL_009e:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_00a3:  ldloc.1
    IL_00a4:  ldloc.2
    IL_00a5:  constrained. "SM$T"
    IL_00ab:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
    IL_00b0:  leave.s    IL_00d6
  }
  catch System.Exception
  {
    IL_00b2:  dup
    IL_00b3:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_00b8:  stloc.s    V_4
    IL_00ba:  ldarg.0
    IL_00bb:  ldc.i4.s   -2
    IL_00bd:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_00c2:  ldarg.0
    IL_00c3:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00c8:  ldloc.s    V_4
    IL_00ca:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_00cf:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_00d4:  leave.s    IL_00eb
  }
  IL_00d6:  ldarg.0
  IL_00d7:  ldc.i4.s   -2
  IL_00d9:  dup
  IL_00da:  stloc.0
  IL_00db:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_00e0:  ldarg.0
  IL_00e1:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_00e6:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_00eb:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_CopyBack_Indexer_Class_IndexAndValue_Async_04()
            Dim comp =
<compilation>
    <file>
Imports System
Imports System.Threading.Tasks

Interface IMoveable
    Property Position(x As Integer) As Integer
End Interface

Class Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Class

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1).Wait()

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2).Wait()
    End Sub

    Private Shared Async Function Call1(Of T As {Class, IMoveable})(item As T) As Task
        M(item.Position(await GetOffsetAsync(GetOffset(item))), await GetOffsetAsync(GetOffset(item)))
    End Function

    Private Shared Async Function Call2(Of T As {IMoveable})(item As T) As Task
        M(item.Position(await GetOffsetAsync(GetOffset(item))), await GetOffsetAsync(GetOffset(item)))
    End Function

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function

    Shared Function GetOffsetAsync(i As Integer) As Task(Of Integer)
        Return Task.FromResult(i)
    End Function

    Shared Sub M(ByRef x As Integer, y As Integer)
        x += y
    End Sub
End Class
    </file>
</compilation>

            ' Wrong output
            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '-1'
Position set for item '-2'
Position get for item '-3'
Position set for item '-4'
").VerifyDiagnostics()

            'Wrong IL
            verifier.VerifyIL("Program.VB$StateMachine_2_Call1(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      367 (0x16f)
  .maxstack  5
  .locals init (Integer V_0,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_1,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_2,
                Integer V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0053
    IL_000a:  ldloc.0
    IL_000b:  ldc.i4.1
    IL_000c:  beq        IL_00e0
    IL_0011:  ldarg.0
    IL_0012:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0017:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_001c:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_0021:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0026:  stloc.2
    IL_0027:  ldloca.s   V_2
    IL_0029:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_002e:  brtrue.s   IL_006f
    IL_0030:  ldarg.0
    IL_0031:  ldc.i4.0
    IL_0032:  dup
    IL_0033:  stloc.0
    IL_0034:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0039:  ldarg.0
    IL_003a:  ldloc.2
    IL_003b:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0040:  ldarg.0
    IL_0041:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_0046:  ldloca.s   V_2
    IL_0048:  ldarg.0
    IL_0049:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_2_Call1(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_2_Call1(Of SM$T))"
    IL_004e:  leave      IL_016e
    IL_0053:  ldarg.0
    IL_0054:  ldc.i4.m1
    IL_0055:  dup
    IL_0056:  stloc.0
    IL_0057:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_005c:  ldarg.0
    IL_005d:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0062:  stloc.2
    IL_0063:  ldarg.0
    IL_0064:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0069:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_006f:  ldarg.0
    IL_0070:  ldarg.0
    IL_0071:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0076:  ldarg.0
    IL_0077:  ldloca.s   V_2
    IL_0079:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_007e:  ldloca.s   V_2
    IL_0080:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0086:  dup
    IL_0087:  stloc.3
    IL_0088:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$S0 As Integer"
    IL_008d:  ldloc.3
    IL_008e:  constrained. "SM$T"
    IL_0094:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
    IL_0099:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$S1 As Integer"
    IL_009e:  ldarg.0
    IL_009f:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_00a4:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_00a9:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_00ae:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00b3:  stloc.1
    IL_00b4:  ldloca.s   V_1
    IL_00b6:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_00bb:  brtrue.s   IL_00fc
    IL_00bd:  ldarg.0
    IL_00be:  ldc.i4.1
    IL_00bf:  dup
    IL_00c0:  stloc.0
    IL_00c1:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_00c6:  ldarg.0
    IL_00c7:  ldloc.1
    IL_00c8:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00cd:  ldarg.0
    IL_00ce:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00d3:  ldloca.s   V_1
    IL_00d5:  ldarg.0
    IL_00d6:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_2_Call1(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_2_Call1(Of SM$T))"
    IL_00db:  leave      IL_016e
    IL_00e0:  ldarg.0
    IL_00e1:  ldc.i4.m1
    IL_00e2:  dup
    IL_00e3:  stloc.0
    IL_00e4:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_00e9:  ldarg.0
    IL_00ea:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00ef:  stloc.1
    IL_00f0:  ldarg.0
    IL_00f1:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00f6:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00fc:  ldarg.0
    IL_00fd:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$S1 As Integer"
    IL_0102:  ldloca.s   V_1
    IL_0104:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_0109:  ldloca.s   V_1
    IL_010b:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0111:  call       "Sub Program.M(ByRef Integer, Integer)"
    IL_0116:  ldarg.0
    IL_0117:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_011c:  ldarg.0
    IL_011d:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$S0 As Integer"
    IL_0122:  ldarg.0
    IL_0123:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$S1 As Integer"
    IL_0128:  constrained. "SM$T"
    IL_012e:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
    IL_0133:  leave.s    IL_0159
  }
  catch System.Exception
  {
    IL_0135:  dup
    IL_0136:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_013b:  stloc.s    V_4
    IL_013d:  ldarg.0
    IL_013e:  ldc.i4.s   -2
    IL_0140:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0145:  ldarg.0
    IL_0146:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_014b:  ldloc.s    V_4
    IL_014d:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_0152:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_0157:  leave.s    IL_016e
  }
  IL_0159:  ldarg.0
  IL_015a:  ldc.i4.s   -2
  IL_015c:  dup
  IL_015d:  stloc.0
  IL_015e:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_0163:  ldarg.0
  IL_0164:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_0169:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_016e:  ret
}
]]>)

            'Wrong IL
            verifier.VerifyIL("Program.VB$StateMachine_3_Call2(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      367 (0x16f)
  .maxstack  5
  .locals init (Integer V_0,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_1,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_2,
                Integer V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0053
    IL_000a:  ldloc.0
    IL_000b:  ldc.i4.1
    IL_000c:  beq        IL_00e0
    IL_0011:  ldarg.0
    IL_0012:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_0017:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_001c:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_0021:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0026:  stloc.2
    IL_0027:  ldloca.s   V_2
    IL_0029:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_002e:  brtrue.s   IL_006f
    IL_0030:  ldarg.0
    IL_0031:  ldc.i4.0
    IL_0032:  dup
    IL_0033:  stloc.0
    IL_0034:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_0039:  ldarg.0
    IL_003a:  ldloc.2
    IL_003b:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0040:  ldarg.0
    IL_0041:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_0046:  ldloca.s   V_2
    IL_0048:  ldarg.0
    IL_0049:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_3_Call2(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_3_Call2(Of SM$T))"
    IL_004e:  leave      IL_016e
    IL_0053:  ldarg.0
    IL_0054:  ldc.i4.m1
    IL_0055:  dup
    IL_0056:  stloc.0
    IL_0057:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_005c:  ldarg.0
    IL_005d:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0062:  stloc.2
    IL_0063:  ldarg.0
    IL_0064:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0069:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_006f:  ldarg.0
    IL_0070:  ldarg.0
    IL_0071:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_0076:  ldarg.0
    IL_0077:  ldloca.s   V_2
    IL_0079:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_007e:  ldloca.s   V_2
    IL_0080:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0086:  dup
    IL_0087:  stloc.3
    IL_0088:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$S0 As Integer"
    IL_008d:  ldloc.3
    IL_008e:  constrained. "SM$T"
    IL_0094:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
    IL_0099:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$S1 As Integer"
    IL_009e:  ldarg.0
    IL_009f:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_00a4:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_00a9:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_00ae:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00b3:  stloc.1
    IL_00b4:  ldloca.s   V_1
    IL_00b6:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_00bb:  brtrue.s   IL_00fc
    IL_00bd:  ldarg.0
    IL_00be:  ldc.i4.1
    IL_00bf:  dup
    IL_00c0:  stloc.0
    IL_00c1:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_00c6:  ldarg.0
    IL_00c7:  ldloc.1
    IL_00c8:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00cd:  ldarg.0
    IL_00ce:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00d3:  ldloca.s   V_1
    IL_00d5:  ldarg.0
    IL_00d6:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_3_Call2(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_3_Call2(Of SM$T))"
    IL_00db:  leave      IL_016e
    IL_00e0:  ldarg.0
    IL_00e1:  ldc.i4.m1
    IL_00e2:  dup
    IL_00e3:  stloc.0
    IL_00e4:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_00e9:  ldarg.0
    IL_00ea:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00ef:  stloc.1
    IL_00f0:  ldarg.0
    IL_00f1:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00f6:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00fc:  ldarg.0
    IL_00fd:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$S1 As Integer"
    IL_0102:  ldloca.s   V_1
    IL_0104:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_0109:  ldloca.s   V_1
    IL_010b:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0111:  call       "Sub Program.M(ByRef Integer, Integer)"
    IL_0116:  ldarg.0
    IL_0117:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$VB$Local_item As SM$T"
    IL_011c:  ldarg.0
    IL_011d:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$S0 As Integer"
    IL_0122:  ldarg.0
    IL_0123:  ldfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$S1 As Integer"
    IL_0128:  constrained. "SM$T"
    IL_012e:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
    IL_0133:  leave.s    IL_0159
  }
  catch System.Exception
  {
    IL_0135:  dup
    IL_0136:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_013b:  stloc.s    V_4
    IL_013d:  ldarg.0
    IL_013e:  ldc.i4.s   -2
    IL_0140:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
    IL_0145:  ldarg.0
    IL_0146:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_014b:  ldloc.s    V_4
    IL_014d:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_0152:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_0157:  leave.s    IL_016e
  }
  IL_0159:  ldarg.0
  IL_015a:  ldc.i4.s   -2
  IL_015c:  dup
  IL_015d:  stloc.0
  IL_015e:  stfld      "Program.VB$StateMachine_3_Call2(Of SM$T).$State As Integer"
  IL_0163:  ldarg.0
  IL_0164:  ldflda     "Program.VB$StateMachine_3_Call2(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_0169:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_016e:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(63221, "https://github.com/dotnet/roslyn/issues/63221")>
        Public Sub GenericTypeParameterAsReceiver_CopyBack_Indexer_Struct_IndexAndValue_Async_04()
            Dim comp =
<compilation>
    <file>
Imports System
Imports System.Threading.Tasks

Interface IMoveable
    Property Position(x As Integer) As Integer
End Interface

Structure Item
    Implements IMoveable

    Public Property Name As String

    Public Property Position(x As Integer) As Integer Implements IMoveable.Position
        Get
            Console.WriteLine("Position get for item '{0}'", Me.Name)
            Return 0
        End Get
        Set
            Console.WriteLine("Position set for item '{0}'", Me.Name)
        End Set
    End Property
End Structure

Class Program
    Shared Sub Main()
        Dim item1 = New Item With {.Name = "1"}
        Call1(item1).Wait()

        Dim item2 = New Item With {.Name = "2"}
        Call2(item2).Wait()
    End Sub

    Private Shared Async Function Call1(Of T As {Structure, IMoveable})(item As T) As Task
        M(item.Position(await GetOffsetAsync(GetOffset(item))), await GetOffsetAsync(GetOffset(item)))
    End Function

    Private Shared Async Function Call2(Of T As {IMoveable})(item As T) As Task
        M(item.Position(await GetOffsetAsync(GetOffset(item))), await GetOffsetAsync(GetOffset(item)))
    End Function

    Shared value as Integer

    Shared Function GetOffset(Of T)(ByRef item As T) As Integer
        value -= 1
        item = DirectCast(DirectCast(New Item With {.Name = value.ToString()}, IMoveable), T)
        Return 0
    End Function

    Shared Function GetOffsetAsync(i As Integer) As Task(Of Integer)
        Return Task.FromResult(i)
    End Function

    Shared Sub M(ByRef x As Integer, y As Integer)
        x += y
    End Sub
End Class
    </file>
</compilation>

            Dim verifier = CompileAndVerifyEx(comp, targetFramework:=TargetFramework.StandardAndVBRuntime, options:=TestOptions.ReleaseExe, expectedOutput:=
"
Position get for item '-1'
Position set for item '-2'
Position get for item '-3'
Position set for item '-4'
").VerifyDiagnostics()

            verifier.VerifyIL("Program.VB$StateMachine_2_Call1(Of SM$T).MoveNext()",
            <![CDATA[
{
  // Code size      367 (0x16f)
  .maxstack  5
  .locals init (Integer V_0,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_1,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_2,
                Integer V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0053
    IL_000a:  ldloc.0
    IL_000b:  ldc.i4.1
    IL_000c:  beq        IL_00e0
    IL_0011:  ldarg.0
    IL_0012:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0017:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_001c:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_0021:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0026:  stloc.2
    IL_0027:  ldloca.s   V_2
    IL_0029:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_002e:  brtrue.s   IL_006f
    IL_0030:  ldarg.0
    IL_0031:  ldc.i4.0
    IL_0032:  dup
    IL_0033:  stloc.0
    IL_0034:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0039:  ldarg.0
    IL_003a:  ldloc.2
    IL_003b:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0040:  ldarg.0
    IL_0041:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_0046:  ldloca.s   V_2
    IL_0048:  ldarg.0
    IL_0049:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_2_Call1(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_2_Call1(Of SM$T))"
    IL_004e:  leave      IL_016e
    IL_0053:  ldarg.0
    IL_0054:  ldc.i4.m1
    IL_0055:  dup
    IL_0056:  stloc.0
    IL_0057:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_005c:  ldarg.0
    IL_005d:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0062:  stloc.2
    IL_0063:  ldarg.0
    IL_0064:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0069:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_006f:  ldarg.0
    IL_0070:  ldarg.0
    IL_0071:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_0076:  ldarg.0
    IL_0077:  ldloca.s   V_2
    IL_0079:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_007e:  ldloca.s   V_2
    IL_0080:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0086:  dup
    IL_0087:  stloc.3
    IL_0088:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$S0 As Integer"
    IL_008d:  ldloc.3
    IL_008e:  constrained. "SM$T"
    IL_0094:  callvirt   "Function IMoveable.get_Position(Integer) As Integer"
    IL_0099:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$S1 As Integer"
    IL_009e:  ldarg.0
    IL_009f:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_00a4:  call       "Function Program.GetOffset(Of SM$T)(ByRef SM$T) As Integer"
    IL_00a9:  call       "Function Program.GetOffsetAsync(Integer) As System.Threading.Tasks.Task(Of Integer)"
    IL_00ae:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00b3:  stloc.1
    IL_00b4:  ldloca.s   V_1
    IL_00b6:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_00bb:  brtrue.s   IL_00fc
    IL_00bd:  ldarg.0
    IL_00be:  ldc.i4.1
    IL_00bf:  dup
    IL_00c0:  stloc.0
    IL_00c1:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_00c6:  ldarg.0
    IL_00c7:  ldloc.1
    IL_00c8:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00cd:  ldarg.0
    IL_00ce:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_00d3:  ldloca.s   V_1
    IL_00d5:  ldarg.0
    IL_00d6:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Program.VB$StateMachine_2_Call1(Of SM$T))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_2_Call1(Of SM$T))"
    IL_00db:  leave      IL_016e
    IL_00e0:  ldarg.0
    IL_00e1:  ldc.i4.m1
    IL_00e2:  dup
    IL_00e3:  stloc.0
    IL_00e4:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_00e9:  ldarg.0
    IL_00ea:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00ef:  stloc.1
    IL_00f0:  ldarg.0
    IL_00f1:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00f6:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00fc:  ldarg.0
    IL_00fd:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$S1 As Integer"
    IL_0102:  ldloca.s   V_1
    IL_0104:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_0109:  ldloca.s   V_1
    IL_010b:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0111:  call       "Sub Program.M(ByRef Integer, Integer)"
    IL_0116:  ldarg.0
    IL_0117:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$VB$Local_item As SM$T"
    IL_011c:  ldarg.0
    IL_011d:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$S0 As Integer"
    IL_0122:  ldarg.0
    IL_0123:  ldfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$S1 As Integer"
    IL_0128:  constrained. "SM$T"
    IL_012e:  callvirt   "Sub IMoveable.set_Position(Integer, Integer)"
    IL_0133:  leave.s    IL_0159
  }
  catch System.Exception
  {
    IL_0135:  dup
    IL_0136:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_013b:  stloc.s    V_4
    IL_013d:  ldarg.0
    IL_013e:  ldc.i4.s   -2
    IL_0140:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
    IL_0145:  ldarg.0
    IL_0146:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_014b:  ldloc.s    V_4
    IL_014d:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_0152:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_0157:  leave.s    IL_016e
  }
  IL_0159:  ldarg.0
  IL_015a:  ldc.i4.s   -2
  IL_015c:  dup
  IL_015d:  stloc.0
  IL_015e:  stfld      "Program.VB$StateMachine_2_Call1(Of SM$T).$State As Integer"
  IL_0163:  ldarg.0
  IL_0164:  ldflda     "Program.VB$StateMachine_2_Call1(Of SM$T).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_0169:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_016e:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(66135, "https://github.com/dotnet/roslyn/issues/66135")>
        Public Sub InvokeAddedStructToStringOverrideOnReadOnlyField()
            Dim libOrig_vb =
<compilation>
    <file>
Public Structure S
    Public i As Integer

    Public Sub Report()
        Throw New System.Exception()
    End Sub
End Structure
    </file>
</compilation>
            Dim libOrig = CreateCompilation(libOrig_vb, assemblyName:="lib")
            libOrig.AssertTheseDiagnostics()

            Dim libChanged_vb =
<compilation>
    <file>
Public Structure S
    Public i As Integer

    Public Overrides Function ToString() As String
        Dim result = i.ToString()
        i = i + 1
        Return result
    End Function

    Public Sub Report()
        System.Console.Write("RAN ")
    End Sub
End Structure
    </file>
</compilation>
            Dim libChanged = CreateCompilation(libChanged_vb, assemblyName:="lib")
            libChanged.AssertTheseDiagnostics()

            Dim libUser_vb =
<compilation>
    <file>
Public Class C
    Public ReadOnly field As S

    Public Sub New(s As S)
        field = s
    End Sub

    Public Sub M()
        System.Console.Write(field.ToString())
        System.Console.Write(field.ToString())
    End Sub
End Class
    </file>
</compilation>
            Dim libUser = CreateCompilation(libUser_vb, references:={libOrig.EmitToImageReference()})
            libUser.AssertTheseDiagnostics()

            CompileAndVerify(libUser).VerifyIL("C.M", <![CDATA[
{
  // Code size       51 (0x33)
  .maxstack  1
  .locals init (S V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "C.field As S"
  IL_0006:  stloc.0
  IL_0007:  ldloca.s   V_0
  IL_0009:  constrained. "S"
  IL_000f:  callvirt   "Function System.ValueType.ToString() As String"
  IL_0014:  call       "Sub System.Console.Write(String)"
  IL_0019:  ldarg.0
  IL_001a:  ldfld      "C.field As S"
  IL_001f:  stloc.0
  IL_0020:  ldloca.s   V_0
  IL_0022:  constrained. "S"
  IL_0028:  callvirt   "Function System.ValueType.ToString() As String"
  IL_002d:  call       "Sub System.Console.Write(String)"
  IL_0032:  ret
}
]]>)

            Dim src =
<compilation>
    <file>
Class D
    Public Shared Sub Main()
        Dim s = New S()
        s.Report()
        Dim c = New C(s)
        c.M()
    End Sub
End Class
    </file>
</compilation>
            Dim comp = CreateCompilation(src, references:={libChanged.EmitToImageReference(), libUser.EmitToImageReference()}, options:=TestOptions.DebugExe)
            comp.AssertTheseDiagnostics()
            CompileAndVerify(comp, expectedOutput:="RAN 00")
        End Sub

        <Fact>
        <WorkItem(66135, "https://github.com/dotnet/roslyn/issues/66135")>
        Public Sub InvokeAddedStructToStringOverrideOnField()
            Dim libOrig_vb =
<compilation>
    <file>
Public Structure S
    Public i As Integer

    Public Sub Report()
        Throw New System.Exception()
    End Sub
End Structure
    </file>
</compilation>
            Dim libOrig = CreateCompilation(libOrig_vb, assemblyName:="lib")
            libOrig.AssertTheseDiagnostics()

            Dim libChanged_vb =
<compilation>
    <file>
Public Structure S
    Public i As Integer

    Public Overrides Function ToString() As String
        Dim result = i.ToString()
        i = i + 1
        Return result
    End Function

    Public Sub Report()
        System.Console.Write("RAN ")
    End Sub
End Structure
    </file>
</compilation>
            Dim libChanged = CreateCompilation(libChanged_vb, assemblyName:="lib")
            libChanged.AssertTheseDiagnostics()

            Dim libUser_vb =
<compilation>
    <file>
Public Class C
    Public field As S

    Public Sub New(s As S)
        field = s
    End Sub

    Public Sub M()
        System.Console.Write(field.ToString())
        System.Console.Write(field.ToString())
    End Sub
End Class
    </file>
</compilation>
            Dim libUser = CreateCompilation(libUser_vb, references:={libOrig.EmitToImageReference()})
            libUser.AssertTheseDiagnostics()

            CompileAndVerify(libUser).VerifyIL("C.M", <![CDATA[
{
  // Code size       45 (0x2d)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldflda     "C.field As S"
  IL_0006:  constrained. "S"
  IL_000c:  callvirt   "Function System.ValueType.ToString() As String"
  IL_0011:  call       "Sub System.Console.Write(String)"
  IL_0016:  ldarg.0
  IL_0017:  ldflda     "C.field As S"
  IL_001c:  constrained. "S"
  IL_0022:  callvirt   "Function System.ValueType.ToString() As String"
  IL_0027:  call       "Sub System.Console.Write(String)"
  IL_002c:  ret
}
]]>)

            Dim src =
<compilation>
    <file>
Class D
    Public Shared Sub Main()
        Dim s = New S()
        s.Report()
        Dim c = New C(s)
        c.M()
    End Sub
End Class
    </file>
</compilation>
            Dim comp = CreateCompilation(src, references:={libChanged.EmitToImageReference(), libUser.EmitToImageReference()}, options:=TestOptions.DebugExe)
            comp.AssertTheseDiagnostics()
            CompileAndVerify(comp, expectedOutput:="RAN 01")
        End Sub

        <Fact>
        <WorkItem(66135, "https://github.com/dotnet/roslyn/issues/66135")>
        Public Sub InvokeStructToAddedStringOverrideOnRefParameter()
            Dim libOrig_vb =
<compilation>
    <file>
Public Structure S
    Public i As Integer

    Public Sub Report()
        Throw New System.Exception()
    End Sub
End Structure
    </file>
</compilation>
            Dim libOrig = CreateCompilation(libOrig_vb, assemblyName:="lib")
            libOrig.AssertTheseDiagnostics()

            Dim libChanged_vb =
<compilation>
    <file>
Public Structure S
    Public i As Integer

    Public Overrides Function ToString() As String
        Dim result = i.ToString()
        i = i + 1
        Return result
    End Function

    Public Sub Report()
        System.Console.Write("RAN ")
    End Sub
End Structure
    </file>
</compilation>
            Dim libChanged = CreateCompilation(libChanged_vb, assemblyName:="lib")
            libChanged.AssertTheseDiagnostics()

            Dim libUser_vb =
<compilation>
    <file>
Public Class C
    Public Sub M(ByRef s As S)
        System.Console.Write(s.ToString())
        System.Console.Write(s.ToString())
    End Sub
End Class
    </file>
</compilation>
            Dim libUser = CreateCompilation(libUser_vb, references:={libOrig.EmitToImageReference()})
            libUser.AssertTheseDiagnostics()

            CompileAndVerify(libUser).VerifyIL("C.M", <![CDATA[
{
  // Code size       35 (0x23)
  .maxstack  1
  IL_0000:  ldarg.1
  IL_0001:  constrained. "S"
  IL_0007:  callvirt   "Function System.ValueType.ToString() As String"
  IL_000c:  call       "Sub System.Console.Write(String)"
  IL_0011:  ldarg.1
  IL_0012:  constrained. "S"
  IL_0018:  callvirt   "Function System.ValueType.ToString() As String"
  IL_001d:  call       "Sub System.Console.Write(String)"
  IL_0022:  ret
}
]]>)

            Dim src =
<compilation>
    <file>
Class D
    Public Shared Sub Main()
        Dim s = New S()
        s.Report()
        Dim c = New C()
        c.M(s)
    End Sub
End Class
    </file>
</compilation>
            Dim comp = CreateCompilation(src, references:={libChanged.EmitToImageReference(), libUser.EmitToImageReference()}, options:=TestOptions.DebugExe)
            comp.AssertTheseDiagnostics()
            CompileAndVerify(comp, expectedOutput:="RAN 01")
        End Sub

        <Fact>
        <WorkItem(66135, "https://github.com/dotnet/roslyn/issues/66135")>
        Public Sub ForEachOnReadOnlyField()
            Dim src =
<compilation>
    <file>
Imports System.Collections
Imports System.Collections.Generic

Class C
    Public Shared Sub Main()
        Dim d = New D()
        d.M()
        d.M()
    End Sub
End Class

Class D
    ReadOnly field As S

    Public Sub M()
        For Each x In field
        Next

        System.Console.Write(field.ToString())
    End Sub
End Class

Structure S
    Implements IEnumerable(Of Integer)

    Public a As Integer

    Public Overrides Function ToString() As String
        Return a.ToString()
    End Function

    Private Iterator Function GetEnumerator() As IEnumerator(Of Integer) _
        Implements IEnumerable(Of Integer).GetEnumerator

        a = a + 1
        Yield 1
    End Function

    Private Function GetEnumerator2() As IEnumerator _
        Implements IEnumerable.GetEnumerator

        Return GetEnumerator()
    End Function
End Structure
    </file>
</compilation>
            Dim comp = CreateCompilation(src, options:=TestOptions.DebugExe)
            comp.AssertTheseDiagnostics()
            CompileAndVerify(comp, expectedOutput:="00")

            CompileAndVerify(comp).VerifyIL("D.M", <![CDATA[
{
  // Code size       83 (0x53)
  .maxstack  1
  .locals init (System.Collections.Generic.IEnumerator(Of Integer) V_0,
                Integer V_1, //x
                Boolean V_2,
                S V_3)
  IL_0000:  nop
  .try
  {
    IL_0001:  ldarg.0
    IL_0002:  ldfld      "D.field As S"
    IL_0007:  box        "S"
    IL_000c:  castclass  "System.Collections.Generic.IEnumerable(Of Integer)"
    IL_0011:  callvirt   "Function System.Collections.Generic.IEnumerable(Of Integer).GetEnumerator() As System.Collections.Generic.IEnumerator(Of Integer)"
    IL_0016:  stloc.0
    IL_0017:  br.s       IL_0021
    IL_0019:  ldloc.0
    IL_001a:  callvirt   "Function System.Collections.Generic.IEnumerator(Of Integer).get_Current() As Integer"
    IL_001f:  stloc.1
    IL_0020:  nop
    IL_0021:  ldloc.0
    IL_0022:  callvirt   "Function System.Collections.IEnumerator.MoveNext() As Boolean"
    IL_0027:  stloc.2
    IL_0028:  ldloc.2
    IL_0029:  brtrue.s   IL_0019
    IL_002b:  leave.s    IL_0038
  }
  finally
  {
    IL_002d:  ldloc.0
    IL_002e:  brfalse.s  IL_0037
    IL_0030:  ldloc.0
    IL_0031:  callvirt   "Sub System.IDisposable.Dispose()"
    IL_0036:  nop
    IL_0037:  endfinally
  }
  IL_0038:  ldarg.0
  IL_0039:  ldfld      "D.field As S"
  IL_003e:  stloc.3
  IL_003f:  ldloca.s   V_3
  IL_0041:  constrained. "S"
  IL_0047:  callvirt   "Function Object.ToString() As String"
  IL_004c:  call       "Sub System.Console.Write(String)"
  IL_0051:  nop
  IL_0052:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(66135, "https://github.com/dotnet/roslyn/issues/66135")>
        Public Sub ForEachOnField()
            Dim src =
<compilation>
    <file>
Imports System.Collections
Imports System.Collections.Generic

Class C
    Public Shared Sub Main()
        Dim d = New D()
        d.M()
        d.M()
    End Sub
End Class

Class D
    Dim field As S

    Public Sub M()
        For Each x In field
        Next

        System.Console.Write(field.ToString())
    End Sub
End Class

Structure S
    Implements IEnumerable(Of Integer)

    Public a As Integer

    Public Overrides Function ToString() As String
        Return a.ToString()
    End Function

    Private Iterator Function GetEnumerator() As IEnumerator(Of Integer) _
        Implements IEnumerable(Of Integer).GetEnumerator

        a = a + 1
        Yield 1
    End Function

    Private Function GetEnumerator2() As IEnumerator _
        Implements IEnumerable.GetEnumerator

        Return GetEnumerator()
    End Function
End Structure
    </file>
</compilation>
            Dim comp = CreateCompilation(src, options:=TestOptions.DebugExe)
            comp.AssertTheseDiagnostics()
            CompileAndVerify(comp, expectedOutput:="00")

            CompileAndVerify(comp).VerifyIL("D.M", <![CDATA[
{
  // Code size       80 (0x50)
  .maxstack  1
  .locals init (System.Collections.Generic.IEnumerator(Of Integer) V_0,
                Integer V_1, //x
                Boolean V_2)
  IL_0000:  nop
  .try
  {
    IL_0001:  ldarg.0
    IL_0002:  ldfld      "D.field As S"
    IL_0007:  box        "S"
    IL_000c:  castclass  "System.Collections.Generic.IEnumerable(Of Integer)"
    IL_0011:  callvirt   "Function System.Collections.Generic.IEnumerable(Of Integer).GetEnumerator() As System.Collections.Generic.IEnumerator(Of Integer)"
    IL_0016:  stloc.0
    IL_0017:  br.s       IL_0021
    IL_0019:  ldloc.0
    IL_001a:  callvirt   "Function System.Collections.Generic.IEnumerator(Of Integer).get_Current() As Integer"
    IL_001f:  stloc.1
    IL_0020:  nop
    IL_0021:  ldloc.0
    IL_0022:  callvirt   "Function System.Collections.IEnumerator.MoveNext() As Boolean"
    IL_0027:  stloc.2
    IL_0028:  ldloc.2
    IL_0029:  brtrue.s   IL_0019
    IL_002b:  leave.s    IL_0038
  }
  finally
  {
    IL_002d:  ldloc.0
    IL_002e:  brfalse.s  IL_0037
    IL_0030:  ldloc.0
    IL_0031:  callvirt   "Sub System.IDisposable.Dispose()"
    IL_0036:  nop
    IL_0037:  endfinally
  }
  IL_0038:  ldarg.0
  IL_0039:  ldflda     "D.field As S"
  IL_003e:  constrained. "S"
  IL_0044:  callvirt   "Function Object.ToString() As String"
  IL_0049:  call       "Sub System.Console.Write(String)"
  IL_004e:  nop
  IL_004f:  ret
}
]]>)

        End Sub

    End Class
End Namespace
