' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub SwitchLocals_01()
            Dim source = <![CDATA[
Class Program
    Public Shared Sub M(input As Integer)
        Select Case input'BIND:"Select Case input"
            Case 1
        End Select
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
ISwitchOperation (1 cases, Exit Label Id: 0) (OperationKind.Switch, Type: null) (Syntax: 'Select Case ... End Select')
  Switch expression: 
    IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'input')
  Sections:
      ISwitchCaseOperation (1 case clauses, 1 statements) (OperationKind.SwitchCase, Type: null) (Syntax: 'Case 1')
          Clauses:
              ISingleValueCaseClauseOperation (CaseKind.SingleValue) (OperationKind.CaseClause, Type: null) (Syntax: '1')
                Value: 
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
          Body:
              IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Case 1')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of SelectBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub SwitchLocals_02()
            Dim source = <![CDATA[
Class Program
    Public Shared Sub M(input As Integer)
        Select Case input'BIND:"Select Case input"
            Case 1
                Dim x  = input
        End Select
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
ISwitchOperation (1 cases, Exit Label Id: 0) (OperationKind.Switch, Type: null) (Syntax: 'Select Case ... End Select')
  Switch expression: 
    IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'input')
  Sections:
      ISwitchCaseOperation (1 case clauses, 1 statements) (OperationKind.SwitchCase, Type: null) (Syntax: 'Case 1 ...  x  = input')
          Clauses:
              ISingleValueCaseClauseOperation (CaseKind.SingleValue) (OperationKind.CaseClause, Type: null) (Syntax: '1')
                Value: 
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
          Body:
              IBlockOperation (1 statements, 1 locals) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Case 1 ...  x  = input')
                Locals: Local_1: x As System.Int32
                IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim x  = input')
                  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'x  = input')
                    Declarators:
                        IVariableDeclaratorOperation (Symbol: x As System.Int32) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'x')
                          Initializer: 
                            null
                    Initializer: 
                      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= input')
                        IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'input')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of SelectBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_001()
            Dim source = <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result as Boolean, input as Integer) 'BIND:"Sub M"
        Select Case input
            Case Else
                result = false
        End Select
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
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'input')

            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = false')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

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
        Public Sub SwitchFlow_002()
            Dim source = <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result as Boolean, input as Integer) 'BIND:"Sub M"
        Select Case input
            Case Else
                result = true
            Case 1
                result = false
        End Select
    End Sub
End Class
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30321: 'Case' cannot follow a 'Case Else' in the same 'Select' statement.
            Case 1
            ~~~~~~
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
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'input')

        Jump if False (Regular) to Block[B2]
            IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: '1')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')

        Next (Regular) Block[B3]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = true')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = true')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

        Next (Regular) Block[B4]
            Leaving: {R1}
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = false')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B4]
            Leaving: {R1}
}

Block[B4] - Exit
    Predecessors: [B2] [B3]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_003()
            Dim source = <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result as Boolean, input as Integer) 'BIND:"Sub M"
        Select Case input
            Case 1
                result = false
            Case Else
                result = true
        End Select
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'input')

        Jump if False (Regular) to Block[B3]
            IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '1')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = false')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B4]
            Leaving: {R1}
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = true')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = true')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

        Next (Regular) Block[B4]
            Leaving: {R1}
}

Block[B4] - Exit
    Predecessors: [B2] [B3]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_004()
            Dim source = <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result as Boolean, input as Integer) 'BIND:"Sub M"
        Select Case input
            Case 1
                result = false
        End Select
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'input')

        Jump if False (Regular) to Block[B3]
            IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '1')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = false')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_005()
            Dim source = <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result as Boolean, input as Integer) 'BIND:"Sub M"
        Select Case input
            Case 0
                Dim x As Boolean = true
                result = x
            Case 1
                result = false
        End Select
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'input')

        Jump if False (Regular) to Block[B3]
            IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '0')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        Locals: [x As System.Boolean]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (2)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'x As Boolean = true')
                  Left: 
                    ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Boolean, IsImplicit) (Syntax: 'x')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = x')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = x')
                      Left: 
                        IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                      Right: 
                        ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Boolean) (Syntax: 'x')

            Next (Regular) Block[B5]
                Leaving: {R2} {R1}
    }

    Block[B3] - Block
        Predecessors: [B1]
        Statements (0)
        Jump if False (Regular) to Block[B5]
            IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '1')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
            Leaving: {R1}

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = false')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B5]
            Leaving: {R1}
}

Block[B5] - Exit
    Predecessors: [B2] [B3] [B4]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_006()
            Dim source = <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result as Boolean, input as Integer) 'BIND:"Sub M"
        Select Case input
            Case Else
            Case 1
                result = false
        End Select
    End Sub
End Class
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30321: 'Case' cannot follow a 'Case Else' in the same 'Select' statement.
            Case 1
            ~~~~~~
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
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'input')

        Jump if False (Regular) to Block[B3]
            IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: '1')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = false')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_007()
            Dim source = <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result as Boolean, input as Integer) 'BIND:"Sub M"
        Select Case input
            Case 1
            Case Else
                result = false
        End Select
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'input')

        Jump if False (Regular) to Block[B2]
            IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '1')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Next (Regular) Block[B3]
            Leaving: {R1}
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = false')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_008()
            Dim source = <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result as Boolean, input as Integer) 'BIND:"Sub M"
        Select Case input
            Case 2, 1
                result = false
        End Select
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'input')

        Jump if False (Regular) to Block[B2]
            IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '2')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

        Next (Regular) Block[B3]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (0)
        Jump if False (Regular) to Block[B4]
            IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '1')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
            Leaving: {R1}

        Next (Regular) Block[B3]
    Block[B3] - Block
        Predecessors: [B1] [B2]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = false')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B4]
            Leaving: {R1}
}

Block[B4] - Exit
    Predecessors: [B2] [B3]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_009()
            Dim source = <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result as Boolean, input as Integer) 'BIND:"Sub M"
        Select Case input
            Case 1
                Goto Case3
            Case 2
                Goto CaseElse
            Case 3
Case3:
                result = true
            Case Else
CaseElse:
                result = false
        End Select
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'input')

        Jump if False (Regular) to Block[B2]
            IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '1')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Next (Regular) Block[B4]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (0)
        Jump if False (Regular) to Block[B3]
            IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '2')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

        Next (Regular) Block[B5]
    Block[B3] - Block
        Predecessors: [B2]
        Statements (0)
        Jump if False (Regular) to Block[B5]
            IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '3')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B1] [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = true')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = true')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

        Next (Regular) Block[B6]
            Leaving: {R1}
    Block[B5] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = false')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Exit
    Predecessors: [B4] [B5]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_010()
            Dim source = <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result as Boolean, input as Integer) 'BIND:"Sub M"
        Select Case input
            Case 1
                if result
                    Exit Select
                End If

                result = true
        End Select
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'input')

        Jump if False (Regular) to Block[B4]
            IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '1')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (0)
        Jump if False (Regular) to Block[B3]
            IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')

        Next (Regular) Block[B4]
            Leaving: {R1}
    Block[B3] - Block
        Predecessors: [B2]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = true')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = true')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

        Next (Regular) Block[B4]
            Leaving: {R1}
}

Block[B4] - Exit
    Predecessors: [B1] [B2] [B3]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_011()
            Dim source = <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result as Boolean, input as Integer) 'BIND:"Sub M"
        Select Case input
            Case Else
                result = result
            Case 1
                result = false
            Case Else
                result = true
        End Select
    End Sub
End Class
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30321: 'Case' cannot follow a 'Case Else' in the same 'Select' statement.
            Case 1
            ~~~~~~
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
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'input')

        Jump if False (Regular) to Block[B2]
            IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: '1')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')

        Next (Regular) Block[B3]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = result')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = result')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')

        Next (Regular) Block[B5]
            Leaving: {R1}
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = false')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B5]
            Leaving: {R1}
    Block[B4] - Block [UnReachable]
        Predecessors (0)
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = true')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = true')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

        Next (Regular) Block[B5]
            Leaving: {R1}
}

Block[B5] - Exit
    Predecessors: [B2] [B3] [B4]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_012()
            Dim source = <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result as Boolean, input as Integer) 'BIND:"Sub M"
        Select Case input
            Case 1L
                result = false
        End Select
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'input')

        Jump if False (Regular) to Block[B3]
            IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '1L')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input')
              Right: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '1L')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (WideningNumeric, InvolvesNarrowingFromNumericConstant)
                  Operand: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int64, Constant: 1) (Syntax: '1L')
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = false')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_013()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result As Boolean, input As C, other As C) 'BIND:"Sub M"
        Select Case input
            Case other
                result = False
        End Select
    End Sub

    Public Shared Operator =(x As C, y As C) As Boolean?
        Return False
    End Operator

    Public Shared Operator <>(x As C, y As C) As Boolean?
        Return True
    End Operator
End Class
        ]]>
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source)

            compilation.AssertNoDiagnostics()

            Dim tree = compilation.SyntaxTrees.Single()
            Dim node = tree.GetRoot().DescendantNodes().OfType(Of SimpleCaseClauseSyntax)().Single()

            compilation.VerifyOperationTree(node, expectedOperationTree:=
            <![CDATA[
ISingleValueCaseClauseOperation (CaseKind.SingleValue) (OperationKind.CaseClause, Type: null) (Syntax: 'other')
  Value: 
    IParameterReferenceOperation: other (OperationKind.ParameterReference, Type: C) (Syntax: 'other')
]]>.Value)

            Dim expectedGraph =
            <![CDATA[
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: C) (Syntax: 'input')

        Jump if False (Regular) to Block[B3]
            IInvocationOperation ( Function System.Nullable(Of System.Boolean).GetValueOrDefault() As System.Boolean) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'other')
              Instance Receiver: 
                IBinaryOperation (BinaryOperatorKind.Equals, Checked) (OperatorMethod: Function C.op_Equality(x As C, y As C) As System.Nullable(Of System.Boolean)) (OperationKind.Binary, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: 'other')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'input')
                  Right: 
                    IParameterReferenceOperation: other (OperationKind.ParameterReference, Type: C) (Syntax: 'other')
              Arguments(0)
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = False')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = False')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'False')

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphForTest(Of MethodBlockSyntax)(compilation, expectedGraph)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_014()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result As Boolean, input As C, other as Integer) 'BIND:"Sub M"
        Select Case input
            Case other
                result = False
        End Select
    End Sub

    Public Shared Operator =(x As C, y As Short) As Object
        Return False
    End Operator

    Public Shared Operator <>(x As C, y As Short) As Object
        Return True
    End Operator
End Class
         ]]>
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source)

            compilation.AssertNoDiagnostics()

            Dim tree = compilation.SyntaxTrees.Single()
            Dim node = tree.GetRoot().DescendantNodes().OfType(Of SimpleCaseClauseSyntax)().Single()

            compilation.VerifyOperationTree(node, expectedOperationTree:=
            <![CDATA[
ISingleValueCaseClauseOperation (CaseKind.SingleValue) (OperationKind.CaseClause, Type: null) (Syntax: 'other')
  Value: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int16, IsImplicit) (Syntax: 'other')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IParameterReferenceOperation: other (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'other')
]]>.Value)

            Dim expectedGraph =
            <![CDATA[
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: C) (Syntax: 'input')

        Jump if False (Regular) to Block[B3]
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Boolean, IsImplicit) (Syntax: 'other')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                (NarrowingValue)
              Operand: 
                IBinaryOperation (BinaryOperatorKind.Equals, Checked) (OperatorMethod: Function C.op_Equality(x As C, y As System.Int16) As System.Object) (OperationKind.Binary, Type: System.Object, IsImplicit) (Syntax: 'other')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'input')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int16, IsImplicit) (Syntax: 'other')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (NarrowingNumeric)
                      Operand: 
                        IParameterReferenceOperation: other (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'other')
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = False')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = False')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'False')

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphForTest(Of MethodBlockSyntax)(compilation, expectedGraph)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_015()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result As Boolean, input As C, other as Integer) 'BIND:"Sub M"
        Select Case other
            Case input
                result = False
        End Select
    End Sub

    Public Shared Operator =(y As Short, x As C) As Boolean
        Return False
    End Operator

    Public Shared Operator <>(y As Short, x As C) As Boolean
        Return True
    End Operator
End Class
         ]]>
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source)

            compilation.AssertNoDiagnostics()

            Dim tree = compilation.SyntaxTrees.Single()
            Dim node = tree.GetRoot().DescendantNodes().OfType(Of SimpleCaseClauseSyntax)().Single()

            compilation.VerifyOperationTree(node, expectedOperationTree:=
            <![CDATA[
ISingleValueCaseClauseOperation (CaseKind.SingleValue) (OperationKind.CaseClause, Type: null) (Syntax: 'input')
  Value: 
    IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: C) (Syntax: 'input')
]]>.Value)

            Dim expectedGraph =
            <![CDATA[
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'other')
              Value: 
                IParameterReferenceOperation: other (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'other')

        Jump if False (Regular) to Block[B3]
            IBinaryOperation (BinaryOperatorKind.Equals, Checked) (OperatorMethod: Function C.op_Equality(y As System.Int16, x As C) As System.Boolean) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: 'input')
              Left: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int16, IsImplicit) (Syntax: 'other')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (NarrowingNumeric)
                  Operand: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'other')
              Right: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: C) (Syntax: 'input')
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = False')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = False')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'False')

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphForTest(Of MethodBlockSyntax)(compilation, expectedGraph)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_016()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result As Boolean, input As C) 'BIND:"Sub M"
        Select Case input
            Case 1
                result = False
        End Select
    End Sub


    Public Shared Widening Operator CType(x As Integer) As C
        Return Nothing
    End Operator
End Class
         ]]>
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source)

            compilation.AssertTheseDiagnostics(
<expected>
BC30452: Operator '=' is not defined for types 'C' and 'Integer'.
            Case 1
                 ~
</expected>)

            Dim tree = compilation.SyntaxTrees.Single()
            Dim node = tree.GetRoot().DescendantNodes().OfType(Of SimpleCaseClauseSyntax)().Single()

            compilation.VerifyOperationTree(node, expectedOperationTree:=
            <![CDATA[
ISingleValueCaseClauseOperation (CaseKind.SingleValue) (OperationKind.CaseClause, Type: null, IsInvalid) (Syntax: '1')
  Value: 
    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
]]>.Value)

            Dim expectedGraph =
            <![CDATA[
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: C) (Syntax: 'input')

        Jump if False (Regular) to Block[B3]
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: '1')
              Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                (DelegateRelaxationLevelNone)
              Operand: 
                IBinaryOperation (BinaryOperatorKind.Equals, Checked) (OperationKind.Binary, Type: ?, IsInvalid, IsImplicit) (Syntax: '1')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'input')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = False')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = False')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'False')

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphForTest(Of MethodBlockSyntax)(compilation, expectedGraph)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_017()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result As Boolean, input As C, other As C) 'BIND:"Sub M"
        Select Case input
            Case other
                result = False
        End Select
    End Sub

    Public Shared Operator =(x As C, y As C) As C
        Return Nothing
    End Operator

    Public Shared Operator <>(x As C, y As C) As C
        Return Nothing
    End Operator

    Public Shared Operator IsTrue(x As C) As Boolean
        Return True
    End Operator

    Public Shared Operator IsFalse(x As C) As Boolean
        Return False
    End Operator
End Class
        ]]>
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source)

            compilation.AssertNoDiagnostics()

            Dim tree = compilation.SyntaxTrees.Single()
            Dim node = tree.GetRoot().DescendantNodes().OfType(Of SimpleCaseClauseSyntax)().Single()

            compilation.VerifyOperationTree(node, expectedOperationTree:=
            <![CDATA[
ISingleValueCaseClauseOperation (CaseKind.SingleValue) (OperationKind.CaseClause, Type: null) (Syntax: 'other')
  Value: 
    IParameterReferenceOperation: other (OperationKind.ParameterReference, Type: C) (Syntax: 'other')
]]>.Value)

            Dim expectedGraph =
            <![CDATA[
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: C) (Syntax: 'input')

        Jump if False (Regular) to Block[B3]
            IUnaryOperation (UnaryOperatorKind.True) (OperatorMethod: Function C.op_True(x As C) As System.Boolean) (OperationKind.Unary, Type: System.Boolean, IsImplicit) (Syntax: 'other')
              Operand: 
                IBinaryOperation (BinaryOperatorKind.Equals, Checked) (OperatorMethod: Function C.op_Equality(x As C, y As C) As C) (OperationKind.Binary, Type: C, IsImplicit) (Syntax: 'other')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'input')
                  Right: 
                    IParameterReferenceOperation: other (OperationKind.ParameterReference, Type: C) (Syntax: 'other')
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = False')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = False')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'False')

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphForTest(Of MethodBlockSyntax)(compilation, expectedGraph)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_018()
            Dim source = <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result as Boolean, input as Integer?, other As Long? ) 'BIND:"Sub M"
        Select Case input
            Case other
                result = false
        End Select
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'input')

        Jump if False (Regular) to Block[B3]
            IInvocationOperation ( Function System.Nullable(Of System.Boolean).GetValueOrDefault() As System.Boolean) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'other')
              Instance Receiver: 
                IBinaryOperation (BinaryOperatorKind.Equals, IsLifted, Checked) (OperationKind.Binary, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: 'other')
                  Left: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Nullable(Of System.Int64), IsImplicit) (Syntax: 'input')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (WideningNullable)
                      Operand: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'input')
                  Right: 
                    IParameterReferenceOperation: other (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int64)) (Syntax: 'other')
              Arguments(0)
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = false')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_019()
            Dim source = <![CDATA[
Option Compare Text
Imports System
Public NotInheritable Class C
    Sub M(result as Boolean, input as String, other As String) 'BIND:"Sub M"
        Select Case input
            Case other
                result = false
        End Select
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.String) (Syntax: 'input')

        Jump if False (Regular) to Block[B3]
            IBinaryOperation (BinaryOperatorKind.Equals, Checked, CompareText) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: 'other')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'input')
              Right: 
                IParameterReferenceOperation: other (OperationKind.ParameterReference, Type: System.String) (Syntax: 'other')
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = false')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_020()
            Dim source = <![CDATA[
Option Compare Binary
Imports System
Public NotInheritable Class C
    Sub M(result as Boolean, input as String, other As String) 'BIND:"Sub M"
        Select Case input
            Case other
                result = false
        End Select
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.String) (Syntax: 'input')

        Jump if False (Regular) to Block[B3]
            IBinaryOperation (BinaryOperatorKind.Equals, Checked) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: 'other')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'input')
              Right: 
                IParameterReferenceOperation: other (OperationKind.ParameterReference, Type: System.String) (Syntax: 'other')
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = false')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_021()
            Dim source = <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result As Boolean, other As C, input as Integer) 'BIND:"Sub M"
        Select Case input
            Case 1, other
                result = False
        End Select
    End Sub

    Public Shared Operator =(y As Integer, x As C) As Boolean
        Return False
    End Operator

    Public Shared Operator <>(y As integer, x As C) As Boolean
        Return True
    End Operator
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'input')

        Jump if True (Regular) to Block[B3]
            IBinaryOperation (BinaryOperatorKind.Equals, Checked) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '1')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (0)
        Jump if False (Regular) to Block[B4]
            IBinaryOperation (BinaryOperatorKind.Equals, Checked) (OperatorMethod: Function C.op_Equality(y As System.Int32, x As C) As System.Boolean) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: 'other')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input')
              Right: 
                IParameterReferenceOperation: other (OperationKind.ParameterReference, Type: C) (Syntax: 'other')
            Leaving: {R1}

        Next (Regular) Block[B3]
    Block[B3] - Block
        Predecessors: [B1] [B2]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = False')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = False')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'False')

        Next (Regular) Block[B4]
            Leaving: {R1}
}

