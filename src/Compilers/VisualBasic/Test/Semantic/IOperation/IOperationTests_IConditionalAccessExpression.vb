' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub IConditionalAccessExpression_SimpleMethodAccess()
            Dim source = <![CDATA[
Option Strict On

Public Class C1
    Public Sub M1()
        Dim o As New Object
        o?.ToString()'BIND:"o?.ToString()"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IConditionalAccessOperation (OperationKind.ConditionalAccess, IsExpression, Type: System.Void) (Syntax: 'o?.ToString()')
  Operation: 
    ILocalReferenceOperation: o (OperationKind.LocalReference, IsExpression, Type: System.Object) (Syntax: 'o')
  WhenNotNull: 
    IInvocationOperation (virtual Function System.Object.ToString() As System.String) (OperationKind.Invocation, IsExpression, Type: System.String) (Syntax: '.ToString()')
      Instance Receiver: 
        IPlaceholderOperation (OperationKind.None, IsExpression, Type: System.Object, IsImplicit) (Syntax: 'o?.ToString()')
      Arguments(0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ConditionalAccessExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub IConditionalAccessExpression_SimplePropertyAccess()
            Dim source = <![CDATA[
Option Strict On

Public Class C1

    Public ReadOnly Property Prop1 As Integer

    Public Sub M1()
        Dim c1 As C1 = Nothing
        Dim propValue = c1?.Prop1'BIND:"c1?.Prop1"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IConditionalAccessOperation (OperationKind.ConditionalAccess, IsExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 'c1?.Prop1')
  Operation: 
    ILocalReferenceOperation: c1 (OperationKind.LocalReference, IsExpression, Type: C1) (Syntax: 'c1')
  WhenNotNull: 
    IPropertyReferenceOperation: ReadOnly Property C1.Prop1 As System.Int32 (OperationKind.PropertyReference, IsExpression, Type: System.Int32) (Syntax: '.Prop1')
      Instance Receiver: 
        IPlaceholderOperation (OperationKind.None, IsExpression, Type: C1, IsImplicit) (Syntax: 'c1?.Prop1')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ConditionalAccessExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub
    End Class
End Namespace
