' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase

        <CompilerTrait(CompilerFeature.IOperation)>
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
IBinaryOperation (BinaryOperatorKind.Add, IsLifted, Checked) (OperationKind.BinaryOperator, Type: System.Nullable(Of System.Int32)) (Syntax: 'x + y')
  Left: 
    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'x')
  Right: 
    IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'y')
]]>.Value

            VerifyOperationTreeForTest(Of BinaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
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
IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'x + y')
  Left: 
    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
  Right: 
    IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'y')
]]>.Value

            VerifyOperationTreeForTest(Of BinaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
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
IBinaryOperation (BinaryOperatorKind.And, IsLifted, Checked) (OperatorMethod: Function C.op_BitwiseAnd(c1 As C, cs As C) As C) (OperationKind.BinaryOperator, Type: System.Nullable(Of C)) (Syntax: 'x And y')
  Left: 
    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Nullable(Of C)) (Syntax: 'x')
  Right: 
    IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.Nullable(Of C)) (Syntax: 'y')
]]>.Value

            VerifyOperationTreeForTest(Of BinaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
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
IBinaryOperation (BinaryOperatorKind.ConditionalAnd, IsLifted) (OperatorMethod: Function C.op_BitwiseAnd(c1 As C, cs As C) As C) (OperationKind.BinaryOperator, Type: System.Nullable(Of C)) (Syntax: 'x AndAlso y')
  Left: 
    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Nullable(Of C)) (Syntax: 'x')
  Right: 
    IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.Nullable(Of C)) (Syntax: 'y')
]]>.Value

            VerifyOperationTreeForTest(Of BinaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
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
IBinaryOperation (BinaryOperatorKind.And, Checked) (OperatorMethod: Function C.op_BitwiseAnd(c1 As C, cs As C) As C) (OperationKind.BinaryOperator, Type: C) (Syntax: 'x And y')
  Left: 
    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: C) (Syntax: 'x')
  Right: 
    IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: C) (Syntax: 'y')
]]>.Value

            VerifyOperationTreeForTest(Of BinaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
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
IBinaryOperation (BinaryOperatorKind.ConditionalAnd) (OperatorMethod: Function C.op_BitwiseAnd(c1 As C, cs As C) As C) (OperationKind.BinaryOperator, Type: C) (Syntax: 'x AndAlso y')
  Left: 
    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: C) (Syntax: 'x')
  Right: 
    IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: C) (Syntax: 'y')
]]>.Value

            VerifyOperationTreeForTest(Of BinaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
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
IBinaryOperation (BinaryOperatorKind.Add, IsLifted, Checked) (OperatorMethod: Function C.op_Addition(c1 As C, c2 As C) As C) (OperationKind.BinaryOperator, Type: System.Nullable(Of C)) (Syntax: 'x + y')
  Left: 
    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Nullable(Of C)) (Syntax: 'x')
  Right: 
    IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.Nullable(Of C)) (Syntax: 'y')
]]>.Value

            VerifyOperationTreeForTest(Of BinaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
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
IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperatorMethod: Function C.op_Addition(c1 As C, c2 As C) As C) (OperationKind.BinaryOperator, Type: C) (Syntax: 'x + y')
  Left: 
    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: C) (Syntax: 'x')
  Right: 
    IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: C) (Syntax: 'y')
]]>.Value

            VerifyOperationTreeForTest(Of BinaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TestBinaryOperators()
            Dim source = <![CDATA[
Module Module1

    Sub Main()'BIND:"Sub Main()"
        Dim x, y As New Integer
        Dim r As Integer
        r = x + y
        r = x - y
        r = x * y
        r = x / y
        r = x \ y
        r = x Mod y
        r = x ^ y
        r = x = y
        r = x <> y
        r = x < y
        r = x > y
        r = x <= y
        r = x >= y
        r = x Like y
        r = x & y
        r = x And y
        r = x Or y
        r = x Xor y
        r = x << 2
        r = x >> 3
        r = DirectCast(x, Object) = y
        r = DirectCast(x, Object) <> y
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IBlockOperation (26 statements, 3 locals) (OperationKind.Block, Type: null) (Syntax: 'Sub Main()' ... End Sub')
  Locals: Local_1: x As System.Int32
    Local_2: y As System.Int32
    Local_3: r As System.Int32
  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim x, y As New Integer')
    IVariableDeclarationOperation (2 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'x, y As New Integer')
      Declarators:
          IVariableDeclaratorOperation (Symbol: x As System.Int32) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'x')
            Initializer: 
              null
          IVariableDeclaratorOperation (Symbol: y As System.Int32) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'y')
            Initializer: 
              null
      Initializer: 
        IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: 'As New Integer')
          IObjectCreationOperation (Constructor: Sub System.Int32..ctor()) (OperationKind.ObjectCreation, Type: System.Int32) (Syntax: 'New Integer')
            Arguments(0)
            Initializer: 
              null
  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim r As Integer')
    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'r As Integer')
      Declarators:
          IVariableDeclaratorOperation (Symbol: r As System.Int32) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'r')
            Initializer: 
              null
      Initializer: 
        null
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'r = x + y')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'r = x + y')
        Left: 
          ILocalReferenceOperation: r (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'r')
        Right: 
          IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'x + y')
            Left: 
              ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
            Right: 
              ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'r = x - y')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'r = x - y')
        Left: 
          ILocalReferenceOperation: r (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'r')
        Right: 
          IBinaryOperation (BinaryOperatorKind.Subtract, Checked) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'x - y')
            Left: 
              ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
            Right: 
              ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'r = x * y')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'r = x * y')
        Left: 
          ILocalReferenceOperation: r (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'r')
        Right: 
          IBinaryOperation (BinaryOperatorKind.Multiply, Checked) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'x * y')
            Left: 
              ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
            Right: 
              ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'r = x / y')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'r = x / y')
        Left: 
          ILocalReferenceOperation: r (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'r')
        Right: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsImplicit) (Syntax: 'x / y')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IBinaryOperation (BinaryOperatorKind.Divide, Checked) (OperationKind.BinaryOperator, Type: System.Double) (Syntax: 'x / y')
                Left: 
                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Double, IsImplicit) (Syntax: 'x')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
                Right: 
                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Double, IsImplicit) (Syntax: 'y')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'r = x \ y')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'r = x \ y')
        Left: 
          ILocalReferenceOperation: r (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'r')
        Right: 
          IBinaryOperation (BinaryOperatorKind.IntegerDivide, Checked) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'x \ y')
            Left: 
              ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
            Right: 
              ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'r = x Mod y')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'r = x Mod y')
        Left: 
          ILocalReferenceOperation: r (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'r')
        Right: 
          IBinaryOperation (BinaryOperatorKind.Remainder, Checked) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'x Mod y')
            Left: 
              ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
            Right: 
              ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'r = x ^ y')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'r = x ^ y')
        Left: 
          ILocalReferenceOperation: r (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'r')
        Right: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsImplicit) (Syntax: 'x ^ y')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IBinaryOperation (BinaryOperatorKind.Power, Checked) (OperationKind.BinaryOperator, Type: System.Double) (Syntax: 'x ^ y')
                Left: 
                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Double, IsImplicit) (Syntax: 'x')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
                Right: 
                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Double, IsImplicit) (Syntax: 'y')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'r = x = y')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'r = x = y')
        Left: 
          ILocalReferenceOperation: r (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'r')
        Right: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsImplicit) (Syntax: 'x = y')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IBinaryOperation (BinaryOperatorKind.Equals, Checked) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'x = y')
                Left: 
                  ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
                Right: 
                  ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'r = x <> y')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'r = x <> y')
        Left: 
          ILocalReferenceOperation: r (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'r')
        Right: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsImplicit) (Syntax: 'x <> y')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IBinaryOperation (BinaryOperatorKind.NotEquals, Checked) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'x <> y')
                Left: 
                  ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
                Right: 
                  ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'r = x < y')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'r = x < y')
        Left: 
          ILocalReferenceOperation: r (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'r')
        Right: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsImplicit) (Syntax: 'x < y')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IBinaryOperation (BinaryOperatorKind.LessThan, Checked) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'x < y')
                Left: 
                  ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
                Right: 
                  ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'r = x > y')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'r = x > y')
        Left: 
          ILocalReferenceOperation: r (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'r')
        Right: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsImplicit) (Syntax: 'x > y')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IBinaryOperation (BinaryOperatorKind.GreaterThan, Checked) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'x > y')
                Left: 
                  ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
                Right: 
                  ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'r = x <= y')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'r = x <= y')
        Left: 
          ILocalReferenceOperation: r (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'r')
        Right: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsImplicit) (Syntax: 'x <= y')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IBinaryOperation (BinaryOperatorKind.LessThanOrEqual, Checked) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'x <= y')
                Left: 
                  ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
                Right: 
                  ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'r = x >= y')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'r = x >= y')
        Left: 
          ILocalReferenceOperation: r (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'r')
        Right: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsImplicit) (Syntax: 'x >= y')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IBinaryOperation (BinaryOperatorKind.GreaterThanOrEqual, Checked) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'x >= y')
                Left: 
                  ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
                Right: 
                  ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'r = x Like y')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'r = x Like y')
        Left: 
          ILocalReferenceOperation: r (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'r')
        Right: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsImplicit) (Syntax: 'x Like y')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IBinaryOperation (BinaryOperatorKind.Like, Checked) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'x Like y')
                Left: 
                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String, IsImplicit) (Syntax: 'x')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
                Right: 
                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String, IsImplicit) (Syntax: 'y')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'r = x & y')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'r = x & y')
        Left: 
          ILocalReferenceOperation: r (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'r')
        Right: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsImplicit) (Syntax: 'x & y')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IBinaryOperation (BinaryOperatorKind.Concatenate, Checked) (OperationKind.BinaryOperator, Type: System.String) (Syntax: 'x & y')
                Left: 
                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String, IsImplicit) (Syntax: 'x')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
                Right: 
                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String, IsImplicit) (Syntax: 'y')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'r = x And y')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'r = x And y')
        Left: 
          ILocalReferenceOperation: r (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'r')
        Right: 
          IBinaryOperation (BinaryOperatorKind.And, Checked) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'x And y')
            Left: 
              ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
            Right: 
              ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'r = x Or y')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'r = x Or y')
        Left: 
          ILocalReferenceOperation: r (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'r')
        Right: 
          IBinaryOperation (BinaryOperatorKind.Or, Checked) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'x Or y')
            Left: 
              ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
            Right: 
              ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'r = x Xor y')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'r = x Xor y')
        Left: 
          ILocalReferenceOperation: r (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'r')
        Right: 
          IBinaryOperation (BinaryOperatorKind.ExclusiveOr, Checked) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'x Xor y')
            Left: 
              ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
            Right: 
              ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'r = x << 2')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'r = x << 2')
        Left: 
          ILocalReferenceOperation: r (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'r')
        Right: 
          IBinaryOperation (BinaryOperatorKind.LeftShift, Checked) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'x << 2')
            Left: 
              ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'r = x >> 3')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'r = x >> 3')
        Left: 
          ILocalReferenceOperation: r (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'r')
        Right: 
          IBinaryOperation (BinaryOperatorKind.RightShift, Checked) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'x >> 3')
            Left: 
              ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'r = DirectC ... Object) = y')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'r = DirectC ... Object) = y')
        Left: 
          ILocalReferenceOperation: r (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'r')
        Right: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsImplicit) (Syntax: 'DirectCast( ... Object) = y')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IBinaryOperation (BinaryOperatorKind.ObjectValueEquals, Checked) (OperationKind.BinaryOperator, Type: System.Object) (Syntax: 'DirectCast( ... Object) = y')
                Left: 
                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object) (Syntax: 'DirectCast(x, Object)')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
                Right: 
                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'y')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'r = DirectC ... bject) <> y')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'r = DirectC ... bject) <> y')
        Left: 
          ILocalReferenceOperation: r (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'r')
        Right: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsImplicit) (Syntax: 'DirectCast( ... bject) <> y')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IBinaryOperation (BinaryOperatorKind.ObjectValueNotEquals, Checked) (OperationKind.BinaryOperator, Type: System.Object) (Syntax: 'DirectCast( ... bject) <> y')
                Left: 
                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object) (Syntax: 'DirectCast(x, Object)')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
                Right: 
                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'y')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
  ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'End Sub')
    Statement: 
      null
  IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'End Sub')
    ReturnedValue: 
      null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TestBinaryOperators_Unchecked()
            Dim source = <![CDATA[
Module Module1

    Sub Main()'BIND:"Sub Main()"
        Dim x, y As New Integer
        Dim r As Integer
        r = x + y
        r = x - y
        r = x * y
        r = x / y
        r = x \ y
        r = x Mod y
        r = x ^ y
        r = x = y
        r = x <> y
        r = x < y
        r = x > y
        r = x <= y
        r = x >= y
        r = x Like y
        r = x & y
        r = x And y
        r = x Or y
        r = x Xor y
        r = x << 2
        r = x >> 3
        r = DirectCast(x, Object) = y
        r = DirectCast(x, Object) <> y
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IBlockOperation (26 statements, 3 locals) (OperationKind.Block, Type: null) (Syntax: 'Sub Main()' ... End Sub')
  Locals: Local_1: x As System.Int32
    Local_2: y As System.Int32
    Local_3: r As System.Int32
  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim x, y As New Integer')
    IVariableDeclarationOperation (2 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'x, y As New Integer')
      Declarators:
          IVariableDeclaratorOperation (Symbol: x As System.Int32) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'x')
            Initializer: 
              null
          IVariableDeclaratorOperation (Symbol: y As System.Int32) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'y')
            Initializer: 
              null
      Initializer: 
        IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: 'As New Integer')
          IObjectCreationOperation (Constructor: Sub System.Int32..ctor()) (OperationKind.ObjectCreation, Type: System.Int32) (Syntax: 'New Integer')
            Arguments(0)
            Initializer: 
              null
  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim r As Integer')
    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'r As Integer')
      Declarators:
          IVariableDeclaratorOperation (Symbol: r As System.Int32) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'r')
            Initializer: 
              null
      Initializer: 
        null
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'r = x + y')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'r = x + y')
        Left: 
          ILocalReferenceOperation: r (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'r')
        Right: 
          IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'x + y')
            Left: 
              ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
            Right: 
              ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'r = x - y')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'r = x - y')
        Left: 
          ILocalReferenceOperation: r (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'r')
        Right: 
          IBinaryOperation (BinaryOperatorKind.Subtract) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'x - y')
            Left: 
              ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
            Right: 
              ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'r = x * y')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'r = x * y')
        Left: 
          ILocalReferenceOperation: r (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'r')
        Right: 
          IBinaryOperation (BinaryOperatorKind.Multiply) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'x * y')
            Left: 
              ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
            Right: 
              ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'r = x / y')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'r = x / y')
        Left: 
          ILocalReferenceOperation: r (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'r')
        Right: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsImplicit) (Syntax: 'x / y')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IBinaryOperation (BinaryOperatorKind.Divide) (OperationKind.BinaryOperator, Type: System.Double) (Syntax: 'x / y')
                Left: 
                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Double, IsImplicit) (Syntax: 'x')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
                Right: 
                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Double, IsImplicit) (Syntax: 'y')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'r = x \ y')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'r = x \ y')
        Left: 
          ILocalReferenceOperation: r (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'r')
        Right: 
          IBinaryOperation (BinaryOperatorKind.IntegerDivide) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'x \ y')
            Left: 
              ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
            Right: 
              ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'r = x Mod y')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'r = x Mod y')
        Left: 
          ILocalReferenceOperation: r (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'r')
        Right: 
          IBinaryOperation (BinaryOperatorKind.Remainder) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'x Mod y')
            Left: 
              ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
            Right: 
              ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'r = x ^ y')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'r = x ^ y')
        Left: 
          ILocalReferenceOperation: r (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'r')
        Right: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsImplicit) (Syntax: 'x ^ y')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IBinaryOperation (BinaryOperatorKind.Power) (OperationKind.BinaryOperator, Type: System.Double) (Syntax: 'x ^ y')
                Left: 
                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Double, IsImplicit) (Syntax: 'x')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
                Right: 
                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Double, IsImplicit) (Syntax: 'y')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'r = x = y')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'r = x = y')
        Left: 
          ILocalReferenceOperation: r (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'r')
        Right: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsImplicit) (Syntax: 'x = y')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'x = y')
                Left: 
                  ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
                Right: 
                  ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'r = x <> y')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'r = x <> y')
        Left: 
          ILocalReferenceOperation: r (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'r')
        Right: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsImplicit) (Syntax: 'x <> y')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IBinaryOperation (BinaryOperatorKind.NotEquals) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'x <> y')
                Left: 
                  ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
                Right: 
                  ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'r = x < y')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'r = x < y')
        Left: 
          ILocalReferenceOperation: r (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'r')
        Right: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsImplicit) (Syntax: 'x < y')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IBinaryOperation (BinaryOperatorKind.LessThan) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'x < y')
                Left: 
                  ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
                Right: 
                  ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'r = x > y')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'r = x > y')
        Left: 
          ILocalReferenceOperation: r (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'r')
        Right: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsImplicit) (Syntax: 'x > y')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IBinaryOperation (BinaryOperatorKind.GreaterThan) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'x > y')
                Left: 
                  ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
                Right: 
                  ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'r = x <= y')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'r = x <= y')
        Left: 
          ILocalReferenceOperation: r (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'r')
        Right: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsImplicit) (Syntax: 'x <= y')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IBinaryOperation (BinaryOperatorKind.LessThanOrEqual) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'x <= y')
                Left: 
                  ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
                Right: 
                  ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'r = x >= y')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'r = x >= y')
        Left: 
          ILocalReferenceOperation: r (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'r')
        Right: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsImplicit) (Syntax: 'x >= y')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IBinaryOperation (BinaryOperatorKind.GreaterThanOrEqual) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'x >= y')
                Left: 
                  ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
                Right: 
                  ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'r = x Like y')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'r = x Like y')
        Left: 
          ILocalReferenceOperation: r (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'r')
        Right: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsImplicit) (Syntax: 'x Like y')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IBinaryOperation (BinaryOperatorKind.Like) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'x Like y')
                Left: 
                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String, IsImplicit) (Syntax: 'x')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
                Right: 
                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String, IsImplicit) (Syntax: 'y')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'r = x & y')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'r = x & y')
        Left: 
          ILocalReferenceOperation: r (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'r')
        Right: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsImplicit) (Syntax: 'x & y')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IBinaryOperation (BinaryOperatorKind.Concatenate) (OperationKind.BinaryOperator, Type: System.String) (Syntax: 'x & y')
                Left: 
                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String, IsImplicit) (Syntax: 'x')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
                Right: 
                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String, IsImplicit) (Syntax: 'y')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'r = x And y')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'r = x And y')
        Left: 
          ILocalReferenceOperation: r (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'r')
        Right: 
          IBinaryOperation (BinaryOperatorKind.And) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'x And y')
            Left: 
              ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
            Right: 
              ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'r = x Or y')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'r = x Or y')
        Left: 
          ILocalReferenceOperation: r (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'r')
        Right: 
          IBinaryOperation (BinaryOperatorKind.Or) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'x Or y')
            Left: 
              ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
            Right: 
              ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'r = x Xor y')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'r = x Xor y')
        Left: 
          ILocalReferenceOperation: r (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'r')
        Right: 
          IBinaryOperation (BinaryOperatorKind.ExclusiveOr) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'x Xor y')
            Left: 
              ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
            Right: 
              ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'r = x << 2')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'r = x << 2')
        Left: 
          ILocalReferenceOperation: r (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'r')
        Right: 
          IBinaryOperation (BinaryOperatorKind.LeftShift) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'x << 2')
            Left: 
              ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'r = x >> 3')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'r = x >> 3')
        Left: 
          ILocalReferenceOperation: r (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'r')
        Right: 
          IBinaryOperation (BinaryOperatorKind.RightShift) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'x >> 3')
            Left: 
              ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'r = DirectC ... Object) = y')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'r = DirectC ... Object) = y')
        Left: 
          ILocalReferenceOperation: r (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'r')
        Right: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsImplicit) (Syntax: 'DirectCast( ... Object) = y')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IBinaryOperation (BinaryOperatorKind.ObjectValueEquals) (OperationKind.BinaryOperator, Type: System.Object) (Syntax: 'DirectCast( ... Object) = y')
                Left: 
                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object) (Syntax: 'DirectCast(x, Object)')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
                Right: 
                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'y')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'r = DirectC ... bject) <> y')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'r = DirectC ... bject) <> y')
        Left: 
          ILocalReferenceOperation: r (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'r')
        Right: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsImplicit) (Syntax: 'DirectCast( ... bject) <> y')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IBinaryOperation (BinaryOperatorKind.ObjectValueNotEquals) (OperationKind.BinaryOperator, Type: System.Object) (Syntax: 'DirectCast( ... bject) <> y')
                Left: 
                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object) (Syntax: 'DirectCast(x, Object)')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
                Right: 
                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'y')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
  ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'End Sub')
    Statement: 
      null
  IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'End Sub')
    ReturnedValue: 
      null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim fileName = "a.vb"
            Dim syntaxTree = Parse(source, fileName)
            Dim references = DefaultVbReferences.Concat({ValueTupleRef, SystemRuntimeFacadeRef})
            Dim compilation = CreateCompilationWithMscorlib45AndVBRuntime({syntaxTree}, references:=references, options:=TestOptions.ReleaseDll.WithOverflowChecks(False))

            VerifyOperationTreeAndDiagnosticsForTest(Of MethodBlockSyntax)(compilation, fileName, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub TestBinaryOperator_CompareText()
            Dim source = <![CDATA[
Option Compare Text

Class C
    Sub M(x As String, y As String, r As Integer)'BIND:"Sub M(x As String, y As String, r As Integer)"
        r = x = y
        r = x <> y
        r = x < y
        r = x > y
        r = x <= y
        r = x >= y
        r = DirectCast(x, Object) = y
        r = DirectCast(x, Object) <> y
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IBlockOperation (10 statements) (OperationKind.Block, Type: null) (Syntax: 'Sub M(x As  ... End Sub')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'r = x = y')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'r = x = y')
        Left: 
          IParameterReferenceOperation: r (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'r')
        Right: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsImplicit) (Syntax: 'x = y')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IBinaryOperation (BinaryOperatorKind.Equals, Checked, CompareText) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'x = y')
                Left: 
                  IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.String) (Syntax: 'x')
                Right: 
                  IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.String) (Syntax: 'y')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'r = x <> y')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'r = x <> y')
        Left: 
          IParameterReferenceOperation: r (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'r')
        Right: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsImplicit) (Syntax: 'x <> y')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IBinaryOperation (BinaryOperatorKind.NotEquals, Checked, CompareText) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'x <> y')
                Left: 
                  IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.String) (Syntax: 'x')
                Right: 
                  IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.String) (Syntax: 'y')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'r = x < y')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'r = x < y')
        Left: 
          IParameterReferenceOperation: r (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'r')
        Right: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsImplicit) (Syntax: 'x < y')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IBinaryOperation (BinaryOperatorKind.LessThan, Checked, CompareText) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'x < y')
                Left: 
                  IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.String) (Syntax: 'x')
                Right: 
                  IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.String) (Syntax: 'y')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'r = x > y')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'r = x > y')
        Left: 
          IParameterReferenceOperation: r (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'r')
        Right: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsImplicit) (Syntax: 'x > y')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IBinaryOperation (BinaryOperatorKind.GreaterThan, Checked, CompareText) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'x > y')
                Left: 
                  IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.String) (Syntax: 'x')
                Right: 
                  IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.String) (Syntax: 'y')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'r = x <= y')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'r = x <= y')
        Left: 
          IParameterReferenceOperation: r (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'r')
        Right: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsImplicit) (Syntax: 'x <= y')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IBinaryOperation (BinaryOperatorKind.LessThanOrEqual, Checked, CompareText) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'x <= y')
                Left: 
                  IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.String) (Syntax: 'x')
                Right: 
                  IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.String) (Syntax: 'y')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'r = x >= y')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'r = x >= y')
        Left: 
          IParameterReferenceOperation: r (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'r')
        Right: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsImplicit) (Syntax: 'x >= y')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IBinaryOperation (BinaryOperatorKind.GreaterThanOrEqual, Checked, CompareText) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'x >= y')
                Left: 
                  IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.String) (Syntax: 'x')
                Right: 
                  IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.String) (Syntax: 'y')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'r = DirectC ... Object) = y')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'r = DirectC ... Object) = y')
        Left: 
          IParameterReferenceOperation: r (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'r')
        Right: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsImplicit) (Syntax: 'DirectCast( ... Object) = y')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IBinaryOperation (BinaryOperatorKind.ObjectValueEquals, Checked, CompareText) (OperationKind.BinaryOperator, Type: System.Object) (Syntax: 'DirectCast( ... Object) = y')
                Left: 
                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object) (Syntax: 'DirectCast(x, Object)')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.String) (Syntax: 'x')
                Right: 
                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'y')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.String) (Syntax: 'y')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'r = DirectC ... bject) <> y')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'r = DirectC ... bject) <> y')
        Left: 
          IParameterReferenceOperation: r (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'r')
        Right: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsImplicit) (Syntax: 'DirectCast( ... bject) <> y')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IBinaryOperation (BinaryOperatorKind.ObjectValueNotEquals, Checked, CompareText) (OperationKind.BinaryOperator, Type: System.Object) (Syntax: 'DirectCast( ... bject) <> y')
                Left: 
                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object) (Syntax: 'DirectCast(x, Object)')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.String) (Syntax: 'x')
                Right: 
                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'y')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.String) (Syntax: 'y')
  ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'End Sub')
    Statement: 
      null
  IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'End Sub')
    ReturnedValue: 
      null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TestBinaryCompoundOperators()
            Dim source = <![CDATA[
Module Module1

    Sub Main()'BIND:"Sub Main()"
        Dim x, y As New Integer
        x += y
        x -= y
        x *= y
        x /= y
        x \= y
        x ^= y
        x &= y
        x <<= 2
        x >>= 3
    End Sub
End Module]]>.Value

            ' We don't seem to be detecting "x ^= y" and "x &= y" as compound operator expressions.
            ' See https://github.com/dotnet/roslyn/issues/21738
            Dim expectedOperationTree = <![CDATA[
IBlockOperation (12 statements, 2 locals) (OperationKind.Block, Type: null) (Syntax: 'Sub Main()' ... End Sub')
  Locals: Local_1: x As System.Int32
    Local_2: y As System.Int32
  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim x, y As New Integer')
    IVariableDeclarationOperation (2 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'x, y As New Integer')
      Declarators:
          IVariableDeclaratorOperation (Symbol: x As System.Int32) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'x')
            Initializer: 
              null
          IVariableDeclaratorOperation (Symbol: y As System.Int32) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'y')
            Initializer: 
              null
      Initializer: 
        IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: 'As New Integer')
          IObjectCreationOperation (Constructor: Sub System.Int32..ctor()) (OperationKind.ObjectCreation, Type: System.Int32) (Syntax: 'New Integer')
            Arguments(0)
            Initializer: 
              null
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x += y')
    Expression: 
      ICompoundAssignmentOperation (BinaryOperatorKind.Add, Checked) (OperationKind.CompoundAssignment, Type: System.Int32, IsImplicit) (Syntax: 'x += y')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Left: 
          ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
        Right: 
          ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x -= y')
    Expression: 
      ICompoundAssignmentOperation (BinaryOperatorKind.Subtract, Checked) (OperationKind.CompoundAssignment, Type: System.Int32, IsImplicit) (Syntax: 'x -= y')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Left: 
          ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
        Right: 
          ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x *= y')
    Expression: 
      ICompoundAssignmentOperation (BinaryOperatorKind.Multiply, Checked) (OperationKind.CompoundAssignment, Type: System.Int32, IsImplicit) (Syntax: 'x *= y')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Left: 
          ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
        Right: 
          ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x /= y')
    Expression: 
      ICompoundAssignmentOperation (BinaryOperatorKind.Divide, Checked) (OperationKind.CompoundAssignment, Type: System.Int32, IsImplicit) (Syntax: 'x /= y')
        InConversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Left: 
          ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
        Right: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Double, IsImplicit) (Syntax: 'y')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x \= y')
    Expression: 
      ICompoundAssignmentOperation (BinaryOperatorKind.IntegerDivide, Checked) (OperationKind.CompoundAssignment, Type: System.Int32, IsImplicit) (Syntax: 'x \= y')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Left: 
          ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
        Right: 
          ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x ^= y')
    Expression: 
      ICompoundAssignmentOperation (BinaryOperatorKind.Power, Checked) (OperationKind.CompoundAssignment, Type: System.Int32, IsImplicit) (Syntax: 'x ^= y')
        InConversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Left: 
          ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
        Right: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Double, IsImplicit) (Syntax: 'y')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x &= y')
    Expression: 
      ICompoundAssignmentOperation (BinaryOperatorKind.Concatenate, Checked) (OperationKind.CompoundAssignment, Type: System.Int32, IsImplicit) (Syntax: 'x &= y')
        InConversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Left: 
          ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
        Right: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String, IsImplicit) (Syntax: 'y')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x <<= 2')
    Expression: 
      ICompoundAssignmentOperation (BinaryOperatorKind.LeftShift, Checked) (OperationKind.CompoundAssignment, Type: System.Int32, IsImplicit) (Syntax: 'x <<= 2')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Left: 
          ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
        Right: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x >>= 3')
    Expression: 
      ICompoundAssignmentOperation (BinaryOperatorKind.RightShift, Checked) (OperationKind.CompoundAssignment, Type: System.Int32, IsImplicit) (Syntax: 'x >>= 3')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Left: 
          ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
        Right: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
  ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'End Sub')
    Statement: 
      null
  IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'End Sub')
    ReturnedValue: 
      null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TestBinaryCompoundOperators_Unchecked()
            Dim source = <![CDATA[
Module Module1

    Sub Main()'BIND:"Sub Main()"
        Dim x, y As New Integer
        x += y
        x -= y
        x *= y
        x /= y
        x \= y
        x ^= y
        x &= y
        x <<= 2
        x >>= 3
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IBlockOperation (12 statements, 2 locals) (OperationKind.Block, Type: null) (Syntax: 'Sub Main()' ... End Sub')
  Locals: Local_1: x As System.Int32
    Local_2: y As System.Int32
  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim x, y As New Integer')
    IVariableDeclarationOperation (2 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'x, y As New Integer')
      Declarators:
          IVariableDeclaratorOperation (Symbol: x As System.Int32) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'x')
            Initializer: 
              null
          IVariableDeclaratorOperation (Symbol: y As System.Int32) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'y')
            Initializer: 
              null
      Initializer: 
        IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: 'As New Integer')
          IObjectCreationOperation (Constructor: Sub System.Int32..ctor()) (OperationKind.ObjectCreation, Type: System.Int32) (Syntax: 'New Integer')
            Arguments(0)
            Initializer: 
              null
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x += y')
    Expression: 
      ICompoundAssignmentOperation (BinaryOperatorKind.Add) (OperationKind.CompoundAssignment, Type: System.Int32, IsImplicit) (Syntax: 'x += y')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Left: 
          ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
        Right: 
          ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x -= y')
    Expression: 
      ICompoundAssignmentOperation (BinaryOperatorKind.Subtract) (OperationKind.CompoundAssignment, Type: System.Int32, IsImplicit) (Syntax: 'x -= y')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Left: 
          ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
        Right: 
          ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x *= y')
    Expression: 
      ICompoundAssignmentOperation (BinaryOperatorKind.Multiply) (OperationKind.CompoundAssignment, Type: System.Int32, IsImplicit) (Syntax: 'x *= y')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Left: 
          ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
        Right: 
          ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x /= y')
    Expression: 
      ICompoundAssignmentOperation (BinaryOperatorKind.Divide) (OperationKind.CompoundAssignment, Type: System.Int32, IsImplicit) (Syntax: 'x /= y')
        InConversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Left: 
          ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
        Right: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Double, IsImplicit) (Syntax: 'y')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x \= y')
    Expression: 
      ICompoundAssignmentOperation (BinaryOperatorKind.IntegerDivide) (OperationKind.CompoundAssignment, Type: System.Int32, IsImplicit) (Syntax: 'x \= y')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Left: 
          ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
        Right: 
          ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x ^= y')
    Expression: 
      ICompoundAssignmentOperation (BinaryOperatorKind.Power) (OperationKind.CompoundAssignment, Type: System.Int32, IsImplicit) (Syntax: 'x ^= y')
        InConversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Left: 
          ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
        Right: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Double, IsImplicit) (Syntax: 'y')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x &= y')
    Expression: 
      ICompoundAssignmentOperation (BinaryOperatorKind.Concatenate) (OperationKind.CompoundAssignment, Type: System.Int32, IsImplicit) (Syntax: 'x &= y')
        InConversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Left: 
          ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
        Right: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String, IsImplicit) (Syntax: 'y')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x <<= 2')
    Expression: 
      ICompoundAssignmentOperation (BinaryOperatorKind.LeftShift) (OperationKind.CompoundAssignment, Type: System.Int32, IsImplicit) (Syntax: 'x <<= 2')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Left: 
          ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
        Right: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x >>= 3')
    Expression: 
      ICompoundAssignmentOperation (BinaryOperatorKind.RightShift) (OperationKind.CompoundAssignment, Type: System.Int32, IsImplicit) (Syntax: 'x >>= 3')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Left: 
          ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
        Right: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
  ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'End Sub')
    Statement: 
      null
  IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'End Sub')
    ReturnedValue: 
      null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim fileName = "a.vb"
            Dim syntaxTree = Parse(source, fileName)
            Dim references = DefaultVbReferences.Concat({ValueTupleRef, SystemRuntimeFacadeRef})
            Dim compilation = CreateCompilationWithMscorlib45AndVBRuntime({syntaxTree}, references:=references, options:=TestOptions.ReleaseDll.WithOverflowChecks(False))

            VerifyOperationTreeAndDiagnosticsForTest(Of MethodBlockSyntax)(compilation, fileName, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TestUserDefinedBinaryOperators()
            Dim source = <![CDATA[
Public Class B2

    Public Shared Operator +(x As B2, y As B2) As B2
        System.Console.WriteLine("+")
        Return x
    End Operator

    Public Shared Operator -(x As B2, y As B2) As B2
        System.Console.WriteLine("-")
        Return x
    End Operator

    Public Shared Operator *(x As B2, y As B2) As B2
        System.Console.WriteLine("*")
        Return x
    End Operator

    Public Shared Operator /(x As B2, y As B2) As B2
        System.Console.WriteLine("/")
        Return x
    End Operator

    Public Shared Operator \(x As B2, y As B2) As B2
        System.Console.WriteLine("\")
        Return x
    End Operator

    Public Shared Operator Mod(x As B2, y As B2) As B2
        System.Console.WriteLine("Mod")
        Return x
    End Operator

    Public Shared Operator ^(x As B2, y As B2) As B2
        System.Console.WriteLine("^")
        Return x
    End Operator

    Public Shared Operator =(x As B2, y As B2) As B2
        System.Console.WriteLine("=")
        Return x
    End Operator

    Public Shared Operator <>(x As B2, y As B2) As B2
        System.Console.WriteLine("<>")
        Return x
    End Operator

    Public Shared Operator <(x As B2, y As B2) As B2
        System.Console.WriteLine("<")
        Return x
    End Operator

    Public Shared Operator >(x As B2, y As B2) As B2
        System.Console.WriteLine(">")
        Return x
    End Operator

    Public Shared Operator <=(x As B2, y As B2) As B2
        System.Console.WriteLine("<=")
        Return x
    End Operator

    Public Shared Operator >=(x As B2, y As B2) As B2
        System.Console.WriteLine(">=")
        Return x
    End Operator

    Public Shared Operator Like(x As B2, y As B2) As B2
        System.Console.WriteLine("Like")
        Return x
    End Operator

    Public Shared Operator &(x As B2, y As B2) As B2
        System.Console.WriteLine("&")
        Return x
    End Operator

    Public Shared Operator And(x As B2, y As B2) As B2
        System.Console.WriteLine("And")
        Return x
    End Operator

    Public Shared Operator Or(x As B2, y As B2) As B2
        System.Console.WriteLine("Or")
        Return x
    End Operator

    Public Shared Operator Xor(x As B2, y As B2) As B2
        System.Console.WriteLine("Xor")
        Return x
    End Operator

    Public Shared Operator <<(x As B2, y As Integer) As B2
        System.Console.WriteLine("<<")
        Return x
    End Operator

    Public Shared Operator >>(x As B2, y As Integer) As B2
        System.Console.WriteLine(">>")
        Return x
    End Operator
End Class

Module Module1

    Sub Main()'BIND:"Sub Main()"
        Dim x, y As New B2()
        Dim r As B2
        r = x + y
        r = x - y
        r = x * y
        r = x / y
        r = x \ y
        r = x Mod y
        r = x ^ y
        r = x = y
        r = x <> y
        r = x < y
        r = x > y
        r = x <= y
        r = x >= y
        r = x Like y
        r = x & y
        r = x And y
        r = x Or y
        r = x Xor y
        r = x << 2
        r = x >> 3
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IBlockOperation (24 statements, 3 locals) (OperationKind.Block, Type: null) (Syntax: 'Sub Main()' ... End Sub')
  Locals: Local_1: x As B2
    Local_2: y As B2
    Local_3: r As B2
  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim x, y As New B2()')
    IVariableDeclarationOperation (2 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'x, y As New B2()')
      Declarators:
          IVariableDeclaratorOperation (Symbol: x As B2) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'x')
            Initializer: 
              null
          IVariableDeclaratorOperation (Symbol: y As B2) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'y')
            Initializer: 
              null
      Initializer: 
        IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: 'As New B2()')
          IObjectCreationOperation (Constructor: Sub B2..ctor()) (OperationKind.ObjectCreation, Type: B2) (Syntax: 'New B2()')
            Arguments(0)
            Initializer: 
              null
  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim r As B2')
    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'r As B2')
      Declarators:
          IVariableDeclaratorOperation (Symbol: r As B2) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'r')
            Initializer: 
              null
      Initializer: 
        null
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'r = x + y')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: B2, IsImplicit) (Syntax: 'r = x + y')
        Left: 
          ILocalReferenceOperation: r (OperationKind.LocalReference, Type: B2) (Syntax: 'r')
        Right: 
          IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperatorMethod: Function B2.op_Addition(x As B2, y As B2) As B2) (OperationKind.BinaryOperator, Type: B2) (Syntax: 'x + y')
            Left: 
              ILocalReferenceOperation: x (OperationKind.LocalReference, Type: B2) (Syntax: 'x')
            Right: 
              ILocalReferenceOperation: y (OperationKind.LocalReference, Type: B2) (Syntax: 'y')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'r = x - y')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: B2, IsImplicit) (Syntax: 'r = x - y')
        Left: 
          ILocalReferenceOperation: r (OperationKind.LocalReference, Type: B2) (Syntax: 'r')
        Right: 
          IBinaryOperation (BinaryOperatorKind.Subtract, Checked) (OperatorMethod: Function B2.op_Subtraction(x As B2, y As B2) As B2) (OperationKind.BinaryOperator, Type: B2) (Syntax: 'x - y')
            Left: 
              ILocalReferenceOperation: x (OperationKind.LocalReference, Type: B2) (Syntax: 'x')
            Right: 
              ILocalReferenceOperation: y (OperationKind.LocalReference, Type: B2) (Syntax: 'y')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'r = x * y')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: B2, IsImplicit) (Syntax: 'r = x * y')
        Left: 
          ILocalReferenceOperation: r (OperationKind.LocalReference, Type: B2) (Syntax: 'r')
        Right: 
          IBinaryOperation (BinaryOperatorKind.Multiply, Checked) (OperatorMethod: Function B2.op_Multiply(x As B2, y As B2) As B2) (OperationKind.BinaryOperator, Type: B2) (Syntax: 'x * y')
            Left: 
              ILocalReferenceOperation: x (OperationKind.LocalReference, Type: B2) (Syntax: 'x')
            Right: 
              ILocalReferenceOperation: y (OperationKind.LocalReference, Type: B2) (Syntax: 'y')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'r = x / y')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: B2, IsImplicit) (Syntax: 'r = x / y')
        Left: 
          ILocalReferenceOperation: r (OperationKind.LocalReference, Type: B2) (Syntax: 'r')
        Right: 
          IBinaryOperation (BinaryOperatorKind.Divide, Checked) (OperatorMethod: Function B2.op_Division(x As B2, y As B2) As B2) (OperationKind.BinaryOperator, Type: B2) (Syntax: 'x / y')
            Left: 
              ILocalReferenceOperation: x (OperationKind.LocalReference, Type: B2) (Syntax: 'x')
            Right: 
              ILocalReferenceOperation: y (OperationKind.LocalReference, Type: B2) (Syntax: 'y')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'r = x \ y')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: B2, IsImplicit) (Syntax: 'r = x \ y')
        Left: 
          ILocalReferenceOperation: r (OperationKind.LocalReference, Type: B2) (Syntax: 'r')
        Right: 
          IBinaryOperation (BinaryOperatorKind.IntegerDivide, Checked) (OperatorMethod: Function B2.op_IntegerDivision(x As B2, y As B2) As B2) (OperationKind.BinaryOperator, Type: B2) (Syntax: 'x \ y')
            Left: 
              ILocalReferenceOperation: x (OperationKind.LocalReference, Type: B2) (Syntax: 'x')
            Right: 
              ILocalReferenceOperation: y (OperationKind.LocalReference, Type: B2) (Syntax: 'y')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'r = x Mod y')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: B2, IsImplicit) (Syntax: 'r = x Mod y')
        Left: 
          ILocalReferenceOperation: r (OperationKind.LocalReference, Type: B2) (Syntax: 'r')
        Right: 
          IBinaryOperation (BinaryOperatorKind.Remainder, Checked) (OperatorMethod: Function B2.op_Modulus(x As B2, y As B2) As B2) (OperationKind.BinaryOperator, Type: B2) (Syntax: 'x Mod y')
            Left: 
              ILocalReferenceOperation: x (OperationKind.LocalReference, Type: B2) (Syntax: 'x')
            Right: 
              ILocalReferenceOperation: y (OperationKind.LocalReference, Type: B2) (Syntax: 'y')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'r = x ^ y')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: B2, IsImplicit) (Syntax: 'r = x ^ y')
        Left: 
          ILocalReferenceOperation: r (OperationKind.LocalReference, Type: B2) (Syntax: 'r')
        Right: 
          IBinaryOperation (BinaryOperatorKind.Power, Checked) (OperatorMethod: Function B2.op_Exponent(x As B2, y As B2) As B2) (OperationKind.BinaryOperator, Type: B2) (Syntax: 'x ^ y')
            Left: 
              ILocalReferenceOperation: x (OperationKind.LocalReference, Type: B2) (Syntax: 'x')
            Right: 
              ILocalReferenceOperation: y (OperationKind.LocalReference, Type: B2) (Syntax: 'y')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'r = x = y')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: B2, IsImplicit) (Syntax: 'r = x = y')
        Left: 
          ILocalReferenceOperation: r (OperationKind.LocalReference, Type: B2) (Syntax: 'r')
        Right: 
          IBinaryOperation (BinaryOperatorKind.Equals, Checked) (OperatorMethod: Function B2.op_Equality(x As B2, y As B2) As B2) (OperationKind.BinaryOperator, Type: B2) (Syntax: 'x = y')
            Left: 
              ILocalReferenceOperation: x (OperationKind.LocalReference, Type: B2) (Syntax: 'x')
            Right: 
              ILocalReferenceOperation: y (OperationKind.LocalReference, Type: B2) (Syntax: 'y')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'r = x <> y')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: B2, IsImplicit) (Syntax: 'r = x <> y')
        Left: 
          ILocalReferenceOperation: r (OperationKind.LocalReference, Type: B2) (Syntax: 'r')
        Right: 
          IBinaryOperation (BinaryOperatorKind.NotEquals, Checked) (OperatorMethod: Function B2.op_Inequality(x As B2, y As B2) As B2) (OperationKind.BinaryOperator, Type: B2) (Syntax: 'x <> y')
            Left: 
              ILocalReferenceOperation: x (OperationKind.LocalReference, Type: B2) (Syntax: 'x')
            Right: 
              ILocalReferenceOperation: y (OperationKind.LocalReference, Type: B2) (Syntax: 'y')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'r = x < y')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: B2, IsImplicit) (Syntax: 'r = x < y')
        Left: 
          ILocalReferenceOperation: r (OperationKind.LocalReference, Type: B2) (Syntax: 'r')
        Right: 
          IBinaryOperation (BinaryOperatorKind.LessThan, Checked) (OperatorMethod: Function B2.op_LessThan(x As B2, y As B2) As B2) (OperationKind.BinaryOperator, Type: B2) (Syntax: 'x < y')
            Left: 
              ILocalReferenceOperation: x (OperationKind.LocalReference, Type: B2) (Syntax: 'x')
            Right: 
              ILocalReferenceOperation: y (OperationKind.LocalReference, Type: B2) (Syntax: 'y')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'r = x > y')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: B2, IsImplicit) (Syntax: 'r = x > y')
        Left: 
          ILocalReferenceOperation: r (OperationKind.LocalReference, Type: B2) (Syntax: 'r')
        Right: 
          IBinaryOperation (BinaryOperatorKind.GreaterThan, Checked) (OperatorMethod: Function B2.op_GreaterThan(x As B2, y As B2) As B2) (OperationKind.BinaryOperator, Type: B2) (Syntax: 'x > y')
            Left: 
              ILocalReferenceOperation: x (OperationKind.LocalReference, Type: B2) (Syntax: 'x')
            Right: 
              ILocalReferenceOperation: y (OperationKind.LocalReference, Type: B2) (Syntax: 'y')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'r = x <= y')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: B2, IsImplicit) (Syntax: 'r = x <= y')
        Left: 
          ILocalReferenceOperation: r (OperationKind.LocalReference, Type: B2) (Syntax: 'r')
        Right: 
          IBinaryOperation (BinaryOperatorKind.LessThanOrEqual, Checked) (OperatorMethod: Function B2.op_LessThanOrEqual(x As B2, y As B2) As B2) (OperationKind.BinaryOperator, Type: B2) (Syntax: 'x <= y')
            Left: 
              ILocalReferenceOperation: x (OperationKind.LocalReference, Type: B2) (Syntax: 'x')
            Right: 
              ILocalReferenceOperation: y (OperationKind.LocalReference, Type: B2) (Syntax: 'y')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'r = x >= y')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: B2, IsImplicit) (Syntax: 'r = x >= y')
        Left: 
          ILocalReferenceOperation: r (OperationKind.LocalReference, Type: B2) (Syntax: 'r')
        Right: 
          IBinaryOperation (BinaryOperatorKind.GreaterThanOrEqual, Checked) (OperatorMethod: Function B2.op_GreaterThanOrEqual(x As B2, y As B2) As B2) (OperationKind.BinaryOperator, Type: B2) (Syntax: 'x >= y')
            Left: 
              ILocalReferenceOperation: x (OperationKind.LocalReference, Type: B2) (Syntax: 'x')
            Right: 
              ILocalReferenceOperation: y (OperationKind.LocalReference, Type: B2) (Syntax: 'y')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'r = x Like y')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: B2, IsImplicit) (Syntax: 'r = x Like y')
        Left: 
          ILocalReferenceOperation: r (OperationKind.LocalReference, Type: B2) (Syntax: 'r')
        Right: 
          IBinaryOperation (BinaryOperatorKind.Like, Checked) (OperatorMethod: Function B2.op_Like(x As B2, y As B2) As B2) (OperationKind.BinaryOperator, Type: B2) (Syntax: 'x Like y')
            Left: 
              ILocalReferenceOperation: x (OperationKind.LocalReference, Type: B2) (Syntax: 'x')
            Right: 
              ILocalReferenceOperation: y (OperationKind.LocalReference, Type: B2) (Syntax: 'y')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'r = x & y')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: B2, IsImplicit) (Syntax: 'r = x & y')
        Left: 
          ILocalReferenceOperation: r (OperationKind.LocalReference, Type: B2) (Syntax: 'r')
        Right: 
          IBinaryOperation (BinaryOperatorKind.Concatenate, Checked) (OperatorMethod: Function B2.op_Concatenate(x As B2, y As B2) As B2) (OperationKind.BinaryOperator, Type: B2) (Syntax: 'x & y')
            Left: 
              ILocalReferenceOperation: x (OperationKind.LocalReference, Type: B2) (Syntax: 'x')
            Right: 
              ILocalReferenceOperation: y (OperationKind.LocalReference, Type: B2) (Syntax: 'y')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'r = x And y')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: B2, IsImplicit) (Syntax: 'r = x And y')
        Left: 
          ILocalReferenceOperation: r (OperationKind.LocalReference, Type: B2) (Syntax: 'r')
        Right: 
          IBinaryOperation (BinaryOperatorKind.And, Checked) (OperatorMethod: Function B2.op_BitwiseAnd(x As B2, y As B2) As B2) (OperationKind.BinaryOperator, Type: B2) (Syntax: 'x And y')
            Left: 
              ILocalReferenceOperation: x (OperationKind.LocalReference, Type: B2) (Syntax: 'x')
            Right: 
              ILocalReferenceOperation: y (OperationKind.LocalReference, Type: B2) (Syntax: 'y')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'r = x Or y')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: B2, IsImplicit) (Syntax: 'r = x Or y')
        Left: 
          ILocalReferenceOperation: r (OperationKind.LocalReference, Type: B2) (Syntax: 'r')
        Right: 
          IBinaryOperation (BinaryOperatorKind.Or, Checked) (OperatorMethod: Function B2.op_BitwiseOr(x As B2, y As B2) As B2) (OperationKind.BinaryOperator, Type: B2) (Syntax: 'x Or y')
            Left: 
              ILocalReferenceOperation: x (OperationKind.LocalReference, Type: B2) (Syntax: 'x')
            Right: 
              ILocalReferenceOperation: y (OperationKind.LocalReference, Type: B2) (Syntax: 'y')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'r = x Xor y')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: B2, IsImplicit) (Syntax: 'r = x Xor y')
        Left: 
          ILocalReferenceOperation: r (OperationKind.LocalReference, Type: B2) (Syntax: 'r')
        Right: 
          IBinaryOperation (BinaryOperatorKind.ExclusiveOr, Checked) (OperatorMethod: Function B2.op_ExclusiveOr(x As B2, y As B2) As B2) (OperationKind.BinaryOperator, Type: B2) (Syntax: 'x Xor y')
            Left: 
              ILocalReferenceOperation: x (OperationKind.LocalReference, Type: B2) (Syntax: 'x')
            Right: 
              ILocalReferenceOperation: y (OperationKind.LocalReference, Type: B2) (Syntax: 'y')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'r = x << 2')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: B2, IsImplicit) (Syntax: 'r = x << 2')
        Left: 
          ILocalReferenceOperation: r (OperationKind.LocalReference, Type: B2) (Syntax: 'r')
        Right: 
          IBinaryOperation (BinaryOperatorKind.LeftShift, Checked) (OperatorMethod: Function B2.op_LeftShift(x As B2, y As System.Int32) As B2) (OperationKind.BinaryOperator, Type: B2) (Syntax: 'x << 2')
            Left: 
              ILocalReferenceOperation: x (OperationKind.LocalReference, Type: B2) (Syntax: 'x')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'r = x >> 3')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: B2, IsImplicit) (Syntax: 'r = x >> 3')
        Left: 
          ILocalReferenceOperation: r (OperationKind.LocalReference, Type: B2) (Syntax: 'r')
        Right: 
          IBinaryOperation (BinaryOperatorKind.RightShift, Checked) (OperatorMethod: Function B2.op_RightShift(x As B2, y As System.Int32) As B2) (OperationKind.BinaryOperator, Type: B2) (Syntax: 'x >> 3')
            Left: 
              ILocalReferenceOperation: x (OperationKind.LocalReference, Type: B2) (Syntax: 'x')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
  ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'End Sub')
    Statement: 
      null
  IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'End Sub')
    ReturnedValue: 
      null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TestEqualityBinaryOperators()
            Dim source = <![CDATA[
Class C
    Sub M(c1 As C, c2 As C, r As Boolean)'BIND:"Sub M(c1 As C, c2 As C, r As Boolean)"
        r = c1 Is c2
        r = c1 IsNot c2
        r = DirectCast(c1, Object) = c2
        r = DirectCast(c1, Object) <> c2
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IBlockOperation (6 statements) (OperationKind.Block, Type: null) (Syntax: 'Sub M(c1 As ... End Sub')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'r = c1 Is c2')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'r = c1 Is c2')
        Left: 
          IParameterReferenceOperation: r (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'r')
        Right: 
          IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'c1 Is c2')
            Left: 
              IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'c1')
                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                Operand: 
                  IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C) (Syntax: 'c1')
            Right: 
              IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'c2')
                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                Operand: 
                  IParameterReferenceOperation: c2 (OperationKind.ParameterReference, Type: C) (Syntax: 'c2')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'r = c1 IsNot c2')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'r = c1 IsNot c2')
        Left: 
          IParameterReferenceOperation: r (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'r')
        Right: 
          IBinaryOperation (BinaryOperatorKind.NotEquals) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'c1 IsNot c2')
            Left: 
              IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'c1')
                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                Operand: 
                  IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C) (Syntax: 'c1')
            Right: 
              IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'c2')
                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                Operand: 
                  IParameterReferenceOperation: c2 (OperationKind.ParameterReference, Type: C) (Syntax: 'c2')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'r = DirectC ... bject) = c2')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'r = DirectC ... bject) = c2')
        Left: 
          IParameterReferenceOperation: r (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'r')
        Right: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Boolean, IsImplicit) (Syntax: 'DirectCast( ... bject) = c2')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IBinaryOperation (BinaryOperatorKind.ObjectValueEquals, Checked) (OperationKind.BinaryOperator, Type: System.Object) (Syntax: 'DirectCast( ... bject) = c2')
                Left: 
                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object) (Syntax: 'DirectCast(c1, Object)')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C) (Syntax: 'c1')
                Right: 
                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'c2')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      IParameterReferenceOperation: c2 (OperationKind.ParameterReference, Type: C) (Syntax: 'c2')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'r = DirectC ... ject) <> c2')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'r = DirectC ... ject) <> c2')
        Left: 
          IParameterReferenceOperation: r (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'r')
        Right: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Boolean, IsImplicit) (Syntax: 'DirectCast( ... ject) <> c2')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IBinaryOperation (BinaryOperatorKind.ObjectValueNotEquals, Checked) (OperationKind.BinaryOperator, Type: System.Object) (Syntax: 'DirectCast( ... ject) <> c2')
                Left: 
                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object) (Syntax: 'DirectCast(c1, Object)')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C) (Syntax: 'c1')
                Right: 
                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'c2')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      IParameterReferenceOperation: c2 (OperationKind.ParameterReference, Type: C) (Syntax: 'c2')
  ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'End Sub')
    Statement: 
      null
  IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'End Sub')
    ReturnedValue: 
      null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub LogicalFlow_01()
            Dim source = <![CDATA[
Imports System
Public Class C
    Public Sub M(a As Integer?, b As Integer)'BIND:"Public Sub M(a As Integer?, b As Integer)"
        GetArray()(0) = If(a, b) + b
    End Sub

    Public Function GetArray() As Integer()
        Return Nothing
    End Function
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedOperationTree = <![CDATA[
IBlockOperation (3 statements) (OperationKind.Block, Type: null) (Syntax: 'Public Sub  ... End Sub')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'GetArray()( ... f(a, b) + b')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'GetArray()( ... f(a, b) + b')
        Left: 
          IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: 'GetArray()(0)')
            Array reference: 
              IInvocationOperation ( Function C.GetArray() As System.Int32()) (OperationKind.Invocation, Type: System.Int32()) (Syntax: 'GetArray()')
                Instance Receiver: 
                  IInstanceReferenceOperation (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'GetArray')
                Arguments(0)
            Indices(1):
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
        Right: 
          IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'If(a, b) + b')
            Left: 
              ICoalesceOperation (OperationKind.Coalesce, Type: System.Int32) (Syntax: 'If(a, b)')
                Expression: 
                  IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'a')
                ValueConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  (Identity)
                WhenNull: 
                  IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')
            Right: 
              IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')
  ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'End Sub')
    Statement: 
      null
  IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'End Sub')
    ReturnedValue: 
      null
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)


            Dim expectedGraph = <![CDATA[
Block[0] - Entry
    Statements (0)
    Next Block[1]
Block[1] - Block
    Predecessors (1)
        [0]
    Statements (2)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetArray()(0)')
          Value: 
            IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: 'GetArray()(0)')
              Array reference: 
                IInvocationOperation ( Function C.GetArray() As System.Int32()) (OperationKind.Invocation, Type: System.Int32()) (Syntax: 'GetArray()')
                  Instance Receiver: 
                    IInstanceReferenceOperation (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'GetArray')
                  Arguments(0)
              Indices(1):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'a')

    Jump if Null to Block[3]
        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'a')

    Next Block[2]
