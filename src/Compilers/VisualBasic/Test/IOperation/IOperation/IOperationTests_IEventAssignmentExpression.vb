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
        Public Sub AddEventHandler()
            Dim source = <![CDATA[
Imports System

Class TestClass
    
    Event TestEvent As Action

    Sub Add()
        AddHandler TestEvent, AddressOf M'BIND:"AddHandler TestEvent, AddressOf M"
    End Sub

    Sub M()
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'AddHandler  ... AddressOf M')
  Expression: 
    IEventAssignmentOperation (EventAdd) (OperationKind.EventAssignment, Type: null, IsImplicit) (Syntax: 'AddHandler  ... AddressOf M')
      Event Reference: 
        IEventReferenceOperation: Event TestClass.TestEvent As System.Action (OperationKind.EventReference, Type: System.Action) (Syntax: 'TestEvent')
          Instance Receiver: 
            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: TestClass, IsImplicit) (Syntax: 'TestEvent')
      Handler: 
        IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action, IsImplicit) (Syntax: 'AddressOf M')
          Target: 
            IMethodReferenceOperation: Sub TestClass.M() (OperationKind.MethodReference, Type: null) (Syntax: 'AddressOf M')
              Instance Receiver: 
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: TestClass, IsImplicit) (Syntax: 'M')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of AddRemoveHandlerStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub RemoveEventHandler()
            Dim source = <![CDATA[
Imports System

Class TestClass
    
    Event TestEvent As Action

    Sub Remove()
        RemoveHandler TestEvent, AddressOf M'BIND:"RemoveHandler TestEvent, AddressOf M"
    End Sub

    Sub M()
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'RemoveHandl ... AddressOf M')
  Expression: 
    IEventAssignmentOperation (EventRemove) (OperationKind.EventAssignment, Type: null, IsImplicit) (Syntax: 'RemoveHandl ... AddressOf M')
      Event Reference: 
        IEventReferenceOperation: Event TestClass.TestEvent As System.Action (OperationKind.EventReference, Type: System.Action) (Syntax: 'TestEvent')
          Instance Receiver: 
            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: TestClass, IsImplicit) (Syntax: 'TestEvent')
      Handler: 
        IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action, IsImplicit) (Syntax: 'AddressOf M')
          Target: 
            IMethodReferenceOperation: Sub TestClass.M() (OperationKind.MethodReference, Type: null) (Syntax: 'AddressOf M')
              Instance Receiver: 
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: TestClass, IsImplicit) (Syntax: 'M')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of AddRemoveHandlerStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub AddEventHandler_StaticEvent()
            Dim source = <![CDATA[
Imports System

Class TestClass
    
    Shared Event TestEvent As Action

    Sub Add()
        AddHandler TestEvent, AddressOf M'BIND:"AddHandler TestEvent, AddressOf M"
    End Sub

    Sub M()
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'AddHandler  ... AddressOf M')
  Expression: 
    IEventAssignmentOperation (EventAdd) (OperationKind.EventAssignment, Type: null, IsImplicit) (Syntax: 'AddHandler  ... AddressOf M')
      Event Reference: 
        IEventReferenceOperation: Event TestClass.TestEvent As System.Action (Static) (OperationKind.EventReference, Type: System.Action) (Syntax: 'TestEvent')
          Instance Receiver: 
            null
      Handler: 
        IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action, IsImplicit) (Syntax: 'AddressOf M')
          Target: 
            IMethodReferenceOperation: Sub TestClass.M() (OperationKind.MethodReference, Type: null) (Syntax: 'AddressOf M')
              Instance Receiver: 
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: TestClass, IsImplicit) (Syntax: 'M')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of AddRemoveHandlerStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub RemoveEventHandler_StaticEvent()
            Dim source = <![CDATA[
Imports System

Class TestClass
    
    Shared Event TestEvent As Action

    Sub Remove()
        RemoveHandler TestEvent, AddressOf M'BIND:"RemoveHandler TestEvent, AddressOf M"
    End Sub

    Sub M()
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'RemoveHandl ... AddressOf M')
  Expression: 
    IEventAssignmentOperation (EventRemove) (OperationKind.EventAssignment, Type: null, IsImplicit) (Syntax: 'RemoveHandl ... AddressOf M')
      Event Reference: 
        IEventReferenceOperation: Event TestClass.TestEvent As System.Action (Static) (OperationKind.EventReference, Type: System.Action) (Syntax: 'TestEvent')
          Instance Receiver: 
            null
      Handler: 
        IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action, IsImplicit) (Syntax: 'AddressOf M')
          Target: 
            IMethodReferenceOperation: Sub TestClass.M() (OperationKind.MethodReference, Type: null) (Syntax: 'AddressOf M')
              Instance Receiver: 
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: TestClass, IsImplicit) (Syntax: 'M')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of AddRemoveHandlerStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub RemoveEventHandler_DelegateTypeMismatch()
            Dim source = <![CDATA[
Imports System

Class TestClass
    
    Shared Event TestEvent As Action

    Sub Remove()
        RemoveHandler TestEvent, AddressOf M'BIND:"RemoveHandler TestEvent, AddressOf M"
    End Sub

    Sub M(x as Integer)
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'RemoveHandl ... AddressOf M')
  Expression: 
    IEventAssignmentOperation (EventRemove) (OperationKind.EventAssignment, Type: null, IsInvalid, IsImplicit) (Syntax: 'RemoveHandl ... AddressOf M')
      Event Reference: 
        IEventReferenceOperation: Event TestClass.TestEvent As System.Action (Static) (OperationKind.EventReference, Type: System.Action) (Syntax: 'TestEvent')
          Instance Receiver: 
            null
      Handler: 
        IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action, IsInvalid, IsImplicit) (Syntax: 'AddressOf M')
          Target: 
            IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'AddressOf M')
              Children(1):
                  IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'M')
                    Children(1):
                        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: TestClass, IsInvalid, IsImplicit) (Syntax: 'M')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC31143: Method 'Public Sub M(x As Integer)' does not have a signature compatible with delegate 'Delegate Sub Action()'.
        RemoveHandler TestEvent, AddressOf M'BIND:"RemoveHandler TestEvent, AddressOf M"
                                           ~
]]>.Value
            VerifyOperationTreeAndDiagnosticsForTest(Of AddRemoveHandlerStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub AddEventHandler_AssignToSharedEventOnInstance()
            Dim source = <![CDATA[
Imports System

Class TestClass

    Shared Event TestEvent As Action

    Sub Remove()
        AddHandler Me.TestEvent, AddressOf M 'BIND:"AddHandler Me.TestEvent, AddressOf M"
    End Sub

    Sub M()
    End Sub
End Class]]>.Value
            Dim expectedOperationTree = <![CDATA[
IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'AddHandler  ... AddressOf M')
  Expression: 
    IEventAssignmentOperation (EventAdd) (OperationKind.EventAssignment, Type: null, IsImplicit) (Syntax: 'AddHandler  ... AddressOf M')
      Event Reference: 
        IEventReferenceOperation: Event TestClass.TestEvent As System.Action (Static) (OperationKind.EventReference, Type: System.Action) (Syntax: 'Me.TestEvent')
          Instance Receiver: 
            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: TestClass) (Syntax: 'Me')
      Handler: 
        IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action, IsImplicit) (Syntax: 'AddressOf M')
          Target: 
            IMethodReferenceOperation: Sub TestClass.M() (OperationKind.MethodReference, Type: null) (Syntax: 'AddressOf M')
              Instance Receiver: 
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: TestClass, IsImplicit) (Syntax: 'M')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        AddHandler Me.TestEvent, AddressOf M 'BIND:"AddHandler Me.TestEvent, AddressOf M"
                   ~~~~~~~~~~~~
]]>.Value
            VerifyOperationTreeAndDiagnosticsForTest(Of AddRemoveHandlerStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        <WorkItem(8909, "https://github.com/dotnet/roslyn/issues/8909")>
        Public Sub AddEventHandler_AssignToNonSharedEventOnType()
            Dim source = <![CDATA[
Imports System

Class TestClass

    Event TestEvent As Action

    Sub Remove()
        AddHandler TestClass.TestEvent, AddressOf M 'BIND:"AddHandler TestClass.TestEvent, AddressOf M"
    End Sub

    Sub M()
    End Sub
End Class]]>.Value
            Dim expectedOperationTree = <![CDATA[
IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'AddHandler  ... AddressOf M')
  Expression: 
    IEventAssignmentOperation (EventAdd) (OperationKind.EventAssignment, Type: null, IsInvalid, IsImplicit) (Syntax: 'AddHandler  ... AddressOf M')
      Event Reference: 
        IEventReferenceOperation: Event TestClass.TestEvent As System.Action (OperationKind.EventReference, Type: System.Action, IsInvalid) (Syntax: 'TestClass.TestEvent')
          Instance Receiver: 
            null
      Handler: 
        IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action, IsImplicit) (Syntax: 'AddressOf M')
          Target: 
            IMethodReferenceOperation: Sub TestClass.M() (OperationKind.MethodReference, Type: null) (Syntax: 'AddressOf M')
              Instance Receiver: 
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: TestClass, IsImplicit) (Syntax: 'M')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30469: Reference to a non-shared member requires an object reference.
        AddHandler TestClass.TestEvent, AddressOf M 'BIND:"AddHandler TestClass.TestEvent, AddressOf M"
                   ~~~~~~~~~~~~~~~~~~~
]]>.Value
            VerifyOperationTreeAndDiagnosticsForTest(Of AddRemoveHandlerStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub EventAssignment_NoControlFlow()
            Dim source = <![CDATA[
Imports System

Class C1
    Public Event MyEvent As EventHandler

    Sub M(handler As EventHandler) 'BIND:"Sub M(handler As EventHandler)"
        AddHandler MyEvent, handler
        RemoveHandler MyEvent, handler
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
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'AddHandler  ... nt, handler')
          Expression: 
            IEventAssignmentOperation (EventAdd) (OperationKind.EventAssignment, Type: null, IsImplicit) (Syntax: 'AddHandler  ... nt, handler')
              Event Reference: 
                IEventReferenceOperation: Event C1.MyEvent As System.EventHandler (OperationKind.EventReference, Type: System.EventHandler) (Syntax: 'MyEvent')
                  Instance Receiver: 
                    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C1, IsImplicit) (Syntax: 'MyEvent')
              Handler: 
                IParameterReferenceOperation: handler (OperationKind.ParameterReference, Type: System.EventHandler) (Syntax: 'handler')

        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'RemoveHandl ... nt, handler')
          Expression: 
            IEventAssignmentOperation (EventRemove) (OperationKind.EventAssignment, Type: null, IsImplicit) (Syntax: 'RemoveHandl ... nt, handler')
              Event Reference: 
                IEventReferenceOperation: Event C1.MyEvent As System.EventHandler (OperationKind.EventReference, Type: System.EventHandler) (Syntax: 'MyEvent')
                  Instance Receiver: 
                    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C1, IsImplicit) (Syntax: 'MyEvent')
              Handler: 
                IParameterReferenceOperation: handler (OperationKind.ParameterReference, Type: System.EventHandler) (Syntax: 'handler')

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub EventAssignment_ControlFlowInEventReference()
            Dim source = <![CDATA[
Imports System

Class C1
    Public Event MyEvent As EventHandler

    Sub M(c As C1, c2 As C1, handler As EventHandler) 'BIND:"Sub M(c As C1, c2 As C1, handler As EventHandler)"
        AddHandler If(c, c2).MyEvent, handler
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
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
                  Value: 
                    IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C1) (Syntax: 'c')

            Jump if True (Regular) to Block[B3]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c')
                  Operand: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'c')
                Leaving: {R2}

            Next (Regular) Block[B2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
                  Value: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'c')

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
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'AddHandler  ... nt, handler')
              Expression: 
                IEventAssignmentOperation (EventAdd) (OperationKind.EventAssignment, Type: null, IsImplicit) (Syntax: 'AddHandler  ... nt, handler')
                  Event Reference: 
                    IEventReferenceOperation: Event C1.MyEvent As System.EventHandler (OperationKind.EventReference, Type: System.EventHandler) (Syntax: 'If(c, c2).MyEvent')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'If(c, c2)')
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
        <Fact()>
        Public Sub EventAssignment_ControlFlowInEventReference_StaticEvent()
            Dim source = <![CDATA[
Imports System

Class C1
    Public Shared Event MyEvent As EventHandler

    Sub M(c As C1, c2 As C1, handler As EventHandler) 'BIND:"Sub M(c As C1, c2 As C1, handler As EventHandler)"
        AddHandler If(c, c2).MyEvent, handler
    End Sub
End Class
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'AddHandler  ... nt, handler')
          Expression: 
            IEventAssignmentOperation (EventAdd) (OperationKind.EventAssignment, Type: null, IsImplicit) (Syntax: 'AddHandler  ... nt, handler')
              Event Reference: 
                IEventReferenceOperation: Event C1.MyEvent As System.EventHandler (Static) (OperationKind.EventReference, Type: System.EventHandler) (Syntax: 'If(c, c2).MyEvent')
                  Instance Receiver: 
                    null
              Handler: 
                IParameterReferenceOperation: handler (OperationKind.ParameterReference, Type: System.EventHandler) (Syntax: 'handler')

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        AddHandler If(c, c2).MyEvent, handler
                   ~~~~~~~~~~~~~~~~~
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub EventAssignment_ControlFlowInHandler_InstanceReceiver()
            Dim source = <![CDATA[
Imports System

Class C1
    Public Event MyEvent As EventHandler

    Sub M(handler1 As EventHandler, handler2 As EventHandler) 'BIND:"Sub M(handler1 As EventHandler, handler2 As EventHandler)"
        RemoveHandler MyEvent, If(handler1, handler2)
    End Sub
End Class
]]>.Value

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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'MyEvent')
              Value: 
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C1, IsImplicit) (Syntax: 'MyEvent')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'handler1')
                  Value: 
                    IParameterReferenceOperation: handler1 (OperationKind.ParameterReference, Type: System.EventHandler) (Syntax: 'handler1')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'handler1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.EventHandler, IsImplicit) (Syntax: 'handler1')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'handler1')
                  Value: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.EventHandler, IsImplicit) (Syntax: 'handler1')

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'handler2')
              Value: 
                IParameterReferenceOperation: handler2 (OperationKind.ParameterReference, Type: System.EventHandler) (Syntax: 'handler2')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'RemoveHandl ... , handler2)')
              Expression: 
                IEventAssignmentOperation (EventRemove) (OperationKind.EventAssignment, Type: null, IsImplicit) (Syntax: 'RemoveHandl ... , handler2)')
                  Event Reference: 
                    IEventReferenceOperation: Event C1.MyEvent As System.EventHandler (OperationKind.EventReference, Type: System.EventHandler) (Syntax: 'MyEvent')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'MyEvent')
                  Handler: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.EventHandler, IsImplicit) (Syntax: 'If(handler1, handler2)')

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
        <Fact()>
        Public Sub EventAssignment_ControlFlowInHandler_NullReceiver()
            Dim source = <![CDATA[
Imports System

Class C1
    Public Shared Event MyEvent As EventHandler

    Sub M(handler1 As EventHandler, handler2 As EventHandler) 'BIND:"Sub M(handler1 As EventHandler, handler2 As EventHandler)"
        RemoveHandler MyEvent, If(handler1, handler2)
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
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'handler1')
                  Value: 
                    IParameterReferenceOperation: handler1 (OperationKind.ParameterReference, Type: System.EventHandler) (Syntax: 'handler1')

            Jump if True (Regular) to Block[B3]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'handler1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.EventHandler, IsImplicit) (Syntax: 'handler1')
                Leaving: {R2}

            Next (Regular) Block[B2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'handler1')
                  Value: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.EventHandler, IsImplicit) (Syntax: 'handler1')

            Next (Regular) Block[B4]
                Leaving: {R2}
    }

    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'handler2')
              Value: 
                IParameterReferenceOperation: handler2 (OperationKind.ParameterReference, Type: System.EventHandler) (Syntax: 'handler2')

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'RemoveHandl ... , handler2)')
              Expression: 
                IEventAssignmentOperation (EventRemove) (OperationKind.EventAssignment, Type: null, IsImplicit) (Syntax: 'RemoveHandl ... , handler2)')
                  Event Reference: 
                    IEventReferenceOperation: Event C1.MyEvent As System.EventHandler (Static) (OperationKind.EventReference, Type: System.EventHandler) (Syntax: 'MyEvent')
                      Instance Receiver: 
                        null
                  Handler: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.EventHandler, IsImplicit) (Syntax: 'If(handler1, handler2)')

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
        <Fact()>
        Public Sub EventAssignment_ControlFlowInEventReferenceAndHandler()
            Dim source = <![CDATA[
Imports System

Class C1
    Public Event MyEvent As EventHandler

    Sub M(c As C1, c2 As C1, handler1 As EventHandler, handler2 As EventHandler) 'BIND:"Sub M(c As C1, c2 As C1, handler1 As EventHandler, handler2 As EventHandler)"
        AddHandler If(c, c2).MyEvent, If(handler1, handler2)
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
    CaptureIds: [1] [3]
    .locals {R2}
    {
        CaptureIds: [0]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (1)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
                  Value: 
                    IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C1) (Syntax: 'c')

            Jump if True (Regular) to Block[B3]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c')
                  Operand: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'c')
                Leaving: {R2}

            Next (Regular) Block[B2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
                  Value: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'c')

            Next (Regular) Block[B4]
                Leaving: {R2}
                Entering: {R3}
    }

    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c2')
              Value: 
                IParameterReferenceOperation: c2 (OperationKind.ParameterReference, Type: C1) (Syntax: 'c2')

        Next (Regular) Block[B4]
            Entering: {R3}

    .locals {R3}
    {
        CaptureIds: [2]
        Block[B4] - Block
            Predecessors: [B2] [B3]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'handler1')
                  Value: 
                    IParameterReferenceOperation: handler1 (OperationKind.ParameterReference, Type: System.EventHandler) (Syntax: 'handler1')

            Jump if True (Regular) to Block[B6]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'handler1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.EventHandler, IsImplicit) (Syntax: 'handler1')
                Leaving: {R3}

            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B4]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'handler1')
                  Value: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.EventHandler, IsImplicit) (Syntax: 'handler1')

            Next (Regular) Block[B7]
                Leaving: {R3}
    }

    Block[B6] - Block
        Predecessors: [B4]
        Statements (1)
            IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'handler2')
              Value: 
                IParameterReferenceOperation: handler2 (OperationKind.ParameterReference, Type: System.EventHandler) (Syntax: 'handler2')

        Next (Regular) Block[B7]
    Block[B7] - Block
        Predecessors: [B5] [B6]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'AddHandler  ... , handler2)')
              Expression: 
                IEventAssignmentOperation (EventAdd) (OperationKind.EventAssignment, Type: null, IsImplicit) (Syntax: 'AddHandler  ... , handler2)')
                  Event Reference: 
                    IEventReferenceOperation: Event C1.MyEvent As System.EventHandler (OperationKind.EventReference, Type: System.EventHandler) (Syntax: 'If(c, c2).MyEvent')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'If(c, c2)')
                  Handler: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.EventHandler, IsImplicit) (Syntax: 'If(handler1, handler2)')

        Next (Regular) Block[B8]
            Leaving: {R1}
}

