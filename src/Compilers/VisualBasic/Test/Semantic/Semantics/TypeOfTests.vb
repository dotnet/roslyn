' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics
    Public Class TypeOfTests
        Inherits BasicTestBase

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub ProducesNoErrorsOnBoxedValueTypes()
            Dim source = <![CDATA[
Option Strict On

Imports System

Module Program
    Sub Main(args As String())'BIND:"Sub Main(args As String())"

        Dim o As Object = 1

        If TypeOf o Is Integer Then
            Console.WriteLine("Boxed as System.Object")
        End If

        If TypeOf o Is IComparable Then
            Console.WriteLine("Boxed as System.Object to interface type")
        End If

        Dim v As ValueType = DayOfWeek.Monday

        If TypeOf v Is DayOfWeek Then
            v = 1

            If TypeOf v Is Integer Then
                Console.WriteLine("Boxed as System.ValueType")
            End If
        End If

        Dim e As [Enum] = DayOfWeek.Tuesday

        If TypeOf e Is DayOfWeek Then
            Console.WriteLine("Boxed as System.Enum")
        End If

    End Sub
End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IBlockStatement (9 statements, 3 locals) (OperationKind.BlockStatement) (Syntax: 'Sub Main(ar ... End Sub')
  Locals: Local_1: o As System.Object
    Local_2: v As System.ValueType
    Local_3: e As System.Enum
  IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim o As Object = 1')
    IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'o')
      Variables: Local_1: o As System.Object
      Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: '1')
          Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
  IIfStatement (OperationKind.IfStatement) (Syntax: 'If TypeOf o ... End If')
    Condition: IIsTypeExpression (OperationKind.IsTypeExpression, Type: System.Boolean) (Syntax: 'TypeOf o Is Integer')
        Operand: ILocalReferenceExpression: o (OperationKind.LocalReferenceExpression, Type: System.Object) (Syntax: 'o')
        IsType: System.Int32
    IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'If TypeOf o ... End If')
        IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.Wri ... em.Object")')
          Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.String)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.Wri ... em.Object")')
              Instance Receiver: null
              Arguments(1):
                  IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '"Boxed as System.Object"')
                    ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "Boxed as System.Object") (Syntax: '"Boxed as System.Object"')
                    InConversion: null
                    OutConversion: null
    IfFalse: null
  IIfStatement (OperationKind.IfStatement) (Syntax: 'If TypeOf o ... End If')
    Condition: IIsTypeExpression (OperationKind.IsTypeExpression, Type: System.Boolean) (Syntax: 'TypeOf o Is IComparable')
        Operand: ILocalReferenceExpression: o (OperationKind.LocalReferenceExpression, Type: System.Object) (Syntax: 'o')
        IsType: System.IComparable
    IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'If TypeOf o ... End If')
        IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.Wri ... face type")')
          Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.String)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.Wri ... face type")')
              Instance Receiver: null
              Arguments(1):
                  IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '"Boxed as S ... rface type"')
                    ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "Boxed as System.Object to interface type") (Syntax: '"Boxed as S ... rface type"')
                    InConversion: null
                    OutConversion: null
    IfFalse: null
  IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim v As Va ... Week.Monday')
    IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'v')
      Variables: Local_1: v As System.ValueType
      Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.ValueType) (Syntax: 'DayOfWeek.Monday')
          Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: IFieldReferenceExpression: System.DayOfWeek.Monday (Static) (OperationKind.FieldReferenceExpression, Type: System.DayOfWeek, Constant: 1) (Syntax: 'DayOfWeek.Monday')
              Instance Receiver: null
  IIfStatement (OperationKind.IfStatement) (Syntax: 'If TypeOf v ... End If')
    Condition: IIsTypeExpression (OperationKind.IsTypeExpression, Type: System.Boolean) (Syntax: 'TypeOf v Is DayOfWeek')
        Operand: ILocalReferenceExpression: v (OperationKind.LocalReferenceExpression, Type: System.ValueType) (Syntax: 'v')
        IsType: System.DayOfWeek
    IfTrue: IBlockStatement (2 statements) (OperationKind.BlockStatement) (Syntax: 'If TypeOf v ... End If')
        IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'v = 1')
          Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.ValueType) (Syntax: 'v = 1')
              Left: ILocalReferenceExpression: v (OperationKind.LocalReferenceExpression, Type: System.ValueType) (Syntax: 'v')
              Right: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.ValueType) (Syntax: '1')
                  Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        IIfStatement (OperationKind.IfStatement) (Syntax: 'If TypeOf v ... End If')
          Condition: IIsTypeExpression (OperationKind.IsTypeExpression, Type: System.Boolean) (Syntax: 'TypeOf v Is Integer')
              Operand: ILocalReferenceExpression: v (OperationKind.LocalReferenceExpression, Type: System.ValueType) (Syntax: 'v')
              IsType: System.Int32
          IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'If TypeOf v ... End If')
              IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.Wri ... ValueType")')
                Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.String)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.Wri ... ValueType")')
                    Instance Receiver: null
                    Arguments(1):
                        IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '"Boxed as S ... .ValueType"')
                          ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "Boxed as System.ValueType") (Syntax: '"Boxed as S ... .ValueType"')
                          InConversion: null
                          OutConversion: null
          IfFalse: null
    IfFalse: null
  IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim e As [E ... eek.Tuesday')
    IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'e')
      Variables: Local_1: e As System.Enum
      Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Enum) (Syntax: 'DayOfWeek.Tuesday')
          Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: IFieldReferenceExpression: System.DayOfWeek.Tuesday (Static) (OperationKind.FieldReferenceExpression, Type: System.DayOfWeek, Constant: 2) (Syntax: 'DayOfWeek.Tuesday')
              Instance Receiver: null
  IIfStatement (OperationKind.IfStatement) (Syntax: 'If TypeOf e ... End If')
    Condition: IIsTypeExpression (OperationKind.IsTypeExpression, Type: System.Boolean) (Syntax: 'TypeOf e Is DayOfWeek')
        Operand: ILocalReferenceExpression: e (OperationKind.LocalReferenceExpression, Type: System.Enum) (Syntax: 'e')
        IsType: System.DayOfWeek
    IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'If TypeOf e ... End If')
        IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.Wri ... stem.Enum")')
          Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.String)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.Wri ... stem.Enum")')
              Instance Receiver: null
              Arguments(1):
                  IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '"Boxed as System.Enum"')
                    ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "Boxed as System.Enum") (Syntax: '"Boxed as System.Enum"')
                    InConversion: null
                    OutConversion: null
    IfFalse: null
  ILabelStatement (Label: exit) (OperationKind.LabelStatement) (Syntax: 'End Sub')
    LabeledStatement: null
  IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'End Sub')
    ReturnedValue: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact()>
        Public Sub ProducesNoErrorsWithClassTypeUnconstrainedTypeParameterTargetTypes()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module Program
    Sub Main(args As String())

    End Sub

    Sub M(Of T, TRef As Class, TVal As Structure, TBase As Class, TDerived As TBase)()'BIND:"Sub M(Of T, TRef As Class, TVal As Structure, TBase As Class, TDerived As TBase)()"

        Dim oT As T = Nothing

        If TypeOf oT Is TRef Then

        End If

        If TypeOf oT Is TVal Then

        End If

        Dim vVal As TVal = Nothing

        If TypeOf vVal Is TRef Then

        End If

        If TypeOf oT Is String Then

        End If

    End Sub
