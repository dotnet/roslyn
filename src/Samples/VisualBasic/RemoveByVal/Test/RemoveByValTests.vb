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

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Roslyn.UnitTestFramework
Imports Xunit

Public Class RemoveByValTests
    Inherits CodeRefactoringProviderTestFixture

    Protected Overrides Function CreateCodeRefactoringProvider() As CodeRefactoringProvider
        Return New RemoveByValCodeRefactoringProvider()
    End Function

    Protected Overrides ReadOnly Property LanguageName As String
        Get
            Return LanguageNames.VisualBasic
        End Get
    End Property

    <Fact()>
    Public Sub TestSimple()
        Test(
<Text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main([||]ByVal args As String())
    End Sub
End Module</Text>.Value,
<Text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
    End Sub
End Module</Text>.Value)
    End Sub

    <Fact(Skip:="Fix when formatting support is in Workspace layer")>
    Public Sub TestFormatting1()
        Test(
<Text>Class A

    Function Go(
                    [||]ByVal x As Integer,
                    ByVal y As Integer,
                    ByVal z As Integer
                ) As Integer
        Return 1
    End Function
End Class
</Text>.Value,
<Text>Class A

    Function Go(
                    x As Integer,
                    ByVal y As Integer,
                    ByVal z As Integer
                ) As Integer
        Return 1
    End Function
End Class
</Text>.Value,
actionIndex:=0,
compareTokens:=False)
    End Sub

    <Fact(Skip:="Fix when formatting support is in Workspace layer")>
    Public Sub TestFormattingAll()
        Test(
<Text>Class A

    Function Go(
                    [||]ByVal x As Integer,
                    ByVal y As Integer,
                    ByVal z As Integer
                ) As Integer
        Return 1
    End Function
End Class
</Text>.Value,
<Text>Class A

    Function Go(
                    x As Integer,
                    y As Integer,
                    z As Integer
                ) As Integer
        Return 1
    End Function
End Class
</Text>.Value,
actionIndex:=1,
compareTokens:=False)
    End Sub
End Class