Block[B4] - Exit
    Predecessors: [B2] [B3]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_022()
            Dim source = <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result As Boolean, other As C, input as Object) 'BIND:"Sub M"
        Select Case input
            Case other
                result = False
        End Select
    End Sub

    Public Shared Operator =(y As Integer, x As C) As Boolean
        Return False
    End Operator

    Public Shared Operator <>(y As integer, x As C) As Boolean
        Return True
    End Operator
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'input')

        Jump if False (Regular) to Block[B3]
            IBinaryOperation (BinaryOperatorKind.Equals, Checked) (OperatorMethod: Function C.op_Equality(y As System.Int32, x As C) As System.Boolean) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: 'other')
              Left: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsImplicit) (Syntax: 'input')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (NarrowingValue)
                  Operand: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'input')
              Right: 
                IParameterReferenceOperation: other (OperationKind.ParameterReference, Type: C) (Syntax: 'other')
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = False')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = False')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'False')

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_023()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(result As Boolean, other As C, input as Object) 'BIND:"Sub M"
        Select Case input
            Case other
                result = False
        End Select
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'input')

        Jump if False (Regular) to Block[B3]
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Boolean, IsImplicit) (Syntax: 'other')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                (NarrowingValue)
              Operand: 
                IBinaryOperation (BinaryOperatorKind.ObjectValueEquals, Checked) (OperationKind.Binary, Type: System.Object, IsImplicit) (Syntax: 'other')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'input')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'other')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (WideningReference)
                      Operand: 
                        IParameterReferenceOperation: other (OperationKind.ParameterReference, Type: C) (Syntax: 'other')
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = False')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = False')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'False')

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_024()
            Dim source = <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result As Boolean, other As Integer, input as Object) 'BIND:"Sub M"
        Select Case input
            Case other
                result = False
        End Select
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'input')

        Jump if False (Regular) to Block[B3]
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Boolean, IsImplicit) (Syntax: 'other')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                (NarrowingValue)
              Operand: 
                IBinaryOperation (BinaryOperatorKind.ObjectValueEquals, Checked) (OperationKind.Binary, Type: System.Object, IsImplicit) (Syntax: 'other')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'input')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'other')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (WideningValue)
                      Operand: 
                        IParameterReferenceOperation: other (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'other')
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = False')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = False')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'False')

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_025()
            Dim source = <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result As Boolean, input As Integer, other as Object) 'BIND:"Sub M"
        Select Case input
            Case other
                result = False
        End Select
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'input')

        Jump if False (Regular) to Block[B3]
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Boolean, IsImplicit) (Syntax: 'other')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                (NarrowingValue)
              Operand: 
                IBinaryOperation (BinaryOperatorKind.ObjectValueEquals, Checked) (OperationKind.Binary, Type: System.Object, IsImplicit) (Syntax: 'other')
                  Left: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'input')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (WideningValue)
                      Operand: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input')
                  Right: 
                    IParameterReferenceOperation: other (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'other')
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = False')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = False')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'False')

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_026()
            Dim source = <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result As Boolean, input As Object, other as Object) 'BIND:"Sub M"
        Select Case input
            Case other
                result = False
        End Select
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'input')

        Jump if False (Regular) to Block[B3]
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Boolean, IsImplicit) (Syntax: 'other')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                (NarrowingValue)
              Operand: 
                IBinaryOperation (BinaryOperatorKind.ObjectValueEquals, Checked) (OperationKind.Binary, Type: System.Object, IsImplicit) (Syntax: 'other')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'input')
                  Right: 
                    IParameterReferenceOperation: other (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'other')
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = False')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = False')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'False')

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_027()
            Dim source = <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result As Boolean, input As C, other1 As C, other2 As C) 'BIND:"Sub M"
        Select Case input
            Case other1, other2
                result = False
        End Select
    End Sub

    Public Shared Operator =(x As C, y As C) As C
        Return Nothing
    End Operator

    Public Shared Operator <>(x As C, y As C) As C
        Return Nothing
    End Operator

    Public Shared Operator IsTrue(x As C) As Boolean
        Return True
    End Operator

    Public Shared Operator IsFalse(x As C) As Boolean
        Return False
    End Operator

    Public Shared Operator Or(x As C, y As C) As C
        Return Nothing
    End Operator
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: C) (Syntax: 'input')

        Next (Regular) Block[B2]
            Entering: {R2} {R3}

    .locals {R2}
    {
        CaptureIds: [2]
        .locals {R3}
        {
            CaptureIds: [1]
            Block[B2] - Block
                Predecessors: [B1]
                Statements (1)
                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'other1')
                      Value: 
                        IBinaryOperation (BinaryOperatorKind.Equals, Checked) (OperatorMethod: Function C.op_Equality(x As C, y As C) As C) (OperationKind.Binary, Type: C, IsImplicit) (Syntax: 'other1')
                          Left: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'input')
                          Right: 
                            IParameterReferenceOperation: other1 (OperationKind.ParameterReference, Type: C) (Syntax: 'other1')

                Jump if False (Regular) to Block[B4]
                    IUnaryOperation (UnaryOperatorKind.True) (OperatorMethod: Function C.op_True(x As C) As System.Boolean) (OperationKind.Unary, Type: System.Boolean, IsImplicit) (Syntax: 'other1')
                      Operand: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'other1')

                Next (Regular) Block[B3]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'Case other1, other2')
                      Value: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'other1')

                Next (Regular) Block[B5]
                    Leaving: {R3}
            Block[B4] - Block
                Predecessors: [B2]
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'Case other1, other2')
                      Value: 
                        IBinaryOperation (BinaryOperatorKind.Or) (OperatorMethod: Function C.op_BitwiseOr(x As C, y As C) As C) (OperationKind.Binary, Type: C, IsImplicit) (Syntax: 'Case other1, other2')
                          Left: 
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'other1')
                          Right: 
                            IBinaryOperation (BinaryOperatorKind.Equals, Checked) (OperatorMethod: Function C.op_Equality(x As C, y As C) As C) (OperationKind.Binary, Type: C, IsImplicit) (Syntax: 'other2')
                              Left: 
                                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'input')
                              Right: 
                                IParameterReferenceOperation: other2 (OperationKind.ParameterReference, Type: C) (Syntax: 'other2')

                Next (Regular) Block[B5]
                    Leaving: {R3}
        }

        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (0)
            Jump if False (Regular) to Block[B7]
                IUnaryOperation (UnaryOperatorKind.True) (OperatorMethod: Function C.op_True(x As C) As System.Boolean) (OperationKind.Unary, Type: System.Boolean, IsImplicit) (Syntax: 'Case other1, other2')
                  Operand: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'Case other1, other2')
                Leaving: {R2} {R1}

            Next (Regular) Block[B6]
                Leaving: {R2}
    }

    Block[B6] - Block
        Predecessors: [B5]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = False')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = False')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'False')

        Next (Regular) Block[B7]
            Leaving: {R1}
}

Block[B7] - Exit
    Predecessors: [B5] [B6]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_028()
            Dim source = <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result as Boolean, input as Integer?) 'BIND:"Sub M"
        Select Case input
            Case 1
                result = false
        End Select
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'input')

        Jump if False (Regular) to Block[B3]
            IInvocationOperation ( Function System.Nullable(Of System.Boolean).GetValueOrDefault() As System.Boolean) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: '1')
              Instance Receiver: 
                IBinaryOperation (BinaryOperatorKind.Equals, IsLifted, Checked) (OperationKind.Binary, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: '1')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'input')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: '1')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (WideningNullable)
                      Operand: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
              Arguments(0)
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = false')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_029()
            Dim source = <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result as Boolean, input as Integer?) 'BIND:"Sub M"
        Select Case input
            Case Nothing
                result = false
        End Select
    End Sub
End Class
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC42037: This expression will always evaluate to Nothing (due to null propagation from the equals operator). To check if the value is null consider using 'Is Nothing'.
            Case Nothing
                 ~~~~~~~
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
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'input')

        Jump if False (Regular) to Block[B3]
            IInvocationOperation ( Function System.Nullable(Of System.Boolean).GetValueOrDefault() As System.Boolean) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'Nothing')
              Instance Receiver: 
                IBinaryOperation (BinaryOperatorKind.Equals, IsLifted, Checked) (OperationKind.Binary, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: 'Nothing')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'input')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'Nothing')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (WideningNothingLiteral)
                      Operand: 
                        ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'Nothing')
              Arguments(0)
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = false')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_030()
            Dim source = <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result as Boolean, input as Integer?) 'BIND:"Sub M"
        Select Case input
            Case 1, Nothing
                result = false
        End Select
    End Sub
End Class
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC42037: This expression will always evaluate to Nothing (due to null propagation from the equals operator). To check if the value is null consider using 'Is Nothing'.
            Case 1, Nothing
                    ~~~~~~~
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
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'input')

        Next (Regular) Block[B2]
            Entering: {R2} {R3}

    .locals {R2}
    {
        CaptureIds: [2]
        .locals {R3}
        {
            CaptureIds: [1]
            Block[B2] - Block
                Predecessors: [B1]
                Statements (1)
                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '1')
                      Value: 
                        IBinaryOperation (BinaryOperatorKind.Equals, IsLifted, Checked) (OperationKind.Binary, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: '1')
                          Left: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'input')
                          Right: 
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: '1')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                (WideningNullable)
                              Operand: 
                                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

                Jump if True (Regular) to Block[B5]
                    IInvocationOperation ( Function System.Nullable(Of System.Boolean).GetValueOrDefault() As System.Boolean) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: '1')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: '1')
                      Arguments(0)

                Next (Regular) Block[B3]
                    Entering: {R4}

            .locals {R4}
            {
                CaptureIds: [3]
                Block[B3] - Block
                    Predecessors: [B2]
                    Statements (1)
                        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'Nothing')
                          Value: 
                            IBinaryOperation (BinaryOperatorKind.Equals, IsLifted, Checked) (OperationKind.Binary, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: 'Nothing')
                              Left: 
                                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'input')
                              Right: 
                                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'Nothing')
                                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                    (WideningNothingLiteral)
                                  Operand: 
                                    ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'Nothing')

                    Jump if True (Regular) to Block[B5]
                        IInvocationOperation ( Function System.Nullable(Of System.Boolean).GetValueOrDefault() As System.Boolean) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'Nothing')
                          Instance Receiver: 
                            IUnaryOperation (UnaryOperatorKind.Not, IsLifted) (OperationKind.Unary, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: 'Nothing')
                              Operand: 
                                IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: 'Nothing')
                          Arguments(0)
                        Leaving: {R4}

                    Next (Regular) Block[B4]
                Block[B4] - Block
                    Predecessors: [B3]
                    Statements (1)
                        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'Case 1, Nothing')
                          Value: 
                            IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: 'Nothing')

                    Next (Regular) Block[B6]
                        Leaving: {R4} {R3}
            }

            Block[B5] - Block
                Predecessors: [B2] [B3]
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'Case 1, Nothing')
                      Value: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: '1')

                Next (Regular) Block[B6]
                    Leaving: {R3}
        }

        Block[B6] - Block
            Predecessors: [B4] [B5]
            Statements (0)
            Jump if False (Regular) to Block[B8]
                IInvocationOperation ( Function System.Nullable(Of System.Boolean).GetValueOrDefault() As System.Boolean) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'Case 1, Nothing')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: 'Case 1, Nothing')
                  Arguments(0)
                Leaving: {R2} {R1}

            Next (Regular) Block[B7]
                Leaving: {R2}
    }

    Block[B7] - Block
        Predecessors: [B6]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = false')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B8]
            Leaving: {R1}
}

Block[B8] - Exit
    Predecessors: [B6] [B7]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_031()
            Dim source = <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result as Boolean, other as Object) 'BIND:"Sub M"
        Select Case Function() 1
            Case other
                result = false
        End Select
    End Sub
End Class
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36635: Lambda expressions are not valid in the first expression of a 'Select Case' statement.
        Select Case Function() 1
                    ~~~~~~~~~~~~
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
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'Function() 1')
              Value: 
                IDelegateCreationOperation (OperationKind.DelegateCreation, Type: Function <generated method>() As System.Int32, IsInvalid, IsImplicit) (Syntax: 'Function() 1')
                  Target: 
                    IFlowAnonymousFunctionOperation (Symbol: Function () As System.Int32) (OperationKind.FlowAnonymousFunction, Type: null, IsInvalid) (Syntax: 'Function() 1')
                    {
                        Block[B0#A0] - Entry
                            Statements (0)
                            Next (Regular) Block[B1#A0]
                                Entering: {R1#A0}

                        .locals {R1#A0}
                        {
                            Locals: [<anonymous local> As System.Int32]
                            Block[B1#A0] - Block
                                Predecessors: [B0#A0]
                                Statements (0)
                                Next (Return) Block[B2#A0]
                                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
                                    Leaving: {R1#A0}
                        }

                        Block[B2#A0] - Exit
                            Predecessors: [B1#A0]
                            Statements (0)
                    }

        Jump if False (Regular) to Block[B3]
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Boolean, IsImplicit) (Syntax: 'other')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                (NarrowingValue)
              Operand: 
                IBinaryOperation (BinaryOperatorKind.ObjectValueEquals, Checked) (OperationKind.Binary, Type: System.Object, IsImplicit) (Syntax: 'other')
                  Left: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsInvalid, IsImplicit) (Syntax: 'Function() 1')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (WideningReference, DelegateRelaxationLevelWideningToNonLambda)
                      Operand: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: Function <generated method>() As System.Int32, IsInvalid, IsImplicit) (Syntax: 'Function() 1')
                  Right: 
                    IParameterReferenceOperation: other (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'other')
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = false')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_032()
            Dim source = <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result as Boolean, other1 as Object, other2 as Object, other3 as Object, other4 as Object) 'BIND:"Sub M"
            Case other1
            Case < other2
            Case other3 To other4
    End Sub
End Class
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30072: 'Case' can only appear inside a 'Select Case' statement.
            Case other1
            ~~~~~~~~~~~
BC30072: 'Case' can only appear inside a 'Select Case' statement.
            Case < other2
            ~~~~~~~~~~~~~
BC30072: 'Case' can only appear inside a 'Select Case' statement.
            Case other3 To other4
            ~~~~~~~~~~~~~~~~~~~~~
]]>.Value
            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (7)
        IParameterReferenceOperation: other1 (OperationKind.ParameterReference, Type: System.Object, IsInvalid) (Syntax: 'other1')

        IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: 'Case other1')
          Children(0)

        IParameterReferenceOperation: other2 (OperationKind.ParameterReference, Type: System.Object, IsInvalid) (Syntax: 'other2')

        IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: 'Case < other2')
          Children(0)

        IParameterReferenceOperation: other3 (OperationKind.ParameterReference, Type: System.Object, IsInvalid) (Syntax: 'other3')

        IParameterReferenceOperation: other4 (OperationKind.ParameterReference, Type: System.Object, IsInvalid) (Syntax: 'other4')

        IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: 'Case other3 To other4')
          Children(0)

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_033()
            Dim source = <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result as Boolean) 'BIND:"Sub M"
            Case Else
                result = false
    End Sub
End Class
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30071: 'Case Else' can only appear inside a 'Select Case' statement.
            Case Else
            ~~~~~~~~~
]]>.Value
            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (2)
        IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: 'Case Else')
          Children(0)

        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = false')
              Left: 
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_034()
            Dim source = <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result as Boolean, input1 as Integer?, input2 as Integer) 'BIND:"Sub M"
        Select Case If(input1, input2) 
            Case 1
                result = false
        End Select
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
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input1')
                  Value: 
                    IParameterReferenceOperation: input1 (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'input1')

            Jump if True (Regular) to Block[B3]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'input1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'input1')
                Leaving: {R2}

            Next (Regular) Block[B2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input1')
                  Value: 
                    IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'input1')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'input1')
                      Arguments(0)

            Next (Regular) Block[B4]
                Leaving: {R2}
    }

    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input2')
              Value: 
                IParameterReferenceOperation: input2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'input2')

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (0)
        Jump if False (Regular) to Block[B6]
            IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '1')
              Left: 
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(input1, input2)')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
            Leaving: {R1}

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = false')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Exit
    Predecessors: [B4] [B5]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_035()
            Dim source = <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result as Boolean, input1 as Integer?, input2 as Integer, input3 as Integer) 'BIND:"Sub M"
        Select Case input3 
            Case If(input1, input2)
                result = false
        End Select
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input3')
              Value: 
                IParameterReferenceOperation: input3 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'input3')

        Next (Regular) Block[B2]
            Entering: {R2} {R3}

    .locals {R2}
    {
        CaptureIds: [2]
        .locals {R3}
        {
            CaptureIds: [1]
            Block[B2] - Block
                Predecessors: [B1]
                Statements (1)
                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input1')
                      Value: 
                        IParameterReferenceOperation: input1 (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'input1')

                Jump if True (Regular) to Block[B4]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'input1')
                      Operand: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'input1')
                    Leaving: {R3}

                Next (Regular) Block[B3]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input1')
                      Value: 
                        IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'input1')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'input1')
                          Arguments(0)

                Next (Regular) Block[B5]
                    Leaving: {R3}
        }

        Block[B4] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input2')
                  Value: 
                    IParameterReferenceOperation: input2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'input2')

            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (0)
            Jump if False (Regular) to Block[B7]
                IBinaryOperation (BinaryOperatorKind.Equals, Checked) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: 'If(input1, input2)')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input3')
                  Right: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(input1, input2)')
                Leaving: {R2} {R1}

            Next (Regular) Block[B6]
                Leaving: {R2}
    }

    Block[B6] - Block
        Predecessors: [B5]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = false')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B7]
            Leaving: {R1}
}

Block[B7] - Exit
    Predecessors: [B5] [B6]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_036()
            Dim source = <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(input1 as Integer, input2 as C) 'BIND:"Sub M"
        Select Case input1
            Case 1
                input2?.ToString()
        End Select
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input1')
              Value: 
                IParameterReferenceOperation: input1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'input1')

        Jump if False (Regular) to Block[B4]
            IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '1')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input1')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
            Leaving: {R1}

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input2')
                  Value: 
                    IParameterReferenceOperation: input2 (OperationKind.ParameterReference, Type: C) (Syntax: 'input2')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'input2')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'input2')
                Leaving: {R2} {R1}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'input2?.ToString()')
                  Expression: 
                    IInvocationOperation (virtual Function System.Object.ToString() As System.String) (OperationKind.Invocation, Type: System.String) (Syntax: '.ToString()')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'input2')
                      Arguments(0)

            Next (Regular) Block[B4]
                Leaving: {R2} {R1}
    }
}

Block[B4] - Exit
    Predecessors: [B1] [B2] [B3]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_037()
            Dim source = <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result as Boolean, input as Integer) 'BIND:"Sub M"
        Select Case input
            Case = 1
                result = false
        End Select
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'input')

        Jump if False (Regular) to Block[B3]
            IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '1')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = false')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_038()
            Dim source = <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result as Boolean, input as Integer) 'BIND:"Sub M"
        Select Case input
            Case = 0
                Dim x As Boolean = true
                result = x
            Case = 1
                result = false
        End Select
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'input')

        Jump if False (Regular) to Block[B3]
            IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '0')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        Locals: [x As System.Boolean]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (2)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'x As Boolean = true')
                  Left: 
                    ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Boolean, IsImplicit) (Syntax: 'x')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = x')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = x')
                      Left: 
                        IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                      Right: 
                        ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Boolean) (Syntax: 'x')

            Next (Regular) Block[B5]
                Leaving: {R2} {R1}
    }

    Block[B3] - Block
        Predecessors: [B1]
        Statements (0)
        Jump if False (Regular) to Block[B5]
            IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '1')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
            Leaving: {R1}

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = false')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B5]
            Leaving: {R1}
}

Block[B5] - Exit
    Predecessors: [B2] [B3] [B4]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_039()
            Dim source = <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result as Boolean, input as Integer) 'BIND:"Sub M"
        Select Case input
            Case = 2, = 1
                result = false
        End Select
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'input')

        Jump if False (Regular) to Block[B2]
            IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '2')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

        Next (Regular) Block[B3]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (0)
        Jump if False (Regular) to Block[B4]
            IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '1')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
            Leaving: {R1}

        Next (Regular) Block[B3]
    Block[B3] - Block
        Predecessors: [B1] [B2]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = false')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B4]
            Leaving: {R1}
}

Block[B4] - Exit
    Predecessors: [B2] [B3]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_040()
            Dim source = <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result as Boolean, input as Integer) 'BIND:"Sub M"
        Select Case input
            Case = 1L
                result = false
        End Select
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'input')

        Jump if False (Regular) to Block[B3]
            IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '1L')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input')
              Right: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '1L')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (WideningNumeric, InvolvesNarrowingFromNumericConstant)
                  Operand: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int64, Constant: 1) (Syntax: '1L')
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = false')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_041()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result As Boolean, input As C, other As C) 'BIND:"Sub M"
        Select Case input
            Case = other
                result = False
        End Select
    End Sub

    Public Shared Operator =(x As C, y As C) As Boolean?
        Return False
    End Operator

    Public Shared Operator <>(x As C, y As C) As Boolean?
        Return True
    End Operator
End Class
        ]]>
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source)

            compilation.AssertNoDiagnostics()

            Dim tree = compilation.SyntaxTrees.Single()
            Dim node = tree.GetRoot().DescendantNodes().OfType(Of RelationalCaseClauseSyntax)().Single()

            compilation.VerifyOperationTree(node, expectedOperationTree:=
            <![CDATA[
IRelationalCaseClauseOperation (Relational operator kind: BinaryOperatorKind.Equals) (CaseKind.Relational) (OperationKind.CaseClause, Type: null) (Syntax: '= other')
  Value: 
    IParameterReferenceOperation: other (OperationKind.ParameterReference, Type: C) (Syntax: 'other')
]]>.Value)

            Dim expectedGraph =
            <![CDATA[
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: C) (Syntax: 'input')

        Jump if False (Regular) to Block[B3]
            IInvocationOperation ( Function System.Nullable(Of System.Boolean).GetValueOrDefault() As System.Boolean) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: '= other')
              Instance Receiver: 
                IBinaryOperation (BinaryOperatorKind.Equals, Checked) (OperatorMethod: Function C.op_Equality(x As C, y As C) As System.Nullable(Of System.Boolean)) (OperationKind.Binary, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: '= other')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'input')
                  Right: 
                    IParameterReferenceOperation: other (OperationKind.ParameterReference, Type: C) (Syntax: 'other')
              Arguments(0)
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = False')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = False')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'False')

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphForTest(Of MethodBlockSyntax)(compilation, expectedGraph)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_042()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result As Boolean, input As C, other as Integer) 'BIND:"Sub M"
        Select Case input
            Case = other
                result = False
        End Select
    End Sub

    Public Shared Operator =(x As C, y As Short) As Object
        Return False
    End Operator

    Public Shared Operator <>(x As C, y As Short) As Object
        Return True
    End Operator