End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IBlockStatement (8 statements, 2 locals) (OperationKind.BlockStatement) (Syntax: 'Sub M(Of T, ... End Sub')
  Locals: Local_1: oT As T
    Local_2: vVal As TVal
  IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim oT As T = Nothing')
    IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'oT')
      Variables: Local_1: oT As T
      Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: T) (Syntax: 'Nothing')
          Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'Nothing')
  IIfStatement (OperationKind.IfStatement) (Syntax: 'If TypeOf o ... End If')
    Condition: IIsTypeExpression (OperationKind.IsTypeExpression, Type: System.Boolean) (Syntax: 'TypeOf oT Is TRef')
        Operand: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: 'TypeOf oT Is TRef')
            Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: ILocalReferenceExpression: oT (OperationKind.LocalReferenceExpression, Type: T) (Syntax: 'oT')
        IsType: TRef
    IfTrue: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'If TypeOf o ... End If')
    IfFalse: null
  IIfStatement (OperationKind.IfStatement) (Syntax: 'If TypeOf o ... End If')
    Condition: IIsTypeExpression (OperationKind.IsTypeExpression, Type: System.Boolean) (Syntax: 'TypeOf oT Is TVal')
        Operand: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: 'TypeOf oT Is TVal')
            Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: ILocalReferenceExpression: oT (OperationKind.LocalReferenceExpression, Type: T) (Syntax: 'oT')
        IsType: TVal
    IfTrue: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'If TypeOf o ... End If')
    IfFalse: null
  IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim vVal As ... l = Nothing')
    IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'vVal')
      Variables: Local_1: vVal As TVal
      Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: TVal) (Syntax: 'Nothing')
          Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'Nothing')
  IIfStatement (OperationKind.IfStatement) (Syntax: 'If TypeOf v ... End If')
    Condition: IIsTypeExpression (OperationKind.IsTypeExpression, Type: System.Boolean) (Syntax: 'TypeOf vVal Is TRef')
        Operand: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: 'TypeOf vVal Is TRef')
            Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: ILocalReferenceExpression: vVal (OperationKind.LocalReferenceExpression, Type: TVal) (Syntax: 'vVal')
        IsType: TRef
    IfTrue: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'If TypeOf v ... End If')
    IfFalse: null
  IIfStatement (OperationKind.IfStatement) (Syntax: 'If TypeOf o ... End If')
    Condition: IIsTypeExpression (OperationKind.IsTypeExpression, Type: System.Boolean) (Syntax: 'TypeOf oT Is String')
        Operand: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: 'TypeOf oT Is String')
            Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: ILocalReferenceExpression: oT (OperationKind.LocalReferenceExpression, Type: T) (Syntax: 'oT')
        IsType: System.String
    IfTrue: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'If TypeOf o ... End If')
    IfFalse: null
  ILabelStatement (Label: exit) (OperationKind.LabelStatement) (Syntax: 'End Sub')
    LabeledStatement: null
  IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'End Sub')
    ReturnedValue: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact>
        Public Sub ProducesErrorsWhenNoReferenceConversionExists()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module Program
    Public Sub Main()'BIND:"Public Sub Main()"

        Dim obj As Object = ""

        If TypeOf obj Is Program Then

        End If

        Dim s As String = Nothing

        If TypeOf s Is AppDomain Then

        ElseIf TypeOf s Is IDisposable Then

        End If

        If TypeOf s Is Integer Then

        End If

        Dim i As Integer = 0

        If TypeOf i Is String Then

        End If

    End Sub

End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IBlockStatement (9 statements, 3 locals) (OperationKind.BlockStatement, IsInvalid) (Syntax: 'Public Sub  ... End Sub')
  Locals: Local_1: obj As System.Object
    Local_2: s As System.String
    Local_3: i As System.Int32
  IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim obj As Object = ""')
    IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'obj')
      Variables: Local_1: obj As System.Object
      Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: '""')
          Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "") (Syntax: '""')
  IIfStatement (OperationKind.IfStatement, IsInvalid) (Syntax: 'If TypeOf o ... End If')
    Condition: IIsTypeExpression (OperationKind.IsTypeExpression, Type: System.Boolean, IsInvalid) (Syntax: 'TypeOf obj Is Program')
        Operand: ILocalReferenceExpression: obj (OperationKind.LocalReferenceExpression, Type: System.Object) (Syntax: 'obj')
        IsType: Program
    IfTrue: IBlockStatement (0 statements) (OperationKind.BlockStatement, IsInvalid) (Syntax: 'If TypeOf o ... End If')
    IfFalse: null
  IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim s As St ... g = Nothing')
    IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 's')
      Variables: Local_1: s As System.String
      Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.String, Constant: null) (Syntax: 'Nothing')
          Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'Nothing')
  IIfStatement (OperationKind.IfStatement, IsInvalid) (Syntax: 'If TypeOf s ... End If')
    Condition: IIsTypeExpression (OperationKind.IsTypeExpression, Type: System.Boolean, IsInvalid) (Syntax: 'TypeOf s Is AppDomain')
        Operand: ILocalReferenceExpression: s (OperationKind.LocalReferenceExpression, Type: System.String, IsInvalid) (Syntax: 's')
        IsType: System.AppDomain
    IfTrue: IBlockStatement (0 statements) (OperationKind.BlockStatement, IsInvalid) (Syntax: 'If TypeOf s ... End If')
    IfFalse: IIfStatement (OperationKind.IfStatement) (Syntax: 'ElseIf Type ... osable Then')
        Condition: IIsTypeExpression (OperationKind.IsTypeExpression, Type: System.Boolean) (Syntax: 'TypeOf s Is IDisposable')
            Operand: ILocalReferenceExpression: s (OperationKind.LocalReferenceExpression, Type: System.String) (Syntax: 's')
            IsType: System.IDisposable
        IfTrue: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'ElseIf Type ... osable Then')
        IfFalse: null
  IIfStatement (OperationKind.IfStatement, IsInvalid) (Syntax: 'If TypeOf s ... End If')
    Condition: IIsTypeExpression (OperationKind.IsTypeExpression, Type: System.Boolean, IsInvalid) (Syntax: 'TypeOf s Is Integer')
        Operand: ILocalReferenceExpression: s (OperationKind.LocalReferenceExpression, Type: System.String, IsInvalid) (Syntax: 's')
        IsType: System.Int32
    IfTrue: IBlockStatement (0 statements) (OperationKind.BlockStatement, IsInvalid) (Syntax: 'If TypeOf s ... End If')
    IfFalse: null
  IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim i As Integer = 0')
    IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i')
      Variables: Local_1: i As System.Int32
      Initializer: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
  IIfStatement (OperationKind.IfStatement, IsInvalid) (Syntax: 'If TypeOf i ... End If')
    Condition: IIsTypeExpression (OperationKind.IsTypeExpression, Type: System.Boolean, IsInvalid) (Syntax: 'TypeOf i Is String')
        Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32, IsInvalid) (Syntax: 'i')
        IsType: System.String
    IfTrue: IBlockStatement (0 statements) (OperationKind.BlockStatement, IsInvalid) (Syntax: 'If TypeOf i ... End If')
    IfFalse: null
  ILabelStatement (Label: exit) (OperationKind.LabelStatement) (Syntax: 'End Sub')
    LabeledStatement: null
  IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'End Sub')
    ReturnedValue: null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30371: Module 'Program' cannot be used as a type.
        If TypeOf obj Is Program Then
                         ~~~~~~~
BC31430: Expression of type 'String' can never be of type 'AppDomain'.
        If TypeOf s Is AppDomain Then
           ~~~~~~~~~~~~~~~~~~~~~
BC31430: Expression of type 'String' can never be of type 'Integer'.
        If TypeOf s Is Integer Then
           ~~~~~~~~~~~~~~~~~~~
BC30021: 'TypeOf ... Is' requires its left operand to have a reference type, but this operand has the value type 'Integer'.
        If TypeOf i Is String Then
                  ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact()>
        Public Sub ProducesErrorsWhenNoReferenceConversionExistsBetweenConstrainedTypeParameterTypes()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module Program

    Sub Main()
    End Sub

    Class A : End Class
    Class B : End Class

    Sub M(Of T, TA As A, TB As B, TC As Structure, TD As IDisposable)(x As T, a As TA, b As TB, c As TC, d As TD, s As String)'BIND:"Sub M(Of T, TA As A, TB As B, TC As Structure, TD As IDisposable)(x As T, a As TA, b As TB, c As TC, d As TD, s As String)"

        If TypeOf x Is TA OrElse TypeOf x Is TB OrElse TypeOf x Is TC OrElse TypeOf x Is TD OrElse TypeOf x Is String Then
            Console.WriteLine("Success!")
        End If

        If TypeOf a Is TB OrElse TypeOf a Is TC OrElse TypeOf a Is String Then
            Console.WriteLine("Fail!")
        ElseIf TypeOf a Is T OrElse TypeOf a Is TD Then
            Console.WriteLine("Success!")
        End If

        If TypeOf b Is TA OrElse TypeOf b Is TC OrElse TypeOf b Is String Then
            Console.WriteLine("Fail!")
        ElseIf TypeOf b Is T OrElse TypeOf b Is TD Then
            Console.WriteLine("Success!")
        End If

        If TypeOf c Is TA OrElse TypeOf c Is TB OrElse TypeOf c Is String Then
            Console.WriteLine("Fail!")
        ElseIf TypeOf c Is T OrElse TypeOf c Is TD Then
            Console.WriteLine("Success!")
        End If

        If TypeOf d Is T OrElse TypeOf d Is TA OrElse TypeOf d Is TB OrElse TypeOf d Is TC OrElse TypeOf d Is String Then
            Console.WriteLine("Success!")
        End If

        If TypeOf s Is TA OrElse TypeOf s Is TB OrElse TypeOf s Is TC Then
            Console.WriteLine("Fail!")
        ElseIf TypeOf s Is T OrElse TypeOf s Is T OrElse TypeOf s Is TD Then
            Console.WriteLine("Success!")
        End If

    End Sub

