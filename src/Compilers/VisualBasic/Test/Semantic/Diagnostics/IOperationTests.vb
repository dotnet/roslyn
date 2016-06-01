' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

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
            comp.VerifyOperationTree(nodes(0), expectedOperationTree:="
IExpressionStatement (OperationKind.ExpressionStatement, IsInvalid)
  IAssignmentExpression (OperationKind.AssignmentExpression, Type: B2, IsInvalid)
    Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: B2)
    Right: IBinaryOperatorExpression (BinaryOperationKind.OperatorMethodAdd) (OperationKind.BinaryOperatorExpression, Type: B2, IsInvalid)
        Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: B2)
        Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10)
")

            ' x = x + y passes semantic analysis.
            Assert.Equal("x = x + y", nodes(1).ToString())
            comp.VerifyOperationTree(nodes(1), expectedOperationTree:="
IExpressionStatement (OperationKind.ExpressionStatement)
  IAssignmentExpression (OperationKind.AssignmentExpression, Type: B2)
    Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: B2)
    Right: IBinaryOperatorExpression (BinaryOperationKind.OperatorMethodAdd) (OperatorMethod: Function B2.op_Addition(x As B2, y As B2) As B2) (OperationKind.BinaryOperatorExpression, Type: B2)
        Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: B2)
        Right: ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: B2)
")

            ' -x fails semantic analysis and does not have an operator method, but the operand is available.
            Assert.Equal("x = -x", nodes(2).ToString())
            comp.VerifyOperationTree(nodes(2), expectedOperationTree:="
IExpressionStatement (OperationKind.ExpressionStatement, IsInvalid)
  IAssignmentExpression (OperationKind.AssignmentExpression, Type: B2, IsInvalid)
    Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: B2)
    Right: IUnaryOperatorExpression (UnaryOperationKind.OperatorMethodMinus) (OperationKind.UnaryOperatorExpression, Type: B2, IsInvalid)
        ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: B2)
")
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
            comp.VerifyOperationTree(nodes(0), expectedOperationTree:="
IExpressionStatement (OperationKind.ExpressionStatement)
  ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Int32)
    Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32)
    Right: ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32)
")

            ' a += b produces a compound assignment with an operator method add.
            Assert.Equal("a += b", nodes(1).ToString())
            comp.VerifyOperationTree(nodes(1), expectedOperationTree:="
IExpressionStatement (OperationKind.ExpressionStatement)
  ICompoundAssignmentExpression (BinaryOperationKind.OperatorMethodAdd) (OperatorMethod: Function B2.op_Addition(x As B2, y As B2) As B2) (OperationKind.CompoundAssignmentExpression, Type: B2)
    Left: ILocalReferenceExpression: a (OperationKind.LocalReferenceExpression, Type: B2)
    Right: ILocalReferenceExpression: b (OperationKind.LocalReferenceExpression, Type: B2)
")
        End Sub

        <Fact>
        Public Sub VerifyOperationTree_IfStatement()
            Dim source = <compilation>
                             <file name="c.vb">
                                 <![CDATA[
Class C
    Sub Foo(x as Integer)
        If x <> 0
          System.Console.Write(x)
        End If
    End Sub
End Class
]]>
                             </file>
                         </compilation>

            CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source) _
                .VerifyOperationTree("Foo", "
Sub C.Foo(x As System.Int32)
  IIfStatement (OperationKind.IfStatement)
    Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerNotEquals) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
        Left: IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32)
        Right: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
    IBlockStatement (1 statements) (OperationKind.BlockStatement)
      IExpressionStatement (OperationKind.ExpressionStatement)
        IInvocationExpression (static Sub System.Console.Write(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void)
          IArgument (Matching Parameter: value) (OperationKind.Argument)
            IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32)
")
        End Sub

        <Fact>
        Public Sub VerifyOperationTree_ForStatement()

            Dim source = <compilation>
                             <file name="c.vb">
                                 <![CDATA[
Class C
    Sub Foo()
        For i = 0 To 10
            System.Console.Write(i)
        Next
    End Sub
End Class
]]>
                             </file>
                         </compilation>

            CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source) _
                .VerifyOperationTree("Foo", "
Sub C.Foo()
  IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement)
    Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
        Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10)
    Before: IExpressionStatement (OperationKind.ExpressionStatement)
        IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
          Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
          Right: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
    AtLoopBottom: IExpressionStatement (OperationKind.ExpressionStatement)
        ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Int32)
          Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
          Right: IConversionExpression (ConversionKind.Basic, Explicit) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 1)
              ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
    IBlockStatement (1 statements) (OperationKind.BlockStatement)
      IExpressionStatement (OperationKind.ExpressionStatement)
        IInvocationExpression (static Sub System.Console.Write(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void)
          IArgument (Matching Parameter: value) (OperationKind.Argument)
            ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
")
        End Sub
    End Class
End Namespace
