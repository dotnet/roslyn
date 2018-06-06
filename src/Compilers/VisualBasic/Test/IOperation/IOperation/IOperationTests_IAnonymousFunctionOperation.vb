' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub LambdaFlow_01()
            Dim source = <![CDATA[
Class C
    Public Property A As System.Func(Of Integer)

    Sub M()'BIND:"Sub M"
        Dim c1, c2 As New C With {.A = Function() 1}
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
    Locals: [c1 As C] [c2 As C]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (6)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'New C With  ... nction() 1}')
              Value: 
                IObjectCreationOperation (Constructor: Sub C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'New C With  ... nction() 1}')
                  Arguments(0)
                  Initializer: 
                    null

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Void) (Syntax: '.A = Function() 1')
              Left: 
                IPropertyReferenceOperation: Property C.A As System.Func(Of System.Int32) (OperationKind.PropertyReference, Type: System.Func(Of System.Int32)) (Syntax: 'A')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'New C With  ... nction() 1}')
              Right: 
                IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32), IsImplicit) (Syntax: 'Function() 1')
                  Target: 
                    IFlowAnonymousFunctionOperation (Symbol: Function () As System.Int32) (OperationKind.FlowAnonymousFunction, Type: null) (Syntax: 'Function() 1')
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
                                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                                    Leaving: {R1#A0}
                        }

                        Block[B2#A0] - Exit
                            Predecessors: [B1#A0]
                            Statements (0)
                    }

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C, IsImplicit) (Syntax: 'c1, c2 As N ... nction() 1}')
              Left: 
                ILocalReferenceOperation: c1 (IsDeclaration: True) (OperationKind.LocalReference, Type: C, IsImplicit) (Syntax: 'c1')
              Right: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'New C With  ... nction() 1}')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'New C With  ... nction() 1}')
              Value: 
                IObjectCreationOperation (Constructor: Sub C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'New C With  ... nction() 1}')
                  Arguments(0)
                  Initializer: 
                    null

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Void) (Syntax: '.A = Function() 1')
              Left: 
                IPropertyReferenceOperation: Property C.A As System.Func(Of System.Int32) (OperationKind.PropertyReference, Type: System.Func(Of System.Int32)) (Syntax: 'A')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'New C With  ... nction() 1}')
              Right: 
                IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32), IsImplicit) (Syntax: 'Function() 1')
                  Target: 
                    IFlowAnonymousFunctionOperation (Symbol: Function () As System.Int32) (OperationKind.FlowAnonymousFunction, Type: null) (Syntax: 'Function() 1')
                    {
                        Block[B0#A1] - Entry
                            Statements (0)
                            Next (Regular) Block[B1#A1]
                                Entering: {R1#A1}

                        .locals {R1#A1}
                        {
                            Locals: [<anonymous local> As System.Int32]
                            Block[B1#A1] - Block
                                Predecessors: [B0#A1]
                                Statements (0)
                                Next (Return) Block[B2#A1]
                                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                                    Leaving: {R1#A1}
                        }

                        Block[B2#A1] - Exit
                            Predecessors: [B1#A1]
                            Statements (0)
                    }

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C, IsImplicit) (Syntax: 'c1, c2 As N ... nction() 1}')
              Left: 
                ILocalReferenceOperation: c2 (IsDeclaration: True) (OperationKind.LocalReference, Type: C, IsImplicit) (Syntax: 'c2')
              Right: 
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'New C With  ... nction() 1}')

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
        Public Sub LambdaFlow_02()
            Dim source = <![CDATA[
Class C
    Public Property A As System.Func(Of Integer)

    Sub M(d1 As System.Action(Of Boolean, Boolean), d2 As System.Action(Of Boolean, Boolean))'BIND:"Sub M"
        d1 = Sub (result1, input1)
                result1 = input1
             End Sub
        d2 = Sub (result2, input2) result2 = input2
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (2)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'd1 = Sub (r ... End Sub')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Action(Of System.Boolean, System.Boolean), IsImplicit) (Syntax: 'd1 = Sub (r ... End Sub')
              Left: 
                IParameterReferenceOperation: d1 (OperationKind.ParameterReference, Type: System.Action(Of System.Boolean, System.Boolean)) (Syntax: 'd1')
              Right: 
                IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action(Of System.Boolean, System.Boolean), IsImplicit) (Syntax: 'Sub (result ... End Sub')
                  Target: 
                    IFlowAnonymousFunctionOperation (Symbol: Sub (result1 As System.Boolean, input1 As System.Boolean)) (OperationKind.FlowAnonymousFunction, Type: null) (Syntax: 'Sub (result ... End Sub')
                    {
                        Block[B0#A0] - Entry
                            Statements (0)
                            Next (Regular) Block[B1#A0]
                        Block[B1#A0] - Block
                            Predecessors: [B0#A0]
                            Statements (1)
                                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result1 = input1')
                                  Expression: 
                                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result1 = input1')
                                      Left: 
                                        IParameterReferenceOperation: result1 (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result1')
                                      Right: 
                                        IParameterReferenceOperation: input1 (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'input1')

                            Next (Regular) Block[B2#A0]
                        Block[B2#A0] - Exit
                            Predecessors: [B1#A0]
                            Statements (0)
                    }

        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'd2 = Sub (r ... t2 = input2')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Action(Of System.Boolean, System.Boolean), IsImplicit) (Syntax: 'd2 = Sub (r ... t2 = input2')
              Left: 
                IParameterReferenceOperation: d2 (OperationKind.ParameterReference, Type: System.Action(Of System.Boolean, System.Boolean)) (Syntax: 'd2')
              Right: 
                IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action(Of System.Boolean, System.Boolean), IsImplicit) (Syntax: 'Sub (result ... t2 = input2')
                  Target: 
                    IFlowAnonymousFunctionOperation (Symbol: Sub (result2 As System.Boolean, input2 As System.Boolean)) (OperationKind.FlowAnonymousFunction, Type: null) (Syntax: 'Sub (result ... t2 = input2')
                    {
                        Block[B0#A1] - Entry
                            Statements (0)
                            Next (Regular) Block[B1#A1]
                        Block[B1#A1] - Block
                            Predecessors: [B0#A1]
                            Statements (1)
                                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result2 = input2')
                                  Expression: 
                                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result2 = input2')
                                      Left: 
                                        IParameterReferenceOperation: result2 (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result2')
                                      Right: 
                                        IParameterReferenceOperation: input2 (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'input2')

                            Next (Regular) Block[B2#A1]
                        Block[B2#A1] - Exit
                            Predecessors: [B1#A1]
                            Statements (0)
                    }

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub LambdaFlow_03()
            Dim source = <![CDATA[
Class C
    Public Property A As System.Func(Of Integer)

    Sub M(d1 As System.Action(Of Integer), d2 As System.Action(Of Boolean))'BIND:"Sub M"
        Dim i As Integer = 0

        d1 = Sub(input1)
                input1 = i

                d2 = Sub(input2)
                        input2 = true
                        i+=1
                     End Sub
             End Sub
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
    Locals: [i As System.Int32]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'i As Integer = 0')
              Left: 
                ILocalReferenceOperation: i (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'i')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'd1 = Sub(in ... End Sub')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Action(Of System.Int32), IsImplicit) (Syntax: 'd1 = Sub(in ... End Sub')
                  Left: 
                    IParameterReferenceOperation: d1 (OperationKind.ParameterReference, Type: System.Action(Of System.Int32)) (Syntax: 'd1')
                  Right: 
                    IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action(Of System.Int32), IsImplicit) (Syntax: 'Sub(input1) ... End Sub')
                      Target: 
                        IFlowAnonymousFunctionOperation (Symbol: Sub (input1 As System.Int32)) (OperationKind.FlowAnonymousFunction, Type: null) (Syntax: 'Sub(input1) ... End Sub')
                        {
                            Block[B0#A0] - Entry
                                Statements (0)
                                Next (Regular) Block[B1#A0]
                            Block[B1#A0] - Block
                                Predecessors: [B0#A0]
                                Statements (2)
                                    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'input1 = i')
                                      Expression: 
                                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'input1 = i')
                                          Left: 
                                            IParameterReferenceOperation: input1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'input1')
                                          Right: 
                                            ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')

                                    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'd2 = Sub(in ... End Sub')
                                      Expression: 
                                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Action(Of System.Boolean), IsImplicit) (Syntax: 'd2 = Sub(in ... End Sub')
                                          Left: 
                                            IParameterReferenceOperation: d2 (OperationKind.ParameterReference, Type: System.Action(Of System.Boolean)) (Syntax: 'd2')
                                          Right: 
                                            IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action(Of System.Boolean), IsImplicit) (Syntax: 'Sub(input2) ... End Sub')
                                              Target: 
                                                IFlowAnonymousFunctionOperation (Symbol: Sub (input2 As System.Boolean)) (OperationKind.FlowAnonymousFunction, Type: null) (Syntax: 'Sub(input2) ... End Sub')
                                                {
                                                    Block[B0#A0#A0] - Entry
                                                        Statements (0)
                                                        Next (Regular) Block[B1#A0#A0]
                                                    Block[B1#A0#A0] - Block
                                                        Predecessors: [B0#A0#A0]
                                                        Statements (2)
                                                            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'input2 = true')
                                                              Expression: 
                                                                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'input2 = true')
                                                                  Left: 
                                                                    IParameterReferenceOperation: input2 (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'input2')
                                                                  Right: 
                                                                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

                                                            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'i+=1')
                                                              Expression: 
                                                                ICompoundAssignmentOperation (BinaryOperatorKind.Add, Checked) (OperationKind.CompoundAssignment, Type: System.Int32, IsImplicit) (Syntax: 'i+=1')
                                                                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                                                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                                                  Left: 
                                                                    ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                                                                  Right: 
                                                                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

                                                        Next (Regular) Block[B2#A0#A0]
                                                    Block[B2#A0#A0] - Exit
                                                        Predecessors: [B1#A0#A0]
                                                        Statements (0)
                                                }

                                Next (Regular) Block[B2#A0]
                            Block[B2#A0] - Exit
                                Predecessors: [B1#A0]
                                Statements (0)
                        }

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
        Public Sub LambdaFlow_04()
            Dim source = <![CDATA[
Class C
    Public Property A As System.Func(Of Integer)

    Sub M()'BIND:"Sub M"
        Static c1, c2 As New C With {.A = Function() 1}
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
    Locals: [c1 As C] [c2 As C]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (0)
        Jump if False (Regular) to Block[B3]
            IStaticLocalInitializationSemaphoreOperation (Local Symbol: c1 As C) (OperationKind.StaticLocalInitializationSemaphore, Type: System.Boolean, IsImplicit) (Syntax: 'c1')

        Next (Regular) Block[B2]
            Entering: {R2}

    .static initializer {R2}
    {
        Block[B2] - Block
            Predecessors: [B1]
            Statements (3)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'New C With  ... nction() 1}')
                  Value: 
                    IObjectCreationOperation (Constructor: Sub C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'New C With  ... nction() 1}')
                      Arguments(0)
                      Initializer: 
                        null

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Void) (Syntax: '.A = Function() 1')
                  Left: 
                    IPropertyReferenceOperation: Property C.A As System.Func(Of System.Int32) (OperationKind.PropertyReference, Type: System.Func(Of System.Int32)) (Syntax: 'A')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'New C With  ... nction() 1}')
                  Right: 
                    IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32), IsImplicit) (Syntax: 'Function() 1')
                      Target: 
                        IFlowAnonymousFunctionOperation (Symbol: Function () As System.Int32) (OperationKind.FlowAnonymousFunction, Type: null) (Syntax: 'Function() 1')
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
                                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                                        Leaving: {R1#A0}
                            }

                            Block[B2#A0] - Exit
                                Predecessors: [B1#A0]
                                Statements (0)
                        }

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C, IsImplicit) (Syntax: 'c1, c2 As N ... nction() 1}')
                  Left: 
                    ILocalReferenceOperation: c1 (IsDeclaration: True) (OperationKind.LocalReference, Type: C, IsImplicit) (Syntax: 'c1')
                  Right: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'New C With  ... nction() 1}')

            Next (Regular) Block[B3]
                Leaving: {R2}
    }

    Block[B3] - Block
        Predecessors: [B1] [B2]
        Statements (0)
        Jump if False (Regular) to Block[B5]
            IStaticLocalInitializationSemaphoreOperation (Local Symbol: c2 As C) (OperationKind.StaticLocalInitializationSemaphore, Type: System.Boolean, IsImplicit) (Syntax: 'c2')
            Leaving: {R1}

        Next (Regular) Block[B4]
            Entering: {R3}

    .static initializer {R3}
    {
        Block[B4] - Block
            Predecessors: [B3]
            Statements (3)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'New C With  ... nction() 1}')
                  Value: 
                    IObjectCreationOperation (Constructor: Sub C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'New C With  ... nction() 1}')
                      Arguments(0)
                      Initializer: 
                        null

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Void) (Syntax: '.A = Function() 1')
                  Left: 
                    IPropertyReferenceOperation: Property C.A As System.Func(Of System.Int32) (OperationKind.PropertyReference, Type: System.Func(Of System.Int32)) (Syntax: 'A')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'New C With  ... nction() 1}')
                  Right: 
                    IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32), IsImplicit) (Syntax: 'Function() 1')
                      Target: 
                        IFlowAnonymousFunctionOperation (Symbol: Function () As System.Int32) (OperationKind.FlowAnonymousFunction, Type: null) (Syntax: 'Function() 1')
                        {
                            Block[B0#A1] - Entry
                                Statements (0)
                                Next (Regular) Block[B1#A1]
                                    Entering: {R1#A1}

                            .locals {R1#A1}
                            {
                                Locals: [<anonymous local> As System.Int32]
                                Block[B1#A1] - Block
                                    Predecessors: [B0#A1]
                                    Statements (0)
                                    Next (Return) Block[B2#A1]
                                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                                        Leaving: {R1#A1}
                            }

                            Block[B2#A1] - Exit
                                Predecessors: [B1#A1]
                                Statements (0)
                        }

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C, IsImplicit) (Syntax: 'c1, c2 As N ... nction() 1}')
                  Left: 
                    ILocalReferenceOperation: c2 (IsDeclaration: True) (OperationKind.LocalReference, Type: C, IsImplicit) (Syntax: 'c2')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'New C With  ... nction() 1}')

            Next (Regular) Block[B5]
                Leaving: {R3} {R1}
    }
}

Block[B5] - Exit
    Predecessors: [B3] [B4]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub LambdaFlow_05()
            Dim source = <![CDATA[
Class C
    Public Property A As System.Func(Of Integer)

    Sub M(d As System.Action(Of C, C, C), x as C, y as C, z as C)'BIND:"Sub M"
        x = If(y, z)
        d = Sub (result1, input1, input2)
               result1 = If(input1, input2)
            End Sub
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (2)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x')
          Value: 
            IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: C) (Syntax: 'x')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'y')
          Value: 
            IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: C) (Syntax: 'y')

    Jump if True (Regular) to Block[B3]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'y')
          Operand: 
            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'y')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'y')
          Value: 
            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'y')

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'z')
          Value: 
            IParameterReferenceOperation: z (OperationKind.ParameterReference, Type: C) (Syntax: 'z')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (2)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = If(y, z)')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C, IsImplicit) (Syntax: 'x = If(y, z)')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'x')
              Right: 
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'If(y, z)')

        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'd = Sub (re ... End Sub')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Action(Of C, C, C), IsImplicit) (Syntax: 'd = Sub (re ... End Sub')
              Left: 
                IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: System.Action(Of C, C, C)) (Syntax: 'd')
              Right: 
                IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action(Of C, C, C), IsImplicit) (Syntax: 'Sub (result ... End Sub')
                  Target: 
                    IFlowAnonymousFunctionOperation (Symbol: Sub (result1 As C, input1 As C, input2 As C)) (OperationKind.FlowAnonymousFunction, Type: null) (Syntax: 'Sub (result ... End Sub')
                    {
                        Block[B0#A0] - Entry
                            Statements (0)
                            Next (Regular) Block[B1#A0]
                        Block[B1#A0] - Block
                            Predecessors: [B0#A0]
                            Statements (2)
                                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'result1')
                                  Value: 
                                    IParameterReferenceOperation: result1 (OperationKind.ParameterReference, Type: C) (Syntax: 'result1')

                                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input1')
                                  Value: 
                                    IParameterReferenceOperation: input1 (OperationKind.ParameterReference, Type: C) (Syntax: 'input1')

                            Jump if True (Regular) to Block[B3#A0]
                                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'input1')
                                  Operand: 
                                    IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'input1')

                            Next (Regular) Block[B2#A0]
                        Block[B2#A0] - Block
                            Predecessors: [B1#A0]
                            Statements (1)
                                IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input1')
                                  Value: 
                                    IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'input1')

                            Next (Regular) Block[B4#A0]
                        Block[B3#A0] - Block
                            Predecessors: [B1#A0]
                            Statements (1)
                                IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input2')
                                  Value: 
                                    IParameterReferenceOperation: input2 (OperationKind.ParameterReference, Type: C) (Syntax: 'input2')

                            Next (Regular) Block[B4#A0]
                        Block[B4#A0] - Block
                            Predecessors: [B2#A0] [B3#A0]
                            Statements (1)
                                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result1 = I ... t1, input2)')
                                  Expression: 
                                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C, IsImplicit) (Syntax: 'result1 = I ... t1, input2)')
                                      Left: 
                                        IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'result1')
                                      Right: 
                                        IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'If(input1, input2)')

                            Next (Regular) Block[B5#A0]
                        Block[B5#A0] - Exit
                            Predecessors: [B4#A0]
                            Statements (0)
                    }

    Next (Regular) Block[B5]
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

    End Class
End Namespace