End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IBlockStatement (8 statements) (OperationKind.BlockStatement, IsInvalid) (Syntax: 'Sub M(Of T, ... End Sub')
  IIfStatement (OperationKind.IfStatement) (Syntax: 'If TypeOf x ... End If')
    Condition: IBinaryOperatorExpression (BinaryOperatorKind.ConditionalOr, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'TypeOf x Is ... x Is String')
        Left: IBinaryOperatorExpression (BinaryOperatorKind.ConditionalOr, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'TypeOf x Is ... eOf x Is TD')
            Left: IBinaryOperatorExpression (BinaryOperatorKind.ConditionalOr, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'TypeOf x Is ... eOf x Is TC')
                Left: IBinaryOperatorExpression (BinaryOperatorKind.ConditionalOr, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'TypeOf x Is ... eOf x Is TB')
                    Left: IIsTypeExpression (OperationKind.IsTypeExpression, Type: System.Boolean) (Syntax: 'TypeOf x Is TA')
                        Operand: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: 'TypeOf x Is TA')
                            Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            Operand: IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: T) (Syntax: 'x')
                        IsType: TA
                    Right: IIsTypeExpression (OperationKind.IsTypeExpression, Type: System.Boolean) (Syntax: 'TypeOf x Is TB')
                        Operand: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: 'TypeOf x Is TB')
                            Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            Operand: IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: T) (Syntax: 'x')
                        IsType: TB
                Right: IIsTypeExpression (OperationKind.IsTypeExpression, Type: System.Boolean) (Syntax: 'TypeOf x Is TC')
                    Operand: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: 'TypeOf x Is TC')
                        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        Operand: IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: T) (Syntax: 'x')
                    IsType: TC
            Right: IIsTypeExpression (OperationKind.IsTypeExpression, Type: System.Boolean) (Syntax: 'TypeOf x Is TD')
                Operand: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: 'TypeOf x Is TD')
                    Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: T) (Syntax: 'x')
                IsType: TD
        Right: IIsTypeExpression (OperationKind.IsTypeExpression, Type: System.Boolean) (Syntax: 'TypeOf x Is String')
            Operand: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: 'TypeOf x Is String')
                Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: T) (Syntax: 'x')
            IsType: System.String
    IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'If TypeOf x ... End If')
        IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.Wri ... "Success!")')
          Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.String)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.Wri ... "Success!")')
              Instance Receiver: null
              Arguments(1):
                  IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '"Success!"')
                    ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "Success!") (Syntax: '"Success!"')
                    InConversion: null
                    OutConversion: null
    IfFalse: null
  IIfStatement (OperationKind.IfStatement, IsInvalid) (Syntax: 'If TypeOf a ... End If')
    Condition: IBinaryOperatorExpression (BinaryOperatorKind.ConditionalOr, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean, IsInvalid) (Syntax: 'TypeOf a Is ... a Is String')
        Left: IBinaryOperatorExpression (BinaryOperatorKind.ConditionalOr, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean, IsInvalid) (Syntax: 'TypeOf a Is ... eOf a Is TC')
            Left: IIsTypeExpression (OperationKind.IsTypeExpression, Type: System.Boolean, IsInvalid) (Syntax: 'TypeOf a Is TB')
                Operand: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object, IsInvalid) (Syntax: 'TypeOf a Is TB')
                    Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: IParameterReferenceExpression: a (OperationKind.ParameterReferenceExpression, Type: TA, IsInvalid) (Syntax: 'a')
                IsType: TB
            Right: IIsTypeExpression (OperationKind.IsTypeExpression, Type: System.Boolean, IsInvalid) (Syntax: 'TypeOf a Is TC')
                Operand: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object, IsInvalid) (Syntax: 'TypeOf a Is TC')
                    Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: IParameterReferenceExpression: a (OperationKind.ParameterReferenceExpression, Type: TA, IsInvalid) (Syntax: 'a')
                IsType: TC
        Right: IIsTypeExpression (OperationKind.IsTypeExpression, Type: System.Boolean, IsInvalid) (Syntax: 'TypeOf a Is String')
            Operand: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object, IsInvalid) (Syntax: 'TypeOf a Is String')
                Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: IParameterReferenceExpression: a (OperationKind.ParameterReferenceExpression, Type: TA, IsInvalid) (Syntax: 'a')
            IsType: System.String
    IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement, IsInvalid) (Syntax: 'If TypeOf a ... End If')
        IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.Wri ... ne("Fail!")')
          Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.String)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.Wri ... ne("Fail!")')
              Instance Receiver: null
              Arguments(1):
                  IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '"Fail!"')
                    ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "Fail!") (Syntax: '"Fail!"')
                    InConversion: null
                    OutConversion: null
    IfFalse: IIfStatement (OperationKind.IfStatement) (Syntax: 'ElseIf Type ... "Success!")')
        Condition: IBinaryOperatorExpression (BinaryOperatorKind.ConditionalOr, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'TypeOf a Is ... eOf a Is TD')
            Left: IIsTypeExpression (OperationKind.IsTypeExpression, Type: System.Boolean) (Syntax: 'TypeOf a Is T')
                Operand: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: 'TypeOf a Is T')
                    Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: IParameterReferenceExpression: a (OperationKind.ParameterReferenceExpression, Type: TA) (Syntax: 'a')
                IsType: T
            Right: IIsTypeExpression (OperationKind.IsTypeExpression, Type: System.Boolean) (Syntax: 'TypeOf a Is TD')
                Operand: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: 'TypeOf a Is TD')
                    Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: IParameterReferenceExpression: a (OperationKind.ParameterReferenceExpression, Type: TA) (Syntax: 'a')
                IsType: TD
        IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'ElseIf Type ... "Success!")')
            IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.Wri ... "Success!")')
              Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.String)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.Wri ... "Success!")')
                  Instance Receiver: null
                  Arguments(1):
                      IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '"Success!"')
                        ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "Success!") (Syntax: '"Success!"')
                        InConversion: null
                        OutConversion: null
        IfFalse: null
  IIfStatement (OperationKind.IfStatement, IsInvalid) (Syntax: 'If TypeOf b ... End If')
    Condition: IBinaryOperatorExpression (BinaryOperatorKind.ConditionalOr, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean, IsInvalid) (Syntax: 'TypeOf b Is ... b Is String')
        Left: IBinaryOperatorExpression (BinaryOperatorKind.ConditionalOr, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean, IsInvalid) (Syntax: 'TypeOf b Is ... eOf b Is TC')
            Left: IIsTypeExpression (OperationKind.IsTypeExpression, Type: System.Boolean, IsInvalid) (Syntax: 'TypeOf b Is TA')
                Operand: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object, IsInvalid) (Syntax: 'TypeOf b Is TA')
                    Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: IParameterReferenceExpression: b (OperationKind.ParameterReferenceExpression, Type: TB, IsInvalid) (Syntax: 'b')
                IsType: TA
            Right: IIsTypeExpression (OperationKind.IsTypeExpression, Type: System.Boolean, IsInvalid) (Syntax: 'TypeOf b Is TC')
                Operand: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object, IsInvalid) (Syntax: 'TypeOf b Is TC')
                    Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: IParameterReferenceExpression: b (OperationKind.ParameterReferenceExpression, Type: TB, IsInvalid) (Syntax: 'b')
                IsType: TC
        Right: IIsTypeExpression (OperationKind.IsTypeExpression, Type: System.Boolean, IsInvalid) (Syntax: 'TypeOf b Is String')
            Operand: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object, IsInvalid) (Syntax: 'TypeOf b Is String')
                Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: IParameterReferenceExpression: b (OperationKind.ParameterReferenceExpression, Type: TB, IsInvalid) (Syntax: 'b')
            IsType: System.String
    IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement, IsInvalid) (Syntax: 'If TypeOf b ... End If')
        IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.Wri ... ne("Fail!")')
          Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.String)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.Wri ... ne("Fail!")')
              Instance Receiver: null
              Arguments(1):
                  IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '"Fail!"')
                    ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "Fail!") (Syntax: '"Fail!"')
                    InConversion: null
                    OutConversion: null
    IfFalse: IIfStatement (OperationKind.IfStatement) (Syntax: 'ElseIf Type ... "Success!")')
        Condition: IBinaryOperatorExpression (BinaryOperatorKind.ConditionalOr, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'TypeOf b Is ... eOf b Is TD')
            Left: IIsTypeExpression (OperationKind.IsTypeExpression, Type: System.Boolean) (Syntax: 'TypeOf b Is T')
                Operand: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: 'TypeOf b Is T')
                    Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: IParameterReferenceExpression: b (OperationKind.ParameterReferenceExpression, Type: TB) (Syntax: 'b')
                IsType: T
            Right: IIsTypeExpression (OperationKind.IsTypeExpression, Type: System.Boolean) (Syntax: 'TypeOf b Is TD')
                Operand: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: 'TypeOf b Is TD')
                    Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: IParameterReferenceExpression: b (OperationKind.ParameterReferenceExpression, Type: TB) (Syntax: 'b')
                IsType: TD
        IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'ElseIf Type ... "Success!")')
            IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.Wri ... "Success!")')
              Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.String)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.Wri ... "Success!")')
                  Instance Receiver: null
                  Arguments(1):
                      IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '"Success!"')
                        ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "Success!") (Syntax: '"Success!"')
                        InConversion: null
                        OutConversion: null
        IfFalse: null
  IIfStatement (OperationKind.IfStatement, IsInvalid) (Syntax: 'If TypeOf c ... End If')
    Condition: IBinaryOperatorExpression (BinaryOperatorKind.ConditionalOr, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean, IsInvalid) (Syntax: 'TypeOf c Is ... c Is String')
        Left: IBinaryOperatorExpression (BinaryOperatorKind.ConditionalOr, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean, IsInvalid) (Syntax: 'TypeOf c Is ... eOf c Is TB')
            Left: IIsTypeExpression (OperationKind.IsTypeExpression, Type: System.Boolean, IsInvalid) (Syntax: 'TypeOf c Is TA')
                Operand: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object, IsInvalid) (Syntax: 'TypeOf c Is TA')
                    Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: IParameterReferenceExpression: c (OperationKind.ParameterReferenceExpression, Type: TC, IsInvalid) (Syntax: 'c')
                IsType: TA
            Right: IIsTypeExpression (OperationKind.IsTypeExpression, Type: System.Boolean, IsInvalid) (Syntax: 'TypeOf c Is TB')
                Operand: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object, IsInvalid) (Syntax: 'TypeOf c Is TB')
                    Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: IParameterReferenceExpression: c (OperationKind.ParameterReferenceExpression, Type: TC, IsInvalid) (Syntax: 'c')
                IsType: TB
        Right: IIsTypeExpression (OperationKind.IsTypeExpression, Type: System.Boolean, IsInvalid) (Syntax: 'TypeOf c Is String')
            Operand: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object, IsInvalid) (Syntax: 'TypeOf c Is String')
                Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: IParameterReferenceExpression: c (OperationKind.ParameterReferenceExpression, Type: TC, IsInvalid) (Syntax: 'c')
            IsType: System.String
    IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement, IsInvalid) (Syntax: 'If TypeOf c ... End If')
        IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.Wri ... ne("Fail!")')
          Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.String)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.Wri ... ne("Fail!")')
              Instance Receiver: null
              Arguments(1):
                  IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '"Fail!"')
                    ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "Fail!") (Syntax: '"Fail!"')
                    InConversion: null
                    OutConversion: null
    IfFalse: IIfStatement (OperationKind.IfStatement) (Syntax: 'ElseIf Type ... "Success!")')
        Condition: IBinaryOperatorExpression (BinaryOperatorKind.ConditionalOr, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'TypeOf c Is ... eOf c Is TD')
            Left: IIsTypeExpression (OperationKind.IsTypeExpression, Type: System.Boolean) (Syntax: 'TypeOf c Is T')
                Operand: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: 'TypeOf c Is T')
                    Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: IParameterReferenceExpression: c (OperationKind.ParameterReferenceExpression, Type: TC) (Syntax: 'c')
                IsType: T
            Right: IIsTypeExpression (OperationKind.IsTypeExpression, Type: System.Boolean) (Syntax: 'TypeOf c Is TD')
                Operand: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: 'TypeOf c Is TD')
                    Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: IParameterReferenceExpression: c (OperationKind.ParameterReferenceExpression, Type: TC) (Syntax: 'c')
                IsType: TD
        IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'ElseIf Type ... "Success!")')
            IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.Wri ... "Success!")')
              Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.String)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.Wri ... "Success!")')
                  Instance Receiver: null
                  Arguments(1):
                      IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '"Success!"')
                        ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "Success!") (Syntax: '"Success!"')
                        InConversion: null
                        OutConversion: null
        IfFalse: null
  IIfStatement (OperationKind.IfStatement) (Syntax: 'If TypeOf d ... End If')
    Condition: IBinaryOperatorExpression (BinaryOperatorKind.ConditionalOr, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'TypeOf d Is ... d Is String')
        Left: IBinaryOperatorExpression (BinaryOperatorKind.ConditionalOr, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'TypeOf d Is ... eOf d Is TC')
            Left: IBinaryOperatorExpression (BinaryOperatorKind.ConditionalOr, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'TypeOf d Is ... eOf d Is TB')
                Left: IBinaryOperatorExpression (BinaryOperatorKind.ConditionalOr, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'TypeOf d Is ... eOf d Is TA')
                    Left: IIsTypeExpression (OperationKind.IsTypeExpression, Type: System.Boolean) (Syntax: 'TypeOf d Is T')
                        Operand: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: 'TypeOf d Is T')
                            Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            Operand: IParameterReferenceExpression: d (OperationKind.ParameterReferenceExpression, Type: TD) (Syntax: 'd')
                        IsType: T
                    Right: IIsTypeExpression (OperationKind.IsTypeExpression, Type: System.Boolean) (Syntax: 'TypeOf d Is TA')
                        Operand: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: 'TypeOf d Is TA')
                            Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            Operand: IParameterReferenceExpression: d (OperationKind.ParameterReferenceExpression, Type: TD) (Syntax: 'd')
                        IsType: TA
                Right: IIsTypeExpression (OperationKind.IsTypeExpression, Type: System.Boolean) (Syntax: 'TypeOf d Is TB')
                    Operand: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: 'TypeOf d Is TB')
                        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        Operand: IParameterReferenceExpression: d (OperationKind.ParameterReferenceExpression, Type: TD) (Syntax: 'd')
                    IsType: TB
            Right: IIsTypeExpression (OperationKind.IsTypeExpression, Type: System.Boolean) (Syntax: 'TypeOf d Is TC')
                Operand: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: 'TypeOf d Is TC')
                    Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: IParameterReferenceExpression: d (OperationKind.ParameterReferenceExpression, Type: TD) (Syntax: 'd')
                IsType: TC
        Right: IIsTypeExpression (OperationKind.IsTypeExpression, Type: System.Boolean) (Syntax: 'TypeOf d Is String')
            Operand: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: 'TypeOf d Is String')
                Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: IParameterReferenceExpression: d (OperationKind.ParameterReferenceExpression, Type: TD) (Syntax: 'd')
            IsType: System.String
    IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'If TypeOf d ... End If')
        IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.Wri ... "Success!")')
          Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.String)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.Wri ... "Success!")')
              Instance Receiver: null
              Arguments(1):
                  IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '"Success!"')
                    ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "Success!") (Syntax: '"Success!"')
                    InConversion: null
                    OutConversion: null
    IfFalse: null
  IIfStatement (OperationKind.IfStatement, IsInvalid) (Syntax: 'If TypeOf s ... End If')
    Condition: IBinaryOperatorExpression (BinaryOperatorKind.ConditionalOr, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean, IsInvalid) (Syntax: 'TypeOf s Is ... eOf s Is TC')
        Left: IBinaryOperatorExpression (BinaryOperatorKind.ConditionalOr, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean, IsInvalid) (Syntax: 'TypeOf s Is ... eOf s Is TB')
            Left: IIsTypeExpression (OperationKind.IsTypeExpression, Type: System.Boolean, IsInvalid) (Syntax: 'TypeOf s Is TA')
                Operand: IParameterReferenceExpression: s (OperationKind.ParameterReferenceExpression, Type: System.String, IsInvalid) (Syntax: 's')
                IsType: TA
            Right: IIsTypeExpression (OperationKind.IsTypeExpression, Type: System.Boolean, IsInvalid) (Syntax: 'TypeOf s Is TB')
                Operand: IParameterReferenceExpression: s (OperationKind.ParameterReferenceExpression, Type: System.String, IsInvalid) (Syntax: 's')
                IsType: TB
        Right: IIsTypeExpression (OperationKind.IsTypeExpression, Type: System.Boolean, IsInvalid) (Syntax: 'TypeOf s Is TC')
            Operand: IParameterReferenceExpression: s (OperationKind.ParameterReferenceExpression, Type: System.String, IsInvalid) (Syntax: 's')
            IsType: TC
    IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement, IsInvalid) (Syntax: 'If TypeOf s ... End If')
        IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.Wri ... ne("Fail!")')
          Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.String)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.Wri ... ne("Fail!")')
              Instance Receiver: null
              Arguments(1):
                  IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '"Fail!"')
                    ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "Fail!") (Syntax: '"Fail!"')
                    InConversion: null
                    OutConversion: null
    IfFalse: IIfStatement (OperationKind.IfStatement) (Syntax: 'ElseIf Type ... "Success!")')
        Condition: IBinaryOperatorExpression (BinaryOperatorKind.ConditionalOr, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'TypeOf s Is ... eOf s Is TD')
            Left: IBinaryOperatorExpression (BinaryOperatorKind.ConditionalOr, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'TypeOf s Is ... peOf s Is T')
                Left: IIsTypeExpression (OperationKind.IsTypeExpression, Type: System.Boolean) (Syntax: 'TypeOf s Is T')
                    Operand: IParameterReferenceExpression: s (OperationKind.ParameterReferenceExpression, Type: System.String) (Syntax: 's')
                    IsType: T
                Right: IIsTypeExpression (OperationKind.IsTypeExpression, Type: System.Boolean) (Syntax: 'TypeOf s Is T')
                    Operand: IParameterReferenceExpression: s (OperationKind.ParameterReferenceExpression, Type: System.String) (Syntax: 's')
                    IsType: T
            Right: IIsTypeExpression (OperationKind.IsTypeExpression, Type: System.Boolean) (Syntax: 'TypeOf s Is TD')
                Operand: IParameterReferenceExpression: s (OperationKind.ParameterReferenceExpression, Type: System.String) (Syntax: 's')
                IsType: TD
        IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'ElseIf Type ... "Success!")')
            IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.Wri ... "Success!")')
              Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.String)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.Wri ... "Success!")')
                  Instance Receiver: null
                  Arguments(1):
                      IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '"Success!"')
                        ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "Success!") (Syntax: '"Success!"')
                        InConversion: null
                        OutConversion: null
        IfFalse: null
  ILabelStatement (Label: exit) (OperationKind.LabelStatement) (Syntax: 'End Sub')
    LabeledStatement: null
  IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'End Sub')
    ReturnedValue: null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC31430: Expression of type 'TA' can never be of type 'TB'.
        If TypeOf a Is TB OrElse TypeOf a Is TC OrElse TypeOf a Is String Then
           ~~~~~~~~~~~~~~
