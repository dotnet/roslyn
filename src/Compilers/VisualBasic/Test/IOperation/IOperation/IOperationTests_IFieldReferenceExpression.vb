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
    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'i')
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
    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C) (Syntax: 'Me')
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
    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C) (Syntax: 'MyBase')
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
    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C) (Syntax: 'MyClass')
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

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub IFieldReference_SharedFieldAccessOnClass()
            Dim source = <![CDATA[
Option Strict On
Imports System

Module M1
    Class C1
        Shared i1 As Integer = 1
        Shared Sub S2()
            Dim i1 = C1.i1'BIND:"C1.i1"
        End Sub
    End Class
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IFieldReferenceOperation: M1.C1.i1 As System.Int32 (Static) (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'C1.i1')
  Instance Receiver: 
    null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of MemberAccessExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub IFieldReference_InstanceFieldAccessOnClass()
            Dim source = <![CDATA[
Option Strict On
Imports System

Module M1
    Class C1
        Dim i1 As Integer = 1
        Shared Sub S2()
            Dim i1 = C1.i1'BIND:"C1.i1"
        End Sub
    End Class
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IFieldReferenceOperation: M1.C1.i1 As System.Int32 (OperationKind.FieldReference, Type: System.Int32, IsInvalid) (Syntax: 'C1.i1')
  Instance Receiver: 
    null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30469: Reference to a non-shared member requires an object reference.
            Dim i1 = C1.i1'BIND:"C1.i1"
                     ~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of MemberAccessExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub IFieldReference_StaticFieldReferenceInInitializer_RightHandSide()
            Dim source = <![CDATA[
Option Strict On
Imports System

Module M1
    Class C1
        Public Shared i1 As Integer
        Public i2 As Integer
    End Class

    Sub S1()
        Dim a = New C1 With {.i2 = .i1}'BIND:".i1"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IFieldReferenceOperation: M1.C1.i1 As System.Int32 (Static) (OperationKind.FieldReference, Type: System.Int32) (Syntax: '.i1')
  Instance Receiver: 
    null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of MemberAccessExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact>
        Public Sub FieldReference_NoControlFlow()
            ' Verify mix of field references with implicit/explicit/null instance in lvalue/rvalue contexts.
            Dim source = <![CDATA[
Imports System

Friend Class C
    Private i As Integer
    Private Shared j As Integer

    Public Sub M(c As C)'BIND:"Public Sub M(c As C)"
        i = C.j
        j = Me.i + c.i
    End Sub
End Class]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (2)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'i = C.j')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'i = C.j')
              Left: 
                IFieldReferenceOperation: C.i As System.Int32 (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'i')
                  Instance Receiver: 
                    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'i')
              Right: 
                IFieldReferenceOperation: C.j As System.Int32 (Static) (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'C.j')
                  Instance Receiver: 
                    null

        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'j = Me.i + c.i')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'j = Me.i + c.i')
              Left: 
                IFieldReferenceOperation: C.j As System.Int32 (Static) (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'j')
                  Instance Receiver: 
                    null
              Right: 
                IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32) (Syntax: 'Me.i + c.i')
                  Left: 
                    IFieldReferenceOperation: C.i As System.Int32 (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'Me.i')
                      Instance Receiver: 
                        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C) (Syntax: 'Me')
                  Right: 
                    IFieldReferenceOperation: C.i As System.Int32 (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'c.i')
                      Instance Receiver: 
                        IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C) (Syntax: 'c')

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact>
        Public Sub FieldReference_ControlFlowInReceiver()
            Dim source = <![CDATA[
Imports System

Friend Class C
    Public i As Integer = 0

    Public Sub M(c1 As C, c2 As C, p As Integer)'BIND:"Public Sub M(c1 As C, c2 As C, p As Integer)"
        p = If (c1, c2).i
    End Sub
End Class]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [2]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'p')
              Value: 
                IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'p')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                  Value: 
                    IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C) (Syntax: 'c1')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                  Value: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c2')
              Value: 
                IParameterReferenceOperation: c2 (OperationKind.ParameterReference, Type: C) (Syntax: 'c2')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'p = If (c1, c2).i')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'p = If (c1, c2).i')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'p')
                  Right: 
                    IFieldReferenceOperation: C.i As System.Int32 (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'If (c1, c2).i')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'If (c1, c2)')

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact>
        Public Sub FieldReference_ControlFlowInReceiver_StaticField()
            Dim source = <![CDATA[
Imports System

Friend Class C
    Public Shared i As Integer = 0

    Public Sub M(c1 As C, c2 As C, p1 As Integer, p2 As Integer)'BIND:"Public Sub M(c1 As C, c2 As C, p1 As Integer, p2 As Integer)"
        p1 = c1.i
        p2 = If (c1, c2).i
    End Sub
End Class]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (2)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'p1 = c1.i')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'p1 = c1.i')
              Left: 
                IParameterReferenceOperation: p1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'p1')
              Right: 
                IFieldReferenceOperation: C.i As System.Int32 (Static) (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'c1.i')
                  Instance Receiver: 
                    null

        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'p2 = If (c1, c2).i')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'p2 = If (c1, c2).i')
              Left: 
                IParameterReferenceOperation: p2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'p2')
              Right: 
                IFieldReferenceOperation: C.i As System.Int32 (Static) (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'If (c1, c2).i')
                  Instance Receiver: 
                    null

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        p1 = c1.i
             ~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        p2 = If (c1, c2).i
             ~~~~~~~~~~~~~
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub
    End Class
End Namespace