Block[2] - Block
    Predecessors (1)
        [1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'a')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'a')
              Arguments(0)

    Next Block[4]
Block[3] - Block
    Predecessors (1)
        [1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')

    Next Block[4]
Block[4] - Block
    Predecessors (2)
        [2]
        [3]
    Statements (3)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'GetArray()( ... f(a, b) + b')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'GetArray()( ... f(a, b) + b')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'GetArray()(0)')
              Right: 
                IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'If(a, b) + b')
                  Left: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(a, b)')
                  Right: 
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')

        ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'End Sub')
          Statement: 
            null

        IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'End Sub')
          ReturnedValue: 
            null

    Next Block[5]
Block[5] - Exit
    Predecessors (1)
        [4]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub LogicalFlow_02()
            Dim source = <![CDATA[
Imports System
Public Class C
    Public Sub M(a As Integer?, b As Integer)'BIND:"Public Sub M(a As Integer?, b As Integer)"
        GetArray()(0) = b + If(a, b)
    End Sub

    Public Function GetArray() As Integer()
        Return Nothing
    End Function
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedOperationTree = <![CDATA[
IBlockOperation (3 statements) (OperationKind.Block, Type: null) (Syntax: 'Public Sub  ... End Sub')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'GetArray()( ...  + If(a, b)')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'GetArray()( ...  + If(a, b)')
        Left: 
          IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: 'GetArray()(0)')
            Array reference: 
              IInvocationOperation ( Function C.GetArray() As System.Int32()) (OperationKind.Invocation, Type: System.Int32()) (Syntax: 'GetArray()')
                Instance Receiver: 
                  IInstanceReferenceOperation (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'GetArray')
                Arguments(0)
            Indices(1):
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
        Right: 
          IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'b + If(a, b)')
            Left: 
              IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')
            Right: 
              ICoalesceOperation (OperationKind.Coalesce, Type: System.Int32) (Syntax: 'If(a, b)')
                Expression: 
                  IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'a')
                ValueConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  (Identity)
                WhenNull: 
                  IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')
  ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'End Sub')
    Statement: 
      null
  IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'End Sub')
    ReturnedValue: 
      null
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)


            Dim expectedGraph = <![CDATA[
Block[0] - Entry
    Statements (0)
    Next Block[1]
Block[1] - Block
    Predecessors (1)
        [0]
    Statements (3)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetArray()(0)')
          Value: 
            IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: 'GetArray()(0)')
              Array reference: 
                IInvocationOperation ( Function C.GetArray() As System.Int32()) (OperationKind.Invocation, Type: System.Int32()) (Syntax: 'GetArray()')
                  Instance Receiver: 
                    IInstanceReferenceOperation (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'GetArray')
                  Arguments(0)
              Indices(1):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')

        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'a')

    Jump if Null to Block[3]
        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'a')

    Next Block[2]
