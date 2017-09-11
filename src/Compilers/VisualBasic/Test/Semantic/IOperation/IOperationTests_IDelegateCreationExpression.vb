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

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_ImplicitLambdaConversion_InvalidReturnType()
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

        <CompilerTrait(CompilerFeature.IOperation)>
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

        <CompilerTrait(CompilerFeature.IOperation)>
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

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_ExplicitLambdaConversion()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module M1
    Sub Main()
        Dim a As Action = CType(Sub() Console.WriteLine(), Action)'BIND:"CType(Sub() Console.WriteLine(), Action)"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Action) (Syntax: 'CType(Sub() ... (), Action)')
  Target: IAnonymousFunctionExpression (Symbol: Sub ()) (OperationKind.AnonymousFunctionExpression, Type: null) (Syntax: 'Sub() Conso ... WriteLine()')
      IBlockStatement (3 statements) (OperationKind.BlockStatement) (Syntax: 'Sub() Conso ... WriteLine()')
        IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'Sub() Conso ... WriteLine()')
          IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.WriteLine()')
            Expression: IInvocationExpression (Sub System.Console.WriteLine()) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.WriteLine()')
                Instance Receiver: null
                Arguments(0)
        ILabeledStatement (Label: exit) (OperationKind.LabeledStatement) (Syntax: 'Sub() Conso ... WriteLine()')
          Statement: null
        IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'Sub() Conso ... WriteLine()')
          ReturnedValue: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of CTypeExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_ExplicitLambdaConversion_InvalidArgumentType()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module M1
    Sub Main()
        Dim a As Action = CType(Sub(i As Integer) Console.WriteLine(), Action)'BIND:"CType(Sub(i As Integer) Console.WriteLine(), Action)"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Action, IsInvalid) (Syntax: 'CType(Sub(i ... (), Action)')
  Target: IAnonymousFunctionExpression (Symbol: Sub (i As System.Int32)) (OperationKind.AnonymousFunctionExpression, Type: null, IsInvalid) (Syntax: 'Sub(i As In ... WriteLine()')
      IBlockStatement (3 statements) (OperationKind.BlockStatement, IsInvalid) (Syntax: 'Sub(i As In ... WriteLine()')
        IBlockStatement (1 statements) (OperationKind.BlockStatement, IsInvalid) (Syntax: 'Sub(i As In ... WriteLine()')
          IExpressionStatement (OperationKind.ExpressionStatement, IsInvalid) (Syntax: 'Console.WriteLine()')
            Expression: IInvocationExpression (Sub System.Console.WriteLine()) (OperationKind.InvocationExpression, Type: System.Void, IsInvalid) (Syntax: 'Console.WriteLine()')
                Instance Receiver: null
                Arguments(0)
        ILabeledStatement (Label: exit) (OperationKind.LabeledStatement, IsInvalid) (Syntax: 'Sub(i As In ... WriteLine()')
          Statement: null
        IReturnStatement (OperationKind.ReturnStatement, IsInvalid) (Syntax: 'Sub(i As In ... WriteLine()')
          ReturnedValue: null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36670: Nested sub does not have a signature that is compatible with delegate 'Action'.
        Dim a As Action = CType(Sub(i As Integer) Console.WriteLine(), Action)'BIND:"CType(Sub(i As Integer) Console.WriteLine(), Action)"
                                ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of CTypeExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_ExplicitLambdaConversion_InvalidReturnType()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module M1
    Sub Main()
        Dim a As Func(Of String) = CType(Function() 1, Func(Of String))'BIND:"CType(Function() 1, Func(Of String))"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Func(Of System.String), IsInvalid) (Syntax: 'CType(Funct ... Of String))')
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
        Dim a As Func(Of String) = CType(Function() 1, Func(Of String))'BIND:"CType(Function() 1, Func(Of String))"
                                                    ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of CTypeExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_ExplicitLambdaConversion_ReturnRelaxation()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module M1
    Sub Main()
        Dim a As Func(Of Object) = CType(Function() 1, Func(Of Object))'BIND:"CType(Function() 1, Func(Of Object))"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Func(Of System.Object)) (Syntax: 'CType(Funct ... Of Object))')
  Target: IAnonymousFunctionExpression (Symbol: Function () As System.Object) (OperationKind.AnonymousFunctionExpression, Type: null) (Syntax: 'Function() 1')
      IBlockStatement (3 statements, 1 locals) (OperationKind.BlockStatement) (Syntax: 'Function() 1')
        Locals: Local_1: <anonymous local> As System.Object
        IReturnStatement (OperationKind.ReturnStatement) (Syntax: '1')
          ReturnedValue: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: '1')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        ILabeledStatement (Label: exit) (OperationKind.LabeledStatement) (Syntax: 'Function() 1')
          Statement: null
        IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'Function() 1')
          ReturnedValue: ILocalReferenceExpression:  (OperationKind.LocalReferenceExpression, Type: System.Object) (Syntax: 'Function() 1')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of CTypeExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateExpression_ExplicitLambdaConversion_ArgumentRelaxation()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module M1
    Sub Main()
        Dim a As Action(Of Object) = CType(Sub() Console.WriteLine(), Action(Of Object))'BIND:"CType(Sub() Console.WriteLine(), Action(Of Object))"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Action(Of System.Object)) (Syntax: 'CType(Sub() ... Of Object))')
  Target: IAnonymousFunctionExpression (Symbol: Sub ()) (OperationKind.AnonymousFunctionExpression, Type: null) (Syntax: 'Sub() Conso ... WriteLine()')
      IBlockStatement (3 statements) (OperationKind.BlockStatement) (Syntax: 'Sub() Conso ... WriteLine()')
        IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'Sub() Conso ... WriteLine()')
          IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.WriteLine()')
            Expression: IInvocationExpression (Sub System.Console.WriteLine()) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.WriteLine()')
                Instance Receiver: null
                Arguments(0)
        ILabeledStatement (Label: exit) (OperationKind.LabeledStatement) (Syntax: 'Sub() Conso ... WriteLine()')
          Statement: null
        IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'Sub() Conso ... WriteLine()')
          ReturnedValue: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of CTypeExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_ImplicitMethodBinding()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module M1
    Sub Method1()
        Dim a As Action = AddressOf Method2'BIND:"AddressOf Method2"
    End Sub

    Sub Method2()
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Action) (Syntax: 'AddressOf Method2')
  Target: IMethodBindingExpression: Sub M1.Method2() (OperationKind.MethodBindingExpression, Type: System.Action) (Syntax: 'AddressOf Method2')
      Instance Receiver: IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: M1) (Syntax: 'Method2')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_ImplicitMethodBinding_InvalidArgumentConversion()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module M1
    Sub Method1()
        Dim a As Action = AddressOf Method2'BIND:"AddressOf Method2"
    End Sub

    Sub Method2(i As Integer)
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Action, IsInvalid) (Syntax: 'AddressOf Method2')
  Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Operand: IOperation:  (OperationKind.None, IsInvalid) (Syntax: 'AddressOf Method2')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC31143: Method 'Public Sub Method2(i As Integer)' does not have a signature compatible with delegate 'Delegate Sub Action()'.
        Dim a As Action = AddressOf Method2'BIND:"AddressOf Method2"
                                    ~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_ImplicitMethodBinding_InvalidReturnType()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module M1
    Sub Method1()
        Dim a As Func(Of String) = AddressOf Method2 'BIND:"AddressOf Method2"
    End Sub

    Function Method2() As Integer
        Return 1
    End Function
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Func(Of System.String), IsInvalid) (Syntax: 'AddressOf Method2')
  Target: IMethodBindingExpression: Function M1.Method2() As System.Int32 (OperationKind.MethodBindingExpression, Type: System.Func(Of System.String), IsInvalid) (Syntax: 'AddressOf Method2')
      Instance Receiver: IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: M1, IsInvalid) (Syntax: 'Method2')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36663: Option Strict On does not allow narrowing in implicit type conversions between method 'Public Function Method2() As Integer' and delegate 'Delegate Function Func(Of String)() As String'.
        Dim a As Func(Of String) = AddressOf Method2 'BIND:"AddressOf Method2"
                                             ~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_ImplicitMethodBinding_ReturnRelaxation()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module M1
    Sub Method1()
        Dim a As Action = AddressOf Method2 'BIND:"AddressOf Method2"
    End Sub

    Function Method2() As Object
        Return 1
    End Function
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Action) (Syntax: 'AddressOf Method2')
  Target: IMethodBindingExpression: Function M1.Method2() As System.Object (OperationKind.MethodBindingExpression, Type: System.Action) (Syntax: 'AddressOf Method2')
      Instance Receiver: IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: M1) (Syntax: 'Method2')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_ImplicitMethodBinding_ArugmentRelaxation()
            Dim source = <![CDATA[
Option Strict Off
Imports System
Module M1
    Sub Method1()
        Dim a As Action(Of Integer) = AddressOf Method2'BIND:"AddressOf Method2"
    End Sub

    Sub Method2()
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Action(Of System.Int32)) (Syntax: 'AddressOf Method2')
  Target: IMethodBindingExpression: Sub M1.Method2() (OperationKind.MethodBindingExpression, Type: System.Action(Of System.Int32)) (Syntax: 'AddressOf Method2')
      Instance Receiver: IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: M1) (Syntax: 'Method2')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_DelegateConstructorLambdaArgument()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module M1
    Sub Method1()
        Dim a As Action = New Action(Sub()'BIND:"New Action(Sub()"
                                     End Sub)
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Action) (Syntax: 'New Action( ... End Sub)')
  Target: IAnonymousFunctionExpression (Symbol: Sub ()) (OperationKind.AnonymousFunctionExpression, Type: null) (Syntax: 'Sub()'BIND: ... End Sub')
      IBlockStatement (2 statements) (OperationKind.BlockStatement) (Syntax: 'Sub()'BIND: ... End Sub')
        ILabeledStatement (Label: exit) (OperationKind.LabeledStatement) (Syntax: 'End Sub')
          Statement: null
        IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'End Sub')
          ReturnedValue: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_DelegateConstructorLambdaArgument_InvalidArgumentType()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module M1
    Sub Method1()
        Dim a As Action = New Action(Sub(i As Integer)'BIND:"New Action(Sub(i As Integer)"
                                     End Sub)
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Action, IsInvalid) (Syntax: 'New Action( ... End Sub)')
  Target: IAnonymousFunctionExpression (Symbol: Sub (i As System.Int32)) (OperationKind.AnonymousFunctionExpression, Type: null, IsInvalid) (Syntax: 'Sub(i As In ... End Sub')
      IBlockStatement (2 statements) (OperationKind.BlockStatement, IsInvalid) (Syntax: 'Sub(i As In ... End Sub')
        ILabeledStatement (Label: exit) (OperationKind.LabeledStatement, IsInvalid) (Syntax: 'End Sub')
          Statement: null
        IReturnStatement (OperationKind.ReturnStatement, IsInvalid) (Syntax: 'End Sub')
          ReturnedValue: null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36670: Nested sub does not have a signature that is compatible with delegate 'Action'.
        Dim a As Action = New Action(Sub(i As Integer)'BIND:"New Action(Sub(i As Integer)"
                                     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_DelegateConstructorLambdaArgument_InvalidReturnType()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module M1
    Sub Method1()
        Dim a As Func(Of String) = New Func(Of String)(Function()'BIND:"New Func(Of String)(Function()"
                                                           Return 1
                                                       End Function)
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Func(Of System.String), IsInvalid) (Syntax: 'New Func(Of ... d Function)')
  Target: IAnonymousFunctionExpression (Symbol: Function () As System.String) (OperationKind.AnonymousFunctionExpression, Type: null, IsInvalid) (Syntax: 'Function()' ... nd Function')
      IBlockStatement (3 statements, 1 locals) (OperationKind.BlockStatement, IsInvalid) (Syntax: 'Function()' ... nd Function')
        Locals: Local_1: <anonymous local> As System.String
        IReturnStatement (OperationKind.ReturnStatement, IsInvalid) (Syntax: 'Return 1')
          ReturnedValue: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.String, IsInvalid) (Syntax: '1')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
        ILabeledStatement (Label: exit) (OperationKind.LabeledStatement) (Syntax: 'End Function')
          Statement: null
        IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'End Function')
          ReturnedValue: ILocalReferenceExpression:  (OperationKind.LocalReferenceExpression, Type: System.String) (Syntax: 'End Function')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30512: Option Strict On disallows implicit conversions from 'Integer' to 'String'.
                                                           Return 1
                                                                  ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_DelegateConstructorLambdaArgument_ArgumentRelaxation()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module M1
    Sub Method1()
        Dim a As Action(Of String) = New Action(Of String)(Sub()'BIND:"New Action(Of String)(Sub()"
                                                           End Sub)
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Action(Of System.String)) (Syntax: 'New Action( ... End Sub)')
  Target: IAnonymousFunctionExpression (Symbol: Sub ()) (OperationKind.AnonymousFunctionExpression, Type: null) (Syntax: 'Sub()'BIND: ... End Sub')
      IBlockStatement (2 statements) (OperationKind.BlockStatement) (Syntax: 'Sub()'BIND: ... End Sub')
        ILabeledStatement (Label: exit) (OperationKind.LabeledStatement) (Syntax: 'End Sub')
          Statement: null
        IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'End Sub')
          ReturnedValue: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_DelegateConstructorLambdaArgument_ReturnRelaxation()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module M1
    Sub Method1()
        Dim a As Action = New Action(Function()'BIND:"New Action(Function()"
                                         Return 1
                                     End Function)
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Action) (Syntax: 'New Action( ... d Function)')
  Target: IAnonymousFunctionExpression (Symbol: Function () As System.Int32) (OperationKind.AnonymousFunctionExpression, Type: null) (Syntax: 'Function()' ... nd Function')
      IBlockStatement (3 statements, 1 locals) (OperationKind.BlockStatement) (Syntax: 'Function()' ... nd Function')
        Locals: Local_1: <anonymous local> As System.Int32
        IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'Return 1')
          ReturnedValue: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        ILabeledStatement (Label: exit) (OperationKind.LabeledStatement) (Syntax: 'End Function')
          Statement: null
        IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'End Function')
          ReturnedValue: ILocalReferenceExpression:  (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'End Function')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_DelegateCreationAddressOfArgument()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module M1
    Sub Method1()
        Dim a As Action = New Action(AddressOf Method2)'BIND:"New Action(AddressOf Method2)"
    End Sub

    Sub Method2()
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Action) (Syntax: 'New Action( ... Of Method2)')
  Target: IMethodBindingExpression: Sub M1.Method2() (OperationKind.MethodBindingExpression, Type: System.Action) (Syntax: 'AddressOf Method2')
      Instance Receiver: IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: M1) (Syntax: 'Method2')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_DelegateCreationAddressOfArgument_ReturnRelaxation()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module M1
    Sub Method1()
        Dim a As Action = New Action(AddressOf Method2)'BIND:"New Action(AddressOf Method2)"
    End Sub

    Function Method2() As Object
        Return 1
    End Function
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Action) (Syntax: 'New Action( ... Of Method2)')
  Target: IMethodBindingExpression: Function M1.Method2() As System.Object (OperationKind.MethodBindingExpression, Type: System.Action) (Syntax: 'AddressOf Method2')
      Instance Receiver: IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: M1) (Syntax: 'Method2')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_DelegateCreationAddressOfArgument_ArgumentRelaxation()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module M1
    Sub Method1()
        Dim a As Action(Of Integer) = New Action(Of Integer)(AddressOf Method2)'BIND:"New Action(Of Integer)(AddressOf Method2)"
    End Sub

    Sub Method2(o As Object)
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Action(Of System.Int32)) (Syntax: 'New Action( ... Of Method2)')
  Target: IMethodBindingExpression: Sub M1.Method2(o As System.Object) (OperationKind.MethodBindingExpression, Type: System.Action(Of System.Int32)) (Syntax: 'AddressOf Method2')
      Instance Receiver: IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: M1) (Syntax: 'Method2')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_DelegateCreationAddressOfArgument_InvalidArgumentType()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module M1
    Sub Method1()
        Dim a As Action= New Action(AddressOf Method2)'BIND:"New Action(AddressOf Method2)"
    End Sub

    Sub Method2(o As Object)
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Action, IsInvalid) (Syntax: 'New Action( ... Of Method2)')
  Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Operand: IOperation:  (OperationKind.None, IsInvalid) (Syntax: 'AddressOf Method2')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC31143: Method 'Public Sub Method2(o As Object)' does not have a signature compatible with delegate 'Delegate Sub Action()'.
        Dim a As Action= New Action(AddressOf Method2)'BIND:"New Action(AddressOf Method2)"
                                              ~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_DelegateCreationAddressOfArgument_InvalidReturnType()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module M1
    Sub Method1()
        Dim a As Func(Of String) = New Func(Of String)(AddressOf Method2)'BIND:"New Func(Of String)(AddressOf Method2)"
    End Sub

    Sub Method2()
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Func(Of System.String), IsInvalid) (Syntax: 'New Func(Of ... Of Method2)')
  Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Operand: IOperation:  (OperationKind.None, IsInvalid) (Syntax: 'AddressOf Method2')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC31143: Method 'Public Sub Method2()' does not have a signature compatible with delegate 'Delegate Function Func(Of String)() As String'.
        Dim a As Func(Of String) = New Func(Of String)(AddressOf Method2)'BIND:"New Func(Of String)(AddressOf Method2)"
                                                                 ~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_DelegateCreationLambdaArgument()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module M1
    Sub Method1()
        Dim a As Action = New Action(Sub() Console.WriteLine())'BIND:"New Action(Sub() Console.WriteLine())"
    End Sub

