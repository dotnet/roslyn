' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics
    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub IBranchStatement_ExitNestedLoop()
            Dim source = <![CDATA[
Class Program
    Private Shared Sub Main() 'BIND:"Private Shared Sub Main()"
        Dim x As Boolean = false
        While True
            Do While True
                If x Then
                    Exit Do
                Else
                    Exit While
                End If
            Loop
        End While

        Exit Sub
    End Sub
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IBlockOperation (5 statements, 1 locals) (OperationKind.Block, Type: null) (Syntax: 'Private Sha ... End Sub')
  Locals: Local_1: x As System.Boolean
  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim x As Boolean = false')
    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'x As Boolean = false')
      Declarators:
          IVariableDeclaratorOperation (Symbol: x As System.Boolean) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'x')
            Initializer: 
              null
      Initializer: 
        IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= false')
          ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')
  IWhileLoopOperation (ConditionIsTop: True, ConditionIsUntil: False) (LoopKind.While, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'While True ... End While')
    Condition: 
      ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'True')
    Body: 
      IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'While True ... End While')
        IWhileLoopOperation (ConditionIsTop: True, ConditionIsUntil: False) (LoopKind.While, Continue Label Id: 2, Exit Label Id: 3) (OperationKind.Loop, Type: null) (Syntax: 'Do While Tr ... Loop')
          Condition: 
            ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'True')
          Body: 
            IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Do While Tr ... Loop')
              IConditionalOperation (OperationKind.Conditional, Type: null) (Syntax: 'If x Then ... End If')
                Condition: 
                  ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Boolean) (Syntax: 'x')
                WhenTrue: 
                  IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'If x Then ... End If')
                    IBranchOperation (BranchKind.Break, Label Id: 3) (OperationKind.Branch, Type: null) (Syntax: 'Exit Do')
                WhenFalse: 
                  IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: 'Else ... Exit While')
                    IBranchOperation (BranchKind.Break, Label Id: 1) (OperationKind.Branch, Type: null) (Syntax: 'Exit While')
          IgnoredCondition: 
            null
    IgnoredCondition: 
      null
  IBranchOperation (BranchKind.Break, Label: exit) (OperationKind.Branch, Type: null) (Syntax: 'Exit Sub')
  ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'End Sub')
    Statement: 
      null
  IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'End Sub')
    ReturnedValue: 
      null
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedOperationTree, "")
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub BranchFlow_01()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M() 'BIND:"Sub M"
        label1:
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Exit
    Predecessors: [B0]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub BranchFlow_06()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M() 'BIND:"Sub M"
        Goto label1
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30132: Label 'label1' is not defined.
        Goto label1
             ~~~~~~
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (2)
        IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: 'label1')
          Children(0)

        IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: 'Goto label1')
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
        Public Sub BranchFlow_12()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(x As Boolean, y As Boolean) 'BIND:"Sub M"
        While y
            If x
                Exit While
            End If

            y = False
        End While
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0] [B3]
    Statements (0)
    Jump if False (Regular) to Block[B4]
        IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'y')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (0)
    Jump if False (Regular) to Block[B3]
        IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'x')

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B2]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'y = False')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'y = False')
              Left: 
                IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'y')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'False')

    Next (Regular) Block[B1]
Block[B4] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub BranchFlow_13()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(x As Boolean, y As Boolean, z As Boolean, u As Boolean) 'BIND:"Sub M"
        While y
            Do While z
                If x
                    Exit While
                End If
                If u
                    Exit Do
                End If
                y = False
            Loop
        End While
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0] [B2] [B4]
    Statements (0)
    Jump if False (Regular) to Block[B6]
        IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'y')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1] [B5]
    Statements (0)
    Jump if False (Regular) to Block[B1]
        IParameterReferenceOperation: z (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'z')

    Next (Regular) Block[B3]
Block[B3] - Block
    Predecessors: [B2]
    Statements (0)
    Jump if False (Regular) to Block[B4]
        IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'x')

    Next (Regular) Block[B6]
Block[B4] - Block
    Predecessors: [B3]
    Statements (0)
    Jump if False (Regular) to Block[B5]
        IParameterReferenceOperation: u (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'u')

    Next (Regular) Block[B1]
Block[B5] - Block
    Predecessors: [B4]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'y = False')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'y = False')
              Left: 
                IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'y')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'False')

    Next (Regular) Block[B2]
Block[B6] - Exit
    Predecessors: [B1] [B3]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub BranchFlow_14()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(x As Boolean) 'BIND:"Sub M"
        While x
            Exit Do
        End While
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30089: 'Exit Do' can only appear inside a 'Do' statement.
            Exit Do
            ~~~~~~~
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0] [B2]
    Statements (0)
    Jump if False (Regular) to Block[B3]
        IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'x')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: 'Exit Do')
          Children(0)

    Next (Regular) Block[B1]
