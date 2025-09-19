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
        <Fact()>
        Public Sub IDynamicMemberReferenceExpression_SimplePropertyAccess()
            Dim source = <![CDATA[
Option Strict Off
Module Program
    Sub Main(args As String())
        Dim d = Nothing
        d.F'BIND:"d.F"
    End Sub
End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDynamicInvocationOperation (OperationKind.DynamicInvocation, Type: System.Object) (Syntax: 'd.F')
  Expression: 
    IDynamicMemberReferenceOperation (Member Name: "F", Containing Type: null) (OperationKind.DynamicMemberReference, Type: System.Object) (Syntax: 'd.F')
      Type Arguments(0)
      Instance Receiver: 
        ILocalReferenceOperation: d (OperationKind.LocalReference, Type: System.Object) (Syntax: 'd')
  Arguments(0)
  ArgumentNames(0)
  ArgumentRefKinds: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub IDynamicMemberReferenceExpression_GenericPropertyAccess()
            Dim source = <![CDATA[
Option Strict Off
Module Program
    Sub Main(args As String())
        Dim d = Nothing
        d.F(Of String)'BIND:"d.F(Of String)"
    End Sub
End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDynamicInvocationOperation (OperationKind.DynamicInvocation, Type: System.Object) (Syntax: 'd.F(Of String)')
  Expression: 
    IDynamicMemberReferenceOperation (Member Name: "F", Containing Type: null) (OperationKind.DynamicMemberReference, Type: System.Object) (Syntax: 'd.F(Of String)')
      Type Arguments(1):
        Symbol: System.String
      Instance Receiver: 
        ILocalReferenceOperation: d (OperationKind.LocalReference, Type: System.Object) (Syntax: 'd')
  Arguments(0)
  ArgumentNames(0)
  ArgumentRefKinds: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub IDynamicMemberReferenceExpression_InvalidGenericPropertyAccess()
            Dim source = <![CDATA[
Option Strict Off
Module Program
    Sub Main(args As String())
        Dim d = Nothing
        d.F(Of)'BIND:"d.F(Of)"
    End Sub
End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDynamicInvocationOperation (OperationKind.DynamicInvocation, Type: System.Object, IsInvalid) (Syntax: 'd.F(Of)')
  Expression: 
    IDynamicMemberReferenceOperation (Member Name: "F", Containing Type: null) (OperationKind.DynamicMemberReference, Type: System.Object, IsInvalid) (Syntax: 'd.F(Of)')
      Type Arguments(1):
        Symbol: ?
      Instance Receiver: 
        ILocalReferenceOperation: d (OperationKind.LocalReference, Type: System.Object) (Syntax: 'd')
  Arguments(0)
  ArgumentNames(0)
  ArgumentRefKinds: null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30182: Type expected.
        d.F(Of)'BIND:"d.F(Of)"
              ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub IDynamicMemberReferenceExpression_SimpleMethodCall()
            Dim source = <![CDATA[
Option Strict Off
Module Program
    Sub Main(args As String())
        Dim d = Nothing
        d.F()'BIND:"d.F()"
    End Sub
End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDynamicInvocationOperation (OperationKind.DynamicInvocation, Type: System.Object) (Syntax: 'd.F()')
  Expression: 
    IDynamicMemberReferenceOperation (Member Name: "F", Containing Type: null) (OperationKind.DynamicMemberReference, Type: System.Object) (Syntax: 'd.F')
      Type Arguments(0)
      Instance Receiver: 
        ILocalReferenceOperation: d (OperationKind.LocalReference, Type: System.Object) (Syntax: 'd')
  Arguments(0)
  ArgumentNames(0)
  ArgumentRefKinds: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub IDynamicMemberReferenceExpression_InvalidMethodCall_MissingParen()
            Dim source = <![CDATA[
Option Strict Off
Module Program
    Sub Main(args As String())
        Dim d = Nothing
        d.F('BIND:"d.F("
    End Sub
End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDynamicInvocationOperation (OperationKind.DynamicInvocation, Type: System.Object, IsInvalid) (Syntax: 'd.F(')
  Expression: 
    IDynamicMemberReferenceOperation (Member Name: "F", Containing Type: null) (OperationKind.DynamicMemberReference, Type: System.Object) (Syntax: 'd.F')
      Type Arguments(0)
      Instance Receiver: 
        ILocalReferenceOperation: d (OperationKind.LocalReference, Type: System.Object) (Syntax: 'd')
  Arguments(1):
      IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
        Children(0)
  ArgumentNames(0)
  ArgumentRefKinds: null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30198: ')' expected.
        d.F('BIND:"d.F("
            ~
BC30201: Expression expected.
        d.F('BIND:"d.F("
            ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub IDynamicMemberReferenceExpression_GenericMethodCall_SingleGeneric()
            Dim source = <![CDATA[
Option Strict Off
Module Program
    Sub Main(args As String())
        Dim d = Nothing
        d.GetValue(Of String)()'BIND:"d.GetValue(Of String)()"
    End Sub
End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDynamicInvocationOperation (OperationKind.DynamicInvocation, Type: System.Object) (Syntax: 'd.GetValue(Of String)()')
  Expression: 
    IDynamicMemberReferenceOperation (Member Name: "GetValue", Containing Type: null) (OperationKind.DynamicMemberReference, Type: System.Object) (Syntax: 'd.GetValue(Of String)')
      Type Arguments(1):
        Symbol: System.String
      Instance Receiver: 
        ILocalReferenceOperation: d (OperationKind.LocalReference, Type: System.Object) (Syntax: 'd')
  Arguments(0)
  ArgumentNames(0)
  ArgumentRefKinds: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub IDynamicMemberReferenceExpression_GenericMethodCall_MultipleGeneric()
            Dim source = <![CDATA[
Option Strict Off
Module Program
    Sub Main(args As String())
        Dim d = Nothing
        d.GetValue(Of String, Integer)()'BIND:"d.GetValue(Of String, Integer)()"
    End Sub
End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDynamicInvocationOperation (OperationKind.DynamicInvocation, Type: System.Object) (Syntax: 'd.GetValue( ...  Integer)()')
  Expression: 
    IDynamicMemberReferenceOperation (Member Name: "GetValue", Containing Type: null) (OperationKind.DynamicMemberReference, Type: System.Object) (Syntax: 'd.GetValue( ... g, Integer)')
      Type Arguments(2):
        Symbol: System.String
        Symbol: System.Int32
      Instance Receiver: 
        ILocalReferenceOperation: d (OperationKind.LocalReference, Type: System.Object) (Syntax: 'd')
  Arguments(0)
  ArgumentNames(0)
  ArgumentRefKinds: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub IDynamicMemberReferenceExpression_GenericMethodCall_InvalidGenericParameter()
            Dim source = <![CDATA[
Option Strict Off
Module Program
    Sub Main(args As String())
        Dim d = Nothing
        d.GetValue(Of String,)()'BIND:"d.GetValue(Of String,)()"
    End Sub
End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDynamicInvocationOperation (OperationKind.DynamicInvocation, Type: System.Object, IsInvalid) (Syntax: 'd.GetValue(Of String,)()')
  Expression: 
    IDynamicMemberReferenceOperation (Member Name: "GetValue", Containing Type: null) (OperationKind.DynamicMemberReference, Type: System.Object, IsInvalid) (Syntax: 'd.GetValue(Of String,)')
      Type Arguments(2):
        Symbol: System.String
        Symbol: ?
      Instance Receiver: 
        ILocalReferenceOperation: d (OperationKind.LocalReference, Type: System.Object) (Syntax: 'd')
  Arguments(0)
  ArgumentNames(0)
  ArgumentRefKinds: null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30182: Type expected.
        d.GetValue(Of String,)()'BIND:"d.GetValue(Of String,)()"
                             ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub IDynamicMemberReferenceExpression_NestedPropertyAccess()
            Dim source = <![CDATA[
Option Strict Off
Module Program
    Sub Main(args As String())
        Dim d = Nothing
        d.Prop1.Prop2'BIND:"d.Prop1.Prop2"
    End Sub
End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDynamicInvocationOperation (OperationKind.DynamicInvocation, Type: System.Object) (Syntax: 'd.Prop1.Prop2')
  Expression: 
    IDynamicMemberReferenceOperation (Member Name: "Prop2", Containing Type: null) (OperationKind.DynamicMemberReference, Type: System.Object) (Syntax: 'd.Prop1.Prop2')
      Type Arguments(0)
      Instance Receiver: 
        IDynamicMemberReferenceOperation (Member Name: "Prop1", Containing Type: null) (OperationKind.DynamicMemberReference, Type: System.Object) (Syntax: 'd.Prop1')
          Type Arguments(0)
          Instance Receiver: 
            ILocalReferenceOperation: d (OperationKind.LocalReference, Type: System.Object) (Syntax: 'd')
  Arguments(0)
  ArgumentNames(0)
  ArgumentRefKinds: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub IDynamicMemberReferenceExpression_NestedMethodAccess()
            Dim source = <![CDATA[
Option Strict Off
Module Program
    Sub Main(args As String())
        Dim d = Nothing
        d.Method1().Method2()'BIND:"d.Method1().Method2()"
    End Sub
End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDynamicInvocationOperation (OperationKind.DynamicInvocation, Type: System.Object) (Syntax: 'd.Method1().Method2()')
  Expression: 
    IDynamicMemberReferenceOperation (Member Name: "Method2", Containing Type: null) (OperationKind.DynamicMemberReference, Type: System.Object) (Syntax: 'd.Method1().Method2')
      Type Arguments(0)
      Instance Receiver: 
        IDynamicInvocationOperation (OperationKind.DynamicInvocation, Type: System.Object) (Syntax: 'd.Method1()')
          Expression: 
            IDynamicMemberReferenceOperation (Member Name: "Method1", Containing Type: null) (OperationKind.DynamicMemberReference, Type: System.Object) (Syntax: 'd.Method1')
              Type Arguments(0)
              Instance Receiver: 
                ILocalReferenceOperation: d (OperationKind.LocalReference, Type: System.Object) (Syntax: 'd')
          Arguments(0)
          ArgumentNames(0)
          ArgumentRefKinds: null
  Arguments(0)
  ArgumentNames(0)
  ArgumentRefKinds: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub IDynamicMemberReferenceExpression_NestedPropertyAndMethodAccess()
            Dim source = <![CDATA[
Option Strict Off
Module Program
    Sub Main(args As String())
        Dim d = Nothing
        d.Prop1.Method2()'BIND:"d.Prop1.Method2()"
    End Sub
End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDynamicInvocationOperation (OperationKind.DynamicInvocation, Type: System.Object) (Syntax: 'd.Prop1.Method2()')
  Expression: 
    IDynamicMemberReferenceOperation (Member Name: "Method2", Containing Type: null) (OperationKind.DynamicMemberReference, Type: System.Object) (Syntax: 'd.Prop1.Method2')
      Type Arguments(0)
      Instance Receiver: 
        IDynamicMemberReferenceOperation (Member Name: "Prop1", Containing Type: null) (OperationKind.DynamicMemberReference, Type: System.Object) (Syntax: 'd.Prop1')
          Type Arguments(0)
          Instance Receiver: 
            ILocalReferenceOperation: d (OperationKind.LocalReference, Type: System.Object) (Syntax: 'd')
  Arguments(0)
  ArgumentNames(0)
  ArgumentRefKinds: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub IDynamicMemberReferenceExpression_LateBoundModuleFunction()
            Dim source = <![CDATA[
Option Strict Off
Imports System.Collections.Generic

Module Module1
    Sub Main()
        Dim x As Object = New List(Of Integer)()
        fun(x)'BIND:"fun(x)"
    End Sub

    Sub fun(Of X)(ByVal a As List(Of X))
    End Sub

End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDynamicInvocationOperation (OperationKind.DynamicInvocation, Type: System.Object) (Syntax: 'fun(x)')
  Expression: 
    IDynamicMemberReferenceOperation (Member Name: "fun", Containing Type: Module1) (OperationKind.DynamicMemberReference, Type: System.Object) (Syntax: 'fun')
      Type Arguments(0)
      Instance Receiver: 
        null
  Arguments(1):
      ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Object) (Syntax: 'x')
  ArgumentNames(0)
  ArgumentRefKinds: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub IDynamicMemberReferenceExpression_LateBoundClassFunction()
            Dim source = <![CDATA[
Option Strict Off
Imports System.Collections.Generic

Module Module1
    Sub Main()
        Dim x As Object = New List(Of Integer)()
        Dim c1 As New C1
        c1.fun(x)'BIND:"c1.fun(x)"
    End Sub

    Class C1
        Sub fun(Of X)(ByVal a As List(Of X))
        End Sub
    End Class
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDynamicInvocationOperation (OperationKind.DynamicInvocation, Type: System.Object) (Syntax: 'c1.fun(x)')
  Expression: 
    IDynamicMemberReferenceOperation (Member Name: "fun", Containing Type: null) (OperationKind.DynamicMemberReference, Type: System.Object) (Syntax: 'c1.fun')
      Type Arguments(0)
      Instance Receiver: 
        ILocalReferenceOperation: c1 (OperationKind.LocalReference, Type: Module1.C1) (Syntax: 'c1')
  Arguments(1):
      ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Object) (Syntax: 'x')
  ArgumentNames(0)
  ArgumentRefKinds: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub DynamicMemberReference_NoControlFlow()
            Dim source = <![CDATA[
Option Strict Off
Class C
    Private Sub M(d As Object, p As Object)'BIND:"Private Sub M(d As Object, p As Object)"
        p = d.Prop1
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
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'p = d.Prop1')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object, IsImplicit) (Syntax: 'p = d.Prop1')
              Left: 
                IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'p')
              Right: 
                IDynamicMemberReferenceOperation (Member Name: "Prop1", Containing Type: null) (OperationKind.DynamicMemberReference, Type: System.Object) (Syntax: 'd.Prop1')
                  Type Arguments(0)
                  Instance Receiver: 
                    IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'd')

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
        Public Sub DynamicMemberReference_ControlFlowInReceiver()
            Dim source = <![CDATA[
Option Strict Off
Class C
    Private Sub M(d1 As Object, d2 As Object, p As Object)'BIND:"Private Sub M(d1 As Object, d2 As Object, p As Object)"
        p = If(d1, d2).Prop1
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'p')
              Value: 
                IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'p')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd1')
                  Value: 
                    IParameterReferenceOperation: d1 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'd1')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'd1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'd1')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd1')
                  Value: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'd1')

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd2')
              Value: 
                IParameterReferenceOperation: d2 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'd2')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'p = If(d1, d2).Prop1')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object, IsImplicit) (Syntax: 'p = If(d1, d2).Prop1')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'p')
                  Right: 
                    IDynamicMemberReferenceOperation (Member Name: "Prop1", Containing Type: null) (OperationKind.DynamicMemberReference, Type: System.Object) (Syntax: 'If(d1, d2).Prop1')
                      Type Arguments(0)
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'If(d1, d2)')

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
        Public Sub DynamicMemberReference_ControlFlowInReceiver_TypeArguments()
            Dim source = <![CDATA[
Option Strict Off
Class C
    Private Sub M(d1 As Object, d2 As Object, p As Object)'BIND:"Private Sub M(d1 As Object, d2 As Object, p As Object)"
        p = If(d1, d2).Prop1(Of Integer)
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'p')
              Value: 
                IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'p')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd1')
                  Value: 
                    IParameterReferenceOperation: d1 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'd1')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'd1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'd1')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd1')
                  Value: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'd1')

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd2')
              Value: 
                IParameterReferenceOperation: d2 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'd2')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'p = If(d1,  ... Of Integer)')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object, IsImplicit) (Syntax: 'p = If(d1,  ... Of Integer)')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'p')
                  Right: 
                    IDynamicMemberReferenceOperation (Member Name: "Prop1", Containing Type: null) (OperationKind.DynamicMemberReference, Type: System.Object) (Syntax: 'If(d1, d2). ... Of Integer)')
                      Type Arguments(1):
                        Symbol: System.Int32
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'If(d1, d2)')

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

        <CompilerTrait(CompilerFeature.IOperation)>
        <WorkItem(27034, "https://github.com/dotnet/roslyn/issues/27034")>
        <Fact()>
        Public Sub DynamicMemberReference_OffObjectCollectionInitializer()
            Dim source = <![CDATA[
Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.Runtime.CompilerServices

Module Mod1
    Class C
        Implements IEnumerable(Of Integer)

        Sub M(a As Object, b As Object)
            Dim i = New C From {a}.Add(b)'BIND:"New C From {a}.Add"
        End Sub

        Public Function GetEnumerator() As IEnumerator(Of Integer) Implements IEnumerable(Of Integer).GetEnumerator
            Throw New NotImplementedException()
        End Function

        Private Function IEnumerable_GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
            Throw New NotImplementedException()
        End Function

        Public Sub Add(i As Integer)
        End Sub

        Public Sub Add(l As Long)
        End Sub
    End Class
End Module]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedOperationTree = <![CDATA[
IDynamicMemberReferenceOperation (Member Name: "Add", Containing Type: null) (OperationKind.DynamicMemberReference, Type: System.Object) (Syntax: 'New C From {a}.Add')
  Type Arguments(0)
  Instance Receiver: 
    IObjectCreationOperation (Constructor: Sub Mod1.C..ctor()) (OperationKind.ObjectCreation, Type: Mod1.C) (Syntax: 'New C From {a}')
      Arguments(0)
      Initializer: 
        IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: Mod1.C) (Syntax: 'From {a}')
          Initializers(1):
              IDynamicInvocationOperation (OperationKind.DynamicInvocation, Type: System.Object, IsImplicit) (Syntax: 'a')
                Expression: 
                  IDynamicMemberReferenceOperation (Member Name: "Add", Containing Type: null) (OperationKind.DynamicMemberReference, Type: System.Object, IsImplicit) (Syntax: 'a')
                    Type Arguments(0)
                    Instance Receiver: 
                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: Mod1.C, IsImplicit) (Syntax: 'New C From {a}')
                Arguments(1):
                    IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'a')
                ArgumentNames(0)
                ArgumentRefKinds: null
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of MemberAccessExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub
    End Class
End Namespace