BC31430: Expression of type 'TA' can never be of type 'TC'.
        If TypeOf a Is TB OrElse TypeOf a Is TC OrElse TypeOf a Is String Then
                                 ~~~~~~~~~~~~~~
BC31430: Expression of type 'TA' can never be of type 'String'.
        If TypeOf a Is TB OrElse TypeOf a Is TC OrElse TypeOf a Is String Then
                                                       ~~~~~~~~~~~~~~~~~~
BC31430: Expression of type 'TB' can never be of type 'TA'.
        If TypeOf b Is TA OrElse TypeOf b Is TC OrElse TypeOf b Is String Then
           ~~~~~~~~~~~~~~
BC31430: Expression of type 'TB' can never be of type 'TC'.
        If TypeOf b Is TA OrElse TypeOf b Is TC OrElse TypeOf b Is String Then
                                 ~~~~~~~~~~~~~~
BC31430: Expression of type 'TB' can never be of type 'String'.
        If TypeOf b Is TA OrElse TypeOf b Is TC OrElse TypeOf b Is String Then
                                                       ~~~~~~~~~~~~~~~~~~
BC31430: Expression of type 'TC' can never be of type 'TA'.
        If TypeOf c Is TA OrElse TypeOf c Is TB OrElse TypeOf c Is String Then
           ~~~~~~~~~~~~~~
