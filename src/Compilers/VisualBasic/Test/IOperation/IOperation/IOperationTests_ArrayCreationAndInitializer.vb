' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Test.Utilities
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
IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.String()) (Syntax: 'New String(0) {}')
  Dimension Sizes(1):
      IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '0')
        Left: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
        Right: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '0')
  Initializer: 
    IArrayInitializerOperation (0 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{}')
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
IArrayCreationOperation (OperationKind.ArrayCreation, Type: M()) (Syntax: 'New M() {}')
  Dimension Sizes(1):
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsImplicit) (Syntax: 'New M() {}')
  Initializer: 
    IArrayInitializerOperation (0 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{}')
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
IArrayCreationOperation (OperationKind.ArrayCreation, Type: M()) (Syntax: 'New M(dimension) {}')
  Dimension Sizes(1):
      IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: 'dimension')
        Left: 
          ILocalReferenceOperation: dimension (OperationKind.LocalReference, Type: System.Int32, Constant: 1) (Syntax: 'dimension')
        Right: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'dimension')
  Initializer: 
    IArrayInitializerOperation (0 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{}')
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
IArrayCreationOperation (OperationKind.ArrayCreation, Type: M()) (Syntax: 'New M(dimension) {}')
  Dimension Sizes(1):
      IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, IsImplicit) (Syntax: 'dimension')
        Left: 
          IParameterReferenceOperation: dimension (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'dimension')
        Right: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'dimension')
  Initializer: 
    IArrayInitializerOperation (0 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{}')
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
IArrayCreationOperation (OperationKind.ArrayCreation, Type: M()) (Syntax: 'New M(dimension) {}')
  Dimension Sizes(1):
      IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, IsImplicit) (Syntax: 'dimension')
        Left: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsImplicit) (Syntax: 'dimension')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IParameterReferenceOperation: dimension (OperationKind.ParameterReference, Type: System.UInt16) (Syntax: 'dimension')
        Right: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'dimension')
  Initializer: 
    IArrayInitializerOperation (0 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{}')
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
IArrayCreationOperation (OperationKind.ArrayCreation, Type: M()) (Syntax: 'New M(Direc ... nteger)) {}')
  Dimension Sizes(1):
      IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, IsImplicit) (Syntax: 'DirectCast( ... n, Integer)')
        Left: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32) (Syntax: 'DirectCast( ... n, Integer)')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IParameterReferenceOperation: dimension (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'dimension')
        Right: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'DirectCast( ... n, Integer)')
  Initializer: 
    IArrayInitializerOperation (0 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{}')
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
IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.String()) (Syntax: 'New String( ... ring.Empty}')
  Dimension Sizes(1):
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'New String( ... ring.Empty}')
  Initializer: 
    IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{String.Empty}')
      Element Values(1):
          IFieldReferenceOperation: System.String.Empty As System.String (Static) (OperationKind.FieldReference, Type: System.String) (Syntax: 'String.Empty')
            Instance Receiver: 
              null
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
IArrayCreationOperation (OperationKind.ArrayCreation, Type: C()) (Syntax: 'New C(1) {N ... C, Nothing}')
  Dimension Sizes(1):
      IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: '1')
        Left: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        Right: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '1')
  Initializer: 
    IArrayInitializerOperation (2 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{New C, Nothing}')
      Element Values(2):
          IObjectCreationOperation (Constructor: Sub C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'New C')
            Arguments(0)
            Initializer: 
              null
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: C, Constant: null, IsImplicit) (Syntax: 'Nothing')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'Nothing')
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
IArrayCreationOperation (OperationKind.ArrayCreation, Type: C(), IsInvalid) (Syntax: 'New C(2) {New C}')
  Dimension Sizes(1):
      IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, Constant: 3, IsImplicit) (Syntax: '2')
        Left: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
        Right: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '2')
  Initializer: 
    IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null, IsInvalid) (Syntax: '{New C}')
      Element Values(1):
          IObjectCreationOperation (Constructor: Sub C..ctor()) (OperationKind.ObjectCreation, Type: C, IsInvalid) (Syntax: 'New C')
            Arguments(0)
            Initializer: 
              null
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
IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32(), IsInvalid) (Syntax: 'New Integer(x(0)) {1, 2}')
  Dimension Sizes(1):
      IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, IsImplicit) (Syntax: 'x(0)')
        Left: 
          IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: 'x(0)')
            Array reference: 
              ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32()) (Syntax: 'x')
            Indices(1):
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
        Right: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'x(0)')
  Initializer: 
    IArrayInitializerOperation (2 elements) (OperationKind.ArrayInitializer, Type: null, IsInvalid) (Syntax: '{1, 2}')
      Element Values(2):
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2, IsInvalid) (Syntax: '2')
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
IArrayCreationOperation (OperationKind.ArrayCreation, Type: M()) (Syntax: 'New M() {New M}')
  Dimension Sizes(1):
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'New M() {New M}')
  Initializer: 
    IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{New M}')
      Element Values(1):
          IObjectCreationOperation (Constructor: Sub M..ctor()) (OperationKind.ObjectCreation, Type: M) (Syntax: 'New M')
            Arguments(0)
            Initializer: 
              null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ArrayCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact, WorkItem(17596, "https://github.com/dotnet/roslyn/issues/17596")>
        Public Sub ArrayCreationWithInitializer_ImplicitlyTyped_01()
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
IArrayCreationOperation (OperationKind.ArrayCreation, Type: M()) (Syntax: '{New M}')
  Dimension Sizes(1):
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '{New M}')
  Initializer: 
    IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null, IsImplicit) (Syntax: '{New M}')
      Element Values(1):
          IObjectCreationOperation (Constructor: Sub M..ctor()) (OperationKind.ObjectCreation, Type: M) (Syntax: 'New M')
            Arguments(0)
            Initializer: 
              null
]]>.Value
            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of CollectionInitializerSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact, WorkItem(17596, "https://github.com/dotnet/roslyn/issues/17596")>
        Public Sub ArrayCreationWithInitializer_ImplicitlyTyped_02()
            Dim source = <![CDATA[
Class C
    Public Sub F(dimension As Object)
        Dim a = {M}'BIND:"{M}"
    End Sub
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: '{M}')
  Children(2):
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid, IsImplicit) (Syntax: '{M}')
      IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null, IsInvalid, IsImplicit) (Syntax: '{M}')
        Element Values(1):
            IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'M')
              Children(0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30451: 'M' is not declared. It may be inaccessible due to its protection level.
        Dim a = {M}'BIND:"{M}"
                 ~
]]>.Value

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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim a = {}')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'a = {}')
    Declarators:
        IVariableDeclaratorOperation (Symbol: a As System.Object()) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'a')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= {}')
        IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Object()) (Syntax: '{}')
          Dimension Sizes(1):
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsImplicit) (Syntax: '{}')
          Initializer: 
            IArrayInitializerOperation (0 elements) (OperationKind.ArrayInitializer, Type: null, IsImplicit) (Syntax: '{}')
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
IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.String()) (Syntax: '{"hello", a, Nothing}')
  Dimension Sizes(1):
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3, IsImplicit) (Syntax: '{"hello", a, Nothing}')
  Initializer: 
    IArrayInitializerOperation (3 elements) (OperationKind.ArrayInitializer, Type: null, IsImplicit) (Syntax: '{"hello", a, Nothing}')
      Element Values(3):
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "hello") (Syntax: '"hello"')
          ILocalReferenceOperation: a (OperationKind.LocalReference, Type: System.String) (Syntax: 'a')
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String, Constant: null, IsImplicit) (Syntax: 'Nothing')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'Nothing')
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
IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Byte(,,)) (Syntax: 'New Byte(0, 1, 2) {}')
  Dimension Sizes(3):
      IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '0')
        Left: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
        Right: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '0')
      IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: '1')
        Left: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        Right: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '1')
      IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, Constant: 3, IsImplicit) (Syntax: '2')
        Left: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
        Right: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '2')
  Initializer: 
    IArrayInitializerOperation (0 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{}')
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
IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Byte(,,)) (Syntax: 'New Byte(,, ... {4, 5, 6}}}')
  Dimension Sizes(3):
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: 'New Byte(,, ... {4, 5, 6}}}')
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'New Byte(,, ... {4, 5, 6}}}')
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3, IsImplicit) (Syntax: 'New Byte(,, ... {4, 5, 6}}}')
  Initializer: 
    IArrayInitializerOperation (2 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{{{1, 2, 3} ... {4, 5, 6}}}')
      Element Values(2):
          IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{{1, 2, 3}}')
            Element Values(1):
                IArrayInitializerOperation (3 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{1, 2, 3}')
                  Element Values(3):
                      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Byte, Constant: 1, IsImplicit) (Syntax: '1')
                        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        Operand: 
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Byte, Constant: 2, IsImplicit) (Syntax: '2')
                        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        Operand: 
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Byte, Constant: 3, IsImplicit) (Syntax: '3')
                        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        Operand: 
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
          IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{{4, 5, 6}}')
            Element Values(1):
                IArrayInitializerOperation (3 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{4, 5, 6}')
                  Element Values(3):
                      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Byte, Constant: 4, IsImplicit) (Syntax: '4')
                        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        Operand: 
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 4) (Syntax: '4')
                      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Byte, Constant: 5, IsImplicit) (Syntax: '5')
                        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        Operand: 
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')
                      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Byte, Constant: 6, IsImplicit) (Syntax: '6')
                        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        Operand: 
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 6) (Syntax: '6')
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
IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32(,)) (Syntax: '{{1, 2, 3}, {4, 5, 6}}')
  Dimension Sizes(2):
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: '{{1, 2, 3}, {4, 5, 6}}')
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3, IsImplicit) (Syntax: '{{1, 2, 3}, {4, 5, 6}}')
  Initializer: 
    IArrayInitializerOperation (2 elements) (OperationKind.ArrayInitializer, Type: null, IsImplicit) (Syntax: '{{1, 2, 3}, {4, 5, 6}}')
      Element Values(2):
          IArrayInitializerOperation (3 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{1, 2, 3}')
            Element Values(3):
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
          IArrayInitializerOperation (3 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{4, 5, 6}')
            Element Values(3):
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 4) (Syntax: '4')
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 6) (Syntax: '6')
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
IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32()(,)) (Syntax: 'New Integer(0)(,) {}')
  Dimension Sizes(1):
      IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '0')
        Left: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
        Right: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '0')
  Initializer: 
    IArrayInitializerOperation (0 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{}')
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
IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32(,)) (Syntax: 'New Integer ... ), x(1)) {}')
  Dimension Sizes(2):
      IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, IsImplicit) (Syntax: 'x(0)')
        Left: 
          IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: 'x(0)')
            Array reference: 
              IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32()) (Syntax: 'x')
            Indices(1):
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
        Right: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'x(0)')
      IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, IsImplicit) (Syntax: 'x(1)')
        Left: 
          IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: 'x(1)')
            Array reference: 
              IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32()) (Syntax: 'x')
            Indices(1):
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        Right: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'x(1)')
  Initializer: 
    IArrayInitializerOperation (0 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{}')
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
IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32(,)) (Syntax: 'New Integer(1, 1) {}')
  Dimension Sizes(2):
      IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: '1')
        Left: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        Right: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '1')
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
IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32(,), IsInvalid) (Syntax: 'New Integer(1, 1) {{}}')
  Dimension Sizes(2):
      IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: '1')
        Left: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        Right: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '1')
      IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: '1')
        Left: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        Right: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '1')
  Initializer: 
    IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null, IsInvalid) (Syntax: '{{}}')
      Element Values(1):
          IArrayInitializerOperation (0 elements) (OperationKind.ArrayInitializer, Type: null, IsInvalid) (Syntax: '{}')
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
IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32(,), IsInvalid) (Syntax: 'New Integer ... 1) {{}, {}}')
  Dimension Sizes(2):
      IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: '1')
        Left: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        Right: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '1')
      IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: '1')
        Left: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        Right: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '1')
  Initializer: 
    IArrayInitializerOperation (2 elements) (OperationKind.ArrayInitializer, Type: null, IsInvalid) (Syntax: '{{}, {}}')
      Element Values(2):
          IArrayInitializerOperation (0 elements) (OperationKind.ArrayInitializer, Type: null, IsInvalid) (Syntax: '{}')
            Element Values(0)
          IArrayInitializerOperation (0 elements) (OperationKind.ArrayInitializer, Type: null, IsInvalid) (Syntax: '{}')
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
IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32(,), IsInvalid) (Syntax: 'New Integer ... 1) {{1, 2}}')
  Dimension Sizes(2):
      IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: '1')
        Left: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        Right: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '1')
      IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: '1')
        Left: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        Right: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '1')
  Initializer: 
    IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null, IsInvalid) (Syntax: '{{1, 2}}')
      Element Values(1):
          IArrayInitializerOperation (2 elements) (OperationKind.ArrayInitializer, Type: null, IsInvalid) (Syntax: '{1, 2}')
            Element Values(2):
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2, IsInvalid) (Syntax: '2')
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
IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32(,), IsInvalid) (Syntax: 'New Integer ... {1, 2}, {}}')
  Dimension Sizes(2):
      IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: '1')
        Left: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        Right: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '1')
      IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: '1')
        Left: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        Right: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '1')
  Initializer: 
    IArrayInitializerOperation (2 elements) (OperationKind.ArrayInitializer, Type: null, IsInvalid) (Syntax: '{{1, 2}, {}}')
      Element Values(2):
          IArrayInitializerOperation (2 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{1, 2}')
            Element Values(2):
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
          IArrayInitializerOperation (0 elements) (OperationKind.ArrayInitializer, Type: null, IsInvalid) (Syntax: '{}')
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
IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32(,)) (Syntax: 'New Integer ... 2}, {1, 2}}')
  Dimension Sizes(2):
      IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: '1')
        Left: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        Right: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '1')
      IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: '1')
        Left: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        Right: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '1')
  Initializer: 
    IArrayInitializerOperation (2 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{{1, 2}, {1, 2}}')
      Element Values(2):
          IArrayInitializerOperation (2 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{1, 2}')
            Element Values(2):
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
          IArrayInitializerOperation (2 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{1, 2}')
            Element Values(2):
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
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
IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32(,,,)) (Syntax: '{{{{1, 2}}}, {{{3, 4}}}}')
  Dimension Sizes(4):
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: '{{{{1, 2}}}, {{{3, 4}}}}')
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '{{{{1, 2}}}, {{{3, 4}}}}')
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '{{{{1, 2}}}, {{{3, 4}}}}')
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: '{{{{1, 2}}}, {{{3, 4}}}}')
  Initializer: 
    IArrayInitializerOperation (2 elements) (OperationKind.ArrayInitializer, Type: null, IsImplicit) (Syntax: '{{{{1, 2}}}, {{{3, 4}}}}')
      Element Values(2):
          IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{{{1, 2}}}')
            Element Values(1):
                IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{{1, 2}}')
                  Element Values(1):
                      IArrayInitializerOperation (2 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{1, 2}')
                        Element Values(2):
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
          IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{{{3, 4}}}')
            Element Values(1):
                IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{{3, 4}}')
                  Element Values(1):
                      IArrayInitializerOperation (2 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{3, 4}')
                        Element Values(2):
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 4) (Syntax: '4')
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
IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.String(,), IsInvalid) (Syntax: 'New String(1,) {}')
  Dimension Sizes(2):
      IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: '1')
        Left: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        Right: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '1')
      IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: '')
        Left: 
          IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: '')
            Children(0)
        Right: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid, IsImplicit) (Syntax: '')
  Initializer: 
    IArrayInitializerOperation (0 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{}')
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
IArrayCreationOperation (OperationKind.ArrayCreation, Type: C(), IsInvalid) (Syntax: 'New C() {1}')
  Dimension Sizes(1):
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid, IsImplicit) (Syntax: 'New C() {1}')
  Initializer: 
    IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null, IsInvalid) (Syntax: '{1}')
      Element Values(1):
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: C, IsInvalid, IsImplicit) (Syntax: '1')
            Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
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
IArrayCreationOperation (OperationKind.ArrayCreation, Type: C(), IsInvalid) (Syntax: 'New C(c) {}')
  Dimension Sizes(1):
      IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'c')
        Left: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'c')
            Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C, IsInvalid) (Syntax: 'c')
        Right: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid, IsImplicit) (Syntax: 'c')
  Initializer: 
    IArrayInitializerOperation (0 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{}')
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
IArrayCreationOperation (OperationKind.ArrayCreation, Type: C()) (Syntax: 'New C(M()) {}')
  Dimension Sizes(1):
      IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, IsImplicit) (Syntax: 'M()')
        Left: 
          IInvocationOperation ( Function C.M() As System.Int32) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'M()')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'M')
            Arguments(0)
        Right: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'M()')
  Initializer: 
    IArrayInitializerOperation (0 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{}')
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
IArrayCreationOperation (OperationKind.ArrayCreation, Type: C()) (Syntax: 'New C(Direc ... nteger)) {}')
  Dimension Sizes(1):
      IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, IsImplicit) (Syntax: 'DirectCast(M(), Integer)')
        Left: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32) (Syntax: 'DirectCast(M(), Integer)')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IInvocationOperation ( Function C.M() As System.Object) (OperationKind.Invocation, Type: System.Object) (Syntax: 'M()')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'M')
                Arguments(0)
        Right: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'DirectCast(M(), Integer)')
  Initializer: 
    IArrayInitializerOperation (0 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{}')
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
IArrayCreationOperation (OperationKind.ArrayCreation, Type: C(), IsInvalid) (Syntax: 'New C(M()) {}')
  Dimension Sizes(1):
      IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'M()')
        Left: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'M()')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IInvocationOperation ( Function C.M() As System.Object) (OperationKind.Invocation, Type: System.Object, IsInvalid) (Syntax: 'M()')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'M')
                Arguments(0)
        Right: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid, IsImplicit) (Syntax: 'M()')
  Initializer: 
    IArrayInitializerOperation (0 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{}')
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
IArrayCreationOperation (OperationKind.ArrayCreation, Type: C(), IsInvalid) (Syntax: 'New C(Direc ... nteger)) {}')
  Dimension Sizes(1):
      IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'DirectCast(M(), Integer)')
        Left: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsInvalid) (Syntax: 'DirectCast(M(), Integer)')
            Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IInvocationOperation ( Function C.M() As C) (OperationKind.Invocation, Type: C, IsInvalid) (Syntax: 'M()')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'M')
                Arguments(0)
        Right: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid, IsImplicit) (Syntax: 'DirectCast(M(), Integer)')
  Initializer: 
    IArrayInitializerOperation (0 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{}')
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim x(2) As Integer')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'x(2) As Integer')
    Declarators:
        IVariableDeclaratorOperation (Symbol: x As System.Int32()) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'x(2)')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsImplicit) (Syntax: 'x(2)')
              IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32(), IsImplicit) (Syntax: 'x(2)')
                Dimension Sizes(1):
                    IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, Constant: 3, IsImplicit) (Syntax: '2')
                      Left: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '2')
                Initializer: 
                  null
    Initializer: 
      null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact, WorkItem(7299, "https://github.com/dotnet/roslyn/issues/7299")>
        Public Sub SimpleArrayCreation_ConstantConversion()
            Dim source = <![CDATA[
Option Strict On
Class C
    Public Sub F()
        Dim a = New String(0.0) {}'BIND:"New String(0.0) {}"
    End Sub
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.String(), IsInvalid) (Syntax: 'New String(0.0) {}')
  Dimension Sizes(1):
      IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, Constant: 1, IsInvalid, IsImplicit) (Syntax: '0.0')
        Left: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, Constant: 0, IsInvalid, IsImplicit) (Syntax: '0.0')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              ILiteralOperation (OperationKind.Literal, Type: System.Double, Constant: 0, IsInvalid) (Syntax: '0.0')
        Right: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid, IsImplicit) (Syntax: '0.0')
  Initializer: 
    IArrayInitializerOperation (0 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{}')
      Element Values(0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30512: Option Strict On disallows implicit conversions from 'Double' to 'Integer'.
        Dim a = New String(0.0) {}'BIND:"New String(0.0) {}"
                           ~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ArrayCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact>
        Public Sub SimpleArrayCreation_WithImplicitArrayInitializer()
            Dim source = <![CDATA[
Class C
    Private s1(10) As Integer'BIND:"10"
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LiteralExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/26794")>
        Public Sub SimpleArrayCreation_WithImplicitArrayInitializer_02()
            Dim source = <![CDATA[
Class C
    Private s1(10) As Integer'BIND:"s1(10)"
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IFieldInitializerOperation (Field: C.s1 As System.Int32()) (OperationKind.FieldInitializer, Type: null) (Syntax: 's1(10)')
  IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32(), IsImplicit) (Syntax: 's1(10)')
    Dimension Sizes(1):
        IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, Constant: 11, IsImplicit) (Syntax: '10')
          Left: 
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
          Right: 
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '10')
    Initializer: 
      null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ModifiedIdentifierSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/26780")>
        Public Sub SimpleArrayCreation_WithImplicitArrayInitializerAndExplicitInitializer()
            Dim source = <![CDATA[
Class C
    Private s1(10) As Integer = Nothing'BIND:"10"
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30672: Explicit initialization is not permitted for arrays declared with explicit bounds.
    Private s1(10) As Integer = Nothing'BIND:"10"
            ~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LiteralExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact>
        Public Sub ArrayCreationAndInitializer_MultiDimArrayInitializer_ErrorCase()
            ' Error case where one array initializer element value is a nested array initializer and another one is not.
            Dim source = <![CDATA[
Class C
    Private Sub M(a1 As Integer(,), v1 As Integer, v2 As Integer)
        a1 = New Integer(1, 0) {v1, {v2}}'BIND:"New Integer(1, 0) {v1, {v2}}"
    End Sub
End Class
]]>.Value

            ' See https://github.com/dotnet/roslyn/issues/26900
            Dim expectedOperationTree = <![CDATA[
IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32(,), IsInvalid) (Syntax: 'New Integer ...  {v1, {v2}}')
  Dimension Sizes(2):
      IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: '1')
        Left: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        Right: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '1')
      IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '0')
        Left: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
        Right: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '0')
  Initializer: 
    IArrayInitializerOperation (2 elements) (OperationKind.ArrayInitializer, Type: null, IsInvalid) (Syntax: '{v1, {v2}}')
      Element Values(2):
          IArrayInitializerOperation (0 elements) (OperationKind.ArrayInitializer, Type: null, IsInvalid) (Syntax: 'v1')
            Element Values(0)
          IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{v2}')
            Element Values(1):
                IParameterReferenceOperation: v2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'v2')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30565: Array initializer has too few dimensions.
        a1 = New Integer(1, 0) {v1, {v2}}'BIND:"New Integer(1, 0) {v1, {v2}}"
                                ~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ArrayCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact>
        Public Sub ArrayCreationAndInitializer_NoControlFlow()
            Dim source = <![CDATA[
Class C
    Const c1 As Integer = 1, c2 As Integer = 0, c3 As Integer = 0

    Private Sub M(a1 As Integer(), a2 As Integer(), a3 As Integer(,), a4 As Integer(,), a5 As Integer()(), a6 As Integer()(), d1 As Integer, d2 As Integer, d3 As Integer, d4 As Integer, v1 As Integer, v2 As Integer, v3 As Integer, v4 As Integer) 'BIND:"Private Sub M(a1 As Integer(), a2 As Integer(), a3 As Integer(,), a4 As Integer(,), a5 As Integer()(), a6 As Integer()(), d1 As Integer, d2 As Integer, d3 As Integer, d4 As Integer, v1 As Integer, v2 As Integer, v3 As Integer, v4 As Integer)"
        a1 = New Integer(d1) {}                         ' Single dimension, no initializer
        a2 = New Integer() {v1}                         ' Single dimension, initializer
        a3 = New Integer(d2, d3) {}                     ' Multi-dimension, no initializer
        a4 = New Integer(c1, c2) {{v2}, {v3}}           ' Multi-dimension, initializer
        a5 = New Integer(d4)() {}                       ' Jagged, no initializer
        a6 = New Integer(c3)() {New Integer() {v4}}     ' Jagged, initializer
        Dim f = {1, 2, 3}                               ' Array creation with array literal initializer
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
    Locals: [f As System.Int32()]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (7)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a1 = New Integer(d1) {}')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32(), IsImplicit) (Syntax: 'a1 = New Integer(d1) {}')
                  Left: 
                    IParameterReferenceOperation: a1 (OperationKind.ParameterReference, Type: System.Int32()) (Syntax: 'a1')
                  Right: 
                    IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32()) (Syntax: 'New Integer(d1) {}')
                      Dimension Sizes(1):
                          IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, IsImplicit) (Syntax: 'd1')
                            Left: 
                              IParameterReferenceOperation: d1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'd1')
                            Right: 
                              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'd1')
                      Initializer: 
                        IArrayInitializerOperation (0 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{}')
                          Element Values(0)

            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a2 = New Integer() {v1}')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32(), IsImplicit) (Syntax: 'a2 = New Integer() {v1}')
                  Left: 
                    IParameterReferenceOperation: a2 (OperationKind.ParameterReference, Type: System.Int32()) (Syntax: 'a2')
                  Right: 
                    IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32()) (Syntax: 'New Integer() {v1}')
                      Dimension Sizes(1):
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'New Integer() {v1}')
                      Initializer: 
                        IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{v1}')
                          Element Values(1):
                              IParameterReferenceOperation: v1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'v1')

            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a3 = New In ... (d2, d3) {}')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32(,), IsImplicit) (Syntax: 'a3 = New In ... (d2, d3) {}')
                  Left: 
                    IParameterReferenceOperation: a3 (OperationKind.ParameterReference, Type: System.Int32(,)) (Syntax: 'a3')
                  Right: 
                    IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32(,)) (Syntax: 'New Integer(d2, d3) {}')
                      Dimension Sizes(2):
                          IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, IsImplicit) (Syntax: 'd2')
                            Left: 
                              IParameterReferenceOperation: d2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'd2')
                            Right: 
                              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'd2')
                          IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, IsImplicit) (Syntax: 'd3')
                            Left: 
                              IParameterReferenceOperation: d3 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'd3')
                            Right: 
                              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'd3')
                      Initializer: 
                        IArrayInitializerOperation (0 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{}')
                          Element Values(0)

            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a4 = New In ... {v2}, {v3}}')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32(,), IsImplicit) (Syntax: 'a4 = New In ... {v2}, {v3}}')
                  Left: 
                    IParameterReferenceOperation: a4 (OperationKind.ParameterReference, Type: System.Int32(,)) (Syntax: 'a4')
                  Right: 
                    IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32(,)) (Syntax: 'New Integer ... {v2}, {v3}}')
                      Dimension Sizes(2):
                          IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: 'c1')
                            Left: 
                              IFieldReferenceOperation: C.c1 As System.Int32 (Static) (OperationKind.FieldReference, Type: System.Int32, Constant: 1) (Syntax: 'c1')
                                Instance Receiver: 
                                  null
                            Right: 
                              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'c1')
                          IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'c2')
                            Left: 
                              IFieldReferenceOperation: C.c2 As System.Int32 (Static) (OperationKind.FieldReference, Type: System.Int32, Constant: 0) (Syntax: 'c2')
                                Instance Receiver: 
                                  null
                            Right: 
                              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'c2')
                      Initializer: 
                        IArrayInitializerOperation (2 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{{v2}, {v3}}')
                          Element Values(2):
                              IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{v2}')
                                Element Values(1):
                                    IParameterReferenceOperation: v2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'v2')
                              IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{v3}')
                                Element Values(1):
                                    IParameterReferenceOperation: v3 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'v3')

            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a5 = New In ... er(d4)() {}')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32()(), IsImplicit) (Syntax: 'a5 = New In ... er(d4)() {}')
                  Left: 
                    IParameterReferenceOperation: a5 (OperationKind.ParameterReference, Type: System.Int32()()) (Syntax: 'a5')
                  Right: 
                    IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32()()) (Syntax: 'New Integer(d4)() {}')
                      Dimension Sizes(1):
                          IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, IsImplicit) (Syntax: 'd4')
                            Left: 
                              IParameterReferenceOperation: d4 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'd4')
                            Right: 
                              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'd4')
                      Initializer: 
                        IArrayInitializerOperation (0 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{}')
                          Element Values(0)

            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a6 = New In ... ger() {v4}}')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32()(), IsImplicit) (Syntax: 'a6 = New In ... ger() {v4}}')
                  Left: 
                    IParameterReferenceOperation: a6 (OperationKind.ParameterReference, Type: System.Int32()()) (Syntax: 'a6')
                  Right: 
                    IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32()()) (Syntax: 'New Integer ... ger() {v4}}')
                      Dimension Sizes(1):
                          IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'c3')
                            Left: 
                              IFieldReferenceOperation: C.c3 As System.Int32 (Static) (OperationKind.FieldReference, Type: System.Int32, Constant: 0) (Syntax: 'c3')
                                Instance Receiver: 
                                  null
                            Right: 
                              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'c3')
                      Initializer: 
                        IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{New Integer() {v4}}')
                          Element Values(1):
                              IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32()) (Syntax: 'New Integer() {v4}')
                                Dimension Sizes(1):
                                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'New Integer() {v4}')
                                Initializer: 
                                  IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{v4}')
                                    Element Values(1):
                                        IParameterReferenceOperation: v4 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'v4')

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32(), IsImplicit) (Syntax: 'f = {1, 2, 3}')
              Left: 
                ILocalReferenceOperation: f (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32(), IsImplicit) (Syntax: 'f')
              Right: 
                IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32()) (Syntax: '{1, 2, 3}')
                  Dimension Sizes(1):
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3, IsImplicit) (Syntax: '{1, 2, 3}')
                  Initializer: 
                    IArrayInitializerOperation (3 elements) (OperationKind.ArrayInitializer, Type: null, IsImplicit) (Syntax: '{1, 2, 3}')
                      Element Values(3):
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')

        Next (Regular) Block[B2]
            Leaving: {R1}
}

Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact>
        Public Sub ArrayCreationAndInitializer_ControlFlowInFirstDimension_NoInitializer()
            Dim source = <![CDATA[
Class C
    Private Sub M(a1 As Integer(,), d1 As Integer?, d2 As Integer, c As Integer) 'BIND:"Private Sub M(a1 As Integer(,), d1 As Integer?, d2 As Integer, c As Integer)"
        a1 = New Integer(If(d1, d2), c) {}
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
                IParameterReferenceOperation: a1 (OperationKind.ParameterReference, Type: System.Int32(,)) (Syntax: 'a1')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd1')
                  Value: 
                    IParameterReferenceOperation: d1 (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'd1')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'd1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'd1')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd1')
                  Value: 
                    IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'd1')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'd1')
                      Arguments(0)

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd2')
              Value: 
                IParameterReferenceOperation: d2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'd2')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a1 = New In ...  d2), c) {}')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32(,), IsImplicit) (Syntax: 'a1 = New In ...  d2), c) {}')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32(,), IsImplicit) (Syntax: 'a1')
                  Right: 
                    IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32(,)) (Syntax: 'New Integer ...  d2), c) {}')
                      Dimension Sizes(2):
                          IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, IsImplicit) (Syntax: 'If(d1, d2)')
                            Left: 
                              IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(d1, d2)')
                            Right: 
                              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'If(d1, d2)')
                          IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, IsImplicit) (Syntax: 'c')
                            Left: 
                              IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'c')
                            Right: 
                              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'c')
                      Initializer: 
                        IArrayInitializerOperation (0 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{}')
                          Element Values(0)

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact>
        Public Sub ArrayCreationAndInitializer_ControlFlowInFirstDimension_WithInitializer()
            Dim source = <![CDATA[
Class C
    Const c As Integer = 0
    Private Sub M(a1 As Integer(,), d1 As Integer?, d2 As Integer, v1 As Integer) 'BIND:"Private Sub M(a1 As Integer(,), d1 As Integer?, d2 As Integer, v1 As Integer)"
        a1 = New Integer(If(d1, d2), c) {{v1}}
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
                IParameterReferenceOperation: a1 (OperationKind.ParameterReference, Type: System.Int32(,)) (Syntax: 'a1')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd1')
                  Value: 
                    IParameterReferenceOperation: d1 (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'd1')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'd1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'd1')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd1')
                  Value: 
                    IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'd1')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'd1')
                      Arguments(0)

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd2')
              Value: 
                IParameterReferenceOperation: d2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'd2')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'a1 = New In ... , c) {{v1}}')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32(,), IsInvalid, IsImplicit) (Syntax: 'a1 = New In ... , c) {{v1}}')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32(,), IsImplicit) (Syntax: 'a1')
                  Right: 
                    IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32(,), IsInvalid) (Syntax: 'New Integer ... , c) {{v1}}')
                      Dimension Sizes(2):
                          IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, IsImplicit) (Syntax: 'If(d1, d2)')
                            Left: 
                              IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(d1, d2)')
                            Right: 
                              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'If(d1, d2)')
                          IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'c')
                            Left: 
                              IFieldReferenceOperation: C.c As System.Int32 (Static) (OperationKind.FieldReference, Type: System.Int32, Constant: 0) (Syntax: 'c')
                                Instance Receiver: 
                                  null
                            Right: 
                              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'c')
                      Initializer: 
                        IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null, IsInvalid) (Syntax: '{{v1}}')
                          Element Values(1):
                              IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null, IsInvalid) (Syntax: '{v1}')
                                Element Values(1):
                                    IParameterReferenceOperation: v1 (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'v1')

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30949: Array initializer cannot be specified for a non constant dimension; use the empty initializer '{}'.
        a1 = New Integer(If(d1, d2), c) {{v1}}
                                        ~~~~~~
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact>
        Public Sub ArrayCreationAndInitializer_ControlFlowInSecondDimension_NoInitializer()
            Dim source = <![CDATA[
Class C
    Private Sub M(a1 As Integer(,), d1 As Integer?, d2 As Integer, c As Integer) 'BIND:"Private Sub M(a1 As Integer(,), d1 As Integer?, d2 As Integer, c As Integer)"
        a1 = New Integer(c, If(d1, d2)) {}
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
    CaptureIds: [0] [1] [3]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a1')
              Value: 
                IParameterReferenceOperation: a1 (OperationKind.ParameterReference, Type: System.Int32(,)) (Syntax: 'a1')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value: 
                IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, IsImplicit) (Syntax: 'c')
                  Left: 
                    IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'c')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'c')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd1')
                  Value: 
                    IParameterReferenceOperation: d1 (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'd1')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'd1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'd1')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd1')
                  Value: 
                    IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'd1')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'd1')
                      Arguments(0)

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd2')
              Value: 
                IParameterReferenceOperation: d2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'd2')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a1 = New In ... d1, d2)) {}')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32(,), IsImplicit) (Syntax: 'a1 = New In ... d1, d2)) {}')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32(,), IsImplicit) (Syntax: 'a1')
                  Right: 
                    IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32(,)) (Syntax: 'New Integer ... d1, d2)) {}')
                      Dimension Sizes(2):
                          IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'c')
                          IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, IsImplicit) (Syntax: 'If(d1, d2)')
                            Left: 
                              IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(d1, d2)')
                            Right: 
                              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'If(d1, d2)')
                      Initializer: 
                        IArrayInitializerOperation (0 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{}')
                          Element Values(0)

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact>
        Public Sub ArrayCreationAndInitializer_ControlFlowInSecondDimension_WithInitializer()
            Dim source = <![CDATA[
Class C
    Const c As Integer = 0
    Private Sub M(a1 As Integer(,), d1 As Integer?, d2 As Integer, v1 As Integer) 'BIND:"Private Sub M(a1 As Integer(,), d1 As Integer?, d2 As Integer, v1 As Integer)"
        a1 = New Integer(c, If(d1, d2)) {{v1}}
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
    CaptureIds: [0] [1] [3]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a1')
              Value: 
                IParameterReferenceOperation: a1 (OperationKind.ParameterReference, Type: System.Int32(,)) (Syntax: 'a1')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value: 
                IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'c')
                  Left: 
                    IFieldReferenceOperation: C.c As System.Int32 (Static) (OperationKind.FieldReference, Type: System.Int32, Constant: 0) (Syntax: 'c')
                      Instance Receiver: 
                        null
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'c')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd1')
                  Value: 
                    IParameterReferenceOperation: d1 (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'd1')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'd1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'd1')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd1')
                  Value: 
                    IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'd1')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'd1')
                      Arguments(0)

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd2')
              Value: 
                IParameterReferenceOperation: d2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'd2')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'a1 = New In ... d2)) {{v1}}')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32(,), IsInvalid, IsImplicit) (Syntax: 'a1 = New In ... d2)) {{v1}}')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32(,), IsImplicit) (Syntax: 'a1')
                  Right: 
                    IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32(,), IsInvalid) (Syntax: 'New Integer ... d2)) {{v1}}')
                      Dimension Sizes(2):
                          IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'c')
                          IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, IsImplicit) (Syntax: 'If(d1, d2)')
                            Left: 
                              IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(d1, d2)')
                            Right: 
                              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'If(d1, d2)')
                      Initializer: 
                        IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null, IsInvalid) (Syntax: '{{v1}}')
                          Element Values(1):
                              IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null, IsInvalid) (Syntax: '{v1}')
                                Element Values(1):
                                    IParameterReferenceOperation: v1 (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'v1')

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30949: Array initializer cannot be specified for a non constant dimension; use the empty initializer '{}'.
        a1 = New Integer(c, If(d1, d2)) {{v1}}
                                         ~~~~
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact>
        Public Sub ArrayCreationAndInitializer_ControlFlowInMultipleDimensions()
            Dim source = <![CDATA[
Class C
    Private Sub M(a1 As Integer(,), d1 As Integer?, d2 As Integer, d3 As Integer?, d4 As Integer, v1 As Integer) 'BIND:"Private Sub M(a1 As Integer(,), d1 As Integer?, d2 As Integer, d3 As Integer?, d4 As Integer, v1 As Integer)"
        a1 = New Integer(If(d1, d2), If(d3, d4)) {{v1}}
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
    CaptureIds: [0] [3] [5]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a1')
              Value: 
                IParameterReferenceOperation: a1 (OperationKind.ParameterReference, Type: System.Int32(,)) (Syntax: 'a1')

        Next (Regular) Block[B2]
            Entering: {R2} {R3}

    .locals {R2}
    {
        CaptureIds: [2]
        .locals {R3}
        {
            CaptureIds: [1]
            Block[B2] - Block
                Predecessors: [B1]
                Statements (1)
                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd1')
                      Value: 
                        IParameterReferenceOperation: d1 (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'd1')

                Jump if True (Regular) to Block[B4]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'd1')
                      Operand: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'd1')
                    Leaving: {R3}

                Next (Regular) Block[B3]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd1')
                      Value: 
                        IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'd1')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'd1')
                          Arguments(0)

                Next (Regular) Block[B5]
                    Leaving: {R3}
        }

        Block[B4] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd2')
                  Value: 
                    IParameterReferenceOperation: d2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'd2')

            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'If(d1, d2)')
                  Value: 
                    IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, IsImplicit) (Syntax: 'If(d1, d2)')
                      Left: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(d1, d2)')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'If(d1, d2)')

            Next (Regular) Block[B6]
                Leaving: {R2}
                Entering: {R4}
    }
    .locals {R4}
    {
        CaptureIds: [4]
        Block[B6] - Block
            Predecessors: [B5]
            Statements (1)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd3')
                  Value: 
                    IParameterReferenceOperation: d3 (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'd3')

            Jump if True (Regular) to Block[B8]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'd3')
                  Operand: 
                    IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'd3')
                Leaving: {R4}

            Next (Regular) Block[B7]
        Block[B7] - Block
            Predecessors: [B6]
            Statements (1)
                IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd3')
                  Value: 
                    IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'd3')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'd3')
                      Arguments(0)

            Next (Regular) Block[B9]
                Leaving: {R4}
    }

    Block[B8] - Block
        Predecessors: [B6]
        Statements (1)
            IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd4')
              Value: 
                IParameterReferenceOperation: d4 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'd4')

        Next (Regular) Block[B9]
    Block[B9] - Block
        Predecessors: [B7] [B8]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'a1 = New In ... d4)) {{v1}}')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32(,), IsInvalid, IsImplicit) (Syntax: 'a1 = New In ... d4)) {{v1}}')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32(,), IsImplicit) (Syntax: 'a1')
                  Right: 
                    IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32(,), IsInvalid) (Syntax: 'New Integer ... d4)) {{v1}}')
                      Dimension Sizes(2):
                          IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(d1, d2)')
                          IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, IsImplicit) (Syntax: 'If(d3, d4)')
                            Left: 
                              IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(d3, d4)')
                            Right: 
                              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'If(d3, d4)')
                      Initializer: 
                        IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null, IsInvalid) (Syntax: '{{v1}}')
                          Element Values(1):
                              IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null, IsInvalid) (Syntax: '{v1}')
                                Element Values(1):
                                    IParameterReferenceOperation: v1 (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'v1')

        Next (Regular) Block[B10]
            Leaving: {R1}
}