Block[B8] - Exit
    Predecessors: [B7]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub EventAssignment_ErrorCase_InaccessibleEvent()
            Dim source = <![CDATA[
Public Class Form1
    Private Event EventB As System.Action

End Class

Public Class Form2
    Inherits Form1

    Public Sub M()
        AddHandler MyBase.EventB, Nothing 'BIND:"AddHandler MyBase.EventB, Nothing"
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30389: 'Form1.EventB' is not accessible in this context because it is 'Private'.
        AddHandler MyBase.EventB, Nothing 'BIND:"AddHandler MyBase.EventB, Nothing"
                   ~~~~~~~~~~~~~
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'AddHandler  ... tB, Nothing')
  Expression: 
    IEventAssignmentOperation (EventAdd) (OperationKind.EventAssignment, Type: null, IsInvalid, IsImplicit) (Syntax: 'AddHandler  ... tB, Nothing')
      Event Reference: 
        IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: 'MyBase.EventB')
          Children(1):
              IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Form1, IsInvalid) (Syntax: 'MyBase')
      Handler: 
        ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'Nothing')
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of AddRemoveHandlerStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub EventAssignment_ErrorCase_MissingEvent()
            Dim source = <![CDATA[
Public Class Form1
    Private Event EventB As System.Action

End Class

Public Class Form2
    Inherits Form1

    Public Sub M()
        RemoveHandler EventX, Nothing'BIND:"RemoveHandler EventX, Nothing"
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30451: 'EventX' is not declared. It may be inaccessible due to its protection level.
        RemoveHandler EventX, Nothing'BIND:"RemoveHandler EventX, Nothing"
                      ~~~~~~
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'RemoveHandl ... tX, Nothing')
  Expression: 
    IEventAssignmentOperation (EventRemove) (OperationKind.EventAssignment, Type: null, IsInvalid, IsImplicit) (Syntax: 'RemoveHandl ... tX, Nothing')
      Event Reference: 
        IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'EventX')
          Children(0)
      Handler: 
        ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'Nothing')
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of AddRemoveHandlerStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact>
        Public Sub EventAssignment_ErrorCases_NoControlFlow()
            Dim source = <![CDATA[
Public Class Form1
    Private Event EventB As System.Action

End Class

Public Class Form2
    Inherits Form1

    Public Sub M()'BIND:"Public Sub M()"
        AddHandler MyBase.EventB, Nothing
        RemoveHandler EventX, Nothing
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30389: 'Form1.EventB' is not accessible in this context because it is 'Private'.
        AddHandler MyBase.EventB, Nothing
                   ~~~~~~~~~~~~~
BC30451: 'EventX' is not declared. It may be inaccessible due to its protection level.
        RemoveHandler EventX, Nothing
                      ~~~~~~
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (2)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'AddHandler  ... tB, Nothing')
          Expression: 
            IEventAssignmentOperation (EventAdd) (OperationKind.EventAssignment, Type: null, IsInvalid, IsImplicit) (Syntax: 'AddHandler  ... tB, Nothing')
              Event Reference: 
                IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: 'MyBase.EventB')
                  Children(1):
                      IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Form1, IsInvalid) (Syntax: 'MyBase')
              Handler: 
                ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'Nothing')

        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'RemoveHandl ... tX, Nothing')
          Expression: 
            IEventAssignmentOperation (EventRemove) (OperationKind.EventAssignment, Type: null, IsInvalid, IsImplicit) (Syntax: 'RemoveHandl ... tX, Nothing')
              Event Reference: 
                IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'EventX')
                  Children(0)
              Handler: 
                ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'Nothing')

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact>
        Public Sub EventAssignment_ErrorCases_ControlFlowInHandler()
            Dim source = <![CDATA[
Imports System

Public Class Form1
    Private Event EventB As EventHandler
End Class

Public Class Form2
    Inherits Form1

    Public Sub M(handler1 As EventHandler, handler2 As EventHandler)'BIND:"Public Sub M(handler1 As EventHandler, handler2 As EventHandler)"
        AddHandler MyBase.EventB, If(handler1, handler2)
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30389: 'Form1.EventB' is not accessible in this context because it is 'Private'.
        AddHandler MyBase.EventB, If(handler1, handler2)
                   ~~~~~~~~~~~~~
]]>.Value

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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'MyBase.EventB')
              Value: 
                IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: 'MyBase.EventB')
                  Children(1):
                      IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Form1, IsInvalid) (Syntax: 'MyBase')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'handler1')
                  Value: 
                    IParameterReferenceOperation: handler1 (OperationKind.ParameterReference, Type: System.EventHandler) (Syntax: 'handler1')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'handler1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.EventHandler, IsImplicit) (Syntax: 'handler1')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'handler1')
                  Value: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.EventHandler, IsImplicit) (Syntax: 'handler1')

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'handler2')
              Value: 
                IParameterReferenceOperation: handler2 (OperationKind.ParameterReference, Type: System.EventHandler) (Syntax: 'handler2')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'AddHandler  ... , handler2)')
              Expression: 
                IEventAssignmentOperation (EventAdd) (OperationKind.EventAssignment, Type: null, IsInvalid, IsImplicit) (Syntax: 'AddHandler  ... , handler2)')
                  Event Reference: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: null, IsInvalid, IsImplicit) (Syntax: 'MyBase.EventB')
                  Handler: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.EventHandler, IsImplicit) (Syntax: 'If(handler1, handler2)')

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub EventAssignment_ParenthesizedEventReference()
            Dim source = <![CDATA[
Imports System
Class C
    Event TestEvent As EventHandler

    Public Sub M(c As C, handler As EventHandler)
        AddHandler(c.TestEvent), handler 'BIND:"AddHandler(c.TestEvent), handler"
    End Sub

    Public Sub M2()
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedOperationTree = <![CDATA[
IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'AddHandler( ... t), handler')
  Expression: 
    IEventAssignmentOperation (EventAdd) (OperationKind.EventAssignment, Type: null, IsImplicit) (Syntax: 'AddHandler( ... t), handler')
      Event Reference: 
        IParenthesizedOperation (OperationKind.Parenthesized, Type: System.EventHandler) (Syntax: '(c.TestEvent)')
          Operand: 
            IEventReferenceOperation: Event C.TestEvent As System.EventHandler (OperationKind.EventReference, Type: System.EventHandler) (Syntax: 'c.TestEvent')
              Instance Receiver: 
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C) (Syntax: 'c')
      Handler: 
        IParameterReferenceOperation: handler (OperationKind.ParameterReference, Type: System.EventHandler) (Syntax: 'handler')
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of AddRemoveHandlerStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact>
        Public Sub EventAssignment_ParenthesizedEventReference_NoControlFlow()
            Dim source = <![CDATA[
Imports System
Class C
    Event TestEvent As EventHandler

    Public Sub M(c As C, handler As EventHandler)'BIND:"Public Sub M(c As C, handler As EventHandler)"
        AddHandler(c.TestEvent), handler
    End Sub

    Public Sub M2()
    End Sub
End Class
]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'AddHandler( ... t), handler')
          Expression: 
            IEventAssignmentOperation (EventAdd) (OperationKind.EventAssignment, Type: null, IsImplicit) (Syntax: 'AddHandler( ... t), handler')
              Event Reference: 
                IEventReferenceOperation: Event C.TestEvent As System.EventHandler (OperationKind.EventReference, Type: System.EventHandler) (Syntax: '(c.TestEvent)')
                  Instance Receiver: 
                    IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C) (Syntax: 'c')
              Handler: 
                IParameterReferenceOperation: handler (OperationKind.ParameterReference, Type: System.EventHandler) (Syntax: 'handler')

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact>
        Public Sub EventAssignment_ParenthesizedInvalidEventReference_NoControlFlow()
            Dim source = <![CDATA[
Imports System        
Module M1
    Sub Main(v As AppDomain, handler As EventHandler)'BIND:"Sub Main(v As AppDomain, handler As EventHandler)"
        AddHandler (v.DomainUnload()), handler
    End Sub
End Module
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30677: 'AddHandler' or 'RemoveHandler' statement event operand must be a dot-qualified expression or a simple name.
        AddHandler (v.DomainUnload()), handler
                    ~~~~~~~~~~~~~~~~
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'AddHandler  ... )), handler')
          Expression: 
            IEventAssignmentOperation (EventAdd) (OperationKind.EventAssignment, Type: null, IsInvalid, IsImplicit) (Syntax: 'AddHandler  ... )), handler')
              Event Reference: 
                IParenthesizedOperation (OperationKind.Parenthesized, Type: ?, IsInvalid) (Syntax: '(v.DomainUnload())')
                  Operand: 
                    IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'v.DomainUnload()')
                      Children(1):
                          IEventReferenceOperation: Event System.AppDomain.DomainUnload As System.EventHandler (OperationKind.EventReference, Type: System.EventHandler, IsInvalid) (Syntax: 'v.DomainUnload')
                            Instance Receiver: 
                              IParameterReferenceOperation: v (OperationKind.ParameterReference, Type: System.AppDomain, IsInvalid) (Syntax: 'v')
              Handler: 
                IParameterReferenceOperation: handler (OperationKind.ParameterReference, Type: System.EventHandler) (Syntax: 'handler')

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact>
        Public Sub EventAssignment_ParenthesizedEventReference_ControlFlowInHandler()
            Dim source = <![CDATA[
Imports System
Class C
    Event TestEvent As EventHandler

    Public Sub M(c As C, handler1 As EventHandler, handler2 As EventHandler)'BIND:"Public Sub M(c As C, handler1 As EventHandler, handler2 As EventHandler)"
        AddHandler(c.TestEvent), If(handler1, handler2)
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
    CaptureIds: [0] [2]
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
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'handler1')
                  Value: 
                    IParameterReferenceOperation: handler1 (OperationKind.ParameterReference, Type: System.EventHandler) (Syntax: 'handler1')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'handler1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.EventHandler, IsImplicit) (Syntax: 'handler1')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'handler1')
                  Value: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.EventHandler, IsImplicit) (Syntax: 'handler1')

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'handler2')
              Value: 
                IParameterReferenceOperation: handler2 (OperationKind.ParameterReference, Type: System.EventHandler) (Syntax: 'handler2')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'AddHandler( ... , handler2)')
              Expression: 
                IEventAssignmentOperation (EventAdd) (OperationKind.EventAssignment, Type: null, IsImplicit) (Syntax: 'AddHandler( ... , handler2)')
                  Event Reference: 
                    IEventReferenceOperation: Event C.TestEvent As System.EventHandler (OperationKind.EventReference, Type: System.EventHandler) (Syntax: '(c.TestEvent)')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c')
                  Handler: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.EventHandler, IsImplicit) (Syntax: 'If(handler1, handler2)')

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub
    End Class
End Namespace