BC31430: Expression of type 'TC' can never be of type 'TB'.
        If TypeOf c Is TA OrElse TypeOf c Is TB OrElse TypeOf c Is String Then
                                 ~~~~~~~~~~~~~~
BC31430: Expression of type 'TC' can never be of type 'String'.
        If TypeOf c Is TA OrElse TypeOf c Is TB OrElse TypeOf c Is String Then
                                                       ~~~~~~~~~~~~~~~~~~
BC31430: Expression of type 'String' can never be of type 'TA'.
        If TypeOf s Is TA OrElse TypeOf s Is TB OrElse TypeOf s Is TC Then
           ~~~~~~~~~~~~~~
BC31430: Expression of type 'String' can never be of type 'TB'.
        If TypeOf s Is TA OrElse TypeOf s Is TB OrElse TypeOf s Is TC Then
                                 ~~~~~~~~~~~~~~
BC31430: Expression of type 'String' can never be of type 'TC'.
        If TypeOf s Is TA OrElse TypeOf s Is TB OrElse TypeOf s Is TC Then
                                                       ~~~~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact>
        Public Sub SeesThroughTypeAndNamespaceAliases()
            Dim source =
<compilation name="SeesThroughTypeAndNamespaceAliases">
    <file name="Program.vb">
Option Strict On

Imports HRESULT = System.Int32
Imports CharacterSequence = System.String

Module Program
    Sub Main(args As String())'BIND:"Sub Main(args As String())"

        Dim o As Object = ""

        Dim isString = TypeOf o Is CharacterSequence

        Dim isInteger = TypeOf o Is HRESULT

        Dim isNotString = TypeOf o IsNot CharacterSequence

        Dim isNotInteger = TypeOf o IsNot HRESULT 

        System.Console.WriteLine(isString)
        System.Console.WriteLine(isInteger)
        System.Console.WriteLine(isNotString)
        System.Console.WriteLine(isNotInteger)

    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, , TestOptions.ReleaseExe)

            Dim expectedOperationTree = <![CDATA[
IBlockStatement (11 statements, 5 locals) (OperationKind.BlockStatement) (Syntax: 'Sub Main(ar ... End Sub')
  Locals: Local_1: o As System.Object
    Local_2: isString As System.Boolean
    Local_3: isInteger As System.Boolean
    Local_4: isNotString As System.Boolean
    Local_5: isNotInteger As System.Boolean
  IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim o As Object = ""')
    IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'o')
      Variables: Local_1: o As System.Object
      Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: '""')
          Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "") (Syntax: '""')
  IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim isStrin ... terSequence')
    IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'isString')
      Variables: Local_1: isString As System.Boolean
      Initializer: IIsTypeExpression (OperationKind.IsTypeExpression, Type: System.Boolean) (Syntax: 'TypeOf o Is ... terSequence')
          Operand: ILocalReferenceExpression: o (OperationKind.LocalReferenceExpression, Type: System.Object) (Syntax: 'o')
          IsType: System.String
  IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim isInteg ...  Is HRESULT')
    IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'isInteger')
      Variables: Local_1: isInteger As System.Boolean
      Initializer: IIsTypeExpression (OperationKind.IsTypeExpression, Type: System.Boolean) (Syntax: 'TypeOf o Is HRESULT')
          Operand: ILocalReferenceExpression: o (OperationKind.LocalReferenceExpression, Type: System.Object) (Syntax: 'o')
          IsType: System.Int32
  IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim isNotSt ... terSequence')
    IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'isNotString')
      Variables: Local_1: isNotString As System.Boolean
      Initializer: IIsTypeExpression (IsNotExpression) (OperationKind.IsTypeExpression, Type: System.Boolean) (Syntax: 'TypeOf o Is ... terSequence')
          Operand: ILocalReferenceExpression: o (OperationKind.LocalReferenceExpression, Type: System.Object) (Syntax: 'o')
          IsType: System.String
  IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim isNotIn ... Not HRESULT')
    IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'isNotInteger')
      Variables: Local_1: isNotInteger As System.Boolean
      Initializer: IIsTypeExpression (IsNotExpression) (OperationKind.IsTypeExpression, Type: System.Boolean) (Syntax: 'TypeOf o IsNot HRESULT')
          Operand: ILocalReferenceExpression: o (OperationKind.LocalReferenceExpression, Type: System.Object) (Syntax: 'o')
          IsType: System.Int32
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... e(isString)')
    Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.Boolean)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... e(isString)')
        Instance Receiver: null
        Arguments(1):
            IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'isString')
              ILocalReferenceExpression: isString (OperationKind.LocalReferenceExpression, Type: System.Boolean) (Syntax: 'isString')
              InConversion: null
              OutConversion: null
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... (isInteger)')
    Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.Boolean)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... (isInteger)')
        Instance Receiver: null
        Arguments(1):
            IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'isInteger')
              ILocalReferenceExpression: isInteger (OperationKind.LocalReferenceExpression, Type: System.Boolean) (Syntax: 'isInteger')
              InConversion: null
              OutConversion: null
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... sNotString)')
    Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.Boolean)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... sNotString)')
        Instance Receiver: null
        Arguments(1):
            IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'isNotString')
              ILocalReferenceExpression: isNotString (OperationKind.LocalReferenceExpression, Type: System.Boolean) (Syntax: 'isNotString')
              InConversion: null
              OutConversion: null
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... NotInteger)')
    Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.Boolean)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... NotInteger)')
        Instance Receiver: null
        Arguments(1):
            IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'isNotInteger')
              ILocalReferenceExpression: isNotInteger (OperationKind.LocalReferenceExpression, Type: System.Boolean) (Syntax: 'isNotInteger')
              InConversion: null
              OutConversion: null
  ILabelStatement (Label: exit) (OperationKind.LabelStatement) (Syntax: 'End Sub')
    LabeledStatement: null
  IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'End Sub')
    ReturnedValue: null
]]>.Value

            Dim expectedDiagnostics = String.Empty
            Dim fileName = "Program.vb"
            VerifyOperationTreeAndDiagnosticsForTest(Of MethodBlockSyntax)(compilation, fileName, expectedOperationTree, expectedDiagnostics)

            CompileAndVerify(compilation, <![CDATA[
True
False
False
True
]]>)

        End Sub

        <Fact>
        Public Sub ReturnsExpectedValuesFromSemanticModelApi()

            Dim source =
