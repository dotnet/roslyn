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
IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'AddHandler  ... AddressOf M')
  Expression: 
    IEventAssignmentOperation (EventAdd) (OperationKind.EventAssignment, Type: null, IsImplicit) (Syntax: 'AddHandler  ... AddressOf M')
      Event Reference: 
        IEventReferenceOperation: Event TestClass.TestEvent As System.Action (OperationKind.EventReference, Type: System.Action) (Syntax: 'TestEvent')
          Instance Receiver: 
            IInstanceReferenceOperation (OperationKind.InstanceReference, Type: TestClass, IsImplicit) (Syntax: 'TestEvent')
      Handler: 
        IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action, IsImplicit) (Syntax: 'AddressOf M')
          Target: 
            IMethodReferenceOperation: Sub TestClass.M() (OperationKind.MethodReference, Type: null) (Syntax: 'AddressOf M')
              Instance Receiver: 
                IInstanceReferenceOperation (OperationKind.InstanceReference, Type: TestClass, IsImplicit) (Syntax: 'M')
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
            IInstanceReferenceOperation (OperationKind.InstanceReference, Type: TestClass, IsImplicit) (Syntax: 'TestEvent')
      Handler: 
        IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action, IsImplicit) (Syntax: 'AddressOf M')
          Target: 
            IMethodReferenceOperation: Sub TestClass.M() (OperationKind.MethodReference, Type: null) (Syntax: 'AddressOf M')
              Instance Receiver: 
                IInstanceReferenceOperation (OperationKind.InstanceReference, Type: TestClass, IsImplicit) (Syntax: 'M')
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
                IInstanceReferenceOperation (OperationKind.InstanceReference, Type: TestClass, IsImplicit) (Syntax: 'M')
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
                IInstanceReferenceOperation (OperationKind.InstanceReference, Type: TestClass, IsImplicit) (Syntax: 'M')
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
                        IInstanceReferenceOperation (OperationKind.InstanceReference, Type: TestClass, IsInvalid, IsImplicit) (Syntax: 'M')
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
            null
      Handler: 
        IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action, IsImplicit) (Syntax: 'AddressOf M')
          Target: 
            IMethodReferenceOperation: Sub TestClass.M() (OperationKind.MethodReference, Type: null) (Syntax: 'AddressOf M')
              Instance Receiver: 
                IInstanceReferenceOperation (OperationKind.InstanceReference, Type: TestClass, IsImplicit) (Syntax: 'M')
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
            IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'TestClass')
      Handler: 
        IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action, IsImplicit) (Syntax: 'AddressOf M')
          Target: 
            IMethodReferenceOperation: Sub TestClass.M() (OperationKind.MethodReference, Type: null) (Syntax: 'AddressOf M')
              Instance Receiver: 
                IInstanceReferenceOperation (OperationKind.InstanceReference, Type: TestClass, IsImplicit) (Syntax: 'M')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30469: Reference to a non-shared member requires an object reference.
        AddHandler TestClass.TestEvent, AddressOf M 'BIND:"AddHandler TestClass.TestEvent, AddressOf M"
                   ~~~~~~~~~~~~~~~~~~~
]]>.Value
            VerifyOperationTreeAndDiagnosticsForTest(Of AddRemoveHandlerStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub
    End Class
End Namespace
