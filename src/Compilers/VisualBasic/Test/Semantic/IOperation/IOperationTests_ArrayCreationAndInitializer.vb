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
IArrayCreationExpression (Dimension sizes: 1, Element Type: M) (OperationKind.ArrayCreationExpression, Type: M())
  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
  IArrayInitializer (OperationKind.ArrayInitializer)
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
IArrayCreationExpression (Dimension sizes: 1, Element Type: M) (OperationKind.ArrayCreationExpression, Type: M())
  IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32, Constant: 2)
    Left: ILocalReferenceExpression: dimension (OperationKind.LocalReferenceExpression, Type: System.Int32, Constant: 1)
    Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  IArrayInitializer (OperationKind.ArrayInitializer)
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
IArrayCreationExpression (Dimension sizes: 1, Element Type: M) (OperationKind.ArrayCreationExpression, Type: M())
  IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32)
    Left: IParameterReferenceExpression: dimension (OperationKind.ParameterReferenceExpression, Type: System.Int32)
    Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  IArrayInitializer (OperationKind.ArrayInitializer)
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
IArrayCreationExpression (Dimension sizes: 1, Element Type: M) (OperationKind.ArrayCreationExpression, Type: M())
  IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32)
    Left: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Int32)
        IParameterReferenceExpression: dimension (OperationKind.ParameterReferenceExpression, Type: System.UInt16)
    Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  IArrayInitializer (OperationKind.ArrayInitializer)
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
IArrayCreationExpression (Dimension sizes: 1, Element Type: M) (OperationKind.ArrayCreationExpression, Type: M())
  IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32)
    Left: IConversionExpression (ConversionKind.Cast, Explicit) (OperationKind.ConversionExpression, Type: System.Int32)
        IParameterReferenceExpression: dimension (OperationKind.ParameterReferenceExpression, Type: System.Object)
    Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  IArrayInitializer (OperationKind.ArrayInitializer)
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
IArrayCreationExpression (Dimension sizes: 1, Element Type: System.String) (OperationKind.ArrayCreationExpression, Type: System.String())
  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  IArrayInitializer (OperationKind.ArrayInitializer)
    IFieldReferenceExpression: System.String.Empty As System.String (Static) (OperationKind.FieldReferenceExpression, Type: System.String)
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
IArrayCreationExpression (Dimension sizes: 1, Element Type: M) (OperationKind.ArrayCreationExpression, Type: M())
  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  IArrayInitializer (OperationKind.ArrayInitializer)
    IObjectCreationExpression (Constructor: Sub M..ctor()) (OperationKind.ObjectCreationExpression, Type: M)
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
IArrayCreationExpression (Dimension sizes: 1, Element Type: M) (OperationKind.ArrayCreationExpression, Type: M())
  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  IArrayInitializer (OperationKind.ArrayInitializer)
    IObjectCreationExpression (Constructor: Sub M..ctor()) (OperationKind.ObjectCreationExpression, Type: M)
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
IArrayCreationExpression (Dimension sizes: 1, Element Type: System.String) (OperationKind.ArrayCreationExpression, Type: System.String())
  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3)
  IArrayInitializer (OperationKind.ArrayInitializer)
    ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: hello)
    ILocalReferenceExpression: a (OperationKind.LocalReferenceExpression, Type: System.String)
    IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.String, Constant: null)
      ILiteralExpression (OperationKind.LiteralExpression, Type: null, Constant: null)
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
IArrayCreationExpression (Dimension sizes: 3, Element Type: System.Byte) (OperationKind.ArrayCreationExpression, Type: System.Byte(,,))
  IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32, Constant: 1)
    Left: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
    Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32, Constant: 2)
    Left: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
    Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32, Constant: 3)
    Left: ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2)
    Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  IArrayInitializer (OperationKind.ArrayInitializer)
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
IArrayCreationExpression (Dimension sizes: 3, Element Type: System.Byte) (OperationKind.ArrayCreationExpression, Type: System.Byte(,,))
  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2)
  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3)
  IArrayInitializer (OperationKind.ArrayInitializer)
    IArrayInitializer (OperationKind.ArrayInitializer)
      IArrayInitializer (OperationKind.ArrayInitializer)
        IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Byte, Constant: 1)
          ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
        IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Byte, Constant: 2)
          ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2)
        IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Byte, Constant: 3)
          ILiteralExpression (Text: 3) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3)
    IArrayInitializer (OperationKind.ArrayInitializer)
      IArrayInitializer (OperationKind.ArrayInitializer)
        IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Byte, Constant: 4)
          ILiteralExpression (Text: 4) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 4)
        IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Byte, Constant: 5)
          ILiteralExpression (Text: 5) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 5)
        IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Byte, Constant: 6)
          ILiteralExpression (Text: 6) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 6)
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
IArrayCreationExpression (Dimension sizes: 2, Element Type: System.Int32) (OperationKind.ArrayCreationExpression, Type: System.Int32(,))
  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2)
  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3)
  IArrayInitializer (OperationKind.ArrayInitializer)
    IArrayInitializer (OperationKind.ArrayInitializer)
      ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
      ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2)
      ILiteralExpression (Text: 3) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3)
    IArrayInitializer (OperationKind.ArrayInitializer)
      ILiteralExpression (Text: 4) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 4)
      ILiteralExpression (Text: 5) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 5)
      ILiteralExpression (Text: 6) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 6)
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
IArrayCreationExpression (Dimension sizes: 1, Element Type: System.Int32(,)) (OperationKind.ArrayCreationExpression, Type: System.Int32()(,))
  IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32, Constant: 1)
    Left: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
    Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  IArrayInitializer (OperationKind.ArrayInitializer)
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
IArrayCreationExpression (Dimension sizes: 4, Element Type: System.Int32) (OperationKind.ArrayCreationExpression, Type: System.Int32(,,,))
  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2)
  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2)
  IArrayInitializer (OperationKind.ArrayInitializer)
    IArrayInitializer (OperationKind.ArrayInitializer)
      IArrayInitializer (OperationKind.ArrayInitializer)
        IArrayInitializer (OperationKind.ArrayInitializer)
          ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
          ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2)
    IArrayInitializer (OperationKind.ArrayInitializer)
      IArrayInitializer (OperationKind.ArrayInitializer)
        IArrayInitializer (OperationKind.ArrayInitializer)
          ILiteralExpression (Text: 3) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3)
          ILiteralExpression (Text: 4) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 4)
]]>.Value

            VerifyOperationTreeForTest(Of CollectionInitializerSyntax)(source, expectedOperationTree)
        End Sub
    End Class
End Namespace
