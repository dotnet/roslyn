' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(18300, "https://github.com/dotnet/roslyn/issues/18300")>
        Public Sub InterpolatedStringExpression_Empty()
            Dim source = <![CDATA[
Imports System

Friend Class [Class]
    Public Sub M()
        Console.WriteLine($"")'BIND:"$"""
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$""')
  Parts(0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InterpolatedStringExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(18300, "https://github.com/dotnet/roslyn/issues/18300")>
        Public Sub InterpolatedStringExpression_OnlyTextPart()
            Dim source = <![CDATA[
Imports System

Friend Class [Class]
    Public Sub M()
        Console.WriteLine($"Only text part")'BIND:"$"Only text part""
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$"Only text part"')
  Parts(1):
      IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: 'Only text part')
        Text: 
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "Only text part", IsImplicit) (Syntax: 'Only text part')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InterpolatedStringExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(18300, "https://github.com/dotnet/roslyn/issues/18300")>
        Public Sub InterpolatedStringExpression_OnlyInterpolationPart()
            Dim source = <![CDATA[
Imports System

Friend Class [Class]
    Public Sub M()
        Console.WriteLine($"{1}")'BIND:"$"{1}""
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$"{1}"')
  Parts(1):
      IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{1}')
        Expression: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        Alignment: 
          null
        FormatString: 
          null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InterpolatedStringExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(18300, "https://github.com/dotnet/roslyn/issues/18300")>
        Public Sub InterpolatedStringExpression_EmptyInterpolationPart()
            Dim source = <![CDATA[
Imports System

Friend Class [Class]
    Public Sub M()
        Console.WriteLine($"{}")'BIND:"$"{}""
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String, IsInvalid) (Syntax: '$"{}"')
  Parts(1):
      IInterpolationOperation (OperationKind.Interpolation, Type: null, IsInvalid) (Syntax: '{}')
        Expression: 
          IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
            Children(0)
        Alignment: 
          null
        FormatString: 
          null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30201: Expression expected.
        Console.WriteLine($"{}")'BIND:"$"{}""
                             ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of InterpolatedStringExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(18300, "https://github.com/dotnet/roslyn/issues/18300")>
        Public Sub InterpolatedStringExpression_TextAndInterpolationParts()
            Dim source = <![CDATA[
Imports System

Friend Class [Class]
    Public Sub M(x As Integer)
        Console.WriteLine($"String {x} and constant {1}")'BIND:"$"String {x} and constant {1}""
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$"String {x ... nstant {1}"')
  Parts(4):
      IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: 'String ')
        Text: 
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "String ", IsImplicit) (Syntax: 'String ')
      IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{x}')
        Expression: 
          IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
        Alignment: 
          null
        FormatString: 
          null
      IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: ' and constant ')
        Text: 
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: " and constant ", IsImplicit) (Syntax: ' and constant ')
      IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{1}')
        Expression: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        Alignment: 
          null
        FormatString: 
          null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InterpolatedStringExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(18300, "https://github.com/dotnet/roslyn/issues/18300")>
        Public Sub InterpolatedStringExpression_FormatAndAlignment()
            Dim source = <![CDATA[
Imports System

Friend Class [Class]
    Private x As String = String.Empty
    Private y As Integer = 0

    Public Sub M()
        Console.WriteLine($"String {x,20} and {y:D3} and constant {1}")'BIND:"$"String {x,20} and {y:D3} and constant {1}""
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$"String {x ... nstant {1}"')
  Parts(6):
      IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: 'String ')
        Text: 
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "String ", IsImplicit) (Syntax: 'String ')
      IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{x,20}')
        Expression: 
          IFieldReferenceOperation: [Class].x As System.String (OperationKind.FieldReference, Type: System.String) (Syntax: 'x')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: [Class], IsImplicit) (Syntax: 'x')
        Alignment: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 20) (Syntax: '20')
        FormatString: 
          null
      IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: ' and ')
        Text: 
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: " and ", IsImplicit) (Syntax: ' and ')
      IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{y:D3}')
        Expression: 
          IFieldReferenceOperation: [Class].y As System.Int32 (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'y')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: [Class], IsImplicit) (Syntax: 'y')
        Alignment: 
          null
        FormatString: 
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "D3") (Syntax: ':D3')
      IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: ' and constant ')
        Text: 
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: " and constant ", IsImplicit) (Syntax: ' and constant ')
      IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{1}')
        Expression: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        Alignment: 
          null
        FormatString: 
          null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InterpolatedStringExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(18300, "https://github.com/dotnet/roslyn/issues/18300")>
        Public Sub InterpolatedStringExpression_InterpolationAndFormatAndAlignment()
            Dim source = <![CDATA[
Imports System

Friend Class [Class]
    Private x As String = String.Empty

    Public Sub M()
        Console.WriteLine($"String {x,20:D3}")'BIND:"$"String {x,20:D3}""
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$"String {x,20:D3}"')
  Parts(2):
      IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: 'String ')
        Text: 
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "String ", IsImplicit) (Syntax: 'String ')
      IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{x,20:D3}')
        Expression: 
          IFieldReferenceOperation: [Class].x As System.String (OperationKind.FieldReference, Type: System.String) (Syntax: 'x')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: [Class], IsImplicit) (Syntax: 'x')
        Alignment: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 20) (Syntax: '20')
        FormatString: 
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "D3") (Syntax: ':D3')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InterpolatedStringExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(18300, "https://github.com/dotnet/roslyn/issues/18300")>
        Public Sub InterpolatedStringExpression_InvocationInInterpolation()
            Dim source = <![CDATA[
Imports System

Friend Class [Class]
    Public Sub M()
        Dim x As String = String.Empty
        Dim y As Integer = 0
        Console.WriteLine($"String {x} and {M2(y)} and constant {1}")'BIND:"$"String {x} and {M2(y)} and constant {1}""
    End Sub

    Private Function M2(z As Integer) As String
        Return z.ToString()
    End Function
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$"String {x ... nstant {1}"')
  Parts(6):
      IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: 'String ')
        Text: 
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "String ", IsImplicit) (Syntax: 'String ')
      IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{x}')
        Expression: 
          ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.String) (Syntax: 'x')
        Alignment: 
          null
        FormatString: 
          null
      IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: ' and ')
        Text: 
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: " and ", IsImplicit) (Syntax: ' and ')
      IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{M2(y)}')
        Expression: 
          IInvocationOperation ( Function [Class].M2(z As System.Int32) As System.String) (OperationKind.Invocation, Type: System.String) (Syntax: 'M2(y)')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: [Class], IsImplicit) (Syntax: 'M2')
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: z) (OperationKind.Argument, Type: null) (Syntax: 'y')
                  ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Alignment: 
          null
        FormatString: 
          null
      IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: ' and constant ')
        Text: 
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: " and constant ", IsImplicit) (Syntax: ' and constant ')
      IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{1}')
        Expression: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        Alignment: 
          null
        FormatString: 
          null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InterpolatedStringExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(18300, "https://github.com/dotnet/roslyn/issues/18300")>
        Public Sub InterpolatedStringExpression_NestedInterpolation()
            Dim source = <![CDATA[
Imports System

Friend Class [Class]
    Public Sub M()
        Dim x As String = String.Empty
        Dim y As Integer = 0
        Console.WriteLine($"String {M2($"{y}")}")'BIND:"$"String {M2($"{y}")}""
    End Sub

    Private Function M2(z As String) As Integer
        Return Int32.Parse(z)
    End Function
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$"String {M2($"{y}")}"')
  Parts(2):
      IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: 'String ')
        Text: 
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "String ", IsImplicit) (Syntax: 'String ')
      IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{M2($"{y}")}')
        Expression: 
          IInvocationOperation ( Function [Class].M2(z As System.String) As System.Int32) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'M2($"{y}")')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: [Class], IsImplicit) (Syntax: 'M2')
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: z) (OperationKind.Argument, Type: null) (Syntax: '$"{y}"')
                  IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$"{y}"')
                    Parts(1):
                        IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{y}')
                          Expression: 
                            ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
                          Alignment: 
                            null
                          FormatString: 
                            null
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Alignment: 
          null
        FormatString: 
          null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InterpolatedStringExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(18300, "https://github.com/dotnet/roslyn/issues/18300")>
        Public Sub InterpolatedStringExpression_InvalidExpressionInInterpolation()
            Dim source = <![CDATA[
Imports System

Friend Class [Class]
    Public Sub M(x As Integer)
        Console.WriteLine($"String {x1} and constant {[Class]}")'BIND:"$"String {x1} and constant {[Class]}""
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String, IsInvalid) (Syntax: '$"String {x ...  {[Class]}"')
  Parts(4):
      IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: 'String ')
        Text: 
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "String ", IsImplicit) (Syntax: 'String ')
      IInterpolationOperation (OperationKind.Interpolation, Type: null, IsInvalid) (Syntax: '{x1}')
        Expression: 
          IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'x1')
            Children(0)
        Alignment: 
          null
        FormatString: 
          null
      IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: ' and constant ')
        Text: 
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: " and constant ", IsImplicit) (Syntax: ' and constant ')
      IInterpolationOperation (OperationKind.Interpolation, Type: null, IsInvalid) (Syntax: '{[Class]}')
        Expression: 
          IOperation:  (OperationKind.None, Type: [Class], IsInvalid) (Syntax: '[Class]')
        Alignment: 
          null
        FormatString: 
          null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30451: 'x1' is not declared. It may be inaccessible due to its protection level.
        Console.WriteLine($"String {x1} and constant {[Class]}")'BIND:"$"String {x1} and constant {[Class]}""
                                    ~~
