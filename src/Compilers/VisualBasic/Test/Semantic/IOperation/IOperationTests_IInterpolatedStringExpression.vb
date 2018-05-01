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
    Public Sub M()'BIND:"Public Sub M()"
        Console.WriteLine($"")
    End Sub
End Class]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.WriteLine($"")')
          Expression: 
            IInvocationOperation (Sub System.Console.WriteLine(value As System.String)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.WriteLine($"")')
              Instance Receiver: 
                null
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: '$""')
                    IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$""')
                      Parts(0)
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

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
    Public Sub M()'BIND:"Public Sub M()"
        Console.WriteLine($"Only text part")
    End Sub
End Class]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.Wri ... text part")')
          Expression: 
            IInvocationOperation (Sub System.Console.WriteLine(value As System.String)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.Wri ... text part")')
              Instance Receiver: 
                null
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: '$"Only text part"')
                    IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$"Only text part"')
                      Parts(1):
                          IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: 'Only text part')
                            Text: 
                              ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "Only text part", IsImplicit) (Syntax: 'Only text part')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

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
        Console.WriteLine($"{If(a, b, c)}")
    End Sub
End Class]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (0)
    Jump if False (Regular) to Block[B3]
        IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'a')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.String) (Syntax: 'b')

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
          Value: 
            IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.String) (Syntax: 'c')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.Wri ... a, b, c)}")')
          Expression: 
            IInvocationOperation (Sub System.Console.WriteLine(value As System.String)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.Wri ... a, b, c)}")')
              Instance Receiver: 
                null
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: '$"{If(a, b, c)}"')
                    IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$"{If(a, b, c)}"')
                      Parts(1):
                          IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{If(a, b, c)}')
                            Expression: 
                              IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'If(a, b, c)')
                            Alignment: 
                              null
                            FormatString: 
                              null
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

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
        Console.WriteLine($"String1 {b = If(a, b, c)} and String2 {If(a, b, c)}")
    End Sub
End Class]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (2)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'String1 ')
          Value: 
            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "String1 ", IsImplicit) (Syntax: 'String1 ')

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
    Statements (2)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b = If(a, b, c)')
          Value: 
            IBinaryOperation (BinaryOperatorKind.Equals, Checked) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'b = If(a, b, c)')
              Left: 
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'b')
              Right: 
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'If(a, b, c)')

        IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: ' and String2 ')
          Value: 
            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: " and String2 ", IsImplicit) (Syntax: ' and String2 ')

    Jump if False (Regular) to Block[B6]
        IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'a')

    Next (Regular) Block[B5]
Block[B5] - Block
    Predecessors: [B4]
    Statements (1)
        IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.String) (Syntax: 'b')

    Next (Regular) Block[B7]
Block[B6] - Block
    Predecessors: [B4]
    Statements (1)
        IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
          Value: 
            IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.String) (Syntax: 'c')

    Next (Regular) Block[B7]
Block[B7] - Block
    Predecessors: [B5] [B6]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.Wri ... a, b, c)}")')
          Expression: 
            IInvocationOperation (Sub System.Console.WriteLine(value As System.String)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.Wri ... a, b, c)}")')
              Instance Receiver: 
                null
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: '$"String1 { ... (a, b, c)}"')
                    IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$"String1 { ... (a, b, c)}"')
                      Parts(4):
                          IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: 'String1 ')
                            Text: 
                              IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.String, Constant: "String1 ", IsImplicit) (Syntax: 'String1 ')
                          IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{b = If(a, b, c)}')
                            Expression: 
                              IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Boolean, IsImplicit) (Syntax: 'b = If(a, b, c)')
                            Alignment: 
                              null
                            FormatString: 
                              null
                          IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: ' and String2 ')
                            Text: 
                              IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.String, Constant: " and String2 ", IsImplicit) (Syntax: ' and String2 ')
                          IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{If(a, b, c)}')
                            Expression: 
                              IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'If(a, b, c)')
                            Alignment: 
                              null
                            FormatString: 
                              null
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

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
    Public Sub M(a As Boolean, b As String, c As String, d As Integer?, e As Integer)'BIND:"Public Sub M(a As Boolean, b As String, c As String, d As Integer?, e As Integer)"
        Console.WriteLine($"String {If(a, b, c),20} and {If(d, e):D3} and constant {1}")
    End Sub
