' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase
        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DynamicInvocationExpression_Basic()
            Dim source = <![CDATA[
Option Strict Off

Class C
    Private Sub M(c As Object, d As Object)
        c.M2(d)'BIND:"c.M2(d)"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDynamicInvocationOperation (OperationKind.DynamicInvocation, Type: System.Object) (Syntax: 'c.M2(d)')
  Expression: 
    IDynamicMemberReferenceOperation (Member Name: "M2", Containing Type: null) (OperationKind.DynamicMemberReference, Type: System.Object) (Syntax: 'c.M2')
      Type Arguments(0)
      Instance Receiver: 
        IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'c')
  Arguments(1):
      IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'd')
  ArgumentNames(0)
  ArgumentRefKinds: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DynamicInvocationExpression_MultipleApplicableSymbols()
            Dim source = <![CDATA[
Option Strict Off

Class C
    Private Sub M(d As Object)
        M2(d)'BIND:"M2(d)"
    End Sub

    Private Sub M2(c As Integer)
    End Sub

    Private Sub M2(c As Long)
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDynamicInvocationOperation (OperationKind.DynamicInvocation, Type: System.Object) (Syntax: 'M2(d)')
  Expression: 
    IDynamicMemberReferenceOperation (Member Name: "M2", Containing Type: null) (OperationKind.DynamicMemberReference, Type: System.Object) (Syntax: 'M2')
      Type Arguments(0)
      Instance Receiver: 
        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'M2')
  Arguments(1):
      IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'd')
  ArgumentNames(0)
  ArgumentRefKinds: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DynamicInvocationExpression_MultipleArgumentsAndApplicableSymbols()
            Dim source = <![CDATA[
Option Strict Off

Class C
    Private Sub M(d As Object, e As Object)
        M2(d, e)'BIND:"M2(d, e)"
    End Sub

    Private Sub M2(c As Integer, d As Object)
    End Sub

    Private Sub M2(c As Long, d As Object)
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDynamicInvocationOperation (OperationKind.DynamicInvocation, Type: System.Object) (Syntax: 'M2(d, e)')
  Expression: 
    IDynamicMemberReferenceOperation (Member Name: "M2", Containing Type: null) (OperationKind.DynamicMemberReference, Type: System.Object) (Syntax: 'M2')
      Type Arguments(0)
      Instance Receiver: 
        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'M2')
  Arguments(2):
      IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'd')
      IParameterReferenceOperation: e (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'e')
  ArgumentNames(0)
  ArgumentRefKinds: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DynamicInvocationExpression_ArgumentNames()
            Dim source = <![CDATA[
Option Strict Off

Class C
    Private Sub M(d As Object, e As Object)
        M2(d:=d, c:=e)'BIND:"M2(d:=d, c:=e)"
    End Sub

    Private Sub M2(c As Integer, d As Object)
    End Sub

    Private Sub M2(c As Long, d As Object)
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDynamicInvocationOperation (OperationKind.DynamicInvocation, Type: System.Object) (Syntax: 'M2(d:=d, c:=e)')
  Expression: 
    IDynamicMemberReferenceOperation (Member Name: "M2", Containing Type: null) (OperationKind.DynamicMemberReference, Type: System.Object) (Syntax: 'M2')
      Type Arguments(0)
      Instance Receiver: 
        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'M2')
  Arguments(2):
      IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'd')
      IParameterReferenceOperation: e (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'e')
  ArgumentNames(2):
    "d"
    "c"
  ArgumentRefKinds: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DynamicInvocationExpression_ArgumentRefKinds()
            Dim source = <![CDATA[
Option Strict Off
Imports System.Runtime.InteropServices

Class C
    Private Sub M(d As Object, e As Object)
        M2(d, e)'BIND:"M2(d, e)"
    End Sub

    Private Sub M2(<Out> ByRef c As Integer, ByRef d As Object)
        c = 0
    End Sub

    Private Sub M2(<Out> ByRef c As Long, ByRef d As Object)
        c = 0
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDynamicInvocationOperation (OperationKind.DynamicInvocation, Type: System.Object) (Syntax: 'M2(d, e)')
  Expression: 
    IDynamicMemberReferenceOperation (Member Name: "M2", Containing Type: null) (OperationKind.DynamicMemberReference, Type: System.Object) (Syntax: 'M2')
      Type Arguments(0)
      Instance Receiver: 
        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'M2')
  Arguments(2):
      IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'd')
      IParameterReferenceOperation: e (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'e')
  ArgumentNames(0)
  ArgumentRefKinds: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DynamicInvocationExpression_OverloadResolutionFailure()
            Dim source = <![CDATA[
Option Strict Off

Class C
    Private Sub M(d As Object)
        M2(d)'BIND:"M2(d)"
    End Sub

    Private Sub M2()
    End Sub

    Private Sub M2(c1 As Integer, c2 As Long)
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInvalidOperation (OperationKind.Invalid, Type: System.Void, IsInvalid) (Syntax: 'M2(d)')
  Children(2):
      IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'M2')
        Children(1):
            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'M2')
      IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'd')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30516: Overload resolution failed because no accessible 'M2' accepts this number of arguments.
        M2(d)'BIND:"M2(d)"
        ~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DynamicInvocationExpression_PropertyGroup_MultipleApplicableSymbols()
            Dim source = <![CDATA[
Option Strict Off

Class C
    Private Sub M(c As C, d As Object)
        Dim x = c(d)'BIND:"c(d)"
    End Sub

    Default ReadOnly Property P1(x As Integer) As Integer
        Get
            Return 1
        End Get
    End Property

    Default ReadOnly Property P1(x As String) As Integer
        Get
            Return 1
        End Get
    End Property
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDynamicInvocationOperation (OperationKind.DynamicInvocation, Type: System.Object) (Syntax: 'c(d)')
  Expression: 
    IDynamicMemberReferenceOperation (Member Name: "P1", Containing Type: null) (OperationKind.DynamicMemberReference, Type: System.Object, IsImplicit) (Syntax: 'c')
      Type Arguments(0)
      Instance Receiver: 
        IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C) (Syntax: 'c')
  Arguments(1):
      IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'd')
  ArgumentNames(0)
  ArgumentRefKinds: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DynamicInvocationExpression_PropertyGroup_MultipleArgumentsAndApplicableSymbols()
            Dim source = <![CDATA[
Option Strict Off

Class C
    Private Sub M(c As C, d As Object)
        Dim x = c(d, d)'BIND:"c(d, d)"
    End Sub

    Default ReadOnly Property P1(x As Integer, x2 As Object) As Integer
        Get
            Return 1
        End Get
    End Property

    Default ReadOnly Property P1(x As String, x2 As Object) As Integer
        Get
            Return 1
        End Get
    End Property
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDynamicInvocationOperation (OperationKind.DynamicInvocation, Type: System.Object) (Syntax: 'c(d, d)')
  Expression: 
    IDynamicMemberReferenceOperation (Member Name: "P1", Containing Type: null) (OperationKind.DynamicMemberReference, Type: System.Object, IsImplicit) (Syntax: 'c')
      Type Arguments(0)
      Instance Receiver: 
        IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C) (Syntax: 'c')
  Arguments(2):
      IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'd')
      IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'd')
  ArgumentNames(0)
  ArgumentRefKinds: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DynamicInvocationExpression_PropertyGroup_ArgumentNames()
            Dim source = <![CDATA[
Option Strict Off

Class C
    Private Sub M(c As C, d As Object, e As Object)
        Dim x = c(x2:=e, x:=d)'BIND:"c(x2:=e, x:=d)"
    End Sub

    Default ReadOnly Property P1(x As Integer, x2 As Object) As Integer
        Get
            Return 1
        End Get
    End Property

    Default ReadOnly Property P1(x As String, x2 As Object) As Integer
        Get
            Return 1
        End Get
    End Property
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDynamicInvocationOperation (OperationKind.DynamicInvocation, Type: System.Object) (Syntax: 'c(x2:=e, x:=d)')
  Expression: 
    IDynamicMemberReferenceOperation (Member Name: "P1", Containing Type: null) (OperationKind.DynamicMemberReference, Type: System.Object, IsImplicit) (Syntax: 'c')
      Type Arguments(0)
      Instance Receiver: 
        IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C) (Syntax: 'c')
  Arguments(2):
      IParameterReferenceOperation: e (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'e')
      IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'd')
  ArgumentNames(2):
    "x2"
    "x"
  ArgumentRefKinds: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DynamicInvocationExpression_PropertyGroup_ArgumentRefKinds()
            Dim source = <![CDATA[
Option Strict Off
Imports System.Runtime.InteropServices

Class C
    Private Sub M(c As C, d As Object, e As Object)
        Dim x = c(x2:=e, x:=d)'BIND:"c(x2:=e, x:=d)"
    End Sub

    Default ReadOnly Property P1(x As Integer, <Out> ByRef x2 As Object) As Integer
        Get
            Return 1
        End Get
    End Property

    Default ReadOnly Property P1(x As String, <Out> ByRef x2 As Object) As Integer
        Get
            Return 1
        End Get
    End Property
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDynamicInvocationOperation (OperationKind.DynamicInvocation, Type: System.Object) (Syntax: 'c(x2:=e, x:=d)')
  Expression: 
    IDynamicMemberReferenceOperation (Member Name: "P1", Containing Type: null) (OperationKind.DynamicMemberReference, Type: System.Object, IsImplicit) (Syntax: 'c')
      Type Arguments(0)
      Instance Receiver: 
        IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C) (Syntax: 'c')
  Arguments(2):
      IParameterReferenceOperation: e (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'e')
      IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'd')
  ArgumentNames(2):
    "x2"
    "x"
  ArgumentRefKinds: null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30651: property parameters cannot be declared 'ByRef'.
    Default ReadOnly Property P1(x As Integer, <Out> ByRef x2 As Object) As Integer
                                                     ~~~~~
