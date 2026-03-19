' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Reflection
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    ' This class tests binding of various statements; i.e., the code in Binder_Statements.vb
    '
    ' Tests should be added here for every construct that can be bound
    ' correctly, with a test that compiles, verifies, and runs code for that construct. 
    ' Tests should also be added here for every diagnostic that can be generated.
    Public Class Binder_Statements_Tests
        Inherits BasicTestBase

        <Fact>
        Public Sub HelloWorld1()
            CompileAndVerify(
<compilation name="HelloWorld1">
    <file name="a.vb">
Module M        
    Sub Main()
        System.Console.WriteLine("Hello, world!")
    End Sub
End Module
    </file>
</compilation>,
    expectedOutput:="Hello, world!")
        End Sub

        <Fact>
        Public Sub HelloWorld2()
            CompileAndVerify(
<compilation name="HelloWorld2">
    <file name="a.vb">
Imports System        
Module M1
    Sub Main()
        dim x as object
        x = 42
        Console.WriteLine("Hello, world {0} {1}", 135.2.ToString(System.Globalization.CultureInfo.InvariantCulture), x)
    End Sub
End Module
    </file>
</compilation>,
    expectedOutput:="Hello, world 135.2 42")
        End Sub

        <Fact>
        Public Sub LocalWithSimpleInitialization()
            CompileAndVerify(
<compilation name="LocalWithSimpleInitialization">
    <file name="a.vb">
Imports System        
Module M1
    Sub Main()

        Dim s As String = "Hello world"
        Console.WriteLine(s)

        s = nothing
        Console.WriteLine(s)

        Dim i As Integer = 1
        Console.WriteLine(i)

        Dim d As Double = 1.5
        Console.WriteLine(d.ToString(System.Globalization.CultureInfo.InvariantCulture))

    End Sub
End Module
    </file>
</compilation>,
    expectedOutput:=<![CDATA[
Hello world

1
1.5
]]>)
        End Sub

        <Fact>
        Public Sub LocalAsNew()
            CompileAndVerify(
<compilation name="LocalAsNew">
    <file name="a.vb">
Imports System 
Class C
  Sub New (msg as string)
    Me.msg = msg
  End Sub

  Sub Report()
    Console.WriteLine(msg)
  End Sub

  private msg as string
End Class

Module M1
    Sub Main()
        dim myC as New C("hello")
        myC.Report()
    End Sub
End Module
    </file>
</compilation>,
    expectedOutput:="hello")
        End Sub

        <Fact>
        Public Sub LocalAsNewArrayError()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="LocalAsNewArrayError">
    <file name="a.vb">
Imports System   
Class C
   Sub New()
   End Sub
End Class

Module M1
    Sub Main()
       ' Arrays cannot be declared with 'New'.
       dim c1() as new C()
    End Sub
End Module
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30053: Arrays cannot be declared with 'New'.
       dim c1() as new C()
                   ~~~    
</expected>)
        End Sub

        <Fact>
        Public Sub LocalAsNewArrayError001()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="LocalAsNewArrayError">
    <file name="a.vb">
Imports System   

Class X
    Dim a(), b As New S
End Class

Class X1
    Dim a, b() As New S
End Class

Class X2
    Dim a, b(3) As New S
End Class

Class X3
    Dim a, b As New S(){}
End Class

Structure S
End Structure

Module M1
    Sub Main()

    End Sub
End Module
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30053: Arrays cannot be declared with 'New'.
    Dim a(), b As New S
        ~~~
BC30053: Arrays cannot be declared with 'New'.
    Dim a, b() As New S
           ~~~
BC30053: Arrays cannot be declared with 'New'.
    Dim a, b(3) As New S
           ~~~~
BC30205: End of statement expected.
    Dim a, b As New S(){}
                       ~
</expected>)
        End Sub

        <WorkItem(545766, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545766")>
        <Fact>
        Public Sub LocalSameNameAsOperatorAllowed()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="LocalSameNameAsOperatorAllowed">
    <file name="a.vb">
Imports System   
Class C
    Public Shared Operator IsTrue(ByVal w As C) As Boolean
        Dim IsTrue As Boolean = True
        Return IsTrue
    End Operator

    Public Shared Operator IsFalse(ByVal w As C) As Boolean
        Dim IsFalse As Boolean = True
        Return IsFalse
    End Operator
End Class

Module M1
    Sub Main()
    End Sub
End Module
    </file>
</compilation>)

            CompilationUtils.AssertNoErrors(compilation)
        End Sub

        <Fact>
        Public Sub ParameterlessSub()
            CompileAndVerify(
<compilation name="ParameterlessSub">
    <file name="a.vb">
Imports System        
Module M1
    Sub Goo()
        Console.WriteLine("Hello, world")
        Console.WriteLine()
        Console.WriteLine("Goodbye, world")
    End Sub
    Sub Main()
        Goo
    End Sub
End Module
    </file>
</compilation>,
    expectedOutput:=<![CDATA[
Hello, world

Goodbye, world
]]>)
        End Sub

        <Fact>
        Public Sub CallStatement()
            CompileAndVerify(
<compilation name="CallStatement">
    <file name="a.vb">
Imports System        
Module M1

    Sub Goo()
        Console.WriteLine("Call without parameters")
    End Sub

    Sub Goo(s as string)
       Console.WriteLine(s)
    End Sub

    Function SayHi as string
       return "Hi"
    End Function

    Function One as integer
       return 1
    End Function

    Sub Main()
       Goo(SayHi)
       goo
       call goo
       call goo("call with parameters")
       dim i = One + One
       Console.WriteLine(i)
       i = One
       Console.WriteLine(i)
    End Sub
End Module
    </file>
</compilation>,
    expectedOutput:=<![CDATA[
Hi
Call without parameters
Call without parameters
call with parameters
2
1
]]>)
        End Sub

        <Fact>
        Public Sub CallStatementMethodNotFound()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="CallStatementMethodNotFound">
    <file name="a.vb">
Imports System        
Module M1
    Sub Main()
       call goo
    End Sub
End Module
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30451: 'goo' is not declared. It may be inaccessible due to its protection level.
       call goo
            ~~~
</expected>)
        End Sub

        <WorkItem(538590, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538590")>
        <Fact>
        Public Sub CallStatementNothingAsInvocationExpression_Bug_4247()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="CallStatementMethodIsNothing">
    <file name="goo.vb">
        Module M1
            Sub Main()
                Dim myLocalArr as Integer()
                Dim myLocalVar as Integer = 42

                call myLocalArr(0)
                call myLocalVar
                call Nothing
                call 911
                call new Integer
            End Sub
        End Module
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30454: Expression is not a method.
                call myLocalArr(0)
                     ~~~~~~~~~~
BC42104: Variable 'myLocalArr' is used before it has been assigned a value. A null reference exception could result at runtime.
                call myLocalArr(0)
                     ~~~~~~~~~~
BC30454: Expression is not a method.
                call myLocalVar
                     ~~~~~~~~~~
BC30454: Expression is not a method.
                call Nothing
                     ~~~~~~~
BC30454: Expression is not a method.
                call 911
                     ~~~
BC30454: Expression is not a method.
                call new Integer
                     ~~~~~~~~~~~    
</expected>)
        End Sub

        ' related to bug 4247
        <Fact>
        Public Sub CallStatementNamespaceAsInvocationExpression()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="CallStatementMethodIsNothing">
    <file name="goo.vb">
        Namespace N1.N2
            Module M1
                Sub Main()
                    call N1
                    call N1.N2
                End Sub
            End Module
        End Namespace
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30112: 'N1' is a namespace and cannot be used as an expression.
                    call N1
                         ~~
BC30112: 'N1.N2' is a namespace and cannot be used as an expression.
                    call N1.N2
                         ~~~~~