BC30109: '[Class]' is a class type and cannot be used as an expression.
        Console.WriteLine($"String {x1} and constant {[Class]}")'BIND:"$"String {x1} and constant {[Class]}""
                                                      ~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of InterpolatedStringExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact>
        Public Sub InterpolatedStringExpression_Empty_Flow()
            Dim source = <![CDATA[
Imports System

Friend Class [Class]
    Public Sub M(p As String)'BIND:"Public Sub M(p As String)"
        p = $""
    End Sub
End Class]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'p = $""')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String, IsImplicit) (Syntax: 'p = $""')
              Left: 
                IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.String) (Syntax: 'p')
              Right: 
                IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$""')
                  Parts(0)

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
        Public Sub InterpolatedStringExpression_OnlyTextPart_Flow()
            Dim source = <![CDATA[
Imports System

Friend Class [Class]
    Public Sub M(p As String)'BIND:"Public Sub M(p As String)"
        p = $"Only text part"
    End Sub
End Class]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'p = $"Only text part"')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String, IsImplicit) (Syntax: 'p = $"Only text part"')
              Left: 
                IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.String) (Syntax: 'p')
              Right: 
                IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$"Only text part"')
                  Parts(1):
                      IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: 'Only text part')
                        Text: 
                          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "Only text part", IsImplicit) (Syntax: 'Only text part')

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
        Public Sub InterpolatedStringExpression_OnlyInterpolationPart_Flow()
            Dim source = <![CDATA[
Imports System

Friend Class [Class]
    Public Sub M(a As Boolean, b As String, c As String, p As String)'BIND:"Public Sub M(a As Boolean, b As String, c As String, p As String)"
        p = $"{If(a, b, c)}"
    End Sub
End Class]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'p')
              Value: 
                IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.String) (Syntax: 'p')

        Jump if False (Regular) to Block[B3]
            IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'a')

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
              Value: 
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.String) (Syntax: 'b')

        Next (Regular) Block[B4]
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value: 
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.String) (Syntax: 'c')

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'p = $"{If(a, b, c)}"')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String, IsImplicit) (Syntax: 'p = $"{If(a, b, c)}"')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'p')
                  Right: 
                    IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$"{If(a, b, c)}"')
                      Parts(1):
                          IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{If(a, b, c)}')
                            Expression: 
                              IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'If(a, b, c)')
                            Alignment: 
                              null
                            FormatString: 
                              null

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
        <Fact>
        Public Sub InterpolatedStringExpression_MultipleInterpolationParts_Flow()
            Dim source = <![CDATA[
Imports System

Friend Class [Class]
    Public Sub M(a As Boolean, b As String, c As String, c2 As String, p As String)'BIND:"Public Sub M(a As Boolean, b As String, c As String, c2 As String, p As String)"
        p = $"{If(a, b, c)}{c2}"
    End Sub
End Class]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'p')
              Value: 
                IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.String) (Syntax: 'p')

        Jump if False (Regular) to Block[B3]
            IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'a')

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
              Value: 
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.String) (Syntax: 'b')

        Next (Regular) Block[B4]
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value: 
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.String) (Syntax: 'c')

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'p = $"{If(a, b, c)}{c2}"')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String, IsImplicit) (Syntax: 'p = $"{If(a, b, c)}{c2}"')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'p')
                  Right: 
                    IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$"{If(a, b, c)}{c2}"')
                      Parts(2):
                          IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{If(a, b, c)}')
                            Expression: 
                              IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'If(a, b, c)')
                            Alignment: 
                              null
                            FormatString: 
                              null
                          IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{c2}')
                            Expression: 
                              IParameterReferenceOperation: c2 (OperationKind.ParameterReference, Type: System.String) (Syntax: 'c2')
                            Alignment: 
                              null
                            FormatString: 
                              null

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
        <Fact>
        Public Sub InterpolatedStringExpression_TextAndInterpolationParts_Flow()
            Dim source = <![CDATA[
Imports System

Friend Class [Class]
    Public Sub M(a As Boolean, b As String, c As String, a2 As Boolean, b2 As String, c2 As String, p As String)'BIND:"Public Sub M(a As Boolean, b As String, c As String, a2 As Boolean, b2 As String, c2 As String, p As String)"
        p = $"String1 {If(a, b, c)} and String2 {If(a2, b2, c2)}"
    End Sub
End Class]]>.Value

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
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'p')
              Value: 
                IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.String) (Syntax: 'p')

        Jump if False (Regular) to Block[B3]
            IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'a')

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
              Value: 
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.String) (Syntax: 'b')

        Next (Regular) Block[B4]
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value: 
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.String) (Syntax: 'c')

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (0)
        Jump if False (Regular) to Block[B6]
            IParameterReferenceOperation: a2 (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'a2')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B4]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b2')
              Value: 
                IParameterReferenceOperation: b2 (OperationKind.ParameterReference, Type: System.String) (Syntax: 'b2')

        Next (Regular) Block[B7]
    Block[B6] - Block
        Predecessors: [B4]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c2')
              Value: 
                IParameterReferenceOperation: c2 (OperationKind.ParameterReference, Type: System.String) (Syntax: 'c2')

        Next (Regular) Block[B7]
    Block[B7] - Block
        Predecessors: [B5] [B6]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'p = $"Strin ... , b2, c2)}"')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String, IsImplicit) (Syntax: 'p = $"Strin ... , b2, c2)}"')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'p')
                  Right: 
                    IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$"String1 { ... , b2, c2)}"')
                      Parts(4):
                          IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: 'String1 ')
                            Text: 
                              ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "String1 ", IsImplicit) (Syntax: 'String1 ')
                          IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{If(a, b, c)}')
                            Expression: 
                              IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'If(a, b, c)')
                            Alignment: 
                              null
                            FormatString: 
                              null
                          IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: ' and String2 ')
                            Text: 
                              ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: " and String2 ", IsImplicit) (Syntax: ' and String2 ')
                          IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{If(a2, b2, c2)}')
                            Expression: 
                              IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'If(a2, b2, c2)')
                            Alignment: 
                              null
                            FormatString: 
                              null

        Next (Regular) Block[B8]
            Leaving: {R1}
}

