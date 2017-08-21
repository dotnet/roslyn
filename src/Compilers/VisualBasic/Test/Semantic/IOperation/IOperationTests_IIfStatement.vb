' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Test.Utilities
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
IIfStatement (OperationKind.IfStatement) (Syntax: 'If count >  ... lue = count')
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'count > 0')
      Left: ILocalReferenceExpression: count (IsDeclaration: False) (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'count')
      Right: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
  IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'If count >  ... lue = count')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'returnValue = count')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'returnValue = count')
            Left: ILocalReferenceExpression: returnValue (IsDeclaration: False) (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'returnValue')
            Right: ILocalReferenceExpression: count (IsDeclaration: False) (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'count')
  IfFalse: null
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
IIfStatement (OperationKind.IfStatement) (Syntax: 'If count >  ... End If')
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'count > 0')
      Left: ILocalReferenceExpression: count (IsDeclaration: False) (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'count')
      Right: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
  IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'If count >  ... End If')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'returnValue = count')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'returnValue = count')
            Left: ILocalReferenceExpression: returnValue (IsDeclaration: False) (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'returnValue')
            Right: ILocalReferenceExpression: count (IsDeclaration: False) (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'count')
  IfFalse: null
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
IIfStatement (OperationKind.IfStatement) (Syntax: 'If count >  ... ata - count')
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'count > 10')
      Left: ILocalReferenceExpression: count (IsDeclaration: False) (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'count')
      Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
  IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'If count >  ... ata - count')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'data = data + count')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'data = data + count')
            Left: ILocalReferenceExpression: data (IsDeclaration: False) (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'data')
            Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'data + count')
                Left: ILocalReferenceExpression: data (IsDeclaration: False) (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'data')
                Right: ILocalReferenceExpression: count (IsDeclaration: False) (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'count')
  IfFalse: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'Else data = data - count')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'data = data - count')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'data = data - count')
            Left: ILocalReferenceExpression: data (IsDeclaration: False) (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'data')
            Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerSubtract) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'data - count')
                Left: ILocalReferenceExpression: data (IsDeclaration: False) (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'data')
                Right: ILocalReferenceExpression: count (IsDeclaration: False) (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'count')
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
IIfStatement (OperationKind.IfStatement) (Syntax: 'If m > 10 T ... rnValue = n')
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'm > 10')
      Left: ILocalReferenceExpression: m (IsDeclaration: False) (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'm')
      Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
  IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'If m > 10 T ... rnValue = n')
      IIfStatement (OperationKind.IfStatement) (Syntax: 'If n > 20 T ... rnValue = n')
        Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'n > 20')
            Left: ILocalReferenceExpression: n (IsDeclaration: False) (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'n')
            Right: ILiteralExpression (Text: 20) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 20) (Syntax: '20')
        IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'If n > 20 T ... rnValue = n')
            IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'returnValue = n')
              Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'returnValue = n')
                  Left: ILocalReferenceExpression: returnValue (IsDeclaration: False) (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'returnValue')
                  Right: ILocalReferenceExpression: n (IsDeclaration: False) (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'n')
        IfFalse: null
  IfFalse: null
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
IIfStatement (OperationKind.IfStatement) (Syntax: 'If 1 = 1 Th ... End If')
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerEquals) (OperationKind.BinaryOperatorExpression, Type: System.Boolean, Constant: True) (Syntax: '1 = 1')
      Left: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
      Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
  IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'If 1 = 1 Th ... End If')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'condition = True')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Boolean) (Syntax: 'condition = True')
            Left: ILocalReferenceExpression: condition (IsDeclaration: False) (OperationKind.LocalReferenceExpression, Type: System.Boolean) (Syntax: 'condition')
            Right: ILiteralExpression (Text: True) (OperationKind.LiteralExpression, Type: System.Boolean, Constant: True) (Syntax: 'True')
  IfFalse: null
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
IIfStatement (OperationKind.IfStatement) (Syntax: 'If False Th ... End If')
  Condition: ILiteralExpression (Text: False) (OperationKind.LiteralExpression, Type: System.Boolean, Constant: False) (Syntax: 'False')
  IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'If False Th ... End If')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'condition = False')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Boolean) (Syntax: 'condition = False')
            Left: ILocalReferenceExpression: condition (IsDeclaration: False) (OperationKind.LocalReferenceExpression, Type: System.Boolean) (Syntax: 'condition')
            Right: ILiteralExpression (Text: False) (OperationKind.LiteralExpression, Type: System.Boolean, Constant: False) (Syntax: 'False')
  IfFalse: null
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
IIfStatement (OperationKind.IfStatement) (Syntax: 'If (m > 10  ... rnValue = n')
  Condition: IParenthesizedExpression (OperationKind.ParenthesizedExpression, Type: System.Boolean) (Syntax: '(m > 10 And n > 20)')
      Operand: IBinaryOperatorExpression (BinaryOperationKind.BooleanAnd) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'm > 10 And n > 20')
          Left: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'm > 10')
              Left: ILocalReferenceExpression: m (IsDeclaration: False) (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'm')
              Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
          Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'n > 20')
              Left: ILocalReferenceExpression: n (IsDeclaration: False) (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'n')
              Right: ILiteralExpression (Text: 20) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 20) (Syntax: '20')
  IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'If (m > 10  ... rnValue = n')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'returnValue = n')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'returnValue = n')
            Left: ILocalReferenceExpression: returnValue (IsDeclaration: False) (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'returnValue')
            Right: ILocalReferenceExpression: n (IsDeclaration: False) (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'n')
  IfFalse: null
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
IIfStatement (OperationKind.IfStatement) (Syntax: 'If count >  ... End If')
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'count > 0')
      Left: ILocalReferenceExpression: count (IsDeclaration: False) (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'count')
      Right: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
  IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'If count >  ... End If')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'returnValue = count')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'returnValue = count')
            Left: ILocalReferenceExpression: returnValue (IsDeclaration: False) (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'returnValue')
            Right: ILocalReferenceExpression: count (IsDeclaration: False) (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'count')
  IfFalse: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'Else ... nValue = -1')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'returnValue = -1')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'returnValue = -1')
            Left: ILocalReferenceExpression: returnValue (IsDeclaration: False) (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'returnValue')
            Right: IUnaryOperatorExpression (UnaryOperationKind.IntegerMinus) (OperationKind.UnaryOperatorExpression, Type: System.Int32, Constant: -1) (Syntax: '-1')
                Operand: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
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
IIfStatement (OperationKind.IfStatement) (Syntax: 'If (m > 10) ... End If')
  Condition: IParenthesizedExpression (OperationKind.ParenthesizedExpression, Type: System.Boolean) (Syntax: '(m > 10)')
      Operand: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'm > 10')
          Left: ILocalReferenceExpression: m (IsDeclaration: False) (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'm')
          Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
  IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'If (m > 10) ... End If')
      IIfStatement (OperationKind.IfStatement) (Syntax: 'If (n > 20) ... End If')
        Condition: IParenthesizedExpression (OperationKind.ParenthesizedExpression, Type: System.Boolean) (Syntax: '(n > 20)')
            Operand: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'n > 20')
                Left: ILocalReferenceExpression: n (IsDeclaration: False) (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'n')
                Right: ILiteralExpression (Text: 20) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 20) (Syntax: '20')
        IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'If (n > 20) ... End If')
            IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.Wri ... "Result 1")')
              Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.String)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.Wri ... "Result 1")')
                  Instance Receiver: null
                  Arguments(1):
                      IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '"Result 1"')
                        ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "Result 1") (Syntax: '"Result 1"')
                        InConversion: null
                        OutConversion: null
        IfFalse: null
  IfFalse: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'Else ... "Result 2")')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.Wri ... "Result 2")')
        Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.String)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.Wri ... "Result 2")')
            Instance Receiver: null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '"Result 2"')
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "Result 2") (Syntax: '"Result 2"')
                  InConversion: null
                  OutConversion: null
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
IIfStatement (OperationKind.IfStatement) (Syntax: 'If (m > 10) ... End If')
  Condition: IParenthesizedExpression (OperationKind.ParenthesizedExpression, Type: System.Boolean) (Syntax: '(m > 10)')
      Operand: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'm > 10')
          Left: ILocalReferenceExpression: m (IsDeclaration: False) (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'm')
          Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
  IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'If (m > 10) ... End If')
      IIfStatement (OperationKind.IfStatement) (Syntax: 'If (n > 20) ... End If')
        Condition: IParenthesizedExpression (OperationKind.ParenthesizedExpression, Type: System.Boolean) (Syntax: '(n > 20)')
            Operand: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'n > 20')
                Left: ILocalReferenceExpression: n (IsDeclaration: False) (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'n')
                Right: ILiteralExpression (Text: 20) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 20) (Syntax: '20')
        IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'If (n > 20) ... End If')
            IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.Wri ... "Result 1")')
              Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.String)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.Wri ... "Result 1")')
                  Instance Receiver: null
                  Arguments(1):
                      IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '"Result 1"')
                        ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "Result 1") (Syntax: '"Result 1"')
                        InConversion: null
                        OutConversion: null
        IfFalse: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'Else ... "Result 2")')
            IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.Wri ... "Result 2")')
              Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.String)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.Wri ... "Result 2")')
                  Instance Receiver: null
                  Arguments(1):
                      IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '"Result 2"')
                        ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "Result 2") (Syntax: '"Result 2"')
                        InConversion: null
                        OutConversion: null
  IfFalse: null
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
IIfStatement (OperationKind.IfStatement) (Syntax: 'If (m >= n  ... End If')
  Condition: IParenthesizedExpression (OperationKind.ParenthesizedExpression, Type: System.Boolean) (Syntax: '(m >= n AndAlso m >= p)')
      Operand: IBinaryOperatorExpression (BinaryOperationKind.BooleanConditionalAnd) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'm >= n AndAlso m >= p')
          Left: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'm >= n')
              Left: ILocalReferenceExpression: m (IsDeclaration: False) (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'm')
              Right: ILocalReferenceExpression: n (IsDeclaration: False) (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'n')
          Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'm >= p')
              Left: ILocalReferenceExpression: m (IsDeclaration: False) (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'm')
              Right: ILocalReferenceExpression: p (IsDeclaration: False) (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'p')
  IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'If (m >= n  ... End If')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.Wri ... r than m.")')
        Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.String)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.Wri ... r than m.")')
            Instance Receiver: null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '"Nothing Is ... er than m."')
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "Nothing Is larger than m.") (Syntax: '"Nothing Is ... er than m."')
                  InConversion: null
                  OutConversion: null
  IfFalse: null
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
IIfStatement (OperationKind.IfStatement) (Syntax: 'If (m > 20) ... End If')
  Condition: IParenthesizedExpression (OperationKind.ParenthesizedExpression, Type: System.Boolean) (Syntax: '(m > 20)')
      Operand: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'm > 20')
          Left: ILocalReferenceExpression: m (IsDeclaration: False) (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'm')
          Right: ILiteralExpression (Text: 20) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 20) (Syntax: '20')
  IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'If (m > 20) ... End If')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.Wri ... ("Result1")')
        Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.String)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.Wri ... ("Result1")')
            Instance Receiver: null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '"Result1"')
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "Result1") (Syntax: '"Result1"')
                  InConversion: null
                  OutConversion: null
  IfFalse: IIfStatement (OperationKind.IfStatement) (Syntax: 'ElseIf (n > ... ("Result2")')
      Condition: IParenthesizedExpression (OperationKind.ParenthesizedExpression, Type: System.Boolean) (Syntax: '(n > 10)')
          Operand: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'n > 10')
              Left: ILocalReferenceExpression: n (IsDeclaration: False) (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'n')
              Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
      IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'ElseIf (n > ... ("Result2")')
          IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.Wri ... ("Result2")')
            Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.String)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.Wri ... ("Result2")')
                Instance Receiver: null
                Arguments(1):
                    IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '"Result2"')
                      ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "Result2") (Syntax: '"Result2"')
                      InConversion: null
                      OutConversion: null
      IfFalse: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'Else ... ("Result3")')
          IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.Wri ... ("Result3")')
            Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.String)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.Wri ... ("Result3")')
                Instance Receiver: null
                Arguments(1):
                    IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '"Result3"')
                      ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "Result3") (Syntax: '"Result3"')
                      InConversion: null
                      OutConversion: null
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
IIfStatement (OperationKind.IfStatement) (Syntax: 'If (m > 20) ... ("Result3")')
  Condition: IParenthesizedExpression (OperationKind.ParenthesizedExpression, Type: System.Boolean) (Syntax: '(m > 20)')
      Operand: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'm > 20')
          Left: ILocalReferenceExpression: m (IsDeclaration: False) (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'm')
          Right: ILiteralExpression (Text: 20) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 20) (Syntax: '20')
  IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'If (m > 20) ... ("Result3")')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... ("Result1")')
        Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.String)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... ("Result1")')
            Instance Receiver: null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '"Result1"')
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "Result1") (Syntax: '"Result1"')
                  InConversion: null
                  OutConversion: null
  IfFalse: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'Else If (n  ... ("Result3")')
      IIfStatement (OperationKind.IfStatement) (Syntax: 'If (n > 10) ... ("Result3")')
        Condition: IParenthesizedExpression (OperationKind.ParenthesizedExpression, Type: System.Boolean) (Syntax: '(n > 10)')
            Operand: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'n > 10')
                Left: ILocalReferenceExpression: n (IsDeclaration: False) (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'n')
                Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
        IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'If (n > 10) ... ("Result3")')
            IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... ("Result2")')
              Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.String)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... ("Result2")')
                  Instance Receiver: null
                  Arguments(1):
                      IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '"Result2"')
                        ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "Result2") (Syntax: '"Result2"')
                        InConversion: null
                        OutConversion: null
        IfFalse: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'Else System ... ("Result3")')
            IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... ("Result3")')
              Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.String)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... ("Result3")')
                  Instance Receiver: null
                  Arguments(1):
                      IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '"Result3"')
                        ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "Result3") (Syntax: '"Result3"')
                        InConversion: null
                        OutConversion: null
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
IIfStatement (OperationKind.IfStatement, IsInvalid) (Syntax: 'If (m > 20) ... Else')
  Condition: IParenthesizedExpression (OperationKind.ParenthesizedExpression, Type: System.Boolean, IsInvalid) (Syntax: '(m > 20)')
      Operand: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean, IsInvalid) (Syntax: 'm > 20')
          Left: ILocalReferenceExpression: m (IsDeclaration: False) (OperationKind.LocalReferenceExpression, Type: System.Int32, IsInvalid) (Syntax: 'm')
          Right: ILiteralExpression (Text: 20) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 20, IsInvalid) (Syntax: '20')
  IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement, IsInvalid) (Syntax: 'If (m > 20) ... Else')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.Wri ... ("Result1")')
        Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.String)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.Wri ... ("Result1")')
            Instance Receiver: null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '"Result1"')
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "Result1") (Syntax: '"Result1"')
                  InConversion: null
                  OutConversion: null
  IfFalse: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'Else')
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
IIfStatement (OperationKind.IfStatement, IsInvalid) (Syntax: 'If () Then' ... End If')
  Condition: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Boolean, IsInvalid) (Syntax: '()')
      Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: IParenthesizedExpression (OperationKind.ParenthesizedExpression, Type: ?, IsInvalid) (Syntax: '()')
          Operand: IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '')
              Children(0)
  IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement, IsInvalid) (Syntax: 'If () Then' ... End If')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.Wri ... ("Result1")')
        Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.String)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.Wri ... ("Result1")')
            Instance Receiver: null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '"Result1"')
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "Result1") (Syntax: '"Result1"')
                  InConversion: null
                  OutConversion: null
  IfFalse: null
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
IIfStatement (OperationKind.IfStatement) (Syntax: 'If (m = 9)  ... End If')
  Condition: IParenthesizedExpression (OperationKind.ParenthesizedExpression, Type: System.Boolean) (Syntax: '(m = 9)')
      Operand: IBinaryOperatorExpression (BinaryOperationKind.IntegerEquals) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'm = 9')
          Left: ILocalReferenceExpression: m (IsDeclaration: False) (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'm')
          Right: ILiteralExpression (Text: 9) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 9) (Syntax: '9')
  IfTrue: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'If (m = 9)  ... End If')
  IfFalse: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'Else')
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
IIfStatement (OperationKind.IfStatement) (Syntax: 'If (True) T ... End If')
  Condition: IParenthesizedExpression (OperationKind.ParenthesizedExpression, Type: System.Boolean, Constant: True) (Syntax: '(True)')
      Operand: ILiteralExpression (Text: True) (OperationKind.LiteralExpression, Type: System.Boolean, Constant: True) (Syntax: 'True')
  IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'If (True) T ... End If')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'A()')
        Expression: IInvocationExpression (Function Module1.A() As System.String) (OperationKind.InvocationExpression, Type: System.String) (Syntax: 'A()')
            Instance Receiver: null
            Arguments(0)
  IfFalse: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'Else ... B()')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'B()')
        Expression: IInvocationExpression (Function Module1.B() As System.String) (OperationKind.InvocationExpression, Type: System.String) (Syntax: 'B()')
            Instance Receiver: null
            Arguments(0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of MultiLineIfBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

    End Class
End Namespace
