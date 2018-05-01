' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Test.Utilities

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
        Public Sub DynamicMemberReference_ControlFlowInSimpleMemberAccess()
            Dim source = <![CDATA[
Option Strict Off
Class C
    Private Sub M(c As Object, p As Object)'BIND:"Private Sub M(c As Object, p As Object)"
        p = c?.Prop1
    End Sub
End Class
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (2)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'p')
          Value: 
            IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'p')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
          Value: 
            IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'c')

    Jump if True (Regular) to Block[B3]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c')
          Operand: 
            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'c')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '.Prop1')
          Value: 
            IDynamicMemberReferenceOperation (Member Name: "Prop1", Containing Type: null) (OperationKind.DynamicMemberReference, Type: System.Object) (Syntax: '.Prop1')
              Type Arguments(0)
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'c')

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
          Value: 
            IDefaultValueOperation (OperationKind.DefaultValue, Type: System.Object, Constant: null, IsImplicit) (Syntax: 'c')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'p = c?.Prop1')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object, IsImplicit) (Syntax: 'p = c?.Prop1')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'p')
              Right: 
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'c?.Prop1')

    Next (Regular) Block[B5]
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub DynamicMemberReference_ControlFlowInNestedMemberAccess()
            Dim source = <![CDATA[
Option Strict Off
Class C
    Private Sub M(c As Object, p As Object)'BIND:"Private Sub M(c As Object, p As Object)"
        p = c?.Prop1?.Prop2
    End Sub
End Class
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (2)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'p')
          Value: 
            IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'p')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
          Value: 
            IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'c')

    Jump if True (Regular) to Block[B4]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c')
          Operand: 
            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'c')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '.Prop1')
          Value: 
            IDynamicMemberReferenceOperation (Member Name: "Prop1", Containing Type: null) (OperationKind.DynamicMemberReference, Type: System.Object) (Syntax: '.Prop1')
              Type Arguments(0)
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'c')

    Jump if True (Regular) to Block[B4]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: '.Prop1')
          Operand: 
            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: '.Prop1')

    Next (Regular) Block[B3]
Block[B3] - Block
    Predecessors: [B2]
    Statements (1)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '.Prop2')
          Value: 
            IDynamicMemberReferenceOperation (Member Name: "Prop2", Containing Type: null) (OperationKind.DynamicMemberReference, Type: System.Object) (Syntax: '.Prop2')
              Type Arguments(0)
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: '.Prop1')

    Next (Regular) Block[B5]
Block[B4] - Block
    Predecessors: [B1] [B2]
    Statements (1)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c?.Prop1?.Prop2')
          Value: 
            IDefaultValueOperation (OperationKind.DefaultValue, Type: System.Object, Constant: null, IsImplicit) (Syntax: 'c?.Prop1?.Prop2')

    Next (Regular) Block[B5]
Block[B5] - Block
    Predecessors: [B3] [B4]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'p = c?.Prop1?.Prop2')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object, IsImplicit) (Syntax: 'p = c?.Prop1?.Prop2')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'p')
              Right: 
                IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'c?.Prop1?.Prop2')

    Next (Regular) Block[B6]
Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub DynamicMemberReference_ControlFlowInGenericInvocation()
            Dim source = <![CDATA[
Option Strict Off
Class C
    Private Sub M(c As Object, p As Object)'BIND:"Private Sub M(c As Object, p As Object)"
        p = c?.GetValue(Of String)()
    End Sub
End Class
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (2)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'p')
          Value: 
            IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'p')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
          Value: 
            IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'c')

    Jump if True (Regular) to Block[B3]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c')
          Operand: 
            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'c')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '.GetValue(Of String)()')
          Value: 
            IDynamicInvocationOperation (OperationKind.DynamicInvocation, Type: System.Object) (Syntax: '.GetValue(Of String)()')
              Expression: 
                IDynamicMemberReferenceOperation (Member Name: "GetValue", Containing Type: null) (OperationKind.DynamicMemberReference, Type: System.Object) (Syntax: '.GetValue(Of String)')
                  Type Arguments(1):
                    Symbol: System.String
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'c')
              Arguments(0)
              ArgumentNames(0)
              ArgumentRefKinds: null

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
          Value: 
            IDefaultValueOperation (OperationKind.DefaultValue, Type: System.Object, Constant: null, IsImplicit) (Syntax: 'c')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'p = c?.GetV ... f String)()')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object, IsImplicit) (Syntax: 'p = c?.GetV ... f String)()')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'p')
              Right: 
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'c?.GetValue(Of String)()')

    Next (Regular) Block[B5]
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub
    End Class
End Namespace