Block[B8] - Exit
    Predecessors: [B7]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact>
        Public Sub InterpolatedStringExpression_FormatAndAlignment_Flow()
            Dim source = <![CDATA[
Imports System

Friend Class [Class]
    Public Sub M(a As Boolean, b As String, c As String, p As String)'BIND:"Public Sub M(a As Boolean, b As String, c As String, p As String)"
        p = $"{If(a, b, c),20:D3}"
    End Sub
End Class]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'p')
              Value: 
                IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.String) (Syntax: 'p')

        Jump if False (Regular) to Block[B3]
            IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'a')

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
              Value: 
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.String) (Syntax: 'b')

        Next (Regular) Block[B4]
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value: 
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.String) (Syntax: 'c')

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'p = $"{If(a ...  c),20:D3}"')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String, IsImplicit) (Syntax: 'p = $"{If(a ...  c),20:D3}"')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'p')
                  Right: 
                    IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$"{If(a, b, c),20:D3}"')
                      Parts(1):
                          IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{If(a, b, c),20:D3}')
                            Expression: 
                              IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'If(a, b, c)')
                            Alignment: 
                              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 20) (Syntax: '20')
                            FormatString: 
                              ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "D3") (Syntax: ':D3')

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
        <Fact>
        Public Sub InterpolatedStringExpression_FormatAndAlignment_Flow_02()
            Dim source = <![CDATA[
Imports System

Friend Class [Class]
    Public Sub M(a As Boolean, b As String, b2 As String, c As String, p As String)'BIND:"Public Sub M(a As Boolean, b As String, b2 As String, c As String, p As String)"
        p = $"{b2,20:D3}{If(a, b, c)}"
    End Sub
End Class]]>.Value

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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'p')
              Value: 
                IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.String) (Syntax: 'p')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b2')
              Value: 
                IParameterReferenceOperation: b2 (OperationKind.ParameterReference, Type: System.String) (Syntax: 'b2')

            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '20')
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 20) (Syntax: '20')

        Jump if False (Regular) to Block[B3]
            IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'a')

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
              Value: 
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.String) (Syntax: 'b')

        Next (Regular) Block[B4]
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value: 
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.String) (Syntax: 'c')

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'p = $"{b2,2 ... (a, b, c)}"')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String, IsImplicit) (Syntax: 'p = $"{b2,2 ... (a, b, c)}"')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'p')
                  Right: 
                    IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$"{b2,20:D3 ... (a, b, c)}"')
                      Parts(2):
                          IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{b2,20:D3}')
                            Expression: 
                              IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'b2')
                            Alignment: 
                              IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 20, IsImplicit) (Syntax: '20')
                            FormatString: 
                              ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "D3") (Syntax: ':D3')
                          IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{If(a, b, c)}')
                            Expression: 
                              IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'If(a, b, c)')
                            Alignment: 
                              null
                            FormatString: 
                              null

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
        <Fact>
        Public Sub InterpolatedStringExpression_FormatAndAlignment_Flow_03()
            Dim source = <![CDATA[
Imports System

Friend Class [Class]
    Public Sub M(a As Boolean, b As String, b2 As String, b3 As String, c As String, p As String)'BIND:"Public Sub M(a As Boolean, b As String, b2 As String, b3 As String, c As String, p As String)"
        p = $"{b2,20:D3}{b3,21:D4}{If(a, b, c)}"
    End Sub
End Class]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [1] [2] [3] [4] [5]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (5)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'p')
              Value: 
                IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.String) (Syntax: 'p')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b2')
              Value: 
                IParameterReferenceOperation: b2 (OperationKind.ParameterReference, Type: System.String) (Syntax: 'b2')

            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '20')
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 20) (Syntax: '20')

            IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b3')
              Value: 
                IParameterReferenceOperation: b3 (OperationKind.ParameterReference, Type: System.String) (Syntax: 'b3')

            IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '21')
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 21) (Syntax: '21')

        Jump if False (Regular) to Block[B3]
            IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'a')

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
              Value: 
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.String) (Syntax: 'b')

        Next (Regular) Block[B4]
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value: 
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.String) (Syntax: 'c')

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'p = $"{b2,2 ... (a, b, c)}"')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String, IsImplicit) (Syntax: 'p = $"{b2,2 ... (a, b, c)}"')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'p')
                  Right: 
                    IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$"{b2,20:D3 ... (a, b, c)}"')
                      Parts(3):
                          IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{b2,20:D3}')
                            Expression: 
                              IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'b2')
                            Alignment: 
                              IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 20, IsImplicit) (Syntax: '20')
                            FormatString: 
                              ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "D3") (Syntax: ':D3')
                          IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{b3,21:D4}')
                            Expression: 
                              IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'b3')
                            Alignment: 
                              IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 21, IsImplicit) (Syntax: '21')
                            FormatString: 
                              ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "D4") (Syntax: ':D4')
                          IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{If(a, b, c)}')
                            Expression: 
                              IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'If(a, b, c)')
                            Alignment: 
                              null
                            FormatString: 
                              null

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
        <Fact>
        Public Sub InterpolatedStringExpression_NestedInterpolation_Flow()
            Dim source = <![CDATA[
Imports System

Friend Class [Class]
    Public Sub M(a As String, b As Integer?, c As Integer)'BIND:"Public Sub M(a As String, b As Integer?, c As Integer)"
        a = $"{$"{If(b, c)}"}"
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
              Value: 
                IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.String) (Syntax: 'a')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
                  Value: 
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'b')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'b')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'b')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
                  Value: 
                    IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'b')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'b')
                      Arguments(0)

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value: 
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'c')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a = $"{$"{If(b, c)}"}"')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String, IsImplicit) (Syntax: 'a = $"{$"{If(b, c)}"}"')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'a')
                  Right: 
                    IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$"{$"{If(b, c)}"}"')
                      Parts(1):
                          IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{$"{If(b, c)}"}')
                            Expression: 
                              IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$"{If(b, c)}"')
                                Parts(1):
                                    IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{If(b, c)}')
                                      Expression: 
                                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(b, c)')
                                      Alignment: 
                                        null
                                      FormatString: 
                                        null
                            Alignment: 
                              null
                            FormatString: 
                              null

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
        Public Sub InterpolatedStringExpression_ConditionalCodeInAlignment_Flow()
            Dim source = <![CDATA[
Imports System

Friend Class [Class]
    Public Sub M(a As Boolean, b As Integer, c As Integer, d As String, p As String)'BIND:"Public Sub M(a As Boolean, b As Integer, c As Integer, d As String, p As String)"
        p = $"{d,If(a, b, c):D3}"
    End Sub
End Class]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'p = $"{d,If ...  b, c):D3}"')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String, IsInvalid, IsImplicit) (Syntax: 'p = $"{d,If ...  b, c):D3}"')
              Left: 
                IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.String) (Syntax: 'p')
              Right: 
                IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String, IsInvalid) (Syntax: '$"{d,If(a, b, c):D3}"')
                  Parts(2):
                      IInterpolationOperation (OperationKind.Interpolation, Type: null, IsInvalid) (Syntax: '{d,')
                        Expression: 
                          IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: System.String) (Syntax: 'd')
                        Alignment: 
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsInvalid) (Syntax: '')
                        FormatString: 
                          null
                      IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null, IsInvalid) (Syntax: 'If(a, b, c):D3')
                        Text: 
                          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "If(a, b, c):D3", IsInvalid, IsImplicit) (Syntax: 'If(a, b, c):D3')

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30204: Integer constant expected.
        p = $"{d,If(a, b, c):D3}"
                 ~
