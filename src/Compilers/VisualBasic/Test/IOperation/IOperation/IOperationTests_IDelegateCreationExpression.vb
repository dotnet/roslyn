' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase

#Region "Lambda Expressions"

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_ImplicitLambdaConversion()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module M1
    Sub Method1()
        Dim a As Action = Sub() Console.WriteLine("")'BIND:"Dim a As Action = Sub() Console.WriteLine("")"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim a As Ac ... iteLine("")')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'a As Action ... iteLine("")')
    Declarators:
        IVariableDeclaratorOperation (Symbol: a As System.Action) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'a')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= Sub() Con ... iteLine("")')
        IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action, IsImplicit) (Syntax: 'Sub() Conso ... iteLine("")')
          Target: 
            IAnonymousFunctionOperation (Symbol: Sub ()) (OperationKind.AnonymousFunction, Type: null) (Syntax: 'Sub() Conso ... iteLine("")')
              IBlockOperation (3 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Sub() Conso ... iteLine("")')
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.WriteLine("")')
                  Expression: 
                    IInvocationOperation (Sub System.Console.WriteLine(value As System.String)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.WriteLine("")')
                      Instance Receiver: 
                        null
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: '""')
                            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "") (Syntax: '""')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'Sub() Conso ... iteLine("")')
                  Statement: 
                    null
                IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'Sub() Conso ... iteLine("")')
                  ReturnedValue: 
                    null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_ImplicitLambdaConversion_JustInitializerReturnsOnlyLambda()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module M1
    Sub Method1()
        Dim a As Action = Sub() Console.WriteLine("")'BIND:"Sub() Console.WriteLine("")"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IAnonymousFunctionOperation (Symbol: Sub ()) (OperationKind.AnonymousFunction, Type: null) (Syntax: 'Sub() Conso ... iteLine("")')
  IBlockOperation (3 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Sub() Conso ... iteLine("")')
    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.WriteLine("")')
      Expression: 
        IInvocationOperation (Sub System.Console.WriteLine(value As System.String)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.WriteLine("")')
          Instance Receiver: 
            null
          Arguments(1):
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: '""')
                ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "") (Syntax: '""')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'Sub() Conso ... iteLine("")')
      Statement: 
        null
    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'Sub() Conso ... iteLine("")')
      ReturnedValue: 
        null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of SingleLineLambdaExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_ImplicitLambdaConversion_DisallowedArgumentType()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module M1
    Sub Method1()
        Dim a As Action = Sub(i As Integer) Console.WriteLine("")'BIND:"Dim a As Action = Sub(i As Integer) Console.WriteLine("")"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'Dim a As Ac ... iteLine("")')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'a As Action ... iteLine("")')
    Declarators:
        IVariableDeclaratorOperation (Symbol: a As System.Action) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'a')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= Sub(i As  ... iteLine("")')
        IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action, IsInvalid, IsImplicit) (Syntax: 'Sub(i As In ... iteLine("")')
          Target: 
            IAnonymousFunctionOperation (Symbol: Sub (i As System.Int32)) (OperationKind.AnonymousFunction, Type: null, IsInvalid) (Syntax: 'Sub(i As In ... iteLine("")')
              IBlockOperation (3 statements) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'Sub(i As In ... iteLine("")')
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'Console.WriteLine("")')
                  Expression: 
                    IInvocationOperation (Sub System.Console.WriteLine(value As System.String)) (OperationKind.Invocation, Type: System.Void, IsInvalid) (Syntax: 'Console.WriteLine("")')
                      Instance Receiver: 
                        null
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null, IsInvalid) (Syntax: '""')
                            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "", IsInvalid) (Syntax: '""')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsInvalid, IsImplicit) (Syntax: 'Sub(i As In ... iteLine("")')
                  Statement: 
                    null
                IReturnOperation (OperationKind.Return, Type: null, IsInvalid, IsImplicit) (Syntax: 'Sub(i As In ... iteLine("")')
                  ReturnedValue: 
                    null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36670: Nested sub does not have a signature that is compatible with delegate 'Action'.
        Dim a As Action = Sub(i As Integer) Console.WriteLine("")'BIND:"Dim a As Action = Sub(i As Integer) Console.WriteLine("")"
                          ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]>.Value
            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_ImplicitLambdaConversion_InvalidArgumentType()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module M1
    Class C1
    End Class
    Sub Method1()
        Dim a As Action(Of String) = Sub(c1 As C1) Console.WriteLine("")'BIND:"Dim a As Action(Of String) = Sub(c1 As C1) Console.WriteLine("")"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'Dim a As Ac ... iteLine("")')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'a As Action ... iteLine("")')
    Declarators:
        IVariableDeclaratorOperation (Symbol: a As System.Action(Of System.String)) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'a')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= Sub(c1 As ... iteLine("")')
        IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action(Of System.String), IsInvalid, IsImplicit) (Syntax: 'Sub(c1 As C ... iteLine("")')
          Target: 
            IAnonymousFunctionOperation (Symbol: Sub (c1 As M1.C1)) (OperationKind.AnonymousFunction, Type: null, IsInvalid) (Syntax: 'Sub(c1 As C ... iteLine("")')
              IBlockOperation (3 statements) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'Sub(c1 As C ... iteLine("")')
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'Console.WriteLine("")')
                  Expression: 
                    IInvocationOperation (Sub System.Console.WriteLine(value As System.String)) (OperationKind.Invocation, Type: System.Void, IsInvalid) (Syntax: 'Console.WriteLine("")')
                      Instance Receiver: 
                        null
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null, IsInvalid) (Syntax: '""')
                            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "", IsInvalid) (Syntax: '""')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsInvalid, IsImplicit) (Syntax: 'Sub(c1 As C ... iteLine("")')
                  Statement: 
                    null
                IReturnOperation (OperationKind.Return, Type: null, IsInvalid, IsImplicit) (Syntax: 'Sub(c1 As C ... iteLine("")')
                  ReturnedValue: 
                    null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36670: Nested sub does not have a signature that is compatible with delegate 'Action(Of String)'.
        Dim a As Action(Of String) = Sub(c1 As C1) Console.WriteLine("")'BIND:"Dim a As Action(Of String) = Sub(c1 As C1) Console.WriteLine("")"
                                     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_ImplicitLambdaConversion_InvalidReturnType()
            Dim source = <![CDATA[
Option Strict Off
Imports System
Module M1
    Sub Method1()
        Dim a As Func(Of String) = Function() New NonExistant()'BIND:"Dim a As Func(Of String) = Function() New NonExistant()"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'Dim a As Fu ... nExistant()')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'a As Func(O ... nExistant()')
    Declarators:
        IVariableDeclaratorOperation (Symbol: a As System.Func(Of System.String)) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'a')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= Function( ... nExistant()')
        IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.String), IsInvalid, IsImplicit) (Syntax: 'Function()  ... nExistant()')
          Target: 
            IAnonymousFunctionOperation (Symbol: Function () As System.String) (OperationKind.AnonymousFunction, Type: null, IsInvalid) (Syntax: 'Function()  ... nExistant()')
              IBlockOperation (3 statements, 1 locals) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'Function()  ... nExistant()')
                Locals: Local_1: <anonymous local> As System.String
                IReturnOperation (OperationKind.Return, Type: null, IsInvalid, IsImplicit) (Syntax: 'New NonExistant()')
                  ReturnedValue: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String, IsInvalid, IsImplicit) (Syntax: 'New NonExistant()')
                      Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      Operand: 
                        IInvalidOperation (OperationKind.Invalid, Type: NonExistant, IsInvalid) (Syntax: 'New NonExistant()')
                          Children(0)
                ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsInvalid, IsImplicit) (Syntax: 'Function()  ... nExistant()')
                  Statement: 
                    null
                IReturnOperation (OperationKind.Return, Type: null, IsInvalid, IsImplicit) (Syntax: 'Function()  ... nExistant()')
                  ReturnedValue: 
                    ILocalReferenceOperation:  (OperationKind.LocalReference, Type: System.String, IsInvalid, IsImplicit) (Syntax: 'Function()  ... nExistant()')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30002: Type 'NonExistant' is not defined.
        Dim a As Func(Of String) = Function() New NonExistant()'BIND:"Dim a As Func(Of String) = Function() New NonExistant()"
                                                  ~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_ImplicitLambdaConversion_DisallowedReturnType()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module M1
    Sub Method1()
        Dim a As Func(Of String) = Function() 1'BIND:"Dim a As Func(Of String) = Function() 1"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'Dim a As Fu ... unction() 1')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'a As Func(O ... unction() 1')
    Declarators:
        IVariableDeclaratorOperation (Symbol: a As System.Func(Of System.String)) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'a')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= Function() 1')
        IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.String), IsInvalid, IsImplicit) (Syntax: 'Function() 1')
          Target: 
            IAnonymousFunctionOperation (Symbol: Function () As System.String) (OperationKind.AnonymousFunction, Type: null, IsInvalid) (Syntax: 'Function() 1')
              IBlockOperation (3 statements, 1 locals) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'Function() 1')
                Locals: Local_1: <anonymous local> As System.String
                IReturnOperation (OperationKind.Return, Type: null, IsInvalid, IsImplicit) (Syntax: '1')
                  ReturnedValue: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String, IsInvalid, IsImplicit) (Syntax: '1')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      Operand: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
                ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsInvalid, IsImplicit) (Syntax: 'Function() 1')
                  Statement: 
                    null
                IReturnOperation (OperationKind.Return, Type: null, IsInvalid, IsImplicit) (Syntax: 'Function() 1')
                  ReturnedValue: 
                    ILocalReferenceOperation:  (OperationKind.LocalReference, Type: System.String, IsInvalid, IsImplicit) (Syntax: 'Function() 1')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30512: Option Strict On disallows implicit conversions from 'Integer' to 'String'.
        Dim a As Func(Of String) = Function() 1'BIND:"Dim a As Func(Of String) = Function() 1"
                                              ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_ImplicitLambdaConversion_ReturnRelaxation()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module M1
    Sub Method1()
        Dim a As Action = Function() 1'BIND:"Dim a As Action = Function() 1"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim a As Ac ... unction() 1')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'a As Action ... unction() 1')
    Declarators:
        IVariableDeclaratorOperation (Symbol: a As System.Action) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'a')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= Function() 1')
        IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action, IsImplicit) (Syntax: 'Function() 1')
          Target: 
            IAnonymousFunctionOperation (Symbol: Function () As System.Int32) (OperationKind.AnonymousFunction, Type: null) (Syntax: 'Function() 1')
              IBlockOperation (3 statements, 1 locals) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Function() 1')
                Locals: Local_1: <anonymous local> As System.Int32
                IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: '1')
                  ReturnedValue: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'Function() 1')
                  Statement: 
                    null
                IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'Function() 1')
                  ReturnedValue: 
                    ILocalReferenceOperation:  (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'Function() 1')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_ImplicitLambdaExpression_RelaxationOfArgument()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module M1
    Sub Method1()
        Dim a As Action(Of String) = Sub(o As Object) Console.WriteLine(o)'BIND:"Dim a As Action(Of String) = Sub(o As Object) Console.WriteLine(o)"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim a As Ac ... riteLine(o)')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'a As Action ... riteLine(o)')
    Declarators:
        IVariableDeclaratorOperation (Symbol: a As System.Action(Of System.String)) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'a')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= Sub(o As  ... riteLine(o)')
        IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action(Of System.String), IsImplicit) (Syntax: 'Sub(o As Ob ... riteLine(o)')
          Target: 
            IAnonymousFunctionOperation (Symbol: Sub (o As System.Object)) (OperationKind.AnonymousFunction, Type: null) (Syntax: 'Sub(o As Ob ... riteLine(o)')
              IBlockOperation (3 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Sub(o As Ob ... riteLine(o)')
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.WriteLine(o)')
                  Expression: 
                    IInvocationOperation (Sub System.Console.WriteLine(value As System.Object)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.WriteLine(o)')
                      Instance Receiver: 
                        null
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'o')
                            IParameterReferenceOperation: o (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'Sub(o As Ob ... riteLine(o)')
                  Statement: 
                    null
                IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'Sub(o As Ob ... riteLine(o)')
                  ReturnedValue: 
                    null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_CTypeLambdaConversion()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module M1
    Sub Main()
        Dim a As Action = CType(Sub() Console.WriteLine(), Action)'BIND:"CType(Sub() Console.WriteLine(), Action)"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action) (Syntax: 'CType(Sub() ... (), Action)')
  Target: 
    IAnonymousFunctionOperation (Symbol: Sub ()) (OperationKind.AnonymousFunction, Type: null) (Syntax: 'Sub() Conso ... WriteLine()')
      IBlockOperation (3 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Sub() Conso ... WriteLine()')
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.WriteLine()')
          Expression: 
            IInvocationOperation (Sub System.Console.WriteLine()) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.WriteLine()')
              Instance Receiver: 
                null
              Arguments(0)
        ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'Sub() Conso ... WriteLine()')
          Statement: 
            null
        IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'Sub() Conso ... WriteLine()')
          ReturnedValue: 
            null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of CTypeExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_CTypeLambdaConversion_DisallowedArgumentType()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module M1
    Sub Main()
        Dim a As Action = CType(Sub(i As Integer) Console.WriteLine(), Action)'BIND:"CType(Sub(i As Integer) Console.WriteLine(), Action)"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action, IsInvalid) (Syntax: 'CType(Sub(i ... (), Action)')
  Target: 
    IAnonymousFunctionOperation (Symbol: Sub (i As System.Int32)) (OperationKind.AnonymousFunction, Type: null, IsInvalid) (Syntax: 'Sub(i As In ... WriteLine()')
      IBlockOperation (3 statements) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'Sub(i As In ... WriteLine()')
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'Console.WriteLine()')
          Expression: 
            IInvocationOperation (Sub System.Console.WriteLine()) (OperationKind.Invocation, Type: System.Void, IsInvalid) (Syntax: 'Console.WriteLine()')
              Instance Receiver: 
                null
              Arguments(0)
        ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsInvalid, IsImplicit) (Syntax: 'Sub(i As In ... WriteLine()')
          Statement: 
            null
        IReturnOperation (OperationKind.Return, Type: null, IsInvalid, IsImplicit) (Syntax: 'Sub(i As In ... WriteLine()')
          ReturnedValue: 
            null
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
        Public Sub DelegateCreationExpression_CTypeLambdaConversion_InvalidArgumentType()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module M1
    Class C1
    End Class
    Sub Method1()
        Dim a As Action(Of String) = CType(Sub(c1 As C1) Console.WriteLine(""), Action(Of String))'BIND:"CType(Sub(c1 As C1) Console.WriteLine(""), Action(Of String))"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action(Of System.String), IsInvalid) (Syntax: 'CType(Sub(c ... Of String))')
  Target: 
    IAnonymousFunctionOperation (Symbol: Sub (c1 As M1.C1)) (OperationKind.AnonymousFunction, Type: null, IsInvalid) (Syntax: 'Sub(c1 As C ... iteLine("")')
      IBlockOperation (3 statements) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'Sub(c1 As C ... iteLine("")')
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'Console.WriteLine("")')
          Expression: 
            IInvocationOperation (Sub System.Console.WriteLine(value As System.String)) (OperationKind.Invocation, Type: System.Void, IsInvalid) (Syntax: 'Console.WriteLine("")')
              Instance Receiver: 
                null
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null, IsInvalid) (Syntax: '""')
                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "", IsInvalid) (Syntax: '""')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsInvalid, IsImplicit) (Syntax: 'Sub(c1 As C ... iteLine("")')
          Statement: 
            null
        IReturnOperation (OperationKind.Return, Type: null, IsInvalid, IsImplicit) (Syntax: 'Sub(c1 As C ... iteLine("")')
          ReturnedValue: 
            null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36670: Nested sub does not have a signature that is compatible with delegate 'Action(Of String)'.
        Dim a As Action(Of String) = CType(Sub(c1 As C1) Console.WriteLine(""), Action(Of String))'BIND:"CType(Sub(c1 As C1) Console.WriteLine(""), Action(Of String))"
                                           ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of CTypeExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_CTypeLambdaConversion_DisallowedReturnType()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module M1
    Sub Main()
        Dim a As Func(Of String) = CType(Function() 1, Func(Of String))'BIND:"CType(Function() 1, Func(Of String))"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.String), IsInvalid) (Syntax: 'CType(Funct ... Of String))')
  Target: 
    IAnonymousFunctionOperation (Symbol: Function () As System.String) (OperationKind.AnonymousFunction, Type: null, IsInvalid) (Syntax: 'Function() 1')
      IBlockOperation (3 statements, 1 locals) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'Function() 1')
        Locals: Local_1: <anonymous local> As System.String
        IReturnOperation (OperationKind.Return, Type: null, IsInvalid, IsImplicit) (Syntax: '1')
          ReturnedValue: 
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String, IsInvalid, IsImplicit) (Syntax: '1')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Operand: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
        ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsInvalid, IsImplicit) (Syntax: 'Function() 1')
          Statement: 
            null
        IReturnOperation (OperationKind.Return, Type: null, IsInvalid, IsImplicit) (Syntax: 'Function() 1')
          ReturnedValue: 
            ILocalReferenceOperation:  (OperationKind.LocalReference, Type: System.String, IsInvalid, IsImplicit) (Syntax: 'Function() 1')
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
        Public Sub DelegateCreationExpression_CTypeLambdaConversion_InvalidReturnType()
            Dim source = <![CDATA[
Option Strict Off
Imports System
Module M1
    Sub Main()
        Dim a As Func(Of String) = CType(Function() New NonExistant(), Func(Of String)) 'BIND:"CType(Function() New NonExistant(), Func(Of String))"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.String), IsInvalid) (Syntax: 'CType(Funct ... Of String))')
  Target: 
    IAnonymousFunctionOperation (Symbol: Function () As System.String) (OperationKind.AnonymousFunction, Type: null, IsInvalid) (Syntax: 'Function()  ... nExistant()')
      IBlockOperation (3 statements, 1 locals) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'Function()  ... nExistant()')
        Locals: Local_1: <anonymous local> As System.String
        IReturnOperation (OperationKind.Return, Type: null, IsInvalid, IsImplicit) (Syntax: 'New NonExistant()')
          ReturnedValue: 
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String, IsInvalid, IsImplicit) (Syntax: 'New NonExistant()')
              Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Operand: 
                IInvalidOperation (OperationKind.Invalid, Type: NonExistant, IsInvalid) (Syntax: 'New NonExistant()')
                  Children(0)
        ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsInvalid, IsImplicit) (Syntax: 'Function()  ... nExistant()')
          Statement: 
            null
        IReturnOperation (OperationKind.Return, Type: null, IsInvalid, IsImplicit) (Syntax: 'Function()  ... nExistant()')
          ReturnedValue: 
            ILocalReferenceOperation:  (OperationKind.LocalReference, Type: System.String, IsInvalid, IsImplicit) (Syntax: 'Function()  ... nExistant()')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30002: Type 'NonExistant' is not defined.
        Dim a As Func(Of String) = CType(Function() New NonExistant(), Func(Of String)) 'BIND:"CType(Function() New NonExistant(), Func(Of String))"
                                                        ~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of CTypeExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_CTypeLambdaConversion_InvalidVariableType()
            Dim source = <![CDATA[
Option Strict On
Imports System

Module Program
    Sub Main()
        Dim a As Action(Of Object) = CType(Sub(i As Integer) Console.WriteLine(), Action(Of Integer))'BIND:"Dim a As Action(Of Object) = CType(Sub(i As Integer) Console.WriteLine(), Action(Of Integer))"
    End Sub
End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'Dim a As Ac ... f Integer))')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'a As Action ... f Integer))')
    Declarators:
        IVariableDeclaratorOperation (Symbol: a As System.Action(Of System.Object)) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'a')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= CType(Sub ... f Integer))')
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Action(Of System.Object), IsInvalid, IsImplicit) (Syntax: 'CType(Sub(i ... f Integer))')
          Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action(Of System.Int32), IsInvalid) (Syntax: 'CType(Sub(i ... f Integer))')
              Target: 
                IAnonymousFunctionOperation (Symbol: Sub (i As System.Int32)) (OperationKind.AnonymousFunction, Type: null, IsInvalid) (Syntax: 'Sub(i As In ... WriteLine()')
                  IBlockOperation (3 statements) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'Sub(i As In ... WriteLine()')
                    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'Console.WriteLine()')
                      Expression: 
                        IInvocationOperation (Sub System.Console.WriteLine()) (OperationKind.Invocation, Type: System.Void, IsInvalid) (Syntax: 'Console.WriteLine()')
                          Instance Receiver: 
                            null
                          Arguments(0)
                    ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsInvalid, IsImplicit) (Syntax: 'Sub(i As In ... WriteLine()')
                      Statement: 
                        null
                    IReturnOperation (OperationKind.Return, Type: null, IsInvalid, IsImplicit) (Syntax: 'Sub(i As In ... WriteLine()')
                      ReturnedValue: 
                        null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36755: 'Action(Of Integer)' cannot be converted to 'Action(Of Object)' because 'Object' is not derived from 'Integer', as required for the 'In' generic parameter 'T' in 'Delegate Sub Action(Of In T)(obj As T)'.
        Dim a As Action(Of Object) = CType(Sub(i As Integer) Console.WriteLine(), Action(Of Integer))'BIND:"Dim a As Action(Of Object) = CType(Sub(i As Integer) Console.WriteLine(), Action(Of Integer))"
                                     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_CTypeLambdaConversion_ReturnRelaxation()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module M1
    Sub Main()
        Dim a As Func(Of Object) = CType(Function() 1, Func(Of Object))'BIND:"CType(Function() 1, Func(Of Object))"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Object)) (Syntax: 'CType(Funct ... Of Object))')
  Target: 
    IAnonymousFunctionOperation (Symbol: Function () As System.Object) (OperationKind.AnonymousFunction, Type: null) (Syntax: 'Function() 1')
      IBlockOperation (3 statements, 1 locals) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Function() 1')
        Locals: Local_1: <anonymous local> As System.Object
        IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: '1')
          ReturnedValue: 
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: '1')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Operand: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'Function() 1')
          Statement: 
            null
        IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'Function() 1')
          ReturnedValue: 
            ILocalReferenceOperation:  (OperationKind.LocalReference, Type: System.Object, IsImplicit) (Syntax: 'Function() 1')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of CTypeExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_CTypeLambdaConversion_ArgumentRelaxation()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module M1
    Sub Main()
        Dim a As Action(Of Object) = CType(Sub() Console.WriteLine(), Action(Of Object))'BIND:"CType(Sub() Console.WriteLine(), Action(Of Object))"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action(Of System.Object)) (Syntax: 'CType(Sub() ... Of Object))')
  Target: 
    IAnonymousFunctionOperation (Symbol: Sub ()) (OperationKind.AnonymousFunction, Type: null) (Syntax: 'Sub() Conso ... WriteLine()')
      IBlockOperation (3 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Sub() Conso ... WriteLine()')
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.WriteLine()')
          Expression: 
            IInvocationOperation (Sub System.Console.WriteLine()) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.WriteLine()')
              Instance Receiver: 
                null
              Arguments(0)
        ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'Sub() Conso ... WriteLine()')
          Statement: 
            null
        IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'Sub() Conso ... WriteLine()')
          ReturnedValue: 
            null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of CTypeExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_CTypeMethodBinding()
            Dim source = <![CDATA[
Option Strict On
Imports System

Module Program
    Sub Main()
        Dim a As Action = CType(AddressOf M1, Action)'BIND:"CType(AddressOf M1, Action)"
    End Sub

    Sub M1()
    End Sub
End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action) (Syntax: 'CType(Addre ... M1, Action)')
  Target: 
    IMethodReferenceOperation: Sub Program.M1() (Static) (OperationKind.MethodReference, Type: null) (Syntax: 'AddressOf M1')
      Instance Receiver: 
        null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of CTypeExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_CTypeMethodBinding_InvalidVariableType()
            Dim source = <![CDATA[
Option Strict On
Imports System

Module Program
    Sub Main()
        Dim a As Action(Of Object) = CType(AddressOf M1, Action(Of Integer))'BIND:"Dim a As Action(Of Object) = CType(AddressOf M1, Action(Of Integer))"
    End Sub

    Sub M1(i As Integer)
    End Sub
End Module
]]>.Value

            ' Explicitly verifying the entire tree here to ensure that the top level initializer statement is actually an IConversion, and not
            ' a delegate creation
            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'Dim a As Ac ... f Integer))')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'a As Action ... f Integer))')
    Declarators:
        IVariableDeclaratorOperation (Symbol: a As System.Action(Of System.Object)) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'a')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= CType(Add ... f Integer))')
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Action(Of System.Object), IsInvalid, IsImplicit) (Syntax: 'CType(Addre ... f Integer))')
          Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action(Of System.Int32), IsInvalid) (Syntax: 'CType(Addre ... f Integer))')
              Target: 
                IMethodReferenceOperation: Sub Program.M1(i As System.Int32) (Static) (OperationKind.MethodReference, Type: null, IsInvalid) (Syntax: 'AddressOf M1')
                  Instance Receiver: 
                    null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36755: 'Action(Of Integer)' cannot be converted to 'Action(Of Object)' because 'Object' is not derived from 'Integer', as required for the 'In' generic parameter 'T' in 'Delegate Sub Action(Of In T)(obj As T)'.
        Dim a As Action(Of Object) = CType(AddressOf M1, Action(Of Integer))'BIND:"Dim a As Action(Of Object) = CType(AddressOf M1, Action(Of Integer))"
                                     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_DirectCastLambdaConversion()
            Dim source = <![CDATA[
Option Strict On
Imports System

Module Program
    Sub Main()
        Dim a As Action = DirectCast(Sub() Console.WriteLine(), Action)'BIND:"DirectCast(Sub() Console.WriteLine(), Action)"
    End Sub
End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action) (Syntax: 'DirectCast( ... (), Action)')
  Target: 
    IAnonymousFunctionOperation (Symbol: Sub ()) (OperationKind.AnonymousFunction, Type: null) (Syntax: 'Sub() Conso ... WriteLine()')
      IBlockOperation (3 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Sub() Conso ... WriteLine()')
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.WriteLine()')
          Expression: 
            IInvocationOperation (Sub System.Console.WriteLine()) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.WriteLine()')
              Instance Receiver: 
                null
              Arguments(0)
        ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'Sub() Conso ... WriteLine()')
          Statement: 
            null
        IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'Sub() Conso ... WriteLine()')
          ReturnedValue: 
            null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of DirectCastExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_DirectCastLambdaConversion_DisallowedArgumentType()
            Dim source = <![CDATA[
Option Strict On
Imports System

Module Program
    Sub Main()
        Dim a As Action = DirectCast(Sub(i As Integer) Console.WriteLine(), Action)'BIND:"DirectCast(Sub(i As Integer) Console.WriteLine(), Action)"
    End Sub
End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action, IsInvalid) (Syntax: 'DirectCast( ... (), Action)')
  Target: 
    IAnonymousFunctionOperation (Symbol: Sub (i As System.Int32)) (OperationKind.AnonymousFunction, Type: null, IsInvalid) (Syntax: 'Sub(i As In ... WriteLine()')
      IBlockOperation (3 statements) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'Sub(i As In ... WriteLine()')
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'Console.WriteLine()')
          Expression: 
            IInvocationOperation (Sub System.Console.WriteLine()) (OperationKind.Invocation, Type: System.Void, IsInvalid) (Syntax: 'Console.WriteLine()')
              Instance Receiver: 
                null
              Arguments(0)
        ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsInvalid, IsImplicit) (Syntax: 'Sub(i As In ... WriteLine()')
          Statement: 
            null
        IReturnOperation (OperationKind.Return, Type: null, IsInvalid, IsImplicit) (Syntax: 'Sub(i As In ... WriteLine()')
          ReturnedValue: 
            null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36670: Nested sub does not have a signature that is compatible with delegate 'Action'.
        Dim a As Action = DirectCast(Sub(i As Integer) Console.WriteLine(), Action)'BIND:"DirectCast(Sub(i As Integer) Console.WriteLine(), Action)"
                                     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of DirectCastExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_DirectCastLambdaConversion_InvalidArgumentType()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module M1
    Class C1
    End Class
    Sub Method1()
        Dim a As Action(Of String) = DirectCast(Sub(c1 As C1) Console.WriteLine(""), Action(Of String))'BIND:"DirectCast(Sub(c1 As C1) Console.WriteLine(""), Action(Of String))"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action(Of System.String), IsInvalid) (Syntax: 'DirectCast( ... Of String))')
  Target: 
    IAnonymousFunctionOperation (Symbol: Sub (c1 As M1.C1)) (OperationKind.AnonymousFunction, Type: null, IsInvalid) (Syntax: 'Sub(c1 As C ... iteLine("")')
      IBlockOperation (3 statements) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'Sub(c1 As C ... iteLine("")')
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'Console.WriteLine("")')
          Expression: 
            IInvocationOperation (Sub System.Console.WriteLine(value As System.String)) (OperationKind.Invocation, Type: System.Void, IsInvalid) (Syntax: 'Console.WriteLine("")')
              Instance Receiver: 
                null
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null, IsInvalid) (Syntax: '""')
                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "", IsInvalid) (Syntax: '""')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsInvalid, IsImplicit) (Syntax: 'Sub(c1 As C ... iteLine("")')
          Statement: 
            null
        IReturnOperation (OperationKind.Return, Type: null, IsInvalid, IsImplicit) (Syntax: 'Sub(c1 As C ... iteLine("")')
          ReturnedValue: 
            null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36670: Nested sub does not have a signature that is compatible with delegate 'Action(Of String)'.
        Dim a As Action(Of String) = DirectCast(Sub(c1 As C1) Console.WriteLine(""), Action(Of String))'BIND:"DirectCast(Sub(c1 As C1) Console.WriteLine(""), Action(Of String))"
                                                ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of DirectCastExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_DirectCastLambdaConversion_DisallowedReturnType()
            Dim source = <![CDATA[
Option Strict On
Imports System

Module Program
    Sub Main()
        Dim a As Func(Of String) = DirectCast(Function() 1, Func(Of String))'BIND:"DirectCast(Function() 1, Func(Of String))"
    End Sub
End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.String), IsInvalid) (Syntax: 'DirectCast( ... Of String))')
  Target: 
    IAnonymousFunctionOperation (Symbol: Function () As System.String) (OperationKind.AnonymousFunction, Type: null, IsInvalid) (Syntax: 'Function() 1')
      IBlockOperation (3 statements, 1 locals) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'Function() 1')
        Locals: Local_1: <anonymous local> As System.String
        IReturnOperation (OperationKind.Return, Type: null, IsInvalid, IsImplicit) (Syntax: '1')
          ReturnedValue: 
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String, IsInvalid, IsImplicit) (Syntax: '1')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Operand: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
        ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsInvalid, IsImplicit) (Syntax: 'Function() 1')
          Statement: 
            null
        IReturnOperation (OperationKind.Return, Type: null, IsInvalid, IsImplicit) (Syntax: 'Function() 1')
          ReturnedValue: 
            ILocalReferenceOperation:  (OperationKind.LocalReference, Type: System.String, IsInvalid, IsImplicit) (Syntax: 'Function() 1')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30512: Option Strict On disallows implicit conversions from 'Integer' to 'String'.
        Dim a As Func(Of String) = DirectCast(Function() 1, Func(Of String))'BIND:"DirectCast(Function() 1, Func(Of String))"
                                                         ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of DirectCastExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_DirectCastLambdaConversion_InvalidReturnType()
            Dim source = <![CDATA[
Option Strict Off
Imports System

Module Program
    Sub Main()
        Dim a As Func(Of String) = DirectCast(Function() New NonExistant(), Func(Of String)) 'BIND:"DirectCast(Function() New NonExistant(), Func(Of String))"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.String), IsInvalid) (Syntax: 'DirectCast( ... Of String))')
  Target: 
    IAnonymousFunctionOperation (Symbol: Function () As System.String) (OperationKind.AnonymousFunction, Type: null, IsInvalid) (Syntax: 'Function()  ... nExistant()')
      IBlockOperation (3 statements, 1 locals) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'Function()  ... nExistant()')
        Locals: Local_1: <anonymous local> As System.String
        IReturnOperation (OperationKind.Return, Type: null, IsInvalid, IsImplicit) (Syntax: 'New NonExistant()')
          ReturnedValue: 
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String, IsInvalid, IsImplicit) (Syntax: 'New NonExistant()')
              Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Operand: 
                IInvalidOperation (OperationKind.Invalid, Type: NonExistant, IsInvalid) (Syntax: 'New NonExistant()')
                  Children(0)
        ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsInvalid, IsImplicit) (Syntax: 'Function()  ... nExistant()')
          Statement: 
            null
        IReturnOperation (OperationKind.Return, Type: null, IsInvalid, IsImplicit) (Syntax: 'Function()  ... nExistant()')
          ReturnedValue: 
            ILocalReferenceOperation:  (OperationKind.LocalReference, Type: System.String, IsInvalid, IsImplicit) (Syntax: 'Function()  ... nExistant()')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30002: Type 'NonExistant' is not defined.
        Dim a As Func(Of String) = DirectCast(Function() New NonExistant(), Func(Of String)) 'BIND:"DirectCast(Function() New NonExistant(), Func(Of String))"
                                                             ~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of DirectCastExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_DirectCastLambdaConversion_InvalidVariableType()
            Dim source = <![CDATA[
Option Strict On
Imports System

Module Program
    Sub Main()
        Dim a As Func(Of String) = DirectCast(Function() 1, Func(Of Integer))'BIND:"DirectCast(Function() 1, Func(Of Integer))"
    End Sub
End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32), IsInvalid) (Syntax: 'DirectCast( ... f Integer))')
  Target: 
    IAnonymousFunctionOperation (Symbol: Function () As System.Int32) (OperationKind.AnonymousFunction, Type: null, IsInvalid) (Syntax: 'Function() 1')
      IBlockOperation (3 statements, 1 locals) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'Function() 1')
        Locals: Local_1: <anonymous local> As System.Int32
        IReturnOperation (OperationKind.Return, Type: null, IsInvalid, IsImplicit) (Syntax: '1')
          ReturnedValue: 
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
        ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsInvalid, IsImplicit) (Syntax: 'Function() 1')
          Statement: 
            null
        IReturnOperation (OperationKind.Return, Type: null, IsInvalid, IsImplicit) (Syntax: 'Function() 1')
          ReturnedValue: 
            ILocalReferenceOperation:  (OperationKind.LocalReference, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'Function() 1')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36754: 'Func(Of Integer)' cannot be converted to 'Func(Of String)' because 'Integer' is not derived from 'String', as required for the 'Out' generic parameter 'TResult' in 'Delegate Function Func(Of Out TResult)() As TResult'.
        Dim a As Func(Of String) = DirectCast(Function() 1, Func(Of Integer))'BIND:"DirectCast(Function() 1, Func(Of Integer))"
                                   ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of DirectCastExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_DirectCastLambdaConversion_ReturnRelaxation()
            Dim source = <![CDATA[
Option Strict On
Imports System

Module Program
    Sub Main()
        Dim a As Func(Of Object) = DirectCast(Function() 1, Func(Of Object))'BIND:"DirectCast(Function() 1, Func(Of Object))"
    End Sub
End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Object)) (Syntax: 'DirectCast( ... Of Object))')
  Target: 
    IAnonymousFunctionOperation (Symbol: Function () As System.Object) (OperationKind.AnonymousFunction, Type: null) (Syntax: 'Function() 1')
      IBlockOperation (3 statements, 1 locals) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Function() 1')
        Locals: Local_1: <anonymous local> As System.Object
        IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: '1')
          ReturnedValue: 
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: '1')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Operand: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'Function() 1')
          Statement: 
            null
        IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'Function() 1')
          ReturnedValue: 
            ILocalReferenceOperation:  (OperationKind.LocalReference, Type: System.Object, IsImplicit) (Syntax: 'Function() 1')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of DirectCastExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_DirectCastLambdaConversion_ArgumentRelaxation()
            Dim source = <![CDATA[
Option Strict On
Imports System

Module Program
    Sub Main()
        Dim a As Action(Of Object) = DirectCast(Sub() Console.WriteLine(), Action(Of Object))'BIND:"DirectCast(Sub() Console.WriteLine(), Action(Of Object))"
    End Sub
End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action(Of System.Object)) (Syntax: 'DirectCast( ... Of Object))')
  Target: 
    IAnonymousFunctionOperation (Symbol: Sub ()) (OperationKind.AnonymousFunction, Type: null) (Syntax: 'Sub() Conso ... WriteLine()')
      IBlockOperation (3 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Sub() Conso ... WriteLine()')
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.WriteLine()')
          Expression: 
            IInvocationOperation (Sub System.Console.WriteLine()) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.WriteLine()')
              Instance Receiver: 
                null
              Arguments(0)
        ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'Sub() Conso ... WriteLine()')
          Statement: 
            null
        IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'Sub() Conso ... WriteLine()')
          ReturnedValue: 
            null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of DirectCastExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_TryCastLambdaConversion()
            Dim source = <![CDATA[
Option Strict On
Imports System

Module Program
    Sub Main()
        Dim a As Action = TryCast(Sub() Console.WriteLine(), Action)'BIND:"TryCast(Sub() Console.WriteLine(), Action)"
    End Sub
End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action) (Syntax: 'TryCast(Sub ... (), Action)')
  Target: 
    IAnonymousFunctionOperation (Symbol: Sub ()) (OperationKind.AnonymousFunction, Type: null) (Syntax: 'Sub() Conso ... WriteLine()')
      IBlockOperation (3 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Sub() Conso ... WriteLine()')
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.WriteLine()')
          Expression: 
            IInvocationOperation (Sub System.Console.WriteLine()) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.WriteLine()')
              Instance Receiver: 
                null
              Arguments(0)
        ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'Sub() Conso ... WriteLine()')
          Statement: 
            null
        IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'Sub() Conso ... WriteLine()')
          ReturnedValue: 
            null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of TryCastExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_TryCastLambdaConversion_DisallowedArgumentType()
            Dim source = <![CDATA[
Option Strict On
Imports System

Module Program
    Sub Main()
        Dim a As Action = TryCast(Sub(i As Integer) Console.WriteLine(), Action)'BIND:"TryCast(Sub(i As Integer) Console.WriteLine(), Action)"
    End Sub
End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action, IsInvalid) (Syntax: 'TryCast(Sub ... (), Action)')
  Target: 
    IAnonymousFunctionOperation (Symbol: Sub (i As System.Int32)) (OperationKind.AnonymousFunction, Type: null, IsInvalid) (Syntax: 'Sub(i As In ... WriteLine()')
      IBlockOperation (3 statements) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'Sub(i As In ... WriteLine()')
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'Console.WriteLine()')
          Expression: 
            IInvocationOperation (Sub System.Console.WriteLine()) (OperationKind.Invocation, Type: System.Void, IsInvalid) (Syntax: 'Console.WriteLine()')
              Instance Receiver: 
                null
              Arguments(0)
        ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsInvalid, IsImplicit) (Syntax: 'Sub(i As In ... WriteLine()')
          Statement: 
            null
        IReturnOperation (OperationKind.Return, Type: null, IsInvalid, IsImplicit) (Syntax: 'Sub(i As In ... WriteLine()')
          ReturnedValue: 
            null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36670: Nested sub does not have a signature that is compatible with delegate 'Action'.
        Dim a As Action = TryCast(Sub(i As Integer) Console.WriteLine(), Action)'BIND:"TryCast(Sub(i As Integer) Console.WriteLine(), Action)"
                                  ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of TryCastExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_TryCastLambdaConversion_InvalidArgumentType()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module M1
    Class C1
    End Class
    Sub Method1()
        Dim a As Action(Of String) = TryCast(Sub(c1 As C1) Console.WriteLine(""), Action(Of String))'BIND:"TryCast(Sub(c1 As C1) Console.WriteLine(""), Action(Of String))"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action(Of System.String), IsInvalid) (Syntax: 'TryCast(Sub ... Of String))')
  Target: 
    IAnonymousFunctionOperation (Symbol: Sub (c1 As M1.C1)) (OperationKind.AnonymousFunction, Type: null, IsInvalid) (Syntax: 'Sub(c1 As C ... iteLine("")')
      IBlockOperation (3 statements) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'Sub(c1 As C ... iteLine("")')
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'Console.WriteLine("")')
          Expression: 
            IInvocationOperation (Sub System.Console.WriteLine(value As System.String)) (OperationKind.Invocation, Type: System.Void, IsInvalid) (Syntax: 'Console.WriteLine("")')
              Instance Receiver: 
                null
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null, IsInvalid) (Syntax: '""')
                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "", IsInvalid) (Syntax: '""')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsInvalid, IsImplicit) (Syntax: 'Sub(c1 As C ... iteLine("")')
          Statement: 
            null
        IReturnOperation (OperationKind.Return, Type: null, IsInvalid, IsImplicit) (Syntax: 'Sub(c1 As C ... iteLine("")')
          ReturnedValue: 
            null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36670: Nested sub does not have a signature that is compatible with delegate 'Action(Of String)'.
        Dim a As Action(Of String) = TryCast(Sub(c1 As C1) Console.WriteLine(""), Action(Of String))'BIND:"TryCast(Sub(c1 As C1) Console.WriteLine(""), Action(Of String))"
                                             ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of TryCastExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_TryCastLambdaConversion_DisallowedReturnType()
            Dim source = <![CDATA[
Option Strict On
Imports System

Module Program
    Sub Main()
        Dim a As Func(Of Object) = TryCast(Sub() Console.WriteLine(), Func(Of Object))'BIND:"TryCast(Sub() Console.WriteLine(), Func(Of Object))"
    End Sub
End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Object), IsInvalid) (Syntax: 'TryCast(Sub ... Of Object))')
  Target: 
    IAnonymousFunctionOperation (Symbol: Sub ()) (OperationKind.AnonymousFunction, Type: null, IsInvalid) (Syntax: 'Sub() Conso ... WriteLine()')
      IBlockOperation (3 statements) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'Sub() Conso ... WriteLine()')
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'Console.WriteLine()')
          Expression: 
            IInvocationOperation (Sub System.Console.WriteLine()) (OperationKind.Invocation, Type: System.Void, IsInvalid) (Syntax: 'Console.WriteLine()')
              Instance Receiver: 
                null
              Arguments(0)
        ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsInvalid, IsImplicit) (Syntax: 'Sub() Conso ... WriteLine()')
          Statement: 
            null
        IReturnOperation (OperationKind.Return, Type: null, IsInvalid, IsImplicit) (Syntax: 'Sub() Conso ... WriteLine()')
          ReturnedValue: 
            null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36670: Nested sub does not have a signature that is compatible with delegate 'Func(Of Object)'.
        Dim a As Func(Of Object) = TryCast(Sub() Console.WriteLine(), Func(Of Object))'BIND:"TryCast(Sub() Console.WriteLine(), Func(Of Object))"
                                           ~~~~~~~~~~~~~~~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of TryCastExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_TryCastLambdaConversion_InvalidReturnType()
            Dim source = <![CDATA[
Option Strict On
Imports System

Module Program
    Sub Main()
        Dim a As Func(Of Object) = TryCast(Function() New NonExistant(), Func(Of Object)) 'BIND:"TryCast(Function() New NonExistant(), Func(Of Object))"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Object), IsInvalid) (Syntax: 'TryCast(Fun ... Of Object))')
  Target: 
    IAnonymousFunctionOperation (Symbol: Function () As System.Object) (OperationKind.AnonymousFunction, Type: null, IsInvalid) (Syntax: 'Function()  ... nExistant()')
      IBlockOperation (3 statements, 1 locals) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'Function()  ... nExistant()')
        Locals: Local_1: <anonymous local> As System.Object
        IReturnOperation (OperationKind.Return, Type: null, IsInvalid, IsImplicit) (Syntax: 'New NonExistant()')
          ReturnedValue: 
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsInvalid, IsImplicit) (Syntax: 'New NonExistant()')
              Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Operand: 
                IInvalidOperation (OperationKind.Invalid, Type: NonExistant, IsInvalid) (Syntax: 'New NonExistant()')
                  Children(0)
        ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsInvalid, IsImplicit) (Syntax: 'Function()  ... nExistant()')
          Statement: 
            null
        IReturnOperation (OperationKind.Return, Type: null, IsInvalid, IsImplicit) (Syntax: 'Function()  ... nExistant()')
          ReturnedValue: 
            ILocalReferenceOperation:  (OperationKind.LocalReference, Type: System.Object, IsInvalid, IsImplicit) (Syntax: 'Function()  ... nExistant()')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30002: Type 'NonExistant' is not defined.
        Dim a As Func(Of Object) = TryCast(Function() New NonExistant(), Func(Of Object)) 'BIND:"TryCast(Function() New NonExistant(), Func(Of Object))"
                                                          ~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of TryCastExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_TryCastLambdaConversion_InvalidVariableType()
            Dim source = <![CDATA[
Option Strict On
Imports System

Module Program
    Sub Main()
        Dim a As Action(Of String) = TryCast(Sub(i As Integer) Console.WriteLine(), Action(Of Integer))'BIND:"TryCast(Sub(i As Integer) Console.WriteLine(), Action(Of Integer))"
    End Sub
End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action(Of System.Int32), IsInvalid) (Syntax: 'TryCast(Sub ... f Integer))')
  Target: 
    IAnonymousFunctionOperation (Symbol: Sub (i As System.Int32)) (OperationKind.AnonymousFunction, Type: null, IsInvalid) (Syntax: 'Sub(i As In ... WriteLine()')
      IBlockOperation (3 statements) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'Sub(i As In ... WriteLine()')
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'Console.WriteLine()')
          Expression: 
            IInvocationOperation (Sub System.Console.WriteLine()) (OperationKind.Invocation, Type: System.Void, IsInvalid) (Syntax: 'Console.WriteLine()')
              Instance Receiver: 
                null
              Arguments(0)
        ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsInvalid, IsImplicit) (Syntax: 'Sub(i As In ... WriteLine()')
          Statement: 
            null
        IReturnOperation (OperationKind.Return, Type: null, IsInvalid, IsImplicit) (Syntax: 'Sub(i As In ... WriteLine()')
          ReturnedValue: 
            null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36755: 'Action(Of Integer)' cannot be converted to 'Action(Of String)' because 'String' is not derived from 'Integer', as required for the 'In' generic parameter 'T' in 'Delegate Sub Action(Of In T)(obj As T)'.
        Dim a As Action(Of String) = TryCast(Sub(i As Integer) Console.WriteLine(), Action(Of Integer))'BIND:"TryCast(Sub(i As Integer) Console.WriteLine(), Action(Of Integer))"
                                     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of TryCastExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_TryCastLambdaConversion_ArgumentRelaxation()
            Dim source = <![CDATA[
Option Strict On
Imports System

Module Program
    Sub Main()
        Dim a As Action(Of Object) = TryCast(Sub() Console.WriteLine(), Action(Of Object))'BIND:"TryCast(Sub() Console.WriteLine(), Action(Of Object))"
    End Sub
End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action(Of System.Object)) (Syntax: 'TryCast(Sub ... Of Object))')
  Target: 
    IAnonymousFunctionOperation (Symbol: Sub ()) (OperationKind.AnonymousFunction, Type: null) (Syntax: 'Sub() Conso ... WriteLine()')
      IBlockOperation (3 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Sub() Conso ... WriteLine()')
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.WriteLine()')
          Expression: 
            IInvocationOperation (Sub System.Console.WriteLine()) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.WriteLine()')
              Instance Receiver: 
                null
              Arguments(0)
        ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'Sub() Conso ... WriteLine()')
          Statement: 
            null
        IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'Sub() Conso ... WriteLine()')
          ReturnedValue: 
            null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of TryCastExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_TryCastLambdaConversion_ReturnRelaxation()
            Dim source = <![CDATA[
Option Strict On
Imports System

Module Program
    Sub Main()
        Dim a As Action = TryCast(Function() 1, Action)'BIND:"TryCast(Function() 1, Action)"
    End Sub
End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action) (Syntax: 'TryCast(Fun ...  1, Action)')
  Target: 
    IAnonymousFunctionOperation (Symbol: Function () As System.Int32) (OperationKind.AnonymousFunction, Type: null) (Syntax: 'Function() 1')
      IBlockOperation (3 statements, 1 locals) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Function() 1')
        Locals: Local_1: <anonymous local> As System.Int32
        IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: '1')
          ReturnedValue: 
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'Function() 1')
          Statement: 
            null
        IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'Function() 1')
          ReturnedValue: 
            ILocalReferenceOperation:  (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'Function() 1')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of TryCastExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
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
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action) (Syntax: 'New Action( ... riteLine())')
  Target: 
    IAnonymousFunctionOperation (Symbol: Sub ()) (OperationKind.AnonymousFunction, Type: null) (Syntax: 'Sub() Conso ... WriteLine()')
      IBlockOperation (3 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Sub() Conso ... WriteLine()')
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.WriteLine()')
          Expression: 
            IInvocationOperation (Sub System.Console.WriteLine()) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.WriteLine()')
              Instance Receiver: 
                null
              Arguments(0)
        ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'Sub() Conso ... WriteLine()')
          Statement: 
            null
        IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'Sub() Conso ... WriteLine()')
          ReturnedValue: 
            null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_DelegateCreationLambdaArgument_MultipleArgumentsToConstructor()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module M1
    Sub Method1()
        Dim a As Action = New Action(Sub() Console.WriteLine(), 1)'BIND:"Dim a As Action = New Action(Sub() Console.WriteLine(), 1)"
    End Sub