Block[2] - Block
    Predecessors (1)
        [1]
    Statements (1)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'a')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'a')
              Arguments(0)

    Next Block[4]
Block[3] - Block
    Predecessors (1)
        [1]
    Statements (1)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')

    Next Block[4]
Block[4] - Block
    Predecessors (2)
        [2]
        [3]
    Statements (3)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'GetArray()( ...  + If(a, b)')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'GetArray()( ...  + If(a, b)')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'GetArray()(0)')
              Right: 
                IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'b + If(a, b)')
                  Left: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b')
                  Right: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(a, b)')

        ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'End Sub')
          Statement: 
            null

        IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'End Sub')
          ReturnedValue: 
            null

    Next Block[5]
Block[5] - Exit
    Predecessors (1)
        [4]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub LogicalFlow_03()
            Dim source = <![CDATA[
Imports System
Public Class C
    Public Sub M(a As Integer?, b As Integer)'BIND:"Public Sub M(a As Integer?, b As Integer)"
        GetArray()(0) = If(a, b) + If(a, b)
    End Sub

    Public Function GetArray() As Integer()
        Return Nothing
    End Function
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedOperationTree = <![CDATA[
IBlockOperation (3 statements) (OperationKind.Block, Type: null) (Syntax: 'Public Sub  ... End Sub')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'GetArray()( ...  + If(a, b)')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'GetArray()( ...  + If(a, b)')
        Left: 
          IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: 'GetArray()(0)')
            Array reference: 
              IInvocationOperation ( Function C.GetArray() As System.Int32()) (OperationKind.Invocation, Type: System.Int32()) (Syntax: 'GetArray()')
                Instance Receiver: 
                  IInstanceReferenceOperation (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'GetArray')
                Arguments(0)
            Indices(1):
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
        Right: 
          IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'If(a, b) + If(a, b)')
            Left: 
              ICoalesceOperation (OperationKind.Coalesce, Type: System.Int32) (Syntax: 'If(a, b)')
                Expression: 
                  IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'a')
                ValueConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  (Identity)
                WhenNull: 
                  IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')
            Right: 
              ICoalesceOperation (OperationKind.Coalesce, Type: System.Int32) (Syntax: 'If(a, b)')
                Expression: 
                  IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'a')
                ValueConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  (Identity)
                WhenNull: 
                  IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')
  ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'End Sub')
    Statement: 
      null
  IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'End Sub')
    ReturnedValue: 
      null
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)


            Dim expectedGraph = <![CDATA[
Block[0] - Entry
    Statements (0)
    Next Block[1]
Block[1] - Block
    Predecessors (1)
        [0]
    Statements (2)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetArray()(0)')
          Value: 
            IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: 'GetArray()(0)')
              Array reference: 
                IInvocationOperation ( Function C.GetArray() As System.Int32()) (OperationKind.Invocation, Type: System.Int32()) (Syntax: 'GetArray()')
                  Instance Receiver: 
                    IInstanceReferenceOperation (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'GetArray')
                  Arguments(0)
              Indices(1):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'a')

    Jump if Null to Block[3]
        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'a')

    Next Block[2]
