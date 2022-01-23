' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Public Class ParseLanguageVersionTests
    Inherits BasicTestBase

    <[Fact]>
    Public Sub AsyncMethodDeclaration()
        ParseAndVerify(
        <![CDATA[
Class C1
    Async Sub M1()

    End Sub

    Async Function M2() As Task

    End Function
End Class
        ]]>.Value,
            LanguageVersion.VisualBasic9,
            Diagnostic(ERRID.ERR_LanguageVersion, "Async").WithArguments("9.0", "async methods or lambdas", "11").WithLocation(3, 5),
            Diagnostic(ERRID.ERR_LanguageVersion, "Async").WithArguments("9.0", "async methods or lambdas", "11").WithLocation(7, 5))
    End Sub

    <[Fact]>
    Public Sub Iterator()
        ParseAndVerify(
        <![CDATA[
Module M1
    Private Iterator Function SomeNumbers() As System.Collections.IEnumerable
        Yield 3
        Yield 5
        Yield 8
    End Function
End Module
        ]]>.Value,
            LanguageVersion.VisualBasic9,
            Diagnostic(ERRID.ERR_LanguageVersion, "Iterator").WithArguments("9.0", "iterators", "11").WithLocation(3, 13),
            Diagnostic(ERRID.ERR_LanguageVersion, "Yield").WithArguments("9.0", "iterators", "11").WithLocation(4, 9),
            Diagnostic(ERRID.ERR_LanguageVersion, "Yield").WithArguments("9.0", "iterators", "11").WithLocation(5, 9),
            Diagnostic(ERRID.ERR_LanguageVersion, "Yield").WithArguments("9.0", "iterators", "11").WithLocation(6, 9))
    End Sub

    <[Fact]>
    Public Sub CollectionInitializers()
        ParseAndVerify(
        <![CDATA[
Module M1
    Sub Test()
        Dim menuOptions = New List(Of MenuOption) From {{1, "Home"}, {2, "Products"}, {3, "News"}, {4, "Contact Us"}}
    End Sub
End Module
        ]]>.Value,
            LanguageVersion.VisualBasic9,
            Diagnostic(ERRID.ERR_LanguageVersion, "From").WithArguments("9.0", "collection initializers", "10").WithLocation(4, 51))
    End Sub

    <[Fact]>
    Public Sub CoAndContraVariance()
        ParseAndVerify(
        <![CDATA[
Interface IVariant(Of Out R, In A)
    Function GetSomething() As R
    Sub SetSomething(ByVal sampleArg As A)
    Function GetSetSomething(ByVal sampleArg As A) As R
End Interface
        ]]>.Value,
            LanguageVersion.VisualBasic9,
            Diagnostic(ERRID.ERR_LanguageVersion, "Out").WithArguments("9.0", "variance", "10").WithLocation(2, 23),
            Diagnostic(ERRID.ERR_LanguageVersion, "In").WithArguments("9.0", "variance", "10").WithLocation(2, 30))
    End Sub

    <[Fact]>
    Public Sub AwaitExpressions()
        ParseAndVerify(
        <![CDATA[
Module M1
    Private Async Function SumPageSizesAsync() As Task

        ' To use the HttpClient type in desktop apps, you must include a using directive and add a 
        ' reference for the System.Net.Http namespace.
        Dim client As HttpClient = New HttpClient() 
        ' . . . 
        Dim getContentsTask As Task(Of Byte()) = client.GetByteArrayAsync(url)
        Dim urlContents As Byte() = Await getContentsTask

        ' Equivalently, now that you see how it works, you can write the same thing in a single line.
        'Dim urlContents As Byte() = Await client.GetByteArrayAsync(url)
    End Function
End Module
        ]]>.Value,
            LanguageVersion.VisualBasic9,
            Diagnostic(ERRID.ERR_LanguageVersion, "Async").WithArguments("9.0", "async methods or lambdas", "11").WithLocation(3, 13))
    End Sub

    <[Fact]>
    Public Sub StatementLambdas()
        ParseAndVerify(
        <![CDATA[
Module M1
    Sub Test()
        Dim l1 = Sub()
                    Console.WriteLine()
                 End Sub
        Dim l2 = Function()
                    Return 1
                 End Function
    End Sub
End Module
        ]]>.Value,
            LanguageVersion.VisualBasic9,
            True,
            Diagnostic(ERRID.ERR_LanguageVersion),
            Diagnostic(ERRID.ERR_LanguageVersion))
    End Sub

    <Fact>
    Public Sub AutoProperties()
        ParseAndVerify(
        <![CDATA[
Class C1
    Public Property Name As String 
    Public Property Owner As String = "DefaultName" 
    Public Property Id As Integer
        Get
            Return 0
        End Get
        Set
            
        End Set
    End Property
End Class
        ]]>.Value,
            LanguageVersion.VisualBasic9,
            Diagnostic(ERRID.ERR_LanguageVersion, "Public Property Name As String").WithArguments("9.0", "auto-implemented properties", "10").WithLocation(3, 5),
            Diagnostic(ERRID.ERR_LanguageVersion, "Public Property Owner As String = ""DefaultName""").WithArguments("9.0", "auto-implemented properties", "10").WithLocation(4, 5))
    End Sub

    <[Fact]>
    Public Sub ImplicitLineContinuationNonQuery()
        ParseAndVerify(
        <![CDATA[
Class C1
    Sub Test(args As String())
        M1(1,
            2)
        M2(1 >
            2)
        For Each x 
            in Args
        Next
        Dim x =
            13
        x.
            ToString()
    End Sub

    Sub M1(x, y) 
    End Sub
    Sub M2(x)
    End Sub 
End Class
        ]]>.Value,
            LanguageVersion.VisualBasic9,
            True,
            Diagnostic(ERRID.ERR_LanguageVersion),
            Diagnostic(ERRID.ERR_LanguageVersion),
            Diagnostic(ERRID.ERR_LanguageVersion),
            Diagnostic(ERRID.ERR_LanguageVersion),
            Diagnostic(ERRID.ERR_LanguageVersion))
    End Sub

    <[Fact]>
    Public Sub ImplicitLineContinuationQuery()
        ParseAndVerify(
        <![CDATA[
Class C1
    Sub Test(args As String())
        Dim e1 = From x in args
                Where x IsNot Nothing
                Group By CountryName = cust.Country
                Into CustomersInCountry = Group, Count()

        Dim e2 = From x 
                 In args
                 Order By
                     x.GetHashCode()
                 Group By
                     x.GetHashCode()
                 Into x
    End Sub
End Class
        ]]>.Value,
            LanguageVersion.VisualBasic9,
            True,
            Diagnostic(ERRID.ERR_LanguageVersion),
            Diagnostic(ERRID.ERR_LanguageVersion))
    End Sub

    <[Fact]>
    Public Sub NameOfExpression()
        ParseAndVerify(
        <![CDATA[
Class C1
    Sub Test(args As String())
        If args Is Nothing Then
            Throw New ArgumentException(NameOf(args))
        End If
    End Sub
End Class
        ]]>.Value,
            LanguageVersion.VisualBasic9,
            Diagnostic(ERRID.ERR_LanguageVersion, "NameOf").WithArguments("9.0", "'nameof' expressions", "14").WithLocation(5, 41))
    End Sub

    <[Fact]>
    Public Sub NullConditionalOperation()
        Dim x = Nothing
        ParseAndVerify(
        <![CDATA[
Class C1
    Sub Test(args As String)
        Dim y As String = args?.ToString()
        Dim x1 = args?(0)
    End Sub
End Class
        ]]>.Value,
            LanguageVersion.VisualBasic9,
            Diagnostic(ERRID.ERR_LanguageVersion, "?").WithArguments("9.0", "null conditional operations", "14").WithLocation(4, 31),
            Diagnostic(ERRID.ERR_LanguageVersion, "?").WithArguments("9.0", "null conditional operations", "14").WithLocation(5, 22))
    End Sub

    <[Fact]>
    Public Sub GlobalKeyword_01()
        Dim source = "