End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'Dim a As Ac ... eLine(), 1)')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'a As Action ... eLine(), 1)')
    Declarators:
        IVariableDeclaratorOperation (Symbol: a As System.Action) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'a')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= New Actio ... eLine(), 1)')
        IInvalidOperation (OperationKind.Invalid, Type: System.Action, IsInvalid) (Syntax: 'New Action( ... eLine(), 1)')
          Children(2):
              IDelegateCreationOperation (OperationKind.DelegateCreation, Type: Sub <generated method>(), IsInvalid, IsImplicit) (Syntax: 'Sub() Conso ... WriteLine()')
                Target: 
                  IAnonymousFunctionOperation (Symbol: Sub ()) (OperationKind.AnonymousFunction, Type: null, IsInvalid) (Syntax: 'Sub() Conso ... WriteLine()')
                    IBlockOperation (3 statements) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'Sub() Conso ... WriteLine()')
                      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'Console.WriteLine()')
                        Expression: 
                          IInvocationOperation (Sub System.Console.WriteLine()) (OperationKind.Invocation, Type: System.Void, IsInvalid) (Syntax: 'Console.WriteLine()')
                            Instance Receiver: 
                              null
                            Arguments(0)
                      ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsInvalid, IsImplicit) (Syntax: 'Sub() Conso ... WriteLine()')
                        Statement: 
                          null
                      IReturnOperation (OperationKind.Return, Type: null, IsInvalid, IsImplicit) (Syntax: 'Sub() Conso ... WriteLine()')
                        ReturnedValue: 
                          null
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC32008: Delegate 'Action' requires an 'AddressOf' expression or lambda expression as the only argument to its constructor.
        Dim a As Action = New Action(Sub() Console.WriteLine(), 1)'BIND:"Dim a As Action = New Action(Sub() Console.WriteLine(), 1)"
                                    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
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
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action(Of System.String)) (Syntax: 'New Action( ... riteLine())')
  Target: 
    IAnonymousFunctionOperation (Symbol: Sub ()) (OperationKind.AnonymousFunction, Type: null) (Syntax: 'Sub() Conso ... WriteLine()')
      IBlockOperation (3 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Sub() Conso ... WriteLine()')
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.WriteLine()')
          Expression: 
            IInvocationOperation (Sub System.Console.WriteLine()) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.WriteLine()')
              Instance Receiver: 
                null
              Arguments(0)
        ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'Sub() Conso ... WriteLine()')
          Statement: 
            null
        IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'Sub() Conso ... WriteLine()')
          ReturnedValue: 
            null
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
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action) (Syntax: 'New Action(Function() 1)')
  Target: 
    IAnonymousFunctionOperation (Symbol: Function () As System.Int32) (OperationKind.AnonymousFunction, Type: null) (Syntax: 'Function() 1')
      IBlockOperation (3 statements, 1 locals) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Function() 1')
        Locals: Local_1: <anonymous local> As System.Int32
        IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: '1')
          ReturnedValue: 
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'Function() 1')
          Statement: 
            null
        IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'Function() 1')
          ReturnedValue: 
            ILocalReferenceOperation:  (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'Function() 1')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_DelegateCreationLambdaArgument_DisallowedArgumentType()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module M1
    Sub Method1()
        Dim a As Action(Of Object) = New Action(Of Object)(Sub(i As Integer) Console.WriteLine())'BIND:"New Action(Of Object)(Sub(i As Integer) Console.WriteLine())"
    End Sub

