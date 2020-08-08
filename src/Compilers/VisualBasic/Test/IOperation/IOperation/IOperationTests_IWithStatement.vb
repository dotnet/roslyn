' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

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
IWithStatementOperation (OperationKind.None, Type: null) (Syntax: 'With c'BIND ... End With')
  Value: 
    IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C) (Syntax: 'c')
  Body: 
    IBlockOperation (2 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'With c'BIND ... End With')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: '.I = 0')
        Expression: 
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: '.I = 0')
            Left: 
              IFieldReferenceOperation: C.I As System.Int32 (OperationKind.FieldReference, Type: System.Int32) (Syntax: '.I')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'c')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: '.J = 0')
        Expression: 
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: '.J = 0')
            Left: 
              IFieldReferenceOperation: C.J As System.Int32 (OperationKind.FieldReference, Type: System.Int32) (Syntax: '.J')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'c')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
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
IBlockOperation (3 statements) (OperationKind.Block, Type: null) (Syntax: 'Private Sub ... End Sub')
  IWithStatementOperation (OperationKind.None, Type: null) (Syntax: 'With c ... End With')
    Value: 
      IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C) (Syntax: 'c')
    Body: 
      IBlockOperation (2 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'With c ... End With')
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: '.I = 0')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: '.I = 0')
              Left: 
                IFieldReferenceOperation: C.I As System.Int32 (OperationKind.FieldReference, Type: System.Int32) (Syntax: '.I')
                  Instance Receiver: 
                    IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'c')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: '.J = 0')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: '.J = 0')
              Left: 
                IFieldReferenceOperation: C.J As System.Int32 (OperationKind.FieldReference, Type: System.Int32) (Syntax: '.J')
                  Instance Receiver: 
                    IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'c')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
  ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'End Sub')
    Statement: 
      null
  IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'End Sub')
    ReturnedValue: 
      null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub IWithStatement_StaticFieldReferenceAsRValue_NoInstanceReceiver()
            Dim source = <![CDATA[
Imports System

Structure SSS
    Public Shared A As String

    Public Sub New(_a As String)
    End Sub
End Structure

Class Clazz
    Sub TEST(i As Integer)
        With New SSS(Me.ToString())
            .A = ""'BIND:".A"
        End With
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IFieldReferenceOperation: SSS.A As System.String (Static) (OperationKind.FieldReference, Type: System.String) (Syntax: '.A')
  Instance Receiver: 
    null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
            .A = ""'BIND:".A"
            ~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of MemberAccessExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub WithFlow_01()
            Dim source = <![CDATA[
Class C
    Public I, J As Integer
End Class

Class D
    Sub M(c As C) 'BIND:"Sub M"
        With c
            .I = 0
            .J = 1
        End With

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
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (3)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value: 
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C) (Syntax: 'c')

            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: '.I = 0')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: '.I = 0')
                  Left: 
                    IFieldReferenceOperation: C.I As System.Int32 (OperationKind.FieldReference, Type: System.Int32) (Syntax: '.I')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: '.J = 1')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: '.J = 1')
                  Left: 
                    IFieldReferenceOperation: C.J As System.Int32 (OperationKind.FieldReference, Type: System.Int32) (Syntax: '.J')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Next (Regular) Block[B2]
            Leaving: {R1}
}

Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub WithFlow_02()
            Dim source = <![CDATA[
Structure C
    Public I, J As Integer
End Structure

Class D
    Sub M(c As C) 'BIND:"Sub M"
        With c
            .I = 0
            .J = 1
        End With

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
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (3)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value: 
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C) (Syntax: 'c')

            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: '.I = 0')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: '.I = 0')
                  Left: 
                    IFieldReferenceOperation: C.I As System.Int32 (OperationKind.FieldReference, Type: System.Int32) (Syntax: '.I')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: '.J = 1')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: '.J = 1')
                  Left: 
                    IFieldReferenceOperation: C.J As System.Int32 (OperationKind.FieldReference, Type: System.Int32) (Syntax: '.J')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Next (Regular) Block[B2]
            Leaving: {R1}
}

Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub WithFlow_03()
            Dim source = <![CDATA[
Structure C
    Public I, J As Integer
End Structure

Class D
    Sub M(c As C) 'BIND:"Sub M"
            .I = 0
    End Sub
End Class
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30157: Leading '.' or '!' can only appear inside a 'With' statement.
            .I = 0
            ~~
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: '.I = 0')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?, IsInvalid, IsImplicit) (Syntax: '.I = 0')
              Left: 
                IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: '.I')
                  Children(1):
                      IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: '.I')
                        Children(0)
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub WithFlow_04()
            Dim source = <![CDATA[
Structure C1
    Public K As C2
    Public L As C3
End Structure

Structure C2
    Public I As Integer
End Structure

Structure C3
    Public J As Integer
End Structure

Class D
    Sub M(c1 As C1, c3 As C3) 'BIND:"Sub M"
        With c1
            .K = new C2() With {.I = 1}

            With c3
                .J = 2
            End With

            .L = new C3() With {.J = 3}
        End With
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
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
              Value: 
                IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C1) (Syntax: 'c1')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1] [2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (4)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '.K')
                  Value: 
                    IFieldReferenceOperation: C1.K As C2 (OperationKind.FieldReference, Type: C2) (Syntax: '.K')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'c1')

                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new C2() With {.I = 1}')
                  Value: 
                    IObjectCreationOperation (Constructor: Sub C2..ctor()) (OperationKind.ObjectCreation, Type: C2) (Syntax: 'new C2() With {.I = 1}')
                      Arguments(0)
                      Initializer: 
                        null

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: '.I = 1')
                  Left: 
                    IFieldReferenceOperation: C2.I As System.Int32 (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'I')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: C2, IsImplicit) (Syntax: 'new C2() With {.I = 1}')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: '.K = new C2 ... th {.I = 1}')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C2, IsImplicit) (Syntax: '.K = new C2 ... th {.I = 1}')
                      Left: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C2, IsImplicit) (Syntax: '.K')
                      Right: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: C2, IsImplicit) (Syntax: 'new C2() With {.I = 1}')

            Next (Regular) Block[B3]
                Leaving: {R2}
                Entering: {R3}
    }
    .locals {R3}
    {
        CaptureIds: [3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (2)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c3')
                  Value: 
                    IParameterReferenceOperation: c3 (OperationKind.ParameterReference, Type: C3) (Syntax: 'c3')

                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: '.J = 2')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: '.J = 2')
                      Left: 
                        IFieldReferenceOperation: C3.J As System.Int32 (OperationKind.FieldReference, Type: System.Int32) (Syntax: '.J')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: C3, IsImplicit) (Syntax: 'c3')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

            Next (Regular) Block[B4]
                Leaving: {R3}
                Entering: {R4}
    }
    .locals {R4}
    {
        CaptureIds: [4] [5]
        Block[B4] - Block
            Predecessors: [B3]
            Statements (4)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '.L')
                  Value: 
                    IFieldReferenceOperation: C1.L As C3 (OperationKind.FieldReference, Type: C3) (Syntax: '.L')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'c1')

                IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new C3() With {.J = 3}')
                  Value: 
                    IObjectCreationOperation (Constructor: Sub C3..ctor()) (OperationKind.ObjectCreation, Type: C3) (Syntax: 'new C3() With {.J = 3}')
                      Arguments(0)
                      Initializer: 
                        null

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: '.J = 3')
                  Left: 
                    IFieldReferenceOperation: C3.J As System.Int32 (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'J')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: C3, IsImplicit) (Syntax: 'new C3() With {.J = 3}')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')

                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: '.L = new C3 ... th {.J = 3}')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C3, IsImplicit) (Syntax: '.L = new C3 ... th {.J = 3}')
                      Left: 
                        IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: C3, IsImplicit) (Syntax: '.L')
                      Right: 
                        IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: C3, IsImplicit) (Syntax: 'new C3() With {.J = 3}')

            Next (Regular) Block[B5]
                Leaving: {R4} {R1}
    }
}

Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub WithFlow_05()
            Dim source = <![CDATA[
Class C
    Public I As Integer
End Class

Class D
    Sub M(c1 As C, c2 As C) 'BIND:"Sub M"
        With If(c1, c2)
            .I = 0
        End With
    End Sub
End Class
]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.locals {R1}
{
    CaptureIds: [1]
    .locals {R2}
    {
        CaptureIds: [0]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (1)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                  Value: 
                    IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C) (Syntax: 'c1')

            Jump if True (Regular) to Block[B3]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')
                Leaving: {R2}

            Next (Regular) Block[B2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                  Value: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')

            Next (Regular) Block[B4]
                Leaving: {R2}
    }

    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c2')
              Value: 
                IParameterReferenceOperation: c2 (OperationKind.ParameterReference, Type: C) (Syntax: 'c2')

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: '.I = 0')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: '.I = 0')
                  Left: 
                    IFieldReferenceOperation: C.I As System.Int32 (OperationKind.FieldReference, Type: System.Int32) (Syntax: '.I')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'If(c1, c2)')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

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
        Public Sub WithFlow_06()
            Dim source = <![CDATA[
Class C
    Public I As Integer
End Class

Class D
    Sub M(c As C) 'BIND:"Sub M"
        With c
            Dim d As System.Action(Of Integer) = Sub(x As Integer) x = .I
        End With
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
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value: 
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C) (Syntax: 'c')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        Locals: [d As System.Action(Of System.Int32)]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Action(Of System.Int32), IsImplicit) (Syntax: 'd As System ... ger) x = .I')
                  Left: 
                    ILocalReferenceOperation: d (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Action(Of System.Int32), IsImplicit) (Syntax: 'd')
                  Right: 
                    IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action(Of System.Int32), IsImplicit) (Syntax: 'Sub(x As Integer) x = .I')
                      Target: 
                        IFlowAnonymousFunctionOperation (Symbol: Sub (x As System.Int32)) (OperationKind.FlowAnonymousFunction, Type: null) (Syntax: 'Sub(x As Integer) x = .I')
                        {
                            Block[B0#A0] - Entry
                                Statements (0)
                                Next (Regular) Block[B1#A0]
                            Block[B1#A0] - Block
                                Predecessors: [B0#A0]
                                Statements (1)
                                    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = .I')
                                      Expression: 
                                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'x = .I')
                                          Left: 
                                            IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
                                          Right: 
                                            IFieldReferenceOperation: C.I As System.Int32 (OperationKind.FieldReference, Type: System.Int32) (Syntax: '.I')
                                              Instance Receiver: 
                                                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c')

                                Next (Regular) Block[B2#A0]
                            Block[B2#A0] - Exit
                                Predecessors: [B1#A0]
                                Statements (0)
                        }

            Next (Regular) Block[B3]
                Leaving: {R2} {R1}
    }
}

Block[B3] - Exit
    Predecessors: [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub WithFlow_07()
            Dim source = <![CDATA[
Class C
    Public I, J As Integer
End Class

Class D
    Sub M() 'BIND:"Sub M"
        With New With { .I = 0 }
            .I = 1
        End With
    End Sub
End Class
]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.locals {R1}
{
    CaptureIds: [1]
    .locals {R2}
    {
        CaptureIds: [0]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (2)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '0')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'New With { .I = 0 }')
                  Value: 
                    IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: I As System.Int32>) (Syntax: 'New With { .I = 0 }')
                      Initializers(1):
                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, Constant: 0) (Syntax: '.I = 0')
                            Left: 
                              IPropertyReferenceOperation: Property <anonymous type: I As System.Int32>.I As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'I')
                                Instance Receiver: 
                                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: I As System.Int32>, IsImplicit) (Syntax: 'New With { .I = 0 }')
                            Right: 
                              IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 0, IsImplicit) (Syntax: '0')

            Next (Regular) Block[B2]
                Leaving: {R2}
    }

    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: '.I = 1')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Void, IsImplicit) (Syntax: '.I = 1')
                  Left: 
                    IPropertyReferenceOperation: Property <anonymous type: I As System.Int32>.I As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: '.I')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: <anonymous type: I As System.Int32>, IsImplicit) (Syntax: 'New With { .I = 0 }')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Exit
    Predecessors: [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub WithFlow_08()
            Dim source = <![CDATA[
Class C
    Public I As Integer
End Class

Class D
    Sub M(c As C) 'BIND:"Sub M"
        With c
            ?.ToString()
        End With
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
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value: 
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C) (Syntax: 'c')

        Jump if True (Regular) to Block[B3]
            IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c')
              Operand: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c')
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: '?.ToString()')
              Expression: 
                IInvocationOperation (virtual Function System.Object.ToString() As System.String) (OperationKind.Invocation, Type: System.String) (Syntax: '.ToString()')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c')
                  Arguments(0)

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub
    End Class
End Namespace
