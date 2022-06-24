' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class CodeGenTryCatchThrow
        Inherits BasicTestBase

        <Fact()>
        Public Sub TryCatchSimple()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System        
Module EmitTest

    Sub Main()
        dim x as integer = 0
        Try
            Console.Write("Try")
            x = x \ x
        Catch ex as Exception
            Console.Write("Catch" &amp; ex.GetType().Name)
        Finally
            Console.Write("Finally")
        End Try
    End Sub

End Module
    </file>
</compilation>,
expectedOutput:="TryCatchDivideByZeroExceptionFinally").
            VerifyIL("EmitTest.Main",
            <![CDATA[
{
  // Code size       70 (0x46)
  .maxstack  2
  .locals init (Integer V_0, //x
                System.Exception V_1) //ex
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  .try
  {
    .try
    {
      IL_0002:  ldstr      "Try"
      IL_0007:  call       "Sub System.Console.Write(String)"
      IL_000c:  ldloc.0
      IL_000d:  ldloc.0
      IL_000e:  div
      IL_000f:  stloc.0
      IL_0010:  leave.s    IL_0045
    }
    catch System.Exception
    {
      IL_0012:  dup
      IL_0013:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
      IL_0018:  stloc.1
      IL_0019:  ldstr      "Catch"
      IL_001e:  ldloc.1
      IL_001f:  callvirt   "Function System.Exception.GetType() As System.Type"
      IL_0024:  callvirt   "Function System.Reflection.MemberInfo.get_Name() As String"
      IL_0029:  call       "Function String.Concat(String, String) As String"
      IL_002e:  call       "Sub System.Console.Write(String)"
      IL_0033:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
      IL_0038:  leave.s    IL_0045
    }
  }
  finally
  {
    IL_003a:  ldstr      "Finally"
    IL_003f:  call       "Sub System.Console.Write(String)"
    IL_0044:  endfinally
  }
  IL_0045:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub GotoOutOfCatch()
            ' ILVerify: Leave into try block. { Offset = 55 }
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System        
Module EmitTest

    Sub Main()
        Dim x As Integer = 0
        Try
            Console.Write("Try")
            If x = 0 Then
                Throw New Exception
            End If

L1:
            x = 1
            Throw New Exception

        Catch ex As Exception
            If x > 0 Then
                GoTo L2
            End If

            Console.Write("GoTo")
            GoTo L1
L2:

            Console.Write("Catch")
            Return

        Finally
            Console.Write("Finally")
        End Try
    End Sub

End Module
    </file>
</compilation>,
verify:=Verification.FailsILVerify,
expectedOutput:="TryGoToCatchFinally").
            VerifyIL("EmitTest.Main",
            <![CDATA[
{
  // Code size       86 (0x56)
  .maxstack  2
  .locals init (Integer V_0, //x
           System.Exception V_1) //ex
  IL_0000:  ldc.i4.0  
  IL_0001:  stloc.0   
  .try
  {
    .try
    {
      IL_0002:  ldstr      "Try"
      IL_0007:  call       "Sub System.Console.Write(String)"
      IL_000c:  ldloc.0   
      IL_000d:  brtrue.s   IL_0015
      IL_000f:  newobj     "Sub System.Exception..ctor()"
      IL_0014:  throw     
      IL_0015:  ldc.i4.1  
      IL_0016:  stloc.0   
      IL_0017:  newobj     "Sub System.Exception..ctor()"
      IL_001c:  throw     
    }
    catch System.Exception
    {
      IL_001d:  dup       
      IL_001e:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
      IL_0023:  stloc.1   
      IL_0024:  ldloc.0   
      IL_0025:  ldc.i4.0  
      IL_0026:  bgt.s      IL_0039
      IL_0028:  ldstr      "GoTo"
      IL_002d:  call       "Sub System.Console.Write(String)"
      IL_0032:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
      IL_0037:  leave.s    IL_0015
      IL_0039:  ldstr      "Catch"
      IL_003e:  call       "Sub System.Console.Write(String)"
      IL_0043:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
      IL_0048:  leave.s    IL_0055
    }
  }
  finally
  {
    IL_004a:  ldstr      "Finally"
    IL_004f:  call       "Sub System.Console.Write(String)"
    IL_0054:  endfinally
  }
  IL_0055:  ret       
}
]]>)
        End Sub

        <Fact()>
        Public Sub TryCatchReuseLocal()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System        
Module EmitTest
    Sub Main()
        Dim x As Integer = 0

        Try
            'throw here
            x = x \ x
        Catch ex As Exception
            Dim exOrig As Exception = ex
            Try
                'throw here
                x = x \ x
            Catch ex
            End Try

            Console.Write(ex IsNot exOrig)
        End Try
    End Sub
End Module
    </file>
</compilation>,
expectedOutput:="True").
            VerifyIL("EmitTest.Main",
            <![CDATA[
{
  // Code size       57 (0x39)
  .maxstack  2
  .locals init (Integer V_0, //x
                System.Exception V_1, //ex
                System.Exception V_2) //exOrig
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  .try
  {
    IL_0002:  ldloc.0
    IL_0003:  ldloc.0
    IL_0004:  div
    IL_0005:  stloc.0
    IL_0006:  leave.s    IL_0038
  }
  catch System.Exception
  {
    IL_0008:  dup
    IL_0009:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_000e:  stloc.1
    IL_000f:  ldloc.1
    IL_0010:  stloc.2
    .try
    {
      IL_0011:  ldloc.0
      IL_0012:  ldloc.0
      IL_0013:  div
      IL_0014:  stloc.0
      IL_0015:  leave.s    IL_0025
    }
    catch System.Exception
    {
      IL_0017:  dup
      IL_0018:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
      IL_001d:  stloc.1
      IL_001e:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
      IL_0023:  leave.s    IL_0025
    }
    IL_0025:  ldloc.1
    IL_0026:  ldloc.2
    IL_0027:  ceq
    IL_0029:  ldc.i4.0
    IL_002a:  ceq
    IL_002c:  call       "Sub System.Console.Write(Boolean)"
    IL_0031:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_0036:  leave.s    IL_0038
  }
  IL_0038:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TryCatchByRef()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System        
Module EmitTest

    Sub Main()
        Dim saveUICulture = System.Threading.Thread.CurrentThread.CurrentUICulture
        System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture
        Try
            Dim ex As Exception
            goo(ex)
            Console.Write(ex.Message)
        Finally
            System.Threading.Thread.CurrentThread.CurrentUICulture = saveUICulture
        End Try
    End Sub

    Sub goo(ByRef ex As Exception)
        Dim x As Integer = 0
        Try
            x = x \ x
        Catch ex
        End Try
    End Sub
End Module
    </file>
</compilation>,
expectedOutput:="Attempted to divide by zero.").
            VerifyIL("EmitTest.goo",
            <![CDATA[
{
  // Code size       26 (0x1a)
  .maxstack  2
  .locals init (Integer V_0, //x
                System.Exception V_1)
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  .try
  {
    IL_0002:  ldloc.0
    IL_0003:  ldloc.0
    IL_0004:  div
    IL_0005:  stloc.0
    IL_0006:  leave.s    IL_0019
  }
  catch System.Exception
  {
    IL_0008:  dup
    IL_0009:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_000e:  stloc.1
    IL_000f:  ldarg.0
    IL_0010:  ldloc.1
    IL_0011:  stind.ref
    IL_0012:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_0017:  leave.s    IL_0019
  }
  IL_0019:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TryFilterSimple()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System        
Module EmitTest

    Function Filter as boolean
        Console.Write("Filter")
        return true
    End Function

    Sub Main()
        Dim x as integer = 0

        Try
            Console.Write("Try")
            x = x \ x
        Catch When Filter
            Console.Write("Catch")
        Finally
            Console.Write("Finally")
        End Try
    End Sub

End Module
    </file>
</compilation>,
expectedOutput:="TryFilterCatchFinally").
            VerifyIL("EmitTest.Main",
            <![CDATA[
{
  // Code size       75 (0x4b)
  .maxstack  2
  .locals init (Integer V_0) //x
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  .try
  {
    .try
    {
      IL_0002:  ldstr      "Try"
      IL_0007:  call       "Sub System.Console.Write(String)"
      IL_000c:  ldloc.0
      IL_000d:  ldloc.0
      IL_000e:  div
      IL_000f:  stloc.0
      IL_0010:  leave.s    IL_004a
    }
    filter
    {
      IL_0012:  isinst     "System.Exception"
      IL_0017:  dup
      IL_0018:  brtrue.s   IL_001e
      IL_001a:  pop
      IL_001b:  ldc.i4.0
      IL_001c:  br.s       IL_002b
      IL_001e:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
      IL_0023:  call       "Function EmitTest.Filter() As Boolean"
      IL_0028:  ldc.i4.0
      IL_0029:  cgt.un
      IL_002b:  endfilter
    }  // end filter
    {  // handler
      IL_002d:  pop
      IL_002e:  ldstr      "Catch"
      IL_0033:  call       "Sub System.Console.Write(String)"
      IL_0038:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
      IL_003d:  leave.s    IL_004a
    }
  }
  finally
  {
    IL_003f:  ldstr      "Finally"
    IL_0044:  call       "Sub System.Console.Write(String)"
    IL_0049:  endfinally
  }
  IL_004a:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TryFilterUseEx()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System        
Module EmitTest
    Sub Main()
        Dim saveUICulture = System.Threading.Thread.CurrentThread.CurrentUICulture
        System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture
        Try
            Test()
        Finally
            System.Threading.Thread.CurrentThread.CurrentUICulture = saveUICulture
        End Try
    End Sub

    Sub Test()
        Dim x as integer = 0

        Try
            Console.Write("Try")
            x = x \ x
        Catch ex As DivideByZeroException When ex.Message is Nothing
            Console.Write("Catch1")
        Catch ex As DivideByZeroException When ex.Message isNot Nothing
            Console.Write("Catch2" &amp; ex.Message.Length)
        Finally
            Console.Write("Finally")
        End Try
    End Sub

End Module
    </file>
</compilation>,
expectedOutput:="TryCatch228Finally").
            VerifyIL("EmitTest.Test",
            <![CDATA[
{
  // Code size      156 (0x9c)
  .maxstack  2
  .locals init (Integer V_0, //x
                System.DivideByZeroException V_1, //ex
                System.DivideByZeroException V_2) //ex
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  .try
  {
    .try
    {
      IL_0002:  ldstr      "Try"
      IL_0007:  call       "Sub System.Console.Write(String)"
      IL_000c:  ldloc.0
      IL_000d:  ldloc.0
      IL_000e:  div
      IL_000f:  stloc.0
      IL_0010:  leave      IL_009b
    }
    filter
    {
      IL_0015:  isinst     "System.DivideByZeroException"
      IL_001a:  dup
      IL_001b:  brtrue.s   IL_0021
      IL_001d:  pop
      IL_001e:  ldc.i4.0
      IL_001f:  br.s       IL_0034
      IL_0021:  dup
      IL_0022:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
      IL_0027:  stloc.1
      IL_0028:  ldloc.1
      IL_0029:  callvirt   "Function System.Exception.get_Message() As String"
      IL_002e:  ldnull
      IL_002f:  ceq
      IL_0031:  ldc.i4.0
      IL_0032:  cgt.un
      IL_0034:  endfilter
    }  // end filter
    {  // handler
      IL_0036:  pop
      IL_0037:  ldstr      "Catch1"
      IL_003c:  call       "Sub System.Console.Write(String)"
      IL_0041:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
      IL_0046:  leave.s    IL_009b
    }
    filter
    {
      IL_0048:  isinst     "System.DivideByZeroException"
      IL_004d:  dup
      IL_004e:  brtrue.s   IL_0054
      IL_0050:  pop
      IL_0051:  ldc.i4.0
      IL_0052:  br.s       IL_0067
      IL_0054:  dup
      IL_0055:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
      IL_005a:  stloc.2
      IL_005b:  ldloc.2
      IL_005c:  callvirt   "Function System.Exception.get_Message() As String"
      IL_0061:  ldnull
      IL_0062:  cgt.un
      IL_0064:  ldc.i4.0
      IL_0065:  cgt.un
      IL_0067:  endfilter
    }  // end filter
    {  // handler
      IL_0069:  pop
      IL_006a:  ldstr      "Catch2"
      IL_006f:  ldloc.2
      IL_0070:  callvirt   "Function System.Exception.get_Message() As String"
      IL_0075:  callvirt   "Function String.get_Length() As Integer"
      IL_007a:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToString(Integer) As String"
      IL_007f:  call       "Function String.Concat(String, String) As String"
      IL_0084:  call       "Sub System.Console.Write(String)"
      IL_0089:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
      IL_008e:  leave.s    IL_009b
    }
  }
  finally
  {
    IL_0090:  ldstr      "Finally"
    IL_0095:  call       "Sub System.Console.Write(String)"
    IL_009a:  endfinally
  }
  IL_009b:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TryFilterScoping()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System        
Module EmitTest
    dim str as String = "S1"

    Sub Main()
        Dim x as integer = 0

        Try
            Console.Write("Try")
            x = x \ x
        Catch When str.Length = 2
            Dim str as integer = 42
            Console.Write("Catch")
        Finally
            Console.Write("Finally")
        End Try
    End Sub

End Module
    </file>
</compilation>,
expectedOutput:="TryCatchFinally").
            VerifyIL("EmitTest.Main",
            <![CDATA[
{
  // Code size       83 (0x53)
  .maxstack  2
  .locals init (Integer V_0) //x
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  .try
  {
    .try
    {
      IL_0002:  ldstr      "Try"
      IL_0007:  call       "Sub System.Console.Write(String)"
      IL_000c:  ldloc.0
      IL_000d:  ldloc.0
      IL_000e:  div
      IL_000f:  stloc.0
      IL_0010:  leave.s    IL_0052
    }
    filter
    {
      IL_0012:  isinst     "System.Exception"
      IL_0017:  dup
      IL_0018:  brtrue.s   IL_001e
      IL_001a:  pop
      IL_001b:  ldc.i4.0
      IL_001c:  br.s       IL_0033
      IL_001e:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
      IL_0023:  ldsfld     "EmitTest.str As String"
      IL_0028:  callvirt   "Function String.get_Length() As Integer"
      IL_002d:  ldc.i4.2
      IL_002e:  ceq
      IL_0030:  ldc.i4.0
      IL_0031:  cgt.un
      IL_0033:  endfilter
    }  // end filter
    {  // handler
      IL_0035:  pop
      IL_0036:  ldstr      "Catch"
      IL_003b:  call       "Sub System.Console.Write(String)"
      IL_0040:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
      IL_0045:  leave.s    IL_0052
    }
  }
  finally
  {
    IL_0047:  ldstr      "Finally"
    IL_004c:  call       "Sub System.Console.Write(String)"
    IL_0051:  endfinally
  }
  IL_0052:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub TryFilterLambda()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System        
Module EmitTest
    dim str as String = "S1"

    Sub Main()
        Dim x as integer = 0

        Try
            Console.Write("Try")
            x = x \ x
        Catch When (Function() str.Length = 2)()
            Dim str as integer = 42
            Console.Write("Catch")
        Finally
            Console.Write("Finally")
        End Try
    End Sub

End Module
    </file>
</compilation>, expectedOutput:="TryCatchFinally")

        End Sub

        <Fact>
        Public Sub LiftedExceptionVariableInGenericIterator()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System
Imports System.IO
Imports System.Collections.Generic

Class C
    Public Iterator Function Iter2(Of T)() As IEnumerable(Of Integer)
        Try
            Throw New IOException("Hi")
        Catch e As Exception
            Dim a = New Action(Sub() Console.WriteLine(e.Message))
            a()
        End Try

        Yield 1
    End Function

    Shared Sub Main()
        For Each x In New C().Iter2(Of Object)()
        Next
    End Sub

End Class
    </file>
</compilation>

            CompileAndVerify(source, expectedOutput:="Hi")
        End Sub

        <Fact>
        Public Sub GenericLiftedExceptionVariableInGenericIterator()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System
Imports System.IO
Imports System.Collections.Generic

Class C
    Public Iterator Function Iter2(Of T, TE As Exception)() As IEnumerable(Of Integer)
        Try
            Throw New IOException("Hi")
        Catch e As TE When (Function() e.Message IsNot Nothing)()
            Dim a = New Action(Sub() Console.WriteLine(e.Message))
            a()
        End Try

        Yield 1
    End Function

    Shared Sub Main()
        For Each x In New C().Iter2(Of Object, IOException)()
        Next
    End Sub

End Class
    </file>
</compilation>

            CompileAndVerify(source, expectedOutput:="Hi")
        End Sub

        <Fact()>
        Public Sub ThrowNothing()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Module M
    Sub M()
        Throw Nothing
    End Sub
End Module
    </file>
</compilation>)
            compilation.AssertTheseDiagnostics(<expected>
BC30665: 'Throw' operand must derive from 'System.Exception'.
        Throw Nothing
        ~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub ThrowNothingAsException()
            Dim compilation = CompileAndVerify(
<compilation>
    <file name="a.vb">
Module M
    Sub M()
        Throw DirectCast(Nothing, System.Exception)
    End Sub
    Sub Main()
        Try
            M()
        Catch
            System.Console.WriteLine("exception")
        End Try
    End Sub
End Module
    </file>
</compilation>, expectedOutput:="exception").
                VerifyIL("M.M",
            <![CDATA[
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldnull    
  IL_0001:  throw     
}]]>)
        End Sub

        <WorkItem(542208, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542208")>
        <Fact()>
        Public Sub OverlappingCatch()
            CompileAndVerify(
            <compilation>
                <file name="a.vb">
            Imports System
            Module Module1
                Sub Main()
                    Try
                    Catch ex As Exception
                    Catch ex As SystemException
                        Console.WriteLine(ex)
                    End Try
                End Sub
            End Module
    </file>
            </compilation>,
            expectedOutput:="")
        End Sub

        <WorkItem(542208, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542208")>
        <Fact>
        Public Sub DuplicateCatch()
            CompileAndVerify(
            <compilation>
                <file name="a.vb">
            Imports System
            Module Module1
                Sub Main()
                    Try
                    Catch ex As Exception
                    Catch ex As Exception
                    End Try
                End Sub
            End Module
    </file>
            </compilation>,
            expectedOutput:="")
        End Sub

        <Fact()>
        Public Sub CatchT()
            CompileAndVerify(
            <compilation>
                <file name="a.vb">
Imports System
Class C
    Private Shared Sub TryCatch(Of T As Exception)()
        Try
            [Throw]()
        Catch e As T
            [Catch](e)
        End Try
    End Sub
    Private Shared Sub [Throw]()
        Throw New NotImplementedException()
    End Sub
    Shared Sub [Catch](e As Exception)
        Console.WriteLine("Handled")
    End Sub
    Shared Sub Main()
        Try
            TryCatch(Of NotImplementedException)()
            TryCatch(Of InvalidOperationException)()
        Catch e As Exception
            Console.WriteLine("Unhandled")
        End Try
    End Sub
End Class
                </file>
            </compilation>,
    expectedOutput:=<![CDATA[
Handled
Unhandled
]]>).
            VerifyIL("C.TryCatch(Of T)()",
            <![CDATA[
{
  // Code size       43 (0x2b)
  .maxstack  2
  .locals init (T V_0) //e
  .try
{
  IL_0000:  call       "Sub C.Throw()"
  IL_0005:  leave.s    IL_002a
}
  catch T
{
  IL_0007:  dup
  IL_0008:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
  IL_000d:  unbox.any  "T"
  IL_0012:  stloc.0
  IL_0013:  ldloc.0
  IL_0014:  box        "T"
  IL_0019:  castclass  "System.Exception"
  IL_001e:  call       "Sub C.Catch(System.Exception)"
  IL_0023:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
  IL_0028:  leave.s    IL_002a
}
  IL_002a:  ret
}
]]>)
        End Sub

        <WorkItem(542510, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542510")>
        <Fact()>
        Public Sub ExitTryFromCatch()
            CompileAndVerify(
            <compilation>
                <file name="a.vb">
Imports System
Module Program
    Sub Main(args As String())
        Try                    
        Catch ex As Exception          
            Console.Write("Exception")
            Exit Try
        Finally
        End Try
    End Sub
End Module
    </file>
            </compilation>)
        End Sub

        <Fact()>
        Public Sub EmptyTryOrEmptyFinally()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System        
Module EmitTest
    Sub Main()
        Try
        Catch
            Console.Write("Catch 0 ")
        Finally
        End Try

        Try
        Catch
            Console.Write("Catch 1 ")
        Finally
            Console.Write("Finally 1 ")
        End Try

        Try
            Console.Write("Try 2 ")
        Catch
            Console.Write("Catch 2 ")
        Finally
        End Try

        Try
            Console.Write("Try 3")
        Finally
        End Try
    End Sub

End Module
    </file>
</compilation>,
expectedOutput:="Finally 1 Try 2 Try 3").
            VerifyIL("EmitTest.Main",
            <![CDATA[
{
  // Code size       60 (0x3c)
  .maxstack  1
  IL_0000:  nop
  .try
  {
    IL_0001:  leave.s    IL_000e
  }
  finally
  {
    IL_0003:  ldstr      "Finally 1 "
    IL_0008:  call       "Sub System.Console.Write(String)"
    IL_000d:  endfinally
  }
  IL_000e:  nop
  .try
  {
    IL_000f:  ldstr      "Try 2 "
    IL_0014:  call       "Sub System.Console.Write(String)"
    IL_0019:  leave.s    IL_0031
  }
  catch System.Exception
  {
    IL_001b:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_0020:  ldstr      "Catch 2 "
    IL_0025:  call       "Sub System.Console.Write(String)"
    IL_002a:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_002f:  leave.s    IL_0031
  }
  IL_0031:  ldstr      "Try 3"
  IL_0036:  call       "Sub System.Console.Write(String)"
  IL_003b:  ret
}
]]>)
        End Sub

        <Fact, WorkItem(7144, "https://github.com/dotnet/roslyn/issues/7144")>
        Public Sub ExitTryNoCatchEmptyFinally_01()
            Dim source =
<compilation>
    <file name="a.vb">
Module EmitTest

    Sub Main()
        Try
            For Each x In {""}
                Exit Try
            Next

            Throw New System.NotSupportedException()    
        Finally
        End Try

        System.Console.WriteLine("Exited Try")
    End Sub

End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.DebugExe)

            CompileAndVerify(compilation, expectedOutput:="Exited Try")

            CompileAndVerify(compilation.WithOptions(TestOptions.ReleaseExe), expectedOutput:="Exited Try")
        End Sub

        <Fact, WorkItem(7144, "https://github.com/dotnet/roslyn/issues/7144")>
        Public Sub ExitTryNoCatchEmptyFinally_02()
            Dim source =
<compilation>
    <file name="a.vb">
Module EmitTest

    Sub Main()
        Try
            Exit Try
            Throw New System.NotSupportedException()    
        Finally
        End Try

        System.Console.WriteLine("Exited Try")
    End Sub

End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.DebugExe)

            CompileAndVerify(compilation, expectedOutput:="Exited Try")

            CompileAndVerify(compilation.WithOptions(TestOptions.ReleaseExe), expectedOutput:="Exited Try")
        End Sub

        <Fact>
        <WorkItem(29481, "https://github.com/dotnet/roslyn/issues/29481")>
        Public Sub Issue29481()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System

Class C
    Shared Sub Main()
        try
            Dim b As Boolean = false
            if b
                try
                    return
                finally
                    Console.WriteLine("Prints")
                end try
            else
                return
            end if
        finally
            GC.KeepAlive(Nothing)
        end try
    End Sub
End Class
    </file>
</compilation>

            CompileAndVerify(source, expectedOutput:="", options:=TestOptions.DebugExe)
            CompileAndVerify(source, expectedOutput:="", options:=TestOptions.ReleaseExe).
            VerifyIL("C.Main",
            <![CDATA[
{
  // Code size       26 (0x1a)
  .maxstack  1
  .try
  {
    IL_0000:  ldc.i4.0
    IL_0001:  brfalse.s  IL_0010
    .try
    {
      IL_0003:  leave.s    IL_0019
    }
    finally
    {
      IL_0005:  ldstr      "Prints"
      IL_000a:  call       "Sub System.Console.WriteLine(String)"
      IL_000f:  endfinally
    }
    IL_0010:  leave.s    IL_0019
  }
  finally
  {
    IL_0012:  ldnull
    IL_0013:  call       "Sub System.GC.KeepAlive(Object)"
    IL_0018:  endfinally
  }
  IL_0019:  ret
}
]]>)
        End Sub

    End Class
End Namespace