End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action(Of System.Object), IsInvalid) (Syntax: 'New Action( ... riteLine())')
  Target: 
    IAnonymousFunctionOperation (Symbol: Sub (i As System.Int32)) (OperationKind.AnonymousFunction, Type: null, IsInvalid) (Syntax: 'Sub(i As In ... WriteLine()')
      IBlockOperation (3 statements) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'Sub(i As In ... WriteLine()')
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'Console.WriteLine()')
          Expression: 
            IInvocationOperation (Sub System.Console.WriteLine()) (OperationKind.Invocation, Type: System.Void, IsInvalid) (Syntax: 'Console.WriteLine()')
              Instance Receiver: 
                null
              Arguments(0)
        ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsInvalid, IsImplicit) (Syntax: 'Sub(i As In ... WriteLine()')
          Statement: 
            null
        IReturnOperation (OperationKind.Return, Type: null, IsInvalid, IsImplicit) (Syntax: 'Sub(i As In ... WriteLine()')
          ReturnedValue: 
            null
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
        Public Sub DelegateCreationExpression_DelegateCreationLambdaArgument_InvalidArgumentType()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module M1
    Class C1
    End Class
    Sub Method1()
        Dim a As Action(Of String) = New Action(Of String)(Sub(c1 As C1) Console.WriteLine(""))'BIND:"New Action(Of String)(Sub(c1 As C1) Console.WriteLine(""))"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action(Of System.String), IsInvalid) (Syntax: 'New Action( ... teLine(""))')
  Target: 
    IAnonymousFunctionOperation (Symbol: Sub (c1 As M1.C1)) (OperationKind.AnonymousFunction, Type: null, IsInvalid) (Syntax: 'Sub(c1 As C ... iteLine("")')
      IBlockOperation (3 statements) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'Sub(c1 As C ... iteLine("")')
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'Console.WriteLine("")')
          Expression: 
            IInvocationOperation (Sub System.Console.WriteLine(value As System.String)) (OperationKind.Invocation, Type: System.Void, IsInvalid) (Syntax: 'Console.WriteLine("")')
              Instance Receiver: 
                null
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null, IsInvalid) (Syntax: '""')
                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "", IsInvalid) (Syntax: '""')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsInvalid, IsImplicit) (Syntax: 'Sub(c1 As C ... iteLine("")')
          Statement: 
            null
        IReturnOperation (OperationKind.Return, Type: null, IsInvalid, IsImplicit) (Syntax: 'Sub(c1 As C ... iteLine("")')
          ReturnedValue: 
            null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36670: Nested sub does not have a signature that is compatible with delegate 'Action(Of String)'.
        Dim a As Action(Of String) = New Action(Of String)(Sub(c1 As C1) Console.WriteLine(""))'BIND:"New Action(Of String)(Sub(c1 As C1) Console.WriteLine(""))"
                                                           ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_DelegateCreationLambdaArgument_DisallowedReturnType()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module M1
    Sub Method1()
        Dim a As Func(Of Object) = New Func(Of Object)(Sub() Console.WriteLine())'BIND:"New Func(Of Object)(Sub() Console.WriteLine())"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Object), IsInvalid) (Syntax: 'New Func(Of ... riteLine())')
  Target: 
    IAnonymousFunctionOperation (Symbol: Sub ()) (OperationKind.AnonymousFunction, Type: null, IsInvalid) (Syntax: 'Sub() Conso ... WriteLine()')
      IBlockOperation (3 statements) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'Sub() Conso ... WriteLine()')
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'Console.WriteLine()')
          Expression: 
            IInvocationOperation (Sub System.Console.WriteLine()) (OperationKind.Invocation, Type: System.Void, IsInvalid) (Syntax: 'Console.WriteLine()')
              Instance Receiver: 
                null
              Arguments(0)
        ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsInvalid, IsImplicit) (Syntax: 'Sub() Conso ... WriteLine()')
          Statement: 
            null
        IReturnOperation (OperationKind.Return, Type: null, IsInvalid, IsImplicit) (Syntax: 'Sub() Conso ... WriteLine()')
          ReturnedValue: 
            null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36670: Nested sub does not have a signature that is compatible with delegate 'Func(Of Object)'.
        Dim a As Func(Of Object) = New Func(Of Object)(Sub() Console.WriteLine())'BIND:"New Func(Of Object)(Sub() Console.WriteLine())"
                                                       ~~~~~~~~~~~~~~~~~~~~~~~~~
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
        Dim a As Func(Of Object) = New Func(Of Object)(Function() New NonExistant())'BIND:"New Func(Of Object)(Function() New NonExistant())"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Object), IsInvalid) (Syntax: 'New Func(Of ... Existant())')
  Target: 
    IAnonymousFunctionOperation (Symbol: Function () As System.Object) (OperationKind.AnonymousFunction, Type: null, IsInvalid) (Syntax: 'Function()  ... nExistant()')
      IBlockOperation (3 statements, 1 locals) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'Function()  ... nExistant()')
        Locals: Local_1: <anonymous local> As System.Object
        IReturnOperation (OperationKind.Return, Type: null, IsInvalid, IsImplicit) (Syntax: 'New NonExistant()')
          ReturnedValue: 
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsInvalid, IsImplicit) (Syntax: 'New NonExistant()')
              Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Operand: 
                IInvalidOperation (OperationKind.Invalid, Type: NonExistant, IsInvalid) (Syntax: 'New NonExistant()')
                  Children(0)
        ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsInvalid, IsImplicit) (Syntax: 'Function()  ... nExistant()')
          Statement: 
            null
        IReturnOperation (OperationKind.Return, Type: null, IsInvalid, IsImplicit) (Syntax: 'Function()  ... nExistant()')
          ReturnedValue: 
            ILocalReferenceOperation:  (OperationKind.LocalReference, Type: System.Object, IsInvalid, IsImplicit) (Syntax: 'Function()  ... nExistant()')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30002: Type 'NonExistant' is not defined.
        Dim a As Func(Of Object) = New Func(Of Object)(Function() New NonExistant())'BIND:"New Func(Of Object)(Function() New NonExistant())"
                                                                      ~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_ParenthesizedLambda_CType()
            Dim source = <![CDATA[
Imports System

Public Class C
    Public Sub M1()
        CType(((Sub() Console.WriteLine())), Action)'BIND:"CType(((Sub() Console.WriteLine())), Action)"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action) (Syntax: 'CType(((Sub ... )), Action)')
  Target: 
    IParenthesizedOperation (OperationKind.Parenthesized, Type: null) (Syntax: '((Sub() Con ... iteLine()))')
      Operand: 
        IParenthesizedOperation (OperationKind.Parenthesized, Type: null) (Syntax: '(Sub() Cons ... riteLine())')
          Operand: 
            IAnonymousFunctionOperation (Symbol: Sub ()) (OperationKind.AnonymousFunction, Type: null) (Syntax: 'Sub() Conso ... WriteLine()')
              IBlockOperation (3 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Sub() Conso ... WriteLine()')
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.WriteLine()')
                  Expression: 
                    IInvocationOperation (Sub System.Console.WriteLine()) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.WriteLine()')
                      Instance Receiver: 
                        null
                      Arguments(0)
                ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'Sub() Conso ... WriteLine()')
                  Statement: 
                    null
                IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'Sub() Conso ... WriteLine()')
                  ReturnedValue: 
                    null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of CTypeExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_ParenthesizedLambda_CType_Multiline()
            Dim source = <![CDATA[
Imports System

Public Class C
    Public Sub M1()
        CType(((Sub()'BIND:"CType(((Sub()"
                End Sub)), Action)
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action) (Syntax: 'CType(((Sub ... )), Action)')
  Target: 
    IParenthesizedOperation (OperationKind.Parenthesized, Type: null) (Syntax: '((Sub()'BIN ... End Sub))')
      Operand: 
        IParenthesizedOperation (OperationKind.Parenthesized, Type: null) (Syntax: '(Sub()'BIND ... End Sub)')
          Operand: 
            IAnonymousFunctionOperation (Symbol: Sub ()) (OperationKind.AnonymousFunction, Type: null) (Syntax: 'Sub()'BIND: ... End Sub')
              IBlockOperation (2 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Sub()'BIND: ... End Sub')
                ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'End Sub')
                  Statement: 
                    null
                IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'End Sub')
                  ReturnedValue: 
                    null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of CTypeExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_ParenthesizedLambda_Implicit()
            Dim source = <![CDATA[
Imports System

Public Class C
    Public Sub M1()
        Dim a As Action = ((Sub() Console.WriteLine()))'BIND:"= ((Sub() Console.WriteLine()))"
    End Sub

    Public Sub M2()
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= ((Sub() C ... iteLine()))')
  IParenthesizedOperation (OperationKind.Parenthesized, Type: System.Action) (Syntax: '((Sub() Con ... iteLine()))')
    Operand: 
      IParenthesizedOperation (OperationKind.Parenthesized, Type: System.Action) (Syntax: '(Sub() Cons ... riteLine())')
        Operand: 
          IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action, IsImplicit) (Syntax: 'Sub() Conso ... WriteLine()')
            Target: 
              IAnonymousFunctionOperation (Symbol: Sub ()) (OperationKind.AnonymousFunction, Type: null) (Syntax: 'Sub() Conso ... WriteLine()')
                IBlockOperation (3 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Sub() Conso ... WriteLine()')
                  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.WriteLine()')
                    Expression: 
                      IInvocationOperation (Sub System.Console.WriteLine()) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.WriteLine()')
                        Instance Receiver: 
                          null
                        Arguments(0)
                  ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'Sub() Conso ... WriteLine()')
                    Statement: 
                      null
                  IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'Sub() Conso ... WriteLine()')
                    ReturnedValue: 
                      null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of EqualsValueSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_ParenthesizedLambda_CType_InvalidMissingParameter()
            Dim source = <![CDATA[
Imports System

Public Class C
    Public Sub M1()
        CType(((Sub()'BIND:"CType(((Sub()"
                End Sub)), Action(Of String))
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action(Of System.String), IsInvalid) (Syntax: 'CType(((Sub ... Of String))')
  Target: 
    IParenthesizedOperation (OperationKind.Parenthesized, Type: null, IsInvalid) (Syntax: '((Sub()'BIN ... End Sub))')
      Operand: 
        IParenthesizedOperation (OperationKind.Parenthesized, Type: null, IsInvalid) (Syntax: '(Sub()'BIND ... End Sub)')
          Operand: 
            IAnonymousFunctionOperation (Symbol: Sub ()) (OperationKind.AnonymousFunction, Type: null, IsInvalid) (Syntax: 'Sub()'BIND: ... End Sub')
              IBlockOperation (2 statements) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'Sub()'BIND: ... End Sub')
                ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsInvalid, IsImplicit) (Syntax: 'End Sub')
                  Statement: 
                    null
                IReturnOperation (OperationKind.Return, Type: null, IsInvalid, IsImplicit) (Syntax: 'End Sub')
                  ReturnedValue: 
                    null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30455: Argument not specified for parameter 'obj' of 'Action(Of String)'.
        CType(((Sub()'BIND:"CType(((Sub()"
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of CTypeExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_ParenthesizedLambda_CType_InvalidConversion()
            Dim source = <![CDATA[
Imports System

Public Class C
    Public Sub M1()
        CType(((Sub()'BIND:"CType(((Sub()"
                End Sub)), Func(Of String))
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.String), IsInvalid) (Syntax: 'CType(((Sub ... Of String))')
  Target: 
    IParenthesizedOperation (OperationKind.Parenthesized, Type: null, IsInvalid) (Syntax: '((Sub()'BIN ... End Sub))')
      Operand: 
        IParenthesizedOperation (OperationKind.Parenthesized, Type: null, IsInvalid) (Syntax: '(Sub()'BIND ... End Sub)')
          Operand: 
            IAnonymousFunctionOperation (Symbol: Sub ()) (OperationKind.AnonymousFunction, Type: null, IsInvalid) (Syntax: 'Sub()'BIND: ... End Sub')
              IBlockOperation (2 statements) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'Sub()'BIND: ... End Sub')
                ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsInvalid, IsImplicit) (Syntax: 'End Sub')
                  Statement: 
                    null
                IReturnOperation (OperationKind.Return, Type: null, IsInvalid, IsImplicit) (Syntax: 'End Sub')
                  ReturnedValue: 
                    null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36670: Nested sub does not have a signature that is compatible with delegate 'Func(Of String)'.
        CType(((Sub()'BIND:"CType(((Sub()"
                ~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of CTypeExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_ParenthesizedLambda_CType_NonDelegateTargetType_SuccessfulConversion()
            Dim source = <![CDATA[
Imports System

Public Class C
    Public Sub M1()
        Dim a = CType(((Sub() Console.WriteLine())), Object)'BIND:"CType(((Sub() Console.WriteLine())), Object)"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object) (Syntax: 'CType(((Sub ... )), Object)')
  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
  Operand: 
    IParenthesizedOperation (OperationKind.Parenthesized, Type: Sub <generated method>()) (Syntax: '((Sub() Con ... iteLine()))')
      Operand: 
        IParenthesizedOperation (OperationKind.Parenthesized, Type: Sub <generated method>()) (Syntax: '(Sub() Cons ... riteLine())')
          Operand: 
            IDelegateCreationOperation (OperationKind.DelegateCreation, Type: Sub <generated method>(), IsImplicit) (Syntax: 'Sub() Conso ... WriteLine()')
              Target: 
                IAnonymousFunctionOperation (Symbol: Sub ()) (OperationKind.AnonymousFunction, Type: null) (Syntax: 'Sub() Conso ... WriteLine()')
                  IBlockOperation (3 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Sub() Conso ... WriteLine()')
                    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.WriteLine()')
                      Expression: 
                        IInvocationOperation (Sub System.Console.WriteLine()) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.WriteLine()')
                          Instance Receiver: 
                            null
                          Arguments(0)
                    ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'Sub() Conso ... WriteLine()')
                      Statement: 
                        null
                    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'Sub() Conso ... WriteLine()')
                      ReturnedValue: 
                        null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of CTypeExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_ParenthesizedLambda_CType_NestedCTypeNonDelegateTargetType_SuccessfulConversion()
            Dim source = <![CDATA[
Imports System

Public Class C
    Public Sub M1()
        Dim a = CType(((CType(Sub() Console.WriteLine(), Action))), Object)'BIND:"CType(((CType(Sub() Console.WriteLine(), Action))), Object)"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object) (Syntax: 'CType(((CTy ... )), Object)')
  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
  Operand: 
    IParenthesizedOperation (OperationKind.Parenthesized, Type: System.Action) (Syntax: '((CType(Sub ... , Action)))')
      Operand: 
        IParenthesizedOperation (OperationKind.Parenthesized, Type: System.Action) (Syntax: '(CType(Sub( ... ), Action))')
          Operand: 
            IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action) (Syntax: 'CType(Sub() ... (), Action)')
              Target: 
                IAnonymousFunctionOperation (Symbol: Sub ()) (OperationKind.AnonymousFunction, Type: null) (Syntax: 'Sub() Conso ... WriteLine()')
                  IBlockOperation (3 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Sub() Conso ... WriteLine()')
                    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.WriteLine()')
                      Expression: 
                        IInvocationOperation (Sub System.Console.WriteLine()) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.WriteLine()')
                          Instance Receiver: 
                            null
                          Arguments(0)
                    ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'Sub() Conso ... WriteLine()')
                      Statement: 
                        null
                    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'Sub() Conso ... WriteLine()')
                      ReturnedValue: 
                        null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of CTypeExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_ParenthesizedLambda_DirectCast_NonDelegateTargetType_SuccessfulConversion()
            Dim source = <![CDATA[
Imports System

Public Class C
    Public Sub M1()
        Dim a = DirectCast(((Sub() Console.WriteLine())), Object)'BIND:"DirectCast(((Sub() Console.WriteLine())), Object)"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object) (Syntax: 'DirectCast( ... )), Object)')
  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
  Operand: 
    IParenthesizedOperation (OperationKind.Parenthesized, Type: Sub <generated method>()) (Syntax: '((Sub() Con ... iteLine()))')
      Operand: 
        IParenthesizedOperation (OperationKind.Parenthesized, Type: Sub <generated method>()) (Syntax: '(Sub() Cons ... riteLine())')
          Operand: 
            IDelegateCreationOperation (OperationKind.DelegateCreation, Type: Sub <generated method>(), IsImplicit) (Syntax: 'Sub() Conso ... WriteLine()')
              Target: 
                IAnonymousFunctionOperation (Symbol: Sub ()) (OperationKind.AnonymousFunction, Type: null) (Syntax: 'Sub() Conso ... WriteLine()')
                  IBlockOperation (3 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Sub() Conso ... WriteLine()')
                    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.WriteLine()')
                      Expression: 
                        IInvocationOperation (Sub System.Console.WriteLine()) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.WriteLine()')
                          Instance Receiver: 
                            null
                          Arguments(0)
                    ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'Sub() Conso ... WriteLine()')
                      Statement: 
                        null
                    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'Sub() Conso ... WriteLine()')
                      ReturnedValue: 
                        null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of DirectCastExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_ParenthesizedLambda_DirectCast_NestedDirectCastNonDelegateTargetType_SuccessfulConversion()
            Dim source = <![CDATA[
Imports System

Public Class C
    Public Sub M1()
        Dim a = DirectCast(((DirectCast(Sub() Console.WriteLine(), Action))), Object)'BIND:"DirectCast(((DirectCast(Sub() Console.WriteLine(), Action))), Object)"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object) (Syntax: 'DirectCast( ... )), Object)')
  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
  Operand: 
    IParenthesizedOperation (OperationKind.Parenthesized, Type: System.Action) (Syntax: '((DirectCas ... , Action)))')
      Operand: 
        IParenthesizedOperation (OperationKind.Parenthesized, Type: System.Action) (Syntax: '(DirectCast ... ), Action))')
          Operand: 
            IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action) (Syntax: 'DirectCast( ... (), Action)')
              Target: 
                IAnonymousFunctionOperation (Symbol: Sub ()) (OperationKind.AnonymousFunction, Type: null) (Syntax: 'Sub() Conso ... WriteLine()')
                  IBlockOperation (3 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Sub() Conso ... WriteLine()')
                    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.WriteLine()')
                      Expression: 
                        IInvocationOperation (Sub System.Console.WriteLine()) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.WriteLine()')
                          Instance Receiver: 
                            null
                          Arguments(0)
                    ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'Sub() Conso ... WriteLine()')
                      Statement: 
                        null
                    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'Sub() Conso ... WriteLine()')
                      ReturnedValue: 
                        null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of DirectCastExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_ParenthesizedLambda_TryCast_NonDelegateTargetType_SuccessfulConversion()
            Dim source = <![CDATA[
Imports System

Public Class C
    Public Sub M1()
        Dim a = TryCast(((Sub() Console.WriteLine())), Object)'BIND:"TryCast(((Sub() Console.WriteLine())), Object)"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IConversionOperation (TryCast: True, Unchecked) (OperationKind.Conversion, Type: System.Object) (Syntax: 'TryCast(((S ... )), Object)')
  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
  Operand: 
    IParenthesizedOperation (OperationKind.Parenthesized, Type: Sub <generated method>()) (Syntax: '((Sub() Con ... iteLine()))')
      Operand: 
        IParenthesizedOperation (OperationKind.Parenthesized, Type: Sub <generated method>()) (Syntax: '(Sub() Cons ... riteLine())')
          Operand: 
            IDelegateCreationOperation (OperationKind.DelegateCreation, Type: Sub <generated method>(), IsImplicit) (Syntax: 'Sub() Conso ... WriteLine()')
              Target: 
                IAnonymousFunctionOperation (Symbol: Sub ()) (OperationKind.AnonymousFunction, Type: null) (Syntax: 'Sub() Conso ... WriteLine()')
                  IBlockOperation (3 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Sub() Conso ... WriteLine()')
                    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.WriteLine()')
                      Expression: 
                        IInvocationOperation (Sub System.Console.WriteLine()) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.WriteLine()')
                          Instance Receiver: 
                            null
                          Arguments(0)
                    ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'Sub() Conso ... WriteLine()')
                      Statement: 
                        null
                    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'Sub() Conso ... WriteLine()')
                      ReturnedValue: 
                        null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of TryCastExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_ParenthesizedLambda_TryCast_NestedTryCastNonDelegateTargetType_SuccessfulConversion()
            Dim source = <![CDATA[
Imports System

Public Class C
    Public Sub M1()
        Dim a = TryCast(((TryCast(Sub() Console.WriteLine(), Action))), Object)'BIND:"TryCast(((TryCast(Sub() Console.WriteLine(), Action))), Object)"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IConversionOperation (TryCast: True, Unchecked) (OperationKind.Conversion, Type: System.Object) (Syntax: 'TryCast(((T ... )), Object)')
  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
  Operand: 
    IParenthesizedOperation (OperationKind.Parenthesized, Type: System.Action) (Syntax: '((TryCast(S ... , Action)))')
      Operand: 
        IParenthesizedOperation (OperationKind.Parenthesized, Type: System.Action) (Syntax: '(TryCast(Su ... ), Action))')
          Operand: 
            IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action) (Syntax: 'TryCast(Sub ... (), Action)')
              Target: 
                IAnonymousFunctionOperation (Symbol: Sub ()) (OperationKind.AnonymousFunction, Type: null) (Syntax: 'Sub() Conso ... WriteLine()')
                  IBlockOperation (3 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Sub() Conso ... WriteLine()')
                    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.WriteLine()')
                      Expression: 
                        IInvocationOperation (Sub System.Console.WriteLine()) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.WriteLine()')
                          Instance Receiver: 
                            null
                          Arguments(0)
                    ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'Sub() Conso ... WriteLine()')
                      Statement: 
                        null
                    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'Sub() Conso ... WriteLine()')
                      ReturnedValue: 
                        null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of TryCastExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_ParenthesizedLambda_Implicit_NonDelegateTargetType_SuccessfulConversion()
            Dim source = <![CDATA[
Imports System

Public Class C
    Public Sub M1()
        Dim a As Object = ((Sub() Console.WriteLine()))'BIND:"= ((Sub() Console.WriteLine()))"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= ((Sub() C ... iteLine()))')
  IParenthesizedOperation (OperationKind.Parenthesized, Type: System.Object) (Syntax: '((Sub() Con ... iteLine()))')
    Operand: 
      IParenthesizedOperation (OperationKind.Parenthesized, Type: System.Object) (Syntax: '(Sub() Cons ... riteLine())')
        Operand: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'Sub() Conso ... WriteLine()')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IDelegateCreationOperation (OperationKind.DelegateCreation, Type: Sub <generated method>(), IsImplicit) (Syntax: 'Sub() Conso ... WriteLine()')
                Target: 
                  IAnonymousFunctionOperation (Symbol: Sub ()) (OperationKind.AnonymousFunction, Type: null) (Syntax: 'Sub() Conso ... WriteLine()')
                    IBlockOperation (3 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Sub() Conso ... WriteLine()')
                      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.WriteLine()')
                        Expression: 
                          IInvocationOperation (Sub System.Console.WriteLine()) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.WriteLine()')
                            Instance Receiver: 
                              null
                            Arguments(0)
                      ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'Sub() Conso ... WriteLine()')
                        Statement: 
                          null
                      IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'Sub() Conso ... WriteLine()')
                        ReturnedValue: 
                          null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of EqualsValueSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_ParenthesizedLambda_CType_InvalidNonDelegateTargetType_InvalidConversion()
            Dim source = <![CDATA[
Imports System

Public Class C
    Public Sub M1()
        Dim a = CType(((Sub() Console.WriteLine())), String)'BIND:"CType(((Sub() Console.WriteLine())), String)"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String, IsInvalid) (Syntax: 'CType(((Sub ... )), String)')
  Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Operand: 
    IParenthesizedOperation (OperationKind.Parenthesized, Type: null, IsInvalid) (Syntax: '((Sub() Con ... iteLine()))')
      Operand: 
        IParenthesizedOperation (OperationKind.Parenthesized, Type: null, IsInvalid) (Syntax: '(Sub() Cons ... riteLine())')
          Operand: 
            IAnonymousFunctionOperation (Symbol: Sub ()) (OperationKind.AnonymousFunction, Type: null, IsInvalid) (Syntax: 'Sub() Conso ... WriteLine()')
              IBlockOperation (3 statements) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'Sub() Conso ... WriteLine()')
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'Console.WriteLine()')
                  Expression: 
                    IInvocationOperation (Sub System.Console.WriteLine()) (OperationKind.Invocation, Type: System.Void, IsInvalid) (Syntax: 'Console.WriteLine()')
                      Instance Receiver: 
                        null
                      Arguments(0)
                ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsInvalid, IsImplicit) (Syntax: 'Sub() Conso ... WriteLine()')
                  Statement: 
                    null
                IReturnOperation (OperationKind.Return, Type: null, IsInvalid, IsImplicit) (Syntax: 'Sub() Conso ... WriteLine()')
                  ReturnedValue: 
                    null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36625: Lambda expression cannot be converted to 'String' because 'String' is not a delegate type.
        Dim a = CType(((Sub() Console.WriteLine())), String)'BIND:"CType(((Sub() Console.WriteLine())), String)"
                        ~~~~~~~~~~~~~~~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of CTypeExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_ParenthesizedLambda_DirectCast()
            Dim source = <![CDATA[
Imports System

Public Class C
    Public Sub M1()
        DirectCast(((Sub()'BIND:"DirectCast(((Sub()"
                End Sub)), Action)
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action) (Syntax: 'DirectCast( ... )), Action)')
  Target: 
    IParenthesizedOperation (OperationKind.Parenthesized, Type: null) (Syntax: '((Sub()'BIN ... End Sub))')
      Operand: 
        IParenthesizedOperation (OperationKind.Parenthesized, Type: null) (Syntax: '(Sub()'BIND ... End Sub)')
          Operand: 
            IAnonymousFunctionOperation (Symbol: Sub ()) (OperationKind.AnonymousFunction, Type: null) (Syntax: 'Sub()'BIND: ... End Sub')
              IBlockOperation (2 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Sub()'BIND: ... End Sub')
                ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'End Sub')
                  Statement: 
                    null
                IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'End Sub')
                  ReturnedValue: 
                    null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of DirectCastExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_ParenthesizedLambda_TryCast()
            Dim source = <![CDATA[
Imports System

Public Class C
    Public Sub M1()
        TryCast(((Sub()'BIND:"TryCast(((Sub()"
                  End Sub)), Action)
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action) (Syntax: 'TryCast(((S ... )), Action)')
  Target: 
    IParenthesizedOperation (OperationKind.Parenthesized, Type: null) (Syntax: '((Sub()'BIN ... End Sub))')
      Operand: 
        IParenthesizedOperation (OperationKind.Parenthesized, Type: null) (Syntax: '(Sub()'BIND ... End Sub)')
          Operand: 
            IAnonymousFunctionOperation (Symbol: Sub ()) (OperationKind.AnonymousFunction, Type: null) (Syntax: 'Sub()'BIND: ... End Sub')
              IBlockOperation (2 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Sub()'BIND: ... End Sub')
                ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'End Sub')
                  Statement: 
                    null
                IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'End Sub')
                  ReturnedValue: 
                    null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of TryCastExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

