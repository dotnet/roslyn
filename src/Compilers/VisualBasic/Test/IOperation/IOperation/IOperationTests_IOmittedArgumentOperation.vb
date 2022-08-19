' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Operations
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics
    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub OmittedArgument_ReferenceConversionInOmittedArgument()

            Dim source = <![CDATA[
Imports System.Collections.Generic
Public Class C
    Public Sub M1(i As Integer, i2 As Integer)
        i = M2(i2,,i2)
    End Sub

    Public Function M2(i as String, Optional i2 as IEnumerable(Of String) = Nothing, Optional i3 as String = Nothing)
        Return i2
    End Function
End Class
]]>.Value

            Dim compilation = CreateCompilation(source)
            Dim syntaxTree = compilation.SyntaxTrees(0)
            Dim semanticModel = compilation.GetSemanticModel(syntaxTree)
            Dim method = syntaxTree.GetRoot().DescendantNodes().OfType(Of MethodBlockSyntax).First()

            Dim methodOperation = semanticModel.GetOperation(method)

            ' When we fetch an omitted argument that involves a conversion, there was a bug that involved
            ' a synthesized conversion node being introduced without a corresponding BoundNode that got
            ' incorrectly duplicated. This exercises that code path to ensure that the BoundNode is correctly
            ' cached in the tree.
            Dim operationsToAnalyze = methodOperation.DescendantsAndSelf().ToArray()
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub OmittedArgumentFlow_01()
            Dim source = <![CDATA[
Public Class C
    Public Sub M1(i As Integer, i2 As Integer)'BIND:"Public Sub M1(i As Integer, i2 As Integer)"
        i = M2(i2,,)
    End Sub

    Public Function M2(i as Integer, optional i2 as Integer = 0)
        Return i2
    End Function
End Class
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
            BC30057: Too many arguments to 'Public Function M2(i As Integer, [i2 As Integer = 0]) As Object'.
        i = M2(i2,,)
                   ~
            ]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'i = M2(i2,,)')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'i = M2(i2,,)')
              Left: 
                IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i')
              Right: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'M2(i2,,)')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (NarrowingValue)
                  Operand: 
                    IInvalidOperation (OperationKind.Invalid, Type: System.Object, IsInvalid) (Syntax: 'M2(i2,,)')
                      Children(4):
                          IOperation:  (OperationKind.None, Type: null) (Syntax: 'M2')
                            Children(1):
                                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'M2')
                          IParameterReferenceOperation: i2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i2')
                          IOmittedArgumentOperation (OperationKind.OmittedArgument, Type: null) (Syntax: '')
                          IOmittedArgumentOperation (OperationKind.OmittedArgument, Type: null, IsInvalid) (Syntax: '')

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub OmittedArgumentFlow_02()
            Dim source = <![CDATA[
Public Class C
    Public Sub M1(i As Integer, i2 As Integer, b As Boolean)'BIND:"Public Sub M1(i As Integer, i2 As Integer, b As Boolean)"
        i = M2(i2, , If(b,1,2))
    End Sub

    Public Function M2(i as Integer, optional i2 as Integer = 0)
        Return i2
    End Function
End Class

]]>.Value

            Dim expectedDiagnostics = <![CDATA[
            BC30057: Too many arguments to 'Public Function M2(i As Integer, [i2 As Integer = 0]) As Object'.
        i = M2(i2, , If(b,1,2))
                     ~~~~~~~~~
            ]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [1] [2] [3]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (3)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i')
              Value: 
                IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'M2')
              Value: 
                IOperation:  (OperationKind.None, Type: null) (Syntax: 'M2')
                  Children(1):
                      IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'M2')

            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i2')
              Value: 
                IParameterReferenceOperation: i2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i2')

        Jump if False (Regular) to Block[B3]
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean, IsInvalid) (Syntax: 'b')

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: '1')
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')

        Next (Regular) Block[B4]
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: '2')
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2, IsInvalid) (Syntax: '2')

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'i = M2(i2, , If(b,1,2))')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'i = M2(i2, , If(b,1,2))')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'M2(i2, , If(b,1,2))')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (NarrowingValue)
                      Operand: 
                        IInvalidOperation (OperationKind.Invalid, Type: System.Object, IsInvalid) (Syntax: 'M2(i2, , If(b,1,2))')
                          Children(4):
                              IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: null, IsImplicit) (Syntax: 'M2')
                              IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i2')
                              IOmittedArgumentOperation (OperationKind.OmittedArgument, Type: null) (Syntax: '')
                              IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'If(b,1,2)')

        Next (Regular) Block[B5]
            Leaving: {R1}
}

Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub OmittedArgumentFlow_03()
            Dim source = <![CDATA[
Public Class C
    Public Sub M1(i As Integer, j As Integer, x As Object) 'BIND:"Public Sub M1(i As Integer, j As Integer, x As Object)"
        x.M(i,, j)
    End Sub
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
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x.M(i,, j)')
          Expression: 
            IDynamicInvocationOperation (OperationKind.DynamicInvocation, Type: System.Object) (Syntax: 'x.M(i,, j)')
              Expression: 
                IDynamicMemberReferenceOperation (Member Name: "M", Containing Type: null) (OperationKind.DynamicMemberReference, Type: System.Object) (Syntax: 'x.M')
                  Type Arguments(0)
                  Instance Receiver: 
                    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'x')
              Arguments(3):
                  IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i')
                  IOmittedArgumentOperation (OperationKind.OmittedArgument, Type: System.Object) (Syntax: '')
                  IParameterReferenceOperation: j (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'j')
              ArgumentNames(0)
              ArgumentRefKinds: null

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub OmittedArgumentFlow_04()
            Dim source = <![CDATA[
Public Class C
    Public Sub M1(o As Object, i As Integer, b As Boolean) 'BIND:"Public Sub M1(o As Object, i As Integer, b As Boolean)"
        i = o.M2( , If(b, 1, 2))
    End Sub
End Class

Public Class D
    Public Function M2(Optional i2 As Integer = 0)
        Return i2
    End Function
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
    CaptureIds: [0] [1] [2]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i')
              Value: 
                IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'o')
              Value: 
                IParameterReferenceOperation: o (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o')

        Jump if False (Regular) to Block[B3]
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '1')
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Next (Regular) Block[B4]
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '2')
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'i = o.M2( , If(b, 1, 2))')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'i = o.M2( , If(b, 1, 2))')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsImplicit) (Syntax: 'o.M2( , If(b, 1, 2))')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (NarrowingValue)
                      Operand: 
                        IDynamicInvocationOperation (OperationKind.DynamicInvocation, Type: System.Object) (Syntax: 'o.M2( , If(b, 1, 2))')
                          Expression: 
                            IDynamicMemberReferenceOperation (Member Name: "M2", Containing Type: null) (OperationKind.DynamicMemberReference, Type: System.Object) (Syntax: 'o.M2')
                              Type Arguments(0)
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'o')
                          Arguments(2):
                              IOmittedArgumentOperation (OperationKind.OmittedArgument, Type: System.Object) (Syntax: '')
                              IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'If(b, 1, 2)')
                                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                  (WideningValue)
                                Operand: 
                                  IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(b, 1, 2)')
                          ArgumentNames(0)
                          ArgumentRefKinds: null

        Next (Regular) Block[B5]
            Leaving: {R1}
}

Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub
    End Class
End Namespace
