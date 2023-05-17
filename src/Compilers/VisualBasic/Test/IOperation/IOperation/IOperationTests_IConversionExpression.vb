' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Operations
Imports Roslyn.Test.Utilities

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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim s As St ... g = Nothing')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 's As String = Nothing')
    Declarators:
        IVariableDeclaratorOperation (Symbol: s As System.String) (OperationKind.VariableDeclarator, Type: null) (Syntax: 's')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= Nothing')
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String, Constant: null, IsImplicit) (Syntax: 'Nothing')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'Nothing')
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim s As In ... r = Nothing')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 's As Integer = Nothing')
    Declarators:
        IVariableDeclaratorOperation (Symbol: s As System.Int32) (OperationKind.VariableDeclarator, Type: null) (Syntax: 's')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= Nothing')
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, Constant: 0, IsImplicit) (Syntax: 'Nothing')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'Nothing')
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim s As Double = 1')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 's As Double = 1')
    Declarators:
        IVariableDeclaratorOperation (Symbol: s As System.Double) (OperationKind.VariableDeclarator, Type: null) (Syntax: 's')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 1')
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Double, Constant: 1, IsImplicit) (Syntax: '1')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim a As A = 0')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'a As A = 0')
    Declarators:
        IVariableDeclaratorOperation (Symbol: a As Program.A) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'a')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 0')
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: Program.A, Constant: 0, IsImplicit) (Syntax: '0')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim a As A = 1')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'a As A = 1')
    Declarators:
        IVariableDeclaratorOperation (Symbol: a As Program.A) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'a')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 1')
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: Program.A, Constant: 1, IsImplicit) (Syntax: '1')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'Dim a As A = 1')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'a As A = 1')
    Declarators:
        IVariableDeclaratorOperation (Symbol: a As Program.A) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'a')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= 1')
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: Program.A, Constant: 1, IsInvalid, IsImplicit) (Syntax: '1')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim a As A = i')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'a As A = i')
    Declarators:
        IVariableDeclaratorOperation (Symbol: a As Program.A) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'a')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= i')
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: Program.A, IsImplicit) (Syntax: 'i')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'Dim a As A = i')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'a As A = i')
    Declarators:
        IVariableDeclaratorOperation (Symbol: a As Program.A) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'a')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= i')
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: Program.A, IsInvalid, IsImplicit) (Syntax: 'i')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32, IsInvalid) (Syntax: 'i')
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'Dim a As A =')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'a As A =')
    Declarators:
        IVariableDeclaratorOperation (Symbol: a As Program.A) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'a')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '=')
        IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'Dim a As Integer = b + c')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'a As Integer = b + c')
    Declarators:
        IVariableDeclaratorOperation (Symbol: a As System.Int32) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'a')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= b + c')
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'b + c')
          Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: ?, IsInvalid) (Syntax: 'b + c')
              Left: 
                IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'b')
                  Children(0)
              Right: 
                IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'c')
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim i As Integer = A.Two')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'i As Integer = A.Two')
    Declarators:
        IVariableDeclaratorOperation (Symbol: i As System.Int32) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= A.Two')
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: 'A.Two')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            IFieldReferenceOperation: Program.A.Two (Static) (OperationKind.FieldReference, Type: Program.A, Constant: 2) (Syntax: 'A.Two')
              Instance Receiver: 
                null
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim i As Single = A.Two')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'i As Single = A.Two')
    Declarators:
        IVariableDeclaratorOperation (Symbol: i As System.Single) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= A.Two')
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Single, Constant: 2, IsImplicit) (Syntax: 'A.Two')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            IFieldReferenceOperation: Program.A.Two (Static) (OperationKind.FieldReference, Type: Program.A, Constant: 2) (Syntax: 'A.Two')
              Instance Receiver: 
                null
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'Dim i As Integer = A.Two')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'i As Integer = A.Two')
    Declarators:
        IVariableDeclaratorOperation (Symbol: i As System.Int32) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= A.Two')
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, Constant: 2, IsInvalid, IsImplicit) (Syntax: 'A.Two')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            IFieldReferenceOperation: Program.A.Two (Static) (OperationKind.FieldReference, Type: Program.A, Constant: 2, IsInvalid) (Syntax: 'A.Two')
              Instance Receiver: 
                null
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim i As Single = 1.0')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'i As Single = 1.0')
    Declarators:
        IVariableDeclaratorOperation (Symbol: i As System.Single) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 1.0')
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Single, Constant: 1, IsImplicit) (Syntax: '1.0')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            ILiteralOperation (OperationKind.Literal, Type: System.Double, Constant: 1) (Syntax: '1.0')
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim s As String = b')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 's As String = b')
    Declarators:
        IVariableDeclaratorOperation (Symbol: s As System.String) (OperationKind.VariableDeclarator, Type: null) (Syntax: 's')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= b')
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String, IsImplicit) (Syntax: 'b')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            ILocalReferenceOperation: b (OperationKind.LocalReference, Type: System.Boolean) (Syntax: 'b')
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'Dim s As String = b')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 's As String = b')
    Declarators:
        IVariableDeclaratorOperation (Symbol: s As System.String) (OperationKind.VariableDeclarator, Type: null) (Syntax: 's')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= b')
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String, IsInvalid, IsImplicit) (Syntax: 'b')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            ILocalReferenceOperation: b (OperationKind.LocalReference, Type: System.Boolean, IsInvalid) (Syntax: 'b')
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim c1 As C1 = c2')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'c1 As C1 = c2')
    Declarators:
        IVariableDeclaratorOperation (Symbol: c1 As Program.C1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c1')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= c2')
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: Program.C1, IsImplicit) (Syntax: 'c2')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            ILocalReferenceOperation: c2 (OperationKind.LocalReference, Type: Program.C2) (Syntax: 'c2')
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'Dim c1 As C1 = c2')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'c1 As C1 = c2')
    Declarators:
        IVariableDeclaratorOperation (Symbol: c1 As Program.C1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c1')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= c2')
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: Program.C1, IsInvalid, IsImplicit) (Syntax: 'c2')
          Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            ILocalReferenceOperation: c2 (OperationKind.LocalReference, Type: Program.C2, IsInvalid) (Syntax: 'c2')
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim i1 As I1 = c1')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'i1 As I1 = c1')
    Declarators:
        IVariableDeclaratorOperation (Symbol: i1 As Program.I1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i1')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= c1')
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: Program.I1, IsImplicit) (Syntax: 'c1')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            ILocalReferenceOperation: c1 (OperationKind.LocalReference, Type: Program.C1) (Syntax: 'c1')
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'Dim i1 As I1 = c1')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'i1 As I1 = c1')
    Declarators:
        IVariableDeclaratorOperation (Symbol: i1 As Program.I1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i1')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= c1')
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: Program.I1, IsInvalid, IsImplicit) (Syntax: 'c1')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            ILocalReferenceOperation: c1 (OperationKind.LocalReference, Type: Program.C1, IsInvalid) (Syntax: 'c1')
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim i1 As I1 = c1')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'i1 As I1 = c1')
    Declarators:
        IVariableDeclaratorOperation (Symbol: i1 As Program.I1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i1')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= c1')
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: Program.I1, IsImplicit) (Syntax: 'c1')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            ILocalReferenceOperation: c1 (OperationKind.LocalReference, Type: Program.C1) (Syntax: 'c1')
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim o As Object = i')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'o As Object = i')
    Declarators:
        IVariableDeclaratorOperation (Symbol: o As System.Object) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'o')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= i')
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'i')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            ILocalReferenceOperation: i (OperationKind.LocalReference, Type: Program.I1) (Syntax: 'i')
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim i1List  ... 1) = i2List')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'i1List As I ... 1) = i2List')
    Declarators:
        IVariableDeclaratorOperation (Symbol: i1List As System.Collections.Generic.IEnumerable(Of Program.I1)) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i1List')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= i2List')
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEnumerable(Of Program.I1), IsImplicit) (Syntax: 'i2List')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            ILocalReferenceOperation: i2List (OperationKind.LocalReference, Type: System.Collections.Generic.IList(Of Program.I2)) (Syntax: 'i2List')
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim a As Ac ... End Sub')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'a As Action ... End Sub')
    Declarators:
        IVariableDeclaratorOperation (Symbol: a As System.Action(Of System.Int32)) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'a')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= Sub(i As  ... End Sub')
        IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action(Of System.Int32), IsImplicit) (Syntax: 'Sub(i As In ... End Sub')
          Target: 
            IAnonymousFunctionOperation (Symbol: Sub (i As System.Int32)) (OperationKind.AnonymousFunction, Type: null) (Syntax: 'Sub(i As In ... End Sub')
              IBlockOperation (2 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Sub(i As In ... End Sub')
                ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'End Sub')
                  Statement: 
                    null
                IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'End Sub')
                  ReturnedValue: 
                    null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier:=AddressOf New ExpectedSymbolVerifier().Verify)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ConversionExpression_Implicit_WideningLambdaToDelegate_JustInitializerReturnsOnlyLambda()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module Program
    Sub M1()
        Dim a As Action(Of Integer) = Sub(i As Integer)'BIND:"Sub(i As Integer)"
                                      End Sub
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IAnonymousFunctionOperation (Symbol: Sub (i As System.Int32)) (OperationKind.AnonymousFunction, Type: null) (Syntax: 'Sub(i As In ... End Sub')
  IBlockOperation (2 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Sub(i As In ... End Sub')
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim a As Ac ... End Sub')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'a As Action ... End Sub')
    Declarators:
        IVariableDeclaratorOperation (Symbol: a As System.Action(Of System.Int32)) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'a')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= Sub()'BIN ... End Sub')
        IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action(Of System.Int32), IsImplicit) (Syntax: 'Sub()'BIND: ... End Sub')
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'Dim a As Ac ... End Sub')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'a As Action ... End Sub')
    Declarators:
        IVariableDeclaratorOperation (Symbol: a As System.Action) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'a')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= Sub(i As  ... End Sub')
        IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action, IsInvalid, IsImplicit) (Syntax: 'Sub(i As In ... End Sub')
          Target: 
            IAnonymousFunctionOperation (Symbol: Sub (i As System.Int32)) (OperationKind.AnonymousFunction, Type: null, IsInvalid) (Syntax: 'Sub(i As In ... End Sub')
              IBlockOperation (2 statements) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'Sub(i As In ... End Sub')
                ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsInvalid, IsImplicit) (Syntax: 'End Sub')
                  Statement: 
                    null
                IReturnOperation (OperationKind.Return, Type: null, IsInvalid, IsImplicit) (Syntax: 'End Sub')
                  ReturnedValue: 
                    null
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim a As Fu ... nd Function')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'a As Func(O ... nd Function')
    Declarators:
        IVariableDeclaratorOperation (Symbol: a As System.Func(Of System.Int64)) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'a')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= Function( ... nd Function')
        IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int64), IsImplicit) (Syntax: 'Function()  ... nd Function')
          Target: 
            IAnonymousFunctionOperation (Symbol: Function () As System.Int32) (OperationKind.AnonymousFunction, Type: null) (Syntax: 'Function()  ... nd Function')
              IBlockOperation (3 statements, 1 locals) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Function()  ... nd Function')
                Locals: Local_1: <anonymous local> As System.Int32
                IReturnOperation (OperationKind.Return, Type: null) (Syntax: 'Return 1')
                  ReturnedValue: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'End Function')
                  Statement: 
                    null
                IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'End Function')
                  ReturnedValue: 
                    ILocalReferenceOperation:  (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'End Function')
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim a As Ac ... nd Function')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'a As Action ... nd Function')
    Declarators:
        IVariableDeclaratorOperation (Symbol: a As System.Action(Of System.Int32)) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'a')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= Function( ... nd Function')
        IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action(Of System.Int32), IsImplicit) (Syntax: 'Function()  ... nd Function')
          Target: 
            IAnonymousFunctionOperation (Symbol: Function () As System.Int32) (OperationKind.AnonymousFunction, Type: null) (Syntax: 'Function()  ... nd Function')
              IBlockOperation (3 statements, 1 locals) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Function()  ... nd Function')
                Locals: Local_1: <anonymous local> As System.Int32
                IReturnOperation (OperationKind.Return, Type: null) (Syntax: 'Return 1')
                  ReturnedValue: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'End Function')
                  Statement: 
                    null
                IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'End Function')
                  ReturnedValue: 
                    ILocalReferenceOperation:  (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'End Function')
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'Dim a As Fu ... End Sub')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'a As Func(O ... End Sub')
    Declarators:
        IVariableDeclaratorOperation (Symbol: a As System.Func(Of System.Int32)) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'a')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= Sub()'BIN ... End Sub')
        IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32), IsInvalid, IsImplicit) (Syntax: 'Sub()'BIND: ... End Sub')
          Target: 
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim a As Ac ... ddressOf M2')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'a As Action ... ddressOf M2')
    Declarators:
        IVariableDeclaratorOperation (Symbol: a As System.Action) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'a')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= AddressOf M2')
        IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action, IsImplicit) (Syntax: 'AddressOf M2')
          Target: 
            IMethodReferenceOperation: Sub Program.M2() (Static) (OperationKind.MethodReference, Type: null) (Syntax: 'AddressOf M2')
              Instance Receiver: 
                null
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim a As Ac ... ddressOf M2')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'a As Action ... ddressOf M2')
    Declarators:
        IVariableDeclaratorOperation (Symbol: a As System.Action(Of System.Int32)) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'a')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= AddressOf M2')
        IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action(Of System.Int32), IsImplicit) (Syntax: 'AddressOf M2')
          Target: 
            IMethodReferenceOperation: Sub Program.M2() (Static) (OperationKind.MethodReference, Type: null) (Syntax: 'AddressOf M2')
              Instance Receiver: 
                null
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'Dim a As Ac ... ddressOf M2')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'a As Action ... ddressOf M2')
    Declarators:
        IVariableDeclaratorOperation (Symbol: a As System.Action) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'a')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= AddressOf M2')
        IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action, IsInvalid, IsImplicit) (Syntax: 'AddressOf M2')
          Target: 
            IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'AddressOf M2')
              Children(1):
                  IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'M2')
                    Children(1):
                        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Program, IsInvalid, IsImplicit) (Syntax: 'M2')
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'Dim a As Ac ... essOf c1.M2')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'a As Action ... essOf c1.M2')
    Declarators:
        IVariableDeclaratorOperation (Symbol: a As System.Action) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'a')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= AddressOf c1.M2')
        IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action, IsInvalid, IsImplicit) (Syntax: 'AddressOf c1.M2')
          Target: 
            IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'AddressOf c1.M2')
              Children(1):
                  IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'c1.M2')
                    Children(1):
                        IOperation:  (OperationKind.None, Type: Program.C1, IsInvalid) (Syntax: 'c1')
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim a As Ac ... ddressOf M2')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'a As Action ... ddressOf M2')
    Declarators:
        IVariableDeclaratorOperation (Symbol: a As System.Action) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'a')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= AddressOf M2')
        IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action, IsImplicit) (Syntax: 'AddressOf M2')
          Target: 
            IMethodReferenceOperation: Function Program.M2() As System.Int32 (Static) (OperationKind.MethodReference, Type: null) (Syntax: 'AddressOf M2')
              Instance Receiver: 
                null
]]>.Value
            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ConversionExpression_Implicit_WideningMethodGroupToDelegate_RelaxationReturnType_JustInitializerReturnsOnlyMethodReference()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module Program
    Sub M1()
        Dim a As Action = AddressOf M2'BIND:"AddressOf M2"
    End Sub

    Function M2() As Integer
        Return 1
    End Function
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IMethodReferenceOperation: Function Program.M2() As System.Int32 (Static) (OperationKind.MethodReference, Type: null) (Syntax: 'AddressOf M2')
  Instance Receiver: 
    null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim a As Fu ... ddressOf M2')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'a As Func(O ... ddressOf M2')
    Declarators:
        IVariableDeclaratorOperation (Symbol: a As System.Func(Of System.Int64)) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'a')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= AddressOf M2')
        IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int64), IsImplicit) (Syntax: 'AddressOf M2')
          Target: 
            IMethodReferenceOperation: Function Program.M2() As System.Int32 (Static) (OperationKind.MethodReference, Type: null) (Syntax: 'AddressOf M2')
              Instance Receiver: 
                null
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'Dim a As Fu ... ddressOf M2')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'a As Func(O ... ddressOf M2')
    Declarators:
        IVariableDeclaratorOperation (Symbol: a As System.Func(Of System.Int64)) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'a')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= AddressOf M2')
        IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int64), IsInvalid, IsImplicit) (Syntax: 'AddressOf M2')
          Target: 
            IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'AddressOf M2')
              Children(1):
                  IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'M2')
                    Children(1):
                        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Program, IsInvalid, IsImplicit) (Syntax: 'M2')
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'Dim a As Ac ... = AddressOf')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'a As Action = AddressOf')
    Declarators:
        IVariableDeclaratorOperation (Symbol: a As System.Action) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'a')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= AddressOf')
        IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action, IsInvalid, IsImplicit) (Syntax: 'AddressOf')
          Target: 
            IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'AddressOf')
              Children(1):
                  IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim a As Ar ... teger(1) {}')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'a As Array  ... teger(1) {}')
    Declarators:
        IVariableDeclaratorOperation (Symbol: a As System.Array) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'a')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= New Integer(1) {}')
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Array, IsImplicit) (Syntax: 'New Integer(1) {}')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32()) (Syntax: 'New Integer(1) {}')
              Dimension Sizes(1):
                  IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: '1')
                    Left: 
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                    Right: 
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '1')
              Initializer: 
                IArrayInitializerOperation (0 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{}')
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim a As Ar ... ger(1)() {}')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'a As Array  ... ger(1)() {}')
    Declarators:
        IVariableDeclaratorOperation (Symbol: a As System.Array) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'a')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= New Integer(1)() {}')
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Array, IsImplicit) (Syntax: 'New Integer(1)() {}')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32()()) (Syntax: 'New Integer(1)() {}')
              Dimension Sizes(1):
                  IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: '1')
                    Left: 
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                    Right: 
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '1')
              Initializer: 
                IArrayInitializerOperation (0 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{}')
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'Dim a As Ar ...  New Object')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'a As Array = New Object')
    Declarators:
        IVariableDeclaratorOperation (Symbol: a As System.Array) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'a')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= New Object')
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Array, IsInvalid, IsImplicit) (Syntax: 'New Object')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            IObjectCreationOperation (Constructor: Sub System.Object..ctor()) (OperationKind.ObjectCreation, Type: System.Object, IsInvalid) (Syntax: 'New Object')
              Arguments(0)
              Initializer: 
                null
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim c1List  ... () = c2List')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'c1List As C1() = c2List')
    Declarators:
        IVariableDeclaratorOperation (Symbol: c1List As Program.C1()) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c1List')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= c2List')
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: Program.C1(), IsImplicit) (Syntax: 'c2List')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            ILocalReferenceOperation: c2List (OperationKind.LocalReference, Type: Program.C2()) (Syntax: 'c2List')
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'Dim c1List  ... () = c2List')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'c1List As C1() = c2List')
    Declarators:
        IVariableDeclaratorOperation (Symbol: c1List As Program.C1()) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c1List')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= c2List')
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: Program.C1(), IsInvalid, IsImplicit) (Syntax: 'c2List')
          Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            ILocalReferenceOperation: c2List (OperationKind.LocalReference, Type: Program.C2()(), IsInvalid) (Syntax: 'c2List')
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'Dim c1List  ... () = c2List')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'c1List As C1() = c2List')
    Declarators:
        IVariableDeclaratorOperation (Symbol: c1List As Program.C1()) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c1List')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= c2List')
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: Program.C1(), IsInvalid, IsImplicit) (Syntax: 'c2List')
          Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            ILocalReferenceOperation: c2List (OperationKind.LocalReference, Type: Program.C2(), IsInvalid) (Syntax: 'c2List')
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim c1List  ... 1) = c2List')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'c1List As I ... 1) = c2List')
    Declarators:
        IVariableDeclaratorOperation (Symbol: c1List As System.Collections.Generic.IEnumerable(Of Program.C1)) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c1List')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= c2List')
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEnumerable(Of Program.C1), IsImplicit) (Syntax: 'c2List')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            ILocalReferenceOperation: c2List (OperationKind.LocalReference, Type: Program.C2()) (Syntax: 'c2List')
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'Dim c1List  ... 1) = c2List')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'c1List As I ... 1) = c2List')
    Declarators:
        IVariableDeclaratorOperation (Symbol: c1List As System.Collections.Generic.IEnumerable(Of Program.C1)) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c1List')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= c2List')
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEnumerable(Of Program.C1), IsInvalid, IsImplicit) (Syntax: 'c2List')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            ILocalReferenceOperation: c2List (OperationKind.LocalReference, Type: Program.C2(), IsInvalid) (Syntax: 'c2List')
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim i1 As I1 = s1')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'i1 As I1 = s1')
    Declarators:
        IVariableDeclaratorOperation (Symbol: i1 As Program.I1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i1')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= s1')
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: Program.I1, IsImplicit) (Syntax: 's1')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            ILocalReferenceOperation: s1 (OperationKind.LocalReference, Type: Program.S1) (Syntax: 's1')
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim v1 As ValueType = s1')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'v1 As ValueType = s1')
    Declarators:
        IVariableDeclaratorOperation (Symbol: v1 As System.ValueType) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'v1')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= s1')
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.ValueType, IsImplicit) (Syntax: 's1')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            ILocalReferenceOperation: s1 (OperationKind.LocalReference, Type: Program.S1) (Syntax: 's1')
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'Dim i1 As I1 = s1')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'i1 As I1 = s1')
    Declarators:
        IVariableDeclaratorOperation (Symbol: i1 As Program.I1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i1')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= s1')
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: Program.I1, IsInvalid, IsImplicit) (Syntax: 's1')
          Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            ILocalReferenceOperation: s1 (OperationKind.LocalReference, Type: Program.S1, IsInvalid) (Syntax: 's1')
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim i As Integer? = 1')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'i As Integer? = 1')
    Declarators:
        IVariableDeclaratorOperation (Symbol: i As System.Nullable(Of System.Int32)) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 1')
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: '1')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim l As Long? = i')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'l As Long? = i')
    Declarators:
        IVariableDeclaratorOperation (Symbol: l As System.Nullable(Of System.Int64)) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'l')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= i')
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Nullable(Of System.Int64), IsImplicit) (Syntax: 'i')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'i')
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim l As Long? = 1')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'l As Long? = 1')
    Declarators:
        IVariableDeclaratorOperation (Symbol: l As System.Nullable(Of System.Int64)) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'l')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 1')
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Nullable(Of System.Int64), IsImplicit) (Syntax: '1')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim i1 As I1 = s1')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'i1 As I1 = s1')
    Declarators:
        IVariableDeclaratorOperation (Symbol: i1 As Program.I1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i1')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= s1')
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: Program.I1, IsImplicit) (Syntax: 's1')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            ILocalReferenceOperation: s1 (OperationKind.LocalReference, Type: System.Nullable(Of Program.S1)) (Syntax: 's1')
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim s1 As S ...  Char(1) {}')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 's1 As Strin ...  Char(1) {}')
    Declarators:
        IVariableDeclaratorOperation (Symbol: s1 As System.String) (OperationKind.VariableDeclarator, Type: null) (Syntax: 's1')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= New Char(1) {}')
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String, IsImplicit) (Syntax: 'New Char(1) {}')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Char()) (Syntax: 'New Char(1) {}')
              Dimension Sizes(1):
                  IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: '1')
                    Left: 
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                    Right: 
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '1')
              Initializer: 
                IArrayInitializerOperation (0 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{}')
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim s1 As String = "a"c')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 's1 As String = "a"c')
    Declarators:
        IVariableDeclaratorOperation (Symbol: s1 As System.String) (OperationKind.VariableDeclarator, Type: null) (Syntax: 's1')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= "a"c')
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String, Constant: "a", IsImplicit) (Syntax: '"a"c')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            ILiteralOperation (OperationKind.Literal, Type: System.Char, Constant: a) (Syntax: '"a"c')
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim c1 As C1 = c3')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'c1 As C1 = c3')
    Declarators:
        IVariableDeclaratorOperation (Symbol: c1 As Module1.C1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c1')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= c3')
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: Module1.C1, IsImplicit) (Syntax: 'c3')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            ILocalReferenceOperation: c3 (OperationKind.LocalReference, Type: Module1.C3) (Syntax: 'c3')
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim c1 As C1 = New T')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'c1 As C1 = New T')
    Declarators:
        IVariableDeclaratorOperation (Symbol: c1 As Module1.C1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c1')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= New T')
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: Module1.C1, IsImplicit) (Syntax: 'New T')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            ITypeParameterObjectCreationOperation (OperationKind.TypeParameterObjectCreation, Type: T) (Syntax: 'New T')
              Initializer: 
                null
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'Dim c1 As C1 = New T')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'c1 As C1 = New T')
    Declarators:
        IVariableDeclaratorOperation (Symbol: c1 As Module1.C1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c1')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= New T')
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: Module1.C1, IsInvalid, IsImplicit) (Syntax: 'New T')
          Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            ITypeParameterObjectCreationOperation (OperationKind.TypeParameterObjectCreation, Type: T, IsInvalid) (Syntax: 'New T')
              Initializer: 
                null
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim i1 As I1 = New T')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'i1 As I1 = New T')
    Declarators:
        IVariableDeclaratorOperation (Symbol: i1 As Module1.I1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i1')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= New T')
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: Module1.I1, IsImplicit) (Syntax: 'New T')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            ITypeParameterObjectCreationOperation (OperationKind.TypeParameterObjectCreation, Type: T) (Syntax: 'New T')
              Initializer: 
                null
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim t1 As T = New U')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 't1 As T = New U')
    Declarators:
        IVariableDeclaratorOperation (Symbol: t1 As T) (OperationKind.VariableDeclarator, Type: null) (Syntax: 't1')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= New U')
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: T, IsImplicit) (Syntax: 'New U')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            ITypeParameterObjectCreationOperation (OperationKind.TypeParameterObjectCreation, Type: U) (Syntax: 'New U')
              Initializer: 
                null
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'Dim t1 As T = New U')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 't1 As T = New U')
    Declarators:
        IVariableDeclaratorOperation (Symbol: t1 As T) (OperationKind.VariableDeclarator, Type: null) (Syntax: 't1')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= New U')
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: T, IsInvalid, IsImplicit) (Syntax: 'New U')
          Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            ITypeParameterObjectCreationOperation (OperationKind.TypeParameterObjectCreation, Type: U, IsInvalid) (Syntax: 'New U')
              Initializer: 
                null
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim t1 As T = Nothing')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 't1 As T = Nothing')
    Declarators:
        IVariableDeclaratorOperation (Symbol: t1 As T) (OperationKind.VariableDeclarator, Type: null) (Syntax: 't1')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= Nothing')
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: T, IsImplicit) (Syntax: 'Nothing')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'Nothing')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier:=AddressOf New ExpectedSymbolVerifier().Verify)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ConversionExpression_Implicit_WideningConstantConversion()
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Const l As Long = i')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'l As Long = i')
    Declarators:
        IVariableDeclaratorOperation (Symbol: l As System.Int64) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'l')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= i')
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int64, Constant: 1, IsImplicit) (Syntax: 'i')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32, Constant: 1) (Syntax: 'i')
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'Const s As SByte = i')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 's As SByte = i')
    Declarators:
        IVariableDeclaratorOperation (Symbol: s As System.SByte) (OperationKind.VariableDeclarator, Type: null) (Syntax: 's')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= i')
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.SByte, IsInvalid, IsImplicit) (Syntax: 'i')
          Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32, Constant: 10000, IsInvalid) (Syntax: 'i')
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'Const s As SByte = i')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 's As SByte = i')
    Declarators:
        IVariableDeclaratorOperation (Symbol: s As System.SByte) (OperationKind.VariableDeclarator, Type: null) (Syntax: 's')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= i')
        IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: 'i')
          Children(1):
              IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.SByte, IsInvalid, IsImplicit) (Syntax: 'i')
                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: 
                  ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32, IsInvalid) (Syntax: 'i')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30059: Constant expression is required.
        Const s As SByte = i'BIND:"Const s As SByte = i"
                           ~
