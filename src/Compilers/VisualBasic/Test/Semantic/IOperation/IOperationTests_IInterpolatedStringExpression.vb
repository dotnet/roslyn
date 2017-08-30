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
IInterpolatedStringExpression (OperationKind.InterpolatedStringExpression, Type: System.String) (Syntax: '$""')
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
IInterpolatedStringExpression (OperationKind.InterpolatedStringExpression, Type: System.String) (Syntax: '$"Only text part"')
  Parts(1):
      IInterpolatedStringText (OperationKind.InterpolatedStringText) (Syntax: 'Only text part')
        Text: ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "Only text part") (Syntax: 'Only text part')]]>.Value

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
IInterpolatedStringExpression (OperationKind.InterpolatedStringExpression, Type: System.String) (Syntax: '$"{1}"')
  Parts(1):
      IInterpolation (OperationKind.Interpolation) (Syntax: '{1}')
        Expression: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        Alignment: null
        FormatString: null]]>.Value

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
IInterpolatedStringExpression (OperationKind.InterpolatedStringExpression, Type: System.String, IsInvalid) (Syntax: '$"{}"')
  Parts(1):
      IInterpolation (OperationKind.Interpolation, IsInvalid) (Syntax: '{}')
        Expression: IInvalidExpression (OperationKind.InvalidExpression, Type: null, IsInvalid) (Syntax: '')
            Children(0)
        Alignment: null
        FormatString: null
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
IInterpolatedStringExpression (OperationKind.InterpolatedStringExpression, Type: System.String) (Syntax: '$"String {x ... nstant {1}"')
  Parts(4):
      IInterpolatedStringText (OperationKind.InterpolatedStringText) (Syntax: 'String ')
        Text: ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "String ") (Syntax: 'String ')
      IInterpolation (OperationKind.Interpolation) (Syntax: '{x}')
        Expression: IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'x')
        Alignment: null
        FormatString: null
      IInterpolatedStringText (OperationKind.InterpolatedStringText) (Syntax: ' and constant ')
        Text: ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: " and constant ") (Syntax: ' and constant ')
      IInterpolation (OperationKind.Interpolation) (Syntax: '{1}')
        Expression: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        Alignment: null
        FormatString: null]]>.Value

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
IInterpolatedStringExpression (OperationKind.InterpolatedStringExpression, Type: System.String) (Syntax: '$"String {x ... nstant {1}"')
  Parts(6):
      IInterpolatedStringText (OperationKind.InterpolatedStringText) (Syntax: 'String ')
        Text: ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "String ") (Syntax: 'String ')
      IInterpolation (OperationKind.Interpolation) (Syntax: '{x,20}')
        Expression: IFieldReferenceExpression: [Class].x As System.String (OperationKind.FieldReferenceExpression, Type: System.String) (Syntax: 'x')
            Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: [Class]) (Syntax: 'x')
        Alignment: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 20) (Syntax: '20')
        FormatString: null
      IInterpolatedStringText (OperationKind.InterpolatedStringText) (Syntax: ' and ')
        Text: ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: " and ") (Syntax: ' and ')
      IInterpolation (OperationKind.Interpolation) (Syntax: '{y:D3}')
        Expression: IFieldReferenceExpression: [Class].y As System.Int32 (OperationKind.FieldReferenceExpression, Type: System.Int32) (Syntax: 'y')
            Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: [Class]) (Syntax: 'y')
        Alignment: null
        FormatString: ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "D3") (Syntax: ':D3')
      IInterpolatedStringText (OperationKind.InterpolatedStringText) (Syntax: ' and constant ')
        Text: ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: " and constant ") (Syntax: ' and constant ')
      IInterpolation (OperationKind.Interpolation) (Syntax: '{1}')
        Expression: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        Alignment: null
        FormatString: null]]>.Value

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
IInterpolatedStringExpression (OperationKind.InterpolatedStringExpression, Type: System.String) (Syntax: '$"String {x,20:D3}"')
  Parts(2):
      IInterpolatedStringText (OperationKind.InterpolatedStringText) (Syntax: 'String ')
        Text: ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "String ") (Syntax: 'String ')
      IInterpolation (OperationKind.Interpolation) (Syntax: '{x,20:D3}')
        Expression: IFieldReferenceExpression: [Class].x As System.String (OperationKind.FieldReferenceExpression, Type: System.String) (Syntax: 'x')
            Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: [Class]) (Syntax: 'x')
        Alignment: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 20) (Syntax: '20')
        FormatString: ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "D3") (Syntax: ':D3')]]>.Value

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
IInterpolatedStringExpression (OperationKind.InterpolatedStringExpression, Type: System.String) (Syntax: '$"String {x ... nstant {1}"')
  Parts(6):
      IInterpolatedStringText (OperationKind.InterpolatedStringText) (Syntax: 'String ')
        Text: ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "String ") (Syntax: 'String ')
      IInterpolation (OperationKind.Interpolation) (Syntax: '{x}')
        Expression: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.String) (Syntax: 'x')
        Alignment: null
        FormatString: null
      IInterpolatedStringText (OperationKind.InterpolatedStringText) (Syntax: ' and ')
        Text: ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: " and ") (Syntax: ' and ')
      IInterpolation (OperationKind.Interpolation) (Syntax: '{M2(y)}')
        Expression: IInvocationExpression ( Function [Class].M2(z As System.Int32) As System.String) (OperationKind.InvocationExpression, Type: System.String) (Syntax: 'M2(y)')
            Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: [Class]) (Syntax: 'M2')
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: z) (OperationKind.Argument) (Syntax: 'y')
                  ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
                  InConversion: null
                  OutConversion: null
        Alignment: null
        FormatString: null
      IInterpolatedStringText (OperationKind.InterpolatedStringText) (Syntax: ' and constant ')
        Text: ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: " and constant ") (Syntax: ' and constant ')
      IInterpolation (OperationKind.Interpolation) (Syntax: '{1}')
        Expression: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        Alignment: null
        FormatString: null]]>.Value

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
IInterpolatedStringExpression (OperationKind.InterpolatedStringExpression, Type: System.String) (Syntax: '$"String {M2($"{y}")}"')
  Parts(2):
      IInterpolatedStringText (OperationKind.InterpolatedStringText) (Syntax: 'String ')
        Text: ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "String ") (Syntax: 'String ')
      IInterpolation (OperationKind.Interpolation) (Syntax: '{M2($"{y}")}')
        Expression: IInvocationExpression ( Function [Class].M2(z As System.String) As System.Int32) (OperationKind.InvocationExpression, Type: System.Int32) (Syntax: 'M2($"{y}")')
            Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: [Class]) (Syntax: 'M2')
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: z) (OperationKind.Argument) (Syntax: '$"{y}"')
                  IInterpolatedStringExpression (OperationKind.InterpolatedStringExpression, Type: System.String) (Syntax: '$"{y}"')
                    Parts(1):
                        IInterpolation (OperationKind.Interpolation) (Syntax: '{y}')
                          Expression: ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
                          Alignment: null
                          FormatString: null
                  InConversion: null
                  OutConversion: null
        Alignment: null
        FormatString: null]]>.Value

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
IInterpolatedStringExpression (OperationKind.InterpolatedStringExpression, Type: System.String, IsInvalid) (Syntax: '$"String {x ...  {[Class]}"')
  Parts(4):
      IInterpolatedStringText (OperationKind.InterpolatedStringText) (Syntax: 'String ')
        Text: ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "String ") (Syntax: 'String ')
      IInterpolation (OperationKind.Interpolation, IsInvalid) (Syntax: '{x1}')
        Expression: IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: 'x1')
            Children(0)
        Alignment: null
        FormatString: null
      IInterpolatedStringText (OperationKind.InterpolatedStringText) (Syntax: ' and constant ')
        Text: ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: " and constant ") (Syntax: ' and constant ')
      IInterpolation (OperationKind.Interpolation, IsInvalid) (Syntax: '{[Class]}')
        Expression: IOperation:  (OperationKind.None, IsInvalid) (Syntax: '[Class]')
        Alignment: null
        FormatString: null]]>.Value

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
    End Class
End Namespace
