' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.Implementation.Interactive
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.ExtractMethod
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.Text.Operations
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.ExtractMethod
    Partial Public Class ExtractMethodTests
        Public Class LanguageInteraction

#Region "Generics"
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestTypeParameterWithConstraints()
                Dim code = <text>Class Program
    Private Function MyMethod1(Of TT As {ICloneable, New})() As Object
        [|Dim abcd As TT
        abcd = New TT()|]
        Return abcd
    End Function
End Class
</text>

                Dim expected = <text>Class Program
    Private Function MyMethod1(Of TT As {ICloneable, New})() As Object
        Dim abcd As TT = NewMethod(Of TT)()
        Return abcd
    End Function

    Private Shared Function NewMethod(Of TT As {ICloneable, New})() As TT
        Return New TT()
    End Function
End Class
</text>

                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestTypeParameter()
                Dim code = <text>Class Program
    Public Function Method(Of T, R)() As String
        Dim x As T
        Dim y As R
        [|x = Nothing
        y = Nothing
        Dim z As String = "hello"|]
        Return z
    End Function

End Class</text>

                Dim expected = <text>Class Program
    Public Function Method(Of T, R)() As String
        Dim x As T
        Dim y As R
        Dim z As String = Nothing
        NewMethod(x, y, z)
        Return z
    End Function

    Private Shared Sub NewMethod(Of T, R)(ByRef x As T, ByRef y As R, ByRef z As String)
        x = Nothing
        y = Nothing
        z = "hello"
    End Sub
End Class</text>

                TestExtractMethod(code, expected, allowMovingDeclaration:=False)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestTypeOfTypeParameter()
                Dim code = <text>Imports System

Class Program
    Public Shared Function meth(Of U)(a As U) As Type
        Return [|GetType(U)|]
    End Function
End Class
</text>

                Dim expected = <text>Imports System

Class Program
    Public Shared Function meth(Of U)(a As U) As Type
        Return NewMethod(Of U)()
    End Function

    Private Shared Function NewMethod(Of U)() As Type
        Return GetType(U)
    End Function
End Class
</text>

                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestTypeParameterDataFlowOut()
                Dim code = <text>Imports System.Collections.Generic
Imports System.Linq

Class Program

    Public Class Test
        Public i As Integer = 5
    End Class

    Public Function Method(Of T)() As String
        Dim a As T
        [|a = DirectCast(New Test(), T)
        a.i = 10|]
        Return a.i.ToString()
    End Function
End Class
</text>

                Dim expected = <text>Imports System.Collections.Generic
Imports System.Linq

Class Program

    Public Class Test
        Public i As Integer = 5
    End Class

    Public Function Method(Of T)() As String
        Dim a As T
        a = NewMethod(Of T)()
        Return a.i.ToString()
    End Function

    Private Shared Function NewMethod(Of T)() As T
        Dim a As T = DirectCast(New Test(), T)
        a.i = 10
        Return a
    End Function
End Class
</text>

                TestExtractMethod(code, expected)
            End Sub

            ' C# disallows this. since vbc supports "Don't Copy Back ByRef" VB extract method allows this
            ' Note that we have to expand Extract Method's selection here to avoid breaking semantics since
            ' this ByRef will not perform copy back to i after Extract Method occurs.
            ' http://blogs.msdn.com/b/jaredpar/archive/2010/01/21/the-many-cases-of-byref.aspx
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestByRefArgument()
                Dim code = <text>Class Program
    Private Shared Sub Main(args As String())
        Dim i As Integer = 2
        Dim c As New C([|i|])
    End Sub

    Private Class C
        Private v As Integer
        Public Sub New(ByRef v As Integer)
            Me.v = v
        End Sub
    End Class
End Class
</text>

                Dim expected = <text>Class Program
    Private Shared Sub Main(args As String())
        Dim i As Integer = 2
        NewMethod(i)
    End Sub

    Private Shared Sub NewMethod(i As Integer)
        Dim c As New C(i)
    End Sub

    Private Class C
        Private v As Integer
        Public Sub New(ByRef v As Integer)
            Me.v = v
        End Sub
    End Class
End Class
</text>

                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestDefaultOfT()
                Dim code = <text>Imports System.Collections.Generic
Imports System.Linq

Class Test11(Of T)
    Private Function method() As T
        Dim t As T = [|Nothing|]
        Return t
    End Function
End Class</text>

                Dim expected = <text>Imports System.Collections.Generic
Imports System.Linq

Class Test11(Of T)
    Private Function method() As T
        Dim t As T = GetT()
        Return t
    End Function

    Private Shared Function GetT() As T
        Return Nothing
    End Function
End Class</text>

                TestExtractMethod(code, expected)
            End Sub
#End Region

            <WorkItem(527791)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestExplicitLineContinuation()
                Dim code = <text>Imports System
Module Program
    Sub Main(args As String())
        Dim a As Integer = [|1 + _
            1|]
    End Sub
End Module</text>

                Dim expected = <text>Imports System
Module Program
    Sub Main(args As String())
        Dim a As Integer = GetA()
    End Sub

    Private Function GetA() As Integer
        Return 1 + _
            1
    End Function
End Module</text>

                ' Bug 5110 was a won't fix. So this test is expected to fail.
                TestExtractMethod(code, expected, temporaryFailing:=True)
            End Sub

            <WorkItem(527791)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestImplicitLineContinuation()
                Dim code = <text>Imports System
Module Program
    Sub Main(args As String())
        Dim a As Integer = [|1 +
            1|]
    End Sub
End Module</text>

                Dim expected = <text>Imports System
Module Program
    Sub Main(args As String())
        Dim a As Integer = GetA()
    End Sub

    Private Function GetA() As Integer
        Return 1 +
            1
    End Function
End Module</text>

                ' Bug 5110 was a won't fix. So this test is expected to fail.
                TestExtractMethod(code, expected, temporaryFailing:=True)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestStatementSeparator()
                Dim code = <text>Imports System
Module Program
    Sub Main(args As String())
        Dim x As Integer = 5 : [|Dim y As String = "Hello World"|]
    End Sub
End Module</text>

                Dim expected = <text>Imports System
Module Program
    Sub Main(args As String())
        Dim x As Integer = 5 : NewMethod()
    End Sub

    Private Sub NewMethod()
        Dim y As String = "Hello World"
    End Sub
End Module</text>

                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestMeKeyword()
                Dim code = <text>Public Class Class1
    Sub MySub()
        Dim x As New Class2
        [|x.Method2(Me)|]
    End Sub
End Class

Public Class Class2
    Public Sub Method2(x As Class1)

    End Sub
End Class</text>

                Dim expected = <text>Public Class Class1
    Sub MySub()
        Dim x As New Class2
        NewMethod(x)
    End Sub

    Private Sub NewMethod(x As Class2)
        x.Method2(Me)
    End Sub
End Class

Public Class Class2
    Public Sub Method2(x As Class1)

    End Sub
End Class</text>

                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestMeKeywordWithByRef()
                Dim code = <text>Public Class Class1
    Dim x As Integer

    Sub MySub(ByRef x As Integer)
        [|x = Me.x|]
    End Sub
End Class</text>

                Dim expected = <text>Public Class Class1
    Dim x As Integer

    Sub MySub(ByRef x As Integer)
        x = NewMethod()
    End Sub

    Private Function NewMethod() As Integer
        Return Me.x
    End Function
End Class</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(5168, "DevDiv_Projects/Roslyn"), WorkItem(542878)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestStatementWithMyClassKeyword()
                Dim code = <text>Public Class Class1
    Dim x As Integer

    Sub MySub(ByRef x As Integer)
        [|MyClass.x = x|]
    End Sub
End Class</text>

                Dim expected = <text>Public Class Class1
    Dim x As Integer

    Sub MySub(ByRef x As Integer)
        NewMethod(x)
    End Sub

    Private Sub NewMethod(x As Integer)
        MyClass.x = x
    End Sub
End Class</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(5171, "DevDiv_Projects/Roslyn"), WorkItem(542878)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestMyClassKeyword()
                Dim code = <text>Public Class Class1
    Dim x As Integer

    Sub MySub(ByRef x As Integer)
        [|MyClass.x|] = x
    End Sub
End Class</text>

                Dim expected = <text>Public Class Class1
    Dim x As Integer

    Sub MySub(ByRef x As Integer)
        NewMethod(x)
    End Sub

    Private Sub NewMethod(x As Integer)
        MyClass.x = x
    End Sub
End Class</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(5173, "DevDiv_Projects/Roslyn")>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestMyBaseKeyword()
                Dim code = <text>MustInherit Class A
    Property X As Integer
End Class
 
Class B
    Inherits A
    Public Sub F()
        Dim a As Integer = [|MyBase.X|]
        a = a + 1
    End Sub