BC30512: Option Strict On disallows implicit conversions from 'Integer' to 'SByte'.
        Const s As SByte = i'BIND:"Const s As SByte = i"
                           ~
]]>.Value

            Dim verifier = New ExpectedSymbolVerifier(operationSelector:=
                                                        Function(operation As IOperation) As IConversionOperation
                                                            Dim initializer As IVariableInitializerOperation = DirectCast(operation, IVariableDeclarationGroupOperation).Declarations.Single().Initializer
                                                            Dim initializerValue As IOperation = initializer.Value
                                                            Return DirectCast(initializerValue, IInvalidOperation).ChildOperations.Cast(Of IConversionOperation).Single()
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim expr As ... um) num < 5')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'expr As Exp ... um) num < 5')
    Declarators:
        IVariableDeclaratorOperation (Symbol: expr As System.Linq.Expressions.Expression(Of System.Func(Of System.Int32, System.Boolean))) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'expr')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= Function(num) num < 5')
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Linq.Expressions.Expression(Of System.Func(Of System.Int32, System.Boolean)), IsImplicit) (Syntax: 'Function(num) num < 5')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            IAnonymousFunctionOperation (Symbol: Function (num As System.Int32) As System.Boolean) (OperationKind.AnonymousFunction, Type: null) (Syntax: 'Function(num) num < 5')
              IBlockOperation (3 statements, 1 locals) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Function(num) num < 5')
                Locals: Local_1: <anonymous local> As System.Boolean
                IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'num < 5')
                  ReturnedValue: 
                    IBinaryOperation (BinaryOperatorKind.LessThan, Checked) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'num < 5')
                      Left: 
                        IParameterReferenceOperation: num (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'num')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')
                ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'Function(num) num < 5')
                  Statement: 
                    null
                IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'Function(num) num < 5')
                  ReturnedValue: 
                    ILocalReferenceOperation:  (OperationKind.LocalReference, Type: System.Boolean, IsImplicit) (Syntax: 'Function(num) num < 5')
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'Dim expr As ... on(num) num')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'expr As Exp ... on(num) num')
    Declarators:
        IVariableDeclaratorOperation (Symbol: expr As System.Linq.Expressions.Expression(Of System.Func(Of System.Int32, System.Boolean))) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'expr')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= Function(num) num')
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Linq.Expressions.Expression(Of System.Func(Of System.Int32, System.Boolean)), IsInvalid, IsImplicit) (Syntax: 'Function(num) num')
          Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            IAnonymousFunctionOperation (Symbol: Function (num As System.Int32) As System.Boolean) (OperationKind.AnonymousFunction, Type: null, IsInvalid) (Syntax: 'Function(num) num')
              IBlockOperation (3 statements, 1 locals) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'Function(num) num')
                Locals: Local_1: <anonymous local> As System.Boolean
                IReturnOperation (OperationKind.Return, Type: null, IsInvalid, IsImplicit) (Syntax: 'num')
                  ReturnedValue: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'num')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      Operand: 
                        IParameterReferenceOperation: num (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'num')
                ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsInvalid, IsImplicit) (Syntax: 'Function(num) num')
                  Statement: 
                    null
                IReturnOperation (OperationKind.Return, Type: null, IsInvalid, IsImplicit) (Syntax: 'Function(num) num')
                  ReturnedValue: 
                    ILocalReferenceOperation:  (OperationKind.LocalReference, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'Function(num) num')
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
IReturnOperation (OperationKind.Return, Type: null) (Syntax: 'Return i')
  ReturnedValue: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int64, IsImplicit) (Syntax: 'i')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
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
IReturnOperation (OperationKind.Return, Type: null, IsInvalid) (Syntax: 'Return i')
  ReturnedValue: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.SByte, IsInvalid, IsImplicit) (Syntax: 'i')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32, IsInvalid) (Syntax: 'i')
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim formatt ... o world!"}"')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'formattable ... o world!"}"')
    Declarators:
        IVariableDeclaratorOperation (Symbol: formattable As System.IFormattable) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'formattable')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= $"{"Hello world!"}"')
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IFormattable, IsImplicit) (Syntax: '$"{"Hello world!"}"')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$"{"Hello world!"}"')
              Parts(1):
                  IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{"Hello world!"}')
                    Expression: 
                      ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "Hello world!") (Syntax: '"Hello world!"')
                    Alignment: 
                      null
                    FormatString: 
                      null
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim c1 As C1 = i')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'c1 As C1 = i')
    Declarators:
        IVariableDeclaratorOperation (Symbol: c1 As Program.C1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c1')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= i')
        IConversionOperation (TryCast: False, Unchecked) (OperatorMethod: Function Program.C1.op_Implicit(i As System.Int32) As Program.C1) (OperationKind.Conversion, Type: Program.C1, IsImplicit) (Syntax: 'i')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: Function Program.C1.op_Implicit(i As System.Int32) As Program.C1)
          Operand: 
            ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim c1 As C1 = i')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'c1 As C1 = i')
    Declarators:
        IVariableDeclaratorOperation (Symbol: c1 As Program.C1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c1')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= i')
        IConversionOperation (TryCast: False, Unchecked) (OperatorMethod: Function Program.C1.op_Implicit(i As System.Int64) As Program.C1) (OperationKind.Conversion, Type: Program.C1, IsImplicit) (Syntax: 'i')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: Function Program.C1.op_Implicit(i As System.Int64) As Program.C1)
          Operand: 
            ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim c2 As C ... 1, Integer)')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'c2 As C2 =  ... 1, Integer)')
    Declarators:
        IVariableDeclaratorOperation (Symbol: c2 As Program.C2) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c2')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= CType(c1, Integer)')
        IConversionOperation (TryCast: False, Unchecked) (OperatorMethod: Function Program.C2.op_Implicit(l As System.Int64) As Program.C2) (OperationKind.Conversion, Type: Program.C2, IsImplicit) (Syntax: 'CType(c1, Integer)')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: Function Program.C2.op_Implicit(l As System.Int64) As Program.C2)
          Operand: 
            IConversionOperation (TryCast: False, Unchecked) (OperatorMethod: Function Program.C1.op_Implicit(i As Program.C1) As System.Int32) (OperationKind.Conversion, Type: System.Int32) (Syntax: 'CType(c1, Integer)')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: Function Program.C1.op_Implicit(i As Program.C1) As System.Int32)
              Operand: 
                ILocalReferenceOperation: c1 (OperationKind.LocalReference, Type: Program.C1) (Syntax: 'c1')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ConversionExpression_Implicit_WideningDelegateTypeConversion()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module M1
    Sub Method1()
        Dim objectAction As Action(Of Object) = New Action(Of Object)(Sub(o As Object) Console.WriteLine(o))
        Dim stringAction As Action(Of String) = objectAction'BIND:"Dim stringAction As Action(Of String) = objectAction"
    End Sub

