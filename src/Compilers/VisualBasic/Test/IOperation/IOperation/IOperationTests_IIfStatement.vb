' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")>
        Public Sub IIfstatementSingleLineIf()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Dim count As Integer = 0
        Dim returnValue As Integer = -1
        If count > 0 Then returnValue = count'BIND:"If count > 0 Then returnValue = count"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IConditionalOperation (OperationKind.Conditional, Type: null) (Syntax: 'If count >  ... lue = count')
  Condition: 
    IBinaryOperation (BinaryOperatorKind.GreaterThan, Checked) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'count > 0')
      Left: 
        ILocalReferenceOperation: count (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'count')
      Right: 
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
  WhenTrue: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'If count >  ... lue = count')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'returnValue = count')
        Expression: 
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'returnValue = count')
            Left: 
              ILocalReferenceOperation: returnValue (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'returnValue')
            Right: 
              ILocalReferenceOperation: count (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'count')
  WhenFalse: 
    null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of SingleLineIfStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")>
        Public Sub IIfstatementMultiLineIf()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Dim count As Integer = 0
        Dim returnValue As Integer = 1
        If count > 0 Then 'BIND:"If count > 0 Then"'BIND:"If count > 0 Then 'BIND:"If count > 0 Then""
            returnValue = count
        End If
    End Sub