Imports System

Namespace Global
    Module Program

        Sub Main()
        End Sub

    End Module
End Namespace"

        For Each version In {LanguageVersion.VisualBasic9, LanguageVersion.VisualBasic10}
            ParseAndVerify(source, version,
                Diagnostic(ERRID.ERR_LanguageVersion, "Global").WithArguments($"{CInt(version)}.0", "declaring a Global namespace", "11").WithLocation(4, 11))
        Next

        For Each version In {LanguageVersion.VisualBasic11, LanguageVersion.VisualBasic12, LanguageVersion.VisualBasic14, VisualBasicParseOptions.Default.LanguageVersion}
            ParseAndVerify(source, version, False, Nothing)
        Next
    End Sub

    <[Fact]>
    Public Sub GlobalKeyword_02()
        Dim source = "
Module Program
    Function getValue() As Global.System.Int32
        Return 14
    End Function
End Module"

        For Each version In {LanguageVersion.VisualBasic9, LanguageVersion.VisualBasic10,
                             LanguageVersion.VisualBasic11, LanguageVersion.VisualBasic12,
                             LanguageVersion.VisualBasic14, LanguageVersion.VisualBasic15,
                             LanguageVersion.Default, LanguageVersion.Latest}
            ParseAndVerify(source, version, False, Nothing)
        Next
    End Sub

    <[Fact]>
    Public Sub GlobalKeyword_03()
        Dim source = "
