' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.IO
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Public Class StatementSyntaxWalkerTests
    <ConditionalFact(GetType(WindowsOnly))>
    Public Sub TestStatementSyntaxWalker()
        Dim tree = ParseAndVerify(<![CDATA[
Option Explicit Off
Imports System
<Assembly: CLSCompliant(False)> 

Namespace Goo.Bar
    Public Class Class1
        Dim x As Integer
        Public Function f(ByVal a As Boolean) As Integer
            Dim r = 1, s = 4
            Try
                If a Then r = 4 Else r = 3 : s = f(True)
                If a Then
                    r = 17 : s = 45
                    s = r + 1
                Else
                    r = 25
                End If
            Catch e As Exception
                Throw e
            Finally
                Console.WriteLine("finally!")
            End Try
            Select Case r
                Case 4
                    Console.WriteLine("f")
                Case Else
                    Return 4 + s
            End Select
            While r < s
                r = r + 1
                s = s - 1
            End While
            Return s
        End Function
    End Class
End Namespace
            ]]>)

        Dim writer As New StringWriter()
        Dim myWalker = New TestWalker(writer)
        myWalker.Visit(tree.GetRoot())

        Dim expected = <![CDATA[
Option Explicit Off
Imports System
<Assembly: CLSCompliant(False)>
Namespace Goo.Bar
Public Class Class1
Dim x As Integer
Public Function f(ByVal a As Boolean) As Integer
Dim r = 1, s = 4
Try
r = 4
r = 3
s = f(True)
If a Then
r = 17
s = 45
s = r + 1
Else
r = 25
End If
Catch e As Exception
Throw e
Finally
Console.WriteLine("finally!")
End Try
Select Case r
Case 4
Console.WriteLine("f")
Case Else
Return 4 + s
End Select
While r < s
r = r + 1
s = s - 1
End While
Return s
End Function
End Class
End Namespace
                           ]]>.Value

        expected = expected.Replace(vbLf, vbCrLf).Trim()
        Dim actual = writer.ToString().Trim()

        Assert.Equal(expected, actual)
    End Sub

    Friend Class TestWalker
        Inherits StatementSyntaxWalker

        Private ReadOnly _arg As TextWriter

        Public Sub New(arg As TextWriter)
            Me._arg = arg
        End Sub

        Public Overrides Sub DefaultVisit(node As SyntaxNode)
            If TypeOf node Is StatementSyntax Then
                _arg.WriteLine(node.ToString())
            End If

            MyBase.DefaultVisit(node)
        End Sub
    End Class
End Class
