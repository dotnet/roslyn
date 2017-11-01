' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Operations
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
IOperation:  (OperationKind.None, Type: null) (Syntax: 'Conditional(field)')
  Children(1):
      IFieldReferenceOperation: C.field As System.String (Static) (OperationKind.FieldReference, Type: System.String, Constant: "field") (Syntax: 'field')
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
IFieldReferenceOperation: C.i As System.Int32 (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'i')
  Instance Receiver: 
    IInstanceReferenceOperation (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'i')
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
IFieldReferenceOperation: C.i As System.Int32 (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'Me.i')
  Instance Receiver: 
    IInstanceReferenceOperation (OperationKind.InstanceReference, Type: C) (Syntax: 'Me')
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
IFieldReferenceOperation: C.i As System.Int32 (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'MyBase.i')
  Instance Receiver: 
    IInstanceReferenceOperation (OperationKind.InstanceReference, Type: C) (Syntax: 'MyBase')
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
IFieldReferenceOperation: C.i As System.Int32 (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'MyClass.i')
  Instance Receiver: 
    IInstanceReferenceOperation (OperationKind.InstanceReference, Type: C) (Syntax: 'MyClass')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of MemberAccessExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub IFieldReference_SharedField()
            Dim source = <![CDATA[
Option Strict On
Imports System

Module M1
    Class C1
        Shared i1 As Integer
        Shared Sub S2()
            Dim i2 = i1'BIND:"i1"
        End Sub
    End Class
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IFieldReferenceOperation: M1.C1.i1 As System.Int32 (Static) (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'i1')
  Instance Receiver: 
    null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of IdentifierNameSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub IFieldReference_SharedFieldWithInstanceReceiver()
            Dim source = <![CDATA[
Option Strict On
Imports System

Module M1
    Class C1
        Shared i1 As Integer = 1
        Shared Sub S2()
            Dim c1Instance As New C1
            Dim i1 = c1Instance.i1'BIND:"c1Instance.i1"
        End Sub
    End Class
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IFieldReferenceOperation: M1.C1.i1 As System.Int32 (Static) (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'c1Instance.i1')
  Instance Receiver: 
    ILocalReferenceOperation: c1Instance (OperationKind.LocalReference, Type: M1.C1) (Syntax: 'c1Instance')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
            Dim i1 = c1Instance.i1'BIND:"c1Instance.i1"
                     ~~~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of MemberAccessExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub
    End Class
End Namespace