</expected>)
        End Sub

        ' related to bug 4247
        <WorkItem(545166, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545166")>
        <Fact>
        Public Sub CallStatementTypeAsInvocationExpression()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="CallStatementMethodIsNothing">
    <file name="goo.vb">
            Class Class1
            End Class

            Module M1
                Sub Main()
                    call Class1
                    call Integer
                End Sub
            End Module
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30109: 'Class1' is a class type and cannot be used as an expression.
                    call Class1
                         ~~~~~~
BC30110: 'Integer' is a structure type and cannot be used as an expression.
                    call Integer
                         ~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub AssignmentStatement()
            CompileAndVerify(
<compilation name="AssignmentStatement1">
    <file name="a.vb">
Imports System        
Module M1
    Sub Main()

        Dim s As String
        s = "Hello world"
        Console.WriteLine(s)

        Dim i As Integer
        i = 1
        Console.WriteLine(i)

        Dim d As Double
        d = 1.5
        Console.WriteLine(d.ToString(System.Globalization.CultureInfo.InvariantCulture))

        d = i
        Console.WriteLine(d)
    End Sub
End Module
    </file>
</compilation>,
    expectedOutput:=<![CDATA[
Hello world
1
1.5
1
]]>)
        End Sub

        <Fact>
        Public Sub FieldAssignmentStatement()
            CompileAndVerify(
<compilation name="FieldAssignmentStatement">
    <file name="a.vb">
Imports System   
Class C1
   public i as integer
End class     

Structure S1
   public s as string
End Structure

Module M1
    Sub Main()
        dim myC as C1 = new C1

        myC.i = 10
        Console.WriteLine(myC.i)

        dim myS as S1 = new S1
        myS.s = "a"
        Console.WriteLine(MyS.s)
    End Sub
End Module
    </file>
</compilation>,
    expectedOutput:=<![CDATA[
10
a
]]>)
        End Sub

        <Fact>
        Public Sub AssignmentWithBadLValue()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="AssignmentWithBadLValue">
    <file name="a.vb">
Imports System  

Module M1

    Function f as integer
        return 0
    End function

    Sub s
    End Sub

    Sub Main()
        f = 0
        s = 1
        dim i as integer
      End Sub
End Module
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
           <expected>
BC30068: Expression is a value and therefore cannot be the target of an assignment.
        f = 0
        ~
BC30068: Expression is a value and therefore cannot be the target of an assignment.
        s = 1
        ~
BC42024: Unused local variable: 'i'.
        dim i as integer
            ~               
           </expected>)
        End Sub

        <Fact>
        Public Sub MultilineIfStatement1()
            CompileAndVerify(
<compilation name="MultilineIfStatement1">
    <file name="a.vb">
Imports System        
Module M1
    Sub Main()
        Dim cond As Boolean
        Dim cond2 As Boolean
        Dim cond3 As Boolean

        cond = True
        cond2 = True
        cond3 = True

        If cond Then
            Console.WriteLine("1. ThenPart")
        End If

        If cond Then
            Console.WriteLine("2. ThenPart")
        Else
            Console.WriteLine("2. ElsePart")
        End If

        If cond Then
            Console.WriteLine("3. ThenPart")
        Else If cond2
            Console.WriteLine("3. ElseIfPart")
        End If

        If cond Then
            Console.WriteLine("4. ThenPart")
        Else If cond2
            Console.WriteLine("4. ElseIf1Part")
        Else If cond3
            Console.WriteLine("4. ElseIf2Part")
        Else
            Console.WriteLine("4. ElsePart")
        End If

        cond = False

        If cond Then
            Console.WriteLine("5. ThenPart")
        End If

        If cond Then
            Console.WriteLine("6. ThenPart")
        Else
            Console.WriteLine("6. ElsePart")
        End If

        If cond Then
            Console.WriteLine("7. ThenPart")
        Else If cond2
            Console.WriteLine("7. ElseIfPart")
        End If

        If cond Then
            Console.WriteLine("8. ThenPart")
        Else If cond2
            Console.WriteLine("8. ElseIf1Part")
        Else If cond3
            Console.WriteLine("8. ElseIf2Part")
        Else
            Console.WriteLine("8. ElsePart")
        End If

        cond2 = false

        If cond Then
            Console.WriteLine("9. ThenPart")
        Else If cond2
            Console.WriteLine("9. ElseIfPart")
        End If

        If cond Then
            Console.WriteLine("10. ThenPart")
        Else If cond2
            Console.WriteLine("10. ElseIf1Part")
        Else If cond3
            Console.WriteLine("10. ElseIf2Part")
        Else
            Console.WriteLine("10. ElsePart")
        End If

        cond3 = false

        If cond Then
            Console.WriteLine("11. ThenPart")
        Else If cond2
            Console.WriteLine("11. ElseIf1Part")
        Else If cond3
            Console.WriteLine("11. ElseIf2Part")
        Else
            Console.WriteLine("11. ElsePart")
        End If

    End Sub
End Module
    </file>
</compilation>,
    expectedOutput:=<![CDATA[
1. ThenPart
2. ThenPart
3. ThenPart
4. ThenPart
6. ElsePart
7. ElseIfPart
8. ElseIf1Part
10. ElseIf2Part
11. ElsePart
]]>)
        End Sub

        <Fact>
        Public Sub SingleLineIfStatement1()
            CompileAndVerify(
<compilation name="SingleLineIfStatement1">
    <file name="a.vb">
Imports System        
Module M1
    Sub Main()
        Dim cond As Boolean

        cond = True

        If cond Then Console.WriteLine("1. ThenPart")

        If cond Then Console.WriteLine("2. ThenPartA"): COnsole.WriteLine("2. ThenPartB")

        If cond Then Console.WriteLine("3. ThenPartA"): COnsole.WriteLine("3. ThenPartB") Else Console.WriteLine("3. ElsePartA"): Console.WriteLine("3. ElsePartB")

        If cond Then Console.WriteLine("4. ThenPart") Else Console.WriteLine("4. ElsePartA"): Console.WriteLine("4. ElsePartB")

        If cond Then Console.WriteLine("5. ThenPartA"): Console.WriteLine("5. ThenPartB") Else Console.WriteLine("5. ElsePart")

        If cond Then Console.WriteLine("6. ThenPart") Else Console.WriteLine("6. ElsePart")

        cond = false

        If cond Then Console.WriteLine("7. ThenPart")

        If cond Then Console.WriteLine("8. ThenPartA"): COnsole.WriteLine("8. ThenPartB")

        If cond Then Console.WriteLine("9. ThenPart"): COnsole.WriteLine("9. ThenPartB") Else Console.WriteLine("9. ElsePartA"): Console.WriteLine("9. ElsePartB")

        If cond Then Console.WriteLine("10. ThenPart") Else Console.WriteLine("10. ElsePartA"): Console.WriteLine("10. ElsePartB")

        If cond Then Console.WriteLine("11. ThenPartA"): Console.WriteLine("11. ThenPartB") Else Console.WriteLine("11. ElsePart")

        If cond Then Console.WriteLine("12. ThenPart") Else Console.WriteLine("12. ElsePart")

    End Sub
End Module
    </file>
</compilation>,
    expectedOutput:=<![CDATA[
1. ThenPart
2. ThenPartA
2. ThenPartB
3. ThenPartA
3. ThenPartB
4. ThenPart
5. ThenPartA
5. ThenPartB
6. ThenPart
9. ElsePartA
9. ElsePartB
10. ElsePartA
10. ElsePartB
11. ElsePart
12. ElsePart
]]>)
        End Sub

        <Fact>
        Public Sub DoLoop1()
            CompileAndVerify(
<compilation name="DoLoop1">
    <file name="a.vb">
Imports System        
Module M1
    Sub Main()
        Dim x As Integer
        dim breakLoop as Boolean
        x = 1
        breakLoop = true
        Do While breakLoop
            Console.WriteLine("Iterate {0}", x)
            breakLoop = false
        Loop    
    End Sub
End Module
    </file>
</compilation>,
    expectedOutput:="Iterate 1")
        End Sub

        <Fact>
        Public Sub DoLoop2()
            CompileAndVerify(
<compilation name="DoLoop2">
    <file name="a.vb">
Imports System        
Module M1
    Sub Main()
        Dim x As Integer
        dim breakLoop as Boolean
        x = 1
        breakLoop = false
        Do Until breakLoop
            Console.WriteLine("Iterate {0}", x)
            breakLoop = true
        Loop    
    End Sub
End Module
    </file>
</compilation>,
    expectedOutput:="Iterate 1")
        End Sub

        <Fact>
        Public Sub DoLoop3()
            CompileAndVerify(
<compilation name="DoLoop3">
    <file name="a.vb">
Imports System        
Module M1
    Sub Main()
        Dim x As Integer
        dim breakLoop as Boolean
        x = 1
        breakLoop = true
        Do 
            Console.WriteLine("Iterate {0}", x)
            breakLoop = false
        Loop While breakLoop   
    End Sub
End Module
    </file>
</compilation>,
    expectedOutput:="Iterate 1")
        End Sub

        <Fact>
        Public Sub DoLoop4()
            CompileAndVerify(
<compilation name="DoLoop4">
    <file name="a.vb">
Imports System        
Module M1
    Sub Main()
        Dim x As Integer
        dim breakLoop as Boolean
        x = 1
        breakLoop = false
        Do 
            Console.WriteLine("Iterate {0}", x)
            breakLoop = true
        Loop Until breakLoop   
    End Sub
End Module
    </file>
</compilation>,
    expectedOutput:="Iterate 1")
        End Sub

        <Fact>
        Public Sub WhileLoop1()
            CompileAndVerify(
<compilation name="WhileLoop1">
    <file name="a.vb">
Imports System        
Module M1
    Sub Main()
        Dim x As Integer
        dim breakLoop as Boolean
        x = 1
        breakLoop = false
        While not breakLoop
            Console.WriteLine("Iterate {0}", x)
            breakLoop = true
        End While  
    End Sub
End Module
    </file>
</compilation>,
    expectedOutput:="Iterate 1")
        End Sub

        <Fact>
        Public Sub ExitContinueDoLoop1()
            CompileAndVerify(
<compilation name="ExitContinueDoLoop1">
    <file name="a.vb">
Imports System        
Module M1
    Sub Main()
        dim breakLoop as Boolean
        dim continueLoop as Boolean
        breakLoop = True: continueLoop = true
        Do While breakLoop
            Console.WriteLine("Stmt1")
            If continueLoop Then
                Console.WriteLine("Continuing")
                continueLoop = false
                Continue Do
            End If
            Console.WriteLine("Exiting")
            Exit Do
            Console.WriteLine("Stmt2")
        Loop   
        Console.WriteLine("After Loop") 
    End Sub
End Module
    </file>
</compilation>,
    expectedOutput:=<![CDATA[
Stmt1
Continuing
Stmt1
Exiting
After Loop
]]>)
        End Sub

        <Fact>
        Public Sub ExitSub()
            CompileAndVerify(
<compilation name="ExitSub">
    <file name="a.vb">
Imports System        
Module M1
    Sub Main()
        dim breakLoop as Boolean
        breakLoop = True
        Do While breakLoop
            Console.WriteLine("Stmt1")
            Console.WriteLine("Exiting")
            Exit Sub
            Console.WriteLine("Stmt2") 'should not output
        Loop   
        Console.WriteLine("After Loop") 'should not output
    End Sub
End Module
    </file>
</compilation>,
    expectedOutput:=<![CDATA[
Stmt1
Exiting
]]>)
        End Sub

        <Fact>
        Public Sub ExitFunction()
            CompileAndVerify(
<compilation name="ExitFunction">
    <file name="a.vb">
Imports System        
Module M1

    Function Fact(i as integer) as integer
        fact = 1
        do
            if i &lt;= 0 then
                exit function 
            else
                fact = i * fact
                i = i - 1
            end if
        loop      
    End Function

    Sub Main()
        Console.WriteLine(Fact(0))
        Console.WriteLine(Fact(3))
    End Sub
End Module
    </file>
</compilation>,
    expectedOutput:=<![CDATA[
1
6
]]>)
        End Sub

        <Fact>
        Public Sub BadExit()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="BadExit">
    <file name="a.vb">
Imports System        
Module M1
    Sub Main()
        Do 
            Exit Do  ' ok
            Exit For
            Exit Try
            Exit Select
            Exit While
        Loop   
        Exit Do  ' outside loop
    End Sub
End Module
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30096: 'Exit For' can only appear inside a 'For' statement.
            Exit For
            ~~~~~~~~
BC30393: 'Exit Try' can only appear inside a 'Try' statement.
            Exit Try
            ~~~~~~~~
BC30099: 'Exit Select' can only appear inside a 'Select' statement.
            Exit Select
            ~~~~~~~~~~~
BC30097: 'Exit While' can only appear inside a 'While' statement.
            Exit While
            ~~~~~~~~~~
BC30089: 'Exit Do' can only appear inside a 'Do' statement.
        Exit Do  ' outside loop
        ~~~~~~~    
</expected>)
        End Sub

        <Fact>
        Public Sub BadContinue()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="BadContinue">
    <file name="a.vb">
Imports System        
Module M1
    Sub Main()
        Do 
            Continue Do  ' ok
            Continue For
            Continue While
        Loop   
        Continue Do  ' outside loop
    End Sub
End Module
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30783: 'Continue For' can only appear inside a 'For' statement.
            Continue For
            ~~~~~~~~~~~~
BC30784: 'Continue While' can only appear inside a 'While' statement.
            Continue While
            ~~~~~~~~~~~~~~
BC30782: 'Continue Do' can only appear inside a 'Do' statement.
        Continue Do  ' outside loop
        ~~~~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub Return1()
            CompileAndVerify(
<compilation name="Return1">
    <file name="a.vb">
Imports System        
Module M1
    Function F1 as Integer
        F1 = 1
    End Function

    Function F2 as Integer
        if true then
            F2 = 2
        else
            return 3
        end if
    End Function

    Function F3 as Integer
        return 3
    End Function

    Sub S1 
        return
    End Sub

    Sub Main()
        dim result as integer 
        result = F1()
        Console.WriteLine(result)
        result = F2()
        Console.WriteLine(result)
        result = F3()
        Console.WriteLine(result)
    End Sub
End Module
    </file>
</compilation>,
    expectedOutput:=<![CDATA[
1
2
3
]]>)
        End Sub

        <Fact>
        Public Sub BadReturn()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="BadReturn">
    <file name="a.vb">
Imports System        
Module M1
    Function F1 as Integer
        return
    End Function

    Sub S1 
        return 1
    End Sub

End Module
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30654: 'Return' statement in a Function, Get, or Operator must return a value.
        return
        ~~~~~~
BC30647: 'Return' statement in a Sub or a Set cannot return a value.
        return 1
        ~~~~~~~~    
</expected>)
        End Sub

        <Fact>
        Public Sub NoReturnUnreachableEnd()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="NoReturnUnreachableEnd">
    <file name="a.vb">
Imports System        
Module M1
    Function goo() As Boolean
        While True
        End While
    End Function
End Module
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC42353: Function 'goo' doesn't return a value on all code paths. Are you missing a 'Return' statement?
    End Function
    ~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub BadArrayInitWithExplicitArraySize()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="BadArrayInitWithExplicitArraySize">
    <file name="a.vb">
Imports System        
Module M1

    Sub S1 
        dim a(3) as integer = 1
    End Sub

End Module
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30672: Explicit initialization is not permitted for arrays declared with explicit bounds.
        dim a(3) as integer = 1
            ~~~~
BC30311: Value of type 'Integer' cannot be converted to 'Integer()'.
        dim a(3) as integer = 1
                              ~
</expected>)
        End Sub

        <Fact>
        Public Sub BadArrayWithNegativeSize()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="BadArrayWithNegativeSize">
    <file name="a.vb">
Imports System        
Module M1

    Sub S1 
        dim a(-3) as integer
        dim b = new integer(-3){}
    End Sub

End Module
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30611: Array dimensions cannot have a negative size.
        dim a(-3) as integer
              ~~
BC30611: Array dimensions cannot have a negative size.
        dim b = new integer(-3){}
                            ~~
</expected>)
        End Sub

        <Fact>
        Public Sub ArrayWithMinusOneUpperBound()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="BadArrayWithNegativeSize">
    <file name="a.vb">
Imports System        
Module M1

    Sub S1 
        dim a(-1) as integer
        dim b = new integer(-1){}
    End Sub

End Module
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
</expected>)
        End Sub

        <WorkItem(542987, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542987")>
        <Fact()>
        Public Sub MultiDimensionalArrayWithTooFewInitializers()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="MultiDimensionalArrayWithTooFewInitializers">
    <file name="Program.vb">
Module Program
    Sub Main()
        Dim x = New Integer(0, 1) {{}}
    End Sub
End Module
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30567: Array initializer is missing 2 elements.
        Dim x = New Integer(0, 1) {{}}
                                   ~~
</expected>)
        End Sub

        <WorkItem(542988, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542988")>
        <Fact()>
        Public Sub Max32ArrayDimensionsAreAllowed()
            CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="Max32ArrayDimensionsAreAllowed">
    <file name="Program.vb">
Module Program
    Sub Main()

        Dim z1(,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,) As Integer = Nothing
        Dim z2(,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,) As Integer = Nothing

        Dim x1 = New Integer(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0) {}
        Dim x2 = New Integer(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0) {}

        Dim y1 = New Integer(,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,) {}
        Dim y2 = New Integer(,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,) {}

    End Sub
End Module
    </file>
</compilation>).
            VerifyDiagnostics(
                Diagnostic(ERRID.ERR_ArrayRankLimit, "(,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,)"),
                Diagnostic(ERRID.ERR_ArrayRankLimit, "(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)"),
                Diagnostic(ERRID.ERR_ArrayRankLimit, "(,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,)"))
        End Sub

        <Fact>
        Public Sub GotoIf()
            CompileAndVerify(
<compilation name="GotoIf">
    <file name="a.vb">
Imports System        
Module M1

   Sub GotoIf()
        GoTo l1

        If False Then
l1:
            Console.WriteLine("Jump into If")
        End If
    End Sub


    Sub GotoWhile()
        GoTo l1

        While False
l1:
            Console.WriteLine("Jump into While")
        End While
    End Sub

    Sub GotoDo()
        GoTo l1

        Do While False
l1:
            Console.WriteLine("Jump into Do")
        Loop
    End Sub

    Sub GotoSelect()
        Dim i As Integer = 0
        GoTo l1

        Select Case i
            Case 0
l1:
                Console.WriteLine("Jump into Select")
        End Select
    End Sub

    Sub Main()
        GotoIf()
        GotoWhile()
        GotoDo()
        GotoSelect()
    End Sub

End Module
    </file>
</compilation>,
    expectedOutput:=<![CDATA[
Jump into If
Jump into While
Jump into Do
Jump into Select
]]>)
        End Sub

        <Fact()>
        Public Sub GotoIntoBlockErrors()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="GotoIntoBlockErrors">
    <file name="a.vb">
Imports System        
Module M1

Sub GotoFor()
        For i as Integer = 0 To 10
l1:
            Console.WriteLine()
        Next

        GoTo l1
    End Sub

    Sub GotoWith()
        Dim c1 = New C()
        With c1