End Class
         ]]>
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source)

            compilation.AssertNoDiagnostics()

            Dim tree = compilation.SyntaxTrees.Single()
            Dim node = tree.GetRoot().DescendantNodes().OfType(Of RelationalCaseClauseSyntax)().Single()

            compilation.VerifyOperationTree(node, expectedOperationTree:=
            <![CDATA[
IRelationalCaseClauseOperation (Relational operator kind: BinaryOperatorKind.Equals) (CaseKind.Relational) (OperationKind.CaseClause, Type: null) (Syntax: '= other')
  Value: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int16, IsImplicit) (Syntax: 'other')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IParameterReferenceOperation: other (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'other')
]]>.Value)

            Dim expectedGraph =
            <![CDATA[
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: C) (Syntax: 'input')

        Jump if False (Regular) to Block[B3]
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Boolean, IsImplicit) (Syntax: '= other')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                (NarrowingValue)
              Operand: 
                IBinaryOperation (BinaryOperatorKind.Equals, Checked) (OperatorMethod: Function C.op_Equality(x As C, y As System.Int16) As System.Object) (OperationKind.Binary, Type: System.Object, IsImplicit) (Syntax: '= other')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'input')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int16, IsImplicit) (Syntax: 'other')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (NarrowingNumeric)
                      Operand: 
                        IParameterReferenceOperation: other (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'other')
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = False')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = False')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'False')

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphForTest(Of MethodBlockSyntax)(compilation, expectedGraph)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_043()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result As Boolean, input As C, other as Integer) 'BIND:"Sub M"
        Select Case other
            Case = input
                result = False
        End Select
    End Sub

    Public Shared Operator =(y As Short, x As C) As Boolean
        Return False
    End Operator

    Public Shared Operator <>(y As Short, x As C) As Boolean
        Return True
    End Operator
End Class
         ]]>
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source)

            compilation.AssertNoDiagnostics()

            Dim tree = compilation.SyntaxTrees.Single()
            Dim node = tree.GetRoot().DescendantNodes().OfType(Of RelationalCaseClauseSyntax)().Single()

            compilation.VerifyOperationTree(node, expectedOperationTree:=
            <![CDATA[
IRelationalCaseClauseOperation (Relational operator kind: BinaryOperatorKind.Equals) (CaseKind.Relational) (OperationKind.CaseClause, Type: null) (Syntax: '= input')
  Value: 
    IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: C) (Syntax: 'input')
]]>.Value)

            Dim expectedGraph =
            <![CDATA[
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'other')
              Value: 
                IParameterReferenceOperation: other (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'other')

        Jump if False (Regular) to Block[B3]
            IBinaryOperation (BinaryOperatorKind.Equals, Checked) (OperatorMethod: Function C.op_Equality(y As System.Int16, x As C) As System.Boolean) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '= input')
              Left: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int16, IsImplicit) (Syntax: 'other')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (NarrowingNumeric)
                  Operand: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'other')
              Right: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: C) (Syntax: 'input')
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = False')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = False')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'False')

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphForTest(Of MethodBlockSyntax)(compilation, expectedGraph)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_044()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result As Boolean, input As C) 'BIND:"Sub M"
        Select Case input
            Case = 1
                result = False
        End Select
    End Sub


    Public Shared Widening Operator CType(x As Integer) As C
        Return Nothing
    End Operator
End Class
         ]]>
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source)

            compilation.AssertTheseDiagnostics(
<expected>
BC30452: Operator '=' is not defined for types 'C' and 'Integer'.
            Case = 1
                 ~~~
</expected>)

            Dim tree = compilation.SyntaxTrees.Single()
            Dim node = tree.GetRoot().DescendantNodes().OfType(Of RelationalCaseClauseSyntax)().Single()

            compilation.VerifyOperationTree(node, expectedOperationTree:=
            <![CDATA[
IRelationalCaseClauseOperation (Relational operator kind: BinaryOperatorKind.Equals) (CaseKind.Relational) (OperationKind.CaseClause, Type: null, IsInvalid) (Syntax: '= 1')
  Value: 
    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
]]>.Value)

            Dim expectedGraph =
            <![CDATA[
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: C) (Syntax: 'input')

        Jump if False (Regular) to Block[B3]
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: '= 1')
              Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                (DelegateRelaxationLevelNone)
              Operand: 
                IBinaryOperation (BinaryOperatorKind.Equals, Checked) (OperationKind.Binary, Type: ?, IsInvalid, IsImplicit) (Syntax: '= 1')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'input')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = False')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = False')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'False')

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphForTest(Of MethodBlockSyntax)(compilation, expectedGraph)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_045()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result As Boolean, input As C, other As C) 'BIND:"Sub M"
        Select Case input
            Case = other
                result = False
        End Select
    End Sub

    Public Shared Operator =(x As C, y As C) As C
        Return Nothing
    End Operator

    Public Shared Operator <>(x As C, y As C) As C
        Return Nothing
    End Operator

    Public Shared Operator IsTrue(x As C) As Boolean
        Return True
    End Operator

    Public Shared Operator IsFalse(x As C) As Boolean
        Return False
    End Operator
End Class
        ]]>
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source)

            compilation.AssertNoDiagnostics()

            Dim tree = compilation.SyntaxTrees.Single()
            Dim node = tree.GetRoot().DescendantNodes().OfType(Of RelationalCaseClauseSyntax)().Single()

            compilation.VerifyOperationTree(node, expectedOperationTree:=
            <![CDATA[
IRelationalCaseClauseOperation (Relational operator kind: BinaryOperatorKind.Equals) (CaseKind.Relational) (OperationKind.CaseClause, Type: null) (Syntax: '= other')
  Value: 
    IParameterReferenceOperation: other (OperationKind.ParameterReference, Type: C) (Syntax: 'other')
]]>.Value)

            Dim expectedGraph =
            <![CDATA[
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: C) (Syntax: 'input')

        Jump if False (Regular) to Block[B3]
            IUnaryOperation (UnaryOperatorKind.True) (OperatorMethod: Function C.op_True(x As C) As System.Boolean) (OperationKind.Unary, Type: System.Boolean, IsImplicit) (Syntax: '= other')
              Operand: 
                IBinaryOperation (BinaryOperatorKind.Equals, Checked) (OperatorMethod: Function C.op_Equality(x As C, y As C) As C) (OperationKind.Binary, Type: C, IsImplicit) (Syntax: '= other')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'input')
                  Right: 
                    IParameterReferenceOperation: other (OperationKind.ParameterReference, Type: C) (Syntax: 'other')
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = False')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = False')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'False')

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphForTest(Of MethodBlockSyntax)(compilation, expectedGraph)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_046()
            Dim source = <![CDATA[
Option Compare Text
Imports System
Public NotInheritable Class C
    Sub M(result as Boolean, input as String, other As String) 'BIND:"Sub M"
        Select Case input
            Case = other
                result = false
        End Select
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.String) (Syntax: 'input')

        Jump if False (Regular) to Block[B3]
            IBinaryOperation (BinaryOperatorKind.Equals, Checked, CompareText) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '= other')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'input')
              Right: 
                IParameterReferenceOperation: other (OperationKind.ParameterReference, Type: System.String) (Syntax: 'other')
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = false')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_047()
            Dim source = <![CDATA[
Option Compare Binary
Imports System
Public NotInheritable Class C
    Sub M(result as Boolean, input as String, other As String) 'BIND:"Sub M"
        Select Case input
            Case = other
                result = false
        End Select
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.String) (Syntax: 'input')

        Jump if False (Regular) to Block[B3]
            IBinaryOperation (BinaryOperatorKind.Equals, Checked) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '= other')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'input')
              Right: 
                IParameterReferenceOperation: other (OperationKind.ParameterReference, Type: System.String) (Syntax: 'other')
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = false')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_048()
            Dim source = <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result As Boolean, other As C, input as Integer) 'BIND:"Sub M"
        Select Case input
            Case = 1, = other
                result = False
        End Select
    End Sub

    Public Shared Operator =(y As Integer, x As C) As Boolean
        Return False
    End Operator

    Public Shared Operator <>(y As integer, x As C) As Boolean
        Return True
    End Operator
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'input')

        Jump if True (Regular) to Block[B3]
            IBinaryOperation (BinaryOperatorKind.Equals, Checked) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '= 1')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (0)
        Jump if False (Regular) to Block[B4]
            IBinaryOperation (BinaryOperatorKind.Equals, Checked) (OperatorMethod: Function C.op_Equality(y As System.Int32, x As C) As System.Boolean) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '= other')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input')
              Right: 
                IParameterReferenceOperation: other (OperationKind.ParameterReference, Type: C) (Syntax: 'other')
            Leaving: {R1}

        Next (Regular) Block[B3]
    Block[B3] - Block
        Predecessors: [B1] [B2]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = False')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = False')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'False')

        Next (Regular) Block[B4]
            Leaving: {R1}
}

Block[B4] - Exit
    Predecessors: [B2] [B3]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_049()
            Dim source = <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result As Boolean, other As C, input as Object) 'BIND:"Sub M"
        Select Case input
            Case = other
                result = False
        End Select
    End Sub

    Public Shared Operator =(y As Integer, x As C) As Boolean
        Return False
    End Operator

    Public Shared Operator <>(y As integer, x As C) As Boolean
        Return True
    End Operator
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'input')

        Jump if False (Regular) to Block[B3]
            IBinaryOperation (BinaryOperatorKind.Equals, Checked) (OperatorMethod: Function C.op_Equality(y As System.Int32, x As C) As System.Boolean) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '= other')
              Left: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsImplicit) (Syntax: 'input')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (NarrowingValue)
                  Operand: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'input')
              Right: 
                IParameterReferenceOperation: other (OperationKind.ParameterReference, Type: C) (Syntax: 'other')
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = False')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = False')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'False')

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_050()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(result As Boolean, other As C, input as Object) 'BIND:"Sub M"
        Select Case input
            Case = other
                result = False
        End Select
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'input')

        Jump if False (Regular) to Block[B3]
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Boolean, IsImplicit) (Syntax: '= other')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                (NarrowingValue)
              Operand: 
                IBinaryOperation (BinaryOperatorKind.ObjectValueEquals, Checked) (OperationKind.Binary, Type: System.Object, IsImplicit) (Syntax: '= other')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'input')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'other')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (WideningReference)
                      Operand: 
                        IParameterReferenceOperation: other (OperationKind.ParameterReference, Type: C) (Syntax: 'other')
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = False')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = False')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'False')

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_051()
            Dim source = <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result As Boolean, other As Integer, input as Object) 'BIND:"Sub M"
        Select Case input
            Case = other
                result = False
        End Select
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'input')

        Jump if False (Regular) to Block[B3]
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Boolean, IsImplicit) (Syntax: '= other')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                (NarrowingValue)
              Operand: 
                IBinaryOperation (BinaryOperatorKind.ObjectValueEquals, Checked) (OperationKind.Binary, Type: System.Object, IsImplicit) (Syntax: '= other')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'input')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'other')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (WideningValue)
                      Operand: 
                        IParameterReferenceOperation: other (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'other')
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = False')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = False')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'False')

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_052()
            Dim source = <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result As Boolean, input As Integer, other as Object) 'BIND:"Sub M"
        Select Case input
            Case = other
                result = False
        End Select
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'input')

        Jump if False (Regular) to Block[B3]
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Boolean, IsImplicit) (Syntax: '= other')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                (NarrowingValue)
              Operand: 
                IBinaryOperation (BinaryOperatorKind.ObjectValueEquals, Checked) (OperationKind.Binary, Type: System.Object, IsImplicit) (Syntax: '= other')
                  Left: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'input')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (WideningValue)
                      Operand: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input')
                  Right: 
                    IParameterReferenceOperation: other (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'other')
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = False')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = False')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'False')

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_053()
            Dim source = <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result As Boolean, input As Object, other as Object) 'BIND:"Sub M"
        Select Case input
            Case = other
                result = False
        End Select
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'input')

        Jump if False (Regular) to Block[B3]
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Boolean, IsImplicit) (Syntax: '= other')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                (NarrowingValue)
              Operand: 
                IBinaryOperation (BinaryOperatorKind.ObjectValueEquals, Checked) (OperationKind.Binary, Type: System.Object, IsImplicit) (Syntax: '= other')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'input')
                  Right: 
                    IParameterReferenceOperation: other (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'other')
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = False')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = False')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'False')

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_054()
            Dim source = <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result As Boolean, input As C, other1 As C, other2 As C) 'BIND:"Sub M"
        Select Case input
            Case = other1, = other2
                result = False
        End Select
    End Sub

    Public Shared Operator =(x As C, y As C) As C
        Return Nothing
    End Operator

    Public Shared Operator <>(x As C, y As C) As C
        Return Nothing
    End Operator

    Public Shared Operator IsTrue(x As C) As Boolean
        Return True
    End Operator

    Public Shared Operator IsFalse(x As C) As Boolean
        Return False
    End Operator

    Public Shared Operator Or(x As C, y As C) As C
        Return Nothing
    End Operator
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: C) (Syntax: 'input')

        Next (Regular) Block[B2]
            Entering: {R2} {R3}

    .locals {R2}
    {
        CaptureIds: [2]
        .locals {R3}
        {
            CaptureIds: [1]
            Block[B2] - Block
                Predecessors: [B1]
                Statements (1)
                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '= other1')
                      Value: 
                        IBinaryOperation (BinaryOperatorKind.Equals, Checked) (OperatorMethod: Function C.op_Equality(x As C, y As C) As C) (OperationKind.Binary, Type: C, IsImplicit) (Syntax: '= other1')
                          Left: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'input')
                          Right: 
                            IParameterReferenceOperation: other1 (OperationKind.ParameterReference, Type: C) (Syntax: 'other1')

                Jump if False (Regular) to Block[B4]
                    IUnaryOperation (UnaryOperatorKind.True) (OperatorMethod: Function C.op_True(x As C) As System.Boolean) (OperationKind.Unary, Type: System.Boolean, IsImplicit) (Syntax: '= other1')
                      Operand: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: '= other1')

                Next (Regular) Block[B3]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'Case = other1, = other2')
                      Value: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: '= other1')

                Next (Regular) Block[B5]
                    Leaving: {R3}
            Block[B4] - Block
                Predecessors: [B2]
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'Case = other1, = other2')
                      Value: 
                        IBinaryOperation (BinaryOperatorKind.Or) (OperatorMethod: Function C.op_BitwiseOr(x As C, y As C) As C) (OperationKind.Binary, Type: C, IsImplicit) (Syntax: 'Case = other1, = other2')
                          Left: 
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: '= other1')
                          Right: 
                            IBinaryOperation (BinaryOperatorKind.Equals, Checked) (OperatorMethod: Function C.op_Equality(x As C, y As C) As C) (OperationKind.Binary, Type: C, IsImplicit) (Syntax: '= other2')
                              Left: 
                                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'input')
                              Right: 
                                IParameterReferenceOperation: other2 (OperationKind.ParameterReference, Type: C) (Syntax: 'other2')

                Next (Regular) Block[B5]
                    Leaving: {R3}
        }

        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (0)
            Jump if False (Regular) to Block[B7]
                IUnaryOperation (UnaryOperatorKind.True) (OperatorMethod: Function C.op_True(x As C) As System.Boolean) (OperationKind.Unary, Type: System.Boolean, IsImplicit) (Syntax: 'Case = other1, = other2')
                  Operand: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'Case = other1, = other2')
                Leaving: {R2} {R1}

            Next (Regular) Block[B6]
                Leaving: {R2}
    }

    Block[B6] - Block
        Predecessors: [B5]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = False')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = False')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'False')

        Next (Regular) Block[B7]
            Leaving: {R1}
}

Block[B7] - Exit
    Predecessors: [B5] [B6]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_055()
            Dim source = <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result as Boolean, input as Integer?) 'BIND:"Sub M"
        Select Case input
            Case = 1
                result = false
        End Select
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'input')

        Jump if False (Regular) to Block[B3]
            IInvocationOperation ( Function System.Nullable(Of System.Boolean).GetValueOrDefault() As System.Boolean) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: '= 1')
              Instance Receiver: 
                IBinaryOperation (BinaryOperatorKind.Equals, IsLifted, Checked) (OperationKind.Binary, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: '= 1')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'input')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: '1')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (WideningNullable)
                      Operand: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
              Arguments(0)
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = false')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_056()
            Dim source = <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result as Boolean, input as Integer?) 'BIND:"Sub M"
        Select Case input
            Case = Nothing
                result = false
        End Select
    End Sub
End Class
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC42037: This expression will always evaluate to Nothing (due to null propagation from the equals operator). To check if the value is null consider using 'Is Nothing'.
            Case = Nothing
                 ~~~~~~~~~
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
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'input')

        Jump if False (Regular) to Block[B3]
            IInvocationOperation ( Function System.Nullable(Of System.Boolean).GetValueOrDefault() As System.Boolean) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: '= Nothing')
              Instance Receiver: 
                IBinaryOperation (BinaryOperatorKind.Equals, IsLifted, Checked) (OperationKind.Binary, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: '= Nothing')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'input')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'Nothing')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (WideningNothingLiteral)
                      Operand: 
                        ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'Nothing')
              Arguments(0)
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = false')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_057()
            Dim source = <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result as Boolean, input as Integer?) 'BIND:"Sub M"
        Select Case input
            Case = 1, = Nothing
                result = false
        End Select
    End Sub
End Class
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC42037: This expression will always evaluate to Nothing (due to null propagation from the equals operator). To check if the value is null consider using 'Is Nothing'.
            Case = 1, = Nothing
                      ~~~~~~~~~
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
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'input')

        Next (Regular) Block[B2]
            Entering: {R2} {R3}

    .locals {R2}
    {
        CaptureIds: [2]
        .locals {R3}
        {
            CaptureIds: [1]
            Block[B2] - Block
                Predecessors: [B1]
                Statements (1)
                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '= 1')
                      Value: 
                        IBinaryOperation (BinaryOperatorKind.Equals, IsLifted, Checked) (OperationKind.Binary, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: '= 1')
                          Left: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'input')
                          Right: 
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: '1')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                (WideningNullable)
                              Operand: 
                                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

                Jump if True (Regular) to Block[B5]
                    IInvocationOperation ( Function System.Nullable(Of System.Boolean).GetValueOrDefault() As System.Boolean) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: '= 1')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: '= 1')
                      Arguments(0)

                Next (Regular) Block[B3]
                    Entering: {R4}

            .locals {R4}
            {
                CaptureIds: [3]
                Block[B3] - Block
                    Predecessors: [B2]
                    Statements (1)
                        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '= Nothing')
                          Value: 
                            IBinaryOperation (BinaryOperatorKind.Equals, IsLifted, Checked) (OperationKind.Binary, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: '= Nothing')
                              Left: 
                                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'input')
                              Right: 
                                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'Nothing')
                                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                    (WideningNothingLiteral)
                                  Operand: 
                                    ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'Nothing')

                    Jump if True (Regular) to Block[B5]
                        IInvocationOperation ( Function System.Nullable(Of System.Boolean).GetValueOrDefault() As System.Boolean) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: '= Nothing')
                          Instance Receiver: 
                            IUnaryOperation (UnaryOperatorKind.Not, IsLifted) (OperationKind.Unary, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: '= Nothing')
                              Operand: 
                                IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: '= Nothing')
                          Arguments(0)
                        Leaving: {R4}

                    Next (Regular) Block[B4]
                Block[B4] - Block
                    Predecessors: [B3]
                    Statements (1)
                        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'Case = 1, = Nothing')
                          Value: 
                            IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: '= Nothing')

                    Next (Regular) Block[B6]
                        Leaving: {R4} {R3}
            }

            Block[B5] - Block
                Predecessors: [B2] [B3]
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'Case = 1, = Nothing')
                      Value: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: '= 1')

                Next (Regular) Block[B6]
                    Leaving: {R3}
        }

        Block[B6] - Block
            Predecessors: [B4] [B5]
            Statements (0)
            Jump if False (Regular) to Block[B8]
                IInvocationOperation ( Function System.Nullable(Of System.Boolean).GetValueOrDefault() As System.Boolean) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'Case = 1, = Nothing')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: 'Case = 1, = Nothing')
                  Arguments(0)
                Leaving: {R2} {R1}

            Next (Regular) Block[B7]
                Leaving: {R2}
    }

    Block[B7] - Block
        Predecessors: [B6]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = false')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B8]
            Leaving: {R1}
}

Block[B8] - Exit
    Predecessors: [B6] [B7]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_058()
            Dim source = <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result as Boolean, other as Object) 'BIND:"Sub M"
        Select Case Function() 1
            Case = other
                result = false
        End Select
    End Sub