Block[2] - Block
    Predecessors (1)
        [1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'a')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'a')
              Arguments(0)

    Next Block[4]
Block[3] - Block
    Predecessors (1)
        [1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')

    Next Block[4]
Block[4] - Block
    Predecessors (2)
        [2]
        [3]
    Statements (1)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'a')

    Jump if Null to Block[6]
        IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'a')

    Next Block[5]
Block[5] - Block
    Predecessors (1)
        [4]
    Statements (1)
        IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'a')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'a')
              Arguments(0)

    Next Block[7]
Block[6] - Block
    Predecessors (1)
        [4]
    Statements (1)
        IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')

    Next Block[7]
Block[7] - Block
    Predecessors (2)
        [5]
        [6]
    Statements (3)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'GetArray()( ...  + If(a, b)')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'GetArray()( ...  + If(a, b)')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'GetArray()(0)')
              Right: 
                IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'If(a, b) + If(a, b)')
                  Left: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(a, b)')
                  Right: 
                    IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(a, b)')

        ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'End Sub')
          Statement: 
            null

        IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'End Sub')
          ReturnedValue: 
            null

    Next Block[8]
Block[8] - Exit
    Predecessors (1)
        [7]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub LogicalFlow_04()
            Dim source = <![CDATA[
Imports System
Public Class C
    Public Sub M(a As C, b As C)'BIND:"Public Sub M(a As C, b As C)"
        GetArray()(0) = If(a, b) + If(a, b)
    End Sub

    Public Function GetArray() As Integer()
        Return Nothing
    End Function

    Public Shared Operator +(c1 As C, c2 As C) As Integer
        Return 0
    End Operator
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedOperationTree = <![CDATA[
IBlockOperation (3 statements) (OperationKind.Block, Type: null) (Syntax: 'Public Sub  ... End Sub')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'GetArray()( ...  + If(a, b)')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'GetArray()( ...  + If(a, b)')
        Left: 
          IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: 'GetArray()(0)')
            Array reference: 
              IInvocationOperation ( Function C.GetArray() As System.Int32()) (OperationKind.Invocation, Type: System.Int32()) (Syntax: 'GetArray()')
                Instance Receiver: 
                  IInstanceReferenceOperation (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'GetArray')
                Arguments(0)
            Indices(1):
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
        Right: 
          IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperatorMethod: Function C.op_Addition(c1 As C, c2 As C) As System.Int32) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'If(a, b) + If(a, b)')
            Left: 
              ICoalesceOperation (OperationKind.Coalesce, Type: C) (Syntax: 'If(a, b)')
                Expression: 
                  IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: C) (Syntax: 'a')
                ValueConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  (Identity)
                WhenNull: 
                  IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: C) (Syntax: 'b')
            Right: 
              ICoalesceOperation (OperationKind.Coalesce, Type: C) (Syntax: 'If(a, b)')
                Expression: 
                  IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: C) (Syntax: 'a')
                ValueConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  (Identity)
                WhenNull: 
                  IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: C) (Syntax: 'b')
  ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'End Sub')
    Statement: 
      null
  IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'End Sub')
    ReturnedValue: 
      null
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)


            Dim expectedGraph = <![CDATA[
Block[0] - Entry
    Statements (0)
    Next Block[1]
Block[1] - Block
    Predecessors (1)
        [0]
    Statements (2)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetArray()(0)')
          Value: 
            IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: 'GetArray()(0)')
              Array reference: 
                IInvocationOperation ( Function C.GetArray() As System.Int32()) (OperationKind.Invocation, Type: System.Int32()) (Syntax: 'GetArray()')
                  Instance Receiver: 
                    IInstanceReferenceOperation (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'GetArray')
                  Arguments(0)
              Indices(1):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: C) (Syntax: 'a')

    Jump if Null to Block[3]
        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'a')

    Next Block[2]