#End Region

#Region "AddressOf"

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_ImplicitAddressOf()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module M1
    Sub Method1()
        Dim a As Action = AddressOf Method2'BIND:"Dim a As Action = AddressOf Method2"
    End Sub

    Sub Method2()
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim a As Ac ... sOf Method2')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'a As Action ... sOf Method2')
    Declarators:
        IVariableDeclaratorOperation (Symbol: a As System.Action) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'a')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= AddressOf Method2')
        IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action, IsImplicit) (Syntax: 'AddressOf Method2')
          Target: 
            IMethodReferenceOperation: Sub M1.Method2() (Static) (OperationKind.MethodReference, Type: null) (Syntax: 'AddressOf Method2')
              Instance Receiver: 
                null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_ImplicitAddressOf_JustInitializerReturnsOnlyMethodReference()
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
IMethodReferenceOperation: Sub M1.Method2() (Static) (OperationKind.MethodReference, Type: null) (Syntax: 'AddressOf Method2')
  Instance Receiver: 
    null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_ImplicitAddressOf_WithReceiver()
            Dim source = <![CDATA[
Option Strict On
Imports System

Module Program
    Sub M1()
        Dim o As New Object
        Dim a As Action = AddressOf o.ToString'BIND:"Dim a As Action = AddressOf o.ToString"
    End Sub
End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim a As Ac ...  o.ToString')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'a As Action ...  o.ToString')
    Declarators:
        IVariableDeclaratorOperation (Symbol: a As System.Action) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'a')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= AddressOf o.ToString')
        IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action, IsImplicit) (Syntax: 'AddressOf o.ToString')
          Target: 
            IMethodReferenceOperation: Function System.Object.ToString() As System.String (IsVirtual) (OperationKind.MethodReference, Type: null) (Syntax: 'AddressOf o.ToString')
              Instance Receiver: 
                ILocalReferenceOperation: o (OperationKind.LocalReference, Type: System.Object) (Syntax: 'o')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_ImplicitAddressOf_DisallowedArgumentType()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module M1
    Sub Method1()
        Dim a As Action = AddressOf Method2'BIND:"Dim a As Action = AddressOf Method2"
    End Sub

    Sub Method2(i As Integer)
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'Dim a As Ac ... sOf Method2')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'a As Action ... sOf Method2')
    Declarators:
        IVariableDeclaratorOperation (Symbol: a As System.Action) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'a')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= AddressOf Method2')
        IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action, IsInvalid, IsImplicit) (Syntax: 'AddressOf Method2')
          Target: 
            IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'AddressOf Method2')
              Children(1):
                  IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'Method2')
                    Children(1):
                        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: M1, IsInvalid, IsImplicit) (Syntax: 'Method2')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC31143: Method 'Public Sub Method2(i As Integer)' does not have a signature compatible with delegate 'Delegate Sub Action()'.
        Dim a As Action = AddressOf Method2'BIND:"Dim a As Action = AddressOf Method2"
                                    ~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_ImplicitAddressOf_InvalidArgumentType()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module M1
    Class C1
    End Class
    Sub Method1()
        Dim a As Action(Of String) = AddressOf Method2'BIND:"Dim a As Action(Of String) = AddressOf Method2"
    End Sub

    Sub Method2(i As C1)
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'Dim a As Ac ... sOf Method2')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'a As Action ... sOf Method2')
    Declarators:
        IVariableDeclaratorOperation (Symbol: a As System.Action(Of System.String)) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'a')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= AddressOf Method2')
        IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action(Of System.String), IsInvalid, IsImplicit) (Syntax: 'AddressOf Method2')
          Target: 
            IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'AddressOf Method2')
              Children(1):
                  IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'Method2')
                    Children(1):
                        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: M1, IsInvalid, IsImplicit) (Syntax: 'Method2')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC31143: Method 'Public Sub Method2(i As M1.C1)' does not have a signature compatible with delegate 'Delegate Sub Action(Of String)(obj As String)'.
        Dim a As Action(Of String) = AddressOf Method2'BIND:"Dim a As Action(Of String) = AddressOf Method2"
                                               ~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_ImplicitAddressOf_DisallowedReturnType()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module M1
    Sub Method1()
        Dim a As Func(Of String) = AddressOf Method2 'BIND:"Dim a As Func(Of String) = AddressOf Method2"
    End Sub

    Function Method2() As Integer
        Return 1
    End Function
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'Dim a As Fu ... sOf Method2')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'a As Func(O ... sOf Method2')
    Declarators:
        IVariableDeclaratorOperation (Symbol: a As System.Func(Of System.String)) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'a')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= AddressOf Method2')
        IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.String), IsInvalid, IsImplicit) (Syntax: 'AddressOf Method2')
          Target: 
            IMethodReferenceOperation: Function M1.Method2() As System.Int32 (Static) (OperationKind.MethodReference, Type: null, IsInvalid) (Syntax: 'AddressOf Method2')
              Instance Receiver: 
                null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36663: Option Strict On does not allow narrowing in implicit type conversions between method 'Public Function Method2() As Integer' and delegate 'Delegate Function Func(Of String)() As String'.
        Dim a As Func(Of String) = AddressOf Method2 'BIND:"Dim a As Func(Of String) = AddressOf Method2"
                                             ~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_ImplicitAddressOf_InvalidReturnType()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module M1
    Sub Method1()
        Dim a As Func(Of String) = AddressOf Method2 'BIND:"Dim a As Func(Of String) = AddressOf Method2"
    End Sub

    Function Method2() As NonExistant
        Return New NonExistant
    End Function
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'Dim a As Fu ... sOf Method2')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'a As Func(O ... sOf Method2')
    Declarators:
        IVariableDeclaratorOperation (Symbol: a As System.Func(Of System.String)) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'a')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= AddressOf Method2')
        IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.String), IsInvalid, IsImplicit) (Syntax: 'AddressOf Method2')
          Target: 
            IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'AddressOf Method2')
              Children(1):
                  IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'Method2')
                    Children(1):
                        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: M1, IsInvalid, IsImplicit) (Syntax: 'Method2')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC31143: Method 'Public Function Method2() As NonExistant' does not have a signature compatible with delegate 'Delegate Function Func(Of String)() As String'.
        Dim a As Func(Of String) = AddressOf Method2 'BIND:"Dim a As Func(Of String) = AddressOf Method2"
                                             ~~~~~~~
