' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_ImplicitLambdaConversion()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module M1
    Sub Method1()
        Dim a As Action = Sub() Console.WriteLine("")'BIND:"Sub() Console.WriteLine("")"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Action) (Syntax: 'Sub() Conso ... iteLine("")')
  Target: IAnonymousFunctionExpression (Symbol: Sub ()) (OperationKind.AnonymousFunctionExpression, Type: null) (Syntax: 'Sub() Conso ... iteLine("")')
      IBlockStatement (3 statements) (OperationKind.BlockStatement) (Syntax: 'Sub() Conso ... iteLine("")')
        IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'Sub() Conso ... iteLine("")')
          IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.WriteLine("")')
            Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.String)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.WriteLine("")')
                Instance Receiver: null
                Arguments(1):
                    IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '""')
                      ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "") (Syntax: '""')
                      InConversion: null
                      OutConversion: null
        ILabeledStatement (Label: exit) (OperationKind.LabeledStatement) (Syntax: 'Sub() Conso ... iteLine("")')
          Statement: null
        IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'Sub() Conso ... iteLine("")')
          ReturnedValue: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of SingleLineLambdaExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_ImplicitLambdaConversion_InvalidArgumentType()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module M1
    Sub Method1()
        Dim a As Action = Sub(i As Integer) Console.WriteLine("")'BIND:"Sub(i As Integer) Console.WriteLine("")"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Action, IsInvalid) (Syntax: 'Sub(i As In ... iteLine("")')
  Target: IAnonymousFunctionExpression (Symbol: Sub (i As System.Int32)) (OperationKind.AnonymousFunctionExpression, Type: null, IsInvalid) (Syntax: 'Sub(i As In ... iteLine("")')
      IBlockStatement (3 statements) (OperationKind.BlockStatement, IsInvalid) (Syntax: 'Sub(i As In ... iteLine("")')
        IBlockStatement (1 statements) (OperationKind.BlockStatement, IsInvalid) (Syntax: 'Sub(i As In ... iteLine("")')
          IExpressionStatement (OperationKind.ExpressionStatement, IsInvalid) (Syntax: 'Console.WriteLine("")')
            Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.String)) (OperationKind.InvocationExpression, Type: System.Void, IsInvalid) (Syntax: 'Console.WriteLine("")')
                Instance Receiver: null
                Arguments(1):
                    IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, IsInvalid) (Syntax: '""')
                      ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "", IsInvalid) (Syntax: '""')
                      InConversion: null
                      OutConversion: null
        ILabeledStatement (Label: exit) (OperationKind.LabeledStatement, IsInvalid) (Syntax: 'Sub(i As In ... iteLine("")')
          Statement: null
        IReturnStatement (OperationKind.ReturnStatement, IsInvalid) (Syntax: 'Sub(i As In ... iteLine("")')
          ReturnedValue: null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36670: Nested sub does not have a signature that is compatible with delegate 'Action'.
        Dim a As Action = Sub(i As Integer) Console.WriteLine("")'BIND:"Sub(i As Integer) Console.WriteLine("")"
                          ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of SingleLineLambdaExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact()>
        Public Sub DelegateCreationExpression_ImplicitLambdaExpression_InvalidReturnType()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module M1
    Sub Method1()
        Dim a As Func(Of String) = Function() 1'BIND:"Function() 1"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Func(Of System.String), IsInvalid) (Syntax: 'Function() 1')
  Target: IAnonymousFunctionExpression (Symbol: Function () As System.String) (OperationKind.AnonymousFunctionExpression, Type: null, IsInvalid) (Syntax: 'Function() 1')
      IBlockStatement (3 statements, 1 locals) (OperationKind.BlockStatement, IsInvalid) (Syntax: 'Function() 1')
        Locals: Local_1: <anonymous local> As System.String
        IReturnStatement (OperationKind.ReturnStatement, IsInvalid) (Syntax: '1')
          ReturnedValue: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.String, IsInvalid) (Syntax: '1')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
        ILabeledStatement (Label: exit) (OperationKind.LabeledStatement, IsInvalid) (Syntax: 'Function() 1')
          Statement: null
        IReturnStatement (OperationKind.ReturnStatement, IsInvalid) (Syntax: 'Function() 1')
          ReturnedValue: ILocalReferenceExpression:  (OperationKind.LocalReferenceExpression, Type: System.String, IsInvalid) (Syntax: 'Function() 1')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30512: Option Strict On disallows implicit conversions from 'Integer' to 'String'.
        Dim a As Func(Of String) = Function() 1'BIND:"Function() 1"
                                              ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of SingleLineLambdaExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact()>
        Public Sub DelegateCreationExpression_ImplicitLambdaConversion_ReturnRelaxation()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module M1
    Sub Method1()
        Dim a As Action = Function() 1'BIND:"Function() 1"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Action) (Syntax: 'Function() 1')
  Target: IAnonymousFunctionExpression (Symbol: Function () As System.Int32) (OperationKind.AnonymousFunctionExpression, Type: null) (Syntax: 'Function() 1')
      IBlockStatement (3 statements, 1 locals) (OperationKind.BlockStatement) (Syntax: 'Function() 1')
        Locals: Local_1: <anonymous local> As System.Int32
        IReturnStatement (OperationKind.ReturnStatement) (Syntax: '1')
          ReturnedValue: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        ILabeledStatement (Label: exit) (OperationKind.LabeledStatement) (Syntax: 'Function() 1')
          Statement: null
        IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'Function() 1')
          ReturnedValue: ILocalReferenceExpression:  (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'Function() 1')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of SingleLineLambdaExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact()>
        Public Sub DelegateCreationExpression_ImplicitLambdaExpression_RelaxationOfArgument()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module M1
    Sub Method1()
        Dim a As Action(Of String) = Sub(o As Object) Console.WriteLine(o)'BIND:"Sub(o As Object) Console.WriteLine(o)"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Action(Of System.String)) (Syntax: 'Sub(o As Ob ... riteLine(o)')
  Target: IAnonymousFunctionExpression (Symbol: Sub (o As System.Object)) (OperationKind.AnonymousFunctionExpression, Type: null) (Syntax: 'Sub(o As Ob ... riteLine(o)')
      IBlockStatement (3 statements) (OperationKind.BlockStatement) (Syntax: 'Sub(o As Ob ... riteLine(o)')
        IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'Sub(o As Ob ... riteLine(o)')
          IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.WriteLine(o)')
            Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.Object)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.WriteLine(o)')
                Instance Receiver: null
                Arguments(1):
                    IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'o')
                      IParameterReferenceExpression: o (OperationKind.ParameterReferenceExpression, Type: System.Object) (Syntax: 'o')
                      InConversion: null
                      OutConversion: null
        ILabeledStatement (Label: exit) (OperationKind.LabeledStatement) (Syntax: 'Sub(o As Ob ... riteLine(o)')
          Statement: null
        IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'Sub(o As Ob ... riteLine(o)')
          ReturnedValue: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of SingleLineLambdaExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub


    End Class
End Namespace
