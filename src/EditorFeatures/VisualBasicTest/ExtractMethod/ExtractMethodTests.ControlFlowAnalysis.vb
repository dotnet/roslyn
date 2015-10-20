' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.ExtractMethod
    Partial Public Class ExtractMethodTests
        ''' <summary>
        ''' This contains tests for Extract Method components that depend on Control Flow Analysis API
        ''' (A) Selection Validator
        ''' (B) Analyzer
        ''' </summary>
        ''' <remarks></remarks>
        Public Class FlowAnalysis

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestExitSub()
                Dim code = <text>Class Test
    Sub Test()
        [|Exit Sub|]
    End Sub
End Class</text>

                ExpectExtractMethodToFail(code)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestExitFunction()
                Dim code = <text>Class Test
    Function Test1() As Integer
        Console.Write(42)
        [|Test1 = 1
        Console.Write(5)
        Exit Function|]
    End Function
End Class</text>

                ExpectExtractMethodToFail(code)
            End Sub

            <WorkItem(540046)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestReturnStatement()
                Dim code = <text>Class A
    Function Test1() As Integer
        Dim x As Integer = 5
        Console.Write(x)
        [|Return x|]
    End Function
End Class</text>

                Dim expected = <text>Class A
    Function Test1() As Integer
        Dim x As Integer = 5
        Console.Write(x)
        Return NewMethod(x)
    End Function

    Private Shared Function NewMethod(x As Integer) As Integer
        Return x
    End Function
End Class</text>

                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestDoBranch()
                Dim code = <text>Class A
    Function Test1() As Integer
        Dim x As Integer = 5
        Console.Write(x)
        Dim i As Integer
        [|Do
            Console.Write(i)
            i = i + 1
        Loop Until i > 5|]
        Return x
    End Function
End Class</text>

                Dim expected = <text>Class A
    Function Test1() As Integer
        Dim x As Integer = 5
        Console.Write(x)
        Dim i As Integer
        NewMethod(i)
        Return x
    End Function

    Private Shared Sub NewMethod(i As Integer)
        Do
            Console.Write(i)
            i = i + 1
        Loop Until i > 5
    End Sub
End Class</text>

                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestDoBranchInvalidSelection()
                Dim code = <text>Class A
    Function Test1() As Integer
        Dim x As Integer = 5
        Console.Write(x)
        Dim i As Integer
        [|Do
            Console.Write(i)|]
            i = i + 1
        Loop Until i > 5
        Return x
    End Function
End Class</text>

                Dim expected = <text>Class A
    Function Test1() As Integer
        Dim x As Integer = 5
        Console.Write(x)
        Dim i As Integer
        NewMethod(i)
        Return x
    End Function

    Private Shared Sub NewMethod(i As Integer)
        Do
            Console.Write(i)
            i = i + 1
        Loop Until i > 5
    End Sub
End Class</text>

                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestDoBranchWithContinue()
                Dim code = <text>Class A
    Function Test1() As Integer
        Dim x As Integer = 5
        Console.Write(x)
        Dim i As Integer
        [|Do
            Console.Write(i)
            i = i + 1
            Continue Do
            'Blah
        Loop Until i > 5|]
        Return x
    End Function
End Class</text>

                Dim expected = <text>Class A
    Function Test1() As Integer
        Dim x As Integer = 5
        Console.Write(x)
        Dim i As Integer
        NewMethod(i)
        Return x
    End Function

    Private Shared Sub NewMethod(i As Integer)
        Do
            Console.Write(i)
            i = i + 1
            Continue Do
            'Blah
        Loop Until i > 5
    End Sub
End Class</text>

                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestInvalidSelectionLeftOfAssignment()
                Dim code = <text>Class A
    Protected x As Integer = 1
    Public Sub New()
        [|x|] = 42
    End Sub
End Class</text>

                Dim expected = <text>Class A
    Protected x As Integer = 1
    Public Sub New()
        NewMethod()
    End Sub

    Private Sub NewMethod()
        x = 42
    End Sub
End Class</text>

                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestInvalidSelectionOfArrayLiterals()
                Dim code = <text>Class A
    Public Sub Test()
        Dim numbers = New Integer() [|{1,2,3,4}|]
    End Sub
End Class</text>

                Dim expected = <text>Class A
    Public Sub Test()
        Dim numbers = GetNumbers()
    End Sub

    Private Shared Function GetNumbers() As Integer()
        Return New Integer() {1, 2, 3, 4}
    End Function
End Class</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(540154)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub BugFix6313()
                Dim code = <text>Imports System

Class A
    Sub Test(b As Boolean)
        [|If b Then
            Return
        End If
        Console.WriteLine(1)|]
    End Sub
End Class</text>

                Dim expected = <text>Imports System

Class A
    Sub Test(b As Boolean)
        NewMethod(b)
    End Sub

    Private Shared Sub NewMethod(b As Boolean)
        If b Then
            Return
        End If
        Console.WriteLine(1)
    End Sub
End Class</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(540154)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub BugFix6313_1()
                Dim code = <text>Imports System

Class A
    Sub Test(b As Boolean)
        [|If b Then
            Return
        End If|]
        Console.WriteLine(1)
    End Sub
End Class</text>

                ExpectExtractMethodToFail(code)
            End Sub

            <WorkItem(540154)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub BugFix6313_2()
                Dim code = <text>Imports System

Class A
    Function Test(b As Boolean) as Integer
        [|If b Then
            Return 1
        End If
        Console.WriteLine(1)|]
    End Function
End Class</text>

                ExpectExtractMethodToFail(code)
            End Sub

            <WorkItem(540154)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub BugFix6313_3()
                Dim code = <text>Imports System