End Class
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36635: Lambda expressions are not valid in the first expression of a 'Select Case' statement.
        Select Case Function() 1
                    ~~~~~~~~~~~~
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
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'Function() 1')
              Value: 
                IDelegateCreationOperation (OperationKind.DelegateCreation, Type: Function <generated method>() As System.Int32, IsInvalid, IsImplicit) (Syntax: 'Function() 1')
                  Target: 
                    IFlowAnonymousFunctionOperation (Symbol: Function () As System.Int32) (OperationKind.FlowAnonymousFunction, Type: null, IsInvalid) (Syntax: 'Function() 1')
                    {
                        Block[B0#A0] - Entry
                            Statements (0)
                            Next (Regular) Block[B1#A0]
                                Entering: {R1#A0}

                        .locals {R1#A0}
                        {
                            Locals: [<anonymous local> As System.Int32]
                            Block[B1#A0] - Block
                                Predecessors: [B0#A0]
                                Statements (0)
                                Next (Return) Block[B2#A0]
                                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
                                    Leaving: {R1#A0}
                        }

                        Block[B2#A0] - Exit
                            Predecessors: [B1#A0]
                            Statements (0)
                    }

        Jump if False (Regular) to Block[B3]
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Boolean, IsImplicit) (Syntax: '= other')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                (NarrowingValue)
              Operand: 
                IBinaryOperation (BinaryOperatorKind.ObjectValueEquals, Checked) (OperationKind.Binary, Type: System.Object, IsImplicit) (Syntax: '= other')
                  Left: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsInvalid, IsImplicit) (Syntax: 'Function() 1')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (WideningReference, DelegateRelaxationLevelWideningToNonLambda)
                      Operand: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: Function <generated method>() As System.Int32, IsInvalid, IsImplicit) (Syntax: 'Function() 1')
                  Right: 
                    IParameterReferenceOperation: other (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'other')
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = false')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_059()
            Dim source = <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result as Boolean, input1 as Integer?, input2 as Integer, input3 as Integer) 'BIND:"Sub M"
        Select Case input3 
            Case = If(input1, input2)
                result = false
        End Select
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input3')
              Value: 
                IParameterReferenceOperation: input3 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'input3')

        Next (Regular) Block[B2]
            Entering: {R2} {R3}

    .locals {R2}
    {
        CaptureIds: [2]
        .locals {R3}
        {
            CaptureIds: [1]
            Block[B2] - Block
                Predecessors: [B1]
                Statements (1)
                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input1')
                      Value: 
                        IParameterReferenceOperation: input1 (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'input1')

                Jump if True (Regular) to Block[B4]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'input1')
                      Operand: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'input1')
                    Leaving: {R3}

                Next (Regular) Block[B3]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input1')
                      Value: 
                        IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'input1')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'input1')
                          Arguments(0)

                Next (Regular) Block[B5]
                    Leaving: {R3}
        }

        Block[B4] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input2')
                  Value: 
                    IParameterReferenceOperation: input2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'input2')

            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (0)
            Jump if False (Regular) to Block[B7]
                IBinaryOperation (BinaryOperatorKind.Equals, Checked) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '= If(input1, input2)')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input3')
                  Right: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(input1, input2)')
                Leaving: {R2} {R1}

            Next (Regular) Block[B6]
                Leaving: {R2}
    }

    Block[B6] - Block
        Predecessors: [B5]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = false')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B7]
            Leaving: {R1}
}

Block[B7] - Exit
    Predecessors: [B5] [B6]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_060()
            Dim source = <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result as Boolean, input as Integer) 'BIND:"Sub M"
        Select Case input
            Case <= 1
                result = false
        End Select
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'input')

        Jump if False (Regular) to Block[B3]
            IBinaryOperation (BinaryOperatorKind.LessThanOrEqual, Checked) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '<= 1')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = false')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_061()
            Dim source = <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result as Boolean, input as Integer) 'BIND:"Sub M"
        Select Case input
            Case < 0
                Dim x As Boolean = true
                result = x
            Case > 1
                result = false
        End Select
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'input')

        Jump if False (Regular) to Block[B3]
            IBinaryOperation (BinaryOperatorKind.LessThan, Checked) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '< 0')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        Locals: [x As System.Boolean]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (2)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'x As Boolean = true')
                  Left: 
                    ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Boolean, IsImplicit) (Syntax: 'x')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = x')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = x')
                      Left: 
                        IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                      Right: 
                        ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Boolean) (Syntax: 'x')

            Next (Regular) Block[B5]
                Leaving: {R2} {R1}
    }

    Block[B3] - Block
        Predecessors: [B1]
        Statements (0)
        Jump if False (Regular) to Block[B5]
            IBinaryOperation (BinaryOperatorKind.GreaterThan, Checked) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '> 1')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
            Leaving: {R1}

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = false')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B5]
            Leaving: {R1}
}

Block[B5] - Exit
    Predecessors: [B2] [B3] [B4]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_062()
            Dim source = <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result as Boolean, input as Integer) 'BIND:"Sub M"
        Select Case input
            Case >= 2, <> 1
                result = false
        End Select
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'input')

        Jump if True (Regular) to Block[B3]
            IBinaryOperation (BinaryOperatorKind.GreaterThanOrEqual, Checked) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '>= 2')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (0)
        Jump if False (Regular) to Block[B4]
            IBinaryOperation (BinaryOperatorKind.NotEquals, Checked) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '<> 1')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
            Leaving: {R1}

        Next (Regular) Block[B3]
    Block[B3] - Block
        Predecessors: [B1] [B2]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = false')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B4]
            Leaving: {R1}
}

Block[B4] - Exit
    Predecessors: [B2] [B3]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_063()
            Dim source = <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result as Boolean, input as Integer) 'BIND:"Sub M"
        Select Case input
            Case <> 1L
                result = false
        End Select
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'input')

        Jump if False (Regular) to Block[B3]
            IBinaryOperation (BinaryOperatorKind.NotEquals, Checked) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '<> 1L')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input')
              Right: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '1L')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (WideningNumeric, InvolvesNarrowingFromNumericConstant)
                  Operand: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int64, Constant: 1) (Syntax: '1L')
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = false')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_064()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result As Boolean, input As C, other As C) 'BIND:"Sub M"
        Select Case input
            Case <> other
                result = False
        End Select
    End Sub

    Public Shared Operator =(x As C, y As C) As Boolean?
        Return False
    End Operator

    Public Shared Operator <>(x As C, y As C) As Boolean?
        Return True
    End Operator
End Class
        ]]>
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source)

            compilation.AssertNoDiagnostics()

            Dim tree = compilation.SyntaxTrees.Single()
            Dim node = tree.GetRoot().DescendantNodes().OfType(Of RelationalCaseClauseSyntax)().Single()

            compilation.VerifyOperationTree(node, expectedOperationTree:=
            <![CDATA[
IRelationalCaseClauseOperation (Relational operator kind: BinaryOperatorKind.NotEquals) (CaseKind.Relational) (OperationKind.CaseClause, Type: null) (Syntax: '<> other')
  Value: 
    IParameterReferenceOperation: other (OperationKind.ParameterReference, Type: C) (Syntax: 'other')
]]>.Value)

            Dim expectedGraph =
            <![CDATA[
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: C) (Syntax: 'input')

        Jump if False (Regular) to Block[B3]
            IInvocationOperation ( Function System.Nullable(Of System.Boolean).GetValueOrDefault() As System.Boolean) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: '<> other')
              Instance Receiver: 
                IBinaryOperation (BinaryOperatorKind.NotEquals, Checked) (OperatorMethod: Function C.op_Inequality(x As C, y As C) As System.Nullable(Of System.Boolean)) (OperationKind.Binary, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: '<> other')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'input')
                  Right: 
                    IParameterReferenceOperation: other (OperationKind.ParameterReference, Type: C) (Syntax: 'other')
              Arguments(0)
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = False')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = False')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'False')

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphForTest(Of MethodBlockSyntax)(compilation, expectedGraph)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_065()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result As Boolean, input As C, other as Integer) 'BIND:"Sub M"
        Select Case input
            Case <= other
                result = False
        End Select
    End Sub

    Public Shared Operator <=(x As C, y As Short) As Object
        Return False
    End Operator

    Public Shared Operator >=(x As C, y As Short) As Object
        Return False
    End Operator
End Class
         ]]>
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source)

            compilation.AssertNoDiagnostics()

            Dim tree = compilation.SyntaxTrees.Single()
            Dim node = tree.GetRoot().DescendantNodes().OfType(Of RelationalCaseClauseSyntax)().Single()

            compilation.VerifyOperationTree(node, expectedOperationTree:=
            <![CDATA[
IRelationalCaseClauseOperation (Relational operator kind: BinaryOperatorKind.LessThanOrEqual) (CaseKind.Relational) (OperationKind.CaseClause, Type: null) (Syntax: '<= other')
  Value: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int16, IsImplicit) (Syntax: 'other')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IParameterReferenceOperation: other (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'other')
]]>.Value)

            Dim expectedGraph =
            <![CDATA[
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: C) (Syntax: 'input')

        Jump if False (Regular) to Block[B3]
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Boolean, IsImplicit) (Syntax: '<= other')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                (NarrowingValue)
              Operand: 
                IBinaryOperation (BinaryOperatorKind.LessThanOrEqual, Checked) (OperatorMethod: Function C.op_LessThanOrEqual(x As C, y As System.Int16) As System.Object) (OperationKind.Binary, Type: System.Object, IsImplicit) (Syntax: '<= other')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'input')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int16, IsImplicit) (Syntax: 'other')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (NarrowingNumeric)
                      Operand: 
                        IParameterReferenceOperation: other (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'other')
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = False')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = False')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'False')

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphForTest(Of MethodBlockSyntax)(compilation, expectedGraph)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_066()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result As Boolean, input As C, other as Integer) 'BIND:"Sub M"
        Select Case other
            Case >= input
                result = False
        End Select
    End Sub

    Public Shared Operator >=(y As Short, x As C) As Boolean
        Return False
    End Operator

    Public Shared Operator <=(y As Short, x As C) As Boolean
        Return False
    End Operator
End Class
         ]]>
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source)

            compilation.AssertNoDiagnostics()

            Dim tree = compilation.SyntaxTrees.Single()
            Dim node = tree.GetRoot().DescendantNodes().OfType(Of RelationalCaseClauseSyntax)().Single()

            compilation.VerifyOperationTree(node, expectedOperationTree:=
            <![CDATA[
IRelationalCaseClauseOperation (Relational operator kind: BinaryOperatorKind.GreaterThanOrEqual) (CaseKind.Relational) (OperationKind.CaseClause, Type: null) (Syntax: '>= input')
  Value: 
    IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: C) (Syntax: 'input')
]]>.Value)

            Dim expectedGraph =
            <![CDATA[
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'other')
              Value: 
                IParameterReferenceOperation: other (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'other')

        Jump if False (Regular) to Block[B3]
            IBinaryOperation (BinaryOperatorKind.GreaterThanOrEqual, Checked) (OperatorMethod: Function C.op_GreaterThanOrEqual(y As System.Int16, x As C) As System.Boolean) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '>= input')
              Left: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int16, IsImplicit) (Syntax: 'other')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (NarrowingNumeric)
                  Operand: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'other')
              Right: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: C) (Syntax: 'input')
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = False')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = False')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'False')

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphForTest(Of MethodBlockSyntax)(compilation, expectedGraph)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_067()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result As Boolean, input As C) 'BIND:"Sub M"
        Select Case input
            Case < 1
                result = False
        End Select
    End Sub

    Public Shared Widening Operator CType(x As Integer) As C
        Return Nothing
    End Operator
End Class
         ]]>
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source)

            compilation.AssertTheseDiagnostics(
<expected><![CDATA[
BC30452: Operator '<' is not defined for types 'C' and 'Integer'.
            Case < 1
                 ~~~
]]></expected>)

            Dim tree = compilation.SyntaxTrees.Single()
            Dim node = tree.GetRoot().DescendantNodes().OfType(Of RelationalCaseClauseSyntax)().Single()

            compilation.VerifyOperationTree(node, expectedOperationTree:=
            <![CDATA[
IRelationalCaseClauseOperation (Relational operator kind: BinaryOperatorKind.LessThan) (CaseKind.Relational) (OperationKind.CaseClause, Type: null, IsInvalid) (Syntax: '< 1')
  Value: 
    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
]]>.Value)

            Dim expectedGraph =
            <![CDATA[
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: C) (Syntax: 'input')

        Jump if False (Regular) to Block[B3]
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: '< 1')
              Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                (DelegateRelaxationLevelNone)
              Operand: 
                IBinaryOperation (BinaryOperatorKind.LessThan, Checked) (OperationKind.Binary, Type: ?, IsInvalid, IsImplicit) (Syntax: '< 1')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'input')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = False')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = False')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'False')

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphForTest(Of MethodBlockSyntax)(compilation, expectedGraph)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_068()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result As Boolean, input As C, other As C) 'BIND:"Sub M"
        Select Case input
            Case <> other
                result = False
        End Select
    End Sub

    Public Shared Operator =(x As C, y As C) As C
        Return Nothing
    End Operator

    Public Shared Operator <>(x As C, y As C) As C
        Return Nothing
    End Operator

    Public Shared Operator IsTrue(x As C) As Boolean
        Return True
    End Operator

    Public Shared Operator IsFalse(x As C) As Boolean
        Return False
    End Operator
End Class
        ]]>
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source)

            compilation.AssertNoDiagnostics()

            Dim tree = compilation.SyntaxTrees.Single()
            Dim node = tree.GetRoot().DescendantNodes().OfType(Of RelationalCaseClauseSyntax)().Single()

            compilation.VerifyOperationTree(node, expectedOperationTree:=
            <![CDATA[
IRelationalCaseClauseOperation (Relational operator kind: BinaryOperatorKind.NotEquals) (CaseKind.Relational) (OperationKind.CaseClause, Type: null) (Syntax: '<> other')
  Value: 
    IParameterReferenceOperation: other (OperationKind.ParameterReference, Type: C) (Syntax: 'other')
]]>.Value)

            Dim expectedGraph =
            <![CDATA[
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: C) (Syntax: 'input')

        Jump if False (Regular) to Block[B3]
            IUnaryOperation (UnaryOperatorKind.True) (OperatorMethod: Function C.op_True(x As C) As System.Boolean) (OperationKind.Unary, Type: System.Boolean, IsImplicit) (Syntax: '<> other')
              Operand: 
                IBinaryOperation (BinaryOperatorKind.NotEquals, Checked) (OperatorMethod: Function C.op_Inequality(x As C, y As C) As C) (OperationKind.Binary, Type: C, IsImplicit) (Syntax: '<> other')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'input')
                  Right: 
                    IParameterReferenceOperation: other (OperationKind.ParameterReference, Type: C) (Syntax: 'other')
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = False')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = False')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'False')

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphForTest(Of MethodBlockSyntax)(compilation, expectedGraph)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_069()
            Dim source = <![CDATA[
Option Compare Text
Imports System
Public NotInheritable Class C
    Sub M(result as Boolean, input as String, other As String) 'BIND:"Sub M"
        Select Case input
            Case > other
                result = false
        End Select
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.String) (Syntax: 'input')

        Jump if False (Regular) to Block[B3]
            IBinaryOperation (BinaryOperatorKind.GreaterThan, Checked, CompareText) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '> other')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'input')
              Right: 
                IParameterReferenceOperation: other (OperationKind.ParameterReference, Type: System.String) (Syntax: 'other')
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = false')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_070()
            Dim source = <![CDATA[
Option Compare Binary
Imports System
Public NotInheritable Class C
    Sub M(result as Boolean, input as String, other As String) 'BIND:"Sub M"
        Select Case input
            Case <> other
                result = false
        End Select
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.String) (Syntax: 'input')

        Jump if False (Regular) to Block[B3]
            IBinaryOperation (BinaryOperatorKind.NotEquals, Checked) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '<> other')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'input')
              Right: 
                IParameterReferenceOperation: other (OperationKind.ParameterReference, Type: System.String) (Syntax: 'other')
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = false')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_071()
            Dim source = <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result As Boolean, other As C, input as Integer) 'BIND:"Sub M"
        Select Case input
            Case < 1, > other
                result = False
        End Select
    End Sub

    Public Shared Operator <(y As Integer, x As C) As Boolean
        Return False
    End Operator

    Public Shared Operator >(y As integer, x As C) As Boolean
        Return True
    End Operator
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'input')

        Jump if True (Regular) to Block[B3]
            IBinaryOperation (BinaryOperatorKind.LessThan, Checked) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '< 1')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (0)
        Jump if False (Regular) to Block[B4]
            IBinaryOperation (BinaryOperatorKind.GreaterThan, Checked) (OperatorMethod: Function C.op_GreaterThan(y As System.Int32, x As C) As System.Boolean) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '> other')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input')
              Right: 
                IParameterReferenceOperation: other (OperationKind.ParameterReference, Type: C) (Syntax: 'other')
            Leaving: {R1}

        Next (Regular) Block[B3]
    Block[B3] - Block
        Predecessors: [B1] [B2]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = False')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = False')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'False')

        Next (Regular) Block[B4]
            Leaving: {R1}
}

Block[B4] - Exit
    Predecessors: [B2] [B3]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_072()
            Dim source = <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result As Boolean, other As C, input as Object) 'BIND:"Sub M"
        Select Case input
            Case <> other
                result = False
        End Select
    End Sub

    Public Shared Operator =(y As Integer, x As C) As Boolean
        Return False
    End Operator

    Public Shared Operator <>(y As integer, x As C) As Boolean
        Return True
    End Operator
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'input')

        Jump if False (Regular) to Block[B3]
            IBinaryOperation (BinaryOperatorKind.NotEquals, Checked) (OperatorMethod: Function C.op_Inequality(y As System.Int32, x As C) As System.Boolean) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '<> other')
              Left: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsImplicit) (Syntax: 'input')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (NarrowingValue)
                  Operand: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'input')
              Right: 
                IParameterReferenceOperation: other (OperationKind.ParameterReference, Type: C) (Syntax: 'other')
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = False')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = False')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'False')

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_073()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(result As Boolean, other As C, input as Object) 'BIND:"Sub M"
        Select Case input
            Case > other
                result = False
        End Select
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'input')

        Jump if False (Regular) to Block[B3]
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Boolean, IsImplicit) (Syntax: '> other')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                (NarrowingValue)
              Operand: 
                IBinaryOperation (BinaryOperatorKind.GreaterThan, Checked) (OperationKind.Binary, Type: System.Object, IsImplicit) (Syntax: '> other')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'input')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'other')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (WideningReference)
                      Operand: 
                        IParameterReferenceOperation: other (OperationKind.ParameterReference, Type: C) (Syntax: 'other')
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = False')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = False')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'False')

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_074()
            Dim source = <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result As Boolean, other As Integer, input as Object) 'BIND:"Sub M"
        Select Case input
            Case >= other
                result = False
        End Select
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'input')

        Jump if False (Regular) to Block[B3]
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Boolean, IsImplicit) (Syntax: '>= other')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                (NarrowingValue)
              Operand: 
                IBinaryOperation (BinaryOperatorKind.GreaterThanOrEqual, Checked) (OperationKind.Binary, Type: System.Object, IsImplicit) (Syntax: '>= other')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'input')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'other')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (WideningValue)
                      Operand: 
                        IParameterReferenceOperation: other (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'other')
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = False')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = False')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'False')

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_075()
            Dim source = <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result As Boolean, input As Integer, other as Object) 'BIND:"Sub M"
        Select Case input
            Case <= other
                result = False
        End Select
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'input')

        Jump if False (Regular) to Block[B3]
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Boolean, IsImplicit) (Syntax: '<= other')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                (NarrowingValue)
              Operand: 
                IBinaryOperation (BinaryOperatorKind.LessThanOrEqual, Checked) (OperationKind.Binary, Type: System.Object, IsImplicit) (Syntax: '<= other')
                  Left: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'input')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (WideningValue)
                      Operand: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input')
                  Right: 
                    IParameterReferenceOperation: other (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'other')
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = False')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = False')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'False')

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_076()
            Dim source = <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result As Boolean, input As Object, other as Object) 'BIND:"Sub M"
        Select Case input
            Case < other
                result = False
        End Select
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'input')

        Jump if False (Regular) to Block[B3]
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Boolean, IsImplicit) (Syntax: '< other')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                (NarrowingValue)
              Operand: 
                IBinaryOperation (BinaryOperatorKind.LessThan, Checked) (OperationKind.Binary, Type: System.Object, IsImplicit) (Syntax: '< other')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'input')
                  Right: 
                    IParameterReferenceOperation: other (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'other')
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = False')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = False')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'False')

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_077()
            Dim source = <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result As Boolean, input As C, other1 As C, other2 As C) 'BIND:"Sub M"
        Select Case input
            Case < other1, > other2
                result = False
        End Select
    End Sub

    Public Shared Operator >(x As C, y As C) As C
        Return Nothing
    End Operator

    Public Shared Operator <(x As C, y As C) As C
        Return Nothing
    End Operator

    Public Shared Operator IsTrue(x As C) As Boolean
        Return True
    End Operator

    Public Shared Operator IsFalse(x As C) As Boolean
        Return False
    End Operator

    Public Shared Operator Or(x As C, y As C) As C
        Return Nothing
    End Operator
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: C) (Syntax: 'input')

        Next (Regular) Block[B2]
            Entering: {R2} {R3}

    .locals {R2}
    {
        CaptureIds: [2]
        .locals {R3}
        {
            CaptureIds: [1]
            Block[B2] - Block
                Predecessors: [B1]
                Statements (1)
                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '< other1')
                      Value: 
                        IBinaryOperation (BinaryOperatorKind.LessThan, Checked) (OperatorMethod: Function C.op_LessThan(x As C, y As C) As C) (OperationKind.Binary, Type: C, IsImplicit) (Syntax: '< other1')
                          Left: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'input')
                          Right: 
                            IParameterReferenceOperation: other1 (OperationKind.ParameterReference, Type: C) (Syntax: 'other1')

                Jump if False (Regular) to Block[B4]
                    IUnaryOperation (UnaryOperatorKind.True) (OperatorMethod: Function C.op_True(x As C) As System.Boolean) (OperationKind.Unary, Type: System.Boolean, IsImplicit) (Syntax: '< other1')
                      Operand: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: '< other1')

                Next (Regular) Block[B3]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'Case < other1, > other2')
                      Value: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: '< other1')

                Next (Regular) Block[B5]
                    Leaving: {R3}
            Block[B4] - Block
                Predecessors: [B2]
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'Case < other1, > other2')
                      Value: 
                        IBinaryOperation (BinaryOperatorKind.Or) (OperatorMethod: Function C.op_BitwiseOr(x As C, y As C) As C) (OperationKind.Binary, Type: C, IsImplicit) (Syntax: 'Case < other1, > other2')
                          Left: 
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: '< other1')
                          Right: 
                            IBinaryOperation (BinaryOperatorKind.GreaterThan, Checked) (OperatorMethod: Function C.op_GreaterThan(x As C, y As C) As C) (OperationKind.Binary, Type: C, IsImplicit) (Syntax: '> other2')
                              Left: 
                                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'input')
                              Right: 
                                IParameterReferenceOperation: other2 (OperationKind.ParameterReference, Type: C) (Syntax: 'other2')

                Next (Regular) Block[B5]
                    Leaving: {R3}
        }

        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (0)
            Jump if False (Regular) to Block[B7]
                IUnaryOperation (UnaryOperatorKind.True) (OperatorMethod: Function C.op_True(x As C) As System.Boolean) (OperationKind.Unary, Type: System.Boolean, IsImplicit) (Syntax: 'Case < other1, > other2')
                  Operand: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'Case < other1, > other2')
                Leaving: {R2} {R1}

            Next (Regular) Block[B6]
                Leaving: {R2}
    }

    Block[B6] - Block
        Predecessors: [B5]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = False')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = False')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'False')

        Next (Regular) Block[B7]
            Leaving: {R1}
}

