' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics
    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub NoneOperation_Expression_01()
            Dim source = <![CDATA[
Class C
    Public Sub F(str As String)'BIND:"Public Sub F(str As String)"
        Mid(str, 1, 1) = ""
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Mid(str, 1, 1) = ""')
          Expression: 
            IOperation:  (OperationKind.None, Type: null, IsImplicit) (Syntax: 'Mid(str, 1, 1) = ""')
              Children(2):
                  IParameterReferenceOperation: str (OperationKind.ParameterReference, Type: System.String) (Syntax: 'str')
                  IOperation:  (OperationKind.None, Type: null, IsImplicit) (Syntax: 'Mid(str, 1, 1) = ""')
                    Children(4):
                        IParenthesizedOperation (OperationKind.Parenthesized, Type: System.String) (Syntax: 'Mid(str, 1, 1)')
                          Operand: 
                            IOperation:  (OperationKind.None, Type: null, IsImplicit) (Syntax: 'str')
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                        ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "") (Syntax: '""')

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub NoneOperation_Expression_02()
            Dim source = <![CDATA[
Class C
    Public Sub F(str As String, b As Boolean, str1 As String, str2 As String)'BIND:"Public Sub F(str As String, b As Boolean, str1 As String, str2 As String)"
        Mid(str, 1, 1) = If(b, str1, str2)
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [1] [2] [3] [4]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (4)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'str')
              Value: 
                IParameterReferenceOperation: str (OperationKind.ParameterReference, Type: System.String) (Syntax: 'str')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'Mid(str, 1, 1)')
              Value: 
                IParenthesizedOperation (OperationKind.Parenthesized, Type: System.String) (Syntax: 'Mid(str, 1, 1)')
                  Operand: 
                    IOperation:  (OperationKind.None, Type: null, IsImplicit) (Syntax: 'str')

            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '1')
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

            IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '1')
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Jump if False (Regular) to Block[B3]
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'str1')
              Value: 
                IParameterReferenceOperation: str1 (OperationKind.ParameterReference, Type: System.String) (Syntax: 'str1')

        Next (Regular) Block[B4]
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'str2')
              Value: 
                IParameterReferenceOperation: str2 (OperationKind.ParameterReference, Type: System.String) (Syntax: 'str2')

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Mid(str, 1, ... str1, str2)')
              Expression: 
                IOperation:  (OperationKind.None, Type: null, IsImplicit) (Syntax: 'Mid(str, 1, ... str1, str2)')
                  Children(2):
                      IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'str')
                      IOperation:  (OperationKind.None, Type: null, IsImplicit) (Syntax: 'Mid(str, 1, ... str1, str2)')
                        Children(4):
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'Mid(str, 1, 1)')
                            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '1')
                            IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '1')
                            IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'If(b, str1, str2)')

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
        Public Sub NoneOperation_Expression_03()
            Dim source = <![CDATA[
Class C
    Public Sub F(str As String, b As Boolean, str1 As String, str2 As String)'BIND:"Public Sub F(str As String, b As Boolean, str1 As String, str2 As String)"
        Mid(If(b, str1, str2), 1, 1) = str
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30068: Expression is a value and therefore cannot be the target of an assignment.
        Mid(If(b, str1, str2), 1, 1) = str
            ~~~~~~~~~~~~~~~~~
]]>.Value

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
        Statements (0)
        Jump if False (Regular) to Block[B3]
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean, IsInvalid) (Syntax: 'b')

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'str1')
              Value: 
                IParameterReferenceOperation: str1 (OperationKind.ParameterReference, Type: System.String, IsInvalid) (Syntax: 'str1')

        Next (Regular) Block[B4]
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'str2')
              Value: 
                IParameterReferenceOperation: str2 (OperationKind.ParameterReference, Type: System.String, IsInvalid) (Syntax: 'str2')

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'Mid(If(b, s ... 1, 1) = str')
              Expression: 
                IOperation:  (OperationKind.None, Type: null, IsInvalid, IsImplicit) (Syntax: 'Mid(If(b, s ... 1, 1) = str')
                  Children(2):
                      IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: 'If(b, str1, str2)')
                        Children(1):
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.String, IsInvalid, IsImplicit) (Syntax: 'If(b, str1, str2)')
                      IOperation:  (OperationKind.None, Type: null, IsInvalid, IsImplicit) (Syntax: 'Mid(If(b, s ... 1, 1) = str')
                        Children(4):
                            IParenthesizedOperation (OperationKind.Parenthesized, Type: System.String, IsInvalid) (Syntax: 'Mid(If(b, s ... tr2), 1, 1)')
                              Operand: 
                                IInvalidOperation (OperationKind.Invalid, Type: System.String, IsInvalid, IsImplicit) (Syntax: 'If(b, str1, str2)')
                                  Children(0)
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                            IParameterReferenceOperation: str (OperationKind.ParameterReference, Type: System.String) (Syntax: 'str')

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