End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Action) (Syntax: 'New Action( ... riteLine())')
  Target: IAnonymousFunctionExpression (Symbol: Sub ()) (OperationKind.AnonymousFunctionExpression, Type: null) (Syntax: 'Sub() Conso ... WriteLine()')
      IBlockStatement (3 statements) (OperationKind.BlockStatement) (Syntax: 'Sub() Conso ... WriteLine()')
        IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'Sub() Conso ... WriteLine()')
          IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.WriteLine()')
            Expression: IInvocationExpression (Sub System.Console.WriteLine()) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.WriteLine()')
                Instance Receiver: null
                Arguments(0)
        ILabeledStatement (Label: exit) (OperationKind.LabeledStatement) (Syntax: 'Sub() Conso ... WriteLine()')
          Statement: null
        IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'Sub() Conso ... WriteLine()')
          ReturnedValue: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_DelegateCreationLambdaArgument_ArgumentRelaxation()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module M1
    Sub Method1()
        Dim a As Action(Of String) = New Action(Of String)(Sub() Console.WriteLine())'BIND:"New Action(Of String)(Sub() Console.WriteLine())"
    End Sub

End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Action(Of System.String)) (Syntax: 'New Action( ... riteLine())')
  Target: IAnonymousFunctionExpression (Symbol: Sub ()) (OperationKind.AnonymousFunctionExpression, Type: null) (Syntax: 'Sub() Conso ... WriteLine()')
      IBlockStatement (3 statements) (OperationKind.BlockStatement) (Syntax: 'Sub() Conso ... WriteLine()')
        IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'Sub() Conso ... WriteLine()')
          IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.WriteLine()')
            Expression: IInvocationExpression (Sub System.Console.WriteLine()) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.WriteLine()')
                Instance Receiver: null
                Arguments(0)
        ILabeledStatement (Label: exit) (OperationKind.LabeledStatement) (Syntax: 'Sub() Conso ... WriteLine()')
          Statement: null
        IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'Sub() Conso ... WriteLine()')
          ReturnedValue: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_DelegateCreationLambdaArgument_ReturnRelaxation()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module M1
    Sub Method1()
        Dim a As Action = New Action(Function() 1)'BIND:"New Action(Function() 1)"
    End Sub

