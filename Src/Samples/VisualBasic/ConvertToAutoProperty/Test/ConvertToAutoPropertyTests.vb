' *********************************************************
'
' Copyright © Microsoft Corporation
'
' Licensed under the Apache License, Version 2.0 (the
' "License"); you may not use this file except in
' compliance with the License. You may obtain a copy of
' the License at
'
' http://www.apache.org/licenses/LICENSE-2.0 
'
' THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES
' OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED,
' INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES
' OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR
' PURPOSE, MERCHANTABILITY OR NON-INFRINGEMENT.
'
' See the Apache 2 License for the specific language
' governing permissions and limitations under the License.
'
' *********************************************************

Imports ConvertToAutoPropertyVB
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Text
Imports Roslyn.UnitTestFramework
Imports Xunit

Public Class ConvertToAutoPropertyTests
    Inherits CodeRefactoringProviderTestFixture

    Protected Overrides Function CreateCodeRefactoringProvider() As ICodeRefactoringProvider
        Return New CodeRefactoringProvider()
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