End Class</text>

                Dim expected = <text>MustInherit Class A
    Property X As Integer
End Class
 
Class B
    Inherits A
    Public Sub F()
        Dim a As Integer = GetA()
        a = a + 1
    End Sub

    Private Function GetA() As Integer
        Return MyBase.X
    End Function
End Class</text>

                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestConstructorWithArgs()
                Dim code = <text>Class A
    Protected x As Integer = 1
    Public Sub New()
        x = 42
    End Sub
    Public Sub New(x As Integer)
        [|Me.x = x|]
    End Sub
End Class</text>

                Dim expected = <text>Class A
    Protected x As Integer = 1
    Public Sub New()
        x = 42
    End Sub
    Public Sub New(x As Integer)
        NewMethod(x)
    End Sub

    Private Sub NewMethod(x As Integer)
        Me.x = x
    End Sub
End Class</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(5170, "DevDiv_Projects/Roslyn")>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestStaticLocalVariable()
                Dim code = <text>Public Class Class1
    Function MySub(ByVal sales As Decimal) As Decimal
        [|Static totalSales As Decimal = 0|]
        totalSales = totalSales + sales
        Return totalSales
    End Function
End Class</text>

                ExpectExtractMethodToFail(code)
            End Sub

            <WorkItem(5170, "DevDiv_Projects/Roslyn")>
            <WorkItem(530808)>
            <WpfFact(), Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestStaticLocalVariable2()
                Dim code = <text>Public Class Class1
    Function MySub(ByVal sales As Decimal) As Decimal
        [|Static totalSales As Decimal = 0
        totalSales = totalSales + sales|]
    End Function
End Class</text>

                Dim expected = <text>Public Class Class1
    Function MySub(ByVal sales As Decimal) As Decimal
        Dim totalSales As Decimal = NewMethod(sales)
    End Function

    Private Shared Function NewMethod(sales As Decimal) As Decimal
        Static totalSales As Decimal = 0
        totalSales = totalSales + sales
        Return totalSales
    End Function
End Class</text>

                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestTypeCharacter1()
                Dim code = <text>Class A
    Public Function Foo(ByVal params&amp;)
        Foo = [|params&amp;|]
    End Function
End Class
</text>

                Dim expected = <text>Class A
    Public Function Foo(ByVal params&amp;)
        Foo = GetParams(params)
    End Function

    Private Shared Function GetParams(params As Long) As Long
        Return params&amp;
    End Function
End Class
</text>

                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestAddressOf()
                Dim code = <text>Delegate Sub SimpleDelegate()
Module Test
    Sub F()
        System.Console.WriteLine("Test.F")
    End Sub

    Sub Main()
        [|Dim d As SimpleDelegate = AddressOf F|]
        d()
    End Sub
End Module</text>

                Dim expected = <text>Delegate Sub SimpleDelegate()
Module Test
    Sub F()
        System.Console.WriteLine("Test.F")
    End Sub

    Sub Main()
        Dim d As SimpleDelegate = NewMethod()
        d()
    End Sub

    Private Function NewMethod() As SimpleDelegate
        Return AddressOf F
    End Function
End Module</text>

                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestAddressOf1()
                Dim code = <text>Delegate Sub SimpleDelegate()
Module Test
    Sub F()
        System.Console.WriteLine("Test.F")
    End Sub

    Sub Main()
        Dim d As SimpleDelegate = [|AddressOf F|]
        d()
    End Sub
End Module</text>

                Dim expected = <text>Delegate Sub SimpleDelegate()
Module Test
    Sub F()
        System.Console.WriteLine("Test.F")
    End Sub

    Sub Main()
        Dim d As SimpleDelegate = GetD()
        d()
    End Sub

    Private Function GetD() As SimpleDelegate
        Return AddressOf F
    End Function
End Module</text>

                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestArrayLiterals()
                Dim code = <text>Class Class1
    Sub Test()
        Dim numbers = New Integer() {1, 3, [|4|]}
    End Sub
End Class</text>

                Dim expected = <text>Class Class1
    Sub Test()
        Dim numbers = New Integer() {1, 3, NewMethod()}
    End Sub

    Private Shared Function NewMethod() As Integer
        Return 4
    End Function
End Class</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(539282)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestXmlLiteral1()
                Dim code = <text>Public Class Class1
    Sub MySub()
        [|Dim book As System.Xml.Linq.XElement = &lt;book title="my"&gt;&lt;/book&gt;|]
    End Sub
End Class</text>

                Dim expected = <text>Public Class Class1
    Sub MySub()
        NewMethod()
    End Sub

    Private Shared Sub NewMethod()
        Dim book As System.Xml.Linq.XElement = &lt;book title="my"&gt;&lt;/book&gt;
    End Sub
End Class</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(5176, "DevDiv_Projects/Roslyn")>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestXmlLiteral2()
                Dim code = <text>Public Class Class1
    Sub MySub()
        Dim book As System.Xml.Linq.XElement = [|&lt;book title="my"&gt;&lt;/book&gt;|]
    End Sub
End Class</text>

                Dim expected = <text>Public Class Class1
    Sub MySub()
        Dim book As System.Xml.Linq.XElement = GetBook()
    End Sub

    Private Shared Function GetBook() As System.Xml.Linq.XElement
        Return &lt;book title="my"&gt;&lt;/book&gt;
    End Function
End Class</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(5179, "DevDiv_Projects/Roslyn")>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestUnboundMethodCall()
                Dim code = <text>Public Class Class1
    Sub MySub()
        Dim TestString As String = "Test"
        [|Dim FirstWord As String = Foo(TestString, 1)|]
    End Sub
End Class</text>

                Dim expected = <text>Public Class Class1
    Sub MySub()
        Dim TestString As String = "Test"
        NewMethod(TestString)
    End Sub

    Private Shared Sub NewMethod(TestString As String)
        Dim FirstWord As String = Foo(TestString, 1)
    End Sub
End Class</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(5180, "DevDiv_Projects/Roslyn")>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestWithEvents()
                Dim code = <text>Class Raiser
    Public Event E1()
    Public Sub Raise()
        RaiseEvent E1
    End Sub
End Class
Module Test
    [|Private WithEvents x As Raiser|]
    Private Sub E1Handler() Handles x.E1
        Console.WriteLine("Raised")
    End Sub
    Public Sub Main()
        x = New Raiser()
    End Sub
End Module</text>

                ExpectExtractMethodToFail(code)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestEvents()
                Dim code = <text>Class Raiser
    [|Public Event E1()|]
    Public Sub Raise()
        RaiseEvent E1
    End Sub
End Class
Module Test
    Private WithEvents x As Raiser
    Private Sub E1Handler() Handles x.E1
        Console.WriteLine("Raised")
    End Sub
    Public Sub Main()
        x = New Raiser()
    End Sub
End Module</text>

                ExpectExtractMethodToFail(code)
            End Sub

            <WorkItem(539286)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestFieldInitializers()
                Dim code = <text>Class Class1
    Public y As Integer = [|10|]
End Class</text>

                Dim expected = <text>Class Class1
    Public y As Integer = GetY()

    Private Shared Function GetY() As Integer
        Return 10
    End Function
End Class</text>

                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestWithBlockBody()
                Dim code = <text>Public Class Class1
    Function MySub() As Integer
        ' In declaration
        Dim a1 As New Class2 With {.X = 1, .Y = "abcd"}
        Dim a As New Class2
        With a
            [|.X = 1|]
            .Y = "test"
        End With

        Return 1
    End Function
End Class

Class Class2
    Public X As Integer
    Public Y As String
End Class</text>

                Dim expected = <text>Public Class Class1
    Function MySub() As Integer
        ' In declaration
        Dim a1 As New Class2 With {.X = 1, .Y = "abcd"}
        Dim a As New Class2
        NewMethod(a)

        Return 1
    End Function

    Private Shared Sub NewMethod(a As Class2)
        With a
            .X = 1
            .Y = "test"
        End With
    End Sub
End Class

Class Class2
    Public X As Integer
    Public Y As String
End Class</text>

                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestExceptionFilter()
                Dim code = <text>Imports System
Public Class Class1
    Function MySub() As Integer
        Dim x As Integer
        Try
            x = 1
        Catch ex As Exception When [|ex.Message.Length > 0|]

        End Try

        Return 1
    End Function
End Class</text>

                Dim expected = <text>Imports System
Public Class Class1
    Function MySub() As Integer
        Dim x As Integer
        Try
            x = 1
        Catch ex As Exception When NewMethod(ex)

        End Try

        Return 1
    End Function

    Private Shared Function NewMethod(ex As Exception) As Boolean
        Return ex.Message.Length > 0
    End Function
End Class</text>

                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestStatementsInTryBlock()
                Dim code = <text>Imports System