Block[2] - Block
    Predecessors (1)
        [1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'a')

    Next Block[4]
Block[3] - Block
    Predecessors (1)
        [1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: C) (Syntax: 'b')

    Next Block[4]
Block[4] - Block
    Predecessors (2)
        [2]
        [3]
    Statements (1)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: C) (Syntax: 'a')

    Jump if Null to Block[6]
        IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'a')

    Next Block[5]
Block[5] - Block
    Predecessors (1)
        [4]
    Statements (1)
        IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'a')

    Next Block[7]
Block[6] - Block
    Predecessors (1)
        [4]
    Statements (1)
        IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: C) (Syntax: 'b')

    Next Block[7]
Block[7] - Block
    Predecessors (2)
        [5]
        [6]
    Statements (3)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'GetArray()( ...  + If(a, b)')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'GetArray()( ...  + If(a, b)')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'GetArray()(0)')
              Right: 
                IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperatorMethod: Function C.op_Addition(c1 As C, c2 As C) As System.Int32) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'If(a, b) + If(a, b)')
                  Left: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'If(a, b)')
                  Right: 
                    IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'If(a, b)')

        ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'End Sub')
          Statement: 
            null

        IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'End Sub')
          ReturnedValue: 
            null

    Next Block[8]
Block[8] - Exit
    Predecessors (1)
        [7]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub LogicalFlow_05()
            Dim source = <![CDATA[
Imports System
Public Class C
    Public Sub M(a As Integer?, b As Integer)'BIND:"Public Sub M(a As Integer?, b As Integer)"
        GetArray()(0) = b - If(a, b)
    End Sub

    Public Function GetArray() As Integer()
        Return Nothing
    End Function
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedOperationTree = <![CDATA[
IBlockOperation (3 statements) (OperationKind.Block, Type: null) (Syntax: 'Public Sub  ... End Sub')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'GetArray()( ...  - If(a, b)')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'GetArray()( ...  - If(a, b)')
        Left: 
          IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: 'GetArray()(0)')
            Array reference: 
              IInvocationOperation ( Function C.GetArray() As System.Int32()) (OperationKind.Invocation, Type: System.Int32()) (Syntax: 'GetArray()')
                Instance Receiver: 
                  IInstanceReferenceOperation (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'GetArray')
                Arguments(0)
            Indices(1):
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
        Right: 
          IBinaryOperation (BinaryOperatorKind.Subtract, Checked) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'b - If(a, b)')
            Left: 
              IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')
            Right: 
              ICoalesceOperation (OperationKind.Coalesce, Type: System.Int32) (Syntax: 'If(a, b)')
                Expression: 
                  IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'a')
                ValueConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  (Identity)
                WhenNull: 
                  IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')
  ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'End Sub')
    Statement: 
      null
  IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'End Sub')
    ReturnedValue: 
      null
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)


            Dim expectedGraph = <![CDATA[
Block[0] - Entry
    Statements (0)
    Next Block[1]
Block[1] - Block
    Predecessors (1)
        [0]
    Statements (3)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetArray()(0)')
          Value: 
            IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: 'GetArray()(0)')
              Array reference: 
                IInvocationOperation ( Function C.GetArray() As System.Int32()) (OperationKind.Invocation, Type: System.Int32()) (Syntax: 'GetArray()')
                  Instance Receiver: 
                    IInstanceReferenceOperation (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'GetArray')
                  Arguments(0)
              Indices(1):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')

        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'a')

    Jump if Null to Block[3]
        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'a')

    Next Block[2]