Block[B7] - Exit
    Predecessors: [B5] [B6]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_078()
            Dim source = <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result as Boolean, input as Integer?) 'BIND:"Sub M"
        Select Case input
            Case <> 1
                result = false
        End Select
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'input')

        Jump if False (Regular) to Block[B3]
            IInvocationOperation ( Function System.Nullable(Of System.Boolean).GetValueOrDefault() As System.Boolean) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: '<> 1')
              Instance Receiver: 
                IBinaryOperation (BinaryOperatorKind.NotEquals, IsLifted, Checked) (OperationKind.Binary, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: '<> 1')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'input')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: '1')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (WideningNullable)
                      Operand: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
              Arguments(0)
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = false')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_079()
            Dim source = <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result as Boolean, input as Integer?) 'BIND:"Sub M"
        Select Case input
            Case <> Nothing
                result = false
        End Select
    End Sub
End Class
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC42038: This expression will always evaluate to Nothing (due to null propagation from the equals operator). To check if the value is not null consider using 'IsNot Nothing'.
            Case <> Nothing
                 ~~~~~~~~~~
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
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'input')

        Jump if False (Regular) to Block[B3]
            IInvocationOperation ( Function System.Nullable(Of System.Boolean).GetValueOrDefault() As System.Boolean) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: '<> Nothing')
              Instance Receiver: 
                IBinaryOperation (BinaryOperatorKind.NotEquals, IsLifted, Checked) (OperationKind.Binary, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: '<> Nothing')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'input')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'Nothing')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (WideningNothingLiteral)
                      Operand: 
                        ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'Nothing')
              Arguments(0)
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = false')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_080()
            Dim source = <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result as Boolean, input as Integer?) 'BIND:"Sub M"
        Select Case input
            Case <= 1, >= Nothing
                result = false
        End Select
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'input')

        Next (Regular) Block[B2]
            Entering: {R2} {R3}

    .locals {R2}
    {
        CaptureIds: [2]
        .locals {R3}
        {
            CaptureIds: [1]
            Block[B2] - Block
                Predecessors: [B1]
                Statements (1)
                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '<= 1')
                      Value: 
                        IBinaryOperation (BinaryOperatorKind.LessThanOrEqual, IsLifted, Checked) (OperationKind.Binary, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: '<= 1')
                          Left: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'input')
                          Right: 
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: '1')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                (WideningNullable)
                              Operand: 
                                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

                Jump if True (Regular) to Block[B5]
                    IInvocationOperation ( Function System.Nullable(Of System.Boolean).GetValueOrDefault() As System.Boolean) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: '<= 1')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: '<= 1')
                      Arguments(0)

                Next (Regular) Block[B3]
                    Entering: {R4}

            .locals {R4}
            {
                CaptureIds: [3]
                Block[B3] - Block
                    Predecessors: [B2]
                    Statements (1)
                        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '>= Nothing')
                          Value: 
                            IBinaryOperation (BinaryOperatorKind.GreaterThanOrEqual, IsLifted, Checked) (OperationKind.Binary, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: '>= Nothing')
                              Left: 
                                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'input')
                              Right: 
                                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'Nothing')
                                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                    (WideningNothingLiteral)
                                  Operand: 
                                    ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'Nothing')

                    Jump if True (Regular) to Block[B5]
                        IInvocationOperation ( Function System.Nullable(Of System.Boolean).GetValueOrDefault() As System.Boolean) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: '>= Nothing')
                          Instance Receiver: 
                            IUnaryOperation (UnaryOperatorKind.Not, IsLifted) (OperationKind.Unary, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: '>= Nothing')
                              Operand: 
                                IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: '>= Nothing')
                          Arguments(0)
                        Leaving: {R4}

                    Next (Regular) Block[B4]
                Block[B4] - Block
                    Predecessors: [B3]
                    Statements (1)
                        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'Case <= 1, >= Nothing')
                          Value: 
                            IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: '>= Nothing')

                    Next (Regular) Block[B6]
                        Leaving: {R4} {R3}
            }

            Block[B5] - Block
                Predecessors: [B2] [B3]
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'Case <= 1, >= Nothing')
                      Value: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: '<= 1')

                Next (Regular) Block[B6]
                    Leaving: {R3}
        }

        Block[B6] - Block
            Predecessors: [B4] [B5]
            Statements (0)
            Jump if False (Regular) to Block[B8]
                IInvocationOperation ( Function System.Nullable(Of System.Boolean).GetValueOrDefault() As System.Boolean) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'Case <= 1, >= Nothing')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: 'Case <= 1, >= Nothing')
                  Arguments(0)
                Leaving: {R2} {R1}

            Next (Regular) Block[B7]
                Leaving: {R2}
    }

    Block[B7] - Block
        Predecessors: [B6]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = false')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B8]
            Leaving: {R1}
}

Block[B8] - Exit
    Predecessors: [B6] [B7]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_081()
            Dim source = <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result as Boolean, other as Object) 'BIND:"Sub M"
        Select Case Function() 1
            Case <> other
                result = false
        End Select
    End Sub
End Class
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36635: Lambda expressions are not valid in the first expression of a 'Select Case' statement.
        Select Case Function() 1
                    ~~~~~~~~~~~~
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
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'Function() 1')
              Value: 
                IDelegateCreationOperation (OperationKind.DelegateCreation, Type: Function <generated method>() As System.Int32, IsInvalid, IsImplicit) (Syntax: 'Function() 1')
                  Target: 
                    IFlowAnonymousFunctionOperation (Symbol: Function () As System.Int32) (OperationKind.FlowAnonymousFunction, Type: null, IsInvalid) (Syntax: 'Function() 1')
                    {
                        Block[B0#A0] - Entry
                            Statements (0)
                            Next (Regular) Block[B1#A0]
                                Entering: {R1#A0}

                        .locals {R1#A0}
                        {
                            Locals: [<anonymous local> As System.Int32]
                            Block[B1#A0] - Block
                                Predecessors: [B0#A0]
                                Statements (0)
                                Next (Return) Block[B2#A0]
                                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
                                    Leaving: {R1#A0}
                        }

                        Block[B2#A0] - Exit
                            Predecessors: [B1#A0]
                            Statements (0)
                    }

        Jump if False (Regular) to Block[B3]
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Boolean, IsImplicit) (Syntax: '<> other')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                (NarrowingValue)
              Operand: 
                IBinaryOperation (BinaryOperatorKind.ObjectValueNotEquals, Checked) (OperationKind.Binary, Type: System.Object, IsImplicit) (Syntax: '<> other')
                  Left: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsInvalid, IsImplicit) (Syntax: 'Function() 1')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (WideningReference, DelegateRelaxationLevelWideningToNonLambda)
                      Operand: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: Function <generated method>() As System.Int32, IsInvalid, IsImplicit) (Syntax: 'Function() 1')
                  Right: 
                    IParameterReferenceOperation: other (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'other')
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = false')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_082()
            Dim source = <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result as Boolean, input1 as Integer?, input2 as Integer, input3 as Integer) 'BIND:"Sub M"
        Select Case input3 
            Case <> If(input1, input2)
                result = false
        End Select
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input3')
              Value: 
                IParameterReferenceOperation: input3 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'input3')

        Next (Regular) Block[B2]
            Entering: {R2} {R3}

    .locals {R2}
    {
        CaptureIds: [2]
        .locals {R3}
        {
            CaptureIds: [1]
            Block[B2] - Block
                Predecessors: [B1]
                Statements (1)
                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input1')
                      Value: 
                        IParameterReferenceOperation: input1 (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'input1')

                Jump if True (Regular) to Block[B4]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'input1')
                      Operand: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'input1')
                    Leaving: {R3}

                Next (Regular) Block[B3]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input1')
                      Value: 
                        IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'input1')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'input1')
                          Arguments(0)

                Next (Regular) Block[B5]
                    Leaving: {R3}
        }

        Block[B4] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input2')
                  Value: 
                    IParameterReferenceOperation: input2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'input2')

            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (0)
            Jump if False (Regular) to Block[B7]
                IBinaryOperation (BinaryOperatorKind.NotEquals, Checked) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '<> If(input1, input2)')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input3')
                  Right: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(input1, input2)')
                Leaving: {R2} {R1}

            Next (Regular) Block[B6]
                Leaving: {R2}
    }

    Block[B6] - Block
        Predecessors: [B5]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = false')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B7]
            Leaving: {R1}
}

Block[B7] - Exit
    Predecessors: [B5] [B6]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_083()
            Dim source = <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result as Boolean, input as Integer) 'BIND:"Sub M"
        Select Case input
            Case 1 To 3
                result = false
        End Select
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'input')

        Jump if False (Regular) to Block[B4]
            IBinaryOperation (BinaryOperatorKind.GreaterThanOrEqual, Checked) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '1 To 3')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (0)
        Jump if False (Regular) to Block[B4]
            IBinaryOperation (BinaryOperatorKind.LessThanOrEqual, Checked) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '1 To 3')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
            Leaving: {R1}

        Next (Regular) Block[B3]
    Block[B3] - Block
        Predecessors: [B2]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = false')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B4]
            Leaving: {R1}
}

Block[B4] - Exit
    Predecessors: [B1] [B2] [B3]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_084()
            Dim source = <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result as Boolean, input as Integer) 'BIND:"Sub M"
        Select Case input
            Case 0 To 2
                Dim x As Boolean = true
                result = x
            Case 5 To 7
                result = false
        End Select
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'input')

        Jump if False (Regular) to Block[B4]
            IBinaryOperation (BinaryOperatorKind.GreaterThanOrEqual, Checked) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '0 To 2')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (0)
        Jump if False (Regular) to Block[B4]
            IBinaryOperation (BinaryOperatorKind.LessThanOrEqual, Checked) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '0 To 2')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

        Next (Regular) Block[B3]
            Entering: {R2}

    .locals {R2}
    {
        Locals: [x As System.Boolean]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (2)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'x As Boolean = true')
                  Left: 
                    ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Boolean, IsImplicit) (Syntax: 'x')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = x')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = x')
                      Left: 
                        IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                      Right: 
                        ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Boolean) (Syntax: 'x')

            Next (Regular) Block[B7]
                Leaving: {R2} {R1}
    }

    Block[B4] - Block
        Predecessors: [B1] [B2]
        Statements (0)
        Jump if False (Regular) to Block[B7]
            IBinaryOperation (BinaryOperatorKind.GreaterThanOrEqual, Checked) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '5 To 7')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')
            Leaving: {R1}

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B4]
        Statements (0)
        Jump if False (Regular) to Block[B7]
            IBinaryOperation (BinaryOperatorKind.LessThanOrEqual, Checked) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '5 To 7')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 7) (Syntax: '7')
            Leaving: {R1}

        Next (Regular) Block[B6]
    Block[B6] - Block
        Predecessors: [B5]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = false')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B7]
            Leaving: {R1}
}

Block[B7] - Exit
    Predecessors: [B3] [B4] [B5] [B6]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_085()
            Dim source = <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result as Boolean, input as Integer) 'BIND:"Sub M"
        Select Case input
            Case 0 To 2, 5 To 7
                result = false
        End Select
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'input')

        Jump if False (Regular) to Block[B3]
            IBinaryOperation (BinaryOperatorKind.GreaterThanOrEqual, Checked) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '0 To 2')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (0)
        Jump if True (Regular) to Block[B5]
            IBinaryOperation (BinaryOperatorKind.LessThanOrEqual, Checked) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '0 To 2')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

        Next (Regular) Block[B3]
    Block[B3] - Block
        Predecessors: [B1] [B2]
        Statements (0)
        Jump if False (Regular) to Block[B6]
            IBinaryOperation (BinaryOperatorKind.GreaterThanOrEqual, Checked) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '5 To 7')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')
            Leaving: {R1}

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B3]
        Statements (0)
        Jump if False (Regular) to Block[B6]
            IBinaryOperation (BinaryOperatorKind.LessThanOrEqual, Checked) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '5 To 7')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 7) (Syntax: '7')
            Leaving: {R1}

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B2] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = false')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Exit
    Predecessors: [B3] [B4] [B5]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_086()
            Dim source = <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result as Boolean, input as Integer) 'BIND:"Sub M"
        Select Case input
            Case 1L To 3L
                result = false
        End Select
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'input')

        Jump if False (Regular) to Block[B4]
            IBinaryOperation (BinaryOperatorKind.GreaterThanOrEqual, Checked) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '1L To 3L')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input')
              Right: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '1L')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (WideningNumeric, InvolvesNarrowingFromNumericConstant)
                  Operand: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int64, Constant: 1) (Syntax: '1L')
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (0)
        Jump if False (Regular) to Block[B4]
            IBinaryOperation (BinaryOperatorKind.LessThanOrEqual, Checked) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '1L To 3L')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input')
              Right: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, Constant: 3, IsImplicit) (Syntax: '3L')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (WideningNumeric, InvolvesNarrowingFromNumericConstant)
                  Operand: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int64, Constant: 3) (Syntax: '3L')
            Leaving: {R1}

        Next (Regular) Block[B3]
    Block[B3] - Block
        Predecessors: [B2]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = false')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B4]
            Leaving: {R1}
}

Block[B4] - Exit
    Predecessors: [B1] [B2] [B3]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_087()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result As Boolean, input As C, other1 As C, other2 As C) 'BIND:"Sub M"
        Select Case input
            Case other1 To other2
                result = False
        End Select
    End Sub

    Public Shared Operator <=(x As C, y As C) As Boolean?
        Return False
    End Operator

    Public Shared Operator >=(x As C, y As C) As Boolean?
        Return True
    End Operator
End Class
        ]]>
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source)

            compilation.AssertNoDiagnostics()

            Dim tree = compilation.SyntaxTrees.Single()
            Dim node = tree.GetRoot().DescendantNodes().OfType(Of RangeCaseClauseSyntax)().Single()

            compilation.VerifyOperationTree(node, expectedOperationTree:=
            <![CDATA[
IRangeCaseClauseOperation (CaseKind.Range) (OperationKind.CaseClause, Type: null) (Syntax: 'other1 To other2')
  Min: 
    IParameterReferenceOperation: other1 (OperationKind.ParameterReference, Type: C) (Syntax: 'other1')
  Max: 
    IParameterReferenceOperation: other2 (OperationKind.ParameterReference, Type: C) (Syntax: 'other2')
]]>.Value)

            Dim expectedGraph =
            <![CDATA[
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: C) (Syntax: 'input')

        Next (Regular) Block[B2]
            Entering: {R2} {R3}

    .locals {R2}
    {
        CaptureIds: [2]
        .locals {R3}
        {
            CaptureIds: [1]
            Block[B2] - Block
                Predecessors: [B1]
                Statements (1)
                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'other1 To other2')
                      Value: 
                        IBinaryOperation (BinaryOperatorKind.GreaterThanOrEqual, Checked) (OperatorMethod: Function C.op_GreaterThanOrEqual(x As C, y As C) As System.Nullable(Of System.Boolean)) (OperationKind.Binary, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: 'other1 To other2')
                          Left: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'input')
                          Right: 
                            IParameterReferenceOperation: other1 (OperationKind.ParameterReference, Type: C) (Syntax: 'other1')

                Jump if True (Regular) to Block[B5]
                    IInvocationOperation ( Function System.Nullable(Of System.Boolean).GetValueOrDefault() As System.Boolean) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'other1 To other2')
                      Instance Receiver: 
                        IUnaryOperation (UnaryOperatorKind.Not, IsLifted) (OperationKind.Unary, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: 'other1 To other2')
                          Operand: 
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: 'other1 To other2')
                      Arguments(0)

                Next (Regular) Block[B3]
                    Entering: {R4}

            .locals {R4}
            {
                CaptureIds: [3]
                Block[B3] - Block
                    Predecessors: [B2]
                    Statements (1)
                        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'other1 To other2')
                          Value: 
                            IBinaryOperation (BinaryOperatorKind.LessThanOrEqual, Checked) (OperatorMethod: Function C.op_LessThanOrEqual(x As C, y As C) As System.Nullable(Of System.Boolean)) (OperationKind.Binary, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: 'other1 To other2')
                              Left: 
                                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'input')
                              Right: 
                                IParameterReferenceOperation: other2 (OperationKind.ParameterReference, Type: C) (Syntax: 'other2')

                    Jump if True (Regular) to Block[B5]
                        IInvocationOperation ( Function System.Nullable(Of System.Boolean).GetValueOrDefault() As System.Boolean) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'other1 To other2')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: 'other1 To other2')
                          Arguments(0)
                        Leaving: {R4}

                    Next (Regular) Block[B4]
                Block[B4] - Block
                    Predecessors: [B3]
                    Statements (1)
                        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'other1 To other2')
                          Value: 
                            IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: 'other1 To other2')

                    Next (Regular) Block[B6]
                        Leaving: {R4} {R3}
            }

            Block[B5] - Block
                Predecessors: [B2] [B3]
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'other1 To other2')
                      Value: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: 'other1 To other2')

                Next (Regular) Block[B6]
                    Leaving: {R3}
        }

        Block[B6] - Block
            Predecessors: [B4] [B5]
            Statements (0)
            Jump if False (Regular) to Block[B8]
                IInvocationOperation ( Function System.Nullable(Of System.Boolean).GetValueOrDefault() As System.Boolean) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'other1 To other2')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: 'other1 To other2')
                  Arguments(0)
                Leaving: {R2} {R1}

            Next (Regular) Block[B7]
                Leaving: {R2}
    }

    Block[B7] - Block
        Predecessors: [B6]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = False')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = False')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'False')

        Next (Regular) Block[B8]
            Leaving: {R1}
}

Block[B8] - Exit
    Predecessors: [B6] [B7]
    Statements (0)
]]>.Value

            VerifyFlowGraphForTest(Of MethodBlockSyntax)(compilation, expectedGraph)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_088()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result As Boolean, input As C, other1 as Integer, other2 as Integer) 'BIND:"Sub M"
        Select Case input
            Case other1 To other2
                result = False
        End Select
    End Sub

    Public Shared Operator <=(x As C, y As Short) As Object
        Return False
    End Operator

    Public Shared Operator >=(x As C, y As Short) As Object
        Return False
    End Operator
