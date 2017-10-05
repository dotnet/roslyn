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
IRaiseEventStatement (OperationKind.RaiseEventStatement) (Syntax: 'RaiseEvent TestEvent()')
  Event Reference: 
    IEventReferenceExpression: Event TestClass.TestEvent As System.Action (OperationKind.EventReferenceExpression, Type: System.Action) (Syntax: 'TestEvent')
      Instance Receiver: 
        IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: TestClass, IsImplicit) (Syntax: 'TestEvent')
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
IRaiseEventStatement (OperationKind.RaiseEventStatement) (Syntax: 'RaiseEvent  ... ring.Empty)')
  Event Reference: 
    IEventReferenceExpression: Event TestClass.MyEvent(x As System.String, y As System.Int32) (OperationKind.EventReferenceExpression, Type: TestClass.MyEventEventHandler) (Syntax: 'MyEvent')
      Instance Receiver: 
        IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: TestClass, IsImplicit) (Syntax: 'MyEvent')
  Arguments(2):
      IArgument (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument) (Syntax: 'x:=String.Empty')
        IFieldReferenceExpression: System.String.Empty As System.String (Static) (OperationKind.FieldReferenceExpression, Type: System.String) (Syntax: 'String.Empty')
          Instance Receiver: 
            null
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IArgument (ArgumentKind.Explicit, Matching Parameter: y) (OperationKind.Argument) (Syntax: 'y:=1')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
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
IRaiseEventStatement (OperationKind.RaiseEventStatement) (Syntax: 'RaiseEvent TestEvent()')
  Event Reference: 
    IEventReferenceExpression: Event TestClass.TestEvent As System.Action (Static) (OperationKind.EventReferenceExpression, Type: System.Action) (Syntax: 'TestEvent')
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
IInvalidStatement (OperationKind.InvalidStatement, IsInvalid) (Syntax: 'RaiseEvent TestEvent(1)')
  Children(1):
      IInvalidExpression (OperationKind.InvalidExpression, Type: System.Void, IsInvalid, IsImplicit) (Syntax: 'RaiseEvent TestEvent(1)')
        Children(2):
            IOperation:  (OperationKind.None, IsInvalid) (Syntax: 'RaiseEvent TestEvent(1)')
              Children(1):
                  IFieldReferenceExpression: TestClass.TestEventEvent As System.Action (OperationKind.FieldReferenceExpression, Type: System.Action, IsImplicit) (Syntax: 'TestEvent')
                    Instance Receiver: 
                      IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: TestClass, IsImplicit) (Syntax: 'TestEvent')
            ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
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
IInvalidStatement (OperationKind.InvalidStatement, IsInvalid) (Syntax: 'RaiseEvent TestEvent2()')
  Children(1):
      IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: 'TestEvent2')
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
IInvocationExpression (virtual Sub System.Action.Invoke()) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'TestEventEvent()')
  Instance Receiver: 
    IFieldReferenceExpression: TestClass.TestEventEvent As System.Action (OperationKind.FieldReferenceExpression, Type: System.Action) (Syntax: 'TestEventEvent')
      Instance Receiver: 
        IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: TestClass, IsImplicit) (Syntax: 'TestEventEvent')
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
IInvocationExpression (virtual Sub System.Action.Invoke()) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'TestEventEvent.Invoke()')
  Instance Receiver: 
    IFieldReferenceExpression: TestClass.TestEventEvent As System.Action (OperationKind.FieldReferenceExpression, Type: System.Action) (Syntax: 'TestEventEvent')
      Instance Receiver: 
        IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: TestClass, IsImplicit) (Syntax: 'TestEventEvent')
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
IRaiseEventStatement (OperationKind.RaiseEventStatement) (Syntax: 'RaiseEvent TestEvent()')
  Event Reference: 
    IEventReferenceExpression: Event TestClass.TestEvent As System.Action (OperationKind.EventReferenceExpression, Type: System.Action) (Syntax: 'TestEvent')
      Instance Receiver: 
        IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: TestClass, IsImplicit) (Syntax: 'TestEvent')
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
IRaiseEventStatement (OperationKind.RaiseEventStatement) (Syntax: 'RaiseEvent  ... g, Nothing)')
  Event Reference: 
    IEventReferenceExpression: Event TestClass.TestEvent As System.EventHandler (OperationKind.EventReferenceExpression, Type: System.EventHandler) (Syntax: 'TestEvent')
      Instance Receiver: 
        IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: TestClass, IsImplicit) (Syntax: 'TestEvent')
  Arguments(2):
      IArgument (ArgumentKind.Explicit, Matching Parameter: sender) (OperationKind.Argument) (Syntax: 'Nothing')
        IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object, Constant: null) (Syntax: 'Nothing')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            ILiteralExpression (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'Nothing')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IArgument (ArgumentKind.Explicit, Matching Parameter: e) (OperationKind.Argument) (Syntax: 'Nothing')
        IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.EventArgs, Constant: null) (Syntax: 'Nothing')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            ILiteralExpression (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'Nothing')
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
IEventReferenceExpression: Event TestClass.TestEvent As System.Action (OperationKind.EventReferenceExpression, Type: System.Action) (Syntax: 'TestEvent')
  Instance Receiver: 
    IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: TestClass, IsImplicit) (Syntax: 'TestEvent')
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
IEventReferenceExpression: Event TestClass.TestEvent As System.Action (OperationKind.EventReferenceExpression, Type: System.Action) (Syntax: 'TestEvent')
  Instance Receiver: 
    IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: TestClass, IsImplicit) (Syntax: 'TestEvent')
]]>.Value
            Dim expectedDiagnostics = String.Empty
            VerifyOperationTreeAndDiagnosticsForTest(Of IdentifierNameSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub
    End Class
End Namespace
