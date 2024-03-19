' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class CodeGenUnstructuredErrorHandling
        Inherits BasicTestBase

        <ConditionalFact(GetType(WindowsDesktopOnly), Reason:="https://github.com/dotnet/roslyn/issues/28044")>
        Public Sub Erl_Property_SimpleBehaviourMultipleLabels()
            Dim source =
    <compilation name="ErrorHandling">
        <file name="a.vb">
            <![CDATA[
Imports System
Imports  Microsoft.VisualBasic

Module Module1
Sub main
	Try
10:
		Err.Raise(5)
		Console.writeline("Incorrect Code Path")
20:
		Console.writeline("No Error")
	Catch ex as exception
		Console.writeline(err.Number)
		Console.writeline(err.erl)
	End Try
End Sub
End Module
]]>
        </file>
    </compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseExe)
            CompileAndVerify(compilation, expectedOutput:=<![CDATA[5
10
]]>)
        End Sub

        <ConditionalFact(GetType(WindowsDesktopOnly), Reason:="https://github.com/dotnet/roslyn/issues/28044")>
        Public Sub Erl_Property_NestedTryCatch()
            'The ERL is correct even though the error occurred within a nested try catch construct
            Dim source =
    <compilation name="ErrorHandling">
        <file name="a.vb">
            <![CDATA[
Imports System
Imports  Microsoft.VisualBasic

Module Module1
Sub main
	Try
10:
      Try
	     Err.Raise(5)
         Console.writeline("Incorrect Code Path")
	   Catch ex as exception
		  Console.writeline("Inner" & err.Number)
		  Console.writeline(err.erl)
	  End Try
20:
        Console.writeline("No Error")
    Catch ex as exception
	  Console.writeline("Outer" & err.Number)
	  Console.writeline(err.erl)
	End Try
End Sub
End Module

]]>
        </file>
    </compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseExe)
            CompileAndVerify(compilation, expectedOutput:=<![CDATA[Inner5
10
No Error]]>)
        End Sub

        <ConditionalFact(GetType(WindowsDesktopOnly), Reason:="https://github.com/dotnet/roslyn/issues/28044")>
        Public Sub Erl_Property_NestedTryCatchNoPropagateToOuter()
            Dim source =
    <compilation name="ErrorHandling">
        <file name="a.vb">
            <![CDATA[
Imports System
Imports  Microsoft.VisualBasic

Module Module1
Sub main
  Try
    10:
      Try
        11:
          Err.Raise(5)
          Console.writeline("Incorrect Code Path")
      Catch ex as exception
         Console.writeline("Inner" & err.Number)
         Console.writeline(err.erl)
      End Try
   20:
      Console.writeline("No Error")
  Catch ex as exception
    Console.writeline("Outer" & err.Number)
    Console.writeline(err.erl)
  End Try
End Sub
End Module
]]>
        </file>
    </compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseExe)
            CompileAndVerify(compilation, expectedOutput:=<![CDATA[Inner5
11
No Error]]>)
        End Sub

        <Fact()>
        Public Sub Erl_Property_DuplicateLabels()
            Dim source =
    <compilation name="ErrorHandling">
        <file name="a.vb">
            <![CDATA[
Imports System
Imports  Microsoft.VisualBasic

Module Module1
Sub main
	Try
10:

Try
10:
	Err.Raise(5)
	Catch ex as exception
	End Try
20:
		Console.writeline("No Error")
	Catch ex as exception
		Console.writeline("Outer" & err.Number)
		Console.writeline(err.erl)
	End Try
End Sub
End Module
]]>
        </file>
    </compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseExe).VerifyDiagnostics(Diagnostic(ERRID.ERR_MultiplyDefined1, "10").WithArguments("10"))
        End Sub

        <ConditionalFact(GetType(WindowsDesktopOnly), Reason:="https://github.com/dotnet/roslyn/issues/28044")>
        Public Sub Erl_Property_NonSequentialLineNumbers()
            'The line numbers do not need to be sequential
            Dim source =
    <compilation name="ErrorHandling">
        <file name="a.vb">
            <![CDATA[
Imports System
Imports  Microsoft.VisualBasic

Module Module1
  Sub main
    10:
      Try
        1:
          Err.Raise(5)
          Console.writeline("Incorrect Code Path")
        20:
	  Console.writeline("No Error")
	Catch ex as exception
	  Console.writeline("Outer" & err.Number)
	  Console.writeline(err.erl)
	End Try
  End Sub
End Module
]]>
        </file>
    </compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseExe)
            CompileAndVerify(compilation, expectedOutput:=<![CDATA[Outer5
1
]]>)
        End Sub

        <ConditionalFact(GetType(WindowsDesktopOnly), Reason:="https://github.com/dotnet/roslyn/issues/28044")>
        Public Sub Erl_Property_LabelIntegerMaxValueValue()
            Dim source =
    <compilation name="ErrorHandling">
        <file name="a.vb">
            <![CDATA[
Imports System
Imports  Microsoft.VisualBasic

Module Module1
  Sub main
    1:
      Try
        2147483647:
          Err.Raise(5)
          Console.writeline("Incorrect Code Path")
        20:
	  Console.writeline("No Error")
	Catch ex as exception
	  Console.writeline("Outer" & err.Number)
	  Console.writeline(err.erl)
	End Try
  End Sub
End Module
]]>
        </file>
    </compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseExe)
            CompileAndVerify(compilation, expectedOutput:=<![CDATA[Outer5
2147483647
]]>)
        End Sub

        <ConditionalFact(GetType(WindowsDesktopOnly), Reason:="https://github.com/dotnet/roslyn/issues/28044")>
        Public Sub Erl_Property_LabelGreaterThanIntegerMaxValueValue()
            Dim source =
    <compilation name="ErrorHandling">
        <file name="a.vb">
            <![CDATA[
Imports System
Imports  Microsoft.VisualBasic

Module Module1
  Sub main
    1:
      Try
        2147483648:
          Err.Raise(5)
          Console.writeline("Incorrect Code Path")
        20:
	  Console.writeline("No Error")
	Catch ex as exception
	  Console.writeline("Outer" & err.Number)
	  Console.writeline(err.erl)
	End Try
  End Sub
End Module
]]>
        </file>
    </compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseExe)
            CompileAndVerify(compilation, expectedOutput:=<![CDATA[Outer5
0
]]>)
        End Sub

        <ConditionalFact(GetType(WindowsDesktopOnly), Reason:="https://github.com/dotnet/roslyn/issues/28044")>
        Public Sub Erl_Property_NonNumericLabels()
            Dim source =
    <compilation name="ErrorHandling">
        <file name="a.vb">
            <![CDATA[
Imports System
Imports  Microsoft.VisualBasic

Module Module1
  Sub main
    Blah:
      Try
        Foo:
	       Err.Raise(5)
           Console.writeline("Incorrect Code Path")
        Goo:
	  Console.writeline("No Error")
	Catch ex as exception
	  Console.writeline("Outer" & err.Number)
	  Console.writeline(err.erl)
	End Try
  End Sub
End Module
]]>
        </file>
    </compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseExe)
            CompileAndVerify(compilation, expectedOutput:=<![CDATA[Outer5
0
]]>)
        End Sub

        <ConditionalFact(GetType(WindowsDesktopOnly), Reason:="https://github.com/dotnet/roslyn/issues/28044")>
        Public Sub Erl_Property_NoLabels()
            Dim source =
    <compilation name="ErrorHandling">
        <file name="a.vb">
            <![CDATA[
Imports System
Imports  Microsoft.VisualBasic

Module Module1
  Sub main
    Try
      Err.Raise(5)
      Console.writeline("Incorrect Code Path")
      Console.writeline("No Error")
    Catch ex as exception
      Console.writeline("Outer" & err.Number)
      Console.writeline(err.erl)
    End Try
End Sub
End Module
]]>
        </file>
    </compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseExe)
            CompileAndVerify(compilation, expectedOutput:=<![CDATA[Outer5
0
]]>)
        End Sub

        <ConditionalFact(GetType(WindowsDesktopOnly), Reason:="https://github.com/dotnet/roslyn/issues/28044")>
        Public Sub Erl_Property_ThrowExceptionInsteadOfErrorRaiseLambdaInvocation()
            Dim source =
    <compilation name="ErrorHandling">
        <file name="a.vb">
            <![CDATA[
Imports System
Imports  Microsoft.VisualBasic

Module Module1
  Sub main
    Try
      10:
        Dim x = SUB()
                  Throw New exception ("Exception Instead Of Error")
                 End Sub		
      20:
		x()
      30:
		Console.writeline("No Error")
	Catch ex as exception
		Console.writeline("Outer" & err.Number)
		Console.writeline(err.erl)
	End Try
  End Sub
End Module
]]>
        </file>
    </compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseExe)
            CompileAndVerify(compilation, expectedOutput:=<![CDATA[Outer5
20
]]>)
        End Sub

        <ConditionalFact(GetType(WindowsDesktopOnly), Reason:="https://github.com/dotnet/roslyn/issues/28044")>
        Public Sub Erl_Property_NestedLambdasAndLabelInLambdaSameLabelInCallerAndLambda()
            Dim source =
    <compilation name="ErrorHandling">
        <file name="a.vb">
            <![CDATA[
Imports System
Imports  Microsoft.VisualBasic

Module Module1
  Sub main
    Try
      10:
        Dim x = Sub()
	          throw new exception ("Exception Instead Of Error")
                  Console.writeline("Incorrect Code Path")
                End Sub		

		Dim y = SUB()
                          30:
                            x()
                            Console.writeline("Incorrect Code Path")
                        End Sub		

     20:
       y()
     30:
        Console.writeline("Incorrect Code Path")
	Console.writeline("No Error")
     Catch ex as exception
	Console.writeline("Outer" & err.Number)
	Console.writeline(err.erl)
    End Try
End Sub
End Module
]]>
        </file>
    </compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseExe)
            CompileAndVerify(compilation, expectedOutput:=<![CDATA[Outer5
20
]]>)
        End Sub

        <ConditionalFact(GetType(WindowsDesktopOnly), Reason:="https://github.com/dotnet/roslyn/issues/28044")>
        Public Sub Erl_Property_OnErrorRetainsValueUntilCleared()
            Dim source =
    <compilation name="ErrorHandling">
        <file name="a.vb">
            <![CDATA[
Imports System
Imports  Microsoft.VisualBasic

Module Module1
  Sub main
    1:
	On error resume next
	Throw New Exception("test")
    2:
	Console.writeline(Err.Number)
	Console.writeline(Err.Erl)
    3:
	Console.writeline("Finish")
	Console.writeline(Err.Erl)
  End Sub
End Module
]]>
        </file>
    </compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseExe)
            CompileAndVerify(compilation, expectedOutput:=<![CDATA[5
1
Finish
1
]]>)
        End Sub

        <ConditionalFact(GetType(WindowsDesktopOnly), Reason:="https://github.com/dotnet/roslyn/issues/28044")>
        Public Sub Erl_Property_OnErrorRetainsValueUntilClearedWithClear()
            Dim source =
    <compilation name="ErrorHandling">
        <file name="a.vb">
            <![CDATA[
Imports System
Imports  Microsoft.VisualBasic

Module Module1
  Sub main
    1:
	On error resume next
	Throw New Exception("test")
    2:
	Console.writeline(Err.Number)
	Console.writeline(Err.Erl)
    Err.Clear
    3:
	Console.writeline("Finish")
	Console.writeline(Err.Erl)
  End Sub
End Module
]]>
        </file>
    </compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseExe)
            CompileAndVerify(compilation, expectedOutput:=<![CDATA[5
1
Finish
0
]]>)
        End Sub

        <ConditionalFact(GetType(WindowsDesktopOnly), Reason:="https://github.com/dotnet/roslyn/issues/28044")>
        Public Sub Erl_Property_UnhandledErrorDontBubbleUp()
            'There is no label in main and the label from SubMethod is not bubbled up.
            Dim source =
    <compilation name="ErrorHandling">
        <file name="a.vb">
            <![CDATA[
Imports System
Imports  Microsoft.VisualBasic

Module Module1
  Sub main
    Try
	SubMethod()
    Catch 
	Console.writeline("Finish")
	Console.writeline(Err.Erl)
    End Try
  End Sub

  Sub SubMethod
    1:
	On error goto -1
	Throw New Exception("test")

    2:
	Console.writeline(Err.Number)
	Console.writeline(Err.Erl)
	Err.Clear
  End Sub
End Module

]]>
        </file>
    </compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseExe)
            CompileAndVerify(compilation, expectedOutput:=<![CDATA[Finish
0
]]>)
        End Sub

        <ConditionalFact(GetType(WindowsDesktopOnly), Reason:="https://github.com/dotnet/roslyn/issues/28044")>
        Public Sub Erl_Property_InClassAndStructureTypes()
            Dim source =
    <compilation name="ErrorHandling">
        <file name="a.vb">
            <![CDATA[
Imports System
Imports  Microsoft.VisualBasic

Module Module1
Sub main
	Dim obj_c as new c1
	obj_c.Method
	obj_C.p1= 1

	Dim obj_s as new s1
	obj_s.Method

	dim z= obj_s.P1
End Sub
End Module


Public Class C1
	Sub Method()
1:
	On Error Resume Next
	Err.Raise(2)
2:
	Console.Writeline(Err.Erl)
	Err.Clear
3:
	Console.Writeline(Err.Erl) 'This should be 0
	Err.Raise(2)
	Console.Writeline(Err.Erl) 'This should be 3
4:
	Console.Writeline(Err.Erl) 'This should still be 3
	End Sub


	Public Property P1 as Integer
		Get 
			return 1
		End Get
		Set (v as Integer)
21:
	On Error Resume Next
	Err.Raise(2)
22:
	Console.Writeline(Err.Erl)
	Err.Clear
23:
	Console.Writeline(Err.Erl) 'This should be 0
	Err.Raise(2)
	Console.Writeline(Err.Erl) 'This should be 3
24:
	Console.Writeline(Err.Erl) 'This should still be 3
End Set
	End Property
End Class

Public Structure S1
	Shared x as integer =1
	
	Sub Method()
11:
	On Error Resume Next
	Err.Raise(2)
12:
	Console.Writeline(Err.Erl)
	Err.Clear
13:
	Console.Writeline(Err.Erl) 'This should be 0
	Err.Raise(2)
	Console.Writeline(Err.Erl) 'This should be 3
14:
	Console.Writeline(Err.Erl) 'This should still be 3
	End Sub

	Public Readonly Property P1
		Get 
21:
	On Error Resume Next
	Err.Raise(2)
22:
	Console.Writeline(Err.Erl)
	Err.Clear
23:
	Console.Writeline(Err.Erl) 'This should be 0
	Err.Raise(2)
	Console.Writeline(Err.Erl) 'This should be 3
24:
	Console.Writeline(Err.Erl) 'This should still be 3
End Get
	End Property
End Structure

]]>
        </file>
    </compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseExe)
            CompileAndVerify(compilation, expectedOutput:=<![CDATA[1
0
3
3
21
0
23
23
11
0
13
13
21
0
23
23
]]>)
        End Sub

        <ConditionalFact(GetType(WindowsDesktopOnly), Reason:="https://github.com/dotnet/roslyn/issues/28044")>
        Public Sub Erl_Property_InGenericType()
            Dim source =
    <compilation name="ErrorHandling">
        <file name="a.vb">
            <![CDATA[
Imports System
Imports  Microsoft.VisualBasic

Module Module1
Sub main
	Dim obj_c as new c1(of integer)
	obj_c.Method(of String)
End Sub
End Module


Public Class C1(of t)
	Sub Method(of u)
          On error resume next
          10:
            Err.Raise(3)
          20:
 	    Console.Writeline(Err.erl)
          30:
	    Console.Writeline(Err.erl)
            err.Clear
          40:
            Console.Writeline(Err.erl)
	End Sub
End Class
]]>
        </file>
    </compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseExe)
            CompileAndVerify(compilation, expectedOutput:=<![CDATA[10
10
0
]]>)
        End Sub

        <ConditionalFact(GetType(WindowsDesktopOnly), Reason:="https://github.com/dotnet/roslyn/issues/28044")>
        Public Sub Erl_Property_InGenericTypeSharedMethod()
            Dim source =
    <compilation name="ErrorHandling">
        <file name="a.vb">
            <![CDATA[
Imports System
Imports  Microsoft.VisualBasic

Module Module1
  Sub main
     c1(of integer).Method(of String)
  End Sub
End Module

Public Class C1(of t)
  Shared Sub Method(of u)
    On error resume next
    10:
      Err.Raise(3)
    20:
      Console.Writeline(Err.erl)
    30:
      Console.Writeline(Err.erl)
      err.Clear
    40:
      Console.Writeline(Err.erl)
  End Sub
End Class
]]>
        </file>
    </compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseExe)
            CompileAndVerify(compilation, expectedOutput:=<![CDATA[10
10
0
]]>)
        End Sub

        <ConditionalFact(GetType(WindowsDesktopOnly), Reason:="https://github.com/dotnet/roslyn/issues/28044")>
        Public Sub Erl_Property_InheritanceScenario()
            Dim source =
    <compilation name="ErrorHandling">
        <file name="a.vb">
            <![CDATA[
Imports System
Imports  Microsoft.VisualBasic

Module Module1
  Sub main
    Dim o as new c1
    o.a
  
    Dim o2 as new derived
    o2.a

    Dim o3 as C1 =  new derived
    o3.a
  End Sub
End Module


Public Class C1

  overridable Sub A()
	10:
		On error Resume next
		Err.Raise(10)
	20:	
		Console.Writeline(Err.Erl)
 End Sub
End Class

Public Class Derived :  Inherits c1
  Overrides Sub A()
	21:
		On error Resume next
		Err.Raise(10)
	22:	
		Console.Writeline(Err.Erl)

  End Sub
End Class
]]>
        </file>
    </compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseExe)
            CompileAndVerify(compilation, expectedOutput:=<![CDATA[10
21
21
]]>)
        End Sub

        <ConditionalFact(GetType(WindowsDesktopOnly), Reason:="https://github.com/dotnet/roslyn/issues/28044")>
        Public Sub Erl_Property_CallingMethodThroughInterface()
            'Verify No problems with erl because of calling using interface
            Dim source =
    <compilation name="ErrorHandling">
        <file name="a.vb">
            <![CDATA[
Imports System
Imports  Microsoft.VisualBasic

Module Module1
  Sub main
    Dim i as igoo = new c1
    i.Testmethod
  End Sub
End Module


Public Class C1 : Implements Igoo
  Sub T1 implements Igoo.Testmethod
    On error resume next
10:
	Err.Raise(3)
20:
	Console.Writeline(Err.erl)
30:
	Console.Writeline(Err.erl)
	err.Clear
40:
	Console.Writeline(Err.erl)

  End Sub
End Class

Interface Igoo
	Sub Testmethod()
End Interface
]]>
        </file>
    </compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseExe)
            CompileAndVerify(compilation, expectedOutput:=<![CDATA[10
10
0
]]>)
        End Sub

        <ConditionalFact(GetType(WindowsDesktopOnly), Reason:="https://github.com/dotnet/roslyn/issues/28044")>
        Public Sub Erl_Property_CallingMethodWithDelegate()
            Dim source =
    <compilation name="ErrorHandling">
        <file name="a.vb">
            <![CDATA[
Imports System
Imports  Microsoft.VisualBasic

Module Module1
  Public Delegate Sub DelSub()

  Sub main
    On error resume next
1:
    Dim d as DelSub 
    d = addressof TestMethod
    d()
2:
    Console.Writeline(Err.Erl)
  End Sub

  Sub TestMethod()
21:
    On error Resume next
    Err.Raise(10)
22:	
    Console.Writeline(Err.Erl)
    Err.Clear
23:
    Console.Writeline(Err.Erl)
  End Sub
End Module
]]>
        </file>
    </compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseExe)
            CompileAndVerify(compilation, expectedOutput:=<![CDATA[21
0
0
]]>)
        End Sub

        <ConditionalFact(GetType(WindowsDesktopOnly), Reason:="https://github.com/dotnet/roslyn/issues/28044")>
        Public Sub Erl_Property_CollectionInitializer()
            Dim source =
    <compilation name="ErrorHandling">
        <file name="a.vb">
            <![CDATA[
Imports System
Imports  Microsoft.VisualBasic

Module Module1
  Sub main
    On error resume next
1:
    Dim d as new  System.Collections.Generic.list(of C1) From {new c1 with {.p1=1}}
2:
    Console.Writeline(Err.Erl)  'This should be 0 
End Sub
End Module



Class C1
  Public Property P1 as integer
		Get
			Return 1
		End Get
		Set( v as integer)
	21:
		On error Resume next
		Err.Raise(10)
	22:	
		Console.Writeline(Err.Erl)
		Err.Clear
	23:
		Console.Writeline(Err.Erl)

		End Set
  End Property
End Class
]]>
        </file>
    </compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseExe)
            CompileAndVerify(compilation, expectedOutput:=<![CDATA[21
0
0
]]>)
        End Sub

        <ConditionalFact(GetType(WindowsDesktopOnly), Reason:="https://github.com/dotnet/roslyn/issues/28044")>
        Public Sub Erl_Property_MultipleHandlers()
            ' Known behavior with multiple handlers causing bogus out of memory exception
            ' Won't Fix the VB Runtime in Roslyn but captured the current behavior
            Dim source =
    <compilation name="ErrorHandling">
        <file name="a.vb">
            <![CDATA[
Imports System
Imports Microsoft.VisualBasic
Module Module1
    Sub Foo()
        On Error Resume Next
1:
        Err.Raise(5)
2:
        Console.WriteLine(Err.Erl)
        On Error GoTo 0
        On Error GoTo 3
        Err.Raise(6)
3:
        Console.WriteLine(Err.Erl)
        On Error GoTo 0
        On Error GoTo 4
        Err.Raise(7)
4:
        Console.WriteLine(Err.Erl)
        Console.WriteLine("Finish")
    End Sub

    Sub main()
        Try
            Foo()
        Catch ex As OutOfMemoryException
            Console.WriteLine("Expected Exception Occurred")
        End Try
    End Sub
End Module
]]>
        </file>
    </compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseExe)
            CompileAndVerify(compilation, expectedOutput:=<![CDATA[1
2
Expected Exception Occurred
]]>)
        End Sub

        <ConditionalFact(GetType(WindowsDesktopOnly), Reason:="https://github.com/dotnet/roslyn/issues/28044")>
        Public Sub Erl_Property_MultipleHandlersWithResume()
            Dim source =
    <compilation name="ErrorHandling">
        <file name="a.vb">
            <![CDATA[
Imports System
Imports  Microsoft.VisualBasic

Module Module1
  Sub main
    On error goto Errhandler
1:
    Err.Raise(5) 
2:
    On error Goto 0
    On error Goto SecondHandler
3:
    Err.Raise(5)

   Exit Sub

ErrHandler:
    Console.Writeline(Err.Erl)
  resume next
  exit sub

SecondHandler:
    Console.Writeline(Err.Erl)

  resume next
  exit sub
End Sub
End Module

]]>
        </file>
    </compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseExe)
            CompileAndVerify(compilation, expectedOutput:=<![CDATA[1
3
]]>)
        End Sub

        <Fact()>
        Public Sub Erl_Property_InvalidNumericLabel()
            'More a test of Invalid Label but as the label is used for ERL I wanted to make sure that this didn't compile
            Dim source =
    <compilation name="ErrorHandling">
        <file name="a.vb">
            <![CDATA[
Imports System
Imports  Microsoft.VisualBasic

Module Module1
	
	Sub main
On error Resume next
        -1:'Invalid Label
			Console.Writeline("Invalid Label")		
        0: 

		1:
			Err.Raise(5)
			Console.Writeline(Err.Erl)		
		2:
	On error Goto 0
	On error Goto 3:
			Err.Raise(6)
			Console.Writeline(Err.Erl)
		3:
			On error Goto 3:
			Err.Raise(7)
	End Sub
End Module
]]>
        </file>
    </compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseExe).VerifyDiagnostics(Diagnostic(ERRID.ERR_Syntax, "-"))
        End Sub

        <ConditionalFact(GetType(WindowsDesktopOnly), Reason:="https://github.com/dotnet/roslyn/issues/28044")>
        Public Sub Erl_Property_NoHandlerOnInner_WithDuplicateLabelInDifferentMethod()
            Dim source =
    <compilation name="ErrorHandling">
        <file name="a.vb">
            <![CDATA[
Imports System
Imports  Microsoft.VisualBasic
	
Module Module1
  Sub main
    Try
1:
      Foo
      Exit Sub
    Catch
2:
       Console.Writeline(Err.Erl)
     End Try
  End Sub

  Sub Foo()
2:
   Err.raise(4)
     Console.Writeline("Incorrect Code Path")
  End Sub
End Module]]>
        </file>
    </compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseExe)
            CompileAndVerify(compilation, expectedOutput:=<![CDATA[1
]]>)
        End Sub

        <Fact()>
        Public Sub Erl_Property_TypeCharsOnLabels()
            'More a test of Invalid Label but as the label is used for ERL I wanted to make sure that this didn't compile
            'Using type characters which would be valid for numerics
            Dim source =
    <compilation name="ErrorHandling">
        <file name="a.vb">
            <![CDATA[
Imports System
Imports  Microsoft.VisualBasic

Module Module1
  Sub main
    Try
1L:
      Foo
      Exit Sub
    Catch
2S:
      Console.Writeline(Err.Erl)
    End Try
  End Sub

  Sub Foo()
    Try
1%:
      Err.Raise(1)
      Exit Sub
    Catch
      Console.Writeline(Err.Erl)
    End Try
  End Sub
End Module

]]>
        </file>
    </compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseExe).VerifyDiagnostics(Diagnostic(ERRID.ERR_Syntax, "1L"),
                                                                                                                                                                                          Diagnostic(ERRID.ERR_Syntax, "2S"),
                                                                                                                                                                                          Diagnostic(ERRID.ERR_Syntax, "1%"))
        End Sub

        <ConditionalFact(GetType(WindowsDesktopOnly), Reason:="https://github.com/dotnet/roslyn/issues/28044")>
        Public Sub Erl_Property_WithinAsyncMethods()
            Dim source =
    <compilation name="ErrorHandling">
        <file name="a.vb">
            <![CDATA[
Imports System
Imports Microsoft.VisualBasic
Imports System.Threading
Imports System.Threading.Tasks

Module Module1
    Dim tcs As AutoResetEvent

    Sub main()                           
1:
            Async_Caller()
            Console.WriteLine(Err.Erl)
    End Sub

    Public async Sub Async_Caller()
11:
        Try
            tcs = New AutoResetEvent(False)
            Await Foob()
            tcs.WaitOne()
        Catch ex As Exception
            Console.WriteLine(Err.Erl)
            Throw
        End Try
    End Sub

    Async Function Foob() As task
        Try
21:
            Dim i = Await Test1()

        Catch ex As Exception
            Console.WriteLine(Err.Erl)
        Finally
            Console.WriteLine(Err.Erl)
            tcs.Set()
        End Try
    End Function

    Async Function Test1() As Task(Of Integer)
31:
        Err.Raise(5)
        Await Task.Delay(10)
        Return 1
    End Function
End Module

]]>
        </file>
    </compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929}, TestOptions.ReleaseExe)
            CompileAndVerify(compilation, expectedOutput:=<![CDATA[21
0
0
]]>)
        End Sub

        <Fact()>
        Public Sub Erl_Property_WithinAsyncMethods_Bug654704()
            Dim source =
    <compilation name="ErrorHandling">
        <file name="a.vb">
            <![CDATA[
Imports System
Imports Microsoft.VisualBasic
Imports System.Threading
Imports System.Threading.Tasks
Module Module1
    Dim tcs As AutoResetEvent
    Sub main()                           
1:
            Async_Caller()
            Console.WriteLine(Err.Erl)
    End Sub
    Public async Sub Async_Caller()
11:
        Try
            tcs = New AutoResetEvent(False)
            Await Foob()
            tcs.WaitOne()
        Catch ex As Exception
            Console.WriteLine(Err.Erl)
            Throw
        End Try
    End Sub
    Async Function Foob() As task
        Try
21:
            Dim i = Await Test1()
        Catch ex As Exception
            Console.WriteLine(Err.Erl)
        Finally
            Console.WriteLine(Err.Erl)
            tcs.Set()
        End Try
    End Function
    Async Function Test1() As Task(Of Integer)
31:
        Err.Raise(5)
        Return 1
    End Function
End Module
]]>
        </file>
    </compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929}, TestOptions.ReleaseExe)
            CompileAndVerify(compilation)
        End Sub

        <ConditionalFact(GetType(WindowsDesktopOnly), Reason:="https://github.com/dotnet/roslyn/issues/28044")>
        Public Sub Erl_Property_WithinIteratorMethods()
            'This is having try catches at each level and ensuring the 
            Dim source =
    <compilation name="ErrorHandling">
        <file name="a.vb">
            <![CDATA[
Imports System
Imports System.Collections.Generic
Imports Microsoft.VisualBasic

Module Module1
    Sub main()
1:
        Try
            IteratorMethod()
        Catch
            Console.writeline("Exception")
            Console.writeline(Err.erl)
        End Try


        'Normal
        Try
            NormalMethod()
        Catch
            Console.writeline("Exception")
            Console.writeline(Err.erl)
        End Try

    End Sub

    Public Sub NormalMethod()
110:
111:
        Try
            For Each i In abcdef()
            Next
        Catch
            Console.writeline("Exception")
            Console.WriteLine(Err.Erl)
            Throw
        End Try
    End Sub

    Function abcdef() As Integer()
120:
121:
        Try
            Err.Raise(5)
            Return {1, 2, 3}
        Catch
            Console.writeline("Exception")
            Console.WriteLine(Err.Erl)
            Throw
        End Try

    End Function

    Public Sub IteratorMethod()
10:
11:
        Try

            Dim x As New Scenario1
            Dim index As Integer = 0
            For Each i In x
                index += 1
            Next
        Catch
            Console.writeline("Exception")
            Console.WriteLine(Err.Erl)
            Throw
        End Try
    End Sub


End Module


Public Class Scenario1
    Implements IEnumerable(Of Integer)

    Public Iterator Function GetEnumerator() As System.Collections.Generic.IEnumerator(Of Integer) Implements System.Collections.Generic.IEnumerable(Of Integer).GetEnumerator
20:
21:
        Try
            Throw New Exception("Test")
            Yield 10
        Catch
            Console.writeline("Exception")
            Console.WriteLine(Err.Erl)
            Throw
        End Try
220:
    End Function

    Public Iterator Function GetEnumerator1() As System.Collections.IEnumerator Implements System.Collections.IEnumerable.GetEnumerator
29:
22:
        Try
            Throw New Exception("Test2")

            Yield 10
        Catch
            Console.writeline("Exception")
            Console.WriteLine(Err.Erl)
            Throw
        End Try
30:
    End Function
End Class


]]>
        </file>
    </compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseExe)
            CompileAndVerify(compilation, expectedOutput:=<![CDATA[Exception
21
Exception
11
Exception
1
Exception
121
Exception
111
Exception
1]]>)
        End Sub

        <Fact()>
        Public Sub Erl_Property_With_VBCore()
            'Error Object Doesn't exist for VBCore - so this should generate correct diagnostics
            Dim source =
    <compilation name="ErrorHandling">
        <file name="a.vb">
            <![CDATA[
Imports System

Module Module1
Sub main
	Try
10:
		Err.Raise(5)
		Console.writeline("Incorrect Code Path")
20:
		Console.writeline("No Error")
	Catch ex as exception
		Console.writeline(err.Number)
		Console.writeline(err.erl)
	End Try
End Sub
End Module
]]>
        </file>
    </compilation>

            Dim compilation = CompilationUtils.CreateEmptyCompilationWithReferences(source,
                                                                            references:={MscorlibRef, SystemRef, SystemCoreRef},
                                                                            options:=TestOptions.ReleaseDll.WithEmbedVbCoreRuntime(True)).VerifyDiagnostics(Diagnostic(ERRID.ERR_NameNotDeclared1, "Err").WithArguments("Err"),
                                                                                                                                                Diagnostic(ERRID.ERR_NameNotDeclared1, "err").WithArguments("err"),
                                                                                                                                                Diagnostic(ERRID.ERR_NameNotDeclared1, "err").WithArguments("err"))

        End Sub

        <ConditionalFact(GetType(WindowsDesktopOnly), Reason:="https://github.com/dotnet/roslyn/issues/28044")>
        Public Sub ERL_Property_CodeGenVerify()
            'Simple Verification of IL to determine that Labels and types are as expected
            CompileAndVerify(
    <compilation>
        <file name="a.vb">
Imports System
Imports  Microsoft.VisualBasic

Module Module1
Sub Main
	Try
10:
		Err.Raise(5)
		Console.writeline("Incorrect Code Path")
20:
		Console.writeline("No Error")
	Catch ex as exception
		Console.writeline(err.Number)
		Console.writeline(err.erl)
	End Try
End Sub
End Module
    </file>
    </compilation>,
    expectedOutput:=<![CDATA[5
10]]>).
            VerifyIL("Module1.Main",
            <![CDATA[{
  // Code size       89 (0x59)
  .maxstack  6
  .locals init (Integer V_0,
  System.Exception V_1) //ex
  .try
{
  IL_0000:  ldc.i4.s   10
  IL_0002:  stloc.0
  IL_0003:  call       "Function Microsoft.VisualBasic.Information.Err() As Microsoft.VisualBasic.ErrObject"
  IL_0008:  ldc.i4.5
  IL_0009:  ldnull
  IL_000a:  ldnull
  IL_000b:  ldnull
  IL_000c:  ldnull
  IL_000d:  callvirt   "Sub Microsoft.VisualBasic.ErrObject.Raise(Integer, Object, Object, Object, Object)"
  IL_0012:  ldstr      "Incorrect Code Path"
  IL_0017:  call       "Sub System.Console.WriteLine(String)"
  IL_001c:  ldc.i4.s   20
  IL_001e:  stloc.0
  IL_001f:  ldstr      "No Error"
  IL_0024:  call       "Sub System.Console.WriteLine(String)"
  IL_0029:  leave.s    IL_0058
}
  catch System.Exception
{
  IL_002b:  dup
  IL_002c:  ldloc.0
  IL_002d:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception, Integer)"
  IL_0032:  stloc.1
  IL_0033:  call       "Function Microsoft.VisualBasic.Information.Err() As Microsoft.VisualBasic.ErrObject"
  IL_0038:  callvirt   "Function Microsoft.VisualBasic.ErrObject.get_Number() As Integer"
  IL_003d:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0042:  call       "Function Microsoft.VisualBasic.Information.Err() As Microsoft.VisualBasic.ErrObject"
  IL_0047:  callvirt   "Function Microsoft.VisualBasic.ErrObject.get_Erl() As Integer"
  IL_004c:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0051:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
  IL_0056:  leave.s    IL_0058
}
  IL_0058:  ret
}
]]>)
        End Sub

    End Class
End Namespace
