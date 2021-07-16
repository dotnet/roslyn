' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub RaiseInstanceEvent()
            Dim source = <![CDATA[
Imports System

Class TestClass
    
    Event TestEvent As Action

    Sub M()
        RaiseEvent TestEvent()'BIND:"RaiseEvent TestEvent()"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IRaiseEventOperation (OperationKind.RaiseEvent, Type: null) (Syntax: 'RaiseEvent TestEvent()')
  Event Reference: 
    IEventReferenceOperation: Event TestClass.TestEvent As System.Action (OperationKind.EventReference, Type: System.Action) (Syntax: 'TestEvent')
      Instance Receiver: 
        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: TestClass, IsImplicit) (Syntax: 'TestEvent')
  Arguments(0)
]]>.Value

            Dim expectedDiagnostics = String.Empty
            VerifyOperationTreeAndDiagnosticsForTest(Of RaiseEventStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub RaiseInstanceEventWithArguments()
            Dim source = <![CDATA[
Imports System

Class TestClass
    
    Public Event MyEvent(x As String, y As Integer)

    Sub M()
        RaiseEvent MyEvent(y:=1, x:=String.Empty)'BIND:"RaiseEvent MyEvent(y:=1, x:=String.Empty)"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IRaiseEventOperation (OperationKind.RaiseEvent, Type: null) (Syntax: 'RaiseEvent  ... ring.Empty)')
  Event Reference: 
    IEventReferenceOperation: Event TestClass.MyEvent(x As System.String, y As System.Int32) (OperationKind.EventReference, Type: TestClass.MyEventEventHandler) (Syntax: 'MyEvent')
      Instance Receiver: 
        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: TestClass, IsImplicit) (Syntax: 'MyEvent')
  Arguments(2):
      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: 'x:=String.Empty')
        IFieldReferenceOperation: System.String.Empty As System.String (Static) (OperationKind.FieldReference, Type: System.String) (Syntax: 'String.Empty')
          Instance Receiver: 
            null
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: y) (OperationKind.Argument, Type: null) (Syntax: 'y:=1')
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = String.Empty
            VerifyOperationTreeAndDiagnosticsForTest(Of RaiseEventStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub RaiseSharedEvent()
            Dim source = <![CDATA[
Imports System

Class TestClass
    
    Shared Event TestEvent As Action

    Sub M()
        RaiseEvent TestEvent()'BIND:"RaiseEvent TestEvent()"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IRaiseEventOperation (OperationKind.RaiseEvent, Type: null) (Syntax: 'RaiseEvent TestEvent()')
  Event Reference: 
    IEventReferenceOperation: Event TestClass.TestEvent As System.Action (Static) (OperationKind.EventReference, Type: System.Action) (Syntax: 'TestEvent')
      Instance Receiver: 
        null
  Arguments(0)
]]>.Value

            Dim expectedDiagnostics = String.Empty
            VerifyOperationTreeAndDiagnosticsForTest(Of RaiseEventStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub RaiseInstanceEventWithExtraArgument()
            Dim source = <![CDATA[
Imports System

Class TestClass
    
    Event TestEvent As Action

    Sub M()
        RaiseEvent TestEvent(1)'BIND:"RaiseEvent TestEvent(1)"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: 'RaiseEvent TestEvent(1)')
  Children(1):
      IInvalidOperation (OperationKind.Invalid, Type: System.Void, IsInvalid, IsImplicit) (Syntax: 'RaiseEvent TestEvent(1)')
        Children(2):
            IOperation:  (OperationKind.None, Type: null, IsInvalid, IsImplicit) (Syntax: 'RaiseEvent TestEvent(1)')
              Children(1):
                  IFieldReferenceOperation: TestClass.TestEventEvent As System.Action (OperationKind.FieldReference, Type: System.Action, IsImplicit) (Syntax: 'TestEvent')
                    Instance Receiver: 
                      IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: TestClass, IsImplicit) (Syntax: 'TestEvent')
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30057: Too many arguments to 'Public Event TestEvent As Action'.
        RaiseEvent TestEvent(1)'BIND:"RaiseEvent TestEvent(1)"
                             ~
]]>.Value
            VerifyOperationTreeAndDiagnosticsForTest(Of RaiseEventStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub RaiseUndefinedEvent()
            Dim source = <![CDATA[
Imports System

Class TestClass
    
    Event TestEvent As Action

    Sub M()
        RaiseEvent TestEvent2()'BIND:"RaiseEvent TestEvent2()"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: 'RaiseEvent TestEvent2()')
  Children(1):
      IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'TestEvent2')
        Children(0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30451: 'TestEvent2' is not declared. It may be inaccessible due to its protection level.
        RaiseEvent TestEvent2()'BIND:"RaiseEvent TestEvent2()"
                   ~~~~~~~~~~
]]>.Value
            VerifyOperationTreeAndDiagnosticsForTest(Of RaiseEventStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub RaiseEventThroughField1()
            Dim source = <![CDATA[
Imports System

Class TestClass
    
    Event TestEvent As Action

    Sub M()
        TestEventEvent()'BIND:"TestEventEvent()"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInvocationOperation (virtual Sub System.Action.Invoke()) (OperationKind.Invocation, Type: System.Void) (Syntax: 'TestEventEvent()')
  Instance Receiver: 
    IFieldReferenceOperation: TestClass.TestEventEvent As System.Action (OperationKind.FieldReference, Type: System.Action) (Syntax: 'TestEventEvent')
      Instance Receiver: 
        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: TestClass, IsImplicit) (Syntax: 'TestEventEvent')
  Arguments(0)
]]>.Value

            Dim expectedDiagnostics = String.Empty
            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub RaiseEventThroughField2()
            Dim source = <![CDATA[
Imports System

Class TestClass
    
    Event TestEvent As Action

    Sub M()
        TestEventEvent.Invoke()'BIND:"TestEventEvent.Invoke()"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInvocationOperation (virtual Sub System.Action.Invoke()) (OperationKind.Invocation, Type: System.Void) (Syntax: 'TestEventEvent.Invoke()')
  Instance Receiver: 
    IFieldReferenceOperation: TestClass.TestEventEvent As System.Action (OperationKind.FieldReference, Type: System.Action) (Syntax: 'TestEventEvent')
      Instance Receiver: 
        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: TestClass, IsImplicit) (Syntax: 'TestEventEvent')
  Arguments(0)
]]>.Value

            Dim expectedDiagnostics = String.Empty
            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub RaiseCustomEvent()
            Dim source = <![CDATA[
Imports System

Class TestClass   
    Public Custom Event TestEvent As Action
        AddHandler(value As Action)
        End AddHandler

        RemoveHandler(value As Action)
        End RemoveHandler

        RaiseEvent()
        End RaiseEvent
	End Event

    Sub M()
        RaiseEvent TestEvent()'BIND:"RaiseEvent TestEvent()"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IRaiseEventOperation (OperationKind.RaiseEvent, Type: null) (Syntax: 'RaiseEvent TestEvent()')
  Event Reference: 
    IEventReferenceOperation: Event TestClass.TestEvent As System.Action (OperationKind.EventReference, Type: System.Action) (Syntax: 'TestEvent')
      Instance Receiver: 
        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: TestClass, IsImplicit) (Syntax: 'TestEvent')
  Arguments(0)
]]>.Value

            Dim expectedDiagnostics = String.Empty
            VerifyOperationTreeAndDiagnosticsForTest(Of RaiseEventStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub RaiseCustomEventWithArguments()
            Dim source = <![CDATA[
Imports System

Class TestClass   
    Public Custom Event TestEvent As Eventhandler
        AddHandler(value As Eventhandler)
        End AddHandler

        RemoveHandler(value As Eventhandler)
        End RemoveHandler

        RaiseEvent(sender As Object, e As EventArgs)
        End RaiseEvent
	End Event

    Sub M()
        RaiseEvent TestEvent(Nothing, Nothing)'BIND:"RaiseEvent TestEvent(Nothing, Nothing)"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IRaiseEventOperation (OperationKind.RaiseEvent, Type: null) (Syntax: 'RaiseEvent  ... g, Nothing)')
  Event Reference: 
    IEventReferenceOperation: Event TestClass.TestEvent As System.EventHandler (OperationKind.EventReference, Type: System.EventHandler) (Syntax: 'TestEvent')
      Instance Receiver: 
        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: TestClass, IsImplicit) (Syntax: 'TestEvent')
  Arguments(2):
      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: sender) (OperationKind.Argument, Type: null) (Syntax: 'Nothing')
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, Constant: null, IsImplicit) (Syntax: 'Nothing')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'Nothing')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: e) (OperationKind.Argument, Type: null) (Syntax: 'Nothing')
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.EventArgs, Constant: null, IsImplicit) (Syntax: 'Nothing')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'Nothing')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = String.Empty
            VerifyOperationTreeAndDiagnosticsForTest(Of RaiseEventStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub EventAccessFromRaiseEventShouldReturnEventReference()
            Dim source = <![CDATA[
Imports System

Class TestClass   
   Event TestEvent As Action

    Sub M()
        RaiseEvent TestEvent()'BIND:"TestEvent"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IEventReferenceOperation: Event TestClass.TestEvent As System.Action (OperationKind.EventReference, Type: System.Action) (Syntax: 'TestEvent')
  Instance Receiver: 
    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: TestClass, IsImplicit) (Syntax: 'TestEvent')
]]>.Value
            Dim expectedDiagnostics = String.Empty
            VerifyOperationTreeAndDiagnosticsForTest(Of IdentifierNameSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub EventAccessFromRaiseCustomEventShouldReturnEventReference()
            Dim source = <![CDATA[
Imports System

Class TestClass   
    Public Custom Event TestEvent As Action
        AddHandler(value As Action)
        End AddHandler

        RemoveHandler(value As Action)
        End RemoveHandler

        RaiseEvent()
        End RaiseEvent
	End Event

    Sub M()
        RaiseEvent TestEvent()'BIND:"TestEvent"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IEventReferenceOperation: Event TestClass.TestEvent As System.Action (OperationKind.EventReference, Type: System.Action) (Syntax: 'TestEvent')
  Instance Receiver: 
    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: TestClass, IsImplicit) (Syntax: 'TestEvent')
]]>.Value
            Dim expectedDiagnostics = String.Empty
            VerifyOperationTreeAndDiagnosticsForTest(Of IdentifierNameSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub RaiseEvent_NoControlFlow()
            Dim source = <![CDATA[
Imports System

Class C1    
    Public Shared Event MyEvent1()
    Public Event MyEvent2(x As String)

    Sub M(x As String)'BIND:"Sub M(x As String)"
        RaiseEvent MyEvent1()
        RaiseEvent MyEvent2(x)
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
        IRaiseEventOperation (OperationKind.RaiseEvent, Type: null) (Syntax: 'RaiseEvent MyEvent1()')
          Event Reference: 
            IEventReferenceOperation: Event C1.MyEvent1() (Static) (OperationKind.EventReference, Type: C1.MyEvent1EventHandler) (Syntax: 'MyEvent1')
              Instance Receiver: 
                null
          Arguments(0)

        IRaiseEventOperation (OperationKind.RaiseEvent, Type: null) (Syntax: 'RaiseEvent MyEvent2(x)')
          Event Reference: 
            IEventReferenceOperation: Event C1.MyEvent2(x As System.String) (OperationKind.EventReference, Type: C1.MyEvent2EventHandler) (Syntax: 'MyEvent2')
              Instance Receiver: 
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C1, IsImplicit) (Syntax: 'MyEvent2')
          Arguments(1):
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: 'x')
                IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.String) (Syntax: 'x')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

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
        Public Sub RaiseEvent_ControlFlowNullReceiver()
            Dim source = <![CDATA[
Imports System

Class C1
    Public Shared Event MyEvent(x As String)

    Sub M(x As String) 'BIND:"Sub M(x As String)"
        RaiseEvent MyEvent(x)
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
        IRaiseEventOperation (OperationKind.RaiseEvent, Type: null) (Syntax: 'RaiseEvent MyEvent(x)')
          Event Reference: 
            IEventReferenceOperation: Event C1.MyEvent(x As System.String) (Static) (OperationKind.EventReference, Type: C1.MyEventEventHandler) (Syntax: 'MyEvent')
              Instance Receiver: 
                null
          Arguments(1):
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: 'x')
                IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.String) (Syntax: 'x')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        ' Note: Control flow is not possible in the EventReference for the RaiseEvent statement as
        ' the first token after RaiseEvent is parsed as an identifier.
        ' Hence, we only test control flow in the RaiseEvent arguments.

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub RaiseEvent_ControlFlowInFirstArgument()
            Dim source = <![CDATA[
Imports System

Class C1
    Public Event MyEvent(x As String, y As String)

    Sub M(x1 As String, x2 As String, y As String) 'BIND:"Sub M(x1 As String, x2 As String, y As String)"
        RaiseEvent MyEvent(If(x1, x2), y)
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
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x1')
                  Value: 
                    IParameterReferenceOperation: x1 (OperationKind.ParameterReference, Type: System.String) (Syntax: 'x1')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'x1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'x1')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x1')
                  Value: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'x1')

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x2')
              Value: 
                IParameterReferenceOperation: x2 (OperationKind.ParameterReference, Type: System.String) (Syntax: 'x2')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IRaiseEventOperation (OperationKind.RaiseEvent, Type: null) (Syntax: 'RaiseEvent  ... x1, x2), y)')
              Event Reference: 
                IEventReferenceOperation: Event C1.MyEvent(x As System.String, y As System.String) (OperationKind.EventReference, Type: C1.MyEventEventHandler) (Syntax: 'MyEvent')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'MyEvent')
              Arguments(2):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: 'If(x1, x2)')
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'If(x1, x2)')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: y) (OperationKind.Argument, Type: null) (Syntax: 'y')
                    IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.String) (Syntax: 'y')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

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
        Public Sub RaiseEvent_ControlFlowInSecondArgument()
            Dim source = <![CDATA[
Imports System

Class C1
    Public Event MyEvent(x As String, y As String)

    Sub M(x1 As String, x2 As String, y As String) 'BIND:"Sub M(x1 As String, x2 As String, y As String)"
        RaiseEvent MyEvent(y, If(x1, x2))
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
    CaptureIds: [0] [1] [3]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'MyEvent')
              Value: 
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C1, IsImplicit) (Syntax: 'MyEvent')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'y')
              Value: 
                IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.String) (Syntax: 'y')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x1')
                  Value: 
                    IParameterReferenceOperation: x1 (OperationKind.ParameterReference, Type: System.String) (Syntax: 'x1')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'x1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'x1')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x1')
                  Value: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'x1')

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x2')
              Value: 
                IParameterReferenceOperation: x2 (OperationKind.ParameterReference, Type: System.String) (Syntax: 'x2')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IRaiseEventOperation (OperationKind.RaiseEvent, Type: null) (Syntax: 'RaiseEvent  ... If(x1, x2))')
              Event Reference: 
                IEventReferenceOperation: Event C1.MyEvent(x As System.String, y As System.String) (OperationKind.EventReference, Type: C1.MyEventEventHandler) (Syntax: 'MyEvent')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'MyEvent')
              Arguments(2):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: 'y')
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'y')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: y) (OperationKind.Argument, Type: null) (Syntax: 'If(x1, x2)')
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'If(x1, x2)')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

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
        Public Sub RaiseEvent_ControlFlowInMultipleArguments()
            Dim source = <![CDATA[
Imports System

Class C1
    Public Event MyEvent(x As String, y As String)

    Sub M(x1 As String, x2 As String, y1 As String, y2 As String) 'BIND:"Sub M(x1 As String, x2 As String, y1 As String, y2 As String)"
        RaiseEvent MyEvent(If(x1, x2), If(y1, y2))
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
    CaptureIds: [0] [2] [4]
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
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x1')
                  Value: 
                    IParameterReferenceOperation: x1 (OperationKind.ParameterReference, Type: System.String) (Syntax: 'x1')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'x1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'x1')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x1')
                  Value: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'x1')

            Next (Regular) Block[B5]
                Leaving: {R2}
                Entering: {R3}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x2')
              Value: 
                IParameterReferenceOperation: x2 (OperationKind.ParameterReference, Type: System.String) (Syntax: 'x2')

        Next (Regular) Block[B5]
            Entering: {R3}

    .locals {R3}
    {
        CaptureIds: [3]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'y1')
                  Value: 
                    IParameterReferenceOperation: y1 (OperationKind.ParameterReference, Type: System.String) (Syntax: 'y1')

            Jump if True (Regular) to Block[B7]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'y1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'y1')
                Leaving: {R3}

            Next (Regular) Block[B6]
        Block[B6] - Block
            Predecessors: [B5]
            Statements (1)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'y1')
                  Value: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'y1')

            Next (Regular) Block[B8]
                Leaving: {R3}
    }

    Block[B7] - Block
        Predecessors: [B5]
        Statements (1)
            IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'y2')
              Value: 
                IParameterReferenceOperation: y2 (OperationKind.ParameterReference, Type: System.String) (Syntax: 'y2')

        Next (Regular) Block[B8]
    Block[B8] - Block
        Predecessors: [B6] [B7]
        Statements (1)
            IRaiseEventOperation (OperationKind.RaiseEvent, Type: null) (Syntax: 'RaiseEvent  ... If(y1, y2))')
              Event Reference: 
                IEventReferenceOperation: Event C1.MyEvent(x As System.String, y As System.String) (OperationKind.EventReference, Type: C1.MyEventEventHandler) (Syntax: 'MyEvent')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'MyEvent')
              Arguments(2):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: 'If(x1, x2)')
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'If(x1, x2)')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: y) (OperationKind.Argument, Type: null) (Syntax: 'If(y1, y2)')
                    IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'If(y1, y2)')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

        Next (Regular) Block[B9]
            Leaving: {R1}
}

Block[B9] - Exit
    Predecessors: [B8]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub
    End Class
End Namespace
