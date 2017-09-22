' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Semantics
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase

        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/18150"), WorkItem(17596, "https://github.com/dotnet/roslyn/issues/17596")>
        Public Sub SimpleArrayCreation_PrimitiveType()
            Dim source = <![CDATA[
Class C
    Public Sub F()
        Dim a = New String(0) {}'BIND:"New String(0) {}"
    End Sub
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IArrayCreationExpression (Dimension sizes: 1, Element Type: System.String) (OperationKind.ArrayCreationExpression, Type: System.String())
  ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
  IArrayInitializer (OperationKind.ArrayInitializer)
]]>.Value

            VerifyOperationTreeForTest(Of ArrayCreationExpressionSyntax)(source, expectedOperationTree)
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
IArrayCreationExpression (Element Type: M) (OperationKind.ArrayCreationExpression, Type: M()) (Syntax: 'New M() {}')
  Dimension Sizes(1):
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: 'New M() {}')
  Initializer: IArrayInitializer (0 elements) (OperationKind.ArrayInitializer) (Syntax: '{}')
      Element Values(0)
]]>.Value

            VerifyOperationTreeForTest(Of ArrayCreationExpressionSyntax)(source, expectedOperationTree)
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
IArrayCreationExpression (Element Type: M) (OperationKind.ArrayCreationExpression, Type: M()) (Syntax: 'New M(dimension) {}')
  Dimension Sizes(1):
      IBinaryOperatorExpression (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32, Constant: 2) (Syntax: 'dimension')
        Left: ILocalReferenceExpression: dimension (OperationKind.LocalReferenceExpression, Type: System.Int32, Constant: 1) (Syntax: 'dimension')
        Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'dimension')
  Initializer: IArrayInitializer (0 elements) (OperationKind.ArrayInitializer) (Syntax: '{}')
      Element Values(0)
]]>.Value

            VerifyOperationTreeForTest(Of ArrayCreationExpressionSyntax)(source, expectedOperationTree)
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
IArrayCreationExpression (Element Type: M) (OperationKind.ArrayCreationExpression, Type: M()) (Syntax: 'New M(dimension) {}')
  Dimension Sizes(1):
      IBinaryOperatorExpression (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'dimension')
        Left: IParameterReferenceExpression: dimension (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'dimension')
        Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'dimension')
  Initializer: IArrayInitializer (0 elements) (OperationKind.ArrayInitializer) (Syntax: '{}')
      Element Values(0)
]]>.Value

            VerifyOperationTreeForTest(Of ArrayCreationExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <Fact, WorkItem(17596, "https://github.com/dotnet/roslyn/issues/17596")>
        Public Sub SimpleArrayCreation_DimensionWithImplicitConversion()
            Dim source = <![CDATA[
Class M
End Class

Class C
    Public Sub F(dimension As UInt16)
        Dim a = New M(dimension) {}'BIND:"New M(dimension) {}"
    End Sub
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IArrayCreationExpression (Element Type: M) (OperationKind.ArrayCreationExpression, Type: M()) (Syntax: 'New M(dimension) {}')
  Dimension Sizes(1):
      IBinaryOperatorExpression (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'dimension')
        Left: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32) (Syntax: 'dimension')
            Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: IParameterReferenceExpression: dimension (OperationKind.ParameterReferenceExpression, Type: UInt16) (Syntax: 'dimension')
        Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'dimension')
  Initializer: IArrayInitializer (0 elements) (OperationKind.ArrayInitializer) (Syntax: '{}')
      Element Values(0)
]]>.Value

            VerifyOperationTreeForTest(Of ArrayCreationExpressionSyntax)(source, expectedOperationTree)
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
IArrayCreationExpression (Element Type: M) (OperationKind.ArrayCreationExpression, Type: M()) (Syntax: 'New M(Direc ... nteger)) {}')
  Dimension Sizes(1):
      IBinaryOperatorExpression (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'DirectCast( ... n, Integer)')
        Left: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32) (Syntax: 'DirectCast( ... n, Integer)')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: IParameterReferenceExpression: dimension (OperationKind.ParameterReferenceExpression, Type: System.Object) (Syntax: 'dimension')
        Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'DirectCast( ... n, Integer)')
  Initializer: IArrayInitializer (0 elements) (OperationKind.ArrayInitializer) (Syntax: '{}')
      Element Values(0)
]]>.Value

            VerifyOperationTreeForTest(Of ArrayCreationExpressionSyntax)(source, expectedOperationTree)
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
IArrayCreationExpression (Element Type: System.String) (OperationKind.ArrayCreationExpression, Type: System.String()) (Syntax: 'New String( ... ring.Empty}')
  Dimension Sizes(1):
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'New String( ... ring.Empty}')
  Initializer: IArrayInitializer (1 elements) (OperationKind.ArrayInitializer) (Syntax: '{String.Empty}')
      Element Values(1):
          IFieldReferenceExpression: System.String.Empty As System.String (Static) (OperationKind.FieldReferenceExpression, Type: System.String) (Syntax: 'String.Empty')
            Instance Receiver: null
]]>.Value

            VerifyOperationTreeForTest(Of ArrayCreationExpressionSyntax)(source, expectedOperationTree)
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
IArrayCreationExpression (Element Type: M) (OperationKind.ArrayCreationExpression, Type: M()) (Syntax: 'New M() {New M}')
  Dimension Sizes(1):
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'New M() {New M}')
  Initializer: IArrayInitializer (1 elements) (OperationKind.ArrayInitializer) (Syntax: '{New M}')
      Element Values(1):
          IObjectCreationExpression (Constructor: Sub M..ctor()) (OperationKind.ObjectCreationExpression, Type: M) (Syntax: 'New M')
            Arguments(0)
            Initializer: null
]]>.Value

            VerifyOperationTreeForTest(Of ArrayCreationExpressionSyntax)(source, expectedOperationTree)
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
IArrayCreationExpression (Element Type: M) (OperationKind.ArrayCreationExpression, Type: M()) (Syntax: '{New M}')
  Dimension Sizes(1):
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '{New M}')
  Initializer: IArrayInitializer (1 elements) (OperationKind.ArrayInitializer) (Syntax: '{New M}')
      Element Values(1):
          IObjectCreationExpression (Constructor: Sub M..ctor()) (OperationKind.ObjectCreationExpression, Type: M) (Syntax: 'New M')
            Arguments(0)
            Initializer: null
]]>.Value

            VerifyOperationTreeForTest(Of CollectionInitializerSyntax)(source, expectedOperationTree)
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
IArrayCreationExpression (Element Type: System.String) (OperationKind.ArrayCreationExpression, Type: System.String()) (Syntax: '{"hello", a, Nothing}')
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

            VerifyOperationTreeForTest(Of CollectionInitializerSyntax)(source, expectedOperationTree)
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
IArrayCreationExpression (Element Type: System.Byte) (OperationKind.ArrayCreationExpression, Type: System.Byte(,,)) (Syntax: 'New Byte(0, 1, 2) {}')
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

            VerifyOperationTreeForTest(Of ArrayCreationExpressionSyntax)(source, expectedOperationTree)
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
IArrayCreationExpression (Element Type: System.Byte) (OperationKind.ArrayCreationExpression, Type: System.Byte(,,)) (Syntax: 'New Byte(,, ... {4, 5, 6}}}')
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

            VerifyOperationTreeForTest(Of ArrayCreationExpressionSyntax)(source, expectedOperationTree)
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
IArrayCreationExpression (Element Type: System.Int32) (OperationKind.ArrayCreationExpression, Type: System.Int32(,)) (Syntax: '{{1, 2, 3}, {4, 5, 6}}')
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

            VerifyOperationTreeForTest(Of CollectionInitializerSyntax)(source, expectedOperationTree)
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
IArrayCreationExpression (Element Type: System.Int32(,)) (OperationKind.ArrayCreationExpression, Type: System.Int32()(,)) (Syntax: 'New Integer(0)(,) {}')
  Dimension Sizes(1):
      IBinaryOperatorExpression (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32, Constant: 1) (Syntax: '0')
        Left: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
        Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '0')
  Initializer: IArrayInitializer (0 elements) (OperationKind.ArrayInitializer) (Syntax: '{}')
      Element Values(0)
]]>.Value

            VerifyOperationTreeForTest(Of ArrayCreationExpressionSyntax)(source, expectedOperationTree)
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
IArrayCreationExpression (Element Type: System.Int32) (OperationKind.ArrayCreationExpression, Type: System.Int32(,,,)) (Syntax: '{{{{1, 2}}}, {{{3, 4}}}}')
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

            VerifyOperationTreeForTest(Of CollectionInitializerSyntax)(source, expectedOperationTree)
        End Sub

        <Fact, WorkItem(17596, "https://github.com/dotnet/roslyn/issues/17596")>
        Public Sub ArrayCreationErrorCase_MissingDimension()
            Dim source = <![CDATA[
Class C
    Public Sub F()
        Dim a = New String(1,) {}'BIND:"New String(1,) {}"
    End Sub
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IArrayCreationExpression (Element Type: System.String) (OperationKind.ArrayCreationExpression, Type: System.String(,), IsInvalid) (Syntax: 'New String(1,) {}')
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

            VerifyOperationTreeForTest(Of ArrayCreationExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <Fact, WorkItem(17596, "https://github.com/dotnet/roslyn/issues/17596")>
        Public Sub ArrayCreationErrorCase_InvalidInitializer()
            Dim source = <![CDATA[
Class C
    Public Sub F()
        Dim a = New C() {1}'BIND:"New C() {1}"
    End Sub
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IArrayCreationExpression (Element Type: C) (OperationKind.ArrayCreationExpression, Type: C(), IsInvalid) (Syntax: 'New C() {1}')
  Dimension Sizes(1):
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: 'New C() {1}')
  Initializer: IArrayInitializer (1 elements) (OperationKind.ArrayInitializer, IsInvalid) (Syntax: '{1}')
      Element Values(1):
          IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: C, IsInvalid) (Syntax: '1')
            Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
]]>.Value

            VerifyOperationTreeForTest(Of ArrayCreationExpressionSyntax)(source, expectedOperationTree)
        End Sub
    End Class
End Namespace