BC30002: Type 'NonExistant' is not defined.
    Function Method2() As NonExistant
                          ~~~~~~~~~~~
BC30002: Type 'NonExistant' is not defined.
        Return New NonExistant
                   ~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_ImplicitAddressOf_ReturnRelaxation()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module M1
    Sub Method1()
        Dim a As Action = AddressOf Method2 'BIND:"Dim a As Action = AddressOf Method2"
    End Sub

    Function Method2() As Object
        Return 1
    End Function
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim a As Ac ... sOf Method2')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'a As Action ... sOf Method2')
    Declarators:
        IVariableDeclaratorOperation (Symbol: a As System.Action) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'a')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= AddressOf Method2')
        IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action, IsImplicit) (Syntax: 'AddressOf Method2')
          Target: 
            IMethodReferenceOperation: Function M1.Method2() As System.Object (Static) (OperationKind.MethodReference, Type: null) (Syntax: 'AddressOf Method2')
              Instance Receiver: 
                null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_ImplicitAddressOf_ArgumentRelaxation()
            Dim source = <![CDATA[
Option Strict Off
Imports System
Module M1
    Sub Method1()
        Dim a As Action(Of Integer) = AddressOf Method2'BIND:"Dim a As Action(Of Integer) = AddressOf Method2"
    End Sub

    Sub Method2()
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim a As Ac ... sOf Method2')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'a As Action ... sOf Method2')
    Declarators:
        IVariableDeclaratorOperation (Symbol: a As System.Action(Of System.Int32)) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'a')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= AddressOf Method2')
        IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action(Of System.Int32), IsImplicit) (Syntax: 'AddressOf Method2')
          Target: 
            IMethodReferenceOperation: Sub M1.Method2() (Static) (OperationKind.MethodReference, Type: null) (Syntax: 'AddressOf Method2')
              Instance Receiver: 
                null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_ImplicitAddressOf_ConvertedToNonDelegateType()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module M1
    Class C1
    End Class
    Sub Method1()
        Dim a As String = AddressOf Method2'BIND:"Dim a As String = AddressOf Method2"
    End Sub

    Sub Method2(i As C1)
    End Sub