Block[B3] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub BranchFlow_15()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M() 'BIND:"Sub M"
        Exit Do
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30089: 'Exit Do' can only appear inside a 'Do' statement.
        Exit Do
        ~~~~~~~
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: 'Exit Do')
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
        Public Sub BranchFlow_16()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(x As Boolean, y As Boolean) 'BIND:"Sub M"
        While y
            If x
                Continue While
            End If

            y = False
        End While
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0] [B2] [B3]
    Statements (0)
    Jump if False (Regular) to Block[B4]
        IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'y')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (0)
    Jump if False (Regular) to Block[B3]
        IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'x')

    Next (Regular) Block[B1]
Block[B3] - Block
    Predecessors: [B2]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'y = False')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'y = False')
              Left: 
                IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'y')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'False')

    Next (Regular) Block[B1]
Block[B4] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub BranchFlow_17()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(x As Boolean, y As Boolean, z As Boolean, u As Boolean) 'BIND:"Sub M"
        While y
            Do While z
                If x
                    Continue While
                End If
                If u
                    Continue Do
                End If
                y = False
            Loop
        End While
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0] [B2] [B3]
    Statements (0)
    Jump if False (Regular) to Block[B6]
        IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'y')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1] [B4] [B5]
    Statements (0)
    Jump if False (Regular) to Block[B1]
        IParameterReferenceOperation: z (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'z')

    Next (Regular) Block[B3]
Block[B3] - Block
    Predecessors: [B2]
    Statements (0)
    Jump if False (Regular) to Block[B4]
        IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'x')

    Next (Regular) Block[B1]
Block[B4] - Block
    Predecessors: [B3]
    Statements (0)
    Jump if False (Regular) to Block[B5]
        IParameterReferenceOperation: u (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'u')

    Next (Regular) Block[B2]
Block[B5] - Block
    Predecessors: [B4]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'y = False')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'y = False')
              Left: 
                IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'y')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'False')

    Next (Regular) Block[B2]
Block[B6] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub BranchFlow_18()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(x As Boolean) 'BIND:"Sub M"
        While x
            Continue Do
        End While
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30782: 'Continue Do' can only appear inside a 'Do' statement.
            Continue Do
            ~~~~~~~~~~~
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0] [B2]
    Statements (0)
    Jump if False (Regular) to Block[B3]
        IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'x')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: 'Continue Do')
          Children(0)

    Next (Regular) Block[B1]
Block[B3] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub BranchFlow_19()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M() 'BIND:"Sub M"
        Continue Do
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30782: 'Continue Do' can only appear inside a 'Do' statement.
        Continue Do
        ~~~~~~~~~~~
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: 'Continue Do')
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
        Public Sub BranchFlow_20()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(x As Boolean) 'BIND:"Sub M"
        Try
            If x
                Exit Try
            End If

            x = True
        Catch
        End Try
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.try {R1, R2}
{
    Block[B1] - Block
        Predecessors: [B0]
        Statements (0)
        Jump if False (Regular) to Block[B2]
            IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'x')

        Next (Regular) Block[B4]
            Leaving: {R2} {R1}
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = True')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'x = True')
                  Left: 
                    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'x')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'True')

        Next (Regular) Block[B4]
            Leaving: {R2} {R1}
}
.catch {R3} (System.Exception)
{
    Block[B3] - Block
        Predecessors (0)
        Statements (0)
        Next (Regular) Block[B4]
            Leaving: {R3} {R1}
}

Block[B4] - Exit
    Predecessors: [B1] [B2] [B3]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub BranchFlow_21()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(x As Boolean) 'BIND:"Sub M"
        Try
            x = True
        Catch
            If x
                Exit Try
            End If

            x = True
        End Try
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.try {R1, R2}
{
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = True')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'x = True')
                  Left: 
                    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'x')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'True')

        Next (Regular) Block[B4]
            Leaving: {R2} {R1}
}
.catch {R3} (System.Exception)
{
    Block[B2] - Block
        Predecessors (0)
        Statements (0)
        Jump if False (Regular) to Block[B3]
            IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'x')

        Next (Regular) Block[B4]
            Leaving: {R3} {R1}
    Block[B3] - Block
        Predecessors: [B2]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = True')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'x = True')
                  Left: 
                    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'x')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'True')

        Next (Regular) Block[B4]
            Leaving: {R3} {R1}
}

Block[B4] - Exit
    Predecessors: [B1] [B2] [B3]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub BranchFlow_22()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M() 'BIND:"Sub M"
        Exit Try
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30393: 'Exit Try' can only appear inside a 'Try' statement.
        Exit Try
        ~~~~~~~~
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: 'Exit Try')
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
        Public Sub BranchFlow_23()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(x As Boolean) 'BIND:"Sub M"
        If x
            Exit Sub
        End If

        x = True
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (0)
    Jump if False (Regular) to Block[B2]
        IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'x')

    Next (Regular) Block[B3]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = True')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'x = True')
              Left: 
                IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'x')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'True')

    Next (Regular) Block[B3]
Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub BranchFlow_24()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M() 'BIND:"Sub M"
        Exit Function
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30067: 'Exit Function' is not valid in a Sub or Property.
        Exit Function
        ~~~~~~~~~~~~~
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: 'Exit Function')
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
        Public Sub BranchFlow_25()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(x As Boolean) 'BIND:"Sub M"
        GoTo finallyLabel
        GoTo catchlabel
        GoTo trylabel

        Try
