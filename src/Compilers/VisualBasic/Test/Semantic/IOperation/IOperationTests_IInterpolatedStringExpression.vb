' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
          IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: '[Class]')
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
    Public Sub M(a As Boolean, b As String, c As String)'BIND:"Public Sub M(a As Boolean, b As String, c As String)"
        b = $"{If(a, b, c)}"
    End Sub
End Class]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.String) (Syntax: 'b')

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
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = $"{If(a, b, c)}"')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String, IsImplicit) (Syntax: 'b = $"{If(a, b, c)}"')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'b')
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
    Public Sub M(a As Boolean, b As String, c As String)'BIND:"Public Sub M(a As Boolean, b As String, c As String)"
        b = $"String1 {b = If(a, b, c)} and String2 {If(a, b, c)}"
    End Sub
End Class]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (2)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.String) (Syntax: 'b')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.String) (Syntax: 'b')

    Jump if False (Regular) to Block[B3]
        IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'a')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.String) (Syntax: 'b')

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
          Value: 
            IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.String) (Syntax: 'c')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b = If(a, b, c)')
          Value: 
            IBinaryOperation (BinaryOperatorKind.Equals, Checked) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'b = If(a, b, c)')
              Left: 
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'b')
              Right: 
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'If(a, b, c)')

    Jump if False (Regular) to Block[B6]
        IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'a')

    Next (Regular) Block[B5]
Block[B5] - Block
    Predecessors: [B4]
    Statements (1)
        IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.String) (Syntax: 'b')

    Next (Regular) Block[B7]
Block[B6] - Block
    Predecessors: [B4]
    Statements (1)
        IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
          Value: 
            IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.String) (Syntax: 'c')

    Next (Regular) Block[B7]
Block[B7] - Block
    Predecessors: [B5] [B6]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = $"Strin ... (a, b, c)}"')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String, IsImplicit) (Syntax: 'b = $"Strin ... (a, b, c)}"')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'b')
              Right: 
                IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$"String1 { ... (a, b, c)}"')
                  Parts(4):
                      IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: 'String1 ')
                        Text: 
                          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "String1 ", IsImplicit) (Syntax: 'String1 ')
                      IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{b = If(a, b, c)}')
                        Expression: 
                          IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Boolean, IsImplicit) (Syntax: 'b = If(a, b, c)')
                        Alignment: 
                          null
                        FormatString: 
                          null
                      IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: ' and String2 ')
                        Text: 
                          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: " and String2 ", IsImplicit) (Syntax: ' and String2 ')
                      IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{If(a, b, c)}')
                        Expression: 
                          IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'If(a, b, c)')
                        Alignment: 
                          null
                        FormatString: 
                          null

    Next (Regular) Block[B8]
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
    Public Sub M(a As Boolean, b As String, c As String)'BIND:"Public Sub M(a As Boolean, b As String, c As String)"
        b = $"{If(a, b, c),20:D3}"
    End Sub
End Class]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.String) (Syntax: 'b')

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
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = $"{If(a ...  c),20:D3}"')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String, IsImplicit) (Syntax: 'b = $"{If(a ...  c),20:D3}"')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'b')
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
        a = $"{M2($"{If(b, c)}")}"
    End Sub

    Private Function M2(z As String) As Integer
        Return Int32.Parse(z)
    End Function
End Class]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (3)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.String) (Syntax: 'a')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'M2')
          Value: 
            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: [Class], IsImplicit) (Syntax: 'M2')

        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'b')

    Jump if True (Regular) to Block[B3]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'b')
          Operand: 
            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'b')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'b')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'b')
              Arguments(0)

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
          Value: 
            IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'c')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a = $"{M2($ ... (b, c)}")}"')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String, IsImplicit) (Syntax: 'a = $"{M2($ ... (b, c)}")}"')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'a')
              Right: 
                IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$"{M2($"{If(b, c)}")}"')
                  Parts(1):
                      IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{M2($"{If(b, c)}")}')
                        Expression: 
                          IInvocationOperation ( Function [Class].M2(z As System.String) As System.Int32) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'M2($"{If(b, c)}")')
                            Instance Receiver: 
                              IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: [Class], IsImplicit) (Syntax: 'M2')
                            Arguments(1):
                                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: z) (OperationKind.Argument, Type: null) (Syntax: '$"{If(b, c)}"')
                                  IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$"{If(b, c)}"')
                                    Parts(1):
                                        IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{If(b, c)}')
                                          Expression: 
                                            IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(b, c)')
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

    Next (Regular) Block[B5]
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact>
        Public Sub InterpolatedStringExpression_InvalidExpressionInInterpolation_Flow()
            Dim source = <![CDATA[
Imports System

Friend Class [Class]
    Public Sub M(x As String)'BIND:"Public Sub M(x As String)"
        x = $"{If(x1, x)}"
    End Sub
End Class]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (2)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x')
          Value: 
            IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.String) (Syntax: 'x')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'x1')
          Value: 
            IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'x1')
              Children(0)

    Jump if True (Regular) to Block[B3]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'x1')
          Operand: 
            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: ?, IsInvalid, IsImplicit) (Syntax: 'x1')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'x1')
          Value: 
            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: ?, IsInvalid, IsImplicit) (Syntax: 'x1')

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x')
          Value: 
            IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.String) (Syntax: 'x')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'x = $"{If(x1, x)}"')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String, IsInvalid, IsImplicit) (Syntax: 'x = $"{If(x1, x)}"')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'x')
              Right: 
                IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String, IsInvalid) (Syntax: '$"{If(x1, x)}"')
                  Parts(1):
                      IInterpolationOperation (OperationKind.Interpolation, Type: null, IsInvalid) (Syntax: '{If(x1, x)}')
                        Expression: 
                          IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.String, IsInvalid, IsImplicit) (Syntax: 'If(x1, x)')
                        Alignment: 
                          null
                        FormatString: 
                          null

    Next (Regular) Block[B5]
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30451: 'x1' is not declared. It may be inaccessible due to its protection level.
        x = $"{If(x1, x)}"
                  ~~
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact>
        Public Sub InterpolatedStringExpression_NonConstantExpressionInFormat_Flow()
            Dim source = <![CDATA[
Imports System

Friend Class [Class]
    Public Sub M(a As Boolean, b As String, c As String, d As Integer)'BIND:"Public Sub M(a As Boolean, b As String, c As String, d As Integer)"
        b = $"{If(a, b, c),d:D3}"
    End Sub
End Class]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.String) (Syntax: 'b')

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
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'b = $"{If(a ... , c),d:D3}"')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String, IsInvalid, IsImplicit) (Syntax: 'b = $"{If(a ... , c),d:D3}"')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'b')
              Right: 
                IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String, IsInvalid) (Syntax: '$"{If(a, b, c),d:D3}"')
                  Parts(2):
                      IInterpolationOperation (OperationKind.Interpolation, Type: null, IsInvalid) (Syntax: '{If(a, b, c),')
                        Expression: 
                          IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'If(a, b, c)')
                        Alignment: 
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsInvalid) (Syntax: '')
                        FormatString: 
                          null
                      IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null, IsInvalid) (Syntax: 'd:D3')
                        Text: 
                          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "d:D3", IsInvalid, IsImplicit) (Syntax: 'd:D3')

    Next (Regular) Block[B5]
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30204: Integer constant expected.
        b = $"{If(a, b, c),d:D3}"
                           ~
BC30370: '}' expected.
        b = $"{If(a, b, c),d:D3}"
                           ~
BC30035: Syntax error.
        b = $"{If(a, b, c),d:D3}"
                               ~
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub
    End Class
End Namespace