l1:
            Console.WriteLine()
        End With
        GoTo l1
    End Sub


    Sub GotoUsing()
        Using c1 as IDisposable = nothing
l1:
            Console.WriteLine()
        End Using

        GoTo l1
    End Sub

    Sub GotoTry()

        Try
l1:
            Console.WriteLine()
        Finally
        End Try
        GoTo l1
    End Sub

    Sub GotoLambda()
        Dim x = Sub()
            l1:     
                End Sub
        GoTo l1
    End Sub

End Module
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30757: 'GoTo l1' is not valid because 'l1' is inside a 'For' or 'For Each' statement that does not contain this statement.
        GoTo l1
             ~~
BC30002: Type 'C' is not defined.
        Dim c1 = New C()
                     ~
BC30756: 'GoTo l1' is not valid because 'l1' is inside a 'With' statement that does not contain this statement.
        GoTo l1
             ~~
BC36009: 'GoTo l1' is not valid because 'l1' is inside a 'Using' statement that does not contain this statement.
        GoTo l1
             ~~
BC30754: 'GoTo l1' is not valid because 'l1' is inside a 'Try', 'Catch' or 'Finally' statement that does not contain this statement.
        GoTo l1
             ~~
BC30132: Label 'l1' is not defined.
        GoTo l1
             ~~
</expected>)
        End Sub

        <Fact()>
        Public Sub GotoDecimalLabels()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="GotoDecimalLabels">
    <file name="a.vb">
Imports System        
Module M
  Sub Main()
    1 : Goto &amp;H2
    2 : Goto 01
  End Sub
End Module
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
</expected>)
        End Sub

        <WorkItem(543381, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543381")>
        <Fact()>
        Public Sub GotoUndefinedLabel()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="GotoUndefinedLabel">
    <file name="a.vb">
Imports System
Class c1    
    Shared Sub Main()        
        GoTo lab1    
    End Sub
End Class
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30132: Label 'lab1' is not defined.
        GoTo lab1    
             ~~~~
</expected>)
        End Sub

        <WorkItem(538574, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538574")>
        <Fact()>
        Public Sub ArrayModifiersOnVariableAndType()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="ArrayModifiersOnVariableAndType">
    <file name="a.vb">
Imports System        
Module M1

    public a() as integer()
    public b(1) as integer()

    Sub S1 
        dim a() as integer() = nothing
        dim b(1) as string()
    End Sub

    Sub S2(x() as integer())
    End Sub

End Module
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation, <errors>
 BC31087: Array modifiers cannot be specified on both a variable and its type.
    public a() as integer()
                  ~~~~~~~~~
BC31087: Array modifiers cannot be specified on both a variable and its type.
    public b(1) as integer()
                   ~~~~~~~~~
BC31087: Array modifiers cannot be specified on both a variable and its type.
        dim a() as integer() = nothing
                   ~~~~~~~~~
BC31087: Array modifiers cannot be specified on both a variable and its type.
        dim b(1) as string()
                    ~~~~~~~~
BC31087: Array modifiers cannot be specified on both a variable and its type.
    Sub S2(x() as integer())
                  ~~~~~~~~~                                                           
                                                            </errors>)
        End Sub

        <Fact()>
        Public Sub Bug6663()
            ' Test dependent on referenced mscorlib, but NOT system.dll.
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation name="Bug6663">
    <file name="a.vb">
Imports System
Module Program
            Sub Main()
                Console.WriteLine("".ToString() = "".ToString())
            End Sub
        End Module
    </file>
</compilation>, options:=TestOptions.ReleaseExe)

            CompileAndVerify(comp, expectedOutput:="True")
        End Sub

        <WorkItem(540390, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540390")>
        <Fact()>
        Public Sub Bug6637()

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
            <compilation name="BadArrayInitWithExplicitArraySize">
                <file name="a.vb">
Option Infer Off
Imports System        
Module M1
    Sub Main()
        Dim a(3) As Integer

        For i = 0 To 3

        Next
    End Sub
End Module
    </file>
            </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30451: 'i' is not declared. It may be inaccessible due to its protection level.
        For i = 0 To 3
            ~
</expected>)

        End Sub

        <WorkItem(540412, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540412")>
        <Fact()>
        Public Sub Bug6662()

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
            <compilation name="BadArrayInitWithExplicitArraySize">
                <file name="a.vb">
Option Infer Off
Class C
    Shared Sub M()
        For i = Nothing To 10
            Dim d as System.Action = Sub() i = i + 1
        Next
    End Sub
End Class
    </file>
            </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30451: 'i' is not declared. It may be inaccessible due to its protection level.
        For i = Nothing To 10
            ~
BC30451: 'i' is not declared. It may be inaccessible due to its protection level.
            Dim d as System.Action = Sub() i = i + 1
                                           ~
BC30451: 'i' is not declared. It may be inaccessible due to its protection level.
            Dim d as System.Action = Sub() i = i + 1
                                               ~
</expected>)

        End Sub

        <WorkItem(542801, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542801")>
        <Fact()>
        Public Sub ExtTryFromFinally()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Linq
Class BaseClass
    Function Method() As String
        Dim x = New Integer() {}

        Try
            Exit Try
        Catch ex1 As Exception When True
            Exit Try
        Finally
            Exit Try
        End Try

        Return "x"
    End Function
End Class
Class DerivedClass
    Inherits BaseClass
    Shared Sub Main()
    End Sub
End Class
                </file>
</compilation>, {SystemCoreRef})

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30393: 'Exit Try' can only appear inside a 'Try' statement.
            Exit Try
            ~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub CatchNotLocal()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="CatchNotLocal">
    <file name="goo.vb">
            Module M1
                Private ex as System.Exception

                Sub Main()
                    Try
                    Catch ex
                    End Try
                End Sub
            End Module
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC31082: 'ex' is not a local variable or parameter, and so cannot be used as a 'Catch' variable.
                    Catch ex
                          ~~
</expected>)
        End Sub

        <Fact(), WorkItem(651622, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/651622")>
        Public Sub Bug651622()

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="goo.vb">
Module Module1
    Sub Main()
        Try
        Catch Main
        Catch x as System.Exception
        End Try
    End Sub
End Module
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC31082: 'Main' is not a local variable or parameter, and so cannot be used as a 'Catch' variable.
        Catch Main
              ~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub CatchStatic()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="CatchStatic">
    <file name="goo.vb">
            Imports System

            Module M1
                Sub Main()
                    Static ex as exception = nothing

                    Try
                    Catch ex
                    End Try
                End Sub
            End Module
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC31082: 'ex' is not a local variable or parameter, and so cannot be used as a 'Catch' variable.
                    Catch ex
                          ~~
</expected>)
        End Sub

        <Fact()>
        Public Sub CatchUndeclared()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="CatchUndeclared">
    <file name="goo.vb">
            Option Explicit Off

            Module M1
                Sub Main()
                    Try

                    ' Explicit off does not have effect on Catch - ex is still undefined.
                    Catch ex
                    End Try
                End Sub
            End Module
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30451: 'ex' is not declared. It may be inaccessible due to its protection level.
                    Catch ex
                          ~~
</expected>)
        End Sub

        <Fact()>
        Public Sub CatchNotException()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="CatchNotException">
    <file name="goo.vb">
            Option Explicit Off

            Module M1
                Sub Main()
                    Dim ex as String = "qq"
                    Try                   
                    Catch ex
                    End Try

                    Try                   
                    Catch ex1 as String
                    End Try
                End Sub
            End Module
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30392: 'Catch' cannot catch type 'String' because it is not 'System.Exception' or a class that inherits from 'System.Exception'.
                    Catch ex
                          ~~
BC30392: 'Catch' cannot catch type 'String' because it is not 'System.Exception' or a class that inherits from 'System.Exception'.
                    Catch ex1 as String
                                 ~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub CatchNotVariableOrParameter()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="CatchNotVariableOrParameter">
    <file name="goo.vb">
            Option Explicit Off

            Module M1
                Sub Goo
                End Sub

                Sub Main()
                    Try                   
                    Catch Goo
                    End Try
                End Sub
            End Module
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC31082: 'Goo' is not a local variable or parameter, and so cannot be used as a 'Catch' variable.
                    Catch Goo
                          ~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub CatchDuplicate()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="CatchNotException">
    <file name="goo.vb">
            Imports System

            Module M1
                Sub Main()
                    Dim ex as Exception = Nothing

                    Try                   
                    Catch ex
                    Catch ex1 as Exception
                    Catch
                    End Try

                End Sub
            End Module
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC42031: 'Catch' block never reached; 'Exception' handled above in the same Try statement.
                    Catch ex1 as Exception
                    ~~~~~~~~~~~~~~~~~~~~~~
BC42031: 'Catch' block never reached; 'Exception' handled above in the same Try statement.
                    Catch
                    ~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub CatchDuplicate1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="CatchNotException">
    <file name="goo.vb">
            Imports System

            Module M1
                Sub Main()
                    Dim ex as Exception = Nothing

                    Try                   
                    Catch
                    Catch ex
                    Catch ex1 as Exception
                    End Try

                End Sub
            End Module
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC42031: 'Catch' block never reached; 'Exception' handled above in the same Try statement.
                    Catch ex
                    ~~~~~~~~
BC42031: 'Catch' block never reached; 'Exception' handled above in the same Try statement.
                    Catch ex1 as Exception
                    ~~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub CatchDuplicate2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="CatchNotException">
    <file name="goo.vb">
            Imports System

            Module M1
                Sub Main()
                    Dim ex as Exception = Nothing

                    Try            
                    ' the following is NOT considered as catching all System.Exceptions       
                    Catch When true

                    Catch ex

                    ' filter does NOT make this reachable.
                    Catch ex1 as Exception When true

                    ' implicitly this is a "Catch ex As Exception When true" so still unreachable
                    Catch When true
                    Catch ex1 as Exception
                    End Try

                End Sub
            End Module
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC42031: 'Catch' block never reached; 'Exception' handled above in the same Try statement.
                    Catch ex1 as Exception When true
                    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42031: 'Catch' block never reached; 'Exception' handled above in the same Try statement.
                    Catch When true
                    ~~~~~~~~~~~~~~~
BC42031: 'Catch' block never reached; 'Exception' handled above in the same Try statement.
                    Catch ex1 as Exception
                    ~~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub CatchOverlapped()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="CatchNotException">
    <file name="goo.vb">
Imports System

Module M1
    Sub Main()
        Dim ex As SystemException = Nothing

        Try
            ' the following is NOT considered as catching all System.Exceptions       
        Catch When True

        Catch ex

            ' filter does NOT make this reachable.
        Catch ex1 As ArgumentException When True

            ' implicitly this is a "Catch ex As Exception When true"
        Catch When True

            ' this is ok since it is not derived from SystemException
            ' and catch above has a filter
        Catch ex1 As ApplicationException
        End Try

    End Sub
End Module
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC42029: 'Catch' block never reached, because 'ArgumentException' inherits from 'SystemException'.
        Catch ex1 As ArgumentException When True
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub CatchShadowing()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="CatchNotException">
    <file name="goo.vb">
Imports System

Module M1
    Dim field As String

    Function Goo(Of T)(ex As Exception) As Exception
        Dim ex1 As SystemException = Nothing

        Try
            Dim ex2 As Exception = nothing
        Catch ex As Exception
        Catch ex1 As Exception
        Catch Goo As ArgumentException When True

            ' this is ok
        Catch ex2 As exception
            Dim ex3 As exception = nothing

            'this is ok
        Catch ex3 As ApplicationException

            ' this is ok
        Catch field As Exception

        End Try

        return nothing
    End Function

    Sub Main()
    End Sub
End Module
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30734: 'ex' is already declared as a parameter of this method.
        Catch ex As Exception
              ~~
BC42031: 'Catch' block never reached; 'Exception' handled above in the same Try statement.
        Catch ex1 As Exception
        ~~~~~~~~~~~~~~~~~~~~~~
BC30616: Variable 'ex1' hides a variable in an enclosing block.
        Catch ex1 As Exception
              ~~~
BC42029: 'Catch' block never reached, because 'ArgumentException' inherits from 'Exception'.
        Catch Goo As ArgumentException When True
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30290: Local variable cannot have the same name as the function containing it.
        Catch Goo As ArgumentException When True
              ~~~
BC42031: 'Catch' block never reached; 'Exception' handled above in the same Try statement.
        Catch ex2 As exception
        ~~~~~~~~~~~~~~~~~~~~~~