trylabel:
            GoTo finallyLabel
            GoTo catchlabel
            GoTo outsideLabel
        Catch
catchlabel:
            GoTo finallyLabel
            GoTo trylabel
            GoTo outsideLabel
        Finally
finallyLabel:
            GoTo catchlabel
            GoTo trylabel
            GoTo outsideLabel
        End Try

        x = true
outsideLabel:
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30754: 'GoTo finallyLabel' is not valid because 'finallyLabel' is inside a 'Try', 'Catch' or 'Finally' statement that does not contain this statement.
        GoTo finallyLabel
             ~~~~~~~~~~~~
BC30754: 'GoTo catchlabel' is not valid because 'catchlabel' is inside a 'Try', 'Catch' or 'Finally' statement that does not contain this statement.
        GoTo catchlabel
             ~~~~~~~~~~
BC30754: 'GoTo trylabel' is not valid because 'trylabel' is inside a 'Try', 'Catch' or 'Finally' statement that does not contain this statement.
        GoTo trylabel
             ~~~~~~~~
BC30754: 'GoTo finallyLabel' is not valid because 'finallyLabel' is inside a 'Try', 'Catch' or 'Finally' statement that does not contain this statement.
            GoTo finallyLabel
                 ~~~~~~~~~~~~
BC30754: 'GoTo catchlabel' is not valid because 'catchlabel' is inside a 'Try', 'Catch' or 'Finally' statement that does not contain this statement.
            GoTo catchlabel
                 ~~~~~~~~~~
BC30754: 'GoTo finallyLabel' is not valid because 'finallyLabel' is inside a 'Try', 'Catch' or 'Finally' statement that does not contain this statement.
            GoTo finallyLabel
                 ~~~~~~~~~~~~
BC30101: Branching out of a 'Finally' is not valid.
            GoTo catchlabel
                 ~~~~~~~~~~
BC30754: 'GoTo catchlabel' is not valid because 'catchlabel' is inside a 'Try', 'Catch' or 'Finally' statement that does not contain this statement.
            GoTo catchlabel
                 ~~~~~~~~~~
BC30101: Branching out of a 'Finally' is not valid.
            GoTo trylabel
                 ~~~~~~~~
BC30101: Branching out of a 'Finally' is not valid.
            GoTo outsideLabel
                 ~~~~~~~~~~~~
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B3]
        Entering: {R1} {R6}

.try {R1, R2}
{
    .try {R3, R4}
    {
        Block[B1] - Block [UnReachable]
            Predecessors (0)
            Statements (0)
            Next (Regular) Block[B3]
                Leaving: {R4} {R3} {R2}
                Entering: {R6}
    }
    .catch {R5} (System.Exception)
    {
        Block[B2] - Block
            Predecessors: [B3]
            Statements (0)
            Next (Regular) Block[B3]
                Leaving: {R5} {R3} {R2}
                Entering: {R6}
    }
}
.finally {R6}
{
    Block[B3] - Block
        Predecessors: [B0] [B1] [B2]
        Statements (0)
        Next (Regular) Block[B2]
            Leaving: {R6}
            Entering: {R2} {R3} {R5}
}

Block[B4] - Block [UnReachable]
    Predecessors (0)
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = true')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'x = true')
              Left: 
                IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'x')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

    Next (Regular) Block[B5]
Block[B5] - Exit [UnReachable]
    Predecessors: [B4]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub BranchFlow_26()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(x As Boolean) 'BIND:"Sub M"
        GoTo finallyLabel
        Try
        Finally
finallyLabel:
        End Try
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30754: 'GoTo finallyLabel' is not valid because 'finallyLabel' is inside a 'Try', 'Catch' or 'Finally' statement that does not contain this statement.
        GoTo finallyLabel
             ~~~~~~~~~~~~
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B2]
        Entering: {R1} {R3}

.try {R1, R2}
{
    Block[B1] - Block [UnReachable]
        Predecessors (0)
        Statements (0)
        Next (Regular) Block[B3]
            Finalizing: {R3}
            Leaving: {R2} {R1}
}
.finally {R3}
{
    Block[B2] - Block
        Predecessors: [B0]
        Statements (0)
        Next (StructuredExceptionHandling) Block[null]
}

Block[B3] - Exit [UnReachable]
    Predecessors: [B1]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub BranchFlow_27()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(x As Boolean) 'BIND:"Sub M"
        GoTo finallyLabel
        Try
            GoTo finallyLabel
        Catch
            GoTo finallyLabel
        Finally
finallyLabel:
        End Try
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30754: 'GoTo finallyLabel' is not valid because 'finallyLabel' is inside a 'Try', 'Catch' or 'Finally' statement that does not contain this statement.
        GoTo finallyLabel
             ~~~~~~~~~~~~
BC30754: 'GoTo finallyLabel' is not valid because 'finallyLabel' is inside a 'Try', 'Catch' or 'Finally' statement that does not contain this statement.
            GoTo finallyLabel
                 ~~~~~~~~~~~~
