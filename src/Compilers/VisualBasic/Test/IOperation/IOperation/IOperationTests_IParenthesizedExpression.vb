' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

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
    IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32) (Syntax: 'a + b')
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
IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32) (Syntax: 'a + b')
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
        IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32) (Syntax: 'a + b')
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
    IBinaryOperation (BinaryOperatorKind.Multiply, Checked) (OperationKind.Binary, Type: System.Int32) (Syntax: '(a + b) * c')
      Left: 
        IParenthesizedOperation (OperationKind.Parenthesized, Type: System.Int32) (Syntax: '(a + b)')
          Operand: 
            IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32) (Syntax: 'a + b')
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
            IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32) (Syntax: 'a + b')
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
    IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32) (Syntax: 'a + b')
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
IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32) (Syntax: 'a + b')
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
    IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32) (Syntax: 'a + b')
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
            IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32) (Syntax: 'a + b')
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
    IUnaryOperation (UnaryOperatorKind.Minus, Checked) (OperationKind.Unary, Type: System.Int32) (Syntax: '-r')
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

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ParenthesizedFlow_01()
            Dim source = <![CDATA[
Class P
    Public Sub M1(i As Integer)'BIND:"Public Sub M1(i As Integer)"
        i = (3)
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'i = (3)')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'i = (3)')
              Left: 
                IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i')
              Right: 
                IParenthesizedOperation (OperationKind.Parenthesized, Type: System.Int32, Constant: 3) (Syntax: '(3)')
                  Operand: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ParenthesizedFlow_02()
            Dim source = <![CDATA[
Imports System
Class C
    Sub M(b As Boolean, i As Integer)'BIND:"Sub M(b As Boolean, i As Integer)"
        i = (If(b,3,5))
    End Sub
End Class
]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i')
              Value: 
                IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i')

        Jump if False (Regular) to Block[B3]
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '3')
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')

        Next (Regular) Block[B4]
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '5')
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'i = (If(b,3,5))')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'i = (If(b,3,5))')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i')
                  Right: 
                    IParenthesizedOperation (OperationKind.Parenthesized, Type: System.Int32) (Syntax: '(If(b,3,5))')
                      Operand: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(b,3,5)')

        Next (Regular) Block[B5]
            Leaving: {R1}
}

Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ParenthesizedFlow_03()
            Dim source = <![CDATA[
Imports System
Class C
    Sub M(b As Boolean, i As Integer)'BIND:"Sub M(b As Boolean, i As Integer)"
        i = If(b,(3),(5))
    End Sub
End Class
]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i')
              Value: 
                IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i')

        Jump if False (Regular) to Block[B3]
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '(3)')
              Value: 
                IParenthesizedOperation (OperationKind.Parenthesized, Type: System.Int32, Constant: 3) (Syntax: '(3)')
                  Operand: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')

        Next (Regular) Block[B4]
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '(5)')
              Value: 
                IParenthesizedOperation (OperationKind.Parenthesized, Type: System.Int32, Constant: 5) (Syntax: '(5)')
                  Operand: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'i = If(b,(3),(5))')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'i = If(b,(3),(5))')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(b,(3),(5))')

        Next (Regular) Block[B5]
            Leaving: {R1}
}

Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ParenthesizedFlow_04()
            Dim source = <![CDATA[
Class C
    Sub M(s As String, b As Boolean)'BIND:"Sub M(s As String, b As Boolean)"
        M2((s.Length), If(b,3,5))
    End Sub

    Private Sub M2(ByRef i As Integer, v As Integer)
        i = v
    End Sub
End Class
]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [1] [2]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'M2')
              Value: 
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'M2')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '(s.Length)')
              Value: 
                IParenthesizedOperation (OperationKind.Parenthesized, Type: System.Int32) (Syntax: '(s.Length)')
                  Operand: 
                    IPropertyReferenceOperation: ReadOnly Property System.String.Length As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 's.Length')
                      Instance Receiver: 
                        IParameterReferenceOperation: s (OperationKind.ParameterReference, Type: System.String) (Syntax: 's')

        Jump if False (Regular) to Block[B3]
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '3')
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')

        Next (Regular) Block[B4]
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '5')
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'M2((s.Lengt ...  If(b,3,5))')
              Expression: 
                IInvocationOperation ( Sub C.M2(ByRef i As System.Int32, v As System.Int32)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'M2((s.Lengt ...  If(b,3,5))')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'M2')
                  Arguments(2):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '(s.Length)')
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: '(s.Length)')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: v) (OperationKind.Argument, Type: null) (Syntax: 'If(b,3,5)')
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(b,3,5)')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

        Next (Regular) Block[B5]
            Leaving: {R1}
}

Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub
    End Class
End Namespace