BC30370: '}' expected.
        p = $"{d,If(a, b, c):D3}"
                 ~
BC30035: Syntax error.
        p = $"{d,If(a, b, c):D3}"
                               ~
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact>
        Public Sub InterpolatedStringExpression_ConditionalCodeInAlignment_Flow_02()
            Dim source = <![CDATA[
Imports System

Friend Class [Class]
    Public Sub M(a As Boolean, b As String, c As String, c2 As String, p As String)'BIND:"Public Sub M(a As Boolean, b As String, c As String, c2 As String, p As String)"
        p = $"{c2,If(a, b, c):D3}"
    End Sub
End Class]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'p = $"{c2,I ...  b, c):D3}"')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String, IsInvalid, IsImplicit) (Syntax: 'p = $"{c2,I ...  b, c):D3}"')
              Left: 
                IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.String) (Syntax: 'p')
              Right: 
                IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String, IsInvalid) (Syntax: '$"{c2,If(a, b, c):D3}"')
                  Parts(2):
                      IInterpolationOperation (OperationKind.Interpolation, Type: null, IsInvalid) (Syntax: '{c2,')
                        Expression: 
                          IParameterReferenceOperation: c2 (OperationKind.ParameterReference, Type: System.String) (Syntax: 'c2')
                        Alignment: 
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsInvalid) (Syntax: '')
                        FormatString: 
                          null
                      IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null, IsInvalid) (Syntax: 'If(a, b, c):D3')
                        Text: 
                          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "If(a, b, c):D3", IsInvalid, IsImplicit) (Syntax: 'If(a, b, c):D3')

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30204: Integer constant expected.
        p = $"{c2,If(a, b, c):D3}"
                  ~
BC30370: '}' expected.
        p = $"{c2,If(a, b, c):D3}"
                  ~
BC30035: Syntax error.
        p = $"{c2,If(a, b, c):D3}"
                                ~
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub
    End Class
End Namespace
