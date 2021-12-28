' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase

        <CompilerTrait(CompilerFeature.IOperation), WorkItem(21769, "https://github.com/dotnet/roslyn/issues/21769")>
        <Fact()>
        Public Sub PropertyReferenceExpression_PropertyReferenceInWithDerivedTypeUsesDerivedTypeAsInstanceType_LValue()
            Dim source = <![CDATA[
Option Strict On
Module M1
    Sub Method1()
        Dim c2 As C2 = New C2 With {.P1 = New Object}'BIND:"P1"
    End Sub

    Class C1
        Public Overridable Property P1 As Object
    End Class

    Class C2
        Inherits C1
    End Class
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IPropertyReferenceOperation: Property M1.C1.P1 As System.Object (OperationKind.PropertyReference, Type: System.Object) (Syntax: 'P1')
  Instance Receiver: 
    IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: M1.C2, IsImplicit) (Syntax: 'New C2 With ... New Object}')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of IdentifierNameSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation), WorkItem(21769, "https://github.com/dotnet/roslyn/issues/21769")>
        <Fact()>
        Public Sub PropertyReferenceExpression_PropertyReferenceInWithDerivedTypeUsesDerivedTypeAsInstanceType_RValue()
            Dim source = <![CDATA[
Option Strict On
Module M1
    Sub Method1()
        Dim c2 As C2 = New C2 With {.P2 = .P1}'BIND:".P1"
        c2.P1 = Nothing
    End Sub

    Class C1
        Public Overridable Property P1 As Object
        Public Property P2 As Object
    End Class

    Class C2
        Inherits C1
    End Class
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IPropertyReferenceOperation: Property M1.C1.P1 As System.Object (OperationKind.PropertyReference, Type: System.Object) (Syntax: '.P1')
  Instance Receiver: 
    IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: M1.C2, IsImplicit) (Syntax: 'New C2 With {.P2 = .P1}')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of MemberAccessExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub IPropertyReference_SharedPropertyWithInstanceReceiver()
            Dim source = <![CDATA[
Option Strict On
Imports System

Module M1
    Class C1
        Shared Property P1 As Integer
        Shared Sub S2()
            Dim c1Instance As New C1
            Dim i1 As Integer = c1Instance.P1'BIND:"c1Instance.P1"
        End Sub
    End Class
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IPropertyReferenceOperation: Property M1.C1.P1 As System.Int32 (Static) (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'c1Instance.P1')
  Instance Receiver: 
    ILocalReferenceOperation: c1Instance (OperationKind.LocalReference, Type: M1.C1) (Syntax: 'c1Instance')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
            Dim i1 As Integer = c1Instance.P1'BIND:"c1Instance.P1"
                                ~~~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of MemberAccessExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub IPropertyReference_SharedPropertyAccessOnClass()
            Dim source = <![CDATA[
Option Strict On
Imports System

Module M1
    Class C1
        Shared Property P1 As Integer
        Shared Sub S2()
            Dim i1 As Integer = C1.P1'BIND:"C1.P1"
        End Sub
    End Class
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IPropertyReferenceOperation: Property M1.C1.P1 As System.Int32 (Static) (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'C1.P1')
  Instance Receiver: 
    null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of MemberAccessExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub IPropertyReference_InstancePropertyAccessOnClass()
            Dim source = <![CDATA[
Option Strict On
Imports System

Module M1
    Class C1
        Property P1 As Integer
        Shared Sub S2()
            Dim i1 As Integer = C1.P1'BIND:"C1.P1"
        End Sub
    End Class
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IPropertyReferenceOperation: Property M1.C1.P1 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsInvalid) (Syntax: 'C1.P1')
  Instance Receiver: 
    null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30469: Reference to a non-shared member requires an object reference.
            Dim i1 As Integer = C1.P1'BIND:"C1.P1"
                                ~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of MemberAccessExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub IPropertyReference_SharedProperty()
            Dim source = <![CDATA[
Option Strict On
Imports System

Module M1
    Class C1
        Shared Property P1 As Integer
        Shared Sub S2()
            Dim i1 = P1'BIND:"P1"
        End Sub
    End Class
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IPropertyReferenceOperation: Property M1.C1.P1 As System.Int32 (Static) (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'P1')
  Instance Receiver: 
    null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of IdentifierNameSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact>
        Public Sub PropertyReference_NoControlFlow()
            ' Verify mix of property references with implicit/explicit/null instance in lvalue/rvalue contexts.
            ' Also verifies property with arguments.
            Dim source = <![CDATA[
Imports System

Friend Class C
    Private Property P1 As Integer
    Private Shared Property P2 As Integer
    Private ReadOnly Property P3(i As Integer) As Integer
        Get
            Return 0
        End Get
    End Property

    Public Sub M(c As C, i As Integer)'BIND:"Public Sub M(c As C, i As Integer)"
        P1 = C.P2 + c.P3(i)
        P2 = Me.P1 + c.P1
    End Sub
End Class]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (2)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'P1 = C.P2 + c.P3(i)')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Void, IsImplicit) (Syntax: 'P1 = C.P2 + c.P3(i)')
              Left: 
                IPropertyReferenceOperation: Property C.P1 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'P1')
                  Instance Receiver: 
                    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'P1')
              Right: 
                IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32) (Syntax: 'C.P2 + c.P3(i)')
                  Left: 
                    IPropertyReferenceOperation: Property C.P2 As System.Int32 (Static) (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'C.P2')
                      Instance Receiver: 
                        null
                  Right: 
                    IPropertyReferenceOperation: ReadOnly Property C.P3(i As System.Int32) As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'c.P3(i)')
                      Instance Receiver: 
                        IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C) (Syntax: 'c')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'i')
                            IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'P2 = Me.P1 + c.P1')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Void, IsImplicit) (Syntax: 'P2 = Me.P1 + c.P1')
              Left: 
                IPropertyReferenceOperation: Property C.P2 As System.Int32 (Static) (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'P2')
                  Instance Receiver: 
                    null
              Right: 
                IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32) (Syntax: 'Me.P1 + c.P1')
                  Left: 
                    IPropertyReferenceOperation: Property C.P1 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'Me.P1')
                      Instance Receiver: 
                        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C) (Syntax: 'Me')
                  Right: 
                    IPropertyReferenceOperation: Property C.P1 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'c.P1')
                      Instance Receiver: 
                        IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C) (Syntax: 'c')

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact>
        Public Sub PropertyReference_ControlFlowInReceiver()
            Dim source = <![CDATA[
Imports System

Friend Class C
    Public ReadOnly Property P1(i As Integer) As Integer
        Get
            Return 0
        End Get
    End Property

    Public Sub M(c1 As C, c2 As C, i As Integer, p As Integer)'BIND:"Public Sub M(c1 As C, c2 As C, i As Integer, p As Integer)"
        p = If(c1, c2).P1(i)
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'p')
              Value: 
                IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'p')

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
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'p = If(c1, c2).P1(i)')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'p = If(c1, c2).P1(i)')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'p')
                  Right: 
                    IPropertyReferenceOperation: ReadOnly Property C.P1(i As System.Int32) As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'If(c1, c2).P1(i)')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'If(c1, c2)')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'i')
                            IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

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
        <Fact>
        Public Sub PropertyReference_ControlFlowInReceiver_StaticProperty()
            Dim source = <![CDATA[
Imports System

Friend Class C
    Public Shared ReadOnly Property P1 As Integer

    Public Sub M(c1 As C, c2 As C, p1 As Integer, p2 As Integer)'BIND:"Public Sub M(c1 As C, c2 As C, p1 As Integer, p2 As Integer)"
        p1 = c1.P1
        p2 = If(c1, c2).P1
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
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'p1 = c1.P1')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'p1 = c1.P1')
              Left: 
                IParameterReferenceOperation: p1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'p1')
              Right: 
                IPropertyReferenceOperation: ReadOnly Property C.P1 As System.Int32 (Static) (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'c1.P1')
                  Instance Receiver: 
                    null

        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'p2 = If(c1, c2).P1')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'p2 = If(c1, c2).P1')
              Left: 
                IParameterReferenceOperation: p2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'p2')
              Right: 
                IPropertyReferenceOperation: ReadOnly Property C.P1 As System.Int32 (Static) (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'If(c1, c2).P1')
                  Instance Receiver: 
                    null

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        p1 = c1.P1
             ~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        p2 = If(c1, c2).P1
             ~~~~~~~~~~~~~
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact>
        Public Sub PropertyReference_ControlFlowInFirstArgument()
            Dim source = <![CDATA[
Imports System

Friend Class C
    Public ReadOnly Property P1(i1 As Integer, i2 As Integer) As Integer
        Get
            Return 0
        End Get
    End Property

    Public Sub M(c As C, i1 As Integer?, i2 As Integer, i3 As Integer, p As Integer)'BIND:"Public Sub M(c As C, i1 As Integer?, i2 As Integer, i3 As Integer, p As Integer)"
        p = c.P1(If(i1, i2), i3)
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'p')
              Value: 
                IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'p')

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
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
                  Value: 
                    IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'i1')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'i1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'i1')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
                  Value: 
                    IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'i1')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'i1')
                      Arguments(0)

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i2')
              Value: 
                IParameterReferenceOperation: i2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i2')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'p = c.P1(If(i1, i2), i3)')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'p = c.P1(If(i1, i2), i3)')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'p')
                  Right: 
                    IPropertyReferenceOperation: ReadOnly Property C.P1(i1 As System.Int32, i2 As System.Int32) As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'c.P1(If(i1, i2), i3)')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c')
                      Arguments(2):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i1) (OperationKind.Argument, Type: null) (Syntax: 'If(i1, i2)')
                            IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(i1, i2)')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i2) (OperationKind.Argument, Type: null) (Syntax: 'i3')
                            IParameterReferenceOperation: i3 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i3')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

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
        <Fact>
        Public Sub PropertyReference_ControlFlowInSecondArgument()
            Dim source = <![CDATA[
Imports System

Friend Class C
    Public ReadOnly Property P1(i1 As Integer, i2 As Integer) As Integer
        Get
            Return 0
        End Get
    End Property

    Public Sub M(c As C, i1 As Integer?, i2 As Integer, i3 As Integer, p As Integer)'BIND:"Public Sub M(c As C, i1 As Integer?, i2 As Integer, i3 As Integer, p As Integer)"
        p = c.P1(i3, If(i1, i2))
    End Sub
End Class]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [1] [2] [4]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (3)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'p')
              Value: 
                IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'p')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value: 
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C) (Syntax: 'c')

            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i3')
              Value: 
                IParameterReferenceOperation: i3 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i3')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [3]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
                  Value: 
                    IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'i1')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'i1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'i1')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
                  Value: 
                    IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'i1')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'i1')
                      Arguments(0)

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i2')
              Value: 
                IParameterReferenceOperation: i2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i2')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'p = c.P1(i3, If(i1, i2))')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'p = c.P1(i3, If(i1, i2))')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'p')
                  Right: 
                    IPropertyReferenceOperation: ReadOnly Property C.P1(i1 As System.Int32, i2 As System.Int32) As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'c.P1(i3, If(i1, i2))')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c')
                      Arguments(2):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i1) (OperationKind.Argument, Type: null) (Syntax: 'i3')
                            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i3')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i2) (OperationKind.Argument, Type: null) (Syntax: 'If(i1, i2)')
                            IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(i1, i2)')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

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
        <Fact>
        Public Sub PropertyReference_ControlFlowInReceiverAndArguments()
            Dim source = <![CDATA[
Imports System

Friend Class C
    Public ReadOnly Property P1(i1 As Integer, i2 As Integer) As Integer
        Get
            Return 0
        End Get
    End Property

    Public Sub M(c1 As C, c2 As C, i1 As Integer?, i2 As Integer, i3 As Integer?, i4 As Integer, p As Integer)'BIND:"Public Sub M(c1 As C, c2 As C, i1 As Integer?, i2 As Integer, i3 As Integer?, i4 As Integer, p As Integer)"
        p = If(c1, c2).P1(If(i1, i2), If(i3, i4))
    End Sub
End Class]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [2] [4] [6]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'p')
              Value: 
                IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'p')

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
                Entering: {R3}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c2')
              Value: 
                IParameterReferenceOperation: c2 (OperationKind.ParameterReference, Type: C) (Syntax: 'c2')

        Next (Regular) Block[B5]
            Entering: {R3}

    .locals {R3}
    {
        CaptureIds: [3]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
                  Value: 
                    IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'i1')

            Jump if True (Regular) to Block[B7]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'i1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'i1')
                Leaving: {R3}

            Next (Regular) Block[B6]
        Block[B6] - Block
            Predecessors: [B5]
            Statements (1)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
                  Value: 
                    IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'i1')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'i1')
                      Arguments(0)

            Next (Regular) Block[B8]
                Leaving: {R3}
                Entering: {R4}
    }

    Block[B7] - Block
        Predecessors: [B5]
        Statements (1)
            IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i2')
              Value: 
                IParameterReferenceOperation: i2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i2')

        Next (Regular) Block[B8]
            Entering: {R4}

    .locals {R4}
    {
        CaptureIds: [5]
        Block[B8] - Block
            Predecessors: [B6] [B7]
            Statements (1)
                IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i3')
                  Value: 
                    IParameterReferenceOperation: i3 (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'i3')

            Jump if True (Regular) to Block[B10]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'i3')
                  Operand: 
                    IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'i3')
                Leaving: {R4}

            Next (Regular) Block[B9]
        Block[B9] - Block
            Predecessors: [B8]
            Statements (1)
                IFlowCaptureOperation: 6 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i3')
                  Value: 
                    IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'i3')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'i3')
                      Arguments(0)

            Next (Regular) Block[B11]
                Leaving: {R4}
    }

    Block[B10] - Block
        Predecessors: [B8]
        Statements (1)
            IFlowCaptureOperation: 6 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i4')
              Value: 
                IParameterReferenceOperation: i4 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i4')

        Next (Regular) Block[B11]
    Block[B11] - Block
        Predecessors: [B9] [B10]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'p = If(c1,  ... If(i3, i4))')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'p = If(c1,  ... If(i3, i4))')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'p')
                  Right: 
                    IPropertyReferenceOperation: ReadOnly Property C.P1(i1 As System.Int32, i2 As System.Int32) As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'If(c1, c2). ... If(i3, i4))')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'If(c1, c2)')
                      Arguments(2):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i1) (OperationKind.Argument, Type: null) (Syntax: 'If(i1, i2)')
                            IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(i1, i2)')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i2) (OperationKind.Argument, Type: null) (Syntax: 'If(i3, i4)')
                            IFlowCaptureReferenceOperation: 6 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(i3, i4)')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

        Next (Regular) Block[B12]
            Leaving: {R1}
}

Block[B12] - Exit
    Predecessors: [B11]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub
    End Class
End Namespace
