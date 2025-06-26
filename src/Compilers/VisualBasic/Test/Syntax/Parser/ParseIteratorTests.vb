' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Public Class ParseIteratorTests
    Inherits BasicTestBase

    <Fact>
    Public Sub ParseIteratorModifier()
        Dim tree = ParseAndVerify(<![CDATA[
Imports Iterator = System.Collections.Generic.IEnumerable(Of Integer)

Module Program
    Public Const Iterator As Integer = 0

    <Iterator(Iterator)>
    Iterator Function M() As Iterator

        Dim l = Function()
                    Dim iterator As Iterator = Nothing
                    Return iterator
                End Function

        Dim l2 = Iterator Function()
                     Dim iterator As Iterator = Nothing
                     Yield iterator
                 End Function
    End Function

    <Iterator(Iterator)>
    Iterator Property P1() As Iterator
        Get
            Dim l = Function()
                        Dim iterator As Iterator = Nothing
                        Return iterator
                    End Function

            Dim l2 = Iterator Function()
                         Dim iterator As Iterator = Nothing
                         Yield iterator
                     End Function
        End Get
        Set(value As Iterator)

        End Set
    End Property

    <Iterator(Iterator)>
    ReadOnly Iterator Property P2() As Iterator
        Get
            Dim l = Function()
                        Dim iterator As Iterator = Nothing
                        Return iterator
                    End Function

            Dim l2 = Iterator Function()
                         Dim iterator As Iterator = Nothing
                         Yield iterator
                     End Function
        End Get
    End Property

End Module

Class IteratorAttribute
    Inherits Attribute

    Sub New(p)
    End Sub
End Class]]>)

        Assert.Equal(6, Aggregate t In tree.GetRoot().DescendantTokens Where t.Kind = SyntaxKind.IteratorKeyword Into Count())

    End Sub

    <Fact>
    Public Sub ParseYieldStatements()
        Dim tree = VisualBasicSyntaxTree.ParseText(<![CDATA[
Module Program

    Iterator Function M()
        Yield (1)
    End Function

    Function M2()
        Yield 1
    End Function

    Iterator Property P1() As IEnumerable(Of Func(Of IEnumerable(Of Integer)))
        Get
            Yield Function()
                      Yield {0}
                  End Function

            Yield Iterator Function()
                      Yield 1
                  End Function
        End Get
        Set(value As IEnumerable(Of Func(Of IEnumerable(Of Integer))))
            ' This will be a semantic error.
            Yield -1
        End Set
    End Property

    Property P2 As IEnumerable(Of Func(Of IEnumerable(Of Integer)))
        Get
            Yield Function()
                      Yield {0}
                  End Function

            Yield Iterator Function()
                      Yield 1
                  End Function
        End Get
        Set(value As IEnumerable(Of Func(Of IEnumerable(Of Integer))))
            Yield -1
        End Set
    End Property

    ReadOnly Iterator Property P3 As IEnumerable(Of Func(Of IEnumerable(Of Integer)))
        Get
            Yield Function()
                      Return Yield({0})
                  End Function

            Yield Iterator Function()
                      Yield 1
                  End Function
        End Get
    End Property

    ReadOnly Property P4 As IEnumerable(Of Func(Of IEnumerable(Of Integer)))
        Get
            Yield Function()
                      Yield({0})
                  End Function

            Yield Iterator Function()
                      Yield 1
                  End Function
        End Get
    End Property

End Module]]>.Value)

        Dim yieldStatements = tree.GetRoot().DescendantNodes.OfType(Of YieldStatementSyntax)().ToArray()

        Assert.Equal(10, yieldStatements.Count)

        Dim methodSyntaxList = tree.GetRoot().DescendantNodes.OfType(Of MethodBlockBaseSyntax)().ToArray()

        ' Iterator Function M()
        Dim firstStatementOfM = methodSyntaxList(0).Statements.First
        Assert.Equal(SyntaxKind.YieldStatement, firstStatementOfM.Kind)     ' 1

        ' Function M2()
        yieldStatements = methodSyntaxList(1).DescendantNodes.OfType(Of YieldStatementSyntax)().ToArray()
        Assert.Equal(0, yieldStatements.Count)

        ' Iterator Property P1() As IEnumerable(Of Func(Of IEnumerable(Of Integer)))
        ' Getter
        Dim statements = methodSyntaxList(2).Statements
        Assert.Equal(SyntaxKind.YieldStatement, statements(0).Kind)     ' 2
        yieldStatements = statements(0).DescendantNodes.OfType(Of YieldStatementSyntax)().ToArray()
        Assert.Equal(0, yieldStatements.Count)
        Assert.Equal(SyntaxKind.YieldStatement, statements(1).Kind)     ' 3
        yieldStatements = statements(1).DescendantNodes.OfType(Of YieldStatementSyntax)().ToArray()
        Assert.Equal(1, yieldStatements.Count)      ' 4
        ' Setter
        statements = methodSyntaxList(3).Statements
        Assert.Equal(SyntaxKind.YieldStatement, statements(0).Kind)     ' 5

        ' Property P2 As IEnumerable(Of Func(Of IEnumerable(Of Integer)))
        ' Getter
        statements = methodSyntaxList(4).Statements
        Assert.NotEqual(SyntaxKind.YieldStatement, statements(0).Kind)
        yieldStatements = statements(0).DescendantNodes.OfType(Of YieldStatementSyntax)().ToArray()
        Assert.Equal(0, yieldStatements.Count)
        Assert.NotEqual(SyntaxKind.YieldStatement, statements(1).Kind)
        yieldStatements = statements(1).DescendantNodes.OfType(Of YieldStatementSyntax)().ToArray()
        Assert.Equal(1, yieldStatements.Count)      ' 6
        ' Setter
        statements = methodSyntaxList(5).Statements
        Assert.NotEqual(SyntaxKind.YieldStatement, statements(0).Kind)

        ' ReadOnly Iterator Property P3 As IEnumerable(Of Func(Of IEnumerable(Of Integer)))
        ' Getter
        statements = methodSyntaxList(6).Statements
        Assert.Equal(SyntaxKind.YieldStatement, statements(0).Kind)     ' 7
        yieldStatements = statements(0).DescendantNodes.OfType(Of YieldStatementSyntax)().ToArray()
        Assert.Equal(0, yieldStatements.Count)
        Assert.Equal(SyntaxKind.YieldStatement, statements(1).Kind)     ' 8
        yieldStatements = statements(1).DescendantNodes.OfType(Of YieldStatementSyntax)().ToArray()
        Assert.Equal(1, yieldStatements.Count)      ' 9

        ' ReadOnly Property P4 As IEnumerable(Of Func(Of IEnumerable(Of Integer)))
        ' Getter
        statements = methodSyntaxList(7).Statements
        Assert.NotEqual(SyntaxKind.YieldStatement, statements(0).Kind)
        yieldStatements = statements(0).DescendantNodes.OfType(Of YieldStatementSyntax)().ToArray()
        Assert.Equal(0, yieldStatements.Count)
        Assert.NotEqual(SyntaxKind.YieldStatement, statements(1).Kind)
        yieldStatements = statements(1).DescendantNodes.OfType(Of YieldStatementSyntax)().ToArray()
        Assert.Equal(1, yieldStatements.Count)  ' 10
    End Sub

    <Fact>
    Public Sub ParseYieldStatementsWithPrecedence()
        Dim tree = VisualBasicSyntaxTree.ParseText(<![CDATA[
Module Program
    Iterator Function M(a As Task(Of Integer), x As Task(Of Integer), y As Task(Of Integer), b As Task(Of Integer)) As Task(Of Integer)

        Yield a * Yield x ^ Yield y + Yield b
        
    End Function
End Module]]>.Value)

        Dim yieldStatements = tree.GetRoot().DescendantNodes.OfType(Of YieldStatementSyntax)().ToArray()

        Assert.Equal(1, yieldStatements.Count)

    End Sub

    <Fact>
    Public Sub ParseIteratorLambdas()
        Dim tree = ParseAndVerify(<![CDATA[
Module Program

    Private t1 As Task
    Private t2 As Task(Of Integer)
    Private f As Integer

    Sub Main()

        ' Iterator Sub lambdas make no sense and we'll need to catch this at binding time but
        ' both Iterator and Yield will parse correctly in this case.
        Dim slIteratorSub1 = Iterator Sub() Yield t1

        ' Again, the binder will need to report this as an error but Iterator and Yield will parse here.
        Dim mlIteratorSub = Iterator Sub()
                                Yield t1
                            End Sub

        Dim mlIteratorFunction = Iterator Function()
                                     Yield t1
                                 End Function

    End Sub
End Module]]>)

        Dim lambdas = tree.GetRoot().DescendantNodes.OfType(Of LambdaExpressionSyntax)().ToArray()

        Assert.Equal(3, lambdas.Count)
        Assert.Equal(1, lambdas.Count(Function(l) SyntaxFacts.IsSingleLineLambdaExpression(l.Kind)))
        Assert.Equal(2, lambdas.Count(Function(l) SyntaxFacts.IsMultiLineLambdaExpression(l.Kind)))

        ' Dim slIteratorSub1 = Iterator Sub() Yield t1
        Assert.Equal(SyntaxKind.YieldStatement, CType(lambdas(0), SingleLineLambdaExpressionSyntax).Body.Kind)

        Dim yieldStatements = lambdas(1).DescendantNodes.OfType(Of YieldStatementSyntax).ToArray()
        Assert.Equal(1, yieldStatements.Count)

        yieldStatements = lambdas(2).DescendantNodes.OfType(Of YieldStatementSyntax).ToArray()
        Assert.Equal(1, yieldStatements.Count)
    End Sub

    <Fact>
    Public Sub ParseIteratorWithNesting()
        Dim tree = VisualBasicSyntaxTree.ParseText(<![CDATA[
Imports Iterator = System.Threading.Tasks.Task

Class C
    Public Const Iterator As Integer = 0

    <Iterator(Iterator)>
    Iterator Function M() As Iterator

        Dim t As Task

        Yield (t) ' Yes

        Dim lambda1 = Function() Yield(t) ' No

        Dim lambda1a = Sub() Yield(t) ' No

        Dim lambda1b = Iterator Function() Yield t ' No

        Dim lambda1c = Iterator Function()
                           Yield (Sub()
                                      Yield(Function() Yield(Function() (Function() Iterator Sub() Yield t)())())
                                  End Sub) ' Yes, No, No, Yes

                            Return Yield t ' No
                       End Function

        [Yield] t ' No
    End Function

    Sub Yield(Optional p = Nothing)

        Dim t As Task

        Yield(t) ' No

        Dim lambda1 = Iterator Function() Yield (t) ' No

        Dim lambda1a = Iterator Sub() Yield (t) ' Yes

        Dim lambda1b = Function() Yield t ' No

        Dim lambda1c = Function()
                            Yield (Iterator Sub()
                                       Yield (Iterator Function() Yield (Iterator Function() (Function() Sub() Yield t)())())                        
                                   End Sub) ' No, Yes, No, No

                            Return Yield t ' No
                       End Function

        Yield t ' No
    End Sub

    Function Yield(t)
        Return Nothing
    End Function
End Class

Class IteratorAttribute
    Inherits Attribute

    Sub New(p)

    End Sub
End Class]]>.Value)

        Dim expected = {SyntaxKind.YieldStatement,
                        SyntaxKind.IdentifierName,
                        SyntaxKind.IdentifierName,
                        SyntaxKind.IdentifierName,
                        SyntaxKind.YieldStatement,
                        SyntaxKind.IdentifierName,
                        SyntaxKind.IdentifierName,
                        SyntaxKind.YieldStatement,
                        SyntaxKind.IdentifierName,
                        SyntaxKind.IdentifierName,
                        SyntaxKind.IdentifierName,
                        SyntaxKind.IdentifierName,
                        SyntaxKind.YieldStatement,
                        SyntaxKind.IdentifierName,
                        SyntaxKind.IdentifierName,
                        SyntaxKind.YieldStatement,
                        SyntaxKind.IdentifierName,
                        SyntaxKind.IdentifierName,
                        SyntaxKind.IdentifierName,
                        SyntaxKind.IdentifierName}

        Dim actual = From expression In tree.GetRoot().DescendantNodes()
                     Where expression.Kind = SyntaxKind.YieldStatement OrElse
                            (expression.Kind = SyntaxKind.IdentifierName AndAlso DirectCast(expression, IdentifierNameSyntax).Identifier.ValueText.Equals("Yield"))
                     Order By expression.FullSpan.Start
                     Select expression.Kind()

        Assert.Equal(expected, actual)
    End Sub

    <Fact>
    Public Sub ParseYieldInScriptingAndInteractive()

        Dim source = "