End Class]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'String ')
          Value: 
            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "String ", IsImplicit) (Syntax: 'String ')

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
    Statements (3)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '20')
          Value: 
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 20) (Syntax: '20')

        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: ' and ')
          Value: 
            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: " and ", IsImplicit) (Syntax: ' and ')

        IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd')
          Value: 
            IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'd')

    Jump if True (Regular) to Block[B6]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'd')
          Operand: 
            IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'd')

    Next (Regular) Block[B5]
Block[B5] - Block
    Predecessors: [B4]
    Statements (1)
        IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd')
          Value: 
            IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'd')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'd')
              Arguments(0)

    Next (Regular) Block[B7]
Block[B6] - Block
    Predecessors: [B4]
    Statements (1)
        IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'e')
          Value: 
            IParameterReferenceOperation: e (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'e')

    Next (Regular) Block[B7]
Block[B7] - Block
    Predecessors: [B5] [B6]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.Wri ... stant {1}")')
          Expression: 
            IInvocationOperation (Sub System.Console.WriteLine(value As System.String)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.Wri ... stant {1}")')
              Instance Receiver: 
                null
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: '$"String {I ... nstant {1}"')
                    IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$"String {I ... nstant {1}"')
                      Parts(6):
                          IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: 'String ')
                            Text: 
                              IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.String, Constant: "String ", IsImplicit) (Syntax: 'String ')
                          IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{If(a, b, c),20}')
                            Expression: 
                              IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'If(a, b, c)')
                            Alignment: 
                              IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 20, IsImplicit) (Syntax: '20')
                            FormatString: 
                              null
                          IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: ' and ')
                            Text: 
                              IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.String, Constant: " and ", IsImplicit) (Syntax: ' and ')
                          IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{If(d, e):D3}')
                            Expression: 
                              IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(d, e)')
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
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

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
        Public Sub InterpolatedStringExpression_InterpolationAndFormatAndAlignment_Flow()
            Dim source = <![CDATA[
Imports System

Friend Class [Class]
    Public Sub M(a As Boolean, b As String, c As String, d As Integer?, e As Integer)'BIND:"Public Sub M(a As Boolean, b As String, c As String, d As Integer?, e As Integer)"
        Console.WriteLine($"String {If(a, b, c),20:D3} and {If(d, e),21:D4}")
    End Sub
End Class]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'String ')
          Value: 
            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "String ", IsImplicit) (Syntax: 'String ')

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
    Statements (4)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '20')
          Value: 
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 20) (Syntax: '20')

        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: ':D3')
          Value: 
            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "D3") (Syntax: ':D3')

        IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: ' and ')
          Value: 
            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: " and ", IsImplicit) (Syntax: ' and ')

        IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd')
          Value: 
            IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'd')

    Jump if True (Regular) to Block[B6]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'd')
          Operand: 
            IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'd')

    Next (Regular) Block[B5]
Block[B5] - Block
    Predecessors: [B4]
    Statements (1)
        IFlowCaptureOperation: 6 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd')
          Value: 
            IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'd')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'd')
              Arguments(0)

    Next (Regular) Block[B7]
Block[B6] - Block
    Predecessors: [B4]
    Statements (1)
        IFlowCaptureOperation: 6 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'e')
          Value: 
            IParameterReferenceOperation: e (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'e')

    Next (Regular) Block[B7]
