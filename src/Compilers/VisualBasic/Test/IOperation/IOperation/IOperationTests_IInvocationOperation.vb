' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub IInvocation_SharedMethodWithInstanceReceiver()
            Dim source = <![CDATA[
Option Strict On

Module M1
    Class C1
        Shared Sub S1()
        End Sub
        Shared Sub S2()
            Dim c1Instance As New C1
            c1Instance.S1()'BIND:"c1Instance.S1()"
        End Sub
    End Class
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInvocationOperation ( Sub M1.C1.S1()) (OperationKind.Invocation, Type: System.Void) (Syntax: 'c1Instance.S1()')
  Instance Receiver: 
    ILocalReferenceOperation: c1Instance (OperationKind.LocalReference, Type: M1.C1) (Syntax: 'c1Instance')
  Arguments(0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
            c1Instance.S1()'BIND:"c1Instance.S1()"
            ~~~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub IInvocation_SharedMethodAccessOnClass()
            Dim source = <![CDATA[
Option Strict On

Module M1
    Class C1
        Shared Sub S1()
        End Sub
        Shared Sub S2()
            C1.S1()'BIND:"C1.S1()"
        End Sub
    End Class
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInvocationOperation (Sub M1.C1.S1()) (OperationKind.Invocation, Type: System.Void) (Syntax: 'C1.S1()')
  Instance Receiver: 
    null
  Arguments(0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub IInvocation_InstanceMethodAccessOnClass()
            Dim source = <![CDATA[
Option Strict On

Module M1
    Class C1
        Sub S1()
        End Sub
        Shared Sub S2()
            C1.S1()'BIND:"C1.S1()"
        End Sub
    End Class
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInvocationOperation (Sub M1.C1.S1()) (OperationKind.Invocation, Type: System.Void, IsInvalid) (Syntax: 'C1.S1()')
  Instance Receiver: 
    null
  Arguments(0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30469: Reference to a non-shared member requires an object reference.
            C1.S1()'BIND:"C1.S1()"
            ~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub InvocationFlow_01()
            Dim source = <![CDATA[
Class C
    Public Sub M1(o1 As Object, o2 As Object, o3 As Object, o4 As Object, b As Boolean)'BIND:"Public Sub M1(o1 As Object, o2 As Object, o3 As Object, o4 As Object, b As Boolean)"
        M2(o1, o2, If(b, o3, o4))
    End Sub
    Public Sub M2(o1 As Object, o2 As Object, o3 As Object)
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (3)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'M2')
          Value: 
            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'M2')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'o1')
          Value: 
            IParameterReferenceOperation: o1 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o1')

        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'o2')
          Value: 
            IParameterReferenceOperation: o2 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o2')

    Jump if False (Regular) to Block[B3]
        IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'o3')
          Value: 
            IParameterReferenceOperation: o3 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o3')

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'o4')
          Value: 
            IParameterReferenceOperation: o4 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o4')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'M2(o1, o2,  ... b, o3, o4))')
          Expression: 
            IInvocationOperation ( Sub C.M2(o1 As System.Object, o2 As System.Object, o3 As System.Object)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'M2(o1, o2,  ... b, o3, o4))')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'M2')
              Arguments(3):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: o1) (OperationKind.Argument, Type: null) (Syntax: 'o1')
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'o1')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: o2) (OperationKind.Argument, Type: null) (Syntax: 'o2')
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'o2')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: o3) (OperationKind.Argument, Type: null) (Syntax: 'If(b, o3, o4)')
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'If(b, o3, o4)')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

    Next (Regular) Block[B5]
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub InvocationFlow_02()
            Dim source = <![CDATA[
Class C
    Public Sub M1(o1 As Object, o2 As Object, o3 As Object, o4 As Object, b As Boolean)'BIND:"Public Sub M1(o1 As Object, o2 As Object, o3 As Object, o4 As Object, b As Boolean)"
        M2(o1, o2, If(b, o3, o4))
    End Sub
    Public Shared Sub M2(o1 As Object, o2 As Object, o3 As Object)
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
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'o1')
          Value: 
            IParameterReferenceOperation: o1 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o1')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'o2')
          Value: 
            IParameterReferenceOperation: o2 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o2')

    Jump if False (Regular) to Block[B3]
        IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'o3')
          Value: 
            IParameterReferenceOperation: o3 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o3')

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'o4')
          Value: 
            IParameterReferenceOperation: o4 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o4')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'M2(o1, o2,  ... b, o3, o4))')
          Expression: 
            IInvocationOperation (Sub C.M2(o1 As System.Object, o2 As System.Object, o3 As System.Object)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'M2(o1, o2,  ... b, o3, o4))')
              Instance Receiver: 
                null
              Arguments(3):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: o1) (OperationKind.Argument, Type: null) (Syntax: 'o1')
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'o1')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: o2) (OperationKind.Argument, Type: null) (Syntax: 'o2')
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'o2')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: o3) (OperationKind.Argument, Type: null) (Syntax: 'If(b, o3, o4)')
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'If(b, o3, o4)')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

    Next (Regular) Block[B5]
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub InvocationFlow_03()
            Dim source = <![CDATA[
Class C
    Public Sub M1(o1 As Object, o2 As Object, b As Boolean)'BIND:"Public Sub M1(o1 As Object, o2 As Object, b As Boolean)"
        Dim x = If(b, o1, o2).ToString()
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
    Locals: [x As System.String]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (0)
        Jump if False (Regular) to Block[B3]
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'o1')
              Value: 
                IParameterReferenceOperation: o1 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o1')

        Next (Regular) Block[B4]
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'o2')
              Value: 
                IParameterReferenceOperation: o2 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o2')

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String, IsImplicit) (Syntax: 'x = If(b, o ... .ToString()')
              Left: 
                ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: System.String, IsImplicit) (Syntax: 'x')
              Right: 
                IInvocationOperation (virtual Function System.Object.ToString() As System.String) (OperationKind.Invocation, Type: System.String) (Syntax: 'If(b, o1, o2).ToString()')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'If(b, o1, o2)')
                  Arguments(0)

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
        Public Sub InvocationFlow_04()
            Dim source = <![CDATA[
Class C
    Public Sub M1(o1 As Object, o2 As Object, o3 As Object, o4 As Object, b As Boolean)'BIND:"Public Sub M1(o1 As Object, o2 As Object, o3 As Object, o4 As Object, b As Boolean)"
        M2(o3:=o3, o2:=o2, o1:=If(b, o1, o4))
    End Sub
    Public Shared Sub M2(o1 As Object, o2 As Object, o3 As Object)
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
    Jump if False (Regular) to Block[B3]
        IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'o1')
          Value: 
            IParameterReferenceOperation: o1 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o1')

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'o4')
          Value: 
            IParameterReferenceOperation: o4 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o4')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'M2(o3:=o3,  ... b, o1, o4))')
          Expression: 
            IInvocationOperation (Sub C.M2(o1 As System.Object, o2 As System.Object, o3 As System.Object)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'M2(o3:=o3,  ... b, o1, o4))')
              Instance Receiver: 
                null
              Arguments(3):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: o1) (OperationKind.Argument, Type: null) (Syntax: 'o1:=If(b, o1, o4)')
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'If(b, o1, o4)')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: o2) (OperationKind.Argument, Type: null) (Syntax: 'o2:=o2')
                    IParameterReferenceOperation: o2 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o2')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: o3) (OperationKind.Argument, Type: null) (Syntax: 'o3:=o3')
                    IParameterReferenceOperation: o3 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o3')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

    Next (Regular) Block[B5]
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub InvocationFlow_05()
            Dim source = <![CDATA[
Class C
    Public Sub M1(o1 As Object, o2 As Object, o3 As Object, o4 As Object, b As Boolean)'BIND:"Public Sub M1(o1 As Object, o2 As Object, o3 As Object, o4 As Object, b As Boolean)"
        M2(o3:=If(b, o3, o4), o2:=o2, o1:=o1)
    End Sub
    Public Shared Sub M2(o1 As Object, o2 As Object, o3 As Object)
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
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'o1')
          Value: 
            IParameterReferenceOperation: o1 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o1')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'o2')
          Value: 
            IParameterReferenceOperation: o2 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o2')

    Jump if False (Regular) to Block[B3]
        IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'o3')
          Value: 
            IParameterReferenceOperation: o3 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o3')

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'o4')
          Value: 
            IParameterReferenceOperation: o4 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o4')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'M2(o3:=If(b ... o2, o1:=o1)')
          Expression: 
            IInvocationOperation (Sub C.M2(o1 As System.Object, o2 As System.Object, o3 As System.Object)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'M2(o3:=If(b ... o2, o1:=o1)')
              Instance Receiver: 
                null
              Arguments(3):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: o1) (OperationKind.Argument, Type: null) (Syntax: 'o1:=o1')
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'o1')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: o2) (OperationKind.Argument, Type: null) (Syntax: 'o2:=o2')
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'o2')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: o3) (OperationKind.Argument, Type: null) (Syntax: 'o3:=If(b, o3, o4)')
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'If(b, o3, o4)')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

    Next (Regular) Block[B5]
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub InvocationFlow_06()
            Dim source = <![CDATA[
Class C
    Public Sub M1(o1 As Object, o2 As Object, o3 As Object, b As Boolean)'BIND:"Public Sub M1(o1 As Object, o2 As Object, o3 As Object, b As Boolean)"
        M2(o2:=If(b, o2, o3), o1:=o1)
    End Sub
    Public Shared Sub M2(o1 As Object, o2 As Object, Optional o3 As Object = Nothing)
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
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'o1')
          Value: 
            IParameterReferenceOperation: o1 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o1')

    Jump if False (Regular) to Block[B3]
        IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'o2')
          Value: 
            IParameterReferenceOperation: o2 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o2')

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'o3')
          Value: 
            IParameterReferenceOperation: o3 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o3')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'M2(o2:=If(b ... 3), o1:=o1)')
          Expression: 
            IInvocationOperation (Sub C.M2(o1 As System.Object, o2 As System.Object, [o3 As System.Object = Nothing])) (OperationKind.Invocation, Type: System.Void) (Syntax: 'M2(o2:=If(b ... 3), o1:=o1)')
              Instance Receiver: 
                null
              Arguments(3):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: o1) (OperationKind.Argument, Type: null) (Syntax: 'o1:=o1')
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'o1')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: o2) (OperationKind.Argument, Type: null) (Syntax: 'o2:=If(b, o2, o3)')
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'If(b, o2, o3)')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: o3) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'M2')
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, Constant: null, IsImplicit) (Syntax: 'M2')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (WideningNothingLiteral)
                      Operand: 
                        ILiteralOperation (OperationKind.Literal, Type: null, Constant: null, IsImplicit) (Syntax: 'M2')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

    Next (Regular) Block[B5]
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub InvocationFlow_07()
            Dim source = <![CDATA[
Class C
    Public Shared Sub M1(o1 As Object, o2 As Object, o3 As Object, b As Boolean)'BIND:"Public Shared Sub M1(o1 As Object, o2 As Object, o3 As Object, b As Boolean)"
        C.M2(o2:=If(b, o2, o3), o1:=o1)
    End Sub
    Public Sub M2(o1 As Object, o2 As Object, Optional o3 As Object = Nothing)
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30469: Reference to a non-shared member requires an object reference.
        C.M2(o2:=If(b, o2, o3), o1:=o1)
        ~~~~
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'o1')
          Value: 
            IParameterReferenceOperation: o1 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o1')

    Jump if False (Regular) to Block[B3]
        IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'o2')
          Value: 
            IParameterReferenceOperation: o2 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o2')

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'o3')
          Value: 
            IParameterReferenceOperation: o3 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o3')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'C.M2(o2:=If ... 3), o1:=o1)')
          Expression: 
            IInvocationOperation (Sub C.M2(o1 As System.Object, o2 As System.Object, [o3 As System.Object = Nothing])) (OperationKind.Invocation, Type: System.Void, IsInvalid) (Syntax: 'C.M2(o2:=If ... 3), o1:=o1)')
              Instance Receiver: 
                null
              Arguments(3):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: o1) (OperationKind.Argument, Type: null) (Syntax: 'o1:=o1')
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'o1')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: o2) (OperationKind.Argument, Type: null) (Syntax: 'o2:=If(b, o2, o3)')
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'If(b, o2, o3)')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: o3) (OperationKind.Argument, Type: null, IsInvalid, IsImplicit) (Syntax: 'C.M2')
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, Constant: null, IsInvalid, IsImplicit) (Syntax: 'C.M2')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (WideningNothingLiteral)
                      Operand: 
                        ILiteralOperation (OperationKind.Literal, Type: null, Constant: null, IsInvalid, IsImplicit) (Syntax: 'C.M2')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

    Next (Regular) Block[B5]
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub InvocationFlow_08()
            Dim source = <![CDATA[
Class C
    Public Sub M1(c1 As C, c2 As C, o1 As Object, o2 As Object) 'BIND:"Public Sub M1(c1 As C, c2 As C, o1 As Object, o2 As Object)"
        c1.M2(o1)
        Call If(c1, c2).M2(o2)
    End Sub
    Public Shared Sub M2(o1 As Object)
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        c1.M2(o1)
        ~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        Call If(c1, c2).M2(o2)
             ~~~~~~~~~~~~~
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (2)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'c1.M2(o1)')
          Expression: 
            IInvocationOperation (Sub C.M2(o1 As System.Object)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'c1.M2(o1)')
              Instance Receiver: 
                null
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: o1) (OperationKind.Argument, Type: null) (Syntax: 'o1')
                    IParameterReferenceOperation: o1 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o1')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Call If(c1, c2).M2(o2)')
          Expression: 
            IInvocationOperation (Sub C.M2(o1 As System.Object)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'If(c1, c2).M2(o2)')
              Instance Receiver: 
                null
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: o1) (OperationKind.Argument, Type: null) (Syntax: 'o2')
                    IParameterReferenceOperation: o2 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o2')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub
    End Class
End Namespace