End Class
         ]]>
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source)

            compilation.AssertNoDiagnostics()

            Dim tree = compilation.SyntaxTrees.Single()
            Dim node = tree.GetRoot().DescendantNodes().OfType(Of RangeCaseClauseSyntax)().Single()

            compilation.VerifyOperationTree(node, expectedOperationTree:=
            <![CDATA[
IRangeCaseClauseOperation (CaseKind.Range) (OperationKind.CaseClause, Type: null) (Syntax: 'other1 To other2')
  Min: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int16, IsImplicit) (Syntax: 'other1')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IParameterReferenceOperation: other1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'other1')
  Max: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int16, IsImplicit) (Syntax: 'other2')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IParameterReferenceOperation: other2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'other2')
]]>.Value)

            Dim expectedGraph =
            <![CDATA[
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: C) (Syntax: 'input')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (0)
            Jump if True (Regular) to Block[B4]
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Boolean, IsImplicit) (Syntax: 'other1 To other2')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (NarrowingValue)
                  Operand: 
                    IBinaryOperation (BinaryOperatorKind.GreaterThanOrEqual, Checked) (OperatorMethod: Function C.op_GreaterThanOrEqual(x As C, y As System.Int16) As System.Object) (OperationKind.Binary, Type: System.Object, IsImplicit) (Syntax: 'other1 To other2')
                      Left: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'input')
                      Right: 
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int16, IsImplicit) (Syntax: 'other1')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            (NarrowingNumeric)
                          Operand: 
                            IParameterReferenceOperation: other1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'other1')

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'other1 To other2')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False, IsImplicit) (Syntax: 'other1 To other2')

            Next (Regular) Block[B5]
        Block[B4] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'other1 To other2')
                  Value: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Boolean, IsImplicit) (Syntax: 'other1 To other2')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (NarrowingValue)
                      Operand: 
                        IBinaryOperation (BinaryOperatorKind.LessThanOrEqual, Checked) (OperatorMethod: Function C.op_LessThanOrEqual(x As C, y As System.Int16) As System.Object) (OperationKind.Binary, Type: System.Object, IsImplicit) (Syntax: 'other1 To other2')
                          Left: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'input')
                          Right: 
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int16, IsImplicit) (Syntax: 'other2')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                (NarrowingNumeric)
                              Operand: 
                                IParameterReferenceOperation: other2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'other2')

            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (0)
            Jump if False (Regular) to Block[B7]
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Boolean, IsImplicit) (Syntax: 'other1 To other2')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (NarrowingValue)
                  Operand: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'other1 To other2')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (WideningValue)
                      Operand: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Boolean, IsImplicit) (Syntax: 'other1 To other2')
                Leaving: {R2} {R1}

            Next (Regular) Block[B6]
                Leaving: {R2}
    }

    Block[B6] - Block
        Predecessors: [B5]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = False')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = False')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'False')

        Next (Regular) Block[B7]
            Leaving: {R1}
}

Block[B7] - Exit
    Predecessors: [B5] [B6]
    Statements (0)
]]>.Value

            VerifyFlowGraphForTest(Of MethodBlockSyntax)(compilation, expectedGraph)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_089()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result As Boolean, input1 As C, input2 As C, other as Integer) 'BIND:"Sub M"
        Select Case other
            Case input1 To input2
                result = False
        End Select
    End Sub

    Public Shared Operator >=(y As Short, x As C) As Boolean
        Return False
    End Operator

    Public Shared Operator <=(y As Short, x As C) As Boolean
        Return False
    End Operator
End Class
         ]]>
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source)

            compilation.AssertNoDiagnostics()

            Dim tree = compilation.SyntaxTrees.Single()
            Dim node = tree.GetRoot().DescendantNodes().OfType(Of RangeCaseClauseSyntax)().Single()

            compilation.VerifyOperationTree(node, expectedOperationTree:=
            <![CDATA[
IRangeCaseClauseOperation (CaseKind.Range) (OperationKind.CaseClause, Type: null) (Syntax: 'input1 To input2')
  Min: 
    IParameterReferenceOperation: input1 (OperationKind.ParameterReference, Type: C) (Syntax: 'input1')
  Max: 
    IParameterReferenceOperation: input2 (OperationKind.ParameterReference, Type: C) (Syntax: 'input2')
]]>.Value)

            Dim expectedGraph =
            <![CDATA[
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'other')
              Value: 
                IParameterReferenceOperation: other (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'other')

        Jump if False (Regular) to Block[B4]
            IBinaryOperation (BinaryOperatorKind.GreaterThanOrEqual, Checked) (OperatorMethod: Function C.op_GreaterThanOrEqual(y As System.Int16, x As C) As System.Boolean) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: 'input1 To input2')
              Left: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int16, IsImplicit) (Syntax: 'other')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (NarrowingNumeric)
                  Operand: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'other')
              Right: 
                IParameterReferenceOperation: input1 (OperationKind.ParameterReference, Type: C) (Syntax: 'input1')
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (0)
        Jump if False (Regular) to Block[B4]
            IBinaryOperation (BinaryOperatorKind.LessThanOrEqual, Checked) (OperatorMethod: Function C.op_LessThanOrEqual(y As System.Int16, x As C) As System.Boolean) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: 'input1 To input2')
              Left: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int16, IsImplicit) (Syntax: 'other')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (NarrowingNumeric)
                  Operand: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'other')
              Right: 
                IParameterReferenceOperation: input2 (OperationKind.ParameterReference, Type: C) (Syntax: 'input2')
            Leaving: {R1}

        Next (Regular) Block[B3]
    Block[B3] - Block
        Predecessors: [B2]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = False')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = False')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'False')

        Next (Regular) Block[B4]
            Leaving: {R1}
}

Block[B4] - Exit
    Predecessors: [B1] [B2] [B3]
    Statements (0)
]]>.Value

            VerifyFlowGraphForTest(Of MethodBlockSyntax)(compilation, expectedGraph)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_090()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result As Boolean, input As C) 'BIND:"Sub M"
        Select Case input
            Case 1 To 3
                result = False
        End Select
    End Sub

    Public Shared Widening Operator CType(x As Integer) As C
        Return Nothing
    End Operator
End Class
         ]]>
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source)

            compilation.AssertTheseDiagnostics(
<expected><![CDATA[
BC30452: Operator '<=' is not defined for types 'C' and 'Integer'.
            Case 1 To 3
                 ~~~~~~
BC30452: Operator '>=' is not defined for types 'C' and 'Integer'.
            Case 1 To 3
                 ~~~~~~
]]></expected>)

            Dim tree = compilation.SyntaxTrees.Single()
            Dim node = tree.GetRoot().DescendantNodes().OfType(Of RangeCaseClauseSyntax)().Single()

            compilation.VerifyOperationTree(node, expectedOperationTree:=
            <![CDATA[
IRangeCaseClauseOperation (CaseKind.Range) (OperationKind.CaseClause, Type: null, IsInvalid) (Syntax: '1 To 3')
  Min: 
    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
  Max: 
    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3, IsInvalid) (Syntax: '3')
]]>.Value)

            Dim expectedGraph =
            <![CDATA[
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: C) (Syntax: 'input')

        Jump if False (Regular) to Block[B3]
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: '1 To 3')
              Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                (DelegateRelaxationLevelNone)
              Operand: 
                IBinaryOperation (BinaryOperatorKind.ConditionalAnd, Checked) (OperationKind.Binary, Type: ?, IsInvalid, IsImplicit) (Syntax: '1 To 3')
                  Left: 
                    IBinaryOperation (BinaryOperatorKind.GreaterThanOrEqual, Checked) (OperationKind.Binary, Type: ?, IsInvalid, IsImplicit) (Syntax: '1 To 3')
                      Left: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'input')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
                  Right: 
                    IBinaryOperation (BinaryOperatorKind.LessThanOrEqual, Checked) (OperationKind.Binary, Type: ?, IsInvalid, IsImplicit) (Syntax: '1 To 3')
                      Left: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'input')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3, IsInvalid) (Syntax: '3')
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = False')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = False')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'False')

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphForTest(Of MethodBlockSyntax)(compilation, expectedGraph)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_091()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result As Boolean, input As C, other1 As C, other2 As C) 'BIND:"Sub M"
        Select Case input
            Case other1 To other2
                result = False
        End Select
    End Sub

    Public Shared Operator >=(x As C, y As C) As C
        Return Nothing
    End Operator

    Public Shared Operator <=(x As C, y As C) As C
        Return Nothing
    End Operator

    Public Shared Operator And(x As C, y As C) As C
        Return Nothing
    End Operator

    Public Shared Operator IsTrue(x As C) As Boolean
        Return True
    End Operator

    Public Shared Operator IsFalse(x As C) As Boolean
        Return False
    End Operator
End Class
        ]]>
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source)

            compilation.AssertNoDiagnostics()

            Dim tree = compilation.SyntaxTrees.Single()
            Dim node = tree.GetRoot().DescendantNodes().OfType(Of RangeCaseClauseSyntax)().Single()

            compilation.VerifyOperationTree(node, expectedOperationTree:=
            <![CDATA[
IRangeCaseClauseOperation (CaseKind.Range) (OperationKind.CaseClause, Type: null) (Syntax: 'other1 To other2')
  Min: 
    IParameterReferenceOperation: other1 (OperationKind.ParameterReference, Type: C) (Syntax: 'other1')
  Max: 
    IParameterReferenceOperation: other2 (OperationKind.ParameterReference, Type: C) (Syntax: 'other2')
]]>.Value)

            Dim expectedGraph =
            <![CDATA[
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: C) (Syntax: 'input')

        Next (Regular) Block[B2]
            Entering: {R2} {R3}

    .locals {R2}
    {
        CaptureIds: [2]
        .locals {R3}
        {
            CaptureIds: [1]
            Block[B2] - Block
                Predecessors: [B1]
                Statements (1)
                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'other1 To other2')
                      Value: 
                        IBinaryOperation (BinaryOperatorKind.GreaterThanOrEqual, Checked) (OperatorMethod: Function C.op_GreaterThanOrEqual(x As C, y As C) As C) (OperationKind.Binary, Type: C, IsImplicit) (Syntax: 'other1 To other2')
                          Left: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'input')
                          Right: 
                            IParameterReferenceOperation: other1 (OperationKind.ParameterReference, Type: C) (Syntax: 'other1')

                Jump if False (Regular) to Block[B4]
                    IUnaryOperation (UnaryOperatorKind.False) (OperatorMethod: Function C.op_False(x As C) As System.Boolean) (OperationKind.Unary, Type: System.Boolean, IsImplicit) (Syntax: 'other1 To other2')
                      Operand: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'other1 To other2')

                Next (Regular) Block[B3]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'other1 To other2')
                      Value: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'other1 To other2')

                Next (Regular) Block[B5]
                    Leaving: {R3}
            Block[B4] - Block
                Predecessors: [B2]
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'other1 To other2')
                      Value: 
                        IBinaryOperation (BinaryOperatorKind.And) (OperatorMethod: Function C.op_BitwiseAnd(x As C, y As C) As C) (OperationKind.Binary, Type: C, IsImplicit) (Syntax: 'other1 To other2')
                          Left: 
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'other1 To other2')
                          Right: 
                            IBinaryOperation (BinaryOperatorKind.LessThanOrEqual, Checked) (OperatorMethod: Function C.op_LessThanOrEqual(x As C, y As C) As C) (OperationKind.Binary, Type: C, IsImplicit) (Syntax: 'other1 To other2')
                              Left: 
                                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'input')
                              Right: 
                                IParameterReferenceOperation: other2 (OperationKind.ParameterReference, Type: C) (Syntax: 'other2')

                Next (Regular) Block[B5]
                    Leaving: {R3}
        }

        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (0)
            Jump if False (Regular) to Block[B7]
                IUnaryOperation (UnaryOperatorKind.True) (OperatorMethod: Function C.op_True(x As C) As System.Boolean) (OperationKind.Unary, Type: System.Boolean, IsImplicit) (Syntax: 'other1 To other2')
                  Operand: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'other1 To other2')
                Leaving: {R2} {R1}

            Next (Regular) Block[B6]
                Leaving: {R2}
    }

    Block[B6] - Block
        Predecessors: [B5]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = False')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = False')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'False')

        Next (Regular) Block[B7]
            Leaving: {R1}
}

Block[B7] - Exit
    Predecessors: [B5] [B6]
    Statements (0)
]]>.Value

            VerifyFlowGraphForTest(Of MethodBlockSyntax)(compilation, expectedGraph)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_092()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result As Boolean, input As C, other1 As C, other2 As C) 'BIND:"Sub M"
        Select Case input
            Case other1 To other2
                result = False
        End Select
    End Sub

    Public Shared Operator >=(x As C, y As C) As C
        Return Nothing
    End Operator

    Public Shared Operator <=(x As C, y As C) As C
        Return Nothing
    End Operator

    Public Shared Operator IsTrue(x As C) As Boolean
        Return True
    End Operator

    Public Shared Operator IsFalse(x As C) As Boolean
        Return False
    End Operator
End Class
        ]]>
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source)

            compilation.AssertTheseDiagnostics(
<expected>
BC30452: Operator 'AndAlso' is not defined for types 'C' and 'C'.
            Case other1 To other2
                 ~~~~~~~~~~~~~~~~
</expected>)

            Dim tree = compilation.SyntaxTrees.Single()
            Dim node = tree.GetRoot().DescendantNodes().OfType(Of RangeCaseClauseSyntax)().Single()

            compilation.VerifyOperationTree(node, expectedOperationTree:=
            <![CDATA[
IRangeCaseClauseOperation (CaseKind.Range) (OperationKind.CaseClause, Type: null, IsInvalid) (Syntax: 'other1 To other2')
  Min: 
    IParameterReferenceOperation: other1 (OperationKind.ParameterReference, Type: C, IsInvalid) (Syntax: 'other1')
  Max: 
    IParameterReferenceOperation: other2 (OperationKind.ParameterReference, Type: C, IsInvalid) (Syntax: 'other2')
]]>.Value)

            Dim expectedGraph =
            <![CDATA[
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: C) (Syntax: 'input')

        Jump if False (Regular) to Block[B3]
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'other1 To other2')
              Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                (DelegateRelaxationLevelNone)
              Operand: 
                IBinaryOperation (BinaryOperatorKind.ConditionalAnd, Checked) (OperationKind.Binary, Type: ?, IsInvalid, IsImplicit) (Syntax: 'other1 To other2')
                  Left: 
                    IBinaryOperation (BinaryOperatorKind.GreaterThanOrEqual, Checked) (OperatorMethod: Function C.op_GreaterThanOrEqual(x As C, y As C) As C) (OperationKind.Binary, Type: C, IsInvalid, IsImplicit) (Syntax: 'other1 To other2')
                      Left: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'input')
                      Right: 
                        IParameterReferenceOperation: other1 (OperationKind.ParameterReference, Type: C, IsInvalid) (Syntax: 'other1')
                  Right: 
                    IBinaryOperation (BinaryOperatorKind.LessThanOrEqual, Checked) (OperatorMethod: Function C.op_LessThanOrEqual(x As C, y As C) As C) (OperationKind.Binary, Type: C, IsInvalid, IsImplicit) (Syntax: 'other1 To other2')
                      Left: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'input')
                      Right: 
                        IParameterReferenceOperation: other2 (OperationKind.ParameterReference, Type: C, IsInvalid) (Syntax: 'other2')
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = False')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = False')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'False')

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphForTest(Of MethodBlockSyntax)(compilation, expectedGraph)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_093()
            Dim source = <![CDATA[
Option Compare Text
Imports System
Public NotInheritable Class C
    Sub M(result as Boolean, input as String, other1 As String, other2 As String) 'BIND:"Sub M"
        Select Case input
            Case other1 To other2
                result = false
        End Select
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.String) (Syntax: 'input')

        Jump if False (Regular) to Block[B4]
            IBinaryOperation (BinaryOperatorKind.GreaterThanOrEqual, Checked, CompareText) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: 'other1 To other2')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'input')
              Right: 
                IParameterReferenceOperation: other1 (OperationKind.ParameterReference, Type: System.String) (Syntax: 'other1')
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (0)
        Jump if False (Regular) to Block[B4]
            IBinaryOperation (BinaryOperatorKind.LessThanOrEqual, Checked, CompareText) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: 'other1 To other2')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'input')
              Right: 
                IParameterReferenceOperation: other2 (OperationKind.ParameterReference, Type: System.String) (Syntax: 'other2')
            Leaving: {R1}

        Next (Regular) Block[B3]
    Block[B3] - Block
        Predecessors: [B2]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = false')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B4]
            Leaving: {R1}
}

Block[B4] - Exit
    Predecessors: [B1] [B2] [B3]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_094()
            Dim source = <![CDATA[
Option Compare Binary
Imports System
Public NotInheritable Class C
    Sub M(result as Boolean, input as String, other1 As String, other2 As String) 'BIND:"Sub M"
        Select Case input
            Case other1 To other2
                result = false
        End Select
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.String) (Syntax: 'input')

        Jump if False (Regular) to Block[B4]
            IBinaryOperation (BinaryOperatorKind.GreaterThanOrEqual, Checked) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: 'other1 To other2')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'input')
              Right: 
                IParameterReferenceOperation: other1 (OperationKind.ParameterReference, Type: System.String) (Syntax: 'other1')
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (0)
        Jump if False (Regular) to Block[B4]
            IBinaryOperation (BinaryOperatorKind.LessThanOrEqual, Checked) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: 'other1 To other2')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'input')
              Right: 
                IParameterReferenceOperation: other2 (OperationKind.ParameterReference, Type: System.String) (Syntax: 'other2')
            Leaving: {R1}

        Next (Regular) Block[B3]
    Block[B3] - Block
        Predecessors: [B2]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = false')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B4]
            Leaving: {R1}
}

Block[B4] - Exit
    Predecessors: [B1] [B2] [B3]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_095()
            Dim source = <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result As Boolean, other1 As C, other2 As C, input as Integer) 'BIND:"Sub M"
        Select Case input
            Case 1 To 3, other1 To other2
                result = False
        End Select
    End Sub

    Public Shared Operator <=(y As Integer, x As C) As Boolean
        Return False
    End Operator

    Public Shared Operator >=(y As integer, x As C) As Boolean
        Return True
    End Operator
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'input')

        Jump if False (Regular) to Block[B3]
            IBinaryOperation (BinaryOperatorKind.GreaterThanOrEqual, Checked) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '1 To 3')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (0)
        Jump if True (Regular) to Block[B5]
            IBinaryOperation (BinaryOperatorKind.LessThanOrEqual, Checked) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '1 To 3')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')

        Next (Regular) Block[B3]
    Block[B3] - Block
        Predecessors: [B1] [B2]
        Statements (0)
        Jump if False (Regular) to Block[B6]
            IBinaryOperation (BinaryOperatorKind.GreaterThanOrEqual, Checked) (OperatorMethod: Function C.op_GreaterThanOrEqual(y As System.Int32, x As C) As System.Boolean) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: 'other1 To other2')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input')
              Right: 
                IParameterReferenceOperation: other1 (OperationKind.ParameterReference, Type: C) (Syntax: 'other1')
            Leaving: {R1}

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B3]
        Statements (0)
        Jump if False (Regular) to Block[B6]
            IBinaryOperation (BinaryOperatorKind.LessThanOrEqual, Checked) (OperatorMethod: Function C.op_LessThanOrEqual(y As System.Int32, x As C) As System.Boolean) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: 'other1 To other2')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input')
              Right: 
                IParameterReferenceOperation: other2 (OperationKind.ParameterReference, Type: C) (Syntax: 'other2')
            Leaving: {R1}

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B2] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = False')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = False')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'False')

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Exit
    Predecessors: [B3] [B4] [B5]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_096()
            Dim source = <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result As Boolean, other1 As C, other2 As C, input as Object) 'BIND:"Sub M"
        Select Case input
            Case other1 To other2
                result = False
        End Select
    End Sub

    Public Shared Operator >=(y As Integer, x As C) As Boolean
        Return False
    End Operator

    Public Shared Operator <=(y As integer, x As C) As Boolean
        Return True
    End Operator
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'input')

        Jump if False (Regular) to Block[B4]
            IBinaryOperation (BinaryOperatorKind.GreaterThanOrEqual, Checked) (OperatorMethod: Function C.op_GreaterThanOrEqual(y As System.Int32, x As C) As System.Boolean) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: 'other1 To other2')
              Left: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsImplicit) (Syntax: 'input')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (NarrowingValue)
                  Operand: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'input')
              Right: 
                IParameterReferenceOperation: other1 (OperationKind.ParameterReference, Type: C) (Syntax: 'other1')
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (0)
        Jump if False (Regular) to Block[B4]
            IBinaryOperation (BinaryOperatorKind.LessThanOrEqual, Checked) (OperatorMethod: Function C.op_LessThanOrEqual(y As System.Int32, x As C) As System.Boolean) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: 'other1 To other2')
              Left: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsImplicit) (Syntax: 'input')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (NarrowingValue)
                  Operand: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'input')
              Right: 
                IParameterReferenceOperation: other2 (OperationKind.ParameterReference, Type: C) (Syntax: 'other2')
            Leaving: {R1}

        Next (Regular) Block[B3]
    Block[B3] - Block
        Predecessors: [B2]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = False')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = False')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'False')

        Next (Regular) Block[B4]
            Leaving: {R1}
}