End Module]]>.Value

            ' We don't expect a delegate creation here. This is documenting that we still have a conversion expression when the target type
            ' isn't a delegate type
            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'Dim a As St ... sOf Method2')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'a As String ... sOf Method2')
    Declarators:
        IVariableDeclaratorOperation (Symbol: a As System.String) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'a')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= AddressOf Method2')
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String, IsInvalid, IsImplicit) (Syntax: 'AddressOf Method2')
          Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'AddressOf Method2')
              Children(1):
                  IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'Method2')
                    Children(1):
                        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: M1, IsInvalid, IsImplicit) (Syntax: 'Method2')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30581: 'AddressOf' expression cannot be converted to 'String' because 'String' is not a delegate type.
        Dim a As String = AddressOf Method2'BIND:"Dim a As String = AddressOf Method2"
                          ~~~~~~~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_CTypeAddressOf()
            Dim source = <![CDATA[
Option Strict On
Imports System

Module Program
    Sub M1()
        Dim a As Action = CType(AddressOf M1, Action)'BIND:"CType(AddressOf M1, Action)"
    End Sub

    Sub M2()
    End Sub
End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action) (Syntax: 'CType(Addre ... M1, Action)')
  Target: 
    IMethodReferenceOperation: Sub Program.M1() (Static) (OperationKind.MethodReference, Type: null) (Syntax: 'AddressOf M1')
      Instance Receiver: 
        null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of CTypeExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_CTypeAddressOf_WithReceiver()
            Dim source = <![CDATA[
Option Strict On
Imports System

Module Program
    Sub M1()
        Dim o As New Object
        Dim a As Action = CType(AddressOf o.ToString, Action)'BIND:"CType(AddressOf o.ToString, Action)"
    End Sub
End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action) (Syntax: 'CType(Addre ... ng, Action)')
  Target: 
    IMethodReferenceOperation: Function System.Object.ToString() As System.String (IsVirtual) (OperationKind.MethodReference, Type: null) (Syntax: 'AddressOf o.ToString')
      Instance Receiver: 
        ILocalReferenceOperation: o (OperationKind.LocalReference, Type: System.Object) (Syntax: 'o')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of CTypeExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_CTypeAddressOf_DisallowedArgumentType()
            Dim source = <![CDATA[
Option Strict On
Imports System

Module Program
    Sub M1()
        Dim o As New Object
        Dim a As Action = CType(AddressOf M2, Action)'BIND:"CType(AddressOf M2, Action)"
    End Sub

    Sub M2(i As Integer)
    End Sub
End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action, IsInvalid) (Syntax: 'CType(Addre ... M2, Action)')
  Target: 
    IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'AddressOf M2')
      Children(1):
          IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'M2')
            Children(1):
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Program, IsInvalid, IsImplicit) (Syntax: 'M2')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC31143: Method 'Public Sub M2(i As Integer)' does not have a signature compatible with delegate 'Delegate Sub Action()'.
        Dim a As Action = CType(AddressOf M2, Action)'BIND:"CType(AddressOf M2, Action)"
                                          ~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of CTypeExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_CTypeAddressOf_InvalidArgumentType()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module M1
    Class C1
    End Class
    Sub Method1()
        Dim a As Action(Of String) = CType(AddressOf Method2, Action(Of String))'BIND:"CType(AddressOf Method2, Action(Of String))"
    End Sub

    Sub Method2(i As C1)
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action(Of System.String), IsInvalid) (Syntax: 'CType(Addre ... Of String))')
  Target: 
    IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'AddressOf Method2')
      Children(1):
          IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'Method2')
            Children(1):
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: M1, IsInvalid, IsImplicit) (Syntax: 'Method2')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC31143: Method 'Public Sub Method2(i As M1.C1)' does not have a signature compatible with delegate 'Delegate Sub Action(Of String)(obj As String)'.
        Dim a As Action(Of String) = CType(AddressOf Method2, Action(Of String))'BIND:"CType(AddressOf Method2, Action(Of String))"
                                                     ~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of CTypeExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_CTypeAddressOf_InvalidReturnConversion()
            Dim source = <![CDATA[
Option Strict On
Imports System

Module Program
    Sub M1()
        Dim o As New Object
        Dim a As Func(Of String) = CType(AddressOf M2, Func(Of String))'BIND:"CType(AddressOf M2, Func(Of String))"
    End Sub

    Sub M2()
    End Sub
End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.String), IsInvalid) (Syntax: 'CType(Addre ... Of String))')
  Target: 
    IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'AddressOf M2')
      Children(1):
          IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'M2')
            Children(1):
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Program, IsInvalid, IsImplicit) (Syntax: 'M2')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC31143: Method 'Public Sub M2()' does not have a signature compatible with delegate 'Delegate Function Func(Of String)() As String'.
        Dim a As Func(Of String) = CType(AddressOf M2, Func(Of String))'BIND:"CType(AddressOf M2, Func(Of String))"
                                                   ~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of CTypeExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_CTypeAddressOf_InvalidVariableType()
            Dim source = <![CDATA[
Option Strict On
Imports System

Module Program
    Sub M1()
        Dim o As New Object
        Dim a As Action(Of String) = CType(AddressOf M2, Action(Of Integer))'BIND:"CType(AddressOf M2, Action(Of Integer))"
    End Sub

    Sub M2(i As Integer)
    End Sub
End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action(Of System.Int32), IsInvalid) (Syntax: 'CType(Addre ... f Integer))')
  Target: 
    IMethodReferenceOperation: Sub Program.M2(i As System.Int32) (Static) (OperationKind.MethodReference, Type: null, IsInvalid) (Syntax: 'AddressOf M2')
      Instance Receiver: 
        null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36755: 'Action(Of Integer)' cannot be converted to 'Action(Of String)' because 'String' is not derived from 'Integer', as required for the 'In' generic parameter 'T' in 'Delegate Sub Action(Of In T)(obj As T)'.
        Dim a As Action(Of String) = CType(AddressOf M2, Action(Of Integer))'BIND:"CType(AddressOf M2, Action(Of Integer))"
                                     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of CTypeExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_CTypeAddressOf_ReturnRelaxation()
            Dim source = <![CDATA[
Option Strict On
Imports System

Module Program
    Sub M1()
        Dim o As New Object
        Dim a As Action = CType(AddressOf M2, Action)'BIND:"CType(AddressOf M2, Action)"
    End Sub

    Function M2() As Integer
        Return 1
    End Function
End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action) (Syntax: 'CType(Addre ... M2, Action)')
  Target: 
    IMethodReferenceOperation: Function Program.M2() As System.Int32 (Static) (OperationKind.MethodReference, Type: null) (Syntax: 'AddressOf M2')
      Instance Receiver: 
        null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of CTypeExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_CTypeAddressOf_ArgumentRelaxation()
            Dim source = <![CDATA[
Option Strict On
Imports System

Module Program
    Sub M1()
        Dim o As New Object
        Dim a As Action(Of String) = CType(AddressOf M2, Action(Of String))'BIND:"CType(AddressOf M2, Action(Of String))"
    End Sub

    Sub M2(o As Object)
    End Sub
End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action(Of System.String)) (Syntax: 'CType(Addre ... Of String))')
  Target: 
    IMethodReferenceOperation: Sub Program.M2(o As System.Object) (Static) (OperationKind.MethodReference, Type: null) (Syntax: 'AddressOf M2')
      Instance Receiver: 
        null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of CTypeExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_DirectCastAddressOf()
            Dim source = <![CDATA[
Option Strict On
Imports System

Module Program
    Sub M1()
        Dim o As New Object
        Dim a As Action = DirectCast(AddressOf M2, Action)'BIND:"DirectCast(AddressOf M2, Action)"
    End Sub

    Sub M2()
    End Sub
End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action) (Syntax: 'DirectCast( ... M2, Action)')
  Target: 
    IMethodReferenceOperation: Sub Program.M2() (Static) (OperationKind.MethodReference, Type: null) (Syntax: 'AddressOf M2')
      Instance Receiver: 
        null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of DirectCastExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_DirectCastAddressOf_WithReceiver()
            Dim source = <![CDATA[
Option Strict On
Imports System

Module Program
    Sub M1()
        Dim o As New Object
        Dim a As Action = DirectCast(AddressOf o.ToString, Action)'BIND:"DirectCast(AddressOf o.ToString, Action)"
    End Sub
End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action) (Syntax: 'DirectCast( ... ng, Action)')
  Target: 
    IMethodReferenceOperation: Function System.Object.ToString() As System.String (IsVirtual) (OperationKind.MethodReference, Type: null) (Syntax: 'AddressOf o.ToString')
      Instance Receiver: 
        ILocalReferenceOperation: o (OperationKind.LocalReference, Type: System.Object) (Syntax: 'o')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of DirectCastExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_DirectCastAddressOf_DisallowedReturnType()
            Dim source = <![CDATA[
Option Strict On
Imports System

Module Program
    Sub M1()
        Dim o As New Object
        Dim a As Func(Of Object) = DirectCast(AddressOf M2, Func(Of Object))'BIND:"DirectCast(AddressOf M2, Func(Of Object))"
    End Sub

    Sub M2()
    End Sub
End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Object), IsInvalid) (Syntax: 'DirectCast( ... Of Object))')
  Target: 
    IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'AddressOf M2')
      Children(1):
          IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'M2')
            Children(1):
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Program, IsInvalid, IsImplicit) (Syntax: 'M2')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC31143: Method 'Public Sub M2()' does not have a signature compatible with delegate 'Delegate Function Func(Of Object)() As Object'.
        Dim a As Func(Of Object) = DirectCast(AddressOf M2, Func(Of Object))'BIND:"DirectCast(AddressOf M2, Func(Of Object))"
                                                        ~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of DirectCastExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_DirectCastAddressOf_InvalidReturnType()
            Dim source = <![CDATA[
Option Strict On
Imports System

Module Program
    Sub M1()
        Dim o As New Object
        Dim a As Func(Of Object) = DirectCast(AddressOf M2, Func(Of Object))'BIND:"DirectCast(AddressOf M2, Func(Of Object))"
    End Sub

    Function M2() As NonExistant
        Return New NonExistant
    End Function
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Object), IsInvalid) (Syntax: 'DirectCast( ... Of Object))')
  Target: 
    IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'AddressOf M2')
      Children(1):
          IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'M2')
            Children(1):
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Program, IsInvalid, IsImplicit) (Syntax: 'M2')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC31143: Method 'Public Function M2() As NonExistant' does not have a signature compatible with delegate 'Delegate Function Func(Of Object)() As Object'.
        Dim a As Func(Of Object) = DirectCast(AddressOf M2, Func(Of Object))'BIND:"DirectCast(AddressOf M2, Func(Of Object))"
                                                        ~~
BC30002: Type 'NonExistant' is not defined.
    Function M2() As NonExistant
                     ~~~~~~~~~~~
