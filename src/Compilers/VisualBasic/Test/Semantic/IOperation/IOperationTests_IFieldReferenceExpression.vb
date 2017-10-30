' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Semantics
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(8884, "https://github.com/dotnet/roslyn/issues/8884")>
        Public Sub FieldReference_Attribute()
            Dim source = <![CDATA[
Imports System.Diagnostics

Class C
    Private Const field As String = NameOf(field)

    <Conditional(field)>'BIND:"Conditional(field)"
    Private Sub M()
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IOperation:  (OperationKind.None) (Syntax: 'Conditional(field)')
  Children(1):
      IFieldReferenceExpression: C.field As System.String (Static) (OperationKind.FieldReferenceExpression, Type: System.String, Constant: "field") (Syntax: 'field')
        Instance Receiver: 
          null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of AttributeSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(7582, "https://github.com/dotnet/roslyn/issues/7582")>
        Public Sub FieldReference_ImplicitMe()
            Dim source = <![CDATA[
Class C
    Private i As Integer

    Private Sub M()
         i = 1 'BIND:"i"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IFieldReferenceExpression: C.i As System.Int32 (OperationKind.FieldReferenceExpression, Type: System.Int32) (Syntax: 'i')
  Instance Receiver: 
    IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: C, IsImplicit) (Syntax: 'i')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of IdentifierNameSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(7582, "https://github.com/dotnet/roslyn/issues/7582")>
        Public Sub FieldReference_ExplicitMe()
            Dim source = <![CDATA[
Class C
    Private i As Integer

    Private Sub M()
         Me.i = 1 'BIND:"Me.i"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IFieldReferenceExpression: C.i As System.Int32 (OperationKind.FieldReferenceExpression, Type: System.Int32) (Syntax: 'Me.i')
  Instance Receiver: 
    IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: C) (Syntax: 'Me')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of MemberAccessExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(7582, "https://github.com/dotnet/roslyn/issues/7582")>
        Public Sub FieldReference_MyBase()
            Dim source = <![CDATA[
Class C
    Protected i As Integer
End Class
Class B
    Inherits C
    Private Sub M()
         MyBase.i = 1 'BIND:"MyBase.i"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IFieldReferenceExpression: C.i As System.Int32 (OperationKind.FieldReferenceExpression, Type: System.Int32) (Syntax: 'MyBase.i')
  Instance Receiver: 
    IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: C) (Syntax: 'MyBase')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of MemberAccessExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(7582, "https://github.com/dotnet/roslyn/issues/7582")>
        Public Sub FieldReference_MyClass()
            Dim source = <![CDATA[
Class C
    Private i As Integer

    Private Sub M()
         MyClass.i = 1 'BIND:"MyClass.i"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IFieldReferenceExpression: C.i As System.Int32 (OperationKind.FieldReferenceExpression, Type: System.Int32) (Syntax: 'MyClass.i')
  Instance Receiver: 
    IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: C) (Syntax: 'MyClass')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of MemberAccessExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub
    End Class
End Namespace