End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim stringA ... bjectAction')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'stringActio ... bjectAction')
    Declarators:
        IVariableDeclaratorOperation (Symbol: stringAction As System.Action(Of System.String)) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'stringAction')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= objectAction')
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Action(Of System.String), IsImplicit) (Syntax: 'objectAction')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            ILocalReferenceOperation: objectAction (OperationKind.LocalReference, Type: System.Action(Of System.Object)) (Syntax: 'objectAction')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ConversionExpression_Implicit_WideningDelegateTypeConversion_InvalidConversion()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module M1
    Sub Method1()
        Dim objectAction As Action(Of Object) = New Action(Of Object)(Sub(o As Object) Console.WriteLine(o))
        Dim integerAction As Action(Of Integer) = objectAction'BIND:"Dim integerAction As Action(Of Integer) = objectAction"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'Dim integer ... bjectAction')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'integerActi ... bjectAction')
    Declarators:
        IVariableDeclaratorOperation (Symbol: integerAction As System.Action(Of System.Int32)) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'integerAction')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= objectAction')
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Action(Of System.Int32), IsInvalid, IsImplicit) (Syntax: 'objectAction')
          Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            ILocalReferenceOperation: objectAction (OperationKind.LocalReference, Type: System.Action(Of System.Object), IsInvalid) (Syntax: 'objectAction')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36755: 'Action(Of Object)' cannot be converted to 'Action(Of Integer)' because 'Integer' is not derived from 'Object', as required for the 'In' generic parameter 'T' in 'Delegate Sub Action(Of In T)(obj As T)'.
        Dim integerAction As Action(Of Integer) = objectAction'BIND:"Dim integerAction As Action(Of Integer) = objectAction"
                                                  ~~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Conversion_CType_ParenthesizedExpressionTree()
            Dim source = <![CDATA[
Imports System
Imports System.Linq.Expressions

Public Class C
    Public Sub M1()
        Dim a = CType(((Function(ByVal i) i < 5)), Expression(Of Func(Of Integer, Boolean)))'BIND:"CType(((Function(ByVal i) i < 5)), Expression(Of Func(Of Integer, Boolean)))"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Linq.Expressions.Expression(Of System.Func(Of System.Int32, System.Boolean))) (Syntax: 'CType(((Fun ...  Boolean)))')
  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Operand: 
    IParenthesizedOperation (OperationKind.Parenthesized, Type: null) (Syntax: '((Function( ...  i) i < 5))')
      Operand: 
        IParenthesizedOperation (OperationKind.Parenthesized, Type: null) (Syntax: '(Function(B ... l i) i < 5)')
          Operand: 
            IAnonymousFunctionOperation (Symbol: Function (i As System.Int32) As System.Boolean) (OperationKind.AnonymousFunction, Type: null) (Syntax: 'Function(ByVal i) i < 5')
              IBlockOperation (3 statements, 1 locals) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Function(ByVal i) i < 5')
                Locals: Local_1: <anonymous local> As System.Boolean
                IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'i < 5')
                  ReturnedValue: 
                    IBinaryOperation (BinaryOperatorKind.LessThan, Checked) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'i < 5')
                      Left: 
                        IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')
                ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'Function(ByVal i) i < 5')
                  Statement: 
                    null
                IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'Function(ByVal i) i < 5')
                  ReturnedValue: 
                    ILocalReferenceOperation:  (OperationKind.LocalReference, Type: System.Boolean, IsImplicit) (Syntax: 'Function(ByVal i) i < 5')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of CTypeExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Conversion_DirectCast_ParenthesizedExpressionTree()
            Dim source = <![CDATA[
Imports System
Imports System.Linq.Expressions

Public Class C
    Public Sub M1()
        Dim a = DirectCast(((Function(ByVal i) i < 5)), Expression(Of Func(Of Integer, Boolean)))'BIND:"DirectCast(((Function(ByVal i) i < 5)), Expression(Of Func(Of Integer, Boolean)))"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Linq.Expressions.Expression(Of System.Func(Of System.Int32, System.Boolean))) (Syntax: 'DirectCast( ...  Boolean)))')
  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Operand: 
    IParenthesizedOperation (OperationKind.Parenthesized, Type: null) (Syntax: '((Function( ...  i) i < 5))')
      Operand: 
        IParenthesizedOperation (OperationKind.Parenthesized, Type: null) (Syntax: '(Function(B ... l i) i < 5)')
          Operand: 
            IAnonymousFunctionOperation (Symbol: Function (i As System.Int32) As System.Boolean) (OperationKind.AnonymousFunction, Type: null) (Syntax: 'Function(ByVal i) i < 5')
              IBlockOperation (3 statements, 1 locals) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Function(ByVal i) i < 5')
                Locals: Local_1: <anonymous local> As System.Boolean
                IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'i < 5')
                  ReturnedValue: 
                    IBinaryOperation (BinaryOperatorKind.LessThan, Checked) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'i < 5')
                      Left: 
                        IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')
                ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'Function(ByVal i) i < 5')
                  Statement: 
                    null
                IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'Function(ByVal i) i < 5')
                  ReturnedValue: 
                    ILocalReferenceOperation:  (OperationKind.LocalReference, Type: System.Boolean, IsImplicit) (Syntax: 'Function(ByVal i) i < 5')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of DirectCastExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Conversion_TryCast_ParenthesizedExpressionTree()
            Dim source = <![CDATA[
Imports System
Imports System.Linq.Expressions

Public Class C
    Public Sub M1()
        Dim a = TryCast(((Function(ByVal i) i < 5)), Expression(Of Func(Of Integer, Boolean)))'BIND:"TryCast(((Function(ByVal i) i < 5)), Expression(Of Func(Of Integer, Boolean)))"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IConversionOperation (TryCast: True, Unchecked) (OperationKind.Conversion, Type: System.Linq.Expressions.Expression(Of System.Func(Of System.Int32, System.Boolean))) (Syntax: 'TryCast(((F ...  Boolean)))')
  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Operand: 
    IParenthesizedOperation (OperationKind.Parenthesized, Type: null) (Syntax: '((Function( ...  i) i < 5))')
      Operand: 
        IParenthesizedOperation (OperationKind.Parenthesized, Type: null) (Syntax: '(Function(B ... l i) i < 5)')
          Operand: 
            IAnonymousFunctionOperation (Symbol: Function (i As System.Int32) As System.Boolean) (OperationKind.AnonymousFunction, Type: null) (Syntax: 'Function(ByVal i) i < 5')
              IBlockOperation (3 statements, 1 locals) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Function(ByVal i) i < 5')
                Locals: Local_1: <anonymous local> As System.Boolean
                IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'i < 5')
                  ReturnedValue: 
                    IBinaryOperation (BinaryOperatorKind.LessThan, Checked) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'i < 5')
                      Left: 
                        IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')
                ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'Function(ByVal i) i < 5')
                  Statement: 
                    null
                IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'Function(ByVal i) i < 5')
                  ReturnedValue: 
                    ILocalReferenceOperation:  (OperationKind.LocalReference, Type: System.Boolean, IsImplicit) (Syntax: 'Function(ByVal i) i < 5')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of TryCastExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Conversion_Implicit_ParenthesizedExpressionTree()
            Dim source = <![CDATA[
Imports System
Imports System.Linq.Expressions

Public Class C
    Public Sub M1()
        Dim a As Expression(Of Func(Of Integer, Boolean)) = ((Function(ByVal i) i < 5))'BIND:"= ((Function(ByVal i) i < 5))"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= ((Functio ...  i) i < 5))')
  IParenthesizedOperation (OperationKind.Parenthesized, Type: System.Linq.Expressions.Expression(Of System.Func(Of System.Int32, System.Boolean))) (Syntax: '((Function( ...  i) i < 5))')
    Operand: 
      IParenthesizedOperation (OperationKind.Parenthesized, Type: System.Linq.Expressions.Expression(Of System.Func(Of System.Int32, System.Boolean))) (Syntax: '(Function(B ... l i) i < 5)')
        Operand: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Linq.Expressions.Expression(Of System.Func(Of System.Int32, System.Boolean)), IsImplicit) (Syntax: 'Function(ByVal i) i < 5')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IAnonymousFunctionOperation (Symbol: Function (i As System.Int32) As System.Boolean) (OperationKind.AnonymousFunction, Type: null) (Syntax: 'Function(ByVal i) i < 5')
                IBlockOperation (3 statements, 1 locals) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Function(ByVal i) i < 5')
                  Locals: Local_1: <anonymous local> As System.Boolean
                  IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'i < 5')
                    ReturnedValue: 
                      IBinaryOperation (BinaryOperatorKind.LessThan, Checked) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'i < 5')
                        Left: 
                          IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i')
                        Right: 
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')
                  ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'Function(ByVal i) i < 5')
                    Statement: 
                      null
                  IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'Function(ByVal i) i < 5')
                    ReturnedValue: 
                      ILocalReferenceOperation:  (OperationKind.LocalReference, Type: System.Boolean, IsImplicit) (Syntax: 'Function(ByVal i) i < 5')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of EqualsValueSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

