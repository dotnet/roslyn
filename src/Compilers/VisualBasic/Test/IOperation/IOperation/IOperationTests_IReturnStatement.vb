' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub SimpleReturnFromRegularMethod()
            Dim source = <![CDATA[
Class C
    Sub M()
        Return'BIND:"Return"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IReturnOperation (OperationKind.Return, Type: null) (Syntax: 'Return')
  ReturnedValue: 
    null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ReturnStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ReturnWithValueFromRegularMethod()
            Dim source = <![CDATA[
Class C
    Function M() As Boolean
        Return True'BIND:"Return True"
    End Function
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IReturnOperation (OperationKind.Return, Type: null) (Syntax: 'Return True')
  ReturnedValue: 
    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'True')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ReturnStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub YieldFromIterator()
            Dim source = <![CDATA[
Class C
    Iterator Function M() As System.Collections.Generic.IEnumerable(Of Integer)
        Yield 0'BIND:"Yield 0"
    End Function
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IReturnOperation (OperationKind.YieldReturn, Type: null) (Syntax: 'Yield 0')
  ReturnedValue: 
    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of YieldStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ReturnFromIterator()
            Dim source = <![CDATA[
Class C
    Iterator Function M() As System.Collections.Generic.IEnumerable(Of Integer)
        Yield 0
        Return'BIND:"Return"
    End Function
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IReturnOperation (OperationKind.Return, Type: null) (Syntax: 'Return')
  ReturnedValue: 
    null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ReturnStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(7299, "https://github.com/dotnet/roslyn/issues/7299")>
        Public Sub Return_ConstantConversions_01()
            Dim source = <![CDATA[
Option Strict On
Class C
    Function M() As Byte
        Return 0.0'BIND:"Return 0.0"
    End Function
End Class
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IReturnOperation (OperationKind.Return, Type: null, IsInvalid) (Syntax: 'Return 0.0')
  ReturnedValue: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Byte, Constant: 0, IsInvalid, IsImplicit) (Syntax: '0.0')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILiteralOperation (OperationKind.Literal, Type: System.Double, Constant: 0, IsInvalid) (Syntax: '0.0')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30512: Option Strict On disallows implicit conversions from 'Double' to 'Byte'.
        Return 0.0'BIND:"Return 0.0"
               ~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ReturnStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ReturnFlow_01()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M() 'BIND:"Sub M"
        Return
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Exit
    Predecessors: [B0]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ReturnFlow_02()
            Dim source = <![CDATA[
Imports System
Public Class C
    Function M() As Integer 'BIND:"Function M"
        Return 1
    End Function
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [M As System.Int32]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (0)
        Next (Return) Block[B2]
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
            Leaving: {R1}
}

Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ReturnFlow_17()
            Dim source = <![CDATA[
Imports System
Public Class C
    Function M() As Integer 'BIND:"Function M"
        M = 1
    End Function
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [M As System.Int32]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'M = 1')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'M = 1')
                  Left: 
                    ILocalReferenceOperation: M (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'M')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Next (Return) Block[B2]
            ILocalReferenceOperation: M (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'End Function')
            Leaving: {R1}
}

Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ReturnFlow_18()
            Dim source = <![CDATA[
Imports System
Public Class C
    Function M() As Integer 'BIND:"Function M"
    End Function
End Class]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC42353: Function 'M' doesn't return a value on all code paths. Are you missing a 'Return' statement?
    End Function
    ~~~~~~~~~~~~
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [M As System.Int32]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (0)
        Next (Return) Block[B2]
            ILocalReferenceOperation: M (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'End Function')
            Leaving: {R1}
}

Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ReturnFlow_19()
            Dim source = <![CDATA[
Imports System
Public Class C
    Function M() As Integer 'BIND:"Function M"
        Return M
    End Function
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [M As System.Int32]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (0)
        Next (Return) Block[B2]
            ILocalReferenceOperation: M (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'M')
            Leaving: {R1}
}

Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

    End Class
End Namespace