Block[B10] - Exit
    Predecessors: [B9]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30949: Array initializer cannot be specified for a non constant dimension; use the empty initializer '{}'.
        a1 = New Integer(If(d1, d2), If(d3, d4)) {{v1}}
                                                 ~~~~~~
BC30949: Array initializer cannot be specified for a non constant dimension; use the empty initializer '{}'.
        a1 = New Integer(If(d1, d2), If(d3, d4)) {{v1}}
                                                  ~~~~
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact>
        Public Sub ArrayCreationAndInitializer_ControlFlowInFirstInitializerValue_SingleDimArray()
            Dim source = <![CDATA[
Class C
    Const c1 As Integer = 1
    Private Sub M(a1 As Integer(), v1 As Integer?, v2 As Integer, v3 As Integer) 'BIND:"Private Sub M(a1 As Integer(), v1 As Integer?, v2 As Integer, v3 As Integer)"
        a1 = New Integer(c1) {If(v1, v2), v3}
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
    CaptureIds: [0] [1] [3]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a1')
              Value: 
                IParameterReferenceOperation: a1 (OperationKind.ParameterReference, Type: System.Int32()) (Syntax: 'a1')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
              Value: 
                IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: 'c1')
                  Left: 
                    IFieldReferenceOperation: C.c1 As System.Int32 (Static) (OperationKind.FieldReference, Type: System.Int32, Constant: 1) (Syntax: 'c1')
                      Instance Receiver: 
                        null
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'c1')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'v1')
                  Value: 
                    IParameterReferenceOperation: v1 (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'v1')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'v1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'v1')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'v1')
                  Value: 
                    IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'v1')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'v1')
                      Arguments(0)

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'v2')
              Value: 
                IParameterReferenceOperation: v2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'v2')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a1 = New In ... 1, v2), v3}')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32(), IsImplicit) (Syntax: 'a1 = New In ... 1, v2), v3}')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32(), IsImplicit) (Syntax: 'a1')
                  Right: 
                    IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32()) (Syntax: 'New Integer ... 1, v2), v3}')
                      Dimension Sizes(1):
                          IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: 'c1')
                      Initializer: 
                        IArrayInitializerOperation (2 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{If(v1, v2), v3}')
                          Element Values(2):
                              IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(v1, v2)')
                              IParameterReferenceOperation: v3 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'v3')

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact>
        Public Sub ArrayCreationAndInitializer_ControlFlowInFirstInitializerValue_MultiDimArray()
            Dim source = <![CDATA[
Class C
    Const c1 As Integer = 1, c2 As Integer = 0
    Private Sub M(a1 As Integer(,), v1 As Integer?, v2 As Integer, v3 As Integer) 'BIND:"Private Sub M(a1 As Integer(,), v1 As Integer?, v2 As Integer, v3 As Integer)"
        a1 = New Integer(c1, c2) {{If(v1, v2)}, {v3}}
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
    CaptureIds: [0] [1] [2] [4]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (3)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a1')
              Value: 
                IParameterReferenceOperation: a1 (OperationKind.ParameterReference, Type: System.Int32(,)) (Syntax: 'a1')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
              Value: 
                IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: 'c1')
                  Left: 
                    IFieldReferenceOperation: C.c1 As System.Int32 (Static) (OperationKind.FieldReference, Type: System.Int32, Constant: 1) (Syntax: 'c1')
                      Instance Receiver: 
                        null
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'c1')

            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c2')
              Value: 
                IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'c2')
                  Left: 
                    IFieldReferenceOperation: C.c2 As System.Int32 (Static) (OperationKind.FieldReference, Type: System.Int32, Constant: 0) (Syntax: 'c2')
                      Instance Receiver: 
                        null
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'c2')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [3]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'v1')
                  Value: 
                    IParameterReferenceOperation: v1 (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'v1')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'v1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'v1')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'v1')
                  Value: 
                    IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'v1')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'v1')
                      Arguments(0)

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'v2')
              Value: 
                IParameterReferenceOperation: v2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'v2')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a1 = New In ... v2)}, {v3}}')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32(,), IsImplicit) (Syntax: 'a1 = New In ... v2)}, {v3}}')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32(,), IsImplicit) (Syntax: 'a1')
                  Right: 
                    IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32(,)) (Syntax: 'New Integer ... v2)}, {v3}}')
                      Dimension Sizes(2):
                          IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: 'c1')
                          IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'c2')
                      Initializer: 
                        IArrayInitializerOperation (2 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{{If(v1, v2)}, {v3}}')
                          Element Values(2):
                              IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{If(v1, v2)}')
                                Element Values(1):
                                    IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(v1, v2)')
                              IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{v3}')
                                Element Values(1):
                                    IParameterReferenceOperation: v3 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'v3')

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact>
        Public Sub ArrayCreationAndInitializer_ControlFlowInSecondInitializerValue_SingleDimArray()
            Dim source = <![CDATA[
Class C
    Const c1 As Integer = 1
    Private Sub M(a1 As Integer(), v1 As Integer?, v2 As Integer, v3 As Integer) 'BIND:"Private Sub M(a1 As Integer(), v1 As Integer?, v2 As Integer, v3 As Integer)"
        a1 = New Integer(c1) {v3, If(v1, v2)}
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
    CaptureIds: [0] [1] [2] [4]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (3)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a1')
              Value: 
                IParameterReferenceOperation: a1 (OperationKind.ParameterReference, Type: System.Int32()) (Syntax: 'a1')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
              Value: 
                IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: 'c1')
                  Left: 
                    IFieldReferenceOperation: C.c1 As System.Int32 (Static) (OperationKind.FieldReference, Type: System.Int32, Constant: 1) (Syntax: 'c1')
                      Instance Receiver: 
                        null
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'c1')

            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'v3')
              Value: 
                IParameterReferenceOperation: v3 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'v3')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [3]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'v1')
                  Value: 
                    IParameterReferenceOperation: v1 (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'v1')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'v1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'v1')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'v1')
                  Value: 
                    IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'v1')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'v1')
                      Arguments(0)

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'v2')
              Value: 
                IParameterReferenceOperation: v2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'v2')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a1 = New In ... If(v1, v2)}')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32(), IsImplicit) (Syntax: 'a1 = New In ... If(v1, v2)}')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32(), IsImplicit) (Syntax: 'a1')
                  Right: 
                    IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32()) (Syntax: 'New Integer ... If(v1, v2)}')
                      Dimension Sizes(1):
                          IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: 'c1')
                      Initializer: 
                        IArrayInitializerOperation (2 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{v3, If(v1, v2)}')
                          Element Values(2):
                              IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'v3')
                              IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(v1, v2)')

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact>
        Public Sub ArrayCreationAndInitializer_ControlFlowInSecondInitializerValue_MultiDimArray()
            Dim source = <![CDATA[
Class C
    Const c1 As Integer = 1, c2 As Integer = 0
    Private Sub M(a1 As Integer(,), v1 As Integer?, v2 As Integer, v3 As Integer) 'BIND:"Private Sub M(a1 As Integer(,), v1 As Integer?, v2 As Integer, v3 As Integer)"
        a1 = New Integer(c1, c2) {{v3}, {If(v1, v2)}}
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
    CaptureIds: [0] [1] [2] [3] [5]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (4)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a1')
              Value: 
                IParameterReferenceOperation: a1 (OperationKind.ParameterReference, Type: System.Int32(,)) (Syntax: 'a1')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
              Value: 
                IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: 'c1')
                  Left: 
                    IFieldReferenceOperation: C.c1 As System.Int32 (Static) (OperationKind.FieldReference, Type: System.Int32, Constant: 1) (Syntax: 'c1')
                      Instance Receiver: 
                        null
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'c1')

            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c2')
              Value: 
                IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'c2')
                  Left: 
                    IFieldReferenceOperation: C.c2 As System.Int32 (Static) (OperationKind.FieldReference, Type: System.Int32, Constant: 0) (Syntax: 'c2')
                      Instance Receiver: 
                        null
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'c2')

            IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'v3')
              Value: 
                IParameterReferenceOperation: v3 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'v3')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [4]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'v1')
                  Value: 
                    IParameterReferenceOperation: v1 (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'v1')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'v1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'v1')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'v1')
                  Value: 
                    IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'v1')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'v1')
                      Arguments(0)

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'v2')
              Value: 
                IParameterReferenceOperation: v2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'v2')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a1 = New In ... f(v1, v2)}}')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32(,), IsImplicit) (Syntax: 'a1 = New In ... f(v1, v2)}}')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32(,), IsImplicit) (Syntax: 'a1')
                  Right: 
                    IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32(,)) (Syntax: 'New Integer ... f(v1, v2)}}')
                      Dimension Sizes(2):
                          IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: 'c1')
                          IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'c2')
                      Initializer: 
                        IArrayInitializerOperation (2 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{{v3}, {If(v1, v2)}}')
                          Element Values(2):
                              IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{v3}')
                                Element Values(1):
                                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'v3')
                              IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{If(v1, v2)}')
                                Element Values(1):
                                    IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(v1, v2)')

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact>
        Public Sub ArrayCreationAndInitializer_ControlFlowInSecondInitializerValue_MultiDimArray_02()
            ' Error case where one array initializer element value is a nested array initializer and another one is not.
            ' Verifies that CFG builder handles the mixed kind element values.

            ' Note: CFG seems to be affected by https://github.com/dotnet/roslyn/issues/26900
            Dim source = <![CDATA[
Class C
    Const c1 As Integer = 1, c2 As Integer = 0
    Private Sub M(a1 As Integer(,), v1 As Integer?, v2 As Integer, v3 As Integer) 'BIND:"Private Sub M(a1 As Integer(,), v1 As Integer?, v2 As Integer, v3 As Integer)"
        a1 = New Integer(c1, c2) {v3, {If(v1, v2)}}
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
    CaptureIds: [0] [1] [2] [4]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (3)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a1')
              Value: 
                IParameterReferenceOperation: a1 (OperationKind.ParameterReference, Type: System.Int32(,)) (Syntax: 'a1')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
              Value: 
                IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: 'c1')
                  Left: 
                    IFieldReferenceOperation: C.c1 As System.Int32 (Static) (OperationKind.FieldReference, Type: System.Int32, Constant: 1) (Syntax: 'c1')
                      Instance Receiver: 
                        null
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'c1')

            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c2')
              Value: 
                IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'c2')
                  Left: 
                    IFieldReferenceOperation: C.c2 As System.Int32 (Static) (OperationKind.FieldReference, Type: System.Int32, Constant: 0) (Syntax: 'c2')
                      Instance Receiver: 
                        null
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'c2')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [3]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'v1')
                  Value: 
                    IParameterReferenceOperation: v1 (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'v1')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'v1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'v1')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'v1')
                  Value: 
                    IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'v1')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'v1')
                      Arguments(0)

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'v2')
              Value: 
                IParameterReferenceOperation: v2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'v2')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'a1 = New In ... f(v1, v2)}}')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32(,), IsInvalid, IsImplicit) (Syntax: 'a1 = New In ... f(v1, v2)}}')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32(,), IsImplicit) (Syntax: 'a1')
                  Right: 
                    IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32(,), IsInvalid) (Syntax: 'New Integer ... f(v1, v2)}}')
                      Dimension Sizes(2):
                          IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: 'c1')
                          IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'c2')
                      Initializer: 
                        IArrayInitializerOperation (2 elements) (OperationKind.ArrayInitializer, Type: null, IsInvalid) (Syntax: '{v3, {If(v1, v2)}}')
                          Element Values(2):
                              IArrayInitializerOperation (0 elements) (OperationKind.ArrayInitializer, Type: null, IsInvalid) (Syntax: 'v3')
                                Element Values(0)
                              IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{If(v1, v2)}')
                                Element Values(1):
                                    IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(v1, v2)')

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30565: Array initializer has too few dimensions.
        a1 = New Integer(c1, c2) {v3, {If(v1, v2)}}
                                  ~~
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact>
        Public Sub ArrayCreationAndInitializer_ControlFlowInMultipleInitializerValues_SingleDimArray()
            Dim source = <![CDATA[
Class C
    Const c1 As Integer = 1
    Private Sub M(a1 As Integer(), v1 As Integer?, v2 As Integer, v3 As Integer?, v4 As Integer) 'BIND:"Private Sub M(a1 As Integer(), v1 As Integer?, v2 As Integer, v3 As Integer?, v4 As Integer)"
        a1 = New Integer(c1) {If(v1, v2), If(v3, v4)}
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
    CaptureIds: [0] [1] [3] [5]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a1')
              Value: 
                IParameterReferenceOperation: a1 (OperationKind.ParameterReference, Type: System.Int32()) (Syntax: 'a1')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
              Value: 
                IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: 'c1')
                  Left: 
                    IFieldReferenceOperation: C.c1 As System.Int32 (Static) (OperationKind.FieldReference, Type: System.Int32, Constant: 1) (Syntax: 'c1')
                      Instance Receiver: 
                        null
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'c1')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'v1')
                  Value: 
                    IParameterReferenceOperation: v1 (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'v1')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'v1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'v1')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'v1')
                  Value: 
                    IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'v1')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'v1')
                      Arguments(0)

            Next (Regular) Block[B5]
                Leaving: {R2}
                Entering: {R3}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'v2')
              Value: 
                IParameterReferenceOperation: v2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'v2')

        Next (Regular) Block[B5]
            Entering: {R3}

    .locals {R3}
    {
        CaptureIds: [4]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (1)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'v3')
                  Value: 
                    IParameterReferenceOperation: v3 (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'v3')

            Jump if True (Regular) to Block[B7]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'v3')
                  Operand: 
                    IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'v3')
                Leaving: {R3}

            Next (Regular) Block[B6]
        Block[B6] - Block
            Predecessors: [B5]
            Statements (1)
                IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'v3')
                  Value: 
                    IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'v3')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'v3')
                      Arguments(0)

            Next (Regular) Block[B8]
                Leaving: {R3}
    }

    Block[B7] - Block
        Predecessors: [B5]
        Statements (1)
            IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'v4')
              Value: 
                IParameterReferenceOperation: v4 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'v4')

        Next (Regular) Block[B8]
    Block[B8] - Block
        Predecessors: [B6] [B7]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a1 = New In ... If(v3, v4)}')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32(), IsImplicit) (Syntax: 'a1 = New In ... If(v3, v4)}')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32(), IsImplicit) (Syntax: 'a1')
                  Right: 
                    IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32()) (Syntax: 'New Integer ... If(v3, v4)}')
                      Dimension Sizes(1):
                          IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: 'c1')
                      Initializer: 
                        IArrayInitializerOperation (2 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{If(v1, v2), If(v3, v4)}')
                          Element Values(2):
                              IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(v1, v2)')
                              IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(v3, v4)')

        Next (Regular) Block[B9]
            Leaving: {R1}
}

