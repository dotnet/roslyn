﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.ExtractMethod
    Partial Public Class ExtractMethodTests
        <[UseExportProvider]>
        Public Class MethodNameGeneration

            <Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestGetLiteralGeneratesSmartName() As Task
                Dim code = <text>Public Class Class1
    Sub MySub()
        Dim a As Integer = [|10|]
    End Sub
End Class</text>

                Dim expected = <text>Public Class Class1
    Sub MySub()
        Dim a As Integer = GetA()
    End Sub

    Private Shared Function GetA() As Integer
        Return 10
    End Function
End Class</text>

                Await TestExtractMethodAsync(code, expected)
            End Function

            <Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestGetLiteralDoesNotGenerateSmartName() As Task
                Dim code = <text>Public Class Class1
    Sub MySub()
        Dim a As Integer = [|10|] + 42
    End Sub
End Class</text>

                Dim expected = <text>Public Class Class1
    Sub MySub()
        Dim a As Integer = NewMethod() + 42
    End Sub

    Private Shared Function NewMethod() As Integer
        Return 10
    End Function
End Class</text>

                Await TestExtractMethodAsync(code, expected)
            End Function

            <Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestGetLiteralGeneratesSmartName2() As Task
                Dim code = <text>Public Class Class1
    Sub MySub()
        Dim b As Integer = 5
        Dim a As Integer = 10 + [|b|]
    End Sub
End Class</text>

                Dim expected = <text>Public Class Class1
    Sub MySub()
        Dim b As Integer = 5
        Dim a As Integer = 10 + GetB(b)
    End Sub

    Private Shared Function GetB(b As Integer) As Integer
        Return b
    End Function
End Class</text>

                Await TestExtractMethodAsync(code, expected)
            End Function

            <Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestAppendingNumberedSuffixToGetMethods() As Task
                Dim code = <text>Class A
    Function Test1() As Integer
        Dim x As Integer = GetX()
        Console.Write([|x|])
        Return x
    End Function

    Private Shared Function GetX() As Integer
        Return 5
    End Function
End Class</text>

                Dim expected = <text>Class A
    Function Test1() As Integer
        Dim x As Integer = GetX()
        Console.Write(GetX1(x))
        Return x
    End Function

    Private Shared Function GetX1(x As Integer) As Integer
        Return x
    End Function

    Private Shared Function GetX() As Integer
        Return 5
    End Function
End Class</text>

                Await TestExtractMethodAsync(code, expected)
            End Function

            <Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestAppendingNumberedSuffixToNewMethods() As Task
                Dim code = <text>Class A
    Function Test1() As Integer
        Dim x As Integer = 5
        [|Console.Write(5)|]
        Return x
    End Function

    Private Shared Sub NewMethod()
    End Sub
End Class</text>

                Dim expected = <text>Class A
    Function Test1() As Integer
        Dim x As Integer = 5
        NewMethod1()
        Return x
    End Function

    Private Shared Sub NewMethod1()
        Console.Write(5)
    End Sub

    Private Shared Sub NewMethod()
    End Sub
End Class</text>

                Await TestExtractMethodAsync(code, expected)
            End Function

            ''' This is a special case in VB as it is case insensitive
            ''' Hence Get_FirstName() would conflict with the internal get_FirstName() that VB generates for the getter
            <WorkItem(540483, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540483")>
            <Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestPropertyGetter() As Task
                Dim code = <text>Class Program
    Private _FirstName As String

    Property FirstName() As String
        Get
            Dim name As String
            name = [|_FirstName|]
            Return name
        End Get
        Set(ByVal value As String)
            _FirstName = value
        End Set
    End Property 
End Class</text>

                Dim expected = <text>Class Program
    Private _FirstName As String

    Property FirstName() As String
        Get
            Dim name As String
            name = GetFirstName()
            Return name
        End Get
        Set(ByVal value As String)
            _FirstName = value
        End Set
    End Property

    Private Function GetFirstName() As String
        Return _FirstName
    End Function
End Class</text>

                ' changed test due to implicit function variable bug in VB
                Await TestExtractMethodAsync(code, expected)
            End Function

            <WorkItem(530674, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530674")>
            <Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestEscapedParameterName() As Task
                Dim code = <text>Imports System.Linq

Module Program
    Sub Goo()
        Dim x = From [char] In ""
                Select [|[char]|] ' Extract method
    End Sub
End Module</text>

                Dim expected = <text>Imports System.Linq

Module Program
    Sub Goo()
        Dim x = From [char] In ""
                Select GetChar([char]) ' Extract method
    End Sub

    Private Function GetChar([char] As Char) As Char
        Return [char]
    End Function
End Module</text>

                Await TestExtractMethodAsync(code, expected)
            End Function
        End Class
    End Class
End Namespace
