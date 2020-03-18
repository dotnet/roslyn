' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class CodeGenGoto
        Inherits BasicTestBase

        <Fact()>
        Public Sub GotoStatements()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System        
Module M1

    Sub Main()

    ' detour by goto without statements gets optimized away
    goto l2
l1:
    goto l3
l2:
        Console.WriteLine("jumped to l2 in if")
        GoTo l1
l3:


    ' detour by goto with statements does not get optimized away
    goto l5
l4:
    Console.WriteLine("jumped to l4 in if")
    goto l6
l5:
        Console.WriteLine("jumped to l5 in if")
        GoTo l4
l6:
        End Sub


End Module
    </file>
</compilation>,
expectedOutput:=<![CDATA[
jumped to l2 in if
jumped to l5 in if
jumped to l4 in if
]]>).
            VerifyIL("M1.Main",
            <![CDATA[
{
  // Code size       35 (0x23)
  .maxstack  1
  IL_0000:  ldstr      "jumped to l2 in if"
  IL_0005:  call       "Sub System.Console.WriteLine(String)"
  IL_000a:  br.s       IL_0017
  IL_000c:  ldstr      "jumped to l4 in if"
  IL_0011:  call       "Sub System.Console.WriteLine(String)"
  IL_0016:  ret
  IL_0017:  ldstr      "jumped to l5 in if"
  IL_001c:  call       "Sub System.Console.WriteLine(String)"
  IL_0021:  br.s       IL_000c
}
]]>)
        End Sub

        <Fact()>
        Public Sub GotoIntoIfStatement()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System        
Module M1

    Sub Main()
        GoTo l1

        If False Then
l1:
            Console.WriteLine("jumped to l1 in if")
        End If


        dim x = 0
        GoTo l3
l2:
        If False Then
l3:
            Console.WriteLine("jumped to l3 in if")
        End If

        if x &lt; 1 then
            x = x + 1
            GoTo l2
        end if
        End Sub

End Module
    </file>