Block[B7] - Block
    Predecessors: [B5] [B6]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.Wri ... e),21:D4}")')
          Expression: 
            IInvocationOperation (Sub System.Console.WriteLine(value As System.String)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.Wri ... e),21:D4}")')
              Instance Receiver: 
                null
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: '$"String {I ...  e),21:D4}"')
                    IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$"String {I ...  e),21:D4}"')
                      Parts(4):
                          IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: 'String ')
                            Text: 
                              IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.String, Constant: "String ", IsImplicit) (Syntax: 'String ')
                          IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{If(a, b, c),20:D3}')
                            Expression: 
                              IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'If(a, b, c)')
                            Alignment: 
                              IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 20, IsImplicit) (Syntax: '20')
                            FormatString: 
                              IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.String, Constant: "D3", IsImplicit) (Syntax: ':D3')
                          IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: ' and ')
                            Text: 
                              IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.String, Constant: " and ", IsImplicit) (Syntax: ' and ')
                          IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{If(d, e),21:D4}')
                            Expression: 
                              IFlowCaptureReferenceOperation: 6 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(d, e)')
                            Alignment: 
                              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 21) (Syntax: '21')
                            FormatString: 
                              ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "D4") (Syntax: ':D4')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

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
        Public Sub InterpolatedStringExpression_InvocationInInterpolation_Flow()
            Dim source = <![CDATA[
Imports System

Friend Class [Class]
    Public Sub M(y As Integer)'BIND:"Public Sub M(y As Integer)"
        Console.WriteLine($"String {M2(y)} and {y}")
    End Sub

    Private Function M2(ByRef z As Integer) As String
        Return z.ToString()
    End Function
End Class]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.Wri ... } and {y}")')
          Expression: 
            IInvocationOperation (Sub System.Console.WriteLine(value As System.String)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.Wri ... } and {y}")')
              Instance Receiver: 
                null
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: '$"String {M ... )} and {y}"')
                    IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$"String {M ... )} and {y}"')
                      Parts(4):
                          IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: 'String ')
                            Text: 
                              ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "String ", IsImplicit) (Syntax: 'String ')
                          IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{M2(y)}')
                            Expression: 
                              IInvocationOperation ( Function [Class].M2(ByRef z As System.Int32) As System.String) (OperationKind.Invocation, Type: System.String) (Syntax: 'M2(y)')
                                Instance Receiver: 
                                  IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: [Class], IsImplicit) (Syntax: 'M2')
                                Arguments(1):
                                    IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: z) (OperationKind.Argument, Type: null) (Syntax: 'y')
                                      IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'y')
                                      InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                      OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            Alignment: 
                              null
                            FormatString: 
                              null
                          IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: ' and ')
                            Text: 
                              ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: " and ", IsImplicit) (Syntax: ' and ')
                          IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{y}')
                            Expression: 
                              IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'y')
                            Alignment: 
                              null
                            FormatString: 
                              null
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

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
        Public Sub InterpolatedStringExpression_NestedInterpolation_Flow()
            Dim source = <![CDATA[
Imports System

Friend Class [Class]
    Public Sub M(a As Boolean, b As Integer, c As Integer?, d As Integer?, e As Integer?)'BIND:"Public Sub M(a As Boolean, b As Integer, c As Integer?, d As Integer?, e As Integer?)"
        Console.WriteLine($"String {e = If (a, b, c)} and {M2($"{If(d, e)}")}")
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
    Statements (2)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'String ')
          Value: 
            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "String ", IsImplicit) (Syntax: 'String ')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'e')
          Value: 
            IParameterReferenceOperation: e (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'e')

    Jump if False (Regular) to Block[B3]
        IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'a')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'b')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                (WideningNullable)
              Operand: 
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
          Value: 
            IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'c')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (4)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'e = If (a, b, c)')
          Value: 
            IBinaryOperation (BinaryOperatorKind.Equals, IsLifted, Checked) (OperationKind.BinaryOperator, Type: System.Nullable(Of System.Boolean)) (Syntax: 'e = If (a, b, c)')
              Left: 
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'e')
              Right: 
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'If (a, b, c)')

        IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: ' and ')
          Value: 
            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: " and ", IsImplicit) (Syntax: ' and ')

        IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'M2')
          Value: 
            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: [Class], IsImplicit) (Syntax: 'M2')

        IFlowCaptureOperation: 6 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd')
          Value: 
            IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'd')

    Jump if True (Regular) to Block[B6]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'd')
          Operand: 
            IFlowCaptureReferenceOperation: 6 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'd')

    Next (Regular) Block[B5]
