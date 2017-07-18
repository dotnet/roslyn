' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase

        <Fact>
        Public Sub AddEventHandler()
            Dim source = <![CDATA[
Imports System

Class TestClass
    
    Event TestEvent As Action

    Sub Add(receiver As TestClass)
        AddHandler TestEvent, AddressOf M'BIND:"AddHandler TestEvent, AddressOf M"
    End Sub

    Sub M()
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'AddHandler  ... AddressOf M')
  IEventAssignmentExpression (EventAdd)) (OperationKind.EventAssignmentExpression, Type: null) (Syntax: 'AddHandler  ... AddressOf M')
    Event Reference: IEventReferenceExpression: Event TestClass.TestEvent As System.Action (OperationKind.EventReferenceExpression, Type: System.Action) (Syntax: 'TestEvent')
        Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: TestClass) (Syntax: 'TestEvent')
    Handler: IOperation:  (OperationKind.None) (Syntax: 'AddressOf M')
        Children(1):
            IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: TestClass) (Syntax: 'M')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of AddRemoveHandlerStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact>
        Public Sub RemoveEventHandler()
            Dim source = <![CDATA[
Imports System

Class TestClass
    
    Event TestEvent As Action

    Sub Remove(receiver As TestClass)
        RemoveHandler TestEvent, AddressOf M'BIND:"RemoveHandler TestEvent, AddressOf M"
    End Sub

    Sub M()
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'RemoveHandl ... AddressOf M')
  IEventAssignmentExpression (EventRemove)) (OperationKind.EventAssignmentExpression, Type: null) (Syntax: 'RemoveHandl ... AddressOf M')
    Event Reference: IEventReferenceExpression: Event TestClass.TestEvent As System.Action (OperationKind.EventReferenceExpression, Type: System.Action) (Syntax: 'TestEvent')
        Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: TestClass) (Syntax: 'TestEvent')
    Handler: IOperation:  (OperationKind.None) (Syntax: 'AddressOf M')
        Children(1):
            IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: TestClass) (Syntax: 'M')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of AddRemoveHandlerStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact>
        Public Sub AddEventHandler_StaticEvent()
            Dim source = <![CDATA[
Imports System

Class TestClass
    
    Shared Event TestEvent As Action

    Sub Add(receiver As TestClass)
        AddHandler TestEvent, AddressOf M'BIND:"AddHandler TestEvent, AddressOf M"
    End Sub

    Sub M()
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'AddHandler  ... AddressOf M')
  IEventAssignmentExpression (EventAdd)) (OperationKind.EventAssignmentExpression, Type: null) (Syntax: 'AddHandler  ... AddressOf M')
    Event Reference: IEventReferenceExpression: Event TestClass.TestEvent As System.Action (Static) (OperationKind.EventReferenceExpression, Type: System.Action) (Syntax: 'TestEvent')
        Instance Receiver: null
    Handler: IOperation:  (OperationKind.None) (Syntax: 'AddressOf M')
        Children(1):
            IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: TestClass) (Syntax: 'M')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of AddRemoveHandlerStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact>
        Public Sub RemoveEventHandler_StaticEvent()
            Dim source = <![CDATA[
Imports System

Class TestClass
    
    Shared Event TestEvent As Action

    Sub Remove(receiver As TestClass)
        RemoveHandler TestEvent, AddressOf M'BIND:"RemoveHandler TestEvent, AddressOf M"
    End Sub

    Sub M()
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'RemoveHandl ... AddressOf M')
  IEventAssignmentExpression (EventRemove)) (OperationKind.EventAssignmentExpression, Type: null) (Syntax: 'RemoveHandl ... AddressOf M')
    Event Reference: IEventReferenceExpression: Event TestClass.TestEvent As System.Action (Static) (OperationKind.EventReferenceExpression, Type: System.Action) (Syntax: 'TestEvent')
        Instance Receiver: null
    Handler: IOperation:  (OperationKind.None) (Syntax: 'AddressOf M')
        Children(1):
            IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: TestClass) (Syntax: 'M')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of AddRemoveHandlerStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact>
        Public Sub RemoveEventHandler_DelegateTypeMismatch()
            Dim source = <![CDATA[
Imports System

Class TestClass
    
    Shared Event TestEvent As Action

    Sub Remove(receiver As TestClass)
        RemoveHandler TestEvent, AddressOf M'BIND:"RemoveHandler TestEvent, AddressOf M"
    End Sub

    Sub M(x as Integer)
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IExpressionStatement (OperationKind.ExpressionStatement, IsInvalid) (Syntax: 'RemoveHandl ... AddressOf M')
  IEventAssignmentExpression (EventRemove)) (OperationKind.EventAssignmentExpression, Type: null, IsInvalid) (Syntax: 'RemoveHandl ... AddressOf M')
    Event Reference: IEventReferenceExpression: Event TestClass.TestEvent As System.Action (Static) (OperationKind.EventReferenceExpression, Type: System.Action) (Syntax: 'TestEvent')
        Instance Receiver: null
    Handler: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: System.Action, IsInvalid) (Syntax: 'AddressOf M')
        IOperation:  (OperationKind.None, IsInvalid) (Syntax: 'AddressOf M')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC31143: Method 'Public Sub M(x As Integer)' does not have a signature compatible with delegate 'Delegate Sub Action()'.
        RemoveHandler TestEvent, AddressOf M'BIND:"RemoveHandler TestEvent, AddressOf M"
                                           ~
]]>.Value
            VerifyOperationTreeAndDiagnosticsForTest(Of AddRemoveHandlerStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

    End Class
End Namespace