BC30754: 'GoTo finallyLabel' is not valid because 'finallyLabel' is inside a 'Try', 'Catch' or 'Finally' statement that does not contain this statement.
            GoTo finallyLabel
                 ~~~~~~~~~~~~
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B3]
        Entering: {R1} {R6}

.try {R1, R2}
{
    .try {R3, R4}
    {
        Block[B1] - Block [UnReachable]
            Predecessors (0)
            Statements (0)
            Next (Regular) Block[B4]
                Finalizing: {R6}
                Leaving: {R4} {R3} {R2} {R1}
    }
    .catch {R5} (System.Exception)
    {
        Block[B2] - Block [UnReachable]
            Predecessors (0)
            Statements (0)
            Next (Regular) Block[B3]
                Leaving: {R5} {R3} {R2}
                Entering: {R6}
    }
}
.finally {R6}
{
    Block[B3] - Block
        Predecessors: [B0] [B2]
        Statements (0)
        Next (StructuredExceptionHandling) Block[null]
}

Block[B4] - Exit [UnReachable]
    Predecessors: [B1]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub BranchFlow_28()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(x As Boolean) 'BIND:"Sub M"
        GoTo finallyLabel
        x = true
        Try
            GoTo finallyLabel
        Finally
finallyLabel:
        End Try
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30754: 'GoTo finallyLabel' is not valid because 'finallyLabel' is inside a 'Try', 'Catch' or 'Finally' statement that does not contain this statement.
        GoTo finallyLabel
             ~~~~~~~~~~~~
BC30754: 'GoTo finallyLabel' is not valid because 'finallyLabel' is inside a 'Try', 'Catch' or 'Finally' statement that does not contain this statement.
            GoTo finallyLabel
                 ~~~~~~~~~~~~
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B3]
        Entering: {R1} {R3}
Block[B1] - Block [UnReachable]
    Predecessors (0)
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = true')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'x = true')
              Left: 
                IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'x')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

    Next (Regular) Block[B2]
        Entering: {R1} {R2}

.try {R1, R2}
{
    Block[B2] - Block [UnReachable]
        Predecessors: [B1]
        Statements (0)
        Next (Regular) Block[B3]
            Leaving: {R2}
            Entering: {R3}
}
.finally {R3}
{
    Block[B3] - Block
        Predecessors: [B0] [B2]
        Statements (0)
        Next (StructuredExceptionHandling) Block[null]
}

Block[B4] - Exit [UnReachable]
    Predecessors (0)
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub BranchFlow_29()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M() 'BIND:"Sub M"
        GoTo catchlabel

        Try
        Catch
catchlabel:
        End Try
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30754: 'GoTo catchlabel' is not valid because 'catchlabel' is inside a 'Try', 'Catch' or 'Finally' statement that does not contain this statement.
        GoTo catchlabel
             ~~~~~~~~~~
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B2]
        Entering: {R1} {R3}

.try {R1, R2}
{
    Block[B1] - Block [UnReachable]
        Predecessors (0)
        Statements (0)
        Next (Regular) Block[B3]
            Leaving: {R2} {R1}
}
.catch {R3} (System.Exception)
{
    Block[B2] - Block
        Predecessors: [B0]
        Statements (0)
        Next (Regular) Block[B3]
            Leaving: {R3} {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub BranchFlow_30()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M() 'BIND:"Sub M"
        GoTo catchlabel

        Try
            GoTo catchlabel
        Catch
catchlabel:
        Finally
            GoTo catchlabel
        End Try
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30754: 'GoTo catchlabel' is not valid because 'catchlabel' is inside a 'Try', 'Catch' or 'Finally' statement that does not contain this statement.
        GoTo catchlabel
             ~~~~~~~~~~
BC30754: 'GoTo catchlabel' is not valid because 'catchlabel' is inside a 'Try', 'Catch' or 'Finally' statement that does not contain this statement.
            GoTo catchlabel
                 ~~~~~~~~~~
BC30101: Branching out of a 'Finally' is not valid.
            GoTo catchlabel
                 ~~~~~~~~~~
BC30754: 'GoTo catchlabel' is not valid because 'catchlabel' is inside a 'Try', 'Catch' or 'Finally' statement that does not contain this statement.
            GoTo catchlabel
                 ~~~~~~~~~~
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B2]
        Entering: {R1} {R2} {R3} {R5}

.try {R1, R2}
{
    .try {R3, R4}
    {
        Block[B1] - Block [UnReachable]
            Predecessors (0)
            Statements (0)
            Next (Regular) Block[B4]
                Finalizing: {R6}
                Leaving: {R4} {R3} {R2} {R1}
    }
    .catch {R5} (System.Exception)
    {
        Block[B2] - Block
            Predecessors: [B0] [B3]
            Statements (0)
            Next (Regular) Block[B4]
                Finalizing: {R6}
                Leaving: {R5} {R3} {R2} {R1}
    }
}
.finally {R6}
{
    Block[B3] - Block
        Predecessors (0)
        Statements (0)
        Next (Regular) Block[B2]
            Leaving: {R6}
            Entering: {R2} {R3} {R5}
}

Block[B4] - Exit [UnReachable]
    Predecessors: [B1] [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub BranchFlow_31()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(x as Boolean) 'BIND:"Sub M"
        GoTo catchlabel
        x = true
        Try
            GoTo catchlabel
        Catch
catchlabel:
        Finally
            GoTo catchlabel
        End Try
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30754: 'GoTo catchlabel' is not valid because 'catchlabel' is inside a 'Try', 'Catch' or 'Finally' statement that does not contain this statement.
        GoTo catchlabel
             ~~~~~~~~~~
BC30754: 'GoTo catchlabel' is not valid because 'catchlabel' is inside a 'Try', 'Catch' or 'Finally' statement that does not contain this statement.
            GoTo catchlabel
                 ~~~~~~~~~~
BC30101: Branching out of a 'Finally' is not valid.
            GoTo catchlabel
                 ~~~~~~~~~~
BC30754: 'GoTo catchlabel' is not valid because 'catchlabel' is inside a 'Try', 'Catch' or 'Finally' statement that does not contain this statement.
            GoTo catchlabel
                 ~~~~~~~~~~
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B3]
        Entering: {R1} {R2} {R3} {R5}
Block[B1] - Block [UnReachable]
    Predecessors (0)
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = true')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'x = true')
              Left: 
                IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'x')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

    Next (Regular) Block[B2]
        Entering: {R1} {R2} {R3} {R4}