</compilation>,
expectedOutput:=<![CDATA[
jumped to l1 in if
jumped to l3 in if
]]>).
            VerifyIL("M1.Main",
            <![CDATA[
{
  // Code size       33 (0x21)
  .maxstack  2
  .locals init (Integer V_0) //x
  IL_0000:  ldstr      "jumped to l1 in if"
  IL_0005:  call       "Sub System.Console.WriteLine(String)"
  IL_000a:  ldc.i4.0
  IL_000b:  stloc.0
  IL_000c:  ldstr      "jumped to l3 in if"
  IL_0011:  call       "Sub System.Console.WriteLine(String)"
  IL_0016:  ldloc.0
  IL_0017:  ldc.i4.1
  IL_0018:  bge.s      IL_0020
  IL_001a:  ldloc.0
  IL_001b:  ldc.i4.1
  IL_001c:  add.ovf
  IL_001d:  stloc.0
  IL_001e:  br.s       IL_0016
  IL_0020:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub NumericLabel()
            Dim source =
<compilation>
    <file name="a.vb">
Module M
    Sub Main()
0:      GoTo 200
100: GoTo 300
200: GoTo 100
300: Return
    End Sub
End Module
    </file>
</compilation>

            CompileAndVerify(source).VerifyIL("M.Main", <![CDATA[
{
 // Code size        1 (0x1)
  .maxstack  0
  IL_0000:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub GotoIf()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System
Module Program
    Sub Main(args As String())
        Dim Flag1 = 0
        GoTo Label9
        Flag1 = 2
Label9: If (2 > 3) Then
            Flag1 = 1
        Else
            Flag1 = 2
        End If
        Console.Write(Flag1)
    End Sub
End Module
    </file>
</compilation>

            CompileAndVerify(source,
                            expectedOutput:="2").VerifyIL("Program.Main", <![CDATA[
{
 // Code size       11 (0xb)
  .maxstack  1
  .locals init (Integer V_0) //Flag1
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  IL_0002:  ldc.i4.2
  IL_0003:  stloc.0
  IL_0004:  ldloc.0
  IL_0005:  call       "Sub System.Console.Write(Integer)"
  IL_000a:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub GotoThen()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System
Module Program
    Sub Main(args As String())
        Dim Flag1 = 0
        GoTo Label9
        Flag1 = 2
        If (2 > 3) Then
Label9:
            Flag1 = 1
        Else
            Flag1 = 2
        End If
        Console.Write(Flag1)
    End Sub
End Module
    </file>
</compilation>

            CompileAndVerify(source,
                            expectedOutput:="1").VerifyIL("Program.Main", <![CDATA[
{
 // Code size       11 (0xb)
  .maxstack  1
  .locals init (Integer V_0) //Flag1
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  IL_0002:  ldc.i4.1
  IL_0003:  stloc.0
  IL_0004:  ldloc.0
  IL_0005:  call       "Sub System.Console.Write(Integer)"
  IL_000a:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub GotoElse()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System
Module M
    Sub Main()
        Dim Flag1 = 1
        GoTo 100
        If Flag1 = 1 Then
            Flag1 = 100
        Else
100: Flag1 = 200
        End If
        Console.Write(Flag1)
    End Sub
End Module
    </file>
</compilation>

            CompileAndVerify(source,
                            expectedOutput:="200").VerifyIL("M.Main", <![CDATA[
{
 // Code size       15 (0xf)
  .maxstack  2
  .locals init (Integer V_0) //Flag1
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  IL_0002:  ldc.i4     0xc8
  IL_0007:  stloc.0
  IL_0008:  ldloc.0
  IL_0009:  call       "Sub System.Console.Write(Integer)"
  IL_000e:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub GotoElse_1()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System        
Module M
    Sub Main()
        Dim Flag1 = 1
        GoTo 100
        If Flag1 = 1 Then
            Flag1 = 100
100:    Else : Flag1 = 200
        End If
        Console.Write(Flag1)
    End Sub
End Module
    </file>
</compilation>

            CompileAndVerify(source,
                            expectedOutput:="1").VerifyIL("M.Main", <![CDATA[
{
 // Code size        9 (0x9)
  .maxstack  2
  .locals init (Integer V_0) //Flag1
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  call       "Sub System.Console.Write(Integer)"
  IL_0008:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub GotoElseIf()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System
Module M
    Sub Main()
        Dim Flag1 = 0
        GoTo Label11
        If (5 > 7) Then
            Flag1 = 2
        ElseIf (1 = 1) Then
Label11:
            Flag1 = 1
        Else
            Flag1 = 2
        End If
        Console.WriteLine(Flag1)
    End Sub
End Module
    </file>
</compilation>

            CompileAndVerify(source,
                            expectedOutput:="1").VerifyIL("M.Main", <![CDATA[
{
 // Code size       11 (0xb)
  .maxstack  1
  .locals init (Integer V_0) //Flag1
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  IL_0002:  ldc.i4.1
  IL_0003:  stloc.0
  IL_0004:  ldloc.0
  IL_0005:  call       "Sub System.Console.WriteLine(Integer)"
  IL_000a:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub GotoElseIf_1()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System
Module M
    Sub Main()
        Dim Flag1 = 0
        GoTo Label11
        If (5 > 7) Then
            Flag1 = 2
Label11: ElseIf (1 = 1) Then
            Flag1 = 1
        Else
            Flag1 = 2
        End If
        Console.WriteLine(Flag1)
    End Sub
End Module
    </file>
</compilation>

            CompileAndVerify(source,
                            expectedOutput:="0").VerifyIL("M.Main", <![CDATA[
{
 // Code size        9 (0x9)
  .maxstack  1
  .locals init (Integer V_0) //Flag1
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0008:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub GotoInCase()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System
Module M
    Sub Main()
        Dim Flag1 = 0
        Select Case Flag1
            Case 1
                GoTo 1
            Case 2
                GoTo 2
            Case 3
                GoTo 3
        End Select
1:
        GoTo 110
2:
        GoTo 110
3:
        GoTo 110
110:
    End Sub
End Module
    </file>
</compilation>

            CompileAndVerify(source).VerifyIL("M.Main", <![CDATA[
{
  // Code size       23 (0x17)
  .maxstack  2
  .locals init (Integer V_0) //Flag1
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  ldc.i4.1
  IL_0004:  sub
  IL_0005:  switch    (
  IL_0016,
  IL_0016,
  IL_0016)
  IL_0016:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub LabelOnCase()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System
Module M
    Sub Main()
        Dim doublevar As Double = 1.0
        GoTo L100
        Select Case doublevar
            Case 1
                Console.WriteLine("In case 1")
                Return
L100:       Case 2.25
                Console.WriteLine("In case 2.25")
                GoTo L200
L200:       Case Else
                Console.WriteLine("In case else")
        End Select
    End Sub
End Module
    </file>
</compilation>

            CompileAndVerify(source).VerifyIL("M.Main", <![CDATA[
{
 // Code size       11 (0xb)
  .maxstack  2
  .locals init (Double V_0, //doublevar
  Double V_1)
  IL_0000:  ldc.r8     1
  IL_0009:  stloc.0
  IL_000a:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub ComplexNestedIf()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System
Module M
    Public Sub Main()
        Dim i As Integer = 3
        If True Then
label1:
            GoTo label3
            If Not False Then
label2:
                GoTo label5
                If i > 2 Then
label3:
                    GoTo label2
                    If i = 3 Then
label4:
                        If i &lt; 5 Then
label5:
            If i = 4 Then
            Else
                System.Console.WriteLine("a")
            End If
                        End If
                    End If
                End If
            End If
        End If
        End Sub
End Module
    </file>
</compilation>

            CompileAndVerify(source, expectedOutput:="a")
        End Sub

        <Fact()>
        Public Sub InfiniteLoop()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System
Module M
    Sub Main()
        A:
        GoTo B
        B:
        GoTo A
    End Sub
End Module
    </file>
</compilation>

            CompileAndVerify(source).VerifyIL("M.Main", <![CDATA[
{
 // Code size        2 (0x2)
  .maxstack  0
  IL_0000:  br.s       IL_0000
}
]]>)
        End Sub

        <Fact()>
        Public Sub InfiniteLoop_1()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System
Module M
    Sub Main()
l1:
        Dim b As Integer
        GoTo l1
    End Sub
End Module
    </file>
</compilation>

            CompileAndVerify(source).VerifyIL("M.Main", <![CDATA[
{
 // Code size        2 (0x2)
  .maxstack  0
  IL_0000:  br.s       IL_0000
}
]]>)
        End Sub

        ' Finally is executed while use 'goto' to exit try block
        <Fact()>
        Public Sub BranchOutFromTry()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System
Module M
    Sub Main()
        Dim i As Integer = 0
        Try
            i = 1
            GoTo lab1
        Catch
            i = 2
        Finally
            i = 3
        End Try
lab1:
        Console.WriteLine(i)
    End Sub
End Module
    </file>
</compilation>

            CompileAndVerify(source, expectedOutput:="3").VerifyIL("M.Main", <![CDATA[
{
 // Code size       30 (0x1e)
  .maxstack  1
  .locals init (Integer V_0) //i
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  .try
{
  .try
{
  IL_0002:  ldc.i4.1
  IL_0003:  stloc.0
  IL_0004:  leave.s    IL_0017
}
  catch System.Exception
{
  IL_0006:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
  IL_000b:  ldc.i4.2
  IL_000c:  stloc.0
  IL_000d:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
  IL_0012:  leave.s    IL_0017
}
}
  finally
{
  IL_0014:  ldc.i4.3
  IL_0015:  stloc.0
  IL_0016:  endfinally
}
  IL_0017:  ldloc.0
  IL_0018:  call       "Sub System.Console.WriteLine(Integer)"
  IL_001d:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub GotoInFinally()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System
Module M
    Sub Main()
        Dim i As Integer = 0
        Try
            i = 1
        Catch
            i = 2
        Finally
lab1:
            i += 3
            GoTo lab1
        End Try
    End Sub
End Module
    </file>
</compilation>

            CompileAndVerify(source).VerifyIL("M.Main", <![CDATA[
{
  // Code size       28 (0x1c)
  .maxstack  2
  .locals init (Integer V_0) //i
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  .try
{
  .try
{
  IL_0002:  ldc.i4.1
  IL_0003:  stloc.0
  IL_0004:  leave.s    IL_001a
}
  catch System.Exception
{
  IL_0006:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
  IL_000b:  ldc.i4.2
  IL_000c:  stloc.0
  IL_000d:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
  IL_0012:  leave.s    IL_001a
}
}
  finally
{
  IL_0014:  ldloc.0
  IL_0015:  ldc.i4.3
  IL_0016:  add.ovf
  IL_0017:  stloc.0
  IL_0018:  br.s       IL_0014
}
  IL_001a:  br.s       IL_001a
}
]]>)
        End Sub

        <Fact()>
        Public Sub BranchOutFromInnerTryToOut()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System
Class c1
    Shared Sub Main()
        Try
            Dim c As c1
            Try
                GoTo label
                c = New c1
            Catch e As Exception
            Finally
                Console.WriteLine("inner Try")
            End Try
        Catch
        Finally
            Console.WriteLine("outer Try")
        End Try
label:
    End Sub
End Class
    </file>
</compilation>

            CompileAndVerify(source, expectedOutput:=<![CDATA[inner Try
outer Try]]>).VerifyIL("c1.Main", <![CDATA[
{
  // Code size       55 (0x37)
  .maxstack  2
  .locals init (System.Exception V_0) //e
  .try
  {
    .try
    {
      .try
      {
        .try
        {
          IL_0000:  leave.s    IL_0036
        }
        catch System.Exception
        {
          IL_0002:  dup
          IL_0003:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
          IL_0008:  stloc.0
          IL_0009:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
          IL_000e:  leave.s    IL_0010
        }
        IL_0010:  leave.s    IL_001d
      }
      finally
      {
        IL_0012:  ldstr      "inner Try"
        IL_0017:  call       "Sub System.Console.WriteLine(String)"
        IL_001c:  endfinally
      }
      IL_001d:  leave.s    IL_0036
    }
    catch System.Exception
    {
      IL_001f:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
      IL_0024:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
      IL_0029:  leave.s    IL_0036
    }
  }
  finally
  {
    IL_002b:  ldstr      "outer Try"
    IL_0030:  call       "Sub System.Console.WriteLine(String)"
    IL_0035:  endfinally
  }
  IL_0036:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub GotoInLambda()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System
Delegate Function del(i As Integer) As Integer
Class c1
    Shared Sub Main()
        Dim q As del = Function(x)
label2:
                           GoTo label1
label1:
        q(1)
                           GoTo label2
                       End Function
    End Sub
End Class
    </file>
</compilation>

            CompileAndVerify(source).VerifyIL("c1.Main", <![CDATA[
{
  // Code size       26 (0x1a)
  .maxstack  3
  .locals init (c1._Closure$__1-0 V_0) //$VB$Closure_0
  IL_0000:  ldloc.0
  IL_0001:  newobj     "Sub c1._Closure$__1-0..ctor(c1._Closure$__1-0)"
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  ldloc.0
  IL_0009:  ldftn      "Function c1._Closure$__1-0._Lambda$__0(Integer) As Integer"
  IL_000f:  newobj     "Sub del..ctor(Object, System.IntPtr)"
  IL_0014:  stfld      "c1._Closure$__1-0.$VB$Local_q As del"
  IL_0019:  ret
}
]]>)
        End Sub
    End Class
End Namespace