Block[2] - Block
    Predecessors (1)
        [1]
    Statements (1)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'a')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'a')
              Arguments(0)

    Next Block[4]
Block[3] - Block
    Predecessors (1)
        [1]
    Statements (1)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')

    Next Block[4]
Block[4] - Block
    Predecessors (2)
        [2]
        [3]
    Statements (3)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'GetArray()( ...  - If(a, b)')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'GetArray()( ...  - If(a, b)')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'GetArray()(0)')
              Right: 
                IBinaryOperation (BinaryOperatorKind.Subtract, Checked) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'b - If(a, b)')
                  Left: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b')
                  Right: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(a, b)')

        ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'End Sub')
          Statement: 
            null

        IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'End Sub')
          ReturnedValue: 
            null

    Next Block[5]
Block[5] - Exit
    Predecessors (1)
        [4]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub LogicalFlow_06()
            Dim source = <![CDATA[
Imports System
Public Class C
    Public Sub M(a As Integer?, b As Integer)'BIND:"Public Sub M(a As Integer?, b As Integer)"
        GetArray()(0) = b << If(a, b)
    End Sub

    Public Function GetArray() As Integer()
        Return Nothing
    End Function
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedOperationTree = <![CDATA[
IBlockOperation (3 statements) (OperationKind.Block, Type: null) (Syntax: 'Public Sub  ... End Sub')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'GetArray()( ... << If(a, b)')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'GetArray()( ... << If(a, b)')
        Left: 
          IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: 'GetArray()(0)')
            Array reference: 
              IInvocationOperation ( Function C.GetArray() As System.Int32()) (OperationKind.Invocation, Type: System.Int32()) (Syntax: 'GetArray()')
                Instance Receiver: 
                  IInstanceReferenceOperation (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'GetArray')
                Arguments(0)
            Indices(1):
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
        Right: 
          IBinaryOperation (BinaryOperatorKind.LeftShift, Checked) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'b << If(a, b)')
            Left: 
              IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')
            Right: 
              ICoalesceOperation (OperationKind.Coalesce, Type: System.Int32) (Syntax: 'If(a, b)')
                Expression: 
                  IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'a')
                ValueConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  (Identity)
                WhenNull: 
                  IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')
  ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'End Sub')
    Statement: 
      null
  IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'End Sub')
    ReturnedValue: 
      null
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)


            Dim expectedGraph = <![CDATA[
Block[0] - Entry
    Statements (0)
    Next Block[1]
Block[1] - Block
    Predecessors (1)
        [0]
    Statements (3)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetArray()(0)')
          Value: 
            IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: 'GetArray()(0)')
              Array reference: 
                IInvocationOperation ( Function C.GetArray() As System.Int32()) (OperationKind.Invocation, Type: System.Int32()) (Syntax: 'GetArray()')
                  Instance Receiver: 
                    IInstanceReferenceOperation (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'GetArray')
                  Arguments(0)
              Indices(1):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')

        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'a')

    Jump if Null to Block[3]
        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'a')

    Next Block[2]
