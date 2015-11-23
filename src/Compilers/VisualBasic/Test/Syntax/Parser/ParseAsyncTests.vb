' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Public Class ParseAsyncTests
    Inherits BasicTestBase

    <Fact>
    Public Sub ParseAsyncModifier()
        Dim tree = ParseAndVerify(<![CDATA[
Imports Async = System.Threading.Tasks.Task

Module Program
    Public Const Async As Integer = 0

    Public Async

    <Async(Async)>
    Async Function M() As Async

        Dim l = Sub()
                    Dim async As Async = Nothing
                End Sub

        Dim l2 = Async Sub()
                     Dim async As Async = Nothing                        
                 End Sub

    End Function
End Module

Class AsyncAttribute
    Inherits Attribute

    Sub New(p)

    End Sub
End Class]]>)

        Assert.Equal(2, Aggregate t In tree.GetRoot().DescendantTokens Where t.Kind = SyntaxKind.AsyncKeyword Into Count())

        Dim fields = tree.GetRoot().DescendantNodes.OfType(Of FieldDeclarationSyntax)().ToArray()
        Assert.Equal(2, Aggregate f In fields Where f.Declarators(0).Names(0).Identifier.ValueText = "Async" Into Count())

    End Sub

    <Fact>
    Public Sub ParseAwaitExpressions()
        Dim tree = ParseAndVerify(<![CDATA[
Imports System.Console

Module Program
    Private t1 As Task
    Private t2 As Task(Of Integer)

    Async Sub M()

        Await t1

        WriteLine(Await t2)

    End Sub

    Sub M2()

        Await t1

        WriteLine(Await t2)

    End Sub

    Async Function N() As Task(Of Integer)

        Await t1

        Return Await t2
        
    End Function

    Function N2()

        Await t1

        Return (Await t2)

    End Function

End Module]]>,
<errors>
    <error id="30800" message="Method arguments must be enclosed in parentheses." start="206" end="208"/>
    <error id="32017" message="Comma, ')', or a valid expression continuation expected." start="234" end="236"/>
    <error id="30800" message="Method arguments must be enclosed in parentheses." start="398" end="400"/>
    <error id="30198" message="')' expected." start="424" end="424"/>
</errors>)

        Dim awaitExpressions = tree.GetRoot().DescendantNodes.OfType(Of AwaitExpressionSyntax)()

        Assert.Equal(4, awaitExpressions.Count)

        Dim firstStatementOfM = tree.GetRoot().DescendantNodes.OfType(Of MethodBlockBaseSyntax).First.Statements.First

        Assert.Equal(SyntaxKind.ExpressionStatement, firstStatementOfM.Kind)
        Assert.Equal(SyntaxKind.AwaitExpression, CType(firstStatementOfM, ExpressionStatementSyntax).Expression.Kind)

        Dim firstStatementOfM2 = tree.GetRoot().DescendantNodes.OfType(Of MethodBlockBaseSyntax)()(1).Statements.First

        Assert.Equal(SyntaxKind.ExpressionStatement, firstStatementOfM2.Kind)
        Assert.Equal(SyntaxKind.InvocationExpression, CType(firstStatementOfM2, ExpressionStatementSyntax).Expression.Kind)

        Dim firstStatementOfN = tree.GetRoot().DescendantNodes.OfType(Of MethodBlockBaseSyntax)()(2).Statements.First

        Assert.Equal(SyntaxKind.ExpressionStatement, firstStatementOfM.Kind)
        Assert.Equal(SyntaxKind.AwaitExpression, CType(firstStatementOfM, ExpressionStatementSyntax).Expression.Kind)

        Dim firstStatementOfN2 = tree.GetRoot().DescendantNodes.OfType(Of MethodBlockBaseSyntax)()(3).Statements.First

        Assert.Equal(SyntaxKind.ExpressionStatement, firstStatementOfM2.Kind)
        Assert.Equal(SyntaxKind.InvocationExpression, CType(firstStatementOfM2, ExpressionStatementSyntax).Expression.Kind)

    End Sub

    <Fact>
    Public Sub ParseAwaitExpressionsWithPrecedence()
        Dim tree = ParseAndVerify(<![CDATA[
Module Program
    Async Function M(a As Task(Of Integer), x As Task(Of Integer), y As Task(Of Integer), b As Task(Of Integer)) As Task(Of Integer)

        Return Await a * Await x ^ Await y + Await b
        
    End Function
End Module]]>)

        Dim returnStatement = tree.GetRoot().DescendantNodes.OfType(Of ReturnStatementSyntax).Single()

        Dim expression = CType(returnStatement.Expression, BinaryExpressionSyntax)

        Assert.Equal(SyntaxKind.AddExpression, expression.Kind)

        Assert.Equal(SyntaxKind.MultiplyExpression, expression.Left.Kind)

        Dim left = CType(expression.Left, BinaryExpressionSyntax)

        Assert.Equal(SyntaxKind.AwaitExpression, left.Left.Kind)

        Assert.Equal(SyntaxKind.ExponentiateExpression, left.Right.Kind)

        Dim right = expression.Right

        Assert.Equal(SyntaxKind.AwaitExpression, right.Kind)

    End Sub

    <Fact>
    Public Sub ParseAwaitStatements()
        Dim tree = ParseAndVerify(<![CDATA[
Imports System.Console

Module Program
    Private t1 As Task
    Private t2 As Task(Of Integer)

    Async Sub M()
        ' await 1
        Await t1

    End Sub

    Sub M2()
        ' await 2
        Await t1

        Dim lambda = Async Sub() If True Then Await t1 : Await t2  ' await 3 and 4

    End Sub

    Async Function N() As Task(Of Integer)
        ' await 5
        Await t1
        ' await 6
        Return Await t2
        
    End Function

    Function N2()

        Await t1

        Return (Await t2)

    End Function

    Async Function N3(a, x, y) As Task

        ' This is a parse error, end of statement expected after 'Await a' 
        Await a * Await x * Await y

    End Function    

End Module]]>,
<errors>
    <error id="30800" message="Method arguments must be enclosed in parentheses." start="177" end="179"/>
    <error id="30800" message="Method arguments must be enclosed in parentheses." start="407" end="409"/>
    <error id="30198" message="')' expected." start="433" end="433"/>
    <error id="30205" message="End of statement expected." start="588" end="589"/>
</errors>)

        Dim awaitExpressions = tree.GetRoot().DescendantNodes.OfType(Of AwaitExpressionSyntax)().ToArray()

        Assert.Equal(6, awaitExpressions.Count)

        Dim firstStatementOfM = tree.GetRoot().DescendantNodes.OfType(Of MethodBlockBaseSyntax).First.Statements.First

        Assert.Equal(SyntaxKind.ExpressionStatement, firstStatementOfM.Kind)
        Assert.Equal(SyntaxKind.AwaitExpression, CType(firstStatementOfM, ExpressionStatementSyntax).Expression.Kind)

        Dim firstStatementOfM2 = tree.GetRoot().DescendantNodes.OfType(Of MethodBlockBaseSyntax)()(1).Statements.First

        Assert.Equal(SyntaxKind.ExpressionStatement, firstStatementOfM2.Kind)
        Assert.Equal(SyntaxKind.InvocationExpression, CType(firstStatementOfM2, ExpressionStatementSyntax).Expression.Kind)

        Dim firstStatementOfN = tree.GetRoot().DescendantNodes.OfType(Of MethodBlockBaseSyntax)()(2).Statements.First

        Assert.Equal(SyntaxKind.ExpressionStatement, firstStatementOfM.Kind)
        Assert.Equal(SyntaxKind.AwaitExpression, CType(firstStatementOfM, ExpressionStatementSyntax).Expression.Kind)

        Dim firstStatementOfN2 = tree.GetRoot().DescendantNodes.OfType(Of MethodBlockBaseSyntax)()(3).Statements.First

        Assert.Equal(SyntaxKind.ExpressionStatement, firstStatementOfM2.Kind)
        Assert.Equal(SyntaxKind.InvocationExpression, CType(firstStatementOfM2, ExpressionStatementSyntax).Expression.Kind)

    End Sub

    <Fact>
    Public Sub ParseAsyncLambdas()
        Dim tree = ParseAndVerify(<![CDATA[
Module Program

    Private t1 As Task
    Private t2 As Task(Of Integer)
    Private f As Integer

    Sub Main()

        ' Statement
        Dim slAsyncSub1 = Async Sub() Await t1

        ' Assignment
        Dim slAsyncSub2 = Async Sub() f = Await t2

        ' Expression
        Dim slAsyncFunction1 = Async Function() Await t2

        ' Comparison
        Dim slAsyncFunction2 = Async Function() f = Await t2

        Dim mlAsyncSub = Async Sub()
                            Await t1
                         End Sub

        Dim mlAsyncFunction1 = Async Function()
                                   Await t1
                               End Function

        Dim mlAsyncFunction2 = Async Function()
                                   Await t1

                                   Return Await t2
                               End Function

    End Sub
End Module]]>)

        Dim lambdas = tree.GetRoot().DescendantNodes.OfType(Of LambdaExpressionSyntax)().ToArray()

        Assert.Equal(7, lambdas.Count)

        Assert.Equal(4, lambdas.Count(Function(l) SyntaxFacts.IsSingleLineLambdaExpression(l.Kind)))

        Assert.Equal(SyntaxKind.ExpressionStatement, CType(lambdas(0), SingleLineLambdaExpressionSyntax).Body.Kind)
        Assert.Equal(SyntaxKind.AwaitExpression, CType(CType(lambdas(0), SingleLineLambdaExpressionSyntax).Body, ExpressionStatementSyntax).Expression.Kind)

        Assert.Equal(SyntaxKind.SimpleAssignmentStatement, CType(lambdas(1), SingleLineLambdaExpressionSyntax).Body.Kind)
        Assert.Equal(SyntaxKind.AwaitExpression, CType(CType(lambdas(1), SingleLineLambdaExpressionSyntax).Body, AssignmentStatementSyntax).Right.Kind)

        Assert.Equal(SyntaxKind.AwaitExpression, CType(lambdas(2), SingleLineLambdaExpressionSyntax).Body.Kind)

        Assert.Equal(SyntaxKind.EqualsExpression, CType(lambdas(3), SingleLineLambdaExpressionSyntax).Body.Kind)
        Assert.Equal(SyntaxKind.AwaitExpression, CType(CType(lambdas(3), SingleLineLambdaExpressionSyntax).Body, BinaryExpressionSyntax).Right.Kind)

        Assert.Equal(3, lambdas.Count(Function(l) SyntaxFacts.IsMultiLineLambdaExpression(l.Kind)))

    End Sub

    <Fact>
    Public Sub ParseAsyncWithNesting()
        Dim tree = VisualBasicSyntaxTree.ParseText(<![CDATA[
Imports Async = System.Threading.Tasks.Task

Class C
    Public Const Async As Integer = 0

    <Async(Async)>
    Async Function M() As Async

        Dim t As Task

        Await (t) ' Yes

        Dim lambda1 = Function() Await (t) ' No

        Dim lambda1a = Sub() Await (t) ' No

        Dim lambda1b = Async Function() Await t ' Yes

        Dim lambda1c = Async Function()
                            Await (Sub()
                                       Await (Function() Await (Function() (Function() Async Sub() Await t)())())                        
                                   End Sub) ' Yes, No, No, Yes

                            Return Await t ' Yes
                       End Function

        [Await] t ' No
    End Function

    Sub Await(Optional p = Nothing)

        Dim t As Task

        Await (t) ' No

        Dim lambda1 = Async Function() Await (t) ' Yes

        Dim lambda1a = Async Sub() Await (t) ' Yes

        Dim lambda1b = Function() Await (t) ' No

        Dim lambda1c = Function()
                            Await (Async Sub()
                                       Await (Async Function() Await (Async Function() (Function() Sub() Await t)())())                        
                                   End Sub) ' No, Yes, Yes, No

                            Return Await (t) ' No
                       End Function

        Await t
    End Sub

    Function Await(t)
        Return Nothing
    End Function
End Class

Class AsyncAttribute
    Inherits Attribute

    Sub New(p)

    End Sub
End Class]]>.Value)

        ' Error BC30800: Method arguments must be enclosed in parentheses.
        Assert.True(Aggregate d In tree.GetDiagnostics() Into All(d.Code = ERRID.ERR_ObsoleteArgumentsNeedParens))

        Dim asyncExpressions = tree.GetRoot().DescendantNodes.OfType(Of AwaitExpressionSyntax).ToArray()

        Assert.Equal(9, asyncExpressions.Count)

        Dim invocationExpression = tree.GetRoot().DescendantNodes.OfType(Of InvocationExpressionSyntax).ToArray()

        Assert.Equal(9, asyncExpressions.Count)

        Dim allParsedExpressions = tree.GetRoot().DescendantNodes.OfType(Of ExpressionSyntax)()
        Dim parsedExpressions = From expression In allParsedExpressions
                                Where expression.Kind = SyntaxKind.AwaitExpression OrElse
                                                (expression.Kind = SyntaxKind.IdentifierName AndAlso DirectCast(expression, IdentifierNameSyntax).Identifier.ValueText.Equals("Await"))
                                Order By expression.Position
                                Select expression.Kind

        Dim expected = {SyntaxKind.AwaitExpression,
                        SyntaxKind.IdentifierName,
                        SyntaxKind.IdentifierName,
                        SyntaxKind.AwaitExpression,
                        SyntaxKind.AwaitExpression,
                        SyntaxKind.IdentifierName,
                        SyntaxKind.IdentifierName,
                        SyntaxKind.AwaitExpression,
                        SyntaxKind.AwaitExpression,
                        SyntaxKind.IdentifierName,
                        SyntaxKind.IdentifierName,
                        SyntaxKind.AwaitExpression,
                        SyntaxKind.AwaitExpression,
                        SyntaxKind.IdentifierName,
                        SyntaxKind.IdentifierName,
                        SyntaxKind.AwaitExpression,
                        SyntaxKind.AwaitExpression,
                        SyntaxKind.IdentifierName,
                        SyntaxKind.IdentifierName,
                        SyntaxKind.IdentifierName}

        Assert.Equal(expected, parsedExpressions)

    End Sub

    <Fact>
    Public Sub ParseAwaitInScriptingAndInteractive()

        Dim source = "
Dim i = Await T + Await(T)      ' Yes, Yes

Dim l = Sub()
            Await T             ' No
        End Sub

Function M()
    Return Await T              ' No
End Function

Async Sub N()
    Await T                     ' Yes
    Await(T)                    ' Yes
End Sub

Async Function F()
    Return Await(T)             ' Yes
End Function"

        Dim tree = VisualBasicSyntaxTree.ParseText(source, options:=TestOptions.Script)

        Dim awaitExpressions = tree.GetRoot().DescendantNodes.OfType(Of AwaitExpressionSyntax).ToArray()

        Assert.Equal(5, awaitExpressions.Count)

        Dim awaitParsedAsIdentifier = tree.GetRoot().DescendantNodes.OfType(Of IdentifierNameSyntax).Where(Function(id) id.Identifier.ValueText.Equals("Await")).ToArray()

        Assert.Equal(2, awaitParsedAsIdentifier.Count)
    End Sub
End Class