Yield T                                     ' No
Yield (T)                                   ' No
Yield T + Yield (T)                         ' No, No
Dim i = Yield T + Yield (T)                 ' No, No

Dim l = Sub()
            Yield T                         ' No
            Yield (T)                       ' No
        End Sub

Function M()
    Return Yield T                          ' No
    Yield T                                 ' No
    Yield (T)                               ' No
End Function

Iterator Sub N()
    Yield T                                 ' Yes
    Yield (T)                               ' Yes
    Yield T + Yield (T)                     ' Yes, No
    Dim i = Yield T + Yield (T)             ' No, No
    Return Yield T                          ' No
End Sub

Iterator Function F()
    Yield T                                 ' Yes
    Yield (T)                               ' Yes
    Yield T + Yield (T)                     ' Yes, No
    Dim i = Yield T + Yield (T)             ' No, No
    Return Yield T                          ' No
End Function"

        Dim tree = VisualBasicSyntaxTree.ParseText(source, options:=TestOptions.Script)

        Dim yieldStatements = tree.GetRoot().DescendantNodes.OfType(Of YieldStatementSyntax).ToArray()

        Assert.Equal(6, yieldStatements.Count)

        For Each yieldStatement In yieldStatements
            Assert.True(IsInIteratorMethod(yieldStatement))
        Next
    End Sub

    Private Shared Function IsIteratorMethod(methodSyntax As MethodBlockBaseSyntax) As Boolean
        Return methodSyntax.BlockStatement.Modifiers.Contains(Function(t As SyntaxToken) t.Kind = SyntaxKind.IteratorKeyword)
    End Function

    Private Shared Function IsInIteratorMethod(yieldStatement As YieldStatementSyntax) As Boolean
        Return IsIteratorMethod(DirectCast(yieldStatement.Parent, MethodBlockBaseSyntax))
    End Function
End Class
