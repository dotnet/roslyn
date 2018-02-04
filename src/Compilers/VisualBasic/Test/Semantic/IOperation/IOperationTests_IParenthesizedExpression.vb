﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Operations
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TestParenthesized()
            Dim source = <![CDATA[
Class P
    Private Shared Function M1(a As Integer, b As Integer) As Integer
        Return (a + b)'BIND:"(a + b)"
    End Function
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IParenthesizedOperation (OperationKind.Parenthesized, Type: System.Int32) (Syntax: '(a + b)')
  Operand: 
    IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'a + b')
      Left: 
        IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'a')
      Right: 
        IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ParenthesizedExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TestParenthesizedChild()
            Dim source = <![CDATA[
Class P
    Private Shared Function M1(a As Integer, b As Integer) As Integer
        Return (a + b)'BIND:"a + b"
    End Function
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'a + b')
  Left: 
    IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'a')
  Right: 
    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of BinaryExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TestParenthesizedParent()
            Dim source = <![CDATA[
Class P
    Private Shared Function M1(a As Integer, b As Integer) As Integer
        Return (a + b)'BIND:"Return (a + b)"
    End Function
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IReturnOperation (OperationKind.Return, Type: null) (Syntax: 'Return (a + b)')
  ReturnedValue: 
    IParenthesizedOperation (OperationKind.Parenthesized, Type: System.Int32) (Syntax: '(a + b)')
      Operand: 
        IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'a + b')
          Left: 
            IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'a')
          Right: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ReturnStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TestParenthesizedMultipleNesting02()
            Dim source = <![CDATA[
Class P
    Private Shared Function M1(a As Integer, b As Integer, c As Integer) As Integer
        Return (((a + b) * c))'BIND:"((a + b) * c)"
    End Function
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IParenthesizedOperation (OperationKind.Parenthesized, Type: System.Int32) (Syntax: '((a + b) * c)')
  Operand: 
    IBinaryOperation (BinaryOperatorKind.Multiply, Checked) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: '(a + b) * c')
      Left: 
        IParenthesizedOperation (OperationKind.Parenthesized, Type: System.Int32) (Syntax: '(a + b)')
          Operand: 
            IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'a + b')
              Left: 
                IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'a')
              Right: 
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')
      Right: 
        IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'c')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ParenthesizedExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TestParenthesizedMultipleNesting03()
            Dim source = <![CDATA[
Class P
    Private Shared Function M1(a As Integer, b As Integer) As Integer
        Return (((a + b)))'BIND:"(((a + b)))"
    End Function
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IParenthesizedOperation (OperationKind.Parenthesized, Type: System.Int32) (Syntax: '(((a + b)))')
  Operand: 
    IParenthesizedOperation (OperationKind.Parenthesized, Type: System.Int32) (Syntax: '((a + b))')
      Operand: 
        IParenthesizedOperation (OperationKind.Parenthesized, Type: System.Int32) (Syntax: '(a + b)')
          Operand: 
            IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'a + b')
              Left: 
                IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'a')
              Right: 
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ParenthesizedExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TestParenthesizedMultipleNesting04()
            Dim source = <![CDATA[
Class P
    Private Shared Function M1(a As Integer, b As Integer) As Integer
        Return (((a + b)))'BIND:"(a + b)"
    End Function
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IParenthesizedOperation (OperationKind.Parenthesized, Type: System.Int32) (Syntax: '(a + b)')
  Operand: 
    IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'a + b')
      Left: 
        IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'a')
      Right: 
        IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ParenthesizedExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TestParenthesizedMultipleNesting05()
            Dim source = <![CDATA[
Class P
    Private Shared Function M1(a As Integer, b As Integer) As Integer
        Return (((a + b)))'BIND:"a + b"
    End Function
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'a + b')
  Left: 
    IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'a')
  Right: 
    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of BinaryExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TestParenthesizedConversion()
            Dim source = <![CDATA[
Class P
    Private Shared Function M1(a As Integer, b As Integer) As Long
        Return (a + b)'BIND:"(a + b)"
    End Function
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IParenthesizedOperation (OperationKind.Parenthesized, Type: System.Int32) (Syntax: '(a + b)')
  Operand: 
    IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'a + b')
      Left: 
        IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'a')
      Right: 
        IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ParenthesizedExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TestParenthesizedConversionParent()
            Dim source = <![CDATA[
Class P
    Private Shared Function M1(a As Integer, b As Integer) As Long
        Return (a + b)'BIND:"Return (a + b)"
    End Function
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IReturnOperation (OperationKind.Return, Type: null) (Syntax: 'Return (a + b)')
  ReturnedValue: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int64, IsImplicit) (Syntax: '(a + b)')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IParenthesizedOperation (OperationKind.Parenthesized, Type: System.Int32) (Syntax: '(a + b)')
          Operand: 
            IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'a + b')
              Left: 
                IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'a')
              Right: 
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ReturnStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TestParenthesizedConstantValue()
            Dim source = <![CDATA[
Class P
    Private Shared Function M1() As Integer
        Return (5)'BIND:"(5)"
    End Function
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IParenthesizedOperation (OperationKind.Parenthesized, Type: System.Int32, Constant: 5) (Syntax: '(5)')
  Operand: 
    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ParenthesizedExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/22879")>
        Public Sub TestParenthesizedDelegateCreation()
            Dim source = <![CDATA[
Class P
    Private Shared Function M1() As Object
        Return CType((Sub() System.Console.WriteLine()), System.Action)'BIND:"(Sub() System.Console.WriteLine())"
    End Function
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IParenthesizedExpression (OperationKind.ParenthesizedExpression, Type: System.Action) (Syntax: '(Sub() Syst ... riteLine())')
  Operand: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Action) (Syntax: 'Sub() Syste ... WriteLine()')
      Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: IAnonymousFunctionExpression (Symbol: Sub ()) (OperationKind.AnonymousFunctionExpression, Type: null) (Syntax: 'Sub() Syste ... WriteLine()')
          IBlockStatement (3 statements) (OperationKind.BlockStatement) (Syntax: 'Sub() Syste ... WriteLine()')
            IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'Sub() Syste ... WriteLine()')
              IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... WriteLine()')
                Expression: IInvocationExpression (Sub System.Console.WriteLine()) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... WriteLine()')
                    Instance Receiver: null
                    Arguments(0)
            ILabelStatement (Label: exit) (OperationKind.LabelStatement) (Syntax: 'Sub() Syste ... WriteLine()')
              LabeledStatement: null
            IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'Sub() Syste ... WriteLine()')
              ReturnedValue: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ParenthesizedExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/22879")>
        Public Sub TestParenthesizedDelegateCreationParent()
            Dim source = <![CDATA[
Class P
    Private Shared Function M1() As Object
        Return CType((Sub() System.Console.WriteLine()), System.Action)'BIND:"CType((Sub() System.Console.WriteLine()), System.Action)"
    End Function
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: 'CType((Sub( ... tem.Action)')
  Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Operand: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Action) (Syntax: 'CType((Sub( ... tem.Action)')
      Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: IParenthesizedExpression (OperationKind.ParenthesizedExpression, Type: System.Action) (Syntax: '(Sub() Syst ... riteLine())')
          Operand: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Action) (Syntax: 'Sub() Syste ... WriteLine()')
              Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Operand: IAnonymousFunctionExpression (Symbol: Sub ()) (OperationKind.AnonymousFunctionExpression, Type: null) (Syntax: 'Sub() Syste ... WriteLine()')
                  IBlockStatement (3 statements) (OperationKind.BlockStatement) (Syntax: 'Sub() Syste ... WriteLine()')
                    IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'Sub() Syste ... WriteLine()')
                      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... WriteLine()')
                        Expression: IInvocationExpression (Sub System.Console.WriteLine()) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... WriteLine()')
                            Instance Receiver: null
                            Arguments(0)
                    ILabelStatement (Label: exit) (OperationKind.LabelStatement) (Syntax: 'Sub() Syste ... WriteLine()')
                      LabeledStatement: null
                    IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'Sub() Syste ... WriteLine()')
                      ReturnedValue: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of CTypeExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TestParenthesizedDelegateCreationWithImplicitConversion()
            Dim source = <![CDATA[
Class P
    Private Shared Function M1() As System.Action
        Return (Sub() System.Console.WriteLine())'BIND:"(Sub() System.Console.WriteLine())"
    End Function
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IParenthesizedOperation (OperationKind.Parenthesized, Type: System.Action) (Syntax: '(Sub() Syst ... riteLine())')
  Operand: 
    IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action, IsImplicit) (Syntax: 'Sub() Syste ... WriteLine()')
      Target: 
        IAnonymousFunctionOperation (Symbol: Sub ()) (OperationKind.AnonymousFunction, Type: null) (Syntax: 'Sub() Syste ... WriteLine()')
          IBlockOperation (3 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Sub() Syste ... WriteLine()')
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'System.Cons ... WriteLine()')
              Expression: 
                IInvocationOperation (Sub System.Console.WriteLine()) (OperationKind.Invocation, Type: System.Void) (Syntax: 'System.Cons ... WriteLine()')
                  Instance Receiver: 
                    null
                  Arguments(0)
            ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'Sub() Syste ... WriteLine()')
              Statement: 
                null
            IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'Sub() Syste ... WriteLine()')
              ReturnedValue: 
                null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ParenthesizedExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TestParenthesizedDelegateCreationWithImplicitConversionParent()
            Dim source = <![CDATA[
Class P
    Private Shared Function M1() As System.Action
        Return (Sub() System.Console.WriteLine())'BIND:"Return (Sub() System.Console.WriteLine())"
    End Function
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IReturnOperation (OperationKind.Return, Type: null) (Syntax: 'Return (Sub ... riteLine())')
  ReturnedValue: 
    IParenthesizedOperation (OperationKind.Parenthesized, Type: System.Action) (Syntax: '(Sub() Syst ... riteLine())')
      Operand: 
        IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action, IsImplicit) (Syntax: 'Sub() Syste ... WriteLine()')
          Target: 
            IAnonymousFunctionOperation (Symbol: Sub ()) (OperationKind.AnonymousFunction, Type: null) (Syntax: 'Sub() Syste ... WriteLine()')
              IBlockOperation (3 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Sub() Syste ... WriteLine()')
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'System.Cons ... WriteLine()')
                  Expression: 
                    IInvocationOperation (Sub System.Console.WriteLine()) (OperationKind.Invocation, Type: System.Void) (Syntax: 'System.Cons ... WriteLine()')
                      Instance Receiver: 
                        null
                      Arguments(0)
                ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'Sub() Syste ... WriteLine()')
                  Statement: 
                    null
                IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'Sub() Syste ... WriteLine()')
                  ReturnedValue: 
                    null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ReturnStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TestParenthesizedQueryClause()
            Dim source = <![CDATA[
Imports System.Linq

Class P
    Private Shared Function M1(a As Integer()) As Object
        Return From r In a Select (-r)'BIND:"(-r)"
    End Function
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IParenthesizedOperation (OperationKind.Parenthesized, Type: System.Int32) (Syntax: '(-r)')
  Operand: 
    IUnaryOperation (UnaryOperatorKind.Minus, Checked) (OperationKind.UnaryOperator, Type: System.Int32) (Syntax: '-r')
      Operand: 
        IParameterReferenceOperation: r (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'r')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ParenthesizedExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TestParenthesizedErrorOperand()
            Dim source = <![CDATA[
Class P
    Private Shared Function M1() As Object
        Return (a)'BIND:"(a)"
    End Function
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IParenthesizedOperation (OperationKind.Parenthesized, Type: ?, IsInvalid) (Syntax: '(a)')
  Operand: 
    IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'a')
      Children(0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30451: 'a' is not declared. It may be inaccessible due to its protection level.
        Return (a)'BIND:"(a)"
                ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ParenthesizedExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub
    End Class
End Namespace
