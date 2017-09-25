' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Semantics
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase

#Region "Widening Conversions"

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ConversionExpression_Implicit_WideningNothingToClass()
            Dim source = <![CDATA[
Option Strict On
Module Program
    Sub M1()
        Dim s As String = Nothing'BIND:"Dim s As String = Nothing"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim s As St ... g = Nothing')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 's')
    Variables: Local_1: s As System.String
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.String, Constant: null) (Syntax: 'Nothing')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'Nothing')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier:=AddressOf New ExpectedSymbolVerifier().Verify)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ConversionExpression_Implicit_WideningNothingToStruct()
            Dim source = <![CDATA[
Option Strict On
Module Program
    Sub M1()
        Dim s As Integer = Nothing'BIND:"Dim s As Integer = Nothing"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim s As In ... r = Nothing')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 's')
    Variables: Local_1: s As System.Int32
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 0) (Syntax: 'Nothing')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'Nothing')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier:=AddressOf New ExpectedSymbolVerifier().Verify)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ConversionExpression_Implicit_WideningNumberToNumber()
            Dim source = <![CDATA[
Option Strict On
Module Program
    Sub M1()
        Dim s As Double = 1'BIND:"Dim s As Double = 1"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim s As Double = 1')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 's')
    Variables: Local_1: s As System.Double
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Double, Constant: 1) (Syntax: '1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier:=AddressOf New ExpectedSymbolVerifier().Verify)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ConversionExpression_Implicit_WideningZeroAsEnum()
            Dim source = <![CDATA[
Option Strict On
Module Program
    Enum A
        Zero
    End Enum

    Sub M1()
        Dim a As A = 0'BIND:"Dim a As A = 0"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim a As A = 0')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'a')
    Variables: Local_1: a As Program.A
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: Program.A, Constant: 0) (Syntax: '0')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            ' We don't look for the type here. Semantic model doesn't have a conversion here
            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ConversionExpression_Implicit_NarrowingOneAsEnum()
            Dim source = <![CDATA[
Module Program
    Sub M1()
        Dim a As A = 1'BIND:"Dim a As A = 1"
    End Sub

    Enum A
        Zero
    End Enum
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim a As A = 1')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'a')
    Variables: Local_1: a As Program.A
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: Program.A, Constant: 1) (Syntax: '1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ConversionExpression_Implicit_NarrowingOneAsEnum_InvalidOptionStrictOn()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module Program
    Sub M1()
        Dim a As A = 1'BIND:"Dim a As A = 1"
    End Sub

    Enum A
        Zero
    End Enum
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Dim a As A = 1')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'a')
    Variables: Local_1: a As Program.A
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: Program.A, Constant: 1, IsInvalid) (Syntax: '1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30512: Option Strict On disallows implicit conversions from 'Integer' to 'Program.A'.
        Dim a As A = 1'BIND:"Dim a As A = 1"
                     ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier:=AddressOf New ExpectedSymbolVerifier().Verify)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ConversionExpression_Implicit_NarrowingIntAsEnum()
            Dim source = <![CDATA[
Module Program
    Sub M1()
        Dim i As Integer = 0
        Dim a As A = i'BIND:"Dim a As A = i"
    End Sub

    Enum A
        Zero
    End Enum
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim a As A = i')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'a')
    Variables: Local_1: a As Program.A
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: Program.A) (Syntax: 'i')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier:=AddressOf New ExpectedSymbolVerifier().Verify)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ConversionExpression_Implicit_NarrowingIntAsEnum_InvalidOptionStrictOn()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module Program
    Sub M1()
        Dim i As Integer = 0
        Dim a As A = i'BIND:"Dim a As A = i"
    End Sub

    Enum A
        Zero
    End Enum
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Dim a As A = i')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'a')
    Variables: Local_1: a As Program.A
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: Program.A, IsInvalid) (Syntax: 'i')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32, IsInvalid) (Syntax: 'i')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30512: Option Strict On disallows implicit conversions from 'Integer' to 'Program.A'.
        Dim a As A = i'BIND:"Dim a As A = i"
                     ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier:=AddressOf New ExpectedSymbolVerifier().Verify)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ConversionExpression_Implicit_InvalidStatement_NoIdentifier()
            Dim source = <![CDATA[
Option Strict On
Module Program
    Enum A
        Zero
    End Enum

    Sub M1()
        Dim a As A ='BIND:"Dim a As A ="
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Dim a As A =')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'a')
    Variables: Local_1: a As Program.A
    Initializer: IInvalidExpression (OperationKind.InvalidExpression, Type: null, IsInvalid) (Syntax: '')
        Children(0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30201: Expression expected.
        Dim a As A ='BIND:"Dim a As A ="
                    ~
]]>.Value

            ' We don't verify the symbols here, as the semantic model says that there is no conversion.
            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ConversionExpression_Implicit_InvalidStatement_InvalidIdentifier()
            Dim source = <![CDATA[
Option Strict On
Module Program
    Sub M1()
        Dim a As Integer = b + c'BIND:"Dim a As Integer = b + c"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Dim a As Integer = b + c')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'a')
    Variables: Local_1: a As System.Int32
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid) (Syntax: 'b + c')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: IBinaryOperatorExpression (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperatorExpression, Type: ?, IsInvalid) (Syntax: 'b + c')
            Left: IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: 'b')
                Children(0)
            Right: IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: 'c')
                Children(0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30451: 'b' is not declared. It may be inaccessible due to its protection level.
        Dim a As Integer = b + c'BIND:"Dim a As Integer = b + c"
                           ~
BC30451: 'c' is not declared. It may be inaccessible due to its protection level.
        Dim a As Integer = b + c'BIND:"Dim a As Integer = b + c"
                               ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier:=AddressOf New ExpectedSymbolVerifier().Verify)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ConversionExpression_Implicit_WideningEnumToUnderlyingType()
            Dim source = <![CDATA[
Option Strict On
Module Program
    Enum A
        Zero
        One
        Two
    End Enum

    Sub M1()
        Dim i As Integer = A.Two'BIND:"Dim i As Integer = A.Two"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim i As Integer = A.Two')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i')
    Variables: Local_1: i As System.Int32
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 2) (Syntax: 'A.Two')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: IFieldReferenceExpression: Program.A.Two (Static) (OperationKind.FieldReferenceExpression, Type: Program.A, Constant: 2) (Syntax: 'A.Two')
            Instance Receiver: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier:=AddressOf New ExpectedSymbolVerifier().Verify)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ConversionExpression_Implicit_WideningEnumToUnderlyingTypeConversion()
            Dim source = <![CDATA[
Option Strict On
Module Program
    Enum A
        Zero
        One
        Two
    End Enum

    Sub M1()
        Dim i As Single = A.Two'BIND:"Dim i As Single = A.Two"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim i As Single = A.Two')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i')
    Variables: Local_1: i As System.Single
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Single, Constant: 2) (Syntax: 'A.Two')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: IFieldReferenceExpression: Program.A.Two (Static) (OperationKind.FieldReferenceExpression, Type: Program.A, Constant: 2) (Syntax: 'A.Two')
            Instance Receiver: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ConversionExpression_Implicit_NarrowingEnumToUnderlyingTypeConversion_InvalidOptionStrictConversion()
            Dim source = <![CDATA[
Option Strict On
Module Program
    Enum A As UInteger
        Zero
        One
        Two
    End Enum

    Sub M1()
        Dim i As Integer = A.Two'BIND:"Dim i As Integer = A.Two"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Dim i As Integer = A.Two')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i')
    Variables: Local_1: i As System.Int32
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 2, IsInvalid) (Syntax: 'A.Two')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: IFieldReferenceExpression: Program.A.Two (Static) (OperationKind.FieldReferenceExpression, Type: Program.A, Constant: 2, IsInvalid) (Syntax: 'A.Two')
            Instance Receiver: null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30512: Option Strict On disallows implicit conversions from 'Program.A' to 'Integer'.
        Dim i As Integer = A.Two'BIND:"Dim i As Integer = A.Two"
                           ~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ConversionExpression_Implicit_WideningConstantExpressionNarrowing()
            Dim source = <![CDATA[
Option Strict On
Module Program
    Sub M1()
        Dim i As Single = 1.0'BIND:"Dim i As Single = 1.0"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim i As Single = 1.0')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i')
    Variables: Local_1: i As System.Single
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Single, Constant: 1) (Syntax: '1.0')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Double, Constant: 1) (Syntax: '1.0')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier:=AddressOf New ExpectedSymbolVerifier().Verify)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ConversionExpression_Implicit_NarrowingBooleanConversion()
            Dim source = <![CDATA[
Module Program
    Sub M1()
        Dim b As Boolean = False
        Dim s As String = b'BIND:"Dim s As String = b"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim s As String = b')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 's')
    Variables: Local_1: s As System.String
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.String) (Syntax: 'b')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: b (OperationKind.LocalReferenceExpression, Type: System.Boolean) (Syntax: 'b')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier:=AddressOf New ExpectedSymbolVerifier().Verify)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ConversionExpression_Implicit_NarrowingBooleanConversion_InvalidOptionStrictOn()
            Dim source = <![CDATA[
Option Strict On
Module Program
    Sub M1()
        Dim b As Boolean = False
        Dim s As String = b'BIND:"Dim s As String = b"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Dim s As String = b')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 's')
    Variables: Local_1: s As System.String
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.String, IsInvalid) (Syntax: 'b')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: b (OperationKind.LocalReferenceExpression, Type: System.Boolean, IsInvalid) (Syntax: 'b')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30512: Option Strict On disallows implicit conversions from 'Boolean' to 'String'.
        Dim s As String = b'BIND:"Dim s As String = b"
                          ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier:=AddressOf New ExpectedSymbolVerifier().Verify)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ConversionExpression_Implicit_WideningClassToBaseClass()
            Dim source = <![CDATA[
Option Strict On
Module Program
    Sub M1()
        Dim c2 As C2 = New C2
        Dim c1 As C1 = c2'BIND:"Dim c1 As C1 = c2"
    End Sub

    Class C1
    End Class

    Class C2
        Inherits C1
    End Class
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim c1 As C1 = c2')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'c1')
    Variables: Local_1: c1 As Program.C1
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: Program.C1) (Syntax: 'c2')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: c2 (OperationKind.LocalReferenceExpression, Type: Program.C2) (Syntax: 'c2')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier:=AddressOf New ExpectedSymbolVerifier().Verify)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ConversionExpression_Implicit_WideningClassToBaseClass_InvalidNoConversion()
            Dim source = <![CDATA[
Option Strict On
Module Program
    Sub M1()
        Dim c2 As C2 = New C2
        Dim c1 As C1 = c2'BIND:"Dim c1 As C1 = c2"
    End Sub

    Class C1
    End Class

    Class C2
    End Class
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Dim c1 As C1 = c2')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'c1')
    Variables: Local_1: c1 As Program.C1
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: Program.C1, IsInvalid) (Syntax: 'c2')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: c2 (OperationKind.LocalReferenceExpression, Type: Program.C2, IsInvalid) (Syntax: 'c2')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30311: Value of type 'Program.C2' cannot be converted to 'Program.C1'.
        Dim c1 As C1 = c2'BIND:"Dim c1 As C1 = c2"
                       ~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier:=AddressOf New ExpectedSymbolVerifier().Verify)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ConversionExpression_Implicit_WideningClassToInterface()
            Dim source = <![CDATA[
Option Strict On
Module Program
    Sub M1()
        Dim c1 As C1 = New C1
        Dim i1 As I1 = c1'BIND:"Dim i1 As I1 = c1"
    End Sub

    Interface I1
    End Interface

    Class C1
        Implements I1
    End Class
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim i1 As I1 = c1')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i1')
    Variables: Local_1: i1 As Program.I1
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: Program.I1) (Syntax: 'c1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: c1 (OperationKind.LocalReferenceExpression, Type: Program.C1) (Syntax: 'c1')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier:=AddressOf New ExpectedSymbolVerifier().Verify)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ConversionExpression_Implicit_WideningClassToInterface_InvalidNoImplementation()
            Dim source = <![CDATA[
Option Strict On
Module Program
    Sub M1()
        Dim c1 As C1 = New C1
        Dim i1 As I1 = c1'BIND:"Dim i1 As I1 = c1"
    End Sub

    Interface I1
    End Interface

    Class C1
    End Class
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Dim i1 As I1 = c1')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i1')
    Variables: Local_1: i1 As Program.I1
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: Program.I1, IsInvalid) (Syntax: 'c1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: c1 (OperationKind.LocalReferenceExpression, Type: Program.C1, IsInvalid) (Syntax: 'c1')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30512: Option Strict On disallows implicit conversions from 'Program.C1' to 'Program.I1'.
        Dim i1 As I1 = c1'BIND:"Dim i1 As I1 = c1"
                       ~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier:=AddressOf New ExpectedSymbolVerifier().Verify)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ConversionExpression_Implicit_NarrowingClassToInterface_OptionStrictOff()
            Dim source = <![CDATA[
Module Program
    Sub M1()
        Dim c1 As C1 = New C1
        Dim i1 As I1 = c1'BIND:"Dim i1 As I1 = c1"
    End Sub

    Interface I1
    End Interface

    Class C1
    End Class
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim i1 As I1 = c1')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i1')
    Variables: Local_1: i1 As Program.I1
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: Program.I1) (Syntax: 'c1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: c1 (OperationKind.LocalReferenceExpression, Type: Program.C1) (Syntax: 'c1')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier:=AddressOf New ExpectedSymbolVerifier().Verify)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ConversionExpression_Implicit_WideningInterfaceToObject()
            Dim source = <![CDATA[
Option Strict On
Module Program
    Sub M1()
        Dim i As I1 = Nothing
        Dim o As Object = i'BIND:"Dim o As Object = i"
    End Sub

    Interface I1
    End Interface
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim o As Object = i')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'o')
    Variables: Local_1: o As System.Object
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: 'i')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: Program.I1) (Syntax: 'i')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier:=AddressOf New ExpectedSymbolVerifier().Verify)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ConversionExpression_Implicit_WideningVarianceConversion()
            Dim source = <![CDATA[
Option Strict On
Imports System.Collections.Generic
Module Program
    Sub M1()
        Dim i2List As IList(Of I2) = Nothing
        Dim i1List As IEnumerable(Of I1) = i2List'BIND:"Dim i1List As IEnumerable(Of I1) = i2List"
    End Sub

    Interface I1
    End Interface

    Interface I2
        Inherits I1
    End Interface
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim i1List  ... 1) = i2List')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i1List')
    Variables: Local_1: i1List As System.Collections.Generic.IEnumerable(Of Program.I1)
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.Generic.IEnumerable(Of Program.I1)) (Syntax: 'i2List')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: i2List (OperationKind.LocalReferenceExpression, Type: System.Collections.Generic.IList(Of Program.I2)) (Syntax: 'i2List')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier:=AddressOf New ExpectedSymbolVerifier().Verify)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ConversionExpression_Implicit_WideningLambdaToDelegate()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module Program
    Sub M1()
        Dim a As Action(Of Integer) = Sub(i As Integer)'BIND:"Dim a As Action(Of Integer) = Sub(i As Integer)"
                                      End Sub
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim a As Ac ... End Sub')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'a')
    Variables: Local_1: a As System.Action(Of System.Int32)
    Initializer: IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Action(Of System.Int32)) (Syntax: 'Sub(i As In ... End Sub')
        Target: IAnonymousFunctionExpression (Symbol: Sub (i As System.Int32)) (OperationKind.AnonymousFunctionExpression, Type: null) (Syntax: 'Sub(i As In ... End Sub')
            IBlockStatement (2 statements) (OperationKind.BlockStatement) (Syntax: 'Sub(i As In ... End Sub')
              ILabeledStatement (Label: exit) (OperationKind.LabeledStatement) (Syntax: 'End Sub')
                Statement: null
              IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'End Sub')
                ReturnedValue: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier:=AddressOf New ExpectedSymbolVerifier().Verify)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ConversionExpression_Implicit_WideningLambdaToDelegate_RelaxationOfArguments()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module Program
    Sub M1()
        Dim a As Action(Of Integer) = Sub()'BIND:"Dim a As Action(Of Integer) = Sub()"
                                      End Sub
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim a As Ac ... End Sub')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'a')
    Variables: Local_1: a As System.Action(Of System.Int32)
    Initializer: IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Action(Of System.Int32)) (Syntax: 'Sub()'BIND: ... End Sub')
        Target: IAnonymousFunctionExpression (Symbol: Sub ()) (OperationKind.AnonymousFunctionExpression, Type: null) (Syntax: 'Sub()'BIND: ... End Sub')
            IBlockStatement (2 statements) (OperationKind.BlockStatement) (Syntax: 'Sub()'BIND: ... End Sub')
              ILabeledStatement (Label: exit) (OperationKind.LabeledStatement) (Syntax: 'End Sub')
                Statement: null
              IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'End Sub')
                ReturnedValue: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier:=AddressOf New ExpectedSymbolVerifier().Verify)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ConversionExpression_Implicit_WideningLambdaToDelegate_RelaxationOfArguments_InvalidConversion()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module Program
    Sub M1()
        Dim a As Action = Sub(i As Integer)'BIND:"Dim a As Action = Sub(i As Integer)"
                          End Sub
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Dim a As Ac ... End Sub')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'a')
    Variables: Local_1: a As System.Action
    Initializer: IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Action, IsInvalid) (Syntax: 'Sub(i As In ... End Sub')
        Target: IAnonymousFunctionExpression (Symbol: Sub (i As System.Int32)) (OperationKind.AnonymousFunctionExpression, Type: null, IsInvalid) (Syntax: 'Sub(i As In ... End Sub')
            IBlockStatement (2 statements) (OperationKind.BlockStatement, IsInvalid) (Syntax: 'Sub(i As In ... End Sub')
              ILabeledStatement (Label: exit) (OperationKind.LabeledStatement, IsInvalid) (Syntax: 'End Sub')
                Statement: null
              IReturnStatement (OperationKind.ReturnStatement, IsInvalid) (Syntax: 'End Sub')
                ReturnedValue: null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36670: Nested sub does not have a signature that is compatible with delegate 'Action'.
        Dim a As Action = Sub(i As Integer)'BIND:"Dim a As Action = Sub(i As Integer)"
                          ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier:=AddressOf New ExpectedSymbolVerifier().Verify)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ConversionExpression_Implicit_WideningLambdaToDelegate_ReturnConversion()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module Program
    Sub M1()
        Dim a As Func(Of Long) = Function() As Integer'BIND:"Dim a As Func(Of Long) = Function() As Integer"
                                     Return 1
                                 End Function
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim a As Fu ... nd Function')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'a')
    Variables: Local_1: a As System.Func(Of System.Int64)
    Initializer: IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Func(Of System.Int64)) (Syntax: 'Function()  ... nd Function')
        Target: IAnonymousFunctionExpression (Symbol: Function () As System.Int32) (OperationKind.AnonymousFunctionExpression, Type: null) (Syntax: 'Function()  ... nd Function')
            IBlockStatement (3 statements, 1 locals) (OperationKind.BlockStatement) (Syntax: 'Function()  ... nd Function')
              Locals: Local_1: <anonymous local> As System.Int32
              IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'Return 1')
                ReturnedValue: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
              ILabeledStatement (Label: exit) (OperationKind.LabeledStatement) (Syntax: 'End Function')
                Statement: null
              IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'End Function')
                ReturnedValue: ILocalReferenceExpression:  (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'End Function')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier:=AddressOf New ExpectedSymbolVerifier().Verify)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ConversionExpression_Implicit_WideningLambdaToDelegate_RelaxationOfReturn()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module Program
    Sub M1()
        Dim a As Action(Of Integer) = Function() As Integer'BIND:"Dim a As Action(Of Integer) = Function() As Integer"
                                          Return 1
                                      End Function
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim a As Ac ... nd Function')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'a')
    Variables: Local_1: a As System.Action(Of System.Int32)
    Initializer: IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Action(Of System.Int32)) (Syntax: 'Function()  ... nd Function')
        Target: IAnonymousFunctionExpression (Symbol: Function () As System.Int32) (OperationKind.AnonymousFunctionExpression, Type: null) (Syntax: 'Function()  ... nd Function')
            IBlockStatement (3 statements, 1 locals) (OperationKind.BlockStatement) (Syntax: 'Function()  ... nd Function')
              Locals: Local_1: <anonymous local> As System.Int32
              IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'Return 1')
                ReturnedValue: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
              ILabeledStatement (Label: exit) (OperationKind.LabeledStatement) (Syntax: 'End Function')
                Statement: null
              IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'End Function')
                ReturnedValue: ILocalReferenceExpression:  (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'End Function')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier:=AddressOf New ExpectedSymbolVerifier().Verify)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ConversionExpression_Implicit_WideningLambdaToDelegate_RelaxationOfReturn_InvalidConversion()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module Program
    Sub M1()
        Dim a As Func(Of Integer) = Sub()'BIND:"Dim a As Func(Of Integer) = Sub()"
                                    End Sub
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Dim a As Fu ... End Sub')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'a')
    Variables: Local_1: a As System.Func(Of System.Int32)
    Initializer: IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Func(Of System.Int32), IsInvalid) (Syntax: 'Sub()'BIND: ... End Sub')
        Target: IAnonymousFunctionExpression (Symbol: Sub ()) (OperationKind.AnonymousFunctionExpression, Type: null, IsInvalid) (Syntax: 'Sub()'BIND: ... End Sub')
            IBlockStatement (2 statements) (OperationKind.BlockStatement, IsInvalid) (Syntax: 'Sub()'BIND: ... End Sub')
              ILabeledStatement (Label: exit) (OperationKind.LabeledStatement, IsInvalid) (Syntax: 'End Sub')
                Statement: null
              IReturnStatement (OperationKind.ReturnStatement, IsInvalid) (Syntax: 'End Sub')
                ReturnedValue: null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36670: Nested sub does not have a signature that is compatible with delegate 'Func(Of Integer)'.
        Dim a As Func(Of Integer) = Sub()'BIND:"Dim a As Func(Of Integer) = Sub()"
                                    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier:=AddressOf New ExpectedSymbolVerifier().Verify)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ConversionExpression_Implicit_WideningMethodGroupToDelegate()
            Dim source = <![CDATA[
Imports System
Module Program
    Sub M1()
        Dim a As Action = AddressOf M2'BIND:"Dim a As Action = AddressOf M2"
    End Sub

    Sub M2()
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim a As Ac ... ddressOf M2')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'a')
    Variables: Local_1: a As System.Action
    Initializer: IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Action) (Syntax: 'AddressOf M2')
        Target: IMethodReferenceExpression: Sub Program.M2() (OperationKind.MethodReferenceExpression, Type: System.Action) (Syntax: 'AddressOf M2')
            Instance Receiver: IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: Program) (Syntax: 'M2')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ConversionExpression_Implicit_WideningMethodGroupToDelegate_RelaxationArguments()
            Dim source = <![CDATA[
Imports System
Module Program
    Sub M1()
        Dim a As Action(Of Integer) = AddressOf M2'BIND:"Dim a As Action(Of Integer) = AddressOf M2"
    End Sub

    Sub M2()
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim a As Ac ... ddressOf M2')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'a')
    Variables: Local_1: a As System.Action(Of System.Int32)
    Initializer: IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Action(Of System.Int32)) (Syntax: 'AddressOf M2')
        Target: IMethodReferenceExpression: Sub Program.M2() (OperationKind.MethodReferenceExpression, Type: System.Action(Of System.Int32)) (Syntax: 'AddressOf M2')
            Instance Receiver: IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: Program) (Syntax: 'M2')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ConversionExpression_Implicit_WideningMethodGroupToDelegate_RelaxationArguments_InvalidConversion()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module Program
    Sub M1()
        Dim a As Action = AddressOf M2'BIND:"Dim a As Action = AddressOf M2"
    End Sub

    Sub M2(i As Integer)
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Dim a As Ac ... ddressOf M2')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'a')
    Variables: Local_1: a As System.Action
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Action, IsInvalid) (Syntax: 'AddressOf M2')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: IOperation:  (OperationKind.None, IsInvalid) (Syntax: 'AddressOf M2')
            Children(1):
                IOperation:  (OperationKind.None, IsInvalid) (Syntax: 'M2')
                  Children(1):
                      IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: Program, IsInvalid) (Syntax: 'M2')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC31143: Method 'Public Sub M2(i As Integer)' does not have a signature compatible with delegate 'Delegate Sub Action()'.
        Dim a As Action = AddressOf M2'BIND:"Dim a As Action = AddressOf M2"
                                    ~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ConversionExpression_Implicit_WideningMethodGroupToDelegate_RelaxationArguments_NonImplicitReceiver_InvalidConversion()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module Program
    Sub M1()
        Dim c1 As New C1
        Dim a As Action = AddressOf c1.M2'BIND:"Dim a As Action = AddressOf c1.M2"
    End Sub

    Class C1
        Sub M2(i As Integer)
        End Sub
    End Class
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Dim a As Ac ... essOf c1.M2')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'a')
    Variables: Local_1: a As System.Action
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Action, IsInvalid) (Syntax: 'AddressOf c1.M2')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: IOperation:  (OperationKind.None, IsInvalid) (Syntax: 'AddressOf c1.M2')
            Children(1):
                IOperation:  (OperationKind.None, IsInvalid) (Syntax: 'c1.M2')
                  Children(1):
                      IOperation:  (OperationKind.None, IsInvalid) (Syntax: 'c1')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC31143: Method 'Public Sub M2(i As Integer)' does not have a signature compatible with delegate 'Delegate Sub Action()'.
        Dim a As Action = AddressOf c1.M2'BIND:"Dim a As Action = AddressOf c1.M2"
                                    ~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ConversionExpression_Implicit_WideningMethodGroupToDelegate_RelaxationReturnType()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module Program
    Sub M1()
        Dim a As Action = AddressOf M2'BIND:"Dim a As Action = AddressOf M2"
    End Sub

    Function M2() As Integer
        Return 1
    End Function
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim a As Ac ... ddressOf M2')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'a')
    Variables: Local_1: a As System.Action
    Initializer: IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Action) (Syntax: 'AddressOf M2')
        Target: IMethodReferenceExpression: Function Program.M2() As System.Int32 (OperationKind.MethodReferenceExpression, Type: System.Action) (Syntax: 'AddressOf M2')
            Instance Receiver: IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: Program) (Syntax: 'M2')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ConversionExpression_Implicit_WideningMethodGroupToDelegate_ReturnConversion()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module Program
    Sub M1()
        Dim a As Func(Of Long) = AddressOf M2'BIND:"Dim a As Func(Of Long) = AddressOf M2"
    End Sub

    Function M2() As Integer
        Return 1
    End Function
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim a As Fu ... ddressOf M2')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'a')
    Variables: Local_1: a As System.Func(Of System.Int64)
    Initializer: IDelegateCreationExpression (OperationKind.DelegateCreationExpression, Type: System.Func(Of System.Int64)) (Syntax: 'AddressOf M2')
        Target: IMethodReferenceExpression: Function Program.M2() As System.Int32 (OperationKind.MethodReferenceExpression, Type: System.Func(Of System.Int64)) (Syntax: 'AddressOf M2')
            Instance Receiver: IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: Program) (Syntax: 'M2')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ConversionExpression_Implicit_WideningMethodGroupToDelegate_RelaxationReturnType_InvalidConversion()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module Program
    Sub M1()
        Dim a As Func(Of Long) = AddressOf M2'BIND:"Dim a As Func(Of Long) = AddressOf M2"
    End Sub

    Sub M2()
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Dim a As Fu ... ddressOf M2')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'a')
    Variables: Local_1: a As System.Func(Of System.Int64)
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Func(Of System.Int64), IsInvalid) (Syntax: 'AddressOf M2')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: IOperation:  (OperationKind.None, IsInvalid) (Syntax: 'AddressOf M2')
            Children(1):
                IOperation:  (OperationKind.None, IsInvalid) (Syntax: 'M2')
                  Children(1):
                      IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: Program, IsInvalid) (Syntax: 'M2')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC31143: Method 'Public Sub M2()' does not have a signature compatible with delegate 'Delegate Function Func(Of Long)() As Long'.
        Dim a As Func(Of Long) = AddressOf M2'BIND:"Dim a As Func(Of Long) = AddressOf M2"
                                           ~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ConversionExpression_Implicit_InvalidMethodGroupToDelegateSyntax()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module Program
    Sub M1()
        Dim a As Action = AddressOf'BIND:"Dim a As Action = AddressOf"
    End Sub

    Function M2() As Integer
        Return 1
    End Function
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Dim a As Ac ... = AddressOf')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'a')
    Variables: Local_1: a As System.Action
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Action, IsInvalid) (Syntax: 'AddressOf')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: 'AddressOf')
            Children(1):
                IInvalidExpression (OperationKind.InvalidExpression, Type: null, IsInvalid) (Syntax: '')
                  Children(0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30201: Expression expected.
        Dim a As Action = AddressOf'BIND:"Dim a As Action = AddressOf"
                                   ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ConversionExpression_Implicit_WideningArrayToSystemArrayConversion()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module Program
    Sub M1()
        Dim a As Array = New Integer(1) {}'BIND:"Dim a As Array = New Integer(1) {}"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim a As Ar ... teger(1) {}')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'a')
    Variables: Local_1: a As System.Array
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Array) (Syntax: 'New Integer(1) {}')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: System.Int32()) (Syntax: 'New Integer(1) {}')
            Dimension Sizes(1):
                IBinaryOperatorExpression (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32, Constant: 2) (Syntax: '1')
                  Left: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
                  Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
            Initializer: IArrayInitializer (0 elements) (OperationKind.ArrayInitializer) (Syntax: '{}')
                Element Values(0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier:=AddressOf New ExpectedSymbolVerifier().Verify)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ConversionExpression_Implicit_WideningArrayToSystemArrayConversion_MultiDimensionalArray()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module Program
    Sub M1()
        Dim a As Array = New Integer(1)() {}'BIND:"Dim a As Array = New Integer(1)() {}"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim a As Ar ... ger(1)() {}')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'a')
    Variables: Local_1: a As System.Array
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Array) (Syntax: 'New Integer(1)() {}')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: System.Int32()()) (Syntax: 'New Integer(1)() {}')
            Dimension Sizes(1):
                IBinaryOperatorExpression (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32, Constant: 2) (Syntax: '1')
                  Left: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
                  Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
            Initializer: IArrayInitializer (0 elements) (OperationKind.ArrayInitializer) (Syntax: '{}')
                Element Values(0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier:=AddressOf New ExpectedSymbolVerifier().Verify)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ConversionExpression_Implicit_WideningArrayToSystemArray_InvalidNotArray()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module Program
    Sub M1()
        Dim a As Array = New Object'BIND:"Dim a As Array = New Object"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Dim a As Ar ...  New Object')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'a')
    Variables: Local_1: a As System.Array
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Array, IsInvalid) (Syntax: 'New Object')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: IObjectCreationExpression (Constructor: Sub System.Object..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Object, IsInvalid) (Syntax: 'New Object')
            Arguments(0)
            Initializer: null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30512: Option Strict On disallows implicit conversions from 'Object' to 'Array'.
        Dim a As Array = New Object'BIND:"Dim a As Array = New Object"
                         ~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ConversionExpression_Implicit_WideningArrayToArray()
            Dim source = <![CDATA[
Option Strict On
Imports System.Collections.Generic
Module Program
    Sub M1()
        Dim c2List(1) As C2
        Dim c1List As C1() = c2List'BIND:"Dim c1List As C1() = c2List"
    End Sub

    Class C1
    End Class

    Class C2
        Inherits C1
    End Class
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim c1List  ... () = c2List')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'c1List')
    Variables: Local_1: c1List As Program.C1()
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: Program.C1()) (Syntax: 'c2List')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: c2List (OperationKind.LocalReferenceExpression, Type: Program.C2()) (Syntax: 'c2List')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier:=AddressOf New ExpectedSymbolVerifier().Verify)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ConversionExpression_Implicit_WideningArrayToArray_InvalidDimensionMismatch()
            Dim source = <![CDATA[
Option Strict On
Imports System.Collections.Generic
Module Program
    Sub M1()
        Dim c2List(1)() As C2
        Dim c1List As C1() = c2List'BIND:"Dim c1List As C1() = c2List"
    End Sub

    Class C1
    End Class

    Class C2
        Inherits C1
    End Class
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Dim c1List  ... () = c2List')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'c1List')
    Variables: Local_1: c1List As Program.C1()
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: Program.C1(), IsInvalid) (Syntax: 'c2List')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: c2List (OperationKind.LocalReferenceExpression, Type: Program.C2()(), IsInvalid) (Syntax: 'c2List')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30332: Value of type 'Program.C2()()' cannot be converted to 'Program.C1()' because 'Program.C2()' is not derived from 'Program.C1'.
        Dim c1List As C1() = c2List'BIND:"Dim c1List As C1() = c2List"
                             ~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier:=AddressOf New ExpectedSymbolVerifier().Verify)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ConversionExpression_Implicit_WideningArrayToArray_InvalidNoConversion()
            Dim source = <![CDATA[
Option Strict On
Imports System.Collections.Generic
Module Program
    Sub M1()
        Dim c2List(1) As C2
        Dim c1List As C1() = c2List'BIND:"Dim c1List As C1() = c2List"
    End Sub

    Class C1
    End Class

    Class C2
    End Class
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Dim c1List  ... () = c2List')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'c1List')
    Variables: Local_1: c1List As Program.C1()
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: Program.C1(), IsInvalid) (Syntax: 'c2List')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: c2List (OperationKind.LocalReferenceExpression, Type: Program.C2(), IsInvalid) (Syntax: 'c2List')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30332: Value of type 'Program.C2()' cannot be converted to 'Program.C1()' because 'Program.C2' is not derived from 'Program.C1'.
        Dim c1List As C1() = c2List'BIND:"Dim c1List As C1() = c2List"
                             ~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier:=AddressOf New ExpectedSymbolVerifier().Verify)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ConversionExpression_Implicit_WideningArrayToSystemIEnumerable()
            Dim source = <![CDATA[
Option Strict On
Imports System.Collections.Generic
Module Program
    Sub M1()
        Dim c2List(1) As C2
        Dim c1List As IEnumerable(Of C1) = c2List'BIND:"Dim c1List As IEnumerable(Of C1) = c2List"
    End Sub

    Class C1
    End Class

    Class C2
        Inherits C1
    End Class
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim c1List  ... 1) = c2List')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'c1List')
    Variables: Local_1: c1List As System.Collections.Generic.IEnumerable(Of Program.C1)
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.Generic.IEnumerable(Of Program.C1)) (Syntax: 'c2List')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: c2List (OperationKind.LocalReferenceExpression, Type: Program.C2()) (Syntax: 'c2List')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier:=AddressOf New ExpectedSymbolVerifier().Verify)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ConversionExpression_Implicit_WideningArrayToSystemIEnumerable_InvalidNoConversion()
            Dim source = <![CDATA[
Option Strict On
Imports System.Collections.Generic
Module Program
    Sub M1()
        Dim c2List(1) As C2
        Dim c1List As IEnumerable(Of C1) = c2List'BIND:"Dim c1List As IEnumerable(Of C1) = c2List"
    End Sub

    Class C1
    End Class

    Class C2
    End Class
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Dim c1List  ... 1) = c2List')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'c1List')
    Variables: Local_1: c1List As System.Collections.Generic.IEnumerable(Of Program.C1)
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.Generic.IEnumerable(Of Program.C1), IsInvalid) (Syntax: 'c2List')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: c2List (OperationKind.LocalReferenceExpression, Type: Program.C2(), IsInvalid) (Syntax: 'c2List')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36754: 'Program.C2()' cannot be converted to 'IEnumerable(Of Program.C1)' because 'Program.C2' is not derived from 'Program.C1', as required for the 'Out' generic parameter 'T' in 'Interface IEnumerable(Of Out T)'.
        Dim c1List As IEnumerable(Of C1) = c2List'BIND:"Dim c1List As IEnumerable(Of C1) = c2List"
                                           ~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier:=AddressOf New ExpectedSymbolVerifier().Verify)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ConversionExpression_Implicit_WideningStructureToInterface()
            Dim source = <![CDATA[
Module Program
    Sub M1()
        Dim s1 = New S1
        Dim i1 As I1 = s1'BIND:"Dim i1 As I1 = s1"
    End Sub

    Interface I1
    End Interface

    Structure S1
        Implements I1
    End Structure
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim i1 As I1 = s1')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i1')
    Variables: Local_1: i1 As Program.I1
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: Program.I1) (Syntax: 's1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: s1 (OperationKind.LocalReferenceExpression, Type: Program.S1) (Syntax: 's1')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ConversionExpression_Implicit_WideningStructureToValueType()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module Program
    Sub M1()
        Dim s1 = New S1
        Dim v1 As ValueType = s1'BIND:"Dim v1 As ValueType = s1"
    End Sub

    Structure S1
    End Structure
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim v1 As ValueType = s1')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'v1')
    Variables: Local_1: v1 As System.ValueType
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.ValueType) (Syntax: 's1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: s1 (OperationKind.LocalReferenceExpression, Type: Program.S1) (Syntax: 's1')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ConversionExpression_Implicit_WideningStructureToInterface_InvalidNoConversion()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module Program
    Sub M1()
        Dim s1 = New S1
        Dim i1 As I1 = s1'BIND:"Dim i1 As I1 = s1"
    End Sub

    Interface I1
    End Interface

    Structure S1
    End Structure
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Dim i1 As I1 = s1')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i1')
    Variables: Local_1: i1 As Program.I1
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: Program.I1, IsInvalid) (Syntax: 's1')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: s1 (OperationKind.LocalReferenceExpression, Type: Program.S1, IsInvalid) (Syntax: 's1')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30311: Value of type 'Program.S1' cannot be converted to 'Program.I1'.
        Dim i1 As I1 = s1'BIND:"Dim i1 As I1 = s1"
                       ~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ConversionExpression_Implicit_WideningValueToNullableValue()
            Dim source = <![CDATA[
Option Strict On
Module Program
    Sub M1()
        Dim i As Integer? = 1'BIND:"Dim i As Integer? = 1"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim i As Integer? = 1')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i')
    Variables: Local_1: i As System.Nullable(Of System.Int32)
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Nullable(Of System.Int32)) (Syntax: '1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier:=AddressOf New ExpectedSymbolVerifier().Verify)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ConversionExpression_Implicit_WideningNullableValueToNullableValue()
            Dim source = <![CDATA[
Option Strict On
Module Program
    Sub M1()
        Dim i As Integer? = 1
        Dim l As Long? = i'BIND:"Dim l As Long? = i"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim l As Long? = i')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'l')
    Variables: Local_1: l As System.Nullable(Of System.Int64)
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Nullable(Of System.Int64)) (Syntax: 'i')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 'i')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ConversionExpression_Implicit_WideningValueToNullableValue_MultipleConversion()
            Dim source = <![CDATA[
Option Strict On
Module Program
    Sub M1()
        Dim l As Long? = 1'BIND:"Dim l As Long? = 1"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim l As Long? = 1')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'l')
    Variables: Local_1: l As System.Nullable(Of System.Int64)
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Nullable(Of System.Int64)) (Syntax: '1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier:=AddressOf New ExpectedSymbolVerifier().Verify)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ConversionExpression_Implicit_WideningNullableValueToImplementedInterface()
            Dim source = <![CDATA[
Option Strict On
Module Program
    Sub M1()
        Dim s1 As S1? = Nothing
        Dim i1 As I1 = s1'BIND:"Dim i1 As I1 = s1"
    End Sub

    Interface I1
    End Interface

    Structure S1
        Implements I1
    End Structure
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim i1 As I1 = s1')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i1')
    Variables: Local_1: i1 As Program.I1
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: Program.I1) (Syntax: 's1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: s1 (OperationKind.LocalReferenceExpression, Type: System.Nullable(Of Program.S1)) (Syntax: 's1')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier:=AddressOf New ExpectedSymbolVerifier().Verify)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ConversionExpression_Implicit_WideningCharArrayToString()
            Dim source = <![CDATA[
Option Strict On
Module Program
    Sub M1()
        Dim s1 As String = New Char(1) {}'BIND:"Dim s1 As String = New Char(1) {}"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim s1 As S ...  Char(1) {}')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 's1')
    Variables: Local_1: s1 As System.String
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.String) (Syntax: 'New Char(1) {}')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: System.Char()) (Syntax: 'New Char(1) {}')
            Dimension Sizes(1):
                IBinaryOperatorExpression (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32, Constant: 2) (Syntax: '1')
                  Left: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
                  Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
            Initializer: IArrayInitializer (0 elements) (OperationKind.ArrayInitializer) (Syntax: '{}')
                Element Values(0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ConversionExpression_Implicit_WideningCharToStringConversion()
            Dim source = <![CDATA[
Option Strict On
Module Program
    Sub M1()
        Dim s1 As String = "a"c'BIND:"Dim s1 As String = "a"c"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim s1 As String = "a"c')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 's1')
    Variables: Local_1: s1 As System.String
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.String, Constant: "a") (Syntax: '"a"c')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Char, Constant: a) (Syntax: '"a"c')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier:=AddressOf New ExpectedSymbolVerifier().Verify)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ConversionExpression_Implicit_WideningTransitiveConversion()
            Dim source = <![CDATA[
Option Strict On
Module Module1

    Sub M1()
        Dim c3 As New C3
        Dim c1 As C1 = c3'BIND:"Dim c1 As C1 = c3"
    End Sub

    Class C1
    End Class

    Class C2
        Inherits C1
    End Class

    Class C3
        Inherits C2
    End Class

End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim c1 As C1 = c3')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'c1')
    Variables: Local_1: c1 As Module1.C1
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: Module1.C1) (Syntax: 'c3')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: c3 (OperationKind.LocalReferenceExpression, Type: Module1.C3) (Syntax: 'c3')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier:=AddressOf New ExpectedSymbolVerifier().Verify)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ConversionExpression_Implicit_WideningTypeParameterConversion()
            Dim source = <![CDATA[
Option Strict On
Module Module1

    Sub M1(Of T As {C2, New})()
        Dim c1 As C1 = New T'BIND:"Dim c1 As C1 = New T"
    End Sub

    Class C1
    End Class

    Class C2
        Inherits C1
    End Class

End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim c1 As C1 = New T')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'c1')
    Variables: Local_1: c1 As Module1.C1
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: Module1.C1) (Syntax: 'New T')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ITypeParameterObjectCreationExpression (OperationKind.TypeParameterObjectCreationExpression, Type: T) (Syntax: 'New T')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier:=AddressOf New ExpectedSymbolVerifier().Verify)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ConversionExpression_Implicit_WideningTypeParameterConversion_InvalidNoConversion()
            Dim source = <![CDATA[
Option Strict On
Module Module1

    Sub M1(Of T As {Class, New})()
        Dim c1 As C1 = New T'BIND:"Dim c1 As C1 = New T"
    End Sub

    Class C1
    End Class

    Class C2
        Inherits C1
    End Class

End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Dim c1 As C1 = New T')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'c1')
    Variables: Local_1: c1 As Module1.C1
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: Module1.C1, IsInvalid) (Syntax: 'New T')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ITypeParameterObjectCreationExpression (OperationKind.TypeParameterObjectCreationExpression, Type: T, IsInvalid) (Syntax: 'New T')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30311: Value of type 'T' cannot be converted to 'Module1.C1'.
        Dim c1 As C1 = New T'BIND:"Dim c1 As C1 = New T"
                       ~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier:=AddressOf New ExpectedSymbolVerifier().Verify)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ConversionExpression_Implicit_WideningTypeParameterConversion_ToInterface()
            Dim source = <![CDATA[
Option Strict On
Module Module1

    Sub M1(Of T As {C1, New})()
        Dim i1 As I1 = New T'BIND:"Dim i1 As I1 = New T"
    End Sub

    Interface I1
    End Interface

    Class C1
        Implements I1
    End Class

End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim i1 As I1 = New T')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i1')
    Variables: Local_1: i1 As Module1.I1
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: Module1.I1) (Syntax: 'New T')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ITypeParameterObjectCreationExpression (OperationKind.TypeParameterObjectCreationExpression, Type: T) (Syntax: 'New T')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier:=AddressOf New ExpectedSymbolVerifier().Verify)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ConversionExpression_Implicit_WideningTypeParameterToTypeParameterConversion()
            Dim source = <![CDATA[
Option Strict On
Module Module1

    Sub M1(Of T, U As {T, New})()
        Dim t1 As T = New U'BIND:"Dim t1 As T = New U"
    End Sub

End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim t1 As T = New U')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 't1')
    Variables: Local_1: t1 As T
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: T) (Syntax: 'New U')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ITypeParameterObjectCreationExpression (OperationKind.TypeParameterObjectCreationExpression, Type: U) (Syntax: 'New U')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier:=AddressOf New ExpectedSymbolVerifier().Verify)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ConversionExpression_Implicit_WideningTypeParameterToTypeParameterConversion_InvalidNoConversion()
            Dim source = <![CDATA[
Option Strict On
Module Module1

    Sub M1(Of T, U As New)()
        Dim t1 As T = New U'BIND:"Dim t1 As T = New U"
    End Sub

End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Dim t1 As T = New U')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 't1')
    Variables: Local_1: t1 As T
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: T, IsInvalid) (Syntax: 'New U')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ITypeParameterObjectCreationExpression (OperationKind.TypeParameterObjectCreationExpression, Type: U, IsInvalid) (Syntax: 'New U')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30311: Value of type 'U' cannot be converted to 'T'.
        Dim t1 As T = New U'BIND:"Dim t1 As T = New U"
                      ~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier:=AddressOf New ExpectedSymbolVerifier().Verify)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ConversionExpression_Implicit_WideningTypeParameterFromNothing()
            Dim source = <![CDATA[
Option Strict On
Module Module1

    Sub M1(Of T)()
        Dim t1 As T = Nothing'BIND:"Dim t1 As T = Nothing"
    End Sub

End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim t1 As T = Nothing')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 't1')
    Variables: Local_1: t1 As T
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: T) (Syntax: 'Nothing')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'Nothing')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier:=AddressOf New ExpectedSymbolVerifier().Verify)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ConversionExpressin_Implicit_WideningConstantConversion()
            Dim source = <![CDATA[
Option Strict On
Module Module1

    Sub M1()
        Const i As Integer = 1
        Const l As Long = i'BIND:"Const l As Long = i"
    End Sub

End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Const l As Long = i')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'l')
    Variables: Local_1: l As System.Int64
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int64, Constant: 1) (Syntax: 'i')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32, Constant: 1) (Syntax: 'i')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC42099: Unused local constant: 'l'.
        Const l As Long = i'BIND:"Const l As Long = i"
              ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier:=AddressOf New ExpectedSymbolVerifier().Verify)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ConversionExpression_Implicit_WideningConstantExpressionConversion_InvalidConstantTooLarge()
            Dim source = <![CDATA[
Option Strict On
Module Module1

    Sub M1()
        Const i As Integer = 10000
        Const s As SByte = i'BIND:"Const s As SByte = i"
    End Sub

End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Const s As SByte = i')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 's')
    Variables: Local_1: s As System.SByte
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.SByte, IsInvalid) (Syntax: 'i')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32, Constant: 10000, IsInvalid) (Syntax: 'i')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30439: Constant expression not representable in type 'SByte'.
        Const s As SByte = i'BIND:"Const s As SByte = i"
                           ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier:=AddressOf New ExpectedSymbolVerifier().Verify)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ConversionExpression_Implicit_WideningConstantExpressionConversion_InvalidNonConstant()
            Dim source = <![CDATA[
Option Strict On
Module Module1

    Sub M1()
        Dim i As Integer = 1
        Const s As SByte = i'BIND:"Const s As SByte = i"
    End Sub

End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Const s As SByte = i')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 's')
    Variables: Local_1: s As System.SByte
    Initializer: IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: 'i')
        Children(1):
            IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.SByte, IsInvalid) (Syntax: 'i')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32, IsInvalid) (Syntax: 'i')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30512: Option Strict On disallows implicit conversions from 'Integer' to 'SByte'.
        Const s As SByte = i'BIND:"Const s As SByte = i"
                           ~
]]>.Value

            Dim verifier = New ExpectedSymbolVerifier(operationSelector:=
                                                        Function(operation As IOperation) As IConversionExpression
                                                            Dim initializer As IOperation = DirectCast(operation, IVariableDeclarationStatement).Declarations.Single().Initializer
                                                            Return DirectCast(initializer, IInvalidExpression).Children.Cast(Of IConversionExpression).Single()
                                                        End Function)

            ' TODO: We're not comparing types because the semantic model doesn't return the correct ConvertedType for this expression. See
            ' https://github.com/dotnet/roslyn/issues/20523
            'VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics,
            'additionalOperationTreeVerifier:=AddressOf verifier.Verify)
            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ConversionExpression_Implicit_WideningLambdaToExpressionTree()
            Dim source = <![CDATA[
Option Strict On
Imports System
Imports System.Linq.Expressions

Module Module1
    Sub M1()
        Dim expr As Expression(Of Func(Of Integer, Boolean)) = Function(num) num < 5'BIND:"Dim expr As Expression(Of Func(Of Integer, Boolean)) = Function(num) num < 5"
    End Sub
End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim expr As ... um) num < 5')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'expr')
    Variables: Local_1: expr As System.Linq.Expressions.Expression(Of System.Func(Of System.Int32, System.Boolean))
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Linq.Expressions.Expression(Of System.Func(Of System.Int32, System.Boolean))) (Syntax: 'Function(num) num < 5')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: IAnonymousFunctionExpression (Symbol: Function (num As System.Int32) As System.Boolean) (OperationKind.AnonymousFunctionExpression, Type: null) (Syntax: 'Function(num) num < 5')
            IBlockStatement (3 statements, 1 locals) (OperationKind.BlockStatement) (Syntax: 'Function(num) num < 5')
              Locals: Local_1: <anonymous local> As System.Boolean
              IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'num < 5')
                ReturnedValue: IBinaryOperatorExpression (BinaryOperatorKind.LessThan, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'num < 5')
                    Left: IParameterReferenceExpression: num (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'num')
                    Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 5) (Syntax: '5')
              ILabeledStatement (Label: exit) (OperationKind.LabeledStatement) (Syntax: 'Function(num) num < 5')
                Statement: null
              IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'Function(num) num < 5')
                ReturnedValue: ILocalReferenceExpression:  (OperationKind.LocalReferenceExpression, Type: System.Boolean) (Syntax: 'Function(num) num < 5')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier:=AddressOf New ExpectedSymbolVerifier().Verify)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ConversionExpression_Implicit_WideningLambdaToExpressionTree_InvalidLambda()
            Dim source = <![CDATA[
Option Strict On
Imports System
Imports System.Linq.Expressions

Module Module1
    Sub M1()
        Dim expr As Expression(Of Func(Of Integer, Boolean)) = Function(num) num'BIND:"Dim expr As Expression(Of Func(Of Integer, Boolean)) = Function(num) num"
    End Sub
End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Dim expr As ... on(num) num')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'expr')
    Variables: Local_1: expr As System.Linq.Expressions.Expression(Of System.Func(Of System.Int32, System.Boolean))
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Linq.Expressions.Expression(Of System.Func(Of System.Int32, System.Boolean)), IsInvalid) (Syntax: 'Function(num) num')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: IAnonymousFunctionExpression (Symbol: Function (num As System.Int32) As System.Boolean) (OperationKind.AnonymousFunctionExpression, Type: null, IsInvalid) (Syntax: 'Function(num) num')
            IBlockStatement (3 statements, 1 locals) (OperationKind.BlockStatement, IsInvalid) (Syntax: 'Function(num) num')
              Locals: Local_1: <anonymous local> As System.Boolean
              IReturnStatement (OperationKind.ReturnStatement, IsInvalid) (Syntax: 'num')
                ReturnedValue: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Boolean, IsInvalid) (Syntax: 'num')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: IParameterReferenceExpression: num (OperationKind.ParameterReferenceExpression, Type: System.Int32, IsInvalid) (Syntax: 'num')
              ILabeledStatement (Label: exit) (OperationKind.LabeledStatement, IsInvalid) (Syntax: 'Function(num) num')
                Statement: null
              IReturnStatement (OperationKind.ReturnStatement, IsInvalid) (Syntax: 'Function(num) num')
                ReturnedValue: ILocalReferenceExpression:  (OperationKind.LocalReferenceExpression, Type: System.Boolean, IsInvalid) (Syntax: 'Function(num) num')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30512: Option Strict On disallows implicit conversions from 'Integer' to 'Boolean'.
        Dim expr As Expression(Of Func(Of Integer, Boolean)) = Function(num) num'BIND:"Dim expr As Expression(Of Func(Of Integer, Boolean)) = Function(num) num"
                                                                             ~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier:=AddressOf New ExpectedSymbolVerifier().Verify)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ConversionExpression_Implicit_WideningReturnTypeConversion()
            Dim source = <![CDATA[
Option Strict On

Module Module1
    Function M1() As Long
        Dim i As Integer = 1
        Return i'BIND:"Return i"
    End Function
End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'Return i')
  ReturnedValue: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int64) (Syntax: 'i')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ReturnStatementSyntax)(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier:=AddressOf New ExpectedSymbolVerifier().Verify)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ConversionExpression_Implicit_WideningReturnTypeConversion_InvalidConversion()
            Dim source = <![CDATA[
Option Strict On

Module Module1
    Function M1() As SByte
        Dim i As Integer = 1
        Return i'BIND:"Return i"
    End Function
End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IReturnStatement (OperationKind.ReturnStatement, IsInvalid) (Syntax: 'Return i')
  ReturnedValue: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.SByte, IsInvalid) (Syntax: 'i')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32, IsInvalid) (Syntax: 'i')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30512: Option Strict On disallows implicit conversions from 'Integer' to 'SByte'.
        Return i'BIND:"Return i"
               ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ReturnStatementSyntax)(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier:=AddressOf New ExpectedSymbolVerifier().Verify)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ConversionExpression_Implicit_WideningInterpolatedStringToIFormattable()
            Dim source = <![CDATA[
Imports System
Module Program
    Sub M1()
        Dim formattable As IFormattable = $"{"Hello world!"}"'BIND:"Dim formattable As IFormattable = $"{"Hello world!"}""
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim formatt ... o world!"}"')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'formattable')
    Variables: Local_1: formattable As System.IFormattable
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.IFormattable) (Syntax: '$"{"Hello world!"}"')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: IInterpolatedStringExpression (OperationKind.InterpolatedStringExpression, Type: System.String) (Syntax: '$"{"Hello world!"}"')
            Parts(1):
                IInterpolation (OperationKind.Interpolation) (Syntax: '{"Hello world!"}')
                  Expression: ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "Hello world!") (Syntax: '"Hello world!"')
                  Alignment: null
                  FormatString: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ConversionExpression_Implicit_WideningUserConversion()
            Dim source = <![CDATA[
Module Program
    Sub M1(args As String())
        Dim i As Integer = 1
        Dim c1 As C1 = i'BIND:"Dim c1 As C1 = i"
    End Sub

    Class C1
        Public Shared Widening Operator CType(ByVal i As Integer) As C1
            Return New C1
        End Operator
    End Class
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim c1 As C1 = i')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'c1')
    Variables: Local_1: c1 As Program.C1
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperatorMethod: Function Program.C1.op_Implicit(i As System.Int32) As Program.C1) (OperationKind.ConversionExpression, Type: Program.C1) (Syntax: 'i')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: Function Program.C1.op_Implicit(i As System.Int32) As Program.C1)
        Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ConversionExpression_Implicit_WideningUserConversionMultiStepConversion()
            Dim source = <![CDATA[
Module Program
    Sub M1(args As String())
        Dim i As Integer = 1
        Dim c1 As C1 = i'BIND:"Dim c1 As C1 = i"
    End Sub

    Class C1
        Public Shared Widening Operator CType(ByVal i As Long) As C1
            Return New C1
        End Operator
    End Class
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim c1 As C1 = i')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'c1')
    Variables: Local_1: c1 As Program.C1
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperatorMethod: Function Program.C1.op_Implicit(i As System.Int64) As Program.C1) (OperationKind.ConversionExpression, Type: Program.C1) (Syntax: 'i')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: Function Program.C1.op_Implicit(i As System.Int64) As Program.C1)
        Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ConversionExpression_Implicit_WideningUserConversionImplicitAndExplicitConversion()
            Dim source = <![CDATA[
Module Program
    Sub M1(args As String())
        Dim c1 As New C1
        Dim c2 As C2 = CType(c1, Integer)'BIND:"Dim c2 As C2 = CType(c1, Integer)"
    End Sub

    Class C1
        Public Shared Widening Operator CType(ByVal i As C1) As Integer
            Return 1
        End Operator
    End Class

    Class C2
        Public Shared Widening Operator CType(ByVal l As Long) As C2
            Return New C2
        End Operator
    End Class
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim c2 As C ... 1, Integer)')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'c2')
    Variables: Local_1: c2 As Program.C2
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperatorMethod: Function Program.C2.op_Implicit(l As System.Int64) As Program.C2) (OperationKind.ConversionExpression, Type: Program.C2) (Syntax: 'CType(c1, Integer)')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: Function Program.C2.op_Implicit(l As System.Int64) As Program.C2)
        Operand: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperatorMethod: Function Program.C1.op_Implicit(i As Program.C1) As System.Int32) (OperationKind.ConversionExpression, Type: System.Int32) (Syntax: 'CType(c1, Integer)')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: Function Program.C1.op_Implicit(i As Program.C1) As System.Int32)
            Operand: ILocalReferenceExpression: c1 (OperationKind.LocalReferenceExpression, Type: Program.C1) (Syntax: 'c1')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact()>
        Public Sub ConversionExpression_Implicit_WideningDelegateTypeConversion()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module M1
    Sub Method1()
        Dim objectAction As Action(Of Object) = New Action(Of Object)(Sub(o As Object) Console.WriteLine(o))
        Dim stringAction As Action(Of String) = objectAction'BIND:"objectAction"
    End Sub