.try {R1, R2}
{
    .try {R3, R4}
    {
        Block[B2] - Block [UnReachable]
            Predecessors: [B1]
            Statements (0)
            Next (Regular) Block[B3]
                Leaving: {R4}
                Entering: {R5}
    }
    .catch {R5} (System.Exception)
    {
        Block[B3] - Block
            Predecessors: [B0] [B2] [B4]
            Statements (0)
            Next (Regular) Block[B5]
                Finalizing: {R6}
                Leaving: {R5} {R3} {R2} {R1}
    }
}
.finally {R6}
{
    Block[B4] - Block
        Predecessors (0)
        Statements (0)
        Next (Regular) Block[B3]
            Leaving: {R6}
            Entering: {R2} {R3} {R5}
}

Block[B5] - Exit [UnReachable]
    Predecessors: [B3]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub BranchFlow_32()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(x As Boolean) 'BIND:"Sub M"
        GoTo trylabel
        x = true
        Try
trylabel:
        Catch
        End Try
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30754: 'GoTo trylabel' is not valid because 'trylabel' is inside a 'Try', 'Catch' or 'Finally' statement that does not contain this statement.
        GoTo trylabel
             ~~~~~~~~
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B2]
        Entering: {R1} {R2}
Block[B1] - Block [UnReachable]
    Predecessors (0)
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = true')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'x = true')
              Left: 
                IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'x')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

    Next (Regular) Block[B2]
        Entering: {R1} {R2}

.try {R1, R2}
{
    Block[B2] - Block
        Predecessors: [B0] [B1]
        Statements (0)
        Next (Regular) Block[B4]
            Leaving: {R2} {R1}
}
.catch {R3} (System.Exception)
{
    Block[B3] - Block
        Predecessors (0)
        Statements (0)
        Next (Regular) Block[B4]
            Leaving: {R3} {R1}
}

Block[B4] - Exit
    Predecessors: [B2] [B3]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub BranchFlow_33()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(x As Boolean) 'BIND:"Sub M"
        Try
trylabel:
        Catch
            Goto tryLabel
        End Try
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.try {R1, R2}
{
    Block[B1] - Block
        Predecessors: [B0] [B2]
        Statements (0)
        Next (Regular) Block[B3]
            Leaving: {R2} {R1}
}
.catch {R3} (System.Exception)
{
    Block[B2] - Block
        Predecessors (0)
        Statements (0)
        Next (Regular) Block[B1]
            Leaving: {R3}
            Entering: {R2}
}

Block[B3] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub BranchFlow_34()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(x As Boolean) 'BIND:"Sub M"
        Try
trylabel:   x = false
        Catch
            Goto tryLabel
        Finally 
            x = true
        End Try
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2} {R3} {R4}

.try {R1, R2}
{
    .try {R3, R4}
    {
        Block[B1] - Block
            Predecessors: [B0] [B2]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = false')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'x = false')
                      Left: 
                        IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'x')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

            Next (Regular) Block[B4]
                Finalizing: {R6}
                Leaving: {R4} {R3} {R2} {R1}
    }
    .catch {R5} (System.Exception)
    {
        Block[B2] - Block
            Predecessors (0)
            Statements (0)
            Next (Regular) Block[B1]
                Leaving: {R5}
                Entering: {R4}
    }
}
.finally {R6}
{
    Block[B3] - Block
        Predecessors (0)
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = true')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'x = true')
                  Left: 
                    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'x')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

        Next (StructuredExceptionHandling) Block[null]
}

Block[B4] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub BranchFlow_35()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(x As Boolean) 'BIND:"Sub M"
        GoTo trylabel
        x = true
        Try