Module Program
    Sub Main(args As String())
        Try
            Dim x As Integer = 5
            [|Throw New Exception("throwing " + x)|]
        Catch ex As Exception
            Console.Write("Caught: " + ex.Message)
        Finally
            Console.Write("Finally!")
        End Try
    End Sub

End Module</text>

                Dim expected = <text>Imports System

Module Program
    Sub Main(args As String())
        Try
            Dim x As Integer = 5
            NewMethod(x)
            Return
        Catch ex As Exception
            Console.Write("Caught: " + ex.Message)
        Finally
            Console.Write("Finally!")
        End Try
    End Sub

    Private Sub NewMethod(x As Integer)
        Throw New Exception("throwing " + x)
    End Sub
End Module</text>

                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestStatementsInCatchBlock()
                Dim code = <text>Imports System

Module Program
    Sub Main(args As String())
        Try
            Dim x As Integer = 5
            Throw New Exception("throwing " + x)
        Catch ex As Exception
            [|Console.Write("Caught: " + ex.Message)|]
        Finally
            Console.Write("Finally!")
        End Try
    End Sub

End Module</text>

                Dim expected = <text>Imports System

Module Program
    Sub Main(args As String())
        Try
            Dim x As Integer = 5
            Throw New Exception("throwing " + x)
        Catch ex As Exception
            NewMethod(ex)
        Finally
            Console.Write("Finally!")
        End Try
    End Sub

    Private Sub NewMethod(ex As Exception)
        Console.Write("Caught: " + ex.Message)
    End Sub
End Module</text>

                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestStatementsInFinallyBlock()
                Dim code = <text>Imports System

Module Program
    Sub Main(args As String())
        Try
            Dim x As Integer = 5
            Throw New Exception("throwing " + x)
        Catch ex As Exception
            Console.Write("Caught: " + ex.Message)
        Finally
            [|Console.Write("Finally!")|]
        End Try
    End Sub

End Module</text>

                Dim expected = <text>Imports System

Module Program
    Sub Main(args As String())
        Try
            Dim x As Integer = 5
            Throw New Exception("throwing " + x)
        Catch ex As Exception
            Console.Write("Caught: " + ex.Message)
        Finally
            NewMethod()
        End Try
    End Sub

    Private Sub NewMethod()
        Console.Write("Finally!")
    End Sub
End Module</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(539292)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestCallStatement()
                Dim code = <text>Public Class Class1
    Function MySub() As Integer
        Call [|MySub2()|]
        Return 1
    End Function

    Sub MySub2()

    End Sub
End Class</text>

                Dim expected = <text>Public Class Class1
    Function MySub() As Integer
        NewMethod()
        Return 1
    End Function

    Private Sub NewMethod()
        Call MySub2()
    End Sub

    Sub MySub2()

    End Sub
End Class</text>

                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestSignatureDoesContainShared()
                Dim code = <text>Class Test
    Shared x As Integer = 5
    Sub Test()
        [|Console.Write(x)|]
    End Sub
End Class</text>

                Dim expected = <text>Class Test
    Shared x As Integer = 5
    Sub Test()
        NewMethod()
    End Sub

    Private Shared Sub NewMethod()
        Console.Write(x)
    End Sub
End Class</text>

                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestSignatureDoesContainShared2()
                Dim code = <text>Class Test
    Sub Test()
        [|Console.Write(42)|]
    End Sub
End Class</text>

                Dim expected = <text>Class Test
    Sub Test()
        NewMethod()
    End Sub

    Private Shared Sub NewMethod()
        Console.Write(42)
    End Sub
End Class</text>

                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestSignatureDoesNotContainShared()
                Dim code = <text>Class Test
    Private x As Integer = 5
    Sub Test()
        [|Console.Write(x)|]
    End Sub
End Class</text>

                Dim expected = <text>Class Test
    Private x As Integer = 5
    Sub Test()
        NewMethod()
    End Sub

    Private Sub NewMethod()
        Console.Write(x)
    End Sub
End Class</text>

                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestSignatureAccessModifierIsNotPublic()
                Dim code = <text>Public Class Test
    Public x As Integer = 5
    Public Sub Test()
        [|Console.Write(x)|]
    End Sub
End Class</text>

                Dim expected = <text>Public Class Test
    Public x As Integer = 5
    Public Sub Test()
        NewMethod()
    End Sub

    Public Sub NewMethod()
        Console.Write(x)
    End Sub
End Class</text>

                TestExtractMethod(code, expected, temporaryFailing:=True)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestSignatureAccessModifierIsNotProtected()
                Dim code = <text>Protected Class Test
    Protected x As Integer = 5
    Protected Sub Test()
        [|Console.Write(x)|]
    End Sub
End Class</text>

                Dim expected = <text>Protected Class Test
    Protected x As Integer = 5
    Protected Sub Test()
        NewMethod()
    End Sub

    Protected Sub NewMethod()
        Console.Write(x)
    End Sub
End Class</text>

                TestExtractMethod(code, expected, temporaryFailing:=True)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestSignatureAccessModifierIsNotFriend()
                Dim code = <text>Friend Class Test
    Friend x As Integer = 5
    Friend Sub Test()
        [|Console.Write(x)|]
    End Sub
End Class</text>

                Dim expected = <text>Friend Class Test
    Friend x As Integer = 5
    Friend Sub Test()
        NewMethod()
    End Sub

    Friend Sub NewMethod()
        Console.Write(x)
    End Sub
End Class</text>

                TestExtractMethod(code, expected, temporaryFailing:=True)
            End Sub

            <WorkItem(539413)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub BugFix5370()
                Dim code = <text>Class Test
    Sub Main(args As String())
        Dim i As Integer
        i = 0
        Do
            i = i + 1
            If i = 3 Then
                Exit Do
            End If
        Loop While [|i &lt;= 5|]
    End Sub
End Class</text>

                Dim expected = <text>Class Test
    Sub Main(args As String())
        Dim i As Integer
        i = 0
        Do
            i = i + 1
            If i = 3 Then
                Exit Do
            End If
        Loop While NewMethod(i)
    End Sub

    Private Shared Function NewMethod(i As integer) As Boolean
        Return i &lt;= 5
    End Function
End Class</text>

                TestExtractMethod(code, expected, temporaryFailing:=True)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestEscapedIdentifiers()
                Dim code = <text>Module Program
    Class C
        Private Sub Main()
            Dim [Single] As Single = 1.2F
            [|[Single] = 1.44F|]
        End Sub
    End Class
End Module</text>

                Dim expected = <text>Module Program
    Class C
        Private Sub Main()
            Dim [Single] As Single = 1.2F
            [Single] = NewMethod()
        End Sub

        Private Function NewMethod() As Single
            Return 1.44F
        End Function
    End Class
End Module</text>

                TestExtractMethod(code, expected, allowMovingDeclaration:=False)
            End Sub

            <WorkItem(6626, "DevDiv_Projects/Roslyn")>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestSectionBeforeUnreachableCode()
                Dim code = <text>Module Program
    Class C
        Private Sub Main()
            [|Dim x As Integer
            x = 1|]
            Return
            Dim y As Integer = x
        End Sub
    End Class
End Module</text>

                Dim expected = <text>Module Program
    Class C
        Private Sub Main()
            Dim x As Integer = NewMethod()
            Return
            Dim y As Integer = x
        End Sub

        Private Function NewMethod() As Integer
            Return 1
        End Function
    End Class
End Module</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(540394)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestForLoopBody()
                Dim code = <text>Module Program
    Class C
        Private Sub Main()
            Dim a(3) As Integer
            Dim i As Integer

            For i = 0 To 3
                [|a(i) = i + 1|]
            Next 
        End Sub
    End Class
End Module</text>

                Dim expected = <text>Module Program
    Class C
        Private Sub Main()
            Dim a(3) As Integer
            Dim i As Integer

            For i = 0 To 3
                NewMethod(a, i)
            Next
        End Sub

        Private Sub NewMethod(a() As Integer, i As Integer)
            a(i) = i + 1
        End Sub
    End Class
End Module</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(540399)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestExpressionLambda()
                Dim code = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program

    Sub Apply(a() As Integer, funct As Func(Of Integer, Integer))
        For index As Integer = 0 To a.Length - 1
            a(index) = funct(a(index))
        Next index
    End Sub

    Sub Main()
        Dim a(3) As Integer
        Dim i As Integer
        For i = 0 To 3
            a(i) = i + 1
        Next
        Apply(a, [|Function(x As Integer) x * 2|])
    End Sub
End Module</text>

                Dim expected = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program

    Sub Apply(a() As Integer, funct As Func(Of Integer, Integer))
        For index As Integer = 0 To a.Length - 1
            a(index) = funct(a(index))
        Next index
    End Sub

    Sub Main()
        Dim a(3) As Integer
        Dim i As Integer
        For i = 0 To 3
            a(i) = i + 1
        Next
        Apply(a, NewMethod())
    End Sub

    Private Function NewMethod() As Func(Of Integer, Integer)
        Return Function(x As Integer) x * 2
    End Function