BC30002: Type 'NonExistant' is not defined.
        Return New NonExistant
                   ~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of DirectCastExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_DirectCastAddressOf_DisallowedArgumentType()
            Dim source = <![CDATA[
Option Strict On
Imports System

Module Program
    Sub M1()
        Dim o As New Object
        Dim a As Action = DirectCast(AddressOf M2, Action)'BIND:"DirectCast(AddressOf M2, Action)"
    End Sub

    Sub M2(s As Integer)
    End Sub
End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action, IsInvalid) (Syntax: 'DirectCast( ... M2, Action)')
  Target: 
    IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'AddressOf M2')
      Children(1):
          IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'M2')
            Children(1):
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Program, IsInvalid, IsImplicit) (Syntax: 'M2')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC31143: Method 'Public Sub M2(s As Integer)' does not have a signature compatible with delegate 'Delegate Sub Action()'.
        Dim a As Action = DirectCast(AddressOf M2, Action)'BIND:"DirectCast(AddressOf M2, Action)"
                                               ~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of DirectCastExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_DirectCastAddressOf_InvalidArgumentType()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module M1
    Class C1
    End Class
    Sub Method1()
        Dim a As Action(Of String) = DirectCast(AddressOf Method2, Action(Of String))'BIND:"DirectCast(AddressOf Method2, Action(Of String))"
    End Sub

    Sub Method2(i As C1)
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action(Of System.String), IsInvalid) (Syntax: 'DirectCast( ... Of String))')
  Target: 
    IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'AddressOf Method2')
      Children(1):
          IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'Method2')
            Children(1):
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: M1, IsInvalid, IsImplicit) (Syntax: 'Method2')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC31143: Method 'Public Sub Method2(i As M1.C1)' does not have a signature compatible with delegate 'Delegate Sub Action(Of String)(obj As String)'.
        Dim a As Action(Of String) = DirectCast(AddressOf Method2, Action(Of String))'BIND:"DirectCast(AddressOf Method2, Action(Of String))"
                                                          ~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of DirectCastExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_DirectCastAddressOf_InvalidVariableType()
            Dim source = <![CDATA[
Option Strict On
Imports System

Module Program
    Sub M1()
        Dim o As New Object
        Dim a As Action = DirectCast(AddressOf M2, Action(Of String))'BIND:"DirectCast(AddressOf M2, Action(Of String))"
    End Sub

    Sub M2(s As String)
    End Sub
End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action(Of System.String), IsInvalid) (Syntax: 'DirectCast( ... Of String))')
  Target: 
    IMethodReferenceOperation: Sub Program.M2(s As System.String) (Static) (OperationKind.MethodReference, Type: null, IsInvalid) (Syntax: 'AddressOf M2')
      Instance Receiver: 
        null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30311: Value of type 'Action(Of String)' cannot be converted to 'Action'.
        Dim a As Action = DirectCast(AddressOf M2, Action(Of String))'BIND:"DirectCast(AddressOf M2, Action(Of String))"
                          ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of DirectCastExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_DirectCastAddressOf_ArgumentRelaxation()
            Dim source = <![CDATA[
Option Strict On
Imports System

Module Program
    Sub M1()
        Dim o As New Object
        Dim a As Action(Of String) = DirectCast(AddressOf M2, Action(Of String))'BIND:"DirectCast(AddressOf M2, Action(Of String))"
    End Sub

    Sub M2()
    End Sub
End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action(Of System.String)) (Syntax: 'DirectCast( ... Of String))')
  Target: 
    IMethodReferenceOperation: Sub Program.M2() (Static) (OperationKind.MethodReference, Type: null) (Syntax: 'AddressOf M2')
      Instance Receiver: 
        null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of DirectCastExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_DirectCastAddressOf_ReturnRelaxation()
            Dim source = <![CDATA[
Option Strict On
Imports System

Module Program
    Sub M1()
        Dim o As New Object
        Dim a As Action = DirectCast(AddressOf M2, Action)'BIND:"DirectCast(AddressOf M2, Action)"
    End Sub

    Function M2() As Integer
        Return 1
    End Function
End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action) (Syntax: 'DirectCast( ... M2, Action)')
  Target: 
    IMethodReferenceOperation: Function Program.M2() As System.Int32 (Static) (OperationKind.MethodReference, Type: null) (Syntax: 'AddressOf M2')
      Instance Receiver: 
        null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of DirectCastExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_TryCastAddressOf()
            Dim source = <![CDATA[
Option Strict On
Imports System

Module Program
    Sub M1()
        Dim o As New Object
        Dim a As Action = TryCast(AddressOf M2, Action)'BIND:"TryCast(AddressOf M2, Action)"
    End Sub

    Sub M2()
    End Sub
End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action) (Syntax: 'TryCast(Add ... M2, Action)')
  Target: 
    IMethodReferenceOperation: Sub Program.M2() (Static) (OperationKind.MethodReference, Type: null) (Syntax: 'AddressOf M2')
      Instance Receiver: 
        null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of TryCastExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_TryCastAddressOf_WithReceiver()
            Dim source = <![CDATA[
Option Strict On
Imports System

Module Program
    Sub M1()
        Dim o As New Object
        Dim a As Action = TryCast(AddressOf o.ToString, Action)'BIND:"TryCast(AddressOf o.ToString, Action)"
    End Sub
End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action) (Syntax: 'TryCast(Add ... ng, Action)')
  Target: 
    IMethodReferenceOperation: Function System.Object.ToString() As System.String (IsVirtual) (OperationKind.MethodReference, Type: null) (Syntax: 'AddressOf o.ToString')
      Instance Receiver: 
        ILocalReferenceOperation: o (OperationKind.LocalReference, Type: System.Object) (Syntax: 'o')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of TryCastExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_TryCastAddressOf_DisallowedReturnType()
            Dim source = <![CDATA[
Option Strict On
Imports System

Module Program
    Sub M1()
        Dim o As New Object
        Dim a As Func(Of Object) = TryCast(AddressOf M2, Func(Of Object))'BIND:"TryCast(AddressOf M2, Func(Of Object))"
    End Sub

    Sub M2()
    End Sub
End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Object), IsInvalid) (Syntax: 'TryCast(Add ... Of Object))')
  Target: 
    IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'AddressOf M2')
      Children(1):
          IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'M2')
            Children(1):
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Program, IsInvalid, IsImplicit) (Syntax: 'M2')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC31143: Method 'Public Sub M2()' does not have a signature compatible with delegate 'Delegate Function Func(Of Object)() As Object'.
        Dim a As Func(Of Object) = TryCast(AddressOf M2, Func(Of Object))'BIND:"TryCast(AddressOf M2, Func(Of Object))"
                                                     ~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of TryCastExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_TryCastAddressOf_InvalidReturnType()
            Dim source = <![CDATA[
Option Strict On
Imports System

Module Program
    Sub M1()
        Dim o As New Object
        Dim a As Func(Of Object) = TryCast(AddressOf M2, Func(Of Object))'BIND:"TryCast(AddressOf M2, Func(Of Object))"
    End Sub

    Function M2() As NonExistant
        Return NonExistant
    End Function
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Object), IsInvalid) (Syntax: 'TryCast(Add ... Of Object))')
  Target: 
    IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'AddressOf M2')
      Children(1):
          IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'M2')
            Children(1):
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Program, IsInvalid, IsImplicit) (Syntax: 'M2')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC31143: Method 'Public Function M2() As NonExistant' does not have a signature compatible with delegate 'Delegate Function Func(Of Object)() As Object'.
        Dim a As Func(Of Object) = TryCast(AddressOf M2, Func(Of Object))'BIND:"TryCast(AddressOf M2, Func(Of Object))"
                                                     ~~
BC30002: Type 'NonExistant' is not defined.
    Function M2() As NonExistant
                     ~~~~~~~~~~~
BC30451: 'NonExistant' is not declared. It may be inaccessible due to its protection level.
        Return NonExistant
               ~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of TryCastExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_TryCastAddressOf_DisallowedArgumentType()
            Dim source = <![CDATA[
Option Strict On
Imports System

Module Program
    Sub M1()
        Dim o As New Object
        Dim a As Action = TryCast(AddressOf M2, Action)'BIND:"TryCast(AddressOf M2, Action)"
    End Sub

    Sub M2(s As Integer)
    End Sub
End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action, IsInvalid) (Syntax: 'TryCast(Add ... M2, Action)')
  Target: 
    IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'AddressOf M2')
      Children(1):
          IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'M2')
            Children(1):
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Program, IsInvalid, IsImplicit) (Syntax: 'M2')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC31143: Method 'Public Sub M2(s As Integer)' does not have a signature compatible with delegate 'Delegate Sub Action()'.
        Dim a As Action = TryCast(AddressOf M2, Action)'BIND:"TryCast(AddressOf M2, Action)"
                                            ~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of TryCastExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_TryCastAddressOf_InvalidArgumentType()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module M1
    Class C1
    End Class
    Sub Method1()
        Dim a As Action(Of String) = TryCast(AddressOf Method2, Action(Of String))'BIND:"TryCast(AddressOf Method2, Action(Of String))"
    End Sub

    Sub Method2(i As C1)
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action(Of System.String), IsInvalid) (Syntax: 'TryCast(Add ... Of String))')
  Target: 
    IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'AddressOf Method2')
      Children(1):
          IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'Method2')
            Children(1):
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: M1, IsInvalid, IsImplicit) (Syntax: 'Method2')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC31143: Method 'Public Sub Method2(i As M1.C1)' does not have a signature compatible with delegate 'Delegate Sub Action(Of String)(obj As String)'.
        Dim a As Action(Of String) = TryCast(AddressOf Method2, Action(Of String))'BIND:"TryCast(AddressOf Method2, Action(Of String))"
                                                       ~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of TryCastExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_TryCastAddressOf_InvalidVariableType()
            Dim source = <![CDATA[
Option Strict On
Imports System

Module Program
    Sub M1()
        Dim o As New Object
        Dim a As Action = TryCast(AddressOf M2, Action(Of String))'BIND:"TryCast(AddressOf M2, Action(Of String))"
    End Sub

    Sub M2(s As String)
    End Sub
End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action(Of System.String), IsInvalid) (Syntax: 'TryCast(Add ... Of String))')
  Target: 
    IMethodReferenceOperation: Sub Program.M2(s As System.String) (Static) (OperationKind.MethodReference, Type: null, IsInvalid) (Syntax: 'AddressOf M2')
      Instance Receiver: 
        null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30311: Value of type 'Action(Of String)' cannot be converted to 'Action'.
        Dim a As Action = TryCast(AddressOf M2, Action(Of String))'BIND:"TryCast(AddressOf M2, Action(Of String))"
                          ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of TryCastExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_TryCastAddressOf_ArgumentRelaxation()
            Dim source = <![CDATA[
Option Strict On
Imports System

Module Program
    Sub M1()
        Dim o As New Object
        Dim a As Action(Of String) = TryCast(AddressOf M2, Action(Of String))'BIND:"TryCast(AddressOf M2, Action(Of String))"
    End Sub

    Sub M2()
    End Sub
End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action(Of System.String)) (Syntax: 'TryCast(Add ... Of String))')
  Target: 
    IMethodReferenceOperation: Sub Program.M2() (Static) (OperationKind.MethodReference, Type: null) (Syntax: 'AddressOf M2')
      Instance Receiver: 
        null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of TryCastExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_TryCastAddressOf_ReturnRelaxation()
            Dim source = <![CDATA[
Option Strict On
Imports System

Module Program
    Sub M1()
        Dim o As New Object
        Dim a As Action = TryCast(AddressOf M2, Action)'BIND:"TryCast(AddressOf M2, Action)"
    End Sub

    Function M2() As Integer
        Return 1
    End Function
End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action) (Syntax: 'TryCast(Add ... M2, Action)')
  Target: 
    IMethodReferenceOperation: Function Program.M2() As System.Int32 (Static) (OperationKind.MethodReference, Type: null) (Syntax: 'AddressOf M2')
      Instance Receiver: 
        null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of TryCastExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        <WorkItem(15513, "https://github.com/dotnet/roslyn/issues/15513")>
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
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action) (Syntax: 'New Action( ... Of Method2)')
  Target: 
    IMethodReferenceOperation: Sub M1.Method2() (Static) (OperationKind.MethodReference, Type: null) (Syntax: 'AddressOf Method2')
      Instance Receiver: 
        null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        <WorkItem(15513, "https://github.com/dotnet/roslyn/issues/15513")>
        Public Sub DelegateCreationExpression_DelegateCreationInstanceAddressOfArgument()
            Dim source = <![CDATA[
Option Strict On
Imports System
Class M1
    Sub Method1()
        Dim a As Action = New Action(AddressOf Method2)'BIND:"New Action(AddressOf Method2)"
    End Sub

    Sub Method2()
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action) (Syntax: 'New Action( ... Of Method2)')
  Target: 
    IMethodReferenceOperation: Sub M1.Method2() (OperationKind.MethodReference, Type: null) (Syntax: 'AddressOf Method2')
      Instance Receiver: 
        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: M1, IsImplicit) (Syntax: 'Method2')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        <WorkItem(15513, "https://github.com/dotnet/roslyn/issues/15513")>
        Public Sub DelegateCreationExpression_DelegateCreationSharedAddressOfArgument()
            Dim source = <![CDATA[
Option Strict On
Imports System
Class M1
    Sub Method1()
        Dim a As Action = New Action(AddressOf Me.Method2)'BIND:"New Action(AddressOf Me.Method2)"
    End Sub

    Shared Sub Method2()
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action) (Syntax: 'New Action( ... Me.Method2)')
  Target: 
    IMethodReferenceOperation: Sub M1.Method2() (Static) (OperationKind.MethodReference, Type: null) (Syntax: 'AddressOf Me.Method2')
      Instance Receiver: 
        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: M1) (Syntax: 'Me')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        Dim a As Action = New Action(AddressOf Me.Method2)'BIND:"New Action(AddressOf Me.Method2)"
                                     ~~~~~~~~~~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_DelegateCreationAddressOfArgument_MultipleArgumentsToConstructor()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module M1
    Sub Method1()
        Dim a As Action = New Action(AddressOf Method2, 1)'BIND:"Dim a As Action = New Action(AddressOf Method2, 1)"
    End Sub

    Sub Method2()
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'Dim a As Ac ... Method2, 1)')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'a As Action ... Method2, 1)')
    Declarators:
        IVariableDeclaratorOperation (Symbol: a As System.Action) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'a')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= New Actio ... Method2, 1)')
        IInvalidOperation (OperationKind.Invalid, Type: System.Action, IsInvalid) (Syntax: 'New Action( ... Method2, 1)')
          Children(2):
              IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: 'AddressOf Method2')
                Children(1):
                    IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'AddressOf Method2')
                      Children(1):
                          IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'Method2')
                            Children(1):
                                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: M1, IsInvalid, IsImplicit) (Syntax: 'Method2')
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC32008: Delegate 'Action' requires an 'AddressOf' expression or lambda expression as the only argument to its constructor.
        Dim a As Action = New Action(AddressOf Method2, 1)'BIND:"Dim a As Action = New Action(AddressOf Method2, 1)"
                                    ~~~~~~~~~~~~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
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
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action) (Syntax: 'New Action( ... Of Method2)')
  Target: 
    IMethodReferenceOperation: Function M1.Method2() As System.Object (Static) (OperationKind.MethodReference, Type: null) (Syntax: 'AddressOf Method2')
      Instance Receiver: 
        null
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
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action(Of System.Int32)) (Syntax: 'New Action( ... Of Method2)')
  Target: 
    IMethodReferenceOperation: Sub M1.Method2(o As System.Object) (Static) (OperationKind.MethodReference, Type: null) (Syntax: 'AddressOf Method2')
      Instance Receiver: 
        null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_DelegateCreationAddressOfArgument_DisallowedArgumentType()
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
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action, IsInvalid) (Syntax: 'New Action( ... Of Method2)')
  Target: 
    IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'AddressOf Method2')
      Children(1):
          IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'Method2')
            Children(1):
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: M1, IsInvalid, IsImplicit) (Syntax: 'Method2')
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
        Public Sub DelegateCreationExpression_DelegateCreationAddressOfArgument_InvalidArgumentType()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module M1
    Class C1
    End Class
    Sub Method1()
        Dim a As Action(Of String) = New Action(Of String)(AddressOf Method2)'BIND:"New Action(Of String)(AddressOf Method2)"
    End Sub

    Sub Method2(i As C1)
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action(Of System.String), IsInvalid) (Syntax: 'New Action( ... Of Method2)')
  Target: 
    IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'AddressOf Method2')
      Children(1):
          IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'Method2')
            Children(1):
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: M1, IsInvalid, IsImplicit) (Syntax: 'Method2')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC31143: Method 'Public Sub Method2(i As M1.C1)' does not have a signature compatible with delegate 'Delegate Sub Action(Of String)(obj As String)'.
        Dim a As Action(Of String) = New Action(Of String)(AddressOf Method2)'BIND:"New Action(Of String)(AddressOf Method2)"
                                                                     ~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_DelegateCreationAddressOfArgument_DisallowedReturnType()
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
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.String), IsInvalid) (Syntax: 'New Func(Of ... Of Method2)')
  Target: 
    IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'AddressOf Method2')
      Children(1):
          IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'Method2')
            Children(1):
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: M1, IsInvalid, IsImplicit) (Syntax: 'Method2')
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
        Public Sub DelegateCreationExpression_DelegateCreationAddressOfArgument_InvalidReturnType()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module M1
    Sub Method1()
        Dim a As Func(Of String) = New Func(Of String)(AddressOf Method2)'BIND:"New Func(Of String)(AddressOf Method2)"
    End Sub

    Function Method2() As NonExistant
        Return New NonExistant()
    End Function
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.String), IsInvalid) (Syntax: 'New Func(Of ... Of Method2)')
  Target: 
    IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'AddressOf Method2')
      Children(1):
          IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'Method2')
            Children(1):
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: M1, IsInvalid, IsImplicit) (Syntax: 'Method2')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC31143: Method 'Public Function Method2() As NonExistant' does not have a signature compatible with delegate 'Delegate Function Func(Of String)() As String'.
        Dim a As Func(Of String) = New Func(Of String)(AddressOf Method2)'BIND:"New Func(Of String)(AddressOf Method2)"
                                                                 ~~~~~~~
BC30002: Type 'NonExistant' is not defined.
    Function Method2() As NonExistant
                          ~~~~~~~~~~~
