' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.IO
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests
Imports Microsoft.CodeAnalysis.Scripting.Test
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.Scripting.VisualBasic.UnitTests

    Public Class InteractiveSessionTests
        Inherits BasicTestBase

        <Fact>
        Public Sub Fields()
            Dim engine = New VisualBasicScriptEngine()
            Dim session As Session = engine.CreateSession()

            session.Execute("Dim x As Integer = 1")
            session.Execute("Dim y As Integer = 2")
            Dim result = session.Execute("?x + y")
            Assert.Equal(3, result)
        End Sub

        <Fact>
        Public Sub StatementExpressions_LineContinuation()
            Dim source = <text>
?1 _
</text>.Value

            Dim engine = New VisualBasicScriptEngine()
            Dim result = engine.CreateSession().Execute(source)
            Assert.Equal(result, 1)
        End Sub

        <Fact>
        Public Sub StatementExpressions_IntLiteral()
            Dim source = <text>
?1
</text>.Value

            Dim engine = New VisualBasicScriptEngine()
            Dim result = engine.CreateSession().Execute(source)
            Assert.Equal(result, 1)
        End Sub

        <Fact>
        Public Sub StatementExpressions_Nothing()
            Dim source = <text>
?  Nothing
</text>.Value

            Dim engine = New VisualBasicScriptEngine()
            Dim session = engine.CreateSession()
            Dim result = session.Execute(source)
            Assert.Equal(result, Nothing)
        End Sub

        Public Class B
            Public x As Integer = 1, w As Integer = 4
        End Class

        <WorkItem(10856, "DevDiv_Projects/Roslyn")>
        <Fact>
        Public Sub IfStatement()
            Dim source = <text>
Dim x As Integer
If (True)
   x = 5
Else
   x = 6
End If

?x + 1
</text>.Value

            Dim engine = New VisualBasicScriptEngine()
            Dim session = engine.CreateSession()
            Dim result = session.Execute(source)

            Assert.Equal(6, result)
        End Sub

        <WorkItem(530404)>
        <Fact>
        Public Sub DiagnosticsPass()
            Dim engine = New VisualBasicScriptEngine()
            Dim session = engine.CreateSession()
            session.AddReference(GetType(Expressions.Expression).Assembly)
            session.Execute(
"Function F(e As System.Linq.Expressions.Expression(Of System.Func(Of Object))) As Object
    Return e.Compile()()
End Function")
            ScriptingTestHelpers.AssertCompilationError(
                session,
                "F(Function()
                        Return Nothing
                    End Function)",
                Diagnostic(ERRID.ERR_StatementLambdaInExpressionTree, "Function()
                        Return Nothing
                    End Function").WithLocation(1, 3))
        End Sub

        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/4003")>
        Public Sub AnonymousTypes_TopLevel_MultipleSubmissions()
            Dim engine = New VisualBasicScriptEngine()
            Dim session = engine.CreateSession()

            session.Execute(
    <text>
Option Infer On
Dim a = New With { .f = 1 }
</text>.Value)

            session.Execute(
    <text>
Option Infer On
Dim b = New With { Key .f = 1 }
</text>.Value)

            Dim result = session.Execute(Of Object)(
            <![CDATA[
    Option Infer On
    Dim c = New With { .F = 222 }
    Dim d = New With { Key .F = 777 }
    ? (a.GetType() is c.GetType()).ToString() _
        & " " & (a.GetType() is b.GetType()).ToString() _ 
        & " " & (b.GetType() is d.GetType()).ToString()
    ]]>.Value)

            Assert.Equal("True False True", result.ToString)
        End Sub

        <Fact>
        Public Sub AnonymousTypes_TopLevel_MultipleSubmissions2()
            Dim engine = New VisualBasicScriptEngine()
            Dim session = engine.CreateSession()

            session.Execute(
    <text>
Option Infer On
Dim a = Sub()
        End Sub
</text>.Value)

            session.Execute(
    <text>
Option Infer On
Dim b = Function () As Integer
            Return 0
        End Function
</text>.Value)

            Dim result = session.Execute(Of Object)(
            <![CDATA[
    Option Infer On
    Dim c = Sub()
            End Sub
    Dim d = Function () As Integer
                Return 0
            End Function
    ? (a.GetType() is c.GetType()).ToString() _
        & " " & (a.GetType() is b.GetType()).ToString() _ 
        & " " & (b.GetType() is d.GetType()).ToString()
    ]]>.Value)

            Assert.Equal("True False True", result.ToString)
        End Sub
    End Class
End Namespace