End Module</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(540411)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestExpressionLambdaParameter()
                Dim code = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program

    Sub Apply(a() As Integer, funct As Func(Of Integer, Integer))
        For index As Integer = 0 To a.Length - 1
            a(index) = funct(a(index))
        Next index
    End Sub

    Sub Main()
        Dim a(3) As Integer
        Dim i As Integer
        For i = 0 To 3
            a(i) = i + 1
        Next
        Apply(a, Function(x As Integer) [|x|] * 2)
    End Sub
End Module</text>

                Dim expected = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program

    Sub Apply(a() As Integer, funct As Func(Of Integer, Integer))
        For index As Integer = 0 To a.Length - 1
            a(index) = funct(a(index))
        Next index
    End Sub

    Sub Main()
        Dim a(3) As Integer
        Dim i As Integer
        For i = 0 To 3
            a(i) = i + 1
        Next
        Apply(a, Function(x As Integer) GetX(x) * 2)
    End Sub

    Private Function GetX(x As Integer) As Integer
        Return x
    End Function
End Module</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(540422)>
            <WorkItem(530596)>
            <WpfFact(), Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestArrayWithDecrementIndex()
                Dim code = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Class A
    Public Function method(s As String, i As Integer) As String
        Dim myvar As String() = New String(i - 1) {}
        myvar(0) = s
        [|myvar(--i) = s &amp; amp; i.ToString()|]
        Return myvar(i)
    End Function
End Class</text>

                Dim expected = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Class A
    Public Function method(s As String, i As Integer) As String
        Dim myvar As String() = New String(i - 1) {}
        myvar(0) = s
        NewMethod(s, i, myvar)
        Return myvar(i)
    End Function

    Private Shared Sub NewMethod(s As String, i As Integer, myvar() As String)
        myvar(--i) = s &amp; amp; i.ToString()
End Sub
End Class</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(540465)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestIfExpression()
                Dim code = <text>Imports System
Imports System.Collections
 
Class A
    Private Sub method()
        While True
            Dim y As Integer = 0
            If [|y|] = 0 Then
                Console.WriteLine()
            End If
            Console.WriteLine()
        End While
    End Sub
End Class</text>

                Dim expected = <text>Imports System
Imports System.Collections
 
Class A
    Private Sub method()
        While True
            Dim y As Integer = 0
            If GetY(y) = 0 Then
                Console.WriteLine()
            End If
            Console.WriteLine()
        End While
    End Sub

    Private Shared Function GetY(y As Integer) As Integer
        Return y
    End Function
End Class</text>

                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestSingleLineElseStatement()
                Dim code = <text>Imports System

Module Program
    Sub Main(args As String())
        Dim digits As Integer
        Dim myString As String
        If digits = 1 Then myString = "One" Else [|myString = "More than one"|]
    End Sub
End Module</text>

                Dim expected = <text>Imports System

Module Program
    Sub Main(args As String())
        Dim digits As Integer
        Dim myString As String
        If digits = 1 Then myString = "One" Else myString = NewMethod()
    End Sub

    Private Function NewMethod() As String
        Return "More than one"
    End Function
End Module</text>

                TestExtractMethod(code, expected, allowMovingDeclaration:=False)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestPropertySetter()
                Dim code = <text>Class Program
    Private _FirstName As String

    Property FirstName() As String
        Get
            Return _FirstName
        End Get
        Set(ByVal value As String)
            [|_FirstName = value|]
        End Set
    End Property 
End Class</text>

                Dim expected = <text>Class Program
    Private _FirstName As String

    Property FirstName() As String
        Get
            Return _FirstName
        End Get
        Set(ByVal value As String)
            NewMethod(value)
        End Set
    End Property

    Private Sub NewMethod(value As String)
        _FirstName = value
    End Sub
End Class</text>

                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestCollectionInitializer()
                Dim code = <text>Imports System.Collections.Generic
Class B
    Dim list = New List(Of String) From [|{"abc", "def", "ghi"}|]
End Class</text>

                Dim expected = <text>Imports System.Collections.Generic
Class B
    Dim list = GetList()

    Private Shared Function GetList() As List(Of String)
        Return New List(Of String) From {"abc", "def", "ghi"}
    End Function
End Class</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(540511)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub BugFix6788()
                Dim code = <text>Imports System
 
Module Program
    Sub Main(args As String())
        [|Dim obj As New C(Of T)(New T())|]
    End Sub
    Private Class T
    End Class
End Module
 
Class C(Of T)
    Sub New(i As T)
    End Sub
End Class</text>

                Dim expected = <text>Imports System
 
Module Program
    Sub Main(args As String())
        NewMethod()
    End Sub

    Private Sub NewMethod()
        Dim obj As New C(Of T)(New T())
    End Sub

    Private Class T
    End Class
End Module
 
Class C(Of T)
    Sub New(i As T)
    End Sub
End Class</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(542139)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub MinimalTypeNameGeneration()
                Dim code = <text>Module M
    Sub Main
        Dim x As New N.[Rem].A
        Dim y = [|x|]
    End Sub
End Module

Namespace N.[Rem]
    Class A
    End Class
End Namespace</text>

                Dim expected = <text>Module M
    Sub Main
        Dim x As New N.[Rem].A
        Dim y = GetY(x)
    End Sub

    Private Function GetY(x As N.[Rem].A) As N.[Rem].A
        Return x
    End Function
End Module

Namespace N.[Rem]
    Class A
    End Class
End Namespace</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(542105)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub NamedArgument()
                Dim code = <text>Module M
    Sub Main
        Test([|a|]:=1)
    End Sub

    Sub Test(a as Integer)
    End Sub
End Module</text>

                Dim expected = <text>Module M
    Sub Main
        NewMethod()
    End Sub

    Private Sub NewMethod()
        Test(a:=1)
    End Sub

    Sub Test(a as Integer)
    End Sub
End Module</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(542094)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TypeName()
                Dim code = <text>Module M
    Sub Main()
        Dim x = ([|System.String|]).Equals("", "")
    End Sub
End Module</text>

                Dim expected = <text>Module M
    Sub Main()
        Dim x = GetX()
    End Sub

    Private Function GetX() As Boolean
        Return (System.String).Equals("", "")
    End Function
End Module</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(542092)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub RangeArgument()
                Dim code = <text>Module M
    Sub Main()
        Dim x() As Integer
        ReDim x([|0|] To 5)
    End Sub
End Module</text>

                Dim expected = <text>Module M
    Sub Main()
        NewMethod()
    End Sub

    Private Sub NewMethod()
        Dim x As Integer()
        ReDim x(0 To 5)
    End Sub
End Module</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(542026), WorkItem(543100)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub NextStatement()
                Dim code = <text>Module M
    Sub Main()
        Dim x(1) As Char
        For Each x(0) In ""
        Next x([|0|])
    End Sub
End Module</text>

                Dim expected = <text>Module M
    Sub Main()
        Dim x(1) As Char
        NewMethod(x)
    End Sub

    Private Sub NewMethod(x() As Char)
        For Each x(0) In ""
        Next x(0)
    End Sub
End Module</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(542030)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub NextStatementWithMultipleControlVariables()
                Dim code = <text>Module M
    Sub Main
        For Each x As Char In ""
            [|For Each y As Char In ""
            Next y|], x
    End Sub
End Module</text>

                Dim expected = <text>Module M
    Sub Main
        NewMethod()
    End Sub

    Private Sub NewMethod()
        For Each x As Char In ""
            For Each y As Char In ""
        Next y, x
    End Sub
End Module</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(542067)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub SelectQueryOperator1()
                Dim code = <text>Imports System.Linq
Module Program
    Sub Main()
        Dim q = From x In "ABC" Select [|x|] Select x
    End Sub
End Module</text>

                Dim expected = <text>Imports System.Linq
Module Program
    Sub Main()
        Dim q = GetQ()
    End Sub

    Private Function GetQ() As System.Collections.Generic.IEnumerable(Of Char)
        Return From x In "ABC" Select x Select x
    End Function
End Module</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(542067)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub SelectQueryOperator2()
                Dim code = <text>Imports System.Linq
Module Program
    Sub Main()
        Dim q = From x In "ABC" Select x = [|x|] Select x
    End Sub
End Module</text>

                Dim expected = <text>Imports System.Linq
Module Program
    Sub Main()
        Dim q = From x In "ABC" Select x = GetX(x) Select x
    End Sub

    Private Function GetX(x As Char) As Char
        Return x
    End Function
End Module</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(542067)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub SelectQueryOperator3()
                Dim code = <text>Imports System.Linq