Imports System

Namespace Global.Ns1
    Module Program

        Sub Main()
        End Sub

    End Module
End Namespace"

        For Each version In {LanguageVersion.VisualBasic9, LanguageVersion.VisualBasic10}
            ParseAndVerify(source, version,
                Diagnostic(ERRID.ERR_LanguageVersion, "Global").WithArguments($"{CInt(version)}.0", "declaring a Global namespace", "11").WithLocation(4, 11))
        Next

        For Each version In {LanguageVersion.VisualBasic11, LanguageVersion.VisualBasic12, LanguageVersion.VisualBasic14, LanguageVersion.VisualBasic15,
                             LanguageVersion.Default, LanguageVersion.Latest}
            ParseAndVerify(source, version, False, Nothing)
        Next
    End Sub

    <Fact>
    Public Sub InterpolatedStrings()
        Dim x = Nothing
        ParseAndVerify(
        <![CDATA[
Module Module1
    Function M() As String
        Dim x1 = $"world"
        Dim x2 = $"hello {x1}"
        Return x2
    End Function
End Module
        ]]>.Value,
            LanguageVersion.VisualBasic12,
            Diagnostic(ERRID.ERR_LanguageVersion, "$""world""").WithArguments("12.0", "interpolated strings", "14").WithLocation(4, 18),
            Diagnostic(ERRID.ERR_LanguageVersion, "$""hello {x1}""").WithArguments("12.0", "interpolated strings", "14").WithLocation(5, 18))
    End Sub

    <Fact>
    Public Sub TupleExpression()
        Dim x = Nothing
        ParseAndVerify(
        <![CDATA[
Module Module1
    Function M() As String
        Dim x1 = (1, 2)
        Dim x2 = (A:=1, B:=2)

        Return nothing
    End Function
End Module
        ]]>.Value,
            LanguageVersion.VisualBasic14,
            Diagnostic(ERRID.ERR_LanguageVersion, "(1, 2)").WithArguments("14.0", "tuples", "15").WithLocation(4, 18),
            Diagnostic(ERRID.ERR_LanguageVersion, "(A:=1, B:=2)").WithArguments("14.0", "tuples", "15").WithLocation(5, 18))
    End Sub

    <Fact>
    Public Sub TupleType()
        Dim x = Nothing
        ParseAndVerify(
        <![CDATA[
Module Module1
    Function M() As String
        Dim x1 As (Integer, Integer) = Nothing
        Dim x1 As (A As Integer, B As Integer) = Nothing

        Return Nothing
    End Function
End Module
        ]]>.Value,
            LanguageVersion.VisualBasic14,
            Diagnostic(ERRID.ERR_LanguageVersion, "(Integer, Integer)").WithArguments("14.0", "tuples", "15").WithLocation(4, 19),
            Diagnostic(ERRID.ERR_LanguageVersion, "(A As Integer, B As Integer)").WithArguments("14.0", "tuples", "15").WithLocation(5, 19))
    End Sub
End Class