#End Region

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        <WorkItem(23203, "https://github.com/dotnet/roslyn/issues/23203")>
        Public Sub ConversionExpression_IntegerOverflow()
            Dim source = <![CDATA[
Imports System

Module Module1

    Class C1
        Shared Widening Operator CType(x As Byte) As C1
            Return Nothing
        End Operator
    End Class

    Sub Main()

        Dim z1 As C1 = &H7FFFFFFFL 'BIND:"= &H7FFFFFFFL"
    End Sub
End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= &H7FFFFFFFL')
  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: Module1.C1, IsInvalid, IsImplicit) (Syntax: '&H7FFFFFFFL')
    Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    Operand: 
      ILiteralOperation (OperationKind.Literal, Type: System.Int64, Constant: 2147483647, IsInvalid) (Syntax: '&H7FFFFFFFL')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30439: Constant expression not representable in type 'Byte'.
        Dim z1 As C1 = &H7FFFFFFFL 'BIND:"= &H7FFFFFFFL"
                       ~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of EqualsValueSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ConversionFlow_01()
            Dim source = <![CDATA[
Imports System

Public Class C
    Public Sub M1(i As Integer, l As Long) 'BIND:"Public Sub M1(i As Integer, l As Long)"
        l = i
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'l = i')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int64, IsImplicit) (Syntax: 'l = i')
              Left: 
                IParameterReferenceOperation: l (OperationKind.ParameterReference, Type: System.Int64) (Syntax: 'l')
              Right: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int64, IsImplicit) (Syntax: 'i')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (WideningNumeric)
                  Operand: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i')

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ConversionFlow_02()
            Dim source = <![CDATA[
Imports System

Public Class C
    Public Sub M1(i As Integer, b As Boolean, l As Long, m As Long) 'BIND:"Public Sub M1(i As Integer, b As Boolean, l As Long, m As Long)"
        i = CType(If(b,l,m), Integer)
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i')
              Value: 
                IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i')

        Jump if False (Regular) to Block[B3]
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'l')
              Value: 
                IParameterReferenceOperation: l (OperationKind.ParameterReference, Type: System.Int64) (Syntax: 'l')

        Next (Regular) Block[B4]
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'm')
              Value: 
                IParameterReferenceOperation: m (OperationKind.ParameterReference, Type: System.Int64) (Syntax: 'm')

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'i = CType(I ... ), Integer)')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'i = CType(I ... ), Integer)')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32) (Syntax: 'CType(If(b, ... ), Integer)')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (NarrowingNumeric)
                      Operand: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int64, IsImplicit) (Syntax: 'If(b,l,m)')

        Next (Regular) Block[B5]
            Leaving: {R1}
}

Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        Private Class ExpectedSymbolVerifier
            Public Shared Function ConversionOrDelegateChildSelector(conv As IOperation) As IOperation
                If (conv.Kind = OperationKind.Conversion) Then
                    Return DirectCast(conv, IConversionOperation).Operand
                Else
                    Return DirectCast(conv, IDelegateCreationOperation).Target
                End If
            End Function

            Private ReadOnly _operationSelector As Func(Of IOperation, IConversionOperation)

            Private ReadOnly _operationChildSelector As Func(Of IOperation, IOperation)

            Private ReadOnly _syntaxSelector As Func(Of SyntaxNode, SyntaxNode)

            Public Sub New(Optional operationSelector As Func(Of IOperation, IConversionOperation) = Nothing,
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
                        Case OperationKind.VariableDeclarationGroup
                            Return DirectCast(operation, IVariableDeclarationGroupOperation).Declarations.Single().Initializer.Value
                        Case OperationKind.Return
                            Return DirectCast(operation, IReturnOperation).ReturnedValue
                        Case Else
                            Throw New ArgumentException($"Cannot handle operation of type {operation.GetType()}")
                    End Select
                End If
            End Function
        End Class
    End Class
End Namespace