Module Program
    Sub Main()
        Dim q = From x In "ABC" Select [|x|] = x Select x
    End Sub
End Module</text>

                Dim expected = <text>Imports System.Linq
Module Program
    Sub Main()
        Dim q = GetQ()
    End Sub

    Private Function GetQ() As System.Collections.Generic.IEnumerable(Of Char)
        Return From x In "ABC" Select x = x Select x
    End Function
End Module</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(542067)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub SelectQueryOperator4()
                Dim code = <text>Imports System.Linq
Module Program
    Sub Main()
        Dim q = From x In "ABC" Select x Select [|x|]
    End Sub
End Module</text>

                Dim expected = <text>Imports System.Linq
Module Program
    Sub Main()
        Dim q = From x In "ABC" Select x Select GetX(x)
    End Sub

    Private Function GetX(x As Char) As Char
        Return x
    End Function
End Module</text>

                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestSyncLockBlock()
                Dim code = <text>Imports System

Class simpleMessageList
    Public messagesList() As String = New String(50) {}
    Public messagesLast As Integer = -1
    Private messagesLock As New Object
    Public Sub addAnotherMessage(ByVal newMessage As String)
        SyncLock [|messagesLock|]
            messagesLast = messagesLast + 1
            If messagesLast &lt; messagesList.Length Then
                messagesList(messagesLast) = newMessage
            End If
        End SyncLock
    End Sub
End Class</text>

                Dim expected = <text>Imports System

Class simpleMessageList
    Public messagesList() As String = New String(50) {}
    Public messagesLast As Integer = -1
    Private messagesLock As New Object
    Public Sub addAnotherMessage(ByVal newMessage As String)
        SyncLock GetMessagesLock()
            messagesLast = messagesLast + 1
            If messagesLast &lt; messagesList.Length Then
                messagesList(messagesLast) = newMessage
            End If
        End SyncLock
    End Sub

    Private Shared Function GetMessagesLock(messagesLock as Object) As Object
        Return messagesLock
    End Function
End Class</text>

                TestExtractMethod(code, expected, temporaryFailing:=True)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestUsingBlock()
                Dim code = <text>Imports System

Module Program
    Private Sub WriteFile()
        Using writer As System.IO.TextWriter = System.IO.File.CreateText("log.txt")
            [|writer.WriteLine("This is line one.")
            writer.WriteLine("This is line two.")|]
        End Using
    End Sub
End Module</text>

                Dim expected = <text>Imports System

Module Program
    Private Sub WriteFile()
        Using writer As System.IO.TextWriter = System.IO.File.CreateText("log.txt")
            NewMethod(writer)
        End Using
    End Sub

    Private Sub NewMethod(writer As IO.TextWriter)
        writer.WriteLine("This is line one.")
        writer.WriteLine("This is line two.")
    End Sub
End Module</text>

                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestWithBlockExpression()
                Dim code = <text>Module Program
    Sub Main()
        Dim t As New A()
        With [|t|]
            .Height = 100
            .Text = "Hello, World"
        End With
    End Sub
End Module
Class A
    Property Height As Integer
    Property Text As String
End Class</text>

                Dim expected = <text>Module Program
    Sub Main()
        Dim t As New A()
        NewMethod(t)
    End Sub

    Private Sub NewMethod(t As A)
        With t
            .Height = 100
            .Text = "Hello, World"
        End With
    End Sub
End Module
Class A
    Property Height As Integer
    Property Text As String
End Class</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(543017)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestCaseBlock()
                Dim code = <text>Imports System

Class Program
    Sub Test()
        Dim i As Integer = 10
        Select Case i
            Case 5
                [|Console.Write(5)|]
        End Select
    End Sub
End Class</text>

                Dim expected = <text>Imports System

Class Program
    Sub Test()
        Dim i As Integer = 10
        Select Case i
            Case 5
                NewMethod()
        End Select
    End Sub

    Private Shared Sub NewMethod()
        Console.Write(5)
    End Sub
End Class</text>

                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestStructureBlock()
                Dim code = <text>Structure A
    Shared x As Integer = [|5 * 3|]
End Structure</text>

                Dim expected = <text>Structure A
    Shared x As Integer = GetX()

    Private Shared Function GetX() As Integer
        Return 5 * 3
    End Function
End Structure</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(542804)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub AnonymousType()
                Dim code = <text>Option Infer On
Imports System
Imports System.Linq
Class BaseClass
    Sub Method()
        Dim x = New Integer() {}
        x.Where(Function(y)
                    Dim z = (From var1 In x Where y > 10 _
                             Select New With {[|.equal = var1|]}).ToList()
                    Return y = ""
                End Function)
    End Sub
End Class
Class DerivedClass
    Shared Sub Main()
    End Sub
End Class
</text>

                ExpectExtractMethodToFail(code)
            End Sub

            <WorkItem(542878)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub MyClassInstance()
                Dim code = <text>Public Class Class1
    Dim x As Integer
 
    Sub MySub(ByRef x As Integer)
        [|MyClass.x = x|]
    End Sub
End Class</text>
                Dim expected = <text>Public Class Class1
    Dim x As Integer

    Sub MySub(ByRef x As Integer)
        NewMethod(x)
    End Sub

    Private Sub NewMethod(x As Integer)
        MyClass.x = x
    End Sub
End Class</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(542904)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub GeneratedMethodBeforeAttribute()
                Dim code = <text>Module Program
    Sub Main(args As String())
        Dim x = [|1 + 1|]
    End Sub

    &lt;Obsolete&gt;
    Sub Foo
    End Sub
End Module</text>
                Dim expected = <text>Module Program
    Sub Main(args As String())
        Dim x = GetX()
    End Sub

    Private Function GetX() As Integer
        Return 1 + 1
    End Function

    &lt;Obsolete&gt;
    Sub Foo
    End Sub
End Module</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(10341, "DevDiv_Projects/Roslyn")>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TryCatchPartDontCrash()
                Dim code = <text>Module Program
    Sub Main(nwindConn As String())
        Dim nwindTxn As SqlTransaction = nwindConn.BeginTransaction()

        [|Try
            Dim cmd As SqlCommand = New SqlCommand("UPDATE Products SET QuantityPerUnit = 'single item' WHERE ProductID = 3")
            cmd.Connection = nwindConn
            cmd.Transaction = nwindTxn
            cmd.ExecuteNonQuery()
 
            interop_db.Transaction = nwindTxn
 
            Dim prod1 As Products = (From p In interop_db.Products _
                                     Where p.ProductID = 4 _
                                     Select p).First()
            Dim prod2 As Products = (From p In interop_db.Products _
                                     Where p.ProductID = 5 _
                                     Select p).First()
            prod1.UnitsInStock = New Nullable(Of Short)(prod1.UnitsInStock.Value - 3)
            prod2.UnitsInStock = New Nullable(Of Short)(prod2.UnitsInStock.Value - 5)    ' ERROR: this will make the units in stock negative
 
            interop_db.SubmitChanges()
 
            nwindTxn.Commit()|]
        Catch e As Exception
            ' If there is a transaction error, all changes are rolled back,
            ' including any changes made directly through the ADO.NET connection
            Console.WriteLine(e.Message)
            Console.WriteLine("Error submitting changes... all changes rolled back.")
        End Try
    End Sub
End Module

Class SqlTransaction
    Public Shared Function BeginTransaction() As SqlTransaction
        Return Nothing
    End Function
End Class</text>
                Dim expected = <text>Module Program
    Sub Main(nwindConn As String())
        Dim nwindTxn As SqlTransaction = nwindConn.BeginTransaction()

        NewMethod(nwindConn, nwindTxn)
    End Sub

    Private Sub NewMethod(nwindConn() As String, nwindTxn As SqlTransaction)
        Try
            Dim cmd As SqlCommand = New SqlCommand("UPDATE Products SET QuantityPerUnit = 'single item' WHERE ProductID = 3")
            cmd.Connection = nwindConn
            cmd.Transaction = nwindTxn
            cmd.ExecuteNonQuery()

            interop_db.Transaction = nwindTxn

            Dim prod1 As Products = (From p In interop_db.Products _
                                     Where p.ProductID = 4 _
                                     Select p).First()
            Dim prod2 As Products = (From p In interop_db.Products _
                                     Where p.ProductID = 5 _
                                     Select p).First()
            prod1.UnitsInStock = New Nullable(Of Short)(prod1.UnitsInStock.Value - 3)
            prod2.UnitsInStock = New Nullable(Of Short)(prod2.UnitsInStock.Value - 5)    ' ERROR: this will make the units in stock negative

            interop_db.SubmitChanges()

            nwindTxn.Commit()
        Catch e As Exception
            ' If there is a transaction error, all changes are rolled back,
            ' including any changes made directly through the ADO.NET connection
            Console.WriteLine(e.Message)
            Console.WriteLine("Error submitting changes... all changes rolled back.")
        End Try
    End Sub
