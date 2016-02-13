' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Reflection
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Scripting.Test
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Roslyn.Test.Utilities
Imports Xunit

#Disable Warning RS0003 ' Do not directly await a Task

Namespace Microsoft.CodeAnalysis.VisualBasic.Scripting.UnitTests

    Public Class InteractiveSessionTests
        Inherits TestBase

        <Fact>
        Public Async Function Fields() As Task
            Dim s = Await VisualBasicScript.
                RunAsync("Dim x As Integer = 1").
                ContinueWith("Dim y As Integer = 2").
                ContinueWith("?x + y")

            Assert.Equal(3, s.ReturnValue)
        End Function

        <Fact>
        Public Sub StatementExpressions_LineContinuation()
            Dim source = "
?1 _
"
            Assert.Equal(1, VisualBasicScript.EvaluateAsync(source).Result)
        End Sub

        <Fact>
        Public Sub StatementExpressions_IntLiteral()
            Dim source = "
?1
"
            Assert.Equal(1, VisualBasicScript.EvaluateAsync(source).Result)
        End Sub

        <Fact>
        Public Sub StatementExpressions_Nothing()
            Dim source = "
?  Nothing
"

            Assert.Null(VisualBasicScript.EvaluateAsync(source).Result)
        End Sub

        <WorkItem(10856, "DevDiv_Projects/Roslyn")>
        <Fact>
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

            Assert.Equal(6, VisualBasicScript.EvaluateAsync(source).Result)
        End Sub

        <Fact>
        Public Sub AnonymousTypes_TopLevel_MultipleSubmissions()
            Dim script = VisualBasicScript.Create("
Option Infer On
Dim a = New With { .f = 1 }
").ContinueWith("
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
").ContinueWith("
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
    End Class

End Namespace
