' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TestAwaitExpression()
            Dim source = <![CDATA[
Imports System
Imports System.Threading.Tasks

Class C
    Async Sub M()
        Await M2()'BIND:"Await M2()"
    End Sub

    Function M2() As Task
        Return Nothing
    End Function
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IAwaitOperation (OperationKind.Await, Type: System.Void) (Syntax: 'Await M2()')
  Expression: 
    IInvocationOperation ( Function C.M2() As System.Threading.Tasks.Task) (OperationKind.Invocation, Type: System.Threading.Tasks.Task) (Syntax: 'M2()')
      Instance Receiver: 
        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'M2')
      Arguments(0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of AwaitExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics, useLatestFramework:=True)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TestAwaitExpression_ParameterReference()
            Dim source = <![CDATA[
Imports System
Imports System.Threading.Tasks

Class C
    Async Sub M(t As Task)
        Await t'BIND:"Await t"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IAwaitOperation (OperationKind.Await, Type: System.Void) (Syntax: 'Await t')
  Expression: 
    IParameterReferenceOperation: t (OperationKind.ParameterReference, Type: System.Threading.Tasks.Task) (Syntax: 't')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of AwaitExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics, useLatestFramework:=True)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TestAwaitExpression_InLambda()
            Dim source = <![CDATA[
Imports System
Imports System.Threading.Tasks

Class C
    Async Sub M(t As Task(Of Integer))
        Dim f As Func(Of Task) = Async Function() Await t'BIND:"Await t"
        Await f()
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IAwaitOperation (OperationKind.Await, Type: System.Int32) (Syntax: 'Await t')
  Expression: 
    IParameterReferenceOperation: t (OperationKind.ParameterReference, Type: System.Threading.Tasks.Task(Of System.Int32)) (Syntax: 't')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of AwaitExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics, useLatestFramework:=True)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TestAwaitExpression_ErrorArgument()
            Dim source = <![CDATA[
Imports System
Imports System.Threading.Tasks

Class C
    Async Sub M()
        Await UndefinedTask'BIND:"Await UndefinedTask"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IAwaitOperation (OperationKind.Await, Type: System.Void, IsInvalid) (Syntax: 'Await UndefinedTask')
  Expression: 
    IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'UndefinedTask')
      Children(0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30451: 'UndefinedTask' is not declared. It may be inaccessible due to its protection level.
        Await UndefinedTask'BIND:"Await UndefinedTask"
              ~~~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of AwaitExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics, useLatestFramework:=True)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TestAwaitExpression_ValueArgument()
            Dim source = <![CDATA[
Imports System
Imports System.Threading.Tasks

Class C
    Async Sub M(i As Integer)
        Await i'BIND:"Await i"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IAwaitOperation (OperationKind.Await, Type: System.Void, IsInvalid) (Syntax: 'Await i')
  Expression: 
    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'i')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36930: 'Await' requires that the type 'Integer' have a suitable GetAwaiter method.
        Await i'BIND:"Await i"
        ~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of AwaitExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics, useLatestFramework:=True)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TestAwaitExpression_MissingArgument()
            Dim source = <![CDATA[
Imports System
Imports System.Threading.Tasks

Class C
    Async Sub M()
        Await'BIND:"Await"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IAwaitOperation (OperationKind.Await, Type: System.Void, IsInvalid) (Syntax: 'Await')
  Expression: 
    IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
      Children(0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30201: Expression expected.
        Await'BIND:"Await"
             ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of AwaitExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics, useLatestFramework:=True)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TestAwaitExpression_NonAsyncMethod()
            Dim source = <![CDATA[
Imports System
Imports System.Threading.Tasks

Class C
    Sub M(t As Task)
        Await t'BIND:"Await t"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'Await t')
  Expression: 
    IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'Await t')
      Children(2):
          IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'Await')
            Children(0)
          IParameterReferenceOperation: t (OperationKind.ParameterReference, Type: System.Threading.Tasks.Task, IsInvalid) (Syntax: 't')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC37058: 'Await' can only be used within an Async method. Consider marking this method with the 'Async' modifier and changing its return type to 'Task'.
        Await t'BIND:"Await t"
        ~~~~~
BC30800: Method arguments must be enclosed in parentheses.
        Await t'BIND:"Await t"
              ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ExpressionStatementSyntax)(source, expectedOperationTree, expectedDiagnostics, useLatestFramework:=True)
        End Sub
    End Class
End Namespace