<compilation name="ReturnsExpectedValuesFromSemanticModelApi">
    <file name="Program.vb">
Option Strict On

Imports HRESULT = System.Int32
Imports CharacterSequence = System.String

Module Program
    Sub Main(args As String())

        Dim o As Object = ""

        Dim isString = TypeOf o Is CharacterSequence '0

        Dim isInteger = TypeOf o Is HRESULT '1

        Dim isNotString = TypeOf o IsNot String '2

        Dim isNotInteger As Object = TypeOf o IsNot Integer '3

        If TypeOf CObj(isString) Is Boolean Then '4
            System.Console.WriteLine(True)
        End If

        System.Console.WriteLine(isString)
        System.Console.WriteLine(isInteger)
        System.Console.WriteLine(isNotString)
        System.Console.WriteLine(isNotInteger)

        System.Console.WriteLine(TypeOf "" Is String) '5

    End Sub

End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, , TestOptions.ReleaseExe)

            CompilationUtils.AssertNoErrors(compilation)

            Dim semantics = compilation.GetSemanticModel(compilation.SyntaxTrees(0))

            Dim typeOfExpressions = compilation.SyntaxTrees(0).GetCompilationUnitRoot().DescendantNodes.OfType(Of TypeOfExpressionSyntax).ToArray()

            ' Dim isString = TypeOf o Is CharacterSequence '0
            Assert.Equal("System.Boolean", semantics.GetTypeInfo(typeOfExpressions(0)).Type.ToDisplayString(SymbolDisplayFormat.TestFormat))
            Assert.Equal("System.String", semantics.GetSymbolInfo(typeOfExpressions(0).Type).Symbol.ToDisplayString(SymbolDisplayFormat.TestFormat))

            ' Dim isInteger = TypeOf o Is HRESULT '1
            Dim aliasSymbol = semantics.GetAliasInfo(CType(typeOfExpressions(1).Type, IdentifierNameSyntax))
            Assert.Equal(SymbolKind.Alias, aliasSymbol.Kind)
            Assert.Equal("HRESULT", aliasSymbol.Name)
            Assert.Equal("System.Int32", semantics.GetSymbolInfo(typeOfExpressions(1).Type).Symbol.ToDisplayString(SymbolDisplayFormat.TestFormat))

            ' Dim isNotString = TypeOf o IsNot String '2
            Assert.Equal("System.Boolean", semantics.GetTypeInfo(typeOfExpressions(2)).Type.ToDisplayString(SymbolDisplayFormat.TestFormat))
            Assert.Equal("System.String", semantics.GetSymbolInfo(typeOfExpressions(2).Type).Symbol.ToDisplayString(SymbolDisplayFormat.TestFormat))

            ' Dim isNotInteger As Object = TypeOf o IsNot Integer '3
            Dim typeInfo = semantics.GetTypeInfo(typeOfExpressions(3))
            Dim conv = semantics.GetConversion(typeOfExpressions(3))
            Assert.Equal(ConversionKind.WideningValue, conv.Kind)
            Assert.Equal(SpecialType.System_Object, typeInfo.ConvertedType.SpecialType)

            ' If TypeOf CObj(isString) Is Boolean Then '4
            Dim symbolInfo = semantics.GetSymbolInfo(CType(typeOfExpressions(4).Expression, PredefinedCastExpressionSyntax).Expression)
            Assert.Equal(SymbolKind.Local, symbolInfo.Symbol.Kind)
            Assert.Equal("isString", symbolInfo.Symbol.Name)

            Dim expressionAnalysis = semantics.AnalyzeDataFlow(typeOfExpressions(4))

            Assert.DoesNotContain(symbolInfo.Symbol, expressionAnalysis.AlwaysAssigned)
            Assert.DoesNotContain(symbolInfo.Symbol, expressionAnalysis.Captured)
            Assert.Contains(symbolInfo.Symbol, expressionAnalysis.DataFlowsIn)
            Assert.DoesNotContain(symbolInfo.Symbol, expressionAnalysis.DataFlowsOut)
            Assert.Contains(symbolInfo.Symbol, expressionAnalysis.ReadInside)
            Assert.Contains(symbolInfo.Symbol, expressionAnalysis.ReadOutside)
            Assert.True(expressionAnalysis.Succeeded)
            Assert.DoesNotContain(symbolInfo.Symbol, expressionAnalysis.VariablesDeclared)
            Assert.DoesNotContain(symbolInfo.Symbol, expressionAnalysis.WrittenInside)
            Assert.Contains(symbolInfo.Symbol, expressionAnalysis.WrittenOutside)

            Dim statementDataAnalysis = semantics.AnalyzeDataFlow(CType(typeOfExpressions(4).Parent.Parent, StatementSyntax))

            AssertSequenceEqual(expressionAnalysis.AlwaysAssigned, statementDataAnalysis.AlwaysAssigned)
            AssertSequenceEqual(expressionAnalysis.Captured, statementDataAnalysis.Captured)
            AssertSequenceEqual(expressionAnalysis.DataFlowsIn, statementDataAnalysis.DataFlowsIn)
            AssertSequenceEqual(expressionAnalysis.DataFlowsOut, statementDataAnalysis.DataFlowsOut)
            AssertSequenceEqual(expressionAnalysis.ReadInside, statementDataAnalysis.ReadInside)
            AssertSequenceEqual(expressionAnalysis.ReadOutside, statementDataAnalysis.ReadOutside)
            Assert.Equal(expressionAnalysis.Succeeded, statementDataAnalysis.Succeeded)
            AssertSequenceEqual(expressionAnalysis.VariablesDeclared, statementDataAnalysis.VariablesDeclared)
            AssertSequenceEqual(expressionAnalysis.WrittenInside, statementDataAnalysis.WrittenInside)
            AssertSequenceEqual(expressionAnalysis.WrittenOutside, statementDataAnalysis.WrittenOutside)

            Assert.False(semantics.GetConstantValue(typeOfExpressions(5)).HasValue)

        End Sub

        Private Shared Sub AssertSequenceEqual(Of TElement)(a1 As ImmutableArray(Of TElement), a2 As ImmutableArray(Of TElement))
            Assert.Equal(a1.Length, a2.Length)
            For i As Integer = 0 To a1.Length - 1
                Assert.Equal(a1(i), a2(i))
            Next
        End Sub

        <Fact>
        Public Sub ExecutesCorrectlyInExpression()
            Dim source =