BC30002: Type 'NonExistant' is not defined.
        Return New NonExistant()
                   ~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub IDelegateCreation_SharedAddressOfWithInstanceReceiver()
            Dim source = <![CDATA[
Option Strict On
Imports System

Module M1
    Class C1
        Shared Sub S1()
        End Sub
        Shared Sub S2()
            Dim c1Instance As New C1
            Dim a As Action = AddressOf c1Instance.S1'BIND:"AddressOf c1Instance.S1"
        End Sub
    End Class
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IMethodReferenceOperation: Sub M1.C1.S1() (Static) (OperationKind.MethodReference, Type: null) (Syntax: 'AddressOf c1Instance.S1')
  Instance Receiver: 
    ILocalReferenceOperation: c1Instance (OperationKind.LocalReference, Type: M1.C1) (Syntax: 'c1Instance')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
            Dim a As Action = AddressOf c1Instance.S1'BIND:"AddressOf c1Instance.S1"
                              ~~~~~~~~~~~~~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub IDelegateCreation_SharedAddressOfAccessOnClass()
            Dim source = <![CDATA[
Option Strict On
Imports System

Module M1
    Class C1
        Shared Sub S1()
        End Sub
        Shared Sub S2()
            Dim a As Action = AddressOf C1.S1'BIND:"AddressOf C1.S1"
        End Sub
    End Class
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IMethodReferenceOperation: Sub M1.C1.S1() (Static) (OperationKind.MethodReference, Type: null) (Syntax: 'AddressOf C1.S1')
  Instance Receiver: 
    null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub IDelegateCreation_InstanceAddressOfAccessOnClass()
            Dim source = <![CDATA[
Option Strict On
Imports System

Module M1
    Class C1
        Sub S1()
        End Sub
        Shared Sub S2()
            Dim a As Action = AddressOf C1.S1'BIND:"AddressOf C1.S1"
        End Sub
    End Class
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'AddressOf C1.S1')
  Children(1):
      IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'C1.S1')
        Children(1):
            IOperation:  (OperationKind.None, Type: M1.C1, IsInvalid) (Syntax: 'C1')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30469: Reference to a non-shared member requires an object reference.
            Dim a As Action = AddressOf C1.S1'BIND:"AddressOf C1.S1"
                              ~~~~~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreation_ParenthesizedAddressOf_CType()
            Dim source = <![CDATA[
Imports System

Public Class C
    Public Sub M1()
        CType(((AddressOf M2)), Action)'BIND:"CType(((AddressOf M2)), Action)"
    End Sub

    Public Sub M2()
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action) (Syntax: 'CType(((Add ... )), Action)')
  Target: 
    IParenthesizedOperation (OperationKind.Parenthesized, Type: null) (Syntax: '((AddressOf M2))')
      Operand: 
        IParenthesizedOperation (OperationKind.Parenthesized, Type: null) (Syntax: '(AddressOf M2)')
          Operand: 
            IMethodReferenceOperation: Sub C.M2() (OperationKind.MethodReference, Type: null) (Syntax: 'AddressOf M2')
              Instance Receiver: 
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'M2')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of CTypeExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreation_ParenthesizedAddressOf_Implicit()
            Dim source = <![CDATA[
Imports System

Public Class C
    Public Sub M1()
        Dim a As Action = ((AddressOf M2))'BIND:"= ((AddressOf M2))"
    End Sub

    Public Sub M2()
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= ((AddressOf M2))')
  IParenthesizedOperation (OperationKind.Parenthesized, Type: System.Action) (Syntax: '((AddressOf M2))')
    Operand: 
      IParenthesizedOperation (OperationKind.Parenthesized, Type: System.Action) (Syntax: '(AddressOf M2)')
        Operand: 
          IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action, IsImplicit) (Syntax: 'AddressOf M2')
            Target: 
              IMethodReferenceOperation: Sub C.M2() (OperationKind.MethodReference, Type: null) (Syntax: 'AddressOf M2')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'M2')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of EqualsValueSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreation_ParenthesizedAddressOf_CType_InvalidMethod()
            Dim source = <![CDATA[
Imports System

Public Class C
    Public Sub M1()
        CType(((AddressOf M2)), Action)'BIND:"CType(((AddressOf M2)), Action)"
    End Sub

    Public Sub M2(o As Object)
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action, IsInvalid) (Syntax: 'CType(((Add ... )), Action)')
  Target: 
    IParenthesizedOperation (OperationKind.Parenthesized, Type: null, IsInvalid) (Syntax: '((AddressOf M2))')
      Operand: 
        IParenthesizedOperation (OperationKind.Parenthesized, Type: null, IsInvalid) (Syntax: '(AddressOf M2)')
          Operand: 
            IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'AddressOf M2')
              Children(1):
                  IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'M2')
                    Children(1):
                        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'M2')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC31143: Method 'Public Sub M2(o As Object)' does not have a signature compatible with delegate 'Delegate Sub Action()'.
        CType(((AddressOf M2)), Action)'BIND:"CType(((AddressOf M2)), Action)"
                          ~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of CTypeExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreation_ParenthesizedAddressOf_CType_InvalidMissingParameter()
            Dim source = <![CDATA[
Imports System

Public Class C
    Public Sub M1()
        CType(((AddressOf M2)), Action(Of String))'BIND:"CType(((AddressOf M2)), Action(Of String))"
    End Sub

    Public Sub M2()
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action(Of System.String), IsInvalid) (Syntax: 'CType(((Add ... Of String))')
  Target: 
    IParenthesizedOperation (OperationKind.Parenthesized, Type: null, IsInvalid) (Syntax: '((AddressOf M2))')
      Operand: 
        IParenthesizedOperation (OperationKind.Parenthesized, Type: null, IsInvalid) (Syntax: '(AddressOf M2)')
          Operand: 
            IMethodReferenceOperation: Sub C.M2() (OperationKind.MethodReference, Type: null, IsInvalid) (Syntax: 'AddressOf M2')
              Instance Receiver: 
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'M2')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30455: Argument not specified for parameter 'obj' of 'Action(Of String)'.
        CType(((AddressOf M2)), Action(Of String))'BIND:"CType(((AddressOf M2)), Action(Of String))"
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of CTypeExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreation_ParenthesizedAddressOf_CType_InvalidCast()
            Dim source = <![CDATA[
Imports System

Public Class C
    Public Sub M1()
        CType(((AddressOf M2)), Func(Of String)) 'BIND:"CType(((AddressOf M2)), Func(Of String))"
    End Sub

    Public Sub M2()
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.String), IsInvalid) (Syntax: 'CType(((Add ... Of String))')
  Target: 
    IParenthesizedOperation (OperationKind.Parenthesized, Type: null, IsInvalid) (Syntax: '((AddressOf M2))')
      Operand: 
        IParenthesizedOperation (OperationKind.Parenthesized, Type: null, IsInvalid) (Syntax: '(AddressOf M2)')
          Operand: 
            IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'AddressOf M2')
              Children(1):
                  IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'M2')
                    Children(1):
                        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'M2')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC31143: Method 'Public Sub M2()' does not have a signature compatible with delegate 'Delegate Function Func(Of String)() As String'.
        CType(((AddressOf M2)), Func(Of String)) 'BIND:"CType(((AddressOf M2)), Func(Of String))"
                          ~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of CTypeExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreation_ParenthesizedAddressOf_CType_InvalidNonDelegateTargetType()
            Dim source = <![CDATA[
Imports System

Public Class C
    Public Sub M1()
        CType(((AddressOf M2)), Object)'BIND:"CType(((AddressOf M2)), Object)"
    End Sub

    Public Sub M2(o As Object)
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsInvalid) (Syntax: 'CType(((Add ... )), Object)')
  Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Operand: 
    IParenthesizedOperation (OperationKind.Parenthesized, Type: null, IsInvalid) (Syntax: '((AddressOf M2))')
      Operand: 
        IParenthesizedOperation (OperationKind.Parenthesized, Type: null, IsInvalid) (Syntax: '(AddressOf M2)')
          Operand: 
            IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'AddressOf M2')
              Children(1):
                  IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'M2')
                    Children(1):
                        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'M2')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30581: 'AddressOf' expression cannot be converted to 'Object' because 'Object' is not a delegate type.
        CType(((AddressOf M2)), Object)'BIND:"CType(((AddressOf M2)), Object)"
                ~~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of CTypeExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreation_ParenthesizedAddressOf_DirectCast()
            Dim source = <![CDATA[
Imports System

Public Class C
    Public Sub M1()
        DirectCast(((AddressOf M2)), Action)'BIND:"DirectCast(((AddressOf M2)), Action)"
    End Sub

    Public Sub M2()
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action) (Syntax: 'DirectCast( ... )), Action)')
  Target: 
    IParenthesizedOperation (OperationKind.Parenthesized, Type: null) (Syntax: '((AddressOf M2))')
      Operand: 
        IParenthesizedOperation (OperationKind.Parenthesized, Type: null) (Syntax: '(AddressOf M2)')
          Operand: 
            IMethodReferenceOperation: Sub C.M2() (OperationKind.MethodReference, Type: null) (Syntax: 'AddressOf M2')
              Instance Receiver: 
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'M2')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of DirectCastExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreation_ParenthesizedAddressOf_TryCast()
            Dim source = <![CDATA[
Imports System

Public Class C
    Public Sub M1()
        TryCast(((AddressOf M2)), Action)'BIND:"TryCast(((AddressOf M2)), Action)"
    End Sub

    Public Sub M2()
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action) (Syntax: 'TryCast(((A ... )), Action)')
  Target: 
    IParenthesizedOperation (OperationKind.Parenthesized, Type: null) (Syntax: '((AddressOf M2))')
      Operand: 
        IParenthesizedOperation (OperationKind.Parenthesized, Type: null) (Syntax: '(AddressOf M2)')
          Operand: 
            IMethodReferenceOperation: Sub C.M2() (OperationKind.MethodReference, Type: null) (Syntax: 'AddressOf M2')
              Instance Receiver: 
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'M2')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of TryCastExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ExplicitCastOnTuple()
            Dim source = <![CDATA[
Imports System

Public Class C
    Public Sub M1()
        Dim x = DirectCast((AddressOf M2, 1), (Action, Integer))'BIND:"DirectCast((AddressOf M2, 1), (Action, Integer))"
    End Sub

    Public Sub M2()
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: (System.Action, System.Int32)) (Syntax: 'DirectCast( ... , Integer))')
  Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Operand:
    ITupleOperation (OperationKind.Tuple, Type: (System.Action, System.Int32)) (Syntax: '(AddressOf M2, 1)')
      NaturalType: null
      Elements(2):
          IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action, IsImplicit) (Syntax: 'AddressOf M2')
            Target:
              IMethodReferenceOperation: Sub C.M2() (OperationKind.MethodReference, Type: null) (Syntax: 'AddressOf M2')
                Instance Receiver:
                  IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'M2')
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '1')
            Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand:
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of DirectCastExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

#End Region

#Region "Anonymous Delegates"

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_ImplicitAnonymousDelegateConversion()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module M1
    Sub Method1()
        Dim a = Sub()'BIND:"Dim a = Sub()"
                End Sub
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim a = Sub ... End Sub')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'a = Sub()'B ... End Sub')
    Declarators:
        IVariableDeclaratorOperation (Symbol: a As Sub <generated method>()) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'a')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= Sub()'BIN ... End Sub')
        IDelegateCreationOperation (OperationKind.DelegateCreation, Type: Sub <generated method>(), IsImplicit) (Syntax: 'Sub()'BIND: ... End Sub')
          Target: 
            IAnonymousFunctionOperation (Symbol: Sub ()) (OperationKind.AnonymousFunction, Type: null) (Syntax: 'Sub()'BIND: ... End Sub')
              IBlockOperation (2 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Sub()'BIND: ... End Sub')
                ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'End Sub')
                  Statement: 
                    null
                IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'End Sub')
                  ReturnedValue: 
                    null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_ImplicitAnonymousDelegateConversion_JustInitializerReturnsOnlyLambda()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module M1
    Sub Method1()
        Dim a = Sub()'BIND:"Sub()"
                End Sub
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IAnonymousFunctionOperation (Symbol: Sub ()) (OperationKind.AnonymousFunction, Type: null) (Syntax: 'Sub()'BIND: ... End Sub')
  IBlockOperation (2 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Sub()'BIND: ... End Sub')
    ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'End Sub')
      Statement: 
        null
    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'End Sub')
      ReturnedValue: 
        null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of MultiLineLambdaExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub DelegateCreationExpression_ImplicitAnonymousDelegateConversion_SingleLineLambda()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module M1
    Sub Method1()
        Dim a = Sub() Console.WriteLine()'BIND:"Dim a = Sub() Console.WriteLine()"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim a = Sub ... WriteLine()')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'a = Sub() C ... WriteLine()')
    Declarators:
        IVariableDeclaratorOperation (Symbol: a As Sub <generated method>()) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'a')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= Sub() Con ... WriteLine()')
        IDelegateCreationOperation (OperationKind.DelegateCreation, Type: Sub <generated method>(), IsImplicit) (Syntax: 'Sub() Conso ... WriteLine()')
          Target: 
            IAnonymousFunctionOperation (Symbol: Sub ()) (OperationKind.AnonymousFunction, Type: null) (Syntax: 'Sub() Conso ... WriteLine()')
              IBlockOperation (3 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Sub() Conso ... WriteLine()')
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.WriteLine()')
                  Expression: 
                    IInvocationOperation (Sub System.Console.WriteLine()) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.WriteLine()')
                      Instance Receiver: 
                        null
                      Arguments(0)
                ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'Sub() Conso ... WriteLine()')
                  Statement: 
                    null
                IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'Sub() Conso ... WriteLine()')
                  ReturnedValue: 
                    null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

#End Region

#Region "Control Flow"

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub DelegateCreation_NoControlFlow()
            Dim source = <![CDATA[
Imports System

Class C
    Private Sub M(a1 As Action, a2 As Action, a3 As Action, a4 As Action) 'BIND:"Private Sub M(a1 As Action, a2 As Action, a3 As Action, a4 As Action)"
        a1 = Sub()
             End Sub
        a2 = AddressOf M2
        a3 = New Action(AddressOf M3)
    End Sub

    Private Sub M2()
    End Sub

    Private Shared Sub M3()
    End Sub
End Class
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (3)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a1 = Sub() ... End Sub')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Action, IsImplicit) (Syntax: 'a1 = Sub() ... End Sub')
              Left: 
                IParameterReferenceOperation: a1 (OperationKind.ParameterReference, Type: System.Action) (Syntax: 'a1')
              Right: 
                IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action, IsImplicit) (Syntax: 'Sub() ... End Sub')
                  Target: 
                    IFlowAnonymousFunctionOperation (Symbol: Sub ()) (OperationKind.FlowAnonymousFunction, Type: null) (Syntax: 'Sub() ... End Sub')
                    {
                        Block[B0#A0] - Entry
                            Statements (0)
                            Next (Regular) Block[B1#A0]
                        Block[B1#A0] - Exit
                            Predecessors: [B0#A0]
                            Statements (0)
                    }

        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a2 = AddressOf M2')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Action, IsImplicit) (Syntax: 'a2 = AddressOf M2')
              Left: 
                IParameterReferenceOperation: a2 (OperationKind.ParameterReference, Type: System.Action) (Syntax: 'a2')
              Right: 
                IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action, IsImplicit) (Syntax: 'AddressOf M2')
                  Target: 
                    IMethodReferenceOperation: Sub C.M2() (OperationKind.MethodReference, Type: null) (Syntax: 'AddressOf M2')
                      Instance Receiver: 
                        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'M2')

        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a3 = New Ac ... dressOf M3)')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Action, IsImplicit) (Syntax: 'a3 = New Ac ... dressOf M3)')
              Left: 
                IParameterReferenceOperation: a3 (OperationKind.ParameterReference, Type: System.Action) (Syntax: 'a3')
              Right: 
                IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action) (Syntax: 'New Action(AddressOf M3)')
                  Target: 
                    IMethodReferenceOperation: Sub C.M3() (Static) (OperationKind.MethodReference, Type: null) (Syntax: 'AddressOf M3')
                      Instance Receiver: 
                        null

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub DelegateCreation_ControlFlowInTarget()
            Dim source = <![CDATA[
Imports System

Class C
    Private Sub M(a1 As Action, a2 As Action, a3 As Action) 'BIND:"Private Sub M(a1 As Action, a2 As Action, a3 As Action)"
        a1 = AddressOf If(a2, a3)
    End Sub
End Class
]]>.Value

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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a1')
              Value: 
                IParameterReferenceOperation: a1 (OperationKind.ParameterReference, Type: System.Action) (Syntax: 'a1')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'a2')
                  Value: 
                    IParameterReferenceOperation: a2 (OperationKind.ParameterReference, Type: System.Action, IsInvalid) (Syntax: 'a2')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'a2')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Action, IsInvalid, IsImplicit) (Syntax: 'a2')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'a2')
                  Value: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Action, IsInvalid, IsImplicit) (Syntax: 'a2')

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'a3')
              Value: 
                IParameterReferenceOperation: a3 (OperationKind.ParameterReference, Type: System.Action, IsInvalid) (Syntax: 'a3')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'a1 = Addres ...  If(a2, a3)')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Action, IsInvalid, IsImplicit) (Syntax: 'a1 = Addres ...  If(a2, a3)')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Action, IsImplicit) (Syntax: 'a1')
                  Right: 
                    IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action, IsInvalid, IsImplicit) (Syntax: 'AddressOf If(a2, a3)')
                      Target: 
                        IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'AddressOf If(a2, a3)')
                          Children(1):
                              IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Action, IsInvalid, IsImplicit) (Syntax: 'If(a2, a3)')

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30577: 'AddressOf' operand must be the name of a method (without parentheses).
        a1 = AddressOf If(a2, a3)
                       ~~~~~~~~~~
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

#End Region

    End Class
End Namespace