End Module

Class SqlTransaction
    Public Shared Function BeginTransaction() As SqlTransaction
        Return Nothing
    End Function
End Class</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(542878)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub MyBaseInstance()
                Dim code = <text>Public Class Class1
    Dim x As Integer
 
    Sub MySub(ByRef x As Integer)
        [|MyBase.Equals(Nothing)|]
    End Sub
End Class</text>
                Dim expected = <text>Public Class Class1
    Dim x As Integer

    Sub MySub(ByRef x As Integer)
        NewMethod()
    End Sub

    Private Sub NewMethod()
        MyBase.Equals(Nothing)
    End Sub
End Class</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(542878)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub MeInstanceExpression()
                Dim code = <text>Public Class Class1
    Dim x As Integer
 
    Sub MySub(ByRef x As Integer)
        [|Me|].Equals(Nothing)
    End Sub
End Class</text>
                Dim expected = <text>Public Class Class1
    Dim x As Integer

    Sub MySub(ByRef x As Integer)
        NewMethod().Equals(Nothing)
    End Sub

    Private Function NewMethod() As Class1
        Return Me
    End Function
End Class</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(543304)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ExtractMethodForLambdaInSyncLock()
                Dim code = <text>Class Program
    Public Shared Sub Main(args As String())
        SyncLock Function(ByRef int As [|Integer|])
                 End Function
        End SyncLock
    End Sub
End Class</text>

                ExpectExtractMethodToFail(code)
            End Sub

            <WorkItem(543332)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ReturnStatement1()
                Dim code = <text>Option Infer On
Option Strict On
Imports System
Class Program
    Shared Sub Main()
        Dim myLock As Object
        [|SyncLock Sub()
                     myLock = New Object()
                     Exit Sub
                 End sub
        End SyncLock|]
        Console.WriteLine(myLock.ToString())
    End Sub
End Class</text>
                Dim expected = <text>Option Infer On
Option Strict On
Imports System
Class Program
    Shared Sub Main()
        Dim myLock As Object
        myLock = NewMethod(myLock)
        Console.WriteLine(myLock.ToString())
    End Sub

    Private Shared Function NewMethod(myLock As Object) As Object
        SyncLock Sub()
                     myLock = New Object()
                     Exit Sub
                 End sub
        End SyncLock

        Return myLock
    End Function
End Class</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(543304)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub LambdaParameter1()
                Dim code = <text>Class C1
    Shared Sub Main()
        [|Dim x As MyDelegate = Sub(ByRef y As Integer)
                              End Sub|]
    End Sub

    Delegate Sub MyDelegate(ByRef y As Integer)
End Class</text>
                Dim expected = <text>Class C1
    Shared Sub Main()
        NewMethod()
    End Sub

    Private Shared Sub NewMethod()
        Dim x As MyDelegate = Sub(ByRef y As Integer)
                              End Sub
    End Sub

    Delegate Sub MyDelegate(ByRef y As Integer)
End Class</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(543096)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub SelectBlock()
                Dim code = <text>Imports System
Module Program
    Sub Main(args As String())
        Dim x = 1
x:
        Select Case [|x|]
            Case x
                GoTo x
        End Select
    End Sub
End Module</text>

                Dim expected = <text>Imports System
Module Program
    Sub Main(args As String())
        Dim x = 1
x:
        Select Case GetX(x)
            Case x
                GoTo x
        End Select
    End Sub

    Private Function GetX(x As Integer) As Integer
        Return x
    End Function
End Module</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(529182)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub CastExpressionImplicitConversion()
                Dim code = <text>
Module Program
    Sub Main()
        Dim x3 As Integer = [|CObj(1)|]
    End Sub
End Module</text>

                Dim expected = <text>
Module Program
    Sub Main()
        Dim x3 As Integer = GetX3()
    End Sub

    Private Function GetX3() As Integer
        Return CObj(1)
    End Function
End Module</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(539310)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ReadOnlyFields_WrittenTo()
                Dim code = <text>
Class M
    Public ReadOnly x As Integer
    Sub New()
        [|x = 4|]
    End Sub
End Class</text>
                ExpectExtractMethodToFail(code)
            End Sub

            <WorkItem(539310)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ReadOnlyFields()
                Dim code = <text>
Class M
    Public ReadOnly x As Integer
    Sub New()
        x = 4
        [|Dim y = x|]
    End Sub
End Class</text>

                Dim expected = <text>
Class M
    Public ReadOnly x As Integer
    Sub New()
        x = 4
        NewMethod()
    End Sub

    Private Sub NewMethod()
        Dim y = x
    End Sub
End Class</text>
                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(544972)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub AnonymousDelegate()
                Dim code = <text>Option Infer On
 
Module M
    Sub Main()
        [|Dim x = Function() From y In "" Select y|]
        Dim a = x, b
    End Sub
End Module
</text>
                ExpectExtractMethodToFail(code)
            End Sub

            <WorkItem(544971)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub AnonymousDelegate2()
                Dim code = <text>Option Infer On
 
Module M
    Sub Main()
        Dim x = Function() From y In "" Select y
        Dim a = x[|, b|]
    End Sub
End Module
</text>
                ExpectExtractMethodToFail(code)
            End Sub

            <WorkItem(545128)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub NoValidRangeOfStatementToExtract()
                Dim code = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq
 
Module Program
    Sub Main(args As String())
        For a = 0 To 1
            For b = 0 To 1
                [|For c = 0 To 1
 
            Next b, a|]
    End Sub
End Module</text>
                ExpectExtractMethodToFail(code)
            End Sub

            <WorkItem(543581)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub NoInitializedDueToGoToLabel()
                Dim code = <text>Module Program
    Sub Main(args As String())
        Dim lambda = Function(ByRef arg As Integer)
                         Return Function(ByRef arg1 As Integer)
                                    GoTo Label
                                    Dim arg2 As Integer = 2
Label:
                                    Return [|arg2 * arg1|]
                                End Function
                     End Function
    End Sub
End Module</text>

                Dim expected = <text>Module Program
    Sub Main(args As String())
        Dim lambda = Function(ByRef arg As Integer)
                         Return Function(ByRef arg1 As Integer)
                                    GoTo Label
                                    Dim arg2 As Integer = 2
Label:
                                    Return NewMethod(arg1, arg2)
                                End Function
                     End Function
    End Sub

    Private Function NewMethod(arg1 As Integer, arg2 As Integer) As Integer
        Return arg2 * arg1
    End Function
End Module</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(545292)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub LocalConst()
                Dim code = <text>Class C
    Sub Method()
        Const i as Integer = [|1|]
    End Sub
End Class</text>

                ExpectExtractMethodToFail(code)
            End Sub

            <WorkItem(543582)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ArgumentForByRefParameter()
                Dim code = <text>Module Module1
    Sub Main(args As String())
        Dim lambda = Function(ByRef arg As Integer)
                         Return Function(ByRef arg1 As Integer)
                                    Return arg1
                                End Function([|arg|])
                     End Function

        Console.WriteLine(lambda.Invoke(2))
    End Sub
End Module</text>

                Dim expected = <text>Module Module1
    Sub Main(args As String())
        Dim lambda = Function(ByRef arg As Integer)
                         Return NewMethod(arg)
                     End Function

        Console.WriteLine(lambda.Invoke(2))
    End Sub

    Private Function NewMethod(ByRef arg As Integer) As Integer
        Return Function(ByRef arg1 As Integer)
                   Return arg1
               End Function(arg)
    End Function
End Module</text>

                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ByRefArgument1()
                Dim code = <code>Module M
    Sub Main()
        Dim i = 0
        Foo([|i|])
        System.Console.WriteLine(i)
    End Sub

    Sub Foo(ByRef i As Integer)
        i = 42
    End Sub
End Module</code>

                Dim expected = <code>Module M
    Sub Main()
        Dim i = 0
        NewMethod(i)
        System.Console.WriteLine(i)
    End Sub

    Sub NewMethod(ByRef i As Integer)
        Foo(i)
    End Sub

    Sub Foo(ByRef i As Integer)
        i = 42
    End Sub
End Module</code>

            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ByRefArgument2()
                Dim code = <code>Module M
    Sub Main()
        Dim i = 0
        Foo(([|i|]))
        System.Console.WriteLine(i)
    End Sub

    Sub Foo(ByRef i As Integer)
        i = 42
    End Sub
End Module</code>

                Dim expected = <code>Module M
    Sub Main()
        Dim i = 0
        Foo((GetI(i)))
        System.Console.WriteLine(i)
    End Sub

    Function GetI(i As Integer) As Integer
        Return i
    End Function

    Sub Foo(ByRef i As Integer)
        i = 42
    End Sub
