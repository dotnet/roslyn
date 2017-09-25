' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics
    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ILabelStatement_SimpleLabelTest()
            Dim source = <![CDATA[
Option Strict On
Imports System

Public Class C1
    Public Sub M1()'BIND:"Public Sub M1()"
        GoTo Label
Label:  Console.WriteLine("Hello World!")
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IBlockStatement (5 statements) (OperationKind.BlockStatement) (Syntax: 'Public Sub  ... End Sub')
  IBranchStatement (BranchKind.GoTo, Label: Label) (OperationKind.BranchStatement) (Syntax: 'GoTo Label')
  ILabeledStatement (Label: Label) (OperationKind.LabeledStatement) (Syntax: 'Label:')
    Statement: null
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.Wri ... lo World!")')
    Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.String)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.Wri ... lo World!")')
        Instance Receiver: null
        Arguments(1):
            IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '"Hello World!"')
              ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "Hello World!") (Syntax: '"Hello World!"')
              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  ILabeledStatement (Label: exit) (OperationKind.LabeledStatement) (Syntax: 'End Sub')
    Statement: null
  IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'End Sub')
    ReturnedValue: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub
    End Class
End Namespace
