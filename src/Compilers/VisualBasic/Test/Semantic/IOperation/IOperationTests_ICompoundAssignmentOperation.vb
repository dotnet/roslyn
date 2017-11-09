' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Operations

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub CompoundAssignment_NullArgumentToGetConversionThrows()
            Dim nullAssignment As ICompoundAssignmentOperation = Nothing
            Assert.Throws(Of ArgumentNullException)("compoundOperation", Function() nullAssignment.GetInConversion())
            Assert.Throws(Of ArgumentNullException)("compoundOperation", Function() nullAssignment.GetOutConversion())
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



    End Class
End Namespace
