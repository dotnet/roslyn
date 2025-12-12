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
IAwaitOperation (OperationKind.Await, Type: ?, IsInvalid) (Syntax: 'Await UndefinedTask')
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
IAwaitOperation (OperationKind.Await, Type: ?, IsInvalid) (Syntax: 'Await i')
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
IAwaitOperation (OperationKind.Await, Type: ?, IsInvalid) (Syntax: 'Await')
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

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67616")>
        Public Sub TestAwaitExpression_InStatement()
            Dim source = <![CDATA[
Imports System.Threading.Tasks

Public Module Program
    Public Async Function M() As Task(Of Integer)
        Await M2()'BIND:"Await M2()"
        Return 0
    End Function

    Public Function M2() As Task(Of String)
        Throw New System.Exception()
    End Function
End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IAwaitOperation (OperationKind.Await, Type: System.String) (Syntax: 'Await M2()')
  Expression:
    IInvocationOperation (Function Program.M2() As System.Threading.Tasks.Task(Of System.String)) (OperationKind.Invocation, Type: System.Threading.Tasks.Task(Of System.String)) (Syntax: 'M2()')
      Instance Receiver:
        null
      Arguments(0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of AwaitExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics, useLatestFramework:=True)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67616")>
        Public Sub TestAwaitExpression_InStatement_InSubLambda()
            Dim source = <compilation>
                             <file name="c.vb"><![CDATA[
Imports System
Imports System.Threading.Tasks

Public Module Program
    Public Sub Main()
        Dim lambda As Action = Async Sub()
                                 Await M2()'BIND:"Await M2()"
                               End Sub

        lambda()
    End Sub

    Public Function M2() As Task(Of String)
        System.Console.WriteLine("M2")
        Return Task.FromResult("")
    End Function
End Module
]]></file>
                         </compilation>

            Dim expectedOperationTree = <![CDATA[
IAwaitOperation (OperationKind.Await, Type: System.String) (Syntax: 'Await M2()')
  Expression:
    IInvocationOperation (Function Program.M2() As System.Threading.Tasks.Task(Of System.String)) (OperationKind.Invocation, Type: System.Threading.Tasks.Task(Of System.String)) (Syntax: 'M2()')
      Instance Receiver:
        null
      Arguments(0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of AwaitExpressionSyntax)(source.Value, expectedOperationTree, expectedDiagnostics, useLatestFramework:=True)
            CompileAndVerify(source, expectedOutput:="M2", useLatestFramework:=True)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67616")>
        Public Sub TestAwaitExpression_InStatement_InSubWithExpressionLambda()
            Dim source = <compilation>
                             <file name="c.vb"><![CDATA[
Imports System
Imports System.Threading.Tasks

Public Module Program
    Public Sub Main()
        Dim lambda As Action = Async Sub() Await M2()'BIND:"Await M2()"

        lambda()
    End Sub

    Public Function M2() As Task(Of String)
        System.Console.WriteLine("M2")
        Return Task.FromResult("")
    End Function
End Module
]]></file>
                         </compilation>

            Dim expectedOperationTree = <![CDATA[
IAwaitOperation (OperationKind.Await, Type: System.String) (Syntax: 'Await M2()')
  Expression:
    IInvocationOperation (Function Program.M2() As System.Threading.Tasks.Task(Of System.String)) (OperationKind.Invocation, Type: System.Threading.Tasks.Task(Of System.String)) (Syntax: 'M2()')
      Instance Receiver:
        null
      Arguments(0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of AwaitExpressionSyntax)(source.Value, expectedOperationTree, expectedDiagnostics, useLatestFramework:=True)
            CompileAndVerify(source, expectedOutput:="M2", useLatestFramework:=True)
        End Sub
    End Class
End Namespace