Block[B5] - Block
    Predecessors: [B4]
    Statements (1)
        IFlowCaptureOperation: 7 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd')
          Value: 
            IFlowCaptureReferenceOperation: 6 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'd')

    Next (Regular) Block[B7]
Block[B6] - Block
    Predecessors: [B4]
    Statements (1)
        IFlowCaptureOperation: 7 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'e')
          Value: 
            IParameterReferenceOperation: e (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'e')

    Next (Regular) Block[B7]
Block[B7] - Block
    Predecessors: [B5] [B6]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.Wri ... d, e)}")}")')
          Expression: 
            IInvocationOperation (Sub System.Console.WriteLine(value As System.String)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.Wri ... d, e)}")}")')
              Instance Receiver: 
                null
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: '$"String {e ... (d, e)}")}"')
                    IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$"String {e ... (d, e)}")}"')
                      Parts(4):
                          IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: 'String ')
                            Text: 
                              IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.String, Constant: "String ", IsImplicit) (Syntax: 'String ')
                          IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{e = If (a, b, c)}')
                            Expression: 
                              IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: 'e = If (a, b, c)')
                            Alignment: 
                              null
                            FormatString: 
                              null
                          IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: ' and ')
                            Text: 
                              IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.String, Constant: " and ", IsImplicit) (Syntax: ' and ')
                          IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{M2($"{If(d, e)}")}')
                            Expression: 
                              IInvocationOperation ( Function [Class].M2(z As System.String) As System.Int32) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'M2($"{If(d, e)}")')
                                Instance Receiver: 
                                  IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: [Class], IsImplicit) (Syntax: 'M2')
                                Arguments(1):
                                    IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: z) (OperationKind.Argument, Type: null) (Syntax: '$"{If(d, e)}"')
                                      IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$"{If(d, e)}"')
                                        Parts(1):
                                            IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{If(d, e)}')
                                              Expression: 
                                                IFlowCaptureReferenceOperation: 7 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'If(d, e)')
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
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

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
        Public Sub InterpolatedStringExpression_InvalidExpressionInInterpolation_Flow()
            Dim source = <![CDATA[
Imports System

Friend Class [Class]
    Public Sub M(x As Integer)'BIND:"Public Sub M(x As Integer)"
        Console.WriteLine($"String {x1} and constant {[Class]}")
    End Sub
End Class]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'Console.Wri ... {[Class]}")')
          Expression: 
            IInvalidOperation (OperationKind.Invalid, Type: System.Void, IsInvalid) (Syntax: 'Console.Wri ... {[Class]}")')
              Children(2):
                  IOperation:  (OperationKind.None, Type: null) (Syntax: 'Console.WriteLine')
                    Children(1):
                        IOperation:  (OperationKind.None, Type: null) (Syntax: 'Console')
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

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30451: 'x1' is not declared. It may be inaccessible due to its protection level.
        Console.WriteLine($"String {x1} and constant {[Class]}")
                                    ~~
BC30109: '[Class]' is a class type and cannot be used as an expression.
        Console.WriteLine($"String {x1} and constant {[Class]}")
                                                      ~~~~~~~
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact>
        Public Sub InterpolatedStringExpression_NonConstantExpressionInFormatAndAlignment_Flow()
            Dim source = <![CDATA[
Imports System

Friend Class [Class]
    Public Sub M(a As Boolean, b As String, c As String, d As Integer?, e As Integer)'BIND:"Public Sub M(a As Boolean, b As String, c As String, d As Integer?, e As Integer)"
        Console.WriteLine($"String {If(a, b, c),e:c} and {If(d, e),e:c}")
    End Sub
End Class]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'String ')
          Value: 
            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "String ", IsImplicit) (Syntax: 'String ')

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
    Statements (4)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: '')
          Value: 
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsInvalid) (Syntax: '')

        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'e:c')
          Value: 
            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "e:c", IsInvalid, IsImplicit) (Syntax: 'e:c')

        IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: ' and ')
          Value: 
            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: " and ", IsInvalid, IsImplicit) (Syntax: ' and ')

        IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd')
          Value: 
            IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'd')

    Jump if True (Regular) to Block[B6]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'd')
          Operand: 
            IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'd')

    Next (Regular) Block[B5]
