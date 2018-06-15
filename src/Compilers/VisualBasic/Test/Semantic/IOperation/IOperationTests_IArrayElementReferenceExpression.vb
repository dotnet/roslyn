' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")>
        Public Sub ArrayElementReference_SingleDimensionArray_ConstantIndex()
            Dim source = <![CDATA[
Class C
    Public Sub F(args As String())
        Dim a = args(0)'BIND:"args(0)"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.String) (Syntax: 'args(0)')
  Array reference: 
    IParameterReferenceOperation: args (OperationKind.ParameterReference, Type: System.String()) (Syntax: 'args')
  Indices(1):
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")>
        Public Sub ArrayElementReference_SingleDimensionArray_NonConstantIndex()
            Dim source = <![CDATA[
Class C
    Public Sub F(args As String(), x As Integer)
        Dim a = args(x)'BIND:"args(x)"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.String) (Syntax: 'args(x)')
  Array reference: 
    IParameterReferenceOperation: args (OperationKind.ParameterReference, Type: System.String()) (Syntax: 'args')
  Indices(1):
      IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")>
        Public Sub ArrayElementReference_SingleDimensionArray_FunctionCallArrayReference()
            Dim source = <![CDATA[
Class C
    Public Sub F()
        Dim a = F2()(0)'BIND:"F2()(0)"
    End Sub

    Public Function F2() As String()
        Return Nothing
    End Function
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.String) (Syntax: 'F2()(0)')
  Array reference: 
    IInvocationOperation ( Function C.F2() As System.String()) (OperationKind.Invocation, Type: System.String()) (Syntax: 'F2()')
      Instance Receiver: 
        IInstanceReferenceOperation (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'F2')
      Arguments(0)
  Indices(1):
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")>
        Public Sub ArrayElementReference_MultiDimensionArray_ConstantIndices()
            Dim source = <![CDATA[
Class C
    Public Sub F(args As String(,))
        Dim a = args(0, 1)'BIND:"args(0, 1)"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.String) (Syntax: 'args(0, 1)')
  Array reference: 
    IParameterReferenceOperation: args (OperationKind.ParameterReference, Type: System.String(,)) (Syntax: 'args')
  Indices(2):
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")>
        Public Sub ArrayElementReference_MultiDimensionArray_NonConstantIndices()
            Dim source = <![CDATA[
Class C
    Public Sub F(args As String(,), x As Integer, y As Integer)
        Dim a = args(x, y)'BIND:"args(x, y)"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.String) (Syntax: 'args(x, y)')
  Array reference: 
    IParameterReferenceOperation: args (OperationKind.ParameterReference, Type: System.String(,)) (Syntax: 'args')
  Indices(2):
      IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
      IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'y')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")>
        Public Sub ArrayElementReference_MultiDimensionArray_InvocationInIndex()
            Dim source = <![CDATA[
Class C
    Public Sub F(args As String(,), x As Integer)
        Dim a = args(x, F2)'BIND:"args(x, F2)"
    End Sub

    Public Function F2() As Integer
        Return 0
    End Function
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.String) (Syntax: 'args(x, F2)')
  Array reference: 
    IParameterReferenceOperation: args (OperationKind.ParameterReference, Type: System.String(,)) (Syntax: 'args')
  Indices(2):
      IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
      IInvocationOperation ( Function C.F2() As System.Int32) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'F2')
        Instance Receiver: 
          IInstanceReferenceOperation (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'F2')
        Arguments(0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")>
        Public Sub ArrayElementReference_JaggedArray_ConstantIndices()
            Dim source = <![CDATA[
Class C
    Public Sub F(args As String()())
        Dim a = args(0)(1)'BIND:"args(0)(1)"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.String) (Syntax: 'args(0)(1)')
  Array reference: 
    IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.String()) (Syntax: 'args(0)')
      Array reference: 
        IParameterReferenceOperation: args (OperationKind.ParameterReference, Type: System.String()()) (Syntax: 'args')
      Indices(1):
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
  Indices(1):
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")>
        Public Sub ArrayElementReference_JaggedArray_NonConstantIndices()
            Dim source = <![CDATA[
Class C
    Public Sub F(args As String()())
        Dim x As Integer = 0
        Dim a = args(x)(F2)'BIND:"args(x)(F2)"
    End Sub

    Public Function F2() As Integer
        Return 0
    End Function
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.String) (Syntax: 'args(x)(F2)')
  Array reference: 
    IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.String()) (Syntax: 'args(x)')
      Array reference: 
        IParameterReferenceOperation: args (OperationKind.ParameterReference, Type: System.String()()) (Syntax: 'args')
      Indices(1):
          ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
  Indices(1):
      IInvocationOperation ( Function C.F2() As System.Int32) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'F2')
        Instance Receiver: 
          IInstanceReferenceOperation (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'F2')
        Arguments(0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")>
        Public Sub ArrayElementReference_JaggedArrayOfMultidimensionalArrays()
            Dim source = <![CDATA[
Class C
    Public Sub F(args As String()(,))
        Dim x As Integer = 0
        Dim a = args(x)(0, F2)'BIND:"args(x)(0, F2)"
    End Sub

    Public Function F2() As Integer
        Return 0
    End Function
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.String) (Syntax: 'args(x)(0, F2)')
  Array reference: 
    IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.String(,)) (Syntax: 'args(x)')
      Array reference: 
        IParameterReferenceOperation: args (OperationKind.ParameterReference, Type: System.String()(,)) (Syntax: 'args')
      Indices(1):
          ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
  Indices(2):
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
      IInvocationOperation ( Function C.F2() As System.Int32) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'F2')
        Instance Receiver: 
          IInstanceReferenceOperation (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'F2')
        Arguments(0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")>
        Public Sub ArrayElementReference_ImplicitConversionInIndexExpression()
            Dim source = <![CDATA[
Class C
    Public Sub F(args As String(), b As Byte)
        Dim a = args(b)'BIND:"args(b)"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.String) (Syntax: 'args(b)')
  Array reference: 
    IParameterReferenceOperation: args (OperationKind.ParameterReference, Type: System.String()) (Syntax: 'args')
  Indices(1):
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsImplicit) (Syntax: 'b')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Byte) (Syntax: 'b')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")>
        Public Sub ArrayElementReference_ExplicitConversionInIndexExpression()
            Dim source = <![CDATA[
Option Strict On

Class C
    Public Sub F(args As String(), o As Object)
        Dim a = args(DirectCast(o, Integer))'BIND:"args(DirectCast(o, Integer))"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.String) (Syntax: 'args(Direct ... , Integer))')
  Array reference: 
    IParameterReferenceOperation: args (OperationKind.ParameterReference, Type: System.String()) (Syntax: 'args')
  Indices(1):
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32) (Syntax: 'DirectCast(o, Integer)')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IParameterReferenceOperation: o (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")>
        Public Sub ArrayElementReference_ImplicitUserDefinedConversionInIndexExpression()
            Dim source = <![CDATA[
Option Strict On

Class C
    Public Sub F(args As String(), c As C)
        Dim a = args(c)'BIND:"args(c)"
    End Sub

    Public Shared Widening Operator CType(ByVal c As C) As Integer
        Return 0
    End Operator
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.String) (Syntax: 'args(c)')
  Array reference: 
    IParameterReferenceOperation: args (OperationKind.ParameterReference, Type: System.String()) (Syntax: 'args')
  Indices(1):
      IConversionOperation (TryCast: False, Unchecked) (OperatorMethod: Function C.op_Implicit(c As C) As System.Int32) (OperationKind.Conversion, Type: System.Int32, IsImplicit) (Syntax: 'c')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: Function C.op_Implicit(c As C) As System.Int32)
        Operand: 
          IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C) (Syntax: 'c')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")>
        Public Sub ArrayElementReference_ExplicitUserDefinedConversionInIndexExpression()
            Dim source = <![CDATA[
Option Strict On

Class C
    Public Sub F(args As String(), c As C)
        Dim a = args(CType(c, Integer))'BIND:"args(CType(c, Integer))"
    End Sub

    Public Shared Narrowing Operator CType(ByVal c As C) As Integer
        Return 0
    End Operator
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.String) (Syntax: 'args(CType(c, Integer))')
  Array reference: 
    IParameterReferenceOperation: args (OperationKind.ParameterReference, Type: System.String()) (Syntax: 'args')
  Indices(1):
      IConversionOperation (TryCast: False, Unchecked) (OperatorMethod: Function C.op_Explicit(c As C) As System.Int32) (OperationKind.Conversion, Type: System.Int32) (Syntax: 'CType(c, Integer)')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: Function C.op_Explicit(c As C) As System.Int32)
        Operand: 
          IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C) (Syntax: 'c')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")>
        Public Sub ArrayElementReference_ExplicitConversionInArrayReference()
            Dim source = <![CDATA[
Class C
    Public Sub F(o As Object, x As Integer)
        Dim a = DirectCast(o, String())(x)'BIND:"DirectCast(o, String())(x)"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.String) (Syntax: 'DirectCast( ... tring())(x)')
  Array reference: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String()) (Syntax: 'DirectCast(o, String())')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IParameterReferenceOperation: o (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o')
  Indices(1):
      IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")>
        Public Sub ArrayElementReferenceError_NoConversionInIndexExpression()
            Dim source = <![CDATA[
Option Strict On

Class C
    Public Sub F(args As String(), c As C)
        Dim a = args(c)'BIND:"args(c)"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.String, IsInvalid) (Syntax: 'args(c)')
  Array reference: 
    IParameterReferenceOperation: args (OperationKind.ParameterReference, Type: System.String()) (Syntax: 'args')
  Indices(1):
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'c')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C, IsInvalid) (Syntax: 'c')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30311: Value of type 'C' cannot be converted to 'Integer'.
        Dim a = args(c)'BIND:"args(c)"
                     ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")>
        Public Sub ArrayElementReferenceError_MissingExplicitCastInIndexExpression()
            Dim source = <![CDATA[
Option Strict On

Class C
    Public Sub F(args As String(), c As C)
        Dim a = args(c)'BIND:"args(c)"
    End Sub

    Public Shared Narrowing Operator CType(ByVal c As C) As Integer
        Return 0
    End Operator
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.String, IsInvalid) (Syntax: 'args(c)')
  Array reference: 
    IParameterReferenceOperation: args (OperationKind.ParameterReference, Type: System.String()) (Syntax: 'args')
  Indices(1):
      IConversionOperation (TryCast: False, Unchecked) (OperatorMethod: Function C.op_Explicit(c As C) As System.Int32) (OperationKind.Conversion, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'c')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: Function C.op_Explicit(c As C) As System.Int32)
        Operand: 
          IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C, IsInvalid) (Syntax: 'c')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30512: Option Strict On disallows implicit conversions from 'C' to 'Integer'.
        Dim a = args(c)'BIND:"args(c)"
                     ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")>
        Public Sub ArrayElementReferenceError_NoIndices()
            Dim source = <![CDATA[
Option Strict On

Class C
    Public Sub F(args As String(), c As C)
        Dim a = args()'BIND:"args()"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.String, IsInvalid) (Syntax: 'args()')
  Array reference: 
    IParameterReferenceOperation: args (OperationKind.ParameterReference, Type: System.String(), IsInvalid) (Syntax: 'args')
  Indices(0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30105: Number of indices is less than the number of dimensions of the indexed array.
        Dim a = args()'BIND:"args()"
                    ~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")>
        Public Sub ArrayElementReferenceError_BadIndexing()
            Dim source = <![CDATA[
Option Strict On

Class C
    Public Sub F(c As C)
        Dim a = c(0)'BIND:"c(0)"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'c(0)')
  Children(2):
      IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C, IsInvalid) (Syntax: 'c')
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30367: Class 'C' cannot be indexed because it has no default property.
        Dim a = c(0)'BIND:"c(0)"
                ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")>
        Public Sub ArrayElementReferenceError_BadIndexCount()
            Dim source = <![CDATA[
Option Strict On

Class C
    Public Sub F(args As String())
        Dim a = args(0, 0)'BIND:"args(0, 0)"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.String, IsInvalid) (Syntax: 'args(0, 0)')
  Array reference: 
    IParameterReferenceOperation: args (OperationKind.ParameterReference, Type: System.String(), IsInvalid) (Syntax: 'args')
  Indices(2):
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsInvalid) (Syntax: '0')
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsInvalid) (Syntax: '0')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30106: Number of indices exceeds the number of dimensions of the indexed array.
        Dim a = args(0, 0)'BIND:"args(0, 0)"
                    ~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")>
        Public Sub ArrayElementReferenceError_ExtraElementAccessOperator()
            Dim source = <![CDATA[
Option Strict On

Class C
    Public Sub F(args As C())
        Dim a = args(0)()'BIND:"args(0)()"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'args(0)()')
  Children(1):
      IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: C, IsInvalid) (Syntax: 'args(0)')
        Array reference: 
          IParameterReferenceOperation: args (OperationKind.ParameterReference, Type: C(), IsInvalid) (Syntax: 'args')
        Indices(1):
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsInvalid) (Syntax: '0')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30367: Class 'C' cannot be indexed because it has no default property.
        Dim a = args(0)()'BIND:"args(0)()"
                ~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")>
        Public Sub ArrayElementReferenceError_IndexErrorExpression()
            Dim source = <![CDATA[
Option Strict On

Class C
    Public Sub F()
        Dim a = ErrorExpression(0)'BIND:"ErrorExpression(0)"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'ErrorExpression(0)')
  Children(2):
      IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'ErrorExpression')
        Children(0)
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30451: 'ErrorExpression' is not declared. It may be inaccessible due to its protection level.
        Dim a = ErrorExpression(0)'BIND:"ErrorExpression(0)"
                ~~~~~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")>
        Public Sub ArrayElementReferenceError_SyntaxErrorInIndexer_MissingValue()
            Dim source = <![CDATA[
Option Strict On

Class C
    Public Sub F(args As String())
        Dim a = args(0,)'BIND:"args(0,)"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.String, IsInvalid) (Syntax: 'args(0,)')
  Array reference: 
    IParameterReferenceOperation: args (OperationKind.ParameterReference, Type: System.String(), IsInvalid) (Syntax: 'args')
  Indices(2):
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsInvalid) (Syntax: '0')
      IOmittedArgumentOperation (OperationKind.OmittedArgument, Type: null, IsInvalid) (Syntax: '')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30106: Number of indices exceeds the number of dimensions of the indexed array.
        Dim a = args(0,)'BIND:"args(0,)"
                    ~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")>
        Public Sub ArrayElementReferenceError_SyntaxErrorInIndexer_MissingParens()
            Dim source = <![CDATA[
Option Strict On

Class C
    Public Sub F(args As String())
        Dim a = args('BIND:"args("
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.String, IsInvalid) (Syntax: 'args(')
  Array reference: 
    IParameterReferenceOperation: args (OperationKind.ParameterReference, Type: System.String()) (Syntax: 'args')
  Indices(1):
      IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
        Children(0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30198: ')' expected.
        Dim a = args('BIND:"args("
                     ~
BC30201: Expression expected.
        Dim a = args('BIND:"args("
                     ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")>
        Public Sub ArrayElementReferenceError_SyntaxErrorInIndexer_MissingParensAfterIndex()
            Dim source = <![CDATA[
Option Strict On

Class C
    Public Sub F(args As String())
        Dim a = args(0'BIND:"args(0"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.String, IsInvalid) (Syntax: 'args(0')
  Array reference: 
    IParameterReferenceOperation: args (OperationKind.ParameterReference, Type: System.String()) (Syntax: 'args')
  Indices(1):
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsInvalid) (Syntax: '0')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30198: ')' expected.
        Dim a = args(0'BIND:"args(0"
                      ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")>
        Public Sub ArrayElementReferenceError_SyntaxErrorInIndexer_DeeplyNestedParameterReference()
            Dim source = <![CDATA[
Option Strict On

Class C
    Public Sub F(args As String(), x As Integer, y As Integer)
        Dim a = args(y)()()()(x)'BIND:"args(y)()()()(x)"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'args(y)()()()(x)')
  Children(2):
      IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'args(y)()()()')
        Children(1):
            IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'args(y)()()')
              Children(1):
                  IInvalidOperation (OperationKind.Invalid, Type: System.Char, IsInvalid) (Syntax: 'args(y)()')
                    Children(1):
                        IOperation:  (OperationKind.None, Type: null, IsInvalid, IsImplicit) (Syntax: 'args(y)')
                          Children(1):
                              IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.String, IsInvalid) (Syntax: 'args(y)')
                                Array reference: 
                                  IParameterReferenceOperation: args (OperationKind.ParameterReference, Type: System.String(), IsInvalid) (Syntax: 'args')
                                Indices(1):
                                    IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'y')
      IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30455: Argument not specified for parameter 'index' of 'Public Overloads ReadOnly Default Property Chars(index As Integer) As Char'.
        Dim a = args(y)()()()(x)'BIND:"args(y)()()()(x)"
                ~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")>
        Public Sub ArrayElementReferenceError_NamedArgumentForArray()
            Dim source = <![CDATA[
Option Strict On

Class C
    Public Sub F(args As String(), x As Integer)
        Dim a = args(name:=x)'BIND:"args(name:=x)"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.String, IsInvalid) (Syntax: 'args(name:=x)')
  Array reference: 
    IParameterReferenceOperation: args (OperationKind.ParameterReference, Type: System.String(), IsInvalid) (Syntax: 'args')
  Indices(1):
      IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'x')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30075: Named arguments are not valid as array subscripts.
        Dim a = args(name:=x)'BIND:"args(name:=x)"
                ~~~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")>
        Public Sub ArrayElementReference_NegativeIndexExpression()
            Dim source = <![CDATA[
Class C
    Public Sub F(args As String())
        Dim a = args(-1)'BIND:"args(-1)"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.String) (Syntax: 'args(-1)')
  Array reference: 
    IParameterReferenceOperation: args (OperationKind.ParameterReference, Type: System.String()) (Syntax: 'args')
  Indices(1):
      IUnaryOperation (UnaryOperatorKind.Minus, Checked) (OperationKind.UnaryOperator, Type: System.Int32, Constant: -1) (Syntax: '-1')
        Operand: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub
    End Class
End Namespace
