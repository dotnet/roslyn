' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Roslyn.UnitTestFramework
Imports Xunit

Public Class ConvertToAutoPropertyTests
    Inherits CodeRefactoringProviderTestFixture

    Protected Overrides Function CreateCodeRefactoringProvider() As CodeRefactoringProvider
        Return New ConvertToAutoPropertyCodeRefactoringProvider()
    End Function

    Protected Overrides ReadOnly Property LanguageName As String
        Get
            Return LanguageNames.VisualBasic
        End Get
    End Property

    <Fact>
    Public Sub TestSimpleCase()
        Dim code =
<Code>Class C
    Private x As Integer
    [|Public Property foo As Integer
        Get
            Return x
        End Get
        Set(value As integer)
            x = value
        End Set
    End Property|]
End Class</Code>

        Dim expected =
<Code>Class C

    Public Property foo As Integer
End Class</Code>

        Test(code, expected)
    End Sub

    <Fact>
    Public Sub TestCommaSeparatedField1()
        Dim code =
<Code>Class C
    Private x, y, z As Integer
    [|Public Property foo As Integer
        Get
            Return x
        End Get
        Set(value As integer)
            x = value
        End Set
    End Property|]
End Class</Code>

        Dim expected =
<Code>Class C
    Private y, z As Integer
    Public Property foo As Integer
End Class</Code>

        Test(code, expected)
    End Sub

    <Fact>
    Public Sub TestCommaSeparatedField2()
        Dim code =
<Code>Class C
    Private x, y, z As Integer
    [|Public Property foo As Integer
        Get
            Return y
        End Get
        Set(value As integer)
            y = value
        End Set
    End Property|]
End Class</Code>

        Dim expected =
<Code>Class C
    Private x, z As Integer
    Public Property foo As Integer
End Class</Code>

        Test(code, expected)
    End Sub

    <Fact>
    Public Sub TestCommaSeparatedField3()
        Dim code =
<Code>Class C
    Private x, y, z As Integer
    [|Public Property foo As Integer
        Get
            Return z
        End Get
        Set(value As integer)
            z = value
        End Set
    End Property|]
End Class</Code>

        Dim expected =
<Code>Class C
    Private x, y As Integer
    Public Property foo As Integer
End Class</Code>

        Test(code, expected)
    End Sub

End Class