End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Action(Of System.String)) (Syntax: 'objectAction')
  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
  Operand: ILocalReferenceExpression: objectAction (OperationKind.LocalReferenceExpression, Type: System.Action(Of System.Object)) (Syntax: 'objectAction')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of IdentifierNameSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact()>
        Public Sub ConversionExpression_Implicit_WideningDelegateTypeConversion_InvalidConversion()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module M1
    Sub Method1()
        Dim objectAction As Action(Of Object) = New Action(Of Object)(Sub(o As Object) Console.WriteLine(o))
        Dim integerAction As Action(Of Integer) = objectAction'BIND:"objectAction"
    End Sub

End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Action(Of System.Int32), IsInvalid) (Syntax: 'objectAction')
  Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Operand: ILocalReferenceExpression: objectAction (OperationKind.LocalReferenceExpression, Type: System.Action(Of System.Object), IsInvalid) (Syntax: 'objectAction')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36755: 'Action(Of Object)' cannot be converted to 'Action(Of Integer)' because 'Integer' is not derived from 'Object', as required for the 'In' generic parameter 'T' in 'Delegate Sub Action(Of In T)(obj As T)'.
        Dim integerAction As Action(Of Integer) = objectAction'BIND:"objectAction"
                                                  ~~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of IdentifierNameSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub


#End Region

        Private Class ExpectedSymbolVerifier
            Public Shared Function ConversionOrDelegateChildSelector(conv As IOperation) As IOperation
                If (conv.Kind = OperationKind.ConversionExpression) Then
                    Return DirectCast(conv, IConversionExpression).Operand
                Else
                    Return DirectCast(conv, IDelegateCreationExpression).Target
                End If
            End Function

            Private ReadOnly _operationSelector As Func(Of IOperation, IConversionExpression)

            Private ReadOnly _operationChildSelector As Func(Of IOperation, IOperation)

            Private ReadOnly _syntaxSelector As Func(Of SyntaxNode, SyntaxNode)

            Public Sub New(Optional operationSelector As Func(Of IOperation, IConversionExpression) = Nothing,
                           Optional operationChildSelector As Func(Of IOperation, IOperation) = Nothing,
                           Optional syntaxSelector As Func(Of SyntaxNode, SyntaxNode) = Nothing)
                _operationSelector = operationSelector
                _operationChildSelector = If(operationChildSelector, AddressOf ConversionOrDelegateChildSelector)
                _syntaxSelector = syntaxSelector

            End Sub

            Public Sub Verify(operation As IOperation, compilation As Compilation, syntaxNode As SyntaxNode)
                Dim finalSyntax = GetAndInvokeSyntaxSelector(syntaxNode)
                Dim semanticModel = compilation.GetSemanticModel(finalSyntax.SyntaxTree)
                Dim typeInfo = semanticModel.GetTypeInfo(finalSyntax)

                Dim conversion = GetAndInvokeOperationSelector(operation)

                Assert.Equal(conversion.Type, typeInfo.ConvertedType)
                Assert.Equal(_operationChildSelector(conversion).Type, typeInfo.Type)
            End Sub

            Private Function GetAndInvokeSyntaxSelector(syntax As SyntaxNode) As SyntaxNode
                If _syntaxSelector IsNot Nothing Then
                    Return _syntaxSelector(syntax)
                Else
                    Select Case syntax.Kind()
                        Case SyntaxKind.LocalDeclarationStatement
                            Return DirectCast(syntax, LocalDeclarationStatementSyntax).Declarators.Single().Initializer.Value
                        Case SyntaxKind.ReturnStatement
                            Return DirectCast(syntax, ReturnStatementSyntax).Expression
                        Case Else
                            Throw New ArgumentException($"Cannot handle syntax of type {syntax.GetType()}")
                    End Select
                End If
            End Function

            Private Function GetAndInvokeOperationSelector(operation As IOperation) As IOperation
                If _operationSelector IsNot Nothing Then
                    Return _operationSelector(operation)
                Else
                    Select Case operation.Kind
                        Case OperationKind.VariableDeclarationStatement
                            Return DirectCast(operation, IVariableDeclarationStatement).Declarations.Single().Initializer
                        Case OperationKind.ReturnStatement
                            Return DirectCast(operation, IReturnStatement).ReturnedValue
                        Case Else
                            Throw New ArgumentException($"Cannot handle operation of type {operation.GetType()}")
                    End Select
                End If
            End Function
        End Class
    End Class
End Namespace