BC42029: 'Catch' block never reached, because 'ApplicationException' inherits from 'Exception'.
        Catch ex3 As ApplicationException
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42031: 'Catch' block never reached; 'Exception' handled above in the same Try statement.
        Catch field As Exception
        ~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <WorkItem(837820, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/837820")>
        <Fact()>
        Public Sub CatchShadowingGeneric()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="CatchNotException">
    <file name="goo.vb">
Imports System

Module M1
Class cls3(Of T As NullReferenceException)
    Sub scen3()
        Try
        Catch ex As T
        Catch ex As NullReferenceException
        End Try
    End Sub
    Sub scen4()
        Try
        Catch ex As NullReferenceException
            'COMPILEWarning: BC42029 ,"Catch ex As T"
        Catch ex As T
        End Try
    End Sub
End Class
 
    Sub Main()
    End Sub
End Module
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
    BC42029: 'Catch' block never reached, because 'T' inherits from 'NullReferenceException'.
        Catch ex As T
        ~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub GotoOutOfFinally()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="GotoOutOfFinally">
    <file name="goo.vb">
            Imports System

            Module M1
                Sub Main()
l1:
                    Try                   
                    Finally
                        try
                            goto l1
                        catch
                        End Try
                    End Try
                End Sub
            End Module
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30101: Branching out of a 'Finally' is not valid.
                            goto l1
                                 ~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BranchOutOfFinally1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="BranchOutOfFinally1">
    <file name="goo.vb">
            Imports System

            Module M1
                Sub Main()
                    for i as integer = 1 to 10
                        Try                   
                        Finally
                            continue for
                        End Try
                    Next
                End Sub

                Function Goo() as integer
l1:
                    Try                   
                    Finally
                        try
                            return 1
                        catch
                            return 1
                        End Try
                    End Try
                End Function

            End Module
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30101: Branching out of a 'Finally' is not valid.
                            continue for
                            ~~~~~~~~~~~~
BC30101: Branching out of a 'Finally' is not valid.
                            return 1
                            ~~~~~~~~
BC30101: Branching out of a 'Finally' is not valid.
                            return 1
                            ~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub GotoFromCatchToTry()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="GotoFromCatchToTry">
    <file name="goo.vb">
Imports System

Module M1
    Sub Main()
        Try

        Catch ex As Exception
l1:

            Try
                GoTo l1
            Catch ex2 As Exception
                GoTo l1
            Finally

            End Try
        End Try
    End Sub
End Module
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
</expected>)
        End Sub

        <Fact()>
        Public Sub GotoFromCatchToTry1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="GotoFromCatchToTry">
    <file name="goo.vb">
Imports System

Module M1
    Sub Main()
        Try
l1:

        Catch ex As Exception
            Try
                GoTo l1
            Catch ex2 As Exception
                GoTo l1
            Finally

            End Try
        End Try
    End Sub
End Module
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
</expected>)
        End Sub

        <Fact()>
        Public Sub UnassignedVariableInLateAddressOf()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="UnassignedVariableInCatchFinallyFilter">
    <file name="goo.vb">
Option Strict Off
Imports System

Module Program

    Delegate Sub d1(ByRef x As Integer, y As Integer)

    Sub Main()
        Dim obj As Object '= New cls1

        Dim o As d1 = AddressOf obj.goo

        Dim l As Integer = 0
        o(l, 2)

        Console.WriteLine(l)
    End Sub

    Class cls1
        Shared Sub goo(ByRef x As Integer, y As Integer)
            x = 42
            Console.WriteLine(x + y)
        End Sub
    End Class
End Module

    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC42104: Variable 'obj' is used before it has been assigned a value. A null reference exception could result at runtime.
        Dim o As d1 = AddressOf obj.goo
                                ~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub UnassignedVariableInCatchFinallyFilter()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="UnassignedVariableInCatchFinallyFilter">
    <file name="goo.vb">
Imports System

Module M1
    Sub Main()

        Dim A as ApplicationException
        Dim B as StackOverflowException
        Dim C as Exception

        Try
            A = new ApplicationException
            B = new StackOverflowException
            C = new Exception

            Console.Writeline(A) 'this is ok

        Catch ex as NullReferenceException When A.Message isnot nothing

        Catch ex as DivideByZeroException
            Console.Writeline(B)              

        Finally        
            Console.Writeline(C)

        End Try
    End Sub
End Module
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC42104: Variable 'A' is used before it has been assigned a value. A null reference exception could result at runtime.
        Catch ex as NullReferenceException When A.Message isnot nothing
                                                ~
BC42104: Variable 'B' is used before it has been assigned a value. A null reference exception could result at runtime.
            Console.Writeline(B)              
                              ~
BC42104: Variable 'C' is used before it has been assigned a value. A null reference exception could result at runtime.
            Console.Writeline(C)
                              ~
</expected>)
        End Sub

        <Fact()>
        Public Sub UnassignedVariableInCatchFinallyFilter1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="UnassignedVariableInCatchFinallyFilter1">
    <file name="goo.vb">
Imports System

Module M1
    Sub Main()

        Dim A as ApplicationException

        Try

        ' ok , A is assigned in the filter and in the catch
        Catch A When A.Message isnot nothing
            Console.Writeline(A)

        Catch ex as Exception
            A = new ApplicationException

        Finally        
            'error
            Console.Writeline(A)

        End Try
    End Sub
End Module
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC42104: Variable 'A' is used before it has been assigned a value. A null reference exception could result at runtime.
            Console.Writeline(A)
                              ~
</expected>)
        End Sub

        <Fact()>
        Public Sub UnassignedVariableInCatchFinallyFilter2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="UnassignedVariableInCatchFinallyFilter2">
    <file name="goo.vb">
Imports System

Module M1
    Sub Main()

        Dim A as ApplicationException

        Try
            A = new ApplicationException
        Catch A 

        Catch 

        End Try

        Console.Writeline(A)
    End Sub
End Module
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC42104: Variable 'A' is used before it has been assigned a value. A null reference exception could result at runtime.
        Console.Writeline(A)
                          ~
</expected>)
        End Sub

        <Fact()>
        Public Sub UnassignedVariableInCatchFinallyFilter3()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="UnassignedVariableInCatchFinallyFilter3">
    <file name="goo.vb">
Imports System

Module M1
    Sub Main()

        Dim A as ApplicationException

        Try
            A = new ApplicationException
        Catch A 
        Catch 
            try
            Finally
                A = new ApplicationException
            End Try           
        End Try

        Console.Writeline(A)
    End Sub
End Module
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
</expected>)
        End Sub

        <Fact()>
        Public Sub UnassignedVariableInCatchFinallyFilter4()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="UnassignedVariableInCatchFinallyFilter4">
    <file name="goo.vb">
Imports System

Module M1
    Sub Main()

        Dim A as ApplicationException

        Try
            A = new ApplicationException
        Catch A 
        Catch 
            try
            Finally
                A = new ApplicationException
            End Try           
        Finally 
            Console.Writeline(A)
        End Try

    End Sub
End Module
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC42104: Variable 'A' is used before it has been assigned a value. A null reference exception could result at runtime.
            Console.Writeline(A)
                              ~
</expected>)
        End Sub

        <Fact()>
        Public Sub ThrowNotValue()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="ThrowNotValue">
    <file name="goo.vb">
Imports System        
Module M1
    ReadOnly Property Moo As Exception
        Get
            Return New Exception
        End Get
    End Property

    WriteOnly Property Boo As Exception
        Set(value As Exception)

        End Set
    End Property

    Sub Main()
        Throw Moo
        Throw Boo
    End Sub
End Module
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30524: Property 'Boo' is 'WriteOnly'.
        Throw Boo
              ~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub ThrowNotException()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="ThrowNotValue">
    <file name="goo.vb">
Imports System        
Module M1
    ReadOnly e as new Exception
    ReadOnly s as string = "qq"

    Sub Main()
        Throw e
        Throw s
    End Sub
End Module
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30665: 'Throw' operand must derive from 'System.Exception'.
        Throw s
        ~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub RethrowNotInCatch()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="RethrowNotInCatch">
    <file name="goo.vb">
Imports System        
Module M1
    Sub Main()
        Throw

        Try
            Throw

        Catch ex As Exception
            Throw

            Dim a As Action = Sub()
                                  ex.ToString()

                                  Throw
                              End Sub

            Try
                Throw

            Catch
                Throw

            Finally
                Throw

            End Try
        End Try
    End Sub
End Module
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30666: 'Throw' statement cannot omit operand outside a 'Catch' statement or inside a 'Finally' statement.
        Throw
        ~~~~~
BC30666: 'Throw' statement cannot omit operand outside a 'Catch' statement or inside a 'Finally' statement.
            Throw
            ~~~~~
BC30666: 'Throw' statement cannot omit operand outside a 'Catch' statement or inside a 'Finally' statement.
                                  Throw
                                  ~~~~~
BC30666: 'Throw' statement cannot omit operand outside a 'Catch' statement or inside a 'Finally' statement.
                Throw
                ~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub ForNotValue()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="ThrowNotValue">
    <file name="goo.vb">
Imports System        
Module M1
    ReadOnly Property Moo As Integer
        Get
            Return 1
        End Get
    End Property

    WriteOnly Property Boo As integer
        Set(value As integer)

        End Set
    End Property

    Sub Main()
        For Moo = 1 to Moo step Moo
        Next

        For Boo = 1 to Boo step Boo
        Next
    End Sub
End Module
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30039: Loop control variable cannot be a property or a late-bound indexed array.
        For Moo = 1 to Moo step Moo
            ~~~
BC30039: Loop control variable cannot be a property or a late-bound indexed array.
        For Boo = 1 to Boo step Boo
            ~~~
BC30524: Property 'Boo' is 'WriteOnly'.
        For Boo = 1 to Boo step Boo
                       ~~~
BC30524: Property 'Boo' is 'WriteOnly'.
        For Boo = 1 to Boo step Boo
                                ~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub CustomDatatypeForLoop()
            Dim source =
<compilation>
    <file name="goo.vb"><![CDATA[
Imports System

Module Module1

    Public Sub Main()
        Dim x As New c1
        For x = 1 To 3
            Console.WriteLine("hi")
        Next
    End Sub
End Module

Public Class c1
    Public val As Integer

    Public Shared Widening Operator CType(ByVal arg1 As Integer) As c1
        Console.WriteLine("c1::CType(Integer) As c1")
        Dim c As New c1
        c.val = arg1 'what happens if this is last statement?
        Return c
    End Operator

    Public Shared Widening Operator CType(ByVal arg1 As c1) As Integer
        Console.WriteLine("c1::CType(c1) As Integer")
        Dim x As Integer
        x = arg1.val
        Return x
    End Operator

    Public Shared Operator +(ByVal arg1 As c1, ByVal arg2 As c1) As c1
        Console.WriteLine("c1::+(c1, c1) As c1")
        Dim c As New c1
        c.val = arg1.val + arg2.val
        Return c
    End Operator

    Public Shared Operator -(ByVal arg1 As c1, ByVal arg2 As c1) As c1
        Console.WriteLine("c1::-(c1, c1) As c1")
        Dim c As New c1
        c.val = arg1.val - arg2.val
        Return c
    End Operator

    Public Shared Operator >=(ByVal arg1 As c1, ByVal arg2 As Integer) As Boolean
        Console.WriteLine("c1::>=(c1, Integer) As Boolean")
        If arg1.val >= arg2 Then
            Return True
        Else
            Return False
        End If
    End Operator

    Public Shared Operator <=(ByVal arg1 As c1, ByVal arg2 As Integer) As Boolean
        Console.WriteLine("c1::<=(c1, Integer) As Boolean")
        If arg1.val <= arg2 Then
            Return True
        Else
            Return False
        End If

    End Operator

    Public Shared Operator <=(ByVal arg2 As Integer, ByVal arg1 As c1) As Boolean
        Console.WriteLine("c1::<=(Integer, c1) As Boolean")
        If arg1.val <= arg2 Then
            Return True
        Else
            Return False
        End If

    End Operator

    Public Shared Operator >=(ByVal arg2 As Integer, ByVal arg1 As c1) As Boolean
        Console.WriteLine("c1::>=(Integer, c1) As Boolean")
        If arg1.val <= arg2 Then
            Return True
        Else
            Return False
        End If
    End Operator

    Public Shared Operator <=(ByVal arg1 As c1, ByVal arg2 As c1) As Boolean
        Console.WriteLine("c1::<=(c1, c1) As Boolean")
        If arg1.val <= arg2.val Then
            Return True
        Else
            Return False
        End If

    End Operator

    Public Shared Operator >=(ByVal arg1 As c1, ByVal arg2 As c1) As Boolean
        Console.WriteLine("c1::>=(c1, c1) As Boolean")
        If arg1.val >= arg2.val Then
            Return True
        Else
            Return False
        End If
    End Operator

End Class
]]>
    </file>
