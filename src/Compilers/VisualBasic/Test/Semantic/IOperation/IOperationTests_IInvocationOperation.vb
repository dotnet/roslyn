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
    End Class
End Namespace
