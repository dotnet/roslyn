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

#If False Then
Option Strict Off
Imports MakeConstVB
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeActions.Providers
Imports Microsoft.CodeAnalysis.Editor
Imports Microsoft.CodeAnalysis.Text
Imports Roslyn.UnitTestFramework
Imports Xunit

Public Class MakeConstTests
    Inherits SyntaxNodeCodeIssueProviderTestFixture

    <Fact()>
    Public Sub SimpleCase1()
        Test("Dim i As Integer = 0", "Const i As Integer = 0")
    End Sub

    <Fact()>
    Public Sub SimpleCase2()
        Test("Dim i As Integer = 0, j As Integer = 1", "Const i As Integer = 0, j As Integer = 1")
    End Sub

    <Fact()>
    Public Sub NotAvailableIfVariableIsWritten()
        Dim code =
<Code>
    Dim i As Integer = 0
    i = 1
</Code>.Value

        TestMissing(code)
    End Sub

    Private Overloads Sub Test(code As String, expected As String, Optional issueIndex As Integer = 0, Optional actionIndex As Integer = 0, Optional compareTokens As Boolean = True)
        ' TODO: There's an oddity in the VB "Hammer of Thor" formatter that causes it to add a line break after the class statement

        Dim codeInClass = "Class C" & vbCrLf &
                          "" & vbCrLf &
                          "    Sub M()" & vbCrLf &
                          "        " & code & vbCrLf &
                          "    End Sub" & vbCrLf &
                          "End Class"

        Dim expectedInClass = "Class C" & vbCrLf &
                              "" & vbCrLf &
                              "    Sub M()" & vbCrLf &
                              "        " & expected & vbCrLf &
                              "    End Sub" & vbCrLf &
                              "End Class"

        Test(codeInClass, expectedInClass,
             nodeFinder:=Function(root) root.Members(0).Members(0).Statements(0),
             issueIndex:=issueIndex,
             actionIndex:=actionIndex,
             compareTokens:=compareTokens)
    End Sub

    Private Overloads Sub TestMissing(code As String)
        Dim codeInClass = "Class C" & vbCrLf &
                          "" & vbCrLf &
                          "    Sub M()" & vbCrLf &
                          "        " & code & vbCrLf &
                          "    End Sub" & vbCrLf &
                          "End Class"

        TestMissing(codeInClass,
             nodeFinder:=Function(root) root.Members(0).Members(0).Statements(0))
    End Sub

    Protected Overrides Function CreateCodeIssueProvider() As ICodeIssueProvider
        Return New CodeIssueProvider()
    End Function

    Protected Overrides ReadOnly Property LanguageName As String
        Get
            Return LanguageNames.VisualBasic
        End Get
    End Property
End Class
#End If