trylabel:
        Finally
            Goto tryLabel
        End Try
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30754: 'GoTo trylabel' is not valid because 'trylabel' is inside a 'Try', 'Catch' or 'Finally' statement that does not contain this statement.
        GoTo trylabel
             ~~~~~~~~
BC30101: Branching out of a 'Finally' is not valid.
            Goto tryLabel
                 ~~~~~~~~
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B2]
        Entering: {R1} {R2}
Block[B1] - Block [UnReachable]
    Predecessors (0)
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = true')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'x = true')
              Left: 
                IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'x')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

    Next (Regular) Block[B2]
        Entering: {R1} {R2}

.try {R1, R2}
{
    Block[B2] - Block
        Predecessors: [B0] [B1] [B3]
        Statements (0)
        Next (Regular) Block[B4]
            Finalizing: {R3}
            Leaving: {R2} {R1}
}
.finally {R3}
{
    Block[B3] - Block
        Predecessors (0)
        Statements (0)
        Next (Regular) Block[B2]
            Leaving: {R3}
            Entering: {R2}
}

Block[B4] - Exit [UnReachable]
    Predecessors: [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub BranchFlow_36()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(x As Boolean) 'BIND:"Sub M"
        x = true
        Try
            GoTo outsideLabel
        Catch
            GoTo outsideLabel
        Finally
            GoTo outsideLabel
        End Try

        x = true
outsideLabel:
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30101: Branching out of a 'Finally' is not valid.
            GoTo outsideLabel
                 ~~~~~~~~~~~~
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = true')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'x = true')
              Left: 
                IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'x')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

    Next (Regular) Block[B2]
        Entering: {R1} {R2} {R3} {R4}

.try {R1, R2}
{
    .try {R3, R4}
    {
        Block[B2] - Block
            Predecessors: [B1]
            Statements (0)
            Next (Regular) Block[B6]
                Finalizing: {R6}
                Leaving: {R4} {R3} {R2} {R1}
    }
    .catch {R5} (System.Exception)
    {
        Block[B3] - Block
            Predecessors (0)
            Statements (0)
            Next (Regular) Block[B6]
                Finalizing: {R6}
                Leaving: {R5} {R3} {R2} {R1}
    }
}
.finally {R6}
{
    Block[B4] - Block
        Predecessors (0)
        Statements (0)
        Next (Regular) Block[B6]
            Leaving: {R6} {R1}
}

Block[B5] - Block [UnReachable]
    Predecessors (0)
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = true')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'x = true')
              Left: 
                IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'x')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

    Next (Regular) Block[B6]
Block[B6] - Exit
    Predecessors: [B2] [B3] [B4] [B5]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub BranchFlow_37()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(x as Boolean) 'BIND:"Sub M"
        Try
        Catch
catchlabel:
        Finally
            Try
                GoTo catchlabel
                x = true
            Catch
            End Try
        End Try
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30101: Branching out of a 'Finally' is not valid.
                GoTo catchlabel
                     ~~~~~~~~~~
BC30754: 'GoTo catchlabel' is not valid because 'catchlabel' is inside a 'Try', 'Catch' or 'Finally' statement that does not contain this statement.
                GoTo catchlabel
                     ~~~~~~~~~~
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2} {R3} {R4}

.try {R1, R2}
{
    .try {R3, R4}
    {
        Block[B1] - Block
            Predecessors: [B0]
            Statements (0)
            Next (Regular) Block[B7]
                Finalizing: {R6}
                Leaving: {R4} {R3} {R2} {R1}
    }
    .catch {R5} (System.Exception)
    {
        Block[B2] - Block
            Predecessors: [B3]
            Statements (0)
            Next (Regular) Block[B7]
                Finalizing: {R6}
                Leaving: {R5} {R3} {R2} {R1}
    }
}
.finally {R6}
{
    .try {R7, R8}
    {
        Block[B3] - Block
            Predecessors (0)
            Statements (0)
            Next (Regular) Block[B2]
                Leaving: {R8} {R7} {R6}
                Entering: {R2} {R3} {R5}
        Block[B4] - Block [UnReachable]
            Predecessors (0)
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = true')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'x = true')
                      Left: 
                        IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'x')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

            Next (Regular) Block[B6]
                Leaving: {R8} {R7}
    }
    .catch {R9} (System.Exception)
    {
        Block[B5] - Block
            Predecessors (0)
            Statements (0)
            Next (Regular) Block[B6]
                Leaving: {R9} {R7}
    }

    Block[B6] - Block
        Predecessors: [B4] [B5]
        Statements (0)
        Next (StructuredExceptionHandling) Block[null]
}

Block[B7] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub BranchFlow_38()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(x As Boolean) 'BIND:"Sub M"
        GoTo label1
        x = true
label1:
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B2]
Block[B1] - Block [UnReachable]
    Predecessors (0)
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = true')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'x = true')
              Left: 
                IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'x')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B0] [B1]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub BranchFlow_39()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M() 'BIND:"Sub M"
label1: GoTo label1
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0] [B1]
    Statements (0)
    Next (Regular) Block[B1]
Block[B2] - Exit [UnReachable]
    Predecessors (0)
    Statements (0)]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub BranchFlow_40()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(x as Boolean) 'BIND:"Sub M"
        Try
            x = true
        Catch