Block[B4] - Exit
    Predecessors: [B1] [B2] [B3]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_097()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(result As Boolean, other1 As C, other2 As C, input as Object) 'BIND:"Sub M"
        Select Case input
            Case other1 To other2
                result = False
        End Select
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'input')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (0)
            Jump if True (Regular) to Block[B4]
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Boolean, IsImplicit) (Syntax: 'other1 To other2')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (NarrowingValue)
                  Operand: 
                    IBinaryOperation (BinaryOperatorKind.GreaterThanOrEqual, Checked) (OperationKind.Binary, Type: System.Object, IsImplicit) (Syntax: 'other1 To other2')
                      Left: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'input')
                      Right: 
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'other1')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                            (WideningReference)
                          Operand: 
                            IParameterReferenceOperation: other1 (OperationKind.ParameterReference, Type: C) (Syntax: 'other1')

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'other1 To other2')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False, IsImplicit) (Syntax: 'other1 To other2')

            Next (Regular) Block[B5]
        Block[B4] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'other1 To other2')
                  Value: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Boolean, IsImplicit) (Syntax: 'other1 To other2')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (NarrowingValue)
                      Operand: 
                        IBinaryOperation (BinaryOperatorKind.LessThanOrEqual, Checked) (OperationKind.Binary, Type: System.Object, IsImplicit) (Syntax: 'other1 To other2')
                          Left: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'input')
                          Right: 
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'other2')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                                (WideningReference)
                              Operand: 
                                IParameterReferenceOperation: other2 (OperationKind.ParameterReference, Type: C) (Syntax: 'other2')

            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (0)
            Jump if False (Regular) to Block[B7]
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Boolean, IsImplicit) (Syntax: 'other1 To other2')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (NarrowingValue)
                  Operand: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'other1 To other2')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (WideningValue)
                      Operand: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Boolean, IsImplicit) (Syntax: 'other1 To other2')
                Leaving: {R2} {R1}

            Next (Regular) Block[B6]
                Leaving: {R2}
    }

    Block[B6] - Block
        Predecessors: [B5]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = False')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = False')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'False')

        Next (Regular) Block[B7]
            Leaving: {R1}
}

Block[B7] - Exit
    Predecessors: [B5] [B6]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_098()
            Dim source = <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result As Boolean, other1 As Integer, other2 As Integer, input as Object) 'BIND:"Sub M"
        Select Case input
            Case other1 To other2
                result = False
        End Select
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'input')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (0)
            Jump if True (Regular) to Block[B4]
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Boolean, IsImplicit) (Syntax: 'other1 To other2')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (NarrowingValue)
                  Operand: 
                    IBinaryOperation (BinaryOperatorKind.GreaterThanOrEqual, Checked) (OperationKind.Binary, Type: System.Object, IsImplicit) (Syntax: 'other1 To other2')
                      Left: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'input')
                      Right: 
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'other1')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            (WideningValue)
                          Operand: 
                            IParameterReferenceOperation: other1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'other1')

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'other1 To other2')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False, IsImplicit) (Syntax: 'other1 To other2')

            Next (Regular) Block[B5]
        Block[B4] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'other1 To other2')
                  Value: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Boolean, IsImplicit) (Syntax: 'other1 To other2')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (NarrowingValue)
                      Operand: 
                        IBinaryOperation (BinaryOperatorKind.LessThanOrEqual, Checked) (OperationKind.Binary, Type: System.Object, IsImplicit) (Syntax: 'other1 To other2')
                          Left: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'input')
                          Right: 
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'other2')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                (WideningValue)
                              Operand: 
                                IParameterReferenceOperation: other2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'other2')

            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (0)
            Jump if False (Regular) to Block[B7]
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Boolean, IsImplicit) (Syntax: 'other1 To other2')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (NarrowingValue)
                  Operand: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'other1 To other2')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (WideningValue)
                      Operand: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Boolean, IsImplicit) (Syntax: 'other1 To other2')
                Leaving: {R2} {R1}

            Next (Regular) Block[B6]
                Leaving: {R2}
    }

    Block[B6] - Block
        Predecessors: [B5]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = False')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = False')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'False')

        Next (Regular) Block[B7]
            Leaving: {R1}
}

Block[B7] - Exit
    Predecessors: [B5] [B6]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_099()
            Dim source = <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result As Boolean, input As Integer, other1 as Object, other2 as Object) 'BIND:"Sub M"
        Select Case input
            Case other1 To other2
                result = False
        End Select
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'input')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (0)
            Jump if True (Regular) to Block[B4]
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Boolean, IsImplicit) (Syntax: 'other1 To other2')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (NarrowingValue)
                  Operand: 
                    IBinaryOperation (BinaryOperatorKind.GreaterThanOrEqual, Checked) (OperationKind.Binary, Type: System.Object, IsImplicit) (Syntax: 'other1 To other2')
                      Left: 
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'input')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            (WideningValue)
                          Operand: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input')
                      Right: 
                        IParameterReferenceOperation: other1 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'other1')

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'other1 To other2')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False, IsImplicit) (Syntax: 'other1 To other2')

            Next (Regular) Block[B5]
        Block[B4] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'other1 To other2')
                  Value: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Boolean, IsImplicit) (Syntax: 'other1 To other2')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (NarrowingValue)
                      Operand: 
                        IBinaryOperation (BinaryOperatorKind.LessThanOrEqual, Checked) (OperationKind.Binary, Type: System.Object, IsImplicit) (Syntax: 'other1 To other2')
                          Left: 
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'input')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                (WideningValue)
                              Operand: 
                                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input')
                          Right: 
                            IParameterReferenceOperation: other2 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'other2')

            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (0)
            Jump if False (Regular) to Block[B7]
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Boolean, IsImplicit) (Syntax: 'other1 To other2')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (NarrowingValue)
                  Operand: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'other1 To other2')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (WideningValue)
                      Operand: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Boolean, IsImplicit) (Syntax: 'other1 To other2')
                Leaving: {R2} {R1}

            Next (Regular) Block[B6]
                Leaving: {R2}
    }

    Block[B6] - Block
        Predecessors: [B5]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = False')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = False')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'False')

        Next (Regular) Block[B7]
            Leaving: {R1}
}

Block[B7] - Exit
    Predecessors: [B5] [B6]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_100()
            Dim source = <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result As Boolean, input As Object, other1 as Object, other2 as Object) 'BIND:"Sub M"
        Select Case input
            Case other1 To other2
                result = False
        End Select
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'input')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (0)
            Jump if True (Regular) to Block[B4]
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Boolean, IsImplicit) (Syntax: 'other1 To other2')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (NarrowingValue)
                  Operand: 
                    IBinaryOperation (BinaryOperatorKind.GreaterThanOrEqual, Checked) (OperationKind.Binary, Type: System.Object, IsImplicit) (Syntax: 'other1 To other2')
                      Left: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'input')
                      Right: 
                        IParameterReferenceOperation: other1 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'other1')

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'other1 To other2')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False, IsImplicit) (Syntax: 'other1 To other2')

            Next (Regular) Block[B5]
        Block[B4] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'other1 To other2')
                  Value: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Boolean, IsImplicit) (Syntax: 'other1 To other2')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (NarrowingValue)
                      Operand: 
                        IBinaryOperation (BinaryOperatorKind.LessThanOrEqual, Checked) (OperationKind.Binary, Type: System.Object, IsImplicit) (Syntax: 'other1 To other2')
                          Left: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'input')
                          Right: 
                            IParameterReferenceOperation: other2 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'other2')

            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (0)
            Jump if False (Regular) to Block[B7]
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Boolean, IsImplicit) (Syntax: 'other1 To other2')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (NarrowingValue)
                  Operand: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'other1 To other2')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (WideningValue)
                      Operand: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Boolean, IsImplicit) (Syntax: 'other1 To other2')
                Leaving: {R2} {R1}

            Next (Regular) Block[B6]
                Leaving: {R2}
    }

    Block[B6] - Block
        Predecessors: [B5]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = False')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = False')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'False')

        Next (Regular) Block[B7]
            Leaving: {R1}
}

Block[B7] - Exit
    Predecessors: [B5] [B6]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_101()
            Dim source = <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result As Boolean, input As C, other1 As C, other2 As C, other3 As C, other4 As C) 'BIND:"Sub M"
        Select Case input
            Case other1 To other2, other3 To other4
                result = False
        End Select
    End Sub

    Public Shared Operator >=(x As C, y As C) As C
        Return Nothing
    End Operator

    Public Shared Operator <=(x As C, y As C) As C
        Return Nothing
    End Operator

    Public Shared Operator IsTrue(x As C) As Boolean
        Return True
    End Operator

    Public Shared Operator IsFalse(x As C) As Boolean
        Return False
    End Operator

    Public Shared Operator And(x As C, y As C) As C
        Return Nothing
    End Operator
End Class
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30452: Operator 'OrElse' is not defined for types 'C' and 'C'.
            Case other1 To other2, other3 To other4
            ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
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
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: C) (Syntax: 'input')

        Next (Regular) Block[B2]
            Entering: {R2} {R3}

    .locals {R2}
    {
        CaptureIds: [2] [4]
        .locals {R3}
        {
            CaptureIds: [1]
            Block[B2] - Block
                Predecessors: [B1]
                Statements (1)
                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'other1 To other2')
                      Value: 
                        IBinaryOperation (BinaryOperatorKind.GreaterThanOrEqual, Checked) (OperatorMethod: Function C.op_GreaterThanOrEqual(x As C, y As C) As C) (OperationKind.Binary, Type: C, IsInvalid, IsImplicit) (Syntax: 'other1 To other2')
                          Left: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'input')
                          Right: 
                            IParameterReferenceOperation: other1 (OperationKind.ParameterReference, Type: C, IsInvalid) (Syntax: 'other1')

                Jump if False (Regular) to Block[B4]
                    IUnaryOperation (UnaryOperatorKind.False) (OperatorMethod: Function C.op_False(x As C) As System.Boolean) (OperationKind.Unary, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'other1 To other2')
                      Operand: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'other1 To other2')

                Next (Regular) Block[B3]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'other1 To other2')
                      Value: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'other1 To other2')

                Next (Regular) Block[B5]
                    Leaving: {R3}
                    Entering: {R4}
            Block[B4] - Block
                Predecessors: [B2]
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'other1 To other2')
                      Value: 
                        IBinaryOperation (BinaryOperatorKind.And) (OperatorMethod: Function C.op_BitwiseAnd(x As C, y As C) As C) (OperationKind.Binary, Type: C, IsInvalid, IsImplicit) (Syntax: 'other1 To other2')
                          Left: 
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'other1 To other2')
                          Right: 
                            IBinaryOperation (BinaryOperatorKind.LessThanOrEqual, Checked) (OperatorMethod: Function C.op_LessThanOrEqual(x As C, y As C) As C) (OperationKind.Binary, Type: C, IsInvalid, IsImplicit) (Syntax: 'other1 To other2')
                              Left: 
                                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'input')
                              Right: 
                                IParameterReferenceOperation: other2 (OperationKind.ParameterReference, Type: C, IsInvalid) (Syntax: 'other2')

                Next (Regular) Block[B5]
                    Leaving: {R3}
                    Entering: {R4}
        }
        .locals {R4}
        {
            CaptureIds: [3]
            Block[B5] - Block
                Predecessors: [B3] [B4]
                Statements (1)
                    IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'other3 To other4')
                      Value: 
                        IBinaryOperation (BinaryOperatorKind.GreaterThanOrEqual, Checked) (OperatorMethod: Function C.op_GreaterThanOrEqual(x As C, y As C) As C) (OperationKind.Binary, Type: C, IsInvalid, IsImplicit) (Syntax: 'other3 To other4')
                          Left: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'input')
                          Right: 
                            IParameterReferenceOperation: other3 (OperationKind.ParameterReference, Type: C, IsInvalid) (Syntax: 'other3')

                Jump if False (Regular) to Block[B7]
                    IUnaryOperation (UnaryOperatorKind.False) (OperatorMethod: Function C.op_False(x As C) As System.Boolean) (OperationKind.Unary, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'other3 To other4')
                      Operand: 
                        IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'other3 To other4')

                Next (Regular) Block[B6]
            Block[B6] - Block
                Predecessors: [B5]
                Statements (1)
                    IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'other3 To other4')
                      Value: 
                        IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'other3 To other4')

                Next (Regular) Block[B8]
                    Leaving: {R4}
            Block[B7] - Block
                Predecessors: [B5]
                Statements (1)
                    IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'other3 To other4')
                      Value: 
                        IBinaryOperation (BinaryOperatorKind.And) (OperatorMethod: Function C.op_BitwiseAnd(x As C, y As C) As C) (OperationKind.Binary, Type: C, IsInvalid, IsImplicit) (Syntax: 'other3 To other4')
                          Left: 
                            IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'other3 To other4')
                          Right: 
                            IBinaryOperation (BinaryOperatorKind.LessThanOrEqual, Checked) (OperatorMethod: Function C.op_LessThanOrEqual(x As C, y As C) As C) (OperationKind.Binary, Type: C, IsInvalid, IsImplicit) (Syntax: 'other3 To other4')
                              Left: 
                                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'input')
                              Right: 
                                IParameterReferenceOperation: other4 (OperationKind.ParameterReference, Type: C, IsInvalid) (Syntax: 'other4')

                Next (Regular) Block[B8]
                    Leaving: {R4}
        }

        Block[B8] - Block
            Predecessors: [B6] [B7]
            Statements (0)
            Jump if False (Regular) to Block[B10]
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'Case other1 ... 3 To other4')
                  Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (DelegateRelaxationLevelNone)
                  Operand: 
                    IBinaryOperation (BinaryOperatorKind.ConditionalOr, Checked) (OperationKind.Binary, Type: ?, IsInvalid, IsImplicit) (Syntax: 'Case other1 ... 3 To other4')
                      Left: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'other1 To other2')
                      Right: 
                        IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'other3 To other4')
                Leaving: {R2} {R1}

            Next (Regular) Block[B9]
                Leaving: {R2}
    }

    Block[B9] - Block
        Predecessors: [B8]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = False')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = False')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'False')

        Next (Regular) Block[B10]
            Leaving: {R1}
}

Block[B10] - Exit
    Predecessors: [B8] [B9]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_102()
            Dim source = <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result As Boolean, input As C, other1 As C, other2 As C, other3 As C, other4 As C) 'BIND:"Sub M"
        Select Case input
            Case other1 To other2, other3 To other4
                result = False
        End Select
    End Sub

    Public Shared Operator >=(x As C, y As C) As C
        Return Nothing
    End Operator

    Public Shared Operator <=(x As C, y As C) As C
        Return Nothing
    End Operator

    Public Shared Operator IsTrue(x As C) As Boolean
        Return True
    End Operator

    Public Shared Operator IsFalse(x As C) As Boolean
        Return False
    End Operator

    Public Shared Operator And(x As C, y As C) As C
        Return Nothing
    End Operator

    Public Shared Operator Or(x As C, y As C) As C
        Return Nothing
    End Operator
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: C) (Syntax: 'input')

        Next (Regular) Block[B2]
            Entering: {R2} {R3} {R4}

    .locals {R2}
    {
        CaptureIds: [3]
        .locals {R3}
        {
            CaptureIds: [2]
            .locals {R4}
            {
                CaptureIds: [1]
                Block[B2] - Block
                    Predecessors: [B1]
                    Statements (1)
                        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'other1 To other2')
                          Value: 
                            IBinaryOperation (BinaryOperatorKind.GreaterThanOrEqual, Checked) (OperatorMethod: Function C.op_GreaterThanOrEqual(x As C, y As C) As C) (OperationKind.Binary, Type: C, IsImplicit) (Syntax: 'other1 To other2')
                              Left: 
                                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'input')
                              Right: 
                                IParameterReferenceOperation: other1 (OperationKind.ParameterReference, Type: C) (Syntax: 'other1')

                    Jump if False (Regular) to Block[B4]
                        IUnaryOperation (UnaryOperatorKind.False) (OperatorMethod: Function C.op_False(x As C) As System.Boolean) (OperationKind.Unary, Type: System.Boolean, IsImplicit) (Syntax: 'other1 To other2')
                          Operand: 
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'other1 To other2')

                    Next (Regular) Block[B3]
                Block[B3] - Block
                    Predecessors: [B2]
                    Statements (1)
                        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'other1 To other2')
                          Value: 
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'other1 To other2')

                    Next (Regular) Block[B5]
                        Leaving: {R4}
                Block[B4] - Block
                    Predecessors: [B2]
                    Statements (1)
                        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'other1 To other2')
                          Value: 
                            IBinaryOperation (BinaryOperatorKind.And) (OperatorMethod: Function C.op_BitwiseAnd(x As C, y As C) As C) (OperationKind.Binary, Type: C, IsImplicit) (Syntax: 'other1 To other2')
                              Left: 
                                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'other1 To other2')
                              Right: 
                                IBinaryOperation (BinaryOperatorKind.LessThanOrEqual, Checked) (OperatorMethod: Function C.op_LessThanOrEqual(x As C, y As C) As C) (OperationKind.Binary, Type: C, IsImplicit) (Syntax: 'other1 To other2')
                                  Left: 
                                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'input')
                                  Right: 
                                    IParameterReferenceOperation: other2 (OperationKind.ParameterReference, Type: C) (Syntax: 'other2')

                    Next (Regular) Block[B5]
                        Leaving: {R4}
            }

            Block[B5] - Block
                Predecessors: [B3] [B4]
                Statements (0)
                Jump if False (Regular) to Block[B7]
                    IUnaryOperation (UnaryOperatorKind.True) (OperatorMethod: Function C.op_True(x As C) As System.Boolean) (OperationKind.Unary, Type: System.Boolean, IsImplicit) (Syntax: 'other1 To other2')
                      Operand: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'other1 To other2')
                    Entering: {R5} {R6}

                Next (Regular) Block[B6]
            Block[B6] - Block
                Predecessors: [B5]
                Statements (1)
                    IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'Case other1 ... 3 To other4')
                      Value: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'other1 To other2')

                Next (Regular) Block[B11]
                    Leaving: {R3}

            .locals {R5}
            {
                CaptureIds: [5]
                .locals {R6}
                {
                    CaptureIds: [4]
                    Block[B7] - Block
                        Predecessors: [B5]
                        Statements (1)
                            IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'other3 To other4')
                              Value: 
                                IBinaryOperation (BinaryOperatorKind.GreaterThanOrEqual, Checked) (OperatorMethod: Function C.op_GreaterThanOrEqual(x As C, y As C) As C) (OperationKind.Binary, Type: C, IsImplicit) (Syntax: 'other3 To other4')
                                  Left: 
                                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'input')
                                  Right: 
                                    IParameterReferenceOperation: other3 (OperationKind.ParameterReference, Type: C) (Syntax: 'other3')

                        Jump if False (Regular) to Block[B9]
                            IUnaryOperation (UnaryOperatorKind.False) (OperatorMethod: Function C.op_False(x As C) As System.Boolean) (OperationKind.Unary, Type: System.Boolean, IsImplicit) (Syntax: 'other3 To other4')
                              Operand: 
                                IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'other3 To other4')

                        Next (Regular) Block[B8]
                    Block[B8] - Block
                        Predecessors: [B7]
                        Statements (1)
                            IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'other3 To other4')
                              Value: 
                                IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'other3 To other4')

                        Next (Regular) Block[B10]
                            Leaving: {R6}
                    Block[B9] - Block
                        Predecessors: [B7]
                        Statements (1)
                            IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'other3 To other4')
                              Value: 
                                IBinaryOperation (BinaryOperatorKind.And) (OperatorMethod: Function C.op_BitwiseAnd(x As C, y As C) As C) (OperationKind.Binary, Type: C, IsImplicit) (Syntax: 'other3 To other4')
                                  Left: 
                                    IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'other3 To other4')
                                  Right: 
                                    IBinaryOperation (BinaryOperatorKind.LessThanOrEqual, Checked) (OperatorMethod: Function C.op_LessThanOrEqual(x As C, y As C) As C) (OperationKind.Binary, Type: C, IsImplicit) (Syntax: 'other3 To other4')
                                      Left: 
                                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'input')
                                      Right: 
                                        IParameterReferenceOperation: other4 (OperationKind.ParameterReference, Type: C) (Syntax: 'other4')

                        Next (Regular) Block[B10]
                            Leaving: {R6}
                }

                Block[B10] - Block
                    Predecessors: [B8] [B9]
                    Statements (1)
                        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'Case other1 ... 3 To other4')
                          Value: 
                            IBinaryOperation (BinaryOperatorKind.Or) (OperatorMethod: Function C.op_BitwiseOr(x As C, y As C) As C) (OperationKind.Binary, Type: C, IsImplicit) (Syntax: 'Case other1 ... 3 To other4')
                              Left: 
                                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'other1 To other2')
                              Right: 
                                IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'other3 To other4')

                    Next (Regular) Block[B11]
                        Leaving: {R5} {R3}
            }
        }

        Block[B11] - Block
            Predecessors: [B6] [B10]
            Statements (0)
            Jump if False (Regular) to Block[B13]
                IUnaryOperation (UnaryOperatorKind.True) (OperatorMethod: Function C.op_True(x As C) As System.Boolean) (OperationKind.Unary, Type: System.Boolean, IsImplicit) (Syntax: 'Case other1 ... 3 To other4')
                  Operand: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'Case other1 ... 3 To other4')
                Leaving: {R2} {R1}

            Next (Regular) Block[B12]
                Leaving: {R2}
    }

    Block[B12] - Block
        Predecessors: [B11]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = False')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = False')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'False')

        Next (Regular) Block[B13]
            Leaving: {R1}
}

