' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase

        <Fact>
        Public Sub IBlockStatement_SubMethodBlock()
            Dim source = "
Class Program
    Sub Method'BIND:""Sub Method""
        If 1 > 2
        End If
    End Sub
End Class"

            Dim expectedOperationTree = "
IBlockStatement (3 statements) (OperationKind.BlockStatement)
  IIfStatement (OperationKind.IfStatement)
    Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean, Constant: False)
        Left: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
        Right: ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2)
    IBlockStatement (0 statements) (OperationKind.BlockStatement)
  ILabelStatement (Label: exit) (OperationKind.LabelStatement)
  IReturnStatement (OperationKind.ReturnStatement)"

            VerifyOperationTreeForTest(Of MethodBlockBaseSyntax)(source, expectedOperationTree)
        End Sub

        <Fact>
        Public Sub IBlockStatement_SubNewBlock()
            Dim source = "
Class Program
    Sub New'BIND:""Sub New""
        If 1 > 2
        End If
    End Sub
End Class"

            Dim expectedOperationTree = "
IBlockStatement (3 statements) (OperationKind.BlockStatement)
  IIfStatement (OperationKind.IfStatement)
    Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean, Constant: False)
        Left: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
        Right: ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2)
    IBlockStatement (0 statements) (OperationKind.BlockStatement)
  ILabelStatement (Label: exit) (OperationKind.LabelStatement)
  IReturnStatement (OperationKind.ReturnStatement)"

            VerifyOperationTreeForTest(Of MethodBlockBaseSyntax)(source, expectedOperationTree)
        End Sub

        <Fact>
        Public Sub IBlockStatement_FunctionMethodBlock()
            Dim source = "
Class Program
    Function Method() As Boolean 'BIND:""Function Method() As Boolean""
        If 1 > 2
        End If

        Return True
    End Sub
End Class"

            Dim expectedOperationTree = "
IBlockStatement (5 statements, 1 locals) (OperationKind.BlockStatement, IsInvalid)
  Local_1: Method As System.Boolean
  IIfStatement (OperationKind.IfStatement)
    Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean, Constant: False)
        Left: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
        Right: ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2)
    IBlockStatement (0 statements) (OperationKind.BlockStatement)
  IReturnStatement (OperationKind.ReturnStatement)
    ILiteralExpression (Text: True) (OperationKind.LiteralExpression, Type: System.Boolean, Constant: True)
  IInvalidStatement (OperationKind.InvalidStatement, IsInvalid)
  ILabelStatement (Label: exit) (OperationKind.LabelStatement)
  IReturnStatement (OperationKind.ReturnStatement)
    ILocalReferenceExpression: Method (OperationKind.LocalReferenceExpression, Type: System.Boolean)"

            VerifyOperationTreeForTest(Of MethodBlockBaseSyntax)(source, expectedOperationTree)
        End Sub

        <Fact>
        Public Sub IBlockStatement_PropertyGetBlock()
            Dim source = "
Class Program
    ReadOnly Property Prop As Integer
        Get'BIND:""Get""
            If 1 > 2
            End If
        End Get
    End Sub
End Class"

            Dim expectedOperationTree = "
IBlockStatement (3 statements, 1 locals) (OperationKind.BlockStatement)
  Local_1: Prop As System.Int32
  IIfStatement (OperationKind.IfStatement)
    Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean, Constant: False)
        Left: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
        Right: ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2)
    IBlockStatement (0 statements) (OperationKind.BlockStatement)
  ILabelStatement (Label: exit) (OperationKind.LabelStatement)
  IReturnStatement (OperationKind.ReturnStatement)
    ILocalReferenceExpression: Prop (OperationKind.LocalReferenceExpression, Type: System.Int32)"

            VerifyOperationTreeForTest(Of MethodBlockBaseSyntax)(source, expectedOperationTree)
        End Sub

        <Fact>
        Public Sub IBlockStatement_PropertySetBlock()
            Dim source = "
Class Program
    WriteOnly Property Prop As Integer
        Set(Value As Integer)'BIND:""Set(Value As Integer)""
            If 1 > 2
            End If
        End Set
    End Sub
End Class"

            Dim expectedOperationTree = "
IBlockStatement (3 statements) (OperationKind.BlockStatement)
  IIfStatement (OperationKind.IfStatement)
    Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean, Constant: False)
        Left: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
        Right: ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2)
    IBlockStatement (0 statements) (OperationKind.BlockStatement)
  ILabelStatement (Label: exit) (OperationKind.LabelStatement)
  IReturnStatement (OperationKind.ReturnStatement)"

            VerifyOperationTreeForTest(Of MethodBlockBaseSyntax)(source, expectedOperationTree)
        End Sub

        <Fact>
        Public Sub IBlockStatement_OperatorBlock()
            Dim source = "
Class Program
    Public Shared Operator +(p As Program, i As Integer) As Integer'BIND:""Public Shared Operator +(p As Program, i As Integer) As Integer""
        If 1 > 2
        End If

        Return 0;
    End Operator
End Class"

            Dim expectedOperationTree = "
IBlockStatement (4 statements, 1 locals) (OperationKind.BlockStatement)
  Local_1:  As System.Int32
  IIfStatement (OperationKind.IfStatement)
    Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean, Constant: False)
        Left: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
        Right: ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2)
    IBlockStatement (0 statements) (OperationKind.BlockStatement)
  IReturnStatement (OperationKind.ReturnStatement)
    ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
  ILabelStatement (Label: exit) (OperationKind.LabelStatement)
  IReturnStatement (OperationKind.ReturnStatement)
    ILocalReferenceExpression:  (OperationKind.LocalReferenceExpression, Type: System.Int32)"

            VerifyOperationTreeForTest(Of MethodBlockBaseSyntax)(source, expectedOperationTree)
        End Sub

        <Fact>
        Public Sub IBlockStatement_MustOverrideSubMethodStatement()
            Dim source = "
MustInherit Class Program
    Public MustOverride Sub Method'BIND:""Public MustOverride Sub Method""
End Class"

            VerifyNoOperationTreeForTest(Of MethodStatementSyntax)(source)
        End Sub

        <Fact>
        Public Sub IBlockStatement_InterfaceSub()
            Dim source = "
Interface IProgram
    Sub Method'BIND:""Sub Method""
End Interface"

            VerifyNoOperationTreeForTest(Of MethodStatementSyntax)(source)
        End Sub

        <Fact>
        Public Sub IBlockStatement_InterfaceFunction()
            Dim source = "
Interface IProgram
    Function Method() As Boolean'BIND:""Function Method() As Boolean""
End Interface"

            VerifyNoOperationTreeForTest(Of MethodStatementSyntax)(source)
        End Sub
    End Class
End Namespace