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
IBinaryOperatorExpression (BinaryOperatorKind.Add, IsLifted, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 'x + y')
  Left: 
    IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 'x')
  Right: 
    IParameterReferenceExpression: y (OperationKind.ParameterReferenceExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 'y')
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
IBinaryOperatorExpression (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'x + y')
  Left: 
    IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'x')
  Right: 
    IParameterReferenceExpression: y (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'y')
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
IBinaryOperatorExpression (BinaryOperatorKind.And, IsLifted, Checked) (OperatorMethod: Function C.op_BitwiseAnd(c1 As C, cs As C) As C) (OperationKind.BinaryOperatorExpression, Type: System.Nullable(Of C)) (Syntax: 'x And y')
  Left: 
    IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Nullable(Of C)) (Syntax: 'x')
  Right: 
    IParameterReferenceExpression: y (OperationKind.ParameterReferenceExpression, Type: System.Nullable(Of C)) (Syntax: 'y')
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
IBinaryOperatorExpression (BinaryOperatorKind.ConditionalAnd, IsLifted) (OperatorMethod: Function C.op_BitwiseAnd(c1 As C, cs As C) As C) (OperationKind.BinaryOperatorExpression, Type: System.Nullable(Of C)) (Syntax: 'x AndAlso y')
  Left: 
    IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Nullable(Of C)) (Syntax: 'x')
  Right: 
    IParameterReferenceExpression: y (OperationKind.ParameterReferenceExpression, Type: System.Nullable(Of C)) (Syntax: 'y')
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
IBinaryOperatorExpression (BinaryOperatorKind.And, Checked) (OperatorMethod: Function C.op_BitwiseAnd(c1 As C, cs As C) As C) (OperationKind.BinaryOperatorExpression, Type: C) (Syntax: 'x And y')
  Left: 
    IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: C) (Syntax: 'x')
  Right: 
    IParameterReferenceExpression: y (OperationKind.ParameterReferenceExpression, Type: C) (Syntax: 'y')
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
IBinaryOperatorExpression (BinaryOperatorKind.ConditionalAnd) (OperatorMethod: Function C.op_BitwiseAnd(c1 As C, cs As C) As C) (OperationKind.BinaryOperatorExpression, Type: C) (Syntax: 'x AndAlso y')
  Left: 
    IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: C) (Syntax: 'x')
  Right: 
    IParameterReferenceExpression: y (OperationKind.ParameterReferenceExpression, Type: C) (Syntax: 'y')
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
IBinaryOperatorExpression (BinaryOperatorKind.Add, IsLifted, Checked) (OperatorMethod: Function C.op_Addition(c1 As C, c2 As C) As C) (OperationKind.BinaryOperatorExpression, Type: System.Nullable(Of C)) (Syntax: 'x + y')
  Left: 
    IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Nullable(Of C)) (Syntax: 'x')
  Right: 
    IParameterReferenceExpression: y (OperationKind.ParameterReferenceExpression, Type: System.Nullable(Of C)) (Syntax: 'y')
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
IBinaryOperatorExpression (BinaryOperatorKind.Add, Checked) (OperatorMethod: Function C.op_Addition(c1 As C, c2 As C) As C) (OperationKind.BinaryOperatorExpression, Type: C) (Syntax: 'x + y')
  Left: 
    IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: C) (Syntax: 'x')
  Right: 
    IParameterReferenceExpression: y (OperationKind.ParameterReferenceExpression, Type: C) (Syntax: 'y')
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
IBlockStatement (26 statements, 3 locals) (OperationKind.BlockStatement) (Syntax: 'Sub Main()' ... End Sub')
  Locals: Local_1: x As System.Int32
    Local_2: y As System.Int32
    Local_3: r As System.Int32
  IVariableDeclarationGroup (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim x, y As New Integer')
    IMultiVariableDeclaration (2 declarations) (OperationKind.MultiVariableDeclaration) (Syntax: 'x, y As New Integer')
      Declarations:
          ISingleVariableDeclaration (Symbol: x As System.Int32) (OperationKind.SingleVariableDeclaration) (Syntax: 'x')
            Initializer: 
              null
          ISingleVariableDeclaration (Symbol: y As System.Int32) (OperationKind.SingleVariableDeclaration) (Syntax: 'y')
            Initializer: 
              null
      Initializer: 
        IVariableInitializer (OperationKind.VariableInitializer) (Syntax: 'As New Integer')
          IObjectCreationExpression (Constructor: Sub System.Int32..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Int32) (Syntax: 'New Integer')
            Arguments(0)
            Initializer: 
              null
  IVariableDeclarationGroup (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim r As Integer')
    IMultiVariableDeclaration (1 declarations) (OperationKind.MultiVariableDeclaration) (Syntax: 'r As Integer')
      Declarations:
          ISingleVariableDeclaration (Symbol: r As System.Int32) (OperationKind.SingleVariableDeclaration) (Syntax: 'r')
            Initializer: 
              null
      Initializer: 
        null
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'r = x + y')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, IsImplicit) (Syntax: 'r = x + y')
        Left: 
          ILocalReferenceExpression: r (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'r')
        Right: 
          IBinaryOperatorExpression (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'x + y')
            Left: 
              ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
            Right: 
              ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'r = x - y')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, IsImplicit) (Syntax: 'r = x - y')
        Left: 
          ILocalReferenceExpression: r (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'r')
        Right: 
          IBinaryOperatorExpression (BinaryOperatorKind.Subtract, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'x - y')
            Left: 
              ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
            Right: 
              ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'r = x * y')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, IsImplicit) (Syntax: 'r = x * y')
        Left: 
          ILocalReferenceExpression: r (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'r')
        Right: 
          IBinaryOperatorExpression (BinaryOperatorKind.Multiply, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'x * y')
            Left: 
              ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
            Right: 
              ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'r = x / y')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, IsImplicit) (Syntax: 'r = x / y')
        Left: 
          ILocalReferenceExpression: r (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'r')
        Right: 
          IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, IsImplicit) (Syntax: 'x / y')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IBinaryOperatorExpression (BinaryOperatorKind.Divide, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Double) (Syntax: 'x / y')
                Left: 
                  IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Double, IsImplicit) (Syntax: 'x')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
                Right: 
                  IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Double, IsImplicit) (Syntax: 'y')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'r = x \ y')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, IsImplicit) (Syntax: 'r = x \ y')
        Left: 
          ILocalReferenceExpression: r (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'r')
        Right: 
          IBinaryOperatorExpression (BinaryOperatorKind.IntegerDivide, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'x \ y')
            Left: 
              ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
            Right: 
              ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'r = x Mod y')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, IsImplicit) (Syntax: 'r = x Mod y')
        Left: 
          ILocalReferenceExpression: r (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'r')
        Right: 
          IBinaryOperatorExpression (BinaryOperatorKind.Remainder, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'x Mod y')
            Left: 
              ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
            Right: 
              ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'r = x ^ y')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, IsImplicit) (Syntax: 'r = x ^ y')
        Left: 
          ILocalReferenceExpression: r (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'r')
        Right: 
          IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, IsImplicit) (Syntax: 'x ^ y')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IBinaryOperatorExpression (BinaryOperatorKind.Power, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Double) (Syntax: 'x ^ y')
                Left: 
                  IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Double, IsImplicit) (Syntax: 'x')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
                Right: 
                  IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Double, IsImplicit) (Syntax: 'y')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'r = x = y')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, IsImplicit) (Syntax: 'r = x = y')
        Left: 
          ILocalReferenceExpression: r (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'r')
        Right: 
          IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, IsImplicit) (Syntax: 'x = y')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IBinaryOperatorExpression (BinaryOperatorKind.Equals, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'x = y')
                Left: 
                  ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
                Right: 
                  ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'r = x <> y')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, IsImplicit) (Syntax: 'r = x <> y')
        Left: 
          ILocalReferenceExpression: r (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'r')
        Right: 
          IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, IsImplicit) (Syntax: 'x <> y')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IBinaryOperatorExpression (BinaryOperatorKind.NotEquals, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'x <> y')
                Left: 
                  ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
                Right: 
                  ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'r = x < y')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, IsImplicit) (Syntax: 'r = x < y')
        Left: 
          ILocalReferenceExpression: r (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'r')
        Right: 
          IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, IsImplicit) (Syntax: 'x < y')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IBinaryOperatorExpression (BinaryOperatorKind.LessThan, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'x < y')
                Left: 
                  ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
                Right: 
                  ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'r = x > y')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, IsImplicit) (Syntax: 'r = x > y')
        Left: 
          ILocalReferenceExpression: r (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'r')
        Right: 
          IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, IsImplicit) (Syntax: 'x > y')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IBinaryOperatorExpression (BinaryOperatorKind.GreaterThan, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'x > y')
                Left: 
                  ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
                Right: 
                  ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'r = x <= y')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, IsImplicit) (Syntax: 'r = x <= y')
        Left: 
          ILocalReferenceExpression: r (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'r')
        Right: 
          IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, IsImplicit) (Syntax: 'x <= y')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IBinaryOperatorExpression (BinaryOperatorKind.LessThanOrEqual, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'x <= y')
                Left: 
                  ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
                Right: 
                  ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'r = x >= y')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, IsImplicit) (Syntax: 'r = x >= y')
        Left: 
          ILocalReferenceExpression: r (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'r')
        Right: 
          IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, IsImplicit) (Syntax: 'x >= y')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IBinaryOperatorExpression (BinaryOperatorKind.GreaterThanOrEqual, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'x >= y')
                Left: 
                  ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
                Right: 
                  ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'r = x Like y')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, IsImplicit) (Syntax: 'r = x Like y')
        Left: 
          ILocalReferenceExpression: r (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'r')
        Right: 
          IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, IsImplicit) (Syntax: 'x Like y')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IBinaryOperatorExpression (BinaryOperatorKind.Like, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'x Like y')
                Left: 
                  IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.String, IsImplicit) (Syntax: 'x')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
                Right: 
                  IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.String, IsImplicit) (Syntax: 'y')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'r = x & y')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, IsImplicit) (Syntax: 'r = x & y')
        Left: 
          ILocalReferenceExpression: r (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'r')
        Right: 
          IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, IsImplicit) (Syntax: 'x & y')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IBinaryOperatorExpression (BinaryOperatorKind.Concatenate, Checked) (OperationKind.BinaryOperatorExpression, Type: System.String) (Syntax: 'x & y')
                Left: 
                  IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.String, IsImplicit) (Syntax: 'x')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
                Right: 
                  IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.String, IsImplicit) (Syntax: 'y')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'r = x And y')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, IsImplicit) (Syntax: 'r = x And y')
        Left: 
          ILocalReferenceExpression: r (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'r')
        Right: 
          IBinaryOperatorExpression (BinaryOperatorKind.And, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'x And y')
            Left: 
              ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
            Right: 
              ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'r = x Or y')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, IsImplicit) (Syntax: 'r = x Or y')
        Left: 
          ILocalReferenceExpression: r (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'r')
        Right: 
          IBinaryOperatorExpression (BinaryOperatorKind.Or, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'x Or y')
            Left: 
              ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
            Right: 
              ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'r = x Xor y')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, IsImplicit) (Syntax: 'r = x Xor y')
        Left: 
          ILocalReferenceExpression: r (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'r')
        Right: 
          IBinaryOperatorExpression (BinaryOperatorKind.ExclusiveOr, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'x Xor y')
            Left: 
              ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
            Right: 
              ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'r = x << 2')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, IsImplicit) (Syntax: 'r = x << 2')
        Left: 
          ILocalReferenceExpression: r (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'r')
        Right: 
          IBinaryOperatorExpression (BinaryOperatorKind.LeftShift, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'x << 2')
            Left: 
              ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
            Right: 
              ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'r = x >> 3')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, IsImplicit) (Syntax: 'r = x >> 3')
        Left: 
          ILocalReferenceExpression: r (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'r')
        Right: 
          IBinaryOperatorExpression (BinaryOperatorKind.RightShift, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'x >> 3')
            Left: 
              ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
            Right: 
              ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3) (Syntax: '3')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'r = DirectC ... Object) = y')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, IsImplicit) (Syntax: 'r = DirectC ... Object) = y')
        Left: 
          ILocalReferenceExpression: r (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'r')
        Right: 
          IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, IsImplicit) (Syntax: 'DirectCast( ... Object) = y')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IBinaryOperatorExpression (BinaryOperatorKind.ObjectValueEquals, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Object) (Syntax: 'DirectCast( ... Object) = y')
                Left: 
                  IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: 'DirectCast(x, Object)')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
                Right: 
                  IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object, IsImplicit) (Syntax: 'y')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'r = DirectC ... bject) <> y')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, IsImplicit) (Syntax: 'r = DirectC ... bject) <> y')
        Left: 
          ILocalReferenceExpression: r (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'r')
        Right: 
          IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, IsImplicit) (Syntax: 'DirectCast( ... bject) <> y')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IBinaryOperatorExpression (BinaryOperatorKind.ObjectValueNotEquals, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Object) (Syntax: 'DirectCast( ... bject) <> y')
                Left: 
                  IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: 'DirectCast(x, Object)')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
                Right: 
                  IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object, IsImplicit) (Syntax: 'y')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
  ILabeledStatement (Label: exit) (OperationKind.LabeledStatement, IsImplicit) (Syntax: 'End Sub')
    Statement: 
      null
  IReturnStatement (OperationKind.ReturnStatement, IsImplicit) (Syntax: 'End Sub')
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
IBlockStatement (26 statements, 3 locals) (OperationKind.BlockStatement) (Syntax: 'Sub Main()' ... End Sub')
  Locals: Local_1: x As System.Int32
    Local_2: y As System.Int32
    Local_3: r As System.Int32
  IVariableDeclarationGroup (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim x, y As New Integer')
    IMultiVariableDeclaration (2 declarations) (OperationKind.MultiVariableDeclaration) (Syntax: 'x, y As New Integer')
      Declarations:
          ISingleVariableDeclaration (Symbol: x As System.Int32) (OperationKind.SingleVariableDeclaration) (Syntax: 'x')
            Initializer: 
              null
          ISingleVariableDeclaration (Symbol: y As System.Int32) (OperationKind.SingleVariableDeclaration) (Syntax: 'y')
            Initializer: 
              null
      Initializer: 
        IVariableInitializer (OperationKind.VariableInitializer) (Syntax: 'As New Integer')
          IObjectCreationExpression (Constructor: Sub System.Int32..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Int32) (Syntax: 'New Integer')
            Arguments(0)
            Initializer: 
              null
  IVariableDeclarationGroup (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim r As Integer')
    IMultiVariableDeclaration (1 declarations) (OperationKind.MultiVariableDeclaration) (Syntax: 'r As Integer')
      Declarations:
          ISingleVariableDeclaration (Symbol: r As System.Int32) (OperationKind.SingleVariableDeclaration) (Syntax: 'r')
            Initializer: 
              null
      Initializer: 
        null
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'r = x + y')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, IsImplicit) (Syntax: 'r = x + y')
        Left: 
          ILocalReferenceExpression: r (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'r')
        Right: 
          IBinaryOperatorExpression (BinaryOperatorKind.Add) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'x + y')
            Left: 
              ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
            Right: 
              ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'r = x - y')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, IsImplicit) (Syntax: 'r = x - y')
        Left: 
          ILocalReferenceExpression: r (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'r')
        Right: 
          IBinaryOperatorExpression (BinaryOperatorKind.Subtract) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'x - y')
            Left: 
              ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
            Right: 
              ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'r = x * y')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, IsImplicit) (Syntax: 'r = x * y')
        Left: 
          ILocalReferenceExpression: r (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'r')
        Right: 
          IBinaryOperatorExpression (BinaryOperatorKind.Multiply) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'x * y')
            Left: 
              ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
            Right: 
              ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'r = x / y')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, IsImplicit) (Syntax: 'r = x / y')
        Left: 
          ILocalReferenceExpression: r (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'r')
        Right: 
          IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, IsImplicit) (Syntax: 'x / y')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IBinaryOperatorExpression (BinaryOperatorKind.Divide) (OperationKind.BinaryOperatorExpression, Type: System.Double) (Syntax: 'x / y')
                Left: 
                  IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Double, IsImplicit) (Syntax: 'x')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
                Right: 
                  IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Double, IsImplicit) (Syntax: 'y')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'r = x \ y')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, IsImplicit) (Syntax: 'r = x \ y')
        Left: 
          ILocalReferenceExpression: r (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'r')
        Right: 
          IBinaryOperatorExpression (BinaryOperatorKind.IntegerDivide) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'x \ y')
            Left: 
              ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
            Right: 
              ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'r = x Mod y')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, IsImplicit) (Syntax: 'r = x Mod y')
        Left: 
          ILocalReferenceExpression: r (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'r')
        Right: 
          IBinaryOperatorExpression (BinaryOperatorKind.Remainder) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'x Mod y')
            Left: 
              ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
            Right: 
              ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'r = x ^ y')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, IsImplicit) (Syntax: 'r = x ^ y')
        Left: 
          ILocalReferenceExpression: r (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'r')
        Right: 
          IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, IsImplicit) (Syntax: 'x ^ y')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IBinaryOperatorExpression (BinaryOperatorKind.Power) (OperationKind.BinaryOperatorExpression, Type: System.Double) (Syntax: 'x ^ y')
                Left: 
                  IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Double, IsImplicit) (Syntax: 'x')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
                Right: 
                  IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Double, IsImplicit) (Syntax: 'y')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'r = x = y')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, IsImplicit) (Syntax: 'r = x = y')
        Left: 
          ILocalReferenceExpression: r (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'r')
        Right: 
          IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, IsImplicit) (Syntax: 'x = y')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IBinaryOperatorExpression (BinaryOperatorKind.Equals) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'x = y')
                Left: 
                  ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
                Right: 
                  ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'r = x <> y')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, IsImplicit) (Syntax: 'r = x <> y')
        Left: 
          ILocalReferenceExpression: r (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'r')
        Right: 
          IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, IsImplicit) (Syntax: 'x <> y')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IBinaryOperatorExpression (BinaryOperatorKind.NotEquals) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'x <> y')
                Left: 
                  ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
                Right: 
                  ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'r = x < y')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, IsImplicit) (Syntax: 'r = x < y')
        Left: 
          ILocalReferenceExpression: r (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'r')
        Right: 
          IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, IsImplicit) (Syntax: 'x < y')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IBinaryOperatorExpression (BinaryOperatorKind.LessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'x < y')
                Left: 
                  ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
                Right: 
                  ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'r = x > y')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, IsImplicit) (Syntax: 'r = x > y')
        Left: 
          ILocalReferenceExpression: r (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'r')
        Right: 
          IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, IsImplicit) (Syntax: 'x > y')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IBinaryOperatorExpression (BinaryOperatorKind.GreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'x > y')
                Left: 
                  ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
                Right: 
                  ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'r = x <= y')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, IsImplicit) (Syntax: 'r = x <= y')
        Left: 
          ILocalReferenceExpression: r (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'r')
        Right: 
          IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, IsImplicit) (Syntax: 'x <= y')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IBinaryOperatorExpression (BinaryOperatorKind.LessThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'x <= y')
                Left: 
                  ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
                Right: 
                  ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'r = x >= y')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, IsImplicit) (Syntax: 'r = x >= y')
        Left: 
          ILocalReferenceExpression: r (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'r')
        Right: 
          IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, IsImplicit) (Syntax: 'x >= y')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IBinaryOperatorExpression (BinaryOperatorKind.GreaterThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'x >= y')
                Left: 
                  ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
                Right: 
                  ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'r = x Like y')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, IsImplicit) (Syntax: 'r = x Like y')
        Left: 
          ILocalReferenceExpression: r (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'r')
        Right: 
          IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, IsImplicit) (Syntax: 'x Like y')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IBinaryOperatorExpression (BinaryOperatorKind.Like) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'x Like y')
                Left: 
                  IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.String, IsImplicit) (Syntax: 'x')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
                Right: 
                  IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.String, IsImplicit) (Syntax: 'y')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'r = x & y')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, IsImplicit) (Syntax: 'r = x & y')
        Left: 
          ILocalReferenceExpression: r (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'r')
        Right: 
          IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, IsImplicit) (Syntax: 'x & y')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IBinaryOperatorExpression (BinaryOperatorKind.Concatenate) (OperationKind.BinaryOperatorExpression, Type: System.String) (Syntax: 'x & y')
                Left: 
                  IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.String, IsImplicit) (Syntax: 'x')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
                Right: 
                  IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.String, IsImplicit) (Syntax: 'y')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'r = x And y')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, IsImplicit) (Syntax: 'r = x And y')
        Left: 
          ILocalReferenceExpression: r (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'r')
        Right: 
          IBinaryOperatorExpression (BinaryOperatorKind.And) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'x And y')
            Left: 
              ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
            Right: 
              ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'r = x Or y')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, IsImplicit) (Syntax: 'r = x Or y')
        Left: 
          ILocalReferenceExpression: r (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'r')
        Right: 
          IBinaryOperatorExpression (BinaryOperatorKind.Or) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'x Or y')
            Left: 
              ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
            Right: 
              ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'r = x Xor y')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, IsImplicit) (Syntax: 'r = x Xor y')
        Left: 
          ILocalReferenceExpression: r (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'r')
        Right: 
          IBinaryOperatorExpression (BinaryOperatorKind.ExclusiveOr) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'x Xor y')
            Left: 
              ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
            Right: 
              ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'r = x << 2')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, IsImplicit) (Syntax: 'r = x << 2')
        Left: 
          ILocalReferenceExpression: r (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'r')
        Right: 
          IBinaryOperatorExpression (BinaryOperatorKind.LeftShift) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'x << 2')
            Left: 
              ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
            Right: 
              ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'r = x >> 3')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, IsImplicit) (Syntax: 'r = x >> 3')
        Left: 
          ILocalReferenceExpression: r (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'r')
        Right: 
          IBinaryOperatorExpression (BinaryOperatorKind.RightShift) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'x >> 3')
            Left: 
              ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
            Right: 
              ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3) (Syntax: '3')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'r = DirectC ... Object) = y')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, IsImplicit) (Syntax: 'r = DirectC ... Object) = y')
        Left: 
          ILocalReferenceExpression: r (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'r')
        Right: 
          IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, IsImplicit) (Syntax: 'DirectCast( ... Object) = y')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IBinaryOperatorExpression (BinaryOperatorKind.ObjectValueEquals) (OperationKind.BinaryOperatorExpression, Type: System.Object) (Syntax: 'DirectCast( ... Object) = y')
                Left: 
                  IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: 'DirectCast(x, Object)')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
                Right: 
                  IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object, IsImplicit) (Syntax: 'y')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'r = DirectC ... bject) <> y')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, IsImplicit) (Syntax: 'r = DirectC ... bject) <> y')
        Left: 
          ILocalReferenceExpression: r (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'r')
        Right: 
          IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, IsImplicit) (Syntax: 'DirectCast( ... bject) <> y')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IBinaryOperatorExpression (BinaryOperatorKind.ObjectValueNotEquals) (OperationKind.BinaryOperatorExpression, Type: System.Object) (Syntax: 'DirectCast( ... bject) <> y')
                Left: 
                  IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: 'DirectCast(x, Object)')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
                Right: 
                  IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object, IsImplicit) (Syntax: 'y')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
  ILabeledStatement (Label: exit) (OperationKind.LabeledStatement, IsImplicit) (Syntax: 'End Sub')
    Statement: 
      null
  IReturnStatement (OperationKind.ReturnStatement, IsImplicit) (Syntax: 'End Sub')
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
IBlockStatement (10 statements) (OperationKind.BlockStatement) (Syntax: 'Sub M(x As  ... End Sub')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'r = x = y')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, IsImplicit) (Syntax: 'r = x = y')
        Left: 
          IParameterReferenceExpression: r (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'r')
        Right: 
          IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, IsImplicit) (Syntax: 'x = y')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IBinaryOperatorExpression (BinaryOperatorKind.Equals, Checked, CompareText) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'x = y')
                Left: 
                  IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.String) (Syntax: 'x')
                Right: 
                  IParameterReferenceExpression: y (OperationKind.ParameterReferenceExpression, Type: System.String) (Syntax: 'y')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'r = x <> y')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, IsImplicit) (Syntax: 'r = x <> y')
        Left: 
          IParameterReferenceExpression: r (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'r')
        Right: 
          IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, IsImplicit) (Syntax: 'x <> y')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IBinaryOperatorExpression (BinaryOperatorKind.NotEquals, Checked, CompareText) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'x <> y')
                Left: 
                  IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.String) (Syntax: 'x')
                Right: 
                  IParameterReferenceExpression: y (OperationKind.ParameterReferenceExpression, Type: System.String) (Syntax: 'y')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'r = x < y')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, IsImplicit) (Syntax: 'r = x < y')
        Left: 
          IParameterReferenceExpression: r (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'r')
        Right: 
          IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, IsImplicit) (Syntax: 'x < y')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IBinaryOperatorExpression (BinaryOperatorKind.LessThan, Checked, CompareText) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'x < y')
                Left: 
                  IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.String) (Syntax: 'x')
                Right: 
                  IParameterReferenceExpression: y (OperationKind.ParameterReferenceExpression, Type: System.String) (Syntax: 'y')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'r = x > y')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, IsImplicit) (Syntax: 'r = x > y')
        Left: 
          IParameterReferenceExpression: r (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'r')
        Right: 
          IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, IsImplicit) (Syntax: 'x > y')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IBinaryOperatorExpression (BinaryOperatorKind.GreaterThan, Checked, CompareText) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'x > y')
                Left: 
                  IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.String) (Syntax: 'x')
                Right: 
                  IParameterReferenceExpression: y (OperationKind.ParameterReferenceExpression, Type: System.String) (Syntax: 'y')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'r = x <= y')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, IsImplicit) (Syntax: 'r = x <= y')
        Left: 
          IParameterReferenceExpression: r (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'r')
        Right: 
          IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, IsImplicit) (Syntax: 'x <= y')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IBinaryOperatorExpression (BinaryOperatorKind.LessThanOrEqual, Checked, CompareText) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'x <= y')
                Left: 
                  IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.String) (Syntax: 'x')
                Right: 
                  IParameterReferenceExpression: y (OperationKind.ParameterReferenceExpression, Type: System.String) (Syntax: 'y')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'r = x >= y')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, IsImplicit) (Syntax: 'r = x >= y')
        Left: 
          IParameterReferenceExpression: r (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'r')
        Right: 
          IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, IsImplicit) (Syntax: 'x >= y')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IBinaryOperatorExpression (BinaryOperatorKind.GreaterThanOrEqual, Checked, CompareText) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'x >= y')
                Left: 
                  IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.String) (Syntax: 'x')
                Right: 
                  IParameterReferenceExpression: y (OperationKind.ParameterReferenceExpression, Type: System.String) (Syntax: 'y')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'r = DirectC ... Object) = y')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, IsImplicit) (Syntax: 'r = DirectC ... Object) = y')
        Left: 
          IParameterReferenceExpression: r (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'r')
        Right: 
          IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, IsImplicit) (Syntax: 'DirectCast( ... Object) = y')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IBinaryOperatorExpression (BinaryOperatorKind.ObjectValueEquals, Checked, CompareText) (OperationKind.BinaryOperatorExpression, Type: System.Object) (Syntax: 'DirectCast( ... Object) = y')
                Left: 
                  IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: 'DirectCast(x, Object)')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.String) (Syntax: 'x')
                Right: 
                  IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object, IsImplicit) (Syntax: 'y')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      IParameterReferenceExpression: y (OperationKind.ParameterReferenceExpression, Type: System.String) (Syntax: 'y')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'r = DirectC ... bject) <> y')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, IsImplicit) (Syntax: 'r = DirectC ... bject) <> y')
        Left: 
          IParameterReferenceExpression: r (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'r')
        Right: 
          IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, IsImplicit) (Syntax: 'DirectCast( ... bject) <> y')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IBinaryOperatorExpression (BinaryOperatorKind.ObjectValueNotEquals, Checked, CompareText) (OperationKind.BinaryOperatorExpression, Type: System.Object) (Syntax: 'DirectCast( ... bject) <> y')
                Left: 
                  IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: 'DirectCast(x, Object)')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.String) (Syntax: 'x')
                Right: 
                  IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object, IsImplicit) (Syntax: 'y')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      IParameterReferenceExpression: y (OperationKind.ParameterReferenceExpression, Type: System.String) (Syntax: 'y')
  ILabeledStatement (Label: exit) (OperationKind.LabeledStatement, IsImplicit) (Syntax: 'End Sub')
    Statement: 
      null
  IReturnStatement (OperationKind.ReturnStatement, IsImplicit) (Syntax: 'End Sub')
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
IBlockStatement (12 statements, 2 locals) (OperationKind.BlockStatement) (Syntax: 'Sub Main()' ... End Sub')
  Locals: Local_1: x As System.Int32
    Local_2: y As System.Int32
  IVariableDeclarationGroup (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim x, y As New Integer')
    IMultiVariableDeclaration (2 declarations) (OperationKind.MultiVariableDeclaration) (Syntax: 'x, y As New Integer')
      Declarations:
          ISingleVariableDeclaration (Symbol: x As System.Int32) (OperationKind.SingleVariableDeclaration) (Syntax: 'x')
            Initializer: 
              null
          ISingleVariableDeclaration (Symbol: y As System.Int32) (OperationKind.SingleVariableDeclaration) (Syntax: 'y')
            Initializer: 
              null
      Initializer: 
        IVariableInitializer (OperationKind.VariableInitializer) (Syntax: 'As New Integer')
          IObjectCreationExpression (Constructor: Sub System.Int32..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Int32) (Syntax: 'New Integer')
            Arguments(0)
            Initializer: 
              null
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'x += y')
    Expression: 
      ICompoundAssignmentExpression (BinaryOperatorKind.Add, Checked) (OperationKind.CompoundAssignmentExpression, Type: System.Int32, IsImplicit) (Syntax: 'x += y')
        Left: 
          ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
        Right: 
          ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'x -= y')
    Expression: 
      ICompoundAssignmentExpression (BinaryOperatorKind.Subtract, Checked) (OperationKind.CompoundAssignmentExpression, Type: System.Int32, IsImplicit) (Syntax: 'x -= y')
        Left: 
          ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
        Right: 
          ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'x *= y')
    Expression: 
      ICompoundAssignmentExpression (BinaryOperatorKind.Multiply, Checked) (OperationKind.CompoundAssignmentExpression, Type: System.Int32, IsImplicit) (Syntax: 'x *= y')
        Left: 
          ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
        Right: 
          ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'x /= y')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, IsImplicit) (Syntax: 'x /= y')
        Left: 
          ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
        Right: 
          IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, IsImplicit) (Syntax: 'x /= y')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IBinaryOperatorExpression (BinaryOperatorKind.Divide, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Double, IsImplicit) (Syntax: 'x /= y')
                Left: 
                  IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Double, IsImplicit) (Syntax: 'x')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      IOperation:  (OperationKind.None, IsImplicit) (Syntax: 'x')
                Right: 
                  IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Double, IsImplicit) (Syntax: 'y')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'x \= y')
    Expression: 
      ICompoundAssignmentExpression (BinaryOperatorKind.IntegerDivide, Checked) (OperationKind.CompoundAssignmentExpression, Type: System.Int32, IsImplicit) (Syntax: 'x \= y')
        Left: 
          ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
        Right: 
          ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'x ^= y')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, IsImplicit) (Syntax: 'x ^= y')
        Left: 
          ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
        Right: 
          IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, IsImplicit) (Syntax: 'x ^= y')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IBinaryOperatorExpression (BinaryOperatorKind.Power, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Double, IsImplicit) (Syntax: 'x ^= y')
                Left: 
                  IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Double, IsImplicit) (Syntax: 'x')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      IOperation:  (OperationKind.None, IsImplicit) (Syntax: 'x')
                Right: 
                  IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Double, IsImplicit) (Syntax: 'y')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'x &= y')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, IsImplicit) (Syntax: 'x &= y')
        Left: 
          ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
        Right: 
          IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, IsImplicit) (Syntax: 'x &= y')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IBinaryOperatorExpression (BinaryOperatorKind.Concatenate, Checked) (OperationKind.BinaryOperatorExpression, Type: System.String, IsImplicit) (Syntax: 'x &= y')
                Left: 
                  IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.String, IsImplicit) (Syntax: 'x')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      IOperation:  (OperationKind.None, IsImplicit) (Syntax: 'x')
                Right: 
                  IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.String, IsImplicit) (Syntax: 'y')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'x <<= 2')
    Expression: 
      ICompoundAssignmentExpression (BinaryOperatorKind.LeftShift, Checked) (OperationKind.CompoundAssignmentExpression, Type: System.Int32, IsImplicit) (Syntax: 'x <<= 2')
        Left: 
          ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
        Right: 
          ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'x >>= 3')
    Expression: 
      ICompoundAssignmentExpression (BinaryOperatorKind.RightShift, Checked) (OperationKind.CompoundAssignmentExpression, Type: System.Int32, IsImplicit) (Syntax: 'x >>= 3')
        Left: 
          ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
        Right: 
          ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3) (Syntax: '3')
  ILabeledStatement (Label: exit) (OperationKind.LabeledStatement, IsImplicit) (Syntax: 'End Sub')
    Statement: 
      null
  IReturnStatement (OperationKind.ReturnStatement, IsImplicit) (Syntax: 'End Sub')
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
IBlockStatement (12 statements, 2 locals) (OperationKind.BlockStatement) (Syntax: 'Sub Main()' ... End Sub')
  Locals: Local_1: x As System.Int32
    Local_2: y As System.Int32
  IVariableDeclarationGroup (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim x, y As New Integer')
    IMultiVariableDeclaration (2 declarations) (OperationKind.MultiVariableDeclaration) (Syntax: 'x, y As New Integer')
      Declarations:
          ISingleVariableDeclaration (Symbol: x As System.Int32) (OperationKind.SingleVariableDeclaration) (Syntax: 'x')
            Initializer: 
              null
          ISingleVariableDeclaration (Symbol: y As System.Int32) (OperationKind.SingleVariableDeclaration) (Syntax: 'y')
            Initializer: 
              null
      Initializer: 
        IVariableInitializer (OperationKind.VariableInitializer) (Syntax: 'As New Integer')
          IObjectCreationExpression (Constructor: Sub System.Int32..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Int32) (Syntax: 'New Integer')
            Arguments(0)
            Initializer: 
              null
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'x += y')
    Expression: 
      ICompoundAssignmentExpression (BinaryOperatorKind.Add) (OperationKind.CompoundAssignmentExpression, Type: System.Int32, IsImplicit) (Syntax: 'x += y')
        Left: 
          ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
        Right: 
          ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'x -= y')
    Expression: 
      ICompoundAssignmentExpression (BinaryOperatorKind.Subtract) (OperationKind.CompoundAssignmentExpression, Type: System.Int32, IsImplicit) (Syntax: 'x -= y')
        Left: 
          ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
        Right: 
          ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'x *= y')
    Expression: 
      ICompoundAssignmentExpression (BinaryOperatorKind.Multiply) (OperationKind.CompoundAssignmentExpression, Type: System.Int32, IsImplicit) (Syntax: 'x *= y')
        Left: 
          ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
        Right: 
          ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'x /= y')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, IsImplicit) (Syntax: 'x /= y')
        Left: 
          ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
        Right: 
          IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, IsImplicit) (Syntax: 'x /= y')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IBinaryOperatorExpression (BinaryOperatorKind.Divide) (OperationKind.BinaryOperatorExpression, Type: System.Double, IsImplicit) (Syntax: 'x /= y')
                Left: 
                  IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Double, IsImplicit) (Syntax: 'x')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      IOperation:  (OperationKind.None, IsImplicit) (Syntax: 'x')
                Right: 
                  IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Double, IsImplicit) (Syntax: 'y')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'x \= y')
    Expression: 
      ICompoundAssignmentExpression (BinaryOperatorKind.IntegerDivide) (OperationKind.CompoundAssignmentExpression, Type: System.Int32, IsImplicit) (Syntax: 'x \= y')
        Left: 
          ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
        Right: 
          ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'x ^= y')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, IsImplicit) (Syntax: 'x ^= y')
        Left: 
          ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
        Right: 
          IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, IsImplicit) (Syntax: 'x ^= y')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IBinaryOperatorExpression (BinaryOperatorKind.Power) (OperationKind.BinaryOperatorExpression, Type: System.Double, IsImplicit) (Syntax: 'x ^= y')
                Left: 
                  IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Double, IsImplicit) (Syntax: 'x')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      IOperation:  (OperationKind.None, IsImplicit) (Syntax: 'x')
                Right: 
                  IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Double, IsImplicit) (Syntax: 'y')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'x &= y')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, IsImplicit) (Syntax: 'x &= y')
        Left: 
          ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
        Right: 
          IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, IsImplicit) (Syntax: 'x &= y')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IBinaryOperatorExpression (BinaryOperatorKind.Concatenate) (OperationKind.BinaryOperatorExpression, Type: System.String, IsImplicit) (Syntax: 'x &= y')
                Left: 
                  IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.String, IsImplicit) (Syntax: 'x')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      IOperation:  (OperationKind.None, IsImplicit) (Syntax: 'x')
                Right: 
                  IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.String, IsImplicit) (Syntax: 'y')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'x <<= 2')
    Expression: 
      ICompoundAssignmentExpression (BinaryOperatorKind.LeftShift) (OperationKind.CompoundAssignmentExpression, Type: System.Int32, IsImplicit) (Syntax: 'x <<= 2')
        Left: 
          ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
        Right: 
          ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'x >>= 3')
    Expression: 
      ICompoundAssignmentExpression (BinaryOperatorKind.RightShift) (OperationKind.CompoundAssignmentExpression, Type: System.Int32, IsImplicit) (Syntax: 'x >>= 3')
        Left: 
          ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
        Right: 
          ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3) (Syntax: '3')
  ILabeledStatement (Label: exit) (OperationKind.LabeledStatement, IsImplicit) (Syntax: 'End Sub')
    Statement: 
      null
  IReturnStatement (OperationKind.ReturnStatement, IsImplicit) (Syntax: 'End Sub')
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
IBlockStatement (24 statements, 3 locals) (OperationKind.BlockStatement) (Syntax: 'Sub Main()' ... End Sub')
  Locals: Local_1: x As B2
    Local_2: y As B2
    Local_3: r As B2
  IVariableDeclarationGroup (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim x, y As New B2()')
    IMultiVariableDeclaration (2 declarations) (OperationKind.MultiVariableDeclaration) (Syntax: 'x, y As New B2()')
      Declarations:
          ISingleVariableDeclaration (Symbol: x As B2) (OperationKind.SingleVariableDeclaration) (Syntax: 'x')
            Initializer: 
              null
          ISingleVariableDeclaration (Symbol: y As B2) (OperationKind.SingleVariableDeclaration) (Syntax: 'y')
            Initializer: 
              null
      Initializer: 
        IVariableInitializer (OperationKind.VariableInitializer) (Syntax: 'As New B2()')
          IObjectCreationExpression (Constructor: Sub B2..ctor()) (OperationKind.ObjectCreationExpression, Type: B2) (Syntax: 'New B2()')
            Arguments(0)
            Initializer: 
              null
  IVariableDeclarationGroup (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim r As B2')
    IMultiVariableDeclaration (1 declarations) (OperationKind.MultiVariableDeclaration) (Syntax: 'r As B2')
      Declarations:
          ISingleVariableDeclaration (Symbol: r As B2) (OperationKind.SingleVariableDeclaration) (Syntax: 'r')
            Initializer: 
              null
      Initializer: 
        null
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'r = x + y')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: B2, IsImplicit) (Syntax: 'r = x + y')
        Left: 
          ILocalReferenceExpression: r (OperationKind.LocalReferenceExpression, Type: B2) (Syntax: 'r')
        Right: 
          IBinaryOperatorExpression (BinaryOperatorKind.Add, Checked) (OperatorMethod: Function B2.op_Addition(x As B2, y As B2) As B2) (OperationKind.BinaryOperatorExpression, Type: B2) (Syntax: 'x + y')
            Left: 
              ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: B2) (Syntax: 'x')
            Right: 
              ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: B2) (Syntax: 'y')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'r = x - y')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: B2, IsImplicit) (Syntax: 'r = x - y')
        Left: 
          ILocalReferenceExpression: r (OperationKind.LocalReferenceExpression, Type: B2) (Syntax: 'r')
        Right: 
          IBinaryOperatorExpression (BinaryOperatorKind.Subtract, Checked) (OperatorMethod: Function B2.op_Subtraction(x As B2, y As B2) As B2) (OperationKind.BinaryOperatorExpression, Type: B2) (Syntax: 'x - y')
            Left: 
              ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: B2) (Syntax: 'x')
            Right: 
              ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: B2) (Syntax: 'y')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'r = x * y')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: B2, IsImplicit) (Syntax: 'r = x * y')
        Left: 
          ILocalReferenceExpression: r (OperationKind.LocalReferenceExpression, Type: B2) (Syntax: 'r')
        Right: 
          IBinaryOperatorExpression (BinaryOperatorKind.Multiply, Checked) (OperatorMethod: Function B2.op_Multiply(x As B2, y As B2) As B2) (OperationKind.BinaryOperatorExpression, Type: B2) (Syntax: 'x * y')
            Left: 
              ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: B2) (Syntax: 'x')
            Right: 
              ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: B2) (Syntax: 'y')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'r = x / y')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: B2, IsImplicit) (Syntax: 'r = x / y')
        Left: 
          ILocalReferenceExpression: r (OperationKind.LocalReferenceExpression, Type: B2) (Syntax: 'r')
        Right: 
          IBinaryOperatorExpression (BinaryOperatorKind.Divide, Checked) (OperatorMethod: Function B2.op_Division(x As B2, y As B2) As B2) (OperationKind.BinaryOperatorExpression, Type: B2) (Syntax: 'x / y')
            Left: 
              ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: B2) (Syntax: 'x')
            Right: 
              ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: B2) (Syntax: 'y')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'r = x \ y')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: B2, IsImplicit) (Syntax: 'r = x \ y')
        Left: 
          ILocalReferenceExpression: r (OperationKind.LocalReferenceExpression, Type: B2) (Syntax: 'r')
        Right: 
          IBinaryOperatorExpression (BinaryOperatorKind.IntegerDivide, Checked) (OperatorMethod: Function B2.op_IntegerDivision(x As B2, y As B2) As B2) (OperationKind.BinaryOperatorExpression, Type: B2) (Syntax: 'x \ y')
            Left: 
              ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: B2) (Syntax: 'x')
            Right: 
              ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: B2) (Syntax: 'y')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'r = x Mod y')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: B2, IsImplicit) (Syntax: 'r = x Mod y')
        Left: 
          ILocalReferenceExpression: r (OperationKind.LocalReferenceExpression, Type: B2) (Syntax: 'r')
        Right: 
          IBinaryOperatorExpression (BinaryOperatorKind.Remainder, Checked) (OperatorMethod: Function B2.op_Modulus(x As B2, y As B2) As B2) (OperationKind.BinaryOperatorExpression, Type: B2) (Syntax: 'x Mod y')
            Left: 
              ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: B2) (Syntax: 'x')
            Right: 
              ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: B2) (Syntax: 'y')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'r = x ^ y')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: B2, IsImplicit) (Syntax: 'r = x ^ y')
        Left: 
          ILocalReferenceExpression: r (OperationKind.LocalReferenceExpression, Type: B2) (Syntax: 'r')
        Right: 
          IBinaryOperatorExpression (BinaryOperatorKind.Power, Checked) (OperatorMethod: Function B2.op_Exponent(x As B2, y As B2) As B2) (OperationKind.BinaryOperatorExpression, Type: B2) (Syntax: 'x ^ y')
            Left: 
              ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: B2) (Syntax: 'x')
            Right: 
              ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: B2) (Syntax: 'y')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'r = x = y')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: B2, IsImplicit) (Syntax: 'r = x = y')
        Left: 
          ILocalReferenceExpression: r (OperationKind.LocalReferenceExpression, Type: B2) (Syntax: 'r')
        Right: 
          IBinaryOperatorExpression (BinaryOperatorKind.Equals, Checked) (OperatorMethod: Function B2.op_Equality(x As B2, y As B2) As B2) (OperationKind.BinaryOperatorExpression, Type: B2) (Syntax: 'x = y')
            Left: 
              ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: B2) (Syntax: 'x')
            Right: 
              ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: B2) (Syntax: 'y')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'r = x <> y')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: B2, IsImplicit) (Syntax: 'r = x <> y')
        Left: 
          ILocalReferenceExpression: r (OperationKind.LocalReferenceExpression, Type: B2) (Syntax: 'r')
        Right: 
          IBinaryOperatorExpression (BinaryOperatorKind.NotEquals, Checked) (OperatorMethod: Function B2.op_Inequality(x As B2, y As B2) As B2) (OperationKind.BinaryOperatorExpression, Type: B2) (Syntax: 'x <> y')
            Left: 
              ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: B2) (Syntax: 'x')
            Right: 
              ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: B2) (Syntax: 'y')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'r = x < y')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: B2, IsImplicit) (Syntax: 'r = x < y')
        Left: 
          ILocalReferenceExpression: r (OperationKind.LocalReferenceExpression, Type: B2) (Syntax: 'r')
        Right: 
          IBinaryOperatorExpression (BinaryOperatorKind.LessThan, Checked) (OperatorMethod: Function B2.op_LessThan(x As B2, y As B2) As B2) (OperationKind.BinaryOperatorExpression, Type: B2) (Syntax: 'x < y')
            Left: 
              ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: B2) (Syntax: 'x')
            Right: 
              ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: B2) (Syntax: 'y')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'r = x > y')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: B2, IsImplicit) (Syntax: 'r = x > y')
        Left: 
          ILocalReferenceExpression: r (OperationKind.LocalReferenceExpression, Type: B2) (Syntax: 'r')
        Right: 
          IBinaryOperatorExpression (BinaryOperatorKind.GreaterThan, Checked) (OperatorMethod: Function B2.op_GreaterThan(x As B2, y As B2) As B2) (OperationKind.BinaryOperatorExpression, Type: B2) (Syntax: 'x > y')
            Left: 
              ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: B2) (Syntax: 'x')
            Right: 
              ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: B2) (Syntax: 'y')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'r = x <= y')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: B2, IsImplicit) (Syntax: 'r = x <= y')
        Left: 
          ILocalReferenceExpression: r (OperationKind.LocalReferenceExpression, Type: B2) (Syntax: 'r')
        Right: 
          IBinaryOperatorExpression (BinaryOperatorKind.LessThanOrEqual, Checked) (OperatorMethod: Function B2.op_LessThanOrEqual(x As B2, y As B2) As B2) (OperationKind.BinaryOperatorExpression, Type: B2) (Syntax: 'x <= y')
            Left: 
              ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: B2) (Syntax: 'x')
            Right: 
              ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: B2) (Syntax: 'y')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'r = x >= y')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: B2, IsImplicit) (Syntax: 'r = x >= y')
        Left: 
          ILocalReferenceExpression: r (OperationKind.LocalReferenceExpression, Type: B2) (Syntax: 'r')
        Right: 
          IBinaryOperatorExpression (BinaryOperatorKind.GreaterThanOrEqual, Checked) (OperatorMethod: Function B2.op_GreaterThanOrEqual(x As B2, y As B2) As B2) (OperationKind.BinaryOperatorExpression, Type: B2) (Syntax: 'x >= y')
            Left: 
              ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: B2) (Syntax: 'x')
            Right: 
              ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: B2) (Syntax: 'y')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'r = x Like y')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: B2, IsImplicit) (Syntax: 'r = x Like y')
        Left: 
          ILocalReferenceExpression: r (OperationKind.LocalReferenceExpression, Type: B2) (Syntax: 'r')
        Right: 
          IBinaryOperatorExpression (BinaryOperatorKind.Like, Checked) (OperatorMethod: Function B2.op_Like(x As B2, y As B2) As B2) (OperationKind.BinaryOperatorExpression, Type: B2) (Syntax: 'x Like y')
            Left: 
              ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: B2) (Syntax: 'x')
            Right: 
              ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: B2) (Syntax: 'y')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'r = x & y')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: B2, IsImplicit) (Syntax: 'r = x & y')
        Left: 
          ILocalReferenceExpression: r (OperationKind.LocalReferenceExpression, Type: B2) (Syntax: 'r')
        Right: 
          IBinaryOperatorExpression (BinaryOperatorKind.Concatenate, Checked) (OperatorMethod: Function B2.op_Concatenate(x As B2, y As B2) As B2) (OperationKind.BinaryOperatorExpression, Type: B2) (Syntax: 'x & y')
            Left: 
              ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: B2) (Syntax: 'x')
            Right: 
              ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: B2) (Syntax: 'y')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'r = x And y')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: B2, IsImplicit) (Syntax: 'r = x And y')
        Left: 
          ILocalReferenceExpression: r (OperationKind.LocalReferenceExpression, Type: B2) (Syntax: 'r')
        Right: 
          IBinaryOperatorExpression (BinaryOperatorKind.And, Checked) (OperatorMethod: Function B2.op_BitwiseAnd(x As B2, y As B2) As B2) (OperationKind.BinaryOperatorExpression, Type: B2) (Syntax: 'x And y')
            Left: 
              ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: B2) (Syntax: 'x')
            Right: 
              ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: B2) (Syntax: 'y')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'r = x Or y')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: B2, IsImplicit) (Syntax: 'r = x Or y')
        Left: 
          ILocalReferenceExpression: r (OperationKind.LocalReferenceExpression, Type: B2) (Syntax: 'r')
        Right: 
          IBinaryOperatorExpression (BinaryOperatorKind.Or, Checked) (OperatorMethod: Function B2.op_BitwiseOr(x As B2, y As B2) As B2) (OperationKind.BinaryOperatorExpression, Type: B2) (Syntax: 'x Or y')
            Left: 
              ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: B2) (Syntax: 'x')
            Right: 
              ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: B2) (Syntax: 'y')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'r = x Xor y')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: B2, IsImplicit) (Syntax: 'r = x Xor y')
        Left: 
          ILocalReferenceExpression: r (OperationKind.LocalReferenceExpression, Type: B2) (Syntax: 'r')
        Right: 
          IBinaryOperatorExpression (BinaryOperatorKind.ExclusiveOr, Checked) (OperatorMethod: Function B2.op_ExclusiveOr(x As B2, y As B2) As B2) (OperationKind.BinaryOperatorExpression, Type: B2) (Syntax: 'x Xor y')
            Left: 
              ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: B2) (Syntax: 'x')
            Right: 
              ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: B2) (Syntax: 'y')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'r = x << 2')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: B2, IsImplicit) (Syntax: 'r = x << 2')
        Left: 
          ILocalReferenceExpression: r (OperationKind.LocalReferenceExpression, Type: B2) (Syntax: 'r')
        Right: 
          IBinaryOperatorExpression (BinaryOperatorKind.LeftShift, Checked) (OperatorMethod: Function B2.op_LeftShift(x As B2, y As System.Int32) As B2) (OperationKind.BinaryOperatorExpression, Type: B2) (Syntax: 'x << 2')
            Left: 
              ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: B2) (Syntax: 'x')
            Right: 
              ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'r = x >> 3')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: B2, IsImplicit) (Syntax: 'r = x >> 3')
        Left: 
          ILocalReferenceExpression: r (OperationKind.LocalReferenceExpression, Type: B2) (Syntax: 'r')
        Right: 
          IBinaryOperatorExpression (BinaryOperatorKind.RightShift, Checked) (OperatorMethod: Function B2.op_RightShift(x As B2, y As System.Int32) As B2) (OperationKind.BinaryOperatorExpression, Type: B2) (Syntax: 'x >> 3')
            Left: 
              ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: B2) (Syntax: 'x')
            Right: 
              ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3) (Syntax: '3')
  ILabeledStatement (Label: exit) (OperationKind.LabeledStatement, IsImplicit) (Syntax: 'End Sub')
    Statement: 
      null
  IReturnStatement (OperationKind.ReturnStatement, IsImplicit) (Syntax: 'End Sub')
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
IBlockStatement (6 statements) (OperationKind.BlockStatement) (Syntax: 'Sub M(c1 As ... End Sub')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'r = c1 Is c2')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Boolean, IsImplicit) (Syntax: 'r = c1 Is c2')
        Left: 
          IParameterReferenceExpression: r (OperationKind.ParameterReferenceExpression, Type: System.Boolean) (Syntax: 'r')
        Right: 
          IBinaryOperatorExpression (BinaryOperatorKind.Equals) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'c1 Is c2')
            Left: 
              IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object, IsImplicit) (Syntax: 'c1')
                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                Operand: 
                  IParameterReferenceExpression: c1 (OperationKind.ParameterReferenceExpression, Type: C) (Syntax: 'c1')
            Right: 
              IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object, IsImplicit) (Syntax: 'c2')
                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                Operand: 
                  IParameterReferenceExpression: c2 (OperationKind.ParameterReferenceExpression, Type: C) (Syntax: 'c2')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'r = c1 IsNot c2')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Boolean, IsImplicit) (Syntax: 'r = c1 IsNot c2')
        Left: 
          IParameterReferenceExpression: r (OperationKind.ParameterReferenceExpression, Type: System.Boolean) (Syntax: 'r')
        Right: 
          IBinaryOperatorExpression (BinaryOperatorKind.NotEquals) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'c1 IsNot c2')
            Left: 
              IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object, IsImplicit) (Syntax: 'c1')
                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                Operand: 
                  IParameterReferenceExpression: c1 (OperationKind.ParameterReferenceExpression, Type: C) (Syntax: 'c1')
            Right: 
              IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object, IsImplicit) (Syntax: 'c2')
                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                Operand: 
                  IParameterReferenceExpression: c2 (OperationKind.ParameterReferenceExpression, Type: C) (Syntax: 'c2')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'r = DirectC ... bject) = c2')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Boolean, IsImplicit) (Syntax: 'r = DirectC ... bject) = c2')
        Left: 
          IParameterReferenceExpression: r (OperationKind.ParameterReferenceExpression, Type: System.Boolean) (Syntax: 'r')
        Right: 
          IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Boolean, IsImplicit) (Syntax: 'DirectCast( ... bject) = c2')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IBinaryOperatorExpression (BinaryOperatorKind.ObjectValueEquals, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Object) (Syntax: 'DirectCast( ... bject) = c2')
                Left: 
                  IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: 'DirectCast(c1, Object)')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      IParameterReferenceExpression: c1 (OperationKind.ParameterReferenceExpression, Type: C) (Syntax: 'c1')
                Right: 
                  IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object, IsImplicit) (Syntax: 'c2')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      IParameterReferenceExpression: c2 (OperationKind.ParameterReferenceExpression, Type: C) (Syntax: 'c2')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'r = DirectC ... ject) <> c2')
    Expression: 
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Boolean, IsImplicit) (Syntax: 'r = DirectC ... ject) <> c2')
        Left: 
          IParameterReferenceExpression: r (OperationKind.ParameterReferenceExpression, Type: System.Boolean) (Syntax: 'r')
        Right: 
          IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Boolean, IsImplicit) (Syntax: 'DirectCast( ... ject) <> c2')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IBinaryOperatorExpression (BinaryOperatorKind.ObjectValueNotEquals, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Object) (Syntax: 'DirectCast( ... ject) <> c2')
                Left: 
                  IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: 'DirectCast(c1, Object)')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      IParameterReferenceExpression: c1 (OperationKind.ParameterReferenceExpression, Type: C) (Syntax: 'c1')
                Right: 
                  IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object, IsImplicit) (Syntax: 'c2')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      IParameterReferenceExpression: c2 (OperationKind.ParameterReferenceExpression, Type: C) (Syntax: 'c2')
  ILabeledStatement (Label: exit) (OperationKind.LabeledStatement, IsImplicit) (Syntax: 'End Sub')
    Statement: 
      null
  IReturnStatement (OperationKind.ReturnStatement, IsImplicit) (Syntax: 'End Sub')
    ReturnedValue: 
      null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub
    End Class
End Namespace
