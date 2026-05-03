' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Scripting
Imports Microsoft.CodeAnalysis.Scripting.TestUtilities
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.Scripting.UnitTests

    Public Class InteractiveSessionTests
        Inherits VisualBasicScriptTestBase

        <Fact>
        Public Async Function Fields() As Task
            Dim s = Await VisualBasicScript.
                RunAsync("Dim x As Integer = 1", ScriptOptions).
                ContinueWith("Dim y As Integer = 2").
                ContinueWith("?x + y")

            Assert.Equal(3, s.ReturnValue)
        End Function

        <Fact>
        Public Sub StatementExpressions_LineContinuation()
            Dim source = "
?1 _
"
            Assert.Equal(1, VisualBasicScript.EvaluateAsync(source, ScriptOptions).Result)
        End Sub

        <Fact>
        Public Sub StatementExpressions_IntLiteral()
            Dim source = "
?1
"
            Assert.Equal(1, VisualBasicScript.EvaluateAsync(source, ScriptOptions).Result)
        End Sub

        <Fact>
        Public Sub StatementExpressions_Nothing()
            Dim source = "
?  Nothing
"

            Assert.Null(VisualBasicScript.EvaluateAsync(source, ScriptOptions).Result)
        End Sub

        <Fact, WorkItem(10856, "DevDiv_Projects/Roslyn")>
        Public Sub IfStatement()
            Dim source = "
Dim x As Integer
If (True)
   x = 5
Else
   x = 6
End If

?x + 1
"

            Assert.Equal(6, VisualBasicScript.EvaluateAsync(source, ScriptOptions).Result)
        End Sub

        <Fact>
        Public Sub AnonymousTypes_TopLevel_MultipleSubmissions()
            Dim script = VisualBasicScript.Create("
Option Infer On
Dim a = New With { .f = 1 }
", ScriptOptions).ContinueWith("
Option Infer On
Dim b = New With { Key .f = 1 }
").ContinueWith("
Option Infer On
Dim c = New With { .F = 222 }
Dim d = New With { Key .F = 777 }

? (a.GetType() Is c.GetType()).ToString() _
    & "" "" & (a.GetType() Is b.GetType()).ToString() _
    & "" "" & (b.GetType() is d.GetType()).ToString()
")
            Assert.Equal("True False True", script.EvaluateAsync().Result)
        End Sub

        <Fact>
        Public Sub AnonymousTypes_TopLevel_MultipleSubmissions2()
            Dim script = VisualBasicScript.Create("
Option Infer On
Dim a = Sub()
        End Sub
", ScriptOptions).ContinueWith("
Option Infer On
Dim b = Function () As Integer
            Return 0
        End Function
").ContinueWith("
Option Infer On
Dim c = Sub()
        End Sub
Dim d = Function () As Integer
            Return 0
        End Function
? (a.GetType() is c.GetType()).ToString() _
    & "" "" & (a.GetType() is b.GetType()).ToString() _ 
    & "" "" & (b.GetType() is d.GetType()).ToString()
")

            Assert.Equal("True False True", script.EvaluateAsync().Result)
        End Sub

        <Fact>
        Public Sub CompilationChain_Accessibility()
            ' Submissions have internal and protected access to one another.
            Dim state1 = VisualBasicScript.RunAsync(
"Friend Class C1
End Class
Protected X As Integer
", ScriptOptions)
            Dim compilation1 = state1.Result.Script.GetCompilation()
            compilation1.VerifyDiagnostics()

            Dim state2 = state1.ContinueWith(
"Friend Class C2
    Inherits C1
End Class
")
            Dim compilation2 = state2.Result.Script.GetCompilation()
            compilation2.VerifyDiagnostics()
            Dim c2C2 = DirectCast(lookupMember(compilation2, "Submission#1", "C2"), INamedTypeSymbol)
            Dim c2C1 = c2C2.BaseType
            Dim c2X = lookupMember(compilation1, "Submission#0", "X")
            Assert.True(compilation2.IsSymbolAccessibleWithin(c2C1, c2C2))
            Assert.True(compilation2.IsSymbolAccessibleWithin(c2X, c2C2))  ' access not enforced among submission symbols

            Dim state3 = state2.ContinueWith(
"Friend Class C3
    Inherits C2
End Class
")
            Dim compilation3 = state3.Result.Script.GetCompilation()
            compilation3.VerifyDiagnostics()
            Dim c3C3 = DirectCast(lookupMember(compilation3, "Submission#2", "C3"), INamedTypeSymbol)
            Dim c3C2 = c3C3.BaseType
            Dim c3C1 = c3C2.BaseType
            Dim action As Action = Sub() compilation2.IsSymbolAccessibleWithin(c3C3, c3C1)
            Assert.Throws(Of ArgumentException)(action)
            Assert.True(compilation3.IsSymbolAccessibleWithin(c3C1, c3C3))
            Assert.True(compilation3.IsSymbolAccessibleWithin(c3C2, c3C3))
        End Sub

        Function lookupType(c As Compilation, name As String) As INamedTypeSymbol
            Return DirectCast(c.GlobalNamespace.GetMembers(name).Single(), INamedTypeSymbol)
        End Function
        Function lookupMember(c As Compilation, typeName As String, memberName As String) As ISymbol
            Return lookupType(c, typeName).GetMembers(memberName).Single()
        End Function
    End Class

End Namespace
