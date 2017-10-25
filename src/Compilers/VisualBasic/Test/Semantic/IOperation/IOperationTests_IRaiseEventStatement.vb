' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        IInstanceReferenceOperation (OperationKind.InstanceReference, Type: TestClass, IsImplicit) (Syntax: 'TestEvent')
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
        IInstanceReferenceOperation (OperationKind.InstanceReference, Type: TestClass, IsImplicit) (Syntax: 'MyEvent')
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
                      IInstanceReferenceOperation (OperationKind.InstanceReference, Type: TestClass, IsImplicit) (Syntax: 'TestEvent')
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
        IInstanceReferenceOperation (OperationKind.InstanceReference, Type: TestClass, IsImplicit) (Syntax: 'TestEventEvent')
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
        IInstanceReferenceOperation (OperationKind.InstanceReference, Type: TestClass, IsImplicit) (Syntax: 'TestEventEvent')
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
        IInstanceReferenceOperation (OperationKind.InstanceReference, Type: TestClass, IsImplicit) (Syntax: 'TestEvent')
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
        IInstanceReferenceOperation (OperationKind.InstanceReference, Type: TestClass, IsImplicit) (Syntax: 'TestEvent')
  Arguments(2):
      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: sender) (OperationKind.Argument, Type: null) (Syntax: 'Nothing')
        IConversionOperation (Implicit, TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, Constant: null, IsImplicit) (Syntax: 'Nothing')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'Nothing')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: e) (OperationKind.Argument, Type: null) (Syntax: 'Nothing')
        IConversionOperation (Implicit, TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.EventArgs, Constant: null, IsImplicit) (Syntax: 'Nothing')
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
    IInstanceReferenceOperation (OperationKind.InstanceReference, Type: TestClass, IsImplicit) (Syntax: 'TestEvent')
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
    IInstanceReferenceOperation (OperationKind.InstanceReference, Type: TestClass, IsImplicit) (Syntax: 'TestEvent')
]]>.Value
            Dim expectedDiagnostics = String.Empty
            VerifyOperationTreeAndDiagnosticsForTest(Of IdentifierNameSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub
    End Class
End Namespace
