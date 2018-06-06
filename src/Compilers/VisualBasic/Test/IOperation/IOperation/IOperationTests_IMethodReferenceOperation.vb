' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Operations
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    <CompilerTrait(CompilerFeature.IOperation)>
    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact>
        Public Sub MethodReference_NoControlFlow()
            ' Verify method references with different kinds of instance references.
            Dim source = <![CDATA[
Imports System

Friend Class C
    Public Overridable Function M1_Method() As Integer
        Return 0
    End Function

    Public Shared Function M2_Method() As Integer
        Return 0
    End Function

    Public Sub M(c As C, m1 As Func(Of Integer), m2 As Func(Of Integer), m3 As Func(Of Integer)) 'BIND:"Public Sub M(c As C, m1 As Func(Of Integer), m2 As Func(Of Integer), m3 As Func(Of Integer))"
        m1 = AddressOf Me.M1_Method
        m2 = AddressOf c.M1_Method
        m3 = AddressOf M2_Method
    End Sub
End Class
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (3)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'm1 = Addres ... e.M1_Method')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Func(Of System.Int32), IsImplicit) (Syntax: 'm1 = Addres ... e.M1_Method')
              Left: 
                IParameterReferenceOperation: m1 (OperationKind.ParameterReference, Type: System.Func(Of System.Int32)) (Syntax: 'm1')
              Right: 
                IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32), IsImplicit) (Syntax: 'AddressOf Me.M1_Method')
                  Target: 
                    IMethodReferenceOperation: Function C.M1_Method() As System.Int32 (IsVirtual) (OperationKind.MethodReference, Type: null) (Syntax: 'AddressOf Me.M1_Method')
                      Instance Receiver: 
                        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C) (Syntax: 'Me')

        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'm2 = Addres ... c.M1_Method')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Func(Of System.Int32), IsImplicit) (Syntax: 'm2 = Addres ... c.M1_Method')
              Left: 
                IParameterReferenceOperation: m2 (OperationKind.ParameterReference, Type: System.Func(Of System.Int32)) (Syntax: 'm2')
              Right: 
                IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32), IsImplicit) (Syntax: 'AddressOf c.M1_Method')
                  Target: 
                    IMethodReferenceOperation: Function C.M1_Method() As System.Int32 (IsVirtual) (OperationKind.MethodReference, Type: null) (Syntax: 'AddressOf c.M1_Method')
                      Instance Receiver: 
                        IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C) (Syntax: 'c')

        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'm3 = AddressOf M2_Method')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Func(Of System.Int32), IsImplicit) (Syntax: 'm3 = AddressOf M2_Method')
              Left: 
                IParameterReferenceOperation: m3 (OperationKind.ParameterReference, Type: System.Func(Of System.Int32)) (Syntax: 'm3')
              Right: 
                IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32), IsImplicit) (Syntax: 'AddressOf M2_Method')
                  Target: 
                    IMethodReferenceOperation: Function C.M2_Method() As System.Int32 (Static) (OperationKind.MethodReference, Type: null) (Syntax: 'AddressOf M2_Method')
                      Instance Receiver: 
                        null

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
        Public Sub MethodReference_ControlFlowInReceiver()
            Dim source = <![CDATA[
Imports System

Friend Class C
    Public Function M1() As Integer
        Return 0
    End Function

    Public Sub M(c1 As C, c2 As C, m As Func(Of Integer))'BIND:"Public Sub M(c1 As C, c2 As C, m As Func(Of Integer))"
        m = AddressOf If(c1, c2).M1
    End Sub
End Class
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (2)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'm')
          Value: 
            IParameterReferenceOperation: m (OperationKind.ParameterReference, Type: System.Func(Of System.Int32)) (Syntax: 'm')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
          Value: 
            IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C) (Syntax: 'c1')

    Jump if True (Regular) to Block[B3]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c1')
          Operand: 
            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
          Value: 
            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c2')
          Value: 
            IParameterReferenceOperation: c2 (OperationKind.ParameterReference, Type: C) (Syntax: 'c2')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'm = Address ... (c1, c2).M1')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Func(Of System.Int32), IsImplicit) (Syntax: 'm = Address ... (c1, c2).M1')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Func(Of System.Int32), IsImplicit) (Syntax: 'm')
              Right: 
                IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32), IsImplicit) (Syntax: 'AddressOf If(c1, c2).M1')
                  Target: 
                    IMethodReferenceOperation: Function C.M1() As System.Int32 (OperationKind.MethodReference, Type: null) (Syntax: 'AddressOf If(c1, c2).M1')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'If(c1, c2)')

    Next (Regular) Block[B5]
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact>
        Public Sub MethodReference_ControlFlowInReceiver_StaticMethod()
            Dim source = <![CDATA[
Imports System

Friend Class C
    Public Shared Function M1() As Integer
        Return 0
    End Function

    Public Sub M(c1 As C, c2 As C, m1 As Func(Of Integer), m2 As Func(Of Integer))'BIND:"Public Sub M(c1 As C, c2 As C, m1 As Func(Of Integer), m2 As Func(Of Integer))"
        m1 = AddressOf c1.M1
        m2 = AddressOf If(c1, c2).M1
    End Sub
End Class
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (2)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'm1 = AddressOf c1.M1')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Func(Of System.Int32), IsImplicit) (Syntax: 'm1 = AddressOf c1.M1')
              Left: 
                IParameterReferenceOperation: m1 (OperationKind.ParameterReference, Type: System.Func(Of System.Int32)) (Syntax: 'm1')
              Right: 
                IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32), IsImplicit) (Syntax: 'AddressOf c1.M1')
                  Target: 
                    IMethodReferenceOperation: Function C.M1() As System.Int32 (Static) (OperationKind.MethodReference, Type: null) (Syntax: 'AddressOf c1.M1')
                      Instance Receiver: 
                        null

        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'm2 = Addres ... (c1, c2).M1')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Func(Of System.Int32), IsImplicit) (Syntax: 'm2 = Addres ... (c1, c2).M1')
              Left: 
                IParameterReferenceOperation: m2 (OperationKind.ParameterReference, Type: System.Func(Of System.Int32)) (Syntax: 'm2')
              Right: 
                IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32), IsImplicit) (Syntax: 'AddressOf If(c1, c2).M1')
                  Target: 
                    IMethodReferenceOperation: Function C.M1() As System.Int32 (Static) (OperationKind.MethodReference, Type: null) (Syntax: 'AddressOf If(c1, c2).M1')
                      Instance Receiver: 
                        null

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        m1 = AddressOf c1.M1
             ~~~~~~~~~~~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        m2 = AddressOf If(c1, c2).M1
             ~~~~~~~~~~~~~~~~~~~~~~~
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub
    End Class
End Namespace