End Module</code>

            End Sub

            <WorkItem(545153)>
            <WorkItem(530596)>
            <WpfFact(), Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub CreateDelegateFromMethod()
                Dim code = <text>Imports System
Imports System.Linq

Module Program
    Sub Main()
        Dim a As Action = AddressOf From x In "" Select x Distinct 
        [|.ToString|]
    End Sub
End Module</text>

                Dim expected = <text>Imports System
Imports System.Linq

Module Program
    Sub Main()
        Dim a As Action = GetA()
    End Sub

    Private Function GetA() As Action
        Return AddressOf From x In "" Select x Distinct
                .ToString
    End Function
End Module</text>
                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(544459)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub BangOperator()
                Dim code = <text>Class S
        Default Property Def(s As String) As String
            Get
                Return Nothing
            End Get
            Set(value As String)
            End Set
        End Property
        Property Y As String
    End Class
    Module Program
        Sub Main(args As String())
            Dim c As New S With {.Y = [|!Hello|]}
        End Sub
    End Module</text>

                Dim expected = <text>Class S
        Default Property Def(s As String) As String
            Get
                Return Nothing
            End Get
            Set(value As String)
            End Set
        End Property
        Property Y As String
    End Class
    Module Program
    Sub Main(args As String())
        NewMethod()
    End Sub

    Private Sub NewMethod()
        Dim c As New S With {.Y = !Hello}
    End Sub
End Module</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(544327)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ObjectInitializer_RValue()
                Dim code = <text>Class C
    Public X As Long = 1
    Public Y As Long = 2
    Public CC As C
End Class
Module Program
    Sub Main(args As String())
        Dim a, b As New C() With {.CC = [|b|]}
    End Sub
End Module</text>

                Dim expected = <text>Class C
    Public X As Long = 1
    Public Y As Long = 2
    Public CC As C
End Class
Module Program
    Sub Main(args As String())
        Dim a, b As New C() With {.CC = GetB(b)}
    End Sub

    Private Function GetB(b As C) As C
        Return b
    End Function
End Module</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(545169)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub XmlEmbeddedExpression()
                Dim code = <text>Module M
    Sub Main()
        Dim x = &lt;x &lt;%= [|123|] %&gt;/&gt; ' Extract Method from 123
    End Sub
End Module</text>

                Dim expected = <text>Module M
    Sub Main()
        Dim x = &lt;x &lt;%= NewMethod() %&gt;/&gt; ' Extract Method from 123
    End Sub

    Private Function NewMethod() As Integer
        Return 123
    End Function
End Module</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(544597)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub DefaultValueForAutoImplementedProperty()
                Dim code = <text>Class B
    Property IntList() As New List(Of Integer) With {.Capacity = [|100|]}
End Class </text>

                Dim expected = <text>Class B
    Property IntList() As New List(Of Integer) With {.Capacity = NewMethod()}

    Private Shared Function NewMethod() As Integer
        Return 100
    End Function
End Class </text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(545546)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ExpressionInWithBlock()
                Dim code = <text>Module Program
    Sub Main()
        With ""
            Dim y = [|1 + 2|] ' Extract method
        End With
    End Sub
End Module</text>

                Dim expected = <text>Module Program
    Sub Main()
        With ""
            Dim y = GetY() ' Extract method
        End With
    End Sub

    Private Function GetY() As Integer
        Return 1 + 2
    End Function
End Module</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(545635), WorkItem(718154)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub RangeArgument_Field()
                Dim code = <text>Module Program
    ' Extract method
    Dim x(0 To [|1 + 2|])
End Module
</text>

                Dim expected = <text>Module Program
    ' Extract method
    Dim x(0 To NewMethod())

    Private Function NewMethod() As Integer
        Return 1 + 2
    End Function
End Module
</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(545628)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub NoEmptyTokenAtEndOfSelection()
                Dim code = <text>Module Program
    Dim x = &lt;x&gt;&lt;%= Sub() [|If True Then Return :|]%&gt;&lt;/x&gt;
End Module</text>

                Dim expected = <text>Module Program
    Dim x = &lt;x&gt;&lt;%= Sub() NewMethod() %&gt;&lt;/x&gt;

    Private Sub NewMethod()
        If True Then Return : 
    End Sub
End Module</text>

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(545628)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub NoEmptyTokenAtEndOfSelection2()
                Dim code = <text>Module Program
    Dim x = &lt;x&gt;&lt;%= [|Sub() If True Then Return :|]%&gt;&lt;/x&gt;
End Module</text>

                ExpectExtractMethodToFail(code)
            End Sub

            <WorkItem(545593)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TypeParameterInReturnType()
                Dim code = <text>Imports System
Imports System.Linq
Module Program
    Function GetExprType(Of T)(ByVal inst As T) As System.Linq.Expressions.Expression(Of Func(Of T))
        Return [|Nothing|]
    End Function
End Module</text>

                Dim expected = <text>Imports System
Imports System.Linq
Module Program
    Function GetExprType(Of T)(ByVal inst As T) As System.Linq.Expressions.Expression(Of Func(Of T))
        Return NewMethod(Of T)()
    End Function

    Private Function NewMethod(Of T)() As Expressions.Expression(Of Func(Of T))
        Return Nothing
    End Function
End Module</text>
                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(544663)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub MadePropertyWithParameterNotValidLValue()
                Dim code = <text>Friend Module Module1
    Class c1
        Sub foo(ByRef x1 As Integer, ByRef x2 As Integer)
        End Sub
    End Class
    Public Property prop(ByVal x As Integer) As Integer
        Get
            Return 0
        End Get
        Set(ByVal value As Integer)
        End Set
    End Property
    Sub Main()
        Dim c As New c1
        c.foo(prop(1), [|prop(2)|])
    End Sub
End Module</text>

                Dim expected = <text>Friend Module Module1
    Class c1
        Sub foo(ByRef x1 As Integer, ByRef x2 As Integer)
        End Sub
    End Class
    Public Property prop(ByVal x As Integer) As Integer
        Get
            Return 0
        End Get
        Set(ByVal value As Integer)
        End Set
    End Property
    Sub Main()
        Dim c As New c1
        NewMethod(c)
    End Sub

    Private Sub NewMethod(c As c1)
        c.foo(prop(1), prop(2))
    End Sub
End Module</text>
                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(530322)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ExtractMethodShouldNotBreakFormatting()
                Dim code =
<text>
Class C
    Sub M(i As Integer, j As Integer, j As Integer)
        M(0,
          [|1|],
          2)
    End Sub
End Class
</text>

                Dim expected =
<text>
Class C
    Sub M(i As Integer, j As Integer, j As Integer)
        M(0,
          NewMethod(),
          2)
    End Sub

    Private Shared Function NewMethod() As Integer
        Return 1
    End Function
End Class
</text>

                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub AwaitExpression_Normal_SingleStatement()
                Dim code =
<text>
Imports System
Imports System.Threading.Tasks

Class X
    Public Async Sub Test()
        [|Await Task.Run(Sub()
                       End Sub)|]
    End Sub
End Class
</text>

                Dim expected =
<text>
Imports System
Imports System.Threading.Tasks

Class X
    Public Async Sub Test()
        Await NewMethod()
    End Sub

    Private Shared Async Function NewMethod() As Task
        Await Task.Run(Sub()
                       End Sub)
    End Function
End Class
</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub AwaitExpression_Normal_MultipleStatements()
                Dim code =
<text>
Imports System
Imports System.Threading.Tasks

Class X
    Public Async Sub Test()
        [|Await Task.Run(Sub()
                       End Sub)

        Await Task.Run(Function() 1)

        Return|]
    End Sub
End Class
</text>

                Dim expected =
<text>
Imports System
Imports System.Threading.Tasks

Class X
    Public Async Sub Test()
        Await NewMethod()
    End Sub

    Private Shared Async Function NewMethod() As Task
        Await Task.Run(Sub()
                       End Sub)

        Await Task.Run(Function() 1)

        Return
    End Function
End Class
</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub AwaitExpression_Normal_ExpressionWithReturn()
                Dim code =
<text>
Imports System
Imports System.Threading.Tasks

Class X
    Public Async Sub Test()
        Await Task.Run(Sub()
                       End Sub)

        [|Await Task.Run(Function() 1)|]

        Return
    End Sub
End Class
</text>

                Dim expected =
<text>
Imports System
Imports System.Threading.Tasks

Class X
    Public Async Sub Test()
        Await Task.Run(Sub()
                       End Sub)

        Await NewMethod()

        Return
    End Sub

    Private Shared Async Function NewMethod() As Task
        Await Task.Run(Function() 1)
    End Function
End Class
</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub AwaitExpression_Normal_ExpressionInAwaitExpression()
                Dim code =
