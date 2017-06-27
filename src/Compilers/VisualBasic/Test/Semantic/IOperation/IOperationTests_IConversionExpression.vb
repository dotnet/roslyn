Imports Microsoft.CodeAnalysis.Semantics
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase

#Region "Widening Conversions"

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
    Initializer: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.String, Constant: null) (Syntax: 'Nothing')
        ILiteralExpression (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'Nothing')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier:=AddressOf New ExpectedSymbolVerifier().Verify)
        End Sub

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
    Initializer: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 0) (Syntax: 'Nothing')
        ILiteralExpression (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'Nothing')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier:=AddressOf New ExpectedSymbolVerifier().Verify)
        End Sub

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
    Initializer: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Double, Constant: 1) (Syntax: '1')
        ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier:=AddressOf New ExpectedSymbolVerifier().Verify)
        End Sub

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
    Initializer: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: Program.A, Constant: 0) (Syntax: '0')
        ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            ' We don't look for the type here. Semantic model doesn't have a conversion here
            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

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
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'a')
    Variables: Local_1: a As Program.A
    Initializer: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: Program.A, IsInvalid) (Syntax: '')
        IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30201: Expression expected.
        Dim a As A ='BIND:"Dim a As A ="
                    ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

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
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'a')
    Variables: Local_1: a As System.Int32
    Initializer: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid) (Syntax: 'b + c')
        IBinaryOperatorExpression (BinaryOperationKind.Invalid) (OperationKind.BinaryOperatorExpression, Type: ?, IsInvalid) (Syntax: 'b + c')
          Left: IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: 'b')
          Right: IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: 'c')
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
    Initializer: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 2) (Syntax: 'A.Two')
        IFieldReferenceExpression: Program.A.Two (Static) (OperationKind.FieldReferenceExpression, Type: Program.A, Constant: 2) (Syntax: 'A.Two')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier:=AddressOf New ExpectedSymbolVerifier().Verify)
        End Sub

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
    Initializer: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Single, Constant: 2) (Syntax: 'A.Two')
        IFieldReferenceExpression: Program.A.Two (Static) (OperationKind.FieldReferenceExpression, Type: Program.A, Constant: 2) (Syntax: 'A.Two')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim i As Integer = A.Two')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i')
    Variables: Local_1: i As System.Int32
    Initializer: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 2) (Syntax: 'A.Two')
        IFieldReferenceExpression: Program.A.Two (Static) (OperationKind.FieldReferenceExpression, Type: Program.A, Constant: 2) (Syntax: 'A.Two')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30512: Option Strict On disallows implicit conversions from 'Program.A' to 'Integer'.
        Dim i As Integer = A.Two'BIND:"Dim i As Integer = A.Two"
                           ~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

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
    Initializer: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Single, Constant: 1) (Syntax: '1.0')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Double, Constant: 1) (Syntax: '1.0')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier:=AddressOf New ExpectedSymbolVerifier().Verify)
        End Sub

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
    Initializer: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.String) (Syntax: 'b')
        ILocalReferenceExpression: b (OperationKind.LocalReferenceExpression, Type: System.Boolean) (Syntax: 'b')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier:=AddressOf New ExpectedSymbolVerifier().Verify)
        End Sub

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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim s As String = b')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 's')
    Variables: Local_1: s As System.String
    Initializer: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.String) (Syntax: 'b')
        ILocalReferenceExpression: b (OperationKind.LocalReferenceExpression, Type: System.Boolean) (Syntax: 'b')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30512: Option Strict On disallows implicit conversions from 'Boolean' to 'String'.
        Dim s As String = b'BIND:"Dim s As String = b"
                          ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier:=AddressOf New ExpectedSymbolVerifier().Verify)
        End Sub

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
    Initializer: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: Program.C1) (Syntax: 'c2')
        ILocalReferenceExpression: c2 (OperationKind.LocalReferenceExpression, Type: Program.C2) (Syntax: 'c2')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier:=AddressOf New ExpectedSymbolVerifier().Verify)
        End Sub

        <Fact()>
        Public Sub ConversionExpression_Implicit_NarrowingClassToBaseClass_InvalidNoConversion()
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
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'c1')
    Variables: Local_1: c1 As Program.C1
    Initializer: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: Program.C1, IsInvalid) (Syntax: 'c2')
        ILocalReferenceExpression: c2 (OperationKind.LocalReferenceExpression, Type: Program.C2) (Syntax: 'c2')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30311: Value of type 'Program.C2' cannot be converted to 'Program.C1'.
        Dim c1 As C1 = c2'BIND:"Dim c1 As C1 = c2"
                       ~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier:=AddressOf New ExpectedSymbolVerifier().Verify)
        End Sub

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
    Initializer: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: Program.I1) (Syntax: 'c1')
        ILocalReferenceExpression: c1 (OperationKind.LocalReferenceExpression, Type: Program.C1) (Syntax: 'c1')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier:=AddressOf New ExpectedSymbolVerifier().Verify)
        End Sub

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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim i1 As I1 = c1')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i1')
    Variables: Local_1: i1 As Program.I1
    Initializer: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: Program.I1) (Syntax: 'c1')
        ILocalReferenceExpression: c1 (OperationKind.LocalReferenceExpression, Type: Program.C1) (Syntax: 'c1')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30512: Option Strict On disallows implicit conversions from 'Program.C1' to 'Program.I1'.
        Dim i1 As I1 = c1'BIND:"Dim i1 As I1 = c1"
                       ~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier:=AddressOf New ExpectedSymbolVerifier().Verify)
        End Sub

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
    Initializer: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: Program.I1) (Syntax: 'c1')
        ILocalReferenceExpression: c1 (OperationKind.LocalReferenceExpression, Type: Program.C1) (Syntax: 'c1')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier:=AddressOf New ExpectedSymbolVerifier().Verify)
        End Sub

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
    Initializer: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: 'i')
        ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: Program.I1) (Syntax: 'i')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier:=AddressOf New ExpectedSymbolVerifier().Verify)
        End Sub

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
    Initializer: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Collections.Generic.IEnumerable(Of Program.I1)) (Syntax: 'i2List')
        ILocalReferenceExpression: i2List (OperationKind.LocalReferenceExpression, Type: System.Collections.Generic.IList(Of Program.I2)) (Syntax: 'i2List')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier:=AddressOf New ExpectedSymbolVerifier().Verify)
        End Sub

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
    Initializer: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Action(Of System.Int32)) (Syntax: 'Sub(i As In ... End Sub')
        ILambdaExpression (Signature: Sub (i As System.Int32)) (OperationKind.LambdaExpression, Type: null) (Syntax: 'Sub(i As In ... End Sub')
          IBlockStatement (2 statements) (OperationKind.BlockStatement) (Syntax: 'Sub(i As In ... End Sub')
            ILabelStatement (Label: exit) (OperationKind.LabelStatement) (Syntax: 'End Sub')
            IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'End Sub')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier:=AddressOf New ExpectedSymbolVerifier().Verify)
        End Sub

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
    Initializer: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Action(Of System.Int32)) (Syntax: 'Sub()'BIND: ... End Sub')
        ILambdaExpression (Signature: Sub ()) (OperationKind.LambdaExpression, Type: null) (Syntax: 'Sub()'BIND: ... End Sub')
          IBlockStatement (2 statements) (OperationKind.BlockStatement) (Syntax: 'Sub()'BIND: ... End Sub')
            ILabelStatement (Label: exit) (OperationKind.LabelStatement) (Syntax: 'End Sub')
            IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'End Sub')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier:=AddressOf New ExpectedSymbolVerifier().Verify)
        End Sub

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
    Initializer: IOperation:  (OperationKind.None) (Syntax: 'AddressOf M2')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

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
    Initializer: IOperation:  (OperationKind.None) (Syntax: 'AddressOf M2')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

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
    Initializer: IOperation:  (OperationKind.None) (Syntax: 'AddressOf M2')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

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
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'a')
    Variables: Local_1: a As System.Action
    Initializer: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Action, IsInvalid) (Syntax: 'AddressOf')
        IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: 'AddressOf')
          Children(1): IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30201: Expression expected.
        Dim a As Action = AddressOf'BIND:"Dim a As Action = AddressOf"
                                   ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

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
    Initializer: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Action(Of System.Int32)) (Syntax: 'Function()  ... nd Function')
        ILambdaExpression (Signature: Function () As System.Int32) (OperationKind.LambdaExpression, Type: null) (Syntax: 'Function()  ... nd Function')
          IBlockStatement (3 statements, 1 locals) (OperationKind.BlockStatement) (Syntax: 'Function()  ... nd Function')
            Locals: Local_1: <anonymous local> As System.Int32
            IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'Return 1')
              ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
            ILabelStatement (Label: exit) (OperationKind.LabelStatement) (Syntax: 'End Function')
            IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'End Function')
              ILocalReferenceExpression:  (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'End Function')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier:=AddressOf New ExpectedSymbolVerifier().Verify)
        End Sub

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
    Initializer: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Array) (Syntax: 'New Integer(1) {}')
        IArrayCreationExpression (Element Type: System.Int32) (OperationKind.ArrayCreationExpression, Type: System.Int32()) (Syntax: 'New Integer(1) {}')
          Dimension Sizes(1): IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32, Constant: 2) (Syntax: '1')
              Left: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
              Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
          Initializer: IArrayInitializer (0 elements) (OperationKind.ArrayInitializer) (Syntax: '{}')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier:=AddressOf New ExpectedSymbolVerifier().Verify)
        End Sub

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
    Initializer: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: Program.C1()) (Syntax: 'c2List')
        ILocalReferenceExpression: c2List (OperationKind.LocalReferenceExpression, Type: Program.C2()) (Syntax: 'c2List')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier:=AddressOf New ExpectedSymbolVerifier().Verify)
        End Sub

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
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'c1List')
    Variables: Local_1: c1List As Program.C1()
    Initializer: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: Program.C1(), IsInvalid) (Syntax: 'c2List')
        ILocalReferenceExpression: c2List (OperationKind.LocalReferenceExpression, Type: Program.C2()) (Syntax: 'c2List')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30332: Value of type 'Program.C2()' cannot be converted to 'Program.C1()' because 'Program.C2' is not derived from 'Program.C1'.
        Dim c1List As C1() = c2List'BIND:"Dim c1List As C1() = c2List"
                             ~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier:=AddressOf New ExpectedSymbolVerifier().Verify)
        End Sub

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
    Initializer: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Collections.Generic.IEnumerable(Of Program.C1)) (Syntax: 'c2List')
        ILocalReferenceExpression: c2List (OperationKind.LocalReferenceExpression, Type: Program.C2()) (Syntax: 'c2List')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier:=AddressOf New ExpectedSymbolVerifier().Verify)
        End Sub

        <Fact()>
        Public Sub ConversionExpression_Implicit_WideningArrayToSystemIEnumberable_InvalidNoConversion()
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim c1List  ... 1) = c2List')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'c1List')
    Variables: Local_1: c1List As System.Collections.Generic.IEnumerable(Of Program.C1)
    Initializer: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Collections.Generic.IEnumerable(Of Program.C1)) (Syntax: 'c2List')
        ILocalReferenceExpression: c2List (OperationKind.LocalReferenceExpression, Type: Program.C2()) (Syntax: 'c2List')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36754: 'Program.C2()' cannot be converted to 'IEnumerable(Of Program.C1)' because 'Program.C2' is not derived from 'Program.C1', as required for the 'Out' generic parameter 'T' in 'Interface IEnumerable(Of Out T)'.
        Dim c1List As IEnumerable(Of C1) = c2List'BIND:"Dim c1List As IEnumerable(Of C1) = c2List"
                                           ~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier:=AddressOf New ExpectedSymbolVerifier().Verify)
        End Sub

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
    Initializer: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: Program.I1) (Syntax: 's1')
        ILocalReferenceExpression: s1 (OperationKind.LocalReferenceExpression, Type: Program.S1) (Syntax: 's1')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

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
    Initializer: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.ValueType) (Syntax: 's1')
        ILocalReferenceExpression: s1 (OperationKind.LocalReferenceExpression, Type: Program.S1) (Syntax: 's1')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

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
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'i1')
    Variables: Local_1: i1 As Program.I1
    Initializer: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: Program.I1, IsInvalid) (Syntax: 's1')
        ILocalReferenceExpression: s1 (OperationKind.LocalReferenceExpression, Type: Program.S1) (Syntax: 's1')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30311: Value of type 'Program.S1' cannot be converted to 'Program.I1'.
        Dim i1 As I1 = s1'BIND:"Dim i1 As I1 = s1"
                       ~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

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
    Initializer: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Nullable(Of System.Int32)) (Syntax: '1')
        ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier:=AddressOf New ExpectedSymbolVerifier().Verify)
        End Sub

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
    Initializer: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Nullable(Of System.Int64)) (Syntax: 'i')
        ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 'i')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

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
    Initializer: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Nullable(Of System.Int64)) (Syntax: '1')
        ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier:=AddressOf New ExpectedSymbolVerifier().Verify)
        End Sub

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
    Initializer: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: Program.I1) (Syntax: 's1')
        ILocalReferenceExpression: s1 (OperationKind.LocalReferenceExpression, Type: System.Nullable(Of Program.S1)) (Syntax: 's1')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier:=AddressOf New ExpectedSymbolVerifier().Verify)
        End Sub

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
    Initializer: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.String) (Syntax: 'New Char(1) {}')
        IArrayCreationExpression (Element Type: System.Char) (OperationKind.ArrayCreationExpression, Type: System.Char()) (Syntax: 'New Char(1) {}')
          Dimension Sizes(1): IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32, Constant: 2) (Syntax: '1')
              Left: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
              Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
          Initializer: IArrayInitializer (0 elements) (OperationKind.ArrayInitializer) (Syntax: '{}')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

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
    Initializer: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.String, Constant: "a") (Syntax: '"a"c')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Char, Constant: a) (Syntax: '"a"c')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics,
                additionalOperationTreeVerifier:=AddressOf New ExpectedSymbolVerifier().Verify)
        End Sub

