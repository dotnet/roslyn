' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics
    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub NameOfFlow_01()
            Dim source = <![CDATA[
Public Class C
    Public Sub M1(b As Boolean, i1 As Integer, i2 As Integer)'BIND:"Public Sub M1(b As Boolean, i1 As Integer, i2 As Integer)"
        Dim s As String = NameOf(If(b, i1, i2))
    End Sub
End Class
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC37244: This expression does not have a name.
        Dim s As String = NameOf(If(b, i1, i2))
                                 ~~~~~~~~~~~~~
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [s As System.String]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String, IsInvalid, IsImplicit) (Syntax: 's As String ... b, i1, i2))')
              Left: 
                ILocalReferenceOperation: s (IsDeclaration: True) (OperationKind.LocalReference, Type: System.String, IsImplicit) (Syntax: 's')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: null, IsInvalid) (Syntax: 'NameOf(If(b, i1, i2))')

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
        Public Sub NameOfFlow_02()
            Dim source = <![CDATA[
Public Class C
    Public Sub M1(i1 As Integer)'BIND:"Public Sub M1(i1 As Integer)"
        Dim s As String = NameOf(i1)
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
    Locals: [s As System.String]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String, IsImplicit) (Syntax: 's As String = NameOf(i1)')
              Left: 
                ILocalReferenceOperation: s (IsDeclaration: True) (OperationKind.LocalReference, Type: System.String, IsImplicit) (Syntax: 's')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "i1") (Syntax: 'NameOf(i1)')

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
        Public Sub NameOfFlow_03()
            Dim source = <![CDATA[
Public Class C
    Public Sub M1(b As Boolean, i1 As Integer, i2 As Integer)'BIND:"Public Sub M1(b As Boolean, i1 As Integer, i2 As Integer)"
        Dim s As String = If(b, NameOf(i1), NameOf(i2))
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
    Locals: [s As System.String]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (0)
        Jump if False (Regular) to Block[B3]
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'NameOf(i1)')
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "i1") (Syntax: 'NameOf(i1)')

        Next (Regular) Block[B4]
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'NameOf(i2)')
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "i2") (Syntax: 'NameOf(i2)')

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String, IsImplicit) (Syntax: 's As String ... NameOf(i2))')
              Left: 
                ILocalReferenceOperation: s (IsDeclaration: True) (OperationKind.LocalReference, Type: System.String, IsImplicit) (Syntax: 's')
              Right: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'If(b, NameO ... NameOf(i2))')

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