BC30651: property parameters cannot be declared 'ByRef'.
    Default ReadOnly Property P1(x As String, <Out> ByRef x2 As Object) As Integer
                                                    ~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DynamicInvocationExpression_PropertyGroup_OverloadResolutionFailure()
            Dim source = <![CDATA[
Option Strict Off

Class C
    Private Sub M(c As C, d As Object)
        Dim x = c(c, d)'BIND:"c(c, d)"
    End Sub

    Default ReadOnly Property P1(x As Integer, x2 As Object) As Integer
        Get
            Return 1
        End Get
    End Property

    Default ReadOnly Property P1(x As String, x2 As Object) As Integer
        Get
            Return 1
        End Get
    End Property
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInvalidOperation (OperationKind.Invalid, Type: System.Int32, IsInvalid) (Syntax: 'c(c, d)')
  Children(3):
      IOperation:  (OperationKind.None, Type: null, IsInvalid, IsImplicit) (Syntax: 'c')
        Children(1):
            IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C, IsInvalid) (Syntax: 'c')
      IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C) (Syntax: 'c')
      IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'd')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30518: Overload resolution failed because no accessible 'P1' can be called with these arguments:
    'Public ReadOnly Default Property P1(x As Integer, x2 As Object) As Integer': Value of type 'C' cannot be converted to 'Integer'.
    'Public ReadOnly Default Property P1(x As String, x2 As Object) As Integer': Value of type 'C' cannot be converted to 'String'.
        Dim x = c(c, d)'BIND:"c(c, d)"
                ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DynamicInvocationExpression_SimpleInvokeDelegateWithArgument()
            Dim source = <![CDATA[
Option Strict Off

Class C
    Delegate Sub MySubDelegate(ByVal x As Integer)

    Private Sub M(d As MySubDelegate, o As Object)
        d(o)'BIND:"d(o)"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInvocationOperation (virtual Sub C.MySubDelegate.Invoke(x As System.Int32)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'd(o)')
  Instance Receiver: 
    IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: C.MySubDelegate) (Syntax: 'd')
  Arguments(1):
      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: 'o')
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsImplicit) (Syntax: 'o')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            IParameterReferenceOperation: o (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DynamicInvocationExpression_InvokeDelegateWithArgument()
            Dim source = <![CDATA[
Option Strict Off
Imports System.Runtime.InteropServices

Class C
    Delegate Sub MySubDelegate(ByVal x As Integer)

    Private Sub M(c As C, d As Object)
        Dim x = c(d)(d)'BIND:"c(d)(d)"
    End Sub

    Default ReadOnly Property P1(x As Integer) As MySubDelegate
        Get
            Return Nothing
        End Get
    End Property

    Default ReadOnly Property P1(x As String) As MySubDelegate
        Get
            Return Nothing
        End Get
    End Property
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDynamicInvocationOperation (OperationKind.DynamicInvocation, Type: System.Object) (Syntax: 'c(d)(d)')
  Expression: 
    IDynamicInvocationOperation (OperationKind.DynamicInvocation, Type: System.Object) (Syntax: 'c(d)')
      Expression: 
        IDynamicMemberReferenceOperation (Member Name: "P1", Containing Type: null) (OperationKind.DynamicMemberReference, Type: System.Object, IsImplicit) (Syntax: 'c')
          Type Arguments(0)
          Instance Receiver: 
            IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C) (Syntax: 'c')
      Arguments(1):
          IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'd')
      ArgumentNames(0)
      ArgumentRefKinds: null
  Arguments(1):
      IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'd')
  ArgumentNames(0)
  ArgumentRefKinds: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub DynamicInvocation_NoControlFlow()
            Dim source = <![CDATA[
Option Strict Off

Class C
    Private Sub M(c As Object, d As Object)'BIND:"Private Sub M(c As Object, d As Object)"
        c.M2(d)
    End Sub
End Class]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'c.M2(d)')
          Expression: 
            IDynamicInvocationOperation (OperationKind.DynamicInvocation, Type: System.Object) (Syntax: 'c.M2(d)')
              Expression: 
                IDynamicMemberReferenceOperation (Member Name: "M2", Containing Type: null) (OperationKind.DynamicMemberReference, Type: System.Object) (Syntax: 'c.M2')
                  Type Arguments(0)
                  Instance Receiver: 
                    IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'c')
              Arguments(1):
                  IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'd')
              ArgumentNames(0)
              ArgumentRefKinds: null

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
        Public Sub DynamicInvocation_ControlFlowNullReceiver()
            ' Also tests non-zero TypeArguments and non-null ContainingType for IDynamicMemberReferenceOperation
            Dim source = <![CDATA[
Option Strict Off

Class C
    Private Sub M(d1 As Object, d2 As Object) 'BIND:"Private Sub M(d1 As Object, d2 As Object)"
        C.M2(Of Integer)(If(d1, d2))
    End Sub

    Private Shared Sub M2(Of T)(d As Integer)
    End Sub

    Private Shared Sub M2(Of T)(d As Char)
    End Sub
End Class]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.locals {R1}
{
    CaptureIds: [1]
    .locals {R2}
    {
        CaptureIds: [0]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (1)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd1')
                  Value: 
                    IParameterReferenceOperation: d1 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'd1')

            Jump if True (Regular) to Block[B3]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'd1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'd1')
                Leaving: {R2}

            Next (Regular) Block[B2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd1')
                  Value: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'd1')

            Next (Regular) Block[B4]
                Leaving: {R2}
    }

    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd2')
              Value: 
                IParameterReferenceOperation: d2 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'd2')

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'C.M2(Of Int ... If(d1, d2))')
              Expression: 
                IDynamicInvocationOperation (OperationKind.DynamicInvocation, Type: System.Object) (Syntax: 'C.M2(Of Int ... If(d1, d2))')
                  Expression: 
                    IDynamicMemberReferenceOperation (Member Name: "M2", Containing Type: C) (OperationKind.DynamicMemberReference, Type: System.Object) (Syntax: 'C.M2(Of Integer)')
                      Type Arguments(1):
                        Symbol: System.Int32
                      Instance Receiver: 
                        null
                  Arguments(1):
                      IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'If(d1, d2)')
                  ArgumentNames(0)
                  ArgumentRefKinds: null

        Next (Regular) Block[B5]
            Leaving: {R1}
}

Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub DynamicInvocation_ControlFlowInReceiver()
            Dim source = <![CDATA[
Option Strict Off

Class C
    Private Sub M(c As Object, d1 As Object, d2 As Object)'BIND:"Private Sub M(c As Object, d1 As Object, d2 As Object)"
        Call If(d1, d2).M2(c)
    End Sub
End Class]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.locals {R1}
{
    CaptureIds: [1]
    .locals {R2}
    {
        CaptureIds: [0]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (1)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd1')
                  Value: 
                    IParameterReferenceOperation: d1 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'd1')

            Jump if True (Regular) to Block[B3]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'd1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'd1')
                Leaving: {R2}

            Next (Regular) Block[B2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd1')
                  Value: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'd1')

            Next (Regular) Block[B4]
                Leaving: {R2}
    }

    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd2')
              Value: 
                IParameterReferenceOperation: d2 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'd2')

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Call If(d1, d2).M2(c)')
              Expression: 
                IDynamicInvocationOperation (OperationKind.DynamicInvocation, Type: System.Object) (Syntax: 'If(d1, d2).M2(c)')
                  Expression: 
                    IDynamicMemberReferenceOperation (Member Name: "M2", Containing Type: null) (OperationKind.DynamicMemberReference, Type: System.Object) (Syntax: 'If(d1, d2).M2')
                      Type Arguments(0)
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'If(d1, d2)')
                  Arguments(1):
                      IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'c')
                  ArgumentNames(0)
                  ArgumentRefKinds: null

        Next (Regular) Block[B5]
            Leaving: {R1}
}

Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub DynamicInvocation_ControlFlowInReceiverEmptyArgList()
            Dim source = <![CDATA[
Option Strict Off

Class C
    Private Sub M(d1 As Object, d2 As Object)'BIND:"Private Sub M(d1 As Object, d2 As Object)"
        Call If(d1, d2).M2()
    End Sub
End Class]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.locals {R1}
{
    CaptureIds: [1]
    .locals {R2}
    {
        CaptureIds: [0]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (1)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd1')
                  Value: 
                    IParameterReferenceOperation: d1 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'd1')

            Jump if True (Regular) to Block[B3]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'd1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'd1')
                Leaving: {R2}

            Next (Regular) Block[B2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd1')
                  Value: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'd1')

            Next (Regular) Block[B4]
                Leaving: {R2}
    }

    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd2')
              Value: 
                IParameterReferenceOperation: d2 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'd2')

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Call If(d1, d2).M2()')
              Expression: 
                IDynamicInvocationOperation (OperationKind.DynamicInvocation, Type: System.Object) (Syntax: 'If(d1, d2).M2()')
                  Expression: 
                    IDynamicMemberReferenceOperation (Member Name: "M2", Containing Type: null) (OperationKind.DynamicMemberReference, Type: System.Object) (Syntax: 'If(d1, d2).M2')
                      Type Arguments(0)
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'If(d1, d2)')
                  Arguments(0)
                  ArgumentNames(0)
                  ArgumentRefKinds: null

        Next (Regular) Block[B5]
            Leaving: {R1}
}

Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub DynamicInvocation_ControlFlowInFirstArgument()
            Dim source = <![CDATA[
Option Strict Off

Class C
    Private Sub M(c As Object, d1 As Object, d2 As Object, d3 As Object)'BIND:"Private Sub M(c As Object, d1 As Object, d2 As Object, d3 As Object)"
        c.M2(If(d1, d2), d3)
    End Sub
End Class]]>.Value

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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value: 
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'c')

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
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'c.M2(If(d1, d2), d3)')
              Expression: 
                IDynamicInvocationOperation (OperationKind.DynamicInvocation, Type: System.Object) (Syntax: 'c.M2(If(d1, d2), d3)')
                  Expression: 
                    IDynamicMemberReferenceOperation (Member Name: "M2", Containing Type: null) (OperationKind.DynamicMemberReference, Type: System.Object) (Syntax: 'c.M2')
                      Type Arguments(0)
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'c')
                  Arguments(2):
                      IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'If(d1, d2)')
                      IParameterReferenceOperation: d3 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'd3')
                  ArgumentNames(0)
                  ArgumentRefKinds: null

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
        Public Sub DynamicInvocation_ControlFlowInSecondArgument()
            Dim source = <![CDATA[
Option Strict Off

Class C
    Private Sub M(c As Object, d1 As Object, d2 As Object, d3 As Object)'BIND:"Private Sub M(c As Object, d1 As Object, d2 As Object, d3 As Object)"
        c.M2(d1, If(d2, d3))
    End Sub
End Class]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [1] [3]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value: 
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'c')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd1')
              Value: 
                IParameterReferenceOperation: d1 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'd1')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd2')
                  Value: 
                    IParameterReferenceOperation: d2 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'd2')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'd2')
                  Operand: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'd2')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd2')
                  Value: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'd2')

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd3')
              Value: 
                IParameterReferenceOperation: d3 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'd3')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'c.M2(d1, If(d2, d3))')
              Expression: 
                IDynamicInvocationOperation (OperationKind.DynamicInvocation, Type: System.Object) (Syntax: 'c.M2(d1, If(d2, d3))')
                  Expression: 
                    IDynamicMemberReferenceOperation (Member Name: "M2", Containing Type: null) (OperationKind.DynamicMemberReference, Type: System.Object) (Syntax: 'c.M2')
                      Type Arguments(0)
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'c')
                  Arguments(2):
                      IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'd1')
                      IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'If(d2, d3)')
                  ArgumentNames(0)
                  ArgumentRefKinds: null

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
        Public Sub DynamicInvocation_ControlFlowInMultipleArguments()
            Dim source = <![CDATA[
Option Strict Off

Class C
    Private Sub M(d1 As Object, d2 As Object, d3 As Object, d4 As Object)'BIND:"Private Sub M(d1 As Object, d2 As Object, d3 As Object, d4 As Object)"
        M2(d:=If(d1, d2), c:=If(d3, d4))
    End Sub

    Private Sub M2(c As Integer, d As Object)
    End Sub

    Private Sub M2(c As Long, d As Object)
    End Sub
End Class]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [2] [4]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'M2')
              Value: 
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'M2')

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
                Entering: {R3}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd2')
              Value: 
                IParameterReferenceOperation: d2 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'd2')

        Next (Regular) Block[B5]
            Entering: {R3}

    .locals {R3}
    {
        CaptureIds: [3]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd3')
                  Value: 
                    IParameterReferenceOperation: d3 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'd3')

            Jump if True (Regular) to Block[B7]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'd3')
                  Operand: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'd3')
                Leaving: {R3}

            Next (Regular) Block[B6]
        Block[B6] - Block
            Predecessors: [B5]
            Statements (1)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd3')
                  Value: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'd3')

            Next (Regular) Block[B8]
                Leaving: {R3}
    }

    Block[B7] - Block
        Predecessors: [B5]
        Statements (1)
            IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd4')
              Value: 
                IParameterReferenceOperation: d4 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'd4')

        Next (Regular) Block[B8]
    Block[B8] - Block
        Predecessors: [B6] [B7]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'M2(d:=If(d1 ... If(d3, d4))')
              Expression: 
                IDynamicInvocationOperation (OperationKind.DynamicInvocation, Type: System.Object) (Syntax: 'M2(d:=If(d1 ... If(d3, d4))')
                  Expression: 
                    IDynamicMemberReferenceOperation (Member Name: "M2", Containing Type: null) (OperationKind.DynamicMemberReference, Type: System.Object) (Syntax: 'M2')
                      Type Arguments(0)
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'M2')
                  Arguments(2):
                      IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'If(d1, d2)')
                      IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'If(d3, d4)')
                  ArgumentNames(2):
                    "d"
                    "c"
                  ArgumentRefKinds: null

        Next (Regular) Block[B9]
            Leaving: {R1}
}

