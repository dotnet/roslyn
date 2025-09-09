' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Operations
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub CompoundAssignment_NullArgumentToGetConversionThrows()
            Dim nullAssignment As ICompoundAssignmentOperation = Nothing
            Assert.Throws(Of ArgumentNullException)("compoundAssignment", Function() nullAssignment.GetInConversion())
            Assert.Throws(Of ArgumentNullException)("compoundAssignment", Function() nullAssignment.GetOutConversion())
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub CompoundAssignment_GetConversionOnValidNode_IdentityConversion()
            Dim source = <![CDATA[
Module Module1
    Sub Main()
        Dim x, y As New Integer
        x += y'BIND:"x += y"
    End Sub
End Module]]>.Value

            Dim fileName = "a.vb"
            Dim syntaxTree = Parse(source, fileName)

            Dim compilation = CreateCompilationWithMscorlib461AndVBRuntime({syntaxTree})
            Dim result = GetOperationTreeForTest(Of AssignmentStatementSyntax)(compilation, fileName)
            Dim compoundAssignment = DirectCast(DirectCast(result.operation, IExpressionStatementOperation).Operation, ICompoundAssignmentOperation)

            Dim identityConversion = New Conversion(Conversions.Identity)
            Assert.Equal(identityConversion, compoundAssignment.GetInConversion())
            Assert.Equal(identityConversion, compoundAssignment.GetOutConversion())
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub CompoundAssignment_GetConversionOnValidNode_StringConversion()
            Dim source = <![CDATA[
Module Module1
    Sub Main()
        Dim x, y As New Integer
        x &= y'BIND:"x &= y"
    End Sub
End Module]]>.Value

            Dim fileName = "a.vb"
            Dim syntaxTree = Parse(source, fileName)

            Dim compilation = CreateCompilationWithMscorlib461AndVBRuntime({syntaxTree})
            Dim result = GetOperationTreeForTest(Of AssignmentStatementSyntax)(compilation, fileName)
            Dim compoundAssignment = DirectCast(DirectCast(result.operation, IExpressionStatementOperation).Operation, ICompoundAssignmentOperation)

            Dim stringConversion = New Conversion(New KeyValuePair(Of ConversionKind, Symbols.MethodSymbol)(ConversionKind.NarrowingString, Nothing))
            Assert.Equal(stringConversion, compoundAssignment.GetInConversion())
            Assert.Equal(stringConversion, compoundAssignment.GetOutConversion())
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub CompoundAssignment_GetConversionOnValidNode_DifferentConversionInOut()
            Dim source = <![CDATA[
Module Module1
    Sub Main()
        Dim a As New C, x As New Integer
        a += x'BIND:"a += x"
    End Sub

    Class C
        Public Shared Widening Operator CType(i As Integer) As C
            Return Nothing
        End Operator
        Public Shared Widening Operator CType(c As C) As Integer
            Return 1
        End Operator
        Public Shared Operator +(c As Integer, i As C) As C
            Return 0
        End Operator
    End Class
End Module]]>.Value

            Dim fileName = "a.vb"
            Dim syntaxTree = Parse(source, fileName)

            Dim compilation = CreateCompilationWithMscorlib461AndVBRuntime({syntaxTree})
            Dim result = GetOperationTreeForTest(Of AssignmentStatementSyntax)(compilation, fileName)
            Dim compoundAssignment = DirectCast(DirectCast(result.operation, IExpressionStatementOperation).Operation, ICompoundAssignmentOperation)

            Dim methodSymbol = compilation.GetSymbolsWithName(Function(sym) sym = "op_Implicit", filter:=SymbolFilter.Member).
                                           Cast(Of MethodSymbol).
                                           Where(Function(sym) sym.ReturnType.SpecialType = SpecialType.System_Int32).
                                           Single()

            Assert.Equal(New Conversion(New KeyValuePair(Of ConversionKind, MethodSymbol)(ConversionKind.UserDefined Or ConversionKind.Widening, methodSymbol)),
                         compoundAssignment.GetInConversion())
            Assert.Equal(New Conversion(Conversions.Identity), compoundAssignment.GetOutConversion())
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub CompoundAssignmentOperation_UserDefinedOperatorInConversion()
            Dim source = <![CDATA[
Module Module1

    Sub Main()
        Dim a As New C, x As New Integer
        a += x'BIND:"a += x"
    End Sub

    Class C
        Public Shared Widening Operator CType(i As Integer) As C
            Return Nothing
        End Operator
        Public Shared Widening Operator CType(c As C) As Integer
            Return 1
        End Operator
        Public Shared Operator +(c As Integer, i As C) As C
            Return 0
        End Operator
    End Class
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a += x')
  Expression: 
    ICompoundAssignmentOperation (BinaryOperatorKind.Add, Checked) (OperatorMethod: Function Module1.C.op_Addition(c As System.Int32, i As Module1.C) As Module1.C) (OperationKind.CompoundAssignment, Type: Module1.C, IsImplicit) (Syntax: 'a += x')
      InConversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: Function Module1.C.op_Implicit(c As Module1.C) As System.Int32)
      OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Left: 
        ILocalReferenceOperation: a (OperationKind.LocalReference, Type: Module1.C) (Syntax: 'a')
      Right: 
        IConversionOperation (TryCast: False, Unchecked) (OperatorMethod: Function Module1.C.op_Implicit(i As System.Int32) As Module1.C) (OperationKind.Conversion, Type: Module1.C, IsImplicit) (Syntax: 'x')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: Function Module1.C.op_Implicit(i As System.Int32) As Module1.C)
          Operand: 
            ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of AssignmentStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub CompoundAssignmentOperation_UserDefinedOperatorOutConversion()
            Dim source = <![CDATA[
Module Module1

    Sub Main()
        Dim a As New C, x As New Integer
        a += x'BIND:"a += x"
    End Sub

    Class C
        Public Shared Widening Operator CType(i As Integer) As C
            Return Nothing
        End Operator
        Public Shared Widening Operator CType(c As C) As Integer
            Return 1
        End Operator
        Public Shared Operator +(c As C, i As Integer) As Integer
            Return 0
        End Operator
    End Class
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a += x')
  Expression: 
    ICompoundAssignmentOperation (BinaryOperatorKind.Add, Checked) (OperatorMethod: Function Module1.C.op_Addition(c As Module1.C, i As System.Int32) As System.Int32) (OperationKind.CompoundAssignment, Type: Module1.C, IsImplicit) (Syntax: 'a += x')
      InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      OutConversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: Function Module1.C.op_Implicit(i As System.Int32) As Module1.C)
      Left: 
        ILocalReferenceOperation: a (OperationKind.LocalReference, Type: Module1.C) (Syntax: 'a')
      Right: 
        ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of AssignmentStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub CompoundAssignmentOperation_UserDefinedOperatorInOutConversion()
            Dim source = <![CDATA[
Module Module1

    Sub Main()
        Dim a As New C, x As New Integer
        a += x'BIND:"a += x"
    End Sub

    Class C
        Public Shared Widening Operator CType(i As Integer) As C
            Return Nothing
        End Operator
        Public Shared Widening Operator CType(c As C) As Integer
            Return 1
        End Operator
        Public Shared Operator +(c As Integer, i As C) As Integer
            Return 0
        End Operator
    End Class
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a += x')
  Expression: 
    ICompoundAssignmentOperation (BinaryOperatorKind.Add, Checked) (OperatorMethod: Function Module1.C.op_Addition(c As System.Int32, i As Module1.C) As System.Int32) (OperationKind.CompoundAssignment, Type: Module1.C, IsImplicit) (Syntax: 'a += x')
      InConversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: Function Module1.C.op_Implicit(c As Module1.C) As System.Int32)
      OutConversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: Function Module1.C.op_Implicit(i As System.Int32) As Module1.C)
      Left: 
        ILocalReferenceOperation: a (OperationKind.LocalReference, Type: Module1.C) (Syntax: 'a')
      Right: 
        IConversionOperation (TryCast: False, Unchecked) (OperatorMethod: Function Module1.C.op_Implicit(i As System.Int32) As Module1.C) (OperationKind.Conversion, Type: Module1.C, IsImplicit) (Syntax: 'x')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: Function Module1.C.op_Implicit(i As System.Int32) As Module1.C)
          Operand: 
            ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of AssignmentStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub CompoundAssignmentExpression_BuiltInOperatorInOutConversion()
            Dim source = <![CDATA[
Module Module1

    Sub Main()
        Dim x, y As New Integer
        x /= y'BIND:"x /= y"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x /= y')
  Expression: 
    ICompoundAssignmentOperation (BinaryOperatorKind.Divide, Checked) (OperationKind.CompoundAssignment, Type: System.Int32, IsImplicit) (Syntax: 'x /= y')
      InConversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      OutConversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Left: 
        ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
      Right: 
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Double, IsImplicit) (Syntax: 'y')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of AssignmentStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub CompoundAssignmentExpression_InvalidNoOperator()
            Dim source = <![CDATA[
Module Module1

    Sub Main()
        Dim a As New C, x As New Integer
        a += x'BIND:"a += x"
    End Sub

    Class C
        Public Shared Widening Operator CType(i As Integer) As C
            Return Nothing
        End Operator
        Public Shared Widening Operator CType(c As C) As Integer
            Return 1
        End Operator
    End Class
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'a += x')
  Expression: 
    ICompoundAssignmentOperation (BinaryOperatorKind.Add, Checked) (OperationKind.CompoundAssignment, Type: Module1.C, IsInvalid, IsImplicit) (Syntax: 'a += x')
      InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      OutConversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Left: 
        ILocalReferenceOperation: a (OperationKind.LocalReference, Type: Module1.C, IsInvalid) (Syntax: 'a')
      Right: 
        ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32, IsInvalid) (Syntax: 'x')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30452: Operator '+' is not defined for types 'Module1.C' and 'Integer'.
        a += x'BIND:"a += x"
        ~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of AssignmentStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub CompoundAssignmentExpression_InvalidNoOutConversion()
            Dim source = <![CDATA[
Module Module1

    Sub Main()
        Dim a As New C, x As New Integer
        a += x'BIND:"a += x"
    End Sub

    Class C
        Public Shared Widening Operator CType(c As C) As Integer
            Return 1
        End Operator
        Public Shared Operator +(c As C, i As Integer) As Integer
            Return 1
        End Operator
    End Class
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'a += x')
  Expression: 
    ICompoundAssignmentOperation (BinaryOperatorKind.Add, Checked) (OperatorMethod: Function Module1.C.op_Addition(c As Module1.C, i As System.Int32) As System.Int32) (OperationKind.CompoundAssignment, Type: Module1.C, IsInvalid, IsImplicit) (Syntax: 'a += x')
      InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      OutConversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Left: 
        ILocalReferenceOperation: a (OperationKind.LocalReference, Type: Module1.C, IsInvalid) (Syntax: 'a')
      Right: 
        ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32, IsInvalid) (Syntax: 'x')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30311: Value of type 'Integer' cannot be converted to 'Module1.C'.
        a += x'BIND:"a += x"
        ~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of AssignmentStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub CompoundAssignmentExpression_InvalidNoInConversion()
            Dim source = <![CDATA[
Module Module1

    Sub Main()
        Dim a As New C, x As New Integer
        a += x'BIND:"a += x"
    End Sub

    Class C
        Public Shared Widening Operator CType(c As Integer) As C
            Return 1
        End Operator
        Public Shared Operator +(c As Integer, i As C) As Integer
            Return 1
        End Operator
    End Class
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'a += x')
  Expression: 
    ICompoundAssignmentOperation (BinaryOperatorKind.Add, Checked) (OperationKind.CompoundAssignment, Type: Module1.C, IsInvalid, IsImplicit) (Syntax: 'a += x')
      InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      OutConversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: Function Module1.C.op_Implicit(c As System.Int32) As Module1.C)
      Left: 
        ILocalReferenceOperation: a (OperationKind.LocalReference, Type: Module1.C, IsInvalid) (Syntax: 'a')
      Right: 
        ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30311: Value of type 'Module1.C' cannot be converted to 'Integer'.
        a += x'BIND:"a += x"
        ~