Block[B9] - Exit
    Predecessors: [B8]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact>
        Public Sub ArrayCreationAndInitializer_ControlFlowInMultipleInitializerValues_MultiDimArray()
            Dim source = <![CDATA[
Class C
    Const c1 As Integer = 1, c2 As Integer = 0
    Private Sub M(a1 As Integer(,), v1 As Integer?, v2 As Integer, v3 As Integer?, v4 As Integer) 'BIND:"Private Sub M(a1 As Integer(,), v1 As Integer?, v2 As Integer, v3 As Integer?, v4 As Integer)"
        a1 = New Integer(c1, c2) {{If(v1, v2)}, {If(v3, v4)}}
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
    CaptureIds: [0] [1] [2] [4] [6]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (3)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a1')
              Value: 
                IParameterReferenceOperation: a1 (OperationKind.ParameterReference, Type: System.Int32(,)) (Syntax: 'a1')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
              Value: 
                IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: 'c1')
                  Left: 
                    IFieldReferenceOperation: C.c1 As System.Int32 (Static) (OperationKind.FieldReference, Type: System.Int32, Constant: 1) (Syntax: 'c1')
                      Instance Receiver: 
                        null
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'c1')

            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c2')
              Value: 
                IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'c2')
                  Left: 
                    IFieldReferenceOperation: C.c2 As System.Int32 (Static) (OperationKind.FieldReference, Type: System.Int32, Constant: 0) (Syntax: 'c2')
                      Instance Receiver: 
                        null
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'c2')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [3]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'v1')
                  Value: 
                    IParameterReferenceOperation: v1 (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'v1')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'v1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'v1')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'v1')
                  Value: 
                    IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'v1')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'v1')
                      Arguments(0)

            Next (Regular) Block[B5]
                Leaving: {R2}
                Entering: {R3}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'v2')
              Value: 
                IParameterReferenceOperation: v2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'v2')

        Next (Regular) Block[B5]
            Entering: {R3}

    .locals {R3}
    {
        CaptureIds: [5]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (1)
                IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'v3')
                  Value: 
                    IParameterReferenceOperation: v3 (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'v3')

            Jump if True (Regular) to Block[B7]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'v3')
                  Operand: 
                    IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'v3')
                Leaving: {R3}

            Next (Regular) Block[B6]
        Block[B6] - Block
            Predecessors: [B5]
            Statements (1)
                IFlowCaptureOperation: 6 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'v3')
                  Value: 
                    IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'v3')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'v3')
                      Arguments(0)

            Next (Regular) Block[B8]
                Leaving: {R3}
    }

    Block[B7] - Block
        Predecessors: [B5]
        Statements (1)
            IFlowCaptureOperation: 6 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'v4')
              Value: 
                IParameterReferenceOperation: v4 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'v4')

        Next (Regular) Block[B8]
    Block[B8] - Block
        Predecessors: [B6] [B7]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a1 = New In ... f(v3, v4)}}')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32(,), IsImplicit) (Syntax: 'a1 = New In ... f(v3, v4)}}')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32(,), IsImplicit) (Syntax: 'a1')
                  Right: 
                    IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32(,)) (Syntax: 'New Integer ... f(v3, v4)}}')
                      Dimension Sizes(2):
                          IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: 'c1')
                          IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'c2')
                      Initializer: 
                        IArrayInitializerOperation (2 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{{If(v1, v2 ... f(v3, v4)}}')
                          Element Values(2):
                              IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{If(v1, v2)}')
                                Element Values(1):
                                    IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(v1, v2)')
                              IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{If(v3, v4)}')
                                Element Values(1):
                                    IFlowCaptureReferenceOperation: 6 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(v3, v4)')

        Next (Regular) Block[B9]
            Leaving: {R1}
}

Block[B9] - Exit
    Predecessors: [B8]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact>
        Public Sub ArrayCreationAndInitializer_ControlFlowInDimensionAndMultipleInitializerValues_SingleDimArray()
            Dim source = <![CDATA[
Class C
    Private Sub M(a1 As Integer(), v1 As Integer?, v2 As Integer, v3 As Integer?, v4 As Integer, d1 As Integer?, d2 As Integer) 'BIND:"Private Sub M(a1 As Integer(), v1 As Integer?, v2 As Integer, v3 As Integer?, v4 As Integer, d1 As Integer?, d2 As Integer)"
        a1 = New Integer(If(d1, d2)) {If(v1, v2), If(v3, v4)}
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
    CaptureIds: [0] [3] [5] [7]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a1')
              Value: 
                IParameterReferenceOperation: a1 (OperationKind.ParameterReference, Type: System.Int32()) (Syntax: 'a1')

        Next (Regular) Block[B2]
            Entering: {R2} {R3}

    .locals {R2}
    {
        CaptureIds: [2]
        .locals {R3}
        {
            CaptureIds: [1]
            Block[B2] - Block
                Predecessors: [B1]
                Statements (1)
                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd1')
                      Value: 
                        IParameterReferenceOperation: d1 (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'd1')

                Jump if True (Regular) to Block[B4]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'd1')
                      Operand: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'd1')
                    Leaving: {R3}

                Next (Regular) Block[B3]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd1')
                      Value: 
                        IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'd1')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'd1')
                          Arguments(0)

                Next (Regular) Block[B5]
                    Leaving: {R3}
        }

        Block[B4] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd2')
                  Value: 
                    IParameterReferenceOperation: d2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'd2')

            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'If(d1, d2)')
                  Value: 
                    IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, IsImplicit) (Syntax: 'If(d1, d2)')
                      Left: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(d1, d2)')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'If(d1, d2)')

            Next (Regular) Block[B6]
                Leaving: {R2}
                Entering: {R4}
    }
    .locals {R4}
    {
        CaptureIds: [4]
        Block[B6] - Block
            Predecessors: [B5]
            Statements (1)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'v1')
                  Value: 
                    IParameterReferenceOperation: v1 (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32), IsInvalid) (Syntax: 'v1')

            Jump if True (Regular) to Block[B8]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'v1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsInvalid, IsImplicit) (Syntax: 'v1')
                Leaving: {R4}

            Next (Regular) Block[B7]
        Block[B7] - Block
            Predecessors: [B6]
            Statements (1)
                IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'v1')
                  Value: 
                    IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'v1')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsInvalid, IsImplicit) (Syntax: 'v1')
                      Arguments(0)

            Next (Regular) Block[B9]
                Leaving: {R4}
                Entering: {R5}
    }

    Block[B8] - Block
        Predecessors: [B6]
        Statements (1)
            IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'v2')
              Value: 
                IParameterReferenceOperation: v2 (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'v2')

        Next (Regular) Block[B9]
            Entering: {R5}

    .locals {R5}
    {
        CaptureIds: [6]
        Block[B9] - Block
            Predecessors: [B7] [B8]
            Statements (1)
                IFlowCaptureOperation: 6 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'v3')
                  Value: 
                    IParameterReferenceOperation: v3 (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32), IsInvalid) (Syntax: 'v3')

            Jump if True (Regular) to Block[B11]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'v3')
                  Operand: 
                    IFlowCaptureReferenceOperation: 6 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsInvalid, IsImplicit) (Syntax: 'v3')
                Leaving: {R5}

            Next (Regular) Block[B10]
        Block[B10] - Block
            Predecessors: [B9]
            Statements (1)
                IFlowCaptureOperation: 7 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'v3')
                  Value: 
                    IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'v3')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 6 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsInvalid, IsImplicit) (Syntax: 'v3')
                      Arguments(0)

            Next (Regular) Block[B12]
                Leaving: {R5}
    }

    Block[B11] - Block
        Predecessors: [B9]
        Statements (1)
            IFlowCaptureOperation: 7 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'v4')
              Value: 
                IParameterReferenceOperation: v4 (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'v4')

        Next (Regular) Block[B12]
    Block[B12] - Block
        Predecessors: [B10] [B11]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'a1 = New In ... If(v3, v4)}')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32(), IsInvalid, IsImplicit) (Syntax: 'a1 = New In ... If(v3, v4)}')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32(), IsImplicit) (Syntax: 'a1')
                  Right: 
                    IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32(), IsInvalid) (Syntax: 'New Integer ... If(v3, v4)}')
                      Dimension Sizes(1):
                          IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(d1, d2)')
                      Initializer: 
                        IArrayInitializerOperation (2 elements) (OperationKind.ArrayInitializer, Type: null, IsInvalid) (Syntax: '{If(v1, v2), If(v3, v4)}')
                          Element Values(2):
                              IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'If(v1, v2)')
                              IFlowCaptureReferenceOperation: 7 (OperationKind.FlowCaptureReference, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'If(v3, v4)')

        Next (Regular) Block[B13]
            Leaving: {R1}
}