<compilation name="ExecutesCorrectlyInExpression">
    <file name="Program.vb">
Option Strict On
Imports System
Module Program
    Sub Main(args As String())

        Dim o As Object = ""

        Dim isString = TypeOf o Is String

        Dim isInteger = TypeOf o Is Integer

        Dim isNotString = TypeOf o IsNot String

        Dim isNotInteger = TypeOf o IsNot Integer 

        Console.WriteLine(isString)
        Console.WriteLine(isInteger)
        Console.WriteLine(isNotString)
        Console.WriteLine(isNotInteger)

    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, , TestOptions.ReleaseExe)

            CompilationUtils.AssertNoErrors(compilation)

            CompileAndVerify(compilation, <![CDATA[
True
False
False
True
]]>)
        End Sub

        <Fact>
        Public Sub ExecutesCorrectlyAsIfCondition()
            Dim source =
<compilation name="ExecutedCorrectlyAsIfCondition">
    <file name="Program.vb">
Option Strict On
Imports System
Module Program
    Sub Main(args As String())

        Dim o As Object = ""

        If TypeOf o Is String Then
            Console.WriteLine("It's a String")
        End If

        If TypeOf o Is Integer Then
            Console.WriteLine("It's an Integer")
        End If

        If TypeOf o IsNot String Then
            Console.WriteLine("It's NOT a String")
        End If

        If TypeOf o IsNot Integer Then
            Console.WriteLine("It's NOT an Integer")
        End If

    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, , TestOptions.ReleaseExe)

            CompilationUtils.AssertNoErrors(compilation)

            CompileAndVerify(compilation, <![CDATA[
It's a String
It's NOT an Integer
]]>)

        End Sub

        <Fact>
        Public Sub GeneratesILCorrectlyUnderRelease()

            Dim source =
<compilation name="GeneratesILCorrectlyUnderRelease">
    <file name="Program.vb">
Option Strict On

Imports System

Module Program
    Sub Main(args As String())

        Dim o As Object = ""

        Dim isString = TypeOf o Is String
        Console.WriteLine(isString)

        If TypeOf o IsNot Integer Then
            Console.WriteLine(True)
        Else
            Console.WriteLine(False)
        End If

        Dim isInteger = TypeOf o Is Integer
        Console.WriteLine(isInteger)

        If TypeOf o IsNot String Then
            Console.WriteLine(True)
        Else
            Console.WriteLine(False)
        End If

        Console.WriteLine(If(TypeOf o Is Decimal, True, False))
        Console.WriteLine(If(TypeOf o IsNot Decimal, True, False))

    End Sub

    Sub M(Of T, TRef As Class, TVal As Structure, TBase As Class, TDerived As TBase)()

        Dim oT As T = Nothing

        If TypeOf oT Is TRef Then
            Console.WriteLine(False)
        End If

        If TypeOf oT Is TVal Then
            Console.WriteLine(False)
        End If

        Dim vVal As TVal = Nothing

        If TypeOf vVal Is TRef Then
            Console.WriteLine(False)
        End If

        If TypeOf oT Is String Then
            Console.WriteLine(False)
        End If

    End Sub
End Module
    </file>
</compilation>

            CompileAndVerify(source).VerifyIL("Program.Main",
            <![CDATA[
{
  // Code size      111 (0x6f)
  .maxstack  3
  IL_0000:  ldstr      ""
  IL_0005:  dup
  IL_0006:  isinst     "String"
  IL_000b:  ldnull
  IL_000c:  cgt.un
  IL_000e:  call       "Sub System.Console.WriteLine(Boolean)"
  IL_0013:  dup
  IL_0014:  isinst     "Integer"
  IL_0019:  brtrue.s   IL_0023
  IL_001b:  ldc.i4.1
  IL_001c:  call       "Sub System.Console.WriteLine(Boolean)"
  IL_0021:  br.s       IL_0029
  IL_0023:  ldc.i4.0
  IL_0024:  call       "Sub System.Console.WriteLine(Boolean)"
  IL_0029:  dup
  IL_002a:  isinst     "Integer"
  IL_002f:  ldnull
  IL_0030:  cgt.un
  IL_0032:  call       "Sub System.Console.WriteLine(Boolean)"
  IL_0037:  dup
  IL_0038:  isinst     "String"
  IL_003d:  brtrue.s   IL_0047
  IL_003f:  ldc.i4.1
  IL_0040:  call       "Sub System.Console.WriteLine(Boolean)"
  IL_0045:  br.s       IL_004d
  IL_0047:  ldc.i4.0
  IL_0048:  call       "Sub System.Console.WriteLine(Boolean)"
  IL_004d:  dup
  IL_004e:  isinst     "Decimal"
  IL_0053:  brtrue.s   IL_0058
  IL_0055:  ldc.i4.0
  IL_0056:  br.s       IL_0059
  IL_0058:  ldc.i4.1
  IL_0059:  call       "Sub System.Console.WriteLine(Boolean)"
  IL_005e:  isinst     "Decimal"
  IL_0063:  brfalse.s  IL_0068
  IL_0065:  ldc.i4.0
  IL_0066:  br.s       IL_0069
  IL_0068:  ldc.i4.1
  IL_0069:  call       "Sub System.Console.WriteLine(Boolean)"
  IL_006e:  ret
}
]]>).VerifyIL("Program.M",
            <![CDATA[
{
  // Code size       93 (0x5d)
  .maxstack  2
  .locals init (T V_0,
  TVal V_1)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    "T"
  IL_0008:  ldloc.0
  IL_0009:  dup
  IL_000a:  box        "T"
  IL_000f:  isinst     "TRef"
  IL_0014:  brfalse.s  IL_001c
  IL_0016:  ldc.i4.0
  IL_0017:  call       "Sub System.Console.WriteLine(Boolean)"
  IL_001c:  dup
  IL_001d:  box        "T"
  IL_0022:  isinst     "TVal"
  IL_0027:  brfalse.s  IL_002f
  IL_0029:  ldc.i4.0
  IL_002a:  call       "Sub System.Console.WriteLine(Boolean)"
  IL_002f:  ldloca.s   V_1
  IL_0031:  initobj    "TVal"
  IL_0037:  ldloc.1
  IL_0038:  box        "TVal"
  IL_003d:  isinst     "TRef"
  IL_0042:  brfalse.s  IL_004a
  IL_0044:  ldc.i4.0
  IL_0045:  call       "Sub System.Console.WriteLine(Boolean)"
  IL_004a:  box        "T"
  IL_004f:  isinst     "String"
  IL_0054:  brfalse.s  IL_005c
  IL_0056:  ldc.i4.0
  IL_0057:  call       "Sub System.Console.WriteLine(Boolean)"
  IL_005c:  ret
}
]]>)

        End Sub

        ' For compatibility with Dev10, "TypeOf t" should be
        ' supported for type parameter with Structure constraint,
        ' and the generated IL should box the argument.
        <Fact()>
        Public Sub TypeParameterWithConstraints01()
            Dim source =
<compilation>
    <file name="Program.vb">
Imports System
Module M
    Sub M(Of T, U As Structure, V As Class)(x As T, y As U, z As V)
        If TypeOf x Is Integer Then
            Console.WriteLine("x")
        End If
        If TypeOf y Is Integer Then
            Console.WriteLine("y")
        End If
        If TypeOf z Is Integer Then
            Console.WriteLine("z")
        End If
    End Sub
    Sub Main()
        M(1, 2.0, DirectCast(3, Object))
        M(1.0, 2, DirectCast(3.0, Object))
    End Sub
End Module
    </file>