BC42004: Expression recursively calls the containing Operator 'Public Shared Widening Operator CType(c As Integer) As Module1.C'.
            Return 1
                   ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of AssignmentStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub CompoundAssignmentExpression_InvalidNoOperatorOrConversions()
            Dim source = <![CDATA[
Module Module1

    Sub Main()
        Dim a As New C, x As New Integer
        a += x'BIND:"a += x"
    End Sub

    Class C
    End Class
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'a += x')
  Expression: 
    ICompoundAssignmentOperation (BinaryOperatorKind.Add, Checked) (OperationKind.CompoundAssignment, Type: Module1.C, IsInvalid, IsImplicit) (Syntax: 'a += x')
      InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      OutConversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Left: 
        ILocalReferenceOperation: a (OperationKind.LocalReference, Type: Module1.C, IsInvalid) (Syntax: 'a')
      Right: 
        ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32, IsInvalid) (Syntax: 'x')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30452: Operator '+' is not defined for types 'Module1.C' and 'Integer'.
        a += x'BIND:"a += x"
        ~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of AssignmentStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub CompoundAssignment_01()
            Dim source = <![CDATA[
Imports System
Public Class C
    Public Sub M(a As Integer(), b As Integer?, c As Integer)'BIND:"Public Sub M(a As Integer(), b As Integer?, c As Integer)"
        a(0) += If(b, c)
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
    CaptureIds: [0] [2]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a(0)')
              Value: 
                IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: 'a(0)')
                  Array reference: 
                    IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32()) (Syntax: 'a')
                  Indices(1):
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
                  Value: 
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'b')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'b')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'b')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
                  Value: 
                    IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'b')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'b')
                      Arguments(0)

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value: 
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'c')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a(0) += If(b, c)')
              Expression: 
                ICompoundAssignmentOperation (BinaryOperatorKind.Add, Checked) (OperationKind.CompoundAssignment, Type: System.Int32, IsImplicit) (Syntax: 'a(0) += If(b, c)')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'a(0)')
                  Right: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(b, c)')

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub CompoundAssignment_02()
            Dim source = <![CDATA[
Imports System
Public Class C
    Public Sub M(a As Integer(), b As Integer?, c As Integer)'BIND:"Public Sub M(a As Integer(), b As Integer?, c As Integer)"
        a(0) -= If(b, c)
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
    CaptureIds: [0] [2]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a(0)')
              Value: 
                IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: 'a(0)')
                  Array reference: 
                    IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32()) (Syntax: 'a')
                  Indices(1):
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
                  Value: 
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'b')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'b')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'b')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
                  Value: 
                    IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'b')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'b')
                      Arguments(0)

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value: 
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'c')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a(0) -= If(b, c)')
              Expression: 
                ICompoundAssignmentOperation (BinaryOperatorKind.Subtract, Checked) (OperationKind.CompoundAssignment, Type: System.Int32, IsImplicit) (Syntax: 'a(0) -= If(b, c)')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'a(0)')
                  Right: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(b, c)')

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub CompoundAssignment_03()
            Dim source = <![CDATA[
Imports System
Public Class C
    Public Sub M(a As Integer(), b As Integer?, c As Integer)'BIND:"Public Sub M(a As Integer(), b As Integer?, c As Integer)"
        a(0) *= If(b, c)
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
    CaptureIds: [0] [2]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a(0)')
              Value: 
                IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: 'a(0)')
                  Array reference: 
                    IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32()) (Syntax: 'a')
                  Indices(1):
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
                  Value: 
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'b')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'b')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'b')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
                  Value: 
                    IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'b')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'b')
                      Arguments(0)

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value: 
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'c')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a(0) *= If(b, c)')
              Expression: 
                ICompoundAssignmentOperation (BinaryOperatorKind.Multiply, Checked) (OperationKind.CompoundAssignment, Type: System.Int32, IsImplicit) (Syntax: 'a(0) *= If(b, c)')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'a(0)')
                  Right: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(b, c)')

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub CompoundAssignment_04()
            Dim source = <![CDATA[
Imports System
Public Class C
    Public Sub M(a As Integer(), b As Integer?, c As Integer)'BIND:"Public Sub M(a As Integer(), b As Integer?, c As Integer)"
        a(0) /= If(b, c)
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
    CaptureIds: [0] [2]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a(0)')
              Value: 
                IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: 'a(0)')
                  Array reference: 
                    IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32()) (Syntax: 'a')
                  Indices(1):
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
                  Value: 
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'b')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'b')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'b')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
                  Value: 
                    IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'b')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'b')
                      Arguments(0)

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value: 
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'c')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a(0) /= If(b, c)')
              Expression: 
                ICompoundAssignmentOperation (BinaryOperatorKind.Divide, Checked) (OperationKind.CompoundAssignment, Type: System.Int32, IsImplicit) (Syntax: 'a(0) /= If(b, c)')
                  InConversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'a(0)')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Double, IsImplicit) (Syntax: 'If(b, c)')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (WideningNumeric)
                      Operand: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(b, c)')

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub CompoundAssignment_05()
            Dim source = <![CDATA[
Imports System
Public Class C
    Public Sub M(a As Integer(), b As Integer?, c As Integer)'BIND:"Public Sub M(a As Integer(), b As Integer?, c As Integer)"
        a(0) \= If(b, c)
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
    CaptureIds: [0] [2]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a(0)')
              Value: 
                IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: 'a(0)')
                  Array reference: 
                    IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32()) (Syntax: 'a')
                  Indices(1):
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
                  Value: 
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'b')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'b')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'b')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
                  Value: 
                    IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'b')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'b')
                      Arguments(0)

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value: 
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'c')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a(0) \= If(b, c)')
              Expression: 
                ICompoundAssignmentOperation (BinaryOperatorKind.IntegerDivide, Checked) (OperationKind.CompoundAssignment, Type: System.Int32, IsImplicit) (Syntax: 'a(0) \= If(b, c)')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'a(0)')
                  Right: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(b, c)')

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub CompoundAssignment_06()
            Dim source = <![CDATA[
Imports System
Public Class C
    Public Sub M(a As Integer(), b As Integer?, c As Integer)'BIND:"Public Sub M(a As Integer(), b As Integer?, c As Integer)"
        a(0) &= If(b, c)
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
    CaptureIds: [0] [2]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a(0)')
              Value: 
                IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: 'a(0)')
                  Array reference: 
                    IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32()) (Syntax: 'a')
                  Indices(1):
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
                  Value: 
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'b')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'b')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'b')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
                  Value: 
                    IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'b')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'b')
                      Arguments(0)

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value: 
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'c')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a(0) &= If(b, c)')
              Expression: 
                ICompoundAssignmentOperation (BinaryOperatorKind.Concatenate, Checked) (OperationKind.CompoundAssignment, Type: System.Int32, IsImplicit) (Syntax: 'a(0) &= If(b, c)')
                  InConversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'a(0)')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String, IsImplicit) (Syntax: 'If(b, c)')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (NarrowingString)
                      Operand: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(b, c)')

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub CompoundAssignment_07()
            Dim source = <![CDATA[
Imports System
Public Class C
    Public Sub M(a As Integer(), b As Integer?, c As Integer)'BIND:"Public Sub M(a As Integer(), b As Integer?, c As Integer)"
        a(0) ^= If(b, c)
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
    CaptureIds: [0] [2]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a(0)')
              Value: 
                IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: 'a(0)')
                  Array reference: 
                    IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32()) (Syntax: 'a')
                  Indices(1):
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
                  Value: 
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'b')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'b')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'b')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
                  Value: 
                    IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'b')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'b')
                      Arguments(0)

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value: 
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'c')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a(0) ^= If(b, c)')
              Expression: 
                ICompoundAssignmentOperation (BinaryOperatorKind.Power, Checked) (OperationKind.CompoundAssignment, Type: System.Int32, IsImplicit) (Syntax: 'a(0) ^= If(b, c)')
                  InConversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'a(0)')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Double, IsImplicit) (Syntax: 'If(b, c)')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (WideningNumeric)
                      Operand: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(b, c)')

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub CompoundAssignment_08()
            Dim source = <![CDATA[
Imports System
Public Class C
    Public Sub M(a As Integer(), b As Integer?, c As Integer)'BIND:"Public Sub M(a As Integer(), b As Integer?, c As Integer)"
        a(0) <<= If(b, c)
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
    CaptureIds: [0] [2]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a(0)')
              Value: 
                IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: 'a(0)')
                  Array reference: 
                    IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32()) (Syntax: 'a')
                  Indices(1):
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
                  Value: 
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'b')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'b')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'b')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
                  Value: 
                    IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'b')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'b')
                      Arguments(0)

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value: 
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'c')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a(0) <<= If(b, c)')
              Expression: 
                ICompoundAssignmentOperation (BinaryOperatorKind.LeftShift, Checked) (OperationKind.CompoundAssignment, Type: System.Int32, IsImplicit) (Syntax: 'a(0) <<= If(b, c)')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'a(0)')
                  Right: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(b, c)')

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub CompoundAssignment_09()
            Dim source = <![CDATA[
Imports System
Public Class C
    Public Sub M(a As Integer(), b As Integer?, c As Integer)'BIND:"Public Sub M(a As Integer(), b As Integer?, c As Integer)"
        a(0) >>= If(b, c)
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
    CaptureIds: [0] [2]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a(0)')
              Value: 
                IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: 'a(0)')
                  Array reference: 
                    IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32()) (Syntax: 'a')
                  Indices(1):
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
                  Value: 
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'b')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'b')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'b')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
                  Value: 
                    IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'b')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'b')
                      Arguments(0)

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value: 
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'c')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a(0) >>= If(b, c)')
              Expression: 
                ICompoundAssignmentOperation (BinaryOperatorKind.RightShift, Checked) (OperationKind.CompoundAssignment, Type: System.Int32, IsImplicit) (Syntax: 'a(0) >>= If(b, c)')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'a(0)')
                  Right: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(b, c)')

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

    End Class
End Namespace