label1:     Goto label2
            x = false
label2:     Goto label1       
        End Try
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.try {R1, R2}
{
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = true')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'x = true')
                  Left: 
                    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'x')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

        Next (Regular) Block[B4]
            Leaving: {R2} {R1}
}
.catch {R3} (System.Exception)
{
    Block[B2] - Block
        Predecessors: [B2] [B3]
        Statements (0)
        Next (Regular) Block[B2]
    Block[B3] - Block [UnReachable]
        Predecessors (0)
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = false')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'x = false')
                  Left: 
                    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'x')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B2]
}

Block[B4] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub BranchFlow_41()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(x as Boolean) 'BIND:"Sub M"
        Try
            x = true
        Finally
label1:     Goto label2
            x = false
label2:     Goto label1       
        End Try
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.try {R1, R2}
{
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = true')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'x = true')
                  Left: 
                    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'x')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

        Next (Regular) Block[B4]
            Finalizing: {R3}
            Leaving: {R2} {R1}
}
.finally {R3}
{
    Block[B2] - Block
        Predecessors: [B2] [B3]
        Statements (0)
        Next (Regular) Block[B2]
    Block[B3] - Block [UnReachable]
        Predecessors (0)
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = false')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'x = false')
                  Left: 
                    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'x')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B2]
}

Block[B4] - Exit [UnReachable]
    Predecessors: [B1]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub BranchFlow_42()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(x as Boolean) 'BIND:"Sub M"
        Try
            x = true
        Catch
label1:     if x Then Goto label2
            x = false
label2:     Goto label1       
        End Try
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.try {R1, R2}
{
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = true')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'x = true')
                  Left: 
                    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'x')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

        Next (Regular) Block[B4]
            Leaving: {R2} {R1}
}
.catch {R3} (System.Exception)
{
    Block[B2] - Block
        Predecessors: [B2] [B3]
        Statements (0)
        Jump if False (Regular) to Block[B3]
            IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'x')

        Next (Regular) Block[B2]
    Block[B3] - Block
        Predecessors: [B2]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = false')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'x = false')
                  Left: 
                    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'x')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B2]
}

Block[B4] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub BranchFlow_43()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(x as Boolean) 'BIND:"Sub M"
        Try
            x = true
        Finally
label1:     if x Then Goto label2
            x = false
label2:     Goto label1       
        End Try
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.try {R1, R2}
{
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = true')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'x = true')
                  Left: 
                    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'x')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

        Next (Regular) Block[B4]
            Finalizing: {R3}
            Leaving: {R2} {R1}
}
.finally {R3}
{
    Block[B2] - Block
        Predecessors: [B2] [B3]
        Statements (0)
        Jump if False (Regular) to Block[B3]
            IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'x')

        Next (Regular) Block[B2]
    Block[B3] - Block
        Predecessors: [B2]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = false')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'x = false')
                  Left: 
                    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'x')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B2]
}

Block[B4] - Exit [UnReachable]
    Predecessors: [B1]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub BranchFlow_44()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(x as Boolean) 'BIND:"Sub M"
        Try
            x = true
        Catch
label1:     if x Then Goto label2
            Goto label3
            x = false
label2:     Goto label1
label3:
        End Try
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.try {R1, R2}
{
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = true')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'x = true')
                  Left: 
                    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'x')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

        Next (Regular) Block[B4]
            Leaving: {R2} {R1}
}
.catch {R3} (System.Exception)
{
    Block[B2] - Block
        Predecessors: [B2] [B3]
        Statements (0)
        Jump if False (Regular) to Block[B4]
            IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'x')
            Leaving: {R3} {R1}

        Next (Regular) Block[B2]
    Block[B3] - Block [UnReachable]
        Predecessors (0)
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = false')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'x = false')
                  Left: 
                    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'x')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B2]
}

Block[B4] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub BranchFlow_45()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(x as Boolean) 'BIND:"Sub M"
        Try
            x = true
        Finally
label1:     if x Then Goto label2
            Goto label3
            x = false
label2:     Goto label1       
label3:
        End Try
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.try {R1, R2}
{
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = true')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'x = true')
                  Left: 
                    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'x')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

        Next (Regular) Block[B5]
            Finalizing: {R3}
            Leaving: {R2} {R1}
}
.finally {R3}
{
    Block[B2] - Block
        Predecessors: [B2] [B3]
        Statements (0)
        Jump if False (Regular) to Block[B4]
            IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'x')

        Next (Regular) Block[B2]
    Block[B3] - Block [UnReachable]
        Predecessors (0)
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = false')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'x = false')
                  Left: 
                    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'x')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B2]
    Block[B4] - Block
        Predecessors: [B2]
        Statements (0)
        Next (StructuredExceptionHandling) Block[null]
}

Block[B5] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub BranchFlow_46()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(x as Boolean) 'BIND:"Sub M"
label1: if x Then Goto label2
        x = false
label2: Goto label1       
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0] [B1] [B2]
    Statements (0)
    Jump if False (Regular) to Block[B2]
        IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'x')

    Next (Regular) Block[B1]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = false')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'x = false')
              Left: 
                IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'x')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

    Next (Regular) Block[B1]
