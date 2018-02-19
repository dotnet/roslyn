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
    End Class
End Namespace
