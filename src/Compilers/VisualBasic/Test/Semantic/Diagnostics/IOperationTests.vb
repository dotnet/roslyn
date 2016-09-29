' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Semantics
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

            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source, parseOptions:=TestOptions.RegularWithIOperationFeature)
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

            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source, parseOptions:=TestOptions.RegularWithIOperationFeature)
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

            comp.VerifyOperationTree(nodes(0), expectedOperationTree:="
IExpressionStatement (OperationKind.ExpressionStatement)
  ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Int32)
    Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32)
    Right: ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32)
")

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

            CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source, parseOptions:=TestOptions.RegularWithIOperationFeature) _
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

            CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source, parseOptions:=TestOptions.RegularWithIOperationFeature) _
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