<text>
Imports System
Imports System.Threading.Tasks

Class X
    Public Async Sub Test()
        Await [|Task.Run(Function() 1)|]
    End Sub
End Class
</text>

                Dim expected =
<text>
Imports System
Imports System.Threading.Tasks

Class X
    Public Async Sub Test()
        Await NewMethod()
    End Sub

    Private Shared Function NewMethod() As Task(Of Integer)
        Return Task.Run(Function() 1)
    End Function
End Class
</text>
                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(718152)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub AwaitExpression_Normal_AwaitWithReturnParameter()
                Dim code =
<text>
Imports System
Imports System.Threading.Tasks

Class X
    Public Async Sub Test(i As Integer)
        [|Await Task.Run(Function() i)
        i = 10|]

        Console.WriteLine(i)
    End Sub
End Class
</text>

                Dim expected =
<text>
Imports System
Imports System.Threading.Tasks

Class X
    Public Async Sub Test(i As Integer)
        i = Await NewMethod(i)

        Console.WriteLine(i)
    End Sub

    Private Shared Async Function NewMethod(i As Integer) As Task(Of Integer)
        Await Task.Run(Function() i)
        i = 10
        Return i
    End Function
End Class
</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub AwaitExpression_Normal_AwaitWithReturnParameter_Error()
                Dim code =
<text>
Imports System
Imports System.Threading.Tasks

Class X
    Public Async Sub Test(i As Integer)
        [|Dim i2 = Await Task.Run(Function() i)
        i = 10|]

        Console.WriteLine(i + i2)
    End Sub
End Class
</text>
                ExpectExtractMethodToFail(code)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub AwaitExpression_AsyncLambda()
                Dim code =
<text>
Imports System
Imports System.Threading.Tasks

Class X
    Public Sub Test(a As Func(Of Task(Of Integer)))
        Test([|Async Function() Await Task.Run(Function() 1)|])
    End Sub
End Class
</text>

                Dim expected =
<text>
Imports System
Imports System.Threading.Tasks

Class X
    Public Sub Test(a As Func(Of Task(Of Integer)))
        Test(NewMethod())
    End Sub

    Private Shared Function NewMethod() As Func(Of Task(Of Integer))
        Return Async Function() Await Task.Run(Function() 1)
    End Function
End Class
</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub AwaitExpression_AsyncLambda_Body()
                Dim code =
<text>
Imports System
Imports System.Threading.Tasks

Class X
    Public Sub Test(a As Func(Of Task(Of Integer)))
        Test(Async Function() [|Await Task.Run(Function() 1)|])
    End Sub
End Class
</text>

                Dim expected =
<text>
Imports System
Imports System.Threading.Tasks

Class X
    Public Sub Test(a As Func(Of Task(Of Integer)))
        Test(Async Function() Await NewMethod())
    End Sub

    Private Shared Async Function NewMethod() As Task(Of Integer)
        Return Await Task.Run(Function() 1)
    End Function
End Class
</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub AwaitExpression_AsyncLambda_WholeExpression()
                Dim code =
<text>
Imports System
Imports System.Threading.Tasks

Class X
    Public Sub Test(a As Func(Of Task(Of Integer)))
        [|Test(Async Function() Await Task.Run(Function() 1)|])
    End Sub
End Class
</text>

                Dim expected =
<text>
Imports System
Imports System.Threading.Tasks

Class X
    Public Sub Test(a As Func(Of Task(Of Integer)))
        NewMethod()
    End Sub

    Private Sub NewMethod()
        Test(Async Function() Await Task.Run(Function() 1))
    End Sub
End Class
</text>
                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(530812)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestOverloadResolution()
                Dim code =
<text>
Imports System

Module M
    Sub Main()
        Foo(Sub(comment) [|Console.WriteLine(comment$)|], Nothing) ' Extract method
    End Sub
    Sub Foo(a As Action(Of String), b As Object)
        Console.WriteLine(1)
    End Sub
    Sub Foo(a As Action(Of Integer), b As String)
        Console.WriteLine(2)
    End Sub
End Module
</text>

                Dim expected =
<text>
Imports System

Module M
    Sub Main()
        Foo(Sub(comment) NewMethod(comment), CObj(Nothing)) ' Extract method
    End Sub

    Private Sub NewMethod(comment As String)
        Console.WriteLine(comment$)
    End Sub

    Sub Foo(a As Action(Of String), b As Object)
        Console.WriteLine(1)
    End Sub
    Sub Foo(a As Action(Of Integer), b As String)
        Console.WriteLine(2)
    End Sub
End Module
</text>
                TestExtractMethod(code, expected)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestDontPutOutOrRefOnStructOff()
                Dim code =
<text>
Imports System.Threading.Tasks

Namespace ClassLibrary9
    Public Structure S
        Public I As Integer
    End Structure

    Public Class Class1
        Public Async Function Test() As Task(Of Integer)
            Dim s = New S()
            s.I = 10

            [|Dim i = Await Task.Run(Function()
                                       Dim i2 = s.I
                                       Return Test()
                                   End Function)|]

            Return i
        End Function
    End Class
End Namespace
</text>

                ExpectExtractMethodToFail(code, dontPutOutOrRefOnStruct:=False)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestDontPutOutOrRefOnStructOn()
                Dim code =
<text>
Imports System.Threading.Tasks

Namespace ClassLibrary9
    Public Structure S
        Public I As Integer
    End Structure

    Public Class Class1
        Public Async Function Test() As Task(Of Integer)
            Dim s = New S()
            s.I = 10

            [|Dim i = Await Task.Run(Function()
                                       Dim i2 = s.I
                                       Return Test()
                                   End Function)|]

            Return i
        End Function
    End Class
End Namespace
</text>

                Dim expected =
<text>
Imports System.Threading.Tasks

Namespace ClassLibrary9
    Public Structure S
        Public I As Integer
    End Structure

    Public Class Class1
        Public Async Function Test() As Task(Of Integer)
            Dim s = New S()
            s.I = 10

            Dim i As Integer = Await NewMethod(s)

            Return i
        End Function

        Private Async Function NewMethod(s As S) As Task(Of Integer)
            Return Await Task.Run(Function()
                                      Dim i2 = s.I
                                      Return Test()
                                  End Function)
        End Function
    End Class
End Namespace
</text>
                TestExtractMethod(code, expected, dontPutOutOrRefOnStruct:=True)
            End Sub

            <WorkItem(3147, "https://github.com/dotnet/roslyn/issues/3147")>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub HandleFormattableStringTargetTyping1()
                Const code = "
Imports System

" & FormattableStringType & "

Namespace N
    Class C
        Public Sub M()
            Dim f = FormattableString.Invariant([|$""""|])
        End Sub
    End Class
End Namespace"

                Const expected = "
Imports System

" & FormattableStringType & "

Namespace N
    Class C
        Public Sub M()
            Dim f = FormattableString.Invariant(NewMethod())
        End Sub

        Private Shared Function NewMethod() As FormattableString
            Return $""""
        End Function
    End Class
End Namespace"

                TestExtractMethod(code, expected)
            End Sub

            <WpfFact>
            <Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            <Trait(Traits.Feature, Traits.Features.Interactive)>
            Public Sub ExtractMethodCommandDisabledInSubmission()
                Dim exportProvider = MinimalTestExportProvider.CreateExportProvider(
                TestExportProvider.EntireAssemblyCatalogWithCSharpAndVisualBasic.WithParts(GetType(InteractiveDocumentSupportsFeatureService)))

                Using workspace = TestWorkspaceFactory.CreateWorkspace(
                <Workspace>
                    <Submission Language="Visual Basic" CommonReferences="true">  
                        GetType(String).$$Name
                    </Submission>
                </Workspace>,
                workspaceKind:=WorkspaceKind.Interactive,
                exportProvider:=exportProvider)

                    ' Force initialization.
                    workspace.GetOpenDocumentIds().Select(Function(id) workspace.GetTestDocument(id).GetTextView()).ToList()

                    Dim textView = workspace.Documents.Single().GetTextView()

                    Dim handler = New ExtractMethodCommandHandler(
                        workspace.GetService(Of ITextBufferUndoManagerProvider)(),
                        workspace.GetService(Of IEditorOperationsFactoryService)(),
                        workspace.GetService(Of IInlineRenameService)(),
                        workspace.GetService(Of Host.IWaitIndicator)())
                    Dim delegatedToNext = False
                    Dim nextHandler =
                    Function()
                        delegatedToNext = True
                        Return CommandState.Unavailable
                    End Function

                    Dim state = handler.GetCommandState(New Commands.ExtractMethodCommandArgs(textView, textView.TextBuffer), nextHandler)
                    Assert.True(delegatedToNext)
                    Assert.False(state.IsAvailable)
                End Using
            End Sub

        End Class
    End Class
End Namespace