End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IConditionalOperation (OperationKind.Conditional, Type: null) (Syntax: 'If count >  ... End If')
  Condition: 
    IBinaryOperation (BinaryOperatorKind.GreaterThan, Checked) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'count > 0')
      Left: 
        ILocalReferenceOperation: count (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'count')
      Right: 
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
  WhenTrue: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'If count >  ... End If')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'returnValue = count')
        Expression: 
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'returnValue = count')
            Left: 
              ILocalReferenceOperation: returnValue (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'returnValue')
            Right: 
              ILocalReferenceOperation: count (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'count')
  WhenFalse: 
    null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of MultiLineIfBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")>
        Public Sub IIfstatementSingleLineIfAndElse()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Dim count As Integer
        Dim data As Integer
        If count > 10 Then data = data + count Else data = data - count'BIND:"If count > 10 Then data = data + count Else data = data - count"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IConditionalOperation (OperationKind.Conditional, Type: null) (Syntax: 'If count >  ... ata - count')
  Condition: 
    IBinaryOperation (BinaryOperatorKind.GreaterThan, Checked) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'count > 10')
      Left: 
        ILocalReferenceOperation: count (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'count')
      Right: 
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
  WhenTrue: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'If count >  ... ata - count')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'data = data + count')
        Expression: 
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'data = data + count')
            Left: 
              ILocalReferenceOperation: data (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'data')
            Right: 
              IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32) (Syntax: 'data + count')
                Left: 
                  ILocalReferenceOperation: data (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'data')
                Right: 
                  ILocalReferenceOperation: count (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'count')
  WhenFalse: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: 'Else data = data - count')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'data = data - count')
        Expression: 
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'data = data - count')
            Left: 
              ILocalReferenceOperation: data (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'data')
            Right: 
              IBinaryOperation (BinaryOperatorKind.Subtract, Checked) (OperationKind.Binary, Type: System.Int32) (Syntax: 'data - count')
                Left: 
                  ILocalReferenceOperation: data (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'data')
                Right: 
                  ILocalReferenceOperation: count (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'count')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of SingleLineIfStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")>
        Public Sub IIfstatementSingleLineIfAndElseNested()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Dim m As Integer = 12
        Dim n As Integer = 18
        Dim returnValue As Integer = -1
        If m > 10 Then If n > 20 Then returnValue = n'BIND:"If m > 10 Then If n > 20 Then returnValue = n"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IConditionalOperation (OperationKind.Conditional, Type: null) (Syntax: 'If m > 10 T ... rnValue = n')
  Condition: 
    IBinaryOperation (BinaryOperatorKind.GreaterThan, Checked) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'm > 10')
      Left: 
        ILocalReferenceOperation: m (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'm')
      Right: 
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
  WhenTrue: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'If m > 10 T ... rnValue = n')
      IConditionalOperation (OperationKind.Conditional, Type: null) (Syntax: 'If n > 20 T ... rnValue = n')
        Condition: 
          IBinaryOperation (BinaryOperatorKind.GreaterThan, Checked) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'n > 20')
            Left: 
              ILocalReferenceOperation: n (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'n')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 20) (Syntax: '20')
        WhenTrue: 
          IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'If n > 20 T ... rnValue = n')
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'returnValue = n')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'returnValue = n')
                  Left: 
                    ILocalReferenceOperation: returnValue (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'returnValue')
                  Right: 
                    ILocalReferenceOperation: n (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'n')
        WhenFalse: 
          null
  WhenFalse: 
    null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of SingleLineIfStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")>
        Public Sub IIfstatementSimpleIfWithConditionEvaluationTrue()
            Dim source = <![CDATA[
Class P
    Private Sub M()
        Dim condition As Boolean = False
        If 1 = 1 Then'BIND:"If 1 = 1 Then"
            condition = True
        End If
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IConditionalOperation (OperationKind.Conditional, Type: null) (Syntax: 'If 1 = 1 Th ... End If')
  Condition: 
    IBinaryOperation (BinaryOperatorKind.Equals, Checked) (OperationKind.Binary, Type: System.Boolean, Constant: True) (Syntax: '1 = 1')
      Left: 
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
      Right: 
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
  WhenTrue: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'If 1 = 1 Th ... End If')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'condition = True')
        Expression: 
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'condition = True')
            Left: 
              ILocalReferenceOperation: condition (OperationKind.LocalReference, Type: System.Boolean) (Syntax: 'condition')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'True')
  WhenFalse: 
    null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of MultiLineIfBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")>
        Public Sub IIfstatementSimpleIfWithConditionConstantFalse()
            Dim source = <![CDATA[
Class P
    Private Sub M()
        Dim condition As Boolean = True
        If False Then'BIND:"If False Then"
            condition = False
        End If
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IConditionalOperation (OperationKind.Conditional, Type: null) (Syntax: 'If False Th ... End If')
  Condition: 
    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'False')
  WhenTrue: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'If False Th ... End If')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'condition = False')
        Expression: 
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'condition = False')
            Left: 
              ILocalReferenceOperation: condition (OperationKind.LocalReference, Type: System.Boolean) (Syntax: 'condition')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'False')
  WhenFalse: 
    null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of MultiLineIfBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")>
        Public Sub IIfstatementSingleLineWithOperator()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Dim m As Integer = 12
        Dim n As Integer = 18
        Dim returnValue As Integer = -1
        If (m > 10 And n > 20) Then returnValue = n'BIND:"If (m > 10 And n > 20) Then returnValue = n"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IConditionalOperation (OperationKind.Conditional, Type: null) (Syntax: 'If (m > 10  ... rnValue = n')
  Condition: 
    IParenthesizedOperation (OperationKind.Parenthesized, Type: System.Boolean) (Syntax: '(m > 10 And n > 20)')
      Operand: 
        IBinaryOperation (BinaryOperatorKind.And, Checked) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'm > 10 And n > 20')
          Left: 
            IBinaryOperation (BinaryOperatorKind.GreaterThan, Checked) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'm > 10')
              Left: 
                ILocalReferenceOperation: m (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'm')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
          Right: 
            IBinaryOperation (BinaryOperatorKind.GreaterThan, Checked) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'n > 20')
              Left: 
                ILocalReferenceOperation: n (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'n')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 20) (Syntax: '20')
  WhenTrue: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'If (m > 10  ... rnValue = n')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'returnValue = n')
        Expression: 
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'returnValue = n')
            Left: 
              ILocalReferenceOperation: returnValue (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'returnValue')
            Right: 
              ILocalReferenceOperation: n (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'n')
  WhenFalse: 
    null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of SingleLineIfStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")>
        Public Sub IIfstatementMultiLineIfWithElse()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Dim count As Integer = 0
        Dim returnValue As Integer = -1
        If count > 0 Then'BIND:"If count > 0 Then"
            returnValue = count
        Else
            returnValue = -1
        End If
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IConditionalOperation (OperationKind.Conditional, Type: null) (Syntax: 'If count >  ... End If')
  Condition: 
    IBinaryOperation (BinaryOperatorKind.GreaterThan, Checked) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'count > 0')
      Left: 
        ILocalReferenceOperation: count (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'count')
      Right: 
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
  WhenTrue: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'If count >  ... End If')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'returnValue = count')
        Expression: 
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'returnValue = count')
            Left: 
              ILocalReferenceOperation: returnValue (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'returnValue')
            Right: 
              ILocalReferenceOperation: count (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'count')
  WhenFalse: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: 'Else ... nValue = -1')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'returnValue = -1')
        Expression: 
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'returnValue = -1')
            Left: 
              ILocalReferenceOperation: returnValue (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'returnValue')
            Right: 
              IUnaryOperation (UnaryOperatorKind.Minus, Checked) (OperationKind.Unary, Type: System.Int32, Constant: -1) (Syntax: '-1')
                Operand: 
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of MultiLineIfBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")>
        Public Sub IIfstatementSimpleIfNested1()
            Dim source = <![CDATA[
Imports System
Module Program
    Sub Main(args As String())
        Dim m As Integer = 12
        Dim n As Integer = 18
        Dim returnValue As Integer = -1
        If (m > 10) Then'BIND:"If (m > 10) Then"
            If (n > 20) Then
                Console.WriteLine("Result 1")
            End If
        Else
            Console.WriteLine("Result 2")
        End If
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IConditionalOperation (OperationKind.Conditional, Type: null) (Syntax: 'If (m > 10) ... End If')
  Condition: 
    IParenthesizedOperation (OperationKind.Parenthesized, Type: System.Boolean) (Syntax: '(m > 10)')
      Operand: 
        IBinaryOperation (BinaryOperatorKind.GreaterThan, Checked) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'm > 10')
          Left: 
            ILocalReferenceOperation: m (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'm')
          Right: 
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
  WhenTrue: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'If (m > 10) ... End If')
      IConditionalOperation (OperationKind.Conditional, Type: null) (Syntax: 'If (n > 20) ... End If')
        Condition: 
          IParenthesizedOperation (OperationKind.Parenthesized, Type: System.Boolean) (Syntax: '(n > 20)')
            Operand: 
              IBinaryOperation (BinaryOperatorKind.GreaterThan, Checked) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'n > 20')
                Left: 
                  ILocalReferenceOperation: n (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'n')
                Right: 
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 20) (Syntax: '20')
        WhenTrue: 
          IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'If (n > 20) ... End If')
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.Wri ... "Result 1")')
              Expression: 
                IInvocationOperation (Sub System.Console.WriteLine(value As System.String)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.Wri ... "Result 1")')
                  Instance Receiver: 
                    null
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: '"Result 1"')
                        ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "Result 1") (Syntax: '"Result 1"')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        WhenFalse: 
          null
  WhenFalse: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: 'Else ... "Result 2")')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.Wri ... "Result 2")')
        Expression: 
          IInvocationOperation (Sub System.Console.WriteLine(value As System.String)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.Wri ... "Result 2")')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: '"Result 2"')
                  ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "Result 2") (Syntax: '"Result 2"')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of MultiLineIfBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")>
        Public Sub IIfstatementIfNested2()
            Dim source = <![CDATA[
Imports System
Module Program
    Sub Main(args As String())
        Dim m As Integer = 12
        Dim n As Integer = 18
        Dim returnValue As Integer = -1
        If (m > 10) Then'BIND:"If (m > 10) Then"
            If (n > 20) Then
                Console.WriteLine("Result 1")
            Else
                Console.WriteLine("Result 2")
            End If
        End If
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IConditionalOperation (OperationKind.Conditional, Type: null) (Syntax: 'If (m > 10) ... End If')
  Condition: 
    IParenthesizedOperation (OperationKind.Parenthesized, Type: System.Boolean) (Syntax: '(m > 10)')
      Operand: 
        IBinaryOperation (BinaryOperatorKind.GreaterThan, Checked) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'm > 10')
          Left: 
            ILocalReferenceOperation: m (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'm')
          Right: 
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
  WhenTrue: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'If (m > 10) ... End If')
      IConditionalOperation (OperationKind.Conditional, Type: null) (Syntax: 'If (n > 20) ... End If')
        Condition: 
          IParenthesizedOperation (OperationKind.Parenthesized, Type: System.Boolean) (Syntax: '(n > 20)')
            Operand: 
              IBinaryOperation (BinaryOperatorKind.GreaterThan, Checked) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'n > 20')
                Left: 
                  ILocalReferenceOperation: n (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'n')
                Right: 
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 20) (Syntax: '20')
        WhenTrue: 
          IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'If (n > 20) ... End If')
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.Wri ... "Result 1")')
              Expression: 
                IInvocationOperation (Sub System.Console.WriteLine(value As System.String)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.Wri ... "Result 1")')
                  Instance Receiver: 
                    null
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: '"Result 1"')
                        ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "Result 1") (Syntax: '"Result 1"')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        WhenFalse: 
          IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: 'Else ... "Result 2")')
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.Wri ... "Result 2")')
              Expression: 
                IInvocationOperation (Sub System.Console.WriteLine(value As System.String)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.Wri ... "Result 2")')
                  Instance Receiver: 
                    null
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: '"Result 2"')
                        ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "Result 2") (Syntax: '"Result 2"')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  WhenFalse: 
    null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of MultiLineIfBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")>
        Public Sub IIfstatementWithMultipleCondition()
            Dim source = <![CDATA[
Imports System
Module Program
    Sub Main(args As String())
        Dim m As Integer = 9
        Dim n As Integer = 7
        Dim p As Integer = 5
        If (m >= n AndAlso m >= p) Then'BIND:"If (m >= n AndAlso m >= p) Then"
            Console.WriteLine("Nothing Is larger than m.")
        End If
    End Sub
End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IConditionalOperation (OperationKind.Conditional, Type: null) (Syntax: 'If (m >= n  ... End If')
  Condition: 
    IParenthesizedOperation (OperationKind.Parenthesized, Type: System.Boolean) (Syntax: '(m >= n AndAlso m >= p)')
      Operand: 
        IBinaryOperation (BinaryOperatorKind.ConditionalAnd, Checked) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'm >= n AndAlso m >= p')
          Left: 
            IBinaryOperation (BinaryOperatorKind.GreaterThanOrEqual, Checked) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'm >= n')
              Left: 
                ILocalReferenceOperation: m (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'm')
              Right: 
                ILocalReferenceOperation: n (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'n')
          Right: 
            IBinaryOperation (BinaryOperatorKind.GreaterThanOrEqual, Checked) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'm >= p')
              Left: 
                ILocalReferenceOperation: m (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'm')
              Right: 
                ILocalReferenceOperation: p (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'p')
  WhenTrue: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'If (m >= n  ... End If')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.Wri ... r than m.")')
        Expression: 
          IInvocationOperation (Sub System.Console.WriteLine(value As System.String)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.Wri ... r than m.")')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: '"Nothing Is ... er than m."')
                  ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "Nothing Is larger than m.") (Syntax: '"Nothing Is ... er than m."')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  WhenFalse: 
    null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of MultiLineIfBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")>
        Public Sub IIfstatementWithElseIfCondition()
            Dim source = <![CDATA[
Imports System
Module Program
    Sub Main(args As String())
        Dim m As Integer = 9
        Dim n As Integer = 7
        If (m > 20) Then'BIND:"If (m > 20) Then"
            Console.WriteLine("Result1")
        ElseIf (n > 10) Then
            Console.WriteLine("Result2")
        Else
            Console.WriteLine("Result3")
        End If
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IConditionalOperation (OperationKind.Conditional, Type: null) (Syntax: 'If (m > 20) ... End If')
  Condition: 
    IParenthesizedOperation (OperationKind.Parenthesized, Type: System.Boolean) (Syntax: '(m > 20)')
      Operand: 
        IBinaryOperation (BinaryOperatorKind.GreaterThan, Checked) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'm > 20')
          Left: 
            ILocalReferenceOperation: m (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'm')
          Right: 
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 20) (Syntax: '20')
  WhenTrue: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'If (m > 20) ... End If')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.Wri ... ("Result1")')
        Expression: 
          IInvocationOperation (Sub System.Console.WriteLine(value As System.String)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.Wri ... ("Result1")')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: '"Result1"')
                  ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "Result1") (Syntax: '"Result1"')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  WhenFalse: 
    IConditionalOperation (OperationKind.Conditional, Type: null) (Syntax: 'ElseIf (n > ... ("Result2")')
      Condition: 
        IParenthesizedOperation (OperationKind.Parenthesized, Type: System.Boolean) (Syntax: '(n > 10)')
          Operand: 
            IBinaryOperation (BinaryOperatorKind.GreaterThan, Checked) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'n > 10')
              Left: 
                ILocalReferenceOperation: n (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'n')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
      WhenTrue: 
        IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'ElseIf (n > ... ("Result2")')
          IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.Wri ... ("Result2")')
            Expression: 
              IInvocationOperation (Sub System.Console.WriteLine(value As System.String)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.Wri ... ("Result2")')
                Instance Receiver: 
                  null
                Arguments(1):
                    IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: '"Result2"')
                      ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "Result2") (Syntax: '"Result2"')
                      InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      WhenFalse: 
        IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: 'Else ... ("Result3")')
          IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.Wri ... ("Result3")')
            Expression: 
              IInvocationOperation (Sub System.Console.WriteLine(value As System.String)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.Wri ... ("Result3")')
                Instance Receiver: 
                  null
                Arguments(1):
                    IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: '"Result3"')
                      ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "Result3") (Syntax: '"Result3"')
                      InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of MultiLineIfBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")>
        Public Sub IIfstatementWithElseIfSingleLine()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Dim m As Integer = 9
        Dim n As Integer = 7
        If (m > 20) Then System.Console.WriteLine("Result1") Else If (n > 10) Then System.Console.WriteLine("Result2") Else System.Console.WriteLine("Result3") End If'BIND:"If (m > 20) Then System.Console.WriteLine("Result1") Else If (n > 10) Then System.Console.WriteLine("Result2") Else System.Console.WriteLine("Result3")"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IConditionalOperation (OperationKind.Conditional, Type: null) (Syntax: 'If (m > 20) ... ("Result3")')
  Condition: 
    IParenthesizedOperation (OperationKind.Parenthesized, Type: System.Boolean) (Syntax: '(m > 20)')
      Operand: 
        IBinaryOperation (BinaryOperatorKind.GreaterThan, Checked) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'm > 20')
          Left: 
            ILocalReferenceOperation: m (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'm')
          Right: 
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 20) (Syntax: '20')
  WhenTrue: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'If (m > 20) ... ("Result3")')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'System.Cons ... ("Result1")')
        Expression: 
          IInvocationOperation (Sub System.Console.WriteLine(value As System.String)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'System.Cons ... ("Result1")')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: '"Result1"')
                  ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "Result1") (Syntax: '"Result1"')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  WhenFalse: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: 'Else If (n  ... ("Result3")')
      IConditionalOperation (OperationKind.Conditional, Type: null) (Syntax: 'If (n > 10) ... ("Result3")')
        Condition: 
          IParenthesizedOperation (OperationKind.Parenthesized, Type: System.Boolean) (Syntax: '(n > 10)')
            Operand: 
              IBinaryOperation (BinaryOperatorKind.GreaterThan, Checked) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'n > 10')
                Left: 
                  ILocalReferenceOperation: n (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'n')
                Right: 
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
        WhenTrue: 
          IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'If (n > 10) ... ("Result3")')
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'System.Cons ... ("Result2")')
              Expression: 
                IInvocationOperation (Sub System.Console.WriteLine(value As System.String)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'System.Cons ... ("Result2")')
                  Instance Receiver: 
                    null
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: '"Result2"')
                        ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "Result2") (Syntax: '"Result2"')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        WhenFalse: 
          IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: 'Else System ... ("Result3")')
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'System.Cons ... ("Result3")')
              Expression: 
                IInvocationOperation (Sub System.Console.WriteLine(value As System.String)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'System.Cons ... ("Result3")')
                  Instance Receiver: 
                    null
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: '"Result3"')
                        ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "Result3") (Syntax: '"Result3"')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30205: End of statement expected.
        If (m > 20) Then System.Console.WriteLine("Result1") Else If (n > 10) Then System.Console.WriteLine("Result2") Else System.Console.WriteLine("Result3") End If'BIND:"If (m > 20) Then System.Console.WriteLine("Result1") Else If (n > 10) Then System.Console.WriteLine("Result2") Else System.Console.WriteLine("Result3")"
                                                                                                                                                                ~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of SingleLineIfStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")>
        Public Sub IIfstatementWithElseMissing()
            Dim source = <![CDATA[
Imports System
Module Program
    Sub Main(args As String())
        Dim m As Integer = 9
        If (m > 20) Then'BIND:"If (m > 20) Then"
            Console.WriteLine("Result1")
        Else
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IConditionalOperation (OperationKind.Conditional, Type: null, IsInvalid) (Syntax: 'If (m > 20) ... Else')
  Condition: 
    IParenthesizedOperation (OperationKind.Parenthesized, Type: System.Boolean, IsInvalid) (Syntax: '(m > 20)')
      Operand: 
        IBinaryOperation (BinaryOperatorKind.GreaterThan, Checked) (OperationKind.Binary, Type: System.Boolean, IsInvalid) (Syntax: 'm > 20')
          Left: 
            ILocalReferenceOperation: m (OperationKind.LocalReference, Type: System.Int32, IsInvalid) (Syntax: 'm')
          Right: 
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 20, IsInvalid) (Syntax: '20')
  WhenTrue: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'If (m > 20) ... Else')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.Wri ... ("Result1")')
        Expression: 
          IInvocationOperation (Sub System.Console.WriteLine(value As System.String)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.Wri ... ("Result1")')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: '"Result1"')
                  ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "Result1") (Syntax: '"Result1"')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  WhenFalse: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: 'Else')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30081: 'If' must end with a matching 'End If'.
        If (m > 20) Then'BIND:"If (m > 20) Then"
        ~~~~~~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of MultiLineIfBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")>
        Public Sub IIfstatementWithConditionMissing()
            Dim source = <![CDATA[
Imports System
Module Program
    Sub Main(args As String())
        Dim m As Integer = 9
        If () Then'BIND:"If () Then"
            Console.WriteLine("Result1")
        End If
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IConditionalOperation (OperationKind.Conditional, Type: null, IsInvalid) (Syntax: 'If () Then' ... End If')
  Condition: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: '()')
      Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IParenthesizedOperation (OperationKind.Parenthesized, Type: ?, IsInvalid) (Syntax: '()')
          Operand: 
            IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
              Children(0)
  WhenTrue: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'If () Then' ... End If')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.Wri ... ("Result1")')
        Expression: 
          IInvocationOperation (Sub System.Console.WriteLine(value As System.String)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.Wri ... ("Result1")')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: '"Result1"')
                  ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "Result1") (Syntax: '"Result1"')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  WhenFalse: 
    null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30201: Expression expected.
        If () Then'BIND:"If () Then"
            ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of MultiLineIfBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")>
        Public Sub IIfstatementWithStatementMissing()
            Dim source = <![CDATA[
Imports System
Module Program
    Sub Main(args As String())
        Dim m As Integer = 9
        If (m = 9) Then'BIND:"If (m = 9) Then"
        Else
        End If

    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IConditionalOperation (OperationKind.Conditional, Type: null) (Syntax: 'If (m = 9)  ... End If')
  Condition: 
    IParenthesizedOperation (OperationKind.Parenthesized, Type: System.Boolean) (Syntax: '(m = 9)')
      Operand: 
        IBinaryOperation (BinaryOperatorKind.Equals, Checked) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'm = 9')
          Left: 
            ILocalReferenceOperation: m (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'm')
          Right: 
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 9) (Syntax: '9')
  WhenTrue: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'If (m = 9)  ... End If')
  WhenFalse: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: 'Else')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of MultiLineIfBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")>
        Public Sub IIfstatementWithFuncCall()
            Dim source = <![CDATA[
Module Module1
    Sub Main()
        If (True) Then'BIND:"If (True) Then"
            A()
        Else
            B()
        End If
    End Sub
    Function A() As String
        Return "A"
    End Function
    Function B() As String
        Return "B"
    End Function
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IConditionalOperation (OperationKind.Conditional, Type: null) (Syntax: 'If (True) T ... End If')
  Condition: 
    IParenthesizedOperation (OperationKind.Parenthesized, Type: System.Boolean, Constant: True) (Syntax: '(True)')
      Operand: 
        ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'True')
  WhenTrue: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'If (True) T ... End If')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'A()')
        Expression: 
          IInvocationOperation (Function Module1.A() As System.String) (OperationKind.Invocation, Type: System.String) (Syntax: 'A()')
            Instance Receiver: 
              null
            Arguments(0)
  WhenFalse: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: 'Else ... B()')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'B()')
        Expression: 
          IInvocationOperation (Function Module1.B() As System.String) (OperationKind.Invocation, Type: System.String) (Syntax: 'B()')
            Instance Receiver: 
              null
            Arguments(0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of MultiLineIfBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub IIfstatement_GetElseIf()
            Dim source = <![CDATA[
Imports System
Module Program
    Sub Main(args As String())
        Dim m As Integer = 9
        Dim n As Integer = 7
        If (m > 20) Then
            Console.WriteLine("Result1")
        ElseIf (n > 10) Then'BIND:"ElseIf (n > 10) Then"
            Console.WriteLine("Result2")
        Else
            Console.WriteLine("Result3")
        End If
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IConditionalOperation (OperationKind.Conditional, Type: null) (Syntax: 'ElseIf (n > ... ("Result2")')
  Condition: 
    IParenthesizedOperation (OperationKind.Parenthesized, Type: System.Boolean) (Syntax: '(n > 10)')
      Operand: 
        IBinaryOperation (BinaryOperatorKind.GreaterThan, Checked) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'n > 10')
          Left: 
            ILocalReferenceOperation: n (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'n')
          Right: 
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
  WhenTrue: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'ElseIf (n > ... ("Result2")')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.Wri ... ("Result2")')
        Expression: 
          IInvocationOperation (Sub System.Console.WriteLine(value As System.String)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.Wri ... ("Result2")')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: '"Result2"')
                  ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "Result2") (Syntax: '"Result2"')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  WhenFalse: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: 'Else ... ("Result3")')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.Wri ... ("Result3")')
        Expression: 
          IInvocationOperation (Sub System.Console.WriteLine(value As System.String)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.Wri ... ("Result3")')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: '"Result3"')
                  ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "Result3") (Syntax: '"Result3"')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ElseIfBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub IIfstatement_GetElse()
            Dim source = <![CDATA[
Imports System
Module Program
    Sub Main(args As String())
        Dim m As Integer = 9
        Dim n As Integer = 7
        If (m > 20) Then
            Console.WriteLine("Result1")
        ElseIf (n > 10) Then
            Console.WriteLine("Result2")
        Else'BIND:"Else"
            Console.WriteLine("Result3")
        End If
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: 'Else'BIND:" ... ("Result3")')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.Wri ... ("Result3")')
    Expression: 
      IInvocationOperation (Sub System.Console.WriteLine(value As System.String)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.Wri ... ("Result3")')
        Instance Receiver: 
          null
        Arguments(1):
            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: '"Result3"')
              ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "Result3") (Syntax: '"Result3"')
              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ElseBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub IIfstatementSingleLineIf_GetElse()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Dim count As Integer
        Dim data As Integer
        If count > 10 Then data = data + count Else data = data - count'BIND:"Else data = data - count"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: 'Else data = data - count')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'data = data - count')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'data = data - count')
        Left: 
          ILocalReferenceOperation: data (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'data')
        Right: 
          IBinaryOperation (BinaryOperatorKind.Subtract, Checked) (OperationKind.Binary, Type: System.Int32) (Syntax: 'data - count')
            Left: 
              ILocalReferenceOperation: data (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'data')
            Right: 
              ILocalReferenceOperation: count (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'count')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of SingleLineElseClauseSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub IfFlow_03()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(a as Boolean, b as Boolean) 'BIND:"Sub M"
        If a AndAlso b
            a = false
        else
            b = true
        End If
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (0)
    Jump if False (Regular) to Block[B4]
        IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'a')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (0)
    Jump if False (Regular) to Block[B4]
        IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

    Next (Regular) Block[B3]
