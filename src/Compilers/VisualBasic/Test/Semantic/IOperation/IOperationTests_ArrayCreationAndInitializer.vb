' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase

        <Fact, WorkItem(17596, "https://github.com/dotnet/roslyn/issues/17596")>
        Public Sub SimpleArrayCreation_PrimitiveType()
            Dim source = <![CDATA[
Class C
    Public Sub F()
        Dim a = New String(0) {}'BIND:"New String(0) {}"
    End Sub
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: System.String()) (Syntax: 'New String(0) {}')
  Dimension Sizes(1):
      IBinaryOperatorExpression (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32, Constant: 1) (Syntax: '0')
        Left: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
        Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '0')
  Initializer: IArrayInitializer (0 elements) (OperationKind.ArrayInitializer) (Syntax: '{}')
      Element Values(0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ArrayCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact, WorkItem(17596, "https://github.com/dotnet/roslyn/issues/17596")>
        Public Sub SimpleArrayCreation_UserDefinedType()
            Dim source = <![CDATA[
Class M
End Class

Class C
    Public Sub F()
        Dim a = New M() {}'BIND:"New M() {}"
    End Sub
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: M()) (Syntax: 'New M() {}')
  Dimension Sizes(1):
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: 'New M() {}')
  Initializer: IArrayInitializer (0 elements) (OperationKind.ArrayInitializer) (Syntax: '{}')
      Element Values(0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ArrayCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact, WorkItem(17596, "https://github.com/dotnet/roslyn/issues/17596")>
        Public Sub SimpleArrayCreation_ConstantDimension()
            Dim source = <![CDATA[
Class M
End Class

Class C
    Public Sub F()
        Const dimension As Integer = 1
        Dim a = New M(dimension) {}'BIND:"New M(dimension) {}"
    End Sub
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: M()) (Syntax: 'New M(dimension) {}')
  Dimension Sizes(1):
      IBinaryOperatorExpression (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32, Constant: 2) (Syntax: 'dimension')
        Left: ILocalReferenceExpression: dimension (OperationKind.LocalReferenceExpression, Type: System.Int32, Constant: 1) (Syntax: 'dimension')
        Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'dimension')
  Initializer: IArrayInitializer (0 elements) (OperationKind.ArrayInitializer) (Syntax: '{}')
      Element Values(0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ArrayCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact, WorkItem(17596, "https://github.com/dotnet/roslyn/issues/17596")>
        Public Sub SimpleArrayCreation_NonConstantDimension()
            Dim source = <![CDATA[
Class M
End Class

Class C
    Public Sub F(dimension As Integer)
        Dim a = New M(dimension) {}'BIND:"New M(dimension) {}"
    End Sub
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: M()) (Syntax: 'New M(dimension) {}')
  Dimension Sizes(1):
      IBinaryOperatorExpression (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'dimension')
        Left: IParameterReferenceExpression: dimension (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'dimension')
        Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'dimension')
  Initializer: IArrayInitializer (0 elements) (OperationKind.ArrayInitializer) (Syntax: '{}')
      Element Values(0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ArrayCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact, WorkItem(17596, "https://github.com/dotnet/roslyn/issues/17596")>
        Public Sub SimpleArrayCreation_DimensionWithImplicitConversion()
            Dim source = <![CDATA[
Imports System

Class M
End Class

Class C
    Public Sub F(dimension As UInt16)
        Dim a = New M(dimension) {}'BIND:"New M(dimension) {}"
    End Sub
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: M()) (Syntax: 'New M(dimension) {}')
  Dimension Sizes(1):
      IBinaryOperatorExpression (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'dimension')
        Left: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32) (Syntax: 'dimension')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: IParameterReferenceExpression: dimension (OperationKind.ParameterReferenceExpression, Type: System.UInt16) (Syntax: 'dimension')
        Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'dimension')
  Initializer: IArrayInitializer (0 elements) (OperationKind.ArrayInitializer) (Syntax: '{}')
      Element Values(0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ArrayCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact, WorkItem(17596, "https://github.com/dotnet/roslyn/issues/17596")>
        Public Sub SimpleArrayCreation_DimensionWithExplicitConversion()
            Dim source = <![CDATA[
Class M
End Class

Class C
    Public Sub F(dimension As Object)
        Dim a = New M(DirectCast(dimension, Integer)) {}'BIND:"New M(DirectCast(dimension, Integer)) {}"
    End Sub
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: M()) (Syntax: 'New M(Direc ... nteger)) {}')
  Dimension Sizes(1):
      IBinaryOperatorExpression (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'DirectCast( ... n, Integer)')
        Left: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32) (Syntax: 'DirectCast( ... n, Integer)')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: IParameterReferenceExpression: dimension (OperationKind.ParameterReferenceExpression, Type: System.Object) (Syntax: 'dimension')
        Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'DirectCast( ... n, Integer)')
  Initializer: IArrayInitializer (0 elements) (OperationKind.ArrayInitializer) (Syntax: '{}')
      Element Values(0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ArrayCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact, WorkItem(17596, "https://github.com/dotnet/roslyn/issues/17596")>
        Public Sub ArrayCreationWithInitializer_PrimitiveType()
            Dim source = <![CDATA[
Class C
    Public Sub F(dimension As Object)
        Dim a = New String() {String.Empty}'BIND:"New String() {String.Empty}"
    End Sub
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: System.String()) (Syntax: 'New String( ... ring.Empty}')
  Dimension Sizes(1):
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'New String( ... ring.Empty}')
  Initializer: IArrayInitializer (1 elements) (OperationKind.ArrayInitializer) (Syntax: '{String.Empty}')
      Element Values(1):
          IFieldReferenceExpression: System.String.Empty As System.String (Static) (OperationKind.FieldReferenceExpression, Type: System.String) (Syntax: 'String.Empty')
            Instance Receiver: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ArrayCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact, WorkItem(17596, "https://github.com/dotnet/roslyn/issues/17596")>
        Public Sub ArrayCreationWithInitializer_WithExplicitDimension()
            Dim source = <![CDATA[
Class C
    Public Sub F()
        Dim a = New C(1) {New C, Nothing}'BIND:"New C(1) {New C, Nothing}"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: C()) (Syntax: 'New C(1) {N ... C, Nothing}')
  Dimension Sizes(1):
      IBinaryOperatorExpression (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32, Constant: 2) (Syntax: '1')
        Left: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
  Initializer: IArrayInitializer (2 elements) (OperationKind.ArrayInitializer) (Syntax: '{New C, Nothing}')
      Element Values(2):
          IObjectCreationExpression (Constructor: Sub C..ctor()) (OperationKind.ObjectCreationExpression, Type: C) (Syntax: 'New C')
            Arguments(0)
            Initializer: null
          IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: C, Constant: null) (Syntax: 'Nothing')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'Nothing')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ArrayCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact, WorkItem(17596, "https://github.com/dotnet/roslyn/issues/17596")>
        Public Sub ArrayCreationWithInitializerErrorCase_WithIncorrectExplicitDimension()
            Dim source = <![CDATA[
Class C
    Public Sub F()
        Dim a = New C(2) {New C}'BIND:"New C(2) {New C}"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: C(), IsInvalid) (Syntax: 'New C(2) {New C}')
  Dimension Sizes(1):
      IBinaryOperatorExpression (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32, Constant: 3) (Syntax: '2')
        Left: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
        Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '2')
  Initializer: IArrayInitializer (1 elements) (OperationKind.ArrayInitializer, IsInvalid) (Syntax: '{New C}')
      Element Values(1):
          IObjectCreationExpression (Constructor: Sub C..ctor()) (OperationKind.ObjectCreationExpression, Type: C, IsInvalid) (Syntax: 'New C')
            Arguments(0)
            Initializer: null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30567: Array initializer is missing 2 elements.
        Dim a = New C(2) {New C}'BIND:"New C(2) {New C}"
                         ~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ArrayCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact, WorkItem(17596, "https://github.com/dotnet/roslyn/issues/17596")>
        Public Sub ArrayCreationWithInitializerErrorCase_WithNonConstantExpressionExplicitDimension()
            Dim source = <![CDATA[
Class C
    Public Sub F()
        Dim x = New Integer(2) {1, 2, 3}
        x = New Integer(x(0)) {1, 2}'BIND:"New Integer(x(0)) {1, 2}"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: System.Int32(), IsInvalid) (Syntax: 'New Integer(x(0)) {1, 2}')
  Dimension Sizes(1):
      IBinaryOperatorExpression (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'x(0)')
        Left: IArrayElementReferenceExpression (OperationKind.ArrayElementReferenceExpression, Type: System.Int32) (Syntax: 'x(0)')
            Array reference: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32()) (Syntax: 'x')
            Indices(1):
                ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
        Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'x(0)')
  Initializer: IArrayInitializer (2 elements) (OperationKind.ArrayInitializer, IsInvalid) (Syntax: '{1, 2}')
      Element Values(2):
          ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
          ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2, IsInvalid) (Syntax: '2')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30949: Array initializer cannot be specified for a non constant dimension; use the empty initializer '{}'.
        x = New Integer(x(0)) {1, 2}'BIND:"New Integer(x(0)) {1, 2}"
                              ~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ArrayCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact, WorkItem(17596, "https://github.com/dotnet/roslyn/issues/17596")>
        Public Sub ArrayCreationWithInitializer_UserDefinedType()
            Dim source = <![CDATA[
Class M
End Class

Class C
    Public Sub F(dimension As Object)
        Dim a = New M() {New M}'BIND:"New M() {New M}"
    End Sub
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: M()) (Syntax: 'New M() {New M}')
  Dimension Sizes(1):
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'New M() {New M}')
  Initializer: IArrayInitializer (1 elements) (OperationKind.ArrayInitializer) (Syntax: '{New M}')
      Element Values(1):
          IObjectCreationExpression (Constructor: Sub M..ctor()) (OperationKind.ObjectCreationExpression, Type: M) (Syntax: 'New M')
            Arguments(0)
            Initializer: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ArrayCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact, WorkItem(17596, "https://github.com/dotnet/roslyn/issues/17596")>
        Public Sub ArrayCreationWithInitializer_ImplicitlyTyped()
            Dim source = <![CDATA[
Class M
End Class

Class C
    Public Sub F(dimension As Object)
        Dim a = {New M}'BIND:"{New M}"
    End Sub
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: M()) (Syntax: '{New M}')
  Dimension Sizes(1):
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '{New M}')
  Initializer: IArrayInitializer (1 elements) (OperationKind.ArrayInitializer) (Syntax: '{New M}')
      Element Values(1):
          IObjectCreationExpression (Constructor: Sub M..ctor()) (OperationKind.ObjectCreationExpression, Type: M) (Syntax: 'New M')
            Arguments(0)
            Initializer: null
]]>.Value
            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of CollectionInitializerSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact, WorkItem(17596, "https://github.com/dotnet/roslyn/issues/17596")>
        Public Sub ArrayCreationWithInitializer_ImplicitlyTypedWithoutInitializerAndDimension()
            Dim source = <![CDATA[
Class C
    Public Sub F()
        Dim a = {}'BIND:"Dim a = {}"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim a = {}')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'a')
    Variables: Local_1: a As System.Object()
    Initializer: IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: System.Object()) (Syntax: '{}')
        Dimension Sizes(1):
            ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '{}')
        Initializer: IArrayInitializer (0 elements) (OperationKind.ArrayInitializer) (Syntax: '{}')
            Element Values(0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact, WorkItem(17596, "https://github.com/dotnet/roslyn/issues/17596")>
        Public Sub ArrayCreationWithInitializer_MultipleInitializersWithConversions()
            Dim source = <![CDATA[
Class C
    Public Sub F()
        Dim a = ""
        Dim b = {"hello", a, Nothing}'BIND:"{"hello", a, Nothing}"
    End Sub
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: System.String()) (Syntax: '{"hello", a, Nothing}')
  Dimension Sizes(1):
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3) (Syntax: '{"hello", a, Nothing}')
  Initializer: IArrayInitializer (3 elements) (OperationKind.ArrayInitializer) (Syntax: '{"hello", a, Nothing}')
      Element Values(3):
          ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "hello") (Syntax: '"hello"')
          ILocalReferenceExpression: a (OperationKind.LocalReferenceExpression, Type: System.String) (Syntax: 'a')
          IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.String, Constant: null) (Syntax: 'Nothing')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'Nothing')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of CollectionInitializerSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact, WorkItem(17596, "https://github.com/dotnet/roslyn/issues/17596")>
        Public Sub MultiDimensionalArrayCreation()
            Dim source = <![CDATA[
Class C
    Public Sub F()
        Dim b As Byte(,,) = New Byte(0, 1, 2) {}'BIND:"New Byte(0, 1, 2) {}"
    End Sub
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: System.Byte(,,)) (Syntax: 'New Byte(0, 1, 2) {}')
  Dimension Sizes(3):
      IBinaryOperatorExpression (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32, Constant: 1) (Syntax: '0')
        Left: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
        Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '0')
      IBinaryOperatorExpression (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32, Constant: 2) (Syntax: '1')
        Left: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
      IBinaryOperatorExpression (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32, Constant: 3) (Syntax: '2')
        Left: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
        Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '2')
  Initializer: IArrayInitializer (0 elements) (OperationKind.ArrayInitializer) (Syntax: '{}')
      Element Values(0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ArrayCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact, WorkItem(17596, "https://github.com/dotnet/roslyn/issues/17596")>
        Public Sub MultiDimensionalArrayCreation_WithInitializer()
            Dim source = <![CDATA[
Class C
    Public Sub F()
        Dim b As Byte(,,) = New Byte(,,) {{{1, 2, 3}}, {{4, 5, 6}}}'BIND:"New Byte(,,) {{{1, 2, 3}}, {{4, 5, 6}}}"
    End Sub
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: System.Byte(,,)) (Syntax: 'New Byte(,, ... {4, 5, 6}}}')
  Dimension Sizes(3):
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: 'New Byte(,, ... {4, 5, 6}}}')
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'New Byte(,, ... {4, 5, 6}}}')
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3) (Syntax: 'New Byte(,, ... {4, 5, 6}}}')
  Initializer: IArrayInitializer (2 elements) (OperationKind.ArrayInitializer) (Syntax: '{{{1, 2, 3} ... {4, 5, 6}}}')
      Element Values(2):
          IArrayInitializer (1 elements) (OperationKind.ArrayInitializer) (Syntax: '{{1, 2, 3}}')
            Element Values(1):
                IArrayInitializer (3 elements) (OperationKind.ArrayInitializer) (Syntax: '{1, 2, 3}')
                  Element Values(3):
                      IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Byte, Constant: 1) (Syntax: '1')
                        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
                      IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Byte, Constant: 2) (Syntax: '2')
                        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
                      IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Byte, Constant: 3) (Syntax: '3')
                        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3) (Syntax: '3')
          IArrayInitializer (1 elements) (OperationKind.ArrayInitializer) (Syntax: '{{4, 5, 6}}')
            Element Values(1):
                IArrayInitializer (3 elements) (OperationKind.ArrayInitializer) (Syntax: '{4, 5, 6}')
                  Element Values(3):
                      IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Byte, Constant: 4) (Syntax: '4')
                        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 4) (Syntax: '4')
                      IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Byte, Constant: 5) (Syntax: '5')
                        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 5) (Syntax: '5')
                      IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Byte, Constant: 6) (Syntax: '6')
                        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 6) (Syntax: '6')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ArrayCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact, WorkItem(17596, "https://github.com/dotnet/roslyn/issues/17596")>
        Public Sub ArrayCreationOfSingleDimensionalArrays()
            Dim source = <![CDATA[
Class C
    Public Sub F()
        Dim a = {{1, 2, 3}, {4, 5, 6}}'BIND:"{{1, 2, 3}, {4, 5, 6}}"
    End Sub
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: System.Int32(,)) (Syntax: '{{1, 2, 3}, {4, 5, 6}}')
  Dimension Sizes(2):
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '{{1, 2, 3}, {4, 5, 6}}')
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3) (Syntax: '{{1, 2, 3}, {4, 5, 6}}')
  Initializer: IArrayInitializer (2 elements) (OperationKind.ArrayInitializer) (Syntax: '{{1, 2, 3}, {4, 5, 6}}')
      Element Values(2):
          IArrayInitializer (3 elements) (OperationKind.ArrayInitializer) (Syntax: '{1, 2, 3}')
            Element Values(3):
                ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
                ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
                ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3) (Syntax: '3')
          IArrayInitializer (3 elements) (OperationKind.ArrayInitializer) (Syntax: '{4, 5, 6}')
            Element Values(3):
                ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 4) (Syntax: '4')
                ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 5) (Syntax: '5')
                ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 6) (Syntax: '6')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of CollectionInitializerSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact, WorkItem(17596, "https://github.com/dotnet/roslyn/issues/17596")>
        Public Sub ArrayCreationOfMultiDimensionalArrays()
            Dim source = <![CDATA[
Class C
    Public Sub F()
        Dim a As Integer()(,) = New Integer(0)(,) {}'BIND:"New Integer(0)(,) {}"
    End Sub
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: System.Int32()(,)) (Syntax: 'New Integer(0)(,) {}')
  Dimension Sizes(1):
      IBinaryOperatorExpression (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32, Constant: 1) (Syntax: '0')
        Left: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
        Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '0')
  Initializer: IArrayInitializer (0 elements) (OperationKind.ArrayInitializer) (Syntax: '{}')
      Element Values(0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ArrayCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact, WorkItem(17596, "https://github.com/dotnet/roslyn/issues/17596")>
        Public Sub ArrayCreationOfMultiDimensionalArrays_MultipleExplicitNonConstantDimensions()
            Dim source = <![CDATA[
Class C
    Public Sub F(x As Integer())
        Dim y = New Integer(x(0), x(1)) {}'BIND:"New Integer(x(0), x(1)) {}"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: System.Int32(,)) (Syntax: 'New Integer ... ), x(1)) {}')
  Dimension Sizes(2):
      IBinaryOperatorExpression (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'x(0)')
        Left: IArrayElementReferenceExpression (OperationKind.ArrayElementReferenceExpression, Type: System.Int32) (Syntax: 'x(0)')
            Array reference: IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32()) (Syntax: 'x')
            Indices(1):
                ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
        Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'x(0)')
      IBinaryOperatorExpression (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'x(1)')
        Left: IArrayElementReferenceExpression (OperationKind.ArrayElementReferenceExpression, Type: System.Int32) (Syntax: 'x(1)')
            Array reference: IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32()) (Syntax: 'x')
            Indices(1):
                ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'x(1)')
  Initializer: IArrayInitializer (0 elements) (OperationKind.ArrayInitializer) (Syntax: '{}')
      Element Values(0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ArrayCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact, WorkItem(17596, "https://github.com/dotnet/roslyn/issues/17596")>
        Public Sub ArrayCreationOfMultiDimensionalArrays_MultipleExplicitConstantDimensions()
            Dim source = <![CDATA[
Class C
    Public Sub F()
        Dim y = New Integer(1, 1) {}'BIND:"New Integer(1, 1) {}"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: System.Int32(,)) (Syntax: 'New Integer(1, 1) {}')
  Dimension Sizes(2):
      IBinaryOperatorExpression (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32, Constant: 2) (Syntax: '1')
        Left: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
      IBinaryOperatorExpression (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32, Constant: 2) (Syntax: '1')
        Left: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
  Initializer: IArrayInitializer (0 elements) (OperationKind.ArrayInitializer) (Syntax: '{}')
      Element Values(0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ArrayCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact, WorkItem(17596, "https://github.com/dotnet/roslyn/issues/17596")>
        Public Sub ArrayCreationOfMultiDimensionalArraysErrorCase_InitializerMissingElements()
            Dim source = <![CDATA[
Class C
    Public Sub F()
        Dim y = New Integer(1, 1) {{}}'BIND:"New Integer(1, 1) {{}}"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: System.Int32(,), IsInvalid) (Syntax: 'New Integer(1, 1) {{}}')
  Dimension Sizes(2):
      IBinaryOperatorExpression (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32, Constant: 2) (Syntax: '1')
        Left: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
      IBinaryOperatorExpression (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32, Constant: 2) (Syntax: '1')
        Left: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
  Initializer: IArrayInitializer (1 elements) (OperationKind.ArrayInitializer, IsInvalid) (Syntax: '{{}}')
      Element Values(1):
          IArrayInitializer (0 elements) (OperationKind.ArrayInitializer, IsInvalid) (Syntax: '{}')
            Element Values(0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30567: Array initializer is missing 1 elements.
        Dim y = New Integer(1, 1) {{}}'BIND:"New Integer(1, 1) {{}}"
                                  ~~~~
BC30567: Array initializer is missing 2 elements.
        Dim y = New Integer(1, 1) {{}}'BIND:"New Integer(1, 1) {{}}"
                                   ~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ArrayCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact, WorkItem(17596, "https://github.com/dotnet/roslyn/issues/17596")>
        Public Sub ArrayCreationOfMultiDimensionalArraysErrorCase_InitializerMissingElements02()
            Dim source = <![CDATA[
Class C
    Public Sub F()
        Dim y = New Integer(1, 1) {{}, {}}'BIND:"New Integer(1, 1) {{}, {}}"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: System.Int32(,), IsInvalid) (Syntax: 'New Integer ... 1) {{}, {}}')
  Dimension Sizes(2):
      IBinaryOperatorExpression (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32, Constant: 2) (Syntax: '1')
        Left: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
      IBinaryOperatorExpression (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32, Constant: 2) (Syntax: '1')
        Left: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
  Initializer: IArrayInitializer (2 elements) (OperationKind.ArrayInitializer, IsInvalid) (Syntax: '{{}, {}}')
      Element Values(2):
          IArrayInitializer (0 elements) (OperationKind.ArrayInitializer, IsInvalid) (Syntax: '{}')
            Element Values(0)
          IArrayInitializer (0 elements) (OperationKind.ArrayInitializer, IsInvalid) (Syntax: '{}')
            Element Values(0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30567: Array initializer is missing 2 elements.
        Dim y = New Integer(1, 1) {{}, {}}'BIND:"New Integer(1, 1) {{}, {}}"
                                   ~~
BC30567: Array initializer is missing 2 elements.
        Dim y = New Integer(1, 1) {{}, {}}'BIND:"New Integer(1, 1) {{}, {}}"
                                       ~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ArrayCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact, WorkItem(17596, "https://github.com/dotnet/roslyn/issues/17596")>
        Public Sub ArrayCreationOfMultiDimensionalArraysErrorCase_InitializerMissingElements03()
            Dim source = <![CDATA[
Class C
    Public Sub F()
        Dim y = New Integer(1, 1) {{1, 2}}'BIND:"New Integer(1, 1) {{1, 2}}"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: System.Int32(,), IsInvalid) (Syntax: 'New Integer ... 1) {{1, 2}}')
  Dimension Sizes(2):
      IBinaryOperatorExpression (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32, Constant: 2) (Syntax: '1')
        Left: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
      IBinaryOperatorExpression (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32, Constant: 2) (Syntax: '1')
        Left: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
  Initializer: IArrayInitializer (1 elements) (OperationKind.ArrayInitializer, IsInvalid) (Syntax: '{{1, 2}}')
      Element Values(1):
          IArrayInitializer (2 elements) (OperationKind.ArrayInitializer, IsInvalid) (Syntax: '{1, 2}')
            Element Values(2):
                ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
                ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2, IsInvalid) (Syntax: '2')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30567: Array initializer is missing 1 elements.
        Dim y = New Integer(1, 1) {{1, 2}}'BIND:"New Integer(1, 1) {{1, 2}}"
                                  ~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ArrayCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact, WorkItem(17596, "https://github.com/dotnet/roslyn/issues/17596")>
        Public Sub ArrayCreationOfMultiDimensionalArraysErrorCase_InitializerMissingElements04()
            Dim source = <![CDATA[
Class C
    Public Sub F()
        Dim y = New Integer(1, 1) {{1, 2}, {}}'BIND:"New Integer(1, 1) {{1, 2}, {}}"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: System.Int32(,), IsInvalid) (Syntax: 'New Integer ... {1, 2}, {}}')
  Dimension Sizes(2):
      IBinaryOperatorExpression (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32, Constant: 2) (Syntax: '1')
        Left: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
      IBinaryOperatorExpression (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32, Constant: 2) (Syntax: '1')
        Left: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
  Initializer: IArrayInitializer (2 elements) (OperationKind.ArrayInitializer, IsInvalid) (Syntax: '{{1, 2}, {}}')
      Element Values(2):
          IArrayInitializer (2 elements) (OperationKind.ArrayInitializer) (Syntax: '{1, 2}')
            Element Values(2):
                ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
                ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
          IArrayInitializer (0 elements) (OperationKind.ArrayInitializer, IsInvalid) (Syntax: '{}')
            Element Values(0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30567: Array initializer is missing 2 elements.
        Dim y = New Integer(1, 1) {{1, 2}, {}}'BIND:"New Integer(1, 1) {{1, 2}, {}}"
                                           ~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ArrayCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact, WorkItem(17596, "https://github.com/dotnet/roslyn/issues/17596")>
        Public Sub ArrayCreationOfMultiDimensionalArrays_InitializerWithNestedArrayInitializers()
            Dim source = <![CDATA[
Class C
    Public Sub F()
        Dim y = New Integer(1, 1) {{1, 2}, {1, 2}}'BIND:"New Integer(1, 1) {{1, 2}, {1, 2}}"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: System.Int32(,)) (Syntax: 'New Integer ... 2}, {1, 2}}')
  Dimension Sizes(2):
      IBinaryOperatorExpression (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32, Constant: 2) (Syntax: '1')
        Left: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
      IBinaryOperatorExpression (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32, Constant: 2) (Syntax: '1')
        Left: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
  Initializer: IArrayInitializer (2 elements) (OperationKind.ArrayInitializer) (Syntax: '{{1, 2}, {1, 2}}')
      Element Values(2):
          IArrayInitializer (2 elements) (OperationKind.ArrayInitializer) (Syntax: '{1, 2}')
            Element Values(2):
                ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
                ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
          IArrayInitializer (2 elements) (OperationKind.ArrayInitializer) (Syntax: '{1, 2}')
            Element Values(2):
                ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
                ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ArrayCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact, WorkItem(17596, "https://github.com/dotnet/roslyn/issues/17596")>
        Public Sub ArrayCreationOfImplicitlyTypedMultiDimensionalArrays_WithInitializer()
            Dim source = <![CDATA[
Class C
    Public Sub F()
        Dim a = {{{{1, 2}}}, {{{3, 4}}}}'BIND:"{{{{1, 2}}}, {{{3, 4}}}}"
    End Sub
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: System.Int32(,,,)) (Syntax: '{{{{1, 2}}}, {{{3, 4}}}}')
  Dimension Sizes(4):
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '{{{{1, 2}}}, {{{3, 4}}}}')
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '{{{{1, 2}}}, {{{3, 4}}}}')
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '{{{{1, 2}}}, {{{3, 4}}}}')
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '{{{{1, 2}}}, {{{3, 4}}}}')
  Initializer: IArrayInitializer (2 elements) (OperationKind.ArrayInitializer) (Syntax: '{{{{1, 2}}}, {{{3, 4}}}}')
      Element Values(2):
          IArrayInitializer (1 elements) (OperationKind.ArrayInitializer) (Syntax: '{{{1, 2}}}')
            Element Values(1):
                IArrayInitializer (1 elements) (OperationKind.ArrayInitializer) (Syntax: '{{1, 2}}')
                  Element Values(1):
                      IArrayInitializer (2 elements) (OperationKind.ArrayInitializer) (Syntax: '{1, 2}')
                        Element Values(2):
                            ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
                            ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
          IArrayInitializer (1 elements) (OperationKind.ArrayInitializer) (Syntax: '{{{3, 4}}}')
            Element Values(1):
                IArrayInitializer (1 elements) (OperationKind.ArrayInitializer) (Syntax: '{{3, 4}}')
                  Element Values(1):
                      IArrayInitializer (2 elements) (OperationKind.ArrayInitializer) (Syntax: '{3, 4}')
                        Element Values(2):
                            ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3) (Syntax: '3')
                            ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 4) (Syntax: '4')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of CollectionInitializerSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact, WorkItem(17596, "https://github.com/dotnet/roslyn/issues/17596")>
        Public Sub ArrayCreationErrorCase_MissingDimension()
            Dim source = <![CDATA[
Class C
    Public Sub F()
        Dim a = New String(1,) {}'BIND:"New String(1,) {}"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: System.String(,), IsInvalid) (Syntax: 'New String(1,) {}')
  Dimension Sizes(2):
      IBinaryOperatorExpression (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32, Constant: 2) (Syntax: '1')
        Left: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
      IBinaryOperatorExpression (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32, IsInvalid) (Syntax: '')
        Left: IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '')
            Children(0)
        Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '')
  Initializer: IArrayInitializer (0 elements) (OperationKind.ArrayInitializer) (Syntax: '{}')
      Element Values(0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30306: Array subscript expression missing.
        Dim a = New String(1,) {}'BIND:"New String(1,) {}"
                             ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ArrayCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact, WorkItem(17596, "https://github.com/dotnet/roslyn/issues/17596")>
        Public Sub ArrayCreationErrorCase_InvalidInitializer()
            Dim source = <![CDATA[
Class C
    Public Sub F()
        Dim a = New C() {1}'BIND:"New C() {1}"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: C(), IsInvalid) (Syntax: 'New C() {1}')
  Dimension Sizes(1):
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: 'New C() {1}')
  Initializer: IArrayInitializer (1 elements) (OperationKind.ArrayInitializer, IsInvalid) (Syntax: '{1}')
      Element Values(1):
          IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: C, IsInvalid) (Syntax: '1')
            Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30311: Value of type 'Integer' cannot be converted to 'C'.
        Dim a = New C() {1}'BIND:"New C() {1}"
                         ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ArrayCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact, WorkItem(17596, "https://github.com/dotnet/roslyn/issues/17596")>
        Public Sub ArrayCreationErrorCase_MissingExplicitCast()
            Dim source = <![CDATA[
Class C
    Public Sub F(c As C)
        Dim a = New C(c) {}'BIND:"New C(c) {}"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: C(), IsInvalid) (Syntax: 'New C(c) {}')
  Dimension Sizes(1):
      IBinaryOperatorExpression (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32, IsInvalid) (Syntax: 'c')
        Left: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid) (Syntax: 'c')
            Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: IParameterReferenceExpression: c (OperationKind.ParameterReferenceExpression, Type: C, IsInvalid) (Syntax: 'c')
        Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: 'c')
  Initializer: IArrayInitializer (0 elements) (OperationKind.ArrayInitializer) (Syntax: '{}')
      Element Values(0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30311: Value of type 'C' cannot be converted to 'Integer'.
        Dim a = New C(c) {}'BIND:"New C(c) {}"
                      ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ArrayCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact, WorkItem(17596, "https://github.com/dotnet/roslyn/issues/17596")>
        Public Sub ArrayCreation_InvocationExpressionAsDimension()
            Dim source = <![CDATA[
Class C
    Public Sub F(c As C)
        Dim a = New C(M()) {}'BIND:"New C(M()) {}"
    End Sub

    Public Function M() As Integer
        Return 1
    End Function
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: C()) (Syntax: 'New C(M()) {}')
  Dimension Sizes(1):
      IBinaryOperatorExpression (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'M()')
        Left: IInvocationExpression ( Function C.M() As System.Int32) (OperationKind.InvocationExpression, Type: System.Int32) (Syntax: 'M()')
            Instance Receiver: IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: C) (Syntax: 'M')
            Arguments(0)
        Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'M()')
  Initializer: IArrayInitializer (0 elements) (OperationKind.ArrayInitializer) (Syntax: '{}')
      Element Values(0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ArrayCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact, WorkItem(17596, "https://github.com/dotnet/roslyn/issues/17596")>
        Public Sub ArrayCreation_InvocationExpressionWithConversionAsDimension()
            Dim source = <![CDATA[
Option Strict On
Class C
    Public Sub F(c As C)
        Dim a = New C(DirectCast(M(), Integer)) {}'BIND:"New C(DirectCast(M(), Integer)) {}"
    End Sub

    Public Function M() As Object
        Return 1
    End Function
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: C()) (Syntax: 'New C(Direc ... nteger)) {}')
  Dimension Sizes(1):
      IBinaryOperatorExpression (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'DirectCast(M(), Integer)')
        Left: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32) (Syntax: 'DirectCast(M(), Integer)')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: IInvocationExpression ( Function C.M() As System.Object) (OperationKind.InvocationExpression, Type: System.Object) (Syntax: 'M()')
                Instance Receiver: IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: C) (Syntax: 'M')
                Arguments(0)
        Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'DirectCast(M(), Integer)')
  Initializer: IArrayInitializer (0 elements) (OperationKind.ArrayInitializer) (Syntax: '{}')
      Element Values(0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ArrayCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact, WorkItem(17596, "https://github.com/dotnet/roslyn/issues/17596")>
        Public Sub ArrayCreationErrorCase_InvocationExpressionAsDimension()
            Dim source = <![CDATA[
Option Strict On
Class C
    Public Sub F(c As C)
        Dim a = New C(M()) {}'BIND:"New C(M()) {}"
    End Sub

    Public Function M() As Object
        Return 1
    End Function
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: C(), IsInvalid) (Syntax: 'New C(M()) {}')
  Dimension Sizes(1):
      IBinaryOperatorExpression (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32, IsInvalid) (Syntax: 'M()')
        Left: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid) (Syntax: 'M()')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: IInvocationExpression ( Function C.M() As System.Object) (OperationKind.InvocationExpression, Type: System.Object, IsInvalid) (Syntax: 'M()')
                Instance Receiver: IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: C, IsInvalid) (Syntax: 'M')
                Arguments(0)
        Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: 'M()')
  Initializer: IArrayInitializer (0 elements) (OperationKind.ArrayInitializer) (Syntax: '{}')
      Element Values(0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30512: Option Strict On disallows implicit conversions from 'Object' to 'Integer'.
        Dim a = New C(M()) {}'BIND:"New C(M()) {}"
                      ~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ArrayCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact, WorkItem(17596, "https://github.com/dotnet/roslyn/issues/17596")>
        Public Sub ArrayCreationErrorCase_InvocationExpressionWithConversionAsDimension()
            Dim source = <![CDATA[
Option Strict On
Class C
    Public Sub F(c As C)
        Dim a = New C(DirectCast(M(), Integer)) {}'BIND:"New C(DirectCast(M(), Integer)) {}"
    End Sub

    Public Function M() As C
        Return New C
    End Function
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: C(), IsInvalid) (Syntax: 'New C(Direc ... nteger)) {}')
  Dimension Sizes(1):
      IBinaryOperatorExpression (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32, IsInvalid) (Syntax: 'DirectCast(M(), Integer)')
        Left: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid) (Syntax: 'DirectCast(M(), Integer)')
            Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: IInvocationExpression ( Function C.M() As C) (OperationKind.InvocationExpression, Type: C, IsInvalid) (Syntax: 'M()')
                Instance Receiver: IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: C, IsInvalid) (Syntax: 'M')
                Arguments(0)
        Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: 'DirectCast(M(), Integer)')
  Initializer: IArrayInitializer (0 elements) (OperationKind.ArrayInitializer) (Syntax: '{}')
      Element Values(0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30311: Value of type 'C' cannot be converted to 'Integer'.
        Dim a = New C(DirectCast(M(), Integer)) {}'BIND:"New C(DirectCast(M(), Integer)) {}"
                                 ~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ArrayCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact, WorkItem(17596, "https://github.com/dotnet/roslyn/issues/17596")>
        Public Sub ArrayCreation_DeclarationWithExplicitDimension()
            Dim source = <![CDATA[
Class C
    Public Sub F()
        Dim x(2) As Integer'BIND:"Dim x(2) As Integer"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim x(2) As Integer')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'x(2)')
    Variables: Local_1: x As System.Int32()
    Initializer: IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: System.Int32()) (Syntax: 'x(2)')
        Dimension Sizes(1):
            IBinaryOperatorExpression (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32, Constant: 3) (Syntax: '2')
              Left: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
              Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '2')
        Initializer: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub
    End Class
End Namespace
