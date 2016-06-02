' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.UnitTests.Diagnostics.SystemLanguage
Imports Microsoft.CodeAnalysis.Semantics
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Public Class IOperationTests
        Inherits BasicTestBase

        <Fact>
        Public Sub InvalidUserDefinedOperators()
            Dim source = <compilation>
                             <file name="c.vb">
                                 <![CDATA[
Public Class B2
    Public Shared Operator +(x As B2, y As B2) As B2 
        System.Console.WriteLine("+")
        Return x
    End Operator

    Public Shared Operator -(x As B2) As B2 
        System.Console.WriteLine("-")
        Return x
    End Operator

    Public Shared Operator -(x As B2) As B2 
        System.Console.WriteLine("-")
        Return x
    End Operator
End Class

Module Module1
    Sub Main() 
        Dim x, y As New B2()
        x = x + 10
        x = x + y
        x = -x
    End Sub
End Module
]]>
                             </file>
                         </compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            Dim tree = comp.SyntaxTrees.Single()
            Dim model = comp.GetSemanticModel(tree)
            Dim nodes = tree.GetRoot().DescendantNodes().OfType(Of AssignmentStatementSyntax).ToArray()
            Assert.Equal(nodes.Length, 3)

            ' x = x + 10 fails semantic analysis and does not have an operator method, but the operands are available.

            Assert.Equal("x = x + 10", nodes(0).ToString())
            Dim statement1 As IOperation = model.GetOperation(nodes(0))
            Assert.Equal(statement1.Kind, OperationKind.ExpressionStatement)
            Dim expression1 As IOperation = DirectCast(statement1, IExpressionStatement).Expression
            Assert.Equal(expression1.Kind, OperationKind.AssignmentExpression)
            Dim assignment1 As IAssignmentExpression = DirectCast(expression1, IAssignmentExpression)
            Assert.Equal(assignment1.Value.Kind, OperationKind.BinaryOperatorExpression)
            Dim add1 As IBinaryOperatorExpression = DirectCast(assignment1.Value, IBinaryOperatorExpression)
            Assert.Equal(add1.BinaryOperationKind, BinaryOperationKind.OperatorMethodAdd)
            Assert.False(add1.UsesOperatorMethod)
            Assert.Null(add1.OperatorMethod)
            Dim left1 As IOperation = add1.LeftOperand
            Assert.Equal(left1.Kind, OperationKind.LocalReferenceExpression)
            Assert.Equal(DirectCast(left1, ILocalReferenceExpression).Local.Name, "x")
            Dim right1 As IOperation = add1.RightOperand
            Assert.Equal(right1.Kind, OperationKind.LiteralExpression)
            Dim literal1 As ILiteralExpression = DirectCast(right1, ILiteralExpression)
            Assert.Equal(CInt(literal1.ConstantValue.Value), 10)

            ' x = x + y passes semantic analysis.

            Assert.Equal("x = x + y", nodes(1).ToString())
            Dim statement2 As IOperation = model.GetOperation(nodes(1))
            Assert.Equal(statement2.Kind, OperationKind.ExpressionStatement)
            Dim expression2 As IOperation = DirectCast(statement2, IExpressionStatement).Expression
            Assert.Equal(expression2.Kind, OperationKind.AssignmentExpression)
            Dim assignment2 As IAssignmentExpression = DirectCast(expression2, IAssignmentExpression)
            Assert.Equal(assignment2.Value.Kind, OperationKind.BinaryOperatorExpression)
            Dim add2 As IBinaryOperatorExpression = DirectCast(assignment2.Value, IBinaryOperatorExpression)
            Assert.Equal(add2.BinaryOperationKind, BinaryOperationKind.OperatorMethodAdd)
            Assert.True(add2.UsesOperatorMethod)
            Assert.NotNull(add2.OperatorMethod)
            Assert.Equal(add2.OperatorMethod.Name, "op_Addition")
            Dim left2 As IOperation = add2.LeftOperand
            Assert.Equal(left2.Kind, OperationKind.LocalReferenceExpression)
            Assert.Equal(DirectCast(left2, ILocalReferenceExpression).Local.Name, "x")
            Dim right2 As IOperation = add2.RightOperand
            Assert.Equal(right2.Kind, OperationKind.LocalReferenceExpression)
            Assert.Equal(DirectCast(right2, ILocalReferenceExpression).Local.Name, "y")

            ' -x fails semantic analysis and does not have an operator method, but the operand is available.

            Assert.Equal("x = -x", nodes(2).ToString())
            Dim statement3 As IOperation = model.GetOperation(nodes(2))
            Assert.Equal(statement3.Kind, OperationKind.ExpressionStatement)
            Dim expression3 As IOperation = DirectCast(statement3, IExpressionStatement).Expression
            Assert.Equal(expression3.Kind, OperationKind.AssignmentExpression)
            Dim assignment3 As IAssignmentExpression = DirectCast(expression3, IAssignmentExpression)
            Assert.Equal(assignment3.Value.Kind, OperationKind.UnaryOperatorExpression)
            Dim negate3 As IUnaryOperatorExpression = DirectCast(assignment3.Value, IUnaryOperatorExpression)
            Assert.Equal(negate3.UnaryOperationKind, UnaryOperationKind.OperatorMethodMinus)
            Assert.False(negate3.UsesOperatorMethod)
            Assert.Null(negate3.OperatorMethod)
            Dim operand3 As IOperation = negate3.Operand
            Assert.Equal(operand3.Kind, OperationKind.LocalReferenceExpression)
            Assert.Equal(DirectCast(operand3, ILocalReferenceExpression).Local.Name, "x")
        End Sub

        <Fact>
        Public Sub SimpleCompoundAssignment()
            Dim source = <compilation>
                             <file name="c.vb">
                                 <![CDATA[
Public Class B2
    Public Shared Operator +(x As B2, y As B2) As B2 
        System.Console.WriteLine("+")
        Return x
    End Operator
End Class

Module Module1
    Sub Main()
        Dim x, y As Integer 
        Dim a, b As New B2()
        x += y
        a += b
    End Sub
End Module
]]>
                             </file>
                         </compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            Dim tree = comp.SyntaxTrees.Single()
            Dim model = comp.GetSemanticModel(tree)
            Dim nodes = tree.GetRoot().DescendantNodes().OfType(Of AssignmentStatementSyntax).ToArray()
            Assert.Equal(nodes.Length, 2)

            ' x += y produces a compound assignment with an integer add.

            Assert.Equal("x += y", nodes(0).ToString())
            Dim statement1 As IOperation = model.GetOperation(nodes(0))
            Assert.Equal(statement1.Kind, OperationKind.ExpressionStatement)
            Dim expression1 As IOperation = DirectCast(statement1, IExpressionStatement).Expression
            Assert.Equal(expression1.Kind, OperationKind.CompoundAssignmentExpression)
            Dim assignment1 As ICompoundAssignmentExpression = DirectCast(expression1, ICompoundAssignmentExpression)
            Dim target1 As ILocalReferenceExpression = TryCast(assignment1.Target, ILocalReferenceExpression)
            Assert.NotNull(target1)
            Assert.Equal(target1.Local.Name, "x")
            Dim value1 As ILocalReferenceExpression = TryCast(assignment1.Value, ILocalReferenceExpression)
            Assert.NotNull(value1)
            Assert.Equal(value1.Local.Name, "y")
            Assert.Equal(assignment1.BinaryOperationKind, BinaryOperationKind.IntegerAdd)
            Assert.False(assignment1.UsesOperatorMethod)
            Assert.Null(assignment1.OperatorMethod)

            ' a += b produces a compound assignment with an operator method add.

            Assert.Equal("a += b", nodes(1).ToString())
            Dim statement2 As IOperation = model.GetOperation(nodes(1))
            Assert.Equal(statement2.Kind, OperationKind.ExpressionStatement)
            Dim expression2 As IOperation = DirectCast(statement2, IExpressionStatement).Expression
            Assert.Equal(expression2.Kind, OperationKind.CompoundAssignmentExpression)
            Dim assignment2 As ICompoundAssignmentExpression = DirectCast(expression2, ICompoundAssignmentExpression)
            Dim target2 As ILocalReferenceExpression = TryCast(assignment2.Target, ILocalReferenceExpression)
            Assert.NotNull(target2)
            Assert.Equal(target2.Local.Name, "a")
            Dim value2 As ILocalReferenceExpression = TryCast(assignment2.Value, ILocalReferenceExpression)
            Assert.NotNull(value2)
            Assert.Equal(value2.Local.Name, "b")
            Assert.Equal(assignment2.BinaryOperationKind, BinaryOperationKind.OperatorMethodAdd)
            Assert.True(assignment2.UsesOperatorMethod)
            Assert.NotNull(assignment2.OperatorMethod)
            Assert.Equal(assignment2.OperatorMethod.Name, "op_Addition")
        End Sub
    End Class
End Namespace