#End Region

        Private Class ExpectedSymbolVerifier
            Public Shared Function ConversionExpressionChildSelector(conv As IConversionExpression) As IOperation
                Return conv.Operand
            End Function

            Public Property OperationSelector As Func(Of IOperation, IConversionExpression)

            Public Property ConversionChildSelector As Func(Of IConversionExpression, IOperation) = AddressOf ConversionExpressionChildSelector

            Public Property SyntaxSelector As Func(Of SyntaxNode, SyntaxNode)

            Public Sub Verify(operation As IOperation, compilation As Compilation, syntaxNode As SyntaxNode)
                Dim finalSyntax = GetAndInvokeSyntaxSelector(syntaxNode)
                Dim semanticModel = compilation.GetSemanticModel(finalSyntax.SyntaxTree)
                Dim typeInfo = semanticModel.GetTypeInfo(finalSyntax)

                Dim conversion = GetAndInvokeOperationSelector(operation)

                Assert.Equal(conversion.Type, typeInfo.ConvertedType)
                Assert.Equal(ConversionChildSelector(conversion).Type, typeInfo.Type)
            End Sub

            Private Function GetAndInvokeSyntaxSelector(syntax As SyntaxNode) As SyntaxNode
                If SyntaxSelector IsNot Nothing Then
                    Return SyntaxSelector(syntax)
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

            Private Function GetAndInvokeOperationSelector(operation As IOperation) As IConversionExpression
                If OperationSelector IsNot Nothing Then
                    Return OperationSelector(operation)
                Else
                    Select Case operation.Kind
                        Case OperationKind.VariableDeclarationStatement
                            Dim initializer As IOperation = DirectCast(operation, IVariableDeclarationStatement).Declarations.Single().Initializer
                            Return DirectCast(initializer, IConversionExpression)
                        Case OperationKind.ReturnStatement
                            Dim value As IOperation = DirectCast(operation, IReturnStatement).ReturnedValue
                            Return DirectCast(value, IConversionExpression)
                        Case Else
                            Throw New ArgumentException($"Cannot handle operation of type {operation.GetType()}")
                    End Select
                End If
            End Function
        End Class
    End Class
End Namespace