</compilation>

            CompileAndVerify(source, <![CDATA[c1::CType(Integer) As c1
c1::CType(Integer) As c1
c1::CType(Integer) As c1
c1::-(c1, c1) As c1
c1::>=(c1, c1) As Boolean
c1::<=(c1, c1) As Boolean
hi
c1::+(c1, c1) As c1
c1::<=(c1, c1) As Boolean
hi
c1::+(c1, c1) As c1
c1::<=(c1, c1) As Boolean
hi
c1::+(c1, c1) As c1
c1::<=(c1, c1) As Boolean
]]>)
        End Sub

        <Fact()>
        Public Sub SelectCase1_SwitchTable()
            CompileAndVerify(
<compilation name="SelectCase1">
    <file name="a.vb"><![CDATA[
Imports System        
Module M1
    Sub Main()
        For x = 0 to 11
            Console.Write(x.ToString() + ":")
            Test(x)
        Next
    End Sub

    Sub Test(number as Integer)
        Select Case number
            Case 0
                Console.WriteLine("Equal to 0")
            Case 1, 2, 3, 4, 5
                Console.WriteLine("Between 1 and 5, inclusive")
            Case 6, 7, 8
                Console.WriteLine("Between 6 and 8, inclusive")
            Case 9, 10
                Console.WriteLine("Equal to 9 or 10")
            Case Else
                Console.WriteLine("Greater than 10")
        End Select
    End Sub
End Module
    ]]></file>
</compilation>,
    expectedOutput:=<![CDATA[0:Equal to 0
1:Between 1 and 5, inclusive
2:Between 1 and 5, inclusive
3:Between 1 and 5, inclusive
4:Between 1 and 5, inclusive
5:Between 1 and 5, inclusive
6:Between 6 and 8, inclusive
7:Between 6 and 8, inclusive
8:Between 6 and 8, inclusive
9:Equal to 9 or 10
10:Equal to 9 or 10
11:Greater than 10]]>)
        End Sub

        <Fact()>
        Public Sub SelectCase2_IfList()
            CompileAndVerify(
<compilation name="SelectCase2">
    <file name="a.vb"><![CDATA[
Imports System        
Module M1
    Sub Main()
        For x = 0 to 11
            Console.Write(x.ToString() + ":")
            Test(x)
        Next
    End Sub

    Sub Test(number as Integer)
        Select Case number
            Case Is < 1
                Console.WriteLine("Less than 1")
            Case 1 To 5
                Console.WriteLine("Between 1 and 5, inclusive")
            Case 6, 7, 8
                Console.WriteLine("Between 6 and 8, inclusive")
            Case 9 To 10
                Console.WriteLine("Equal to 9 or 10")
            Case Else
                Console.WriteLine("Greater than 10")
        End Select
    End Sub
End Module
    ]]></file>
</compilation>,
    expectedOutput:=<![CDATA[0:Less than 1
1:Between 1 and 5, inclusive
2:Between 1 and 5, inclusive
3:Between 1 and 5, inclusive
4:Between 1 and 5, inclusive
5:Between 1 and 5, inclusive
6:Between 6 and 8, inclusive
7:Between 6 and 8, inclusive
8:Between 6 and 8, inclusive
9:Equal to 9 or 10
10:Equal to 9 or 10
11:Greater than 10]]>)
        End Sub

        <WorkItem(542156, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542156")>
        <Fact()>
        Public Sub ImplicitVarInRedim()
            CompileAndVerify(
<compilation name="HelloWorld1">
    <file name="a.vb">
Option Explicit Off
Module M        
    Sub Main()
        Redim x(10)
        System.Console.WriteLine("OK")
    End Sub
End Module
    </file>
</compilation>,
    expectedOutput:="OK")
        End Sub

        <Fact()>
        Public Sub EndStatementsInMethodBodyShouldNotThrowNYI()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="EndStatementsInMethodBodyShouldNotThrowNYI">
        <file name="a.vb">
Namespace N1
    Public Class C1
        Public Sub S1()
            for i as integer = 23 to 42 
            next

            next

            do 
            loop while true
            loop
            end if
            end select
            end try
            end using
            end while
            end with 
            end synclock

            Try
            Catch ex As System.Exception
            End Try
            catch    

            Try
            Catch ex As System.Exception
            finally
            finally
            End Try

            finally
        End Sub

        Public Sub S2
            end namespace 
            end module
            end class 
            end structure
            end interface
            end enum 
            end function                     
            end operator
            end property
            end get
            end set                     
            end event
            end addhandler
            end removehandler
            end raiseevent
        End Sub
    end Class
end Namespace     

Namespace N2
    Class C2    
        function F1() as integer
            end sub

            return 42
        end function
    End Class
End Namespace
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC30481: 'Class' statement must end with a matching 'End Class'.
    Public Class C1
    ~~~~~~~~~~~~~~~
BC30092: 'Next' must be preceded by a matching 'For'.
            next
            ~~~~
BC30091: 'Loop' must be preceded by a matching 'Do'.
            loop
            ~~~~
BC30087: 'End If' must be preceded by a matching 'If'.
            end if
            ~~~~~~
BC30088: 'End Select' must be preceded by a matching 'Select Case'.
            end select
            ~~~~~~~~~~
BC30383: 'End Try' must be preceded by a matching 'Try'.
            end try
            ~~~~~~~
BC36007: 'End Using' must be preceded by a matching 'Using'.
            end using
            ~~~~~~~~~
BC30090: 'End While' must be preceded by a matching 'While'.
            end while
            ~~~~~~~~~
BC30093: 'End With' must be preceded by a matching 'With'.
            end with 
            ~~~~~~~~
BC30674: 'End SyncLock' must be preceded by a matching 'SyncLock'.
            end synclock
            ~~~~~~~~~~~~
BC30380: 'Catch' cannot appear outside a 'Try' statement.
            catch    
            ~~~~~
BC30381: 'Finally' can only appear once in a 'Try' statement.
            finally
            ~~~~~~~
BC30382: 'Finally' cannot appear outside a 'Try' statement.
            finally
            ~~~~~~~
BC30026: 'End Sub' expected.
        Public Sub S2
        ~~~~~~~~~~~~~
BC30622: 'End Module' must be preceded by a matching 'Module'.
            end module
            ~~~~~~~~~~
BC30460: 'End Class' must be preceded by a matching 'Class'.
            end class 
            ~~~~~~~~~
BC30621: 'End Structure' must be preceded by a matching 'Structure'.
            end structure
            ~~~~~~~~~~~~~
BC30252: 'End Interface' must be preceded by a matching 'Interface'.
            end interface
            ~~~~~~~~~~~~~
BC30184: 'End Enum' must be preceded by a matching 'Enum'.
            end enum 
            ~~~~~~~~
BC30430: 'End Function' must be preceded by a matching 'Function'.
            end function                     
            ~~~~~~~~~~~~
BC33007: 'End Operator' must be preceded by a matching 'Operator'.
            end operator
            ~~~~~~~~~~~~
BC30431: 'End Property' must be preceded by a matching 'Property'.
            end property
            ~~~~~~~~~~~~
BC30630: 'End Get' must be preceded by a matching 'Get'.
            end get
            ~~~~~~~
BC30632: 'End Set' must be preceded by a matching 'Set'.
            end set                     
            ~~~~~~~
BC31123: 'End Event' must be preceded by a matching 'Custom Event'.
            end event
            ~~~~~~~~~
BC31124: 'End AddHandler' must be preceded by a matching 'AddHandler' declaration.
            end addhandler
            ~~~~~~~~~~~~~~
BC31125: 'End RemoveHandler' must be preceded by a matching 'RemoveHandler' declaration.
            end removehandler
            ~~~~~~~~~~~~~~~~~
BC31126: 'End RaiseEvent' must be preceded by a matching 'RaiseEvent' declaration.
            end raiseevent
            ~~~~~~~~~~~~~~
BC30429: 'End Sub' must be preceded by a matching 'Sub'.
        End Sub
        ~~~~~~~
BC30460: 'End Class' must be preceded by a matching 'Class'.
    end Class
    ~~~~~~~~~
BC30623: 'End Namespace' must be preceded by a matching 'Namespace'.
end Namespace     
~~~~~~~~~~~~~
BC30429: 'End Sub' must be preceded by a matching 'Sub'.
            end sub
            ~~~~~~~

     </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub AddHandlerMissingStuff()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="AddHandlerNotSimple">
    <file name="goo.vb">
Imports System        
Module M1
    Sub Main()
        Dim del As System.EventHandler =
            Sub(sender As Object, a As EventArgs) Console.Write("unload")

        Dim v = AppDomain.CreateDomain("qq")

        AddHandler v.DomainUnload,

        AddHandler , del

        AddHandler

    End Sub
End Module
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30201: Expression expected.
        AddHandler v.DomainUnload,
                                  ~
BC30201: Expression expected.
        AddHandler , del
                   ~
BC30196: Comma expected.
        AddHandler
                  ~
BC30201: Expression expected.
        AddHandler
                  ~
BC30201: Expression expected.
        AddHandler
                  ~
</expected>)
        End Sub

        <Fact()>
        Public Sub AddHandlerUninitialized()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="AddHandlerNotSimple">
    <file name="goo.vb">
Imports System
Module M1
    Sub Main()
        ' no warnings here, variable is used
        Dim del As System.EventHandler

        ' warning here
        Dim v = AppDomain.CreateDomain("qq")

        AddHandler v.DomainUnload, del

    End Sub
End Module
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC42104: Variable 'del' is used before it has been assigned a value. A null reference exception could result at runtime.
        AddHandler v.DomainUnload, del
                                   ~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub AddHandlerNotSimple()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="AddHandlerNotSimple">
    <file name="goo.vb">
Imports System        
Module M1
    Sub Main()
        Dim del As System.EventHandler =
            Sub(sender As Object, a As EventArgs) Console.Write("unload")

        Dim v = AppDomain.CreateDomain("qq")

        ' real event with arg list
        AddHandler (v.DomainUnload()), del

        ' not an event
        AddHandler (v.GetType()), del

    End Sub
End Module
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30677: 'AddHandler' or 'RemoveHandler' statement event operand must be a dot-qualified expression or a simple name.
        AddHandler (v.DomainUnload()), del
                    ~~~~~~~~~~~~~~~~
BC30677: 'AddHandler' or 'RemoveHandler' statement event operand must be a dot-qualified expression or a simple name.
        AddHandler (v.GetType()), del
                    ~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub RemoveHandlerLambda()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="AddHandlerNotSimple">
    <file name="goo.vb">
Imports System

Module MyClass1

    Sub Main(args As String())
        Dim v = AppDomain.CreateDomain("qq")

        RemoveHandler v.DomainUnload, Sub(sender As Object, a As EventArgs) Console.Write("unload")

        AppDomain.Unload(v)
    End Sub
End Module
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
    BC42326: Lambda expression will not be removed from this event handler. Assign the lambda expression to a variable and use the variable to add and remove the event.
        RemoveHandler v.DomainUnload, Sub(sender As Object, a As EventArgs) Console.Write("unload")
                                      ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

</expected>)
        End Sub

        <Fact()>
        Public Sub RemoveHandlerNotEvent()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="AddHandlerNotSimple">
    <file name="goo.vb">
Imports System        
Module M1
    Sub Main()
        Dim del As System.EventHandler =
            Sub(sender As Object, a As EventArgs) Console.Write("unload")

        Dim v = AppDomain.CreateDomain("qq")

        ' not an event
        AddHandler (v.GetType), del

        ' not anything
        AddHandler v.GetTyp, del

    End Sub
End Module
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30676: 'GetType' is not an event of 'AppDomain'.
        AddHandler (v.GetType), del
                      ~~~~~~~
BC30456: 'GetTyp' is not a member of 'AppDomain'.
        AddHandler v.GetTyp, del
                   ~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub AddHandlerNoConversion()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="AddHandlerNotSimple">
    <file name="goo.vb">
Imports System        
Module M1
    Sub Main()
        Dim v = AppDomain.CreateDomain("qq")

        AddHandler (v.DomainUnload), Sub(sender As Object, sender1 As Object, sender2 As Object) Console.Write("unload")

        AddHandler v.DomainUnload, AddressOf H

        Dim del as Action(of Object, EventArgs) = Sub(sender As Object, a As EventArgs) Console.Write("unload")
        AddHandler v.DomainUnload, del

    End Sub

    Sub H(i as integer)
    End Sub

End Module
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36670: Nested sub does not have a signature that is compatible with delegate 'EventHandler'.
        AddHandler (v.DomainUnload), Sub(sender As Object, sender1 As Object, sender2 As Object) Console.Write("unload")
                                     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC31143: Method 'Public Sub H(i As Integer)' does not have a signature compatible with delegate 'Delegate Sub EventHandler(sender As Object, e As EventArgs)'.
        AddHandler v.DomainUnload, AddressOf H
                                             ~
BC30311: Value of type 'Action(Of Object, EventArgs)' cannot be converted to 'EventHandler'.
        AddHandler v.DomainUnload, del
                                   ~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub LegalGotoCasesTryCatchFinally()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="LegalGotoCasesTryCatchFinally">
    <file name="a.vb">
Module M1
        Sub Main()

            dim x1 = function()
                    labelOK6:
                        goto labelok7
                        if true then
                            goto labelok6
                            labelok7:
                        end if

                        return 23
                    end function

            dim x2 = sub()
                    labelOK8:
                        goto labelok9
                        if true then
                            goto labelok8
                            labelok9:
                        end if
                    end sub

            Try
                Goto LabelOK1
LabelOK1:             
            Catch
                Goto LabelOK2
LabelOK2:             
                Try
                    goto LabelOK1
                    goto LabelOK2:
LabelOK5:
                Catch
                    goto LabelOK1
                    goto LabelOK5
                    goto LabelOK2
                Finally
                End Try
            Finally
                Goto LabelOK3
LabelOK3:             
            End Try
            Exit Sub
        End Sub
    End Module
    </file>