Block[B5] - Block
    Predecessors: [B4]
    Statements (1)
        IFlowCaptureOperation: 6 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd')
          Value: 
            IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'd')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'd')
              Arguments(0)

    Next (Regular) Block[B7]
Block[B6] - Block
    Predecessors: [B4]
    Statements (1)
        IFlowCaptureOperation: 6 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'e')
          Value: 
            IParameterReferenceOperation: e (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'e')

    Next (Regular) Block[B7]
Block[B7] - Block
    Predecessors: [B5] [B6]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'Console.Wri ... , e),e:c}")')
          Expression: 
            IInvocationOperation (Sub System.Console.WriteLine(value As System.String)) (OperationKind.Invocation, Type: System.Void, IsInvalid) (Syntax: 'Console.Wri ... , e),e:c}")')
              Instance Receiver: 
                null
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null, IsInvalid) (Syntax: '$"String {I ... d, e),e:c}"')
                    IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String, IsInvalid) (Syntax: '$"String {I ... d, e),e:c}"')
                      Parts(6):
                          IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: 'String ')
                            Text: 
                              IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.String, Constant: "String ", IsImplicit) (Syntax: 'String ')
                          IInterpolationOperation (OperationKind.Interpolation, Type: null, IsInvalid) (Syntax: '{If(a, b, c),')
                            Expression: 
                              IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'If(a, b, c)')
                            Alignment: 
                              IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 0, IsInvalid, IsImplicit) (Syntax: '')
                            FormatString: 
                              null
                          IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null, IsInvalid) (Syntax: 'e:c')
                            Text: 
                              IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.String, Constant: "e:c", IsInvalid, IsImplicit) (Syntax: 'e:c')
                          IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null, IsInvalid) (Syntax: ' and ')
                            Text: 
                              IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.String, Constant: " and ", IsInvalid, IsImplicit) (Syntax: ' and ')
                          IInterpolationOperation (OperationKind.Interpolation, Type: null, IsInvalid) (Syntax: '{If(d, e),')
                            Expression: 
                              IFlowCaptureReferenceOperation: 6 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(d, e)')
                            Alignment: 
                              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsInvalid) (Syntax: '')
                            FormatString: 
                              null
                          IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null, IsInvalid) (Syntax: 'e:c')
                            Text: 
                              ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "e:c", IsInvalid, IsImplicit) (Syntax: 'e:c')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

    Next (Regular) Block[B8]
Block[B8] - Exit
    Predecessors: [B7]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30204: Integer constant expected.
        Console.WriteLine($"String {If(a, b, c),e:c} and {If(d, e),e:c}")
                                                ~
BC30370: '}' expected.
        Console.WriteLine($"String {If(a, b, c),e:c} and {If(d, e),e:c}")
                                                ~
BC30035: Syntax error.
        Console.WriteLine($"String {If(a, b, c),e:c} and {If(d, e),e:c}")
                                                   ~
BC30204: Integer constant expected.
        Console.WriteLine($"String {If(a, b, c),e:c} and {If(d, e),e:c}")
                                                                   ~
BC30370: '}' expected.
        Console.WriteLine($"String {If(a, b, c),e:c} and {If(d, e),e:c}")
                                                                   ~
BC30035: Syntax error.
        Console.WriteLine($"String {If(a, b, c),e:c} and {If(d, e),e:c}")
                                                                      ~
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub
    End Class
End Namespace
