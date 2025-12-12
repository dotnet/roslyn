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
        Public Sub TestGetType()
            Dim source = <![CDATA[
Imports System
Class C
    Sub M(t As Type)
        t = GetType(Integer)'BIND:"GetType(Integer)"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITypeOfOperation (OperationKind.TypeOf, Type: System.Type) (Syntax: 'GetType(Integer)')
  TypeOperand: System.Int32
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of GetTypeExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TestGetType_NonPrimitiveTypeArgument()
            Dim source = <![CDATA[
Imports System
Class C
    Sub M(t As Type)
        t = GetType(C)'BIND:"GetType(C)"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITypeOfOperation (OperationKind.TypeOf, Type: System.Type) (Syntax: 'GetType(C)')
  TypeOperand: C
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of GetTypeExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TestGetType_ErrorTypeArgument()
            Dim source = <![CDATA[
Imports System
Class C
    Sub M(t As Type)
        t = GetType(UndefinedType)'BIND:"GetType(UndefinedType)"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITypeOfOperation (OperationKind.TypeOf, Type: System.Type, IsInvalid) (Syntax: 'GetType(UndefinedType)')
  TypeOperand: UndefinedType
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30002: Type 'UndefinedType' is not defined.
        t = GetType(UndefinedType)'BIND:"GetType(UndefinedType)"
                    ~~~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of GetTypeExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TestGetType_IdentifierArgument()
            Dim source = <![CDATA[
Imports System
Class C
    Sub M(t As Type)
        t = GetType(t)'BIND:"GetType(t)"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITypeOfOperation (OperationKind.TypeOf, Type: System.Type, IsInvalid) (Syntax: 'GetType(t)')
  TypeOperand: t
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30002: Type 't' is not defined.
        t = GetType(t)'BIND:"GetType(t)"
                    ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of GetTypeExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TestGetType_ExpressionArgument()
            Dim source = <![CDATA[
Imports System
Class C
    Sub M(t As Type)
        t = GetType(M2())'BIND:"GetType(M2())"
    End Sub

    Function M2() As Type
        Return Nothing
    End Function
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITypeOfOperation (OperationKind.TypeOf, Type: System.Type, IsInvalid) (Syntax: 'GetType(M2())')
  TypeOperand: M2()
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30002: Type 'M2' is not defined.
        t = GetType(M2())'BIND:"GetType(M2())"
                    ~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of GetTypeExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TestGetType_MissingArgument()
            Dim source = <![CDATA[
Imports System
Class C
    Sub M(t As Type)
        t = GetType()'BIND:"GetType()"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITypeOfOperation (OperationKind.TypeOf, Type: System.Type, IsInvalid) (Syntax: 'GetType()')
  TypeOperand: ?
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30182: Type expected.
        t = GetType()'BIND:"GetType()"
                    ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of GetTypeExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact>
        Public Sub TypeOfFlow_01()
            Dim source = <![CDATA[
Imports System
Class C
    Public Sub M(t As Type)'BIND:"Public Sub M(t As Type)"
        t = GetType(Boolean)
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 't = GetType(Boolean)')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Type, IsImplicit) (Syntax: 't = GetType(Boolean)')
              Left: 
                IParameterReferenceOperation: t (OperationKind.ParameterReference, Type: System.Type) (Syntax: 't')
              Right: 
                ITypeOfOperation (OperationKind.TypeOf, Type: System.Type) (Syntax: 'GetType(Boolean)')
                  TypeOperand: System.Boolean

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub IsTypeFlow_01()
            Dim source = <![CDATA[
Class C
    Sub M(c As C2, b As Boolean)'BIND:"Sub M(c As C2, b As Boolean)"
        b = TypeOf c Is C2
    End Sub

    Class C2 : Dim i As Integer : End Class
End Class
]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = TypeOf c Is C2')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'b = TypeOf c Is C2')
              Left: 
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
              Right: 
                IIsTypeOperation (OperationKind.IsType, Type: System.Boolean) (Syntax: 'TypeOf c Is C2')
                  Operand: 
                    IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C.C2) (Syntax: 'c')
                  IsType: C.C2

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub IsTypeFlow_02()
            Dim source = <![CDATA[
Class C
    Sub M(x As C2, y As C2, b As Boolean)'BIND:"Sub M(x As C2, y As C2, b As Boolean)"
        b = TypeOf If(x, y) IsNot C2
    End Sub

    Class C2 : Dim i As Integer : End Class
End Class
]]>.Value

            Dim expectedDiagnostics = String.Empty

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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
              Value: 
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x')
                  Value: 
                    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: C.C2) (Syntax: 'x')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'x')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C.C2, IsImplicit) (Syntax: 'x')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x')
                  Value: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C.C2, IsImplicit) (Syntax: 'x')

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'y')
              Value: 
                IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: C.C2) (Syntax: 'y')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = TypeOf  ... y) IsNot C2')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'b = TypeOf  ... y) IsNot C2')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Boolean, IsImplicit) (Syntax: 'b')
                  Right: 
                    IIsTypeOperation (IsNotExpression) (OperationKind.IsType, Type: System.Boolean) (Syntax: 'TypeOf If(x, y) IsNot C2')
                      Operand: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: C.C2, IsImplicit) (Syntax: 'If(x, y)')
                      IsType: C.C2

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub
    End Class
End Namespace