Block[B3] - Exit [UnReachable]
    Predecessors (0)
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub BranchFlow_47()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(x as Boolean) 'BIND:"Sub M"
label2: Goto label1       
        x = false
label1: if x Then Goto label2
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B2]
Block[B1] - Block [UnReachable]
    Predecessors (0)
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = false')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'x = false')
              Left: 
                IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'x')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B0] [B1] [B2]
    Statements (0)
    Jump if False (Regular) to Block[B3]
        IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'x')

    Next (Regular) Block[B2]
Block[B3] - Exit
    Predecessors: [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub BranchFlow_48()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(x as Boolean) 'BIND:"Sub M"
label1: if x Then Goto label2
        Goto label2
        x = false
label2: Goto label1       
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0] [B1*2] [B2]
    Statements (0)
    Jump if False (Regular) to Block[B1]
        IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'x')

    Next (Regular) Block[B1]
Block[B2] - Block [UnReachable]
    Predecessors (0)
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = false')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'x = false')
              Left: 
                IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'x')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

    Next (Regular) Block[B1]
Block[B3] - Exit [UnReachable]
    Predecessors (0)
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub BranchFlow_49()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M() 'BIND:"Sub M"
        Continue Do
        Continue While
        Continue For
        Exit Do
        Exit While
        Exit For
        Exit Select
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30782: 'Continue Do' can only appear inside a 'Do' statement.
        Continue Do
        ~~~~~~~~~~~
BC30784: 'Continue While' can only appear inside a 'While' statement.
        Continue While
        ~~~~~~~~~~~~~~
BC30783: 'Continue For' can only appear inside a 'For' statement.
        Continue For
        ~~~~~~~~~~~~
BC30089: 'Exit Do' can only appear inside a 'Do' statement.
        Exit Do
        ~~~~~~~
BC30097: 'Exit While' can only appear inside a 'While' statement.
        Exit While
        ~~~~~~~~~~
BC30096: 'Exit For' can only appear inside a 'For' statement.
        Exit For
        ~~~~~~~~
BC30099: 'Exit Select' can only appear inside a 'Select' statement.
        Exit Select
        ~~~~~~~~~~~
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (7)
        IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: 'Continue Do')
          Children(0)

        IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: 'Continue While')
          Children(0)

        IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: 'Continue For')
          Children(0)

        IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: 'Exit Do')
          Children(0)

        IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: 'Exit While')
          Children(0)

        IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: 'Exit For')
          Children(0)

        IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: 'Exit Select')
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
        Public Sub BranchFlow_51()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(a as boolean, b as boolean) 'BIND:"Sub M"
        GoTo label1
label1:
        a = true
        GoTo label3
label1:
        b = false
label3:
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30094: Label 'label1' is already defined in the current method.
label1:
~~~~~~
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a = true')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'a = true')
              Left: 
                IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'a')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

    Next (Regular) Block[B3]
Block[B2] - Block [UnReachable]
    Predecessors (0)
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = false')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'b = false')
              Left: 
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

    Next (Regular) Block[B3]
Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub BranchFlow_52()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(a as boolean, b as boolean) 'BIND:"Sub M"
label1:
        a = true
        GoTo label3
label1:
        b = false
        GoTo label1
label3:
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30094: Label 'label1' is already defined in the current method.
label1:
~~~~~~
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0] [B2]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a = true')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'a = true')
              Left: 
                IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'a')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

    Next (Regular) Block[B3]
Block[B2] - Block [UnReachable]
    Predecessors (0)
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = false')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'b = false')
              Left: 
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

    Next (Regular) Block[B1]
Block[B3] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub BranchFlow_53()
            Dim source = <![CDATA[
Class C
    Public Property A As System.Func(Of Integer)

    Sub M(d As System.Action)'BIND:"Sub M"
        d = Sub ()
                GoTo label1
             End Sub
label1:
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30132: Label 'label1' is not defined.
                GoTo label1
                     ~~~~~~
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'd = Sub () ... End Sub')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Action, IsInvalid, IsImplicit) (Syntax: 'd = Sub () ... End Sub')
              Left: 
                IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: System.Action) (Syntax: 'd')
              Right: 
                IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action, IsInvalid, IsImplicit) (Syntax: 'Sub () ... End Sub')
                  Target: 
                    IFlowAnonymousFunctionOperation (Symbol: Sub ()) (OperationKind.FlowAnonymousFunction, Type: null, IsInvalid) (Syntax: 'Sub () ... End Sub')
                    {
                        Block[B0#A0] - Entry
                            Statements (0)
                            Next (Regular) Block[B1#A0]
                        Block[B1#A0] - Block
                            Predecessors: [B0#A0]
                            Statements (2)
                                IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: 'label1')
                                  Children(0)

                                IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: 'GoTo label1')
                                  Children(0)

                            Next (Regular) Block[B2#A0]
                        Block[B2#A0] - Exit
                            Predecessors: [B1#A0]
                            Statements (0)
                    }

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

    End Class
End Namespace