Block[2] - Block
    Predecessors (1)
        [1]
    Statements (1)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'a')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'a')
              Arguments(0)

    Next Block[4]
Block[3] - Block
    Predecessors (1)
        [1]
    Statements (1)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')

    Next Block[4]
Block[4] - Block
    Predecessors (2)
        [2]
        [3]
    Statements (3)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'GetArray()( ... << If(a, b)')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'GetArray()( ... << If(a, b)')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'GetArray()(0)')
              Right: 
                IBinaryOperation (BinaryOperatorKind.LeftShift, Checked) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'b << If(a, b)')
                  Left: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b')
                  Right: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(a, b)')

        ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'End Sub')
          Statement: 
            null

        IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'End Sub')
          ReturnedValue: 
            null

    Next Block[5]
Block[5] - Exit
    Predecessors (1)
        [4]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub LogicalFlow_07()
            Dim source = <![CDATA[
Imports System
Public Class C
    Public Sub M(a As Integer?, b As Integer)'BIND:"Public Sub M(a As Integer?, b As Integer)"
        GetArray()(0) = b >> If(a, b)
    End Sub

    Public Function GetArray() As Integer()
        Return Nothing
    End Function
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedOperationTree = <![CDATA[
IBlockOperation (3 statements) (OperationKind.Block, Type: null) (Syntax: 'Public Sub  ... End Sub')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'GetArray()( ... >> If(a, b)')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'GetArray()( ... >> If(a, b)')
        Left: 
          IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: 'GetArray()(0)')
            Array reference: 
              IInvocationOperation ( Function C.GetArray() As System.Int32()) (OperationKind.Invocation, Type: System.Int32()) (Syntax: 'GetArray()')
                Instance Receiver: 
                  IInstanceReferenceOperation (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'GetArray')
                Arguments(0)
            Indices(1):
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
        Right: 
          IBinaryOperation (BinaryOperatorKind.RightShift, Checked) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'b >> If(a, b)')
            Left: 
              IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')
            Right: 
              ICoalesceOperation (OperationKind.Coalesce, Type: System.Int32) (Syntax: 'If(a, b)')
                Expression: 
                  IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'a')
                ValueConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  (Identity)
                WhenNull: 
                  IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')
  ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'End Sub')
    Statement: 
      null
  IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'End Sub')
    ReturnedValue: 
      null
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)


            Dim expectedGraph = <![CDATA[
Block[0] - Entry
    Statements (0)
    Next Block[1]
Block[1] - Block
    Predecessors (1)
        [0]
    Statements (3)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetArray()(0)')
          Value: 
            IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: 'GetArray()(0)')
              Array reference: 
                IInvocationOperation ( Function C.GetArray() As System.Int32()) (OperationKind.Invocation, Type: System.Int32()) (Syntax: 'GetArray()')
                  Instance Receiver: 
                    IInstanceReferenceOperation (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'GetArray')
                  Arguments(0)
              Indices(1):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')

        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'a')

    Jump if Null to Block[3]
        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'a')

    Next Block[2]
