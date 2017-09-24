' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub WithStatement_Basic()
            Dim source = <![CDATA[
Class C
    Public I, J As Integer
End Class

Class D
    Private Sub M(c As C)
        With c'BIND:"With c"
            .I = 0
            .J = 0
        End With

    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IWithStatement (OperationKind.None, Language: Visual Basic) (Syntax: 'With c'BIND ... End With')
  Value: IParameterReferenceExpression: c (OperationKind.ParameterReferenceExpression, Type: C, Language: Visual Basic) (Syntax: 'c')
  Body: IBlockStatement (2 statements) (OperationKind.BlockStatement, Language: Visual Basic) (Syntax: 'With c'BIND ... End With')
      IExpressionStatement (OperationKind.ExpressionStatement, Language: Visual Basic) (Syntax: '.I = 0')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, Language: Visual Basic) (Syntax: '.I = 0')
            Left: IFieldReferenceExpression: C.I As System.Int32 (OperationKind.FieldReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: '.I')
                Instance Receiver: IOperation:  (OperationKind.None, IsImplicit, Language: Visual Basic) (Syntax: 'c')
            Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0, Language: Visual Basic) (Syntax: '0')
      IExpressionStatement (OperationKind.ExpressionStatement, Language: Visual Basic) (Syntax: '.J = 0')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, Language: Visual Basic) (Syntax: '.J = 0')
            Left: IFieldReferenceExpression: C.J As System.Int32 (OperationKind.FieldReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: '.J')
                Instance Receiver: IOperation:  (OperationKind.None, IsImplicit, Language: Visual Basic) (Syntax: 'c')
            Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0, Language: Visual Basic) (Syntax: '0')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of WithBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub WithStatement_Parent()
            Dim source = <![CDATA[
Class C
    Public I, J As Integer
End Class

Class D
    Private Sub M(c As C)'BIND:"Private Sub M(c As C)"
        With c
            .I = 0
            .J = 0
        End With

    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IBlockStatement (3 statements) (OperationKind.BlockStatement, Language: Visual Basic) (Syntax: 'Private Sub ... End Sub')
  IWithStatement (OperationKind.None, Language: Visual Basic) (Syntax: 'With c ... End With')
    Value: IParameterReferenceExpression: c (OperationKind.ParameterReferenceExpression, Type: C, Language: Visual Basic) (Syntax: 'c')
    Body: IBlockStatement (2 statements) (OperationKind.BlockStatement, Language: Visual Basic) (Syntax: 'With c ... End With')
        IExpressionStatement (OperationKind.ExpressionStatement, Language: Visual Basic) (Syntax: '.I = 0')
          Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, Language: Visual Basic) (Syntax: '.I = 0')
              Left: IFieldReferenceExpression: C.I As System.Int32 (OperationKind.FieldReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: '.I')
                  Instance Receiver: IOperation:  (OperationKind.None, IsImplicit, Language: Visual Basic) (Syntax: 'c')
              Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0, Language: Visual Basic) (Syntax: '0')
        IExpressionStatement (OperationKind.ExpressionStatement, Language: Visual Basic) (Syntax: '.J = 0')
          Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, Language: Visual Basic) (Syntax: '.J = 0')
              Left: IFieldReferenceExpression: C.J As System.Int32 (OperationKind.FieldReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: '.J')
                  Instance Receiver: IOperation:  (OperationKind.None, IsImplicit, Language: Visual Basic) (Syntax: 'c')
              Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0, Language: Visual Basic) (Syntax: '0')
  ILabeledStatement (Label: exit) (OperationKind.LabeledStatement, Language: Visual Basic) (Syntax: 'End Sub')
    Statement: null
  IReturnStatement (OperationKind.ReturnStatement, Language: Visual Basic) (Syntax: 'End Sub')
    ReturnedValue: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub
    End Class
End Namespace