End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Action) (Syntax: 'New Action(Function() 1)')
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

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_DelegateCreationLambdaArgument_InvalidArgumentType()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module M1
    Sub Method1()
        Dim a As Action(Of Object) = New Action(Of Object)(Sub(i As Integer) Console.WriteLine())'BIND:"New Action(Of Object)(Sub(i As Integer) Console.WriteLine())"
    End Sub

End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Action(Of System.Object), IsInvalid) (Syntax: 'New Action( ... riteLine())')
  Target: IAnonymousFunctionExpression (Symbol: Sub (i As System.Int32)) (OperationKind.AnonymousFunctionExpression, Type: null, IsInvalid) (Syntax: 'Sub(i As In ... WriteLine()')
      IBlockStatement (3 statements) (OperationKind.BlockStatement, IsInvalid) (Syntax: 'Sub(i As In ... WriteLine()')
        IBlockStatement (1 statements) (OperationKind.BlockStatement, IsInvalid) (Syntax: 'Sub(i As In ... WriteLine()')
          IExpressionStatement (OperationKind.ExpressionStatement, IsInvalid) (Syntax: 'Console.WriteLine()')
            Expression: IInvocationExpression (Sub System.Console.WriteLine()) (OperationKind.InvocationExpression, Type: System.Void, IsInvalid) (Syntax: 'Console.WriteLine()')
                Instance Receiver: null
                Arguments(0)
        ILabeledStatement (Label: exit) (OperationKind.LabeledStatement, IsInvalid) (Syntax: 'Sub(i As In ... WriteLine()')
          Statement: null
        IReturnStatement (OperationKind.ReturnStatement, IsInvalid) (Syntax: 'Sub(i As In ... WriteLine()')
          ReturnedValue: null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30512: Option Strict On disallows implicit conversions from 'Object' to 'Integer'.
        Dim a As Action(Of Object) = New Action(Of Object)(Sub(i As Integer) Console.WriteLine())'BIND:"New Action(Of Object)(Sub(i As Integer) Console.WriteLine())"
                                                           ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_DelegateCreationLambdaArgument_InvalidReturnType()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module M1
    Sub Method1()
        Dim a As Func(Of Object) = New Func(Of Object)(Sub() Console.WriteLine())'BIND:"New Func(Of Object)(Sub() Console.WriteLine())"
    End Sub