</compilation>
            CompileAndVerify(source, expectedOutput:=<![CDATA[
x
z
y
]]>).VerifyIL("M.M(Of T, U, V)(T, U, V)",
            <![CDATA[
{
  // Code size       70 (0x46)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  box        "T"
  IL_0006:  isinst     "Integer"
  IL_000b:  brfalse.s  IL_0017
  IL_000d:  ldstr      "x"
  IL_0012:  call       "Sub System.Console.WriteLine(String)"
  IL_0017:  ldarg.1
  IL_0018:  box        "U"
  IL_001d:  isinst     "Integer"
  IL_0022:  brfalse.s  IL_002e
  IL_0024:  ldstr      "y"
  IL_0029:  call       "Sub System.Console.WriteLine(String)"
  IL_002e:  ldarg.2
  IL_002f:  box        "V"
  IL_0034:  isinst     "Integer"
  IL_0039:  brfalse.s  IL_0045
  IL_003b:  ldstr      "z"
  IL_0040:  call       "Sub System.Console.WriteLine(String)"
  IL_0045:  ret
}
]]>)
        End Sub

        <WorkItem(543781, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543781")>
        <Fact()>
        Public Sub TypeParameterWithConstraints02()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation>
    <file name="Program.vb">
Structure S1
End Structure
Structure S2
End Structure
MustInherit Class A0(Of T1, T2)
    Public MustOverride Sub M(Of U1 As T1, U2 As T2)(_1 As U1, _2 As U2)
End Class
Class B0
    Inherits A0(Of S1, S2)
    Public Overloads Overrides Sub M(Of U1 As S1, U2 As S2)(_1 As U1, _2 As U2)
        If TypeOf _1 Is S2 Then ' B0.M
        End If
        If TypeOf _1 Is U2 Then ' B0.M
        End If
    End Sub
End Class
MustInherit Class A1(Of T1, T2)
    Public MustOverride Sub M(Of U1 As {T1, Structure}, U2 As {T2})(_1 As U1, _2 As U2)
End Class
Class B1
    Inherits A1(Of S1, S2)
    Public Overloads Overrides Sub M(Of U1 As {Structure, S1}, U2 As {S2})(_1 As U1, _2 As U2)
        If TypeOf _1 Is S2 Then ' B1.M
        End If
        If TypeOf _1 Is U2 Then ' B1.M
        End If
        If TypeOf _2 Is S1 Then ' B1.M
        End If
        If TypeOf _2 Is U1 Then ' B1.M
        End If
    End Sub
End Class
MustInherit Class A2(Of T1, T2)
    Public MustOverride Sub M(Of U1 As {T1, Structure}, U2 As {T2, Structure})(_1 As U1, _2 As U2)
End Class
Class B2
    Inherits A2(Of S1, S2)
    Public Overloads Overrides Sub M(Of U1 As {Structure, S1}, U2 As {Structure, S2})(_1 As U1, _2 As U2)
        If TypeOf _1 Is S2 Then ' B2.M
        End If
        If TypeOf _1 Is U2 Then ' B2.M (Dev10 no error)
        End If
    End Sub
End Class
    </file>
</compilation>)
            compilation.AssertTheseDiagnostics(<expected>
BC31430: Expression of type 'U1' can never be of type 'S2'.
        If TypeOf _1 Is S2 Then ' B0.M
           ~~~~~~~~~~~~~~~
BC31430: Expression of type 'U1' can never be of type 'U2'.
        If TypeOf _1 Is U2 Then ' B0.M
           ~~~~~~~~~~~~~~~
BC31430: Expression of type 'U1' can never be of type 'S2'.
        If TypeOf _1 Is S2 Then ' B1.M
           ~~~~~~~~~~~~~~~
BC31430: Expression of type 'U1' can never be of type 'U2'.
        If TypeOf _1 Is U2 Then ' B1.M
           ~~~~~~~~~~~~~~~
BC31430: Expression of type 'U2' can never be of type 'S1'.
        If TypeOf _2 Is S1 Then ' B1.M
           ~~~~~~~~~~~~~~~
BC31430: Expression of type 'U2' can never be of type 'U1'.
        If TypeOf _2 Is U1 Then ' B1.M
           ~~~~~~~~~~~~~~~
BC31430: Expression of type 'U1' can never be of type 'S2'.
        If TypeOf _1 Is S2 Then ' B2.M
           ~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub TypeParameterWithConstraints03()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation>
    <file name="Program.vb">
Interface I
End Interface
NotInheritable Class C
End Class
Structure S
End Structure
MustInherit Class A(Of T1, T2)
    Public MustOverride Sub M(Of U1 As T1, U2 As T2)(_1 As U1, _2 As U2)
End Class
Class B1
    Inherits A(Of C, I)
    Public Overrides Sub M(Of U1 As C, U2 As I)(_1 As U1, _2 As U2)
        If TypeOf _1 Is I Then
        End If
        If TypeOf _1 Is U2 Then
        End If
        If TypeOf _2 Is C Then
        End If
        If TypeOf _2 Is U1 Then
        End If
    End Sub
End Class
Class B2
    Inherits A(Of S, I)
    Public Overrides Sub M(Of U1 As S, U2 As I)(_1 As U1, _2 As U2)
        If TypeOf _1 Is I Then
        End If
        If TypeOf _1 Is U2 Then
        End If
        If TypeOf _2 Is S Then
        End If
        If TypeOf _2 Is U1 Then
        End If
    End Sub
End Class
    </file>
</compilation>)
            compilation.AssertNoErrors()
        End Sub

        <Fact()>
        Public Sub TypeParameterWithConstraints04()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation>
    <file name="Program.vb">
MustInherit Class A(Of T)
    Public MustOverride Sub M(Of U As T)(_u As U)
End Class
Class B0
    Inherits A(Of System.Array)
    Public Overrides Sub M(Of U As System.Array)(_u As U)
        If TypeOf _u Is String() Then
        End If
    End Sub
End Class
Class B1
    Inherits A(Of String())
    Public Overrides Sub M(Of U As String())(_u As U)
        If TypeOf _u Is System.Array Then
        End If
        If TypeOf _u Is Integer() Then
        End If
    End Sub
End Class
    </file>
</compilation>)
            compilation.AssertTheseDiagnostics(<expected>
BC31430: Expression of type 'U' can never be of type 'Integer()'.
        If TypeOf _u Is Integer() Then
           ~~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub TypeParameterWithConstraints05()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation>
    <file name="Program.vb">
Enum E
    A
End Enum
MustInherit Class A(Of T)
    Public MustOverride Sub M(Of U As T)(_u As U)
End Class
Class B1
    Inherits A(Of Integer)
    Public Overrides Sub M(Of U As Integer)(_u As U)
        If TypeOf _u Is E Then
        End If
    End Sub
End Class
Class B2
    Inherits A(Of E)
    Public Overrides Sub M(Of U As E)(_u As U)
        If TypeOf _u Is Integer Then
        End If
    End Sub
End Class
    </file>
</compilation>)
            compilation.AssertTheseDiagnostics(<expected>
BC31430: Expression of type 'U' can never be of type 'E'.
        If TypeOf _u Is E Then
           ~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30021ERR_TypeOfRequiresReferenceType1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb">
    Class C
        Shared Sub M()
            Dim i2 As Integer?
            'COMPILEERROR: BC30021, "i2"
            If TypeOf i2 Is Integer Then
            End If
        End Sub
    End Class
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30021: 'TypeOf ... Is' requires its left operand to have a reference type, but this operand has the value type 'Integer?'.
            If TypeOf i2 Is Integer Then
                      ~~
</expected>)
        End Sub

        ' For compatibility with Dev10, "TypeOf t" should be
        ' supported for type parameter T, regardless of constraints.
        <Fact()>
        Public Sub BC30021ERR_TypeOfRequiresReferenceType1_1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb">
Interface I
End Interface
Class A
End Class
Class C
    Shared Sub M(Of T1, T2 As Class, T3 As Structure, T4 As New, T5 As I, T6 As A, T7 As U, U)(_1 As T1, _2 As T2, _3 As T3, _4 As T4, _5 As T5, _6 As T6, _7 As T7)
        If TypeOf _1 Is Object Then
        End If
        If TypeOf _2 Is Object Then
        End If
        If TypeOf _3 Is Object Then
        End If
        If TypeOf _4 Is Object Then
        End If
        If TypeOf _5 Is Object Then
        End If
        If TypeOf _6 Is Object Then
        End If
        If TypeOf _7 Is Object Then
        End If
    End Sub
End Class
    </file>
</compilation>)
            CompilationUtils.AssertNoErrors(compilation)
        End Sub

        <Fact()>
        Public Sub BC31430ERR_TypeOfExprAlwaysFalse2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
    <compilation name="TypeOfExprAlwaysFalse2">
        <file name="a.vb">
        Module M
            Sub Goo(Of T As Structure)(ByVal x As T)
                Dim y = TypeOf x Is String
            End Sub
        End Module
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC31430: Expression of type 'T' can never be of type 'String'.
                Dim y = TypeOf x Is String
                        ~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

    End Class
End Namespace