Block[B13] - Exit
    Predecessors: [B11] [B12]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_103()
            Dim source = <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result as Boolean, input as Integer?) 'BIND:"Sub M"
        Select Case input
            Case 1 To 3
                result = false
        End Select
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'input')

        Next (Regular) Block[B2]
            Entering: {R2} {R3}

    .locals {R2}
    {
        CaptureIds: [2]
        .locals {R3}
        {
            CaptureIds: [1]
            Block[B2] - Block
                Predecessors: [B1]
                Statements (1)
                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '1 To 3')
                      Value: 
                        IBinaryOperation (BinaryOperatorKind.GreaterThanOrEqual, IsLifted, Checked) (OperationKind.Binary, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: '1 To 3')
                          Left: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'input')
                          Right: 
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: '1')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                (WideningNullable)
                              Operand: 
                                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

                Jump if True (Regular) to Block[B5]
                    IInvocationOperation ( Function System.Nullable(Of System.Boolean).GetValueOrDefault() As System.Boolean) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: '1 To 3')
                      Instance Receiver: 
                        IUnaryOperation (UnaryOperatorKind.Not, IsLifted) (OperationKind.Unary, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: '1 To 3')
                          Operand: 
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: '1 To 3')
                      Arguments(0)

                Next (Regular) Block[B3]
                    Entering: {R4}

            .locals {R4}
            {
                CaptureIds: [3]
                Block[B3] - Block
                    Predecessors: [B2]
                    Statements (1)
                        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '1 To 3')
                          Value: 
                            IBinaryOperation (BinaryOperatorKind.LessThanOrEqual, IsLifted, Checked) (OperationKind.Binary, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: '1 To 3')
                              Left: 
                                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'input')
                              Right: 
                                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: '3')
                                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                    (WideningNullable)
                                  Operand: 
                                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')

                    Jump if True (Regular) to Block[B5]
                        IInvocationOperation ( Function System.Nullable(Of System.Boolean).GetValueOrDefault() As System.Boolean) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: '1 To 3')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: '1 To 3')
                          Arguments(0)
                        Leaving: {R4}

                    Next (Regular) Block[B4]
                Block[B4] - Block
                    Predecessors: [B3]
                    Statements (1)
                        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '1 To 3')
                          Value: 
                            IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: '1 To 3')

                    Next (Regular) Block[B6]
                        Leaving: {R4} {R3}
            }

            Block[B5] - Block
                Predecessors: [B2] [B3]
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '1 To 3')
                      Value: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: '1 To 3')

                Next (Regular) Block[B6]
                    Leaving: {R3}
        }

        Block[B6] - Block
            Predecessors: [B4] [B5]
            Statements (0)
            Jump if False (Regular) to Block[B8]
                IInvocationOperation ( Function System.Nullable(Of System.Boolean).GetValueOrDefault() As System.Boolean) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: '1 To 3')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: '1 To 3')
                  Arguments(0)
                Leaving: {R2} {R1}

            Next (Regular) Block[B7]
                Leaving: {R2}
    }

    Block[B7] - Block
        Predecessors: [B6]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = false')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B8]
            Leaving: {R1}
}

Block[B8] - Exit
    Predecessors: [B6] [B7]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_104()
            Dim source = <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result as Boolean, input as Integer?) 'BIND:"Sub M"
        Select Case input
            Case Nothing To 1
                result = false
        End Select
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'input')

        Next (Regular) Block[B2]
            Entering: {R2} {R3}

    .locals {R2}
    {
        CaptureIds: [2]
        .locals {R3}
        {
            CaptureIds: [1]
            Block[B2] - Block
                Predecessors: [B1]
                Statements (1)
                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'Nothing To 1')
                      Value: 
                        IBinaryOperation (BinaryOperatorKind.GreaterThanOrEqual, IsLifted, Checked) (OperationKind.Binary, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: 'Nothing To 1')
                          Left: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'input')
                          Right: 
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'Nothing')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                (WideningNothingLiteral)
                              Operand: 
                                ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'Nothing')

                Jump if True (Regular) to Block[B5]
                    IInvocationOperation ( Function System.Nullable(Of System.Boolean).GetValueOrDefault() As System.Boolean) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'Nothing To 1')
                      Instance Receiver: 
                        IUnaryOperation (UnaryOperatorKind.Not, IsLifted) (OperationKind.Unary, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: 'Nothing To 1')
                          Operand: 
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: 'Nothing To 1')
                      Arguments(0)

                Next (Regular) Block[B3]
                    Entering: {R4}

            .locals {R4}
            {
                CaptureIds: [3]
                Block[B3] - Block
                    Predecessors: [B2]
                    Statements (1)
                        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'Nothing To 1')
                          Value: 
                            IBinaryOperation (BinaryOperatorKind.LessThanOrEqual, IsLifted, Checked) (OperationKind.Binary, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: 'Nothing To 1')
                              Left: 
                                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'input')
                              Right: 
                                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: '1')
                                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                    (WideningNullable)
                                  Operand: 
                                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

                    Jump if True (Regular) to Block[B5]
                        IInvocationOperation ( Function System.Nullable(Of System.Boolean).GetValueOrDefault() As System.Boolean) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'Nothing To 1')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: 'Nothing To 1')
                          Arguments(0)
                        Leaving: {R4}

                    Next (Regular) Block[B4]
                Block[B4] - Block
                    Predecessors: [B3]
                    Statements (1)
                        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'Nothing To 1')
                          Value: 
                            IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: 'Nothing To 1')

                    Next (Regular) Block[B6]
                        Leaving: {R4} {R3}
            }

            Block[B5] - Block
                Predecessors: [B2] [B3]
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'Nothing To 1')
                      Value: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: 'Nothing To 1')

                Next (Regular) Block[B6]
                    Leaving: {R3}
        }

        Block[B6] - Block
            Predecessors: [B4] [B5]
            Statements (0)
            Jump if False (Regular) to Block[B8]
                IInvocationOperation ( Function System.Nullable(Of System.Boolean).GetValueOrDefault() As System.Boolean) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'Nothing To 1')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: 'Nothing To 1')
                  Arguments(0)
                Leaving: {R2} {R1}

            Next (Regular) Block[B7]
                Leaving: {R2}
    }

    Block[B7] - Block
        Predecessors: [B6]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = false')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B8]
            Leaving: {R1}
}

Block[B8] - Exit
    Predecessors: [B6] [B7]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_105()
            Dim source = <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result as Boolean, input as Integer?) 'BIND:"Sub M"
        Select Case input
            Case 1 To 3, 5 To Nothing
                result = false
        End Select
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'input')

        Next (Regular) Block[B2]
            Entering: {R2} {R3} {R4}

    .locals {R2}
    {
        CaptureIds: [4]
        .locals {R3}
        {
            CaptureIds: [2]
            .locals {R4}
            {
                CaptureIds: [1]
                Block[B2] - Block
                    Predecessors: [B1]
                    Statements (1)
                        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '1 To 3')
                          Value: 
                            IBinaryOperation (BinaryOperatorKind.GreaterThanOrEqual, IsLifted, Checked) (OperationKind.Binary, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: '1 To 3')
                              Left: 
                                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'input')
                              Right: 
                                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: '1')
                                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                    (WideningNullable)
                                  Operand: 
                                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

                    Jump if True (Regular) to Block[B5]
                        IInvocationOperation ( Function System.Nullable(Of System.Boolean).GetValueOrDefault() As System.Boolean) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: '1 To 3')
                          Instance Receiver: 
                            IUnaryOperation (UnaryOperatorKind.Not, IsLifted) (OperationKind.Unary, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: '1 To 3')
                              Operand: 
                                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: '1 To 3')
                          Arguments(0)

                    Next (Regular) Block[B3]
                        Entering: {R5}

                .locals {R5}
                {
                    CaptureIds: [3]
                    Block[B3] - Block
                        Predecessors: [B2]
                        Statements (1)
                            IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '1 To 3')
                              Value: 
                                IBinaryOperation (BinaryOperatorKind.LessThanOrEqual, IsLifted, Checked) (OperationKind.Binary, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: '1 To 3')
                                  Left: 
                                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'input')
                                  Right: 
                                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: '3')
                                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        (WideningNullable)
                                      Operand: 
                                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')

                        Jump if True (Regular) to Block[B5]
                            IInvocationOperation ( Function System.Nullable(Of System.Boolean).GetValueOrDefault() As System.Boolean) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: '1 To 3')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: '1 To 3')
                              Arguments(0)
                            Leaving: {R5}

                        Next (Regular) Block[B4]
                    Block[B4] - Block
                        Predecessors: [B3]
                        Statements (1)
                            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '1 To 3')
                              Value: 
                                IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: '1 To 3')

                        Next (Regular) Block[B6]
                            Leaving: {R5} {R4}
                }

                Block[B5] - Block
                    Predecessors: [B2] [B3]
                    Statements (1)
                        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '1 To 3')
                          Value: 
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: '1 To 3')

                    Next (Regular) Block[B6]
                        Leaving: {R4}
            }

            Block[B6] - Block
                Predecessors: [B4] [B5]
                Statements (0)
                Jump if True (Regular) to Block[B13]
                    IInvocationOperation ( Function System.Nullable(Of System.Boolean).GetValueOrDefault() As System.Boolean) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: '1 To 3')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: '1 To 3')
                      Arguments(0)

                Next (Regular) Block[B7]
                    Entering: {R6} {R7}

            .locals {R6}
            {
                CaptureIds: [6]
                .locals {R7}
                {
                    CaptureIds: [5]
                    Block[B7] - Block
                        Predecessors: [B6]
                        Statements (1)
                            IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '5 To Nothing')
                              Value: 
                                IBinaryOperation (BinaryOperatorKind.GreaterThanOrEqual, IsLifted, Checked) (OperationKind.Binary, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: '5 To Nothing')
                                  Left: 
                                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'input')
                                  Right: 
                                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: '5')
                                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        (WideningNullable)
                                      Operand: 
                                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')

                        Jump if True (Regular) to Block[B10]
                            IInvocationOperation ( Function System.Nullable(Of System.Boolean).GetValueOrDefault() As System.Boolean) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: '5 To Nothing')
                              Instance Receiver: 
                                IUnaryOperation (UnaryOperatorKind.Not, IsLifted) (OperationKind.Unary, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: '5 To Nothing')
                                  Operand: 
                                    IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: '5 To Nothing')
                              Arguments(0)

                        Next (Regular) Block[B8]
                            Entering: {R8}

                    .locals {R8}
                    {
                        CaptureIds: [7]
                        Block[B8] - Block
                            Predecessors: [B7]
                            Statements (1)
                                IFlowCaptureOperation: 7 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '5 To Nothing')
                                  Value: 
                                    IBinaryOperation (BinaryOperatorKind.LessThanOrEqual, IsLifted, Checked) (OperationKind.Binary, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: '5 To Nothing')
                                      Left: 
                                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'input')
                                      Right: 
                                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'Nothing')
                                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                            (WideningNothingLiteral)
                                          Operand: 
                                            ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'Nothing')

                            Jump if True (Regular) to Block[B10]
                                IInvocationOperation ( Function System.Nullable(Of System.Boolean).GetValueOrDefault() As System.Boolean) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: '5 To Nothing')
                                  Instance Receiver: 
                                    IFlowCaptureReferenceOperation: 7 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: '5 To Nothing')
                                  Arguments(0)
                                Leaving: {R8}

                            Next (Regular) Block[B9]
                        Block[B9] - Block
                            Predecessors: [B8]
                            Statements (1)
                                IFlowCaptureOperation: 6 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '5 To Nothing')
                                  Value: 
                                    IFlowCaptureReferenceOperation: 7 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: '5 To Nothing')

                            Next (Regular) Block[B11]
                                Leaving: {R8} {R7}
                    }

                    Block[B10] - Block
                        Predecessors: [B7] [B8]
                        Statements (1)
                            IFlowCaptureOperation: 6 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '5 To Nothing')
                              Value: 
                                IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: '5 To Nothing')

                        Next (Regular) Block[B11]
                            Leaving: {R7}
                }

                Block[B11] - Block
                    Predecessors: [B9] [B10]
                    Statements (0)
                    Jump if True (Regular) to Block[B13]
                        IInvocationOperation ( Function System.Nullable(Of System.Boolean).GetValueOrDefault() As System.Boolean) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: '5 To Nothing')
                          Instance Receiver: 
                            IUnaryOperation (UnaryOperatorKind.Not, IsLifted) (OperationKind.Unary, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: '5 To Nothing')
                              Operand: 
                                IFlowCaptureReferenceOperation: 6 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: '5 To Nothing')
                          Arguments(0)
                        Leaving: {R6}

                    Next (Regular) Block[B12]
                Block[B12] - Block
                    Predecessors: [B11]
                    Statements (1)
                        IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'Case 1 To 3 ...  To Nothing')
                          Value: 
                            IFlowCaptureReferenceOperation: 6 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: '5 To Nothing')

                    Next (Regular) Block[B14]
                        Leaving: {R6} {R3}
            }

            Block[B13] - Block
                Predecessors: [B6] [B11]
                Statements (1)
                    IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'Case 1 To 3 ...  To Nothing')
                      Value: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: '1 To 3')

                Next (Regular) Block[B14]
                    Leaving: {R3}
        }

        Block[B14] - Block
            Predecessors: [B12] [B13]
            Statements (0)
            Jump if False (Regular) to Block[B16]
                IInvocationOperation ( Function System.Nullable(Of System.Boolean).GetValueOrDefault() As System.Boolean) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'Case 1 To 3 ...  To Nothing')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: 'Case 1 To 3 ...  To Nothing')
                  Arguments(0)
                Leaving: {R2} {R1}

            Next (Regular) Block[B15]
                Leaving: {R2}
    }

    Block[B15] - Block
        Predecessors: [B14]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = false')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B16]
            Leaving: {R1}
}

Block[B16] - Exit
    Predecessors: [B14] [B15]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_106()
            Dim source = <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result as Boolean, other1 as Object, other2 as Object) 'BIND:"Sub M"
        Select Case Function() 1
            Case other1 To other2
                result = false
        End Select
    End Sub
End Class
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36635: Lambda expressions are not valid in the first expression of a 'Select Case' statement.
        Select Case Function() 1
                    ~~~~~~~~~~~~
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
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'Function() 1')
              Value: 
                IDelegateCreationOperation (OperationKind.DelegateCreation, Type: Function <generated method>() As System.Int32, IsInvalid, IsImplicit) (Syntax: 'Function() 1')
                  Target: 
                    IFlowAnonymousFunctionOperation (Symbol: Function () As System.Int32) (OperationKind.FlowAnonymousFunction, Type: null, IsInvalid) (Syntax: 'Function() 1')
                    {
                        Block[B0#A0] - Entry
                            Statements (0)
                            Next (Regular) Block[B1#A0]
                                Entering: {R1#A0}

                        .locals {R1#A0}
                        {
                            Locals: [<anonymous local> As System.Int32]
                            Block[B1#A0] - Block
                                Predecessors: [B0#A0]
                                Statements (0)
                                Next (Return) Block[B2#A0]
                                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
                                    Leaving: {R1#A0}
                        }

                        Block[B2#A0] - Exit
                            Predecessors: [B1#A0]
                            Statements (0)
                    }

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (0)
            Jump if True (Regular) to Block[B4]
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Boolean, IsImplicit) (Syntax: 'other1 To other2')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (NarrowingValue)
                  Operand: 
                    IBinaryOperation (BinaryOperatorKind.GreaterThanOrEqual, Checked) (OperationKind.Binary, Type: System.Object, IsImplicit) (Syntax: 'other1 To other2')
                      Left: 
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsInvalid, IsImplicit) (Syntax: 'Function() 1')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                            (WideningReference, DelegateRelaxationLevelWideningToNonLambda)
                          Operand: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: Function <generated method>() As System.Int32, IsInvalid, IsImplicit) (Syntax: 'Function() 1')
                      Right: 
                        IParameterReferenceOperation: other1 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'other1')

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'other1 To other2')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False, IsImplicit) (Syntax: 'other1 To other2')

            Next (Regular) Block[B5]
        Block[B4] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'other1 To other2')
                  Value: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Boolean, IsImplicit) (Syntax: 'other1 To other2')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (NarrowingValue)
                      Operand: 
                        IBinaryOperation (BinaryOperatorKind.LessThanOrEqual, Checked) (OperationKind.Binary, Type: System.Object, IsImplicit) (Syntax: 'other1 To other2')
                          Left: 
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsInvalid, IsImplicit) (Syntax: 'Function() 1')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                                (WideningReference, DelegateRelaxationLevelWideningToNonLambda)
                              Operand: 
                                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: Function <generated method>() As System.Int32, IsInvalid, IsImplicit) (Syntax: 'Function() 1')
                          Right: 
                            IParameterReferenceOperation: other2 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'other2')

            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (0)
            Jump if False (Regular) to Block[B7]
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Boolean, IsImplicit) (Syntax: 'other1 To other2')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (NarrowingValue)
                  Operand: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'other1 To other2')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (WideningValue)
                      Operand: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Boolean, IsImplicit) (Syntax: 'other1 To other2')
                Leaving: {R2} {R1}

            Next (Regular) Block[B6]
                Leaving: {R2}
    }

    Block[B6] - Block
        Predecessors: [B5]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = false')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B7]
            Leaving: {R1}
}

Block[B7] - Exit
    Predecessors: [B5] [B6]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_107()
            Dim source = <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result as Boolean, input1 as Integer?, input2 as Integer, input3 as Integer, input4 as Integer?, input5 as Integer) 'BIND:"Sub M"
        Select Case input3 
            Case If(input1, input2) To If(input4, input5)
                result = false
        End Select
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input3')
              Value: 
                IParameterReferenceOperation: input3 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'input3')

        Next (Regular) Block[B2]
            Entering: {R2} {R3}

    .locals {R2}
    {
        CaptureIds: [2]
        .locals {R3}
        {
            CaptureIds: [1]
            Block[B2] - Block
                Predecessors: [B1]
                Statements (1)
                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input1')
                      Value: 
                        IParameterReferenceOperation: input1 (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'input1')

                Jump if True (Regular) to Block[B4]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'input1')
                      Operand: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'input1')
                    Leaving: {R3}

                Next (Regular) Block[B3]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input1')
                      Value: 
                        IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'input1')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'input1')
                          Arguments(0)

                Next (Regular) Block[B5]
                    Leaving: {R3}
        }

        Block[B4] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input2')
                  Value: 
                    IParameterReferenceOperation: input2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'input2')

            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (0)
            Jump if False (Regular) to Block[B11]
                IBinaryOperation (BinaryOperatorKind.GreaterThanOrEqual, Checked) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: 'If(input1,  ... t4, input5)')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input3')
                  Right: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(input1, input2)')
                Leaving: {R2} {R1}

            Next (Regular) Block[B6]
                Leaving: {R2}
                Entering: {R4} {R5}
    }
    .locals {R4}
    {
        CaptureIds: [4]
        .locals {R5}
        {
            CaptureIds: [3]
            Block[B6] - Block
                Predecessors: [B5]
                Statements (1)
                    IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input4')
                      Value: 
                        IParameterReferenceOperation: input4 (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'input4')

                Jump if True (Regular) to Block[B8]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'input4')
                      Operand: 
                        IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'input4')
                    Leaving: {R5}

                Next (Regular) Block[B7]
            Block[B7] - Block
                Predecessors: [B6]
                Statements (1)
                    IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input4')
                      Value: 
                        IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'input4')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'input4')
                          Arguments(0)

                Next (Regular) Block[B9]
                    Leaving: {R5}
        }

        Block[B8] - Block
            Predecessors: [B6]
            Statements (1)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input5')
                  Value: 
                    IParameterReferenceOperation: input5 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'input5')

            Next (Regular) Block[B9]
        Block[B9] - Block
            Predecessors: [B7] [B8]
            Statements (0)
            Jump if False (Regular) to Block[B11]
                IBinaryOperation (BinaryOperatorKind.LessThanOrEqual, Checked) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: 'If(input1,  ... t4, input5)')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input3')
                  Right: 
                    IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(input4, input5)')
                Leaving: {R4} {R1}

            Next (Regular) Block[B10]
                Leaving: {R4}
    }

    Block[B10] - Block
        Predecessors: [B9]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = false')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B11]
            Leaving: {R1}
}

Block[B11] - Exit
    Predecessors: [B5] [B9] [B10]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub SwitchFlow_108()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result As Boolean, input As C, other As C) 'BIND:"Sub M"
        Select Case input
            Case other
                result = False
        End Select
    End Sub

    Public Shared Operator =(x As C, y As C) As Boolean?
        Return False
    End Operator

    Public Shared Operator <>(x As C, y As C) As Boolean?
        Return True
    End Operator
End Class
        ]]>
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source)
            compilation.MakeMemberMissing(SpecialMember.System_Nullable_T_GetValueOrDefault)

            compilation.AssertNoDiagnostics()
            compilation.AssertTheseEmitDiagnostics(
<errors>
BC35000: Requested operation is not available because the runtime library function 'System.Nullable`1.GetValueOrDefault' is not defined.
            Case other
                 ~~~~~
</errors>)

            Dim expectedGraph = <![CDATA[
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: C) (Syntax: 'input')

        Jump if False (Regular) to Block[B3]
            IInvalidOperation (OperationKind.Invalid, Type: System.Boolean, IsImplicit) (Syntax: 'other')
              Children(1):
                  IBinaryOperation (BinaryOperatorKind.Equals, Checked) (OperatorMethod: Function C.op_Equality(x As C, y As C) As System.Nullable(Of System.Boolean)) (OperationKind.Binary, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: 'other')
                    Left: 
                      IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'input')
                    Right: 
                      IParameterReferenceOperation: other (OperationKind.ParameterReference, Type: C) (Syntax: 'other')
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = False')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = False')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'False')

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphForTest(Of MethodBlockSyntax)(compilation, expectedGraph)
        End Sub
    End Class
End Namespace
