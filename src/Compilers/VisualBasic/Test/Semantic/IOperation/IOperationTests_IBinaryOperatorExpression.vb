﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase

        <Fact>
        Public Sub VerifyLiftedBinaryOperators1()
            Dim source = <![CDATA[
Class C
    Sub F(x as Integer?, y as Integer?)
        dim z = x + y 'BIND:"x + y"
    End Sub
End Class
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd-IsLifted) (OperationKind.BinaryOperatorExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 'x + y')
  Left: IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 'x')
  Right: IParameterReferenceExpression: y (OperationKind.ParameterReferenceExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 'y')]]>.Value

            VerifyOperationTreeForTest(Of BinaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <Fact>
        Public Sub VerifyNonLiftedBinaryOperators1()
            Dim source = <![CDATA[
Class C
    Sub F(x as Integer, y as Integer)
        dim z = x + y 'BIND:"x + y"
    End Sub
End Class
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'x + y')
  Left: IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'x')
  Right: IParameterReferenceExpression: y (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'y')
]]>.Value

            VerifyOperationTreeForTest(Of BinaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <Fact>
        Public Sub VerifyLiftedUserDefinedShortCircuitBinaryOperators1()
            Dim source = <![CDATA[
Structure C
    Public Shared Operator And(c1 as C, cs as C) as C
    End Operator

    Public Shared Operator IsFalse(c1 as C) as Boolean
    End Operator

    Sub F(x as C?, y as C?)
        dim z = x And y 'BIND:"x And y"
    End Sub
End Structure
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IBinaryOperatorExpression (BinaryOperationKind.OperatorMethodAnd-IsLifted) (OperatorMethod: Function C.op_BitwiseAnd(c1 As C, cs As C) As C) (OperationKind.BinaryOperatorExpression, Type: System.Nullable(Of C)) (Syntax: 'x And y')
  Left: IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Nullable(Of C)) (Syntax: 'x')
  Right: IParameterReferenceExpression: y (OperationKind.ParameterReferenceExpression, Type: System.Nullable(Of C)) (Syntax: 'y')
]]>.Value

            VerifyOperationTreeForTest(Of BinaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <Fact>
        Public Sub VerifyLiftedUserDefinedShortCircuitBinaryOperators2()
            Dim source = <![CDATA[
Structure C
    Public Shared Operator And(c1 as C, cs as C) as C
    End Operator

    Public Shared Operator IsFalse(c1 as C) as Boolean
    End Operator

    Sub F(x as C?, y as C?)
        dim z = x AndAlso y 'BIND:"x AndAlso y"
    End Sub
End Structure
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IBinaryOperatorExpression (BinaryOperationKind.OperatorMethodConditionalAnd-IsLifted) (OperatorMethod: Function C.op_BitwiseAnd(c1 As C, cs As C) As C) (OperationKind.BinaryOperatorExpression, Type: System.Nullable(Of C)) (Syntax: 'x AndAlso y')
  Left: IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Nullable(Of C)) (Syntax: 'x')
  Right: IParameterReferenceExpression: y (OperationKind.ParameterReferenceExpression, Type: System.Nullable(Of C)) (Syntax: 'y')
]]>.Value

            VerifyOperationTreeForTest(Of BinaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <Fact>
        Public Sub VerifyNonLiftedUserDefinedShortCircuitBinaryOperators1()
            Dim source = <![CDATA[
Structure C
    Public Shared Operator And(c1 as C, cs as C) as C
    End Operator

    Public Shared Operator IsFalse(c1 as C) as Boolean
    End Operator

    Sub F(x as C, y as C)
        dim z = x And y 'BIND:"x And y"
    End Sub
End Structure
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IBinaryOperatorExpression (BinaryOperationKind.OperatorMethodAnd) (OperatorMethod: Function C.op_BitwiseAnd(c1 As C, cs As C) As C) (OperationKind.BinaryOperatorExpression, Type: C) (Syntax: 'x And y')
  Left: IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: C) (Syntax: 'x')
  Right: IParameterReferenceExpression: y (OperationKind.ParameterReferenceExpression, Type: C) (Syntax: 'y')
]]>.Value

            VerifyOperationTreeForTest(Of BinaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <Fact>
        Public Sub VerifyNonLiftedUserDefinedShortCircuitBinaryOperators2()
            Dim source = <![CDATA[
Structure C
    Public Shared Operator And(c1 as C, cs as C) as C
    End Operator

    Public Shared Operator IsFalse(c1 as C) as Boolean
    End Operator

    Sub F(x as C, y as C)
        dim z = x AndAlso y 'BIND:"x AndAlso y"
    End Sub
End Structure
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IBinaryOperatorExpression (BinaryOperationKind.OperatorMethodConditionalAnd) (OperatorMethod: Function C.op_BitwiseAnd(c1 As C, cs As C) As C) (OperationKind.BinaryOperatorExpression, Type: C) (Syntax: 'x AndAlso y')
  Left: IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: C) (Syntax: 'x')
  Right: IParameterReferenceExpression: y (OperationKind.ParameterReferenceExpression, Type: C) (Syntax: 'y')]]>.Value

            VerifyOperationTreeForTest(Of BinaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <Fact>
        Public Sub VerifyLiftedUserDefinedBinaryOperators1()
            Dim source = <![CDATA[
Structure C
    Public Shared Operator + (c1 as C, c2 as C) as C
    End Operator

    Sub F(x as C?, y as C?)
        dim z = x + y 'BIND:"x + y"
    End Sub
End Structure
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IBinaryOperatorExpression (BinaryOperationKind.OperatorMethodAdd-IsLifted) (OperatorMethod: Function C.op_Addition(c1 As C, c2 As C) As C) (OperationKind.BinaryOperatorExpression, Type: System.Nullable(Of C)) (Syntax: 'x + y')
  Left: IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Nullable(Of C)) (Syntax: 'x')
  Right: IParameterReferenceExpression: y (OperationKind.ParameterReferenceExpression, Type: System.Nullable(Of C)) (Syntax: 'y')
]]>.Value

            VerifyOperationTreeForTest(Of BinaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <Fact>
        Public Sub VerifyNonLiftedUserDefinedBinaryOperators1()
            Dim source = <![CDATA[
Structure C
    Public Shared Operator + (c1 as C, c2 as C) as C
    End Operator

    Sub F(x as C, y as C)
        dim z = x + y 'BIND:"x + y"
    End Sub
End Structure
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IBinaryOperatorExpression (BinaryOperationKind.OperatorMethodAdd) (OperatorMethod: Function C.op_Addition(c1 As C, c2 As C) As C) (OperationKind.BinaryOperatorExpression, Type: C) (Syntax: 'x + y')
  Left: IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: C) (Syntax: 'x')
  Right: IParameterReferenceExpression: y (OperationKind.ParameterReferenceExpression, Type: C) (Syntax: 'y')
]]>.Value

            VerifyOperationTreeForTest(Of BinaryExpressionSyntax)(source, expectedOperationTree)
        End Sub
    End Class
End Namespace
