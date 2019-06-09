' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub IEventReference_AddHandlerSharedEvent()
            Dim source = <![CDATA[
Option Strict On
Imports System

Module M1
    Class C1
        Shared Event E1 As EventHandler
        Shared Sub S2()
            AddHandler E1, Sub(sender, args)'BIND:"E1"
                           End Sub
        End Sub
    End Class
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IEventReferenceOperation: Event M1.C1.E1 As System.EventHandler (Static) (OperationKind.EventReference, Type: System.EventHandler) (Syntax: 'E1')
  Instance Receiver: 
    null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of IdentifierNameSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub IEventReference_AddHandlerSharedEventWithInstanceReference()
            Dim source = <![CDATA[
Option Strict On
Imports System

Module M1
    Class C1
        Shared Event E1 As EventHandler
        Shared Sub S2()
            Dim c1Instance As New C1
            AddHandler c1Instance.E1, Sub(sender, arg) Console.WriteLine()'BIND:"c1Instance.E1"
        End Sub
    End Class
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IEventReferenceOperation: Event M1.C1.E1 As System.EventHandler (Static) (OperationKind.EventReference, Type: System.EventHandler) (Syntax: 'c1Instance.E1')
  Instance Receiver: 
    ILocalReferenceOperation: c1Instance (OperationKind.LocalReference, Type: M1.C1) (Syntax: 'c1Instance')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
            AddHandler c1Instance.E1, Sub(sender, arg) Console.WriteLine()'BIND:"c1Instance.E1"
                       ~~~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of MemberAccessExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub IEventReference_AddHandlerSharedEventAccessOnClass()
            Dim source = <![CDATA[
Option Strict On
Imports System

Module M1
    Class C1
        Shared Event E1 As EventHandler
        Shared Sub S2()
            Dim c1Instance As New C1
            AddHandler C1.E1, Sub(sender, arg) Console.WriteLine()'BIND:"C1.E1"
        End Sub
    End Class
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IEventReferenceOperation: Event M1.C1.E1 As System.EventHandler (Static) (OperationKind.EventReference, Type: System.EventHandler) (Syntax: 'C1.E1')
  Instance Receiver: 
    null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of MemberAccessExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub IEventReference_AddHandlerInstanceEventAccessOnClass()
            Dim source = <![CDATA[
Option Strict On
Imports System

Module M1
    Class C1
        Event E1 As EventHandler
        Shared Sub S2()
            Dim c1Instance As New C1
            AddHandler C1.E1, Sub(sender, arg) Console.WriteLine()'BIND:"C1.E1"
        End Sub
    End Class
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IEventReferenceOperation: Event M1.C1.E1 As System.EventHandler (OperationKind.EventReference, Type: System.EventHandler, IsInvalid) (Syntax: 'C1.E1')
  Instance Receiver: 
    null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30469: Reference to a non-shared member requires an object reference.
            AddHandler C1.E1, Sub(sender, arg) Console.WriteLine()'BIND:"C1.E1"
                       ~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of MemberAccessExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact>
        Public Sub EventReference_NoControlFlow()
            ' Verify event references with different kinds of instance references.
            Dim source = <![CDATA[
Option Strict On
Imports System

Class C1
    Public Event Event1 As EventHandler
    Public Shared Event Event2 As EventHandler

    Public Sub M1(c As C1, handler1 As EventHandler, handler2 As EventHandler, handler3 As EventHandler)'BIND:"Public Sub M1(c As C1, handler1 As EventHandler, handler2 As EventHandler, handler3 As EventHandler)"
        AddHandler Me.Event1, handler1
        AddHandler c.Event1, handler2
        AddHandler Event2, handler3
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
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'AddHandler  ... 1, handler1')
          Expression: 
            IEventAssignmentOperation (EventAdd) (OperationKind.EventAssignment, Type: null, IsImplicit) (Syntax: 'AddHandler  ... 1, handler1')
              Event Reference: 
                IEventReferenceOperation: Event C1.Event1 As System.EventHandler (OperationKind.EventReference, Type: System.EventHandler) (Syntax: 'Me.Event1')
                  Instance Receiver: 
                    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C1) (Syntax: 'Me')
              Handler: 
                IParameterReferenceOperation: handler1 (OperationKind.ParameterReference, Type: System.EventHandler) (Syntax: 'handler1')

        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'AddHandler  ... 1, handler2')
          Expression: 
            IEventAssignmentOperation (EventAdd) (OperationKind.EventAssignment, Type: null, IsImplicit) (Syntax: 'AddHandler  ... 1, handler2')
              Event Reference: 
                IEventReferenceOperation: Event C1.Event1 As System.EventHandler (OperationKind.EventReference, Type: System.EventHandler) (Syntax: 'c.Event1')
                  Instance Receiver: 
                    IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C1) (Syntax: 'c')
              Handler: 
                IParameterReferenceOperation: handler2 (OperationKind.ParameterReference, Type: System.EventHandler) (Syntax: 'handler2')

        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'AddHandler  ... 2, handler3')
          Expression: 
            IEventAssignmentOperation (EventAdd) (OperationKind.EventAssignment, Type: null, IsImplicit) (Syntax: 'AddHandler  ... 2, handler3')
              Event Reference: 
                IEventReferenceOperation: Event C1.Event2 As System.EventHandler (Static) (OperationKind.EventReference, Type: System.EventHandler) (Syntax: 'Event2')
                  Instance Receiver: 
                    null
              Handler: 
                IParameterReferenceOperation: handler3 (OperationKind.ParameterReference, Type: System.EventHandler) (Syntax: 'handler3')

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
        Public Sub EventReference_ControlFlowInReceiver()
            Dim source = <![CDATA[
Option Strict On
Imports System

Class C1
    Public Event Event1 As EventHandler

    Public Sub M1(c1 As C1, c2 As C1, handler As EventHandler) 'BIND:"Public Sub M1(c1 As C1, c2 As C1, handler As EventHandler)"
        AddHandler If(c1, c2).Event1, handler
    End Sub
End Class
]]>.Value

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
                    IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C1) (Syntax: 'c1')

            Jump if True (Regular) to Block[B3]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'c1')
                Leaving: {R2}

            Next (Regular) Block[B2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                  Value: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'c1')

            Next (Regular) Block[B4]
                Leaving: {R2}
    }

    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c2')
              Value: 
                IParameterReferenceOperation: c2 (OperationKind.ParameterReference, Type: C1) (Syntax: 'c2')

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'AddHandler  ... t1, handler')
              Expression: 
                IEventAssignmentOperation (EventAdd) (OperationKind.EventAssignment, Type: null, IsImplicit) (Syntax: 'AddHandler  ... t1, handler')
                  Event Reference: 
                    IEventReferenceOperation: Event C1.Event1 As System.EventHandler (OperationKind.EventReference, Type: System.EventHandler) (Syntax: 'If(c1, c2).Event1')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'If(c1, c2)')
                  Handler: 
                    IParameterReferenceOperation: handler (OperationKind.ParameterReference, Type: System.EventHandler) (Syntax: 'handler')

        Next (Regular) Block[B5]
            Leaving: {R1}
}

Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact>
        Public Sub EventReference_ControlFlowInReceiver_StaticEvent()
            Dim source = <![CDATA[
Option Strict On
Imports System

Class C1
    Public Shared Event Event1 As EventHandler

    Public Sub M1(c As C1, c2 As C1, handler1 As EventHandler, handler2 As EventHandler) 'BIND:"Public Sub M1(c As C1, c2 As C1, handler1 As EventHandler, handler2 As EventHandler)"
        AddHandler c.Event1, handler1
        AddHandler If(c, c2).Event1, handler2
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
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'AddHandler  ... 1, handler1')
          Expression: 
            IEventAssignmentOperation (EventAdd) (OperationKind.EventAssignment, Type: null, IsImplicit) (Syntax: 'AddHandler  ... 1, handler1')
              Event Reference: 
                IEventReferenceOperation: Event C1.Event1 As System.EventHandler (Static) (OperationKind.EventReference, Type: System.EventHandler) (Syntax: 'c.Event1')
                  Instance Receiver: 
                    null
              Handler: 
                IParameterReferenceOperation: handler1 (OperationKind.ParameterReference, Type: System.EventHandler) (Syntax: 'handler1')

        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'AddHandler  ... 1, handler2')
          Expression: 
            IEventAssignmentOperation (EventAdd) (OperationKind.EventAssignment, Type: null, IsImplicit) (Syntax: 'AddHandler  ... 1, handler2')
              Event Reference: 
                IEventReferenceOperation: Event C1.Event1 As System.EventHandler (Static) (OperationKind.EventReference, Type: System.EventHandler) (Syntax: 'If(c, c2).Event1')
                  Instance Receiver: 
                    null
              Handler: 
                IParameterReferenceOperation: handler2 (OperationKind.ParameterReference, Type: System.EventHandler) (Syntax: 'handler2')

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        AddHandler c.Event1, handler1
                   ~~~~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        AddHandler If(c, c2).Event1, handler2
                   ~~~~~~~~~~~~~~~~
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub
    End Class
End Namespace
