' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'AddHandler  ... AddressOf M')
  Expression: IEventAssignmentExpression (EventAdd) (OperationKind.EventAssignmentExpression, Type: null) (Syntax: 'AddHandler  ... AddressOf M')
      Event Reference: IEventReferenceExpression: Event TestClass.TestEvent As System.Action (OperationKind.EventReferenceExpression, Type: System.Action) (Syntax: 'TestEvent')
          Instance Receiver: IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: TestClass) (Syntax: 'TestEvent')
      Handler: IOperation:  (OperationKind.None) (Syntax: 'AddressOf M')
          Children(1):
              IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: TestClass) (Syntax: 'M')
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
IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'RemoveHandl ... AddressOf M')
  Expression: IEventAssignmentExpression (EventRemove) (OperationKind.EventAssignmentExpression, Type: null) (Syntax: 'RemoveHandl ... AddressOf M')
      Event Reference: IEventReferenceExpression: Event TestClass.TestEvent As System.Action (OperationKind.EventReferenceExpression, Type: System.Action) (Syntax: 'TestEvent')
          Instance Receiver: IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: TestClass) (Syntax: 'TestEvent')
      Handler: IOperation:  (OperationKind.None) (Syntax: 'AddressOf M')
          Children(1):
              IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: TestClass) (Syntax: 'M')
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
IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'AddHandler  ... AddressOf M')
  Expression: IEventAssignmentExpression (EventAdd) (OperationKind.EventAssignmentExpression, Type: null) (Syntax: 'AddHandler  ... AddressOf M')
      Event Reference: IEventReferenceExpression: Event TestClass.TestEvent As System.Action (Static) (OperationKind.EventReferenceExpression, Type: System.Action) (Syntax: 'TestEvent')
          Instance Receiver: null
      Handler: IOperation:  (OperationKind.None) (Syntax: 'AddressOf M')
          Children(1):
              IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: TestClass) (Syntax: 'M')
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
IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'RemoveHandl ... AddressOf M')
  Expression: IEventAssignmentExpression (EventRemove) (OperationKind.EventAssignmentExpression, Type: null) (Syntax: 'RemoveHandl ... AddressOf M')
      Event Reference: IEventReferenceExpression: Event TestClass.TestEvent As System.Action (Static) (OperationKind.EventReferenceExpression, Type: System.Action) (Syntax: 'TestEvent')
          Instance Receiver: null
      Handler: IOperation:  (OperationKind.None) (Syntax: 'AddressOf M')
          Children(1):
              IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: TestClass) (Syntax: 'M')
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
IExpressionStatement (OperationKind.ExpressionStatement, IsInvalid) (Syntax: 'RemoveHandl ... AddressOf M')
  Expression: IEventAssignmentExpression (EventRemove) (OperationKind.EventAssignmentExpression, Type: null, IsInvalid) (Syntax: 'RemoveHandl ... AddressOf M')
      Event Reference: IEventReferenceExpression: Event TestClass.TestEvent As System.Action (Static) (OperationKind.EventReferenceExpression, Type: System.Action) (Syntax: 'TestEvent')
          Instance Receiver: null
      Handler: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Action, IsInvalid) (Syntax: 'AddressOf M')
          Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: IOperation:  (OperationKind.None, IsInvalid) (Syntax: 'AddressOf M')
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
IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'AddHandler  ... AddressOf M')
  Expression: IEventAssignmentExpression (EventAdd) (OperationKind.EventAssignmentExpression, Type: null) (Syntax: 'AddHandler  ... AddressOf M')
      Event Reference: IEventReferenceExpression: Event TestClass.TestEvent As System.Action (Static) (OperationKind.EventReferenceExpression, Type: System.Action) (Syntax: 'Me.TestEvent')
          Instance Receiver: null
      Handler: IOperation:  (OperationKind.None) (Syntax: 'AddressOf M')
          Children(1):
              IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: TestClass) (Syntax: 'M')
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
IExpressionStatement (OperationKind.ExpressionStatement, IsInvalid) (Syntax: 'AddHandler  ... AddressOf M')
  Expression: IEventAssignmentExpression (EventAdd) (OperationKind.EventAssignmentExpression, Type: null, IsInvalid) (Syntax: 'AddHandler  ... AddressOf M')
      Event Reference: IEventReferenceExpression: Event TestClass.TestEvent As System.Action (OperationKind.EventReferenceExpression, Type: System.Action, IsInvalid) (Syntax: 'TestClass.TestEvent')
          Instance Receiver: IOperation:  (OperationKind.None, IsInvalid) (Syntax: 'TestClass')
      Handler: IOperation:  (OperationKind.None) (Syntax: 'AddressOf M')
          Children(1):
              IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: TestClass) (Syntax: 'M')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30469: Reference to a non-shared member requires an object reference.
        AddHandler TestClass.TestEvent, AddressOf M 'BIND:"AddHandler TestClass.TestEvent, AddressOf M"
                   ~~~~~~~~~~~~~~~~~~~
]]>.Value
            VerifyOperationTreeAndDiagnosticsForTest(Of AddRemoveHandlerStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

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
IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'RaiseEvent TestEvent()')
  Expression: IRaiseEventExpression (OperationKind.RaiseEventExpression, Type: null) (Syntax: 'RaiseEvent TestEvent()')
      Event Reference: IEventReferenceExpression: Event TestClass.TestEvent As System.Action (OperationKind.EventReferenceExpression, Type: System.Action) (Syntax: 'TestEvent')
          Instance Receiver: IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: TestClass) (Syntax: 'TestEvent')
      Arguments(0)
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
IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'RaiseEvent TestEvent()')
  Expression: IRaiseEventExpression (OperationKind.RaiseEventExpression, Type: null) (Syntax: 'RaiseEvent TestEvent()')
      Event Reference: IEventReferenceExpression: Event TestClass.TestEvent As System.Action (Static) (OperationKind.EventReferenceExpression, Type: System.Action) (Syntax: 'TestEvent')
          Instance Receiver: null
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
IExpressionStatement (OperationKind.ExpressionStatement, IsInvalid) (Syntax: 'RaiseEvent TestEvent(1)')
  Expression: IInvalidExpression (OperationKind.InvalidExpression, Type: System.Void, IsInvalid) (Syntax: 'RaiseEvent TestEvent(1)')
      Children(2):
          IOperation:  (OperationKind.None, IsInvalid) (Syntax: 'RaiseEvent TestEvent(1)')
          ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
]]>.Value
            
            Dim expectedDiagnostics = <![CDATA[
BC30057: Too many arguments to 'Public Event TestEvent As Action'.
        RaiseEvent TestEvent(1)'BIND:"RaiseEvent TestEvent(1)"
                             ~
]]>.Value
            Debugger.Launch()
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
  Instance Receiver: IFieldReferenceExpression: TestClass.TestEventEvent As System.Action (OperationKind.FieldReferenceExpression, Type: System.Action) (Syntax: 'TestEventEvent')
      Instance Receiver: IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: TestClass) (Syntax: 'TestEventEvent')
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
  Instance Receiver: IFieldReferenceExpression: TestClass.TestEventEvent As System.Action (OperationKind.FieldReferenceExpression, Type: System.Action) (Syntax: 'TestEventEvent')
      Instance Receiver: IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: TestClass) (Syntax: 'TestEventEvent')
  Arguments(0)
]]>.Value

            Dim expectedDiagnostics = String.Empty
            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub
    End Class
End Namespace