</compilation>)

            CompilationUtils.AssertNoErrors(compilation)
        End Sub

        <WorkItem(543055, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543055")>
        <Fact()>
        Public Sub Bug10583()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="LegalGotoCasesTryCatchFinally">
    <file name="a.vb">
Imports System
Module Program
    Sub Main(args As String())
        Try
            GoTo label
            GoTo label5
        Catch ex As Exception
label:
        Finally
label5:
        End Try
    End Sub
End Module

    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
                                               <expected>
BC30754: 'GoTo label' is not valid because 'label' is inside a 'Try', 'Catch' or 'Finally' statement that does not contain this statement.
            GoTo label
                 ~~~~~
BC30754: 'GoTo label5' is not valid because 'label5' is inside a 'Try', 'Catch' or 'Finally' statement that does not contain this statement.
            GoTo label5
                 ~~~~~~
                                               </expected>)
        End Sub

        <WorkItem(543060, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543060")>
        <Fact()>
        Public Sub SelectCase_ImplicitOperator()
            Dim compilation1 = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="SelectCase">
    <file name="a.vb"><![CDATA[
Imports System
Module M1
    Class X
        Public Shared Operator =(left As X, right As X) As Boolean
            Return True
        End Operator

        Public Shared Operator <>(left As X, right As X) As Boolean
            Return True
        End Operator

        Public Shared Widening Operator CType(expandedName As String) As X
            Return New X()
        End Operator
    End Class

    Sub Main()

    End Sub

    Sub Test(x As X)
        Select Case x
            Case "a"
                Console.WriteLine("Equal to a")
            Case "s"
                Console.WriteLine("Equal to A")
            Case "3"
                Console.WriteLine("Error")
            Case "5"
                Console.WriteLine("Error")
            Case "6"
                Console.WriteLine("Error")
            Case "9"
                Console.WriteLine("Error")
            Case "11"
                Console.WriteLine("Error")
            Case "12"
                Console.WriteLine("Error")
            Case "13"
                Console.WriteLine("Error")
            Case Else
                Console.WriteLine("Error")
        End Select
    End Sub
End Module
    ]]></file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation1,
<expected>
</expected>)
        End Sub

        <WorkItem(543333, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543333")>
        <Fact()>
        Public Sub Binding_Return_As_Declaration()
            Dim compilation1 = CreateCompilationWithMscorlib40(
<compilation name="SelectCase">
    <file name="a.vb"><![CDATA[
Class Program
    Shared  Main()
      Return Nothing
    End sub
End Class
    ]]></file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation1,
<expected>
BC30689: Statement cannot appear outside of a method body.
      Return Nothing
      ~~~~~~~~~~~~~~
BC30429: 'End Sub' must be preceded by a matching 'Sub'.
    End sub
    ~~~~~~~
</expected>)
        End Sub

        <WorkItem(529050, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529050")>
        <Fact>
        Public Sub WhileOutOfMethod()
            Dim source =
<compilation>
    <file name="a.vb">
While (true)
    </file>
</compilation>

            CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source).VerifyDiagnostics(Diagnostic(ERRID.ERR_ExecutableAsDeclaration, "While (true)"))
        End Sub

        <WorkItem(529050, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529050")>
        <Fact>
        Public Sub WhileOutOfMethod_1()
            Dim source =
<compilation>
    <file name="a.vb">
Class c1        
    While (true)
End Class
    </file>
</compilation>

            CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source).VerifyDiagnostics(Diagnostic(ERRID.ERR_ExecutableAsDeclaration, "While (true)"))
        End Sub

        <WorkItem(529051, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529051")>
        <Fact>
        Public Sub IfOutOfMethod()
            Dim source =
<compilation>
    <file name="a.vb">
If (true)
    </file>
</compilation>

            CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source).VerifyDiagnostics(Diagnostic(ERRID.ERR_ExecutableAsDeclaration, "If (true)"))
        End Sub

        <WorkItem(529051, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529051")>
        <Fact>
        Public Sub IfOutOfMethod_1()
            Dim source =
<compilation>
    <file name="a.vb">
Class c1        
    If (true)
End Class
    </file>
</compilation>

            CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source).VerifyDiagnostics(Diagnostic(ERRID.ERR_ExecutableAsDeclaration, "If (true)"))
        End Sub

        <WorkItem(529052, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529052")>
        <Fact>
        Public Sub TryOutOfMethod()
            Dim source =
<compilation>
    <file name="a.vb">
Try
Catch
    </file>
</compilation>

            CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source).VerifyDiagnostics(Diagnostic(ERRID.ERR_ExecutableAsDeclaration, "Try"),
    Diagnostic(ERRID.ERR_ExecutableAsDeclaration, "Catch"))
        End Sub

        <WorkItem(529052, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529052")>
        <Fact>
        Public Sub TryOutOfMethod_1()
            Dim source =
<compilation>
    <file name="a.vb">
Class c1        
    Try
    Catch
End Class
    </file>
</compilation>

            CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source).VerifyDiagnostics(Diagnostic(ERRID.ERR_ExecutableAsDeclaration, "Try"),
    Diagnostic(ERRID.ERR_ExecutableAsDeclaration, "Catch"))
        End Sub

        <WorkItem(529053, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529053")>
        <Fact>
        Public Sub DoOutOfMethod()
            Dim source =
<compilation>
    <file name="a.vb">
Do
    </file>
</compilation>

            CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source).VerifyDiagnostics(Diagnostic(ERRID.ERR_ExecutableAsDeclaration, "Do"))
        End Sub

        <WorkItem(529053, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529053")>
        <Fact>
        Public Sub DoOutOfMethod_1()
            Dim source =
<compilation>
    <file name="a.vb">
Class c1        
    Do
End Class
    </file>
</compilation>

            CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source).VerifyDiagnostics(Diagnostic(ERRID.ERR_ExecutableAsDeclaration, "Do"))
        End Sub

        <WorkItem(11031, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub ElseOutOfMethod()
            Dim source =
<compilation>
    <file name="a.vb">
Else
    </file>
</compilation>

            CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source).VerifyDiagnostics(Diagnostic(ERRID.ERR_ExecutableAsDeclaration, "Else"))
        End Sub

        <WorkItem(11031, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub ElseOutOfMethod_1()
            Dim source =
<compilation>
    <file name="a.vb">
Class c1        
    Else
End Class
    </file>
</compilation>

            CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source).VerifyDiagnostics(Diagnostic(ERRID.ERR_ExecutableAsDeclaration, "Else"))
        End Sub

        <WorkItem(544465, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544465")>
        <Fact()>
        Public Sub DuplicateNullableLocals()
            Dim source =
    <compilation>
        <file name="a.vb">
Option Explicit Off
Module M
    Sub S()
        Dim A? As Integer = 1
        Dim A? As Integer? = 1
    End Sub
End Module
    </file>
    </compilation>

            CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source).VerifyDiagnostics(
                    Diagnostic(ERRID.ERR_CantSpecifyNullableOnBoth, "As Integer?"),
                    Diagnostic(ERRID.ERR_DuplicateLocals1, "A?").WithArguments("A"))
        End Sub

        <WorkItem(544431, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544431")>
        <Fact()>
        Public Sub IllegalModifiers()
            Dim source =
<compilation>
    <file name="a.vb">
Class C
    Public Custom E
End Class
    </file>
</compilation>

            CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source).VerifyDiagnostics(Diagnostic(ERRID.ERR_InvalidUseOfCustomModifier, "Custom"))
        End Sub

        <Fact()>
        Public Sub InvalidCode_ConstInterface()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Const Interface
    </file>
</compilation>)
            compilation.AssertTheseDiagnostics(<errors>
BC30397: 'Const' is not valid on an Interface declaration.
Const Interface
~~~~~
BC30253: 'Interface' must end with a matching 'End Interface'.
Const Interface
~~~~~~~~~~~~~~~
BC30203: Identifier expected.
Const Interface
               ~
                </errors>)
        End Sub

        <WorkItem(545196, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545196")>
        <Fact()>
        Public Sub InvalidCode_Event()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Event
    </file>
</compilation>)
            compilation.AssertTheseParseDiagnostics(<errors>
BC30203: Identifier expected.
Event
     ~
                </errors>)
        End Sub

        <Fact>
        Public Sub StopAndEnd_1()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Module Module1

    Public Sub Main()
        Dim m = GetType(Module1)

        System.Console.WriteLine(m.GetMethod("TestEnd").GetMethodImplementationFlags)
        System.Console.WriteLine(m.GetMethod("TestStop").GetMethodImplementationFlags)
        System.Console.WriteLine(m.GetMethod("Dummy").GetMethodImplementationFlags)

        Try
            System.Console.WriteLine("Before End")
            TestEnd()
            System.Console.WriteLine("After End")
        Finally
            System.Console.WriteLine("In Finally")
        End Try

        System.Console.WriteLine("After Try")
    End Sub

    Sub TestEnd()
        End
    End Sub

    Sub TestStop()
        Stop
    End Sub

    Sub Dummy()
    End Sub
End Module
    ]]>
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseExe)

            Dim compilationVerifier = CompileAndVerify(compilation,
                                                       symbolValidator:=Sub(m As ModuleSymbol)
                                                                            Dim m1 = m.ContainingAssembly.GetTypeByMetadataName("Module1")
                                                                            Assert.Equal(MethodImplAttributes.Managed Or MethodImplAttributes.NoInlining Or MethodImplAttributes.NoOptimization,
                                                                                         DirectCast(m1.GetMembers("TestEnd").Single(), PEMethodSymbol).ImplementationAttributes)
                                                                            Assert.Equal(MethodImplAttributes.Managed,
                                                                                         DirectCast(m1.GetMembers("TestStop").Single(), PEMethodSymbol).ImplementationAttributes)
                                                                            Assert.Equal(MethodImplAttributes.Managed,
                                                                                         DirectCast(m1.GetMembers("Dummy").Single(), PEMethodSymbol).ImplementationAttributes)
                                                                        End Sub)

            compilationVerifier.VerifyIL("Module1.TestEnd",
            <![CDATA[
{
  // Code size        6 (0x6)
  .maxstack  0
  IL_0000:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.EndApp()"
  IL_0005:  ret
}
]]>)

            compilationVerifier.VerifyIL("Module1.TestStop",
            <![CDATA[
{
  // Code size        6 (0x6)
  .maxstack  0
  IL_0000:  call       "Sub System.Diagnostics.Debugger.Break()"
  IL_0005:  ret
}
]]>)

            compilation = compilation.WithOptions(compilation.Options.WithOutputKind(OutputKind.DynamicallyLinkedLibrary))

            AssertTheseDiagnostics(compilation,
<expected>
BC30615: 'End' statement cannot be used in class library projects.
        End
        ~~~
</expected>)

        End Sub

        <Fact>
        Public Sub StopAndEnd_2()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Module Module1
    Public Sub Main()
        Dim x As Object
        Dim y As Object


        Stop
        x.ToString()

        End
        y.ToString()
    End Sub
End Module
    ]]>
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseExe)

            AssertTheseDiagnostics(compilation,
<expected>
BC42104: Variable 'x' is used before it has been assigned a value. A null reference exception could result at runtime.
        x.ToString()
        ~
</expected>)

        End Sub

        <ConditionalFact(GetType(WindowsDesktopOnly), Reason:="https://github.com/dotnet/roslyn/issues/28044")>
        Public Sub StopAndEnd_3()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System

Module Module1
    Public state As Integer = 0

    Public Sub Main()
        On Error GoTo handler
        Throw New NullReferenceException()
        Stop
        Console.WriteLine("Done")

        Return
handler:
        Console.WriteLine(Microsoft.VisualBasic.Information.Err.GetException().GetType())
        If state = 1 Then
            Resume
        End If

        Resume Next
    End Sub
End Module

Namespace System.Diagnostics
    Public Class Debugger
        Public Shared Sub Break()
            Console.WriteLine("In Break")
            Select Case Module1.state
                Case 0, 1
                    Module1.state += 1
                Case Else
                    Console.WriteLine("Test issue!!!")
                    Return 
            End Select

            Throw New NotSupportedException()
        End Sub
    End Class
End Namespace
    ]]>
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseExe)

            Dim compilationVerifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
System.NullReferenceException
In Break
System.NotSupportedException
In Break
System.NotSupportedException
Done
]]>)

        End Sub

        <WorkItem(45158, "https://github.com/dotnet/roslyn/issues/45158")>
        <Fact>
        Public Sub EndWithSingleLineIf()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System

Module Module1
    Public Sub Main()
        If True Then End Else Console.WriteLine("Test")
    End Sub
End Module
    ]]>
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseExe)
            AssertTheseDiagnostics(compilation)
        End Sub

        <WorkItem(45158, "https://github.com/dotnet/roslyn/issues/45158")>
        <Fact>
        Public Sub EndWithSingleLineIfWithDll()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System

Module Module1
    Public Sub Main()
        If True Then End Else Console.WriteLine("Test")
    End Sub
End Module
    ]]>
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseDll)
            AssertTheseDiagnostics(compilation,
<expected>
BC30615: 'End' statement cannot be used in class library projects.
        If True Then End Else Console.WriteLine("Test")
                     ~~~
</expected>)
        End Sub

        <WorkItem(45158, "https://github.com/dotnet/roslyn/issues/45158")>
        <Fact>
        Public Sub EndWithMultiLineIf()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System

Module Module1
    Public Sub Main()
        If True Then
            End
        Else
            Console.WriteLine("Test")
        End If
    End Sub