Block[B13] - Exit
    Predecessors: [B12]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30949: Array initializer cannot be specified for a non constant dimension; use the empty initializer '{}'.
        a1 = New Integer(If(d1, d2)) {If(v1, v2), If(v3, v4)}
                                     ~~~~~~~~~~~~~~~~~~~~~~~~
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact>
        Public Sub ArrayCreationAndInitializer_ControlFlowInMultipleDimensionsAndInitializerValues_MultiDimArray()
            Dim source = <![CDATA[
Class C
    Private Sub M(a1 As Integer(,), v1 As Integer?, v2 As Integer, v3 As Integer?, v4 As Integer, d1 As Integer?, d2 As Integer, d3 As Integer?, d4 As Integer) 'BIND:"Private Sub M(a1 As Integer(,), v1 As Integer?, v2 As Integer, v3 As Integer?, v4 As Integer, d1 As Integer?, d2 As Integer, d3 As Integer?, d4 As Integer)"
        a1 = New Integer(If(d1, d2), If(d3, d4)) {{If(v1, v2)}, {If(v3, v4)}}
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
    CaptureIds: [0] [3] [6] [8] [10]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a1')
              Value: 
                IParameterReferenceOperation: a1 (OperationKind.ParameterReference, Type: System.Int32(,)) (Syntax: 'a1')

        Next (Regular) Block[B2]
            Entering: {R2} {R3}

    .locals {R2}
    {
        CaptureIds: [2]
        .locals {R3}
        {
            CaptureIds: [1]
            Block[B2] - Block
                Predecessors: [B1]
                Statements (1)
                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd1')
                      Value: 
                        IParameterReferenceOperation: d1 (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'd1')

                Jump if True (Regular) to Block[B4]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'd1')
                      Operand: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'd1')
                    Leaving: {R3}

                Next (Regular) Block[B3]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd1')
                      Value: 
                        IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'd1')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'd1')
                          Arguments(0)

                Next (Regular) Block[B5]
                    Leaving: {R3}
        }

        Block[B4] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd2')
                  Value: 
                    IParameterReferenceOperation: d2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'd2')

            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'If(d1, d2)')
                  Value: 
                    IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, IsImplicit) (Syntax: 'If(d1, d2)')
                      Left: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(d1, d2)')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'If(d1, d2)')

            Next (Regular) Block[B6]
                Leaving: {R2}
                Entering: {R4} {R5}
    }
    .locals {R4}
    {
        CaptureIds: [5]
        .locals {R5}
        {
            CaptureIds: [4]
            Block[B6] - Block
                Predecessors: [B5]
                Statements (1)
                    IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd3')
                      Value: 
                        IParameterReferenceOperation: d3 (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'd3')

                Jump if True (Regular) to Block[B8]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'd3')
                      Operand: 
                        IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'd3')
                    Leaving: {R5}

                Next (Regular) Block[B7]
            Block[B7] - Block
                Predecessors: [B6]
                Statements (1)
                    IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd3')
                      Value: 
                        IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'd3')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'd3')
                          Arguments(0)

                Next (Regular) Block[B9]
                    Leaving: {R5}
        }

        Block[B8] - Block
            Predecessors: [B6]
            Statements (1)
                IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd4')
                  Value: 
                    IParameterReferenceOperation: d4 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'd4')

            Next (Regular) Block[B9]
        Block[B9] - Block
            Predecessors: [B7] [B8]
            Statements (1)
                IFlowCaptureOperation: 6 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'If(d3, d4)')
                  Value: 
                    IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, IsImplicit) (Syntax: 'If(d3, d4)')
                      Left: 
                        IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(d3, d4)')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'If(d3, d4)')

            Next (Regular) Block[B10]
                Leaving: {R4}
                Entering: {R6}
    }
    .locals {R6}
    {
        CaptureIds: [7]
        Block[B10] - Block
            Predecessors: [B9]
            Statements (1)
                IFlowCaptureOperation: 7 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'v1')
                  Value: 
                    IParameterReferenceOperation: v1 (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32), IsInvalid) (Syntax: 'v1')

            Jump if True (Regular) to Block[B12]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'v1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 7 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsInvalid, IsImplicit) (Syntax: 'v1')
                Leaving: {R6}

            Next (Regular) Block[B11]
        Block[B11] - Block
            Predecessors: [B10]
            Statements (1)
                IFlowCaptureOperation: 8 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'v1')
                  Value: 
                    IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'v1')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 7 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsInvalid, IsImplicit) (Syntax: 'v1')
                      Arguments(0)

            Next (Regular) Block[B13]
                Leaving: {R6}
                Entering: {R7}
    }

    Block[B12] - Block
        Predecessors: [B10]
        Statements (1)
            IFlowCaptureOperation: 8 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'v2')
              Value: 
                IParameterReferenceOperation: v2 (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'v2')

        Next (Regular) Block[B13]
            Entering: {R7}

    .locals {R7}
    {
        CaptureIds: [9]
        Block[B13] - Block
            Predecessors: [B11] [B12]
            Statements (1)
                IFlowCaptureOperation: 9 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'v3')
                  Value: 
                    IParameterReferenceOperation: v3 (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32), IsInvalid) (Syntax: 'v3')

            Jump if True (Regular) to Block[B15]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'v3')
                  Operand: 
                    IFlowCaptureReferenceOperation: 9 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsInvalid, IsImplicit) (Syntax: 'v3')
                Leaving: {R7}

            Next (Regular) Block[B14]
        Block[B14] - Block
            Predecessors: [B13]
            Statements (1)
                IFlowCaptureOperation: 10 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'v3')
                  Value: 
                    IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'v3')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 9 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsInvalid, IsImplicit) (Syntax: 'v3')
                      Arguments(0)

            Next (Regular) Block[B16]
                Leaving: {R7}
    }

    Block[B15] - Block
        Predecessors: [B13]
        Statements (1)
            IFlowCaptureOperation: 10 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'v4')
              Value: 
                IParameterReferenceOperation: v4 (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'v4')

        Next (Regular) Block[B16]
    Block[B16] - Block
        Predecessors: [B14] [B15]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'a1 = New In ... f(v3, v4)}}')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32(,), IsInvalid, IsImplicit) (Syntax: 'a1 = New In ... f(v3, v4)}}')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32(,), IsImplicit) (Syntax: 'a1')
                  Right: 
                    IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32(,), IsInvalid) (Syntax: 'New Integer ... f(v3, v4)}}')
                      Dimension Sizes(2):
                          IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(d1, d2)')
                          IFlowCaptureReferenceOperation: 6 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(d3, d4)')
                      Initializer: 
                        IArrayInitializerOperation (2 elements) (OperationKind.ArrayInitializer, Type: null, IsInvalid) (Syntax: '{{If(v1, v2 ... f(v3, v4)}}')
                          Element Values(2):
                              IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null, IsInvalid) (Syntax: '{If(v1, v2)}')
                                Element Values(1):
                                    IFlowCaptureReferenceOperation: 8 (OperationKind.FlowCaptureReference, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'If(v1, v2)')
                              IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null, IsInvalid) (Syntax: '{If(v3, v4)}')
                                Element Values(1):
                                    IFlowCaptureReferenceOperation: 10 (OperationKind.FlowCaptureReference, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'If(v3, v4)')

        Next (Regular) Block[B17]
            Leaving: {R1}
}

Block[B17] - Exit
    Predecessors: [B16]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30949: Array initializer cannot be specified for a non constant dimension; use the empty initializer '{}'.
        a1 = New Integer(If(d1, d2), If(d3, d4)) {{If(v1, v2)}, {If(v3, v4)}}
                                                 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30949: Array initializer cannot be specified for a non constant dimension; use the empty initializer '{}'.
        a1 = New Integer(If(d1, d2), If(d3, d4)) {{If(v1, v2)}, {If(v3, v4)}}
                                                  ~~~~~~~~~~~~
BC30949: Array initializer cannot be specified for a non constant dimension; use the empty initializer '{}'.
        a1 = New Integer(If(d1, d2), If(d3, d4)) {{If(v1, v2)}, {If(v3, v4)}}
                                                                ~~~~~~~~~~~~
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub
    End Class
End Namespace