Class A
    Sub Test()
        [|Dim b as Boolean = True
        If b Then
            Return
        End If

        Dim d As Action = Sub()
                              If b Then
                                  Return
                              End If
                              Console.WriteLine(1)
                          End Sub|]
    End Sub
End Class</text>

                Dim expected = <text>Imports System

Class A
    Sub Test()
        NewMethod()
    End Sub

    Private Shared Sub NewMethod()
        Dim b as Boolean = True
        If b Then
            Return
        End If

        Dim d As Action = Sub()
                              If b Then
                                  Return
                              End If
                              Console.WriteLine(1)
                          End Sub
    End Sub
End Class</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(540154)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub BugFix6313_4()
                Dim code = <text>Imports System

Class A
    Sub Test()
        [|Dim d As Action = Sub()
                              Dim i As Integer = 1
                              If i > 10 Then
                                  Return
                              End If
                              Console.WriteLine(1)
                          End Sub

        Dim d2 As Action = Sub()
                               Dim i As Integer = 1
                               If i > 10 Then
                                   Return
                               End If
                               Console.WriteLine(1)
                           End Sub|]

        Console.WriteLine(1)
    End Sub
End Class</text>

                Dim expected = <text>Imports System

Class A
    Sub Test()
        NewMethod()

        Console.WriteLine(1)
    End Sub

    Private Shared Sub NewMethod()
        Dim d As Action = Sub()
                              Dim i As Integer = 1
                              If i > 10 Then
                                  Return
                              End If
                              Console.WriteLine(1)
                          End Sub

        Dim d2 As Action = Sub()
                               Dim i As Integer = 1
                               If i > 10 Then
                                   Return
                               End If
                               Console.WriteLine(1)
                           End Sub
    End Sub
End Class</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(540154)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub BugFix6313_5()
                Dim code = <text>Imports System

Class A
    Sub Test()
        Dim d As Action = Sub()
                              [|Dim i As Integer = 1
                              If i > 10 Then
                                  Return
                              End If
                              Console.WriteLine(1)|]
                          End Sub
    End Sub
End Class</text>

                Dim expected = <text>Imports System

Class A
    Sub Test()
        Dim d As Action = Sub()
                              NewMethod()
                          End Sub
    End Sub

    Private Shared Sub NewMethod()
        Dim i As Integer = 1
        If i > 10 Then
            Return
        End If
        Console.WriteLine(1)
    End Sub
End Class</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(540154), WorkItem(541484)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub BugFix6313_6()
                Dim code = <text>Imports System

Class A
    Sub Test()
        Dim d As Action = Sub()
                              [|Dim i As Integer = 1
                              If i > 10 Then
                                  Return
                              End If|]
                              Console.WriteLine(1)
                          End Sub
    End Sub
End Class</text>

                ExpectExtractMethodToFail(code)
            End Sub

            <WorkItem(543670)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub AnonymousLambdaInVarDecl()
                Dim code = <text>Imports System

Module Program
    Sub Main
       [|Dim u = Function(x As Integer) 5|]
        u.Invoke(Nothing)
    End Sub
End Module</text>

                ExpectExtractMethodToFail(code)
            End Sub

            <WpfFact, WorkItem(531451), Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestInvalidSelectionNonExecutableStatementSyntax_01()
                Dim code = <text>Module Program
    Sub Main(args As String())
        [|If True Then ElseIf True Then Return|]
    End Sub
End Module</text>

                Dim expected = <text>Module Program
    Sub Main(args As String())
        NewMethod()
    End Sub

    Private Sub NewMethod()
        If True Then ElseIf True Then Return
End Sub
End Module</text>

                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, WorkItem(547156), Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestInvalidSelectionNonExecutableStatementSyntax_02()
                Dim code = <text>Module Program
    Sub Main()
        If True Then Dim x
        [|Else Console.WriteLine()|]
    End Sub
End Module</text>

                ExpectExtractMethodToFail(code)
            End Sub

            <WpfFact, WorkItem(530625), Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestUnreachableEndInFunction()
                Dim code = <text>Module Program
    Function Foo() As Integer
        If True Then
            [|Do : Loop|] ' Extract method
            Exit Function
        Else
            Return 0
        End If
    End Function
End Module</text>

                Dim expected = <text>Module Program
    Function Foo() As Integer
        If True Then
            NewMethod() ' Extract method
            Exit Function
        Else
            Return 0
        End If
    End Function

    Private Sub NewMethod()
        Do : Loop
    End Sub
End Module</text>

                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, WorkItem(578066), Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestExitAsSupportedExitPoints()
                Dim code = <text>Imports System.Threading
Imports System.Threading.Tasks

Module Module1
    Sub Main()
    End Sub
    Async Sub test()

    End Sub
    Async Function asyncfunc(x As Integer) As Task(Of Integer)
        [|Await Task.Delay(100)
        If x = 1 Then
            Return 1
        Else
            GoTo foo
        End If
        Exit Function
foo:
        Return 2L|]
    End Function
End Module</text>

                Dim expected = <text>Imports System.Threading
Imports System.Threading.Tasks

Module Module1
    Sub Main()
    End Sub
    Async Sub test()

    End Sub
    Async Function asyncfunc(x As Integer) As Task(Of Integer)
        Return Await NewMethod(x)
    End Function

    Private Async Function NewMethod(x As Integer) As Task(Of Integer)
        Await Task.Delay(100)
        If x = 1 Then
            Return 1
        Else
            GoTo foo
        End If
        Exit Function
foo:
        Return 2L
    End Function
End Module</text>

                TestExtractMethod(code, expected)
            End Sub
        End Class
    End Class
End Namespace