Block[2] - Block
    Predecessors (1)
        [1]
    Statements (1)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'a')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'a')
              Arguments(0)

    Next Block[4]
Block[3] - Block
    Predecessors (1)
        [1]
    Statements (1)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')

    Next Block[4]
Block[4] - Block
    Predecessors (2)
        [2]
        [3]
    Statements (3)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'GetArray()( ... >> If(a, b)')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'GetArray()( ... >> If(a, b)')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'GetArray()(0)')
              Right: 
                IBinaryOperation (BinaryOperatorKind.RightShift, Checked) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'b >> If(a, b)')
                  Left: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b')
                  Right: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(a, b)')

        ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'End Sub')
          Statement: 
            null

        IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'End Sub')
          ReturnedValue: 
            null

    Next Block[5]
Block[5] - Exit
    Predecessors (1)
        [4]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub LogicalFlow_08()
            Dim source = <![CDATA[
Imports System
Public Class C
    Public Sub M(a As Integer?, b As Integer)'BIND:"Public Sub M(a As Integer?, b As Integer)"
        GetArray()(0) = b * If(a, b)
    End Sub

    Public Function GetArray() As Integer()
        Return Nothing
    End Function
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedOperationTree = <![CDATA[
IBlockOperation (3 statements) (OperationKind.Block, Type: null) (Syntax: 'Public Sub  ... End Sub')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'GetArray()( ...  * If(a, b)')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'GetArray()( ...  * If(a, b)')
        Left: 
          IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: 'GetArray()(0)')
            Array reference: 
              IInvocationOperation ( Function C.GetArray() As System.Int32()) (OperationKind.Invocation, Type: System.Int32()) (Syntax: 'GetArray()')
                Instance Receiver: 
                  IInstanceReferenceOperation (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'GetArray')
                Arguments(0)
            Indices(1):
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
        Right: 
          IBinaryOperation (BinaryOperatorKind.Multiply, Checked) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'b * If(a, b)')
            Left: 
              IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')
            Right: 
              ICoalesceOperation (OperationKind.Coalesce, Type: System.Int32) (Syntax: 'If(a, b)')
                Expression: 
                  IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'a')
                ValueConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  (Identity)
                WhenNull: 
                  IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')
  ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'End Sub')
    Statement: 
      null
  IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'End Sub')
    ReturnedValue: 
      null
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)


            Dim expectedGraph = <![CDATA[
Block[0] - Entry
    Statements (0)
    Next Block[1]
Block[1] - Block
    Predecessors (1)
        [0]
    Statements (3)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetArray()(0)')
          Value: 
            IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: 'GetArray()(0)')
              Array reference: 
                IInvocationOperation ( Function C.GetArray() As System.Int32()) (OperationKind.Invocation, Type: System.Int32()) (Syntax: 'GetArray()')
                  Instance Receiver: 
                    IInstanceReferenceOperation (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'GetArray')
                  Arguments(0)
              Indices(1):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')

        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'a')

    Jump if Null to Block[3]
        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'a')

    Next Block[2]
Block[2] - Block
    Predecessors (1)
        [1]
    Statements (1)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'a')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'a')
              Arguments(0)

    Next Block[4]
Block[3] - Block
    Predecessors (1)
        [1]
    Statements (1)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')

    Next Block[4]
Block[4] - Block
    Predecessors (2)
        [2]
        [3]
    Statements (3)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'GetArray()( ...  * If(a, b)')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'GetArray()( ...  * If(a, b)')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'GetArray()(0)')
              Right: 
                IBinaryOperation (BinaryOperatorKind.Multiply, Checked) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'b * If(a, b)')
                  Left: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b')
                  Right: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(a, b)')

        ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'End Sub')
          Statement: 
            null

        IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'End Sub')
          ReturnedValue: 
            null

    Next Block[5]
Block[5] - Exit
    Predecessors (1)
        [4]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub LogicalFlow_09()
            Dim source = <![CDATA[
Imports System
Public Class C
    Public Sub M(a As Integer?, b As Integer)'BIND:"Public Sub M(a As Integer?, b As Integer)"
        GetArray()(0) = b / If(a, b)
    End Sub

    Public Function GetArray() As Integer()
        Return Nothing
    End Function
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedOperationTree = <![CDATA[
IBlockOperation (3 statements) (OperationKind.Block, Type: null) (Syntax: 'Public Sub  ... End Sub')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'GetArray()( ...  / If(a, b)')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'GetArray()( ...  / If(a, b)')
        Left: 
          IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: 'GetArray()(0)')
            Array reference: 
              IInvocationOperation ( Function C.GetArray() As System.Int32()) (OperationKind.Invocation, Type: System.Int32()) (Syntax: 'GetArray()')
                Instance Receiver: 
                  IInstanceReferenceOperation (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'GetArray')
                Arguments(0)
            Indices(1):
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
        Right: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsImplicit) (Syntax: 'b / If(a, b)')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IBinaryOperation (BinaryOperatorKind.Divide, Checked) (OperationKind.BinaryOperator, Type: System.Double) (Syntax: 'b / If(a, b)')
                Left: 
                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Double, IsImplicit) (Syntax: 'b')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')
                Right: 
                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Double, IsImplicit) (Syntax: 'If(a, b)')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ICoalesceOperation (OperationKind.Coalesce, Type: System.Int32) (Syntax: 'If(a, b)')
                        Expression: 
                          IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'a')
                        ValueConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          (Identity)
                        WhenNull: 
                          IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')
  ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'End Sub')
    Statement: 
      null
  IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'End Sub')
    ReturnedValue: 
      null
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)


            Dim expectedGraph = <![CDATA[
Block[0] - Entry
    Statements (0)
    Next Block[1]
Block[1] - Block
    Predecessors (1)
        [0]
    Statements (3)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetArray()(0)')
          Value: 
            IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: 'GetArray()(0)')
              Array reference: 
                IInvocationOperation ( Function C.GetArray() As System.Int32()) (OperationKind.Invocation, Type: System.Int32()) (Syntax: 'GetArray()')
                  Instance Receiver: 
                    IInstanceReferenceOperation (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'GetArray')
                  Arguments(0)
              Indices(1):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Double, IsImplicit) (Syntax: 'b')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                (WideningNumeric)
              Operand: 
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')

        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'a')

    Jump if Null to Block[3]
        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'a')

    Next Block[2]
Block[2] - Block
    Predecessors (1)
        [1]
    Statements (1)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'a')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'a')
              Arguments(0)

    Next Block[4]
Block[3] - Block
    Predecessors (1)
        [1]
    Statements (1)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')

    Next Block[4]
Block[4] - Block
    Predecessors (2)
        [2]
        [3]
    Statements (3)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'GetArray()( ...  / If(a, b)')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'GetArray()( ...  / If(a, b)')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'GetArray()(0)')
              Right: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsImplicit) (Syntax: 'b / If(a, b)')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (NarrowingNumeric)
                  Operand: 
                    IBinaryOperation (BinaryOperatorKind.Divide, Checked) (OperationKind.BinaryOperator, Type: System.Double) (Syntax: 'b / If(a, b)')
                      Left: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Double, IsImplicit) (Syntax: 'b')
                      Right: 
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Double, IsImplicit) (Syntax: 'If(a, b)')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            (WideningNumeric)
                          Operand: 
                            IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(a, b)')

        ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'End Sub')
          Statement: 
            null

        IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'End Sub')
          ReturnedValue: 
            null

    Next Block[5]
Block[5] - Exit
    Predecessors (1)
        [4]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub LogicalFlow_10()
            Dim source = <![CDATA[
Imports System
Public Class C
    Public Sub M(a As Integer?, b As Integer)'BIND:"Public Sub M(a As Integer?, b As Integer)"
        GetArray()(0) = b Mod If(a, b)
    End Sub

    Public Function GetArray() As Integer()
        Return Nothing
    End Function
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedOperationTree = <![CDATA[
IBlockOperation (3 statements) (OperationKind.Block, Type: null) (Syntax: 'Public Sub  ... End Sub')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'GetArray()( ... od If(a, b)')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'GetArray()( ... od If(a, b)')
        Left: 
          IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: 'GetArray()(0)')
            Array reference: 
              IInvocationOperation ( Function C.GetArray() As System.Int32()) (OperationKind.Invocation, Type: System.Int32()) (Syntax: 'GetArray()')
                Instance Receiver: 
                  IInstanceReferenceOperation (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'GetArray')
                Arguments(0)
            Indices(1):
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
        Right: 
          IBinaryOperation (BinaryOperatorKind.Remainder, Checked) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'b Mod If(a, b)')
            Left: 
              IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')
            Right: 
              ICoalesceOperation (OperationKind.Coalesce, Type: System.Int32) (Syntax: 'If(a, b)')
                Expression: 
                  IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'a')
                ValueConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  (Identity)
                WhenNull: 
                  IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')
  ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'End Sub')
    Statement: 
      null
  IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'End Sub')
    ReturnedValue: 
      null
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)


            Dim expectedGraph = <![CDATA[
Block[0] - Entry
    Statements (0)
    Next Block[1]
Block[1] - Block
    Predecessors (1)
        [0]
    Statements (3)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetArray()(0)')
          Value: 
            IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: 'GetArray()(0)')
              Array reference: 
                IInvocationOperation ( Function C.GetArray() As System.Int32()) (OperationKind.Invocation, Type: System.Int32()) (Syntax: 'GetArray()')
                  Instance Receiver: 
                    IInstanceReferenceOperation (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'GetArray')
                  Arguments(0)
              Indices(1):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')

        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'a')

    Jump if Null to Block[3]
        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'a')

    Next Block[2]
Block[2] - Block
    Predecessors (1)
        [1]
    Statements (1)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'a')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'a')
              Arguments(0)

    Next Block[4]
Block[3] - Block
    Predecessors (1)
        [1]
    Statements (1)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')

    Next Block[4]
Block[4] - Block
    Predecessors (2)
        [2]
        [3]
    Statements (3)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'GetArray()( ... od If(a, b)')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'GetArray()( ... od If(a, b)')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'GetArray()(0)')
              Right: 
                IBinaryOperation (BinaryOperatorKind.Remainder, Checked) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'b Mod If(a, b)')
                  Left: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b')
                  Right: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(a, b)')

        ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'End Sub')
          Statement: 
            null

        IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'End Sub')
          ReturnedValue: 
            null

    Next Block[5]
Block[5] - Exit
    Predecessors (1)
        [4]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedGraph, expectedDiagnostics)
        End Sub
    End Class
End Namespace