Block[B9] - Exit
    Predecessors: [B8]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub DynamicInvocation_ControlFlowInvocationOffObject()
            Dim source = <![CDATA[
Option Strict Off

Class C
    Private Sub M(c1 As C, c2 As C, d As Object, p As Object) 'BIND:"Private Sub M(c1 As C, c2 As C, d As Object, p As Object)"
        p = If(c1, c2)(d)
    End Sub

    Default ReadOnly Property P1(x As Integer) As Integer
        Get
            Return 1
        End Get
    End Property

    Default ReadOnly Property P1(x As String) As Integer
        Get
            Return 1
        End Get
    End Property
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
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                  Value: 
                    IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C) (Syntax: 'c1')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                  Value: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c2')
              Value: 
                IParameterReferenceOperation: c2 (OperationKind.ParameterReference, Type: C) (Syntax: 'c2')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'p = If(c1, c2)(d)')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object, IsImplicit) (Syntax: 'p = If(c1, c2)(d)')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'p')
                  Right: 
                    IDynamicInvocationOperation (OperationKind.DynamicInvocation, Type: System.Object) (Syntax: 'If(c1, c2)(d)')
                      Expression: 
                        IDynamicMemberReferenceOperation (Member Name: "P1", Containing Type: null) (OperationKind.DynamicMemberReference, Type: System.Object, IsImplicit) (Syntax: 'If(c1, c2)')
                          Type Arguments(0)
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'If(c1, c2)')
                      Arguments(1):
                          IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'd')
                      ArgumentNames(0)
                      ArgumentRefKinds: null

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
        Public Sub DynamicInvocation_ControlFlowInArgumentWithDelegateInvocation()
            Dim source = <![CDATA[
Option Strict Off
Imports System.Runtime.InteropServices

Class C
    Delegate Sub MySubDelegate(ByVal x As Integer)

    Private Sub M(c As C, d1 As Object, d2 As Object, d3 As Object, p As Object)'BIND:"Private Sub M(c As C, d1 As Object, d2 As Object, d3 As Object, p As Object)"
        p = c(If(d1, d2))(d3)
    End Sub

    Default ReadOnly Property P1(x As Integer) As MySubDelegate
        Get
            Return Nothing
        End Get
    End Property

    Default ReadOnly Property P1(x As String) As MySubDelegate
        Get
            Return Nothing
        End Get
    End Property
End Class
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [1] [3]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'p')
              Value: 
                IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'p')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value: 
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C) (Syntax: 'c')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd1')
                  Value: 
                    IParameterReferenceOperation: d1 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'd1')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'd1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'd1')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd1')
                  Value: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'd1')

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd2')
              Value: 
                IParameterReferenceOperation: d2 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'd2')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'p = c(If(d1, d2))(d3)')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object, IsImplicit) (Syntax: 'p = c(If(d1, d2))(d3)')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'p')
                  Right: 
                    IDynamicInvocationOperation (OperationKind.DynamicInvocation, Type: System.Object) (Syntax: 'c(If(d1, d2))(d3)')
                      Expression: 
                        IDynamicInvocationOperation (OperationKind.DynamicInvocation, Type: System.Object) (Syntax: 'c(If(d1, d2))')
                          Expression: 
                            IDynamicMemberReferenceOperation (Member Name: "P1", Containing Type: null) (OperationKind.DynamicMemberReference, Type: System.Object, IsImplicit) (Syntax: 'c')
                              Type Arguments(0)
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c')
                          Arguments(1):
                              IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'If(d1, d2)')
                          ArgumentNames(0)
                          ArgumentRefKinds: null
                      Arguments(1):
                          IParameterReferenceOperation: d3 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'd3')
                      ArgumentNames(0)
                      ArgumentRefKinds: null

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
    End Class
End Namespace