End Module
    ]]>
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseExe)
            AssertTheseDiagnostics(compilation)
        End Sub

        <WorkItem(660010, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/660010")>
        <Fact>
        Public Sub Regress660010()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Class C
   Inherits value
End C

    ]]>
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source, references:=XmlReferences)

            AssertTheseDiagnostics(compilation,
<expected>
BC30481: 'Class' statement must end with a matching 'End Class'.
Class C
~~~~~~~
BC30002: Type 'value' is not defined.
   Inherits value
            ~~~~~
BC30678: 'End' statement not valid.
End C
~~~
</expected>)

        End Sub

        <WorkItem(718436, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/718436")>
        <Fact>
        Public Sub NotYetImplementedStatement()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Class C
    Sub M()
        Inherits A
        Implements I
        Imports X
        Option Strict On
    End Sub
End Class
    ]]>
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source)

            AssertTheseDiagnostics(compilation,
<expected>
BC30024: Statement is not valid inside a method.
        Inherits A
        ~~~~~~~~~~
BC30024: Statement is not valid inside a method.
        Implements I
        ~~~~~~~~~~~~
BC30024: Statement is not valid inside a method.
        Imports X
        ~~~~~~~~~
BC30024: Statement is not valid inside a method.
        Option Strict On
        ~~~~~~~~~~~~~~~~
</expected>)

        End Sub

        <Fact(), WorkItem(603290, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/603290")>
        Public Sub InaccessibleRemoveAccessor()

            Dim ilSource = <![CDATA[
.class public auto ansi E1
       extends [mscorlib]System.Object
{
  .field private class [mscorlib]System.Action TestEvent
  .method public specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method E1::.ctor

  .method public specialname instance void 
          add_Test(class [mscorlib]System.Action obj) cil managed synchronized
  {
    // Code size       24 (0x18)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.0
    IL_0002:  ldfld      class [mscorlib]System.Action E1::TestEvent
    IL_0007:  ldarg.1
    IL_0008:  call       class [mscorlib]System.Delegate [mscorlib]System.Delegate::Combine(class [mscorlib]System.Delegate,
                                                                                            class [mscorlib]System.Delegate)
    IL_000d:  castclass  [mscorlib]System.Action
    IL_0012:  stfld      class [mscorlib]System.Action E1::TestEvent
    IL_0017:  ret
  } // end of method E1::add_Test

  .method family specialname instance void 
          remove_Test(class [mscorlib]System.Action obj) cil managed synchronized
  {
    // Code size       24 (0x18)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.0
    IL_0002:  ldfld      class [mscorlib]System.Action E1::TestEvent
    IL_0007:  ldarg.1
    IL_0008:  call       class [mscorlib]System.Delegate [mscorlib]System.Delegate::Remove(class [mscorlib]System.Delegate,
                                                                                           class [mscorlib]System.Delegate)
    IL_000d:  castclass  [mscorlib]System.Action
    IL_0012:  stfld      class [mscorlib]System.Action E1::TestEvent
    IL_0017:  ret
  } // end of method E1::remove_Test

  .event [mscorlib]System.Action Test
  {
    .addon instance void E1::add_Test(class [mscorlib]System.Action)
    .removeon instance void E1::remove_Test(class [mscorlib]System.Action)
  } // end of event E1::Test
} // end of class E1
]]>

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1

    Sub Main()
	    Dim e = New E1()
	    Dim d As System.Action = Nothing

	    AddHandler e.Test, d
        RemoveHandler e.Test, d
    End Sub

End Module

Class E2
    Inherits E1

    Sub Main()
	    Dim d As System.Action = Nothing

	    AddHandler Test, d
        RemoveHandler Test, d
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithCustomILSource(compilationDef, ilSource.Value, includeVbRuntime:=True)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30390: 'E1.Protected RemoveHandler Event Test(obj As Action)' is not accessible in this context because it is 'Protected'.
        RemoveHandler e.Test, d
                      ~~~~~~
</expected>)

            'CompileAndVerify(compilation)
        End Sub

        <Fact(), WorkItem(603290, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/603290")>
        Public Sub InaccessibleAddAccessor()

            Dim ilSource = <![CDATA[
.class public auto ansi E1
       extends [mscorlib]System.Object
{
  .field private class [mscorlib]System.Action TestEvent
  .method public specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method E1::.ctor

  .method family specialname instance void 
          add_Test(class [mscorlib]System.Action obj) cil managed synchronized
  {
    // Code size       24 (0x18)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.0
    IL_0002:  ldfld      class [mscorlib]System.Action E1::TestEvent
    IL_0007:  ldarg.1
    IL_0008:  call       class [mscorlib]System.Delegate [mscorlib]System.Delegate::Combine(class [mscorlib]System.Delegate,
                                                                                            class [mscorlib]System.Delegate)
    IL_000d:  castclass  [mscorlib]System.Action
    IL_0012:  stfld      class [mscorlib]System.Action E1::TestEvent
    IL_0017:  ret
  } // end of method E1::add_Test

  .method public specialname instance void 
          remove_Test(class [mscorlib]System.Action obj) cil managed synchronized
  {
    // Code size       24 (0x18)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.0
    IL_0002:  ldfld      class [mscorlib]System.Action E1::TestEvent
    IL_0007:  ldarg.1
    IL_0008:  call       class [mscorlib]System.Delegate [mscorlib]System.Delegate::Remove(class [mscorlib]System.Delegate,
                                                                                           class [mscorlib]System.Delegate)
    IL_000d:  castclass  [mscorlib]System.Action
    IL_0012:  stfld      class [mscorlib]System.Action E1::TestEvent
    IL_0017:  ret
  } // end of method E1::remove_Test

  .event [mscorlib]System.Action Test
  {
    .addon instance void E1::add_Test(class [mscorlib]System.Action)
    .removeon instance void E1::remove_Test(class [mscorlib]System.Action)
  } // end of event E1::Test
} // end of class E1
]]>

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1

    Sub Main()
	    Dim e = New E1()
	    Dim d As System.Action = Nothing

        AddHandler e.Test, d
        RemoveHandler e.Test, d
    End Sub

End Module

Class E2
    Inherits E1

    Sub Main()
	    Dim d As System.Action = Nothing

	    AddHandler Test, d
        RemoveHandler Test, d
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithCustomILSource(compilationDef, ilSource.Value, includeVbRuntime:=True)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30390: 'E1.Protected AddHandler Event Test(obj As Action)' is not accessible in this context because it is 'Protected'.
        AddHandler e.Test, d
                   ~~~~~~
</expected>)

            'CompileAndVerify(compilation)
        End Sub

        <Fact(), WorkItem(603290, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/603290")>
        Public Sub EventTypeIsNotADelegate()

            Dim ilSource = <![CDATA[
.class public auto ansi E1
       extends [mscorlib]System.Object
{
  .method public specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method E1::.ctor

  .method public specialname instance void 
          add_Test(class E1 obj) cil managed synchronized
  {
    IL_0017:  ret
  } // end of method E1::add_Test

  .method public specialname instance void 
          remove_Test(class E1 obj) cil managed synchronized
  {
    IL_0017:  ret
  } // end of method E1::remove_Test

  .event E1 Test
  {
    .addon instance void E1::add_Test(class E1)
    .removeon instance void E1::remove_Test(class E1)
  } // end of event E1::Test
} // end of class E1
]]>

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1

    Sub Main()
	    Dim e = New E1()

        AddHandler e.Test, e
        RemoveHandler e.Test, e
    End Sub

End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithCustomILSource(compilationDef, ilSource.Value, includeVbRuntime:=True)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC37223: 'Public Event Test As E1' is an unsupported event.
        AddHandler e.Test, e
                   ~~~~~~
BC37223: 'Public Event Test As E1' is an unsupported event.
        RemoveHandler e.Test, e
                      ~~~~~~
</expected>)

            'CompileAndVerify(compilation)
        End Sub

        <Fact(), WorkItem(603290, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/603290")>
        Public Sub InvalidAddAccessor_01()

            Dim ilSource = <![CDATA[
.class public auto ansi E1
       extends [mscorlib]System.Object
{
  .method public specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method E1::.ctor

  .method public specialname instance void 
          add_Test(class E1 obj) cil managed synchronized
  {
    IL_0008:  ldstr      "add_Test"
    IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0017:  ret
  } // end of method E1::add_Test

  .method public specialname instance void 
          remove_Test(class [mscorlib]System.Action obj) cil managed synchronized
  {
    IL_0008:  ldstr      "remove_Test"
    IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0017:  ret
  } // end of method E1::remove_Test

  .event [mscorlib]System.Action Test
  {
    .addon instance void E1::add_Test(class E1)
    .removeon instance void E1::remove_Test(class [mscorlib]System.Action)
  } // end of event E1::Test
} // end of class E1
]]>

            Dim compilationDef1 =
<compilation>
    <file name="a.vb">
Module Module1

    Sub Main()
        Dim e = New E1()
        Dim d As System.Action = Nothing

        AddHandler e.Test, d
        AddHandler e.Test, e
        RemoveHandler e.Test, e
    End Sub

End Module
    </file>
</compilation>

            Dim compilation1 = CreateCompilationWithCustomILSource(compilationDef1, ilSource.Value, includeVbRuntime:=True)

            CompilationUtils.AssertTheseDiagnostics(compilation1,
<expected>
BC30657: 'Public AddHandler Event Test(obj As E1)' has a return type that is not supported or parameter types that are not supported.
        AddHandler e.Test, d
                   ~~~~~~
BC30657: 'Public AddHandler Event Test(obj As E1)' has a return type that is not supported or parameter types that are not supported.
        AddHandler e.Test, e
                   ~~~~~~
BC30311: Value of type 'E1' cannot be converted to 'Action'.
        AddHandler e.Test, e
                           ~
BC30311: Value of type 'E1' cannot be converted to 'Action'.
        RemoveHandler e.Test, e
                              ~
</expected>)

            'CompileAndVerify(compilation1)

            Dim compilationDef2 =
<compilation>
    <file name="a.vb">
Module Module1

    Sub Main()
        Dim e = New E1()
        Dim d As System.Action = Nothing

        RemoveHandler e.Test, d
    End Sub

End Module
    </file>
</compilation>

            Dim compilation2 = CreateCompilationWithCustomILSource(compilationDef2, ilSource.Value, includeVbRuntime:=True, options:=TestOptions.ReleaseExe)

            CompileAndVerify(compilation2, expectedOutput:="remove_Test")
        End Sub

        <Fact(), WorkItem(603290, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/603290")>
        Public Sub InvalidAddAccessor_02()

            Dim ilSource = <![CDATA[
.class public auto ansi E1
       extends [mscorlib]System.Object
{
  .method public specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method E1::.ctor

  .method public specialname instance void 
          add_Test() cil managed synchronized
  {
    IL_0008:  ldstr      "add_Test"
    IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0017:  ret
  } // end of method E1::add_Test

  .method public specialname instance void 
          remove_Test(class [mscorlib]System.Action obj) cil managed synchronized
  {
    IL_0008:  ldstr      "remove_Test"
    IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0017:  ret
  } // end of method E1::remove_Test

  .event [mscorlib]System.Action Test
  {
    .addon instance void E1::add_Test()
    .removeon instance void E1::remove_Test(class [mscorlib]System.Action)
  } // end of event E1::Test
} // end of class E1
]]>

            Dim compilationDef1 =
<compilation>
    <file name="a.vb">
Module Module1

    Sub Main()
        Dim e = New E1()
        Dim d As System.Action = Nothing

        AddHandler e.Test, d
        AddHandler e.Test, e
        RemoveHandler e.Test, e
    End Sub

End Module
    </file>
</compilation>

            Dim compilation1 = CreateCompilationWithCustomILSource(compilationDef1, ilSource.Value, includeVbRuntime:=True)

            CompilationUtils.AssertTheseDiagnostics(compilation1,
<expected>
BC30657: 'Public AddHandler Event Test()' has a return type that is not supported or parameter types that are not supported.
        AddHandler e.Test, d
                   ~~~~~~
BC30657: 'Public AddHandler Event Test()' has a return type that is not supported or parameter types that are not supported.
        AddHandler e.Test, e
                   ~~~~~~
BC30311: Value of type 'E1' cannot be converted to 'Action'.
        AddHandler e.Test, e
                           ~
BC30311: Value of type 'E1' cannot be converted to 'Action'.
        RemoveHandler e.Test, e
                              ~
</expected>)

            'CompileAndVerify(compilation1)

            Dim compilationDef2 =
<compilation>
    <file name="a.vb">
Module Module1

    Sub Main()
        Dim e = New E1()
        Dim d As System.Action = Nothing

        RemoveHandler e.Test, d
    End Sub

End Module
    </file>
</compilation>

            Dim compilation2 = CreateCompilationWithCustomILSource(compilationDef2, ilSource.Value, includeVbRuntime:=True, options:=TestOptions.ReleaseExe)

            CompileAndVerify(compilation2, expectedOutput:="remove_Test")
        End Sub

        <Fact(), WorkItem(603290, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/603290")>
        Public Sub InvalidAddAccessor_03()

            Dim ilSource = <![CDATA[
.class public auto ansi E1
       extends [mscorlib]System.Object
{
  .method public specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method E1::.ctor

  .method public specialname instance void 
          add_Test(class [mscorlib]System.Action obj1, class E1 obj2) cil managed synchronized
  {
    IL_0008:  ldstr      "add_Test"
    IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0017:  ret
  } // end of method E1::add_Test

  .method public specialname instance void 
          remove_Test(class [mscorlib]System.Action obj) cil managed synchronized
  {
    IL_0008:  ldstr      "remove_Test"
    IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0017:  ret
  } // end of method E1::remove_Test

  .event [mscorlib]System.Action Test
  {
    .addon instance void E1::add_Test(class [mscorlib]System.Action, class E1)
    .removeon instance void E1::remove_Test(class [mscorlib]System.Action)
  } // end of event E1::Test
} // end of class E1
]]>

            Dim compilationDef1 =
<compilation>
    <file name="a.vb">
Module Module1

    Sub Main()
        Dim e = New E1()
        Dim d As System.Action = Nothing

        AddHandler e.Test, d
        AddHandler e.Test, e
        RemoveHandler e.Test, e
    End Sub

End Module
    </file>
</compilation>

            Dim compilation1 = CreateCompilationWithCustomILSource(compilationDef1, ilSource.Value, includeVbRuntime:=True)

            CompilationUtils.AssertTheseDiagnostics(compilation1,
<expected>
BC30657: 'Public AddHandler Event Test(obj1 As Action, obj2 As E1)' has a return type that is not supported or parameter types that are not supported.
        AddHandler e.Test, d
                   ~~~~~~
BC30657: 'Public AddHandler Event Test(obj1 As Action, obj2 As E1)' has a return type that is not supported or parameter types that are not supported.
        AddHandler e.Test, e
                   ~~~~~~
BC30311: Value of type 'E1' cannot be converted to 'Action'.
        AddHandler e.Test, e
                           ~
BC30311: Value of type 'E1' cannot be converted to 'Action'.
        RemoveHandler e.Test, e
                              ~
</expected>)

            'CompileAndVerify(compilation1)

            Dim compilationDef2 =
<compilation>
    <file name="a.vb">
Module Module1

    Sub Main()
        Dim e = New E1()
        Dim d As System.Action = Nothing

        RemoveHandler e.Test, d
    End Sub

End Module
    </file>
</compilation>

            Dim compilation2 = CreateCompilationWithCustomILSource(compilationDef2, ilSource.Value, includeVbRuntime:=True, options:=TestOptions.ReleaseExe)

            CompileAndVerify(compilation2, expectedOutput:="remove_Test")
        End Sub

        <Fact(), WorkItem(603290, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/603290")>
        Public Sub InvalidRemoveAccessor_01()

            Dim ilSource = <![CDATA[
.class public auto ansi E1
       extends [mscorlib]System.Object
{
  .method public specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method E1::.ctor

  .method public specialname instance void 
          add_Test(class [mscorlib]System.Action obj) cil managed synchronized
  {
    IL_0008:  ldstr      "add_Test"
    IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0017:  ret
  } // end of method E1::add_Test

  .method public specialname instance void 
          remove_Test(class E1 obj) cil managed synchronized
  {
    IL_0008:  ldstr      "remove_Test"
    IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0017:  ret
  } // end of method E1::remove_Test

  .event [mscorlib]System.Action Test
  {
    .addon instance void E1::add_Test(class [mscorlib]System.Action)
    .removeon instance void E1::remove_Test(class E1)
  } // end of event E1::Test
} // end of class E1
]]>

            Dim compilationDef1 =
<compilation>
    <file name="a.vb">
Module Module1

    Sub Main()
        Dim e = New E1()
        Dim d As System.Action = Nothing

        AddHandler e.Test, e
        RemoveHandler e.Test, e
        RemoveHandler e.Test, d
    End Sub

End Module
    </file>
</compilation>

            Dim compilation1 = CreateCompilationWithCustomILSource(compilationDef1, ilSource.Value, includeVbRuntime:=True)

            CompilationUtils.AssertTheseDiagnostics(compilation1,
<expected>
BC30311: Value of type 'E1' cannot be converted to 'Action'.
        AddHandler e.Test, e
                           ~
BC30657: 'Public RemoveHandler Event Test(obj As E1)' has a return type that is not supported or parameter types that are not supported.
        RemoveHandler e.Test, e
                      ~~~~~~
BC30311: Value of type 'E1' cannot be converted to 'Action'.
        RemoveHandler e.Test, e
                              ~
BC30657: 'Public RemoveHandler Event Test(obj As E1)' has a return type that is not supported or parameter types that are not supported.
        RemoveHandler e.Test, d
                      ~~~~~~
</expected>)

            'CompileAndVerify(compilation1)

            Dim compilationDef2 =
<compilation>
    <file name="a.vb">
Module Module1

    Sub Main()
        Dim e = New E1()
        Dim d As System.Action = Nothing

        AddHandler e.Test, d
    End Sub

End Module
    </file>
</compilation>

            Dim compilation2 = CreateCompilationWithCustomILSource(compilationDef2, ilSource.Value, includeVbRuntime:=True, options:=TestOptions.ReleaseExe)

            CompileAndVerify(compilation2, expectedOutput:="add_Test")
        End Sub

        <Fact(), WorkItem(603290, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/603290")>
        Public Sub InvalidRemoveAccessor_02()

            Dim ilSource = <![CDATA[
.class public auto ansi E1
       extends [mscorlib]System.Object
{
  .method public specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method E1::.ctor

  .method public specialname instance void 
          add_Test(class [mscorlib]System.Action) cil managed synchronized
  {
    IL_0008:  ldstr      "add_Test"
    IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0017:  ret
  } // end of method E1::add_Test

  .method public specialname instance void 
          remove_Test() cil managed synchronized
  {
    IL_0008:  ldstr      "remove_Test"
    IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0017:  ret
  } // end of method E1::remove_Test

  .event [mscorlib]System.Action Test
  {
    .addon instance void E1::add_Test(class [mscorlib]System.Action)
    .removeon instance void E1::remove_Test()
  } // end of event E1::Test
} // end of class E1
]]>

            Dim compilationDef1 =
<compilation>
    <file name="a.vb">
Module Module1

    Sub Main()
        Dim e = New E1()
        Dim d As System.Action = Nothing

        AddHandler e.Test, e
        RemoveHandler e.Test, e
        RemoveHandler e.Test, d
    End Sub

End Module
    </file>
</compilation>

            Dim compilation1 = CreateCompilationWithCustomILSource(compilationDef1, ilSource.Value, includeVbRuntime:=True)

            CompilationUtils.AssertTheseDiagnostics(compilation1,
<expected>
BC30311: Value of type 'E1' cannot be converted to 'Action'.
        AddHandler e.Test, e
                           ~
BC30657: 'Public RemoveHandler Event Test()' has a return type that is not supported or parameter types that are not supported.
        RemoveHandler e.Test, e
                      ~~~~~~
BC30311: Value of type 'E1' cannot be converted to 'Action'.
        RemoveHandler e.Test, e
                              ~
BC30657: 'Public RemoveHandler Event Test()' has a return type that is not supported or parameter types that are not supported.
        RemoveHandler e.Test, d
                      ~~~~~~
</expected>)

            'CompileAndVerify(compilation1)

            Dim compilationDef2 =
<compilation>
    <file name="a.vb">
Module Module1

    Sub Main()
        Dim e = New E1()
        Dim d As System.Action = Nothing

        AddHandler e.Test, d
    End Sub

End Module
    </file>
</compilation>

            Dim compilation2 = CreateCompilationWithCustomILSource(compilationDef2, ilSource.Value, includeVbRuntime:=True, options:=TestOptions.ReleaseExe)

            CompileAndVerify(compilation2, expectedOutput:="add_Test")
        End Sub

        <Fact(), WorkItem(603290, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/603290")>
        Public Sub InvalidRemoveAccessor_03()

            Dim ilSource = <![CDATA[
.class public auto ansi E1
       extends [mscorlib]System.Object
{
  .method public specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method E1::.ctor

  .method public specialname instance void 
          add_Test(class [mscorlib]System.Action obj1) cil managed synchronized
  {
    IL_0008:  ldstr      "add_Test"
    IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0017:  ret
  } // end of method E1::add_Test

  .method public specialname instance void 
          remove_Test(class [mscorlib]System.Action obj1, class E1 obj2) cil managed synchronized
  {
    IL_0008:  ldstr      "remove_Test"
    IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0017:  ret
  } // end of method E1::remove_Test

  .event [mscorlib]System.Action Test
  {
    .addon instance void E1::add_Test(class [mscorlib]System.Action)
    .removeon instance void E1::remove_Test(class [mscorlib]System.Action, class E1)
  } // end of event E1::Test
} // end of class E1
]]>

            Dim compilationDef1 =
<compilation>
    <file name="a.vb">
Module Module1

    Sub Main()
        Dim e = New E1()
        Dim d As System.Action = Nothing

        AddHandler e.Test, e
        RemoveHandler e.Test, e
        RemoveHandler e.Test, d
    End Sub

End Module
    </file>
</compilation>

            Dim compilation1 = CreateCompilationWithCustomILSource(compilationDef1, ilSource.Value, includeVbRuntime:=True)

            CompilationUtils.AssertTheseDiagnostics(compilation1,
<expected>
BC30311: Value of type 'E1' cannot be converted to 'Action'.
        AddHandler e.Test, e
                           ~
BC30657: 'Public RemoveHandler Event Test(obj1 As Action, obj2 As E1)' has a return type that is not supported or parameter types that are not supported.
        RemoveHandler e.Test, e
                      ~~~~~~
BC30311: Value of type 'E1' cannot be converted to 'Action'.
        RemoveHandler e.Test, e
                              ~
BC30657: 'Public RemoveHandler Event Test(obj1 As Action, obj2 As E1)' has a return type that is not supported or parameter types that are not supported.
        RemoveHandler e.Test, d
                      ~~~~~~
</expected>)

            'CompileAndVerify(compilation1)

            Dim compilationDef2 =
<compilation>
    <file name="a.vb">
Module Module1

    Sub Main()
        Dim e = New E1()
        Dim d As System.Action = Nothing

        AddHandler e.Test, d
    End Sub

End Module
    </file>
</compilation>

            Dim compilation2 = CreateCompilationWithCustomILSource(compilationDef2, ilSource.Value, includeVbRuntime:=True, options:=TestOptions.ReleaseExe)

            CompileAndVerify(compilation2, expectedOutput:="add_Test")
        End Sub

        <Fact(), WorkItem(603290, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/603290")>
        Public Sub NonVoidAccessors()

            Dim ilSource = <![CDATA[
.class public auto ansi E1
       extends [mscorlib]System.Object
{
  .method public specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method E1::.ctor

  .method public specialname instance int32 
          add_Test(class [mscorlib]System.Action obj) cil managed synchronized
  {
    IL_0008:  ldstr      "add_Test"
    IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0016:  ldc.i4.0
    IL_0017:  ret
  } // end of method E1::add_Test

  .method public specialname instance int32 
          remove_Test(class [mscorlib]System.Action obj) cil managed synchronized
  {
    IL_0008:  ldstr      "remove_Test"
    IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0016:  ldc.i4.0
    IL_0017:  ret
  } // end of method E1::remove_Test

  .event [mscorlib]System.Action Test
  {
    .addon instance int32 E1::add_Test(class [mscorlib]System.Action)
    .removeon instance int32 E1::remove_Test(class [mscorlib]System.Action)
  } // end of event E1::Test
} // end of class E1
]]>

            Dim compilationDef1 =
<compilation>
    <file name="a.vb">
Module Module1

    Sub Main()
        Dim e = New E1()
        Dim d As System.Action = Nothing

        AddHandler e.Test, d
        RemoveHandler e.Test, d
    End Sub

End Module
    </file>
</compilation>

            Dim compilation1 = CreateCompilationWithCustomILSource(compilationDef1, ilSource.Value, includeVbRuntime:=True, options:=TestOptions.ReleaseExe)

            CompileAndVerify(compilation1, expectedOutput:="add_Test
remove_Test")
        End Sub

        ''' <summary>
        ''' Tests that FULLWIDTH COLON (U+FF1A) is never parsed as part of XML name,
        ''' but is instead parsed as a statement separator when it immediately follows an XML name.
        ''' If the next token is an identifier or keyword, it should be parsed as a separate statement.
        ''' An XML name should never include more than one colon.
        ''' See also: http://fileformat.info/info/unicode/char/FF1A
        ''' </summary>
        <Fact>
        <WorkItem(529880, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529880")>
        Public Sub FullWidthColonInXmlNames()

            ' FULLWIDTH COLON is represented by "~" below
            Dim source = <![CDATA[
Imports System

Module M

    Sub Main()
        Test1()
        Test2()
        Test3()
        Test4()
        Test5()
        Test6()
        Test7()
        Test8()
    End Sub

    Sub Test1()
        Console.WriteLine(">1")
        Dim x = <a/>.@xml:goo
        Console.WriteLine("<1")
    End Sub

    Sub Test2()
        Console.WriteLine(">2")
        Dim x = <a/>.@xml:goo:goo
        Console.WriteLine("<2")
    End Sub

    Sub Test3()
        Console.WriteLine(">3")
        Dim x = <a/>.@xml:return
        Console.WriteLine("<3")
    End Sub

    Sub Test4()
        Console.WriteLine(">4")
        Dim x = <a/>.@xml:return:return
        Console.WriteLine("<4")
    End Sub

    Sub Test5()
        Console.WriteLine(">5")
        Dim x = <a/>.@xml~goo
        Console.WriteLine("<5")
    End Sub

    Sub Test6()
        Console.WriteLine(">6")
        Dim x = <a/>.@xml~return
        Console.WriteLine("<6")
    End Sub

    Sub Test7()
        Console.WriteLine(">7")
        Dim x = <a/>.@xml~goo~return
        Console.WriteLine("<7")
    End Sub

    Sub Test8()
        Console.WriteLine(">8")
        Dim x = <a/>.@xml~REM
        Console.WriteLine("<8")
    End Sub

    Sub goo
        Console.WriteLine("goo")
    End Sub

    Sub [return]
        Console.WriteLine("return")
    End Sub

    Sub [REM]
        Console.WriteLine("REM")
    End Sub
End Module]]>.Value.Replace("~"c, SyntaxFacts.FULLWIDTH_COLON)

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
                <compilation name="FullWidthColonInXmlNames">
                    <file name="M.vb"><%= source %></file>
                </compilation>,
                XmlReferences,
                TestOptions.ReleaseExe)

            CompileAndVerify(compilation, expectedOutput:=<![CDATA[
>1
<1
>2
goo
<2
>3
<3
>4
>5
goo
<5
>6
>7
goo
>8
<8]]>.Value.Replace(vbLf, Environment.NewLine))
        End Sub

    End Class

End Namespace