End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Func(Of System.Object), IsInvalid) (Syntax: 'New Func(Of ... riteLine())')
  Target: IAnonymousFunctionExpression (Symbol: Sub ()) (OperationKind.AnonymousFunctionExpression, Type: null, IsInvalid) (Syntax: 'Sub() Conso ... WriteLine()')
      IBlockStatement (3 statements) (OperationKind.BlockStatement, IsInvalid) (Syntax: 'Sub() Conso ... WriteLine()')
        IBlockStatement (1 statements) (OperationKind.BlockStatement, IsInvalid) (Syntax: 'Sub() Conso ... WriteLine()')
          IExpressionStatement (OperationKind.ExpressionStatement, IsInvalid) (Syntax: 'Console.WriteLine()')
            Expression: IInvocationExpression (Sub System.Console.WriteLine()) (OperationKind.InvocationExpression, Type: System.Void, IsInvalid) (Syntax: 'Console.WriteLine()')
                Instance Receiver: null
                Arguments(0)
        ILabeledStatement (Label: exit) (OperationKind.LabeledStatement, IsInvalid) (Syntax: 'Sub() Conso ... WriteLine()')
          Statement: null
        IReturnStatement (OperationKind.ReturnStatement, IsInvalid) (Syntax: 'Sub() Conso ... WriteLine()')
          ReturnedValue: null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36670: Nested sub does not have a signature that is compatible with delegate 'Func(Of Object)'.
        Dim a As Func(Of Object) = New Func(Of Object)(Sub() Console.WriteLine())'BIND:"New Func(Of Object)(Sub() Console.WriteLine())"
                                                       ~~~~~~~~~~~~~~~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub
    End Class
End Namespace