Block[B3] - Block
    Predecessors: [B2]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a = false')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'a = false')
              Left: 
                IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'a')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

    Next (Regular) Block[B5]
Block[B4] - Block
    Predecessors: [B1] [B2]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = true')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'b = true')
              Left: 
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

    Next (Regular) Block[B5]
Block[B5] - Exit
    Predecessors: [B3] [B4]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub IfFlow_23()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(a as Boolean, b as Boolean) 'BIND:"Sub M"
        If (a AndAlso b)
            a = false
        else
            b = true
        End If
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (0)
    Jump if False (Regular) to Block[B4]
        IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'a')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (0)
    Jump if False (Regular) to Block[B4]
        IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

    Next (Regular) Block[B3]
Block[B3] - Block
    Predecessors: [B2]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a = false')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'a = false')
              Left: 
                IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'a')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

    Next (Regular) Block[B5]
Block[B4] - Block
    Predecessors: [B1] [B2]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = true')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'b = true')
              Left: 
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

    Next (Regular) Block[B5]
Block[B5] - Exit
    Predecessors: [B3] [B4]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub IfFlow_24()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(a as Object, b as Boolean) 'BIND:"Sub M"
        If a
            b = true
        End If
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (0)
    Jump if False (Regular) to Block[B3]
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Boolean, IsImplicit) (Syntax: 'a')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            (NarrowingValue)
          Operand: 
            IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'a')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = true')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'b = true')
              Left: 
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

    Next (Regular) Block[B3]
Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

    End Class
End Namespace
